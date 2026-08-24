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

namespace Minotaur.Plugins.Java;

/// <summary>
/// Java language plugin for unparsing and compiler backend generation.
/// Supports Java 8 through Java 17+ features including records, sealed classes, and text blocks.
/// </summary>
public class JavaLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly JavaUnparseVisitor _unparseVisitor;
    private readonly JavaValidationVisitor _validationVisitor;

    /// <summary>
    /// Initializes a new instance of the JavaLanguagePlugin.
    /// </summary>
    public JavaLanguagePlugin()
    {
        _unparseVisitor = new JavaUnparseVisitor();
        _validationVisitor = new JavaValidationVisitor();
    }

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
    public string[] SupportedExtensions => new[] { ".java", ".JAVA" };

    /// <summary>
    /// Converts a cognitive graph representation back to Java source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated Java code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        _unparseVisitor.Reset();
        _unparseVisitor.Visit(graph);
        await Task.CompletedTask;
        return _unparseVisitor.GetGeneratedCode();
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
            GenerationTemplate = "package {name};\n\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java import declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_declaration",
            GenerationTemplate = "import {name};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_static_declaration",
            GenerationTemplate = "import static {name};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java class declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "class_declaration",
            GenerationTemplate = "{modifiers} class {name}{type_parameters} {extends} {implements} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java interface declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "interface_declaration",
            GenerationTemplate = "{modifiers} interface {name}{type_parameters} {extends} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java enum declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enum_declaration",
            GenerationTemplate = "{modifiers} enum {name} {implements} {{ {constants} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java enum constant
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enum_constant",
            GenerationTemplate = "{name}{arguments}{class_body}",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java record declaration (Java 14+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "record_declaration",
            GenerationTemplate = "{modifiers} record {name}{type_parameters}({parameters}) {implements} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false, ["MinJavaVersion"] = 14 }
        });

        // Java sealed class declaration (Java 15+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "sealed_class_declaration",
            GenerationTemplate = "{modifiers} sealed class {name}{type_parameters} {extends} {implements} permits {permitted_types} {{ {members} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false, ["MinJavaVersion"] = 15 }
        });

        // Java method declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_declaration",
            GenerationTemplate = "{modifiers} {type_parameters} {return_type} {name}({parameters}) {throws} {{ {body} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java constructor declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "constructor_declaration",
            GenerationTemplate = "{modifiers} {name}({parameters}) {throws} {{ {body} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java field declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "field_declaration",
            GenerationTemplate = "{modifiers} {type} {name} {initializer};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java local variable declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "local_variable_declaration",
            GenerationTemplate = "{modifiers} {type} {name} {initializer};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java if statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_statement",
            GenerationTemplate = "if ({condition}) {{ {then_statement} }}{else_statement}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java for statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "for_statement",
            GenerationTemplate = "for ({initialization}; {condition}; {update}) {{ {statement} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java enhanced for statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enhanced_for_statement",
            GenerationTemplate = "for ({variable_modifiers} {type} {name} : {expression}) {{ {statement} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java while statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "while_statement",
            GenerationTemplate = "while ({condition}) {{ {statement} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java do statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "do_statement",
            GenerationTemplate = "do {{ {statement} }} while ({condition});\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = true }
        });

        // Java try statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "try_statement",
            GenerationTemplate = "try {{ {block} }}{catches}{finally}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java catch clause
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "catch_clause",
            GenerationTemplate = " catch ({parameter}) {{ {block} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java finally clause
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "finally_clause",
            GenerationTemplate = " finally {{ {block} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java switch statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_statement",
            GenerationTemplate = "switch ({expression}) {{ {case_groups} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java switch expression (Java 14+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_expression",
            GenerationTemplate = "switch ({expression}) {{ {case_groups} }}",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["MinJavaVersion"] = 14 }
        });

        // Java case group
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_case_group",
            GenerationTemplate = "case {labels}: {statements}",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = false }
        });

        // Java synchronized statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "synchronized_statement",
            GenerationTemplate = "synchronized ({expression}) {{ {block} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java return statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "return_statement",
            GenerationTemplate = "return {expression};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java throw statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "throw_statement",
            GenerationTemplate = "throw {expression};\n",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = true }
        });

        // Java try-with-resources
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "try_with_resources_statement",
            GenerationTemplate = "try ({resources}) {{ {block} }}{catches}{finally}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false }
        });

        // Java lambda expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "lambda_expression",
            GenerationTemplate = "{parameters} -> {body}",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = false }
        });

        // Java method reference
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_reference",
            GenerationTemplate = "{name}::{method}",
            GenerationHints = new Dictionary<string, object> { ["Semicolon"] = false }
        });

        // Java text block (Java 15+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "text_block",
            GenerationTemplate = """{content}"""\n",
            GenerationHints = new Dictionary<string, object> { ["MinJavaVersion"] = 15 }
        });

        // Java module declaration (Java 9+)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "module_declaration",
            GenerationTemplate = "{modifiers} module {name} {{ {directives} }}\n",
            GenerationHints = new Dictionary<string, object> { ["BraceStyle"] = "K&R", ["Semicolon"] = false, ["MinJavaVersion"] = 9 }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Get cosmetic code formatting options for Java output.
    /// </summary>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentSize = 4,
            UseTabs = false,
            BraceStyle = "K&R",
            IndentBraces = true,
            IndentCaseLabels = true,
            NewLineAfterSemicolon = true,
            SpaceAfterKeywords = true,
            SpaceBeforeBraces = false,
            LanguageSpecificOptions = new Dictionary<string, object>
            {
                ["JavaVersion"] = "17",
                ["TextBlockEnabled"] = true,
                ["RecordsEnabled"] = true
            }
        };
    }

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid Java code.
    /// </summary>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        _validationVisitor.Reset();
        _validationVisitor.Visit(graph);
        await Task.CompletedTask;
        return _validationVisitor.GetValidationResult();
    }

    /// <summary>
    /// Gets the symbolic analysis visitor for Java.
    /// </summary>
    public ISymbolicAnalysisVisitor GetSymbolicAnalysisVisitor()
    {
        return _validationVisitor;
    }
}
