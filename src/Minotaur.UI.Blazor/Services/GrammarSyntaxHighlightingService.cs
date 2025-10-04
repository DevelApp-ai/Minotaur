/*
 * Grammar-based Syntax Highlighting Service for Minotaur
 * Provides real-time syntax highlighting based on parsed grammar rules
 */

using System.Text.Json;
using System.Text.RegularExpressions;
using Minotaur.Projects.Grammar;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for providing grammar-based syntax highlighting and tokenization
/// </summary>
public class GrammarSyntaxHighlightingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GrammarSyntaxHighlightingService> _logger;

    // Cache for parsed grammar rules and highlighting patterns
    private readonly Dictionary<string, GrammarHighlightingRules> _grammarCache = new();
    private readonly Dictionary<string, List<SyntaxToken>> _tokenCache = new();

    public GrammarSyntaxHighlightingService(
        HttpClient httpClient,
        ILogger<GrammarSyntaxHighlightingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Analyze and tokenize content based on grammar rules
    /// </summary>
    public async Task<List<SyntaxToken>> TokenizeContentAsync(string content, string grammarName, GrammarVersion? version = null)
    {
        try
        {
            var cacheKey = $"{grammarName}:{version?.ToString() ?? "latest"}:{content.GetHashCode()}";

            // Check cache first
            if (_tokenCache.TryGetValue(cacheKey, out var cachedTokens))
            {
                return cachedTokens;
            }

            // Get or create grammar highlighting rules
            var rules = await GetGrammarHighlightingRulesAsync(grammarName, version);

            // Tokenize content using grammar rules
            var tokens = TokenizeWithGrammar(content, rules);

            // Cache results (with size limit)
            if (_tokenCache.Count > 1000)
            {
                _tokenCache.Clear();
            }
            _tokenCache[cacheKey] = tokens;

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tokenizing content with grammar {GrammarName}", grammarName);
            return GetFallbackTokens(content);
        }
    }

    /// <summary>
    /// Get syntax highlighting CSS classes for content
    /// </summary>
    public async Task<string> GetHighlightedHtmlAsync(string content, string grammarName, GrammarVersion? version = null)
    {
        var tokens = await TokenizeContentAsync(content, grammarName, version);
        return GenerateHighlightedHtml(content, tokens);
    }

    /// <summary>
    /// Get grammar-based code completion suggestions
    /// </summary>
    public async Task<List<CompletionItem>> GetCompletionSuggestionsAsync(
        string content,
        int position,
        string grammarName,
        GrammarVersion? version = null)
    {
        try
        {
            var rules = await GetGrammarHighlightingRulesAsync(grammarName, version);
            var context = AnalyzeContext(content, position);

            return GenerateCompletions(context, rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completions for grammar {GrammarName}", grammarName);
            return new List<CompletionItem>();
        }
    }

    /// <summary>
    /// Get real-time syntax validation errors
    /// </summary>
    public async Task<List<SyntaxError>> ValidateSyntaxAsync(string content, string grammarName, GrammarVersion? version = null)
    {
        try
        {
            var rules = await GetGrammarHighlightingRulesAsync(grammarName, version);
            return ValidateWithGrammar(content, rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating syntax for grammar {GrammarName}", grammarName);
            return new List<SyntaxError>();
        }
    }

    /// <summary>
    /// Get or create grammar highlighting rules
    /// </summary>
    private async Task<GrammarHighlightingRules> GetGrammarHighlightingRulesAsync(string grammarName, GrammarVersion? version)
    {
        var cacheKey = $"{grammarName}:{version?.ToString() ?? "latest"}";

        if (_grammarCache.TryGetValue(cacheKey, out var cachedRules))
        {
            return cachedRules;
        }

        // Try to fetch grammar from API
        var rules = await FetchGrammarRulesFromApiAsync(grammarName, version)
                    ?? GenerateDefaultRulesForGrammar(grammarName);

        _grammarCache[cacheKey] = rules;
        return rules;
    }

    /// <summary>
    /// Fetch grammar rules from API or marketplace
    /// </summary>
    private async Task<GrammarHighlightingRules?> FetchGrammarRulesFromApiAsync(string grammarName, GrammarVersion? version)
    {
        try
        {
            // In a real implementation, this would call the grammar service API
            var response = await _httpClient.GetAsync($"/api/grammar/{grammarName}/highlighting-rules?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GrammarHighlightingRules>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch grammar rules from API for {GrammarName}", grammarName);
        }

        return null;
    }

    /// <summary>
    /// Generate default highlighting rules based on common patterns
    /// </summary>
    private GrammarHighlightingRules GenerateDefaultRulesForGrammar(string grammarName)
    {
        var rules = new GrammarHighlightingRules { GrammarName = grammarName };

        // Add common patterns based on grammar name
        if (grammarName.Contains("CSharp", StringComparison.OrdinalIgnoreCase) ||
            grammarName.Contains("C#", StringComparison.OrdinalIgnoreCase))
        {
            rules.TokenPatterns.AddRange(GetCSharpPatterns());
            rules.Keywords.AddRange(GetCSharpKeywords());
        }
        else if (grammarName.Contains("TypeScript", StringComparison.OrdinalIgnoreCase))
        {
            rules.TokenPatterns.AddRange(GetTypeScriptPatterns());
            rules.Keywords.AddRange(GetTypeScriptKeywords());
        }
        else if (grammarName.Contains("Python", StringComparison.OrdinalIgnoreCase))
        {
            rules.TokenPatterns.AddRange(GetPythonPatterns());
            rules.Keywords.AddRange(GetPythonKeywords());
        }
        else if (grammarName.Contains("JavaScript", StringComparison.OrdinalIgnoreCase))
        {
            rules.TokenPatterns.AddRange(GetJavaScriptPatterns());
            rules.Keywords.AddRange(GetJavaScriptKeywords());
        }
        else
        {
            // Generic programming language patterns
            rules.TokenPatterns.AddRange(GetGenericPatterns());
            rules.Keywords.AddRange(GetGenericKeywords());
        }

        return rules;
    }

    /// <summary>
    /// Tokenize content using grammar rules
    /// </summary>
    private List<SyntaxToken> TokenizeWithGrammar(string content, GrammarHighlightingRules rules)
    {
        var tokens = new List<SyntaxToken>();
        var position = 0;

        while (position < content.Length)
        {
            var token = FindNextToken(content, position, rules);
            tokens.Add(token);
            position = token.End;
        }

        return tokens;
    }

    /// <summary>
    /// Find the next token in the content
    /// </summary>
    private SyntaxToken FindNextToken(string content, int position, GrammarHighlightingRules rules)
    {
        var remainingContent = content[position..];

        // Try each token pattern in priority order
        foreach (var pattern in rules.TokenPatterns.OrderByDescending(p => p.Priority))
        {
            var match = pattern.Regex.Match(remainingContent);
            if (match.Success && match.Index == 0)
            {
                return new SyntaxToken
                {
                    Type = pattern.TokenType,
                    Value = match.Value,
                    Start = position,
                    End = position + match.Length,
                    CssClass = pattern.CssClass
                };
            }
        }

        // Check for keywords
        var wordMatch = Regex.Match(remainingContent, @"^\w+");
        if (wordMatch.Success && rules.Keywords.Contains(wordMatch.Value))
        {
            return new SyntaxToken
            {
                Type = TokenType.Keyword,
                Value = wordMatch.Value,
                Start = position,
                End = position + wordMatch.Length,
                CssClass = "keyword"
            };
        }

        // Default: single character
        return new SyntaxToken
        {
            Type = TokenType.Unknown,
            Value = content[position].ToString(),
            Start = position,
            End = position + 1,
            CssClass = "text"
        };
    }

    /// <summary>
    /// Generate highlighted HTML from tokens
    /// </summary>
    private string GenerateHighlightedHtml(string content, List<SyntaxToken> tokens)
    {
        var html = new System.Text.StringBuilder();

        foreach (var token in tokens)
        {
            var escapedValue = System.Web.HttpUtility.HtmlEncode(token.Value);
            html.Append($"<span class=\"{token.CssClass}\">{escapedValue}</span>");
        }

        return html.ToString();
    }

    /// <summary>
    /// Analyze context around cursor position
    /// </summary>
    private SyntaxCompletionContext AnalyzeContext(string content, int position)
    {
        var context = new SyntaxCompletionContext { Position = position };

        // Find current line
        var lineStart = content.LastIndexOf('\n', position - 1) + 1;
        var lineEnd = content.IndexOf('\n', position);
        if (lineEnd == -1) lineEnd = content.Length;

        context.CurrentLine = content[lineStart..lineEnd];
        context.CurrentLinePosition = position - lineStart;

        // Find current word
        var wordStart = position;
        while (wordStart > 0 && char.IsLetterOrDigit(content[wordStart - 1]))
            wordStart--;

        var wordEnd = position;
        while (wordEnd < content.Length && char.IsLetterOrDigit(content[wordEnd]))
            wordEnd++;

        context.CurrentWord = content[wordStart..wordEnd];
        context.WordStart = wordStart;

        // Analyze surrounding context
        context.PrecedingText = content[Math.Max(0, position - 100)..position];
        context.FollowingText = content[position..Math.Min(content.Length, position + 100)];

        return context;
    }

    /// <summary>
    /// Generate completion suggestions based on context
    /// </summary>
    private List<CompletionItem> GenerateCompletions(SyntaxCompletionContext context, GrammarHighlightingRules rules)
    {
        var completions = new List<CompletionItem>();

        // Add keywords that match current prefix
        foreach (var keyword in rules.Keywords)
        {
            if (keyword.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                completions.Add(new CompletionItem
                {
                    Label = keyword,
                    InsertText = keyword,
                    Kind = CompletionItemKind.Keyword,
                    Detail = "Language keyword",
                    Documentation = $"Keyword: {keyword}"
                });
            }
        }

        // Add grammar rule completions
        foreach (var completion in rules.CompletionSuggestions)
        {
            if (completion.Label.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                completions.Add(completion);
            }
        }

        return completions.OrderByDescending(c => c.Priority).ToList();
    }

    /// <summary>
    /// Validate syntax using grammar rules
    /// </summary>
    private List<SyntaxError> ValidateWithGrammar(string content, GrammarHighlightingRules rules)
    {
        var errors = new List<SyntaxError>();

        // Basic validation patterns
        foreach (var validator in rules.ValidationRules)
        {
            var matches = validator.Pattern.Matches(content);
            foreach (Match match in matches)
            {
                var line = content.Take(match.Index).Count(c => c == '\n') + 1;
                var column = match.Index - content.LastIndexOf('\n', match.Index - 1);

                errors.Add(new SyntaxError
                {
                    Message = validator.ErrorMessage,
                    Line = line,
                    Column = column,
                    Start = match.Index,
                    End = match.Index + match.Length,
                    Severity = validator.Severity
                });
            }
        }

        return errors;
    }

    /// <summary>
    /// Get fallback tokens when grammar parsing fails
    /// </summary>
    private List<SyntaxToken> GetFallbackTokens(string content)
    {
        // Simple fallback tokenization
        return new List<SyntaxToken>
        {
            new SyntaxToken
            {
                Type = TokenType.Text,
                Value = content,
                Start = 0,
                End = content.Length,
                CssClass = "text"
            }
        };
    }

    #region Language-Specific Patterns

    private List<TokenPattern> GetCSharpPatterns()
    {
        return new List<TokenPattern>
        {
            new() { Regex = new Regex(@"//.*$", RegexOptions.Multiline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"""(?:[^""\\]|\\.)*"""), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"@""(?:[^""]|"""")*"""), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"\b\d+\.?\d*\b"), TokenType = TokenType.Number, CssClass = "number", Priority = 8 },
            new() { Regex = new Regex(@"\b[A-Z][a-zA-Z0-9]*\b"), TokenType = TokenType.Type, CssClass = "type", Priority = 7 },
        };
    }

    private List<string> GetCSharpKeywords()
    {
        return new List<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
            "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "var", "virtual", "void", "volatile", "while"
        };
    }

    private List<TokenPattern> GetTypeScriptPatterns()
    {
        return new List<TokenPattern>
        {
            new() { Regex = new Regex(@"//.*$", RegexOptions.Multiline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"""(?:[^""\\]|\\.)*"""), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"'(?:[^'\\]|\\.)*'"), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"`(?:[^`\\]|\\.)*`"), TokenType = TokenType.String, CssClass = "template-string", Priority = 9 },
            new() { Regex = new Regex(@"\b\d+\.?\d*\b"), TokenType = TokenType.Number, CssClass = "number", Priority = 8 },
        };
    }

    private List<string> GetTypeScriptKeywords()
    {
        return new List<string>
        {
            "abstract", "any", "as", "boolean", "break", "case", "catch", "class", "const", "constructor",
            "continue", "debugger", "default", "delete", "do", "else", "enum", "export", "extends",
            "false", "finally", "for", "from", "function", "get", "if", "implements", "import",
            "in", "instanceof", "interface", "let", "module", "namespace", "new", "null", "number",
            "object", "package", "private", "protected", "public", "readonly", "return", "set",
            "static", "string", "super", "switch", "this", "throw", "true", "try", "type", "typeof",
            "undefined", "var", "void", "while", "with", "yield"
        };
    }

    private List<TokenPattern> GetPythonPatterns()
    {
        return new List<TokenPattern>
        {
            new() { Regex = new Regex(@"#.*$", RegexOptions.Multiline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex("\"\"\".*?\"\"\"", RegexOptions.Singleline), TokenType = TokenType.String, CssClass = "docstring", Priority = 10 },
            new() { Regex = new Regex(@"""(?:[^""\\]|\\.)*"""), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"'(?:[^'\\]|\\.)*'"), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"\b\d+\.?\d*\b"), TokenType = TokenType.Number, CssClass = "number", Priority = 8 },
        };
    }

    private List<string> GetPythonKeywords()
    {
        return new List<string>
        {
            "and", "as", "assert", "break", "class", "continue", "def", "del", "elif", "else",
            "except", "False", "finally", "for", "from", "global", "if", "import", "in", "is",
            "lambda", "None", "nonlocal", "not", "or", "pass", "raise", "return", "True", "try",
            "while", "with", "yield"
        };
    }

    private List<TokenPattern> GetJavaScriptPatterns()
    {
        return GetTypeScriptPatterns(); // Similar patterns
    }

    private List<string> GetJavaScriptKeywords()
    {
        return new List<string>
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "export", "extends", "false", "finally", "for", "function", "if", "import",
            "in", "instanceof", "let", "new", "null", "return", "super", "switch", "this", "throw",
            "true", "try", "typeof", "undefined", "var", "void", "while", "with", "yield"
        };
    }

    private List<TokenPattern> GetGenericPatterns()
    {
        return new List<TokenPattern>
        {
            new() { Regex = new Regex(@"//.*$", RegexOptions.Multiline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline), TokenType = TokenType.Comment, CssClass = "comment", Priority = 10 },
            new() { Regex = new Regex(@"""(?:[^""\\]|\\.)*"""), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"'(?:[^'\\]|\\.)*'"), TokenType = TokenType.String, CssClass = "string", Priority = 9 },
            new() { Regex = new Regex(@"\b\d+\.?\d*\b"), TokenType = TokenType.Number, CssClass = "number", Priority = 8 },
        };
    }

    private List<string> GetGenericKeywords()
    {
        return new List<string>
        {
            "if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue",
            "return", "function", "class", "struct", "enum", "interface", "public", "private",
            "protected", "static", "const", "var", "let", "new", "delete", "this", "super"
        };
    }

    #endregion
}

