using System.Text.RegularExpressions;

namespace Minotaur.Labyrinth;

/// <summary>
/// Textual metavariable extraction for Labyrinth patterns. Schema-validation only:
/// this helper answers "which $NAME metavariables does this pattern mention?"
/// quickly and without a target grammar, which is what the YAML loader needs.
/// <para>
/// The real pattern parsing (pattern snippets to mini-ASTs through the
/// DevelApp.StepLexer/StepParser pipeline with the target grammar, including
/// metavariable and ellipsis tokens, landed with ENFAStepLexer-StepParser
/// #65/#66 and lives in <see cref="LabyrinthPatternParser"/> and
/// <see cref="LabyrinthRuleCompiler"/>; use those whenever a target grammar is
/// available.
/// </para>
/// </summary>
public static partial class LabyrinthMetavariables
{
    /// <summary>Metavariable: a dollar sign followed by an uppercase identifier, e.g. <c>$DATA</c>.</summary>
    [GeneratedRegex(@"\$(?<name>[A-Z_][A-Z0-9_]*)")]
    private static partial Regex MetavariableRegex();

    /// <summary>All metavariable names in the pattern, in order of occurrence (duplicates preserved).</summary>
    public static IReadOnlyList<string> NamesIn(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (var m in MetavariableRegex().EnumerateMatches(pattern))
        {
            names.Add(pattern.Substring(m.Index + 1, m.Length - 1));
        }

        return names;
    }

    /// <summary>Distinct metavariable names in the pattern, in order of first occurrence.</summary>
    public static IReadOnlyList<string> DistinctNamesIn(string? pattern)
    {
        var seen = new HashSet<string>();
        var names = new List<string>();
        foreach (var name in NamesIn(pattern))
        {
            if (seen.Add(name))
            {
                names.Add(name);
            }
        }

        return names;
    }
}
