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
using DevelApp.StepParser;
using Minotaur.Benchmarks.ParserRuntime;
using Minotaur.Benchmarks.ParserRuntime.Antlr.Generated;
using Minotaur.Benchmarks.ParserRuntime.HandWritten;

// ---------------------------------------------------------------------------
// Minotaur issue #127 harness.
//
// Measures generated-parser runtime performance against two baselines for
// the three case-study DSLs of the DSL workbench article (configuration,
// analytics query and multi-language template):
//
//   Minotaur    the engine's shipped RealTimeParserSession (DevelApp.StepParser,
//               the C#-process target Minotaur ships), a fresh session per
//               document: the session is a per-buffer object with no reset
//               or disposal API, and re-initializing one session across
//               documents accumulates retained memory without bound
//               (verified empirically during this issue: megabytes per
//               Initialize/Parse cycle survive a full collection), so a
//               fresh session per document is the only supported way to
//               parse N independent documents, and the per-document cost
//               includes session construction.
//   Hand-written  single-pass baseline parsers for the exact same languages.
//   ANTLR       generated C# lexers/parsers (Antlr4.Runtime.Standard 4.13.1,
//               fresh lexer/token-stream/parser per document, the standard
//               generated-parser usage).
//
// All three engines parse the identical document stream: deterministic
// corpora of bounded documents per DSL and size class (1K / 10K / 100K
// lines, seed 127, see Corpus). Metrics per engine and size class:
//
//   latency p50/p95  per-document parse time, 10 reps x up to 500 sampled
//                    documents (Stopwatch timestamps per document).
//   throughput       full-corpus pass, MB/s of corpus text.
//   full-parse rate  fraction of documents accepted; Minotaur additionally
//                    requires full consumption (last resolved token ends at
//                    the end of the document, not just Success=True).
//   allocations      managed bytes allocated per document over the full
//                    pass (GC.GetTotalAllocatedBytes delta), plus a
//                    process working-set snapshot.
//
// Cold numbers (first construction / first parse, including JIT) are
// measured in a fresh child process per DSL and printed as one JSON line
// by the --measure-cold child mode; the parent aggregates everything into
// results.json + RESULTS.md.
//
// Usage:
//   dotnet run --project src/Minotaur.Benchmarks.ParserRuntime [-- <outputDir>]
//   dotnet run --project src/Minotaur.Benchmarks.ParserRuntime -- --measure-cold <dslIndex>
//   dotnet run --project src/Minotaur.Benchmarks.ParserRuntime -- --write-corpus <outputDir>
//   dotnet run --project src/Minotaur.Benchmarks.ParserRuntime -- --cell <dsl> <size> <engine>
//   dotnet run --project src/Minotaur.Benchmarks.ParserRuntime -- --merge <outputDir>
// The --cell mode measures one DSL x size x engine cell (indices into
// config/query/template, small/medium/large, minotaur/hand-written/antlr)
// and prints one JSON line; --merge combines cells.ndjson + cold.ndjson
// written by repeated --cell / --measure-cold invocations into the
// artifacts, for machines where the full aggregate run exceeds wall-clock
// limits.
// Default output: docs/measurements/issue-127 (results.json + RESULTS.md)
//
// The harness writes the small and medium corpora (the ones committed under
// docs/corpus/issue-127) with --write-corpus; the large corpora are
// generated at runtime to keep the repository small (SHA-256 recorded).
//
// Timings are recorded, never asserted: re-running the harness regenerates
// docs/measurements/issue-127 on the current machine; the committed
// artifacts document the recorded run.
// ---------------------------------------------------------------------------

const int LatencyReps = 10;
const int LatencySampleMax = 500;
const int WarmupDocs = 50;

var dslNames = new[] { "config", "query", "template" };
var sizeNames = new[] { "small", "medium", "large" };
var engineNames = new[] { "minotaur", "hand-written", "antlr" };

// Child mode: measure cold start of one DSL in this fresh process.
if (args.Length == 2 && args[0] == "--measure-cold" && int.TryParse(args[1], out var coldIndex)
    && coldIndex >= 0 && coldIndex < dslNames.Length)
{
    Console.WriteLine(MeasureCold((CaseDsl)coldIndex));
    return 0;
}

