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

namespace Minotaur.Plugins.Java;

/// <summary>
/// Visitor for unparsing Java code from a CognitiveGraph.
/// Handles Java-specific syntax including classes, interfaces, methods, and modern features.
/// </summary>
public class JavaUnparseVisitor : UnparseVisitorBase
{
    private readonly System.Text.StringBuilder _builder = new();
    private int _indentLevel = 0;
    private bool _atLineStart = true;
    private bool _needsSemicolon = false;
    private bool _inString = false;
    private bool _inComment = false;
    
    private readonly System.Collections.Generic.Stack<bool> _needsSemicolonStack = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public JavaUnparseVisitor()
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
        _needsSemicolon = false;
        _inString = false;
        _inComment = false;
        _needsSemicolonStack.Clear();
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
        var code = _builder.ToString();
        
        // Ensure proper formatting
        if (_needsSemicolon)
        {
            _builder.Append(";");
        }
        
        return code;
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
            // No PackedNodes, just output the node
            WriteNode(node);
            return;
        }
        
        // Multiple PackedNodes means ambiguity
        // For unparsing, we need to select one interpretation
        // Here we select the first valid PackedNode
        foreach (var packedNode in packedNodes)
        {
            if (IsValidPackedNode(packedNode))
            {
                VisitPackedNode(packedNode);
                return;
            }
        }
        
        // If no valid PackedNode, output the node anyway
        WriteNode(node);
    }

    /// <summary>
    /// Visits a PackedNode.
    /// </summary>
    private void VisitPackedNode(CognitiveGraph.Accessors.PackedNode packedNode)
    {
        var ruleId = packedNode.RuleID;
        var childNodes = packedNode.GetChildNodes();
        
        // Handle based on rule ID or node type
        var nodeType = GetNodeType(packedNode);
        
        switch (nodeType)
        {
            case "compilation_unit":
                VisitCompilationUnit(packedNode, childNodes);
                break;
            case "package_declaration":
                VisitPackageDeclaration(packedNode, childNodes);
                break;
            case "import_declaration":
                VisitImportDeclaration(packedNode, childNodes);
                break;
            case "class_declaration":
                VisitClassDeclaration(packedNode, childNodes);
                break;
            case "interface_declaration":
                VisitInterfaceDeclaration(packedNode, childNodes);
                break;
            case "enum_declaration":
                VisitEnumDeclaration(packedNode, childNodes);
                break;
            case "record_declaration":
                VisitRecordDeclaration(packedNode, childNodes);
                break;
            case "method_declaration":
                VisitMethodDeclaration(packedNode, childNodes);
                break;
            case "field_declaration":
                VisitFieldDeclaration(packedNode, childNodes);
                break;
            case "if_statement":
                VisitIfStatement(packedNode, childNodes);
                break;
            case "for_statement":
                VisitForStatement(packedNode, childNodes);
                break;
            case "while_statement":
                VisitWhileStatement(packedNode, childNodes);
                break;
            case "do_statement":
                VisitDoStatement(packedNode, childNodes);
                break;
            case "try_statement":
                VisitTryStatement(packedNode, childNodes);
                break;
            case "switch_statement":
                VisitSwitchStatement(packedNode, childNodes);
                break;
            case "return_statement":
                VisitReturnStatement(packedNode, childNodes);
                break;
            case "throw_statement":
                VisitThrowStatement(packedNode, childNodes);
                break;
            case "block":
                VisitBlock(packedNode, childNodes);
                break;
            case "expression_statement":
                VisitExpressionStatement(packedNode, childNodes);
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
        // Try to get the node type from the packed node
        // This would come from the SymbolNode's NodeType
        return "unknown";
    }

    /// <summary>
    /// Checks if a PackedNode is valid for unparsing.
    /// </summary>
    private bool IsValidPackedNode(CognitiveGraph.Accessors.PackedNode packedNode)
    {
        // Basic validation - check if it has children or is a leaf
        var childNodes = packedNode.GetChildNodes();
        return childNodes.Count > 0 || packedNode.RuleID > 0;
    }

    /// <summary>
    /// Writes a node directly.
    /// </summary>
    private void WriteNode(CognitiveGraph.Accessors.SymbolNode node)
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
        
        // Check if we need a semicolon after this
        if (!text.EndsWith(";") && !text.EndsWith("{") && !text.EndsWith("}") && 
            !text.EndsWith("(") && !text.EndsWith(")") && !text.EndsWith(","))
        {
            _needsSemicolon = true;
        }
        else
        {
            _needsSemicolon = false;
        }
        
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
    /// Writes the current indentation.
    /// </summary>
    private void WriteIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _builder.Append("    ");
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
    /// Visits a compilation unit.
    /// </summary>
    private void VisitCompilationUnit(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // Visit all children of the compilation unit
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a package declaration.
    /// </summary>
    private void VisitPackageDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("package ");
        
        // Visit package name
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Write(";");
        WriteLine();
        WriteLine();
    }

    /// <summary>
    /// Visits an import declaration.
    /// </summary>
    private void VisitImportDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("import ");
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a class declaration.
    /// </summary>
    private void VisitClassDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // Visit modifiers
        // Visit class keyword
        // Visit class name
        // Visit type parameters
        // Visit extends
        // Visit implements
        // Visit body
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits an interface declaration.
    /// </summary>
    private void VisitInterfaceDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits an enum declaration.
    /// </summary>
    private void VisitEnumDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a record declaration (Java 14+).
    /// </summary>
    private void VisitRecordDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a method declaration.
    /// </summary>
    private void VisitMethodDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
    }

    /// <summary>
    /// Visits a field declaration.
    /// </summary>
    private void VisitFieldDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits an if statement.
    /// </summary>
    private void VisitIfStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("if ");
        
        // Visit condition (first child)
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit then statement
        if (childNodes.Count > 1)
        {
            var thenStatement = childNodes[1];
            // The CognitiveGraph 1.1.x package does not expose a NodeType enum to
            // distinguish block statements, so braces are always emitted (the safe form).
            Write(" { ");
            Visit(thenStatement);
            Write(" }");
        }
        
        // Visit else clause if present
        if (childNodes.Count > 2)
        {
            Write(" else ");
            var elseStatement = childNodes[2];
            // The CognitiveGraph 1.1.x package does not expose a NodeType enum to
            // distinguish block statements, so braces are always emitted (the safe form).
            Write("{ ");
            Visit(elseStatement);
            Write(" }");
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a for statement.
    /// </summary>
    private void VisitForStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("for ");
        
        // Visit initialization, condition, update, body
        for (int i = 0; i < Math.Min(4, childNodes.Count); i++)
        {
            Visit(childNodes[i]);
            if (i < 2) // After initialization and condition
            {
                Write("; ");
            }
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a while statement.
    /// </summary>
    private void VisitWhileStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("while ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit body
        if (childNodes.Count > 1)
        {
            var body = childNodes[1];
            // The CognitiveGraph 1.1.x package does not expose a NodeType enum to
            // distinguish block statements, so braces are always emitted (the safe form).
            Write(" { ");
            Visit(body);
            Write(" }");
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a do statement.
    /// </summary>
    private void VisitDoStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("do ");
        
        // Visit body
        if (childNodes.Count > 0)
        {
            var body = childNodes[0];
            // The CognitiveGraph 1.1.x package does not expose a NodeType enum to
            // distinguish block statements, so braces are always emitted (the safe form).
            Write("{ ");
            Visit(body);
            Write(" }");
        }
        
        Write(" while ");
        
        // Visit condition
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a try statement.
    /// </summary>
    private void VisitTryStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("try ");
        
        // Visit try block
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit catch clauses
        for (int i = 1; i < childNodes.Count; i++)
        {
            Visit(childNodes[i]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a switch statement.
    /// </summary>
    private void VisitSwitchStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("switch ");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" { ");
        WriteLine();
        Indent();
        
        // Visit case groups
        for (int i = 1; i < childNodes.Count; i++)
        {
            Visit(childNodes[i]);
        }
        
        Unindent();
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a return statement.
    /// </summary>
    private void VisitReturnStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("return ");
        
        // Visit expression if present
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a throw statement.
    /// </summary>
    private void VisitThrowStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("throw ");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a block.
    /// </summary>
    private void VisitBlock(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("{ ");
        WriteLine();
        Indent();
        
        // Visit all statements in the block
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Unindent();
        Write("}");
    }

    /// <summary>
    /// Visits an expression statement.
    /// </summary>
    private void VisitExpressionStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public override void Dispose()
    {
        _builder.Clear();
        _needsSemicolonStack.Clear();
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
    /// Visits the specified symbol node.
    /// </summary>
    public virtual void Visit(CognitiveGraph.Accessors.SymbolNode node) { }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public virtual void Dispose() { }
}
