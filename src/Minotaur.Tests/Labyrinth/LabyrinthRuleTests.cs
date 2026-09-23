using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Layer 1 tests for the Labyrinth rule engine (Minotaur issue #101):
/// YAML rule schema loading and metavariable/ellipsis pattern tokenization.
/// </summary>
public sealed class LabyrinthRuleLoaderTests
{
    private const string TaintRuleYaml = """
        rules:
          - id: custom-grammar-injection
            severity: ERROR
            message: "Untrusted data flows into a critical execution sink."
            type: taint
            sources:
              - pattern: "ReceiveData($DATA)"
            sinks:
              - pattern: "ExecuteAction(..., $DATA, ...)"
            sanitizers:
              - pattern: "VerifyIntegrity($DATA)"
            propagators:
              - pattern: "$TARGET = FormatString($SRC)"
                from: $SRC
                to: $TARGET
        """;

    private const string SearchRuleYaml = """
        rules:
          - id: advanced-size-check
            type: search
            pattern: "AllocateMemory($SIZE)"
            condition: "node.Arguments[0].Value > 1024 && node.Parent.Type != 'SafeBlock'"
        """;

    private readonly LabyrinthRuleLoader _loader = new();

    [Fact]
    public void Load_TaintRule_ParsesAllSections()
    {
        var ruleSet = _loader.Load(TaintRuleYaml);

        var rule = Assert.Single(ruleSet.Rules);
        Assert.Equal("custom-grammar-injection", rule.Id);
        Assert.Equal(LabyrinthSeverity.Error, rule.SeverityValue);
        Assert.Equal(LabyrinthRuleType.Taint, rule.TypeValue);
        Assert.Equal("Untrusted data flows into a critical execution sink.", rule.Message);

        Assert.NotNull(rule.Sources);
        Assert.Equal("ReceiveData($DATA)", Assert.Single(rule.Sources).Pattern);
        Assert.NotNull(rule.Sinks);
        Assert.Equal("ExecuteAction(..., $DATA, ...)", Assert.Single(rule.Sinks).Pattern);
        Assert.NotNull(rule.Sanitizers);
        Assert.Equal("VerifyIntegrity($DATA)", Assert.Single(rule.Sanitizers).Pattern);

        Assert.NotNull(rule.Propagators);
        var propagator = Assert.Single(rule.Propagators!);
        Assert.Equal("$TARGET = FormatString($SRC)", propagator.Pattern);
        Assert.Equal("$SRC", propagator.From);
        Assert.Equal("$TARGET", propagator.To);
    }

    [Fact]
    public void Load_SearchRule_ParsesPatternAndCondition()
    {
        var ruleSet = _loader.Load(SearchRuleYaml);

        var rule = Assert.Single(ruleSet.Rules);
        Assert.Equal("advanced-size-check", rule.Id);
        Assert.Equal(LabyrinthRuleType.Search, rule.TypeValue);
        Assert.Equal(LabyrinthSeverity.Warning, rule.SeverityValue);
        Assert.Equal("advanced-size-check", rule.EffectiveMessage);
        Assert.Equal("AllocateMemory($SIZE)", rule.Pattern);
        Assert.Contains("node.Arguments[0].Value > 1024", rule.Condition);
    }

    [Fact]
    public void Load_MergedExampleRuleFile_ContainsBothRules()
    {
        var yaml = """
            rules:
              - id: custom-grammar-injection
                severity: ERROR
                message: "Untrusted data flows into a critical execution sink."
                type: taint
                sources:
                  - pattern: "ReceiveData($DATA)"
                sinks:
                  - pattern: "ExecuteAction(..., $DATA, ...)"
              - id: advanced-size-check
                type: search
                pattern: "AllocateMemory($SIZE)"
                condition: "node.Arguments[0].Value > 1024"
            """;
        var ruleSet = _loader.Load(yaml);

        Assert.Equal(2, ruleSet.Rules.Count);
        Assert.All(ruleSet.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Id)));
        Assert.Equal(new[] { "custom-grammar-injection", "advanced-size-check" },
            ruleSet.Rules.Select(r => r.Id));
    }

    [Fact]
    public void Load_EmptyFile_Throws()
    {
        Assert.Throws<LabyrinthRuleException>(() => _loader.Load(""));
        Assert.Throws<LabyrinthRuleException>(() => _loader.Load("# only a comment\n"));
    }

    [Fact]
    public void Load_MalformedYaml_ThrowsWithClearMessage()
    {
        var ex = Assert.Throws<LabyrinthRuleException>(() => _loader.Load("rules: ["));
        Assert.Contains("Invalid rule file YAML", ex.Message);
    }

    [Fact]
    public void Validate_TaintRuleWithoutSourcesAndSinks_ReportsErrors()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: broken-taint
                type: taint
                message: "m"
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least one source"));
        Assert.Contains(result.Errors, e => e.Contains("at least one sink"));
    }

    [Fact]
    public void Validate_SearchRuleWithoutPattern_ReportsError()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: broken-search
                type: search
                message: "m"
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("search rules require a 'pattern'"));
    }

    [Fact]
    public void Validate_DuplicateRuleIds_ReportsError()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: dup
                type: search
                message: "m"
                pattern: "Foo($X)"
              - id: dup
                type: search
                message: "m2"
                pattern: "Bar($X)"
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("duplicate rule id 'dup'"));
    }

    [Fact]
    public void Validate_UnknownSeverityAndType_ReportsErrors()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: bad-values
                severity: CRITICAL
                type: regex
                message: "m"
                pattern: "Foo($X)"
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown severity 'CRITICAL'"));
        Assert.Contains(result.Errors, e => e.Contains("Unknown rule type 'regex'"));
    }

    [Fact]
    public void Validate_PropagatorWithUnboundMetavariable_ReportsError()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: bad-propagator
                type: taint
                message: "m"
                sources:
                  - pattern: "ReceiveData($DATA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
                propagators:
                  - pattern: "$TARGET = FormatString($SRC)"
                    from: $SRC
                    to: $MISSING
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'to' metavariable '$MISSING' does not occur in the propagator pattern"));
    }

    [Fact]
    public void Validate_CrossModeKeys_ReportsErrors()
    {
        var ruleSet = _loader.Parse("""
            rules:
              - id: mixed-modes
                type: taint
                message: "m"
                pattern: "Foo($X)"
                sources:
                  - pattern: "ReceiveData($DATA)"
                sinks:
                  - pattern: "ExecuteAction($DATA)"
            """);

        var result = _loader.Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'pattern' is only valid for type 'search'"));
    }
}

