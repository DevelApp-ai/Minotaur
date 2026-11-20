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

// Issue #132 case-study pilots (2/3 + 3/3): nginx-combined, syslog RFC 3164
// and logfmt, grammar side. Per-line mode parses one line per fresh session
// (the engine's only supported multi-document mode); file mode parses the
// whole corpus with ONE Initialize+Parse over the merged line sequence
// (binary rule merges keep the GLR path budget in range). The walkers below
// rebuild the same records the hand parsers in HandWritten/HandLogParsers.cs
// produce, so per-line and file-mode outputs are compared record by record.

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Diagnostics;
using DevelApp.StepParser;
using DevelApp.StepLexer;

public sealed class MinotaurLogParsers
{
    private readonly Dictionary<string, string> grammars = new(StringComparer.Ordinal);

    public MinotaurLogParsers()
    {
        foreach (var format in new[] { "nginx-combined", "syslog-3164", "logfmt" })
        {
            this.grammars[format] = Pilot.LoadGrammar($"{(format == "nginx-combined" ? "NginxCombined" : format == "syslog-3164" ? "Syslog3164" : "Logfmt")}.grammar");
        }
    }

    public (bool Valid, LogRecord? Record) ParseLine(string format, string line)
    {
        var result = Pilot.TryParseDocument(this.grammars[format], line);
        if (result is null)
        {
            return (false, null);
        }

        var tokens = result.ResolvedTokens.Where(t => !t.Type.EndsWith("eol", StringComparison.Ordinal)).ToArray();
        var record = format switch
        {
            "nginx-combined" => NginxWalker.RecordFromTokens(line, tokens),
            "syslog-3164" => SyslogWalker.RecordFromTokens(line, tokens),
            _ => LogfmtWalker.RecordFromTokens(line, tokens),
        };
        return (record is not null, record);
    }

    /// <summary>Whole-corpus parse: one Initialize+Parse. Returns the
    /// engine's success flag as Parsed (success=false with an empty resolved
    /// stream and no diagnostics is how the GLR path budget expresses
    /// exhaustion) plus the records when it succeeded.</summary>
    public (bool Parsed, LogRecord[]? Records) ParseFile(string format, string text)
    {
        RealTimeParserSession session;
        try
        {
            session = new RealTimeParserSession(this.grammars[format]);
            session.Initialize(text);
            var result = session.Parse();
            if (!result.Success || result.ResolvedTokens is not { Count: > 0 })
            {
                return (false, null);
            }

            var last = result.ResolvedTokens[^1];
            if (last.StartPosition + last.Length < text.TrimEnd().Length)
            {
                return (false, null);
            }

            return (true, format switch
            {
                "nginx-combined" => NginxWalker.RecordsFromTokens(text, result.ResolvedTokens),
                "syslog-3164" => SyslogWalker.RecordsFromTokens(text, result.ResolvedTokens),
                _ => LogfmtWalker.RecordsFromTokens(text, result.ResolvedTokens),
            });
        }
        catch (Exception)
        {
            return (false, null);
        }
    }
}

internal static class NginxWalker
{
    private const int FieldCount = 6;

    public static LogRecord? RecordFromTokens(string line, StepToken[] tokens)
    {
        if (tokens.Length != FieldCount)
        {
            return null;
        }

        var values = tokens.Select(t => Pilot.Slice(line, t)).ToArray();
        var stb = values[5];
        var statusSplit = stb.IndexOf(' ');
        if (statusSplit <= 0 || stb.Length - statusSplit < 2)
        {
            return null;
        }

        var bytesSplit = stb.IndexOf(' ', statusSplit + 1);
        if (bytesSplit <= 0 || stb.Length - bytesSplit < 2)
        {
            return null;
        }

        var status = stb[..statusSplit];
        var bytes = stb[(statusSplit + 1)..bytesSplit];
        var tail = stb[(bytesSplit + 1)..];
        var separator = tail.IndexOf("\" \"", StringComparison.Ordinal);
        if (tail.Length < 5 || separator <= 0)
        {
            return null;
        }

        var referrer = tail[..(separator + 1)];
        var agent = tail[(separator + 2)..];
        if (!referrer.StartsWith('"') || !referrer.EndsWith('"') || !agent.StartsWith('"') || !agent.EndsWith('"'))
        {
            return null;
        }

        return new LogRecord(
        [
            values[0], values[1], values[2],
            values[3].StartsWith('[') && values[3].EndsWith(']') ? values[3][1..^1] : values[3],
            Pilot.Unquote(values[4]), status, bytes, Pilot.Unquote(referrer), Pilot.Unquote(agent),
        ]);
    }

