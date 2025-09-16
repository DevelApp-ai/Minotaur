using GolemCognitiveGraph.Core;

namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Built-in C# language plugin implementation for unparsing and compiler-compiler generation
/// </summary>
public class CSharpLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "csharp";
    public string DisplayName => "C#";
    public string[] SupportedExtensions => new[] { ".cs", ".csx" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        // Generate C# source code from cognitive graph
        var visitor = new CSharpUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<CompilerGeneratorRules> GenerateCompilerRulesAsync()
    {
        var rules = new CompilerGeneratorRules
        {
            LanguageId = LanguageId
        };

        // Add C# grammar production rules for compiler-compiler generation
        rules.ProductionRules.AddRange(new[]
        {
            new GrammarRule
            {
                NonTerminal = "compilation_unit",
                Productions = new List<string> { "using_directives namespace_declaration*" }
            },
            new GrammarRule
            {
                NonTerminal = "class_declaration",
                Productions = new List<string> { "class IDENTIFIER '{' class_member* '}'" }
            },
            new GrammarRule
            {
                NonTerminal = "method_declaration",
                Productions = new List<string> { "type IDENTIFIER '(' parameter_list? ')' method_body" }
            }
        });

        // Add C# lexical rules
        rules.LexicalRules.AddRange(new[]
        {
            new LexicalRule { TokenType = "IDENTIFIER", Pattern = @"[a-zA-Z_][a-zA-Z0-9_]*", Priority = 1 },
            new LexicalRule { TokenType = "STRING", Pattern = @"""([^""\\]|\\.)*""", Priority = 2 },
            new LexicalRule { TokenType = "NUMBER", Pattern = @"\d+(\.\d+)?", Priority = 3 }
        });

        await Task.CompletedTask;
        return rules;
    }

    public LanguageFormattingOptions GetFormattingOptions()
    {
        return new LanguageFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\r\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            LanguageSpecific = new Dictionary<string, object>
            {
                ["BraceStyle"] = "Allman",
                ["UseTabs"] = false,
                ["SpaceAfterKeywords"] = true
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };

        // Validate that the graph structure is compatible with C# syntax
        if (graph.NodeType != "compilation_unit" && graph.NodeType != "class_declaration" &&
            graph.NodeType != "method_declaration" && graph.NodeType != "expression")
        {
            result.Warnings.Add(new UnparseValidationWarning
            {
                Message = $"Root node type '{graph.NodeType}' may not generate valid C# code",
                NodeId = graph.Id.ToString(),
                NodeType = graph.NodeType
            });
        }

        await Task.CompletedTask;
        return result;
    }
}

/// <summary>
/// Built-in JavaScript language plugin implementation
/// </summary>
public class JavaScriptLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "javascript";
    public string DisplayName => "JavaScript";
    public string[] SupportedExtensions => new[] { ".js", ".mjs", ".jsx" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new JavaScriptUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<CompilerGeneratorRules> GenerateCompilerRulesAsync()
    {
        var rules = new CompilerGeneratorRules
        {
            LanguageId = LanguageId
        };

        // Add JavaScript grammar rules
        rules.ProductionRules.AddRange(new[]
        {
            new GrammarRule
            {
                NonTerminal = "program",
                Productions = new List<string> { "statement_list" }
            },
            new GrammarRule
            {
                NonTerminal = "function_declaration",
                Productions = new List<string> { "function IDENTIFIER '(' parameter_list? ')' '{' statement_list '}'" }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    public LanguageFormattingOptions GetFormattingOptions()
    {
        return new LanguageFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 2,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 100,
            LanguageSpecific = new Dictionary<string, object>
            {
                ["SemicolonStyle"] = "ASI", // Automatic Semicolon Insertion
                ["QuoteStyle"] = "single"
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };
        await Task.CompletedTask;
        return result;
    }
}

/// <summary>
/// Built-in Python language plugin implementation
/// </summary>
public class PythonLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "python";
    public string DisplayName => "Python";
    public string[] SupportedExtensions => new[] { ".py", ".pyw" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new PythonUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<CompilerGeneratorRules> GenerateCompilerRulesAsync()
    {
        var rules = new CompilerGeneratorRules
        {
            LanguageId = LanguageId
        };

        // Add Python grammar rules
        rules.ProductionRules.AddRange(new[]
        {
            new GrammarRule
            {
                NonTerminal = "file_input",
                Productions = new List<string> { "stmt*" }
            },
            new GrammarRule
            {
                NonTerminal = "funcdef",
                Productions = new List<string> { "'def' NAME '(' parameters ')' ':' suite" }
            }
        });

        await Task.CompletedTask;
        return rules;
    }

    public LanguageFormattingOptions GetFormattingOptions()
    {
        return new LanguageFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 88, // Black formatter default
            LanguageSpecific = new Dictionary<string, object>
            {
                ["UseBlackFormatting"] = true,
                ["QuoteStyle"] = "double"
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };
        await Task.CompletedTask;
        return result;
    }
}