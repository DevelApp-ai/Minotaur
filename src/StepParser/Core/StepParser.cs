using StepParser.Context;
using StepParser.Grammar;
using StepParser.Lexer;
using StepParser.Memory;
using StepParser.Utils;

namespace StepParser.Core;

/// <summary>
/// Context-aware parser path with additional state for context tracking.
/// </summary>
public class ContextAwareParserPath : ParserPath
{
    private StepParsingContextSnapshot? _contextSnapshot;
    private readonly List<ScopeInfo> _scopeStack;
    private readonly Dictionary<string, SymbolInfo> _symbolContext;
    private double _score;
    private double _confidence;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAwareParserPath"/> class.
    /// </summary>
    public ContextAwareParserPath() : base()
    {
        _scopeStack = new List<ScopeInfo>();
        _symbolContext = new Dictionary<string, SymbolInfo>();
        _score = 0.0;
        _confidence = 1.0;
    }

    /// <summary>
    /// Gets or sets the context snapshot.
    /// </summary>
    public StepParsingContextSnapshot? ContextSnapshot
    {
        get => _contextSnapshot;
        set => _contextSnapshot = value;
    }

    /// <summary>
    /// Gets the scope stack.
    /// </summary>
    public List<ScopeInfo> ScopeStack => _scopeStack;

    /// <summary>
    /// Gets the symbol context.
    /// </summary>
    public Dictionary<string, SymbolInfo> SymbolContext => _symbolContext;

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    public double Score
    {
        get => _score;
        set => _score = value;
    }

    /// <summary>
    /// Gets or sets the confidence.
    /// </summary>
    public double Confidence
    {
        get => _confidence;
        set => _confidence = value;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _contextSnapshot = null;
        _scopeStack.Clear();
        _symbolContext.Clear();
        _score = 0.0;
        _confidence = 1.0;
    }
}

/// <summary>
/// Factory for creating context-aware parser paths.
/// </summary>
public class ContextAwareParserPathFactory : IObjectFactory<ContextAwareParserPath>
{
    /// <inheritdoc/>
    public ContextAwareParserPath Create()
    {
        return new ContextAwareParserPath();
    }

    /// <inheritdoc/>
    public void Reset(ContextAwareParserPath obj)
    {
        obj.Reset();
    }

    /// <inheritdoc/>
    public bool Validate(ContextAwareParserPath obj)
    {
        return obj.ParserPathId != 0;
    }
}

/// <summary>
/// Unified StepParser class with context-aware and optimized capabilities.
/// Maintains backward compatibility while providing advanced features.
/// </summary>
public class StepParser : IDisposable
{
    // Zero-copy infrastructure
    private readonly MemoryArena _arena;
    private readonly ObjectPool<ContextAwareParserPath> _pathPool;

    // Context integration
    private readonly StepParsingContextAdapter _contextAdapter;
    private readonly SymbolTable _symbolTable;

    // Grammar and parsing state
    private string _activeGrammarName;
    private Grammar.Grammar? _grammar;
    private readonly Dictionary<int, List<IProductionPart>> _activeProductionPartsForLexerPath;
    private readonly Dictionary<int, ContextAwareParserPath> _parserPaths;
    private int _maxParserPathId;
    private readonly Dictionary<string, bool> _contextStates;

    // Managers for advanced features
    private readonly SemanticActionManager _semanticActionManager;
    private readonly PrecedenceManager _precedenceManager;
    private readonly InheritanceResolver _inheritanceResolver;

    // Callback support
    private readonly Dictionary<string, Action<Dictionary<string, object>>> _callbackRegistry;
    private readonly Dictionary<string, object> _callbackContext;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepParser"/> class.
    /// </summary>
    public StepParser()
    {
        // Initialize zero-copy infrastructure
        _arena = new MemoryArena(4 * 1024 * 1024); // 4MB initial size

        var pathFactory = new ContextAwareParserPathFactory();
        _pathPool = new ObjectPool<ContextAwareParserPath>(pathFactory, _arena, 200, 2000);

        // Initialize context integration
        _contextAdapter = new StepParsingContextAdapter();
        _symbolTable = new SymbolTable();

        // Initialize managers
        _inheritanceResolver = new InheritanceResolver();
        _semanticActionManager = new SemanticActionManager(_inheritanceResolver);
        _precedenceManager = new PrecedenceManager(_inheritanceResolver);

        // Initialize parsing state
        _activeGrammarName = string.Empty;
        _grammar = null;
        _activeProductionPartsForLexerPath = new Dictionary<int, List<IProductionPart>>();
        _parserPaths = new Dictionary<int, ContextAwareParserPath>();
        _maxParserPathId = 0;
        _contextStates = new Dictionary<string, bool>();

        // Initialize callback support
        _callbackRegistry = new Dictionary<string, Action<Dictionary<string, object>>>();
        _callbackContext = new Dictionary<string, object>();

        Reset();
    }

