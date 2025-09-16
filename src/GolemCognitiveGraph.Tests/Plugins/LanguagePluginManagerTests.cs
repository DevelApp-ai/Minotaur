using GolemCognitiveGraph.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolemCognitiveGraph.Tests.Plugins;

[TestClass]
public class LanguagePluginManagerTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeBuiltInPlugins()
    {
        // Arrange & Act
        using var manager = new LanguagePluginManager();

        // Assert
        Assert.IsTrue(manager.RegisteredPlugins.Count >= 3);
        Assert.IsNotNull(manager.GetPlugin("csharp"));
        Assert.IsNotNull(manager.GetPlugin("javascript"));
        Assert.IsNotNull(manager.GetPlugin("python"));
    }

    [TestMethod]
    public void GetPlugin_WithValidLanguageId_ShouldReturnPlugin()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var plugin = manager.GetPlugin("csharp");

        // Assert
        Assert.IsNotNull(plugin);
        Assert.AreEqual("csharp", plugin.LanguageId);
        Assert.AreEqual("C#", plugin.DisplayName);
    }

    [TestMethod]
    public void GetPlugin_WithInvalidLanguageId_ShouldReturnNull()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var plugin = manager.GetPlugin("nonexistent");

        // Assert
        Assert.IsNull(plugin);
    }

    [TestMethod]
    public void GetPluginByExtension_WithValidExtension_ShouldReturnPlugin()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var plugin = manager.GetPluginByExtension(".cs");

        // Assert
        Assert.IsNotNull(plugin);
        Assert.AreEqual("csharp", plugin.LanguageId);
    }

    [TestMethod]
    public void GetPluginByExtension_WithExtensionWithoutDot_ShouldReturnPlugin()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var plugin = manager.GetPluginByExtension("js");

        // Assert
        Assert.IsNotNull(plugin);
        Assert.AreEqual("javascript", plugin.LanguageId);
    }

    [TestMethod]
    public void GetPluginByExtension_WithInvalidExtension_ShouldReturnNull()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var plugin = manager.GetPluginByExtension(".unknown");

        // Assert
        Assert.IsNull(plugin);
    }

    [TestMethod]
    public void GetSupportedExtensions_ShouldReturnAllExtensions()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var extensions = manager.GetSupportedExtensions();

        // Assert
        Assert.IsTrue(extensions.Length > 0);
        Assert.IsTrue(extensions.Contains(".cs"));
        Assert.IsTrue(extensions.Contains(".js"));
        Assert.IsTrue(extensions.Contains(".py"));
    }

    [TestMethod]
    public void GetSupportedLanguages_ShouldReturnAllLanguageIds()
    {
        // Arrange
        using var manager = new LanguagePluginManager();

        // Act
        var languages = manager.GetSupportedLanguages();

        // Assert
        Assert.IsTrue(languages.Length >= 3);
        Assert.IsTrue(languages.Contains("csharp"));
        Assert.IsTrue(languages.Contains("javascript"));
        Assert.IsTrue(languages.Contains("python"));
    }

    [TestMethod]
    public void RegisterPlugin_WithCustomPlugin_ShouldAddToRegisteredPlugins()
    {
        // Arrange
        using var manager = new LanguagePluginManager();
        var customPlugin = new TestLanguagePlugin();

        // Act
        manager.RegisterPlugin(customPlugin);

        // Assert
        var retrievedPlugin = manager.GetPlugin("test");
        Assert.IsNotNull(retrievedPlugin);
        Assert.AreEqual("test", retrievedPlugin.LanguageId);
    }

    private class TestLanguagePlugin : ILanguagePlugin
    {
        public string LanguageId => "test";
        public string DisplayName => "Test Language";
        public string[] SupportedExtensions => new[] { ".test" };

        public Task<string> UnparseAsync(Core.CognitiveGraphNode graph)
        {
            return Task.FromResult("test code");
        }

        public CodeFormattingOptions GetFormattingOptions()
        {
            return new CodeFormattingOptions();
        }

        public Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(Core.CognitiveGraphNode graph)
        {
            return Task.FromResult(new UnparseValidationResult { CanUnparse = true });
        }
    }
}