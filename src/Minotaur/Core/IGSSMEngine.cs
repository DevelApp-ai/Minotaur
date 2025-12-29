using Minotaur.Distributed;
using Minotaur.Validation;

namespace Minotaur.Core;

public interface IGSSMEngine
{
    Task<TransformationResult> ProcessTransformationAsync(
        GSSMState currentState,
        GSSMInput input,
        TransformationContext context,
        CancellationToken cancellationToken = default
    );

    Task<TransformationResult> ProcessTransformationAsync(
        GSSMState currentState,
        GSSMInput input,
        TransformationRequirements requirements,
        TransformationContext context,
        CancellationToken cancellationToken = default
    );

    Task<EngineCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}

public interface IGSSMEngineExtended : IGSSMEngine
{
    Task<ValidationResult> ValidateAndIntegrateAsync(
        GSSMState currentState,
        TransformationResult transformationResult,
        ValidationContext context,
        CancellationToken cancellationToken = default
    );
}
