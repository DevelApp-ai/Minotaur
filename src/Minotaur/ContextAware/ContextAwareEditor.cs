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
using Minotaur.Editor;

namespace Minotaur.ContextAware;

/// <summary>
/// Provides context-aware editing capabilities and rule activation callbacks.
/// Implements the remaining 5% of GrammarForge documentation requirements.
/// </summary>
public class ContextAwareEditor
{
    private readonly GraphEditor _editor;
    private readonly List<IRuleActivationCallback> _callbacks = new();

    /// <summary>
    /// Initializes a new instance of the ContextAwareEditor with the specified graph editor.
    /// </summary>
    /// <param name="editor">The graph editor to use for contextual operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when editor is null.</exception>
    public ContextAwareEditor(GraphEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    /// <summary>
    /// Registers a rule activation callback for surgical operations.
    /// </summary>
    public void RegisterCallback(IRuleActivationCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
    }

    /// <summary>
    /// Removes a rule activation callback.
    /// </summary>
    public bool UnregisterCallback(IRuleActivationCallback callback)
    {
        return _callbacks.Remove(callback);
    }

    /// <summary>
    /// Performs a context-aware edit operation with rule activation.
    /// </summary>
    public async Task<EditResult> EditWithContextAsync(ContextualEdit edit)
    {
        var context = await BuildEditContextAsync(edit);

        // Trigger before-edit callbacks
        foreach (var callback in _callbacks)
        {
            await callback.BeforeEditAsync(context);
        }

        var result = await ExecuteEditAsync(edit, context);

        // Trigger after-edit callbacks
        foreach (var callback in _callbacks)
        {
            await callback.AfterEditAsync(context, result);
        }

        return result;
    }

    /// <summary>
    /// Creates a precise location tracker for enhanced positioning.
    /// </summary>
    public ILocationTracker CreateLocationTracker(string sourceText, string? sourceFile = null)
    {
        return new PrecisionLocationTracker(sourceText, sourceFile);
    }

    private Task<EditContext> BuildEditContextAsync(ContextualEdit edit)
    {
        var targetNode = FindNodeAtPosition(edit.TargetPosition);
        var contextNodes = FindContextNodes(targetNode, edit.ContextRadius);

        return Task.FromResult(new EditContext
        {
            TargetNode = targetNode,
            ContextNodes = contextNodes,
            SourcePosition = edit.TargetPosition,
            EditType = edit.Type,
            Metadata = edit.Metadata
        });
    }

    private CognitiveGraphNode? FindNodeAtPosition(SourcePosition position)
    {
        return _editor.Root?.FindNodeAt(position);
    }

    private List<CognitiveGraphNode> FindContextNodes(CognitiveGraphNode? targetNode, int radius)
    {
        var contextNodes = new List<CognitiveGraphNode>();

        if (targetNode == null) return contextNodes;

        // Add siblings
        if (targetNode.Parent != null)
        {
            contextNodes.AddRange(targetNode.Parent.Children);
        }

        // Add ancestors up to radius
        var current = targetNode.Parent;
        for (var i = 0; i < radius && current != null; i++)
        {
            contextNodes.Add(current);
            current = current.Parent;
        }

        return contextNodes;
    }

    private async Task<EditResult> ExecuteEditAsync(ContextualEdit edit, EditContext context)
    {
        try
        {
            switch (edit.Type)
            {
                case EditType.Insert:
                    return await ExecuteInsertAsync(edit, context);
                case EditType.Update:
                    return await ExecuteUpdateAsync(edit, context);
                case EditType.Delete:
                    return await ExecuteDeleteAsync(edit, context);
                case EditType.Move:
                    return await ExecuteMoveAsync(edit, context);
                default:
                    throw new ArgumentException($"Unsupported edit type: {edit.Type}");
            }
        }
        catch (Exception ex)
        {
            return new EditResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ModifiedNodes = new List<CognitiveGraphNode>()
            };
        }
    }

    private async Task<EditResult> ExecuteInsertAsync(ContextualEdit edit, EditContext context)
    {
        if (context.TargetNode == null || edit.NewNode == null)
        {
            return EditResult.Failed("Target node or new node is null");
        }

        context.TargetNode.AddChild(edit.NewNode);

        await Task.CompletedTask;
        return EditResult.Success(new[] { context.TargetNode, edit.NewNode });
    }

    private async Task<EditResult> ExecuteUpdateAsync(ContextualEdit edit, EditContext context)
    {
        if (context.TargetNode == null)
        {
            return EditResult.Failed("Target node is null");
        }

        // Update node properties based on edit
        if (edit.NewValue != null)
        {
            context.TargetNode.Metadata["UpdatedValue"] = edit.NewValue;
        }

        await Task.CompletedTask;
        return EditResult.Success(new[] { context.TargetNode });
    }

