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

using CognitiveGraph.Builder;
using CognitiveGraph.Schema;
using Microsoft.Extensions.Logging;
using Minotaur.Core.Models.Visualization;
using Minotaur.Core.Services.Visualization;
using Moq;
using Xunit;
using CGraph = CognitiveGraph.CognitiveGraph;

namespace Minotaur.Tests.Visualization;

/// <summary>
/// Tests for CognitiveGraphVisualizer service.
///
/// These tests verify that the visualizer correctly:
/// 1. Traverses CognitiveGraph SymbolNode/PackedNode structure
/// 2. Preserves ambiguity by showing all PackedNode alternatives
/// 3. Identifies ambiguous nodes (nodes with multiple PackedNodes)
/// 4. Generates all possible interpretation paths
/// 5. Filters to show only selected PackedNode paths
///
/// Graphs are built with the real DevelApp.CognitiveGraph builder API.
/// </summary>
public class CognitiveGraphVisualizerTests
{
    private readonly CognitiveGraphVisualizer _visualizer;
    private readonly Mock<ILogger<CognitiveGraphVisualizer>> _loggerMock;

    public CognitiveGraphVisualizerTests()
    {
        _loggerMock = new Mock<ILogger<CognitiveGraphVisualizer>>();
        _visualizer = new CognitiveGraphVisualizer(_loggerMock.Object);
    }

