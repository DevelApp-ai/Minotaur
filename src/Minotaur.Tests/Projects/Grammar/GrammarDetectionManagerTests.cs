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

using Minotaur.Projects;
using Minotaur.Projects.Grammar;
using Xunit;

namespace Minotaur.Tests.Projects.Grammar;

public class GrammarDetectionManagerTests
{
    [Fact]
    public async Task DetectGrammarAsync_CSharpFile_ReturnsCorrectGrammar()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var tempFile = Path.GetTempFileName();
        var csharpFile = Path.ChangeExtension(tempFile, ".cs");
        
        try
        {
            await File.WriteAllTextAsync(csharpFile, "using System;\nnamespace MyNamespace\n{\n    public class Test { }\n}");

            // Act
            var result = await manager.DetectGrammarAsync(csharpFile, Path.GetDirectoryName(csharpFile)!);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal("CSharp10.grammar", result.GrammarName);
            Assert.NotNull(result.Version);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(csharpFile)) File.Delete(csharpFile);
        }
    }

    [Fact]
    public async Task DetectGrammarAsync_WithContent_ProcessesContentCorrectly()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var content = "using System;\nnamespace MyNamespace\n{\n    public class Test { }\n}";

        // Act
        var result = await manager.DetectGrammarAsync("/path/to/test.cs", content, "/path/to");

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("CSharp10.grammar", result.GrammarName);
        Assert.True(result.Confidence > 0.8); // Content-based detection should have high confidence
    }

    [Fact]
    public async Task DetectGrammarsAsync_MultipleFiles_ReturnsResultsForAll()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var files = new[]
            {
                Path.Combine(tempDir, "test.cs"),
                Path.Combine(tempDir, "script.js"),
                Path.Combine(tempDir, "main.py")
            };

            foreach (var file in files)
            {
                await File.WriteAllTextAsync(file, "// test content");
            }

            // Act
            var results = await manager.DetectGrammarsAsync(files, tempDir);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results.Values, result => Assert.True(result.IsSuccessful));
            
            Assert.Equal("CSharp10.grammar", results[files[0]].GrammarName);
            Assert.Equal("JavaScriptES2022.grammar", results[files[1]].GrammarName);
            Assert.Equal("Python311.grammar", results[files[2]].GrammarName);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetConfigurationAsync_NoConfigFile_ReturnsNull()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var config = await manager.GetConfigurationAsync(tempDir);

            // Assert
            Assert.Null(config);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetConfigurationAsync_WithConfigFile_LoadsConfiguration()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var configPath = Path.Combine(tempDir, "minotaur.grammar.json");
            var config = new GrammarConfiguration
            {
                DefaultGrammar = "CustomGrammar.grammar",
                DefaultVersion = "1.0.0"
            };
            await config.SaveToFileAsync(configPath);

            // Act
            var loadedConfig = await manager.GetConfigurationAsync(tempDir);

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.Equal("CustomGrammar.grammar", loadedConfig.DefaultGrammar);
            Assert.Equal("1.0.0", loadedConfig.DefaultVersion);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveConfigurationAsync_SavesAndLoadsCorrectly()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new GrammarConfiguration
            {
                DefaultGrammar = "TestGrammar.grammar",
                DefaultVersion = "2.0.0"
            };
            config.ExtensionMappings[".test"] = new GrammarMapping
            {
                Grammar = "TestSpecific.grammar",
                Version = "1.5.0",
                Confidence = 0.9
            };

            // Act
            await manager.SaveConfigurationAsync(tempDir, config);
            var loadedConfig = await manager.GetConfigurationAsync(tempDir);

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.Equal("TestGrammar.grammar", loadedConfig.DefaultGrammar);
            Assert.Equal("2.0.0", loadedConfig.DefaultVersion);
            Assert.Contains(".test", loadedConfig.ExtensionMappings.Keys);
            Assert.Equal("TestSpecific.grammar", loadedConfig.ExtensionMappings[".test"].Grammar);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ClearConfigurationCache_ClearsCache()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();

        // Act
        manager.ClearConfigurationCache();
        var stats = manager.GetStatistics();

        // Assert
        Assert.Equal(0, (int)stats["configurationsCached"]);
    }

    [Fact]
    public void GetStatistics_ReturnsValidStatistics()
    {
        // Arrange
        using var manager = GrammarDetectionManager.CreateDefault();

        // Act
        var stats = manager.GetStatistics();

        // Assert
        Assert.Contains("configurationsCached", stats.Keys);
        Assert.Contains("detectorsRegistered", stats.Keys);
        Assert.Contains("detectorTypes", stats.Keys);
        Assert.True((int)stats["detectorsRegistered"] > 0);
    }

    [Fact]
    public void CreateDefault_CreatesManagerWithStandardDetectors()
    {
        // Act
        using var manager = GrammarDetectionManager.CreateDefault();
        var detectors = manager.GetDetectors();

        // Assert
        Assert.NotEmpty(detectors);
        Assert.Contains(detectors, d => d.DetectorId == "content-based");
        Assert.Contains(detectors, d => d.DetectorId == "file-extension");
    }
}