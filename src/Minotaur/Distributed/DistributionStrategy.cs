namespace Minotaur.Distributed;

public record DistributionStrategy
{
    public required ProcessingMode Mode { get; init; }
    public required ConsistencyLevel RequiredConsistency { get; init; }
    public PartitionStrategy PartitionStrategy { get; init; } = PartitionStrategy.Hash;
}

public enum ProcessingMode
{
    Sequential,
    Parallel,
    Distributed
}

public enum ConsistencyLevel
{
    Eventual,
    Strong,
    BoundedStaleness
}

public enum PartitionStrategy
{
    None,
    Hash,
    Range,
    Consistent,
    Custom
}
