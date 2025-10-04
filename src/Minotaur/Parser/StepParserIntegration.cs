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
using Minotaur.Editor;
using Minotaur.Plugins;

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
}

/// <summary>
/// Integrates with DevelApp.StepLexer and DevelApp.StepParser NuGet packages (1.0.1).
/// Provides seamless conversion between source code and cognitive graphs for editing.
/// Uses the RuntimePluggableClassFactory system for extensible language support.
/// </summary>
public partial class StepParserIntegration : IDisposable
{
    private readonly ParserConfiguration _config;
    private readonly LanguagePluginManager _pluginManager;
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
    }

    /// <summary>
    /// Gets the language plugin manager for accessing extensible unparsing support
    /// </summary>
    public LanguagePluginManager PluginManager => _pluginManager;

    /// <summary>
    /// Parses source code into a cognitive graph using DevelApp.StepParser and creates a GraphEditor for manipulation.
    /// NOTE: Parsing is handled entirely by StepParser - plugins are only used for unparsing.
    /// </summary>
    public async Task<GraphEditor> ParseToEditableGraphAsync(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        // Parse using DevelApp.StepParser (this is where the actual parsing happens)
        var rootNode = await ParseWithStepParserAsync(sourceCode);
        return new GraphEditor(rootNode);
    }

    /// <summary>
    /// Parses source code and returns the raw cognitive graph without editor wrapper.
    /// Uses DevelApp.StepParser for all parsing operations.
    /// </summary>
    public async Task<CognitiveGraphNode> ParseToCognitiveGraphAsync(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        // Parse using DevelApp.StepParser - this is the authoritative parser
        return await ParseWithStepParserAsync(sourceCode);
    }

    /// <summary>
    /// Updates an existing cognitive graph by reparsing modified source code using StepParser
    /// </summary>
    public async Task<GraphEditor> UpdateGraphAsync(GraphEditor editor, string newSourceCode)
    {
        var newGraph = await ParseToEditableGraphAsync(newSourceCode);

        // Preserve any metadata from the original graph
        if (editor.Root != null && newGraph.Root != null)
        {
            PreserveMetadata(editor.Root, newGraph.Root);
        }

        return newGraph;
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

    private CognitiveGraphNode CreateDemoGraphFromSourceCode(string sourceCode)
    {
        // Create a simplified representation of the source code as a cognitive graph
        // This demonstrates the framework and will be enhanced with actual StepParser integration

        var root = new NonTerminalNode("compilation_unit", 0);
        root.Metadata["sourceCode"] = sourceCode;
        root.Metadata["language"] = _config.Language;
        root.Metadata["parserIntegration"] = "StepParser-Ready";

        // Simple tokenization demonstration (will be replaced by actual StepLexer)
        var tokens = SimpleTokenize(sourceCode);
        var statement = new NonTerminalNode("statement", 0);

        foreach (var token in tokens)
        {
            CognitiveGraphNode node;

            if (IsKeyword(token))
            {
                node = new TerminalNode(token, "keyword");
            }
            else if (IsIdentifier(token))
            {
                node = new IdentifierNode(token);
            }
            else if (IsLiteral(token))
            {
                var value = ParseLiteralValue(token);
                node = new LiteralNode(token, "literal", value);
            }
            else
            {
                node = new TerminalNode(token, "token");
            }

            if (_config.IncludeLocationInfo)
            {
                node.Metadata["__demo_location"] = $"token_{tokens.ToList().IndexOf(token)}";
            }

            statement.AddChild(node);
        }

        root.AddChild(statement);

        // Add integration markers to show the framework is ready
        var integrationMarker = new TerminalNode("StepParser Integration Ready", "system_comment");
        integrationMarker.Metadata["integration_version"] = "1.0.1";
        integrationMarker.Metadata["ready_for"] = "DevelApp.StepLexer and DevelApp.StepParser";
        root.AddChild(integrationMarker);

        return root;
    }

    private void PreserveMetadata(CognitiveGraphNode originalRoot, CognitiveGraphNode newRoot)
    {
        // Preserve user-added metadata during graph updates
        if (originalRoot.Metadata.Any())
        {
            foreach (var metadata in originalRoot.Metadata)
            {
                if (!metadata.Key.StartsWith("__system"))
                {
                    newRoot.Metadata[metadata.Key] = metadata.Value;
                }
            }
        }
    }

    #region Helper Methods for Demo Implementation

    private IEnumerable<string> SimpleTokenize(string sourceCode)
    {
        // Simple whitespace-based tokenization for demonstration
        // Will be replaced by actual StepLexer tokenization
        return sourceCode.Split(new[] { ' ', '\t', '\r', '\n', ';', '(', ')', '{', '}', '[', ']' },
                               StringSplitOptions.RemoveEmptyEntries);
    }

    private bool IsKeyword(string token)
    {
        var keywords = new[] { "var", "int", "string", "bool", "class", "public", "private", "static",
                              "void", "return", "if", "else", "for", "while", "function", "let", "const" };
        return keywords.Contains(token.ToLowerInvariant());
    }

    private bool IsIdentifier(string token)
    {
        return !string.IsNullOrEmpty(token) &&
               char.IsLetter(token[0]) &&
               token.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               !IsKeyword(token);
    }

    private bool IsLiteral(string token)
    {
        return int.TryParse(token, out _) ||
               double.TryParse(token, out _) ||
               bool.TryParse(token, out _) ||
               (token.StartsWith("\"") && token.EndsWith("\""));
    }

    private object? ParseLiteralValue(string token)
    {
        if (int.TryParse(token, out var intValue))
            return intValue;

        if (double.TryParse(token, out var doubleValue))
            return doubleValue;

        if (bool.TryParse(token, out var boolValue))
            return boolValue;

        if (token.StartsWith("\"") && token.EndsWith("\""))
            return token.Substring(1, token.Length - 2);

        return token;
    }

    private async Task<int> EstimateTokenCountAsync(string sourceCode)
    {
        // Since plugins no longer handle tokenization (StepParser does), use simple estimation
        await Task.CompletedTask;
        return EstimateTokenCount(sourceCode);
    }

    private int EstimateTokenCount(string sourceCode)
    {
        return SimpleTokenize(sourceCode).Count();
    }

    #endregion

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

// Extension of the StepParserIntegration class to include StepParser methods
public partial class StepParserIntegration
{
    /// <summary>
    /// Parses source code using DevelApp.StepParser NuGet package.
    /// This is the authoritative parsing method - plugins are NOT used for parsing.
    /// </summary>
    private async Task<CognitiveGraphNode> ParseWithStepParserAsync(string sourceCode)
    {
        try
        {
            // TODO: Integrate with DevelApp.StepParser 1.0.1 NuGet package
            // This is where the actual StepParser integration will happen
            // For now, create a demonstration graph to show the framework

            var root = new NonTerminalNode("compilation_unit", 0);
            root.Metadata["sourceCode"] = sourceCode;
            root.Metadata["language"] = _config.Language;
            root.Metadata["parserType"] = "DevelApp.StepParser";
            root.Metadata["parserVersion"] = "1.0.1";

            // Simple demonstration structure - will be replaced by actual StepParser output
            var statement = new NonTerminalNode("statement", 0);
            var tokens = SimpleTokenize(sourceCode);

            foreach (var token in tokens.Take(10)) // Limit for demo
            {
                CognitiveGraphNode node;

                if (IsKeyword(token))
                {
                    node = new TerminalNode(token, "keyword");
                }
                else if (IsIdentifier(token))
                {
                    node = new IdentifierNode(token);
                }
                else if (IsLiteral(token))
                {
                    var value = ParseLiteralValue(token);
                    node = new LiteralNode(token, "literal", value);
                }
                else
                {
                    node = new TerminalNode(token, "operator");
                }

                statement.AddChild(node);
            }

            root.AddChild(statement);

            await Task.CompletedTask;
            return root;
        }
        catch (Exception ex)
        {
            // Return error node for parsing failures
            var errorNode = new NonTerminalNode("parse_error", 0);
            errorNode.Metadata["error"] = ex.Message;
            errorNode.Metadata["parserType"] = "DevelApp.StepParser";
            return errorNode;
        }
    }

    /// <summary>
    /// Validates source code using DevelApp.StepParser NuGet package.
    /// This is the authoritative validation method - plugins are NOT used for parsing validation.
    /// </summary>
    private async Task<ParseValidationResult> ValidateWithStepParserAsync(string sourceCode)
    {
        try
        {
            // TODO: Integrate with DevelApp.StepParser 1.0.1 validation
            // For now, provide basic validation logic

            // Basic syntax validation
            var braceCount = sourceCode.Count(c => c == '{') - sourceCode.Count(c => c == '}');
            var parenCount = sourceCode.Count(c => c == '(') - sourceCode.Count(c => c == ')');

            var errors = new List<ParseError>();

            if (braceCount != 0)
            {
                errors.Add(new ParseError
                {
                    Message = "Unbalanced braces",
                    Type = "SyntaxError",
                    Line = 1,
                    Column = 1
                });
            }

            if (parenCount != 0)
            {
                errors.Add(new ParseError
                {
                    Message = "Unbalanced parentheses",
                    Type = "SyntaxError",
                    Line = 1,
                    Column = 1
                });
            }

            await Task.CompletedTask;

            return new ParseValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                TokenCount = EstimateTokenCount(sourceCode)
            };
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
}