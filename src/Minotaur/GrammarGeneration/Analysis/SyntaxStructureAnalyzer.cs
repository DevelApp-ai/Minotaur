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

using System.Text.RegularExpressions;
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.GrammarGeneration.Analysis;

/// <summary>
/// Analyzes source code to discover syntax structure patterns
/// </summary>
public class SyntaxStructureAnalyzer
{
    private readonly Dictionary<string, List<string>> _patterns = new();
    private readonly Dictionary<string, int> _patternFrequency = new();

    public ProductionRules DiscoverSyntaxPatterns(TokenDefinitions tokens, string[] sourceFiles)
    {
        // Analyze source files for patterns
        foreach (var sourceFile in sourceFiles)
        {
            AnalyzeSourceStructure(sourceFile, tokens);
        }

        // 1. Identify common patterns (function definitions, variable declarations)
        var functionPatterns = DiscoverFunctionPatterns();
        var declarationPatterns = DiscoverDeclarationPatterns();
        
        // 2. Analyze expression precedence and associativity
        var expressionRules = AnalyzeExpressionStructure();
        
        // 3. Identify statement types and control flow
        var statementRules = AnalyzeStatementStructure();
        
        return CombineIntoProductionRules(
            functionPatterns, declarationPatterns, 
            expressionRules, statementRules);
    }

