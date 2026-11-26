using System.Text.Json;
using Minotaur.Labyrinth;

namespace Minotaur.Cli;

/// <summary>
/// Runs the Minotaur-Labyrinth test corpus (tests/&lt;grammar&gt;/&lt;rule-id&gt;/)
/// through the real Labyrinth engine and checks each case's expected.json:
/// the vulnerable file must produce the expected findings, the safe file none.
/// </summary>
public static class CorpusVerifier
{
    public static int Verify(string rulesRoot, string testsRoot)
    {
        var grammarsRoot = ScanHost.FindGrammarsRoot();
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
                try
                {
                    if (!VerifyCase(grammarsRoot, rulesRoot, grammarDirName, caseDir, ruleId))
                    {
                        failures++;
                        Console.WriteLine($"FAIL {label}");
                    }
                    else
                    {
                        Console.WriteLine($"PASS {label}");
                    }
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

    private static bool VerifyCase(string grammarsRoot, string rulesRoot, string grammarDirName, string caseDir, string ruleId)
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

        var rulesDir = Path.Combine(rulesRoot, grammarDirName);
        var pack = new LabyrinthRulePackLoader().TryLoadDirectory(rulesDir);
        if (!pack.IsValid)
        {
            Console.Error.WriteLine($"{grammarDirName}/{ruleId}: invalid rule pack: {string.Join("; ", pack.Errors)}");
            return false;
        }

        var rule = pack.RuleSet.Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null)
        {
            Console.Error.WriteLine($"{grammarDirName}/{ruleId}: rule '{ruleId}' not found in {rulesDir}");
            return false;
        }

        var single = new LabyrinthRuleSet();
        single.Rules.Add(rule);

        var findings = ScanFile(single, grammarContent, target.Directory, vulnerable);
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

        var safeFindings = ScanFile(single, grammarContent, target.Directory, safe)
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

    private static List<LabyrinthTaintFinding> ScanFile(LabyrinthRuleSet rules, string grammarContent, string grammarName, string path)
    {
        var source = File.ReadAllText(path);
        var graph = ScanHost.BuildGraph(source, path, grammarContent, grammarName, out var nodes);
        return ScanHost.RunRules(rules, grammarContent, grammarName, graph, nodes, path);
    }
}
