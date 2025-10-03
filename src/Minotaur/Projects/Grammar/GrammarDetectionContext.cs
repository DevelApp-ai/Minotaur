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

namespace Minotaur.Projects.Grammar;

/// <summary>
/// Provides context information for grammar detection operations.
/// </summary>
public class GrammarDetectionContext
{
    /// <summary>
    /// Gets the absolute path to the file being analyzed.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the relative path from the project root.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file extension (including the dot).
    /// </summary>
    public string FileExtension => Path.GetExtension(FilePath);

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>
    /// Gets the complete file name.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the project root path.
    /// </summary>
    public string ProjectRootPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the detected project type.
    /// </summary>
    public ProjectType ProjectType { get; init; }

    /// <summary>
    /// Gets the grammar configuration for the project (if available).
    /// </summary>
    public GrammarConfiguration? Configuration { get; init; }

    /// <summary>
    /// Gets the file content for content-based detection (lazily loaded).
    /// </summary>
    public Lazy<string>? FileContent { get; init; }

    /// <summary>
    /// Gets additional context metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets a value indicating whether the file exists.
    /// </summary>
    public bool FileExists => File.Exists(FilePath);

    /// <summary>
    /// Gets the file size in bytes (0 if file doesn't exist).
    /// </summary>
    public long FileSize => FileExists ? new FileInfo(FilePath).Length : 0;

    /// <summary>
    /// Creates a new grammar detection context for a file.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="projectType">The detected project type.</param>
    /// <param name="configuration">Optional grammar configuration.</param>
    /// <param name="metadata">Additional context metadata.</param>
    /// <returns>A new grammar detection context.</returns>
    public static GrammarDetectionContext Create(
        string filePath,
        string projectRootPath,
        ProjectType projectType = ProjectType.GenericFolder,
        GrammarConfiguration? configuration = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var relativePath = Path.GetRelativePath(projectRootPath, filePath);

        return new GrammarDetectionContext
        {
            FilePath = Path.GetFullPath(filePath),
            RelativePath = relativePath,
            ProjectRootPath = Path.GetFullPath(projectRootPath),
            ProjectType = projectType,
            Configuration = configuration,
            FileContent = new Lazy<string>(() => File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty),
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a context with pre-loaded file content.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="fileContent">The file content.</param>
    /// <param name="projectType">The detected project type.</param>
    /// <param name="configuration">Optional grammar configuration.</param>
    /// <param name="metadata">Additional context metadata.</param>
    /// <returns>A new grammar detection context with pre-loaded content.</returns>
    public static GrammarDetectionContext CreateWithContent(
        string filePath,
        string projectRootPath,
        string fileContent,
        ProjectType projectType = ProjectType.GenericFolder,
        GrammarConfiguration? configuration = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var relativePath = Path.GetRelativePath(projectRootPath, filePath);

        return new GrammarDetectionContext
        {
            FilePath = Path.GetFullPath(filePath),
            RelativePath = relativePath,
            ProjectRootPath = Path.GetFullPath(projectRootPath),
            ProjectType = projectType,
            Configuration = configuration,
            FileContent = new Lazy<string>(() => fileContent),
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}