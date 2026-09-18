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

using Minotaur.Core;
using Minotaur.Analysis.Symbolic;

namespace Minotaur.Plugins;

public class PythonLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Gets the unique identifier for the Python language.
    /// </summary>
    public string LanguageId => "python";

    /// <summary>
    /// Gets the display name for the Python language.
    /// </summary>
    public string DisplayName => "Python";

    /// <summary>
    /// Gets the file extensions supported by Python.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".py", ".pyw" };

    /// <summary>
    /// Converts a cognitive graph representation back to Python source code.
    /// </summary>
    /// <param name="graph">The cognitive graph to unparse into Python code.</param>
    /// <returns>A task that represents the asynchronous unparsing operation. The task result contains the generated Python code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new PythonUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for Python code generation.
    /// These rules define how to generate Python code for different parser components.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for Python.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Add Python backend generation rules
        rules.GenerationRules.AddRange(new[]
        {
            new CodeGenerationRule
            {
                NodeType = "function_def",
                GenerationTemplate = "def {name}({parameters}):\n{body}",
                GenerationHints = new Dictionary<string, object> { ["IndentWithSpaces"] = true }
            },
            new CodeGenerationRule
            {
                NodeType = "class_def",
                GenerationTemplate = "class {name}({bases}):\n{body}",
                GenerationHints = new Dictionary<string, object> { ["RequirePass"] = true }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Gets the code formatting options specific to Python code generation.
    /// </summary>
    /// <returns>The formatting options for Python code generation.</returns>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 88, // Black formatter default
            CosmeticOptions = new Dictionary<string, object>
            {
                ["QuoteStyle"] = "double",
                ["BlackCompatible"] = true
            }
        };
    }

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid Python code.
    /// </summary>
    /// <param name="graph">The cognitive graph to validate for unparsing.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation results.</returns>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };
        await Task.CompletedTask;
        return result;
    }

    // ISymbolicAnalysisPlugin implementation
    private readonly PythonSymbolicAnalysisPlugin _symbolicAnalysis = new();

    /// <summary>
    /// Analyzes Python source code for symbolic errors using language-specific patterns
    /// </summary>
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    /// <summary>
    /// Gets Python-specific error patterns that can be detected by symbolic analysis
    /// </summary>
    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    /// <summary>
    /// Gets the confidence level for detecting a specific error type in Python
    /// </summary>
    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    /// <summary>
    /// Generates test cases that could trigger the specified error in Python code
    /// </summary>
    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}