    private async Task<EditResult> ExecuteDeleteAsync(ContextualEdit edit, EditContext context)
    {
        if (context.TargetNode?.Parent == null)
        {
            return EditResult.Failed("Cannot delete root node or node without parent");
        }

        context.TargetNode.Parent.RemoveChild(context.TargetNode);

        await Task.CompletedTask;
        return EditResult.Success(new[] { context.TargetNode.Parent });
    }

    private async Task<EditResult> ExecuteMoveAsync(ContextualEdit edit, EditContext context)
    {
        if (context.TargetNode?.Parent == null || edit.NewParent == null)
        {
            return EditResult.Failed("Cannot move node without parent or new parent");
        }

        context.TargetNode.Parent.RemoveChild(context.TargetNode);
        edit.NewParent.AddChild(context.TargetNode);

        await Task.CompletedTask;
        return EditResult.Success(new[] { context.TargetNode, edit.NewParent });
    }
}

/// <summary>
/// Represents a contextual edit operation with enhanced precision.
/// </summary>
public class ContextualEdit
{
    /// <summary>
    /// Gets or sets the type of edit operation to perform.
    /// </summary>
    public EditType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the target position within the source for the edit operation.
    /// </summary>
    public SourcePosition TargetPosition { get; set; } = new(0, 0, 0, 0);
    
    /// <summary>
    /// Gets or sets the new node to be inserted or used in the edit operation.
    /// </summary>
    public CognitiveGraphNode? NewNode { get; set; }
    
    /// <summary>
    /// Gets or sets the new parent node for the edit operation.
    /// </summary>
    public CognitiveGraphNode? NewParent { get; set; }
    
    /// <summary>
    /// Gets or sets the new value to be assigned during the edit operation.
    /// </summary>
    public object? NewValue { get; set; }
    
    /// <summary>
    /// Gets or sets the radius of context nodes to consider around the target position.
    /// </summary>
    public int ContextRadius { get; set; } = 2;
    
    /// <summary>
    /// Gets or sets additional metadata for the edit operation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the context of an edit operation.
/// </summary>
public class EditContext
{
    /// <summary>
    /// Gets or sets the target node for the edit operation.
    /// </summary>
    public CognitiveGraphNode? TargetNode { get; set; }
    
    /// <summary>
    /// Gets or sets the list of context nodes surrounding the target.
    /// </summary>
    public List<CognitiveGraphNode> ContextNodes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the source position of the edit operation.
    /// </summary>
    public SourcePosition SourcePosition { get; set; } = new(0, 0, 0, 0);
    
    /// <summary>
    /// Gets or sets the type of edit operation being performed.
    /// </summary>
    public EditType EditType { get; set; }
    
    /// <summary>
    /// Gets or sets additional metadata for the edit context.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of an edit operation.
/// </summary>
public class EditResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the edit operation was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the list of nodes that were modified during the operation.
    /// </summary>
    public List<CognitiveGraphNode> ModifiedNodes { get; set; } = new();

    /// <summary>
    /// Creates a successful edit result with the specified modified nodes.
    /// </summary>
    /// <param name="modifiedNodes">The nodes that were modified during the operation.</param>
    /// <returns>A successful EditResult instance.</returns>
    public static EditResult Success(IEnumerable<CognitiveGraphNode> modifiedNodes)
    {
        return new EditResult
        {
            IsSuccessful = true,
            ModifiedNodes = modifiedNodes.ToList()
        };
    }

    /// <summary>
    /// Creates a failed edit result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the operation failed.</param>
    /// <returns>A failed EditResult instance.</returns>
    public static EditResult Failed(string errorMessage)
    {
        return new EditResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ModifiedNodes = new List<CognitiveGraphNode>()
        };
    }
}

/// <summary>
/// Enum for edit operation types.
/// </summary>
public enum EditType
{
    /// <summary>
    /// Insert a new node into the graph.
    /// </summary>
    Insert,
    
    /// <summary>
    /// Update an existing node in the graph.
    /// </summary>
    Update,
    
    /// <summary>
    /// Delete a node from the graph.
    /// </summary>
    Delete,
    
    /// <summary>
    /// Move a node to a different position in the graph.
    /// </summary>
    Move
}

/// <summary>
/// Interface for rule activation callbacks.
/// </summary>
public interface IRuleActivationCallback
{
    /// <summary>
    /// Called before an edit operation is performed.
    /// </summary>
    /// <param name="context">The context of the edit operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeforeEditAsync(EditContext context);
    
    /// <summary>
    /// Called after an edit operation is completed.
    /// </summary>
    /// <param name="context">The context of the edit operation.</param>
    /// <param name="result">The result of the edit operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AfterEditAsync(EditContext context, EditResult result);
}

/// <summary>
/// Interface for precise location tracking.
/// </summary>
public interface ILocationTracker
{
    /// <summary>
    /// Gets the source position at the specified character offset.
    /// </summary>
    /// <param name="offset">The character offset in the source text.</param>
    /// <returns>The source position at the specified offset.</returns>
    SourcePosition GetPositionAt(int offset);
    
