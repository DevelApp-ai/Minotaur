/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Core.Models.Grammar;
using Minotaur.Projects.Grammar;

namespace Minotaur.Tests.Projects.Grammar;

[TestClass]
public class GrammarExtensionLoaderTests
{
    private const string SampleExtension = @"
Grammar: CEBNF
EmbeddedLanguages: JavaScript, CSS
ContextAware: true
BaseGrammar: HTMLEmbedded.grammar
MergeStrategy: Replace

/* HTML core tokens */
HTML_TAG_OPEN = <[a-zA-Z][a-zA-Z0-9]*

/* Explicit operations */
~HTML_TEXT_CONTENT = [^<>&]+
-HTML_COMMENT

@CONTEXT[CSS] {
    CSS_SELECTOR = [.#]?[a-zA-Z][a-zA-Z0-9_-]*
    -CSS_COMMENT
}

@VALIDATE {
    css_id_selector_validation: CSS_ID_SELECTOR -> HTML_ID_ATTR
}
";

    [TestMethod]
    public void Parse_WithHeader_ParsesHeaderFields()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors.Select(e => e.ToString())));
        var extension = result.Extension!;

        Assert.AreEqual(ExtensionGrammarType.CEBNF, extension.GrammarType);
        Assert.IsTrue(extension.IsContextAware);
        Assert.AreEqual("HTMLEmbedded.grammar", extension.BaseGrammarRef);
        Assert.AreEqual(ExtensionMergeStrategy.Replace, extension.MergeStrategy);
        CollectionAssert.AreEquivalent(new[] { "JavaScript", "CSS" }, extension.EmbeddedLanguages.Distinct().ToList());
    }

    [TestMethod]
    public void Parse_WithEntries_ParsesTokenEntries()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");
        var extension = result.Extension!;

        var tagOpen = extension.Entries.Single(e => e.Name == "HTML_TAG_OPEN");
        Assert.AreEqual(ExtensionAction.Add, tagOpen.Action);
        Assert.AreEqual("<[a-zA-Z][a-zA-Z0-9]*", tagOpen.Pattern);
        Assert.IsFalse(tagOpen.IsRule);
    }

    [TestMethod]
    public void Parse_WithOperationPrefixes_AppliesActions()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");
        var extension = result.Extension!;

        var overrideEntry = extension.Entries.Single(e => e.Name == "HTML_TEXT_CONTENT");
        Assert.AreEqual(ExtensionAction.Override, overrideEntry.Action);
        Assert.AreEqual("[^<>&]+", overrideEntry.Pattern);

        var removal = extension.Entries.Single(e => e.Name == "HTML_COMMENT");
        Assert.AreEqual(ExtensionAction.Remove, removal.Action);
        Assert.AreEqual(string.Empty, removal.Pattern);
        CollectionAssert.Contains(extension.Removals.ToList(), "HTML_COMMENT");
    }

    [TestMethod]
    public void Parse_WithContextBlock_CollectsContextRules()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");
        var extension = result.Extension!;

        Assert.IsTrue(extension.ContextRules.TryGetValue("CSS", out var cssEntries));
        var cssSelector = cssEntries.Single(e => e.Name == "CSS_SELECTOR");
        Assert.AreEqual("CSS", cssSelector.Context);
        Assert.AreEqual("[.#]?[a-zA-Z][a-zA-Z0-9_-]*", cssSelector.Pattern);
        CollectionAssert.Contains(extension.Removals.ToList(), "CSS_COMMENT");
    }

    [TestMethod]
    public void Parse_WithValidateBlock_CollectsValidationRules()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");
        var extension = result.Extension!;

        var rule = extension.ValidationRules.Single();
        Assert.AreEqual("css_id_selector_validation", rule.Name);
        Assert.AreEqual("CSS_ID_SELECTOR", rule.From);
        Assert.AreEqual("HTML_ID_ATTR", rule.To);
    }

    [TestMethod]
    public void Parse_WithRuleProduction_MarksEntryAsRule()
    {
        var content = @"
Grammar: CEBNF

<program> ::= <declaration>*
<declaration> ::= <IDENTIFIER> <SEMICOLON>
";
        var result = GrammarExtensionLoader.Parse(content);
        Assert.IsTrue(result.Success);

        var rule = result.Extension!.Entries.Single(e => e.Name == "program");
        Assert.IsTrue(rule.IsRule);
        Assert.AreEqual("<declaration>*", rule.Pattern);
    }

    [TestMethod]
    public void Parse_WithIncompleteRule_ReportsErrorWithLineInfo()
    {
        var content = "Grammar: CEBNF\n\n<Regex> ::=\n";
        var result = GrammarExtensionLoader.Parse(content);

        Assert.IsFalse(result.Success);
        var error = result.Errors.First(e => e.Message.Contains("empty pattern", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(3, error.Line);
        Assert.IsTrue(error.Column >= 1);
    }

    [TestMethod]
    public void Parse_WithUnknownGrammarType_ReportsErrorWithLineInfo()
    {
        var content = "Grammar: XMLNF\n";
        var result = GrammarExtensionLoader.Parse(content);

        Assert.IsFalse(result.Success);
        var error = result.Errors.Single(e => e.Message.Contains("Unknown grammar type", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, error.Line);
        Assert.AreEqual(9, error.Column);
    }

    [TestMethod]
    public void Parse_WithBom_StripsBomBeforeParsingHeader()
    {
        var content = "\uFEFFGrammar: BNF\nIDENTIFIER = [a-zA-Z_][a-zA-Z0-9_]*\n";
        var result = GrammarExtensionLoader.Parse(content);

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors.Select(e => e.ToString())));
        Assert.AreEqual(ExtensionGrammarType.BNF, result.Extension!.GrammarType);
    }

    [TestMethod]
    public void Parse_WithMultilineComments_SkipsCommentedEntries()
    {
        var content = @"
Grammar: CEBNF
/* comment start
   comment continuation */
TOKEN = [a-z]+
/* TOKEN_COMMENTED = should-not-parse */
";
        var result = GrammarExtensionLoader.Parse(content);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Extension!.Entries.Count);
        Assert.AreEqual("TOKEN", result.Extension.Entries[0].Name);
    }

    [TestMethod]
    public void Parse_WithUnclosedContextBlock_ReportsError()
    {
        var content = "Grammar: CEBNF\n@CONTEXT[CSS] {\nCSS_A = a+\n";
        var result = GrammarExtensionLoader.Parse(content);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Unclosed", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void RenderOverlay_ExcludesRemovalsAndIncludesAdditions()
    {
        var result = GrammarExtensionLoader.Parse(SampleExtension, "HTMLEmbedded");
        var overlay = GrammarExtensionOverlayRenderer.RenderOverlay(result.Extension!);

        StringAssert.Contains(overlay, "<HTML_TAG_OPEN> ::= /<[a-zA-Z][a-zA-Z0-9]*/");
        StringAssert.Contains(overlay, "(css-context)");
        StringAssert.Contains(overlay, "Grammar: HTMLEmbeddedOverlay");

        Assert.IsFalse(overlay.Contains("HTML_COMMENT", StringComparison.Ordinal));
        Assert.IsFalse(overlay.Contains("CSS_COMMENT", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LoadFromFileAsync_WithRepoExtensionFiles_ParsesAllSuccessfully()
    {
        // Uses the repository's own extension files to guard against regressions
        // in the extension corpus (issue #88, item 7).
        var repoRoot = FindRepoRoot();
        var extensionsDirectory = Path.Join(repoRoot, "extensions");

        if (!Directory.Exists(extensionsDirectory))
        {
            Assert.Inconclusive($"extensions directory not found under {repoRoot}");
        }

        foreach (var extensionPath in Directory.EnumerateFiles(extensionsDirectory, "*.extension"))
        {
            var result = await GrammarExtensionLoader.LoadFromFileAsync(extensionPath);

            Assert.IsTrue(result.Success,
                $"{Path.GetFileName(extensionPath)}: {string.Join("; ", result.Errors.Select(e => e.ToString()))}");
            Assert.IsFalse(string.IsNullOrEmpty(result.Extension!.Name));
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Join(directory.FullName, "extensions")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? AppContext.BaseDirectory;
    }
}
