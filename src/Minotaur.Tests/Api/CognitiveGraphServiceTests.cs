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
using Minotaur.Core;
using Minotaur.Editor;
using Minotaur.UI.Blazor.Api.Services;
using Minotaur.UI.Blazor.Api.GraphQL;

namespace Minotaur.Tests.Api;

public class CognitiveGraphServiceTests
{
    private CognitiveGraphService CreateService()
    {
        var rootNode = new NonTerminalNode("TestRoot", 0);
        var graphEditor = new GraphEditor(rootNode);
        return new CognitiveGraphService(graphEditor);
    }

    private CognitiveGraphNode CreateTestGraph()
    {
        var root = new NonTerminalNode("Program", 0);

        var classNode = new NonTerminalNode("ClassDeclaration", 1);
        var identifier = new IdentifierNode("TestClass");
        var methodNode = new NonTerminalNode("MethodDeclaration", 2);
        var methodName = new IdentifierNode("TestMethod");
        var literal = new LiteralNode("42", "integer", 42);
        var terminal = new TerminalNode("return", "keyword");

        root.AddChild(classNode);
        classNode.AddChild(identifier);
        classNode.AddChild(methodNode);
        methodNode.AddChild(methodName);
        methodNode.AddChild(terminal);
        methodNode.AddChild(literal);

        return root;
    }

    [Fact]
    public void StoreCognitiveGraph_ShouldReturnValidGraphId()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();

        // Act
        var graphId = service.StoreCognitiveGraph(testGraph);

        // Assert
        Assert.NotNull(graphId);
        Assert.NotEmpty(graphId);
        Assert.True(Guid.TryParse(graphId, out _));
    }

    [Fact]
    public async Task QueryCognitiveGraphAsync_WithValidGraphId_ShouldReturnNodes()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        var graphId = service.StoreCognitiveGraph(testGraph);

        var query = new CognitiveGraphQuery
        {
            MaxDepth = 2,
            IncludeMetadata = false,
            IncludeSourcePosition = true
        };

        // Act
        var result = await service.QueryCognitiveGraphAsync(graphId, query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Nodes);
        Assert.True(result.TotalNodes > 0);
        Assert.True(result.Nodes.Count <= 100); // Respects max limit
    }

    [Fact]
    public async Task QueryCognitiveGraphAsync_WithMaxDepthLimit_ShouldRespectDepth()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        var graphId = service.StoreCognitiveGraph(testGraph);

        var query = new CognitiveGraphQuery
        {
            MaxDepth = 1, // Only root + 1 level
            IncludeMetadata = false
        };

        // Act
        var result = await service.QueryCognitiveGraphAsync(graphId, query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Nodes);

        // Should only contain nodes up to depth 1
        var maxDepthInResult = result.Nodes
            .Select(node => CalculateDepthFromRoot(result.Nodes, node.Id))
            .DefaultIfEmpty(0)
            .Max();
        Assert.True(maxDepthInResult <= 1);
    }

    [Fact]
    public async Task QueryCognitiveGraphAsync_WithNodeTypeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        var graphId = service.StoreCognitiveGraph(testGraph);

        var query = new CognitiveGraphQuery
        {
            MaxDepth = 5,
            NodeTypes = new List<string> { "identifier" }
        };

        // Act
        var result = await service.QueryCognitiveGraphAsync(graphId, query);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Nodes, node => Assert.Equal("identifier", node.NodeType));
    }

    [Fact]
    public async Task GetNodeAsync_WithValidNodeId_ShouldReturnNode()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        service.StoreCognitiveGraph(testGraph);
        var nodeId = testGraph.Id.ToString();

        // Act
        var result = await service.GetNodeAsync(nodeId, includeMetadata: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        Assert.Equal("nonterminal", result.NodeType);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public async Task SearchNodesAsync_WithValidSearchTerm_ShouldReturnMatchingNodes()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        var graphId = service.StoreCognitiveGraph(testGraph);

        // Act
        var result = await service.SearchNodesAsync(graphId, "Test", limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, node =>
            Assert.True(node.DisplayText?.ToLowerInvariant().Contains("test") == true ||
                       node.NodeType.ToLowerInvariant().Contains("test")));
    }

    [Fact]
    public async Task SearchNodesAsync_WithLimitParameter_ShouldRespectLimit()
    {
        // Arrange
        var service = CreateService();
        var testGraph = CreateTestGraph();
        var graphId = service.StoreCognitiveGraph(testGraph);

        // Act
        var result = await service.SearchNodesAsync(graphId, "Test", limit: 2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 2);
    }

    private int CalculateDepthFromRoot(List<CognitiveGraphNodeDto> nodes, string nodeId)
    {
        var nodeMap = nodes.ToDictionary(n => n.Id, n => n);
        var currentNode = nodeMap.GetValueOrDefault(nodeId);
        int depth = 0;

        while (currentNode?.ParentId != null && nodeMap.TryGetValue(currentNode.ParentId, out var parentNode))
        {
            depth++;
            currentNode = parentNode;
        }

        return depth;
    }
}