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
using Minotaur.Core.Models.Grammar;
using Minotaur.Plugins;
using System.Reflection;
using System.Linq;

namespace Minotaur.Parser;

/// <summary>
/// Configuration for the parsing process
/// </summary>
public class ParserConfiguration
{
    /// <summary>
    /// The language to parse (e.g., "csharp", "javascript", "python")
    /// </summary>
    public string Language { get; set; } = "csharp";

    /// <summary>
    /// Whether to include location information in nodes
    /// </summary>
    public bool IncludeLocationInfo { get; set; } = true;

    /// <summary>
    /// Whether to preserve comments during parsing
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Whether to include whitespace tokens
    /// </summary>
    public bool IncludeWhitespace { get; set; } = false;

    /// <summary>
    /// The cognitive graph version to use.
    /// Auto: automatically selects based on project size (small projects use V1, large projects use V2).
    /// V1: optimized for small to medium projects.
    /// V2: optimized for large-scale project analysis (requires CognitiveGraph 1.1.0+).
    /// </summary>
    public CognitiveGraphVersion GraphVersion { get; set; } = CognitiveGraphVersion.Auto;

    /// <summary>
    /// Paths to grammar extension files (<c>.extension</c>) that should be
    /// applied to the base grammar before parsing (Minotaur issue #88).
    /// </summary>
    public List<string> GrammarExtensionPaths { get; set; } = new();

    /// <summary>
    /// The merge strategy used when resolving name collisions between the base
    /// grammar and applied grammar extensions.
    /// </summary>
    public ExtensionMergeStrategy ExtensionMergeStrategy { get; set; } = ExtensionMergeStrategy.Replace;
}

/// <summary>
/// Integrates with DevelApp.StepLexer and DevelApp.StepParser NuGet packages (1.12.0).
/// Provides seamless conversion between source code and cognitive graphs for editing.
/// Uses the RuntimePluggableClassFactory system for extensible language support.
/// </summary>

public partial class StepParserIntegration : IDisposable
{
    private readonly ParserConfiguration _config;
    private readonly LanguagePluginManager _pluginManager;
    private readonly ProjectSizeAnalyzer _sizeAnalyzer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the StepParserIntegration class.
    /// </summary>
    /// <param name="config">The parser configuration to use. If null, uses default configuration.</param>
    /// <param name="pluginManager">The language plugin manager to use. If null, creates a new instance.</param>
    public StepParserIntegration(ParserConfiguration? config = null, LanguagePluginManager? pluginManager = null)
    {
        _config = config ?? new ParserConfiguration();
        _pluginManager = pluginManager ?? new LanguagePluginManager();
        _sizeAnalyzer = new ProjectSizeAnalyzer();
    }

    /// <summary>
    /// Gets the language plugin manager for accessing extensible unparsing support
    /// </summary>
    public LanguagePluginManager PluginManager => _pluginManager;

    /// <summary>
    /// Gets the project size analyzer for determining appropriate cognitive graph version
    /// </summary>
    public ProjectSizeAnalyzer SizeAnalyzer => _sizeAnalyzer;

    /// <summary>
    /// Parses source code and returns the raw cognitive graph without editor wrapper.
    /// Uses DevelApp.StepParser for all parsing operations.
    /// Automatically selects V1 or V2 based on configuration and project size.
    /// </summary>
    public async Task<CognitiveGraphNode> ParseToCognitiveGraphAsync(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        // Determine which version to use
        var effectiveVersion = DetermineGraphVersion(sourceCode);

        // Parse using DevelApp.StepParser with the appropriate version
        return await ParseWithStepParserAsync(sourceCode, effectiveVersion);
    }

    /// <summary>
    /// Determines the cognitive graph version to use based on configuration and source code analysis.
    /// </summary>
    /// <param name="sourceCode">The source code to analyze.</param>
    /// <returns>The cognitive graph version to use (V1 or V2).</returns>
    private CognitiveGraphVersion DetermineGraphVersion(string sourceCode)
    {
        if (_config.GraphVersion == CognitiveGraphVersion.Auto)
        {
            // Automatically determine based on project size
            return _sizeAnalyzer.GetRecommendedVersion(sourceCode);
        }

        return _config.GraphVersion;
    }

