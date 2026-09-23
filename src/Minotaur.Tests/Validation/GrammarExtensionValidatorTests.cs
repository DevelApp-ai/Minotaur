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
using Minotaur.Validation;

namespace Minotaur.Tests.Validation;

[TestClass]
public class GrammarExtensionValidatorTests
{
    private readonly GrammarExtensionValidator _validator = new();

    [TestMethod]
    public void Validate_WithDuplicateAddition_ReportsError()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            Entries =
            {
                new ExtensionEntry { Name = "TOKEN", Pattern = "a+", Action = ExtensionAction.Add, SourceLine = 3 },
                new ExtensionEntry { Name = "TOKEN", Pattern = "b+", Action = ExtensionAction.Add, SourceLine = 4 }
            }
        };

        var results = _validator.Validate(extension);

        var duplicate = results.Single(r => r.Code == "MNT-EXT-001");
        Assert.AreEqual(ValidationSeverity.Error, duplicate.Severity);
        Assert.AreEqual(3, duplicate.Location!.Line);
        StringAssert.Contains(duplicate.Message, "override intent");
    }

    [TestMethod]
    public void Validate_WithDuplicateOverride_DoesNotReportDuplicate()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            Entries =
            {
                new ExtensionEntry { Name = "TOKEN", Pattern = "a+", Action = ExtensionAction.Override, SourceLine = 3 },
                new ExtensionEntry { Name = "TOKEN", Pattern = "b+", Action = ExtensionAction.Add, SourceLine = 4 }
            }
        };

        var results = _validator.Validate(extension);
        Assert.IsFalse(results.Any(r => r.Code == "MNT-EXT-001"));
    }

    [TestMethod]
    public void Validate_WithRemovalOfUnknownRule_ReportsError()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            Entries =
            {
                new ExtensionEntry { Name = "GHOST", Action = ExtensionAction.Remove, SourceLine = 5 }
            }
        };
        var baseGrammar = new BaseGrammarInfo("Base", new[] { "REAL_TOKEN" });

        var results = _validator.Validate(extension, baseGrammar);

        var error = results.Single(r => r.Code == "MNT-EXT-002");
        Assert.AreEqual(ValidationSeverity.Error, error.Severity);
        Assert.AreEqual(5, error.Location!.Line);
        StringAssert.Contains(error.Message, "GHOST");
    }

    [TestMethod]
    public void Validate_WithOverrideOfKnownRule_DoesNotReportError()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            Entries =
            {
                new ExtensionEntry { Name = "REAL_TOKEN", Pattern = "b+", Action = ExtensionAction.Override, SourceLine = 3 }
            }
        };
        var baseGrammar = new BaseGrammarInfo("Base", new[] { "REAL_TOKEN" });

        var results = _validator.Validate(extension, baseGrammar);
        Assert.IsFalse(results.Any(r => r.Code == "MNT-EXT-002"));
    }

    [TestMethod]
    public void Validate_WithUndeclaredContextLanguage_ReportsWarning()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            EmbeddedLanguages = { "CSS" },
            ContextRules =
            {
                ["CSS"] = new List<ExtensionEntry>
                {
                    new() { Name = "CSS_A", Pattern = "a+", SourceLine = 7 }
                },
                ["JavaScript"] = new List<ExtensionEntry>
                {
                    new() { Name = "JS_A", Pattern = "b+", SourceLine = 9 }
                }
            }
        };

        var results = _validator.Validate(extension);

        var warning = results.Single(r => r.Code == "MNT-EXT-003");
        Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
        StringAssert.Contains(warning.Message, "JavaScript");
    }

    [TestMethod]
    public void Validate_WithValidationRuleReferencingUnknownEntry_ReportsWarning()
    {
        var extension = new GrammarExtension
        {
            Name = "Test",
            Entries = { new ExtensionEntry { Name = "A", Pattern = "a+", SourceLine = 3 } },
            ValidationRules =
            {
                new ExtensionValidationRule { Name = "check", From = "A", To = "MISSING", SourceLine = 5 }
            }
        };

        var results = _validator.Validate(extension);

        var warning = results.Single(r => r.Code == "MNT-EXT-004");
        StringAssert.Contains(warning.Message, "MISSING");
    }

    [TestMethod]
    public void ValidateAll_WithConflictingExtensions_ReportsWarning()
    {
        var a = new GrammarExtension
        {
            Name = "A",
            Entries = { new ExtensionEntry { Name = "SHARED", Pattern = "a+", SourceLine = 2 } }
        };
        var b = new GrammarExtension
        {
            Name = "B",
            Entries = { new ExtensionEntry { Name = "SHARED", Pattern = "b+", SourceLine = 2 } }
        };

        var results = _validator.ValidateAll(new[] { a, b });

        var conflict = results.Single(r => r.Code == "MNT-EXT-005");
        Assert.AreEqual(ValidationSeverity.Warning, conflict.Severity);
        StringAssert.Contains(conflict.Message, "SHARED");
    }

    [TestMethod]
    public void Validate_WithCleanExtension_ReturnsNoResults()
    {
        var extension = new GrammarExtension
        {
            Name = "Clean",
            EmbeddedLanguages = { "CSS" },
            Entries =
            {
                new ExtensionEntry { Name = "TOKEN_A", Pattern = "a+", SourceLine = 4 },
                new ExtensionEntry { Name = "TOKEN_B", Pattern = "b+", Action = ExtensionAction.Override, SourceLine = 5 }
            },
            ContextRules =
            {
                ["CSS"] = new List<ExtensionEntry>
                {
                    new() { Name = "CSS_X", Pattern = "x+", SourceLine = 8 }
                }
            }
        };
        var baseGrammar = new BaseGrammarInfo("Base", new[] { "TOKEN_B" });

        var results = _validator.Validate(extension, baseGrammar);
        Assert.AreEqual(0, results.Count, string.Join("; ", results.Select(r => r.Message)));
    }
}
