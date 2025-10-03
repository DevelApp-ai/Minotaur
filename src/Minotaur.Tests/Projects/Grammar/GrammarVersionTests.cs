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

using Minotaur.Projects.Grammar;
using Xunit;

namespace Minotaur.Tests.Projects.Grammar;

public class GrammarVersionTests
{
    [Fact]
    public void Constructor_CreatesSemverVersion_Correctly()
    {
        // Arrange & Act
        var version = new GrammarVersion(1, 2, 3, "alpha", "build123");

        // Assert
        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal("alpha", version.PreRelease);
        Assert.Equal("build123", version.BuildMetadata);
        Assert.Equal("1.2.3-alpha+build123", version.ToString());
    }

    [Theory]
    [InlineData("1.2.3", 1, 2, 3, null, null)]
    [InlineData("2.0.0-beta", 2, 0, 0, "beta", null)]
    [InlineData("1.0.0+build", 1, 0, 0, null, "build")]
    [InlineData("1.2.3-alpha+build", 1, 2, 3, "alpha", "build")]
    [InlineData("10", 10, 0, 0, null, null)]
    [InlineData("5.4", 5, 4, 0, null, null)]
    public void Parse_SemverVersions_ParsesCorrectly(string input, int major, int minor, int patch, string? preRelease, string? buildMetadata)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(preRelease, version.PreRelease);
        Assert.Equal(buildMetadata, version.BuildMetadata);
    }

    [Theory]
    [InlineData("C#10", 10, 0)]
    [InlineData("CSharp10", 10, 0)]
    [InlineData("C#11.2", 11, 2)]
    [InlineData("csharp10", 10, 0)] // Case insensitive
    public void Parse_CSharpVersions_ParsesCorrectly(string input, int expectedMajor, int expectedMinor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("ES2022", 13)]
    [InlineData("ES6", 6)]
    [InlineData("JavaScript2022", 13)]
    [InlineData("ECMAScript2015", 6)]
    public void Parse_JavaScriptVersions_ParsesCorrectly(string input, int expectedMajor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("Python311", 3, 11)]
    [InlineData("Python39", 3, 9)]
    [InlineData("python312", 3, 12)] // Case insensitive
    public void Parse_PythonVersions_ParsesCorrectly(string input, int expectedMajor, int expectedMinor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("Java17", 17)]
    [InlineData("Java8", 8)]
    [InlineData("java11", 11)] // Case insensitive
    public void Parse_JavaVersions_ParsesCorrectly(string input, int expectedMajor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("C17", 17)]
    [InlineData("Cpp20", 20)]
    [InlineData("c11", 11)] // Case insensitive
    public void Parse_CVersions_ParsesCorrectly(string input, int expectedMajor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("Rust2021", 2021)]
    [InlineData("rust2018", 2018)] // Case insensitive
    public void Parse_RustVersions_ParsesCorrectly(string input, int expectedMajor)
    {
        // Act
        var version = GrammarVersion.Parse(input);

        // Assert
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(input, version.OriginalString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("1.2.3.4.5")]
    public void Parse_InvalidVersions_ThrowsArgumentException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => GrammarVersion.Parse(input));
    }

    [Theory]
    [InlineData("1.2.3", true)]
    [InlineData("invalid", false)]
    [InlineData("C#10", true)]
    [InlineData("", false)]
    public void TryParse_ReturnsExpectedResult(string input, bool expectedSuccess)
    {
        // Act
        var success = GrammarVersion.TryParse(input, out var version);

        // Assert
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.NotNull(version);
        }
        else
        {
            Assert.Null(version);
        }
    }

    [Fact]
    public void CompareTo_OrdersVersionsCorrectly()
    {
        // Arrange
        var versions = new[]
        {
            GrammarVersion.Parse("1.0.0-alpha"),
            GrammarVersion.Parse("1.0.0"),
            GrammarVersion.Parse("1.0.1"),
            GrammarVersion.Parse("1.1.0"),
            GrammarVersion.Parse("2.0.0")
        };

        // Act
        Array.Sort(versions);

        // Assert
        Assert.Equal("1.0.0-alpha", versions[0].ToString());
        Assert.Equal("1.0.0", versions[1].ToString());
        Assert.Equal("1.0.1", versions[2].ToString());
        Assert.Equal("1.1.0", versions[3].ToString());
        Assert.Equal("2.0.0", versions[4].ToString());
    }

    [Fact]
    public void Equals_ReturnsTrueForSameVersions()
    {
        // Arrange
        var version1 = GrammarVersion.Parse("1.2.3");
        var version2 = GrammarVersion.Parse("1.2.3");

        // Act & Assert
        Assert.True(version1.Equals(version2));
        Assert.True(version1 == version2);
        Assert.False(version1 != version2);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashForEqualVersions()
    {
        // Arrange
        var version1 = GrammarVersion.Parse("1.2.3");
        var version2 = GrammarVersion.Parse("1.2.3");

        // Act & Assert
        Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    public void LessThanOperator_WorksCorrectly(string left, string right, bool expected)
    {
        // Arrange
        var leftVersion = GrammarVersion.Parse(left);
        var rightVersion = GrammarVersion.Parse(right);

        // Act & Assert
        Assert.Equal(expected, leftVersion < rightVersion);
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0", true)]
    [InlineData("1.0.0", "2.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    public void GreaterThanOperator_WorksCorrectly(string left, string right, bool expected)
    {
        // Arrange
        var leftVersion = GrammarVersion.Parse(left);
        var rightVersion = GrammarVersion.Parse(right);

        // Act & Assert
        Assert.Equal(expected, leftVersion > rightVersion);
    }
}