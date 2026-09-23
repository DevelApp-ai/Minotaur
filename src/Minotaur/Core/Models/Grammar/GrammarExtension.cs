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
/// The grammar format an extension file is written in.
/// </summary>
public enum ExtensionGrammarType
{
    /// <summary>Plain Backus-Naur Form rules.</summary>
    BNF,

    /// <summary>Extended Backus-Naur Form with regex token patterns.</summary>
    CEBNF
}

/// <summary>
/// The operation an extension entry performs on the base grammar.
/// </summary>
public enum ExtensionAction
{
    /// <summary>Add a new rule or token to the base grammar.</summary>
    Add,

    /// <summary>Remove an existing rule or token from the base grammar.</summary>
    Remove,

    /// <summary>Replace the pattern of an existing rule or token.</summary>
    Override
}

/// <summary>
/// The strategy used to resolve name collisions between a base grammar and
/// an extension. Mirrors the overlay composition strategies implemented by
/// the DevelApp.StepParser grammar merge engine (ENFAStepLexer-StepParser#66).
/// </summary>
public enum ExtensionMergeStrategy
{
    /// <summary>Extension rules are appended to the base grammar's rules.</summary>
    Additive,

    /// <summary>Extension rules replace the base grammar's rules on collision (default).</summary>
    Replace,

    /// <summary>The rule with the higher priority wins (ties go to the extension).</summary>
    PriorityBased
}

/// <summary>
/// A single token or rule entry declared by a grammar extension.
/// </summary>
public class ExtensionEntry
{
    /// <summary>Gets or sets the entry name (rule or token name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entry pattern: a regex for token definitions or a
    /// production body for grammar rules. Empty for <see cref="ExtensionAction.Remove"/>.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation to apply to the base grammar.</summary>
    public ExtensionAction Action { get; set; } = ExtensionAction.Add;

    /// <summary>
    /// Gets or sets the embedded-language context the entry applies to
    /// (e.g. "CSS" for a <c>@CONTEXT[CSS]</c> block). Empty for global entries.
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this entry is a production rule (as opposed to a token).</summary>
    public bool IsRule { get; set; } = false;

    /// <summary>Gets or sets the priority for <see cref="ExtensionMergeStrategy.PriorityBased"/> merges.</summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the line number in the extension file where this entry was declared.
    /// </summary>
    public int SourceLine { get; set; }

    /// <summary>Returns a string representation of this entry.</summary>
    public override string ToString()
    {
        return $"{Action} {(IsRule ? "rule" : "token")} '{Name}'{(string.IsNullOrEmpty(Context) ? string.Empty : $" [{Context}]")}";
    }
}

/// <summary>
/// A cross-language validation hint declared in an extension's
/// <c>@VALIDATE</c> block (e.g. <c>css_id_selector_validation: CSS_ID_SELECTOR -> HTML_ID_ATTR</c>).
/// </summary>
public class ExtensionValidationRule
{
    /// <summary>Gets or sets the validation rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source token/rule the validation starts from.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Gets or sets the target token/rule that must exist for validation to pass.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Gets or sets the line number in the extension file where this rule was declared.</summary>
    public int SourceLine { get; set; }

    /// <summary>Returns a string representation of this validation rule.</summary>
    public override string ToString()
    {
        return $"{Name}: {From} -> {To}";
    }
}

