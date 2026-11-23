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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Minotaur.Cli;
using Minotaur.Labyrinth;

// ---------------------------------------------------------------------------
// Minotaur-Labyrinth L3 harness (DevelApp-ai/Minotaur#246, sub-issue #252):
// "detection corpus at article scale + ground truth + analysis-time
// measurement artifacts" for the graph-symbolic-execution article lane
// (research-article-builder backlog; per Lars's 2026-11-21 decision the
// article experiments run on Minotaur-Labyrinth).
//
// Evaluates detection quality and analysis time of the committed C# 10 rule
// pack (rules/csharp10, the L2 rule pack) over a deterministic, generated
// article-scale corpus: for each of the five rule ids, program shapes at
// declared statement counts 10/25/50/100 in two variants - injected defects
// (exact expected findings at generator-recorded lines) and clean
// near-misses (the guarded/sanitized counterparts). Ground truth is exact
// (generated corpus, no adjudication): TP/FP/FN per rule -> precision and
// recall.
//
// Analysis-time numbers follow the CognitiveGraph #56 protocol: N (default
// 3) repetitions per file, median reported, all repetitions recorded in
// timing.csv. Every scan covers the whole engine code path used by
// 'minotaur labyrinth verify-corpus' (ScanHost.BuildGraph + ScanHost.RunRules:
// grammar load + token tiling + graph build + rule compile + taint
// analysis; no cross-file reuse) inside a child worker process with a
// per-file kill budget, mirroring the issue-133 EmbeddedDetection harness
// isolation (the engine cannot be safely multiplexed inside one process
// for adversarial inputs).
//
// Outcomes: success | timeout | parse-crash | startup-fail | engine-oom |
//           engine-error | read-fail
//
// Outputs (under arg0, default docs/measurements/issue-246):
//   results.json       machine-readable full results + environment record
//   RESULTS.md         human-readable report
//   detection.csv      per-rule TP/FP/FN + precision/recall + median scan
//   timing.csv         per-file rows (outcome + wall-clock per repetition)
//   ground-truth.json  ground-truth manifest (expected findings per
//                      program + SHA-256 corpus fingerprint)
//   corpus/*.cs        the generated programs (regenerated every run;
//                      byte-stable: template + index driven, no RNG)
//
// Environment:
//   MINOTAUR_GRAMMARS_DIR         required: path to a Minotaur-Grammars checkout
//   MINOTAUR_COMMIT               recorded in results
//   MINOTAUR_DETECTION_REPS       repetitions per file (default 3, #56 protocol)
//   DETECTION_PER_FILE_TIMEOUT_MS per-file scan budget (default 120000)
//   MINOTAUR_RULES_ROOT           rules root (default "rules"; must contain
//                                 csharp10/)
// ---------------------------------------------------------------------------

// ---- Worker mode must be dispatched BEFORE any main-flow setup. Workers are
// spawned with only --worker + grammar path + rules dir + reps; they never
// see env vars and never touch output dirs.
if (args.Length >= 4 && args[0] == "--worker")
{
    return await RunWorker(args[1], args[2], args[3]);
}

// ============================== configuration ================================

var outputDir = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "docs/measurements/issue-246";
var grammarsDir = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR");
if (string.IsNullOrWhiteSpace(grammarsDir) || !Directory.Exists(grammarsDir))
{
    Console.Error.WriteLine("MINOTAUR_GRAMMARS_DIR is required and must exist (a Minotaur-Grammars checkout).");
    return 2;
}
var rulesRoot = Environment.GetEnvironmentVariable("MINOTAUR_RULES_ROOT");
if (string.IsNullOrWhiteSpace(rulesRoot))
{
    rulesRoot = "rules";
}

var commit = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "(unrecorded)";
var reps = ParsePositiveInt(Environment.GetEnvironmentVariable("MINOTAUR_DETECTION_REPS"), 3);
if (reps < 0)
{
    return 2;
}

var perFileTimeoutMs = ParsePositiveInt(Environment.GetEnvironmentVariable("DETECTION_PER_FILE_TIMEOUT_MS"), 120000);
if (perFileTimeoutMs < 0)
{
    return 2;
}

Console.WriteLine("Minotaur.Benchmarks.SymbolicDetection");
Console.WriteLine("  issue:      DevelApp-ai/Minotaur#246 (L3) / #252 (graph-symbolic-execution lane)");
Console.WriteLine($"  outputDir:  {outputDir}");
Console.WriteLine($"  grammars:   {grammarsDir}");
Console.WriteLine($"  rules:      {Path.Combine(rulesRoot, "csharp10")}");
Console.WriteLine($"  reps:       {reps} (CognitiveGraph #56 protocol)");
Console.WriteLine($"  perFileTimeoutMs: {perFileTimeoutMs}");
Directory.CreateDirectory(outputDir);

