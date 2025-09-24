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

using Minotaur.GrammarGeneration.Models;

namespace Minotaur.GrammarGeneration.Validation;

/// <summary>
/// Validates generated grammars for correctness and quality
/// </summary>
public class GrammarValidator
{
    /// <summary>
    /// Validate a grammar against source files
    /// </summary>
    public async Task<GrammarValidationResult> ValidateGrammarAsync(Grammar grammar, string[] sourceFiles)
    {
        var result = new GrammarValidationResult
        {
            Grammar = grammar,
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string>(),
            QualityReport = new QualityReport()
        };

        // 1. Structural validation
        await ValidateStructure(grammar, result);

        // 2. Consistency validation
        await ValidateConsistency(grammar, result);

        // 3. Coverage validation against source files
        await ValidateCoverage(grammar, sourceFiles, result);

        // 4. Performance validation
        await ValidatePerformance(grammar, result);

        // 5. Generate quality metrics
        result.QualityReport = await GenerateQualityReport(grammar, sourceFiles);

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private async Task ValidateStructure(Grammar grammar, GrammarValidationResult result)
    {
        // Check for required elements
        if (string.IsNullOrEmpty(grammar.Name))
        {
            result.Errors.Add("Grammar must have a name");
        }

        if (!grammar.ProductionRules.Rules.Any())
        {
            result.Errors.Add("Grammar must have at least one production rule");
        }

        // Check for start rule
        var hasStartRule = grammar.ProductionRules.Rules.Any(r => 
            r.Name == "program" || r.Name == "start" || r.Name == "compilation_unit");
        
        if (!hasStartRule)
        {
            result.Warnings.Add("Grammar should have a start rule (program, start, or compilation_unit)");
        }

        // Check for token rules
        if (!grammar.TokenRules.Patterns.Any())
        {
            result.Warnings.Add("Grammar has no token rules defined");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateConsistency(Grammar grammar, GrammarValidationResult result)
    {
        var definedTokens = new HashSet<string>(grammar.TokenRules.Patterns.Select(p => p.Name));
        var definedRules = new HashSet<string>(grammar.ProductionRules.Rules.Select(r => r.Name));
        var referencedTokens = new HashSet<string>();
        var referencedRules = new HashSet<string>();

        // Extract all referenced tokens and rules
        foreach (var rule in grammar.ProductionRules.Rules)
        {
            foreach (var alternative in rule.Alternatives)
            {
                ExtractReferences(alternative, referencedTokens, referencedRules);
            }
        }

        // Check for undefined references
        foreach (var token in referencedTokens)
        {
            if (!definedTokens.Contains(token) && !IsBuiltInToken(token))
            {
                result.Errors.Add($"Referenced token '{token}' is not defined");
            }
        }

        foreach (var rule in referencedRules)
        {
            if (!definedRules.Contains(rule))
            {
                result.Errors.Add($"Referenced rule '{rule}' is not defined");
            }
        }

        // Check for unused definitions
        foreach (var token in definedTokens)
        {
            if (!referencedTokens.Contains(token))
            {
                result.Warnings.Add($"Token '{token}' is defined but never used");
            }
        }

        foreach (var rule in definedRules)
        {
            if (!referencedRules.Contains(rule) && !IsStartRule(rule))
            {
                result.Warnings.Add($"Rule '{rule}' is defined but never used");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ValidateCoverage(Grammar grammar, string[] sourceFiles, GrammarValidationResult result)
    {
        var totalLines = 0;
        var coveredLines = 0;

        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(sourceFile);
                totalLines += lines.Length;

                // Simple coverage check - see if we can identify language constructs
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || IsComment(line))
                    {
                        coveredLines++; // Comments and whitespace are always "covered"
                        continue;
                    }

                    if (HasIdentifiablePattern(line, grammar))
                    {
                        coveredLines++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Could not read source file '{sourceFile}': {ex.Message}");
            }
        }

        var coverage = totalLines > 0 ? (double)coveredLines / totalLines : 1.0;
        
        if (coverage < 0.8)
        {
            result.Warnings.Add($"Low grammar coverage: {coverage:P1} of source code is recognized");
        }
    }

    private async Task ValidatePerformance(Grammar grammar, GrammarValidationResult result)
    {
        // Check for potential performance issues
        var complexRules = grammar.ProductionRules.Rules
            .Where(r => r.Alternatives.Count > 10)
            .ToList();

        if (complexRules.Any())
        {
            result.Warnings.Add($"Rules with many alternatives may impact performance: {string.Join(", ", complexRules.Select(r => r.Name))}");
        }

        // Check for left recursion
        var leftRecursiveRules = FindLeftRecursiveRules(grammar);
        if (leftRecursiveRules.Any())
        {
            result.Errors.Add($"Left recursive rules detected: {string.Join(", ", leftRecursiveRules)}");
        }

        await Task.CompletedTask;
    }

    private async Task<QualityReport> GenerateQualityReport(Grammar grammar, string[] sourceFiles)
    {
        var report = new QualityReport();

        // Calculate feature coverage
        var features = new[] { "variables", "functions", "classes", "control_flow", "expressions" };
        var coveredFeatures = features.Count(feature => 
            grammar.ProductionRules.Rules.Any(r => r.Name.Contains(feature)));
        
        report.LanguageFeatureCoverage = (double)coveredFeatures / features.Length;

        // Calculate token coverage
        var expectedTokenTypes = Enum.GetValues<TokenType>().Length;
        var actualTokenTypes = grammar.TokenRules.Patterns.Select(p => p.Type).Distinct().Count();
        report.TokenCoverage = (double)actualTokenTypes / expectedTokenTypes;

        // Estimate complexity
        report.ComplexityScore = CalculateComplexityScore(grammar);

        // Estimate readability
        report.ReadabilityScore = CalculateReadabilityScore(grammar);

        // Other metrics
        report.ParseAccuracy = 0.85; // Placeholder - would need actual parsing tests
        report.SemanticAccuracy = 0.80; // Placeholder
        report.ParseSpeed = TimeSpan.FromMilliseconds(100); // Placeholder
        report.MemoryUsage = 1024 * 1024; // 1MB placeholder
        report.AmbiguityCount = CountAmbiguities(grammar);
        report.ModularityScore = CalculateModularityScore(grammar);

        return await Task.FromResult(report);
    }

    private void ExtractReferences(string alternative, HashSet<string> tokens, HashSet<string> rules)
    {
        // Simple reference extraction using regex
        var tokenPattern = @"<([A-Z_][A-Z0-9_]*)>";
        var rulePattern = @"<([a-z_][a-z0-9_]*)>";

        var tokenMatches = System.Text.RegularExpressions.Regex.Matches(alternative, tokenPattern);
        var ruleMatches = System.Text.RegularExpressions.Regex.Matches(alternative, rulePattern);

        foreach (System.Text.RegularExpressions.Match match in tokenMatches)
        {
            tokens.Add(match.Groups[1].Value);
        }

        foreach (System.Text.RegularExpressions.Match match in ruleMatches)
        {
            rules.Add(match.Groups[1].Value);
        }
    }

    private bool IsBuiltInToken(string token)
    {
        var builtInTokens = new[] { "IDENTIFIER", "NUMBER", "STRING", "WHITESPACE", "EOF" };
        return builtInTokens.Contains(token);
    }

    private bool IsStartRule(string ruleName)
    {
        var startRules = new[] { "program", "start", "compilation_unit", "file_input" };
        return startRules.Contains(ruleName);
    }

    private bool IsComment(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("#");
    }

    private bool HasIdentifiablePattern(string line, Grammar grammar)
    {
        // Check if the line contains patterns that match our grammar rules
        var trimmed = line.Trim();

        // Check against token patterns
        foreach (var token in grammar.TokenRules.Patterns)
        {
            if (token.Type == TokenType.Keyword && trimmed.Contains(token.Name.ToLower()))
            {
                return true;
            }
        }

        // Check against common programming constructs
        var patterns = new[]
        {
            @"\b(if|while|for|function|class|var|let|const)\b",
            @"\w+\s*=\s*\w+", // assignment
            @"\w+\s*\([^)]*\)", // function call
            @"\{|\}|\(|\)|\[|\]" // structural elements
        };

        return patterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(trimmed, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    private List<string> FindLeftRecursiveRules(Grammar grammar)
    {
        var leftRecursive = new List<string>();

        foreach (var rule in grammar.ProductionRules.Rules)
        {
            foreach (var alternative in rule.Alternatives)
            {
                if (alternative.TrimStart().StartsWith($"<{rule.Name}>"))
                {
                    leftRecursive.Add(rule.Name);
                    break;
                }
            }
        }

        return leftRecursive;
    }

    private double CalculateComplexityScore(Grammar grammar)
    {
        var totalComplexity = 0.0;
        var ruleCount = grammar.ProductionRules.Rules.Count;

        foreach (var rule in grammar.ProductionRules.Rules)
        {
            // Simple complexity calculation based on alternatives and nesting
            totalComplexity += rule.Alternatives.Count * 0.1;
            
            foreach (var alternative in rule.Alternatives)
            {
                var nesting = alternative.Count(c => c == '<');
                totalComplexity += nesting * 0.05;
            }
        }

        return ruleCount > 0 ? totalComplexity / ruleCount : 0.0;
    }

    private double CalculateReadabilityScore(Grammar grammar)
    {
        var score = 1.0;

        // Penalize very long rule names
        var averageNameLength = grammar.ProductionRules.Rules.Average(r => r.Name.Length);
        if (averageNameLength > 20)
        {
            score -= 0.1;
        }

        // Reward consistent naming conventions
        var snakeCaseCount = grammar.ProductionRules.Rules.Count(r => r.Name.Contains('_'));
        var consistencyRatio = (double)snakeCaseCount / grammar.ProductionRules.Rules.Count;
        
        if (consistencyRatio > 0.8 || consistencyRatio < 0.2)
        {
            score += 0.1; // Consistent naming (either mostly snake_case or mostly not)
        }

        return Math.Max(0.0, Math.Min(1.0, score));
    }

    private int CountAmbiguities(Grammar grammar)
    {
        var ambiguities = 0;

        foreach (var rule in grammar.ProductionRules.Rules)
        {
            if (rule.Alternatives.Count > 1)
            {
                // Simple ambiguity detection
                for (int i = 0; i < rule.Alternatives.Count; i++)
                {
                    for (int j = i + 1; j < rule.Alternatives.Count; j++)
                    {
                        if (MightBeAmbiguous(rule.Alternatives[i], rule.Alternatives[j]))
                        {
                            ambiguities++;
                        }
                    }
                }
            }
        }

        return ambiguities;
    }

    private bool MightBeAmbiguous(string alt1, string alt2)
    {
        // Very simple ambiguity check - see if they start with the same token
        var tokens1 = alt1.Trim().Split(' ');
        var tokens2 = alt2.Trim().Split(' ');

        return tokens1.Length > 0 && tokens2.Length > 0 && tokens1[0] == tokens2[0];
    }

    private double CalculateModularityScore(Grammar grammar)
    {
        // Simple modularity calculation based on rule reuse
        var referenceCounts = new Dictionary<string, int>();

        foreach (var rule in grammar.ProductionRules.Rules)
        {
            foreach (var alternative in rule.Alternatives)
            {
                var references = System.Text.RegularExpressions.Regex.Matches(alternative, @"<(\w+)>");
                foreach (System.Text.RegularExpressions.Match match in references)
                {
                    var refName = match.Groups[1].Value;
                    referenceCounts[refName] = referenceCounts.GetValueOrDefault(refName, 0) + 1;
                }
            }
        }

        // Rules that are referenced multiple times indicate good modularity
        var wellReusedRules = referenceCounts.Values.Count(count => count > 2);
        var totalRules = grammar.ProductionRules.Rules.Count;

        return totalRules > 0 ? (double)wellReusedRules / totalRules : 0.0;
    }
}

/// <summary>
/// Result of grammar validation
/// </summary>
public class GrammarValidationResult
{
    public Grammar Grammar { get; set; } = new();
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public QualityReport QualityReport { get; set; } = new();
}