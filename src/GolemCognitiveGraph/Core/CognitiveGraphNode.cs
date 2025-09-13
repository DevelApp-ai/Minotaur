using System.Text.Json;
using GolemCognitiveGraph.Visitors;

namespace GolemCognitiveGraph.Core;

/// <summary>
/// Represents a node in the cognitive graph with zero-copy semantics.
/// Serves as the base class for all graph nodes in the Golem system.
/// </summary>
public abstract class CognitiveGraphNode
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the node type identifier.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the parent node, if any.
    /// </summary>
    public CognitiveGraphNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public IReadOnlyList<CognitiveGraphNode> Children => _children.AsReadOnly();

    private readonly List<CognitiveGraphNode> _children = new();

    /// <summary>
    /// Gets or sets the metadata associated with this node.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Gets or sets the source position information for this node.
    /// </summary>
    public SourcePosition? SourcePosition { get; set; }

    /// <summary>
    /// Adds a child node to this node.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    public virtual void AddChild(CognitiveGraphNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        
        if (child.Parent != null)
        {
            throw new InvalidOperationException("Node already has a parent");
        }

        _children.Add(child);
        child.Parent = this;
    }

    /// <summary>
    /// Removes a child node from this node.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns>True if the child was removed; otherwise, false.</returns>
    public virtual bool RemoveChild(CognitiveGraphNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a deep copy of this node and its descendants.
    /// </summary>
    /// <returns>A deep copy of this node.</returns>
    public abstract CognitiveGraphNode Clone();

    /// <summary>
    /// Accepts a visitor for traversal operations.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    public abstract void Accept(ICognitiveGraphVisitor visitor);
}

/// <summary>
/// Represents source position information for tracking location in original source.
/// </summary>
public record SourcePosition(int Line, int Column, int Offset, int Length)
{
    /// <summary>
    /// Gets the end position of this source span.
    /// </summary>
    public SourcePosition End => this with { Offset = Offset + Length, Length = 0 };
}