    public static LogRecord[] RecordsFromTokens(string text, System.Collections.Generic.IReadOnlyList<StepToken> tokens)
    {
        var records = new List<LogRecord>();
        var line = new List<StepToken>();
        foreach (var token in tokens)
        {
            if (token.Type == "eol")
            {
                if (line.Count > 0)
                {
                    records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
                }

                line.Clear();
                continue;
            }

            line.Add(token);
        }

        if (line.Count > 0)
        {
            records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
        }

        return [.. records];
    }
}

internal static class SyslogWalker
{
    private const int FieldCount = 3;

    public static LogRecord? RecordFromTokens(string line, StepToken[] tokens)
    {
        if (tokens.Length != FieldCount)
        {
            return null;
        }

        var pri = Pilot.Slice(line, tokens[0]);
        if (pri.StartsWith('<') && pri.EndsWith('>'))
        {
            pri = pri[1..^1];
        }

        var value = Pilot.Slice(line, tokens[2]);
        var space = value.IndexOf(' ');
        var colon = value.IndexOf(':', space + 1);
        if (space <= 0 || colon < 0 || colon <= space)
        {
            return null;
        }

        return new LogRecord(
        [
            pri,
            Pilot.Slice(line, tokens[1]),
            value[..space],
            value[(space + 1)..colon],
            value[(colon + 1)..].TrimStart(),
        ]);
    }

    public static LogRecord[] RecordsFromTokens(string text, System.Collections.Generic.IReadOnlyList<StepToken> tokens)
    {
        var records = new List<LogRecord>();
        var line = new List<StepToken>();
        foreach (var token in tokens)
        {
            if (token.Type == "eol")
            {
                if (line.Count > 0)
                {
                    records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
                }

                line.Clear();
                continue;
            }

            line.Add(token);
        }

        if (line.Count > 0)
        {
            records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
        }

        return [.. records];
    }
}

internal static class LogfmtWalker
{
    public static LogRecord? RecordFromTokens(string line, StepToken[] tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        var pairs = new List<string>();
        foreach (var token in tokens)
        {
            var raw = Pilot.Slice(line, token);
            var eq = raw.IndexOf('=');
            if (eq <= 0)
            {
                return null;
            }

            pairs.Add($"{raw[..eq]}={Pilot.Unquote(raw[(eq + 1)..])}");
        }

        return new LogRecord([.. pairs]);
    }

    public static LogRecord[] RecordsFromTokens(string text, System.Collections.Generic.IReadOnlyList<StepToken> tokens)
    {
        var records = new List<LogRecord>();
        var line = new List<StepToken>();
        foreach (var token in tokens)
        {
            if (token.Type == "eol")
            {
                if (line.Count > 0)
                {
                    records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
                }

                line.Clear();
                continue;
            }

            line.Add(token);
        }

        if (line.Count > 0)
        {
            records.Add(RecordFromTokens(text, [.. line]) ?? new LogRecord([]));
        }

        return [.. records];
    }
}

/// <summary>Deterministic per-format corpora. Per-line mode measures 5,000
/// generated lines per format; file mode measures the first 30 lines per
/// format (the GLR path-budget ceiling differs per format and whole-file
/// parses above it fail; the file corpus is deliberately identical in size
/// across the three formats so the walls stay comparable). The two measured
/// scales and byte counts are recorded per format, never asserted.</summary>
internal static class LogCorpus
{
    public static string[] Lines(string format, int count, int seed)
    {
        var lines = new string[count];
        var generator = Generator(format);
        for (var i = 0; i < count; i++)
        {
            lines[i] = generator(new Random(unchecked(seed + i * 1_000_003)));
        }

        return lines;
    }

