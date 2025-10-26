# Minotaur - Cognitive Graph Editing and Unparsing

This implementation provides a cognitive graph editing system and unparser for the Minotaur compiler-compiler platform. It integrates with DevelApp.StepParser for parsing and provides language plugins for code generation.

## Architecture Overview

### Core Components

#### 1. CognitiveGraphNode Base Class
- **Zero-copy semantics**: Nodes maintain references to underlying parser data
- **Metadata support**: Extensible key-value metadata storage
- **Source position tracking**: Location information for position-aware operations
- **Visitor pattern support**: For traversal and transformation operations

#### 2. StepParserIntegration
- **Parsing via DevelApp.StepParser**: All parsing handled by StepParser NuGet package
- **Cognitive graph generation**: Converts parse trees to cognitive graphs
- **Validation support**: Source code validation and error reporting

#### 3. GraphUnparser System
- **Strategy-based unparsing**: Configurable output strategies per node type
- **Asynchronous support**: Non-blocking unparsing operations
- **Format preservation**: Maintains code formatting preferences

#### 4. Language Plugin System
- **Extensible plugins**: Support for multiple language backends
- **Unparsing strategies**: Language-specific code generation
- **Compiler backend rules**: Generate code generation templates

### Key Features

#### Zero-Copy Architecture
- **Memory efficiency**: Nodes reference existing data without duplication
- **Performance optimization**: Minimal memory allocation during operations
- **Direct manipulation**: Edit operations work on cognitive graph structures

#### Direct Graph Editing
- **Node manipulation**: Add, remove, and modify graph nodes
- **Structural integrity**: Parent-child relationships automatically managed
- **Position tracking**: Source location information preserved

## Node Types

### Terminal Nodes
- `TerminalNode`: Basic leaf nodes with text content
- `LiteralNode`: Specialized for literal values (strings, numbers, booleans)
- `IdentifierNode`: For identifiers with namespace support

### Non-Terminal Nodes
- `NonTerminalNode`: Container nodes representing grammar rules
- Extensible for specific language constructs

## Usage Examples

### Basic Graph Construction and Parsing
```csharp
using Minotaur.Core;
using Minotaur.Parser;
using Minotaur.Plugins;

// Parse source code to cognitive graph
using var integration = new StepParserIntegration();
var sourceCode = "var x = 42;";
var graph = await integration.ParseToCognitiveGraphAsync(sourceCode);

// Edit the graph
var comment = new TerminalNode("comment", "// Generated code");
graph.AddChild(comment);
```

### Code Generation with Language Plugins
```csharp
using Minotaur.Plugins;

// Get language plugin
var pluginManager = new LanguagePluginManager();
var csharpPlugin = pluginManager.GetPlugin("csharp");

// Generate code from graph
var code = await csharpPlugin.UnparseAsync(graph);
```

### Custom Unparsing Strategies
```csharp
using Minotaur.Unparser;

var config = new UnparseConfiguration
{
    FormatOutput = true,
    IncludeComments = true
};

using var unparser = new GraphUnparser(config);
var code = await unparser.UnparseAsync(root);
```

## Implementation Status

### ✅ Completed Features
- **StepParser Integration**: Full parsing via DevelApp.StepParser 1.9.0
- **Cognitive graph infrastructure**: Node types, visitor pattern, metadata
- **Direct graph editing**: Add, remove, and modify nodes
- **Unparsing framework**: Strategy-based code generation
- **Language plugins**: C#, JavaScript, Python, LLVM support
- **Comprehensive testing**: 111 passing unit tests
- **Grammar generation**: Automated grammar discovery system
- **Symbolic analysis**: Advanced code analysis capabilities

### 🔄 Not Implemented
- **GraphEditor class**: Does not exist - use direct node manipulation
- **Undo/redo system**: Not implemented
- **Context-aware editor**: Not implemented
- **Location tracker**: Not implemented

## Testing

The implementation includes comprehensive unit tests covering:
- StepParser integration and validation
- Graph node operations
- Language plugin functionality
- Code unparsing with various strategies
- Grammar generation and analysis

```bash
cd src
dotnet test Minotaur.Tests
```

## Dependencies

- **.NET 8.0**: Target framework
- **DevelApp.CognitiveGraph 1.0.2**: Cognitive graph data structures
- **DevelApp.StepLexer 1.9.0**: Tokenization
- **DevelApp.StepParser 1.9.0**: Parsing
- **DevelApp.RuntimePluggableClassFactory 2.0.1**: Plugin system

## Notes

This implementation provides the foundation for the Minotaur compiler-compiler platform. It integrates with production-ready parsing libraries and provides extensible language plugin support for code generation.