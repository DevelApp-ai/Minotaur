# CognitiveGraph Editor

Graph editor backend for DevelApp CognitiveGraph that provides zero-copy editing capabilities with undo/redo support.

## Features

- **Zero-Copy Editing**: Efficient graph modifications without memory overhead
- **Undo/Redo Support**: Full command pattern implementation for edit operations
- **Thread-Safe Operations**: Concurrent access support for multi-threaded applications
- **Real-Time Updates**: SignalR hub for live collaboration scenarios
- **Type-Safe Operations**: Strongly typed edit operations with compile-time safety

## Installation

```bash
dotnet add package DevelApp.CognitiveGraph.Editor
```

## Usage

```csharp
using CognitiveGraph.Editor;

// Create a graph editor
var editor = new GraphEditor();

// Insert nodes
var root = new NonTerminalNode("expression", 1);
editor.SetRoot(root);

var left = new IdentifierNode("x");
var op = new TerminalNode("operator", "+");
var right = new LiteralNode("number", 42);

editor.InsertNode(root.Id, left);
editor.InsertNode(root.Id, op);
editor.InsertNode(root.Id, right);

// Undo/Redo operations
if (editor.CanUndo)
{
    editor.Undo();
}

if (editor.CanRedo)
{
    editor.Redo();
}
```

## API Components

### CognitiveGraphService

Provides efficient API access to cognitive graphs with caching and pagination:

```csharp
using CognitiveGraph.Api.Services;

var service = new CognitiveGraphService(editor);
var graphId = service.StoreCognitiveGraph(rootNode);

var query = new CognitiveGraphQuery
{
    MaxDepth = 3,
    NodeTypes = new List<string> { "Terminal", "NonTerminal" }
};

var response = await service.QueryCognitiveGraphAsync(graphId, query);
```

### CognitiveGraphHub

SignalR hub for real-time graph collaboration:

```csharp
using CognitiveGraph.Api.Hubs;

// Join a graph session for real-time updates
await hub.JoinGraphSession(graphId);

// Subscribe to node changes
await hub.SubscribeToNode(nodeId);
```

## License

This project is licensed under the AGPL 3.0 License.