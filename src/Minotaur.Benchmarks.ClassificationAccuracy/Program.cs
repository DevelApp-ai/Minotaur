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

using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Minotaur.Projects;
using Minotaur.Projects.Grammar;
using Minotaur.Projects.Grammar.Detectors;

// ---------------------------------------------------------------------------
// Minotaur issue #133 harness: extension-guess classification accuracy on
// real-world polyglot repositories, vs a Linguist extension-map baseline.
//
// Corpus: four pinned public repositories (codeload tarballs, exact tags),
// covering genuinely mixed language trees:
//   redis/redis      8.0.2          (C, Tcl, Shell, Lua, Python, ...)
//   curl/curl        8.23.0        (C, Perl, Python, CMake, Roff, DCL, ...)
//   git/git          2.56.1         (C, Shell, Perl, Tcl, AsciiDoc, Meson, ...)
//   twbs/bootstrap   5.3.9          (SCSS, JS, TS, MDX, Astro, HTML, ...)
// plus github-linguist's languages.yml (pinned v9.7.0) as the baseline data.
//
// Ground truth: adjudicated per-file language labels. Extension-level rules
// (documented in the results) assign the label; genuinely ambiguous
// extensions (.h, .ts, .inc) and generated/container files are adjudicated
// by inspection (see RESULTS.md "Ground truth" section). Files without an
// honest single-language label (binary blobs, test fixtures, build-system
// templates, ignore files) are EXCLUDED from scoring and counted in the
// exclusions breakdown instead, so the accuracy denominators are explicit.
//
// Classifiers compared (identical corpus, same adjudicated truth):
//   minotaur-composite  GrammarDetectionManager as shipped (content-based
//                       regex detector + extension detector, min confidence
//                       0.5) - exactly the detection path ProjectLoader uses.
//   minotaur-extension  the FileExtensionGrammarDetector mapping alone
//                       (the shipped registry metadata, isolated).
//   linguist-extension  github-linguist languages.yml extension map alone
//                       (line-parsed; extension-only strategy, so a
//                       deterministic file-order tie-break picks top-1 when
//                       several languages claim an extension; set membership
//                       is reported alongside).
//
// Categories (per issue #133): standard / ambiguous / generated / container.
// Embedded-language detection at parse time (issue item 5) is NOT measured
// here; it is structural and runs at grammar-compile/parse time, not in the
// extension-based detection phase this harness exercises.
//
// Usage:
//   dotnet run --project src/Minotaur.Benchmarks.ClassificationAccuracy \
//       [-- <outputDir>]
// Default output: docs/measurements/issue-133 (results.json, RESULTS.md,
// accuracy.csv, classification-detail.csv). Env: MINOTAUR_COMMIT (recorded),
// CLASSACC_CACHE_DIR (corpus cache override; default a temp dir so repeated
// local runs do not re-download).
//
// Outcomes are recorded, never asserted.
// ---------------------------------------------------------------------------

var outputDir = args.Length > 0
    ? args[0]
    : Path.Combine("docs", "measurements", "issue-133");
Directory.CreateDirectory(outputDir);

var corpusRoot = Environment.GetEnvironmentVariable("CLASSACC_CACHE_DIR")
    ?? Path.Combine(Path.GetTempPath(), "minotaur-classacc-corpus");
Directory.CreateDirectory(corpusRoot);

