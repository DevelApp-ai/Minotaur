namespace Minotaur.Distributed;

public record EngineHealthInfo
{
    public required string EngineId { get; init; }
    public required HealthStatus Status { get; init; }
    public DateTime LastCheckTime { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}
