using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for the <c>pattern-not</c> and <c>pattern-inside</c> entry filters
/// (Labyrinth issue #246, LAB track):
/// - <c>pattern-not</c>: the entry fails when the filter matches the SAME node.
/// - <c>pattern-inside</c>: the entry requires SOME ANCESTOR of the node
///   (match-node tree, via <see cref="LabyrinthMatchContext.ParentMap"/>) to match.
/// Both compose with <c>pattern-either</c> (inside each alternative) and with
/// conditions. Filter metavariables run on a scratch context and never enter
/// the entry's unification scope (v1).
/// </summary>
public sealed class LabyrinthPatternNotInsideTests
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

    public LabyrinthPatternNotInsideTests()
    {
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static ILabyrinthMatchNode Ident(string name) => new LabyrinthMatchNode("Identifier", name);

    private LabyrinthCompiledMatchers CompileSourceEntry(LabyrinthPatternEntry source, string sink = "ExecuteAction($DATA)")
    {
        var rule = new LabyrinthRule
        {
            Id = "filtered-taint",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry> { source },
            Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = sink } },
        };

        return _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));
    }

    // ------------------------------------------------------------------
    // YAML loader: pattern-not / pattern-inside parsing and validation
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_ParsesPatternNotAndInside()
    {
        const string yaml = """
            rules:
              - id: filtered-source
                severity: ERROR
                type: taint
                sources:
                  - pattern: "ReceiveData($DATA)"
                    pattern-not: "ReceiveData(safe)"
                    pattern-inside: "Try(...)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
              - id: filtered-search
                type: search
                pattern: "Do($X)"
                pattern-not: "Do(safe)"
                pattern-inside: "Guard(...)"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        Assert.Equal(2, ruleSet.Rules.Count);
        var taint = ruleSet.Rules[0];
        var entry = Assert.Single(taint.Sources!);
        Assert.Equal("ReceiveData($DATA)", entry.Pattern);
        Assert.Equal("ReceiveData(safe)", entry.PatternNot);
        Assert.Equal("Try(...)", entry.PatternInside);

        var search = ruleSet.Rules[1];
        Assert.Equal("Do(safe)", search.PatternNot);
        Assert.Equal("Guard(...)", search.PatternInside);
    }

    [Fact]
    public void YamlLoader_RejectsEntryLevelFiltersBesidePatternEither()
    {
        const string yaml = """
            rules:
              - id: either-with-not
                severity: ERROR
                type: taint
                sources:
                  - pattern-either:
                      - pattern: "ReceiveData($DATA)"
                    pattern-not: "ReceiveData(safe)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("inside the pattern-either alternatives", ex.Message);
    }

    [Fact]
    public void YamlLoader_AllowsFiltersInsideAlternatives()
    {
        const string yaml = """
            rules:
              - id: either-with-alt-not
                severity: ERROR
                type: taint
                sources:
                  - pattern-either:
                      - pattern: "ReceiveData($DATA)"
                        pattern-not: "ReceiveData(safe)"
                      - pattern: "ReadFile($DATA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        var entry = Assert.Single(ruleSet.Rules[0].Sources!);
        Assert.NotNull(entry.PatternEither);
        Assert.Equal(2, entry.PatternEither!.Count);
        Assert.Equal("ReceiveData(safe)", entry.PatternEither[0].PatternNot);
        Assert.Null(entry.PatternEither[1].PatternNot);
    }

    // ------------------------------------------------------------------
    // pattern-not semantics
    // ------------------------------------------------------------------

    [Fact]
    public void PatternNot_ExcludesNodeMatchingTheFilter()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            Pattern = "Do($X)",
            PatternNot = "Do(safe)",
        });
        var source = Assert.Single(matchers.Sources);

        // Positive match, filter does not: entry matches and binds $X.
        var ctx = new LabyrinthMatchContext();
        var arg = Ident("evil");
        Assert.True(source(Call("Do", arg), ctx));
        Assert.True(ctx.TryGet("X", out var bound));
        Assert.Same(arg, bound);

        // Filter matches the same node: entry fails.
        Assert.False(source(Call("Do", Ident("safe")), new LabyrinthMatchContext()));

        // Different call name: neither pattern applies.
        Assert.False(source(Call("Other", Ident("x")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void PatternNot_MetavariablesDoNotLeakIntoEntryScope()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            Pattern = "Do($X)",
            PatternNot = "Other($Y)",
        });
        var source = Assert.Single(matchers.Sources);

        var ctx = new LabyrinthMatchContext();
        Assert.True(source(Call("Do", Ident("evil")), ctx));
        Assert.True(ctx.TryGet("X", out _));
        Assert.False(ctx.TryGet("Y", out _)); // the not-pattern's $Y never binds
    }

    // ------------------------------------------------------------------
    // pattern-inside semantics
    // ------------------------------------------------------------------

    [Fact]
    public void PatternInside_RequiresMatchingAncestor()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            Pattern = "Do($X)",
            PatternInside = "Try(...)",
        });
        var source = Assert.Single(matchers.Sources);

        // Tree: Try( Middle( Do(x) ) )
        var x = Ident("x");
        var doCall = Call("Do", x);
        var middle = Call("Middle", doCall);
        var tryNode = Call("Try", middle);
        var topLevelDo = Call("Do", Ident("y"));
        var allNodes = new ILabyrinthMatchNode[] { tryNode, middle, doCall, x, topLevelDo };
        var ctx = new LabyrinthMatchContext { ParentMap = LabyrinthMatchContext.BuildParentMap(allNodes) };

        // Do nested (at any depth) inside Try: matches, $X binds.
        Assert.True(source(doCall, ctx));
        Assert.True(ctx.TryGet("X", out var bound));
        Assert.Same(x, bound);

        // Top-level Do has no Try ancestor: fails.
        Assert.False(source(topLevelDo, new LabyrinthMatchContext
        {
            ParentMap = LabyrinthMatchContext.BuildParentMap(allNodes),
        }));

        // No parent map at all: fails closed.
        Assert.False(source(doCall, new LabyrinthMatchContext()));
    }

    [Fact]
    public void PatternInside_MatchesAtAnyAncestorDepth_AndRespectsPattern()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            Pattern = "Do($X)",
            PatternInside = "Middle(...)",
        });
        var source = Assert.Single(matchers.Sources);

        var doCall = Call("Do", Ident("x"));
        var middle = Call("Middle", doCall);
        var tryNode = Call("Try", middle);
        var catchDo = Call("Do", Ident("z"));
        var catchNode = Call("Catch", catchDo);
        var allNodes = new ILabyrinthMatchNode[] { tryNode, middle, doCall, catchNode, catchDo };
        var parentMap = LabyrinthMatchContext.BuildParentMap(allNodes);

        // Ancestor Middle(...) exists (not necessarily the direct parent).
        Assert.True(source(doCall, new LabyrinthMatchContext { ParentMap = parentMap }));
        // Do under Catch only: no Middle ancestor.
        Assert.False(source(catchDo, new LabyrinthMatchContext { ParentMap = parentMap }));
    }

    // ------------------------------------------------------------------
    // Composition with pattern-either and conditions
    // ------------------------------------------------------------------

    [Fact]
    public void PatternEither_AlternativeLevelPatternNot()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            PatternEither = new List<LabyrinthPatternEntry>
            {
                new() { Pattern = "A($X)", PatternNot = "A(bad)" },
                new() { Pattern = "B($X)" },
            },
        });
        var source = Assert.Single(matchers.Sources);

        Assert.True(source(Call("A", Ident("good")), new LabyrinthMatchContext()));
        Assert.False(source(Call("A", Ident("bad")), new LabyrinthMatchContext()));
        Assert.True(source(Call("B", Ident("anything")), new LabyrinthMatchContext()));
        Assert.False(source(Call("C", Ident("x")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void PatternNot_CombinedWithCondition_BothMustPass()
    {
        var matchers = CompileSourceEntry(new LabyrinthPatternEntry
        {
            Pattern = "Do($X)",
            PatternNot = "Do(safe)",
            Condition = "node.Arguments.Count == 1",
        });
        var source = Assert.Single(matchers.Sources);

        Assert.True(source(Call("Do", Ident("evil")), new LabyrinthMatchContext()));
        Assert.False(source(Call("Do", Ident("safe")), new LabyrinthMatchContext())); // not-filter
        Assert.False(source(Call("Do", Ident("a"), Ident("b")), new LabyrinthMatchContext())); // condition
    }

    // ------------------------------------------------------------------
    // Search rules
    // ------------------------------------------------------------------

    [Fact]
    public void SearchRule_PatternNotAndInside()
    {
        var rule = new LabyrinthRule
        {
            Id = "filtered-search",
            Type = "search",
            Pattern = "Do($X)",
            PatternNot = "Do(safe)",
            PatternInside = "Try(...)",
        };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));
        var search = matchers.SearchPattern;
        Assert.NotNull(search);

        var doInside = Call("Do", Ident("evil"));
        var tryNode = Call("Try", doInside);
        var parentMap = LabyrinthMatchContext.BuildParentMap(new ILabyrinthMatchNode[] { tryNode, doInside });

        Assert.True(search!(doInside, new LabyrinthMatchContext { ParentMap = parentMap }));
        Assert.False(search!(Call("Do", Ident("safe")), new LabyrinthMatchContext { ParentMap = parentMap }));
        Assert.False(search!(Call("Do", Ident("top")), new LabyrinthMatchContext { ParentMap = parentMap }));
    }

    // ------------------------------------------------------------------
    // Taint engine integration: pattern-inside works over the DFG node tree
    // ------------------------------------------------------------------

    [Fact]
    public void TaintEngine_SourceWithPatternInside_OnlySeedsInsideAncestor()
    {
        var rule = new LabyrinthRule
        {
            Id = "inside-source",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry>
            {
                new() { Pattern = "ReceiveData($DATA)", PatternInside = "Try(...)" },
            },
            Sinks = new List<LabyrinthPatternEntry>
            {
                new() { Pattern = "ExecuteAction($DATA)" },
            },
        };
        var matchers = _compiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, _parser));

        var data = Ident("userInput");
        var sourceInside = Call("ReceiveData", data);
        var tryNode = Call("Try", sourceInside);
        var sink = Call("ExecuteAction", data);
        var graph = new LabyrinthDataFlowGraph()
            .AddNode(tryNode)
            .AddNode(sourceInside)
            .AddNode(sink);
        graph.Connect(tryNode, sourceInside);
        graph.Connect(sourceInside, sink);

        // The engine builds the parent map from the graph nodes itself.
        var findings = new LabyrinthTaintEngine(matchers, graph).Analyze();
        var finding = Assert.Single(findings);
        Assert.Same(sourceInside, finding.Source);
        Assert.Same(sink, finding.Sink);

        // Without the Try ancestor the source pattern does not match: no findings.
        var bareSource = Call("ReceiveData", Ident("other"));
        var bareSink = Call("ExecuteAction", Ident("other"));
        var graph2 = new LabyrinthDataFlowGraph()
            .AddNode(bareSource)
            .AddNode(bareSink);
        graph2.Connect(bareSource, bareSink);

        Assert.Empty(new LabyrinthTaintEngine(matchers, graph2).Analyze());
    }
}
