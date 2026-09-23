namespace Minotaur.Labyrinth;

/// <summary>
/// The outcome of loading a rule pack from a directory (issue #105):
/// the merged rule set and, when loading failed, one error per invalid
/// rule file with the file and rule index it came from.
/// </summary>
public sealed class LabyrinthRulePackResult
{
    /// <summary>The merged rule set; empty when <see cref="IsValid"/> is false.</summary>
    public LabyrinthRuleSet RuleSet { get; init; } = new();

    /// <summary>
    /// One error per invalid rule, prefixed with the originating file and
    /// rule index, e.g. <c>sql/rules.yaml: rules[2]: unknown severity 'high'</c>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>True when the whole pack loaded and validated.</summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Loads Labyrinth rule sets from a directory of YAML rule files (.yaml/.yml)
/// (Labyrinth Layer 5, issue #105).
/// </summary>
public sealed class LabyrinthRulePackLoader(LabyrinthRuleLoader ruleLoader)
{
    /// <summary>Creates a pack loader with the default rule loader.</summary>
    public LabyrinthRulePackLoader() : this(new LabyrinthRuleLoader())
    {
    }

    private readonly LabyrinthRuleLoader _ruleLoader = ruleLoader ?? new LabyrinthRuleLoader();

    /// <summary>
    /// Loads and merges every rule file in <paramref name="directory"/> into one
    /// rule set. Throws <see cref="LabyrinthRuleException"/> when any rule is
    /// invalid, with all rule errors aggregated.
    /// </summary>
    public LabyrinthRuleSet LoadDirectory(string directory)
    {
        var result = TryLoadDirectory(directory);
        if (!result.IsValid)
        {
            throw new LabyrinthRuleException(
                "Invalid Labyrinth rule pack:\n  - " + string.Join("\n  - ", result.Errors));
        }

        return result.RuleSet;
    }

    /// <summary>
    /// Loads and merges every rule file in <paramref name="directory"/>,
    /// collecting validation errors per rule instead of failing fast:
    /// every invalid rule is reported (prefixed by its file), and valid
    /// rules from valid files still load. Used by the reporting layer so a
    /// CI run can show *all* rule authoring mistakes in one pass.
    /// </summary>
    public LabyrinthRulePackResult TryLoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new LabyrinthRulePackResult
            {
                Errors = new[] { $"Rule directory does not exist: '{directory}'." },
            };
        }

        var merged = new LabyrinthRuleSet();
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        var files = Directory.EnumerateFiles(directory, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(directory, "*.yml", SearchOption.AllDirectories))
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(directory, file);
            var parsed = TryParseFile(file, relative, errors);
            if (parsed is null)
            {
                continue;
            }

            for (var i = 0; i < parsed.Rules.Count; i++)
            {
                var rule = parsed.Rules[i];
                var single = new LabyrinthRuleSet();
                single.Rules.Add(rule);
                var validation = _ruleLoader.Validate(single);
                if (!validation.IsValid)
                {
                    errors.AddRange(validation.Errors.Select(e => $"{relative}: rules[{i}]: {e}"));
                    continue;
                }

                if (!seenIds.Add(rule.Id))
                {
                    errors.Add($"{relative}: rules[{i}]: duplicate rule id '{rule.Id}' (already defined in this rule pack)");
                }
                else
                {
                    merged.Rules.Add(rule);
                }
            }
        }

        if (merged.Rules.Count == 0 && errors.Count == 0)
        {
            errors.Add($"Rule pack directory contains no rule files: '{directory}'.");
        }

        return new LabyrinthRulePackResult { RuleSet = merged, Errors = errors };
    }

    private LabyrinthRuleSet? TryParseFile(string file, string relative, List<string> errors)
    {
        try
        {
            return _ruleLoader.Parse(File.ReadAllText(file));
        }
        catch (LabyrinthRuleException ex)
        {
            errors.Add($"{relative}: {ex.Message}");
            return null;
        }
    }
}
