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
}
