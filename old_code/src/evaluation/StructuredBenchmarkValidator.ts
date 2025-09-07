/**
 * Structured Benchmark Validator
 *
 * Enhanced version of BenchmarkValidator that produces structured error data
 * for AST-guided code correction. Extends the original validator with
 * machine-readable error parsing and correction suggestions.
 */

import { BenchmarkValidator, ValidationConfig } from './BenchmarkValidator';
import { BenchmarkProblem, HumanEvalProblem, MBPPProblem } from './BenchmarkDatasetManager';
import { GolemSolution } from './GolemBenchmarkSolver';
import {
  StructuredValidationError,
  ErrorParser,
  ValidationResult as StructuredValidationResult,
  TestResult as StructuredTestResult,
  CodeMetrics,
  ErrorType,
  ErrorSeverity,
} from './StructuredValidationError';
import { promises as fs } from 'fs';
import * as path from 'path';
import * as os from 'os';
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

/**
 * Enhanced validation configuration
 */
export interface StructuredValidationConfig extends ValidationConfig {
  enableStructuredErrors: boolean;
  enableErrorCorrection: boolean;
  maxCorrectionAttempts: number;
  generateCodeMetrics: boolean;
  enablePerformanceAnalysis: boolean;
}

/**
 * Structured Benchmark Validator
 * Provides machine-readable error analysis for AST-guided corrections
 */
export class StructuredBenchmarkValidator extends BenchmarkValidator {

  /**
   * Validate solution with structured error reporting
   */
  async validateWithStructuredErrors(
    problem: BenchmarkProblem,
    solution: GolemSolution,
    config?: Partial<StructuredValidationConfig>,
  ): Promise<StructuredValidationResult> {

    const defaultConfig: StructuredValidationConfig = {
      timeoutMs: 30000,
      memoryLimitMB: 512,
      maxRetries: 2,
      enableSandbox: true,
      validateSyntax: true,
      validateSemantics: true,
      runPerformanceTests: false,
      captureOutput: true,
      parallelValidation: false,
      strictMode: false,
      enableStructuredErrors: true,
      enableErrorCorrection: true,
      maxCorrectionAttempts: 3,
      generateCodeMetrics: true,
      enablePerformanceAnalysis: false,
    };

    const finalConfig = { ...defaultConfig, ...config };

    // Create temporary directory for validation
    const tempDir = path.join(os.tmpdir(), `validation_${problem.id}_${Date.now()}`);
    await fs.mkdir(tempDir, { recursive: true });

    try {
      const result = await this.performStructuredValidation(
        problem,
        solution,
        finalConfig,
        tempDir,
      );

      return result;

    } finally {
      // Cleanup
      try {
        await fs.rm(tempDir, { recursive: true, force: true });
      } catch (error) {
        console.warn(`Failed to cleanup temp directory ${tempDir}:`, error);
      }
    }
  }

  /**
   * Perform validation with structured error analysis
   */
  private async performStructuredValidation(
    problem: BenchmarkProblem,
    solution: GolemSolution,
    config: StructuredValidationConfig,
    tempDir: string,
  ): Promise<StructuredValidationResult> {

    const startTime = Date.now();

    // Write solution to file
    const solutionFile = path.join(tempDir, 'solution.py');
    await fs.writeFile(solutionFile, solution.solutionCode);

    // Create comprehensive test script
    const testScript = this.createTestScript(problem, solution);
    const testFile = path.join(tempDir, 'test.py');
    await fs.writeFile(testFile, testScript);

    // Execute validation
    const executionResult = await this.executeValidation(tempDir, config);

    // Parse structured errors
    const errors = ErrorParser.parsePythonError(
      executionResult.stderr,
      executionResult.stdout,
      solution.solutionCode,
      executionResult.exitCode,
    );

    // Generate code metrics if enabled
    const codeMetrics = config.generateCodeMetrics
      ? this.generateCodeMetrics(solution.solutionCode)
      : undefined;

    // Parse test results
    const testResults = this.parseTestResults(
      executionResult.stdout,
      executionResult.stderr,
      problem,
    );

    const executionTime = Date.now() - startTime;
    const success = executionResult.exitCode === 0 && errors.length === 0;

    return {
      success,
      errors,
      warnings: errors.filter(e => e.severity === ErrorSeverity.LOW),
      executionTime,
      testResults,
      codeMetrics,
    };
  }

