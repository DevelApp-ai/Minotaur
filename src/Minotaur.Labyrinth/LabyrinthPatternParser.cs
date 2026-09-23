namespace Minotaur.Labyrinth;

/// <summary>
/// A node of the pattern mini-AST produced by <see cref="LabyrinthPatternParser"/> (issue #101).
/// Layer 2 (issue #102) translates this structure into C# Expression Trees.
/// </summary>
public abstract record LabyrinthPatternNode
{
    /// <summary>Literal target-grammar syntax to match exactly.</summary>
    public sealed record Literal(string Text) : LabyrinthPatternNode;

    /// <summary>A metavariable capture: binds the matched node under <see cref="Name"/>.</summary>
    public sealed record Metavariable(string Name) : LabyrinthPatternNode;

    /// <summary>The ellipsis operator: matches zero or more nodes in this position.</summary>
    public sealed record Ellipsis : LabyrinthPatternNode;
}

/// <summary>
/// The parsed representation of a single Labyrinth pattern:
/// a flat sequence of mini-AST nodes plus the set of metavariables it defines.
/// </summary>
public sealed class LabyrinthPatternAst
{
    /// <summary>The pattern's mini-AST node sequence.</summary>
    public IReadOnlyList<LabyrinthPatternNode> Nodes { get; init; } = Array.Empty<LabyrinthPatternNode>();

    /// <summary>Distinct metavariable names defined by this pattern, in order of first occurrence.</summary>
    public IReadOnlyList<string> Metavariables { get; init; } = Array.Empty<string>();

    /// <summary>The original pattern text.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>True when the pattern contains at least one ellipsis operator.</summary>
    public bool HasEllipsis { get; init; }
}

/// <summary>
/// Layer 1 pattern parser (issue #101): parses the token stream produced by
/// <see cref="LabyrinthPatternLexer"/> into a mini-AST structure. The mini-AST is
/// the input for Layer 2 Expression Tree compilation (issue #102); pattern-internal
/// target-grammar syntax validation happens in the engine against the loaded grammar
/// via DevelApp.StepLexer/StepParser.
/// </summary>
public static class LabyrinthPatternParser
{
    /// <summary>
    /// Parses a raw pattern string into a mini-AST.
    /// </summary>
    public static LabyrinthPatternAst Parse(string pattern)
    {
        var tokens = LabyrinthPatternLexer.Tokenize(pattern);
        var nodes = new List<LabyrinthPatternNode>(tokens.Count);
        var metas = new List<string>();
        var hasEllipsis = false;

        foreach (var t in tokens)
        {
            switch (t.Kind)
            {
                case LabyrinthTokenKind.Literal:
                    nodes.Add(new LabyrinthPatternNode.Literal(t.Text));
                    break;
                case LabyrinthTokenKind.Metavariable:
                    nodes.Add(new LabyrinthPatternNode.Metavariable(t.Text));
                    if (!metas.Contains(t.Text))
                    {
                        metas.Add(t.Text);
                    }

                    break;
                case LabyrinthTokenKind.Ellipsis:
                    nodes.Add(new LabyrinthPatternNode.Ellipsis());
                    hasEllipsis = true;
                    break;
                default:
                    throw new LabyrinthRuleException($"Unknown token kind '{t.Kind}'.");
            }
        }

        return new LabyrinthPatternAst
        {
            Nodes = nodes,
            Metavariables = metas,
            Source = pattern,
            HasEllipsis = hasEllipsis
        };
    }
}
