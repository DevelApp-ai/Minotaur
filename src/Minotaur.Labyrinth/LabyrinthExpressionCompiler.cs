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
/// Supported pattern shapes (v1, issue #102):
/// <list type="bullet">
/// <item>Call: <c>ReceiveData($DATA)</c> ⇒ <c>node.NodeType == "FunctionCall" &amp;&amp; node.Name == "ReceiveData" &amp;&amp; …args</c></item>
/// <item>Assignment: <c>$TARGET = FormatString($SRC)</c> ⇒ matches <c>Assignment</c> nodes, binding both sides.</item>
/// <item>Bare metavariable <c>$DATA</c> (binds anything) or bare identifier <c>Require</c> (name equality).</item>
/// </list>
/// <para>
/// Supported pattern shapes (v2, Minotaur issue #247):
/// <list type="bullet">
/// <item>Dotted metavariable call: <c>$X.DoWork($A)</c> ⇒ <c>FunctionCall</c> whose Name ends with <c>.DoWork</c>, binding <c>$X</c> to the call's Receiver; a literal receiver (<c> ArgumentNullException.ThrowIfNull($X)</c> alternatively without spaces) compiles to full name equality through the same shape.</item>
/// <item>Conditional access: <c>$X?.DoWork(...)</c> ⇒ like the dotted call but the Name must end with <c>?.DoWork</c>.</item>
/// <item>Binary operator: <c>$A / $B</c>, <c>$A % $B</c>, <c>$A as $B</c>, <c>$A ?? $B</c> ⇒ <c>FunctionCall</c> nodes named <c>op:divide</c>, <c>op:modulo</c>, <c>op:as</c>, <c>op:coalesce</c> with two bound operands.</item>
/// <item>Index: <c>$ARR[$I]</c> ⇒ <c>IndexAccess</c> node binding the array through its Receiver.</item>
/// <item>Assignment RHS is no longer restricted to call patterns: <c>$X = $Y</c>, <c>$X = $A op $B</c>, <c>$X = $Y.M(...)</c> and <c>$X = $A[$B]</c> are all writable.</item>
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

    // Pattern v2 (issue #247): receivers are a first-class part of the node
    // contract, and metavariable receiver patterns check Name suffixes on
    // the string type.
    private static readonly System.Reflection.PropertyInfo ReceiverProperty =
        typeof(ILabyrinthMatchNode).GetProperty(nameof(ILabyrinthMatchNode.Receiver))
        ?? throw new InvalidOperationException("Receiver property is missing.");

    private static readonly System.Reflection.MethodInfo StringEndsWithMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })
        ?? throw new InvalidOperationException("string.EndsWith is missing.");

    private static readonly System.Reflection.MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
        ?? throw new InvalidOperationException("string.Contains is missing.");

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

        // Assignment pattern: "<lhs> = <rhs>" at the top level. The EQUALS
        // token type is the common one; value-based detection is a v2
        // (#247) fallback for grammars that lex "=" under a different type.
        var equalsIndex = FindTopLevel(nodes, "EQUALS");
        if (equalsIndex < 0)
        {
            equalsIndex = FindTopLevelByValue(nodes, "=");
        }

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
        if (IsIdentifierToken(nodes[0]) && IsOpenParen(nodes[1]) && IsCloseParen(nodes[^1]) && nodes.Count >= 3)
        {
            return BuildCallBody(nodes[0].Value, nodes.Skip(2).Take(nodes.Count - 3).ToList(), node, ctx);
        }

        // Dotted call pattern (v2, #247): "$X.Method(<args>)" (receiver
        // binding), "$X?.Method(<args>)" (conditional access), or
        // "prefix.Name(<args>)" (dotted name equality).
        if (TrySplitDottedCall(nodes, out var head, out var methodParts, out var condAccess, out var dottedArgs))
        {
            return BuildDottedCallBody(head, methodParts, condAccess, dottedArgs, node, ctx);
        }

        // Binary operator pattern (v2, #247): "$A / $B", "$A % $B",
        // "$A as $B", "$A ?? $B".
        if (TrySplitBinaryOperator(nodes, out var opLeft, out var opName, out var opRight))
        {
            return BuildBinaryOperatorBody(opLeft, opName, opRight, node, ctx);
        }

        // Index pattern (v2, #247): "$ARR[$I]".
        if (TrySplitIndex(nodes, out var arrayNode, out var indexNode))
        {
            return BuildIndexBody(arrayNode, indexNode, node, ctx);
        }

        throw new LabyrinthRuleException(
            $"Pattern '{pattern.Pattern}' has an unsupported shape for expression compilation " +
            "(supported: call, dotted call, assignment, bare metavariable or bare identifier, binary operator, index).");
    }

    private static Expression BuildSingleExpression(LabyrinthPatternNode single, Expression node, ParameterExpression ctx)
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
        if (rhsNodes.Count == 0)
        {
            throw new LabyrinthRuleException(
                $"Assignment right-hand side '{rhs.Pattern}' is empty.");
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

        // Right-hand side (v2, #247): any single-pattern shape, matched
        // against Arguments[1] — no longer restricted to call patterns.
        var right = IndexArgumentAt(node, 1);
        Expression rhsBody;
        if (rhsNodes.Count == 1)
        {
            rhsBody = BuildSingleExpression(rhsNodes[0], right, ctx);
        }
        else if (IsIdentifierToken(rhsNodes[0]) && IsOpenParen(rhsNodes[1]) && IsCloseParen(rhsNodes[^1]) && rhsNodes.Count >= 3)
        {
            rhsBody = BuildCallBody(rhsNodes[0].Value, rhsNodes.Skip(2).Take(rhsNodes.Count - 3).ToList(), right, ctx);
        }
        else if (TrySplitDottedCall(rhsNodes, out var head, out var methodParts, out var condAccess, out var dottedArgs))
        {
            rhsBody = BuildDottedCallBody(head, methodParts, condAccess, dottedArgs, right, ctx);
        }
        else if (TrySplitBinaryOperator(rhsNodes, out var opLeft, out var opName, out var opRight))
        {
            rhsBody = BuildBinaryOperatorBody(opLeft, opName, opRight, right, ctx);
        }
        else if (TrySplitIndex(rhsNodes, out var arrayNode, out var indexNode))
        {
            rhsBody = BuildIndexBody(arrayNode, indexNode, right, ctx);
        }
        else
        {
            throw new LabyrinthRuleException(
                $"Assignment right-hand side '{rhs.Pattern}' is not a supported shape " +
                "(supported: bare metavariable/identifier, call, dotted call, binary operator, index).");
        }

        return AndAlso(body, rhsBody);
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
        return AndAlso(body, BuildArgumentList(argNodes, callNode, ctx));
    }

    /// <summary>
    /// The argument-list matching half of a call pattern (fixed arity,
    /// ellipsis prefix/middle/suffix), shared by the v1 call body and the v2
    /// dotted-call bodies (issue #247).
    /// </summary>
    private static Expression BuildArgumentList(
        IReadOnlyList<LabyrinthPatternNode> argNodes, Expression callNode, ParameterExpression ctx)
    {
        var args = SplitArguments(argNodes);

        var firstEllipsis = args.FindIndex(a => a.Kind == LabyrinthPatternNodeKind.Ellipsis);
        if (firstEllipsis < 0)
        {
            // Fixed arity: count must match exactly, then match positionally.
            var terms = new List<Expression>
            {
                Expression.Equal(ArgumentsCount(callNode), Expression.Constant(args.Count))
            };
            for (var i = 0; i < args.Count; i++)
            {
                terms.Add(BuildArgExpression(args[i], IndexArgumentAt(callNode, i), ctx));
            }

            return terms.Aggregate(AndAlso);
        }

        // Ellipsis present: prefix before the first ellipsis, suffix after the
        // last; argument nodes strictly between the ellipses bind via Any(...).
        var lastEllipsis = args.FindLastIndex(a => a.Kind == LabyrinthPatternNodeKind.Ellipsis);
        var prefix = args.Take(firstEllipsis).ToList();
        var middle = args.Skip(firstEllipsis + 1).Take(lastEllipsis - firstEllipsis - 1)
            .Where(a => a.Kind != LabyrinthPatternNodeKind.Ellipsis).ToList();
        var suffix = args.Skip(lastEllipsis + 1).ToList();

        var minimum = prefix.Count + suffix.Count + (middle.Count > 0 ? middle.Count : 0);
        var ellipsis = new List<Expression>
        {
            Expression.GreaterThanOrEqual(ArgumentsCount(callNode), Expression.Constant(minimum))
        };

        for (var i = 0; i < prefix.Count; i++)
        {
            ellipsis.Add(BuildArgExpression(prefix[i], IndexArgumentAt(callNode, i), ctx));
        }

        for (var i = 0; i < suffix.Count; i++)
        {
            ellipsis.Add(BuildArgExpression(
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
            ellipsis.Add(BuildArgExpression(
                middle[0],
                IndexArgumentAt(callNode, prefix.Count),
                ctx));
            return ellipsis.Aggregate(AndAlso);
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
            ellipsis.Add(anyCall);
        }

        return ellipsis.Aggregate(AndAlso);
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

    // -----------------------------------------------------------------
    // Pattern v2 shapes (Minotaur issue #247)
    // -----------------------------------------------------------------

    private static bool IsValue(LabyrinthPatternNode node, string value) =>
        node.Kind == LabyrinthPatternNodeKind.Literal && node.Value == value;

    private static bool IsIdentifierToken(LabyrinthPatternNode node) =>
        node.Kind == LabyrinthPatternNodeKind.Literal
        && (node.TokenType.ToUpperInvariant().Contains("IDENT") || node.TokenType == "NAME");

    private static bool IsOpenParen(LabyrinthPatternNode node) =>
        node.TokenType == "LPAREN" || IsValue(node, "(");

    private static bool IsCloseParen(LabyrinthPatternNode node) =>
        node.TokenType == "RPAREN" || IsValue(node, ")");

    /// <summary>
    /// Splits a dotted call pattern: a head (metavariable or identifier),
    /// one or more member segments joined by "." or the "?." conditional
    /// access pair, then a parenthesized argument list. Recognizes both
    /// <c>$X.DoWork($A)</c> (receiver binding) and
    /// <c>ArgumentNullException.ThrowIfNull($X)</c> (dotted name equality),
    /// as well as the conditional access spelling <c>$X?.DoWork($A)</c>.
    /// </summary>
    private static bool TrySplitDottedCall(
        IReadOnlyList<LabyrinthPatternNode> nodes,
        out LabyrinthPatternNode head,
        out List<string> methodParts,
        out bool condAccess,
        out List<LabyrinthPatternNode> args)
    {
        head = null!;
        methodParts = null!;
        condAccess = false;
        args = null!;

        if (nodes.Count < 5)
        {
            return false;
        }

        var headNode = nodes[0];
        if (headNode.Kind != LabyrinthPatternNodeKind.Metavariable && !IsIdentifierToken(headNode))
        {
            return false;
        }

        var parts = new List<string>();
        var conditional = false;
        var k = 1;
        while (k < nodes.Count && !IsOpenParen(nodes[k]))
        {
            if (k + 1 < nodes.Count && IsValue(nodes[k], ".") && IsIdentifierToken(nodes[k + 1]))
            {
                parts.Add(nodes[k + 1].Value);
                k += 2;
                continue;
            }

            // `?.` may lex as one token or as a "?" "." pair.
            if (k + 1 < nodes.Count && IsValue(nodes[k], "?.") && IsIdentifierToken(nodes[k + 1]))
            {
                parts.Add(nodes[k + 1].Value);
                conditional = true;
                k += 2;
                continue;
            }

            if (k + 2 < nodes.Count && IsValue(nodes[k], "?") && IsValue(nodes[k + 1], ".") && IsIdentifierToken(nodes[k + 2]))
            {
                parts.Add(nodes[k + 2].Value);
                conditional = true;
                k += 3;
                continue;
            }

            return false;
        }

        if (parts.Count == 0 || k >= nodes.Count || !IsCloseParen(nodes[^1]) || k >= nodes.Count - 1)
        {
            return false;
        }

        head = headNode;
        methodParts = parts;
        condAccess = conditional;
        args = nodes.Skip(k + 1).Take(nodes.Count - k - 2).ToList();
        return true;
    }

    /// <summary>
    /// Splits a binary operator pattern into its two operands and the
    /// materialized operator name. Handles <c>/</c>, <c>%</c>, <c>as</c>, a
    /// single <c>??</c> token, and the two-token <c>? ?</c> coalesce spelling.
    /// </summary>
    private static bool TrySplitBinaryOperator(
        IReadOnlyList<LabyrinthPatternNode> nodes,
        out LabyrinthPatternNode left,
        out string opName,
        out LabyrinthPatternNode right)
    {
        left = null!;
        opName = null!;
        right = null!;

        if (nodes.Count != 3 && nodes.Count != 4)
        {
            return false;
        }

        var candidateLeft = nodes[0];
        var mid = nodes[1];
        var candidateRight = nodes.Count == 3 ? nodes[2] : nodes[3];
        string? candidateOp = null;
        if (nodes.Count == 3)
        {
            if (IsValue(mid, "/"))
            {
                candidateOp = "op:divide";
            }
            else if (IsValue(mid, "%"))
            {
                candidateOp = "op:modulo";
            }
            else if (IsValue(mid, "as"))
            {
                candidateOp = "op:as";
            }
            else if (IsValue(mid, "??"))
            {
                candidateOp = "op:coalesce";
            }
        }
        else if (IsValue(mid, "?") && IsValue(nodes[2], "?"))
        {
            candidateOp = "op:coalesce";
        }

        if (candidateOp is null)
        {
            return false;
        }

        var operand = (LabyrinthPatternNode n) =>
            n.Kind == LabyrinthPatternNodeKind.Metavariable || IsIdentifierToken(n);
        if (!operand(candidateLeft) || !operand(candidateRight))
        {
            return false;
        }

        left = candidateLeft;
        opName = candidateOp;
        right = candidateRight;
        return true;
    }

    /// <summary>Splits an index pattern: <c>$ARR[$I]</c> or <c>arr[$I]</c>.</summary>
    private static bool TrySplitIndex(
        IReadOnlyList<LabyrinthPatternNode> nodes,
        out LabyrinthPatternNode array,
        out LabyrinthPatternNode index)
    {
        array = null!;
        index = null!;

        if (nodes.Count != 4)
        {
            return false;
        }

        if (!(IsValue(nodes[1], "[") || nodes[1].TokenType == "LBRACKET"))
        {
            return false;
        }

        if (!(IsValue(nodes[3], "]") || nodes[3].TokenType == "RBRACKET"))
        {
            return false;
        }

        var arrayNode = nodes[0];
        var indexNode = nodes[2];
        var arrayOk = arrayNode.Kind == LabyrinthPatternNodeKind.Metavariable || IsIdentifierToken(arrayNode);
        var indexOk = indexNode.Kind == LabyrinthPatternNodeKind.Metavariable || IsIdentifierToken(indexNode);
        if (!arrayOk || !indexOk)
        {
            return false;
        }

        array = arrayNode;
        index = indexNode;
        return true;
    }

    private static Expression BuildDottedCallBody(
        LabyrinthPatternNode head, IReadOnlyList<string> methodParts, bool condAccess,
        List<LabyrinthPatternNode> argNodes, Expression callNode, ParameterExpression ctx)
    {
        var method = string.Join(".", methodParts);

        // Literal receiver: plain (v1-compatible) name equality on the full
        // dotted name, e.g. ArgumentNullException.ThrowIfNull($X).
        if (head.Kind == LabyrinthPatternNodeKind.Literal)
        {
            if (condAccess)
            {
                throw new LabyrinthRuleException(
                    $"Conditional access patterns need a metavariable receiver; '{head.Value}' is literal.");
            }

            return BuildCallBody(head.Value + "." + method, argNodes, callNode, ctx);
        }

        if (methodParts.Count != 1)
        {
            throw new LabyrinthRuleException(
                "Dotted metavariable call patterns support a single method segment: $X.Method(...).");
        }

        var name = Expression.Property(callNode, nameof(ILabyrinthMatchNode.Name));
        Expression body = Expression.Equal(
            Expression.Property(callNode, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, Expression.NotEqual(name, Expression.Constant(null, typeof(string))));

        // Plain deref: the Name ends with ".Method" and contains no "?";
        // conditional access: the Name ends with "?.Method".
        var suffix = Expression.Constant((condAccess ? "?." : ".") + method);
        body = AndAlso(body, Expression.Call(name, StringEndsWithMethod, suffix));
        var containsQuestion = Expression.Call(name, StringContainsMethod, Expression.Constant("?"));
        body = AndAlso(body, condAccess ? containsQuestion : Expression.Not(containsQuestion));

        // Receiver: present and bound to the head metavariable.
        var receiver = Expression.Property(callNode, ReceiverProperty);
        body = AndAlso(body, Expression.NotEqual(receiver, Expression.Constant(null, typeof(ILabyrinthMatchNode))));
        body = AndAlso(body, Expression.Call(ctx, TryBindMethod, Expression.Constant(head.MetavariableName), receiver));

        return AndAlso(body, BuildArgumentList(argNodes, callNode, ctx));
    }

    private static Expression BuildBinaryOperatorBody(
        LabyrinthPatternNode left, string opName, LabyrinthPatternNode right, Expression node, ParameterExpression ctx)
    {
        Expression body = Expression.Equal(
            Expression.Property(node, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant(FunctionCallNodeType));
        body = AndAlso(body, EqualName(node, opName));
        body = AndAlso(body, Expression.Equal(ArgumentsCount(node), Expression.Constant(2)));
        body = AndAlso(body, BuildArgExpression(left, IndexArgumentAt(node, 0), ctx));
        body = AndAlso(body, BuildArgExpression(right, IndexArgumentAt(node, 1), ctx));
        return body;
    }

    private static Expression BuildIndexBody(
        LabyrinthPatternNode array, LabyrinthPatternNode index, Expression node, ParameterExpression ctx)
    {
        Expression body = Expression.Equal(
            Expression.Property(node, nameof(ILabyrinthMatchNode.NodeType)),
            Expression.Constant("IndexAccess"));

        if (array.Kind == LabyrinthPatternNodeKind.Metavariable)
        {
            var receiver = Expression.Property(node, ReceiverProperty);
            body = AndAlso(body, Expression.NotEqual(receiver, Expression.Constant(null, typeof(ILabyrinthMatchNode))));
            body = AndAlso(body, Expression.Call(ctx, TryBindMethod, Expression.Constant(array.MetavariableName), receiver));
        }
        else
        {
            body = AndAlso(body, EqualName(node, array.Value));
        }

        body = AndAlso(body, Expression.Equal(ArgumentsCount(node), Expression.Constant(1)));
        return AndAlso(body, BuildArgExpression(index, IndexArgumentAt(node, 0), ctx));
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
            // v2 (#247): tokens are compared by type first (v1 behaviour)
            // and by value, so grammars that lex "(" / ")" under different
            // type names still nest correctly.
            if (n.TokenType == "LPAREN" || IsValue(n, "("))
            {
                depth++;
            }
            else if (n.TokenType == "RPAREN" || IsValue(n, ")"))
            {
                depth--;
            }
            else if ((n.TokenType == "COMMA" || IsValue(n, ",")) && depth == 0)
            {
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

    /// <summary>
    /// v2 (#247): finds a top-level literal token by value, for grammars
    /// that lex "=" under a type name other than EQUALS. The comparison is
    /// exact, so "==" (comparison) tokens are not matched.
    /// </summary>
    private static int FindTopLevelByValue(IReadOnlyList<LabyrinthPatternNode> nodes, string value)
    {
        var depth = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].TokenType == "LPAREN" || IsValue(nodes[i], "("))
            {
                depth++;
            }
            else if (nodes[i].TokenType == "RPAREN" || IsValue(nodes[i], ")"))
            {
                depth--;
            }
            else if (depth == 0 && IsValue(nodes[i], value))
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
