using Minotaur.Cli;
using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Grammar-root resolution tests for <see cref="ScanHost.GrammarFilePath"/>
/// (Minotaur issue #254 follow-up, Labyrinth L3 article lane): the download
/// layout ("<c>Grammars/targets/&lt;dir&gt;/&lt;file&gt;</c>", populated by the
/// <c>DownloadLabyrinthGrammars</c> build target) must keep resolving
/// unchanged, and the Minotaur-Grammars checkout layout
/// ("<c>&lt;dir&gt;/&lt;file&gt;</c>", used by the article-lane harnesses via
/// <c>MINOTAUR_GRAMMARS_DIR</c>) must resolve as a fallback. When neither
/// exists the canonical download-layout path is returned so "not found"
/// diagnostics stay stable.
/// </summary>
public sealed class ScanHostGrammarFilePathTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "minotaur-scanhost-grammarpath-" + Guid.NewGuid().ToString("N"));

    private static LabyrinthTargetGrammar Target() => ScanHost.ResolveTarget("csharp10");

    private string WriteGrammar(params string[] segments)
    {
        var relative = Path.Combine(segments);
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "Grammar: Stub");
        return path;
    }

    [Fact]
    public void DownloadLayout_PrefersTargetsSubdirectory()
    {
        var downloaded = WriteGrammar("targets", "programming-languages", "csharp10", "CSharp10.grammar");
        WriteGrammar("programming-languages", "csharp10", "CSharp10.grammar"); // stale checkout-style copy must lose

        var resolved = ScanHost.GrammarFilePath(_root, Target());

        Assert.Equal(downloaded, resolved);
    }

    [Fact]
    public void CheckoutLayout_FallsBackWithoutTargetsSubdirectory()
    {
        var checkedOut = WriteGrammar("programming-languages", "csharp10", "CSharp10.grammar");

        var resolved = ScanHost.GrammarFilePath(_root, Target());

        Assert.Equal(checkedOut, resolved);
        Assert.True(File.Exists(resolved));
    }

    [Fact]
    public void MissingInBothLayouts_ReturnsCanonicalDownloadPath()
    {
        Directory.CreateDirectory(_root);

        var resolved = ScanHost.GrammarFilePath(_root, Target());

        Assert.Equal(
            Path.Combine(_root, "targets", "programming-languages", "csharp10", "CSharp10.grammar"),
            resolved);
        Assert.False(File.Exists(resolved));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
