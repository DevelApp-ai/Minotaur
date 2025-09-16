using GolemCognitiveGraph.Core;

namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Built-in C# language plugin for unparsing ONLY - no grammar or syntax handling
/// All grammar and syntax comes from StepParser - plugins only generate formatted output
/// </summary>
public class CSharpLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "csharp";
    public string DisplayName => "C#";
    public string[] SupportedExtensions => new[] { ".cs", ".csx" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        // Generate C# source code from cognitive graph
        // All syntax and grammar knowledge comes from StepParser - this only formats output
        var visitor = new CSharpUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\r\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["BraceNewLine"] = true,  // Cosmetic only - not syntax
                ["SpaceAfterComma"] = true,
                ["SpaceAroundOperators"] = true
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };

        // Only validate that we can generate output - no syntax validation (that's StepParser's job)
        if (graph == null)
        {
            result.CanUnparse = false;
            result.Errors.Add(new UnparseValidationError
            {
                Message = "Cannot unparse null graph",
                NodeId = "null",
                NodeType = "null"
            });
        }

        await Task.CompletedTask;
        return result;
    }
}

/// <summary>
/// Built-in JavaScript language plugin for unparsing ONLY
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

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 2,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 100,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["SemicolonInsertion"] = true, // Cosmetic ASI behavior
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
/// Built-in Python language plugin for unparsing ONLY
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

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 4,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 88, // Black formatter default
            CosmeticOptions = new Dictionary<string, object>
            {
                ["QuoteStyle"] = "double",
                ["BlackCompatible"] = true
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