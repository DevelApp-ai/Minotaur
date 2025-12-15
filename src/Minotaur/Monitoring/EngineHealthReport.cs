namespace Minotaur.Monitoring;

public record EngineHealthReport
{
    public required string EngineId { get; init; }
    public required bool IsHealthy { get; init; }
    public required ResourceUtilization ResourceUtilization { get; init; }
    public int AvailableEngines { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? AdditionalMetrics { get; init; }
}
