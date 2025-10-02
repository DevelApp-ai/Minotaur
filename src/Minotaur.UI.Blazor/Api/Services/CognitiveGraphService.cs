using Minotaur.Core;
using Minotaur.Editor;
using Minotaur.UI.Blazor.Api.GraphQL;
using System.Collections.Concurrent;

namespace Minotaur.UI.Blazor.Api.Services;

/// <summary>
/// Service for managing cognitive graph data efficiently for API access
/// </summary>
public class CognitiveGraphService
{
    private readonly ConcurrentDictionary<string, CognitiveGraphNode> _nodeCache = new();
    private readonly ConcurrentDictionary<string, List<CognitiveGraphEdgeDto>> _edgeCache = new();
    private readonly GraphEditor _graphEditor;

    public CognitiveGraphService(GraphEditor graphEditor)
    {
        _graphEditor = graphEditor;
    }

    /// <summary>
    /// Store a cognitive graph and return its ID for efficient reference
    /// </summary>
    public string StoreCognitiveGraph(CognitiveGraphNode rootNode)
    {
        var graphId = Guid.NewGuid().ToString();
        CacheGraphNodes(rootNode, graphId);
        return graphId;
    }

    /// <summary>
    /// Query cognitive graph data with efficient pagination and filtering
    /// </summary>
    public async Task<CognitiveGraphResponse> QueryCognitiveGraphAsync(string graphId, CognitiveGraphQuery query)
    {
        var response = new CognitiveGraphResponse();
        
        // Find starting node
        var startNode = string.IsNullOrEmpty(query.NodeId) 
            ? FindRootNode(graphId)
            : _nodeCache.Values.FirstOrDefault(n => n.Id.ToString() == query.NodeId);

        if (startNode == null)
        {
            return response;
        }

        // Build response with limited depth
        var visitedNodes = new HashSet<string>();
        var nodesToProcess = new Queue<(CognitiveGraphNode node, int depth)>();
        nodesToProcess.Enqueue((startNode, 0));

        while (nodesToProcess.Count > 0 && response.Nodes.Count < 100) // Limit to 100 nodes per query
        {
            var (currentNode, depth) = nodesToProcess.Dequeue();
            
            if (depth > query.MaxDepth || visitedNodes.Contains(currentNode.Id.ToString()))
                continue;

            visitedNodes.Add(currentNode.Id.ToString());

            // Apply node type filter
            if (query.NodeTypes != null && query.NodeTypes.Count > 0 && 
                !query.NodeTypes.Contains(currentNode.NodeType))
                continue;

            // Convert to DTO
            var nodeDto = ConvertToDto(currentNode, query.IncludeMetadata, query.IncludeSourcePosition);
            response.Nodes.Add(nodeDto);

            // Add children to processing queue
            foreach (var child in currentNode.Children)
            {
                nodesToProcess.Enqueue((child, depth + 1));
                
                // Add edge
                response.Edges.Add(new CognitiveGraphEdgeDto
                {
                    FromNodeId = currentNode.Id.ToString(),
                    ToNodeId = child.Id.ToString(),
                    EdgeType = "parent-child"
                });
            }
        }

        response.TotalNodes = CountTotalNodes(startNode);
        response.HasMore = response.Nodes.Count < response.TotalNodes;

        return response;
    }

    /// <summary>
    /// Get a specific node with its immediate children
    /// </summary>
    public async Task<CognitiveGraphNodeDto?> GetNodeAsync(string nodeId, bool includeMetadata = false)
    {
        var node = _nodeCache.Values.FirstOrDefault(n => n.Id.ToString() == nodeId);
        return node != null ? ConvertToDto(node, includeMetadata, true) : null;
    }

    /// <summary>
    /// Search nodes by text content or type
    /// </summary>
    public async Task<List<CognitiveGraphNodeDto>> SearchNodesAsync(string graphId, string searchTerm, int limit = 20)
    {
        var results = new List<CognitiveGraphNodeDto>();
        var searchLower = searchTerm.ToLowerInvariant();

        foreach (var node in _nodeCache.Values.Take(limit))
        {
            bool matches = false;
            
            // Search in node type
            if (node.NodeType.ToLowerInvariant().Contains(searchLower))
                matches = true;
            
            // Search in terminal node text
            if (node is TerminalNode terminal && 
                terminal.Text.ToLowerInvariant().Contains(searchLower))
                matches = true;
            
            // Search in identifier node name
            if (node is IdentifierNode identifier && 
                identifier.Text.ToLowerInvariant().Contains(searchLower))
                matches = true;

            if (matches)
            {
                results.Add(ConvertToDto(node, false, true));
            }
        }

        return results;
    }

    private void CacheGraphNodes(CognitiveGraphNode node, string graphId)
    {
        _nodeCache[node.Id.ToString()] = node;
        
        foreach (var child in node.Children)
        {
            CacheGraphNodes(child, graphId);
        }
    }

    private CognitiveGraphNode? FindRootNode(string graphId)
    {
        // Find a node that has no parent
        return _nodeCache.Values.FirstOrDefault(n => n.Parent == null);
    }

    private CognitiveGraphNodeDto ConvertToDto(CognitiveGraphNode node, bool includeMetadata, bool includeSourcePosition)
    {
        var dto = new CognitiveGraphNodeDto
        {
            Id = node.Id.ToString(),
            NodeType = node.NodeType,
            ChildIds = node.Children.Select(c => c.Id.ToString()).ToList(),
            ParentId = node.Parent?.Id.ToString()
        };

        // Set display text based on node type
        dto.DisplayText = node switch
        {
            TerminalNode terminal => terminal.Text,
            NonTerminalNode nonTerminal => nonTerminal.RuleName,
            IdentifierNode identifier => identifier.FullName,
            LiteralNode literal => literal.Value?.ToString(),
            _ => node.NodeType
        };

        if (includeMetadata)
        {
            dto.Metadata = new Dictionary<string, object>(node.Metadata);
        }

        if (includeSourcePosition && node.SourcePosition != null)
        {
            dto.SourcePosition = new SourcePositionDto
            {
                Line = node.SourcePosition.Line,
                Column = node.SourcePosition.Column,
                Offset = node.SourcePosition.Offset,
                Length = node.SourcePosition.Length,
                SourceFile = node.SourcePosition.SourceFile
            };
        }

        return dto;
    }

    private int CountTotalNodes(CognitiveGraphNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountTotalNodes(child);
        }
        return count;
    }
}