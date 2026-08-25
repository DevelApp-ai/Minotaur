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

using System;
using System.Collections.Generic;
using System.Linq;
using CognitiveGraph;
using Minotaur.Core.Models.Visualization;

namespace Minotaur.Core.Services.Visualization;

/// <summary>
/// Implementation of ICognitiveGraphVisualizer for visualizing CognitiveGraph
/// with native ambiguity support through PackedNode structures.
/// 
/// This service preserves all ambiguity in the parse by showing all PackedNode
/// alternatives, allowing users to see all possible interpretations of the source code.
/// </summary>
public class CognitiveGraphVisualizer : ICognitiveGraphVisualizer
{
    private readonly ILogger<CognitiveGraphVisualizer> _logger;

    /// <summary>
    /// Initializes a new instance of the CognitiveGraphVisualizer.
    /// </summary>
    public CognitiveGraphVisualizer(ILogger<CognitiveGraphVisualizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate visualization data from a CognitiveGraph.
    /// Preserves all PackedNode alternatives.
    /// </summary>
    public CognitiveGraphVisualization GenerateVisualization(
        CognitiveGraph graph,
        VisualizationOptions? options = null)
    {
        options ??= new VisualizationOptions();

        var visualization = new CognitiveGraphVisualization
        {
            GraphName = graph.Name,
            GrammarName = graph.GrammarName,
            SourceCode = graph.SourceCode,
            Options = options
        };

        // Process all SymbolNodes
        var nodeMap = new Dictionary<SymbolNode, GraphNode>();
        
        foreach (var symbolNode in graph.AllNodes)
        {
            var graphNode = CreateGraphNode(symbolNode, options);
            nodeMap[symbolNode] = graphNode;
            visualization.Nodes.Add(graphNode);
        }

        // Process all PackedNodes and create edges
        var edgeId = 0;
        foreach (var symbolNode in graph.AllNodes)
        {
            if (symbolNode.PackedNodes == null || symbolNode.PackedNodes.Count == 0)
                continue;

            var sourceNode = nodeMap[symbolNode];

            // Check if this node has multiple PackedNodes (ambiguity)
            if (symbolNode.PackedNodes.Count > 1)
            {
                sourceNode.IsAmbiguous = true;
                sourceNode.AlternativeCount = symbolNode.PackedNodes.Count;
            }

            // Create edges for each PackedNode
            for (int i = 0; i < symbolNode.PackedNodes.Count; i++)
            {
                var packedNode = symbolNode.PackedNodes[i];
                
                // Create edges to child SymbolNodes
                foreach (var child in packedNode.Children)
                {
                    if (child != null && nodeMap.TryGetValue(child, out var targetNode))
                    {
                        var edge = new GraphEdge
                        {
                            Id = $"edge_{edgeId++}",
                            Source = sourceNode.Id,
                            Target = targetNode.Id,
                            Type = GetEdgeType(packedNode),
                            Weight = 1,
                            IsAlternative = symbolNode.PackedNodes.Count > 1,
                            PackedNodeIndex = i
                        };
                        
                        // Add edge properties
                        edge.Properties["packedNodeIndex"] = i;
                        edge.Properties["isAmbiguous"] = symbolNode.PackedNodes.Count > 1;
                        
                        visualization.Edges.Add(edge);
                    }
                }
            }
        }

        // Add ambiguity information
        visualization.AmbiguityPoints = GetAmbiguityPoints(graph);
        visualization.InterpretationPaths = options.Mode == VisualizationMode.ListAllInterpretations 
            ? GetAllInterpretationPaths(graph)
            : new List<InterpretationPath>();

        return visualization;
    }

    /// <summary>
    /// Get all ambiguity points (nodes with multiple PackedNodes).
    /// </summary>
    public List<NodeAmbiguityInfo> GetAmbiguityPoints(CognitiveGraph graph)
    {
        var ambiguities = new List<NodeAmbiguityInfo>();

        foreach (var symbolNode in graph.AllNodes)
        {
            if (symbolNode.PackedNodes != null && symbolNode.PackedNodes.Count > 1)
            {
                var ambiguity = new NodeAmbiguityInfo
                {
                    NodeId = symbolNode.Id,
                    NodeName = symbolNode.Text,
                    NodeType = symbolNode.Kind.ToString(),
                    AlternativeCount = symbolNode.PackedNodes.Count,
                    Location = new CodeLocation
                    {
                        Line = symbolNode.StartLine,
                        Column = symbolNode.StartColumn,
                        Length = symbolNode.Text?.Length ?? 0
                    }
                };

                // Add information about each PackedNode
                for (int i = 0; i < symbolNode.PackedNodes.Count; i++)
                {
                    var packedNode = symbolNode.PackedNodes[i];
                    var alternative = new PackedNodeAlternative
                    {
                        Index = i,
                        ChildCount = packedNode.Children?.Count ?? 0,
                        IsPreferred = i == 0 // First is usually preferred
                    };
                    ambiguity.Alternatives.Add(alternative);
                }

                ambiguities.Add(ambiguity);
            }
        }

        return ambiguities;
    }

    /// <summary>
    /// Get all possible interpretation paths through the graph.
    /// Each path represents one way to resolve all ambiguities.
    /// </summary>
    public List<InterpretationPath> GetAllInterpretationPaths(CognitiveGraph graph)
    {
        var paths = new List<InterpretationPath>();
        var ambiguityNodes = graph.AllNodes
            .Where(n => n.PackedNodes != null && n.PackedNodes.Count > 1)
            .ToList();

        if (!ambiguityNodes.Any())
        {
            // No ambiguity, single path
            paths.Add(new InterpretationPath());
            return paths;
        }

        // Generate all combinations of PackedNode choices
        GenerateAllPaths(graph.Root, new InterpretationPath(), paths);

        return paths;
    }

    /// <summary>
    /// Recursively generate all interpretation paths.
    /// </summary>
    private void GenerateAllPaths(
        SymbolNode node,
        InterpretationPath currentPath,
        List<InterpretationPath> allPaths)
    {
        if (node == null)
            return;

        // If this node has multiple PackedNodes, we need to branch
        if (node.PackedNodes != null && node.PackedNodes.Count > 1)
        {
            // Create a new path for each PackedNode choice
            for (int i = 0; i < node.PackedNodes.Count; i++)
            {
                var newPath = currentPath.Clone();
                newPath.NodeChoices[node.Id] = i;
                
                // Recursively process children of this PackedNode
                foreach (var child in node.PackedNodes[i].Children)
                {
                    GenerateAllPaths(child, newPath, allPaths);
                }
                
                // If this is a leaf ambiguity node, add the path
                if (node.PackedNodes[i].Children.Count == 0)
                {
                    allPaths.Add(newPath);
                }
            }
        }
        else
        {
            // Single PackedNode, continue with current path
            if (node.PackedNodes != null && node.PackedNodes.Count == 1)
            {
                currentPath.NodeChoices[node.Id] = 0;
            }
            
            // Process children
            if (node.PackedNodes != null && node.PackedNodes.Count > 0)
            {
                foreach (var child in node.PackedNodes[0].Children)
                {
                    GenerateAllPaths(child, currentPath, allPaths);
                }
            }
            
            // If this is a leaf node, add the path
            if (node.PackedNodes == null || node.PackedNodes.Count == 0 ||
                node.PackedNodes[0].Children.Count == 0)
            {
                allPaths.Add(currentPath.Clone());
            }
        }
    }

    /// <summary>
    /// Generate visualization for a specific interpretation path.
    /// Shows only the selected PackedNode choices.
    /// </summary>
    public CognitiveGraphVisualization GenerateSingleInterpretation(
        CognitiveGraph graph,
        InterpretationPath path)
    {
        var options = new VisualizationOptions
        {
            ShowAllAlternatives = false,
            HighlightAmbiguities = true,
            Mode = VisualizationMode.ShowSingleInterpretation
        };

        var visualization = new CognitiveGraphVisualization
        {
            GraphName = graph.Name,
            GrammarName = graph.GrammarName,
            SourceCode = graph.SourceCode,
            Options = options,
            SelectedPath = path
        };

        // Process all SymbolNodes
        var nodeMap = new Dictionary<SymbolNode, GraphNode>();
        
        foreach (var symbolNode in graph.AllNodes)
        {
            var graphNode = CreateGraphNode(symbolNode, options);
            nodeMap[symbolNode] = graphNode;
            visualization.Nodes.Add(graphNode);
        }

        // Process only the selected PackedNode paths
        var edgeId = 0;
        foreach (var symbolNode in graph.AllNodes)
        {
            if (symbolNode.PackedNodes == null || symbolNode.PackedNodes.Count == 0)
                continue;

            var sourceNode = nodeMap[symbolNode];

            // Get the selected PackedNode index for this node
            int selectedIndex = 0;
            if (path.NodeChoices.TryGetValue(symbolNode.Id, out var choice))
            {
                selectedIndex = choice;
            }

            // Only process the selected PackedNode
            if (selectedIndex < symbolNode.PackedNodes.Count)
            {
                var packedNode = symbolNode.PackedNodes[selectedIndex];
                
                // Create edges to child SymbolNodes
                foreach (var child in packedNode.Children)
                {
                    if (child != null && nodeMap.TryGetValue(child, out var targetNode))
                    {
                        var edge = new GraphEdge
                        {
                            Id = $"edge_{edgeId++}",
                            Source = sourceNode.Id,
                            Target = targetNode.Id,
                            Type = GetEdgeType(packedNode),
                            Weight = 1,
                            IsAlternative = false,
                            PackedNodeIndex = selectedIndex
                        };
                        
                        // Mark as selected path
                        edge.Properties["isSelected"] = true;
                        edge.Properties["packedNodeIndex"] = selectedIndex;
                        
                        visualization.Edges.Add(edge);
                    }
                }
            }
        }

        return visualization;
    }

    /// <summary>
    /// Creates a GraphNode from a SymbolNode.
    /// </summary>
    private GraphNode CreateGraphNode(SymbolNode symbolNode, VisualizationOptions options)
    {
        var node = new GraphNode
        {
            Id = symbolNode.Id,
            Type = symbolNode.Kind.ToString(),
            Name = symbolNode.Text ?? string.Empty,
            FullName = symbolNode.FullName,
            Size = 10,
            Location = new CodeLocation
            {
                Line = symbolNode.StartLine,
                Column = symbolNode.StartColumn,
                Length = symbolNode.Text?.Length ?? 0
            }
        };

        // Add properties
        node.Properties["symbolId"] = symbolNode.SymbolId;
        node.Properties["kind"] = symbolNode.Kind.ToString();
        
        if (symbolNode.Value != null)
        {
            node.Properties["value"] = symbolNode.Value.ToString();
        }

        return node;
    }

    /// <summary>
    /// Gets the edge type based on the PackedNode.
    /// </summary>
    private string GetEdgeType(PackedNode packedNode)
    {
        if (packedNode == null)
            return "default";

        // Check if this is a specific type of relationship
        if (packedNode.IsAmbiguous)
            return "ambiguous";
        
        if (packedNode.IsPreferred)
            return "preferred";

        return "default";
    }
}
