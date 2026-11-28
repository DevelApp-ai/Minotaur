using System.Text.Json;
using Minotaur.Labyrinth;

namespace Minotaur.Cli;

/// <summary>
/// Runs the Minotaur-Labyrinth test corpus (tests/&lt;grammar&gt;/&lt;rule-id&gt;/)
/// through the real Labyrinth engine and checks each case's expected.json:
/// the vulnerable file must produce the expected findings, the safe file none.
/// <para>
/// <c>--rules</c> may be a directory of YAML rule files (the classic path) or
/// a binary rule pack file built by <c>pack build</c> (Minotaur issue #283):
/// then every case is verified through the packed rules — YAML parse, pattern
/// tokenization and pattern mini-AST building are skipped, and the pack's
/// grammar hash is checked against the resolved grammar file so the corpus
/// can never silently drift from the pack it was built from.
/// </para>
/// </summary>
public static class CorpusVerifier
{
    /// <summary>Hard per-case timeout: a case that exceeds it is failed (not hung),
    /// so one pathological input cannot stall the whole corpus run for hours.</summary>
    private static readonly TimeSpan CaseTimeout = TimeSpan.FromSeconds(120);

    public static int Verify(string rulesRoot, string testsRoot)
    {
        var grammarsRoot = ScanHost.FindGrammarsRoot();

        // Binary rule pack mode (issue #283): load once, verify every case
        // through the packed rules.
        LabyrinthRulePackFileResult? packed = null;
        if (File.Exists(rulesRoot) && !Directory.Exists(rulesRoot))
        {
            packed = LabyrinthRulePack.TryRead(rulesRoot);
            if (!packed.IsValid)
            {
                Console.Error.WriteLine($"labyrinth: rule pack '{rulesRoot}' is invalid ({packed.Errors.Count} error(s)):");
                foreach (var error in packed.Errors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }

                return LabyrinthExitCodes.ConfigurationError;
            }

            Console.WriteLine(
                $"labyrinth: using binary rule pack '{rulesRoot}' ({packed.Manifest?.RuleCount} rule(s), " +
                $"target grammar {packed.Manifest?.TargetGrammarName}).");
        }

        var cases = 0;
        var failures = 0;
        foreach (var grammarDir in Directory.GetDirectories(testsRoot).OrderBy(d => d, StringComparer.Ordinal))
        {
            var grammarDirName = Path.GetFileName(grammarDir);
            foreach (var caseDir in Directory.GetDirectories(grammarDir).OrderBy(d => d, StringComparer.Ordinal))
            {
                cases++;
                var ruleId = Path.GetFileName(caseDir);
                var label = $"{grammarDirName}/{ruleId}";
                Console.WriteLine($"RUN {label}");
                try
                {
                    // LongRunning: the abandoned task (on timeout) keeps its own
                    // thread instead of starving the thread pool for later cases.
                    var task = Task.Factory.StartNew(
                        () => VerifyCase(grammarsRoot, rulesRoot, grammarDirName, caseDir, ruleId, packed),
                        TaskCreationOptions.LongRunning);
                    if (!task.Wait(CaseTimeout))
                    {
                        failures++;
                        Console.WriteLine(
                            $"FAIL {label}: timeout after {(int)CaseTimeout.TotalSeconds}s " +
                            "(engine hang suspected — see DevelApp-ai/Minotaur#246)");
                        continue;
                    }

                    if (!task.Result)
                    {
                        failures++;
                        Console.WriteLine($"FAIL {label}");
                    }
                    else
                    {
                        Console.WriteLine($"PASS {label}");
                    }
                }
                catch (AggregateException agg) when (agg.InnerException is not null)
                {
                    failures++;
                    Console.WriteLine($"FAIL {label}: {agg.InnerException.Message}");
                }
                catch (Exception ex)
                {
                    failures++;
                    Console.WriteLine($"FAIL {label}: {ex.Message}");
                }
            }
        }

        Console.WriteLine(failures == 0
            ? $"OK: {cases} corpus case(s) pass."
            : $"FAILED: {failures} of {cases} corpus case(s) failed.");
        return failures == 0 ? LabyrinthExitCodes.Success : LabyrinthExitCodes.Findings;
    }

