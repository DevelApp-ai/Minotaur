namespace StepParser.Core;

/// <summary>
/// Represents a token produced by the lexer.
/// </summary>
public class Token
{
    /// <summary>
    /// Constant value for externally removed lexer path.
    /// </summary>
    public const string LEXERPATH_EXTERN_REMOVED = "EXTERN_REMOVED";

    private readonly int _lexerPathId;
    private readonly Terminal _terminal;
    private readonly string _value;
    private readonly int _lineNumber;
    private readonly int _characterNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="lexerPathId">The lexer path ID.</param>
    /// <param name="terminal">The terminal that matched.</param>
    /// <param name="value">The matched value.</param>
    /// <param name="lineNumber">The line number in the source.</param>
    /// <param name="characterNumber">The character number in the source.</param>
    public Token(int lexerPathId, Terminal terminal, string value, int lineNumber, int characterNumber)
    {
        _lexerPathId = lexerPathId;
        _terminal = terminal;
        _value = value;
        _lineNumber = lineNumber;
        _characterNumber = characterNumber;
    }

    /// <summary>
    /// Gets the lexer path ID.
    /// </summary>
    public int LexerPathId => _lexerPathId;

    /// <summary>
    /// Gets the terminal that matched.
    /// </summary>
    public Terminal Terminal => _terminal;

    /// <summary>
    /// Gets the matched value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the line number in the source.
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Gets the character number in the source.
    /// </summary>
    public int CharacterNumber => _characterNumber;

    /// <summary>
    /// Returns a string representation of this token.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"Token({_terminal.Name}, '{_value}', {_lineNumber}:{_characterNumber})";
    }
}