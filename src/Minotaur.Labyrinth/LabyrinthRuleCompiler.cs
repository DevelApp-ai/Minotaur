namespace Minotaur.Labyrinth;

/// <summary>
/// A taint propagator whose pattern has been parsed into a mini-AST and whose
/// <c>from</c>/<c>to</c> metavariables have been validated against the parsed
/// pattern (they must actually occur in it).
/// </summary>
public sealed class CompiledLabyrinthPropagator
{
    /// <summary>The parsed propagator pattern.</summary>
    public LabyrinthPatternAst Pattern { get; }

    /// <summary>The metavariable taint state flows from (validated to occur in <see cref="Pattern"/>).</summary>
    public string From { get; }

    /// <summary>The metavariable taint state flows to (validated to occur in <see cref="Pattern"/>).</summary>
    public string To { get; }

    /// <summary>
    /// Optional Dynamic LINQ condition (issue #104) evaluated against the matched
    /// node before the propagator transfers taint. Null when the entry has none.
    /// </summary>
    public string? Condition { get; }

    internal CompiledLabyrinthPropagator(LabyrinthPatternAst pattern, string from, string to, string? condition)
    {
        Pattern = pattern;
        From = from;
        To = to;
        Condition = condition;
    }
}

/// <summary>
/// One alternative of a pattern entry: the parsed positive pattern plus its
/// optional negation (<c>pattern-not</c>) and ancestor (<c>pattern-inside</c>)
/// filters and its own Dynamic LINQ condition. The alternative matches when the
/// positive pattern matches, NO not-pattern matches the same node, and EVERY
/// inside-pattern matches some ancestor of the node.
/// </summary>
public sealed class CompiledLabyrinthAlternative
{
    /// <summary>The parsed positive pattern.</summary>
    public LabyrinthPatternAst Pattern { get; }

    /// <summary>This alternative's own condition; falls back to the entry condition when null.</summary>
    public string? Condition { get; }

    /// <summary>Negation filters (YAML <c>pattern-not:</c>); the alternative fails when any matches the node.</summary>
    public IReadOnlyList<LabyrinthPatternAst> NotPatterns { get; }

    /// <summary>Ancestor filters (YAML <c>pattern-inside:</c>); the alternative requires each to match some ancestor.</summary>
    public IReadOnlyList<LabyrinthPatternAst> InsidePatterns { get; }

    internal CompiledLabyrinthAlternative(
        LabyrinthPatternAst pattern,
        string? condition,
        IReadOnlyList<LabyrinthPatternAst> notPatterns,
        IReadOnlyList<LabyrinthPatternAst> insidePatterns)
    {
        Pattern = pattern;
        Condition = condition;
        NotPatterns = notPatterns;
        InsidePatterns = insidePatterns;
    }
}

/// <summary>
/// A parsed taint pattern entry with its optional Dynamic LINQ condition
/// (issue #104): the pattern mini-AST plus the <c>condition</c> string of the
/// YAML entry, kept together so the expression compiler can merge both into
/// one delegate.
/// </summary>
public sealed class CompiledLabyrinthPatternEntry
{
    /// <summary>
    /// The parsed alternatives of this entry: a single element for a plain
    /// <c>pattern:</c> entry, or all alternatives of a <c>pattern-either:</c> entry.
    /// The entry matches when ANY alternative matches.
    /// </summary>
    public IReadOnlyList<CompiledLabyrinthAlternative> Alternatives { get; }

    /// <summary>The parsed pattern (first alternative's pattern; kept for compatibility).</summary>
    public LabyrinthPatternAst Pattern => Alternatives[0].Pattern;

    /// <summary>The entry's optional Dynamic LINQ condition; applies to every alternative; null when absent.</summary>
    public string? Condition { get; }

    internal CompiledLabyrinthPatternEntry(IReadOnlyList<CompiledLabyrinthAlternative> alternatives, string? condition)
    {
        Alternatives = alternatives;
        Condition = condition;
    }
}

