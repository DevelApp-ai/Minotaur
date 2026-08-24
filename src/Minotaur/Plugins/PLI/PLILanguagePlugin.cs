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

namespace Minotaur.Plugins.PLI;

/// <summary>
/// PL/I language plugin for Minotaur.
/// Provides full PL/I (F) standard support for mainframe modernization.
/// Supports advanced data structures, exception handling, multitasking, and IBM mainframe integration.
/// </summary>
public class PLILanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly PLIUnparseVisitor _unparseVisitor;
    private readonly PLIValidationVisitor _validationVisitor;

    /// <summary>
    /// Initializes a new instance of the PLILanguagePlugin.
    /// </summary>
    public PLILanguagePlugin()
    {
        _unparseVisitor = new PLIUnparseVisitor();
        _validationVisitor = new PLIValidationVisitor();
    }

    /// <summary>
    /// Gets the unique identifier for the PL/I language.
    /// </summary>
    public string LanguageId => "pli";

    /// <summary>
    /// Gets the display name for the PL/I language.
    /// </summary>
    public string DisplayName => "PL/I";

    /// <summary>
    /// Gets the array of file extensions supported by PL/I.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".pli", ".PLI", ".pl1", ".PL1" };

    /// <summary>
    /// Converts a cognitive graph representation back to PL/I source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated PL/I code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        _unparseVisitor.Reset();
        _unparseVisitor.Visit(graph);
        await Task.CompletedTask;
        return _unparseVisitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for PL/I code generation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for PL/I.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // PL/I program structure
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "program_declaration",
            GenerationTemplate = "{name}: PROC OPTIONS(MAIN);\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I data declarations
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "data_declaration",
            GenerationTemplate = "DCL {name} {type} {initialization};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I structure declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "structure_declaration",
            GenerationTemplate = "DCL 1 {name},\n  2 {members};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I array declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "array_declaration",
            GenerationTemplate = "DCL {name}({dimensions}) {type};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I file declaration
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "file_declaration",
            GenerationTemplate = "DCL {name} FILE;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I procedure
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "procedure_declaration",
            GenerationTemplate = "{name}: PROC({parameters});\n{body}\nEND {name};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I if statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_statement",
            GenerationTemplate = "IF {condition} THEN;\n{then_statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I if-else statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_else_statement",
            GenerationTemplate = "IF {condition} THEN;\n{then_statements}\nELSE;\n{else_statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I do group
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "do_group",
            GenerationTemplate = "DO;\n{statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I do while
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "do_while",
            GenerationTemplate = "DO WHILE({condition});\n{statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I do for
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "do_for",
            GenerationTemplate = "DO {index} = {start} TO {end} BY {step};\n{statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I select statement (switch)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "select_statement",
            GenerationTemplate = "SELECT({expression});\n{when_clauses}\nOTHER;\n{default_statements}\nEND;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I when clause
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "when_clause",
            GenerationTemplate = "WHEN({value}) {statements}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I call statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "call_statement",
            GenerationTemplate = "CALL {name}({arguments});\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I return statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "return_statement",
            GenerationTemplate = "RETURN({expression});\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I go to statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "goto_statement",
            GenerationTemplate = "GO TO {label};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I stop statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "stop_statement",
            GenerationTemplate = "STOP;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I exception handling
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "exception_declaration",
            GenerationTemplate = "ON {condition} {action};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I multitasking
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "task_declaration",
            GenerationTemplate = "{name}: TASK;\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I signal statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "signal_statement",
            GenerationTemplate = "SIGNAL {condition};\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I wait statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "wait_statement",
            GenerationTemplate = "WAIT({condition});\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I put statement (output)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "put_statement",
            GenerationTemplate = "PUT {destination}({data});\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I get statement (input)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "get_statement",
            GenerationTemplate = "GET {source}({data});\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        // PL/I comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "comment",
            GenerationTemplate = "/* {text} */\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Mixed", ["Indent"] = 4 }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Get cosmetic code formatting options for PL/I output.
    /// </summary>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentSize = 4,
            UseTabs = false,
            BraceStyle = "PL/I",
            IndentBraces = false,
            IndentCaseLabels = false,
            NewLineAfterSemicolon = true,
            SpaceAfterKeywords = true,
            SpaceBeforeBraces = false,
            LanguageSpecificOptions = new Dictionary<string, object>
            {
                ["PLIVersion"] = "PL/I F",
                ["Case"] = "Mixed",
                ["UseSemicolons"] = true,
                ["FreeForm"] = true
            }
        };
    }

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid PL/I code.
    /// </summary>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        _validationVisitor.Reset();
        _validationVisitor.Visit(graph);
        await Task.CompletedTask;
        return _validationVisitor.GetValidationResult();
    }

    /// <summary>
    /// Gets the symbolic analysis visitor for PL/I.
    /// </summary>
    public ISymbolicAnalysisVisitor GetSymbolicAnalysisVisitor()
    {
        return _validationVisitor;
    }
}