    /// <summary>
    /// Gets the active grammar name.
    /// </summary>
    public string ActiveGrammarName => _activeGrammarName;

    /// <summary>
    /// Sets the active grammar for parsing.
    /// </summary>
    /// <param name="grammar">The grammar to set as active.</param>
    public void SetActiveGrammar(Grammar.Grammar grammar)
    {
        _grammar = grammar;
        _activeGrammarName = grammar.Name;

        // Register grammar with inheritance resolver
        _inheritanceResolver.RegisterGrammar(grammar);

        // Update context adapter with new grammar
        _contextAdapter.SetActiveGrammar(grammar);

        // Clear existing parser paths as they're specific to the previous grammar
        Reset();
    }

    /// <summary>
    /// Gets valid terminals for a lexer path.
    /// </summary>
    /// <param name="lexerPathId">The ID of the lexer path.</param>
    /// <returns>The valid terminals.</returns>
    public IEnumerable<Terminal> GetValidTerminalsForLexerPath(int lexerPathId)
    {
        if (!_activeProductionPartsForLexerPath.TryGetValue(lexerPathId, out var parts))
        {
            if (_grammar != null)
            {
                var startTerminals = _grammar.ValidStartTerminals;
                return FilterTerminalsWithContext(startTerminals, lexerPathId);
            }
            return Enumerable.Empty<Terminal>();
        }

        var terminals = new List<Terminal>();
        foreach (var part in parts)
        {
            if (part.Type == ProductionPartType.Terminal)
            {
                terminals.Add((Terminal)part);
            }
        }

        return FilterTerminalsWithContext(terminals, lexerPathId);
    }

    /// <summary>
    /// Gets valid terminals for a lexer path (compatibility method).
    /// </summary>
    /// <param name="lexerPath">The lexer path.</param>
    /// <returns>The valid terminals.</returns>
    public IEnumerable<Terminal> GetValidTerminals(LexerPath lexerPath)
    {
        // Get active productions from the parser path
        var path = _parserPaths.Values.FirstOrDefault(p => p.LexerPathId == lexerPath.LexerPathId);
        if (path?.ActiveProductions == null)
        {
            return Enumerable.Empty<Terminal>();
        }

        var validTerminals = new List<Terminal>();
        foreach (var production in path.ActiveProductions)
        {
            var parts = production.Parts;
            if (parts.Count > 0)
            {
                var firstPart = parts[0];
                if (firstPart.Type == ProductionPartType.Terminal)
                {
                    validTerminals.Add((Terminal)firstPart);
                }
            }
        }

        return validTerminals;
    }

    /// <summary>
    /// Parses input using the specified grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar to use.</param>
    /// <param name="sourceLinesContainer">The source lines container.</param>
    /// <returns>The parse results.</returns>
    public async Task<List<ProductionMatch>> ParseAsync(
        string grammarName,
        IParserLexerSourceContainer sourceLinesContainer)
    {
        if (_grammar == null || _grammar.Name != grammarName)
        {
            throw new InvalidOperationException($"Grammar {grammarName} not set as active");
        }

        Reset();

        // Initialize context for parsing
        await _contextAdapter.InitializeForParsingAsync(sourceLinesContainer);

        // Create lexer
        var lexerOptions = new LexerOptions();
        using var lexer = new StepLexer(this, lexerOptions, sourceLinesContainer);

        // Process tokens with context awareness
        foreach (var tokens in lexer.NextTokens())
        {
            ProcessTokensWithContext(tokens);
        }

        // Collect results
        var matches = new List<ProductionMatch>();
        foreach (var path in _parserPaths.Values)
        {
            matches.AddRange(path.ActiveMatches);
        }

        return matches;
    }

