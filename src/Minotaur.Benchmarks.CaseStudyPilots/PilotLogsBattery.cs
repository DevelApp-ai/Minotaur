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

// Issue #132 structured-logs pilot: correctness battery (19 cases across
// nginx-combined, syslog RFC 3164 and logfmt), per-line measurement over
// 5,000 generated lines per format, and one whole-corpus file parse per
// format over a 30-line subset (the sizes stay identical across formats so
// the file-mode walls are comparable). Timings are recorded, never asserted.

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Diagnostics;
using System.Text.Json.Nodes;

public sealed record FormatStats(
    string Format,
    int Lines,
    long Bytes,
    double MinotaurPerLineMeanUs,
    double MinotaurThroughputLinesPerS,
    double MinotaurFileMs,
    int MinotaurFileLines,
    bool MinotaurFileParsed,
    double HandPerLineMeanUs,
    double HandThroughputLinesPerS,
    double SlowdownFactor,
    bool RecordsEqual,
    int RecordCount);

internal static class LogsBattery
{
    public sealed record BatteryRow(string Format, string Id, bool Expected, bool Minotaur, bool Hand, bool Pass);

    public static (string Format, string Id, string Text, bool ExpectedValid)[] Cases() =>

    [
        ("nginx-combined", "V1", "10.0.0.15 - - [20/Nov/2026:13:05:00 +0100] \"GET /index.html HTTP/1.1\" 200 2043 \"-\" \"curl/8.5.0\"", true),
        ("nginx-combined", "V2", "192.168.4.22 - - [20/Nov/2026:08:00:12 +0100] \"POST /api/v1/session HTTP/1.1\" 201 1512 \"https://example.net/page3\" \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120\"", true),
        ("nginx-combined", "V3", "172.16.2.9 - - [20/Nov/2026:23:59:59 +0100] \"GET /search?q=rnd HTTP/2.0\" 304 1280 \"-\" \"Go-http-client/2.0\"", true),
        ("nginx-combined", "I1", "10.0.0.15 - - [20/Nov/2026:13:05:00 +0100] \"GET /x HTTP/1.1\" 200 1000 \"-\" \"curl/8.5.0", false),
        ("nginx-combined", "I2", "10.0.0.15", false),
        ("nginx-combined", "I3", "10.0.0.15 - - [20/Nov/2026:13:05:00 +0100] \"GET /x HTTP/1.1\" dev 1000 \"-\" \"curl/8.5.0\"", false),
        ("nginx-combined", "I4", "10.0.0.15 - - [20/Nov/2026:13:05:00 +0100] \"GET /x HTTP/1.1\" 200 999 \"-\" \"curl/8.5.0\"", false),

        ("syslog-3164", "V1", "<14>Nov 20 19:05:01 gw-01 app[1234]: connection established", true),
        ("syslog-3164", "V2", "<13>Nov  5 09:12:45 lb-04 app[712]: retrying upstream after 250ms", true),
        ("syslog-3164", "V3", "<190>Nov 20 23:59:59 cache-05 app[9001]: disk usage 61 percent", true),
        ("syslog-3164", "I1", "Nov 20 19:05:01 gw-01 app[1234]: no pri", false),
        ("syslog-3164", "I2", "<14>Xy 20 19:05:01 gw-01 app[1234]: bad month", false),
        ("syslog-3164", "I3", "<14>Nov 20 19:05:01 gw-01 app[1234] no colon", false),

        ("logfmt", "V1", "level=info msg=\"listening\" port=8080", true),
        ("logfmt", "V2", "ts=2026-11-20T19:02:33Z level=info path=/api/v1/users status=200 duration_ms=42 service=api", true),
        ("logfmt", "V3", "ts=2026-11-20T22:00:00Z level=warn msg=\"session_list_refreshed\" retry=3", true),
        ("logfmt", "I1", "level=info listening port=8080", false),
        ("logfmt", "I2", "", false),
        ("logfmt", "I3", "level=info msg=hello world", false),
    ];

