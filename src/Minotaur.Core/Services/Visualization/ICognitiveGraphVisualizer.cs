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

using CognitiveGraph;
using Minotaur.Core.Models.Visualization;

namespace Minotaur.Core.Services.Visualization;

/// <summary>
/// Service for visualizing CognitiveGraph with native ambiguity support.
/// 
/// This is the PRIMARY visualization service for Minotaur. It directly uses
/// CognitiveGraph's SymbolNode and PackedNode structure to visualize parse
/// data WITHOUT forcing a single unambiguous tree.
/// 
/// Key features:
/// - Shows all SymbolNodes as graph nodes
/// - Highlights nodes with multiple PackedNodes (ambiguous)
/// - Shows edges for each PackedNode's children (alternative paths)
/// - Allows selection of specific PackedNode paths
/// - Never forces a single interpretation
/// 
/// This is essential for Minotaur's support of evolving languages where
/// ambiguity is a feature, not a bug.
/// </summary>
public interface ICognitiveGraphVisualizer
{
    /// <summary>
    /// Generate visualization data from a CognitiveGraph.
    /// Preserves all PackedNode alternatives.
    /// </summary>
    /// <param name="graph">The CognitiveGraph to visualize.</param>
    /// <param name="options">Visualization options.</param>
    /// <returns>Visualization data with full ambiguity support.</returns>
    CognitiveGraphVisualization GenerateVisualization(
        CognitiveGraph graph,
        VisualizationOptions? options = null);

    /// <summary>
    /// Get all ambiguity points (nodes with multiple PackedNodes).
    /// </summary>
    /// <param name="graph">The CognitiveGraph to analyze.</param>
    /// <returns>List of ambiguity information.</returns>
    List<NodeAmbiguityInfo> GetAmbiguityPoints(CognitiveGraph graph);

    /// <summary>
    /// Get all possible interpretation paths through the graph.
    /// Each path represents one way to resolve all ambiguities.
    /// </summary>
    /// <param name="graph">The CognitiveGraph to analyze.</param>
    /// <returns>List of interpretation paths.</returns>
    List<InterpretationPath> GetAllInterpretationPaths(CognitiveGraph graph);

    /// <summary>
    /// Generate visualization for a specific interpretation path.
    /// Shows only the selected PackedNode choices.
    /// </summary>
    /// <param name="graph">The CognitiveGraph.</param>
    /// <param name="path">The interpretation path to visualize.</param>
    /// <returns>Visualization data for the single interpretation.</returns>
    CognitiveGraphVisualization GenerateSingleInterpretation(
        CognitiveGraph graph,
        InterpretationPath path);
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
/// Represents a complete path through all ambiguities.
/// Maps SymbolNode ID to the chosen PackedNode index.
/// </summary>
public class InterpretationPath
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the choices: SymbolNode ID -> PackedNode index.
    /// Only includes nodes that have multiple PackedNodes.
    /// </summary>
    public Dictionary<ulong, int> Choices { get; set; } = new();

    /// <summary>Gets or sets the rules applied along this path.</summary>
    public List<string> AppliedRules { get; set; } = new();

    /// <summary>Gets or sets whether this path is valid.</summary>
    public bool IsValid { get; set; } = true;
}

