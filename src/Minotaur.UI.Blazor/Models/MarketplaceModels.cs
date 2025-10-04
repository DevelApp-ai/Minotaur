namespace Minotaur.UI.Blazor.Models;

public class MarketplaceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DetailedDescription { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public float Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Downloads { get; set; }
    public string LatestVersion { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Language { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public string License { get; set; } = "";
    public string PackageSize { get; set; } = "";
    public bool IsInstalled { get; set; }
    public List<DependencyInfo>? Dependencies { get; set; }
    public MarketplaceItemType ItemType { get; set; } = MarketplaceItemType.Grammar;
    public string? PreviewImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string Visibility { get; set; } = "public"; // public, unlisted, private
    public bool RequiresAuthentication { get; set; } = false;
}

public class DependencyInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsOptional { get; set; }
}

public class InstallationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? InstalledVersion { get; set; }
    public List<string> InstalledFiles { get; set; } = new();

    public static InstallationResult Successful(string version, List<string>? files = null)
    {
        return new InstallationResult
        {
            Success = true,
            InstalledVersion = version,
            InstalledFiles = files ?? new List<string>()
        };
    }

    public static InstallationResult Failed(string error)
    {
        return new InstallationResult { Success = false, ErrorMessage = error };
    }
}

public class MarketplaceSearchFilters
{
    public string? Language { get; set; }
    public string? Category { get; set; }
    public bool? ShiftDetection { get; set; }
    public bool? MultiVersion { get; set; }
    public bool? SyntaxHighlighting { get; set; }
    public string? SourceLanguage { get; set; }
    public string? TargetLanguage { get; set; }
    public float? MinRating { get; set; }
    public string? License { get; set; }
    public DateTime? UpdatedAfter { get; set; }
}

public class PipelineTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public List<PipelineParameter> Parameters { get; set; } = new();
    public List<PipelineStep> Steps { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PipelineParameter
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string Description { get; set; } = "";
}

public class PipelineStep
{
    public string Name { get; set; } = "";
    public string Uses { get; set; } = "";
    public Dictionary<string, object> With { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();
}

public class GrammarPackage
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Language { get; set; } = "";
    public List<string> SupportedVersions { get; set; } = new();
    public GrammarFeatures Features { get; set; } = new();
    public byte[] PackageData { get; set; } = Array.Empty<byte>();
    public string CheckSum { get; set; } = "";
}

public class GrammarFeatures
{
    public bool SyntaxHighlighting { get; set; }
    public bool ErrorDetection { get; set; }
    public bool ShiftDetection { get; set; }
    public bool MultiVersionSupport { get; set; }
    public bool SemanticAnalysis { get; set; }
    public bool CodeCompletion { get; set; }
}

public class TranspilerPackage
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    public TranspilerFeatures Features { get; set; } = new();
    public byte[] PackageData { get; set; } = Array.Empty<byte>();
    public string CheckSum { get; set; } = "";
}

public class TranspilerFeatures
{
    public bool TypePreservation { get; set; }
    public bool CommentMigration { get; set; }
    public bool AsyncAwaitSupport { get; set; }
    public bool LinqConversion { get; set; }
    public bool CustomMappings { get; set; }
}

public enum MarketplaceItemType
{
    Grammar,
    Transpiler,
    PipelineTemplate,
    CodeTemplate,
    ProjectTemplate,
    Snippet
}

public enum TemplateCategory
{
    // Code Templates
    ClassTemplate,
    InterfaceTemplate,
    ControllerTemplate,
    ServiceTemplate,
    ComponentTemplate,

    // Project Templates
    WebApiProject,
    BlazorProject,
    ConsoleProject,
    LibraryProject,

    // Grammar Templates
    ParserGrammar,
    LexerGrammar,
    CombinedGrammar,

    // Pipeline Templates
    CITemplate,
    CDTemplate,
    TestingTemplate,
    DeploymentTemplate,

    // Snippet Templates
    CodeSnippet,
    ConfigSnippet,
    DocumentationSnippet
}

