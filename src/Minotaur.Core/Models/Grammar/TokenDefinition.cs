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
/// Defines a token for a language grammar.
/// Tokens are the atomic units of source code that the lexer recognizes.
/// </summary>
public class TokenDefinition
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the token name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the regular expression pattern for matching this token.</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Gets or sets the token description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this token is a terminal (cannot be broken down further).</summary>
    public bool IsTerminal { get; set; } = true;

    /// <summary>Gets or sets whether this token can be skipped (e.g., whitespace).</summary>
    public bool IsSkippable { get; set; } = false;

    /// <summary>Gets or sets the token priority (higher priority tokens are matched first).</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Gets or sets the token category (e.g., keyword, identifier, operator).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the color for syntax highlighting.</summary>
    public string Color { get; set; } = "#000000";

    /// <summary>Gets or sets whether this token is case-sensitive.</summary>
    public bool IsCaseSensitive { get; set; } = true;

    /// <summary>Gets or sets custom properties for this token.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this token definition.
    /// </summary>
    public TokenDefinition Clone()
    {
        return new TokenDefinition
        {
            Id = Id,
            Name = Name,
            Pattern = Pattern,
            Description = Description,
            IsTerminal = IsTerminal,
            IsSkippable = IsSkippable,
            Priority = Priority,
            Category = Category,
            Color = Color,
            IsCaseSensitive = IsCaseSensitive,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    /// <summary>
    /// Returns a string representation of this token.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({Pattern}) [{Category}]";
    }
}

/// <summary>
/// Token category constants.
/// </summary>
public static class TokenCategories
{
    public const string Whitespace = "whitespace";
    public const string Comment = "comment";
    public const string Identifier = "identifier";
    public const string Keyword = "keyword";
    public const string Literal = "literal";
    public const string Number = "number";
    public const string String = "string";
    public const string Character = "character";
    public const string Operator = "operator";
    public const string Punctuation = "punctuation";
    public const string Preprocessor = "preprocessor";
}
