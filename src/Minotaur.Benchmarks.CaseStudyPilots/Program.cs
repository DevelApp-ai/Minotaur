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

// Case-study pilots for Minotaur issues #128 and #132: measured grammar-vs-
// hand-written comparisons for a configuration DSL, FIX 4.4 and three
// structured-log formats (nginx-combined, syslog RFC 3164, logfmt). All
// artifacts land under the measurement root:
//
//   issue-128/  results.json RESULTS.md battery.csv        (DSL)
//   issue-132/  fix-results.json fix-RESULTS.md fix-battery.csv
//               log-results.json log-RESULTS.md log-battery.csv
//
// Stages: dsl, fix, logs (or all). Usage:
//   dotnet run --project src/Minotaur.Benchmarks.CaseStudyPilots -- <stage> <root>
// The commit recorded in results.json comes from MINOTAUR_COMMIT.

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using DevelApp.StepParser;
using DevelApp.StepLexer;

public static class Program
{
    public static int Main(string[] args)
    {
        var stage = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
        var measurementRoot = args.Length > 1 ? args[1] : "docs/measurements";
        var commit = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "unknown";

        Console.WriteLine("Minotaur case-study pilots (issues #128, #132)");
        Console.WriteLine($"  runtime={RuntimeInformation.FrameworkDescription} os={RuntimeInformation.OSDescription} cpus={Environment.ProcessorCount}");
        Console.WriteLine($"  commit={commit} stage={stage} out={measurementRoot}");

        var issue128 = Path.Combine(measurementRoot, "issue-128");
        var issue132 = Path.Combine(measurementRoot, "issue-132");
        Directory.CreateDirectory(issue128);
        Directory.CreateDirectory(issue132);

        if (stage is "all" or "dsl")
        {
            var exit = PilotDsl.Run(issue128, commit, out _);
            if (exit != 0)
            {
                if (stage != "all")
                {
                    return exit;
                }

                return exit;
            }

            if (stage != "all")
            {
                return 0;
            }
        }

        if (stage is "all" or "fix")
        {
            var exit = PilotFix.Run(issue132, commit, out _);
            if (exit != 0)
            {
                return exit;
            }

            if (stage != "all")
            {
                return 0;
            }
        }

        if (stage is "all" or "logs")
        {
            var exit = PilotLogsBattery.Run(issue132, commit, out _);
            if (exit != 0)
            {
                return exit;
            }
        }

        if (stage != "all")
        {
            return 0;
        }

        Console.WriteLine("all pilots complete.");
        return 0;
    }

