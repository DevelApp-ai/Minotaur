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

using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;

namespace GolemCognitiveGraph.Advanced;

/// <summary>
/// Provides context-aware editing capabilities and rule activation callbacks.
/// Implements the remaining 5% of GrammarForge documentation requirements.
/// </summary>
public class ContextAwareEditor
{
    private readonly GraphEditor _editor;
    private readonly List<IRuleActivationCallback> _callbacks = new();

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

    private async Task<EditContext> BuildEditContextAsync(ContextualEdit edit)
    {
        var targetNode = FindNodeAtPosition(edit.TargetPosition);
        var contextNodes = FindContextNodes(targetNode, edit.ContextRadius);
        
        return new EditContext
        {
            TargetNode = targetNode,
            ContextNodes = contextNodes,
            SourcePosition = edit.TargetPosition,
            EditType = edit.Type,
            Metadata = edit.Metadata
        };
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
    public EditType Type { get; set; }
    public SourcePosition TargetPosition { get; set; } = new(0, 0, 0, 0);
    public CognitiveGraphNode? NewNode { get; set; }
    public CognitiveGraphNode? NewParent { get; set; }
    public object? NewValue { get; set; }
    public int ContextRadius { get; set; } = 2;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the context of an edit operation.
/// </summary>
public class EditContext
{
    public CognitiveGraphNode? TargetNode { get; set; }
    public List<CognitiveGraphNode> ContextNodes { get; set; } = new();
    public SourcePosition SourcePosition { get; set; } = new(0, 0, 0, 0);
    public EditType EditType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of an edit operation.
/// </summary>
public class EditResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CognitiveGraphNode> ModifiedNodes { get; set; } = new();

    public static EditResult Success(IEnumerable<CognitiveGraphNode> modifiedNodes)
    {
        return new EditResult
        {
            IsSuccessful = true,
            ModifiedNodes = modifiedNodes.ToList()
        };
    }

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
    Insert,
    Update,
    Delete,
    Move
}

/// <summary>
/// Interface for rule activation callbacks.
/// </summary>
public interface IRuleActivationCallback
{
    Task BeforeEditAsync(EditContext context);
    Task AfterEditAsync(EditContext context, EditResult result);
}

/// <summary>
/// Interface for precise location tracking.
/// </summary>
public interface ILocationTracker
{
    SourcePosition GetPositionAt(int offset);
    SourcePosition GetPositionAt(int line, int column);
    int GetOffsetAt(int line, int column);
    SourcePosition[] GetPositionsInRange(SourcePosition range);
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

    public PrecisionLocationTracker(string sourceText, string? sourceFile = null)
    {
        _sourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
        _sourceFile = sourceFile;
        _lineOffsets = BuildLineOffsets();
    }

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

    public int GetOffsetAt(int line, int column)
    {
        var position = GetPositionAt(line, column);
        return position.Offset;
    }

    public SourcePosition[] GetPositionsInRange(SourcePosition range)
    {
        var positions = new List<SourcePosition>();
        
        for (var offset = range.Offset; offset < range.Offset + range.Length; offset++)
        {
            positions.Add(GetPositionAt(offset));
        }

        return positions.ToArray();
    }

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