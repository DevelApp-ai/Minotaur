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
using System.Text;

namespace Minotaur.Plugins.TypeScript;

/// <summary>
/// Visitor for generating TypeScript source code from cognitive graph.
/// Extends JavaScript with TypeScript-specific features.
/// </summary>
public class TypeScriptUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _code = new();
    private int _indentLevel = 0;
    private bool _needsNewline = false;
    private readonly Stack<string> _contextStack = new();
    private bool _inInterface = false;
    private bool _inTypeAlias = false;
    private bool _inClass = false;
    private bool _inMethod = false;

    /// <summary>
    /// Gets the generated TypeScript source code.
    /// </summary>
    /// <returns>The generated source code as a string.</returns>
    public string GetGeneratedCode() => _code.ToString().Trim();

    /// <summary>
    /// Visits a cognitive graph node before traversing its children.
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

    /// <summary>
    /// Visits a cognitive graph node after traversing its children.
    /// </summary>
    /// <param name="node">The cognitive graph node that was visited.</param>
    protected override void AfterVisitNode(CognitiveGraphNode node)
    {
        if (node is NonTerminalNode nonTerminal)
        {
            switch (nonTerminal.RuleName.ToLowerInvariant())
            {
                case "interface_declaration":
                case "interface_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inInterface = false;
                    _contextStack.Pop();
                    break;
                    
                case "type_alias_declaration":
                    Append(";\n");
                    _inTypeAlias = false;
                    _contextStack.Pop();
                    break;
                    
                case "class_declaration":
                case "class_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inClass = false;
                    _contextStack.Pop();
                    break;
                    
                case "enum_declaration":
                case "enum_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "method_declaration":
                case "method_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inMethod = false;
                    _contextStack.Pop();
                    break;
                    
                case "constructor_declaration":
                case "constructor_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inMethod = false;
                    _contextStack.Pop();
                    break;
                    
                case "block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "type_parameters":
                    Append(">");
                    _contextStack.Pop();
                    break;
                    
                case "type_arguments":
                    Append(">");
                    _contextStack.Pop();
                    break;
                    
                case "formal_parameters":
                    Append(")");
                    _contextStack.Pop();
                    break;
                    
                case "argument_list":
                    Append(")");
                    _contextStack.Pop();
                    break;
                    
                case "switch_block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "try_block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "catch_block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "finally_block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "array_initializer":
                    Append("]");
                    _contextStack.Pop();
                    break;
            }
        }
    }

    private void VisitTerminalNode(TerminalNode node)
    {
        var text = node.Text;
        
        if (IsTypeScriptKeyword(text))
        {
            if (_needsNewline && !IsControlKeyword(text))
            {
                Append(" ");
            }
            Append(text);
            
            if (NeedsSpaceAfter(text))
            {
                Append(" ");
            }
            
            if (text == "interface") _inInterface = true;
            if (text == "type") _inTypeAlias = true;
            if (text == "class") _inClass = true;
            
            return;
        }
        
        if (IsTypeScriptOperator(text))
        {
            if (!string.IsNullOrEmpty(_code.ToString()) && _code.ToString().Last() != ' ')
            {
                Append(" ");
            }
            Append(text);
            Append(" ");
            return;
        }
        
        if (IsSeparator(text))
        {
            Append(text);
            
            if (text == ",")
            {
                Append(" ");
            }
            return;
        }
        
        Append(text);
    }

    private void VisitNonTerminalNode(NonTerminalNode node)
    {
        var ruleName = node.RuleName.ToLowerInvariant();
        
        switch (ruleName)
        {
            case "import_declaration":
                Append("import ");
                _contextStack.Push("import");
                break;
                
            case "import_all_declaration":
                Append("import * as ");
                _contextStack.Push("import_all");
                break;
                
            case "export_declaration":
                Append("export ");
                _contextStack.Push("export");
                break;
                
            case "export_default":
                Append("export default ");
                _contextStack.Push("export_default");
                break;
                
            case "interface_declaration":
                AppendModifiers(node);
                Append("interface ");
                _contextStack.Push("interface");
                _inInterface = true;
                break;
                
            case "interface_header":
                break;
                
            case "interface_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("interface_body");
                break;
                
            case "type_alias_declaration":
                AppendModifiers(node);
                Append("type ");
                _contextStack.Push("type_alias");
                _inTypeAlias = true;
                break;
                
            case "type_annotation":
                Append(": ");
                _contextStack.Push("type_annotation");
                break;
                
            case "class_declaration":
                AppendModifiers(node);
                Append("class ");
                _contextStack.Push("class");
                _inClass = true;
                break;
                
            case "class_header":
                break;
                
            case "class_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("class_body");
                break;
                
            case "enum_declaration":
                AppendModifiers(node);
                Append("enum ");
                _contextStack.Push("enum");
                break;
                
            case "enum_header":
                break;
                
            case "enum_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("enum_body");
                break;
                
            case "method_declaration":
                AppendModifiers(node);
                _contextStack.Push("method");
                _inMethod = true;
                break;
                
            case "method_header":
                break;
                
            case "method_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("method_body");
                break;
                
            case "constructor_declaration":
                AppendModifiers(node);
                _contextStack.Push("constructor");
                _inMethod = true;
                break;
                
            case "constructor_header":
                break;
                
            case "constructor_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("constructor_body");
                break;
                
            case "property_declaration":
                AppendModifiers(node);
                _contextStack.Push("property");
                break;
                
            case "field_declaration":
                AppendModifiers(node);
                _contextStack.Push("field");
                break;
                
            case "local_variable_declaration":
                AppendModifiers(node);
                _contextStack.Push("local_variable");
                break;
                
            case "modifiers":
                AppendModifiers(node);
                break;
                
            case "type_parameters":
                Append("<");
                _contextStack.Push("type_parameters");
                break;
                
            case "type_parameter":
                break;
                
            case "type_arguments":
                Append("<");
                _contextStack.Push("type_arguments");
                break;
                
            case "formal_parameters":
                Append("(");
                _contextStack.Push("formal_parameters");
                break;
                
            case "formal_parameter":
                break;
                
            case "argument_list":
                Append("(");
                _contextStack.Push("argument_list");
                break;
                
            case "decorator":
                Append("@");
                _contextStack.Push("decorator");
                break;
                
            case "decorator_expression":
                break;
                
            case "arrow_function":
                _contextStack.Push("arrow_function");
                break;
                
            case "arrow_parameters":
                Append("(");
                _contextStack.Push("arrow_parameters");
                break;
                
            case "arrow_body":
                Append(") => ");
                _contextStack.Push("arrow_body");
                break;
                
            case "function_expression":
                Append("function ");
                _contextStack.Push("function_expression");
                break;
                
            case "function_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("function_body");
                break;
                
            case "optional_parameter":
                break;
                
            case "rest_parameter":
                Append("...");
                break;
                
            case "extends_clause":
                Append(" extends ");
                break;
                
            case "implements_clause":
                Append(" implements ");
                break;
                
            case "return_type":
                Append(": ");
                break;
                
            case "type_guard":
                Append(" is ");
                break;
                
            case "as_expression":
                Append(" as ");
                break;
                
            case "non_null_expression":
                Append("!");
                break;
                
            case "optional_chaining":
                Append("?.");
                break;
                
            case "nullish_coalescing":
                Append("??");
                break;
                
            case "template_string":
                Append("`");
                _contextStack.Push("template_string");
                break;
                
            case "template_head":
                Append("`");
                break;
                
            case "template_middle":
                Append("${");
                break;
                
            case "template_tail":
                Append("}`");
                break;
                
            case "jsx_element":
                Append("<");
                _contextStack.Push("jsx_element");
                break;
                
            case "jsx_opening_element":
                break;
                
            case "jsx_closing_element":
                Append("</");
                break;
                
            case "jsx_self_closing_element":
                Append("/>");
                break;
                
            case "jsx_attribute":
                break;
                
            case "jsx_expression":
                Append("{");
                _contextStack.Push("jsx_expression");
                break;
                
            case "jsx_children":
                break;
                
            case "block":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("block");
                break;
                
            case "if_statement":
                Append("if (");
                _contextStack.Push("if");
                break;
                
            case "then_statement":
                Append(") ");
                _contextStack.Push("then");
                break;
                
            case "else_statement":
                Append(" else ");
                _contextStack.Push("else");
                break;
                
            case "switch_statement":
                Append("switch (");
                _contextStack.Push("switch");
                break;
                
            case "switch_block":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("switch_block");
                break;
                
            case "case_clause":
                AppendIndented("case ");
                _contextStack.Push("case");
                break;
                
            case "default_clause":
                AppendIndented("default:");
                _contextStack.Push("default");
                break;
                
            case "for_statement":
                Append("for (");
                _contextStack.Push("for");
                break;
                
            case "for_in_statement":
                Append("for (");
                _contextStack.Push("for_in");
                break;
                
            case "for_of_statement":
                Append("for (");
                _contextStack.Push("for_of");
                break;
                
            case "while_statement":
                Append("while (");
                _contextStack.Push("while");
                break;
                
            case "do_while_statement":
                Append("do ");
                _contextStack.Push("do_while");
                break;
                
            case "try_statement":
                Append("try ");
                _contextStack.Push("try");
                break;
                
            case "try_block":
                Append("{\n");
                _indentLevel++;
                _contextStack.Push("try_block");
                break;
                
            case "catch_clause":
                _indentLevel--;
                AppendIndented(" } catch (");
                _contextStack.Push("catch");
                break;
                
            case "catch_block":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("catch_block");
                break;
                
            case "finally_clause":
                _indentLevel--;
                AppendIndented(" } finally {\n");
                _indentLevel++;
                _contextStack.Push("finally");
                break;
                
            case "throw_statement":
                AppendIndented("throw ");
                _contextStack.Push("throw");
                break;
                
            case "return_statement":
                AppendIndented("return");
                _contextStack.Push("return");
                break;
                
            case "break_statement":
                AppendIndented("break");
                break;
                
            case "continue_statement":
                AppendIndented("continue");
                break;
                
            case "empty_statement":
                Append(";\n");
                break;
                
            case "expression_statement":
                Append(";\n");
                break;
                
            case "labeled_statement":
                break;
                
            case "array_initializer":
                Append("[");
                _contextStack.Push("array_initializer");
                break;
                
            default:
                break;
        }
    }

    private void VisitIdentifierNode(IdentifierNode node)
    {
        Append(node.Text);
    }

    private void VisitLiteralNode(LiteralNode node)
    {
        if (node.Value == null)
        {
            Append("null");
            return;
        }
        
        switch (node.Value)
        {
            case string stringValue:
                var escaped = stringValue
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("`", "\\`")
                    .Replace("$", "\\$");
                Append($"\"{escaped}\"");
                break;
                
            case int intValue:
                Append(intValue.ToString());
                break;
                
            case long longValue:
                Append(longValue.ToString() + "n");
                break;
                
            case float floatValue:
                Append(floatValue.ToString());
                break;
                
            case double doubleValue:
                Append(doubleValue.ToString());
                break;
                
            case bool boolValue:
                Append(boolValue ? "true" : "false");
                break;
                
            case char charValue:
                Append($"'{EscapeChar(charValue)}'");
                break;
                
            default:
                Append(node.Value.ToString());
                break;
        }
    }

    private void AppendModifiers(NonTerminalNode node)
    {
        foreach (var child in node.Children)
        {
            if (child is TerminalNode terminal && IsTypeScriptModifier(terminal.Text))
            {
                Append(terminal.Text);
                Append(" ");
            }
        }
    }

    private void Append(string text)
    {
        _code.Append(text);
        _needsNewline = false;
    }

    private void AppendIndented(string text)
    {
        Append(new string(' ', _indentLevel * 2));
        Append(text);
    }

    private void AppendIndentedLine(string text)
    {
        AppendIndented(text);
        AppendLine();
    }

    private void AppendLine()
    {
        Append("\n");
        _needsNewline = false;
    }

    private void AppendLine(string text)
    {
        Append(text);
        AppendLine();
    }

    private bool IsTypeScriptKeyword(string text)
    {
        var keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default",
            "delete", "do", "else", "enum", "export", "extends", "false", "finally",
            "for", "function", "if", "import", "in", "instanceof", "new", "null",
            "return", "super", "switch", "this", "throw", "true", "try", "typeof",
            "var", "void", "while", "with", "as", "implements", "interface",
            "let", "package", "private", "protected", "public", "static", "yield",
            "any", "boolean", "constructor", "declare", "get", "module", "require",
            "number", "set", "string", "symbol", "type", "from", "of", "namespace",
            "async", "await"
        };
        
        return keywords.Contains(text);
    }

    private bool IsControlKeyword(string text)
    {
        var controlKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "try", "catch", "finally", "return", "throw", "break", "continue"
        };
        
        return controlKeywords.Contains(text);
    }

    private bool NeedsSpaceAfter(string text)
    {
        var keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "class", "interface", "type", "enum", "function", "const", "let", "var",
            "import", "export", "extends", "implements", "from", "as", "in",
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "try", "catch", "finally", "return", "throw", "new", "delete",
            "typeof", "instanceof", "void", "async", "await", "yield"
        };
        
        return keywords.Contains(text);
    }

    private bool IsTypeScriptOperator(string text)
    {
        var operators = new HashSet<string>
        {
            "+", "-", "*", "/", "%", "=", "+=", "-=", "*=", "/=", "%=",
            "==", "!=", ">", "<", ">=", "<=", "===", "!==", "&&", "||", "!",
            "&", "|", "^", "~", "<<", ">>", ">>>", "<<<", ">>>", "->", "...",
            ".", "?", ":", "??", "?.", "!", "as", "/**", "*/", "//"
        };
        
        return operators.Contains(text);
    }

    private bool IsSeparator(string text)
    {
        return text == "(" || text == ")" || text == "{" || text == "}" || 
               text == "[" || text == "]" || text == ";" || text == "," || 
               text == "." || text == ":" || text == "?" || text == "`";
    }

    private bool IsTypeScriptModifier(string text)
    {
        var modifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "public", "private", "protected", "static", "readonly", "abstract",
            "const", "let", "var", "export", "default", "async"
        };
        
        return modifiers.Contains(text);
    }

    private string EscapeChar(char c)
    {
        switch (c)
        {
            case '\\': return "\\\\";
            case '"': return "\\\"";
            case '\': return "\\'";
            case '\n': return "\\n";
            case '\r': return "\\r";
            case '\t': return "\\t";
            case '`': return "\\`";
            case '$': return "\\$";
            default: return c.ToString();
        }
    }
}
