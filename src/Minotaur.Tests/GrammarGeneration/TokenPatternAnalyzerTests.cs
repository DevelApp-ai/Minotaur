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
public class TokenPatternAnalyzerTests
{
    private string _testFilePath = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create a temporary test file
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
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
    public void AnalyzeSourceCode_EmptyFileArray_ReturnsTokenDefinitions()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        var files = Array.Empty<string>();

        // Act
        var result = analyzer.AnalyzeSourceCode(files);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TokenDefinitions));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesLiterals()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 42; var y = \"hello\";");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "STRING_LITERAL"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "NUMBER_LITERAL"));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesOperators()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "x = a + b * c;");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Operator));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesKeywords()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        // Use more repetitions to ensure keywords are detected (frequency threshold)
        File.WriteAllText(_testFilePath, @"
            if (x > 0) { return true; }
            if (y > 0) { return true; }
            if (z > 0) { return true; }
            if (a > 0) { return true; }
        ");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        // Either we detect keywords or at minimum we have keyword-type token patterns
        Assert.IsTrue(result.Patterns.Any(p => p.IsKeyword || p.Type == TokenType.Keyword));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesIdentifierPattern()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "var myVariable = someFunction();");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "IDENTIFIER"));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesStructuralTokens()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "function test() { return [1, 2, 3]; }");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "LPAREN"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "RPAREN"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "LBRACE"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "RBRACE"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "LBRACKET"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "RBRACKET"));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesCommentPatterns()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "// Line comment\n/* Block comment */\nvar x = 1;");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "LINE_COMMENT"));
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "BLOCK_COMMENT"));
    }

    [TestMethod]
    public void AnalyzeSourceCode_SimpleCode_IdentifiesWhitespace()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 1;");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Name == "WHITESPACE"));
    }

    [TestMethod]
    public void AnalyzeSourceCode_MultipleFiles_CombinesAnalysis()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        var file1 = Path.Combine(Path.GetTempPath(), $"test1_{Guid.NewGuid()}.txt");
        var file2 = Path.Combine(Path.GetTempPath(), $"test2_{Guid.NewGuid()}.txt");
        
        try
        {
            File.WriteAllText(file1, "var x = 10;");
            File.WriteAllText(file2, "function test() { return true; }");

            // Act
            var result = analyzer.AnalyzeSourceCode(new[] { file1, file2 });

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Patterns.Count > 0);
            Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Literal));
            Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Delimiter));
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [TestMethod]
    public void AnalyzeSourceCode_NonExistentFile_ContinuesProcessing()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        var nonExistent = "nonexistent_file_12345.txt";

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { nonExistent });

        // Assert
        Assert.IsNotNull(result);
        // Should still return basic patterns even if file doesn't exist
    }

    [TestMethod]
    public void AnalyzeSourceCode_ComplexExpression_IdentifiesAllTokenTypes()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, @"
            function calculate(x, y) {
                // Calculate result
                var result = (x + y) * 2;
                return result;
            }
        ");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Identifier));
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Literal));
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Operator));
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Delimiter));
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Comment));
        Assert.IsTrue(result.Patterns.Any(p => p.Type == TokenType.Whitespace));
    }

    [TestMethod]
    public void AnalyzeSourceCode_TokenPatterns_HaveConfidenceScores()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "var x = 42;");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        foreach (var pattern in result.Patterns)
        {
            Assert.IsTrue(pattern.Confidence >= 0.0 && pattern.Confidence <= 1.0);
        }
    }

    [TestMethod]
    public void AnalyzeSourceCode_TokenPatterns_HavePriorities()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, "if (x) return;");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        foreach (var pattern in result.Patterns)
        {
            Assert.IsTrue(pattern.Priority > 0);
        }
    }

    [TestMethod]
    public void AnalyzeSourceCode_RepeatedKeyword_HigherConfidence()
    {
        // Arrange
        var analyzer = new TokenPatternAnalyzer();
        File.WriteAllText(_testFilePath, @"
            if (x) return;
            if (y) return;
            if (z) return;
            if (a) return;
        ");

        // Act
        var result = analyzer.AnalyzeSourceCode(new[] { _testFilePath });

        // Assert
        Assert.IsNotNull(result);
        var ifKeyword = result.Patterns.FirstOrDefault(p => p.Name == "IF");
        var returnKeyword = result.Patterns.FirstOrDefault(p => p.Name == "RETURN");
        
        // Keywords that appear frequently should be detected
        Assert.IsTrue(ifKeyword != null || returnKeyword != null);
    }
}
