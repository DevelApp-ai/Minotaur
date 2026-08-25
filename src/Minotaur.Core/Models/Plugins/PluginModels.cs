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

using System;
using System.Collections.Generic;

namespace Minotaur.Core.Models.Plugins;

/// <summary>
/// Information about a plugin.
/// </summary>
public class PluginInfo
{
    /// <summary>Gets or sets the unique identifier of the plugin.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the plugin.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of the plugin (e.g., "Language", "Tool", "Exporter").</summary>
    public string Type { get; set; } = "Language";

    /// <summary>Gets or sets the version of the plugin.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Gets or sets the author of the plugin.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the plugin.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the path to the plugin assembly.</summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the plugin is currently loaded.</summary>
    public bool IsLoaded { get; set; } = false;

    /// <summary>Gets or sets the list of dependencies.</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Gets or sets the supported file extensions.</summary>
    public List<string> SupportedExtensions { get; set; } = new();

    /// <summary>Gets or sets the supported features.</summary>
    public List<string> SupportedFeatures { get; set; } = new();

    /// <summary>Gets or sets the icon for the plugin.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the license of the plugin.</summary>
    public string? License { get; set; }

    /// <summary>Gets or sets the repository URL.</summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>Gets or sets the documentation URL.</summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>Gets or sets the date when the plugin was installed.</summary>
    public DateTimeOffset InstalledDate { get; set; } = DateTimeOffset.Now;

    /// <summary>Gets or sets the last time the plugin was updated.</summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Creates a copy of this PluginInfo.
    /// </summary>
    public PluginInfo Clone()
    {
        return new PluginInfo
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Version = Version,
            Author = Author,
            Description = Description,
            AssemblyPath = AssemblyPath,
            IsLoaded = IsLoaded,
            Dependencies = new List<string>(Dependencies),
            SupportedExtensions = new List<string>(SupportedExtensions),
            SupportedFeatures = new List<string>(SupportedFeatures),
            Icon = Icon,
            License = License,
            RepositoryUrl = RepositoryUrl,
            DocumentationUrl = DocumentationUrl,
            InstalledDate = InstalledDate,
            LastUpdated = LastUpdated
        };
    }
}

/// <summary>
/// Represents a plugin package from the marketplace.
/// </summary>
public class PluginPackage
{
    /// <summary>Gets or sets the package identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the package name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the package version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the package description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the author.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the download URL.</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the package size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the download count.</summary>
    public int DownloadCount { get; set; }

    /// <summary>Gets or sets the rating (0-5).</summary>
    public double Rating { get; set; }

    /// <summary>Gets or sets the tags.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the supported Minotaur versions.</summary>
    public List<string> SupportedVersions { get; set; } = new();

    /// <summary>Gets or sets the last updated date.</summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>Gets or sets whether this is an official plugin.</summary>
    public bool IsOfficial { get; set; }

    /// <summary>Gets or sets whether this is verified.</summary>
    public bool IsVerified { get; set; }
}

/// <summary>
/// Represents a plugin installation request.
/// </summary>
public class PluginInstallationRequest
{
    /// <summary>Gets or sets the package ID to install.</summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the version to install.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the installation directory.</summary>
    public string? InstallationDirectory { get; set; }

    /// <summary>Gets or sets whether to overwrite existing installation.</summary>
    public bool Overwrite { get; set; } = false;
}

/// <summary>
/// Represents a plugin uninstallation request.
/// </summary>
public class PluginUninstallationRequest
{
    /// <summary>Gets or sets the plugin ID to uninstall.</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to delete the plugin files.</summary>
    public bool DeleteFiles { get; set; } = false;
}

/// <summary>
/// Represents the result of a plugin operation.
/// </summary>
public class PluginOperationResult
{
    /// <summary>Gets or sets whether the operation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin ID.</summary>
    public string? PluginId { get; set; }

    /// <summary>Gets or sets the error message if any.</summary>
    public string? Error { get; set; }

    /// <summary>Gets or sets the details.</summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents plugin load status.
/// </summary>
public enum PluginLoadStatus
{
    /// <summary>Plugin is not loaded.</summary>
    NotLoaded,
    /// <summary>Plugin is loaded and active.</summary>
    Loaded,
    /// <summary>Plugin failed to load.</summary>
    Failed,
    /// <summary>Plugin is being loaded.</summary>
    Loading,
    /// <summary>Plugin is being unloaded.</summary>
    Unloading
}

/// <summary>
/// Represents plugin compatibility information.
/// </summary>
public class PluginCompatibility
{
    /// <summary>Gets or sets the Minotaur version.</summary>
    public string MinotaurVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the plugin is compatible.</summary>
    public bool IsCompatible { get; set; } = true;

    /// <summary>Gets or sets the compatibility issues.</summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>Gets or sets the recommended action.</summary>
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Represents plugin configuration.
/// </summary>
public class PluginConfiguration
{
    /// <summary>Gets or sets the plugin ID.</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration values.</summary>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>Gets or sets whether the plugin is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; } = 0;
}
