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

namespace Minotaur.Benchmarks.ParserRuntime.HandWritten;

/// <summary>
/// Hand-written baseline parser for the analytics query DSL: a single-pass
/// tokenizer (words, numbers, quoted strings and punctuation) plus
/// positional checks for the statement shape
/// SELECT field FROM source WHERE col = value WINDOW n unit.
/// </summary>
public sealed class HandWrittenQueryParser
{
    /// <summary>Parse one statement; returns true when it is well formed.</summary>
    public bool Parse(string input)
    {
        if (!Tokenize(input, out var tokens))
        {
            return false;
        }

        // select field from source where col = value window n unit
        if (tokens.Count != 11
            || tokens[0] != (TokenKind.Word, "select")
            || tokens[2] != (TokenKind.Word, "from")
            || tokens[4] != (TokenKind.Word, "where")
            || tokens[6].Kind != TokenKind.Equals
            || tokens[7].Kind != TokenKind.String
            || tokens[8] != (TokenKind.Word, "window")
            || tokens[9].Kind != TokenKind.Number)
        {
            return false;
        }

        foreach (var i in new[] { 1, 3, 5, 10 })
        {
            if (tokens[i].Kind != TokenKind.Word)
            {
                return false;
            }
        }

        return true;
    }

    private enum TokenKind
    {
        Word,
        Number,
        String,
        Equals,
    }

    private static bool Tokenize(string input, out List<(TokenKind Kind, string Text)> tokens)
    {
        tokens = new List<(TokenKind, string)>(12);
        var i = 0;
        var n = input.Length;
        while (i < n)
        {
            var c = input[i];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                i++;
                continue;
            }

            if (c == '"')
            {
                var close = input.IndexOf('"', i + 1);
                if (close < 0 || input[close - 1] == '\\')
                {
                    return false;
                }

                tokens.Add((TokenKind.String, input[i..(close + 1)]));
                i = close + 1;
                continue;
            }

            if (c == '=')
            {
                tokens.Add((TokenKind.Equals, "="));
                i++;
                continue;
            }

            if (c == '-' || char.IsAsciiDigit(c))
            {
                var start = i;
                if (c == '-')
                {
                    i++;
                }

                while (i < n && char.IsAsciiDigit(input[i]))
                {
                    i++;
                }

                if (i < n && input[i] == '.' && i + 1 < n && char.IsAsciiDigit(input[i + 1]))
                {
                    i++;
                    while (i < n && char.IsAsciiDigit(input[i]))
                    {
                        i++;
                    }
                }

                tokens.Add((TokenKind.Number, input[start..i]));
                continue;
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                var start = i;
                while (i < n && (char.IsAsciiLetterOrDigit(input[i]) || input[i] == '_'
                    || input[i] == '.' || input[i] == '-'))
                {
                    i++;
                }

                tokens.Add((TokenKind.Word, input[start..i]));
                continue;
            }

            return false;
        }

        return true;
    }
}
