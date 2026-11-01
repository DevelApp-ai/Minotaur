/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Core;
using Minotaur.Parser;
using Minotaur.Parser.Structure;

namespace Minotaur.Tests.Parser;

/// <summary>
/// Tests for the structure-preserving C# parser backend (Minotaur issue #136):
/// complete token stream with punctuation, balanced structure graph, explicit
/// failures instead of truncation, deterministic output, and graph versioning.
/// </summary>
[TestClass]
public class StructurePreservingParserTests
{
    private const string SampleClass = @"
using System;
using System.Threading.Tasks;

namespace Demo
{
    [Serializable]
    public class Sample
    {
        private readonly string _path = ""C:\temp"";

        public async Task SaveAsync(string content)
        {
            await File.WriteAllTextAsync(_path, content);
        }
    }
}
";

    [TestMethod]
    public void Parse_IncludesPunctuationTokens_InSourceOrder()
    {
        var parser = new StructurePreservingParser(includePunctuation: true);
        var result = parser.Parse(SampleClass);

        var punctuation = result.Tokens.Where(t => t.Kind == StructureTokenKind.Punctuation).ToList();
        Assert.IsTrue(punctuation.Count > 0, "Punctuation tokens must be preserved in the stream.");

        // Order is preserved: the first identifier is 'using', and the first
        // punctuation token must appear after it (the ';').
        var firstIdentifier = result.Tokens.First(t => t.Kind == StructureTokenKind.Identifier);
        Assert.AreEqual("using", firstIdentifier.Text);
        Assert.IsTrue(punctuation[0].StartPosition > firstIdentifier.StartPosition);

        // Punctuation present: braces, parens, semicolon, comma, dot.
        var texts = punctuation.Select(t => t.Text).ToHashSet();
        Assert.IsTrue(texts.Contains("{"));
        Assert.IsTrue(texts.Contains("}"));
        Assert.IsTrue(texts.Contains("("));
        Assert.IsTrue(texts.Contains(")"));
        Assert.IsTrue(texts.Contains(";"));
        Assert.IsTrue(texts.Contains(","));
        Assert.IsTrue(texts.Contains("."));
    }

    [TestMethod]
    public void Parse_BuildsBalancedStructure_WithInvocationGrouping()
    {
        var parser = new StructurePreservingParser();
        var result = parser.Parse(SampleClass);

        Assert.IsNotNull(result.Root);
        Assert.AreEqual("compilation_unit", result.Root.RuleName);

        // All grouping delimiters are balanced and represented as non-terminals.
        var groups = Flatten(result.Root).OfType<NonTerminalNode>().ToList();
        var blockCount = groups.Count(g => g.RuleName == "block");
        var parenCount = groups.Count(g => g.RuleName == "paren_group");
        Assert.IsTrue(blockCount >= 2, $"Expected namespace/class/method blocks, got {blockCount}.");
        Assert.IsTrue(parenCount >= 1, "Expected at least one parenthesized group (method args).");

        // The WriteAllTextAsync invocation: identifier 'WriteAllTextAsync'
        // followed by a paren_group containing the two arguments and the
        // separating comma.
        var tokens = result.Tokens;
        var invocationIndex = FindIndex(tokens, t => t.Kind == StructureTokenKind.Identifier && t.Text == "WriteAllTextAsync");
        Assert.IsTrue(invocationIndex >= 0, "Invocation identifier not found in token stream.");

        // The invocation's argument list is the first paren_group opened
        // immediately after the invocation identifier: a parent-child edge.
        var invocationEnd = tokens[invocationIndex].EndPosition;
        var openParen = tokens.First(t => t.Kind == StructureTokenKind.Punctuation && t.Text == "(" && t.StartPosition >= invocationEnd);
        var parenGroup = FindGroupByOpenPosition(result.Root, openParen.StartPosition);
        Assert.IsNotNull(parenGroup, "Invocation must be grouped by a paren_group parent edge.");
        Assert.AreEqual("paren_group", parenGroup!.RuleName);

        // Inside the paren group: _path, comma, content.
        var inner = ChildrenTexts(parenGroup);
        CollectionAssert.Contains(inner, "_path");
        CollectionAssert.Contains(inner, ",");
        CollectionAssert.Contains(inner, "content");
    }

