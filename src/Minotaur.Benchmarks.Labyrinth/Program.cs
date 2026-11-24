using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Minotaur.Cli;
using Minotaur.Labyrinth;
using Minotaur.Benchmarks.Labyrinth;

// Minotaur.Benchmarks.Labyrinth: Track GSE L3 measurement harness (Minotaur
// issue #246). Measures the shipped csharp10 rule pack end to end over a
// deterministic synthetic corpus (see CorpusGenerator for the ground-truth
// contract): parse cost (ScanHost.BuildGraph), full-pack scan cost
// (ScanHost.RunRules), and per-rule attribution (single-rule scans over that
// rule's own corpus), against exact-by-construction expected findings.
// Also emits the engine-side rule-language surface report (LAB harness #263:
// LabyrinthRuleSurfaceReport — feature usage + per-rule compile cost).
// Emits results.json, surface-report.json, RESULTS.md and ground-truth.json
// into the outdir.
// Pass --only-rule <id> for a fast single-rule smoke of the same protocol.

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("""
        Usage: dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth -- <outdir> [options]

        Options:
          --pairs N        vulnerable/safe program pairs per rule (default 10;
                           the committed reference and the CI lane use 10)
          --reps R         measurement repetitions; medians are reported and
                           correctness uses the last repetition (default 3)
          --rules DIR      rule pack root containing csharp10/ (default: the
                           repository checkout this binary lives in)
          --only-rule ID   smoke mode: measure only rule ID (its corpus,
                           pack filtered to that rule, attribution skipped)
          --corpus-only    emit ground-truth.json and corpus/ without scanning
                           (deterministic generator, no grammar needed)
          --write-corpus   also emit the generated programs under
                           <outdir>/corpus/<rule-id>/

        Outputs into <outdir>: results.json, surface-report.json, RESULTS.md, ground-truth.json.
        MINOTAUR_COMMIT (set by CI) is recorded in results.json and RESULTS.md.
        The production 'minotaur labyrinth scan' path resolves and compiles the
        grammar once per file, and this harness reports the same per-file cost.
        Exit code 2 when detection is not 100% or any safe program reports a
        finding, so a CI measurement lane fails loudly on a regression.
        """);
    return 0;
}

var outdir = args[0];
int pairs = GetInt(args, "--pairs", 10);
int reps = GetInt(args, "--reps", 3);
bool writeCorpus = HasFlag(args, "--write-corpus");
string rulesRoot = GetString(args, "--rules") ?? FindRepositoryRulesRoot();
string? onlyRule = GetString(args, "--only-rule");

Directory.CreateDirectory(outdir);

// ---------- setup: rules, grammar, corpus ----------

var packResult = new LabyrinthRulePackLoader().TryLoadDirectory(rulesRoot);
if (!packResult.IsValid)
{
    Console.Error.WriteLine("invalid rule pack: " + string.Join("; ", packResult.Errors));
    return 1;
}

if (onlyRule is not null && packResult.RuleSet.Rules.All(r => r.Id != onlyRule))
{
    Console.Error.WriteLine($"unknown --only-rule '{onlyRule}'; pack has: " +
        string.Join(", ", packResult.RuleSet.Rules.Select(r => r.Id)));
    return 1;
}

// Smoke mode scans with a pack filtered to the one rule; the default scans
// with the full shipped pack (the production 'labyrinth scan' cost profile).
var pack = new LabyrinthRuleSet();
pack.Rules.AddRange(onlyRule is null
    ? packResult.RuleSet.Rules
    : packResult.RuleSet.Rules.Where(r => r.Id == onlyRule));

var grammarsRoot = ScanHost.FindGrammarsRoot();
var target = ScanHost.ResolveTarget("csharp10");
var grammarContent = File.ReadAllText(ScanHost.GrammarFilePath(grammarsRoot, target));
var grammarName = target.Directory;

// Engine-side rule-language surface report (LAB harness, #263): feature usage and
// per-rule compile cost / delegate counts for the pack under measurement.
var surfaceReport = LabyrinthRuleSurfaceReport.Build(pack, grammarContent, grammarName);

var cases = CorpusGenerator.Generate(pairs);
if (onlyRule is not null)
{
    cases = cases.Where(c => c.RuleId == onlyRule).ToList();
}

