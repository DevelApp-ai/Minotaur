# StepParser C# Implementation

A comprehensive C# translation of the TypeScript StepParser with advanced features including context switching, lexer interaction, grammar loading, semantic actions, grammar inheritance, and production precedence.

## Project Structure

```
src/
├── Minotaur.sln                    # Main solution file
├── StepParser/                     # Core parser library
│   ├── Core/                       # Core parsing components
│   │   ├── IProductionPart.cs     # Production part interface
│   │   ├── Terminal.cs            # Terminal symbols
│   │   ├── NonTerminal.cs         # Non-terminal symbols  
│   │   ├── Production.cs          # Grammar productions
│   │   ├── Token.cs               # Lexer tokens
│   │   ├── ProductionMatch.cs     # Parse results
│   │   ├── ParserPath.cs          # Parser state tracking
│   │   └── StepParser.cs          # Main parser class
│   ├── Grammar/                   # Grammar management
│   │   ├── Grammar.cs             # Grammar definition
│   │   └── GrammarTypes.cs        # Grammar-related types
│   ├── Lexer/                     # Lexical analysis
│   │   ├── StepLexer.cs           # Main lexer class
│   │   ├── LexerPath.cs           # Lexer state tracking
│   │   ├── LexerOptions.cs        # Lexer configuration
│   │   ├── IParserLexerSource.cs  # Source code interfaces
│   │   └── TerminalMatch.cs       # Terminal matching results
│   ├── Memory/                    # Memory management
│   │   ├── MemoryArena.cs         # Memory arena for zero-copy
│   │   ├── StringInterner.cs      # String deduplication
│   │   └── ObjectPool.cs          # Object pooling
│   ├── Context/                   # Context management
│   │   ├── ContextTypes.cs        # Context-related types
│   │   ├── SymbolTable.cs         # Symbol tracking
│   │   └── StepParsingContextAdapter.cs # Context integration
│   └── Utils/                     # Utility components
│       ├── SemanticActionManager.cs    # Semantic actions
│       ├── PrecedenceManager.cs        # Operator precedence
│       └── InheritanceResolver.cs      # Grammar inheritance
├── StepParser.Tests/              # Unit tests
└── StepParser.Demo/               # Demonstration application
```

## Key Features

### 1. Context Switching
- **Context-aware parsing**: The parser maintains parsing context and can switch between different grammatical contexts
- **Scope management**: Tracks nested scopes (functions, classes, blocks)
- **Symbol table integration**: Maintains symbol definitions and references across contexts

### 2. Lexer Interaction
- **Multi-path lexing**: Handles ambiguous grammars by maintaining multiple lexer paths
- **Path merging**: Optimizes by merging equivalent lexer paths
- **Token coordination**: Coordinates between lexer and parser for context-sensitive tokenization

### 3. Grammar Loading
- **File-based loading**: Supports loading grammars from files
- **Content-based loading**: Can parse grammar definitions from strings
- **Multiple formats**: Supports various grammar formats (CEBNF, ANTLR4, Bison, etc.)

### 4. Semantic Actions
- **Action registration**: Register callbacks for specific productions
- **Rich context**: Callbacks receive comprehensive context information
- **Inheritance support**: Semantic actions can be inherited from base grammars

### 5. Grammar Inheritance
- **Base grammar support**: Grammars can inherit from other grammars
- **Rule override**: Derived grammars can override base grammar rules
- **Semantic inheritance**: Option to inherit semantic actions from base grammars

### 6. Production Precedence
- **Operator precedence**: Define precedence levels for operators
- **Associativity rules**: Support for left, right, and non-associative operators
- **Conflict resolution**: Automatically resolves shift/reduce conflicts using precedence

### 7. Memory Management
- **Zero-copy architecture**: Minimizes memory allocations and copying
- **Memory arena**: Efficient memory allocation for parsing operations
- **String interning**: Deduplicates strings to reduce memory usage
- **Object pooling**: Reuses objects to minimize garbage collection pressure

## Usage Examples

### Basic Parsing

```csharp
using var parser = new StepParser.Core.StepParser();

// Create a simple grammar
var grammar = new Grammar("SimpleGrammar");
var identifierTerminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
grammar.AddValidStartTerminal(identifierTerminal);

var identifierProduction = new Production("identifier_rule");
identifierProduction.AddPart(identifierTerminal);
grammar.AddProduction(identifierProduction);
grammar.AddStartProduction(identifierProduction);

parser.SetActiveGrammar(grammar);

var sourceContainer = new SourceContainer("testVariable");
var matches = await parser.ParseAsync("SimpleGrammar", sourceContainer);
```

