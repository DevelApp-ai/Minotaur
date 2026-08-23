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

namespace Minotaur.Plugins.Java;

/// <summary>
/// Visitor for generating Java source code from cognitive graph.
/// Handles all Java-specific constructs including classes, interfaces, methods,
/// generics, lambda expressions, and more.
/// </summary>
public class JavaUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _code = new();
    private int _indentLevel = 0;
    private bool _needsNewline = false;
    private bool _inClass = false;
    private bool _inMethod = false;
    private bool _inInterface = false;
    private bool _inEnum = false;
    private readonly Stack<string> _contextStack = new();

    /// <summary>
    /// Gets the generated Java source code.
    /// </summary>
    /// <returns>The generated source code as a string.</returns>
    public string GetGeneratedCode() => _code.ToString().Trim();

    /// <summary>
    /// Visits a cognitive graph node before traversing its children.
    /// </summary>
    /// <param name="node">The cognitive graph node to visit and process.</param>
    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        // Handle different node types
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
        // Handle closing braces and other post-children processing
        if (node is NonTerminalNode nonTerminal)
        {
            switch (nonTerminal.RuleName.ToLowerInvariant())
            {
                case "class_declaration":
                case "class_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inClass = false;
                    _contextStack.Pop();
                    break;
                    
                case "interface_declaration":
                case "interface_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inInterface = false;
                    _contextStack.Pop();
                    break;
                    
                case "enum_declaration":
                case "enum_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _inEnum = false;
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
                    
                case "if_statement":
                case "then_statement":
                case "else_statement":
                    if (_contextStack.Count > 0 && _contextStack.Peek() == "if")
                    {
                        // Closing if block
                    }
                    break;
                    
                case "switch_statement":
                case "switch_block":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "for_statement":
                case "for_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "while_statement":
                case "while_body":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
                    
                case "do_while_statement":
                    Append(" ");
                    break;
                    
                case "try_statement":
                    _indentLevel--;
                    AppendIndentedLine("}");
                    _contextStack.Pop();
                    break;
            }
        }
    }

    private void VisitTerminalNode(TerminalNode node)
    {
        // Handle Java keywords and operators
        var text = node.Text;
        
        // Check if this is a Java keyword
        if (IsJavaKeyword(text))
        {
            // Add space before keyword if needed
            if (_needsNewline && !IsControlKeyword(text))
            {
                Append(" ");
            }
            Append(text);
            
            // Some keywords need space after
            if (NeedsSpaceAfter(text))
            {
                Append(" ");
            }
            
            // Track context
            if (text == "class") _inClass = true;
            if (text == "interface") _inInterface = true;
            if (text == "enum") _inEnum = true;
            
            return;
        }
        
        // Handle operators
        if (IsJavaOperator(text))
        {
            // Add spaces around operators
            if (!string.IsNullOrEmpty(_code.ToString()) && _code.ToString().Last() != ' ')
            {
                Append(" ");
            }
            Append(text);
            Append(" ");
            return;
        }
        
        // Handle separators
        if (IsSeparator(text))
        {
            Append(text);
            
            // Add space after comma
            if (text == ",")
            {
                Append(" ");
            }
            return;
        }
        
        // Default: just append
        Append(text);
    }

    private void VisitNonTerminalNode(NonTerminalNode node)
    {
        var ruleName = node.RuleName.ToLowerInvariant();
        
        switch (ruleName)
        {
            // Package and imports
            case "package_declaration":
                Append("package ");
                _contextStack.Push("package");
                break;
                
            case "import_declaration":
                Append("import ");
                _contextStack.Push("import");
                break;
                
            // Class declarations
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
                
            // Interface declarations
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
                
            // Enum declarations
            case "enum_declaration":
                AppendModifiers(node);
                Append("enum ");
                _contextStack.Push("enum");
                _inEnum = true;
                break;
                
            case "enum_header":
                break;
                
            case "enum_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("enum_body");
                break;
                
            case "enum_constant":
                break;
                
            // Method declarations
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
                
            // Constructor declarations
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
                
            // Field declarations
            case "field_declaration":
                AppendModifiers(node);
                _contextStack.Push("field");
                break;
                
            // Local variable declarations
            case "local_variable_declaration":
                AppendModifiers(node);
                _contextStack.Push("local_variable");
                break;
                
            // Type declarations
            case "type_declaration":
                break;
                
            // Modifiers
            case "modifiers":
                AppendModifiers(node);
                break;
                
            // Type parameters (generics)
            case "type_parameters":
                Append("<");
                _contextStack.Push("type_parameters");
                break;
                
            case "type_parameter":
                break;
                
            case "type_parameter_list":
                break;
                
            // Type arguments
            case "type_arguments":
                Append("<");
                _contextStack.Push("type_arguments");
                break;
                
            case "type_argument":
                break;
                
            case "type_argument_list":
                break;
                
            // Parameters
            case "formal_parameters":
                Append("(");
                _contextStack.Push("formal_parameters");
                break;
                
            case "formal_parameter":
                break;
                
            case "formal_parameter_list":
                break;
                
            // Method calls
            case "method_invocation":
                _contextStack.Push("method_invocation");
                break;
                
            case "argument_list":
                Append("(");
                _contextStack.Push("argument_list");
                break;
                
            // Array declarations
            case "array_declarator":
                break;
                
            case "dimensions":
                break;
                
            // Initializers
            case "array_initializer":
                Append("{");
                _contextStack.Push("array_initializer");
                break;
                
            case "variable_initializer":
                Append(" = ");
                _contextStack.Push("variable_initializer");
                break;
                
            // Control flow
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
                
            case "switch_expression":
                Append("switch (");
                _contextStack.Push("switch_expression");
                break;
                
            case "switch_block":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("switch_block");
                break;
                
            case "case_label":
                AppendIndented("case ");
                _contextStack.Push("case");
                break;
                
            case "default_label":
                AppendIndented("default:");
                _contextStack.Push("default");
                break;
                
            // Loops
            case "for_statement":
                Append("for (");
                _contextStack.Push("for");
                break;
                
            case "for_init":
                break;
                
            case "for_condition":
                Append("; ");
                break;
                
            case "for_update":
                Append("; ");
                break;
                
            case "for_body":
                Append(") ");
                _contextStack.Push("for_body");
                break;
                
            case "enhanced_for_statement":
                Append("for (");
                _contextStack.Push("enhanced_for");
                break;
                
            case "enhanced_for_header":
                break;
                
            case "enhanced_for_body":
                Append(") ");
                _contextStack.Push("enhanced_for_body");
                break;
                
            case "while_statement":
                Append("while (");
                _contextStack.Push("while");
                break;
                
            case "while_condition":
                break;
                
            case "while_body":
                Append(") ");
                _contextStack.Push("while_body");
                break;
                
            case "do_while_statement":
                Append("do ");
                _contextStack.Push("do_while");
                break;
                
            case "do_body":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("do_body");
                break;
                
            case "while_condition_at_end":
                _indentLevel--;
                AppendIndented("} while (");
                _contextStack.Push("while_condition_end");
                break;
                
            // Try-catch-finally
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
                
            case "catch_formal_parameter":
                break;
                
            case "catch_block":
                Append(" {\n");
                _indentLevel++;
                _contextStack.Push("catch_block");
                break;
                
            case "finally_block":
                _indentLevel--;
                AppendIndented(" } finally {\n");
                _indentLevel++;
                _contextStack.Push("finally");
                break;
                
            // Try-with-resources
            case "try_with_resources_statement":
                Append("try (");
                _contextStack.Push("try_with_resources");
                break;
                
            case "resource_specification":
                break;
                
            case "resource":
                break;
                
            // Synchronized
            case "synchronized_statement":
                Append("synchronized (");
                _contextStack.Push("synchronized");
                break;
                
            case "synchronized_block":
                Append(") {\n");
                _indentLevel++;
                _contextStack.Push("synchronized_block");
                break;
                
            // Lambda expressions
            case "lambda_expression":
                Append("(");
                _contextStack.Push("lambda");
                break;
                
            case "lambda_parameters":
                break;
                
            case "lambda_body":
                Append(") -> ");
                _contextStack.Push("lambda_body");
                break;
                
            // Method reference
            case "method_reference":
                _contextStack.Push("method_reference");
                break;
                
            // Array creation
            case "array_creation_expression":
                Append("new ");
                _contextStack.Push("array_creation");
                break;
                
            // Object creation
            case "object_creation_expression":
                Append("new ");
                _contextStack.Push("object_creation");
                break;
                
            // Static initializer
            case "static_initializer":
                AppendIndented("static {\n");
                _indentLevel++;
                _contextStack.Push("static_initializer");
                break;
                
            // Instance initializer
            case "instance_initializer":
                AppendIndented("{\n");
                _indentLevel++;
                _contextStack.Push("instance_initializer");
                break;
                
            // Annotations
            case "annotation":
                Append("@");
                _contextStack.Push("annotation");
                break;
                
            case "annotation_name":
                break;
                
            case "element_value_pairs":
                Append("(");
                _contextStack.Push("element_value_pairs");
                break;
                
            case "element_value_pair":
                break;
                
            // Types
            case "type":
                break;
                
            case "reference_type":
                break;
                
            case "primitive_type":
                break;
                
            // Statements
            case "statement":
                break;
                
            case "statement_expression":
                break;
                
            case "expression_statement":
                break;
                
            case "empty_statement":
                Append(";\n");
                break;
                
            case "labeled_statement":
                break;
                
            case "break_statement":
                AppendIndented("break");
                break;
                
            case "continue_statement":
                AppendIndented("continue");
                break;
                
            case "return_statement":
                AppendIndented("return");
                _contextStack.Push("return");
                break;
                
            case "throw_statement":
                AppendIndented("throw ");
                _contextStack.Push("throw");
                break;
                
            // Blocks
            case "block":
                Append("{\n");
                _indentLevel++;
                _contextStack.Push("block");
                break;
                
            // Expressions
            case "expression":
                break;
                
            case "primary_expression":
                break;
                
            case "assignment_expression":
                break;
                
            case "conditional_expression":
                break;
                
            case "binary_expression":
                break;
                
            case "unary_expression":
                break;
                
            case "cast_expression":
                Append("(");
                _contextStack.Push("cast");
                break;
                
            case "instanceof_expression":
                Append(" instanceof ");
                _contextStack.Push("instanceof");
                break;
                
            // Literals
            case "string_literal":
                break;
                
            case "integer_literal":
                break;
                
            case "floating_point_literal":
                break;
                
            case "boolean_literal":
                break;
                
            case "null_literal":
                Append("null");
                break;
                
            case "character_literal":
                break;
                
            // Names
            case "qualified_name":
                break;
                
            case "simple_name":
                break;
                
            // Comments (preserve if present)
            case "comment":
                Append("// ");
                _contextStack.Push("comment");
                break;
                
            case "block_comment":
                Append("/* ");
                _contextStack.Push("block_comment");
                break;
                
            case "javadoc_comment":
                Append("/**\n");
                _indentLevel++;
                _contextStack.Push("javadoc");
                break;
                
            default:
                // Unknown node type - just continue
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
                // Escape special characters in string
                var escaped = stringValue
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
                Append($"\"{escaped}\"");
                break;
                
            case int intValue:
                Append(intValue.ToString());
                break;
                
            case long longValue:
                Append(longValue.ToString() + "L");
                break;
                
            case float floatValue:
                Append(floatValue.ToString() + "f");
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
        // Find modifier children and append them
        foreach (var child in node.Children)
        {
            if (child is TerminalNode terminal && IsJavaModifier(terminal.Text))
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
        Append(new string(' ', _indentLevel * 4));
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

    private bool IsJavaKeyword(string text)
    {
        var keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            // Modifiers
            "public", "private", "protected", "static", "final", "abstract", 
            "synchronized", "native", "strictfp", "transient", "volatile",
            
            // Class/interface/enum
            "class", "interface", "enum", "record", "sealed", "non-sealed",
            
            // Method
            "void", "return", "this", "super",
            
            // Control flow
            "if", "else", "switch", "case", "default", "for", "while", "do",
            "break", "continue", "try", "catch", "finally", "throw", "throws",
            
            // Types
            "byte", "short", "int", "long", "char", "float", "double", "boolean",
            "String", "Object", "Class", "Void",
            
            // Object-oriented
            "new", "extends", "implements", "instanceof", "import", "package",
            
            // Generics
            "<", ">",
            
            // Annotations
            "@interface",
            
            // Other
            "var", "yield"
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
            "public", "private", "protected", "static", "final", "abstract",
            "synchronized", "native", "class", "interface", "enum",
            "void", "int", "long", "double", "float", "char", "boolean",
            "byte", "short", "String", "if", "else", "for", "while", "do",
            "switch", "case", "default", "try", "catch", "finally", "return",
            "throw", "new", "extends", "implements"
        };
        
        return keywords.Contains(text);
    }

    private bool IsJavaOperator(string text)
    {
        var operators = new HashSet<string>
        {
            "+", "-", "*", "/", "%", "=", "+=", "-=", "*=", "/=", "%=",
            "==", "!=", ">", "<", ">=", "<=", "&&", "||", "!", "&", "|",
            "^", "~", "<<", ">>", ">>>", "<<<", ">>>", "->", "::", ".", "?", ":"
        };
        
        return operators.Contains(text);
    }

    private bool IsSeparator(string text)
    {
        return text == "(" || text == ")" || text == "{" || text == "}" || 
               text == "[" || text == "]" || text == ";" || text == "," || 
               text == "." || text == ":" || text == "?";
    }

    private bool IsJavaModifier(string text)
    {
        var modifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "public", "private", "protected", "static", "final", "abstract",
            "synchronized", "native", "strictfp", "transient", "volatile"
        };
        
        return modifiers.Contains(text);
    }

    private string EscapeChar(char c)
    {
        switch (c)
        {
            case '\\': return "\\\\";
            case '"': return "\\\"";
            case '\'': return "\\'"";
            case '\n': return "\\n";
            case '\r': return "\\r";
            case '\t': return "\\t";
            default: return c.ToString();
        }
    }
}
