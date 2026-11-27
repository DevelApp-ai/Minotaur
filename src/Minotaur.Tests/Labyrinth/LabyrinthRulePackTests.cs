using System.Text;
using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Binary rule pack tests (Minotaur issue #283): a pack built from YAML rules
/// round-trips the compiled rules (pattern mini-ASTs, metavariable tables,
/// attribution, metadata, path filters), loads through the packed scan path
/// with findings identical to the YAML path, refuses grammar name/hash drift,
/// and presents the rule attribution where the pattern is found (console and
/// SARIF). Uses an inline target grammar, so no downloaded grammars are
/// needed.
/// </summary>
public sealed class LabyrinthRulePackTests : IDisposable
{
    private const string TestGrammar = """
        Grammar: TestLangPack
        FormatType: EBNF

        <WS> ::= /[ \t\r\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <SEMICOLON> ::= ";"
        <EQUALS> ::= "="
        <DOT> ::= "."
        <SLASH> ::= "/"

        <program> ::= <statement> ;
        <statement> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | SEMICOLON | EQUALS | DOT | SLASH ;
        """;

    private const string RulesYaml = """
        rules:
          - id: test-maybe-null-deref
            severity: ERROR
            message: "Value from a maybe-null API is dereferenced without a null guard."
            type: taint
            source: test-corpus
            source-url: https://example.org/test-maybe-null-deref
            license: MIT
            metadata:
              cwe: "476"
              confidence: HIGH
            sources:
              - pattern: "$TARGET = $DATA.FirstOrDefault(...)"
            sinks:
              - pattern: "$TARGET.ToString()"

          - id: test-constant-zero-division
            severity: WARNING
            message: "Division by the constant zero."
            type: search
            pattern: "$A / 0"
            source: test-corpus
            license: CC-BY-4.0
            paths:
              include:
                - "**/*.cs"
        """;

    private const string VulnerableSource = """
        var x = db.FirstOrDefault();
        x.ToString();
        var q = a / 0;
        """;

    private readonly string _root;

    public LabyrinthRulePackTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "labyrinth-pack-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private string RulesDirectory()
    {
        var dir = Path.Combine(_root, "rules");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "rules.yaml"), RulesYaml);
        return dir;
    }

    private static List<LabyrinthTaintFinding> ScanYaml(LabyrinthRuleSet ruleSet, string filePath)
    {
        var graph = ScanHost.BuildGraph(VulnerableSource, filePath, TestGrammar, "TestLangPack", out var nodes);
        return ScanHost.RunRules(ruleSet, TestGrammar, "TestLangPack", graph, nodes, filePath);
    }

    [Fact]
    public void Yaml_ProvenanceFields_Parse()
    {
        var ruleSet = new LabyrinthRuleLoader().Parse(RulesYaml);

        var taint = ruleSet.Rules.Single(r => r.Id == "test-maybe-null-deref");
        Assert.Equal("test-corpus", taint.Source);
        Assert.Equal("https://example.org/test-maybe-null-deref", taint.SourceUrl);
        Assert.Equal("MIT", taint.License);

        var search = ruleSet.Rules.Single(r => r.Id == "test-constant-zero-division");
        Assert.Equal("test-corpus", search.Source);
        Assert.Null(search.SourceUrl);
        Assert.Equal("CC-BY-4.0", search.License);
    }

    [Fact]
    public void Pack_RoundTripsRulesManifestAndProvenance()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        var built = LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath, "test-pack");
        Assert.True(built.IsValid, string.Join("; ", built.Errors));

        var read = LabyrinthRulePack.TryRead(packPath, "TestLangPack", LabyrinthRulePack.GrammarSha256(TestGrammar));
        Assert.True(read.IsValid, string.Join("; ", read.Errors));

        var manifest = read.Manifest!;
        Assert.Equal("test-pack", manifest.PackId);
        Assert.Equal("TestLangPack", manifest.TargetGrammarName);
        Assert.Equal(LabyrinthRulePack.GrammarSha256(TestGrammar), manifest.GrammarSha256);
        Assert.Equal(2, manifest.RuleCount);

        var taint = read.Rules.Single(r => r.Rule.Id == "test-maybe-null-deref");
        Assert.Equal("ERROR", taint.Rule.Severity);
        Assert.Equal("test-corpus", taint.Rule.Source);
        Assert.Equal("https://example.org/test-maybe-null-deref", taint.Rule.SourceUrl);
        Assert.Equal("MIT", taint.Rule.License);
        Assert.Equal("476", taint.Rule.Metadata!["cwe"] as string);
        Assert.Equal("HIGH", taint.Rule.Metadata!["confidence"] as string);
        Assert.Equal("TARGET", taint.Metavariables[0]);
        Assert.Equal("$TARGET = $DATA.FirstOrDefault(...)", taint.Sources[0].Alternatives[0].Pattern.Pattern);
        Assert.True(taint.Sources[0].Alternatives[0].Pattern.Nodes.Count > 0);
        Assert.Equal("$TARGET.ToString()", taint.Sinks[0].Alternatives[0].Pattern.Pattern);
        Assert.Equal(2, taint.MetavariableOccurrences["TARGET"]);

        var search = read.Rules.Single(r => r.Rule.Id == "test-constant-zero-division");
        Assert.Equal("$A / 0", search.SearchPattern!.Pattern);
        Assert.Equal("CC-BY-4.0", search.Rule.License);
        Assert.Equal("**/*.cs", Assert.Single(search.Rule.Paths!.Include));
    }

    [Fact]
    public void Pack_Findings_MatchYamlPath_AndCarryProvenance()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");
        var packed = LabyrinthRulePack.TryRead(packPath, "TestLangPack", LabyrinthRulePack.GrammarSha256(TestGrammar));
        Assert.True(packed.IsValid, string.Join("; ", packed.Errors));

        var filePath = Path.Combine(_root, "vulnerable.cs");
        var yamlFindings = ScanYaml(new LabyrinthRuleLoader().Parse(RulesYaml), filePath);

        var graph = ScanHost.BuildGraph(VulnerableSource, filePath, TestGrammar, "TestLangPack", out var nodes);
        var packedFindings = ScanHost.RunCompiledRules(packed.Rules, graph, nodes, filePath);

        Assert.Equal(yamlFindings.Count, packedFindings.Count);
        foreach (var (yaml, packedFinding) in yamlFindings.Zip(packedFindings, (a, b) => (a, b)))
        {
            Assert.Equal(yaml.RuleId, packedFinding.RuleId);
            Assert.Equal(yaml.Severity, packedFinding.Severity);
            Assert.Equal(
                ((ILabyrinthFindingLocation)yaml.Sink).StartLine,
                ((ILabyrinthFindingLocation)packedFinding.Sink).StartLine);
        }

        var taintFinding = packedFindings.Single(f => f.RuleId == "test-maybe-null-deref");
        Assert.Equal("test-corpus", taintFinding.ProvenanceSource);
        Assert.Equal("https://example.org/test-maybe-null-deref", taintFinding.ProvenanceSourceUrl);
        Assert.Equal("MIT", taintFinding.ProvenanceLicense);
    }

    [Fact]
    public void Pack_RejectsGrammarHashMismatch()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");

        var changedGrammar = TestGrammar + "\n# grammar changed\n";
        var read = LabyrinthRulePack.TryRead(
            packPath, "TestLangPack", LabyrinthRulePack.GrammarSha256(changedGrammar));

        Assert.False(read.IsValid);
        Assert.Contains("rebuild the pack", Assert.Single(read.Errors));
    }

    [Fact]
    public void Pack_RejectsGrammarNameMismatch()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");

        var read = LabyrinthRulePack.TryRead(packPath, expectedTargetGrammarName: "OtherLang");

        Assert.False(read.IsValid);
        Assert.Contains("built for target grammar", Assert.Single(read.Errors));
    }

    [Fact]
    public void Pack_RejectsNonPackFile()
    {
        var notAPack = Path.Combine(_root, "not-a-pack.labyrinthpack");
        File.WriteAllText(notAPack, "this is not a labyrinth rule pack");
        var read = LabyrinthRulePack.TryRead(notAPack);

        Assert.False(read.IsValid);
        Assert.Contains("bad magic", Assert.Single(read.Errors));
    }

    [Fact]
    public void Provenance_PresentedInConsoleAndSarif()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");
        var packed = LabyrinthRulePack.TryRead(packPath, "TestLangPack", LabyrinthRulePack.GrammarSha256(TestGrammar));
        Assert.True(packed.IsValid, string.Join("; ", packed.Errors));

        var filePath = Path.Combine(_root, "vulnerable.cs");
        var graph = ScanHost.BuildGraph(VulnerableSource, filePath, TestGrammar, "TestLangPack", out var nodes);
        var findings = ScanHost.RunCompiledRules(packed.Rules, graph, nodes, filePath);
        Assert.True(findings.Count > 0);

        var console = new StringWriter();
        new LabyrinthConsoleReporter(console).Report(findings);
        var consoleText = console.ToString();
        Assert.Contains("Rule source: test-corpus (license: MIT)", consoleText);
        Assert.Contains("Rule source: test-corpus (license: CC-BY-4.0)", consoleText);

        var rulesById = packed.Rules.Select(c => c.Rule)
            .GroupBy(r => r.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var sarif = LabyrinthSarifWriter.Write(findings, rulesById: rulesById);
        Assert.Contains("\"helpUri\": \"https://example.org/test-maybe-null-deref\"", sarif);
        Assert.Contains("\"source\": \"test-corpus\"", sarif);
        Assert.Contains("\"license\": \"CC-BY-4.0\"", sarif);
    }

    [Fact]
    public void Pack_FileCarriesMagicHeader()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");

        var header = new byte[8];
        using (var stream = File.OpenRead(packPath))
        {
            Assert.Equal(8, stream.Read(header, 0, 8));
        }

        Assert.Equal("MLBPK1\0\0", Encoding.ASCII.GetString(header));
    }

    [Fact]
    public void RunCompiledRules_IsStableAcrossRepeatedScans()
    {
        var packPath = Path.Combine(_root, "test.labyrinthpack");
        Assert.True(LabyrinthRulePack.Build(RulesDirectory(), TestGrammar, "TestLangPack", packPath).IsValid,
            "pack build failed");
        var packed = LabyrinthRulePack.TryRead(packPath, "TestLangPack", LabyrinthRulePack.GrammarSha256(TestGrammar));
        Assert.True(packed.IsValid, string.Join("; ", packed.Errors));

        var filePath = Path.Combine(_root, "vulnerable.cs");
        var first = default(List<LabyrinthTaintFinding>);
        for (var round = 0; round < 3; round++)
        {
            var graph = ScanHost.BuildGraph(VulnerableSource, filePath, TestGrammar, "TestLangPack", out var nodes);
            var findings = ScanHost.RunCompiledRules(packed.Rules, graph, nodes, filePath);
            if (first is null)
            {
                first = findings;
            }
            else
            {
                Assert.Equal(first.Count, findings.Count);
            }
        }

        Assert.True(first!.Count > 0);
    }
}
