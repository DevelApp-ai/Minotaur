namespace Minotaur.Distributed;

public class SynchronizationResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
}
