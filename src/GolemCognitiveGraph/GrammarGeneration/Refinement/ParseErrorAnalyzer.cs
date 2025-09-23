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

using GolemCognitiveGraph.GrammarGeneration.Models;

namespace GolemCognitiveGraph.GrammarGeneration.Refinement;

/// <summary>
/// Analyzes parse errors to suggest grammar improvements
/// </summary>
public class ParseErrorAnalyzer
{
    private readonly Dictionary<string, int> _errorFrequency = new();
    private readonly Dictionary<ParseErrorType, List<ParseError>> _errorsByType = new();

    public GrammarRefinement AnalyzeParseErrors(ParseError[] errors, Grammar currentGrammar)
    {
        var refinements = new List<GrammarRefinement>();
        
        // Categorize errors by type
        CategorizeErrors(errors);
        
        foreach (var error in errors)
        {
            _errorFrequency[error.Message] = _errorFrequency.GetValueOrDefault(error.Message, 0) + 1;
            
            switch (error.Type)
            {
                case ParseErrorType.UnexpectedToken:
                    var tokenRefinement = SuggestTokenRefinement(error, currentGrammar);
                    if (tokenRefinement != null)
                        refinements.Add(tokenRefinement);
                    break;
                    
                case ParseErrorType.MissingProduction:
                    var productionRefinement = SuggestProductionAddition(error, currentGrammar);
                    if (productionRefinement != null)
                        refinements.Add(productionRefinement);
                    break;
                    
                case ParseErrorType.AmbiguousGrammar:
                    var ambiguityRefinement = SuggestDisambiguation(error, currentGrammar);
                    if (ambiguityRefinement != null)
                        refinements.Add(ambiguityRefinement);
                    break;
                    
                case ParseErrorType.LeftRecursion:
                    var recursionRefinement = SuggestLeftRecursionFix(error, currentGrammar);
                    if (recursionRefinement != null)
                        refinements.Add(recursionRefinement);
                    break;
                    
                case ParseErrorType.TokenizationError:
                    var tokenizationRefinement = SuggestTokenizationFix(error, currentGrammar);
                    if (tokenizationRefinement != null)
                        refinements.Add(tokenizationRefinement);
                    break;
            }
        }
        
        // Return the most confident refinement for now
        // In a full implementation, we'd return an aggregate of all refinements
        return refinements.OrderByDescending(r => r.Confidence).FirstOrDefault() ?? new GrammarRefinement();
    }

    private void CategorizeErrors(ParseError[] errors)
    {
        _errorsByType.Clear();
        
        foreach (var error in errors)
        {
            if (!_errorsByType.ContainsKey(error.Type))
            {
                _errorsByType[error.Type] = new List<ParseError>();
            }
            _errorsByType[error.Type].Add(error);
        }
    }

    private GrammarRefinement? SuggestTokenRefinement(ParseError error, Grammar grammar)
    {
        // Analyze the unexpected token and suggest fixes
        var actualToken = error.ActualToken;
        var expectedTokens = error.ExpectedTokens;
        
        if (string.IsNullOrEmpty(actualToken))
            return null;
        
        // Check if the token might be a new keyword or operator
        if (IsLikelyNewKeyword(actualToken))
        {
            return new GrammarRefinement
            {
                Type = RefinementType.AddTokenRule,
                Description = $"Add new keyword token for '{actualToken}'",
                TargetRule = "keywords",
                Suggestion = $"<{actualToken.ToUpperInvariant()}> ::= \"{actualToken}\"",
                Confidence = CalculateKeywordConfidence(actualToken),
                AffectedRules = new List<string> { "keywords", "identifier" }
            };
        }
        
        if (IsLikelyNewOperator(actualToken))
        {
            return new GrammarRefinement
            {
                Type = RefinementType.AddTokenRule,
                Description = $"Add new operator token for '{actualToken}'",
                TargetRule = "operators",
                Suggestion = $"<OP_{SanitizeTokenName(actualToken)}> ::= \"{actualToken}\"",
                Confidence = CalculateOperatorConfidence(actualToken),
                AffectedRules = new List<string> { "operators", "expression" }
            };
        }
        
        // Check if existing token pattern needs modification
        var existingToken = FindSimilarToken(actualToken, grammar);
        if (existingToken != null)
        {
            return new GrammarRefinement
            {
                Type = RefinementType.ModifyTokenRule,
                Description = $"Modify token pattern to include '{actualToken}'",
                TargetRule = existingToken.Name,
                Suggestion = $"Update pattern: {existingToken.Pattern} to include {actualToken}",
                Confidence = 0.6,
                AffectedRules = new List<string> { existingToken.Name }
            };
        }
        
        return null;
    }

