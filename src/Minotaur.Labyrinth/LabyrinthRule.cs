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
    /// All metavariable names defined across the rule's pattern entries,
    /// in order of first occurrence (unification scope is the single rule).
    /// </summary>
    public static IEnumerable<string> DefinedMetavariables(LabyrinthRule rule)
    {
        var seen = new HashSet<string>();
        foreach (var p in EnumeratePatterns(rule))
        {
            foreach (var name in LabyrinthPatternLexer.MetavariableNames(p))
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

        foreach (var list in new[] { rule.Sources, rule.Sinks, rule.Sanitizers })
        {
            if (list is null)
            {
                continue;
            }

            foreach (var e in list)
            {
                if (e?.Pattern is not null)
                {
                    yield return e.Pattern;
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
