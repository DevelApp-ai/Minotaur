using StepParser.Core;
using StepParser.Lexer;

namespace StepParser.Grammar;

/// <summary>
/// Represents a grammar definition with productions and terminals.
/// </summary>
public class Grammar
{
    private readonly string _name;
    private readonly List<Production> _productions;
    private readonly List<Terminal> _validStartTerminals;
    private readonly List<Production> _startProductions;
    private readonly List<PrecedenceRule> _precedenceRules;
    private readonly Dictionary<string, SemanticActionTemplate> _semanticActionTemplates;
    private readonly List<string> _baseGrammars;
    private readonly List<string> _inheritedRules;
    private readonly List<string> _overriddenRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="Grammar"/> class.
    /// </summary>
    /// <param name="name">The name of the grammar.</param>
    public Grammar(string name)
    {
        _name = name;
        _productions = new List<Production>();
        _validStartTerminals = new List<Terminal>();
        _startProductions = new List<Production>();
        _precedenceRules = new List<PrecedenceRule>();
        _semanticActionTemplates = new Dictionary<string, SemanticActionTemplate>();
        _baseGrammars = new List<string>();
        _inheritedRules = new List<string>();
        _overriddenRules = new List<string>();

        // Initialize properties
        TokenSplitterType = TokenSplitterType.None;
        RegexTokenSplitter = string.Empty;
        IsInheritable = false;
        FormatType = GrammarFormatType.CEBNF;
        ImportSemantics = false;
        CoordinateTokens = false;
        ErrorRecoveryStrategy = new ErrorRecoveryStrategy();
        Content = string.Empty;
    }

    /// <summary>
    /// Gets the name of the grammar.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the productions in the grammar.
    /// </summary>
    public IReadOnlyList<Production> Productions => _productions;

    /// <summary>
    /// Gets the valid start terminals.
    /// </summary>
    public IReadOnlyList<Terminal> ValidStartTerminals => _validStartTerminals;

    /// <summary>
    /// Gets the start productions.
    /// </summary>
    public IReadOnlyList<Production> StartProductions => _startProductions;

    /// <summary>
    /// Gets or sets the token splitter type.
    /// </summary>
    public TokenSplitterType TokenSplitterType { get; set; }

    /// <summary>
    /// Gets or sets the regex token splitter.
    /// </summary>
    public string RegexTokenSplitter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this grammar is inheritable.
    /// </summary>
    public bool IsInheritable { get; set; }

    /// <summary>
    /// Gets or sets the format type of this grammar.
    /// </summary>
    public GrammarFormatType FormatType { get; set; }

    /// <summary>
    /// Gets the base grammars this grammar inherits from.
    /// </summary>
    public IReadOnlyList<string> BaseGrammars => _baseGrammars;

    /// <summary>
    /// Gets or sets a value indicating whether semantic actions should be imported from base grammars.
    /// </summary>
    public bool ImportSemantics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tokens should be coordinated between lexer and parser.
    /// </summary>
    public bool CoordinateTokens { get; set; }

    /// <summary>
    /// Gets the precedence rules for this grammar.
    /// </summary>
    public IReadOnlyList<PrecedenceRule> PrecedenceRules => _precedenceRules;

    /// <summary>
    /// Gets or sets the error recovery strategy.
    /// </summary>
    public ErrorRecoveryStrategy ErrorRecoveryStrategy { get; set; }

    /// <summary>
    /// Gets all semantic action templates.
    /// </summary>
    public IReadOnlyDictionary<string, SemanticActionTemplate> SemanticActionTemplates => _semanticActionTemplates;

    /// <summary>
    /// Gets or sets the grammar content.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets the inherited rules.
    /// </summary>
    public IReadOnlyList<string> InheritedRules => _inheritedRules;

    /// <summary>
    /// Gets the overridden rules.
    /// </summary>
    public IReadOnlyList<string> OverriddenRules => _overriddenRules;

    // Production management

    /// <summary>
    /// Adds a production to the grammar.
    /// </summary>
    /// <param name="production">The production to add.</param>
    public void AddProduction(Production production)
    {
        _productions.Add(production);
    }

    /// <summary>
    /// Gets a production by name.
    /// </summary>
    /// <param name="name">The name of the production.</param>
    /// <returns>The production or null if not found.</returns>
    public Production? GetProductionByName(string name)
    {
        return _productions.FirstOrDefault(p => p.Name == name);
    }

    // Terminal management

    /// <summary>
    /// Adds a valid start terminal.
    /// </summary>
    /// <param name="terminal">The terminal to add.</param>
    public void AddValidStartTerminal(Terminal terminal)
    {
        _validStartTerminals.Add(terminal);
    }

    // Start production management

    /// <summary>
    /// Adds a start production.
    /// </summary>
    /// <param name="production">The production to add.</param>
    public void AddStartProduction(Production production)
    {
        _startProductions.Add(production);
    }

