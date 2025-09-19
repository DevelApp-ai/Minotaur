# Golem Cognitive Graph Editor & Unparser

This implementation provides a zero-copy cognitive graph editing system and unparser based on the technical specification. It includes a sophisticated GraphEditor for performing surgical operations on cognitive graph structures and an Unparser for generating code from the graph.

## Architecture Overview

### Core Components

#### 1. CognitiveGraphNode Base Class
- **Zero-copy semantics**: Nodes maintain references without copying data
- **Metadata support**: Extensible key-value metadata storage
- **Source position tracking**: Location information for position-aware operations
- **Visitor pattern support**: For traversal and transformation operations

#### 2. GraphEditor Class
- **Zero-copy modification operations**: Insert, remove, replace, move nodes
- **Undo/redo support**: Full operation history with command pattern
- **Node indexing**: Fast lookup by ID for efficient operations
- **Thread-safe operations**: Concurrent access protection

#### 3. GraphUnparser System
- **Strategy-based unparsing**: Configurable output strategies per node type
- **Format preservation**: Optional original formatting preservation
- **Asynchronous support**: Non-blocking unparsing operations
- **Stream output**: Direct writing to streams for large outputs

### Key Features

#### Zero-Copy Architecture
- **Memory efficiency**: Nodes reference existing data without duplication
- **Performance optimization**: Minimal memory allocation during operations
- **Direct manipulation**: Edit operations work on original structures

#### Context-Aware Editing
- **Metadata preservation**: Node metadata maintained during operations
- **Structural integrity**: Parent-child relationships automatically managed
- **Position tracking**: Source location information preserved

#### Advanced Operations
- **Surgical editing**: Precise node-level modifications
- **Bulk operations**: Efficient multi-node transformations
- **History management**: Complete undo/redo with operation tracking

## Node Types

### Terminal Nodes
- `TerminalNode`: Basic leaf nodes with text content
- `LiteralNode`: Specialized for literal values (strings, numbers, booleans)
- `IdentifierNode`: For identifiers with namespace support

### Non-Terminal Nodes
- `NonTerminalNode`: Container nodes representing grammar rules
- Extensible for specific language constructs

## Usage Examples

### Basic Graph Construction
```csharp
using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;

// Create expression: x + 5
var root = new NonTerminalNode("expression");
using var editor = new GraphEditor(root);

var x = new IdentifierNode("x");
var plus = new TerminalNode("+", "operator");
var five = new LiteralNode("5", "number", 5);

editor.InsertNode(root.Id, x);
editor.InsertNode(root.Id, plus);
editor.InsertNode(root.Id, five);
```

### Zero-Copy Editing Operations
```csharp
// Replace a node while preserving children
var replacement = new IdentifierNode("y");
editor.ReplaceNode(x.Id, replacement, preserveChildren: true);

// Move a node to new parent
editor.MoveNode(five.Id, newParent.Id);

// Undo/redo operations
editor.Undo();
editor.Redo();
```

### Code Generation
```csharp
using GolemCognitiveGraph.Unparser;

var config = new UnparseConfiguration
{
    FormatOutput = true,
    IncludeComments = true,
    MaxLineLength = 120
};

using var unparser = new GraphUnparser(config);
var code = await unparser.UnparseAsync(root);
```

### Custom Unparsing Strategies
```csharp
public class CustomStrategy : IUnparseStrategy
{
    public void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        // Custom unparsing logic
        context.Write($"CUSTOM[{node.NodeType}]");
    }
}

unparser.RegisterStrategy("custom", new CustomStrategy());
```

## Implementation Status

### ✅ Completed Features
- **Core graph infrastructure**: Node types, visitor pattern, metadata
- **Zero-copy editing**: GraphEditor with full operation set
- **Undo/redo system**: Complete operation history management
- **Unparsing framework**: Strategy-based code generation
- **Thread safety**: Concurrent access protection
- **Comprehensive testing**: Full test suite with 16 passing tests

### 🔄 Future Enhancements (as needed)
- **CognitiveGraph NuGet integration**: When package becomes available
- **Advanced language support**: Specific language unparsing strategies
- **Performance optimizations**: Memory pooling, bulk operations
- **Serialization support**: Graph persistence and loading

## Testing

The implementation includes comprehensive unit tests covering:
- Graph editing operations (insert, remove, replace, move)
- Undo/redo functionality
- Node finding and indexing
- Code unparsing with various strategies
- Error handling and edge cases

```bash
cd src
dotnet test GolemCognitiveGraph.Tests
```

## Demo Application

A complete demo application showcases the key features:

```bash
cd src
dotnet run --project GolemCognitiveGraph.Demo
```

The demo demonstrates:
1. Basic graph construction and editing
2. Code unparsing with different configurations
3. Advanced editing operations with undo/redo

## Dependencies

- **.NET 8.0**: Target framework
- **System.Memory**: Memory management optimizations
- **System.Text.Json**: JSON serialization support

## Notes

This implementation provides a solid foundation for the Golem Cognitive Graph Editor & Unparser system. It implements the core concepts described in the technical specification while maintaining extensibility for future enhancements. The zero-copy architecture ensures optimal performance for surgical editing operations on large code structures.

When the CognitiveGraph 1.0.0 NuGet package becomes available, this implementation can be easily integrated or serve as a reference for the integration.