    private static Func<Random, string> Generator(string format)
    {
        return format switch
        {
            "nginx-combined" => NginxLine,
            "syslog-3164" => SyslogLine,
            _ => LogfmtLine,
        };
    }

    private static readonly string[] NginxPaths =
    [
        "/index.html", "/api/v1/users", "/api/v2/orders", "/static/app.js", "/health",
        "/search?q=rnd", "/api/v1/session", "/assets/logo.svg", "/favicon.ico", "/a/b/c",
    ];

    private static readonly string[] NginxAgents =
    [
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120",
        "curl/8.5.0",
        "Go-http-client/2.0",
    ];

    private static string NginxLine(Random rnd)
    {
        var ip = $"{rnd.Next(10, 240)}.{rnd.Next(10, 240)}.{rnd.Next(10, 240)}.{rnd.Next(10, 240)}";
        var time = $"20/Nov/2026:{rnd.Next(24):D2}:{rnd.Next(60):D2}:{rnd.Next(60):D2} +0100";
        var verb = rnd.Next(100) switch
        {
            < 60 => "GET",
            < 85 => "POST",
            < 95 => "PUT",
            _ => "DELETE",
        };
        var http = rnd.Next(100) switch
        {
            < 90 => "1.1",
            < 95 => "1.0",
            _ => "2.0",
        };
        var status = rnd.Next(100) switch
        {
            < 55 => "200",
            < 65 => "201",
            < 72 => "204",
            < 80 => "301",
            < 85 => "304",
            < 92 => "404",
            _ => "500",
        };
        var bytes = rnd.Next(1000, 100_000);
        var referrer = rnd.Next(100) < 70 ? "-" : $"https://example.net/page{rnd.Next(1, 50)}";
        return $"{ip} - - [{time}] \"{verb} {NginxPaths[rnd.Next(NginxPaths.Length)]} HTTP/{http}\" {status} {bytes} \"{referrer}\" \"{NginxAgents[rnd.Next(NginxAgents.Length)]}\"";
    }

    private static readonly string[] SyslogHosts = ["gw-01", "app-02", "db-03", "lb-04", "cache-05"];

    private static readonly string[] SyslogMessages =
    [
        "connection established", "cache hit ratio 0.94", "retrying upstream after 250ms",
        "user session refreshed", "queue depth 12", "disk usage 61 percent",
        "cert renewed for api.example.net", "background sweep completed",
    ];

    private static string SyslogLine(Random rnd)
    {
        var pri = rnd.Next(0, 192);
        var day = rnd.Next(1, 29);
        var dayText = day < 10 ? $" {day}" : day.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var ts = $"Nov {dayText} {rnd.Next(24):D2}:{rnd.Next(60):D2}:{rnd.Next(60):D2}";
        var host = SyslogHosts[rnd.Next(SyslogHosts.Length)];
        var message = SyslogMessages[rnd.Next(SyslogMessages.Length)];
        return $"<{pri}>{ts} {host} app[{rnd.Next(100, 9999)}]: {message}";
    }

    private static string LogfmtLine(Random rnd)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"ts=2026-11-20T{rnd.Next(24):D2}:{rnd.Next(60):D2}:{rnd.Next(60):D2}Z");
        sb.Append($" level={rnd.Next(100) switch { < 60 => "info", < 85 => "warn", _ => "error" }}");
        var pairs = rnd.Next(4, 8); // 2 built-in + 4-7 more
        for (var i = 0; i < pairs; i++)
        {
            _ = rnd.Next(6) switch
            {
                0 => sb.Append($" service={rnd.Next(100) switch { < 50 => "web", < 80 => "api", _ => "worker" }}"),
                1 => sb.Append($" port={8000 + rnd.Next(999)}"),
                2 => sb.Append($" path={NginxPaths[rnd.Next(NginxPaths.Length)]}"),
                3 => sb.Append($" status={rnd.Next(200, 599)}"),
                4 => sb.Append($" duration_ms={rnd.Next(1, 500)}"),
                _ => sb.Append(" msg=\"session_list_refreshed\""),
            };
        }

        return sb.ToString();
    }
}