// Corpus writer mode: render the committed small/medium corpus files.
if (args.Length == 2 && args[0] == "--write-corpus")
{
    Directory.CreateDirectory(args[1]);
    foreach (var dsl in Enum.GetValues<CaseDsl>())
    {
        foreach (var size in new[] { CorpusSize.Small, CorpusSize.Medium })
        {
            var path = Path.Combine(args[1], $"{DslName(dsl)}-{SizeName(size)}.txt");
            File.WriteAllText(path, Corpus.Render(dsl, size));
            Console.WriteLine($"wrote {path}");
        }
    }

    return 0;
}

// Cell mode: measure one DSL x size x engine cell and print one JSON line.
// Used to run the harness cell-by-cell on machines with wall-clock limits;
// a --merge invocation afterwards combines the cells into the artifacts.
if (args.Length == 4 && args[0] == "--cell"
    && int.TryParse(args[1], out var cellDsl) && cellDsl >= 0 && cellDsl < dslNames.Length
    && int.TryParse(args[2], out var cellSize) && cellSize >= 0 && cellSize < sizeNames.Length
    && int.TryParse(args[3], out var cellEngine) && cellEngine >= 0 && cellEngine < engineNames.Length)
{
    var dsl = (CaseDsl)cellDsl;
    var size = (CorpusSize)cellSize;
    var engineName = engineNames[cellEngine];
    var docs = new string[Corpus.DocumentCount(dsl, size)];
    long corpusBytes = 0;
    for (var i = 0; i < docs.Length; i++)
    {
        docs[i] = Corpus.Document(dsl, i);
        corpusBytes += Encoding.UTF8.GetByteCount(docs[i]);
    }

    var cellEngineInstance = CreateEngine(engineName, dsl);
    Console.WriteLine(MeasureCell(engineName, cellEngineInstance, docs, corpusBytes,
        DslName(dsl), SizeName(size)).ToJsonString());
    return 0;
}

// Merge mode: combine cells.ndjson + cold.ndjson in the output directory
// (written by --cell / --measure-cold invocations) into results.json +
// RESULTS.md, in the standard DSL x size x engine order.
if (args.Length == 2 && args[0] == "--merge")
{
    var mergeDir = args[1];
    var mergedColdRows = ReadNdjson(Path.Combine(mergeDir, "cold.ndjson"));
    var mergedCells = ReadNdjson(Path.Combine(mergeDir, "cells.ndjson"));
    mergedCells.Sort((left, right) =>
    {
        var byDsl = Array.IndexOf(dslNames, left["dsl"]!.GetValue<string>())
            - Array.IndexOf(dslNames, right["dsl"]!.GetValue<string>());
        if (byDsl != 0)
        {
            return byDsl;
        }

        var bySize = Array.IndexOf(sizeNames, left["size"]!.GetValue<string>())
            - Array.IndexOf(sizeNames, right["size"]!.GetValue<string>());
        if (bySize != 0)
        {
            return bySize;
        }

        return Array.IndexOf(engineNames, left["engine"]!.GetValue<string>())
            - Array.IndexOf(engineNames, right["engine"]!.GetValue<string>());
    });

    var merged = BuildReport(BuildCorpusInfo(), mergedColdRows, mergedCells, "cell");
    File.WriteAllText(Path.Combine(mergeDir, "results.json"), merged.ToString());
    File.WriteAllText(Path.Combine(mergeDir, "RESULTS.md"), BuildResultsMarkdown(merged));
    Console.WriteLine($"merged {mergedCells.Count} cells and {mergedColdRows.Count} cold rows into {mergeDir}");
    return 0;
}

// Parent mode: measure all DSL x size x engine cells and aggregate.
var outputDir = args.Length > 0
    ? args[0]
    : Path.Combine("docs", "measurements", "issue-127");
Directory.CreateDirectory(outputDir);

