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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minotaur.Projects.Grammar;

/// <summary>
/// Represents grammar configuration settings for a project or specific files.
/// This configuration can be defined in a separate file and applied to control grammar selection.
/// </summary>
public class GrammarConfiguration
{
    /// <summary>
    /// Gets or sets the default grammar to use when no specific mapping is found.
    /// </summary>
    [JsonPropertyName("defaultGrammar")]
    public string? DefaultGrammar { get; set; }

    /// <summary>
    /// Gets or sets the default grammar version to use.
    /// </summary>
    [JsonPropertyName("defaultVersion")]
    public string? DefaultVersion { get; set; }

    /// <summary>
    /// Gets or sets the file extension to grammar mappings.
    /// </summary>
    [JsonPropertyName("extensionMappings")]
    public Dictionary<string, GrammarMapping> ExtensionMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the file path pattern to grammar mappings.
    /// Supports glob patterns like "**/*.test.js" or "src/components/**/*.tsx".
    /// </summary>
    [JsonPropertyName("pathMappings")]
    public Dictionary<string, GrammarMapping> PathMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets content-based detection rules.
    /// </summary>
    [JsonPropertyName("contentRules")]
    public List<ContentDetectionRule> ContentRules { get; set; } = new();

    /// <summary>
    /// Gets or sets project-specific overrides based on project type.
    /// </summary>
    [JsonPropertyName("projectTypeOverrides")]
    public Dictionary<string, GrammarMapping> ProjectTypeOverrides { get; set; } = new();

    /// <summary>
    /// Gets or sets the grammar search paths for resolving grammar files.
    /// </summary>
    [JsonPropertyName("grammarSearchPaths")]
    public List<string> GrammarSearchPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata for the configuration.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the grammar mapping for a specific file extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without dot).</param>
    /// <returns>The grammar mapping if found, null otherwise.</returns>
    public GrammarMapping? GetMappingForExtension(string extension)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;

        return ExtensionMappings.TryGetValue(extension, out var mapping) ? mapping : null;
    }

    /// <summary>
    /// Gets the grammar mapping for a specific file path using glob pattern matching.
    /// </summary>
    /// <param name="filePath">The file path to match against.</param>
    /// <returns>The best matching grammar mapping, or null if no match is found.</returns>
    public GrammarMapping? GetMappingForPath(string filePath)
    {
        // Convert to forward slashes for consistent pattern matching
        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var (pattern, mapping) in PathMappings)
        {
            if (IsPathMatch(normalizedPath, pattern))
            {
                return mapping;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the project type override for a specific project type.
    /// </summary>
    /// <param name="projectType">The project type.</param>
    /// <returns>The grammar mapping override if found, null otherwise.</returns>
    public GrammarMapping? GetProjectTypeOverride(ProjectType projectType)
    {
        var projectTypeName = projectType.ToString();
        return ProjectTypeOverrides.TryGetValue(projectTypeName, out var mapping) ? mapping : null;
    }

    /// <summary>
    /// Loads grammar configuration from a JSON file.
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded configuration.</returns>
    public static async Task<GrammarConfiguration> LoadFromFileAsync(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            throw new FileNotFoundException($"Grammar configuration file not found: {configFilePath}");

        var json = await File.ReadAllTextAsync(configFilePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var config = JsonSerializer.Deserialize<GrammarConfiguration>(json, options);
        return config ?? new GrammarConfiguration();
    }

    /// <summary>
    /// Tries to load grammar configuration from a file.
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded configuration, or null if loading failed.</returns>
    public static async Task<GrammarConfiguration?> TryLoadFromFileAsync(string configFilePath)
    {
        try
        {
            return await LoadFromFileAsync(configFilePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the grammar configuration to a JSON file.
    /// </summary>
    /// <param name="configFilePath">Path where to save the configuration file.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveToFileAsync(string configFilePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(this, options);
        await File.WriteAllTextAsync(configFilePath, json);
    }

    private static bool IsPathMatch(string filePath, string pattern)
    {
        // Simple glob pattern matching implementation
        // For production use, consider using a dedicated glob matching library
        
        // Convert glob pattern to regex
        var regexPattern = pattern
            .Replace(".", "\\.")
            .Replace("**", ".*")
            .Replace("*", "[^/]*")
            .Replace("?", ".")
            .Replace("/", "\\/");

        regexPattern = "^" + regexPattern + "$";

        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(filePath, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            // If regex compilation fails, fall back to simple string comparison
            return string.Equals(filePath, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}

/// <summary>
/// Represents a mapping from file characteristics to a specific grammar and version.
/// </summary>
public class GrammarMapping
{
    /// <summary>
    /// Gets or sets the grammar name.
    /// </summary>
    [JsonPropertyName("grammar")]
    public string Grammar { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the grammar version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the confidence level for this mapping (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets fallback grammar options.
    /// </summary>
    [JsonPropertyName("fallbacks")]
    public List<string> Fallbacks { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata for this mapping.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a content-based detection rule for grammar selection.
/// </summary>
public class ContentDetectionRule
{
    /// <summary>
    /// Gets or sets the rule name for identification.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the regular expression pattern to match against file content.
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the grammar mapping to use when this rule matches.
    /// </summary>
    [JsonPropertyName("mapping")]
    public GrammarMapping Mapping { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of lines to search (0 for unlimited).
    /// </summary>
    [JsonPropertyName("maxLines")]
    public int MaxLines { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether the pattern should be case-sensitive.
    /// </summary>
    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority of this rule (higher numbers are checked first).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;
}