Console.WriteLine("Minotaur issue #133 classification-accuracy harness");
Console.WriteLine($"  runtime: {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"  os:      {RuntimeInformation.OSDescription}");
Console.WriteLine($"  cpus:    {Environment.ProcessorCount}");
Console.WriteLine();

// --- corpus pins -----------------------------------------------------------
var repos = new (string Name, string Url)[]
{
    ("redis", "https://codeload.github.com/redis/redis/tar.gz/refs/tags/8.0.2"),
    ("curl", "https://codeload.github.com/curl/curl/tar.gz/refs/tags/curl-8_23_0"),
    ("git", "https://codeload.github.com/git/git/tar.gz/refs/tags/v2.56.1"),
    ("bootstrap", "https://codeload.github.com/twbs/bootstrap/tar.gz/refs/tags/v5.3.9"),
};
const string LinguistYamlUrl =
    "https://raw.githubusercontent.com/github-linguist/linguist/v9.7.0/lib/linguist/languages.yml";

// --- ground truth: extension rules (language label, category) -------------
// Adjudicated against this exact corpus (pinned tags above); samples are
// listed in RESULTS.md. Labels use Linguist language names so the baseline
// and the system under test share the same label space.
var extTruth = new Dictionary<string, (string Lang, string Cat)>(StringComparer.OrdinalIgnoreCase)
{
    // C family (all .h headers across the four repos are C headers - see
    // RESULTS.md adjudication; kept in the ambiguous category because the
    // extension itself has competing claimants in the wild).
    { ".c", ("C", "standard") },
    { ".cpp", ("C++", "standard") },
    { ".h", ("C", "ambiguous") },
    // Scripting
    { ".sh", ("Shell", "standard") },
    { ".bash", ("Shell", "standard") },
    { ".pl", ("Perl", "standard") },
    { ".pm", ("Perl", "standard") },
    { ".perl", ("Perl", "standard") },
    { ".py", ("Python", "standard") },
    { ".rb", ("Ruby", "standard") },
    { ".tcl", ("Tcl", "standard") },
    { ".lua", ("Lua", "standard") },
    { ".awk", ("Awk", "standard") },
    // .ts in this corpus: all TypeScript sources (bootstrap site config is
    // TS); the extension itself has competing claimants in the wild.
    { ".ts", ("TypeScript", "ambiguous") },
    { ".js", ("JavaScript", "standard") },
    { ".mjs", ("JavaScript", "standard") },
    // Web / markup
    { ".html", ("HTML", "container") },
    { ".htm", ("HTML", "container") },
    { ".md", ("Markdown", "container") },
    { ".mdx", ("MDX", "container") },
    { ".astro", ("Astro", "container") },
    { ".css", ("CSS", "standard") },
    { ".scss", ("SCSS", "standard") },
    { ".svg", ("SVG", "standard") },
    // Data / config
    { ".json", ("JSON", "standard") },
    { ".yml", ("YAML", "standard") },
    { ".yaml", ("YAML", "standard") },
    { ".xml", ("XML", "standard") },
    { ".vcxproj", ("XML", "standard") },
    { ".nuspec", ("XML", "standard") },
    { ".txt", ("Text", "standard") },
    // Build
    { ".cmake", ("CMake", "standard") },
    { ".m4", ("M4Sugar", "standard") },
    { ".build", ("Meson", "standard") },
    { ".mak", ("Makefile", "standard") },
    { ".mk", ("Makefile", "standard") },
    { ".am", ("Makefile", "standard") },
    { ".ac", ("M4Sugar", "standard") },
    { ".com", ("DIGITAL Command Language", "standard") },
    { ".inc", ("Makefile", "ambiguous") }, // curl Makefile.inc fragments; .inc is multi-claim in the wild
    { ".po", ("Gettext Catalog", "standard") },
    { ".adoc", ("AsciiDoc", "standard") },
    { ".cocci", ("SmPL", "standard") },
    { ".rs", ("Rust", "standard") },
    { ".hpp", ("C++", "standard") },
    { ".t", ("Perl", "standard") }, // git/perl test scripts
    { ".xsl", ("XSLT", "standard") },
    { ".toml", ("TOML", "standard") },
    { ".ps1", ("PowerShell", "standard") },
    { ".bat", ("Batchfile", "standard") },
    { ".cmd", ("Batchfile", "standard") },
    { ".el", ("Emacs Lisp", "standard") },
    { ".cjs", ("JavaScript", "standard") },
    { ".zsh", ("Shell", "standard") },
    { ".tcsh", ("Shell", "standard") },
    { ".csh", ("Shell", "standard") },
    { ".ksh", ("Shell", "standard") },
    { ".markdown", ("Markdown", "container") },
    { ".pot", ("Gettext Catalog", "standard") },
    { ".sln", ("XML", "standard") },
    { ".targets", ("XML", "standard") },
    { ".manifest", ("XML", "standard") },
    { ".xhtml", ("HTML", "container") },
    { ".1", ("Roff", "standard") }, // man pages (seeper-path overrides for the exceptions)
    { ".2", ("Roff", "standard") },
    { ".3", ("Roff", "standard") },
    { ".4", ("Roff", "standard") },
    { ".5", ("Roff", "standard") },
    { ".6", ("Roff", "standard") },
    { ".7", ("Roff", "standard") },
    { ".8", ("Roff", "standard") },
    { ".9", ("Roff", "standard") },
};

// Per-path adjudication overrides for files whose extension rule would be
// wrong (adjudicated by inspection on this pinned corpus): exact
// "repo:relative/path" -> language + category, or exclusion reason.
var pathOverrides = new Dictionary<string, (string? Lang, string Cat, string? Excluded)>()
{
    { "git:LGPL-2.1", ("Text", "standard", null) },
    { "curl:tests/data/data1705-stdout.1", (null, "test-fixture", "test-fixture") },
    { "git:.git-blame-ignore-revs", (null, "vcs-config", "vcs-config") },
};

// Generated-file markers: category overrides the rule above when a file is
// generated. Source maps are JSON (generated); minified bundles are
// JavaScript (generated).
var generatedMarkers = new (Func<string, bool> Match, string Lang)[]
{
    (p => p.EndsWith(".map", StringComparison.OrdinalIgnoreCase), "JSON"),
    (p => p.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase), "JavaScript"),
    (p => p.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase), "CSS"),
};

