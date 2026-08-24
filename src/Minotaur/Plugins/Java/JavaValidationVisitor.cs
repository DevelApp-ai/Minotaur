/*
 * This file is part of Minotaur.
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>. 
 */

using CognitiveGraph;
using Minotaur.Analysis.Symbolic;
using Minotaur.Core;

namespace Minotaur.Plugins.Java;

/// <summary>
/// Visitor for validating Java code during unparsing.
/// Checks for Java-specific issues and ensures the graph can be unparsed to valid Java.
/// </summary>
public class JavaValidationVisitor : SymbolicAnalysisVisitorBase, ISymbolicAnalysisVisitor
{
    private readonly System.Collections.Generic.List<UnparseValidationError> _errors = new();
    private readonly System.Collections.Generic.List<UnparseValidationError> _warnings = new();
    private int _currentDepth = 0;
    private int _maxDepth = 100; // Prevent infinite recursion

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public JavaValidationVisitor()
    {
    }

    /// <summary>
    /// Initializes the visitor.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _errors.Clear();
        _warnings.Clear();
        _currentDepth = 0;
    }

    /// <summary>
    /// Resets the visitor state.
    /// </summary>
    public override void Reset()
    {
        Initialize();
    }

    /// <summary>
    /// Gets the validation result.
    /// </summary>
    public UnparseValidationResult GetValidationResult()
    {
        return new UnparseValidationResult
        {
            IsValid = _errors.Count == 0,
            Errors = _errors.ToList(),
            Warnings = _warnings.ToList()
        };
    }

    /// <summary>
    /// Visits the specified node.
    /// </summary>
    public override void Visit(CognitiveGraphNode node)
    {
        if (node == null)
            return;

        // Check depth to prevent infinite recursion
        if (_currentDepth > _maxDepth)
        {
            AddError(node, "JV001", "Maximum nesting depth exceeded", ValidationSeverity.Error);
            return;
        }

        _currentDepth++;
        
        try
        {
            // Check if this is a SymbolNode
            if (node is CognitiveGraph.SymbolNode symbolNode)
            {
                VisitSymbolNode(symbolNode);
            }
            else
            {
                // Visit children if it's a container
                base.Visit(node);
            }
        }
        finally
        {
            _currentDepth--;
        }
    }

    /// <summary>
    /// Visits a SymbolNode.
    /// </summary>
    private void VisitSymbolNode(CognitiveGraph.SymbolNode node)
    {
        // Check for PackedNodes (ambiguity)
        var packedNodes = node.GetPackedNodes();
        
        if (packedNodes.Count == 0)
        {
            // No PackedNodes - this is a leaf node
            ValidateLeafNode(node);
            return;
        }
        
        // Multiple PackedNodes - check for ambiguity issues
        if (packedNodes.Count > 1)
        {
            ValidateAmbiguity(node, packedNodes);
        }
        
        // Visit each PackedNode
        foreach (var packedNode in packedNodes)
        {
            VisitPackedNode(packedNode);
        }
    }

    /// <summary>
    /// Visits a PackedNode.
    /// </summary>
    private void VisitPackedNode(CognitiveGraph.PackedNode packedNode)
    {
        var childNodes = packedNode.GetChildNodes();
        
        // Validate the packed node
        ValidatePackedNode(packedNode);
        
        // Visit children
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Validates a leaf node.
    /// </summary>
    private void ValidateLeafNode(CognitiveGraph.SymbolNode node)
    {
        // Check for empty nodes
        var text = node.GetSourceText();
        if (string.IsNullOrEmpty(text.ToString()))
        {
            AddWarning(node, "JV002", "Empty node content", ValidationSeverity.Warning);
        }
        
        // Check for valid node type
        if (node.NodeType == 0)
        {
            AddError(node, "JV003", "Invalid node type (0)", ValidationSeverity.Error);
        }
    }

    /// <summary>
    /// Validates ambiguity (multiple PackedNodes).
    /// </summary>
    private void ValidateAmbiguity(CognitiveGraph.SymbolNode node, CognitiveGraph.PackedNodeOffsetCollection packedNodes)
    {
        // For Java, some ambiguity is acceptable (e.g., expression vs statement)
        // But we should warn about excessive ambiguity
        if (packedNodes.Count > 5)
        {
            AddWarning(node, "JV004", $"High ambiguity: {packedNodes.Count} PackedNodes", ValidationSeverity.Warning);
        }
        
        // Check if all PackedNodes have the same rule ID (redundant)
        var ruleIds = new System.Collections.Generic.HashSet<uint>();
        foreach (var packedNode in packedNodes)
        {
            ruleIds.Add(packedNode.RuleID);
        }
        
        if (ruleIds.Count == 1 && packedNodes.Count > 1)
        {
            AddWarning(node, "JV005", "Multiple PackedNodes with same RuleId", ValidationSeverity.Warning);
        }
    }

    /// <summary>
    /// Validates a PackedNode.
    /// </summary>
    private void ValidatePackedNode(CognitiveGraph.PackedNode packedNode)
    {
        // Check for valid RuleId
        if (packedNode.RuleID == 0)
        {
            AddError(packedNode, "JV006", "Invalid RuleId (0)", ValidationSeverity.Error);
        }
        
        // Check for children
        var childNodes = packedNode.GetChildNodes();
        if (childNodes.Count == 0)
        {
            // A PackedNode with no children might be valid for terminal nodes
            // But we should check if it's expected
            AddWarning(packedNode, "JV007", "PackedNode with no children", ValidationSeverity.Info);
        }
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    private void AddError(CognitiveGraph.SymbolNode node, string code, string message, ValidationSeverity severity)
    {
        var error = new UnparseValidationError
        {
            Code = code,
            Message = message,
            Severity = severity,
            NodeType = node.NodeType.ToString(),
            SourceStart = node.SourceStart,
            SourceLength = node.SourceLength
        };
        
        _errors.Add(error);
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    private void AddError(CognitiveGraph.PackedNode packedNode, string code, string message, ValidationSeverity severity)
    {
        var error = new UnparseValidationError
        {
            Code = code,
            Message = message,
            Severity = severity,
            NodeType = "PackedNode",
            RuleId = packedNode.RuleID
        };
        
        _errors.Add(error);
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    private void AddWarning(CognitiveGraph.SymbolNode node, string code, string message, ValidationSeverity severity)
    {
        var warning = new UnparseValidationError
        {
            Code = code,
            Message = message,
            Severity = severity,
            NodeType = node.NodeType.ToString(),
            SourceStart = node.SourceStart,
            SourceLength = node.SourceLength
        };
        
        _warnings.Add(warning);
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    private void AddWarning(CognitiveGraph.PackedNode packedNode, string code, string message, ValidationSeverity severity)
    {
        var warning = new UnparseValidationError
        {
            Code = code,
            Message = message,
            Severity = severity,
            NodeType = "PackedNode",
            RuleId = packedNode.RuleID
        };
        
        _warnings.Add(warning);
    }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public override void Dispose()
    {
        _errors.Clear();
        _warnings.Clear();
    }
}

/// <summary>
/// Base class for symbolic analysis visitors.
/// </summary>
public abstract class SymbolicAnalysisVisitorBase : IDisposable
{
    /// <summary>
    /// Initializes the visitor.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Resets the visitor state.
    /// </summary>
    public virtual void Reset() { }

    /// <summary>
    /// Visits the specified node.
    /// </summary>
    public virtual void Visit(CognitiveGraphNode node) { }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public virtual void Dispose() { }
}

/// <summary>
/// Unparse validation result.
/// </summary>
public class UnparseValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of errors.
    /// </summary>
    public System.Collections.Generic.List<UnparseValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of warnings.
    /// </summary>
    public System.Collections.Generic.List<UnparseValidationError> Warnings { get; set; } = new();

    /// <summary>
    /// Gets all validation messages.
    /// </summary>
    public System.Collections.Generic.IEnumerable<UnparseValidationError> AllMessages
    {
        get
        {
            foreach (var error in Errors)
                yield return error;
            foreach (var warning in Warnings)
                yield return warning;
        }
    }
}

/// <summary>
/// Unparse validation error.
/// </summary>
public class UnparseValidationError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// Gets or sets the node type.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source start position.
    /// </summary>
    public uint SourceStart { get; set; }

    /// <summary>
    /// Gets or sets the source length.
    /// </summary>
    public uint SourceLength { get; set; }

    /// <summary>
    /// Gets or sets the rule ID (for PackedNode errors).
    /// </summary>
    public uint RuleId { get; set; }

    /// <summary>
    /// Returns a string representation of this error.
    /// </summary>
    public override string ToString()
    {
        return $"[{Severity}] {Code}: {Message} (Node: {NodeType}, Position: {SourceStart}-{SourceStart + SourceLength})";
    }
}

/// <summary>
/// Validation severity levels.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}
