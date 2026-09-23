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
    /// sample, dotnetproject) are intentionally excluded.
    /// </summary>
    public static readonly IReadOnlyList<LabyrinthTargetGrammar> All = new LabyrinthTargetGrammar[]
    {
        new() { Directory = "programming-languages/c17", MainFile = "C17.grammar" },
        new() { Directory = "programming-languages/classicasp", MainFile = "ClassicASP.grammar" },
        new() { Directory = "programming-languages/clojure", MainFile = "Clojure.grammar" },
        new() { Directory = "programming-languages/cobol2023", MainFile = "COBOL2023.grammar" },
        new() { Directory = "programming-languages/cpp20", MainFile = "Cpp20.grammar" },
        new() { Directory = "programming-languages/csharp10", MainFile = "CSharp10.grammar" },
        new() { Directory = "programming-languages/css", MainFile = "CSS.grammar" },
        new() { Directory = "programming-languages/dart", MainFile = "Dart.grammar" },
        new() { Directory = "programming-languages/elixir", MainFile = "Elixir.grammar" },
        new() { Directory = "programming-languages/erlang", MainFile = "Erlang.grammar" },
        new() { Directory = "programming-languages/fsharp", MainFile = "FSharp.grammar" },
        new() { Directory = "programming-languages/go119", MainFile = "Go119.grammar" },
        new() { Directory = "programming-languages/haskell", MainFile = "Haskell.grammar" },
        new() { Directory = "programming-languages/htmlembedded", MainFile = "HTMLEmbedded.grammar" },
        new() { Directory = "programming-languages/java", MainFile = "Java.grammar" },
        new() { Directory = "programming-languages/java17", MainFile = "Java17.grammar" },
        new() { Directory = "programming-languages/javascript", MainFile = "JavaScript.grammar" },
        new() { Directory = "programming-languages/javascriptes2022", MainFile = "JavaScriptES2022.grammar" },
        new() { Directory = "programming-languages/json", MainFile = "JSON.grammar" },
        new() { Directory = "programming-languages/jsonschema", MainFile = "JSONSchema.grammar" },
        new() { Directory = "programming-languages/kotlin", MainFile = "Kotlin.grammar" },
        new() { Directory = "programming-languages/lua", MainFile = "Lua.grammar" },
        new() { Directory = "programming-languages/perl", MainFile = "Perl.grammar" },
        new() { Directory = "programming-languages/php", MainFile = "PHP.grammar" },
        new() { Directory = "programming-languages/pl1", MainFile = "PL1.grammar" },
        new() { Directory = "programming-languages/python311", MainFile = "Python311.grammar" },
        new() { Directory = "programming-languages/r", MainFile = "RLang.grammar" },
        new() { Directory = "programming-languages/ruby", MainFile = "Ruby.grammar" },
        new() { Directory = "programming-languages/rust2021", MainFile = "Rust2021.grammar" },
        new() { Directory = "programming-languages/scala", MainFile = "Scala.grammar" },
        new() { Directory = "programming-languages/swift", MainFile = "Swift.grammar" },
        new() { Directory = "programming-languages/typescript", MainFile = "TypeScript.grammar" },
        new() { Directory = "programming-languages/visualbasic", MainFile = "VisualBasic.grammar" },
        new() { Directory = "programming-languages/webassembly20", MainFile = "WebAssembly20.grammar" },
        new() { Directory = "programming-languages/xaml", MainFile = "XAML.grammar" },
        new() { Directory = "programming-languages/xml", MainFile = "XML.grammar" },
    };
}