// Exclusions: files excluded from scoring, with an explicit reason (so the
// denominators stay honest). Extensions with no single honest language label.
var excludedExts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    // binaries / images / fonts
    { ".png", "binary" }, { ".jpg", "binary" }, { ".jpeg", "binary" },
    { ".gif", "binary" }, { ".ico", "binary" }, { ".webp", "binary" },
    { ".bin", "binary" }, { ".rdb", "binary" }, { ".woff", "binary" },
    { ".woff2", "binary" }, { ".ttf", "binary" }, { ".eot", "binary" },
    // vcs / tool config
    { ".gitignore", "vcs-config" }, { ".gitattributes", "vcs-config" },
    { ".editorconfig", "tool-config" }, { ".npmignore", "tool-config" },
    { ".browserslistrc", "tool-config" }, { ".stylelintignore", "tool-config" },
    { ".lock", "lockfile" }, { ".pom", "lockfile" },
    // build templates / test fixtures / data without a language identity
    { ".in", "build-template" }, { ".conf", "no-single-label" },
    { ".expect", "test-fixture" }, { ".test", "test-fixture" },
    { ".side", "test-fixture" }, { ".patch", "test-fixture" },
    { ".diff", "test-fixture" }, { ".checksrc", "no-single-label" },
    { ".properties", "no-single-label" }, { ".ini", "no-single-label" },
    { ".filters", "build-template" }, { ".example", "build-template" },
    { ".cfg", "no-single-label" }, { ".clang-format", "tool-config" },
    { ".gitmodules", "no-single-label" }, { ".netrc", "no-single-label" },
    { ".gpg", "binary" }, { ".p12", "binary" }, { ".pdf", "binary" },
    { ".zip", "binary" }, { ".tar", "binary" },
    { ".mbox", "no-single-label" }, { ".mboxrd", "no-single-label" },
    { ".midx", "binary" }, { ".dump", "test-fixture" },
    { ".sample", "build-template" }, { ".res", "no-single-label" },
    { ".key", "no-single-label" }, { ".cnf", "no-single-label" },
    { ".prm", "no-single-label" }, { ".service", "no-single-label" },
    { ".idx", "no-single-label" }, { ".soname", "no-single-label" },
    { ".words", "no-single-label" }, { ".rc", "no-single-label" },
    { ".msg", "no-single-label" }, { ".dev", "no-single-label" },
    { ".uname", "no-single-label" }, { ".acl", "no-single-label" },
    { ".aof", "test-fixture" }, { ".tpl", "no-single-label" },
    { ".sdl", "no-single-label" }, { ".wrap", "no-single-label" },
    { ".export", "test-fixture" }, { ".codespellrc", "tool-config" },
    { ".eslintignore", "tool-config" }, { ".prettierignore", "tool-config" },
    { ".nvmrc", "tool-config" }, { ".guess", "build-template" },
    { ".sub", "test-fixture" }, { ".opt", "no-single-label" },
    { ".mailmap", "no-single-label" }, { ".b4-config", "tool-config" },
    { ".b4-cover-template", "no-single-label" }, { ".curl", "tool-config" },
};

// Minotaur grammar id -> Linguist language name (label-space normalization).
var grammarToLanguage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "CSharp10.grammar", "C#" },
    { "VisualBasic.grammar", "Visual Basic .NET" },
    { "FSharp.grammar", "F#" },
    { "HTMLEmbedded.grammar", "HTML" },
    { "XAML.grammar", "XAML" },
    { "JSON.grammar", "JSON" },
    { "XML.grammar", "XML" },
    { "JavaScript.grammar", "JavaScript" },
    { "JavaScriptES2022.grammar", "JavaScript" },
    { "TypeScript.grammar", "TypeScript" },
    { "Python311.grammar", "Python" },
    { "Java17.grammar", "Java" },
    { "Cpp20.grammar", "C++" },
    { "C17.grammar", "C" },
    { "Rust2021.grammar", "Rust" },
    { "Go119.grammar", "Go" },
    { "CSS.grammar", "CSS" },
    { "SQL2023.grammar", "SQL" },
};

