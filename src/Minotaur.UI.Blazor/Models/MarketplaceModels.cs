namespace Minotaur.UI.Blazor.Models;

public class MarketplaceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DetailedDescription { get; set; } = "";
    public string Author { get; set; } = "";
    public float Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Downloads { get; set; }
    public string LatestVersion { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    public string? Language { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public string License { get; set; } = "";
    public string PackageSize { get; set; } = "";
    public bool IsInstalled { get; set; }
    public List<DependencyInfo>? Dependencies { get; set; }
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
    PipelineTemplate
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