    /// <summary>
    /// Sets a context state.
    /// </summary>
    /// <param name="contextName">The name of the context.</param>
    /// <param name="active">Whether the context is active.</param>
    public void SetContextState(string contextName, bool active)
    {
        _contextStates[contextName] = active;
        _contextAdapter.UpdateContextState(contextName, active);
    }

    /// <summary>
    /// Gets a context state.
    /// </summary>
    /// <param name="contextName">The name of the context.</param>
    /// <returns>Whether the context is active.</returns>
    public bool GetContextState(string contextName)
    {
        return _contextStates.TryGetValue(contextName, out var active) && active;
    }

    // ============================================================================
    // CALLBACK SUPPORT METHODS
    // ============================================================================

    /// <summary>
    /// Registers a callback for a specific production.
    /// </summary>
    /// <param name="productionName">The name of the production.</param>
    /// <param name="callback">The callback function to execute.</param>
    public void RegisterCallback(string productionName, Action<Dictionary<string, object>> callback)
    {
        _callbackRegistry[productionName] = callback;
    }

    /// <summary>
    /// Removes a callback for a specific production.
    /// </summary>
    /// <param name="productionName">The name of the production.</param>
    public void UnregisterCallback(string productionName)
    {
        _callbackRegistry.Remove(productionName);
    }

