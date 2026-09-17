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

using Minotaur.Core;
using Minotaur.Visitors;

namespace Minotaur.Plugins.Java;

/// <summary>
/// Validator for Java unparsing operations.
/// Validates that a cognitive graph can be successfully unparsed to Java code.
/// </summary>
public class JavaUnparseValidator : CognitiveGraphVisitorBase
{
    private readonly List<Minotaur.Plugins.UnparseValidationError> _errors = new();
    private readonly Stack<string> _contextStack = new();
    private int _braceDepth = 0;
    private int _parenDepth = 0;
    private int _bracketDepth = 0;

    /// <summary>
    /// Validates that a cognitive graph can be unparsed to valid Java code.
    /// </summary>
    /// <param name="graph">The cognitive graph to validate.</param>
    /// <returns>List of validation errors, if any.</returns>
    public List<Minotaur.Plugins.UnparseValidationError> Validate(CognitiveGraphNode graph)
    {
        _errors.Clear();
        _contextStack.Clear();
        _braceDepth = 0;
        _parenDepth = 0;
        _bracketDepth = 0;

        if (graph == null)
        {
            _errors.Add(new Minotaur.Plugins.UnparseValidationError
            {
                Message = "Cannot validate null graph",
                NodeId = "null",
                NodeType = "null",
                Severity = "Error"
            });
            return _errors;
        }

        Visit(graph);

        if (_braceDepth != 0)
        {
            _errors.Add(new Minotaur.Plugins.UnparseValidationError
            {
                Message = $"Unbalanced braces: {_braceDepth} unclosed braces",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = "Error"
            });
        }

        if (_parenDepth != 0)
        {
            _errors.Add(new Minotaur.Plugins.UnparseValidationError
            {
                Message = $"Unbalanced parentheses: {_parenDepth} unclosed parentheses",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = "Error"
            });
        }

        if (_bracketDepth != 0)
        {
            _errors.Add(new Minotaur.Plugins.UnparseValidationError
            {
                Message = $"Unbalanced brackets: {_bracketDepth} unclosed brackets",
                NodeId = "root",
                NodeType = "compilation_unit",
                Severity = "Error"
            });
        }

        return _errors;
    }

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
            case "class_body":
            case "interface_body":
            case "enum_body":
            case "record_body":
            case "method_body":
            case "constructor_body":
            case "block":
            case "switch_block":
            case "try_block":
            case "catch_block":
            case "finally_block":
            case "static_initializer":
            case "array_initializer":
            case "annotation_body":
                _braceDepth++;
                _contextStack.Push(ruleName);
                break;

            case "formal_parameters":
            case "argument_list":
            case "type_parameters":
            case "type_arguments":
            case "resource_list":
                _parenDepth++;
                _contextStack.Push(ruleName);
                break;

            case "array_access":
            case "array_creation":
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
            case "record_body":
            case "method_body":
            case "constructor_body":
            case "block":
            case "switch_block":
            case "try_block":
            case "catch_block":
            case "finally_block":
            case "static_initializer":
            case "array_initializer":
            case "annotation_body":
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
            case "resource_list":
                _parenDepth--;
                if (_contextStack.Count > 0 && _contextStack.Peek() == ruleName)
                {
                    _contextStack.Pop();
                }
                break;

            case "array_access":
            case "array_creation":
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
        var text = node.Text;

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
