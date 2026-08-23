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

using Minotaur.Core;
using Minotaur.Plugins.Java;
using Xunit;

namespace Minotaur.Tests.Plugins.Java;

/// <summary>
/// Unit tests for the Java language plugin.
/// </summary>
public class JavaLanguagePluginTests
{
    private readonly JavaLanguagePlugin _plugin = new();

    [Fact]
    public void LanguageId_ShouldReturnJava()
    {
        Assert.Equal("java", _plugin.LanguageId);
    }

    [Fact]
    public void DisplayName_ShouldReturnJava()
    {
        Assert.Equal("Java", _plugin.DisplayName);
    }

    [Fact]
    public void SupportedExtensions_ShouldReturnJavaExtensions()
    {
        var extensions = _plugin.SupportedExtensions;
        Assert.Contains(".java", extensions);
        Assert.Single(extensions);
    }

    [Fact]
    public async Task GenerateCompilerBackendRulesAsync_ShouldReturnJavaRules()
    {
        var rules = await _plugin.GenerateCompilerBackendRulesAsync();
        
        Assert.NotNull(rules);
        Assert.Equal("java", rules.LanguageId);
        Assert.NotEmpty(rules.GenerationRules);
        
        // Check for key Java rules
        var ruleTypes = rules.GenerationRules.Select(r => r.NodeType).ToList();
        Assert.Contains("class_declaration", ruleTypes);
        Assert.Contains("interface_declaration", ruleTypes);
        Assert.Contains("method_declaration", ruleTypes);
    }

    [Fact]
    public void GetFormattingOptions_ShouldReturnJavaOptions()
    {
        var options = _plugin.GetFormattingOptions();
        
        Assert.NotNull(options);
        Assert.Equal("spaces", options.IndentStyle);
        Assert.Equal(4, options.IndentSize);
        Assert.Equal("\n", options.LineEnding);
        Assert.True(options.InsertTrailingNewline);
    }

    [Fact]
    public async Task ValidateGraphForUnparsingAsync_NullGraph_ShouldReturnError()
    {
        var result = await _plugin.ValidateGraphForUnparsingAsync(null!);
        
        Assert.False(result.CanUnparse);
        Assert.Contains(result.Errors, e => e.NodeId == "null");
    }

    [Fact]
    public void AnalyzeSymbolic_ShouldReturnResults()
    {
        var sourceCode = "public class Test { public void method() { } } ";
        var constraints = new List<SymbolicConstraint>();
        
        var errors = _plugin.AnalyzeSymbolic(sourceCode, constraints);
        
        Assert.NotNull(errors);
    }

    [Fact]
    public void GetErrorPatterns_ShouldReturnPatterns()
    {
        var patterns = _plugin.GetErrorPatterns();
        
        Assert.NotNull(patterns);
    }

    [Fact]
    public void GetErrorConfidence_ShouldReturnValue()
    {
        var confidence = _plugin.GetErrorConfidence(Analysis.Symbolic.SymbolicErrorType.NullDereference);
        
        Assert.True(confidence >= 0.0 && confidence <= 1.0);
    }

    [Fact]
    public void GenerateTestCases_ShouldReturnTestCases()
    {
        var error = new Analysis.Symbolic.SymbolicError
        {
            ErrorType = Analysis.Symbolic.SymbolicErrorType.NullDereference,
            Message = "Test error"
        };
        
        var testCases = _plugin.GenerateTestCases(error, "test code");
        
        Assert.NotNull(testCases);
    }
}

/// <summary>
/// Unit tests for the Java unparse visitor.
/// </summary>
public class JavaUnparseVisitorTests
{
    [Fact]
    public void GetGeneratedCode_EmptyVisitor_ShouldReturnEmptyString()
    {
        var visitor = new JavaUnparseVisitor();
        var code = visitor.GetGeneratedCode();
        
        Assert.Equal(string.Empty, code);
    }
}

/// <summary>
/// Unit tests for the Java unparse validator.
/// </summary>
public class JavaUnparseValidatorTests
{
    private readonly JavaUnparseValidator _validator = new();

    [Fact]
    public void Validate_NullGraph_ShouldReturnError()
    {
        var errors = _validator.Validate(null!);
        
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.NodeId == "null");
    }
}
