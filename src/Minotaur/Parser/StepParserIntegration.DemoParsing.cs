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
using Minotaur.Plugins;
using System.Reflection;
using System.Linq;

namespace Minotaur.Parser;

/// <summary>
/// Demo/fallback parsing implementation for StepParserIntegration.
/// Builds a simplified cognitive graph from tokenized source when the
/// external DevelApp.StepParser/StepLexer packages are not available.
/// </summary>
public partial class StepParserIntegration
{
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
                var childrenObj = childrenProperty.GetValue(syntaxTree);
                if (childrenObj != null)
                {
                    var children = childrenObj as System.Collections.IEnumerable;
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
        }
        catch (ArgumentException ex)
        {
            root.Metadata["conversionError"] = ex.Message;
            var fallbackNodes = FallbackParsing(sourceCode);
            foreach (var node in fallbackNodes)
            {
                root.AddChild(node);
            }
        }
        catch (TargetInvocationException ex)
        {
            root.Metadata["conversionError"] = ex.Message;
            var fallbackNodes = FallbackParsing(sourceCode);
            foreach (var node in fallbackNodes)
            {
                root.AddChild(node);
            }
        }
        catch (InvalidCastException ex)
        {
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
    /// <param name="sourceCode">The source code to parse.</param>
    /// <param name="version">The cognitive graph version to use (V1 or V2).</param>
}
