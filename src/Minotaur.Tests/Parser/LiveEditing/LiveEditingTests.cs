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

using DevelApp.StepParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Parser.LiveEditing;

namespace Minotaur.Tests.Parser.LiveEditing;

[TestClass]
public class TextEditCalculatorTests
{
    [TestMethod]
    public void Compute_Insert_AtStart_ProducesInsertOnlyEdit()
    {
        var edit = TextEditCalculator.Compute("bc", "abc");

        Assert.IsNotNull(edit);
        Assert.AreEqual(0, edit.Value.Offset);
        Assert.AreEqual(0, edit.Value.DeleteLength);
        Assert.AreEqual("a", edit.Value.InsertText);
    }

    [TestMethod]
    public void Compute_Delete_AtEnd_ProducesDeleteOnlyEdit()
    {
        var edit = TextEditCalculator.Compute("abcd", "abc");

        Assert.IsNotNull(edit);
        Assert.AreEqual(3, edit.Value.Offset);
        Assert.AreEqual(1, edit.Value.DeleteLength);
        Assert.AreEqual(string.Empty, edit.Value.InsertText);
    }

    [TestMethod]
    public void Compute_Replace_InMiddle_ProducesSingleRegion()
    {
        var edit = TextEditCalculator.Compute("var count = 0;", "var limit = 0;");

        // Common prefix "var " and common suffix "t = 0;" (the calculator
        // maximizes the suffix), so only "coun" is replaced by "limi".
        Assert.IsNotNull(edit);
        Assert.AreEqual(4, edit.Value.Offset);
        Assert.AreEqual(4, edit.Value.DeleteLength);
        Assert.AreEqual("limi", edit.Value.InsertText);
    }

    [TestMethod]
    public void Compute_BlockPaste_IsASingleInsertRegion()
    {
        var oldText = "line one\nline two\n";
        var newText = "line one\nline two\nline three\nline four\n";

        var edit = TextEditCalculator.Compute(oldText, newText);

        Assert.IsNotNull(edit);
        Assert.AreEqual(oldText.Length, edit.Value.Offset);
        Assert.AreEqual(0, edit.Value.DeleteLength);
        Assert.AreEqual("line three\nline four\n", edit.Value.InsertText);
    }

    [TestMethod]
    public void Compute_UnchangedText_ReturnsNull()
    {
        Assert.IsNull(TextEditCalculator.Compute("same", "same"));
    }

    [TestMethod]
    public void Compute_UnrelatedTexts_ReplaceWholeDocument()
    {
        var edit = TextEditCalculator.Compute("alpha", "omega");

        // No common prefix; the common suffix "a" is trimmed, leaving
        // "alph" -> "omeg".
        Assert.IsNotNull(edit);
        Assert.AreEqual(0, edit.Value.Offset);
        Assert.AreEqual(4, edit.Value.DeleteLength);
        Assert.AreEqual("omeg", edit.Value.InsertText);
    }

    [TestMethod]
    public void Compute_NonAsciiInput_Throws()
    {
        Assert.ThrowsException<NotSupportedException>(() => TextEditCalculator.Compute("caf\u00e9", "cafe"));
    }

    [TestMethod]
    public void RequireAscii_AsciiText_DoesNotThrow()
    {
        TextEditCalculator.RequireAscii("printable ascii text", "text");
    }

    [TestMethod]
    public void RequireAscii_NonAsciiText_Throws()
    {
        Assert.ThrowsException<NotSupportedException>(() => TextEditCalculator.RequireAscii("caf\u00e9", "text"));
    }
}

[TestClass]
public class LatencyRecorderTests
{
    [TestMethod]
    public void Percentile_NearestRank_ComputesExpectedValues()
    {
        var recorder = new LatencyRecorder();

        // 1..100 microseconds: nearest-rank percentile of p is ceil(p/100 * n)-th value.
        for (var i = 1; i <= 100; i++)
        {
            recorder.AddMicroseconds(i);
        }

        Assert.AreEqual(50, recorder.PercentileMicroseconds(50), 1e-9);
        Assert.AreEqual(95, recorder.PercentileMicroseconds(95), 1e-9);
        Assert.AreEqual(99, recorder.PercentileMicroseconds(99), 1e-9);
        Assert.AreEqual(50.5, recorder.MeanMicroseconds, 1e-9);
    }

    [TestMethod]
    public void Percentile_SingleSample_ReturnsThatSample()
    {
        var recorder = new LatencyRecorder();
        recorder.AddMicroseconds(123.4);

        Assert.AreEqual(123.4, recorder.PercentileMicroseconds(50), 1e-9);
        Assert.AreEqual(123.4, recorder.PercentileMicroseconds(99), 1e-9);
    }

    [TestMethod]
    public void Reset_ClearsSamples()
    {
        var recorder = new LatencyRecorder();
        recorder.AddMicroseconds(10);
        recorder.AddMicroseconds(20);
        recorder.Reset();

        Assert.AreEqual(0, recorder.Count);
        Assert.AreEqual(0, recorder.PercentileMicroseconds(50));
    }

