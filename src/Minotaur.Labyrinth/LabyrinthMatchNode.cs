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
/// <item><c>FunctionCall</c> — a call with <see cref="Name"/> = callee name and <see cref="Arguments"/> = argument nodes.</item>
/// <item><c>Assignment</c> — <see cref="Arguments"/>[0] = left-hand side, <see cref="Arguments"/>[1] = assigned expression.</item>
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

    /// <summary>Creates a match node.</summary>
    /// <param name="nodeType">The semantic node type (e.g. <c>FunctionCall</c>).</param>
    /// <param name="name">The identifier text, if any.</param>
    /// <param name="arguments">The child nodes, in positional order.</param>
    public LabyrinthMatchNode(string nodeType, string? name = null, IReadOnlyList<ILabyrinthMatchNode>? arguments = null)
    {
        NodeType = nodeType;
        Name = name;
        Arguments = arguments ?? Array.Empty<ILabyrinthMatchNode>();
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