    [Fact]
    public void GenerateVisualization_SingleNode_ReturnsSingleNode()
    {
        // Arrange
        var graph = CreateGraphWithSingleNode("test");

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.GraphData.Nodes);
        Assert.Empty(result.GraphData.Edges);
        Assert.Empty(result.AmbiguityPoints);
        Assert.False(result.HasAmbiguities);
    }

    [Fact]
    public void GenerateVisualization_NodeWithSinglePackedNode_ReturnsNodeWithEdges()
    {
        // Arrange
        var graph = CreateGraphWithHierarchy();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.GraphData.Nodes.Count); // root + child1 + child2
        Assert.Equal(2, result.GraphData.Edges.Count); // root->child1, root->child2
        Assert.Empty(result.AmbiguityPoints);
        Assert.False(result.HasAmbiguities);
    }

    [Fact]
    public void GenerateVisualization_NodeWithMultiplePackedNodes_IdentifiesAmbiguity()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasAmbiguities);
        Assert.Equal(1, result.AmbiguityCount);
        Assert.Single(result.AmbiguityPoints);

        var ambiguity = result.AmbiguityPoints.First();
        Assert.True(ambiguity.IsAmbiguous);
        Assert.Equal(2, ambiguity.AlternativeCount);
        Assert.Equal(2, ambiguity.Alternatives.Count);
    }

    [Fact]
    public void GenerateVisualization_PackedNodeEdges_MarkedAsAlternative()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var alternativeEdges = result.GraphData.Edges
            .Where(e => e.IsAlternative)
            .ToList();

        Assert.NotEmpty(alternativeEdges);
    }

    [Fact]
    public void GetAmbiguityPoints_SingleNodeGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateGraphWithSingleNode("test");

        // Act
        var result = _visualizer.GetAmbiguityPoints(graph);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAmbiguityPoints_WithAmbiguity_ReturnsAllAmbiguousNodes()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GetAmbiguityPoints(graph);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsAmbiguous);
        Assert.Equal(2, result[0].AlternativeCount);
    }

    [Fact]
    public void GetAllInterpretationPaths_SinglePath_ReturnsOnePath()
    {
        // Arrange
        var graph = CreateGraphWithHierarchy();

        // Act
        var result = _visualizer.GetAllInterpretationPaths(graph);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void GetAllInterpretationPaths_WithAmbiguity_ReturnsMultiplePaths()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GetAllInterpretationPaths(graph);

        // Assert
        Assert.Equal(2, result.Count); // 2 PackedNodes = 2 paths
    }

    [Fact]
    public void GenerateSingleInterpretation_ValidPath_FiltersToSelectedPath()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();
        var paths = _visualizer.GetAllInterpretationPaths(graph);
        var path = paths[0];

        // Act
        var result = _visualizer.GenerateSingleInterpretation(graph, path);

        // Assert
        Assert.Equal(VisualizationMode.ShowSelectedInterpretation, result.Options.Mode);
        Assert.Same(path, result.SelectedPath);
        Assert.NotEmpty(result.GraphData.Edges);
    }

    [Fact]
    public void GenerateVisualization_NodeProperties_PreservedInGraphNode()
    {
        // Arrange
        var graph = CreateGraphWithSingleNode("test");

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var node = result.GraphData.Nodes[0];
        Assert.Equal("test", node.Name);
        Assert.NotEmpty(node.Id);
        Assert.NotNull(node.Location);
    }

    [Fact]
    public void GenerateVisualization_AmbiguousNode_HasCorrectProperties()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var ambiguousNode = result.GraphData.Nodes
            .FirstOrDefault(n => n.IsAmbiguous);

        Assert.NotNull(ambiguousNode);
        Assert.True(ambiguousNode.IsAmbiguous);
        Assert.Equal(2, ambiguousNode.AlternativeCount);

        var ambiguity = result.AmbiguityPoints
            .FirstOrDefault(a => a.NodeId == ambiguousNode.Id);
        Assert.NotNull(ambiguity);
        Assert.Equal(2, ambiguity.AlternativeCount);
    }

    [Fact]
    public void GenerateVisualization_EdgeProperties_IncludeRuleIdAndPackedNodeIndex()
    {
        // Arrange
        var graph = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var edges = result.GraphData.Edges;
        Assert.NotEmpty(edges);
        Assert.Contains(edges, e => e.RuleId == TestRuleId1);
        Assert.Contains(edges, e => e.RuleId == TestRuleId2);
    }

    [Fact]
    public void GenerateVisualization_NullGraph_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _visualizer.GenerateVisualization(null!));
    }

    // ==================== Helper Methods ====================

    private const ushort TestRuleId1 = 7;
    private const ushort TestRuleId2 = 8;

    private static CGraph CreateGraphWithSingleNode(string sourceText)
    {
        var builder = new CognitiveGraphBuilder();
        var node = WriteSymbolNode(builder, 1, 1, 0, (uint)sourceText.Length, new List<uint>());
        return new CGraph(builder.Build(node, sourceText));
    }

    private static CGraph CreateGraphWithHierarchy()
    {
        var builder = new CognitiveGraphBuilder();
        var child1 = WriteSymbolNode(builder, 2, 2, 0, 6, new List<uint>());
        var child2 = WriteSymbolNode(builder, 3, 2, 6, 6, new List<uint>());
        var packed = builder.WritePackedNode(TestRuleId1, new List<uint> { child1, child2 }, new List<CpgEdgeData>());
        var root = WriteSymbolNode(builder, 1, 1, 0, 12, new List<uint> { packed });
        return new CGraph(builder.Build(root, "root text here"));
    }

    private static CGraph CreateGraphWithAmbiguity()
    {
        var builder = new CognitiveGraphBuilder();
        var child1 = WriteSymbolNode(builder, 2, 2, 0, 6, new List<uint>());
        var child2 = WriteSymbolNode(builder, 3, 2, 0, 6, new List<uint>());
        var pk1 = builder.WritePackedNode(TestRuleId1, new List<uint> { child1 }, new List<CpgEdgeData>());
        var pk2 = builder.WritePackedNode(TestRuleId2, new List<uint> { child2 }, new List<CpgEdgeData>());
        var root = WriteSymbolNode(builder, 1, 1, 0, 12, new List<uint> { pk1, pk2 });
        return new CGraph(builder.Build(root, "root text here"));
    }

    private static uint WriteSymbolNode(
        CognitiveGraphBuilder builder,
        ushort symbolId,
        ushort nodeType,
        uint sourceStart,
        uint sourceLength,
        List<uint> packedNodeOffsets)
    {
        return builder.WriteSymbolNode(
            symbolId,
            nodeType,
            sourceStart,
            sourceLength,
            packedNodeOffsets,
            new List<(string, PropertyValueType, object)>());
    }
}
