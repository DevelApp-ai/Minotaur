using StepParser.Core;
using StepParser.Memory;

namespace StepParser.Core;

/// <summary>
/// Represents a parser path for tracking parsing state.
/// </summary>
public class ParserPath : IPoolableObject
{
    private int _parserPathId;
    private int _lexerPathId;
    private int _position;
    private readonly List<Production> _activeProductions;
    private readonly List<ProductionMatch> _activeMatches;
    private bool _isInUse;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParserPath"/> class.
    /// </summary>
    /// <param name="parserPathId">The parser path ID.</param>
    /// <param name="lexerPathId">The lexer path ID.</param>
    /// <param name="position">The position in the input.</param>
    public ParserPath(int parserPathId, int lexerPathId, int position)
    {
        _parserPathId = parserPathId;
        _lexerPathId = lexerPathId;
        _position = position;
        _activeProductions = new List<Production>();
        _activeMatches = new List<ProductionMatch>();
        _isInUse = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParserPath"/> class with default values.
    /// </summary>
    public ParserPath() : this(0, 0, 0)
    {
    }

    /// <summary>
    /// Gets or sets the parser path ID.
    /// </summary>
    public int ParserPathId
    {
        get => _parserPathId;
        set => _parserPathId = value;
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
    /// Gets or sets the position in the input.
    /// </summary>
    public int Position
    {
        get => _position;
        set => _position = value;
    }

    /// <summary>
    /// Gets the active productions.
    /// </summary>
    public List<Production> ActiveProductions => _activeProductions;

    /// <summary>
    /// Gets the active matches.
    /// </summary>
    public List<ProductionMatch> ActiveMatches => _activeMatches;

    /// <inheritdoc/>
    public bool IsInUse
    {
        get => _isInUse;
        set => _isInUse = value;
    }

    /// <summary>
    /// Adds an active production.
    /// </summary>
    /// <param name="production">The production to add.</param>
    public void AddActiveProduction(Production production)
    {
        _activeProductions.Add(production);
    }

    /// <summary>
    /// Adds an active match.
    /// </summary>
    /// <param name="match">The match to add.</param>
    public void AddActiveMatch(ProductionMatch match)
    {
        _activeMatches.Add(match);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _parserPathId = 0;
        _lexerPathId = 0;
        _position = 0;
        _activeProductions.Clear();
        _activeMatches.Clear();
        _isInUse = false;
    }

    /// <summary>
    /// Returns a string representation of this parser path.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"ParserPath(Id={_parserPathId}, LexerId={_lexerPathId}, Pos={_position}, Productions={_activeProductions.Count}, Matches={_activeMatches.Count})";
    }
}