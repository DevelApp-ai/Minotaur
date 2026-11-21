using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Pattern v2 tests (Minotaur issue #247): receiver-member calls
/// (<c>$X.Method(...)</c>, <c>$X?.Method(...)</c>), index accesses
/// (<c>$ARR[$I]</c>), binary operator chains (<c>/</c>, <c>%</c>, <c>as</c>,
/// <c>??</c> as <c>op:</c> nodes) and assignment right-hand sides extended
/// beyond calls — while every v1 shape keeps compiling and matching as before.
/// </summary>
public sealed class LabyrinthPatternV2Tests
{
    private readonly LabyrinthPatternParser _parser;

    private const string TestGrammarV2 = """
        Grammar: TestLangV2
        FormatType: EBNF

        <WS> ::= /[ \t\r\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
        <NUMBER> ::= /[0-9]+/
        <STRING> ::= /"[^"]*"/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <EQUALS> ::= "="
        <DOT> ::= "."
        <SLASH> ::= "/"
        <PERCENT> ::= "%"
        <LBRACKET> ::= "["
        <RBRACKET> ::= "]"
        <COALESCE> ::= "??"
        <QMARK> ::= "?"

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | STRING | LPAREN | RPAREN | COMMA | EQUALS | DOT | SLASH | PERCENT | LBRACKET | RBRACKET | COALESCE | QMARK ;
        """;

    public LabyrinthPatternV2Tests()
    {
        _parser = new LabyrinthPatternParser(TestGrammarV2, "TestLangV2");
    }

    private LabyrinthPatternAst Parse(string pattern) => _parser.ParsePattern(pattern);

    private static LabyrinthMatchNode Ident(string name) => new("Identifier", name);

    private static LabyrinthMatchNode Operator(
        string opName, ILabyrinthMatchNode left, ILabyrinthMatchNode right) =>
        new("FunctionCall", opName, new[] { left, right });

    private static ILabyrinthMatchNode Assignment(ILabyrinthMatchNode target, ILabyrinthMatchNode value) =>
        new LabyrinthMatchNode("Assignment", null, new[] { target, value });

    // ---------------------------------------------------------------------
    // Receiver-member call patterns: $X.Method(...)
    // ---------------------------------------------------------------------

    [Fact]
    public void ReceiverCallPattern_BindsReceiverAndName()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$X.DoWork()"));
        var receiver = Ident("svc");
        var node = new LabyrinthMatchNode("FunctionCall", "DoWork", Array.Empty<ILabyrinthMatchNode>(), receiver);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var bound));
        Assert.Same(receiver, bound);
    }

    [Fact]
    public void ReceiverCallPattern_BindsArguments()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$X.DoWork($A)"));
        var argument = Ident("payload");
        var node = new LabyrinthMatchNode("FunctionCall", "DoWork", new[] { argument }, Ident("svc"));

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("A", out var bound));
        Assert.Same(argument, bound);
    }

    [Fact]
    public void ReceiverCallPattern_LiteralReceiverMatchesByName()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("list.Find($V)"));
        var node = new LabyrinthMatchNode("FunctionCall", "Find", new ILabyrinthMatchNode[] { Ident("item") }, Ident("list"));

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("V", out var bound));
        Assert.Equal("item", bound.Name);

        // A receiver with another name must not match.
        var other = new LabyrinthMatchNode("FunctionCall", "Find", new ILabyrinthMatchNode[] { Ident("item") }, Ident("other"));
        Assert.False(matcher(other, new LabyrinthMatchContext()));
    }

    [Fact]
    public void ReceiverCallPattern_RejectsMissingReceiver()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$X.DoWork()"));
        var bareCall = new LabyrinthMatchNode("FunctionCall", "DoWork");
        Assert.False(matcher(bareCall, new LabyrinthMatchContext()));
    }

    [Fact]
    public void ReceiverCallPattern_RejectsNullConditionalAccess()
    {
        // A plain '.' pattern deliberately does not match safe derefs: a null
        // on the receiver is handled by the conditional access itself.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$X.DoWork()"));
        var safe = new LabyrinthMatchNode(
            "FunctionCall", "DoWork", Array.Empty<ILabyrinthMatchNode>(), Ident("svc"), isNullConditional: true);
        Assert.False(matcher(safe, new LabyrinthMatchContext()));
    }

    [Fact]
    public void NullConditionalCallPattern_MatchesOnlySafeAccess()
    {
        // "?." may lex as one token or as a '?' '.' pair; the TestLangV2
        // grammar lexes the pair, exercising both detection paths.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$X?.ReadConfig()"));

        var safe = new LabyrinthMatchNode(
            "FunctionCall", "ReadConfig", Array.Empty<ILabyrinthMatchNode>(), Ident("cfg"), isNullConditional: true);
        var plain = new LabyrinthMatchNode(
            "FunctionCall", "ReadConfig", Array.Empty<ILabyrinthMatchNode>(), Ident("cfg"));

        Assert.True(matcher(safe, new LabyrinthMatchContext()));
        Assert.False(matcher(plain, new LabyrinthMatchContext()));
    }

    [Fact]
    public void PlainCallPattern_StillMatchesReceiverCallByName()
    {
        // v1 compatibility: a bare call pattern matches by callee name, with
        // or without a materialized receiver.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("DoWork()"));
        var withReceiver = new LabyrinthMatchNode("FunctionCall", "DoWork", Array.Empty<ILabyrinthMatchNode>(), Ident("svc"));
        Assert.True(matcher(withReceiver, new LabyrinthMatchContext()));
        Assert.True(matcher(new LabyrinthMatchNode("FunctionCall", "DoWork"), new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Index access patterns: $ARR[$I]
    // ---------------------------------------------------------------------

    [Fact]
    public void IndexAccessPattern_BindsReceiverAndIndex()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$ARR[$I]"));
        var buffer = Ident("buffer");
        var index = Ident("i");
        var node = new LabyrinthMatchNode("IndexAccess", null, new[] { index }, buffer);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("ARR", out var boundArray));
        Assert.Same(buffer, boundArray);
        Assert.True(context.TryGet("I", out var boundIndex));
        Assert.Same(index, boundIndex);
    }

    [Fact]
    public void IndexAccessPattern_LiteralArrayNameMatchesReceiver()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("buffer[$I]"));
        var node = new LabyrinthMatchNode("IndexAccess", null, new ILabyrinthMatchNode[] { Ident("0") }, Ident("buffer"));

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));

        var other = new LabyrinthMatchNode("IndexAccess", null, new ILabyrinthMatchNode[] { Ident("i") }, Ident("cache"));
        Assert.False(matcher(other, new LabyrinthMatchContext()));
    }

    [Fact]
    public void IndexAccessPattern_RejectsMissingReceiver()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$ARR[$I]"));
        Assert.False(matcher(new LabyrinthMatchNode("IndexAccess", null, new ILabyrinthMatchNode[] { Ident("i") }),
            new LabyrinthMatchContext()));
    }

    [Fact]
    public void IndexAccessPattern_RejectsWrongArity()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$ARR[$I]"));
        var twoIndices = new LabyrinthMatchNode("IndexAccess", null, new ILabyrinthMatchNode[] { Ident("i"), Ident("j") }, Ident("buffer"));
        Assert.False(matcher(twoIndices, new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Binary operator patterns: $A / $B, %, as, ??
    // ---------------------------------------------------------------------

    [Fact]
    public void OperatorPattern_Divide_BindsOperands()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$A / $Z"));
        var dividend = Ident("total");
        var divisor = Ident("count");
        var node = Operator("op:divide", dividend, divisor);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("A", out var boundA));
        Assert.Same(dividend, boundA);
        Assert.True(context.TryGet("Z", out var boundZ));
        Assert.Same(divisor, boundZ);
    }

    [Fact]
    public void OperatorPattern_Modulo_MatchesByOpName()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$A % $Z"));
        Assert.True(matcher(Operator("op:modulo", Ident("value"), Ident("modulo")), new LabyrinthMatchContext()));
        Assert.False(matcher(Operator("op:divide", Ident("value"), Ident("modulo")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void OperatorPattern_As_BindsValueAndTarget()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$V as $T"));
        var node = Operator("op:as", Ident("value"), Ident("string"));

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("V", out var boundV));
        Assert.Equal("value", boundV.Name);
        Assert.True(context.TryGet("T", out var boundT));
        Assert.Equal("string", boundT.Name);
    }

    [Fact]
    public void OperatorPattern_Coalesce_MatchesSingleToken()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$A ?? $B"));
        var context = new LabyrinthMatchContext();
        Assert.True(matcher(Operator("op:coalesce", Ident("first"), Ident("fallback")), context));
        Assert.True(context.TryGet("A", out var boundA));
        Assert.Equal("first", boundA.Name);
        Assert.False(matcher(Operator("op:divide", Ident("first"), Ident("fallback")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void OperatorPattern_LeftAssociativeChain()
    {
        // (total / count) / scale materializes left-associatively; the pattern
        // $A / $B / $C matches that shape, not the right-folded one.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$A / $B / $C"));
        var leftFold = Operator("op:divide", Operator("op:divide", Ident("total"), Ident("count")), Ident("scale"));
        Assert.True(matcher(leftFold, new LabyrinthMatchContext()));

        var rightFold = Operator("op:divide", Ident("total"), Operator("op:divide", Ident("count"), Ident("scale")));
        Assert.False(matcher(rightFold, new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Assignment right-hand sides (v2 forms)
    // ---------------------------------------------------------------------

    [Fact]
    public void Assignment_BareMetavariableRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$TARGET = $DATA"));
        var target = Ident("sink");
        var value = Ident("origin");
        var node = Assignment(target, value);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("TARGET", out var boundT));
        Assert.Same(target, boundT);
        Assert.True(context.TryGet("DATA", out var boundD));
        Assert.Same(value, boundD);
    }

    [Fact]
    public void Assignment_BareIdentifierRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$TARGET = origin"));
        var node = Assignment(Ident("sink"), Ident("origin"));
        Assert.True(matcher(node, new LabyrinthMatchContext()));
    }

    [Fact]
    public void Assignment_AsCastRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$T = $V as $T2"));
        var node = Assignment(Ident("typed"), Operator("op:as", Ident("value"), Ident("string")));

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("V", out var boundV));
        Assert.Equal("value", boundV.Name);
    }

    [Fact]
    public void Assignment_DivideRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$T = $A / $B"));
        var node = Assignment(Ident("result"), Operator("op:divide", Ident("total"), Ident("count")));
        Assert.True(matcher(node, new LabyrinthMatchContext()));
        Assert.False(matcher(Assignment(Ident("result"), Operator("op:modulo", Ident("total"), Ident("count"))),
            new LabyrinthMatchContext()));
    }

    [Fact]
    public void Assignment_ReceiverCallRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$T = $X.Find($V)"));
        var argument = Ident("key");
        var call = new LabyrinthMatchNode("FunctionCall", "Find", new[] { argument }, Ident("list"));
        var node = Assignment(Ident("item"), call);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var boundX));
        Assert.Equal("list", boundX.Name);
        Assert.True(context.TryGet("V", out var boundV));
        Assert.Same(argument, boundV);
    }

    [Fact]
    public void Assignment_IndexRightHandSide()
    {
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$T = $ARR[$I]"));
        var index = new LabyrinthMatchNode("IndexAccess", null, new ILabyrinthMatchNode[] { Ident("0") }, Ident("buffer"));
        var node = Assignment(Ident("slot"), index);

        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("ARR", out var boundArray));
        Assert.Equal("buffer", boundArray.Name);
    }

    [Fact]
    public void Assignment_CallRightHandSide_StillSupported()
    {
        // v1 compatibility: '<lhs> = Call(<args>)' keeps working.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("$TARGET = FormatString($SRC)"));
        var node = Assignment(Ident("msg"), new LabyrinthMatchNode("FunctionCall", "FormatString", new ILabyrinthMatchNode[] { Ident("raw") }));
        Assert.True(matcher(node, new LabyrinthMatchContext()));
    }

    [Fact]
    public void Assignment_ReceiverCall_SanitizerStillSupported()
    {
        // The sanitizer win for the rule pack: a guard call materializes as
        // a member call named ThrowIfNull on the ArgumentNullException
        // receiver, and the pattern binds its argument like any other
        // receiver call.
        var matcher = new LabyrinthExpressionCompiler().Compile("r", Parse("ArgumentNullException.ThrowIfNull($X)"));
        var argument = Ident("svc");
        var node = new LabyrinthMatchNode("FunctionCall", "ThrowIfNull", new[] { argument }, Ident("ArgumentNullException"));
        var context = new LabyrinthMatchContext();
        Assert.True(matcher(node, context));
        Assert.True(context.TryGet("X", out var bound));
        Assert.Same(argument, bound);

        // The guard is tied to its guard class: calls on another receiver do not match.
        var other = new LabyrinthMatchNode("FunctionCall", "ThrowIfNull", new[] { argument }, Ident("OtherClass"));
        Assert.False(matcher(other, new LabyrinthMatchContext()));
    }

    // ---------------------------------------------------------------------
    // Ambiguous lexing: the exploratory lexer forks at every position where
    // more than one token rule matches and emits ALL readings into the token
    // union. Both readings must collapse to the longest-match tiling.
    // ---------------------------------------------------------------------

    [Fact]
    public void AmbiguousLexUnion_CoalesceTilesToLongestMatch()
    {
        // '??' lexes two ways in this grammar (one COALESCE token or two
        // QMARKs); the tiling must keep the single '??' token so the pattern
        // compiles as a plain operator chain.
        var ast = Parse("$A ?? $B");
        Assert.Equal(3, ast.Nodes.Count);
        Assert.Equal("$A", ast.Nodes[0].Value);
        Assert.Equal("??", ast.Nodes[1].Value);
        Assert.Equal("$B", ast.Nodes[2].Value);
    }

    [Fact]
    public void AmbiguousLexUnion_EllipsisTilesOverDotForks()
    {
        // '...' lexes two ways once the grammar defines a DOT rule: one
        // ellipsis token or three DOTs. The tiling keeps the single
        // ellipsis, so the classic ellipsis sink pattern keeps its shape.
        var ast = Parse("ExecuteAction(..., $DATA, ...)");
        Assert.Equal(8, ast.Nodes.Count);
        Assert.Equal(2, ast.Nodes.Count(n => n.Kind == LabyrinthPatternNodeKind.Ellipsis));
        Assert.DoesNotContain(ast.Nodes, n => n.Kind == LabyrinthPatternNodeKind.Literal && n.Value == ".");
    }
}