Console.WriteLine("Minotaur issue #127 parser-runtime harness");
Console.WriteLine($"  runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
Console.WriteLine($"  os:      {RuntimeInformation.OSDescription}");
Console.WriteLine($"  cpus:    {Environment.ProcessorCount}");
Console.WriteLine($"  corpora: seed {Corpus.Seed}, sizes 1K/10K/100K lines");
Console.WriteLine();

var entryAssembly = System.Reflection.Assembly.GetEntryAssembly()!.Location;
var processPath = Environment.ProcessPath!;
var isDotnetDriver = string.Equals(
    Path.GetFileName(processPath), "dotnet", StringComparison.OrdinalIgnoreCase);

// Cold start: one fresh child per DSL.
var coldRows = new List<JsonObject>();
foreach (var (index, dslName) in dslNames.Select((n, i) => (i, n)))
{
    Console.WriteLine($"measuring cold start: {dslName} ...");
    var startInfo = new ProcessStartInfo
    {
        FileName = processPath,
        Arguments = isDotnetDriver
            ? $"\"{entryAssembly}\" --measure-cold {index}"
            : $"--measure-cold {index}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    using var child = Process.Start(startInfo)!;
    var stdout = child.StandardOutput.ReadToEnd();
    var stderr = child.StandardError.ReadToEnd();
    child.WaitForExit();

    if (child.ExitCode != 0)
    {
        Console.WriteLine($"  child FAILED (exit {child.ExitCode}): {stderr.Trim()}");
        continue;
    }

    var line = stdout.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith("{")) ?? "";
    var row = JsonNode.Parse(line)!.AsObject();
    coldRows.Add(row);
    Console.WriteLine($"  minotaur construction {row["minotaurConstructionMs"]!.GetValue<double>():F1} ms, "
        + $"first parse {row["minotaurFirstParseMs"]!.GetValue<double>():F1} ms; "
        + $"hand-written first parse {row["handWrittenFirstParseMs"]!.GetValue<double>():F2} ms; "
        + $"antlr first parse {row["antlrFirstParseMs"]!.GetValue<double>():F1} ms");
}

Console.WriteLine();

// Warm cells: DSL x size x engine.
var cells = new List<JsonObject>();
var corpusCache = new Dictionary<(CaseDsl, CorpusSize), (string[] Docs, long Bytes)>();

foreach (var dsl in Enum.GetValues<CaseDsl>())
{
    var engines = new Dictionary<string, IRuntimeEngine>
    {
        ["minotaur"] = new MinotaurEngine(GrammarText(dsl)),
        ["hand-written"] = new HandWrittenEngine(dsl),
        ["antlr"] = new AntlrEngine(dsl),
    };

    foreach (var size in Enum.GetValues<CorpusSize>())
    {
        if (!corpusCache.TryGetValue((dsl, size), out var corpus))
        {
            var docs = new string[Corpus.DocumentCount(dsl, size)];
            long bytes = 0;
            for (var i = 0; i < docs.Length; i++)
            {
                docs[i] = Corpus.Document(dsl, i);
                bytes += Encoding.UTF8.GetByteCount(docs[i]);
            }

            corpus = (docs, bytes);
            corpusCache[(dsl, size)] = corpus;
        }

        foreach (var engineName in engineNames)
        {
            Console.WriteLine($"measuring {DslName(dsl)}/{SizeName(size)}/{engineName} "
                + $"({corpus.Docs.Length} docs) ...");
            var cell = MeasureCell(engineName, engines[engineName], corpus.Docs, corpus.Bytes,
                DslName(dsl), SizeName(size));
            cells.Add(cell);
            Console.WriteLine($"  p50 {cell["latencyP50Us"]!.GetValue<double>():F1} us, "
                + $"p95 {cell["latencyP95Us"]!.GetValue<double>():F1} us, "
                + $"throughput {cell["throughputMbPerS"]!.GetValue<double>():F1} MB/s, "
                + $"full-parse {cell["fullParseRate"]!.GetValue<double>():P1}");
        }
    }
}

var report = BuildReport(BuildCorpusInfo(), coldRows, cells, "aggregate");

File.WriteAllText(Path.Combine(outputDir, "results.json"), report.ToString());
File.WriteAllText(Path.Combine(outputDir, "RESULTS.md"), BuildResultsMarkdown(report));

Console.WriteLine();
Console.WriteLine($"wrote {Path.Combine(outputDir, "results.json")} and RESULTS.md");
return 0;

// ---------------------------------------------------------------------------

static string MeasureCold(CaseDsl dsl)
{
    var doc = Corpus.Document(dsl, 0);

    // Minotaur session construction in the fresh process (includes JIT).
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var liveBefore = GC.GetTotalMemory(forceFullCollection: true);

    var stopwatch = Stopwatch.StartNew();
    var session = new RealTimeParserSession(GrammarText(dsl));
    stopwatch.Stop();
    var constructionMs = stopwatch.Elapsed.TotalMilliseconds;

    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var retainedBytes = GC.GetTotalMemory(forceFullCollection: true) - liveBefore;

    // First parses in the fresh process (include JIT of each engine's path).
    stopwatch.Restart();
    var minotaurOk = ParseWithSession(session, doc);
    stopwatch.Stop();
    var minotaurFirstParseMs = stopwatch.Elapsed.TotalMilliseconds;

    var handWritten = new HandWrittenEngine(dsl);
    stopwatch.Restart();
    var handWrittenOk = handWritten.ParseDocument(doc);
    stopwatch.Stop();
    var handWrittenFirstParseMs = stopwatch.Elapsed.TotalMilliseconds;

    var antlr = new AntlrEngine(dsl);
    stopwatch.Restart();
    var antlrOk = antlr.ParseDocument(doc);
    stopwatch.Stop();
    var antlrFirstParseMs = stopwatch.Elapsed.TotalMilliseconds;

    GC.KeepAlive(session);

    return new JsonObject
    {
        ["dsl"] = DslName(dsl),
        ["minotaurConstructionMs"] = Math.Round(constructionMs, 3),
        ["minotaurSessionRetainedKb"] = Math.Round(retainedBytes / 1024.0, 1),
        ["minotaurFirstParseMs"] = Math.Round(minotaurFirstParseMs, 3),
        ["minotaurFirstParseOk"] = minotaurOk,
        ["handWrittenFirstParseMs"] = Math.Round(handWrittenFirstParseMs, 3),
        ["handWrittenFirstParseOk"] = handWrittenOk,
        ["antlrFirstParseMs"] = Math.Round(antlrFirstParseMs, 3),
        ["antlrFirstParseOk"] = antlrOk,
    }.ToJsonString();
}

static bool ParseWithSession(RealTimeParserSession session, string document)
{
    try
    {
        session.Initialize(document);
        var result = session.Parse();
        var last = result.Success ? result.ResolvedTokens?.LastOrDefault() : null;
        return last is not null
            && last.StartPosition + last.Length == document.TrimEnd().Length;
    }
    catch (Exception)
    {
        return false;
    }
}

static JsonObject MeasureCell(string engineName, IRuntimeEngine engine, string[] docs, long corpusBytes, string dslName, string sizeName)
{
    // Warmup: JIT the engine's parse path before any measurement.
    foreach (var doc in docs.Take(Math.Min(WarmupDocs, docs.Length)))
    {
        _ = engine.ParseDocument(doc);
    }

    // Latency: 10 reps x deterministic sample of up to 500 documents.
    var sampleCount = Math.Min(LatencySampleMax, docs.Length);
    var sampleIndices = new int[sampleCount];
    for (var i = 0; i < sampleCount; i++)
    {
        sampleIndices[i] = (int)((long)i * docs.Length / sampleCount);
    }

    var latencies = new List<double>(LatencyReps * sampleCount);
    for (var rep = 0; rep < LatencyReps; rep++)
    {
        foreach (var index in sampleIndices)
        {
            var doc = docs[index];
            var start = Stopwatch.GetTimestamp();
            _ = engine.ParseDocument(doc);
            var end = Stopwatch.GetTimestamp();
            latencies.Add((end - start) * 1_000_000.0 / Stopwatch.Frequency);
        }
    }

    var sorted = latencies.ToArray();
    Array.Sort(sorted);

    // Throughput + allocations + validity: one full pass over the corpus.
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);

    long ok = 0;
    var stopwatch = Stopwatch.StartNew();
    foreach (var doc in docs)
    {
        if (engine.ParseDocument(doc))
        {
            ok++;
        }
    }

    stopwatch.Stop();
    var allocatedPerDoc = (GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore)
        / (double)docs.Length;
    var workingSetMb = Environment.WorkingSet / (1024.0 * 1024.0);

    return new JsonObject
    {
        ["dsl"] = dslName,
        ["size"] = sizeName,
        ["engine"] = engineName,
        ["docs"] = docs.Length,
        ["fullParseRate"] = Math.Round(ok / (double)docs.Length, 4),
        ["latencyP50Us"] = Math.Round(Percentile(sorted, 0.50), 2),
        ["latencyP95Us"] = Math.Round(Percentile(sorted, 0.95), 2),
        ["latencyMeanUs"] = Math.Round(sorted.Average(), 2),
        ["throughputMbPerS"] = Math.Round(corpusBytes / stopwatch.Elapsed.TotalSeconds / (1024.0 * 1024.0), 1),
        ["allocatedBytesPerDoc"] = Math.Round(allocatedPerDoc, 0),
        ["workingSetMb"] = Math.Round(workingSetMb, 1),
        ["elapsedS"] = Math.Round(stopwatch.Elapsed.TotalSeconds, 3),
    };
}