    /// <summary>
    /// Gets the source position at the specified line and column.
    /// </summary>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>The source position at the specified line and column.</returns>
    SourcePosition GetPositionAt(int line, int column);
    
    /// <summary>
    /// Gets the character offset at the specified line and column.
    /// </summary>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>The character offset at the specified position.</returns>
    int GetOffsetAt(int line, int column);
    
    /// <summary>
    /// Gets all positions within the specified range.
    /// </summary>
    /// <param name="range">The source position range to query.</param>
    /// <returns>An array of source positions within the range.</returns>
    SourcePosition[] GetPositionsInRange(SourcePosition range);
    
    /// <summary>
    /// Determines whether the specified position is valid within the source text.
    /// </summary>
    /// <param name="position">The position to validate.</param>
    /// <returns>true if the position is valid; otherwise, false.</returns>
    bool IsValidPosition(SourcePosition position);
}

/// <summary>
/// High-precision location tracker implementation.
/// </summary>
public class PrecisionLocationTracker : ILocationTracker
{
    private readonly string _sourceText;
    private readonly string? _sourceFile;
    private readonly int[] _lineOffsets;

    /// <summary>
    /// Initializes a new instance of the PrecisionLocationTracker with the specified source text.
    /// </summary>
    /// <param name="sourceText">The source text to track positions within.</param>
    /// <param name="sourceFile">The optional source file name.</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceText is null.</exception>
    public PrecisionLocationTracker(string sourceText, string? sourceFile = null)
    {
        _sourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
        _sourceFile = sourceFile;
        _lineOffsets = BuildLineOffsets();
    }

    /// <summary>
    /// Gets the source position (line and column) at the specified character offset.
    /// </summary>
    /// <param name="offset">The zero-based character offset in the source text.</param>
    /// <returns>A <see cref="SourcePosition"/> containing the line, column, and offset information.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset"/> is negative or exceeds the source text length.</exception>
    public SourcePosition GetPositionAt(int offset)
    {
        if (offset < 0 || offset > _sourceText.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var line = Array.BinarySearch(_lineOffsets, offset);
        if (line < 0)
        {
            line = ~line - 1;
        }

        var column = offset - _lineOffsets[line] + 1;

        return new SourcePosition(line + 1, column, offset, 0)
        {
            SourceFile = _sourceFile
        };
    }

    /// <summary>
    /// Gets the source position at the specified line and column coordinates.
    /// </summary>
    /// <param name="line">The one-based line number.</param>
    /// <param name="column">The one-based column number.</param>
    /// <returns>A <see cref="SourcePosition"/> containing the line, column, and calculated offset information.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="line"/> or <paramref name="column"/> are invalid.</exception>
    public SourcePosition GetPositionAt(int line, int column)
    {
        if (line < 1 || line > _lineOffsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(line));
        }

        var offset = _lineOffsets[line - 1] + column - 1;

        if (offset < 0 || offset > _sourceText.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        return new SourcePosition(line, column, offset, 0)
        {
            SourceFile = _sourceFile
        };
    }

    /// <summary>
    /// Gets the character offset at the specified line and column coordinates.
    /// </summary>
    /// <param name="line">The one-based line number.</param>
    /// <param name="column">The one-based column number.</param>
    /// <returns>The zero-based character offset in the source text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="line"/> or <paramref name="column"/> are invalid.</exception>
    public int GetOffsetAt(int line, int column)
    {
        var position = GetPositionAt(line, column);
        return position.Offset;
    }

    /// <summary>
    /// Gets all source positions within the specified range.
    /// </summary>
    /// <param name="range">The source position range to get positions for, where Length specifies the range size.</param>
    /// <returns>An array of <see cref="SourcePosition"/> objects covering each character in the range.</returns>
    public SourcePosition[] GetPositionsInRange(SourcePosition range)
    {
        var positions = new List<SourcePosition>();

        for (var offset = range.Offset; offset < range.Offset + range.Length; offset++)
        {
            positions.Add(GetPositionAt(offset));
        }

        return positions.ToArray();
    }

    /// <summary>
    /// Determines whether the specified source position is valid for this source text.
    /// </summary>
    /// <param name="position">The source position to validate.</param>
    /// <returns><c>true</c> if the position is within valid bounds; otherwise, <c>false</c>.</returns>
    public bool IsValidPosition(SourcePosition position)
    {
        return position.Line >= 1 &&
               position.Line <= _lineOffsets.Length &&
               position.Column >= 1 &&
               position.Offset >= 0 &&
               position.Offset <= _sourceText.Length;
    }

    private int[] BuildLineOffsets()
    {
        var offsets = new List<int> { 0 };

        for (var i = 0; i < _sourceText.Length; i++)
        {
            if (_sourceText[i] == '\n')
            {
                offsets.Add(i + 1);
            }
        }

        return offsets.ToArray();
    }
}