// --- 1. fetch + extract the corpus -----------------------------------------
using var http = new HttpClient();
http.Timeout = TimeSpan.FromMinutes(10);

var repoRoots = new List<(string Repo, string Root, int Files)>();
foreach (var (name, url) in repos)
{
    var root = Path.Combine(corpusRoot, name);
    var marker = Path.Combine(corpusRoot, name + ".done");
    if (!File.Exists(marker))
    {
        Console.WriteLine($"downloading corpus: {name} ...");
        var tgz = Path.Combine(corpusRoot, name + ".tgz");
        if (!File.Exists(tgz))
        {
            await DownloadWithRetryAsync(http, url, tgz);
        }

        Directory.CreateDirectory(root);
        await using var fs = File.OpenRead(tgz);
        await using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var tar = new TarReader(gz);
        while (await tar.GetNextEntryAsync() is { } entry)
        {
            if (entry.EntryType is TarEntryType.RegularFile && !string.IsNullOrEmpty(entry.Name))
            {
                var dest = Path.GetFullPath(Path.Combine(root, entry.Name));
                // Defend against path traversal outside the corpus dir.
                if (!dest.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar))
                {
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, overwrite: true);
            }
        }

        File.WriteAllText(marker, url);
        Console.WriteLine($"  extracted to {root}");
    }

    // codeload tarballs contain a single top-level directory: strip it.
    var inner = Directory.GetDirectories(root).Single();
    var files = Directory.GetFiles(inner, "*", SearchOption.AllDirectories);
    repoRoots.Add((name, inner, files.Length));
    Console.WriteLine($"  {name}: {files.Length} files");
}

// --- 2. linguist extension baseline -----------------------------------------
Console.WriteLine("parsing linguist languages.yml (extension-only baseline) ...");
var yamlPath = Path.Combine(corpusRoot, "languages-" + LinguistYamlUrl[(LinguistYamlUrl.LastIndexOf('/') + 1)..] + ".yml");
if (!File.Exists(yamlPath))
{
    await DownloadWithRetryAsync(http, LinguistYamlUrl, yamlPath);
}
var linguistExtMap = ParseLinguistExtensions(yamlPath);
Console.WriteLine($"  {linguistExtMap.Count} extensions mapped");

// --- 3. enumerate + label + classify ----------------------------------------
Console.WriteLine("classifying corpus ...");
var compositeManager = new GrammarDetectionManager();
var extensionOnlyManager = new GrammarDetectionManager(
    new CompositeGrammarDetector(
        new IGrammarDetector[] { new FileExtensionGrammarDetector() },
        requireConsensus: false,
        minimumConfidence: 0.5));

var detail = new List<DetailRow>();
var excluded = new Dictionary<string, int>();
var unlabelledExt = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

