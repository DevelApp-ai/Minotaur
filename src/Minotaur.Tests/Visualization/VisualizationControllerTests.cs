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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minotaur.Core.Controllers;
using Minotaur.Core.Models.Visualization;
using Minotaur.Core.Services.Visualization;
using Moq;
using Xunit;

namespace Minotaur.Tests.Visualization;

/// <summary>
/// Tests for VisualizationController.
/// 
/// These tests verify that the controller:
/// 1. Returns proper visualization data for CognitiveGraph
/// 2. Handles ambiguity points correctly
/// 3. Returns interpretation paths
/// 4. Allows selection of specific interpretations
/// 5. Returns proper error responses
/// </summary>
public class VisualizationControllerTests
{
    private readonly Mock<ICognitiveGraphVisualizer> _visualizerMock;
    private readonly Mock<ILogger<VisualizationController>> _loggerMock;
    private readonly VisualizationController _controller;

    public VisualizationControllerTests()
    {
        _visualizerMock = new Mock<ICognitiveGraphVisualizer>();
        _loggerMock = new Mock<ILogger<VisualizationController>>();
        _controller = new VisualizationController(_visualizerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetVisualization_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        var expectedVisualization = new CognitiveGraphVisualization
        {
            GraphData = new GraphData
            {
                SourceCode = "test code",
                GrammarName = "TestGrammar"
            }
        };

        _visualizerMock.Setup(v => v.GenerateVisualization(It.IsAny<object>(), It.IsAny<VisualizationOptions>()))
            .Returns(expectedVisualization);

        // Act
        var result = await _controller.GetVisualization(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<CognitiveGraphVisualization>(okResult.Value);
        Assert.Equal("test code", returnValue.GraphData.SourceCode);
        Assert.Equal("TestGrammar", returnValue.GraphData.GrammarName);
    }

    [Fact]
    public async Task GetVisualization_EmptyRequest_ReturnsOkResultWithMockData()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "",
            GrammarName = ""
        };

        // Act
        var result = await _controller.GetVisualization(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<CognitiveGraphVisualization>(okResult.Value);
        Assert.NotNull(returnValue);
        Assert.NotNull(returnValue.GraphData);
    }

    [Fact]
    public async Task GetAmbiguityPoints_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        var expectedAmbiguities = new List<NodeAmbiguityInfo>
        {
            new NodeAmbiguityInfo
            {
                NodeId = "1",
                IsAmbiguous = true,
                AlternativeCount = 2
            }
        };

        _visualizerMock.Setup(v => v.GetAmbiguityPoints(It.IsAny<object>()))
            .Returns(expectedAmbiguities);

        // Act
        var result = await _controller.GetAmbiguityPoints(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<NodeAmbiguityInfo>>(okResult.Value);
        Assert.Single(returnValue);
        Assert.True(returnValue[0].IsAmbiguous);
    }

    [Fact]
    public async Task GetInterpretationPaths_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        var expectedPaths = new List<InterpretationPath>
        {
            new InterpretationPath
            {
                Id = "path_0",
                IsValid = true
            },
            new InterpretationPath
            {
                Id = "path_1",
                IsValid = true
            }
        };

        _visualizerMock.Setup(v => v.GetAllInterpretationPaths(It.IsAny<object>()))
            .Returns(expectedPaths);

        // Act
        var result = await _controller.GetInterpretationPaths(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<InterpretationPath>>(okResult.Value);
        Assert.Equal(2, returnValue.Count);
    }

    [Fact]
    public async Task SelectInterpretation_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new VisualizationController.InterpretationSelectionRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar",
            PathId = "path_0"
        };

        var expectedVisualization = new CognitiveGraphVisualization
        {
            Mode = VisualizationMode.ShowSelectedInterpretation
        };

        _visualizerMock.Setup(v => v.GenerateSingleInterpretation(It.IsAny<object>(), It.IsAny<InterpretationPath>()))
            .Returns(expectedVisualization);

        // Act
        var result = await _controller.SelectInterpretation(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<CognitiveGraphVisualization>(okResult.Value);
        Assert.Equal(VisualizationMode.ShowSelectedInterpretation, returnValue.Mode);
    }

    [Fact]
    public async Task GetVisualization_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        _visualizerMock.Setup(v => v.GenerateVisualization(It.IsAny<object>(), It.IsAny<VisualizationOptions>()))
            .Throws<Exception>();

        // Act
        var result = await _controller.GetVisualization(request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAmbiguityPoints_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        _visualizerMock.Setup(v => v.GetAmbiguityPoints(It.IsAny<object>()))
            .Throws<Exception>();

        // Act
        var result = await _controller.GetAmbiguityPoints(request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetInterpretationPaths_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar"
        };

        _visualizerMock.Setup(v => v.GetAllInterpretationPaths(It.IsAny<object>()))
            .Throws<Exception>();

        // Act
        var result = await _controller.GetInterpretationPaths(request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task SelectInterpretation_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VisualizationController.InterpretationSelectionRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar",
            PathId = "path_0"
        };

        _visualizerMock.Setup(v => v.GenerateSingleInterpretation(It.IsAny<object>(), It.IsAny<InterpretationPath>()))
            .Throws<Exception>();

        // Act
        var result = await _controller.SelectInterpretation(request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public void VisualizationRequest_Properties_SetCorrectly()
    {
        // Arrange & Act
        var request = new VisualizationController.VisualizationRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar",
            Options = new VisualizationOptions
            {
                ShowAllAlternatives = true,
                HighlightAmbiguities = true
            }
        };

        // Assert
        Assert.Equal("test code", request.SourceCode);
        Assert.Equal("TestGrammar", request.GrammarName);
        Assert.NotNull(request.Options);
        Assert.True(request.Options.ShowAllAlternatives);
        Assert.True(request.Options.HighlightAmbiguities);
    }

    [Fact]
    public void InterpretationSelectionRequest_Properties_SetCorrectly()
    {
        // Arrange & Act
        var request = new VisualizationController.InterpretationSelectionRequest
        {
            SourceCode = "test code",
            GrammarName = "TestGrammar",
            PathId = "path_0"
        };

        // Assert
        Assert.Equal("test code", request.SourceCode);
        Assert.Equal("TestGrammar", request.GrammarName);
        Assert.Equal("path_0", request.PathId);
    }

    [Fact]
    public void ErrorResponse_Properties_SetCorrectly()
    {
        // Arrange & Act
        var errorResponse = new VisualizationController.ErrorResponse
        {
            Error = "Internal server error",
            Message = "Test error message",
            Details = "Stack trace"
        };

        // Assert
        Assert.Equal("Internal server error", errorResponse.Error);
        Assert.Equal("Test error message", errorResponse.Message);
        Assert.Equal("Stack trace", errorResponse.Details);
    }
}
