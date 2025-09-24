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

namespace Minotaur.Editor;

/// <summary>
/// Base class for all edit operations on the cognitive graph.
/// Implements the Command pattern for undo/redo functionality.
/// </summary>
public abstract class EditOperation
{
    /// <summary>
    /// Gets the unique identifier for this operation.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this operation was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the type of this edit operation.
    /// </summary>
    public abstract string OperationType { get; }

    /// <summary>
    /// Executes this edit operation on the specified graph editor.
    /// </summary>
    /// <param name="editor">The graph editor to execute the operation on.</param>
    public abstract void Execute(GraphEditor editor);

    /// <summary>
    /// Undoes this edit operation on the specified graph editor.
    /// </summary>
    /// <param name="editor">The graph editor to undo the operation on.</param>
    public abstract void Undo(GraphEditor editor);
}

/// <summary>
/// Edit operation for inserting a new node into the graph.
/// </summary>
public class InsertNodeOperation : EditOperation
{
    public Guid ParentId { get; }
    public CognitiveGraphNode Node { get; }
    public int Index { get; }

    public override string OperationType => "InsertNode";

    public InsertNodeOperation(Guid parentId, CognitiveGraphNode node, int index)
    {
        ParentId = parentId;
        Node = node;
        Index = index;
    }

    public override void Execute(GraphEditor editor)
    {
        var parent = editor.FindNode(ParentId);
        if (parent == null)
        {
            throw new InvalidOperationException($"Parent node with ID {ParentId} not found");
        }

        // Insert node at specified index
        parent.AddChild(Node);
        editor.AddToIndex(Node);

        // Add all descendants to index
        var indexBuilder = new OperationNodeIndexBuilder(editor);
        Node.Accept(indexBuilder);
    }

    public override void Undo(GraphEditor editor)
    {
        var parent = editor.FindNode(ParentId);
        if (parent == null)
        {
            throw new InvalidOperationException($"Parent node with ID {ParentId} not found");
        }

        parent.RemoveChild(Node);
        RemoveFromIndex(editor, Node);
    }

    private void RemoveFromIndex(GraphEditor editor, CognitiveGraphNode node)
    {
        editor.RemoveFromIndex(node.Id);
        foreach (var child in node.Children)
        {
            RemoveFromIndex(editor, child);
        }
    }
}

/// <summary>
/// Edit operation for removing a node from the graph.
/// </summary>
public class RemoveNodeOperation : EditOperation
{
    public Guid NodeId { get; }

    // State for undo
    private Guid _originalParentId;
    private int _originalIndex;
    private CognitiveGraphNode? _removedNode;

    public override string OperationType => "RemoveNode";

    public RemoveNodeOperation(Guid nodeId)
    {
        NodeId = nodeId;
    }

    public override void Execute(GraphEditor editor)
    {
        var node = editor.FindNode(NodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node with ID {NodeId} not found");
        }

        if (node.Parent == null)
        {
            throw new InvalidOperationException("Cannot remove root node");
        }

        // Store state for undo
        _originalParentId = node.Parent.Id;
        _originalIndex = node.Parent.Children.ToList().IndexOf(node);
        _removedNode = node;

        // Remove node
        node.Parent.RemoveChild(node);
        RemoveFromIndex(editor, node);
    }

    public override void Undo(GraphEditor editor)
    {
        if (_removedNode == null)
        {
            throw new InvalidOperationException("Cannot undo: removed node state not available");
        }

        var parent = editor.FindNode(_originalParentId);
        if (parent == null)
        {
            throw new InvalidOperationException($"Original parent node with ID {_originalParentId} not found");
        }

        parent.AddChild(_removedNode);
        editor.AddToIndex(_removedNode);

        // Add all descendants back to index
        var indexBuilder = new OperationNodeIndexBuilder(editor);
        _removedNode.Accept(indexBuilder);
    }

    private void RemoveFromIndex(GraphEditor editor, CognitiveGraphNode node)
    {
        editor.RemoveFromIndex(node.Id);
        foreach (var child in node.Children)
        {
            RemoveFromIndex(editor, child);
        }
    }
}

/// <summary>
/// Edit operation for replacing a node with another node.
/// </summary>
public class ReplaceNodeOperation : EditOperation
{
    public Guid NodeId { get; }
    public CognitiveGraphNode Replacement { get; }
    public bool PreserveChildren { get; }

