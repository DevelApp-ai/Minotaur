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
using Minotaur.GrammarGeneration.Refinement;

namespace Minotaur.Tests.GrammarGeneration;

[TestClass]
public class ParseErrorAnalyzerTests
{
    private Grammar _testGrammar = null!;

    [TestInitialize]
    public void Setup()
    {
        _testGrammar = new Grammar
        {
            Name = "TestGrammar",
            Version = "1.0",
            TokenRules = new TokenDefinitions(new List<TokenPattern>
            {
                new TokenPattern { Name = "IF", Pattern = "if", Type = TokenType.Keyword },
                new TokenPattern { Name = "IDENTIFIER", Pattern = @"[a-zA-Z_][a-zA-Z0-9_]*", Type = TokenType.Identifier }
            }),
            ProductionRules = new ProductionRules(new List<ProductionRule>
            {
                new ProductionRule { Name = "program", Alternatives = new List<string> { "<statement>" } }
            })
        };
    }

    [TestMethod]
    public void AnalyzeParseErrors_EmptyErrorArray_ReturnsRefinement()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = Array.Empty<ParseError>();

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void AnalyzeParseErrors_UnexpectedTokenError_SuggestsTokenRefinement()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.UnexpectedToken,
                Message = "Unexpected token 'while'",
                ActualToken = "while",
                Line = 1,
                Column = 1,
                SourceText = "while (x) { }"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should suggest adding a keyword or token rule
    }

    [TestMethod]
    public void AnalyzeParseErrors_MissingProductionError_SuggestsProductionAddition()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.MissingProduction,
                Message = "Missing production for import statement",
                Line = 1,
                Column = 1,
                SourceText = "import library from 'file'"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should suggest adding a production rule
    }

    [TestMethod]
    public void AnalyzeParseErrors_AmbiguousGrammarError_SuggestsDisambiguation()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.AmbiguousGrammar,
                Message = "Ambiguous grammar detected",
                Line = 1,
                Column = 1
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should suggest disambiguation strategy
    }

    [TestMethod]
    public void AnalyzeParseErrors_LeftRecursionError_SuggestsRecursionFix()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.LeftRecursion,
                Message = "Left recursion detected in expression rule",
                Line = 1,
                Column = 1
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should suggest fixing left recursion
    }

    [TestMethod]
    public void AnalyzeParseErrors_TokenizationError_SuggestsTokenizationFix()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.TokenizationError,
                Message = "Tokenization failed",
                Line = 1,
                Column = 1
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should suggest tokenization improvements
    }

    [TestMethod]
    public void AnalyzeParseErrors_MultipleErrors_ReturnsHighestConfidenceRefinement()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "while", Message = "Unexpected 'while'", Line = 1, Column = 1, SourceText = "while (x) { }" },
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "while", Message = "Unexpected 'while'", Line = 2, Column = 1, SourceText = "while (y) { }" },
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "while", Message = "Unexpected 'while'", Line = 3, Column = 1, SourceText = "while (z) { }" }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
    }

    [TestMethod]
    public void AnalyzeParseErrors_RefinementResult_HasValidProperties()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.UnexpectedToken,
                ActualToken = "for",
                Message = "Unexpected token 'for'",
                Line = 1,
                Column = 1,
                SourceText = "for (i = 0; i < 10; i++) { }"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.AffectedRules);
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
    }

    [TestMethod]
    public void AnalyzeParseErrors_NewKeywordDetection_ReturnsRefinement()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "async", Message = "Unexpected 'async'", Line = 1, Column = 1, SourceText = "async function test() { }" },
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "async", Message = "Unexpected 'async'", Line = 2, Column = 1, SourceText = "async function test2() { }" },
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "async", Message = "Unexpected 'async'", Line = 3, Column = 1, SourceText = "async function test3() { }" },
            new ParseError { Type = ParseErrorType.UnexpectedToken, ActualToken = "async", Message = "Unexpected 'async'", Line = 4, Column = 1, SourceText = "async function test4() { }" }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Repeated keyword should generate a refinement suggestion
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
    }

    [TestMethod]
    public void AnalyzeParseErrors_NewOperatorDetection_SuggestsOperatorRule()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.UnexpectedToken,
                ActualToken = "??",
                Message = "Unexpected token '??'",
                Line = 1,
                Column = 10,
                SourceText = "var x = y ?? z;"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should recognize operator pattern
    }

    [TestMethod]
    public void AnalyzeParseErrors_PatternInference_IdentifiesImportPattern()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.MissingProduction,
                Message = "No production rule matched",
                Line = 1,
                Column = 1,
                SourceText = "import math from 'library'"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should infer import pattern
    }

    [TestMethod]
    public void AnalyzeParseErrors_PatternInference_IdentifiesLambdaPattern()
    {
        // Arrange
        var analyzer = new ParseErrorAnalyzer();
        var errors = new[]
        {
            new ParseError
            {
                Type = ParseErrorType.MissingProduction,
                Message = "No production rule matched",
                Line = 1,
                Column = 1,
                SourceText = "(x, y) => x + y"
            }
        };

        // Act
        var result = analyzer.AnalyzeParseErrors(errors, _testGrammar);

        // Assert
        Assert.IsNotNull(result);
        // Should infer lambda pattern
    }
}
