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

using Minotaur.Core.Models.Visualization;
using System.Text.Json;
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
        Assert.Null(node.FullName);
        Assert.NotNull(node.Properties);
        Assert.Empty(node.Properties);
        Assert.Null(node.Group);
        Assert.Equal(10, node.Size);
        Assert.False(node.IsAmbiguous);
        Assert.Equal(0, node.AlternativeCount);
        Assert.NotNull(node.Location);
    }

    [Fact]
    public void GraphNode_AmbiguousNode_HasCorrectProperties()
    {
        // Act
        var node = new GraphNode
        {
            Id = "1",
            Type = "expression",
            Name = "a+b",
            IsAmbiguous = true,
            AlternativeCount = 2,
            Size = 15
        };

        // Assert
        Assert.Equal("1", node.Id);
        Assert.Equal("expression", node.Type);
        Assert.Equal("a+b", node.Name);
        Assert.True(node.IsAmbiguous);
        Assert.Equal(2, node.AlternativeCount);
        Assert.Equal(15, node.Size);
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
        Assert.Equal(1, edge.Weight);
        Assert.NotNull(edge.Properties);
        Assert.Empty(edge.Properties);
        Assert.False(edge.IsAlternative);
        Assert.Equal(0, edge.PackedNodeIndex);
        Assert.Equal(0u, edge.RuleId);
    }

    [Fact]
    public void GraphEdge_AlternativeEdge_HasCorrectProperties()
    {
        // Act
        var edge = new GraphEdge
        {
            Id = "1-2-0",
            Source = "1",
            Target = "2",
            Type = "alternative",
            IsAlternative = true,
            PackedNodeIndex = 0,
            RuleId = 101
        };

        // Assert
        Assert.Equal("1-2-0", edge.Id);
        Assert.Equal("1", edge.Source);
        Assert.Equal("2", edge.Target);
        Assert.Equal("alternative", edge.Type);
        Assert.True(edge.IsAlternative);
        Assert.Equal(0, edge.PackedNodeIndex);
        Assert.Equal(101u, edge.RuleId);
    }

    [Fact]
    public void CodeLocation_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var location = new CodeLocation();

        // Assert
        Assert.NotNull(location.Start);
        Assert.NotNull(location.End);
    }

    [Fact]
    public void Position_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var position = new Position();

        // Assert
        Assert.Equal(1, position.Line);
        Assert.Equal(1, position.Column);
        Assert.Equal(0, position.Offset);
    }

    [Fact]
    public void Position_CustomValues_SetsCorrectly()
    {
        // Act
        var position = new Position
        {
            Line = 5,
            Column = 10,
            Offset = 20
        };

        // Assert
        Assert.Equal(5, position.Line);
        Assert.Equal(10, position.Column);
        Assert.Equal(20, position.Offset);
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
                Start = new Position { Line = 3, Column = 5, Offset = 20 },
                End = new Position { Line = 3, Column = 15, Offset = 30 }
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
        Assert.Equal(3, ambiguity.Location.Start.Line);
        Assert.Equal(5, ambiguity.Location.Start.Column);
        Assert.Equal(20, ambiguity.Location.Start.Offset);
        Assert.Equal(2, ambiguity.PackedNodes.Count);
    }

    [Fact]
    public void PackedNodeInfo_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var packedNode = new PackedNodeInfo();

        // Assert
        Assert.Equal(0, packedNode.Index);
        Assert.Equal(0u, packedNode.RuleId);
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
        Assert.Equal(102u, packedNode.RuleId);
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
        Assert.NotNull(visualization.Ambiguities);
        Assert.Empty(visualization.Ambiguities);
        Assert.Equal(VisualizationMode.ShowAllInterpretations, visualization.Mode);
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
            Ambiguities = new Dictionary<string, NodeAmbiguityInfo>
            {
                ["1"] = new NodeAmbiguityInfo { NodeId = "1", IsAmbiguous = true, AlternativeCount = 2 }
            },
            Mode = VisualizationMode.ShowAllInterpretations
        };

        // Assert
        Assert.True(visualization.HasAmbiguities);
        Assert.Equal(1, visualization.AmbiguityCount);
        Assert.Equal(VisualizationMode.ShowAllInterpretations, visualization.Mode);
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
                        Start = new Position { Line = 1, Column = 1, Offset = 0 },
                        End = new Position { Line = 1, Column = 10, Offset = 9 }
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
            Ambiguities = new Dictionary<string, NodeAmbiguityInfo>
            {
                ["1"] = new NodeAmbiguityInfo
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
            Mode = VisualizationMode.ShowAllInterpretations
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CognitiveGraphVisualization>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.GraphData.SourceCode, deserialized.GraphData.SourceCode);
        Assert.Equal(original.GraphData.GrammarName, deserialized.GraphData.GrammarName);
        Assert.Single(deserialized.Ambiguities);
        Assert.True(deserialized.HasAmbiguities);
        Assert.Equal(1, deserialized.AmbiguityCount);
        Assert.Equal(2, deserialized.Ambiguities["1"].AlternativeCount);
    }

    [Fact]
    public void InterpretationPath_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var path = new InterpretationPath();

        // Assert
        Assert.Equal(string.Empty, path.Id);
        Assert.NotNull(path.Choices);
        Assert.Empty(path.Choices);
        Assert.NotNull(path.AppliedRules);
        Assert.Empty(path.AppliedRules);
        Assert.True(path.IsValid);
    }

    [Fact]
    public void InterpretationPath_WithChoices_HasCorrectProperties()
    {
        // Act
        var path = new InterpretationPath
        {
            Id = "path_0",
            Choices = new Dictionary<ulong, int>
            {
                [5] = 0,
                [10] = 1
            },
            AppliedRules = new List<string> { "rule1", "rule2" },
            IsValid = true
        };

        // Assert
        Assert.Equal("path_0", path.Id);
        Assert.Equal(2, path.Choices.Count);
        Assert.Equal(0, path.Choices[5]);
        Assert.Equal(1, path.Choices[10]);
        Assert.Equal(2, path.AppliedRules.Count);
        Assert.True(path.IsValid);
    }

    [Fact]
    public void VisualizationOptions_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var options = new VisualizationOptions();

        // Assert
        Assert.True(options.ShowAllAlternatives);
        Assert.True(options.HighlightAmbiguities);
        Assert.Equal(VisualizationMode.ShowAllInterpretations, options.Mode);
        Assert.Equal("default", options.ColorScheme);
    }

    [Fact]
    public void VisualizationOptions_CustomValues_SetsCorrectly()
    {
        // Act
        var options = new VisualizationOptions
        {
            ShowAllAlternatives = false,
            HighlightAmbiguities = false,
            Mode = VisualizationMode.ShowAmbiguityOnly,
            ColorScheme = "dark"
        };

        // Assert
        Assert.False(options.ShowAllAlternatives);
        Assert.False(options.HighlightAmbiguities);
        Assert.Equal(VisualizationMode.ShowAmbiguityOnly, options.Mode);
        Assert.Equal("dark", options.ColorScheme);
    }
}