/// <summary>
/// A Labyrinth rule with every pattern entry parsed into a mini-AST and a
/// rule-level metavariable unification table: the input structure consumed by
/// Labyrinth Layer 2 (Expression Trees, issue #102).
/// </summary>
public sealed class CompiledLabyrinthRule
{
    /// <summary>The originating rule.</summary>
    public LabyrinthRule Rule { get; }

    /// <summary>
    /// The parsed search pattern. Only set for search rules; taint rules leave it null.
    /// </summary>
    public LabyrinthPatternAst? SearchPattern { get; }

    /// <summary>
    /// The search rule's optional Dynamic LINQ condition (issue #104); null when absent.
    /// </summary>
    public string? SearchCondition { get; }

    /// <summary>Search rule negation filters (YAML <c>pattern-not:</c>); the search pattern must not match them.</summary>
    public IReadOnlyList<LabyrinthPatternAst> SearchNotPatterns { get; }

    /// <summary>Search rule ancestor filters (YAML <c>pattern-inside:</c>); the search pattern must be inside a match of each.</summary>
    public IReadOnlyList<LabyrinthPatternAst> SearchInsidePatterns { get; }

    /// <summary>The parsed taint source patterns with their optional conditions.</summary>
    public IReadOnlyList<CompiledLabyrinthPatternEntry> Sources { get; }

    /// <summary>The parsed taint sink patterns with their optional conditions.</summary>
    public IReadOnlyList<CompiledLabyrinthPatternEntry> Sinks { get; }

    /// <summary>The parsed taint sanitizer patterns with their optional conditions.</summary>
    public IReadOnlyList<CompiledLabyrinthPatternEntry> Sanitizers { get; }

    /// <summary>The parsed, validated taint propagators.</summary>
    public IReadOnlyList<CompiledLabyrinthPropagator> Propagators { get; }

    /// <summary>
    /// The rule-level unification table: every metavariable name occurring in
    /// any pattern of the rule, distinct, in order of first occurrence. A
    /// metavariable bound in one pattern entry refers to the same capture as
    /// the same name in any other entry of the same rule.
    /// </summary>
    public IReadOnlyList<string> Metavariables { get; }

    /// <summary>
    /// The number of occurrences of each metavariable across all patterns of
    /// the rule (binding cardinality used by the unification layer).
    /// </summary>
    public IReadOnlyDictionary<string, int> MetavariableOccurrences { get; }

    internal CompiledLabyrinthRule(
        LabyrinthRule rule,
        LabyrinthPatternAst? searchPattern,
        string? searchCondition,
        IReadOnlyList<LabyrinthPatternAst>? searchNotPatterns,
        IReadOnlyList<LabyrinthPatternAst>? searchInsidePatterns,
        IReadOnlyList<CompiledLabyrinthPatternEntry> sources,
        IReadOnlyList<CompiledLabyrinthPatternEntry> sinks,
        IReadOnlyList<CompiledLabyrinthPatternEntry> sanitizers,
        IReadOnlyList<CompiledLabyrinthPropagator> propagators,
        IReadOnlyList<string> metavariables,
        IReadOnlyDictionary<string, int> metavariableOccurrences)
    {
        Rule = rule;
        SearchPattern = searchPattern;
        SearchCondition = searchCondition;
        SearchNotPatterns = searchNotPatterns ?? Array.Empty<LabyrinthPatternAst>();
        SearchInsidePatterns = searchInsidePatterns ?? Array.Empty<LabyrinthPatternAst>();
        Sources = sources;
        Sinks = sinks;
        Sanitizers = sanitizers;
        Propagators = propagators;
        Metavariables = metavariables;
        MetavariableOccurrences = metavariableOccurrences;
    }
}

