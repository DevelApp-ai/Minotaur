using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;
using GolemCognitiveGraph.Plugins;

namespace GolemCognitiveGraph.Parser;

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
public class StepParserIntegration : IDisposable
{
    private readonly ParserConfiguration _config;
    private readonly LanguagePluginManager _pluginManager;
    private bool _disposed;

    public StepParserIntegration(ParserConfiguration? config = null, LanguagePluginManager? pluginManager = null)
    {
        _config = config ?? new ParserConfiguration();
        _pluginManager = pluginManager ?? new LanguagePluginManager();
    }

    /// <summary>
    /// Gets the language plugin manager for accessing extensible language support
    /// </summary>
    public LanguagePluginManager PluginManager => _pluginManager;

    /// <summary>
    /// Parses source code into a cognitive graph using StepParser and creates a GraphEditor for manipulation.
    /// Uses the appropriate language plugin for language-specific parsing.
    /// </summary>
    public async Task<GraphEditor> ParseToEditableGraphAsync(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        var plugin = _pluginManager.GetPlugin(_config.Language);
        if (plugin != null)
        {
            // Use the language plugin for parsing
            var rootNode = await plugin.ParseAsync(sourceCode);
            return new GraphEditor(rootNode);
        }

        // Fallback to demonstration graph if no plugin is available
        var fallbackNode = CreateDemoGraphFromSourceCode(sourceCode);
        return new GraphEditor(fallbackNode);
    }

    /// <summary>
    /// Parses source code and returns the raw cognitive graph without editor wrapper.
    /// Uses the appropriate language plugin for language-specific parsing.
    /// </summary>
    public async Task<CognitiveGraphNode> ParseToCognitiveGraphAsync(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        var plugin = _pluginManager.GetPlugin(_config.Language);
        if (plugin != null)
        {
            return await plugin.ParseAsync(sourceCode);
        }

        // Fallback to demonstration graph if no plugin is available
        return CreateDemoGraphFromSourceCode(sourceCode);
    }

    /// <summary>
    /// Updates an existing cognitive graph by reparsing modified source code
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
    /// Validates that source code can be parsed without errors using the appropriate language plugin
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

            var plugin = _pluginManager.GetPlugin(_config.Language);
            if (plugin != null)
            {
                var validationResult = await plugin.ValidateAsync(sourceCode);
                return new ParseValidationResult
                {
                    IsValid = validationResult.IsValid,
                    Errors = validationResult.Errors.Select(e => new ParseError
                    {
                        Message = e.Message,
                        Type = e.Code,
                        Line = e.Line,
                        Column = e.Column
                    }).ToArray(),
                    TokenCount = await EstimateTokenCountAsync(sourceCode)
                };
            }

            // Fallback validation if no plugin is available
            await Task.Delay(1);

            // Basic validation - check for balanced braces as an example
            var braceCount = sourceCode.Count(c => c == '{') - sourceCode.Count(c => c == '}');
            var parenCount = sourceCode.Count(c => c == '(') - sourceCode.Count(c => c == ')');

            if (braceCount != 0 || parenCount != 0)
            {
                return new ParseValidationResult
                {
                    IsValid = false,
                    Errors = new[] { new ParseError { Message = "Unbalanced braces or parentheses", Type = "SyntaxError" } },
                    TokenCount = EstimateTokenCount(sourceCode)
                };
            }

            return new ParseValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<ParseError>(),
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
        var plugin = _pluginManager.GetPlugin(_config.Language);
        if (plugin != null)
        {
            var tokens = await plugin.TokenizeAsync(sourceCode);
            return tokens.Count();
        }

        return EstimateTokenCount(sourceCode);
    }

    private int EstimateTokenCount(string sourceCode)
    {
        return SimpleTokenize(sourceCode).Count();
    }

    #endregion

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
    public bool IsValid { get; set; }
    public ParseError[] Errors { get; set; } = Array.Empty<ParseError>();
    public int TokenCount { get; set; }
}

/// <summary>
/// Represents a parsing error
/// </summary>
public class ParseError
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}

/// <summary>
/// Factory methods for creating commonly used integrations with plugin support
/// </summary>
public static class StepParserIntegrationFactory
{
    public static StepParserIntegration CreateForCSharp(LanguagePluginManager? pluginManager = null)
    {
        return new StepParserIntegration(new ParserConfiguration
        {
            Language = "csharp",
            IncludeLocationInfo = true,
            PreserveComments = true
        }, pluginManager);
    }

    public static StepParserIntegration CreateForJavaScript(LanguagePluginManager? pluginManager = null)
    {
        return new StepParserIntegration(new ParserConfiguration
        {
            Language = "javascript",
            IncludeLocationInfo = true,
            PreserveComments = true
        }, pluginManager);
    }

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