static double Percentile(double[] sortedValues, double fraction)
{
    var index = (int)Math.Ceiling(fraction * sortedValues.Length) - 1;
    return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
}

static string GrammarText(CaseDsl dsl) => dsl switch
{
    CaseDsl.Config => CaseStudyDsls.Config,
    CaseDsl.Query => CaseStudyDsls.Query,
    CaseDsl.Template => CaseStudyDsls.Template,
    _ => throw new ArgumentOutOfRangeException(nameof(dsl)),
};

static string DslName(CaseDsl dsl) => dsl switch
{
    CaseDsl.Config => "config",
    CaseDsl.Query => "query",
    CaseDsl.Template => "template",
    _ => throw new ArgumentOutOfRangeException(nameof(dsl)),
};

static string SizeName(CorpusSize size) => size switch
{
    CorpusSize.Small => "small",
    CorpusSize.Medium => "medium",
    CorpusSize.Large => "large",
    _ => throw new ArgumentOutOfRangeException(nameof(size)),
};

static JsonObject BuildReport(JsonArray corpusInfo, List<JsonObject> coldRows, List<JsonObject> cells, string runMode)
{
    return new JsonObject
    {
        ["issue"] = "DevelApp-ai/Minotaur#127",
        ["harness"] = "Minotaur.Benchmarks.ParserRuntime",
        ["minotaurCommit"] = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "unrecorded",
        ["runMode"] = runMode,
        ["corpus"] = new JsonObject
        {
            ["seed"] = Corpus.Seed,
            ["sizes"] = "1K/10K/100K lines",
            ["committed"] = "docs/corpus/issue-127 (small + medium); large generated at runtime",
            ["corpora"] = corpusInfo,
        },
        ["machine"] = new JsonObject
        {
            ["os"] = RuntimeInformation.OSDescription,
            ["runtime"] = RuntimeInformation.FrameworkDescription,
            ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["processorCount"] = Environment.ProcessorCount,
        },
        ["protocol"] = new JsonObject
        {
            ["latencyReps"] = LatencyReps,
            ["latencySampleMax"] = LatencySampleMax,
            ["throughput"] = "one full-corpus pass",
            ["memory"] = "GC.GetTotalAllocatedBytes delta over the full pass + working-set snapshot",
            ["minotaurValidity"] = "Success and last resolved token ends at end of document",
        },
        ["cold"] = new JsonArray(coldRows.Select(r => (JsonNode)r!.DeepClone()).ToArray()),
        ["cells"] = new JsonArray(cells.Select(c => (JsonNode)c!.DeepClone()).ToArray()),
    };
}

