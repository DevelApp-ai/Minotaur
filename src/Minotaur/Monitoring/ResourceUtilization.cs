namespace Minotaur.Monitoring;

public class ResourceUtilization
{
    public required double CPUUsagePercent { get; init; }
    public required long MemoryUsedBytes { get; init; }
    public required long MemoryTotalBytes { get; init; }
    public double? GPUUsagePercent { get; init; }
    public Dictionary<string, double>? CustomMetrics { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