    private GrammarRefinement? SuggestProductionAddition(ParseError error, Grammar grammar)
    {
        var sourceContext = ExtractContext(error.SourceText, error.Line, error.Column);
        
        // Try to infer what production rule might be missing
        var missingPattern = InferMissingPattern(sourceContext, error);
        
        if (!string.IsNullOrEmpty(missingPattern))
        {
            return new GrammarRefinement
            {
                Type = RefinementType.AddProductionRule,
                Description = $"Add missing production rule for pattern: {missingPattern}",
                TargetRule = "statements",
                Suggestion = $"<{missingPattern}_statement> ::= {GenerateProductionSuggestion(missingPattern)}",
                Confidence = 0.7,
                AffectedRules = new List<string> { "statements", missingPattern + "_statement" }
            };
        }
        
        return null;
    }

    private GrammarRefinement? SuggestDisambiguation(ParseError error, Grammar grammar)
    {
        // Find ambiguous rules and suggest disambiguation
        var ambiguousRules = FindAmbiguousRules(grammar);
        
        if (ambiguousRules.Any())
        {
            var rule = ambiguousRules.First();
            return new GrammarRefinement
            {
                Type = RefinementType.ResolveAmbiguity,
                Description = $"Resolve ambiguity in rule '{rule.Name}'",
                TargetRule = rule.Name,
                Suggestion = $"Add precedence declarations or reorder alternatives in {rule.Name}",
                Confidence = 0.5,
                AffectedRules = new List<string> { rule.Name }
            };
        }
        
        return null;
    }

    private GrammarRefinement? SuggestLeftRecursionFix(ParseError error, Grammar grammar)
    {
        var recursiveRule = FindLeftRecursiveRule(error, grammar);
        
        if (recursiveRule != null)
        {
            return new GrammarRefinement
            {
                Type = RefinementType.ModifyProductionRule,
                Description = $"Fix left recursion in rule '{recursiveRule.Name}'",
                TargetRule = recursiveRule.Name,
                Suggestion = $"Convert {recursiveRule.Name} to right recursion or use iteration",
                Confidence = 0.8,
                AffectedRules = new List<string> { recursiveRule.Name }
            };
        }
        
        return null;
    }

    private GrammarRefinement? SuggestTokenizationFix(ParseError error, Grammar grammar)
    {
        return new GrammarRefinement
        {
            Type = RefinementType.ModifyTokenRule,
            Description = "Fix tokenization issue",
            TargetRule = "lexer",
            Suggestion = "Review token patterns for conflicts or missing patterns",
            Confidence = 0.4,
            AffectedRules = new List<string> { "lexer" }
        };
    }

    private bool IsLikelyNewKeyword(string token)
    {
        // Keywords are typically:
        // - All lowercase
        // - Alphabetic characters only
        // - Not too long
        // - Appear in contexts where keywords are expected
        return token.All(char.IsLetter) && 
               token.All(char.IsLower) && 
               token.Length >= 2 && 
               token.Length <= 12 &&
               !CommonIdentifierPrefixes.Any(prefix => token.StartsWith(prefix));
    }

    private bool IsLikelyNewOperator(string token)
    {
        // Operators typically contain special characters
        return token.Any(c => "+-*/%=<>!&|^~".Contains(c)) && 
               token.Length >= 1 && 
               token.Length <= 3;
    }

    private double CalculateKeywordConfidence(string token)
    {
        var baseConfidence = 0.5;
        
        // Increase confidence based on frequency
        var frequency = _errorFrequency.GetValueOrDefault($"Unexpected token '{token}'", 0);
        baseConfidence += Math.Min(0.3, frequency * 0.1);
        
        // Increase confidence if it looks like a common keyword pattern
        if (CommonKeywordPatterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(token, pattern)))
        {
            baseConfidence += 0.2;
        }
        
