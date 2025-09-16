namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Interface for language-specific unparsing plugins.
/// Implements the runtime pluggable class factory pattern for extensibility.
/// NOTE: All parsing, grammar, and syntax is handled by DevelApp.StepParser - plugins only generate code.
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
    /// This is the ONLY function of language plugins - all grammar/syntax comes from StepParser.
    /// </summary>
    Task<string> UnparseAsync(Core.CognitiveGraphNode graph);

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
/// Cosmetic code formatting options for output (NOT syntax-related - syntax comes from StepParser grammar)
/// </summary>
public class CodeFormattingOptions
{
    public string IndentStyle { get; set; } = "spaces"; // "spaces" or "tabs"
    public int IndentSize { get; set; } = 4;
    public string LineEnding { get; set; } = "\n";
    public bool InsertTrailingNewline { get; set; } = true;
    public int MaxLineLength { get; set; } = 120;
    public Dictionary<string, object> CosmeticOptions { get; set; } = new();
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