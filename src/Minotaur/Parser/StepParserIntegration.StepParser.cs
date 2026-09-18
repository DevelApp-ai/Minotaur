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

public partial class StepParserIntegration
{
    /// <summary>
    /// Parses source code using DevelApp.StepParser NuGet package.
    /// This is the authoritative parsing method - plugins are NOT used for parsing.
    /// </summary>
    /// <param name="sourceCode">The source code to parse.</param>
    /// <param name="version">The cognitive graph version to use (V1 or V2).</param>
    private async Task<CognitiveGraphNode> ParseWithStepParserAsync(string sourceCode, CognitiveGraphVersion version)
    {
        try
        {
            // Integrate with DevelApp.StepParser 1.12.0 NuGet package using reflection
            // This approach allows us to work with the actual API structure
            var (lexResult, parseResult) = await InvokeStepParserAsync(sourceCode, version);

            if (lexResult == null || parseResult == null)
            {
                // If StepParser integration fails, use fallback parsing
                var fallbackRoot = CreateFallbackCognitiveGraph(sourceCode);
                fallbackRoot.Metadata["parsingMethod"] = "Fallback";
                fallbackRoot.Metadata["cognitiveGraphVersion"] = version.ToString();
                return fallbackRoot;
            }

            // Convert StepParser result to CognitiveGraphNode
            var root = ConvertToCognitiveGraphNode(parseResult, sourceCode);

            // Set metadata
            root.Metadata["sourceCode"] = sourceCode;
            root.Metadata["language"] = _config.Language;
            root.Metadata["parserType"] = "DevelApp.StepParser";
            root.Metadata["parserVersion"] = "1.12.0";
            root.Metadata["cognitiveGraphVersion"] = version.ToString();
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
            errorNode.Metadata["parserVersion"] = "1.12.0";
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
            // Determine which version to use
            var effectiveVersion = DetermineGraphVersion(sourceCode);

            // Integrate with DevelApp.StepParser 1.12.0 validation using reflection
            var (lexResult, parseResult) = await InvokeStepParserAsync(sourceCode, effectiveVersion);

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
