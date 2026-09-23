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

using DevelApp.StepParser;
using Minotaur.Core.Models.Grammar;
using Minotaur.Projects.Grammar;

namespace Minotaur.Parser;

/// <summary>
/// Grammar extension support for <see cref="StepParserIntegration"/>
/// (Minotaur issue #88): loading <c>.extension</c> files and applying them to
/// a base grammar via the DevelApp.StepParser grammar composition engine
/// (ENFAStepLexer-StepParser#66: <c>GrammarLoader.ComposeGrammars</c> /
/// <c>ComposeWithOverlayContent</c>).
/// </summary>
public partial class StepParserIntegration
{
    /// <summary>
    /// Maps a Minotaur extension merge strategy to the strategy of the
    /// DevelApp.StepParser overlay composition engine.
    /// </summary>
    private static OverlayConflictResolution ToOverlayConflictResolution(ExtensionMergeStrategy strategy)
    {
        return strategy switch
        {
            ExtensionMergeStrategy.Additive => OverlayConflictResolution.Additive,
            ExtensionMergeStrategy.PriorityBased => OverlayConflictResolution.PriorityBased,
            _ => OverlayConflictResolution.OverlayWins
        };
    }

    /// <summary>
    /// Loads the grammar extension files configured on the parser
    /// configuration (<see cref="ParserConfiguration.GrammarExtensionPaths"/>).
    /// </summary>
    /// <returns>The successfully loaded grammar extensions.</returns>
    /// <exception cref="ExtensionParseException">
    /// Thrown when any configured extension file cannot be parsed.
    /// </exception>
    public async Task<IReadOnlyList<GrammarExtension>> LoadConfiguredGrammarExtensionsAsync()
    {
        var extensions = new List<GrammarExtension>();

        foreach (var path in _config.GrammarExtensionPaths)
        {
            var result = await GrammarExtensionLoader.LoadFromFileAsync(path);
            if (result.Success && result.Extension is not null)
            {
                extensions.Add(result.Extension);
            }
            else
            {
                throw new ExtensionParseException(path, result.Errors);
            }
        }

        return extensions;
    }

    /// <summary>
    /// Applies grammar extensions to a base grammar and returns the merged
    /// DevelApp.StepParser grammar definition. Additions and overrides are
    /// composed via the grammar merge engine using the configured merge
    /// strategy; removals are applied to the composite grammar afterwards.
    /// The base grammar content is not modified.
    /// </summary>
    /// <param name="baseGrammarContent">The grammar file content of the base grammar.</param>
    /// <param name="extensions">The extensions to apply, in order.</param>
    /// <returns>The merge result: the composite grammar plus resolved conflicts.</returns>
    public GrammarMergeResult ApplyGrammarExtensions(
        string baseGrammarContent,
        IReadOnlyList<GrammarExtension> extensions)
    {
        if (string.IsNullOrEmpty(baseGrammarContent))
        {
            throw new ArgumentException("Base grammar content cannot be null or empty", nameof(baseGrammarContent));
        }

        ArgumentNullException.ThrowIfNull(extensions);

        var loader = new GrammarLoader();
        var merged = loader.ParseGrammarContent(baseGrammarContent, "base.grammar");
        var conflicts = new List<string>();

        foreach (var extension in extensions)
        {
            var overlay = GrammarExtensionOverlayRenderer.RenderOverlay(extension);
            var hasOverlayEntries = extension.AllEntries.Any(e => e.Action != ExtensionAction.Remove);

            if (hasOverlayEntries)
            {
                var overlayGrammar = loader.ParseGrammarContent(overlay, $"{extension.Name}.extension");
                var strategy = extension.MergeStrategy == ExtensionMergeStrategy.Replace &&
                               _config.ExtensionMergeStrategy != ExtensionMergeStrategy.Replace
                    ? _config.ExtensionMergeStrategy
                    : extension.MergeStrategy;

                var merge = loader.ComposeGrammars(merged, overlayGrammar, ToOverlayConflictResolution(strategy));
                merged = merge.Grammar;
                conflicts.AddRange(merge.Conflicts);
            }

            ApplyRemovals(merged, extension);
        }

        return new GrammarMergeResult(merged, conflicts);
    }

    /// <summary>
    /// Applies extension removals to a merged grammar by removing token rules
    /// and production rules whose names appear in the extension's removal list.
    /// </summary>
    private static void ApplyRemovals(DevelApp.StepParser.GrammarDefinition grammar, GrammarExtension extension)
    {
        var removals = extension.Removals;
        if (removals.Count == 0)
        {
            return;
        }

        var removalSet = new HashSet<string>(removals, StringComparer.Ordinal);
        grammar.TokenRules.RemoveAll(rule => removalSet.Contains(rule.Name));
        grammar.ProductionRules.RemoveAll(rule => removalSet.Contains(rule.Name));
    }
}

/// <summary>
/// Exception thrown when a grammar extension file cannot be parsed.
/// </summary>
public class ExtensionParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ExtensionParseException class.
    /// </summary>
    /// <param name="extensionPath">The extension file path.</param>
    /// <param name="errors">The parse errors.</param>
    public ExtensionParseException(string extensionPath, IReadOnlyList<ExtensionParseError> errors)
        : base($"Failed to parse grammar extension '{extensionPath}': {string.Join("; ", errors)}")
    {
        ExtensionPath = extensionPath;
        Errors = errors;
    }

    /// <summary>Gets the extension file path that failed to parse.</summary>
    public string ExtensionPath { get; }

    /// <summary>Gets the parse errors found in the extension file.</summary>
    public IReadOnlyList<ExtensionParseError> Errors { get; }
}
