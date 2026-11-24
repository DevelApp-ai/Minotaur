using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for the <c>pattern-either</c> rule-list entry (Labyrinth issue #246,
/// LAB track): an entry may list alternatives via <c>pattern-either:</c> instead of
/// <c>pattern:</c>; the entry matches when ANY alternative matches. The two keys are
/// mutually exclusive, nesting pattern-either inside an alternative is rejected, and
/// the entry-level condition applies to every alternative.
/// </summary>
public sealed class LabyrinthPatternEitherTests
{
    private readonly LabyrinthPatternParser _parser;

    private const string TestGrammar = """
        Grammar: TestLang
        FormatType: EBNF

        <WS> ::= /[ \\t\\r\\n]+/ => { skip(); }
        <IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
        <NUMBER> ::= /[0-9]+/
        <LPAREN> ::= "("
        <RPAREN> ::= ")"
        <COMMA> ::= ","
        <EQUALS> ::= "="

        <expr> ::= <term> ;
        <term> ::= IDENTIFIER | NUMBER | LPAREN | RPAREN | COMMA | EQUALS | STRING ;
        """;

    public LabyrinthPatternEitherTests()
    {
        _parser = new LabyrinthPatternParser(TestGrammar, "TestLang");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static LabyrinthMatchNode Call(string name, params ILabyrinthMatchNode[] args) =>
        new("FunctionCall", name, args);

    private static ILabyrinthMatchNode Ident(string name) => new LabyrinthMatchNode("Identifier", name);

    private static LabyrinthRule EitherSourceRule(string? condition = null) => new()
    {
        Id = "either-taint",
        Severity = "ERROR",
        Type = "taint",
        Sources = new List<LabyrinthPatternEntry>
        {
            new()
            {
                Condition = condition,
                PatternEither = new List<LabyrinthPatternEntry>
                {
                    new() { Pattern = "ReceiveData($DATA)" },
                    new() { Pattern = "ReadFile($DATA)" },
                },
            },
        },
        Sinks = new List<LabyrinthPatternEntry>
        {
            new() { Pattern = "ExecuteAction($DATA)" },
        },
    };

    // ------------------------------------------------------------------
    // YAML loader: pattern-either parsing and validation
    // ------------------------------------------------------------------

    [Fact]
    public void YamlLoader_ParsesPatternEitherAlternatives()
    {
        const string yaml = """
            rules:
              - id: either-source
                severity: ERROR
                type: taint
                sources:
                  - pattern-either:
                      - pattern: "ReceiveData($DATA)"
                      - pattern: "ReadFile($DATA, $EXTRA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ruleSet = new LabyrinthRuleLoader().Load(yaml);

        var rule = Assert.Single(ruleSet.Rules);
        var entry = Assert.Single(rule.Sources!);
        Assert.True(string.IsNullOrEmpty(entry.Pattern));
        Assert.NotNull(entry.PatternEither);
        Assert.Equal(2, entry.PatternEither!.Count);
        Assert.Equal("ReceiveData($DATA)", entry.PatternEither[0].Pattern);
        Assert.Equal("ReadFile($DATA, $EXTRA)", entry.PatternEither[1].Pattern);

        // Metavariable enumeration descends into the alternatives:
        // $EXTRA only occurs in the second alternative.
        var metavars = LabyrinthRuleSet.DefinedMetavariables(rule).ToList();
        Assert.Contains("DATA", metavars);
        Assert.Contains("EXTRA", metavars);
    }

    [Fact]
    public void YamlLoader_RejectsPatternAndPatternEitherTogether()
    {
        const string yaml = """
            rules:
              - id: both-keys
                severity: ERROR
                type: taint
                sources:
                  - pattern: "ReceiveData($DATA)"
                    pattern-either:
                      - pattern: "ReadFile($DATA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void YamlLoader_RejectsNestedPatternEither()
    {
        const string yaml = """
            rules:
              - id: nested-either
                severity: ERROR
                type: taint
                sources:
                  - pattern-either:
                      - pattern: "ReceiveData($DATA)"
                        pattern-either:
                          - pattern: "ReadFile($DATA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("nested pattern-either", ex.Message);
    }

    [Fact]
    public void YamlLoader_RejectsEntryWithNeitherPatternNorEither()
    {
        const string yaml = """
            rules:
              - id: empty-entry
                severity: ERROR
                type: taint
                sources:
                  - condition: "node.Name == \"ReceiveData\""
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """;

        var ex = Assert.Throws<LabyrinthRuleException>(() => new LabyrinthRuleLoader().Load(yaml));
        Assert.Contains("'pattern' is required", ex.Message);
    }

    // ------------------------------------------------------------------
    // Compiler: pattern-either compiles to the OR of its alternatives
    // ------------------------------------------------------------------

    [Fact]
    public void Compiler_PatternEither_ExposesAllAlternatives()
    {
        var compiled = LabyrinthRuleCompiler.Compile(EitherSourceRule(), _parser);

        var entry = Assert.Single(compiled.Sources);
        Assert.Equal(2, entry.Alternatives.Count);
        Assert.Equal("ReceiveData($DATA)", entry.Alternatives[0].Pattern);
        Assert.Equal("ReadFile($DATA)", entry.Alternatives[1].Pattern);
        // Compatibility shim: Pattern is the first alternative.
        Assert.Same(entry.Alternatives[0], entry.Pattern);
    }

    [Fact]
    public void Compiler_PatternEither_MatchesAnyAlternative_AndBindsMetavariable()
    {
        var compiled = LabyrinthRuleCompiler.Compile(EitherSourceRule(), _parser);
        var matchers = new LabyrinthExpressionCompiler().CompileRule(compiled);
        var source = Assert.Single(matchers.Sources);

        // First alternative matches.
        var ctxA = new LabyrinthMatchContext();
        var argA = Ident("userInput");
        Assert.True(source(Call("ReceiveData", argA), ctxA));
        Assert.True(ctxA.TryGet("DATA", out var boundA));
        Assert.Same(argA, boundA);

        // Second alternative matches.
        var ctxB = new LabyrinthMatchContext();
        var argB = Ident("configFile");
        Assert.True(source(Call("ReadFile", argB), ctxB));
        Assert.True(ctxB.TryGet("DATA", out var boundB));
        Assert.Same(argB, boundB);

        // Neither alternative matches.
        Assert.False(source(Call("WriteLog", Ident("x")), new LabyrinthMatchContext()));
        Assert.False(source(Ident("notACall"), new LabyrinthMatchContext()));
    }

    [Fact]
    public void Compiler_PatternEither_ConditionAppliesToEveryAlternative()
    {
        var compiled = LabyrinthRuleCompiler.Compile(EitherSourceRule("node.Name == \"ReceiveData\""), _parser);
        var matchers = new LabyrinthExpressionCompiler().CompileRule(compiled);
        var source = Assert.Single(matchers.Sources);

        // Alternative 1 satisfies the condition.
        Assert.True(source(Call("ReceiveData", Ident("u")), new LabyrinthMatchContext()));
        // Alternative 2 matches structurally but the entry condition rejects it.
        Assert.False(source(Call("ReadFile", Ident("f")), new LabyrinthMatchContext()));
    }

    [Fact]
    public void Compiler_PatternEither_InSinks_AlsoMatchesAnyAlternative()
    {
        var rule = new LabyrinthRule
        {
            Id = "either-sink",
            Severity = "ERROR",
            Type = "taint",
            Sources = new List<LabyrinthPatternEntry>
            {
                new() { Pattern = "ReceiveData($DATA)" },
            },
            Sinks = new List<LabyrinthPatternEntry>
            {
                new()
                {
                    PatternEither = new List<LabyrinthPatternEntry>
                    {
                        new() { Pattern = "ExecuteAction($DATA)" },
                        new() { Pattern = "SendOverNetwork($DATA)" },
                    },
                },
            },
        };

        var compiled = LabyrinthRuleCompiler.Compile(rule, _parser);
        var matchers = new LabyrinthExpressionCompiler().CompileRule(compiled);
        var sink = Assert.Single(matchers.Sinks);

        Assert.True(sink(Call("ExecuteAction", Ident("d")), new LabyrinthMatchContext()));
        Assert.True(sink(Call("SendOverNetwork", Ident("d")), new LabyrinthMatchContext()));
        Assert.False(sink(Call("WriteLog", Ident("d")), new LabyrinthMatchContext()));
    }
}
