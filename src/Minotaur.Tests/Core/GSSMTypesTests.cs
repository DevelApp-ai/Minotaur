using Xunit;
using Minotaur.Core;
using System;

namespace Minotaur.Tests.Core;

public class GSSMTypesTests
{
    [Fact]
    public void TransformationRequest_WithValidData_CreatesInstance()
    {
        // Arrange
        var cognitiveGraph = new CognitiveGraphNode("root", "test");
        var targetGrammar = "target.grammar";

        // Act
        var request = new TransformationRequest(cognitiveGraph, targetGrammar);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(cognitiveGraph, request.CognitiveGraph);
        Assert.Equal(targetGrammar, request.TargetGrammar);
    }

    [Fact]
    public void TransformationRequest_WithNullCognitiveGraph_CreatesInstance()
    {
        // Arrange
        var targetGrammar = "target.grammar";

        // Act
        var request = new TransformationRequest(null!, targetGrammar);

        // Assert
        Assert.NotNull(request);
        Assert.Null(request.CognitiveGraph);
        Assert.Equal(targetGrammar, request.TargetGrammar);
    }

    [Fact]
    public void TransformationRequest_WithNullTargetGrammar_CreatesInstance()
    {
        // Arrange
        var cognitiveGraph = new CognitiveGraphNode("root", "test");

        // Act
        var request = new TransformationRequest(cognitiveGraph, null!);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(cognitiveGraph, request.CognitiveGraph);
        Assert.Null(request.TargetGrammar);
    }

    [Fact]
    public void TransformationResult_WithSuccessStatus_CreatesInstance()
    {
        // Arrange
        var output = "transformed output";

        // Act
        var result = new TransformationResult(true, output);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(output, result.Output);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TransformationResult_WithFailureStatus_CreatesInstance()
    {
        // Arrange
        var errorMessage = "transformation failed";

        // Act
        var result = new TransformationResult(false, null, errorMessage);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void TransformationResult_WithNullOutput_CreatesInstance()
    {
        // Act
        var result = new TransformationResult(true, null);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Output);
    }

    [Fact]
    public void TransformationResult_WithNullErrorMessage_CreatesInstance()
    {
        // Act
        var result = new TransformationResult(false, null, null);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TransformationResult_WithOutputAndError_CreatesInstance()
    {
        // Arrange
        var output = "partial output";
        var error = "partial failure";

        // Act
        var result = new TransformationResult(false, output, error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(output, result.Output);
        Assert.Equal(error, result.ErrorMessage);
    }

    [Fact]
    public void TransformationResult_DefaultErrorMessage_IsNull()
    {
        // Act
        var result = new TransformationResult(true, "output");

        // Assert
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TransformationRequest_PropertiesAreReadable()
    {
        // Arrange
        var cognitiveGraph = new CognitiveGraphNode("root", "test");
        var targetGrammar = "target.grammar";
        var request = new TransformationRequest(cognitiveGraph, targetGrammar);

        // Act
        var graph = request.CognitiveGraph;
        var grammar = request.TargetGrammar;

        // Assert
        Assert.Equal(cognitiveGraph, graph);
        Assert.Equal(targetGrammar, grammar);
    }

    [Fact]
    public void TransformationResult_PropertiesAreReadable()
    {
        // Arrange
        var result = new TransformationResult(true, "output", "error");

        // Act
        var success = result.Success;
        var output = result.Output;
        var error = result.ErrorMessage;

        // Assert
        Assert.True(success);
        Assert.Equal("output", output);
        Assert.Equal("error", error);
    }
}
