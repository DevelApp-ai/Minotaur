using Minotaur.Core;
using Minotaur.Learning;
using Minotaur.Monitoring;
using Xunit;

namespace Minotaur.Tests.Learning;

public class LearningTypesTests
{
    [Fact]
    public void LearningData_RequiredProperties_InitializedCorrectly()
    {
        // Arrange
        var sourceEngineId = "engine-123";
        var stateSnapshot = CreateGSSMState();
        var transformations = new List<TransformationResult>();
        var performance = CreatePerformanceMetrics();

        // Act
        var learningData = new LearningData
        {
            SourceEngineId = sourceEngineId,
            StateSnapshot = stateSnapshot,
            SuccessfulTransformations = transformations,
            Performance = performance
        };

        // Assert
        Assert.Equal(sourceEngineId, learningData.SourceEngineId);
        Assert.Same(stateSnapshot, learningData.StateSnapshot);
        Assert.Same(transformations, learningData.SuccessfulTransformations);
        Assert.Same(performance, learningData.Performance);
    }

    [Fact]
    public void LearningData_Timestamp_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var learningData = new LearningData
        {
            SourceEngineId = "engine-1",
            StateSnapshot = CreateGSSMState(),
            SuccessfulTransformations = new List<TransformationResult>(),
            Performance = CreatePerformanceMetrics()
        };

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(learningData.Timestamp >= before);
        Assert.True(learningData.Timestamp <= after);
    }

    [Fact]
    public void LearningQuery_AllProperties_OptionalAndNullable()
    {
        // Act
        var query = new LearningQuery();

        // Assert
        Assert.Null(query.EngineId);
        Assert.Null(query.FromTimestamp);
        Assert.Null(query.ToTimestamp);
        Assert.Null(query.TransformationTypes);
    }

    [Fact]
    public void LearningQuery_Properties_CanBeSet()
    {
        // Arrange
        var engineId = "engine-456";
        var fromTimestamp = DateTime.UtcNow.AddDays(-7);
        var toTimestamp = DateTime.UtcNow;
        var transformationTypes = new List<string> { "Refactor", "Optimize" };

        // Act
        var query = new LearningQuery
        {
            EngineId = engineId,
            FromTimestamp = fromTimestamp,
            ToTimestamp = toTimestamp,
            TransformationTypes = transformationTypes
        };

        // Assert
        Assert.Equal(engineId, query.EngineId);
        Assert.Equal(fromTimestamp, query.FromTimestamp);
        Assert.Equal(toTimestamp, query.ToTimestamp);
        Assert.Equal(transformationTypes, query.TransformationTypes);
    }

    [Fact]
    public void LearningQueryResult_RequiredProperties_InitializedCorrectly()
    {
        // Arrange
        var results = new List<LearningData>
        {
            CreateLearningData("engine-1")
        };
        var totalCount = 100;

        // Act
        var queryResult = new LearningQueryResult
        {
            Results = results,
            TotalCount = totalCount
        };

        // Assert
        Assert.Same(results, queryResult.Results);
        Assert.Equal(totalCount, queryResult.TotalCount);
        Assert.False(queryResult.HasMore);
    }

    [Fact]
    public void LearningQueryResult_HasMore_IndicatesMoreResults()
    {
        // Arrange & Act
        var queryResult = new LearningQueryResult
        {
            Results = new List<LearningData>(),
            TotalCount = 150,
            HasMore = true
        };

        // Assert
        Assert.True(queryResult.HasMore);
        Assert.Equal(150, queryResult.TotalCount);
    }

    [Fact]
    public void LearningQueryResult_EmptyResults_ValidState()
    {
        // Act
        var queryResult = new LearningQueryResult
        {
            Results = new List<LearningData>(),
            TotalCount = 0,
            HasMore = false
        };

        // Assert
        Assert.Empty(queryResult.Results);
        Assert.Equal(0, queryResult.TotalCount);
        Assert.False(queryResult.HasMore);
    }

    // Helper methods
    private static GSSMState CreateGSSMState(string id = "state-1")
    {
        return new GSSMState
        {
            Id = id,
            Timestamp = DateTime.UtcNow
        };
    }

    private static PerformanceMetrics CreatePerformanceMetrics()
    {
        return new PerformanceMetrics
        {
            AverageResponseTime = 100.0,
            ThroughputPerSecond = 10.0
        };
    }

    private static LearningData CreateLearningData(string engineId)
    {
        return new LearningData
        {
            SourceEngineId = engineId,
            StateSnapshot = CreateGSSMState(),
            SuccessfulTransformations = new List<TransformationResult>(),
            Performance = CreatePerformanceMetrics()
        };
    }
}
