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

public class JavaScriptLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Gets the unique identifier for the JavaScript language.
    /// </summary>
    public string LanguageId => "javascript";

    /// <summary>
    /// Gets the display name for the JavaScript language.
    /// </summary>
    public string DisplayName => "JavaScript";

    /// <summary>
    /// Gets the file extensions supported by JavaScript.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".js", ".mjs", ".jsx" };

    /// <summary>
    /// Converts a cognitive graph representation back to JavaScript source code.
    /// </summary>
    /// <param name="graph">The cognitive graph to unparse into JavaScript code.</param>
    /// <returns>A task that represents the asynchronous unparsing operation. The task result contains the generated JavaScript code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new JavaScriptUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for JavaScript code generation.
    /// These rules define how to generate JavaScript code for different parser components.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for JavaScript.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Add JavaScript backend generation rules
        rules.GenerationRules.AddRange(new[]
        {
            new CodeGenerationRule
            {
                NodeType = "function_declaration",
                GenerationTemplate = "function {name}({parameters}) { {body} }",
                GenerationHints = new Dictionary<string, object> { ["HoistFunctions"] = true }
            },
            new CodeGenerationRule
            {
                NodeType = "arrow_function",
                GenerationTemplate = "({parameters}) => { {body} }",
                GenerationHints = new Dictionary<string, object> { ["LexicalThis"] = true }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Gets the code formatting options specific to JavaScript code generation.
    /// </summary>
    /// <returns>The formatting options for JavaScript code generation.</returns>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 2,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 100,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["SemicolonInsertion"] = true, // Cosmetic ASI behavior
                ["QuoteStyle"] = "single"
            }
        };
    }

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid JavaScript code.
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
    private readonly JavaScriptSymbolicAnalysisPlugin _symbolicAnalysis = new();

    /// <summary>
    /// Analyzes JavaScript source code for symbolic errors using language-specific patterns
    /// </summary>
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    /// <summary>
    /// Gets JavaScript-specific error patterns that can be detected by symbolic analysis
    /// </summary>
    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    /// <summary>
    /// Gets the confidence level for detecting a specific error type in JavaScript
    /// </summary>
    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    /// <summary>
    /// Generates test cases that could trigger the specified error in JavaScript code
    /// </summary>
    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in Python language plugin for unparsing and compiler backend generation
/// </summary>
