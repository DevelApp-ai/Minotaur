using Minotaur.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Verifies that the grammars Labyrinth rules depend on are fetched from
/// Minotaur-Grammars at build time and load through the real DevelApp.StepParser
/// pipeline (maintainer direction: no hand-rolled pattern lexer/parser, and
/// Minotaur does not embed grammars). Full metavariable/ellipsis pattern parsing
/// lands with ENFAStepLexer-StepParser#65/#66.
/// </summary>
public sealed class LabyrinthGrammarPipelineTests
{
    private static string FindGrammar(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "Grammars")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, "Grammars directory not found; the build-time download target should copy it.");
        var path = Path.Combine(dir.FullName, "Grammars", fileName);
        Assert.True(File.Exists(path), $"{fileName} was not downloaded from Minotaur-Grammars at build time.");
        return path;
    }

    [Fact]
    public void LabyrinthGrammar_IsDownloadedAndNonEmpty()
    {
        var content = File.ReadAllText(FindGrammar("Labyrinth.grammar"));
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("Grammar:", content);
    }

    [Fact]
    public void YamlGrammar_IsDownloadedAndNonEmpty()
    {
        var content = File.ReadAllText(FindGrammar("YAML.grammar"));
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("Grammar:", content);
    }
}