/// <summary>
/// A grammar extension loaded from a <c>.extension</c> file: a set of rules
/// and tokens to add to, remove from, or override in an existing base grammar,
/// without rewriting the full grammar (Minotaur issue #88).
/// </summary>
public class GrammarExtension
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the extension name (defaults to the file name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a reference to the base grammar this extension applies to
    /// (a grammar name or file name). May be empty when the association is
    /// made via <c>GrammarConfiguration.ExtensionMappings</c> instead.
    /// </summary>
    public string BaseGrammarRef { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar format the extension is written in.</summary>
    public ExtensionGrammarType GrammarType { get; set; } = ExtensionGrammarType.CEBNF;

    /// <summary>Gets or sets the embedded languages declared by the extension.</summary>
    public List<string> EmbeddedLanguages { get; set; } = new();

    /// <summary>Gets or sets whether the extension declares context-aware rules.</summary>
    public bool IsContextAware { get; set; }

    /// <summary>Gets or sets the merge strategy for name collisions with the base grammar.</summary>
    public ExtensionMergeStrategy MergeStrategy { get; set; } = ExtensionMergeStrategy.Replace;

    /// <summary>Gets or sets the token and rule entries declared at the top level.</summary>
    public List<ExtensionEntry> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the entries declared inside <c>@CONTEXT[lang]</c> blocks,
    /// keyed by embedded-language name.
    /// </summary>
    public Dictionary<string, List<ExtensionEntry>> ContextRules { get; set; } = new();

    /// <summary>Gets or sets the cross-language validation hints from <c>@VALIDATE</c> blocks.</summary>
    public List<ExtensionValidationRule> ValidationRules { get; set; } = new();

    /// <summary>Gets or sets the raw optimization hints from <c>@OPTIMIZE</c> blocks (advisory only).</summary>
    public List<string> OptimizeHints { get; set; } = new();

    /// <summary>Gets or sets the file path this extension was loaded from.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the date when this extension was loaded.</summary>
    public DateTime LoadedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets all entries including those inside context blocks.</summary>
    public IEnumerable<ExtensionEntry> AllEntries =>
        Entries.Concat(ContextRules.Values.SelectMany(entries => entries));

    /// <summary>Gets the names of all rules and tokens marked for removal.</summary>
    public IReadOnlyList<string> Removals =>
        AllEntries.Where(e => e.Action == ExtensionAction.Remove)
            .Select(e => e.Name)
            .Distinct()
            .ToList();

    /// <summary>
    /// Creates a deep copy of this grammar extension.
    /// </summary>
    public GrammarExtension Clone()
    {
        return new GrammarExtension
        {
            Id = Id,
            Name = Name,
            BaseGrammarRef = BaseGrammarRef,
            GrammarType = GrammarType,
            EmbeddedLanguages = new List<string>(EmbeddedLanguages),
            IsContextAware = IsContextAware,
            MergeStrategy = MergeStrategy,
            Entries = Entries.Select(e => CloneEntry(e)).ToList(),
            ContextRules = ContextRules.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(e => CloneEntry(e)).ToList()),
            ValidationRules = ValidationRules.Select(v => new ExtensionValidationRule
            {
                Name = v.Name,
                From = v.From,
                To = v.To,
                SourceLine = v.SourceLine
            }).ToList(),
            OptimizeHints = new List<string>(OptimizeHints),
            FilePath = FilePath,
            LoadedDate = LoadedDate
        };
    }

    private static ExtensionEntry CloneEntry(ExtensionEntry entry)
    {
        return new ExtensionEntry
        {
            Name = entry.Name,
            Pattern = entry.Pattern,
            Action = entry.Action,
            Context = entry.Context,
            IsRule = entry.IsRule,
            Priority = entry.Priority,
            SourceLine = entry.SourceLine
        };
    }

    /// <summary>Returns a string representation of this extension.</summary>
    public override string ToString()
    {
        return $"{Name} ({GrammarType}, {AllEntries.Count()} entries) -> {BaseGrammarRef}";
    }
}

/// <summary>
/// Provenance metadata tracking which grammar extension added, removed, or
/// modified a rule, token, or cognitive graph node (Minotaur issue #88, item 6).
/// </summary>
public class ExtensionProvenance
{
    /// <summary>Gets or sets the id of the extension responsible for the change.</summary>
    public string ExtensionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the extension responsible for the change.</summary>
    public string ExtensionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation the extension performed.</summary>
    public ExtensionAction Action { get; set; } = ExtensionAction.Add;

    /// <summary>
    /// Gets or sets the embedded-language context the change applies to, if any.
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>Returns a string representation of this provenance record.</summary>
    public override string ToString()
    {
        return $"{Action} by {ExtensionName} ({ExtensionId})" +
            (string.IsNullOrEmpty(Context) ? string.Empty : $" in context [{Context}]");
    }
}
