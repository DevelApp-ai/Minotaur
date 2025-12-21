using Minotaur.Parser;
using Xunit;

namespace Minotaur.Tests.Parser;

public class ProjectSizeAnalyzerTests
{
    [Fact]
    public void ShouldUseV2_EmptyString_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();

        // Act
        var result = analyzer.ShouldUseV2("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseV2_NullString_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();

        // Act
        var result = analyzer.ShouldUseV2((string)null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseV2_SmallProject_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var smallCode = "int x = 5;\nint y = 10;";

        // Act
        var result = analyzer.ShouldUseV2(smallCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseV2_ExceedsCharThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectCharThreshold = 1000
        };
        var largeCode = new string('x', 1001);

        // Act
        var result = analyzer.ShouldUseV2(largeCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_ExceedsLineThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 100
        };
        var largeCode = string.Join("\n", Enumerable.Repeat("line", 101));

        // Act
        var result = analyzer.ShouldUseV2(largeCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_ExactlyAtCharThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectCharThreshold = 1000
        };
        var code = new string('x', 1000);

        // Act
        var result = analyzer.ShouldUseV2(code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_ExactlyAtLineThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 50
        };
        var code = string.Join("\n", Enumerable.Repeat("line", 50));

        // Act
        var result = analyzer.ShouldUseV2(code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_EmptyDictionary_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var files = new Dictionary<string, string>();

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_NullDictionary_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();

        // Act
        var result = analyzer.ShouldUseV2((Dictionary<string, string>)null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_ExceedsFileCountThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectFileThreshold = 10
        };
        var files = new Dictionary<string, string>();
        for (int i = 0; i < 11; i++)
        {
            files[$"file{i}.cs"] = "content";
        }

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_ExceedsTotalCharThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectCharThreshold = 1000
        };
        var files = new Dictionary<string, string>
        {
            ["file1.cs"] = new string('x', 600),
            ["file2.cs"] = new string('y', 500)
        };

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_ExceedsTotalLineThreshold_ReturnsTrue()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 100
        };
        var files = new Dictionary<string, string>
        {
            ["file1.cs"] = string.Join("\n", Enumerable.Repeat("line", 60)),
            ["file2.cs"] = string.Join("\n", Enumerable.Repeat("line", 50))
        };

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_SmallProject_ReturnsFalse()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["file1.cs"] = "int x = 5;",
            ["file2.cs"] = "int y = 10;"
        };

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetRecommendedVersion_SmallProject_ReturnsV1()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var smallCode = "int x = 5;";

        // Act
        var version = analyzer.GetRecommendedVersion(smallCode);

        // Assert
        Assert.Equal(CognitiveGraphVersion.V1, version);
    }

    [Fact]
    public void GetRecommendedVersion_LargeProject_ReturnsV2()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectCharThreshold = 100
        };
        var largeCode = new string('x', 150);

        // Act
        var version = analyzer.GetRecommendedVersion(largeCode);

        // Assert
        Assert.Equal(CognitiveGraphVersion.V2, version);
    }

    [Fact]
    public void GetRecommendedVersion_MultipleFiles_SmallProject_ReturnsV1()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["file1.cs"] = "code1",
            ["file2.cs"] = "code2"
        };

        // Act
        var version = analyzer.GetRecommendedVersion(files);

        // Assert
        Assert.Equal(CognitiveGraphVersion.V1, version);
    }

    [Fact]
    public void GetRecommendedVersion_MultipleFiles_LargeProject_ReturnsV2()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectFileThreshold = 5
        };
        var files = new Dictionary<string, string>();
        for (int i = 0; i < 6; i++)
        {
            files[$"file{i}.cs"] = "content";
        }

        // Act
        var version = analyzer.GetRecommendedVersion(files);

        // Assert
        Assert.Equal(CognitiveGraphVersion.V2, version);
    }

    [Fact]
    public void LargeProjectLineThreshold_DefaultValue_Is10000()
    {
        // Arrange & Act
        var analyzer = new ProjectSizeAnalyzer();

        // Assert
        Assert.Equal(10_000, analyzer.LargeProjectLineThreshold);
    }

    [Fact]
    public void LargeProjectCharThreshold_DefaultValue_Is500000()
    {
        // Arrange & Act
        var analyzer = new ProjectSizeAnalyzer();

        // Assert
        Assert.Equal(500_000, analyzer.LargeProjectCharThreshold);
    }

    [Fact]
    public void LargeProjectFileThreshold_DefaultValue_Is100()
    {
        // Arrange & Act
        var analyzer = new ProjectSizeAnalyzer();

        // Assert
        Assert.Equal(100, analyzer.LargeProjectFileThreshold);
    }

    [Fact]
    public void Thresholds_CanBeCustomized()
    {
        // Arrange & Act
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 5000,
            LargeProjectCharThreshold = 100_000,
            LargeProjectFileThreshold = 50
        };

        // Assert
        Assert.Equal(5000, analyzer.LargeProjectLineThreshold);
        Assert.Equal(100_000, analyzer.LargeProjectCharThreshold);
        Assert.Equal(50, analyzer.LargeProjectFileThreshold);
    }

    [Fact]
    public void ShouldUseV2_CodeWithoutTrailingNewline_CountsLinesCorrectly()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 3
        };
        var code = "line1\nline2\nline3"; // 3 lines, no trailing newline

        // Act
        var result = analyzer.ShouldUseV2(code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_CodeWithTrailingNewline_CountsLinesCorrectly()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 3
        };
        var code = "line1\nline2\nline3\n"; // 3 lines with trailing newline

        // Act
        var result = analyzer.ShouldUseV2(code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_SingleLineNoNewline_CountsAsOneLine()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 1
        };
        var code = "single line";

        // Act
        var result = analyzer.ShouldUseV2(code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseV2_MultipleFiles_MixedSizes_CorrectlyAggregates()
    {
        // Arrange
        var analyzer = new ProjectSizeAnalyzer
        {
            LargeProjectLineThreshold = 10
        };
        var files = new Dictionary<string, string>
        {
            ["small.cs"] = "line1\nline2",      // 2 lines
            ["medium.cs"] = "line1\nline2\nline3\nline4",  // 4 lines
            ["large.cs"] = "line1\nline2\nline3\nline4\nline5"  // 5 lines
        };
        // Total: 11 lines, should exceed threshold of 10

        // Act
        var result = analyzer.ShouldUseV2(files);

        // Assert
        Assert.True(result);
    }
}
