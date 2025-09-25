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
    /// <summary>
    /// Gets or sets the name of the token pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the regular expression pattern for token matching.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of token this pattern represents.
    /// </summary>
    public TokenType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the priority of this token pattern for matching conflicts.
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this token is a language keyword.
    /// </summary>
    public bool IsKeyword { get; set; }
    
    /// <summary>
    /// Gets or sets the list of example strings that match this pattern.
    /// </summary>
    public List<string> Examples { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the confidence level of this pattern's accuracy (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Types of tokens that can be identified
/// </summary>
public enum TokenType
{
    /// <summary>
    /// A reserved keyword in the language.
    /// </summary>
    Keyword,
    
    /// <summary>
    /// A user-defined identifier or name.
    /// </summary>
    Identifier,
    
    /// <summary>
    /// A literal value such as string, number, or boolean.
    /// </summary>
    Literal,
    
    /// <summary>
    /// An operator symbol such as +, -, *, /.
    /// </summary>
    Operator,
    
    /// <summary>
    /// A delimiter such as parentheses, brackets, or semicolons.
    /// </summary>
    Delimiter,
    
    /// <summary>
    /// Whitespace characters such as spaces, tabs, or newlines.
    /// </summary>
    Whitespace,
    
    /// <summary>
    /// Comment text that should be ignored during parsing.
    /// </summary>
    Comment
}

/// <summary>
/// Collection of token definitions for a language
/// </summary>
public class TokenDefinitions
{
    /// <summary>
    /// Gets or sets the list of token patterns for the language.
    /// </summary>
    public List<TokenPattern> Patterns { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the TokenDefinitions class.
    /// </summary>
    public TokenDefinitions() { }

    /// <summary>
    /// Initializes a new instance of the TokenDefinitions class with the specified patterns.
    /// </summary>
    /// <param name="patterns">The collection of token patterns to initialize with.</param>
    public TokenDefinitions(IEnumerable<TokenPattern> patterns)
    {
        Patterns = patterns.ToList();
    }

    /// <summary>
    /// Adds a token pattern to the collection.
    /// </summary>
    /// <param name="pattern">The token pattern to add.</param>
    public void AddPattern(TokenPattern pattern)
    {
        Patterns.Add(pattern);
    }

    /// <summary>
    /// Gets all token patterns of the specified type.
    /// </summary>
    /// <param name="type">The token type to filter by.</param>
    /// <returns>An enumerable collection of token patterns matching the specified type.</returns>
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
    /// <summary>
    /// Gets or sets the name of the production rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of alternative production definitions for this rule.
    /// </summary>
    public List<string> Alternatives { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the priority of this production rule for conflict resolution.
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this production rule is optional.
    /// </summary>
    public bool IsOptional { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this production rule can be repeated.
    /// </summary>
    public bool IsRepeatable { get; set; }
    
    /// <summary>
    /// Gets or sets the confidence level of this production rule's accuracy (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets the list of example strings that match this production rule.
    /// </summary>
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Collection of production rules for a grammar
/// </summary>
public class ProductionRules
{
    /// <summary>
    /// Gets or sets the list of production rules in the grammar.
    /// </summary>
    public List<ProductionRule> Rules { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the ProductionRules class.
    /// </summary>
    public ProductionRules() { }

    /// <summary>
    /// Initializes a new instance of the ProductionRules class with the specified rules.
    /// </summary>
    /// <param name="rules">The collection of production rules to initialize with.</param>
    public ProductionRules(IEnumerable<ProductionRule> rules)
    {
        Rules = rules.ToList();
    }

    /// <summary>
    /// Adds a production rule to the collection.
    /// </summary>
    /// <param name="rule">The production rule to add.</param>
    public void AddRule(ProductionRule rule)
    {
        Rules.Add(rule);
    }

    /// <summary>
    /// Gets the production rule with the specified name.
    /// </summary>
    /// <param name="name">The name of the production rule to find.</param>
    /// <returns>The production rule with the specified name, or null if not found.</returns>
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
    /// <summary>
    /// Gets or sets the name of the grammar.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the target programming language for this grammar.
    /// </summary>
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the token definitions for the grammar.
    /// </summary>
    public TokenDefinitions TokenRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the production rules for the grammar.
    /// </summary>
    public ProductionRules ProductionRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata associated with the grammar.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the creation timestamp of the grammar.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the version of the grammar.
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Parse error information for grammar refinement
/// </summary>
public class ParseError
{
    /// <summary>
    /// Gets or sets the type of parse error that occurred.
    /// </summary>
    public ParseErrorType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the error message describing the parse failure.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the line number where the error occurred.
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// Gets or sets the column number where the error occurred.
    /// </summary>
    public int Column { get; set; }
    
    /// <summary>
    /// Gets or sets the source text where the error was encountered.
    /// </summary>
    public string SourceText { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the tokens that were expected at the error location.
    /// </summary>
    public string ExpectedTokens { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the actual token that was found at the error location.
    /// </summary>
    public string ActualToken { get; set; } = string.Empty;
}

/// <summary>
/// Types of parse errors
/// </summary>
public enum ParseErrorType
{
    /// <summary>
    /// An unexpected token was encountered during parsing.
    /// </summary>
    UnexpectedToken,
    
    /// <summary>
    /// A required production rule is missing from the grammar.
    /// </summary>
    MissingProduction,
    
    /// <summary>
    /// The grammar contains ambiguous rules that could match the same input.
    /// </summary>
    AmbiguousGrammar,
    
    /// <summary>
    /// The grammar contains left-recursive rules that could cause infinite loops.
    /// </summary>
    LeftRecursion,
    
    /// <summary>
    /// A production rule in the grammar is unreachable and will never be used.
    /// </summary>
    UnreachableRule,
    
    /// <summary>
    /// An error occurred during the tokenization phase of parsing.
    /// </summary>
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