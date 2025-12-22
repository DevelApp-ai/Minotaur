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
using Minotaur.GrammarGeneration;
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.Tests.GrammarGeneration;

[TestClass]
public class GrammarIntegrationTests
{
    [TestMethod]
    public void Grammar_CompleteWorkflow_CreatesValidGrammar()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "CompleteTest",
            Language = "TestLang",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern
                {
                    Name = "KEYWORD",
                    Pattern = "if|else|while",
                    Type = TokenType.Keyword,
                    Priority = 10
                },
                new TokenPattern
                {
                    Name = "ID",
                    Pattern = "[a-zA-Z]+",
                    Type = TokenType.Identifier,
                    Priority = 5
                }
            }),
            ProductionRules = new ProductionRules
            {
                Rules = new List<ProductionRule>
                {
                    new ProductionRule
                    {
                        Name = "statement",
                        Alternatives = new List<string> { "if_stmt", "while_stmt" },
                        Priority = 1
                    }
                }
            }
        };

        var generator = new GrammarGenerator();

        // Act
        var grammarFile = generator.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(grammarFile);
        StringAssert.Contains(grammarFile, "CompleteTest");
        // Grammar generator includes tokens and rules
        Assert.IsTrue(grammarFile.Length > 50);
    }

    [TestMethod]
    public void Grammar_WithComplexProductionRules_GeneratesCorrectly()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "ComplexGrammar",
            Language = "TestLang",
            ProductionRules = new ProductionRules
            {
                Rules = new List<ProductionRule>
                {
                    new ProductionRule
                    {
                        Name = "expression",
                        Alternatives = new List<string>
                        {
                            "term",
                            "expression + term",
                            "expression - term"
                        },
                        Priority = 10
                    },
                    new ProductionRule
                    {
                        Name = "term",
                        Alternatives = new List<string>
                        {
                            "factor",
                            "term * factor",
                            "term / factor"
                        },
                        Priority = 5
                    }
                }
            }
        };

        var generator = new GrammarGenerator();

        // Act
        var result = generator.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "expression");
        StringAssert.Contains(result, "term");
        StringAssert.Contains(result, "+");
        StringAssert.Contains(result, "*");
    }

    [TestMethod]
    public void Grammar_WithAllTokenTypes_GeneratesCompletely()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "AllTypes",
            Language = "Complete",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern { Name = "KW", Type = TokenType.Keyword, Pattern = "keyword" },
                new TokenPattern { Name = "ID", Type = TokenType.Identifier, Pattern = "[a-z]+" },
                new TokenPattern { Name = "LIT", Type = TokenType.Literal, Pattern = "\\d+" },
                new TokenPattern { Name = "OP", Type = TokenType.Operator, Pattern = "[+\\-*/]" },
                new TokenPattern { Name = "DELIM", Type = TokenType.Delimiter, Pattern = "[;,]" },
                new TokenPattern { Name = "WS", Type = TokenType.Whitespace, Pattern = "\\s+" },
                new TokenPattern { Name = "COMMENT", Type = TokenType.Comment, Pattern = "//.*" }
            })
        };

        var generator = new GrammarGenerator();

        // Act
        var result = generator.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "AllTypes");
        // Grammar generator includes various token types
        Assert.IsTrue(result.Length > 100);
    }

    [TestMethod]
    public void TokenDefinitions_MultiplePatternTypes_OrganizedCorrectly()
    {
        // Arrange
        var patterns = new[]
        {
            new TokenPattern { Name = "A", Type = TokenType.Keyword },
            new TokenPattern { Name = "B", Type = TokenType.Identifier },
            new TokenPattern { Name = "C", Type = TokenType.Keyword },
            new TokenPattern { Name = "D", Type = TokenType.Literal }
        };

        var definitions = new TokenDefinitions(patterns);

        // Act
        var keywords = definitions.GetPatternsByType(TokenType.Keyword).ToList();
        var identifiers = definitions.GetPatternsByType(TokenType.Identifier).ToList();
        var literals = definitions.GetPatternsByType(TokenType.Literal).ToList();

        // Assert
        Assert.AreEqual(2, keywords.Count);
        Assert.AreEqual(1, identifiers.Count);
        Assert.AreEqual(1, literals.Count);
    }

    [TestMethod]
    public void ProductionRules_AddMultipleRules_MaintainsOrder()
    {
        // Arrange
        var rules = new ProductionRules();

        // Act
        rules.AddRule(new ProductionRule { Name = "first", Priority = 1 });
        rules.AddRule(new ProductionRule { Name = "second", Priority = 2 });
        rules.AddRule(new ProductionRule { Name = "third", Priority = 3 });

        // Assert
        Assert.AreEqual(3, rules.Rules.Count);
        Assert.AreEqual("first", rules.Rules[0].Name);
        Assert.AreEqual("second", rules.Rules[1].Name);
        Assert.AreEqual("third", rules.Rules[2].Name);
    }

    [TestMethod]
    public void ProductionRules_GetRuleByName_FindsCorrectRule()
    {
        // Arrange
        var rules = new ProductionRules();
        rules.AddRule(new ProductionRule { Name = "ruleA" });
        rules.AddRule(new ProductionRule { Name = "ruleB" });
        rules.AddRule(new ProductionRule { Name = "ruleC" });

        // Act
        var found = rules.GetRule("ruleB");

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual("ruleB", found.Name);
    }

    [TestMethod]
    public void ProductionRules_GetNonExistentRule_ReturnsNull()
    {
        // Arrange
        var rules = new ProductionRules();
        rules.AddRule(new ProductionRule { Name = "exists" });

        // Act
        var notFound = rules.GetRule("doesNotExist");

        // Assert
        Assert.IsNull(notFound);
    }

    [TestMethod]
    public void Grammar_WithVersion_IncludesInOutput()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "VersionedGrammar",
            Language = "Test",
            Version = "2.5.1"
        };

        var generator = new GrammarGenerator();

        // Act
        var result = generator.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "2.5.1");
    }

    [TestMethod]
    public void TokenPattern_WithExamples_StoresMultipleExamples()
    {
        // Arrange
        var pattern = new TokenPattern
        {
            Name = "NUMBER",
            Type = TokenType.Literal
        };

        // Act
        pattern.Examples.Add("123");
        pattern.Examples.Add("456");
        pattern.Examples.Add("789");

        // Assert
        Assert.AreEqual(3, pattern.Examples.Count);
        CollectionAssert.Contains(pattern.Examples, "123");
        CollectionAssert.Contains(pattern.Examples, "456");
        CollectionAssert.Contains(pattern.Examples, "789");
    }

    [TestMethod]
    public void Grammar_LargeNumberOfRules_HandlesCorrectly()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "LargeGrammar",
            Language = "Test",
            ProductionRules = new ProductionRules()
        };

        // Add 50 production rules
        for (int i = 0; i < 50; i++)
        {
            grammar.ProductionRules.AddRule(new ProductionRule
            {
                Name = $"rule{i}",
                Alternatives = new List<string> { $"alt{i}a", $"alt{i}b" }
            });
        }

        var generator = new GrammarGenerator();

        // Act
        var result = generator.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "rule0");
        StringAssert.Contains(result, "rule49");
    }
}
