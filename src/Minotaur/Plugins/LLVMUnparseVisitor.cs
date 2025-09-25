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
/// Visitor for generating LLVM IR from cognitive graph structures.
/// Implements the LLVM intermediate language generation strategy as specified
/// in the LLVM feasibility report.
/// </summary>
public class LLVMUnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly StringBuilder _llvmIR = new();
    private readonly Dictionary<string, int> _ssaCounters = new();
    private readonly Dictionary<string, string> _basicBlocks = new();
    private readonly List<string> _globalDeclarations = new();
    private readonly List<string> _typeDefinitions = new();
    private readonly List<string> _functionDefinitions = new();
    private int _currentIndentLevel = 0;
    private string _currentFunction = "";
    private bool _inFunctionBody = false;

    public string GetGeneratedLLVMIR()
    {
        var result = new StringBuilder();

        // Add module header
        result.AppendLine("; Generated LLVM IR for Minotaur Parser");
        result.AppendLine($"; Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        result.AppendLine();
        result.AppendLine("target datalayout = \"e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128\"");
        result.AppendLine("target triple = \"x86_64-unknown-linux-gnu\"");
        result.AppendLine();

        // Add type definitions
        result.AppendLine("; Parser data structure types");
        result.AppendLine("%parser_state = type { i32, i8*, i32, i32, i32 }  ; state, input, position, line, column");
        result.AppendLine("%token = type { i32, i8*, i32, i32 }              ; type, value, start_pos, end_pos");
        result.AppendLine("%ast_node = type { i32, i8*, %ast_node**, i32 }   ; node_type, value, children, child_count");
        result.AppendLine("%parser_context = type { %parser_state*, %token*, %ast_node* }");
        result.AppendLine();

        // Add external function declarations
        result.AppendLine("; External function declarations");
        result.AppendLine("declare i8* @malloc(i64) nounwind");
        result.AppendLine("declare void @free(i8*) nounwind");
        result.AppendLine("declare i8* @memcpy(i8*, i8*, i64) nounwind");
        result.AppendLine("declare i32 @printf(i8*, ...) nounwind");
        result.AppendLine();

        // Add type definitions
        foreach (var typedef in _typeDefinitions)
        {
            result.AppendLine(typedef);
        }

        // Add global declarations
        foreach (var global in _globalDeclarations)
        {
            result.AppendLine(global);
        }

        // Add function definitions
        foreach (var function in _functionDefinitions)
        {
            result.AppendLine(function);
        }

        // Add generated IR
        result.Append(_llvmIR.ToString());

        // Add function attributes
        result.AppendLine();
        result.AppendLine("; Function attributes");
        result.AppendLine("attributes #0 = { nounwind uwtable \"frame-pointer\"=\"non-leaf\" \"min-legal-vector-width\"=\"0\" \"no-trapping-math\"=\"true\" \"stack-protector-buffer-size\"=\"8\" \"target-cpu\"=\"x86-64\" \"target-features\"=\"+cx8,+fxsr,+mmx,+sse,+sse2,+x87\" \"tune-cpu\"=\"generic\" }");

        return result.ToString();
    }

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        switch (node)
        {
            case TerminalNode terminal:
                GenerateTerminalIR(terminal);
                break;
            case NonTerminalNode nonTerminal:
                GenerateNonTerminalIR(nonTerminal);
                break;
        }
    }

    private void GenerateTerminalIR(TerminalNode node)
    {
        if (IsParserRule(node.Text))
        {
            GenerateParserFunctionIR(node.Text);
        }
        else if (IsLexerToken(node.Text))
        {
            GenerateLexerFunctionIR(node.Text);
        }
        else
        {
            // Generate basic token matching IR
            AppendIndentedIR($"; Terminal: {node.Text}");
            
            // Only generate inline IR if we're inside a function body
            if (_inFunctionBody)
            {
                var tokenVar = GetNextSSAName("token");
                AppendIndentedIR($"{tokenVar} = call i32 @match_token(i8* getelementptr inbounds ([{node.Text.Length + 1} x i8], [{node.Text.Length + 1} x i8]* @.str.{GetStringLiteralId(node.Text)}, i64 0, i64 0))");
            }
        }
    }

    private void GenerateNonTerminalIR(NonTerminalNode node)
    {
        var ruleName = SanitizeIdentifier(node.RuleName);
        
        switch (node.RuleName.ToLowerInvariant())
        {
            case "program":
            case "compilation_unit":
                GenerateMainParserFunction(ruleName);
                break;
            case "expression":
            case "statement":
            case "declaration":
                GenerateRuleParserFunction(ruleName, node);
                break;
            default:
                GenerateGenericRuleFunction(ruleName, node);
                break;
        }

        // Handle special cases where terminal might represent identifiers or literals
        if (IsIdentifierRule(node.RuleName))
        {
            GenerateIdentifierIR(node);
        }
        else if (IsLiteralRule(node.RuleName))
        {
            GenerateLiteralIR(node);
        }
    }

    private void GenerateIdentifierIR(CognitiveGraphNode node)
    {
        var text = GetNodeText(node);
        var identifier = SanitizeIdentifier(text);
        AppendIndentedIR($"; Identifier: {identifier}");
        
        var identifierVar = GetNextSSAName("identifier");
        AppendIndentedIR($"{identifierVar} = call %ast_node* @create_identifier_node(i8* getelementptr inbounds ([{text.Length + 1} x i8], [{text.Length + 1} x i8]* @.str.{GetStringLiteralId(text)}, i64 0, i64 0))");
    }

    private void GenerateLiteralIR(CognitiveGraphNode node)
    {
        var text = GetNodeText(node);
        AppendIndentedIR($"; Literal: {text}");
        
        var literalVar = GetNextSSAName("literal");
        
        // Try to determine literal type from the text
        if (IsStringLiteral(text))
        {
            var cleanText = text.Trim('"', '\'');
            AppendIndentedIR($"{literalVar} = call %ast_node* @create_string_literal_node(i8* getelementptr inbounds ([{cleanText.Length + 1} x i8], [{cleanText.Length + 1} x i8]* @.str.{GetStringLiteralId(cleanText)}, i64 0, i64 0))");
        }
        else if (int.TryParse(text, out int intValue))
        {
            AppendIndentedIR($"{literalVar} = call %ast_node* @create_integer_literal_node(i32 {intValue})");
        }
        else if (double.TryParse(text, out double doubleValue))
        {
            AppendIndentedIR($"{literalVar} = call %ast_node* @create_float_literal_node(double {doubleValue:F6})");
        }
        else
        {
            AppendIndentedIR($"{literalVar} = call %ast_node* @create_generic_literal_node(i8* getelementptr inbounds ([{text.Length + 1} x i8], [{text.Length + 1} x i8]* @.str.{GetStringLiteralId(text)}, i64 0, i64 0))");
        }
    }

    private void GenerateMainParserFunction(string ruleName)
    {
        _currentFunction = $"parse_{ruleName}";
        _inFunctionBody = true;

        AppendIR($"\n; Main parser function for {ruleName}");
        AppendIR($"define %ast_node* @{_currentFunction}(%parser_state* %state) {{");
        IncreaseIndent();
        
        AppendIndentedIR("entry:");
        IncreaseIndent();
        
        AppendIndentedIR("; Initialize parser state");
        AppendIndentedIR("%parser_initialized = call i32 @initialize_parser_state(%parser_state* %state)");
        AppendIndentedIR("%init_check = icmp eq i32 %parser_initialized, 1");
        AppendIndentedIR("br i1 %init_check, label %parse_start, label %error_exit");
        
        DecreaseIndent();
        AppendIndentedIR("\nparse_start:");
        IncreaseIndent();
        
        AppendIndentedIR("; Begin main parsing logic");
        var resultVar = GetNextSSAName("parse_result");
        AppendIndentedIR($"{resultVar} = call %ast_node* @parse_grammar_start(%parser_state* %state)");
        AppendIndentedIR($"ret %ast_node* {resultVar}");
        
        DecreaseIndent();
        AppendIndentedIR("\nerror_exit:");
        IncreaseIndent();
        AppendIndentedIR("ret %ast_node* null");
        
        DecreaseIndent();
        DecreaseIndent();
        AppendIR("}");
        
        _inFunctionBody = false;
    }

    private void GenerateRuleParserFunction(string ruleName, NonTerminalNode node)
    {
        var functionName = $"parse_{ruleName}";
        _inFunctionBody = true;
        
        AppendIR($"\n; Parser function for {ruleName} rule");
        AppendIR($"define %ast_node* @{functionName}(%parser_state* %state) {{");
        IncreaseIndent();
        
        AppendIndentedIR("entry:");
        IncreaseIndent();
        
        // Generate state machine logic for rule parsing
        AppendIndentedIR($"; Parse {ruleName} using state machine");
        AppendIndentedIR("%current_token = call %token* @get_current_token(%parser_state* %state)");
        AppendIndentedIR("%token_type = getelementptr inbounds %token, %token* %current_token, i32 0, i32 0");  
        AppendIndentedIR("%type_value = load i32, i32* %token_type, align 4");
        
        // Generate switch statement for different token types
        AppendIndentedIR("switch i32 %type_value, label %parse_error [");
        IncreaseIndent();
        
        // Add cases for different parsing alternatives
        AppendIndentedIR("i32 1, label %parse_alternative_1");
        AppendIndentedIR("i32 2, label %parse_alternative_2");
        AppendIndentedIR("i32 3, label %parse_alternative_3");
        
        DecreaseIndent();
        AppendIndentedIR("]");
        
        // Generate alternative parsing blocks
        GenerateParsingAlternatives(ruleName);
        
        DecreaseIndent();
        DecreaseIndent();
        AppendIR("}");
    }

    private void GenerateParsingAlternatives(string ruleName)
    {
        // Alternative 1
        DecreaseIndent();
        AppendIndentedIR("\nparse_alternative_1:");
        IncreaseIndent();
        AppendIndentedIR($"; First alternative for {ruleName}");
        var result1 = GetNextSSAName("alt1_result");
        AppendIndentedIR($"{result1} = call %ast_node* @parse_alternative_1_%{ruleName}(%parser_state* %state)");
        AppendIndentedIR("br label %parse_success");
        
        // Alternative 2
        DecreaseIndent();
        AppendIndentedIR("\nparse_alternative_2:");
        IncreaseIndent();
        AppendIndentedIR($"; Second alternative for {ruleName}");
        var result2 = GetNextSSAName("alt2_result");
        AppendIndentedIR($"{result2} = call %ast_node* @parse_alternative_2_%{ruleName}(%parser_state* %state)");
        AppendIndentedIR("br label %parse_success");
        
        // Alternative 3
        DecreaseIndent();
        AppendIndentedIR("\nparse_alternative_3:");
        IncreaseIndent();
        AppendIndentedIR($"; Third alternative for {ruleName}");
        var result3 = GetNextSSAName("alt3_result");
        AppendIndentedIR($"{result3} = call %ast_node* @parse_alternative_3_%{ruleName}(%parser_state* %state)");
        AppendIndentedIR("br label %parse_success");
        
        // Success block with PHI node
        DecreaseIndent();
        AppendIndentedIR("\nparse_success:");
        IncreaseIndent();
        var phiResult = GetNextSSAName("parse_result");
        AppendIndentedIR($"{phiResult} = phi %ast_node* [ {result1}, %parse_alternative_1 ], [ {result2}, %parse_alternative_2 ], [ {result3}, %parse_alternative_3 ]");
        AppendIndentedIR($"ret %ast_node* {phiResult}");
        
        // Error block
        DecreaseIndent();
        AppendIndentedIR("\nparse_error:");
        IncreaseIndent();
        AppendIndentedIR($"; Error parsing {ruleName}");
        AppendIndentedIR("call void @report_parse_error(%parser_state* %state, i32 1)");
        AppendIndentedIR("ret %ast_node* null");
    }

    private void GenerateGenericRuleFunction(string ruleName, NonTerminalNode node)
    {
        var functionName = $"parse_{ruleName}";
        _inFunctionBody = true;
        
        AppendIR($"\n; Generic parser function for {ruleName}");
        AppendIR($"define %ast_node* @{functionName}(%parser_state* %state) {{");
        IncreaseIndent();
        
        AppendIndentedIR("entry:");
        IncreaseIndent();
        
        AppendIndentedIR($"; Generic parsing logic for {ruleName}");
        var genericResult = GetNextSSAName("generic_result");
        AppendIndentedIR($"{genericResult} = call %ast_node* @create_nonterminal_node(i8* getelementptr inbounds ([{ruleName.Length + 1} x i8], [{ruleName.Length + 1} x i8]* @.str.{GetStringLiteralId(ruleName)}, i64 0, i64 0))");
        AppendIndentedIR($"ret %ast_node* {genericResult}");
        
        DecreaseIndent();
        DecreaseIndent();
        AppendIR("}");
        
        _inFunctionBody = false;
    }

    private void GenerateParserFunctionIR(string ruleName)
    {
        // This would be called for parser rules detected in terminals
        AppendIndentedIR($"; Call to parser rule: {ruleName}");
        var ruleResult = GetNextSSAName("rule_result");
        AppendIndentedIR($"{ruleResult} = call %ast_node* @parse_{SanitizeIdentifier(ruleName)}(%parser_state* %state)");
    }

    private void GenerateLexerFunctionIR(string tokenName)
    {
        var functionName = $"lex_{SanitizeIdentifier(tokenName)}";
        
        AppendIR($"\n; Lexer function for {tokenName} token");
        AppendIR($"define i32 @{functionName}(%parser_state* %state) {{");
        IncreaseIndent();
        
        AppendIndentedIR("entry:");
        IncreaseIndent();
        
        AppendIndentedIR($"; Tokenize {tokenName}");
        AppendIndentedIR("%input_ptr = getelementptr inbounds %parser_state, %parser_state* %state, i32 0, i32 1");
        AppendIndentedIR("%input = load i8*, i8** %input_ptr");
        AppendIndentedIR("%position_ptr = getelementptr inbounds %parser_state, %parser_state* %state, i32 0, i32 2");
        AppendIndentedIR("%position = load i32, i32* %position_ptr");
        
        var tokenResult = GetNextSSAName("token_result");
        AppendIndentedIR($"{tokenResult} = call i32 @match_{SanitizeIdentifier(tokenName)}_token(i8* %input, i32 %position)");
        AppendIndentedIR($"ret i32 {tokenResult}");
        
        DecreaseIndent();
        DecreaseIndent();
        AppendIR("}");
    }

    private string GetNextSSAName(string baseName)
    {
        if (!_ssaCounters.ContainsKey(baseName))
        {
            _ssaCounters[baseName] = 0;
        }
        
        return $"%{baseName}{_ssaCounters[baseName]++}";
    }

    private string SanitizeIdentifier(string identifier)
    {
        return identifier.Replace("-", "_").Replace(" ", "_").Replace(".", "_");
    }

    private int GetStringLiteralId(string literal)
    {
        // Simple hash-based ID for string literals
        return Math.Abs(literal.GetHashCode());
    }

    private bool IsParserRule(string text)
    {
        // Heuristic to detect parser rules (capitalized words, common rule patterns)
        return char.IsUpper(text[0]) || text.Contains("_rule") || text.Contains("_expr") || text.Contains("_stmt");
    }

    private bool IsLexerToken(string text)
    {
        // Heuristic to detect lexer tokens (all caps, special patterns)
        return text.All(char.IsUpper) || text.Contains("TOKEN") || text.Contains("_T");
    }

    private void AppendIR(string code)
    {
        _llvmIR.AppendLine(code);
    }

    private void AppendIndentedIR(string code)
    {
        _llvmIR.AppendLine(new string(' ', _currentIndentLevel * 2) + code);
    }

    private void IncreaseIndent()
    {
        _currentIndentLevel++;
    }

    private void DecreaseIndent()
    {
        _currentIndentLevel = Math.Max(0, _currentIndentLevel - 1);
    }

    private string GetNodeText(CognitiveGraphNode node)
    {
        return node switch
        {
            TerminalNode terminal => terminal.Text,
            NonTerminalNode nonTerminal => nonTerminal.RuleName,
            _ => node.NodeType
        };
    }

    private bool IsIdentifierRule(string ruleName)
    {
        var lowerRule = ruleName.ToLowerInvariant();
        return lowerRule.Contains("identifier") || 
               lowerRule.Contains("name") || 
               lowerRule.Contains("id") ||
               lowerRule == "var" ||
               lowerRule == "variable";
    }

    private bool IsLiteralRule(string ruleName)
    {
        var lowerRule = ruleName.ToLowerInvariant();
        return lowerRule.Contains("literal") || 
               lowerRule.Contains("constant") || 
               lowerRule.Contains("number") ||
               lowerRule.Contains("string") ||
               lowerRule.Contains("char") ||
               lowerRule.Contains("bool");
    }

    private bool IsStringLiteral(string text)
    {
        return (text.StartsWith('"') && text.EndsWith('"')) || 
               (text.StartsWith('\'') && text.EndsWith('\''));
    }
}