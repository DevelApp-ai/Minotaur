namespace Minotaur.UI.Blazor.Models;

/// <summary>
/// Represents a socratic tutoring session
/// </summary>
public class SocraticSession
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Topic { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SocraticQuestion> Questions { get; set; } = new();
    public List<SocraticQuestionResponse> Responses { get; set; } = new();
    public string? CurrentQuestionId { get; set; }
    public int TotalQuestionsAsked { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
}

/// <summary>
/// Represents a socratic question with adaptive learning capabilities
/// </summary>
public class SocraticQuestion
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string Topic { get; set; } = "";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public QuestionType Type { get; set; } = QuestionType.OpenEnded;
    public string[] KeyConcepts { get; set; } = Array.Empty<string>();
    public List<string>? Hints { get; set; }
    public string? ExpectedAnswerPattern { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a user's response to a socratic question with analysis
/// </summary>
public class SocraticQuestionResponse
{
    public string Id { get; set; } = "";
    public string QuestionId { get; set; } = "";
    public string Response { get; set; } = "";
    public ResponseAnalysis Analysis { get; set; } = new();
    public string Feedback { get; set; } = "";
    public SocraticQuestion? NextQuestion { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int HintsUsed { get; set; } = 0;
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Analysis of a user's response for adaptive learning
/// </summary>
public class ResponseAnalysis
{
    public string Id { get; set; } = "";
    public string OriginalResponse { get; set; } = "";
    public ConfidenceLevel ConfidenceLevel { get; set; } = ConfidenceLevel.Unknown;
    public UnderstandingLevel UnderstandingLevel { get; set; } = UnderstandingLevel.Unknown;
    public List<string> ConceptsIdentified { get; set; } = new();
    public List<string> ConceptsMissed { get; set; } = new();
    public List<string> Misconceptions { get; set; } = new();
    public Dictionary<string, double> ConceptConfidence { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? SuggestedNextTopic { get; set; }
}

/// <summary>
/// Tracks a user's learning progress across topics and concepts
/// </summary>
public class UserLearningProgress
{
    public string UserId { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public int TotalQuestions { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
    public TimeSpan TotalLearningTime { get; set; }
    public Dictionary<string, TopicProgress> TopicProgress { get; set; } = new();
    public HashSet<string> MasteredConcepts { get; set; } = new();
    public Dictionary<string, int> ConceptExposure { get; set; } = new();
    public LearningStyle PreferredLearningStyle { get; set; } = LearningStyle.Mixed;
    public DifficultyLevel CurrentLevel { get; set; } = DifficultyLevel.Beginner;
    public List<string> LearningGoals { get; set; } = new();
    public Dictionary<string, object> PersonalizationData { get; set; } = new();
    
    public double OverallAccuracy => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions : 0.0;
}

/// <summary>
/// Progress within a specific topic area
/// </summary>
public class TopicProgress
{
    public string TopicId { get; set; } = "";
    public string TopicName { get; set; } = "";
    public DateTime FirstStarted { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    public int QuestionsAnswered { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
    public TimeSpan TimeSpent { get; set; }
    public CompletionStatus Status { get; set; } = CompletionStatus.NotStarted;
    public DifficultyLevel HighestLevelReached { get; set; } = DifficultyLevel.Beginner;
    public HashSet<string> MasteredConcepts { get; set; } = new();
    public List<string> WeakAreas { get; set; } = new();
    public Dictionary<string, double> ConceptScores { get; set; } = new();
    
    public double TopicAccuracy => QuestionsAnswered > 0 ? (double)CorrectAnswers / QuestionsAnswered : 0.0;
}

/// <summary>
/// Represents a learning topic available in the socratic tutor
/// </summary>
public class SocraticTopic
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public List<string> Prerequisites { get; set; } = new();
    public List<string> LearningObjectives { get; set; } = new();
    public int EstimatedTimeMinutes { get; set; }
    public string[] KeyConcepts { get; set; } = Array.Empty<string>();
    public bool IsAvailable { get; set; } = true;
    public string? IconUrl { get; set; }
    public int QuestionCount { get; set; }
}

/// <summary>
/// Configuration for personalizing the socratic tutor experience
/// </summary>
public class SocraticTutorConfig
{
    public string UserId { get; set; } = "";
    public LearningStyle PreferredStyle { get; set; } = LearningStyle.Mixed;
    public DifficultyLevel StartingLevel { get; set; } = DifficultyLevel.Beginner;
    public int MaxHintsPerQuestion { get; set; } = 3;
    public TimeSpan SessionTimeLimit { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableAdaptiveDifficulty { get; set; } = true;
    public bool EnableHints { get; set; } = true;
    public bool EnableProgressTracking { get; set; } = true;
    public Dictionary<string, object> PersonalizationSettings { get; set; } = new();
}

/// <summary>
/// Represents a learning achievement or milestone
/// </summary>
public class LearningAchievement
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public AchievementType Type { get; set; } = AchievementType.Conceptual;
    public DateTime EarnedDate { get; set; } = DateTime.UtcNow;
    public string Topic { get; set; } = "";
    public string? BadgeIconUrl { get; set; }
    public int PointsAwarded { get; set; } = 0;
}

#region Enumerations

/// <summary>
/// Difficulty levels for questions and topics
/// </summary>
public enum DifficultyLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

/// <summary>
/// Types of socratic questions
/// </summary>
public enum QuestionType
{
    OpenEnded,
    Comparison,
    CauseEffect,
    Application,
    Reflection,
    Hypothesis,
    Analysis,
    Synthesis,
    Evaluation
}

/// <summary>
/// User's confidence level in their response
/// </summary>
public enum ConfidenceLevel
{
    Unknown = 0,
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    VeryHigh = 5
}

/// <summary>
/// Depth of understanding demonstrated in response
/// </summary>
public enum UnderstandingLevel
{
    Unknown = 0,
    Superficial = 1,
    Basic = 2,
    Moderate = 3,
    Deep = 4,
    Expert = 5
}

/// <summary>
/// Preferred learning styles for personalization
/// </summary>
public enum LearningStyle
{
    Visual,
    Auditory,
    Kinesthetic,
    ReadingWriting,
    Mixed
}

/// <summary>
/// Completion status for topics or learning paths
/// </summary>
public enum CompletionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Mastered,
    NeedsReview
}

/// <summary>
/// Types of achievements users can earn
/// </summary>
public enum AchievementType
{
    Conceptual,      // Understanding a key concept
    Persistence,     // Continuing despite difficulties
    Speed,           // Quick correct responses
    Depth,           // Demonstrating deep understanding
    Breadth,         // Covering multiple topics
    Improvement,     // Showing progress over time
    Milestone        // Reaching specific goals
}

#endregion