    [TestMethod]
    public void Summary_FromRecorder_ContainsAllPercentiles()
    {
        var recorder = new LatencyRecorder();
        for (var i = 1; i <= 10; i++)
        {
            recorder.AddMicroseconds(i * 10.0);
        }

        var summary = LatencySummary.From(recorder);

        Assert.AreEqual(10, summary.Count);
        Assert.AreEqual(recorder.PercentileMicroseconds(50), summary.P50Microseconds);
        Assert.AreEqual(recorder.PercentileMicroseconds(95), summary.P95Microseconds);
        Assert.AreEqual(recorder.PercentileMicroseconds(99), summary.P99Microseconds);
        Assert.AreEqual(10, summary.MinMicroseconds);
        Assert.AreEqual(100, summary.MaxMicroseconds);
    }
}

[TestClass]
public class LiveEditorGrammarsTests
{
    [TestMethod]
    public void CSharpLike_LoadsInRealTimeParserSession_AndLexesSnippet()
    {
        var session = new RealTimeParserSession(LiveEditorGrammars.CSharpLike);
        session.Initialize("""
            public class Sample
            {
                private const string Greeting = "hello";
                private int[] counts = { 1, 42 };
                private int count = 42;

                public int Add(int value)
                {
                    # add the value to the counter
                    count += value;
                    return count == 42 ? count : value;
                }
            }
            """);

        Assert.IsTrue(session.Tokens.Count > 0, "The grammar should produce tokens for the snippet.");
        Assert.AreEqual(
            session.Text.Length,
            session.Tokens[^1].StartPosition + session.Tokens[^1].Length,
            "The token stream must cover the whole snippet without stalling.");

        var types = session.Tokens.Select(t => t.Type).ToHashSet();
        var expected = new[] { "WORD", "NUMBER", "STRING_LITERAL", "COMMENT", "OPERATOR", "PUNCT", "WS" };
        foreach (var type in expected)
        {
            Assert.IsTrue(types.Contains(type), $"Token type {type} expected in the lexed snippet.");
        }
    }

    [TestMethod]
    public void CSharpLike_LexesWholeLineWithoutDuplication()
    {
        var session = new RealTimeParserSession(LiveEditorGrammars.CSharpLike);
        session.Initialize("var x = 1;");

        Assert.AreEqual(
            "WORD,WS,WORD,WS,OPERATOR,WS,NUMBER,PUNCT",
            string.Join(",", session.Tokens.Select(t => t.Type)),
            "The exploratory-mode lexer must emit exactly one token per position.");
    }

