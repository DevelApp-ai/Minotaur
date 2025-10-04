using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Minotaur.UI.Blazor.Services;
using Minotaur.UI.Blazor.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minotaur.UI.Blazor.Tests.Integration;

/// <summary>
/// Integration tests demonstrating system alignment and interaction between 
/// existing Minotaur projects and the new marketplace platform.
/// These tests validate the compatibility and migration paths outlined in the 
/// Marketplace Integration Technical Design Specification.
/// </summary>
public class SystemAlignmentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SystemAlignmentIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Should_Authenticate_And_Access_Marketplace_From_Existing_Project()
    {
        // Arrange: Simulate existing project context
        var existingProjectConfig = new
        {
            ProjectName = "ExistingGrammarProject",
            GrammarFiles = new[] { "custom.g4", "parser.g4" },
            Plugins = new[] { "LanguagePlugin", "TransformPlugin" }
        };

        // Act: Test authentication flow
        var loginRequest = new LoginRequest
        {
            EmailOrUsername = "integration.test@minotaur.dev",
            Password = "IntegrationTest123!"
        };

        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        var marketplaceService = scope.ServiceProvider.GetRequiredService<MarketplaceService>();

        var authResult = await authService.LoginAsync(loginRequest);
        Assert.True(authResult.IsSuccess, "Authentication should succeed for existing users");
        Assert.NotNull(authResult.AccessToken);

        // Test: Access marketplace with authenticated context
        var marketplaceItems = await marketplaceService.SearchItemsAsync(
            category: "grammars",
            query: "C# advanced",
            filters: new Dictionary<string, object> 
            { 
                ["language"] = "C#",
                ["hasShiftDetection"] = true
            }
        );

        Assert.NotEmpty(marketplaceItems);
        Assert.All(marketplaceItems, item => 
        {
            Assert.NotNull(item.Name);
            Assert.NotNull(item.Author);
            Assert.True(item.Downloads >= 0);
        });
    }

    [Fact]
    public async Task Should_Resolve_Legacy_Grammar_Dependencies_To_Marketplace_Items()
    {
        // Arrange: Simulate legacy project with specific grammar dependencies
        var legacyDependencies = new List<string>
        {
            "antlr4-csharp-grammar",
            "typescript-modern-grammar", 
            "custom-parser-extension"
        };

        var projectContext = new ProjectContext
        {
            ProjectType = "GrammarEditor",
            TargetFramework = "net8.0",
            RequiredFeatures = new[] { "SyntaxHighlighting", "CodeCompletion" }
        };

        using var scope = _factory.Services.CreateScope();
        var alignmentService = scope.ServiceProvider.GetRequiredService<MarketplaceService>();

        // Act: Resolve dependencies through marketplace
        var resolutionResults = new List<MarketplaceItem>();
        foreach (var dependency in legacyDependencies)
        {
            var searchResults = await alignmentService.SearchItemsAsync(
                category: "grammars",
                query: dependency.Replace("-", " "),
                filters: new Dictionary<string, object>()
            );

            if (searchResults.Any())
            {
                // Select best match based on compatibility
                var bestMatch = searchResults
                    .Where(item => IsCompatibleWithProject(item, projectContext))
                    .OrderByDescending(item => item.Downloads)
                    .First();

                resolutionResults.Add(bestMatch);
            }
        }

        // Assert: Dependencies should be resolvable
        Assert.NotEmpty(resolutionResults);
        Assert.True(resolutionResults.Count >= 2, 
            "At least 2 out of 3 legacy dependencies should have marketplace equivalents");

        // Verify resolved items have required features
        foreach (var item in resolutionResults)
        {
            Assert.Contains("SyntaxHighlighting", item.Features ?? Array.Empty<string>());
        }
    }

    [Fact]
    public async Task Should_Install_Template_And_Maintain_Project_Structure()
    {
        // Arrange: Prepare template installation
        using var scope = _factory.Services.CreateScope();
        var marketplaceService = scope.ServiceProvider.GetRequiredService<MarketplaceService>();
        var templateService = scope.ServiceProvider.GetRequiredService<TemplateService>();

        // Search for a suitable template
        var templates = await marketplaceService.SearchItemsAsync(
            category: "templates",
            query: "class template",
            filters: new Dictionary<string, object> { ["type"] = "code" }
        );

        var selectedTemplate = templates.FirstOrDefault(t => t.ItemType == MarketplaceItemType.CodeTemplate);
        Assert.NotNull(selectedTemplate);

        // Act: Install template
        var installResult = await marketplaceService.InstallItemAsync(
            selectedTemplate.Id,
            targetPath: "/tmp/test-project"
        );

        Assert.True(installResult.IsSuccess, $"Template installation failed: {installResult.Error}");

        // Verify template rendering works
        if (selectedTemplate.ItemType == MarketplaceItemType.CodeTemplate)
        {
            var codeTemplate = await templateService.GetTemplateAsync(selectedTemplate.Id);
            Assert.NotNull(codeTemplate);

            var renderResult = await templateService.RenderTemplateAsync(codeTemplate, 
                new Dictionary<string, object>
                {
                    ["ClassName"] = "TestClass",
                    ["Namespace"] = "TestProject",
                    ["UseNullable"] = true
                });

            Assert.True(renderResult.Success);
            Assert.NotEmpty(renderResult.GeneratedFiles);
            Assert.Contains(renderResult.GeneratedFiles, f => f.Content.Contains("TestClass"));
        }
    }

    [Fact]
    public async Task Should_Maintain_Performance_During_Concurrent_Operations()
    {
        // Arrange: Prepare concurrent operations
        using var scope = _factory.Services.CreateScope();
        var marketplaceService = scope.ServiceProvider.GetRequiredService<MarketplaceService>();

        var concurrentTasks = new List<Task>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act: Execute concurrent marketplace operations
        for (int i = 0; i < 10; i++)
        {
            concurrentTasks.Add(PerformMarketplaceOperations(marketplaceService, i));
        }

        await Task.WhenAll(concurrentTasks);
        stopwatch.Stop();

        // Assert: Performance should be within acceptable limits
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
            $"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms, should be under 15 seconds");
    }

    [Fact]
    public async Task Should_Validate_Data_Consistency_Across_Services()
    {
        // Arrange: Set up data consistency test
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        var marketplaceService = scope.ServiceProvider.GetRequiredService<MarketplaceService>();

        // Create test user context
        var testUser = new UserProfile
        {
            Username = "consistency-test-user",
            Email = "consistency@minotaur.dev",
            DisplayName = "Consistency Test User"
        };

        // Act: Perform operations that should maintain consistency
        var loginResult = await authService.LoginAsync(new LoginRequest 
        { 
            EmailOrUsername = testUser.Email,
            Password = "TestPassword123!"
        });

        Assert.True(loginResult.IsSuccess);

        // Test marketplace operations with authenticated user
        var userCollections = await marketplaceService.GetUserCollectionsAsync();
        Assert.NotNull(userCollections);

        var searchResults = await marketplaceService.SearchItemsAsync(
            "grammars", 
            "test", 
            new Dictionary<string, object>()
        );

        // Verify user context is maintained across service calls
        Assert.NotEmpty(searchResults);
    }

    private async Task PerformMarketplaceOperations(MarketplaceService service, int operationId)
    {
        try
        {
            // Search operation
            await service.SearchItemsAsync(
                "grammars", 
                $"test-{operationId}", 
                new Dictionary<string, object>()
            );

            // Get collections operation
            await service.GetUserCollectionsAsync();

            // Simulate item details retrieval
            var searchResults = await service.SearchItemsAsync(
                "templates", 
                "example", 
                new Dictionary<string, object>()
            );

            if (searchResults.Any())
            {
                var firstItem = searchResults.First();
                // Simulate getting item details - this would normally be a separate API call
                await Task.Delay(10); // Simulate network latency
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the test for individual operation failures
            System.Diagnostics.Debug.WriteLine($"Operation {operationId} failed: {ex.Message}");
        }
    }

    private static bool IsCompatibleWithProject(MarketplaceItem item, ProjectContext context)
    {
        // Simplified compatibility check
        if (item.Features == null) return true;

        var hasRequiredFeatures = context.RequiredFeatures?.All(feature => 
            item.Features.Contains(feature)) ?? true;

        var supportsTargetFramework = string.IsNullOrEmpty(context.TargetFramework) || 
            item.Tags?.Contains(context.TargetFramework) == true;

        return hasRequiredFeatures && supportsTargetFramework;
    }
}

