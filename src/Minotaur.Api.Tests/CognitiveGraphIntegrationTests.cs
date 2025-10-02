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

namespace Minotaur.Api.Tests;

/// <summary>
/// Integration tests for CognitiveGraph with API concepts
/// </summary>
public class CognitiveGraphIntegrationTests
{
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
    public void CognitiveGraph_Creation_ShouldWork()
    {
        // Arrange & Act
        var graph = CreateTestGraph();

        // Assert
        Assert.NotNull(graph);
        Assert.Equal("Program", ((NonTerminalNode)graph).RuleName);
        Assert.Single(graph.Children); // Should have one ClassDeclaration
        
        var classNode = graph.Children.First() as NonTerminalNode;
        Assert.NotNull(classNode);
        Assert.Equal("ClassDeclaration", classNode.RuleName);
        Assert.Equal(2, classNode.Children.Count); // Identifier + Method
    }

    [Fact]
    public void CognitiveGraph_ToApiDto_Concept_ShouldWork()
    {
        // Arrange
        var graph = CreateTestGraph();
        var allNodes = new List<CognitiveGraphNode>();
        CollectAllNodes(graph, allNodes);

        // Act - Convert to API DTO format (simulate)
        var dtoNodes = allNodes.Select(node => new
        {
            Id = node.Id.ToString(),
            NodeType = node.NodeType,
            DisplayText = GetDisplayText(node),
            ChildIds = node.Children.Select(c => c.Id.ToString()).ToList(),
            ParentId = node.Parent?.Id.ToString()
        }).ToList();

        // Assert
        Assert.Equal(6, dtoNodes.Count); // Program + Class + Identifier + Method + MethodName + return + 42
        
        var rootDto = dtoNodes.First(n => n.NodeType == "nonterminal" && n.DisplayText == "Program");
        Assert.NotNull(rootDto);
        Assert.Null(rootDto.ParentId);
        Assert.Single(rootDto.ChildIds);
        
        var identifierDtos = dtoNodes.Where(n => n.NodeType == "identifier").ToList();
        Assert.Equal(2, identifierDtos.Count); // TestClass + TestMethod
    }

    [Fact]
    public void CognitiveGraph_DepthTraversal_ShouldWork()
    {
        // Arrange
        var graph = CreateTestGraph();
        
        // Act - Simulate depth-limited traversal
        var nodesAtDepth = new Dictionary<int, List<CognitiveGraphNode>>();
        TraverseWithDepth(graph, 0, nodesAtDepth, maxDepth: 2);

        // Assert
        Assert.True(nodesAtDepth.ContainsKey(0)); // Root level
        Assert.True(nodesAtDepth.ContainsKey(1)); // Class level  
        Assert.True(nodesAtDepth.ContainsKey(2)); // Identifier + Method level
        Assert.False(nodesAtDepth.ContainsKey(3)); // Should be limited by maxDepth

        Assert.Single(nodesAtDepth[0]); // Only Program
        Assert.Single(nodesAtDepth[1]); // Only ClassDeclaration
        Assert.Equal(2, nodesAtDepth[2].Count); // Identifier + MethodDeclaration
    }

    [Fact]
    public void CognitiveGraph_NodeTypeFiltering_ShouldWork()
    {
        // Arrange
        var graph = CreateTestGraph();
        var allNodes = new List<CognitiveGraphNode>();
        CollectAllNodes(graph, allNodes);

        // Act - Filter by node type (simulate GraphQL filtering)
        var identifierNodes = allNodes.Where(n => n.NodeType == "identifier").ToList();
        var terminalNodes = allNodes.Where(n => n.NodeType == "terminal").ToList();
        var nonterminalNodes = allNodes.Where(n => n.NodeType == "nonterminal").ToList();

        // Assert
        Assert.Equal(2, identifierNodes.Count); // TestClass + TestMethod
        Assert.Equal(2, terminalNodes.Count); // return + 42 (literal extends terminal)
        Assert.Equal(3, nonterminalNodes.Count); // Program + ClassDeclaration + MethodDeclaration
    }

    [Fact]
    public void CognitiveGraph_SearchByContent_ShouldWork()
    {
        // Arrange
        var graph = CreateTestGraph();
        var allNodes = new List<CognitiveGraphNode>();
        CollectAllNodes(graph, allNodes);

        // Act - Simulate content-based search
        var searchTerm = "test";
        var matchingNodes = allNodes.Where(node => 
            GetDisplayText(node).ToLowerInvariant().Contains(searchTerm) ||
            node.NodeType.ToLowerInvariant().Contains(searchTerm)
        ).ToList();

        // Assert
        Assert.Equal(2, matchingNodes.Count); // TestClass + TestMethod
        Assert.All(matchingNodes, node => 
            Assert.Contains(searchTerm, GetDisplayText(node).ToLowerInvariant()));
    }