  /**
   * Create comprehensive test script for validation
   */
  private createTestScript(problem: BenchmarkProblem, solution: GolemSolution): string {

    if (problem.benchmark === 'humaneval') {
      return this.createHumanEvalTestScript(problem as HumanEvalProblem, solution);
    } else if (problem.benchmark === 'mbpp') {
      return this.createMBPPTestScript(problem as MBPPProblem, solution);
    }

    // Generic test script
    return `
import sys
import traceback
import time
import json
from io import StringIO

def capture_execution():
    """Execute solution and capture detailed results"""
    results = {
        'syntax_valid': False,
        'execution_successful': False,
        'test_results': [],
        'errors': [],
        'execution_time': 0,
        'memory_usage': 0
    }
    
    start_time = time.time()
    
    try:
        # Test syntax
        with open('solution.py', 'r') as f:
            code = f.read()
        
        compile(code, 'solution.py', 'exec')
        results['syntax_valid'] = True
        print("SYNTAX_VALID")
        
        # Execute code
        exec(code, globals())
        results['execution_successful'] = True
        print("EXECUTION_SUCCESSFUL")
        
    except SyntaxError as e:
        error_info = {
            'type': 'SyntaxError',
            'message': str(e),
            'line': getattr(e, 'lineno', 0),
            'column': getattr(e, 'offset', 0)
        }
        results['errors'].append(error_info)
        print(f"SYNTAX_ERROR: {e}")
        
    except Exception as e:
        error_info = {
            'type': type(e).__name__,
            'message': str(e),
            'traceback': traceback.format_exc()
        }
        results['errors'].append(error_info)
        print(f"RUNTIME_ERROR: {e}")
    
    results['execution_time'] = time.time() - start_time
    
    # Output results as JSON for parsing
    print("RESULTS_JSON_START")
    print(json.dumps(results, indent=2))
    print("RESULTS_JSON_END")
    
    return results['syntax_valid'] and results['execution_successful']

if __name__ == "__main__":
    success = capture_execution()
    sys.exit(0 if success else 1)
    `;
  }

  /**
   * Create HumanEval-specific test script
   */
  private createHumanEvalTestScript(problem: HumanEvalProblem, solution: GolemSolution): string {
    return `
import sys
import traceback
import time
import json
from io import StringIO

def test_humaneval_solution():
    """Test HumanEval solution with actual test cases"""
    results = {
        'syntax_valid': False,
        'execution_successful': False,
        'test_results': [],
        'errors': [],
        'execution_time': 0
    }
    
    start_time = time.time()
    
    try:
        # Load and compile solution
        with open('solution.py', 'r') as f:
            solution_code = f.read()
        
        compile(solution_code, 'solution.py', 'exec')
        results['syntax_valid'] = True
        print("SYNTAX_VALID")
        
        # Execute solution in isolated namespace
        solution_namespace = {}
        exec(solution_code, solution_namespace)
        
        # Check if entry point exists
        entry_point = "${problem.entryPoint}"
        if entry_point not in solution_namespace:
            raise NameError(f"Function '{entry_point}' not found in solution")
        
        func = solution_namespace[entry_point]
        results['execution_successful'] = True
        print("EXECUTION_SUCCESSFUL")
        
        # Run test cases (embedded in problem)
        test_code = """${problem.testCode.replace(/"/g, '\\"').replace(/\n/g, '\\n')}"""
        
        # Execute tests
        test_namespace = solution_namespace.copy()
        exec(test_code, test_namespace)
        
        print("ALL_TESTS_PASSED")
        
    except SyntaxError as e:
        error_info = {
            'type': 'SyntaxError',
            'message': str(e),
            'line': getattr(e, 'lineno', 0),
            'column': getattr(e, 'offset', 0),
            'text': getattr(e, 'text', '')
        }
        results['errors'].append(error_info)
        print(f"SYNTAX_ERROR: Line {e.lineno}: {e.msg}")
        
    except NameError as e:
        error_info = {
            'type': 'NameError',
            'message': str(e),
            'function_name': entry_point
        }
        results['errors'].append(error_info)
        print(f"NAME_ERROR: {e}")
        
    except Exception as e:
        error_info = {
            'type': type(e).__name__,
            'message': str(e),
            'traceback': traceback.format_exc()
        }
        results['errors'].append(error_info)
        print(f"RUNTIME_ERROR: {e}")
    
    results['execution_time'] = time.time() - start_time
    
    # Output structured results
    print("RESULTS_JSON_START")
    print(json.dumps(results, indent=2))
    print("RESULTS_JSON_END")
    
    return len(results['errors']) == 0

if __name__ == "__main__":
    success = test_humaneval_solution()
    sys.exit(0 if success else 1)
    `;
  }

