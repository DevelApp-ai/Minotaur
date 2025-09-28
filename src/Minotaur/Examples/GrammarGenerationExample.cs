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

using Minotaur.GrammarGeneration;
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.Examples;

/// <summary>
/// Example demonstrating grammar generation capabilities
/// </summary>
public class GrammarGenerationExample
{
    /// <summary>
    /// Runs the grammar generation example demonstrating Minotaur's capabilities.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync()
    {
        Console.WriteLine("🔧 Minotaur Grammar Generation Example");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        // Create some sample source files for demonstration
        await CreateSampleSourceFiles();

        // Initialize the grammar generator
        var generator = new GrammarGenerator();

        try
        {
            // Generate grammar for a simple language
            Console.WriteLine("🔄 Generating grammar for SimpleScript language...");

            var sourceFiles = new[]
            {
                "/tmp/sample1.sscript",
                "/tmp/sample2.sscript",
                "/tmp/sample3.sscript"
            };

            var context = new LanguageContext
            {
                HasNestedScopes = true,
                HasTypeSystem = false,
                HasMacroSystem = false,
                HasComments = true,
                HasStringLiterals = true,
                HasNumericLiterals = true,
                FileExtensions = new List<string> { ".sscript" },
                DefaultEncoding = "UTF-8"
            };

            // Progress tracking
            var progress = new Progress<GrammarGenerationProgress>(p =>
            {
                Console.WriteLine($"  {p.Stage}... {p.Progress}%");
            });

            var grammar = await generator.GenerateGrammarAsync(
                "SimpleScript",
                sourceFiles,
                context,
                progress);

            Console.WriteLine();
            Console.WriteLine("✅ Grammar generation completed!");
            Console.WriteLine($"📊 Generated {grammar.ProductionRules.Rules.Count} production rules");
            Console.WriteLine($"🎯 Generated {grammar.TokenRules.Patterns.Count} token patterns");
            Console.WriteLine();

            // Display the generated grammar
            Console.WriteLine("📄 Generated Grammar File:");
            Console.WriteLine(new string('=', 50));
            var grammarContent = generator.GenerateGrammarFile(grammar);
            Console.WriteLine(grammarContent);

            // Save the grammar to a file
            var outputPath = "/tmp/SimpleScript.grammar";
            await generator.SaveGrammarAsync(grammar, outputPath);
            Console.WriteLine($"💾 Grammar saved to: {outputPath}");

            // Show some statistics
            ShowGrammarStatistics(grammar);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            // Clean up sample files
            CleanupSampleFiles();
        }
    }

    private static async Task CreateSampleSourceFiles()
    {
        // Sample 1: Function definitions and variables
        var sample1 = @"// SimpleScript Sample 1
var x = 10;
var name = ""Hello World"";

function greet(name) {
    print(""Hello, "" + name + ""!"");
}

function add(a, b) {
    return a + b;
}

greet(name);
var result = add(5, 3);
print(result);
";

        // Sample 2: Control flow
        var sample2 = @"// SimpleScript Sample 2
var i = 0;
while (i < 10) {
    if (i % 2 == 0) {
        print(""Even: "" + i);
    } else {
        print(""Odd: "" + i);
    }
    i = i + 1;
}

for (var j = 0; j < 5; j++) {
    print(""Loop: "" + j);
}
";

        // Sample 3: More complex expressions
        var sample3 = @"// SimpleScript Sample 3
function fibonacci(n) {
    if (n <= 1) {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}

var num = 10;
var fib = fibonacci(num);
print(""Fibonacci of "" + num + "" is "" + fib);

// Array-like operations
var items = [1, 2, 3, 4, 5];
for (var item in items) {
    print(item);
}
";

        await File.WriteAllTextAsync("/tmp/sample1.sscript", sample1);
        await File.WriteAllTextAsync("/tmp/sample2.sscript", sample2);
        await File.WriteAllTextAsync("/tmp/sample3.sscript", sample3);
    }

    private static void ShowGrammarStatistics(Grammar grammar)
    {
        Console.WriteLine();
        Console.WriteLine("📈 Grammar Statistics:");
        Console.WriteLine(new string('-', 30));

        // Token statistics
        var tokensByType = grammar.TokenRules.Patterns
            .GroupBy(p => p.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("Token Types:");
        foreach (var (type, count) in tokensByType)
        {
            Console.WriteLine($"  {type}: {count}");
        }

        // Rule statistics
        var totalAlternatives = grammar.ProductionRules.Rules.Sum(r => r.Alternatives.Count);
        var avgAlternatives = grammar.ProductionRules.Rules.Count > 0
            ? (double)totalAlternatives / grammar.ProductionRules.Rules.Count
            : 0;

        Console.WriteLine();
        Console.WriteLine("Production Rules:");
        Console.WriteLine($"  Total rules: {grammar.ProductionRules.Rules.Count}");
        Console.WriteLine($"  Total alternatives: {totalAlternatives}");
        Console.WriteLine($"  Average alternatives per rule: {avgAlternatives:F2}");

        // Top rules by confidence
        var topRules = grammar.ProductionRules.Rules
            .OrderByDescending(r => r.Confidence)
            .Take(5)
            .ToList();

        if (topRules.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top rules by confidence:");
            foreach (var rule in topRules)
            {
                Console.WriteLine($"  {rule.Name}: {rule.Confidence:F2}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Metadata:");
        foreach (var (key, value) in grammar.Metadata)
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }

    private static void CleanupSampleFiles()
    {
        try
        {
            File.Delete("/tmp/sample1.sscript");
            File.Delete("/tmp/sample2.sscript");
            File.Delete("/tmp/sample3.sscript");
            File.Delete("/tmp/SimpleScript.grammar");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}