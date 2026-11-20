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

// Shared engine access for the case-study pilots: grammar loading and the
// full-consumption validity rule (a document is only accepted when the
// session resolves a token path that consumes the whole input; Success=True
// alone is not sufficient, as established by the issue #127 harness).

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Text.RegularExpressions;
using DevelApp.StepParser;
using DevelApp.StepLexer;

internal static partial class Pilot
{
    public static string LoadGrammar(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Grammars", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Minotaur.Benchmarks.CaseStudyPilots", "Grammars", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Grammars", fileName),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
        }

        throw new FileNotFoundException($"grammar file not found: {fileName} (searched {AppContext.BaseDirectory} and the working directory)", fileName);
    }

    /// <summary>Parses one full document with a fresh session (the per-buffer
    /// engine object has no reset API, issue #127 finding) and returns the
    /// raw parsing result when the whole input was consumed.</summary>
    public static StepParsingResult? TryParseDocument(string grammarText, string document)
    {
        try
        {
            var session = new RealTimeParserSession(grammarText);
            session.Initialize(document);
            var result = session.Parse();
            if (!result.Success || result.ResolvedTokens is not { Count: > 0 })
            {
                return null;
            }

            var last = result.ResolvedTokens[^1];
            var trimmedLength = document.TrimEnd().Length;
            if (last.StartPosition + last.Length < trimmedLength)
            {
                return null;
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Whitespace-pinned token value direct from the source text,
    /// used by the walkers to keep the grammar side and the hand side
    /// byte-identical.</summary>
    public static string Slice(string text, StepToken token)
    {
        return text.Substring(token.StartPosition, Math.Min(token.Length, text.Length - token.StartPosition));
    }

    /// <summary>Strips one pair of double quotes, mirroring the hand regex
    /// capture groups for quoted fields.</summary>
    public static string Unquote(string value)
    {
        return value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"')
            ? value[1..^1]
            : value;
    }

    [GeneratedRegex(@"^environment[\x20\t]+(?<name>dev|staging|prod)[\x20\t]*\{$")]
    private static partial Regex HeaderRegex();

    public static string HeaderName(string headerTokenValue)
    {
        var match = HeaderRegex().Match(headerTokenValue);
        return match.Success ? match.Groups["name"].Value : string.Empty;
    }

    [GeneratedRegex(@"^include[\x20\t]+""(?<path>[^""\n]*)""$")]
    private static partial Regex IncludeRegex();

    public static string IncludePath(string includeTokenValue)
    {
        var match = IncludeRegex().Match(includeTokenValue);
        return match.Success ? match.Groups["path"].Value : string.Empty;
    }
}
