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

namespace Minotaur.Plugins.Rust;

/// <summary>
/// Rust language plugin for Minotaur.
/// Provides full Rust 2021 edition support for modern systems programming.
/// Supports ownership model, traits, generics, async/await, and FFI.
/// </summary>
public class RustLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly RustUnparseVisitor _unparseVisitor;
    private readonly RustValidationVisitor _validationVisitor;

    /// <summary>
    /// Initializes a new instance of the RustLanguagePlugin.
    /// </summary>
    public RustLanguagePlugin()
    {
        _unparseVisitor = new RustUnparseVisitor();
        _validationVisitor = new RustValidationVisitor();
    }

    /// <summary>
    /// Gets the unique identifier for the Rust language.
    /// </summary>
    public string LanguageId => "rust";

    /// <summary>
    /// Gets the display name for the Rust language.
    /// </summary>
    public string DisplayName => "Rust";

    /// <summary>
    /// Gets the array of file extensions supported by Rust.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".rs" };

    /// <summary>
    /// Converts a cognitive graph representation back to Rust source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated Rust code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        _unparseVisitor.Reset();
        _unparseVisitor.Visit(graph);
        await Task.CompletedTask;
        return _unparseVisitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for Rust code generation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for Rust.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Rust module system
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "module_declaration",
            GenerationTemplate = "mod {name};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Snake", ["FilePerModule"] = true }
        });

        // Rust use declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "use_declaration",
            GenerationTemplate = "use {path}::{items};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Snake" }
        });

        // Rust struct declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "struct_declaration",
            GenerationTemplate = "pub struct {name} { {fields} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal", ["Visibility"] = "pub" }
        });

        // Rust enum declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "enum_declaration",
            GenerationTemplate = "pub enum {name} { {variants} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal", ["Visibility"] = "pub" }
        });

        // Rust trait declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "trait_declaration",
            GenerationTemplate = "pub trait {name} { {items} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal", ["Visibility"] = "pub" }
        });

        // Rust impl block
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "impl_block",
            GenerationTemplate = "impl {trait} for {type} { {items} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Pascal" }
        });

        // Rust function
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "function_declaration",
            GenerationTemplate = "pub fn {name}({parameters}) -> {return_type} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Snake", ["Visibility"] = "pub" }
        });

        // Rust method
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "method_declaration",
            GenerationTemplate = "pub fn {name}(&{self_kind}self{parameters}) -> {return_type} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Snake", ["Visibility"] = "pub" }
        });

        // Rust if expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_expression",
            GenerationTemplate = "if {condition} { {then_block} } else { {else_block} }\n",
            GenerationHints = new Dictionary<string, object> { ["ExpressionBased"] = true }
        });

        // Rust match expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "match_expression",
            GenerationTemplate = "match {expression} { {arms} }\n",
            GenerationHints = new Dictionary<string, object> { ["ExpressionBased"] = true }
        });

        // Rust loop expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "loop_expression",
            GenerationTemplate = "loop { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["ExpressionBased"] = true }
        });

        // Rust while loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "while_loop",
            GenerationTemplate = "while {condition} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["ExpressionBased"] = true }
        });

        // Rust for loop
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "for_loop",
            GenerationTemplate = "for {pattern} in {iterator} { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["ExpressionBased"] = true }
        });

        // Rust let declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "let_declaration",
            GenerationTemplate = "let {pattern}: {type} = {expression};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Snake" }
        });

        // Rust const declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "const_declaration",
            GenerationTemplate = "const {name}: {type} = {expression};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "ScreamingSnake" }
        });

        // Rust static declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "static_declaration",
            GenerationTemplate = "static {name}: {type} = {expression};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "ScreamingSnake" }
        });

        // Rust return expression
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "return_expression",
            GenerationTemplate = "return {expression};\n",
            GenerationHints = new Dictionary<string, object> { }
        });

        // Rust async block
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "async_block",
            GenerationTemplate = "async { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Async"] = true }
        });

        // Rust unsafe block
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "unsafe_block",
            GenerationTemplate = "unsafe { {body} }\n",
            GenerationHints = new Dictionary<string, object> { ["Unsafe"] = true }
        });

        // Rust macro invocation
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "macro_invocation",
            GenerationTemplate = "{name}!({arguments})\n",
            GenerationHints = new Dictionary<string, object> { ["Macro"] = true }
        });

        // Rust attribute
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "attribute",
            GenerationTemplate = "#[{name}({arguments})]\n",
            GenerationHints = new Dictionary<string, object> { ["Attribute"] = true }
        });

        // Rust comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "comment",
            GenerationTemplate = "// {text}\n",
            GenerationHints = new Dictionary<string, object> { ["Comment"] = true }
        });

        // Rust doc comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "doc_comment",
            GenerationTemplate = "/// {text}\n",
            GenerationHints = new Dictionary<string, object> { ["DocComment"] = true }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Get cosmetic code formatting options for Rust output.
    /// </summary>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentSize = 4,
            UseTabs = false,
            BraceStyle = "Rust",
            IndentBraces = true,
            IndentCaseLabels = false,
            NewLineAfterSemicolon = true,
            SpaceAfterKeywords = true,
            SpaceBeforeBraces = false,
            LanguageSpecificOptions = new Dictionary<string, object>
            {
                ["RustEdition"] = "2021",
                ["Case"] = "Snake",
                ["UseRustfmt"] = true,
                ["MaxLineLength"] = 100
            }
        };
    }

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid Rust code.
    /// </summary>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        _validationVisitor.Reset();
        _validationVisitor.Visit(graph);
        await Task.CompletedTask;
        return _validationVisitor.GetValidationResult();
    }

    /// <summary>
    /// Gets the symbolic analysis visitor for Rust.
    /// </summary>
    public ISymbolicAnalysisVisitor GetSymbolicAnalysisVisitor()
    {
        return _validationVisitor;
    }
}
