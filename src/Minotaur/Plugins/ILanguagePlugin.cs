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

namespace Minotaur.Plugins;

/// <summary>
/// Interface for language-specific unparsing and compiler-compiler generation plugins.
/// Implements the runtime pluggable class factory pattern for extensibility.
/// NOTE: Parsing, grammar, and syntax is handled by DevelApp.StepParser - plugins handle unparsing and compiler-compiler generation.
/// </summary>
public interface ILanguagePlugin
{
    /// <summary>
    /// The unique identifier for this language (e.g., "csharp", "javascript", "python")
    /// </summary>
    string LanguageId { get; }

    /// <summary>
    /// Human-readable display name for the language
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// File extensions supported by this language plugin (e.g., [".cs", ".csx"])
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Generate source code from a cognitive graph using language-specific unparsing rules.
    /// This is the primary function of language plugins.
    /// </summary>
    Task<string> UnparseAsync(Core.CognitiveGraphNode graph);

    /// <summary>
    /// Generate compiler-compiler backend rules for this language.
    /// Used for extending the system with new language backends.
    /// NOTE: This does NOT generate grammar - it generates backend/target-specific generation rules.
    /// </summary>
    Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync();

    /// <summary>
    /// Get cosmetic code formatting options for output (NOT syntax-related)
    /// </summary>
    CodeFormattingOptions GetFormattingOptions();

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid code for this language
    /// </summary>
    Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(Core.CognitiveGraphNode graph);
}

/// <summary>
/// Compiler-compiler backend rules for language generation.
/// NOTE: These are backend/target generation rules, NOT grammar rules - grammar comes from StepParser.
/// </summary>
public class CompilerBackendRules
{
    /// <summary>
    /// Gets or sets the unique identifier for the target language.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of code generation rules for different node types.
    /// </summary>
    public List<CodeGenerationRule> GenerationRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of template rules used for code generation.
    /// </summary>
    public List<TemplateRule> TemplateRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata specific to the backend implementation.
    /// </summary>
    public Dictionary<string, object> BackendMetadata { get; set; } = new();
}

/// <summary>
/// Code generation rule for compiler backend (NOT grammar rule)
/// </summary>
public class CodeGenerationRule
{
    /// <summary>
    /// Gets or sets the type of cognitive graph node this rule applies to.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the template used to generate code for this node type.
    /// </summary>
    public string GenerationTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional hints and metadata for code generation.
    /// </summary>
    public Dictionary<string, object> GenerationHints { get; set; } = new();
}

/// <summary>
/// Template rule for backend code generation
/// </summary>
public class TemplateRule
{
    /// <summary>
    /// Gets or sets the unique name identifier for this template.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the template content with placeholders for substitution.
    /// </summary>
    public string TemplateContent { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of parameters required for this template.
    /// </summary>
    public List<string> RequiredParameters { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata for the template.
    /// </summary>
    public Dictionary<string, object> TemplateMetadata { get; set; } = new();
}

/// <summary>
/// Cosmetic code formatting options for output (NOT syntax-related - syntax comes from StepParser grammar)
/// </summary>
public class CodeFormattingOptions
{
    /// <summary>
    /// Gets or sets the indentation style. Valid values are "spaces" or "tabs".
    /// </summary>
    public string IndentStyle { get; set; } = "spaces"; // "spaces" or "tabs"
    
    /// <summary>
    /// Gets or sets the number of spaces or tabs for indentation.
    /// </summary>
    public int IndentSize { get; set; } = 4;
    
    /// <summary>
    /// Gets or sets the line ending character sequence.
    /// </summary>
    public string LineEnding { get; set; } = "\n";
    
    /// <summary>
    /// Gets or sets a value indicating whether to insert a trailing newline at the end of files.
    /// </summary>
    public bool InsertTrailingNewline { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum line length before wrapping is considered.
    /// </summary>
    public int MaxLineLength { get; set; } = 120;
    
    /// <summary>
    /// Gets or sets additional cosmetic formatting options specific to the language.
    /// </summary>
    public Dictionary<string, object> CosmeticOptions { get; set; } = new();
}

/// <summary>
/// Result of validating a cognitive graph for unparsing
/// </summary>
public class UnparseValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the graph can be successfully unparsed.
    /// </summary>
    public bool CanUnparse { get; set; }
    
    /// <summary>
    /// Gets or sets the list of validation errors that prevent unparsing.
    /// </summary>
    public List<UnparseValidationError> Errors { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of validation warnings that may affect unparsing quality.
    /// </summary>
    public List<UnparseValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Unparsing validation error
/// </summary>
public class UnparseValidationError
{
    /// <summary>
    /// Gets or sets the error message describing the validation failure.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the unique identifier of the node that caused the error.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of the node that caused the error.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the severity level of the error.
    /// </summary>
    public string Severity { get; set; } = "Error";
}

/// <summary>
/// Unparsing validation warning
/// </summary>
public class UnparseValidationWarning
{
    /// <summary>
    /// Gets or sets the warning message describing the potential issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the unique identifier of the node that caused the warning.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of the node that caused the warning.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;
}