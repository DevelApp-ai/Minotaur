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

namespace Minotaur.Analysis.Symbolic;

/// <summary>
/// Python-specific symbolic analyzer
/// </summary>
public class PythonSymbolicAnalyzer : ILanguageSymbolicAnalyzer
{
    public string LanguageName => "Python";

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        // Python-specific analysis
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
                    SymbolicErrorType.AnalysisFailure, // Using as syntax error
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
                        SymbolicErrorType.AnalysisFailure, // Using as name error
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
                    SymbolicErrorType.AnalysisFailure, // Using as type error
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
/// JavaScript-specific symbolic analyzer
/// </summary>
public class JavaScriptSymbolicAnalyzer : ILanguageSymbolicAnalyzer
{
    public string LanguageName => "JavaScript";

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
            )
        };
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

/// <summary>
/// C#-specific symbolic analyzer
/// </summary>
public class CSharpSymbolicAnalyzer : ILanguageSymbolicAnalyzer
{
    public string LanguageName => "C#";

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        errors.AddRange(DetectNullReferenceErrors(sourceCode));
        errors.AddRange(DetectIndexOutOfRangeErrors(sourceCode, constraints));
        errors.AddRange(DetectDisposalIssues(sourceCode));

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
                @"new\s+\w+\([^)]*\)\s*;",
                0.4,
                "Consider using 'using' statement for disposable resources"
            )
        };
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
                !line.Contains("using"))
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
}

/// <summary>
/// Java-specific symbolic analyzer
/// </summary>
public class JavaSymbolicAnalyzer : ILanguageSymbolicAnalyzer
{
    public string LanguageName => "Java";

    public List<SymbolicError> AnalyzeSymbolic(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        errors.AddRange(DetectNullPointerErrors(sourceCode));
        errors.AddRange(DetectArrayIndexErrors(sourceCode, constraints));
        errors.AddRange(DetectResourceLeaks(sourceCode));

        return errors;
    }

    public List<ErrorPattern> GetErrorPatterns()
    {
        return new List<ErrorPattern>
        {
            new ErrorPattern(
                "null_pointer_exception",
                SymbolicErrorType.NullPointerAccess,
                @"\bnull\.\w+",
                0.95,
                "Check for null before method/field access"
            ),
            new ErrorPattern(
                "array_index_out_of_bounds",
                SymbolicErrorType.ArrayBoundsViolation,
                @"\w+\[\d+\]",
                0.6,
                "Validate array index bounds"
            )
        };
    }

    private List<SymbolicError> DetectNullPointerErrors(string sourceCode)
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
                    "Null pointer exception",
                    0.95
                ));
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectArrayIndexErrors(string sourceCode, List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Detect hard-coded array access that might be out of bounds
            var matches = Regex.Matches(line, @"\w+\[(\d+)\]");
            foreach (Match match in matches)
            {
                var index = int.Parse(match.Groups[1].Value);
                if (index > 10) // Arbitrary threshold for demonstration
                {
                    errors.Add(new SymbolicError(
                        SymbolicErrorType.ArrayBoundsViolation,
                        new SourceLocation(i + 1, match.Index),
                        $"High array index ({index}) may cause IndexOutOfBoundsException",
                        0.4
                    ));
                }
            }
        }

        return errors;
    }

    private List<SymbolicError> DetectResourceLeaks(string sourceCode)
    {
        var errors = new List<SymbolicError>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Detect resource creation without try-with-resources
            if (Regex.IsMatch(line, @"new\s+(FileInputStream|FileOutputStream|BufferedReader)") &&
                !IsInTryWithResources(lines, i))
            {
                errors.Add(new SymbolicError(
                    SymbolicErrorType.MemoryLeak,
                    new SourceLocation(i + 1, 1),
                    "Resource created without try-with-resources",
                    0.5
                ));
            }
        }

        return errors;
    }

    private bool IsInTryWithResources(string[] lines, int currentLine)
    {
        // Look backwards for try-with-resources pattern
        for (int i = currentLine; i >= Math.Max(0, currentLine - 5); i--)
        {
            if (lines[i].TrimStart().StartsWith("try ("))
            {
                return true;
            }
        }
        return false;
    }
}