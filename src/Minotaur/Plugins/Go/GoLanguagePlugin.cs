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

namespace Minotaur.Plugins.Go;

/// <summary>
/// Go language plugin for Minotaur.
/// Provides full Go 1.21+ support for modern backend development.
/// Supports goroutines, channels, interfaces, and concurrency primitives.
/// </summary>
public class GoLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly GoUnparseVisitor _unparseVisitor;
    private readonly GoValidationVisitor _validationVisitor;

    /// <summary>
    /// Initializes a new instance of the GoLanguagePlugin.
    /// </summary>
    public GoLanguagePlugin()
    {
        _unparseVisitor = new GoUnparseVisitor();
        _validationVisitor = new GoValidationVisitor();
    }

    /// <summary>
    /// Gets the unique identifier for the Go language.
    /// </summary>
    public string LanguageId => "go";

    /// <summary>
    /// Gets the display name for the Go language.
    /// </summary>
    public string DisplayName => "Go";

    /// <summary>
    /// Gets the array of file extensions supported by Go.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".go" };

    /// <summary>
    /// Converts a cognitive graph representation back to Go source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated Go code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        _unparseVisitor.Reset();
        _unparseVisitor.Visit(graph);
        await Task.CompletedTask;
        return _unparseVisitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for Go code generation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for Go.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Go package declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "package_declaration",
            GenerationTemplate = "package {name}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Lower", ["Required"] = true }
        });

        // Go import declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "import_declaration",
            GenerationTemplate = "import (\n\t\"{path}\"\n)\n",
            GenerationHints = new Dictionary<string, object> { ["Grouped"] = true }
        });

        // Go function declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "function_declaration",
            GenerationTemplate = "func {name}({parameters}) {return_type} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Camel", ["BracesOnNewline"] = false }
        });

        // Go method declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_declaration",
            GenerationTemplate = "func ({receiver}) {name}({parameters}) {return_type} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Camel" }
        });

        // Go struct declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "struct_declaration",
            GenerationTemplate = "type {name} struct { {fields} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal" }
        });

        // Go interface declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "interface_declaration",
            GenerationTemplate = "type {name} interface { {methods} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal" }
        });

        // Go type alias
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "type_alias",
            GenerationTemplate = "type {name} {type}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal" }
        });

        // Go variable declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "variable_declaration",
            GenerationTemplate = "var {name} {type} = {value}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Camel" }
        });

        // Go constant declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "const_declaration",
            GenerationTemplate = "const {name} {type} = {value}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal" }
        });

        // Go if statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_statement",
            GenerationTemplate = "if {condition} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["BracesOnNewline"] = false }
        });

        // Go if-else statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_else_statement",
            GenerationTemplate = "if {condition} { {then_body} } else { {else_body} }\n",
            GenerationHints = new Dictionary<string, object> { ["BracesOnNewline"] = false }
        });

        // Go switch statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "switch_statement",
            GenerationTemplate = "switch {expression} { {cases} }\n",
            GenerationHints = new Dictionary<string, object> { ["BracesOnNewline"] = false }
        });

        // Go for loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "for_loop",
            GenerationTemplate = "for {initialization}; {condition}; {post} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["BracesOnNewline"] = false }
        });

        // Go range loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "range_loop",
            GenerationTemplate = "for {index}, {value} := range {collection} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["BracesOnNewline"] = false }
        });

        // Go go statement (goroutine)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "go_statement",
            GenerationTemplate = "go {function_call}\n",
            GenerationHints = new Dictionary<string, object> { ["Concurrent"] = true }
        });

        // Go channel declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "channel_declaration",
            GenerationTemplate = "{name} := make(chan {type}, {buffer})\n",
            GenerationHints = new Dictionary<string, object> { ["Concurrent"] = true }
        });

        // Go select statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "select_statement",
            GenerationTemplate = "select { {cases} }\n",
            GenerationHints = new Dictionary<string, object> { ["Concurrent"] = true }
        });

        // Go defer statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "defer_statement",
            GenerationTemplate = "defer {function_call}\n",
            GenerationHints = new Dictionary<string, object> { ["Deferred"] = true }
        });

        // Go return statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "return_statement",
            GenerationTemplate = "return {values}\n",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Go comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "comment",
            GenerationTemplate = "// {text}\n",
            GenerationHints = new Dictionary<string, object> { ["Comment"] = true }
        });

        // Go doc comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "doc_comment",
            GenerationTemplate = "// {text}\n",
            GenerationHints = new Dictionary<string, object> { ["DocComment"] = true }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Get cosmetic code formatting options for Go output.
    /// </summary>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentSize = 1,
            UseTabs = true,
            BraceStyle = "Go",
            IndentBraces = false,
            IndentCaseLabels = false,
            NewLineAfterSemicolon = false,
            SpaceAfterKeywords = true,
            SpaceBeforeBraces = false,
            LanguageSpecificOptions = new Dictionary<string, object>
            {
                ["GoVersion"] = "1.21",
                ["Case"] = "Camel",
                ["UseGoFmt"] = true,
                ["MaxLineLength"] = 80
            }
        };
    }

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid Go code.
    /// </summary>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        _validationVisitor.Reset();
        _validationVisitor.Visit(graph);
        await Task.CompletedTask;
        return _validationVisitor.GetValidationResult();
    }

    /// <summary>
    /// Gets the symbolic analysis visitor for Go.
    /// </summary>
    public ISymbolicAnalysisVisitor GetSymbolicAnalysisVisitor()
    {
        return _validationVisitor;
    }
}
