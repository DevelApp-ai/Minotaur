using Minotaur.Core;

namespace Minotaur.Distributed;

public class SynchronizationRequest
{
    public required string SourceEngineId { get; init; }
    public required string TargetEngineId { get; init; }
    public required GSSMState State { get; init; }
    public required SynchronizationStrategy Strategy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
}

public enum SynchronizationStrategy
{
    Full,
    Incremental,
    Selective
}