    [TestMethod]
    public void Parse_PreservesAttributes_AsTokensBeforeTheClass()
    {
        var parser = new StructurePreservingParser();
        var result = parser.Parse(SampleClass);

        var attributeIndex = FindIndex(result.Tokens, t => t.Text == "Serializable");
        var classIndex = FindIndex(result.Tokens, t => t.Text == "class");
        Assert.IsTrue(attributeIndex >= 0, "Attribute name must be tokenized.");
        Assert.IsTrue(classIndex > attributeIndex, "Attribute must precede the class keyword in source order.");

        // The attribute brackets form a bracket_group.
        var groups = Flatten(result.Root).OfType<NonTerminalNode>().ToList();
        Assert.IsTrue(groups.Any(g => g.RuleName == "bracket_group"), "Attributes must form a bracket_group.");
    }

    [TestMethod]
    public void Parse_UnterminatedString_ThrowsExplicitParseException()
    {
        var parser = new StructurePreservingParser();
        var source = "class C { string s = \"never closed; }";

        var ex = Assert.ThrowsException<StructureParseException>(() => parser.Parse(source));
        Assert.IsTrue(ex.Message.Contains("string") || ex.Message.Contains("literal"),
            $"Error message should describe the problem: {ex.Message}");
        Assert.IsTrue(ex.Line > 0);
        Assert.IsTrue(ex.Column > 0);
    }

    [TestMethod]
    public void Parse_UnterminatedBlockComment_ThrowsExplicitParseException()
    {
        var parser = new StructurePreservingParser();
        Assert.ThrowsException<StructureParseException>(() => parser.Parse("class C { /* never closed"));
    }

    [TestMethod]
    public void Parse_MismatchedBrace_ThrowsExplicitParseException()
    {
        var parser = new StructurePreservingParser();
        // A closing brace with no matching opener must fail loudly, not truncate.
        Assert.ThrowsException<StructureParseException>(() => parser.Parse("class C { void M() { } } }"));
    }

    [TestMethod]
    public void Parse_LargeFile_IsFullyTokenized_NoTruncation()
    {
        // Generate ~1 MB of realistic C# source.
        var sb = new StringBuilder();
        sb.AppendLine("namespace Generated {");
        for (var i = 0; i < 10000; i++)
        {
            sb.AppendLine($"    public class Generated{i} {{ private int _value{i} = {i}; public int Compute{i}(int x) {{ return x + {i}; }} }}");
        }

        sb.AppendLine("}");
        var source = sb.ToString();
        Assert.IsTrue(source.Length > 1_000_000, $"Test source should exceed 1MB, was {source.Length}.");

        var parser = new StructurePreservingParser();
        var result = parser.Parse(source);

        // No truncation: the final non-whitespace token must end exactly at
        // the end of the source, and the token count must scale with input.
        var last = result.Tokens.Where(t => t.Kind != StructureTokenKind.Whitespace).Last();
        Assert.AreEqual(source.TrimEnd().Length, last.EndPosition,
            "Final token must reach the end of the source: the stream is never truncated.");
        Assert.IsTrue(result.Tokens.Count > 100_000, $"Expected a large token stream, got {result.Tokens.Count}.");
        Assert.AreEqual(source.Length, result.SourceLength);
    }

    [TestMethod]
    public void Parse_IsDeterministic_IdenticalTokenStreamAndStructure()
    {
        var parser = new StructurePreservingParser();

        var first = parser.Parse(SampleClass);
        var second = parser.Parse(SampleClass);

        Assert.AreEqual(first.Tokens.Count, second.Tokens.Count);
        for (var i = 0; i < first.Tokens.Count; i++)
        {
            Assert.AreEqual(first.Tokens[i].Kind, second.Tokens[i].Kind, $"Token {i} kind differs.");
            Assert.AreEqual(first.Tokens[i].Text, second.Tokens[i].Text, $"Token {i} text differs.");
            Assert.AreEqual(first.Tokens[i].StartPosition, second.Tokens[i].StartPosition, $"Token {i} position differs.");
            Assert.AreEqual(first.Tokens[i].Line, second.Tokens[i].Line, $"Token {i} line differs.");
            Assert.AreEqual(first.Tokens[i].Column, second.Tokens[i].Column, $"Token {i} column differs.");
        }

        Assert.AreEqual(Canonicalize(first.Root), Canonicalize(second.Root),
            "Structure graphs for identical input must serialize identically.");
    }

    [TestMethod]
    public void Parse_GraphVersion_IsStableAndPinned()
    {
        var parser = new StructurePreservingParser();
        var result = parser.Parse(SampleClass);

        Assert.AreEqual("1.0.0", result.GraphVersion);
        Assert.AreEqual("1.0.0", StructureGraphVersions.Current);
    }

