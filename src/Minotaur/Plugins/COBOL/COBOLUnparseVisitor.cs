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
using Minotaur.Core;

namespace Minotaur.Plugins.COBOL;

/// <summary>
/// Visitor for unparsing COBOL code from a CognitiveGraph.
/// Handles COBOL-specific syntax including divisions, sections, and data descriptions.
/// </summary>
public class COBOLUnparseVisitor : UnparseVisitorBase
{
    private readonly System.Text.StringBuilder _builder = new();
    private int _currentColumn = 0;
    private int _currentLine = 1;
    private int _margin = 8; // Standard COBOL margin
    private bool _inDataDivision = false;
    private bool _inProcedureDivision = false;
    private bool _atLineStart = true;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public COBOLUnparseVisitor()
    {
    }

    /// <summary>
    /// Initializes the visitor.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _builder.Clear();
        _currentColumn = 0;
        _currentLine = 1;
        _inDataDivision = false;
        _inProcedureDivision = false;
        _atLineStart = true;
        // Write COBOL header (optional)
        // Most COBOL programs start with identification division
    }

    /// <summary>
    /// Resets the visitor state.
    /// </summary>
    public override void Reset()
    {
        Initialize();
    }

    /// <summary>
    /// Gets the generated code.
    /// </summary>
    public override string GetGeneratedCode()
    {
        return _builder.ToString();
    }

    /// <summary>
    /// Visits the specified node.
    /// </summary>
    public override void Visit(CognitiveGraphNode node)
    {
        if (node == null)
            return;

        // CognitiveGraphNode is a class-based wrapper. The zero-copy
        // CognitiveGraph.Accessors.SymbolNode type is a ref struct and cannot be reached
        // from a class reference, so the class-based graph is traversed here. The
        // zero-copy path is entered through the Visit(SymbolNode) overload below.
        base.Visit(node);
    }

    /// <summary>
    /// Visits a zero-copy SymbolNode (entered while walking packed-node children).
    /// </summary>
    public override void Visit(CognitiveGraph.Accessors.SymbolNode node)
    {
        VisitSymbolNode(node);
    }

    /// <summary>
    /// Visits a SymbolNode and handles its PackedNodes.
    /// </summary>
    private void VisitSymbolNode(CognitiveGraph.Accessors.SymbolNode node)
    {
        var packedNodes = node.GetPackedNodes();
        if (packedNodes.Count == 0)
        {
            WriteNode(node);
            return;
        }
        // For COBOL, select the first valid PackedNode
        foreach (var packedNode in packedNodes)
        {
            if (IsValidPackedNode(packedNode))
            {
                VisitPackedNode(packedNode);
                return;
            }
        }
        WriteNode(node);
    }

    /// <summary>
    /// Visits a PackedNode.
    /// </summary>
    private void VisitPackedNode(CognitiveGraph.Accessors.PackedNode packedNode)
    {
        var ruleId = packedNode.RuleID;
        var childNodes = packedNode.GetChildNodes();
        var nodeType = GetNodeType(packedNode);
        switch (nodeType)
        {
            case "identification_division":
                VisitIdentificationDivision(packedNode, childNodes);
                break;
            case "data_division":
                VisitDataDivision(packedNode, childNodes);
                break;
            case "environment_division":
                VisitEnvironmentDivision(packedNode, childNodes);
                break;
            case "procedure_division":
                VisitProcedureDivision(packedNode, childNodes);
                break;
            case "program_id":
                VisitProgramId(packedNode, childNodes);
                break;
            case "working_storage_section":
                VisitWorkingStorageSection(packedNode, childNodes);
                break;
            case "file_section":
                VisitFileSection(packedNode, childNodes);
                break;
            case "paragraph":
                VisitParagraph(packedNode, childNodes);
                break;
            case "data_description_entry":
                VisitDataDescriptionEntry(packedNode, childNodes);
                break;
            case "picture_clause":
                VisitPictureClause(packedNode, childNodes);
                break;
            case "value_clause":
                VisitValueClause(packedNode, childNodes);
                break;
            case "move_statement":
                VisitMoveStatement(packedNode, childNodes);
                break;
            case "display_statement":
                VisitDisplayStatement(packedNode, childNodes);
                break;
            case "accept_statement":
                VisitAcceptStatement(packedNode, childNodes);
                break;
            case "if_statement":
                VisitIfStatement(packedNode, childNodes);
                break;
            case "perform_statement":
                VisitPerformStatement(packedNode, childNodes);
                break;
            case "call_statement":
                VisitCallStatement(packedNode, childNodes);
                break;
            case "division_header":
                VisitDivisionHeader(packedNode, childNodes);
                break;
            case "section_header":
                VisitSectionHeader(packedNode, childNodes);
                break;
            default:
                // Visit children
                foreach (var child in childNodes)
                {
                    Visit(child);
                }
                break;
        }
    }

    /// <summary>
    /// Gets the node type from a PackedNode.
    /// </summary>
    private string GetNodeType(CognitiveGraph.Accessors.PackedNode packedNode)
    {
        return "unknown";
    }

    /// <summary>
    /// Checks if a PackedNode is valid for unparsing.
    /// </summary>
    private bool IsValidPackedNode(CognitiveGraph.Accessors.PackedNode packedNode)
    {
        var childNodes = packedNode.GetChildNodes();
        return childNodes.Count > 0 || packedNode.RuleID > 0;
    }

    /// <summary>
    /// Writes a node directly.
    /// </summary>
    private void WriteNode(CognitiveGraph.Accessors.SymbolNode node)
    {
        var text = node.GetSourceText();
        if (!text.IsEmpty)
        {
            Write(text.ToString());
        }
    }

    /// <summary>
    /// Writes text to the output at the current position.
    /// </summary>
    private void Write(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Handle line breaks
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                NewLine();
            }
            WriteLineContent(lines[i]);
        }
    }

    /// <summary>
    /// Writes content on the current line.
    /// </summary>
    private void WriteLineContent(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Ensure we're at the margin for COBOL
        if (_atLineStart)
        {
            WriteIndent();
            _atLineStart = false;
        }

        _builder.Append(text);
        _currentColumn += text.Length;
    }

    /// <summary>
    /// Writes indentation based on current context.
    /// </summary>
    private void WriteIndent()
    {
        if (_inDataDivision || _inProcedureDivision)
        {
            // Standard COBOL margin
            for (int i = 0; i < _margin; i++)
            {
                _builder.Append(" ");
                _currentColumn++;
            }
        }
        else
        {
            // Identification division typically starts at column 8
            for (int i = 0; i < _margin; i++)
            {
                _builder.Append(" ");
                _currentColumn++;
            }
        }
    }

    /// <summary>
    /// Starts a new line.
    /// </summary>
    private void NewLine()
    {
        _builder.AppendLine();
        _currentLine++;
        _currentColumn = 0;
        _atLineStart = true;
    }

    /// <summary>
    /// Visits the identification division.
    /// </summary>
    private void VisitIdentificationDivision(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        _inDataDivision = false;
        _inProcedureDivision = false;
        Write("       IDENTIFICATION DIVISION.");
        NewLine();
        // Visit children (should include program-id, etc.)
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits the data division.
    /// </summary>
    private void VisitDataDivision(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        _inDataDivision = true;
        _inProcedureDivision = false;
        Write("       DATA DIVISION.");
        NewLine();
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        _inDataDivision = false;
    }

    /// <summary>
    /// Visits the environment division.
    /// </summary>
    private void VisitEnvironmentDivision(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       ENVIRONMENT DIVISION.");
        NewLine();
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits the procedure division.
    /// </summary>
    private void VisitProcedureDivision(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        _inDataDivision = false;
        _inProcedureDivision = true;
        Write("       PROCEDURE DIVISION.");
        NewLine();
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        _inProcedureDivision = false;
    }

    /// <summary>
    /// Visits the program ID paragraph.
    /// </summary>
    private void VisitProgramId(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       PROGRAM-ID. ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits the working storage section.
    /// </summary>
    private void VisitWorkingStorageSection(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       WORKING-STORAGE SECTION.");
        NewLine();
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits the file section.
    /// </summary>
    private void VisitFileSection(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       FILE SECTION.");
        NewLine();
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a paragraph.
    /// </summary>
    private void VisitParagraph(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // Get paragraph name
        if (childNodes.Count > 0)
        {
            var nameNode = childNodes[0];
            var name = nameNode.GetSourceText().ToString();
            Write($"{name}.");
            NewLine();
            // Visit remaining children (statements)
            for (int i = 1; i < childNodes.Count; i++)
            {
                Visit(childNodes[i]);
            }
        }
    }

    /// <summary>
    /// Visits a data description entry.
    /// </summary>
    private void VisitDataDescriptionEntry(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // Level number should be the first child
        if (childNodes.Count > 0)
        {
            var levelNode = childNodes[0];
            var level = levelNode.GetSourceText().ToString();
            Write($"       {level} ");
            // Visit remaining children (name, picture, value, etc.)
            for (int i = 1; i < childNodes.Count; i++)
            {
                Visit(childNodes[i]);
                Write(" ");
            }
            Write(".");
            NewLine();
        }
    }

    /// <summary>
    /// Visits a picture clause.
    /// </summary>
    private void VisitPictureClause(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("PIC ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a value clause.
    /// </summary>
    private void VisitValueClause(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("VALUE ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a move statement.
    /// </summary>
    private void VisitMoveStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       MOVE ");
        // First child is source
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        Write(" TO ");
        // Second child is destination
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits a display statement.
    /// </summary>
    private void VisitDisplayStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       DISPLAY ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits an accept statement.
    /// </summary>
    private void VisitAcceptStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       ACCEPT ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits an if statement.
    /// </summary>
    private void VisitIfStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       IF ");
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        NewLine();
        // Visit then statements
        if (childNodes.Count > 1)
        {
            // Manual loop: the zero-copy child collection is a ref struct and cannot
            // be used with LINQ extension methods.
            for (int i = 1; i < childNodes.Count && !IsElseClause(childNodes[i]); i++)
            {
                Visit(childNodes[i]);
            }
        }
        // Check for else
        var elseIndex = -1;
        for (int i = 0; i < childNodes.Count; i++)
        {
            if (IsElseClause(childNodes[i]))
            {
                elseIndex = i;
                break;
            }
        }
        if (elseIndex >= 0)
        {
            Write("       ELSE");
            NewLine();
            // Visit else statements
            for (int i = elseIndex; i < childNodes.Count; i++)
            {
                Visit(childNodes[i]);
            }
        }
        Write("       END-IF.");
        NewLine();
    }

    /// <summary>
    /// Visits a perform statement.
    /// </summary>
    private void VisitPerformStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       PERFORM ");
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits a call statement.
    /// </summary>
    private void VisitCallStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("       CALL ");
        // Visit program name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        // Check for USING clause
        if (childNodes.Count > 1)
        {
            Write(" USING ");
            // Visit parameters
            for (int i = 1; i < childNodes.Count; i++)
            {
                if (i > 1)
                    Write(", ");
                Visit(childNodes[i]);
            }
        }
        Write(".");
        NewLine();
    }

    /// <summary>
    /// Visits a division header.
    /// </summary>
    private void VisitDivisionHeader(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        NewLine();
    }

    /// <summary>
    /// Visits a section header.
    /// </summary>
    private void VisitSectionHeader(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        NewLine();
    }

    /// <summary>
    /// Checks if a node is an else clause.
    /// </summary>
    private bool IsElseClause(CognitiveGraph.Accessors.SymbolNode node)
    {
        // The CognitiveGraph 1.1.x package does not expose a NodeType enum to identify
        // else clauses, so clauses cannot currently be distinguished. TODO: detect else
        // clauses once node-type metadata is available from the graph.
        return false;
    }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public override void Dispose()
    {
        _builder.Clear();
    }
}

/// <summary>
/// Base class for unparse visitors.
/// </summary>
public abstract class UnparseVisitorBase : IDisposable
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
    /// Gets the generated code.
    /// </summary>
    public abstract string GetGeneratedCode();

    /// <summary>
    /// Visits the specified node.
    /// </summary>
    public virtual void Visit(CognitiveGraphNode node) { }

    /// <summary>
    /// Visits the specified zero-copy symbol node.
    /// </summary>
    public virtual void Visit(CognitiveGraph.Accessors.SymbolNode node) { }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public virtual void Dispose() { }
}
