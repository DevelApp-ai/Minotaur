using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace Minotaur.Labyrinth;

/// <summary>
/// A compiled taint propagator matcher: a compiled pattern delegate plus the
/// metavariable names taint state flows from (<see cref="From"/>) and to
/// (<see cref="To"/>), consumed by the traversal layer (#103).
/// </summary>
public sealed class LabyrinthCompiledPropagator
{
    /// <summary>The compiled pattern matcher for the propagator's assignment pattern.</summary>
    public Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> Matcher { get; }

    /// <summary>The metavariable taint state flows from.</summary>
    public string From { get; }

    /// <summary>The metavariable taint state flows to.</summary>
    public string To { get; }

    internal LabyrinthCompiledPropagator(Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> matcher, string from, string to)
    {
        Matcher = matcher;
        From = from;
        To = to;
    }
}

/// <summary>
/// A Labyrinth rule with every pattern entry compiled to a native delegate
/// (issue #102, Layer 2). Feeding a <see cref="ILabyrinthMatchNode"/> plus a
/// fresh <see cref="LabyrinthMatchContext"/> to any matcher runs compiled code —
/// no interpretation of the pattern at match time.
/// </summary>
public sealed class LabyrinthCompiledMatchers
{
    /// <summary>The originating rule.</summary>
    public LabyrinthRule Rule { get; }

    /// <summary>The compiled search pattern (search rules only).</summary>
    public Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>? SearchPattern { get; }

    /// <summary>The compiled taint source matchers.</summary>
    public IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> Sources { get; }

    /// <summary>The compiled taint sink matchers.</summary>
    public IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> Sinks { get; }

    /// <summary>The compiled taint sanitizer matchers.</summary>
    public IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> Sanitizers { get; }

    /// <summary>The compiled taint propagators.</summary>
    public IReadOnlyList<LabyrinthCompiledPropagator> Propagators { get; }

    internal LabyrinthCompiledMatchers(
        LabyrinthRule rule,
        Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>? searchPattern,
        IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> sources,
        IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> sinks,
        IReadOnlyList<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> sanitizers,
        IReadOnlyList<LabyrinthCompiledPropagator> propagators)
    {
        Rule = rule;
        SearchPattern = searchPattern;
        Sources = sources;
        Sinks = sinks;
        Sanitizers = sanitizers;
        Propagators = propagators;
    }
}

/// <summary>
/// Compiles Labyrinth pattern mini-ASTs into C# Expression Trees
/// (System.Linq.Expressions) and further into native delegates at engine
/// initialization — no runtime interpretation (issue #102).
/// <para>
/// Supported pattern shapes (v2, issue #247 — adds receiver binding, index
/// accesses, binary operators and non-call assignment right-hand sides; all v1
/// shapes keep their meaning):
/// <list type="bullet">
/// <item>Call: <c>ReceiveData($DATA)</c> ⇒ <c>node.NodeType == "FunctionCall" &amp;&amp; node.Name == "ReceiveData" &amp;&amp; …args</c></item>
/// <item>Member call with receiver binding: <c>$X.DoWork()</c> ⇒ <c>FunctionCall</c> with Name = <c>DoWork</c>,
/// binding <c>$X</c> to <c>node.Receiver</c> (plain <c>.</c> requires a non-null-conditional access;
/// <c>$X?.DoWork()</c> matches only null-conditional accesses).</item>
/// <item>Index access: <c>$ARR[$I]</c> ⇒ <c>IndexAccess</c> node, binding <c>$ARR</c> to <c>node.Receiver</c>
/// (the indexed expression) and matching the index expressions positionally.</item>
/// <item>Binary operator: <c>$A / $Z</c>, <c>$A % $Z</c>, <c>$A as $B</c>, <c>$A ?? $B</c> ⇒ a
/// <c>FunctionCall</c> named <c>op:divide</c>/<c>op:modulo</c>/<c>op:as</c>/<c>op:coalesce</c> with the two
/// operands as arguments (left-associative: <c>$A / $B / $C</c> matches <c>(($A / $B) / $C)</c>).</item>
/// <item>Assignment (any of the above, a bare call or a bare metavariable/identifier on the right):
/// <c>$TARGET = FormatString($SRC)</c>, <c>$TARGET = $DATA</c>, <c>$T = $A as $B</c> ⇒ matches
/// <c>Assignment</c> nodes, binding both sides.</item>
/// <item>Bare metavariable <c>$DATA</c> (binds anything) or bare identifier <c>Require</c> (name equality).</item>
/// </list>
/// <para>
/// Any pattern may carry an optional Dynamic LINQ condition (issue #104):
/// a string-based C# logical expression over the matched node (parameter
/// name <c>node</c>), parsed with System.Linq.Dynamic.Core at initialization
/// and appended to the pattern expression tree before compilation, so the
/// condition runs as compiled code inside the same delegate.
/// </para>
/// Argument lists support metavariables, ellipsis (<c>...</c> = zero or more
/// arguments) and identifier/number/string literals. Arguments between two
/// ellipses (<c>ExecuteAction(..., $DATA, ...)</c>) bind to any argument via
/// an <c>Enumerable.Any</c> term.
/// </para>
/// <para>
/// Delegates are cached keyed by rule id + pattern hash, so compiling the
/// same rule repeatedly (e.g. per file scanned) returns the same delegate.
/// </para>
/// </summary>
public sealed class LabyrinthExpressionCompiler
{
    private const string FunctionCallNodeType = "FunctionCall";
    private const string AssignmentNodeType = "Assignment";
    private const string IndexAccessNodeType = "IndexAccess";

