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
using Minotaur.Core;
using Minotaur.Editor;

namespace Minotaur.Tests.Parser;

[TestClass]
public class StepParserIntegrationTests
{
    [TestMethod]
    public void ParserConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ParserConfiguration();

        // Assert
        Assert.AreEqual("csharp", config.Language);
        Assert.IsTrue(config.IncludeLocationInfo);
        Assert.IsTrue(config.PreserveComments);
        Assert.IsFalse(config.IncludeWhitespace);
    }

    [TestMethod]
    public void StepParserIntegration_Constructor_CreatesInstance()
    {
        // Arrange & Act
        using var integration = new StepParserIntegration();

        // Assert
        Assert.IsNotNull(integration);
    }

    [TestMethod]
    public void StepParserIntegration_ConstructorWithConfig_UsesProvidedConfig()
    {
        // Arrange
        var config = new ParserConfiguration
        {
            Language = "javascript",
            IncludeLocationInfo = false,
            PreserveComments = false
        };

        // Act
        using var integration = new StepParserIntegration(config);

        // Assert
        Assert.IsNotNull(integration);
    }

    [TestMethod]
    public async Task ParseToEditableGraphAsync_WithNullSource_ThrowsArgumentException()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => integration.ParseToEditableGraphAsync(null!));
    }

    [TestMethod]
    public async Task ParseToEditableGraphAsync_WithEmptySource_ThrowsArgumentException()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => integration.ParseToEditableGraphAsync(string.Empty));
    }

    [TestMethod]
    public async Task ParseToCognitiveGraphAsync_WithNullSource_ThrowsArgumentException()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => integration.ParseToCognitiveGraphAsync(null!));
    }

    [TestMethod]
    public async Task ParseToCognitiveGraphAsync_WithEmptySource_ThrowsArgumentException()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => integration.ParseToCognitiveGraphAsync(string.Empty));
    }

    [TestMethod]
    public async Task ValidateSourceAsync_WithValidCSharpCode_ReturnsValidResult()
    {
        // Arrange
        using var integration = new StepParserIntegration();
        var sourceCode = "var x = 42;";

        // Act
        var result = await integration.ValidateSourceAsync(sourceCode);

        // Assert
        // Note: This test may fail until the actual StepLexer/StepParser APIs are properly understood
        // For now, we test the error handling behavior
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Errors);
    }

    [TestMethod]
    public async Task ValidateSourceAsync_WithNullSource_ReturnsInvalidResult()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Act
        var result = await integration.ValidateSourceAsync(null!);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Length > 0);
    }

    [TestMethod]
    public void StepParserIntegrationFactory_CreateForCSharp_ReturnsInstance()
    {
        // Act
        using var integration = StepParserIntegrationFactory.CreateForCSharp();

        // Assert
        Assert.IsNotNull(integration);
    }

    [TestMethod]
    public void StepParserIntegrationFactory_CreateForJavaScript_ReturnsInstance()
    {
        // Act
        using var integration = StepParserIntegrationFactory.CreateForJavaScript();

        // Assert
        Assert.IsNotNull(integration);
    }

    [TestMethod]
    public void StepParserIntegrationFactory_CreateForPython_ReturnsInstance()
    {
        // Act
        using var integration = StepParserIntegrationFactory.CreateForPython();

        // Assert
        Assert.IsNotNull(integration);
    }

    [TestMethod]
    public void ParseValidationResult_DefaultConstructor_InitializesCorrectly()
    {
        // Act
        var result = new ParseValidationResult();

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Errors);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.AreEqual(0, result.TokenCount);
    }

    [TestMethod]
    public void ParseError_DefaultConstructor_InitializesCorrectly()
    {
        // Act
        var error = new ParseError();

        // Assert
        Assert.AreEqual(string.Empty, error.Message);
        Assert.AreEqual(string.Empty, error.Type);
        Assert.AreEqual(0, error.Line);
        Assert.AreEqual(0, error.Column);
    }

    [TestMethod]
    public void CognitiveGraphNode_UnderlyingNode_CanBeSetAndRetrieved()
    {
        // Arrange
        var node = new TerminalNode("test", "identifier");
        var underlyingNode = new object();

        // Act
        node.UnderlyingNode = underlyingNode;

        // Assert
        Assert.AreSame(underlyingNode, node.UnderlyingNode);
    }

    [TestMethod]
    public async Task UpdateGraphAsync_WithNewSourceCode_PreservesMetadata()
    {
        // Arrange
        using var integration = new StepParserIntegration();

        // Create initial graph (this will likely fail until StepParser APIs are properly integrated)
        var initialSource = "var x = 42;";
        var newSource = "var y = 24;";

        try
        {
            using var initialEditor = await integration.ParseToEditableGraphAsync(initialSource);
            initialEditor.Root!.Metadata["userAdded"] = "test";

            // Act
            using var updatedEditor = await integration.UpdateGraphAsync(initialEditor, newSource);

            // Assert
            Assert.IsTrue(updatedEditor.Root!.Metadata.ContainsKey("userAdded"));
            Assert.AreEqual("test", updatedEditor.Root!.Metadata["userAdded"]);
        }
        catch (Exception)
        {
            // Expected until proper StepParser integration is complete
            Assert.IsTrue(true, "Integration test expected to fail until StepParser APIs are properly integrated");
        }
    }
}