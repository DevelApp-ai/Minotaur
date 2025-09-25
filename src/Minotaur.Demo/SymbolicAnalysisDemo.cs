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

namespace Minotaur.Demo;

/// <summary>
/// Demonstrates the symbolic analysis capabilities as an alternative to KLEE LLVM integration
/// </summary>
public class SymbolicAnalysisDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("=== Minotaur Symbolic Analysis Demo ===");
        Console.WriteLine("Demonstrating KLEE-like capabilities without external dependencies\n");

        var engine = new SymbolicAnalysisEngine();

        // Demo 1: Python analysis
        Console.WriteLine("1. Python Code Analysis:");
        RunPythonDemo(engine);
        Console.WriteLine();

        // Demo 2: JavaScript analysis
        Console.WriteLine("2. JavaScript Code Analysis:");
        RunJavaScriptDemo(engine);
        Console.WriteLine();

        // Demo 3: C# analysis
        Console.WriteLine("3. C# Code Analysis:");
        RunCSharpDemo(engine);
        Console.WriteLine();

        // Demo 4: Comparative analysis
        Console.WriteLine("4. Comparison with KLEE approach:");
        ShowComparisonAnalysis();
        Console.WriteLine();
    }

    private static void RunPythonDemo(SymbolicAnalysisEngine engine)
    {
        var pythonCode = @"
def process_array(arr, index):
    if arr is None:
        return None.length  # Null pointer access
    
    if index < 0:
        return arr[-10]  # Potential bounds issue
    
    return arr[index] / 0  # Division by zero
";

        Console.WriteLine("Python code to analyze:");
        Console.WriteLine(pythonCode);

        var result = engine.AnalyzeCode(pythonCode, "python");
        PrintAnalysisResult(result);
    }

    private static void RunJavaScriptDemo(SymbolicAnalysisEngine engine)
    {
        var jsCode = @"
function processData(obj) {
    if (obj == null) {
        return null.property;  // Null access
    }
    
    if (obj.value === undefined) {
        return undefined.toString();  // Undefined access
    }
    
    return obj.value + """";  // Type coercion
}
";

        Console.WriteLine("JavaScript code to analyze:");
        Console.WriteLine(jsCode);

        var result = engine.AnalyzeCode(jsCode, "javascript");
        PrintAnalysisResult(result);
    }

    private static void RunCSharpDemo(SymbolicAnalysisEngine engine)
    {
        var csharpCode = @"
public void ProcessFile(string filename)
{
    string content = null;
    Console.WriteLine(content.Length);  // Null reference
    
    var stream = new FileStream(filename, FileMode.Open);
    // No using statement - potential resource leak
    
    int[] array = new int[5];
    Console.WriteLine(array[10]);  // Index out of bounds
}
";

        Console.WriteLine("C# code to analyze:");
        Console.WriteLine(csharpCode);

        var result = engine.AnalyzeCode(csharpCode, "csharp");
        PrintAnalysisResult(result);
    }

    private static void PrintAnalysisResult(SymbolicAnalysisResult result)
    {
        Console.WriteLine($"Analysis completed in {result.AnalysisTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Errors found: {result.Errors.Count}");
        Console.WriteLine($"Execution paths: {result.ExecutionPaths.Count}");
        Console.WriteLine($"Constraints: {result.Constraints.Count}");

        if (result.Errors.Any())
        {
            Console.WriteLine("\nDetected Errors:");
            foreach (var error in result.Errors.OrderByDescending(e => e.Confidence))
            {
                Console.WriteLine($"  - {error.Type} at {error.Location}: {error.Message}");
                Console.WriteLine($"    Confidence: {error.Confidence:P1}, Severity: {error.Severity}");
                
                if (error.TestCases.Any())
                {
                    Console.WriteLine($"    Test cases generated: {error.TestCases.Count}");
                }
            }
        }

        if (result.Constraints.Any())
        {
            Console.WriteLine("\nConstraints Extracted:");
            var constraintGroups = result.Constraints.GroupBy(c => c.Type);
            foreach (var group in constraintGroups)
            {
                Console.WriteLine($"  - {group.Key}: {group.Count()} constraints");
            }
        }
    }

    private static void ShowComparisonAnalysis()
    {
        Console.WriteLine("KLEE vs Minotaur Symbolic Analysis Comparison:");
        Console.WriteLine();

        var comparison = new[]
        {
            ("Integration Complexity", "KLEE: High (LLVM IR bridge required)", "Minotaur: Native (seamless)"),
            ("Language Support", "KLEE: C/C++ focused", "Minotaur: Any language parsed by StepParser"),
            ("Setup Requirements", "KLEE: LLVM toolchain, SMT solvers", "Minotaur: None (self-contained)"),
            ("Performance", "KLEE: Translation overhead", "Minotaur: Direct AST analysis"),
            ("Maintenance", "KLEE: External dependency updates", "Minotaur: Internal codebase control"),
            ("Grammar Integration", "KLEE: Not applicable", "Minotaur: Native grammar-aware analysis"),
            ("Error Context", "KLEE: Binary-level", "Minotaur: Source-level with AST context")
        };

        foreach (var (aspect, kleeValue, minotaurValue) in comparison)
        {
            Console.WriteLine($"{aspect,-20} | {kleeValue,-35} | {minotaurValue}");
        }

        Console.WriteLine();
        Console.WriteLine("Recommendation: Use Minotaur's native symbolic analysis for:");
        Console.WriteLine("✓ Better architectural alignment");
        Console.WriteLine("✓ Reduced complexity and dependencies");
        Console.WriteLine("✓ Superior multi-language support");
        Console.WriteLine("✓ Grammar-driven error analysis");
        Console.WriteLine("✓ Faster development and deployment");
    }

    /// <summary>
    /// Demonstrates integration with existing ParseErrorAnalyzer
    /// </summary>
    public static void DemonstrateGrammarIntegration()
    {
        Console.WriteLine("\n=== Grammar-Integrated Symbolic Analysis ===");
        
        var engine = new SymbolicAnalysisEngine();
        
        // Example of how symbolic analysis could enhance grammar error detection
        var codeWithGrammarIssues = @"
// Missing semicolon
var x = 5
console.log(x)

// Unmatched parentheses  
if (x > 0 {
    console.log('positive')
}
";

        Console.WriteLine("Code with both symbolic and grammar issues:");
        Console.WriteLine(codeWithGrammarIssues);

        var result = engine.AnalyzeCode(codeWithGrammarIssues, "javascript");
        
        Console.WriteLine("\nSymbolic analysis results:");
        PrintAnalysisResult(result);

        Console.WriteLine("\nHow this integrates with ParseErrorAnalyzer:");
        Console.WriteLine("1. ParseErrorAnalyzer detects grammar/syntax issues");
        Console.WriteLine("2. SymbolicAnalysisEngine detects runtime behavior issues");
        Console.WriteLine("3. Both feed into unified error reporting and correction system");
        Console.WriteLine("4. Grammar context improves symbolic analysis accuracy");
    }
}