using StepParser.Core;
using StepParser.Grammar;
using StepParser.Lexer;

namespace StepParser.Context;

/// <summary>
/// Context snapshot for step-by-step parsing.
/// </summary>
public record StepParsingContextSnapshot
{
    /// <summary>
    /// Gets the scope stack.
    /// </summary>
    public List<ScopeInfo> ScopeStack { get; init; } = new();

    /// <summary>
    /// Gets the symbol context.
    /// </summary>
    public Dictionary<string, SymbolInfo> SymbolContext { get; init; } = new();

    /// <summary>
    /// Gets the parse state.
    /// </summary>
    public ParseStateInfo? ParseState { get; init; }

    /// <summary>
    /// Gets the position.
    /// </summary>
    public CodePosition? Position { get; init; }

    /// <summary>
    /// Gets the hash.
    /// </summary>
    public int Hash { get; init; }
}

/// <summary>
/// Error recovery strategy.
/// </summary>
public record ErrorRecoveryStrategyResult
{
    /// <summary>
    /// Gets a value indicating whether recovery is possible.
    /// </summary>
    public bool CanRecover { get; init; }

    /// <summary>
    /// Gets the recovery strategy.
    /// </summary>
    public string Strategy { get; init; } = "skip";

    /// <summary>
    /// Gets the suggestion.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Gets the confidence level.
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Context integration adapter that bridges step-by-step parsing with context management.
/// </summary>
public class StepParsingContextAdapter
{
    private readonly SymbolTable _symbolTable;
    private string _currentCode = string.Empty;
    private CodePosition _currentPosition = new() { Line = 1, Column = 1, Offset = 0 };
    private ContextInfo? _currentContext;
    private Grammar.Grammar? _activeGrammar;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepParsingContextAdapter"/> class.
    /// </summary>
    public StepParsingContextAdapter()
    {
        _symbolTable = new SymbolTable();
    }

    /// <summary>
    /// Sets the active grammar for context-aware parsing.
    /// </summary>
    /// <param name="grammar">The grammar to set as active.</param>
    public void SetActiveGrammar(Grammar.Grammar grammar)
    {
        _activeGrammar = grammar;
        InitializeGrammarContext(grammar);
    }

    /// <summary>
    /// Initializes context for a parsing session.
    /// </summary>
    /// <param name="sourceLinesContainer">The source lines container.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeForParsingAsync(IParserLexerSourceContainer sourceLinesContainer)
    {
        // Extract code from source lines container
        _currentCode = ExtractCodeFromContainer(sourceLinesContainer);

        // Initialize context with the code
        _currentContext = await CreateContextFromCodeAsync(_currentCode);

        // Reset position
        _currentPosition = new CodePosition { Line = 1, Column = 1, Offset = 0 };
    }

    /// <summary>
    /// Updates context with token information during parsing.
    /// </summary>
    /// <param name="token">The token to process.</param>
    /// <param name="contextSnapshot">Optional context snapshot.</param>
    public void UpdateContext(Token token, StepParsingContextSnapshot? contextSnapshot = null)
    {
        // Update current position based on token
        UpdatePositionWithToken(token);

        // If we have a context snapshot, use it to maintain consistency
        if (contextSnapshot != null)
        {
            SynchronizeWithSnapshot(contextSnapshot);
        }

        // Update the context with the new position
        UpdateContextManager();
    }

    /// <summary>
    /// Gets the current context information.
    /// </summary>
    /// <returns>The current context information.</returns>
    public ContextInfo GetCurrentContext()
    {
        return _currentContext ?? CreateDefaultContext();
    }

    /// <summary>
    /// Gets current scopes for context snapshot.
    /// </summary>
    /// <returns>The current scopes.</returns>
    public List<ScopeInfo> GetCurrentScopes()
    {
        var context = GetCurrentContext();
        return context.ScopeStack.Count > 0 ? context.ScopeStack :
               context.Scope != null ? new List<ScopeInfo> { context.Scope } :
               new List<ScopeInfo>();
    }

    /// <summary>
    /// Gets current parse state.
    /// </summary>
    /// <returns>The current parse state.</returns>
    public ParseStateInfo GetCurrentParseState()
    {
        var context = GetCurrentContext();
        return context.ParseState ?? CreateDefaultParseState();
    }