  /**
   * Create MBPP-specific test script
   */
  private createMBPPTestScript(problem: MBPPProblem, solution: GolemSolution): string {
    const testCases = problem.testList.map(test => test.replace(/"/g, '\\"')).join('\\n');

    return `
import sys
import traceback
import time
import json

def test_mbpp_solution():
    """Test MBPP solution with provided test cases"""
    results = {
        'syntax_valid': False,
        'execution_successful': False,
        'test_results': [],
        'errors': [],
        'execution_time': 0
    }
    
    start_time = time.time()
    
    try:
        # Load solution
        with open('solution.py', 'r') as f:
            solution_code = f.read()
        
        compile(solution_code, 'solution.py', 'exec')
        results['syntax_valid'] = True
        print("SYNTAX_VALID")
        
        # Execute solution
        solution_namespace = {}
        exec(solution_code, solution_namespace)
        results['execution_successful'] = True
        print("EXECUTION_SUCCESSFUL")
        
        # Run test cases
        test_cases = """${testCases}"""
        
        for i, test_case in enumerate(test_cases.split('\\n')):
            if test_case.strip():
                try:
                    exec(test_case, solution_namespace)
                    results['test_results'].append({
                        'test_id': f'test_{i}',
                        'passed': True,
                        'test_case': test_case
                    })
                    print(f"TEST_PASSED: {i}")
                except Exception as e:
                    results['test_results'].append({
                        'test_id': f'test_{i}',
                        'passed': False,
                        'test_case': test_case,
                        'error': str(e)
                    })
                    print(f"TEST_FAILED: {i}: {e}")
        
        all_passed = all(t['passed'] for t in results['test_results'])
        if all_passed:
            print("ALL_TESTS_PASSED")
        
    except SyntaxError as e:
        error_info = {
            'type': 'SyntaxError',
            'message': str(e),
            'line': getattr(e, 'lineno', 0),
            'column': getattr(e, 'offset', 0)
        }
        results['errors'].append(error_info)
        print(f"SYNTAX_ERROR: {e}")
        
    except Exception as e:
        error_info = {
            'type': type(e).__name__,
            'message': str(e),
            'traceback': traceback.format_exc()
        }
        results['errors'].append(error_info)
        print(f"RUNTIME_ERROR: {e}")
    
    results['execution_time'] = time.time() - start_time
    
    print("RESULTS_JSON_START")
    print(json.dumps(results, indent=2))
    print("RESULTS_JSON_END")
    
    return len(results['errors']) == 0

if __name__ == "__main__":
    success = test_mbpp_solution()
    sys.exit(0 if success else 1)
    `;
  }

  /**
   * Execute validation script and capture results
   */
  private async executeValidation(
    tempDir: string,
    config: StructuredValidationConfig,
  ): Promise<{ stdout: string; stderr: string; exitCode: number }> {

    const timeoutSeconds = Math.floor(config.timeoutMs / 1000);
    const isWindows = os.platform() === 'win32';

    let command: string;
    if (isWindows) {
      command = `cd /d "${tempDir}" && python test.py`;
    } else {
      command = `cd "${tempDir}" && timeout ${timeoutSeconds} python3 test.py`;
    }

    try {
      const { stdout, stderr } = await execAsync(command, {
        timeout: config.timeoutMs,
        encoding: 'utf8',
      });

      return { stdout, stderr, exitCode: 0 };

    } catch (error: any) {
      return {
        stdout: error.stdout || '',
        stderr: error.stderr || error.message || '',
        exitCode: error.code || 1,
      };
    }
  }

  /**
   * Parse test results from execution output
   */
  private parseTestResults(
    stdout: string,
    stderr: string,
    problem: BenchmarkProblem,
  ): StructuredTestResult[] {

    const results: StructuredTestResult[] = [];

    // Look for JSON results in stdout
    const jsonMatch = stdout.match(/RESULTS_JSON_START\n([\s\S]*?)\nRESULTS_JSON_END/);
    if (jsonMatch) {
      try {
        const parsedResults = JSON.parse(jsonMatch[1]);

        if (parsedResults.test_results) {
          for (const testResult of parsedResults.test_results) {
            results.push({
              testName: testResult.test_id || 'unknown',
              passed: testResult.passed || false,
              executionTime: parsedResults.execution_time || 0,
              expectedOutput: undefined,
              actualOutput: testResult.error || undefined,
              error: testResult.error ? {
                id: `test_error_${Date.now()}`,
                type: ErrorType.ASSERTION_ERROR,
                severity: ErrorSeverity.HIGH,
                message: testResult.error,
                originalMessage: testResult.error,
                location: { line: 1, column: 1 },
                context: {
                  sourceCode: '',
                  errorLine: testResult.test_case || '',
                  surroundingLines: [],
                },
                suggestedFixes: [],
                timestamp: new Date(),
              } : undefined,
            });
          }
        }
      } catch (e) {
        // Fallback to simple parsing
      }
    }

    // Fallback: parse simple test indicators
    if (results.length === 0) {
      const syntaxValid = stdout.includes('SYNTAX_VALID');
      const executionSuccessful = stdout.includes('EXECUTION_SUCCESSFUL');
      const allTestsPassed = stdout.includes('ALL_TESTS_PASSED');

      results.push({
        testName: 'syntax_validation',
        passed: syntaxValid,
        executionTime: 0,
      });

      results.push({
        testName: 'execution_test',
        passed: executionSuccessful,
        executionTime: 0,
      });

      if (problem.benchmark === 'humaneval' || problem.benchmark === 'mbpp') {
        results.push({
          testName: 'functional_tests',
          passed: allTestsPassed,
          executionTime: 0,
        });
      }
    }

    return results;
  }

  /**
   * Generate code metrics for solution analysis
   */
  private generateCodeMetrics(sourceCode: string): CodeMetrics {
    const lines = sourceCode.split('\n');
    const nonEmptyLines = lines.filter(line => line.trim().length > 0);

    // Simple metrics calculation
    const linesOfCode = nonEmptyLines.length;
    const cyclomaticComplexity = this.calculateCyclomaticComplexity(sourceCode);
    const maintainabilityIndex = this.calculateMaintainabilityIndex(sourceCode);

    return {
      linesOfCode,
      cyclomaticComplexity,
      maintainabilityIndex,
      technicalDebt: Math.max(0, cyclomaticComplexity - 10) * 0.1,
      performanceScore: Math.max(0, 100 - linesOfCode * 0.5),
    };
  }

  /**
   * Calculate cyclomatic complexity (simplified)
   */
  private calculateCyclomaticComplexity(code: string): number {
    // Count decision points: if, elif, while, for, try, except, and, or
    const decisionKeywords = ['if ', 'elif ', 'while ', 'for ', 'try:', 'except', ' and ', ' or '];
    let complexity = 1; // Base complexity

    for (const keyword of decisionKeywords) {
      const matches = code.match(new RegExp(keyword, 'g'));
      if (matches) {
        complexity += matches.length;
      }
    }

    return complexity;
  }

  /**
   * Calculate maintainability index (simplified)
   */
  private calculateMaintainabilityIndex(code: string): number {
    const linesOfCode = code.split('\n').filter(line => line.trim().length > 0).length;
    const cyclomaticComplexity = this.calculateCyclomaticComplexity(code);

    // Simplified maintainability index formula
    const maintainabilityIndex = Math.max(0,
      171 - 5.2 * Math.log(linesOfCode) - 0.23 * cyclomaticComplexity - 16.2 * Math.log(linesOfCode),
    );

    return Math.min(100, maintainabilityIndex);
  }
}

