namespace Minotaur.Monitoring;

public record PerformanceMetrics
{
    public required double AverageResponseTime { get; init; }
    public required double ThroughputPerSecond { get; init; }
    public TimeSpan AverageProcessingTime { get; init; }
    public double ThroughputPerMinute => ThroughputPerSecond * 60;
    public Dictionary<string, double>? CustomMetrics { get; init; }
}
