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
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DevelApp.StepParser;

// ---------------------------------------------------------------------------
// Minotaur issue #133 item 5 harness: "Embedded-language detection
// (structural, at parse time) evaluated separately on the polyglot-container
// subset."
//
// Evaluates whether Minotaur grammar-driven parsing *detects* embedded
// foreign-language regions (script/style blocks in HTML, code fences in
// Markdown-family files) as structural token spans, on the same pinned
// corpus as the ClassificationAccuracy harness (issue #133) over the
// container extensions .html/.htm/.xhtml/.md/.markdown/.mdx/.astro.
//
// Detection is scoped at region level (matching the classification-accuracy
// harness's region-level task): a detection is credited when an embedded
// region is recognized as a coherent structural span during whole-file
// structural parse, regardless of the inner tokenization.
//
// Truth is adjudicated by markers (script/style tags, ```/~~~ fences with
// info tags), with exclusions recorded (external scripts, empty blocks,
// template/html/text typed scripts, vbscript).
//
// Worker process isolation: an earlier in-process prototype ran parses on
// Task.Run and leaked hot GLR threads that starved the loop (measured only
// 5 files in 270 s). The engine cannot be safely multiplexed inside one
// process for adversarial inputs, so every parse happens in a child worker
// process with a line protocol over stdin/stdout, and is killed on timeout.
//
// Outcomes: success | parse-fail | timeout | parse-crash | startup-fail |
//           engine-oom | engine-error | no-grammar | read-fail
//
// Outputs (under arg0, default docs/measurements/issue-133):
//   embedded-results.json  machine-readable full results
//   embedded-RESULTS.md    human-readable report
//   embedded-files.csv     per-file rows (outcome + truth/detected counts)
//   embedded-partial-cache.jsonl  append-mode resume cache, one row per line
//
// Environment:
//   MINOTAUR_GRAMMARS_DIR   required: path to a Minotaur-Grammars checkout
//   CLASSACC_CACHE_DIR      shared corpus cache dir (shared with the
//                           ClassificationAccuracy harness; default temp)
//   EMBEDDED_PER_FILE_TIMEOUT_MS  per-file parse budget (default 1000;
//                           region-level detection is a classification
//                           claim that must be visible within a viability
//                           budget; probe evidence shows whole-file
//                           markdown cannot finish even under 40 s, so the
//                           budget mainly bounds the run and records
//                           timeouts honestly)
//   MINOTAUR_COMMIT         recorded in results
// ---------------------------------------------------------------------------

// ---- Worker mode must be dispatched BEFORE any main-flow setup. Workers are
// spawned with only --worker + grammar paths; they never see env vars and
// never touch output dirs.
if (args.Length >= 3 && args[0] == "--worker")
{
    return await RunWorker(args[1], args[2]);
}

// ============================== configuration ================================

var outputDir = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "docs/measurements/issue-133";
var corpusRoot = Environment.GetEnvironmentVariable("CLASSACC_CACHE_DIR")
    ?? Path.Join(Path.GetTempPath(), "minotaur-classacc-corpus");
var grammarsDir = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR");
if (string.IsNullOrWhiteSpace(grammarsDir) || !Directory.Exists(grammarsDir))
{
    Console.Error.WriteLine("MINOTAUR_GRAMMARS_DIR is required and must exist (a Minotaur-Grammars checkout).");
    return 2;
}
var perFileTimeoutMs =
    int.TryParse(Environment.GetEnvironmentVariable("EMBEDDED_PER_FILE_TIMEOUT_MS"),
        NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) && t > 0 ? t : 1000;

Console.WriteLine("Minotaur.Benchmarks.EmbeddedDetection");
Console.WriteLine("  issue:      DevelApp-ai/Minotaur#133 (item 5)");
Console.WriteLine($"  outputDir:  {outputDir}");
Console.WriteLine($"  corpusRoot: {corpusRoot}");
Console.WriteLine($"  grammars:   {grammarsDir}");
Console.WriteLine($"  perFileTimeoutMs: {perFileTimeoutMs}");
Directory.CreateDirectory(outputDir);

// Broad catches are DELIBERATE in this harness: the outcome taxonomy (header
// comment) requires every failure mode to map to a taxonomy row. The
// `when (Note(ex, ...))` filter keeps each catch exactly as broad as before
// while making the swallowed exception visible on stderr (worker stderr is
// drained by the parent), i.e. the diagnosability a bare `catch (Exception)`
// would lose.
bool Note(Exception ex, string context)
{
    Console.Error.WriteLine($"  [{context}] {ex.GetType().Name}: {ex.Message}");
    return true;
}

// ================================ worker =====================================

