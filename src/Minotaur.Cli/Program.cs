using Minotaur.Labyrinth;

namespace Minotaur.Cli;

/// <summary>
/// Minotaur command line: runs Labyrinth SAST rule packs over source files.
///
///   minotaur labyrinth scan --rules &lt;dir&gt; --grammar &lt;name&gt; --src &lt;file|dir&gt;
///       [--ext .cs,.py] [--sarif out.sarif] [--baseline b.json] [--write-baseline b.json]
///   minotaur labyrinth verify-corpus --rules &lt;dir&gt; --tests &lt;dir&gt;
///   minotaur labyrinth grammars
/// </summary>
public static class Program
{
    private static readonly Dictionary<string, string[]> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp10"] = new[] { ".cs" },
        ["python311"] = new[] { ".py" },
        ["javascript"] = new[] { ".js" },
        ["javascriptes2022"] = new[] { ".js" },
        ["typescript"] = new[] { ".ts" },
        ["java8"] = new[] { ".java" },
        ["java17"] = new[] { ".java" },
        ["go119"] = new[] { ".go" },
        ["rust2021"] = new[] { ".rs" },
        ["cpp20"] = new[] { ".cpp", ".c", ".h", ".hpp" },
        ["c17"] = new[] { ".c", ".h" },
        ["json"] = new[] { ".json" },
        ["xml"] = new[] { ".xml" },
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return Usage();
            }

            return args[0] switch
            {
                "labyrinth" => Labyrinth(args.Skip(1).ToArray()),
                "help" or "--help" or "-h" => Usage(),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"minotaur: error: {ex.Message}");
            return LabyrinthExitCodes.ConfigurationError;
        }
    }

    private static int Labyrinth(string[] args)
    {
        if (args.Length == 0)
        {
            return Usage();
        }

        var command = args[0];
        var options = ParseOptions(args.Skip(1).ToArray());
        return command switch
        {
            "scan" => Scan(options),
            "verify-corpus" => CorpusVerifier.Verify(Required(options, "rules"), Required(options, "tests")),
            "pack" => Pack(options),
            "grammars" => ListGrammars(),
            _ => Usage(),
        };
    }

    /// <summary>
    /// Builds a binary rule pack (issue #283): loads the YAML rule directory,
    /// compiles every pattern against the target grammar once, and writes the
    /// compact pack file that <c>scan --rules</c> and
    /// <c>verify-corpus --rules</c> accept in place of the directory.
    /// </summary>
    private static int Pack(Dictionary<string, string> options)
    {
        var rules = Required(options, "rules");
        var grammarName = Required(options, "grammar");
        var output = Required(options, "out");

        var grammarsRoot = ScanHost.FindGrammarsRoot();
        var target = ScanHost.ResolveTarget(grammarName);
        var grammarPath = ScanHost.GrammarFilePath(grammarsRoot, target);
        if (!File.Exists(grammarPath))
        {
            Console.Error.WriteLine($"minotaur: error: grammar file not found: {grammarPath}");
            return LabyrinthExitCodes.ConfigurationError;
        }

        var grammarContent = File.ReadAllText(grammarPath);
        var result = LabyrinthRulePack.Build(
            rules, grammarContent, target.Directory, output,
            options.TryGetValue("id", out var id) ? id : null);
        if (!result.IsValid)
        {
            Console.Error.WriteLine($"minotaur: error: rule pack build failed ({result.Errors.Count} error(s)):");
            foreach (var error in result.Errors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return LabyrinthExitCodes.ConfigurationError;
        }

        var manifest = result.Manifest!;
        var packInfo = new FileInfo(output);
        Console.WriteLine(
            $"labyrinth pack: wrote {manifest.RuleCount} rule(s) for target grammar '{manifest.TargetGrammarName}' " +
            $"to '{output}' ({packInfo.Length} bytes, grammar sha256 {manifest.GrammarSha256[..12]}...).");
        return LabyrinthExitCodes.Success;
    }

    private static int Scan(Dictionary<string, string> options)
    {
        var rules = Required(options, "rules");
        var grammarName = Required(options, "grammar");
        var src = Required(options, "src");

        var grammarsRoot = ScanHost.FindGrammarsRoot();
        var target = ScanHost.ResolveTarget(grammarName);
        var grammarPath = ScanHost.GrammarFilePath(grammarsRoot, target);
        if (!File.Exists(grammarPath))
        {
            Console.Error.WriteLine($"minotaur: error: grammar file not found: {grammarPath}");
            return LabyrinthExitCodes.ConfigurationError;
        }

        var grammarContent = File.ReadAllText(grammarPath);

        // --rules may be a binary pack file (.labyrinthpack, issue #283) or a
        // directory of YAML rule files; a pack root with a per-grammar
        // subdirectory (Minotaur-Labyrinth layout) scans with just that subdir.
        List<CompiledLabyrinthRule>? compiledRules = null;
        LabyrinthRulePackResult? yamlPack = null;
        if (File.Exists(rules) && !Directory.Exists(rules))
        {
            var packed = LabyrinthRulePack.TryRead(
                rules, expectedTargetGrammarName: target.Directory,
                expectedGrammarSha256: LabyrinthRulePack.GrammarSha256(grammarContent));
            if (!packed.IsValid)
            {
                Console.Error.WriteLine($"minotaur: error: rule pack '{rules}' is invalid ({packed.Errors.Count} error(s)):");
                foreach (var error in packed.Errors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }

                return LabyrinthExitCodes.ConfigurationError;
            }

            compiledRules = packed.Rules.ToList();
        }
        else
        {
            var rulesDir = Directory.Exists(Path.Combine(rules, grammarName)) ? Path.Combine(rules, grammarName) : rules;
            yamlPack = new LabyrinthRulePackLoader().TryLoadDirectory(rulesDir);
            if (!yamlPack.IsValid)
            {
                return new LabyrinthConsoleReporter(Console.Error).ReportConfigurationError(yamlPack);
            }
        }

        var files = CollectFiles(src, grammarName, options);
        if (files.Count == 0)
        {
            Console.Error.WriteLine($"minotaur: error: no source files under '{src}'");
            return LabyrinthExitCodes.ConfigurationError;
        }

        var findings = new List<LabyrinthTaintFinding>();
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var graph = ScanHost.BuildGraph(source, file, grammarContent, target.Directory, out var nodes);
            findings.AddRange(compiledRules is not null
                ? ScanHost.RunCompiledRules(compiledRules, graph, nodes, file)
                : ScanHost.RunRules(yamlPack!.RuleSet, grammarContent, target.Directory, graph, nodes, file));
        }

        if (options.TryGetValue("write-baseline", out var writeBaseline))
        {
            LabyrinthBaseline.Save(writeBaseline, findings);
        }

        var reportable = (IReadOnlyList<LabyrinthTaintFinding>)findings;
        var suppressed = 0;
        if (options.TryGetValue("baseline", out var baselinePath))
        {
            reportable = LabyrinthBaseline.Load(baselinePath).Apply(findings, out suppressed);
        }

        if (options.TryGetValue("sarif", out var sarifPath))
        {
            var allRules = compiledRules is not null
                ? compiledRules.Select(c => c.Rule)
                : yamlPack!.RuleSet.Rules;
            var rulesById = allRules
                .GroupBy(r => r.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            File.WriteAllText(sarifPath, LabyrinthSarifWriter.Write(reportable, rulesById: rulesById));
        }

        return new LabyrinthConsoleReporter(Console.Out).Report(reportable, suppressed);
    }

    private static List<string> CollectFiles(string src, string grammarName, Dictionary<string, string> options)
    {
        string[] extensions;
        if (options.TryGetValue("ext", out var extOpt))
        {
            extensions = extOpt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e : "." + e)
                .ToArray();
        }
        else if (DefaultExtensions.TryGetValue(grammarName, out var defaults))
        {
            extensions = defaults;
        }
        else
        {
            extensions = Array.Empty<string>();
        }

        var files = new List<string>();
        if (File.Exists(src))
        {
            files.Add(src);
            return files;
        }

        foreach (var file in Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            if (extensions.Length == 0 || extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                files.Add(file);
            }
        }

        return files;
    }

    private static int ListGrammars()
    {
        foreach (var target in LabyrinthTargetGrammars.All)
        {
            Console.WriteLine(target.Directory);
        }

        return LabyrinthExitCodes.Success;
    }

    private static int Usage()
    {
        Console.WriteLine("""
            Minotaur CLI — Labyrinth SAST scanning.

            Usage:
              minotaur labyrinth scan --rules <dir|pack> --grammar <name> --src <file|dir>
                  [--ext .cs,.py] [--sarif out.sarif] [--baseline b.json] [--write-baseline b.json]
              minotaur labyrinth verify-corpus --rules <dir|pack> --tests <dir>
              minotaur labyrinth pack build --rules <dir> --grammar <name> --out <file> [--id <packId>]
              minotaur labyrinth grammars

            scan          Scan source files with a Labyrinth rule pack. --rules may be a pack
                          root with per-grammar subdirectories (then only rules/<grammar> is
                          used), a single pack directory, or a binary rule pack file built by
                          'pack build' (patterns pre-compiled; the pack is refused when the
                          target grammar content changed since it was built). Exit codes: 0 no
                          findings, 1 findings, 2 configuration error.
            verify-corpus Run the Minotaur-Labyrinth tests/ corpus through the engine and
                          check every case's expected.json (used by Minotaur-Labyrinth CI).
                          --rules may also be a binary rule pack file.
            pack build    Compile a YAML rule directory against the target grammar once and
                          write the compact binary rule pack (patterns parsed at build time;
                          attribution source/source-url/license of each rule is carried inside
                          and presented with every finding).
            grammars      List the Labyrinth target grammars.
            """);
        return LabyrinthExitCodes.Success;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            var eq = key.IndexOf('=');
            if (eq >= 0)
            {
                options[key[..eq]] = key[(eq + 1)..];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = args[i + 1];
                i++;
            }
            else
            {
                options[key] = "true";
            }
        }

        return options;
    }

    private static string Required(Dictionary<string, string> options, string key)
    {
        if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required option --{key}");
        }

        return value;
    }
}