static JsonArray BuildCorpusInfo()
{
    var corpusInfo = new JsonArray();
    foreach (var dsl in Enum.GetValues<CaseDsl>())
    {
        foreach (var size in Enum.GetValues<CorpusSize>())
        {
            var text = Corpus.Render(dsl, size);
            corpusInfo.Add(new JsonObject
            {
                ["dsl"] = DslName(dsl),
                ["size"] = SizeName(size),
                ["docs"] = Corpus.DocumentCount(dsl, size),
                ["lines"] = Corpus.CountLines(text),
                ["bytes"] = Encoding.UTF8.GetByteCount(text),
                ["sha256"] = Corpus.Sha256(text),
            });
        }
    }

    return corpusInfo;
}

static List<JsonObject> ReadNdjson(string path)
{
    var rows = new List<JsonObject>();
    if (!File.Exists(path))
    {
        return rows;
    }

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("{"))
        {
            rows.Add(JsonNode.Parse(trimmed)!.AsObject());
        }
    }

    return rows;
}

static IRuntimeEngine CreateEngine(string engineName, CaseDsl dsl) => engineName switch
{
    "minotaur" => new MinotaurEngine(GrammarText(dsl)),
    "hand-written" => new HandWrittenEngine(dsl),
    "antlr" => new AntlrEngine(dsl),
    _ => throw new ArgumentOutOfRangeException(nameof(engineName)),
};