    /// <summary>
    /// Gets current position.
    /// </summary>
    /// <returns>The current position.</returns>
    public CodePosition GetCurrentPosition()
    {
        return _currentPosition;
    }

    /// <summary>
    /// Computes a hash for the current context.
    /// </summary>
    /// <returns>The context hash.</returns>
    public int ComputeContextHash()
    {
        var context = GetCurrentContext();
        var currentScopes = GetCurrentScopes();

        // Get symbol information from the symbol table
        var symbolTableStats = _symbolTable.GetStatistics();
        var symbolNames = symbolTableStats.SymbolsByScope.Keys.ToList();

        var contextString = System.Text.Json.JsonSerializer.Serialize(new
        {
            scopes = currentScopes.Select(s => s.Id),
            symbols = symbolNames,
            position = _currentPosition
        });

        // Simple hash function
        var hash = 0;
        foreach (var c in contextString)
        {
            hash = ((hash << 5) - hash) + c;
            hash = hash & hash; // Convert to 32-bit integer
        }
        return hash;
    }

    /// <summary>
    /// Checks if a terminal is valid in the current context.
    /// </summary>
    /// <param name="terminal">The terminal to check.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <returns>True if the terminal is valid; otherwise, false.</returns>
    public bool IsTerminalValidInContext(Terminal terminal, ContextInfo contextInfo)
    {
        var currentScopes = GetScopesFromContext(contextInfo);
        var terminalName = terminal.Name;

        // Check if terminal is appropriate for current scope
        foreach (var scope in currentScopes)
        {
            if (IsTerminalValidInScope(terminalName, scope))
            {
                return true;
            }
        }

        // If no specific scope validation, allow by default
        return true;
    }

    /// <summary>
    /// Checks if a production is valid in the current context.
    /// </summary>
    /// <param name="production">The production to check.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <param name="terminal">The terminal being matched.</param>
    /// <returns>True if the production is valid; otherwise, false.</returns>
    public bool IsProductionValidInContext(Production production, ContextInfo contextInfo, Terminal terminal)
    {
        var currentScopes = GetScopesFromContext(contextInfo);
        var productionName = production.Name;

        // Check if production is appropriate for current scope
        foreach (var scope in currentScopes)
        {
            if (IsProductionValidInScope(productionName, scope))
            {
                return true;
            }
        }

        return true; // Allow by default
    }

    /// <summary>
    /// Ranks productions by context relevance.
    /// </summary>
    /// <param name="productions">The productions to rank.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <returns>The ranked productions.</returns>
    public List<Production> RankProductionsByContext(List<Production> productions, ContextInfo contextInfo)
    {
        return productions
            .OrderByDescending(p => ComputeProductionContextScore(p, contextInfo))
            .ToList();
    }

    /// <summary>
    /// Gets error recovery strategy for a token.
    /// </summary>
    /// <param name="token">The token to analyze.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <returns>The error recovery strategy.</returns>
    public ErrorRecoveryStrategyResult GetErrorRecoveryStrategy(Token token, ContextInfo contextInfo)
    {
        var currentScopes = GetScopesFromContext(contextInfo);
        var tokenValue = token.Value;

        // Analyze context to determine recovery strategy
        if (currentScopes.Count > 0)
        {
            var currentScope = currentScopes[^1];

            // If we're in a block scope and encounter unexpected token, try to skip
            if (currentScope.Type == "block" && IsLikelyTypo(tokenValue))
            {
                return new ErrorRecoveryStrategyResult
                {
                    CanRecover = true,
                    Strategy = "skip",
                    Suggestion = $"Skip unexpected token '{tokenValue}'",
                    Confidence = 0.7
                };
            }

            // If we're missing a closing brace, suggest insertion
            if (IsMissingClosingBrace(currentScope, tokenValue))
            {
                return new ErrorRecoveryStrategyResult
                {
                    CanRecover = true,
                    Strategy = "insert",
                    Suggestion = "Insert missing closing brace",
                    Confidence = 0.8
                };
            }
        }

        // Default: cannot recover
        return new ErrorRecoveryStrategyResult
        {
            CanRecover = false,
            Strategy = "skip",
            Confidence = 0.0
        };
    }

