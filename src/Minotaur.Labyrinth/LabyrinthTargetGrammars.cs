namespace Minotaur.Labyrinth;

/// <summary>
/// One Labyrinth target grammar: a language grammar in the
/// Minotaur-Grammars repository that ships an optional
/// <c>Labyrinth.extension</c> overlay next to its <c>.grammar</c> file
/// (Minotaur-Grammars issue #184), and can therefore host Labyrinth
/// security-check rule patterns written in that language.
/// </summary>
public sealed record LabyrinthTargetGrammar
{
    /// <summary>Repository-relative directory of the grammar (e.g. <c>programming-languages/csharp10</c>).</summary>
    public required string Directory { get; init; }

    /// <summary>File name of the main grammar file (e.g. <c>CSharp10.grammar</c>).</summary>
    public required string MainFile { get; init; }
}

/// <summary>
/// The registry of Labyrinth target grammars (the languages Labyrinth
/// security-check rules can be written against). Each entry's
/// <c>Labyrinth.extension</c> overlay — downloaded at build time together
/// with the grammar itself by the <c>DownloadLabyrinthGrammars</c> target —
/// adds the Labyrinth pattern operators (<c>$NAME</c> metavariable,
/// <c>...</c> ellipsis) on top of the language grammar through StepParser
/// overlay composition (DevelApp-ai/ENFAStepLexer-StepParser#65, #66).
/// </summary>
public static class LabyrinthTargetGrammars
{
    /// <summary>The initial Labyrinth target grammars (Minotaur-Grammars issue #184).</summary>
    public static readonly IReadOnlyList<LabyrinthTargetGrammar> All = new LabyrinthTargetGrammar[]
    {
        new() { Directory = "programming-languages/csharp10", MainFile = "CSharp10.grammar" },
        new() { Directory = "programming-languages/typescript", MainFile = "TypeScript.grammar" },
        new() { Directory = "programming-languages/javascriptes2022", MainFile = "JavaScriptES2022.grammar" },
        new() { Directory = "programming-languages/python311", MainFile = "Python311.grammar" },
        new() { Directory = "programming-languages/java17", MainFile = "Java17.grammar" },
        new() { Directory = "programming-languages/go119", MainFile = "Go119.grammar" },
        new() { Directory = "programming-languages/rust2021", MainFile = "Rust2021.grammar" },
        new() { Directory = "programming-languages/cpp20", MainFile = "Cpp20.grammar" },
    };
}
