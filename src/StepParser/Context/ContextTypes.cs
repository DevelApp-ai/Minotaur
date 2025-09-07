using StepParser.Grammar;

namespace StepParser.Context;

/// <summary>
/// Represents positional information in code.
/// </summary>
public record CodePosition
{
    /// <summary>
    /// Gets the line number.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Gets the column number.
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Gets the character offset.
    /// </summary>
    public int Offset { get; init; }
}

/// <summary>
/// Represents scope information.
/// </summary>
public record ScopeInfo
{
    /// <summary>
    /// Gets the scope ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the scope name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the scope type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the start position.
    /// </summary>
    public CodePosition? StartPosition { get; init; }

    /// <summary>
    /// Gets the end position.
    /// </summary>
    public CodePosition? EndPosition { get; init; }
}

/// <summary>
/// Represents symbol information.
/// </summary>
public record SymbolInfo
{
    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the symbol type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the symbol kind.
    /// </summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Gets the scope where the symbol is defined.
    /// </summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// Gets the position where the symbol is defined.
    /// </summary>
    public CodePosition? Position { get; init; }

    /// <summary>
    /// Gets the references to this symbol.
    /// </summary>
    public List<CodePosition> References { get; init; } = new();

    /// <summary>
    /// Gets the definition position.
    /// </summary>
    public CodePosition? Definition { get; init; }

    /// <summary>
    /// Gets the context where the symbol is used.
    /// </summary>
    public List<string> Context { get; init; } = new();
}

/// <summary>
/// Represents parse state information.
/// </summary>
public record ParseStateInfo
{
    /// <summary>
    /// Gets the current rule being parsed.
    /// </summary>
    public string CurrentRule { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current position.
    /// </summary>
    public CodePosition? Position { get; init; }

    /// <summary>
    /// Gets the context stack.
    /// </summary>
    public List<string> ContextStack { get; init; } = new();

    /// <summary>
    /// Gets the scope stack.
    /// </summary>
    public List<ScopeInfo> ScopeStack { get; init; } = new();

    /// <summary>
    /// Gets the valid terminals at this position.
    /// </summary>
    public List<string> ValidTerminals { get; init; } = new();

    /// <summary>
    /// Gets any errors at this position.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Gets any warnings at this position.
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Gets any suggestions at this position.
    /// </summary>
    public List<string> Suggestions { get; init; } = new();

    /// <summary>
    /// Gets the grammar state.
    /// </summary>
    public GrammarState? GrammarState { get; init; }
}

/// <summary>
/// Represents grammar state information.
/// </summary>
public record GrammarState
{
    /// <summary>
    /// Gets the active grammar name.
    /// </summary>
    public string ActiveGrammar { get; init; } = string.Empty;

    /// <summary>
    /// Gets the format type.
    /// </summary>
    public GrammarFormatType FormatType { get; init; }

    /// <summary>
    /// Gets the base grammars.
    /// </summary>
    public List<string> BaseGrammars { get; init; } = new();

    /// <summary>
    /// Gets the active rules.
    /// </summary>
    public List<string> ActiveRules { get; init; } = new();

    /// <summary>
    /// Gets the context modifiers.
    /// </summary>
    public List<string> ContextModifiers { get; init; } = new();

    /// <summary>
    /// Gets the inheritance chain.
    /// </summary>
    public List<string> InheritanceChain { get; init; } = new();
}

/// <summary>
/// Represents comprehensive context information.
/// </summary>
public record ContextInfo
{
    /// <summary>
    /// Gets the current scope.
    /// </summary>
    public ScopeInfo? Scope { get; init; }

    /// <summary>
    /// Gets the scope stack.
    /// </summary>
    public List<ScopeInfo> ScopeStack { get; init; } = new();

    /// <summary>
    /// Gets the context stack.
    /// </summary>
    public List<string> ContextStack { get; init; } = new();

    /// <summary>
    /// Gets the available symbols.
    /// </summary>
    public List<SymbolInfo> Symbols { get; init; } = new();

    /// <summary>
    /// Gets the parse state.
    /// </summary>
    public ParseStateInfo? ParseState { get; init; }

    /// <summary>
    /// Gets the active grammar.
    /// </summary>
    public string? ActiveGrammar { get; init; }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    public long Timestamp { get; init; }
}