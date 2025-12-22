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
public class GrammarGeneratorTests
{
    private GrammarGenerator? _generator;

    [TestInitialize]
    public void Setup()
    {
        _generator = new GrammarGenerator();
    }

    [TestMethod]
    public void GrammarGenerator_Constructor_InitializesSuccessfully()
    {
        // Arrange & Act
        var generator = new GrammarGenerator();

        // Assert
        Assert.IsNotNull(generator);
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_CreatesValidOutput()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            Version = "1.0",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern
                {
                    Name = "IDENTIFIER",
                    Pattern = "[a-zA-Z_][a-zA-Z0-9_]*",
                    Type = TokenType.Identifier,
                    Priority = 1
                },
                new TokenPattern
                {
                    Name = "NUMBER",
                    Pattern = "[0-9]+",
                    Type = TokenType.Literal,
                    Priority = 2
                }
            }),
            ProductionRules = new ProductionRules
            {
                Rules = new List<ProductionRule>
                {
                    new ProductionRule
                    {
                        Name = "expression",
                        Alternatives = new List<string> { "term", "term + expression" },
                        Priority = 1
                    }
                }
            }
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("Grammar: TestGrammar"));
        Assert.IsTrue(result.Contains("TestLang Grammar Definition"));
        Assert.IsTrue(result.Contains("<expression>"));
        Assert.IsTrue(result.Contains("IDENTIFIER"));
        Assert.IsTrue(result.Contains("NUMBER"));
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_IncludesMetadata()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Author"] = "Test Author",
                ["Description"] = "Test Description"
            }
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsTrue(result.Contains("Author: Test Author"));
        Assert.IsTrue(result.Contains("Description: Test Description"));
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_HandlesKeywords()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern
                {
                    Name = "IF",
                    Pattern = "if",
                    Type = TokenType.Keyword,
                    IsKeyword = true,
                    Priority = 10
                },
                new TokenPattern
                {
                    Name = "ELSE",
                    Pattern = "else",
                    Type = TokenType.Keyword,
                    IsKeyword = true,
                    Priority = 10
                }
            })
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsTrue(result.Contains("Keywords:"));
        Assert.IsTrue(result.Contains("if"));
        Assert.IsTrue(result.Contains("else"));
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_HandlesWhitespaceTokens()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern
                {
                    Name = "WHITESPACE",
                    Pattern = "\\s+",
                    Type = TokenType.Whitespace,
                    Priority = 0
                }
            })
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsTrue(result.Contains("<WHITESPACE>"));
        Assert.IsTrue(result.Contains("skip"));
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_HandlesEmptyGrammar()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "EmptyGrammar",
            Language = "Empty"
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("Grammar: EmptyGrammar"));
    }

    [TestMethod]
    public void GrammarGenerator_GenerateGrammarFile_IncludesExamples()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "TestGrammar",
            Language = "TestLang",
            TokenRules = new TokenDefinitions(new[]
            {
                new TokenPattern
                {
                    Name = "NUMBER",
                    Pattern = "[0-9]+",
                    Type = TokenType.Literal,
                    Examples = new List<string> { "123", "456", "789" }
                }
            }),
            ProductionRules = new ProductionRules
            {
                Rules = new List<ProductionRule>
                {
                    new ProductionRule
                    {
                        Name = "expression",
                        Alternatives = new List<string> { "NUMBER" },
                        Examples = new List<string> { "123", "456" }
                    }
                }
            }
        };

        // Act
        var result = _generator!.GenerateGrammarFile(grammar);

        // Assert
        Assert.IsTrue(result.Contains("// Examples:"));
    }
}