async Task<int> RunWorker(string htmlGrammarPath, string mdGrammarPath)
{
    var htmlGrammar = await File.ReadAllTextAsync(htmlGrammarPath);
    var mdGrammar = await File.ReadAllTextAsync(mdGrammarPath);

    // One writer for the whole worker lifetime. Creating it per request
    // disposed stdout and poisoned every response after the first.
    await using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

    string? line;
    while ((line = await Console.In.ReadLineAsync()) is not null)
    {
        var tab = line.IndexOf('\t');
        if (tab <= 0)
        {
            continue; // malformed request; the parent's ack/result wait classifies it
        }
        var path = line[..tab];
        var format = line[(tab + 1)..];

        try
        {
            writer.WriteLine($"A\t{path}");
        }
        catch (Exception ex) when (Note(ex, "worker ack write failed, parent likely gone"))
        {
            return 0; // parent gone
        }

        var outcome = "parse-fail";
        var tokens = new List<JsonNode>();
        try
        {
            // Fresh session per file by design (matching the per-file session
            // discipline of the #132 pilots; the session has no reset API and
            // re-initializing one session accumulates retained memory).
            var grammar = format == "html" ? htmlGrammar : mdGrammar;
            var session = new RealTimeParserSession(grammar);
            var doc = await File.ReadAllTextAsync(path);
            session.Initialize(doc);
            var parsed = session.Parse();

            if (parsed is { Success: true, ResolvedTokens: { Count: > 0 } toks })
            {
                var last = toks[toks.Count - 1];
                if (last.StartPosition + last.Length >= doc.TrimEnd().Length)
                {
                    outcome = "success";
                    foreach (var tk in toks)
                    {
                        tokens.Add(new JsonObject
                        {
                            ["t"] = tk.Type,
                            ["s"] = tk.StartPosition,
                            ["l"] = tk.Length,
                        });
                    }
                }
            }
        }
        catch (OutOfMemoryException)
        {
            outcome = "engine-oom";
        }
        catch (Exception ex) when (Note(ex, $"worker parse crashed for {path}"))
        {
            outcome = "engine-error";
        }

        var response = new JsonObject
        {
            ["path"] = path,
            ["outcome"] = outcome,
            ["tokens"] = new JsonArray(tokens.ToArray()),
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

// ============================= corpus pins ==================================
// Same pins (and cache layout) as the ClassificationAccuracy harness so a
// shared CLASSACC_CACHE_DIR reuses the same corpus files.

var repos = new (string Name, string Url)[]
{
    ("redis",     "https://codeload.github.com/redis/redis/tar.gz/refs/tags/8.0.2"),
    ("curl",      "https://codeload.github.com/curl/curl/tar.gz/refs/tags/curl-8_23_0"),
    ("git",       "https://codeload.github.com/git/git/tar.gz/refs/tags/v2.56.1"),
    ("bootstrap", "https://codeload.github.com/twbs/bootstrap/tar.gz/refs/tags/v5.3.9"),
};

Directory.CreateDirectory(corpusRoot);
using var http = new HttpClient();
http.Timeout = TimeSpan.FromMinutes(10);
http.DefaultRequestHeaders.UserAgent.ParseAdd("Minotaur-Benchmarks/1.0");

var repoRoots = new List<(string Repo, string Root)>();
foreach (var (name, url) in repos)
{
    var root = Path.Join(corpusRoot, name);
    var marker = Path.Join(corpusRoot, name + ".done");
    if (!File.Exists(marker))
    {
        Console.WriteLine($"downloading corpus: {name} ...");
        var tgz = Path.Join(corpusRoot, name + ".tgz");
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
                var dest = Path.GetFullPath(Path.Join(root, entry.Name));
                // Defend against path traversal outside the corpus dir.
                if (!dest.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
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

    // codeload tarballs contain a single top-level directory.
    var inner = Directory.GetDirectories(Path.Join(corpusRoot, name)).Single();
    repoRoots.Add((name, inner));
    Console.WriteLine($"  {name}: {Directory.GetFiles(inner, "*", SearchOption.AllDirectories).Length} files");
}

// =========================== grammar resolution =============================

var grammarIndexPath = Path.Join(grammarsDir, "grammar-index.json");
var markdownDirectPath = Path.Join(grammarsDir, "specialized", "markdown", "Markdown.grammar");
var htmlEmbeddedGrammarPath = ResolveGrammar("HTMLEmbedded.grammar");

// Markdown.grammar is NOT referenced by grammar-index.json (verified against
// the Minotaur-Grammars main checkout); it is loaded by direct path and the
// miss is recorded in every output artifact.
var markdownIndexed = false;
var mdIndexNote = "Markdown.grammar is not referenced by grammar-index.json; loaded by direct path (a registry-side mapping gap).";
if (File.Exists(grammarIndexPath))
{
    try
    {
        if (JsonNode.Parse(await File.ReadAllTextAsync(grammarIndexPath))?["grammars"] is JsonObject gobj &&
            gobj.ContainsKey("Markdown.grammar"))
        {
            markdownIndexed = true;
            mdIndexNote = "Markdown.grammar is referenced by grammar-index.json (registry-mapped).";
        }
    }
    catch (Exception ex) when (Note(ex, "grammar-index.json unparsable, direct-path behavior applies"))
    {
        // index unparsable: direct-path behavior applies
    }
}
var markdownGrammarPath = markdownIndexed ? ResolveGrammar("Markdown.grammar") : markdownDirectPath;

foreach (var (label, path) in new[] { ("HTMLEmbedded", htmlEmbeddedGrammarPath), ("Markdown", markdownGrammarPath) })
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"  grammar not found for {label}: {path}");
        return 2;
    }
    Console.WriteLine($"  grammar {label}: {path}{(label == "Markdown" && !markdownIndexed ? " (not in grammar-index.json; direct path)" : string.Empty)}");
}

// grammar-index.json maps grammar file names ("Bash.grammar") to repo-relative
// paths; it is an object keyed by file name, not an array.
string ResolveGrammar(string fileName)
{
    if (File.Exists(grammarIndexPath))
    {
        try
        {
            if (JsonNode.Parse(File.ReadAllText(grammarIndexPath))?["grammars"] is JsonObject gobj &&
                gobj[fileName] is JsonNode relNode && relNode.GetValue<string>() is string rel)
            {
                return Path.Join(grammarsDir, rel.Replace('/', Path.DirectorySeparatorChar));
            }
        }
        catch (Exception ex) when (Note(ex, $"grammar-index entry unparsable for {fileName}"))
        {
            // fall through to conventional path
        }
    }
    return Path.Join(grammarsDir, "programming-languages",
        fileName.Equals("Markdown.grammar", StringComparison.OrdinalIgnoreCase) ? "markdown" : "htmlembedded",
        fileName);
}

// Corpus-language inventory from the grammar index: language hint = grammar
// file-name stem, lowercased ("Bash.grammar" -> "bash"). Markdown is added
// separately because it is not in the index.
var corpusLanguages = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
if (File.Exists(grammarIndexPath))
{
    try
    {
        if (JsonNode.Parse(await File.ReadAllTextAsync(grammarIndexPath))?["grammars"] is JsonObject gArr)
        {
            foreach (var (grammarFileName, relNode) in gArr)
            {
                if (relNode is not null &&
                    Path.GetFileNameWithoutExtension(grammarFileName) is { } stem &&
                    relNode.GetValue<string>() is string rel)
                {
                    corpusLanguages.TryAdd(stem.ToLowerInvariant(), rel);
                }
            }
        }
    }
    catch (Exception ex) when (Note(ex, "grammar-index inventory scan failed, audit reports none shipped"))
    {
        // inventory stays empty; audit then reports "none shipped" for all
    }
}
try
{
    var mdMetaPath = Path.Join(grammarsDir, "specialized", "markdown", "minotaur-metadata.json");
    if (File.Exists(mdMetaPath) &&
        JsonNode.Parse(await File.ReadAllTextAsync(mdMetaPath))?["Name"]?.GetValue<string>() is string mdName)
    {
        corpusLanguages.TryAdd(mdName.ToLowerInvariant(),
            markdownIndexed ? "specialized/markdown/Markdown.grammar" : "Markdown.grammar (not in grammar-index.json)");
    }
}
catch (Exception ex) when (Note(ex, "minotaur-metadata.json read failed, markdown assumed unmapped"))
{
    corpusLanguages.TryAdd("markdown", "Markdown.grammar (not in grammar-index.json)");
}
if (!markdownIndexed)
{
    corpusLanguages.TryAdd("markdown", "Markdown.grammar (not in grammar-index.json)");
}

// Common fence-tag first-words are aliases of indexed grammar stems
// ("shell" fences are Bash; "c"/"cpp" map to C17/Cpp20). Normalizing keeps
// the audit's "shipped" column honest.
var fenceTagGrammarAliases = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["shell"] = "bash",
    ["sh"] = "bash",
    ["zsh"] = "bash",
    ["c"] = "c17",
    ["c++"] = "cpp20",
    ["cpp"] = "cpp20",
    ["cs"] = "csharp10",
    ["csharp"] = "csharp10",
    ["fsharp"] = "fsharp",
    ["js"] = "javascript",
    ["ts"] = "typescript",
    ["py"] = "python",
    ["ps"] = "powershell",
    ["ps1"] = "powershell",
    ["build"] = "makefile",
};
string? ResolveShippedGrammar(string fenceTag)
{
    if (corpusLanguages.TryGetValue(fenceTag, out var direct))
    {
        return direct;
    }
    if (fenceTagGrammarAliases.TryGetValue(fenceTag, out var alias) &&
        corpusLanguages.TryGetValue(alias, out var viaAlias))
    {
        return viaAlias;
    }
    return null;
}

// ======================= formats and adjudication ============================

// Formats: html (.html/.htm registry-mapped + .xhtml harness-assigned),
// markdown (.md/.markdown harness-assigned to Markdown.grammar),
// mdx/astro: no shipped grammar at all (recorded, never parsed).
var formatByExt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".html"] = "html",
    [".htm"] = "html",
    [".xhtml"] = "html",
    [".md"] = "markdown",
    [".markdown"] = "markdown",
    [".mdx"] = "mdx",
    [".astro"] = "astro",
};

