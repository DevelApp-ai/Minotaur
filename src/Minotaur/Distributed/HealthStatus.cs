namespace Minotaur.Distributed;

/// <summary>
/// Represents the health status of a distributed engine.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Engine is functioning normally with no issues.
    /// </summary>
    Healthy,

    /// <summary>
    /// Engine is operational but experiencing performance issues or minor problems.
    /// </summary>
    Degraded,

    /// <summary>
    /// Engine is not functioning properly and may not be able to process requests.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Health status cannot be determined, typically due to connectivity or monitoring issues.
    /// </summary>
    Unknown
}
