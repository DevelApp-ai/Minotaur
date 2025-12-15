namespace Minotaur.Analysis;

public class QualityMetrics
{
    public required double Accuracy { get; init; }
    public required double Precision { get; init; }
    public required double Recall { get; init; }
    public required double F1Score { get; init; }
    public double? Confidence { get; init; }
    public Dictionary<string, double>? CustomMetrics { get; init; }
}
