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

using System;
using Minotaur.Parser;
using Minotaur.Editor;

/// <summary>
/// Demonstrates the fully integrated StepParser functionality in Minotaur.
/// This example shows real parsing capabilities replacing the previous placeholder implementation.
/// </summary>
class StepParserIntegrationDemo
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Minotaur StepParser Integration Demo");
        Console.WriteLine("====================================");
        Console.WriteLine("This demo showcases the enhanced StepParser integration that replaces");
        Console.WriteLine("the previous placeholder implementation with real parsing capabilities.\n");

        // Test 1: Multi-language parsing
        await DemonstrateMultiLanguageParsing();
        
        // Test 2: Validation and error detection
        await DemonstrateValidationCapabilities();
        
        // Test 3: Metadata preservation during updates
        await DemonstrateMetadataPreservation();
        
        // Test 4: Location tracking
        await DemonstrateLocationTracking();
    }

    static async Task DemonstrateMultiLanguageParsing()
    {
        Console.WriteLine("=== Multi-Language Parsing Support ===");
        
        var testCases = new[]
        {
            ("C#", "csharp", "public class Test { public int Value = 42; }"),
            ("JavaScript", "javascript", "const test = { value: 42, method() { return this.value; } };"),
            ("Python", "python", "class Test:\n    def __init__(self):\n        self.value = 42"),
        };

        foreach (var (language, langId, code) in testCases)
        {
            Console.WriteLine($"\n{language} Code: {code.Replace("\n", "\\n")}");
            
            var config = new ParserConfiguration 
            { 
                Language = langId,
                IncludeLocationInfo = true,
                PreserveComments = true 
            };
            
            using var integration = new StepParserIntegration(config);
            
            var graph = await integration.ParseToCognitiveGraphAsync(code);
            var validation = await integration.ValidateSourceAsync(code);
            
            Console.WriteLine($"  ✓ Parsed into {graph.Children.Count} statements");
            Console.WriteLine($"  ✓ Validation: {(validation.IsValid ? "Valid" : "Invalid")} ({validation.TokenCount} tokens)");
            Console.WriteLine($"  ✓ Parser: {graph.Metadata.GetValueOrDefault("parserType", "Unknown")}");
        }
    }

    static async Task DemonstrateValidationCapabilities()
    {
        Console.WriteLine("\n\n=== Validation and Error Detection ===");
        
        var errorCases = new[]
        {
            ("Syntax Error", "var x = 42 // Missing semicolon\nvar y = 24"),
            ("Unmatched Braces", "{ var x = 42; // Missing closing brace"),
            ("Unmatched Parentheses", "var result = (42 + 5; // Missing closing paren"),
        };

        var config = new ParserConfiguration { Language = "csharp" };
        using var integration = new StepParserIntegration(config);

        foreach (var (description, code) in errorCases)
        {
            Console.WriteLine($"\n{description}: {code.Replace("\n", " ")}");
            
            var validation = await integration.ValidateSourceAsync(code);
            Console.WriteLine($"  Valid: {validation.IsValid}");
            
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors.Take(2))
                {
                    Console.WriteLine($"  ✗ {error.Type}: {error.Message} (Line {error.Line}, Col {error.Column})");
                }
            }
        }
    }

    static async Task DemonstrateMetadataPreservation()
    {
        Console.WriteLine("\n\n=== Metadata Preservation During Updates ===");
        
        var originalCode = "var original = 42;";
        var updatedCode = "var original = 42; var updated = 24;";

        var config = new ParserConfiguration { Language = "csharp" };
        using var integration = new StepParserIntegration(config);

        // Parse original and add metadata
        using var originalEditor = await integration.ParseToEditableGraphAsync(originalCode);
        if (originalEditor.Root != null)
        {
            originalEditor.Root.Metadata["author"] = "Developer";
            originalEditor.Root.Metadata["version"] = "1.0";
            originalEditor.Root.Metadata["timestamp"] = DateTime.Now.ToString();
            
            Console.WriteLine($"Original code parsed with {originalEditor.Root.Metadata.Count} metadata items");
            
            // Update the graph with new source code
            using var updatedEditor = await integration.UpdateGraphAsync(originalEditor, updatedCode);
            if (updatedEditor.Root != null)
            {
                Console.WriteLine($"Updated code preserves {updatedEditor.Root.Metadata.Count} metadata items");
                Console.WriteLine($"  ✓ Author preserved: {updatedEditor.Root.Metadata.GetValueOrDefault("author", "LOST")}");
                Console.WriteLine($"  ✓ Version preserved: {updatedEditor.Root.Metadata.GetValueOrDefault("version", "LOST")}");
                Console.WriteLine($"  ✓ Additional children: {updatedEditor.Root.Children.Count} statements");
            }
        }
    }

    static async Task DemonstrateLocationTracking()
    {
        Console.WriteLine("\n\n=== Location Tracking Capabilities ===");
        
        var multiLineCode = @"var first = 42;
string second = ""Hello"";
int third = 100;";

        var config = new ParserConfiguration 
        { 
            Language = "csharp", 
            IncludeLocationInfo = true 
        };
        
        using var integration = new StepParserIntegration(config);
        var graph = await integration.ParseToCognitiveGraphAsync(multiLineCode);
        
        Console.WriteLine("Source code with location tracking:");
        Console.WriteLine(multiLineCode.Replace("\n", "\\n"));
        Console.WriteLine("\nParsed statements with positions:");
        
        foreach (var statement in graph.Children.Take(3))
        {
            if (statement.SourcePosition != null)
            {
                Console.WriteLine($"  Statement: Line {statement.SourcePosition.Line}, " +
                                $"Col {statement.SourcePosition.Column}, " +
                                $"Offset {statement.SourcePosition.Offset}, " +
                                $"Length {statement.SourcePosition.Length}");
            }
        }
        
        Console.WriteLine($"\nTotal parsed statements: {graph.Children.Count}");
    }
}