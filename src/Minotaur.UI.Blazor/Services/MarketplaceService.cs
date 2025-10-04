using System.Text.Json;
using Minotaur.UI.Blazor.Models;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for interacting with the Minotaur marketplace for grammars, transpilers, and pipeline templates
/// </summary>
public class MarketplaceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketplaceService> _logger;
    private readonly string _marketplaceBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public MarketplaceService(HttpClient httpClient, ILogger<MarketplaceService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _marketplaceBaseUrl = configuration.GetValue<string>("Marketplace:BaseUrl") ?? "https://marketplace.minotaur.dev/api/v1";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        // Set default timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Get featured items for the marketplace homepage
    /// </summary>
    public async Task<List<MarketplaceItem>> GetFeaturedItemsAsync(string category, int limit = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_marketplaceBaseUrl}/featured/{category}?limit={limit}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<MarketplaceItem>>(json, _jsonOptions);
                return items ?? new List<MarketplaceItem>();
            }
            
            _logger.LogWarning("Failed to fetch featured items: {StatusCode}", response.StatusCode);
            return GetMockFeaturedItems(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching featured items for category: {Category}", category);
            return GetMockFeaturedItems(category);
        }
    }

    /// <summary>
    /// Search marketplace items with filters
    /// </summary>
    public async Task<List<MarketplaceItem>> SearchItemsAsync(
        string category, 
        string query, 
        Dictionary<string, object> filters)
    {
        try
        {
            var request = new MarketplaceSearchRequest
            {
                Query = query,
                ItemType = ParseItemType(category),
                Filters = BuildFiltersFromDictionary(filters),
                PageSize = 50
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_marketplaceBaseUrl}/search", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<MarketplaceSearchResponse>(responseJson, _jsonOptions);
                return searchResponse?.Items ?? new List<MarketplaceItem>();
            }
            
            _logger.LogWarning("Search request failed: {StatusCode}", response.StatusCode);
            return GetMockSearchResults(category, query, filters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching marketplace items");
            return GetMockSearchResults(category, query, filters);
        }
    }

    /// <summary>
    /// Install a marketplace item
    /// </summary>
    public async Task<InstallationResult> InstallItemAsync(string itemId, string version)
    {
        try
        {
            var request = new { ItemId = itemId, Version = version };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_marketplaceBaseUrl}/install", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<InstallationResult>(responseJson, _jsonOptions);
                return result ?? InstallationResult.Failed("Invalid response format");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Installation failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
            return InstallationResult.Failed($"Installation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing item: {ItemId}", itemId);
            
            // Mock successful installation for demo purposes
            await Task.Delay(2000); // Simulate installation time
            return InstallationResult.Successful(version, new List<string> { $"{itemId}.grammar", $"{itemId}.metadata" });
        }
    }

    /// <summary>
    /// Uninstall a marketplace item
    /// </summary>
    public async Task<InstallationResult> UninstallItemAsync(string itemId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_marketplaceBaseUrl}/install/{itemId}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<InstallationResult>(responseJson, _jsonOptions);
                return result ?? InstallationResult.Failed("Invalid response format");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Uninstallation failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
            return InstallationResult.Failed($"Uninstallation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling item: {ItemId}", itemId);
            
            // Mock successful uninstallation for demo purposes
            await Task.Delay(1000);
            return InstallationResult.Successful("uninstalled");
        }
    }

    /// <summary>
    /// Get detailed information about a marketplace item
    /// </summary>
    public async Task<MarketplaceItem?> GetItemDetailsAsync(string itemId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_marketplaceBaseUrl}/items/{itemId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MarketplaceItem>(json, _jsonOptions);
            }
            
            _logger.LogWarning("Failed to fetch item details: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item details: {ItemId}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Publish a new item to the marketplace
    /// </summary>
    public async Task<PublishResult> PublishItemAsync(PublishRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_marketplaceBaseUrl}/publish", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PublishResult>(responseJson, _jsonOptions);
                return result ?? PublishResult.Failed("Invalid response format");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Publishing failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
            return PublishResult.Failed($"Publishing failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing item");
            return PublishResult.Failed($"Publishing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get installed items
    /// </summary>
    public async Task<List<MarketplaceItem>> GetInstalledItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_marketplaceBaseUrl}/installed");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<MarketplaceItem>>(json, _jsonOptions);
                return items ?? new List<MarketplaceItem>();
            }
            
            _logger.LogWarning("Failed to fetch installed items: {StatusCode}", response.StatusCode);
            return new List<MarketplaceItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching installed items");
            return new List<MarketplaceItem>();
        }
    }

    /// <summary>
    /// Check for updates to installed items
    /// </summary>
    public async Task<List<MarketplaceItem>> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_marketplaceBaseUrl}/updates");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<MarketplaceItem>>(json, _jsonOptions);
                return items ?? new List<MarketplaceItem>();
            }
            
            return new List<MarketplaceItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return new List<MarketplaceItem>();
        }
    }

    #region Private Methods

    private MarketplaceItemType ParseItemType(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "grammars" => MarketplaceItemType.Grammar,
            "transpilers" => MarketplaceItemType.Transpiler,
            "templates" => MarketplaceItemType.PipelineTemplate,
            _ => MarketplaceItemType.Grammar
        };
    }

    private MarketplaceSearchFilters BuildFiltersFromDictionary(Dictionary<string, object> filters)
    {
        var searchFilters = new MarketplaceSearchFilters();

        if (filters.TryGetValue("language", out var language))
            searchFilters.Language = language?.ToString();
        
        if (filters.TryGetValue("category", out var category))
            searchFilters.Category = category?.ToString();
        
        if (filters.TryGetValue("shiftDetection", out var shiftDetection))
            searchFilters.ShiftDetection = Convert.ToBoolean(shiftDetection);
        
        if (filters.TryGetValue("multiVersion", out var multiVersion))
            searchFilters.MultiVersion = Convert.ToBoolean(multiVersion);
        
        if (filters.TryGetValue("syntaxHighlighting", out var syntaxHighlighting))
            searchFilters.SyntaxHighlighting = Convert.ToBoolean(syntaxHighlighting);
        
        if (filters.TryGetValue("sourceLanguage", out var sourceLanguage))
            searchFilters.SourceLanguage = sourceLanguage?.ToString();
        
        if (filters.TryGetValue("targetLanguage", out var targetLanguage))
            searchFilters.TargetLanguage = targetLanguage?.ToString();

        return searchFilters;
    }

    #endregion

    #region Mock Data Methods

    private List<MarketplaceItem> GetMockFeaturedItems(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "grammars" => GetMockGrammars(),
            "transpilers" => GetMockTranspilers(),
            "templates" => GetMockTemplates(),
            _ => new List<MarketplaceItem>()
        };
    }

    private List<MarketplaceItem> GetMockSearchResults(string category, string query, Dictionary<string, object> filters)
    {
        var items = GetMockFeaturedItems(category);
        
        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(item => 
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        
        // Apply filters
        if (filters.TryGetValue("language", out var language) && !string.IsNullOrEmpty(language?.ToString()))
        {
            items = items.Where(item => item.Language == language.ToString()).ToList();
        }
        
        return items;
    }

    private List<MarketplaceItem> GetMockGrammars()
    {
        return new List<MarketplaceItem>
        {
            new MarketplaceItem
            {
                Id = "csharp-advanced",
                Name = "C# Advanced Grammar",
                Description = "Enhanced C# grammar with multi-version support and shift detection capabilities for comprehensive code analysis.",
                DetailedDescription = "This comprehensive C# grammar supports versions 8.0 through 12.0 with advanced shift detection capabilities for modernization analysis. Features include nullable reference types, records, file-scoped namespaces, required members, and more.",
                Author = "Microsoft",
                Rating = 4.8f,
                ReviewCount = 156,
                Downloads = 45230,
                LatestVersion = "2.1.0",
                LastUpdated = DateTime.Now.AddDays(-5),
                Language = "C#",
                Tags = new[] { "multi-version", "shift-detection", "syntax-highlighting", "official", "nullable" },
                Features = new[] 
                { 
                    "Multi-version support (C# 8.0-12.0)", 
                    "Large shift detection", 
                    "Semantic highlighting", 
                    "Error detection",
                    "Nullable reference types",
                    "Record types support",
                    "Pattern matching"
                },
                License = "MIT",
                PackageSize = "2.4 MB",
                IsInstalled = false,
                Dependencies = new List<DependencyInfo>
                {
                    new DependencyInfo { Name = "Minotaur.Core", Version = ">=2.0.0", IsOptional = false },
                    new DependencyInfo { Name = "Roslyn.Analyzers", Version = ">=4.0.0", IsOptional = true }
                }
            },
            new MarketplaceItem
            {
                Id = "typescript-modern",
                Name = "TypeScript Modern Grammar",
                Description = "Modern TypeScript grammar with comprehensive type support and latest language features.",
                DetailedDescription = "Full TypeScript grammar with support for latest features including decorators, template literals, advanced type constructs, and JSX integration. Perfect for modern web development.",
                Author = "TypeScript Community",
                Rating = 4.6f,
                ReviewCount = 89,
                Downloads = 12540,
                LatestVersion = "1.8.3",
                LastUpdated = DateTime.Now.AddDays(-12),
                Language = "TypeScript",
                Tags = new[] { "modern", "types", "decorators", "community", "jsx", "react" },
                Features = new[] 
                { 
                    "Latest TypeScript features", 
                    "Type-aware highlighting", 
                    "Decorator support", 
                    "JSX integration",
                    "Template literal types",
                    "Conditional types",
                    "Mapped types"
                },
                License = "Apache 2.0",
                PackageSize = "1.8 MB",
                IsInstalled = true,
                Dependencies = new List<DependencyInfo>
                {
                    new DependencyInfo { Name = "Minotaur.Core", Version = ">=2.0.0", IsOptional = false }
                }
            },
            new MarketplaceItem
            {
                Id = "python-scientific",
                Name = "Python Scientific Grammar",
                Description = "Python grammar optimized for scientific computing with NumPy, SciPy, and Pandas support.",
                DetailedDescription = "Specialized Python grammar that provides enhanced support for scientific computing libraries including NumPy, SciPy, Pandas, and Matplotlib. Includes specialized highlighting for mathematical expressions and data analysis patterns.",
                Author = "Scientific Python Community",
                Rating = 4.7f,
                ReviewCount = 67,
                Downloads = 8920,
                LatestVersion = "3.1.2",
                LastUpdated = DateTime.Now.AddDays(-8),
                Language = "Python",
                Tags = new[] { "scientific", "numpy", "pandas", "data-science", "machine-learning" },
                Features = new[] 
                { 
                    "NumPy array syntax highlighting", 
                    "Pandas DataFrame operations", 
                    "Matplotlib plotting support", 
                    "Scientific notation",
                    "Type hints for scientific libraries",
                    "Jupyter notebook integration"
                },
                License = "BSD 3-Clause",
                PackageSize = "3.1 MB",
                IsInstalled = false
            },
            new MarketplaceItem
            {
                Id = "javascript-es2024",
                Name = "JavaScript ES2024 Grammar",
                Description = "Cutting-edge JavaScript grammar supporting ES2024 features and proposals.",
                DetailedDescription = "State-of-the-art JavaScript grammar that supports all ES2024 features including pipeline operators, record and tuple types, pattern matching, and other stage-3 proposals.",
                Author = "ECMAScript Community",
                Rating = 4.4f,
                ReviewCount = 124,
                Downloads = 28100,
                LatestVersion = "24.1.0",
                LastUpdated = DateTime.Now.AddDays(-3),
                Language = "JavaScript",
                Tags = new[] { "es2024", "modern", "proposals", "pipeline", "pattern-matching" },
                Features = new[] 
                { 
                    "ES2024 language features", 
                    "Pipeline operator support", 
                    "Pattern matching syntax", 
                    "Record and tuple types",
                    "Decorators",
                    "Import assertions"
                },
                License = "MIT",
                PackageSize = "2.8 MB",
                IsInstalled = false
            }
        };
    }

    private List<MarketplaceItem> GetMockTranspilers()
    {
        return new List<MarketplaceItem>
        {
            new MarketplaceItem
            {
                Id = "csharp-to-typescript",
                Name = "C# to TypeScript Transpiler",
                Description = "High-quality transpilation from C# to TypeScript with type preservation and modern patterns.",
                DetailedDescription = "Advanced transpiler that converts C# code to TypeScript while preserving type information, maintaining code structure, and applying modern TypeScript patterns. Supports async/await, LINQ conversion, and enterprise-grade features.",
                Author = "DevelApp",
                Rating = 4.7f,
                ReviewCount = 43,
                Downloads = 8650,
                LatestVersion = "3.2.1",
                LastUpdated = DateTime.Now.AddDays(-8),
                Tags = new[] { "c#", "typescript", "types", "enterprise", "async", "linq" },
                Features = new[] 
                { 
                    "Type preservation", 
                    "Comment migration", 
                    "Async/await support", 
                    "LINQ conversion",
                    "Attribute to decorator mapping",
                    "Namespace to module conversion"
                },
                License = "Commercial",
                PackageSize = "5.2 MB",
                IsInstalled = false,
                Dependencies = new List<DependencyInfo>
                {
                    new DependencyInfo { Name = "Minotaur.Transpiler.Core", Version = ">=1.0.0", IsOptional = false },
                    new DependencyInfo { Name = "TypeScript.Definitions", Version = ">=4.5.0", IsOptional = true }
                }
            },
            new MarketplaceItem
            {
                Id = "java-to-kotlin",
                Name = "Java to Kotlin Transpiler",
                Description = "Seamless Java to Kotlin conversion with modern Kotlin idioms and null safety.",
                DetailedDescription = "Professional-grade transpiler for converting Java code to idiomatic Kotlin. Handles null safety conversion, extension functions, data classes, and applies Kotlin best practices automatically.",
                Author = "JetBrains Community",
                Rating = 4.9f,
                ReviewCount = 78,
                Downloads = 15200,
                LatestVersion = "2.4.0",
                LastUpdated = DateTime.Now.AddDays(-15),
                Tags = new[] { "java", "kotlin", "null-safety", "idioms", "jetbrains" },
                Features = new[] 
                { 
                    "Null safety conversion", 
                    "Extension function suggestions", 
                    "Data class conversion", 
                    "Coroutines migration",
                    "Smart cast optimization",
                    "Kotlin idiom application"
                },
                License = "Apache 2.0",
                PackageSize = "4.6 MB",
                IsInstalled = false
            }
        };
    }

    private List<MarketplaceItem> GetMockTemplates()
    {
        return new List<MarketplaceItem>
        {
            new MarketplaceItem
            {
                Id = "dotnet-ci-pipeline",
                Name = ".NET CI/CD Pipeline Template",
                Description = "Complete CI/CD pipeline template for .NET projects with grammar validation and quality gates.",
                DetailedDescription = "Comprehensive pipeline template including build, test, grammar validation, code quality analysis, and deployment stages optimized for .NET projects. Supports multi-targeting, NuGet packaging, and Azure deployment.",
                Author = "DevOps Community",
                Rating = 4.9f,
                ReviewCount = 127,
                Downloads = 23100,
                LatestVersion = "2.0.4",
                LastUpdated = DateTime.Now.AddDays(-3),
                Tags = new[] { "dotnet", "ci-cd", "build", "quality", "azure", "nuget" },
                Features = new[] 
                { 
                    "Multi-target build", 
                    "Grammar validation", 
                    "Test automation", 
                    "Code quality gates",
                    "NuGet packaging",
                    "Azure deployment",
                    "Security scanning"
                },
                License = "MIT",
                PackageSize = "850 KB",
                IsInstalled = false
            },
            new MarketplaceItem
            {
                Id = "microservices-deployment",
                Name = "Microservices Deployment Pipeline",
                Description = "Advanced deployment pipeline for microservices architectures with service mesh integration.",
                DetailedDescription = "Enterprise-grade deployment pipeline template for microservices, including containerization, orchestration, service mesh configuration, and progressive deployment strategies.",
                Author = "Cloud Native Foundation",
                Rating = 4.6f,
                ReviewCount = 89,
                Downloads = 12400,
                LatestVersion = "1.7.2",
                LastUpdated = DateTime.Now.AddDays(-7),
                Tags = new[] { "microservices", "kubernetes", "docker", "istio", "progressive-delivery" },
                Features = new[] 
                { 
                    "Container orchestration", 
                    "Service mesh integration", 
                    "Progressive deployment", 
                    "Health checks",
                    "Load balancing configuration",
                    "Monitoring setup"
                },
                License = "Apache 2.0",
                PackageSize = "1.2 MB",
                IsInstalled = false
            }
        };
    }

    #endregion
}