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
using Minotaur.Parser;
using Minotaur.Projects;
using Minotaur.Projects.Grammar;

// ---------------------------------------------------------------------------
// Minotaur issue #134 harness: project-load time and memory at increasing
// project scales (100 / 1K / 10K files; 50K with PROJECTLOAD_INCLUDE_50K=1).
//
// For each scale a synthetic mixed-language project tree is generated
// (deterministic, seed-free: content is a pure function of the file index),
// then measured:
//
//   discover   Directory.GetFiles over the tree (same call the loader makes)
//   classify   per-file GrammarDetectionManager.DetectGrammarAsync, replayed
//              by the harness (the loader does not expose phase timings, so
//              the classify phase is measured by repeating the same calls)
//   total      ProjectLoader.LoadFolderAsync end to end (discover + classify
//              + per-file analysis + structure assembly)
//   memory     managed-heap delta, GC allocated bytes, process working set
//   outcomes   file count, per-FileType mix, unclassified (no grammar) count
//
// Note: ProjectLoader's per-file symbol/dependency extraction is currently
// stubbed (ExtractSymbolsFromGraph returns empty), so "parse" contributes no
// per-file work in the shipped loader; the total curve therefore measures
// discover + classify + assemble. That is itself a #134 finding.
//
// Usage:
//   dotnet run --project src/Minotaur.Benchmarks.ProjectLoad [-- <outputDir>]
// Default output: docs/measurements/issue-134 (results.json + RESULTS.md +
// load-curve.csv). Env: PROJECTLOAD_SCALES="100,1000,10000",
// PROJECTLOAD_INCLUDE_50K=1, MINOTAUR_GRAMMARS_DIR (local grammars checkout
// for detection), MINOTAUR_COMMIT (recorded in results).
//
// Timings are recorded, never asserted.
// ---------------------------------------------------------------------------

var outputDir = args.Length > 0
    ? args[0]
    : Path.Combine("docs", "measurements", "issue-134");
Directory.CreateDirectory(outputDir);

var scales = (Environment.GetEnvironmentVariable("PROJECTLOAD_SCALES") ?? "100,1000,10000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(int.Parse)
    .ToArray();
if (Environment.GetEnvironmentVariable("PROJECTLOAD_INCLUDE_50K") == "1"
    && !scales.Contains(50_000))
{
    scales = scales.Append(50_000).ToArray();
}

// Deterministic mixed-language file templates. (.log is skipped on purpose:
// ProjectLoader.IsIgnoredPath filters *.log.)
var mix = new (string Ext, Func<int, string> Content)[]
{
    (".cs", i => $"namespace P{i % 50} {{ public class C{i} {{ public int F() => {i}; }} }}\n"),
    (".json", i => $"{{\"id\": {i}, \"name\": \"item{i}\", \"tags\": [\"a\", \"b\"]}}\n"),
    (".xml", i => $"<item id=\"{i}\"><name>item{i}</name></item>\n"),
    (".py", i => $"def f{i}():\n    return {i}\n"),
    (".fix", i => $"8=FIX.4.4\u00019=12\u000135=A\u000149=S{i}\u000110=000\u0001"),
    (".txt", i => $"note {i}\n"),
};

Console.WriteLine("Minotaur issue #134 project-load harness");
Console.WriteLine($"  runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
Console.WriteLine($"  os:      {RuntimeInformation.OSDescription}");
Console.WriteLine($"  cpus:    {Environment.ProcessorCount}");
Console.WriteLine($"  scales:  {string.Join(", ", scales)} files");
Console.WriteLine();

var loader = new ProjectLoader(new StepParserIntegration());
var detector = new GrammarDetectionManager();
var rows = new List<JsonObject>();

foreach (var scale in scales)
{
    var reps = scale <= 1_000 ? 3 : 1;
    var root = Path.Combine(Path.GetTempPath(), $"minotaur-projectload-{scale}-{Guid.NewGuid():N}");
    CreateProjectTree(root, scale, mix);
    Console.WriteLine($"measuring {scale} files ({reps} rep(s)) ...");

    // Discover phase (same call the loader makes).
    var sw = Stopwatch.StartNew();
    var discovered = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
    sw.Stop();
    var discoverMs = sw.Elapsed.TotalMilliseconds;

    // Classify phase: replay the same per-file detection calls.
    sw.Restart();
    var classified = 0;
    foreach (var file in discovered)
    {
        var result = await detector.DetectGrammarAsync(file, root, ProjectType.GenericFolder);
        if (result.IsSuccessful)
        {
            classified++;
        }
    }
    sw.Stop();
    var classifyMs = sw.Elapsed.TotalMilliseconds;

    // Total load, reps times over the same tree.
    var totalSamples = new List<double>();
    var allocSamples = new List<long>();
    long heapDeltaBytes = 0;
    long wsDeltaBytes = 0;
    ProjectStructure? structure = null;
    for (var rep = 0; rep < reps; rep++)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var heapBefore = GC.GetTotalMemory(forceFullCollection: true);
        var wsBefore = Process.GetCurrentProcess().WorkingSet64;
        var allocBefore = GC.GetAllocatedBytesForCurrentThread();

        sw.Restart();
        structure = await loader.LoadFolderAsync(root);
        sw.Stop();
        totalSamples.Add(sw.Elapsed.TotalMilliseconds);

        allocSamples.Add(GC.GetAllocatedBytesForCurrentThread() - allocBefore);
        heapDeltaBytes = GC.GetTotalMemory(forceFullCollection: true) - heapBefore;
        wsDeltaBytes = Process.GetCurrentProcess().WorkingSet64 - wsBefore;
    }

    Array.Sort(totalSamples.ToArray());
    var totalMedianMs = Median(totalSamples);

    var fileTypeMix = structure!.Files
        .GroupBy(f => f.FileType.ToString())
        .ToDictionary(g => g.Key, g => g.Count());
    var unclassified = structure.Files.Count(f => f.EmbeddedGrammar == null);

    var row = new JsonObject
    {
        ["scaleFiles"] = scale,
        ["discoveredFiles"] = discovered.Length,
        ["loadedFiles"] = structure.Files.Count,
        ["reps"] = reps,
        ["discoverMs"] = Math.Round(discoverMs, 2),
        ["classifyMs"] = Math.Round(classifyMs, 2),
        ["totalMedianMs"] = Math.Round(totalMedianMs, 2),
        ["totalAllMs"] = new JsonArray(totalSamples.Select(t => (JsonNode)Math.Round(t, 2)).ToArray()),
        ["classified"] = classified,
        ["unclassified"] = unclassified,
        ["fileTypeMix"] = new JsonObject(fileTypeMix.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value))),
        ["symbolsExtracted"] = structure.Files.Sum(f => f.Symbols.Count),
        ["heapDeltaBytes"] = heapDeltaBytes,
        ["allocBytes"] = allocSamples.Max(),
        ["workingSetDeltaBytes"] = wsDeltaBytes,
    };
    rows.Add(row);

    Console.WriteLine($"  discover {discoverMs:F0} ms, classify {classifyMs:F0} ms, " +
        $"total median {totalMedianMs:F0} ms, unclassified {unclassified}/{structure.Files.Count}, " +
        $"heap delta {heapDeltaBytes / 1024.0 / 1024.0:F1} MB, ws delta {wsDeltaBytes / 1024.0 / 1024.0:F1} MB");

    Directory.Delete(root, recursive: true);
}

