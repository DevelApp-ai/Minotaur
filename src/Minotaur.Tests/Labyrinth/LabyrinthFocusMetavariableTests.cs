using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for <c>focus-metavariable</c> (Labyrinth issue #246, LAB track): when a
/// rule declares <c>focus-metavariable: $X</c>, the finding is reported at the
/// node bound to <c>$X</c> instead of the whole matched node. Search rules must
/// declare it in <c>pattern:</c>; taint rules in a sink pattern (the sink is
/// focused). Unbound focus falls back to the matched node.
/// </summary>
public sealed class LabyrinthFocusMetavariableTests
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

    // Pattern-v2 grammar used for the ScanHost end-to-end test (member calls).
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
        <SLASH> ::= "/"
        <PERCENT> ::= "%"
        <LBRACKET> ::= "["
        <RBRACKET> ::= "]"
        <COALESCE> ::= "??"
        <QMARK> ::= "?"

        <program> ::= <statement> ;
        <statement> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | SEMICOLON | EQUALS | DOT | SLASH | PERCENT | LBRACKET | RBRACKET | COALESCE | QMARK ;
        """;

    public LabyrinthFocusMetavariableTests()
    {
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static ILabyrinthMatchNode Ident(string name) => new LabyrinthMatchNode("Identifier", name);

    // ------------------------------------------------------------------
    // YAML loader: focus-metavariable validation
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_SearchFocusMustOccurInPattern()
    {
        const string yaml = """
            rules:
              - id: bad-focus
                type: search
                pattern: "Do($X)"
                focus-metavariable: $Y
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("focus-metavariable", ex.Message);
        Assert.Contains("does not occur in the search pattern", ex.Message);
    }

    [Fact]
    public void YamlLoader_TaintFocusMustOccurInSinkPattern()
    {
        const string yaml = """
            rules:
              - id: bad-taint-focus
                severity: ERROR
                type: taint
                sources:
                  - pattern: "ReceiveData($SRC)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
                focus-metavariable: $SRC
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("does not occur in any sink pattern", ex.Message);
    }

    [Fact]
    public void YamlLoader_ValidFocusLoads()
    {
        const string yaml = """
            rules:
              - id: search-focus
                type: search
                pattern: "Do($X)"
                focus-metavariable: $X
              - id: taint-focus
                severity: ERROR
                type: taint
                sources:
                  - pattern: "ReceiveData($DATA)"
                sinks:
                  - pattern: "ExecuteAction($PREFIX, $DATA)"
                focus-metavariable: $PREFIX
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        Assert.Equal(2, ruleSet.Rules.Count);
        Assert.Equal("$X", ruleSet.Rules[0].FocusMetavariable);
        Assert.Equal("$PREFIX", ruleSet.Rules[1].FocusMetavariable);
    }

    // ------------------------------------------------------------------
    // Compiler / matcher level
    // ------------------------------------------------------------------

    [Fact]
    public void Compiler_NormalizesFocus_AndResolveReportNodeBinds()
    {
        var rule = new LabyrinthRule
        {
            Id = "focus-search",
            Type = "search",
            Pattern = "Do($X)",
            FocusMetavariable = "$X",
        };
        var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
        Assert.Equal("X", compiled.FocusMetavariable); // normalized: no leading $

        var matchers = _compiler.CompileRule(compiled);
        Assert.Equal("X", matchers.FocusMetavariable);

        var arg = Ident("payload");
        var node = Call("Do", arg);
        var ctx = new LabyrinthMatchContext();
        Assert.True(matchers.SearchPattern!(node, ctx));

        // Focus bound: the bound node is the report node.
        Assert.Same(arg, matchers.ResolveReportNode(node, ctx));
        // Focus unbound: fall back to the matched node.
        Assert.Same(node, matchers.ResolveReportNode(node, new LabyrinthMatchContext()));
    }

    [Fact]
    public void Compiler_NoFocus_ResolveReportNodeReturnsMatchedNode()
    {
        var rule = new LabyrinthRule { Id = "plain", Type = "search", Pattern = "Do($X)" };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));

        Assert.Null(matchers.FocusMetavariable);
        var node = Call("Do", Ident("a"));
        var ctx = new LabyrinthMatchContext();
        Assert.True(matchers.SearchPattern!(node, ctx));
        Assert.Same(node, matchers.ResolveReportNode(node, ctx));
    }

    // ------------------------------------------------------------------
    // Search rule end-to-end through the ScanHost
    // ------------------------------------------------------------------

    [Fact]
    public void ScanHost_SearchFocus_ReportsFindingAtBoundReceiver()
    {
        const string yaml = """
            rules:
              - id: deref-focus
                severity: ERROR
                type: search
                pattern: "$X.DoWork()"
                focus-metavariable: $X
              - id: deref-nofocus
                severity: WARNING
                type: search
                pattern: "$X.DoWork()"
            """;

        var graph = ScanHost.BuildGraph("svc.DoWork();", "test.cs", TestGrammarV2, "TestLangV2", out var nodes);
        var ruleSet = new LabyrinthRuleLoader().Load(yaml);
        var findings = ScanHost.RunRules(ruleSet, TestGrammarV2, "TestLangV2", graph, nodes);

        Assert.Equal(2, findings.Count);

        var focused = findings.Single(f => f.RuleId == "deref-focus");
        // Reported at the bound receiver identifier, not at the whole call.
        Assert.Equal("Identifier", focused.Sink.NodeType);
        Assert.Equal("svc", focused.Sink.Name);

        var plain = findings.Single(f => f.RuleId == "deref-nofocus");
        Assert.Equal("FunctionCall", plain.Sink.NodeType);
        Assert.Equal("DoWork", plain.Sink.Name);
    }

    // ------------------------------------------------------------------
    // Taint engine: focus applies to the sink
    // ------------------------------------------------------------------

    [Fact]
    public void TaintEngine_Focus_ReportsSinkAtBoundNode()
    {
        var rule = new LabyrinthRule
        {
            Id = "focused-taint",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry> { new() { Pattern = "ReceiveData($DATA)" } },
            Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "ExecuteAction($PREFIX, $DATA)" } },
            FocusMetavariable = "$PREFIX",
        };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));

        var data = Ident("userInput");
        var prefix = Ident("prefix");
        var source = Call("ReceiveData", data);
        var sink = Call("ExecuteAction", prefix, data);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(sink);
        graph.Connect(source, sink);

        var finding = Assert.Single(new LabyrinthTaintEngine(matchers, graph).Analyze());

        Assert.Same(source, finding.Source);
        // Focused at $PREFIX (the first argument), not at the ExecuteAction call.
        Assert.Same(prefix, finding.Sink);
        Assert.Equal("Identifier", finding.Sink.NodeType);
    }
}
