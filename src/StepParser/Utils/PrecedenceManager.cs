using StepParser.Grammar;

namespace StepParser.Utils;

/// <summary>
/// Represents an associativity rule.
/// </summary>
public record AssociativityRule
{
    /// <summary>
    /// Gets the operator name.
    /// </summary>
    public string OperatorName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the associativity type.
    /// </summary>
    public AssociativityType AssociativityType { get; init; }

    /// <summary>
    /// Gets the precedence level.
    /// </summary>
    public int PrecedenceLevel { get; init; }

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
/// Manages precedence and associativity rules with inheritance support.
/// </summary>
public class PrecedenceManager
{
    private readonly Dictionary<string, PrecedenceRule> _precedenceRegistry;
    private readonly Dictionary<string, AssociativityRule> _associativityRegistry;
    private readonly Dictionary<string, Dictionary<string, PrecedenceRule>> _precedenceCache;
    private readonly Dictionary<string, Dictionary<string, AssociativityRule>> _associativityCache;
    private readonly InheritanceResolver _inheritanceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrecedenceManager"/> class.
    /// </summary>
    /// <param name="inheritanceResolver">The inheritance resolver.</param>
    public PrecedenceManager(InheritanceResolver inheritanceResolver)
    {
        _precedenceRegistry = new Dictionary<string, PrecedenceRule>();
        _associativityRegistry = new Dictionary<string, AssociativityRule>();
        _precedenceCache = new Dictionary<string, Dictionary<string, PrecedenceRule>>();
        _associativityCache = new Dictionary<string, Dictionary<string, AssociativityRule>>();
        _inheritanceResolver = inheritanceResolver;
    }

    // ============================================================================
    // PRECEDENCE RULE MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Registers a precedence rule.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="operatorName">The name of the operator.</param>
    /// <param name="rule">The precedence rule.</param>
    public void RegisterPrecedenceRule(string grammarName, string operatorName, PrecedenceRule rule)
    {
        var key = CreatePrecedenceKey(grammarName, operatorName);
        _precedenceRegistry[key] = rule;

        // Clear cache for affected grammars
        ClearPrecedenceCacheForGrammar(grammarName);
    }

    /// <summary>
    /// Gets a precedence rule with inheritance resolution.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="operatorName">The name of the operator.</param>
    /// <returns>The resolved precedence rule or null if not found.</returns>
    public PrecedenceRule? GetPrecedenceRule(string grammarName, string operatorName)
    {
        // Check cache first
        if (_precedenceCache.TryGetValue(grammarName, out var grammarCache) &&
            grammarCache.TryGetValue(operatorName, out var cachedRule))
        {
            return cachedRule;
        }

        // Resolve rule with inheritance
        var resolvedRule = ResolvePrecedenceRule(grammarName, operatorName);

        // Cache the result
        if (!_precedenceCache.ContainsKey(grammarName))
        {
            _precedenceCache[grammarName] = new Dictionary<string, PrecedenceRule>();
        }

        if (resolvedRule != null)
        {
            _precedenceCache[grammarName][operatorName] = resolvedRule;
        }

        return resolvedRule;
    }

    /// <summary>
    /// Gets all precedence rules for a grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>All precedence rules for the grammar.</returns>
    public Dictionary<string, PrecedenceRule> GetPrecedenceRulesForGrammar(string grammarName)
    {
        var rules = new Dictionary<string, PrecedenceRule>();

        // Get rules directly defined for this grammar
        foreach (var kvp in _precedenceRegistry)
        {
            if (kvp.Key.StartsWith($"{grammarName}::"))
            {
                var operatorName = kvp.Key.Substring($"{grammarName}::".Length);
                rules[operatorName] = kvp.Value;
            }
        }

        // Get inherited rules
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            foreach (var kvp in _precedenceRegistry)
            {
                if (kvp.Key.StartsWith($"{baseGrammarName}::"))
                {
                    var operatorName = kvp.Key.Substring($"{baseGrammarName}::".Length);
                    // Only add if not already defined in derived grammar
                    if (!rules.ContainsKey(operatorName))
                    {
                        rules[operatorName] = kvp.Value;
                    }
                }
            }
        }

        return rules;
    }

