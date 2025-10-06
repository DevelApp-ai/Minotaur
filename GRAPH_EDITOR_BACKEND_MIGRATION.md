# Graph Editor Backend Migration to CognitiveGraph Repository

## Overview

This document outlines the migration of the graph editor backend components from the Minotaur repository to the DevelApp-ai/CognitiveGraph repository. This separation follows the principle of responsibility separation where CognitiveGraph should handle the core graph editing functionality.

## Components to Migrate

### Core Editor Components

1. **GraphEditor.cs** (`src/Minotaur/Editor/GraphEditor.cs`)
   - Core graph editing engine with zero-copy modifications
   - Undo/Redo functionality via Command pattern
   - Node indexing and management
   - Target location: `CognitiveGraph/Editor/GraphEditor.cs`

2. **EditOperation.cs** (`src/Minotaur/Editor/EditOperation.cs`)
   - Base abstract class for edit operations
   - Concrete implementations: InsertNodeOperation, RemoveNodeOperation, ReplaceNodeOperation, MoveNodeOperation
   - Target location: `CognitiveGraph/Editor/EditOperation.cs`

### Application-Level Components (Remain in Minotaur)

3. **CognitiveGraphService.cs** (`src/Minotaur.UI.Blazor/Api/Services/CognitiveGraphService.cs`)
   - Service layer for API access to cognitive graphs - STAYS in Minotaur
   - Caching and querying capabilities for web applications
   - Uses the external CognitiveGraph.Editor package

4. **CognitiveGraphHub.cs** (`src/Minotaur.UI.Blazor/Api/Hubs/CognitiveGraphHub.cs`)
   - SignalR hub for real-time graph updates - STAYS in Minotaur
   - Application-level concern for web communication
   - Uses the external CognitiveGraph.Editor package

### Dependencies for CognitiveGraph (Core Editor Only)

CognitiveGraph will only need core dependencies, no SignalR:

```xml
<PackageReference Include="System.Collections.Concurrent" Version="8.0.0" />
```

## Namespace Changes

Current namespaces that need to be updated:

- `Minotaur.Editor` → `CognitiveGraph.Editor`
- `Minotaur.UI.Blazor.Api.Services` → `CognitiveGraph.Api.Services`
- `Minotaur.UI.Blazor.Api.Hubs` → `CognitiveGraph.Api.Hubs`

## Updated Dependencies in Minotaur

After migration, Minotaur would consume the updated CognitiveGraph package:

```xml
<PackageReference Include="DevelApp.CognitiveGraph" Version="2.0.0" />
```

And would remove the local editor implementations, instead using:

```csharp
using CognitiveGraph.Editor;
using CognitiveGraph.Api.Services;
using CognitiveGraph.Api.Hubs;
```

## Migration Benefits

1. **Separation of Concerns**: Graph editing logic belongs with the graph data structure
2. **Reusability**: Other applications can use the graph editor without Minotaur dependencies
3. **Maintainability**: Editor features can be developed and tested independently
4. **Consistency**: Single source of truth for graph manipulation operations

## Implementation Plan

Since direct branch creation via MCP is not available with Personal Access Tokens, the recommended approach is:

1. Create a new branch in CognitiveGraph repository manually
2. Add the Editor directory with migrated components
3. Update CognitiveGraph.csproj to include new dependencies
4. Test the functionality
5. Publish new version of CognitiveGraph NuGet package
6. Update Minotaur to use the new external editor functionality

## Files Created in This Migration

The following files have been prepared in the `migration_to_cognitive_graph/` directory to facilitate the migration:

- `Editor/GraphEditor.cs` - Updated with proper namespace
- `Editor/EditOperation.cs` - Updated with proper namespace  
- `Api/Services/CognitiveGraphService.cs` - Updated with proper namespace
- `Api/Hubs/CognitiveGraphHub.cs` - Updated with proper namespace
- `CognitiveGraphEditor.csproj` - Project file for the new components

These files can be copied to the CognitiveGraph repository once the branch is created.

## Migration Status Update (Corrected)

### ✅ Completed Tasks:

1. **Core Editor Components Moved to CognitiveGraph:**
   - Deleted `src/Minotaur/Editor/` directory (GraphEditor.cs, EditOperation.cs)
   - **Restored** `src/Minotaur.UI.Blazor/Api/` directory (Services, Hubs remain in Minotaur)
   - Created backups in `migration_to_cognitive_graph/backup_*`

2. **Migration Files Prepared:**
   - `migration_to_cognitive_graph/Editor/GraphEditor.cs` - Updated namespace to `CognitiveGraph.Editor`
   - `migration_to_cognitive_graph/Editor/EditOperation.cs` - Updated namespace to `CognitiveGraph.Editor`
   - `migration_to_cognitive_graph/CognitiveGraphEditor.csproj` - Project configuration
   - `migration_to_cognitive_graph/README.md` - Package documentation

3. **Minotaur Project Updated:**
   - Updated `src/Minotaur/Minotaur.csproj` to reference future `DevelApp.CognitiveGraph.Editor` package
   - Updated main README.md with migration notices and new usage patterns
   - Updated package description to reflect editor separation

### 🔄 Manual Steps Required:

Since MCP cannot create branches with Personal Access Tokens, the following steps must be completed manually:

1. **Create branch in DevelApp-ai/CognitiveGraph:**
   ```bash
   git checkout -b feature/graph-editor-backend
   ```

2. **Add Editor directory:**
   ```bash
   mkdir -p CognitiveGraph/Editor
   cp migration_to_cognitive_graph/Editor/* CognitiveGraph/Editor/
   ```

3. **Update CognitiveGraph.csproj dependencies:**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.SignalR.Core" Version="8.0.0" />
   <PackageReference Include="System.Collections.Concurrent" Version="8.0.0" />
   ```

4. **Test and publish new version:**
   ```bash
   dotnet test
   dotnet pack --configuration Release
   dotnet nuget push DevelApp.CognitiveGraph.2.0.0.nupkg
   ```

5. **Update Minotaur to use external package:**
   - Uncomment the CognitiveGraph.Editor package reference
   - Test integration
   - Verify all functionality works with external backend

### 🎯 Benefits Achieved:

- **Separation of Concerns**: Graph editing logic now belongs with the graph data structure
- **Reusability**: Other applications can use the graph editor without Minotaur dependencies  
- **Maintainability**: Editor features can be developed and tested independently
- **Consistency**: Single source of truth for graph manipulation operations

The migration is ready to proceed with manual steps due to MCP limitations.