#region Data Models

/// <summary>
/// Grammar highlighting rules for a specific language
/// </summary>
public class GrammarHighlightingRules
{
    public string GrammarName { get; set; } = "";
    public List<TokenPattern> TokenPatterns { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<CompletionItem> CompletionSuggestions { get; set; } = new();
    public List<ValidationRule> ValidationRules { get; set; } = new();
}

/// <summary>
/// Pattern for matching tokens
/// </summary>
public class TokenPattern
{
    public required Regex Regex { get; set; }
    public TokenType TokenType { get; set; }
    public string CssClass { get; set; } = "";
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Syntax token with position and type information
/// </summary>
public class SyntaxToken
{
    public TokenType Type { get; set; }
    public string Value { get; set; } = "";
    public int Start { get; set; }
    public int End { get; set; }
    public string CssClass { get; set; } = "";
}

/// <summary>
/// Code completion item
/// </summary>
public class CompletionItem
{
    public string Label { get; set; } = "";
    public string InsertText { get; set; } = "";
    public CompletionItemKind Kind { get; set; }
    public string Detail { get; set; } = "";
    public string Documentation { get; set; } = "";
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Context information for code completion
/// </summary>
public class SyntaxCompletionContext
{
    public int Position { get; set; }
    public string CurrentLine { get; set; } = "";
    public int CurrentLinePosition { get; set; }
    public string CurrentWord { get; set; } = "";
    public int WordStart { get; set; }
    public string PrecedingText { get; set; } = "";
    public string FollowingText { get; set; } = "";
}

/// <summary>
/// Syntax error information
/// </summary>
public class SyntaxError
{
    public string Message { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
}

/// <summary>
/// Validation rule for syntax checking
/// </summary>
public class ValidationRule
{
    public required Regex Pattern { get; set; }
    public string ErrorMessage { get; set; } = "";
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
}

/// <summary>
/// Token types for syntax highlighting
/// </summary>
public enum TokenType
{
    Unknown,
    Text,
    Keyword,
    String,
    Number,
    Comment,
    Type,
    Identifier,
    Operator,
    Punctuation
}

/// <summary>
/// Completion item kinds
/// </summary>
public enum CompletionItemKind
{
    Text,
    Method,
    Function,
    Constructor,
    Field,
    Variable,
    Class,
    Interface,
    Module,
    Property,
    Keyword,
    Reference,
    File,
    Folder
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    Information,
    Warning,
    Error
}

#endregion