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

namespace Minotaur.Plugins;

/// <summary>
/// Validation visitor for LLVM IR generation from cognitive graphs.
/// Ensures the graph structure is suitable for LLVM IR generation.
/// </summary>
public class LLVMIRValidationVisitor : CognitiveGraphVisitorBase
{
    private readonly List<UnparseValidationError> _validationErrors = new();
    private readonly List<UnparseValidationWarning> _performanceWarnings = new();
    private bool _hasStructuralIssues = false;
    private int _nodeDepth = 0;
    private int _maxDepth = 0;
    private int _totalNodes = 0;
    private int _complexityScore = 0;

    public bool HasStructuralIssues => _hasStructuralIssues;
    public bool HasPerformanceWarnings => _performanceWarnings.Count > 0;
    public List<UnparseValidationError> ValidationErrors => _validationErrors;
    public List<UnparseValidationWarning> PerformanceWarnings => _performanceWarnings;

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        _nodeDepth++;
        _totalNodes++;
        _maxDepth = Math.Max(_maxDepth, _nodeDepth);

        ValidateNodeStructure(node);
        CheckPerformanceImplications(node);
    }

    protected override void AfterVisitNode(CognitiveGraphNode node)
    {
        _nodeDepth--;
    }

    private void ValidateNodeStructure(CognitiveGraphNode node)
    {
        switch (node)
        {
            case TerminalNode terminal:
                ValidateTerminalNode(terminal);
                break;
            case NonTerminalNode nonTerminal:
                ValidateNonTerminalNode(nonTerminal);
                break;
            default:
                _validationErrors.Add(new UnparseValidationError
                {
                    Message = $"Unknown node type: {node.GetType().Name}",
                    NodeId = node.GetHashCode().ToString(),
                    NodeType = node.GetType().Name,
                    Severity = "Error"
                });
                _hasStructuralIssues = true;
                break;
        }

        // Additional validation for identifier-like and literal-like nodes
        if (IsIdentifierLikeNode(node))
        {
            ValidateIdentifierLikeNode(node);
        }
        else if (IsLiteralLikeNode(node))
        {
            ValidateLiteralLikeNode(node);
        }
    }

    private void ValidateTerminalNode(TerminalNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Text))
        {
            _validationErrors.Add(new UnparseValidationError
            {
                Message = "Terminal node has empty or null text",
                NodeId = node.GetHashCode().ToString(),
                NodeType = "TerminalNode",
                Severity = "Error"
            });
            _hasStructuralIssues = true;
        }

        // Check for LLVM-incompatible characters
        if (ContainsLLVMIncompatibleCharacters(node.Text))
        {
            _performanceWarnings.Add(new UnparseValidationWarning
            {
                Message = $"Terminal '{node.Text}' contains characters that may require escaping in LLVM IR",
                NodeId = node.GetHashCode().ToString(),
                NodeType = "TerminalNode"
            });
        }

        // Complexity scoring
        _complexityScore += 1;
    }

    private void ValidateNonTerminalNode(NonTerminalNode node)
    {
        if (string.IsNullOrWhiteSpace(node.RuleName))
        {
            _validationErrors.Add(new UnparseValidationError
            {
                Message = "NonTerminal node has empty or null rule name",
                NodeId = node.GetHashCode().ToString(),
                NodeType = "NonTerminalNode",
                Severity = "Error"
            });
            _hasStructuralIssues = true;
        }

        // Check for valid LLVM function naming
        if (!IsValidLLVMFunctionName(node.RuleName))
        {
            _performanceWarnings.Add(new UnparseValidationWarning
            {
                Message = $"Rule name '{node.RuleName}' may not be valid for LLVM function naming",
                NodeId = node.GetHashCode().ToString(),
                NodeType = "NonTerminalNode"
            });
        }

        // Check for excessive nesting that could impact LLVM optimization
        if (_nodeDepth > 50) // Arbitrary threshold for deep nesting
        {
            _performanceWarnings.Add(new UnparseValidationWarning
            {
                Message = $"Deep nesting detected at depth {_nodeDepth} - may impact LLVM optimization",
                NodeId = node.GetHashCode().ToString(),
                NodeType = "NonTerminalNode"
            });
        }

        // Complexity scoring based on rule complexity
        _complexityScore += GetRuleComplexityScore(node.RuleName);
    }

    private void ValidateIdentifierLikeNode(CognitiveGraphNode node)
    {
        var text = GetNodeText(node);
        if (string.IsNullOrWhiteSpace(text))
        {
            _validationErrors.Add(new UnparseValidationError
            {
                Message = "Identifier-like node has empty or null text",
                NodeId = node.GetHashCode().ToString(),
                NodeType = node.GetType().Name,
                Severity = "Error"
            });
            _hasStructuralIssues = true;
        }

        // Check for LLVM reserved keywords
        if (IsLLVMReservedKeyword(text))
        {
            _performanceWarnings.Add(new UnparseValidationWarning
            {
                Message = $"Identifier '{text}' is an LLVM reserved keyword and will be escaped",
                NodeId = node.GetHashCode().ToString(),
                NodeType = node.GetType().Name
            });
        }

        _complexityScore += 1;
    }

    private void ValidateLiteralLikeNode(CognitiveGraphNode node)
    {
        var text = GetNodeText(node);
        if (text == null)
        {
            _performanceWarnings.Add(new UnparseValidationWarning
            {
                Message = "Literal-like node has null text - will generate null pointer in LLVM IR",
                NodeId = node.GetHashCode().ToString(),
                NodeType = node.GetType().Name
            });
        }

        _complexityScore += 1;
    }

    private void CheckPerformanceImplications(CognitiveGraphNode node)
    {
        // Check for potential performance issues in LLVM IR generation

        // Warn about very large graphs
        if (_totalNodes > 1000) // Arbitrary threshold
        {
            if (_totalNodes % 500 == 0) // Only warn periodically to avoid spam
            {
                _performanceWarnings.Add(new UnparseValidationWarning
                {
                    Message = $"Large graph detected ({_totalNodes} nodes) - LLVM compilation may be slow",
                    NodeId = node.GetHashCode().ToString(),
                    NodeType = node.GetType().Name
                });
            }
        }

        // Warn about high complexity
        if (_complexityScore > 500) // Arbitrary threshold
        {
            if (_complexityScore % 100 == 0) // Only warn periodically
            {
                _performanceWarnings.Add(new UnparseValidationWarning
                {
                    Message = $"High complexity score ({_complexityScore}) - generated LLVM IR may be complex",
                    NodeId = node.GetHashCode().ToString(),
                    NodeType = node.GetType().Name
                });
            }
        }
    }

    private bool ContainsLLVMIncompatibleCharacters(string text)
    {
        // Characters that have special meaning in LLVM IR
        var incompatibleChars = new[] { '@', '%', '!', '"', '\\', '\n', '\r', '\t' };
        return text.IndexOfAny(incompatibleChars) >= 0;
    }

    private bool IsValidLLVMFunctionName(string name)
    {
        // LLVM function names must start with a letter or underscore
        // and contain only letters, digits, and underscores
        if (string.IsNullOrEmpty(name))
            return false;

        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;

        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsLLVMReservedKeyword(string identifier)
    {
        // Common LLVM IR keywords that should be avoided as identifiers
        var reservedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "define", "declare", "call", "ret", "br", "switch", "invoke", "resume",
            "unreachable", "add", "sub", "mul", "div", "rem", "and", "or", "xor",
            "shl", "shr", "icmp", "fcmp", "phi", "select", "alloca", "load", "store",
            "getelementptr", "bitcast", "inttoptr", "ptrtoint", "trunc", "zext", "sext",
            "fptrunc", "fpext", "fptoui", "fptosi", "uitofp", "sitofp", "entry",
            "target", "datalayout", "triple", "type", "global", "constant", "private",
            "internal", "external", "linkonce", "weak", "common", "appending",
            "extern_weak", "linkonce_odr", "weak_odr", "available_externally",
            "dllimport", "dllexport", "thread_local", "unnamed_addr", "null", "undef",
            "true", "false", "attributes", "align", "nounwind", "readonly", "readnone"
        };

        return reservedKeywords.Contains(identifier);
    }

    private int GetRuleComplexityScore(string ruleName)
    {
        // Heuristic scoring based on rule name patterns
        int score = 1; // Base score

        // More complex rules typically have longer names
        score += ruleName.Length / 10;

        // Rules with certain patterns are typically more complex
        if (ruleName.Contains("expression") || ruleName.Contains("expr"))
            score += 3;
        if (ruleName.Contains("statement") || ruleName.Contains("stmt"))
            score += 2;
        if (ruleName.Contains("declaration") || ruleName.Contains("decl"))
            score += 2;
        if (ruleName.Contains("type"))
            score += 2;
        if (ruleName.Contains("function") || ruleName.Contains("method"))
            score += 4;
        if (ruleName.Contains("class") || ruleName.Contains("struct"))
            score += 3;

        return score;
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

    private bool IsIdentifierLikeNode(CognitiveGraphNode node)
    {
        if (node is NonTerminalNode nonTerminal)
        {
            var ruleName = nonTerminal.RuleName.ToLowerInvariant();
            return ruleName.Contains("identifier") || 
                   ruleName.Contains("name") || 
                   ruleName.Contains("id") ||
                   ruleName == "var" ||
                   ruleName == "variable";
        }
        return false;
    }

    private bool IsLiteralLikeNode(CognitiveGraphNode node)
    {
        if (node is NonTerminalNode nonTerminal)
        {
            var ruleName = nonTerminal.RuleName.ToLowerInvariant();
            return ruleName.Contains("literal") || 
                   ruleName.Contains("constant") || 
                   ruleName.Contains("number") ||
                   ruleName.Contains("string") ||
                   ruleName.Contains("char") ||
                   ruleName.Contains("bool");
        }
        else if (node is TerminalNode terminal)
        {
            // Check if terminal text looks like a literal
            var text = terminal.Text;
            return (text.StartsWith('"') && text.EndsWith('"')) ||
                   (text.StartsWith('\'') && text.EndsWith('\'')) ||
                   int.TryParse(text, out _) ||
                   double.TryParse(text, out _) ||
                   bool.TryParse(text, out _);
        }
        return false;
    }
}

