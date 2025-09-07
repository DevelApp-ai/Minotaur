/**
 * Enhanced Validator for Golem Evaluation System
 *
 * This validator includes automatic import detection and injection,
 * intelligent error analysis, and improved retry logic.
 */

import { PromptSystem, ErrorContext, ProblemContext } from '../prompts/PromptSystem';
import { BenchmarkValidator } from '../evaluation/BenchmarkValidator';

export interface ValidationResult {
  passed: boolean;
  score: number;
  executionTime: number;
  memoryUsage: number;
  testResults: TestResult[];
  errors: string[];
  warnings: string[];
  metadata: ValidationMetadata;
  enhancedMetadata?: EnhancedValidationMetadata;
}

export interface TestResult {
  testId: string;
  passed: boolean;
  executionTime: number;
  expectedOutput?: any;
  actualOutput?: any;
  errorMessage?: string;
}

export interface ValidationMetadata {
  validationTime: number;
  testEnvironment: string;
  validatorVersion: string;
  retryCount: number;
}

export interface EnhancedValidationMetadata {
  importInjectionApplied: boolean;
  injectedImports: string[];
  errorType: 'import' | 'syntax' | 'runtime' | 'algorithmic' | 'none';
  autoFixAttempted: boolean;
  originalCode: string;
  finalCode: string;
}

export class Validator extends BenchmarkValidator {
  private promptSystem: PromptSystem;
  private enableAutoFix: boolean;
  private maxAutoFixAttempts: number;

  constructor(enableAutoFix: boolean = true, maxAutoFixAttempts: number = 3) {
    super();
    this.enableAutoFix = enableAutoFix;
    this.maxAutoFixAttempts = maxAutoFixAttempts;
    this.promptSystem = new PromptSystem({
      enableImportInjection: true,
      enableErrorFeedback: true,
      enableContextualHints: true,
      maxRetryAttempts: maxAutoFixAttempts,
    });
  }

  /**
   * Enhanced validation with automatic import injection and error correction
   */
  async validateSolutionEnhanced(
    problemId: string,
    solutionCode: string,
    benchmark: string,
    testCases: any[],
    problemContext?: ProblemContext,
  ): Promise<ValidationResult> {
    const originalCode = solutionCode;
    let currentCode = solutionCode;
    let autoFixAttempted = false;
    let injectedImports: string[] = [];
    let errorType: 'import' | 'syntax' | 'runtime' | 'algorithmic' | 'none' = 'none';

    // Step 1: Detect and inject missing imports
    if (this.enableAutoFix) {
      const missingImports = this.promptSystem.detectMissingImports(currentCode);
      if (missingImports.length > 0) {
        currentCode = this.promptSystem.injectMissingImports(currentCode);
        injectedImports = missingImports;
        autoFixAttempted = true;
    // eslint-disable-next-line no-console
        console.log(`🔧 Auto-injected imports: ${missingImports.join(', ')}`);
      }
    }

    // Step 2: Perform initial validation
    const problem = {
      id: problemId,
      benchmark: benchmark as 'swe-bench' | 'quixbugs' | 'fim' | 'mbpp' | 'humaneval',
      title: problemContext?.description || problemId,
      description: problemContext?.description || '',
      prompt: problemContext?.description || '',
      language: 'python',
      difficulty: 'medium' as const,
      category: 'coding',
      testCases: testCases || [],
      metadata: {},
    };

    const solution = {
      problemId,
      benchmark: benchmark as 'swe-bench' | 'quixbugs' | 'fim' | 'mbpp' | 'humaneval',
      solutionCode: currentCode,
      language: 'python' as const,
      approach: 'hybrid' as const,
      confidence: 0.8,
      generationTime: 0,
      engineUsed: 'enhanced-validator',
      metadata: {
        attempts: 1,
        fallbackUsed: false,
        engineHealth: {},
        transformationSteps: [],
      },
    };

    let result = await this.validateSolution(problem, solution);

    // Step 3: If validation failed, attempt error-specific fixes
    if (!result.passed && this.enableAutoFix && result.errors.length > 0) {
      const primaryError = result.errors[0];
      errorType = PromptSystem.analyzeErrorType(primaryError);

    // eslint-disable-next-line no-console
      console.log(`🔍 Detected error type: ${errorType}`);

      // Attempt automatic fixes based on error type
      const fixedCode = await this.attemptAutoFix(
        currentCode,
        primaryError,
        errorType,
        problemContext,
      );

      if (fixedCode && fixedCode !== currentCode) {
    // eslint-disable-next-line no-console
        console.log('🔧 Attempting auto-fix...');
        currentCode = fixedCode;
        autoFixAttempted = true;

        // Re-validate with fixed code
        const updatedSolution = { ...solution, solutionCode: currentCode };
        result = await this.validateSolution(problem, updatedSolution);
      }
    }

    // Step 4: Add enhanced metadata
    const enhancedMetadata: EnhancedValidationMetadata = {
      importInjectionApplied: injectedImports.length > 0,
      injectedImports,
      errorType,
      autoFixAttempted,
      originalCode,
      finalCode: currentCode,
    };

    return {
      ...result,
      enhancedMetadata,
    };
  }