foreach (var (repo, root, _) in repoRoots)
{
    var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToList();
    foreach (var file in files)
    {
        var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
        var ext = Path.GetExtension(file);

        string truth;
        string cat;
        if (string.IsNullOrEmpty(ext))
        {
            excluded["extensionless"] = excluded.GetValueOrDefault("extensionless") + 1;
            continue;
        }

        // Per-path adjudication overrides (see table above the corpus loop).
        if (pathOverrides.TryGetValue(repo + ":" + rel, out var over))
        {
            if (over.Excluded != null)
            {
                excluded[over.Excluded + ":" + rel] = excluded.GetValueOrDefault(over.Excluded + ":" + rel) + 1;
                continue;
            }
            truth = over.Lang!;
            cat = over.Cat;
        }
        else
        {
            if (excludedExts.TryGetValue(ext, out var reason))
            {
                excluded[reason + ":" + ext] = excluded.GetValueOrDefault(reason + ":" + ext) + 1;
                continue;
            }

            // Generated markers first: a generated file owns its category even
            // when its extension also has a plain rule (.map would otherwise
            // fall through to the unlabelled branch, which it is not).
            truth = "";
            cat = "generated";
            var generated = false;
            foreach (var (match, lang) in generatedMarkers)
            {
                if (match(file))
                {
                    truth = lang;
                    generated = true;
                    break;
                }
            }
            if (!generated)
            {
                if (!extTruth.TryGetValue(ext, out var rule))
                {
                    unlabelledExt[ext] = unlabelledExt.GetValueOrDefault(ext) + 1;
                    continue;
                }
                (truth, cat) = (rule.Lang, rule.Cat);
            }
        }

        // System under test: full pipeline (as ProjectLoader uses it).
        var comp = await compositeManager.DetectGrammarAsync(file, root, ProjectType.GenericFolder);
        var compPred = comp.IsSuccessful && comp.GrammarName != null
            ? grammarToLanguage.GetValueOrDefault(comp.GrammarName, comp.GrammarName)
            : "(none)";

        // Extension-only registry, isolated.
        var extRes = await extensionOnlyManager.DetectGrammarAsync(file, root, ProjectType.GenericFolder);
        var extPred = extRes.IsSuccessful && extRes.GrammarName != null
            ? grammarToLanguage.GetValueOrDefault(extRes.GrammarName, extRes.GrammarName)
            : "(none)";

        // Linguist extension map: top-1 by first-claim order in languages.yml,
        // plus the full candidate set (set membership is the honest metric for
        // a mapping that several languages claim).
        var linguistTop = "(none)";
        var linguistSet = Array.Empty<string>();
        if (linguistExtMap.TryGetValue(ext, out var cands) && cands.Count > 0)
        {
            linguistSet = cands.ToArray();
            linguistTop = cands[0];
        }

        // Set membership for the Minotaur systems: prediction plus fallback
        // grammars (both normalized to language labels).
        var compSet = comp.IsSuccessful
            ? new[] { compPred }
                .Concat((comp.FallbackGrammars ?? Array.Empty<string>())
                    .Select(g => grammarToLanguage.GetValueOrDefault(g, g)))
                .Distinct()
                .ToArray()
            : Array.Empty<string>();
        var extSet = extRes.IsSuccessful
            ? new[] { extPred }
                .Concat((extRes.FallbackGrammars ?? Array.Empty<string>())
                    .Select(g => grammarToLanguage.GetValueOrDefault(g, g)))
                .Distinct()
                .ToArray()
            : Array.Empty<string>();

        detail.Add(new DetailRow(repo, rel, ext.ToLowerInvariant(), cat, truth,
            compPred, extPred, linguistTop,
            Outcome(compPred, truth), Outcome(extPred, truth), Outcome(linguistTop, truth),
            compSet.Contains(truth), extSet.Contains(truth), linguistSet.Contains(truth)));
    }
}

// --- 4. aggregate -----------------------------------------------------------
var categories = new[] { "standard", "ambiguous", "generated", "container" };
var systemSelectors = new Dictionary<string, Func<DetailRow, string>>
{
    { "minotaur-composite", d => d.Comp },
    { "minotaur-extension", d => d.Ext1 },
    { "linguist-extension", d => d.Ling },
};

var systemSetSelectors = new Dictionary<string, Func<DetailRow, bool>>
{
    { "minotaur-composite", d => d.InSetComp },
    { "minotaur-extension", d => d.InSetExt },
    { "linguist-extension", d => d.InSetLing },
};

var accuracyRows = new List<(string System, string Cat, int N, int Correct, int Wrong, int Unclassified, int SetHit)>();
foreach (var (system, select) in systemSelectors)
{
    accuracyRows.Add(Aggregate(system, "all", detail, select, systemSetSelectors[system]));
    foreach (var cat in categories)
    {
        accuracyRows.Add(Aggregate(system, cat, detail.Where(d => d.Cat == cat).ToList(), select, systemSetSelectors[system]));
    }
}

Console.WriteLine();
Console.WriteLine($"labelled corpus: {detail.Count} scored files");
foreach (var row in accuracyRows)
{
    var acc = row.N == 0 ? 0d : (double)row.Correct / row.N;
    var setAcc = row.N == 0 ? 0d : (double)row.SetHit / row.N;
    Console.WriteLine($"  {row.System,-20} {row.Cat,-10} n={row.N,-5} correct={row.Correct,-5} " +
                      $"wrong={row.Wrong,-4} unclassified={row.Unclassified,-5} accuracy={acc:P1} set-hit={setAcc:P1}");
}
Console.WriteLine($"excluded files: {excluded.Values.Sum()}");
Console.WriteLine();

// --- 5. write outputs --------------------------------------------------------
var run = new JsonObject
{
    ["harvestedAt"] = DateTimeOffset.UtcNow.ToString("u"),
    ["commit"] = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "(unrecorded)",
    ["corpus"] = new JsonArray(
        repos.Select(r => new JsonObject
        {
            ["repo"] = r.Name,
            ["tarball"] = r.Url,
            ["files"] = repoRoots.First(x => x.Repo == r.Name).Files,
        }).ToArray()),
    ["linguistData"] = LinguistYamlUrl,
    ["labelledFiles"] = detail.Count,
    ["excludedFiles"] = excluded.Sum(kv => kv.Value),
    ["excluded"] = new JsonObject(excluded.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, (JsonNode?)kv.Value))),
    ["unlabelledExtensions"] = new JsonObject(unlabelledExt.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, (JsonNode?)kv.Value))),
    ["categories"] = new JsonArray(categories.Select(c => (JsonNode)c).ToArray()),
    ["systems"] = new JsonArray(systemSelectors.Keys.Select(k => (JsonNode)k).ToArray()),
};

