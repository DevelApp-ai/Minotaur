namespace Minotaur.Labyrinth;

/// <summary>
/// The kind of a node in a Labyrinth pattern mini-AST (issue #101).
/// </summary>
public enum LabyrinthPatternNodeKind
{
    /// <summary>A literal token of the target grammar (e.g. <c>IDENTIFIER</c>, <c>LPAREN</c>).</summary>
    Literal,

    /// <summary>A metavariable placeholder (<c>$NAME</c>) that binds during matching.</summary>
    Metavariable,

    /// <summary>The ellipsis operator (<c>...</c>) matching any sequence of tokens.</summary>
    Ellipsis
}

/// <summary>
/// One node of a Labyrinth pattern mini-AST: a target-grammar token
/// (<see cref="Literal"/>), a metavariable (<see cref="Metavariable"/>) or an
/// ellipsis (<see cref="Ellipsis"/>).
/// </summary>
public sealed class LabyrinthPatternNode
{
    /// <summary>The kind of the node.</summary>
    public LabyrinthPatternNodeKind Kind { get; }

    /// <summary>
    /// The target-grammar token type (e.g. <c>IDENTIFIER</c>). Only meaningful
    /// for <see cref="Literal"/> nodes; metavariable and ellipsis nodes carry
    /// the standard pattern token types.
    /// </summary>
    public string TokenType { get; }

    /// <summary>
    /// The matched source text. For metavariables this includes the leading
    /// dollar sign (e.g. <c>$DATA</c>); use <see cref="MetavariableName"/> for
    /// the bare name.
    /// </summary>
    public string Value { get; }

    /// <summary>The zero-based start position of the node in the pattern text.</summary>
    public int StartPosition { get; }

    /// <summary>The length of the node text in the pattern.</summary>
    public int Length { get; }

    /// <summary>
    /// The bare metavariable name (without the leading <c>$</c>), e.g. <c>DATA</c>.
    /// Empty for non-metavariable nodes.
    /// </summary>
    public string MetavariableName =>
        Kind == LabyrinthPatternNodeKind.Metavariable ? Value.TrimStart('$') : string.Empty;

    internal LabyrinthPatternNode(LabyrinthPatternNodeKind kind, string tokenType, string value, int startPosition, int length)
    {
        Kind = kind;
        TokenType = tokenType;
        Value = value;
        StartPosition = startPosition;
        Length = length;
    }
}

/// <summary>
/// The mini-AST of a single Labyrinth pattern, produced by parsing the pattern
/// snippet with the target grammar through the DevelApp.StepLexer/StepParser
/// pipeline with metavariable and ellipsis pattern tokens enabled.
/// </summary>
public sealed class LabyrinthPatternAst
{
    /// <summary>The original pattern text.</summary>
    public string Pattern { get; }

    /// <summary>The parsed nodes, in source order.</summary>
    public IReadOnlyList<LabyrinthPatternNode> Nodes { get; }

    /// <summary>
    /// All metavariable names occurring in the pattern, distinct, in order of
    /// first occurrence.
    /// </summary>
    public IReadOnlyList<string> Metavariables { get; }

    internal LabyrinthPatternAst(string pattern, IReadOnlyList<LabyrinthPatternNode> nodes)
    {
        Pattern = pattern;
        Nodes = nodes;

        var seen = new HashSet<string>();
        var names = new List<string>();
        foreach (var node in nodes.Where(n => n.Kind == LabyrinthPatternNodeKind.Metavariable))
        {
            if (seen.Add(node.MetavariableName))
            {
                names.Add(node.MetavariableName);
            }
        }

        Metavariables = names;
    }
}
