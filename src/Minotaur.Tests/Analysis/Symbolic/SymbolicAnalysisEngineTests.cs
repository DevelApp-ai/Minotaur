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
using Minotaur.Analysis.Symbolic;
using Minotaur.Plugins;

namespace Minotaur.Tests.Analysis.Symbolic;

[TestClass]
public class SymbolicAnalysisEngineTests
{
    [TestMethod]
    public void AnalyzeCode_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "x = 5\nprint(x)";
        var language = "python";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Constraints);
        Assert.IsNotNull(result.ExecutionPaths);
        Assert.IsTrue(result.AnalysisTime.TotalMilliseconds > 0);
    }

    [TestMethod]
    public void AnalyzeCode_PythonNullAccess_DetectsError()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "x = None\nprint(x.value)";
        var language = "python";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Type == SymbolicErrorType.NullPointerAccess));
    }

    [TestMethod]
    public void AnalyzeCode_PythonArrayAccess_DetectsArrayBounds()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "arr = [1, 2, 3]\nprint(arr[5])";
        var language = "python";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Type == SymbolicErrorType.ArrayBoundsViolation));
    }

    [TestMethod]
    public void AnalyzeCode_JavaScriptNullAccess_DetectsError()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "let x = null;\nconsole.log(x.property);";
        var language = "javascript";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Type == SymbolicErrorType.NullPointerAccess));
    }

    [TestMethod]
    public void AnalyzeCode_CSharpNullAccess_DetectsError()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "string s = null;\nConsole.WriteLine(s.Length);";
        var language = "csharp";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Type == SymbolicErrorType.NullPointerAccess));
    }

    [TestMethod]
    public void AnalyzeCode_DivisionByZero_DetectsError()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = "result = 10 / 0";
        var language = "python";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Type == SymbolicErrorType.DivisionByZero));
    }

    [TestMethod]
    public void AnalyzeCode_WithBranching_CreatesMultiplePaths()
    {
        // Arrange
        var engine = new SymbolicAnalysisEngine();
        var sourceCode = @"
if x > 0:
    print('positive')
else:
    print('non-positive')";
        var language = "python";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ExecutionPaths.Count >= 1);
        Assert.IsTrue(result.Constraints.Any(c => c.Type == ConstraintType.ConditionalBranch));
    }

    [TestMethod]
    public void RegisterLanguageAnalyzer_CustomAnalyzer_IsUsed()
    {
        // Arrange
        var pluginManager = new LanguagePluginManager();
        var customPlugin = new TestLanguagePlugin();
        pluginManager.RegisterPlugin(customPlugin);

        var engine = new SymbolicAnalysisEngine(null, pluginManager);

        var sourceCode = "test code";
        var language = "test";

        // Act
        var result = engine.AnalyzeCode(sourceCode, language);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Test error")));
    }

    [TestMethod]
    public void AnalysisResult_GetErrorsBySeverity_FiltersCorrectly()
    {
        // Arrange
        var errors = new List<SymbolicError>
        {
            new SymbolicError(SymbolicErrorType.NullPointerAccess, new SourceLocation(1, 1), "Error 1", 0.9),
            new SymbolicError(SymbolicErrorType.DeadCode, new SourceLocation(2, 1), "Error 2", 0.8),
            new SymbolicError(SymbolicErrorType.UnreachableCode, new SourceLocation(3, 1), "Error 3", 0.7)
        };

        var result = new SymbolicAnalysisResult(
            true,
            errors,
            new List<ExecutionPath>(),
            new List<SymbolicConstraint>(),
            TimeSpan.FromMilliseconds(100)
        );

        // Act
        var criticalErrors = result.GetErrorsBySeverity(ErrorSeverity.Critical);
        var mediumErrors = result.GetErrorsBySeverity(ErrorSeverity.Medium);
        var lowErrors = result.GetErrorsBySeverity(ErrorSeverity.Low);

        // Assert
        Assert.AreEqual(1, criticalErrors.Count);
        Assert.AreEqual(1, mediumErrors.Count);
        Assert.AreEqual(1, lowErrors.Count);
    }

    [TestMethod]
    public void ConstraintSolver_SolveConstraints_GeneratesSolutions()
    {
        // Arrange
        var solver = new ConstraintSolver();
        var constraints = new List<SymbolicConstraint>
        {
            new SymbolicConstraint(
                ConstraintType.ConditionalBranch,
                new SourceLocation(1, 1),
                "Branch condition",
                "x > 0"
            ),
            new SymbolicConstraint(
                ConstraintType.ArrayAccess,
                new SourceLocation(2, 1),
                "Array access",
                "arr[i]"
            )
        };

        // Act
        var solutions = solver.SolveConstraints(constraints);

        // Assert
        Assert.IsTrue(solutions.Count > 0);
        Assert.IsTrue(solutions.Any(s => s.Constraint.Type == ConstraintType.ConditionalBranch));
        Assert.IsTrue(solutions.Any(s => s.Constraint.Type == ConstraintType.ArrayAccess));
    }
}

/// <summary>
/// Test implementation of language plugin for testing purposes
/// </summary>
public class TestLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    public string LanguageId => "test";
    public string DisplayName => "Test Language";
    public string[] SupportedExtensions => new[] { ".test" };

    public async Task<string> UnparseAsync(Core.CognitiveGraphNode graph)
    {
        await Task.CompletedTask;
        return "test code";
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        await Task.CompletedTask;
        return new CompilerBackendRules { LanguageId = LanguageId };
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(Core.CognitiveGraphNode graph)
    {
        await Task.CompletedTask;
        return new UnparseValidationResult { CanUnparse = true };
    }

    // ISymbolicAnalysisPlugin implementation
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return new List<SymbolicError>
        {
            new SymbolicError(
                SymbolicErrorType.AnalysisFailure,
                new SourceLocation(1, 1),
                "Test error detected",
                0.8
            )
        };
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return new List<ErrorPattern>
        {
            new ErrorPattern(
                "test_pattern",
                SymbolicErrorType.AnalysisFailure,
                "test",
                0.8,
                "This is a test pattern"
            )
        };
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return 0.8;
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return new List<TestCase>
        {
            new TestCase("test_case", new Dictionary<string, object>(), "Test case")
        };
    }
}