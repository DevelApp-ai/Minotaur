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

using Minotaur.Core;
using Minotaur.Parser;
using Minotaur.Unparser;
using Xunit;

namespace Minotaur.Tests.Integration;

/// <summary>
/// End-to-end round-trip tests: parse → edit → unparse → re-parse.
///
/// These tests exercise the full pipeline on real source code:
/// 1. Parse source code into a CognitiveGraphNode tree
///    (StepParserIntegration, falling back to its built-in demo parser
///    when the external DevelApp.StepParser package is unavailable).
/// 2. Edit a node in the resulting tree.
/// 3. Unparse the tree back into source code with GraphUnparser.
/// 4. Re-parse the generated code and verify the edit persisted.
/// </summary>
public class ParseEditUnparseRoundTripTests
{
    [Fact]
    public async Task RoundTrip_RenameIdentifier_PersistsAfterReparse()
    {
        // Arrange
        const string source = "var counter = 42;";
        using var parser = StepParserIntegrationFactory.CreateForCSharp();

        // Act 1 - parse
        var root = await parser.ParseToCognitiveGraphAsync(source);
        Assert.NotNull(root);

        var identifier = FindNodes(root).OfType<IdentifierNode>()
            .SingleOrDefault(n => n.Text == "counter");
        Assert.NotNull(identifier);

        // Act 2 - edit (rename identifier)
        identifier.Text = "total";
        identifier.Metadata["text"] = "total";

        // Act 3 - unparse
        using var unparser = CreateUnparser();
        var code = unparser.Unparse(root);

        // Assert - generated code reflects the edit, not the original
        Assert.NotNull(code);
        Assert.Contains("total", code);
        Assert.DoesNotContain("counter", code);
        Assert.Contains("42", code);
        Assert.DoesNotContain("Unknown node type", code);

        // Act 4 - re-parse and verify the edit survived the round trip
        var reparsed = await parser.ParseToCognitiveGraphAsync(code);
        Assert.NotNull(reparsed);

        var reparsedIdentifier = FindNodes(reparsed).OfType<IdentifierNode>()
            .SingleOrDefault(n => n.Text == "total");
        Assert.NotNull(reparsedIdentifier);
        Assert.Null(FindNodes(reparsed).OfType<IdentifierNode>()
            .SingleOrDefault(n => n.Text == "counter"));
    }

    [Fact]
    public async Task RoundTrip_ChangeLiteralValue_PersistsAfterReparse()
    {
        // Arrange
        const string source = "var total = 42;";
        using var parser = StepParserIntegrationFactory.CreateForCSharp();

        // Act 1 - parse
        var root = await parser.ParseToCognitiveGraphAsync(source);
        Assert.NotNull(root);

        var literal = FindNodes(root).OfType<LiteralNode>()
            .SingleOrDefault(n => n.Text == "42");
        Assert.NotNull(literal);

        // Act 2 - edit (change literal value)
        literal.Text = "99";
        literal.Metadata["text"] = "99";

        // Act 3 - unparse
        using var unparser = CreateUnparser();
        var code = unparser.Unparse(root);

        // Assert
        Assert.Contains("99", code);
        Assert.DoesNotContain("42", code);

        // Act 4 - re-parse
        var reparsed = await parser.ParseToCognitiveGraphAsync(code);
        Assert.NotNull(reparsed);
        Assert.NotNull(FindNodes(reparsed).OfType<LiteralNode>()
            .SingleOrDefault(n => n.Text == "99"));
    }

    [Fact]
    public async Task RoundTrip_UnmodifiedTree_ProducesParsableCode()
    {
        // Arrange
        const string source = "var alpha = 1;";
        using var parser = StepParserIntegrationFactory.CreateForCSharp();

        // Act - parse, then unparse without editing
        var root = await parser.ParseToCognitiveGraphAsync(source);
        Assert.NotNull(root);

        using var unparser = CreateUnparser();
        var code = unparser.Unparse(root);

        // Assert - the unparsed code re-parses to the same token set
        Assert.NotNull(code);
        Assert.Contains("alpha", code);
        Assert.Contains("1", code);
        Assert.DoesNotContain("Unknown node type", code);

        var reparsed = await parser.ParseToCognitiveGraphAsync(code);
        Assert.NotNull(reparsed);

        // The source's identifier/literal tokens must survive the round trip.
        // (The demo parser appends an integration-marker node after the
        // statement, so compare the original tokens as a prefix.)
        var originalTokens = ExtractMeaningfulTokens(root);
        var reparsedTokens = ExtractMeaningfulTokens(reparsed);
        Assert.Equal(originalTokens, reparsedTokens.Take(originalTokens.Count));
    }

    // ==================== Helper Methods ====================

    private static GraphUnparser CreateUnparser()
    {
        var unparser = new GraphUnparser(new UnparseConfiguration
        {
            PreserveFormatting = false,
            IncludeComments = false
        });

        // The demo parser emits a compilation_unit root that has no
        // built-in strategy; treat it as a plain container.
        unparser.RegisterStrategy("compilation_unit", new NonTerminalUnparseStrategy());

        return unparser;
    }

    private static IEnumerable<CognitiveGraphNode> FindNodes(CognitiveGraphNode root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var descendant in FindNodes(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Extracts the identifier and literal token texts from a tree, in order.
    /// </summary>
    private static List<string> ExtractMeaningfulTokens(CognitiveGraphNode root)
    {
        return FindNodes(root)
            .Where(n => n.NodeType is "identifier" or "literal")
            .Select(n => n.Metadata.TryGetValue("text", out var text) ? text.ToString() ?? string.Empty : string.Empty)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
    }
}
