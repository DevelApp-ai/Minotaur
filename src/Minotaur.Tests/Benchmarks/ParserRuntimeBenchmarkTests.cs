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

using Antlr4.Runtime;
using DevelApp.StepParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Benchmarks.ParserRuntime;
using Minotaur.Benchmarks.ParserRuntime.Antlr.Generated;
using Minotaur.Benchmarks.ParserRuntime.HandWritten;

namespace Minotaur.Tests.Benchmarks;

/// <summary>
/// Tests for the issue #127 parser-runtime benchmark assets: the three
/// case-study GrammarForge grammars must parse their corpus documents with
/// full consumption in the shipped RealTimeParserSession, the hand-written
/// and ANTLR baselines must accept the same documents, malformed documents
/// must be rejected by every engine, and the deterministic corpus generator
/// must reproduce the committed corpus files byte for byte.
/// </summary>
[TestClass]
public class ParserRuntimeBenchmarkTests
{
    private const int SampleDocs = 25;

    // ------------------------------------------------------------------
    // Minotaur engine: grammar full-consumption positives
    // ------------------------------------------------------------------

    [TestMethod]
    public void MinotaurConfigGrammar_ParsesCorpusDocuments_WithFullConsumption()
    {
        AssertMinotaurAccepts(CaseStudyDsls.Config, CaseDsl.Config);
    }

    [TestMethod]
    public void MinotaurQueryGrammar_ParsesCorpusDocuments_WithFullConsumption()
    {
        AssertMinotaurAccepts(CaseStudyDsls.Query, CaseDsl.Query);
    }

    [TestMethod]
    public void MinotaurTemplateGrammar_ParsesCorpusDocuments_WithFullConsumption()
    {
        AssertMinotaurAccepts(CaseStudyDsls.Template, CaseDsl.Template);
    }

    // ------------------------------------------------------------------
    // Minotaur engine: garbage rejection negatives
    // ------------------------------------------------------------------

    [TestMethod]
    public void MinotaurConfigGrammar_RejectsGarbage()
    {
        Assert.IsFalse(ParseMinotaurValid(
            CaseStudyDsls.Config, "this is not a config document at all\n"),
            "Garbage text must not produce a valid full-consumption parse.");
        Assert.IsFalse(ParseMinotaurValid(
            CaseStudyDsls.Config, "[server]\nport = \n"),
            "A document truncated before the fixed slot skeleton must be rejected.");
    }

    [TestMethod]
    public void MinotaurQueryGrammar_RejectsGarbage()
    {
        // Eleven well-formed words, but no equals/string/number slots: the
        // flat root skeleton must not complete.
        Assert.IsFalse(ParseMinotaurValid(
            CaseStudyDsls.Query, "nonsense nonsense nonsense nonsense nonsense nonsense nonsense nonsense nonsense nonsense nonsense\n"),
            "A statement without the fixed slot skeleton must be rejected.");
    }

    [TestMethod]
    public void MinotaurTemplateGrammar_RejectsGarbage()
    {
        Assert.IsFalse(ParseMinotaurValid(
            CaseStudyDsls.Template, "{{sql}} never terminated\n"),
            "An unterminated block must not produce a valid full-consumption parse.");
    }

    // ------------------------------------------------------------------
    // Hand-written baselines
    // ------------------------------------------------------------------

    [TestMethod]
    public void HandWrittenConfigParser_AcceptsCorpusDocuments()
    {
        var parser = new HandWrittenConfigParser();
        for (var i = 0; i < SampleDocs; i++)
        {
            var result = parser.Parse(Corpus.Document(CaseDsl.Config, i));
            Assert.IsTrue(result.Ok, $"Config document {i} must parse: {result.Error}");
        }
    }