// Broad catches are DELIBERATE in this harness: the outcome taxonomy (header
// comment) requires every failure mode to map to a taxonomy row, and the
// 'when (Note(ex, ...))' filter keeps each catch exactly as broad as before
// while making the swallowed exception visible on stderr (worker stderr is
// drained by the parent).
bool Note(Exception ex, string context)
{
    Console.Error.WriteLine($"  [{context}] {ex.GetType().Name}: {ex.Message}");
    return true;
}

int ParsePositiveInt(string? value, int fallback)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return fallback;
    }
    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
    {
        return parsed;
    }
    Console.Error.WriteLine($"Expected a positive integer, got: {value}");
    return -1;
}

// ================== grammar and rule pack resolution ========================

var target = ScanHost.ResolveTarget("csharp10");
var grammarPath = ScanHost.GrammarFilePath(grammarsDir, target);
if (!File.Exists(grammarPath))
{
    Console.Error.WriteLine($"Grammar file not found for target csharp10: {grammarPath}");
    return 2;
}

const string RuleNullDeref = "csharp-null-deref-unguarded-maybe-null";
const string RuleAsCast = "csharp-unsafe-as-cast-deref";
const string RuleArrayIndex = "csharp-array-index-unvalidated-input";
const string RuleDivide = "csharp-divide-by-constant-zero";
const string RuleModulo = "csharp-modulo-by-constant-zero";
var ruleIds = new[] { RuleNullDeref, RuleAsCast, RuleArrayIndex, RuleDivide, RuleModulo };

var csharpRulesDir = Path.Combine(rulesRoot, "csharp10");
if (!Directory.Exists(csharpRulesDir))
{
    Console.Error.WriteLine($"Rules directory not found: {csharpRulesDir} (set MINOTAUR_RULES_ROOT to the rules root).");
    return 2;
}

var pack = new LabyrinthRulePackLoader().TryLoadDirectory(csharpRulesDir);
if (!pack.IsValid)
{
    Console.Error.WriteLine($"Invalid rule pack: {string.Join("; ", pack.Errors)}");
    return 2;
}

var packIds = pack.RuleSet.Rules.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
foreach (var id in ruleIds)
{
    if (!packIds.Contains(id))
    {
        Console.Error.WriteLine($"rules/csharp10 pack is missing rule id '{id}' (expected: {string.Join(", ", ruleIds)}).");
        return 2;
    }
}

// =========================== corpus generation ==============================
// Deterministic by construction: fixed templates plus per-construct and
// per-filler indices, no RNG. Every construct statement is a single line, so
// the generator-recorded expected line numbers are exact.

var sizes = new[] { 10, 25, 50, 100 };
var variants = new[] { "defects", "clean" };
var corpusDir = Path.Combine(outputDir, "corpus");

var corpusFiles = new List<CorpusFile>();
foreach (var rule in ruleIds)
{
    foreach (var size in sizes)
    {
        foreach (var variant in variants)
        {
            corpusFiles.Add(GenerateFile(rule, size, variant));
        }
    }
}

Console.WriteLine($"  corpus: {corpusFiles.Count} programs under {corpusDir}");

// ======================= worker process management ===========================
// Same isolation discipline as the issue-133 EmbeddedDetection harness: every
// scan happens in a child worker process and is killed at budget.

Process? worker = null;
bool stderrPrinted = false;

void KillWorker()
{
    if (worker is null)
    {
        return;
    }
    try
    {
        if (!worker.HasExited)
        {
            worker.Kill(entireProcessTree: true);
            worker.WaitForExit(5000);
        }
        worker.Dispose();
    }
    catch (Exception ex) when (Note(ex, "worker kill race, process already gone"))
    {
        // already gone
    }
    worker = null;
}

string AssemblyLocation() => System.Reflection.Assembly.GetExecutingAssembly().Location;

void EnsureWorker()
{
    if (worker is not null && !worker.HasExited)
    {
        return;
    }
    KillWorker();

    var psi = new ProcessStartInfo
    {
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    // Apphost-vs-dotnet spawn: when this parent runs under the dotnet host
    // ('dotnet run' / 'dotnet <dll>'), ProcessPath is dotnet and the worker
    // needs the assembly dll as its first argument. When the parent runs
    // from an apphost executable (CI builds and runs the apphost directly),
    // ProcessPath IS the apphost and passing a dll argument would crash it
    // (the issue-133 harness hit exactly this at bring-up).
    if (Environment.ProcessPath is string pp &&
        (pp.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
         pp.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)))
    {
        psi.FileName = pp;
        psi.ArgumentList.Add(AssemblyLocation());
    }
    else if (Environment.ProcessPath is string apphost)
    {
        psi.FileName = apphost;
    }
    else
    {
        psi.FileName = "dotnet";
        psi.ArgumentList.Add(AssemblyLocation());
    }

    psi.ArgumentList.Add("--worker");
    psi.ArgumentList.Add(grammarPath);
    psi.ArgumentList.Add(csharpRulesDir);
    psi.ArgumentList.Add(reps.ToString(CultureInfo.InvariantCulture));

    worker = Process.Start(psi) ?? throw new InvalidOperationException("failed to start worker");

    // Drain stderr in the background; load notices appear once per worker.
    _ = Task.Run(async () =>
    {
        try
        {
            var spawned = worker;
            var text = await spawned.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(text) && !stderrPrinted)
            {
                stderrPrinted = true;
                Console.WriteLine("  [worker stderr]");
                foreach (var l in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    Console.WriteLine($"    {l.Trim()}");
                }
            }
        }
        catch (Exception ex) when (Note(ex, "worker stderr drain ended as worker died"))
        {
            // worker died; parent classifies via ack/result handling
        }
    });
}

