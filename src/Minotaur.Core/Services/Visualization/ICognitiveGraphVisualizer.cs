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

using CognitiveGraph.Accessors;
using Minotaur.Core.Models.Visualization;
using CGraph = CognitiveGraph.CognitiveGraph;

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
        CGraph graph,
        VisualizationOptions? options = null);

    /// <summary>
    /// Get all ambiguity points (nodes with multiple PackedNodes).
    /// </summary>
    /// <param name="graph">The CognitiveGraph to analyze.</param>
    /// <returns>List of ambiguity information.</returns>
    List<NodeAmbiguityInfo> GetAmbiguityPoints(CGraph graph);

    /// <summary>
    /// Get all possible interpretation paths through the graph.
    /// Each path represents one way to resolve all ambiguities.
    /// </summary>
    /// <param name="graph">The CognitiveGraph to analyze.</param>
    /// <returns>List of interpretation paths.</returns>
    List<InterpretationPath> GetAllInterpretationPaths(CGraph graph);

    /// <summary>
    /// Generate visualization for a specific interpretation path.
    /// Shows only the selected PackedNode choices.
    /// </summary>
    /// <param name="graph">The CognitiveGraph.</param>
    /// <param name="path">The interpretation path to visualize.</param>
    /// <returns>Visualization data for the single interpretation.</returns>
    CognitiveGraphVisualization GenerateSingleInterpretation(
        CGraph graph,
        InterpretationPath path);
}
