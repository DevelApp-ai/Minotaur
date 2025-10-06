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

namespace Minotaur.Core;

/// <summary>
/// Placeholder for GraphEditor until it's moved to the external CognitiveGraph.Editor package.
/// This class will be removed once the migration is complete.
/// </summary>
[Obsolete("GraphEditor has been moved to DevelApp.CognitiveGraph.Editor package. This placeholder will be removed in a future version.")]
public class GraphEditor : IDisposable
{
    /// <summary>
    /// Gets the root node of the graph being edited.
    /// </summary>
    public CognitiveGraphNode? Root { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are operations that can be undone.
    /// </summary>
    public bool CanUndo => false;

    /// <summary>
    /// Gets a value indicating whether there are operations that can be redone.
    /// </summary>
    public bool CanRedo => false;

    /// <summary>
    /// Initializes a new instance of the GraphEditor placeholder.
    /// </summary>
    /// <param name="sourceCode">Source code (ignored in placeholder)</param>
    public GraphEditor(string sourceCode = "")
    {
        // Placeholder implementation
        throw new NotImplementedException(
            "GraphEditor functionality has been moved to DevelApp.CognitiveGraph.Editor package. " +
            "Please install the external package: dotnet add package DevelApp.CognitiveGraph.Editor");
    }

    /// <summary>
    /// Initializes a new instance of the GraphEditor placeholder.
    /// </summary>
    /// <param name="root">Root node (ignored in placeholder)</param>
    public GraphEditor(CognitiveGraphNode? root = null)
    {
        // Placeholder implementation
        throw new NotImplementedException(
            "GraphEditor functionality has been moved to DevelApp.CognitiveGraph.Editor package. " +
            "Please install the external package: dotnet add package DevelApp.CognitiveGraph.Editor");
    }

    /// <summary>
    /// Placeholder method for setting root.
    /// </summary>
    public void SetRoot(CognitiveGraphNode root)
    {
        throw new NotImplementedException(
            "GraphEditor functionality has been moved to DevelApp.CognitiveGraph.Editor package.");
    }

    /// <summary>
    /// Placeholder method for undo operation.
    /// </summary>
    public void Undo()
    {
        throw new NotImplementedException(
            "GraphEditor functionality has been moved to DevelApp.CognitiveGraph.Editor package.");
    }

    /// <summary>
    /// Placeholder method for redo operation.
    /// </summary>
    public void Redo()
    {
        throw new NotImplementedException(
            "GraphEditor functionality has been moved to DevelApp.CognitiveGraph.Editor package.");
    }

    /// <summary>
    /// Releases all resources used by the placeholder.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose in placeholder
    }
}