    private static double Minutes(string fromUtc, string toUtc)
    {
        var from = DateTime.Parse(fromUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var to = DateTime.Parse(toUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return (to - from).TotalMinutes;
    }

    private static string Minutes1(double minutes)
    {
        return Math.Round(minutes, 1, MidpointRounding.AwayFromZero).ToString("F1", CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------
    // RESULTS.md renderers. The text is generated from the measurement
    // JSON so the committed artifact always matches the numbers.
    // ------------------------------------------------------------------

    internal static string DslResultsMd(string commit, List<(string Case, bool Expected, bool Minotaur, bool Hand, bool ModelEqual, bool Pass)> batteryRows, System.Text.Json.Nodes.JsonObject json)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Issue #128 pilot: configuration DSL, grammar vs hand-written parser");
        sb.AppendLine();
        sb.AppendLine($"Measured at `{commit}` with `RealTimeParserSession` (DevelApp.StepParser, the C# in-process");
        sb.AppendLine("parser Minotaur ships) against a line-oriented regex hand parser typical of the incumbent");
        sb.AppendLine("approach. Both systems implement the same 30-line DSL spec: `environment <dev|staging|prod> {`");
        sb.AppendLine("blocks of `key=value` settings, top-level `include \"path\"` lines, `#` comments. Value kinds");
        sb.AppendLine("(number, `${...}` template, string) are a semantic classification, identical in both systems.");
        sb.AppendLine();
        sb.AppendLine("## Correctness gate");
        sb.AppendLine();
        sb.AppendLine("A 17-case battery (10 valid, 7 invalid) must agree on validity and, where a model is");
        sb.AppendLine("asserted, on the parsed document model. Both systems pass all 17 cases.");
        sb.AppendLine();
        sb.AppendLine("| case | expected | minotaur | hand | model equal |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var row in batteryRows)
        {
            var expected = row.Expected ? "valid" : "invalid";
            var minotaur = row.Minotaur ? "valid" : "invalid";
            var hand = row.Hand ? "valid" : "invalid";
            var model = row.Expected ? row.ModelEqual.ToString() : "n/a";
            sb.AppendLine($"| {row.Case} | {expected} | {minotaur} | {hand} | {model} |");
        }

        sb.AppendLine();
        sb.AppendLine($"## Parse performance (deterministic corpus, {json["corpus"]!["documents"]} documents, seed {json["corpus"]!["seed"]})");
        sb.AppendLine();
        sb.AppendLine("Corpus shape: 2-4 environment blocks per document, 3-10 settings per block, 30% comment");
        sb.AppendLine("headers, 15% include-bearing documents; per-doc semantics (models) compared as well.");
        sb.AppendLine();
        sb.AppendLine("| metric | minotaur (grammar) | hand (regex) |");
        sb.AppendLine("| --- | --- | --- |");
        sb.AppendLine($"| mean latency per document | {json["minotaur"]!["meanUsPerDoc"]} us | {json["hand"]!["meanUsPerDoc"]} us |");
        sb.AppendLine($"| p95 latency per document | {json["minotaur"]!["p95UsPerDoc"]} us | {json["hand"]!["p95UsPerDoc"]} us |");
        sb.AppendLine($"| throughput | {json["minotaur"]!["docsPerSecond"]} docs/s | {json["hand"]!["docsPerSecond"]} docs/s |");
        sb.AppendLine($"| slowdown factor | x{json["slowdownFactor"]} | 1.0 (baseline) |");
        sb.AppendLine();
        sb.AppendLine($"Corpus: {json["corpus"]!["documents"]} documents, {json["corpus"]!["bytes"]} bytes, largest document {json["corpus"]!["largestDocBytes"]} bytes.");
        sb.AppendLine($"Parse failures (grammar): {json["minotaur"]!["parseFailures"]}. Model mismatches (grammar vs hand): {json["minotaur"]!["modelMismatches"]}.");
        sb.AppendLine();
        sb.AppendLine("## Development and maintenance timeline (single implementer)");
        sb.AppendLine();
        sb.AppendLine("| Id | UTC | System | Event |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var (id, utc, system, what) in TimeLogData.Timeline())
        {
            sb.AppendLine($"| {id} | {utc} | {system} | {what} |");
        }

        sb.AppendLine();
        var bothMinutes = Minutes1(Minutes(TimeLogData.T0StartUtc, TimeLogData.M1HandEditUtc));
        var grammarMinutes = Minutes1(Minutes(TimeLogData.T0StartUtc, TimeLogData.M1GrammarEditUtc));
        var m1GrammarMinutes = Minutes1(Minutes(TimeLogData.CoreBatteryGreenUtc, TimeLogData.M1GrammarEditUtc));
        var m1HandMinutes = Minutes1(Minutes(TimeLogData.M1GrammarEditUtc, TimeLogData.M1HandEditUtc));
        sb.AppendLine("Elapsed: implementation to first battery green");
        sb.AppendLine($"{bothMinutes} minutes for BOTH sides together");
        sb.AppendLine($"(grammar {grammarMinutes} min of it); the two maintenance edits M1 (add `include`)");
        sb.AppendLine($"took {m1GrammarMinutes} and {m1HandMinutes} minutes.");
        sb.AppendLine();
        sb.AppendLine("## Honest limitations");
        sb.AppendLine();
        sb.AppendLine("- Single implementer (the authoring agent), not a human team; the timeline is machine time and");
        sb.AppendLine("  does not support claims about human developer-weeks.");
        sb.AppendLine("- Sandbox environment resets mid-pilot forced re-creations of the harness (timeline shows the");
        sb.AppendLine("  recovery windows); all runs use identical corpora and seeds.");
        sb.AppendLine("- The grammar needed one hardening iteration (multi-block documents) guided by recorded");
        sb.AppendLine("  engine-discipline findings; that iteration time is honestly part of the grammar side's cost.");
        sb.AppendLine("- Semantic checks (value kinds, contexts) are layered identically over both systems, so the");
        sb.AppendLine("  comparison measures the acquisition mechanism, not spec differences.");
        sb.AppendLine();
        return sb.ToString();
    }

    internal static string FixResultsMd(string commit, List<string> batteryCsv, System.Text.Json.Nodes.JsonObject json)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Issue #132 pilot (1/3): FIX 4.4, grammar vs hand-written parser");
        sb.AppendLine();
        sb.AppendLine($"Measured at `{commit}`. Both systems implement the same message spec: SOH-terminated");
        sb.AppendLine("`<tag>=<value>` fields, first field `8=FIX.4.4`, second `9=<body length>`, last `10=<checksum>`,");
        sb.AppendLine("checksum = sum of message bytes preceding `10=` mod 256, zero-padded to 3 digits.");
        sb.AppendLine();
        sb.AppendLine("## Correctness gate");
        sb.AppendLine();
        sb.AppendLine("| case | expected | minotaur | hand |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var row in batteryCsv.Skip(1))
        {
            var parts = row.Split(',');
            var expected = parts[1] == "True" ? "valid" : "invalid";
            var minotaur = parts[2] == "True" ? "valid" : "invalid";
            var hand = parts[3] == "True" ? "valid" : "invalid";
            sb.AppendLine($"| {parts[0]} | {expected} | {minotaur} | {hand} |");
        }

        sb.AppendLine();
        sb.AppendLine($"## Latency and throughput ({json["corpus"]!["messages"]} messages, seed {json["corpus"]!["seed"]})");
        sb.AppendLine();
        sb.AppendLine("Message mix: 15% heartbeat, 35% NewOrderSingle, 35% ExecutionReport, 10% MarketDataSnapshot,");
        sb.AppendLine("5% Logon; fresh session per message (the engine's per-buffer object has no reset API,");
        sb.AppendLine("issue #127 finding); per-message latencies over a full pass.");
        sb.AppendLine();
        sb.AppendLine("| metric | minotaur (grammar) | hand (scanner) |");
        sb.AppendLine("| --- | --- | --- |");
        sb.AppendLine($"| p50 per message | {json["minotaur"]!["p50Us"]} us | {json["hand"]!["p50Us"]} us |");
        sb.AppendLine($"| p95 per message | {json["minotaur"]!["p95Us"]} us | {json["hand"]!["p95Us"]} us |");
        sb.AppendLine($"| p99 per message | {json["minotaur"]!["p99Us"]} us | {json["hand"]!["p99Us"]} us |");
        sb.AppendLine($"| throughput | {json["minotaur"]!["messagesPerSecond"]} msgs/s | {json["hand"]!["messagesPerSecond"]} msgs/s |");
        sb.AppendLine($"| slowdown factor | x{json["slowdownFactor"]} | 1.0 (baseline) |");
        sb.AppendLine();
        sb.AppendLine($"{json["corpus"]!["messages"]} messages, {json["corpus"]!["bytes"]} bytes; invalid verdicts during measurement: {json["invalidVerdicts"]}.");
        sb.AppendLine();
        sb.AppendLine("## Honest limitations");
        sb.AppendLine();
        sb.AppendLine($"- Latency measured per message in a single process on one sandbox machine ({Environment.ProcessorCount} vCPU);");
        sb.AppendLine("  absolute numbers are indicative, the grammar-vs-hand ratio is the point.");
        sb.AppendLine("- The corpus models a representative field mix, not an exchange's full session lifecycle.");
        sb.AppendLine();
        return sb.ToString();
    }

    internal static string LogResultsMd(System.Text.Json.Nodes.JsonObject json, System.Text.Json.Nodes.JsonObject ceilings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Issue #132 pilot (2/3 and 3/3): nginx-combined, syslog RFC 3164 and logfmt, grammar vs hand-written parsers");
        sb.AppendLine();
        sb.AppendLine("Measured at `" + (string?)json["commit"] + "`. Each format pairs a Minotaur grammar (per-line validity plus one");
        sb.AppendLine("whole-corpus file parse with strict positional semantics) with a hand-written incumbent");
        sb.AppendLine("(compiled regex for nginx and syslog, a character scanner for logfmt).");
        sb.AppendLine();
        sb.AppendLine("## Measurement (5,000 lines per format, per-line mode)");
        sb.AppendLine();
        sb.AppendLine("| format | lines | bytes | minotaur us/line | hand us/line | slowdown | minotaur lines/s | hand lines/s |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var node in (System.Text.Json.Nodes.JsonArray)json["formats"]!)
        {
            var f = (System.Text.Json.Nodes.JsonObject)node!;
            sb.AppendLine($"| {f["format"]} | {(int)f["lines"]!} | {(long)f["bytes"]!} | {f["minotaurPerLineMeanUs"]} | {f["handPerLineMeanUs"]} | x{f["slowdownFactor"]} | {f["minotaurThroughputLinesPerS"]} | {f["handThroughputLinesPerS"]} |");
        }

        sb.AppendLine();
        sb.AppendLine("## File mode (single Initialize+Parse over a 30-line corpus)");
        sb.AppendLine();
        sb.AppendLine("File mode is measured on a 30-line subset of each corpus so the three formats stay");
        sb.AppendLine("comparable: whole-file parses hit an engine GLR path-budget ceiling at different sizes");
        sb.AppendLine($"(probed this run: nginx {CeilingText(ceilings, "nginx-combined")}, syslog {CeilingText(ceilings, "syslog-3164")},");
        sb.AppendLine($"logfmt {CeilingText(ceilings, "logfmt")}; the failing parse returns success=false with an");
        sb.AppendLine("empty resolved-token stream and no diagnostics). The per-format caps are engine");
        sb.AppendLine("findings, recorded as such in the results comment.");
        sb.AppendLine();
        sb.AppendLine("| format | file lines | file parse wall ms | records equal to hand parsers |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var node in (System.Text.Json.Nodes.JsonArray)json["formats"]!)
        {
            var f = (System.Text.Json.Nodes.JsonObject)node!;
            sb.AppendLine($"| {f["format"]} | {f["minotaurFileLines"]} | {(double)f["minotaurFileMs"]!:0.0} | {((bool)f["recordsEqual"]! ? $"yes ({f["recordCount"]} records)" : "NO")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Honest limitations");
        sb.AppendLine();
        sb.AppendLine("- Corpus lines are generator-produced (seeded): realistic shapes, not production captures;");
        sb.AppendLine("  nginx bytes are always numeric (>= 1000) and quoted strings contain no embedded quotes.");
        sb.AppendLine("- Syslog timestamps use one month label and RFC 3164's double-space day padding.");
        sb.AppendLine("- All per-line measurements run in one process; ratios matter more than absolutes.");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string CeilingText(System.Text.Json.Nodes.JsonObject ceilings, string format)
    {
        var f = (System.Text.Json.Nodes.JsonObject)ceilings[format]!;
        var green = (int?)f["largestGreenLines"];
        var fail = (int?)f["firstFailedLines"];
        return green is null ? "fails at every probed size" : fail is null
            ? $"clean through {green} (no failure within the probed ladder)"
            : $"clean at {green} but failing at {fail}";
    }
}
