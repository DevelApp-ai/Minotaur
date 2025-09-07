namespace StepParser.Core;

/// <summary>
/// Represents a match result for a production.
/// </summary>
public class ProductionMatch
{
    private readonly Production _production;
    private readonly string _value;
    private readonly int _startPosition;
    private readonly int _endPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionMatch"/> class.
    /// </summary>
    /// <param name="production">The production that matched.</param>
    /// <param name="value">The matched value.</param>
    /// <param name="startPosition">The start position of the match.</param>
    /// <param name="endPosition">The end position of the match.</param>
    public ProductionMatch(Production production, string value, int startPosition, int endPosition)
    {
        _production = production;
        _value = value;
        _startPosition = startPosition;
        _endPosition = endPosition;
    }

    /// <summary>
    /// Gets the production that matched.
    /// </summary>
    public Production Production => _production;

    /// <summary>
    /// Gets the matched value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the start position of the match.
    /// </summary>
    public int StartPosition => _startPosition;

    /// <summary>
    /// Gets the end position of the match.
    /// </summary>
    public int EndPosition => _endPosition;

    /// <summary>
    /// Gets the length of the match.
    /// </summary>
    public int Length => _endPosition - _startPosition;

    /// <summary>
    /// Returns a string representation of this production match.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"ProductionMatch({_production.Name}, '{_value}', {_startPosition}-{_endPosition})";
    }
}