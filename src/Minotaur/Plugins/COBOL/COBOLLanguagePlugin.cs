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

namespace Minotaur.Plugins.COBOL;

/// <summary>
/// COBOL language plugin for Minotaur.
/// Provides full COBOL 85+ support for legacy system modernization.
/// Supports DATA DIVISION, PROCEDURE DIVISION, and modern COBOL features.
/// </summary>
public class COBOLLanguagePlugin : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly COBOLUnparseVisitor _unparseVisitor;
    private readonly COBOLValidationVisitor _validationVisitor;

    /// <summary>
    /// Initializes a new instance of the COBOLLanguagePlugin.
    /// </summary>
    public COBOLLanguagePlugin()
    {
        _unparseVisitor = new COBOLUnparseVisitor();
        _validationVisitor = new COBOLValidationVisitor();
    }

    /// <summary>
    /// Gets the unique identifier for the COBOL language.
    /// </summary>
    public string LanguageId => "cobol";

    /// <summary>
    /// Gets the display name for the COBOL language.
    /// </summary>
    public string DisplayName => "COBOL";

    /// <summary>
    /// Gets the array of file extensions supported by COBOL.
    /// </summary>
    public string[] SupportedExtensions => new[] { ".cob", ".cbl", ".COB", ".CBL" };

    /// <summary>
    /// Converts a cognitive graph representation back to COBOL source code.
    /// </summary>
    /// <param name="graph">The cognitive graph node to unparse.</param>
    /// <returns>A task that represents the asynchronous unparse operation, containing the generated COBOL code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        _unparseVisitor.Reset();
        _unparseVisitor.Visit(graph);
        await Task.CompletedTask;
        return _unparseVisitor.GetGeneratedCode();
    }

    /// <summary>
    /// Generates compiler-compiler backend rules for COBOL code generation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the compiler backend rules for COBOL.</returns>
    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // COBOL identification division
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "identification_division",
            GenerationTemplate = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {name}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL data division
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "data_division",
            GenerationTemplate = "       DATA DIVISION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL working-storage section
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "working_storage_section",
            GenerationTemplate = "       WORKING-STORAGE SECTION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL procedure division
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "procedure_division",
            GenerationTemplate = "       PROCEDURE DIVISION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL paragraph
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "paragraph",
            GenerationTemplate = "{name}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL data description entry
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "data_description_entry",
            GenerationTemplate = "       {level_number} {name} {picture_clause} {value_clause}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL picture clause
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "picture_clause",
            GenerationTemplate = "PIC {picture_string}",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper" }
        });

        // COBOL value clause
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "value_clause",
            GenerationTemplate = "VALUE {value}",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper" }
        });

        // COBOL move statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "move_statement",
            GenerationTemplate = "       MOVE {source} TO {destination}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL display statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "display_statement",
            GenerationTemplate = "       DISPLAY {message}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL accept statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "accept_statement",
            GenerationTemplate = "       ACCEPT {identifier}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL if statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "if_statement",
            GenerationTemplate = "       IF {condition}\n           {then_statements}\n       END-IF.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL perform statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "perform_statement",
            GenerationTemplate = "       PERFORM {procedure_name}\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL call statement
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "call_statement",
            GenerationTemplate = "       CALL {program_name} USING {parameters}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL file section
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "file_section",
            GenerationTemplate = "       FILE SECTION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL file description entry
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "file_description_entry",
            GenerationTemplate = "       FD {name}\n           {clauses}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL select statement (for file handling)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "select_statement",
            GenerationTemplate = "       SELECT {file_name} ASSIGN TO {external_name}\n           {clauses}.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL division header
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "division_header",
            GenerationTemplate = "       {division_name} DIVISION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL section header
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "section_header",
            GenerationTemplate = "       {section_name} SECTION.\n",
            GenerationHints = new Dictionary<string, object> { ["Case"] = "Upper", ["Margin"] = 8 }
        });

        // COBOL comment line
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "comment_line",
            GenerationTemplate = "       * {comment_text}\n",
            GenerationHints = new Dictionary<string, object> { ["Margin"] = 8 }
        });

        // COBOL asterisk comment (full line)
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "asterisk_comment",
            GenerationTemplate = "*{comment_text}\n",
            GenerationHints = new Dictionary<string, object> { ["Margin"] = 0 }
        });

        // COBOL slash comment
        rules.GenerationRules.Add(new CodeGenerationRule
        {
            NodeType = "slash_comment",
            GenerationTemplate = "       /{comment_text}/\n",
            GenerationHints = new Dictionary<string, object> { ["Margin"] = 8 }
        });

        await Task.CompletedTask;
        return rules;
    }

    /// <summary>
    /// Get cosmetic code formatting options for COBOL output.
    /// </summary>
    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentSize = 4,
            UseTabs = false,
            BraceStyle = "COBOL",
            IndentBraces = false,
            IndentCaseLabels = false,
            NewLineAfterSemicolon = false,
            SpaceAfterKeywords = true,
            SpaceBeforeBraces = false,
            LanguageSpecificOptions = new Dictionary<string, object>
            {
                ["COBOLVersion"] = "COBOL85",
                ["Margin"] = 8,
                ["Case"] = "Upper",
                ["UseAreas"] = true
            }
        };
    }

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid COBOL code.
    /// </summary>
    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        _validationVisitor.Reset();
        _validationVisitor.Visit(graph);
        await Task.CompletedTask;
        return _validationVisitor.GetValidationResult();
    }

    /// <summary>
    /// Gets the symbolic analysis visitor for COBOL.
    /// </summary>
    public ISymbolicAnalysisVisitor GetSymbolicAnalysisVisitor()
    {
        return _validationVisitor;
    }
}
