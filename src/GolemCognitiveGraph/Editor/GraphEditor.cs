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

using CognitiveGraph;
using CognitiveGraph.Builder;
using CognitiveGraph.Schema;
using CognitiveGraph.Accessors;
using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Visitors;
using System.Collections.Concurrent;

namespace GolemCognitiveGraph.Editor;

/// <summary>
/// Core component for performing zero-copy modifications on cognitive graphs.
/// Implements the strategy pattern for different editing operations.
/// Uses DevelApp.CognitiveGraph as the underlying data structure.
/// </summary>
public class GraphEditor : IDisposable
{
    private readonly ConcurrentDictionary<Guid, CognitiveGraphNode> _nodeIndex = new();
    private readonly Stack<EditOperation> _undoStack = new();
    private readonly Stack<EditOperation> _redoStack = new();
    private readonly object _editLock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the underlying CognitiveGraph.CognitiveGraph instance.
    /// </summary>
    public CognitiveGraph.CognitiveGraph? UnderlyingGraph { get; private set; }

    /// <summary>
    /// Gets the graph builder for creating new nodes.
    /// </summary>
    public CognitiveGraphBuilder? GraphBuilder { get; private set; }

    /// <summary>
    /// Gets the root node of the graph being edited.
    /// </summary>
    public CognitiveGraphNode? Root { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are operations that can be undone.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are operations that can be redone.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Event raised when the graph is modified.
    /// </summary>
    public event EventHandler<GraphModifiedEventArgs>? GraphModified;

    /// <summary>
    /// Initializes a new instance of the GraphEditor class.
    /// </summary>
    /// <param name="root">The root node of the graph to edit.</param>
    /// <param name="underlyingGraph">The underlying CognitiveGraph.CognitiveGraph instance.</param>
    public GraphEditor(CognitiveGraphNode? root = null, CognitiveGraph.CognitiveGraph? underlyingGraph = null)
    {
        UnderlyingGraph = underlyingGraph;
        GraphBuilder = new CognitiveGraphBuilder();

        if (root != null)
        {
            SetRoot(root);
        }
        else if (underlyingGraph != null)
        {
            // Create wrapper node from underlying graph root
            var rootNode = underlyingGraph.GetRootNode();
            // TODO: Create appropriate wrapper based on node type
            // This is a simplified implementation - in practice would need
            // more sophisticated node wrapping logic
        }
    }

    /// <summary>
    /// Initializes a new instance of the GraphEditor class from source code.
    /// </summary>
    /// <param name="sourceCode">The source code to parse into a cognitive graph.</param>
    public GraphEditor(string sourceCode)
    {
        GraphBuilder = new CognitiveGraphBuilder();
        // For demonstration, create a simple root node
        // In practice, this would involve parsing the source code
        var rootOffset = GraphBuilder.WriteSymbolNode(
            symbolId: 1,
            nodeType: 200,
            sourceStart: 0,
            sourceLength: (uint)sourceCode.Length,
            properties: new List<(string key, PropertyValueType type, object value)>
            {
                ("NodeType", PropertyValueType.String, "CompilationUnit"),
                ("Source", PropertyValueType.String, sourceCode)
            }
        );

        var buffer = GraphBuilder.Build(rootOffset, sourceCode);
        UnderlyingGraph = new CognitiveGraph.CognitiveGraph(buffer);

        // Create wrapper root node
        var underlyingRoot = UnderlyingGraph.GetRootNode();
        Root = new NonTerminalNode("CompilationUnit", 0, underlyingRoot);
        RebuildNodeIndex();
    }

    /// <summary>
    /// Sets the root node of the graph and rebuilds the node index.
    /// </summary>
    /// <param name="root">The new root node.</param>
    public void SetRoot(CognitiveGraphNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        lock (_editLock)
        {
            Root = root;
            RebuildNodeIndex();
        }
    }

    /// <summary>
    /// Inserts a new node at the specified location with zero-copy semantics.
    /// </summary>
    /// <param name="parentId">The ID of the parent node.</param>
    /// <param name="newNode">The node to insert.</param>
    /// <param name="index">The index at which to insert the node. If null, appends to the end.</param>
    /// <returns>The edit operation that was performed.</returns>
    public EditOperation InsertNode(Guid parentId, CognitiveGraphNode newNode, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(newNode);

        lock (_editLock)
        {
            if (!_nodeIndex.TryGetValue(parentId, out var parent))
            {
                throw new InvalidOperationException($"Parent node with ID {parentId} not found");
            }

            var operation = new InsertNodeOperation(parentId, newNode, index ?? parent.Children.Count);
            ExecuteOperation(operation);
            return operation;
        }
    }

    /// <summary>
    /// Removes a node from the graph while maintaining structural integrity.
    /// </summary>
    /// <param name="nodeId">The ID of the node to remove.</param>
    /// <returns>The edit operation that was performed.</returns>
    public EditOperation RemoveNode(Guid nodeId)
    {
        lock (_editLock)
        {
            if (!_nodeIndex.TryGetValue(nodeId, out var node))
            {
                throw new InvalidOperationException($"Node with ID {nodeId} not found");
            }

            if (node.Parent == null)
            {
                throw new InvalidOperationException("Cannot remove root node");
            }

            var operation = new RemoveNodeOperation(nodeId);
            ExecuteOperation(operation);
            return operation;
        }
    }

    /// <summary>
    /// Replaces a node with another node, preserving children if specified.
    /// </summary>
    /// <param name="nodeId">The ID of the node to replace.</param>
    /// <param name="replacement">The replacement node.</param>
    /// <param name="preserveChildren">Whether to preserve the children of the original node.</param>
    /// <returns>The edit operation that was performed.</returns>
    public EditOperation ReplaceNode(Guid nodeId, CognitiveGraphNode replacement, bool preserveChildren = true)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        lock (_editLock)
        {
            if (!_nodeIndex.TryGetValue(nodeId, out var node))
            {
                throw new InvalidOperationException($"Node with ID {nodeId} not found");
            }

            var operation = new ReplaceNodeOperation(nodeId, replacement, preserveChildren);
            ExecuteOperation(operation);
            return operation;
        }
    }

