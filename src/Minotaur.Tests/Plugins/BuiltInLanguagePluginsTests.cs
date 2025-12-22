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

using Xunit;
using Minotaur.Plugins;
using Minotaur.Core;

namespace Minotaur.Tests.Plugins;

/// <summary>
/// Tests for built-in language plugins (C#, JavaScript, Python)
/// </summary>
public class BuiltInLanguagePluginsTests
{
    // C# Plugin Tests
    [Fact]
    public void CSharpPlugin_Properties_AreCorrect()
    {
        // Arrange & Act
        var plugin = new CSharpLanguagePlugin();

        // Assert
        Assert.Equal("csharp", plugin.LanguageId);
        Assert.Equal("C#", plugin.DisplayName);
        Assert.Equal(new[] { ".cs", ".csx" }, plugin.SupportedExtensions);
    }

    [Fact]
    public void CSharpPlugin_GetFormattingOptions_ReturnsCorrectDefaults()
    {
        // Arrange
        var plugin = new CSharpLanguagePlugin();

        // Act
        var options = plugin.GetFormattingOptions();

        // Assert
        Assert.Equal("spaces", options.IndentStyle);
        Assert.Equal(4, options.IndentSize);
        Assert.Equal("\r\n", options.LineEnding);
        Assert.True(options.InsertTrailingNewline);
        Assert.Equal(120, options.MaxLineLength);
        Assert.Contains("BraceNewLine", options.CosmeticOptions.Keys);
    }