    private static bool VerifyCase(
        string grammarsRoot, string rulesRoot, string grammarDirName, string caseDir, string ruleId,
        LabyrinthRulePackFileResult? packed)
    {
        var vulnerable = Directory.GetFiles(caseDir, "vulnerable.*").FirstOrDefault();
        var safe = Directory.GetFiles(caseDir, "safe.*").FirstOrDefault();
        var expectedPath = Path.Combine(caseDir, "expected.json");
        if (vulnerable is null || safe is null || !File.Exists(expectedPath))
        {
            Console.Error.WriteLine($"{grammarDirName}/{ruleId}: needs vulnerable.*, safe.* and expected.json");
            return false;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(expectedPath));
        var root = doc.RootElement;
        if (root.GetProperty("rule").GetString() != ruleId)
        {
            Console.Error.WriteLine($"{grammarDirName}/{ruleId}: expected.json 'rule' mismatch");
            return false;
        }

        var expectedFindings = root.GetProperty("findings").EnumerateArray()
            .Select(f => (file: f.GetProperty("file").GetString()!, line: f.GetProperty("line").GetInt32()))
            .ToList();

        var extension = Path.GetExtension(vulnerable).TrimStart('.').ToLowerInvariant();
        var targetName = grammarDirName == "common" ? extension : grammarDirName;
        var target = ScanHost.ResolveTarget(targetName);
        var grammarContent = File.ReadAllText(ScanHost.GrammarFilePath(grammarsRoot, target));

        // Resolve the rule under verification: from the binary pack, or from
        // the YAML rule directory (the classic path).
        LabyrinthRule rule;
        IReadOnlyList<CompiledLabyrinthRule> compiledCaseRules;
        LabyrinthRuleSet? yamlCaseRules = null;
        if (packed is not null)
        {
            var manifest = packed.Manifest!;
            if (!string.Equals(manifest.TargetGrammarName, target.Directory, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    $"{grammarDirName}/{ruleId}: rule pack was built for target grammar '{manifest.TargetGrammarName}', not '{target.Directory}'");
                return false;
            }

            var grammarHash = LabyrinthRulePack.GrammarSha256(grammarContent);
            if (!string.Equals(manifest.GrammarSha256, grammarHash, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    $"{grammarDirName}/{ruleId}: rule pack was built against different grammar content " +
                    $"({manifest.GrammarSha256[..12]}... vs current {grammarHash[..12]}...); rebuild the pack");
                return false;
            }

            var compiledRule = packed.Rules.FirstOrDefault(r =>
                string.Equals(r.Rule.Id, ruleId, StringComparison.Ordinal));
            if (compiledRule is null)
            {
                Console.Error.WriteLine(
                    $"{grammarDirName}/{ruleId}: rule '{ruleId}' not found in rule pack '{rulesRoot}'");
                return false;
            }

            rule = compiledRule.Rule;
            compiledCaseRules = new[] { compiledRule };
        }
        else
        {
            var rulesDir = Path.Combine(rulesRoot, grammarDirName);
            var pack = new LabyrinthRulePackLoader().TryLoadDirectory(rulesDir);
            if (!pack.IsValid)
            {
                Console.Error.WriteLine($"{grammarDirName}/{ruleId}: invalid rule pack: {string.Join("; ", pack.Errors)}");
                return false;
            }

            var yamlRule = pack.RuleSet.Rules.FirstOrDefault(r => r.Id == ruleId);
            if (yamlRule is null)
            {
                Console.Error.WriteLine($"{grammarDirName}/{ruleId}: rule '{ruleId}' not found in {rulesDir}");
                return false;
            }

            rule = yamlRule;
            yamlCaseRules = new LabyrinthRuleSet();
            yamlCaseRules.Rules.Add(yamlRule);
            compiledCaseRules = Array.Empty<CompiledLabyrinthRule>();
        }

        var findings = ScanFile(compiledCaseRules, yamlCaseRules, grammarContent, target.Directory, vulnerable);
        foreach (var (file, line) in expectedFindings)
        {
            var hit = findings.Any(f =>
                f.RuleId == ruleId
                && f.Sink is ILabyrinthFindingLocation loc
                && Path.GetFileName(loc.FilePath) == file
                && loc.StartLine == line);
            if (!hit)
            {
                Console.Error.WriteLine(
                    $"{grammarDirName}/{ruleId}: expected finding {ruleId} @{file}:{line} not produced " +
                    $"(got: {string.Join(", ", findings.Where(f => f.RuleId == ruleId).Select(Describe))})");
                return false;
            }
        }

        var safeFindings = ScanFile(compiledCaseRules, yamlCaseRules, grammarContent, target.Directory, safe)
            .Where(f => f.RuleId == ruleId)
            .ToList();
        if (safeFindings.Count > 0)
        {
            Console.Error.WriteLine(
                $"{grammarDirName}/{ruleId}: safe file produced findings: {string.Join(", ", safeFindings.Select(Describe))}");
            return false;
        }

        return true;
    }

    private static string Describe(LabyrinthTaintFinding finding) =>
        finding.Sink is ILabyrinthFindingLocation loc
            ? $"{finding.RuleId} @{Path.GetFileName(loc.FilePath)}:{loc.StartLine}"
            : $"{finding.RuleId} @<no location>";

    private static List<LabyrinthTaintFinding> ScanFile(
        IReadOnlyList<CompiledLabyrinthRule> compiledRules,
        LabyrinthRuleSet? yamlRules,
        string grammarContent,
        string grammarName,
        string path)
    {
        var name = Path.GetFileName(path);
        var source = File.ReadAllText(path);
        Console.Error.WriteLine($"  [{name}] load grammar + parse + materialize...");
        var graph = ScanHost.BuildGraph(source, path, grammarContent, grammarName, out var nodes);
        Console.Error.WriteLine($"  [{name}] graph built ({nodes.Count} node(s)); compile rules + run...");
        var findings = compiledRules.Count > 0
            ? ScanHost.RunCompiledRules(compiledRules, graph, nodes, path)
            : ScanHost.RunRules(yamlRules!, grammarContent, grammarName, graph, nodes, path);
        Console.Error.WriteLine($"  [{name}] done: {findings.Count} finding(s)");
        return findings;
    }
}
