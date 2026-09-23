using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Layer 1 pattern-parsing tests for the Labyrinth rule engine (Minotaur issue
/// #101): pattern snippets are parsed into mini-ASTs through the real
/// DevelApp.StepLexer/StepParser pipeline with the target grammar and the
/// standard metavariable/ellipsis pattern tokens, and rules are compiled into
/// a rule-level unification table (input for Layer 2, issue #102).
/// </summary>
public sealed class LabyrinthPatternParserTests
{
    /// <summary>
    /// A minimal expression-ish target grammar for tests, using only token
    /// patterns the DevelApp.StepLexer matcher supports (identifier, number,
    /// quoted literals plus the injected metavariable/ellipsis tokens).
    /// Pattern snippets are deliberately partial (they do not have to satisfy
    /// the production rules); what matters is the token vocabulary.
    /// </summary>
    private const string TestGrammar = """
        Grammar: TestLang
        TokenSplitter: Space
        FormatType: EBNF

        <WS> ::= /[ \t\r\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <EQUALS> ::= "="

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | EQUALS ;
        """;

    private static LabyrinthPatternParser CreateParser() =>
        new(TestGrammar, "TestLang");

    private static string NodeTypes(LabyrinthPatternAst ast) =>
        string.Join(" ", ast.Nodes.Select(n => n.TokenType));

    // ---------------------------------------------------------------------
    // Pattern parsing (issue #101: StepParser pattern -> mini-AST)
    // ---------------------------------------------------------------------

    [Fact]
    public void ParsePattern_SimpleCall_MapsTokensToLiteralAndMetavariableNodes()
    {
        using var parser = CreateParser();

        var ast = parser.ParsePattern("ReceiveData($DATA)");

        Assert.Equal(
            "IDENTIFIER LPAREN METAVARIABLE RPAREN",
            NodeTypes(ast));
        Assert.Equal(LabyrinthPatternNodeKind.Literal, ast.Nodes[0].Kind);
        Assert.Equal("ReceiveData", ast.Nodes[0].Value);
        Assert.Equal(LabyrinthPatternNodeKind.Metavariable, ast.Nodes[2].Kind);
        Assert.Equal("$DATA", ast.Nodes[2].Value);
        Assert.Equal("DATA", ast.Nodes[2].MetavariableName);
        Assert.Equal("DATA", Assert.Single(ast.Metavariables));
    }

    [Fact]
    public void ParsePattern_WithEllipsis_MapsEllipsisNodes()
    {
        using var parser = CreateParser();

        var ast = parser.ParsePattern("ExecuteAction(..., $DATA, ...)");

        Assert.Equal(
            "IDENTIFIER LPAREN ELLIPSIS COMMA METAVARIABLE COMMA ELLIPSIS RPAREN",
            NodeTypes(ast));
        Assert.Equal(LabyrinthPatternNodeKind.Ellipsis, ast.Nodes[2].Kind);
        Assert.Equal("...", ast.Nodes[2].Value);
    }

    [Fact]
    public void ParsePattern_NodePositionsAreSourcePositions()
    {
        using var parser = CreateParser();

        var ast = parser.ParsePattern("ReadInput($DATA)");

        Assert.Equal(0, ast.Nodes[0].StartPosition);
        Assert.Equal("ReadInput".Length, ast.Nodes[0].Length);
        Assert.Equal("ReadInput(".Length, ast.Nodes[2].StartPosition);
        Assert.Equal("$DATA".Length, ast.Nodes[2].Length);
    }

    [Fact]
    public void ParsePattern_PartialSnippet_ToleratesParserErrors()
    {
        using var parser = CreateParser();

        // Unbalanced parenthesis: not a syntactically valid <expr>, but the
        // snippet tokenizes fine - and tokenization is what Layer 1 needs.
        var ast = parser.ParsePattern("ReceiveData($DATA");

        Assert.Equal("IDENTIFIER LPAREN METAVARIABLE", NodeTypes(ast));
    }

    [Fact]
    public void ParsePattern_MultipleMetavariables_ListsDistinctInOrder()
    {
        using var parser = CreateParser();

        var ast = parser.ParsePattern("$TARGET = FormatString($SRC, $DATA)");

        Assert.Equal(
            "METAVARIABLE EQUALS IDENTIFIER LPAREN METAVARIABLE COMMA METAVARIABLE RPAREN",
            NodeTypes(ast));
        Assert.Equal(new[] { "TARGET", "SRC", "DATA" }, ast.Metavariables);
    }

    [Fact]
    public void ParsePattern_LowercaseMetavariable_ThrowsBecauseLexerCannotTokenize()
    {
        using var parser = CreateParser();

        // '$data' does not match the metavariable token rule (lowercase start),
        // and nothing else in the target grammar matches '$', so the lexer
        // cannot consume the input: a genuine lexer-phase failure.
        var ex = Assert.Throws<LabyrinthRuleException>(() => parser.ParsePattern("f($data)"));
        Assert.Contains("cannot be tokenized", ex.Message);
    }

    [Fact]
    public void ParsePattern_UnknownInput_Throws()
    {
        using var parser = CreateParser();

        var ex = Assert.Throws<LabyrinthRuleException>(() => parser.ParsePattern("###"));
        Assert.Contains("cannot be tokenized", ex.Message);
    }

    [Fact]
    public void ParsePattern_EmptyPattern_YieldsEmptyAst()
    {
        using var parser = CreateParser();

        var ast = parser.ParsePattern("   ");

        Assert.Empty(ast.Nodes);
        Assert.Empty(ast.Metavariables);
    }

    [Fact]
    public void Parser_ReusesEngineAcrossPatterns()
    {
        using var parser = CreateParser();

        var first = parser.ParsePattern("ReadInput($DATA)");
        var second = parser.ParsePattern("WriteOutput($DATA)");

        Assert.Equal("IDENTIFIER LPAREN METAVARIABLE RPAREN", NodeTypes(first));
        Assert.Equal("IDENTIFIER LPAREN METAVARIABLE RPAREN", NodeTypes(second));
    }

    [Fact]
    public void Parser_InvalidGrammar_Throws()
    {
        var ex = Assert.Throws<LabyrinthRuleException>(() =>
            new LabyrinthPatternParser("this is not a grammar at all", "broken"));
        Assert.Contains("broken", ex.Message);
    }

    // ---------------------------------------------------------------------
    // Rule compilation (issue #101: rule-level unification)
    // ---------------------------------------------------------------------

    private static LabyrinthRule TaintRule() => new()
    {
        Id = "custom-grammar-injection",
        Severity = "ERROR",
        Message = "Untrusted data flows into a critical execution sink.",
        Type = "taint",
        Sources = [new LabyrinthPatternEntry { Pattern = "ReceiveData($DATA)" }],
        Sinks = [new LabyrinthPatternEntry { Pattern = "ExecuteAction(..., $DATA, ...)" }],
        Sanitizers = [new LabyrinthPatternEntry { Pattern = "VerifyIntegrity($DATA)" }],
        Propagators =
        [
            new LabyrinthPropagatorEntry { Pattern = "$TARGET = FormatString($SRC)", From = "$SRC", To = "$TARGET" }
        ]
    };

    [Fact]
    public void Compile_TaintRule_ParsesAllEntries()
    {
        using var parser = CreateParser();

        var compiled = LabyrinthRuleCompiler.Compile(TaintRule(), parser);

        Assert.Null(compiled.SearchPattern);
        var source = Assert.Single(compiled.Sources);
        Assert.Equal("ReceiveData($DATA)", source.Pattern.Pattern);
        Assert.Equal("IDENTIFIER LPAREN METAVARIABLE RPAREN", NodeTypes(source.Pattern));
        Assert.Equal(
            "IDENTIFIER LPAREN ELLIPSIS COMMA METAVARIABLE COMMA ELLIPSIS RPAREN",
            NodeTypes(Assert.Single(compiled.Sinks).Pattern));
        Assert.Single(compiled.Sanitizers);

        var propagator = Assert.Single(compiled.Propagators);
        Assert.Equal("SRC", propagator.From);
        Assert.Equal("TARGET", propagator.To);
        Assert.Equal(new[] { "TARGET", "SRC" }, propagator.Pattern.Metavariables);
    }

    [Fact]
    public void Compile_TaintRule_BuildsRuleLevelUnificationTable()
    {
        using var parser = CreateParser();

        var compiled = LabyrinthRuleCompiler.Compile(TaintRule(), parser);

        // $DATA occurs in source, sink and sanitizer; $TARGET and $SRC in the
        // propagator. The unification scope is the whole rule, so each name
        // is listed once with its total occurrence count across entries.
        Assert.Equal(new[] { "DATA", "TARGET", "SRC" }, compiled.Metavariables);
        Assert.Equal(3, compiled.MetavariableOccurrences["DATA"]);
        Assert.Equal(1, compiled.MetavariableOccurrences["TARGET"]);
        Assert.Equal(1, compiled.MetavariableOccurrences["SRC"]);
    }

    [Fact]
    public void Compile_SearchRule_ParsesSearchPattern()
    {
        using var parser = CreateParser();

        var rule = new LabyrinthRule
        {
            Id = "search-example",
            Severity = "WARNING",
            Type = "search",
            Pattern = "ReadConfig($PATH)",
            Condition = "node.Arguments[0].Value != \"safe\""
        };
        var compiled = LabyrinthRuleCompiler.Compile(rule, parser);

        Assert.NotNull(compiled.SearchPattern);
        Assert.Equal("IDENTIFIER LPAREN METAVARIABLE RPAREN", NodeTypes(compiled.SearchPattern));
        Assert.Equal(new[] { "PATH" }, compiled.Metavariables);
        Assert.Empty(compiled.Sources);
        Assert.Empty(compiled.Sinks);
        Assert.Empty(compiled.Propagators);
    }

    [Fact]
    public void Compile_PropagatorFromNotInPattern_Throws()
    {
        using var parser = CreateParser();

        var rule = TaintRule();
        rule.Propagators![0].From = "$MISSING";

        var ex = Assert.Throws<LabyrinthRuleException>(() => LabyrinthRuleCompiler.Compile(rule, parser));
        Assert.Contains("'from' metavariable '$MISSING'", ex.Message);
    }

    [Fact]
    public void Compile_PropagatorToNotInPattern_Throws()
    {
        using var parser = CreateParser();

        var rule = TaintRule();
        rule.Propagators![0].To = "$MISSING";

        var ex = Assert.Throws<LabyrinthRuleException>(() => LabyrinthRuleCompiler.Compile(rule, parser));
        Assert.Contains("'to' metavariable '$MISSING'", ex.Message);
    }

    [Fact]
    public void Compile_UntokenizableSinkPattern_Throws()
    {
        using var parser = CreateParser();

        var rule = TaintRule();
        rule.Sinks![0].Pattern = "ExecuteAction(..., ###, ...)";

        Assert.Throws<LabyrinthRuleException>(() => LabyrinthRuleCompiler.Compile(rule, parser));
    }

    [Fact]
    public void Compile_WithGrammarContent_CreatesOwnParser()
    {
        var compiled = LabyrinthRuleCompiler.Compile(
            new LabyrinthRule { Id = "search", Type = "search", Pattern = "ReadConfig($PATH)" },
            TestGrammar,
            "TestLang");

        Assert.Equal(new[] { "PATH" }, compiled.Metavariables);
    }
}
