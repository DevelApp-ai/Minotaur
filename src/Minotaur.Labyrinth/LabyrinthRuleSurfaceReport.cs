using System.Diagnostics;
using System.Text.Json;

namespace Minotaur.Labyrinth;

/// <summary>
/// Per-rule entry of a <see cref="LabyrinthRuleSurfaceReportData"/>: which
/// rule-language features the rule exercises and what compiling it cost.
/// </summary>
public sealed class LabyrinthRuleSurfaceEntry
{
    /// <summary>Rule id.</summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>Rule type as written (<c>search</c>/<c>taint</c>).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Rule severity as written.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Number of pattern entries (a search rule has one: its pattern).</summary>
    public int Entries { get; set; }

    /// <summary>Positive patterns across all entries (pattern-either alternatives counted individually).</summary>
    public int Patterns { get; set; }

    /// <summary>Distinct metavariables defined by the rule (unification scope).</summary>
    public int Metavariables { get; set; }

    /// <summary>Native delegates the expression compiler produced for this rule (cache-size delta).</summary>
    public int CompiledDelegates { get; set; }

    /// <summary>Wall time of pattern parsing + delegate compilation for this rule, in milliseconds.</summary>
    public double CompileMs { get; set; }

    /// <summary>Rule-language features this rule uses (e.g. <c>pattern-either</c>, <c>fix</c>).</summary>
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// Pack-level rule-surface report: per-rule entries plus aggregates. Serializable
/// via <see cref="LabyrinthRuleSurfaceReport.ToJson"/> for committed measurement
/// artifacts (issue #246 LAB harness).
/// </summary>
public sealed class LabyrinthRuleSurfaceReportData
{
    /// <summary>Display name of the target grammar the pack was compiled against.</summary>
    public string GrammarName { get; set; } = string.Empty;

    /// <summary>Number of rules in the pack.</summary>
    public int RuleCount { get; set; }

    /// <summary>Total positive patterns across all rules.</summary>
    public int TotalPatterns { get; set; }

    /// <summary>Total compiled delegates across all rules.</summary>
    public int TotalCompiledDelegates { get; set; }

    /// <summary>Total compile time of all rules, in milliseconds.</summary>
    public double TotalCompileMs { get; set; }

    /// <summary>Distinct rule-language features used anywhere in the pack, sorted.</summary>
    public List<string> FeaturesUsed { get; set; } = new();

    /// <summary>Per-rule entries.</summary>
    public List<LabyrinthRuleSurfaceEntry> Rules { get; set; } = new();
}

/// <summary>
/// Engine-side measurement primitive of the Labyrinth expressiveness/analysis-cost
/// harness (Minotaur issue #246, LAB track; sub-issue #263): builds a
/// <see cref="LabyrinthRuleSurfaceReportData"/> for a rule pack by compiling every
/// rule against the target grammar — recording feature usage, pattern/metavariable
/// counts, compiled-delegate count and compile time per rule. Deterministic and
/// honest: it measures only what the engine itself does at load/compile time.
/// </summary>
public static class LabyrinthRuleSurfaceReport
{
    /// <summary>
    /// Compiles every rule of <paramref name="ruleSet"/> against the target grammar
    /// and builds the surface report. Throws <see cref="LabyrinthRuleException"/> if
    /// any rule fails to compile (fail fast: a pack that does not compile has no
    /// measurable surface).
    /// </summary>
    public static LabyrinthRuleSurfaceReportData Build(
        LabyrinthRuleSet ruleSet, string grammarContent, string grammarName = "target grammar")
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(grammarContent);

