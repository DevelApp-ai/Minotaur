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

namespace Minotaur.Core.Models.Visualization;

/// <summary>
/// Represents graph data for CognitiveGraph visualization.
/// Designed to work directly with CognitiveGraph's SymbolNode and PackedNode structure.
/// Preserves ambiguity by showing all PackedNode alternatives.
/// </summary>
public class GraphData
{
    /// <summary>Gets or sets the list of graph nodes (SymbolNodes).</summary>
    public List<GraphNode> Nodes { get; set; } = new();

    /// <summary>Gets or sets the list of graph edges.</summary>
    public List<GraphEdge> Edges { get; set; } = new();

    /// <summary>Gets or sets the source code.</summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar name.</summary>
    public string GrammarName { get; set; } = string.Empty;
}

/// <summary>
/// Represents a node in the visualization graph (corresponds to SymbolNode).
/// </summary>
public class GraphNode
{
    /// <summary>Gets or sets the unique identifier (SymbolNode.Id).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the node type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name (source text).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the full qualified name.</summary>
    public string? FullName { get; set; }

    /// <summary>Gets or sets the properties.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>Gets or sets the group for styling.</summary>
    public string? Group { get; set; }

    /// <summary>Gets or sets the size for visualization.</summary>
    public int Size { get; set; } = 10;

    /// <summary>Gets or sets whether this node is ambiguous (has multiple PackedNodes).</summary>
    public bool IsAmbiguous { get; set; } = false;

    /// <summary>Gets or sets the number of PackedNodes (alternative interpretations).</summary>
    public int AlternativeCount { get; set; } = 0;

    /// <summary>Gets or sets the source code location.</summary>
    public CodeLocation Location { get; set; } = new();
}

/// <summary>
/// Represents an edge in the visualization graph.
/// </summary>
public class GraphEdge
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the source node identifier.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the target node identifier.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the edge type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the edge weight for layout.</summary>
    public int Weight { get; set; } = 1;

    /// <summary>Gets or sets additional properties.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>Gets or sets whether this edge is part of an alternative PackedNode.</summary>
    public bool IsAlternative { get; set; } = false;

    /// <summary>Gets or sets which PackedNode index this edge belongs to.</summary>
    public int PackedNodeIndex { get; set; } = 0;

    /// <summary>Gets or sets the Rule ID from the PackedNode.</summary>
    public uint RuleId { get; set; }
}

/// <summary>
/// Represents a position in source code.
/// </summary>
public class CodeLocation
{
    /// <summary>Gets or sets the line number (1-based).</summary>
    public int Line { get; set; } = 1;

    /// <summary>Gets or sets the column number (1-based).</summary>
    public int Column { get; set; } = 1;

    /// <summary>Gets or sets the character offset (0-based).</summary>
    public int Offset { get; set; } = 0;

    /// <summary>Gets or sets the length of the span.</summary>
    public int Length { get; set; } = 0;
}

/// <summary>
/// Information about ambiguity at a specific SymbolNode.
/// </summary>
public class NodeAmbiguityInfo
{
    /// <summary>Gets or sets the node identifier.</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the node name.</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the node type.</summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this node is ambiguous.</summary>
    public bool IsAmbiguous { get; set; } = false;

    /// <summary>Gets or sets the source code location.</summary>
    public CodeLocation Location { get; set; } = new();

    /// <summary>Gets or sets the number of PackedNodes (alternatives).</summary>
    public int AlternativeCount { get; set; } = 0;

    /// <summary>Gets or sets the PackedNode alternatives.</summary>
    public List<PackedNodeAlternative> Alternatives { get; set; } = new();

    /// <summary>Gets or sets the currently selected PackedNode index.</summary>
    public int? SelectedPackedNode { get; set; }
}

/// <summary>
/// Information about a PackedNode alternative.
/// </summary>
public class PackedNodeAlternative
{
    /// <summary>Gets or sets the index of this PackedNode.</summary>
    public int Index { get; set; }

