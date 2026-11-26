namespace Minotaur.Labyrinth;

/// <summary>
/// The node contract Labyrinth pattern matchers compile against (issue #102).
/// <para>
/// The CognitiveGraph accessors (<c>SymbolNode</c> and friends) are
/// <see langword="ref"/> <c>struct</c>s over the graph buffer and therefore
/// cannot be generic type arguments of compiled delegates. Labyrinth instead
/// defines this small adapter contract; the traversal layer (#103)
/// materializes match nodes from graph nodes.
/// </para>
/// <para>
/// Node type conventions (Labyrinth matching model):
/// <list type="bullet">
/// <item><c>FunctionCall</c> — a call with <see cref="Name"/> = callee name and <see cref="Arguments"/> = argument nodes;
/// the receiver of a member call (<c>x.DoWork()</c>) is exposed via <see cref="Receiver"/> (pattern v2, issue #247).</item>
/// <item><c>Assignment</c> — <see cref="Arguments"/>[0] = left-hand side, <see cref="Arguments"/>[1] = assigned expression.</item>
/// <item><c>IndexAccess</c> — an indexing operation <c>a[i]</c>: <see cref="Receiver"/> = the array expression
/// node, <see cref="Arguments"/> = the index expressions (pattern v2, issue #247).</item>
/// <item>Binary operators (<c>/</c>, <c>%</c>, <c>as</c>, <c>??</c>) materialize as <c>FunctionCall</c> nodes with
/// <see cref="Name"/> = <c>op:divide</c>, <c>op:modulo</c>, <c>op:as</c>, <c>op:coalesce</c> and the two operands
/// as <see cref="Arguments"/> (pattern v2, issue #247).</item>
/// </list>
/// </para>
/// </summary>
public interface ILabyrinthMatchNode
{
    /// <summary>The semantic node type (e.g. <c>FunctionCall</c>, <c>Assignment</c>, <c>Identifier</c>).</summary>
    string NodeType { get; }

    /// <summary>The node's identifier text (callee name, identifier literal), or null when the node has none.</summary>
    string? Name { get; }

    /// <summary>The child nodes in positional order (arguments, assignment sides).</summary>
    IReadOnlyList<ILabyrinthMatchNode> Arguments { get; }

    /// <summary>
    /// The receiver of a member access: the <c>x</c> in <c>x.DoWork()</c>, the array in <c>a[i]</c>.
    /// Null when the node has no receiver (pattern v2, issue #247). Default interface member:
    /// existing implementations keep compiling unchanged and simply expose no receiver.
    /// </summary>
    ILabyrinthMatchNode? Receiver => null;

    /// <summary>
    /// True when the access is null-conditional (<c>x?.DoWork()</c>), i.e. the deref itself is
    /// safe (pattern v2, issue #247). Default interface member: plain accesses report false.
    /// </summary>
    bool IsNullConditional => false;
}

/// <summary>
/// A simple default implementation of <see cref="ILabyrinthMatchNode"/> for
/// tests and for adapters that materialize nodes from a parsed graph.
/// </summary>
public sealed class LabyrinthMatchNode : ILabyrinthMatchNode
{
    /// <inheritdoc/>
    public string NodeType { get; }

    /// <inheritdoc/>
    public string? Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ILabyrinthMatchNode> Arguments { get; }

    /// <inheritdoc/>
    public ILabyrinthMatchNode? Receiver { get; }

    /// <inheritdoc/>
    public bool IsNullConditional { get; }

    /// <summary>Creates a match node.</summary>
    /// <param name="nodeType">The semantic node type (e.g. <c>FunctionCall</c>).</param>
    /// <param name="name">The identifier text, if any.</param>
    /// <param name="arguments">The child nodes, in positional order.</param>
    /// <param name="receiver">The receiver node for member accesses / indexing, if any (pattern v2).</param>
    /// <param name="isNullConditional">True when the access is null-conditional (<c>?.</c>, pattern v2).</param>
    public LabyrinthMatchNode(string nodeType, string? name = null, IReadOnlyList<ILabyrinthMatchNode>? arguments = null,
        ILabyrinthMatchNode? receiver = null, bool isNullConditional = false)
    {
        NodeType = nodeType;
        Name = name;
        Arguments = arguments ?? Array.Empty<ILabyrinthMatchNode>();
        Receiver = receiver;
        IsNullConditional = isNullConditional;
    }
}

