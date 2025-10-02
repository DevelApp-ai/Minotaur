using Minotaur.UI.Blazor.Api.Services;

namespace Minotaur.UI.Blazor.Api.GraphQL;

/// <summary>
/// GraphQL Query root for cognitive graph operations
/// </summary>
public class Query
{
    /// <summary>
    /// Query cognitive graph data with efficient filtering and pagination
    /// </summary>
    public async Task<CognitiveGraphResponse> GetCognitiveGraph(
        [Service] CognitiveGraphService cognitiveGraphService,
        string graphId,
        string? nodeId = null,
        int maxDepth = 3,
        List<string>? nodeTypes = null,
        bool includeMetadata = false,
        bool includeSourcePosition = true)
    {
        var query = new CognitiveGraphQuery
        {
            NodeId = nodeId,
            MaxDepth = Math.Min(maxDepth, 10), // Cap at 10 levels
            NodeTypes = nodeTypes,
            IncludeMetadata = includeMetadata,
            IncludeSourcePosition = includeSourcePosition
        };

        return await cognitiveGraphService.QueryCognitiveGraphAsync(graphId, query);
    }

    /// <summary>
    /// Get a specific node by ID
    /// </summary>
    public async Task<CognitiveGraphNodeDto?> GetNode(
        [Service] CognitiveGraphService cognitiveGraphService,
        string nodeId,
        bool includeMetadata = false)
    {
        return await cognitiveGraphService.GetNodeAsync(nodeId, includeMetadata);
    }

    /// <summary>
    /// Search nodes by text content
    /// </summary>
    public async Task<List<CognitiveGraphNodeDto>> SearchNodes(
        [Service] CognitiveGraphService cognitiveGraphService,
        string graphId,
        string searchTerm,
        int limit = 20)
    {
        return await cognitiveGraphService.SearchNodesAsync(graphId, searchTerm, Math.Min(limit, 50));
    }
}