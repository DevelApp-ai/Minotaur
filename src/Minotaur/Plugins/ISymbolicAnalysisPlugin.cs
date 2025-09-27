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

using Minotaur.Analysis.Symbolic;

namespace Minotaur.Plugins;

/// <summary>
/// Interface for language plugins that support symbolic analysis capabilities.
/// This extends the plugin system to provide language-specific error detection and analysis.
/// </summary>
public interface ISymbolicAnalysisPlugin
{
    /// <summary>
    /// Performs language-specific symbolic analysis on source code
    /// </summary>
    /// <param name="sourceCode">The source code to analyze</param>
    /// <param name="constraints">Symbolic constraints extracted from the code</param>
    /// <returns>List of symbolic errors detected</returns>
    List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints);

    /// <summary>
    /// Gets language-specific error patterns that can be detected
    /// </summary>
    /// <returns>List of error patterns supported by this plugin</returns>
    List<ErrorPattern> GetErrorPatterns();

    /// <summary>
    /// Gets the confidence level for a specific error type in this language
    /// </summary>
    /// <param name="errorType">The type of error</param>
    /// <returns>Confidence level between 0.0 and 1.0</returns>
    double GetErrorConfidence(SymbolicErrorType errorType);

    /// <summary>
    /// Generates test cases for a specific error to help validate the analysis
    /// </summary>
    /// <param name="error">The symbolic error to generate test cases for</param>
    /// <param name="sourceCode">The original source code</param>
    /// <returns>List of test cases that could trigger the error</returns>
    List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode);
}