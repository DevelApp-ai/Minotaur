using Microsoft.AspNetCore.SignalR;
using Minotaur.UI.Blazor.Api.GraphQL;
using Minotaur.UI.Blazor.Api.Services;

namespace Minotaur.UI.Blazor.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time cognitive graph updates
/// </summary>
public class CognitiveGraphHub : Hub
{
    private readonly CognitiveGraphService _cognitiveGraphService;

    public CognitiveGraphHub(CognitiveGraphService cognitiveGraphService)
    {
        _cognitiveGraphService = cognitiveGraphService;
    }

    /// <summary>
    /// Join a specific graph session for real-time updates
    /// </summary>
    public async Task JoinGraphSession(string graphId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"graph-{graphId}");
        await Clients.Caller.SendAsync("JoinedGraphSession", graphId);
    }

    /// <summary>
    /// Leave a graph session
    /// </summary>
    public async Task LeaveGraphSession(string graphId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"graph-{graphId}");
        await Clients.Caller.SendAsync("LeftGraphSession", graphId);
    }

    /// <summary>
    /// Subscribe to node changes for a specific node
    /// </summary>
    public async Task SubscribeToNode(string nodeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"node-{nodeId}");
    }

    /// <summary>
    /// Unsubscribe from node changes
    /// </summary>
    public async Task UnsubscribeFromNode(string nodeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"node-{nodeId}");
    }

    /// <summary>
    /// Request efficient graph data with pagination
    /// </summary>
    public async Task RequestGraphData(string graphId, CognitiveGraphQuery query)
    {
        try
        {
            var response = await _cognitiveGraphService.QueryCognitiveGraphAsync(graphId, query);
            await Clients.Caller.SendAsync("GraphDataReceived", response);
        }
        catch (ArgumentException ex)
        {
            await Clients.Caller.SendAsync("GraphError", $"Invalid argument: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("GraphError", $"Operation error: {ex.Message}");
        }
        // Add additional known exception types as needed, e.g. custom GraphQueryException
    }

    /// <summary>
    /// Notify clients when a graph is updated
    /// </summary>
    public async Task NotifyGraphUpdated(string graphId, List<string> updatedNodeIds)
    {
        await Clients.Group($"graph-{graphId}").SendAsync("GraphUpdated", new
        {
            GraphId = graphId,
            UpdatedNodes = updatedNodeIds,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify clients when a specific node is updated
    /// </summary>
    public async Task NotifyNodeUpdated(string nodeId, CognitiveGraphNodeDto updatedNode)
    {
        await Clients.Group($"node-{nodeId}").SendAsync("NodeUpdated", updatedNode);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up any subscriptions
        await base.OnDisconnectedAsync(exception);
    }
}