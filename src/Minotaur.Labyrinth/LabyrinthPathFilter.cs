using System.Text.RegularExpressions;

namespace Minotaur.Labyrinth;

/// <summary>
/// File-path filter for a rule (YAML <c>paths:</c> with <c>include:</c>/<c>exclude:</c>
/// glob lists, Semgrep-style). A rule applies to a scanned file when the path
/// matches at least one include glob (or include is empty) and no exclude glob.
/// <para>
/// Glob semantics (v1, gitignore-flavored): <c>*</c> matches within a path segment,
/// <c>?</c> one character within a segment, <c>**</c> across segments
/// (<c>**/x</c> also matches a root-level <c>x</c>). A leading <c>/</c> is dropped:
/// all patterns are root-anchored. Matching is ordinal and case-sensitive on the
/// normalized (forward-slash) path. A null path matches only when no include
/// globs are set.
/// </para>
/// </summary>
public sealed class LabyrinthPathFilter
{
    /// <summary>Globs the file path must match (at least one); empty = all paths.</summary>
    public List<string> Include { get; set; } = new();

    /// <summary>Globs the file path must NOT match; exclude wins over include.</summary>
    public List<string> Exclude { get; set; } = new();

    /// <summary>True when neither include nor exclude is set (rule applies everywhere).</summary>
    public bool IsEmpty => Include.Count == 0 && Exclude.Count == 0;

    /// <summary>Whether a rule with this filter applies to <paramref name="path"/>.</summary>
    public bool Matches(string? path)
    {
        if (IsEmpty)
        {
            return true;
        }

        var normalized = NormalizePath(path);
        if (normalized is null)
        {
            // No path information: only rules without include globs apply.
            return Include.Count == 0;
        }

        foreach (var exclude in Exclude)
        {
            if (GlobMatches(exclude, normalized))
            {
                return false;
            }
        }

        if (Include.Count == 0)
        {
            return true;
        }

        foreach (var include in Include)
        {
            if (GlobMatches(include, normalized))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Normalizes a path to forward slashes; null when null/empty.</summary>
    public static string? NormalizePath(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : path.Replace('\\', '/').TrimStart('/');

    /// <summary>Matches a glob pattern against a normalized path.</summary>
    public static bool GlobMatches(string? pattern, string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(pattern) || normalizedPath is null)
        {
            return false;
        }

        var p = pattern.Trim().Replace('\\', '/').TrimStart('/');
        return Regex.IsMatch(normalizedPath, "^" + GlobToRegex(p) + "$", RegexOptions.CultureInvariant);
    }

    private static string GlobToRegex(string pattern)
    {
        var sb = new System.Text.StringBuilder(pattern.Length * 2);
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    // '**' across segments; '**/' also matches zero segments.
                    if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        sb.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        sb.Append(".*");
                        i += 1;
                    }
                }
                else
                {
                    sb.Append("[^/]*");
                }
            }
            else if (c == '?')
            {
                sb.Append("[^/]");
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }

        return sb.ToString();
    }
}
