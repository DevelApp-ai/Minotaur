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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Minotaur.Core.Models.Plugins;
using Minotaur.Plugins;

namespace Minotaur.Core.Services.Plugins;

/// <summary>
/// Service for managing language plugins in Minotaur.
/// Handles loading, unloading, and discovery of plugins from directories.
/// </summary>
public class PluginManagerService : IPluginManagerService
{
    private readonly ILogger<PluginManagerService> _logger;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly Dictionary<string, ILanguagePlugin> _loadedPlugins = new();
    private readonly List<string> _pluginDirectories = new();

    /// <summary>
    /// Initializes a new instance of the PluginManagerService.
    /// </summary>
    public PluginManagerService(ILogger<PluginManagerService> logger)
    {
        _logger = logger;
        
        // Add default plugin directories
        AddDefaultPluginDirectories();
    }

    /// <summary>
    /// Gets the list of plugin directories being monitored.
    /// </summary>
    public IReadOnlyList<string> PluginDirectories => _pluginDirectories.AsReadOnly();

    /// <summary>
    /// Gets the loaded assemblies.
    /// </summary>
    public IReadOnlyDictionary<string, Assembly> LoadedAssemblies => _loadedAssemblies;

    /// <summary>
    /// Gets the loaded plugins.
    /// </summary>
    public IReadOnlyDictionary<string, ILanguagePlugin> LoadedPlugins => _loadedPlugins;

    /// <summary>
    /// Adds default plugin directories.
    /// </summary>
    private void AddDefaultPluginDirectories()
    {
        // Add standard plugin directories
        var defaultDirs = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "Plugins"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Minotaur.Plugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Minotaur", "Plugins")
        };

