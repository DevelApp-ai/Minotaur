namespace Minotaur.Distributed;

public class EstimatedResources
{
    public required TimeSpan EstimatedDuration { get; init; }
    public required int RequiredMemoryMB { get; init; }
    public required double RequiredCPUCores { get; init; }
    public int? RequiredGPUs { get; init; }
    public Dictionary<string, object>? AdditionalRequirements { get; init; }
}
