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
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevelApp.StepParser;
using Minotaur.GrammarGeneration;

// ---------------------------------------------------------------------------
// Minotaur issue #126 harness.
//
// Measures grammar-compilation performance by grammar size: the wall-clock
// time and memory footprint of compiling a GrammarForge grammar into the
// engine's shipped RealTimeParserSession (DevelApp.StepParser), the
// C#-process target Minotaur actually ships. One real grammar per size
// class is fetched from the pinned Minotaur-Grammars corpus:
//
//   Small DSL   (15-25 rules)    KDL.grammar      (config DSL)
//   Medium DSL  (50-80 rules)    SQL.grammar      (query DSL)
//   Large DSL   (150-200 rules)  C17.grammar      (C17)
//   Complex DSL (300+ rules)     CSharp10.grammar (C# 10)
//
// Cold numbers are measured in a fresh child process (the first
// construction includes JIT of the compilation path); warm numbers are the
// median of 10 in-process re-compilations. Both are printed as one JSON
// line by the --measure-one child mode; the parent aggregates them into
// results.json + RESULTS.md.
//
// Usage:
//   dotnet run --project src/Minotaur.Benchmarks.GrammarCompilation [-- <outputDir>]
//   dotnet run --project src/Minotaur.Benchmarks.GrammarCompilation -- --measure-one <classIndex>
// Default output: docs/measurements/issue-126 (results.json + RESULTS.md)
//
// Grammar source: MINOTAUR_GRAMMARS_DIR points at a local checkout of the
// pinned Minotaur-Grammars ref; without it the harness downloads from
// raw.githubusercontent.com at the pinned ref (recorded in the results).
//
// Timings are recorded, never asserted: re-running the harness regenerates
// docs/measurements/issue-126 (results.json + RESULTS.md) on the current
// machine; the committed artifacts document the recorded run.
// ---------------------------------------------------------------------------

const string GrammarsRepo = "DevelApp-ai/Minotaur-Grammars";
const string GrammarsRef = "85360f6ff73304ef0d063fc33af9d03845f3d1c3";
// The synthetic rule-count bins (Minotaur-Grammars benchmarks/synthetic,
// issue Minotaur-Grammars#192) were added after the pinned ref above, so the
// #131 mode resolves them from main (or MINOTAUR_GRAMMARS_DIR when set).
const string SyntheticRef = "main";
const int MeasuredConstructs = 10;

var grammarClasses = new (string ClassName, int MinRules, int MaxRules, string RepoPath)[]
{
    ("Small DSL", 15, 25, "data-config/kdl/KDL.grammar"),
    ("Medium DSL", 50, 80, "data-query/sql/SQL.grammar"),
    ("Large DSL", 150, 200, "programming-languages/c17/C17.grammar"),
    ("Complex DSL", 300, int.MaxValue, "programming-languages/csharp10/CSharp10.grammar"),
};

// Issue #131: compile-time by rule-count bin, using the committed synthetic
// grammars (deterministic, gen_synthetic.py) instead of one real grammar per
// class. Bins map to the article's <50 / 50-200 / 200+ classes.
var syntheticClasses = new (string ClassName, int MinRules, int MaxRules, string RepoPath)[]
{
    ("Bin 20 rules", 0, 50, "benchmarks/synthetic/rules-020/rules-020.grammar"),
    ("Bin 60 rules", 50, 200, "benchmarks/synthetic/rules-060/rules-060.grammar"),
    ("Bin 180 rules", 50, 200, "benchmarks/synthetic/rules-180/rules-180.grammar"),
    ("Bin 320 rules", 200, int.MaxValue, "benchmarks/synthetic/rules-320/rules-320.grammar"),
};

// Child mode: measure one grammar in this fresh process and print one JSON line.
if (args.Length == 2 && args[0] == "--measure-one" && int.TryParse(args[1], out var classIndex)
    && classIndex >= 0 && classIndex < grammarClasses.Length)
{
    Console.WriteLine(MeasureOne(grammarClasses[classIndex], GrammarsRef));
    return 0;
}

