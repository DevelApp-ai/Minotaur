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
using Minotaur.GrammarGeneration.Analysis;
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.Tests.GrammarGeneration;

[TestClass]
public class SyntaxStructureAnalyzerTests
{
    private string _testFilePath = null!;
    private TokenDefinitions _basicTokens = null!;

    [TestInitialize]
    public void Setup()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");

        // Create basic token definitions for testing
        _basicTokens = new TokenDefinitions(new List<TokenPattern>
        {
            new TokenPattern { Name = "IF", Pattern = "if", Type = TokenType.Keyword },
            new TokenPattern { Name = "ELSE", Pattern = "else", Type = TokenType.Keyword },
            new TokenPattern { Name = "FUNCTION", Pattern = "function", Type = TokenType.Keyword },
            new TokenPattern { Name = "VAR", Pattern = "var", Type = TokenType.Keyword },
            new TokenPattern { Name = "RETURN", Pattern = "return", Type = TokenType.Keyword },
            new TokenPattern { Name = "IDENTIFIER", Pattern = @"[a-zA-Z_][a-zA-Z0-9_]*", Type = TokenType.Identifier },
            new TokenPattern { Name = "NUMBER", Pattern = @"\d+", Type = TokenType.Literal }
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_EmptyFileArray_ReturnsProductionRules()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        var files = Array.Empty<string>();

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, files);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ProductionRules));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_FunctionDefinition_IdentifiesFunctionPattern()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "function test() { return 42; }");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("function")));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_VariableDeclaration_IdentifiesDeclarationPattern()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 10;");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("declaration")));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_IfStatement_IdentifiesControlFlow()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "if (x > 0) { return true; }");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name == "if_statement"));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_ComplexCode_IdentifiesExpressionRules()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "var result = (a + b) * c;");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name == "expression"));
        Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("expression")));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_ComplexCode_IdentifiesStatementRules()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, @"
            function test() {
                var x = 1;
                return x;
            }
        ");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name == "statement" || r.Name == "block"));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_HasProgramRule_AsTopLevel()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 1;");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name == "program"));
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_ProductionRules_HaveAlternatives()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "function test() { var x = 1; }");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        foreach (var rule in result.Rules)
        {
            Assert.IsNotNull(rule.Alternatives);
            Assert.IsTrue(rule.Alternatives.Count > 0);
        }
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_ProductionRules_HaveConfidence()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 1;");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        foreach (var rule in result.Rules)
        {
            Assert.IsTrue(rule.Confidence >= 0.0 && rule.Confidence <= 1.0);
        }
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_ProductionRules_HavePriority()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 1;");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        foreach (var rule in result.Rules)
        {
            Assert.IsTrue(rule.Priority > 0);
        }
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_CommentsInCode_IgnoresComments()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, @"
            // This is a comment
            var x = 1;
            /* Another comment */
        ");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        // Should still produce rules despite comments
        Assert.IsTrue(result.Rules.Count > 0);
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_MultipleFiles_CombinesPatterns()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        var file1 = Path.Join(Path.GetTempPath(), $"test1_{Guid.NewGuid()}.txt");
        var file2 = Path.Join(Path.GetTempPath(), $"test2_{Guid.NewGuid()}.txt");

        try
        {
            File.WriteAllText(file1, "function test1() {}");
            File.WriteAllText(file2, "var x = 1;");

            // Act
            var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { file1, file2 });

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("function")));
            Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("declaration")));
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_NonExistentFile_ContinuesProcessing()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        var nonExistent = "nonexistent_file_12345.txt";

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { nonExistent });

        // Assert
        Assert.IsNotNull(result);
        // Should still return basic expression and statement rules
        Assert.IsTrue(result.Rules.Count > 0);
    }

    [TestMethod]
    public void DiscoverSyntaxPatterns_NestedBlocks_HandlesNesting()
    {
        // Arrange
        var analyzer = new SyntaxStructureAnalyzer();
        File.WriteAllText(_testFilePath, @"
            function test() {
                if (x) {
                    return true;
                }
            }
        ");

        // Act
        var result = analyzer.DiscoverSyntaxPatterns(_basicTokens, new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Rules.Any(r => r.Name == "block"));
        Assert.IsTrue(result.Rules.Any(r => r.Name.Contains("statement")));
    }
}
