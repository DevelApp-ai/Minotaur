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

using Minotaur.Analysis.Symbolic;
using Minotaur.Plugins;

namespace Minotaur.Demo;

/// <summary>
/// Demonstrates the NuGet plugin-based symbolic analysis system in response to @larsbuch feedback
/// </summary>
public class PluginBasedSymbolicAnalysisDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("=== NuGet Plugin-Based Symbolic Analysis Demo ===");
        Console.WriteLine("Demonstrating symbolic analysis using the existing NuGet plugin architecture\n");

        // Create plugin manager and symbolic analysis engine
        var pluginManager = new LanguagePluginManager();
        var engine = new SymbolicAnalysisEngine(null, pluginManager);

        Console.WriteLine("Available Language Plugins with Symbolic Analysis:");
        foreach (var plugin in pluginManager.RegisteredPlugins)
        {
            var hasSymbolicAnalysis = plugin.Value is ISymbolicAnalysisPlugin;
            Console.WriteLine($"  - {plugin.Value.DisplayName} ({plugin.Key}): {(hasSymbolicAnalysis ? "✅ Symbolic Analysis" : "❌ No Symbolic Analysis")}");
            
            if (hasSymbolicAnalysis)
            {
                var symbolicPlugin = (ISymbolicAnalysisPlugin)plugin.Value;
                var patterns = symbolicPlugin.GetErrorPatterns();
                Console.WriteLine($"    Error patterns: {patterns.Count}");
                foreach (var pattern in patterns.Take(2))
                {
                    Console.WriteLine($"      • {pattern.Name}: {pattern.ErrorType}");
                }
            }
        }

        Console.WriteLine("\n1. C# Analysis with Plugin System:");
        DemonstrateCSharpAnalysis(engine);

        Console.WriteLine("\n2. Python Analysis with Plugin System:");
        DemonstratePythonAnalysis(engine);

        Console.WriteLine("\n3. JavaScript Analysis with Plugin System:");
        DemonstrateJavaScriptAnalysis(engine);

        Console.WriteLine("\n4. Plugin System Benefits:");
        ShowPluginSystemBenefits();
    }

    private static void DemonstrateCSharpAnalysis(SymbolicAnalysisEngine engine)
    {
        var csharpCode = @"
public void ProcessData(string data)
{
    string content = null;
    Console.WriteLine(content.Length);  // Null reference via plugin analysis
    
    var stream = new FileStream(""file.txt"", FileMode.Open);
    // No using statement - plugin detects resource leak
}
";

        Console.WriteLine("C# Code:");
        Console.WriteLine(csharpCode);

        var result = engine.AnalyzeCode(csharpCode, "csharp");
        PrintPluginAnalysisResult(result, "C#");
    }

    private static void DemonstratePythonAnalysis(SymbolicAnalysisEngine engine)
    {
        var pythonCode = @"
def process_data(data):
    value = None
    print(value.length)  # None access via plugin analysis
    
    items = [1, 2, 3]
    print(items[10])  # Index error via plugin analysis
";

        Console.WriteLine("Python Code:");
        Console.WriteLine(pythonCode);

        var result = engine.AnalyzeCode(pythonCode, "python");
        PrintPluginAnalysisResult(result, "Python");
    }

    private static void DemonstrateJavaScriptAnalysis(SymbolicAnalysisEngine engine)
    {
        var jsCode = @"
function processData(obj) {
    let value = null;
    console.log(value.property);  // Null access via plugin analysis
    
    if (obj == 5) {  // Type coercion issue via plugin analysis
        return true;
    }
}
";

        Console.WriteLine("JavaScript Code:");
        Console.WriteLine(jsCode);

        var result = engine.AnalyzeCode(jsCode, "javascript");
        PrintPluginAnalysisResult(result, "JavaScript");
    }

    private static void PrintPluginAnalysisResult(SymbolicAnalysisResult result, string language)
    {
        Console.WriteLine($"Plugin-based {language} Analysis Result:");
        Console.WriteLine($"  Success: {result.Success}");
        Console.WriteLine($"  Errors detected: {result.Errors.Count}");
        Console.WriteLine($"  Analysis time: {result.AnalysisTime.TotalMilliseconds:F2}ms");

        if (result.Errors.Any())
        {
            Console.WriteLine("  Detected Errors (via plugins):");
            foreach (var error in result.Errors.Take(3))
            {
                Console.WriteLine($"    • {error.Type} at line {error.Location.Line}: {error.Message}");
                Console.WriteLine($"      Confidence: {error.Confidence:P1}");
            }
        }
        Console.WriteLine();
    }

    private static void ShowPluginSystemBenefits()
    {
        Console.WriteLine("NuGet Plugin System Benefits:");
        Console.WriteLine("✅ **Extensible Architecture**: New languages can be added via NuGet packages");
        Console.WriteLine("✅ **Unified Interface**: Both unparsing and symbolic analysis in same plugin");
        Console.WriteLine("✅ **Runtime Loading**: Plugins can be loaded dynamically from assemblies");
        Console.WriteLine("✅ **Separation of Concerns**: Language-specific logic isolated in plugins");
        Console.WriteLine("✅ **Maintainable**: Each language plugin can be versioned independently");
        Console.WriteLine("✅ **Testable**: Plugins can be tested in isolation");
        Console.WriteLine("✅ **Consistent API**: Same interface for all language-specific operations");
        
        Console.WriteLine("\nPlugin System Architecture:");
        Console.WriteLine("┌─────────────────────────────────────────────────┐");
        Console.WriteLine("│            SymbolicAnalysisEngine              │");
        Console.WriteLine("├─────────────────────────────────────────────────┤");
        Console.WriteLine("│           LanguagePluginManager                │");
        Console.WriteLine("├─────────────────────────────────────────────────┤");
        Console.WriteLine("│  ILanguagePlugin + ISymbolicAnalysisPlugin     │");
        Console.WriteLine("├─────┬─────────────┬─────────────┬───────────────┤");
        Console.WriteLine("│ C#  │ JavaScript  │   Python    │   (Custom)    │");
        Console.WriteLine("│ via │     via     │     via     │      via      │");
        Console.WriteLine("│NuGet│    NuGet    │    NuGet    │    NuGet      │");
        Console.WriteLine("└─────┴─────────────┴─────────────┴───────────────┘");
    }
}