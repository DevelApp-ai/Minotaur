using System.Text.Json;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Layer 5 tests for the Labyrinth rule engine (Minotaur issue #105):
/// console reporting, SARIF serialization, exit codes, baselines and
/// rule-pack directory loading with per-rule validation errors.
/// </summary>
public sealed class LabyrinthReportingTests
{
    private sealed record LocatedNode(
        string NodeType,
        string? Name,
        string FilePath,
        int StartLine,
        int StartColumn) : ILabyrinthMatchNode, ILabyrinthFindingLocation
    {
        public IReadOnlyList<ILabyrinthMatchNode> Arguments { get; } = Array.Empty<ILabyrinthMatchNode>();
    }

    private static LabyrinthTaintFinding MakeFinding(int line = 10, string ruleId = "custom-grammar-injection")
    {
        var source = new LocatedNode("FunctionCall", "ReceiveData", "app.cs", line, 5);
        var temp = new LocatedNode("Identifier", "temp", "app.cs", line + 1, 9);
        var sink = new LocatedNode("FunctionCall", "ExecuteAction", "app.cs", line + 2, 1);
        var path = new ILabyrinthMatchNode[] { source, temp, sink };
        return new LabyrinthTaintFinding(
            ruleId, LabyrinthSeverity.Error, "Untrusted data flows into a critical execution sink.",
            source, sink, path);
    }

    // ----- Console reporter -----

    [Fact]
    public void ConsoleReporter_WritesFindingBlocks_AndReturnsFindingsExitCode()
    {
        var writer = new StringWriter();
        var reporter = new LabyrinthConsoleReporter(writer);
        var exitCode = reporter.Report(new[] { MakeFinding() });

        var output = writer.ToString();
        Assert.Equal(LabyrinthExitCodes.Findings, exitCode);
        Assert.Contains("ERROR [custom-grammar-injection] @app.cs:12:1", output);
        Assert.Contains("Untrusted data flows into a critical execution sink.", output);
        Assert.Contains("FunctionCall:ReceiveData @app.cs:10:5", output);
        Assert.Contains("-> FunctionCall:ExecuteAction @app.cs:12:1", output);
        Assert.Contains("1 finding(s).", output);
    }

    [Fact]
    public void ConsoleReporter_NoFindings_ReturnsSuccessExitCode()
    {
        var reporter = new LabyrinthConsoleReporter(new StringWriter());
        Assert.Equal(LabyrinthExitCodes.Success, reporter.Report(Array.Empty<LabyrinthTaintFinding>()));
    }

    [Fact]
    public void ConsoleReporter_ReportsSuppressedBaselineCount()
    {
        var writer = new StringWriter();
        var reporter = new LabyrinthConsoleReporter(writer);
        reporter.Report(new[] { MakeFinding() }, suppressedCount: 3);
        Assert.Contains("1 finding(s), 3 suppressed by baseline.", writer.ToString());
    }

    // ----- SARIF -----

    [Fact]
    public void SarifWriter_ProducesValidSarif21_WithCodeFlow()
    {
        var sarif = LabyrinthSarifWriter.Write(new[] { MakeFinding() }, root: "/repo/");

        using var document = JsonDocument.Parse(sarif);
        var root = document.RootElement;
        Assert.Equal("2.1.0", root.GetProperty("version").GetString());

        var run = root.GetProperty("runs")[0];
        Assert.Equal("Minotaur.Labyrinth", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.Equal("/repo/", run.GetProperty("originalUriBaseIds").GetProperty("ROOT").GetProperty("uri").GetString());

        var rule = run.GetProperty("tool").GetProperty("driver").GetProperty("rules")[0];
        Assert.Equal("custom-grammar-injection", rule.GetProperty("id").GetString());

        var result = run.GetProperty("results")[0];
        Assert.Equal("custom-grammar-injection", result.GetProperty("ruleId").GetString());
        Assert.Equal("error", result.GetProperty("level").GetString());
        Assert.Equal(
            "Untrusted data flows into a critical execution sink.",
            result.GetProperty("message").GetProperty("text").GetString());

        var physical = result.GetProperty("locations")[0].GetProperty("physicalLocation");
        Assert.Equal("app.cs", physical.GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal("ROOT", physical.GetProperty("artifactLocation").GetProperty("uriBaseId").GetString());
        Assert.Equal(12, physical.GetProperty("region").GetProperty("startLine").GetInt32());

        var threadFlow = result.GetProperty("codeFlows")[0].GetProperty("threadFlows")[0].GetProperty("locations");
        Assert.Equal(3, threadFlow.GetArrayLength());
        Assert.Equal("app.cs", threadFlow[0].GetProperty("location").GetProperty("physicalLocation")
            .GetProperty("artifactLocation").GetProperty("uri").GetString());
    }

    [Theory]
    [InlineData(LabyrinthSeverity.Error, "error")]
    [InlineData(LabyrinthSeverity.Warning, "warning")]
    [InlineData(LabyrinthSeverity.Info, "note")]
    public void SarifWriter_MapsSeverityToLevel(LabyrinthSeverity severity, string expectedLevel)
    {
        var finding = new LabyrinthTaintFinding(
            "rule-1", severity, "msg",
            new LabyrinthMatchNode("Identifier", "a"),
            new LabyrinthMatchNode("Identifier", "b"),
            new ILabyrinthMatchNode[] { new LabyrinthMatchNode("Identifier", "a"), new LabyrinthMatchNode("Identifier", "b") });

        var sarif = LabyrinthSarifWriter.Write(new[] { finding });
        using var document = JsonDocument.Parse(sarif);
        Assert.Equal(expectedLevel, document.RootElement.GetProperty("runs")[0]
            .GetProperty("results")[0].GetProperty("level").GetString());
    }

    // ----- Baseline -----

    [Fact]
    public void Baseline_Fingerprint_IsStableAndSensitiveToSinkLocation()
    {
        var finding = MakeFinding();
        Assert.Equal(LabyrinthBaseline.Fingerprint(finding), LabyrinthBaseline.Fingerprint(MakeFinding()));
        Assert.NotEqual(LabyrinthBaseline.Fingerprint(finding), LabyrinthBaseline.Fingerprint(MakeFinding(line: 42)));
    }

    [Fact]
    public void Baseline_RoundTrips_AndSuppressesKnownFindings()
    {
        var findings = new[] { MakeFinding(), MakeFinding(line: 30, ruleId: "second-rule") };
        var json = SerializeFindings(findings);

        var baseline = LabyrinthBaseline.Parse(json);
        Assert.Equal(2, baseline.Fingerprints.Count);

        var reportable = baseline.Apply(findings, out var suppressed);
        Assert.Empty(reportable);
        Assert.Equal(2, suppressed);
    }

    [Fact]
    public void Baseline_KeepsNewFindings()
    {
        var baseline = LabyrinthBaseline.Parse(SerializeFindings(new[] { MakeFinding() }));

        var findings = new[] { MakeFinding(), MakeFinding(line: 77, ruleId: "new-rule") };
        var reportable = baseline.Apply(findings, out var suppressed);
        Assert.Single(reportable);
        Assert.Equal("new-rule", reportable[0].RuleId);
        Assert.Equal(1, suppressed);
    }

    [Fact]
    public void Baseline_Load_MissingFile_ThrowsRuleException()
    {
        Assert.Throws<LabyrinthRuleException>(() => LabyrinthBaseline.Load("/does/not/exist.json"));
    }

    private static string SerializeFindings(IReadOnlyList<LabyrinthTaintFinding> findings)
    {
        var fingerprints = findings.Select(LabyrinthBaseline.Fingerprint).Distinct().ToList();
        return LabyrinthBaseline.Serialize(fingerprints);
    }

    // ----- Rule pack loading (per-rule errors) -----

    [Fact]
    public void RulePack_TryLoadDirectory_ReportsErrorsPerRule_AndLoadsValidRules()
    {
        var dir = Path.Combine(Path.GetTempPath(), "labyrinth-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "good.yaml"), """
                rules:
                  - id: good-rule
                    severity: ERROR
                    type: search
                    pattern: "DoThing($X)"
                """);
            File.WriteAllText(Path.Combine(dir, "bad.yaml"), """
                rules:
                  - id: broken-rule-1
                    severity: HIGH
                    type: search
                    pattern: "DoThing($X)"
                  - id: broken-rule-2
                    type: taint
                    sources:
                      - pattern: "Receive($DATA)"
                """);

            var result = new LabyrinthRulePackLoader().TryLoadDirectory(dir);

            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, e => e.Contains("bad.yaml: rules[0]:") && e.Contains("HIGH"));
            Assert.Contains(result.Errors, e => e.Contains("bad.yaml: rules[1]:") && e.Contains("sink"));
            Assert.Single(result.RuleSet.Rules);
            Assert.Equal("good-rule", result.RuleSet.Rules[0].Id);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void RulePack_TryLoadDirectory_DetectsDuplicateRuleIds_AcrossFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "labyrinth-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var yaml = """
                rules:
                  - id: same-id
                    severity: WARNING
                    type: search
                    pattern: "DoThing($X)"
                """;
            File.WriteAllText(Path.Combine(dir, "a.yaml"), yaml);
            File.WriteAllText(Path.Combine(dir, "b.yaml"), yaml);

            var result = new LabyrinthRulePackLoader().TryLoadDirectory(dir);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("duplicate rule id 'same-id'"));
            Assert.Single(result.RuleSet.Rules);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void RulePack_TryLoadDirectory_MissingDirectory_ReportsError()
    {
        var result = new LabyrinthRulePackLoader().TryLoadDirectory("/does/not/exist");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("does not exist"));
    }

    [Fact]
    public void RulePack_LoadDirectory_ThrowsWithAllErrors_WhenInvalid()
    {
        var dir = Path.Combine(Path.GetTempPath(), "labyrinth-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "bad.yaml"), """
                rules:
                  - id: broken-rule
                    severity: SUPERBAD
                    type: search
                    pattern: "DoThing($X)"
                """);

            var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRulePackLoader().LoadDirectory(dir));
            Assert.Contains("broken-rule", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void RulePack_LoadDirectory_MergesValidPack()
    {
        var dir = Path.Combine(Path.GetTempPath(), "labyrinth-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "sub"));
            File.WriteAllText(Path.Combine(dir, "a.yaml"), """
                rules:
                  - id: rule-a
                    severity: WARNING
                    type: search
                    pattern: "DoThing($X)"
                """);
            File.WriteAllText(Path.Combine(dir, "sub", "b.yml"), """
                rules:
                  - id: rule-b
                    severity: ERROR
                    type: search
                    pattern: "OtherThing($X)"
                """);

            var ruleSet = new LabyrinthRulePackLoader().LoadDirectory(dir);
            Assert.Equal(new[] { "rule-a", "rule-b" }, ruleSet.Rules.Select(r => r.Id).OrderBy(i => i).ToArray());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