    [TestMethod]
    public void ParseToCognitiveGraphAsync_CSharpDefault_RoutesToStructurePreservingBackend()
    {
        using var integration = new StepParserIntegration(new ParserConfiguration());
        var graph = integration.ParseToCognitiveGraphAsync(SampleClass).GetAwaiter().GetResult();

        Assert.IsNotNull(graph);
        Assert.IsNotNull(graph.Children);
        Assert.IsTrue(graph.Children.Count > 0);

        Assert.AreEqual("Minotaur.StructurePreservingParser", graph.Metadata["parserType"]);
        Assert.AreEqual("1.0.0", graph.Metadata["graphVersion"]);
        Assert.AreEqual(true, graph.Metadata["parseSuccess"]);
        Assert.AreEqual(true, graph.Metadata["structurePreserving"]);
        Assert.IsTrue((int)graph.Metadata["tokenCount"] > 0);
    }

    [TestMethod]
    public void ParseToCognitiveGraphAsync_StructurePreservingDisabled_FallsBackToStepParser()
    {
        using var integration = new StepParserIntegration(new ParserConfiguration
        {
            StructurePreserving = false,
        });

        // Legacy path unchanged: must produce a graph without
        // structure-preserving metadata markers.
        var graph = integration.ParseToCognitiveGraphAsync(SampleClass).GetAwaiter().GetResult();
        Assert.IsNotNull(graph);
        Assert.IsFalse(graph.Metadata.ContainsKey("structurePreserving"));
        Assert.IsFalse(graph.Metadata.ContainsKey("parserType") &&
            string.Equals(graph.Metadata["parserType"], "Minotaur.StructurePreservingParser"));
    }

    [TestMethod]
    public async Task ValidateSourceAsync_UnchangedBehavior_StillValidates()
    {
        using var integration = new StepParserIntegration(new ParserConfiguration());
        var result = await integration.ValidateSourceAsync(SampleClass);
        Assert.IsNotNull(result);
        // ValidateSourceAsync keeps its existing contract: it returns a
        // result object rather than throwing on valid C#.
    }

    [DataTestMethod]
    [DataRow("class C { void M() { } }")]
    [DataRow("var x = 1 + 2 * 3;")]
    [DataRow("string s = \"hello, world\"; char c = 'x';")]
    [DataRow("/* comment */ int Hex = 0xFF; double Real = 3.14e2;")]
    [DataRow("var raw = \"\"\"raw string\"\"\"; var v = @\"verbatim\";")]
    [DataRow("var q = a ? b : c; var n = a ?? b; var co = a?.b?.c;")]
    public void Parse_CommonCSharpConstructs_TokenizesWithoutError(string source)
    {
        var parser = new StructurePreservingParser();
        var result = parser.Parse(source);
        Assert.IsTrue(result.Tokens.Any(t => t.Kind != StructureTokenKind.Whitespace), "Expected at least one token.");
    }

    private static int FindIndex(IReadOnlyList<StructureToken> tokens, Func<StructureToken, bool> predicate)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (predicate(tokens[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static IEnumerable<CognitiveGraphNode> Flatten(CognitiveGraphNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static NonTerminalNode? FindGroupByOpenPosition(CognitiveGraphNode node, int openPosition)
    {
        foreach (var child in node.Children)
        {
            if (child is NonTerminalNode nt &&
                (nt.RuleName == "paren_group" || nt.RuleName == "block" || nt.RuleName == "bracket_group") &&
                GetMetadataInt(nt, "openPosition", -1) == openPosition)
            {
                return nt;
            }

            var found = FindGroupByOpenPosition(child, openPosition);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static int GetMetadataInt(NonTerminalNode node, string key, int fallback)
    {
        if (node.Metadata.TryGetValue(key, out var value) && value is int i)
        {
            return i;
        }

        return fallback;
    }

    private static List<string> ChildrenTexts(NonTerminalNode node) =>
        node.Children.OfType<TerminalNode>().Select(t => t.Text).ToList();

    private static string Canonicalize(CognitiveGraphNode node)
    {
        var sb = new StringBuilder();
        AppendCanonical(sb, node, 0);
        return sb.ToString();
    }

    private static void AppendCanonical(StringBuilder sb, CognitiveGraphNode node, int depth)
    {
        sb.Append(new string(' ', depth));
        if (node is NonTerminalNode nt)
        {
            sb.AppendLine($"NT:{nt.RuleName}:{nt.ProductionIndex}");
        }
        else if (node is TerminalNode term)
        {
            sb.AppendLine($"T:{term.TokenType}:{term.Text}");
        }
        else
        {
            sb.AppendLine("?");
        }

        foreach (var child in node.Children)
        {
            AppendCanonical(sb, child, depth + 1);
        }
    }
}
