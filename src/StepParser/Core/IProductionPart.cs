namespace StepParser.Core;

/// <summary>
/// Represents the type of a production part.
/// </summary>
public enum ProductionPartType
{
    Terminal,
    NonTerminal,
    Optional,
    ZeroOrMore,
    OneOrMore,
    Group
}

/// <summary>
/// Interface for production parts in a grammar rule.
/// </summary>
public interface IProductionPart
{
    /// <summary>
    /// Gets the type of this production part.
    /// </summary>
    ProductionPartType Type { get; }

    /// <summary>
    /// Gets the name of this production part.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether order is important for this part.
    /// </summary>
    bool IsOrderImportant { get; }
}