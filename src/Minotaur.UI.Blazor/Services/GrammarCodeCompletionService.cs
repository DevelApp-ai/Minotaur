/*
 * Grammar-based Code Completion Service for Minotaur
 * Provides intelligent code completion based on grammar rules and context analysis
 */

using System.Text.Json;
using System.Text.RegularExpressions;
using Minotaur.Projects.Grammar;
using Minotaur.UI.Blazor.Services;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for providing intelligent code completion based on grammar analysis
/// </summary>
public class GrammarCodeCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly GrammarSyntaxHighlightingService _syntaxService;
    private readonly ILogger<GrammarCodeCompletionService> _logger;

    // Cache for grammar rules and completion data
    private readonly Dictionary<string, GrammarCompletionRules> _completionCache = new();
    private readonly Dictionary<string, List<Symbol>> _symbolCache = new();

    public GrammarCodeCompletionService(
        HttpClient httpClient,
        GrammarSyntaxHighlightingService syntaxService,
        ILogger<GrammarCodeCompletionService> logger)
    {
        _httpClient = httpClient;
        _syntaxService = syntaxService;
        _logger = logger;
    }

    /// <summary>
    /// Get intelligent code completion suggestions
    /// </summary>
    public async Task<CompletionResult> GetCompletionsAsync(
        string content,
        int position,
        string grammarName,
        GrammarVersion? version = null,
        CompletionOptions? options = null)
    {
        try
        {
            options ??= new CompletionOptions();

            // Analyze the context around the cursor
            var context = await AnalyzeCompletionContextAsync(content, position, grammarName, version);

            // Get grammar-specific completion rules
            var rules = await GetCompletionRulesAsync(grammarName, version);

            // Generate completions based on context and rules
            var completions = await GenerateContextualCompletionsAsync(context, rules, options);

            return new CompletionResult
            {
                IsSuccess = true,
                Items = completions,
                Context = context
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completions for {GrammarName} at position {Position}",
                grammarName, position);

            return new CompletionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Items = new List<CompletionItem>()
            };
        }
    }

    /// <summary>
    /// Get signature help for function calls
    /// </summary>
    public async Task<SignatureHelp?> GetSignatureHelpAsync(
        string content,
        int position,
        string grammarName,
        GrammarVersion? version = null)
    {
        try
        {
            var context = await AnalyzeCompletionContextAsync(content, position, grammarName, version);
            var rules = await GetCompletionRulesAsync(grammarName, version);

            return await GenerateSignatureHelpAsync(context, rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signature help for {GrammarName}", grammarName);
            return null;
        }
    }

    /// <summary>
    /// Get hover information for symbols
    /// </summary>
    public async Task<HoverInfo?> GetHoverInfoAsync(
        string content,
        int position,
        string grammarName,
        GrammarVersion? version = null)
    {
        try
        {
            var context = await AnalyzeCompletionContextAsync(content, position, grammarName, version);
            var rules = await GetCompletionRulesAsync(grammarName, version);

            return await GenerateHoverInfoAsync(context, rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hover info for {GrammarName}", grammarName);
            return null;
        }
    }

    /// <summary>
    /// Analyze completion context with deep grammar understanding
    /// </summary>
    private async Task<CompletionContext> AnalyzeCompletionContextAsync(
        string content,
        int position,
        string grammarName,
        GrammarVersion? version)
    {
        var context = new CompletionContext { Position = position };

        // Basic context analysis
        AnalyzeBasicContext(content, position, context);

        // Tokenize content for advanced analysis
        var tokens = await _syntaxService.TokenizeContentAsync(content, grammarName, version);
        context.Tokens = tokens;

        // Find current token
        context.CurrentToken = FindTokenAtPosition(tokens, position);

        // Analyze surrounding structure
        AnalyzeSyntacticStructure(content, position, context);

        // Detect completion trigger
        DetectCompletionTrigger(content, position, context);

        // Build symbol table from current content
        context.AvailableSymbols = await BuildSymbolTableAsync(content, grammarName, version);

        return context;
    }

    /// <summary>
    /// Analyze basic context information
    /// </summary>
    private void AnalyzeBasicContext(string content, int position, CompletionContext context)
    {
        // Current line analysis
        var lineStart = content.LastIndexOf('\n', position - 1) + 1;
        var lineEnd = content.IndexOf('\n', position);
        if (lineEnd == -1) lineEnd = content.Length;

        context.CurrentLine = content[lineStart..lineEnd];
        context.CurrentLinePosition = position - lineStart;
        context.LineNumber = content.Take(position).Count(c => c == '\n') + 1;

        // Current word analysis
        var wordStart = position;
        while (wordStart > 0 && IsWordCharacter(content[wordStart - 1]))
            wordStart--;

        var wordEnd = position;
        while (wordEnd < content.Length && IsWordCharacter(content[wordEnd]))
            wordEnd++;

        context.CurrentWord = content[wordStart..wordEnd];
        context.WordStart = wordStart;
        context.WordEnd = wordEnd;

        // Surrounding context
        context.PrecedingText = content[Math.Max(0, position - 200)..position];
        context.FollowingText = content[position..Math.Min(content.Length, position + 200)];
    }

    /// <summary>
    /// Analyze syntactic structure around position
    /// </summary>
    private void AnalyzeSyntacticStructure(string content, int position, CompletionContext context)
    {
        // Find containing blocks/scopes
        context.ContainingScopes = FindContainingScopes(content, position);

        // Detect if we're inside a string/comment
        context.IsInString = IsInsideString(content, position);
        context.IsInComment = IsInsideComment(content, position);

        // Detect statement context
        context.StatementContext = DetectStatementContext(content, position);

        // Find indentation level
        var lineStart = content.LastIndexOf('\n', position - 1) + 1;
        var indentation = 0;
        while (lineStart + indentation < position &&
               char.IsWhiteSpace(content[lineStart + indentation]))
        {
            indentation++;
        }
        context.IndentationLevel = indentation;
    }

    /// <summary>
    /// Detect what triggered the completion request
    /// </summary>
    private void DetectCompletionTrigger(string content, int position, CompletionContext context)
    {
        if (position > 0)
        {
            var previousChar = content[position - 1];
            context.TriggerCharacter = previousChar;

            context.TriggerKind = previousChar switch
            {
                '.' => CompletionTriggerKind.MemberAccess,
                ':' when position > 1 && content[position - 2] == ':' => CompletionTriggerKind.ScopeResolution,
                '(' => CompletionTriggerKind.SignatureHelp,
                '<' => CompletionTriggerKind.GenericParameters,
                ' ' => CompletionTriggerKind.Invoked,
                _ => char.IsLetter(previousChar) ? CompletionTriggerKind.Typing : CompletionTriggerKind.Invoked
            };
        }
        else
        {
            context.TriggerKind = CompletionTriggerKind.Invoked;
        }
    }

    /// <summary>
    /// Build symbol table from content analysis
    /// </summary>
    private Task<List<Symbol>> BuildSymbolTableAsync(string content, string grammarName, GrammarVersion? version)
    {
        var symbols = new List<Symbol>();

        // Use cached symbols if available
        var cacheKey = $"{grammarName}:{version}:{content.GetHashCode()}";
        if (_symbolCache.TryGetValue(cacheKey, out var cachedSymbols))
        {
            return Task.FromResult(cachedSymbols);
        }

        // Extract symbols based on grammar rules
        symbols.AddRange(ExtractClassDeclarations(content));
        symbols.AddRange(ExtractMethodDeclarations(content));
        symbols.AddRange(ExtractVariableDeclarations(content));
        symbols.AddRange(ExtractPropertyDeclarations(content));

        // Cache results
        if (_symbolCache.Count > 100) _symbolCache.Clear();
        _symbolCache[cacheKey] = symbols;

        return Task.FromResult(symbols);
    }

    /// <summary>
    /// Generate contextual completions based on analysis
    /// </summary>
    private Task<List<CompletionItem>> GenerateContextualCompletionsAsync(
        CompletionContext context,
        GrammarCompletionRules rules,
        CompletionOptions options)
    {
        var completions = new List<CompletionItem>();

        // Don't provide completions inside strings/comments (unless specifically requested)
        if ((context.IsInString || context.IsInComment) && !options.IncludeInStrings)
        {
            return Task.FromResult(completions);
        }

        // Generate completions based on trigger kind
        switch (context.TriggerKind)
        {
            case CompletionTriggerKind.MemberAccess:
                completions.AddRange(GetMemberAccessCompletions(context, rules));
                break;

            case CompletionTriggerKind.ScopeResolution:
                completions.AddRange(GetScopeResolutionCompletions(context, rules));
                break;

            case CompletionTriggerKind.Typing:
            case CompletionTriggerKind.Invoked:
                completions.AddRange(GetGeneralCompletions(context, rules));
                break;
        }

        // Filter by current prefix
        if (!string.IsNullOrEmpty(context.CurrentWord))
        {
            completions = completions
                .Where(c => c.Label.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Sort by relevance
        return Task.FromResult(completions
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.Label)
            .Take(options.MaxResults)
            .ToList());
    }

    /// <summary>
    /// Get completions for member access (after '.')
    /// </summary>
    private List<CompletionItem> GetMemberAccessCompletions(CompletionContext context, GrammarCompletionRules rules)
    {
        var completions = new List<CompletionItem>();

        // Find the expression before the dot
        var beforeDot = GetExpressionBeforeDot(context);
        if (string.IsNullOrEmpty(beforeDot))
            return completions;

        // Look up type/symbol for the expression
        var symbol = context.AvailableSymbols.FirstOrDefault(s => s.Name == beforeDot);
        if (symbol != null)
        {
            // Add members of the found symbol
            completions.AddRange(symbol.Members.Select(member => new CompletionItem
            {
                Label = member.Name,
                InsertText = member.Name,
                Kind = member.Kind switch
                {
                    SymbolKind.Method => CompletionItemKind.Method,
                    SymbolKind.Property => CompletionItemKind.Property,
                    SymbolKind.Field => CompletionItemKind.Field,
                    _ => CompletionItemKind.Text
                },
                Detail = member.Type,
                Documentation = member.Documentation,
                Priority = 10
            }));
        }

        return completions;
    }

    /// <summary>
    /// Get completions for scope resolution (after '::')
    /// </summary>
    private List<CompletionItem> GetScopeResolutionCompletions(CompletionContext context, GrammarCompletionRules rules)
    {
        var completions = new List<CompletionItem>();

        // Add static members and nested types
        completions.AddRange(rules.StaticMembers.Select(member => new CompletionItem
        {
            Label = member.Name,
            InsertText = member.Name,
            Kind = CompletionItemKind.Method,
            Detail = member.Signature,
            Documentation = member.Documentation,
            Priority = 8
        }));

        return completions;
    }

    /// <summary>
    /// Get general completions (keywords, symbols, etc.)
    /// </summary>
    private List<CompletionItem> GetGeneralCompletions(CompletionContext context, GrammarCompletionRules rules)
    {
        var completions = new List<CompletionItem>();

        // Add keywords
        completions.AddRange(rules.Keywords.Select(keyword => new CompletionItem
        {
            Label = keyword,
            InsertText = keyword,
            Kind = CompletionItemKind.Keyword,
            Detail = "Keyword",
            Priority = 5
        }));

        // Add available symbols
        completions.AddRange(context.AvailableSymbols.Select(symbol => new CompletionItem
        {
            Label = symbol.Name,
            InsertText = symbol.Name,
            Kind = symbol.Kind switch
            {
                SymbolKind.Class => CompletionItemKind.Class,
                SymbolKind.Method => CompletionItemKind.Method,
                SymbolKind.Variable => CompletionItemKind.Variable,
                SymbolKind.Property => CompletionItemKind.Property,
                _ => CompletionItemKind.Text
            },
            Detail = symbol.Type,
            Documentation = symbol.Documentation,
            Priority = 7
        }));

        // Add code snippets for current context
        completions.AddRange(GetContextualSnippets(context, rules));

        return completions;
    }

    /// <summary>
    /// Get contextual code snippets
    /// </summary>
    private List<CompletionItem> GetContextualSnippets(CompletionContext context, GrammarCompletionRules rules)
    {
        var snippets = new List<CompletionItem>();

        foreach (var snippet in rules.CodeSnippets)
        {
            if (snippet.IsApplicableInContext(context))
            {
                snippets.Add(new CompletionItem
                {
                    Label = snippet.Prefix,
                    InsertText = snippet.Body,
                    Kind = CompletionItemKind.Reference,
                    Detail = snippet.Description,
                    Documentation = snippet.Documentation,
                    Priority = 6
                });
            }
        }

        return snippets;
    }

    #region Grammar Rules Management

    /// <summary>
    /// Get completion rules for a grammar
    /// </summary>
    private async Task<GrammarCompletionRules> GetCompletionRulesAsync(string grammarName, GrammarVersion? version)
    {
        var cacheKey = $"{grammarName}:{version?.ToString() ?? "latest"}";

        if (_completionCache.TryGetValue(cacheKey, out var cachedRules))
        {
            return cachedRules;
        }

        var rules = await FetchCompletionRulesFromApiAsync(grammarName, version)
                    ?? GenerateDefaultCompletionRules(grammarName);

        _completionCache[cacheKey] = rules;
        return rules;
    }

    /// <summary>
    /// Fetch completion rules from API
    /// </summary>
    private async Task<GrammarCompletionRules?> FetchCompletionRulesFromApiAsync(string grammarName, GrammarVersion? version)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/grammar/{grammarName}/completion-rules?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GrammarCompletionRules>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch completion rules for {GrammarName}", grammarName);
        }

        return null;
    }

    /// <summary>
    /// Generate default completion rules for a grammar
    /// </summary>
    private GrammarCompletionRules GenerateDefaultCompletionRules(string grammarName)
    {
        var rules = new GrammarCompletionRules { GrammarName = grammarName };

        if (grammarName.Contains("CSharp", StringComparison.OrdinalIgnoreCase))
        {
            rules.Keywords.AddRange(GetCSharpKeywords());
            rules.CodeSnippets.AddRange(GetCSharpSnippets());
        }
        else if (grammarName.Contains("TypeScript", StringComparison.OrdinalIgnoreCase))
        {
            rules.Keywords.AddRange(GetTypeScriptKeywords());
            rules.CodeSnippets.AddRange(GetTypeScriptSnippets());
        }
        // Add more languages as needed

        return rules;
    }

    #endregion

    #region Symbol Extraction

    private List<Symbol> ExtractClassDeclarations(string content)
    {
        var symbols = new List<Symbol>();
        var classPattern = new Regex(@"(?:public\s+|private\s+|protected\s+)?class\s+(\w+)", RegexOptions.Multiline);

        foreach (Match match in classPattern.Matches(content))
        {
            symbols.Add(new Symbol
            {
                Name = match.Groups[1].Value,
                Kind = SymbolKind.Class,
                Type = "class",
                Position = match.Index
            });
        }

        return symbols;
    }

    private List<Symbol> ExtractMethodDeclarations(string content)
    {
        var symbols = new List<Symbol>();
        var methodPattern = new Regex(@"(?:public\s+|private\s+|protected\s+)?(?:static\s+)?(\w+)\s+(\w+)\s*\([^)]*\)", RegexOptions.Multiline);

        foreach (Match match in methodPattern.Matches(content))
        {
            symbols.Add(new Symbol
            {
                Name = match.Groups[2].Value,
                Kind = SymbolKind.Method,
                Type = match.Groups[1].Value,
                Position = match.Index
            });
        }

        return symbols;
    }

    private List<Symbol> ExtractVariableDeclarations(string content)
    {
        var symbols = new List<Symbol>();
        var varPattern = new Regex(@"(?:var|let|const|int|string|bool|double|float)\s+(\w+)", RegexOptions.Multiline);

        foreach (Match match in varPattern.Matches(content))
        {
            symbols.Add(new Symbol
            {
                Name = match.Groups[1].Value,
                Kind = SymbolKind.Variable,
                Type = "variable",
                Position = match.Index
            });
        }

        return symbols;
    }

    private List<Symbol> ExtractPropertyDeclarations(string content)
    {
        var symbols = new List<Symbol>();
        var propPattern = new Regex(@"(\w+)\s+(\w+)\s*{\s*get", RegexOptions.Multiline);

        foreach (Match match in propPattern.Matches(content))
        {
            symbols.Add(new Symbol
            {
                Name = match.Groups[2].Value,
                Kind = SymbolKind.Property,
                Type = match.Groups[1].Value,
                Position = match.Index
            });
        }

        return symbols;
    }

    #endregion

    #region Utility Methods

    private bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    private bool IsInsideString(string content, int position)
    {
        var quoteCount = content.Take(position).Count(c => c == '"');
        return quoteCount % 2 == 1;
    }

    private bool IsInsideComment(string content, int position)
    {
        var lineStart = content.LastIndexOf('\n', position - 1) + 1;
        var beforePosition = content[lineStart..position];
        return beforePosition.Contains("//");
    }

    private StatementContext DetectStatementContext(string content, int position)
    {
        // Simplified context detection
        var beforePosition = content[Math.Max(0, position - 50)..position];

        if (beforePosition.Contains("if"))
            return StatementContext.IfStatement;
        if (beforePosition.Contains("for"))
            return StatementContext.ForLoop;
        if (beforePosition.Contains("while"))
            return StatementContext.WhileLoop;

        return StatementContext.General;
    }

    private List<string> FindContainingScopes(string content, int position)
    {
        // Simplified scope detection - count braces
        var scopes = new List<string>();
        var braceCount = 0;

        for (int i = 0; i < position; i++)
        {
            if (content[i] == '{')
                braceCount++;
            else if (content[i] == '}')
                braceCount--;
        }

        for (int i = 0; i < braceCount; i++)
        {
            scopes.Add($"scope_{i}");
        }

        return scopes;
    }

    private SyntaxToken? FindTokenAtPosition(List<SyntaxToken> tokens, int position)
    {
        return tokens.FirstOrDefault(t => position >= t.Start && position <= t.End);
    }

    private string GetExpressionBeforeDot(CompletionContext context)
    {
        // Find the identifier before the dot
        var dotPosition = context.Position - 1;
        var start = dotPosition - 1;

        while (start >= 0 && IsWordCharacter(context.PrecedingText[start]))
            start--;

        return start < dotPosition - 1
            ? context.PrecedingText[(start + 1)..(dotPosition)]
            : "";
    }

    #endregion

    #region Language-Specific Data

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

    private List<CodeSnippet> GetCSharpSnippets()
    {
        return new List<CodeSnippet>
        {
            new()
            {
                Prefix = "class",
                Body = "public class ${1:ClassName}\n{\n    ${2:// TODO: Implement}\n}",
                Description = "Class declaration",
                ApplicableContexts = { StatementContext.General }
            },
            new()
            {
                Prefix = "method",
                Body = "public ${1:void} ${2:MethodName}(${3:})\n{\n    ${4:// TODO: Implement}\n}",
                Description = "Method declaration",
                ApplicableContexts = { StatementContext.General }
            },
            new()
            {
                Prefix = "if",
                Body = "if (${1:condition})\n{\n    ${2:// TODO: Implement}\n}",
                Description = "If statement",
                ApplicableContexts = { StatementContext.General }
            }
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

    private List<CodeSnippet> GetTypeScriptSnippets()
    {
        return new List<CodeSnippet>
        {
            new()
            {
                Prefix = "interface",
                Body = "interface ${1:InterfaceName} {\n    ${2:// TODO: Define properties}\n}",
                Description = "Interface declaration",
                ApplicableContexts = { StatementContext.General }
            },
            new()
            {
                Prefix = "function",
                Body = "function ${1:functionName}(${2:}): ${3:void} {\n    ${4:// TODO: Implement}\n}",
                Description = "Function declaration",
                ApplicableContexts = { StatementContext.General }
            }
        };
    }

    private async Task<SignatureHelp?> GenerateSignatureHelpAsync(CompletionContext context, GrammarCompletionRules rules)
    {
        // Simplified signature help implementation
        return await Task.FromResult<SignatureHelp?>(null);
    }

    private async Task<HoverInfo?> GenerateHoverInfoAsync(CompletionContext context, GrammarCompletionRules rules)
    {
        // Simplified hover info implementation
        return await Task.FromResult<HoverInfo?>(null);
    }

    #endregion
}

#region Additional Data Models

public class CompletionResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = "";
    public List<CompletionItem> Items { get; set; } = new();
    public CompletionContext? Context { get; set; }
}

public class CompletionOptions
{
    public int MaxResults { get; set; } = 50;
    public bool IncludeInStrings { get; set; } = false;
    public bool IncludeSnippets { get; set; } = true;
    public bool IncludeKeywords { get; set; } = true;
    public bool IncludeSymbols { get; set; } = true;
}

public class CompletionContext
{
    public int Position { get; set; }
    public string CurrentLine { get; set; } = "";
    public int CurrentLinePosition { get; set; }
    public int LineNumber { get; set; }
    public string CurrentWord { get; set; } = "";
    public int WordStart { get; set; }
    public int WordEnd { get; set; }
    public string PrecedingText { get; set; } = "";
    public string FollowingText { get; set; } = "";
    public List<SyntaxToken> Tokens { get; set; } = new();
    public SyntaxToken? CurrentToken { get; set; }
    public List<string> ContainingScopes { get; set; } = new();
    public bool IsInString { get; set; }
    public bool IsInComment { get; set; }
    public StatementContext StatementContext { get; set; }
    public int IndentationLevel { get; set; }
    public CompletionTriggerKind TriggerKind { get; set; }
    public char? TriggerCharacter { get; set; }
    public List<Symbol> AvailableSymbols { get; set; } = new();
}

public class GrammarCompletionRules
{
    public string GrammarName { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public List<CodeSnippet> CodeSnippets { get; set; } = new();
    public List<StaticMember> StaticMembers { get; set; } = new();
    public List<TypeDefinition> Types { get; set; } = new();
}

public class Symbol
{
    public string Name { get; set; } = "";
    public SymbolKind Kind { get; set; }
    public string Type { get; set; } = "";
    public string Documentation { get; set; } = "";
    public int Position { get; set; }
    public List<Symbol> Members { get; set; } = new();
}

public class CodeSnippet
{
    public string Prefix { get; set; } = "";
    public string Body { get; set; } = "";
    public string Description { get; set; } = "";
    public string Documentation { get; set; } = "";
    public List<StatementContext> ApplicableContexts { get; set; } = new();

    public bool IsApplicableInContext(CompletionContext context)
    {
        return ApplicableContexts.Count == 0 || ApplicableContexts.Contains(context.StatementContext);
    }
}

public class StaticMember
{
    public string Name { get; set; } = "";
    public string Signature { get; set; } = "";
    public string Documentation { get; set; } = "";
}

public class TypeDefinition
{
    public string Name { get; set; } = "";
    public List<Symbol> Members { get; set; } = new();
}

public class SignatureHelp
{
    public List<SignatureInformation> Signatures { get; set; } = new();
    public int ActiveSignature { get; set; }
    public int ActiveParameter { get; set; }
}

public class SignatureInformation
{
    public string Label { get; set; } = "";
    public string Documentation { get; set; } = "";
    public List<ParameterInformation> Parameters { get; set; } = new();
}

public class ParameterInformation
{
    public string Label { get; set; } = "";
    public string Documentation { get; set; } = "";
}

public class HoverInfo
{
    public string Contents { get; set; } = "";
    public int Start { get; set; }
    public int End { get; set; }
}

public enum CompletionTriggerKind
{
    Invoked,
    TriggerCharacter,
    MemberAccess,
    ScopeResolution,
    SignatureHelp,
    GenericParameters,
    Typing
}

public enum StatementContext
{
    General,
    IfStatement,
    ForLoop,
    WhileLoop,
    MethodBody,
    ClassBody,
    InterfaceBody
}

public enum SymbolKind
{
    Class,
    Interface,
    Method,
    Property,
    Field,
    Variable,
    Namespace,
    Enum
}

#endregion