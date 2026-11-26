using System.Text.RegularExpressions;

namespace Minotaur.Labyrinth;

/// <summary>
/// Interpolates a rule's <c>fix:</c> suggestion (issue #246, LAB track): every
/// <c>$NAME</c> metavariable reference in the fix text is replaced by the text of
/// the node bound to <c>$NAME</c> in the match context. A reference that is not
/// bound is left as-is (the fix is a hint, never a crash). The node's text is its
/// <see cref="ILabyrinthMatchNode.Name"/> (identifier/callee text), falling back
/// to the node type.
/// </summary>
public static class LabyrinthFixInterpolator
{
    private static readonly Regex MetavarRef = new(@"\$[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The metavariable names referenced by a fix text (without <c>$</c>), distinct, in order.</summary>
    public static IReadOnlyList<string> MetavariablesIn(string? fix) =>
        string.IsNullOrEmpty(fix)
            ? Array.Empty<string>()
            : MetavarRef.Matches(fix).Select(m => m.Value.TrimStart('$')).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Replaces every <c>$NAME</c> in <paramref name="fix"/> with the bound node's text.</summary>
    public static string Interpolate(string fix, LabyrinthMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(fix);
        ArgumentNullException.ThrowIfNull(context);

        return MetavarRef.Replace(fix, m =>
        {
            var name = m.Value.TrimStart('$');
            return context.TryGet(name, out var node) ? NodeText(node) : m.Value;
        });
    }

    private static string NodeText(ILabyrinthMatchNode node) => node.Name ?? node.NodeType;
}
