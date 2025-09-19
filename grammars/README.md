# Minotaur Project Grammar Files

This directory contains grammar files for project-level analysis and cross-file editing support in the Minotaur compiler-compiler system.

## Grammar Files

### DotNetProject.grammar
Comprehensive grammar for .NET solution and project file parsing, supporting:
- **Solution files (.sln)**: Project references, configurations, dependencies
- **Project files (.csproj, .vbproj, .fsproj)**: Build properties, package references, file inclusions
- **Cross-file analysis**: Namespace usage, type dependencies, resource references
- **Embedded grammars**: Automatic grammar selection based on file types

**Supported File Types:**
- C# files (*.cs) → CSharp10.grammar
- VB.NET files (*.vb) → VisualBasic.grammar  
- F# files (*.fs) → FSharp.grammar
- Razor files (*.razor, *.cshtml) → HTMLEmbedded.grammar + CSharp10.grammar
- XAML files (*.xaml) → XAML.grammar
- JSON files (*.json) → JSON.grammar
- XML files (*.xml, *.config) → XML.grammar

### FolderProject.grammar
Generic folder-based project structure analysis, supporting:
- **Automatic project type detection** based on file patterns
- **Multi-language support** through embedded grammars
- **Cross-file relationship tracking** (includes, imports, references)
- **Symbol extraction** for intelligent analysis
- **Build system integration** (CMake, MSBuild, Maven, npm, Cargo, etc.)

**Supported Project Types:**
- .NET projects (*.sln, *.csproj)
- Node.js projects (package.json, *.ts, *.js)
- Python projects (*.py, requirements.txt, setup.py)
- Java projects (*.java, pom.xml, build.gradle)
- C++ projects (*.cpp, *.h, CMakeLists.txt)
- Rust projects (*.rs, Cargo.toml)
- Go projects (*.go, go.mod)
- Generic projects (mixed file types)

## Usage in StepParser

These grammars are designed to be loaded by the StepParser plugin system for project-level analysis:

```csharp
// Load .NET solution for cross-file analysis
var dotnetProject = stepParser.LoadProjectGrammar("DotNetProject.grammar");
var analysis = dotnetProject.AnalyzeSolution("MySolution.sln");

// Load folder-based project
var folderProject = stepParser.LoadProjectGrammar("FolderProject.grammar");
var projectAnalysis = folderProject.AnalyzeFolder("/path/to/project");
```

## Cross-File Analysis Features

### Namespace and Type Tracking
- **Using/Import statements**: Track namespace dependencies across files
- **Type references**: Identify class, interface, and type dependencies
- **Inheritance chains**: Map class hierarchy across multiple files

### Resource Dependencies  
- **Embedded resources**: Track resource file usage in source code
- **Configuration files**: Analyze settings and configuration dependencies
- **Documentation links**: Connect code to documentation references

### Build Dependencies
- **Project references**: Map dependencies between projects
- **Package references**: Track NuGet/npm/PyPI package usage
- **Build targets**: Analyze compilation and deployment relationships

## Grammar Embedding

The `@embed_grammar()` directive allows automatic selection of appropriate grammars based on file extensions:

```grammar
csharp_file:
    @embed_grammar("CSharp10.grammar")
    file_path: *.cs
    cross_file_references: using_directives, namespace_declarations, type_references
    ;
```

This enables the StepParser to:
1. **Detect file types** automatically
2. **Load appropriate grammars** for each file
3. **Parse multiple languages** in a single project
4. **Maintain cross-file relationships** between different file types

## Integration with Minotaur

These grammars integrate with the Minotaur system to provide:

- **Project-wide refactoring**: Safe renaming and restructuring across files
- **Intelligent code generation**: Context-aware code generation based on project structure
- **Dependency analysis**: Impact analysis for changes across the project
- **Cross-language support**: Unified analysis of multi-language projects

## License

These grammar files are part of the Minotaur project and are licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).