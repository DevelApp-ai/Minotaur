namespace Minotaur.Labyrinth;

/// <summary>
/// Severity of a Labyrinth finding (issue #101 rule schema).
/// </summary>
public enum LabyrinthSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// The kind of analysis a Labyrinth rule performs.
/// <para><see cref="Search"/> matches a single syntactic pattern (with optional Dynamic LINQ condition).</para>
/// <para><see cref="Taint"/> connects sources, sinks, sanitizers and propagators with a data-flow traversal.</para>
/// </summary>
public enum LabyrinthRuleType
{
    Search,
    Taint
}

/// <summary>
/// A pattern entry: a raw snippet of the target grammar augmented with
/// Semgrep-style metavariables (<c>$NAME</c>) and the ellipsis operator (<c>...</c>).
/// </summary>
public sealed class LabyrinthPatternEntry
{
    /// <summary>
    /// The pattern written in the target grammar's syntax.
    /// Example: <c>ExecuteAction(..., $DATA, ...)</c>
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Alternatives for this entry (YAML <c>pattern-either:</c>): the entry matches when
    /// ANY of the listed sub-patterns matches. Mutually exclusive with
    /// <see cref="Pattern"/>; sub-entries may not themselves nest pattern-either.
    /// </summary>
    public List<LabyrinthPatternEntry>? PatternEither { get; set; }

    /// <summary>
    /// Negation filter (YAML <c>pattern-not:</c>): the entry only matches when this
    /// pattern does NOT match the same node. Only valid beside <see cref="Pattern"/>
    /// (when <see cref="PatternEither"/> is used, put pattern-not inside each alternative).
    /// Metavariables in a pattern-not are not unified with the entry scope (v1).
    /// </summary>
    public string? PatternNot { get; set; }

    /// <summary>
    /// Ancestor filter (YAML <c>pattern-inside:</c>): the entry only matches when some
    /// ancestor of the matched node (in the materialized match-node tree) matches this
    /// pattern. Only valid beside <see cref="Pattern"/> (with <see cref="PatternEither"/>,
    /// put pattern-inside inside each alternative). Metavariables in a pattern-inside
    /// are not unified with the entry scope (v1).
    /// </summary>
    public string? PatternInside { get; set; }

    /// <summary>
    /// Optional Dynamic LINQ condition (System.Linq.Dynamic.Core syntax) evaluated
    /// against the matched node. Example: <c>node.Arguments[0].Value &gt; 1024</c>
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// A propagator transfers taint state from one metavariable binding to another.
/// Example: <c>$TARGET = FormatString($SRC)</c> with <c>from: $SRC</c>, <c>to: $TARGET</c>.
/// </summary>
public sealed class LabyrinthPropagatorEntry
{
    /// <summary>
    /// The pattern written in the target grammar's syntax.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// The metavariable the taint state flows from (e.g. <c>$SRC</c>). Must occur in <see cref="Pattern"/>.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// The metavariable the taint state flows to (e.g. <c>$TARGET</c>). Must occur in <see cref="Pattern"/>.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Optional Dynamic LINQ condition (issue #104): a string-based C# logical
    /// expression evaluated against the matched node. The propagator only
    /// transfers taint when both its pattern and this condition match.
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// A single Labyrinth SAST rule (issue #101, engine epic #100).
/// </summary>
public sealed class LabyrinthRule
{
    /// <summary>Unique rule identifier, kebab-case (e.g. <c>custom-grammar-injection</c>).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Finding severity. YAML values: <c>ERROR</c>, <c>WARNING</c>, <c>INFO</c>.</summary>
    public string Severity { get; set; } = "WARNING";

    /// <summary>
    /// Message reported for a finding. Optional: when omitted, the rule id is used
    /// as the finding message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The message to report: <see cref="Message"/> when set, otherwise the rule id.</summary>
    public string EffectiveMessage => string.IsNullOrWhiteSpace(Message) ? Id : Message;

    /// <summary>Rule type. YAML values: <c>search</c>, <c>taint</c>.</summary>
    public string Type { get; set; } = "search";

    /// <summary>Search-mode pattern (mutually exclusive with taint keys).</summary>
    public string? Pattern { get; set; }

    /// <summary>Search-mode negation filter (YAML <c>pattern-not:</c>): the search pattern only fires when this pattern does not match the same node.</summary>
    public string? PatternNot { get; set; }

    /// <summary>Search-mode ancestor filter (YAML <c>pattern-inside:</c>): the search pattern only fires inside a node matching this pattern.</summary>
    public string? PatternInside { get; set; }

    /// <summary>
    /// Focus metavariable (YAML <c>focus-metavariable: $X</c>): the finding is reported
    /// at the node bound to <c>$X</c> instead of the whole matched node. For search rules
    /// it must occur in <see cref="Pattern"/>; for taint rules in a sink pattern (the
    /// finding's sink is focused).
    /// </summary>
    public string? FocusMetavariable { get; set; }

    /// <summary>Search-mode Dynamic LINQ condition.</summary>
    public string? Condition { get; set; }

    /// <summary>Taint sources.</summary>
    public List<LabyrinthPatternEntry>? Sources { get; set; }

    /// <summary>Taint sinks.</summary>
    public List<LabyrinthPatternEntry>? Sinks { get; set; }

    /// <summary>Taint sanitizers (optional).</summary>
    public List<LabyrinthPatternEntry>? Sanitizers { get; set; }

