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

namespace Minotaur.Core.Models.Grammar;

/// <summary>
/// Defines a production rule for a language grammar.
/// Rules define how tokens and other rules can be combined to form larger structures.
/// </summary>
public class RuleDefinition
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule pattern in StepParser syntax.</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this rule is a terminal (cannot be broken down further).</summary>
    public bool IsTerminal { get; set; } = false;

    /// <summary>Gets or sets whether this rule can produce ambiguous parses.</summary>
    public bool IsAmbiguous { get; set; } = false;

    /// <summary>Gets or sets the rule priority (higher priority rules are applied first).</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Gets or sets the rule category (e.g., expression, statement, declaration).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the return type for this rule (used for type checking).</summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>Gets or sets custom code to execute when this rule is matched.</summary>
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule parameters.</summary>
    public List<RuleParameter> Parameters { get; set; } = new();

    /// <summary>Gets or sets whether this rule is a start rule (can be the root of a parse).</summary>
    public bool IsStartRule { get; set; } = false;

    /// <summary>Gets or sets whether this rule is abstract (cannot be directly instantiated).</summary>
    public bool IsAbstract { get; set; } = false;

    /// <summary>Gets or sets custom properties for this rule.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this rule definition.
    /// </summary>
    public RuleDefinition Clone()
    {
        return new RuleDefinition
        {
            Id = Id,
            Name = Name,
            Pattern = Pattern,
            Description = Description,
            IsTerminal = IsTerminal,
            IsAmbiguous = IsAmbiguous,
            Priority = Priority,
            Category = Category,
            ReturnType = ReturnType,
            ActionCode = ActionCode,
            Parameters = Parameters.Select(p => p.Clone()).ToList(),
            IsStartRule = IsStartRule,
            IsAbstract = IsAbstract,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    public override string ToString()
    {
        return $"{Name}: {Pattern}";
    }
}

/// <summary>
/// Defines a parameter for a rule.
/// </summary>
public class RuleParameter
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this parameter is required.</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Gets or sets the default value for this parameter.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Gets or sets custom properties for this parameter.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this parameter.
    /// </summary>
    public RuleParameter Clone()
    {
        return new RuleParameter
        {
            Id = Id,
            Name = Name,
            Type = Type,
            IsRequired = IsRequired,
            DefaultValue = DefaultValue,
            Properties = new Dictionary<string, object>(Properties)
        };
    }
}

/// <summary>
/// Rule category constants.
/// </summary>
public static class RuleCategories
{
    public const string CompilationUnit = "compilation_unit";
    public const string Expression = "expression";
    public const string Statement = "statement";
    public const string Declaration = "declaration";
    public const string Type = "type";
    public const string Literal = "literal";
    public const string Operator = "operator";
    public const string Modifier = "modifier";
    public const string Attribute = "attribute";
    public const string Directive = "directive";
}
