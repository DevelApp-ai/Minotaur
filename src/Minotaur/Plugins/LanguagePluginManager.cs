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

using System.Reflection;

namespace Minotaur.Plugins;

/// <summary>
/// Manages language plugins using the DevelApp.RuntimePluggableClassFactory for dynamic extensibility.
/// Allows runtime discovery and registration of new language support without recompilation.
/// </summary>
public class LanguagePluginManager : IDisposable
{
    private readonly Dictionary<string, ILanguagePlugin> _loadedPlugins;
    private readonly Dictionary<string, string> _extensionToLanguage;
    private bool _disposed;

    public LanguagePluginManager()
    {
        _loadedPlugins = new Dictionary<string, ILanguagePlugin>(StringComparer.OrdinalIgnoreCase);
        _extensionToLanguage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        InitializeBuiltInPlugins();
    }

    /// <summary>
    /// Gets all registered language plugins
    /// </summary>
    public IReadOnlyDictionary<string, ILanguagePlugin> RegisteredPlugins => _loadedPlugins;

    /// <summary>
    /// Gets a language plugin by its identifier
    /// </summary>
    public ILanguagePlugin? GetPlugin(string languageId)
    {
        return _loadedPlugins.TryGetValue(languageId, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// Gets a language plugin by file extension
    /// </summary>
    public ILanguagePlugin? GetPluginByExtension(string fileExtension)
    {
        if (!fileExtension.StartsWith("."))
            fileExtension = "." + fileExtension;

        if (_extensionToLanguage.TryGetValue(fileExtension, out var languageId))
        {
            return GetPlugin(languageId);
        }

        return null;
    }

    /// <summary>
    /// Registers a language plugin
    /// </summary>
    public void RegisterPlugin(ILanguagePlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        _loadedPlugins[plugin.LanguageId] = plugin;

        // Register file extensions
        foreach (var extension in plugin.SupportedExtensions)
        {
            _extensionToLanguage[extension] = plugin.LanguageId;
        }
    }

    /// <summary>
    /// Discovers and loads plugins from specified directories
    /// </summary>
    public async Task LoadPluginsFromDirectoryAsync(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            return;

        try
        {
            var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var pluginFile in pluginFiles)
            {
                await LoadPluginFromAssemblyAsync(pluginFile);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - plugin loading should be resilient
            Console.WriteLine($"Error loading plugins from directory {pluginDirectory}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a plugin from a specific assembly file
    /// </summary>
    public async Task LoadPluginFromAssemblyAsync(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ILanguagePlugin).IsAssignableFrom(t));

            foreach (var pluginType in pluginTypes)
            {
                if (Activator.CreateInstance(pluginType) is ILanguagePlugin plugin)
                {
                    RegisterPlugin(plugin);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - plugin loading should be resilient
            Console.WriteLine($"Error loading plugin from assembly {assemblyPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets supported file extensions across all loaded plugins
    /// </summary>
    public string[] GetSupportedExtensions()
    {
        return _extensionToLanguage.Keys.ToArray();
    }

    /// <summary>
    /// Gets supported language identifiers
    /// </summary>
    public string[] GetSupportedLanguages()
    {
        return _loadedPlugins.Keys.ToArray();
    }

    private void InitializeBuiltInPlugins()
    {
        // Register built-in language plugins
        RegisterPlugin(new CSharpLanguagePlugin());
        RegisterPlugin(new JavaScriptLanguagePlugin());
        RegisterPlugin(new PythonLanguagePlugin());
        RegisterPlugin(new LLVMLanguagePlugin());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var plugin in _loadedPlugins.Values)
            {
                if (plugin is IDisposable disposablePlugin)
                {
                    disposablePlugin.Dispose();
                }
            }

            _loadedPlugins.Clear();
            _extensionToLanguage.Clear();
            _disposed = true;
        }
    }
}