    [TestMethod]
    public void CSharpLike_BareMultiplyOperator_LexesWithoutStalling()
    {
        // Regression for issue #196: the single-character operator class
        // lacked bare *, so the lexer hit input no rule matched, every
        // active path died, and the session silently stopped lexing at
        // that offset, truncating every harness corpus to ~50 tokens.
        var session = new RealTimeParserSession(LiveEditorGrammars.CSharpLike);
        session.Initialize("""
            var result = input * 1 + count0;
            """);

        Assert.AreEqual(
            session.Text.Length,
            session.Tokens[^1].StartPosition + session.Tokens[^1].Length,
            "Bare * must match the OPERATOR rule; unmatched input silently ends the token stream.");
        CollectionAssert.AreEqual(
            new[] { "WORD", "WS", "WORD", "WS", "OPERATOR", "WS", "WORD", "WS", "OPERATOR", "WS", "NUMBER", "WS", "OPERATOR", "WS", "WORD", "PUNCT" },
            session.Tokens.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public void CSharpLike_ConsecutiveAccessors_ReturnSameContent()
    {
        var first = LiveEditorGrammars.CSharpLike;
        var second = LiveEditorGrammars.CSharpLike;

        Assert.AreEqual(first, second);
        Assert.IsTrue(first.StartsWith("Grammar:", StringComparison.Ordinal));
    }
}

[TestClass]
public class LiveEditorSessionTests
{
    private const string InitialSource = """
        public class Widget
        {
            private int count = 0;

            public int Calculate(int input)
            {
                var result = input + count;
                return result;
            }
        }
        """;

    [TestMethod]
    public void Open_InitializesTokensAndResetsStatistics()
    {
        var session = CreateSession();

        session.Open(InitialSource);

        Assert.IsTrue(session.TokenCount > 0);
        Assert.AreEqual(0, session.EditCount);
        Assert.AreEqual(0, session.Version);
    }

    [TestMethod]
    public void OnTextChange_NoChange_ReturnsNull()
    {
        var session = CreateSession();
        session.Open(InitialSource);

        Assert.IsNull(session.OnTextChange(InitialSource));
    }

    [TestMethod]
    public void OnTextChange_NonAscii_ThrowsAndDocumentUnchanged()
    {
        var session = CreateSession();
        session.Open(InitialSource);

        Assert.ThrowsException<NotSupportedException>(() => session.OnTextChange(InitialSource + " // caf\u00e9"));

        Assert.AreEqual(InitialSource, session.Text);
    }

    [TestMethod]
    public void OnTextChange_IncrementalMatchesFreshFullRelex_AfterSeededEditSequence()
    {
        var session = CreateSession();
        session.Open(InitialSource);
        var rng = new Random(196);

        for (var i = 0; i < 200; i++)
        {
            var text = session.Text;
            var edit = NextSeededEdit(text, rng, i);
            var newText = text.Remove(edit.Offset, edit.DeleteLength).Insert(edit.Offset, edit.InsertText);

            var latency = session.OnTextChange(newText);

            Assert.IsNotNull(latency, $"Every seeded edit {i} must change the text.");
        }

        // Fresh full re-lex of the final document.
        var fresh = new RealTimeParserSession(LiveEditorGrammars.CSharpLike);
        fresh.Initialize(session.Text);

        Assert.AreEqual(fresh.Tokens.Count, session.TokenCount);

        for (var i = 0; i < fresh.Tokens.Count; i++)
        {
            var expected = fresh.Tokens[i];
            var actual = session.Session.Tokens[i];

            Assert.AreEqual(expected.Type, actual.Type, $"Token {i} type mismatch.");
            Assert.AreEqual(expected.Value, actual.Value, $"Token {i} value mismatch.");
            Assert.AreEqual(expected.StartPosition, actual.StartPosition, $"Token {i} start position mismatch.");
            Assert.AreEqual(expected.Length, actual.Length, $"Token {i} length mismatch.");
        }

        Assert.AreEqual(200, session.EditCount);
        Assert.AreEqual(200, session.Latency.Count);
        Assert.AreEqual(200, session.ReuseRatios.Count);
    }

    [TestMethod]
    public void OnTextChange_FullReparseMode_MatchesIncrementalTokens()
    {
        var incremental = CreateSession();
        var fullReparse = CreateSession();
        fullReparse.Mode = LiveEditingMode.FullReparse;

        incremental.Open(InitialSource);
        fullReparse.Open(InitialSource);

        var rng = new Random(196);
        for (var i = 0; i < 100; i++)
        {
            var text = incremental.Text;
            var edit = NextSeededEdit(text, rng, i);
            var newText = text.Remove(edit.Offset, edit.DeleteLength).Insert(edit.Offset, edit.InsertText);

            incremental.OnTextChange(newText);
            fullReparse.OnTextChange(newText);
        }

        Assert.AreEqual(incremental.TokenCount, fullReparse.TokenCount);

        for (var i = 0; i < incremental.TokenCount; i++)
        {
            Assert.AreEqual(
                incremental.Session.Tokens[i].Value,
                fullReparse.Session.Tokens[i].Value,
                $"Token {i} value mismatch between modes.");
        }

        Assert.AreEqual(0, fullReparse.ReuseRatios.Count, "Full re-parse mode must not record reuse ratios.");
    }

    private static LiveEditorSession CreateSession() => new(LiveEditorGrammars.CSharpLike);

    /// <summary>
    /// Deterministic single-region edits confined to safe anchor points so
    /// every edit is valid for any document state reached from the initial
    /// source with this seed.
    /// </summary>
    private static TextEdit NextSeededEdit(string text, Random rng, int index)
    {
        var lineStarts = new List<int>();
        for (var i = text.IndexOf('\n'); i >= 0 && i < text.Length - 1; i = text.IndexOf('\n', i + 1))
        {
            lineStarts.Add(i + 1);
        }

        var lineStart = lineStarts.Count > 0 ? lineStarts[rng.Next(lineStarts.Count)] : 0;
        var lineEnd = text.IndexOf('\n', lineStart);
        if (lineEnd < 0)
        {
            lineEnd = text.Length;
        }

        switch (index % 4)
        {
            case 0: // single-char insert mid-line
                {
                    var column = lineEnd > lineStart ? rng.Next(lineEnd - lineStart) : 0;
                    return new TextEdit(lineStart + column, 0, "x");
                }

            case 1: // word-completion style insert at first identifier end
                {
                    var line = text[lineStart..lineEnd];
                    var wordEnd = -1;
                    for (var i = 0; i < line.Length; i++)
                    {
                        if (char.IsAsciiLetterOrDigit(line[i]))
                        {
                            wordEnd = i + 1;
                        }
                        else if (wordEnd >= 0)
                        {
                            break;
                        }
                    }

                    return new TextEdit(lineStart + (wordEnd >= 0 ? wordEnd : line.Length), 0, "Data");
                }

            case 2: // line insertion
                {
                    return new TextEdit(lineStart, 0, $"        var temp{index} = count + {index};\n");
                }

            default: // block paste
                {
                    return new TextEdit(lineStart, 0, $"    public int Pasted{index}(int v) {{ var p = v + 1; return p; }}\n");
                }
        }
    }
}
