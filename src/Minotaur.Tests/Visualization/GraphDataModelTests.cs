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

using System.Text.Json;
using Minotaur.Core.Models.Visualization;
using Xunit;

namespace Minotaur.Tests.Visualization;

/// <summary>
/// Tests for GraphData models.
///
/// These tests verify that the visualization models:
/// 1. Can be created with default values
/// 2. Can be serialized and deserialized
/// 3. Have correct properties for ambiguous nodes
/// 4. Have correct properties for alternative edges
/// </summary>
public class GraphDataModelTests
{
    [Fact]
    public void GraphData_DefaultConstructor_CreatesEmptyCollections()
    {
        // Act
        var graphData = new GraphData();

        // Assert
        Assert.NotNull(graphData.Nodes);
        Assert.Empty(graphData.Nodes);
        Assert.NotNull(graphData.Edges);
        Assert.Empty(graphData.Edges);
        Assert.Equal(string.Empty, graphData.SourceCode);
        Assert.Equal(string.Empty, graphData.GrammarName);
    }

    [Fact]
    public void GraphNode_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var node = new GraphNode();

        // Assert
        Assert.Equal(string.Empty, node.Id);
        Assert.Equal(string.Empty, node.Type);
        Assert.Equal(string.Empty, node.Name);
        Assert.False(node.IsAmbiguous);
        Assert.Equal(0, node.AlternativeCount);
        Assert.NotNull(node.Location);
        Assert.NotNull(node.Properties);
    }

    [Fact]
    public void GraphNode_CustomValues_SetsCorrectly()
    {
        // Act
        var node = new GraphNode
        {
            Id = "node_1",
            Type = "compilation_unit",
            Name = "root",
            IsAmbiguous = true,
            AlternativeCount = 3,
            Location = new CodeLocation
            {
                Line = 4,
                Column = 2,
                Offset = 40,
                Length = 12
            }
        };

        // Assert
        Assert.Equal("node_1", node.Id);
        Assert.Equal("compilation_unit", node.Type);
        Assert.Equal("root", node.Name);
        Assert.True(node.IsAmbiguous);
        Assert.Equal(3, node.AlternativeCount);
        Assert.Equal(4, node.Location.Line);
        Assert.Equal(2, node.Location.Column);
        Assert.Equal(40, node.Location.Offset);
        Assert.Equal(12, node.Location.Length);
    }

    [Fact]
    public void GraphEdge_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var edge = new GraphEdge();

        // Assert
        Assert.Equal(string.Empty, edge.Id);
        Assert.Equal(string.Empty, edge.Source);
        Assert.Equal(string.Empty, edge.Target);
        Assert.Equal(string.Empty, edge.Type);
        Assert.False(edge.IsAlternative);
        Assert.Equal(0, edge.PackedNodeIndex);
        Assert.Equal(0u, edge.RuleId);
    }

    [Fact]
    public void GraphEdge_CustomValues_SetsCorrectly()
    {
        // Act
        var edge = new GraphEdge
        {
            Id = "edge_1",
            Source = "1",
            Target = "2",
            Type = "hierarchy",
            IsAlternative = true,
            PackedNodeIndex = 1,
            RuleId = 100
        };

        // Assert
        Assert.Equal("edge_1", edge.Id);
        Assert.Equal("1", edge.Source);
        Assert.Equal("2", edge.Target);
        Assert.Equal("hierarchy", edge.Type);
        Assert.True(edge.IsAlternative);
        Assert.Equal(1, edge.PackedNodeIndex);
        Assert.Equal(100u, edge.RuleId);
    }

    [Fact]
    public void CodeLocation_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var location = new CodeLocation();

        // Assert
        Assert.Equal(1, location.Line);
        Assert.Equal(1, location.Column);
        Assert.Equal(0, location.Offset);
        Assert.Equal(0, location.Length);
    }

    [Fact]
    public void CodeLocation_CustomValues_SetsCorrectly()
    {
        // Act
        var location = new CodeLocation
        {
            Line = 5,
            Column = 10,
            Offset = 20,
            Length = 7
        };

        // Assert
        Assert.Equal(5, location.Line);
        Assert.Equal(10, location.Column);
        Assert.Equal(20, location.Offset);
        Assert.Equal(7, location.Length);
    }

    [Fact]
    public void NodeAmbiguityInfo_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var ambiguity = new NodeAmbiguityInfo();

        // Assert
        Assert.Equal(string.Empty, ambiguity.NodeId);
        Assert.False(ambiguity.IsAmbiguous);
        Assert.NotNull(ambiguity.Location);
        Assert.Equal(0, ambiguity.AlternativeCount);
        Assert.NotNull(ambiguity.PackedNodes);
        Assert.Empty(ambiguity.PackedNodes);
        Assert.Null(ambiguity.SelectedPackedNode);
    }

    [Fact]
    public void NodeAmbiguityInfo_AmbiguousNode_HasCorrectProperties()
    {
        // Act
        var ambiguity = new NodeAmbiguityInfo
        {
            NodeId = "5",
            IsAmbiguous = true,
            AlternativeCount = 2,
            Location = new CodeLocation
            {
                Line = 3,
                Column = 5,
                Offset = 20,
                Length = 10
            },
            PackedNodes = new List<PackedNodeInfo>
            {
                new PackedNodeInfo { Index = 0, RuleId = 101, RuleName = "method_declaration" },
                new PackedNodeInfo { Index = 1, RuleId = 102, RuleName = "function_expression" }
            }
        };

        // Assert
        Assert.Equal("5", ambiguity.NodeId);
        Assert.True(ambiguity.IsAmbiguous);
        Assert.Equal(2, ambiguity.AlternativeCount);
        Assert.Equal(3, ambiguity.Location.Line);
        Assert.Equal(5, ambiguity.Location.Column);
        Assert.Equal(20, ambiguity.Location.Offset);
        Assert.Equal(2, ambiguity.PackedNodes.Count);
    }

    [Fact]
    public void PackedNodeInfo_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var packedNode = new PackedNodeInfo();

        // Assert
        Assert.Equal(0, packedNode.Index);
        Assert.Equal(0, packedNode.RuleId);
        Assert.Equal(string.Empty, packedNode.RuleName);
        Assert.NotNull(packedNode.ChildNodeIds);
        Assert.Empty(packedNode.ChildNodeIds);
        Assert.True(packedNode.IsValid);
    }

    [Fact]
    public void PackedNodeInfo_CustomValues_SetsCorrectly()
    {
        // Act
        var packedNode = new PackedNodeInfo
        {
            Index = 1,
            RuleId = 102,
            RuleName = "function_expression",
            ChildNodeIds = new List<string> { "8", "9" },
            IsValid = true
        };

        // Assert
        Assert.Equal(1, packedNode.Index);
        Assert.Equal(102, packedNode.RuleId);
        Assert.Equal("function_expression", packedNode.RuleName);
        Assert.Equal(2, packedNode.ChildNodeIds.Count);
        Assert.True(packedNode.IsValid);
    }

    [Fact]
    public void CognitiveGraphVisualization_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var visualization = new CognitiveGraphVisualization();

        // Assert
        Assert.NotNull(visualization.GraphData);
        Assert.NotNull(visualization.AmbiguityPoints);
        Assert.Empty(visualization.AmbiguityPoints);
        Assert.Equal(VisualizationMode.ShowAllInterpretations, visualization.Options.Mode);
        Assert.False(visualization.HasAmbiguities);
        Assert.Equal(0, visualization.AmbiguityCount);
    }

    [Fact]
    public void CognitiveGraphVisualization_WithAmbiguities_HasCorrectProperties()
    {
        // Act
        var visualization = new CognitiveGraphVisualization
        {
            GraphData = new GraphData
            {
                Nodes = new List<GraphNode>
                {
                    new GraphNode { Id = "1", IsAmbiguous = true, AlternativeCount = 2 }
                }
            },
            AmbiguityPoints = new List<NodeAmbiguityInfo>
            {
                new NodeAmbiguityInfo { NodeId = "1", IsAmbiguous = true, AlternativeCount = 2 }
            },
            Options = new VisualizationOptions { Mode = VisualizationMode.ShowAllInterpretations }
        };

        // Assert
        Assert.True(visualization.HasAmbiguities);
        Assert.Equal(1, visualization.AmbiguityCount);
        Assert.Equal(VisualizationMode.ShowAllInterpretations, visualization.Options.Mode);
    }

    [Fact]
    public void VisualizationMode_Values_AreCorrect()
    {
        // Assert
        Assert.Equal(0, (int)VisualizationMode.ShowAllInterpretations);
        Assert.Equal(1, (int)VisualizationMode.ShowSelectedInterpretation);
        Assert.Equal(2, (int)VisualizationMode.ShowAmbiguityOnly);
    }

    [Fact]
    public void GraphData_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new GraphData
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar",
            Nodes = new List<GraphNode>
            {
                new GraphNode
                {
                    Id = "1",
                    Type = "compilation_unit",
                    Name = "root",
                    IsAmbiguous = false,
                    AlternativeCount = 0,
                    Location = new CodeLocation
                    {
                        Line = 1,
                        Column = 1,
                        Offset = 0,
                        Length = 9
                    }
                }
            },
            Edges = new List<GraphEdge>
            {
                new GraphEdge
                {
                    Id = "1-2",
                    Source = "1",
                    Target = "2",
                    Type = "hierarchy",
                    IsAlternative = false,
                    PackedNodeIndex = 0,
                    RuleId = 100
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<GraphData>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.SourceCode, deserialized.SourceCode);
        Assert.Equal(original.GrammarName, deserialized.GrammarName);
        Assert.Single(deserialized.Nodes);
        Assert.Single(deserialized.Edges);
        Assert.Equal("compilation_unit", deserialized.Nodes[0].Type);
        Assert.False(deserialized.Nodes[0].IsAmbiguous);
    }

    [Fact]
    public void CognitiveGraphVisualization_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new CognitiveGraphVisualization
        {
            GraphData = new GraphData
            {
                SourceCode = "test code",
                GrammarName = "TestGrammar"
            },
            AmbiguityPoints = new List<NodeAmbiguityInfo>
            {
                new NodeAmbiguityInfo
                {
                    NodeId = "1",
                    IsAmbiguous = true,
                    AlternativeCount = 2,
                    PackedNodes = new List<PackedNodeInfo>
                    {
                        new PackedNodeInfo { Index = 0, RuleId = 101, RuleName = "rule1" },
                        new PackedNodeInfo { Index = 1, RuleId = 102, RuleName = "rule2" }
                    }
                }
            },
            Options = new VisualizationOptions { Mode = VisualizationMode.ShowAllInterpretations }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CognitiveGraphVisualization>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.GraphData.SourceCode, deserialized.GraphData.SourceCode);
        Assert.Equal(original.GraphData.GrammarName, deserialized.GraphData.GrammarName);
        Assert.Single(deserialized.AmbiguityPoints);
        Assert.True(deserialized.HasAmbiguities);
        Assert.Equal(1, deserialized.AmbiguityCount);
        Assert.Equal(2, deserialized.AmbiguityPoints[0].AlternativeCount);
    }

    [Fact]
    public void InterpretationPath_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var path = new InterpretationPath();

        // Assert
        Assert.NotNull(path.NodeChoices);
        Assert.Empty(path.NodeChoices);
        Assert.Equal(0, path.AmbiguityCount);
    }

    [Fact]
    public void InterpretationPath_WithChoices_HasCorrectProperties()
    {
        // Act
        var path = new InterpretationPath
        {
            NodeChoices = new Dictionary<string, int>
            {
                ["node_5"] = 0,
                ["node_10"] = 1
            }
        };

        // Assert
        Assert.Equal(2, path.NodeChoices.Count);
        Assert.Equal(0, path.NodeChoices["node_5"]);
        Assert.Equal(1, path.NodeChoices["node_10"]);
        Assert.Equal(2, path.AmbiguityCount);
    }

    [Fact]
    public void InterpretationPath_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var path = new InterpretationPath
        {
            NodeChoices = new Dictionary<string, int> { ["node_1"] = 1 }
        };

        // Act
        var clone = path.Clone();
        clone.NodeChoices["node_1"] = 0;

        // Assert
        Assert.Equal(1, path.NodeChoices["node_1"]);
        Assert.Equal(0, clone.NodeChoices["node_1"]);
    }
}
