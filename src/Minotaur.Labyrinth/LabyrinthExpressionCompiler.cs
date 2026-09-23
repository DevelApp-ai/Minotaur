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
/// Supported pattern shapes (v1):
/// <list type="bullet">
/// <item>Call: <c>ReceiveData($DATA)</c> ⇒ <c>node.NodeType == "FunctionCall" &amp;&amp; node.Name == "ReceiveData" &amp;&amp; …args</c></item>
/// <item>Assignment: <c>$TARGET = FormatString($SRC)</c> ⇒ matches <c>Assignment</c> nodes, binding both sides.</item>
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

        var sources = rule.Sources.Select(e => Compile(rule.Rule.Id, e.Pattern, e.Condition)).ToArray();
        var sinks = rule.Sinks.Select(e => Compile(rule.Rule.Id, e.Pattern, e.Condition)).ToArray();
        var sanitizers = rule.Sanitizers.Select(e => Compile(rule.Rule.Id, e.Pattern, e.Condition)).ToArray();
        var propagators = rule.Propagators
            .Select(p => new LabyrinthCompiledPropagator(Compile(rule.Rule.Id, p.Pattern, p.Condition), p.From, p.To))
            .ToArray();

        var search = rule.SearchPattern is null
            ? null
            : Compile(rule.Rule.Id, rule.SearchPattern, rule.SearchCondition);

        return new LabyrinthCompiledMatchers(rule.Rule, search, sources, sinks, sanitizers, propagators);
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

        // Assignment pattern: "<lhs> = <rhs>" at the top level.
        var equalsIndex = FindTopLevel(nodes, "EQUALS");
        if (equalsIndex == 1 && nodes.Count > 2)
        {
            var lhs = nodes[0];
            var rhs = new LabyrinthPatternAst(pattern.Pattern, nodes.Skip(2).ToArray());
            return BuildAssignmentBody(lhs, rhs, node, ctx);
        }

        if (nodes.Count == 0)
        {
            throw new LabyrinthRuleException($"Pattern '{pattern.Pattern}' is empty.");
        }

        // Bare pattern: a single metavariable or identifier.
        if (nodes.Count == 1)
        {
            return BuildSingleExpression(nodes[0], node, ctx);
        }

        // Call pattern: "Name(<args>)".
        if (nodes[0].TokenType == "IDENTIFIER" && nodes[1].TokenType == "LPAREN"
            && nodes[^1].TokenType == "RPAREN" && nodes.Count >= 3)
        {
            return BuildCallBody(nodes[0].Value, nodes.Skip(2).Take(nodes.Count - 3).ToList(), node, ctx);
        }

        throw new LabyrinthRuleException(
            $"Pattern '{pattern.Pattern}' has an unsupported shape for expression compilation " +
            "(supported: call, assignment, bare metavariable or bare identifier).");
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

        var rhsNodes = rhs.Nodes;
        if (rhsNodes.Count < 3 || rhsNodes[0].TokenType != "IDENTIFIER"
            || rhsNodes[1].TokenType != "LPAREN" || rhsNodes[^1].TokenType != "RPAREN")
        {
            throw new LabyrinthRuleException(
                $"Assignment right-hand side '{rhs.Pattern}' is not a call pattern; only '<lhs> = Call(<args>)' is supported.");
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

        // Right-hand side: a call pattern against Arguments[1].
        var right = IndexArgumentAt(node, 1);
        var callBody = BuildCallBody(rhsNodes[0].Value, rhsNodes.Skip(2).Take(rhsNodes.Count - 3).ToList(), right, ctx);
        return AndAlso(body, callBody);
    }

    private static Expression BuildCallBody(
        string name, IReadOnlyList<LabyrinthPatternNode> argNodes, Expression callNode, ParameterExpression ctx)
    {
        var args = SplitArguments(argNodes);

        Expression body = Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.Name)),
            Expression.Constant(name)));

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

    private static int FindTopLevel(IReadOnlyList<LabyrinthPatternNode> nodes, string tokenType)
    {
        var depth = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            switch (nodes[i].TokenType)
            {
                case "LPAREN":
                    depth++;
                    break;
                case "RPAREN":
                    depth--;
                    break;
                default:
                    if (depth == 0 && nodes[i].TokenType == tokenType)
                    {
                        return i;
                    }

                    break;
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
