using Minotaur.Distributed;
using Minotaur.Learning;

namespace Minotaur.Core;

public interface IRemoteGSSMCoordinator
{
    Task<SynchronizationResult> SynchronizeStateAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken = default
    );

    Task ShareLearningAsync(
        LearningData learningData,
        CancellationToken cancellationToken = default
    );

    Task<LearningQueryResult> QueryLearningAsync(
        LearningQuery query,
        CancellationToken cancellationToken = default
    );

    // Health monitoring
    Task<EngineHealthInfo> GetEngineHealthAsync(
        string engineId,
        CancellationToken cancellationToken = default
    );

    // Capability discovery
    Task<EngineCapabilities> GetCapabilitiesAsync(
        string engineId,
        CancellationToken cancellationToken = default
    );

    // Distributed task processing
    Task<DistributedTaskResult> ProcessDistributedTaskAsync(
        DistributedTask task,
        DistributedProcessingOptions options,
        CancellationToken cancellationToken = default
    );
}
