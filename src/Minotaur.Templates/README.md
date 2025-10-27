# Minotaur Application Templates

This package contains source code templates and examples for building applications on top of the **DevelApp.Minotaur** cognitive graph framework.

## What's Included

This package provides complete source code for:

### Demo Application (`content/Demo/`)
A comprehensive console application demonstrating:
- Creating and manipulating CognitiveGraphNodes
- Using SymbolicAnalysis types
- Integration with the Minotaur core library
- Best practices for working with the framework

### Blazor UI Application (`content/BlazorUI/`)
A complete Blazor web application featuring:
- Interactive UI components for graph visualization
- GraphQL API integration with HotChocolate
- SignalR real-time communication
- Service layer architecture
- Component-based design

## Installation

```bash
dotnet add package DevelApp.Minotaur.Templates
```

## Usage

After installing this package, you can access the source code in your NuGet packages cache or extract the templates to use as a starting point for your own applications.

### Getting Started with the Demo

1. Copy the Demo folder contents to your project
2. Add a reference to `DevelApp.Minotaur` package:
   ```bash
   dotnet add package DevelApp.Minotaur
   ```
3. Run the demo:
   ```bash
   dotnet run
   ```

### Getting Started with the Blazor UI

1. Copy the BlazorUI folder contents to your project
2. Add required package references:
   ```bash
   dotnet add package DevelApp.Minotaur
   dotnet add package HotChocolate.AspNetCore
   dotnet add package HotChocolate.Data
   dotnet add package Microsoft.AspNetCore.SignalR.Client
   ```
3. Run the application:
   ```bash
   dotnet run
   ```

## Requirements

- .NET 8.0 or later
- DevelApp.Minotaur package (automatically referenced)

## Examples

The included code demonstrates:

- **Graph Node Creation**: Working with TerminalNode, IdentifierNode, LiteralNode, and NonTerminalNode
- **Symbolic Analysis**: Using SymbolicAnalysisResult, ExecutionPath, and SymbolicConstraint
- **UI Components**: Building interactive graph editors with Blazor
- **GraphQL Integration**: Setting up type-safe APIs for graph operations
- **Real-time Updates**: Implementing SignalR for live graph synchronization

## Documentation

For complete documentation on the Minotaur framework, visit:
- [GitHub Repository](https://github.com/DevelApp-ai/Minotaur)
- [API Documentation](https://github.com/DevelApp-ai/Minotaur/tree/main/docs)

## License

This package is licensed under AGPL-3.0-or-later, the same as the Minotaur framework.

## Support

For issues, questions, or contributions:
- Open an issue on [GitHub](https://github.com/DevelApp-ai/Minotaur/issues)
- Check the [documentation](https://github.com/DevelApp-ai/Minotaur)

## Version

This package follows the same versioning as the DevelApp.Minotaur core package.
