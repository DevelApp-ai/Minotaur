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
using Microsoft.AspNetCore.Mvc;
using Minotaur.Core.Models.Visualization;
using Minotaur.Core.Services.Visualization;

namespace Minotaur.Core.Controllers;

/// <summary>
/// API controller for CognitiveGraph visualization.
/// 
/// Provides endpoints for visualizing CognitiveGraph data with full
/// support for ambiguity through PackedNode structures.
/// 
/// Key endpoints:
/// - POST /visualization: Full visualization with all PackedNode alternatives
/// - POST /visualization/ambiguities: List all ambiguity points
/// - POST /visualization/interpretations: List all interpretation paths
/// - POST /visualization/select: Select a specific PackedNode path
/// </summary>
[ApiController]
[Route("api/visualization")]
public class VisualizationController : ControllerBase
{
    private readonly ICognitiveGraphVisualizer _visualizer;
    private readonly ILogger<VisualizationController> _logger;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public VisualizationController(
        ICognitiveGraphVisualizer visualizer,
        ILogger<VisualizationController> logger)
    {
        _visualizer = visualizer;
        _logger = logger;
    }

    /// <summary>
    /// Get full CognitiveGraph visualization with all PackedNode alternatives.
    /// 
    /// This is the primary endpoint. It returns visualization data that
    /// preserves all ambiguity in the parse, showing all PackedNode alternatives.
    /// 
    /// Returns:
    /// - All SymbolNodes as graph nodes
    /// - All PackedNode edges (including alternatives)
    /// - Ambiguity information for nodes with multiple PackedNodes
    /// - All possible interpretation paths
    /// </summary>
    /// <param name="request">The visualization request.</param>
    /// <returns>Complete visualization data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CognitiveGraphVisualization), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVisualization(
        [FromBody] VisualizationRequest request)
    {
        try
        {
            // In a real implementation:
            // 1. Parse the source code using the specified grammar
            // 2. Build the CognitiveGraph
            // 3. Generate visualization using the visualizer
            //
            // For demonstration, we'll create a mock CognitiveGraph
            // that demonstrates ambiguity through PackedNodes
            
            var visualization = CreateMockVisualization(request);
            
            return Ok(visualization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CognitiveGraph visualization");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Internal server error",
                Message = ex.Message,
                Details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Get all ambiguity points (nodes with multiple PackedNodes).
    /// 
    /// Returns information about where ambiguity occurs and what the
    /// PackedNode alternatives are.
    /// </summary>
    /// <param name="request">The visualization request.</param>
    /// <returns>List of ambiguity points.</returns>
    [HttpPost("ambiguities")]
    [ProducesResponseType(typeof(List<NodeAmbiguityInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAmbiguityPoints(
        [FromBody] VisualizationRequest request)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Parse the source code
            // 2. Build the CognitiveGraph
            // 3. Get ambiguity points from the visualizer
            
            // For now, return mock data
            var ambiguities = new List<NodeAmbiguityInfo>
            {
                new NodeAmbiguityInfo
                {
                    NodeId = "5",
                    IsAmbiguous = true,
                    Location = new CodeLocation
                    {
                        Start = new Position { Line = 3, Column = 5, Offset = 20 },
                        End = new Position { Line = 3, Column = 15, Offset = 30 }
                    },
                    AlternativeCount = 2,
                    PackedNodes = new List<PackedNodeInfo>
                    {
                        new PackedNodeInfo
                        {
                            Index = 0,
                            RuleId = 101,
                            RuleName = "method_declaration",
                            ChildNodeIds = new List<string> { "6", "7" },
                            IsValid = true
                        },
                        new PackedNodeInfo
                        {
                            Index = 1,
                            RuleId = 102,
                            RuleName = "function_expression",
                            ChildNodeIds = new List<string> { "8" },
                            IsValid = true
                        }
                    }
                }
            };

            return Ok(ambiguities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ambiguity points");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Internal server error",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all possible interpretation paths.
    /// 
    /// Each path represents one way to resolve all ambiguities by
    /// selecting one PackedNode at each ambiguous SymbolNode.
    /// </summary>
    /// <param name="request">The visualization request.</param>
    /// <returns>List of interpretation paths.</returns>
    [HttpPost("interpretations")]
    [ProducesResponseType(typeof(List<InterpretationPath>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterpretationPaths(
        [FromBody] VisualizationRequest request)
    {
        try
        {
            // In a real implementation, this would return all paths
            // For now, return mock data
            var paths = new List<InterpretationPath>
            {
                new InterpretationPath
                {
                    Id = "path_0",
                    Choices = new Dictionary<ulong, int> { [5] = 0 },
                    AppliedRules = new List<string> { "compilation_unit", "class_declaration", "method_declaration" },
                    IsValid = true
                },
                new InterpretationPath
                {
                    Id = "path_1",
                    Choices = new Dictionary<ulong, int> { [5] = 1 },
                    AppliedRules = new List<string> { "compilation_unit", "class_declaration", "function_expression" },
                    IsValid = true
                }
            };

            return Ok(paths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interpretation paths");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Internal server error",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Select a specific PackedNode path to visualize.
    /// 
    /// Returns visualization data showing only the selected PackedNode
    /// choices, similar to a traditional AST but with the understanding
    /// that it's one of multiple valid interpretations.
    /// </summary>
    /// <param name="request">The interpretation selection request.</param>
    /// <returns>Visualization data for the selected interpretation.</returns>
    [HttpPost("select-interpretation")]
    [ProducesResponseType(typeof(CognitiveGraphVisualization), StatusCodes.Status200OK)]
    public async Task<IActionResult> SelectInterpretation(
        [FromBody] InterpretationSelectionRequest request)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Load the CognitiveGraph
            // 2. Apply the selected PackedNode choices
            // 3. Generate visualization for that specific path
            
            // For now, return mock data
            var visualization = CreateMockVisualization(new VisualizationRequest
            {
                SourceCode = request.SourceCode,
                GrammarName = request.GrammarName
            });
            
            // Filter to show only the selected path
            visualization.Mode = VisualizationMode.ShowSelectedInterpretation;
            
            // In a real implementation, we would filter the edges
            // For mock, we'll just mark it
            visualization.GraphData.Properties["selectedPath"] = request.PathId;

            return Ok(visualization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting interpretation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Internal server error",
                Message = ex.Message
            });
        }
    }

    private CognitiveGraphVisualization CreateMockVisualization(VisualizationRequest request)
    {
        // Create visualization that demonstrates PackedNode ambiguity
        return new CognitiveGraphVisualization
        {
            GraphData = new GraphData
            {
                SourceCode = request.SourceCode.Length > 50 
                    ? request.SourceCode[..50] + "..." 
                    : request.SourceCode,
                GrammarName = request.GrammarName,
                Nodes = new List<GraphNode>
                {
                    // Root SymbolNode
                    new GraphNode { 
                        Id = "1", 
                        Type = "compilation_unit", 
                        Name = "compilation_unit", 
                        Group = "declaration", 
                        Size = 15 
                    },
                    // Class SymbolNode
                    new GraphNode { 
                        Id = "2", 
                        Type = "class_declaration", 
                        Name = "TestClass", 
                        Group = "declaration", 
                        Size = 12 
                    },
                    // AMBIGUOUS SymbolNode (has 2 PackedNodes)
                    new GraphNode { 
                        Id = "5", 
                        Type = "expression", 
                        Name = "a+b*c", 
                        Group = "expression", 
                        Size = 10, 
                        IsAmbiguous = true, 
                        AlternativeCount = 2,
                        Location = new CodeLocation
                        {
                            Start = new Position { Line = 3, Column = 5, Offset = 20 },
                            End = new Position { Line = 3, Column = 15, Offset = 30 }
                        }
                    },
                    // Child nodes for PackedNode[0] (method_declaration interpretation)
                    new GraphNode { Id = "6", Type = "identifier", Name = "methodName", Group = "declaration", Size = 8 },
                    new GraphNode { Id = "7", Type = "block", Name = "{...}", Group = "declaration", Size = 8 },
                    // Child node for PackedNode[1] (function_expression interpretation)
                    new GraphNode { Id = "8", Type = "identifier", Name = "functionName", Group = "expression", Size = 8 }
                },
                Edges = new List<GraphEdge>
                {
                    // Hierarchy edges
                    new GraphEdge { Id = "1-2", Source = "1", Target = "2", Type = "hierarchy", Weight = 1 },
                    new GraphEdge { Id = "2-5", Source = "2", Target = "5", Type = "hierarchy", Weight = 1 },
                    
                    // ALTERNATIVE edges from ambiguous node (SymbolNode 5)
                    // PackedNode[0] edges (method_declaration interpretation)
                    new GraphEdge { 
                        Id = "5-6-0", 
                        Source = "5", 
                        Target = "6", 
                        Type = "alternative", 
                        Weight = 1, 
                        IsAlternative = true, 
                        PackedNodeIndex = 0,
                        RuleId = 101
                    },
                    new GraphEdge { 
                        Id = "5-7-0", 
                        Source = "5", 
                        Target = "7", 
                        Type = "alternative", 
                        Weight = 1, 
                        IsAlternative = true, 
                        PackedNodeIndex = 0,
                        RuleId = 101
                    },
                    // PackedNode[1] edges (function_expression interpretation)
                    new GraphEdge { 
                        Id = "5-8-1", 
                        Source = "5", 
                        Target = "8", 
                        Type = "alternative", 
                        Weight = 1, 
                        IsAlternative = true, 
                        PackedNodeIndex = 1,
                        RuleId = 102
                    }
                }
            },
            Ambiguities = new Dictionary<string, NodeAmbiguityInfo>
            {
                ["5"] = new NodeAmbiguityInfo
                {
                    NodeId = "5",
                    IsAmbiguous = true,
                    Location = new CodeLocation
                    {
                        Start = new Position { Line = 3, Column = 5, Offset = 20 },
                        End = new Position { Line = 3, Column = 15, Offset = 30 }
                    },
                    AlternativeCount = 2,
                    PackedNodes = new List<PackedNodeInfo>
                    {
                        new PackedNodeInfo
                        {
                            Index = 0,
                            RuleId = 101,
                            RuleName = "method_declaration",
                            ChildNodeIds = new List<string> { "6", "7" },
                            IsValid = true
                        },
                        new PackedNodeInfo
                        {
                            Index = 1,
                            RuleId = 102,
                            RuleName = "function_expression",
                            ChildNodeIds = new List<string> { "8" },
                            IsValid = true
                        }
                    }
                }
            },
            Mode = VisualizationMode.ShowAllInterpretations,
            HasAmbiguities = true,
            AmbiguityCount = 1
        };
    }

    /// <summary>
    /// Request model for visualization.
    /// </summary>
    public class VisualizationRequest
    {
        /// <summary>Gets or sets the source code to parse.</summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>Gets or sets the grammar name.</summary>
        public string GrammarName { get; set; } = string.Empty;

        /// <summary>Gets or sets visualization options.</summary>
        public VisualizationOptions? Options { get; set; }
    }

    /// <summary>
    /// Request model for interpretation selection.
    /// </summary>
    public class InterpretationSelectionRequest
    {
        /// <summary>Gets or sets the source code.</summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>Gets or sets the grammar name.</summary>
        public string GrammarName { get; set; } = string.Empty;

        /// <summary>Gets or sets the interpretation path ID.</summary>
        public string PathId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error response model.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>Gets or sets the error type.</summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>Gets or sets the error message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Gets or sets additional details.</summary>
        public string? Details { get; set; }
    }
}
