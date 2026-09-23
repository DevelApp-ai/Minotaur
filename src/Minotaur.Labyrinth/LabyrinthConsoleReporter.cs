using System.Text;

namespace Minotaur.Labyrinth;

/// <summary>
/// Exit codes for CI pipelines running Labyrinth analysis (issue #105),
/// following the grep/codeql convention.
/// </summary>
public static class LabyrinthExitCodes
{
    /// <summary>Analysis ran and reported no (unsuppressed) findings.</summary>
    public const int Success = 0;

    /// <summary>Analysis ran and reported one or more findings.</summary>
    public const int Findings = 1;

    /// <summary>The analysis could not run: invalid rules, baseline or graph error.</summary>
    public const int ConfigurationError = 2;
}

/// <summary>
/// Console reporting for Labyrinth findings (Labyrinth Layer 5, issue #105).
/// Produces SARIF-friendly plain text: one block per finding with rule id,
/// severity, location, message and the full rendered source→sink path, plus a
/// trailing summary line that a human can grep and a CI can show inline.
/// </summary>
public sealed class LabyrinthConsoleReporter
{
    private readonly TextWriter _writer;

    public LabyrinthConsoleReporter(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Writes all findings to the configured writer and returns the process
    /// exit code per the <see cref="LabyrinthExitCodes"/> convention.
    /// </summary>
    public int Report(IReadOnlyList<LabyrinthTaintFinding> findings, int suppressedCount = 0)
    {
        if (findings is null)
        {
            throw new ArgumentNullException(nameof(findings));
        }

        foreach (var finding in findings)
        {
            WriteFinding(finding);
        }

        WriteSummary(findings.Count, suppressedCount);
        return findings.Count == 0 ? LabyrinthExitCodes.Success : LabyrinthExitCodes.Findings;
    }

    /// <summary>Writes a rule-pack load error and returns the <see cref="LabyrinthExitCodes.ConfigurationError"/> exit code.</summary>
    public int ReportConfigurationError(LabyrinthRulePackResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _writer.WriteLine($"labyrinth: error: rule pack is invalid ({result.Errors.Count} error(s)):");
        foreach (var error in result.Errors)
        {
            _writer.WriteLine($"  - {error}");
        }

        return LabyrinthExitCodes.ConfigurationError;
    }

    private void WriteFinding(LabyrinthTaintFinding finding)
    {
        var sb = new StringBuilder(256);
        sb.Append(finding.Severity.ToString().ToUpperInvariant());
        sb.Append(" [").Append(finding.RuleId).Append(']');
        if (finding.Sink is ILabyrinthFindingLocation sinkLocation)
        {
            sb.Append(" @").Append(sinkLocation.FilePath)
                .Append(':').Append(sinkLocation.StartLine)
                .Append(':').Append(sinkLocation.StartColumn);
        }

        sb.AppendLine();
        sb.Append("  ").AppendLine(finding.Message);
        sb.Append("  Path:").AppendLine();
        foreach (var line in LabyrinthPathRenderer.RenderPath(finding).Split('\n'))
        {
            sb.Append("    ").AppendLine(line.TrimEnd('\r'));
        }

        _writer.Write(sb.ToString());
    }

    private void WriteSummary(int findingCount, int suppressedCount)
    {
        var sb = new StringBuilder(64);
        sb.Append(findingCount).Append(" finding(s)");
        if (suppressedCount > 0)
        {
            sb.Append(", ").Append(suppressedCount).Append(" suppressed by baseline");
        }

        sb.Append(findingCount == 0 ? " — clean." : ".");
        _writer.WriteLine(sb.ToString());
    }
}