public class MarketplaceSearchRequest
{
    public string Query { get; set; } = "";
    public MarketplaceItemType ItemType { get; set; }
    public MarketplaceSearchFilters Filters { get; set; } = new();
    public string SortBy { get; set; } = "relevance";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MarketplaceSearchResponse
{
    public List<MarketplaceItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => (Page * PageSize) < TotalCount;
    public bool HasPreviousPage => Page > 1;
}

public class PublishRequest
{
    public MarketplaceItem Metadata { get; set; } = new();
    public byte[] PackageData { get; set; } = Array.Empty<byte>();
    public string License { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public string ReadmeContent { get; set; } = "";
    public string ChangelogContent { get; set; } = "";
}

public class PublishResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PublishedId { get; set; }
    public string? PublishedVersion { get; set; }

    public static PublishResult Successful(string id, string version)
    {
        return new PublishResult
        {
            Success = true,
            PublishedId = id,
            PublishedVersion = version
        };
    }

    public static PublishResult Failed(string error)
    {
        return new PublishResult { Success = false, ErrorMessage = error };
    }
}

// Authentication Models
public class UserProfile
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public UserSubscription Subscription { get; set; } = new();
    public UserPreferences Preferences { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UserSubscription
{
    public string Plan { get; set; } = "free"; // free, pro, enterprise
    public DateTime? ExpiresAt { get; set; }
    public int DownloadsRemaining { get; set; } = 10;
    public int PrivateRepositoriesLimit { get; set; } = 0;
    public bool CanPublishPremium { get; set; } = false;
}

public class UserPreferences
{
    public string PreferredLanguage { get; set; } = "en";
    public string Theme { get; set; } = "dark";
    public bool EmailNotifications { get; set; } = true;
    public bool ShowBetaFeatures { get; set; } = false;
    public List<string> FavoriteCategories { get; set; } = new();
    public Dictionary<string, object> EditorSettings { get; set; } = new();
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfile? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public static AuthenticationResult Success(UserProfile user, string accessToken, string? refreshToken = null) =>
        new AuthenticationResult { IsSuccess = true, User = user, AccessToken = accessToken, RefreshToken = refreshToken };

    public static AuthenticationResult Failed(string error) =>
        new AuthenticationResult { IsSuccess = false, ErrorMessage = error };
}

public class LoginRequest
{
    public string EmailOrUsername { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; } = false;
}

public class RegistrationRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool AcceptTerms { get; set; } = false;
}

// Code Template Models
public class CodeTemplate : MarketplaceItem
{
    public TemplateCategory Category { get; set; }
    public string TemplateEngine { get; set; } = "liquid"; // liquid, mustache, razor
    public List<TemplateFile> Files { get; set; } = new();
    public List<TemplateParameter> Parameters { get; set; } = new();
    public TemplateConfiguration Configuration { get; set; } = new();
    public List<string> SupportedLanguages { get; set; } = new();
    public Dictionary<string, string> Examples { get; set; } = new();
}

public class TemplateFile
{
    public string RelativePath { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentType { get; set; } = "text/plain";
    public bool IsTemplate { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TemplateParameter
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Type { get; set; } = "string"; // string, int, bool, choice, file
    public object? DefaultValue { get; set; }
    public bool Required { get; set; } = true;
    public string? Description { get; set; }
    public string? ValidationRegex { get; set; }
    public List<string>? Choices { get; set; } // For choice type
    public Dictionary<string, object> Constraints { get; set; } = new();
}

public class TemplateConfiguration
{
    public string MinMinotaurVersion { get; set; } = "1.0.0";
    public List<string> RequiredExtensions { get; set; } = new();
    public Dictionary<string, string> VariableReplacements { get; set; } = new();
    public List<string> PostInstallScripts { get; set; } = new();
    public bool RequiresProjectContext { get; set; } = false;
}

public class TemplateRenderResult
{
    public bool Success { get; set; }
    public List<GeneratedFile> GeneratedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public static TemplateRenderResult Failed(string error) =>
        new TemplateRenderResult { Success = false, Errors = new List<string> { error } };
}

public class GeneratedFile
{
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentType { get; set; } = "text/plain";
}

// Collection and Favorites
public class Collection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MarketplaceItem> Items { get; set; } = new();
    public int ItemCount { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public string UserId { get; set; } = "";
}

public class CreateCollectionRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsPublic { get; set; } = false;
    public List<string> Tags { get; set; } = new();
}