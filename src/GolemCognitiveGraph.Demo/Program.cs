using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;
using GolemCognitiveGraph.Unparser;

namespace GolemCognitiveGraph.Demo;

/// <summary>
/// Demonstration of the Golem Cognitive Graph Editor & Unparser functionality.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Golem Cognitive Graph Editor & Unparser Demo ===\n");

        // Demo 1: Basic graph construction and editing
        await DemoBasicGraphEditing();
        
        // Demo 2: Code unparsing
        await DemoCodeUnparsing();
        
        // Demo 3: Advanced editing operations
        await DemoAdvancedEditing();

        Console.WriteLine("\nDemo completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task DemoBasicGraphEditing()
    {
        Console.WriteLine("1. Basic Graph Editing Demo");
        Console.WriteLine("===========================");

        // Create a simple expression: x + 5
        var root = new NonTerminalNode("expression", 0);
        Console.WriteLine($"Created root node: {root}");

        using var editor = new GraphEditor(root);

        // Add left operand
        var leftOperand = new IdentifierNode("x");
        editor.InsertNode(root.Id, leftOperand);
        Console.WriteLine($"Added left operand: {leftOperand}");

        // Add operator
        var plusOperator = new TerminalNode("+", "operator");
        plusOperator.Metadata["precedence"] = "5";
        editor.InsertNode(root.Id, plusOperator);
        Console.WriteLine($"Added operator: {plusOperator}");

        // Add right operand
        var rightOperand = new LiteralNode("5", "number", 5);
        editor.InsertNode(root.Id, rightOperand);
        Console.WriteLine($"Added right operand: {rightOperand}");

        Console.WriteLine($"\nGraph structure: {root.Children.Count} children");
        for (int i = 0; i < root.Children.Count; i++)
        {
            Console.WriteLine($"  [{i}] {root.Children[i]}");
        }

        // Demonstrate undo/redo
        Console.WriteLine("\nTesting undo/redo...");
        Console.WriteLine($"Can undo: {editor.CanUndo}");
        
        editor.Undo(); // Remove right operand
        Console.WriteLine($"After undo: {root.Children.Count} children");
        
        editor.Redo(); // Add right operand back
        Console.WriteLine($"After redo: {root.Children.Count} children");

        Console.WriteLine();
    }

    static async Task DemoCodeUnparsing()
    {
        Console.WriteLine("2. Code Unparsing Demo");
        Console.WriteLine("======================");

        // Create a more complex expression: (x + y) * 2
        var root = new NonTerminalNode("expression");
        
        // Left parenthesized expression
        var leftExpr = new NonTerminalNode("parenthesized_expression");
        leftExpr.Metadata["blockType"] = "parentheses";
        
        var x = new IdentifierNode("x");
        var plus = new TerminalNode("+", "operator");
        var y = new IdentifierNode("y");
        
        leftExpr.AddChild(x);
        leftExpr.AddChild(plus);
        leftExpr.AddChild(y);
        
        // Multiplication operator
        var multiply = new TerminalNode("*", "operator");
        
        // Right operand
        var two = new LiteralNode("2", "number", 2);
        
        root.AddChild(leftExpr);
        root.AddChild(multiply);
        root.AddChild(two);

        // Configure unparser
        var config = new UnparseConfiguration
        {
            FormatOutput = true,
            IncludeComments = true,
            MaxLineLength = 80
        };

        using var unparser = new GraphUnparser(config);

        // Unparse to code
        var code = await unparser.UnparseAsync(root);
        Console.WriteLine("Generated code:");
        Console.WriteLine($"  {code.Trim()}");

        // Add a comment and unparse again
        var comment = new TerminalNode("Calculate result", "comment");
        comment.Metadata["commentType"] = "line";
        
        var commentedRoot = new NonTerminalNode("commented_expression");
        commentedRoot.AddChild(comment);
        commentedRoot.AddChild(root);

        var codeWithComment = await unparser.UnparseAsync(commentedRoot);
        Console.WriteLine("\nGenerated code with comment:");
        Console.WriteLine($"  {codeWithComment.Trim()}");

        Console.WriteLine();
    }

    static async Task DemoAdvancedEditing()
    {
        Console.WriteLine("3. Advanced Editing Operations Demo");
        Console.WriteLine("===================================");

        // Create a method structure
        var method = new NonTerminalNode("method", 0);
        method.Metadata["methodName"] = "CalculateSum";

        using var editor = new GraphEditor(method);

        // Add method signature
        var signature = new NonTerminalNode("signature");
        var returnType = new IdentifierNode("int");
        var methodName = new IdentifierNode("CalculateSum");
        var parameters = new NonTerminalNode("parameters");
        
        signature.AddChild(returnType);
        signature.AddChild(methodName);
        signature.AddChild(parameters);
        
        editor.InsertNode(method.Id, signature);
        Console.WriteLine("Added method signature");

        // Add method body
        var body = new NonTerminalNode("body");
        body.Metadata["blockType"] = "braces";
        
        editor.InsertNode(method.Id, body);
        Console.WriteLine("Added method body");

        // Add statements to body
        var returnStmt = new NonTerminalNode("return_statement");
        var returnKeyword = new TerminalNode("return", "keyword");
        var returnExpr = new NonTerminalNode("expression");
        
        // Create: a + b
        var a = new IdentifierNode("a");
        var plus = new TerminalNode("+", "operator");
        var b = new IdentifierNode("b");
        
        returnExpr.AddChild(a);
        returnExpr.AddChild(plus);
        returnExpr.AddChild(b);
        
        returnStmt.AddChild(returnKeyword);
        returnStmt.AddChild(returnExpr);
        
        editor.InsertNode(body.Id, returnStmt);
        Console.WriteLine("Added return statement: return a + b;");

        // Demonstrate node replacement
        Console.WriteLine("\nReplacing 'a + b' with 'x * y'...");
        var newExpr = new NonTerminalNode("expression");
        var x = new IdentifierNode("x");
        var multiply = new TerminalNode("*", "operator");
        var y = new IdentifierNode("y");
        
        newExpr.AddChild(x);
        newExpr.AddChild(multiply);
        newExpr.AddChild(y);
        
        editor.ReplaceNode(returnExpr.Id, newExpr, preserveChildren: false);
        Console.WriteLine("Replacement completed");

        // Unparse the entire method
        using var unparser = new GraphUnparser();
        var methodCode = await unparser.UnparseAsync(method);
        Console.WriteLine("\nGenerated method structure:");
        Console.WriteLine($"  {methodCode.Trim()}");

        // Display editing history
        Console.WriteLine($"\nEditing capabilities:");
        Console.WriteLine($"  Can undo: {editor.CanUndo}");
        Console.WriteLine($"  Can redo: {editor.CanRedo}");

        // Find nodes by ID
        var foundSignature = editor.FindNode(signature.Id);
        Console.WriteLine($"  Found signature node: {foundSignature != null}");

        Console.WriteLine();
    }
}