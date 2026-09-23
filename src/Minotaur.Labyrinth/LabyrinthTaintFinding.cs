namespace Minotaur.Labyrinth;

/// <summary>
/// A confirmed vulnerability: tainted data reached a sink along a full
/// source→sink data-flow path (Labyrinth Layer 3, issue #103). Path rendering
/// and reporting formats (console, SARIF) are Layer 4 territory (issue #105);
/// this model carries everything those formats need.
/// </summary>
public sealed class LabyrinthTaintFinding
{
    /// <summary>The id of the rule that produced the finding.</summary>
    public string RuleId { get; }

    /// <summary>The severity of the rule.</summary>
    public LabyrinthSeverity Severity { get; }

    /// <summary>The rule's message.</summary>
    public string Message { get; }

    /// <summary>The source node where the untrusted data entered the flow.</summary>
    public ILabyrinthMatchNode Source { get; }

    /// <summary>The sink node where the tainted data arrived.</summary>
    public ILabyrinthMatchNode Sink { get; }

    /// <summary>
    /// The full data-flow path from <see cref="Source"/> (inclusive) to
    /// <see cref="Sink"/> (inclusive), in flow order. The shortest such path
    /// found by the fixed-point traversal is reported.
    /// </summary>
    public IReadOnlyList<ILabyrinthMatchNode> Path { get; }

    internal LabyrinthTaintFinding(
        string ruleId,
        LabyrinthSeverity severity,
        string message,
        ILabyrinthMatchNode source,
        ILabyrinthMatchNode sink,
        IReadOnlyList<ILabyrinthMatchNode> path)
    {
        RuleId = ruleId;
        Severity = severity;
        Message = message;
        Source = source;
        Sink = sink;
        Path = path;
    }
}
