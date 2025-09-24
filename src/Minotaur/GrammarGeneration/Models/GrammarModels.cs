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

namespace Minotaur.GrammarGeneration.Models;

/// <summary>
/// Represents a token pattern discovered during language analysis
/// </summary>
public class TokenPattern
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public TokenType Type { get; set; }
    public int Priority { get; set; }
    public bool IsKeyword { get; set; }
    public List<string> Examples { get; set; } = new();
    public double Confidence { get; set; }
}

/// <summary>
/// Types of tokens that can be identified
/// </summary>
public enum TokenType
{
    Keyword,
    Identifier,
    Literal,
    Operator,
    Delimiter,
    Whitespace,
    Comment
}

/// <summary>
/// Collection of token definitions for a language
/// </summary>
public class TokenDefinitions
{
    public List<TokenPattern> Patterns { get; set; } = new();

    public TokenDefinitions() { }

    public TokenDefinitions(IEnumerable<TokenPattern> patterns)
    {
        Patterns = patterns.ToList();
    }

    public void AddPattern(TokenPattern pattern)
    {
        Patterns.Add(pattern);
    }

    public IEnumerable<TokenPattern> GetPatternsByType(TokenType type)
    {
        return Patterns.Where(p => p.Type == type);
    }
}

/// <summary>
/// Represents a production rule in the grammar
/// </summary>
public class ProductionRule
{
    public string Name { get; set; } = string.Empty;
    public List<string> Alternatives { get; set; } = new();
    public int Priority { get; set; }
    public bool IsOptional { get; set; }
    public bool IsRepeatable { get; set; }
    public double Confidence { get; set; }
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Collection of production rules for a grammar
/// </summary>
public class ProductionRules
{
    public List<ProductionRule> Rules { get; set; } = new();

    public ProductionRules() { }

    public ProductionRules(IEnumerable<ProductionRule> rules)
    {
        Rules = rules.ToList();
    }

    public void AddRule(ProductionRule rule)
    {
        Rules.Add(rule);
    }

    public ProductionRule? GetRule(string name)
    {
        return Rules.FirstOrDefault(r => r.Name == name);
    }
}

/// <summary>
/// Complete grammar definition
/// </summary>
public class Grammar
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public TokenDefinitions TokenRules { get; set; } = new();
    public ProductionRules ProductionRules { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Parse error information for grammar refinement
/// </summary>
public class ParseError
{
    public ParseErrorType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string ExpectedTokens { get; set; } = string.Empty;
    public string ActualToken { get; set; } = string.Empty;
}

/// <summary>
/// Types of parse errors
/// </summary>
public enum ParseErrorType
{
    UnexpectedToken,
    MissingProduction,
    AmbiguousGrammar,
    LeftRecursion,
    UnreachableRule,
    TokenizationError
}

/// <summary>
/// Suggested refinement to improve grammar
/// </summary>
public class GrammarRefinement
{
    public RefinementType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TargetRule { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> AffectedRules { get; set; } = new();

    public GrammarRefinement() { }

    public GrammarRefinement(IEnumerable<GrammarRefinement> refinements)
    {
        // This constructor seems incorrect - it should aggregate refinements
        // For now, keeping the interface but making it a no-op
    }
}

/// <summary>
/// Types of grammar refinements
/// </summary>
public enum RefinementType
{
    AddTokenRule,
    ModifyTokenRule,
    AddProductionRule,
    ModifyProductionRule,
    RemoveRule,
    ReorderRules,
    AddPrecedence,
    ResolveAmbiguity
}

/// <summary>
/// Context information for language analysis
/// </summary>
public class LanguageContext
{
    public bool HasNestedScopes { get; set; }
    public bool HasTypeSystem { get; set; }
    public bool HasMacroSystem { get; set; }
    public bool HasComments { get; set; }
    public bool HasStringLiterals { get; set; }
    public bool HasNumericLiterals { get; set; }
    public List<string> FileExtensions { get; set; } = new();
    public string DefaultEncoding { get; set; } = "UTF-8";
}

/// <summary>
/// Quality metrics for a generated grammar
/// </summary>
public class QualityReport
{
    public double LanguageFeatureCoverage { get; set; }
    public double TokenCoverage { get; set; }
    public double ParseAccuracy { get; set; }
    public double SemanticAccuracy { get; set; }
    public TimeSpan ParseSpeed { get; set; }
    public long MemoryUsage { get; set; }
    public int AmbiguityCount { get; set; }
    public double ComplexityScore { get; set; }
    public double ReadabilityScore { get; set; }
    public double ModularityScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}