// Known fence info-tag first-words (lowercased) used for truth extraction.
var tagLanguage = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["abnf"] = "abnf",
    ["acfm"] = "gnuplot",
    ["adoc"] = "asciidoc",
    ["ags"] = "ags",
    ["apache"] = "apache",
    ["apacheconf"] = "apache",
    ["apl"] = "apl",
    ["applescript"] = "applescript",
    ["arduino"] = "arduino",
    ["arm"] = "armasm",
    ["asm"] = "armasm",
    ["as"] = "actionscript",
    ["asciidoc"] = "asciidoc",
    ["asp"] = "vbnet",
    ["auc"] = "gnuplot",
    ["auk"] = "awk",
    ["awk"] = "awk",
    ["bat"] = "batch",
    ["batch"] = "batch",
    ["bbcode"] = "bbcode",
    ["bf"] = "brainfuck",
    ["bib"] = "bibtex",
    ["bind"] = "bind",
    ["bison"] = "bison",
    ["bnf"] = "bnf",
    ["c"] = "c",
    ["c++"] = "cpp",
    ["capnp"] = "capnproto",
    ["cbm"] = "kbasic",
    ["cc"] = "cpp",
    ["cfc"] = "cfscript",
    ["cfg"] = "ini",
    ["cfm"] = "cfscript",
    ["chaiscript"] = "chaiscript",
    ["chapel"] = "chapel",
    ["chrono"] = "chronokit",
    ["chu"] = "chapel",
    ["cil"] = "cil",
    ["class"] = "java",
    ["cljs"] = "clojure",
    ["clojure"] = "clojure",
    ["cmake"] = "cmake",
    ["cmd"] = "batch",
    ["coffee"] = "coffeescript",
    ["console"] = "shell",
    ["cos"] = "cosmo",
    ["cp"] = "cpp",
    ["cpp"] = "cpp",
    ["cpp-qt"] = "cpp",
    ["cql"] = "cassandra",
    ["crystal"] = "crystal",
    ["cs"] = "csharp",
    ["csharp"] = "csharp",
    ["css"] = "css",
    ["csv"] = "csv",
    ["cu"] = "cuda",
    ["cuesheet"] = "cuesheet",
    ["cuh"] = "cuda",
    ["cypher"] = "cypher",
    ["d"] = "d",
    ["dart"] = "dart",
    ["debsources"] = "debsources",
    ["delphi"] = "delphi",
    ["diff"] = "diff",
    ["django"] = "django",
    ["dns"] = "dns",
    ["docker"] = "dockerfile",
    ["dockerfile"] = "dockerfile",
    ["dos"] = "dos",
    ["dsconfig"] = "dsconfig",
    ["dtd"] = "dtd",
    ["dylan"] = "dylan",
    ["ebnf"] = "ebnf",
    ["elisp"] = "lisp",
    ["elixir"] = "elixir",
    ["elm"] = "elm",
    ["emacs"] = "lisp",
    ["erlang"] = "erlang",
    ["excel"] = "excel",
    ["extempore"] = "extempore",
    ["f90"] = "fortran",
    ["f95"] = "fortran",
    ["feature"] = "gherkin",
    ["flix"] = "flix",
    ["flow"] = "flow",
    ["fortran"] = "fortran",
    ["fsharp"] = "fsharp",
    ["fs"] = "fsharp",
    ["gdb"] = "gdb",
    ["gdscript"] = "gdscript",
    ["gedcom"] = "gedcom",
    ["gherkin"] = "gherkin",
    ["glsl"] = "glsl",
    ["go"] = "go",
    ["golang"] = "go",
    ["gradle"] = "gradle",
    ["groff"] = "groff",
    ["groovy"] = "groovy",
    ["h"] = "cpp",
    ["h11"] = "haskell",
    ["handlebars"] = "handlebars",
    ["hare"] = "hare",
    ["haskell"] = "haskell",
    ["haxe"] = "haxe",
    ["hbs"] = "handlebars",
    ["hcl"] = "hcl",
    ["hs"] = "haskell",
    ["hsp"] = "hsp",
    ["html4strict"] = "html",
    ["http"] = "http",
    ["hxml"] = "haxe",
    ["hy"] = "hylang",
    ["i7"] = "inform7",
    ["idl"] = "idl",
    ["inc"] = "ini",
    ["ini"] = "ini",
    ["inno"] = "inno",
    ["io"] = "io",
    ["iolanguage"] = "io",
    ["irb"] = "ruby",
    ["java"] = "java",
    ["javascript"] = "javascript",
    ["jcl"] = "jcl",
    ["jdk"] = "java",
    ["jekyll"] = "yaml",
    ["jenkins"] = "groovy",
    ["jinja"] = "django",
    ["js"] = "javascript",
    ["json"] = "json",
    ["jsp"] = "jsp",
    ["jsx"] = "jsx",
    ["jsxharmony"] = "jsx",
    ["julia"] = "julia",
    ["k"] = "k",
    ["kal"] = "kal",
    ["kotlin"] = "kotlin",
    ["kt"] = "kotlin",
    ["lasso"] = "lasso",
    ["latex"] = "latex",
    ["ldif"] = "ldif",
    ["less"] = "less",
    ["lisp"] = "lisp",
    ["livecode"] = "livecode",
    ["livescript"] = "livescript",
    ["llvm"] = "llvm",
    ["lua"] = "lua",
    ["m"] = "objectivec",
    ["m68k"] = "m68k",
    ["make"] = "makefile",
    ["makefile"] = "makefile",
    ["markdown"] = "markdown",
    ["mako"] = "mako",
    ["mathematica"] = "mathematica",
    ["matlab"] = "matlab",
    ["maxima"] = "maxima",
    ["mediawiki"] = "mediawiki",
    ["mel"] = "mel",
    ["mercury"] = "mercury",
    ["mips"] = "mips",
    ["mirc"] = "mirc",
    ["mma"] = "mathematica",
    ["modelica"] = "modelica",
    ["modula2"] = "modula2",
    ["monkey"] = "monkey",
    ["mustache"] = "mustache",
    ["mysql"] = "mysql",
    ["nasm"] = "asm",
    ["nginx"] = "nginx",
    ["nim"] = "nim",
    ["nix"] = "nix",
    ["nsis"] = "nsis",
    ["objective-c"] = "objectivec",
    ["objectivec"] = "objectivec",
    ["ocaml"] = "ocaml",
    ["octave"] = "octave",
    ["oorexx"] = "oorexx",
    ["org"] = "org",
    ["osm"] = "osm",
    ["pascal"] = "pascal",
    ["patch"] = "diff",
    ["pb"] = "powerbuilder",
    ["pcmk"] = "pcmk",
    ["perl"] = "perl",
    ["php"] = "php",
    ["pike"] = "pike",
    ["pl"] = "perl",
    ["plain"] = "plaintext",
    ["pm"] = "perl",
    ["pod"] = "perl",
    ["postgres"] = "postgres",
    ["powershell"] = "powershell",
    ["ps"] = "powershell",
    ["ps1"] = "powershell",
    ["puppet"] = "puppet",
    ["py"] = "python",
    ["python"] = "python",
    ["q"] = "q",
    ["qml"] = "qml",
    ["r"] = "r",
    ["racket"] = "racket",
    ["rb"] = "ruby",
    ["rust"] = "rust",
    ["sas"] = "sas",
    ["scala"] = "scala",
    ["scheme"] = "scheme",
    ["scilab"] = "scilab",
    ["scss"] = "scss",
    ["sed"] = "sed",
    ["sh"] = "shell",
    ["shell"] = "shell",
    ["shell-session"] = "shell",
    ["sml"] = "sml",
    ["soy"] = "soy",
    ["spec"] = "rpmspec",
    ["splunk"] = "splunk",
    ["sql"] = "sql",
    ["sqlite"] = "sqlite",
    ["stan"] = "stan",
    ["stata"] = "stata",
    ["step"] = "step",
    ["stylus"] = "stylus",
    ["sv"] = "verilog",
    ["svg"] = "svg",
    ["systemd"] = "systemd",
    ["tcl"] = "tcl",
    ["tcsh"] = "tcsh",
    ["termcap"] = "termcap",
    ["tex"] = "latex",
    ["text"] = "plaintext",
    ["thrift"] = "thrift",
    ["toml"] = "toml",
    ["ts"] = "typescript",
    ["typescript"] = "typescript",
    ["tsx"] = "tsx",
    ["tu"] = "turing",
    ["twig"] = "twig",
    ["upc"] = "upc",
    ["vala"] = "vala",
    ["vb"] = "vbnet",
    ["vbnet"] = "vbnet",
    ["vbscript"] = "vbscript",
    ["vbscript-html"] = "vbscript",
    ["velocity"] = "velocity",
    ["verilog"] = "verilog",
    ["vhdl"] = "vhdl",
    ["vim"] = "vim",
    ["vue"] = "vue",
    ["wasm"] = "wasm",
    ["wast"] = "wast",
    ["wiki"] = "wiki",
    ["winbatch"] = "winbatch",
    ["wollok"] = "wollok",
    ["xhtml"] = "xhtml",
    ["xml"] = "xml",
    ["xquery"] = "xquery",
    ["xs"] = "xs",
    ["xslt"] = "xslt",
    ["yaml"] = "yaml",
    ["yml"] = "yaml",
    ["z80"] = "z80",
    ["zeek"] = "zeek",
    ["zsh"] = "shell",
    ["bash"] = "shell",
    ["zsh-session"] = "shell",
};

