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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Minotaur.Parser.LiveEditing
{
    /// <summary>
    /// Records per-edit latencies and computes descriptive statistics
    /// (count, mean, min, max and nearest-rank percentiles). Used by the
    /// live editor page and the benchmark harness to report
    /// keystroke-to-token-update latency as p50/p95/p99.
    /// </summary>
    public sealed class LatencyRecorder
    {
        private readonly List<double> _microseconds = new();

        /// <summary>
        /// Gets the number of recorded samples.
        /// </summary>
        public int Count => _microseconds.Count;

        /// <summary>
        /// Gets the mean latency in microseconds, or 0 when no samples exist.
        /// </summary>
        public double MeanMicroseconds => _microseconds.Count == 0
            ? 0
            : _microseconds.Average();

        /// <summary>
        /// Gets the minimum recorded latency in microseconds, or 0 when no samples exist.
        /// </summary>
        public double MinMicroseconds => _microseconds.Count == 0 ? 0 : _microseconds.Min();

        /// <summary>
        /// Gets the maximum recorded latency in microseconds, or 0 when no samples exist.
        /// </summary>
        public double MaxMicroseconds => _microseconds.Count == 0 ? 0 : _microseconds.Max();

        /// <summary>
        /// Record a latency sample.
        /// </summary>
        /// <param name="microseconds">The elapsed latency in microseconds.</param>
        public void AddMicroseconds(double microseconds)
        {
            if (microseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(microseconds), "Latency must not be negative.");
            }

            _microseconds.Add(microseconds);
        }

        /// <summary>
        /// Record a latency sample given as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="elapsed">The elapsed latency.</param>
        public void Add(TimeSpan elapsed)
        {
            AddMicroseconds(elapsed.TotalMicroseconds);
        }

        /// <summary>
        /// Compute the nearest-rank percentile of the recorded latencies.
        /// </summary>
        /// <param name="percentile">Percentile in the range [0, 100].</param>
        /// <returns>
        /// The nearest-rank percentile in microseconds, or 0 when no samples exist.
        /// </returns>
        public double PercentileMicroseconds(double percentile)
        {
            if (_microseconds.Count == 0)
            {
                return 0;
            }

            if (percentile < 0 || percentile > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be in [0, 100].");
            }

            var sorted = _microseconds.OrderBy(x => x).ToList();
            var rank = (int)Math.Ceiling(percentile / 100.0 * sorted.Count);
            rank = Math.Max(1, Math.Min(sorted.Count, rank));
            return sorted[rank - 1];
        }

        /// <summary>
        /// Remove all recorded samples.
        /// </summary>
        public void Reset()
        {
            _microseconds.Clear();
        }
    }

    /// <summary>
    /// Summary statistics of a set of latency samples, formatted for
    /// display and for the committed benchmark results.
    /// </summary>
    /// <param name="Count">Number of samples.</param>
    /// <param name="MeanMicroseconds">Mean latency in microseconds.</param>
    /// <param name="P50Microseconds">Nearest-rank p50 in microseconds.</param>
    /// <param name="P95Microseconds">Nearest-rank p95 in microseconds.</param>
    /// <param name="P99Microseconds">Nearest-rank p99 in microseconds.</param>
    /// <param name="MinMicroseconds">Minimum latency in microseconds.</param>
    /// <param name="MaxMicroseconds">Maximum latency in microseconds.</param>
    public readonly record struct LatencySummary(
        int Count,
        double MeanMicroseconds,
        double P50Microseconds,
        double P95Microseconds,
        double P99Microseconds,
        double MinMicroseconds,
        double MaxMicroseconds)
    {
        /// <summary>
        /// Compute the summary of a recorder's samples. The recorder is not modified.
        /// </summary>
        /// <param name="recorder">The recorder to summarize.</param>
        /// <returns>The summary statistics.</returns>
        public static LatencySummary From(LatencyRecorder recorder)
        {
            if (recorder is null)
            {
                throw new ArgumentNullException(nameof(recorder));
            }

            return new LatencySummary(
                Count: recorder.Count,
                MeanMicroseconds: recorder.MeanMicroseconds,
                P50Microseconds: recorder.PercentileMicroseconds(50),
                P95Microseconds: recorder.PercentileMicroseconds(95),
                P99Microseconds: recorder.PercentileMicroseconds(99),
                MinMicroseconds: recorder.MinMicroseconds,
                MaxMicroseconds: recorder.MaxMicroseconds);
        }

        /// <summary>
        /// Render the summary as a single human-readable line, using
        /// invariant culture so the output is stable across machines.
        /// </summary>
        /// <returns>A formatted one-line summary.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "n={0} mean={1:0.###}us p50={2:0.###}us p95={3:0.###}us p99={4:0.###}us min={5:0.###}us max={6:0.###}us",
                Count, MeanMicroseconds, P50Microseconds, P95Microseconds, P99Microseconds, MinMicroseconds, MaxMicroseconds);
        }
    }
}
