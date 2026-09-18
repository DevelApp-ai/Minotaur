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

using Minotaur.Core;
using Minotaur.Analysis.Symbolic;

namespace Minotaur.Plugins;

public class JavaLanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.Java.JavaLanguagePlugin _javaPlugin = new();

    public string LanguageId => _javaPlugin.LanguageId;
    public string DisplayName => _javaPlugin.DisplayName;
    public string[] SupportedExtensions => _javaPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _javaPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _javaPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _javaPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _javaPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _javaPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _javaPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _javaPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _javaPlugin.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in TypeScript language plugin for unparsing and compiler backend generation
/// </summary>
public class TypeScriptLanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.TypeScript.TypeScriptLanguagePlugin _tsPlugin = new();

    public string LanguageId => _tsPlugin.LanguageId;
    public string DisplayName => _tsPlugin.DisplayName;
    public string[] SupportedExtensions => _tsPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _tsPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _tsPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _tsPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _tsPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _tsPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _tsPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _tsPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _tsPlugin.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in COBOL language plugin wrapper for unparsing and compiler backend generation
/// </summary>
public class COBOLLanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.COBOL.COBOLLanguagePlugin _cobolPlugin = new();

    public string LanguageId => _cobolPlugin.LanguageId;
    public string DisplayName => _cobolPlugin.DisplayName;
    public string[] SupportedExtensions => _cobolPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _cobolPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _cobolPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _cobolPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _cobolPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _cobolPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _cobolPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _cobolPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _cobolPlugin.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in PL/I language plugin wrapper for unparsing and compiler backend generation
/// </summary>
public class PLILanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.PLI.PLILanguagePlugin _pliPlugin = new();

    public string LanguageId => _pliPlugin.LanguageId;
    public string DisplayName => _pliPlugin.DisplayName;
    public string[] SupportedExtensions => _pliPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _pliPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _pliPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _pliPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _pliPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _pliPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _pliPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _pliPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _pliPlugin.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in Rust language plugin wrapper for unparsing and compiler backend generation
/// </summary>
public class RustLanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.Rust.RustLanguagePlugin _rustPlugin = new();

    public string LanguageId => _rustPlugin.LanguageId;
    public string DisplayName => _rustPlugin.DisplayName;
    public string[] SupportedExtensions => _rustPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _rustPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _rustPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _rustPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _rustPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _rustPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _rustPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _rustPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _rustPlugin.GenerateTestCases(error, sourceCode);
    }
}

/// <summary>
/// Built-in Go language plugin wrapper for unparsing and compiler backend generation
/// </summary>
public class GoLanguagePluginWrapper : ILanguagePlugin, ISymbolicAnalysisPlugin
{
    private readonly Plugins.Go.GoLanguagePlugin _goPlugin = new();

    public string LanguageId => _goPlugin.LanguageId;
    public string DisplayName => _goPlugin.DisplayName;
    public string[] SupportedExtensions => _goPlugin.SupportedExtensions;

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        return await _goPlugin.UnparseAsync(graph);
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        return await _goPlugin.GenerateCompilerBackendRulesAsync();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return _goPlugin.GetFormattingOptions();
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        return await _goPlugin.ValidateGraphForUnparsingAsync(graph);
    }

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        return _goPlugin.AnalyzeSymbolic(sourceCode, constraints);
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return _goPlugin.GetErrorPatterns();
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return _goPlugin.GetErrorConfidence(errorType);
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        return _goPlugin.GenerateTestCases(error, sourceCode);
    }
}
