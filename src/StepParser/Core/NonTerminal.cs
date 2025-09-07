namespace StepParser.Core;

/// <summary>
/// Represents a non-terminal symbol in the grammar.
/// </summary>
public class NonTerminal : IProductionPart
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="NonTerminal"/> class.
    /// </summary>
    /// <param name="name">The name of the non-terminal.</param>
    public NonTerminal(string name)
    {
        _name = name;
    }

    /// <inheritdoc/>
    public ProductionPartType Type => ProductionPartType.NonTerminal;

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    public bool IsOrderImportant => false;

    /// <summary>
    /// Returns a string representation of this non-terminal.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"NonTerminal({_name})";
    }
}