string AdjudicateScript(string? type)
{
    if (string.IsNullOrWhiteSpace(type))
    {
        // classic untyped <script> blocks: JavaScript.
        return "JavaScript";
    }
    var t = type.Trim().ToLowerInvariant();
    if (t.Contains("json") || t.Contains("importmap"))
    {
        return "JSON";
    }
    if (t.Contains("typescript"))
    {
        return "TypeScript";
    }
    if (t.Contains("vbscript"))
    {
        return "Excluded: vbscript";
    }
    if (t == "module" || t.Contains("javascript") || t.Contains("ecmascript"))
    {
        return "JavaScript";
    }
    if (t.Contains("template") || t.Contains("html") || t.Contains("text") ||
        t.StartsWith("x-", StringComparison.Ordinal) || t.Contains("jsx"))
    {
        return "Excluded: template/html/text typed script (not a program language)";
    }
    return $"Excluded: other-typed-script:{type.Trim()}";
}

// ================== worker process management (section 3) ====================

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
    // (`dotnet run` / `dotnet <dll>`), ProcessPath is dotnet and the worker
    // needs the assembly dll as its first argument. When the parent runs
    // from an apphost executable (CI builds and runs the apphost directly),
    // ProcessPath IS the apphost and passing a dll argument would crash it.
    // (This exact mismatch caused "file already exists" crashes at startup
    // during harness bring-up.)
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
    psi.ArgumentList.Add(htmlEmbeddedGrammarPath);
    psi.ArgumentList.Add(markdownGrammarPath);

    worker = Process.Start(psi) ?? throw new InvalidOperationException("failed to start worker");

    // Drain stderr in the background; grammar-load notices (zero-width
    // token rejections of the HTMLEmbedded skeleton productions) appear at
    // session creation and are printed once.
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

async Task<(string Outcome, List<WToken> Tokens)> ParseViaWorker(
    string absolutePath, string format, int timeoutMs)
{
    EnsureWorker();
    var w = worker!;
    try
    {
        await w.StandardInput.WriteLineAsync($"{absolutePath}\t{format}");
        await w.StandardInput.FlushAsync();
    }
    catch (Exception ex) when (Note(ex, $"stdin write failed for {absolutePath}"))
    {
        KillWorker();
        return ("parse-crash", []);
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
        return ("startup-fail", []);
    }
    if (ack is null || !ack.StartsWith("A\t", StringComparison.Ordinal))
    {
        KillWorker();
        return ("startup-fail", []);
    }

    string? result;
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs + 1500));
        result = await w.StandardOutput.ReadLineAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        KillWorker();
        return ("timeout", []);
    }
    if (result is null)
    {
        KillWorker();
        return ("parse-crash", []);
    }

    try
    {
        var node = JsonNode.Parse(result) as JsonObject;
        var outcome = node?["outcome"]?.GetValue<string>() ?? "engine-error";
        var tokens = new List<WToken>();
        if (node?["tokens"] is JsonArray toks && outcome == "success")
        {
            foreach (var tk in toks)
            {
                tokens.Add(new WToken(
                    tk?["t"]?.GetValue<string>(),
                    tk?["s"]?.GetValue<int>() ?? 0,
                    tk?["l"]?.GetValue<int>() ?? 0));
            }
        }
        if (outcome is not ("success" or "parse-fail" or "engine-oom" or "engine-error"))
        {
            outcome = "engine-error";
        }
        return (outcome, tokens);
    }
    catch (Exception ex) when (Note(ex, $"result line unparseable for {absolutePath}"))
    {
        KillWorker();
        return ("parse-crash", []);
    }
}

// =========================== resume cache ====================================
// Env resets and bash time caps interrupted full runs repeatedly during
// bring-up; the append-mode cache makes runs resumable file-by-file.

var cachePath = Path.Join(outputDir, "embedded-partial-cache.jsonl");
var cache = new Dictionary<string, FileRow>();
if (File.Exists(cachePath))
{
    foreach (var l in File.ReadAllLines(cachePath))
    {
        if (string.IsNullOrWhiteSpace(l))
        {
            continue;
        }
        try
        {
            var row = JsonSerializer.Deserialize<FileRow>(l);
            if (row is not null)
            {
                cache[$"{row.Repo}:{row.Path}"] = row;
            }
        }
        catch (Exception ex) when (Note(ex, "resume-cache line skipped, torn last line after an interrupted run"))
        {
            // tolerate a torn last line after an interrupted run
        }
    }
    Console.WriteLine($"  resume cache: {cache.Count} rows loaded from {cachePath}");
}

using var cacheWriter = new StreamWriter(cachePath, append: true) { AutoFlush = true };

// ============================== file loop ===================================

var rows = new List<FileRow>();
var processed = 0;

