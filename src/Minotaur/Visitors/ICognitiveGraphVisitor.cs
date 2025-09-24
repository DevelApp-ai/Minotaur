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

namespace Minotaur.Visitors;

/// <summary>
/// Visitor interface for traversing and operating on cognitive graph nodes.
/// </summary>
public interface ICognitiveGraphVisitor
{
    /// <summary>
    /// Visits a cognitive graph node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    void Visit(Core.CognitiveGraphNode node);

    /// <summary>
    /// Visits the children of a node before visiting the node itself.
    /// </summary>
    /// <param name="node">The node whose children to visit.</param>
    void VisitChildren(Core.CognitiveGraphNode node);
}

/// <summary>
/// Base implementation of cognitive graph visitor with depth-first traversal.
/// </summary>
public abstract class CognitiveGraphVisitorBase : ICognitiveGraphVisitor
{
    /// <summary>
    /// Visits a cognitive graph node with depth-first traversal.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    public virtual void Visit(Core.CognitiveGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        BeforeVisitNode(node);
        VisitChildren(node);
        AfterVisitNode(node);
    }

    /// <summary>
    /// Visits all children of the specified node.
    /// </summary>
    /// <param name="node">The node whose children to visit.</param>
    public virtual void VisitChildren(Core.CognitiveGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var child in node.Children)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Called before visiting a node.
    /// </summary>
    /// <param name="node">The node about to be visited.</param>
    protected virtual void BeforeVisitNode(Core.CognitiveGraphNode node) { }

    /// <summary>
    /// Called after visiting a node and its children.
    /// </summary>
    /// <param name="node">The node that was visited.</param>
    protected virtual void AfterVisitNode(Core.CognitiveGraphNode node) { }
}