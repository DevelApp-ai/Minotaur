namespace Minotaur.Labyrinth;

/// <summary>
/// Loads Labyrinth rule sets from a directory of YAML rule files (.yaml/.yml).
/// </summary>
public sealed class LabyrinthRulePackLoader(LabyrinthRuleLoader ruleLoader)
{
    private readonly LabyrinthRuleLoader _ruleLoader = ruleLoader ?? new LabyrinthRuleLoader();

    /// <summary>
    /// Loads and merges every rule file in <paramref name="directory"/> into one rule set.
    /// Fails fast (throws <see cref="LabyrinthRuleException"/>) on the first invalid file.
    /// </summary>
    public LabyrinthRuleSet LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new LabyrinthRuleException($"Rule directory does not exist: '{directory}'.");
        }

        var merged = new LabyrinthRuleSet();
        var files = Directory.EnumerateFiles(directory, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(directory, "*.yml", SearchOption.AllDirectories))
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var ruleSet = _ruleLoader.Load(File.ReadAllText(file));
            merged.Rules.AddRange(ruleSet.Rules);
        }

        var final = new LabyrinthRuleLoader().Validate(merged);
        if (!final.IsValid)
        {
            throw new LabyrinthRuleException(
                $"Invalid Labyrinth rule pack:\n  - " + string.Join("\n  - ", final.Errors));
        }

        return merged;
    }
}
