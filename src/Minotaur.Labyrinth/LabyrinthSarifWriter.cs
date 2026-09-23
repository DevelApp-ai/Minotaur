using System.Text.Encodings.Web;
using System.Text.Json;

namespace Minotaur.Labyrinth;

/// <summary>
/// Serializes Labyrinth findings to SARIF 2.1.0 for GitHub code scanning
/// (Labyrinth Layer 5, issue #105). Uses <see cref="System.Text.Json"/> (no
/// external dependency). The full source→sink path is emitted as a
/// <c>codeFlow</c> threadFlow when the path nodes carry locations.
/// </summary>
public static class LabyrinthSarifWriter
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serializes findings to a SARIF 2.1.0 log document.</summary>
    public static string Write(IReadOnlyList<LabyrinthTaintFinding> findings, string? root = null)
    {
        if (findings is null)
        {
            throw new ArgumentNullException(nameof(findings));
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, Options))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json");
            writer.WriteString("version", "2.1.0");
            writer.WriteStartArray("runs");

            writer.WriteStartObject();
            writer.WriteStartObject("tool");
            writer.WriteStartObject("driver");
            writer.WriteString("name", "Minotaur.Labyrinth");
            writer.WriteString("informationUri", "https://github.com/DevelApp-ai/Minotaur");
            writer.WriteString("version", typeof(LabyrinthSarifWriter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");
            WriteRules(writer, findings);
            writer.WriteEndObject(); // driver
            writer.WriteEndObject(); // tool

            if (!string.IsNullOrEmpty(root))
            {
                writer.WriteStartObject("originalUriBaseIds");
                writer.WriteStartObject("ROOT");
                writer.WriteString("uri", RootUri(root));
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteStartArray("results");
            foreach (var finding in findings)
            {
                WriteResult(writer, finding, root);
            }

            writer.WriteEndArray(); // results
            writer.WriteEndObject(); // run

            writer.WriteEndArray(); // runs
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteRules(Utf8JsonWriter writer, IReadOnlyList<LabyrinthTaintFinding> findings)
    {
        var rules = findings
            .GroupBy(f => f.RuleId, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        writer.WriteStartArray("rules");
        foreach (var group in rules)
        {
            writer.WriteStartObject();
            writer.WriteString("id", group.Key);
            writer.WriteStartObject("properties");
            writer.WriteString("tags", "security");
            writer.WriteEndObject();
            writer.WriteStartObject("defaultConfiguration");
            writer.WriteString("level", SarifLevel(group.First().Severity));
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteResult(Utf8JsonWriter writer, LabyrinthTaintFinding finding, string? root)
    {
        writer.WriteStartObject();
        writer.WriteString("ruleId", finding.RuleId);
        writer.WriteString("level", SarifLevel(finding.Severity));
        writer.WriteStartObject("message");
        writer.WriteString("text", finding.Message);
        writer.WriteEndObject();
        WriteLocations(writer, finding, root);
        WriteCodeFlows(writer, finding, root);
        writer.WriteEndObject();
    }

    private static void WriteLocations(Utf8JsonWriter writer, LabyrinthTaintFinding finding, string? root)
    {
        writer.WriteStartArray("locations");
        writer.WriteStartObject();
        writer.WriteStartObject("physicalLocation");

        if (finding.Sink is ILabyrinthFindingLocation sink)
        {
            WriteArtifactLocation(writer, sink.FilePath, root);
            writer.WriteStartObject("region");
            writer.WriteNumber("startLine", Math.Max(1, sink.StartLine));
            writer.WriteNumber("startColumn", Math.Max(1, sink.StartColumn));
            writer.WriteEndObject();
        }
        else
        {
            WriteArtifactLocation(writer, "<unknown>", root);
            writer.WriteStartObject("region");
            writer.WriteNumber("startLine", 1);
            writer.WriteNumber("startColumn", 1);
            writer.WriteEndObject();
        }

        writer.WriteEndObject(); // physicalLocation
        writer.WriteEndObject(); // location
        writer.WriteEndArray();
    }

    private static void WriteCodeFlows(Utf8JsonWriter writer, LabyrinthTaintFinding finding, string? root)
    {
        var located = finding.Path.OfType<ILabyrinthFindingLocation>().ToList();
        if (located.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("codeFlows");
        writer.WriteStartObject();
        writer.WriteStartArray("threadFlows");
        writer.WriteStartObject();
        writer.WriteStartArray("locations");

        foreach (var node in finding.Path)
        {
            writer.WriteStartObject();
            writer.WriteStartObject("location");
            writer.WriteStartObject("physicalLocation");
            if (node is ILabyrinthFindingLocation locatedNode)
            {
                WriteArtifactLocation(writer, locatedNode.FilePath, root);
                writer.WriteStartObject("region");
                writer.WriteNumber("startLine", Math.Max(1, locatedNode.StartLine));
                writer.WriteNumber("startColumn", Math.Max(1, locatedNode.StartColumn));
                writer.WriteEndObject();
            }
            else
            {
                WriteArtifactLocation(writer, "<unknown>", root);
                writer.WriteStartObject("region");
                writer.WriteNumber("startLine", 1);
                writer.WriteNumber("startColumn", 1);
                writer.WriteEndObject();
            }

            writer.WriteEndObject(); // physicalLocation

            writer.WriteStartObject("message");
            writer.WriteString("text", LabyrinthPathRenderer.Render(node));
            writer.WriteEndObject();

            writer.WriteEndObject(); // location
            writer.WriteEndObject();
        }

        writer.WriteEndArray(); // locations
        writer.WriteEndObject(); // threadFlow
        writer.WriteEndArray(); // threadFlows
        writer.WriteEndObject(); // codeFlow
        writer.WriteEndArray(); // codeFlows
    }

    private static void WriteArtifactLocation(Utf8JsonWriter writer, string filePath, string? root)
    {
        writer.WriteStartObject("artifactLocation");
        writer.WriteString("uri", filePath);
        if (!string.IsNullOrEmpty(root))
        {
            writer.WriteString("uriBaseId", "ROOT");
        }

        writer.WriteEndObject();
    }

    private static string SarifLevel(LabyrinthSeverity severity) => severity switch
    {
        LabyrinthSeverity.Error => "error",
        LabyrinthSeverity.Warning => "warning",
        _ => "note",
    };

    private static string RootUri(string root)
    {
        var normalized = root.Replace('\\', '/');
        if (!normalized.EndsWith('/'))
        {
            normalized += "/";
        }

        return normalized;
    }
}
