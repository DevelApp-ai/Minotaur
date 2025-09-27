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
using Minotaur.Visitors;
using System.Text;

namespace Minotaur.Plugins;

/// <summary>
/// Visitor for generating C# source code from cognitive graph
/// </summary>
public class CSharpUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _code = new();
    private int _indentLevel = 0;

    /// <summary>
    /// Gets the generated C# source code.
    /// </summary>
    /// <returns>The generated source code as a string.</returns>
    public string GetGeneratedCode() => _code.ToString();

    /// <summary>
    /// Visits a cognitive graph node before traversing its children and generates appropriate C# code.
    /// </summary>
    /// <param name="node">The cognitive graph node to visit and process.</param>
    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                VisitIdentifierNode(identifier);
                break;
            case LiteralNode literal:
                VisitLiteralNode(literal);
                break;
            case TerminalNode terminal:
                VisitTerminalNode(terminal);
                break;
            case NonTerminalNode nonTerminal:
                VisitNonTerminalNode(nonTerminal);
                break;
        }
    }

    private void VisitTerminalNode(TerminalNode node)
    {
        AppendIndented(node.Text);

        if (node.TokenType == "keyword" && IsStatementKeyword(node.Text))
        {
            _code.Append(" ");
        }
    }

    private void VisitNonTerminalNode(NonTerminalNode node)
    {
        switch (node.RuleName.ToLowerInvariant())
        {
            case "namespace_declaration":
                AppendIndented("namespace ");
                break;
            case "class_declaration":
                AppendIndented("public class ");
                break;
            case "method_declaration":
                AppendIndented("public void Method()");
                AppendLine();
                AppendIndented("{");
                _indentLevel++;
                break;
        }
    }

    private void VisitIdentifierNode(IdentifierNode node)
    {
        _code.Append(node.Text);
    }

    private void VisitLiteralNode(LiteralNode node)
    {
        if (node.Value is string stringValue)
        {
            _code.Append($"\"{stringValue}\"");
        }
        else
        {
            _code.Append(node.Value?.ToString() ?? "null");
        }
    }

    private void AppendIndented(string text)
    {
        _code.Append(new string(' ', _indentLevel * 4));
        _code.Append(text);
    }

    private void AppendLine()
    {
        _code.AppendLine();
    }

    private bool IsStatementKeyword(string keyword)
    {
        var statementKeywords = new[] { "using", "namespace", "class", "public", "private", "static" };
        return statementKeywords.Contains(keyword.ToLowerInvariant());
    }
}

/// <summary>
/// Visitor for generating JavaScript source code from cognitive graph
/// </summary>
public class JavaScriptUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _code = new();
    private int _indentLevel = 0;

    /// <summary>
    /// Gets the generated JavaScript source code.
    /// </summary>
    /// <returns>The generated source code as a string.</returns>
    public string GetGeneratedCode() => _code.ToString();

    /// <summary>
    /// Visits a cognitive graph node before traversing its children and generates appropriate JavaScript code.
    /// </summary>
    /// <param name="node">The cognitive graph node to visit and process.</param>
    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                _code.Append(identifier.Text);
                break;
            case LiteralNode literal:
                VisitLiteralNode(literal);
                break;
            case TerminalNode terminal:
                AppendIndented(terminal.Text);
                break;
            case NonTerminalNode nonTerminal:
                VisitNonTerminalNode(nonTerminal);
                break;
        }
    }

    private void VisitNonTerminalNode(NonTerminalNode node)
    {
        switch (node.RuleName.ToLowerInvariant())
        {
            case "function_declaration":
                AppendIndented("function ");
                break;
            case "class_declaration":
                AppendIndented("class ");
                break;
        }
    }

    private void VisitLiteralNode(LiteralNode node)
    {
        if (node.Value is string stringValue)
        {
            _code.Append($"'{stringValue}'");
        }
        else
        {
            _code.Append(node.Value?.ToString() ?? "null");
        }
    }

    private void AppendIndented(string text)
    {
        _code.Append(new string(' ', _indentLevel * 2));
        _code.Append(text);
    }
}

/// <summary>
/// Visitor for generating Python source code from cognitive graph
/// </summary>
public class PythonUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _code = new();
    private int _indentLevel = 0;

    /// <summary>
    /// Gets the generated Python source code.
    /// </summary>
    /// <returns>The generated source code as a string.</returns>
    public string GetGeneratedCode() => _code.ToString();

    /// <summary>
    /// Visits a cognitive graph node before traversing its children and generates appropriate Python code.
    /// </summary>
    /// <param name="node">The cognitive graph node to visit and process.</param>
    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                _code.Append(identifier.Text);
                break;
            case LiteralNode literal:
                VisitLiteralNode(literal);
                break;
            case TerminalNode terminal:
                AppendIndented(terminal.Text);
                break;
            case NonTerminalNode nonTerminal:
                VisitNonTerminalNode(nonTerminal);
                break;
        }
    }

    private void VisitNonTerminalNode(NonTerminalNode node)
    {
        switch (node.RuleName.ToLowerInvariant())
        {
            case "function_def":
                AppendIndented("def ");
                break;
            case "class_def":
                AppendIndented("class ");
                break;
        }
    }

    private void VisitLiteralNode(LiteralNode node)
    {
        if (node.Value is string stringValue)
        {
            _code.Append($"'{stringValue}'");
        }
        else
        {
            _code.Append(node.Value?.ToString() ?? "None");
        }
    }

    private void AppendIndented(string text)
    {
        _code.Append(new string(' ', _indentLevel * 4));
        _code.Append(text);
    }
}