    /// <summary>Taint propagators (optional).</summary>
    public List<LabyrinthPropagatorEntry>? Propagators { get; set; }

    /// <summary>
    /// Optional file-path filter (YAML <c>paths:</c> with include/exclude globs):
    /// the rule only applies to scanned files whose path matches. Null = all files.
    /// </summary>
    public LabyrinthPathFilter? Paths { get; set; }

    /// <summary>
    /// Optional fix suggestion (YAML <c>fix:</c>): a replacement snippet in the target
    /// grammar's syntax with metavariable interpolation (<c>$X</c> is replaced by the
    /// bound node's text). Every metavariable in the fix must occur in the rule's
    /// patterns (load-time validation). Carried on each finding as <c>Fix</c>.
    /// </summary>
    public string? Fix { get; set; }

    /// <summary>
    /// Arbitrary rule metadata (YAML <c>metadata:</c> map, Semgrep-style): free-form
    /// key/value pairs (e.g. <c>cwe</c>, <c>owasp</c>, <c>confidence</c>). Surfaced in
    /// SARIF rule properties when the writer is given the rule table.
    /// </summary>
    public Dictionary<string, object?>? Metadata { get; set; }

    /// <summary>Parsed severity value.</summary>
    public LabyrinthSeverity SeverityValue => ParseSeverity(Severity);

    /// <summary>Parsed rule type value.</summary>
    public LabyrinthRuleType TypeValue => ParseType(Type);

    private static LabyrinthSeverity ParseSeverity(string s) => (s ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        "ERROR" => LabyrinthSeverity.Error,
        "WARNING" => LabyrinthSeverity.Warning,
        "INFO" => LabyrinthSeverity.Info,
        _ => throw new LabyrinthRuleException($"Unknown severity '{s}'. Expected ERROR, WARNING or INFO."),
    };

    private static LabyrinthRuleType ParseType(string s) => (s ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "search" => LabyrinthRuleType.Search,
        "taint" => LabyrinthRuleType.Taint,
        _ => throw new LabyrinthRuleException($"Unknown rule type '{s}'. Expected 'search' or 'taint'."),
    };
}

/// <summary>
/// A parsed Labyrinth rule file: a set of rules sharing a file.
/// </summary>
public sealed class LabyrinthRuleSet
{
    public const string RootKey = "rules";

    /// <summary>The rules loaded from a single YAML file.</summary>
    public List<LabyrinthRule> Rules { get; set; } = new();

    /// <summary>
    /// The rules that apply to a scanned file path: rules without a <c>paths:</c>
    /// filter always apply; rules with one apply when the filter matches.
    /// Null path (unknown): only unfiltered rules apply.
    /// </summary>
    public IEnumerable<LabyrinthRule> RulesForPath(string? filePath) =>
        Rules.Where(r => r.Paths is null || r.Paths.Matches(filePath));

    /// <summary>
    /// All metavariable names defined across the rule's pattern entries,
    /// in order of first occurrence (unification scope is the single rule).
    /// </summary>
    public static IEnumerable<string> DefinedMetavariables(LabyrinthRule rule)
    {
        var seen = new HashSet<string>();
        foreach (var p in EnumeratePatterns(rule))
        {
            foreach (var name in LabyrinthMetavariables.NamesIn(p))
            {
                if (seen.Add(name))
                {
                    yield return name;
                }
            }
        }
    }

    internal static IEnumerable<string> EnumeratePatterns(LabyrinthRule rule)
    {
        if (rule.Pattern is not null)
        {
            yield return rule.Pattern;
        }

        if (!string.IsNullOrWhiteSpace(rule.PatternNot))
        {
            yield return rule.PatternNot;
        }

        if (!string.IsNullOrWhiteSpace(rule.PatternInside))
        {
            yield return rule.PatternInside;
        }

        foreach (var list in new[] { rule.Sources, rule.Sinks, rule.Sanitizers })
        {
            if (list is null)
            {
                continue;
            }

            foreach (var e in list)
            {
                if (e is null)
                {
                    continue;
                }

                if (e.Pattern is not null)
                {
                    yield return e.Pattern;
                }

                if (!string.IsNullOrWhiteSpace(e.PatternNot))
                {
                    yield return e.PatternNot;
                }

                if (!string.IsNullOrWhiteSpace(e.PatternInside))
                {
                    yield return e.PatternInside;
                }

                if (e.PatternEither is not null)
                {
                    foreach (var alt in e.PatternEither)
                    {
                        if (alt is null)
                        {
                            continue;
                        }

                        if (alt.Pattern is not null)
                        {
                            yield return alt.Pattern;
                        }

                        if (!string.IsNullOrWhiteSpace(alt.PatternNot))
                        {
                            yield return alt.PatternNot;
                        }

                        if (!string.IsNullOrWhiteSpace(alt.PatternInside))
                        {
                            yield return alt.PatternInside;
                        }
                    }
                }
            }
        }

        if (rule.Propagators is not null)
        {
            foreach (var p in rule.Propagators)
            {
                if (p?.Pattern is not null)
                {
                    yield return p.Pattern;
                }
            }
        }
    }
}

/// <summary>Thrown when a Labyrinth rule file or rule is invalid.</summary>
public sealed class LabyrinthRuleException : Exception
{
    public LabyrinthRuleException(string message) : base(message)
    {
    }

    public LabyrinthRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