    /// <summary>
    /// Sets the callback context.
    /// </summary>
    /// <param name="context">The context to set.</param>
    public void SetCallbackContext(Dictionary<string, object> context)
    {
        foreach (var kvp in context)
        {
            _callbackContext[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Gets the callback context.
    /// </summary>
    /// <returns>The current callback context.</returns>
    public Dictionary<string, object> GetCallbackContext()
    {
        return new Dictionary<string, object>(_callbackContext);
    }

    /// <summary>
    /// Clears all registered callbacks.
    /// </summary>
    public void ClearCallbacks()
    {
        _callbackRegistry.Clear();
    }

    /// <summary>
    /// Clears the callback context.
    /// </summary>
    public void ClearCallbackContext()
    {
        _callbackContext.Clear();
    }

    // ============================================================================
    // PRIVATE METHODS
    // ============================================================================

    /// <summary>
    /// Resets the parser state.
    /// </summary>
    private void Reset()
    {
        // Clear existing paths and return them to pool
        foreach (var path in _parserPaths.Values)
        {
            _pathPool.Release(path);
        }
        _parserPaths.Clear();

        _activeProductionPartsForLexerPath.Clear();
        _maxParserPathId = 0;
        _contextStates.Clear();

        // Reset context components
        _contextAdapter.Reset();
        _symbolTable.Clear();
    }

    /// <summary>
    /// Filters terminals based on current context and lexer path.
    /// </summary>
    /// <param name="terminals">The terminals to filter.</param>
    /// <param name="lexerPathId">The lexer path ID.</param>
    /// <returns>The filtered terminals.</returns>
    private IEnumerable<Terminal> FilterTerminalsWithContext(IEnumerable<Terminal> terminals, int lexerPathId)
    {
        var currentContext = _contextAdapter.GetCurrentContext();

        // Get the parser path associated with this lexer path
        var associatedParserPath = _parserPaths.Values
            .FirstOrDefault(path => path.LexerPathId == lexerPathId);

        return terminals.Where(terminal =>
        {
            // Use basic context validation
            var isValidInContext = _contextAdapter.IsTerminalValidInContext(terminal, currentContext);

            // Add path-specific validation if we have an associated parser path
            if (associatedParserPath?.ActiveProductions.Count > 0)
            {
                var activeProductions = associatedParserPath.ActiveProductions;
                var isCompatibleWithPath = activeProductions.Any(prod =>
                    _contextAdapter.IsProductionValidInContext(prod, currentContext, terminal));
                return isValidInContext && isCompatibleWithPath;
            }

            return isValidInContext;
        });
    }

    /// <summary>
    /// Processes tokens with context awareness.
    /// </summary>
    /// <param name="tokens">The tokens to process.</param>
    private void ProcessTokensWithContext(List<Token> tokens)
    {
        foreach (var token in tokens)
        {
            ProcessTokenWithContext(token);
        }
    }

    /// <summary>
    /// Processes a single token with context awareness.
    /// </summary>
    /// <param name="token">The token to process.</param>
    private void ProcessTokenWithContext(Token token)
    {
        var lexerPathId = token.LexerPathId;

        // Handle special lexer tokens
        if (token.Terminal.Name == "LEXERPATH_REMOVED")
        {
            HandleLexerPathRemoval(lexerPathId, token.Value);
            return;
        }

        if (token.Terminal.Name == "LEXERPATH_MERGE")
        {
            if (int.TryParse(token.Value, out var targetLexerPathId))
            {
                HandleLexerPathMerge(lexerPathId, targetLexerPathId);
            }
            return;
        }

        // Get parser paths for this lexer path
        var parserPathsToProcess = _parserPaths.Values
            .Where(path => path.LexerPathId == lexerPathId)
            .ToList();

        // If no parser paths exist for this lexer path, create one
        if (parserPathsToProcess.Count == 0)
        {
            var newPath = CreateContextAwareParserPath(lexerPathId, 0);
            parserPathsToProcess.Add(newPath);
        }

        // Process token for each parser path with context awareness
        foreach (var path in parserPathsToProcess)
        {
            ProcessTokenForContextAwarePath(path, token);
        }
    }

    /// <summary>
    /// Creates a new context-aware parser path using object pooling.
    /// </summary>
    /// <param name="lexerPathId">The lexer path ID.</param>
    /// <param name="position">The position.</param>
    /// <returns>The created parser path.</returns>
    private ContextAwareParserPath CreateContextAwareParserPath(int lexerPathId, int position)
    {
        var path = _pathPool.Acquire();
        path.ParserPathId = ++_maxParserPathId;
        path.LexerPathId = lexerPathId;
        path.Position = position;

        // Initialize context snapshot
        var contextSnapshot = new StepParsingContextSnapshot
        {
            ScopeStack = _contextAdapter.GetCurrentScopes(),
            SymbolContext = new Dictionary<string, SymbolInfo>(),
            ParseState = _contextAdapter.GetCurrentParseState(),
            Position = _contextAdapter.GetCurrentPosition(),
            Hash = _contextAdapter.ComputeContextHash()
        };
        path.ContextSnapshot = contextSnapshot;

        _parserPaths[path.ParserPathId] = path;
        return path;
    }

    /// <summary>
    /// Processes a token for a specific context-aware parser path.
    /// </summary>
    /// <param name="path">The parser path.</param>
    /// <param name="token">The token to process.</param>
    private void ProcessTokenForContextAwarePath(ContextAwareParserPath path, Token token)
    {
        // Update context before processing
        _contextAdapter.UpdateContext(token, path.ContextSnapshot);

        var contextInfo = _contextAdapter.GetCurrentContext();
        var terminal = token.Terminal;
        var activeProductions = path.ActiveProductions;

        // If no active productions, start with grammar's start productions
        if (activeProductions.Count == 0 && _grammar != null)
        {
            foreach (var prod in _grammar.StartProductions)
            {
                path.AddActiveProduction(prod);
            }
        }

        // Try to match token against active productions with context awareness
        var matchingProductions = new List<Production>();

        foreach (var prod in activeProductions)
        {
            if (_contextAdapter.IsProductionValidInContext(prod, contextInfo, terminal))
            {
                var parts = prod.Parts;
                if (parts.Count > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.Type == ProductionPartType.Terminal)
                    {
                        var terminalPart = (Terminal)firstPart;
                        if (terminalPart.Name == terminal.Name)
                        {
                            matchingProductions.Add(prod);
                        }
                    }
                }
            }
        }

        // Handle results with context-aware processing
        if (matchingProductions.Count == 0)
        {
            HandleContextAwareParsingError(path, token, contextInfo);
        }
        else if (matchingProductions.Count > 1)
        {
            HandleContextAwareAmbiguity(path, token, matchingProductions, contextInfo);
        }
        else
        {
            ProcessMatchingProductionWithContext(path, token, matchingProductions[0], contextInfo);
        }
    }

    /// <summary>
    /// Handles parsing errors with context awareness.
    /// </summary>
    /// <param name="path">The parser path.</param>
    /// <param name="token">The token that caused the error.</param>
    /// <param name="contextInfo">The context information.</param>
    private void HandleContextAwareParsingError(ContextAwareParserPath path, Token token, ContextInfo contextInfo)
    {
        var recoveryStrategy = _contextAdapter.GetErrorRecoveryStrategy(token, contextInfo);

        if (recoveryStrategy.CanRecover)
        {
            ApplyErrorRecovery(path, token, recoveryStrategy);
        }
        else
        {
            _parserPaths.Remove(path.ParserPathId);
            _pathPool.Release(path);
        }
    }

    /// <summary>
    /// Handles ambiguity with context awareness.
    /// </summary>
    /// <param name="path">The parser path.</param>
    /// <param name="token">The token being processed.</param>
    /// <param name="matchingProductions">The matching productions.</param>
    /// <param name="contextInfo">The context information.</param>
    private void HandleContextAwareAmbiguity(
        ContextAwareParserPath path,
        Token token,
        List<Production> matchingProductions,
        ContextInfo contextInfo)
    {
        // Use context to rank productions
        var rankedProductions = _contextAdapter.RankProductionsByContext(matchingProductions, contextInfo);

        // Use the highest-ranked production for the current path
        ProcessMatchingProductionWithContext(path, token, rankedProductions[0], contextInfo);

        // Create new paths for other high-ranking productions (limit to top 3)
        for (int i = 1; i < Math.Min(rankedProductions.Count, 3); i++)
        {
            var newPath = CreateContextAwareParserPath(path.LexerPathId, path.Position);

            // Copy context from original path
            newPath.ContextSnapshot = path.ContextSnapshot;

            // Copy active productions from original path
            foreach (var prod in path.ActiveProductions)
            {
                if (prod != rankedProductions[i])
                {
                    newPath.AddActiveProduction(prod);
                }
            }

            // Copy active matches from original path
            foreach (var match in path.ActiveMatches)
            {
                newPath.AddActiveMatch(match);
            }

            // Process the production for this new path
            ProcessMatchingProductionWithContext(newPath, token, rankedProductions[i], contextInfo);
        }
    }

    /// <summary>
    /// Processes a matching production with context awareness.
    /// </summary>
    /// <param name="path">The parser path.</param>
    /// <param name="token">The token being processed.</param>
    /// <param name="production">The matching production.</param>
    /// <param name="contextInfo">The context information.</param>
    private void ProcessMatchingProductionWithContext(
        ContextAwareParserPath path,
        Token token,
        Production production,
        ContextInfo contextInfo)
    {
        // Create a match for this token
        var match = new ProductionMatch(
            production,
            token.Value,
            path.Position,
            path.Position + token.Value.Length);

        // Update path position
        path.Position += token.Value.Length;

        // Add match to path
        path.AddActiveMatch(match);

        // Update context with production information
        _contextAdapter.UpdateContextWithProduction(production, token, contextInfo);

        // Update symbol table if this production defines symbols
        UpdateSymbolTableWithProduction(production, token, contextInfo);

        // Execute callbacks with rich context
        ExecuteCallbacksForProduction(production, token, path, contextInfo);

        // Execute legacy callback if exists
        production.ExecuteCallback(token.Value, _contextStates, path.Position);

        // Update active productions
        path.ActiveProductions.Remove(production);

        // If production has more parts, add them to active parts for this lexer path
        var parts = production.Parts;
        if (parts.Count > 1)
        {
            var remainingParts = parts.Skip(1).ToList();
            _activeProductionPartsForLexerPath[path.LexerPathId] = remainingParts;
        }
        else
        {
            // Production completed, remove from active parts
            _activeProductionPartsForLexerPath.Remove(path.LexerPathId);
        }

        // Update path score based on context confidence
        var confidence = _contextAdapter.ComputeProductionConfidence(production, contextInfo);
        path.Confidence = confidence;
        path.Score += confidence;
    }

    /// <summary>
    /// Updates the symbol table with production information.
    /// </summary>
    /// <param name="production">The production.</param>
    /// <param name="token">The token.</param>
    /// <param name="contextInfo">The context information.</param>
    private void UpdateSymbolTableWithProduction(Production production, Token token, ContextInfo contextInfo)
    {
        var symbolInfo = _contextAdapter.ExtractSymbolInfo(production, token, contextInfo);
        if (symbolInfo != null)
        {
            _symbolTable.DefineSymbol(symbolInfo);
        }
    }

    /// <summary>
    /// Executes registered callbacks for a production with rich context.
    /// </summary>
    /// <param name="production">The production.</param>
    /// <param name="token">The token.</param>
    /// <param name="path">The parser path.</param>
    /// <param name="contextInfo">The context information.</param>
    private void ExecuteCallbacksForProduction(
        Production production,
        Token token,
        ContextAwareParserPath path,
        ContextInfo contextInfo)
    {
        var productionName = production.Name;
        if (_callbackRegistry.TryGetValue(productionName, out var callback))
        {
            try
            {
                // Create rich callback context
                var callbackContextInfo = new Dictionary<string, object>
                {
                    ["token"] = token.Value,
                    ["position"] = path.Position,
                    ["contextInfo"] = contextInfo,
                    ["symbolTable"] = _symbolTable,
                    ["customContext"] = _callbackContext,
                    ["production"] = productionName,
                    ["grammarName"] = _activeGrammarName
                };

                // Execute callback with rich context
                callback(callbackContextInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Callback error for production {productionName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies error recovery strategy.
    /// </summary>
    /// <param name="path">The parser path.</param>
    /// <param name="token">The token.</param>
    /// <param name="recoveryStrategy">The recovery strategy.</param>
    private void ApplyErrorRecovery(ContextAwareParserPath path, Token token, ErrorRecoveryStrategyResult recoveryStrategy)
    {
        switch (recoveryStrategy.Strategy)
        {
            case "skip":
                System.Diagnostics.Debug.WriteLine($"Skipping token {token.Value} at position {path.Position}");
                path.Position += token.Value.Length;
                break;
            case "backtrack":
                System.Diagnostics.Debug.WriteLine($"Backtracking from token {token.Value} at position {path.Position}");
                _parserPaths.Remove(path.ParserPathId);
                _pathPool.Release(path);
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Applying {recoveryStrategy.Strategy} recovery for token {token.Value} at position {path.Position}");
                break;
        }
    }

    /// <summary>
    /// Handles lexer path removal.
    /// </summary>
    /// <param name="lexerPathId">The lexer path ID.</param>
    /// <param name="reason">The reason for removal.</param>
    private void HandleLexerPathRemoval(int lexerPathId, string reason)
    {
        System.Diagnostics.Debug.WriteLine($"Removing lexer path {lexerPathId} - reason: {reason}");

        var pathsToRemove = _parserPaths.Values
            .Where(path => path.LexerPathId == lexerPathId)
            .ToList();

        foreach (var path in pathsToRemove)
        {
            _parserPaths.Remove(path.ParserPathId);
            _pathPool.Release(path);
        }

        _activeProductionPartsForLexerPath.Remove(lexerPathId);
    }

    /// <summary>
    /// Handles lexer path merge.
    /// </summary>
    /// <param name="lexerPathId">The source lexer path ID.</param>
    /// <param name="targetLexerPathId">The target lexer path ID.</param>
    private void HandleLexerPathMerge(int lexerPathId, int targetLexerPathId)
    {
        // Update all parser paths that reference the merged lexer path
        foreach (var path in _parserPaths.Values)
        {
            if (path.LexerPathId == lexerPathId)
            {
                path.LexerPathId = targetLexerPathId;
            }
        }

        // Merge active production parts
        if (_activeProductionPartsForLexerPath.TryGetValue(lexerPathId, out var sourceParts))
        {
            if (_activeProductionPartsForLexerPath.TryGetValue(targetLexerPathId, out var targetParts))
            {
                // Merge the parts (simple concatenation)
                _activeProductionPartsForLexerPath[targetLexerPathId] = targetParts.Concat(sourceParts).ToList();
            }
            else
            {
                _activeProductionPartsForLexerPath[targetLexerPathId] = sourceParts;
            }
        }

        _activeProductionPartsForLexerPath.Remove(lexerPathId);
    }

    /// <summary>
    /// Releases all resources used by the step parser.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _pathPool?.Dispose();
            _arena?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}