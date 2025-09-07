using StepParser.Memory;

namespace StepParser.Lexer;

/// <summary>
/// Represents a lexer path for tracking parsing state.
/// </summary>
public class LexerPath : IPoolableObject
{
    /// <summary>
    /// Constant indicating the lexer path ID is not set.
    /// </summary>
    public const int NOTSET = -1;

    private int _lexerPathId;
    private int _activeLineNumber;
    private int _activeCharacterNumber;
    private bool _isInUse;

    /// <summary>
    /// Initializes a new instance of the <see cref="LexerPath"/> class.
    /// </summary>
    public LexerPath()
    {
        Reset();
    }

    /// <summary>
    /// Gets or sets the lexer path ID.
    /// </summary>
    public int LexerPathId
    {
        get => _lexerPathId;
        set => _lexerPathId = value;
    }

    /// <summary>
    /// Gets or sets the active line number.
    /// </summary>
    public int ActiveLineNumber
    {
        get => _activeLineNumber;
        set => _activeLineNumber = value;
    }

    /// <summary>
    /// Gets or sets the active character number.
    /// </summary>
    public int ActiveCharacterNumber
    {
        get => _activeCharacterNumber;
        set => _activeCharacterNumber = value;
    }

    /// <inheritdoc/>
    public bool IsInUse
    {
        get => _isInUse;
        set => _isInUse = value;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _lexerPathId = NOTSET;
        _activeLineNumber = 0;
        _activeCharacterNumber = 0;
        _isInUse = false;
    }

    /// <summary>
    /// Returns a string representation of this lexer path.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"LexerPath(Id={_lexerPathId}, Line={_activeLineNumber}, Char={_activeCharacterNumber})";
    }
}