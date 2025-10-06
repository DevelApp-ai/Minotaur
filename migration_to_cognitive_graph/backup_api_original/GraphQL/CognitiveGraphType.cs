using Minotaur.Core;

namespace Minotaur.UI.Blazor.Api.GraphQL;

/// <summary>
/// GraphQL type for cognitive graph nodes - lightweight representation for API queries
/// </summary>
public class CognitiveGraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? DisplayText { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> ChildIds { get; set; } = new();
    public string? ParentId { get; set; }
    public SourcePositionDto? SourcePosition { get; set; }
}

/// <summary>
/// Lightweight source position for GraphQL
/// </summary>
public class SourcePositionDto
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Offset { get; set; }
    public int Length { get; set; }
    public string? SourceFile { get; set; }
}

/// <summary>
/// GraphQL query for cognitive graph subset
/// </summary>
public class CognitiveGraphQuery
{
    public string? NodeId { get; set; }
    public int MaxDepth { get; set; } = 3; // Limit depth to prevent large queries
    public List<string>? NodeTypes { get; set; } // Filter by node types
    public bool IncludeMetadata { get; set; } = false;
    public bool IncludeSourcePosition { get; set; } = true;
}

/// <summary>
/// GraphQL response for cognitive graph data
/// </summary>
public class CognitiveGraphResponse
{
    public List<CognitiveGraphNodeDto> Nodes { get; set; } = new();
    public List<CognitiveGraphEdgeDto> Edges { get; set; } = new();
    public int TotalNodes { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// GraphQL type for cognitive graph edges
/// </summary>
public class CognitiveGraphEdgeDto
{
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string EdgeType { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}