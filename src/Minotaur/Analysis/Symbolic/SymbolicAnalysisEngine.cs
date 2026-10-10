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

using Minotaur.Core;
using Minotaur.GrammarGeneration.Models;
using Minotaur.Plugins;
using Minotaur.Parser;

namespace Minotaur.Analysis.Symbolic;

/// <summary>
/// Native symbolic analysis engine for Minotaur that provides KLEE-like capabilities
/// without external dependencies, designed to work seamlessly with StepParser and cognitive graphs.
/// Uses the NuGet-based plugin system for language-specific analysis.
/// </summary>
public class SymbolicAnalysisEngine
{
    private readonly object? _stepParser;
    private readonly LanguagePluginManager _pluginManager;
    private readonly ConstraintSolver _constraintSolver;

    /// <summary>
    /// When true (the default), AnalyzeCode parses the source through the real
    /// StepParser pipeline into a cognitive graph. The mock graph path is isolated
    /// behind this flag for tests that need a deterministic graph without parser
    /// availability (Minotaur issue #121).
    /// </summary>
    public bool UseRealParser { get; set; } = true;

    public SymbolicAnalysisEngine(object? stepParser = null, LanguagePluginManager? pluginManager = null)
    {
        _stepParser = stepParser;
        _pluginManager = pluginManager ?? new LanguagePluginManager();
        _constraintSolver = new ConstraintSolver();
    }

    /// <summary>
    /// Performs symbolic analysis on source code to detect potential errors
    /// </summary>
    public SymbolicAnalysisResult AnalyzeCode(string sourceCode, string language, Grammar? grammar = null)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<SymbolicError>();
        var executionPaths = new List<ExecutionPath>();