    // State for undo
    private CognitiveGraphNode? _originalNode;
    private Guid _originalParentId;
    private int _originalIndex;

    public override string OperationType => "ReplaceNode";

    public ReplaceNodeOperation(Guid nodeId, CognitiveGraphNode replacement, bool preserveChildren)
    {
        NodeId = nodeId;
        Replacement = replacement;
        PreserveChildren = preserveChildren;
    }

    public override void Execute(GraphEditor editor)
    {
        var node = editor.FindNode(NodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node with ID {NodeId} not found");
        }

        if (node.Parent == null)
        {
            throw new InvalidOperationException("Cannot replace root node");
        }

        // Store state for undo
        _originalNode = node;
        _originalParentId = node.Parent.Id;
        _originalIndex = node.Parent.Children.ToList().IndexOf(node);

        // Store parent reference before removing
        var parent = node.Parent;

        // Preserve children if requested
        if (PreserveChildren)
        {
            foreach (var child in node.Children.ToList())
            {
                node.RemoveChild(child);
                Replacement.AddChild(child);
            }
        }

        // Replace the node
        parent.RemoveChild(node);
        parent.AddChild(Replacement);

        // Update index
        RemoveFromIndex(editor, node);
        editor.AddToIndex(Replacement);
        var indexBuilder = new OperationNodeIndexBuilder(editor);
        Replacement.Accept(indexBuilder);
    }

    public override void Undo(GraphEditor editor)
    {
        if (_originalNode == null)
        {
            throw new InvalidOperationException("Cannot undo: original node state not available");
        }

        var parent = editor.FindNode(_originalParentId);
        if (parent == null)
        {
            throw new InvalidOperationException($"Original parent node with ID {_originalParentId} not found");
        }

        // Remove replacement
        parent.RemoveChild(Replacement);
        RemoveFromIndex(editor, Replacement);

        // Restore original node
        parent.AddChild(_originalNode);
        editor.AddToIndex(_originalNode);
        var indexBuilder = new OperationNodeIndexBuilder(editor);
        _originalNode.Accept(indexBuilder);
    }

    private void RemoveFromIndex(GraphEditor editor, CognitiveGraphNode node)
    {
        editor.RemoveFromIndex(node.Id);
        foreach (var child in node.Children)
        {
            RemoveFromIndex(editor, child);
        }
    }
}

/// <summary>
/// Edit operation for moving a node to a new parent location.
/// </summary>
public class MoveNodeOperation : EditOperation
{
    public Guid NodeId { get; }
    public Guid NewParentId { get; }
    public int NewIndex { get; }

    // State for undo
    private Guid _originalParentId;
    private int _originalIndex;

    public override string OperationType => "MoveNode";

    public MoveNodeOperation(Guid nodeId, Guid newParentId, int newIndex)
    {
        NodeId = nodeId;
        NewParentId = newParentId;
        NewIndex = newIndex;
    }

    public override void Execute(GraphEditor editor)
    {
        var node = editor.FindNode(NodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node with ID {NodeId} not found");
        }

        var newParent = editor.FindNode(NewParentId);
        if (newParent == null)
        {
            throw new InvalidOperationException($"New parent node with ID {NewParentId} not found");
        }

        if (node.Parent == null)
        {
            throw new InvalidOperationException("Cannot move root node");
        }

        // Store state for undo
        _originalParentId = node.Parent.Id;
        _originalIndex = node.Parent.Children.ToList().IndexOf(node);

        // Move the node
        node.Parent.RemoveChild(node);
        newParent.AddChild(node);
    }

    public override void Undo(GraphEditor editor)
    {
        var node = editor.FindNode(NodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node with ID {NodeId} not found");
        }

        var originalParent = editor.FindNode(_originalParentId);
        if (originalParent == null)
        {
            throw new InvalidOperationException($"Original parent node with ID {_originalParentId} not found");
        }

        // Move back to original location
        node.Parent?.RemoveChild(node);
        originalParent.AddChild(node);
    }
}

/// <summary>
/// Internal helper class for building node indices during operations.
/// </summary>
internal class OperationNodeIndexBuilder : Visitors.CognitiveGraphVisitorBase
{
    private readonly GraphEditor _editor;

    public OperationNodeIndexBuilder(GraphEditor editor)
    {
        _editor = editor;
    }

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        _editor.AddToIndex(node);
    }
}