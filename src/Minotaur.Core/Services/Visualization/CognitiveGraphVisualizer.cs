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
using CognitiveGraph.Accessors;
using Microsoft.Extensions.Logging;
using Minotaur.Core.Models.Visualization;
using CGraph = CognitiveGraph.CognitiveGraph;

namespace Minotaur.Core.Services.Visualization;

/// <summary>
/// Implementation of <see cref="ICognitiveGraphVisualizer"/> for visualizing
/// CognitiveGraph with native ambiguity support through PackedNode structures.
/// 
/// This service preserves all ambiguity in the parse by showing all PackedNode
/// alternatives, allowing users to see all possible interpretations of the
/// source code.
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
    /// Preserves all PackedNode alternatives.
    /// </summary>
    public CognitiveGraphVisualization GenerateVisualization(
        CGraph graph,
        VisualizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        options ??= new VisualizationOptions();

        var sourceText = graph.GetSourceText() ?? string.Empty;
        var nodeOffsets = CollectAllNodeOffsets(graph);

        var visualization = new CognitiveGraphVisualization
        {
            GraphName = "cognitive-graph",
            GrammarName = string.Empty,
            SourceCode = sourceText,
            Options = options
        };

        // Process all SymbolNodes
        var nodeMap = new Dictionary<uint, GraphNode>(nodeOffsets.Count);

        foreach (var offset in nodeOffsets)
        {
            var symbolNode = graph.GetNodeAt(offset);
            var graphNode = CreateGraphNode(symbolNode, sourceText);
            nodeMap[offset] = graphNode;
            visualization.GraphData.Nodes.Add(graphNode);
        }

        // Process all PackedNodes and create edges
        var edgeId = 0;
        foreach (var offset in nodeOffsets)
        {
            var symbolNode = graph.GetNodeAt(offset);
            var packedNodes = symbolNode.GetPackedNodes();
            if (packedNodes.Count == 0)
                continue;

            var sourceNode = nodeMap[offset];

            // Check if this node has multiple PackedNodes (ambiguity)
            if (packedNodes.Count > 1)
            {
                sourceNode.IsAmbiguous = true;
                sourceNode.AlternativeCount = packedNodes.Count;
            }

            // Create edges for each PackedNode
            for (int i = 0; i < packedNodes.Count; i++)
            {
                var packedNode = packedNodes[i];
                var childNodes = packedNode.GetChildNodes();

                // Create edges to child SymbolNodes
                foreach (var child in childNodes)
                {
                    if (child.Offset != 0 && nodeMap.TryGetValue(child.Offset, out var targetNode))
                    {
                        var edge = new GraphEdge
                        {
                            Id = $"edge_{edgeId++}",
                            Source = sourceNode.Id,
                            Target = targetNode.Id,
                            Type = GetEdgeType(packedNodes.Count),
                            Weight = 1,
                            IsAlternative = packedNodes.Count > 1,
                            PackedNodeIndex = i,
                            RuleId = packedNode.RuleID
                        };

                        // Add edge properties
                        edge.Properties["packedNodeIndex"] = i;
                        edge.Properties["isAmbiguous"] = packedNodes.Count > 1;

                        visualization.GraphData.Edges.Add(edge);
                    }
                }
            }
        }

        // Add ambiguity information
        visualization.AmbiguityPoints = GetAmbiguityPoints(graph);
        visualization.InterpretationPaths = options.Mode == VisualizationMode.ShowAllInterpretations
            ? GetAllInterpretationPaths(graph)
            : new List<InterpretationPath>();

        return visualization;
    }

    /// <summary>
    /// Get all ambiguity points (nodes with multiple PackedNodes).
    /// </summary>
    public List<NodeAmbiguityInfo> GetAmbiguityPoints(CGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var ambiguities = new List<NodeAmbiguityInfo>();
        var sourceText = graph.GetSourceText() ?? string.Empty;

        foreach (var offset in CollectAllNodeOffsets(graph))
        {
            var symbolNode = graph.GetNodeAt(offset);
            var packedNodes = symbolNode.GetPackedNodes();

            if (packedNodes.Count > 1)
            {
                var (line, column) = GetLineColumn(sourceText, symbolNode.SourceStart);
                var ambiguity = new NodeAmbiguityInfo
                {
                    NodeId = GetNodeId(offset),
                    NodeName = symbolNode.GetSourceText().ToString(),
                    NodeType = symbolNode.NodeType.ToString(),
                    IsAmbiguous = true,
                    AlternativeCount = packedNodes.Count,
                    Location = new CodeLocation
                    {
                        Line = line,
                        Column = column,
                        Offset = (int)symbolNode.SourceStart,
                        Length = (int)symbolNode.SourceLength
                    }
                };

                // Add information about each PackedNode
                for (int i = 0; i < packedNodes.Count; i++)
                {
                    var alternative = new PackedNodeAlternative
                    {
                        Index = i,
                        ChildCount = packedNodes[i].GetChildNodes().Count,
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
    public List<InterpretationPath> GetAllInterpretationPaths(CGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var paths = new List<InterpretationPath>();

        // Enumerate ambiguous node offsets
        var ambiguousOffsets = new List<uint>();
        foreach (var offset in CollectAllNodeOffsets(graph))
        {
            if (graph.GetNodeAt(offset).GetPackedNodes().Count > 1)
                ambiguousOffsets.Add(offset);
        }

        if (ambiguousOffsets.Count == 0)
        {
            // No ambiguity, single (empty) path
            paths.Add(new InterpretationPath());
            return paths;
        }

        // Generate all combinations of PackedNode choices across ambiguous
        // nodes. The result count is the product of the alternative counts,
        // so cap it to avoid runaway memory on heavily ambiguous graphs.
        const int maxPaths = 10_000;

        var choices = new int[ambiguousOffsets.Count];
        var alternativeCounts = ambiguousOffsets
            .Select(o => graph.GetNodeAt(o).GetPackedNodes().Count)
            .ToArray();

        while (true)
        {
            var path = new InterpretationPath();
            for (int i = 0; i < ambiguousOffsets.Count; i++)
                path.NodeChoices[GetNodeId(ambiguousOffsets[i])] = choices[i];
            paths.Add(path);

            if (paths.Count >= maxPaths)
                break;

            // Increment the combination counter
            int position = choices.Length - 1;
            while (position >= 0)
            {
                choices[position]++;
                if (choices[position] < alternativeCounts[position])
                    break;
                choices[position] = 0;
                position--;
            }

            // All combinations exhausted
            if (position < 0)
                break;
        }

        return paths;
    }

    /// <summary>
    /// Generate visualization for a specific interpretation path.
    /// Shows only the selected PackedNode choices.
    /// </summary>
    public CognitiveGraphVisualization GenerateSingleInterpretation(
        CGraph graph,
        InterpretationPath path)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(path);

        var options = new VisualizationOptions
        {
            ShowAllAlternatives = false,
            HighlightAmbiguities = true,
            Mode = VisualizationMode.ShowSelectedInterpretation
        };

        var sourceText = graph.GetSourceText() ?? string.Empty;
        var nodeOffsets = CollectAllNodeOffsets(graph);

        var visualization = new CognitiveGraphVisualization
        {
            GraphName = "cognitive-graph",
            GrammarName = string.Empty,
            SourceCode = sourceText,
            Options = options,
            SelectedPath = path
        };

        // Process all SymbolNodes
        var nodeMap = new Dictionary<uint, GraphNode>(nodeOffsets.Count);

        foreach (var offset in nodeOffsets)
        {
            var symbolNode = graph.GetNodeAt(offset);
            var graphNode = CreateGraphNode(symbolNode, sourceText);
            nodeMap[offset] = graphNode;
            visualization.GraphData.Nodes.Add(graphNode);
        }

        // Process only the selected PackedNode paths
        var edgeId = 0;
        foreach (var offset in nodeOffsets)
        {
            var symbolNode = graph.GetNodeAt(offset);
            var packedNodes = symbolNode.GetPackedNodes();
            if (packedNodes.Count == 0)
                continue;

            var sourceNode = nodeMap[offset];

            // Get the selected PackedNode index for this node
            var selectedIndex = 0;
            if (path.NodeChoices.TryGetValue(GetNodeId(offset), out var choice))
            {
                selectedIndex = choice;
            }

            // Only process the selected PackedNode
            if (selectedIndex < packedNodes.Count)
            {
                var packedNode = packedNodes[selectedIndex];
                var childNodes = packedNode.GetChildNodes();

                // Create edges to child SymbolNodes
                foreach (var child in childNodes)
                {
                    if (child.Offset != 0 && nodeMap.TryGetValue(child.Offset, out var targetNode))
                    {
                        var edge = new GraphEdge
                        {
                            Id = $"edge_{edgeId++}",
                            Source = sourceNode.Id,
                            Target = targetNode.Id,
                            Type = "selected",
                            Weight = 1,
                            IsAlternative = false,
                            PackedNodeIndex = selectedIndex,
                            RuleId = packedNode.RuleID
                        };

                        // Mark as selected path
                        edge.Properties["isSelected"] = true;
                        edge.Properties["packedNodeIndex"] = selectedIndex;

                        visualization.GraphData.Edges.Add(edge);
                    }
                }
            }
        }

        return visualization;
    }

    /// <summary>
    /// Collects the offsets of all SymbolNodes reachable from the root node,
    /// traversing every PackedNode alternative (breadth-first).
    /// </summary>
    private static List<uint> CollectAllNodeOffsets(CGraph graph)
    {
        var result = new List<uint>();
        var visited = new HashSet<uint>();
        var queue = new Queue<uint>();

        var statistics = graph.GetStatistics();
        if (statistics.NodeCount == 0)
            return result;

        var root = graph.GetRootNode();
        queue.Enqueue(root.Offset);
        visited.Add(root.Offset);

        while (queue.Count > 0)
        {
            var offset = queue.Dequeue();
            result.Add(offset);

            var symbolNode = graph.GetNodeAt(offset);
            var packedNodes = symbolNode.GetPackedNodes();

            for (int i = 0; i < packedNodes.Count; i++)
            {
                var childNodes = packedNodes[i].GetChildNodes();
                for (int c = 0; c < childNodes.Count; c++)
                {
                    var childOffset = childNodes[c].Offset;
                    if (childOffset != 0 && visited.Add(childOffset))
                    {
                        queue.Enqueue(childOffset);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a GraphNode from a SymbolNode.
    /// </summary>
    private static GraphNode CreateGraphNode(SymbolNode symbolNode, string sourceText)
    {
        var (line, column) = GetLineColumn(sourceText, symbolNode.SourceStart);
        var text = symbolNode.GetSourceText().ToString();

        var node = new GraphNode
        {
            Id = GetNodeId(symbolNode.Offset),
            Type = symbolNode.NodeType.ToString(),
            Name = text,
            Size = 10,
            Location = new CodeLocation
            {
                Line = line,
                Column = column,
                Offset = (int)symbolNode.SourceStart,
                Length = (int)symbolNode.SourceLength
            }
        };

        // Add properties
        node.Properties["symbolId"] = symbolNode.SymbolID;
        node.Properties["nodeType"] = symbolNode.NodeType;
        node.Properties["sourceStart"] = symbolNode.SourceStart;
        node.Properties["sourceLength"] = symbolNode.SourceLength;

        if (text.Length > 0)
        {
            node.Properties["value"] = text;
        }

        return node;
    }

    /// <summary>
    /// Gets the edge type based on the number of PackedNode alternatives.
    /// </summary>
    private static string GetEdgeType(int packedNodeCount)
    {
        if (packedNodeCount > 1)
            return "ambiguous";

        return "default";
    }

    /// <summary>
    /// Creates a stable node identifier from a SymbolNode offset.
    /// </summary>
    private static string GetNodeId(uint offset) => $"node_{offset}";

    /// <summary>
    /// Computes 1-based line and column numbers for a source offset.
    /// </summary>
    private static (int Line, int Column) GetLineColumn(string sourceText, uint offset)
    {
        if (string.IsNullOrEmpty(sourceText) || offset == 0)
            return (1, 1);

        var limit = (int)Math.Min(offset, (uint)sourceText.Length);
        var line = 1;
        var lastLineStart = 0;

        for (int i = 0; i < limit; i++)
        {
            if (sourceText[i] == '\n')
            {
                line++;
                lastLineStart = i + 1;
            }
        }

        return (line, limit - lastLineStart + 1);
    }
}