    [Fact]
    public async Task CSharpPlugin_GenerateCompilerBackendRules_ReturnsRules()
    {
        // Arrange
        var plugin = new CSharpLanguagePlugin();

        // Act
        var rules = await plugin.GenerateCompilerBackendRulesAsync();

        // Assert
        Assert.Equal("csharp", rules.LanguageId);
        Assert.NotEmpty(rules.GenerationRules);
        Assert.NotEmpty(rules.TemplateRules);
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "class_declaration");
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "method_declaration");
        Assert.Contains(rules.TemplateRules, t => t.TemplateName == "namespace_template");
    }

    [Fact]
    public async Task CSharpPlugin_ValidateGraph_NullGraph_ReturnsCannotUnparse()
    {
        // Arrange
        var plugin = new CSharpLanguagePlugin();

        // Act
        var result = await plugin.ValidateGraphForUnparsingAsync(null!);

        // Assert
        Assert.False(result.CanUnparse);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("null"));
    }

    [Fact]
    public async Task CSharpPlugin_ValidateGraph_ValidGraph_ReturnsCanUnparse()
    {
        // Arrange
        var plugin = new CSharpLanguagePlugin();
        var graph = new TerminalNode("test", "identifier");

        // Act
        var result = await plugin.ValidateGraphForUnparsingAsync(graph);

        // Assert
        Assert.True(result.CanUnparse);
        Assert.Empty(result.Errors);
    }

    // JavaScript Plugin Tests
    [Fact]
    public void JavaScriptPlugin_Properties_AreCorrect()
    {
        // Arrange & Act
        var plugin = new JavaScriptLanguagePlugin();

        // Assert
        Assert.Equal("javascript", plugin.LanguageId);
        Assert.Equal("JavaScript", plugin.DisplayName);
        Assert.Equal(new[] { ".js", ".mjs", ".jsx" }, plugin.SupportedExtensions);
    }

    [Fact]
    public void JavaScriptPlugin_GetFormattingOptions_ReturnsCorrectDefaults()
    {
        // Arrange
        var plugin = new JavaScriptLanguagePlugin();

        // Act
        var options = plugin.GetFormattingOptions();

        // Assert
        Assert.Equal("spaces", options.IndentStyle);
        Assert.Equal(2, options.IndentSize);
        Assert.Equal("\n", options.LineEnding);
        Assert.True(options.InsertTrailingNewline);
        Assert.Equal(100, options.MaxLineLength);
        Assert.Contains("QuoteStyle", options.CosmeticOptions.Keys);
    }

    [Fact]
    public async Task JavaScriptPlugin_GenerateCompilerBackendRules_ReturnsRules()
    {
        // Arrange
        var plugin = new JavaScriptLanguagePlugin();

        // Act
        var rules = await plugin.GenerateCompilerBackendRulesAsync();

        // Assert
        Assert.Equal("javascript", rules.LanguageId);
        Assert.NotEmpty(rules.GenerationRules);
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "function_declaration");
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "arrow_function");
    }

    [Fact]
    public async Task JavaScriptPlugin_ValidateGraph_ReturnsCanUnparse()
    {
        // Arrange
        var plugin = new JavaScriptLanguagePlugin();
        var graph = new TerminalNode("test", "identifier");

        // Act
        var result = await plugin.ValidateGraphForUnparsingAsync(graph);

        // Assert
        Assert.True(result.CanUnparse);
    }

    // Python Plugin Tests
    [Fact]
    public void PythonPlugin_Properties_AreCorrect()
    {
        // Arrange & Act
        var plugin = new PythonLanguagePlugin();

        // Assert
        Assert.Equal("python", plugin.LanguageId);
        Assert.Equal("Python", plugin.DisplayName);
        Assert.Equal(new[] { ".py", ".pyw" }, plugin.SupportedExtensions);
    }

    [Fact]
    public void PythonPlugin_GetFormattingOptions_ReturnsCorrectDefaults()
    {
        // Arrange
        var plugin = new PythonLanguagePlugin();

        // Act
        var options = plugin.GetFormattingOptions();

        // Assert
        Assert.Equal("spaces", options.IndentStyle);
        Assert.Equal(4, options.IndentSize);
        Assert.Equal("\n", options.LineEnding);
        Assert.True(options.InsertTrailingNewline);
        Assert.Equal(88, options.MaxLineLength); // Black formatter default
        Assert.Contains("BlackCompatible", options.CosmeticOptions.Keys);
    }

    [Fact]
    public async Task PythonPlugin_GenerateCompilerBackendRules_ReturnsRules()
    {
        // Arrange
        var plugin = new PythonLanguagePlugin();

        // Act
        var rules = await plugin.GenerateCompilerBackendRulesAsync();

        // Assert
        Assert.Equal("python", rules.LanguageId);
        Assert.NotEmpty(rules.GenerationRules);
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "function_def");
        Assert.Contains(rules.GenerationRules, r => r.NodeType == "class_def");
    }

    [Fact]
    public async Task PythonPlugin_ValidateGraph_ReturnsCanUnparse()
    {
        // Arrange
        var plugin = new PythonLanguagePlugin();
        var graph = new TerminalNode("test", "identifier");

        // Act
        var result = await plugin.ValidateGraphForUnparsingAsync(graph);

        // Assert
        Assert.True(result.CanUnparse);
    }

    // Model Tests
    [Fact]
    public void CompilerBackendRules_InitializesWithEmptyCollections()
    {
        // Arrange & Act
        var rules = new CompilerBackendRules();

        // Assert
        Assert.Empty(rules.LanguageId);
        Assert.NotNull(rules.GenerationRules);
        Assert.NotNull(rules.TemplateRules);
        Assert.NotNull(rules.BackendMetadata);
    }

    [Fact]
    public void CodeGenerationRule_PropertiesCanBeSet()
    {
        // Arrange & Act
        var rule = new CodeGenerationRule
        {
            NodeType = "test_node",
            GenerationTemplate = "template {placeholder}",
            GenerationHints = new Dictionary<string, object> { ["hint1"] = "value1" }
        };

        // Assert
        Assert.Equal("test_node", rule.NodeType);
        Assert.Equal("template {placeholder}", rule.GenerationTemplate);
        Assert.Contains("hint1", rule.GenerationHints.Keys);
    }

    [Fact]
    public void TemplateRule_PropertiesCanBeSet()
    {
        // Arrange & Act
        var template = new TemplateRule
        {
            TemplateName = "test_template",
            TemplateContent = "content {param}",
            RequiredParameters = new List<string> { "param" },
            TemplateMetadata = new Dictionary<string, object> { ["meta1"] = "value1" }
        };

        // Assert
        Assert.Equal("test_template", template.TemplateName);
        Assert.Equal("content {param}", template.TemplateContent);
        Assert.Contains("param", template.RequiredParameters);
        Assert.Contains("meta1", template.TemplateMetadata.Keys);
    }

    [Fact]
    public void CodeFormattingOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CodeFormattingOptions();

        // Assert
        Assert.Equal("spaces", options.IndentStyle);
        Assert.Equal(4, options.IndentSize);
        Assert.Equal("\n", options.LineEnding);
        Assert.True(options.InsertTrailingNewline);
        Assert.Equal(120, options.MaxLineLength);
        Assert.NotNull(options.CosmeticOptions);
    }

    [Fact]
    public void UnparseValidationResult_InitializesCorrectly()
    {
        // Arrange & Act
        var result = new UnparseValidationResult
        {
            CanUnparse = true,
            Errors = new List<UnparseValidationError>(),
            Warnings = new List<UnparseValidationWarning>()
        };

        // Assert
        Assert.True(result.CanUnparse);
        Assert.NotNull(result.Errors);
        Assert.NotNull(result.Warnings);
    }

    [Fact]
    public void UnparseValidationError_PropertiesCanBeSet()
    {
        // Arrange & Act
        var error = new UnparseValidationError
        {
            Message = "Test error",
            NodeId = "node_123",
            NodeType = "test_type",
            Severity = "Error"
        };

        // Assert
        Assert.Equal("Test error", error.Message);
        Assert.Equal("node_123", error.NodeId);
        Assert.Equal("test_type", error.NodeType);
        Assert.Equal("Error", error.Severity);
    }

    [Fact]
    public void UnparseValidationWarning_PropertiesCanBeSet()
    {
        // Arrange & Act
        var warning = new UnparseValidationWarning
        {
            Message = "Test warning",
            NodeId = "node_456",
            NodeType = "warning_type"
        };

        // Assert
        Assert.Equal("Test warning", warning.Message);
        Assert.Equal("node_456", warning.NodeId);
        Assert.Equal("warning_type", warning.NodeType);
    }
}
