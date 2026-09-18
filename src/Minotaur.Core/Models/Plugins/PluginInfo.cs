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