var allFiles = cases.SelectMany(c => c.Files).ToList();
var fileToCase = cases.SelectMany(c => c.Files.Select(f => (f.FileName, Case: c)))
    .ToDictionary(t => t.FileName, t => t.Case);
int totalLoc = allFiles.Sum(f => f.Content.Count(ch => ch == '\n'));
int totalExpected = cases.Sum(c => c.ExpectedFindings.Count);

Console.WriteLine($"rules: {string.Join(", ", pack.Rules.Select(r => r.Id))}");
Console.WriteLine($"corpus: {cases.Count} rules x {pairs} pairs = {allFiles.Count} programs, {totalLoc} lines, {totalExpected} expected findings");
Console.WriteLine($"grammars root: {grammarsRoot} (target {target.Directory})");
Console.WriteLine($"environment: {RuntimeInformation.OSDescription}, {RuntimeInformation.FrameworkDescription}, {Environment.ProcessorCount} logical processors");

if (HasFlag(args, "--corpus-only"))
{
    // Corpus-only mode: emit the deterministic corpus and its ground truth
    // without scanning (the generator is pure, so this needs seconds, not
    // the minutes the full grammar-loading protocol costs). Useful to pin
    // ground-truth.json in the repository at a given --pairs scale.
    EmitGroundTruth(outdir, cases);
    EmitCorpus(outdir, cases);
    Console.WriteLine($"corpus-only mode: wrote ground-truth.json and corpus/ to {outdir}");
    return 0;
}

// ---------- passes: parse + full-pack scan, correctness from last rep ----------

var parseSamples = new List<double>[reps];
var packSamples = new List<double>[reps];
for (int r = 0; r < reps; r++)
{
    parseSamples[r] = new List<double>();
    packSamples[r] = new List<double>();
}

var lastFindings = new List<LabyrinthTaintFinding>();
var allocationsBefore = GC.GetTotalAllocatedBytes(precise: true);
var chrono = Stopwatch.StartNew();

for (int r = 0; r < reps; r++)
{
    lastFindings.Clear();
    foreach (var file in allFiles)
    {
        var parseWatch = Stopwatch.StartNew();
        var graph = ScanHost.BuildGraph(file.Content, file.FileName, grammarContent, grammarName, out var nodes);
        parseWatch.Stop();
        parseSamples[r].Add(parseWatch.Elapsed.TotalMilliseconds);

        var packWatch = Stopwatch.StartNew();
        var findings = ScanHost.RunRules(pack, grammarContent, grammarName, graph, nodes, file.FileName);
        packWatch.Stop();
        packSamples[r].Add(packWatch.Elapsed.TotalMilliseconds);
        lastFindings.AddRange(findings);
    }

    Console.WriteLine($"rep {r + 1}/{reps}: {allFiles.Count} programs scanned, {lastFindings.Count} findings");
}

chrono.Stop();
var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocationsBefore;

// ---------- correctness: findings vs ground truth ----------

int truePositives = 0, falseNegatives = 0, falsePositives = 0;
var perRule = new Dictionary<string, (int Tp, int Fn, int Expected)>();
foreach (var kase in cases)
{
    int tp = 0, fn = 0;
    foreach (var (file, line) in kase.ExpectedFindings)
    {
        if (lastFindings.Any(f =>
                f.RuleId == kase.RuleId
                && f.Sink is ILabyrinthFindingLocation loc
                && Path.GetFileName(loc.FilePath) == file
                && loc.StartLine == line))
        {
            tp++;
        }
        else
        {
            fn++;
        }
    }

    perRule[kase.RuleId] = (tp, fn, kase.ExpectedFindings.Count);
    truePositives += tp;
    falseNegatives += fn;
}