    /// <summary>Gets or sets the child count.</summary>
    public int ChildCount { get; set; }

    /// <summary>Gets or sets whether this PackedNode is valid.</summary>
    public bool IsValid { get; set; } = true;

    /// <summary>Gets or sets whether this is the preferred interpretation.</summary>
    public bool IsPreferred { get; set; } = false;
}

/// <summary>
/// Represents a complete path through all ambiguities.
/// Maps SymbolNode ID to the chosen PackedNode index.
/// </summary>
public class InterpretationPath
{
    /// <summary>Gets or sets the choices made at each ambiguous node (node ID -> PackedNode index).</summary>
    public Dictionary<string, int> NodeChoices { get; set; } = new();

    /// <summary>Gets or sets the total number of ambiguities in this path.</summary>
    public int AmbiguityCount => NodeChoices.Count;

    /// <summary>
    /// Creates a copy of this interpretation path.
    /// </summary>
    public InterpretationPath Clone()
    {
        return new InterpretationPath
        {
            NodeChoices = new Dictionary<string, int>(NodeChoices)
        };
    }
}

/// <summary>
/// Complete visualization data for CognitiveGraph.
/// </summary>
public class CognitiveGraphVisualization
{
    /// <summary>Gets or sets the graph name.</summary>
    public string GraphName { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar name.</summary>
    public string GrammarName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source code.</summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the graph data.</summary>
    public GraphData GraphData { get; set; } = new();

    /// <summary>Gets or sets the list of ambiguity points.</summary>
    public List<NodeAmbiguityInfo> AmbiguityPoints { get; set; } = new();

    /// <summary>Gets or sets all possible interpretation paths.</summary>
    public List<InterpretationPath> InterpretationPaths { get; set; } = new();

    /// <summary>Gets or sets the selected interpretation path.</summary>
    public InterpretationPath? SelectedPath { get; set; }

    /// <summary>Gets or sets the visualization options.</summary>
    public VisualizationOptions Options { get; set; } = new();

    /// <summary>Gets whether there are any ambiguities.</summary>
    public bool HasAmbiguities => AmbiguityPoints.Any(a => a.IsAmbiguous);

    /// <summary>Gets the ambiguity count.</summary>
    public int AmbiguityCount => AmbiguityPoints.Count;
}

/// <summary>
/// Visualization modes.
/// </summary>
public enum VisualizationMode
{
    /// <summary>Show all nodes and all PackedNode edges.</summary>
    ShowAllInterpretations,
    
    /// <summary>Show only the currently selected PackedNode path.</summary>
    ShowSelectedInterpretation,
    
    /// <summary>Show only ambiguity points.</summary>
    ShowAmbiguityOnly
}

/// <summary>
/// Options for visualization.
/// </summary>
public class VisualizationOptions
{
    /// <summary>Gets or sets whether to show all PackedNode alternatives.</summary>
    public bool ShowAllAlternatives { get; set; } = true;

    /// <summary>Gets or sets whether to highlight ambiguity points.</summary>
    public bool HighlightAmbiguities { get; set; } = true;

    /// <summary>Gets or sets the visualization mode.</summary>
    public VisualizationMode Mode { get; set; } = VisualizationMode.ShowAllInterpretations;

    /// <summary>Gets or sets the color scheme.</summary>
    public string ColorScheme { get; set; } = "default";
}

/// <summary>
/// Request for visualization.
/// </summary>
public class VisualizationRequest
{
    /// <summary>Gets or sets the source code to visualize.</summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the grammar name.</summary>
    public string GrammarName { get; set; } = string.Empty;

    /// <summary>Gets or sets the visualization options.</summary>
    public VisualizationOptions Options { get; set; } = new();
}

/// <summary>
/// Error response for visualization API.
/// </summary>
public class ErrorResponse
{
    /// <summary>Gets or sets the error type.</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the error details.</summary>
    public string? Details { get; set; }
}
