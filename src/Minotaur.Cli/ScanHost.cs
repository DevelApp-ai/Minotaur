using DevelApp.StepLexer;
using DevelApp.StepParser;
using Minotaur.Labyrinth;

namespace Minotaur.Cli;

/// <summary>
/// A match node materialized from a parsed source file. Implements
/// <see cref="ILabyrinthFindingLocation"/> so the reporting layer can render
/// file/line and the baseline can fingerprint findings.
/// </summary>
public sealed class SourceMatchNode : ILabyrinthMatchNode, ILabyrinthFindingLocation
{
    public SourceMatchNode(string nodeType, string? name, IReadOnlyList<ILabyrinthMatchNode> arguments,
        string filePath, int position, string source, ILabyrinthMatchNode? receiver = null)
    {
        NodeType = nodeType;
        Name = name;
        Arguments = arguments;
        Receiver = receiver;
        FilePath = filePath;
        Position = position;
        var (line, col) = LineCol(source, position);
        StartLine = line;
        StartColumn = col;
    }

    public string NodeType { get; }
    public string? Name { get; }
    public IReadOnlyList<ILabyrinthMatchNode> Arguments { get; }
    public ILabyrinthMatchNode? Receiver { get; }
    public string FilePath { get; }
    public int StartLine { get; }
    public int StartColumn { get; }
    public int Position { get; }

