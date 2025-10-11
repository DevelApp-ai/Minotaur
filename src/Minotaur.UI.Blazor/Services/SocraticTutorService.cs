using System.Text.Json;
using Minotaur.UI.Blazor.Models;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for managing Socratic tutor interactions, adaptive questioning, and learning progress
/// </summary>
public class SocraticTutorService
{
    private readonly ILogger<SocraticTutorService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Pre-defined question bank organized by topic and difficulty
    private readonly Dictionary<string, List<SocraticQuestion>> _questionBank;
    
    // User progress tracking
    private readonly Dictionary<string, UserLearningProgress> _userProgress;

    public SocraticTutorService(ILogger<SocraticTutorService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        _questionBank = InitializeQuestionBank();
        _userProgress = new Dictionary<string, UserLearningProgress>();
    }

    /// <summary>
    /// Start a new socratic tutoring session for a user
    /// </summary>
    public async Task<SocraticSession> StartSessionAsync(string userId, string topic = "grammar_basics")
    {
        try
        {
            var session = new SocraticSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Topic = topic,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            // Get or create user progress
            var progress = GetOrCreateUserProgress(userId);
            
            // Generate initial question based on user's level and topic
            var initialQuestion = await GenerateNextQuestionAsync(session, progress, null);
            session.Questions.Add(initialQuestion);
            session.CurrentQuestionId = initialQuestion.Id;

            _logger.LogInformation("Started Socratic tutor session {SessionId} for user {UserId} on topic {Topic}", 
                session.Id, userId, topic);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Socratic tutor session for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Submit a response to the current question and get the next question
    /// </summary>
    public async Task<SocraticQuestionResponse> SubmitResponseAsync(string sessionId, string response)
    {
        try
        {
            // This would typically retrieve the session from a database
            // For now, we'll simulate session state management
            var currentQuestion = GetCurrentQuestion(sessionId);
            if (currentQuestion == null)
            {
                throw new InvalidOperationException("No active question found for session");
            }

            // Analyze the user's response
            var analysis = await AnalyzeResponseAsync(response, currentQuestion);
            
            // Update user progress based on response analysis
            var progress = GetOrCreateUserProgress("demo_user"); // In real implementation, get from session
            UpdateUserProgress(progress, analysis);

            // Generate feedback and next question
            var feedback = GenerateFeedback(analysis);
            var nextQuestion = await GenerateNextQuestionAsync(null, progress, analysis);

            var questionResponse = new SocraticQuestionResponse
            {
                Id = Guid.NewGuid().ToString(),
                QuestionId = currentQuestion.Id,
                Response = response,
                Analysis = analysis,
                Feedback = feedback,
                NextQuestion = nextQuestion,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Processed response for session {SessionId}, confidence: {Confidence}", 
                sessionId, analysis.ConfidenceLevel);

            return questionResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing response for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Get a hint for the current question without submitting a full response
    /// </summary>
    public async Task<string> GetHintAsync(string sessionId, int hintLevel = 1)
    {
        try
        {
            var currentQuestion = GetCurrentQuestion(sessionId);
            if (currentQuestion == null)
            {
                return "No active question found.";
            }

            // Provide progressive hints based on hint level
            var hints = currentQuestion.Hints ?? new List<string>();
            if (hintLevel <= hints.Count)
            {
                return hints[hintLevel - 1];
            }

            // Generate contextual hint if no predefined hint exists
            return GenerateContextualHint(currentQuestion, hintLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hint for session {SessionId}", sessionId);
            return "Sorry, I couldn't generate a hint at this time.";
        }
    }

    /// <summary>
    /// Get user's learning progress and achievements
    /// </summary>
    public async Task<UserLearningProgress> GetProgressAsync(string userId)
    {
        return GetOrCreateUserProgress(userId);
    }

    /// <summary>
    /// Get available topics for socratic tutoring
    /// </summary>
    public async Task<List<SocraticTopic>> GetAvailableTopicsAsync()
    {
        return new List<SocraticTopic>
        {
            new() { Id = "grammar_basics", Name = "Grammar Fundamentals", Description = "Learn the basic concepts of formal grammars", Difficulty = DifficultyLevel.Beginner, EstimatedTimeMinutes = 15, QuestionCount = 5 },
            new() { Id = "cognitive_graphs", Name = "Cognitive Graph Architecture", Description = "Understand how cognitive graphs represent language structures", Difficulty = DifficultyLevel.Intermediate, EstimatedTimeMinutes = 20, QuestionCount = 7 },
            new() { Id = "stepparser", Name = "StepParser Integration", Description = "Explore parsing and integration capabilities", Difficulty = DifficultyLevel.Intermediate, EstimatedTimeMinutes = 25, QuestionCount = 8 },
            new() { Id = "symbolic_analysis", Name = "Symbolic Analysis", Description = "Advanced analysis of grammar execution paths", Difficulty = DifficultyLevel.Advanced, EstimatedTimeMinutes = 30, QuestionCount = 10 },
            new() { Id = "version_control", Name = "Grammar Version Control", Description = "Managing grammar changes and collaboration", Difficulty = DifficultyLevel.Beginner, EstimatedTimeMinutes = 12, QuestionCount = 4 }
        };
    }

    #region Private Methods

    private Dictionary<string, List<SocraticQuestion>> InitializeQuestionBank()
    {
        return new Dictionary<string, List<SocraticQuestion>>
        {
            ["grammar_basics"] = new List<SocraticQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "What do you think a grammar is, and why might we need formal definitions for languages?",
                    Topic = "grammar_basics",
                    Difficulty = DifficultyLevel.Beginner,
                    Type = QuestionType.OpenEnded,
                    KeyConcepts = new[] { "grammar", "formal definition", "language structure" },
                    Hints = new List<string>
                    { 
                        "Think about how we learn the rules of a natural language like English.",
                        "Consider what happens when a computer needs to understand code.",
                        "What would happen if there were no rules about how to write valid sentences or code?"
                    }
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "How do you think a visual representation of grammar rules might help us better than just text?",
                    Topic = "grammar_basics",
                    Difficulty = DifficultyLevel.Beginner,
                    Type = QuestionType.Comparison,
                    KeyConcepts = new[] { "visual representation", "graph structure", "cognitive benefits" },
                    Hints = new List<string>
                    {
                        "Think about how you learn better - reading instructions or seeing a diagram?",
                        "Consider how complex relationships are easier to see when drawn out.",
                        "What are the advantages of mind maps over linear notes?"
                    }
                }
            },
            ["cognitive_graphs"] = new List<SocraticQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "What do you think is the difference between a terminal node and a non-terminal node in a grammar graph?",
                    Topic = "cognitive_graphs",
                    Difficulty = DifficultyLevel.Intermediate,
                    Type = QuestionType.Comparison,
                    KeyConcepts = new[] { "terminal node", "non-terminal node", "grammar hierarchy" },
                    Hints = new List<string>
                    {
                        "Think about building blocks vs. compound structures.",
                        "Consider what cannot be broken down further vs. what can be expanded.",
                        "In language, think about individual words vs. phrases or sentences."
                    }
                }
            },
            ["stepparser"] = new List<SocraticQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Why do you think we need to parse source code into a structured format before we can work with it?",
                    Topic = "stepparser",
                    Difficulty = DifficultyLevel.Intermediate,
                    Type = QuestionType.CauseEffect,
                    KeyConcepts = new[] { "parsing", "structure", "analysis" },
                    Hints = new List<string>
                    {
                        "Think about how you would analyze a complex sentence - what steps would you take?",
                        "Consider what a computer needs to understand about code to help you modify it.",
                        "What information is hidden in plain text that becomes visible when structured?"
                    }
                }
            }
        };
    }

    private UserLearningProgress GetOrCreateUserProgress(string userId)
    {
        if (!_userProgress.ContainsKey(userId))
        {
            _userProgress[userId] = new UserLearningProgress
            {
                UserId = userId,
                StartDate = DateTime.UtcNow,
                TopicProgress = new Dictionary<string, TopicProgress>()
            };
        }
        return _userProgress[userId];
    }

    private async Task<SocraticQuestion> GenerateNextQuestionAsync(
        SocraticSession? session, 
        UserLearningProgress progress, 
        ResponseAnalysis? previousAnalysis)
    {
        // Determine appropriate topic and difficulty based on progress and previous response
        var topic = session?.Topic ?? "grammar_basics";
        var difficulty = DetermineNextDifficulty(progress, previousAnalysis);

        // Get available questions for the topic
        if (_questionBank.TryGetValue(topic, out var questions))
        {
            // Filter questions by difficulty and concepts not yet mastered
            var availableQuestions = questions
                .Where(q => q.Difficulty <= difficulty)
                .Where(q => !IsConceptMastered(progress, q.KeyConcepts))
                .ToList();

            if (availableQuestions.Any())
            {
                // Select question based on learning path optimization
                return availableQuestions.First(); // Simple selection for now
            }
        }

        // Fallback to a default question if no specific question is found
        return new SocraticQuestion
        {
            Id = Guid.NewGuid().ToString(),
            Text = "What would you like to explore about Minotaur's grammar development capabilities?",
            Topic = topic,
            Difficulty = DifficultyLevel.Beginner,
            Type = QuestionType.OpenEnded,
            KeyConcepts = new[] { "exploration", "interest" }
        };
    }

    private async Task<ResponseAnalysis> AnalyzeResponseAsync(string response, SocraticQuestion question)
    {
        // Simple analysis - in a real implementation, this would use NLP and AI
        var analysis = new ResponseAnalysis
        {
            Id = Guid.NewGuid().ToString(),
            OriginalResponse = response,
            Timestamp = DateTime.UtcNow
        };

        // Basic keyword analysis
        var responseWords = response.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keyConceptMatches = question.KeyConcepts
            .Count(concept => responseWords.Any(word => concept.ToLower().Contains(word) || word.Contains(concept.ToLower())));

        // Determine confidence based on response length and keyword matches
        if (response.Length < 10)
        {
            analysis.ConfidenceLevel = ConfidenceLevel.Low;
            analysis.UnderstandingLevel = UnderstandingLevel.Superficial;
        }
        else if (keyConceptMatches >= question.KeyConcepts.Length / 2)
        {
            analysis.ConfidenceLevel = ConfidenceLevel.High;
            analysis.UnderstandingLevel = UnderstandingLevel.Deep;
        }
        else
        {
            analysis.ConfidenceLevel = ConfidenceLevel.Medium;
            analysis.UnderstandingLevel = UnderstandingLevel.Moderate;
        }

        analysis.ConceptsIdentified = question.KeyConcepts
            .Where(concept => responseWords.Any(word => 
                concept.ToLower().Contains(word) || word.Contains(concept.ToLower())))
            .ToList();

        analysis.ConceptsMissed = question.KeyConcepts
            .Except(analysis.ConceptsIdentified)
            .ToList();

        return analysis;
    }

    private string GenerateFeedback(ResponseAnalysis analysis)
    {
        var feedback = new List<string>();

        // Encouraging opening based on confidence level
        switch (analysis.ConfidenceLevel)
        {
            case ConfidenceLevel.High:
                var highEncouragements = new[] 
                {
                    "Excellent thinking! You've demonstrated a solid understanding.",
                    "That's a thoughtful response! You're really grasping these concepts.",
                    "Great insight! Your understanding is coming through clearly."
                };
                feedback.Add(highEncouragements[Random.Shared.Next(highEncouragements.Length)]);
                break;
            case ConfidenceLevel.Medium:
                var mediumEncouragements = new[]
                {
                    "Good start! You're on the right track.",
                    "I can see you're thinking about this carefully. You're making progress!",
                    "That's a solid foundation. Let's build on what you've shared."
                };
                feedback.Add(mediumEncouragements[Random.Shared.Next(mediumEncouragements.Length)]);
                break;
            case ConfidenceLevel.Low:
                var lowEncouragements = new[]
                {
                    "That's a beginning. Let's explore this further together.",
                    "I appreciate you sharing your thoughts. Every insight helps us learn.",
                    "Let's work through this together. Your perspective is valuable."
                };
                feedback.Add(lowEncouragements[Random.Shared.Next(lowEncouragements.Length)]);
                break;
        }

        // Acknowledge what they got right
        if (analysis.ConceptsIdentified.Any())
        {
            var conceptAcknowledgments = new[]
            {
                $"I notice you mentioned {string.Join(", ", analysis.ConceptsIdentified)}. That's exactly what I was hoping to hear!",
                $"You've identified key concepts: {string.Join(", ", analysis.ConceptsIdentified)}. Well done!",
                $"Great! You're thinking about {string.Join(", ", analysis.ConceptsIdentified)} - these are crucial ideas."
            };
            feedback.Add(conceptAcknowledgments[Random.Shared.Next(conceptAcknowledgments.Length)]);
        }

        // Guide toward missing concepts without being too direct
        if (analysis.ConceptsMissed.Any())
        {
            var guidingQuestions = new[]
            {
                $"Now, let's also consider how {string.Join(", ", analysis.ConceptsMissed)} might fit into this picture.",
                $"What do you think about the role of {string.Join(", ", analysis.ConceptsMissed)} in this context?",
                $"I'm curious about your thoughts on {string.Join(", ", analysis.ConceptsMissed)} - how might that connect?"
            };
            feedback.Add(guidingQuestions[Random.Shared.Next(guidingQuestions.Length)]);
        }

        return string.Join(" ", feedback);
    }

    private DifficultyLevel DetermineNextDifficulty(UserLearningProgress progress, ResponseAnalysis? analysis)
    {
        if (analysis == null)
            return DifficultyLevel.Beginner;

        // Adjust difficulty based on user's demonstrated understanding
        return analysis.UnderstandingLevel switch
        {
            UnderstandingLevel.Deep => DifficultyLevel.Advanced,
            UnderstandingLevel.Moderate => DifficultyLevel.Intermediate,
            _ => DifficultyLevel.Beginner
        };
    }

    private bool IsConceptMastered(UserLearningProgress progress, string[] concepts)
    {
        // Simple check - in reality, this would be more sophisticated
        return false; // For now, assume no concepts are fully mastered to allow continued learning
    }

    private void UpdateUserProgress(UserLearningProgress progress, ResponseAnalysis analysis)
    {
        progress.TotalQuestions++;
        progress.LastActivity = DateTime.UtcNow;

        if (analysis.ConfidenceLevel == ConfidenceLevel.High)
        {
            progress.CorrectAnswers++;
        }

        // Update concept understanding
        foreach (var concept in analysis.ConceptsIdentified)
        {
            if (!progress.MasteredConcepts.Contains(concept))
            {
                progress.ConceptExposure[concept] = progress.ConceptExposure.GetValueOrDefault(concept, 0) + 1;
                
                // Consider concept mastered after multiple successful exposures
                if (progress.ConceptExposure[concept] >= 3 && analysis.ConfidenceLevel >= ConfidenceLevel.Medium)
                {
                    progress.MasteredConcepts.Add(concept);
                }
            }
        }
    }

    private SocraticQuestion? GetCurrentQuestion(string sessionId)
    {
        // In a real implementation, this would retrieve from session state or database
        // For now, return a sample question
        return new SocraticQuestion
        {
            Id = Guid.NewGuid().ToString(),
            Text = "What do you think makes a grammar useful for understanding code structure?",
            Topic = "grammar_basics",
            Difficulty = DifficultyLevel.Beginner,
            Type = QuestionType.OpenEnded,
            KeyConcepts = new[] { "grammar", "structure", "usefulness" },
            Hints = new List<string>
            {
                "Think about what you need to understand when reading code written by someone else.",
                "Consider how rules help us organize and make sense of complex information."
            }
        };
    }

    private string GenerateContextualHint(SocraticQuestion question, int hintLevel)
    {
        // Generate progressively more specific hints
        return hintLevel switch
        {
            1 => "Think about the key concepts: " + string.Join(", ", question.KeyConcepts),
            2 => "Consider breaking this down into smaller parts. What's the most basic element here?",
            3 => "Try connecting this to something you already know. What's similar to this in your experience?",
            _ => "Sometimes it helps to think out loud. What's your first instinct about this question?"
        };
    }

    #endregion
}