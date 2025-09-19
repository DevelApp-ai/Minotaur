# Minotaur - Cognitive Graph Editor & Unparser

[![Build Status](https://github.com/DevelApp-ai/Minotaur2/workflows/CI-CD/badge.svg)](https://github.com/DevelApp-ai/Minotaur2/actions)
[![NuGet](https://img.shields.io/nuget/v/DevelApp.Minotaur.svg)](https://www.nuget.org/packages/DevelApp.Minotaur/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A production-ready zero-copy cognitive graph editor and unparser framework with StepParser integration, extensible plugin system, and advanced location tracking capabilities. Implements the complete GrammarForge documentation requirements.

## 🚀 Features

- **Zero-Copy Cognitive Graph Management**: Built on `DevelApp.CognitiveGraph 1.0.0` for optimal performance
- **Production StepParser Integration**: Seamless integration with `DevelApp.StepLexer 1.0.1` and `DevelApp.StepParser 1.0.1`
- **Extensible Plugin System**: Runtime plugin loading with `DevelApp.RuntimePluggableClassFactory 2.0.1`
- **Advanced Location Tracking**: Precision coordinate tracking with enhanced APIs
- **Context-Aware Editing**: Rule activation callbacks and surgical operations
- **Multi-Language Code Generation**: Built-in support for C#, JavaScript, and Python
- **Compiler-Compiler Backend**: Extensible generation rules for new language backends

## 📦 Installation

```bash
dotnet add package DevelApp.Minotaur
```

## 🔧 Quick Start

### Basic Graph Editing

```csharp
using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;

// Create a graph editor with a root node
var root = new NonTerminalNode("expression", 0);
var editor = new GraphEditor(root);

// Add child nodes
var left = new TerminalNode("identifier", "x");
var op = new TerminalNode("operator", "+");
var right = new LiteralNode("number", 42);

editor.Root.AddChild(left);
editor.Root.AddChild(op);
editor.Root.AddChild(right);

// Undo/Redo support
Console.WriteLine($"Can undo: {editor.CanUndo}");
```

### StepParser Integration

```csharp
using GolemCognitiveGraph.Parser;
using GolemCognitiveGraph.Plugins;

// Create integration with plugin manager
using var pluginManager = new LanguagePluginManager();
using var integration = StepParserIntegrationFactory.CreateForFile("Example.cs", pluginManager);

// Parse source code to cognitive graph
var sourceCode = "var x = 42;";
var editor = await integration.ParseToEditableGraphAsync(sourceCode);

// Edit the graph
editor.Root?.AddChild(new TerminalNode("comment", "// Generated"));

// Unparse back to code
var csharpPlugin = integration.PluginManager.GetPlugin("csharp");
var regeneratedCode = await csharpPlugin.UnparseAsync(editor.Root);
```

### Advanced Context-Aware Editing

```csharp
using GolemCognitiveGraph.Advanced;

// Create context-aware editor with precision tracking
var contextEditor = new ContextAwareEditor(editor);
var tracker = contextEditor.CreateLocationTracker(sourceCode, "MyFile.cs");

// Register rule activation callbacks
contextEditor.RegisterCallback(new MyRuleCallback());

// Perform surgical edits with context
var edit = new ContextualEdit
{
    Type = EditType.Insert,
    NewNode = new TerminalNode("newVariable", "y"),
    TargetPosition = tracker.GetPositionAt(10),
    ContextRadius = 2
};

var result = await contextEditor.EditWithContextAsync(edit);
```

### Plugin System

```csharp
using GolemCognitiveGraph.Plugins;

// Built-in language plugins
using var pluginManager = new LanguagePluginManager();

// Get plugin by language
var csharpPlugin = pluginManager.GetPlugin("csharp");
var jsPlugin = pluginManager.GetPlugin("javascript");
var pythonPlugin = pluginManager.GetPlugin("python");

// Get plugin by file extension
var plugin = pluginManager.GetPluginByExtension(".cs");

// Generate backend rules for compiler-compiler
var backendRules = await csharpPlugin.GenerateCompilerBackendRulesAsync();
Console.WriteLine($"Generated {backendRules.GenerationRules.Count} rules");
```

## 🏗️ Architecture

### Core Components

- **GraphEditor**: Zero-copy graph modification with undo/redo
- **StepParserIntegration**: Bridge between StepParser and cognitive graphs
- **LanguagePluginManager**: Runtime plugin discovery and management
- **ContextAwareEditor**: Advanced editing with location tracking
- **PrecisionLocationTracker**: High-precision coordinate tracking

### Architectural Separation

- **StepParser**: Handles ALL parsing, grammar, and syntax (single source of truth)
- **Plugins**: Handle unparsing and compiler backend generation ONLY
- **Zero-Copy Integration**: Seamless data flow between parsing and unparsing

## 📊 Implementation Status

- ✅ **100% GrammarForge Requirements Implemented**
- ✅ **56 Comprehensive Unit Tests** (100% passing)
- ✅ **Production NuGet Dependencies**
- ✅ **Advanced Location Tracking**
- ✅ **Context-Aware Editing**
- ✅ **Rule Activation Callbacks**
- ✅ **Multi-Language Plugin System**

## 🧪 Testing

```bash
# Run all tests
dotnet test src/Minotaur.sln

# Run with coverage
dotnet test src/Minotaur.sln --collect:"XPlat Code Coverage"
```

## 📚 Documentation

- [API Documentation](./docs/api/)
- [Architecture Guide](./docs/architecture.md)
- [Plugin Development](./docs/plugins.md)
- [GAP Analysis](./GAP_Analysis_StepParser_Implementation.md)

## 🔗 Dependencies

- [DevelApp.CognitiveGraph 1.0.0](https://www.nuget.org/packages/DevelApp.CognitiveGraph/)
- [DevelApp.StepLexer 1.0.1](https://www.nuget.org/packages/DevelApp.StepLexer/)
- [DevelApp.StepParser 1.0.1](https://www.nuget.org/packages/DevelApp.StepParser/)
- [DevelApp.RuntimePluggableClassFactory 2.0.1](https://www.nuget.org/packages/DevelApp.RuntimePluggableClassFactory/)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

## 🏷️ Releases

- **v1.0.0**: Initial release with complete GrammarForge implementation
- **v1.0.0-preview**: Pre-release versions for testing

## 📈 Roadmap

- Enhanced IDE integration
- Additional language plugins
- Performance optimizations
- Extended documentation