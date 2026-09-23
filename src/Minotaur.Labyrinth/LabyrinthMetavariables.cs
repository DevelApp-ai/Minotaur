using System.Text.RegularExpressions;

namespace Minotaur.Labyrinth;

/// <summary>
/// Textual metavariable extraction for Labyrinth patterns. Schema-validation only.
/// <para>
/// Pattern bodies themselves are parsed by the DevelApp.StepLexer/StepParser pipeline
/// with the target grammar once metavariable/ellipsis token support and grammar
/// overlay/composition land there (ENFAStepLexer-StepParser#65, #66). Until then,
/// patterns stay opaque strings in Layer 1; this helper only answers
/// "which $NAME metavariables does this pattern mention?" for propagator
/// from/to binding checks.
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
