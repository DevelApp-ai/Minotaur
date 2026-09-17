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

using Minotaur.Core;

namespace Minotaur.Analysis.Symbolic;

/// <summary>
/// Contract for visitors that traverse a cognitive graph with symbolic-analysis awareness.
/// Implemented by the language plugin validation visitors so that language plugins can
/// expose their validation traversal through <c>GetSymbolicAnalysisVisitor()</c>.
/// </summary>
public interface ISymbolicAnalysisVisitor : IDisposable
{
    /// <summary>
    /// Prepares the visitor for a new traversal.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Resets any state accumulated by previous traversals.
    /// </summary>
    void Reset();

    /// <summary>
    /// Visits a cognitive graph node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    void Visit(CognitiveGraphNode node);
}
