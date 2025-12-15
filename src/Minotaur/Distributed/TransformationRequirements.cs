namespace Minotaur.Distributed;

public class TransformationRequirements
{
    public required List<string> RequiredCapabilities { get; init; }
    public EstimatedResources? EstimatedResources { get; init; }
    public int Priority { get; init; } = 0;
    public TimeSpan? Timeout { get; init; }
    public Dictionary<string, object>? AdditionalRequirements { get; init; }
}