    /// <summary>
    /// Binary operators materialized as op: FunctionCall nodes (pattern v2, issue #247):
    /// pattern token value → materialized callee name.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> OperatorNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/"] = "op:divide",
            ["%"] = "op:modulo",
            ["as"] = "op:as",
            ["??"] = "op:coalesce"
        };

    /// <summary>
    /// Literal token values that are never identifier operands. Pattern shape
    /// detection is value-based so the same shapes compile for every target
    /// grammar regardless of how the grammar names its token rules
    /// (e.g. <c>EQUALS</c> vs <c>OP_ASSIGN</c> for <c>=</c>).
    /// </summary>
    private static readonly HashSet<string> NonIdentifierValues = new(StringComparer.Ordinal)
    {
        "(", ")", "[", "]", ",", "=", "/", "%", "?", "??", ".", "as"
    };

    private static readonly System.Reflection.MethodInfo TryBindMethod =
        typeof(LabyrinthMatchContext).GetMethod(nameof(LabyrinthMatchContext.TryBind),
            new[] { typeof(string), typeof(ILabyrinthMatchNode) })
        ?? throw new InvalidOperationException("TryBind is missing.");

    // Count lives on IReadOnlyCollection<T> and the indexer on IReadOnlyList<T>;
    // Expression.Property(name) does not search interface inheritance, so the
    // property infos are resolved explicitly.
    private static readonly System.Reflection.PropertyInfo CountProperty =
        typeof(IReadOnlyCollection<ILabyrinthMatchNode>).GetProperty(nameof(IReadOnlyCollection<ILabyrinthMatchNode>.Count))
        ?? throw new InvalidOperationException("Count property is missing.");

    private static readonly System.Reflection.PropertyInfo ItemProperty =
        typeof(IReadOnlyList<ILabyrinthMatchNode>).GetProperty("Item")
        ?? throw new InvalidOperationException("Item property is missing.");

    private readonly ConcurrentDictionary<string, Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>> _cache = new();

    /// <summary>
    /// Compiles every pattern of a compiled rule into native delegates,
    /// using the cache to reuse delegates for already-seen rule patterns.
    /// </summary>
    /// <param name="rule">The rule (Layer 1 output, see <see cref="LabyrinthRuleCompiler"/>).</param>
    /// <exception cref="LabyrinthRuleException">Thrown when a pattern has an unsupported shape.</exception>
    public LabyrinthCompiledMatchers CompileRule(CompiledLabyrinthRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var sources = rule.Sources.Select(e => CompileEntry(rule.Rule.Id, e)).ToArray();
        var sinks = rule.Sinks.Select(e => CompileEntry(rule.Rule.Id, e)).ToArray();
        var sanitizers = rule.Sanitizers.Select(e => CompileEntry(rule.Rule.Id, e)).ToArray();
        var propagators = rule.Propagators
            .Select(p => new LabyrinthCompiledPropagator(Compile(rule.Rule.Id, p.Pattern, p.Condition), p.From, p.To))
            .ToArray();

        var search = rule.SearchPattern is null
            ? null
            : CompileAlternative(rule.Rule.Id, rule.SearchPattern, rule.SearchCondition,
                rule.SearchNotPatterns, rule.SearchInsidePatterns);

        return new LabyrinthCompiledMatchers(rule.Rule, search, sources, sinks, sanitizers, propagators);
    }

    /// <summary>
    /// Compile one pattern entry into a matcher. A <c>pattern-either</c> entry compiles
    /// to the OR of its alternatives (any alternative matching makes the entry match);
    /// the entry condition applies to every alternative unless the alternative has
    /// its own condition.
    /// </summary>
    private Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> CompileEntry(string ruleId, CompiledLabyrinthPatternEntry entry)
    {
        var compiled = entry.Alternatives
            .Select(a => CompileAlternative(ruleId, a.Pattern, a.Condition ?? entry.Condition, a.NotPatterns, a.InsidePatterns))
            .ToArray();
        if (compiled.Length == 1)
        {
            return compiled[0];
        }

        return (node, ctx) =>
        {
            foreach (var f in compiled)
            {
                if (f(node, ctx))
                {
                    return true;
                }
            }

            return false;
        };
    }

    /// <summary>
    /// Compile one alternative (positive pattern + optional condition + optional
    /// pattern-not / pattern-inside filters) into a single matcher. The filters wrap
    /// the positive delegate: <c>pattern-not</c> fails the alternative when it matches
    /// the same node; <c>pattern-inside</c> requires some ancestor of the node to match.
    /// Filter patterns run on a scratch context so their metavariables never leak into
    /// the entry's unification scope (v1 simplification).
    /// </summary>
    private Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> CompileAlternative(
        string ruleId,
        LabyrinthPatternAst pattern,
        string? condition,
        IReadOnlyList<LabyrinthPatternAst> notPatterns,
        IReadOnlyList<LabyrinthPatternAst> insidePatterns)
    {
        var positive = Compile(ruleId, pattern, condition);
        if (notPatterns.Count == 0 && insidePatterns.Count == 0)
        {
            return positive;
        }

        var nots = notPatterns.Select(p => Compile(ruleId, p, null)).ToArray();
        var insides = insidePatterns.Select(p => Compile(ruleId, p, null)).ToArray();

        return (node, ctx) =>
        {
            if (!positive(node, ctx))
            {
                return false;
            }

            foreach (var not in nots)
            {
                if (not(node, new LabyrinthMatchContext()))
                {
                    return false;
                }
            }

            foreach (var inside in insides)
            {
                var found = false;
                foreach (var ancestor in ctx.AncestorsOf(node))
                {
                    if (inside(ancestor, new LabyrinthMatchContext()))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        };
    }

    /// <summary>
    /// Compiles one pattern into a native delegate, caching by rule id and
    /// pattern hash.
    /// </summary>
    /// <exception cref="LabyrinthRuleException">Thrown when the pattern has an unsupported shape.</exception>
    public Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> Compile(string ruleId, LabyrinthPatternAst pattern)
        => Compile(ruleId, pattern, condition: null);

    /// <summary>
    /// Compiles one pattern with an optional Dynamic LINQ condition (issue #104)
    /// into a single native delegate: the condition expression is appended to
    /// the pattern expression tree before compilation, so both run inside one
    /// compiled call. Delegates are cached by rule id, pattern hash and
    /// condition hash.
    /// </summary>
    /// <exception cref="LabyrinthRuleException">
    /// Thrown when the pattern has an unsupported shape, or when the condition
    /// cannot be parsed by Dynamic LINQ (fail fast at initialization).
    /// </exception>
    public Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> Compile(string ruleId, LabyrinthPatternAst pattern, string? condition)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var key = ruleId + "-" + PatternHash(pattern.Pattern + "|" + (condition ?? string.Empty));
        return _cache.GetOrAdd(key, _ => BuildMatcher(pattern, condition));
    }

    /// <summary>The number of distinct compiled delegates currently cached.</summary>
    public int CachedPatternCount => _cache.Count;

    private static string PatternHash(string pattern)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(pattern));
        return Convert.ToHexString(hash, 0, 8);
    }

    private static Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool> BuildMatcher(LabyrinthPatternAst pattern, string? condition)
    {
        var node = Expression.Parameter(typeof(ILabyrinthMatchNode), "node");
        var ctx = Expression.Parameter(typeof(LabyrinthMatchContext), "ctx");

        var body = BuildBody(pattern, node, ctx);
        if (!string.IsNullOrWhiteSpace(condition))
        {
            body = AndAlso(body, ParseCondition(condition, node));
        }

        return Expression.Lambda<Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>>(body, node, ctx).Compile();
    }

    /// <summary>
    /// Parses a Dynamic LINQ condition (issue #104) into an expression tree
    /// over the matched node. Parsed at initialization — a malformed or
    /// unsupported condition fails fast with a clear rule error instead of a
    /// runtime crash mid-analysis.
    /// </summary>
    private static Expression ParseCondition(string condition, ParameterExpression node)
    {
        try
        {
            // The condition sees exactly one parameter: the matched node. It
            // cannot name types, assemblies or arbitrary static members —
            // Dynamic LINQ resolves only members of the node contract.
            var parsed = DynamicExpressionParser.ParseLambda(
                new ParsingConfig(),
                new[] { node },
                typeof(bool),
                condition);

            return parsed.Body;
        }
        catch (ParseException ex)
        {
            throw new LabyrinthRuleException(
                $"Invalid Dynamic LINQ condition '{condition}': {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            throw new LabyrinthRuleException(
                $"Unsupported Dynamic LINQ condition '{condition}': {ex.Message}", ex);
        }
    }

    private static Expression BuildBody(LabyrinthPatternAst pattern, ParameterExpression node, ParameterExpression ctx)
    {
        var nodes = pattern.Nodes;

        // Assignment pattern: "<lhs> = <rhs>" at the top level. Detection is
        // value-based (v2): "=" may be tokenized as EQUALS or as any operator
        // token rule, in whichever target grammar.
        var equalsIndex = FindTopLevelEquals(nodes);
        if (equalsIndex > 0 && equalsIndex < nodes.Count - 1)
        {
            if (equalsIndex > 1)
            {
                throw new LabyrinthRuleException(
                    $"Pattern '{pattern.Pattern}' has an unsupported assignment shape: " +
                    "only '<metavariable-or-identifier> = <rhs>' is supported.");
            }

            var lhs = nodes[0];
            var rhs = new LabyrinthPatternAst(pattern.Pattern, nodes.Skip(equalsIndex + 1).ToArray());
            return BuildAssignmentBody(lhs, rhs, node, ctx);
        }

        if (nodes.Count == 0)
        {
            throw new LabyrinthRuleException($"Pattern '{pattern.Pattern}' is empty.");
        }

        // Receiver-member call (v2): "$X.Method(...)", "list.Find($V)", "$X?.Method(...)".
        if (TrySplitReceiverCall(nodes, out var callShape))
        {
            return BuildReceiverCallBody(callShape, node, ctx);
        }

        // Index access (v2): "$ARR[$I]".
        if (TrySplitIndexAccess(nodes, out var indexShape))
        {
            return BuildIndexAccessBody(indexShape, node, ctx);
        }

        // Binary operator chain (v2): "$A / $Z", "$A as $B", "$A / $B / $C".
        if (TrySplitBinaryOperator(nodes, out var opShape))
        {
            return BuildOperatorChainBody(opShape, node, ctx);
        }

        // Bare pattern: a single metavariable or identifier.
        if (nodes.Count == 1)
        {
            return BuildSingleExpression(nodes[0], node, ctx);
        }

        // Call pattern: "Name(<args>)" (value-based identifier detection so
        // target grammars with different token rule names work too).
        if (IsIdentifierLiteral(nodes[0]) && IsOpenParen(nodes[1]) && IsCloseParen(nodes[^1]) && nodes.Count >= 3)
        {
            return BuildCallBody(nodes[0].Value, nodes.Skip(2).Take(nodes.Count - 3).ToList(), node, ctx);
        }

        throw new LabyrinthRuleException(
            $"Pattern '{pattern.Pattern}' has an unsupported shape for expression compilation. " +
            "Supported: call, member call with receiver ('$X.Method(...)'), index access ('$ARR[$I]'), " +
            "binary operators ('$A / $Z', '$A as $B', '$A ?? $B'), assignment ('<lhs> = <rhs>' with any of " +
            "those forms or a bare identifier/metavariable on the right), bare metavariable or bare identifier.");
    }

    private static Expression BuildSingleExpression(LabyrinthPatternNode single, ParameterExpression node, ParameterExpression ctx)
    {
        if (single.Kind == LabyrinthPatternNodeKind.Metavariable)
        {
            return Expression.Call(ctx, TryBindMethod, Expression.Constant(single.MetavariableName), node);
        }

        if (single.Kind == LabyrinthPatternNodeKind.Literal)
        {
            return EqualName(node, single.Value);
        }

        throw new LabyrinthRuleException(
            $"Pattern '{single.Value}' has an unsupported shape: a lone ellipsis matches nothing.");
    }

    private static Expression BuildAssignmentBody(
        LabyrinthPatternNode lhs, LabyrinthPatternAst rhs, ParameterExpression node, ParameterExpression ctx)
    {
        if (lhs.Kind == LabyrinthPatternNodeKind.Ellipsis)
        {
            throw new LabyrinthRuleException("Assignment left-hand side cannot be an ellipsis.");
        }

        Expression body = Expression.Equal(
            Expression.Property(node, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(AssignmentNodeType));
        body = AndAlso(body, Expression.Equal(ArgumentsCount(node), Expression.Constant(2)));

        // Left-hand side: bind (metavariable) or compare (identifier literal).
        var left = IndexArgumentAt(node, 0);
        body = AndAlso(body, lhs.Kind == LabyrinthPatternNodeKind.Metavariable
            ? Expression.Call(ctx, TryBindMethod, Expression.Constant(lhs.MetavariableName), left)
            : EqualName(left, lhs.Value));

        // Right-hand side (v2): any supported expression shape — a call, a
        // member call with receiver, an index access, a binary operator chain,
        // or a bare metavariable/identifier.
        var right = IndexArgumentAt(node, 1);
        var rhsNodes = rhs.Nodes;
        if (rhsNodes.Count == 0)
        {
            throw new LabyrinthRuleException(
                $"Assignment right-hand side '{rhs.Pattern}' is empty.");
        }

        if (rhsNodes.Count == 1)
        {
            return AndAlso(body, BuildOperand(rhsNodes[0], right, ctx));
        }

        if (rhsNodes.Count >= 3 && IsIdentifierLiteral(rhsNodes[0])
            && IsOpenParen(rhsNodes[1]) && IsCloseParen(rhsNodes[^1]))
        {
            var callBody = BuildCallBody(rhsNodes[0].Value, rhsNodes.Skip(2).Take(rhsNodes.Count - 3).ToList(), right, ctx);
            return AndAlso(body, callBody);
        }

        if (TrySplitReceiverCall(rhsNodes, out var callShape))
        {
            return AndAlso(body, BuildReceiverCallBody(callShape, right, ctx));
        }

        if (TrySplitIndexAccess(rhsNodes, out var indexShape))
        {
            return AndAlso(body, BuildIndexAccessBody(indexShape, right, ctx));
        }

        if (TrySplitBinaryOperator(rhsNodes, out var opShape))
        {
            return AndAlso(body, BuildOperatorChainBody(opShape, right, ctx));
        }

        throw new LabyrinthRuleException(
            $"Assignment right-hand side '{rhs.Pattern}' has an unsupported shape. Supported: " +
            "'<lhs> = Call(<args>)', '<lhs> = $X.Method(<args>)', '<lhs> = $ARR[$I]', " +
            "'<lhs> = $A / $B' (also %, as, ??), '<lhs> = $DATA', '<lhs> = <identifier>'.");
    }

    private static Expression BuildCallBody(
        string name, IReadOnlyList<LabyrinthPatternNode> argNodes, Expression callNode, ParameterExpression ctx)
    {
        Expression body = Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.Name)),
            Expression.Constant(name)));

        return AndAlso(body, BuildArgumentsBody(argNodes, callNode, ctx));
    }

    /// <summary>
    /// Matches a node's <see cref="ILabyrinthMatchNode.Arguments"/> against a
    /// pattern argument list: fixed arity when no ellipsis is present, prefix /
    /// middle / suffix segmentation otherwise. Shared by call, member-call and
    /// index-access patterns (the index expressions of an <c>IndexAccess</c>
    /// node live in the same argument slot).
    /// </summary>
    private static Expression BuildArgumentsBody(
        IReadOnlyList<LabyrinthPatternNode> argNodes, Expression callNode, ParameterExpression ctx)
    {
        var args = SplitArguments(argNodes);

        Expression body = Expression.Constant(true);

        var firstEllipsis = args.FindIndex(a => a.Kind == LabyrinthPatternNodeKind.Ellipsis);
        if (firstEllipsis < 0)
        {
            // Fixed arity: count must match exactly, then match positionally.
            body = AndAlso(body, Expression.Equal(ArgumentsCount(callNode), Expression.Constant(args.Count)));
            for (var i = 0; i < args.Count; i++)
            {
                body = AndAlso(body, BuildArgExpression(args[i], IndexArgumentAt(callNode, i), ctx));
            }

            return body;
        }

        // Ellipsis present: prefix before the first ellipsis, suffix after the
        // last; argument nodes strictly between the ellipses bind via Any(...).
        var lastEllipsis = args.FindLastIndex(a => a.Kind == LabyrinthPatternNodeKind.Ellipsis);
        var prefix = args.Take(firstEllipsis).ToList();
        var middle = args.Skip(firstEllipsis + 1).Take(lastEllipsis - firstEllipsis - 1)
            .Where(a => a.Kind != LabyrinthPatternNodeKind.Ellipsis).ToList();
        var suffix = args.Skip(lastEllipsis + 1).ToList();

        var minimum = prefix.Count + suffix.Count + (middle.Count > 0 ? middle.Count : 0);
        body = AndAlso(body, Expression.GreaterThanOrEqual(ArgumentsCount(callNode), Expression.Constant(minimum)));

        for (var i = 0; i < prefix.Count; i++)
        {
            body = AndAlso(body, BuildArgExpression(prefix[i], IndexArgumentAt(callNode, i), ctx));
        }

        for (var i = 0; i < suffix.Count; i++)
        {
            body = AndAlso(body, BuildArgExpression(
                suffix[i],
                IndexArgumentAt(callNode, Expression.Subtract(ArgumentsCount(callNode), Expression.Constant(suffix.Count - i))),
                ctx));
        }

        // Middle segment. The overwhelmingly common case is a single
        // metavariable between two ellipses (e.g. `..., $DATA, ...`): the
        // minimum-arity check already guarantees an argument exists at
        // prefix.Count, so bind it directly by index instead of routing a
        // delegate through Enumerable.Any. Multi-node middle segments keep
        // the Any(...) term; the first candidate that matches (and binds)
        // wins, and the traversal layer (#103) drives candidate-specific
        // bindings.
        if (middle.Count == 1 && middle[0].Kind == LabyrinthPatternNodeKind.Metavariable)
        {
            return AndAlso(body, BuildArgExpression(
                middle[0],
                IndexArgumentAt(callNode, prefix.Count),
                ctx));
        }

        if (middle.Count > 0)
        {
            var candidate = Expression.Parameter(typeof(ILabyrinthMatchNode), "candidate");
            Expression middleBody = middle.Select(a => BuildArgExpression(a, candidate, ctx))
                .Aggregate(Expression.OrElse);
            var anyCall = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                new[] { typeof(ILabyrinthMatchNode) },
                Expression.Property(callNode, nameof(ILabyrinthMatchNode.Arguments)),
                Expression.Lambda<Func<ILabyrinthMatchNode, bool>>(middleBody, candidate));
            body = AndAlso(body, anyCall);
        }

        return body;
    }

    // ------------------------------------------------------------------
    // Pattern v2 shapes (issue #247)
    // ------------------------------------------------------------------

    /// <summary>A parsed member-call pattern shape: <c>$X.Method(args)</c> or <c>$X?.Method(args)</c>.</summary>
    private sealed record ReceiverCallShape(
        LabyrinthPatternNode Receiver, string MethodName, IReadOnlyList<LabyrinthPatternNode> ArgNodes, bool NullConditional);

    /// <summary>A parsed index-access pattern shape: <c>$ARR[$I]</c>.</summary>
    private sealed record IndexAccessShape(
        LabyrinthPatternNode Receiver, IReadOnlyList<LabyrinthPatternNode> ArgNodes);

    /// <summary>A parsed binary-operator chain shape: <c>$A / $B / $C</c>.</summary>
    private sealed record OperatorChainShape(
        IReadOnlyList<LabyrinthPatternNode> Operands, IReadOnlyList<string> Operators);

    private static bool IsValue(LabyrinthPatternNode node, string value) =>
        node.Kind == LabyrinthPatternNodeKind.Literal && string.Equals(node.Value, value, StringComparison.Ordinal);

    private static bool IsOpenParen(LabyrinthPatternNode node) =>
        node.TokenType == "LPAREN" || IsValue(node, "(");

    private static bool IsCloseParen(LabyrinthPatternNode node) =>
        node.TokenType == "RPAREN" || IsValue(node, ")");

    /// <summary>
    /// True when the node is a literal that names an identifier/callee (as opposed to punctuation or an operator).
    /// Value-based so patterns compile identically across target grammars.
    /// </summary>
    private static bool IsIdentifierLiteral(LabyrinthPatternNode node) =>
        node.Kind == LabyrinthPatternNodeKind.Literal
        && (node.TokenType == "IDENTIFIER" || !NonIdentifierValues.Contains(node.Value));

    /// <summary>True when the node can start a pattern operand (a metavariable or an identifier literal).</summary>
    private static bool IsOperand(LabyrinthPatternNode node) =>
        node.Kind == LabyrinthPatternNodeKind.Metavariable || IsIdentifierLiteral(node);

    /// <summary>
    /// Recognizes <c>[$X|ident] (.|?.) Name ( args )</c>. The null-conditional
    /// marker may lex as one token (<c>?.</c>) or as two (<c>? .</c>), depending
    /// on the target grammar.
    /// </summary>
    private static bool TrySplitReceiverCall(IReadOnlyList<LabyrinthPatternNode> nodes, out ReceiverCallShape shape)
    {
        shape = null!;
        if (nodes.Count < 5 || !IsOperand(nodes[0]))
        {
            return false;
        }

        var i = 1;
        var nullConditional = false;
        if (IsValue(nodes[i], "?."))
        {
            nullConditional = true;
            i++;
        }
        else if (IsValue(nodes[i], "?") && i + 1 < nodes.Count && IsValue(nodes[i + 1], "."))
        {
            nullConditional = true;
            i += 2;
        }
        else if (IsValue(nodes[i], "."))
        {
            i++;
        }
        else
        {
            return false;
        }

        if (i >= nodes.Count || !IsIdentifierLiteral(nodes[i]) || IsValue(nodes[i], "as"))
        {
            return false;
        }

        var methodName = nodes[i].Value;
        i++;
        if (i >= nodes.Count || !IsOpenParen(nodes[i]) || !IsCloseParen(nodes[^1]) || i == nodes.Count - 1)
        {
            return false;
        }

        var argNodes = nodes.Skip(i + 1).Take(nodes.Count - i - 2).ToList();
        shape = new ReceiverCallShape(nodes[0], methodName, argNodes, nullConditional);
        return true;
    }

    /// <summary>Recognizes <c>[$ARR|ident] [ args ]</c>.</summary>
    private static bool TrySplitIndexAccess(IReadOnlyList<LabyrinthPatternNode> nodes, out IndexAccessShape shape)
    {
        shape = null!;
        if (nodes.Count < 3 || !IsOperand(nodes[0]) || !IsValue(nodes[1], "[") || !IsValue(nodes[^1], "]"))
        {
            return false;
        }

        if (nodes.Count == 3)
        {
            shape = new IndexAccessShape(nodes[0], Array.Empty<LabyrinthPatternNode>());
            return true;
        }

        var argNodes = nodes.Skip(2).Take(nodes.Count - 3).ToList();
        shape = new IndexAccessShape(nodes[0], argNodes);
        return true;
    }

    /// <summary>
    /// Recognizes <c>operand (op operand)+</c> with op in <c>/</c>, <c>%</c>, <c>as</c>, <c>??</c>
    /// (<c>??</c> may lex as one token or as two <c>?</c>). Operands must be single
    /// tokens (metavariable or identifier); anything else is not an operator chain.
    /// </summary>
    private static bool TrySplitBinaryOperator(IReadOnlyList<LabyrinthPatternNode> nodes, out OperatorChainShape shape)
    {
        shape = null!;
        if (nodes.Count < 3 || !IsOperand(nodes[0]))
        {
            return false;
        }

        var operands = new List<LabyrinthPatternNode> { nodes[0] };
        var operators = new List<string>();
        var i = 1;
        while (i < nodes.Count)
        {
            string? opName = null;
            if (IsValue(nodes[i], "??"))
            {
                opName = OperatorNames["??"];
                i += 1;
            }
            else if (IsValue(nodes[i], "?") && i + 1 < nodes.Count && IsValue(nodes[i + 1], "?"))
            {
                opName = OperatorNames["??"];
                i += 2;
            }
            else if (nodes[i].Kind == LabyrinthPatternNodeKind.Literal
                && OperatorNames.TryGetValue(nodes[i].Value, out var mapped))
            {
                opName = mapped;
                i += 1;
            }

            if (opName is null)
            {
                return false;
            }

            if (i >= nodes.Count || !IsOperand(nodes[i]))
            {
                return false;
            }

            operators.Add(opName);
            operands.Add(nodes[i]);
            i++;
        }

        shape = new OperatorChainShape(operands, operators);
        return operators.Count > 0;
    }

    /// <summary>
    /// Builds a member-call matcher: FunctionCall with the given method name,
    /// receiver bound or name-compared, and (for plain <c>.</c> patterns)
    /// <see cref="ILabyrinthMatchNode.IsNullConditional"/> == false;
    /// <c>?.</c> patterns match only null-conditional accesses.
    /// </summary>
    private static Expression BuildReceiverCallBody(ReceiverCallShape shape, Expression callNode, ParameterExpression ctx)
    {
        Expression body = Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.Name)),
            Expression.Constant(shape.MethodName)));
        body = AndAlso(body, Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.IsNullConditional)),
            Expression.Constant(shape.NullConditional)));

        var receiver = Expression.Property(callNode, nameof(ILabyrinthMatchNode.Receiver));
        body = AndAlso(body, Expression.NotEqual(
            receiver,
            Expression.Constant(null, typeof(ILabyrinthMatchNode))));
        body = AndAlso(body, shape.Receiver.Kind == LabyrinthPatternNodeKind.Metavariable
            ? Expression.Call(ctx, TryBindMethod, Expression.Constant(shape.Receiver.MetavariableName), receiver)
            : EqualName(receiver, shape.Receiver.Value));

        return AndAlso(body, BuildArgumentsBody(shape.ArgNodes, callNode, ctx));
    }

    /// <summary>
    /// Builds an index-access matcher: an <c>IndexAccess</c> node whose
    /// <see cref="ILabyrinthMatchNode.Receiver"/> is the indexed expression and
    /// whose index expressions match the bracketed pattern positionally.
    /// </summary>
    private static Expression BuildIndexAccessBody(IndexAccessShape shape, Expression indexNode, ParameterExpression ctx)
    {
        Expression body = Expression.Equal(
            Expression.Property(indexNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(IndexAccessNodeType));

        var receiver = Expression.Property(indexNode, nameof(ILabyrinthMatchNode.Receiver));
        body = AndAlso(body, Expression.NotEqual(
            receiver,
            Expression.Constant(null, typeof(ILabyrinthMatchNode))));
        body = AndAlso(body, shape.Receiver.Kind == LabyrinthPatternNodeKind.Metavariable
            ? Expression.Call(ctx, TryBindMethod, Expression.Constant(shape.Receiver.MetavariableName), receiver)
            : EqualName(receiver, shape.Receiver.Value));

        return AndAlso(body, BuildArgumentsBody(shape.ArgNodes, indexNode, ctx));
    }

    /// <summary>
    /// Builds a left-associative operator-chain matcher: the pattern's last
    /// operator is the outermost node; earlier operands fold into
    /// <c>Arguments[0]</c>, the last operand matches <c>Arguments[1]</c>.
    /// </summary>
    private static Expression BuildOperatorChainBody(OperatorChainShape shape, Expression operandNode, ParameterExpression ctx)
    {
        if (shape.Operators.Count == 0)
        {
            return BuildOperand(shape.Operands[0], operandNode, ctx);
        }

        var op = shape.Operators[^1];
        Expression body = Expression.Equal(
            Expression.Property(operandNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, Expression.Equal(
            Expression.Property(operandNode, nameof(ILabyrinthMatchNode.Name)),
            Expression.Constant(op)));
        body = AndAlso(body, Expression.Equal(ArgumentsCount(operandNode), Expression.Constant(2)));

        var left = BuildOperatorChainBody(
            new OperatorChainShape(
                shape.Operands.Take(shape.Operands.Count - 1).ToList(),
                shape.Operators.Take(shape.Operators.Count - 1).ToList()),
            IndexArgumentAt(operandNode, 0),
            ctx);
        body = AndAlso(body, left);
        return AndAlso(body, BuildOperand(shape.Operands[^1], IndexArgumentAt(operandNode, 1), ctx));
    }

    /// <summary>
    /// Matches a single-token operand of an operator chain: a metavariable
    /// (binds) or an identifier literal (name equality).
    /// </summary>
    private static Expression BuildOperand(LabyrinthPatternNode operand, Expression operandNode, ParameterExpression ctx)
    {
        if (operand.Kind == LabyrinthPatternNodeKind.Metavariable)
        {
            return Expression.Call(ctx, TryBindMethod, Expression.Constant(operand.MetavariableName), operandNode);
        }

        if (operand.Kind != LabyrinthPatternNodeKind.Literal)
        {
            throw new LabyrinthRuleException(
                "Operator operands must be a single metavariable or an identifier literal.");
        }

        return EqualName(operandNode, LiteralText(operand));
    }

    private static Expression BuildArgExpression(LabyrinthPatternNode arg, Expression argNode, ParameterExpression ctx)
    {
        return arg.Kind switch
        {
            LabyrinthPatternNodeKind.Metavariable =>
                Expression.Call(ctx, TryBindMethod, Expression.Constant(arg.MetavariableName), argNode),
            LabyrinthPatternNodeKind.Ellipsis => throw new LabyrinthRuleException(
                "An ellipsis is only allowed as an argument of its own."),
            _ => EqualName(argNode, LiteralText(arg))
        };
    }

    private static string LiteralText(LabyrinthPatternNode literal)
    {
        // Strip quotes from string literals so "msg" in a pattern compares
        // equal to a text node holding msg.
        return literal.TokenType == "STRING" ? literal.Value.Trim('"') : literal.Value;
    }

    private static List<LabyrinthPatternNode> SplitArguments(IReadOnlyList<LabyrinthPatternNode> argNodes)
    {
        var args = new List<LabyrinthPatternNode>();
        var current = new List<LabyrinthPatternNode>();
        var depth = 0;

        foreach (var n in argNodes)
        {
            switch (n.TokenType)
            {
                case "LPAREN":
                    depth++;
                    break;
                case "RPAREN":
                    depth--;
                    break;
                case "COMMA" when depth == 0:
                    AddCurrent(args, current);
                    continue;
            }

            current.Add(n);
        }

        AddCurrent(args, current);
        return args;
    }

    private static void AddCurrent(List<LabyrinthPatternNode> args, List<LabyrinthPatternNode> current)
    {
        if (current.Count == 0)
        {
            return; // tolerate trailing commas / empty argument slots between ellipses
        }

        if (current.Count > 1)
        {
            throw new LabyrinthRuleException(
                $"Argument '{string.Join(" ", current.Select(n => n.Value))}' is a nested expression; " +
                "only single-token arguments are supported by expression compilation.");
        }

        args.Add(current[0]);
        current.Clear();
    }

    /// <summary>
    /// Finds the top-level assignment token ("="), tracking bracket depth.
    /// Value-based (v2): the target grammar may tokenize "=" as EQUALS or as
    /// any operator token rule; "==", "&gt;=" and friends have different values
    /// and never match.
    /// </summary>
    private static int FindTopLevelEquals(IReadOnlyList<LabyrinthPatternNode> nodes)
    {
        var depth = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (IsOpenParen(node) || IsValue(node, "["))
            {
                depth++;
            }
            else if (IsCloseParen(node) || IsValue(node, "]"))
            {
                depth--;
            }
            else if (depth == 0 && (node.TokenType == "EQUALS" || IsValue(node, "=")))
            {
                return i;
            }
        }

        return -1;
    }

    private static Expression EqualName(Expression argNode, string name) => Expression.Equal(
        Expression.Property(argNode, nameof(ILabyrinthMatchNode.Name)),
        Expression.Constant(name, typeof(string)));

    private static Expression ArgumentsCount(Expression node) => Expression.Property(
        Expression.Property(node, nameof(ILabyrinthMatchNode.Arguments)), CountProperty);

    private static Expression IndexArgumentAt(Expression node, int index) => Expression.Property(
        Expression.Property(node, nameof(ILabyrinthMatchNode.Arguments)), ItemProperty, Expression.Constant(index));

    private static Expression IndexArgumentAt(Expression node, Expression index) => Expression.Property(
        Expression.Property(node, nameof(ILabyrinthMatchNode.Arguments)), ItemProperty, index);

    private static Expression AndAlso(Expression left, Expression right) => Expression.AndAlso(left, right);
}
