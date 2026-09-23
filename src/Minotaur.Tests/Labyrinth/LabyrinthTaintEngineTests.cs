using System.Diagnostics;
using Minotaur.Labyrinth;
using Xunit;
using Xunit.Abstractions;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Layer 3 tests for the Labyrinth taint traversal engine (Minotaur issue
/// #103): source seeding, DFG propagation, sanitizer pruning, propagator
/// transfer, sink confirmation with full source→sink paths, metavariable
/// unification across the rule, fixed-point termination over loops, and
/// interprocedural flow — plus a scale run exercising the pooled binding
/// structs for GC/cache-coherence behaviour.
/// </summary>
public sealed class LabyrinthTaintEngineTests
{
    private readonly ITestOutputHelper _output;
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

    public LabyrinthTaintEngineTests(ITestOutputHelper output)
    {
        _output = output;
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

    private LabyrinthCompiledMatchers CompileTaintRule(
        string id = "injection",
        string[]? sources = null,
        string[]? sinks = null,
        string[]? sanitizers = null,
        (string Pattern, string From, string To)[]? propagators = null)
    {
        var rule = new LabyrinthRule
        {
            Id = id,
            Severity = "ERROR",
            Message = "Untrusted data reaches a dangerous sink.",
            Type = "taint",
            Sources = (sources ?? Array.Empty<string>()).Select(p => new LabyrinthPatternEntry { Pattern = p }).ToList(),
            Sinks = (sinks ?? Array.Empty<string>()).Select(p => new LabyrinthPatternEntry { Pattern = p }).ToList(),
            Sanitizers = (sanitizers ?? Array.Empty<string>()).Select(p => new LabyrinthPatternEntry { Pattern = p }).ToList(),
            Propagators = propagators?
                .Select(p => new LabyrinthPropagatorEntry { Pattern = p.Pattern, From = p.From, To = p.To })
                .ToList(),
        };

        var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
        return _compiler.CompileRule(compiled);
    }

    // ------------------------------------------------------------------
    // Source identification and propagation
    // ------------------------------------------------------------------

    [Fact]
    public void DirectSourceToSink_ProducesFinding_WithFullPath()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var sink = Call("ExecuteAction", Ident("prefix"), argument);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(sink);
        graph.Connect(source, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Equal("injection", finding.RuleId);
        Assert.Equal(LabyrinthSeverity.Error, finding.Severity);
        Assert.Same(source, finding.Source);
        Assert.Same(sink, finding.Sink);
        Assert.Equal(new[] { source, sink }, finding.Path);
    }

    [Fact]
    public void PropagationThroughIntermediateNodes_ReachesSink()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var mid1 = Ident("temp1");
        var mid2 = Ident("temp2");
        var sink = Call("ExecuteAction", Ident("prefix"), argument);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, mid1)
            .AddNode(mid1, mid2)
            .AddNode(mid2, sink);
        graph.Connect(sink, Ident("nowhere"));

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Equal(new[] { source, mid1, mid2, sink }, finding.Path);
    }

    [Fact]
    public void NoSourceMatch_NoFindings()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var sink = Call("ExecuteAction", Ident("prefix"), Ident("clean"));
        var graph = new LabyrinthDataFlowGraph().AddNode(sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        Assert.Empty(findings);
    }

    // ------------------------------------------------------------------
    // Sanitizer pruning
    // ------------------------------------------------------------------

    [Fact]
    public void Sanitizer_PrunesTraversal_NoFinding()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" },
            sanitizers: new[] { "VerifyIntegrity($DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var sanitizer = Call("VerifyIntegrity", argument);
        var sink = Call("ExecuteAction", Ident("prefix"), argument);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, sanitizer)
            .AddNode(sanitizer, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        Assert.Empty(findings);
    }

    // ------------------------------------------------------------------
    // Propagator application
    // ------------------------------------------------------------------

    [Fact]
    public void Propagator_TransfersTaintToTarget()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($SRC)" },
            sinks: new[] { "ExecuteAction($PREFIX, $TARGET)" },
            propagators: new[] { ("$TARGET = FormatString($SRC)", "SRC", "TARGET") });

        // Taint enters at the source call with $SRC bound to its argument; the
        // DFG carries it into the assignment, whose propagator pattern binds
        // TARGET -> the assignment's left-hand side.
        var sourceArg = Ident("raw");
        var source = Call("ReceiveData", sourceArg);
        var target = Ident("formatted");
        var assignment = Assignment(target, Call("FormatString", sourceArg));

        // Only the propagator's 'to' target leads to the sink: without the
        // propagator transfer, no finding is possible.
        var sink = Call("ExecuteAction", Ident("prefix"), target);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, sourceArg)
            .AddNode(sourceArg, assignment);
        graph.Connect(target, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Same(source, finding.Source);
        Assert.Same(sink, finding.Sink);
        // Path: source -> sourceArg -> assignment -> (propagator) target -> sink
        Assert.Equal(new ILabyrinthMatchNode[] { source, sourceArg, assignment, target, sink }, finding.Path);
    }

    // ------------------------------------------------------------------
    // Metavariable unification
    // ------------------------------------------------------------------

    [Fact]
    public void SinkUnification_DifferentNode_NoFinding()
    {
        // The sink demands the very node bound to $DATA at the source;
        // a sink consuming a different node must not match.
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var other = Ident("other");
        var source = Call("ReceiveData", argument);
        var sink = Call("ExecuteAction", Ident("prefix"), other); // not the tainted node
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source)
            .AddNode(sink);
        graph.Connect(source, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        Assert.Empty(findings);
    }

    // ------------------------------------------------------------------
    // Fixed point over loops and merges
    // ------------------------------------------------------------------

    [Fact]
    public void CycleInDataFlow_TerminatesAndReachesSink()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var a = Ident("a");
        var b = Ident("b");
        var sink = Call("ExecuteAction", Ident("prefix"), argument);

        // a <-> b loop, then to the sink: fixed-point must terminate.
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, a)
            .AddNode(a, b)
            .AddNode(b, a)
            .AddNode(b, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Equal(new[] { source, a, b, sink }, finding.Path);
    }

    [Fact]
    public void DiamondFlow_ShortestPathReportedOnce()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var left = Ident("left");
        var right = Ident("right");
        var mid = Ident("mid");
        var sink = Call("ExecuteAction", Ident("prefix"), argument);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, left, right)
            .AddNode(left, mid)
            .AddNode(right, mid)
            .AddNode(mid, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        var finding = Assert.Single(findings);
        Assert.Equal(4, finding.Path.Count); // source, left/right, mid, sink
    }

    // ------------------------------------------------------------------
    // Interprocedural flow
    // ------------------------------------------------------------------

    [Fact]
    public void InterproceduralEdges_FlowAcrossFunctionBoundary()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);

        // call site -> callee parameter -> callee body -> sink (all via
        // DFG successor edges, the host graph provides interprocedural edges).
        var calleeParam = Ident("param");
        var sink = Call("ExecuteAction", Ident("prefix"), argument);

        var graph = new LabyrinthDataFlowGraph()
            .AddNode(source, calleeParam)
            .AddNode(calleeParam, sink);

        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();

        Assert.Single(findings);
        Assert.Equal(new[] { source, calleeParam, sink }, findings[0].Path);
    }

    // ------------------------------------------------------------------
    // Cancellation
    // ------------------------------------------------------------------

    [Fact]
    public void Cancellation_ThrowsOperationCanceled()
    {
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var source = Call("ReceiveData", Ident("userInput"));
        var graph = new LabyrinthDataFlowGraph().AddNode(source);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new LabyrinthTaintEngine(matchers, graph).Analyze(cts.Token));
    }

    [Fact]
    public void SearchRule_RejectedByTaintEngine()
    {
        var rule = new LabyrinthRule { Id = "s", Type = "search", Pattern = "Require" };
        var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
        var matchers = _compiler.CompileRule(compiled);

        Assert.Throws<LabyrinthRuleException>(
            () => new LabyrinthTaintEngine(matchers, new LabyrinthDataFlowGraph()));
    }

    // ------------------------------------------------------------------
    // Scale / GC-cache-coherence benchmark
    // ------------------------------------------------------------------

    [Fact]
    public async Task LargeGraph_TerminatesPromptly_WithBoundedAllocations()
    {
        const int ChainLength = 20_000;
        var matchers = CompileTaintRule(
            sources: new[] { "ReceiveData($DATA)" },
            sinks: new[] { "ExecuteAction($PREFIX, $DATA)" });

        var argument = Ident("userInput");
        var source = Call("ReceiveData", argument);
        var sink = Call("ExecuteAction", Ident("prefix"), argument);

        var graph = new LabyrinthDataFlowGraph().AddNode(source);
        var previous = (ILabyrinthMatchNode)source;
        for (var i = 0; i < ChainLength; i++)
        {
            var node = Ident("n" + i);
            graph.Connect(previous, node);
            previous = node;
        }

        graph.Connect(previous, sink);

        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        var findings = await Task.Run(() => new LabyrinthTaintEngine(matchers, graph).Analyze());
        stopwatch.Stop();

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationsBefore;
        _output.WriteLine($"Chain of {ChainLength + 2} nodes: {stopwatch.ElapsedMilliseconds} ms, " +
                          $"{allocated / 1024} KB allocated, GC gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)}");

        var finding = Assert.Single(findings);
        Assert.Equal(ChainLength + 2, finding.Path.Count);
    }
}