static string BuildResultsMarkdown(JsonObject report)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Issue #127 — Generated-parser runtime vs hand-written and ANTLR parsers");
    sb.AppendLine();
    sb.AppendLine("Measured by `Minotaur.Benchmarks.ParserRuntime`: the engine's shipped");
    sb.AppendLine("`RealTimeParserSession` (DevelApp.StepParser, the C#-process target Minotaur");
    sb.AppendLine("ships, a fresh session per document: the session is a per-buffer object");
    sb.AppendLine("with no reset API, and re-initializing one session across documents");
    sb.AppendLine("accumulates retained memory without bound, so parsing N independent");
    sb.AppendLine("documents requires N sessions and the per-document cost includes");
    sb.AppendLine("session construction) against hand-written single-pass parsers and ANTLR-generated C#");
    sb.AppendLine("parsers (Antlr4.Runtime.Standard 4.13.1, fresh lexer/stream/parser per");
    sb.AppendLine("document). All engines parse identical deterministic corpora of bounded");
    sb.AppendLine("documents (seed 127; 1K / 10K / 100K lines per size class). Latency p50/p95");
    sb.AppendLine("are per-document parse times over 10 reps x up to 500 sampled documents;");
    sb.AppendLine("throughput is one full-corpus pass. A document counts as parsed for the");
    sb.AppendLine("Minotaur engine only when parsing succeeds AND the last resolved token ends");
    sb.AppendLine("at the end of the document (full consumption; `Success=True` alone is not");
    sb.AppendLine("accepted). Allocations are the managed bytes allocated per document over");
    sb.AppendLine("the full pass.");
    sb.AppendLine();
    sb.AppendLine("Machine: " + report["machine"]!["os"]!.GetValue<string>());
    sb.AppendLine();
    sb.AppendLine("- Runtime: " + report["machine"]!["runtime"]!.GetValue<string>()
        + " (" + report["machine"]!["architecture"]!.GetValue<string>()
        + ", " + report["machine"]!["processorCount"]!.GetValue<int>() + " CPUs)");
    sb.AppendLine("- Minotaur commit: `" + report["minotaurCommit"]!.GetValue<string>() + "`");
    sb.AppendLine("- Corpora: seed " + report["corpus"]!["seed"]!.GetValue<int>()
        + "; small + medium committed under `docs/corpus/issue-127`, large generated");
    sb.AppendLine("  at runtime (SHA-256 in `results.json`)");
    sb.AppendLine();

    foreach (var dslName in new[] { "config", "query", "template" })
    {
        sb.AppendLine($"## {dslName} DSL");
        sb.AppendLine();
        sb.AppendLine("| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |");
        sb.AppendLine("|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|");

        foreach (var engineName in new[] { "minotaur", "hand-written", "antlr" })
        {
            var row = new List<string>();
            foreach (var sizeName in new[] { "small", "medium", "large" })
            {
                var cell = FindCell(report, dslName, sizeName, engineName);
                row.Add(cell is null
                    ? "n/a"
                    : $"{cell["latencyP50Us"]!.GetValue<double>():F1} / {cell["latencyP95Us"]!.GetValue<double>():F1}");
            }

            var throughput = new List<string>();
            var allocations = new List<string>();
            var rate = 1.0;
            foreach (var sizeName in new[] { "small", "medium", "large" })
            {
                var cell = FindCell(report, dslName, sizeName, engineName);
                if (cell is null)
                {
                    throughput.Add("n/a");
                    allocations.Add("n/a");
                    continue;
                }

                throughput.Add($"{cell["throughputMbPerS"]!.GetValue<double>():F1}");
                allocations.Add($"{cell["allocatedBytesPerDoc"]!.GetValue<double>():F0}");
                rate = Math.Min(rate, cell["fullParseRate"]!.GetValue<double>());
            }

            sb.AppendLine($"| {engineName} | {row[0]} | {row[1]} | {row[2]} | "
                + $"{string.Join(" / ", throughput)} | {string.Join(" / ", allocations)} | {rate:P1} |");
        }

        sb.AppendLine();
    }

    sb.AppendLine("## Cold start (fresh process, includes JIT)");
    sb.AppendLine();
    sb.AppendLine("| DSL | Minotaur construction | Session footprint | Minotaur first parse | Hand-written first parse | ANTLR first parse |");
    sb.AppendLine("|-----|-----------------------|-------------------|----------------------|--------------------------|------------------|");

    foreach (var row in report["cold"]!.AsArray().Cast<JsonObject>())
    {
        sb.AppendLine($"| {row["dsl"]!.GetValue<string>()} "
            + $"| {row["minotaurConstructionMs"]!.GetValue<double>():F1} ms "
            + $"| {row["minotaurSessionRetainedKb"]!.GetValue<double>():F1} KB "
            + $"| {row["minotaurFirstParseMs"]!.GetValue<double>():F1} ms "
            + $"| {row["handWrittenFirstParseMs"]!.GetValue<double>():F2} ms "
            + $"| {row["antlrFirstParseMs"]!.GetValue<double>():F1} ms |");
    }

    sb.AppendLine();
    sb.AppendLine("## Findings");
    sb.AppendLine();
    sb.AppendLine("The measurements above replace the article's projected runtime table");
    sb.AppendLine("(config 2.3 / 4.7 / 2.8 ms, query 15.4 / 28.1 / 16.9 ms, template 8.7 /");
    sb.AppendLine("19.3 / 9.2 ms for hand-written / ANTLR / Minotaur respectively); the");
    sb.AppendLine("projected values must not be published. Two caveats keep the comparison");
    sb.AppendLine("honest:");
    sb.AppendLine();
    sb.AppendLine("1. **Per-buffer session model.** The engine's session is a per-buffer");
    sb.AppendLine("   object (live editing applies incremental edits in place); it has no");
    sb.AppendLine("   reset or disposal API, and re-initializing one session across");
    sb.AppendLine("   documents accumulates retained memory without bound. Every Minotaur");
    sb.AppendLine("   measurement therefore constructs a fresh session per document and");
    sb.AppendLine("   includes session construction in the per-document cost; this is the");
    sb.AppendLine("   engine's supported model, not a benchmark artifact.");
    sb.AppendLine("1. **Bounded-document operating mode.** The shipped engine explores a");
    sb.AppendLine("   bounded GLR path budget per document, so the benchmark feeds every");
    sb.AppendLine("   engine a stream of bounded, fixed-shape documents (the live-editing");
    sb.AppendLine("   usage the engine is shipped for: one document per editor buffer). The");
    sb.AppendLine("   baselines accept unbounded documents; the generated Minotaur grammars");
    sb.AppendLine("   deliberately encode the bounded document skeleton (see the grammar");
    sb.AppendLine("   discipline documented in `CaseStudyDsls.cs`). Throughput over a large");
    sb.AppendLine("   corpus is therefore per-document parse cost multiplied out, which is");
    sb.AppendLine("   exactly the number a live editor experiences per keystroke reparse.");
    sb.AppendLine("3. **Per-document timestamping.** Latency samples bracket each document");
    sb.AppendLine("   with two `Stopwatch` timestamps (~tens of nanoseconds); for the");
    sb.AppendLine("   microsecond hand-written baselines this adds a small fixed overhead");
    sb.AppendLine("   to every sample, identically for all engines.");
    sb.AppendLine();
    sb.AppendLine("Full per-cell detail (p50/p95/mean latency, throughput, allocations,");
    sb.AppendLine("working set, elapsed wall time, full-parse rate, cold-start rows, corpus");
    sb.AppendLine("SHA-256 hashes) is in `results.json` next to this file.");
    return sb.ToString();
}