// Every finding whose (rule, file, line) is not an expected triple of the
// case its file belongs to is a false positive. Findings with no location
// count as false positives too.
var falsePosByRule = new Dictionary<string, int>();
foreach (var finding in lastFindings)
{
    bool isFalsePositive;
    if (finding.Sink is not ILabyrinthFindingLocation loc)
    {
        isFalsePositive = true;
    }
    else
    {
        var name = Path.GetFileName(loc.FilePath);
        isFalsePositive = !fileToCase.TryGetValue(name, out var owner)
            || owner.RuleId != finding.RuleId
            || !owner.ExpectedFindings.Any(e => e.File == name && e.Line == loc.StartLine);
    }

    if (isFalsePositive)
    {
        falsePositives++;
        falsePosByRule[finding.RuleId] = falsePosByRule.GetValueOrDefault(finding.RuleId) + 1;
    }
}

// ---------- per-rule attribution: single-rule scans over that rule's corpus ----------

// The full-pack pass reports the shipped cost profile. This pass answers
// "how much of that cost is one rule alone?": each rule is compiled and run
// solo over exactly its own generated programs (parse included), which is
// also the corpus the rule is written against.
var perRuleMs = new Dictionary<string, double>();
if (onlyRule is not null)
{
    Console.WriteLine("smoke mode: skipping per-rule attribution (single-rule corpus)");
}
else
{
    foreach (var kase in cases)
    {
        var rule = pack.Rules.First(r => r.Id == kase.RuleId);
        var single = new LabyrinthRuleSet();
        single.Rules.Add(rule);
        var ruleWatch = Stopwatch.StartNew();
        foreach (var file in kase.Files)
        {
            var graph = ScanHost.BuildGraph(file.Content, file.FileName, grammarContent, grammarName, out var nodes);
            ScanHost.RunRules(single, grammarContent, grammarName, graph, nodes, file.FileName);
        }

        ruleWatch.Stop();
        perRuleMs[kase.RuleId] = kase.Files.Count == 0 ? 0 : ruleWatch.Elapsed.TotalMilliseconds / kase.Files.Count;
    }
}

// ---------- emit ----------

double parseMedian = Median(parseSamples.SelectMany(s => s).ToList());
double packMedian = Median(packSamples.SelectMany(s => s).ToList());
double detectionRate = totalExpected == 0 ? 0 : (double)truePositives / totalExpected * 100.0;
double precision = truePositives + falsePositives == 0 ? 100.0 : (double)truePositives / (truePositives + falsePositives) * 100.0;
var commit = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "unrecorded";

var timing = new
{
    wallClockMs = Math.Round(chrono.Elapsed.TotalMilliseconds, 1),
    parseMsPerFile = Math.Round(parseMedian, 3),
    fullPackMsPerFile = Math.Round(packMedian, 3),
    perRuleMsPerFile = perRuleMs.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 3)),
    allocatedBytes = allocated,
};
var report = new
{
    minotaurCommit = commit,
    harness = "Minotaur.Benchmarks.Labyrinth",
    track = "Minotaur #246 L3",
    pairsPerRule = pairs,
    reps,
    rules = pack.Rules.Select(r => r.Id).ToArray(),
    corpus = new
    {
        programsPerRule = pairs * 2,
        programs = allFiles.Count,
        lines = totalLoc,
        expectedFindings = totalExpected,
    },
    environment = new
    {
        os = RuntimeInformation.OSDescription,
        runtime = RuntimeInformation.FrameworkDescription,
        architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        logicalProcessors = Environment.ProcessorCount,
    },
    detection = perRule.Select(kv => new
    {
        rule = kv.Key,
        expected = kv.Value.Expected,
        truePositives = kv.Value.Tp,
        falseNegatives = kv.Value.Fn,
        falsePositives = falsePosByRule.GetValueOrDefault(kv.Key),
        detectionRate = kv.Value.Expected == 0 ? 0 : Math.Round(kv.Value.Tp * 100.0 / kv.Value.Expected, 1),
    }).ToArray(),
    overall = new
    {
        truePositives,
        falseNegatives,
        falsePositives,
        detectionRate = Math.Round(detectionRate, 1),
        precision = Math.Round(precision, 1),
    },
    ruleSurface = new
    {
        featuresUsed = surfaceReport.FeaturesUsed.ToArray(),
        totalPatterns = surfaceReport.TotalPatterns,
        totalCompiledDelegates = surfaceReport.TotalCompiledDelegates,
        totalCompileMs = Math.Round(surfaceReport.TotalCompileMs, 3),
        perRule = surfaceReport.Rules.Select(r => new
        {
            rule = r.RuleId,
            type = r.Type,
            entries = r.Entries,
            patterns = r.Patterns,
            metavariables = r.Metavariables,
            compiledDelegates = r.CompiledDelegates,
            compileMs = Math.Round(r.CompileMs, 3),
            features = r.Features.ToArray(),
        }).ToArray(),
    },
    timing,
};

