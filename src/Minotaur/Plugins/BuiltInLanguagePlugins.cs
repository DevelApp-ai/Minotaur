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

/// <summary>
/// Built-in C# language plugin for unparsing and compiler backend generation
/// All grammar and syntax comes from StepParser - plugins handle backend generation
/// </summary>
public class CSharpLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Gets the unique identifier for the C# language.
    /// </summary>
    public string LanguageId => "csharp";

    /// <summary>
    /// Gets the display name for the C# language.
    /// </summary>
    public string DisplayName => "C#";

    /// <summary>
    /// Gets the array of file extensions supported by C#.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".cs", ".csx" };

    /// <summary>
    /// Converts a cognitive graph representation back to C# source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated C# code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        // Generate C# source code from cognitive graph
        // All syntax and grammar knowledge comes from StepParser - this only formats output
        var visitor = new CSharpUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Add C# backend generation rules (NOT grammar rules - those come from StepParser)
        rules.GenerationRules.AddRange(new[]
        {
            new CodeGenerationRule
            {
                NodeType = "class_declaration",
                GenerationTemplate = "public class {name} { {members} }",
                GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "Allman" }
            },
            new CodeGenerationRule
            {
                NodeType = "method_declaration",
                GenerationTemplate = "{modifiers} {returnType} {name}({parameters}) { {body} }",
                GenerationHints = new Dictionary<string, object> { ["IndentBody"] = true }
            },
            new CodeGenerationRule
            {
                NodeType = "property_declaration",
                GenerationTemplate = "{modifiers} {type} {name} { get; set; }",
                GenerationHints = new Dictionary<string, object> { ["AutoProperty"] = true }
            }
        });

        // Add C# template rules
        rules.TemplateRules.AddRange(new[]
        {
            new TemplateRule
            {
                TemplateName = "namespace_template",
                TemplateContent = "namespace {namespace_name}\n{\n{content}\n}",
                RequiredParameters = new List<string> { "namespace_name", "content" }
            },
            new TemplateRule
            {
                TemplateName = "using_template",
                TemplateContent = "using {namespace};",
                RequiredParameters = new List<string> { "namespace" }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\r\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["BraceNewLine"] = true,  // Cosmetic only - not syntax
                ["SpaceAfterComma"] = true,
                ["SpaceAroundOperators"] = true
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };

        // Only validate that we can generate output - no syntax validation (that's StepParser's job)
        if (graph == null)
        {
            result.CanUnparse = false;
            result.Errors.Add(new UnparseValidationError
            {
                Message = "Cannot unparse null graph",
                NodeId = "null",
                NodeType = "null"
            });
        }

        await Task.CompletedTask;
        return result;
    }

    // ISymbolicAnalysisPlugin implementation
    private readonly CSharpSymbolicAnalysisPlugin _symbolicAnalysis = new();

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in JavaScript language plugin for unparsing and compiler backend generation
/// </summary>
public class JavaScriptLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    public string LanguageId => "javascript";
    public string DisplayName => "JavaScript";
    public string[] SupportedExtensions => new[] { ".js", ".mjs", ".jsx" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new JavaScriptUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

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

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };
        await Task.CompletedTask;
        return result;
    }

    // ISymbolicAnalysisPlugin implementation
    private readonly JavaScriptSymbolicAnalysisPlugin _symbolicAnalysis = new();

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in Python language plugin for unparsing and compiler backend generation
/// </summary>
public class PythonLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    public string LanguageId => "python";
    public string DisplayName => "Python";
    public string[] SupportedExtensions => new[] { ".py", ".pyw" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new PythonUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

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

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };
        await Task.CompletedTask;
        return result;
    }

    // ISymbolicAnalysisPlugin implementation
    private readonly PythonSymbolicAnalysisPlugin _symbolicAnalysis = new();

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}