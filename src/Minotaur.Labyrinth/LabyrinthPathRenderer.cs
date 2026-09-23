using System.Text;

namespace Minotaur.Labyrinth;

/// <summary>
/// Renders the source→sink data-flow path of a finding as a compact,
/// human-readable chain (Labyrinth Layer 5, issue #105).
/// </summary>
public static class LabyrinthPathRenderer
{
    /// <summary>
    /// Renders a single node as <c>NodeType:'Name'</c>, with a
    /// <c>@file:line:col</c> suffix when the node carries location information.
    /// </summary>
    public static string Render(ILabyrinthMatchNode node)
    {
        var sb = new StringBuilder(32);
        sb.Append(node.NodeType);
        if (node.Name is not null)
        {
            sb.Append(":").Append(node.Name);
        }

        if (node is ILabyrinthFindingLocation location)
        {
            sb.Append(" @").Append(location.FilePath)
                .Append(':').Append(location.StartLine)
                .Append(':').Append(location.StartColumn);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the full path of a finding as an indented chain, one node per
    /// line, with <c>-></c> flow markers: e.g.
    /// <code>
    /// FunctionCall:ReceiveInput @app.cs:12:5
    ///   -> Identifier:temp
    ///     -> FunctionCall:ExecuteQuery @app.cs:15:1
    /// </code>
    /// </summary>
    public static string RenderPath(LabyrinthTaintFinding finding)
    {
        var sb = new StringBuilder(64 * finding.Path.Count);
        for (var i = 0; i < finding.Path.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(' ', 2 * i).Append("-> ");
            }

            sb.Append(Render(finding.Path[i]));
            if (i < finding.Path.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
