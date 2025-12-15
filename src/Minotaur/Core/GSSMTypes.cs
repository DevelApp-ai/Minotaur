namespace Minotaur.Core;

public record GSSMState
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public Dictionary<string, object>? StateData { get; init; }
    public List<string>? ActiveRules { get; init; }
}

public record GSSMInput
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record TransformationResult
{
    public required bool Success { get; init; }
    public required GSSMState ResultState { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}

public record TransformationContext
{
    public required string ContextId { get; init; }
    public Dictionary<string, object>? Properties { get; init; }
}

public enum TransformationType
{
    Parse,
    Transform,
    Validate,
    Optimize
}

public enum TransitionType
{
    StateChange,
    Input,
    Output,
    Transformation
}

public record EngineCapabilities
{
    public required List<string> SupportedLanguages { get; init; }
    public required List<TransformationType> SupportedTransformations { get; init; }
    public int MaxConcurrentTransformations { get; init; } = 10;
    public List<string> Features { get; init; } = new();
    public Dictionary<string, object> CustomCapabilities { get; init; } = new();
}
