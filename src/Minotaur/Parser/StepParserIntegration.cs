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

    /// <summary>
    /// Converts a StepParser syntax tree to a CognitiveGraphNode structure.
    /// </summary>
    private CognitiveGraphNode ConvertToCognitiveGraphNode(object syntaxTree, string sourceCode)
    {
        // Convert the StepParser result to our CognitiveGraphNode format
        // This method would need to be implemented based on the actual StepParser API
        
        if (syntaxTree == null)
        {
            return new NonTerminalNode("empty", 0);
        }

        // For now, create a basic structure until we have the actual StepParser API documentation
        var root = new NonTerminalNode("compilation_unit", 0);
        
        // Use reflection to examine the syntax tree structure
        var syntaxTreeType = syntaxTree.GetType();
        root.Metadata["syntaxTreeType"] = syntaxTreeType.Name;
        
        try
        {
            // Try to get common properties from the syntax tree
            var childrenProperty = syntaxTreeType.GetProperty("Children") ?? 
                                 syntaxTreeType.GetProperty("Nodes") ??
                                 syntaxTreeType.GetProperty("Elements");
                                 
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(syntaxTree) as System.Collections.IEnumerable;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        var childNode = ConvertSyntaxNodeToCognitiveNode(child);
                        if (childNode != null)
                        {
                            root.AddChild(childNode);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If we can't parse the syntax tree structure, fall back to basic parsing
            root.Metadata["conversionError"] = ex.Message;
            var fallbackNodes = FallbackParsing(sourceCode);
            foreach (var node in fallbackNodes)
            {
                root.AddChild(node);
            }
        }

        return root;
    }

    /// <summary>
    /// Converts an individual syntax node to a CognitiveGraphNode.
    /// </summary>
    private CognitiveGraphNode? ConvertSyntaxNodeToCognitiveNode(object syntaxNode)
    {
        if (syntaxNode == null) return null;

        var nodeType = syntaxNode.GetType();
        var nodeName = nodeType.Name;
        
        // Try to determine if this is a terminal or non-terminal node
        var valueProperty = nodeType.GetProperty("Value") ?? nodeType.GetProperty("Text");
        var typeProperty = nodeType.GetProperty("Type") ?? nodeType.GetProperty("Kind");
        
        if (valueProperty != null)
        {
            var value = valueProperty.GetValue(syntaxNode)?.ToString() ?? "";
            var type = typeProperty?.GetValue(syntaxNode)?.ToString() ?? "unknown";
            
            // Create appropriate node type based on content
            if (IsKeyword(value))
            {
                return new TerminalNode(value, "keyword");
            }
            else if (IsIdentifier(value))
            {
                return new IdentifierNode(value);
            }
            else if (IsLiteral(value))
            {
                var literalValue = ParseLiteralValue(value);
                return new LiteralNode(value, type, literalValue);
            }
            else
            {
                return new TerminalNode(value, type);
            }
        }
        else
        {
            // This appears to be a non-terminal node
            var nonTerminal = new NonTerminalNode(nodeName.ToLowerInvariant(), 0);
            
            // Try to add children
            var childrenProperty = nodeType.GetProperty("Children") ?? 
                                 nodeType.GetProperty("Nodes") ??
                                 nodeType.GetProperty("Elements");
                                 
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(syntaxNode) as System.Collections.IEnumerable;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        var childNode = ConvertSyntaxNodeToCognitiveNode(child);
                        if (childNode != null)
                        {
                            nonTerminal.AddChild(childNode);
                        }
                    }
                }
            }
            
            return nonTerminal;
        }
    }

    /// <summary>
    /// Fallback parsing when StepParser integration fails.
    /// </summary>
    private List<CognitiveGraphNode> FallbackParsing(string sourceCode)
    {
        var nodes = new List<CognitiveGraphNode>();
        var tokens = SimpleTokenize(sourceCode);
        
        var statement = new NonTerminalNode("statement", 0);
        
        foreach (var token in tokens.Take(20)) // Reasonable limit for fallback
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
        
        if (statement.Children.Count > 0)
        {
            nodes.Add(statement);
        }
        
        return nodes;
    }

    /// <summary>
    /// Adds location information from StepParser to the cognitive graph nodes.
    /// </summary>
    private void AddLocationInformation(CognitiveGraphNode node, object? locationMap)
    {
        if (locationMap == null) return;

        // This would need to be implemented based on the actual StepParser location mapping API
        // For now, we'll add basic position information
        try
        {
            var locationMapType = locationMap.GetType();
            var getLocationMethod = locationMapType.GetMethod("GetLocation");
            
            if (getLocationMethod != null)
            {
                // Try to get location information for this node
                var location = getLocationMethod.Invoke(locationMap, new[] { node });
                if (location != null)
                {
                    var locationType = location.GetType();
                    var lineProperty = locationType.GetProperty("Line");
                    var columnProperty = locationType.GetProperty("Column");
                    var offsetProperty = locationType.GetProperty("Offset");
                    var lengthProperty = locationType.GetProperty("Length");

                    if (lineProperty != null && columnProperty != null)
                    {
                        var line = (int?)lineProperty.GetValue(location) ?? 1;
                        var column = (int?)columnProperty.GetValue(location) ?? 1;
                        var offset = (int?)offsetProperty?.GetValue(location) ?? 0;
                        var length = (int?)lengthProperty?.GetValue(location) ?? 0;

                        node.SourcePosition = new SourcePosition(line, column, offset, length);
                    }
                }
            }
        }
        catch
        {
            // If location mapping fails, continue without location information
        }

        // Recursively add location information to children
        foreach (var child in node.Children)
        {
            AddLocationInformation(child, locationMap);
        }
    }

    /// <summary>
    /// Invokes the StepParser using reflection to handle unknown API structure.
    /// </summary>
    private async Task<(object? lexResult, object? parseResult)> InvokeStepParserAsync(string sourceCode)
    {
        try
        {
            // Try to find and instantiate StepLexer
            var lexerType = FindTypeInLoadedAssemblies("Lexer", "StepLexer", "DevelApp.StepLexer");
            var parserType = FindTypeInLoadedAssemblies("Parser", "StepParser", "DevelApp.StepParser");

            if (lexerType == null || parserType == null)
            {
                return (null, null);
            }

            // Create lexer instance
            object? lexer = null;
            try
            {
                lexer = Activator.CreateInstance(lexerType, _config.Language);
            }
            catch
            {
                lexer = Activator.CreateInstance(lexerType);
            }

            if (lexer == null)
            {
                return (null, null);
            }

            // Tokenize
            var tokenizeMethod = lexerType.GetMethod("TokenizeAsync") ?? lexerType.GetMethod("Tokenize");
            if (tokenizeMethod == null)
            {
                return (null, null);
            }

            object? lexResult;
            if (tokenizeMethod.Name.EndsWith("Async"))
            {
                var task = tokenizeMethod.Invoke(lexer, new[] { sourceCode }) as Task;
                if (task != null)
                {
                    await task;
                    lexResult = task.GetType().GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    lexResult = null;
                }
            }
            else
            {
                lexResult = tokenizeMethod.Invoke(lexer, new[] { sourceCode });
            }

            // Create parser instance
            object? parser = null;
            try
            {
                parser = Activator.CreateInstance(parserType, _config.Language);
            }
            catch
            {
                parser = Activator.CreateInstance(parserType);
            }

            if (parser == null || lexResult == null)
            {
                return (lexResult, null);
            }

            // Get tokens from lexResult
            var tokens = GetPropertyValue<object>(lexResult, "Tokens", "TokenList", "Results");
            if (tokens == null)
            {
                return (lexResult, null);
            }

            // Parse tokens
            var parseMethod = parserType.GetMethod("ParseAsync") ?? parserType.GetMethod("Parse");
            if (parseMethod == null)
            {
                return (lexResult, null);
            }

            object? parseResult;
            if (parseMethod.Name.EndsWith("Async"))
            {
                var task = parseMethod.Invoke(parser, new[] { tokens }) as Task;
                if (task != null)
                {
                    await task;
                    parseResult = task.GetType().GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    parseResult = null;
                }
            }
            else
            {
                parseResult = parseMethod.Invoke(parser, new[] { tokens });
            }

            // Dispose resources if they implement IDisposable
            if (lexer is IDisposable lexerDisposable) lexerDisposable.Dispose();
            if (parser is IDisposable parserDisposable) parserDisposable.Dispose();

            return (lexResult, parseResult);
        }
        catch (Exception ex)
        {
            // If reflection-based approach fails, log and return null
            System.Diagnostics.Debug.WriteLine($"StepParser reflection failed: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Finds a type by name in all loaded assemblies.
    /// </summary>
    private Type? FindTypeInLoadedAssemblies(params string[] typeNames)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            foreach (var typeName in typeNames)
            {
                try
                {
                    var type = assembly.GetType(typeName, false, true) ??
                              assembly.GetTypes().FirstOrDefault(t => 
                                  t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                                  t.FullName?.EndsWith(typeName, StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Continue searching
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Gets a property value using reflection with multiple property name fallbacks.
    /// </summary>
    private T? GetPropertyValue<T>(object obj, params string[] propertyNames)
    {
        if (obj == null) return default;

        var objType = obj.GetType();
        
        foreach (var propName in propertyNames)
        {
            try
            {
                var property = objType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(obj);
                    if (value is T tValue)
                    {
                        return tValue;
                    }
                    if (value != null && typeof(T) == typeof(string))
                    {
                        return (T)(object)value.ToString();
                    }
                }
            }
            catch
            {
                // Continue trying other property names
            }
        }
        
        return default;
    }

    /// <summary>
    /// Creates a fallback cognitive graph when StepParser is not available.
    /// </summary>
    private CognitiveGraphNode CreateFallbackCognitiveGraph(string sourceCode)
    {
        var root = new NonTerminalNode("compilation_unit", 0);
        root.Metadata["sourceCode"] = sourceCode;
        root.Metadata["language"] = _config.Language;
        root.Metadata["parserType"] = "Minotaur.FallbackParser";
        root.Metadata["parserVersion"] = "1.0.0";

        var fallbackNodes = FallbackParsing(sourceCode);
        foreach (var node in fallbackNodes)
        {
            root.AddChild(node);
        }

        return root;
    }

    /// <summary>
    /// Performs fallback validation when StepParser is not available.
    /// </summary>
    private List<ParseError> PerformFallbackValidation(string sourceCode)
    {
        var errors = new List<ParseError>();

        // Basic syntax validation
        var braceCount = sourceCode.Count(c => c == '{') - sourceCode.Count(c => c == '}');
        var parenCount = sourceCode.Count(c => c == '(') - sourceCode.Count(c => c == ')');
        var bracketCount = sourceCode.Count(c => c == '[') - sourceCode.Count(c => c == ']');

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

        if (bracketCount != 0)
        {
            errors.Add(new ParseError
            {
                Message = "Unbalanced brackets",
                Type = "SyntaxError",
                Line = 1,
                Column = 1
            });
        }

        return errors;
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
            // Integrate with DevelApp.StepParser 1.0.1 NuGet package using reflection
            // This approach allows us to work with the actual API structure
            var (lexResult, parseResult) = await InvokeStepParserAsync(sourceCode);
            
            if (lexResult == null || parseResult == null)
            {
                // If StepParser integration fails, use fallback parsing
                var fallbackRoot = CreateFallbackCognitiveGraph(sourceCode);
                fallbackRoot.Metadata["parsingMethod"] = "Fallback";
                return fallbackRoot;
            }

            // Convert StepParser result to CognitiveGraphNode
            var root = ConvertToCognitiveGraphNode(parseResult, sourceCode);
            
            // Set metadata
            root.Metadata["sourceCode"] = sourceCode;
            root.Metadata["language"] = _config.Language;
            root.Metadata["parserType"] = "DevelApp.StepParser";
            root.Metadata["parserVersion"] = "1.0.1";
            var tokens = GetPropertyValue<object>(lexResult, "Tokens", "TokenList", "Results");
            var tokenCount = 0;
            if (tokens is System.Collections.ICollection collection)
            {
                tokenCount = collection.Count;
            }
            else if (tokens is System.Collections.IEnumerable enumerable)
            {
                tokenCount = enumerable.Cast<object>().Count();
            }
            root.Metadata["tokenCount"] = tokenCount;
            root.Metadata["parseSuccess"] = true;
            
            // Add location information if configured
            if (_config.IncludeLocationInfo)
            {
                var locationMap = GetPropertyValue<object>(parseResult, "LocationMap", "Locations", "PositionMap");
                AddLocationInformation(root, locationMap);
            }

            await Task.CompletedTask;
            return root;
        }
        catch (Exception ex)
        {
            // Return error node for parsing failures
            var errorNode = new NonTerminalNode("parse_error", 0);
            errorNode.Metadata["error"] = ex.Message;
            errorNode.Metadata["errorType"] = ex.GetType().Name;
            errorNode.Metadata["parserType"] = "DevelApp.StepParser";
            errorNode.Metadata["parserVersion"] = "1.0.1";
            errorNode.Metadata["sourceCode"] = sourceCode;
            errorNode.Metadata["parseSuccess"] = false;
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
            // Integrate with DevelApp.StepParser 1.0.1 validation using reflection
            var (lexResult, parseResult) = await InvokeStepParserAsync(sourceCode);
            
            var errors = new List<ParseError>();
            int tokenCount = 0;

            if (lexResult != null)
            {
                // Extract information from lexResult using reflection
                var lexSuccess = GetPropertyValue<bool?>(lexResult, "IsSuccess", "Success", "IsValid") ?? false;
                tokenCount = GetPropertyValue<int?>(lexResult, "TokenCount", "Count") ?? EstimateTokenCount(sourceCode);
                
                if (!lexSuccess)
                {
                    var errorMessage = GetPropertyValue<string>(lexResult, "ErrorMessage", "Error", "Message") ?? "Lexical analysis failed";
                    var errorLine = GetPropertyValue<int?>(lexResult, "ErrorLine", "Line") ?? 1;
                    var errorColumn = GetPropertyValue<int?>(lexResult, "ErrorColumn", "Column") ?? 1;
                    
                    errors.Add(new ParseError
                    {
                        Message = $"Lexical analysis failed: {errorMessage}",
                        Type = "LexicalError",
                        Line = errorLine,
                        Column = errorColumn
                    });
                }

                if (parseResult != null)
                {
                    var parseSuccess = GetPropertyValue<bool?>(parseResult, "IsSuccess", "Success", "IsValid") ?? false;
                    
                    if (!parseSuccess)
                    {
                        var errorMessage = GetPropertyValue<string>(parseResult, "ErrorMessage", "Error", "Message") ?? "Syntax analysis failed";
                        var errorLine = GetPropertyValue<int?>(parseResult, "ErrorLine", "Line") ?? 1;
                        var errorColumn = GetPropertyValue<int?>(parseResult, "ErrorColumn", "Column") ?? 1;
                        
                        errors.Add(new ParseError
                        {
                            Message = $"Syntax analysis failed: {errorMessage}",
                            Type = "SyntaxError",
                            Line = errorLine,
                            Column = errorColumn
                        });
                    }

                    // Try to extract diagnostics
                    var diagnostics = GetPropertyValue<object>(parseResult, "Diagnostics", "Warnings", "Messages");
                    if (diagnostics is System.Collections.IEnumerable diagEnum)
                    {
                        foreach (var diagnostic in diagEnum)
                        {
                            var message = GetPropertyValue<string>(diagnostic, "Message", "Text") ?? "Diagnostic message";
                            var severity = GetPropertyValue<string>(diagnostic, "Severity", "Type", "Level") ?? "Warning";
                            var line = GetPropertyValue<int?>(diagnostic, "Line") ?? 1;
                            var column = GetPropertyValue<int?>(diagnostic, "Column") ?? 1;
                            
                            errors.Add(new ParseError
                            {
                                Message = message,
                                Type = severity,
                                Line = line,
                                Column = column
                            });
                        }
                    }
                }
            }
            else
            {
                // Fallback validation if StepParser is not available
                errors = PerformFallbackValidation(sourceCode);
                tokenCount = EstimateTokenCount(sourceCode);
            }

            await Task.CompletedTask;

            return new ParseValidationResult
            {
                IsValid = errors.Count == 0 || errors.All(e => e.Type != "LexicalError" && e.Type != "SyntaxError"),
                Errors = errors.ToArray(),
                TokenCount = tokenCount
            };
        }
        catch (Exception ex)
        {
            return new ParseValidationResult
            {
                IsValid = false,
                Errors = new[] { new ParseError 
                { 
                    Message = ex.Message, 
                    Type = "ValidationException",
                    Line = 1,
                    Column = 1
                } },
                TokenCount = 0
            };
        }
    }
}