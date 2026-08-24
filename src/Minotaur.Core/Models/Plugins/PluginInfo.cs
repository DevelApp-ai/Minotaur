/*
 * This file is part of Minotaur.
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>. 
 */

namespace Minotaur.Core.Models.Plugins;

/// <summary>
/// Information about a plugin.
/// Contains metadata, configuration, and runtime information.
/// </summary>
public class PluginInfo
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the plugin name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin version.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Gets or sets the plugin author.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin category (e.g., language, visualization, export).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin license.</summary>
    public string License { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin website URL.</summary>
    public string WebsiteUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin repository URL.</summary>
    public string RepositoryUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of tags for this plugin.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the list of dependencies for this plugin.</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Gets or sets whether this plugin is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets whether this plugin is installed.</summary>
    public bool IsInstalled { get; set; } = false;

    /// <summary>Gets or sets the date when this plugin was installed.</summary>
    public DateTime InstallDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the number of downloads.</summary>
    public int Downloads { get; set; } = 0;

    /// <summary>Gets or sets the plugin rating (0-5).</summary>
    public double Rating { get; set; } = 0;

    /// <summary>Gets or sets the plugin documentation.</summary>
    public string Documentation { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin changelog.</summary>
    public List<ChangelogEntry> Changelog { get; set; } = new();

    /// <summary>Gets or sets the plugin configuration options.</summary>
    public List<PluginConfiguration> Configuration { get; set; } = new();

    /// <summary>Gets or sets custom properties for this plugin.</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>Gets or sets the path to the plugin assembly.</summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin type name.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this plugin info.
    /// </summary>
    public PluginInfo Clone()
    {
        return new PluginInfo
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Version = Version,
            Author = Author,
            Category = Category,
            License = License,
            WebsiteUrl = WebsiteUrl,
            RepositoryUrl = RepositoryUrl,
            Tags = new List<string>(Tags),
            Dependencies = new List<string>(Dependencies),
            IsEnabled = IsEnabled,
            IsInstalled = IsInstalled,
            InstallDate = InstallDate,
            Downloads = Downloads,
            Rating = Rating,
            Documentation = Documentation,
            Changelog = Changelog.Select(e => e.Clone()).ToList(),
            Configuration = Configuration.Select(c => c.Clone()).ToList(),
            Properties = new Dictionary<string, object>(Properties),
            AssemblyPath = AssemblyPath,
            TypeName = TypeName
        };
    }

    /// <summary>
    /// Returns a string representation of this plugin.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} v{Version} by {Author}";
    }
}

/// <summary>
/// Represents an entry in the plugin changelog.
/// </summary>
public class ChangelogEntry
{
    /// <summary>Gets or sets the version for this changelog entry.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the date of this changelog entry.</summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the list of changes in this entry.</summary>
    public List<ChangeEntry> Changes { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this changelog entry.
    /// </summary>
    public ChangelogEntry Clone()
    {
        return new ChangelogEntry
        {
            Version = Version,
            Date = Date,
            Changes = Changes.Select(c => c.Clone()).ToList()
        };
    }
}

/// <summary>
/// Represents a single change in a changelog entry.
/// </summary>
public class ChangeEntry
{
    /// <summary>Gets or sets the type of change (Added, Changed, Fixed, Removed).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the change.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this change entry.
    /// </summary>
    public ChangeEntry Clone()
    {
        return new ChangeEntry
        {
            Type = Type,
            Description = Description
        };
    }
}

/// <summary>
/// Represents a configuration option for a plugin.
/// </summary>
public class PluginConfiguration
{
    /// <summary>Gets or sets the configuration key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration type (boolean, number, string, select).</summary>
    public string Type { get; set; } = "string";

    /// <summary>Gets or sets the current value.</summary>
    public object? Value { get; set; }

    /// <summary>Gets or sets the default value.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Gets or sets the list of options (for select type).</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Gets or sets whether this configuration is required.</summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    public PluginConfiguration Clone()
    {
        return new PluginConfiguration
        {
            Key = Key,
            DisplayName = DisplayName,
            Description = Description,
            Type = Type,
            Value = Value,
            DefaultValue = DefaultValue,
            Options = new List<string>(Options),
            IsRequired = IsRequired
        };
    }
}

/// <summary>
/// Plugin category constants.
/// </summary>
public static class PluginCategories
{
    public const string Language = "language";
    public const string Visualization = "visualization";
    public const string Export = "export";
    public const string Analysis = "analysis";
    public const string Theme = "theme";
    public const string Parser = "parser";
    public const string Generator = "generator";
    public const string Tool = "tool";
}

/// <summary>
/// Change type constants.
/// </summary>
public static class ChangeTypes
{
    public const string Added = "Added";
    public const string Changed = "Changed";
    public const string Fixed = "Fixed";
    public const string Removed = "Removed";
}
