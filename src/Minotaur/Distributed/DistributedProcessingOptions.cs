namespace Minotaur.Distributed;

public record DistributedProcessingOptions
{
    public int MaxParallelism { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object>? AdditionalOptions { get; init; }
}
