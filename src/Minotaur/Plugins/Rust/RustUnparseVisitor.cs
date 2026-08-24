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

namespace Minotaur.Plugins.Rust;

/// <summary>
/// Visitor for unparsing Rust code from a CognitiveGraph.
/// Handles Rust-specific syntax including ownership, traits, and pattern matching.
/// </summary>
public class RustUnparseVisitor : UnparseVisitorBase
{
    private readonly System.Text.StringBuilder _builder = new();
    private int _indentLevel = 0;
    private bool _atLineStart = true;
    private bool _needsSemicolon = false;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public RustUnparseVisitor()
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
        var code = _builder.ToString().Trim();
        if (_needsSemicolon && !code.EndsWith(";"))
        {
            _builder.Append(";");
        }
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
        
        // For Rust, select the first valid PackedNode
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
            case "module_declaration":
                VisitModuleDeclaration(packedNode, childNodes);
                break;
            case "use_declaration":
                VisitUseDeclaration(packedNode, childNodes);
                break;
            case "struct_declaration":
                VisitStructDeclaration(packedNode, childNodes);
                break;
            case "enum_declaration":
                VisitEnumDeclaration(packedNode, childNodes);
                break;
            case "trait_declaration":
                VisitTraitDeclaration(packedNode, childNodes);
                break;
            case "impl_block":
                VisitImplBlock(packedNode, childNodes);
                break;
            case "function_declaration":
                VisitFunctionDeclaration(packedNode, childNodes);
                break;
            case "method_declaration":
                VisitMethodDeclaration(packedNode, childNodes);
                break;
            case "if_expression":
                VisitIfExpression(packedNode, childNodes);
                break;
            case "match_expression":
                VisitMatchExpression(packedNode, childNodes);
                break;
            case "loop_expression":
                VisitLoopExpression(packedNode, childNodes);
                break;
            case "while_loop":
                VisitWhileLoop(packedNode, childNodes);
                break;
            case "for_loop":
                VisitForLoop(packedNode, childNodes);
                break;
            case "let_declaration":
                VisitLetDeclaration(packedNode, childNodes);
                break;
            case "const_declaration":
                VisitConstDeclaration(packedNode, childNodes);
                break;
            case "static_declaration":
                VisitStaticDeclaration(packedNode, childNodes);
                break;
            case "return_expression":
                VisitReturnExpression(packedNode, childNodes);
                break;
            case "async_block":
                VisitAsyncBlock(packedNode, childNodes);
                break;
            case "unsafe_block":
                VisitUnsafeBlock(packedNode, childNodes);
                break;
            case "macro_invocation":
                VisitMacroInvocation(packedNode, childNodes);
                break;
            case "attribute":
                VisitAttribute(packedNode, childNodes);
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
        
        // Check if we need a semicolon
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
    /// Visits a module declaration.
    /// </summary>
    private void VisitModuleDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("mod ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a use declaration.
    /// </summary>
    private void VisitUseDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("use ");
        
        // Visit path
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Check for items
        if (childNodes.Count > 1)
        {
            Write("::{ ");
            Visit(childNodes[1]);
            Write(" }");
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a struct declaration.
    /// </summary>
    private void VisitStructDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("pub struct ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit fields
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits an enum declaration.
    /// </summary>
    private void VisitEnumDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("pub enum ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit variants
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a trait declaration.
    /// </summary>
    private void VisitTraitDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("pub trait ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit items
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits an impl block.
    /// </summary>
    private void VisitImplBlock(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("impl ");
        
        // Visit trait (optional)
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
            Write(" for ");
        }
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(" {");
        
        // Visit items
        if (childNodes.Count > 2)
        {
            WriteLine();
            Indent();
            Visit(childNodes[2]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a function declaration.
    /// </summary>
    private void VisitFunctionDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("pub fn ");
        
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
            Write("-> ");
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
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits a method declaration.
    /// </summary>
    private void VisitMethodDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("pub fn ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(&");
        
        // Visit self kind (mut/imm)
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write("self");
        
        // Visit parameters
        if (childNodes.Count > 2)
        {
            Write(", ");
            Visit(childNodes[2]);
        }
        
        Write(") ");
        
        // Visit return type
        if (childNodes.Count > 3)
        {
            Write("-> ");
            Visit(childNodes[3]);
        }
        
        Write(" {");
        
        // Visit body
        if (childNodes.Count > 4)
        {
            WriteLine();
            Indent();
            Visit(childNodes[4]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
        WriteLine();
    }

    /// <summary>
    /// Visits an if expression.
    /// </summary>
    private void VisitIfExpression(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("if ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit then block
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
            WriteIndent();
        }
        
        // Check for else
        if (childNodes.Count > 2)
        {
            Write(" } else {");
            WriteLine();
            Indent();
            Visit(childNodes[2]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a match expression.
    /// </summary>
    private void VisitMatchExpression(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("match ");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" {");
        
        // Visit arms
        if (childNodes.Count > 1)
        {
            WriteLine();
            Indent();
            Visit(childNodes[1]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a loop expression.
    /// </summary>
    private void VisitLoopExpression(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("loop ");
        Write("{");
        
        // Visit body
        if (childNodes.Count > 0)
        {
            WriteLine();
            Indent();
            Visit(childNodes[0]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a while loop.
    /// </summary>
    private void VisitWhileLoop(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("while ");
        
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
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a for loop.
    /// </summary>
    private void VisitForLoop(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("for ");
        
        // Visit pattern
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" in ");
        
        // Visit iterator
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
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a let declaration.
    /// </summary>
    private void VisitLetDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("let ");
        
        // Visit pattern
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit type annotation
        if (childNodes.Count > 1)
        {
            Write(": ");
            Visit(childNodes[1]);
        }
        
        // Visit expression
        if (childNodes.Count > 2)
        {
            Write(" = ");
            Visit(childNodes[2]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a const declaration.
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
            Write(": ");
            Visit(childNodes[1]);
        }
        
        // Visit expression
        if (childNodes.Count > 2)
        {
            Write(" = ");
            Visit(childNodes[2]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a static declaration.
    /// </summary>
    private void VisitStaticDeclaration(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("static ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit type
        if (childNodes.Count > 1)
        {
            Write(": ");
            Visit(childNodes[1]);
        }
        
        // Visit expression
        if (childNodes.Count > 2)
        {
            Write(" = ");
            Visit(childNodes[2]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a return expression.
    /// </summary>
    private void VisitReturnExpression(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("return ");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits an async block.
    /// </summary>
    private void VisitAsyncBlock(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("async ");
        Write("{");
        
        // Visit body
        if (childNodes.Count > 0)
        {
            WriteLine();
            Indent();
            Visit(childNodes[0]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits an unsafe block.
    /// </summary>
    private void VisitUnsafeBlock(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("unsafe ");
        Write("{");
        
        // Visit body
        if (childNodes.Count > 0)
        {
            WriteLine();
            Indent();
            Visit(childNodes[0]);
            Unindent();
            WriteIndent();
        }
        
        Write("}");
    }

    /// <summary>
    /// Visits a macro invocation.
    /// </summary>
    private void VisitMacroInvocation(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("!(");
        
        // Visit arguments
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits an attribute.
    /// </summary>
    private void VisitAttribute(CognitiveGraph.PackedNode packedNode, CognitiveGraph.SymbolNodeCollection childNodes)
    {
        Write("#[");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        // Visit arguments
        if (childNodes.Count > 1)
        {
            Write("(");
            Visit(childNodes[1]);
            Write(")");
        }
        
        Write("]");
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
        Write("/// ");
        
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
