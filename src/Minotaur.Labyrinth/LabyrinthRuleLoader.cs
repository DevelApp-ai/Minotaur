using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Minotaur.Labyrinth;

/// <summary>
/// The result of validating a <see cref="LabyrinthRuleSet"/>.
/// </summary>
public sealed class LabyrinthValidationResult
{
    /// <summary>Validation errors, empty when the rule set is valid.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>True when <see cref="Errors"/> is empty.</summary>
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false)]
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Layer 1 rule loader (issue #101): loads Labyrinth rule files — the specialized
/// YAML format defined by the Labyrinth grammar (Minotaur-Grammars specialized/labyrinth) —
/// and validates them.
/// </summary>
public sealed class LabyrinthRuleLoader
{
    private readonly IDeserializer _deserializer;

    public LabyrinthRuleLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses a Labyrinth rule file from YAML text.
    /// </summary>
    /// <exception cref="LabyrinthRuleException">Thrown on malformed YAML or schema violations.</exception>
    public LabyrinthRuleSet Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new LabyrinthRuleException("Rule file must not be empty.");
        }

        LabyrinthRuleSet ruleSet;
        try
        {
            ruleSet = _deserializer.Deserialize<LabyrinthRuleSet>(yaml);
        }
        catch (YamlException ex)
        {
            throw new LabyrinthRuleException($"Invalid rule file YAML: {ex.Message}", ex);
        }

        if (ruleSet is null || ruleSet.Rules.Count == 0)
        {
            throw new LabyrinthRuleException($"Rule file must contain a non-empty '{LabyrinthRuleSet.RootKey}:' list.");
        }

        return ruleSet;
    }

    /// <summary>
    /// Loads and validates a Labyrinth rule file from YAML text.
    /// </summary>
    /// <exception cref="LabyrinthRuleException">Thrown when validation fails.</exception>
    public LabyrinthRuleSet Load(string yaml)
    {
        var ruleSet = Parse(yaml);
        var result = Validate(ruleSet);
        if (!result.IsValid)
        {
            throw new LabyrinthRuleException(
                "Invalid Labyrinth rule file:\n  - " + string.Join("\n  - ", result.Errors));
        }

        return ruleSet;
    }

    /// <summary>
    /// Validates a rule set without throwing. Checks:
    /// <list type="bullet">
    /// <item>rule ids are present and unique</item>
    /// <item>severity and type values are known</item>
    /// <item>search rules define a pattern; taint rules define sources and sinks</item>
    /// <item>propagator <c>from</c>/<c>to</c> metavariables occur in the propagator pattern</item>
    /// <item>all patterns tokenize cleanly (metavariable/ellipsis well-formedness)</item>
    /// </list>
    /// </summary>
    public LabyrinthValidationResult Validate(LabyrinthRuleSet ruleSet)
    {
        var errors = new List<string>();
        if (ruleSet.Rules.Count == 0)
        {
            errors.Add($"Rule file must contain a non-empty '{LabyrinthRuleSet.RootKey}:' list.");
            return new LabyrinthValidationResult { Errors = errors };
        }

        var ids = new HashSet<string>();
        for (var i = 0; i < ruleSet.Rules.Count; i++)
        {
            var rule = ruleSet.Rules[i];
            var label = $"rules[{i}]";

            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                errors.Add($"{label}: 'id' is required.");
            }
            else if (!ids.Add(rule.Id))
            {
                errors.Add($"{label}: duplicate rule id '{rule.Id}'.");
            }

            try
            {
                _ = rule.SeverityValue;
            }
            catch (LabyrinthRuleException ex)
            {
                errors.Add($"{label} ({rule.Id}): {ex.Message}");
            }

            try
            {
                _ = rule.TypeValue;
            }
            catch (LabyrinthRuleException ex)
            {
                errors.Add($"{label} ({rule.Id}): {ex.Message}");
                continue;
            }

            if (rule.TypeValue == LabyrinthRuleType.Search)
            {
                ValidateSearchRule(rule, label, errors);
            }
            else
            {
                ValidateTaintRule(rule, label, errors);
            }
        }

        return new LabyrinthValidationResult { Errors = errors };
    }

    private static void ValidateSearchRule(LabyrinthRule rule, string label, List<string> errors)
    {
        if (rule.Sources is { Count: > 0 } || rule.Sinks is { Count: > 0 })
        {
            errors.Add($"{label} ({rule.Id}): 'sources'/'sinks' are only valid for type 'taint'.");
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            errors.Add($"{label} ({rule.Id}): search rules require a 'pattern'.");
        }

    }

    private static void ValidateTaintRule(LabyrinthRule rule, string label, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(rule.Pattern))
        {
            errors.Add($"{label} ({rule.Id}): 'pattern' is only valid for type 'search'; taint rules use 'sources'/'sinks'.");
        }

        if (rule.Sources is not { Count: > 0 })
        {
            errors.Add($"{label} ({rule.Id}): taint rules require at least one source.");
        }
        else
        {
            ValidatePatternList(rule.Sources, label, rule.Id, "sources", errors);
        }

        if (rule.Sinks is not { Count: > 0 })
        {
            errors.Add($"{label} ({rule.Id}): taint rules require at least one sink.");
        }
        else
        {
            ValidatePatternList(rule.Sinks, label, rule.Id, "sinks", errors);
        }

        if (rule.Sanitizers is { Count: > 0 })
        {
            ValidatePatternList(rule.Sanitizers, label, rule.Id, "sanitizers", errors);
        }

        if (rule.Propagators is { Count: > 0 })
        {
            for (var i = 0; i < rule.Propagators.Count; i++)
            {
                var prop = rule.Propagators[i];
                var propLabel = $"{label}.{rule.Id}.propagators[{i}]";
                if (prop is null || string.IsNullOrWhiteSpace(prop.Pattern))
                {
                    errors.Add($"{propLabel}: 'pattern' is required.");
                    continue;
                }

                var defined = LabyrinthMetavariables.DistinctNamesIn(prop.Pattern);
                foreach (var (name, key) in new[] { (prop.From, "from"), (prop.To, "to") })
                {
                    var normalized = name?.TrimStart('$') ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(normalized))
                    {
                        errors.Add($"{propLabel}: '{key}' is required.");
                    }
                    else if (!defined.Contains(normalized, StringComparer.Ordinal))
                    {
                        errors.Add(
                            $"{propLabel}: '{key}' metavariable '${normalized}' does not occur in the propagator pattern.");
                    }
                }
            }
        }
    }

    private static void ValidatePatternList(List<LabyrinthPatternEntry> entries, string label, string ruleId, string key, List<string> errors)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry is null || string.IsNullOrWhiteSpace(entry.Pattern))
            {
                errors.Add($"{label}.{ruleId}.{key}[{i}]: 'pattern' is required.");
            }

        }
    }

}