    /// <summary>
    /// Moves a node to a new parent location.
    /// </summary>
    /// <param name="nodeId">The ID of the node to move.</param>
    /// <param name="newParentId">The ID of the new parent node.</param>
    /// <param name="index">The index at which to insert the node in the new parent.</param>
    /// <returns>The edit operation that was performed.</returns>
    public EditOperation MoveNode(Guid nodeId, Guid newParentId, int? index = null)
    {
        lock (_editLock)
        {
            if (!_nodeIndex.TryGetValue(nodeId, out var node))
            {
                throw new InvalidOperationException($"Node with ID {nodeId} not found");
            }

            if (!_nodeIndex.TryGetValue(newParentId, out var newParent))
            {
                throw new InvalidOperationException($"New parent node with ID {newParentId} not found");
            }

            var operation = new MoveNodeOperation(nodeId, newParentId, index ?? newParent.Children.Count);
            ExecuteOperation(operation);
            return operation;
        }
    }

    /// <summary>
    /// Undoes the last edit operation.
    /// </summary>
    public void Undo()
    {
        lock (_editLock)
        {
            if (_undoStack.Count == 0)
            {
                throw new InvalidOperationException("No operations to undo");
            }

            var operation = _undoStack.Pop();
            operation.Undo(this);
            _redoStack.Push(operation);

            OnGraphModified(new GraphModifiedEventArgs(operation, GraphModificationType.Undo));
        }
    }

    /// <summary>
    /// Redoes the last undone edit operation.
    /// </summary>
    public void Redo()
    {
        lock (_editLock)
        {
            if (_redoStack.Count == 0)
            {
                throw new InvalidOperationException("No operations to redo");
            }

            var operation = _redoStack.Pop();
            operation.Execute(this);
            _undoStack.Push(operation);

            OnGraphModified(new GraphModifiedEventArgs(operation, GraphModificationType.Redo));
        }
    }

    /// <summary>
    /// Finds a node by its ID.
    /// </summary>
    /// <param name="nodeId">The ID of the node to find.</param>
    /// <returns>The node if found; otherwise, null.</returns>
    public CognitiveGraphNode? FindNode(Guid nodeId)
    {
        return _nodeIndex.TryGetValue(nodeId, out var node) ? node : null;
    }

    private void ExecuteOperation(EditOperation operation)
    {
        operation.Execute(this);
        _undoStack.Push(operation);
        _redoStack.Clear(); // Clear redo stack when new operation is executed

        OnGraphModified(new GraphModifiedEventArgs(operation, GraphModificationType.Execute));
    }

    private void RebuildNodeIndex()
    {
        _nodeIndex.Clear();

        if (Root != null)
        {
            var indexBuilder = new NodeIndexBuilder(_nodeIndex);
            Root.Accept(indexBuilder);
        }
    }

    internal void AddToIndex(CognitiveGraphNode node)
    {
        _nodeIndex[node.Id] = node;
    }

    internal void RemoveFromIndex(Guid nodeId)
    {
        _nodeIndex.TryRemove(nodeId, out _);
    }

    protected virtual void OnGraphModified(GraphModifiedEventArgs e)
    {
        GraphModified?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _nodeIndex.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Event arguments for graph modification events.
/// </summary>
public class GraphModifiedEventArgs : EventArgs
{
    public EditOperation Operation { get; }
    public GraphModificationType ModificationType { get; }

    public GraphModifiedEventArgs(EditOperation operation, GraphModificationType modificationType)
    {
        Operation = operation;
        ModificationType = modificationType;
    }
}

/// <summary>
/// Specifies the type of graph modification.
/// </summary>
public enum GraphModificationType
{
    Execute,
    Undo,
    Redo
}

/// <summary>
/// Visitor implementation for building the node index.
/// </summary>
internal class NodeIndexBuilder : CognitiveGraphVisitorBase
{
    private readonly ConcurrentDictionary<Guid, CognitiveGraphNode> _index;

    public NodeIndexBuilder(ConcurrentDictionary<Guid, CognitiveGraphNode> index)
    {
        _index = index;
    }

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        _index[node.Id] = node;
    }
}