/// <summary>
/// Implementation of ICognitiveGraphVisualizer.
/// 
/// This implementation directly traverses CognitiveGraph's SymbolNode and
/// PackedNode structures to generate visualization data that preserves
/// all ambiguity.
/// </summary>
public class CognitiveGraphVisualizer : ICognitiveGraphVisualizer
{
    private readonly ILogger<CognitiveGraphVisualizer>? _logger;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public CognitiveGraphVisualizer(ILogger<CognitiveGraphVisualizer>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate visualization data from a CognitiveGraph.
    /// </summary>
    public CognitiveGraphVisualization GenerateVisualization(
        CognitiveGraph graph,
        VisualizationOptions? options = null)
    {
        options ??= new VisualizationOptions();

        var visualization = new CognitiveGraphVisualization
        {
            GraphData = new GraphData
            {
                SourceCode = graph.GetSourceText(),
                GrammarName = "Unknown" // Would come from graph metadata
            },
            Mode = options.Mode
        };

        // Get all SymbolNodes from the graph
        var allNodes = graph.GetAllNodes();
        var nodeMap = new Dictionary<ulong, GraphNode>();

        // First pass: Create GraphNode for each SymbolNode
        foreach (var node in allNodes)
        {
            var graphNode = ConvertToGraphNode(node, graph);
            visualization.GraphData.Nodes.Add(graphNode);
            nodeMap[node.Id] = graphNode;

            // Check for ambiguity (multiple PackedNodes)
            var packedNodes = node.GetPackedNodes();
            if (packedNodes.Count > 1)
            {
                var ambiguity = CreateAmbiguityInfo(node, packedNodes, graph);
                visualization.Ambiguities[graphNode.Id] = ambiguity;
            }
        }

        // Second pass: Create edges from PackedNodes
        foreach (var node in allNodes)
        {
            var packedNodes = node.GetPackedNodes();
            
            // Each PackedNode represents a different interpretation
            for (int i = 0; i < packedNodes.Count; i++)
            {
                var packedNode = packedNodes[i];
                var childNodes = packedNode.GetChildNodes();

                foreach (var child in childNodes)
                {
                    var edge = new GraphEdge
                    {
                        Id = $"{node.Id}-{i}-{child.Id}",
                        Source = node.Id.ToString(),
                        Target = child.Id.ToString(),
                        Type = packedNodes.Count > 1 ? "alternative" : "hierarchy",
                        Weight = 1,
                        IsAlternative = packedNodes.Count > 1,
                        PackedNodeIndex = i,
                        RuleId = packedNode.RuleID,
                        Properties = new Dictionary<string, object>
                        {
                            ["ruleId"] = packedNode.RuleID,
                            ["packedNodeIndex"] = i
                        }
                    };

                    visualization.GraphData.Edges.Add(edge);
                }
            }
        }

        return visualization;
    }

    /// <summary>
    /// Get all ambiguity points.
    /// </summary>
    public List<NodeAmbiguityInfo> GetAmbiguityPoints(CognitiveGraph graph)
    {
        var ambiguities = new List<NodeAmbiguityInfo>();
        var allNodes = graph.GetAllNodes();

        foreach (var node in allNodes)
        {
            var packedNodes = node.GetPackedNodes();
            if (packedNodes.Count > 1)
            {
                ambiguities.Add(CreateAmbiguityInfo(node, packedNodes, graph));
            }
        }

        return ambiguities;
    }

    /// <summary>
    /// Get all possible interpretation paths.
    /// </summary>
    public List<InterpretationPath> GetAllInterpretationPaths(CognitiveGraph graph)
    {
        var paths = new List<InterpretationPath>();
        var rootNode = graph.GetRootNode();

        // Start with empty choices
        GeneratePathsRecursive(graph, rootNode, new Dictionary<ulong, int>(), paths);

        return paths;
    }

    /// <summary>
    /// Generate visualization for a specific interpretation path.
    /// </summary>
    public CognitiveGraphVisualization GenerateSingleInterpretation(
        CognitiveGraph graph,
        InterpretationPath path)
    {
        var fullVisualization = GenerateVisualization(graph);

        // Filter edges to only show the selected PackedNode path
        var filteredEdges = new List<GraphEdge>();
        foreach (var edge in fullVisualization.GraphData.Edges)
        {
            // If the source node has a choice in the path, only show the selected PackedNode
            if (path.Choices.TryGetValue(ulong.Parse(edge.Source), out var selectedIndex))
            {
                if (edge.PackedNodeIndex == selectedIndex)
                {
                    filteredEdges.Add(edge);
                }
            }
            else
            {
                // No ambiguity at source, include the edge
                filteredEdges.Add(edge);
            }
        }

        fullVisualization.GraphData.Edges = filteredEdges;
        fullVisualization.Mode = VisualizationMode.ShowSelectedInterpretation;

        return fullVisualization;
    }

    private GraphNode ConvertToGraphNode(SymbolNode node, CognitiveGraph graph)
    {
        var sourceText = node.GetSourceText();
        
        // Get node properties
        var properties = new Dictionary<string, object>();
        var nodeProperties = node.GetProperties();
        foreach (var prop in nodeProperties)
        {
            properties[prop.GetKey()] = prop.GetValue().ToObject();
        }

        // Calculate line/column from offset
        var (line, column) = CalculateLineColumn(graph, node.SourceStart);

        var packedNodes = node.GetPackedNodes();

        return new GraphNode
        {
            Id = node.Id.ToString(),
            Type = node.NodeType.ToString(),
            Name = sourceText.Length > 30 ? sourceText.ToString()[..30] + "..." : sourceText.ToString(),
            FullName = properties.ContainsKey("FullName") ? properties["FullName"]?.ToString() : null,
            Properties = properties,
            Group = DetermineGroup(node.NodeType),
            Size = 10,
            IsAmbiguous = packedNodes.Count > 1,
            AlternativeCount = packedNodes.Count,
            Location = new CodeLocation
            {
                Start = new Position { Line = line, Column = column, Offset = (int)node.SourceStart },
                End = new Position { Line = line, Column = (int)(column + node.SourceLength), Offset = (int)(node.SourceStart + node.SourceLength) }
            }
        };
    }

    private NodeAmbiguityInfo CreateAmbiguityInfo(
        SymbolNode node,
        PackedNodeOffsetCollection packedNodes,
        CognitiveGraph graph)
    {
        var packedNodeInfos = new List<PackedNodeInfo>();
        var (line, column) = CalculateLineColumn(graph, node.SourceStart);

        for (int i = 0; i < packedNodes.Count; i++)
        {
            var packedNode = packedNodes[i];
            var childNodes = packedNode.GetChildNodes();

            packedNodeInfos.Add(new PackedNodeInfo
            {
                Index = i,
                RuleId = packedNode.RuleID,
                RuleName = packedNode.RuleID.ToString(), // Would map to actual rule name
                ChildNodeIds = childNodes.Select(c => c.Id.ToString()).ToList(),
                IsValid = true
            });
        }

        return new NodeAmbiguityInfo
        {
            NodeId = node.Id.ToString(),
            IsAmbiguous = true,
            Location = new CodeLocation
            {
                Start = new Position { Line = line, Column = column, Offset = (int)node.SourceStart },
                End = new Position { Line = line, Column = (int)(column + node.SourceLength), Offset = (int)(node.SourceStart + node.SourceLength) }
            },
            AlternativeCount = packedNodes.Count,
            PackedNodes = packedNodeInfos
        };
    }

    private (int line, int column) CalculateLineColumn(CognitiveGraph graph, uint offset)
    {
        var sourceText = graph.GetSourceText();
        var line = 1;
        var column = 1;
        var currentOffset = 0u;

        for (int i = 0; i < offset && i < sourceText.Length; i++)
        {
            if (sourceText[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            currentOffset++;
        }

        return (line, column);
    }

    private void GeneratePathsRecursive(
        CognitiveGraph graph,
        SymbolNode node,
        Dictionary<ulong, int> currentPath,
        List<InterpretationPath> paths)
    {
        var packedNodes = node.GetPackedNodes();

        // If this node has multiple PackedNodes, we need to branch
        if (packedNodes.Count > 1)
        {
            for (int i = 0; i < packedNodes.Count; i++)
            {
                var newPath = new Dictionary<ulong, int>(currentPath)
                {
                    [node.Id] = i
                };

                var childNodes = packedNodes[i].GetChildNodes();
                
                if (childNodes.Count == 0)
                {
                    // Leaf node in this interpretation
                    paths.Add(new InterpretationPath
                    {
                        Id = $"path_{string.Join("_", newPath.Select(kv => $"{kv.Key}_{kv.Value}"))}",
                        Choices = newPath,
                        IsValid = true
                    });
                }
                else
                {
                    // Recurse on children
                    foreach (var child in childNodes)
                    {
                        GeneratePathsRecursive(graph, child, newPath, paths);
                    }
                }
            }
        }
        else if (packedNodes.Count == 1)
        {
            // Single PackedNode, no branching needed
            var childNodes = packedNodes[0].GetChildNodes();
            
            if (childNodes.Count == 0)
            {
                // Leaf node
                paths.Add(new InterpretationPath
                {
                    Id = $"path_{string.Join("_", currentPath.Select(kv => $"{kv.Key}_{kv.Value}"))}",
                    Choices = new Dictionary<ulong, int>(currentPath),
                    IsValid = true
                });
            }
            else
            {
                // Recurse on children
                foreach (var child in childNodes)
                {
                    GeneratePathsRecursive(graph, child, currentPath, paths);
                }
            }
        }
        else
        {
            // No PackedNodes (leaf)
            paths.Add(new InterpretationPath
            {
                Id = $"path_{string.Join("_", currentPath.Select(kv => $"{kv.Key}_{kv.Value}"))}",
                Choices = new Dictionary<ulong, int>(currentPath),
                IsValid = true
            });
        }
    }

    private string DetermineGroup(ushort nodeType)
    {
        // Map node type IDs to semantic groups
        return nodeType switch
        {
            >= 100 and < 200 => "declaration",
            >= 200 and < 300 => "expression",
            >= 300 and < 400 => "statement",
            >= 400 and < 500 => "type",
            >= 500 and < 600 => "literal",
            _ => "other"
        };
    }
}