  /**
   * Attempt automatic fixes based on error type
   */
  private async attemptAutoFix(
    code: string,
    errorMessage: string,
    errorType: 'import' | 'syntax' | 'runtime' | 'algorithmic',
    problemContext?: ProblemContext,
  ): Promise<string | null> {

    switch (errorType) {
      case 'import':
        return this.fixImportError(code, errorMessage);

      case 'syntax':
        return this.fixSyntaxError(code, errorMessage);

      case 'runtime':
        return this.fixRuntimeError(code, errorMessage);

      case 'algorithmic':
        // Algorithmic errors require more sophisticated handling
        // For now, return null to indicate no automatic fix available
        return null;

      default:
        return null;
    }
  }

  /**
   * Fix import-related errors
   */
  private fixImportError(code: string, errorMessage: string): string {
    // Extract the undefined name from error message
    const match = errorMessage.match(/name '(\w+)' is not defined/);
    if (!match) {
       return code;
    }

    const undefinedName = match[1];

    // Check if it's a common typing import
    const commonImports = {
      'List': 'from typing import List',
      'Dict': 'from typing import Dict',
      'Optional': 'from typing import Optional',
      'Union': 'from typing import Union',
      'Tuple': 'from typing import Tuple',
      'Set': 'from typing import Set',
      'Any': 'from typing import Any',
    };

    if (commonImports[undefinedName as keyof typeof commonImports]) {
      const importStatement = commonImports[undefinedName as keyof typeof commonImports];
      return importStatement + '\n\n' + code;
    }

    return code;
  }

  /**
   * Fix syntax-related errors
   */
  private fixSyntaxError(code: string, errorMessage: string): string {
    // Basic syntax error fixes

    // Fix missing colons
    if (errorMessage.includes('invalid syntax') && errorMessage.includes(':')) {
      // This is a complex fix that would require AST parsing
      // For now, return original code
      return code;
    }

    // Fix indentation errors
    if (errorMessage.includes('IndentationError')) {
      // Attempt to fix basic indentation issues
      const lines = code.split('\n');
      const fixedLines = lines.map(line => {
        // Basic indentation fix - ensure proper 4-space indentation
        if (line.trim() && !line.startsWith(' ') && !line.startsWith('\t')) {
          // Check if this line should be indented (follows def, if, for, while, etc.)
          const prevLineIndex = lines.indexOf(line) - 1;
          if (prevLineIndex >= 0) {
            const prevLine = lines[prevLineIndex].trim();
            if (prevLine.endsWith(':')) {
              return '    ' + line;
            }
          }
        }
        return line;
      });

      return fixedLines.join('\n');
    }

    return code;
  }

  /**
   * Fix runtime-related errors
   */
  private fixRuntimeError(code: string, errorMessage: string): string {
    // Handle common runtime errors

    // Fix index out of range errors
    if (errorMessage.includes('list index out of range')) {
      // Add bounds checking - this is a complex transformation
      // For now, return original code
      return code;
    }

    // Fix key errors
    if (errorMessage.includes('KeyError')) {
      // Add key existence checking - this is a complex transformation
      // For now, return original code
      return code;
    }

    return code;
  }

  /**
   * Generate detailed error report for debugging
   */
  generateErrorReport(
    problemId: string,
    originalCode: string,
    finalCode: string,
    validationResult: ValidationResult,
  ): string {
    const report = `
# Enhanced Validation Error Report

**Problem ID**: ${problemId}
**Validation Status**: ${validationResult.passed ? 'PASSED' : 'FAILED'}
**Error Type**: ${validationResult.enhancedMetadata?.errorType || 'unknown'}

## Original Code
\`\`\`python
${originalCode}
\`\`\`

## Final Code (After Auto-fixes)
\`\`\`python
${finalCode}
\`\`\`

## Auto-fixes Applied
- **Import Injection**: ${validationResult.enhancedMetadata?.importInjectionApplied ? 'Yes' : 'No'}
- **Injected Imports**: ${validationResult.enhancedMetadata?.injectedImports.join(', ') || 'None'}
- **Auto-fix Attempted**: ${validationResult.enhancedMetadata?.autoFixAttempted ? 'Yes' : 'No'}

## Validation Results
- **Score**: ${validationResult.score}
- **Execution Time**: ${validationResult.executionTime}ms
// eslint-disable-next-line max-len
- **Test Results**: ${validationResult.testResults.length} tests, ${validationResult.testResults.filter(t => t.passed).length} passed

## Errors
${validationResult.errors.map(error => `- ${error}`).join('\n')}

## Warnings  
${validationResult.warnings.map(warning => `- ${warning}`).join('\n')}

## Recommendations
${this.generateRecommendations(validationResult)}
`;

    return report;
  }

  /**
   * Generate recommendations based on validation results
   */
  private generateRecommendations(result: ValidationResult): string {
    const recommendations: string[] = [];

    if (result.enhancedMetadata?.errorType === 'import') {
      recommendations.push('- Consider using explicit imports at the top of your code');
      recommendations.push('- For type hints, always import from typing module');
    }

    if (result.enhancedMetadata?.errorType === 'syntax') {
      recommendations.push('- Check for missing colons, parentheses, or brackets');
      recommendations.push('- Ensure proper indentation (4 spaces per level)');
    }

    if (result.enhancedMetadata?.errorType === 'runtime') {
      recommendations.push('- Add bounds checking for list/array access');
      recommendations.push('- Validate input parameters before processing');
    }

    if (result.enhancedMetadata?.errorType === 'algorithmic') {
      recommendations.push('- Review the algorithm logic and edge cases');
      recommendations.push('- Test with simple examples manually');
      recommendations.push('- Consider alternative approaches or optimizations');
    }

    if (recommendations.length === 0) {
      recommendations.push('- No specific recommendations available');
    }

    return recommendations.join('\n');
  }
}

export default Validator;

