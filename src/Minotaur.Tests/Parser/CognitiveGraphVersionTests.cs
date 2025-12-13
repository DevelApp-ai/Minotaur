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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Parser;

namespace Minotaur.Tests.Parser;

[TestClass]
public class CognitiveGraphVersionTests
{
    [TestMethod]
    public void ProjectSizeAnalyzer_SmallProject_RecommendsV1()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var smallCode = "var x = 42;";

        // Act
        var version = analyzer.GetRecommendedVersion(smallCode);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V1, version);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_LargeProject_RecommendsV2()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var largeCode = GenerateLargeCode(15000); // More than default 10,000 line threshold

        // Act
        var version = analyzer.GetRecommendedVersion(largeCode);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V2, version);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_LargeCharCount_RecommendsV2()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var largeCode = new string('a', 600000); // More than default 500,000 char threshold

        // Act
        var version = analyzer.GetRecommendedVersion(largeCode);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V2, version);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_MultipleFiles_LargeProject_RecommendsV2()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var files = new Dictionary<string, string>();
        
        // Create 120 files (more than default 100 file threshold)
        for (int i = 0; i < 120; i++)
        {
            files[$"file{i}.cs"] = "var x = 42;";
        }

        // Act
        var version = analyzer.GetRecommendedVersion(files);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V2, version);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_MultipleFiles_SmallProject_RecommendsV1()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var files = new Dictionary<string, string>
        {
            { "file1.cs", "var x = 42;" },
            { "file2.cs", "var y = 43;" },
            { "file3.cs", "var z = 44;" }
        };

        // Act
        var version = analyzer.GetRecommendedVersion(files);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V1, version);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_CustomThresholds_Works()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 100,
            LargeProjectCharThreshold = 1000,
            LargeProjectFileThreshold = 5
        };
        var mediumCode = GenerateLargeCode(150); // More than custom 100 line threshold

        // Act
        var version = analyzer.GetRecommendedVersion(mediumCode);

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V2, version);
    }

    [TestMethod]
    public void ParserConfiguration_DefaultGraphVersion_IsAuto()
    {
        // Arrange & Act
        var config = new ParserConfiguration();

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.Auto, config.GraphVersion);
    }

    [TestMethod]
    public void ParserConfiguration_CanSetGraphVersionToV1()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.V1
        };

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V1, config.GraphVersion);
    }

    [TestMethod]
    public void ParserConfiguration_CanSetGraphVersionToV2()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.V2
        };

        // Assert
        Assert.AreEqual(CognitiveGraphVersion.V2, config.GraphVersion);
    }

    [TestMethod]
    public async Task StepParserIntegration_SmallProject_UsesV1WithAutoMode()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.Auto
        };
        using var integration = new StepParserIntegration(config);
        var smallCode = "var x = 42;";

        // Act
        var result = await integration.ParseToCognitiveGraphAsync(smallCode);

        // Assert
        Assert.IsNotNull(result);
        // Check metadata indicates V1 was used
        if (result.Metadata.TryGetValue("cognitiveGraphVersion", out var versionObj))
        {
            var version = versionObj?.ToString();
            Assert.AreEqual("V1", version);
        }
    }

    [TestMethod]
    public async Task StepParserIntegration_LargeProject_UsesV2WithAutoMode()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.Auto
        };
        using var integration = new StepParserIntegration(config);
        var largeCode = GenerateLargeCode(15000); // More than default 10,000 line threshold

        // Act
        var result = await integration.ParseToCognitiveGraphAsync(largeCode);

        // Assert
        Assert.IsNotNull(result);
        // Check metadata indicates V2 was used
        if (result.Metadata.TryGetValue("cognitiveGraphVersion", out var versionObj))
        {
            var version = versionObj?.ToString();
            Assert.AreEqual("V2", version);
        }
    }

    [TestMethod]
    public async Task StepParserIntegration_ForceV1_UsesV1EvenForLargeProject()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.V1
        };
        using var integration = new StepParserIntegration(config);
        var largeCode = GenerateLargeCode(15000);

        // Act
        var result = await integration.ParseToCognitiveGraphAsync(largeCode);

        // Assert
        Assert.IsNotNull(result);
        // Check metadata indicates V1 was forced
        if (result.Metadata.TryGetValue("cognitiveGraphVersion", out var versionObj))
        {
            var version = versionObj?.ToString();
            Assert.AreEqual("V1", version);
        }
    }

    [TestMethod]
    public async Task StepParserIntegration_ForceV2_UsesV2EvenForSmallProject()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            GraphVersion = CognitiveGraphVersion.V2
        };
        using var integration = new StepParserIntegration(config);
        var smallCode = "var x = 42;";

        // Act
        var result = await integration.ParseToCognitiveGraphAsync(smallCode);

        // Assert
        Assert.IsNotNull(result);
        // Check metadata indicates V2 was forced
        if (result.Metadata.TryGetValue("cognitiveGraphVersion", out var versionObj))
        {
            var version = versionObj?.ToString();
            Assert.AreEqual("V2", version);
        }
    }

    [TestMethod]
    public void StepParserIntegration_ExposesProjectSizeAnalyzer()
    {
        // Arrange & Act
        using var integration = new StepParserIntegration();

        // Assert
        Assert.IsNotNull(integration.SizeAnalyzer);
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_NullOrEmptySource_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();

        // Act & Assert
        Assert.IsFalse(analyzer.ShouldUseV2(string.Empty));
        Assert.IsFalse(analyzer.ShouldUseV2((string)null!));
    }

    [TestMethod]
    public void ProjectSizeAnalyzer_NullOrEmptyFiles_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();

        // Act & Assert
        Assert.IsFalse(analyzer.ShouldUseV2(new Dictionary<string, string>()));
        Assert.IsFalse(analyzer.ShouldUseV2((Dictionary<string, string>)null!));
    }

    /// <summary>
    /// Generates a large code snippet with the specified number of lines.
    /// </summary>
    private static string GenerateLargeCode(int lines)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lines; i++)
        {
            sb.AppendLine($"var x{i} = {i};");
        }
        return sb.ToString();
    }
}