async Task<ScanResult> ScanViaWorker(string absolutePath)
{
    EnsureWorker();
    var w = worker!;
    try
    {
        await w.StandardInput.WriteLineAsync(absolutePath);
        await w.StandardInput.FlushAsync();
    }
    catch (Exception ex) when (Note(ex, $"stdin write failed for {absolutePath}"))
    {
        KillWorker();
        return new ScanResult("parse-crash", [], []);
    }

    string? ack;
    try
    {
        using var ctsAck = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        ack = await w.StandardOutput.ReadLineAsync(ctsAck.Token);
    }
    catch (OperationCanceledException)
    {
        KillWorker();
        return new ScanResult("startup-fail", [], []);
    }
    if (ack is null || !ack.StartsWith("A\t", StringComparison.Ordinal))
    {
        KillWorker();
        return new ScanResult("startup-fail", [], []);
    }

    string? result;
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(perFileTimeoutMs + 1500));
        result = await w.StandardOutput.ReadLineAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        KillWorker();
        return new ScanResult("timeout", [], []);
    }
    if (result is null)
    {
        KillWorker();
        return new ScanResult("parse-crash", [], []);
    }

    try
    {
        var node = JsonNode.Parse(result) as JsonObject;
        var outcome = node?["outcome"]?.GetValue<string>() ?? "engine-error";
        if (outcome is not ("success" or "read-fail" or "engine-oom" or "engine-error"))
        {
            outcome = "engine-error";
        }

        var times = new List<double>();
        if (node?["timesMs"] is JsonArray timeArr)
        {
            foreach (var t in timeArr)
            {
                if (t is not null)
                {
                    times.Add(t.GetValue<double>());
                }
            }
        }

        var findings = new List<DetectedFinding>();
        if (node?["findings"] is JsonArray findingArr)
        {
            foreach (var f in findingArr)
            {
                var rule = f?["rule"]?.GetValue<string>();
                var line = f?["line"]?.GetValue<int>() ?? -1;
                if (rule is not null)
                {
                    findings.Add(new DetectedFinding(rule, line));
                }
            }
        }

        return new ScanResult(outcome, findings, times);
    }
    catch (Exception ex) when (Note(ex, $"result line unparseable for {absolutePath}"))
    {
        KillWorker();
        return new ScanResult("parse-crash", [], []);
    }
}

// ================================ worker =====================================

