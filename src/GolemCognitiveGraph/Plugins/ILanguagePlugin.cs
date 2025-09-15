namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Interface for language-specific parsing and unparsing plugins.
/// Implements the runtime pluggable class factory pattern for extensibility.
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
    /// Parse source code into tokens using language-specific lexing rules
    /// </summary>
    Task<IEnumerable<LanguageToken>> TokenizeAsync(string sourceCode);

    /// <summary>
    /// Parse source code into a cognitive graph using language-specific grammar
    /// </summary>
    Task<Core.CognitiveGraphNode> ParseAsync(string sourceCode);

    /// <summary>
    /// Generate source code from a cognitive graph using language-specific unparsing rules
    /// </summary>
    Task<string> UnparseAsync(Core.CognitiveGraphNode graph);

    /// <summary>
    /// Validate source code syntax without full parsing
    /// </summary>
    Task<ValidationResult> ValidateAsync(string sourceCode);
}

/// <summary>
/// Represents a language token with metadata
/// </summary>
public class LanguageToken
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of syntax validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Validation error information
/// </summary>
public class ValidationError
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Severity { get; set; } = "Error";
}

/// <summary>
/// Validation warning information
/// </summary>
public class ValidationWarning
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}