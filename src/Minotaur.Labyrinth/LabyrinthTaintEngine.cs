using System.Collections.Concurrent;

namespace Minotaur.Labyrinth;

/// <summary>
/// Labyrinth Layer 3 (issue #103): the native taint analysis execution engine.
/// <para>
/// A fixed-point worklist algorithm operating directly on node references of
/// the <see cref="ILabyrinthDataFlowGraph"/> — zero serialization. Every
/// source/sink/sanitizer/propagator matcher is a delegate compiled by
/// <see cref="LabyrinthExpressionCompiler"/> (Layer 2), so the hot traversal
/// loop is pure memory accesses and compiled calls.
/// </para>
/// <para>
/// Execution flow (docs/SAST Rule Language Evaluation.md §5.3):
/// <list type="number">
/// <item><b>Source identification</b>: every node is tested against the compiled
/// source matchers; each match instantiates a <see cref="LabyrinthTaintBindings"/>
/// state (carrying the metavariables bound at the source) and seeds the worklist.</item>
/// <item><b>Propagation</b>: nodes are dequeued and every DFG edge out of the
/// node transfers taint to the successor.</item>
/// <item><b>Sanitizer evaluation</b>: a successor that matches a sanitizer (with
/// the taint's bindings unified) prunes the traversal along that path.</item>
/// <item><b>Propagator application</b>: a successor that matches a propagator
/// additionally transfers the taint state to the propagator's <c>to</c> target,
/// with the pattern's metavariable bindings merged in.</item>
/// <item><b>Sink evaluation</b>: a successor carrying taint that matches a sink
/// confirms a vulnerability and records the full source→sink path.</item>
/// <item><b>Fixed-point re-evaluation</b>: a node whose state gained a new taint
/// origin or new bindings is re-enqueued; the loop terminates when the worklist
/// is empty — so loops, mutual recursion and complex control flow are evaluated
/// to equilibrium without the caller bounding the iteration count.</item>
/// </list>
/// </para>
/// </summary>
public sealed class LabyrinthTaintEngine
{
    /// <summary>
    /// Per-node taint state: for every taint origin (the source node where the
    /// taint entered), the shortest path seen so far — as a linked list of
    /// path steps, so a long flow chain costs one step per node, not one
    /// O(n) list per node — and the unified metavariable bindings carried
    /// along it.
    /// </summary>
    private sealed class NodeTaintState
    {
        public Dictionary<ILabyrinthMatchNode, LabyrinthTaintPathStep> Paths { get; } = new();

        public Dictionary<ILabyrinthMatchNode, LabyrinthTaintBindings> Bindings { get; } = new();
    }

    /// <summary>
    /// One step of a source→sink path: the node reached and the step that
    /// led to it. The full path is materialized only when a finding is
    /// reported, keeping traversal memory O(nodes), not O(path²).
    /// </summary>
    private sealed class LabyrinthTaintPathStep
    {
        public required ILabyrinthMatchNode Node { get; init; }

        public LabyrinthTaintPathStep? Parent { get; init; }

        public List<ILabyrinthMatchNode> Materialize()
        {
            var list = new List<ILabyrinthMatchNode>();
            for (var step = this; step is not null; step = step.Parent)
            {
                list.Add(step.Node);
            }

            list.Reverse();
            return list;
        }
    }

    private readonly LabyrinthCompiledMatchers _matchers;
    private readonly ILabyrinthDataFlowGraph _graph;

    /// <summary>Creates an engine for one compiled taint rule and one graph.</summary>
    /// <param name="matchers">The compiled rule (Layer 2 output). Taint rules use sources/sinks/sanitizers/propagators.</param>
    /// <param name="graph">The data-flow graph to analyze.</param>
    /// <param name="searchOnlyIsError">
    /// When true (default), constructing the engine for a non-taint rule throws.
    /// The taint engine only consumes taint rules.
    /// </param>
    public LabyrinthTaintEngine(LabyrinthCompiledMatchers matchers, ILabyrinthDataFlowGraph graph, bool searchOnlyIsError = true)
    {
        ArgumentNullException.ThrowIfNull(matchers);
        ArgumentNullException.ThrowIfNull(graph);
        if (searchOnlyIsError && matchers.Rule.TypeValue != LabyrinthRuleType.Taint)
        {
            throw new LabyrinthRuleException(
                $"Rule '{matchers.Rule.Id}' is a '{matchers.Rule.Type}' rule; the taint engine analyzes taint rules only.");
        }

        _matchers = matchers;
        _graph = graph;
    }