async Task<int> RunWorker(string grammarPath, string rulesDir, string repsArg)
{
    if (!int.TryParse(repsArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var workerReps) || workerReps < 1)
    {
        Console.Error.WriteLine($"worker: invalid repetition count: {repsArg}");
        return 2;
    }
    if (!File.Exists(grammarPath))
    {
        Console.Error.WriteLine($"worker: grammar file not found: {grammarPath}");
        return 2;
    }
    if (!Directory.Exists(rulesDir))
    {
        Console.Error.WriteLine($"worker: rules directory not found: {rulesDir}");
        return 2;
    }

    var grammarContent = await File.ReadAllTextAsync(grammarPath);
    // CorpusVerifier passes the resolved target directory as the grammar name
    // for the csharp10 corpus; that value is "programming-languages/csharp10".
    var grammarName = "programming-languages/csharp10";

    var workerPack = new LabyrinthRulePackLoader().TryLoadDirectory(rulesDir);
    if (!workerPack.IsValid)
    {
        Console.Error.WriteLine($"worker: invalid rule pack: {string.Join("; ", workerPack.Errors)}");
        return 2;
    }

    // Scan every file with the whole pack (like 'labyrinth scan'): findings on
    // unexpected rules or lines count as false positives in the parent.
    var ruleSet = workerPack.RuleSet;

    // One writer for the whole worker lifetime (creating it per request
    // disposes stdout and poisons every response after the first).
    await using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

    string? line;
    while ((line = await Console.In.ReadLineAsync()) is not null)
    {
        var path = line.Trim();
        if (path.Length == 0)
        {
            continue; // malformed request; the parent's ack/result wait classifies it
        }

        try
        {
            writer.WriteLine($"A\t{path}");
        }
        catch (Exception ex) when (Note(ex, "worker ack write failed, parent likely gone"))
        {
            return 0; // parent gone
        }

        var outcome = "success";
        var times = new List<double>();
        var findings = new List<DetectedFinding>();
        string source;
        try
        {
            source = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) when (Note(ex, $"worker read failed for {path}"))
        {
            try
            {
                writer.WriteLine(new JsonObject { ["path"] = path, ["outcome"] = "read-fail" }.ToJsonString());
            }
            catch (Exception ex2) when (Note(ex2, $"worker result write failed for {path}, parent likely gone"))
            {
                return 0;
            }
            continue;
        }

        for (var rep = 1; rep <= workerReps; rep++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var graph = ScanHost.BuildGraph(source, path, grammarContent, grammarName, out var nodes);
                var raw = ScanHost.RunRules(ruleSet, grammarContent, grammarName, graph, nodes);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
                if (rep == workerReps)
                {
                    // Findings are deterministic across repetitions; report the last.
                    foreach (var f in raw)
                    {
                        findings.Add(new DetectedFinding(
                            f.RuleId,
                            f.Sink is ILabyrinthFindingLocation loc ? loc.StartLine : -1));
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                sw.Stop();
                outcome = "engine-oom";
                break;
            }
            catch (Exception ex) when (Note(ex, $"worker scan crashed for {path}"))
            {
                sw.Stop();
                outcome = "engine-error";
                break;
            }
        }

        var response = new JsonObject
        {
            ["path"] = path,
            ["outcome"] = outcome,
            ["timesMs"] = new JsonArray(times.Select(t => (JsonNode?)JsonValue.Create(Math.Round(t, 3))).ToArray()),
            ["findings"] = new JsonArray(findings.Select(f => (JsonNode)new JsonObject
            {
                ["rule"] = f.Rule,
                ["line"] = f.Line,
            }).ToArray()),
        };
        try
        {
            writer.WriteLine(response.ToJsonString());
        }
        catch (Exception ex) when (Note(ex, $"worker result write failed for {path}, parent likely gone"))
        {
            return 0;
        }
    }

    return 0;
}

// ============================== scan loop ====================================

var reports = new List<FileReportData>();
foreach (var file in corpusFiles)
{
    Console.WriteLine($"  scanning {file.Name} ({ShortName(file.Rule)}, {file.Variant}, statements {file.Size}) ...");
    var scan = await ScanViaWorker(Path.Join(corpusDir, file.Name));
    reports.Add(new FileReportData(
        file.Name, file.Rule, file.Size, file.Variant, scan.Outcome, scan.TimesMs, scan.Findings, file.Expected));
}

KillWorker();
Console.WriteLine($"  scanned {reports.Count} programs");

// ============================ evaluation =====================================

static double Median(List<double> xs)
{
    if (xs.Count == 0)
    {
        return double.NaN;
    }
    var sorted = xs.OrderBy(v => v).ToList();
    return sorted.Count % 2 == 1
        ? sorted[sorted.Count / 2]
        : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2.0;
}

var perFile = new List<(FileReportData Report, CorpusFile Meta, int Tp, int Fp, int Fn, int Truth, int RawDetected, int UniqueDetected)>();
for (var i = 0; i < reports.Count; i++)
{
    var report = reports[i];
    var meta = corpusFiles[i];
    var expectedKeys = report.Expected.Select(e => (e.Rule, e.Line)).ToHashSet();
    var detectedKeys = report.Detected.Select(d => (d.Rule, d.Line)).ToHashSet();
    var tp = expectedKeys.Intersect(detectedKeys).Count();
    var fp = detectedKeys.Except(expectedKeys).Count();
    var fn = expectedKeys.Except(detectedKeys).Count();
    perFile.Add((report, meta, tp, fp, fn, expectedKeys.Count, report.Detected.Count, detectedKeys.Count));
}

// ruleStats: [files, truth, tp, fp, fn] over successfully scanned files only.
var ruleStats = ruleIds.ToDictionary(r => r, _ => new int[5]);
var overallStats = new int[5];
foreach (var (report, meta, tp, fp, fn, truth, _, _) in perFile)
{
    if (report.Outcome != "success")
    {
        continue;
    }
    var s = ruleStats[meta.Rule];
    s[0]++;
    s[1] += truth;
    s[2] += tp;
    s[3] += fp;
    s[4] += fn;
    overallStats[0]++;
    overallStats[1] += truth;
    overallStats[2] += tp;
    overallStats[3] += fp;
    overallStats[4] += fn;
}

// Per-file medians for one rule, or for every rule when rid is "*" (the
// overall row); negative size matches every size.
List<double> MediansOf(string rid, int size)
{
    var medians = new List<double>();
    foreach (var (report, meta, _, _, _, _, _, _) in perFile)
    {
        if (report.Outcome == "success" && (meta.Rule == rid || rid == "*") && (size < 0 || meta.Size == size))
        {
            medians.Add(Median(report.TimesMs));
        }
    }
    return medians;
}

string FormatPct(int part, int whole) =>
    whole == 0 ? "n/a" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture);

string FormatMsList(List<double> xs) =>
    xs.Count == 0 ? "n/a" : Median(xs).ToString("0.###", CultureInfo.InvariantCulture);

// ============================== outputs ======================================