/// <summary>
/// Compiles <see cref="LabyrinthRule"/>s against a target grammar: parses every
/// pattern entry (search pattern, sources, sinks, sanitizers, propagators)
/// into a mini-AST via <see cref="LabyrinthPatternParser"/> and builds the
/// rule-level metavariable unification table. This is the pattern-parsing step
/// of Labyrinth Layer 1 (issue #101); matching against real code is Layer 2.
/// </summary>
public static class LabyrinthRuleCompiler
{
    /// <summary>
    /// Compiles a rule against a target grammar.
    /// </summary>
    /// <param name="rule">The rule to compile.</param>
    /// <param name="grammarContent">The target grammar file content the patterns are written in.</param>
    /// <param name="grammarName">A display name for the grammar, used in error messages.</param>
    /// <exception cref="LabyrinthRuleException">
    /// Thrown when any pattern of the rule cannot be tokenized by the target
    /// grammar, or when a propagator's <c>from</c>/<c>to</c> metavariable does
    /// not occur in its pattern.
    /// </exception>
    public static CompiledLabyrinthRule Compile(LabyrinthRule rule, string grammarContent, string grammarName = "target grammar")
    {
        ArgumentNullException.ThrowIfNull(rule);

        using var parser = new LabyrinthPatternParser(grammarContent, grammarName);
        return Compile(rule, parser);
    }

    /// <summary>
    /// Compiles a rule using an already-configured pattern parser (one parser
    /// can compile many rules of the same target grammar).
    /// </summary>
    /// <param name="rule">The rule to compile.</param>
    /// <param name="parser">The pattern parser configured with the target grammar.</param>
    /// <exception cref="LabyrinthRuleException">
    /// Thrown when any pattern of the rule cannot be tokenized by the target
    /// grammar, or when a propagator's <c>from</c>/<c>to</c> metavariable does
    /// not occur in its pattern.
    /// </exception>
    public static CompiledLabyrinthRule Compile(LabyrinthRule rule, LabyrinthPatternParser parser)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(parser);

        if (rule.TypeValue == LabyrinthRuleType.Taint)
        {
            return CompileTaint(rule, parser);
        }

        var searchPattern = parser.ParsePattern(rule.Pattern);
        var searchNots = ParseOptionalPatterns(rule.PatternNot, "pattern-not", parser);
        var searchInsides = ParseOptionalPatterns(rule.PatternInside, "pattern-inside", parser);
        var metavariables = CollectMetavariables(
            searchPattern.Metavariables
                .Concat(searchNots.SelectMany(p => p.Metavariables))
                .Concat(searchInsides.SelectMany(p => p.Metavariables)),
            out var occurrences);