static JsonObject? FindCell(JsonObject report, string dsl, string size, string engine)
{
    foreach (var cell in report["cells"]!.AsArray().Cast<JsonObject>())
    {
        if (cell["dsl"]!.GetValue<string>() == dsl
            && cell["size"]!.GetValue<string>() == size
            && cell["engine"]!.GetValue<string>() == engine)
        {
            return cell;
        }
    }

    return null;
}

// ---------------------------------------------------------------------------
// Engine adapters.
// ---------------------------------------------------------------------------

namespace Minotaur.Benchmarks.ParserRuntime
{
    using Antlr4.Runtime;

    internal interface IRuntimeEngine
    {
        string Name { get; }

        bool ParseDocument(string document);
    }

    /// <summary>
    /// The shipped engine: a fresh RealTimeParserSession per document. The
    /// session is a per-buffer object (live editing applies incremental edits
    /// through ApplyEdit); it offers no reset or disposal API, and
    /// re-initializing one session across documents accumulates retained
    /// memory without bound (verified empirically during this issue:
    /// megabytes per Initialize/Parse cycle survive a full collection, even
    /// when re-initializing the same document). A fresh session per document
    /// is therefore the only supported way to parse N independent documents,
    /// and the per-document cost includes session construction. A document
    /// counts as parsed only on success with full consumption.
    /// </summary>
    internal sealed class MinotaurEngine : IRuntimeEngine
    {
        private readonly string grammar;