        try
        {
            // Step 1: Parse code if parser is available
            CognitiveGraphNode? graph = null;
            if (UseRealParser)
            {
                graph = ParseWithRealParser(sourceCode, language);
            }
            else if (_stepParser != null)
            {
                // Mock path, isolated for tests that need a deterministic graph
                graph = CreateMockCognitiveGraph(sourceCode, language);
            }

            // Step 2: Extract symbolic constraints from the cognitive graph
            var constraints = ExtractConstraints(graph, sourceCode, language);

            // Step 3: Analyze execution paths
            executionPaths = AnalyzeExecutionPaths(constraints, graph);

            // Step 4: Detect potential errors using language-specific analysis
            errors.AddRange(PerformLanguageSpecificAnalysis(sourceCode, language, constraints));

            // Step 5: Detect general symbolic errors
            errors.AddRange(DetectGeneralSymbolicErrors(constraints, executionPaths));

            // Step 6: Generate test cases for found errors
            foreach (var error in errors)
            {
                error.TestCases = GenerateTestCases(error, constraints);
            }

            return new SymbolicAnalysisResult(
                success: true,
                errors: errors,
                executionPaths: executionPaths,
                constraints: constraints,
                analysisTime: DateTime.UtcNow - startTime
            );
        }
        catch (Exception ex)
        {
            return new SymbolicAnalysisResult(
                success: false,
                errors: new List<SymbolicError>
                {
                    new SymbolicError(
                        SymbolicErrorType.AnalysisFailure,
                        new SourceLocation(1, 1),
                        $"Symbolic analysis failed: {ex.Message}",
                        0.9)
                },
                executionPaths: new List<ExecutionPath>(),
                constraints: new List<SymbolicConstraint>(),
                analysisTime: DateTime.UtcNow - startTime
            );
        }
    }

    /// <summary>
    /// Gets the plugin manager used for language-specific analysis
    /// </summary>
    public LanguagePluginManager PluginManager => _pluginManager;

    private void RegisterBuiltInAnalyzers()
    {
        // Language analyzers are now provided by the plugin system
        // No need to register them here as they're integrated into the language plugins
    }

    private CognitiveGraphNode CreateMockCognitiveGraph(string sourceCode, string language)
    {
        // Mock graph, isolated behind UseRealParser=false for deterministic tests
        return new NonTerminalNode("program", 0);
    }

    /// <summary>
    /// Parses source code through the real StepParser pipeline and binds the
    /// resulting cognitive graph with the grammar-driven semantic binder
    /// (Minotaur issue #121). Binding is driven by annotations in the supplied
    /// grammar's metadata; without a grammar, no binding occurs.
    /// </summary>
    public async Task<Binding.BindingResult> ParseAndBindAsync(string sourceCode, string language, Grammar? grammar = null)
    {
        var graph = await ParseWithRealParserAsync(sourceCode, language);

        var binder = new Binding.SemanticBinder(
            language,
            grammar != null ? Binding.GrammarBindingProfile.FromGrammar(grammar) : new Binding.GrammarBindingProfile());
        binder.BindFile(graph, $"{language}://inline");
        binder.ResolvePendingReferences();
        return binder.Result;
    }

    private CognitiveGraphNode ParseWithRealParser(string sourceCode, string language)
    {
        return ParseWithRealParserAsync(sourceCode, language).GetAwaiter().GetResult();
    }

    private async Task<CognitiveGraphNode> ParseWithRealParserAsync(string sourceCode, string language)
    {
        var integration = _stepParser as StepParserIntegration;
        var ownsIntegration = integration == null;
        if (integration == null)
        {
            integration = new StepParserIntegration(new ParserConfiguration { Language = language });
        }

        try
        {
            var graph = await integration.ParseToCognitiveGraphAsync(sourceCode);
            graph.Metadata["language"] = language;
            return graph;
        }
        finally
        {
            if (ownsIntegration)
            {
                integration.Dispose();
            }
        }
    }

    private List<SymbolicConstraint> ExtractConstraints(CognitiveGraphNode? graph, string sourceCode, string language)
    {
        var constraints = new List<SymbolicConstraint>();

        // Extract constraints from source code analysis
        constraints.AddRange(ExtractVariableConstraints(sourceCode));
        constraints.AddRange(ExtractControlFlowConstraints(sourceCode));
        constraints.AddRange(ExtractDataFlowConstraints(sourceCode));

        if (graph != null)
        {
            constraints.AddRange(ExtractGraphConstraints(graph));
        }

        return constraints;
    }

    private List<SymbolicConstraint> ExtractVariableConstraints(string sourceCode)
    {
        var constraints = new List<SymbolicConstraint>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Look for variable assignments
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*\w+\s*="))
            {
                constraints.Add(new SymbolicConstraint(
                    ConstraintType.VariableAssignment,
                    new SourceLocation(i + 1, 1),
                    $"Variable assignment on line {i + 1}",
                    line.Trim()
                ));
            }

            // Look for array/list access
            if (line.Contains("[") && line.Contains("]"))
            {
                constraints.Add(new SymbolicConstraint(
                    ConstraintType.ArrayAccess,
                    new SourceLocation(i + 1, line.IndexOf('[')),
                    "Array access detected",
                    line.Trim()
                ));
            }

            // Look for null checks
            if (line.Contains("null") || line.Contains("None") || line.Contains("undefined"))
            {
                constraints.Add(new SymbolicConstraint(
                    ConstraintType.NullCheck,
                    new SourceLocation(i + 1, 1),
                    "Null reference detected",
                    line.Trim()
                ));
            }
        }

        return constraints;
    }

    private List<SymbolicConstraint> ExtractControlFlowConstraints(string sourceCode)
    {
        var constraints = new List<SymbolicConstraint>();
        var lines = sourceCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("if ") || line.StartsWith("elif ") || line.StartsWith("while "))
            {
                constraints.Add(new SymbolicConstraint(
                    ConstraintType.ConditionalBranch,
                    new SourceLocation(i + 1, 1),
                    "Conditional branch detected",
                    line
                ));
            }
        }

        return constraints;
    }

    private List<SymbolicConstraint> ExtractDataFlowConstraints(string sourceCode)
    {
        var constraints = new List<SymbolicConstraint>();

        // Simple data flow analysis based on variable usage patterns
        var variables = new HashSet<string>();
        var lines = sourceCode.Split('\n');

        foreach (var line in lines)
        {
            // Extract variable names (simplified)
            var matches = System.Text.RegularExpressions.Regex.Matches(line, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                variables.Add(match.Value);
            }
        }

        foreach (var variable in variables)
        {
            constraints.Add(new SymbolicConstraint(
                ConstraintType.DataFlow,
                new SourceLocation(1, 1),
                $"Data flow for variable: {variable}",
                variable
            ));
        }

        return constraints;
    }

    private List<SymbolicConstraint> ExtractGraphConstraints(CognitiveGraphNode graph)
    {
        var constraints = new List<SymbolicConstraint>();

        // Walk the cognitive graph and extract structural constraints
        WalkGraphForConstraints(graph, constraints);

        return constraints;
    }

    private void WalkGraphForConstraints(CognitiveGraphNode node, List<SymbolicConstraint> constraints)
    {
        // Add constraint based on node type
        constraints.Add(new SymbolicConstraint(
            ConstraintType.StructuralPattern,
            new SourceLocation(1, 1),
            $"Graph node: {node.GetType().Name}",
            node.ToString() ?? string.Empty
        ));

        // Recursively walk children
        foreach (var child in node.Children)
        {
            WalkGraphForConstraints(child, constraints);
        }
    }

    private List<ExecutionPath> AnalyzeExecutionPaths(List<SymbolicConstraint> constraints, CognitiveGraphNode? graph)
    {
        var paths = new List<ExecutionPath>();

        // Simple path analysis based on conditional constraints
        var branches = constraints.Where(c => c.Type == ConstraintType.ConditionalBranch).ToList();

        if (branches.Count == 0)
        {
            // Single execution path
            paths.Add(new ExecutionPath("main", constraints, 1.0));
        }
        else
        {
            // Multiple paths based on branching
            foreach (var branch in branches)
            {
                paths.Add(new ExecutionPath(
                    $"branch_{branch.Location.Line}",
                    constraints,
                    0.5)); // Simplified probability
            }
        }

        return paths;
    }

    private List<SymbolicError> PerformLanguageSpecificAnalysis(string sourceCode, string language, List<SymbolicConstraint> constraints)
    {
        var plugin = _pluginManager.GetPlugin(language.ToLowerInvariant());

        if (plugin is ISymbolicAnalysisPlugin symbolicPlugin)
        {
            return symbolicPlugin.AnalyzeSymbolic(sourceCode, constraints);
        }

        return new List<SymbolicError>();
    }

    private List<SymbolicError> DetectGeneralSymbolicErrors(List<SymbolicConstraint> constraints, List<ExecutionPath> paths)
    {
        var errors = new List<SymbolicError>();

        // Detect potential null pointer access
        errors.AddRange(DetectNullPointerErrors(constraints));

        // Detect potential array bounds errors
        errors.AddRange(DetectArrayBoundsErrors(constraints));

        // Detect potential division by zero
        errors.AddRange(DetectDivisionByZeroErrors(constraints));

        return errors;
    }

    private List<SymbolicError> DetectNullPointerErrors(List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        var nullConstraints = constraints.Where(c => c.Type == ConstraintType.NullCheck).ToList();

        foreach (var constraint in nullConstraints)
        {
            errors.Add(new SymbolicError(
                SymbolicErrorType.NullPointerAccess,
                constraint.Location,
                "Potential null pointer access detected",
                0.7
            ));
        }

        return errors;
    }

    private List<SymbolicError> DetectArrayBoundsErrors(List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        var arrayConstraints = constraints.Where(c => c.Type == ConstraintType.ArrayAccess).ToList();

        foreach (var constraint in arrayConstraints)
        {
            errors.Add(new SymbolicError(
                SymbolicErrorType.ArrayBoundsViolation,
                constraint.Location,
                "Potential array bounds violation",
                0.6
            ));
        }

        return errors;
    }

    private List<SymbolicError> DetectDivisionByZeroErrors(List<SymbolicConstraint> constraints)
    {
        var errors = new List<SymbolicError>();

        // Look for division operations in constraints
        var divisionConstraints = constraints.Where(c =>
            c.Expression.Contains("/") || c.Expression.Contains("div")).ToList();

        foreach (var constraint in divisionConstraints)
        {
            errors.Add(new SymbolicError(
                SymbolicErrorType.DivisionByZero,
                constraint.Location,
                "Potential division by zero",
                0.5
            ));
        }

        return errors;
    }

    private List<TestCase> GenerateTestCases(SymbolicError error, List<SymbolicConstraint> constraints)
    {
        var testCases = new List<TestCase>();

        // Generate a basic test case that might trigger the error
        testCases.Add(new TestCase(
            $"test_{error.Type}_{error.Location.Line}",
            GenerateTestInput(error, constraints),
            error.Message
        ));

        return testCases;
    }

    private Dictionary<string, object> GenerateTestInput(SymbolicError error, List<SymbolicConstraint> constraints)
    {
        var input = new Dictionary<string, object>();

        // Generate inputs based on error type
        switch (error.Type)
        {
            case SymbolicErrorType.ArrayBoundsViolation:
                input["array_size"] = 5;
                input["access_index"] = 10; // Out of bounds
                break;

            case SymbolicErrorType.NullPointerAccess:
                input["nullable_value"] = null!;
                break;

            case SymbolicErrorType.DivisionByZero:
                input["divisor"] = 0;
                break;
        }

        return input;
    }
}