        return new CompiledLabyrinthRule(
            rule,
            searchPattern,
            searchCondition: rule.Condition,
            searchNotPatterns: searchNots,
            searchInsidePatterns: searchInsides,
            sources: Array.Empty<CompiledLabyrinthPatternEntry>(),
            sinks: Array.Empty<CompiledLabyrinthPatternEntry>(),
            sanitizers: Array.Empty<CompiledLabyrinthPatternEntry>(),
            propagators: Array.Empty<CompiledLabyrinthPropagator>(),
            metavariables,
            occurrences);
    }

    private static IReadOnlyList<LabyrinthPatternAst> ParseOptionalPatterns(
        string? pattern, string key, LabyrinthPatternParser parser)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Array.Empty<LabyrinthPatternAst>();
        }

        try
        {
            return new[] { parser.ParsePattern(pattern) };
        }
        catch (LabyrinthRuleException ex)
        {
            throw new LabyrinthRuleException($"Invalid search '{key}' pattern: {ex.Message}", ex);
        }
    }

    private static CompiledLabyrinthRule CompileTaint(LabyrinthRule rule, LabyrinthPatternParser parser)
    {
        var sources = ParseEntries(rule.Sources, "source", parser);
        var sinks = ParseEntries(rule.Sinks, "sink", parser);
        var sanitizers = ParseEntries(rule.Sanitizers, "sanitizer", parser);
        var propagators = CompilePropagators(rule.Propagators, parser);

        var metavariables = CollectMetavariables(
            sources.Concat(sinks).Concat(sanitizers).SelectMany(e => e.Alternatives.SelectMany(a =>
                a.Pattern.Metavariables
                    .Concat(a.NotPatterns.SelectMany(n => n.Metavariables))
                    .Concat(a.InsidePatterns.SelectMany(n => n.Metavariables))))
                .Concat(propagators.SelectMany(p => p.Pattern.Metavariables)),
            out var occurrences);

        return new CompiledLabyrinthRule(
            rule,
            searchPattern: null,
            searchCondition: null,
            searchNotPatterns: null,
            searchInsidePatterns: null,
            sources,
            sinks,
            sanitizers,
            propagators,
            metavariables,
            occurrences);
    }

    private static IReadOnlyList<CompiledLabyrinthPatternEntry> ParseEntries(
        List<LabyrinthPatternEntry>? entries, string role, LabyrinthPatternParser parser)
    {
        if (entries is null)
        {
            return Array.Empty<CompiledLabyrinthPatternEntry>();
        }

        var asts = new List<CompiledLabyrinthPatternEntry>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            try
            {
                var alternatives = entry.PatternEither is { Count: > 0 }
                    ? entry.PatternEither.Select(alt => ParseAlternative(alt, parser)).ToList()
                    : new List<CompiledLabyrinthAlternative> { ParseAlternative(entry, parser) };
                asts.Add(new CompiledLabyrinthPatternEntry(alternatives, entry.Condition));
            }
            catch (LabyrinthRuleException ex)
            {
                throw new LabyrinthRuleException($"Invalid taint {role} pattern: {ex.Message}", ex);
            }
        }

        return asts;
    }

    private static CompiledLabyrinthAlternative ParseAlternative(LabyrinthPatternEntry? entry, LabyrinthPatternParser parser)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.Pattern))
        {
            throw new LabyrinthRuleException("'pattern' is required.");
        }

        var pattern = parser.ParsePattern(entry.Pattern);
        var nots = string.IsNullOrWhiteSpace(entry.PatternNot)
            ? (IReadOnlyList<LabyrinthPatternAst>)Array.Empty<LabyrinthPatternAst>()
            : new[] { parser.ParsePattern(entry.PatternNot) };
        var insides = string.IsNullOrWhiteSpace(entry.PatternInside)
            ? (IReadOnlyList<LabyrinthPatternAst>)Array.Empty<LabyrinthPatternAst>()
            : new[] { parser.ParsePattern(entry.PatternInside) };
        return new CompiledLabyrinthAlternative(pattern, entry.Condition, nots, insides);
    }

    private static IReadOnlyList<CompiledLabyrinthPropagator> CompilePropagators(
        List<LabyrinthPropagatorEntry>? propagators, LabyrinthPatternParser parser)
    {
        if (propagators is null)
        {
            return Array.Empty<CompiledLabyrinthPropagator>();
        }

        var compiled = new List<CompiledLabyrinthPropagator>(propagators.Count);
        foreach (var propagator in propagators)
        {
            if (propagator is null)
            {
                continue;
            }

            var ast = parser.ParsePattern(propagator.Pattern);

            // The unification table comes from the parsed AST, so from/to are
            // validated against the tokens the lexer actually produced rather
            // than against the raw text.
            var from = propagator.From.TrimStart('$');
            var to = propagator.To.TrimStart('$');
            if (!ast.Metavariables.Contains(from))
            {
                throw new LabyrinthRuleException(
                    $"Propagator pattern '{propagator.Pattern}' does not contain its 'from' metavariable '${from}'.");
            }

            if (!ast.Metavariables.Contains(to))
            {
                throw new LabyrinthRuleException(
                    $"Propagator pattern '{propagator.Pattern}' does not contain its 'to' metavariable '${to}'.");
            }

            compiled.Add(new CompiledLabyrinthPropagator(ast, from, to, propagator.Condition));
        }

        return compiled;
    }

    private static IReadOnlyList<string> CollectMetavariables(
        IEnumerable<string> names, out IReadOnlyDictionary<string, int> occurrences)
    {
        // Occurrence counting per rule (not per pattern entry): the same name
        // in different entries of one rule is one unification variable.
        var counts = new Dictionary<string, int>();
        foreach (var name in names)
        {
            counts[name] = counts.TryGetValue(name, out var count) ? count + 1 : 1;
        }

        occurrences = counts;
        return counts.Keys.ToList();
    }
}
