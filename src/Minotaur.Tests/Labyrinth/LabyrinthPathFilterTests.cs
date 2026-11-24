using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for rule-level path filters (YAML <c>paths:</c> include/exclude globs,
/// Labyrinth issue #246, LAB track): glob semantics, loader validation, and
/// end-to-end filtering through <see cref="ScanHost.RunRules"/>.
/// </summary>
public sealed class LabyrinthPathFilterTests
{
    // Pattern-v2 grammar used for the ScanHost end-to-end test.
    private const string TestGrammarV2 = """
        Grammar: TestLangV2
        FormatType: EBNF

        <WS> ::= /[ \\t\\r\\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <SEMICOLON> ::= ";"
        <EQUALS> ::= "="
        <DOT> ::= "."

        <program> ::= <statement> ;
        <statement> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | SEMICOLON | EQUALS | DOT ;
        """;

    // ------------------------------------------------------------------
    // Glob semantics
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("**/*.cs", "src/deep/file.cs", true)]
    [InlineData("**/*.cs", "file.cs", true)]              // **/ also matches zero segments
    [InlineData("**/*.cs", "src/deep/file.py", false)]
    [InlineData("*.cs", "file.cs", true)]
    [InlineData("*.cs", "src/file.cs", false)]             // * stays within a segment
    [InlineData("tests/**", "tests/a/b/c.cs", true)]
    [InlineData("tests/**", "src/tests/a.cs", false)]      // root-anchored
    [InlineData("/tests/**", "tests/a.cs", true)]          // leading / dropped
    [InlineData("src/*.cs", "src/a.cs", true)]
    [InlineData("src/*.cs", "src/sub/a.cs", false)]
    [InlineData("**/vuln?.cs", "app/vuln1.cs", true)]
    [InlineData("**/vuln?.cs", "app/vuln12.cs", false)]
    [InlineData("src/**", "src", false)]                   // ** needs the trailing part matched... src/** -> src/.* requires slash
    [InlineData("**", "anything/at/all.py", true)]
    public void GlobMatches_Cases(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, LabyrinthPathFilter.GlobMatches(pattern, path.Replace('\\', '/')));
    }

    [Fact]
    public void Matches_ExcludeWinsOverInclude_AndEmptyIncludeMeansAll()
    {
        var filter = new LabyrinthPathFilter
        {
            Include = new List<string> { "**/*.cs" },
            Exclude = new List<string> { "tests/fixtures/**" },
        };

        Assert.True(filter.Matches("src/app.cs"));
        Assert.False(filter.Matches("src/app.py"));
        Assert.False(filter.Matches("tests/fixtures/gen.cs")); // excluded despite include
        Assert.True(filter.Matches("tests/other.cs"));

        var excludeOnly = new LabyrinthPathFilter { Exclude = new List<string> { "**/*.generated.cs" } };
        Assert.True(excludeOnly.Matches("src/app.cs"));
        Assert.False(excludeOnly.Matches("src/app.generated.cs"));

        Assert.True(new LabyrinthPathFilter().Matches("any/path.cs")); // empty filter = all
        Assert.True(new LabyrinthPathFilter().Matches(null));

        // Null path with include globs: rule does not apply.
        Assert.False(filter.Matches(null));
        Assert.True(excludeOnly.Matches(null));

        // Windows-style separators are normalized.
        Assert.True(filter.Matches("src\\app.cs"));
    }

    // ------------------------------------------------------------------
    // YAML loader
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_ParsesPathsFilter()
    {
        const string yaml = """
            rules:
              - id: path-limited
                type: search
                pattern: "Do($X)"
                paths:
                  include:
                    - "tests/**"
                  exclude:
                    - "tests/fixtures/**"
              - id: unrestricted
                type: search
                pattern: "Do($Y)"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        Assert.Equal(2, ruleSet.Rules.Count);
        var limited = ruleSet.Rules[0];
        Assert.NotNull(limited.Paths);
        Assert.Equal(new[] { "tests/**" }, limited.Paths!.Include);
        Assert.Equal(new[] { "tests/fixtures/**" }, limited.Paths.Exclude);
        Assert.Null(ruleSet.Rules[1].Paths);

        Assert.Single(ruleSet.RulesForPath("tests/unit/a.cs"));
        Assert.Equal(2, ruleSet.RulesForPath("src/app.cs").Count());
        Assert.Single(ruleSet.RulesForPath(null)); // only the unrestricted rule
    }

    [Fact]
    public void YamlLoader_RejectsEmptyPathGlob()
    {
        const string yaml = """
            rules:
              - id: bad-paths
                type: search
                pattern: "Do($X)"
                paths:
                  include:
                    - ""
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("paths.include[0]", ex.Message);
    }

    // ------------------------------------------------------------------
    // End-to-end through ScanHost.RunRules
    // ------------------------------------------------------------------

    [Fact]
    public void ScanHost_RunRules_AppliesPathFilters()
    {
        const string yaml = """
            rules:
              - id: vuln-only
                severity: ERROR
                type: search
                pattern: "$X.DoWork()"
                paths:
                  include:
                    - "**/vuln*.cs"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        // Matching path: the rule fires.
        var graphVuln = ScanHost.BuildGraph("svc.DoWork();", "src/vuln1.cs", TestGrammarV2, "TestLangV2", out var nodesVuln);
        var findingsVuln = ScanHost.RunRules(ruleSet, TestGrammarV2, "TestLangV2", graphVuln, nodesVuln, "src/vuln1.cs");
        var finding = Assert.Single(findingsVuln);
        Assert.Equal("vuln-only", finding.RuleId);

        // Non-matching path: filtered out, no findings.
        var graphSafe = ScanHost.BuildGraph("svc.DoWork();", "src/safe.cs", TestGrammarV2, "TestLangV2", out var nodesSafe);
        Assert.Empty(ScanHost.RunRules(ruleSet, TestGrammarV2, "TestLangV2", graphSafe, nodesSafe, "src/safe.cs"));

        // No path passed (legacy callers): include-filtered rules do not apply.
        Assert.Empty(ScanHost.RunRules(ruleSet, TestGrammarV2, "TestLangV2", graphSafe, nodesSafe));
    }
}