/// <summary>
/// Tests for <see cref="LabyrinthMetavariables"/> (schema-validation helper only;
/// pattern bodies are parsed by DevelApp.StepParser with the target grammar once
/// ENFAStepLexer-StepParser#65/#66 land).
/// </summary>
public sealed class LabyrinthMetavariablesTests
{
    [Fact]
    public void NamesIn_ReturnsAllNamesInOrder_WithDuplicates()
    {
        var names = LabyrinthMetavariables.NamesIn("f($A, $B, $A)");
        Assert.Equal(new[] { "A", "B", "A" }, names);
    }

    [Fact]
    public void NamesIn_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Empty(LabyrinthMetavariables.NamesIn(null));
        Assert.Empty(LabyrinthMetavariables.NamesIn(""));
        Assert.Empty(LabyrinthMetavariables.NamesIn("no metas here"));
    }

    [Fact]
    public void NamesIn_LowercaseIdentifier_IsNotAMetavariable()
    {
        Assert.Empty(LabyrinthMetavariables.NamesIn("f($data)"));
    }

    [Fact]
    public void NamesIn_TrailingDollarAlone_IsIgnored()
    {
        Assert.Equal(new[] { "A" }, LabyrinthMetavariables.NamesIn("f($A, $)"));
    }

    [Fact]
    public void DistinctNamesIn_RemovesDuplicates_PreservingFirstOccurrence()
    {
        var names = LabyrinthMetavariables.DistinctNamesIn("f($A, $B, $A)");
        Assert.Equal(new[] { "A", "B" }, names);
    }
}

/// <summary>
/// Tests for <see cref="LabyrinthRulePackLoader"/>.
/// </summary>
public sealed class LabyrinthRulePackLoaderTests
{
    [Fact]
    public void LoadDirectory_MergesRuleFilesAndReportsRelativePaths()
    {
        var loader = new LabyrinthRulePackLoader(new LabyrinthRuleLoader());

        var ex = Record.Exception(() => loader.LoadDirectory("/nonexistent/labyrinth-rules"));
        Assert.IsType<LabyrinthRuleException>(ex);
        Assert.Contains("Rule directory does not exist", ex!.Message);
    }
}
