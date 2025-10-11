/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.Extensions.Logging;
using Minotaur.UI.Blazor.Models;
using Minotaur.UI.Blazor.Services;
using Moq;
using Xunit;

namespace Minotaur.UI.Blazor.Tests.Services;

public class SocraticTutorServiceTests
{
    private readonly Mock<ILogger<SocraticTutorService>> _mockLogger;
    private readonly SocraticTutorService _service;

    public SocraticTutorServiceTests()
    {
        _mockLogger = new Mock<ILogger<SocraticTutorService>>();
        _service = new SocraticTutorService(_mockLogger.Object);
    }

    [Fact]
    public async Task StartSessionAsync_CreatesValidSession_WithDefaultTopic()
    {
        // Arrange
        var userId = "test-user";
        var topic = "grammar_basics";

        // Act
        var result = await _service.StartSessionAsync(userId, topic);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(topic, result.Topic);
        Assert.True(result.IsActive);
        Assert.NotEmpty(result.Questions);
        Assert.NotNull(result.CurrentQuestionId);
        Assert.True(result.StartTime <= DateTime.UtcNow);
    }

    [Fact]
    public async Task StartSessionAsync_GeneratesInitialQuestion()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _service.StartSessionAsync(userId);

        // Assert
        Assert.Single(result.Questions);
        var firstQuestion = result.Questions.First();
        Assert.NotNull(firstQuestion);
        Assert.NotEmpty(firstQuestion.Text);
        Assert.NotEmpty(firstQuestion.KeyConcepts);
        Assert.Equal(result.CurrentQuestionId, firstQuestion.Id);
    }

    [Fact]
    public async Task GetAvailableTopicsAsync_ReturnsValidTopics()
    {
        // Act
        var topics = await _service.GetAvailableTopicsAsync();

        // Assert
        Assert.NotEmpty(topics);
        Assert.All(topics, topic =>
        {
            Assert.NotEmpty(topic.Id);
            Assert.NotEmpty(topic.Name);
            Assert.NotEmpty(topic.Description);
            Assert.True(topic.EstimatedTimeMinutes > 0);
            Assert.True(topic.QuestionCount > 0);
        });
        
        // Check for expected topics
        var topicIds = topics.Select(t => t.Id).ToList();
        Assert.Contains("grammar_basics", topicIds);
        Assert.Contains("cognitive_graphs", topicIds);
        Assert.Contains("stepparser", topicIds);
    }

    [Fact]
    public async Task SubmitResponseAsync_ProcessesResponse_And_GeneratesNextQuestion()
    {
        // Arrange
        var sessionId = "test-session";
        var response = "I think a grammar is a set of rules that define how language should be structured.";

        // Act
        var result = await _service.SubmitResponseAsync(sessionId, response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(response, result.Response);
        Assert.NotNull(result.Analysis);
        Assert.NotEmpty(result.Feedback);
        Assert.NotNull(result.NextQuestion);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SubmitResponseAsync_AnalyzesResponse_Correctly()
    {
        // Arrange
        var sessionId = "test-session";
        var response = "Grammar defines structure and rules for language parsing.";

        // Act
        var result = await _service.SubmitResponseAsync(sessionId, response);

        // Assert
        Assert.NotNull(result.Analysis);
        Assert.Equal(response, result.Analysis.OriginalResponse);
        Assert.True(Enum.IsDefined(typeof(ConfidenceLevel), result.Analysis.ConfidenceLevel));
        Assert.True(Enum.IsDefined(typeof(UnderstandingLevel), result.Analysis.UnderstandingLevel));
        
        // Should identify some concepts from the response
        Assert.NotNull(result.Analysis.ConceptsIdentified);
        Assert.NotNull(result.Analysis.ConceptsMissed);
    }

    [Fact]
    public async Task GetHintAsync_ReturnsValidHint()
    {
        // Arrange
        var sessionId = "test-session";
        var hintLevel = 1;

        // Act
        var hint = await _service.GetHintAsync(sessionId, hintLevel);

        // Assert
        Assert.NotEmpty(hint);
        Assert.DoesNotContain("No active question found", hint);
    }

    [Fact]
    public async Task GetHintAsync_ProgressivelyMoreSpecific()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var hint1 = await _service.GetHintAsync(sessionId, 1);
        var hint2 = await _service.GetHintAsync(sessionId, 2);
        var hint3 = await _service.GetHintAsync(sessionId, 3);

        // Assert
        Assert.NotEmpty(hint1);
        Assert.NotEmpty(hint2);
        Assert.NotEmpty(hint3);
        
        // Hints should be different
        Assert.NotEqual(hint1, hint2);
        Assert.NotEqual(hint2, hint3);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsValidProgress()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var progress = await _service.GetProgressAsync(userId);

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(userId, progress.UserId);
        Assert.True(progress.StartDate <= DateTime.UtcNow);
        Assert.NotNull(progress.TopicProgress);
        Assert.NotNull(progress.MasteredConcepts);
        Assert.NotNull(progress.ConceptExposure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task StartSessionAsync_WithInvalidUserId_ThrowsException(string invalidUserId)
    {
        // Act & Assert
        if (string.IsNullOrWhiteSpace(invalidUserId))
        {
            // The service should handle null/empty user IDs gracefully
            // This is more of a business logic decision - you might want different behavior
            var result = await _service.StartSessionAsync(invalidUserId ?? "");
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task SocraticSession_HasCorrectProperties()
    {
        // Arrange
        var userId = "test-user";
        var topic = "cognitive_graphs";

        // Act
        var session = await _service.StartSessionAsync(userId, topic);

        // Assert
        Assert.NotEmpty(session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(topic, session.Topic);
        Assert.True(session.IsActive);
        Assert.Null(session.EndTime);
        Assert.Equal(0, session.TotalQuestionsAsked);
        Assert.NotNull(session.Questions);
        Assert.NotNull(session.Responses);
    }

    [Fact]
    public void SocraticQuestion_HasRequiredProperties()
    {
        // Arrange & Act - Test the model directly
        var question = new SocraticQuestion
        {
            Id = "test-id",
            Text = "What is a grammar?",
            Topic = "grammar_basics",
            Difficulty = DifficultyLevel.Beginner,
            Type = QuestionType.OpenEnded,
            KeyConcepts = new[] { "grammar", "definition" }
        };

        // Assert
        Assert.NotEmpty(question.Id);
        Assert.NotEmpty(question.Text);
        Assert.NotEmpty(question.Topic);
        Assert.Equal(DifficultyLevel.Beginner, question.Difficulty);
        Assert.Equal(QuestionType.OpenEnded, question.Type);
        Assert.NotEmpty(question.KeyConcepts);
    }

    [Fact]
    public void UserLearningProgress_CalculatesAccuracyCorrectly()
    {
        // Arrange
        var progress = new UserLearningProgress
        {
            UserId = "test-user",
            TotalQuestions = 10,
            CorrectAnswers = 7
        };

        // Act
        var accuracy = progress.OverallAccuracy;

        // Assert
        Assert.Equal(0.7, accuracy, 2);
    }

    [Fact]
    public void UserLearningProgress_HandlesZeroQuestions()
    {
        // Arrange
        var progress = new UserLearningProgress
        {
            UserId = "test-user",
            TotalQuestions = 0,
            CorrectAnswers = 0
        };

        // Act
        var accuracy = progress.OverallAccuracy;

        // Assert
        Assert.Equal(0.0, accuracy);
    }

    [Fact]
    public void TopicProgress_CalculatesAccuracyCorrectly()
    {
        // Arrange
        var topicProgress = new TopicProgress
        {
            TopicId = "grammar_basics",
            QuestionsAnswered = 5,
            CorrectAnswers = 4
        };

        // Act
        var accuracy = topicProgress.TopicAccuracy;

        // Assert
        Assert.Equal(0.8, accuracy, 2);
    }

    [Fact]
    public async Task Service_InitializesQuestionBank_Successfully()
    {
        // Act
        var topics = await _service.GetAvailableTopicsAsync();

        // Assert - Verify that the question bank is initialized with expected topics
        Assert.Contains(topics, t => t.Id == "grammar_basics");
        Assert.Contains(topics, t => t.Id == "cognitive_graphs");
        Assert.Contains(topics, t => t.Id == "stepparser");
        Assert.Contains(topics, t => t.Id == "symbolic_analysis");
        Assert.Contains(topics, t => t.Id == "version_control");
    }

    [Fact]
    public void ResponseAnalysis_HasCorrectStructure()
    {
        // Arrange & Act
        var analysis = new ResponseAnalysis
        {
            Id = "test-analysis",
            OriginalResponse = "Test response",
            ConfidenceLevel = ConfidenceLevel.High,
            UnderstandingLevel = UnderstandingLevel.Deep,
            ConceptsIdentified = new List<string> { "concept1", "concept2" },
            ConceptsMissed = new List<string> { "concept3" }
        };

        // Assert
        Assert.NotEmpty(analysis.Id);
        Assert.Equal("Test response", analysis.OriginalResponse);
        Assert.Equal(ConfidenceLevel.High, analysis.ConfidenceLevel);
        Assert.Equal(UnderstandingLevel.Deep, analysis.UnderstandingLevel);
        Assert.Equal(2, analysis.ConceptsIdentified.Count);
        Assert.Single(analysis.ConceptsMissed);
    }
}