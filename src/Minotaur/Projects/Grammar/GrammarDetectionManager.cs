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

using Minotaur.Projects.Grammar.Detectors;

namespace Minotaur.Projects.Grammar;

/// <summary>
/// Manages grammar detection for files in a project, providing a centralized interface for all detection operations.
/// </summary>
public class GrammarDetectionManager : IDisposable
{
    private readonly CompositeGrammarDetector _primaryDetector;
    private readonly Dictionary<string, GrammarConfiguration> _configurationCache;
    private readonly string[] _configurationFileNames = { "minotaur.grammar.json", ".minotaur.grammar.json", "grammar.config.json" };
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the GrammarDetectionManager class.
    /// </summary>
    /// <param name="primaryDetector">The primary composite detector to use. If null, creates a default detector.</param>
    public GrammarDetectionManager(CompositeGrammarDetector? primaryDetector = null)
    {
        _primaryDetector = primaryDetector ?? CompositeGrammarDetector.CreateDefault();
        _configurationCache = new Dictionary<string, GrammarConfiguration>();
    }

    /// <summary>
    /// Detects the appropriate grammar for a file.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="projectType">The detected project type.</param>
    /// <returns>A task that represents the asynchronous detection operation.</returns>
    public async Task<GrammarDetectionResult> DetectGrammarAsync(
        string filePath,
        string projectRootPath,
        ProjectType projectType = ProjectType.GenericFolder)
    {
        var configuration = await GetConfigurationAsync(projectRootPath);
        var context = GrammarDetectionContext.Create(filePath, projectRootPath, projectType, configuration);

        return await _primaryDetector.DetectGrammarAsync(context);
    }

    /// <summary>
    /// Detects grammar for a file with pre-loaded content.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <param name="fileContent">The file content.</param>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="projectType">The detected project type.</param>
    /// <returns>A task that represents the asynchronous detection operation.</returns>
    public async Task<GrammarDetectionResult> DetectGrammarAsync(
        string filePath,
        string fileContent,
        string projectRootPath,
        ProjectType projectType = ProjectType.GenericFolder)
    {
        var configuration = await GetConfigurationAsync(projectRootPath);
        var context = GrammarDetectionContext.CreateWithContent(filePath, projectRootPath, fileContent, projectType, configuration);

        return await _primaryDetector.DetectGrammarAsync(context);
    }

    /// <summary>
    /// Detects grammars for multiple files in a project.
    /// </summary>
    /// <param name="filePaths">The file paths to analyze.</param>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="projectType">The detected project type.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent detection operations.</param>
    /// <returns>A task that represents the asynchronous batch detection operation.</returns>
    public async Task<Dictionary<string, GrammarDetectionResult>> DetectGrammarsAsync(
        IEnumerable<string> filePaths,
        string projectRootPath,
        ProjectType projectType = ProjectType.GenericFolder,
        int maxConcurrency = 4)
    {
        var configuration = await GetConfigurationAsync(projectRootPath);
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var results = new Dictionary<string, GrammarDetectionResult>();
        var tasks = new List<Task>();

        foreach (var filePath in filePaths)
        {
            tasks.Add(DetectSingleFileAsync(filePath, projectRootPath, projectType, configuration, semaphore, results));
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Gets or loads the grammar configuration for a project.
    /// </summary>
    /// <param name="projectRootPath">The project root path.</param>
    /// <returns>A task that represents the asynchronous configuration load operation.</returns>
    public async Task<GrammarConfiguration?> GetConfigurationAsync(string projectRootPath)
    {
        if (_configurationCache.TryGetValue(projectRootPath, out var cachedConfig))
        {
            return cachedConfig;
        }

        // Look for configuration files in the project root
        foreach (var configFileName in _configurationFileNames)
        {
            var configPath = Path.Combine(projectRootPath, configFileName);
            var config = await GrammarConfiguration.TryLoadFromFileAsync(configPath);
            if (config != null)
            {
                _configurationCache[projectRootPath] = config;
                return config;
            }
        }

        // No configuration found - cache null to avoid repeated lookups
        _configurationCache[projectRootPath] = null!;
        return null;
    }

    /// <summary>
    /// Clears the configuration cache, forcing configurations to be reloaded.
    /// </summary>
    public void ClearConfigurationCache()
    {
        _configurationCache.Clear();
    }

    /// <summary>
    /// Adds a custom detector to the detection pipeline.
    /// </summary>
    /// <param name="detector">The detector to add.</param>
    public void AddDetector(IGrammarDetector detector)
    {
        _primaryDetector.AddDetector(detector);
    }

    /// <summary>
    /// Removes a detector from the detection pipeline.
    /// </summary>
    /// <param name="detectorId">The ID of the detector to remove.</param>
    /// <returns>True if the detector was removed, false if it wasn't found.</returns>
    public bool RemoveDetector(string detectorId)
    {
        return _primaryDetector.RemoveDetector(detectorId);
    }

    /// <summary>
    /// Gets all registered detectors.
    /// </summary>
    /// <returns>A read-only list of detectors.</returns>
    public IReadOnlyList<IGrammarDetector> GetDetectors()
    {
        return _primaryDetector.GetDetectors();
    }

    /// <summary>
    /// Creates a grammar configuration file for a project.
    /// </summary>
    /// <param name="projectRootPath">The project root path.</param>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="fileName">The configuration file name (defaults to "minotaur.grammar.json").</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveConfigurationAsync(
        string projectRootPath,
        GrammarConfiguration configuration,
        string fileName = "minotaur.grammar.json")
    {
        var configPath = Path.Combine(projectRootPath, fileName);
        await configuration.SaveToFileAsync(configPath);

        // Update cache
        _configurationCache[projectRootPath] = configuration;
    }

    /// <summary>
    /// Gets usage statistics for the detection manager.
    /// </summary>
    /// <returns>A dictionary containing usage statistics.</returns>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            ["configurationsCached"] = _configurationCache.Count,
            ["detectorsRegistered"] = _primaryDetector.GetDetectors().Count,
            ["detectorTypes"] = _primaryDetector.GetDetectors().Select(d => d.GetType().Name).ToArray()
        };
    }

    private async Task DetectSingleFileAsync(
        string filePath,
        string projectRootPath,
        ProjectType projectType,
        GrammarConfiguration? configuration,
        SemaphoreSlim semaphore,
        Dictionary<string, GrammarDetectionResult> results)
    {
        await semaphore.WaitAsync();
        try
        {
            var context = GrammarDetectionContext.Create(filePath, projectRootPath, projectType, configuration);
            var result = await _primaryDetector.DetectGrammarAsync(context);

            lock (results)
            {
                results[filePath] = result;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Releases all resources used by the GrammarDetectionManager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _configurationCache.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Creates a default grammar detection manager with standard detectors.
    /// </summary>
    /// <returns>A configured grammar detection manager.</returns>
    public static GrammarDetectionManager CreateDefault()
    {
        return new GrammarDetectionManager();
    }
}