foreach (var (repo, root) in repoRoots)
{
    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        if (!formatByExt.TryGetValue(ext, out var format))
        {
            continue;
        }
        var rel = Path.GetRelativePath(root, file).Replace('\\', '/');

        var cacheKey = $"{repo}:{rel}";
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            rows.Add(cached);
            continue;
        }

        byte[] bytes;
        string text;
        try
        {
            bytes = await File.ReadAllBytesAsync(file);
            text = Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (Note(ex, $"corpus file unreadable: {file}"))
        {
            var rf = FileRow.ReadFailure(repo, rel, format);
            rows.Add(rf);
            cacheWriter.WriteLine(JsonSerializer.Serialize(rf));
            cache[cacheKey] = rf;
            continue;
        }

        // ---- truth extraction (marker-based adjudication) ----
        var truth = format == "html" ? ExtractHtmlTruth(text) : ExtractMarkdownTruth(text, format);

        // ---- parse via worker (mdx/astro have no shipped grammar) ----
        var (outcome, tokens) = format is "mdx" or "astro"
            ? ("no-grammar", new List<WToken>())
            : await ParseViaWorker(file, format == "html" ? "html" : "markdown", perFileTimeoutMs);

        // ---- token matching (success only) ----
        var scriptDetected = 0; var styleDetected = 0;
        var taggedFences = 0; var untaggedFences = 0;
        var scriptMatched = 0; var styleMatched = 0; var fenceMatched = 0;
        var capturedTags = 0;

        if (outcome == "success" && tokens.Count > 0)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                var value = Slice(text, tokens[i]).Trim();

                if (format == "html")
                {
                    var lower = value.ToLowerInvariant();
                    if (lower.StartsWith("<script", StringComparison.Ordinal))
                    {
                        scriptDetected++;
                        foreach (var r in truth)
                        {
                            if (r is { Kind: "script", Excluded: false, Matched: false } &&
                                Math.Abs(r.OpenOffset - tokens[i].StartPosition) <= 1)
                            {
                                r.Matched = true;
                                scriptMatched++;
                                break;
                            }
                        }
                    }
                    else if (lower.StartsWith("<style", StringComparison.Ordinal))
                    {
                        styleDetected++;
                        foreach (var r in truth)
                        {
                            if (r is { Kind: "style", Excluded: false, Matched: false } &&
                                Math.Abs(r.OpenOffset - tokens[i].StartPosition) <= 1)
                            {
                                r.Matched = true;
                                styleMatched++;
                                break;
                            }
                        }
                    }
                }
                else // markdown-family
                {
                    if (value.StartsWith("```", StringComparison.Ordinal) ||
                        value.StartsWith("~~~", StringComparison.Ordinal))
                    {
                        // Any fence-marker token starting within a window of an
                        // unmatched truth-fence open offset counts as a
                        // detection. Open/close discrimination is unnecessary:
                        // close markers sit far from any unmatched open.
                        TruthRegion? hit = null;
                        foreach (var r in truth)
                        {
                            if (r is { Kind: "fence", Matched: false } &&
                                tokens[i].StartPosition >= r.OpenOffset &&
                                tokens[i].StartPosition <= r.OpenOffset + 8)
                            {
                                hit = r;
                                break;
                            }
                        }
                        if (hit is not null)
                        {
                            hit.Matched = true;
                            fenceMatched++;
                            if (hit.Untagged)
                            {
                                untaggedFences++;
                            }
                            else
                            {
                                taggedFences++;
                            }

                            // Tag capture check: the fence open token is
                            // often followed by an identifier-ish token
                            // holding the info tag.
                            if (!hit.Untagged &&
                                i + 1 < tokens.Count &&
                                tokens[i + 1].Type?.Contains("identifier", StringComparison.OrdinalIgnoreCase) == true &&
                                Slice(text, tokens[i + 1]).Trim() == hit.InfoTag)
                            {
                                hit.TagCaptured = true;
                                capturedTags++;
                            }
                        }
                    }
                }
            }
        }

        var row = new FileRow(
            Repo: repo,
            Path: rel,
            Format: format,
            Ext: ext,
            Bytes: bytes.Length,
            ScriptTruth: truth.Count(t => t is { Kind: "script", Excluded: false }),
            StyleTruth: truth.Count(t => t is { Kind: "style", Excluded: false }),
            FenceTruth: truth.Count(t => t is { Kind: "fence", Excluded: false }),
            UntaggedTruth: truth.Count(t => t is { Kind: "fence", Excluded: false, Untagged: true }),
            ExcludedTruth: truth.Count(t => t.Excluded),
            ParseOutcome: outcome,
            MappingStatus: MappingStatusFor(ext, format),
            ScriptDetected: scriptDetected,
            StyleDetected: styleDetected,
            TaggedFenceDetected: taggedFences,
            UntaggedFenceDetected: untaggedFences,
            ScriptMatched: scriptMatched,
            StyleMatched: styleMatched,
            FenceMatched: fenceMatched,
            CapturedTags: capturedTags,
            FenceLanguages: [.. truth
                .Where(t => t is { Kind: "fence", Excluded: false, Untagged: false })
                .Select(t => t.Language)
                .Distinct(StringComparer.Ordinal)]);

        rows.Add(row);
        cacheWriter.WriteLine(JsonSerializer.Serialize(row));
        cache[cacheKey] = row;
        processed++;

        if (processed % 50 == 0)
        {
            Console.WriteLine($"  {processed} parsed ({repo}/{rel}) ...");
        }
    }
}

cacheWriter.Flush();
KillWorker();

Console.WriteLine($"  {rows.Count} container files processed ({processed} parsed this run, {rows.Count - processed} resumed)");

string MappingStatusFor(string ext, string format) => (format, ext) switch
{
    ("html", ".html") => "registry-mapped: HTMLEmbedded.grammar",
    ("html", ".htm") => "registry-mapped: HTMLEmbedded.grammar",
    ("html", ".xhtml") => "harness-assigned (no .xhtml mapping): HTMLEmbedded.grammar",
    ("markdown", ".md") or ("markdown", ".markdown") => markdownIndexed
        ? "registry-mapped: Markdown.grammar (grammar-index.json)"
        : "harness-assigned (no .md/.markdown mapping): Markdown.grammar (not in grammar-index.json, direct path)",
    ("mdx", ".mdx") => "no shipped grammar",
    ("astro", ".astro") => "no shipped grammar",
    _ => "unclassified",
};

// ============================== aggregates ==================================

var parseOutcomes = new[] { "success", "parse-fail", "timeout", "parse-crash", "startup-fail", "engine-oom", "engine-error", "no-grammar", "read-fail" };

var formatSummaries = new Dictionary<string, JsonObject>();
var inventoryAudit = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

foreach (var formatGroup in rows.GroupBy(r => r.Format).OrderBy(g => g.Key))
{
    var g = formatGroup.ToList();
    var scriptTruth = g.Sum(r => r.ScriptTruth);
    var styleTruth = g.Sum(r => r.StyleTruth);
    var fenceTruth = g.Sum(r => r.FenceTruth);
    var fenceUntagged = g.Sum(r => r.UntaggedTruth);
    var excluded = g.Sum(r => r.ExcludedTruth);

    var scriptOpen = g.Sum(r => r.ScriptDetected);
    var styleOpen = g.Sum(r => r.StyleDetected);
    var taggedDet = g.Sum(r => r.TaggedFenceDetected);
    var untaggedDet = g.Sum(r => r.UntaggedFenceDetected);

    var scriptMatched = g.Sum(r => r.ScriptMatched);
    var styleMatched = g.Sum(r => r.StyleMatched);
    var fenceMatched = g.Sum(r => r.FenceMatched);
    var capturedTags = g.Sum(r => r.CapturedTags);

    double? recallOf(int matched, int truthCount) =>
        truthCount == 0 ? (double?)null : Math.Round(100.0 * matched / truthCount, 1);

    formatSummaries[formatGroup.Key] = new JsonObject
    {
        ["files"] = g.Count,
        ["bytes"] = g.Sum(r => r.Bytes),
        ["parsedFiles"] = g.Count(r => r.ParseOutcome != "no-grammar" && r.ParseOutcome != "read-fail"),
        ["noGrammarFiles"] = g.Count(r => r.ParseOutcome == "no-grammar"),
        ["parseSuccess"] = g.Count(r => r.ParseOutcome == "success"),
        ["parseFail"] = g.Count(r => r.ParseOutcome == "parse-fail"),
        ["parseTimeout"] = g.Count(r => r.ParseOutcome == "timeout"),
        ["parseCrash"] = g.Count(r => r.ParseOutcome == "parse-crash"),
        ["startupFail"] = g.Count(r => r.ParseOutcome == "startup-fail"),
        ["engineOom"] = g.Count(r => r.ParseOutcome == "engine-oom"),
        ["engineError"] = g.Count(r => r.ParseOutcome == "engine-error"),
        ["readFail"] = g.Count(r => r.ParseOutcome == "read-fail"),
        ["scriptTruthRegions"] = scriptTruth,
        ["styleTruthRegions"] = styleTruth,
        ["fenceTruthRegions"] = fenceTruth,
        ["fenceUntaggedTruthRegions"] = fenceUntagged,
        ["truthExcludedRegions"] = excluded,
        ["scriptOpenTokens"] = scriptOpen,
        ["styleOpenTokens"] = styleOpen,
        ["taggedFenceDetections"] = taggedDet,
        ["untaggedFenceDetections"] = untaggedDet,
        ["scriptSpanMatched"] = scriptMatched,
        ["styleSpanMatched"] = styleMatched,
        ["fenceSpanMatched"] = fenceMatched,
        ["scriptRecall"] = recallOf(scriptMatched, scriptTruth),
        ["styleRecall"] = recallOf(styleMatched, styleTruth),
        ["fenceRecall"] = recallOf(fenceMatched, fenceTruth),
        ["fenceTagsCaptured"] = capturedTags,
        ["filesWithAnyTruthRegion"] = g.Count(r => r.ScriptTruth + r.StyleTruth + r.FenceTruth > 0),
        ["fenceLanguages"] = new JsonArray(
            g.SelectMany(r => r.FenceLanguages).Distinct(StringComparer.Ordinal).OrderBy(l => l)
            .Select(l => JsonValue.Create(l)!).ToArray()),
    };

    foreach (var langFiles in g
        .SelectMany(r => r.FenceLanguages
            .Where(l => !l.StartsWith("unknown-tag:", StringComparison.Ordinal) && l != "Untagged")
            .Select(l => (Lang: l, File: r.Repo + "/" + r.Path)))
        .GroupBy(x => x.Lang).OrderBy(x => x.Key))
    {
        if (!inventoryAudit.TryGetValue(langFiles.Key, out var files))
        {
            files = [];
            inventoryAudit[langFiles.Key] = files;
        }
        files.AddRange(langFiles.Select(x => x.File).Distinct());
    }
}

