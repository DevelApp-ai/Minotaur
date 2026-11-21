using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Pattern v2 materialization tests (Minotaur issue #247): the ScanHost host
/// adapter turns member calls into receiver-bearing FunctionCall nodes,
/// indexing into IndexAccess nodes with the indexed expression as Receiver,
/// and binary operators into left-associative op: FunctionCall nodes. The
/// grammar content is passed inline, so these tests run without downloaded
/// target grammars.
/// </summary>
public sealed class ScanHostMaterializationTests
{
    private const string TestGrammarV2 = """
        Grammar: TestLangV2
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
        <PERCENT> ::= "%"
        <LBRACKET> ::= "["
        <RBRACKET> ::= "]"
        <COALESCE> ::= "??"
        <QMARK> ::= "?"

        <program> ::= <statement> ;
        <statement> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | SEMICOLON | EQUALS | DOT | SLASH | PERCENT | LBRACKET | RBRACKET | COALESCE | QMARK ;
        """;

    private static (LabyrinthDataFlowGraph Graph, IReadOnlyList<ILabyrinthMatchNode> Nodes) Build(
        string source)
    {
        var graph = ScanHost.BuildGraph(source, "test.cs", TestGrammarV2, "TestLangV2", out var nodes);
        return (graph, nodes);
    }

    private static ILabyrinthMatchNode Find(
        IReadOnlyList<ILabyrinthMatchNode> nodes, string nodeType, string? name) =>
        nodes.FirstOrDefault(n => n.NodeType == nodeType && n.Name == name)
        ?? throw new InvalidOperationException($"No {nodeType}:{name} node materialized.");

    [Fact]
    public void MemberCall_MaterializesReceiverAndName()
    {
        var (_, nodes) = Build("svc.DoWork();");

        var call = (ILabyrinthMatchNode)Find(nodes, "FunctionCall", "DoWork");
        Assert.False(call.IsNullConditional);
        Assert.NotNull(call.Receiver);
        Assert.Equal("svc", call.Receiver.Name);
        Assert.Equal("Identifier", call.Receiver.NodeType);
    }

    [Fact]
    public void MemberCall_ReceiverIsTheInternedIdentifier()
    {
        // The receiver must be the very same interned node as a later bare
        // occurrence of the identifier, so taint unification (reference
        // equality) works: the source binds $X at the identifier, the sink
        // binds the receiver.
        var (_, nodes) = Build("svc.DoWork();\nsvc;");

        var call = (ILabyrinthMatchNode)Find(nodes, "FunctionCall", "DoWork");
        var bare = nodes.Single(n => n.NodeType == "Identifier" && n.Name == "svc");
        Assert.Same(bare, call.Receiver);
    }

    [Fact]
    public void NullConditionalCall_MarksSafeAccess()
    {
        var (_, nodes) = Build("svc?.DoWork();");

        var call = (ILabyrinthMatchNode)Find(nodes, "FunctionCall", "DoWork");
        Assert.True(call.IsNullConditional);
        Assert.Equal("svc", call.Receiver?.Name);
    }

    [Fact]
    public void IndexedAccess_MaterializesAsIndexAccessWithReceiver()
    {
        var (_, nodes) = Build("buffer[index];");

        var index = (ILabyrinthMatchNode)Find(nodes, "IndexAccess", null);
        Assert.Equal("Identifier", index.Receiver?.NodeType);
        Assert.Equal("buffer", index.Receiver?.Name);
        Assert.Single(index.Arguments);
        Assert.Equal("Identifier", index.Arguments[0].NodeType);
        Assert.Equal("index", index.Arguments[0].Name);
    }

    [Fact]
    public void Assignment_BareIdentifierRightHandSideIsASingleNode()
    {
        var (_, nodes) = Build("y = z;");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        Assert.Equal("Identifier", assignment.Arguments[1].NodeType);
        Assert.Equal("z", assignment.Arguments[1].Name);
    }

    [Fact]
    public void Assignment_DivideRightHandSideMaterializesOpDivide()
    {
        var (_, nodes) = Build("x = total / count;");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        var rhs = assignment.Arguments[1];
        Assert.Equal("FunctionCall", rhs.NodeType);
        Assert.Equal("op:divide", rhs.Name);
        Assert.Equal(2, rhs.Arguments.Count);
        Assert.Equal("total", rhs.Arguments[0].Name);
        Assert.Equal("count", rhs.Arguments[1].Name);
    }

    [Fact]
    public void Assignment_AsCastMaterializesOpAs()
    {
        var (_, nodes) = Build("p = value as Thing;");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        var rhs = assignment.Arguments[1];
        Assert.Equal("op:as", rhs.Name);
        Assert.Equal("value", rhs.Arguments[0].Name);
        Assert.Equal("Thing", rhs.Arguments[1].Name);
    }

    [Fact]
    public void Assignment_CoalesceMaterializesOpCoalesce()
    {
        var (_, nodes) = Build("h = first ?? fallback;");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        Assert.Equal("op:coalesce", assignment.Arguments[1].Name);
        Assert.Equal("first", assignment.Arguments[1].Arguments[0].Name);
        Assert.Equal("fallback", assignment.Arguments[1].Arguments[1].Name);
    }

    [Fact]
    public void CoalesceLexFork_MaterializesASingleOperatorNode()
    {
        // ?? lexes two ways in this grammar (one COALESCE token or two
        // QMARKs), and the exploratory lexer emits both readings into the
        // token union with overlapping spans. The longest-match tiling must
        // collapse them: one folded operator, no stray '?'/literal nodes.
        var (_, nodes) = Build("h = first ?? fallback;");

        Assert.DoesNotContain(nodes, n => n.Name == "?");
        var op = (ILabyrinthMatchNode)Find(nodes, "FunctionCall", "op:coalesce");
        Assert.Equal(2, op.Arguments.Count);
    }

    [Fact]
    public void Division_ChainsLeftAssociatively()
    {
        var (_, nodes) = Build("x = a / b / c;");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        var outer = assignment.Arguments[1];
        Assert.Equal("op:divide", outer.Name);
        var inner = outer.Arguments[0];
        Assert.Equal("op:divide", inner.Name);
        Assert.Equal("a", inner.Arguments[0].Name);
        Assert.Equal("b", inner.Arguments[1].Name);
        Assert.Equal("c", outer.Arguments[1].Name);
    }

    [Fact]
    public void Assignment_FromReceiverCallProducesPropagatableBinding()
    {
        var (_, nodes) = Build("item = list.FirstOrDefault();");

        var assignment = (ILabyrinthMatchNode)Find(nodes, "Assignment", null);
        var rhs = assignment.Arguments[1];
        Assert.Equal("FunctionCall", rhs.NodeType);
        Assert.Equal("FirstOrDefault", rhs.Name);
        Assert.Equal("list", rhs.Receiver?.Name);
    }

    [Fact]
    public void Compiled_V2PatternsMatchOverMaterializedSourceNodes()
    {
        // End to end within the host conventions: the compiled
        // receiver-call sink pattern matches nodes materialized by BuildGraph
        // for the same source shapes, and the plain member-call chain binds.
        using var parser = new LabyrinthPatternParser(TestGrammarV2, "TestLangV2");
        var compiler = new LabyrinthExpressionCompiler();
        var sink = compiler.Compile("r", parser.ParsePattern("$X.DoWork()"));

        var (_, working) = Build("svc.DoWork();");
        var workingNode = Find(working, "FunctionCall", "DoWork");
        var context = new LabyrinthMatchContext();
        Assert.True(sink(workingNode, context));
        Assert.True(context.TryGet("X", out var bound));
        Assert.Equal("Identifier", bound.NodeType);
        Assert.Equal("svc", bound.Name);

        var (_, safe) = Build("svc?.DoWork();");
        var safeNode = Find(safe, "FunctionCall", "DoWork");
        Assert.False(sink(safeNode, new LabyrinthMatchContext()), "plain '.' sink must not fire on a safe deref");
    }
}
