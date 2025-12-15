using Minotaur.Core;
using Minotaur.Monitoring;

namespace Minotaur.Learning;

public class LearningData
{
    public required string SourceEngineId { get; init; }
    public required GSSMState StateSnapshot { get; init; }
    public required List<TransformationResult> SuccessfulTransformations { get; init; }
    public required PerformanceMetrics Performance { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class LearningQuery
{
    public string? EngineId { get; init; }
    public DateTime? FromTimestamp { get; init; }
    public DateTime? ToTimestamp { get; init; }
    public List<string>? TransformationTypes { get; init; }
}

public class LearningQueryResult
{
    public required List<LearningData> Results { get; init; }
    public required int TotalCount { get; init; }
    public bool HasMore { get; init; }
}