    /// <summary>
    /// Updates context with production information.
    /// </summary>
    /// <param name="production">The production that matched.</param>
    /// <param name="token">The token that was matched.</param>
    /// <param name="contextInfo">The context information.</param>
    public void UpdateContextWithProduction(Production production, Token token, ContextInfo contextInfo)
    {
        var productionName = production.Name;

        // Update scope if production represents a scope change
        if (IsProductionScopeChange(productionName))
        {
            HandleScopeChange(production, token, contextInfo);
        }

        // Update symbol information if production defines symbols
        var symbolInfo = ExtractSymbolInfo(production, token, contextInfo);
        if (symbolInfo != null)
        {
            _symbolTable.DeclareSymbol(symbolInfo);
        }
    }

    /// <summary>
    /// Extracts symbol information from production.
    /// </summary>
    /// <param name="production">The production that matched.</param>
    /// <param name="token">The token that was matched.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <returns>The symbol information or null.</returns>
    public SymbolInfo? ExtractSymbolInfo(Production production, Token token, ContextInfo contextInfo)
    {
        var productionName = production.Name;
        var tokenValue = token.Value;

        // Check if this production defines a symbol
        if (IsSymbolDefiningProduction(productionName))
        {
            return new SymbolInfo
            {
                Name = tokenValue,
                Type = InferSymbolType(productionName, tokenValue),
                Kind = InferSymbolKind(productionName),
                Scope = GetCurrentScopeId(contextInfo),
                Position = _currentPosition,
                References = new List<CodePosition>(),
                Definition = _currentPosition,
                Context = GetScopesFromContext(contextInfo).Select(s => s.Name ?? s.Id).ToList()
            };
        }

        return null;
    }

    /// <summary>
    /// Computes confidence for a production in context.
    /// </summary>
    /// <param name="production">The production to analyze.</param>
    /// <param name="contextInfo">The context information.</param>
    /// <returns>The confidence level.</returns>
    public double ComputeProductionConfidence(Production production, ContextInfo contextInfo)
    {
        var productionName = production.Name;
        var currentScopes = GetScopesFromContext(contextInfo);

        var confidence = 0.5; // Base confidence

        // Increase confidence if production matches current scope
        foreach (var scope in currentScopes)
        {
            if (IsProductionExpectedInScope(productionName, scope))
            {
                confidence += 0.3;
            }
        }

        // Increase confidence if production follows expected patterns
        if (IsProductionFollowingPattern(production, contextInfo))
        {
            confidence += 0.2;
        }

        return Math.Min(confidence, 1.0);
    }

    /// <summary>
    /// Updates context state.
    /// </summary>
    /// <param name="contextName">The context name.</param>
    /// <param name="active">Whether the context is active.</param>
    public void UpdateContextState(string contextName, bool active)
    {
        // This could be used to maintain additional context state
        // For now, we'll just log it for debugging purposes
        System.Diagnostics.Debug.WriteLine($"Context state updated: {contextName} = {active}");
    }

    /// <summary>
    /// Resets the context adapter.
    /// </summary>
    public void Reset()
    {
        _currentContext = null;
        _currentPosition = new CodePosition { Line = 1, Column = 1, Offset = 0 };
        _currentCode = string.Empty;
        _symbolTable.Clear();
    }

    // PRIVATE HELPER METHODS

    private void InitializeGrammarContext(Grammar.Grammar grammar)
    {
        // Initialize context based on grammar rules
        // This is a placeholder for grammar-specific context initialization
    }

    private string ExtractCodeFromContainer(IParserLexerSourceContainer container)
    {
        var lines = new List<string>();
        foreach (var line in container.SourceLines)
        {
            lines.Add(line.Content);
        }
        return string.Join("\n", lines);
    }

    private async Task<ContextInfo> CreateContextFromCodeAsync(string code)
    {
        // This would normally parse the code and create context
        // For now, return a basic context
        await Task.Delay(1); // Simulate async operation
        return CreateDefaultContext();
    }

    private void UpdatePositionWithToken(Token token)
    {
        var tokenValue = token.Value;

        // Update position based on token content
        foreach (var c in tokenValue)
        {
            if (c == '\n')
            {
                _currentPosition = _currentPosition with
                {
                    Line = _currentPosition.Line + 1,
                    Column = 1
                };
            }
            else
            {
                _currentPosition = _currentPosition with
                {
                    Column = _currentPosition.Column + 1
                };
            }
            _currentPosition = _currentPosition with
            {
                Offset = _currentPosition.Offset + 1
            };
        }
    }

