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
/// Interface for grammar detection strategies that can determine which grammar and version to use for a file.
/// </summary>
public interface IGrammarDetector
{
    /// <summary>
    /// Gets the unique identifier for this detection strategy.
    /// </summary>
    string DetectorId { get; }

    /// <summary>
    /// Gets the priority of this detector (higher numbers are checked first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Detects the appropriate grammar for a file based on the provided context.
    /// </summary>
    /// <param name="context">The detection context containing file information and project settings.</param>
    /// <returns>A task that represents the asynchronous detection operation. The task result contains the detection result.</returns>
    Task<GrammarDetectionResult> DetectGrammarAsync(GrammarDetectionContext context);

    /// <summary>
    /// Determines if this detector can handle the given file context.
    /// </summary>
    /// <param name="context">The detection context to evaluate.</param>
    /// <returns>True if this detector can process the given context, false otherwise.</returns>
    bool CanDetect(GrammarDetectionContext context);
}