# Minotaur Architecture Implementation

## Overview

This document describes the correct architecture implementation for Minotaur, focusing on:
- **CognitiveGraph Visualization** with full PackedNode ambiguity support
- **Grammar Editor** UI components
- **Plugin Management** system

## Architecture Components

### 1. CognitiveGraph Visualization System

#### Core Models (`src/Minotaur.Core/Models/Visualization/`)

- **`GraphData.cs`** - Contains all data models for visualization:
  - `GraphData` - Root container for graph visualization
  - `GraphNode` - Represents a SymbolNode with visualization properties
  - `GraphEdge` - Represents an edge between nodes with PackedNode info
  - `CodeLocation` & `Position` - Source code location tracking
  - `NodeAmbiguityInfo` - Information about ambiguous SymbolNodes
  - `PackedNodeInfo` - Information about each PackedNode alternative
  - `CognitiveGraphVisualization` - Complete visualization data with ambiguity support
  - `VisualizationMode` - Enum for different visualization modes

#### Services (`src/Minotaur.Core/Services/Visualization/`)

- **`ICognitiveGraphVisualizer.cs`** - Interface for visualization service:
  - `GenerateVisualization()` - Creates visualization from CognitiveGraph
  - `GetAmbiguityPoints()` - Identifies all ambiguous nodes
  - `GetAllInterpretationPaths()` - Returns all possible parse paths
  - `GenerateSingleInterpretation()` - Shows only selected PackedNode path
  - `CognitiveGraphVisualizer` - Implementation that directly traverses SymbolNode/PackedNode structures

#### Controllers (`src/Minotaur.Core/Controllers/Visualization/`)

- **`VisualizationController.cs`** - REST API endpoints:
  - `POST /api/visualization` - Full visualization with all PackedNode alternatives
  - `POST /api/visualization/ambiguities` - List all ambiguity points
  - `POST /api/visualization/interpretations` - List all interpretation paths
  - `POST /api/visualization/select-interpretation` - Select specific PackedNode path

#### Blazor Components (`src/Minotaur.UI.Blazor/Components/Shared/`)

- **`CognitiveGraphVisualizer.razor`** - Main visualization component:
  - Displays graph with D3.js
  - Shows all SymbolNodes and PackedNode edges
  - Highlights ambiguous nodes
  - Allows interactive selection of PackedNode paths
  - Shows ambiguity details panel

#### JavaScript (`src/Minotaur.UI.Blazor/wwwroot/js/`)

- **`cognitiveGraphVisualization.js`** - D3.js visualization:
  - Force-directed graph layout
  - Interactive node/edge manipulation
  - Ambiguity menu for PackedNode selection
  - Zoom and pan controls
  - Toggle for showing/hiding alternative edges

#### Tests (`src/Minotaur.Tests/Visualization/`)

- **`CognitiveGraphVisualizerTests.cs`** - Unit tests for visualizer service
- **`VisualizationControllerTests.cs`** - Unit tests for controller
- **`GraphDataModelTests.cs`** - Unit tests for data models

### 2. Grammar Editor System

#### Core Models (`src/Minotaur.Core/Models/Grammar/`)

- **`TokenDefinition.cs`** - Defines a token:
  - Name, Pattern (regex), Description
  - IsTerminal, IsSkippable, Priority
  - Category, Color, Properties

- **`RuleDefinition.cs`** - Defines a production rule:
  - Name, Pattern (StepParser syntax)
  - IsTerminal, IsAmbiguous, Priority
  - Category, ReturnType, ActionCode
  - Parameters (list of RuleParameter)
  - Properties

- **`GrammarDefinition.cs`** - Complete grammar definition:
  - Name, Description, Version, Language
  - Tokens (list of TokenDefinition)
  - Rules (list of RuleDefinition)
  - StartRuleId, Properties
  - FilePath, IsModified

#### Blazor Components (`src/Minotaur.UI.Blazor/Components/GrammarEditor/`)

- **`TokenEditor.razor`** - Token management:
  - Add, edit, delete tokens
  - Search and filter
  - Configure token properties
  - Import/export functionality

- **`RuleEditor.razor`** - Rule management:
  - Add, edit, delete rules
  - Configure rule parameters
  - Search and filter
  - Template support

- **`GrammarPreview.razor`** - Grammar overview:
  - Summary statistics (tokens, rules, ambiguities)
  - Structure visualization
  - Quick navigation
  - Validation results

- **`TestPanel.razor`** - Grammar testing:
  - Code input with templates
  - Parse and visualize results
  - Multiple views (graph, tree, tokens, log)
  - Ambiguity highlighting

### 3. Plugin Management System

#### Core Models (`src/Minotaur.Core/Models/Plugins/`)

- **`PluginInfo.cs`** - Plugin metadata:
  - Name, Description, Version, Author
  - Category, License, URLs
  - Tags, Dependencies
  - IsEnabled, IsInstalled
  - InstallDate, Downloads, Rating
  - Documentation, Changelog
  - Configuration options
  - AssemblyPath, TypeName

#### Blazor Components (`src/Minotaur.UI.Blazor/Components/PluginManager/`)

- **`PluginGallery.razor`** - Available plugins:
  - Search and filter by category
  - Pagination
  - Install/Uninstall actions
  - Plugin details preview

