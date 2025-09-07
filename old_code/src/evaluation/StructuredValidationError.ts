/**
 * Structured Validation Error System
 *
 * Provides machine-readable error data for AST-guided code correction.
 * Parses Python execution errors into structured format for intelligent fixes.
 */

export enum ErrorType {
  SYNTAX_ERROR = 'SyntaxError',
  NAME_ERROR = 'NameError',
  TYPE_ERROR = 'TypeError',
  ATTRIBUTE_ERROR = 'AttributeError',
  INDEX_ERROR = 'IndexError',
  KEY_ERROR = 'KeyError',
  VALUE_ERROR = 'ValueError',
  IMPORT_ERROR = 'ImportError',
  MODULE_NOT_FOUND_ERROR = 'ModuleNotFoundError',
  INDENTATION_ERROR = 'IndentationError',
  RUNTIME_ERROR = 'RuntimeError',
  ASSERTION_ERROR = 'AssertionError',
  TIMEOUT_ERROR = 'TimeoutError',
  UNKNOWN_ERROR = 'UnknownError',
  SEMANTIC_ERROR = 'SemanticError'
}

export enum ErrorSeverity {
  CRITICAL = 'critical',    // Prevents execution entirely
  HIGH = 'high',           // Causes incorrect results
  MEDIUM = 'medium',       // Performance or style issues
  LOW = 'low'              // Minor warnings
}

export interface ErrorLocation {
  line: number;
  column: number;
  offset?: number;
  length?: number;
}

export interface ErrorContext {
  sourceCode: string;
  errorLine: string;
  surroundingLines: string[];
  functionName?: string;
  className?: string;
  variablesInScope?: string[];
}

export interface StructuredValidationError {
  id: string;
  type: ErrorType;
  severity: ErrorSeverity;
  message: string;
  originalMessage: string;
  location: ErrorLocation;
  context: ErrorContext;
  suggestedFixes: ErrorFix[];
  astNodePath?: string[];  // Path to AST node causing error
  relatedErrors?: string[]; // IDs of related errors
  nodeType?: string; // AST node type for transformation mapping
  timestamp: Date;
}

export interface ErrorFix {
  id: string;
  description: string;
  confidence: number;
  fixType: FixType;
  astTransformation?: ASTTransformation;
  codeReplacement?: CodeReplacement;
  estimatedImpact: number; // 0-1 scale, lower is better
}

export enum FixType {
  AST_TRANSFORMATION = 'ast_transformation',
  CODE_REPLACEMENT = 'code_replacement',
  IMPORT_ADDITION = 'import_addition',
  VARIABLE_DECLARATION = 'variable_declaration',
  FUNCTION_SIGNATURE_FIX = 'function_signature_fix',
  INDENTATION_FIX = 'indentation_fix',
  SYNTAX_CORRECTION = 'syntax_correction'
}

export interface ASTTransformation {
  nodeType: string;
  operation: 'insert' | 'delete' | 'modify' | 'replace';
  targetPath: string[];
  newValue?: any;
  newNode?: any;
}

export interface CodeReplacement {
  startLine: number;
  endLine: number;
  startColumn: number;
  endColumn: number;
  newCode: string;
  preserveIndentation: boolean;
}

export interface ValidationResult {
  success: boolean;
  errors: StructuredValidationError[];
  warnings: StructuredValidationError[];
  executionTime: number;
  memoryUsage?: number;
  testResults?: TestResult[];
  codeMetrics?: CodeMetrics;
}

export interface TestResult {
  testName: string;
  passed: boolean;
  error?: StructuredValidationError;
  executionTime: number;
  expectedOutput?: any;
  actualOutput?: any;
}

export interface CodeMetrics {
  linesOfCode: number;
  cyclomaticComplexity: number;
  maintainabilityIndex: number;
  technicalDebt: number;
  performanceScore: number;
}

/**
 * Error Parser - Converts raw Python error output to structured format
 */
export class ErrorParser {

