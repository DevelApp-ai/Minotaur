using System.Text.RegularExpressions;

namespace StepParser.Core;

/// <summary>
/// Represents a terminal symbol in the grammar.
/// </summary>
public class Terminal : IProductionPart
{
    private readonly string _name;
    private readonly bool _orderImportant;
    private readonly int _terminalOrder;
    private readonly Regex _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="Terminal"/> class.
    /// </summary>
    /// <param name="name">The name of the terminal.</param>
    /// <param name="pattern">The regular expression pattern for matching.</param>
    /// <param name="orderImportant">Whether the order of this terminal is important.</param>
    /// <param name="terminalOrder">The order of this terminal.</param>
    public Terminal(string name, string pattern, bool orderImportant = false, int terminalOrder = 0)
    {
        _name = name;
        _orderImportant = orderImportant;
        _terminalOrder = terminalOrder;
        _pattern = new Regex($"^({pattern})", RegexOptions.Compiled);
    }

    /// <inheritdoc/>
    public ProductionPartType Type => ProductionPartType.Terminal;

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    public bool IsOrderImportant => _orderImportant;

    /// <summary>
    /// Gets the order of this terminal.
    /// </summary>
    public int TerminalOrder => _terminalOrder;

    /// <summary>
    /// Gets the pattern for this terminal.
    /// </summary>
    public Regex Pattern => _pattern;

    /// <summary>
    /// Matches the input against this terminal's pattern.
    /// </summary>
    /// <param name="input">The input string to match.</param>
    /// <returns>The match result or null if no match.</returns>
    public Match? MatchInput(string input)
    {
        var match = _pattern.Match(input);
        return match.Success ? match : null;
    }

    /// <summary>
    /// Returns a string representation of this terminal.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"Terminal({_name})";
    }
}