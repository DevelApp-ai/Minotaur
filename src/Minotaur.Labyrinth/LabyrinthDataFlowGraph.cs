namespace Minotaur.Labyrinth;

/// <summary>
/// The data-flow view of the code under analysis, consumed by the taint
/// traversal engine (Labyrinth Layer 3, issue #103).
/// <para>
/// The traversal operates directly on node references — no serialization,
/// no copying: the host materializes <see cref="ILabyrinthMatchNode"/>s from
/// the CognitiveGraph (or any parse result) and exposes the Data Flow Graph
/// edges through <see cref="Successors"/>. Interprocedural flow is supported
/// simply by including interprocedural edges (call-site → callee body,
/// return → call-site continuation) in the successor set.
/// </para>
/// </summary>
public interface ILabyrinthDataFlowGraph
{
    /// <summary>All nodes of the graph, in any order (source identification iterates them).</summary>
    IEnumerable<ILabyrinthMatchNode> Nodes { get; }

    /// <summary>
    /// The data-flow successors of <paramref name="node"/>: every node taint
    /// arriving at <paramref name="node"/> can flow to next.
    /// </summary>
    IEnumerable<ILabyrinthMatchNode> Successors(ILabyrinthMatchNode node);
}

/// <summary>
/// A simple adjacency-list implementation of <see cref="ILabyrinthDataFlowGraph"/>
/// for tests and for adapters that build the data-flow view up front.
/// Successor sets are stored per node reference.
/// </summary>
public sealed class LabyrinthDataFlowGraph : ILabyrinthDataFlowGraph
{
    private readonly List<ILabyrinthMatchNode> _nodes = new();
    private readonly Dictionary<ILabyrinthMatchNode, List<ILabyrinthMatchNode>> _successors = new();

    /// <inheritdoc/>
    public IEnumerable<ILabyrinthMatchNode> Nodes => _nodes;

    /// <summary>Registers a node with the graph (idempotent).</summary>
    public LabyrinthDataFlowGraph AddNode(ILabyrinthMatchNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_successors.ContainsKey(node))
        {
            _nodes.Add(node);
            _successors[node] = new List<ILabyrinthMatchNode>();
        }

        return this;
    }

    /// <summary>Registers a node and its data-flow successors.</summary>
    public LabyrinthDataFlowGraph AddNode(ILabyrinthMatchNode node, params ILabyrinthMatchNode[] successors)
    {
        AddNode(node);
        var list = _successors[node];
        foreach (var successor in successors)
        {
            ArgumentNullException.ThrowIfNull(successor, nameof(successors));
            AddNode(successor);
            if (!list.Contains(successor))
            {
                list.Add(successor);
            }
        }

        return this;
    }

    /// <summary>Connects two already-registered nodes.</summary>
    public LabyrinthDataFlowGraph Connect(ILabyrinthMatchNode from, ILabyrinthMatchNode to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        AddNode(from);
        AddNode(to);
        var list = _successors[from];
        if (!list.Contains(to))
        {
            list.Add(to);
        }

        return this;
    }

    /// <inheritdoc/>
    public IEnumerable<ILabyrinthMatchNode> Successors(ILabyrinthMatchNode node) =>
        _successors.TryGetValue(node, out var list) ? list : Array.Empty<ILabyrinthMatchNode>();
}