  /**
   * Parse Python traceback into structured error
   */
  static parsePythonError(
    stderr: string,
    stdout: string,
    sourceCode: string,
    exitCode: number,
  ): StructuredValidationError[] {
    const errors: StructuredValidationError[] = [];

    if (exitCode === 0 && !stderr.trim()) {
      return errors; // No errors
    }

    // Handle timeout errors
    if (stderr.includes('timeout') || stderr.includes('TimeoutExpired')) {
      errors.push(this.createTimeoutError(stderr, sourceCode));
      return errors;
    }

    // Parse Python traceback format
    const tracebackPattern = /Traceback \(most recent call last\):([\s\S]*?)(\w+Error: .+)/g;
    let match;

    while ((match = tracebackPattern.exec(stderr)) !== null) {
      const traceback = match[1];
      const errorLine = match[2];

      const error = this.parseErrorLine(errorLine, traceback, sourceCode);
      if (error) {
        errors.push(error);
      }
    }

    // Handle single-line errors (syntax errors without traceback)
    if (errors.length === 0 && stderr.trim()) {
      const singleLineError = this.parseSingleLineError(stderr, sourceCode);
      if (singleLineError) {
        errors.push(singleLineError);
      }
    }

    return errors;
  }

  private static parseErrorLine(
    errorLine: string,
    traceback: string,
    sourceCode: string,
  ): StructuredValidationError | null {

    // Extract error type and message
    const errorMatch = errorLine.match(/^(\w+Error): (.+)$/);
    if (!errorMatch) {
return null;
}

    const errorTypeName = errorMatch[1];
    const message = errorMatch[2];

    // Extract line number from traceback
    const lineMatch = traceback.match(/line (\d+)/);
    const line = lineMatch ? parseInt(lineMatch[1]) : 1;

    // Map Python error types to our enum
    const errorType = this.mapPythonErrorType(errorTypeName);
    const severity = this.determineSeverity(errorType);

    // Extract context
    const context = this.extractErrorContext(sourceCode, line);

    return {
      id: `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type: errorType,
      severity,
      message: this.cleanErrorMessage(message),
      originalMessage: errorLine,
      location: {
        line,
        column: 1, // Python doesn't always provide column info
      },
      context,
      suggestedFixes: this.generateSuggestedFixes(errorType, message, context),
      timestamp: new Date(),
    };
  }

  private static parseSingleLineError(
    stderr: string,
    sourceCode: string,
  ): StructuredValidationError | null {

    // Handle syntax errors
    const syntaxMatch = stderr.match(/SyntaxError: (.+)/);
    if (syntaxMatch) {
      return {
        id: `syntax_error_${Date.now()}`,
        type: ErrorType.SYNTAX_ERROR,
        severity: ErrorSeverity.CRITICAL,
        message: this.cleanErrorMessage(syntaxMatch[1]),
        originalMessage: stderr.trim(),
        location: { line: 1, column: 1 },
        context: this.extractErrorContext(sourceCode, 1),
        suggestedFixes: [],
        timestamp: new Date(),
      };
    }

    return null;
  }

  private static createTimeoutError(
    stderr: string,
    sourceCode: string,
  ): StructuredValidationError {
    return {
      id: `timeout_error_${Date.now()}`,
      type: ErrorType.TIMEOUT_ERROR,
      severity: ErrorSeverity.HIGH,
      message: 'Code execution timed out',
      originalMessage: stderr,
      location: { line: 1, column: 1 },
      context: this.extractErrorContext(sourceCode, 1),
      suggestedFixes: [{
        id: 'optimize_performance',
        description: 'Optimize algorithm for better performance',
        confidence: 0.6,
        fixType: FixType.AST_TRANSFORMATION,
        estimatedImpact: 0.7,
      }],
      timestamp: new Date(),
    };
  }

  private static mapPythonErrorType(pythonError: string): ErrorType {
    const mapping: Record<string, ErrorType> = {
      'SyntaxError': ErrorType.SYNTAX_ERROR,
      'NameError': ErrorType.NAME_ERROR,
      'TypeError': ErrorType.TYPE_ERROR,
      'AttributeError': ErrorType.ATTRIBUTE_ERROR,
      'IndexError': ErrorType.INDEX_ERROR,
      'KeyError': ErrorType.KEY_ERROR,
      'ValueError': ErrorType.VALUE_ERROR,
      'ImportError': ErrorType.IMPORT_ERROR,
      'ModuleNotFoundError': ErrorType.MODULE_NOT_FOUND_ERROR,
      'IndentationError': ErrorType.INDENTATION_ERROR,
      'RuntimeError': ErrorType.RUNTIME_ERROR,
      'AssertionError': ErrorType.ASSERTION_ERROR,
    };

    return mapping[pythonError] || ErrorType.UNKNOWN_ERROR;
  }

  private static determineSeverity(errorType: ErrorType): ErrorSeverity {
    const criticalErrors = [
      ErrorType.SYNTAX_ERROR,
      ErrorType.INDENTATION_ERROR,
      ErrorType.IMPORT_ERROR,
      ErrorType.MODULE_NOT_FOUND_ERROR,
    ];

    const highErrors = [
      ErrorType.NAME_ERROR,
      ErrorType.TYPE_ERROR,
      ErrorType.ATTRIBUTE_ERROR,
      ErrorType.TIMEOUT_ERROR,
    ];

    if (criticalErrors.includes(errorType)) {
return ErrorSeverity.CRITICAL;
}
    if (highErrors.includes(errorType)) {
return ErrorSeverity.HIGH;
}
    return ErrorSeverity.MEDIUM;
  }

  private static extractErrorContext(
    sourceCode: string,
    errorLine: number,
  ): ErrorContext {
    const lines = sourceCode.split('\n');
    const errorLineIndex = errorLine - 1;

    const surroundingLines = [];
    for (let i = Math.max(0, errorLineIndex - 2); i <= Math.min(lines.length - 1, errorLineIndex + 2); i++) {
      surroundingLines.push(`${i + 1}: ${lines[i] || ''}`);
    }

    return {
      sourceCode,
      errorLine: lines[errorLineIndex] || '',
      surroundingLines,
      functionName: this.extractFunctionName(lines, errorLineIndex),
      variablesInScope: this.extractVariablesInScope(lines, errorLineIndex),
    };
  }

  private static extractFunctionName(lines: string[], errorLineIndex: number): string | undefined {
    // Look backwards for function definition
    for (let i = errorLineIndex; i >= 0; i--) {
      const match = lines[i]?.match(/^\s*def\s+(\w+)/);
      if (match) {
return match[1];
}
    }
    return undefined;
  }

  private static extractVariablesInScope(lines: string[], errorLineIndex: number): string[] {
    const variables: Set<string> = new Set();

    // Look for variable assignments before error line
    for (let i = 0; i < errorLineIndex; i++) {
      const line = lines[i];
      if (!line) {
continue;
}

      // Simple variable assignment pattern
      const assignMatch = line.match(/^\s*(\w+)\s*=/);
      if (assignMatch) {
        variables.add(assignMatch[1]);
      }

      // Function parameters
      const funcMatch = line.match(/def\s+\w+\s*\(([^)]*)\)/);
      if (funcMatch) {
        const params = funcMatch[1].split(',').map(p => p.trim().split('=')[0].trim());
        params.forEach(p => p && variables.add(p));
      }
    }

    return Array.from(variables);
  }

  private static cleanErrorMessage(message: string): string {
    // Remove file paths and line references for cleaner messages
    return message
      .replace(/File ".*?", line \d+, in .+\n/g, '')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private static generateSuggestedFixes(
    errorType: ErrorType,
    message: string,
    context: ErrorContext,
  ): ErrorFix[] {
    const fixes: ErrorFix[] = [];

    switch (errorType) {
      case ErrorType.NAME_ERROR:
        if (message.includes('is not defined')) {
          const varName = message.match(/'(\w+)'/)?.[1];
          if (varName) {
            fixes.push({
              id: 'declare_variable',
              description: `Declare variable '${varName}'`,
              confidence: 0.8,
              fixType: FixType.VARIABLE_DECLARATION,
              estimatedImpact: 0.2,
            });
          }
        }
        break;

      case ErrorType.IMPORT_ERROR:
      case ErrorType.MODULE_NOT_FOUND_ERROR: {
        const moduleName = message.match(/No module named '(\w+)'/)?.[1];
        if (moduleName) {
          fixes.push({
            id: 'add_import',
            description: `Add import for '${moduleName}'`,
            confidence: 0.9,
            fixType: FixType.IMPORT_ADDITION,
            estimatedImpact: 0.1,
          });
        }
        break;
      }

      case ErrorType.INDENTATION_ERROR:
        fixes.push({
          id: 'fix_indentation',
          description: 'Fix indentation',
          confidence: 0.95,
          fixType: FixType.INDENTATION_FIX,
          estimatedImpact: 0.1,
        });
        break;

      case ErrorType.SYNTAX_ERROR:
        fixes.push({
          id: 'fix_syntax',
          description: 'Fix syntax error',
          confidence: 0.7,
          fixType: FixType.SYNTAX_CORRECTION,
          estimatedImpact: 0.3,
        });
        break;
    }

    return fixes;
  }
}

