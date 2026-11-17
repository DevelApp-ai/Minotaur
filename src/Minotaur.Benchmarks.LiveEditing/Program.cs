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

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Minotaur.Parser.LiveEditing;

// ---------------------------------------------------------------------------
// Minotaur issue #196 harness.
//
// Wires a LiveEditorSession (Minotaur.Parser.LiveEditing) to the engine's
// shipped RealTimeParserSession and measures keystroke-to-token-update
// latency of the incremental path (ApplyEdit) against a full-re-parse
// baseline (Initialize) on deterministic, seeded corpora and edit streams.
//
// Usage: dotnet run --project src/Minotaur.Benchmarks.LiveEditing [-- <outputDir>]
// Default output: docs/measurements/issue-196 (results.json + RESULTS.md)
// ---------------------------------------------------------------------------

const int Seed = 196;
const int WarmupEdits = 10;
const int MeasuredEdits = 75;
int[] corpusSizes = { 150, 800, 3500 };
string[] editTypeNames = { "single-char-insert", "word-completion", "line-insertion", "block-paste" };

var outputDir = args.Length > 0
    ? args[0]
    : Path.Combine("docs", "measurements", "issue-196");
Directory.CreateDirectory(outputDir);

Console.WriteLine($"Minotaur issue-196 live-editing latency harness");
Console.WriteLine($"  runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
Console.WriteLine($"  os:      {RuntimeInformation.OSDescription}");
Console.WriteLine($"  cpus:    {Environment.ProcessorCount}");
Console.WriteLine($"  seed:    {Seed}, warmup edits: {WarmupEdits}, measured edits: {MeasuredEdits}");
Console.WriteLine();

var report = new JsonObject
{
    ["issue"] = "DevelApp-ai/Minotaur#196",
    ["harness"] = "Minotaur.Benchmarks.LiveEditing",
    ["seed"] = Seed,
    ["warmupEdits"] = WarmupEdits,
    ["measuredEdits"] = MeasuredEdits,
    ["environment"] = new JsonObject
    {
        ["framework"] = RuntimeInformation.FrameworkDescription,
        ["os"] = RuntimeInformation.OSDescription,
        ["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
        ["processorCount"] = Environment.ProcessorCount,
    },
    ["scenarios"] = new JsonArray(),
};

var resultsDir = (report["scenarios"] as JsonArray)!;
var markdownRows = new List<string>();
var totalEdits = WarmupEdits + MeasuredEdits;

// Per-scenario checkpoint: the harness is deterministic (fixed seeds), so
// a scenario measured in an earlier invocation is identical to what this
// invocation would produce. Long runs can therefore resume after an
// interruption instead of starting over.
var checkpointPath = Path.Combine(outputDir, "checkpoint.json");
var completed = new Dictionary<string, JsonObject>();
if (File.Exists(checkpointPath))
{
    var stored = JsonNode.Parse(File.ReadAllText(checkpointPath))!.AsObject();
    foreach (var entry in stored)
    {
        completed[entry.Key] = JsonNode.Parse(entry.Value!.GetValue<string>())!.AsObject();
    }

    Console.WriteLine($"  resuming: {completed.Count} scenario(s) already measured");
    Console.WriteLine();
}

var orderedKeys = new List<string>();
for (var sizeIndex = 0; sizeIndex < corpusSizes.Length; sizeIndex++)
{
    var corpusLines = corpusSizes[sizeIndex];

    for (var editKind = 0; editKind < editTypeNames.Length; editKind++)
    {
        var editTypeName = editTypeNames[editKind];
        var scenarioSeed = Seed + (corpusLines * 397) + (editKind * 100003);
        var key = $"{corpusLines}|{editTypeName}";
        orderedKeys.Add(key);

        if (!completed.TryGetValue(key, out var scenario))
        {
            var planRng = new Random(scenarioSeed);

            // Deterministic edit stream: identical for both modes.
            var versions = new List<string>(totalEdits + 1) { GenerateCorpus(corpusLines) };
            for (var i = 1; i <= totalEdits; i++)
            {
                versions.Add(ApplyNextEdit(versions[i - 1], editKind, planRng, i));
            }

            var incremental = RunScenario(LiveEditingMode.Incremental, versions);
            var fullReparse = RunScenario(LiveEditingMode.FullReparse, versions);

            scenario = new JsonObject
            {
                ["corpusLines"] = corpusLines,
                ["editType"] = editTypeName,
                ["seed"] = scenarioSeed,
                ["incremental"] = ToJson(incremental, includeReuse: true),
                ["fullReparse"] = ToJson(fullReparse, includeReuse: false),
            };
            completed[key] = scenario;

            var checkpoint = new JsonObject();
            foreach (var (measuredKey, measured) in completed)
            {
                checkpoint[measuredKey] = measured.ToJsonString();
            }

            File.WriteAllText(checkpointPath, checkpoint.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        var inc = scenario["incremental"]!.AsObject();
        var full = scenario["fullReparse"]!.AsObject();
        var incP50 = inc["p50Microseconds"]!.GetValue<double>();
        var fullP50 = full["p50Microseconds"]!.GetValue<double>();
        Console.WriteLine(
            $"  corpus {scenario["corpusLines"]!.GetValue<int>(),5} lines | {editTypeName,-20} | " +
            $"inc p50/p95/p99: {incP50:F1}/{inc["p95Microseconds"]!.GetValue<double>():F1}/{inc["p99Microseconds"]!.GetValue<double>():F1} us | " +
            $"full p50: {fullP50:F1} us | " +
            $"speedup p50: {fullP50 / Math.Max(incP50, 0.01):F1}x");
    }
}

foreach (var key in orderedKeys)
{
    resultsDir.Add(JsonNode.Parse(completed[key].ToJsonString())!.AsObject());
    markdownRows.Add(ScenarioMarkdownRow(completed[key]));
}

File.WriteAllText(Path.Combine(outputDir, "results.json"), report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(outputDir, "RESULTS.md"), BuildResultsMarkdown(markdownRows));
File.Delete(checkpointPath);
Console.WriteLine();
Console.WriteLine($"Wrote {Path.Combine(outputDir, "results.json")} and RESULTS.md");

// ------------------------- scenario execution -------------------------

ScenarioResult RunScenario(LiveEditingMode mode, List<string> versions)
{
    var session = new LiveEditorSession(LiveEditorGrammars.CSharpLike)
    {
        Mode = mode,
    };
    session.Open(versions[0]);

    // The shipped session silently stops lexing at input no rule matches
    // (all active paths die) and returns a truncated token list without
    // error. Guard the measurement: a corpus that does not lex to the end
    // would make the latency numbers meaningless.
    var opened = session.Session.Tokens;
    var coveredTo = opened.Count > 0 ? opened[^1].StartPosition + opened[^1].Length : 0;
    if (opened.Count == 0 || coveredTo != versions[0].Length)
    {
        throw new InvalidOperationException(
            $"Grammar does not cover the corpus ({versions[0].Length} chars); the token stream ends at {coveredTo}.");
    }

    long allocatedBefore = 0;
    double cpuBeforeMs = 0;
    var process = Process.GetCurrentProcess();
    var samples = new List<double>(MeasuredEdits);

    for (var i = 1; i < versions.Count; i++)
    {
        if (i == WarmupEdits + 1)
        {
            // Measured window starts after JIT warm-up. The explicit collects
            // are deliberate benchmarking practice (stable allocation-delta
            // baseline), so suppress the CodeQL quality alerts on them.
            GC.Collect(); // lgtm[cs/call-to-gc-collect]
            GC.WaitForPendingFinalizers();
            GC.Collect(); // lgtm[cs/call-to-gc-collect]
            allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            cpuBeforeMs = process.TotalProcessorTime.TotalMilliseconds;
        }

        var latency = session.OnTextChange(versions[i]);
        if (latency is null)
        {
            throw new InvalidOperationException($"Edit {i} produced no change in mode {mode}.");
        }

        if (i > WarmupEdits)
        {
            samples.Add(latency.Value.TotalMicroseconds);
        }
    }

    var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
    var cpuAfterMs = process.TotalProcessorTime.TotalMilliseconds;

    var recorder = new LatencyRecorder();
    foreach (var sample in samples)
    {
        recorder.AddMicroseconds(sample);
    }

    var reuseWindow = mode == LiveEditingMode.Incremental
        ? session.ReuseRatios.TakeLast(MeasuredEdits).ToList()
        : [];

    return new ScenarioResult(
        Summary: LatencySummary.From(recorder),
        SamplesMicroseconds: samples,
        AllocatedBytesDelta: allocatedAfter - allocatedBefore,
        CpuMilliseconds: cpuAfterMs - cpuBeforeMs,
        WorkingSetBytes: process.WorkingSet64,
        MeanReuseRatio: reuseWindow.Count > 0 ? reuseWindow.Average() : 0.0,
        TokenCount: session.TokenCount);
}

static JsonObject ToJson(ScenarioResult result, bool includeReuse)
{
    var json = new JsonObject
    {
        ["p50Microseconds"] = result.Summary.P50Microseconds,
        ["p95Microseconds"] = result.Summary.P95Microseconds,
        ["p99Microseconds"] = result.Summary.P99Microseconds,
        ["meanMicroseconds"] = result.Summary.MeanMicroseconds,
        ["minMicroseconds"] = result.Summary.MinMicroseconds,
        ["maxMicroseconds"] = result.Summary.MaxMicroseconds,
        ["samplesMicroseconds"] = new JsonArray(result.SamplesMicroseconds.Select(s => JsonValue.Create(Math.Round(s, 3))).ToArray()),
        ["allocatedBytesDelta"] = result.AllocatedBytesDelta,
        ["cpuMilliseconds"] = Math.Round(result.CpuMilliseconds, 2),
        ["workingSetBytes"] = result.WorkingSetBytes,
        ["finalTokenCount"] = result.TokenCount,
    };

    if (includeReuse)
    {
        json["meanReuseRatio"] = Math.Round(result.MeanReuseRatio, 4);
    }

    return json;
}

static string Fmt(double microseconds) => microseconds >= 1000
    ? $"{microseconds / 1000.0:F2} ms"
    : $"{microseconds:F1} us";

static string ScenarioMarkdownRow(JsonObject scenario)
{
    var inc = scenario["incremental"]!.AsObject();
    var full = scenario["fullReparse"]!.AsObject();

    // Round-trip through the JSON text so both long-backed and double-backed
    // numbers (fresh in-process values and values reparsed from the
    // checkpoint) convert without a node-type mismatch.
    static double Get(JsonObject o, string name) =>
        double.Parse(o[name]!.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture);

    var incP50 = Get(inc, "p50Microseconds");
    var fullP50 = Get(full, "p50Microseconds");
    var incAllocMb = Get(inc, "allocatedBytesDelta") / 1024.0 / 1024.0;
    var fullAllocMb = Get(full, "allocatedBytesDelta") / 1024.0 / 1024.0;

    return $"| {scenario["corpusLines"]!.GetValue<int>()} | {scenario["editType"]!.GetValue<string>()} | " +
        $"{Fmt(incP50)} | {Fmt(Get(inc, "p95Microseconds"))} | {Fmt(Get(inc, "p99Microseconds"))} | " +
        $"{Fmt(fullP50)} | {Fmt(Get(full, "p95Microseconds"))} | {Fmt(Get(full, "p99Microseconds"))} | " +
        $"{fullP50 / Math.Max(incP50, 0.01):F1}x | " +
        $"{Get(inc, "meanReuseRatio"):F3} | " +
        $"{incAllocMb:F2} | {fullAllocMb:F2} | " +
        $"{Get(inc, "cpuMilliseconds"):F0} | {Get(full, "cpuMilliseconds"):F0} |";
}

// ------------------------- corpus and edit stream -------------------------

static string GenerateCorpus(int targetLines)
{
    var sb = new StringBuilder();
    var lines = 0;
    var index = 0;

    while (lines < targetLines)
    {
        var block = new[]
        {
            $"public class Widget{index}",
            "{",
            $"    private int count{index} = {index};",
            $"    private const string Name{index} = \"widget-{index}\";",
            "",
            $"    public int Calculate{index}(int input)",
            "    {",
            $"        var result = input * {index + 1} + count{index};",
            "        if (result > 100)",
            "        {",
            $"            return result; # over threshold for widget {index}",
            "        }",
            "        return result;",
            "    }",
            "}",
            "",
        };

        foreach (var line in block)
        {
            if (lines >= targetLines)
            {
                break;
            }

            sb.AppendLine(line);
            lines++;
        }

        index++;
    }

    return sb.ToString();
}

static string ApplyNextEdit(string text, int editKind, Random rng, int editIndex)
{
    var lineStarts = new List<int>();
    for (var i = text.IndexOf('\n'); i >= 0 && i < text.Length - 1; i = text.IndexOf('\n', i + 1))
    {
        lineStarts.Add(i + 1);
    }

    var insertAtLine = lineStarts.Count > 0 ? rng.Next(lineStarts.Count) : 0;
    var lineStart = lineStarts.Count > 0 ? lineStarts[insertAtLine] : 0;
    var lineEnd = text.IndexOf('\n', lineStart);
    if (lineEnd < 0)
    {
        lineEnd = text.Length;
    }

    switch (editKind)
    {
        case 0: // single-char-insert: one identifier character mid-line
            {
                var column = lineEnd > lineStart ? rng.Next(lineEnd - lineStart) : 0;
                var offset = lineStart + column;
                return text.Insert(offset, "x");
            }

        case 1: // word-completion: extend the first identifier on the line
            {
                var line = text[lineStart..lineEnd];
                var wordEnd = -1;
                for (var i = 0; i < line.Length; i++)
                {
                    if (char.IsAsciiLetterOrDigit(line[i]))
                    {
                        wordEnd = i + 1;
                    }
                    else if (wordEnd >= 0)
                    {
                        break;
                    }
                }

                var offset = lineStart + (wordEnd >= 0 ? wordEnd : line.Length);
                return text.Insert(offset, "Data");
            }

        case 2: // line-insertion: one new statement line at a line boundary
            {
                return text.Insert(lineStart, $"        var temp{editIndex} = count + {editIndex};\n");
            }

        default: // block-paste: a full 12-line method block
            {
                var block = new[]
                {
                "    public int PastedMethod" + editIndex + "(int input)",
                "    {",
                $"        var pasted{editIndex} = input + count{editIndex % 100};",
                "        if (pasted" + editIndex + " > 50)",
                "        {",
                "            return pasted" + editIndex + ";",
                "        }",
                "        return 0;",
                "    }",
                "",
            };
                return text.Insert(lineStart, string.Join('\n', block) + "\n");
            }
    }
}

static string BuildResultsMarkdown(List<string> rows)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Live editing latency measurements (Minotaur issue #196)");
    sb.AppendLine();
    sb.AppendLine("Keystroke-to-token-update latency of `RealTimeParserSession.ApplyEdit` (incremental)");
    sb.AppendLine("vs a full re-parse baseline (`RealTimeParserSession.Initialize`), measured by");
    sb.AppendLine($"`src/Minotaur.Benchmarks.LiveEditing` with seed {Seed}, {WarmupEdits} warm-up edits and {MeasuredEdits}");
    sb.AppendLine("measured edits per scenario. Identical edit streams are applied to both modes.");
    sb.AppendLine();
    sb.AppendLine("Raw per-edit microsecond samples: `results.json`.");
    sb.AppendLine();
    sb.AppendLine("| Corpus (lines) | Edit type | Inc p50 | Inc p95 | Inc p99 | Full p50 | Full p95 | Full p99 | p50 speedup | Mean reuse | Inc alloc (MB) | Full alloc (MB) | Inc CPU (ms) | Full CPU (ms) |");
    sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
    foreach (var row in rows)
    {
        sb.AppendLine(row);
    }

    sb.AppendLine();
    sb.AppendLine("Notes:");
    sb.AppendLine();
    sb.AppendLine("- Latency is the wall-clock time of the single `ApplyEdit`/`Initialize` call that");
    sb.AppendLine("  produces the updated token stream (the session's own stopwatch).");
    sb.AppendLine("- Allocation delta is `GC.GetTotalAllocatedBytes` over the measured window only;");
    sb.AppendLine("  CPU is `Process.TotalProcessorTime` over the same window.");
    sb.AppendLine("- Reuse ratio is the engine-reported share of tail tokens reused by `ApplyEdit`.");
    sb.AppendLine("- Text is ASCII-only by design: the engine documents UTF-8 byte offsets, and");
    sb.AppendLine("  `TextEditCalculator` enforces the ASCII assumption.");
    sb.AppendLine();
    return sb.ToString();
}

// ------------------------- record -------------------------

internal sealed record ScenarioResult(
    LatencySummary Summary,
    List<double> SamplesMicroseconds,
    long AllocatedBytesDelta,
    double CpuMilliseconds,
    long WorkingSetBytes,
    double MeanReuseRatio,
    int TokenCount);
