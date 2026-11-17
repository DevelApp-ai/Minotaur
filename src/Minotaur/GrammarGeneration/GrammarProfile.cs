/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Text.RegularExpressions;

namespace Minotaur.GrammarGeneration;

/// <summary>
/// Rule-count statistics of a GrammarForge grammar document, the size axis of
/// the grammar-compilation measurements (issue #126).
/// </summary>
public sealed record GrammarProfileStats
{
    /// <summary>
    /// Gets the number of lexer token-rule lines: rules whose right-hand side
    /// is a single <c>/regex/</c> pattern or a single quoted literal
    /// (optionally followed by an <c>=&gt; { ... }</c> action such as
    /// <c>skip</c>). Same-named rules (alternative spellings of one token
    /// type) each count.
    /// </summary>
    public int TokenRuleCount { get; init; }

    /// <summary>
    /// Gets the number of distinct token-type names declared by the lexer
    /// token rules (same-named alternatives collapse to one).
    /// </summary>
    public int DistinctTokenTypeCount { get; init; }

    /// <summary>
    /// Gets the number of EBNF production-rule lines: rules whose right-hand
    /// side references other rules or bare keywords rather than a single
    /// pattern or literal.
    /// </summary>
    public int ProductionRuleCount { get; init; }

    /// <summary>
    /// Gets the total number of token and production rule lines.
    /// </summary>
    public int TotalRuleCount => TokenRuleCount + ProductionRuleCount;

    /// <summary>
    /// Gets the number of entries in the <c>Keywords:</c> header line, or
    /// zero when the grammar declares no keywords header.
    /// </summary>
    public int KeywordCount { get; init; }
}

/// <summary>
/// Classifies the rules of a GrammarForge grammar document into lexer token
/// rules and EBNF production rules so the grammar-compilation measurements
/// (issue #126) can report the size axis of each measured grammar.
/// </summary>
/// <remarks>
/// The classifier is intentionally line-based, mirroring the grammar file
/// format: every rule occupies one line starting with
/// <c>&lt;rule_name&gt; ::=</c>. Whole-line <c>#</c> comments and
/// <c>/* ... */</c> comment blocks (full-line or spanning lines, as emitted
/// between rules) are skipped; they are never counted as rules. It is not a
/// full grammar parser — trailing same-line comments after a rule are out of
/// scope, matching the corpus style.
/// </remarks>
public static partial class GrammarProfile
{
    [GeneratedRegex(@"^<([^>]+)>\s*::=(.*)$")]
    private static partial Regex RuleLineRegex();

    /// <summary>
    /// Analyzes a GrammarForge grammar document and returns its rule counts.
    /// </summary>
    /// <param name="grammarContent">The grammar file content.</param>
    /// <returns>The rule statistics of the document.</returns>
    public static GrammarProfileStats Analyze(string grammarContent)
    {
        var tokenRuleCount = 0;
        var productionRuleCount = 0;
        var keywordCount = 0;
        var tokenTypeNames = new HashSet<string>(StringComparer.Ordinal);
        var inBlockComment = false;

        foreach (var rawLine in grammarContent.Split('\n'))
        {
            var line = rawLine.Trim('\r', ' ', '\t');

            // /* ... */ comment blocks span lines; a block opens on a line
            // that starts with /* and closes on a line containing */.
            if (inBlockComment)
            {
                if (line.Contains("*/"))
                {
                    inBlockComment = false;
                }

                continue;
            }

            if (line.StartsWith("/*", StringComparison.Ordinal))
            {
                inBlockComment = !line.Contains("*/");
                continue;
            }

            // Whole-line # comments (the grammar format has no // line
            // comments: a bare '/' would collide with the /.../ rule
            // delimiters).
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("Keywords:", StringComparison.OrdinalIgnoreCase))
            {
                keywordCount = CountKeywords(line["Keywords:".Length..]);
                continue;
            }

            var match = RuleLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var ruleName = match.Groups[1].Value;
            var rhs = StripAction(StripTerminator(match.Groups[2].Value.Trim()));

            if (IsTokenRule(rhs))
            {
                tokenRuleCount++;
                tokenTypeNames.Add(ruleName);
            }
            else
            {
                productionRuleCount++;
            }
        }

        return new GrammarProfileStats
        {
            TokenRuleCount = tokenRuleCount,
            DistinctTokenTypeCount = tokenTypeNames.Count,
            ProductionRuleCount = productionRuleCount,
            KeywordCount = keywordCount,
        };
    }

    /// <summary>
    /// Strips a trailing <c>=&gt; { ... }</c> action from a rule right-hand
    /// side, e.g. <c>skip</c> for whitespace and comment rules.
    /// </summary>
    private static string StripAction(string rhs)
    {
        var arrow = rhs.IndexOf("=>", StringComparison.Ordinal);
        return arrow >= 0 ? rhs[..arrow].Trim() : rhs;
    }

    /// <summary>
    /// Strips a trailing <c>;</c> rule terminator. Corpus grammars are not
    /// uniform: some terminate every rule with a semicolon, some none.
    /// </summary>
    private static string StripTerminator(string rhs)
    {
        return rhs.EndsWith(";", StringComparison.Ordinal) ? rhs[..^1].Trim() : rhs;
    }

    /// <summary>
    /// Determines whether a right-hand side declares a lexer token rule.
    /// This mirrors the engine's rule classification, verified against
    /// DevelApp.StepParser 1.75.0: a token rule is ONE complete
    /// <c>/regex/</c> pattern or ONE complete quoted literal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A regex token rule may not contain an inner unescaped slash: the
    /// loader closes the pattern at the first inner <c>/</c> (issue #196
    /// finding, corpus grammars write <c>\x2F</c> instead). An alternation
    /// of several patterns such as <c>/[0-9]+/ | /[a-z]+/</c> is therefore
    /// a production rule, as is an alternation of quoted literals such as
    /// <c>"true" | "false"</c>.
    /// </para>
    /// <para>
    /// A quoted literal is delimited by single or double quotes and may
    /// not contain the closing quote character; anything else between the
    /// quotes is literal content, including spaces and angle brackets.
    /// A right-hand side that merely starts and ends with a quoted literal
    /// but references rules in between (SQL coverage style, e.g.
    /// <c>"WITH" &lt;id&gt; "END"</c>) is a production rule.
    /// </para>
    /// </remarks>
    private static bool IsTokenRule(string rhs)
    {
        if (rhs.Length < 2)
        {
            return false;
        }

        if (rhs[0] == '/' && rhs[^1] == '/')
        {
            return !rhs[1..^1].Contains('/');
        }

        return (rhs[0] == '"' || rhs[0] == '\'')
            && rhs[^1] == rhs[0]
            && rhs.IndexOf(rhs[0], 1) == rhs.Length - 1;
    }

    /// <summary>
    /// Counts the comma-separated entries of a <c>Keywords:</c> header.
    /// </summary>
    private static int CountKeywords(string keywordsList)
    {
        var count = 0;
        foreach (var part in keywordsList.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                count++;
            }
        }

        return count;
    }
}
