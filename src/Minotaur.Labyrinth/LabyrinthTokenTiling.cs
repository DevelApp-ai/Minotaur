using DevelApp.StepLexer;

namespace Minotaur.Labyrinth;

/// <summary>
/// Resolves the StepParser's exploratory-phase token union into a single
/// coherent tiling (Minotaur issue #247).
/// <para>
/// The lexer's default exploratory mode forks a path at every position where
/// more than one token rule matches, and emits every surviving path's tokens
/// into the parse result's token list. With a grammar that defines both
/// <c>??</c> and <c>?</c> rules (the C# 10 target grammar defines both), the
/// union therefore contains overlapping readings of the same span:
/// <c>??</c> as one token AND as two <c>?</c> tokens; likewise
/// <c>...</c> (the ellipsis pattern token) versus three <c>.</c> tokens when
/// the target grammar defines a dot rule. The parser's winning derivation
/// resolves the ambiguity only for inputs that fully derive, which pattern
/// snippets and partial sources deliberately are not.
/// </para>
/// <para>
/// This class selects a deterministic longest-match tiling from the union:
/// tokens are ordered by start position (longest first at equal starts, union
/// order as the final tie-break) and a token is kept when its span does not
/// overlap an already-kept token. The result is one token per covered span,
/// in document order, so the pattern side and the source-materialization side
/// always see the same token shape for a given grammar.
/// </para>
/// </summary>
public static class LabyrinthTokenTiling
{
    /// <summary>
    /// Resolves an (possibly ambiguous, possibly overlapping) lexed token
    /// union to a single longest-match tiling in document order. Zero-length
    /// tokens never cover input (the lexer already rejects zero-width rules)
    /// and are dropped as defense in depth.
    /// </summary>
    /// <param name="tokens">The lexed token union, in any order.</param>
    /// <returns>The kept tokens in document order: one per covered span, the
    /// longest match preferred at every forked position.</returns>
    public static List<StepToken> Resolve(IEnumerable<StepToken> tokens)
    {
        var ordered = tokens
            .OrderBy(t => t.StartPosition)
            .ThenByDescending(t => t.Length)
            .ToList();
        var kept = new List<StepToken>(ordered.Count);
        long coveredEnd = long.MinValue;
        foreach (var token in ordered)
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (token.StartPosition >= coveredEnd)
            {
                kept.Add(token);
                coveredEnd = token.StartPosition + token.Length;
            }
        }

        return kept;
    }
}
