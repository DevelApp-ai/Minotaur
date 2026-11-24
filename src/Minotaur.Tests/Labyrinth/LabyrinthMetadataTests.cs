using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for rule metadata blocks (YAML <c>metadata:</c> free-form map, Labyrinth
/// issue #246, LAB track): parsing, validation, and surfacing into SARIF rule
/// properties.
/// </summary>
public sealed class LabyrinthMetadataTests
{
    // ------------------------------------------------------------------
    // Loader
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_ParsesMetadataBlock()
    {
        const string yaml = """
            rules:
              - id: sql-injection
                severity: ERROR
                type: search
                pattern: "Execute($Q)"
                metadata:
                  cwe: "CWE-89"
                  owasp: "A03:2021-Injection"
                  confidence: high
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        var rule = Assert.Single(ruleSet.Rules);
        Assert.NotNull(rule.Metadata);
        Assert.Equal(3, rule.Metadata!.Count);
        Assert.Equal("CWE-89", rule.Metadata["cwe"]);
        Assert.Equal("A03:2021-Injection", rule.Metadata["owasp"]);
        Assert.Equal("high", rule.Metadata["confidence"]);
    }

    [Fact]
    public void YamlLoader_RuleWithoutMetadata_HasNull()
    {
        const string yaml = """
            rules:
              - id: plain
                type: search
                pattern: "Do($X)"
            """;

        Assert.Null(Assert.Single(new LabyrinthRuleLoader().Load(yaml).Rules).Metadata);
    }

    // ------------------------------------------------------------------
    // SARIF surfacing
    // ------------------------------------------------------------------

    [Fact]
    public void SarifWriter_EmitsRuleMetadataIntoRuleProperties()
    {
        var yaml = """
            rules:
              - id: sql-injection
                severity: ERROR
                type: search
                pattern: "Execute($Q)"
                metadata:
                  cwe: "CWE-89"
                  severity-rank: 9
            """;
        var ruleSet = new LabyrinthRuleLoader().Load(yaml);
        var rule = ruleSet.Rules[0];

        var node = new LabyrinthMatchNode("FunctionCall", "Execute");
        var finding = new LabyrinthTaintFinding(
            rule.Id, rule.SeverityValue, rule.EffectiveMessage, node, node, new ILabyrinthMatchNode[] { node });

        var rulesById = new Dictionary<string, LabyrinthRule>(StringComparer.Ordinal) { [rule.Id] = rule };
        var sarif = LabyrinthSarifWriter.Write(new[] { finding }, rulesById: rulesById);

        Assert.Contains("\"cwe\": \"CWE-89\"", sarif);
        Assert.Contains("\"severity-rank\": 9", sarif);

        // Without the rule table, no metadata is emitted (properties keep just tags).
        var sarifNoMeta = LabyrinthSarifWriter.Write(new[] { finding });
        Assert.DoesNotContain("CWE-89", sarifNoMeta);
    }
}
