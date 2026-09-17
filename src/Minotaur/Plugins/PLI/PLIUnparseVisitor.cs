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

namespace Minotaur.Plugins.PLI;

/// <summary>
/// Visitor for unparsing PL/I code from a CognitiveGraph.
/// Handles PL/I-specific syntax including procedures, data structures, and control flow.
/// </summary>
public class PLIUnparseVisitor : UnparseVisitorBase
{
    private readonly System.Text.StringBuilder _builder = new();
    private int _indentLevel = 0;
    private bool _atLineStart = true;
    private bool _needsSemicolon = false;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PLIUnparseVisitor()
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
        var code = _builder.ToString();
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
            WriteNode(node);
            return;
        }
        
        // For PL/I, select the first valid PackedNode
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
            case "program_declaration":
                VisitProgramDeclaration(packedNode, childNodes);
                break;
            case "data_declaration":
                VisitDataDeclaration(packedNode, childNodes);
                break;
            case "structure_declaration":
                VisitStructureDeclaration(packedNode, childNodes);
                break;
            case "array_declaration":
                VisitArrayDeclaration(packedNode, childNodes);
                break;
            case "file_declaration":
                VisitFileDeclaration(packedNode, childNodes);
                break;
            case "procedure_declaration":
                VisitProcedureDeclaration(packedNode, childNodes);
                break;
            case "if_statement":
                VisitIfStatement(packedNode, childNodes);
                break;
            case "if_else_statement":
                VisitIfElseStatement(packedNode, childNodes);
                break;
            case "do_group":
                VisitDoGroup(packedNode, childNodes);
                break;
            case "do_while":
                VisitDoWhile(packedNode, childNodes);
                break;
            case "do_for":
                VisitDoFor(packedNode, childNodes);
                break;
            case "select_statement":
                VisitSelectStatement(packedNode, childNodes);
                break;
            case "when_clause":
                VisitWhenClause(packedNode, childNodes);
                break;
            case "call_statement":
                VisitCallStatement(packedNode, childNodes);
                break;
            case "return_statement":
                VisitReturnStatement(packedNode, childNodes);
                break;
            case "goto_statement":
                VisitGotoStatement(packedNode, childNodes);
                break;
            case "stop_statement":
                VisitStopStatement(packedNode, childNodes);
                break;
            case "exception_declaration":
                VisitExceptionDeclaration(packedNode, childNodes);
                break;
            case "task_declaration":
                VisitTaskDeclaration(packedNode, childNodes);
                break;
            case "signal_statement":
                VisitSignalStatement(packedNode, childNodes);
                break;
            case "wait_statement":
                VisitWaitStatement(packedNode, childNodes);
                break;
            case "put_statement":
                VisitPutStatement(packedNode, childNodes);
                break;
            case "get_statement":
                VisitGetStatement(packedNode, childNodes);
                break;
            case "comment":
                VisitComment(packedNode, childNodes);
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
    /// Visits a program declaration.
    /// </summary>
    private void VisitProgramDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // First child is program name
        if (childNodes.Count > 0)
        {
            var nameNode = childNodes[0];
            var name = nameNode.GetSourceText().ToString();
            Write($"{name}: PROC OPTIONS(MAIN);");
            WriteLine();
            
            // Visit remaining children (procedures, data, etc.)
            for (int i = 1; i < childNodes.Count; i++)
            {
                Visit(childNodes[i]);
            }
            
            Write("END ");
            Write(name);
            Write(";");
            WriteLine();
        }
    }

    /// <summary>
    /// Visits a data declaration.
    /// </summary>
    private void VisitDataDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DCL ");
        
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
        
        // Visit initialization if present
        if (childNodes.Count > 2)
        {
            Write(" ");
            Visit(childNodes[2]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a structure declaration.
    /// </summary>
    private void VisitStructureDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DCL 1 ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(",\n");
        Indent();
        
        // Visit members
        for (int i = 1; i < childNodes.Count; i++)
        {
            Write("  2 ");
            Visit(childNodes[i]);
            Write(",\n");
        }
        
        Unindent();
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits an array declaration.
    /// </summary>
    private void VisitArrayDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DCL ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(");
        
        // Visit dimensions
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(") ");
        
        // Visit type
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a file declaration.
    /// </summary>
    private void VisitFileDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DCL ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" FILE;");
        WriteLine();
    }

    /// <summary>
    /// Visits a procedure declaration.
    /// </summary>
    private void VisitProcedureDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        // First child is procedure name
        if (childNodes.Count > 0)
        {
            var nameNode = childNodes[0];
            var name = nameNode.GetSourceText().ToString();
            Write($"{name}: PROC(");
            
            // Visit parameters
            if (childNodes.Count > 1)
            {
                Visit(childNodes[1]);
            }
            
            Write(");");
            WriteLine();
            Indent();
            
            // Visit body
            if (childNodes.Count > 2)
            {
                Visit(childNodes[2]);
            }
            
            Unindent();
            Write("END ");
            Write(name);
            Write(";");
            WriteLine();
        }
    }

    /// <summary>
    /// Visits an if statement.
    /// </summary>
    private void VisitIfStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("IF ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" THEN;");
        WriteLine();
        Indent();
        
        // Visit then statements
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits an if-else statement.
    /// </summary>
    private void VisitIfElseStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("IF ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" THEN;");
        WriteLine();
        Indent();
        
        // Visit then statements
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Unindent();
        Write("ELSE;");
        WriteLine();
        Indent();
        
        // Visit else statements
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits a do group.
    /// </summary>
    private void VisitDoGroup(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DO;");
        WriteLine();
        Indent();
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits a do while statement.
    /// </summary>
    private void VisitDoWhile(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DO WHILE(");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(");");
        WriteLine();
        Indent();
        
        // Visit statements
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits a do for statement.
    /// </summary>
    private void VisitDoFor(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("DO ");
        
        // Visit index
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" = ");
        
        // Visit start
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(" TO ");
        
        // Visit end
        if (childNodes.Count > 2)
        {
            Visit(childNodes[2]);
        }
        
        // Check for BY clause
        if (childNodes.Count > 3)
        {
            Write(" BY ");
            Visit(childNodes[3]);
        }
        
        Write(";");
        WriteLine();
        Indent();
        
        // Visit statements
        if (childNodes.Count > 4)
        {
            Visit(childNodes[4]);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits a select statement (switch).
    /// </summary>
    private void VisitSelectStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("SELECT(");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(");");
        WriteLine();
        Indent();
        
        // Visit when clauses
        for (int i = 1; i < childNodes.Count; i++)
        {
            Visit(childNodes[i]);
        }
        
        Unindent();
        Write("END;");
        WriteLine();
    }

    /// <summary>
    /// Visits a when clause.
    /// </summary>
    private void VisitWhenClause(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("WHEN(");
        
        // Visit value
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(") ");
        
        // Visit statements
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        WriteLine();
    }

    /// <summary>
    /// Visits a call statement.
    /// </summary>
    private void VisitCallStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("CALL ");
        
        // Visit name
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(");
        
        // Visit arguments
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits a return statement.
    /// </summary>
    private void VisitReturnStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("RETURN(");
        
        // Visit expression
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits a go to statement.
    /// </summary>
    private void VisitGotoStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("GO TO ");
        
        // Visit label
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a stop statement.
    /// </summary>
    private void VisitStopStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("STOP;");
        WriteLine();
    }

    /// <summary>
    /// Visits an exception declaration.
    /// </summary>
    private void VisitExceptionDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("ON ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(" ");
        
        // Visit action
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a task declaration.
    /// </summary>
    private void VisitTaskDeclaration(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        if (childNodes.Count > 0)
        {
            var nameNode = childNodes[0];
            var name = nameNode.GetSourceText().ToString();
            Write($"{name}: TASK;");
            WriteLine();
        }
    }

    /// <summary>
    /// Visits a signal statement.
    /// </summary>
    private void VisitSignalStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("SIGNAL ");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(";");
        WriteLine();
    }

    /// <summary>
    /// Visits a wait statement.
    /// </summary>
    private void VisitWaitStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("WAIT(");
        
        // Visit condition
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits a put statement.
    /// </summary>
    private void VisitPutStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("PUT ");
        
        // Visit destination
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(");
        
        // Visit data
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits a get statement.
    /// </summary>
    private void VisitGetStatement(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("GET ");
        
        // Visit source
        if (childNodes.Count > 0)
        {
            Visit(childNodes[0]);
        }
        
        Write("(");
        
        // Visit data
        if (childNodes.Count > 1)
        {
            Visit(childNodes[1]);
        }
        
        Write(");");
        WriteLine();
    }

    /// <summary>
    /// Visits a comment.
    /// </summary>
    private void VisitComment(CognitiveGraph.Accessors.PackedNode packedNode, CognitiveGraph.Accessors.SymbolNodeOffsetCollection childNodes)
    {
        Write("/* ");
        
        foreach (var child in childNodes)
        {
            Visit(child);
        }
        
        Write(" */");
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
    /// Visits the specified zero-copy symbol node.
    /// </summary>
    public virtual void Visit(CognitiveGraph.Accessors.SymbolNode node) { }

    /// <summary>
    /// Disposes the visitor.
    /// </summary>
    public virtual void Dispose() { }
}
