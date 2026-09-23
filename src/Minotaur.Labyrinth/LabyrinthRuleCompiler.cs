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

    internal CompiledLabyrinthPropagator(LabyrinthPatternAst pattern, string from, string to)
    {
        Pattern = pattern;
        From = from;
        To = to;
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

    /// <summary>The parsed taint source patterns.</summary>
    public IReadOnlyList<LabyrinthPatternAst> Sources { get; }

    /// <summary>The parsed taint sink patterns.</summary>
    public IReadOnlyList<LabyrinthPatternAst> Sinks { get; }

    /// <summary>The parsed taint sanitizer patterns.</summary>
    public IReadOnlyList<LabyrinthPatternAst> Sanitizers { get; }

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
        IReadOnlyList<LabyrinthPatternAst> sources,
        IReadOnlyList<LabyrinthPatternAst> sinks,
        IReadOnlyList<LabyrinthPatternAst> sanitizers,
        IReadOnlyList<CompiledLabyrinthPropagator> propagators,
        IReadOnlyList<string> metavariables,
        IReadOnlyDictionary<string, int> metavariableOccurrences)
    {
        Rule = rule;
        SearchPattern = searchPattern;
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
        var metavariables = CollectMetavariables(searchPattern.Metavariables, out var occurrences);

        return new CompiledLabyrinthRule(
            rule,
            searchPattern,
            sources: Array.Empty<LabyrinthPatternAst>(),
            sinks: Array.Empty<LabyrinthPatternAst>(),
            sanitizers: Array.Empty<LabyrinthPatternAst>(),
            propagators: Array.Empty<CompiledLabyrinthPropagator>(),
            metavariables,
            occurrences);
    }

    private static CompiledLabyrinthRule CompileTaint(LabyrinthRule rule, LabyrinthPatternParser parser)
    {
        var sources = ParseEntries(rule.Sources, "source", parser);
        var sinks = ParseEntries(rule.Sinks, "sink", parser);
        var sanitizers = ParseEntries(rule.Sanitizers, "sanitizer", parser);
        var propagators = CompilePropagators(rule.Propagators, parser);

        var metavariables = CollectMetavariables(
            sources.Concat(sinks).Concat(sanitizers).SelectMany(a => a.Metavariables)
                .Concat(propagators.SelectMany(p => p.Pattern.Metavariables)),
            out var occurrences);

        return new CompiledLabyrinthRule(
            rule,
            searchPattern: null,
            sources,
            sinks,
            sanitizers,
            propagators,
            metavariables,
            occurrences);
    }

    private static IReadOnlyList<LabyrinthPatternAst> ParseEntries(
        List<LabyrinthPatternEntry>? entries, string role, LabyrinthPatternParser parser)
    {
        if (entries is null)
        {
            return Array.Empty<LabyrinthPatternAst>();
        }

        var asts = new List<LabyrinthPatternAst>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            try
            {
                asts.Add(parser.ParsePattern(entry.Pattern));
            }
            catch (LabyrinthRuleException ex)
            {
                throw new LabyrinthRuleException($"Invalid taint {role} pattern: {ex.Message}", ex);
            }
        }

        return asts;
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

            compiled.Add(new CompiledLabyrinthPropagator(ast, from, to));
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