        foreach (var dir in defaultDirs)
        {
            if (Directory.Exists(dir) && !_pluginDirectories.Contains(dir))
            {
                _pluginDirectories.Add(dir);
            }
        }
    }

    /// <summary>
    /// Adds a plugin directory to monitor for plugins.
    /// </summary>
    public void AddPluginDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

        if (!_pluginDirectories.Contains(directoryPath))
        {
            _pluginDirectories.Add(directoryPath);
            _logger.LogInformation("Added plugin directory: {Directory}", directoryPath);
        }
    }

    /// <summary>
    /// Removes a plugin directory from monitoring.
    /// </summary>
    public bool RemovePluginDirectory(string directoryPath)
    {
        return _pluginDirectories.Remove(directoryPath);
    }

    /// <summary>
    /// Discovers available plugins in all monitored directories.
    /// </summary>
    public async Task<List<PluginInfo>> DiscoverAvailablePluginsAsync()
    {
        var plugins = new List<PluginInfo>();

        foreach (var directory in _pluginDirectories)
        {
            if (!Directory.Exists(directory))
                continue;

            try
            {
                var directoryPlugins = await DiscoverPluginsInDirectoryAsync(directory);
                plugins.AddRange(directoryPlugins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering plugins in directory: {Directory}", directory);
            }
        }

        return plugins;
    }

    /// <summary>
    /// Discovers plugins in a specific directory.
    /// </summary>
    private async Task<List<PluginInfo>> DiscoverPluginsInDirectoryAsync(string directory)
    {
        var plugins = new List<PluginInfo>();

        foreach (var file in Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var pluginInfo = await GetPluginInfoFromAssemblyAsync(file);
                if (pluginInfo != null)
                {
                    plugins.Add(pluginInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading plugin from: {File}", file);
            }
        }

        return plugins;
    }

    /// <summary>
    /// Gets plugin information from an assembly file.
    /// </summary>
    private async Task<PluginInfo?> GetPluginInfoFromAssemblyAsync(string assemblyPath)
    {
        try
        {
            // Load the assembly using AssemblyLoadContext for isolation
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            
            // Look for ILanguagePlugin implementations
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(ILanguagePlugin).IsAssignableFrom(type) && 
                    !type.IsInterface && !type.IsAbstract)
                {
                    // Create an instance to get metadata
                    var plugin = (ILanguagePlugin)Activator.CreateInstance(type);
                    
                    return new PluginInfo
                    {
                        Id = plugin.LanguageId,
                        Name = plugin.LanguageName,
                        Type = plugin.GetType().Name,
                        Version = plugin.Version,
                        Author = plugin.Author,
                        Description = plugin.Description,
                        AssemblyPath = assemblyPath,
                        IsLoaded = _loadedPlugins.ContainsKey(plugin.LanguageId),
                        Dependencies = GetAssemblyDependencies(assembly)
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error inspecting assembly: {Assembly}", assemblyPath);
        }

        return null;
    }

    /// <summary>
    /// Gets the dependencies of an assembly.
    /// </summary>
    private List<string> GetAssemblyDependencies(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => !n.StartsWith("System") && 
                           !n.StartsWith("Microsoft") && 
                           !n.StartsWith("netstandard") &&
                           !n.StartsWith("System") &&
                           !n.StartsWith("mscorlib"))
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Loads a plugin by its ID.
    /// </summary>
    public async Task<bool> LoadPluginAsync(string pluginId)
    {
        var availablePlugins = await DiscoverAvailablePluginsAsync();
        var pluginInfo = availablePlugins.FirstOrDefault(p => p.Id == pluginId);

        if (pluginInfo == null)
        {
            _logger.LogWarning("Plugin not found: {PluginId}", pluginId);
            return false;
        }

        return await LoadPluginFromInfoAsync(pluginInfo);
    }

    /// <summary>
    /// Loads a plugin from its PluginInfo.
    /// </summary>
    private async Task<bool> LoadPluginFromInfoAsync(PluginInfo pluginInfo)
    {
        if (_loadedPlugins.ContainsKey(pluginInfo.Id))
        {
            _logger.LogInformation("Plugin already loaded: {PluginId}", pluginInfo.Id);
            return true;
        }

        try
        {
            // Load the assembly
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginInfo.AssemblyPath);
            _loadedAssemblies[pluginInfo.Id] = assembly;

            // Find and instantiate the plugin
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(ILanguagePlugin).IsAssignableFrom(type) && 
                    !type.IsInterface && !type.IsAbstract)
                {
                    var plugin = (ILanguagePlugin)Activator.CreateInstance(type);
                    _loadedPlugins[pluginInfo.Id] = plugin;
                    
                    _logger.LogInformation("Successfully loaded plugin: {PluginId} ({PluginName})", 
                        pluginInfo.Id, pluginInfo.Name);
                    
                    return true;
                }
            }

            _logger.LogWarning("No ILanguagePlugin implementation found in: {Assembly}", pluginInfo.AssemblyPath);
            _loadedAssemblies.Remove(pluginInfo.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plugin: {PluginId}", pluginInfo.Id);
            if (_loadedAssemblies.ContainsKey(pluginInfo.Id))
                _loadedAssemblies.Remove(pluginInfo.Id);
            return false;
        }
    }

    /// <summary>
    /// Unloads a plugin by its ID.
    /// </summary>
    public bool UnloadPlugin(string pluginId)
    {
        if (!_loadedPlugins.ContainsKey(pluginId))
        {
            _logger.LogWarning("Plugin not loaded: {PluginId}", pluginId);
            return false;
        }

        try
        {
            // Remove from loaded plugins
            _loadedPlugins.Remove(pluginId);

            // Unload the assembly
            if (_loadedAssemblies.TryGetValue(pluginId, out var assembly))
            {
                // Note: In .NET Core, assemblies loaded by AssemblyLoadContext.Default
                // cannot be unloaded. For true unloading, we'd need a custom
                // AssemblyLoadContext. For now, we just remove the reference.
                _loadedAssemblies.Remove(pluginId);
                
                _logger.LogInformation("Plugin unloaded: {PluginId}", pluginId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin: {PluginId}", pluginId);
            return false;
        }
    }

    /// <summary>
    /// Reloads a plugin by its ID.
    /// </summary>
    public async Task<bool> ReloadPluginAsync(string pluginId)
    {
        // Unload first
        UnloadPlugin(pluginId);
        
        // Then load
        return await LoadPluginAsync(pluginId);
    }

    /// <summary>
    /// Gets a loaded plugin by its ID.
    /// </summary>
    public ILanguagePlugin? GetPlugin(string pluginId)
    {
        _loadedPlugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyDictionary<string, ILanguagePlugin> GetAllLoadedPlugins()
    {
        return new Dictionary<string, ILanguagePlugin>(_loadedPlugins);
    }

    /// <summary>
    /// Gets information about all available plugins.
    /// </summary>
    public async Task<List<PluginInfo>> GetAllPluginInfoAsync()
    {
        return await DiscoverAvailablePluginsAsync();
    }

    /// <summary>
    /// Gets information about a specific plugin.
    /// </summary>
    public async Task<PluginInfo?> GetPluginInfoAsync(string pluginId)
    {
        var allPlugins = await GetAllPluginInfoAsync();
        return allPlugins.FirstOrDefault(p => p.Id == pluginId);
    }

    /// <summary>
    /// Checks if a plugin is loaded.
    /// </summary>
    public bool IsPluginLoaded(string pluginId)
    {
        return _loadedPlugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Refreshes the list of available plugins.
    /// </summary>
    public async Task RefreshPluginsAsync()
    {
        // Clear cached info and rediscover
        await DiscoverAvailablePluginsAsync();
    }

    /// <summary>
    /// Gets plugins that support a specific file extension.
    /// </summary>
    public List<ILanguagePlugin> GetPluginsForExtension(string fileExtension)
    {
        return _loadedPlugins.Values
            .Where(p => p.SupportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets the plugin for a specific language ID.
    /// </summary>
    public ILanguagePlugin? GetPluginForLanguage(string languageId)
    {
        _loadedPlugins.TryGetValue(languageId, out var plugin);
        return plugin;
    }

    /// <summary>
    /// Gets all plugins that support a specific feature.
    /// </summary>
    public List<ILanguagePlugin> GetPluginsWithFeature(string feature)
    {
        return _loadedPlugins.Values
            .Where(p => p.SupportedFeatures.Contains(feature, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Loads all available plugins.
    /// </summary>
    public async Task<int> LoadAllPluginsAsync()
    {
        var plugins = await DiscoverAvailablePluginsAsync();
        var loadedCount = 0;

        foreach (var plugin in plugins)
        {
            if (await LoadPluginAsync(plugin.Id))
            {
                loadedCount++;
            }
        }

        return loadedCount;
    }

    /// <summary>
    /// Unloads all plugins.
    /// </summary>
    public void UnloadAllPlugins()
    {
        foreach (var pluginId in _loadedPlugins.Keys.ToList())
        {
            UnloadPlugin(pluginId);
        }
    }

    /// <summary>
    /// Gets the count of loaded plugins.
    /// </summary>
    public int LoadedPluginCount => _loadedPlugins.Count;

    /// <summary>
    /// Gets the count of available plugins.
    /// </summary>
    public async Task<int> AvailablePluginCountAsync => (await DiscoverAvailablePluginsAsync()).Count;
}

/// <summary>
/// Interface for plugin manager service.
/// </summary>
public interface IPluginManagerService
{
    /// <summary>Gets the list of plugin directories being monitored.</summary>
    IReadOnlyList<string> PluginDirectories { get; }

    /// <summary>Gets the loaded assemblies.</summary>
    IReadOnlyDictionary<string, Assembly> LoadedAssemblies { get; }

    /// <summary>Gets the loaded plugins.</summary>
    IReadOnlyDictionary<string, ILanguagePlugin> LoadedPlugins { get; }

    /// <summary>Adds a plugin directory to monitor for plugins.</summary>
    void AddPluginDirectory(string directoryPath);

    /// <summary>Removes a plugin directory from monitoring.</summary>
    bool RemovePluginDirectory(string directoryPath);

    /// <summary>Discovers available plugins in all monitored directories.</summary>
    Task<List<PluginInfo>> DiscoverAvailablePluginsAsync();

    /// <summary>Loads a plugin by its ID.</summary>
    Task<bool> LoadPluginAsync(string pluginId);

    /// <summary>Unloads a plugin by its ID.</summary>
    bool UnloadPlugin(string pluginId);

    /// <summary>Reloads a plugin by its ID.</summary>
    Task<bool> ReloadPluginAsync(string pluginId);

    /// <summary>Gets a loaded plugin by its ID.</summary>
    ILanguagePlugin? GetPlugin(string pluginId);

    /// <summary>Gets all loaded plugins.</summary>
    IReadOnlyDictionary<string, ILanguagePlugin> GetAllLoadedPlugins();

    /// <summary>Gets information about all available plugins.</summary>
    Task<List<PluginInfo>> GetAllPluginInfoAsync();

    /// <summary>Gets information about a specific plugin.</summary>
    Task<PluginInfo?> GetPluginInfoAsync(string pluginId);

    /// <summary>Checks if a plugin is loaded.</summary>
    bool IsPluginLoaded(string pluginId);

    /// <summary>Refreshes the list of available plugins.</summary>
    Task RefreshPluginsAsync();

    /// <summary>Gets plugins that support a specific file extension.</summary>
    List<ILanguagePlugin> GetPluginsForExtension(string fileExtension);

    /// <summary>Gets the plugin for a specific language ID.</summary>
    ILanguagePlugin? GetPluginForLanguage(string languageId);

    /// <summary>Gets all plugins that support a specific feature.</summary>
    List<ILanguagePlugin> GetPluginsWithFeature(string feature);

    /// <summary>Loads all available plugins.</summary>
    Task<int> LoadAllPluginsAsync();

    /// <summary>Unloads all plugins.</summary>
    void UnloadAllPlugins();
}
