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

using Minotaur.Core.Models.Grammar;

namespace Minotaur.Validation;

/// <summary>
/// Minimal view of a base grammar used by <see cref="GrammarExtensionValidator"/>
/// so validation does not depend on a concrete grammar model type.
/// </summary>
public interface IBaseGrammarInfo
{
    /// <summary>Gets the base grammar name.</summary>
    string Name { get; }

    /// <summary>Gets the names of all rules and tokens in the base grammar.</summary>
    IReadOnlyCollection<string> RuleAndTokenNames { get; }
}

/// <summary>
/// Simple implementation of <see cref="IBaseGrammarInfo"/>.
/// </summary>
public record BaseGrammarInfo(string Name, IReadOnlyCollection<string> RuleAndTokenNames) : IBaseGrammarInfo;

/// <summary>
/// Validates grammar extensions against a base grammar before they are
/// applied (Minotaur issue #88, item 5): detects duplicate definitions
/// without override intent, removal/override references to rules that do not
/// exist in the base grammar, and <c>@CONTEXT[lang]</c> blocks that reference
/// languages not declared in the extension header.
/// </summary>
public class GrammarExtensionValidator
{
    /// <summary>
    /// Validates a single grammar extension against a base grammar.
    /// </summary>
    /// <param name="extension">The extension to validate.</param>
    /// <param name="baseGrammar">The base grammar the extension is applied to, if known.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation results (empty when the extension is valid).</returns>
    public IReadOnlyList<ValidationResult> Validate(
        GrammarExtension extension,
        IBaseGrammarInfo? baseGrammar = null,
        ValidationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(extension);

        var results = new List<ValidationResult>();
        var fileName = Path.GetFileName(extension.FilePath);

        ValidateDuplicateDefinitions(extension, fileName, results);
        ValidateAgainstBaseGrammar(extension, baseGrammar, fileName, results);
        ValidateContextLanguages(extension, fileName, results);
        ValidateValidationRules(extension, fileName, results);

        return results;
    }

    /// <summary>
    /// Validates multiple extensions the way they will be applied: in
    /// sequence, against the base grammar and against each other.
    /// </summary>
    /// <param name="extensions">The extensions in application order.</param>
    /// <param name="baseGrammar">The base grammar, if known.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation results (empty when all extensions are valid).</returns>
    public IReadOnlyList<ValidationResult> ValidateAll(
        IEnumerable<GrammarExtension> extensions,
        IBaseGrammarInfo? baseGrammar = null,
        ValidationContext? context = null)
    {
        var results = new List<ValidationResult>();
        var all = extensions?.ToList() ?? new List<GrammarExtension>();

        foreach (var extension in all)
        {
            results.AddRange(Validate(extension, baseGrammar, context));
        }

        ValidateCrossExtensionConflicts(all, results);
        return results;
    }