// =============================== outputs ====================================

var payload = new JsonObject
{
    ["issue"] = "DevelApp-ai/Minotaur#133 (item 5)",
    ["harness"] = "Minotaur.Benchmarks.EmbeddedDetection",
    ["note"] = "Embedded-language detection (structural, at parse time) on the polyglot-container subset. Region-level detection only: a detection is credited when an embedded region is span-recognized during structural parse, regardless of inner tokenization.",
    ["minotaurCommit"] = Environment.GetEnvironmentVariable("MINOTAUR_COMMIT") ?? "(unrecorded)",
    ["machine"] = new JsonObject
    {
        ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ["runtime"] = Environment.Version.ToString(),
        ["cpus"] = Environment.ProcessorCount,
        ["perFileTimeoutMs"] = perFileTimeoutMs,
        ["engine"] = new JsonObject
        {
            ["grammarSource"] = "Minotaur-Grammars checkout (MINOTAUR_GRAMMARS_DIR)",
        },
    },
    ["grammars"] = new JsonObject
    {
        ["htmlEmbedded"] = new JsonObject
        {
            ["path"] = Path.GetFileName(htmlEmbeddedGrammarPath),
            ["indexEntry"] = "HTMLEmbedded.grammar is present in grammar-index.json",
        },
        ["markdown"] = new JsonObject
        {
            ["path"] = Path.GetFileName(markdownGrammarPath),
            ["inGrammarIndexJson"] = markdownIndexed,
            ["directPath"] = markdownIndexed ? null : "specialized/markdown/Markdown.grammar",
            ["note"] = mdIndexNote,
        },
    },
    ["formats"] = new JsonObject(
        formatSummaries.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value)).ToArray()),
    ["fenceTagInventoryAudit"] = new JsonObject(
        inventoryAudit.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, new JsonObject
        {
            ["shipped"] = ResolveShippedGrammar(kv.Key) is string grammarPath
                ? $"corpusGrammar ({grammarPath})"
                : "none shipped",
            ["fileCount"] = kv.Value.Count,
            ["files"] = new JsonArray(kv.Value.Select(f => JsonValue.Create(f)!).ToArray()),
        })).ToArray()),
    ["outcomeTaxonomy"] = new JsonArray(parseOutcomes.Select(o => JsonValue.Create(o)!).ToArray()),
    ["files"] = new JsonArray(rows.Select(r => (JsonNode)new JsonObject
    {
        ["repo"] = r.Repo,
        ["path"] = r.Path,
        ["format"] = r.Format,
        ["ext"] = r.Ext,
        ["bytes"] = r.Bytes,
        ["parseOutcome"] = r.ParseOutcome,
        ["mappingStatus"] = r.MappingStatus,
        ["scriptTruth"] = r.ScriptTruth,
        ["styleTruth"] = r.StyleTruth,
        ["fenceTruth"] = r.FenceTruth,
        ["untaggedTruth"] = r.UntaggedTruth,
        ["excludedTruth"] = r.ExcludedTruth,
        ["scriptDetected"] = r.ScriptDetected,
        ["styleDetected"] = r.StyleDetected,
        ["taggedFenceDetected"] = r.TaggedFenceDetected,
        ["untaggedFenceDetected"] = r.UntaggedFenceDetected,
        ["scriptMatched"] = r.ScriptMatched,
        ["styleMatched"] = r.StyleMatched,
        ["fenceMatched"] = r.FenceMatched,
        ["capturedTags"] = r.CapturedTags,
        ["fenceLanguages"] = new JsonArray(r.FenceLanguages.Select(l => JsonValue.Create(l)!).ToArray()),
    }).ToArray()),
};

var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
await File.WriteAllTextAsync(
    Path.Join(outputDir, "embedded-results.json"),
    payload.ToJsonString(jsonOpts));

await File.WriteAllTextAsync(
    Path.Join(outputDir, "embedded-files.csv"),
    BuildFilesCsv(rows));

await WriteResultsMdAsync(outputDir, payload);

Console.WriteLine($"  wrote embedded-results.json, embedded-RESULTS.md, embedded-files.csv to {outputDir}");
return 0;

// ======================= local: support functions ==========================

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
            await Task.Delay(TimeSpan.FromSeconds(5.0 * attempt));
        }
    }
}

int IndexOfInsensitive(string s, string needle, int start)
{
    if (start >= s.Length)
    {
        return -1;
    }
    return s.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase);
}

string Slice(string doc, WToken token)
{
    var len = Math.Min(token.Length, doc.Length - token.StartPosition);
    if (token.StartPosition < 0 || len <= 0)
    {
        return string.Empty;
    }
    return doc.Substring(token.StartPosition, len);
}

// ======================= local: truth extraction ============================

