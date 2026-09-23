using System.Text.Json;
using Xunit;

namespace Minotaur.Tests.Examples;

/// <summary>
/// Verification tests for the files under <c>examples/</c> (Minotaur issue #89):
/// the legacy <c>.gf</c> grammar format, the current grammar configuration and
/// the example test data files.
/// <para>
/// Verification results (issue #89): the <c>.gf</c> files are a *legacy*
/// grammar format migrated from the old Minotaur codebase. No component of
/// the current pipeline can load them — <c>ProjectLoader</c>,
/// <c>GrammarDetectionManager</c> and <c>GrammarConfiguration</c> exclusively
/// reference <c>.grammar</c> files (owned by the external Minotaur-Grammars
/// repository, e.g. <c>HTMLEmbedded.grammar</c> for embedded-language HTML).
/// The files are kept as reference material for the embedded-grammar feature;
/// these tests codify that status and their structural integrity.
/// </para>
/// </summary>
public sealed class ExampleGrammarVerificationTests
{
    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var hasSrc = Directory.Exists(Path.Join(directory.FullName, "src"));
            var hasExamples = Directory.Exists(Path.Join(directory.FullName, "examples"));
            if (hasSrc && hasExamples)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root (src/ + examples/) from " + AppContext.BaseDirectory);
    }

    // ---------------------------------------------------------------------
    // Legacy .gf grammar files: format status and structure
    // ---------------------------------------------------------------------

    public static TheoryData<string, string> GfGrammarFiles => new()
    {
        { "examples/embedded-grammars/grammars/html_base.gf", "HTMLBase" },
        { "examples/embedded-grammars/grammars/css.gf", "CSS" },
        { "examples/embedded-grammars/grammars/javascript.gf", "JavaScript" },
        { "examples/embedded-grammars/grammars/html_embedded.gf", "HTMLEmbedded" },
    };

    [Theory]
    [MemberData(nameof(GfGrammarFiles))]
    public void GfFiles_ExistWithLegacyFormatHeaders(string relativePath, string grammarName)
    {
        var content = ReadRepoFile(relativePath);

        // The .gf format declares itself with these headers.
        Assert.Contains("@Minotaur", content, StringComparison.Ordinal);
        Assert.Contains("@FormatType: Minotaur", content, StringComparison.Ordinal);

        // ... and wraps its rules in a named grammar block.
        Assert.Contains($"grammar {grammarName} {{", content, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(GfGrammarFiles))]
    public void GfFiles_AreStructurallyBalanced(string relativePath, string _)
    {
        var content = ReadRepoFile(relativePath);
        var balance = CountBraceBalance(content);

        // Not truncated: every grammar/annotation block opens and closes.
        // (Braces inside character classes, comments and literals are excluded.)
        Assert.True(balance == 0, $"{relativePath}: unbalanced braces (depth {balance})");
    }

    [Fact]
    public void HtmlEmbeddedGf_InheritanceReferences_ResolveToSiblingGrammars()
    {
        var embedded = ReadRepoFile("examples/embedded-grammars/grammars/html_embedded.gf");

        // @Inherits: HTMLBase, JavaScript, CSS
        var inheritsLine = embedded.Split('\n')
            .Select(l => l.Trim())
            .First(l => l.StartsWith("@Inherits:", StringComparison.Ordinal));
        var inheritedNames = inheritsLine["@Inherits:".Length..]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(new[] { "HTMLBase", "JavaScript", "CSS" }, inheritedNames);

        // Every inherited grammar name resolves to a grammar block in a sibling file.
        var siblingContents = GfGrammarFiles
            .Select(t => (string)t[0])
            .Where(p => !p.EndsWith("html_embedded.gf", StringComparison.Ordinal))
            .Select(ReadRepoFile);
        foreach (var name in inheritedNames)
        {
            Assert.Contains(siblingContents, c => c.Contains($"grammar {name} {{", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CurrentPipeline_HasNoGfSupport()
    {
        var root = FindRepoRoot();

        // The grammar-discovery surfaces of the current pipeline reference
        // .grammar files exclusively: no loader, detector or configuration
        // under src/ mentions the legacy .gf extension.
        var srcCsFiles = Directory.EnumerateFiles(Path.Join(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("ExampleGrammarVerificationTests.cs", StringComparison.Ordinal));
        foreach (var file in srcCsFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain(".gf\"", content);
        }
    }

    [Fact]
    public void GrammarConfigExample_ReferencesOnlyGrammarFiles()
    {
        var root = FindRepoRoot();
        var configPath = Path.Join(root, "examples", "grammar-config", "minotaur.grammar.json");
        Assert.True(File.Exists(configPath), "examples/grammar-config/minotaur.grammar.json is missing");

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var rootElement = document.RootElement;

        // The example config is the current-format counterpart of the legacy
        // .gf files: every grammar reference points at a .grammar file.
        Assert.EndsWith(".grammar", rootElement.GetProperty("defaultGrammar").GetString());
        foreach (var mapping in rootElement.GetProperty("extensionMappings").EnumerateObject())
        {
            var grammar = mapping.Value.GetProperty("grammar").GetString();
            Assert.EndsWith(".grammar", grammar);
        }
    }

    // ---------------------------------------------------------------------
    // Example test data files
    // ---------------------------------------------------------------------

    public static TheoryData<string> JsonExampleFiles => new()
    {
        "examples/data_formats/json/simple_object.json",
        "examples/data_formats/json/complex_structure.json",
    };

    [Theory]
    [MemberData(nameof(JsonExampleFiles))]
    public void JsonExamples_ParseAsValidJson(string relativePath)
    {
        using var document = JsonDocument.Parse(ReadRepoFile(relativePath));
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    public static TheoryData<string> CsvExampleFiles => new()
    {
        "examples/data_formats/csv/basic_data.csv",
        "examples/data_formats/csv/quoted_fields.csv",
    };

    [Theory]
    [MemberData(nameof(CsvExampleFiles))]
    public void CsvExamples_HaveConsistentColumnCounts(string relativePath)
    {
        var lines = ReadRepoFile(relativePath)
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToList();
        Assert.True(lines.Count > 1, $"{relativePath}: expected a header row and data rows");

        var columns = -1;
        foreach (var line in lines)
        {
            var fieldCount = SplitCsvLine(line).Count;
            if (columns == -1)
            {
                columns = fieldCount;
            }
            else
            {
                Assert.True(fieldCount == columns,
                    $"{relativePath}: row has {fieldCount} fields, header has {columns}");
            }
        }
    }

    public static TheoryData<string> OtherExampleFiles => new()
    {
        "examples/postal/danish_addresses.txt",
        "examples/postal/us_addresses.txt",
        "examples/postal/international_addresses.txt",
        "examples/hyperlambda/basic_example.hl",
        "examples/hyperlambda/advanced_example.hl",
        "examples/programming/arithmetic/simple_expressions.txt",
        "examples/embedded_test_simple.html",
        "examples/embedded_test_complex.html",
        "examples/embedded_validation_test.html",
        "examples/embedded-grammars/html-examples/simple-example.html",
        "examples/embedded-grammars/html-examples/complex-example.html",
    };

    [Theory]
    [MemberData(nameof(OtherExampleFiles))]
    public void OtherExamples_ExistAndAreNonEmpty(string relativePath)
    {
        var content = ReadRepoFile(relativePath);
        Assert.False(string.IsNullOrWhiteSpace(content), $"{relativePath} is empty");
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static string ReadRepoFile(string relativePath)
    {
        var root = FindRepoRoot();
        var fullPath = Path.Join(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath), $"Example file missing: {relativePath}");
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Counts the structural brace balance of a legacy .gf grammar: comments,
    /// string literals and EBNF character classes (<c>[...]</c>, which can
    /// legitimately contain an unpaired <c>{</c>) are ignored.
    /// </summary>
    private static int CountBraceBalance(string content)
    {
        var balance = 0;
        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            switch (c)
            {
                case '/' when i + 1 < content.Length && content[i + 1] == '*':
                    var end = content.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end == -1 ? content.Length - 1 : end + 1;
                    break;
                case '/' when i + 1 < content.Length && content[i + 1] == '/':
                    var newline = content.IndexOf('\n', i);
                    i = newline == -1 ? content.Length - 1 : newline - 1;
                    break;
                case '\'':
                case '"':
                    i = SkipQuoted(content, i, c);
                    break;
                case '[':
                    i = SkipCharClass(content, i);
                    break;
                case '{':
                    balance++;
                    break;
                case '}':
                    balance--;
                    break;
            }
        }

        return balance;
    }

    private static int SkipQuoted(string content, int start, char quote)
    {
        for (var i = start + 1; i < content.Length; i++)
        {
            if (content[i] == '\\')
            {
                i++;
            }
            else if (content[i] == quote)
            {
                return i;
            }
        }

        return content.Length - 1;
    }

    private static int SkipCharClass(string content, int start)
    {
        for (var i = start + 1; i < content.Length; i++)
        {
            if (content[i] == '\\')
            {
                i++;
            }
            else if (content[i] == ']')
            {
                return i;
            }
        }

        return content.Length - 1;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
