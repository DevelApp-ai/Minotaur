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

using System.Text.RegularExpressions;
using Minotaur.Analysis.Symbolic;

namespace Minotaur.Plugins;

/// <summary>
/// C# symbolic analysis plugin providing language-specific error detection
/// </summary>
public class CSharpSymbolicAnalysisPlugin : ISymbolicAnalysisPlugin
{
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        errors.AddRange(DetectNullReferenceErrors(sourceCode));
        errors.AddRange(DetectIndexOutOfRangeErrors(sourceCode, constraints));
        errors.AddRange(DetectDisposalIssues(sourceCode));
        errors.AddRange(DetectAsyncVoidMethods(sourceCode));

        return errors;
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return new List<ErrorPattern>
        {
            new ErrorPattern(
                "null_reference",
                SymbolicErrorType.NullPointerAccess,
                @"\bnull\.\w+",
                0.95,
                "Check for null before accessing members"
            ),
            new ErrorPattern(
                "undisposed_resource",
                SymbolicErrorType.MemoryLeak,
                @"new\s+(FileStream|StreamReader|StreamWriter|HttpClient)",
                0.6,
                "Consider using 'using' statement for disposable resources"
            ),
            new ErrorPattern(
                "async_void",
                SymbolicErrorType.AnalysisFailure,
                @"async\s+void\s+\w+",
                0.8,
                "Prefer async Task over async void except for event handlers"
            )
        };
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return errorType switch
        {
            SymbolicErrorType.NullPointerAccess => 0.9,
            SymbolicErrorType.ArrayBoundsViolation => 0.7,
            SymbolicErrorType.MemoryLeak => 0.6,
            SymbolicErrorType.DivisionByZero => 0.8,
            _ => 0.5
        };
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        var testCases = new List<TestCase>();

        switch (error.Type)
        {
            case SymbolicErrorType.NullPointerAccess:
                testCases.Add(new TestCase(
                    "null_reference_test",
                    new Dictionary<string, object> { ["object"] = (object?)null },
                    "NullReferenceException should be thrown"
                ));
                break;

            case SymbolicErrorType.ArrayBoundsViolation:
                testCases.Add(new TestCase(
                    "index_out_of_range_test",
                    new Dictionary<string, object> { ["array"] = new int[5], ["index"] = 10 },
                    "IndexOutOfRangeException should be thrown"
                ));
                break;

            case SymbolicErrorType.MemoryLeak:
                testCases.Add(new TestCase(
                    "resource_leak_test",
                    new Dictionary<string, object> { ["dispose_called"] = false },
                    "Resource should be properly disposed"
                ));
                break;
        }

