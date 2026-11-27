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
        string filePath, int position, string source,
        ILabyrinthMatchNode? receiver = null, bool isNullConditional = false)
    {
        NodeType = nodeType;
        Name = name;
        Arguments = arguments;
        Receiver = receiver;
        IsNullConditional = isNullConditional;
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
    public bool IsNullConditional { get; }
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
    /// <para>
    /// Materialization conventions (pattern v2, issue #247):
    /// <list type="bullet">
    /// <item><c>x.DoWork()</c> ⇒ <c>FunctionCall</c> Name=<c>DoWork</c> with
    /// <c>Receiver</c> = the interned <c>x</c> identifier node.</item>
    /// <item><c>x?.DoWork()</c> ⇒ same, with <c>IsNullConditional</c> = true
    /// (the pattern-side plain <c>$X.DoWork()</c> deliberately does not match
    /// safe derefs).</item>
    /// <item><c>a[i]</c> ⇒ <c>IndexAccess</c> with <c>Receiver</c> = the array
    /// expression node and the index expressions as arguments.</item>
    /// <item><c>x / y</c>, <c>x % y</c>, <c>x as T</c>, <c>x ?? y</c> ⇒
    /// <c>FunctionCall</c> Name=<c>op:divide</c>/<c>op:modulo</c>/<c>op:as</c>/
    /// <c>op:coalesce</c> with the two operands as arguments, folded
    /// left-associatively.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static LabyrinthDataFlowGraph BuildGraph(
        string source, string filePath, string grammarContent, string grammarName, out List<ILabyrinthMatchNode> nodes)
    {
        using var engine = new StepParserEngine();
        engine.LoadGrammarFromContent(grammarContent, grammarName);
        var result = engine.Parse(source, filePath);

        // The exploratory lexer emits one token per matching rule fork into
        // result.Tokens, so fragments that the target grammar can lex two
        // ways (?? as one token or as two ?, ... as one ellipsis or as three
        // dots) appear as overlapping readings in the union. Resolve to the
        // deterministic longest-match tiling before materializing (issue #247).
        var tokens = LabyrinthTokenTiling.Resolve(result.Tokens).Where(t => !IsSkippable(t)).ToList();
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

    // In-process compile caches (Minotaur issue #283): scanning many files
    // with one rule pack must not recompile the pack per file. The pattern
    // parser (grammar load) is cached per grammar name; compiled rules are
    // cached per (rule instance, grammar); matchers per compiled rule. The
    // grammar content is assumed stable per grammar name within the process —
    // true for a CLI run, which resolves one grammar per scan.
    private static readonly object CompileGate = new();
    private static readonly Dictionary<string, LabyrinthPatternParser> PatternParsers = new(StringComparer.Ordinal);
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<LabyrinthRule, Dictionary<string, CompiledLabyrinthRule>> CompiledRules = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CompiledLabyrinthRule, LabyrinthCompiledMatchers> Matchers = new();

    /// <summary>
    /// Compiles every rule of the pack against the target grammar and runs them over
    /// the graph. When <paramref name="filePath"/> is given, rules with a
    /// <c>paths:</c> include/exclude filter only apply when the filter matches it.
    /// Compilation is cached in-process (issue #283): the grammar parses once
    /// per grammar name and each rule compiles once per run, not once per file.
    /// </summary>
    public static List<LabyrinthTaintFinding> RunRules(
        LabyrinthRuleSet ruleSet, string grammarContent, string grammarName,
        LabyrinthDataFlowGraph graph, IReadOnlyList<ILabyrinthMatchNode> nodes,
        string? filePath = null)
    {
        var parser = PatternParserFor(grammarContent, grammarName);
        var compiled = new List<CompiledLabyrinthRule>();
        foreach (var rule in ruleSet.RulesForPath(filePath))
        {
            compiled.Add(CompiledRuleFor(rule, parser, grammarName));
        }

        return RunCompiledRules(compiled, graph, nodes, filePath);
    }

    /// <summary>
    /// Runs already-compiled rules (from a binary rule pack or a prior
    /// <see cref="RunRules"/> compile) over the graph — no grammar and no
    /// pattern parsing involved (issue #283, P2). Delegates compile on first
    /// use and are cached per compiled rule for the process lifetime.
    /// </summary>
    public static List<LabyrinthTaintFinding> RunCompiledRules(
        IReadOnlyList<CompiledLabyrinthRule> compiledRules,
        LabyrinthDataFlowGraph graph, IReadOnlyList<ILabyrinthMatchNode> nodes,
        string? filePath = null)
    {
        var findings = new List<LabyrinthTaintFinding>();
        var expressionCompiler = new LabyrinthExpressionCompiler();
        // Child-to-parent map over the materialized node tree, enabling pattern-inside.
        var parentMap = LabyrinthMatchContext.BuildParentMap(nodes);
        foreach (var compiled in compiledRules)
        {
            var rule = compiled.Rule;
            if (rule.Paths is not null && !rule.Paths.Matches(filePath))
            {
                continue;
            }

            var matchers = Matchers.GetValue(compiled, _ => expressionCompiler.CompileRule(compiled));
            if (rule.TypeValue == LabyrinthRuleType.Taint)
            {
                findings.AddRange(new LabyrinthTaintEngine(matchers, graph).Analyze());
            }
            else
            {
                var context = new LabyrinthMatchContext { ParentMap = parentMap };
                foreach (var node in nodes)
                {
                    context.Clear();
                    if (matchers.SearchPattern is not null && matchers.SearchPattern(node, context))
                    {
                        // focus-metavariable: report at the bound node when declared.
                        var reportNode = matchers.ResolveReportNode(node, context);
                        // fix: interpolate the rule's fix suggestion against the bindings.
                        var fix = string.IsNullOrEmpty(rule.Fix) ? null : LabyrinthFixInterpolator.Interpolate(rule.Fix, context);
                        findings.Add(new LabyrinthTaintFinding(
                            rule.Id, rule.SeverityValue, rule.EffectiveMessage, reportNode, reportNode,
                            new ILabyrinthMatchNode[] { reportNode }, fix,
                            rule.Source, rule.SourceUrl, rule.License));
                    }
                }
            }
        }

        return findings;
    }

    private static LabyrinthPatternParser PatternParserFor(string grammarContent, string grammarName)
    {
        lock (CompileGate)
        {
            if (!PatternParsers.TryGetValue(grammarName, out var parser))
            {
                parser = new LabyrinthPatternParser(grammarContent, grammarName);
                PatternParsers.Add(grammarName, parser);
            }

            return parser;
        }
    }

    private static CompiledLabyrinthRule CompiledRuleFor(LabyrinthRule rule, LabyrinthPatternParser parser, string grammarName)
    {
        var perGrammar = CompiledRules.GetOrCreateValue(rule);
        lock (perGrammar)
        {
            if (!perGrammar.TryGetValue(grammarName, out var compiled))
            {
                compiled = LabyrinthRuleCompiler.Compile(rule, parser);
                perGrammar[grammarName] = compiled;
            }

            return compiled;
        }
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

    /// <summary>
    /// Identifier-like token: matches the target grammar's identifier kind (whose
    /// token rule name varies across grammars) or, by value, any bare word —
    /// so grammar keywords such as <c>string</c> or <c>value</c> lex as usable
    /// identifiers. Pure punctuation/operator values never look like words.
    /// </summary>
    private static bool IsIdentLike(StepToken token) =>
        IsIdent(token) || (token.Value.Length > 0
            && (char.IsLetter(token.Value[0]) || token.Value[0] == '_')
            && token.Value.All(c => char.IsLetterOrDigit(c) || c == '_'));

    /// <summary>
    /// Recognizes the v2 binary operators at token position <paramref name="i"/>
    /// (value-based, so every target grammar lexes the same): <c>/</c>,
    /// <c>%</c>, <c>as</c>, and <c>??</c> (as one token or as two <c>?</c>).
    /// </summary>
    private static bool BinaryOperatorAt(List<StepToken> tokens, int i, out string opName, out int width)
    {
        opName = string.Empty;
        width = 0;
        var value = tokens[i].Value;
        if (value == "/")
        {
            opName = "op:divide";
            width = 1;
        }
        else if (value == "%")
        {
            opName = "op:modulo";
            width = 1;
        }
        else if (value == "as")
        {
            opName = "op:as";
            width = 1;
        }
        else if (value == "??")
        {
            opName = "op:coalesce";
            width = 1;
        }
        else if (value == "?" && i + 1 < tokens.Count && tokens[i + 1].Value == "?")
        {
            opName = "op:coalesce";
            width = 2;
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Folds v2 binary operators onto a materialized primary: for each
    /// operator token, parses the right-hand operand and wraps the pair in an
    /// op: node (<c>op:divide</c>, <c>op:modulo</c>, <c>op:as</c>,
    /// <c>op:coalesce</c>), left-associatively.
    /// </summary>
    private static ILabyrinthMatchNode WithBinaryOperatorTail(
        ILabyrinthMatchNode lhs, List<StepToken> tokens, ref int i, string source, string filePath,
        Dictionary<string, SourceMatchNode> intern, List<SourceMatchNode> all, HashSet<string> stops, int position)
    {
        while (i < tokens.Count && !stops.Contains(tokens[i].Value) && BinaryOperatorAt(tokens, i, out var opName, out var width))
        {
            i += width;

            // The operand parse must stop on further operator tokens: the
            // while loop here folds them left-associatively. Letting the
            // operand's own recursion fold them instead would produce a
            // right-leaning tree the pattern compiler does not match.
            var operandStops = new HashSet<string>(stops, StringComparer.Ordinal)
            {
                ";", ",", ")", "]", "/", "%", "as", "?", "??"
            };
            var rhsList = ParseExpr(tokens, ref i, source, filePath, intern, all, operandStops);
            var rhs = rhsList.Count switch
            {
                0 => throw new InvalidOperationException(
                    $"Binary operator '{opName}' without a right-hand operand at position {i - width}."),
                1 => rhsList[0],
                _ => new SourceMatchNode("Expression", null, rhsList, filePath, position, source)
            };
            var op = new SourceMatchNode("FunctionCall", opName, new ILabyrinthMatchNode[] { lhs, rhs }, filePath, position, source);
            all.Add(op);
            lhs = op;
        }

        return lhs;
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

            if (!IsIdent(token))
            {
                var literal = new SourceMatchNode(token.Type, token.Value, Array.Empty<ILabyrinthMatchNode>(), filePath, token.StartPosition, source);
                all.Add(literal);
                result.Add(literal);
                i++;
                continue;
            }

            var parts = new List<string> { token.Value };
            var startPos = token.StartPosition;
            var nullConditional = false;
            i++;

            // Member chain, including null-conditional links (?. / . ) — the
            // marker distinguishes safe derefs (v2).
            while (i < tokens.Count)
            {
                if (tokens[i].Value == "." && i + 1 < tokens.Count && IsIdentLike(tokens[i + 1]))
                {
                    parts.Add(tokens[i + 1].Value);
                    i += 2;
                }
                else if (tokens[i].Value == "?." && i + 1 < tokens.Count && IsIdentLike(tokens[i + 1]))
                {
                    nullConditional = true;
                    parts.Add(tokens[i + 1].Value);
                    i += 2;
                }
                else if (tokens[i].Value == "?" && i + 2 < tokens.Count
                    && tokens[i + 1].Value == "." && IsIdentLike(tokens[i + 2]))
                {
                    nullConditional = true;
                    parts.Add(tokens[i + 2].Value);
                    i += 3;
                }
                else
                {
                    break;
                }
            }

            if (i < tokens.Count && tokens[i].Value == "(")
            {
                i++;
                var callArgs = new List<ILabyrinthMatchNode>();
                var argStops = new HashSet<string>(StringComparer.Ordinal) { ",", ")" };
                while (i < tokens.Count && tokens[i].Value != ")")
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

                // v2: split receiver from method name; bare calls keep the
                // receiver-free FunctionCall of v1.
                SourceMatchNode? receiver = null;
                string callName;
                if (parts.Count > 1)
                {
                    var receiverParts = parts.Take(parts.Count - 1).ToList();
                    callName = parts[^1];
                    receiver = Intern(receiverParts, source, filePath, intern, all, startPos);
                }
                else
                {
                    callName = parts[0];
                }

                var call = new SourceMatchNode("FunctionCall", callName, callArgs, filePath, startPos, source, receiver, nullConditional);
                all.Add(call);
                result.Add(WithBinaryOperatorTail(call, tokens, ref i, source, filePath, intern, all, stops, startPos));
            }
            else if (i < tokens.Count && IsIndexOpen(tokens, i))
            {
                var conditionalIndex = tokens[i].Value != "[";
                if (tokens[i].Value == "?")
                {
                    i += 2; // the "? [" pair
                }
                else
                {
                    i++; // "[" or "?["
                }

                var indexArgs = new List<ILabyrinthMatchNode>();
                var argStops = new HashSet<string>(StringComparer.Ordinal) { ",", "]" };
                while (i < tokens.Count && tokens[i].Value != "]")
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

                    indexArgs.Add(arg.Count == 1
                        ? arg[0]
                        : new SourceMatchNode("Expression", null, arg, filePath, startPos, source));
                }

                if (i < tokens.Count)
                {
                    i++;
                }

                // v2: indexing materializes as IndexAccess with the indexed
                // expression exposed as the Receiver (taint binds to it).
                var receiver = Intern(parts, source, filePath, intern, all, startPos);
                var index = new SourceMatchNode("IndexAccess", null, indexArgs, filePath, startPos, source, receiver, conditionalIndex);
                all.Add(index);
                result.Add(WithBinaryOperatorTail(index, tokens, ref i, source, filePath, intern, all, stops, startPos));
            }
            else if (i < tokens.Count && tokens[i].Value == "=")
            {
                i++;
                var lhs = MakeLhs(parts, source, filePath, intern, all, startPos);
                var rhsStops = new HashSet<string>(StringComparer.Ordinal) { ";", ")", ",", "]" };
                var rhs = ParseExpr(tokens, ref i, source, filePath, intern, all, rhsStops);
                var rhsNode = rhs.Count == 1
                    ? rhs[0]
                    : new SourceMatchNode("Expression", null, rhs, filePath, startPos, source);
                var assignment = new SourceMatchNode("Assignment", null, new ILabyrinthMatchNode[] { lhs, rhsNode }, filePath, startPos, source);
                all.Add(assignment);
                result.Add(assignment);
            }
            else
            {
                var identifier = Intern(parts, source, filePath, intern, all, startPos);
                result.Add(WithBinaryOperatorTail(identifier, tokens, ref i, source, filePath, intern, all, stops, startPos));
            }
        }

        return result;
    }

    /// <summary>
    /// True when the token position opens an indexing bracket: <c>[</c>,
    /// a single-token null-conditional <c>?[</c>, or the <c>? [</c> pair.
    /// </summary>
    private static bool IsIndexOpen(List<StepToken> tokens, int i)
    {
        var value = tokens[i].Value;
        return value == "[" || value == "?["
            || (value == "?" && i + 1 < tokens.Count && tokens[i + 1].Value == "[");
    }
}