    /// <summary>
    /// Compares the precedence of two operators.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="operator1">The first operator.</param>
    /// <param name="operator2">The second operator.</param>
    /// <returns>
    /// A value less than 0 if operator1 has lower precedence,
    /// 0 if they have equal precedence,
    /// or a value greater than 0 if operator1 has higher precedence.
    /// </returns>
    public int ComparePrecedence(string grammarName, string operator1, string operator2)
    {
        var rule1 = GetPrecedenceRule(grammarName, operator1);
        var rule2 = GetPrecedenceRule(grammarName, operator2);

        if (rule1 == null && rule2 == null)
            return 0;
        if (rule1 == null)
            return -1;
        if (rule2 == null)
            return 1;

        return rule1.Level.CompareTo(rule2.Level);
    }

    // ============================================================================
    // ASSOCIATIVITY RULE MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Registers an associativity rule.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="operatorName">The name of the operator.</param>
    /// <param name="rule">The associativity rule.</param>
    public void RegisterAssociativityRule(string grammarName, string operatorName, AssociativityRule rule)
    {
        var key = CreateAssociativityKey(grammarName, operatorName);
        _associativityRegistry[key] = rule;

        // Clear cache for affected grammars
        ClearAssociativityCacheForGrammar(grammarName);
    }

    /// <summary>
    /// Gets an associativity rule with inheritance resolution.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <param name="operatorName">The name of the operator.</param>
    /// <returns>The resolved associativity rule or null if not found.</returns>
    public AssociativityRule? GetAssociativityRule(string grammarName, string operatorName)
    {
        // Check cache first
        if (_associativityCache.TryGetValue(grammarName, out var grammarCache) &&
            grammarCache.TryGetValue(operatorName, out var cachedRule))
        {
            return cachedRule;
        }

        // Resolve rule with inheritance
        var resolvedRule = ResolveAssociativityRule(grammarName, operatorName);

        // Cache the result
        if (!_associativityCache.ContainsKey(grammarName))
        {
            _associativityCache[grammarName] = new Dictionary<string, AssociativityRule>();
        }

        if (resolvedRule != null)
        {
            _associativityCache[grammarName][operatorName] = resolvedRule;
        }

        return resolvedRule;
    }

    /// <summary>
    /// Gets all associativity rules for a grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    /// <returns>All associativity rules for the grammar.</returns>
    public Dictionary<string, AssociativityRule> GetAssociativityRulesForGrammar(string grammarName)
    {
        var rules = new Dictionary<string, AssociativityRule>();

        // Get rules directly defined for this grammar
        foreach (var kvp in _associativityRegistry)
        {
            if (kvp.Key.StartsWith($"{grammarName}::"))
            {
                var operatorName = kvp.Key.Substring($"{grammarName}::".Length);
                rules[operatorName] = kvp.Value;
            }
        }

        // Get inherited rules
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            foreach (var kvp in _associativityRegistry)
            {
                if (kvp.Key.StartsWith($"{baseGrammarName}::"))
                {
                    var operatorName = kvp.Key.Substring($"{baseGrammarName}::".Length);
                    // Only add if not already defined in derived grammar
                    if (!rules.ContainsKey(operatorName))
                    {
                        rules[operatorName] = kvp.Value;
                    }
                }
            }
        }

        return rules;
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    /// <summary>
    /// Removes all rules for a grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar.</param>
    public void RemoveRulesForGrammar(string grammarName)
    {
        var keysToRemove = new List<string>();

        // Find precedence rules to remove
        foreach (var key in _precedenceRegistry.Keys)
        {
            if (key.StartsWith($"{grammarName}::"))
            {
                keysToRemove.Add(key);
            }
        }

        // Remove precedence rules
        foreach (var key in keysToRemove)
        {
            _precedenceRegistry.Remove(key);
        }

        keysToRemove.Clear();

        // Find associativity rules to remove
        foreach (var key in _associativityRegistry.Keys)
        {
            if (key.StartsWith($"{grammarName}::"))
            {
                keysToRemove.Add(key);
            }
        }

        // Remove associativity rules
        foreach (var key in keysToRemove)
        {
            _associativityRegistry.Remove(key);
        }

        // Clear caches
        ClearPrecedenceCacheForGrammar(grammarName);
        ClearAssociativityCacheForGrammar(grammarName);
    }

    /// <summary>
    /// Clears all rules.
    /// </summary>
    public void Clear()
    {
        _precedenceRegistry.Clear();
        _associativityRegistry.Clear();
        _precedenceCache.Clear();
        _associativityCache.Clear();
    }