    public static (List<LogsBattery.BatteryRow> Rows, bool Green, List<string> Csv) RunBattery(MinotaurLogParsers minotaur, HandNginxParser nginx, HandSyslogParser syslog, HandLogfmtParser logfmt)
    {
        var rows = new List<BatteryRow>();
        var csv = new List<string> { "format,case,expected,minotaur,hand,pass" };
        var green = true;
        foreach (var (format, id, text, expected) in Cases())
        {
            var (minotaurValid, minotaurRecord) = minotaur.ParseLine(format, text);
            bool handValid;
            LogRecord? handRecord;
            switch (format)
            {
                case "nginx-combined":
                    var n = nginx.ParseLine(text, 1);
                    handValid = n.Valid;
                    handRecord = n.Record;
                    break;
                case "syslog-3164":
                    var s = syslog.ParseLine(text, 1);
                    handValid = s.Valid;
                    handRecord = s.Record;
                    break;
                default:
                    var l = logfmt.ParseLine(text, 1);
                    handValid = l.Valid;
                    handRecord = l.Record;
                    break;
            }

            var pass = minotaurValid == expected && handValid == expected
                && (!expected || LogRecords.Equal(minotaurRecord, handRecord));
            green &= pass;
            rows.Add(new BatteryRow(format, id, expected, minotaurValid, handValid, pass));
            csv.Add($"{format},{id},{expected},{minotaurValid},{handValid},{pass}");
            Console.WriteLine($"  {format} {id} expected={expected} minotaur={minotaurValid} hand={handValid} pass={pass}");
        }

        return (rows, green, csv);
    }
}

public static class PilotLogsBattery
{
    public const int LogSeed = 210001;

    public static int Run(string outDir, string commit, out List<FormatStats> stats)
    {
        Console.WriteLine();
        Console.WriteLine("== pilot: log formats (issue #132) ==");
        var minotaur = new MinotaurLogParsers();
        var nginx = new HandNginxParser();
        var syslog = new HandSyslogParser();
        var logfmt = new HandLogfmtParser();

        var (rows, batteryGreen, csv) = LogsBattery.RunBattery(minotaur, nginx, syslog, logfmt);
        File.WriteAllLines(Path.Combine(outDir, "log-battery.csv"), csv);

        stats = [];
        var measurementGreen = true;

        foreach (var format in new[] { "nginx-combined", "syslog-3164", "logfmt" })
        {
            Console.WriteLine($"  [measure] {format}: per-line 5000 + file mode 30...");
            stats.Add(Measure(format, minotaur, nginx, syslog, logfmt, out var formatOk));
            measurementGreen &= formatOk;
            Console.WriteLine($"  [measure] {format} done ok={formatOk}");
        }

        var resultsJson = new JsonObject
        {
            ["commit"] = commit,
            ["formats"] = new JsonArray(stats.Select(s => new JsonObject
            {
                ["format"] = s.Format,
                ["lines"] = s.Lines,
                ["bytes"] = s.Bytes,
                ["minotaurPerLineMeanUs"] = Math.Round(s.MinotaurPerLineMeanUs, 2),
                ["minotaurThroughputLinesPerS"] = (int)Math.Round(s.MinotaurThroughputLinesPerS, 0),
                ["minotaurFileMs"] = Math.Round(s.MinotaurFileMs, 1),
                ["minotaurFileLines"] = s.MinotaurFileLines,
                ["minotaurFileParsed"] = s.MinotaurFileParsed,
                ["handPerLineMeanUs"] = Math.Round(s.HandPerLineMeanUs, 2),
                ["handThroughputLinesPerS"] = (int)Math.Round(s.HandThroughputLinesPerS, 0),
                ["slowdownFactor"] = Math.Round(s.SlowdownFactor, 1),
                ["recordsEqual"] = s.RecordsEqual,
                ["recordCount"] = s.RecordCount,
            }).ToArray()),
        };
        File.WriteAllText(Path.Combine(outDir, "log-results.json"), resultsJson.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outDir, "log-RESULTS.md"), Program.LogResultsMd(resultsJson, ProbeCeilings(minotaur, stats.Select(s => s.Format).ToArray())));

        Console.WriteLine($"  wrote {outDir}/log-results.json and log-RESULTS.md");

