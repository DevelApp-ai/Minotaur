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
using GolemCognitiveGraph.GrammarGeneration.Models;

namespace GolemCognitiveGraph.GrammarGeneration.Analysis;

/// <summary>
/// Analyzes source code to identify token patterns
/// </summary>
public class TokenPatternAnalyzer
{
    private readonly Dictionary<string, int> _tokenFrequency = new();
    private readonly HashSet<string> _potentialKeywords = new();
    private readonly List<string> _identifierPatterns = new();
    private readonly List<string> _literalPatterns = new();
    private readonly List<string> _operatorPatterns = new();

    public TokenDefinitions AnalyzeSourceCode(string[] sourceFiles)
    {
        var patterns = new List<TokenPattern>();
        
        // Analyze all source files
        foreach (var sourceFile in sourceFiles)
        {
            AnalyzeFile(sourceFile);
        }
        
        // 1. Identify literals and operators
        patterns.AddRange(ExtractLiteralPatterns());
        patterns.AddRange(ExtractOperatorPatterns());
        
        // 2. Identify keywords vs identifiers
        patterns.AddRange(AnalyzeKeywordPatterns());
        
        // 3. Identify structural patterns (brackets, delimiters)
        patterns.AddRange(ExtractStructuralPatterns());
        
        // 4. Identify comments and whitespace
        patterns.AddRange(ExtractCommentPatterns());
        patterns.AddRange(ExtractWhitespacePatterns());
        
        return new TokenDefinitions(patterns);
    }

    private void AnalyzeFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            
            // Tokenize basic patterns
            var tokens = BasicTokenize(content);
            
            foreach (var token in tokens)
            {
                _tokenFrequency[token] = _tokenFrequency.GetValueOrDefault(token, 0) + 1;
                
                // Classify token type
                if (IsIdentifierPattern(token))
                {
                    _identifierPatterns.Add(token);
                    
                    // Check if it might be a keyword (appears frequently, follows naming conventions)
                    if (_tokenFrequency[token] > 3 && IsKeywordCandidate(token))
                    {
                        _potentialKeywords.Add(token);
                    }
                }
                else if (IsLiteralPattern(token))
                {
                    _literalPatterns.Add(token);
                }
                else if (IsOperatorPattern(token))
                {
                    _operatorPatterns.Add(token);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue processing other files
            Console.WriteLine($"Error analyzing file {filePath}: {ex.Message}");
        }
    }

    private List<string> BasicTokenize(string content)
    {
        var tokens = new List<string>();
        
        // Simple tokenization - split by common delimiters but preserve them
        var regex = new Regex(@"(\w+|[^\w\s]|\s+)", RegexOptions.Compiled);
        var matches = regex.Matches(content);
        
        foreach (Match match in matches)
        {
            var token = match.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                tokens.Add(token.Trim());
            }
        }
        
