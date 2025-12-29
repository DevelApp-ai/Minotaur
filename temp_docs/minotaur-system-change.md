# Minimal Minotaur Changes Required for Golem

## Overview

This document outlines the **minimal changes required in Minotaur** for Golem to run without diminished power. Most Golem requirements will be implemented on the Golem side using property systems and domain-specific interfaces.

## Required Minotaur Changes

Only two changes are needed in Minotaur to enable Golem's full functionality:

### 1. IRemoteGSSMCoordinator Interface Enhancement

**Problem**: Minotaur's `IRemoteGSSMCoordinator` interface lacks essential operational methods needed for distributed engine management.

**Current State**: The interface exists but is missing critical methods for:
- Health monitoring of distributed engines
- Capability discovery across remote engines
- Distributed task processing

**Required Changes**: Add the following methods to `Minotaur.Core.IRemoteGSSMCoordinator`:

```csharp
public interface IRemoteGSSMCoordinator
{
    // Existing methods...
    
    // NEW: Health monitoring
    Task<EngineHealthInfo> GetEngineHealthAsync(
        string engineId, 
        CancellationToken cancellationToken = default
    );
    
    // NEW: Capability discovery
    Task<EngineCapabilities> GetCapabilitiesAsync(
        string engineId, 
        CancellationToken cancellationToken = default
    );
    
    // NEW: Distributed task processing
    Task<DistributedTaskResult> ProcessDistributedTaskAsync(
        DistributedTask task,
        DistributedProcessingOptions options,
        CancellationToken cancellationToken = default
    );
}

// Supporting types
public record EngineHealthInfo
{
    public required string EngineId { get; init; }
    public required HealthStatus Status { get; init; }
    public DateTime LastCheckTime { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public record DistributedTask
{
    public required string TaskId { get; init; }
    public required string TaskType { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

public record DistributedTaskResult
{
    public required string TaskId { get; init; }
    public required bool Success { get; init; }
    public Dictionary<string, object>? Results { get; init; }
    public string? ErrorMessage { get; init; }
}

public record DistributedProcessingOptions
{
    public int MaxParallelism { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object>? AdditionalOptions { get; init; }
}
```

**Rationale**: 
- These are general-purpose distributed coordination operations, not Golem-specific
- Any system using Minotaur for distributed processing needs health monitoring and capability discovery
- Belongs in Minotaur's core distributed coordination infrastructure

**Priority**: High - Golem's distributed operations depend on these methods

### 2. Validation Context Type

**Problem**: Code references `Minotaur.Validation.ValidationContext` but this type doesn't exist in Minotaur.

**Current State**: Golem creates validation contexts inline, but there's no standard type in Minotaur.

**Required Changes**: Add `ValidationContext` type to `Minotaur.Validation` namespace:

```csharp
namespace Minotaur.Validation
{
    /// <summary>
    /// Context for validation operations, providing additional information for validators.
    /// </summary>
    public record ValidationContext
    {
        /// <summary>
        /// Unique identifier for this validation context.
        /// </summary>
        public required string ContextId { get; init; }
        
        /// <summary>
        /// Timestamp when validation was initiated.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Additional properties for extensibility.
        /// Domain-specific systems can store custom data here.
        /// </summary>
        public Dictionary<string, object>? Properties { get; init; }
    }
}
```

**Rationale**:
- Validation is a core state machine concern (ensuring state consistency and integrity)
- While domain-specific business rules belong in Golem, the infrastructure for validation belongs in Minotaur
- Provides a minimal, extensible type that any Minotaur-based system can use
- The `Properties` dictionary allows Golem and other systems to add domain-specific context without modifying the base type

**Priority**: Medium - Workarounds exist but standardization improves consistency

## What Stays in Golem

The following will be implemented on the Golem side and do NOT require Minotaur changes:

### 1. Domain-Specific Engine Methods
- `InitializeStateAsync`, `ValidateStateAsync`, `CreateTransformationPlanAsync`, etc.
- **Implementation**: Create `IGolemGSSMEngine` interface in Golem.Core
- **Rationale**: These are Golem-specific workflow operations, not general state machine operations

### 2. Rich State Properties  
- StateId, SourceGraph, IntentHierarchy, StateType, Metadata, etc.
- **Implementation**: Use Minotaur's existing `StateData` property with `GolemStateDataKeys` constants
- **Rationale**: Keeps Minotaur minimal; Golem stores domain data in properties

### 3. Input Type Information
- GSSMInputType, Priority, SourceComponent, etc.
- **Implementation**: Use Minotaur's existing `Metadata` property with `GolemInputMetadataKeys` constants
- **Rationale**: Same property system approach as state data

## Implementation Guide

### For Minotaur Team

**Change #1: IRemoteGSSMCoordinator**
1. Add the three new methods to the interface
2. Define the supporting types (EngineHealthInfo, HealthStatus, DistributedTask, etc.)
3. Update any existing implementations to provide these methods
4. Add basic documentation

**Change #2: ValidationContext**
1. Create `Minotaur.Validation` namespace if it doesn't exist
2. Add the `ValidationContext` record type
3. Update relevant validation interfaces to accept `ValidationContext` parameters

### For Golem Team

**After Minotaur changes are available:**
1. Remove inline `ValidationContext` definitions in Golem
2. Reference `Minotaur.Validation.ValidationContext` directly
3. Store Golem-specific validation data in the `Properties` dictionary

**Golem-side implementations:**
1. Define `GolemStateDataKeys` and `GolemInputMetadataKeys` constants
2. Create `IGolemGSSMEngine` interface with domain-specific methods
3. Update code to use property system for state and input data access
4. Create helper extension methods for convenient property access

## Benefits

### Minimal Minotaur Impact
- Only 2 changes required
- Both are general-purpose infrastructure, not Golem-specific
- No breaking changes to existing Minotaur functionality
- Other projects benefit from these additions

### Golem Flexibility
- Full domain-specific richness via property systems
- Clean separation from Minotaur's minimal core
- No need to wait for Minotaur changes for most features
- Can evolve independently

### Maintainability
- Clear boundaries: Minotaur = infrastructure, Golem = domain
- Minimal coupling between systems
- Each team owns their concerns

## Timeline

1. **Immediate (Golem)**: Implement property system approach with existing Minotaur
2. **Short-term (Minotaur)**: Add IRemoteGSSMCoordinator methods and ValidationContext
3. **Short-term (Golem)**: Update to use new Minotaur types once available
4. **Ongoing**: Both systems evolve independently with clear boundaries