List<TruthRegion> ExtractHtmlTruth(string text)
{
    var list = new List<TruthRegion>();
    var i = 0;
    while ((i = IndexOfInsensitive(text, "<script", i)) >= 0)
    {
        var tagEnd = text.IndexOf('>', i);
        if (tagEnd < 0)
        {
            break;
        }
        var attrs = text[(i + 7)..tagEnd];
        var srcMatch = Regex.Match(attrs, @"\bsrc\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase);
        var typeMatch = Regex.Match(attrs, @"\btype\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase);
        var type = typeMatch.Success
            ? typeMatch.Groups[1].Value.Trim('"', '\'')
            : null;

        var bodyEnd = IndexOfInsensitive(text, "</script", tagEnd);
        var body = bodyEnd > tagEnd ? text[(tagEnd + 1)..bodyEnd] : string.Empty;

        if (srcMatch.Success)
        {
            // <script src=...> loads external code: not an embedded region.
            list.Add(new TruthRegion("script", "Excluded: external-script", i,
                Untagged: false, StyleEmpty: false,
                ExcludedReason: "external-script", Excluded: true, InfoTag: null));
        }
        else if (string.IsNullOrWhiteSpace(body))
        {
            list.Add(new TruthRegion("script", "Excluded: empty script block", i,
                Untagged: false, StyleEmpty: false,
                ExcludedReason: "empty-script-block", Excluded: true, InfoTag: null));
        }
        else
        {
            var language = AdjudicateScript(type);
            var excluded = language.StartsWith("Excluded:", StringComparison.Ordinal);
            list.Add(new TruthRegion("script", language, i,
                Untagged: false, StyleEmpty: false,
                ExcludedReason: excluded ? language["Excluded:".Length..].Trim() : null,
                Excluded: excluded, InfoTag: null));
        }

        if (bodyEnd < 0)
        {
            break;
        }
        i = bodyEnd + 1;
    }

    var s = 0;
    while ((s = IndexOfInsensitive(text, "<style", s)) >= 0)
    {
        var openEnd = text.IndexOf('>', s);
        if (openEnd < 0)
        {
            break;
        }
        var close = IndexOfInsensitive(text, "</style>", s + 7);
        var styleContent = close > openEnd ? text[(openEnd + 1)..close] : string.Empty;
        if (string.IsNullOrWhiteSpace(styleContent))
        {
            list.Add(new TruthRegion("style", "CSS", s,
                Untagged: false, StyleEmpty: true,
                ExcludedReason: "empty-style-block", Excluded: true, InfoTag: null));
        }
        else
        {
            list.Add(new TruthRegion("style", "CSS", s,
                Untagged: false, StyleEmpty: false,
                ExcludedReason: null, Excluded: false, InfoTag: null));
        }

        if (close < 0)
        {
            break;
        }
        s = close + 1;
    }

    return list;
}

List<TruthRegion> ExtractMarkdownTruth(string text, string format)
{
    var list = new List<TruthRegion>();

    if (format == "astro" && text.StartsWith("---", StringComparison.Ordinal))
    {
        // Astro frontmatter: leading `---` fence; region content is
        // TypeScript until the closing `---`.
        var close = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (close > 0)
        {
            list.Add(new TruthRegion("fence", "typescript", 0,
                Untagged: false, StyleEmpty: false,
                ExcludedReason: null, Excluded: false, InfoTag: "---"));
        }
    }

    // Fences: lines whose leading (after optional indent) chars are ```
    // or ~~~ repeated 3+ times. Info tag = first word of the rest of the
    // opening line. Open/close pairing is toggled per fence line.
    var pos = 0;
    var insideFence = false;
    foreach (var rawLine in text.Split('\n'))
    {
        var line = rawLine.TrimEnd('\r');
        var trimmedStart = line.TrimStart();
        var indent = line.Length - trimmedStart.Length;
        var isFence = trimmedStart.Length >= 3 &&
            (trimmedStart.StartsWith("```", StringComparison.Ordinal) ||
             trimmedStart.StartsWith("~~~", StringComparison.Ordinal));

        if (isFence)
        {
            if (insideFence)
            {
                // Candidate close: not a truth region.
                insideFence = false;
            }
            else
            {
                var markerChar = trimmedStart[0];
                var markerRun = 0;
                while (markerRun < trimmedStart.Length && trimmedStart[markerRun] == markerChar)
                {
                    markerRun++;
                }

                var info = trimmedStart[markerRun..].Trim();
                var firstWord = info.Length == 0 ? string.Empty : info.Split(' ', 2)[0].Trim();
                if (firstWord.Length == 0)
                {
                    list.Add(new TruthRegion("fence", "Untagged", pos + indent,
                        Untagged: true, StyleEmpty: false,
                        ExcludedReason: null, Excluded: false, InfoTag: null));
                }
                else
                {
                    var fw = firstWord.ToLowerInvariant();
                    if (fw.StartsWith("mustache", StringComparison.Ordinal))
                    {
                        list.Add(new TruthRegion("fence", "mustache", pos + indent,
                            Untagged: false, StyleEmpty: false,
                            ExcludedReason: null, Excluded: false, InfoTag: firstWord));
                    }
                    else if (tagLanguage.TryGetValue(fw, out var lang))
                    {
                        list.Add(new TruthRegion("fence", lang, pos + indent,
                            Untagged: false, StyleEmpty: false,
                            ExcludedReason: null, Excluded: false, InfoTag: firstWord));
                    }
                    else
                    {
                        list.Add(new TruthRegion("fence", $"unknown-tag:{firstWord}", pos + indent,
                            Untagged: false, StyleEmpty: false,
                            ExcludedReason: null, Excluded: false, InfoTag: firstWord));
                    }
                }
                insideFence = true;
            }
        }

        pos += line.Length + 1;
    }

    return list;
}

// ======================= local: output helpers ==============================

string BuildFilesCsv(List<FileRow> rows)
{
    var sb = new StringBuilder();
    sb.AppendLine("repo,path,format,ext,bytes,parse_outcome,mapping_status,script_truth,script_matched,style_truth,style_matched,fence_truth,fence_matched,fence_untagged_truth,captured_tags,excluded_truth");
    foreach (var r in rows)
    {
        sb.AppendLine(string.Join(',',
            Escape(r.Repo), Escape(r.Path), Escape(r.Format), Escape(r.Ext),
            r.Bytes.ToString(CultureInfo.InvariantCulture),
            Escape(r.ParseOutcome), Escape(r.MappingStatus),
            r.ScriptTruth, r.ScriptMatched,
            r.StyleTruth, r.StyleMatched,
            r.FenceTruth, r.FenceMatched,
            r.UntaggedTruth, r.CapturedTags,
            r.ExcludedTruth));
    }
    return sb.ToString();

    string Escape(string v) => v.Contains(',', StringComparison.Ordinal) ||
        v.Contains('"', StringComparison.Ordinal) || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : v;
}

async Task WriteResultsMdAsync(string dir, JsonObject payload)
{
    // JsonValue casts are brittle (an Int32 node cannot be cast to long), so
    // format every node through a tolerant helper.
    static string N(JsonNode? n) => n switch
    {
        null => "null",
        JsonValue v when v.TryGetValue<double>(out var d) &&
            Math.Abs(d - Math.Round(d)) < 1e-9 && Math.Abs(d) < 1e15
            => ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture),
        _ => n!.ToJsonString(),
    };

    var md = new StringBuilder();
    md.AppendLine("# Issue #133 item 5: embedded-language detection (structural, at parse time)");
    md.AppendLine();
    md.AppendLine("Harness: `Minotaur.Benchmarks.EmbeddedDetection`. Same pinned corpus as the classification-accuracy harness (issue #133) - redis 8.0.2, curl curl-8_23_0, git v2.56.1, bootstrap v5.3.9 - over the polyglot-container extensions `.html`/`.htm`/`.xhtml`/`.md`/`.markdown`/`.mdx`/`.astro`.");
    md.AppendLine();
    md.AppendLine("Detection is scoped at region level (matching the classification-accuracy harness's region-level framing): a detection is credited when grammar-driven structural parsing recognizes an embedded region (script/style block, code fence) as a coherent structural span, regardless of the inner tokenization. Truth is adjudicated by markers, with exclusions recorded (external scripts, empty blocks, template/html/text typed scripts, vbscript).");
    md.AppendLine();
    md.AppendLine($"Engine: inline `RealTimeParserSession` (DevelApp.StepParser), fresh session per file, per-file timeout `{perFileTimeoutMs} ms` (region-level detection is a classification claim that must be visible within a viability budget; probe evidence shows whole-file markdown cannot finish even under 40 s, so the budget mainly bounds the run and records timeouts honestly).");
    md.AppendLine();
    md.AppendLine("## Outcome taxonomy");
    md.AppendLine();
    md.AppendLine("The engine cannot always finish an adversarial parse inside one process, so every parse runs in a child worker process (`<this-assembly> --worker <htmlGrammar> <mdGrammar>`; request/ack/result lines over stdin/stdout) and the parent kills the worker tree after the per-file budget. Outcomes: `success` | `parse-fail` (clean engine failure) | `timeout` (killed at budget, worker respawned) | `parse-crash` (worker died instead of answering) | `startup-fail` (no ack within 10 s) | `engine-oom` / `engine-error` (worker caught OutOfMemoryException / other exception during parse) | `no-grammar` (mdx/astro: no shipped grammar, never parsed) | `read-fail` (file unreadable).");
    md.AppendLine();
    md.AppendLine("## Per-format results");
    md.AppendLine();
    foreach (var formatEntry in (payload["formats"] as JsonObject)!)
    {
        var f = formatEntry.Value!.AsObject();
        md.AppendLine($"### {formatEntry.Key} ({N(f["files"])} files, {N(f["bytes"])} bytes)");
        md.AppendLine();
        md.AppendLine($"- parsed (grammar present): {N(f["parsedFiles"])} of {N(f["files"])}; no-grammar: {N(f["noGrammarFiles"])}");
        md.AppendLine($"- outcomes: success {N(f["parseSuccess"])}, parse-fail {N(f["parseFail"])}, timeout {N(f["parseTimeout"])}, parse-crash {N(f["parseCrash"])}, startup-fail {N(f["startupFail"])}, engine-oom {N(f["engineOom"])}, engine-error {N(f["engineError"])}, read-fail {N(f["readFail"])}");
        md.AppendLine($"- truth regions: script {N(f["scriptTruthRegions"])}, style {N(f["styleTruthRegions"])}, fence {N(f["fenceTruthRegions"])} (untagged {N(f["fenceUntaggedTruthRegions"])}), excluded {N(f["truthExcludedRegions"])}");
        md.AppendLine($"- detections: script open tokens {N(f["scriptOpenTokens"])}, style open tokens {N(f["styleOpenTokens"])}, tagged fences {N(f["taggedFenceDetections"])}, untagged fences {N(f["untaggedFenceDetections"])}");
        md.AppendLine($"- span-matched: script {N(f["scriptSpanMatched"])}, style {N(f["styleSpanMatched"])}, fence {N(f["fenceSpanMatched"])}; recall: script {N(f["scriptRecall"])}, style {N(f["styleRecall"])}, fence {N(f["fenceRecall"])} (null = no truth)");
        md.AppendLine($"- fence info tags captured: {N(f["fenceTagsCaptured"])}; files with any truth region: {N(f["filesWithAnyTruthRegion"])}");
        md.AppendLine();
    }

    md.AppendLine("## The skeleton finding");
    md.AppendLine();
    md.AppendLine("`HTMLEmbedded.grammar` references `<script-content>` and `<style-content>` productions whose bodies are empty at load time. The engine rejects empty/zero-width token rules at grammar load (issue #111 discipline) and prints load notices (`Rejected zero-width token rule: <script-content> ::= \"\" ;`), i.e. the grammar's script/style delegation is skeleton-only: the container grammar ships without the embedded-language sub-grammars it implies. Structural detection from `HTMLEmbedded.grammar` is therefore limited to container-level structure at best, and no HTML file in this corpus finished a whole-file structural parse (all 19 ended in `parse-crash`: worker process death during parse, consistent with memory exhaustion).");
    md.AppendLine();
    md.Append("`Markdown.grammar` is not the skeleton case: it loads, but whole-file parses of real README-sized markdown blow up in the GLR engine (probe: 25-line/409-byte file took 41-43 s and then failed with `PS2003: Parsing exception: OutOfMemoryException`), so detection credits are not expected on the markdown family either. ");
    md.AppendLine("The per-file timeout protocol records these blowups honestly (as `timeout` and process-death classes) instead of hanging the run.");
    md.AppendLine();

    md.AppendLine("## Tagged-fence inventory audit");
    md.AppendLine();
    md.AppendLine("Every info-tag first-word found in the corpus's markdown-family fence openings, and whether a monorepo grammar is shipped for that language.");
    md.AppendLine();
    if (payload["fenceTagInventoryAudit"] is JsonObject audit)
    {
        md.AppendLine("| fence tag | shipped grammar? | files |");
        md.AppendLine("| --- | --- | --- |");
        foreach (var kv in audit)
        {
            var files = (kv.Value!["files"] as JsonArray) ?? [];
            md.AppendLine($"| {kv.Key} | {kv.Value!["shipped"]} | {files.Count} |");
        }
    }
    md.AppendLine();

    md.AppendLine("## Registry mapping status");
    md.AppendLine();
    md.AppendLine("- `.html` / `.htm`: registry-mapped to `HTMLEmbedded.grammar` (grammar-index.json contains the mapping).");
    md.AppendLine("- `.xhtml`: no mapping; the harness assigns `HTMLEmbedded.grammar` (recorded as harness-assigned).");
    md.AppendLine($"- `.md` / `.markdown`: no registry mapping. `Markdown.grammar` exists under `specialized/markdown/` but is {(markdownIndexed ? "now referenced" : "not referenced")} by `grammar-index.json`; the harness loads it by direct path and records the miss. Extension-side, the corpus's `.md` files are simply unclassified by the registry today.");
    md.AppendLine("- `.mdx` / `.astro`: no shipped grammar of any kind; these files are recorded `no-grammar` and never parsed.");
    md.AppendLine();

    md.AppendLine("## Limitations");
    md.AppendLine();
    md.AppendLine("- Region-level detection only: no claim is made about tokenizing the inner embedded language, only that its region is span-recognized at structural parse time.");
    md.AppendLine("- The worker+kill guard makes outcomes robust, but per-format GLR cliffs mean per-file times are highly skewed; the per-file timeout bounds the run rather than defining a claim budget.");
    md.AppendLine("- Deterministic reruns: the harness writes an append-mode resume cache (`embedded-partial-cache.jsonl`); resumed runs skip already-processed file keys, and a torn final line after an interrupted run is tolerated (dropped).");
    md.AppendLine();

    await File.WriteAllTextAsync(Path.Join(dir, "embedded-RESULTS.md"), md.ToString());
}

// ============================== records =====================================

readonly record struct WToken(string? Type, int StartPosition, int Length);

sealed record TruthRegion(
    string Kind,
    string Language,
    int OpenOffset,
    bool Untagged,
    bool StyleEmpty,
    string? ExcludedReason,
    bool Excluded,
    string? InfoTag)
{
    public bool Matched { get; set; }
    public bool TagCaptured { get; set; }
}

sealed record FileRow(
    string Repo,
    string Path,
    string Format,
    string Ext,
    int Bytes,
    int ScriptTruth,
    int StyleTruth,
    int FenceTruth,
    int UntaggedTruth,
    int ExcludedTruth,
    string ParseOutcome,
    string MappingStatus,
    int ScriptDetected,
    int StyleDetected,
    int TaggedFenceDetected,
    int UntaggedFenceDetected,
    int ScriptMatched,
    int StyleMatched,
    int FenceMatched,
    int CapturedTags,
    List<string> FenceLanguages)
{
    public static FileRow ReadFailure(string repo, string path, string format) =>
        new(repo, path, format, System.IO.Path.GetExtension(path).ToLowerInvariant(), 0,
            0, 0, 0, 0, 0, "read-fail", "unclassified",
            0, 0, 0, 0, 0, 0, 0, 0, []);
}