// ---- detection.csv ----
var csv = new StringBuilder();
csv.AppendLine("rule,files,truth_findings,tp,fp,fn,precision_pct,recall_pct,median_scan_ms");
foreach (var rid in ruleIds)
{
    var s = ruleStats[rid];
    csv.AppendLine(string.Join(',',
        rid, s[0], s[1], s[2], s[3], s[4],
        FormatPct(s[2], s[2] + s[3]), FormatPct(s[2], s[2] + s[4]),
        FormatMsList(MediansOf(rid, -1))));
}
csv.AppendLine(string.Join(',',
    "overall", overallStats[0], overallStats[1], overallStats[2], overallStats[3], overallStats[4],
    FormatPct(overallStats[2], overallStats[2] + overallStats[3]),
    FormatPct(overallStats[2], overallStats[2] + overallStats[4]),
    FormatMsList(MediansOf("*", -1))));

// ---- timing.csv ----
var timingCsv = new StringBuilder();
timingCsv.AppendLine("file,rule,size,variant,outcome,median_ms,times_ms");
foreach (var (report, meta, _, _, _, _, _, _) in perFile)
{
    var medianMs = report.TimesMs.Count == 0
        ? "n/a"
        : Median(report.TimesMs).ToString("0.###", CultureInfo.InvariantCulture);
    var times = string.Join("|", report.TimesMs.Select(t => t.ToString("0.###", CultureInfo.InvariantCulture)));
    timingCsv.AppendLine(string.Join(',',
        report.Name, report.Rule, report.Size, report.Variant, report.Outcome, medianMs, times));
}

// ---- ground-truth.json (manifest + fingerprint) ----
var manifest = new JsonObject
{
    ["issue"] = "DevelApp-ai/Minotaur#246 (L3, sub-issue #252)",
    ["harness"] = "Minotaur.Benchmarks.SymbolicDetection",
    ["generator"] = new JsonObject
    {
        ["ruleIds"] = new JsonArray(ruleIds.Select(r => (JsonNode?)JsonValue.Create(r)!).ToArray()),
        ["sizes"] = new JsonArray(sizes.Select(s => (JsonNode?)JsonValue.Create(s)!).ToArray()),
        ["variants"] = new JsonArray(variants.Select(v => (JsonNode?)JsonValue.Create(v)!).ToArray()),
        ["constructFormula"] = "max(1, size / 10)",
        ["determinism"] = "no RNG: fixed templates plus per-construct and per-filler indices; corpus files and manifest are byte-stable across runs",
    },
    ["files"] = new JsonArray(corpusFiles.Select(f => (JsonNode)new JsonObject
    {
        ["name"] = f.Name,
        ["rule"] = f.Rule,
        ["size"] = f.Size,
        ["variant"] = f.Variant,
        ["constructs"] = f.Constructs,
        ["totalLines"] = f.TotalLines,
        ["expected"] = new JsonArray(f.Expected.Select(e => (JsonNode)new JsonObject
        {
            ["rule"] = e.Rule,
            ["line"] = e.Line,
        }).ToArray()),
    }).ToArray()),
};
var fingerprint = Convert.ToHexString(SHA256.HashData(
    Encoding.UTF8.GetBytes(manifest["files"]!.ToJsonString()))).ToLowerInvariant();
manifest["corpusFingerprint"] = fingerprint;

// ---- results.json ----
var perRuleDetection = new JsonObject();
foreach (var rid in ruleIds)
{
    var s = ruleStats[rid];
    var obj = new JsonObject
    {
        ["files"] = s[0],
        ["truthFindings"] = s[1],
        ["tp"] = s[2],
        ["fp"] = s[3],
        ["fn"] = s[4],
        ["medianScanMs"] = MediansOf(rid, -1).Count == 0
            ? null
            : Math.Round(Median(MediansOf(rid, -1)), 3),
    };
    if (s[2] + s[3] > 0)
    {
        obj["precision"] = Math.Round(100.0 * s[2] / (s[2] + s[3]), 1);
    }
    if (s[2] + s[4] > 0)
    {
        obj["recall"] = Math.Round(100.0 * s[2] / (s[2] + s[4]), 1);
    }
    perRuleDetection[rid] = obj;
}

