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

namespace Minotaur.Plugins.Go;

/// <summary>
/// Visitor for unparsing Go code from a CognitiveGraph.
/// Handles Go-specific syntax including goroutines, channels, and interfaces.
/// </summary>
public class GoUnparseVisitor : UnparseVisitorBase
{
    private readonly System.Text.StringBuilder _builder = new();
    private int _indentLevel = 0;
    private bool _atLineStart = true;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GoUnparseVisitor()
    {
    }

    /// <summary>
    /// Initializes the visitor.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _builder.Clear();
        _indentLevel = 0;
        _atLineStart = true;
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

        if (node is CognitiveGraph.SymbolNode symbolNode)
        {
            VisitSymbolNode(symbolNode);
        }
        else
        {
            base.Visit(node);
        }
    }

    /// <summary>
    /// Visits a SymbolNode and handles its PackedNodes.
    /// </summary>
    private void VisitSymbolNode(CognitiveGraph.SymbolNode node)
    {
        var packedNodes = node.GetPackedNodes();
        
        if (packedNodes.Count == 0)
        {
            WriteNode(node);
            return;
        }
        
        // For Go, select the first valid PackedNode
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
    private void VisitPackedNode(CognitiveGraph.PackedNode packedNode)
    {
        var ruleId = packedNode.RuleID;
        var childNodes = packedNode.GetChildNodes();
        var nodeType = GetNodeType(packedNode);
        
        switch (nodeType)
        {
            case "package_declaration":
                VisitPackageDeclaration(packedNode, childNodes);
                break;
            case "import_declaration":
                VisitImportDeclaration(packedNode, childNodes);
                break;
            case "function_declaration":
                VisitFunctionDeclaration(packedNode, childNodes);
                break;
            case "method_declaration":
                VisitMethodDeclaration(packedNode, childNodes);
                break;
            case "struct_declaration":
                VisitStructDeclaration(packedNode, childNodes);
                break;
            case "interface_declaration":
                VisitInterfaceDeclaration(packedNode, childNodes);
                break;
            case "type_alias":
                VisitTypeAlias(packedNode, childNodes);
                break;
            case "variable_declaration":
                VisitVariableDeclaration(packedNode, childNodes);
                break;
            case "const_declaration":
                VisitConstDeclaration(packedNode, childNodes);
                break;
            case "if_statement":
                VisitIfStatement(packedNode, childNodes);
                break;
            case "if_else_statement":
                VisitIfElseStatement(packedNode, childNodes);
                break;
            case "switch_statement":
                VisitSwitchStatement(packedNode, childNodes);
                break;
            case "for_loop":
                VisitForLoop(packedNode, childNodes);
                break;
            case "range_loop":
                VisitRangeLoop(packedNode, childNodes);
                break;
            case "go_statement":
                VisitGoStatement(packedNode, childNodes);
                break;
            case "channel_declaration":
                VisitChannelDeclaration(packedNode, childNodes);
                break;
            case "select_statement":
                VisitSelectStatement(packedNode, childNodes);
                break;
            case "defer_statement":
                VisitDeferStatement(packedNode, childNodes);
                break;
            case "return_statement":
                VisitReturnStatement(packedNode, childNodes);
                break;
            case "comment":
                VisitComment(packedNode, childNodes);
                break;
            case "doc_comment":
                VisitDocComment(packedNode, childNodes);
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
    private string GetNodeType(CognitiveGraph.PackedNode packedNode)
    {
        return "unknown";
    }

    /// <summary>
    /// Checks if a PackedNode is valid for unparsing.
    /// </summary>
    private bool IsValidPackedNode(CognitiveGraph.PackedNode packedNode)
    {
        var childNodes = packedNode.GetChildNodes();
        return childNodes.Count > 0 || packedNode.RuleID > 0;
    }

    /// <summary>
    /// Writes a node directly.
    /// </summary>
    private void WriteNode(CognitiveGraph.SymbolNode node)
    {
        var text = node.GetSourceText();
        if (!string.IsNullOrEmpty(text))
        {
            Write(text.ToString());
        }
    }

    /// <summary>
    /// Writes text to the output.
    /// </summary>
    private void Write(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (_atLineStart)
        {
            WriteIndent();
            _atLineStart = false;
        }

        _builder.Append(text);
        
        // Check for newlines
        if (text.Contains("\n"))
        {
            _atLineStart = true;
        }
    }

    /// <summary>
    /// Writes a newline.
    /// </summary>
    private void WriteLine()
    {
        _builder.AppendLine();
        _atLineStart = true;
    }

    /// <summary>
    /// Writes the current indentation (tabs for Go).
    /// </summary>
    private void WriteIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _builder.Append("\t");
        }
    }

    /// <summary>
    /// Increases the indentation level.
    /// </summary>
    private void Indent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the indentation level.
    /// </summary>
    private void Unindent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
    }

    /// <summary>
    /// Visits a package declaration.
    /// </summary>
    private void VisitPackageDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("package ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits an import declaration.
    /// </summary>
    private void VisitImportDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("import (");
        WriteLine();
        
        // Visit imports
        foreach (var child in childNodes)
        {
            Write("\t");
            Visit(child);
            WriteLine();
        }
        
        Write(")");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits a function declaration.
    /// </summary>
    private void VisitFunctionDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("func ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(");
        
        // Visit parameters
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(") ");
        
        // Visit return type
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
            Write(" ");
        }
        
        Write("{");
        
        // Visit body
        if (childNodes.Count > 3)
        {
            WriteLine();
            Indent();
            Visit(childNodes[3]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits a method declaration.
    /// </summary>
    private void VisitMethodDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("func ");
        
        // Visit receiver
        if (childNodes.Count > 0)
        {
            Write("(");
            Visit(childNodes[0]);
            Write(") ");
        }
        
        // Visit name
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write("(");
        
        // Visit parameters
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
        }
        
        Write(") ");
        
        // Visit return type
        if (childNodes.Count > 3)
        {
            Visit(childNodes[3]);
            Write(" ");
        }
        
        Write("{");
        
        // Visit body
        if (childNodes.Count > 4)
        {
            WriteLine();
            Indent();
            Visit(childNodes[4]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits a struct declaration.
    /// </summary>
    private void VisitStructDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("type ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" struct {");
        
        // Visit fields
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits an interface declaration.
    /// </summary>
    private void VisitInterfaceDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("type ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" interface {");
        
        // Visit methods
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits a type alias.
    /// </summary>
    private void VisitTypeAlias(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("type ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" ");
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits a variable declaration.
    /// </summary>
    private void VisitVariableDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("var ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Write(" ");
            Visit(childNodes[1]);
        }
        
        // Visit value
        if (childNodes.Count > 2)
        {
            Write(" = ");
            Visit(childNodes[2]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a constant declaration.
    /// </summary>
    private void VisitConstDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("const ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Write(" ");
            Visit(childNodes[1]);
        }
        
        // Visit value
        if (childNodes.Count > 2)
        {
            Write(" = ");
            Visit(childNodes[2]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits an if statement.
    /// </summary>
    private void VisitIfStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("if ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit body
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits an if-else statement.
    /// </summary>
    private void VisitIfElseStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("if ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit then body
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write(" } else {");
        
        // Visit else body
        if (childNodes.Count > 2)
        {
            WriteLine();
            Indent();
            Visit(childNodes[2]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a switch statement.
    /// </summary>
    private void VisitSwitchStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("switch ");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit cases
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a for loop.
    /// </summary>
    private void VisitForLoop(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("for ");
        
        // Visit initialization
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("; ");
        
        // Visit condition
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write("; ");
        
        // Visit post
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
        }
        
        Write(" {");
        
        // Visit body
        if (childNodes.Count > 3)
        {
            WriteLine();
            Indent();
            Visit(childNodes[3]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a range loop.
    /// </summary>
    private void VisitRangeLoop(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("for ");
        
        // Visit index and value
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" := range ");
        
        // Visit collection
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(" {");
        
        // Visit body
        if (childNodes.Count > 2)
        {
            WriteLine();
            Indent();
            Visit(childNodes[2]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a go statement (goroutine).
    /// </summary>
    private void VisitGoStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("go ");
        
        // Visit function call
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a channel declaration.
    /// </summary>
    private void VisitChannelDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" := make(chan ");
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        // Visit buffer
        if (childNodes.Count > 2)
        {
            Write(", ");
            Visit(childNodes[2]);
        }
        
        Write(")");
        WriteLine();
    }

    /// <summary>
    /// Visits a select statement.
    /// </summary>
    private void VisitSelectStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("select {");
        
        // Visit cases
        if (childNodes.Count > 0)
        {
            WriteLine();
            Indent();
            Visit(childNodes[0]);
            Unindent();
        }
        else
        {
            WriteLine();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a defer statement.
    /// </summary>
    private void VisitDeferStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("defer ");
        
        // Visit function call
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a return statement.
    /// </summary>
    private void VisitReturnStatement(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("return ");
        
        // Visit values
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a comment.
    /// </summary>
    private void VisitComment(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("// ");
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a doc comment.
    /// </summary>
    private void VisitDocComment(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("// ");
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        WriteLine();
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
    /// Disposes the visitor.
    /// </summary>
    public virtual void Dispose() { }
}
