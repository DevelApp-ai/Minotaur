namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Interface for language-specific unparsing and compiler-compiler generation plugins.
/// Implements the runtime pluggable class factory pattern for extensibility.
/// Note: Parsing is handled by DevelApp.StepParser - plugins focus on code generation.
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
    /// Generate compiler-compiler grammar rules for this language.
    /// Used for extending the system with new language backends.
    /// </summary>
    Task<CompilerGeneratorRules> GenerateCompilerRulesAsync();

    /// <summary>
    /// Get language-specific formatting options for code generation
    /// </summary>
    LanguageFormattingOptions GetFormattingOptions();

    /// <summary>
    /// Validate that a cognitive graph can be unparsed to valid code for this language
    /// </summary>
    Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(Core.CognitiveGraphNode graph);
}

/// <summary>
/// Compiler-compiler grammar rules for language generation
/// </summary>
public class CompilerGeneratorRules
{
    public string LanguageId { get; set; } = string.Empty;
    public List<GrammarRule> ProductionRules { get; set; } = new();
    public List<LexicalRule> LexicalRules { get; set; } = new();
    public Dictionary<string, object> LanguageMetadata { get; set; } = new();
}

/// <summary>
/// Grammar production rule for compiler generation
/// </summary>
public class GrammarRule
{
    public string NonTerminal { get; set; } = string.Empty;
    public List<string> Productions { get; set; } = new();
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Lexical rule for token recognition
/// </summary>
public class LexicalRule
{
    public string TokenType { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public int Priority { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Language-specific formatting options for code generation
/// </summary>
public class LanguageFormattingOptions
{
    public string IndentStyle { get; set; } = "spaces"; // "spaces" or "tabs"
    public int IndentSize { get; set; } = 4;
    public string LineEnding { get; set; } = "\n";
    public bool InsertTrailingNewline { get; set; } = true;
    public int MaxLineLength { get; set; } = 120;
    public Dictionary<string, object> LanguageSpecific { get; set; } = new();
}

/// <summary>
/// Result of validating a cognitive graph for unparsing
/// </summary>
public class UnparseValidationResult
{
    public bool CanUnparse { get; set; }
    public List<UnparseValidationError> Errors { get; set; } = new();
    public List<UnparseValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Unparsing validation error
/// </summary>
public class UnparseValidationError
{
    public string Message { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Error";
}

/// <summary>
/// Unparsing validation warning
/// </summary>
public class UnparseValidationWarning
{
    public string Message { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
}