    [TestMethod]
    public void HandWrittenQueryParser_AcceptsCorpusDocuments()
    {
        var parser = new HandWrittenQueryParser();
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.IsTrue(parser.Parse(Corpus.Document(CaseDsl.Query, i)),
                $"Query document {i} must parse.");
        }
    }

    [TestMethod]
    public void HandWrittenTemplateParser_AcceptsCorpusDocuments()
    {
        var parser = new HandWrittenTemplateParser();
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.IsTrue(parser.Parse(Corpus.Document(CaseDsl.Template, i)),
                $"Template document {i} must parse.");
        }
    }

    // ------------------------------------------------------------------
    // ANTLR baselines
    // ------------------------------------------------------------------

    [TestMethod]
    public void AntlrConfigParser_AcceptsCorpusDocuments()
    {
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.AreEqual(0, CountAntlrErrorsConfig(Corpus.Document(CaseDsl.Config, i)),
                $"Config document {i} must parse without syntax errors.");
        }
    }

    [TestMethod]
    public void AntlrQueryParser_AcceptsCorpusDocuments()
    {
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.AreEqual(0, CountAntlrErrorsQuery(Corpus.Document(CaseDsl.Query, i)),
                $"Query document {i} must parse without syntax errors.");
        }
    }

    [TestMethod]
    public void AntlrTemplateParser_AcceptsCorpusDocuments()
    {
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.AreEqual(0, CountAntlrErrorsTemplate(Corpus.Document(CaseDsl.Template, i)),
                $"Template document {i} must parse without syntax errors.");
        }
    }

    [TestMethod]
    public void AntlrQueryParser_RejectsMalformedStatement()
    {
        // Parser error: missing WINDOW clause (entry rule ends in EOF).
        Assert.IsTrue(CountAntlrErrorsQuery(
            "select metric_1 from sales_data_1 where region_1 = \"region 1\"\n") > 0,
            "A truncated statement must produce a parser syntax error.");

        // Lexer error: unterminated string (counted by the error listener).
        Assert.IsTrue(CountAntlrErrorsQuery(
            "select metric_1 from sales_data_1 where region_1 = \"region 1 window 2 unit_1\n") > 0,
            "An unterminated string literal must produce a lexer syntax error.");
    }

    // ------------------------------------------------------------------
    // Corpus determinism and size targets
    // ------------------------------------------------------------------

    [TestMethod]
    public void Corpus_CommittedFiles_MatchGeneratorByteForByte()
    {
        var corpusDir = Path.Join(FindRepoRoot(), "docs", "corpus", "issue-127");
        Assert.IsTrue(Directory.Exists(corpusDir),
            $"Committed corpus directory must exist: {corpusDir}");

        foreach (var dslName in new[] { "config", "query", "template" })
        {
            foreach (var sizeName in new[] { "small", "medium" })
            {
                var path = Path.Join(corpusDir, $"{dslName}-{sizeName}.txt");
                Assert.IsTrue(File.Exists(path), $"Committed corpus file must exist: {path}");
                var committed = File.ReadAllText(path);
                var generated = Corpus.Render(ParseDsl(dslName), ParseSize(sizeName));
                Assert.AreEqual(generated, committed,
                    $"Committed corpus file must match the deterministic generator: {path}");
            }
        }
    }

    [TestMethod]
    public void Corpus_MeetsLineCountTargets()
    {
        // Config: documents of 7 lines each.
        Assert.AreEqual(142 * 7, Lines(CaseDsl.Config, CorpusSize.Small));
        Assert.AreEqual(1428 * 7, Lines(CaseDsl.Config, CorpusSize.Medium));
        Assert.AreEqual(14285 * 7, Lines(CaseDsl.Config, CorpusSize.Large));

        // Query and template: one document per line.
        foreach (var dsl in new[] { CaseDsl.Query, CaseDsl.Template })
        {
            Assert.AreEqual(1_000, Lines(dsl, CorpusSize.Small));
            Assert.AreEqual(10_000, Lines(dsl, CorpusSize.Medium));
            Assert.AreEqual(100_000, Lines(dsl, CorpusSize.Large));
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void AssertMinotaurAccepts(string grammar, CaseDsl dsl)
    {
        for (var i = 0; i < SampleDocs; i++)
        {
            Assert.IsTrue(ParseMinotaurValid(grammar, Corpus.Document(dsl, i)),
                $"{dsl} document {i} must parse with full consumption.");
        }
    }

    /// <summary>
    /// The benchmark's validity check: Success AND the last resolved token
    /// ends at the end of the document (Success=True alone is a known false
    /// positive for partial consumption).
    /// </summary>
    private static bool ParseMinotaurValid(string grammar, string document)
    {
        try
        {
            var session = new RealTimeParserSession(grammar);
            session.Initialize(document);
            var result = session.Parse();
            var last = result.Success ? result.ResolvedTokens?.LastOrDefault() : null;
            return last is not null
                && last.StartPosition + last.Length == document.TrimEnd().Length;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static int CountAntlrErrorsConfig(string document)
    {
        var lexer = new ConfigDslLexer(new AntlrInputStream(document));
        lexer.RemoveErrorListeners();
        var lexerErrors = new CountingErrorListener();
        lexer.AddErrorListener(lexerErrors);
        var parser = new ConfigDslParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.config_file();
        return lexerErrors.Count + parser.NumberOfSyntaxErrors;
    }

    private static int CountAntlrErrorsQuery(string document)
    {
        var lexer = new QueryDslLexer(new AntlrInputStream(document));
        lexer.RemoveErrorListeners();
        var lexerErrors = new CountingErrorListener();
        lexer.AddErrorListener(lexerErrors);
        var parser = new QueryDslParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.query();
        return lexerErrors.Count + parser.NumberOfSyntaxErrors;
    }

    private static int CountAntlrErrorsTemplate(string document)
    {
        var lexer = new TemplateDslLexer(new AntlrInputStream(document));
        lexer.RemoveErrorListeners();
        var lexerErrors = new CountingErrorListener();
        lexer.AddErrorListener(lexerErrors);
        var parser = new TemplateDslParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.template();
        return lexerErrors.Count + parser.NumberOfSyntaxErrors;
    }

    private static int Lines(CaseDsl dsl, CorpusSize size) =>
        Corpus.CountLines(Corpus.Render(dsl, size));

    private static CaseDsl ParseDsl(string name) => name switch
    {
        "config" => CaseDsl.Config,
        "query" => CaseDsl.Query,
        "template" => CaseDsl.Template,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static CorpusSize ParseSize(string name) => name switch
    {
        "small" => CorpusSize.Small,
        "medium" => CorpusSize.Medium,
        "large" => CorpusSize.Large,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Join(directory.FullName, "src"))
                && Directory.Exists(Path.Join(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (src/ + docs/) from " + AppContext.BaseDirectory);
    }

    /// <summary>Counts lexer syntax errors (the generated C# lexers do not
    /// expose NumberOfSyntaxErrors the way the parsers do).</summary>
    private sealed class CountingErrorListener : IAntlrErrorListener<int>
    {
        public int Count { get; private set; }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException recognitionException)
        {
            this.Count++;
        }
    }
}
