using Minotaur.Core;
using Minotaur.Distributed;
using Xunit;

namespace Minotaur.Tests.Distributed;

public class DistributedModelsTests
{
    #region DistributionStrategy Tests

    [Fact]
    public void DistributionStrategy_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var strategy = new DistributionStrategy
        {
            Mode = ProcessingMode.Distributed,
            RequiredConsistency = ConsistencyLevel.Strong
        };

        // Assert
        Assert.Equal(ProcessingMode.Distributed, strategy.Mode);
        Assert.Equal(ConsistencyLevel.Strong, strategy.RequiredConsistency);
        Assert.Equal(PartitionStrategy.Hash, strategy.PartitionStrategy); // Default
    }

    [Fact]
    public void DistributionStrategy_SupportsAllProcessingModes()
    {
        // Arrange & Act
        var sequential = new DistributionStrategy { Mode = ProcessingMode.Sequential, RequiredConsistency = ConsistencyLevel.Strong };
        var parallel = new DistributionStrategy { Mode = ProcessingMode.Parallel, RequiredConsistency = ConsistencyLevel.Eventual };
        var distributed = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.BoundedStaleness };

        // Assert
        Assert.Equal(ProcessingMode.Sequential, sequential.Mode);
        Assert.Equal(ProcessingMode.Parallel, parallel.Mode);
        Assert.Equal(ProcessingMode.Distributed, distributed.Mode);
    }

    [Fact]
    public void DistributionStrategy_SupportsAllConsistencyLevels()
    {
        // Arrange & Act
        var eventual = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Eventual };
        var strong = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Strong };
        var boundedStaleness = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.BoundedStaleness };

        // Assert
        Assert.Equal(ConsistencyLevel.Eventual, eventual.RequiredConsistency);
        Assert.Equal(ConsistencyLevel.Strong, strong.RequiredConsistency);
        Assert.Equal(ConsistencyLevel.BoundedStaleness, boundedStaleness.RequiredConsistency);
    }

    [Fact]
    public void DistributionStrategy_SupportsAllPartitionStrategies()
    {
        // Arrange & Act
        var none = new DistributionStrategy { Mode = ProcessingMode.Sequential, RequiredConsistency = ConsistencyLevel.Strong, PartitionStrategy = PartitionStrategy.None };
        var hash = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Strong, PartitionStrategy = PartitionStrategy.Hash };
        var range = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Strong, PartitionStrategy = PartitionStrategy.Range };
        var consistent = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Strong, PartitionStrategy = PartitionStrategy.Consistent };
        var custom = new DistributionStrategy { Mode = ProcessingMode.Distributed, RequiredConsistency = ConsistencyLevel.Strong, PartitionStrategy = PartitionStrategy.Custom };

        // Assert
        Assert.Equal(PartitionStrategy.None, none.PartitionStrategy);
        Assert.Equal(PartitionStrategy.Hash, hash.PartitionStrategy);
        Assert.Equal(PartitionStrategy.Range, range.PartitionStrategy);
        Assert.Equal(PartitionStrategy.Consistent, consistent.PartitionStrategy);
        Assert.Equal(PartitionStrategy.Custom, custom.PartitionStrategy);
    }

    #endregion

    #region EstimatedResources Tests

    [Fact]
    public void EstimatedResources_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var resources = new EstimatedResources
        {
            EstimatedDuration = TimeSpan.FromMinutes(5),
            RequiredMemoryMB = 512,
            RequiredCPUCores = 2.5
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), resources.EstimatedDuration);
        Assert.Equal(512, resources.RequiredMemoryMB);
        Assert.Equal(2.5, resources.RequiredCPUCores);
    }

    [Fact]
    public void EstimatedResources_CanIncludeGPURequirements()
    {
        // Arrange & Act
        var resources = new EstimatedResources
        {
            EstimatedDuration = TimeSpan.FromHours(1),
            RequiredMemoryMB = 2048,
            RequiredCPUCores = 4.0,
            RequiredGPUs = 2
        };

        // Assert
        Assert.NotNull(resources.RequiredGPUs);
        Assert.Equal(2, resources.RequiredGPUs.Value);
    }

    [Fact]
    public void EstimatedResources_GPURequirementsCanBeNull()
    {
        // Arrange & Act
        var resources = new EstimatedResources
        {
            EstimatedDuration = TimeSpan.FromSeconds(30),
            RequiredMemoryMB = 128,
            RequiredCPUCores = 1.0
        };

        // Assert
        Assert.Null(resources.RequiredGPUs);
    }

    [Fact]
    public void EstimatedResources_CanIncludeAdditionalRequirements()
    {
        // Arrange
        var additionalReqs = new Dictionary<string, object>
        {
            { "DiskSpaceGB", 50 },
            { "NetworkBandwidthMbps", 1000 },
            { "SpecialHardware", "TPU" }
        };

        // Act
        var resources = new EstimatedResources
        {
            EstimatedDuration = TimeSpan.FromMinutes(30),
            RequiredMemoryMB = 1024,
            RequiredCPUCores = 8.0,
            AdditionalRequirements = additionalReqs
        };

        // Assert
        Assert.NotNull(resources.AdditionalRequirements);
        Assert.Equal(3, resources.AdditionalRequirements.Count);
        Assert.Equal(50, resources.AdditionalRequirements["DiskSpaceGB"]);
    }

    #endregion

    #region RemoteEngine Tests

    [Fact]
    public void RemoteEngine_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var engine = new RemoteEngine
        {
            Id = "engine-001",
            Status = EngineStatus.Online,
            Endpoint = "https://engine1.example.com:8080"
        };

        // Assert
        Assert.Equal("engine-001", engine.Id);
        Assert.Equal(EngineStatus.Online, engine.Status);
        Assert.Equal("https://engine1.example.com:8080", engine.Endpoint);
    }

    [Fact]
    public void RemoteEngine_IsAvailable_WhenOnlineAndNotOverloaded()
    {
        // Arrange & Act
        var engine = new RemoteEngine
        {
            Id = "engine-002",
            Status = EngineStatus.Online,
            Endpoint = "https://engine2.example.com:8080"
        };

        // Assert
        Assert.True(engine.IsAvailable);
    }

    [Fact]
    public void RemoteEngine_IsNotAvailable_WhenOffline()
    {
        // Arrange & Act
        var engine = new RemoteEngine
        {
            Id = "engine-003",
            Status = EngineStatus.Offline,
            Endpoint = "https://engine3.example.com:8080"
        };

        // Assert
        Assert.False(engine.IsAvailable);
    }

    [Fact]
    public void RemoteEngine_IsNotAvailable_WhenInMaintenance()
    {
        // Arrange & Act
        var engine = new RemoteEngine
        {
            Id = "engine-004",
            Status = EngineStatus.Maintenance,
            Endpoint = "https://engine4.example.com:8080"
        };

        // Assert
        Assert.False(engine.IsAvailable);
    }

    [Fact]
    public void RemoteEngine_IsNotAvailable_WhenInError()
    {
        // Arrange & Act
        var engine = new RemoteEngine
        {
            Id = "engine-005",
            Status = EngineStatus.Error,
            Endpoint = "https://engine5.example.com:8080"
        };

        // Assert
        Assert.False(engine.IsAvailable);
    }

    [Fact]
    public void RemoteEngine_SupportsAllEngineStatuses()
    {
        // Arrange & Act
        var online = new RemoteEngine { Id = "e1", Status = EngineStatus.Online, Endpoint = "http://e1" };
        var offline = new RemoteEngine { Id = "e2", Status = EngineStatus.Offline, Endpoint = "http://e2" };
        var maintenance = new RemoteEngine { Id = "e3", Status = EngineStatus.Maintenance, Endpoint = "http://e3" };
        var error = new RemoteEngine { Id = "e4", Status = EngineStatus.Error, Endpoint = "http://e4" };

        // Assert
        Assert.Equal(EngineStatus.Online, online.Status);
        Assert.Equal(EngineStatus.Offline, offline.Status);
        Assert.Equal(EngineStatus.Maintenance, maintenance.Status);
        Assert.Equal(EngineStatus.Error, error.Status);
    }

    #endregion

    #region SynchronizationRequest Tests

    [Fact]
    public void SynchronizationRequest_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var state = new GSSMState
        {
            Id = "state-001",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var request = new SynchronizationRequest
        {
            SourceEngineId = "engine-001",
            TargetEngineId = "engine-002",
            State = state,
            Strategy = SynchronizationStrategy.Full
        };

        // Assert
        Assert.Equal("engine-001", request.SourceEngineId);
        Assert.Equal("engine-002", request.TargetEngineId);
        Assert.Equal(state, request.State);
        Assert.Equal(SynchronizationStrategy.Full, request.Strategy);
        Assert.True(request.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void SynchronizationRequest_SupportsAllSyncStrategies()
    {
        // Arrange
        var state = new GSSMState { Id = "state-001", Timestamp = DateTime.UtcNow };

        // Act
        var full = new SynchronizationRequest { SourceEngineId = "s1", TargetEngineId = "t1", State = state, Strategy = SynchronizationStrategy.Full };
        var incremental = new SynchronizationRequest { SourceEngineId = "s2", TargetEngineId = "t2", State = state, Strategy = SynchronizationStrategy.Incremental };
        var selective = new SynchronizationRequest { SourceEngineId = "s3", TargetEngineId = "t3", State = state, Strategy = SynchronizationStrategy.Selective };

        // Assert
        Assert.Equal(SynchronizationStrategy.Full, full.Strategy);
        Assert.Equal(SynchronizationStrategy.Incremental, incremental.Strategy);
        Assert.Equal(SynchronizationStrategy.Selective, selective.Strategy);
    }

    [Fact]
    public void SynchronizationRequest_CanIncludeMetadata()
    {
        // Arrange
        var state = new GSSMState { Id = "state-001", Timestamp = DateTime.UtcNow };
        var metadata = new Dictionary<string, object>
        {
            { "Priority", "High" },
            { "RetryCount", 0 },
            { "Timeout", 30000 }
        };

        // Act
        var request = new SynchronizationRequest
        {
            SourceEngineId = "engine-001",
            TargetEngineId = "engine-002",
            State = state,
            Strategy = SynchronizationStrategy.Incremental,
            Metadata = metadata
        };

        // Assert
        Assert.NotNull(request.Metadata);
        Assert.Equal(3, request.Metadata.Count);
        Assert.Equal("High", request.Metadata["Priority"]);
    }

    #endregion

    #region SynchronizationResult Tests

    [Fact]
    public void SynchronizationResult_CanBeCreated_WithSuccessResult()
    {
        // Arrange & Act
        var result = new SynchronizationResult
        {
            Success = true,
            Message = "Synchronization completed successfully"
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Synchronization completed successfully", result.Message);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void SynchronizationResult_CanBeCreated_WithFailureResult()
    {
        // Arrange & Act
        var result = new SynchronizationResult
        {
            Success = false,
            Message = "Synchronization failed: Network timeout"
        };

        // Assert
        Assert.False(result.Success);
        Assert.Contains("failed", result.Message);
    }

    [Fact]
    public void SynchronizationResult_CanIncludeMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "SyncedItems", 150 },
            { "Duration", 5.5 },
            { "ErrorDetails", "None" }
        };

        // Act
        var result = new SynchronizationResult
        {
            Success = true,
            Message = "Sync complete",
            Metadata = metadata
        };

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(3, result.Metadata.Count);
        Assert.Equal(150, result.Metadata["SyncedItems"]);
    }

    #endregion

    #region TransformationRequirements Tests

    [Fact]
    public void TransformationRequirements_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var capabilities = new List<string> { "Parse", "Transform", "Optimize" };

        // Act
        var requirements = new TransformationRequirements
        {
            RequiredCapabilities = capabilities
        };

        // Assert
        Assert.Equal(3, requirements.RequiredCapabilities.Count);
        Assert.Contains("Parse", requirements.RequiredCapabilities);
        Assert.Equal(0, requirements.Priority); // Default
    }

    [Fact]
    public void TransformationRequirements_CanIncludeEstimatedResources()
    {
        // Arrange
        var capabilities = new List<string> { "Parse", "Analyze" };
        var estimatedResources = new EstimatedResources
        {
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredMemoryMB = 1024,
            RequiredCPUCores = 4.0
        };

        // Act
        var requirements = new TransformationRequirements
        {
            RequiredCapabilities = capabilities,
            EstimatedResources = estimatedResources
        };

        // Assert
        Assert.NotNull(requirements.EstimatedResources);
        Assert.Equal(1024, requirements.EstimatedResources.RequiredMemoryMB);
    }

    [Fact]
    public void TransformationRequirements_CanSetPriority()
    {
        // Arrange
        var capabilities = new List<string> { "HighPriorityTask" };

        // Act
        var requirements = new TransformationRequirements
        {
            RequiredCapabilities = capabilities,
            Priority = 10
        };

        // Assert
        Assert.Equal(10, requirements.Priority);
    }

    [Fact]
    public void TransformationRequirements_CanIncludeTimeout()
    {
        // Arrange
        var capabilities = new List<string> { "TimeSensitiveTask" };

        // Act
        var requirements = new TransformationRequirements
        {
            RequiredCapabilities = capabilities,
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.NotNull(requirements.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(30), requirements.Timeout.Value);
    }

    [Fact]
    public void TransformationRequirements_CanIncludeAdditionalRequirements()
    {
        // Arrange
        var capabilities = new List<string> { "SpecialProcessing" };
        var additional = new Dictionary<string, object>
        {
            { "GPU", true },
            { "MinCoreVersion", "2.0" },
            { "NetworkAccess", true }
        };

        // Act
        var requirements = new TransformationRequirements
        {
            RequiredCapabilities = capabilities,
            AdditionalRequirements = additional
        };

        // Assert
        Assert.NotNull(requirements.AdditionalRequirements);
        Assert.Equal(3, requirements.AdditionalRequirements.Count);
        Assert.Equal(true, requirements.AdditionalRequirements["GPU"]);
    }

    #endregion
}
