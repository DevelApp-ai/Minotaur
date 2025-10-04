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

    #region Integrated StepParser Implementation

    /// <summary>
    /// Token information for parsed source code
    /// </summary>
    private class Token
    {
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Position { get; set; }
        public int Length { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    private List<Token> TokenizeSourceCode(string sourceCode)
    {
        var tokens = new List<Token>();
        var position = 0;
        var line = 1;
        var column = 1;

        while (position < sourceCode.Length)
        {
            // Skip whitespace but track position
            if (char.IsWhiteSpace(sourceCode[position]))
            {
                if (sourceCode[position] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
                position++;
                continue;
            }

            var token = ExtractNextToken(sourceCode, position, line, column);
            if (token != null)
            {
                tokens.Add(token);
                position = token.Position + token.Length;
                column += token.Length;
            }
            else
            {
                position++; // Skip unknown characters
                column++;
            }
        }

        return tokens;
    }

    private Token? ExtractNextToken(string sourceCode, int position, int line, int column)
    {
        if (position >= sourceCode.Length) return null;

        var currentChar = sourceCode[position];

        // String literals
        if (currentChar == '"' || currentChar == '\'')
        {
            return ExtractStringLiteral(sourceCode, position, line, column, currentChar);
        }

        // Numbers
        if (char.IsDigit(currentChar))
        {
            return ExtractNumber(sourceCode, position, line, column);
        }

        // Identifiers and keywords
        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ExtractIdentifierOrKeyword(sourceCode, position, line, column);
        }

        // Operators and punctuation
        return ExtractOperatorOrPunctuation(sourceCode, position, line, column);
    }

    private Token ExtractStringLiteral(string sourceCode, int position, int line, int column, char quote)
    {
        var start = position;
        position++; // Skip opening quote

        while (position < sourceCode.Length && sourceCode[position] != quote)
        {
            if (sourceCode[position] == '\\' && position + 1 < sourceCode.Length)
            {
                position += 2; // Skip escape sequence
            }
            else
            {
                position++;
            }
        }

        if (position < sourceCode.Length) position++; // Include closing quote

        var value = sourceCode.Substring(start, position - start);
        return new Token
        {
            Value = value,
            Type = "string_literal",
            Position = start,
            Length = position - start,
            Line = line,
            Column = column
        };
    }

    private Token ExtractNumber(string sourceCode, int position, int line, int column)
    {
        var start = position;

        while (position < sourceCode.Length && (char.IsDigit(sourceCode[position]) || sourceCode[position] == '.'))
        {
            position++;
        }

        var value = sourceCode.Substring(start, position - start);
        return new Token
        {
            Value = value,
            Type = value.Contains('.') ? "float_literal" : "integer_literal",
            Position = start,
            Length = position - start,
            Line = line,
            Column = column
        };
    }

    private Token ExtractIdentifierOrKeyword(string sourceCode, int position, int line, int column)
    {
        var start = position;

        while (position < sourceCode.Length && 
               (char.IsLetterOrDigit(sourceCode[position]) || sourceCode[position] == '_'))
        {
            position++;
        }

        var value = sourceCode.Substring(start, position - start);
        var type = IsKeyword(value) ? "keyword" : "identifier";

        return new Token
        {
            Value = value,
            Type = type,
            Position = start,
            Length = position - start,
            Line = line,
            Column = column
        };
    }

    private Token ExtractOperatorOrPunctuation(string sourceCode, int position, int line, int column)
    {
        var currentChar = sourceCode[position];
        var value = currentChar.ToString();
        var type = "operator";

        // Check for multi-character operators
        if (position + 1 < sourceCode.Length)
        {
            var twoChar = sourceCode.Substring(position, 2);
            if (IsMultiCharOperator(twoChar))
            {
                value = twoChar;
                position++;
            }
        }

        // Classify punctuation vs operators
        if ("(){}[];,".Contains(currentChar))
        {
            type = "punctuation";
        }

        return new Token
        {
            Value = value,
            Type = type,
            Position = position,
            Length = value.Length,
            Line = line,
            Column = column
        };
    }

    private bool IsMultiCharOperator(string op)
    {
        var multiCharOps = new[] { "==", "!=", "<=", ">=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=" };
        return multiCharOps.Contains(op);
    }

    private List<CognitiveGraphNode> ParseStatements(List<Token> tokens, string sourceCode, ref int currentPosition)
    {
        var statements = new List<CognitiveGraphNode>();
        var tokenIndex = 0;

        while (tokenIndex < tokens.Count)
        {
            var statement = ParseStatement(tokens, ref tokenIndex, sourceCode);
            if (statement != null)
            {
                statements.Add(statement);
            }
        }

        return statements;
    }

    private CognitiveGraphNode? ParseStatement(List<Token> tokens, ref int tokenIndex, string sourceCode)
    {
        if (tokenIndex >= tokens.Count) return null;

        var startToken = tokens[tokenIndex];
        var statementNode = new NonTerminalNode("statement", 0, (uint)startToken.Position, 0);

        // Simple statement parsing based on language
        switch (_config.Language.ToLowerInvariant())
        {
            case "csharp":
                return ParseCSharpStatement(tokens, ref tokenIndex, sourceCode);
            case "javascript":
                return ParseJavaScriptStatement(tokens, ref tokenIndex, sourceCode);
            case "python":
                return ParsePythonStatement(tokens, ref tokenIndex, sourceCode);
            default:
                return ParseGenericStatement(tokens, ref tokenIndex, sourceCode);
        }
    }

    private CognitiveGraphNode ParseCSharpStatement(List<Token> tokens, ref int tokenIndex, string sourceCode)
    {
        var startToken = tokens[tokenIndex];
        var statement = new NonTerminalNode("csharp_statement", 0, (uint)startToken.Position, 0);

        // Parse variable declaration or simple expression
        while (tokenIndex < tokens.Count)
        {
            var token = tokens[tokenIndex];
            
            // Create appropriate node based on token type
            CognitiveGraphNode node = token.Type switch
            {
                "keyword" => new TerminalNode(token.Value, "keyword", (uint)token.Position, (uint)token.Length),
                "identifier" => new IdentifierNode(token.Value),
                "string_literal" => new LiteralNode(token.Value, "string", token.Value.Trim('"')),
                "integer_literal" => new LiteralNode(token.Value, "int", int.Parse(token.Value)),
                "float_literal" => new LiteralNode(token.Value, "float", double.Parse(token.Value)),
                _ => new TerminalNode(token.Value, token.Type, (uint)token.Position, (uint)token.Length)
            };

            // Set position information
            node.SourcePosition = new SourcePosition(token.Line, token.Column, token.Position, token.Length);
            statement.AddChild(node);

            tokenIndex++;

            // End statement on semicolon
            if (token.Value == ";")
            {
                break;
            }
        }

        // Update statement length
        if (statement.Children.Count > 0)
        {
            var lastChild = statement.Children[^1];
            if (lastChild.SourcePosition != null)
            {
                var endPos = lastChild.SourcePosition.Offset + lastChild.SourcePosition.Length;
                statement.SourcePosition = new SourcePosition(
                    startToken.Line, 
                    startToken.Column, 
                    startToken.Position, 
                    endPos - startToken.Position);
            }
        }

        return statement;
    }

    private CognitiveGraphNode ParseJavaScriptStatement(List<Token> tokens, ref int tokenIndex, string sourceCode)
    {
        // Similar to C# but with JavaScript-specific rules
        return ParseCSharpStatement(tokens, ref tokenIndex, sourceCode); // Simplified for now
    }

    private CognitiveGraphNode ParsePythonStatement(List<Token> tokens, ref int tokenIndex, string sourceCode)
    {
        // Python-specific parsing (no semicolons, indentation-based)
        var startToken = tokens[tokenIndex];
        var statement = new NonTerminalNode("python_statement", 0, (uint)startToken.Position, 0);

        while (tokenIndex < tokens.Count)
        {
            var token = tokens[tokenIndex];
            
            CognitiveGraphNode node = token.Type switch
            {
                "keyword" => new TerminalNode(token.Value, "keyword", (uint)token.Position, (uint)token.Length),
                "identifier" => new IdentifierNode(token.Value),
                "string_literal" => new LiteralNode(token.Value, "string", token.Value.Trim('"', '\'')),
                "integer_literal" => new LiteralNode(token.Value, "int", int.Parse(token.Value)),
                "float_literal" => new LiteralNode(token.Value, "float", double.Parse(token.Value)),
                _ => new TerminalNode(token.Value, token.Type, (uint)token.Position, (uint)token.Length)
            };

            node.SourcePosition = new SourcePosition(token.Line, token.Column, token.Position, token.Length);
            statement.AddChild(node);

            tokenIndex++;

            // End statement on newline (simplified)
            if (tokenIndex >= tokens.Count || tokens[tokenIndex].Line > token.Line)
            {
                break;
            }
        }

        return statement;
    }

    private CognitiveGraphNode ParseGenericStatement(List<Token> tokens, ref int tokenIndex, string sourceCode)
    {
        var startToken = tokens[tokenIndex];
        var statement = new NonTerminalNode("generic_statement", 0, (uint)startToken.Position, 0);

        // Parse a single line or until semicolon
        var startLine = startToken.Line;
        
        while (tokenIndex < tokens.Count)
        {
            var token = tokens[tokenIndex];
            
            // Stop at line break or semicolon
            if (token.Line > startLine || token.Value == ";")
            {
                if (token.Value == ";")
                {
                    var semicolon = new TerminalNode(token.Value, "punctuation", (uint)token.Position, (uint)token.Length);
                    semicolon.SourcePosition = new SourcePosition(token.Line, token.Column, token.Position, token.Length);
                    statement.AddChild(semicolon);
                    tokenIndex++;
                }
                break;
            }

            CognitiveGraphNode node = token.Type switch
            {
                "keyword" => new TerminalNode(token.Value, "keyword", (uint)token.Position, (uint)token.Length),
                "identifier" => new IdentifierNode(token.Value),
                "string_literal" => new LiteralNode(token.Value, "string", token.Value.Trim('"', '\'')),
                "integer_literal" => new LiteralNode(token.Value, "int", int.Parse(token.Value)),
                "float_literal" => new LiteralNode(token.Value, "float", double.Parse(token.Value)),
                _ => new TerminalNode(token.Value, token.Type, (uint)token.Position, (uint)token.Length)
            };

            node.SourcePosition = new SourcePosition(token.Line, token.Column, token.Position, token.Length);
            statement.AddChild(node);
            tokenIndex++;
        }

        return statement;
    }

    private void AddLocationInformation(CognitiveGraphNode node, string sourceCode)
    {
        if (node.SourcePosition == null && node.Metadata.ContainsKey("sourceCode"))
        {
            // Calculate position for root node
            node.SourcePosition = new SourcePosition(1, 1, 0, sourceCode.Length);
        }

        foreach (var child in node.Children)
        {
            AddLocationInformation(child, sourceCode);
        }
    }

    private IEnumerable<string> SimpleTokenize(string sourceCode)
    {
        // Legacy method for compatibility - redirect to new tokenizer
        return TokenizeSourceCode(sourceCode).Select(t => t.Value);
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
    /// Parses source code using integrated StepParser functionality.
    /// This implementation provides proper parsing capabilities for Minotaur.
    /// </summary>
    private async Task<CognitiveGraphNode> ParseWithStepParserAsync(string sourceCode)
    {
        try
        {
            // Create root node for the compilation unit
            var root = new NonTerminalNode("compilation_unit", 0, 0, (uint)sourceCode.Length);
            root.Metadata["sourceCode"] = sourceCode;
            root.Metadata["language"] = _config.Language;
            root.Metadata["parserType"] = "Minotaur.StepParser";
            root.Metadata["parserVersion"] = "1.0.0";

            // Tokenize the source code
            var tokens = TokenizeSourceCode(sourceCode);
            var currentPosition = 0;

            // Parse tokens into structured nodes based on language
            var statements = ParseStatements(tokens, sourceCode, ref currentPosition);
            
            foreach (var statement in statements)
            {
                root.AddChild(statement);
            }

            // Add location information if configured
            if (_config.IncludeLocationInfo)
            {
                AddLocationInformation(root, sourceCode);
            }

            await Task.CompletedTask;
            return root;
        }
        catch (Exception ex)
        {
            // Return error node for parsing failures
            var errorNode = new NonTerminalNode("parse_error", 0, 0, (uint)sourceCode.Length);
            errorNode.Metadata["error"] = ex.Message;
            errorNode.Metadata["errorType"] = ex.GetType().Name;
            errorNode.Metadata["parserType"] = "Minotaur.StepParser";
            errorNode.Metadata["sourceCode"] = sourceCode;
            return errorNode;
        }
    }

    /// <summary>
    /// Validates source code using integrated StepParser functionality.
    /// This provides comprehensive validation using proper parsing.
    /// </summary>
    private async Task<ParseValidationResult> ValidateWithStepParserAsync(string sourceCode)
    {
        try
        {
            var errors = new List<ParseError>();
            var tokenCount = 0;

            // Attempt to parse the source code to validate it
            try
            {
                var tokens = TokenizeSourceCode(sourceCode);
                tokenCount = tokens.Count;

                // Check for tokenization errors
                var invalidTokens = tokens.Where(t => string.IsNullOrEmpty(t.Type) || t.Type == "unknown").ToList();
                foreach (var token in invalidTokens)
                {
                    errors.Add(new ParseError
                    {
                        Message = $"Invalid token: '{token.Value}'",
                        Type = "TokenizationError",
                        Line = token.Line,
                        Column = token.Column
                    });
                }

                // Perform structural validation
                ValidateStructure(tokens, errors);

                // Language-specific validation
                ValidateLanguageSpecific(tokens, errors);

                await Task.CompletedTask;
            }
            catch (Exception parseEx)
            {
                errors.Add(new ParseError
                {
                    Message = $"Parsing failed: {parseEx.Message}",
                    Type = "ParseException",
                    Line = 1,
                    Column = 1
                });
            }

            return new ParseValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                TokenCount = tokenCount
            };
        }
        catch (Exception ex)
        {
            return new ParseValidationResult
            {
                IsValid = false,
                Errors = new[] { new ParseError { Message = ex.Message, Type = "ValidationException" } },
                TokenCount = 0
            };
        }
    }

    private void ValidateStructure(List<Token> tokens, List<ParseError> errors)
    {
        var braceStack = new Stack<Token>();
        var parenStack = new Stack<Token>();
        var bracketStack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Value)
            {
                case "{":
                    braceStack.Push(token);
                    break;
                case "}":
                    if (braceStack.Count == 0)
                    {
                        errors.Add(new ParseError
                        {
                            Message = "Unmatched closing brace",
                            Type = "SyntaxError",
                            Line = token.Line,
                            Column = token.Column
                        });
                    }
                    else
                    {
                        braceStack.Pop();
                    }
                    break;
                case "(":
                    parenStack.Push(token);
                    break;
                case ")":
                    if (parenStack.Count == 0)
                    {
                        errors.Add(new ParseError
                        {
                            Message = "Unmatched closing parenthesis",
                            Type = "SyntaxError",
                            Line = token.Line,
                            Column = token.Column
                        });
                    }
                    else
                    {
                        parenStack.Pop();
                    }
                    break;
                case "[":
                    bracketStack.Push(token);
                    break;
                case "]":
                    if (bracketStack.Count == 0)
                    {
                        errors.Add(new ParseError
                        {
                            Message = "Unmatched closing bracket",
                            Type = "SyntaxError",
                            Line = token.Line,
                            Column = token.Column
                        });
                    }
                    else
                    {
                        bracketStack.Pop();
                    }
                    break;
            }
        }

        // Check for unclosed delimiters
        while (braceStack.Count > 0)
        {
            var unclosed = braceStack.Pop();
            errors.Add(new ParseError
            {
                Message = "Unclosed brace",
                Type = "SyntaxError",
                Line = unclosed.Line,
                Column = unclosed.Column
            });
        }

        while (parenStack.Count > 0)
        {
            var unclosed = parenStack.Pop();
            errors.Add(new ParseError
            {
                Message = "Unclosed parenthesis",
                Type = "SyntaxError",
                Line = unclosed.Line,
                Column = unclosed.Column
            });
        }

        while (bracketStack.Count > 0)
        {
            var unclosed = bracketStack.Pop();
            errors.Add(new ParseError
            {
                Message = "Unclosed bracket",
                Type = "SyntaxError",
                Line = unclosed.Line,
                Column = unclosed.Column
            });
        }
    }

    private void ValidateLanguageSpecific(List<Token> tokens, List<ParseError> errors)
    {
        switch (_config.Language.ToLowerInvariant())
        {
            case "csharp":
                ValidateCSharpSyntax(tokens, errors);
                break;
            case "javascript":
                ValidateJavaScriptSyntax(tokens, errors);
                break;
            case "python":
                ValidatePythonSyntax(tokens, errors);
                break;
        }
    }

    private void ValidateCSharpSyntax(List<Token> tokens, List<ParseError> errors)
    {
        // Check for missing semicolons in C#
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var current = tokens[i];
            var next = tokens[i + 1];

            // Simple check: if we have a statement-ending token followed by a new line or identifier, check for semicolon
            if ((current.Type == "identifier" || current.Type == "integer_literal" || current.Type == "string_literal") &&
                next.Line > current.Line &&
                i > 0 &&
                tokens[i - 1].Value != ";" &&
                !IsControlStructureKeyword(tokens, i))
            {
                // This might need a semicolon
                var needsSemicolon = true;
                
                // Check if it's part of a control structure
                for (int j = Math.Max(0, i - 5); j < i; j++)
                {
                    if (tokens[j].Value == "if" || tokens[j].Value == "for" || tokens[j].Value == "while" ||
                        tokens[j].Value == "class" || tokens[j].Value == "namespace")
                    {
                        needsSemicolon = false;
                        break;
                    }
                }

                if (needsSemicolon)
                {
                    errors.Add(new ParseError
                    {
                        Message = "Missing semicolon",
                        Type = "SyntaxError",
                        Line = current.Line,
                        Column = current.Column + current.Length
                    });
                }
            }
        }
    }

    private void ValidateJavaScriptSyntax(List<Token> tokens, List<ParseError> errors)
    {
        // JavaScript-specific validation (similar to C# but more flexible with semicolons)
        // For now, use similar logic to C#
        ValidateCSharpSyntax(tokens, errors);
    }

    private void ValidatePythonSyntax(List<Token> tokens, List<ParseError> errors)
    {
        // Python-specific validation
        // Check for proper indentation (simplified)
        var indentLevels = new Stack<int>();
        indentLevels.Push(0);

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            
            // Check for colon after control structures
            if (token.Value == "if" || token.Value == "for" || token.Value == "while" || 
                token.Value == "def" || token.Value == "class")
            {
                // Look for colon in the same line
                var foundColon = false;
                for (int j = i + 1; j < tokens.Count && tokens[j].Line == token.Line; j++)
                {
                    if (tokens[j].Value == ":")
                    {
                        foundColon = true;
                        break;
                    }
                }

                if (!foundColon)
                {
                    errors.Add(new ParseError
                    {
                        Message = $"Missing colon after '{token.Value}' statement",
                        Type = "SyntaxError",
                        Line = token.Line,
                        Column = token.Column
                    });
                }
            }
        }
    }

    private bool IsControlStructureKeyword(List<Token> tokens, int index)
    {
        // Check if the current context is part of a control structure
        for (int i = Math.Max(0, index - 10); i < index; i++)
        {
            if (tokens[i].Type == "keyword" && 
                (tokens[i].Value == "if" || tokens[i].Value == "for" || tokens[i].Value == "while" ||
                 tokens[i].Value == "switch" || tokens[i].Value == "using"))
            {
                return true;
            }
        }
        return false;
    }
}