        if (!batteryGreen || !measurementGreen)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>Per-format ceilings for the whole-file GLR path budget,
    /// probed on the deterministic corpus: the ladder records the largest
    /// green size and the first failing size per format. The failing parse
    /// returns success=false with an empty resolved-token stream and no
    /// diagnostics (recorded as an engine finding).</summary>
    internal static JsonObject ProbeCeilings(MinotaurLogParsers minotaur, string[] formats)
    {
        var probe = new JsonObject();
        foreach (var format in formats)
        {
            var ladder = format == "logfmt"
                ? new[] { 10, 20, 30, 40 }
                : new[] { 500, 1000, 2000 };
            int? largestGreen = null;
            int? firstFail = null;
            foreach (var size in ladder)
            {
                Console.WriteLine($"  [ceiling probe] {format} {size} lines...");
                var corpus = LogCorpus.Lines(format, size, LogSeed);
                var text = string.Concat(corpus.Select(l => l + "\n"));
                var (parsed, _) = minotaur.ParseFile(format, text);
                Console.WriteLine($"  [ceiling probe] {format} {size} parsed={parsed}");
                if (parsed)
                {
                    largestGreen = size;
                }
                else if (firstFail is null)
                {
                    firstFail = size;
                    break;
                }
            }

            probe[format] = new JsonObject
            {
                ["largestGreenLines"] = largestGreen,
                ["firstFailedLines"] = firstFail,
            };
        }

        return probe;
    }

    internal static FormatStats Measure(string format, MinotaurLogParsers minotaur, HandNginxParser nginx, HandSyslogParser syslog, HandLogfmtParser logfmt, out bool ok)
    {
        const int linesPerFormat = 5000;
        const int fileLines = 30;
        var lines = LogCorpus.Lines(format, linesPerFormat, LogSeed);
        long bytes = 0;
        foreach (var line in lines)
        {
            bytes += System.Text.Encoding.UTF8.GetByteCount(line) + 1;
        }

        // Warmup both parse paths (JIT).
        foreach (var line in lines.Take(30))
        {
            _ = minotaur.ParseLine(format, line);
            _ = ParseHandLine(format, line, nginx, syslog, logfmt);
        }

        var minotaurSamples = new double[lines.Length];
        var handSamples = new double[lines.Length];
        var parseFailures = 0;
        var recordMismatches = 0;

        var wall = Stopwatch.StartNew();
        for (var i = 0; i < lines.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var (valid, record) = minotaur.ParseLine(format, lines[i]);
            sw.Stop();
            minotaurSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;

            var hand = ParseHandLine(format, lines[i], nginx, syslog, logfmt);
            if (!valid || hand.Valid is false)
            {
                parseFailures++;
            }
            else if (!LogRecords.Equal(record, hand.Record))
            {
                recordMismatches++;
            }
        }

        wall.Stop();
        var minotaurThroughput = lines.Length / wall.Elapsed.TotalSeconds;

        wall.Restart();
        for (var i = 0; i < lines.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var hand = ParseHandLine(format, lines[i], nginx, syslog, logfmt);
            sw.Stop();
            handSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;
        }

        wall.Stop();
        var handThroughput = lines.Length / wall.Elapsed.TotalSeconds;

        // File mode: one Initialize+Parse over the first 30 lines.
        var fileCorpus = lines.Take(fileLines).ToArray();
        var fileText = string.Concat(fileCorpus.Select(l => l + "\n"));
        _ = minotaur.ParseFile(format, fileText); // warmup
        var fileWatch = Stopwatch.StartNew();
        var (parsed, records) = minotaur.ParseFile(format, fileText);
        fileWatch.Stop();

        var handRecords = fileCorpus.Select(l => ParseHandLine(format, l, nginx, syslog, logfmt).Record).ToArray();
        var recordsEqual = parsed && records is not null && records.Length == fileLines
            && handRecords.All(r => r is not null)
            && LogRecords.ListsEqual(records!, handRecords!);

        ok = parseFailures == 0 && recordMismatches == 0 && parsed && recordsEqual && records is { Length: fileLines };

        var minotaurMean = PilotDsl.Mean(minotaurSamples);
        var handMean = PilotDsl.Mean(handSamples);
        return new FormatStats(
            format,
            linesPerFormat,
            bytes,
            minotaurMean,
            minotaurThroughput,
            fileWatch.Elapsed.TotalMilliseconds,
            fileLines,
            parsed,
            handMean,
            handThroughput,
            handThroughput / minotaurThroughput,
            recordsEqual,
            records?.Length ?? 0);
    }

    internal static (bool? Valid, LogRecord? Record) ParseHandLine(string format, string line, HandNginxParser nginx, HandSyslogParser syslog, HandLogfmtParser logfmt)
    {
        return format switch
        {
            "nginx-combined" => (nginx.ParseLine(line, 1).Valid, nginx.ParseLine(line, 1).Record),
            "syslog-3164" => (syslog.ParseLine(line, 1).Valid, syslog.ParseLine(line, 1).Record),
            _ => (logfmt.ParseLine(line, 1).Valid, logfmt.ParseLine(line, 1).Record),
        };
    }
}