        return tokens.Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    private bool IsIdentifierPattern(string token)
    {
        return Regex.IsMatch(token, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    private bool IsLiteralPattern(string token)
    {
        return Regex.IsMatch(token, @"^(\d+(\.\d+)?|""[^""]*""|'[^']*')$");
    }

    private bool IsOperatorPattern(string token)
    {
        var operators = new[] { "+", "-", "*", "/", "=", "==", "!=", "<", ">", "<=", ">=", "&&", "||", "!", "&", "|", "^", "~", "<<", ">>", "++", "--", "+=", "-=", "*=", "/=" };
        return operators.Contains(token);
    }

    private bool IsKeywordCandidate(string token)
    {
        // Keywords are usually:
        // - All lowercase or have specific patterns
        // - Appear frequently
        // - Have specific semantic meaning
        return token.All(char.IsLower) || 
               Regex.IsMatch(token, @"^[a-z]+(_[a-z]+)*$") ||
               CommonKeywords.Contains(token.ToLower());
    }

    private static readonly HashSet<string> CommonKeywords = new()
    {
        "if", "else", "while", "for", "do", "break", "continue", "return", "function", "class", "struct",
        "public", "private", "protected", "static", "const", "var", "let", "def", "lambda", "try", "catch",
        "finally", "throw", "new", "delete", "this", "super", "null", "undefined", "true", "false"
    };

    private List<TokenPattern> ExtractLiteralPatterns()
    {
        var patterns = new List<TokenPattern>();
        
        // String literals
        patterns.Add(new TokenPattern
        {
            Name = "STRING_LITERAL",
            Pattern = @"""[^""]*""",
            Type = TokenType.Literal,
            Priority = 100,
            Confidence = 0.9,
            Examples = _literalPatterns.Where(p => p.StartsWith("\"")).Take(5).ToList()
        });
        
        // Numeric literals
        patterns.Add(new TokenPattern
        {
            Name = "NUMBER_LITERAL",
            Pattern = @"\d+(\.\d+)?",
            Type = TokenType.Literal,
            Priority = 100,
            Confidence = 0.9,
            Examples = _literalPatterns.Where(p => Regex.IsMatch(p, @"^\d")).Take(5).ToList()
        });
        
        return patterns;
    }

    private List<TokenPattern> ExtractOperatorPatterns()
    {
        var patterns = new List<TokenPattern>();
        var operators = _operatorPatterns.Distinct().OrderByDescending(op => _tokenFrequency.GetValueOrDefault(op, 0));
        
        foreach (var op in operators.Take(20)) // Top 20 most common operators
        {
            patterns.Add(new TokenPattern
            {
                Name = $"OP_{op.Replace("+", "PLUS").Replace("-", "MINUS").Replace("*", "MULT").Replace("/", "DIV").Replace("=", "EQ")}",
                Pattern = Regex.Escape(op),
                Type = TokenType.Operator,
                Priority = 90,
                Confidence = 0.8,
                Examples = new List<string> { op }
            });
        }
        
        return patterns;
    }

    private List<TokenPattern> AnalyzeKeywordPatterns()
    {
        var patterns = new List<TokenPattern>();
        
        // Identify keywords by frequency and common patterns
        var keywordCandidates = _potentialKeywords
            .Where(kw => _tokenFrequency[kw] >= 2) // Appears at least twice
            .OrderByDescending(kw => _tokenFrequency[kw])
            .Take(50); // Top 50 keyword candidates
        
        foreach (var keyword in keywordCandidates)
        {
            patterns.Add(new TokenPattern
            {
                Name = keyword.ToUpperInvariant(),
                Pattern = $@"\b{Regex.Escape(keyword)}\b",
                Type = TokenType.Keyword,
                IsKeyword = true,
                Priority = 110, // Higher priority than identifiers
                Confidence = Math.Min(0.9, _tokenFrequency[keyword] * 0.1),
                Examples = new List<string> { keyword }
            });
        }
        
        // General identifier pattern
        patterns.Add(new TokenPattern
        {
            Name = "IDENTIFIER",
            Pattern = @"[a-zA-Z_][a-zA-Z0-9_]*",
            Type = TokenType.Identifier,
            Priority = 50,
            Confidence = 0.95,
            Examples = _identifierPatterns.Where(id => !_potentialKeywords.Contains(id)).Take(5).ToList()
        });
        
        return patterns;
    }

    private List<TokenPattern> ExtractStructuralPatterns()
    {
        var patterns = new List<TokenPattern>();
        
        var structuralTokens = new Dictionary<string, string>
        {
            { "(", "LPAREN" },
            { ")", "RPAREN" },
            { "{", "LBRACE" },
            { "}", "RBRACE" },
            { "[", "LBRACKET" },
            { "]", "RBRACKET" },
            { ";", "SEMICOLON" },
            { ",", "COMMA" },
            { ".", "DOT" },
            { ":", "COLON" }
        };
        
        foreach (var (token, name) in structuralTokens)
        {
            if (_tokenFrequency.ContainsKey(token))
            {
                patterns.Add(new TokenPattern
                {
                    Name = name,
                    Pattern = Regex.Escape(token),
                    Type = TokenType.Delimiter,
                    Priority = 95,
                    Confidence = 0.95,
                    Examples = new List<string> { token }
                });
            }
        }
        
        return patterns;
    }

    private List<TokenPattern> ExtractCommentPatterns()
    {
        var patterns = new List<TokenPattern>();
        
        // Common comment patterns
        patterns.Add(new TokenPattern
        {
            Name = "LINE_COMMENT",
            Pattern = @"//[^\r\n]*",
            Type = TokenType.Comment,
            Priority = 120,
            Confidence = 0.8,
            Examples = new List<string> { "// comment" }
        });
        
        patterns.Add(new TokenPattern
        {
            Name = "BLOCK_COMMENT",
            Pattern = @"/\*.*?\*/",
            Type = TokenType.Comment,
            Priority = 120,
            Confidence = 0.8,
            Examples = new List<string> { "/* comment */" }
        });
        
        return patterns;
    }

    private List<TokenPattern> ExtractWhitespacePatterns()
    {
        var patterns = new List<TokenPattern>();
        
        patterns.Add(new TokenPattern
        {
            Name = "WHITESPACE",
            Pattern = @"[ \t\r\n]+",
            Type = TokenType.Whitespace,
            Priority = 10,
            Confidence = 0.99,
            Examples = new List<string> { " ", "\t", "\n" }
        });
        
        return patterns;
    }
}