    private void SynchronizeWithSnapshot(StepParsingContextSnapshot snapshot)
    {
        // Synchronize current state with the provided snapshot
        if (snapshot.Position != null)
        {
            _currentPosition = snapshot.Position;
        }
    }

    private void UpdateContextManager()
    {
        // Update the context manager with current position
        // This is a simplified implementation
    }

    private ContextInfo CreateDefaultContext()
    {
        return new ContextInfo
        {
            Scope = null,
            ScopeStack = new List<ScopeInfo>(),
            ContextStack = new List<string>(),
            Symbols = new List<SymbolInfo>(),
            ParseState = CreateDefaultParseState(),
            ActiveGrammar = null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private ParseStateInfo CreateDefaultParseState()
    {
        return new ParseStateInfo
        {
            CurrentRule = "start",
            Position = _currentPosition,
            ContextStack = new List<string>(),
            ScopeStack = new List<ScopeInfo>(),
            ValidTerminals = new List<string>(),
            Errors = new List<string>(),
            Warnings = new List<string>(),
            Suggestions = new List<string>(),
            GrammarState = new GrammarState
            {
                ActiveGrammar = "minotaur",
                FormatType = GrammarFormatType.CEBNF,
                BaseGrammars = new List<string>(),
                ActiveRules = new List<string>(),
                ContextModifiers = new List<string>(),
                InheritanceChain = new List<string>()
            }
        };
    }

    private List<ScopeInfo> GetScopesFromContext(ContextInfo contextInfo)
    {
        return contextInfo.ScopeStack.Count > 0 ? contextInfo.ScopeStack :
               contextInfo.Scope != null ? new List<ScopeInfo> { contextInfo.Scope } :
               new List<ScopeInfo>();
    }

    private bool IsTerminalValidInScope(string terminalName, ScopeInfo scope)
    {
        // Basic scope validation logic
        return true; // Allow by default
    }

    private bool IsProductionValidInScope(string productionName, ScopeInfo scope)
    {
        // Basic scope validation logic
        return true; // Allow by default
    }

    private double ComputeProductionContextScore(Production production, ContextInfo contextInfo)
    {
        // Compute relevance score for production in current context
        return 0.5; // Default score
    }

    private bool IsLikelyTypo(string tokenValue)
    {
        // Simple heuristic for detecting typos
        return tokenValue.Length == 1 && !char.IsLetterOrDigit(tokenValue[0]) && !char.IsWhiteSpace(tokenValue[0]);
    }

    private bool IsMissingClosingBrace(ScopeInfo scope, string tokenValue)
    {
        // Check if we're missing a closing brace
        return scope.Type == "block" && tokenValue == "}";
    }

    private bool IsProductionScopeChange(string productionName)
    {
        // Check if production represents a scope change
        var scopeChangeProductions = new[] { "block_start", "function_start", "class_start" };
        return scopeChangeProductions.Contains(productionName);
    }

    private void HandleScopeChange(Production production, Token token, ContextInfo contextInfo)
    {
        // Handle scope changes
        // This is a placeholder for scope management logic
    }

    private bool IsSymbolDefiningProduction(string productionName)
    {
        // Check if production defines a symbol
        var symbolDefiningProductions = new[] { "variable_declaration", "function_declaration", "class_declaration" };
        return symbolDefiningProductions.Contains(productionName);
    }

    private string InferSymbolType(string productionName, string tokenValue)
    {
        // Infer symbol type from production and token
        if (productionName.Contains("function"))
        {
            return "function";
        }
        if (productionName.Contains("class"))
        {
            return "class";
        }
        return "variable";
    }

    private string InferSymbolKind(string productionName)
    {
        // Infer symbol kind from production
        if (productionName.Contains("function"))
        {
            return "FUNCTION";
        }
        if (productionName.Contains("class"))
        {
            return "CLASS";
        }
        return "VARIABLE";
    }

    private string GetCurrentScopeId(ContextInfo contextInfo)
    {
        var scopes = GetScopesFromContext(contextInfo);
        return scopes.Count > 0 ? scopes[^1].Id : "global";
    }

    private bool IsProductionExpectedInScope(string productionName, ScopeInfo scope)
    {
        // Check if production is expected in the given scope
        return true; // Allow by default
    }

    private bool IsProductionFollowingPattern(Production production, ContextInfo contextInfo)
    {
        // Check if production follows expected patterns
        return true; // Allow by default
    }
}