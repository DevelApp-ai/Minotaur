using StepParser.Core;

namespace StepParser.Lexer;

/// <summary>
/// Represents a match result for a terminal.
/// </summary>
public class TerminalMatch
{
    private readonly Terminal _terminal;
    private readonly string _value;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalMatch"/> class.
    /// </summary>
    /// <param name="terminal">The terminal that matched.</param>
    /// <param name="value">The matched value.</param>
    /// <param name="length">The length of the match.</param>
    public TerminalMatch(Terminal terminal, string value, int length)
    {
        _terminal = terminal;
        _value = value;
        _length = length;
    }

    /// <summary>
    /// Gets the terminal that matched.
    /// </summary>
    public Terminal Terminal => _terminal;

    /// <summary>
    /// Gets the matched value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the length of the match.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Returns a string representation of this terminal match.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"TerminalMatch({_terminal.Name}, '{_value}', {_length})";
    }
}