        return Math.Min(0.9, baseConfidence);
    }

    private double CalculateOperatorConfidence(string token)
    {
        var baseConfidence = 0.6;
        
        // Common operator patterns get higher confidence
        if (CommonOperators.Contains(token))
        {
            baseConfidence = 0.8;
        }
        
        return baseConfidence;
    }

    private TokenPattern? FindSimilarToken(string token, Grammar grammar)
    {
        return grammar.TokenRules.Patterns
            .Where(p => p.Type == TokenType.Identifier || p.Type == TokenType.Operator)
            .OrderBy(p => LevenshteinDistance(token, p.Name))
            .FirstOrDefault();
    }

    private string ExtractContext(string sourceText, int line, int column)
    {
        var lines = sourceText.Split('\n');
        if (line > 0 && line <= lines.Length)
        {
            return lines[line - 1].Trim();
        }
        return string.Empty;
    }

    private string InferMissingPattern(string context, ParseError error)
    {
        // Simple pattern inference based on common structures
        if (context.Contains("import") || context.Contains("include"))
            return "import";
        if (context.Contains("class") && context.Contains("{"))
            return "class_definition";
        if (context.Contains("=>") || context.Contains("->"))
            return "lambda";
        
        return string.Empty;
    }

    private string GenerateProductionSuggestion(string pattern)
    {
        return pattern switch
        {
            "import" => "<IMPORT> <identifier> <FROM>? <string_literal>",
            "class_definition" => "<CLASS> <identifier> <class_body>",
            "lambda" => "<lambda_params> <ARROW> <expression>",
            _ => "<identifier> <expression>*"
        };
    }

    private List<ProductionRule> FindAmbiguousRules(Grammar grammar)
    {
        // Simple ambiguity detection - look for rules with overlapping patterns
        var ambiguous = new List<ProductionRule>();
        
        foreach (var rule in grammar.ProductionRules.Rules)
        {
            if (rule.Alternatives.Count > 1)
            {
                // Check if alternatives might conflict
                var hasConflict = rule.Alternatives
                    .SelectMany((alt1, i) => rule.Alternatives.Skip(i + 1), (alt1, alt2) => new { alt1, alt2 })
                    .Any(pair => MightConflict(pair.alt1, pair.alt2));
                
                if (hasConflict)
                {
                    ambiguous.Add(rule);
                }
            }
        }
        
        return ambiguous;
    }

    private bool MightConflict(string alt1, string alt2)
    {
        // Simple conflict detection - check if they start with the same token
        var tokens1 = alt1.Split(' ');
        var tokens2 = alt2.Split(' ');
        
        return tokens1.Length > 0 && tokens2.Length > 0 && tokens1[0] == tokens2[0];
    }

    private ProductionRule? FindLeftRecursiveRule(ParseError error, Grammar grammar)
    {
        // Look for rules that might be left-recursive
        return grammar.ProductionRules.Rules
            .FirstOrDefault(rule => rule.Alternatives.Any(alt => alt.TrimStart().StartsWith($"<{rule.Name}>")));
    }

    private string SanitizeTokenName(string token)
    {
        return token.Replace("+", "PLUS")
                   .Replace("-", "MINUS")
                   .Replace("*", "MULT")
                   .Replace("/", "DIV")
                   .Replace("=", "EQ")
                   .Replace("<", "LT")
                   .Replace(">", "GT")
                   .Replace("!", "NOT")
                   .Replace("&", "AND")
                   .Replace("|", "OR");
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }
        
        return matrix[s1.Length, s2.Length];
    }

    private static readonly HashSet<string> CommonIdentifierPrefixes = new()
    {
        "temp", "tmp", "var", "val", "arg", "param", "obj", "item", "elem"
    };

    private static readonly string[] CommonKeywordPatterns = new[]
    {
        @"^(if|else|while|for|do|try|catch|finally|return|break|continue)$",
        @"^(class|struct|interface|enum|namespace)$",
        @"^(public|private|protected|static|const|final)$"
    };

    private static readonly HashSet<string> CommonOperators = new()
    {
        "+", "-", "*", "/", "%", "=", "==", "!=", "<", ">", "<=", ">=", 
        "&&", "||", "!", "&", "|", "^", "~", "<<", ">>", "++", "--",
        "+=", "-=", "*=", "/=", "%=", "?:", "??"
    };
}