var results = new JsonObject
{
    ["run"] = run,
    ["accuracy"] = new JsonArray(accuracyRows.Select(row => new JsonObject
    {
        ["system"] = row.System,
        ["category"] = row.Cat,
        ["n"] = row.N,
        ["correct"] = row.Correct,
        ["wrong"] = row.Wrong,
        ["unclassified"] = row.Unclassified,
        ["coverage"] = row.N == 0 ? 0 : Math.Round((double)(row.Correct + row.Wrong) / row.N, 4),
        ["top1Accuracy"] = row.N == 0 ? 0 : Math.Round((double)row.Correct / row.N, 4),
        ["answeredAccuracy"] = row.Correct + row.Wrong == 0 ? 0 : Math.Round((double)row.Correct / (row.Correct + row.Wrong), 4),
        ["setHitAccuracy"] = row.N == 0 ? 0 : Math.Round((double)row.SetHit / row.N, 4),
    }).ToArray()),
};
results["errorTaxonomy"] = new JsonObject
{
    ["minotaur-extension-unclassifiedByExtension"] = Taxonomy(detail, d => d.OutcomeExt == "unclassified", d => d.Ext),
    ["minotaur-extension-wrongPairs"] = Taxonomy(detail, d => d.OutcomeExt == "wrong", d => $"{d.Truth} -> {d.Ext1}"),
    ["minotaur-composite-unclassifiedByExtension"] = Taxonomy(detail, d => d.OutcomeComp == "unclassified", d => d.Ext),
    ["minotaur-composite-wrongPairs"] = Taxonomy(detail, d => d.OutcomeComp == "wrong", d => $"{d.Truth} -> {d.Comp}"),
    ["minotaur-composite-set-vs-extension"] = Taxonomy(
        detail,
        d => d.OutcomeComp != d.OutcomeExt,
        d => $"composite={d.OutcomeComp} extension={d.OutcomeExt}"),
    ["linguist-extension-unclassifiedByExtension"] = Taxonomy(detail, d => d.OutcomeLing == "unclassified", d => d.Ext),
    ["linguist-extension-wrongPairs"] = Taxonomy(detail, d => d.OutcomeLing == "wrong", d => $"{d.Truth} -> {d.Ling}"),
};
var resultsBytes = JsonSerializer.SerializeToUtf8Bytes(results, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(Path.Combine(outputDir, "results.json"), System.Text.Encoding.UTF8.GetString(resultsBytes));

await File.WriteAllTextAsync(Path.Combine(outputDir, "accuracy.csv"),
    "system,category,n,correct,wrong,unclassified,set_hit,coverage,top1_accuracy,answered_accuracy\n" +
    string.Join("\n", accuracyRows.Select(row =>
        $"{row.System},{row.Cat},{row.N},{row.Correct},{row.Wrong},{row.Unclassified},{row.SetHit}," +
        $"{(row.N == 0 ? 0 : Math.Round((double)(row.Correct + row.Wrong) / row.N, 4))}," +
        $"{(row.N == 0 ? 0 : Math.Round((double)row.Correct / row.N, 4))}," +
        $"{(row.Correct + row.Wrong == 0 ? 0 : Math.Round((double)row.Correct / (row.Correct + row.Wrong), 4))}")));

await File.WriteAllTextAsync(Path.Combine(outputDir, "classification-detail.csv"),
    "repo,path,extension,category,truth,minotaur_composite,minotaur_extension,linguist_extension," +
    "outcome_composite,outcome_extension,outcome_linguist,in_set_composite,in_set_extension,in_set_linguist\n" +
    string.Join("\n", detail.Select(d =>
        $"{d.Repo},{Quote(d.Path)},{d.Ext},{d.Cat},{Quote(d.Truth)},{Quote(d.Comp)},{Quote(d.Ext1)},{Quote(d.Ling)}," +
        $"{d.OutcomeComp},{d.OutcomeExt},{d.OutcomeLing},{d.InSetComp},{d.InSetExt},{d.InSetLing}")));

await WriteResultsMdAsync(outputDir, run, results, accuracyRows, detail, categories);

Console.WriteLine($"wrote {outputDir}/results.json, accuracy.csv, classification-detail.csv, RESULTS.md");
Console.WriteLine("done.");

// --- local functions --------------------------------------------------------
static string Outcome(string pred, string truth) =>
    pred == truth ? "correct" : pred == "(none)" ? "unclassified" : "wrong";

static string Quote(string value) =>
    value.Contains(',', StringComparison.Ordinal) ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : value;

static (string System, string Cat, int N, int Correct, int Wrong, int Unclassified, int SetHit) Aggregate(
    string system, string cat, List<DetailRow> rows, Func<DetailRow, string> select, Func<DetailRow, bool> inSet)
{
    var correct = 0; var wrong = 0; var unclassified = 0; var setHit = 0;
    foreach (var row in rows)
    {
        var pred = select(row);
        if (pred == row.Truth) correct++;
        else if (pred == "(none)") unclassified++;
        else wrong++;
        if (inSet(row)) setHit++;
    }
    return (system, cat, rows.Count, correct, wrong, unclassified, setHit);
}

static JsonArray Taxonomy(List<DetailRow> rows, Func<DetailRow, bool> match, Func<DetailRow, string> key) =>
    new(rows.Where(match)
        .GroupBy(key)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => g.Key, StringComparer.Ordinal)
        .Take(15)
        .Select(g => (JsonNode)new JsonObject { ["key"] = g.Key, ["count"] = g.Count() })
        .ToArray());