if (args.Length == 2 && args[0] == "--measure-one-synthetic" && int.TryParse(args[1], out var synthIndex)
    && synthIndex >= 0 && synthIndex < syntheticClasses.Length)
{
    Console.WriteLine(MeasureOne(syntheticClasses[synthIndex], SyntheticRef));
    return 0;
}

// Parent modes: spawn one fresh child per grammar class and aggregate.
if (args.Length >= 2 && args[0] == "--synthetic")
{
    // Issue #131: compile-time by rule-count bin over the committed synthetic
    // grammars (benchmarks/synthetic/rules-*). Resolved from main because the
    // synthetic corpus postdates the pinned #126 ref.
    return RunAggregation(
        args[1],
        "DevelApp-ai/Minotaur#131",
        syntheticClasses,
        SyntheticRef,
        "--measure-one-synthetic",
        "Issue #131 — Grammar compilation time by rule-count bin (synthetic corpus)");
}

var outputDir = args.Length > 0
    ? args[0]
    : Path.Combine("docs", "measurements", "issue-126");
return RunAggregation(
    outputDir,
    "DevelApp-ai/Minotaur#126",
    grammarClasses,
    GrammarsRef,
    "--measure-one",
    "Issue #126 — Grammar compilation performance by grammar size");

// ---------------------------------------------------------------------------
// Parent-mode aggregation: one fresh child process per grammar class (cold
// numbers include JIT), then results.json + RESULTS.md in outputDir.
static int RunAggregation(
    string outputDir,
    string issueTag,
    (string ClassName, int MinRules, int MaxRules, string RepoPath)[] classes,
    string grammarsRef,
    string childArg,
    string title)
{
    Directory.CreateDirectory(outputDir);

    Console.WriteLine("Minotaur grammar-compilation harness (" + issueTag + ")");
    Console.WriteLine($"  runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
    Console.WriteLine($"  os:      {RuntimeInformation.OSDescription}");
    Console.WriteLine($"  cpus:    {Environment.ProcessorCount}");
    Console.WriteLine($"  grammars: {GrammarsRepo} @ {grammarsRef[..12]}");
    Console.WriteLine();

    var rows = new List<JsonObject>();
    var entryAssembly = System.Reflection.Assembly.GetEntryAssembly()!
        .Location;

    // Under `dotnet run` the current process is the apphost (it already knows
    // its entry assembly); when launched via the dotnet driver directly, the
    // entry assembly must be passed explicitly.
    var processPath = Environment.ProcessPath!;
    var isDotnetDriver = string.Equals(
        Path.GetFileName(processPath),
        "dotnet",
        StringComparison.OrdinalIgnoreCase);

    foreach (var (index, grammarClass) in classes.Select((g, i) => (i, g)))
    {
        Console.WriteLine($"measuring {grammarClass.ClassName}: {grammarClass.RepoPath} ...");

        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = isDotnetDriver
                ? $"\"{entryAssembly}\" {childArg} {index}"
                : $"{childArg} {index}",
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
        rows.Add(row);

        var warmMs = row["warmMedianMs"]!.GetValue<double>();
        var coldMs = row["coldMs"]!.GetValue<double>();
        var rules = row["totalRules"]!.GetValue<int>();
        Console.WriteLine($"  rules {rules}, cold {coldMs:F1} ms, warm median {warmMs:F1} ms, " +
            $"parser footprint {row["retainedKb"]!.GetValue<double>():F1} KB");
}

var commitSha = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "unrecorded";
var grammarSource = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR") is { Length: > 0 } dir
    ? $"local checkout of {GrammarsRepo} @ {grammarsRef}"
    : $"raw.githubusercontent.com {GrammarsRepo} @ {grammarsRef}";

var report = new JsonObject
{
    ["issue"] = issueTag,
    ["harness"] = "Minotaur.Benchmarks.GrammarCompilation",
    ["measuredConstructs"] = MeasuredConstructs,
    ["minotaurCommit"] = commitSha,
    ["grammarsRepo"] = GrammarsRepo,
    ["grammarsRef"] = grammarsRef,
    ["grammarSource"] = grammarSource,
    ["machine"] = new JsonObject
    {
        ["os"] = RuntimeInformation.OSDescription,
        ["runtime"] = RuntimeInformation.FrameworkDescription,
        ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
        ["processorCount"] = Environment.ProcessorCount,
        ["engine"] = new JsonObject
        {
            ["develapp.steplexer"] = EngineAssemblyVersion("DevelApp.StepLexer"),
            ["develapp.stepparser"] = EngineAssemblyVersion("DevelApp.StepParser"),
        },
    },
    ["rows"] = new JsonArray(rows.Select(r => (JsonNode)r!.DeepClone()).ToArray()),
};

File.WriteAllText(
    Path.Combine(outputDir, "results.json"),
    report.ToString());

File.WriteAllText(
    Path.Combine(outputDir, "RESULTS.md"),
    BuildResultsMarkdown(report, title));

Console.WriteLine();
Console.WriteLine($"wrote {Path.Combine(outputDir, "results.json")} and RESULTS.md");
return 0;
}

// ---------------------------------------------------------------------------

static string MeasureOne((string ClassName, int MinRules, int MaxRules, string RepoPath) grammarClass, string grammarsRef)
{
    var content = LoadGrammar(grammarClass.RepoPath, grammarsRef);
    var grammarBytes = System.Text.Encoding.UTF8.GetByteCount(content);
    var profile = GrammarProfile.Analyze(content);

    // Cold: first construction in this fresh process (includes JIT). The
    // baseline heap snapshot is taken after a full collection.
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var liveBefore = GC.GetTotalMemory(forceFullCollection: true);

    var stopwatch = Stopwatch.StartNew();
    var session = new RealTimeParserSession(content, Path.GetFileName(grammarClass.RepoPath));
    var coldMs = stopwatch.Elapsed.TotalMilliseconds;

    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var retainedBytes = GC.GetTotalMemory(forceFullCollection: true) - liveBefore;

    // Functional smoke: the compiled parser must lex and parse a sample.
    stopwatch.Restart();
    session.Initialize("var x = 1;\n");
    var initializeMs = stopwatch.Elapsed.TotalMilliseconds;
    var tokenCount = session.Tokens.Count;
    var parseOk = true;
    try
    {
        session.Parse();
    }
    catch (Exception)
    {
        parseOk = false;
    }

    GC.KeepAlive(session);

    // Warm: median of MeasuredConstructs re-compilations in-process.
    var warmMs = new double[MeasuredConstructs];
    for (var i = 0; i < warmMs.Length; i++)
    {
        stopwatch.Restart();
        var warmSession = new RealTimeParserSession(content, Path.GetFileName(grammarClass.RepoPath));
        warmMs[i] = stopwatch.Elapsed.TotalMilliseconds;
        GC.KeepAlive(warmSession);
    }

    Array.Sort(warmMs);
    var warmMedianMs = Median(warmMs);

    var row = new JsonObject
    {
        ["class"] = grammarClass.ClassName,
        ["grammar"] = grammarClass.RepoPath,
        ["grammarBytes"] = grammarBytes,
        ["grammarKb"] = Math.Round(grammarBytes / 1024.0, 1),
        ["tokenRules"] = profile.TokenRuleCount,
        ["distinctTokenTypes"] = profile.DistinctTokenTypeCount,
        ["productionRules"] = profile.ProductionRuleCount,
        ["totalRules"] = profile.TotalRuleCount,
        ["keywords"] = profile.KeywordCount,
        ["coldMs"] = Math.Round(coldMs, 3),
        ["warmMedianMs"] = Math.Round(warmMedianMs, 3),
        ["warmAllMs"] = new JsonArray(warmMs.Select(m => (JsonNode)Math.Round(m, 3)).ToArray()),
        ["initializeMs"] = Math.Round(initializeMs, 3),
        ["initializeTokens"] = tokenCount,
        ["parseOk"] = parseOk,
        ["retainedBytes"] = retainedBytes,
        ["retainedKb"] = Math.Round(retainedBytes / 1024.0, 1),
    };

    return row.ToJsonString();
}

static double Median(double[] sortedValues)
{
    var mid = sortedValues.Length / 2;
    return sortedValues.Length % 2 == 1
        ? sortedValues[mid]
        : (sortedValues[mid - 1] + sortedValues[mid]) / 2.0;
}

static string LoadGrammar(string repoPath, string grammarsRef)
{
    var localDir = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR");
    if (!string.IsNullOrEmpty(localDir))
    {
        var localPath = Path.Combine(localDir, repoPath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(localPath);
    }

    using var http = new HttpClient();
    var url = $"https://raw.githubusercontent.com/DevelApp-ai/Minotaur-Grammars/{grammarsRef}/{repoPath}";
    var content = http.GetStringAsync(url).GetAwaiter().GetResult();
    return content;
}

static string BuildResultsMarkdown(JsonObject report, string title)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# " + title);
    sb.AppendLine();
    sb.AppendLine("Measured by `Minotaur.Benchmarks.GrammarCompilation`: compiling a");
    sb.AppendLine("GrammarForge grammar into the engine's shipped `RealTimeParserSession`");
    sb.AppendLine("(DevelApp.StepParser, the C#-process target Minotaur ships). Cold = first");
    sb.AppendLine("construction in a fresh process (includes JIT); warm = median of 10");
    sb.AppendLine("in-process re-compilations. Parser footprint = the live managed-heap delta");
    sb.AppendLine("of the compiled session after a full collection (approximate).");
    sb.AppendLine();
    sb.AppendLine("Machine: " + report["machine"]!["os"]!.GetValue<string>());
    sb.AppendLine("");
    sb.AppendLine("- Runtime: " + report["machine"]!["runtime"]!.GetValue<string>()
        + " (" + report["machine"]!["architecture"]!.GetValue<string>()
        + ", " + report["machine"]!["processorCount"]!.GetValue<int>() + " CPUs)");
    sb.AppendLine("- Engine: DevelApp.StepLexer `" + report["machine"]!["engine"]!["develapp.steplexer"]!.GetValue<string>()
        + "` / DevelApp.StepParser `" + report["machine"]!["engine"]!["develapp.stepparser"]!.GetValue<string>() + "`");
    sb.AppendLine("- Grammars: " + report["grammarsRepo"]!.GetValue<string>()
        + " @ `" + report["grammarsRef"]!.GetValue<string>()[..12] + "`");
    sb.AppendLine("- Minotaur commit: `" + report["minotaurCommit"]!.GetValue<string>() + "`");
    sb.AppendLine("- Harness: measured " + report["measuredConstructs"]!.GetValue<int>()
        + " warm constructs per class");
    sb.AppendLine();
    sb.AppendLine("| Grammar Size | Grammar | Rules | Tokens | Cold | Warm median | Parser footprint | Grammar file |");
    sb.AppendLine("|-------------|---------|-------|--------|------|-------------|------------------|--------------|");

    foreach (var row in report["rows"]!.AsArray().Cast<JsonObject>())
    {
        sb.AppendLine($"| {row["class"]!.GetValue<string>()} "
            + $"| `{Path.GetFileName(row["grammar"]!.GetValue<string>())}` "
            + $"| {row["totalRules"]!.GetValue<int>()} "
            + $"| {row["distinctTokenTypes"]!.GetValue<int>()} "
            + $"| {row["coldMs"]!.GetValue<double>():F1} ms "
            + $"| {row["warmMedianMs"]!.GetValue<double>():F1} ms "
            + $"| {row["retainedKb"]!.GetValue<double>():F1} KB "
            + $"| {row["grammarKb"]!.GetValue<double>():F1} KB |");
    }

    sb.AppendLine();
    sb.AppendLine("Measured compilation times are recorded, never asserted: re-running the");
    sb.AppendLine("harness regenerates these artifacts on the current machine. Compare against");
    sb.AppendLine("the article's projected tables and replace the projections with these values.");
    sb.AppendLine();
    sb.AppendLine("Full per-class detail (rule split, keyword counts, all warm samples, smoke");
    sb.AppendLine("parse results) is in `results.json` next to this file.");
    return sb.ToString();
}

static string EngineAssemblyVersion(string assemblyName)
{
    try
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
            ?? System.Reflection.Assembly.Load(assemblyName);
        return loaded.GetName().Version?.ToString() ?? "0.0.0";
    }
    catch
    {
        return "unrecorded";
    }
}
