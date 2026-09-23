using DevelApp.StepParser;
using Minotaur.Core.Models.Grammar;
using Minotaur.Projects.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Verifies that every Labyrinth target grammar (the languages Labyrinth
/// security-check rules are written against) is fetched from Minotaur-Grammars
/// at build time together with its <c>Labyrinth.extension</c> overlay, and
/// that the overlay actually composes with the target grammar through the real
/// DevelApp.StepParser overlay engine (ENFAStepLexer-StepParser#65, #66;
/// Minotaur-Grammars issue #184). This gates the overlays in CI now that the
/// engine-side overlay support has landed.
/// </summary>
public sealed class LabyrinthTargetGrammarOverlayTests
{
    private readonly ITestOutputHelper _output;
    private static readonly string[] OperatorTokens = ["LABYRINTH_METAVAR", "LABYRINTH_ELLIPSIS"];
    private static readonly string[] OperatorRules = ["labyrinth-metavar", "labyrinth-ellipsis"];

    public LabyrinthTargetGrammarOverlayTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string FindGrammarsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Join(dir.FullName, "Grammars")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, "Grammars directory not found; the build-time download target should copy it.");
        return Path.Join(dir.FullName, "Grammars");
    }

    public static IEnumerable<object[]> Targets() =>
        Minotaur.Labyrinth.LabyrinthTargetGrammars.All.Select(t => new object[] { t });

    private static (string BaseContent, string ExtensionPath) LoadTarget(Minotaur.Labyrinth.LabyrinthTargetGrammar target, string root)
    {
        var dir = Path.Join(root, "targets", target.Directory.Replace('/', Path.DirectorySeparatorChar));
        var baseFile = Path.Join(dir, target.MainFile);
        var extensionFile = Path.Join(dir, "Labyrinth.extension");
        Assert.True(File.Exists(baseFile), $"{target.MainFile} was not downloaded from Minotaur-Grammars at build time.");
        Assert.True(File.Exists(extensionFile), $"Labyrinth.extension for {target.MainFile} was not downloaded from Minotaur-Grammars at build time (it is committed next to the grammar; Minotaur-Grammars issue #184).");
        return (File.ReadAllText(baseFile), extensionFile);
    }

    [Fact]
    public void Registry_CoversAllProgrammingLanguages()
    {
        var dirs = Minotaur.Labyrinth.LabyrinthTargetGrammars.All.Select(t => t.Directory).ToList();
        Assert.Equal(36, dirs.Count);
        Assert.All(dirs, d => Assert.StartsWith("programming-languages/", d));
        Assert.DoesNotContain(dirs, d => d.EndsWith("extensionfile"));
        Assert.DoesNotContain(dirs, d => d.EndsWith("folderproject"));
        Assert.DoesNotContain(dirs, d => d.EndsWith("grammarfile"));
        Assert.DoesNotContain(dirs, d => d.EndsWith("sample"));
        Assert.DoesNotContain(dirs, d => d.EndsWith("dotnetproject"));
        // spot-check a few languages across the alphabet
        Assert.Contains("programming-languages/c17", dirs);
        Assert.Contains("programming-languages/csharp10", dirs);
        Assert.Contains("programming-languages/kotlin", dirs);
        Assert.Contains("programming-languages/swift", dirs);
        Assert.Contains("programming-languages/xml", dirs);
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public async Task Extension_ParsesThroughGrammarExtensionLoader(Minotaur.Labyrinth.LabyrinthTargetGrammar target)
    {
        var (_, extensionPath) = LoadTarget(target, FindGrammarsRoot());
        var result = await Minotaur.Projects.Grammar.GrammarExtensionLoader.LoadFromFileAsync(extensionPath);

        Assert.True(result.Success, $"{target.MainFile}: {string.Join("; ", result.Errors.Select(e => e.ToString()))}");
        var extension = result.Extension!;
        Assert.Equal(target.MainFile, extension.BaseGrammarRef);
        Assert.Equal(ExtensionMergeStrategy.Additive, extension.MergeStrategy);

        foreach (var token in OperatorTokens)
        {
            Assert.Contains(extension.Entries, e => e.Name == token && !e.IsRule);
        }

        foreach (var rule in OperatorRules)
        {
            Assert.Contains(extension.Entries, e => e.Name == rule && e.IsRule);
        }
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public async Task Overlay_ComposesWithTargetGrammarThroughStepParser(Minotaur.Labyrinth.LabyrinthTargetGrammar target)
    {
        var (baseContent, extensionPath) = LoadTarget(target, FindGrammarsRoot());
        var extension = (await Minotaur.Projects.Grammar.GrammarExtensionLoader.LoadFromFileAsync(extensionPath)).Extension!;

        // The overlay is rendered to grammar content and composed with the
        // target grammar through the real StepParser overlay engine.
        var overlayContent = GrammarExtensionOverlayRenderer.RenderOverlay(extension);
        var loader = new GrammarLoader();
        var merged = loader.ComposeWithOverlayContent(
            baseContent,
            overlayContent,
            OverlayConflictResolution.Additive,
            baseFileName: target.MainFile,
            overlayFileName: "Labyrinth.extension");

        Assert.All(merged.Conflicts, c => _output.WriteLine($"{target.MainFile} conflict: {c}"));

        // The pattern-operator tokens and their production-position wrappers
        // must be present in the merged grammar: they are what makes Labyrinth
        // security-check patterns ($NAME, ...) parseable in this language.
        foreach (var token in OperatorTokens)
        {
            Assert.Contains(merged.Grammar.TokenRules, r => r.Name == token);
        }

        foreach (var rule in OperatorRules)
        {
            Assert.Contains(merged.Grammar.ProductionRules, r => r.Name == rule);
        }
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public async Task Overlay_IsAdditive_TargetProductionsArePreserved(Minotaur.Labyrinth.LabyrinthTargetGrammar target)
    {
        var (baseContent, extensionPath) = LoadTarget(target, FindGrammarsRoot());
        var extension = (await Minotaur.Projects.Grammar.GrammarExtensionLoader.LoadFromFileAsync(extensionPath)).Extension!;

        var loader = new GrammarLoader();
        var baseGrammar = loader.ParseGrammarContent(baseContent, target.MainFile);
        var overlayContent = GrammarExtensionOverlayRenderer.RenderOverlay(extension);
        var merged = loader.ComposeWithOverlayContent(baseContent, overlayContent, OverlayConflictResolution.Additive,
            baseFileName: target.MainFile).Grammar;

        // Every production rule of the target grammar survives the merge:
        // the Labyrinth overlay adds pattern operators, it never removes or
        // narrows the language itself.
        var mergedNames = merged.ProductionRules.Select(r => r.Name).ToHashSet();
        foreach (var rule in baseGrammar.ProductionRules)
        {
            Assert.True(mergedNames.Contains(rule.Name), $"Target production '{rule.Name}' was lost by the Labyrinth overlay merge.");
        }
    }
}
