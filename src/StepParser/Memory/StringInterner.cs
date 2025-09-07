using System.Collections.Concurrent;

namespace StepParser.Memory;

/// <summary>
/// String interner for deduplicating strings in memory.
/// </summary>
public class StringInterner
{
    private readonly ConcurrentDictionary<string, string> _internedStrings;
    private readonly MemoryArena _arena;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringInterner"/> class.
    /// </summary>
    /// <param name="arena">The memory arena to use for statistics.</param>
    public StringInterner(MemoryArena arena)
    {
        _internedStrings = new ConcurrentDictionary<string, string>();
        _arena = arena;
    }

    /// <summary>
    /// Interns a string, returning the canonical instance.
    /// </summary>
    /// <param name="value">The string to intern.</param>
    /// <returns>The interned string instance.</returns>
    public string Intern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return _internedStrings.GetOrAdd(value, value);
    }

    /// <summary>
    /// Gets the number of interned strings.
    /// </summary>
    public int Count => _internedStrings.Count;

    /// <summary>
    /// Clears all interned strings.
    /// </summary>
    public void Clear()
    {
        _internedStrings.Clear();
    }

    /// <summary>
    /// Gets statistics about the string interner.
    /// </summary>
    /// <returns>Statistics information.</returns>
    public StringInternerStatistics GetStatistics()
    {
        var totalChars = _internedStrings.Keys.Sum(s => s.Length);
        var estimatedSavings = _internedStrings.Sum(kvp => 
            kvp.Key.Length * sizeof(char) * Math.Max(1, _internedStrings.Count / 10)); // Rough estimate

        return new StringInternerStatistics
        {
            UniqueStrings = _internedStrings.Count,
            TotalCharacters = totalChars,
            EstimatedMemorySavings = estimatedSavings
        };
    }
}

/// <summary>
/// Statistics about string interning.
/// </summary>
public record StringInternerStatistics
{
    /// <summary>
    /// Gets the number of unique strings.
    /// </summary>
    public int UniqueStrings { get; init; }

    /// <summary>
    /// Gets the total number of characters across all unique strings.
    /// </summary>
    public int TotalCharacters { get; init; }

    /// <summary>
    /// Gets the estimated memory savings in bytes.
    /// </summary>
    public long EstimatedMemorySavings { get; init; }
}