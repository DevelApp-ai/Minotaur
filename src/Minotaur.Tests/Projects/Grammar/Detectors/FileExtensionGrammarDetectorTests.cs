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
using Minotaur.Projects.Grammar.Detectors;
using Xunit;

namespace Minotaur.Tests.Projects.Grammar.Detectors;

public class FileExtensionGrammarDetectorTests
{
    private readonly FileExtensionGrammarDetector _detector = new();

    [Fact]
    public void DetectorId_ReturnsCorrectValue()
    {
        // Act & Assert
        Assert.Equal("file-extension", _detector.DetectorId);
    }

    [Fact]
    public void Priority_ReturnsExpectedValue()
    {
        // Act & Assert
        Assert.Equal(100, _detector.Priority);
    }

    [Theory]
    [InlineData("/path/to/file.cs", true)]
    [InlineData("/path/to/file.txt", true)]
    [InlineData("/path/to/file", false)]
    [InlineData("/path/to/file.", false)]
    public void CanDetect_ReturnsExpectedResult(string filePath, bool expected)
    {
        // Arrange
        var context = GrammarDetectionContext.Create(filePath, "/path/to", ProjectType.GenericFolder);

        // Act
        var result = _detector.CanDetect(context);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".cs", "CSharp10.grammar", "10.0")]
    [InlineData(".js", "JavaScriptES2022.grammar", "ES2022")]
    [InlineData(".py", "Python311.grammar", "3.11")]
    [InlineData(".java", "Java17.grammar", "17")]
    [InlineData(".rs", "Rust2021.grammar", "2021")]
    [InlineData(".go", "Go119.grammar", "1.19")]
    public async Task DetectGrammarAsync_KnownExtensions_ReturnsCorrectGrammar(string extension, string expectedGrammar, string expectedVersion)
    {
        // Arrange
        var filePath = $"/path/to/file{extension}";
        var context = GrammarDetectionContext.Create(filePath, "/path/to", ProjectType.GenericFolder);

        // Act
        var result = await _detector.DetectGrammarAsync(context);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(expectedGrammar, result.GrammarName);
        Assert.NotNull(result.Version);
        Assert.Equal(expectedVersion, result.Version.OriginalString);
        Assert.Equal("file-extension", result.DetectorId);
        Assert.True(result.Confidence > 0.5);
    }

    [Fact]
    public async Task DetectGrammarAsync_UnknownExtension_ReturnsFailure()
    {
        // Arrange
        var context = GrammarDetectionContext.Create("/path/to/file.unknown", "/path/to", ProjectType.GenericFolder);

        // Act
        var result = await _detector.DetectGrammarAsync(context);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("file-extension", result.DetectorId);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("unknown", result.FailureReason);
    }

    [Fact]
    public async Task DetectGrammarAsync_NoExtension_ReturnsFailure()
    {
        // Arrange
        var context = GrammarDetectionContext.Create("/path/to/file", "/path/to", ProjectType.GenericFolder);

        // Act
        var result = await _detector.DetectGrammarAsync(context);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("file-extension", result.DetectorId);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task DetectGrammarAsync_WithProjectConfiguration_UsesConfigurationOverDefault()
    {
        // Arrange
        var configuration = new GrammarConfiguration();
        configuration.ExtensionMappings[".cs"] = new GrammarMapping
        {
            Grammar = "CustomCSharp.grammar",
            Version = "12.0",
            Confidence = 0.95
        };

        var context = GrammarDetectionContext.Create("/path/to/file.cs", "/path/to", ProjectType.GenericFolder, configuration);

        // Act
        var result = await _detector.DetectGrammarAsync(context);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("CustomCSharp.grammar", result.GrammarName);
        Assert.Equal("12.0", result.Version?.OriginalString);
        Assert.Equal(0.95, result.Confidence);
        Assert.Equal("project-configuration", result.Metadata["source"]);
    }

    [Fact]
    public void GetSupportedExtensions_ReturnsAllExtensions()
    {
        // Act
        var extensions = FileExtensionGrammarDetector.GetSupportedExtensions();

        // Assert
        Assert.NotEmpty(extensions);
        Assert.Contains(".cs", extensions);
        Assert.Contains(".js", extensions);
        Assert.Contains(".py", extensions);
        Assert.Contains(".java", extensions);
    }

    [Fact]
    public void GetDefaultMapping_ReturnsCorrectMapping()
    {
        // Act
        var mapping = FileExtensionGrammarDetector.GetDefaultMapping(".cs");

        // Assert
        Assert.NotNull(mapping);
        Assert.Equal("CSharp10.grammar", mapping.Grammar);
        Assert.Equal("10.0", mapping.Version);
    }

    [Fact]
    public void GetDefaultMapping_UnknownExtension_ReturnsNull()
    {
        // Act
        var mapping = FileExtensionGrammarDetector.GetDefaultMapping(".unknown");

        // Assert
        Assert.Null(mapping);
    }

    [Fact]
    public void AddExtensionMapping_AddsNewMapping()
    {
        // Arrange
        var customMapping = new GrammarMapping { Grammar = "CustomGrammar.grammar", Confidence = 0.8 };

        // Act
        FileExtensionGrammarDetector.AddExtensionMapping(".custom", customMapping);
        var result = FileExtensionGrammarDetector.GetDefaultMapping(".custom");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CustomGrammar.grammar", result.Grammar);
        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public void AddExtensionMapping_WithoutDot_AddsCorrectly()
    {
        // Arrange
        var customMapping = new GrammarMapping { Grammar = "AnotherCustom.grammar", Confidence = 0.9 };

        // Act
        FileExtensionGrammarDetector.AddExtensionMapping("nodot", customMapping);
        var result = FileExtensionGrammarDetector.GetDefaultMapping(".nodot");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AnotherCustom.grammar", result.Grammar);
    }
}