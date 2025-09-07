namespace StepParser.Lexer;

/// <summary>
/// Enum representing token splitter types.
/// </summary>
public enum TokenSplitterType
{
    None = 0,
    Whitespace = 1,
    Regex = 2,
    Custom = 3
}

/// <summary>
/// Options for configuring the lexer.
/// </summary>
public class LexerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to return lexer path tokens.
    /// </summary>
    public bool ReturnLexerPathTokens { get; set; } = false;

    /// <summary>
    /// Gets or sets the token splitter type.
    /// </summary>
    public TokenSplitterType TokenSplitterType { get; set; } = TokenSplitterType.None;

    /// <summary>
    /// Gets or sets the regex pattern for token splitting.
    /// </summary>
    public string? RegexTokenSplitter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore case.
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable debugging.
    /// </summary>
    public bool EnableDebugging { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of lexer paths.
    /// </summary>
    public int MaxLexerPaths { get; set; } = 1000;
}