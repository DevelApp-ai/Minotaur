using System.Collections.Concurrent;

namespace StepParser.Context;

/// <summary>
/// Manages symbols and their scopes.
/// </summary>
public class SymbolTable
{
    private readonly ConcurrentDictionary<string, SymbolInfo> _symbols;
    private readonly ConcurrentDictionary<string, List<SymbolInfo>> _symbolsByScope;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolTable"/> class.
    /// </summary>
    public SymbolTable()
    {
        _symbols = new ConcurrentDictionary<string, SymbolInfo>();
        _symbolsByScope = new ConcurrentDictionary<string, List<SymbolInfo>>();
    }

    /// <summary>
    /// Declares a new symbol.
    /// </summary>
    /// <param name="symbol">The symbol to declare.</param>
    public void DeclareSymbol(SymbolInfo symbol)
    {
        var key = CreateSymbolKey(symbol.Name, symbol.Scope);
        _symbols[key] = symbol;

        // Add to scope mapping
        _symbolsByScope.AddOrUpdate(
            symbol.Scope,
            new List<SymbolInfo> { symbol },
            (scope, existing) =>
            {
                lock (_lock)
                {
                    existing.RemoveAll(s => s.Name == symbol.Name); // Remove any existing symbol with same name
                    existing.Add(symbol);
                    return existing;
                }
            });
    }

    /// <summary>
    /// Defines a symbol (alias for DeclareSymbol for compatibility).
    /// </summary>
    /// <param name="symbol">The symbol to define.</param>
    public void DefineSymbol(SymbolInfo symbol)
    {
        DeclareSymbol(symbol);
    }

    /// <summary>
    /// Looks up a symbol by name in the given scope.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="scope">The scope to search in.</param>
    /// <returns>The symbol if found; otherwise, null.</returns>
    public SymbolInfo? LookupSymbol(string name, string scope)
    {
        var key = CreateSymbolKey(name, scope);
        return _symbols.TryGetValue(key, out var symbol) ? symbol : null;
    }

    /// <summary>
    /// Looks up a symbol by name in any scope, starting from the most specific.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="scopes">The scopes to search in, ordered by preference.</param>
    /// <returns>The symbol if found; otherwise, null.</returns>
    public SymbolInfo? LookupSymbol(string name, IEnumerable<string> scopes)
    {
        foreach (var scope in scopes)
        {
            var symbol = LookupSymbol(name, scope);
            if (symbol != null)
            {
                return symbol;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all symbols in a given scope.
    /// </summary>
    /// <param name="scope">The scope to get symbols from.</param>
    /// <returns>The symbols in the scope.</returns>
    public IReadOnlyList<SymbolInfo> GetSymbolsInScope(string scope)
    {
        return _symbolsByScope.TryGetValue(scope, out var symbols)
            ? symbols.ToList() // Return a copy to avoid concurrent modification
            : new List<SymbolInfo>();
    }

    /// <summary>
    /// Gets all symbols.
    /// </summary>
    /// <returns>All symbols in the table.</returns>
    public IEnumerable<SymbolInfo> GetAllSymbols()
    {
        return _symbols.Values;
    }

    /// <summary>
    /// Checks if a symbol exists.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="scope">The scope to check in.</param>
    /// <returns>True if the symbol exists; otherwise, false.</returns>
    public bool SymbolExists(string name, string scope)
    {
        var key = CreateSymbolKey(name, scope);
        return _symbols.ContainsKey(key);
    }

    /// <summary>
    /// Removes a symbol from the table.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="scope">The scope to remove from.</param>
    /// <returns>True if the symbol was removed; otherwise, false.</returns>
    public bool RemoveSymbol(string name, string scope)
    {
        var key = CreateSymbolKey(name, scope);
        var removed = _symbols.TryRemove(key, out var symbol);

        if (removed && symbol != null)
        {
            // Remove from scope mapping
            if (_symbolsByScope.TryGetValue(scope, out var symbols))
            {
                lock (_lock)
                {
                    symbols.RemoveAll(s => s.Name == name);
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Clears all symbols from the table.
    /// </summary>
    public void Clear()
    {
        _symbols.Clear();
        _symbolsByScope.Clear();
    }

    /// <summary>
    /// Gets statistics about the symbol table.
    /// </summary>
    /// <returns>Symbol table statistics.</returns>
    public SymbolTableStatistics GetStatistics()
    {
        return new SymbolTableStatistics
        {
            TotalSymbols = _symbols.Count,
            ScopeCount = _symbolsByScope.Count,
            SymbolsByScope = _symbolsByScope.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count)
        };
    }

    private string CreateSymbolKey(string name, string scope)
    {
        return $"{scope}::{name}";
    }
}

/// <summary>
/// Statistics about the symbol table.
/// </summary>
public record SymbolTableStatistics
{
    /// <summary>
    /// Gets the total number of symbols.
    /// </summary>
    public int TotalSymbols { get; init; }

    /// <summary>
    /// Gets the number of scopes.
    /// </summary>
    public int ScopeCount { get; init; }

    /// <summary>
    /// Gets the number of symbols per scope.
    /// </summary>
    public Dictionary<string, int> SymbolsByScope { get; init; } = new();
}