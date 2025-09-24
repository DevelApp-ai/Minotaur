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

using System.Text.Json;
using CognitiveGraph;
using CognitiveGraph.Schema;
using CognitiveGraph.Accessors;
using Minotaur.Visitors;

namespace Minotaur.Core;

/// <summary>
/// Represents a node in the cognitive graph with zero-copy semantics.
/// Serves as a wrapper around DevelApp.CognitiveGraph.SymbolNode for the Golem system.
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
    /// Gets the underlying SymbolNode from CognitiveGraph.
    /// Since SymbolNode is a ref struct, we store a reference flag instead.
    /// </summary>
    public bool HasUnderlyingNode { get; protected set; }

    /// <summary>
    /// Reference to the underlying node from StepParser or CognitiveGraph packages.
    /// Used for zero-copy integration with external parsers.
    /// </summary>
    public object? UnderlyingNode { get; set; }

    /// <summary>
    /// The source start position from the underlying node.
    /// </summary>
    protected uint UnderlyingSourceStart { get; set; }

    /// <summary>
    /// The source length from the underlying node.
    /// </summary>
    protected uint UnderlyingSourceLength { get; set; }

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
    /// Initializes a new instance of the CognitiveGraphNode class.
    /// </summary>
    /// <param name="underlyingNode">The underlying SymbolNode from CognitiveGraph.</param>
    protected CognitiveGraphNode(SymbolNode underlyingNode)
    {
        HasUnderlyingNode = true;
        UnderlyingSourceStart = underlyingNode.SourceStart;
        UnderlyingSourceLength = underlyingNode.SourceLength;

        // Extract source position from underlying node
        SourcePosition = new SourcePosition(
            Line: 0, // Will be calculated from offset
            Column: 0, // Will be calculated from offset
            Offset: (int)underlyingNode.SourceStart,
            Length: (int)underlyingNode.SourceLength
        );
    }

    /// <summary>
    /// Initializes a new instance of the CognitiveGraphNode class without an underlying node.
    /// </summary>
    protected CognitiveGraphNode()
    {
        HasUnderlyingNode = false;
    }

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
    /// Finds the node at the specified source position.
    /// </summary>
    public CognitiveGraphNode? FindNodeAt(SourcePosition position)
    {
        // Check if this node contains the position
        if (SourcePosition?.Contains(position) == true)
        {
            // Check children first (more specific)
            foreach (var child in Children)
            {
                var result = child.FindNodeAt(position);
                if (result != null)
                {
                    return result;
                }
            }

            // If no child contains it, this node is the best match
            return this;
        }

        return null;
    }

    /// <summary>
    /// Accepts a visitor for traversal operations.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    public abstract void Accept(ICognitiveGraphVisitor visitor);
}

/// <summary>
/// Represents source position information with enhanced precision for tracking location in original source.
/// </summary>
public record SourcePosition(int Line, int Column, int Offset, int Length)
{
    /// <summary>
    /// Gets the end position of this source span.
    /// </summary>
    public SourcePosition End => this with { Offset = Offset + Length, Length = 0 };

    /// <summary>
    /// Gets the end line number (calculated from start line and content).
    /// </summary>
    public int EndLine { get; init; } = Line;

    /// <summary>
    /// Gets the end column number (calculated from start column and content).
    /// </summary>
    public int EndColumn { get; init; } = Column;

    /// <summary>
    /// Gets the filename or source identifier this position refers to.
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Checks if this position contains another position.
    /// </summary>
    public bool Contains(SourcePosition other)
    {
        return Offset <= other.Offset && (Offset + Length) >= (other.Offset + other.Length);
    }

    /// <summary>
    /// Checks if this position overlaps with another position.
    /// </summary>
    public bool OverlapsWith(SourcePosition other)
    {
        return !(Offset + Length <= other.Offset || other.Offset + other.Length <= Offset);
    }

    /// <summary>
    /// Creates a new position spanning from this position to another.
    /// </summary>
    public SourcePosition SpanTo(SourcePosition other)
    {
        var startOffset = Math.Min(Offset, other.Offset);
        var endOffset = Math.Max(Offset + Length, other.Offset + other.Length);
        return this with
        {
            Offset = startOffset,
            Length = endOffset - startOffset,
            EndLine = other.EndLine,
            EndColumn = other.EndColumn
        };
    }

    /// <summary>
    /// Converts offset-based position to line/column coordinates using source text.
    /// </summary>
    public static SourcePosition FromOffset(int offset, int length, string sourceText, string? sourceFile = null)
    {
        var (line, column) = CalculateLineColumn(sourceText, offset);
        var (endLine, endColumn) = CalculateLineColumn(sourceText, offset + length);

        return new SourcePosition(line, column, offset, length)
        {
            EndLine = endLine,
            EndColumn = endColumn,
            SourceFile = sourceFile
        };
    }

    private static (int Line, int Column) CalculateLineColumn(string sourceText, int offset)
    {
        var line = 1;
        var column = 1;

        for (var i = 0; i < Math.Min(offset, sourceText.Length); i++)
        {
            if (sourceText[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}