var payload = new JsonObject
{
    ["issue"] = "DevelApp-ai/Minotaur#246 (L3, sub-issue #252)",
    ["harness"] = "Minotaur.Benchmarks.SymbolicDetection",
    ["note"] = "Rule-pack detection at article scale: deterministic generated C# 10 corpus (5 rules/csharp10 rule ids, declared statement counts 10/25/50/100, defects vs clean), scanned through the engine code path used by 'minotaur labyrinth verify-corpus', whole pack per file, worker-process isolation with per-file kill. Ground truth exact (generator-recorded lines); duplicate detections collapse to one unique (rule, line) pair (raw counts in this file). Timing follows the CognitiveGraph #56 protocol: reps per file, median reported.",
    ["minotaurCommit"] = commit,
    ["machine"] = new JsonObject
    {
        ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ["runtime"] = Environment.Version.ToString(),
        ["cpus"] = Environment.ProcessorCount,
        ["reps"] = reps,
        ["perFileTimeoutMs"] = perFileTimeoutMs,
    },
    ["grammar"] = new JsonObject
    {
        ["target"] = target.Directory,
        ["mainFile"] = target.MainFile,
        ["path"] = grammarPath,
    },
    ["rules"] = new JsonObject
    {
        ["root"] = csharpRulesDir,
        ["ids"] = new JsonArray(ruleIds.Select(r => (JsonNode?)JsonValue.Create(r)!).ToArray()),
    },
    ["corpusFingerprint"] = fingerprint,
    ["detection"] = perRuleDetection,
    ["files"] = new JsonArray(perFile.Select(row => (JsonNode)new JsonObject
    {
        ["name"] = row.Report.Name,
        ["rule"] = row.Report.Rule,
        ["size"] = row.Report.Size,
        ["variant"] = row.Report.Variant,
        ["outcome"] = row.Report.Outcome,
        ["expectedCount"] = row.Truth,
        ["rawDetectedCount"] = row.RawDetected,
        ["uniqueDetectedCount"] = row.UniqueDetected,
        ["tp"] = row.Tp,
        ["fp"] = row.Fp,
        ["fn"] = row.Fn,
        ["timesMs"] = new JsonArray(row.Report.TimesMs.Select(t => (JsonNode?)JsonValue.Create(Math.Round(t, 3))).ToArray()),
        ["medianScanMs"] = row.Report.TimesMs.Count == 0 ? null : Math.Round(Median(row.Report.TimesMs), 3),
    }).ToArray()),
};

// ================================ RESULTS.md =================================

var md = new StringBuilder();
md.AppendLine("# Labyrinth L3 (#246, sub-issue #252): rule-pack detection corpus at article scale");
md.AppendLine();
md.AppendLine($"Harness: Minotaur.Benchmarks.SymbolicDetection. Deterministic generated C# 10 corpus ({corpusFiles.Count} programs: 5 rules/csharp10 rule ids x declared statement counts 10/25/50/100 x defects/clean variants), scanned through the shipped rules/csharp10 pack via the engine code path used by 'minotaur labyrinth verify-corpus' (ScanHost.BuildGraph + ScanHost.RunRules), one worker-process scan per file with a per-file kill budget. Ground truth is exact (generator-recorded): injected defects expect findings at recorded lines; clean variants use the guarded/sanitized near-miss shapes (ThrowIfNull or coalesce guards, int.Parse validation, non-zero denominators).");
md.AppendLine();
md.AppendLine("## Protocol");
md.AppendLine();
md.AppendLine($"- Analysis time: {reps} repetitions per file, median reported (CognitiveGraph #56 protocol); every repetition covers the whole per-file engine path (grammar load, token tiling, graph build, rule compile, taint analysis) with no cross-file reuse, worker-process isolated with a per-file kill at {perFileTimeoutMs} ms.");
md.AppendLine("- Detection: every file is scanned with the whole five-rule pack; ground-truth comparison is per rule at (rule id, line) granularity; duplicate detections on the same (rule id, line) collapse to one (raw counts in results.json).");
md.AppendLine("- The corpus generator is deterministic (fixed templates plus indices, no RNG): re-running the harness regenerates byte-identical corpus files and manifest. Only wall-clock numbers vary run to run.");
md.AppendLine();
md.AppendLine("## Detection results (precision/recall per rule)");
md.AppendLine();
var collected = perFile.Count(p => p.Report.Outcome == "success");
md.AppendLine($"{collected} of {perFile.Count} programs scanned successfully" +
    (collected == perFile.Count
        ? "."
        : $" (non-success outcomes: {string.Join(", ", perFile.Where(p => p.Report.Outcome != "success").Select(p => $"{p.Report.Name}={p.Report.Outcome}"))})."));
