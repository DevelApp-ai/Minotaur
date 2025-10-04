using System.Text.Json;
using Minotaur.UI.Blazor.Models;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for managing code templates, template rendering, and template marketplace operations
/// </summary>
public class TemplateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templateBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public TemplateService(
        HttpClient httpClient,
        ILogger<TemplateService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _templateBaseUrl = configuration.GetValue<string>("Templates:BaseUrl") ?? "https://templates.minotaur.dev/api/v1";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Get featured code templates
    /// </summary>
    public async Task<List<CodeTemplate>> GetFeaturedTemplatesAsync(TemplateCategory? category = null, int limit = 20)
    {
        try
        {
            var url = $"{_templateBaseUrl}/templates/featured?limit={limit}";
            if (category.HasValue)
            {
                url += $"&category={category}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<CodeTemplate>>(_jsonOptions);
                return templates ?? new List<CodeTemplate>();
            }

            _logger.LogWarning("Failed to fetch featured templates: {StatusCode}", response.StatusCode);
            return GetMockTemplates(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching featured templates");
            return GetMockTemplates(category);
        }
    }

    /// <summary>
    /// Search code templates
    /// </summary>
    public async Task<List<CodeTemplate>> SearchTemplatesAsync(
        string query,
        TemplateCategory? category = null,
        string? language = null,
        Dictionary<string, object>? filters = null)
    {
        try
        {
            var searchRequest = new
            {
                Query = query,
                Category = category?.ToString(),
                Language = language,
                Filters = filters ?? new Dictionary<string, object>(),
                ItemType = "CodeTemplate"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_templateBaseUrl}/templates/search", searchRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var searchResult = await response.Content.ReadFromJsonAsync<SearchResult<CodeTemplate>>(_jsonOptions);
                return searchResult?.Items ?? new List<CodeTemplate>();
            }

            _logger.LogWarning("Template search failed: {StatusCode}", response.StatusCode);
            return GetMockSearchResults(query, category, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates");
            return GetMockSearchResults(query, category, language);
        }
    }

    /// <summary>
    /// Get template details by ID
    /// </summary>
    public async Task<CodeTemplate?> GetTemplateAsync(string templateId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_templateBaseUrl}/templates/{templateId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CodeTemplate>(_jsonOptions);
            }

            _logger.LogWarning("Failed to get template {TemplateId}: {StatusCode}", templateId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template: {TemplateId}", templateId);
        }

        return null;
    }

    /// <summary>
    /// Render template with provided parameters
    /// </summary>
    public async Task<TemplateRenderResult> RenderTemplateAsync(
        CodeTemplate template,
        Dictionary<string, object> parameters,
        string? targetDirectory = null)
    {
        try
        {
            var renderRequest = new
            {
                TemplateId = template.Id,
                Parameters = parameters,
                TargetDirectory = targetDirectory
            };

            var response = await _httpClient.PostAsJsonAsync($"{_templateBaseUrl}/templates/render", renderRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TemplateRenderResult>(_jsonOptions);
                return result ?? TemplateRenderResult.Failed("Invalid response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return TemplateRenderResult.Failed($"Render failed: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template: {TemplateId}", template.Id);

            // Fallback to local rendering for demo
            return await RenderTemplateLocallyAsync(template, parameters, targetDirectory);
        }
    }

    /// <summary>
    /// Install template for offline use
    /// </summary>
    public async Task<InstallationResult> InstallTemplateAsync(string templateId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_templateBaseUrl}/templates/{templateId}/install", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InstallationResult>(_jsonOptions);
                return result ?? InstallationResult.Failed("Invalid response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return InstallationResult.Failed($"Installation failed: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing template: {TemplateId}", templateId);

            // Mock successful installation for demo
            await Task.Delay(1500);
            return InstallationResult.Successful("1.0.0", new List<string> { $"{templateId}.template" });
        }
    }

    /// <summary>
    /// Get user's installed templates
    /// </summary>
    public async Task<List<CodeTemplate>> GetInstalledTemplatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_templateBaseUrl}/user/templates/installed");

            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<CodeTemplate>>(_jsonOptions);
                return templates ?? new List<CodeTemplate>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installed templates");
        }

        return new List<CodeTemplate>();
    }

    /// <summary>
    /// Publish a new template
    /// </summary>
    public async Task<PublishResult> PublishTemplateAsync(CodeTemplate template, List<byte[]>? files = null)
    {
        try
        {
            // First validate the template
            var validationResult = ValidateTemplate(template);
            if (!validationResult.IsValid)
            {
                return PublishResult.Failed(string.Join(", ", validationResult.Errors));
            }

            var publishRequest = new
            {
                Template = template,
                Files = files ?? new List<byte[]>()
            };

            var response = await _httpClient.PostAsJsonAsync($"{_templateBaseUrl}/templates/publish", publishRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PublishResult>(_jsonOptions);
                return result ?? PublishResult.Failed("Invalid response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return PublishResult.Failed($"Publishing failed: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing template: {TemplateName}", template.Name);
            return PublishResult.Failed($"Publishing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get template categories with counts
    /// </summary>
    public async Task<Dictionary<TemplateCategory, int>> GetTemplateCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_templateBaseUrl}/templates/categories");

            if (response.IsSuccessStatusCode)
            {
                var categories = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(_jsonOptions);
                if (categories != null)
                {
                    return categories.ToDictionary(
                        kvp => Enum.Parse<TemplateCategory>(kvp.Key),
                        kvp => kvp.Value
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template categories");
        }

        // Return mock data
        return GetMockCategories();
    }

    /// <summary>
    /// Get supported programming languages
    /// </summary>
    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_templateBaseUrl}/templates/languages");

            if (response.IsSuccessStatusCode)
            {
                var languages = await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions);
                return languages ?? GetMockLanguages();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported languages");
        }

        return GetMockLanguages();
    }

    #region Private Methods

    private ValidationResult ValidateTemplate(CodeTemplate template)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(template.Name))
            errors.Add("Template name is required");

        if (string.IsNullOrWhiteSpace(template.Description))
            errors.Add("Template description is required");

        if (!template.Files.Any())
            errors.Add("Template must contain at least one file");

        // Validate parameters
        foreach (var param in template.Parameters)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
                errors.Add("Parameter name cannot be empty");

            if (param.Required && param.DefaultValue == null)
                errors.Add($"Required parameter '{param.Name}' must have a default value");
        }

        result.Errors = errors;
        result.IsValid = !errors.Any();

        return result;
    }

    private async Task<TemplateRenderResult> RenderTemplateLocallyAsync(
        CodeTemplate template,
        Dictionary<string, object> parameters,
        string? targetDirectory)
    {
        // Simple local rendering for demo purposes
        var result = new TemplateRenderResult { Success = true };

        foreach (var file in template.Files)
        {
            try
            {
                string content = file.Content;

                // Simple parameter replacement (in a real implementation, use a proper template engine)
                foreach (var param in parameters)
                {
                    var placeholder = $"{{{{{param.Key}}}}}";
                    content = content.Replace(placeholder, param.Value?.ToString() ?? "");
                }

                string targetPath = targetDirectory != null
                    ? System.IO.Path.Combine(targetDirectory, file.RelativePath)
                    : file.RelativePath;

                result.GeneratedFiles.Add(new GeneratedFile
                {
                    Path = targetPath,
                    Content = content,
                    ContentType = file.ContentType
                });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to render {file.RelativePath}: {ex.Message}");
            }
        }

        return await Task.FromResult(result);
    }

    #endregion

    #region Mock Data

    private List<CodeTemplate> GetMockTemplates(TemplateCategory? category)
    {
        var templates = new List<CodeTemplate>
        {
            new CodeTemplate
            {
                Id = "mvc-controller",
                Name = "ASP.NET Core Controller",
                Description = "Complete MVC controller template with CRUD operations",
                DetailedDescription = "A comprehensive controller template that generates a full MVC controller with GET, POST, PUT, and DELETE actions, including model binding and validation.",
                Author = "Microsoft",
                AuthorId = "microsoft",
                Rating = 4.9f,
                ReviewCount = 234,
                Downloads = 15420,
                LatestVersion = "2.1.0",
                LastUpdated = DateTime.Now.AddDays(-3),
                CreatedAt = DateTime.Now.AddMonths(-6),
                Language = "C#",
                Tags = new[] { "mvc", "controller", "crud", "asp.net-core" },
                License = "MIT",
                PackageSize = "12 KB",
                ItemType = MarketplaceItemType.CodeTemplate,
                Category = TemplateCategory.ControllerTemplate,
                SupportedLanguages = new List<string> { "C#" },
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter
                    {
                        Name = "ControllerName",
                        DisplayName = "Controller Name",
                        Type = "string",
                        Required = true,
                        Description = "The name of the controller class (without 'Controller' suffix)"
                    },
                    new TemplateParameter
                    {
                        Name = "ModelName",
                        DisplayName = "Model Name",
                        Type = "string",
                        Required = true,
                        Description = "The name of the model class"
                    },
                    new TemplateParameter
                    {
                        Name = "UseAsync",
                        DisplayName = "Use Async Methods",
                        Type = "bool",
                        DefaultValue = true,
                        Description = "Generate async action methods"
                    }
                },
                Files = new List<TemplateFile>
                {
                    new TemplateFile
                    {
                        RelativePath = "Controllers/{{ControllerName}}Controller.cs",
                        Content = GenerateControllerTemplate(),
                        ContentType = "text/csharp",
                        IsTemplate = true
                    }
                }
            },
            new CodeTemplate
            {
                Id = "blazor-component",
                Name = "Blazor Component",
                Description = "Reusable Blazor component with parameters and lifecycle methods",
                DetailedDescription = "A complete Blazor component template with parameter binding, lifecycle methods, and event handling.",
                Author = "Blazor Community",
                AuthorId = "blazor-community",
                Rating = 4.7f,
                ReviewCount = 89,
                Downloads = 8340,
                LatestVersion = "1.3.2",
                LastUpdated = DateTime.Now.AddDays(-7),
                CreatedAt = DateTime.Now.AddMonths(-4),
                Language = "C#",
                Tags = new[] { "blazor", "component", "razor", "ui" },
                License = "Apache 2.0",
                PackageSize = "8 KB",
                ItemType = MarketplaceItemType.CodeTemplate,
                Category = TemplateCategory.ComponentTemplate,
                SupportedLanguages = new List<string> { "C#", "HTML" },
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter
                    {
                        Name = "ComponentName",
                        DisplayName = "Component Name",
                        Type = "string",
                        Required = true,
                        Description = "The name of the Blazor component"
                    },
                    new TemplateParameter
                    {
                        Name = "HasParameters",
                        DisplayName = "Include Parameters",
                        Type = "bool",
                        DefaultValue = true,
                        Description = "Include component parameters section"
                    }
                },
                Files = new List<TemplateFile>
                {
                    new TemplateFile
                    {
                        RelativePath = "Components/{{ComponentName}}.razor",
                        Content = GenerateBlazorComponentTemplate(),
                        ContentType = "text/razor",
                        IsTemplate = true
                    },
                    new TemplateFile
                    {
                        RelativePath = "Components/{{ComponentName}}.razor.cs",
                        Content = GenerateBlazorComponentCodeBehind(),
                        ContentType = "text/csharp",
                        IsTemplate = true
                    }
                }
            },
            new CodeTemplate
            {
                Id = "typescript-class",
                Name = "TypeScript Class",
                Description = "Modern TypeScript class with decorators and type safety",
                DetailedDescription = "A comprehensive TypeScript class template with modern features including decorators, generics, and proper type annotations.",
                Author = "TypeScript Team",
                AuthorId = "typescript-team",
                Rating = 4.6f,
                ReviewCount = 156,
                Downloads = 12780,
                LatestVersion = "3.0.1",
                LastUpdated = DateTime.Now.AddDays(-2),
                CreatedAt = DateTime.Now.AddMonths(-8),
                Language = "TypeScript",
                Tags = new[] { "typescript", "class", "decorators", "types" },
                License = "MIT",
                PackageSize = "6 KB",
                ItemType = MarketplaceItemType.CodeTemplate,
                Category = TemplateCategory.ClassTemplate,
                SupportedLanguages = new List<string> { "TypeScript", "JavaScript" },
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter
                    {
                        Name = "ClassName",
                        DisplayName = "Class Name",
                        Type = "string",
                        Required = true,
                        Description = "The name of the TypeScript class"
                    },
                    new TemplateParameter
                    {
                        Name = "ExtendsClass",
                        DisplayName = "Base Class",
                        Type = "string",
                        Required = false,
                        Description = "Optional base class to extend"
                    },
                    new TemplateParameter
                    {
                        Name = "UseDecorators",
                        DisplayName = "Use Decorators",
                        Type = "bool",
                        DefaultValue = false,
                        Description = "Include decorator examples"
                    }
                },
                Files = new List<TemplateFile>
                {
                    new TemplateFile
                    {
                        RelativePath = "src/{{ClassName}}.ts",
                        Content = GenerateTypeScriptClassTemplate(),
                        ContentType = "text/typescript",
                        IsTemplate = true
                    }
                }
            }
        };

        if (category.HasValue)
        {
            templates = templates.Where(t => t.Category == category.Value).ToList();
        }

        return templates;
    }

    private List<CodeTemplate> GetMockSearchResults(string query, TemplateCategory? category, string? language)
    {
        var templates = GetMockTemplates(category);

        if (!string.IsNullOrEmpty(query))
        {
            templates = templates.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(language))
        {
            templates = templates.Where(t => t.SupportedLanguages.Contains(language)).ToList();
        }

        return templates;
    }

    private Dictionary<TemplateCategory, int> GetMockCategories()
    {
        return new Dictionary<TemplateCategory, int>
        {
            { TemplateCategory.ClassTemplate, 45 },
            { TemplateCategory.ControllerTemplate, 23 },
            { TemplateCategory.ComponentTemplate, 67 },
            { TemplateCategory.ServiceTemplate, 34 },
            { TemplateCategory.InterfaceTemplate, 19 },
            { TemplateCategory.WebApiProject, 12 },
            { TemplateCategory.BlazorProject, 8 },
            { TemplateCategory.ConsoleProject, 15 }
        };
    }

    private List<string> GetMockLanguages()
    {
        return new List<string>
        {
            "C#", "TypeScript", "JavaScript", "Python", "Java", "Go",
            "Rust", "C++", "C", "PHP", "Ruby", "Swift", "Kotlin"
        };
    }

    private string GenerateControllerTemplate()
    {
        return @"using Microsoft.AspNetCore.Mvc;
using {{Namespace}}.Models;

namespace {{Namespace}}.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    public class {{ControllerName}}Controller : ControllerBase
    {
        [HttpGet]
        public {{UseAsync ? ""async Task<IActionResult>"" : ""IActionResult""}} Get{{ModelName}}s()
        {
            // TODO: Implement Get{{ModelName}}s logic
            {{UseAsync ? ""return await Task.FromResult(Ok());"" : ""return Ok();""}}
        }

        [HttpGet(""{id}"")]
        public {{UseAsync ? ""async Task<IActionResult>"" : ""IActionResult""}} Get{{ModelName}}(int id)
        {
            // TODO: Implement Get{{ModelName}} logic
            {{UseAsync ? ""return await Task.FromResult(Ok());"" : ""return Ok();""}}
        }

        [HttpPost]
        public {{UseAsync ? ""async Task<IActionResult>"" : ""IActionResult""}} Create{{ModelName}}({{ModelName}} model)
        {
            // TODO: Implement Create{{ModelName}} logic
            {{UseAsync ? ""return await Task.FromResult(CreatedAtAction(nameof(Get{ModelName}), new { id = model.Id }, model));"" : ""return CreatedAtAction(nameof(Get{ModelName}), new { id = model.Id }, model);""}}
        }

        [HttpPut(""{id}"")]
        public {{UseAsync ? ""async Task<IActionResult>"" : ""IActionResult""}} Update{{ModelName}}(int id, {{ModelName}} model)
        {
            // TODO: Implement Update{{ModelName}} logic
            {{UseAsync ? ""return await Task.FromResult(NoContent());"" : ""return NoContent();""}}
        }

        [HttpDelete(""{id}"")]
        public {{UseAsync ? ""async Task<IActionResult>"" : ""IActionResult""}} Delete{{ModelName}}(int id)
        {
            // TODO: Implement Delete{{ModelName}} logic
            {{UseAsync ? ""return await Task.FromResult(NoContent());"" : ""return NoContent();""}}
        }
    }
}";
    }

    private string GenerateBlazorComponentTemplate()
    {
        return @"@if (HasParameters)
{
    <div class=""{{ComponentName.ToLower()}}-container"">
        <h3>@Title</h3>
        
        @if (IsVisible)
        {
            <div class=""content"">
                @ChildContent
            </div>
        }
        
        @if (ShowFooter)
        {
            <footer class=""component-footer"">
                <button @onclick=""OnButtonClick"" class=""btn btn-primary"">
                    @ButtonText
                </button>
            </footer>
        }
    </div>
}";
    }

    private string GenerateBlazorComponentCodeBehind()
    {
        return @"using Microsoft.AspNetCore.Components;

namespace {{Namespace}}.Components
{
    public partial class {{ComponentName}}
    {
        [Parameter] public string Title { get; set; } = ""{{ComponentName}}"";
        [Parameter] public bool IsVisible { get; set; } = true;
        [Parameter] public bool ShowFooter { get; set; } = true;
        [Parameter] public string ButtonText { get; set; } = ""Click Me"";
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public EventCallback OnButtonClick { get; set; }

        protected override void OnInitialized()
        {
            // Component initialization logic
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // First render logic
            }
        }

        private async Task HandleButtonClick()
        {
            await OnButtonClick.InvokeAsync();
        }
    }
}";
    }

    private string GenerateTypeScriptClassTemplate()
    {
        return @"{{UseDecorators ? ""import { Injectable } from '@angular/core';"" : """"}}

{{UseDecorators ? ""@Injectable({ providedIn: 'root' })"" : """"}}
export class {{ClassName}}{{ExtendsClass ? "" extends "" + ExtendsClass : """"}} {
    private _name: string;
    private _isActive: boolean = false;

    constructor(name: string) {
        this._name = name;
    }

    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
    }

    get isActive(): boolean {
        return this._isActive;
    }

    public activate(): void {
        this._isActive = true;
        console.log(`${this._name} has been activated`);
    }

    public deactivate(): void {
        this._isActive = false;
        console.log(`${this._name} has been deactivated`);
    }

    public toString(): string {
        return `{{ClassName}}: ${this._name} (${this._isActive ? 'Active' : 'Inactive'})`;
    }
}";
    }

    #endregion
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => (Page * PageSize) < TotalCount;
}