using System.Text;
using System.Text.RegularExpressions;

namespace Minotaur.Labyrinth;

/// <summary>
/// The kind of a token produced by <see cref="LabyrinthPatternLexer"/>.
/// </summary>
public enum LabyrinthTokenKind
{
    /// <summary>Literal target-grammar syntax (identifiers, operators, punctuation, whitespace).</summary>
    Literal,

    /// <summary>A Semgrep-style metavariable capture group: <c>$NAME</c> (uppercase).</summary>
    Metavariable,

    /// <summary>The ellipsis operator <c>...</c>: matches zero or more arguments/statements/characters.</summary>
    Ellipsis
}

/// <summary>
/// A single token of a Labyrinth pattern.
/// </summary>
public readonly record struct LabyrinthToken(LabyrinthTokenKind Kind, string Text, int Position)
{
    public override string ToString() => Kind switch
    {
        LabyrinthTokenKind.Literal => $"Literal({Text})",
        LabyrinthTokenKind.Metavariable => $"Metavariable({Text})",
        LabyrinthTokenKind.Ellipsis => "Ellipsis(...)",
        _ => Kind.ToString()
    };
}

/// <summary>
/// Layer 1 pattern lexer (issue #101): recognizes Semgrep-style metavariables
/// (<c>$DATA</c>, <c>$TARGET</c>, ...) and the ellipsis operator (<c>...</c>) as dedicated
/// token types within a pattern written in any target grammar.
/// <para>
/// The lexer is grammar-agnostic: everything that is not a metavariable or an ellipsis
/// is emitted as literal text so the surrounding syntax stays valid input for the
/// target grammar's StepLexer/StepParser pipeline (Layer 2 compilation, issue #102).
/// </para>
/// </summary>
public static partial class LabyrinthPatternLexer
{
    /// <summary>Metavariable: a dollar sign followed by an uppercase identifier, e.g. <c>$DATA</c>.</summary>
    [GeneratedRegex(@"\$(?<name>[A-Z_][A-Z0-9_]*)")]
    private static partial Regex MetavariableRegex();

    /// <summary>Ellipsis: three consecutive dots.</summary>
    [GeneratedRegex(@"\.\.\.")]
    private static partial Regex EllipsisRegex();

    /// <summary>
    /// Tokenizes a pattern into literal, metavariable and ellipsis tokens.
    /// </summary>
    /// <param name="pattern">The raw pattern string, e.g. <c>ExecuteAction(..., $DATA, ...)</c>.</param>
    /// <exception cref="LabyrinthRuleException">Thrown when the pattern is null/empty or a
    /// dollar sign is not followed by a valid metavariable name.</exception>
    public static IReadOnlyList<LabyrinthToken> Tokenize(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new LabyrinthRuleException("Pattern must not be null or empty.");
        }

        var tokens = new List<LabyrinthToken>();
        var literal = new StringBuilder();
        var literalStart = 0;

        void FlushLiteral(int endExclusive)
        {
            if (literal.Length > 0)
            {
                tokens.Add(new LabyrinthToken(LabyrinthTokenKind.Literal, literal.ToString(), literalStart));
                literal.Clear();
            }

            _ = endExclusive;
        }

        for (var i = 0; i < pattern.Length;)
        {
            var mvMatch = MetavariableRegex().Match(pattern, i);
            var elMatch = EllipsisRegex().Match(pattern, i);

            // Ellipsis wins only if it starts at or before the metavariable match.
            if (elMatch.Success && elMatch.Index == i)
            {
                FlushLiteral(i);
                tokens.Add(new LabyrinthToken(LabyrinthTokenKind.Ellipsis, "...", i));
                i += 3;
                continue;
            }

            if (mvMatch.Success && mvMatch.Index == i)
            {
                FlushLiteral(i);
                tokens.Add(new LabyrinthToken(LabyrinthTokenKind.Metavariable, mvMatch.Groups["name"].Value, i));
                i += mvMatch.Length;
                continue;
            }

            // A dangling '$' that does not start a metavariable is an error: catch it early
            // rather than silently treating it as literal target-grammar syntax.
            if (pattern[i] == '$')
            {
                var next = i + 1 < pattern.Length ? $"'{pattern[i + 1]}'" : "end of pattern";
                throw new LabyrinthRuleException(
                    $"Invalid metavariable at position {i}: '$' must be followed by an uppercase identifier ([A-Z_][A-Z0-9_]*), found {next}.");
            }

            if (literal.Length == 0)
            {
                literalStart = i;
            }

            literal.Append(pattern[i]);
            i++;
        }

        FlushLiteral(pattern.Length);
        return tokens;
    }

    /// <summary>
    /// Returns all metavariable names in the pattern, in order of occurrence
    /// (duplicates preserved — the same metavariable may intentionally recur,
    /// e.g. <c>$X = f($X)</c>).
    /// </summary>
    public static IReadOnlyList<string> MetavariableNames(string pattern)
    {
        var names = new List<string>();
        foreach (var m in MetavariableRegex().EnumerateMatches(pattern))
        {
            names.Add(pattern.Substring(m.Index + 1, m.Length - 1));
        }

        return names;
    }

    /// <summary>
    /// Returns all metavariable names in the pattern, distinct, in order of first occurrence.
    /// </summary>
    public static IReadOnlyList<string> DistinctMetavariableNames(string pattern) =>
        MetavariableNames(pattern).Distinct().ToList();
}