        using var parser = new LabyrinthPatternParser(grammarContent, grammarName);
        var expressionCompiler = new LabyrinthExpressionCompiler();
        var data = new LabyrinthRuleSurfaceReportData { GrammarName = grammarName };
        var featuresUsed = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var rule in ruleSet.Rules)
        {
            var watch = Stopwatch.StartNew();
            var compiled = LabyrinthRuleCompiler.Compile(rule, parser);
            var before = expressionCompiler.CachedPatternCount;
            _ = expressionCompiler.CompileRule(compiled);
            var delegates = expressionCompiler.CachedPatternCount - before;
            watch.Stop();

            var features = DetectFeatures(rule);
            foreach (var f in features)
            {
                featuresUsed.Add(f);
            }

            var patterns = rule.TypeValue == LabyrinthRuleType.Search
                ? 1
                : compiled.Sources.Sum(e => e.Alternatives.Count)
                    + compiled.Sinks.Sum(e => e.Alternatives.Count)
                    + compiled.Sanitizers.Sum(e => e.Alternatives.Count);

            data.Rules.Add(new LabyrinthRuleSurfaceEntry
            {
                RuleId = rule.Id,
                Type = rule.Type,
                Severity = rule.Severity,
                Entries = rule.TypeValue == LabyrinthRuleType.Search
                    ? 1
                    : (rule.Sources?.Count ?? 0) + (rule.Sinks?.Count ?? 0) + (rule.Sanitizers?.Count ?? 0),
                Patterns = patterns,
                Metavariables = compiled.Metavariables.Count,
                CompiledDelegates = delegates,
                CompileMs = watch.Elapsed.TotalMilliseconds,
                Features = features.ToList(),
            });
        }

        data.RuleCount = data.Rules.Count;
        data.TotalPatterns = data.Rules.Sum(r => r.Patterns);
        data.TotalCompiledDelegates = data.Rules.Sum(r => r.CompiledDelegates);
        data.TotalCompileMs = data.Rules.Sum(r => r.CompileMs);
        data.FeaturesUsed = featuresUsed.ToList();
        return data;
    }

    /// <summary>Serializes a report to indented camelCase JSON (for committed artifacts).</summary>
    public static string ToJson(LabyrinthRuleSurfaceReportData data) =>
        JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

    /// <summary>The rule-language features a single rule exercises.</summary>
    public static IReadOnlyList<string> DetectFeatures(LabyrinthRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var features = new SortedSet<string>(StringComparer.Ordinal);
        if (rule.TypeValue == LabyrinthRuleType.Search)
        {
            features.Add("search-pattern");
        }
        else
        {
            features.Add("taint");
            features.Add("sources");
            features.Add("sinks");
        }

        if (!string.IsNullOrWhiteSpace(rule.Condition))
        {
            features.Add("condition");
        }

        if (!string.IsNullOrWhiteSpace(rule.PatternNot))
        {
            features.Add("pattern-not");
        }

        if (!string.IsNullOrWhiteSpace(rule.PatternInside))
        {
            features.Add("pattern-inside");
        }

        if (!string.IsNullOrWhiteSpace(rule.FocusMetavariable))
        {
            features.Add("focus-metavariable");
        }

        if (rule.Paths is { IsEmpty: false })
        {
            features.Add("paths");
        }

        if (!string.IsNullOrWhiteSpace(rule.Fix))
        {
            features.Add("fix");
        }

        if (rule.Metadata is { Count: > 0 })
        {
            features.Add("metadata");
        }

        foreach (var list in new[] { rule.Sources, rule.Sinks, rule.Sanitizers })
        {
            if (list is null)
            {
                continue;
            }

            foreach (var entry in list)
            {
                if (entry is null)
                {
                    continue;
                }

                var alternatives = entry.PatternEither ?? new List<LabyrinthPatternEntry> { entry };
                foreach (var alt in alternatives)
                {
                    if (alt is null)
                    {
                        continue;
                    }

                    if (entry.PatternEither is { Count: > 0 })
                    {
                        features.Add("pattern-either");
                    }

                    if (!string.IsNullOrWhiteSpace(alt.PatternNot))
                    {
                        features.Add("pattern-not");
                    }

                    if (!string.IsNullOrWhiteSpace(alt.PatternInside))
                    {
                        features.Add("pattern-inside");
                    }

                    if (!string.IsNullOrWhiteSpace(alt.Condition))
                    {
                        features.Add("condition");
                    }
                }
            }
        }

        if (rule.Sanitizers is { Count: > 0 })
        {
            features.Add("sanitizers");
        }

        if (rule.Propagators is { Count: > 0 })
        {
            features.Add("propagators");
            if (rule.Propagators.Any(p => p is not null && !string.IsNullOrWhiteSpace(p.Condition)))
            {
                features.Add("condition");
            }
        }

        return features.ToList();
    }
}
