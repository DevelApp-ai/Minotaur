namespace Minotaur.Distributed;

public record DistributedTaskResult
{
    public required string TaskId { get; init; }
    public required bool Success { get; init; }
    public Dictionary<string, object>? Results { get; init; }
    public string? ErrorMessage { get; init; }
}
