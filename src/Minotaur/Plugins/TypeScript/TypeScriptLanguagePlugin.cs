/*
 * This file is part of Minotaur.
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>. 
 */

using Minotaur.Core;
using Minotaur.Analysis.Symbolic;

namespace Minotaur.Plugins.TypeScript;

/// <summary>
/// TypeScript language plugin for unparsing and compiler backend generation.
/// Extends JavaScript with TypeScript-specific features like interfaces, type aliases,
/// generics, decorators, and type annotations.
/// </summary>
public class TypeScriptLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Gets the unique identifier for the TypeScript language.
    /// </summary>
    public string LanguageId => "typescript";

    /// <summary>
    /// Gets the display name for the TypeScript language.
    /// </summary>
    public string DisplayName => "TypeScript";

    /// <summary>
    /// Gets the array of file extensions supported by TypeScript.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".ts", ".tsx" };

    /// <summary>
    /// Converts a cognitive graph representation back to TypeScript source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated TypeScript code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new TypeScriptUnparseVisitor();
        visitor.Visit(graph);
        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for TypeScript code generation.
    /// These rules define how to generate TypeScript code for different parser components.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for TypeScript.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // TypeScript import declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_declaration",
            GenerationTemplate = "import {imported} from '{module}';\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_all_declaration",
            GenerationTemplate = "import * as {name} from '{module}';\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript export declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "export_declaration",
            GenerationTemplate = "export {{ {declarations} }}\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript interface declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "interface_declaration",
            GenerationTemplate = "{modifiers} interface {name}{type_parameters} {extends} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = true }
        });

        // TypeScript type alias declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_alias_declaration",
            GenerationTemplate = "{modifiers} type {name}{type_parameters} = {type};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript enum declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enum_declaration",
            GenerationTemplate = "{modifiers} enum {name} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = true }
        });

        // TypeScript class declaration (extends JavaScript)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "class_declaration",
            GenerationTemplate = "{modifiers} class {name}{type_parameters} {extends} {implements} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // TypeScript method declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_declaration",
            GenerationTemplate = "{modifiers} {name}{type_parameters}({parameters}): {return_type} {{ {body} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // TypeScript constructor declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "constructor_declaration",
            GenerationTemplate = "constructor({parameters}) {{ {body} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // TypeScript property declaration with type annotation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "property_declaration",
            GenerationTemplate = "{modifiers} {name}{optional}: {type} {initializer};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript field declaration with type annotation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "field_declaration",
            GenerationTemplate = "{modifiers} {name}{optional}: {type} {initializer};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript local variable declaration with type annotation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "local_variable_declaration",
            GenerationTemplate = "{modifiers} {name}{optional}: {type} {initializer};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // TypeScript arrow function
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "arrow_function",
            GenerationTemplate = "{modifiers} ({parameters}): {return_type} => {body}\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = false }
        });

        // TypeScript function expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "function_expression",
            GenerationTemplate = "function ({parameters}): {return_type} {{ {body} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // TypeScript decorator
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "decorator",
            GenerationTemplate = "@{name}({arguments})\n",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript generic type declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_parameter",
            GenerationTemplate = "{name}{constraint}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript generic type invocation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_argument",
            GenerationTemplate = "{type}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript type annotation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_annotation",
            GenerationTemplate = ": {type}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript optional parameter
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "optional_parameter",
            GenerationTemplate = "{name}?: {type}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript rest parameter
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "rest_parameter",
            GenerationTemplate = "...{name}: {type}[]",
            GenerationHints = new Dictionary<string, object> { }
        });

        // TypeScript template rules
        rules.TemplateRules.AddRange(new[]
        {
            new TemplateRule
            {
                TemplateName = "typescript_interface_template",
                TemplateContent = "export interface {name}{type_parameters} {extends} {{\n{members}\n}}",
                RequiredParameters = new List<string> { "name", "type_parameters", "extends", "members" }
            },
            new TemplateRule
            {
                TemplateName = "typescript_type_alias_template",
                TemplateContent = "export type {name}{type_parameters} = {type};",
                RequiredParameters = new List<string> { "name", "type_parameters", "type" }
            },
            new TemplateRule
            {
                TemplateName = "typescript_class_template",
                TemplateContent = "export class {name}{type_parameters} {extends} {implements} {{\n{members}\n}}",
                RequiredParameters = new List<string> { "name", "type_parameters", "extends", "implements", "members" }
            },
            new TemplateRule
            {
                TemplateName = "typescript_arrow_function_template",
                TemplateContent = "const {name}: ({parameters}) => {return_type} = ({parameters}) => {body};",
                RequiredParameters = new List<string> { "name", "parameters", "return_type", "body" }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Gets the code formatting options specific to TypeScript code generation.
    /// </summary>
    /// <returns>The formatting options for TypeScript code generation.</returns>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 2,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["BraceNewLine"] = false,
                ["SpaceAfterComma"] = true,
                ["SpaceAroundOperators"] = true,
                ["SpaceAfterKeywords"] = true,
                ["SpaceBeforeBrace"] = false,
                ["Semicolons"] = "prefer",
                ["QuoteStyle"] = "double"
            }
        };
    }

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid TypeScript code.
    /// </summary>
    /// <param name="graph">The cognitive graph to validate for unparsing.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation results.</returns>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };

        if (graph == null)
        {
            result.CanUnparse = false;
            result.Errors.Add(new UnparseValidationError
            {
                Message = "Cannot unparse null graph",
                NodeId = "null",
                NodeType = "null"
            });
            return result;
        }

        // Validate TypeScript-specific constructs
        var validator = new TypeScriptUnparseValidator();
        var validationErrors = validator.Validate(graph);
        
        if (validationErrors.Any())
        {
            result.CanUnparse = false;
            result.Errors.AddRange(validationErrors);
        }

        await Task.CompletedTask;
        return result;
    }

    // ISymbolicAnalysisPlugin implementation
    private readonly TypeScriptSymbolicAnalysisPlugin _symbolicAnalysis = new();

    /// <summary>
    /// Analyzes TypeScript source code for symbolic errors using language-specific patterns
    /// </summary>
    /// <param name="sourceCode">The TypeScript source code to analyze</param>
    /// <param name="constraints">Symbolic constraints extracted from the code</param>
    /// <returns>List of detected symbolic errors</returns>
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    /// <summary>
    /// Gets TypeScript-specific error patterns that can be detected by symbolic analysis
    /// </summary>
    /// <returns>List of error patterns for TypeScript</returns>
    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    /// <summary>
    /// Gets the confidence level for detecting a specific error type in TypeScript
    /// </summary>
    /// <param name="errorType">The type of error to check confidence for</param>
    /// <returns>Confidence level between 0.0 and 1.0</returns>
    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    /// <summary>
    /// Generates test cases that could trigger the specified error in TypeScript code
    /// </summary>
    /// <param name="error">The symbolic error to generate test cases for</param>
    /// <param name="sourceCode">The original TypeScript source code</param>
    /// <returns>List of generated test cases</returns>
    public List<string> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}
