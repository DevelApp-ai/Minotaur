using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Layer 4 tests for the Labyrinth rule engine (Minotaur issue #104):
/// string-based C# conditions on patterns parsed via System.Linq.Dynamic.Core
/// and appended to the compiled pattern delegate, per
/// docs/SAST Rule Language Evaluation.md §5.4.
/// </summary>
public sealed class LabyrinthDynamicLinqConditionTests
{
    private readonly LabyrinthPatternParser _parser;
    private readonly LabyrinthExpressionCompiler _compiler = new();

    private const string TestGrammar = """
        Grammar: TestLang
        TokenSplitter: Space
        FormatType: EBNF

        <WS> ::= /[ \t\r\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <EQUALS> ::= "="

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | EQUALS | STRING ;
        """;

    public LabyrinthDynamicLinqConditionTests()
    {
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static ILabyrinthMatchNode Ident(string name) => new LabyrinthMatchNode("Identifier", name);

    private static ILabyrinthMatchNode Assignment(ILabyrinthMatchNode target, ILabyrinthMatchNode value) =>
        new LabyrinthMatchNode("Assignment", null, new[] { target, value });

    private LabyrinthRule SearchRule(string pattern, string condition) => new()
    {
        Id = "advanced-size-check",
        Type = "search",
        Pattern = pattern,
        Condition = condition,
    };

    private LabyrinthRule TaintRule(
        string[] sources,
        string[] sinks,
        string[]? sourceConditions = null,
        string[]? sinkConditions = null,
        string[]? sanitizers = null,
        string[]? sanitizerConditions = null,
        (string Pattern, string From, string To, string? Condition)[]? propagators = null)
    {
        var rule = new LabyrinthRule
        {
            Id = "injection",
            Severity = "ERROR",
            Type = "taint",
            Sources = sources.Select((p, i) => new LabyrinthPatternEntry
            {
                Pattern = p,
                Condition = sourceConditions?[i],
            }).ToList(),
            Sinks = sinks.Select((p, i) => new LabyrinthPatternEntry
            {
                Pattern = p,
                Condition = sinkConditions?[i],
            }).ToList(),
            Sanitizers = sanitizers?.Select((p, i) => new LabyrinthPatternEntry
            {
                Pattern = p,
                Condition = sanitizerConditions?[i],
            }).ToList(),
            Propagators = propagators?
                .Select(p => new LabyrinthPropagatorEntry { Pattern = p.Pattern, From = p.From, To = p.To, Condition = p.Condition })
                .ToList(),
        };
        return rule;
    }

    private LabyrinthCompiledMatchers Compile(LabyrinthRule rule)
    {
        var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
        return _compiler.CompileRule(compiled);
    }

    // ------------------------------------------------------------------
    // Search rules with conditions
    // ------------------------------------------------------------------

    [Fact]
    public void SearchRule_ConditionTrue_Matches()
    {
        var matchers = Compile(SearchRule("AllocateMemory($SIZE)", "node.Arguments.Count == 1"));

        // Pattern matches (name + arity) and condition holds.
        var node = Call("AllocateMemory", Ident("size"));
        Assert.True(matchers.SearchPattern!(node, new LabyrinthMatchContext()));
    }

    [Fact]
    public void SearchRule_ConditionFalse_DoesNotMatch()
    {
        var matchers = Compile(SearchRule("AllocateMemory($SIZE)", "node.Arguments.Count > 1"));

        // Pattern matches, but the condition fails: no finding.
        var node = Call("AllocateMemory", Ident("size"));
        Assert.False(matchers.SearchPattern!(node, new LabyrinthMatchContext()));
    }

    [Fact]
    public void SearchRule_ConditionCanInspectBoundNodes()
    {
        // The condition inspects the matched node's argument names.
        var matchers = Compile(SearchRule("AllocateMemory($SIZE)", "node.Arguments[0].Name == \"big\""));

        Assert.True(matchers.SearchPattern!(Call("AllocateMemory", Ident("big")), new LabyrinthMatchContext()));
        Assert.False(matchers.SearchPattern!(Call("AllocateMemory", Ident("small")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void SearchRule_ConditionCanUseLogicalOperators()
    {
        var matchers = Compile(SearchRule(
            "AllocateMemory($SIZE)",
            "node.Arguments.Count == 1 && node.Arguments[0].Name == \"big\" && node.NodeType == \"FunctionCall\""));

        Assert.True(matchers.SearchPattern!(Call("AllocateMemory", Ident("big")), new LabyrinthMatchContext()));
        Assert.False(matchers.SearchPattern!(Call("AllocateMemory", Ident("big"), Ident("x")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void SearchRule_UnificationStillHoldsWithCondition()
    {
        var matchers = Compile(SearchRule("AllocateMemory($SIZE)", "node.Arguments.Count == 1"));

        var context = new LabyrinthMatchContext();
        var argument = Ident("size");
        Assert.True(matchers.SearchPattern!(Call("AllocateMemory", argument), context));
        Assert.True(context.TryGet("SIZE", out var bound));
        Assert.Same(argument, bound);
    }

    // ------------------------------------------------------------------
    // Invalid conditions fail fast
    // ------------------------------------------------------------------

    [Fact]
    public void MalformedCondition_FailsFastAtCompilation()
    {
        var rule = SearchRule("AllocateMemory($SIZE)", "node.Arguments[0. &&&");

        var ex = Assert.Throws<LabyrinthRuleException>(() =>
        {
            var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
            _compiler.CompileRule(compiled);
        });
        Assert.Contains("Invalid Dynamic LINQ condition", ex.Message);
    }

    [Fact]
    public void ConditionReferencingUnknownMember_FailsFast()
    {
        var rule = SearchRule("AllocateMemory($SIZE)", "node.NoSuchProperty > 1024");

        Assert.Throws<LabyrinthRuleException>(() =>
        {
            var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
            _compiler.CompileRule(compiled);
        });
    }

    // ------------------------------------------------------------------
    // Taint rules with per-entry conditions
    // ------------------------------------------------------------------

    [Fact]
    public void SourceCondition_FiltersSourceIdentification()
    {
        // Only sources whose argument is named "big" seed taint.
        var rule = TaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sourceConditions: new[] { "node.Arguments[0].Name == \"big\"" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var matchers = Compile(rule);

        var big = Ident("big");
        var small = Ident("small");
        var bigSource = Call("ReceiveData", big);
        var smallSource = Call("ReceiveData", small);
        var sink = Call("ExecuteAction", Ident("prefix"), big);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(bigSource, sink)
            .AddNode(smallSource, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        // Only the source passing its condition produced a finding.
        var finding = Assert.Single(findings);
        Assert.Same(bigSource, finding.Source);
    }

    [Fact]
    public void SinkCondition_FiltersSinkConfirmation()
    {
        var rule = TaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" },
            sinkConditions: new[] { "node.Arguments[0].Name == \"dangerous\"" });

        var matchers = Compile(rule);

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var safeSink = Call("ExecuteAction", Ident("safe"), argument);
        var dangerousSink = Call("ExecuteAction", Ident("dangerous"), argument);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(safeSink)
            .AddNode(dangerousSink);
        graph.Connect(source, safeSink);
        graph.Connect(source, dangerousSink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        // Taint reached both sinks, but only the one passing its condition is a finding.
        var finding = Assert.Single(findings);
        Assert.Same(dangerousSink, finding.Sink);
    }

    [Fact]
    public void SanitizerCondition_SanitizesOnlyWhenConditionHolds()
    {
        // A sanitizer that only applies when its argument is named "raw".
        var rule = TaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" },
            sanitizers: new[] { "VerifyIntegrity($DATA)" },
            sanitizerConditions: new[] { "node.Arguments[0].Name == \"raw\"" });

        var matchers = Compile(rule);

        var raw = Ident("raw");
        var cooked = Ident("cooked");
        var rawSource = Call("ReceiveData", raw);
        var cookedSource = Call("ReceiveData", cooked);
        var rawSanitizer = Call("VerifyIntegrity", raw);
        var cookedSanitizer = Call("VerifyIntegrity", cooked);
        var rawSink = Call("ExecuteAction", Ident("prefix"), raw);
        var cookedSink = Call("ExecuteAction", Ident("prefix"), cooked);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(rawSource, rawSanitizer)
            .AddNode(rawSanitizer, rawSink)
            .AddNode(cookedSource, cookedSanitizer)
            .AddNode(cookedSanitizer, cookedSink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        // The raw path was sanitized (finding pruned); the cooked path was not
        // (the sanitizer's condition excludes it), so exactly one finding.
        var finding = Assert.Single(findings);
        Assert.Same(cookedSink, finding.Sink);
    }

    [Fact]
    public void PropagatorCondition_GatesTaintTransfer()
    {
        // The propagator only transfers when the assignment's RHS call has
        // exactly one argument.
        var rule = TaintRule(
            sources: new[] { "ReceiveData($SRC)" },
            sinks: new[] { "ExecuteAction($PREFIX, $TARGET)" },
            propagators: new[]
            {
                ("$TARGET = FormatString($SRC)", "SRC", "TARGET", "node.Arguments[1].Name == \"FormatString\""),
            });

        var matchers = Compile(rule);

        var src = Ident("raw");
        var source = Call("ReceiveData", src);
        var target = Ident("formatted");
        var assignment = Assignment(target, Call("FormatString", src));
        var sink = Call("ExecuteAction", Ident("prefix"), target);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, src)
            .AddNode(src, assignment);
        graph.Connect(target, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Same(sink, finding.Sink);
    }

    [Fact]
    public void PropagatorCondition_False_NoTransferNoFinding()
    {
        // Same graph, but the propagator's condition is never satisfiable.
        var rule = TaintRule(
            sources: new[] { "ReceiveData($SRC)" },
            sinks: new[] { "ExecuteAction($PREFIX, $TARGET)" },
            propagators: new[]
            {
                ("$TARGET = FormatString($SRC)", "SRC", "TARGET", "node.NodeType == \"NoSuchType\""),
            });

        var matchers = Compile(rule);

        var src = Ident("raw");
        var source = Call("ReceiveData", src);
        var target = Ident("formatted");
        var assignment = Assignment(target, Call("FormatString", src));
        var sink = Call("ExecuteAction", Ident("prefix"), target);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, src)
            .AddNode(src, assignment);
        graph.Connect(target, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        Assert.Empty(findings);
    }

    [Fact]
    public void TaintEntry_MalformedCondition_FailsFast()
    {
        var rule = TaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sourceConditions: new[] { "node.Arguments[0. &&&" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        Assert.Throws<LabyrinthRuleException>(() => Compile(rule));
    }

    // ------------------------------------------------------------------
    // YAML schema: condition on propagator entries
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_ParsesPropagatorCondition()
    {
        const string yaml = """
            rules:
              - id: conditioned-propagator
                severity: ERROR
                type: taint
                sources:
                  - pattern: "ReceiveData($SRC)"
                sinks:
                  - pattern: "ExecuteAction($PREFIX, $TARGET)"
                propagators:
                  - pattern: "$TARGET = FormatString($SRC)"
                    from: $SRC
                    to: $TARGET
                    condition: "node.Arguments[1].Name == \"FormatString\""
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        var rule = Assert.Single(ruleSet.Rules);
        var propagator = Assert.Single(rule.Propagators!);
        Assert.Equal("$SRC", propagator.From);
        Assert.Equal("$TARGET", propagator.To);
        Assert.Equal("node.Arguments[1].Name == \"FormatString\"", propagator.Condition);
    }
}