    private static void ValidateDuplicateDefinitions(
        GrammarExtension extension,
        string fileName,
        List<ValidationResult> results)
    {
        var entries = extension.AllEntries.ToList();

        var groups = entries
            .GroupBy(e => new { e.Name, e.Context })
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var hasOverride = group.Any(e => e.Action == ExtensionAction.Override);
            var actions = string.Join(", ", group.Select(e => e.Action));

            if (hasOverride || group.All(e => e.Action == ExtensionAction.Remove))
            {
                // Multiple entries with override intent are a valid way to
                // express precedence; pure removals collapse into one.
                continue;
            }

            results.Add(new ValidationResult
            {
                Message = $"Extension '{extension.Name}' defines '{group.Key.Name}' more than once " +
                          $"({actions}) without override intent. Use '~NAME = pattern' to override.",
                Severity = ValidationSeverity.Error,
                Code = "MNT-EXT-001",
                Location = new SourcePosition
                {
                    Line = group.Min(e => e.SourceLine),
                    Column = 1,
                    File = fileName
                },
                Context = new Dictionary<string, string>
                {
                    ["extension"] = extension.Name,
                    ["entry"] = group.Key.Name
                }
            });
        }
    }

    private static void ValidateAgainstBaseGrammar(
        GrammarExtension extension,
        IBaseGrammarInfo? baseGrammar,
        string fileName,
        List<ValidationResult> results)
    {
        if (baseGrammar is null)
        {
            return;
        }

        var baseNames = baseGrammar.RuleAndTokenNames.ToHashSet(StringComparer.Ordinal);

        foreach (var entry in extension.AllEntries)
        {
            if (entry.Action != ExtensionAction.Remove && entry.Action != ExtensionAction.Override)
            {
                continue;
            }

            if (!baseNames.Contains(entry.Name))
            {
                results.Add(new ValidationResult
                {
                    Message = $"Extension '{extension.Name}' tries to {entry.Action.ToString().ToLowerInvariant()} " +
                              $"'{entry.Name}', which does not exist in base grammar '{baseGrammar.Name}'.",
                    Severity = ValidationSeverity.Error,
                    Code = "MNT-EXT-002",
                    Location = new SourcePosition
                    {
                        Line = entry.SourceLine,
                        Column = 1,
                        File = fileName
                    },
                    Context = new Dictionary<string, string>
                    {
                        ["extension"] = extension.Name,
                        ["baseGrammar"] = baseGrammar.Name,
                        ["entry"] = entry.Name,
                        ["action"] = entry.Action.ToString()
                    }
                });
            }
        }
    }

    private static void ValidateContextLanguages(
        GrammarExtension extension,
        string fileName,
        List<ValidationResult> results)
    {
        var declared = new HashSet<string>(extension.EmbeddedLanguages, StringComparer.OrdinalIgnoreCase);

        foreach (var (language, entries) in extension.ContextRules)
        {
            if (!declared.Contains(language))
            {
                results.Add(new ValidationResult
                {
                    Message = $"Extension '{extension.Name}' has a @CONTEXT[{language}] block, but '{language}' " +
                              "is not declared in the EmbeddedLanguages header.",
                    Severity = ValidationSeverity.Warning,
                    Code = "MNT-EXT-003",
                    Location = new SourcePosition
                    {
                        Line = entries.FirstOrDefault()?.SourceLine ?? 1,
                        Column = 1,
                        File = fileName
                    },
                    Context = new Dictionary<string, string>
                    {
                        ["extension"] = extension.Name,
                        ["language"] = language
                    }
                });
            }
        }
    }

    private static void ValidateValidationRules(
        GrammarExtension extension,
        string fileName,
        List<ValidationResult> results)
    {
        var knownNames = extension.AllEntries.Select(e => e.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var rule in extension.ValidationRules)
        {
            foreach (var (referenced, side) in new[] { (rule.From, "from"), (rule.To, "to") })
            {
                if (!knownNames.Contains(referenced))
                {
                    results.Add(new ValidationResult
                    {
                        Message = $"Validation rule '{rule.Name}' references '{referenced}' ({side} side), " +
                                  $"which is not defined in extension '{extension.Name}'.",
                        Severity = ValidationSeverity.Warning,
                        Code = "MNT-EXT-004",
                        Location = new SourcePosition
                        {
                            Line = rule.SourceLine,
                            Column = 1,
                            File = fileName
                        },
                        Context = new Dictionary<string, string>
                        {
                            ["extension"] = extension.Name,
                            ["validationRule"] = rule.Name,
                            ["referenced"] = referenced
                        }
                    });
                }
            }
        }
    }

    private static void ValidateCrossExtensionConflicts(
        List<GrammarExtension> extensions,
        List<ValidationResult> results)
    {
        for (var i = 0; i < extensions.Count; i++)
        {
            for (var j = i + 1; j < extensions.Count; j++)
            {
                var a = extensions[i];
                var b = extensions[j];

                var aAdds = AllAddNames(a);
                var bAdds = AllAddNames(b);

                var shared = aAdds.Keys.Intersect(bAdds.Keys, StringComparer.Ordinal)
                    .Where(name => aAdds[name] == ExtensionAction.Add && bAdds[name] == ExtensionAction.Add)
                    .ToList();

                foreach (var name in shared)
                {
                    results.Add(new ValidationResult
                    {
                        Message = $"Extensions '{a.Name}' and '{b.Name}' both add '{name}'. " +
                                  "Set an explicit MergeStrategy or use '~' (override) to resolve the ambiguity.",
                        Severity = ValidationSeverity.Warning,
                        Code = "MNT-EXT-005",
                        Context = new Dictionary<string, string>
                        {
                            ["extensionA"] = a.Name,
                            ["extensionB"] = b.Name,
                            ["entry"] = name
                        }
                    });
                }
            }
        }
    }

    private static Dictionary<string, ExtensionAction> AllAddNames(GrammarExtension extension)
    {
        return extension.AllEntries
            .GroupBy(e => e.Name)
            .ToDictionary(g => g.Key, g => g.First().Action, StringComparer.Ordinal);
    }
}