/// <summary>
/// Metavariable bindings for a single rule evaluation: the unification scope
/// of a Labyrinth rule is the whole rule, so matchers receive one shared
/// context and <see cref="TryBind"/> enforces that the same metavariable binds
/// the same node everywhere it occurs.
/// </summary>
public sealed class LabyrinthMatchContext
{
    private readonly Dictionary<string, ILabyrinthMatchNode> _bindings = new(StringComparer.Ordinal);

    /// <summary>The current bindings, by metavariable name (without the leading <c>$</c>).</summary>
    public IReadOnlyDictionary<string, ILabyrinthMatchNode> Bindings => _bindings;

    /// <summary>
    /// Optional child-to-parent map over the materialized match-node tree, set once
    /// per scan by the host (see <see cref="BuildParentMap"/>). Enables
    /// <c>pattern-inside</c> evaluation via <see cref="AncestorsOf"/>. Not cleared
    /// by <see cref="Clear"/>: it is per-scan state, not per-match state.
    /// </summary>
    public IReadOnlyDictionary<ILabyrinthMatchNode, ILabyrinthMatchNode?>? ParentMap { get; set; }

    /// <summary>
    /// The ancestors of <paramref name="node"/> (nearest first) according to
    /// <see cref="ParentMap"/>. Empty when no parent map is set.
    /// </summary>
    public IEnumerable<ILabyrinthMatchNode> AncestorsOf(ILabyrinthMatchNode node)
    {
        if (ParentMap is null)
        {
            yield break;
        }

        var seen = new HashSet<ILabyrinthMatchNode>(ReferenceEqualityComparer.Instance);
        var current = node;
        while (ParentMap.TryGetValue(current, out var parent) && parent is not null && seen.Add(parent))
        {
            yield return parent;
            current = parent;
        }
    }

    /// <summary>
    /// Builds a child-to-parent map over the match-node tree reachable from
    /// <paramref name="nodes"/> by walking <see cref="ILabyrinthMatchNode.Receiver"/>
    /// and <see cref="ILabyrinthMatchNode.Arguments"/>. Pass every materialized node
    /// (not just roots): each node then contributes its own children as entries.
    /// </summary>
    public static IReadOnlyDictionary<ILabyrinthMatchNode, ILabyrinthMatchNode?> BuildParentMap(
        IEnumerable<ILabyrinthMatchNode> nodes)
    {
        var map = new Dictionary<ILabyrinthMatchNode, ILabyrinthMatchNode?>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
        {
            if (node.Receiver is { } receiver)
            {
                map[receiver] = node;
            }

            foreach (var argument in node.Arguments)
            {
                map[argument] = node;
            }
        }

        return map;
    }

    /// <summary>
    /// Binds <paramref name="metavariable"/> to <paramref name="node"/>, or — when
    /// the metavariable is already bound — succeeds only if it is bound to the
    /// very same node (unification: one metavariable, one capture per rule).
    /// </summary>
    public bool TryBind(string metavariable, ILabyrinthMatchNode node)
    {
        if (_bindings.TryGetValue(metavariable, out var existing))
        {
            return ReferenceEquals(existing, node);
        }

        _bindings[metavariable] = node;
        return true;
    }

    /// <summary>Looks up an existing binding.</summary>
    public bool TryGet(string metavariable, out ILabyrinthMatchNode node) => _bindings.TryGetValue(metavariable, out node!);

    /// <summary>Clears all bindings, returning the context to a fresh state.</summary>
    public void Clear() => _bindings.Clear();
}
