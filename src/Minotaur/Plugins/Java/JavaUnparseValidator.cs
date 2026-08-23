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

namespace Minotaur.Plugins.Java;

/// <summary>
/// Validator for Java unparsing operations.
/// Validates that a cognitive graph can be successfully unparsed to Java code.
/// </summary>
public class JavaUnparseValidator : CognitiveGraphVisitorBase
{
    private readonly List<UnparseValidationError> _errors = new();
    private readonly Stack<string> _contextStack = new();
    private int _braceDepth = 0;
    private int _parenDepth = 0;
    private int _bracketDepth = 0;

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid Java code.
    /// </summary>
    /// <param name="graph">The cognitive graph to validate.</param>
    /// <returns>List of validation errors, if any.</returns>
    public List<UnparseValidationError> Validate(CognitiveGraphNode graph)
    {
        _errors.Clear();
        _contextStack.Clear();
        _braceDepth = 0;
        _parenDepth = 0;
        _bracketDepth = 0;
        
        if (graph == null)
        {
            _errors.Add(new UnparseValidationError
            {
                Message = "Cannot validate null graph",
                NodeId = "null",
                NodeType = "null",
                Severity = ValidationErrorSeverity.Error
            });
            return _errors;
        }
        
        // Visit the graph to validate structure
        Visit(graph);
        
        // Check for unbalanced delimiters
        if (_braceDepth != 0)
        {
            _errors.Add(new UnparseValidationError
            {
                Message = $"Unbalanced braces: {_braceDepth} unclosed braces",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = ValidationErrorSeverity.Error
            });
        }
        
        if (_parenDepth != 0)
        {
            _errors.Add(new UnparseValidationError
            {
                Message = $"Unbalanced parentheses: {_parenDepth} unclosed parentheses",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = ValidationErrorSeverity.Error
            });
        }
        
        if (_bracketDepth != 0)
        {
            _errors.Add(new UnparseValidationError
            {
                Message = $"Unbalanced brackets: {_bracketDepth} unclosed brackets",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = ValidationErrorSeverity.Error
            });
        }
        
        return _errors;
    }

    /// <summary>
    /// Visits a node before its children.
    /// </summary>
    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case NonTerminalNode nonTerminal:
                ValidateNonTerminal(nonTerminal);
                break;
            case TerminalNode terminal:
                ValidateTerminal(terminal);
                break;
        }
    }

    /// <summary>
    /// Visits a node after its children.
    /// </summary>
    protected override void AfterVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case NonTerminalNode nonTerminal:
                ValidateNonTerminalAfter(nonTerminal);
                break;
        }
    }

    private void ValidateNonTerminal(NonTerminalNode node)
    {
        var ruleName = node.RuleName.ToLowerInvariant();
        
        switch (ruleName)
        {
            case "class_declaration":
            case "class_body":
            case "interface_declaration":
            case "interface_body":
            case "enum_declaration":
            case "enum_body":
            case "method_declaration":
            case "method_body":
            case "constructor_declaration":
            case "constructor_body":
            case "block":
            case "static_initializer":
            case "instance_initializer":
            case "switch_block":
            case "try_block":
            case "catch_block":
            case "finally_block":
            case "synchronized_block":
            case "array_initializer":
                _braceDepth++;
                _contextStack.Push(ruleName);
                break;
                
            case "formal_parameters":
            case "argument_list":
            case "type_parameters":
            case "type_arguments":
                _parenDepth++;
                _contextStack.Push(ruleName);
                break;
                
            case "dimensions":
            case "array_declarator":
                _bracketDepth++;
                _contextStack.Push(ruleName);
                break;
        }
    }

    private void ValidateNonTerminalAfter(NonTerminalNode node)
    {
        var ruleName = node.RuleName.ToLowerInvariant();
        
        switch (ruleName)
        {
            case "class_body":
            case "interface_body":
            case "enum_body":
            case "method_body":
            case "constructor_body":
            case "block":
            case "static_initializer":
            case "instance_initializer":
            case "switch_block":
            case "try_block":
            case "catch_block":
            case "finally_block":
            case "synchronized_block":
            case "array_initializer":
                _braceDepth--;
                if (_contextStack.Count > 0 && _contextStack.Peek() == ruleName)
                {
                    _contextStack.Pop();
                }
                break;
                
            case "formal_parameters":
            case "argument_list":
            case "type_parameters":
            case "type_arguments":
                _parenDepth--;
                if (_contextStack.Count > 0 && _contextStack.Peek() == ruleName)
                {
                    _contextStack.Pop();
                }
                break;
                
            case "dimensions":
            case "array_declarator":
                _bracketDepth--;
                if (_contextStack.Count > 0 && _contextStack.Peek() == ruleName)
                {
                    _contextStack.Pop();
                }
                break;
        }
    }

    private void ValidateTerminal(TerminalNode node)
    {
        // Validate terminal nodes
        var text = node.Text;
        
        // Check for balanced delimiters in terminals
        foreach (var c in text)
        {
            switch (c)
            {
                case '{':
                    _braceDepth++;
                    break;
                case '}':
                    _braceDepth--;
                    break;
                case '(':
                    _parenDepth++;
                    break;
                case ')':
                    _parenDepth--;
                    break;
                case '[':
                    _bracketDepth++;
                    break;
                case ']':
                    _bracketDepth--;
                    break;
            }
        }
    }
}
