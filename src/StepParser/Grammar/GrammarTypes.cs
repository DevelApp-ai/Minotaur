using StepParser.Core;
using StepParser.Lexer;

namespace StepParser.Grammar;

/// <summary>
/// Enum representing the grammar format type.
/// </summary>
public enum GrammarFormatType
{
    CEBNF,
    ANTLR4,
    Bison,
    Flex,
    Yacc,
    Lex,
    Minotaur
}

/// <summary>
/// Enum representing associativity types.
/// </summary>
public enum AssociativityType
{
    Left,
    Right,
    None
}

/// <summary>
/// Represents a precedence rule for operators.
/// </summary>
public class PrecedenceRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrecedenceRule"/> class.
    /// </summary>
    /// <param name="level">The precedence level.</param>
    /// <param name="operators">The operators.</param>
    /// <param name="associativity">The associativity type.</param>
    /// <param name="description">Optional description.</param>
    public PrecedenceRule(int level, IEnumerable<string> operators, AssociativityType associativity, string description = "")
    {
        Level = level;
        Operators = operators.ToList();
        Associativity = associativity;
        Description = description;
    }

    /// <summary>
    /// Gets the precedence level.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the operators.
    /// </summary>
    public IReadOnlyList<string> Operators { get; }

    /// <summary>
    /// Gets the associativity type.
    /// </summary>
    public AssociativityType Associativity { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Checks if this rule contains the specified operator.
    /// </summary>
    /// <param name="operatorName">The operator to check.</param>
    /// <returns>True if the operator is in this rule; otherwise, false.</returns>
    public bool HasOperator(string operatorName)
    {
        return Operators.Contains(operatorName);
    }
}

/// <summary>
/// Represents an error recovery strategy.
/// </summary>
public class ErrorRecoveryStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorRecoveryStrategy"/> class.
    /// </summary>
    public ErrorRecoveryStrategy()
    {
        Strategy = "automatic";
        SyncTokens = new List<string>();
        RecoveryActions = new Dictionary<string, string>();
        ReportingLevel = "basic";
    }

    /// <summary>
    /// Gets or sets the recovery strategy.
    /// </summary>
    public string Strategy { get; set; }

    /// <summary>
    /// Gets the synchronization tokens.
    /// </summary>
    public List<string> SyncTokens { get; }

    /// <summary>
    /// Gets the recovery actions.
    /// </summary>
    public Dictionary<string, string> RecoveryActions { get; }

    /// <summary>
    /// Gets or sets the reporting level.
    /// </summary>
    public string ReportingLevel { get; set; }

    /// <summary>
    /// Adds a synchronization token.
    /// </summary>
    /// <param name="token">The token to add.</param>
    public void AddSyncToken(string token)
    {
        if (!SyncTokens.Contains(token))
        {
            SyncTokens.Add(token);
        }
    }

    /// <summary>
    /// Sets a recovery action for an error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <param name="action">The recovery action.</param>
    public void SetRecoveryAction(string errorType, string action)
    {
        RecoveryActions[errorType] = action;
    }

    /// <summary>
    /// Gets a recovery action for an error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The recovery action, or null if not found.</returns>
    public string? GetRecoveryAction(string errorType)
    {
        return RecoveryActions.TryGetValue(errorType, out var action) ? action : null;
    }
}

/// <summary>
/// Represents a semantic action template.
/// </summary>
public class SemanticActionTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticActionTemplate"/> class.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="template">The template string.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="returnType">The return type.</param>
    /// <param name="description">Optional description.</param>
    public SemanticActionTemplate(string name, string template, IEnumerable<string>? parameters = null, string returnType = "void", string description = "")
    {
        Name = name;
        Template = template;
        Parameters = parameters?.ToList() ?? new List<string>();
        ReturnType = returnType;
        Description = description;
    }

    /// <summary>
    /// Gets the template name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the template string.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    public IReadOnlyList<string> Parameters { get; }

    /// <summary>
    /// Gets the return type.
    /// </summary>
    public string ReturnType { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Instantiates the template with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The instantiated template.</returns>
    public string Instantiate(IReadOnlyList<string> args)
    {
        var result = Template;
        for (int i = 0; i < args.Count && i < Parameters.Count; i++)
        {
            var paramPattern = $"${{{Parameters[i]}}}";
            result = result.Replace(paramPattern, args[i]);
        }
        return result;
    }
}