    [Fact]
    public void CognitiveGraph_ApiPagination_Concept_ShouldWork()
    {
        // Arrange - Create a larger graph
        var root = new NonTerminalNode("LargeProgram", 0);
        for (int i = 0; i < 150; i++) // Create 150 child nodes
        {
            var child = new IdentifierNode($"Variable{i}");
            root.AddChild(child);
        }

        var allNodes = new List<CognitiveGraphNode>();
        CollectAllNodes(root, allNodes);

        // Act - Simulate API pagination (max 100 results)
        const int maxResults = 100;
        var paginatedResults = allNodes.Take(maxResults).ToList();

        // Assert
        Assert.Equal(maxResults, paginatedResults.Count);
        Assert.True(allNodes.Count > maxResults); // Ensure we actually have more data
        Assert.True(paginatedResults.Count <= maxResults); // Ensure pagination limit is respected
    }

    [Fact]
    public void CognitiveGraph_WithGraphEditor_ShouldIntegrateCorrectly()
    {
        // Arrange
        var graph = CreateTestGraph();
        var graphEditor = new GraphEditor(graph);

        // Act - Add a new node via GraphEditor
        var newIdentifier = new IdentifierNode("NewVariable");
        var operation = graphEditor.InsertNode(graph.Children.First().Id, newIdentifier);

        // Assert
        Assert.NotNull(operation);
        Assert.True(graphEditor.CanUndo);
        
        // Verify the graph has been modified
        var updatedRoot = graphEditor.Root;
        Assert.NotNull(updatedRoot);
        
        // The new structure should be accessible for API conversion
        var allNodes = new List<CognitiveGraphNode>();
        CollectAllNodes(updatedRoot, allNodes);
        Assert.Contains(allNodes, n => n is IdentifierNode id && id.Text == "NewVariable");
    }

    [Fact]
    public void CognitiveGraph_PerformanceMetrics_ShouldBeCollectable()
    {
        // Arrange
        var graph = CreateTestGraph();
        var allNodes = new List<CognitiveGraphNode>();
        
        // Act - Collect performance metrics
        var startTime = DateTime.UtcNow;
        CollectAllNodes(graph, allNodes);
        var endTime = DateTime.UtcNow;
        
        var metrics = new
        {
            TotalNodes = allNodes.Count,
            TraversalTime = endTime - startTime,
            NodeTypeDistribution = allNodes.GroupBy(n => n.NodeType)
                .ToDictionary(g => g.Key, g => g.Count()),
            MaxDepth = CalculateMaxDepth(graph),
            MemoryFootprint = allNodes.Count * 64 // Rough estimate
        };

        // Assert
        Assert.True(metrics.TotalNodes > 0);
        Assert.True(metrics.TraversalTime.TotalMilliseconds >= 0);
        Assert.Contains("nonterminal", metrics.NodeTypeDistribution.Keys);
        Assert.Contains("identifier", metrics.NodeTypeDistribution.Keys);
        Assert.True(metrics.MaxDepth > 0);
        Assert.True(metrics.MemoryFootprint > 0);
    }

    // Helper methods
    private void CollectAllNodes(CognitiveGraphNode node, List<CognitiveGraphNode> collector)
    {
        collector.Add(node);
        foreach (var child in node.Children)
        {
            CollectAllNodes(child, collector);
        }
    }

    private void TraverseWithDepth(CognitiveGraphNode node, int currentDepth, 
        Dictionary<int, List<CognitiveGraphNode>> depthMap, int maxDepth)
    {
        if (currentDepth > maxDepth) return;

        if (!depthMap.ContainsKey(currentDepth))
            depthMap[currentDepth] = new List<CognitiveGraphNode>();
        
        depthMap[currentDepth].Add(node);

        foreach (var child in node.Children)
        {
            TraverseWithDepth(child, currentDepth + 1, depthMap, maxDepth);
        }
    }

    private string GetDisplayText(CognitiveGraphNode node)
    {
        return node switch
        {
            IdentifierNode identifier => identifier.Text,
            LiteralNode literal => literal.Value?.ToString() ?? "",
            TerminalNode terminal => terminal.Text,
            NonTerminalNode nonTerminal => nonTerminal.RuleName,
            _ => node.NodeType
        };
    }

    private int CalculateMaxDepth(CognitiveGraphNode node, int currentDepth = 0)
    {
        if (!node.Children.Any()) return currentDepth;
        
        return node.Children.Max(child => CalculateMaxDepth(child, currentDepth + 1));
    }
}