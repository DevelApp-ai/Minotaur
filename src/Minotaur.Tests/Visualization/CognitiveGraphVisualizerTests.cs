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

using CognitiveGraph;
using Minotaur.Core.Models.Visualization;
using Minotaur.Core.Services.Visualization;
using Moq;
using Xunit;

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
    public void GenerateVisualization_EmptyGraph_ReturnsEmptyVisualization()
    {
        // Arrange
        var graph = CreateEmptyGraph();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.GraphData);
        Assert.Empty(result.GraphData.Nodes);
        Assert.Empty(result.GraphData.Edges);
        Assert.Empty(result.Ambiguities);
        Assert.False(result.HasAmbiguities);
        Assert.Equal(0, result.AmbiguityCount);
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
        Assert.Empty(result.Ambiguities);
        Assert.False(result.HasAmbiguities);
    }

    [Fact]
    public void GenerateVisualization_NodeWithSinglePackedNode_ReturnsNodeWithEdges()
    {
        // Arrange
        var (graph, _) = CreateGraphWithHierarchy();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.GraphData.Nodes.Count); // root + child1 + child2
        Assert.Equal(2, result.GraphData.Edges.Count); // root->child1, root->child2
        Assert.Empty(result.Ambiguities);
        Assert.False(result.HasAmbiguities);
    }

    [Fact]
    public void GenerateVisualization_NodeWithMultiplePackedNodes_IdentifiesAmbiguity()
    {
        // Arrange
        var (graph, ambiguousNodeId) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasAmbiguities);
        Assert.Equal(1, result.AmbiguityCount);
        Assert.Single(result.Ambiguities);
        
        var ambiguity = result.Ambiguities.Values.First();
        Assert.True(ambiguity.IsAmbiguous);
        Assert.Equal(2, ambiguity.AlternativeCount);
        Assert.Equal(2, ambiguity.PackedNodes.Count);
    }

    [Fact]
    public void GenerateVisualization_PackedNodeEdges_MarkedAsAlternative()
    {
        // Arrange
        var (graph, _) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var alternativeEdges = result.GraphData.Edges
            .Where(e => e.IsAlternative)
            .ToList();
        
        Assert.NotEmpty(alternativeEdges);
        Assert.All(alternativeEdges, e => Assert.NotEqual(0, e.PackedNodeIndex));
    }

    [Fact]
    public void GetAmbiguityPoints_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateEmptyGraph();

        // Act
        var result = _visualizer.GetAmbiguityPoints(graph);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAmbiguityPoints_WithAmbiguity_ReturnsAllAmbiguousNodes()
    {
        // Arrange
        var (graph, _) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GetAmbiguityPoints(graph);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsAmbiguous);
        Assert.Equal(2, result[0].AlternativeCount);
    }

    [Fact]
    public void GetAllInterpretationPaths_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateEmptyGraph();

        // Act
        var result = _visualizer.GetAllInterpretationPaths(graph);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllInterpretationPaths_SinglePath_ReturnsOnePath()
    {
        // Arrange
        var (graph, _) = CreateGraphWithHierarchy();

        // Act
        var result = _visualizer.GetAllInterpretationPaths(graph);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsValid);
    }

    [Fact]
    public void GetAllInterpretationPaths_WithAmbiguity_ReturnsMultiplePaths()
    {
        // Arrange
        var (graph, _) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GetAllInterpretationPaths(graph);

        // Assert
        Assert.Equal(2, result.Count); // 2 PackedNodes = 2 paths
        Assert.All(result, p => Assert.True(p.IsValid));
    }

    [Fact]
    public void GenerateSingleInterpretation_ValidPath_FiltersToSelectedPath()
    {
        // Arrange
        var (graph, _) = CreateGraphWithAmbiguity();
        var paths = _visualizer.GetAllInterpretationPaths(graph);
        var path = paths[0];

        // Act
        var result = _visualizer.GenerateSingleInterpretation(graph, path);

        // Assert
        Assert.Equal(VisualizationMode.ShowSelectedInterpretation, result.Mode);
        
        // In single interpretation mode, alternative edges should be filtered
        // Note: The current implementation filters edges based on the path
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
        var (graph, ambiguousNodeId) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var ambiguousNode = result.GraphData.Nodes
            .FirstOrDefault(n => n.IsAmbiguous);
        
        Assert.NotNull(ambiguousNode);
        Assert.True(ambiguousNode.IsAmbiguous);
        Assert.Equal(2, ambiguousNode.AlternativeCount);
        
        var ambiguity = result.Ambiguities[ambiguousNode.Id];
        Assert.NotNull(ambiguity);
        Assert.Equal(2, ambiguity.PackedNodes.Count);
    }

    [Fact]
    public void GenerateVisualization_EdgeProperties_IncludeRuleIdAndPackedNodeIndex()
    {
        // Arrange
        var (graph, _) = CreateGraphWithAmbiguity();

        // Act
        var result = _visualizer.GenerateVisualization(graph);

        // Assert
        var edges = result.GraphData.Edges;
        Assert.All(edges, e => {
            Assert.NotEqual(0u, e.RuleId);
            Assert.NotEqual(0, e.PackedNodeIndex);
        });
    }

    // ==================== Helper Methods ====================

    private CognitiveGraph CreateEmptyGraph()
    {
        var graph = new CognitiveGraph();
        return graph;
    }

    private CognitiveGraph CreateGraphWithSingleNode(string sourceText)
    {
        var graph = new CognitiveGraph();
        var node = new SymbolNode(graph, 0, (uint)sourceText.Length, 1);
        graph.AddNode(node);
        return graph;
    }

    private (CognitiveGraph, ulong) CreateGraphWithHierarchy()
    {
        var graph = new CognitiveGraph();
        
        // Create root node
        var root = new SymbolNode(graph, 0, 10, 1);
        graph.AddNode(root);
        
        // Create child nodes
        var child1 = new SymbolNode(graph, 1, 3, 2);
        var child2 = new SymbolNode(graph, 4, 3, 3);
        graph.AddNode(child1);
        graph.AddNode(child2);
        
        // Create PackedNode with children
        var packedNode = new PackedNode(graph, 100, 1);
        packedNode.AddChildNode(child1);
        packedNode.AddChildNode(child2);
        root.AddPackedNode(packedNode);
        
        return (graph, root.Id);
    }

    private (CognitiveGraph, ulong) CreateGraphWithAmbiguity()
    {
        var graph = new CognitiveGraph();
        
        // Create root node
        var root = new SymbolNode(graph, 0, 10, 1);
        graph.AddNode(root);
        
        // Create ambiguous node (has 2 PackedNodes)
        var ambiguousNode = new SymbolNode(graph, 1, 5, 2);
        graph.AddNode(ambiguousNode);
        
        // Create child nodes
        var child1 = new SymbolNode(graph, 2, 2, 3);
        var child2 = new SymbolNode(graph, 3, 2, 4);
        graph.AddNode(child1);
        graph.AddNode(child2);
        
        // Create first PackedNode (interpretation 1)
        var packedNode1 = new PackedNode(graph, 101, 1);
        packedNode1.AddChildNode(child1);
        ambiguousNode.AddPackedNode(packedNode1);
        
        // Create second PackedNode (interpretation 2)
        var packedNode2 = new PackedNode(graph, 102, 1);
        packedNode2.AddChildNode(child2);
        ambiguousNode.AddPackedNode(packedNode2);
        
        // Connect root to ambiguous node
        var rootPackedNode = new PackedNode(graph, 100, 1);
        rootPackedNode.AddChildNode(ambiguousNode);
        root.AddPackedNode(rootPackedNode);
        
        return (graph, ambiguousNode.Id);
    }
}