static async Task WriteResultsMdAsync(
    string outputDir, JsonObject run, JsonObject results, List<(string System, string Cat, int N, int Correct, int Wrong, int Unclassified, int SetHit)> accuracyRows,
    List<DetailRow> detail, string[] categories)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# Issue #133 — extension-based classification accuracy on real-world polyglot repositories");
    sb.AppendLine();
    sb.AppendLine($"Measured {run["labelledFiles"]} scored files (plus {run["excludedFiles"]} excluded " +
                  "files, excluded only with an explicit reason recorded in results.json).");
    sb.AppendLine();
    sb.AppendLine("## Corpus");
    sb.AppendLine();
    sb.AppendLine("| repo | pin |");
    sb.AppendLine("|---|---|");
    foreach (var repoNode in (JsonArray)run["corpus"]!)
    {
        var o = (JsonObject)repoNode!;
        sb.AppendLine($"| {o["repo"]} | {((string)o["tarball"]!).Replace("https://codeload.github.com/", "").Replace("/tar.gz/refs/tags/", " @ ")} ({o["files"]} files) |");
    }
    sb.AppendLine();
    sb.AppendLine($"Linguist baseline data: [languages.yml @ v9.7.0]({(string)run["linguistData"]!}), extension map only.");
    sb.AppendLine();
    sb.AppendLine("## Ground truth");
    sb.AppendLine();
    sb.AppendLine("Per-file language labels adjudicated against this exact corpus (labels use");
    sb.AppendLine("Linguist language names so both systems share the label space). Extension rules");
    sb.AppendLine("are recorded in the harness source (`src/Minotaur.Benchmarks.ClassificationAccuracy/Program.cs`);");
    sb.AppendLine("adjudicated rulings on the genuinely ambiguous cases:");
    sb.AppendLine();
    sb.AppendLine("- `.h` — every `.h` header across the four repos is a C header (redis, curl, git, and no C++-only headers sampled): truth `C`, category `ambiguous`.");
    sb.AppendLine("- `.ts` — all bootstrap `.ts` files are TypeScript source: truth `TypeScript`, category `ambiguous`.");
    sb.AppendLine("- `.inc` — curl's `Makefile.inc` fragments are Makefile: truth `Makefile`, category `ambiguous`.");
    sb.AppendLine("- `.map` — source maps under `js/dist/`: truth `JSON`, category `generated`.");
    sb.AppendLine("- Excluded (never scored): binaries/images, VCS/tool config, lockfiles, build templates (`.in`), test fixtures (`.expect`, `.test`, `.side`, `.patch`, `.diff`), extensionless files, and extensions without an honest single-language label. Full counts in `results.json`.");
    sb.AppendLine();
    sb.AppendLine("## Accuracy");
    sb.AppendLine();
    sb.AppendLine("Outcome per (system, file): top-1 prediction equals the adjudicated truth (`correct`),");
    sb.AppendLine("prediction exists but differs (`wrong`), or the system has no mapping entry at");
    sb.AppendLine("all (`unclassified`; counted against accuracy and coverage).");
    sb.AppendLine();
    sb.AppendLine("Set-hit accuracy additionally counts the ground-truth language appearing anywhere");
    sb.AppendLine("among the system's candidate set (fallback grammars for the Minotaur systems, all");
    sb.AppendLine("claiming languages for Linguist) even when top-1 differs.");
    sb.AppendLine();
    foreach (var cat in categories.Append("all"))
    {
        sb.AppendLine($"### Category: {cat}");
        sb.AppendLine();
        sb.AppendLine("| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var row in accuracyRows.Where(r => r.Cat == cat))
        {
            var n = row.N;
            var cov = n == 0 ? 0 : (double)(row.Correct + row.Wrong) / n;
            var acc = n == 0 ? 0 : (double)row.Correct / n;
            var setHit = n == 0 ? 0 : (double)row.SetHit / n;
            var answered = row.Correct + row.Wrong == 0 ? 0 : (double)row.Correct / (row.Correct + row.Wrong);
            sb.AppendLine($"| {row.System} | {n} | {row.Correct} | {row.Wrong} | {row.Unclassified} | {acc:P1} | {setHit:P1} | {answered:P1} | {cov:P1} |");
        }
        sb.AppendLine();
    }
    sb.AppendLine("## Error taxonomy (top entries)");
    sb.AppendLine();
    var taxonomy = (JsonObject)results["errorTaxonomy"]!;
    foreach (var prop in taxonomy)
    {
        sb.AppendLine($"### {prop.Key}");
        sb.AppendLine();
        sb.AppendLine("| key | count |");
        sb.AppendLine("|---|---|");
        foreach (var entryNode in (JsonArray)prop.Value!)
        {
            var entry = (JsonObject)entryNode!;
            sb.AppendLine($"| {(string)entry["key"]!} | {entry["count"]} |");
        }
        sb.AppendLine();
    }
    sb.AppendLine("## Limitations / scope");
    sb.AppendLine();
    sb.AppendLine("- This harness measures the extension/detection phase only. Issue-item 5");
    sb.AppendLine("  (embedded-language detection, structural, at parse time) is not measured here.");
    sb.AppendLine("- The Linguist column is the extension map of languages.yml alone (no heuristics,");
    sb.AppendLine("  no content classifier). Top-1 on a multi-claim extension uses first-claim order");
    sb.AppendLine("  in the file — an extension-only strategy is genuinely underspecified there, which");
    sb.AppendLine("  is itself a measured finding.");
    sb.AppendLine("- Corpus gap: `.m` and `.v` ambiguity (issue text) does not occur in these four");
    sb.AppendLine("  repos; the ambiguous category is carried by `.h`, `.ts`, `.inc`.");
    sb.AppendLine("- Timings/values are recorded, never asserted; reruns on the pinned corpus are");
    sb.AppendLine("  deterministic.");
    sb.AppendLine();
    await File.WriteAllTextAsync(Path.Combine(outputDir, "RESULTS.md"), sb.ToString());
}

static async Task DownloadWithRetryAsync(HttpClient client, string url, string dest)
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            await using var s = await resp.Content.ReadAsStreamAsync();
            await using var outFs = File.Create(dest);
            await s.CopyToAsync(outFs);
            return;
        }
        catch (Exception) when (attempt < 4)
        {
            await Task.Delay(TimeSpan.FromSeconds(5 * attempt));
        }
    }
}

