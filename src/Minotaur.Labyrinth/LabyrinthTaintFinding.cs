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

    /// <summary>
    /// The rule's fix suggestion with metavariables interpolated against this
    /// finding's bindings (YAML <c>fix:</c>); null when the rule declares none.
    /// </summary>
    public string? Fix { get; }

    /// <summary>
    /// Attribution: where the rule that produced this finding comes from
    /// (rule YAML <c>source:</c>, e.g. <c>gitleaks</c> or
    /// <c>cve:CVE-2024-12345</c>); null when the rule declares none.
    /// Presented with the finding in the console and SARIF output so
    /// harvested rules keep their provenance where the pattern is found
    /// (Minotaur issue #283).
    /// </summary>
    public string? ProvenanceSource { get; }

    /// <summary>Attribution URL (rule YAML <c>source-url:</c>); null when absent.</summary>
    public string? ProvenanceSourceUrl { get; }

    /// <summary>SPDX license of the rule content (rule YAML <c>license:</c>); null when absent.</summary>
    public string? ProvenanceLicense { get; }

    /// <summary>
    /// Creates a finding. Public so hosts and the reporting layer (issue #105)
    /// can construct findings — e.g. from a persisted scan — without going
    /// through the taint engine.
    /// </summary>
    public LabyrinthTaintFinding(
        string ruleId,
        LabyrinthSeverity severity,
        string message,
        ILabyrinthMatchNode source,
        ILabyrinthMatchNode sink,
        IReadOnlyList<ILabyrinthMatchNode> path,
        string? fix = null,
        string? provenanceSource = null,
        string? provenanceSourceUrl = null,
        string? provenanceLicense = null)
    {
        RuleId = ruleId;
        Severity = severity;
        Message = message;
        Source = source;
        Sink = sink;
        Path = path;
        Fix = fix;
        ProvenanceSource = provenanceSource;
        ProvenanceSourceUrl = provenanceSourceUrl;
        ProvenanceLicense = provenanceLicense;
    }
}
