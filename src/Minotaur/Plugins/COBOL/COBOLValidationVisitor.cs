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

namespace Minotaur.Plugins.COBOL;

/// <summary>
/// Visitor for validating COBOL code during unparsing.
/// Checks for COBOL-specific issues and ensures the graph can be unparsed to valid COBOL.
/// </summary>
public class COBOLValidationVisitor : SymbolicAnalysisVisitorBase, ISymbolicAnalysisVisitor
{
    private readonly System.Collections.Generic.List<UnparseValidationError> _errors = new();
    private readonly System.Collections.Generic.List<UnparseValidationError> _warnings = new();
    private int _currentDepth = 0;
    private int _maxDepth = 100;
    
    private bool _hasIdentificationDivision = false;
    private bool _hasDataDivision = false;
    private bool _hasProcedureDivision = false;
    private bool _inProcedureDivision = false;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public COBOLValidationVisitor()
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
        _hasIdentificationDivision = false;
        _hasDataDivision = false;
        _hasProcedureDivision = false;
        _inProcedureDivision = false;
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
        // Check for required divisions
        if (!_hasIdentificationDivision)
        {
            AddError(null, "CV001", "Missing IDENTIFICATION DIVISION", ValidationSeverity.Error);
        }
        
        if (!_hasProcedureDivision)
        {
            AddError(null, "CV002", "Missing PROCEDURE DIVISION", ValidationSeverity.Error);
        }
        
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

        if (_currentDepth > _maxDepth)
        {
            AddError(node, "CV003", "Maximum nesting depth exceeded", ValidationSeverity.Error);
            return;
        }

        _currentDepth++;
        
        try
        {
            if (node is CognitiveGraph.SymbolNode symbolNode)
            {
                VisitSymbolNode(symbolNode);
            }
            else
            {
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
        var packedNodes = node.GetPackedNodes();
        
        if (packedNodes.Count == 0)
        {
            ValidateLeafNode(node);
            return;
        }
        
        // Check for ambiguity
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
        var nodeType = GetNodeType(packedNode);
        
        // Track divisions
        switch (nodeType)
        {
            case "identification_division":
                _hasIdentificationDivision = true;
                break;
            case "data_division":
                _hasDataDivision = true;
                break;
            case "procedure_division":
                _hasProcedureDivision = true;
                _inProcedureDivision = true;
                break;
        }
        
        // Validate the packed node
        ValidatePackedNode(packedNode);
        
        // Visit children
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        // Reset procedure division flag
        if (nodeType == "procedure_division")
        {
            _inProcedureDivision = false;
        }
    }

    /// <summary>
    /// Gets the node type from a PackedNode.
    /// </summary>
    private string GetNodeType(CognitiveGraph.PackedNode packedNode)
    {
        return "unknown";
    }

    /// <summary>
    /// Validates a leaf node.
    /// </summary>
    private void ValidateLeafNode(CognitiveGraph.SymbolNode node)
    {
        var text = node.GetSourceText();
        if (string.IsNullOrEmpty(text.ToString()))
        {
            AddWarning(node, "CV004", "Empty node content", ValidationSeverity.Warning);
        }
        
        if (node.NodeType == 0)
        {
            AddError(node, "CV005", "Invalid node type (0)", ValidationSeverity.Error);
        }
    }

    /// <summary>
    /// Validates ambiguity.
    /// </summary>
    private void ValidateAmbiguity(CognitiveGraph.SymbolNode node, CognitiveGraph.PackedNodeOffsetCollection packedNodes)
    {
        // COBOL typically has less ambiguity than other languages
        // But we should still check for excessive ambiguity
        if (packedNodes.Count > 3)
        {
            AddWarning(node, "CV006", $"High ambiguity: {packedNodes.Count} PackedNodes", ValidationSeverity.Warning);
        }
    }

    /// <summary>
    /// Validates a PackedNode.
    /// </summary>
    private void ValidatePackedNode(CognitiveGraph.PackedNode packedNode)
    {
        if (packedNode.RuleID == 0)
        {
            AddError(packedNode, "CV007", "Invalid RuleId (0)", ValidationSeverity.Error);
        }
        
        var childNodes = packedNode.GetChildNodes();
        if (childNodes.Count == 0)
        {
            AddWarning(packedNode, "CV008", "PackedNode with no children", ValidationSeverity.Info);
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
            NodeType = node?.NodeType.ToString() ?? "unknown",
            SourceStart = node?.SourceStart ?? 0,
            SourceLength = node?.SourceLength ?? 0
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
            NodeType = node?.NodeType.ToString() ?? "unknown",
            SourceStart = node?.SourceStart ?? 0,
            SourceLength = node?.SourceLength ?? 0
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
/// Validation severity levels.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Unparse validation result.
/// </summary>
public class UnparseValidationResult
{
    public bool IsValid { get; set; } = true;
    public System.Collections.Generic.List<UnparseValidationError> Errors { get; set; } = new();
    public System.Collections.Generic.List<UnparseValidationError> Warnings { get; set; } = new();
    
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
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public string NodeType { get; set; } = string.Empty;
    public uint SourceStart { get; set; }
    public uint SourceLength { get; set; }
    public uint RuleId { get; set; }
    
    public override string ToString()
    {
        return $"[{Severity}] {Code}: {Message} (Node: {NodeType}, Position: {SourceStart}-{SourceStart + SourceLength})";
    }
}