var report = new JsonObject
{
    ["issue"] = "DevelApp-ai/Minotaur#134",
    ["harness"] = "Minotaur.Benchmarks.ProjectLoad",
    ["minotaurCommit"] = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "unrecorded",
    ["grammarsDir"] = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR") ?? "unset",
    ["machine"] = new JsonObject
    {
        ["os"] = RuntimeInformation.OSDescription,
        ["runtime"] = RuntimeInformation.FrameworkDescription,
        ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
        ["processorCount"] = Environment.ProcessorCount,
    },
    ["method"] = new JsonObject
    {
        ["discover"] = "Directory.GetFiles recursive (same call ProjectLoader makes)",
        ["classify"] = "per-file GrammarDetectionManager.DetectGrammarAsync replayed by the harness (loader exposes no phase timings)",
        ["total"] = "ProjectLoader.LoadFolderAsync end to end; per-file symbol extraction is stubbed in the shipped loader, so total = discover + classify + assemble",
        ["memory"] = "GC.GetTotalMemory delta after full collection, GC.GetAllocatedBytesForCurrentThread delta, process WorkingSet64 delta",
        ["reps"] = "3 for <=1K files, 1 above; median reported",
    },
    ["rows"] = new JsonArray(rows.Select(r => (JsonNode)r.DeepClone()).ToArray()),
};

File.WriteAllText(Path.Combine(outputDir, "results.json"), report.ToString());
File.WriteAllText(Path.Combine(outputDir, "load-curve.csv"), BuildCsv(rows));
File.WriteAllText(Path.Combine(outputDir, "RESULTS.md"), BuildMarkdown(report));
Console.WriteLine();
Console.WriteLine($"wrote results.json, load-curve.csv, RESULTS.md into {outputDir}");
return 0;

// ---------------------------------------------------------------------------

static void CreateProjectTree(string root, int fileCount, (string Ext, Func<int, string> Content)[] mix)
{
    Directory.CreateDirectory(root);
    var dirCount = Math.Max(1, fileCount / 100);
    for (var d = 0; d < dirCount; d++)
    {
        Directory.CreateDirectory(Path.Combine(root, $"pkg{d}"));
    }
    for (var i = 0; i < fileCount; i++)
    {
        var (ext, content) = mix[i % mix.Length];
        File.WriteAllText(Path.Combine(root, $"pkg{i % dirCount}", $"file{i}{ext}"), content(i));
    }
}

