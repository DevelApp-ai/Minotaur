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
/// Defines a complete grammar for a language.
/// Contains tokens, rules, and metadata about the language.
/// </summary>
public class GrammarDefinition
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the grammar name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar version.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Gets or sets the target language name.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar author.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar license.</summary>
    public string License { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of token definitions.</summary>
    public List<TokenDefinition> Tokens { get; set; } = new();

    /// <summary>Gets or sets the list of rule definitions.</summary>
    public List<RuleDefinition> Rules { get; set; } = new();

    /// <summary>Gets or sets the start rule ID (entry point for parsing).</summary>
    public string StartRuleId { get; set; } = string.Empty;

    /// <summary>Gets or sets the date when this grammar was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the date when this grammar was last modified.</summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets custom properties for this grammar.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>Gets or sets the file path where this grammar is stored.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this grammar has been modified since last save.</summary>
    public bool IsModified { get; set; } = false;

    /// <summary>Gets the start rule.</summary>
    public RuleDefinition? StartRule => Rules.FirstOrDefault(r => r.Id == StartRuleId || r.IsStartRule);

    /// <summary>Gets all terminal tokens.</summary>
    public IEnumerable<TokenDefinition> TerminalTokens => Tokens.Where(t => t.IsTerminal);

    /// <summary>Gets all skippable tokens.</summary>
    public IEnumerable<TokenDefinition> SkippableTokens => Tokens.Where(t => t.IsSkippable);

    /// <summary>Gets all ambiguous rules.</summary>
    public IEnumerable<RuleDefinition> AmbiguousRules => Rules.Where(r => r.IsAmbiguous);

    /// <summary>Gets the total number of tokens and rules.</summary>
    public int TotalDefinitions => Tokens.Count + Rules.Count;

    /// <summary>
    /// Creates a deep copy of this grammar definition.
    /// </summary>
    public GrammarDefinition Clone()
    {
        return new GrammarDefinition
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Version = Version,
            Language = Language,
            Author = Author,
            License = License,
            Tokens = Tokens.Select(t => t.Clone()).ToList(),
            Rules = Rules.Select(r => r.Clone()).ToList(),
            StartRuleId = StartRuleId,
            CreatedDate = CreatedDate,
            ModifiedDate = ModifiedDate,
            Properties = new Dictionary<string, object>(Properties),
            FilePath = FilePath,
            IsModified = IsModified
        };
    }

    /// <summary>
    /// Returns a string representation of this grammar.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} v{Version} ({Language})";
    }
}
