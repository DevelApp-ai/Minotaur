using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for rule fix suggestions (YAML <c>fix:</c>, Labyrinth issue #246, LAB
/// track): the fix text's <c>$NAME</c> references are interpolated against the
/// finding's metavariable bindings; the fix rides on the finding
/// (<see cref="LabyrinthTaintFinding.Fix"/>). A fix referencing a metavariable
/// that occurs in no pattern of the rule is a load-time error.
/// </summary>
public sealed class LabyrinthFixSuggestionTests
{
    private readonly LabyrinthPatternParser _parser;
    private readonly LabyrinthExpressionCompiler _compiler = new();

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

    public LabyrinthFixSuggestionTests()
    {
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static ILabyrinthMatchNode Ident(string name) => new LabyrinthMatchNode("Identifier", name);

    // ------------------------------------------------------------------
    // Loader validation
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_RejectsFixReferencingUnknownMetavariable()
    {
        const string yaml = """
            rules:
              - id: bad-fix
                type: search
                pattern: "Do($X)"
                fix: "Safe($Y)"
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("'fix' references '$Y'", ex.Message);
    }

    [Fact]
    public void YamlLoader_ParsesValidFix()
    {
        const string yaml = """
            rules:
              - id: with-fix
                type: search
                pattern: "Do($X)"
                fix: "Guard($X); Do($X)"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);
        Assert.Equal("Guard($X); Do($X)", Assert.Single(ruleSet.Rules).Fix);
    }

    // ------------------------------------------------------------------
    // Interpolator
    // ------------------------------------------------------------------

    [Fact]
    public void Interpolate_ReplacesBoundMetavars_LeavesUnboundAsIs()
    {
        var ctx = new LabyrinthMatchContext();
        ctx.TryBind("X", Ident("svc"));
        ctx.TryBind("DATA", Ident("userInput"));

        Assert.Equal("Guard(svc); Do(svc)", LabyrinthFixInterpolator.Interpolate("Guard($X); Do($X)", ctx));
        Assert.Equal("Validate(userInput)", LabyrinthFixInterpolator.Interpolate("Validate($DATA)", ctx));
        Assert.Equal("Safe($Y)", LabyrinthFixInterpolator.Interpolate("Safe($Y)", ctx)); // unbound: untouched
        Assert.Equal("no metavars", LabyrinthFixInterpolator.Interpolate("no metavars", ctx));

        Assert.Equal(new[] { "X" }, LabyrinthFixInterpolator.MetavariablesIn("Guard($X)"));
        Assert.Equal(new[] { "X", "DATA" }, LabyrinthFixInterpolator.MetavariablesIn("$X = $DATA + $X"));
        Assert.Empty(LabyrinthFixInterpolator.MetavariablesIn(null));
    }

    // ------------------------------------------------------------------
    // Search rule end-to-end through ScanHost
    // ------------------------------------------------------------------

    [Fact]
    public void ScanHost_SearchRule_FixIsInterpolatedOnFinding()
    {
        const string yaml = """
            rules:
              - id: deref-fix
                severity: ERROR
                type: search
                pattern: "$X.DoWork()"
                fix: "$X?.DoWork()"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);
        var graph = ScanHost.BuildGraph("svc.DoWork();", "test.cs", TestGrammarV2, "TestLangV2", out var nodes);
        var findings = ScanHost.RunRules(ruleSet, TestGrammarV2, "TestLangV2", graph, nodes, "test.cs");

        var finding = Assert.Single(findings);
        Assert.Equal("svc?.DoWork()", finding.Fix);
    }

    // ------------------------------------------------------------------
    // Taint engine: fix interpolated against the sink bindings
    // ------------------------------------------------------------------

    [Fact]
    public void TaintEngine_FixIsInterpolatedOnFinding()
    {
        var rule = new LabyrinthRule
        {
            Id = "taint-fix",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry> { new() { Pattern = "ReceiveData($DATA)" } },
            Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "ExecuteAction($DATA)" } },
            Fix = "Sanitize($DATA)",
        };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));

        var data = Ident("userInput");
        var source = Call("ReceiveData", data);
        var sink = Call("ExecuteAction", data);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(sink);
        graph.Connect(source, sink);

        var finding = Assert.Single(new LabyrinthTaintEngine(matchers, graph).Analyze());
        Assert.Equal("Sanitize(userInput)", finding.Fix);
    }

    [Fact]
    public void TaintEngine_NoFix_FindingFixIsNull()
    {
        var rule = new LabyrinthRule
        {
            Id = "taint-nofix",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry> { new() { Pattern = "ReceiveData($DATA)" } },
            Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "ExecuteAction($DATA)" } },
        };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));

        var data = Ident("userInput");
        var source = Call("ReceiveData", data);
        var sink = Call("ExecuteAction", data);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(sink);
        graph.Connect(source, sink);

        Assert.Null(Assert.Single(new LabyrinthTaintEngine(matchers, graph).Analyze()).Fix);
    }
}
