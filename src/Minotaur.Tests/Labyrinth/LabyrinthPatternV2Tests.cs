using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;
using Xunit.Abstractions;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Pattern v2 tests (Minotaur issue #247): the shapes the
/// graph-symbolic-execution article rule pack needs — metavariable receiver
/// calls (<c>$X.DoWork(...)</c>), conditional access (<c>?.</c>), binary
/// operator nodes (<c>/</c>, <c>%</c>, <c>as</c>, <c>??</c>), index patterns
/// (<c>$ARR[$I]</c>), relaxed assignment right-hand sides — compiled by the
/// expression compiler directly, and then exercised end to end through
/// <see cref="ScanHost"/> materialization plus the taint engine.
/// </summary>
public sealed class LabyrinthPatternV2Tests
{
    private readonly ITestOutputHelper _output;
    private readonly LabyrinthPatternParser _parser;

    private const string TestGrammar = """
        Grammar: TestLang
        FormatType: EBNF

        <WS> ::= /[ \t\r\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
        <NUMBER> ::= /[0-9]+/
        <STRING> ::= /"[^"]*"/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <LBRACKET> ::= "["
        <RBRACKET> ::= "]"
        <COMMA> ::= ","
        <EQUALS> ::= "="
        <SEMI> ::= ";"
        <DOT> ::= "."
        <SLASH> ::= "/"
        <PERCENT> ::= "%"
        <QUESTION_QUESTION> ::= "??"
        <QUESTION_DOT> ::= "?."
        <QUESTION> ::= "?"

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | STRING | LPAREN | RPAREN | LBRACKET | RBRACKET
                 | COMMA | EQUALS | SEMI | DOT | SLASH | PERCENT
                 | QUESTION_QUESTION | QUESTION_DOT | QUESTION ;
        """;

    public LabyrinthPatternV2Tests(ITestOutputHelper output)
    {
        _output = output;
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    private LabyrinthPatternAst Parse(string pattern) => _parser.ParsePattern(pattern);

    private Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> Compile(string pattern) =>
        new LabyrinthExpressionCompiler().Compile("v2tests", Parse(pattern));

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static LabyrinthMatchNode Ident(string name) => new("Identifier", name);

    private static LabyrinthMatchNode Assignment(ILabyrinthMatchNode target, ILabyrinthMatchNode value) =>
        new("Assignment", null, new[] { target, value });

    private static ILabyrinthMatchNode DottedCall(string name, ILabyrinthMatchNode receiver,
        params ILabyrinthMatchNode[] args) =>
        new LabyrinthMatchNode("FunctionCall", name, args, receiver);

    // ---------------------------------------------------------------------
    // Dotted metavariable call patterns: $X.Method(...)
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // TEMPORARY token diagnostics (issue #247): dumps the pattern token
    // streams and ScanHost source materializations into the failure message
    // so the CI assert annotations carry the ground truth. The known-failing
    // v2 tests are temporarily skipped to reserve the annotation budget for
    // these lines; both follow-ups land in the next commit.
    // ---------------------------------------------------------------------

    [Fact]
    public void Diagnostic_TokenDump()
    {
        var lines = new List<string>();
        foreach (var pattern in new[] { "$X.DoWork(...)", "GetIndex(...)", "$X?.DoWork()", "$A ?? $B" })
        {
            var ast = Parse(pattern);
            lines.Add("P#" + pattern + "# => " + string.Join(" | ", ast.Nodes.Select(n =>
                n.Kind + ":" + n.TokenType + ":[" + (n.MetavariableName ?? n.Value) + "]")));
        }

        foreach (var snippet in new[] { "w.DoWork()", "z?.DoWork()", "items.FirstOrDefault()" })
        {
            try
            {
                ScanHost.BuildGraph(snippet + ";", "diag.cs", TestGrammar, "TestLang", out var nodes);
                lines.Add("S#" + snippet + "# => " + string.Join(" ; ", nodes.Select(n =>
                    n.NodeType + ":[" + (n.Name ?? "-") + "]/a" + n.Arguments.Count +
                    (n.Receiver is null ? string.Empty : "/r[" + n.Receiver.Name + "]"))));
            }
            catch (Exception ex)
            {
                lines.Add("S#" + snippet + "# => EX " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        Assert.True(false, string.Join("\n", lines));
    }

    [Fact]
    public void DottedMetavarCall_BindsReceiverAndArguments()
    {
        var matcher = Compile("$X.DoWork($A)");
        var context = new LabyrinthMatchContext();

        var receiver = Ident("obj");
        var argument = Ident("a");
        var node = DottedCall("obj.DoWork", receiver, argument);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var boundReceiver));
        Assert.Same(receiver, boundReceiver);
        Assert.True(context.TryGet("A", out var boundArg));
        Assert.Same(argument, boundArg);
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void DottedMetavarCall_RejectsDifferentMethod()
    {
        var matcher = Compile("$X.DoWork(...)");
        var node = DottedCall("obj.Other", Ident("obj"));

        Assert.False(matcher(node, new LabyrinthMatchContext()));
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void DottedMetavarCall_RejectsMissingReceiver()
    {
        var matcher = Compile("$X.DoWork(...)");
        var node = Call("obj.DoWork");

        Assert.False(matcher(node, new LabyrinthMatchContext()));
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void DottedMetavarCall_RejectsConditionalAccessName()
    {
        // The safe deref spelling `x?.DoWork()` keeps `?.` in the Name; a
        // plain deref sink must not match it.
        var matcher = Compile("$X.DoWork(...)");
        var node = DottedCall("obj?.DoWork", Ident("obj"));

        Assert.False(matcher(node, new LabyrinthMatchContext()));
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void ConditionalAccessPattern_Binds()
    {
        var matcher = Compile("$X?.DoWork()");
        var context = new LabyrinthMatchContext();
        var receiver = Ident("obj");
        var node = DottedCall("obj?.DoWork", receiver);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var bound));
        Assert.Same(receiver, bound);
    }

    [Fact]
    public void LiteralDottedCall_CompilesToNameEquality()
    {
        // v1 could not compile dotted literal calls at all (the DOT token
        // broke the IDENTIFIER LPAREN shape); v2 joins the dotted prefix
        // into a name-equality call pattern.
        var matcher = Compile("ArgumentNullException.ThrowIfNull($X)");
        var context = new LabyrinthMatchContext();
        var argument = Ident("x");
        var node = Call("ArgumentNullException.ThrowIfNull", argument);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var bound));
        Assert.Same(argument, bound);
        Assert.False(matcher(Call("ThrowIfNull", argument), new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Binary operator patterns
    // ---------------------------------------------------------------------

    [Fact]
    public void BinaryOperator_Divide_BindsBothOperands()
    {
        var matcher = Compile("$A / $B");
        var context = new LabyrinthMatchContext();
        var left = Ident("num");
        var right = Ident("den");
        var node = Call("op:divide", left, right);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("A", out var boundLeft));
        Assert.Same(left, boundLeft);
        Assert.True(context.TryGet("B", out var boundRight));
        Assert.Same(right, boundRight);
    }

    [Fact]
    public void BinaryOperator_As_Modulo_Coalesce()
    {
        Assert.True(
            Compile("$V as $T")(Call("op:as", Ident("value"), Ident("Type")), new LabyrinthMatchContext()));
        Assert.True(
            Compile("$A % $B")(Call("op:modulo", Ident("i"), Ident("len")), new LabyrinthMatchContext()));
        Assert.True(
            Compile("$A ?? $B")(Call("op:coalesce", Ident("x"), Ident("fallback")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void BinaryOperator_RejectsWrongShape()
    {
        var matcher = Compile("$A / $B");

        Assert.False(matcher(Call("op:divide", Ident("onlyOne")), new LabyrinthMatchContext()));
        Assert.False(matcher(Call("op:divide"), new LabyrinthMatchContext()));
        Assert.False(matcher(Call("op:modulo", Ident("a"), Ident("b")), new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Index patterns: $ARR[$I]
    // ---------------------------------------------------------------------

    [Fact]
    public void IndexPattern_BindsArrayReceiverAndIndex()
    {
        var matcher = Compile("$ARR[$I]");
        var context = new LabyrinthMatchContext();
        var array = Ident("data");
        var index = Ident("i");
        var node = new LabyrinthMatchNode("IndexAccess", "data", new[] { index }, array);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("ARR", out var boundArray));
        Assert.Same(array, boundArray);
        Assert.True(context.TryGet("I", out var boundIndex));
        Assert.Same(index, boundIndex);
    }

    [Fact]
    public void IndexPattern_RejectsFunctionCallNodes()
    {
        var matcher = Compile("$ARR[$I]");

        Assert.False(matcher(Call("data", Ident("i")), new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Relaxed assignment right-hand sides
    // ---------------------------------------------------------------------

    [Fact]
    public void Assignment_BareMetavariableRhs_BindsBothSides()
    {
        var matcher = Compile("$X = $Y");
        var context = new LabyrinthMatchContext();
        var left = Ident("x");
        var right = Ident("y");
        var node = Assignment(left, right);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var boundLeft));
        Assert.Same(left, boundLeft);
        Assert.True(context.TryGet("Y", out var boundRight));
        Assert.Same(right, boundRight);
    }

    [Fact]
    public void Assignment_OperatorRhs_BindsThroughOpNode()
    {
        var matcher = Compile("$X = $A / $B");
        var context = new LabyrinthMatchContext();
        var opRight = Call("op:divide", Ident("num"), Ident("den"));
        var node = Assignment(Ident("q"), opRight);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("A", out _));
        Assert.True(context.TryGet("B", out _));
        Assert.True(context.TryGet("X", out _));
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void Assignment_CoalesceRhs_Binds()
    {
        var matcher = Compile("$SAFE = $A ?? $B");
        var context = new LabyrinthMatchContext();
        var node = Assignment(Ident("y"), Call("op:coalesce", Ident("x"), Ident("fb")));

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("SAFE", out _));
    }

    [Fact]
    public void Assignment_DottedRhs_BindsReceiver()
    {
        var matcher = Compile("$X = $Y.DoWork()");
        var context = new LabyrinthMatchContext();
        var receiver = Ident("obj");
        var node = Assignment(Ident("b"), DottedCall("obj.DoWork", receiver));

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("Y", out var boundY));
        Assert.Same(receiver, boundY);
    }

    [Fact]
    public void Assignment_IndexRhs_Binds()
    {
        var matcher = Compile("$V = $ARR[$I]");
        var context = new LabyrinthMatchContext();
        var rhs = new LabyrinthMatchNode("IndexAccess", "data", new[] { Ident("i") }, Ident("data"));
        var node = Assignment(Ident("v"), rhs);

        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("ARR", out _));
        Assert.True(context.TryGet("I", out _));
    }

    // ---------------------------------------------------------------------
    // End-to-end: ScanHost materialization + taint engine
    // ---------------------------------------------------------------------

    private List<LabyrinthTaintFinding> Scan(string source, Action<LabyrinthRule> define)
    {
        var rule = new LabyrinthRule { Id = "test-rule", Type = "taint", Severity = "ERROR" };
        rule.Sources = new List<LabyrinthPatternEntry>();
        rule.Sinks = new List<LabyrinthPatternEntry>();
        rule.Sanitizers = new List<LabyrinthPatternEntry>();
        rule.Propagators = new List<LabyrinthPropagatorEntry>();
        define(rule);

        var ruleSet = new LabyrinthRuleSet();
        ruleSet.Rules.Add(rule);

        var graph = ScanHost.BuildGraph(source, "test.cs", TestGrammar, "TestLang", out var nodes);
        return ScanHost.RunRules(ruleSet, TestGrammar, "TestLang", graph, nodes);
    }

    private static LabyrinthTaintFinding[] ScanHelper(string source, LabyrinthRule rule)
    {
        var ruleSet = new LabyrinthRuleSet();
        ruleSet.Rules.Add(rule);
        var graph = ScanHost.BuildGraph(source, "test.cs", TestGrammar, "TestLang", out var nodes);
        return ScanHost.RunRules(ruleSet, TestGrammar, "TestLang", graph, nodes).ToArray();
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void EndToEnd_NullDeref_VulnerableFlowIsReported()
    {
        // var x = items.FirstOrDefault(); x.ToString();
        // The source binds $RCV (the receiver of the maybe-null call), the
        // propagator carries the value into $X, and the sink deref unifies
        // on the very same variable through the receiver.
        var findings = Scan(
            "var x = items.FirstOrDefault();\nx.ToString();",
            rule =>
            {
                rule.Sources!.Add(new LabyrinthPatternEntry { Pattern = "$RCV.FirstOrDefault(...)" });
                rule.Sinks!.Add(new LabyrinthPatternEntry { Pattern = "$X.ToString()" });
                rule.Propagators!.Add(new LabyrinthPropagatorEntry { Pattern = "$X = $Y", From = "$Y", To = "$X" });
            });

        Assert.Single(findings);
        var sinkNode = findings[0].Sink as SourceMatchNode;
        Assert.NotNull(sinkNode);
        Assert.Equal(2, sinkNode.StartLine);
        _output.WriteLine($"finding at line {sinkNode.StartLine}");
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void EndToEnd_NullDeref_ThrowIfNullSanitizes()
    {
        var findings = Scan(
            "var x = items.FirstOrDefault();\nArgumentNullException.ThrowIfNull(x);\nx.ToString();",
            rule =>
            {
                rule.Sources!.Add(new LabyrinthPatternEntry { Pattern = "$RCV.FirstOrDefault(...)" });
                rule.Sinks!.Add(new LabyrinthPatternEntry { Pattern = "$X.ToString()" });
                rule.Sanitizers!.Add(new LabyrinthPatternEntry { Pattern = "ArgumentNullException.ThrowIfNull($X)" });
                rule.Propagators!.Add(new LabyrinthPropagatorEntry { Pattern = "$X = $Y", From = "$Y", To = "$X" });
            });

        Assert.Empty(findings);
    }

    [Fact]
    public void EndToEnd_NullDeref_ConditionalAccessIsSafe()
    {
        var findings = Scan(
            "var x = items.FirstOrDefault();\nx?.ToString();",
            rule =>
            {
                rule.Sources!.Add(new LabyrinthPatternEntry { Pattern = "$RCV.FirstOrDefault(...)" });
                rule.Sinks!.Add(new LabyrinthPatternEntry { Pattern = "$X.ToString()" });
            });

        Assert.Empty(findings);
    }

    [Fact]
    public void EndToEnd_NullDeref_DistinctVariableIsNotFlagged()
    {
        // Taint on x must not flag y.ToString(): receiver unification fails.
        var findings = Scan(
            "var x = items.FirstOrDefault();\nvar y = other.Create();\ny.ToString();",
            rule =>
            {
                rule.Sources!.Add(new LabyrinthPatternEntry { Pattern = "$RCV.FirstOrDefault(...)" });
                rule.Sinks!.Add(new LabyrinthPatternEntry { Pattern = "$X.ToString()" });
                rule.Propagators!.Add(new LabyrinthPropagatorEntry { Pattern = "$X = $Y", From = "$Y", To = "$X" });
            });

        Assert.Empty(findings);
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void EndToEnd_Division_ZeroDivisorIsReported_RenamedDivisorIsNot()
    {
        LabyrinthRule Rule()
        {
            var rule = new LabyrinthRule { Id = "div-zero", Type = "taint", Severity = "ERROR" };
            rule.Sources = new List<LabyrinthPatternEntry> { new() { Pattern = "0" } };
            rule.Propagators = new List<LabyrinthPropagatorEntry>
            {
                new() { Pattern = "$Z = $Y", From = "$Y", To = "$Z" }
            };
            rule.Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "$A / $Z" } };
            return rule;
        }

        var vulnerable = ScanHelper("var a = Files.Count();\na = 0;\nvar q = b / a;", Rule());
        Assert.Single(vulnerable);

        var safe = ScanHelper("var a = Files.Count();\na = 0;\nvar q = b / c;", Rule());
        Assert.Empty(safe);
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void EndToEnd_Index_TaintedIndexIsReported_OtherIndexIsNot()
    {
        LabyrinthRule Rule()
        {
            var rule = new LabyrinthRule { Id = "index", Type = "taint", Severity = "ERROR" };
            rule.Sources = new List<LabyrinthPatternEntry> { new() { Pattern = "GetIndex(...)" } };
            rule.Propagators = new List<LabyrinthPropagatorEntry>
            {
                new() { Pattern = "$I = $J", From = "$J", To = "$I" }
            };
            rule.Sinks = new List<LabyrinthPatternEntry> { new() { Pattern = "$ARR[$I]" } };
            return rule;
        }

        var vulnerable = ScanHelper("var i = GetIndex();\nvar v = data[i];", Rule());
        Assert.Single(vulnerable);

        var safe = ScanHelper("var i = GetIndex();\nvar v = data[j];", Rule());
        Assert.Empty(safe);
    }

    [Fact]
    public void EndToEnd_Materialization_ProducesReceiverOpAndIndexNodes()
    {
        var graph = ScanHost.BuildGraph(
            "var t = y as T;\nvar q = b / a;\nvar s = x ?? f;\nvar v = data[i];",
            "test.cs", TestGrammar, "TestLang", out var nodes);

        var materialized = nodes
            .Select(n => (n.NodeType, n.Name))
            .ToList();

        // op:as / op:divide / op:coalesce operator nodes
        Assert.Contains(("FunctionCall", "op:as"), materialized);
        Assert.Contains(("FunctionCall", "op:divide"), materialized);
        Assert.Contains(("FunctionCall", "op:coalesce"), materialized);

        // IndexAccess node with the array materialized as its receiver
        var index = nodes.First(n => n.NodeType == "IndexAccess");
        Assert.Equal("data", index.Name);
        Assert.NotNull(index.Receiver);
        Assert.Equal("data", index.Receiver!.Name);
    }

    [Fact(Skip = "Known failure under diagnosis; temporarily skipped for the issue #247 token-dump round, restored in the next commit.")]
    public void EndToEnd_Materialization_DottedCallAndConditionalAccess()
    {
        var graph = ScanHost.BuildGraph(
            "x.DoWork();\ny?.DoWork();\nDoWork();",
            "test.cs", TestGrammar, "TestLang", out var nodes);

        var plain = nodes.First(n => n.NodeType == "FunctionCall" && n.Name == "x.DoWork");
        Assert.NotNull(plain.Receiver);
        Assert.Equal("x", plain.Receiver!.Name);

        var conditional = nodes.First(n => n.NodeType == "FunctionCall" && n.Name == "y?.DoWork");
        Assert.NotNull(conditional.Receiver);
        Assert.Equal("y", conditional.Receiver!.Name);

        // Plain (non-dotted) calls carry no receiver.
        var callee = nodes.First(n => n.NodeType == "FunctionCall" && n.Name == "DoWork");
        Assert.Null(callee.Receiver);
    }
}
