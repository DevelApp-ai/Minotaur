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

// Hand-written incumbent for the issue #128 configuration DSL: a typical
// line-oriented regex parser, deliberately in the style such configs are
// usually handled in production code. Errors carry line number, column,
// the offending line and a message (the M2 maintenance edit added the
// column and source line).

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Text.RegularExpressions;

public sealed record HandParseError(int LineNumber, int Column, string Text, string Message);

public sealed record ConfigSetting(string Key, string Value, string Kind);

public sealed record ConfigEnv(string Name, ConfigSetting[] Settings);

public sealed record ConfigDoc(ConfigEnv[] Environments, string[] Includes, HandParseError[] Errors);

public static class ConfigModels
{
    /// <summary>Value-kind classification shared by BOTH systems (number,
    /// ${...} template, string): a semantic layer, identical on each side,
    /// so the comparison measures the acquisition mechanism only.</summary>
    public static string ClassifyValue(string value)
    {
        if (Regex.IsMatch(value, @"^-?[0-9]+(\.[0-9]+)?$"))
        {
            return "number";
        }

        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}') && value.Length > 3)
        {
            return "template";
        }

        return "string";
    }

    public static bool Equal(ConfigDoc left, ConfigDoc right)
    {
        if (left.Environments.Length != right.Environments.Length
            || left.Includes.Length != right.Includes.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Includes.Length; i++)
        {
            if (left.Includes[i] != right.Includes[i])
            {
                return false;
            }
        }

        for (var i = 0; i < left.Environments.Length; i++)
        {
            var le = left.Environments[i];
            var re = right.Environments[i];
            if (le.Name != re.Name || le.Settings.Length != re.Settings.Length)
            {
                return false;
            }

            for (var j = 0; j < le.Settings.Length; j++)
            {
                if (le.Settings[j] != re.Settings[j])
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public sealed partial class HandConfigParser
{
    [GeneratedRegex(@"^[\x20\t]*environment[\x20\t]+(?<name>dev|staging|prod)[\x20\t]*\{[\x20\t]*(#.*)?$")]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^[\x20\t]*(?<key>[A-Za-z_][A-Za-z0-9_.\-]*)=(?<value>.*)$")]
    private static partial Regex SetRegex();

    [GeneratedRegex(@"^[\x20\t]*include[\x20\t]+""(?<path>[^""\n]*)""[\x20\t]*(#.*)?$")]
    private static partial Regex IncludeRegex();

    public (bool Valid, ConfigDoc Doc) Parse(string text)
    {
        var environments = new List<ConfigEnv>();
        var includes = new List<string>();
        var errors = new List<HandParseError>();
        string? openName = null;
        var openSettings = new List<ConfigSetting>();
        var lineNumber = 0;

        var lines = text.Split('\n');
        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');
            var trimmedStart = line.TrimStart();
            if (trimmedStart.Length == 0 || trimmedStart.StartsWith('#'))
            {
                continue;
            }

            var headerMatch = HeaderRegex().Match(trimmedStart);
            if (headerMatch.Success)
            {
                if (openName is not null)
                {
                    errors.Add(new HandParseError(lineNumber, 1, line, "environment block not closed before the next environment header"));
                    continue;
                }

                openName = headerMatch.Groups["name"].Value;
                openSettings.Clear();
                continue;
            }

            if (trimmedStart == "}")
            {
                if (openName is null)
                {
                    errors.Add(new HandParseError(lineNumber, 1, line, "closing brace without an open environment block"));
                    continue;
                }

                environments.Add(new ConfigEnv(openName, [.. openSettings]));
                openName = null;
                continue;
            }

            var includeMatch = IncludeRegex().Match(trimmedStart);
            if (includeMatch.Success)
            {
                includes.Add(includeMatch.Groups["path"].Value);
                continue;
            }

            var setMatch = SetRegex().Match(trimmedStart);
            if (setMatch.Success)
            {
                if (openName is null)
                {
                    errors.Add(new HandParseError(lineNumber, 1, line, "setting outside an environment block"));
                    continue;
                }

                var key = setMatch.Groups["key"].Value;
                var value = setMatch.Groups["value"].Value;
                if (string.IsNullOrEmpty(value))
                {
                    errors.Add(new HandParseError(lineNumber, key.Length + 2, line, "setting without a value"));
                    continue;
                }

                openSettings.Add(new ConfigSetting(key, value, ConfigModels.ClassifyValue(value)));
                continue;
            }

            errors.Add(new HandParseError(lineNumber, 1, line, "unrecognized line"));
        }

        if (openName is not null)
        {
            errors.Add(new HandParseError(lineNumber, 1, string.Empty, "environment block not closed at end of document"));
        }

        return (errors.Count == 0, new ConfigDoc([.. environments], [.. includes], [.. errors]));
    }
}
