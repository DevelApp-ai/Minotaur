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
using Minotaur.GrammarGeneration.Models;
using Minotaur.GrammarGeneration;

namespace Minotaur.Tests.GrammarGeneration;

[TestClass]
public class GrammarModelsTests
{
    [TestMethod]
    public void TokenPattern_Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var pattern = new TokenPattern();

        // Assert
        Assert.IsNotNull(pattern);
        Assert.AreEqual(string.Empty, pattern.Name);
        Assert.AreEqual(string.Empty, pattern.Pattern);
        Assert.IsFalse(pattern.IsKeyword);
        Assert.IsNotNull(pattern.Examples);
        Assert.AreEqual(0, pattern.Examples.Count);
    }

    [TestMethod]
    public void TokenPattern_Properties_CanBeSet()
    {
        // Arrange & Act
        var pattern = new TokenPattern
        {
            Name = "IDENTIFIER",
            Pattern = "[a-zA-Z]+",
            Type = TokenType.Identifier,
            Priority = 5,
            IsKeyword = false,
            Confidence = 0.95
        };

        // Assert
        Assert.AreEqual("IDENTIFIER", pattern.Name);
        Assert.AreEqual("[a-zA-Z]+", pattern.Pattern);
        Assert.AreEqual(TokenType.Identifier, pattern.Type);
        Assert.AreEqual(5, pattern.Priority);
        Assert.IsFalse(pattern.IsKeyword);
        Assert.AreEqual(0.95, pattern.Confidence);
    }

    [TestMethod]
    public void TokenPattern_Examples_CanBeAdded()
    {
        // Arrange
        var pattern = new TokenPattern
        {
            Name = "NUMBER"
        };

        // Act
        pattern.Examples.Add("123");
        pattern.Examples.Add("456");

        // Assert
        Assert.AreEqual(2, pattern.Examples.Count);
        Assert.IsTrue(pattern.Examples.Contains("123"));
        Assert.IsTrue(pattern.Examples.Contains("456"));
    }

    [TestMethod]
    public void TokenDefinitions_Constructor_InitializesEmpty()
    {
        // Arrange & Act
        var definitions = new TokenDefinitions();

        // Assert
        Assert.IsNotNull(definitions);
        Assert.IsNotNull(definitions.Patterns);
        Assert.AreEqual(0, definitions.Patterns.Count);
    }

    [TestMethod]
    public void TokenDefinitions_ConstructorWithPatterns_InitializesWithData()
    {
        // Arrange
        var patterns = new[]
        {
            new TokenPattern { Name = "PATTERN1", Type = TokenType.Identifier },
            new TokenPattern { Name = "PATTERN2", Type = TokenType.Keyword }
        };

        // Act
        var definitions = new TokenDefinitions(patterns);

        // Assert
        Assert.AreEqual(2, definitions.Patterns.Count);
    }

    [TestMethod]
    public void TokenDefinitions_AddPattern_AddsPatternSuccessfully()
    {
        // Arrange
        var definitions = new TokenDefinitions();
        var pattern = new TokenPattern { Name = "TEST", Type = TokenType.Identifier };

        // Act
        definitions.AddPattern(pattern);

        // Assert
        Assert.AreEqual(1, definitions.Patterns.Count);
        Assert.AreEqual("TEST", definitions.Patterns[0].Name);
    }

    [TestMethod]
    public void TokenDefinitions_GetPatternsByType_FiltersCorrectly()
    {
        // Arrange
        var definitions = new TokenDefinitions(new[]
        {
            new TokenPattern { Name = "ID", Type = TokenType.Identifier },
            new TokenPattern { Name = "IF", Type = TokenType.Keyword },
            new TokenPattern { Name = "ELSE", Type = TokenType.Keyword },
            new TokenPattern { Name = "NUM", Type = TokenType.Literal }
        });

        // Act
        var keywords = definitions.GetPatternsByType(TokenType.Keyword).ToList();
        var identifiers = definitions.GetPatternsByType(TokenType.Identifier).ToList();

        // Assert
        Assert.AreEqual(2, keywords.Count);
        Assert.AreEqual(1, identifiers.Count);
        Assert.IsTrue(keywords.Any(k => k.Name == "IF"));
        Assert.IsTrue(keywords.Any(k => k.Name == "ELSE"));
    }

    [TestMethod]
    public void ProductionRule_Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var rule = new ProductionRule();

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(string.Empty, rule.Name);
        Assert.IsNotNull(rule.Alternatives);
        Assert.AreEqual(0, rule.Alternatives.Count);
        Assert.IsFalse(rule.IsOptional);
        Assert.IsFalse(rule.IsRepeatable);
    }

    [TestMethod]
    public void ProductionRule_Properties_CanBeSet()
    {
        // Arrange & Act
        var rule = new ProductionRule
        {
            Name = "expression",
            Priority = 10,
            IsOptional = true,
            IsRepeatable = false,
            Confidence = 0.85
        };

        // Assert
        Assert.AreEqual("expression", rule.Name);
        Assert.AreEqual(10, rule.Priority);
        Assert.IsTrue(rule.IsOptional);
        Assert.IsFalse(rule.IsRepeatable);
        Assert.AreEqual(0.85, rule.Confidence);
    }

    [TestMethod]
    public void ProductionRule_Alternatives_CanBeAdded()
    {
        // Arrange
        var rule = new ProductionRule { Name = "statement" };

        // Act
        rule.Alternatives.Add("expression ;");
        rule.Alternatives.Add("declaration ;");
        rule.Alternatives.Add("return expression ;");

        // Assert
        Assert.AreEqual(3, rule.Alternatives.Count);
        Assert.IsTrue(rule.Alternatives.Contains("expression ;"));
    }

    [TestMethod]
    public void ProductionRules_Constructor_InitializesEmpty()
    {
        // Arrange & Act
        var rules = new ProductionRules();

        // Assert
        Assert.IsNotNull(rules);
        Assert.IsNotNull(rules.Rules);
        Assert.AreEqual(0, rules.Rules.Count);
    }

    [TestMethod]
    public void Grammar_Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var grammar = new Grammar();

        // Assert
        Assert.IsNotNull(grammar);
        Assert.AreEqual(string.Empty, grammar.Name);
        Assert.AreEqual(string.Empty, grammar.Language);
        Assert.IsNotNull(grammar.TokenRules);
        Assert.IsNotNull(grammar.ProductionRules);
        Assert.IsNotNull(grammar.Metadata);
    }

    [TestMethod]
    public void Grammar_Properties_CanBeSet()
    {
        // Arrange & Act
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            Version = "1.0"
        };

        // Assert
        Assert.AreEqual("TestGrammar", grammar.Name);
        Assert.AreEqual("TestLang", grammar.Language);
        Assert.AreEqual("1.0", grammar.Version);
    }

    [TestMethod]
    public void Grammar_Metadata_CanBePopulated()
    {
        // Arrange
        var grammar = new Grammar();

        // Act
        grammar.Metadata["Author"] = "Test Author";
        grammar.Metadata["License"] = "AGPL";
        grammar.Metadata["Date"] = "2024-01-01";

        // Assert
        Assert.AreEqual(3, grammar.Metadata.Count);
        Assert.AreEqual("Test Author", grammar.Metadata["Author"]);
        Assert.AreEqual("AGPL", grammar.Metadata["License"]);
    }

    [TestMethod]
    public void LanguageContext_Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var context = new LanguageContext();

        // Assert
        Assert.IsNotNull(context);
        Assert.IsNotNull(context.FileExtensions);
        Assert.AreEqual(0, context.FileExtensions.Count);
        Assert.IsFalse(context.HasNestedScopes);
        Assert.IsFalse(context.HasTypeSystem);
    }

    [TestMethod]
    public void LanguageContext_Properties_CanBeSet()
    {
        // Arrange & Act
        var context = new LanguageContext
        {
            HasNestedScopes = true,
            HasTypeSystem = true,
            HasMacroSystem = false,
            HasComments = true,
            HasStringLiterals = true,
            HasNumericLiterals = true,
            FileExtensions = new List<string> { ".test", ".tst" },
            DefaultEncoding = "UTF-8"
        };

        // Assert
        Assert.IsTrue(context.HasNestedScopes);
        Assert.IsTrue(context.HasTypeSystem);
        Assert.IsFalse(context.HasMacroSystem);
        Assert.IsTrue(context.HasComments);
        Assert.IsTrue(context.HasStringLiterals);
        Assert.IsTrue(context.HasNumericLiterals);
        Assert.AreEqual(2, context.FileExtensions.Count);
        Assert.AreEqual("UTF-8", context.DefaultEncoding);
    }

    [TestMethod]
    public void GrammarGenerationProgress_Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var progress = new GrammarGenerationProgress();

        // Assert
        Assert.IsNotNull(progress);
        Assert.AreEqual(string.Empty, progress.Stage);
        Assert.AreEqual(0, progress.Progress);
    }

    [TestMethod]
    public void GrammarGenerationProgress_Properties_CanBeSet()
    {
        // Arrange & Act
        var progress = new GrammarGenerationProgress
        {
            Stage = "Analyzing tokens",
            Progress = 50,
            Details = "Processing file 5 of 10"
        };

        // Assert
        Assert.AreEqual("Analyzing tokens", progress.Stage);
        Assert.AreEqual(50, progress.Progress);
        Assert.AreEqual("Processing file 5 of 10", progress.Details);
    }
}
