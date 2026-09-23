using System.Security.Cryptography;
using System.Text;

namespace Minotaur.Labyrinth;

/// <summary>
/// Baseline support for CI pipelines (Labyrinth Layer 5, issue #105).
/// A baseline stores a stable fingerprint per accepted finding; on the next
/// run, findings whose fingerprint is present are suppressed so the pipeline
/// exit code only signals *new* findings.
/// </summary>
public sealed class LabyrinthBaseline
{
    /// <summary>The fingerprints accepted in this baseline, as read from the baseline file.</summary>
    public IReadOnlyCollection<string> Fingerprints { get; private set; } = Array.Empty<string>();

    private LabyrinthBaseline(IReadOnlyCollection<string> fingerprints)
    {
        Fingerprints = fingerprints;
    }

    /// <summary>Computes the stable fingerprint for a finding.</summary>
    /// <remarks>
    /// The fingerprint is a SHA-256 hash over the rule id, severity, the sink
    /// identity and the source identity. Identities use the CognitiveGraph
    /// location when the match node provides one (file + line + column +
    /// node type/name); otherwise they fall back to the structural identity
    /// (node type + name), which is stable for a fixed input file.
    /// </remarks>
    public static string Fingerprint(LabyrinthTaintFinding finding)
    {
        var builder = new StringBuilder(128);
        builder.Append(finding.RuleId).Append('\u0001');
        builder.Append(finding.Severity).Append('\u0001');
        builder.Append(Identity(finding.Sink)).Append('\u0001');
        builder.Append(Identity(finding.Source));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Writes a baseline file covering every finding in <paramref name="findings"/>.</summary>
    public static void Save(string path, IEnumerable<LabyrinthTaintFinding> findings)
    {
        var fingerprints = findings.Select(Fingerprint).Distinct(StringComparer.Ordinal).ToList();
        File.WriteAllText(path, Serialize(fingerprints));
    }

    /// <summary>Serializes fingerprints to the baseline file format (JSON).</summary>
    public static string Serialize(IEnumerable<string> fingerprints)
    {
        var distinct = fingerprints.Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"fingerprints\": [");
        for (var i = 0; i < distinct.Count; i++)
        {
            sb.Append("    \"").Append(distinct[i]).Append('"');
            sb.AppendLine(i == distinct.Count - 1 ? string.Empty : ",");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Deserializes a baseline file (JSON) previously written by <see cref="Save"/> or <see cref="Serialize"/>.</summary>
    public static LabyrinthBaseline Parse(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("fingerprints", out var array))
        {
            throw new LabyrinthRuleException("Invalid baseline file: missing 'fingerprints' array.");
        }

        var fingerprints = array.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();

        return new LabyrinthBaseline(fingerprints);
    }

    /// <summary>Loads a baseline file from disk.</summary>
    public static LabyrinthBaseline Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new LabyrinthRuleException($"Baseline file does not exist: '{path}'.");
        }

        return Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Partitions <paramref name="findings"/> into reportable findings and
    /// findings suppressed by this baseline. The return value is the list to
    /// report; <paramref name="suppressedCount"/> receives how many findings
    /// the baseline absorbed.
    /// </summary>
    public IReadOnlyList<LabyrinthTaintFinding> Apply(
        IReadOnlyList<LabyrinthTaintFinding> findings, out int suppressedCount)
    {
        var reportable = new List<LabyrinthTaintFinding>(findings.Count);
        suppressedCount = 0;
        foreach (var finding in findings)
        {
            if (Fingerprints.Contains(Fingerprint(finding)))
            {
                suppressedCount++;
            }
            else
            {
                reportable.Add(finding);
            }
        }

        return reportable;
    }

    private static string Identity(ILabyrinthMatchNode node)
    {
        var sb = new StringBuilder(64);
        sb.Append(node.NodeType);
        if (node.Name is not null)
        {
            sb.Append(':').Append(node.Name);
        }

        if (node is ILabyrinthFindingLocation location)
        {
            sb.Append('@').Append(location.FilePath)
                .Append(':').Append(location.StartLine)
                .Append(':').Append(location.StartColumn);
        }

        return sb.ToString();
    }
}