File.WriteAllText(Path.Combine(outdir, "results.json"),
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");

File.WriteAllText(Path.Combine(outdir, "surface-report.json"),
    LabyrinthRuleSurfaceReport.ToJson(surfaceReport) + "\n");

EmitGroundTruth(outdir, cases);

if (writeCorpus)
{
    EmitCorpus(outdir, cases);
}

File.WriteAllText(Path.Combine(outdir, "RESULTS.md"), BuildResultsMarkdown());
Console.WriteLine($"wrote results.json, surface-report.json, RESULTS.md, ground-truth.json to {outdir}");
Console.WriteLine($"detection {truePositives}/{totalExpected} ({report.overall.detectionRate}%), false positives {falsePositives}, precision {report.overall.precision}%");

return detectionRate < 100.0 || falsePositives > 0 ? 2 : 0;

// ---------- local functions ----------

static void EmitGroundTruth(string outdir, List<CorpusGenerator.GeneratedCase> cases)
{
    var groundTruth = cases.SelectMany(c => c.ExpectedFindings.Select(e => new { rule = c.RuleId, file = e.File, line = e.Line }));
    File.WriteAllText(Path.Combine(outdir, "ground-truth.json"),
        JsonSerializer.Serialize(groundTruth, new JsonSerializerOptions { WriteIndented = true }) + "\n");
}

static void EmitCorpus(string outdir, List<CorpusGenerator.GeneratedCase> cases)
{
    foreach (var kase in cases)
    {
        var dir = Path.Combine(outdir, "corpus", kase.RuleId);
        Directory.CreateDirectory(dir);
        foreach (var file in kase.Files)
        {
            File.WriteAllText(Path.Combine(dir, file.FileName), file.Content);
        }
    }
}

string BuildResultsMarkdown()
{
    var sb = new StringBuilder();
    sb.AppendLine("# Labyrinth csharp10 Rule Pack Measurement (Minotaur #246, Track GSE L3)");
    sb.AppendLine();
    sb.AppendLine($"Minotaur commit: `{commit}`");
    sb.AppendLine();
    sb.AppendLine("## Protocol");
    sb.AppendLine();
    sb.AppendLine("Each generated program is parsed once per repetition (grammar `csharp10`, the");
    sb.AppendLine("same `ScanHost.BuildGraph` path as `minotaur labyrinth scan`, which resolves and");
    sb.AppendLine("compiles the grammar once per file), then the full shipped rule pack runs over");
    sb.AppendLine("the graph (`ScanHost.RunRules`). Detection compares the last repetition's");
    sb.AppendLine("findings against the exact ground truth (rule id, file, 1-based sink line)");
    sb.AppendLine("built into the corpus. Per-rule attribution compiles each rule alone and runs");
    sb.AppendLine("it over that rule's own programs (parse included). Reported timings are");
    sb.AppendLine($"medians over all {reps} repetitions and {allFiles.Count} files.");
    sb.AppendLine();
    sb.AppendLine("## Environment");
    sb.AppendLine();
    sb.AppendLine($"- OS: {RuntimeInformation.OSDescription}");
    sb.AppendLine($"- Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
    sb.AppendLine($"- Logical processors: {Environment.ProcessorCount}");
    sb.AppendLine();
    sb.AppendLine("## Corpus");
    sb.AppendLine();
    sb.AppendLine($"- Rules: {cases.Count}");
    sb.AppendLine($"- Programs per rule: {pairs * 2} ({pairs} vulnerable + {pairs} safe)");
    sb.AppendLine($"- Programs total: {allFiles.Count}");
    sb.AppendLine($"- Lines total: {totalLoc}");
    sb.AppendLine($"- Expected findings: {totalExpected}");
    sb.AppendLine();
    sb.AppendLine("## Detection vs ground truth");
    sb.AppendLine();
    sb.AppendLine("| Rule | Expected | TP | FN | FP | Detection rate |");
    sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
    foreach (var c in cases)
    {
        var (tp, fn, expected) = perRule[c.RuleId];
        var ruleFp = falsePosByRule.GetValueOrDefault(c.RuleId);
        var rate = expected == 0 ? 0.0 : Math.Round(tp * 100.0 / expected, 1);
        sb.AppendLine($"| `{c.RuleId}` | {expected} | {tp} | {fn} | {ruleFp} | {rate}% |");
    }

    sb.AppendLine();
    sb.AppendLine($"**Overall**: TP {truePositives}, FN {falseNegatives}, FP {falsePositives}, detection rate {report.overall.detectionRate}%, precision {report.overall.precision}%.");
    sb.AppendLine();
    sb.AppendLine("## Timing");
    sb.AppendLine();
    sb.AppendLine("Per-file medians (grammar load and rule compilation included, as the shipped");
    sb.AppendLine("scan path performs them):");
    sb.AppendLine();
    sb.AppendLine("| Phase | ms/file (median) |");
    sb.AppendLine("| --- | ---: |");
    sb.AppendLine($"| Parse (`ScanHost.BuildGraph`) | {timing.parseMsPerFile} |");
    sb.AppendLine($"| Full pack (`ScanHost.RunRules`, {cases.Count} rules) | {timing.fullPackMsPerFile} |");
    sb.AppendLine();
    sb.AppendLine("Single-rule attribution (rule compiled alone, run over that rule's programs,");
    sb.AppendLine("parse included), ms/file:");
    sb.AppendLine();
    sb.AppendLine("| Rule | ms/file |");
    sb.AppendLine("| --- | ---: |");
    foreach (var kv in perRuleMs)
    {
        sb.AppendLine($"| `{kv.Key}` | {kv.Value:F3} |");
    }

    sb.AppendLine();
    sb.AppendLine($"Wall clock for {reps} full repetitions over {allFiles.Count} programs: {timing.wallClockMs} ms.");
    sb.AppendLine($"Managed allocation across those repetitions: {timing.allocatedBytes} bytes.");
    sb.AppendLine();
    sb.AppendLine("## Rule-language surface (engine-side, LAB harness #263)");
    sb.AppendLine();
    sb.AppendLine("Features used by the pack: "
        + string.Join(", ", surfaceReport.FeaturesUsed.Select(f => $"`{f}`")) + ".");
    sb.AppendLine();
    sb.AppendLine("| Rule | Type | Entries | Patterns | Metavars | Delegates | Compile ms | Features |");
    sb.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |");
    foreach (var r in surfaceReport.Rules)
    {
        sb.AppendLine($"| `{r.RuleId}` | {r.Type} | {r.Entries} | {r.Patterns} | {r.Metavariables} | {r.CompiledDelegates} | {r.CompileMs:F3} | {string.Join(", ", r.Features.Select(f => $"`{f}`"))} |");
    }

    sb.AppendLine();
    sb.AppendLine($"Pack totals: {surfaceReport.TotalPatterns} patterns, {surfaceReport.TotalCompiledDelegates} compiled delegates, {surfaceReport.TotalCompileMs:F3} ms compile time. Full detail in `surface-report.json`.");
    return sb.ToString();
}

static int GetInt(string[] args, string flag, int fallback)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == flag && int.TryParse(args[i + 1], out var v))
        {
            return v;
        }
    }

    return fallback;
}

static string? GetString(string[] args, string flag)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == flag)
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasFlag(string[] args, string flag) => args.Contains(flag);

static double Median(List<double> values)
{
    var sorted = values.OrderBy(v => v).ToList();
    int n = sorted.Count;
    return n switch
    {
        0 => 0,
        1 => sorted[0],
        _ => n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0,
    };
}

static string FindRepositoryRulesRoot()
{
    // Walk up from the binary to the checkout containing rules/csharp10,
    // mirroring ScanHost.FindGrammarsRoot's Grammars lookup.
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "rules", "csharp10")))
        {
            return Path.Combine(dir.FullName, "rules");
        }

        dir = dir.Parent!;
    }

    throw new InvalidOperationException(
        "could not locate the repository's rules/ directory; pass --rules explicitly");
}
