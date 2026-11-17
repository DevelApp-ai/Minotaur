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
    /// <summary>
    /// Every language grammar under <c>programming-languages/</c> in Minotaur-Grammars
    /// (36 languages; Minotaur-Grammars issue #184 and follow-up PR #187). The
    /// meta/project grammars (extensionfile, folderproject, grammarfile,
    /// sample, dotnetproject) are intentionally excluded. The Java target is
    /// the versioned <c>java8</c> grammar (Java SE 8), replacing the unversioned
    /// legacy <c>java</c> grammar (Minotaur-Grammars issue #189).
    /// </summary>
    public static readonly IReadOnlyList<LabyrinthTargetGrammar> All = new LabyrinthTargetGrammar[]
    {
        new() { Directory = "programming-languages/c17", MainFile = "C17.grammar" },
        new() { Directory = "programming-languages/classicasp001", MainFile = "ClassicASP001.grammar" },
        new() { Directory = "programming-languages/clojure001", MainFile = "Clojure001.grammar" },
        new() { Directory = "programming-languages/cobol2023", MainFile = "COBOL2023.grammar" },
        new() { Directory = "programming-languages/cpp20", MainFile = "Cpp20.grammar" },
        new() { Directory = "programming-languages/csharp10", MainFile = "CSharp10.grammar" },
        new() { Directory = "programming-languages/css", MainFile = "CSS.grammar" },
        new() { Directory = "programming-languages/dart001", MainFile = "Dart001.grammar" },
        new() { Directory = "programming-languages/elixir001", MainFile = "Elixir001.grammar" },
        new() { Directory = "programming-languages/erlang001", MainFile = "Erlang001.grammar" },
        new() { Directory = "programming-languages/fsharp001", MainFile = "FSharp001.grammar" },
        new() { Directory = "programming-languages/go119", MainFile = "Go119.grammar" },
        new() { Directory = "programming-languages/haskell001", MainFile = "Haskell001.grammar" },
        new() { Directory = "programming-languages/htmlembedded", MainFile = "HTMLEmbedded.grammar" },
        new() { Directory = "programming-languages/java8", MainFile = "Java8.grammar" },
        new() { Directory = "programming-languages/java17", MainFile = "Java17.grammar" },
        new() { Directory = "programming-languages/javascript001", MainFile = "JavaScript001.grammar" },
        new() { Directory = "programming-languages/javascriptes2022", MainFile = "JavaScriptES2022.grammar" },
        new() { Directory = "programming-languages/json", MainFile = "JSON.grammar" },
        new() { Directory = "programming-languages/jsonschema", MainFile = "JSONSchema.grammar" },
        new() { Directory = "programming-languages/kotlin001", MainFile = "Kotlin001.grammar" },
        new() { Directory = "programming-languages/lua001", MainFile = "Lua001.grammar" },
        new() { Directory = "programming-languages/perl001", MainFile = "Perl001.grammar" },
        new() { Directory = "programming-languages/php001", MainFile = "PHP001.grammar" },
        new() { Directory = "programming-languages/pl1", MainFile = "PL1.grammar" },
        new() { Directory = "programming-languages/python311", MainFile = "Python311.grammar" },
        new() { Directory = "programming-languages/r001", MainFile = "RLang001.grammar" },
        new() { Directory = "programming-languages/ruby001", MainFile = "Ruby001.grammar" },
        new() { Directory = "programming-languages/rust2021", MainFile = "Rust2021.grammar" },
        new() { Directory = "programming-languages/scala001", MainFile = "Scala001.grammar" },
        new() { Directory = "programming-languages/swift001", MainFile = "Swift001.grammar" },
        new() { Directory = "programming-languages/typescript001", MainFile = "TypeScript001.grammar" },
        new() { Directory = "programming-languages/visualbasic001", MainFile = "VisualBasic001.grammar" },
        new() { Directory = "programming-languages/webassembly20", MainFile = "WebAssembly20.grammar" },
        new() { Directory = "programming-languages/xaml", MainFile = "XAML.grammar" },
        new() { Directory = "programming-languages/xml", MainFile = "XML.grammar" },
    };
}