    private void AnalyzeSourceStructure(string filePath, TokenDefinitions tokens)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || IsComment(trimmedLine))
                    continue;
                
                AnalyzeLine(trimmedLine, tokens);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing syntax structure in {filePath}: {ex.Message}");
        }
    }

    private bool IsComment(string line)
    {
        return line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("#");
    }

    private void AnalyzeLine(string line, TokenDefinitions tokens)
    {
        // Identify patterns based on structure
        if (IsFunctionDefinition(line))
        {
            RecordPattern("function_definition", line);
        }
        else if (IsVariableDeclaration(line))
        {
            RecordPattern("variable_declaration", line);
        }
        else if (IsControlStatement(line))
        {
            RecordPattern("control_statement", line);
        }
        else if (IsAssignment(line))
        {
            RecordPattern("assignment", line);
        }
        else if (IsExpressionStatement(line))
        {
            RecordPattern("expression_statement", line);
        }
    }

    private void RecordPattern(string patternType, string example)
    {
        if (!_patterns.ContainsKey(patternType))
        {
            _patterns[patternType] = new List<string>();
        }
        
        _patterns[patternType].Add(example);
        _patternFrequency[patternType] = _patternFrequency.GetValueOrDefault(patternType, 0) + 1;
    }

    private bool IsFunctionDefinition(string line)
    {
        var functionPatterns = new[]
        {
            @"^\s*(function|def|fun|func)\s+\w+\s*\(",
            @"^\s*\w+\s+\w+\s*\([^)]*\)\s*\{",
            @"^\s*(public|private|protected)?\s*(static)?\s*\w+\s+\w+\s*\("
        };
        
        return functionPatterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
    }

    private bool IsVariableDeclaration(string line)
    {
        var declarationPatterns = new[]
        {
            @"^\s*(var|let|const|int|string|float|double|bool)\s+\w+",
            @"^\s*\w+\s+\w+\s*=",
            @"^\s*(public|private|protected)?\s*(static)?\s*\w+\s+\w+\s*;"
        };
        
        return declarationPatterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
    }

    private bool IsControlStatement(string line)
    {
        var controlPatterns = new[]
        {
            @"^\s*(if|while|for|switch|try|catch|finally)\s*\(",
            @"^\s*(else|elif)\s*(\{|$)",
            @"^\s*(return|break|continue|throw)\s*(;|$|\w)"
        };
        
        return controlPatterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
    }

    private bool IsAssignment(string line)
    {
        return Regex.IsMatch(line, @"^\s*\w+\s*=\s*[^=]");
    }

    private bool IsExpressionStatement(string line)
    {
        return line.EndsWith(";") && !IsVariableDeclaration(line) && !IsAssignment(line);
    }

    private List<ProductionRule> DiscoverFunctionPatterns()
    {
        var rules = new List<ProductionRule>();
        
        if (_patterns.ContainsKey("function_definition"))
        {
            var examples = _patterns["function_definition"];
            
            rules.Add(new ProductionRule
            {
                Name = "function_definition",
                Alternatives = new List<string>
                {
                    "<function_keyword> <identifier> <LPAREN> <parameter_list>? <RPAREN> <block>",
                    "<type> <identifier> <LPAREN> <parameter_list>? <RPAREN> <block>",
                    "<modifier>* <type> <identifier> <LPAREN> <parameter_list>? <RPAREN> <block>"
                },
                Priority = 100,
                Confidence = 0.8,
                Examples = examples.Take(3).ToList()
            });
            
            rules.Add(new ProductionRule
            {
                Name = "parameter_list",
                Alternatives = new List<string>
                {
                    "<parameter>",
                    "<parameter_list> <COMMA> <parameter>"
                },
                Priority = 80,
                Confidence = 0.7
            });
            
            rules.Add(new ProductionRule
            {
                Name = "parameter",
                Alternatives = new List<string>
                {
                    "<type> <identifier>",
                    "<identifier>"
                },
                Priority = 70,
                Confidence = 0.8
            });
        }
        
        return rules;
    }

    private List<ProductionRule> DiscoverDeclarationPatterns()
    {
        var rules = new List<ProductionRule>();
        
        if (_patterns.ContainsKey("variable_declaration"))
        {
            var examples = _patterns["variable_declaration"];
            
            rules.Add(new ProductionRule
            {
                Name = "variable_declaration",
                Alternatives = new List<string>
                {
                    "<var_keyword> <identifier>",
                    "<var_keyword> <identifier> <ASSIGN> <expression>",
                    "<type> <identifier>",
                    "<type> <identifier> <ASSIGN> <expression>",
                    "<modifier>* <type> <identifier> <ASSIGN>? <expression>? <SEMICOLON>"
                },
                Priority = 90,
                Confidence = 0.85,
                Examples = examples.Take(3).ToList()
            });
        }
        
        return rules;
    }

    private List<ProductionRule> AnalyzeExpressionStructure()
    {
        var rules = new List<ProductionRule>();
        
        // Basic expression hierarchy
        rules.Add(new ProductionRule
        {
            Name = "expression",
            Alternatives = new List<string>
            {
                "<logical_or_expression>"
            },
            Priority = 50,
            Confidence = 0.9
        });
        
        rules.Add(new ProductionRule
        {
            Name = "logical_or_expression",
            Alternatives = new List<string>
            {
                "<logical_and_expression>",
                "<logical_or_expression> <OR_OP> <logical_and_expression>"
            },
            Priority = 45,
            Confidence = 0.8
        });
        
        rules.Add(new ProductionRule
        {
            Name = "logical_and_expression",
            Alternatives = new List<string>
            {
                "<equality_expression>",
                "<logical_and_expression> <AND_OP> <equality_expression>"
            },
            Priority = 44,
            Confidence = 0.8
        });
        
        rules.Add(new ProductionRule
        {
            Name = "equality_expression",
            Alternatives = new List<string>
            {
                "<relational_expression>",
                "<equality_expression> <EQ_OP> <relational_expression>",
                "<equality_expression> <NEQ_OP> <relational_expression>"
            },
            Priority = 43,
            Confidence = 0.8
        });
        
        rules.Add(new ProductionRule
        {
            Name = "relational_expression",
            Alternatives = new List<string>
            {
                "<additive_expression>",
                "<relational_expression> <LT_OP> <additive_expression>",
                "<relational_expression> <GT_OP> <additive_expression>",
                "<relational_expression> <LE_OP> <additive_expression>",
                "<relational_expression> <GE_OP> <additive_expression>"
            },
            Priority = 42,
            Confidence = 0.8
        });
        
        rules.Add(new ProductionRule
        {
            Name = "additive_expression",
            Alternatives = new List<string>
            {
                "<multiplicative_expression>",
                "<additive_expression> <PLUS_OP> <multiplicative_expression>",
                "<additive_expression> <MINUS_OP> <multiplicative_expression>"
            },
            Priority = 41,
            Confidence = 0.9
        });
        
        rules.Add(new ProductionRule
        {
            Name = "multiplicative_expression",
            Alternatives = new List<string>
            {
                "<primary_expression>",
                "<multiplicative_expression> <MULT_OP> <primary_expression>",
                "<multiplicative_expression> <DIV_OP> <primary_expression>"
            },
            Priority = 40,
            Confidence = 0.9
        });
        
        rules.Add(new ProductionRule
        {
            Name = "primary_expression",
            Alternatives = new List<string>
            {
                "<identifier>",
                "<NUMBER_LITERAL>",
                "<STRING_LITERAL>",
                "<LPAREN> <expression> <RPAREN>",
                "<function_call>"
            },
            Priority = 39,
            Confidence = 0.95
        });
        
        return rules;
    }

    private List<ProductionRule> AnalyzeStatementStructure()
    {
        var rules = new List<ProductionRule>();
        
        if (_patterns.ContainsKey("control_statement"))
        {
            // Control flow statements
            rules.Add(new ProductionRule
            {
                Name = "if_statement",
                Alternatives = new List<string>
                {
                    "<IF> <LPAREN> <expression> <RPAREN> <statement>",
                    "<IF> <LPAREN> <expression> <RPAREN> <statement> <ELSE> <statement>"
                },
                Priority = 85,
                Confidence = 0.9
            });
            
            rules.Add(new ProductionRule
            {
                Name = "while_statement",
                Alternatives = new List<string>
                {
                    "<WHILE> <LPAREN> <expression> <RPAREN> <statement>"
                },
                Priority = 85,
                Confidence = 0.9
            });
            
            rules.Add(new ProductionRule
            {
                Name = "for_statement",
                Alternatives = new List<string>
                {
                    "<FOR> <LPAREN> <for_init>? <SEMICOLON> <expression>? <SEMICOLON> <for_update>? <RPAREN> <statement>"
                },
                Priority = 85,
                Confidence = 0.8
            });
        }
        
        // Block statement
        rules.Add(new ProductionRule
        {
            Name = "block",
            Alternatives = new List<string>
            {
                "<LBRACE> <statement_list>? <RBRACE>"
            },
            Priority = 95,
            Confidence = 0.95
        });
        
        rules.Add(new ProductionRule
        {
            Name = "statement_list",
            Alternatives = new List<string>
            {
                "<statement>",
                "<statement_list> <statement>"
            },
            Priority = 90,
            Confidence = 0.9
        });
        
        rules.Add(new ProductionRule
        {
            Name = "statement",
            Alternatives = new List<string>
            {
                "<expression_statement>",
                "<variable_declaration>",
                "<if_statement>",
                "<while_statement>",
                "<for_statement>",
                "<return_statement>",
                "<block>"
            },
            Priority = 80,
            Confidence = 0.85
        });
        
        return rules;
    }

    private ProductionRules CombineIntoProductionRules(
        List<ProductionRule> functionPatterns,
        List<ProductionRule> declarationPatterns,
        List<ProductionRule> expressionRules,
        List<ProductionRule> statementRules)
    {
        var allRules = new List<ProductionRule>();
        
        // Add a top-level program rule
        allRules.Add(new ProductionRule
        {
            Name = "program",
            Alternatives = new List<string>
            {
                "<declaration_list>"
            },
            Priority = 100,
            Confidence = 0.95
        });
        
        allRules.Add(new ProductionRule
        {
            Name = "declaration_list",
            Alternatives = new List<string>
            {
                "<declaration>",
                "<declaration_list> <declaration>"
            },
            Priority = 95,
            Confidence = 0.9
        });
        
        allRules.Add(new ProductionRule
        {
            Name = "declaration",
            Alternatives = new List<string>
            {
                "<function_definition>",
                "<variable_declaration>"
            },
            Priority = 90,
            Confidence = 0.9
        });
        
        // Combine all rule sets
        allRules.AddRange(functionPatterns);
        allRules.AddRange(declarationPatterns);
        allRules.AddRange(expressionRules);
        allRules.AddRange(statementRules);
        
        return new ProductionRules(allRules);
    }
}