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
/// Hand-written baseline parser for the multi-language template DSL: a
/// single forward scan that validates text runs, {{ expression }} holes and
/// the &lt;style&gt;, &lt;script&gt; and {{sql}} ... {{/sql}} embedded
/// blocks against the same shape the case-study grammar accepts.
/// </summary>
public sealed class HandWrittenTemplateParser
{
    public bool Parse(string input)
    {
        var i = 0;
        var n = input.Length;
        while (i < n)
        {
            var c = input[i];
            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    i++;
                    break;
                case '<':
                    if (input.IndexOf("<style>", i, StringComparison.Ordinal) == i)
                    {
                        var end = input.IndexOf("</style>", i, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            return false;
                        }

                        i = end + "</style>".Length;
                    }
                    else if (input.IndexOf("<script>", i, StringComparison.Ordinal) == i)
                    {
                        var end = input.IndexOf("</script>", i, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            return false;
                        }

                        i = end + "</script>".Length;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                case '{':
                    if (input.IndexOf("{{sql}}", i, StringComparison.Ordinal) == i)
                    {
                        var close = input.IndexOf("{{/sql}}", i, StringComparison.Ordinal);
                        if (close < 0)
                        {
                            return false;
                        }

                        i = close + "{{/sql}}".Length;
                    }
                    else if (input.IndexOf("{{ ", i, StringComparison.Ordinal) == i)
                    {
                        var close = input.IndexOf("}}", i, StringComparison.Ordinal);
                        if (close < 0 || close == i + 3)
                        {
                            return false;
                        }

                        i = close + 2;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                default:
                    if (!IsTextChar(c))
                    {
                        return false;
                    }

                    while (i < n && IsTextChar(input[i]))
                    {
                        i++;
                    }

                    break;
            }
        }

        return true;
    }

    private static bool IsTextChar(char c) =>
        char.IsAsciiLetterOrDigit(c) || c == '_' || c == '.' || c == '!' || c == '?' || c == ','
        || c == '-';
}
