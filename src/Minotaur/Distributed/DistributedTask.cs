namespace Minotaur.Distributed;

public record DistributedTask
{
    public required string TaskId { get; init; }
    public required string TaskType { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}