        public MinotaurEngine(string grammar)
        {
            this.grammar = grammar;
        }

        public string Name => "minotaur";

        public bool ParseDocument(string document)
        {
            try
            {
                var session = new RealTimeParserSession(this.grammar);
                session.Initialize(document);
                var result = session.Parse();
                var last = result.Success ? result.ResolvedTokens?.LastOrDefault() : null;
                return last is not null
                    && last.StartPosition + last.Length == document.TrimEnd().Length;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Hand-written single-pass baselines for the three case-study DSLs.
    /// </summary>
    internal sealed class HandWrittenEngine : IRuntimeEngine
    {
        private readonly CaseDsl dsl;
        private readonly HandWrittenConfigParser config = new();
        private readonly HandWrittenQueryParser query = new();
        private readonly HandWrittenTemplateParser template = new();

        public HandWrittenEngine(CaseDsl dsl)
        {
            this.dsl = dsl;
        }

        public string Name => "hand-written";

        public bool ParseDocument(string document) => this.dsl switch
        {
            CaseDsl.Config => this.config.Parse(document).Ok,
            CaseDsl.Query => this.query.Parse(document),
            CaseDsl.Template => this.template.Parse(document),
            _ => false,
        };
    }

    /// <summary>
    /// ANTLR-generated C# baselines: fresh lexer, token stream and parser per
    /// document (the standard generated-parser usage). A document counts as
    /// parsed when neither the lexer nor the parser reported a syntax error;
    /// the entry rules end in EOF, so full consumption is enforced by the
    /// grammar itself.
    /// </summary>
    internal sealed class AntlrEngine : IRuntimeEngine
    {
        private readonly CaseDsl dsl;

        public AntlrEngine(CaseDsl dsl)
        {
            this.dsl = dsl;
        }

        public string Name => "antlr";

        public bool ParseDocument(string document)
        {
            var input = new AntlrInputStream(document);
            var errors = 0;
            switch (this.dsl)
            {
                case CaseDsl.Config:
                    {
                        var lexer = new ConfigDslLexer(input);
                        lexer.RemoveErrorListeners();
                        var lexerErrors = new CountingErrorListener();
                        lexer.AddErrorListener(lexerErrors);
                        var parser = new ConfigDslParser(new CommonTokenStream(lexer));
                        parser.RemoveErrorListeners();
                        parser.config_file();
                        errors = lexerErrors.Count + parser.NumberOfSyntaxErrors;
                        break;
                    }

                case CaseDsl.Query:
                    {
                        var lexer = new QueryDslLexer(input);
                        lexer.RemoveErrorListeners();
                        var lexerErrors = new CountingErrorListener();
                        lexer.AddErrorListener(lexerErrors);
                        var parser = new QueryDslParser(new CommonTokenStream(lexer));
                        parser.RemoveErrorListeners();
                        parser.query();
                        errors = lexerErrors.Count + parser.NumberOfSyntaxErrors;
                        break;
                    }

                case CaseDsl.Template:
                    {
                        var lexer = new TemplateDslLexer(input);
                        lexer.RemoveErrorListeners();
                        var lexerErrors = new CountingErrorListener();
                        lexer.AddErrorListener(lexerErrors);
                        var parser = new TemplateDslParser(new CommonTokenStream(lexer));
                        parser.RemoveErrorListeners();
                        parser.template();
                        errors = lexerErrors.Count + parser.NumberOfSyntaxErrors;
                        break;
                    }

                default:
                    return false;
            }

            return errors == 0;
        }
    }

    /// <summary>
    /// Counts lexer syntax errors (the generated C# lexers do not expose
    /// NumberOfSyntaxErrors the way the parsers do).
    /// </summary>
    internal sealed class CountingErrorListener : IAntlrErrorListener<int>
    {
        public int Count { get; private set; }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException recognitionException)
        {
            this.Count++;
        }
    }
}