    private static (int line, int col) LineCol(string source, int pos)
    {
        var line = 1;
        var col = 1;
        var end = Math.Min(pos, source.Length);
        for (var k = 0; k < end; k++)
        {
            if (source[k] == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }

        return (line, col);
    }
}

/// <summary>
/// The host adapter that makes the Labyrinth engine runnable from the CLI:
/// resolves a target grammar (downloaded at build time from Minotaur-Grammars
/// into the Grammars/ output directory), parses a source file with it through
/// the real DevelApp.StepLexer/StepParser pipeline, materializes
/// <see cref="ILabyrinthMatchNode"/>s from the token stream, builds a simple
/// document-order data-flow graph, and runs the compiled rules.
/// </summary>
public static class ScanHost
{
    /// <summary>Finds the Grammars directory (copied to the build output by the DownloadLabyrinthGrammars target).</summary>
    public static string FindGrammarsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "Grammars")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Grammars directory not found; build without -p:SkipGrammarDownload=true so the target grammars are downloaded.");
        }

        return Path.Combine(dir.FullName, "Grammars");
    }

    /// <summary>Resolves a target grammar by directory name (e.g. "java17") from the LabyrinthTargetGrammars registry.</summary>
    public static LabyrinthTargetGrammar ResolveTarget(string name)
    {
        var match = LabyrinthTargetGrammars.All.FirstOrDefault(t =>
            t.Directory.Equals($"programming-languages/{name}", StringComparison.Ordinal)
            || t.Directory.EndsWith("/" + name, StringComparison.Ordinal));
        if (match is null)
        {
            var known = string.Join(", ", LabyrinthTargetGrammars.All.Select(t => t.Directory.Split('/').Last()));
            throw new ArgumentException($"Unknown Labyrinth target grammar '{name}'. Known target grammars: {known}");
        }

        return match;
    }

    /// <summary>The downloaded grammar file for a target grammar.</summary>
    public static string GrammarFilePath(string grammarsRoot, LabyrinthTargetGrammar target) =>
        Path.Combine(grammarsRoot, "targets", target.Directory.Replace('/', Path.DirectorySeparatorChar), target.MainFile);

    /// <summary>
    /// Parses one source file with the target grammar and builds the data-flow
    /// graph. The graph chains all materialized nodes in document order
    /// (a simple ordered-flow approximation; interprocedural edges are not
    /// built yet). Identifier nodes are interned per name so metavariable
    /// unification (reference equality) works across occurrences.
    /// </summary>
    public static LabyrinthDataFlowGraph BuildGraph(
        string source, string filePath, string grammarContent, string grammarName, out List<ILabyrinthMatchNode> nodes)
    {
        using var engine = new StepParserEngine();
        engine.LoadGrammarFromContent(grammarContent, grammarName);
        var result = engine.Parse(source, filePath);

        var tokens = result.Tokens.Where(t => !IsSkippable(t)).ToList();
        var intern = new Dictionary<string, SourceMatchNode>(StringComparer.Ordinal);
        var all = new List<SourceMatchNode>();
        var i = 0;
        var topStops = new HashSet<string>(StringComparer.Ordinal) { ";" };
        while (i < tokens.Count)
        {
            ParseExpr(tokens, ref i, source, filePath, intern, all, topStops);
            if (i < tokens.Count && tokens[i].Value == ";")
            {
                i++;
            }
        }

        var graph = new LabyrinthDataFlowGraph();
        SourceMatchNode? prev = null;
        foreach (var node in all.OrderBy(n => n.Position))
        {
            graph.AddNode(node);
            if (prev is not null)
            {
                graph.Connect(prev, node);
            }

            prev = node;
        }

        nodes = all.Cast<ILabyrinthMatchNode>().ToList();
        return graph;
    }

    /// <summary>Compiles every rule of the pack against the target grammar and runs them over the graph.</summary>
    public static List<LabyrinthTaintFinding> RunRules(
        LabyrinthRuleSet ruleSet, string grammarContent, string grammarName,
        LabyrinthDataFlowGraph graph, IReadOnlyList<ILabyrinthMatchNode> nodes)
    {
        var findings = new List<LabyrinthTaintFinding>();
        using var parser = new LabyrinthPatternParser(grammarContent, grammarName);
        var expressionCompiler = new LabyrinthExpressionCompiler();
        foreach (var rule in ruleSet.Rules)
        {
            var matchers = expressionCompiler.CompileRule(LabyrinthRuleCompiler.Compile(rule, parser));
            if (rule.TypeValue == LabyrinthRuleType.Taint)
            {
                findings.AddRange(new LabyrinthTaintEngine(matchers, graph).Analyze());
            }
            else
            {
                var context = new LabyrinthMatchContext();
                foreach (var node in nodes)
                {
                    context.Clear();
                    if (matchers.SearchPattern is not null && matchers.SearchPattern(node, context))
                    {
                        findings.Add(new LabyrinthTaintFinding(
                            rule.Id, rule.SeverityValue, rule.EffectiveMessage, node, node,
                            new ILabyrinthMatchNode[] { node }));
                    }
                }
            }
        }

        return findings;
    }

    private static bool IsSkippable(StepToken token)
    {
        var type = token.Type.ToUpperInvariant();
        return type.Contains("WS") || type.Contains("COMMENT");
    }

    private static bool IsIdent(StepToken token)
    {
        var type = token.Type.ToUpperInvariant();
        return type.Contains("IDENT") || type == "NAME";
    }

    private static SourceMatchNode Intern(
        IReadOnlyList<string> parts, string source, string filePath,
        Dictionary<string, SourceMatchNode> intern, List<SourceMatchNode> all, int position)
    {
        var key = string.Join(".", parts);
        if (!intern.TryGetValue(key, out var node))
        {
            node = new SourceMatchNode("Identifier", key, Array.Empty<ILabyrinthMatchNode>(), filePath, position, source);
            intern[key] = node;
            all.Add(node);
        }

        return node;
    }

    private static ILabyrinthMatchNode MakeLhs(
        IReadOnlyList<string> parts, string source, string filePath,
        Dictionary<string, SourceMatchNode> intern, List<SourceMatchNode> all, int position)
    {
        if (parts.Count == 1)
        {
            return Intern(parts, source, filePath, intern, all, position);
        }

        var sequence = new List<ILabyrinthMatchNode>();
        for (var k = 0; k < parts.Count; k++)
        {
            sequence.Add(Intern(new[] { parts[k] }, source, filePath, intern, all, position));
            if (k < parts.Count - 1)
            {
                var dot = new SourceMatchNode("DOT", ".", Array.Empty<ILabyrinthMatchNode>(), filePath, position, source);
                all.Add(dot);
                sequence.Add(dot);
            }
        }

        return new SourceMatchNode("Expression", null, sequence, filePath, position, source);
    }

    private static List<ILabyrinthMatchNode> ParseExpr(
        List<StepToken> tokens, ref int i, string source, string filePath,
        Dictionary<string, SourceMatchNode> intern, List<SourceMatchNode> all, HashSet<string> stops)
    {
        var result = new List<ILabyrinthMatchNode>();
        while (i < tokens.Count)
        {
            var token = tokens[i];
            if (stops.Contains(token.Value))
            {
                break;
            }

            // Pattern v2 (#247): the `as` operator is lexed as an
            // identifier-shaped token by most grammars. When a primary
            // operand was just produced it is a binary operator, not an
            // identifier occurrence: materialize op:as(left, right).
            if (result.Count > 0 && token.Value == "as")
            {
                var asPos = token.StartPosition;
                i++;
                var rhsStopsAs = new HashSet<string>(StringComparer.Ordinal) { ",", ")", "]", ";" };
                var rhs = ParseExpr(tokens, ref i, source, filePath, intern, all, rhsStopsAs);
                if (rhs.Count > 0)
                {
                    var rhsNode = rhs.Count == 1
                        ? rhs[0]
                        : new SourceMatchNode("Expression", null, rhs, filePath, asPos, source);
                    var op = new SourceMatchNode(
                        "FunctionCall", "op:as",
                        new ILabyrinthMatchNode[] { result[^1], rhsNode }, filePath, asPos, source);
                    all.Add(op);
                    result[^1] = op;
                }

                continue;
            }

            if (!IsIdent(token))
            {
                var literal = new SourceMatchNode(token.Type, token.Value, Array.Empty<ILabyrinthMatchNode>(), filePath, token.StartPosition, source);
                all.Add(literal);
                result.Add(literal);
                i++;
                continue;
            }

            var parts = new List<string> { token.Value };
            var condAccess = false;
            var startPos = token.StartPosition;
            i++;
            // Pattern v2 (#247): dotted member access, with `?.`
            // (conditional access) accepted as a marker that is kept in the
            // materialized Name so rules can tell safe deref from plain.
            while (i < tokens.Count)
            {
                if (i + 1 < tokens.Count && tokens[i].Value == "." && IsIdent(tokens[i + 1]))
                {
                    parts.Add(tokens[i + 1].Value);
                    i += 2;
                    continue;
                }

                // `?.` may lex as one token or as a "?" "." pair.
                if (i + 1 < tokens.Count && tokens[i].Value == "?." && IsIdent(tokens[i + 1]))
                {
                    parts.Add(tokens[i + 1].Value);
                    condAccess = true;
                    i += 2;
                    continue;
                }

                if (i + 2 < tokens.Count && tokens[i].Value == "?" && tokens[i + 1].Value == "." && IsIdent(tokens[i + 2]))
                {
                    parts.Add(tokens[i + 2].Value);
                    condAccess = true;
                    i += 3;
                    continue;
                }

                break;
            }

            if (i < tokens.Count && (tokens[i].Value == "(" || tokens[i].Value == "["))
            {
                var isIndex = tokens[i].Value == "[";
                var close = isIndex ? "]" : ")";
                i++;
                var callArgs = new List<ILabyrinthMatchNode>();
                var argStops = new HashSet<string>(StringComparer.Ordinal) { ",", close };
                while (i < tokens.Count && tokens[i].Value != close)
                {
                    if (tokens[i].Value == ",")
                    {
                        i++;
                        continue;
                    }

                    var before = i;
                    var arg = ParseExpr(tokens, ref i, source, filePath, intern, all, argStops);
                    if (i == before)
                    {
                        i++;
                    }

                    callArgs.Add(arg.Count == 1
                        ? arg[0]
                        : new SourceMatchNode("Expression", null, arg, filePath, startPos, source));
                }

                if (i < tokens.Count)
                {
                    i++;
                }

                // Pattern v2 (#247): dotted calls keep the full dotted Name
                // (v1 behaviour) and materialize the receiver so patterns can
                // bind/unify on it; `?.` stays visible in the Name; index
                // expressions become IndexAccess nodes with the array as
                // the receiver. Plain (non-dotted) calls have no receiver.
                var name = condAccess
                    ? string.Join(".", parts.Take(parts.Count - 1)) + "?." + parts[^1]
                    : string.Join(".", parts);
                var receiver = parts.Count > 1 || isIndex
                    ? Intern(parts.GetRange(0, Math.Max(1, parts.Count - 1)), source, filePath, intern, all, startPos)
                    : null;
                var node = new SourceMatchNode(
                    isIndex ? "IndexAccess" : "FunctionCall", name, callArgs, filePath, startPos, source, receiver);
                all.Add(node);
                result.Add(WithBinaryOperatorTail(tokens, ref i, node, source, filePath, intern, all));
            }
            else if (i < tokens.Count && tokens[i].Value == "=")
            {
                i++;
                var lhs = MakeLhs(parts, source, filePath, intern, all, startPos);
                var rhsStops = new HashSet<string>(StringComparer.Ordinal) { ";", ")", "," };
                var rhs = ParseExpr(tokens, ref i, source, filePath, intern, all, rhsStops);
                var rhsNode = rhs.Count == 1
                    ? rhs[0]
                    : new SourceMatchNode("Expression", null, rhs, filePath, startPos, source);
                // Pattern v2 (#247): the assignment sits at the START of its
                // right-hand side in the document-order chain, after the
                // RHS nodes, so taint produced by RHS sources (e.g. a
                // maybe-null `FirstOrDefault()` value) reaches the
                // assignment's propagator before flowing on.
                var assignPos = rhs.Count > 0 ? rhs[0].Position : startPos;
                var assignment = new SourceMatchNode("Assignment", null, new ILabyrinthMatchNode[] { lhs, rhsNode }, filePath, assignPos, source);
                all.Add(assignment);
                result.Add(assignment);
            }
            else
            {
                result.Add(WithBinaryOperatorTail(
                    tokens, ref i, Intern(parts, source, filePath, intern, all, startPos),
                    source, filePath, intern, all));
            }
        }

        return result;
    }

    /// <summary>
    /// Pattern v2 (#247): folds a left-associative chain of binary operators
    /// (<c>/</c>, <c>%</c>, <c>??</c>) onto <paramref name="left"/> until no
    /// operator token follows. Each operator materializes a
    /// <c>FunctionCall</c> named <c>op:divide</c> / <c>op:modulo</c> /
    /// <c>op:coalesce</c> whose two arguments are the operand nodes, so rules
    /// can use them as sources, sanitizers and sinks.
    /// </summary>
    private static ILabyrinthMatchNode WithBinaryOperatorTail(
        List<StepToken> tokens, ref int i, ILabyrinthMatchNode left,
        string source, string filePath, Dictionary<string, SourceMatchNode> intern, List<SourceMatchNode> all)
    {
        while (true)
        {
            if (i >= tokens.Count)
            {
                break;
            }

            var v = tokens[i].Value;
            var pair = v == "?" && i + 1 < tokens.Count && tokens[i + 1].Value == "?";
            var opName = v switch
            {
                "/" => "op:divide",
                "%" => "op:modulo",
                "??" => "op:coalesce",
                "?" when pair => "op:coalesce",
                _ => null
            };
            if (opName is null)
            {
                break;
            }

            var opPos = tokens[i].StartPosition;
            i += pair ? 2 : 1;
            var operandStops = new HashSet<string>(StringComparer.Ordinal) { ",", ")", "]", ";" };
            var before = i;
            var rhs = ParseExpr(tokens, ref i, source, filePath, intern, all, operandStops);
            if (i == before || rhs.Count == 0)
            {
                // No right operand after the operator: leave it be.
                break;
            }

            var rhsNode = rhs.Count == 1
                ? rhs[0]
                : new SourceMatchNode("Expression", null, rhs, filePath, opPos, source);
            var op = new SourceMatchNode(
                "FunctionCall", opName, new ILabyrinthMatchNode[] { left, rhsNode }, filePath, opPos, source);
            all.Add(op);
            left = op;
        }

        return left;
    }
}
