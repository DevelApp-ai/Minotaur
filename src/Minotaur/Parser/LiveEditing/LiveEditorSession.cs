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

using System;
using System.Diagnostics;
using DevelApp.StepLexer;
using DevelApp.StepParser;

namespace Minotaur.Parser.LiveEditing
{
    /// <summary>
    /// How the session reacts to text changes.
    /// </summary>
    public enum LiveEditingMode
    {
        /// <summary>
        /// Apply the minimal edit via <c>RealTimeParserSession.ApplyEdit</c>
        /// and incrementally re-lex only the affected region. This is the
        /// production mode of the live editor.
        /// </summary>
        Incremental,

        /// <summary>
        /// Re-lex the whole document on every text change via
        /// <c>RealTimeParserSession.Initialize</c>. This is the baseline
        /// the incremental mode must be compared against in the issue-#196
        /// measurements.
        /// </summary>
        FullReparse,
    }

    /// <summary>
    /// The bridge between a Minotaur editor surface (Blazor page or harness)
    /// and the engine's shipped incremental
    /// <see cref="RealTimeParserSession"/>. The editor reports old/new text
    /// pairs via <see cref="OnTextChange"/>; this class computes the minimal
    /// edit with <see cref="TextEditCalculator"/>, applies it in the selected
    /// <see cref="Mode"/>, and records keystroke-to-token-update latency and
    /// the engine's token-reuse ratio per edit.
    /// </summary>
    /// <remarks>
    /// The session is not thread-safe; a Blazor editor invokes it from a
    /// single synchronization context. ASCII text only, see
    /// <see cref="TextEditCalculator"/> for the reason.
    /// </remarks>
    public sealed class LiveEditorSession
    {
        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// Initializes a new session backed by a grammar given as content.
        /// </summary>
        /// <param name="grammarContent">The GrammarForge grammar file content.</param>
        /// <param name="fileName">Optional file name used for diagnostics.</param>
        public LiveEditorSession(string grammarContent, string fileName = "live-editor.txt")
        {
            Session = new RealTimeParserSession(grammarContent, fileName);

            // Greedy longest-match lexing (engine issue #142): the session's
            // default exploratory mode forks a lexer path per matching rule
            // and unions the tokens of all active paths, so any overlapping
            // rule pair (keyword vs. identifier is the classic one) makes the
            // token stream grow multiplicatively per position. The engine's
            // issue-101 measurement harness runs greedy for exactly this
            // reason, and a live editor wants the same deterministic,
            // exactly-one-token-per-position behavior on every ApplyEdit
            // re-lex. For the non-overlapping live-editor grammars the
            // emitted tokens are identical to exploratory; with overlapping
            // grammars only greedy stays linear.
            Session.LexingMode = LexingMode.GreedyLongestMatch;

            Latency = new LatencyRecorder();
        }

        /// <summary>
        /// Gets the wrapped engine session. Consumers re-render token-derived
        /// views from <c>Session.Tokens</c> after each edit.
        /// </summary>
        public RealTimeParserSession Session { get; }

        /// <summary>
        /// Gets or sets how text changes are processed. Switching modes does
        /// not reset the document or the recorded statistics.
        /// </summary>
        public LiveEditingMode Mode { get; set; } = LiveEditingMode.Incremental;

        /// <summary>
        /// Gets the recorder of per-edit keystroke-to-token-update latencies.
        /// </summary>
        public LatencyRecorder Latency { get; }

        /// <summary>
        /// Gets the token-reuse ratio reported by the engine for every
        /// applied edit, in edit order (issue #76 instrumentation).
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<double> ReuseRatios => _reuseRatios;

        private readonly System.Collections.Generic.List<double> _reuseRatios = new();

        /// <summary>
        /// Gets the current document text known to the session.
        /// </summary>
        public string Text => Session.Text;

        /// <summary>
        /// Gets the number of tokens in the current document.
        /// </summary>
        public int TokenCount => Session.Tokens.Count;

        /// <summary>
        /// Gets the token-list version; incremented by every applied edit.
        /// </summary>
        public int Version => Session.Version;

        /// <summary>
        /// Gets the number of text changes applied since the last
        /// <see cref="Open"/> call.
        /// </summary>
        public int EditCount { get; private set; }

        /// <summary>
        /// Gets the reuse ratio of the most recent edit (1.0 before any edit).
        /// </summary>
        public double LastReuseRatio => Session.LastReuseRatio;

        /// <summary>
        /// Open (or reopen) a document with the given full source text,
        /// performing a complete lex and resetting all statistics.
        /// </summary>
        /// <param name="source">The full ASCII source text.</param>
        public void Open(string source)
        {
            TextEditCalculator.RequireAscii(source, nameof(source));
            Latency.Reset();
            _reuseRatios.Clear();
            EditCount = 0;
            Session.Initialize(source);
        }

        /// <summary>
        /// Report a text change from the editor and update the token stream.
        /// The edit is derived from the difference between the current
        /// session text and the new text. When the texts are identical the
        /// call is a no-op.
        /// </summary>
        /// <param name="newText">The new full text of the document.</param>
        /// <returns>
        /// The measured keystroke-to-token-update latency of this edit, or
        /// <see langword="null"/> when the text had not changed.
        /// </returns>
        /// <exception cref="NotSupportedException">The text is not ASCII.</exception>
        public TimeSpan? OnTextChange(string newText)
        {
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            if (newText == Session.Text)
            {
                return null;
            }

            var edit = TextEditCalculator.Compute(Session.Text, newText)
                ?? throw new InvalidOperationException("TextEditCalculator returned no edit for different texts.");

            TimeSpan elapsed;
            if (Mode == LiveEditingMode.FullReparse)
            {
                _stopwatch.Restart();
                Session.Initialize(newText);
                _stopwatch.Stop();
                elapsed = _stopwatch.Elapsed;
            }
            else
            {
                _stopwatch.Restart();
                Session.ApplyEdit(edit.Offset, edit.DeleteLength, edit.InsertText);
                _stopwatch.Stop();
                elapsed = _stopwatch.Elapsed;
                _reuseRatios.Add(Session.LastReuseRatio);
            }

            Latency.Add(elapsed);
            EditCount++;
            return elapsed;
        }
    }
}
