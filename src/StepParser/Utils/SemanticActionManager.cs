using StepParser.Grammar;

namespace StepParser.Utils;

/// <summary>
/// Represents a semantic action.
/// </summary>
public record SemanticAction
{
    /// <summary>
    /// Gets the action name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the action implementation.
    /// </summary>
    public Action<Dictionary<string, object>>? Implementation { get; init; }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    public List<string> Parameters { get; init; } = new();

    /// <summary>
    /// Gets the return type.
    /// </summary>
    public string ReturnType { get; init; } = "void";

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the grammar scope.
    /// </summary>
    public string GrammarScope { get; init; } = string.Empty;
}

/// <summary>
/// Manages semantic actions and their inheritance relationships.
/// </summary>
public class SemanticActionManager
{
    private readonly Dictionary<string, SemanticAction> _actionRegistry;
    private readonly Dictionary<string, Dictionary<string, SemanticAction>> _actionCache;
    private readonly InheritanceResolver _inheritanceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticActionManager"/> class.
    /// </summary>
    /// <param name="inheritanceResolver">The inheritance resolver.</param>
    public SemanticActionManager(InheritanceResolver inheritanceResolver)
    {
        _actionRegistry = new Dictionary<string, SemanticAction>();
        _actionCache = new Dictionary<string, Dictionary<string, SemanticAction>>();
        _inheritanceResolver = inheritanceResolver;
    }

    /// <summary>
    /// Registers a semantic action.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="action">The semantic action.</param>
    public void RegisterAction(string grammarName, string actionName, SemanticAction action)
    {
        var key = CreateActionKey(grammarName, actionName);
        _actionRegistry[key] = action;

        // Clear cache for affected grammars
        ClearCacheForGrammar(grammarName);
    }

    /// <summary>
    /// Gets a semantic action with inheritance resolution.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>The resolved semantic action or null if not found.</returns>
    public SemanticAction? GetAction(string grammarName, string actionName)
    {
        // Check cache first
        if (_actionCache.TryGetValue(grammarName, out var grammarCache) &&
            grammarCache.TryGetValue(actionName, out var cachedAction))
        {
            return cachedAction;
        }

        // Resolve action with inheritance
        var resolvedAction = ResolveAction(grammarName, actionName);

        // Cache the result
        if (!_actionCache.ContainsKey(grammarName))
        {
            _actionCache[grammarName] = new Dictionary<string, SemanticAction>();
        }
        
        if (resolvedAction != null)
        {
            _actionCache[grammarName][actionName] = resolvedAction;
        }

        return resolvedAction;
    }

    /// <summary>
    /// Gets all actions for a grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>All actions for the grammar.</returns>
    public Dictionary<string, SemanticAction> GetActionsForGrammar(string grammarName)
    {
        var actions = new Dictionary<string, SemanticAction>();

        // Get actions directly defined for this grammar
        foreach (var kvp in _actionRegistry)
        {
            if (kvp.Key.StartsWith($"{grammarName}::"))
            {
                var actionName = kvp.Key.Substring($"{grammarName}::".Length);
                actions[actionName] = kvp.Value;
            }
        }

        // Get inherited actions
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            foreach (var kvp in _actionRegistry)
            {
                if (kvp.Key.StartsWith($"{baseGrammarName}::"))
                {
                    var actionName = kvp.Key.Substring($"{baseGrammarName}::".Length);
                    // Only add if not already defined in derived grammar
                    if (!actions.ContainsKey(actionName))
                    {
                        actions[actionName] = kvp.Value;
                    }
                }
            }
        }

        return actions;
    }

    /// <summary>
    /// Removes a semantic action.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>True if the action was removed; otherwise, false.</returns>
    public bool RemoveAction(string grammarName, string actionName)
    {
        var key = CreateActionKey(grammarName, actionName);
        var removed = _actionRegistry.Remove(key);

        if (removed)
        {
            ClearCacheForGrammar(grammarName);
        }

        return removed;
    }

    /// <summary>
    /// Clears all actions.
    /// </summary>
    public void Clear()
    {
        _actionRegistry.Clear();
        _actionCache.Clear();
    }

    /// <summary>
    /// Gets statistics about the semantic action manager.
    /// </summary>
    /// <returns>Statistics information.</returns>
    public SemanticActionStatistics GetStatistics()
    {
        var grammarCount = _actionRegistry.Keys
            .Select(key => key.Split("::")[0])
            .Distinct()
            .Count();

        return new SemanticActionStatistics
        {
            TotalActions = _actionRegistry.Count,
            GrammarCount = grammarCount,
            CachedGrammars = _actionCache.Count
        };
    }

    private string CreateActionKey(string grammarName, string actionName)
    {
        return $"{grammarName}::{actionName}";
    }

    private void ClearCacheForGrammar(string grammarName)
    {
        _actionCache.Remove(grammarName);

        // Also clear cache for grammars that inherit from this one
        var dependentGrammars = _inheritanceResolver.GetDependentGrammars(grammarName);
        foreach (var dependentGrammar in dependentGrammars)
        {
            _actionCache.Remove(dependentGrammar);
        }
    }

    private SemanticAction? ResolveAction(string grammarName, string actionName)
    {
        // Try to find action in current grammar
        var key = CreateActionKey(grammarName, actionName);
        if (_actionRegistry.TryGetValue(key, out var action))
        {
            return action;
        }

        // Try to find action in base grammars
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            key = CreateActionKey(baseGrammarName, actionName);
            if (_actionRegistry.TryGetValue(key, out action))
            {
                return action;
            }
        }

        return null;
    }
}

/// <summary>
/// Statistics about semantic actions.
/// </summary>
public record SemanticActionStatistics
{
    /// <summary>
    /// Gets the total number of actions.
    /// </summary>
    public int TotalActions { get; init; }

    /// <summary>
    /// Gets the number of grammars with actions.
    /// </summary>
    public int GrammarCount { get; init; }

    /// <summary>
    /// Gets the number of cached grammars.
    /// </summary>
    public int CachedGrammars { get; init; }
}