md.AppendLine();
md.AppendLine("| rule | files | truth findings | TP | FP | FN | precision % | recall % |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var rid in ruleIds)
{
    var s = ruleStats[rid];
    md.AppendLine($"| {rid} | {s[0]} | {s[1]} | {s[2]} | {s[3]} | {s[4]} | {FormatPct(s[2], s[2] + s[3])} | {FormatPct(s[2], s[2] + s[4])} |");
}
md.AppendLine($"| overall | {overallStats[0]} | {overallStats[1]} | {overallStats[2]} | {overallStats[3]} | {overallStats[4]} | {FormatPct(overallStats[2], overallStats[2] + overallStats[3])} | {FormatPct(overallStats[2], overallStats[2] + overallStats[4])} |");
md.AppendLine();
md.AppendLine("## Analysis time (median scan wall-clock per file, ms, median of per-file medians)");
md.AppendLine();
md.AppendLine("| rule | statements 10 | statements 25 | statements 50 | statements 100 |");
md.AppendLine("| --- | --- | --- | --- | --- |");
foreach (var rid in ruleIds)
{
    md.AppendLine($"| {rid} | {FormatMsList(MediansOf(rid, 10))} | {FormatMsList(MediansOf(rid, 25))} | {FormatMsList(MediansOf(rid, 50))} | {FormatMsList(MediansOf(rid, 100))} |");
}
md.AppendLine($"| overall median across all {corpusFiles.Count} programs | {FormatMsList(MediansOf("*", -1))} |  |  |  |");
md.AppendLine();
md.AppendLine("## Environment record");
md.AppendLine();
md.AppendLine($"- commit: {commit}");
md.AppendLine($"- os: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
md.AppendLine($"- runtime: .NET {Environment.Version}; CPUs: {Environment.ProcessorCount}");
md.AppendLine($"- grammar: {target.Directory}/{target.MainFile} (resolved from MINOTAUR_GRAMMARS_DIR)");
md.AppendLine($"- rules: {csharpRulesDir} (5 committed rule ids)");
md.AppendLine($"- reps per file: {reps}; per-file timeout: {perFileTimeoutMs} ms");
md.AppendLine($"- corpus fingerprint (SHA-256 over the ground-truth manifest files array): {fingerprint}");
md.AppendLine();
md.AppendLine("## Reproducibility");
md.AppendLine();
md.AppendLine("Re-run: dotnet run --project src/Minotaur.Benchmarks.SymbolicDetection -- <output-dir> with MINOTAUR_GRAMMARS_DIR pointing at a Minotaur-Grammars checkout and MINOTAUR_COMMIT pinned. The corpus and ground truth (ground-truth.json) are regenerated byte-identically; artifacts land under the output dir.");
md.AppendLine();
md.AppendLine("## Limitations");
md.AppendLine();
md.AppendLine("- Single shared CI runner: medians are of per-file repetitions on one machine, wall-clock noise included (the #56 protocol accepts this).");
md.AppendLine("- The substrate materializes a document-order data-flow approximation (no interprocedural edges; intraprocedural control flow approximated; see ScanHost). Generated constructs deliberately place each source before its sink and keep each statement on one line, so expected lines are exact.");
md.AppendLine("- RSS memory and path-explosion-reduction numbers are follow-ups on the same harness (out of the #252 first pass); external baselines (Pex, static analyzers, KLEE) remain documented-as-external.");
md.AppendLine();
var resultsMd = md.ToString();

// ============================== write files ==================================

Directory.CreateDirectory(corpusDir);
await File.WriteAllTextAsync(Path.Join(outputDir, "detection.csv"), csv.ToString());
await File.WriteAllTextAsync(Path.Join(outputDir, "timing.csv"), timingCsv.ToString());
var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
await File.WriteAllTextAsync(Path.Join(outputDir, "results.json"), payload.ToJsonString(jsonOpts));
await File.WriteAllTextAsync(Path.Join(outputDir, "ground-truth.json"), manifest.ToJsonString(jsonOpts));
await File.WriteAllTextAsync(Path.Join(outputDir, "RESULTS.md"), resultsMd);

Console.WriteLine("  detection (per rule):");
foreach (var rid in ruleIds)
{
    var s = ruleStats[rid];
    Console.WriteLine($"    {rid}: tp={s[2]} fp={s[3]} fn={s[4]} precision={FormatPct(s[2], s[2] + s[3])}% recall={FormatPct(s[2], s[2] + s[4])}%");
}
Console.WriteLine($"  analysis time: overall median {FormatMsList(MediansOf("*", -1))} ms per file");
Console.WriteLine($"  wrote results.json, RESULTS.md, detection.csv, timing.csv, ground-truth.json, corpus/*.cs to {outputDir}");
return 0;

// ======================= local: corpus generation ===========================

string ShortName(string rule) => rule switch
{
    RuleNullDeref => "null-deref",
    RuleAsCast => "as-cast",
    RuleArrayIndex => "array-index",
    RuleDivide => "divide",
    RuleModulo => "modulo",
    _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "unknown rule id"),
};

string ClassNameOf(string part) => part switch
{
    "null-deref" => "NullDeref",
    "as-cast" => "AsCast",
    "array-index" => "ArrayIndex",
    "divide" => "Divide",
    "modulo" => "Modulo",
    "defects" => "Defects",
    "clean" => "Clean",
    _ => throw new ArgumentOutOfRangeException(nameof(part), part, "unknown part"),
};

CorpusFile GenerateFile(string rule, int size, string variant)
{
    var constructs = Math.Max(1, size / 10);
    var lines = new List<string>
    {
        "using System;",
        "using System.Collections.Generic;",
        "using System.Linq;",
        "",
        $"public sealed class Corpus{ClassNameOf(ShortName(rule))}{size}{ClassNameOf(variant)}",
        "{",
    };
    var statements = 0;
    var expected = new List<ExpectedFinding>();

    for (var j = 0; j < constructs; j++)
    {
        switch (rule)
        {
            case RuleNullDeref:
                lines.Add(variant == "defects"
                    ? $"    public string Pick{j}(List<string> pile{j})"
                    : $"    public string Guard{j}(List<string> pile{j})");
                lines.Add("    {");
                lines.Add($"        var pick{j} = pile{j}.FirstOrDefault();");
                if (variant == "defects")
                {
                    var sinkLine = lines.Count + 1;
                    lines.Add($"        return pick{j}.ToString();");
                    expected.Add(new ExpectedFinding(rule, sinkLine));
                    statements += 2;
                }
                else
                {
                    if (j % 2 == 0)
                    {
                        lines.Add($"        ArgumentNullException.ThrowIfNull(pick{j});");
                    }
                    else
                    {
                        lines.Add($"        pick{j} = pick{j} ?? \"none\";");
                    }
                    lines.Add($"        return pick{j}.ToString();");
                    statements += 3;
                }
                lines.Add("    }");
                break;

            case RuleAsCast:
                lines.Add(variant == "defects"
                    ? $"    public string Cast{j}(object box{j})"
                    : $"    public string CastSafe{j}(object box{j})");
                lines.Add("    {");
                lines.Add($"        var cast{j} = box{j} as string;");
                if (variant == "defects")
                {
                    var sinkLine = lines.Count + 1;
                    lines.Add($"        return cast{j}.ToString();");
                    expected.Add(new ExpectedFinding(rule, sinkLine));
                    statements += 2;
                }
                else
                {
                    lines.Add($"        cast{j} = cast{j} ?? \"empty\";");
                    lines.Add($"        return cast{j}.ToString();");
                    statements += 3;
                }
                lines.Add("    }");
                break;

            case RuleArrayIndex:
                lines.Add(variant == "defects"
                    ? $"    public char Read{j}(string[] data{j})"
                    : $"    public char ReadSafe{j}(string[] data{j})");
                lines.Add("    {");
                lines.Add($"        var input{j} = Console.ReadLine();");
                if (variant == "defects")
                {
                    var sinkLine = lines.Count + 1;
                    lines.Add($"        return data{j}[input{j}];");
                    expected.Add(new ExpectedFinding(rule, sinkLine));
                    statements += 2;
                }
                else
                {
                    lines.Add($"        var index{j} = int.Parse(input{j});");
                    lines.Add($"        return data{j}[index{j}];");
                    statements += 3;
                }
                lines.Add("    }");
                break;

            case RuleDivide:
                lines.Add(variant == "defects"
                    ? $"    public int Broken{j}(int total{j})"
                    : $"    public int Sound{j}(int total{j}, int count{j})");
                lines.Add("    {");
                if (variant == "defects")
                {
                    var sinkLine = lines.Count + 1;
                    lines.Add($"        return total{j} / 0;");
                    expected.Add(new ExpectedFinding(rule, sinkLine));
                    statements += 1;
                }
                else
                {
                    lines.Add($"        return count{j} == 0 ? 0 : total{j} / count{j};");
                    statements += 1;
                }
                lines.Add("    }");
                break;

            case RuleModulo:
                lines.Add(variant == "defects"
                    ? $"    public int Bucket{j}(int id{j})"
                    : $"    public int SoundBucket{j}(int id{j}, int size{j})");
                lines.Add("    {");
                if (variant == "defects")
                {
                    var sinkLine = lines.Count + 1;
                    lines.Add($"        return id{j} % 0;");
                    expected.Add(new ExpectedFinding(rule, sinkLine));
                    statements += 1;
                }
                else
                {
                    lines.Add($"        return id{j} % size{j};");
                    statements += 1;
                }
                lines.Add("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule, "unknown rule id");
        }
    }

    // Filler statements: arithmetic only (no sinks, no v2 operators, no
    // indexing), padded to the declared statement count.
    var filler = Math.Max(0, size - statements);
    if (filler > 0)
    {
        lines.Add("    public void Filler()");
        lines.Add("    {");
        for (var k = 0; k < filler; k++)
        {
            lines.Add(k % 2 == 0
                ? $"        var f{k} = {k} + 3;"
                : $"        var g{k} = f{k - 1} + {k};");
            statements++;
        }
        lines.Add("    }");
    }

    lines.Add("}");
    var text = string.Join("\n", lines) + "\n";

    var name = $"{ShortName(rule)}-{size}-{variant}.cs";
    Directory.CreateDirectory(corpusDir);
    File.WriteAllText(Path.Join(corpusDir, name), text);

    return new CorpusFile(name, rule, size, variant, constructs, lines.Count, expected);
}

// ============================== records ======================================

sealed record DetectedFinding(string Rule, int Line);

sealed record ScanResult(string Outcome, List<DetectedFinding> Findings, List<double> TimesMs);

sealed record ExpectedFinding(string Rule, int Line);

sealed record CorpusFile(
    string Name,
    string Rule,
    int Size,
    string Variant,
    int Constructs,
    int TotalLines,
    List<ExpectedFinding> Expected);

sealed record FileReportData(
    string Name,
    string Rule,
    int Size,
    string Variant,
    string Outcome,
    List<double> TimesMs,
    List<DetectedFinding> Detected,
    List<ExpectedFinding> Expected);