    /// <summary>
    /// Validates that source code can be parsed without errors using DevelApp.StepParser
    /// </summary>
    public async Task<ParseValidationResult> ValidateSourceAsync(string sourceCode)
    {
        try
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                return new ParseValidationResult
                {
                    IsValid = false,
                    Errors = new[] { new ParseError { Message = "Source code cannot be null or empty", Type = "ArgumentError" } },
                    TokenCount = 0
                };
            }

            // Use DevelApp.StepParser for validation - plugins are NOT used for parsing
            return await ValidateWithStepParserAsync(sourceCode);
        }
        catch (Exception ex)
        {
            return new ParseValidationResult
            {
                IsValid = false,
                Errors = new[] { new ParseError { Message = ex.Message, Type = "ParseException" } },
                TokenCount = 0
            };
        }
    }
    /// <summary>
    /// Releases all resources used by the StepParserIntegration and disposes of the plugin manager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _pluginManager?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Result of source code validation
/// </summary>

public class ParseValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the source code is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the array of parse errors found during validation.
    /// </summary>
    public ParseError[] Errors { get; set; } = Array.Empty<ParseError>();

    /// <summary>
    /// Gets or sets the number of tokens found in the source code.
    /// </summary>
    public int TokenCount { get; set; }
}

/// <summary>
/// Represents a parsing error
/// </summary>
public class ParseError
{
    /// <summary>
    /// Gets or sets the error message describing the parsing issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of parse error (e.g., "SyntaxError", "UnexpectedToken").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the error occurred.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number where the error occurred.
    /// </summary>
    public int Column { get; set; }
}

/// <summary>
/// Factory methods for creating commonly used integrations with plugin support
/// </summary>
public static class StepParserIntegrationFactory
{
    /// <summary>
    /// Creates a StepParserIntegration instance configured for C# parsing.
    /// </summary>
    /// <param name="pluginManager">Optional language plugin manager. If null, creates a new instance.</param>
    /// <returns>A configured StepParserIntegration for C# language parsing.</returns>
    public static StepParserIntegration CreateForCSharp(LanguagePluginManager? pluginManager = null)
    {
        return new StepParserIntegration(new ParserConfiguration
        {
            Language = "csharp",
            IncludeLocationInfo = true,
            PreserveComments = true
        }, pluginManager);
    }

    /// <summary>
    /// Creates a StepParserIntegration instance configured for JavaScript parsing.
    /// </summary>
    /// <param name="pluginManager">Optional language plugin manager. If null, creates a new instance.</param>
    /// <returns>A configured StepParserIntegration for JavaScript language parsing.</returns>
    public static StepParserIntegration CreateForJavaScript(LanguagePluginManager? pluginManager = null)
    {
        return new StepParserIntegration(new ParserConfiguration
        {
            Language = "javascript",
            IncludeLocationInfo = true,
            PreserveComments = true
        }, pluginManager);
    }

    /// <summary>
    /// Creates a StepParserIntegration instance configured for Python parsing.
    /// </summary>
    /// <param name="pluginManager">Optional language plugin manager. If null, creates a new instance.</param>
    /// <returns>A configured StepParserIntegration for Python language parsing.</returns>
    public static StepParserIntegration CreateForPython(LanguagePluginManager? pluginManager = null)
    {
        return new StepParserIntegration(new ParserConfiguration
        {
            Language = "python",
            IncludeLocationInfo = true,
            PreserveComments = true
        }, pluginManager);
    }

    /// <summary>
    /// Creates an integration for a specific file extension using plugin auto-detection
    /// </summary>
    public static StepParserIntegration CreateForFile(string filePath, LanguagePluginManager? pluginManager = null)
    {
        var manager = pluginManager ?? new LanguagePluginManager();
        var extension = Path.GetExtension(filePath);
        var plugin = manager.GetPluginByExtension(extension);

        if (plugin != null)
        {
            return new StepParserIntegration(new ParserConfiguration
            {
                Language = plugin.LanguageId,
                IncludeLocationInfo = true,
                PreserveComments = true
            }, manager);
        }

        // Default to C# if no plugin is found
        return CreateForCSharp(manager);
    }
}