    /// <summary>
    /// Gets statistics about the precedence manager.
    /// </summary>
    /// <returns>Statistics information.</returns>
    public PrecedenceManagerStatistics GetStatistics()
    {
        var precedenceGrammarCount = _precedenceRegistry.Keys
            .Select(key => key.Split("::")[0])
            .Distinct()
            .Count();

        var associativityGrammarCount = _associativityRegistry.Keys
            .Select(key => key.Split("::")[0])
            .Distinct()
            .Count();

        return new PrecedenceManagerStatistics
        {
            TotalPrecedenceRules = _precedenceRegistry.Count,
            TotalAssociativityRules = _associativityRegistry.Count,
            PrecedenceGrammarCount = precedenceGrammarCount,
            AssociativityGrammarCount = associativityGrammarCount,
            CachedPrecedenceGrammars = _precedenceCache.Count,
            CachedAssociativityGrammars = _associativityCache.Count
        };
    }

    // ============================================================================
    // PRIVATE METHODS
    // ============================================================================

    private string CreatePrecedenceKey(string grammarName, string operatorName)
    {
        return $"{grammarName}::{operatorName}";
    }

    private string CreateAssociativityKey(string grammarName, string operatorName)
    {
        return $"{grammarName}::{operatorName}";
    }

    private void ClearPrecedenceCacheForGrammar(string grammarName)
    {
        _precedenceCache.Remove(grammarName);

        // Also clear cache for grammars that inherit from this one
        var dependentGrammars = _inheritanceResolver.GetDependentGrammars(grammarName);
        foreach (var dependentGrammar in dependentGrammars)
        {
            _precedenceCache.Remove(dependentGrammar);
        }
    }

    private void ClearAssociativityCacheForGrammar(string grammarName)
    {
        _associativityCache.Remove(grammarName);

        // Also clear cache for grammars that inherit from this one
        var dependentGrammars = _inheritanceResolver.GetDependentGrammars(grammarName);
        foreach (var dependentGrammar in dependentGrammars)
        {
            _associativityCache.Remove(dependentGrammar);
        }
    }

    private PrecedenceRule? ResolvePrecedenceRule(string grammarName, string operatorName)
    {
        // Try to find rule in current grammar
        var key = CreatePrecedenceKey(grammarName, operatorName);
        if (_precedenceRegistry.TryGetValue(key, out var rule))
        {
            return rule;
        }

        // Try to find rule in base grammars
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            key = CreatePrecedenceKey(baseGrammarName, operatorName);
            if (_precedenceRegistry.TryGetValue(key, out rule))
            {
                return rule;
            }
        }

        return null;
    }

    private AssociativityRule? ResolveAssociativityRule(string grammarName, string operatorName)
    {
        // Try to find rule in current grammar
        var key = CreateAssociativityKey(grammarName, operatorName);
        if (_associativityRegistry.TryGetValue(key, out var rule))
        {
            return rule;
        }

        // Try to find rule in base grammars
        var inheritanceChain = _inheritanceResolver.GetInheritanceChain(grammarName);
        foreach (var baseGrammarName in inheritanceChain)
        {
            key = CreateAssociativityKey(baseGrammarName, operatorName);
            if (_associativityRegistry.TryGetValue(key, out rule))
            {
                return rule;
            }
        }

        return null;
    }
}

/// <summary>
/// Statistics about the precedence manager.
/// </summary>
public record PrecedenceManagerStatistics
{
    /// <summary>
    /// Gets the total number of precedence rules.
    /// </summary>
    public int TotalPrecedenceRules { get; init; }

    /// <summary>
    /// Gets the total number of associativity rules.
    /// </summary>
    public int TotalAssociativityRules { get; init; }

    /// <summary>
    /// Gets the number of grammars with precedence rules.
    /// </summary>
    public int PrecedenceGrammarCount { get; init; }

    /// <summary>
    /// Gets the number of grammars with associativity rules.
    /// </summary>
    public int AssociativityGrammarCount { get; init; }

    /// <summary>
    /// Gets the number of cached precedence grammars.
    /// </summary>
    public int CachedPrecedenceGrammars { get; init; }

    /// <summary>
    /// Gets the number of cached associativity grammars.
    /// </summary>
    public int CachedAssociativityGrammars { get; init; }
}