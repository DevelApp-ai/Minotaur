using GolemCognitiveGraph.Core;

namespace GolemCognitiveGraph.Plugins;

/// <summary>
/// Built-in C# language plugin implementation
/// </summary>
public class CSharpLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "csharp";
    public string DisplayName => "C#";
    public string[] SupportedExtensions => new[] { ".cs", ".csx" };

    public async Task<IEnumerable<LanguageToken>> TokenizeAsync(string sourceCode)
    {
        // Integrate with DevelApp.StepLexer for C# tokenization
        var tokens = new List<LanguageToken>();

        // Basic tokenization for demonstration - will be enhanced with StepLexer integration
        var words = sourceCode.Split(new[] { ' ', '\t', '\r', '\n', ';', '(', ')', '{', '}', '[', ']' },
                                    StringSplitOptions.RemoveEmptyEntries);

        int position = 0;
        int line = 1;
        int column = 1;

        foreach (var word in words)
        {
            var token = new LanguageToken
            {
                Text = word,
                Type = DetermineTokenType(word),
                StartPosition = position,
                EndPosition = position + word.Length,
                Line = line,
                Column = column
            };

            tokens.Add(token);
            position += word.Length;
            column += word.Length;
        }

        await Task.CompletedTask;
        return tokens;
    }

    public async Task<CognitiveGraphNode> ParseAsync(string sourceCode)
    {
        // Integrate with DevelApp.StepParser for C# parsing
        var root = new NonTerminalNode("compilation_unit", 0);
        root.Metadata["language"] = LanguageId;
        root.Metadata["source"] = sourceCode;

        // Basic parsing structure for demonstration
        var @namespace = new NonTerminalNode("namespace_declaration", 0);
        var @class = new NonTerminalNode("class_declaration", 0);
        var method = new NonTerminalNode("method_declaration", 0);

        @class.AddChild(method);
        @namespace.AddChild(@class);
        root.AddChild(@namespace);

        await Task.CompletedTask;
        return root;
    }

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        // Generate C# source code from cognitive graph
        var visitor = new CSharpUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<ValidationResult> ValidateAsync(string sourceCode)
    {
        var result = new ValidationResult { IsValid = true };

        // Basic C# syntax validation
        var braceCount = sourceCode.Count(c => c == '{') - sourceCode.Count(c => c == '}');
        var parenCount = sourceCode.Count(c => c == '(') - sourceCode.Count(c => c == ')');

        if (braceCount != 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Message = "Unbalanced braces",
                Code = "CS0116",
                Severity = "Error"
            });
        }

        if (parenCount != 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Message = "Unbalanced parentheses",
                Code = "CS1026",
                Severity = "Error"
            });
        }

        await Task.CompletedTask;
        return result;
    }

    private string DetermineTokenType(string word)
    {
        var keywords = new[] { "using", "namespace", "class", "public", "private", "static", "void", "string", "int", "bool" };

        if (keywords.Contains(word.ToLowerInvariant()))
            return "keyword";

        if (char.IsLetter(word.FirstOrDefault()))
            return "identifier";

        if (char.IsDigit(word.FirstOrDefault()))
            return "literal";

        return "operator";
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

    public async Task<IEnumerable<LanguageToken>> TokenizeAsync(string sourceCode)
    {
        // Similar implementation for JavaScript
        await Task.CompletedTask;
        return new List<LanguageToken>();
    }

    public async Task<CognitiveGraphNode> ParseAsync(string sourceCode)
    {
        var root = new NonTerminalNode("program", 0);
        root.Metadata["language"] = LanguageId;
        root.Metadata["source"] = sourceCode;

        await Task.CompletedTask;
        return root;
    }

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new JavaScriptUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<ValidationResult> ValidateAsync(string sourceCode)
    {
        await Task.CompletedTask;
        return new ValidationResult { IsValid = true };
    }
}

/// <summary>
/// Built-in Python language plugin implementation
/// </summary>
public class PythonLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "python";
    public string DisplayName => "Python";
    public string[] SupportedExtensions => new[] { ".py", ".pyw", ".pyx" };

    public async Task<IEnumerable<LanguageToken>> TokenizeAsync(string sourceCode)
    {
        // Python-specific tokenization
        await Task.CompletedTask;
        return new List<LanguageToken>();
    }

    public async Task<CognitiveGraphNode> ParseAsync(string sourceCode)
    {
        var root = new NonTerminalNode("module", 0);
        root.Metadata["language"] = LanguageId;
        root.Metadata["source"] = sourceCode;

        await Task.CompletedTask;
        return root;
    }

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        var visitor = new PythonUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedCode();
    }

    public async Task<ValidationResult> ValidateAsync(string sourceCode)
    {
        await Task.CompletedTask;
        return new ValidationResult { IsValid = true };
    }
}