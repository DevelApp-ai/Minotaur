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
/// Hand-written baseline parser for the configuration DSL (issue #127
/// baseline a): a line-oriented recursive-descent parser for sections,
/// key=value settings and comma-separated arrays, accepting the same
/// language the ANTLR grammar accepts.
/// </summary>
public sealed class HandWrittenConfigParser
{
    /// <summary>Parse result of a hand-written parse.</summary>
    public sealed record Result(bool Ok, int Sections, int Settings, string? Error);

    public Result Parse(string input)
    {
        var sections = 0;
        var settings = 0;
        var inSection = false;
        var lineNo = 0;

        foreach (var raw in input.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            lineNo++;
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line[0] == '[')
            {
                var close = line.IndexOf(']');
                if (close < 0 || close != line.Length - 1 || close == 1)
                {
                    return new Result(false, sections, settings, $"line {lineNo}: malformed section header");
                }

                if (!IsWord(line[1..close]))
                {
                    return new Result(false, sections, settings, $"line {lineNo}: bad section name");
                }

                sections++;
                inSection = true;
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0 || eq == line.Length - 1)
            {
                return new Result(false, sections, settings, $"line {lineNo}: expected key = value");
            }

            var key = line[..eq].TrimEnd();
            var value = line[(eq + 1)..].Trim();
            if (!IsWord(key) || !IsValidValue(value, allowArray: true, out var itemCount))
            {
                return new Result(false, sections, settings, $"line {lineNo}: bad setting");
            }

            if (value.StartsWith('['))
            {
                settings += itemCount;
            }
            else
            {
                settings++;
            }

            _ = inSection;
        }

        return new Result(true, sections, settings, null);
    }

    private static bool IsValidValue(string value, bool allowArray, out int itemCount)
    {
        itemCount = 1;
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == '"')
        {
            return value.Length >= 2 && value.EndsWith('"') && !value.EndsWith("\\\"", StringComparison.Ordinal);
        }

        if (value[0] == '[')
        {
            if (!allowArray || !value.EndsWith(']'))
            {
                return false;
            }

            var inner = value[1..^1].Trim();
            if (inner.Length == 0)
            {
                itemCount = 0;
                return true;
            }

            itemCount = 0;
            foreach (var item in inner.Split(','))
            {
                var trimmed = item.Trim();
                if (!IsValidValue(trimmed, allowArray: false, out _))
                {
                    return false;
                }

                itemCount++;
            }

            return true;
        }

        if (value[0] == '-' || char.IsAsciiDigit(value[0]))
        {
            var dot = false;
            for (var i = value[0] == '-' ? 1 : 0; i < value.Length; i++)
            {
                if (value[i] == '.')
                {
                    if (dot)
                    {
                        return false;
                    }

                    dot = true;
                }
                else if (!char.IsAsciiDigit(value[i]))
                {
                    return false;
                }
            }

            return value.Length > 0 && value != "-" && value != ".";
        }

        return IsWord(value);
    }

    private static bool IsWord(string text)
    {
        if (text.Length == 0 || !char.IsAsciiLetter(text[0]) && text[0] != '_')
        {
            return false;
        }

        foreach (var c in text[1..])
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.' && c != '-')
            {
                return false;
            }
        }

        return true;
    }
}
