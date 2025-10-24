# Minotaur - Advanced Compiler-Compiler Platform

[![CI/CD Pipeline](https://github.com/DevelApp-ai/Minotaur/actions/workflows/ci-cd-enhanced.yml/badge.svg)](https://github.com/DevelApp-ai/Minotaur/actions/workflows/ci-cd-enhanced.yml)
[![NuGet Version](https://img.shields.io/nuget/v/Minotaur)](https://www.nuget.org/packages/Minotaur)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

![Minotaur Logo](assets/logos/Minotaur_logo.png)

Minotaur is a powerful compiler-compiler platform that revolutionizes grammar development through automated grammar generation, error-driven refinement, and comprehensive language analysis capabilities.

## 🚀 Key Features

### Automated Grammar Generation
- **Source Code Analysis**: Automatically analyzes existing codebases to extract grammar patterns
- **Token Pattern Recognition**: Identifies keywords, operators, literals, and structural elements
- **Syntax Structure Discovery**: Discovers expression precedence, statement types, and control flow patterns
- **Error-Driven Refinement**: Uses parsing errors to iteratively improve grammar accuracy

### Advanced Language Support
- **Context-Aware Processing**: Leverages advanced context analysis for precise grammar generation
- **Multi-Language Support**: Handles diverse programming languages and domain-specific languages
- **Embedded Grammar Support**: Processes languages embedded within other languages (e.g., JavaScript in HTML)
- **Grammar File Creation Guide Compliance**: Outputs standard-compliant grammar files

### Interactive Development Environment
- **Command-Line Interface**: Comprehensive CLI for grammar generation, validation, and testing
- **Real-Time Progress Tracking**: Monitor grammar generation progress with detailed metrics
- **Quality Assessment**: Built-in validation and quality scoring for generated grammars
- **Comprehensive Testing**: Extensive test suites for grammar validation and refinement

## 📦 Installation

```bash
dotnet add package DevelApp.Minotaur
```

## 🔧 Quick Start

### StepParser Integration

```csharp
using Minotaur.Parser;
using Minotaur.Plugins;
using Minotaur.Core;

// Create integration with plugin manager
using var pluginManager = new LanguagePluginManager();
using var integration = new StepParserIntegration();

// Parse source code to cognitive graph
var sourceCode = "var x = 42;";
var cognitiveGraph = await integration.ParseToCognitiveGraphAsync(sourceCode);

// Edit the graph
editor.Root?.AddChild(new TerminalNode("comment", "// Generated"));

// Unparse back to code
var csharpPlugin = integration.PluginManager.GetPlugin("csharp");
var regeneratedCode = await csharpPlugin.UnparseAsync(editor.Root);
```

### Advanced Context-Aware Editing

```csharp
using CognitiveGraph.Editor; // Requires DevelApp.CognitiveGraph.Editor package
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

- **LanguagePluginManager**: Runtime plugin discovery and management
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

### Core Dependencies
- [DevelApp.CognitiveGraph 1.0.2](https://www.nuget.org/packages/DevelApp.CognitiveGraph/) - Includes simplified GraphQL and fluid interface for high-speed integrations
- [DevelApp.StepLexer 1.9.0](https://www.nuget.org/packages/DevelApp.StepLexer/)
- [DevelApp.StepParser 1.9.0](https://www.nuget.org/packages/DevelApp.StepParser/)
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