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

namespace Minotaur.Plugins.Java;

/// <summary>
/// Java language plugin for unparsing and compiler backend generation.
/// All grammar and syntax comes from StepParser with Java/Java17 grammars.
/// This plugin handles Java-specific unparsing and backend code generation.
/// </summary>
public class JavaLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Gets the unique identifier for the Java language.
    /// </summary>
    public string LanguageId => "java";

    /// <summary>
    /// Gets the display name for the Java language.
    /// </summary>
    public string DisplayName => "Java";

    /// <summary>
    /// Gets the array of file extensions supported by Java.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".java" };

    /// <summary>
    /// Converts a cognitive graph representation back to Java source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated Java code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new JavaUnparseVisitor();
        visitor.Visit(graph);
        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for Java code generation.
    /// These rules define how to generate Java code for different parser components.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for Java.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Java package declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "package_declaration",
            GenerationTemplate = "package {package_name};\n\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java import declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_declaration",
            GenerationTemplate = "import {import_path};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java class declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "class_declaration",
            GenerationTemplate = "{modifiers} class {name}{type_parameters} {extends} {implements} {{ {members} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java interface declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "interface_declaration",
            GenerationTemplate = "{modifiers} interface {name}{type_parameters} {extends} {{ {members} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java enum declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enum_declaration",
            GenerationTemplate = "{modifiers} enum {name} {{ {constants} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java annotation type declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "annotation_type_declaration",
            GenerationTemplate = "@interface {name} {{ {members} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java method declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_declaration",
            GenerationTemplate = "{modifiers} {type_parameters} {return_type} {name}({parameters}) {throws} {{ {body} }}",
            GenerationHints = new Dictionary<string, object> { ["IndentBody"] = true, ["BraceStyle"] = "K&R" }
        });

        // Java constructor declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "constructor_declaration",
            GenerationTemplate = "{modifiers} {name}({parameters}) {throws} {{ {body} }}",
            GenerationHints = new Dictionary<string, object> { ["IndentBody"] = true, ["BraceStyle"] = "K&R" }
        });

        // Java field declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "field_declaration",
            GenerationTemplate = "{modifiers} {type} {name}{array_declarator} {initializer};",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java local variable declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "local_variable_declaration",
            GenerationTemplate = "{modifiers} {type} {name}{array_declarator} {initializer};",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java if statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_statement",
            GenerationTemplate = "if ({condition}) {{ {then_statement} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java if-else statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_else_statement",
            GenerationTemplate = "if ({condition}) {{ {then_statement} }} else {{ {else_statement} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java switch statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_statement",
            GenerationTemplate = "switch ({expression}) {{ {case_groups} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java switch expression (Java 14+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_expression",
            GenerationTemplate = "switch ({expression}) {{ {case_expressions} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java for loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "for_statement",
            GenerationTemplate = "for ({init}; {condition}; {update}) {{ {body} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java enhanced for loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enhanced_for_statement",
            GenerationTemplate = "for ({type} {variable} : {expression}) {{ {body} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java while loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "while_statement",
            GenerationTemplate = "while ({condition}) {{ {body} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java do-while loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "do_while_statement",
            GenerationTemplate = "do {{ {body} }} while ({condition});",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = true }
        });

        // Java try-catch-finally
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "try_statement",
            GenerationTemplate = "try {{ {try_block} }} {catch_clauses} {finally_block}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java try-with-resources
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "try_with_resources_statement",
            GenerationTemplate = "try ({resources}) {{ {try_block} }} {catch_clauses} {finally_block}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java synchronized statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "synchronized_statement",
            GenerationTemplate = "synchronized ({expression}) {{ {block} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java lambda expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "lambda_expression",
            GenerationTemplate = "({parameters}) -> {body}",
            GenerationHints = new Dictionary<string, object> { ["ArrowStyle"] = "->" }
        });

        // Java method reference
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_reference",
            GenerationTemplate = "{class_name}::{method_name}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java array creation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "array_creation_expression",
            GenerationTemplate = "new {type}[{dimensions}]{array_initializer}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java object creation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "object_creation_expression",
            GenerationTemplate = "new {type}({arguments}){class_body}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java static initializer
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "static_initializer",
            GenerationTemplate = "static {{ {statements} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java instance initializer
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "instance_initializer",
            GenerationTemplate = "{{ {statements} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R" }
        });

        // Java generic type declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_parameter",
            GenerationTemplate = "{name}{bounds}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java generic type invocation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_argument",
            GenerationTemplate = "{type}",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java annotation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "annotation",
            GenerationTemplate = "@{name}({elements})",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Java template rules
        rules.TemplateRules.AddRange(new[]
        {
            new TemplateRule
            {
                TemplateName = "java_package_template",
                TemplateContent = "package {package_name};\n\n{imports}\n\n{content}",
                RequiredParameters = new List<string> { "package_name", "imports", "content" }
            },
            new TemplateRule
            {
                TemplateName = "java_class_template",
                TemplateContent = "{modifiers} class {name}{type_parameters} {extends} {implements} {{\n{members}\n}}",
                RequiredParameters = new List<string> { "modifiers", "name", "type_parameters", "extends", "implements", "members" }
            },
            new TemplateRule
            {
                TemplateName = "java_method_template",
                TemplateContent = "{modifiers} {type_parameters} {return_type} {name}({parameters}) {throws} {{\n{body}\n}}",
                RequiredParameters = new List<string> { "modifiers", "type_parameters", "return_type", "name", "parameters", "throws", "body" }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Gets the code formatting options specific to Java code generation.
    /// </summary>
    /// <returns>The formatting options for Java code generation.</returns>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["BraceNewLine"] = false,
                ["SpaceAfterComma"] = true,
                ["SpaceAroundOperators"] = true,
                ["SpaceAfterKeywords"] = true,
                ["SpaceBeforeBrace"] = true
            }
        };
    }

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid Java code.
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

        // Validate Java-specific constructs
        var validator = new JavaUnparseValidator();
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
    private readonly JavaSymbolicAnalysisPlugin _symbolicAnalysis = new();

    /// <summary>
    /// Analyzes Java source code for symbolic errors using language-specific patterns
    /// </summary>
    /// <param name="sourceCode">The Java source code to analyze</param>
    /// <param name="constraints">Symbolic constraints extracted from the code</param>
    /// <returns>List of detected symbolic errors</returns>
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _symbolicAnalysis.AnalyzeSymbolic(sourceCode, constraints);
    }

    /// <summary>
    /// Gets Java-specific error patterns that can be detected by symbolic analysis
    /// </summary>
    /// <returns>List of error patterns for Java</returns>
    public List<ErrorPattern> GetErrorPatterns()
    {
        return _symbolicAnalysis.GetErrorPatterns();
    }

    /// <summary>
    /// Gets the confidence level for detecting a specific error type in Java
    /// </summary>
    /// <param name="errorType">The type of error to check confidence for</param>
    /// <returns>Confidence level between 0.0 and 1.0</returns>
    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _symbolicAnalysis.GetErrorConfidence(errorType);
    }

    /// <summary>
    /// Generates test cases that could trigger the specified error in Java code
    /// </summary>
    /// <param name="error">The symbolic error to generate test cases for</param>
    /// <param name="sourceCode">The original Java source code</param>
    /// <returns>List of generated test cases</returns>
    public List<string> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _symbolicAnalysis.GenerateTestCases(error, sourceCode);
    }
}