    /// <summary>
    /// Runs the fixed-point taint analysis to completion and returns every
    /// confirmed finding. The same engine can be reused on the same graph
    /// (each run starts from fresh taint states).
    /// </summary>
    public IReadOnlyList<LabyrinthTaintFinding> Analyze() => Analyze(CancellationToken.None);

    /// <summary>Runs the analysis with cooperative cancellation.</summary>
    public IReadOnlyList<LabyrinthTaintFinding> Analyze(CancellationToken cancellationToken)
    {
        var states = new Dictionary<ILabyrinthMatchNode, NodeTaintState>();
        var worklist = new ConcurrentQueue<ILabyrinthMatchNode>();
        var findings = new List<LabyrinthTaintFinding>();
        var reported = new HashSet<(ILabyrinthMatchNode Source, ILabyrinthMatchNode Sink)>();
        var context = new LabyrinthMatchContext(); // reused; cleared before each matcher call

        SeedSources(states, worklist, context);

        while (worklist.TryDequeue(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentState = states[current];

            foreach (var successor in _graph.Successors(current))
            {
                if (ReferenceEquals(successor, current))
                {
                    continue; // a self-loop cannot change the state
                }

                foreach (var (origin, incomingPath, incomingBindings) in Entries(currentState))
                {
                    // 1. Sanitizer evaluation: prune the path here.
                    if (MatchesAny(_matchers.Sanitizers, successor, incomingBindings, context))
                    {
                        continue;
                    }

                    // 2. Transfer the taint state along the DFG edge.
                    var path = new LabyrinthTaintPathStep { Node = successor, Parent = incomingPath };

                    var changed = Merge(states, worklist, successor, origin, path, incomingBindings.Clone());
                    if (!changed)
                    {
                        continue;
                    }

                    // 3. Sink evaluation: confirm the vulnerability once per
                    //    (source, sink) pair; the first (shortest) path wins.
                    if (MatchesAny(_matchers.Sinks, successor, incomingBindings, context)
                        && reported.Add((origin, successor)))
                    {
                        findings.Add(new LabyrinthTaintFinding(
                            _matchers.Rule.Id,
                            _matchers.Rule.SeverityValue,
                            _matchers.Rule.EffectiveMessage,
                            origin,
                            successor,
                            states[successor].Paths[origin].Materialize()));
                    }

                    // 4. Propagator application: also transfer/duplicate the
                    //    state to the propagator's 'to' target.
                    ApplyPropagators(states, worklist, context, successor, origin, path, incomingBindings);
                }
            }
        }

        return findings;
    }

    private void SeedSources(
        Dictionary<ILabyrinthMatchNode, NodeTaintState> states,
        ConcurrentQueue<ILabyrinthMatchNode> worklist,
        LabyrinthMatchContext context)
    {
        foreach (var node in _graph.Nodes)
        {
            context.Clear();
            var bindings = new LabyrinthTaintBindings();
            if (!MatchesAny(_matchers.Sources, node, bindings, context))
            {
                continue;
            }

            // The source match bound its metavariables into the context; carry
            // them along the traversal as the unification dictionary.
            CaptureContext(context, ref bindings);
            Merge(states, worklist, node, node, new LabyrinthTaintPathStep { Node = node }, bindings);
        }
    }

    private void ApplyPropagators(
        Dictionary<ILabyrinthMatchNode, NodeTaintState> states,
        ConcurrentQueue<ILabyrinthMatchNode> worklist,
        LabyrinthMatchContext context,
        ILabyrinthMatchNode propagatorNode,
        ILabyrinthMatchNode origin,
        LabyrinthTaintPathStep path,
        LabyrinthTaintBindings incomingBindings)
    {
        foreach (var propagator in _matchers.Propagators)
        {
            context.Clear();
            if (!SeedAndMatch(propagator.Matcher, propagatorNode, incomingBindings, context))
            {
                continue;
            }

            if (!context.TryGet(propagator.To, out var target) || ReferenceEquals(target, propagatorNode))
            {
                continue;
            }

            // Duplicate the state to the propagator's target, with the
            // propagator pattern's own bindings (e.g. $TARGET) merged in.
            var transferred = incomingBindings.Clone();
            CaptureContext(context, ref transferred);
            var transferredPath = new LabyrinthTaintPathStep { Node = target, Parent = path };
            Merge(states, worklist, target, origin, transferredPath, transferred);
        }
    }

    private static IEnumerable<(ILabyrinthMatchNode Origin, LabyrinthTaintPathStep Path, LabyrinthTaintBindings Bindings)> Entries(
        NodeTaintState state)
    {
        foreach (var origin in state.Paths.Keys)
        {
            yield return (origin, state.Paths[origin], state.Bindings[origin]);
        }
    }

    private static bool Merge(
        Dictionary<ILabyrinthMatchNode, NodeTaintState> states,
        ConcurrentQueue<ILabyrinthMatchNode> worklist,
        ILabyrinthMatchNode node,
        ILabyrinthMatchNode origin,
        LabyrinthTaintPathStep path,
        LabyrinthTaintBindings bindings)
    {
        if (!states.TryGetValue(node, out var state))
        {
            state = new NodeTaintState();
            states[node] = state;
        }

        if (!state.Paths.ContainsKey(origin))
        {
            // New taint origin at this node: state changed, re-evaluate.
            state.Paths[origin] = path;
            state.Bindings[origin] = bindings;
            worklist.Enqueue(node);
            return true;
        }

        // The shortest path (arrives first, worklist is FIFO) is kept;
        // monotone binding additions still re-enqueue the node.
        var changed = false;
        var existingBindings = state.Bindings[origin];
        for (var i = 0; i < bindings.Count; i++)
        {
            var (name, value) = bindings[i];
            if (!existingBindings.TryGet(name, out var already))
            {
                existingBindings.TryBind(name, value);
                changed = true; // a new metavariable binding: state grew
            }
            // Already bound to the same node: no change. Bound to a
            // different node: unification conflict, the carried binding
            // does not apply on this path — keep the existing one.
        }

        if (changed)
        {
            worklist.Enqueue(node);
        }

        return changed;
    }

    private static bool MatchesAny(
        IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> matchers,
        ILabyrinthMatchNode node,
        LabyrinthTaintBindings bindings,
        LabyrinthMatchContext context)
    {
        foreach (var matcher in matchers)
        {
            context.Clear();
            if (SeedAndMatch(matcher, node, bindings, context))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Runs a compiled matcher against <paramref name="node"/> with the taint's
    /// current bindings pre-seeded into the context, so metavariable unification
    /// holds across the whole rule (a $DATA bound at the source only matches a
    /// sink binding the very same node).
    /// </summary>
    private static bool SeedAndMatch(
        Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> matcher,
        ILabyrinthMatchNode node,
        LabyrinthTaintBindings bindings,
        LabyrinthMatchContext context)
    {
        for (var i = 0; i < bindings.Count; i++)
        {
            var (name, value) = bindings[i];
            if (!context.TryBind(name, value))
            {
                return false; // unification conflict with the carried state
            }
        }

        return matcher(node, context);
    }

    /// <summary>Copies context bindings into the value-type bindings struct.</summary>
    private static void CaptureContext(LabyrinthMatchContext context, ref LabyrinthTaintBindings bindings)
    {
        foreach (var (name, node) in context.Bindings)
        {
            bindings.TryBind(name, node);
        }
    }
}
