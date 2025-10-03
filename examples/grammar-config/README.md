# Grammar Configuration Example

This directory demonstrates how to use Minotaur's advanced grammar detection system with custom configuration files.

## Overview

The grammar detection system in Minotaur allows you to:

1. **Determine which grammar to use for which file** based on multiple strategies
2. **Specify exact grammar versions** for different file types and project contexts  
3. **Define project-specific grammar configurations** in separate files
4. **Override default detection** with custom rules and patterns

## Configuration File

The `minotaur.grammar.json` file in this directory shows a comprehensive example of grammar configuration options:

### Key Features

- **Extension Mappings**: Map file extensions to specific grammars and versions
- **Path Mappings**: Use glob patterns to map file paths to grammars
- **Content Rules**: Detect grammars based on file content patterns
- **Project Type Overrides**: Different grammars for different project types
- **Fallback Grammars**: Alternative grammars when primary detection fails
- **Confidence Levels**: Specify how confident the detection should be

### Detection Priority

The grammar detection system uses the following priority order:

1. **Content-Based Detection** (Priority: 200+)
   - Analyzes file content for language-specific patterns
   - Most accurate but requires file reading

2. **File Extension Detection** (Priority: 100)
   - Fast lookup based on file extensions
   - Good fallback when content analysis fails

3. **Configuration Overrides** always take precedence over defaults

## Usage Examples

### Basic Usage

```csharp
using Minotaur.Projects.Grammar;

// Create detection manager
using var manager = GrammarDetectionManager.CreateDefault();

// Detect grammar for a single file
var result = await manager.DetectGrammarAsync(
    "/path/to/file.cs", 
    "/path/to/project"
);

if (result.IsSuccessful)
{
    Console.WriteLine($"Grammar: {result.GrammarName}");
    Console.WriteLine($"Version: {result.Version}");
    Console.WriteLine($"Confidence: {result.Confidence:P}");
}
```

### Batch Detection

```csharp
// Detect grammars for multiple files
var files = new[] { "file1.cs", "file2.js", "file3.py" };
var results = await manager.DetectGrammarsAsync(files, projectRoot);

foreach (var (file, result) in results)
{
    Console.WriteLine($"{file}: {result.GrammarName} (v{result.Version})");
}
```

### Custom Configuration

```csharp
// Load custom configuration
var config = await GrammarConfiguration.LoadFromFileAsync("minotaur.grammar.json");

// Use with detection manager
var context = GrammarDetectionContext.Create(filePath, projectRoot, projectType, config);
var result = await detector.DetectGrammarAsync(context);
```

## Integration with ProjectLoader

The `ProjectLoader` class automatically uses the grammar detection system:

```csharp
using Minotaur.Projects;
using Minotaur.Parser;

var stepParser = StepParserIntegrationFactory.CreateForCSharp();
var projectLoader = new ProjectLoader(stepParser);

// Automatically detects grammars for all files
var project = await projectLoader.LoadFolderAsync("/path/to/project");

foreach (var file in project.Files)
{
    Console.WriteLine($"{file.RelativePath}: {file.EmbeddedGrammar}");
}
```

## Extending the System

### Custom Detectors

```csharp
public class MyCustomDetector : IGrammarDetector
{
    public string DetectorId => "my-custom";
    public int Priority => 150;
    
    public async Task<GrammarDetectionResult> DetectGrammarAsync(GrammarDetectionContext context)
    {
        // Custom detection logic
        return GrammarDetectionResult.Success("MyGrammar.grammar", confidence: 0.8);
    }
    
    public bool CanDetect(GrammarDetectionContext context) => true;
}

// Register custom detector
manager.AddDetector(new MyCustomDetector());
```

### Custom Content Rules

```csharp
ContentBasedGrammarDetector.AddContentRule(new ContentDetectionRule
{
    Name = "my-language-pattern",
    Pattern = @"^\s*@MyDirective\s+",
    Mapping = new GrammarMapping 
    { 
        Grammar = "MyLanguage.grammar", 
        Confidence = 0.95 
    },
    Priority = 20
});
```

## Configuration Schema

The grammar configuration file supports the following structure:

```json
{
  "defaultGrammar": "string",           // Default grammar name
  "defaultVersion": "string",           // Default version
  "grammarSearchPaths": ["string"],     // Paths to search for grammar files
  "extensionMappings": {                // File extension mappings
    ".ext": {
      "grammar": "string",
      "version": "string", 
      "confidence": 0.0-1.0,
      "fallbacks": ["string"],
      "metadata": {}
    }
  },
  "pathMappings": {                     // Glob pattern mappings
    "pattern": { /* same as extension mapping */ }
  },
  "projectTypeOverrides": {             // Project type specific overrides
    "ProjectType": { /* same as extension mapping */ }
  },
  "contentRules": [                     // Content-based detection rules
    {
      "name": "string",
      "pattern": "regex",
      "mapping": { /* same as extension mapping */ },
      "priority": 0,
      "maxLines": 0,
      "caseSensitive": false
    }
  ],
  "metadata": {}                        // Additional metadata
}
```

This powerful grammar detection system ensures that Minotaur can accurately determine the appropriate grammar and version for any file in your project, supporting complex multi-language scenarios and custom requirements.