using StepParser.Grammar;

namespace StepParser.Utils;

/// <summary>
/// Resolves inheritance relationships between grammars.
/// </summary>
public class InheritanceResolver
{
    private readonly Dictionary<string, Grammar.Grammar> _grammars;
    private readonly Dictionary<string, List<string>> _inheritanceChainCache;
    private readonly Dictionary<string, List<string>> _dependentsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="InheritanceResolver"/> class.
    /// </summary>
    public InheritanceResolver()
    {
        _grammars = new Dictionary<string, Grammar.Grammar>();
        _inheritanceChainCache = new Dictionary<string, List<string>>();
        _dependentsCache = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Registers a grammar for inheritance resolution.
    /// </summary>
    /// <param name="grammar">The grammar to register.</param>
    public void RegisterGrammar(Grammar.Grammar grammar)
    {
        _grammars[grammar.Name] = grammar;
        
        // Clear cache for this grammar and its dependents
        ClearCacheForGrammar(grammar.Name);
    }

    /// <summary>
    /// Unregisters a grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar to unregister.</param>
    /// <returns>True if the grammar was unregistered; otherwise, false.</returns>
    public bool UnregisterGrammar(string grammarName)
    {
        var removed = _grammars.Remove(grammarName);
        if (removed)
        {
            ClearCacheForGrammar(grammarName);
        }
        return removed;
    }

    /// <summary>
    /// Gets the inheritance chain for a grammar (from most derived to most base).
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>The inheritance chain.</returns>
    public List<string> GetInheritanceChain(string grammarName)
    {
        // Check cache first
        if (_inheritanceChainCache.TryGetValue(grammarName, out var cachedChain))
        {
            return new List<string>(cachedChain);
        }

        var chain = new List<string>();
        var visited = new HashSet<string>();
        BuildInheritanceChain(grammarName, chain, visited);

        // Cache the result
        _inheritanceChainCache[grammarName] = new List<string>(chain);

        return chain;
    }

    /// <summary>
    /// Gets the grammars that depend on the specified grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>The dependent grammars.</returns>
    public List<string> GetDependentGrammars(string grammarName)
    {
        // Check cache first
        if (_dependentsCache.TryGetValue(grammarName, out var cachedDependents))
        {
            return new List<string>(cachedDependents);
        }

        var dependents = new List<string>();

        foreach (var grammar in _grammars.Values)
        {
            if (grammar.BaseGrammars.Contains(grammarName))
            {
                dependents.Add(grammar.Name);
                
                // Recursively get dependents of dependents
                var transitiveDependents = GetDependentGrammars(grammar.Name);
                dependents.AddRange(transitiveDependents);
            }
        }

        // Remove duplicates
        dependents = dependents.Distinct().ToList();

        // Cache the result
        _dependentsCache[grammarName] = new List<string>(dependents);

        return dependents;
    }

    /// <summary>
    /// Checks if a grammar inherits from another grammar.
    /// </summary>
    /// <param name="derivedGrammarName">The derived grammar name.</param>
    /// <param name="baseGrammarName">The base grammar name.</param>
    /// <returns>True if the derived grammar inherits from the base grammar; otherwise, false.</returns>
    public bool InheritsFrom(string derivedGrammarName, string baseGrammarName)
    {
        var inheritanceChain = GetInheritanceChain(derivedGrammarName);
        return inheritanceChain.Contains(baseGrammarName);
    }

    /// <summary>
    /// Gets the most derived common base grammar for multiple grammars.
    /// </summary>
    /// <param name="grammarNames">The grammar names.</param>
    /// <returns>The most derived common base grammar, or null if none found.</returns>
    public string? GetCommonBaseGrammar(IEnumerable<string> grammarNames)
    {
        var grammarList = grammarNames.ToList();
        if (grammarList.Count == 0)
            return null;
        if (grammarList.Count == 1)
            return grammarList[0];

        // Get inheritance chains for all grammars
        var inheritanceChains = grammarList
            .Select(name => GetInheritanceChain(name))
            .ToList();

        // Find common base grammars
        var commonBases = inheritanceChains[0].AsEnumerable();
        for (int i = 1; i < inheritanceChains.Count; i++)
        {
            commonBases = commonBases.Intersect(inheritanceChains[i]);
        }

        // Return the most derived common base (first in the inheritance chain)
        foreach (var grammar in inheritanceChains[0])
        {
            if (commonBases.Contains(grammar))
            {
                return grammar;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that there are no circular inheritance dependencies.
    /// </summary>
    /// <returns>A list of circular dependencies found.</returns>
    public List<string> ValidateInheritance()
    {
        var circularDependencies = new List<string>();

        foreach (var grammarName in _grammars.Keys)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            if (HasCircularDependency(grammarName, visited, recursionStack))
            {
                circularDependencies.Add(grammarName);
            }
        }

        return circularDependencies;
    }

    /// <summary>
    /// Gets all grammars that are registered.
    /// </summary>
    /// <returns>All registered grammars.</returns>
    public IEnumerable<Grammar.Grammar> GetAllGrammars()
    {
        return _grammars.Values;
    }

    /// <summary>
    /// Gets a grammar by name.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>The grammar, or null if not found.</returns>
    public Grammar.Grammar? GetGrammar(string grammarName)
    {
        return _grammars.TryGetValue(grammarName, out var grammar) ? grammar : null;
    }

    /// <summary>
    /// Clears all caches.
    /// </summary>
    public void ClearCache()
    {
        _inheritanceChainCache.Clear();
        _dependentsCache.Clear();
    }

    /// <summary>
    /// Gets statistics about the inheritance resolver.
    /// </summary>
    /// <returns>Statistics information.</returns>
    public InheritanceResolverStatistics GetStatistics()
    {
        var totalBaseGrammars = _grammars.Values
            .SelectMany(g => g.BaseGrammars)
            .Distinct()
            .Count();

        var averageInheritanceDepth = _grammars.Keys
            .Select(name => GetInheritanceChain(name).Count)
            .DefaultIfEmpty(0)
            .Average();

        return new InheritanceResolverStatistics
        {
            TotalGrammars = _grammars.Count,
            TotalBaseGrammars = totalBaseGrammars,
            AverageInheritanceDepth = averageInheritanceDepth,
            CachedInheritanceChains = _inheritanceChainCache.Count,
            CachedDependents = _dependentsCache.Count
        };
    }

    // ============================================================================
    // PRIVATE METHODS
    // ============================================================================

    private void BuildInheritanceChain(string grammarName, List<string> chain, HashSet<string> visited)
    {
        if (visited.Contains(grammarName))
        {
            // Circular dependency detected - stop here
            return;
        }

        visited.Add(grammarName);

        if (!_grammars.TryGetValue(grammarName, out var grammar))
        {
            // Grammar not found - stop here
            return;
        }

        // Add base grammars to the chain
        foreach (var baseGrammarName in grammar.BaseGrammars)
        {
            if (!chain.Contains(baseGrammarName))
            {
                chain.Add(baseGrammarName);
                BuildInheritanceChain(baseGrammarName, chain, visited);
            }
        }
    }

    private bool HasCircularDependency(string grammarName, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(grammarName))
        {
            return true; // Circular dependency found
        }

        if (visited.Contains(grammarName))
        {
            return false; // Already processed
        }

        visited.Add(grammarName);
        recursionStack.Add(grammarName);

        if (_grammars.TryGetValue(grammarName, out var grammar))
        {
            foreach (var baseGrammarName in grammar.BaseGrammars)
            {
                if (HasCircularDependency(baseGrammarName, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(grammarName);
        return false;
    }

    private void ClearCacheForGrammar(string grammarName)
    {
        _inheritanceChainCache.Remove(grammarName);
        _dependentsCache.Remove(grammarName);

        // Also clear cache for grammars that might be affected
        var keysToRemove = new List<string>();

        foreach (var key in _inheritanceChainCache.Keys)
        {
            var chain = _inheritanceChainCache[key];
            if (chain.Contains(grammarName))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _inheritanceChainCache.Remove(key);
        }

        keysToRemove.Clear();

        foreach (var key in _dependentsCache.Keys)
        {
            var dependents = _dependentsCache[key];
            if (dependents.Contains(grammarName))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _dependentsCache.Remove(key);
        }
    }
}

/// <summary>
/// Statistics about the inheritance resolver.
/// </summary>
public record InheritanceResolverStatistics
{
    /// <summary>
    /// Gets the total number of grammars.
    /// </summary>
    public int TotalGrammars { get; init; }

    /// <summary>
    /// Gets the total number of base grammars.
    /// </summary>
    public int TotalBaseGrammars { get; init; }

    /// <summary>
    /// Gets the average inheritance depth.
    /// </summary>
    public double AverageInheritanceDepth { get; init; }

    /// <summary>
    /// Gets the number of cached inheritance chains.
    /// </summary>
    public int CachedInheritanceChains { get; init; }

    /// <summary>
    /// Gets the number of cached dependents.
    /// </summary>
    public int CachedDependents { get; init; }
}