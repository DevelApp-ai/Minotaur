using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using Minotaur.UI.Blazor.Api.GraphQL;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for efficient communication with cognitive graph API using GraphQL and SignalR
/// </summary>
public class CognitiveGraphApiService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;
    private readonly string _graphQLEndpoint = "/graphql";
    private readonly string _hubEndpoint = "/cognitive-graph-hub";

    public CognitiveGraphApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initialize SignalR connection for real-time updates
    /// </summary>
    public async Task InitializeAsync(string baseUrl)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}{_hubEndpoint}")
            .WithAutomaticReconnect()
            .Build();

        // Set up event handlers
        _hubConnection.On<CognitiveGraphResponse>("GraphDataReceived", OnGraphDataReceived);
        _hubConnection.On<string>("GraphError", OnGraphError);
        _hubConnection.On<object>("GraphUpdated", OnGraphUpdated);
        _hubConnection.On<CognitiveGraphNodeDto>("NodeUpdated", OnNodeUpdated);

        await _hubConnection.StartAsync();
    }

    /// <summary>
    /// Query cognitive graph using GraphQL with efficient pagination
    /// </summary>
    public async Task<CognitiveGraphResponse?> QueryCognitiveGraphAsync(
        string graphId,
        string? nodeId = null,
        int maxDepth = 3,
        List<string>? nodeTypes = null,
        bool includeMetadata = false)
    {
        var query = BuildGraphQLQuery();
        var variables = new
        {
            graphId,
            nodeId,
            maxDepth = Math.Min(maxDepth, 5), // Limit depth for performance
            nodeTypes,
            includeMetadata,
            includeSourcePosition = true
        };

        var request = new
        {
            query,
            variables
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_graphQLEndpoint, request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("getCognitiveGraph", out var graphElement))
            {
                return JsonSerializer.Deserialize<CognitiveGraphResponse>(graphElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GraphQL query error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Search nodes efficiently using GraphQL
    /// </summary>
    public async Task<List<CognitiveGraphNodeDto>?> SearchNodesAsync(string graphId, string searchTerm, int limit = 20)
    {
        var query = @"
            query SearchNodes($graphId: String!, $searchTerm: String!, $limit: Int!) {
                searchNodes(graphId: $graphId, searchTerm: $searchTerm, limit: $limit) {
                    id
                    nodeType
                    displayText
                    sourcePosition {
                        line
                        column
                        offset
                        length
                        sourceFile
                    }
                }
            }";

        var variables = new { graphId, searchTerm, limit = Math.Min(limit, 50) };
        var request = new { query, variables };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_graphQLEndpoint, request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("searchNodes", out var nodesElement))
            {
                return JsonSerializer.Deserialize<List<CognitiveGraphNodeDto>>(nodesElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request error in SearchNodesAsync: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parsing error in SearchNodesAsync: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timeout in SearchNodesAsync: {ex.Message}");
        }

        return new List<CognitiveGraphNodeDto>();
    }

    /// <summary>
    /// Get a specific node by ID using GraphQL
    /// </summary>
    public async Task<CognitiveGraphNodeDto?> GetNodeAsync(string nodeId, bool includeMetadata = false)
    {
        var query = @"
            query GetNode($nodeId: String!, $includeMetadata: Boolean!) {
                getNode(nodeId: $nodeId, includeMetadata: $includeMetadata) {
                    id
                    nodeType
                    displayText
                    metadata
                    childIds
                    parentId
                    sourcePosition {
                        line
                        column
                        offset
                        length
                        sourceFile
                    }
                }
            }";

        var variables = new { nodeId, includeMetadata };
        var request = new { query, variables };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_graphQLEndpoint, request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("getNode", out var nodeElement))
            {
                return JsonSerializer.Deserialize<CognitiveGraphNodeDto>(nodeElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request error in GetNodeAsync: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parsing error in GetNodeAsync: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timeout in GetNodeAsync: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Subscribe to real-time updates for a specific graph
    /// </summary>
    public async Task SubscribeToGraphUpdatesAsync(string graphId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinGraphSession", graphId);
        }
    }

    /// <summary>
    /// Unsubscribe from graph updates
    /// </summary>
    public async Task UnsubscribeFromGraphUpdatesAsync(string graphId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveGraphSession", graphId);
        }
    }

    /// <summary>
    /// Request graph data via SignalR with real-time response
    /// </summary>
    public async Task RequestGraphDataAsync(string graphId, CognitiveGraphQuery query)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("RequestGraphData", graphId, query);
        }
    }

    // Event handlers
    private void OnGraphDataReceived(CognitiveGraphResponse response)
    {
        GraphDataReceived?.Invoke(response);
    }

    private void OnGraphError(string error)
    {
        GraphError?.Invoke(error);
    }

    private void OnGraphUpdated(object updateInfo)
    {
        GraphUpdated?.Invoke(updateInfo);
    }

    private void OnNodeUpdated(CognitiveGraphNodeDto node)
    {
        NodeUpdated?.Invoke(node);
    }

    // Events for real-time updates
    public event Action<CognitiveGraphResponse>? GraphDataReceived;
    public event Action<string>? GraphError;
    public event Action<object>? GraphUpdated;
    public event Action<CognitiveGraphNodeDto>? NodeUpdated;

    private static string BuildGraphQLQuery()
    {
        return @"
            query GetCognitiveGraph(
                $graphId: String!,
                $nodeId: String,
                $maxDepth: Int!,
                $nodeTypes: [String!],
                $includeMetadata: Boolean!,
                $includeSourcePosition: Boolean!
            ) {
                getCognitiveGraph(
                    graphId: $graphId,
                    nodeId: $nodeId,
                    maxDepth: $maxDepth,
                    nodeTypes: $nodeTypes,
                    includeMetadata: $includeMetadata,
                    includeSourcePosition: $includeSourcePosition
                ) {
                    nodes {
                        id
                        nodeType
                        displayText
                        metadata
                        childIds
                        parentId
                        sourcePosition {
                            line
                            column
                            offset
                            length
                            sourceFile
                        }
                    }
                    edges {
                        fromNodeId
                        toNodeId
                        edgeType
                        properties
                    }
                    totalNodes
                    hasMore
                }
            }";
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}