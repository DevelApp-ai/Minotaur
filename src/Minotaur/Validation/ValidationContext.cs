namespace Minotaur.Validation;

/// <summary>
/// Context for validation operations, providing additional information for validators.
/// </summary>
public record ValidationContext
{
    /// <summary>
    /// Unique identifier for this validation context.
    /// </summary>
    public required string ContextId { get; init; }

    /// <summary>
    /// Timestamp when validation was initiated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional properties for extensibility.
    /// Domain-specific systems can store custom data here.
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }
}