        return testCases;
    }

    private List<SymbolicError> DetectNullReferenceErrors(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (Regex.IsMatch(line, @"\bnull\.\w+"))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.NullPointerAccess,
                    new SourceLocation(i + 1, line.IndexOf("null.")),
                    "Null reference access",
                    0.95
                ));
            }

            // Detect potential null parameter usage
            if (Regex.IsMatch(line, @"(\w+)\s*\?\s*\.\s*\w+") && !line.Contains("?."))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.NullPointerAccess,
                    new SourceLocation(i + 1, 1),
                    "Potential null reference without null-conditional operator",
                    0.6
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectIndexOutOfRangeErrors(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();
        var arrayConstraints = constraints.Where(c => c.Type == ConstraintType.ArrayAccess);

        foreach (var constraint in arrayConstraints)
        {
            errors.Add(new SymbolicError(
                SymbolicErrorType.ArrayBoundsViolation,
                constraint.Location,
                "Potential array index out of range",
                0.5
            ));
        }

        return errors;
    }

    private List<SymbolicError> DetectDisposalIssues(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect object creation without disposal
            if (Regex.IsMatch(line, @"new\s+(FileStream|StreamReader|StreamWriter|HttpClient)") &&
                !line.Contains("using") && !IsInUsingStatement(lines, i))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.MemoryLeak,
                    new SourceLocation(i + 1, 1),
                    "Disposable resource created without using statement",
                    0.6
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectAsyncVoidMethods(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (Regex.IsMatch(line, @"async\s+void\s+\w+") && !IsEventHandler(line))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.AnalysisFailure,
                    new SourceLocation(i + 1, line.IndexOf("async")),
                    "async void should be avoided except for event handlers",
                    0.8
                ));
            }
        }

        return errors;
    }

    private bool IsInUsingStatement(string[] lines, int currentLine)
    {
        // Look backwards for using statement
        for (int i = currentLine; i >= Math.Max(0, currentLine - 3); i--)
        {
            if (lines[i].TrimStart().StartsWith("using ("))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsEventHandler(string line)
    {
        return line.Contains("EventArgs") || line.Contains("_Click") || line.Contains("_Changed");
    }
}

/// <summary>
/// Python symbolic analysis plugin providing language-specific error detection
/// </summary>
public class PythonSymbolicAnalysisPlugin : ISymbolicAnalysisPlugin
{
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        errors.AddRange(DetectIndentationErrors(sourceCode));
        errors.AddRange(DetectNameErrors(sourceCode));
        errors.AddRange(DetectTypeErrors(sourceCode, constraints));
        errors.AddRange(DetectIndexErrors(sourceCode, constraints));

        return errors;
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return new List<ErrorPattern>
        {
            new ErrorPattern(
                "list_index_out_of_range",
                SymbolicErrorType.ArrayBoundsViolation,
                @"\w+\[\d+\]",
                0.7,
                "Check array bounds before accessing elements"
            ),
            new ErrorPattern(
                "none_attribute_access",
                SymbolicErrorType.NullPointerAccess,
                @"None\.\w+",
                0.9,
                "Check for None before accessing attributes"
            ),
            new ErrorPattern(
                "division_by_zero",
                SymbolicErrorType.DivisionByZero,
                @"\w+\s*/\s*0",
                0.95,
                "Add zero division check"
            )
        };
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return errorType switch
        {
            SymbolicErrorType.NullPointerAccess => 0.85,
            SymbolicErrorType.ArrayBoundsViolation => 0.75,
            SymbolicErrorType.DivisionByZero => 0.9,
            SymbolicErrorType.AnalysisFailure => 0.8, // For syntax/indentation errors
            _ => 0.5
        };
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        var testCases = new List<TestCase>();

        switch (error.Type)
        {
            case SymbolicErrorType.NullPointerAccess:
                testCases.Add(new TestCase(
                    "none_access_test",
                    new Dictionary<string, object> { ["value"] = (object?)null },
                    "AttributeError should be raised"
                ));
                break;

            case SymbolicErrorType.ArrayBoundsViolation:
                testCases.Add(new TestCase(
                    "index_error_test",
                    new Dictionary<string, object> { ["list"] = new[] { 1, 2, 3 }, ["index"] = 5 },
                    "IndexError should be raised"
                ));
                break;

            case SymbolicErrorType.DivisionByZero:
                testCases.Add(new TestCase(
                    "zero_division_test",
                    new Dictionary<string, object> { ["dividend"] = 10, ["divisor"] = 0 },
                    "ZeroDivisionError should be raised"
                ));
                break;
        }

        return testCases;
    }

    private List<SymbolicError> DetectIndentationErrors(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Check for inconsistent indentation
            if (line.TrimStart() != line && line.Contains("\t") && line.Contains("    "))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.AnalysisFailure,
                    new SourceLocation(i + 1, 1),
                    "Mixed tabs and spaces detected",
                    0.95
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectNameErrors(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');
        var definedVariables = new HashSet<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Track variable definitions
            var assignmentMatch = Regex.Match(line, @"^\s*(\w+)\s*=");
            if (assignmentMatch.Success)
            {
                definedVariables.Add(assignmentMatch.Groups[1].Value);
            }

            // Check for undefined variable usage
            var usageMatches = Regex.Matches(line, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
            foreach (Match match in usageMatches)
            {
                var varName = match.Groups[1].Value;
                if (!definedVariables.Contains(varName) &&
                    !IsPythonBuiltin(varName) &&
                    !line.TrimStart().StartsWith($"{varName} ="))
                {
                    errors.Add(new SymbolicError(
                        SymbolicErrorType.AnalysisFailure,
                        new SourceLocation(i + 1, match.Index),
                        $"Potentially undefined variable: {varName}",
                        0.6
                    ));
                }
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectTypeErrors(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect string + number operations
            if (Regex.IsMatch(line, @"""[^""]*""\s*\+\s*\d+") ||
                Regex.IsMatch(line, @"\d+\s*\+\s*""[^""]*"""))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.AnalysisFailure,
                    new SourceLocation(i + 1, 1),
                    "Potential type error: string + number",
                    0.8
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectIndexErrors(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect potential negative indexing issues
            if (Regex.IsMatch(line, @"\w+\[-\d+\]"))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.ArrayBoundsViolation,
                    new SourceLocation(i + 1, line.IndexOf('[')),
                    "Negative array index detected",
                    0.4 // Lower confidence as negative indexing is valid in Python
                ));
            }
        }

        return errors;
    }

    private bool IsPythonBuiltin(string name)
    {
        var builtins = new HashSet<string>
        {
            "print", "len", "range", "str", "int", "float", "list", "dict", "set",
            "True", "False", "None", "if", "else", "elif", "for", "while", "def",
            "class", "import", "from", "return", "break", "continue", "pass"
        };
        return builtins.Contains(name);
    }
}

/// <summary>
/// JavaScript symbolic analysis plugin providing language-specific error detection
/// </summary>
public class JavaScriptSymbolicAnalysisPlugin : ISymbolicAnalysisPlugin
{
    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        errors.AddRange(DetectNullUndefinedErrors(sourceCode));
        errors.AddRange(DetectTypeCoercionIssues(sourceCode));
        errors.AddRange(DetectAsyncAwaitIssues(sourceCode));

        return errors;
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return new List<ErrorPattern>
        {
            new ErrorPattern(
                "null_undefined_access",
                SymbolicErrorType.NullPointerAccess,
                @"(null|undefined)\.\w+",
                0.9,
                "Check for null/undefined before property access"
            ),
            new ErrorPattern(
                "type_coercion_issue",
                SymbolicErrorType.AnalysisFailure,
                @"""[^""]*""\s*[+\-*/]\s*\d+",
                0.6,
                "Be explicit about type conversions"
            ),
            new ErrorPattern(
                "await_without_async",
                SymbolicErrorType.AnalysisFailure,
                @"await\s+\w+",
                0.8,
                "await must be used within async function"
            )
        };
    }

    public double GetErrorConfidence(SymbolicErrorType errorType)
    {
        return errorType switch
        {
            SymbolicErrorType.NullPointerAccess => 0.85,
            SymbolicErrorType.AnalysisFailure => 0.7,
            SymbolicErrorType.ArrayBoundsViolation => 0.6,
            _ => 0.5
        };
    }

    public List<TestCase> GenerateTestCases(SymbolicError error, string sourceCode)
    {
        var testCases = new List<TestCase>();

        switch (error.Type)
        {
            case SymbolicErrorType.NullPointerAccess:
                testCases.Add(new TestCase(
                    "null_access_test",
                    new Dictionary<string, object> { ["object"] = (object?)null },
                    "TypeError should be thrown"
                ));
                break;

            case SymbolicErrorType.AnalysisFailure when error.Message.Contains("type coercion"):
                testCases.Add(new TestCase(
                    "type_coercion_test",
                    new Dictionary<string, object> { ["string"] = "hello", ["number"] = 42 },
                    "Unexpected type coercion result"
                ));
                break;
        }

        return testCases;
    }

    private List<SymbolicError> DetectNullUndefinedErrors(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (Regex.IsMatch(line, @"(null|undefined)\.\w+"))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.NullPointerAccess,
                    new SourceLocation(i + 1, 1),
                    "Null/undefined property access",
                    0.9
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectTypeCoercionIssues(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect implicit type coercion
            if (Regex.IsMatch(line, @"""[^""]*""\s*==\s*\d+") ||
                Regex.IsMatch(line, @"\d+\s*==\s*""[^""]*"""))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.AnalysisFailure,
                    new SourceLocation(i + 1, 1),
                    "Potential type coercion issue with == operator",
                    0.7
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectAsyncAwaitIssues(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect await without async
            if (line.Contains("await ") && i > 0)
            {
                var functionLine = FindFunctionDeclaration(lines, i);
                if (functionLine != null && !functionLine.Contains("async"))
                {
                    errors.Add(new SymbolicError(
                        SymbolicErrorType.AnalysisFailure,
                        new SourceLocation(i + 1, line.IndexOf("await")),
                        "await used outside async function",
                        0.95
                    ));
                }
            }
        }

        return errors;
    }

    private string? FindFunctionDeclaration(string[] lines, int currentLine)
    {
        for (int i = currentLine; i >= 0; i--)
        {
            if (lines[i].TrimStart().StartsWith("function ") ||
                lines[i].TrimStart().StartsWith("async function ") ||
                Regex.IsMatch(lines[i], @"=>\s*{"))
            {
                return lines[i];
            }
        }
        return null;
    }
}