static Dictionary<string, List<string>> ParseLinguistExtensions(string yamlPath)
{
    var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    var currentLanguage = "";
    var inExtensions = false;
    foreach (var raw in File.ReadLines(yamlPath))
    {
        var line = raw.TrimEnd();
        if (line.Length == 0)
        {
            continue;
        }
        if (line[0] != ' ' && line[0] != '\t')
        {
            inExtensions = false;
            currentLanguage = line.TrimEnd(':');
            continue;
        }
        if (line.StartsWith("  extensions:", StringComparison.Ordinal))
        {
            inExtensions = true;
            continue;
        }
        if (line.Length > 2 && line[0] == ' ' && line[1] == ' ' && (line.Length <= 3 || (line[2] != ' ' && line[2] != '-')))
        {
            inExtensions = false; // new top-level property of the language
            continue;
        }
        if (inExtensions)
        {
            var item = line.Trim();
            if (item.StartsWith("- "))
            {
                var ext = item[2..].Trim('"');
                if (!map.TryGetValue(ext, out var list))
                {
                    map[ext] = list = new List<string>();
                }
                if (!list.Contains(currentLanguage))
                {
                    list.Add(currentLanguage);
                }
            }
        }
    }
    return map;
}

// Detail row recorded per scored corpus file (also the classification-detail.csv shape).
record DetailRow(
    string Repo, string Path, string Ext, string Cat, string Truth,
    string Comp, string Ext1, string Ling,
    string OutcomeComp, string OutcomeExt, string OutcomeLing,
    bool InSetComp, bool InSetExt, bool InSetLing);
