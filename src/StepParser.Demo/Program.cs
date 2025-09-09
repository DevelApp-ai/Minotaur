using StepParser.Core;
using StepParser.Grammar;
using StepParser.Lexer;

namespace StepParser.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("StepParser C# Demo");
        Console.WriteLine("==================");

        await DemonstrateBasicParsing();
        await DemonstrateContextSwitching();
        await DemonstrateSemanticActions();
        await DemonstrateGrammarInheritance();

        Console.WriteLine("\nDemo completed successfully!");
    }

    static async Task DemonstrateBasicParsing()
    {
        Console.WriteLine("\n1. Basic Parsing Demo");
        Console.WriteLine("--------------------");

        using var parser = new StepParser.Core.StepParser();

        // Create a simple grammar for identifiers
        var grammar = new Grammar.Grammar("SimpleGrammar");

        // Define terminals
        var identifierTerminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
        var numberTerminal = new Terminal("number", @"\d+");

        grammar.AddValidStartTerminal(identifierTerminal);
        grammar.AddValidStartTerminal(numberTerminal);

        // Define productions
        var identifierProduction = new Production("identifier_rule");
        identifierProduction.AddPart(identifierTerminal);

        var numberProduction = new Production("number_rule");
        numberProduction.AddPart(numberTerminal);

        grammar.AddProduction(identifierProduction);
        grammar.AddProduction(numberProduction);
        grammar.AddStartProduction(identifierProduction);
        grammar.AddStartProduction(numberProduction);

        parser.SetActiveGrammar(grammar);

        var sourceContainer = new SourceContainer("testVariable");

        try
        {
            var matches = await parser.ParseAsync("SimpleGrammar", sourceContainer);
            Console.WriteLine($"✓ Parsed input successfully. Found {matches.Count} matches.");
            Console.WriteLine($"✓ Grammar has {grammar.Productions.Count} productions and {grammar.ValidStartTerminals.Count} start terminals.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Parsing failed: {ex.Message}");
        }
    }

    static async Task DemonstrateContextSwitching()
    {
        Console.WriteLine("\n2. Context Switching Demo");
        Console.WriteLine("-------------------------");

        using var parser = new StepParser.Core.StepParser();

        // Set up different contexts
        parser.SetContextState("inside_function", false);
        parser.SetContextState("inside_class", false);

        Console.WriteLine($"Initial context - inside_function: {parser.GetContextState("inside_function")}");
        Console.WriteLine($"Initial context - inside_class: {parser.GetContextState("inside_class")}");

        // Simulate entering a function
        parser.SetContextState("inside_function", true);
        Console.WriteLine($"After entering function - inside_function: {parser.GetContextState("inside_function")}");

        // Simulate entering a class within the function
        parser.SetContextState("inside_class", true);
        Console.WriteLine($"After entering class - inside_class: {parser.GetContextState("inside_class")}");

        // Simulate leaving contexts
        parser.SetContextState("inside_class", false);
        parser.SetContextState("inside_function", false);
        Console.WriteLine($"After leaving both - inside_function: {parser.GetContextState("inside_function")}, inside_class: {parser.GetContextState("inside_class")}");

        Console.WriteLine("✓ Context switching demonstrated successfully.");

        await Task.Delay(1); // Suppress async warning
    }

    static async Task DemonstrateSemanticActions()
    {
        Console.WriteLine("\n3. Semantic Actions Demo");
        Console.WriteLine("------------------------");

        using var parser = new StepParser.Core.StepParser();

        var callbackCount = 0;

        // Register a callback for a production
        parser.RegisterCallback("variable_declaration", (context) =>
        {
            callbackCount++;
            Console.WriteLine($"  Callback executed for variable_declaration #{callbackCount}");

            if (context.TryGetValue("token", out var token))
            {
                Console.WriteLine($"  Token: {token}");
            }

            if (context.TryGetValue("grammarName", out var grammarName))
            {
                Console.WriteLine($"  Grammar: {grammarName}");
            }
        });

        // Set callback context
        parser.SetCallbackContext(new Dictionary<string, object>
        {
            ["demo_mode"] = true,
            ["debug_level"] = 2
        });

        var context = parser.GetCallbackContext();
        Console.WriteLine($"Callback context set with {context.Count} items");

        Console.WriteLine("✓ Semantic actions configured successfully.");

        await Task.Delay(1); // Suppress async warning
    }

    static async Task DemonstrateGrammarInheritance()
    {
        Console.WriteLine("\n4. Grammar Inheritance Demo");
        Console.WriteLine("---------------------------");

        // Create a base grammar
        var baseGrammar = new Grammar.Grammar("BaseLanguage");
        baseGrammar.IsInheritable = true;
        baseGrammar.FormatType = GrammarFormatType.CEBNF;

        // Add some basic terminals and productions
        var identifierTerminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
        var numberTerminal = new Terminal("number", @"\d+");

        baseGrammar.AddValidStartTerminal(identifierTerminal);
        baseGrammar.AddValidStartTerminal(numberTerminal);

        // Create a derived grammar
        var derivedGrammar = new Grammar.Grammar("ExtendedLanguage");
        derivedGrammar.AddBaseGrammar("BaseLanguage");
        derivedGrammar.ImportSemantics = true;

        // Add additional features
        var stringTerminal = new Terminal("string", "\"[^\"]*\"");
        derivedGrammar.AddValidStartTerminal(stringTerminal);

        // Add precedence rules
        var precedenceRule = new PrecedenceRule(1, new[] { "+", "-" }, AssociativityType.Left, "Addition and subtraction");
        derivedGrammar.AddPrecedenceRule(precedenceRule);

        var higherPrecedenceRule = new PrecedenceRule(2, new[] { "*", "/" }, AssociativityType.Left, "Multiplication and division");
        derivedGrammar.AddPrecedenceRule(higherPrecedenceRule);

        Console.WriteLine($"✓ Base grammar '{baseGrammar.Name}' created with {baseGrammar.ValidStartTerminals.Count} terminals");
        Console.WriteLine($"✓ Derived grammar '{derivedGrammar.Name}' inherits from {string.Join(", ", derivedGrammar.BaseGrammars)}");
        Console.WriteLine($"✓ Derived grammar has {derivedGrammar.PrecedenceRules.Count} precedence rules");
        Console.WriteLine($"✓ Grammar inheritance: {derivedGrammar.InheritsFrom("BaseLanguage")}");

        await Task.Delay(1); // Suppress async warning
    }
}
