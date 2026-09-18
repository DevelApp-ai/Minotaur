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
/// Reflection-based interop with the DevelApp.StepParser/StepLexer packages
/// for StepParserIntegration, plus fallback graph construction and validation.
/// </summary>
public partial class StepParserIntegration
{
    private async Task<(object? lexResult, object? parseResult)> InvokeStepParserAsync(string sourceCode, CognitiveGraphVersion version)
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

            // Create parser instance with version support
            object? parser = null;
            try
            {
                // Try to create with language and version parameters (StepParser 1.12.0+)
                // Falls back to language-only or default constructor for older versions
                parser = Activator.CreateInstance(parserType, _config.Language, (int)version);
            }
            catch
            {
                try
                {
                    // Fallback for versions without version parameter
                    parser = Activator.CreateInstance(parserType, _config.Language);
                }
                catch
                {
                    // Fallback for versions without any parameters
                    parser = Activator.CreateInstance(parserType);
                }
            }

            if (parser == null || lexResult == null)
            {
                return (lexResult, null);
            }

            // Set cognitive graph version if property exists
            // Different versions of StepParser may use different property names:
            // - "CognitiveGraphVersion" (StepParser 1.12.0+)
            // - "GraphVersion" (older versions)
            try
            {
                var versionProperty = parserType.GetProperty("CognitiveGraphVersion") ??
                                     parserType.GetProperty("GraphVersion");
                if (versionProperty != null && versionProperty.CanWrite)
                {
                    versionProperty.SetValue(parser, (int)version);
                }
            }
            catch
            {
                // Property doesn't exist or can't be set - continue without it
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
                        var stringValue = value.ToString();
                        return stringValue != null ? (T)(object)stringValue : default;
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

}
