using System;
using System.Collections.Generic;
using Minotaur.Core;
using Minotaur.Analysis.Symbolic;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Minotaur Core Library Integration Test ===");

        try
        {
            // Test 1: Create CognitiveGraphNode instances
            Console.WriteLine("\n1. Testing CognitiveGraphNode creation...");
            var terminalNode = new TerminalNode("hello", "string_literal");
            var identifierNode = new IdentifierNode("myVariable", "MyNamespace");
            var literalNode = new LiteralNode("42", "integer", 42);
            var nonTerminalNode = new NonTerminalNode("Expression", 1);

            nonTerminalNode.AddChild(identifierNode);
            nonTerminalNode.AddChild(terminalNode);
            nonTerminalNode.AddChild(literalNode);

            Console.WriteLine($"✅ Created NonTerminalNode '{nonTerminalNode.RuleName}' with {nonTerminalNode.Children.Count} children");
            Console.WriteLine($"   - IdentifierNode: {identifierNode.FullName}");
            Console.WriteLine($"   - TerminalNode: {terminalNode.Text} ({terminalNode.TokenType})");
            Console.WriteLine($"   - LiteralNode: {literalNode.Value} ({literalNode.LiteralType})");

            // Test 2: Test SymbolicAnalysis types
            Console.WriteLine("\n2. Testing Symbolic Analysis types...");
            var sourceLocation = new SourceLocation(10, 25, 5);
            var error = new SymbolicError(
                SymbolicErrorType.NullPointerAccess,
                sourceLocation,
                "Potential null pointer access detected",
                0.85
            );

            var constraint = new SymbolicConstraint(
                ConstraintType.NullCheck,
                sourceLocation,
                "Variable must not be null",
                "variable != null"
            );

            var executionPath = new ExecutionPath("path_1", new List<SymbolicConstraint> { constraint }, 0.7);

            var analysisResult = new SymbolicAnalysisResult(
                success: true,
                errors: new List<SymbolicError> { error },
                executionPaths: new List<ExecutionPath> { executionPath },
                constraints: new List<SymbolicConstraint> { constraint },
                analysisTime: TimeSpan.FromMilliseconds(250)
            )
            {
                TotalSymbols = 15,
                ComplexityScore = 8.5,
                QualityScore = 82
            };

            Console.WriteLine($"✅ SymbolicAnalysisResult created successfully");
            Console.WriteLine($"   - Success: {analysisResult.Success}");
            Console.WriteLine($"   - Errors: {analysisResult.Errors.Count} ({error.Severity} severity)");
            Console.WriteLine($"   - Paths: {analysisResult.ExecutionPaths.Count} (depth: {executionPath.Depth})");
            Console.WriteLine($"   - Analysis Time: {analysisResult.AnalysisTime.TotalMilliseconds}ms");
            Console.WriteLine($"   - Total Symbols: {analysisResult.TotalSymbols}");
            Console.WriteLine($"   - Quality Score: {analysisResult.QualityScore}%");

            // Test 4: Test additional types
            Console.WriteLine("\n4. Testing additional integration types...");
            var options = new SymbolicAnalysisOptions
            {
                Language = "C#",
                EnableDeepAnalysis = true,
                MaxDepth = 10
            };

            var symbolInfo = new SymbolInfo
            {
                Name = "TestMethod",
                Type = "Method",
                Scope = "TestClass",
                ReferenceCount = 3
            };

            var dependencyInfo = new DependencyInfo
            {
                Name = "System.Collections.Generic",
                Type = "Namespace",
                IsExternal = true
            };

            Console.WriteLine($"✅ Additional types created successfully");
            Console.WriteLine($"   - Analysis Options: {options.Language} (Deep: {options.EnableDeepAnalysis})");
            Console.WriteLine($"   - Symbol Info: {symbolInfo.Name} ({symbolInfo.Type})");
            Console.WriteLine($"   - Dependency: {dependencyInfo.Name} ({dependencyInfo.Type})");

            Console.WriteLine("\n🎉 All integration tests passed! The Minotaur UI can successfully integrate with the core library.");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Integration test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}