static double Median(List<double> values)
{
    var sorted = values.OrderBy(v => v).ToArray();
    var mid = sorted.Length / 2;
    return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
}

static string BuildCsv(List<JsonObject> rows)
{
    var sb = new StringBuilder();
    sb.AppendLine("scale_files,discovered_files,loaded_files,discover_ms,classify_ms,total_median_ms,classified,unclassified,heap_delta_mb,working_set_delta_mb,alloc_mb");
    foreach (var r in rows)
    {
        sb.AppendLine(string.Join(",",
            r["scaleFiles"]!.GetValue<int>(),
            r["discoveredFiles"]!.GetValue<int>(),
            r["loadedFiles"]!.GetValue<int>(),
            r["discoverMs"]!.GetValue<double>().ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            r["classifyMs"]!.GetValue<double>().ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            r["totalMedianMs"]!.GetValue<double>().ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            r["classified"]!.GetValue<int>(),
            r["unclassified"]!.GetValue<int>(),
            (r["heapDeltaBytes"]!.GetValue<long>() / 1024.0 / 1024.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            (r["workingSetDeltaBytes"]!.GetValue<long>() / 1024.0 / 1024.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            (r["allocBytes"]!.GetValue<long>() / 1024.0 / 1024.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
    }
    return sb.ToString();
}

static string BuildMarkdown(JsonObject report)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Issue #134 — Project-load time and memory by project scale");
    sb.AppendLine();
    sb.AppendLine("Measured by `Minotaur.Benchmarks.ProjectLoad`: synthetic mixed-language");
    sb.AppendLine("project trees (deterministic content per file index; mix recorded per row)");
    sb.AppendLine("loaded through `ProjectLoader.LoadFolderAsync`.");
    sb.AppendLine();
    var machine = report["machine"]!.AsObject();
    sb.AppendLine("Machine: " + machine["os"]!.GetValue<string>());
    sb.AppendLine("");
    sb.AppendLine("- Runtime: " + machine["runtime"]!.GetValue<string>()
        + " (" + machine["architecture"]!.GetValue<string>()
        + ", " + machine["processorCount"]!.GetValue<int>() + " CPUs)");
    sb.AppendLine("- Minotaur commit: `" + report["minotaurCommit"]!.GetValue<string>() + "`");
    sb.AppendLine("- Grammars dir: `" + report["grammarsDir"]!.GetValue<string>() + "`");
    sb.AppendLine();
    sb.AppendLine("| Files | Discover (ms) | Classify (ms) | Total load median (ms) | Unclassified | Heap delta (MB) | WS delta (MB) |");
    sb.AppendLine("|------:|--------------:|--------------:|-----------------------:|-------------:|----------------:|--------------:|");
    foreach (var row in report["rows"]!.AsArray().Cast<JsonObject>())
    {
        sb.AppendLine($"| {row["scaleFiles"]!.GetValue<int>()} "
            + $"| {row["discoverMs"]!.GetValue<double>():F0} "
            + $"| {row["classifyMs"]!.GetValue<double>():F0} "
            + $"| {row["totalMedianMs"]!.GetValue<double>():F0} "
            + $"| {row["unclassified"]!.GetValue<int>()}/{row["loadedFiles"]!.GetValue<int>()} "
            + $"| {row["heapDeltaBytes"]!.GetValue<long>() / 1024.0 / 1024.0:F1} "
            + $"| {row["workingSetDeltaBytes"]!.GetValue<long>() / 1024.0 / 1024.0:F1} |");
    }
    sb.AppendLine();
    sb.AppendLine("Notes:");
    sb.AppendLine("- `discover` is `Directory.GetFiles` recursive (the call the loader makes);");
    sb.AppendLine("  `classify` replays the loader's per-file `DetectGrammarAsync` calls (the");
    sb.AppendLine("  loader exposes no phase timings); `total` is end-to-end `LoadFolderAsync`.");
    sb.AppendLine("- Per-file symbol/dependency extraction in the shipped `ProjectLoader` is");
    sb.AppendLine("  stubbed (`ExtractSymbolsFromGraph` returns empty), so the per-file parse");
    sb.AppendLine("  phase currently contributes no work — the total curve measures");
    sb.AppendLine("  discover + classify + assemble. CognitiveGraph V1/V2 threshold validation");
    sb.AppendLine("  is tracked with CognitiveGraph (see #134 item 3) and is not measured here.");
    sb.AppendLine();
    sb.AppendLine("Full detail (file-type mix, all rep samples, allocation) is in `results.json`;");
    sb.AppendLine("the curve is in `load-curve.csv`.");
    return sb.ToString();
}
