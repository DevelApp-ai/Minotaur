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

namespace Minotaur.Projects.Grammar;

/// <summary>
/// Represents the result of grammar detection for a file.
/// </summary>
public class GrammarDetectionResult
{
    /// <summary>
    /// Gets a value indicating whether grammar detection was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Gets the detected grammar name (e.g., "CSharp10.grammar", "Python311.grammar").
    /// </summary>
    public string? GrammarName { get; init; }

    /// <summary>
    /// Gets the detected grammar version.
    /// </summary>
    public GrammarVersion? Version { get; init; }

    /// <summary>
    /// Gets the confidence level of this detection (0.0 to 1.0).
    /// Higher values indicate greater confidence in the detection result.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets the ID of the detector that produced this result.
    /// </summary>
    public string DetectorId { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional metadata about the detection process.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets any fallback grammar options if the primary detection has low confidence.
    /// </summary>
    public IReadOnlyList<string> FallbackGrammars { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the reason for detection failure if <see cref="IsSuccessful"/> is false.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Creates a successful grammar detection result.
    /// </summary>
    /// <param name="grammarName">The detected grammar name.</param>
    /// <param name="version">The detected grammar version.</param>
    /// <param name="confidence">The confidence level (0.0 to 1.0).</param>
    /// <param name="detectorId">The ID of the detector that produced this result.</param>
    /// <param name="metadata">Additional metadata about the detection.</param>
    /// <param name="fallbackGrammars">Optional fallback grammar options.</param>
    /// <returns>A successful detection result.</returns>
    public static GrammarDetectionResult Success(
        string grammarName,
        GrammarVersion? version = null,
        double confidence = 1.0,
        string detectorId = "",
        IReadOnlyDictionary<string, object>? metadata = null,
        IReadOnlyList<string>? fallbackGrammars = null)
    {
        return new GrammarDetectionResult
        {
            IsSuccessful = true,
            GrammarName = grammarName,
            Version = version,
            Confidence = Math.Clamp(confidence, 0.0, 1.0),
            DetectorId = detectorId,
            Metadata = metadata ?? new Dictionary<string, object>(),
            FallbackGrammars = fallbackGrammars ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed grammar detection result.
    /// </summary>
    /// <param name="reason">The reason for the detection failure.</param>
    /// <param name="detectorId">The ID of the detector that produced this result.</param>
    /// <param name="metadata">Additional metadata about the detection attempt.</param>
    /// <returns>A failed detection result.</returns>
    public static GrammarDetectionResult Failure(
        string reason,
        string detectorId = "",
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new GrammarDetectionResult
        {
            IsSuccessful = false,
            FailureReason = reason,
            DetectorId = detectorId,
            Confidence = 0.0,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}