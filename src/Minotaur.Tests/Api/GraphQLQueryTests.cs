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

using Xunit;
using Minotaur.UI.Blazor.Api.GraphQL;

namespace Minotaur.Tests.Api;

public class GraphQLQueryTests
{
    [Fact]
    public void CognitiveGraphQuery_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var query = new CognitiveGraphQuery();

        // Assert
        Assert.Equal(3, query.MaxDepth);
        Assert.Null(query.NodeId);
        Assert.Null(query.NodeTypes);
        Assert.False(query.IncludeMetadata);
        Assert.True(query.IncludeSourcePosition);
    }

    [Fact]
    public void CognitiveGraphNodeDto_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var nodeDto = new CognitiveGraphNodeDto();

        // Assert
        Assert.Equal(string.Empty, nodeDto.Id);
        Assert.Equal(string.Empty, nodeDto.NodeType);
        Assert.Null(nodeDto.DisplayText);
        Assert.NotNull(nodeDto.Metadata);
        Assert.Empty(nodeDto.Metadata);
        Assert.NotNull(nodeDto.ChildIds);
        Assert.Empty(nodeDto.ChildIds);
        Assert.Null(nodeDto.ParentId);
        Assert.Null(nodeDto.SourcePosition);
    }

    [Fact]
    public void SourcePositionDto_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var sourcePos = new SourcePositionDto
        {
            Line = 10,
            Column = 5,
            Offset = 123,
            Length = 8,
            SourceFile = "test.cs"
        };

        // Assert
        Assert.Equal(10, sourcePos.Line);
        Assert.Equal(5, sourcePos.Column);
        Assert.Equal(123, sourcePos.Offset);
        Assert.Equal(8, sourcePos.Length);
        Assert.Equal("test.cs", sourcePos.SourceFile);
    }

    [Fact]
    public void CognitiveGraphResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new CognitiveGraphResponse();

        // Assert
        Assert.NotNull(response.Nodes);
        Assert.Empty(response.Nodes);
        Assert.NotNull(response.Edges);
        Assert.Empty(response.Edges);
        Assert.Equal(0, response.TotalNodes);
        Assert.False(response.HasMore);
    }

    [Fact]
    public void CognitiveGraphEdgeDto_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var edge = new CognitiveGraphEdgeDto();

        // Assert
        Assert.Equal(string.Empty, edge.FromNodeId);
        Assert.Equal(string.Empty, edge.ToNodeId);
        Assert.Equal(string.Empty, edge.EdgeType);
        Assert.NotNull(edge.Properties);
        Assert.Empty(edge.Properties);
    }

    [Fact]
    public void CognitiveGraphEdgeDto_WithData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var edge = new CognitiveGraphEdgeDto
        {
            FromNodeId = "node1",
            ToNodeId = "node2",
            EdgeType = "parent-child",
            Properties = new Dictionary<string, object>
            {
                { "weight", 1.5 },
                { "label", "contains" }
            }
        };

        // Assert
        Assert.Equal("node1", edge.FromNodeId);
        Assert.Equal("node2", edge.ToNodeId);
        Assert.Equal("parent-child", edge.EdgeType);
        Assert.Equal(2, edge.Properties.Count);
        Assert.Equal(1.5, edge.Properties["weight"]);
        Assert.Equal("contains", edge.Properties["label"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CognitiveGraphQuery_MaxDepth_ShouldAcceptValidValues(int maxDepth)
    {
        // Arrange & Act
        var query = new CognitiveGraphQuery { MaxDepth = maxDepth };

        // Assert
        Assert.Equal(maxDepth, query.MaxDepth);
    }

    [Fact]
    public void CognitiveGraphQuery_WithNodeTypes_ShouldFilterCorrectly()
    {
        // Arrange
        var nodeTypes = new List<string> { "terminal", "nonterminal", "identifier" };

        // Act
        var query = new CognitiveGraphQuery
        {
            NodeTypes = nodeTypes,
            MaxDepth = 5
        };

        // Assert
        Assert.NotNull(query.NodeTypes);
        Assert.Equal(3, query.NodeTypes.Count);
        Assert.Contains("terminal", query.NodeTypes);
        Assert.Contains("nonterminal", query.NodeTypes);
        Assert.Contains("identifier", query.NodeTypes);
    }

    [Fact]
    public void CognitiveGraphNodeDto_WithCompleteData_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var node = new CognitiveGraphNodeDto
        {
            Id = "node123",
            NodeType = "identifier",
            DisplayText = "TestVariable",
            Metadata = new Dictionary<string, object>
            {
                { "namespace", "TestNamespace" },
                { "isPublic", true }
            },
            ChildIds = new List<string> { "child1", "child2" },
            ParentId = "parent1",
            SourcePosition = new SourcePositionDto
            {
                Line = 15,
                Column = 10,
                Offset = 250,
                Length = 12,
                SourceFile = "example.cs"
            }
        };

        // Assert
        Assert.Equal("node123", node.Id);
        Assert.Equal("identifier", node.NodeType);
        Assert.Equal("TestVariable", node.DisplayText);
        Assert.Equal(2, node.Metadata.Count);
        Assert.Equal("TestNamespace", node.Metadata["namespace"]);
        Assert.Equal(true, node.Metadata["isPublic"]);
        Assert.Equal(2, node.ChildIds.Count);
        Assert.Contains("child1", node.ChildIds);
        Assert.Contains("child2", node.ChildIds);
        Assert.Equal("parent1", node.ParentId);
        Assert.NotNull(node.SourcePosition);
        Assert.Equal(15, node.SourcePosition.Line);
        Assert.Equal("example.cs", node.SourcePosition.SourceFile);
    }
}