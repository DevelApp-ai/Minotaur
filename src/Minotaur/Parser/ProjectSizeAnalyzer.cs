/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

namespace Minotaur.Parser;

/// <summary>
/// Analyzes project characteristics to determine appropriate cognitive graph version.
/// </summary>
public class ProjectSizeAnalyzer
{
    /// <summary>
    /// Threshold for lines of code above which V2 is recommended (default: 10,000 lines).
    /// </summary>
    public int LargeProjectLineThreshold { get; set; } = 10_000;

    /// <summary>
    /// Threshold for character count above which V2 is recommended (default: 500,000 chars).
    /// </summary>
    public int LargeProjectCharThreshold { get; set; } = 500_000;

    /// <summary>
    /// Threshold for file count above which V2 is recommended (default: 100 files).
    /// </summary>
    public int LargeProjectFileThreshold { get; set; } = 100;

    /// <summary>
    /// Determines if a project should use V2 based on source code metrics.
    /// </summary>
    /// <param name="sourceCode">The source code to analyze.</param>
    /// <returns>True if V2 should be used, false if V1 is sufficient.</returns>
    public bool ShouldUseV2(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return false;

        // Check character count
        if (sourceCode.Length >= LargeProjectCharThreshold)
            return true;

        // Check line count
        int lineCount = CountLines(sourceCode);
        if (lineCount >= LargeProjectLineThreshold)
            return true;

        return false;
    }

    /// <summary>
    /// Determines if a project should use V2 based on multiple files.
    /// </summary>
    /// <param name="files">Dictionary of file paths to their content.</param>
    /// <returns>True if V2 should be used, false if V1 is sufficient.</returns>
    public bool ShouldUseV2(Dictionary<string, string> files)
    {
        if (files == null || files.Count == 0)
            return false;

        // Check file count
        if (files.Count >= LargeProjectFileThreshold)
            return true;

        // Check total character count
        long totalChars = files.Values.Sum(content => (long)content.Length);
        if (totalChars >= LargeProjectCharThreshold)
            return true;

        // Check total line count
        int totalLines = files.Values.Sum(content => CountLines(content));
        if (totalLines >= LargeProjectLineThreshold)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the recommended cognitive graph version based on source code analysis.
    /// </summary>
    /// <param name="sourceCode">The source code to analyze.</param>
    /// <returns>The recommended cognitive graph version.</returns>
    public CognitiveGraphVersion GetRecommendedVersion(string sourceCode)
    {
        return ShouldUseV2(sourceCode) ? CognitiveGraphVersion.V2 : CognitiveGraphVersion.V1;
    }

    /// <summary>
    /// Gets the recommended cognitive graph version based on multiple files.
    /// </summary>
    /// <param name="files">Dictionary of file paths to their content.</param>
    /// <returns>The recommended cognitive graph version.</returns>
    public CognitiveGraphVersion GetRecommendedVersion(Dictionary<string, string> files)
    {
        return ShouldUseV2(files) ? CognitiveGraphVersion.V2 : CognitiveGraphVersion.V1;
    }

    /// <summary>
    /// Counts the number of lines in the source code.
    /// </summary>
    private static int CountLines(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return 0;

        int count = 0;
        for (int i = 0; i < sourceCode.Length; i++)
        {
            if (sourceCode[i] == '\n')
                count++;
        }
        // Add 1 for the last line if source is not empty and doesn't end with newline
        if (sourceCode.Length > 0 && sourceCode[sourceCode.Length - 1] != '\n')
            count++;
        return count;
    }
}
