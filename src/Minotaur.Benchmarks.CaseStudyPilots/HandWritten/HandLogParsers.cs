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

// Hand-written incumbents for the issue #132 structured-logs pilot: a
// compiled regex for nginx-combined, a compiled regex for syslog RFC 3164,
// and a character scanner for logfmt. Errors carry line/column/text like
// the DSL hand parser (the M2 maintenance edit).

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Text.RegularExpressions;

public sealed record LogError(int LineNumber, int Column, string Text, string Message);

public sealed record LogRecord(string[] Fields);

public static class LogRecords
{
    public static bool Equal(string[]? left, string[]? right)
    {
        if (left is null || right is null || left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool Equal(LogRecord left, LogRecord right) => Equal(left.Fields, right.Fields);

    public static bool ListsEqual(LogRecord[] left, LogRecord[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!Equal(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>nginx combined log line: the incumbent is a single compiled
/// regex, exactly as such log lines are consumed in production glue code.
/// Field order: ip, ident, user, time, request, status, bytes, referrer,
/// agent. bytes are >= 1000 (adopted from the corpus shape of the previous
/// runs; digits-only status).</summary>
public sealed partial class HandNginxParser
{
    [GeneratedRegex(@"^(?<ip>\S+) (?<ident>\S+) (?<user>\S+) \[(?<time>[^\]]+)\] ""(?<request>[^""]*)"" (?<status>[0-9]{3}) (?<bytes>[0-9]{4,}) ""(?<referrer>[^""]*)"" ""(?<agent>[^""]*)""$")]
    private static partial Regex LineRegex();

    public (bool Valid, LogRecord? Record, LogError? Error) ParseLine(string line, int lineNumber)
    {
        var match = LineRegex().Match(line);
        if (!match.Success)
        {
            return (false, null, new LogError(lineNumber, 1, line, "line does not match the nginx combined shape"));
        }

        return (true, new LogRecord(
        [
            match.Groups["ip"].Value, match.Groups["ident"].Value, match.Groups["user"].Value,
            match.Groups["time"].Value, match.Groups["request"].Value, match.Groups["status"].Value,
            match.Groups["bytes"].Value, match.Groups["referrer"].Value, match.Groups["agent"].Value,
        ]), null);
    }
}

/// <summary>syslog RFC 3164 line: `&lt;pri&gt;Mon DD HH:MM:SS host tag: message`.
/// The incumbent regex mirrors the colon-anchored grammar token: host to the
/// first space, tag up to the colon (a tag without colon is invalid on both
/// sides), message after the colon with leading whitespace trimmed.</summary>
public sealed partial class HandSyslogParser
{
    [GeneratedRegex(@"^<(?<pri>[0-9]{1,3})>(?<ts>[A-Z][a-z]{2}\x20+[0-9]{1,2}\x20[0-9]{2}:[0-9]{2}:[0-9]{2}) (?<tail>[^\n]+)$")]
    private static partial Regex LineRegex();

    public (bool Valid, LogRecord? Record, LogError? Error) ParseLine(string line, int lineNumber)
    {
        var match = LineRegex().Match(line);
        if (!match.Success)
        {
            return (false, null, new LogError(lineNumber, 1, line, "line does not match the RFC 3164 shape"));
        }

        var tail = match.Groups["tail"].Value;
        var space = tail.IndexOf(' ');
        if (space <= 0)
        {
            return (false, null, new LogError(lineNumber, match.Length + 1, line, "no tag separator"));
        }

        var colon = tail.IndexOf(':', space + 1);
        if (colon < 0)
        {
            return (false, null, new LogError(lineNumber, match.Length + space + 2, line, "no colon after the tag"));
        }

        return (true, new LogRecord(
        [
            match.Groups["pri"].Value, match.Groups["ts"].Value, tail[..space],
            tail[(space + 1)..colon], tail[(colon + 1)..].TrimStart(),
        ]), null);
    }
}

/// <summary>logfmt line: `key=value key=value ...`; quoted values carry no
/// whitespace in the corpus (the grammar token stops at whitespace either
/// way). The incumbent is a character scanner splitting on spaces.</summary>
public sealed class HandLogfmtParser
{
    public (bool Valid, LogRecord? Record, LogError? Error) ParseLine(string line, int lineNumber)
    {
        if (line.Length == 0)
        {
            return (false, null, new LogError(lineNumber, 1, line, "empty line"));
        }

        var pairs = new List<string>();
        foreach (var segment in line.Split(' '))
        {
            var eq = segment.IndexOf('=');
            if (eq <= 0)
            {
                return (false, null, new LogError(lineNumber, 1, line, "segment without key=value"));
            }

            var key = segment[..eq];
            var value = segment[(eq + 1)..];
            pairs.Add($"{key}={Pilot.Unquote(value)}");
        }

        return (true, new LogRecord([.. pairs]), null);
    }
}