- **`InstalledPlugins.razor`** - Installed plugins:
  - Enable/Disable toggle
  - Configure options
  - Uninstall
  - Bulk actions
  - Update notifications

- **`PluginDetails.razor`** - Plugin details:
  - Overview (version, author, category, etc.)
  - Metadata (tags, dependencies)
  - Configuration form
  - Documentation viewer
  - Changelog

## Key Design Decisions

### 1. Preserving Ambiguity

**Problem**: Traditional AST visualizers force a single unambiguous tree, but Minotaur needs to support evolving languages where ambiguity is a feature.

**Solution**: 
- Each SymbolNode can have multiple PackedNodes
- Each PackedNode represents a different interpretation
- Visualization shows ALL PackedNode alternatives as edges
- Ambiguous nodes are highlighted
- Users can select specific PackedNode paths

### 2. Direct CognitiveGraph Integration

**Problem**: Need to visualize CognitiveGraph without losing information.

**Solution**:
- Directly traverse SymbolNode and PackedNode structures
- No intermediate conversion that loses data
- Preserve all metadata (RuleId, positions, etc.)
- Map CognitiveGraph concepts directly to visualization concepts

### 3. Interactive Exploration

**Problem**: Users need to understand and navigate ambiguity.

**Solution**:
- Click on ambiguous nodes to see PackedNode alternatives
- Select specific PackedNode to highlight that path
- Toggle between showing all alternatives or single path
- Visual indicators for ambiguity (colors, badges, etc.)

### 4. Grammar Editor Architecture

**Problem**: Need a flexible editor for defining grammars.

**Solution**:
- Separate editors for tokens and rules
- Support for StepParser syntax
- Preview panel for quick feedback
- Test panel for validation
- Template support for common patterns

### 5. Plugin System

**Problem**: Need extensibility for different languages and features.

**Solution**:
- Plugin gallery for discovering plugins
- Installed plugins management
- Configuration support
- Update notifications
- Category-based organization

## File Structure

```
Minotaur/
├── src/
│   ├── Minotaur.Core/
│   │   ├── Models/
│   │   │   ├── Visualization/
│   │   │   │   └── GraphData.cs
│   │   │   ├── Grammar/
│   │   │   │   ├── TokenDefinition.cs
│   │   │   │   ├── RuleDefinition.cs
│   │   │   │   └── GrammarDefinition.cs
│   │   │   └── Plugins/
│   │   │       └── PluginInfo.cs
│   │   ├── Services/
│   │   │   └── Visualization/
│   │   │       └── ICognitiveGraphVisualizer.cs
│   │   └── Controllers/
│   │       └── Visualization/
│   │           └── VisualizationController.cs
│   │
│   ├── Minotaur.UI.Blazor/
│   │   ├── Components/
│   │   │   ├── Shared/
│   │   │   │   └── CognitiveGraphVisualizer.razor
│   │   │   ├── GrammarEditor/
│   │   │   │   ├── TokenEditor.razor
│   │   │   │   ├── RuleEditor.razor
│   │   │   │   ├── GrammarPreview.razor
│   │   │   │   └── TestPanel.razor
│   │   │   └── PluginManager/
│   │   │       ├── PluginGallery.razor
│   │   │       ├── InstalledPlugins.razor
│   │   │       └── PluginDetails.razor
│   │   └── wwwroot/
│   │       └── js/
│   │           └── cognitiveGraphVisualization.js
│   │
│   └── Minotaur.Tests/
│       └── Visualization/
│           ├── CognitiveGraphVisualizerTests.cs
│           ├── VisualizationControllerTests.cs
│           └── GraphDataModelTests.cs
```

## Integration Points

### CognitiveGraph Integration

The visualization system directly uses CognitiveGraph's native types:
- `SymbolNode` → `GraphNode`
- `PackedNode` → `GraphEdge` (with PackedNode metadata)
- Multiple `PackedNode` per `SymbolNode` → Ambiguity

### Blazor/JavaScript Interop

- Blazor components pass JSON data to JavaScript
- JavaScript renders with D3.js
- User interactions trigger Blazor callbacks
- DotNetObjectReference for two-way communication

## Testing Strategy

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **Visual Tests**: Manual verification of rendering
4. **End-to-End Tests**: Full user workflows

## Future Enhancements

1. **Performance Optimization**: Large graph handling
2. **Advanced Layouts**: Hierarchical, tree, circular layouts
3. **3D Visualization**: WebGL-based 3D graph
4. **Collaboration**: Real-time shared visualization
5. **Export**: SVG, PNG, PDF export
6. **Animation**: Smooth transitions between interpretations

## Conclusion

This architecture provides:
- ✅ Full CognitiveGraph integration with ambiguity support
- ✅ Interactive visualization of PackedNode alternatives
- ✅ Grammar editor with preview and testing
- ✅ Plugin system for extensibility
- ✅ Comprehensive test coverage
- ✅ Clean separation of concerns
- ✅ Scalable and maintainable codebase

The implementation correctly handles the unique requirements of Minotaur:
- Preserving ambiguity in evolving languages
- Direct CognitiveGraph integration
- Interactive exploration of multiple interpretations
- Extensible architecture for future growth
