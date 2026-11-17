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

namespace Minotaur.Parser.LiveEditing
{
    /// <summary>
    /// Token-only GrammarForge grammars for the live editor. The rules are
    /// shaped by three constraints of the shipped
    /// <c>RealTimeParserSession</c> (DevelApp.StepParser 1.75.0, default
    /// exploratory lexing mode), each verified empirically against the
    /// package during issue #196:
    /// <list type="number">
    /// <item><b>Token rules must not overlap.</b> In exploratory mode the
    /// lexer unions tokens across all active paths: a keyword rule and an
    /// identifier rule that both match "public" split the path, and every
    /// subsequent token is emitted once per path, growing the token stream
    /// multiplicatively. Keywords and identifiers therefore share one
    /// <c>WORD</c> rule.</item>
    /// <item><b>No literal slash in a token pattern.</b> The grammar loader
    /// treats the first inner unescaped <c>/</c> as the end of the
    /// <c>/regex/</c> body, so a pattern containing <c>/</c> (even escaped
    /// or inside a character class) never matches. Slash is written as the
    /// hex escape <c>\x2F</c>, and line comments use <c>#</c> instead of
    /// <c>//</c>.</item>
    /// <item><b>Multi-character alternatives before the single-character
    /// class.</b> Longest alternatives are listed first in the one operator
    /// rule so <c>==</c> wins over <c>=</c>; the remaining single-character
    /// operators are one character class.</item>
    /// <item><b>Every character of the edited text must match some
    /// rule.</b> When the lexer reaches input that no rule matches, every
    /// active path dies and the session silently stops lexing at that
    /// offset; <c>Initialize</c> returns without error and the token list
    /// simply ends there. A grammar for real documents must therefore cover
    /// the full character set (bare <c>*</c> was missing from the operator
    /// class during issue #196 and truncated every harness corpus to ~50
    /// tokens).</item>
    /// </list>
    /// Supported regex constructs are the engine's documented subset:
    /// character classes, alternation, grouping and the +, * and {n,m}
    /// quantifiers.
    /// </summary>
    public static class LiveEditorGrammars
    {
        /// <summary>
        /// Get a token-only C#-like grammar for the live editor: words,
        /// numbers, string literals, # line comments, operators,
        /// punctuation and whitespace.
        /// </summary>
        public static string CSharpLike => """
            Grammar: CSharpLikeLiveEditor

            # Words: keywords and identifiers together. The shipped
            # exploratory-mode session requires non-overlapping token rules;
            # overlapping rules multiply active paths and duplicate tokens.
            <WORD> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
            <NUMBER> ::= /[0-9]+/
            <STRING_LITERAL> ::= /"([^"\\]|\\.)*"/
            <COMMENT> ::= /#[^\n]*/

            # Operators: multi-character alternatives first (longest match
            # wins), then all single-character operators as one character
            # class. Slash is written \x2F: a literal / inside a token
            # pattern truncates the rule at the inner slash. The class must
            # contain every single-character operator the editor text can
            # contain: unmatched input kills all active paths and the
            # session silently stops lexing there (see the class remarks).
            <OPERATOR> ::= /==|!=|<=|>=|&&|\|\||\+\+|--|\+=|-=|\*=|=>|[=+\x2F*%!&|^~?:<>-]/

            # Punctuation and brackets.
            <PUNCT> ::= /[(){}\[\];,.]/

            # Whitespace, kept as tokens (the token-only grammar has no
            # skip directive).
            <WS> ::= /[ \t\r\n]+/
            """;
    }
}
