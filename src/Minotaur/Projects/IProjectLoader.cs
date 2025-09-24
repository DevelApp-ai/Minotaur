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

namespace Minotaur.Projects;

/// <summary>
/// Interface for loading and analyzing project structures for cross-file editing and analysis.
/// </summary>
public interface IProjectLoader
{
    /// <summary>
    /// Loads a .NET solution for cross-file analysis.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file</param>
    /// <returns>Project structure with cross-file relationships</returns>
    Task<ProjectStructure> LoadSolutionAsync(string solutionPath);

    /// <summary>
    /// Loads a .NET project for cross-file analysis.
    /// </summary>
    /// <param name="projectPath">Path to the project file (.csproj, .vbproj, .fsproj)</param>
    /// <returns>Project structure with cross-file relationships</returns>
    Task<ProjectStructure> LoadProjectAsync(string projectPath);

    /// <summary>
    /// Loads a folder-based project with automatic type detection.
    /// </summary>
    /// <param name="folderPath">Path to the project folder</param>
    /// <returns>Project structure with cross-file relationships</returns>
    Task<ProjectStructure> LoadFolderAsync(string folderPath);

    /// <summary>
    /// Gets supported project types for the given path.
    /// </summary>
    /// <param name="path">Path to analyze</param>
    /// <returns>Detected project types</returns>
    Task<IEnumerable<ProjectType>> DetectProjectTypesAsync(string path);
}