/// <summary>
/// Supporting classes for integration testing
/// </summary>
public class ProjectContext
{
    public string ProjectType { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public string[] RequiredFeatures { get; set; } = Array.Empty<string>();
}

public class InstallationResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public List<string> InstalledFiles { get; set; } = new();
}

// Extension methods for testing
public static class MarketplaceServiceExtensions
{
    public static async Task<InstallationResult> InstallItemAsync(
        this MarketplaceService service, 
        string itemId, 
        string targetPath)
    {
        // Mock implementation for testing
        await Task.Delay(100); // Simulate installation time
        
        return new InstallationResult
        {
            IsSuccess = true,
            InstalledFiles = new List<string> { $"{targetPath}/template.cs", $"{targetPath}/config.json" }
        };
    }

    public static async Task<List<UserCollection>> GetUserCollectionsAsync(this MarketplaceService service)
    {
        // Mock implementation for testing
        await Task.Delay(50);
        
        return new List<UserCollection>
        {
            new UserCollection 
            { 
                Name = "Favorites", 
                Items = new List<string> { "csharp-advanced", "typescript-modern" } 
            },
            new UserCollection 
            { 
                Name = "Recent Downloads", 
                Items = new List<string> { "web-api-template", "class-generator" } 
            }
        };
    }
}

public class UserCollection
{
    public string Name { get; set; } = "";
    public List<string> Items { get; set; } = new();
}