/// <summary>
/// Visitor to detect parser entry points in cognitive graphs.
/// </summary>
public class EntryPointDetectionVisitor : CognitiveGraphVisitorBase
{
    public bool HasEntryPoint { get; private set; } = false;

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        if (node is NonTerminalNode nonTerminal)
        {
            var ruleName = nonTerminal.RuleName.ToLowerInvariant();
            
            // Common entry point rule names
            if (ruleName == "program" || 
                ruleName == "compilation_unit" || 
                ruleName == "start" || 
                ruleName == "root" ||
                ruleName == "document" ||
                ruleName == "module")
            {
                HasEntryPoint = true;
            }
        }
    }
}

/// <summary>
/// Visitor to detect error handling constructs in cognitive graphs.
/// </summary>
public class ErrorHandlingDetectionVisitor : CognitiveGraphVisitorBase
{
    public bool HasErrorHandling { get; private set; } = false;

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        if (node is NonTerminalNode nonTerminal)
        {
            var ruleName = nonTerminal.RuleName.ToLowerInvariant();
            
            // Look for error handling rule patterns
            if (ruleName.Contains("error") ||
                ruleName.Contains("exception") ||
                ruleName.Contains("recovery") ||
                ruleName.Contains("catch") ||
                ruleName.Contains("finally"))
            {
                HasErrorHandling = true;
            }
        }
        else if (node is TerminalNode terminal)
        {
            var text = terminal.Text.ToLowerInvariant();
            
            // Look for error handling keywords
            if (text.Contains("try") ||
                text.Contains("catch") ||
                text.Contains("throw") ||
                text.Contains("error") ||
                text.Contains("exception"))
            {
                HasErrorHandling = true;
            }
        }
    }
}