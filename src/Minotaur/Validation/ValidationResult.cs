namespace Minotaur.Validation;

public record ValidationResult
{
    public required string Message { get; init; }
    public required ValidationSeverity Severity { get; init; }
    public SourcePosition? Location { get; init; }
    public string? Code { get; init; }
    public Dictionary<string, string>? Context { get; init; }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public record SourcePosition
{
    public required int Line { get; init; }
    public required int Column { get; init; }
    public string? File { get; init; }
}
