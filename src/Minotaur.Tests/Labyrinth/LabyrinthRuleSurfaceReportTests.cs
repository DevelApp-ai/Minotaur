using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for <see cref="LabyrinthRuleSurfaceReport"/> (Minotaur issue #246 LAB
/// track, sub-issue #263): the engine-side rule-surface report — per-rule feature
/// usage, pattern/metavariable counts, compiled-delegate count and compile time,
/// plus JSON serialization — over a synthetic all-features pack and over the
/// shipped rules/csharp10 pack compiled against the downloaded CSharp10 grammar.
/// </summary>
public sealed class LabyrinthRuleSurfaceReportTests
{
    private const string TestGrammar = """
        Grammar: TestLang
        FormatType: EBNF

        <WS> ::= /[ \\t\\r\\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <EQUALS> ::= "="

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | EQUALS | STRING ;
        """;

    private static LabyrinthRuleSet SyntheticAllFeaturesPack() => new()
    {
        Rules =
        {
            new LabyrinthRule
            {
                Id = "search-all-features",
                Type = "search",
                Severity = "WARNING",
                Pattern = "Do($X)",
                Condition = "node.Arguments.Count == 1",
                PatternNot = "Do(safe)",
                PatternInside = "Try(...)",
                FocusMetavariable = "$X",
                Fix = "Guard($X)",
                Paths = new LabyrinthPathFilter { Include = new List<string> { "src/**" } },
                Metadata = new Dictionary<string, object?> { ["cwe"] = "CWE-476" },
            },
            new LabyrinthRule
            {
                Id = "taint-all-features",
                Type = "taint",
                Severity = "ERROR",
                Sources = new List<LabyrinthPatternEntry>
                {
                    new()
                    {
                        PatternEither = new List<LabyrinthPatternEntry>
                        {
                            new() { Pattern = "ReceiveData($DATA)", PatternNot = "ReceiveData(safe)", Condition = "node.Arguments.Count == 1" },
                            new() { Pattern = "ReadFile($DATA)" },
                        },
                    },
                },
                Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "ExecuteAction($DATA)" } },
                Sanitizers = new List<LabyrinthPatternEntry> { new() { Pattern = "Sanitize($DATA)" } },
                Propagators = new List<LabyrinthPropagatorEntry>
                {
                    new() { Pattern = "$TARGET = Format($DATA)", From = "$DATA", To = "$TARGET", Condition = "node.Arguments.Count == 2" },
                },
                FocusMetavariable = "$DATA",
            },
        },
    };

    [Fact]
    public void SyntheticPack_ReportsEveryFeatureAndCompilesDelegates()
    {
        var report = LabyrinthRuleSurfaceReport.Build(SyntheticAllFeaturesPack(), TestGrammar, "TestLang");

        Assert.Equal(2, report.RuleCount);
        Assert.Equal(2, report.Rules.Count);

        var search = report.Rules.Single(r => r.RuleId == "search-all-features");
        Assert.Equal(new[]
        {
            "condition", "fix", "focus-metavariable", "metadata", "paths",
            "pattern-inside", "pattern-not", "search-pattern",
        }, search.Features);
        Assert.Equal(1, search.Entries);
        Assert.Equal(1, search.Patterns);
        Assert.True(search.CompiledDelegates > 0);
        Assert.True(search.Metavariables >= 1);

        var taint = report.Rules.Single(r => r.RuleId == "taint-all-features");
        Assert.Contains("taint", taint.Features);
        Assert.Contains("sources", taint.Features);
        Assert.Contains("sinks", taint.Features);
        Assert.Contains("sanitizers", taint.Features);
        Assert.Contains("propagators", taint.Features);
        Assert.Contains("pattern-either", taint.Features);
        Assert.Contains("pattern-not", taint.Features);
        Assert.Contains("condition", taint.Features);
        Assert.Contains("focus-metavariable", taint.Features);
        Assert.Equal(4, taint.Patterns); // 2 source alternatives + 1 sink + 1 sanitizer
        Assert.Equal(3, taint.Entries); // sources + sinks + sanitizers lists
        Assert.True(taint.CompiledDelegates > 0);

        // Pack aggregates.
        Assert.Equal(report.Rules.Sum(r => r.Patterns), report.TotalPatterns);
        Assert.Equal(report.Rules.Sum(r => r.CompiledDelegates), report.TotalCompiledDelegates);
        Assert.Contains("pattern-either", report.FeaturesUsed);
        Assert.Contains("fix", report.FeaturesUsed);
        Assert.True(report.TotalCompileMs >= 0);

        // JSON round-trip for committed artifacts.
        var json = LabyrinthRuleSurfaceReport.ToJson(report);
        Assert.Contains("\"search-all-features\"", json);
        Assert.Contains("\"taint-all-features\"", json);
        Assert.Contains("\"pattern-either\"", json);
        var parsed = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(2, parsed.RootElement.GetProperty("ruleCount").GetInt32());
    }

    [Fact]
    public void ShippedCsharp10Pack_CompilesAgainstTargetGrammar()
    {
        var repoRoot = FindRepoRoot();
        var packDir = Path.Join(repoRoot, "rules", "csharp10");
        if (!Directory.Exists(packDir))
        {
            return; // xunit has no runtime skip; the pack ships with the repo, so CI always runs the assertions below
        }

        var pack = new LabyrinthRulePackLoader().TryLoadDirectory(packDir);
        Assert.True(pack.IsValid, string.Join("; ", pack.Errors));

        var grammarsRoot = ScanHost.FindGrammarsRoot();
        var target = ScanHost.ResolveTarget("csharp10");
        var grammarFile = ScanHost.GrammarFilePath(grammarsRoot, target);
        if (!File.Exists(grammarFile))
        {
            return; // xunit has no runtime skip; the grammar is downloaded at build time, so CI always has it
        }

        var grammarContent = File.ReadAllText(grammarFile);
        var report = LabyrinthRuleSurfaceReport.Build(pack.RuleSet, grammarContent, target.Directory);

        Assert.True(report.RuleCount >= 3, $"expected the shipped csharp10 rules, got {report.RuleCount}");
        Assert.All(report.Rules, r => Assert.True(r.CompiledDelegates > 0, $"{r.RuleId} compiled no delegates"));
        Assert.Contains(report.Rules, r => r.Features.Contains("taint")); // null-deref / as-cast rules
        Assert.Contains(report.Rules, r => r.Features.Contains("search-pattern")); // constant-zero-division rule
        Assert.Contains("sanitizers", report.FeaturesUsed);
        Assert.True(report.TotalPatterns > 0);

        var json = LabyrinthRuleSurfaceReport.ToJson(report);
        Assert.Contains("csharp-null-deref-unguarded-maybe-null", json);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Join(dir.FullName, "rules", "csharp10")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