    // Inheritance support

    /// <summary>
    /// Adds a base grammar to inherit from.
    /// </summary>
    /// <param name="baseGrammarName">The name of the base grammar.</param>
    public void AddBaseGrammar(string baseGrammarName)
    {
        if (!_baseGrammars.Contains(baseGrammarName))
        {
            _baseGrammars.Add(baseGrammarName);
        }
    }

    /// <summary>
    /// Sets the base grammars to inherit from.
    /// </summary>
    /// <param name="baseGrammars">Array of base grammar names.</param>
    public void SetBaseGrammars(IEnumerable<string> baseGrammars)
    {
        _baseGrammars.Clear();
        _baseGrammars.AddRange(baseGrammars);
    }

    /// <summary>
    /// Checks if this grammar inherits from another grammar.
    /// </summary>
    /// <param name="grammarName">The name of the grammar to check.</param>
    /// <returns>True if this grammar inherits from the specified grammar; otherwise, false.</returns>
    public bool InheritsFrom(string grammarName)
    {
        return _baseGrammars.Contains(grammarName);
    }

    /// <summary>
    /// Adds an inherited rule.
    /// </summary>
    /// <param name="ruleName">The name of the rule.</param>
    public void AddInheritedRule(string ruleName)
    {
        if (!_inheritedRules.Contains(ruleName))
        {
            _inheritedRules.Add(ruleName);
        }
    }

    /// <summary>
    /// Adds an overridden rule.
    /// </summary>
    /// <param name="ruleName">The name of the rule.</param>
    public void AddOverriddenRule(string ruleName)
    {
        if (!_overriddenRules.Contains(ruleName))
        {
            _overriddenRules.Add(ruleName);
        }
    }

    // Precedence management

    /// <summary>
    /// Adds a precedence rule.
    /// </summary>
    /// <param name="rule">The precedence rule to add.</param>
    public void AddPrecedenceRule(PrecedenceRule rule)
    {
        _precedenceRules.Add(rule);
    }

    /// <summary>
    /// Sets the precedence rules.
    /// </summary>
    /// <param name="rules">The precedence rules.</param>
    public void SetPrecedenceRules(IEnumerable<PrecedenceRule> rules)
    {
        _precedenceRules.Clear();
        _precedenceRules.AddRange(rules);
    }

    // Semantic action management

    /// <summary>
    /// Gets a semantic action template by name.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>The template or null if not found.</returns>
    public SemanticActionTemplate? GetSemanticActionTemplate(string name)
    {
        return _semanticActionTemplates.TryGetValue(name, out var template) ? template : null;
    }

    /// <summary>
    /// Adds a semantic action template.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="template">The template.</param>
    public void AddSemanticActionTemplate(string name, SemanticActionTemplate template)
    {
        _semanticActionTemplates[name] = template;
    }

    /// <summary>
    /// Creates a string representation of this grammar.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        var result = $"Grammar: {_name}\n";
        result += $"TokenSplitter: {TokenSplitterType}";

        if (TokenSplitterType == TokenSplitterType.Regex && !string.IsNullOrEmpty(RegexTokenSplitter))
        {
            result += $" \"{RegexTokenSplitter}\"";
        }

        if (IsInheritable)
        {
            result += "\nInheritable: true";
        }

        if (FormatType != GrammarFormatType.CEBNF)
        {
            result += $"\nFormatType: {FormatType}";
        }

        if (_baseGrammars.Count > 0)
        {
            result += $"\nInherits: {string.Join(", ", _baseGrammars)}";
        }

        if (ImportSemantics)
        {
            result += "\nImportSemantics: true";
        }

        if (CoordinateTokens)
        {
            result += "\nCoordinateTokens: true";
        }

        result += $"\n\n{_productions.Count} productions";

        return result;
    }

    // Static factory methods

    /// <summary>
    /// Loads a grammar from file content.
    /// </summary>
    /// <param name="content">The grammar content as string.</param>
    /// <param name="fileName">Optional filename for reference.</param>
    /// <returns>The loaded grammar.</returns>
    public static async Task<Grammar> LoadFromContentAsync(string content, string fileName = "grammar")
    {
        // For now, return a basic grammar with Python311 name as seen in TypeScript version
        // This would be enhanced with actual grammar parsing logic
        await Task.Delay(1); // Simulate async operation
        var grammar = new Grammar("Python311");
        grammar.Content = content;
        return grammar;
    }

    /// <summary>
    /// Loads a grammar from a file.
    /// </summary>
    /// <param name="filePath">Path to the grammar file.</param>
    /// <returns>The loaded grammar.</returns>
    public static async Task<Grammar> LoadFromFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            return await LoadFromContentAsync(content, fileName);
        }
        catch (Exception)
        {
            // If file loading fails, return a basic grammar
            var grammar = new Grammar("Python311");
            return grammar;
        }
    }
}