### Context Switching

```csharp
using var parser = new StepParser.Core.StepParser();

// Set initial context
parser.SetContextState("inside_function", false);
parser.SetContextState("inside_class", false);

// Switch contexts during parsing
parser.SetContextState("inside_function", true);
bool inFunction = parser.GetContextState("inside_function"); // true
```

### Semantic Actions

```csharp
using var parser = new StepParser.Core.StepParser();

// Register a callback for variable declarations
parser.RegisterCallback("variable_declaration", (context) =>
{
    if (context.TryGetValue("token", out var token))
    {
        Console.WriteLine($"Declared variable: {token}");
    }
});

// Set callback context
parser.SetCallbackContext(new Dictionary<string, object>
{
    ["debug_mode"] = true,
    ["optimization_level"] = 2
});
```

### Grammar Inheritance

```csharp
// Create base grammar
var baseGrammar = new Grammar("BaseLanguage");
baseGrammar.IsInheritable = true;

// Create derived grammar
var derivedGrammar = new Grammar("ExtendedLanguage");
derivedGrammar.AddBaseGrammar("BaseLanguage");
derivedGrammar.ImportSemantics = true;
```

### Precedence Rules

```csharp
var grammar = new Grammar("ArithmeticGrammar");

// Define precedence rules
var addSubRule = new PrecedenceRule(1, new[] { "+", "-" }, AssociativityType.Left);
var mulDivRule = new PrecedenceRule(2, new[] { "*", "/" }, AssociativityType.Left);

grammar.AddPrecedenceRule(addSubRule);
grammar.AddPrecedenceRule(mulDivRule);
```

## Building and Testing

### Prerequisites
- .NET 8.0 SDK or later
- C# 12.0 support

### Build the Solution
```bash
cd src
dotnet build
```

### Run Tests
```bash
cd src
dotnet test
```

### Run Demo
```bash
cd src
dotnet run --project StepParser.Demo
```

## Architecture

The StepParser follows a modular architecture with clear separation of concerns:

1. **Core Layer**: Basic parsing primitives (terminals, productions, tokens)
2. **Grammar Layer**: Grammar definition and management
3. **Lexer Layer**: Lexical analysis and tokenization
4. **Memory Layer**: Efficient memory management
5. **Context Layer**: Context tracking and management
6. **Utils Layer**: Advanced features (semantic actions, precedence, inheritance)

### Key Design Patterns

- **Object Pooling**: Reduces garbage collection pressure
- **Factory Pattern**: Creates and manages parser components
- **Strategy Pattern**: Pluggable semantic actions and error recovery
- **Observer Pattern**: Callback mechanisms for semantic actions
- **Template Method**: Grammar inheritance and override mechanisms

## Performance Features

### Zero-Copy Architecture
- Memory arena allocation reduces heap fragmentation
- String interning eliminates duplicate string storage
- Object pooling minimizes garbage collection
- Efficient path management for ambiguous parsing

### Context Optimization
- Cached context computations
- Incremental context updates
- Lazy evaluation of context-dependent operations

### Scalability
- Supports large grammars with inheritance hierarchies
- Efficient handling of ambiguous grammars
- Optimized for step-by-step parsing scenarios

## Extensibility

The StepParser is designed for extensibility:

- **Custom semantic actions**: Implement domain-specific processing
- **Custom error recovery**: Define application-specific error handling
- **Grammar extensions**: Create domain-specific language extensions
- **Custom terminals**: Implement specialized tokenization logic

## Comparison with TypeScript Version

The C# implementation maintains feature parity with the TypeScript version while adding:

- **Strong typing**: Compile-time type safety
- **Memory efficiency**: Better memory management through arenas and pooling
- **Performance**: Optimized for .NET runtime
- **Integration**: Natural integration with .NET ecosystem

## Contributing

When contributing to this project:

1. Follow C# coding conventions
2. Add unit tests for new features
3. Update documentation for API changes
4. Ensure backward compatibility when possible
5. Profile performance impact of changes

## License

This project maintains the same license as the original TypeScript implementation.