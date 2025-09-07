/**
 * Enhanced Prompt System for Golem Evaluation
 *
 * This module provides intelligent prompting capabilities that address common
 * code generation issues, particularly import-related failures.
 */

export interface PromptEnhancementConfig {
  enableImportInjection: boolean;
  enableErrorFeedback: boolean;
  enableContextualHints: boolean;
  maxRetryAttempts: number;
}

export interface ErrorContext {
  errorType: 'import' | 'syntax' | 'runtime' | 'algorithmic';
  errorMessage: string;
  failedCode: string;
  attemptNumber: number;
}

export interface ProblemContext {
  problemId: string;
  benchmark: string;
  description: string;
  functionSignature?: string;
  testCases?: string[];
  expectedOutput?: string;
}

export class PromptSystem {
  private config: PromptEnhancementConfig;

  // Common Python imports that are frequently needed
  private readonly COMMON_IMPORTS = {
    'List': 'from typing import List',
    'Dict': 'from typing import Dict',
    'Optional': 'from typing import Optional',
    'Union': 'from typing import Union',
    'Tuple': 'from typing import Tuple',
    'Set': 'from typing import Set',
    'Any': 'from typing import Any',
    'Callable': 'from typing import Callable',
    'Iterator': 'from typing import Iterator',
    'Iterable': 'from typing import Iterable',
  };

  constructor(config: PromptEnhancementConfig) {
    this.config = config;
  }

  /**
   * Generate initial prompt for problem solving
   */
  generateInitialPrompt(context: ProblemContext): string {
    let prompt = this.buildBasePrompt(context);

    if (this.config.enableImportInjection) {
      prompt += this.addImportGuidance();
    }

    if (this.config.enableContextualHints) {
      prompt += this.addContextualHints(context);
    }

    return prompt;
  }

  /**
   * Generate corrective prompt based on error feedback
   */
  generateCorrectivePrompt(
    context: ProblemContext,
    errorContext: ErrorContext,
  ): string {
    let prompt = this.buildBasePrompt(context);

    // Add error-specific guidance
    switch (errorContext.errorType) {
      case 'import':
        prompt += this.buildImportErrorPrompt(errorContext);
        break;
      case 'syntax':
        prompt += this.buildSyntaxErrorPrompt(errorContext);
        break;
      case 'runtime':
        prompt += this.buildRuntimeErrorPrompt(errorContext);
        break;
      case 'algorithmic':
        prompt += this.buildAlgorithmicErrorPrompt(errorContext);
        break;
    }

    return prompt;
  }

  /**
   * Detect missing imports in code
   */
  detectMissingImports(code: string): string[] {
    const missingImports: string[] = [];

    // Check for typing imports
    for (const [typeName, importStatement] of Object.entries(this.COMMON_IMPORTS)) {
      if (this.codeUsesType(code, typeName) && !this.codeHasImport(code, typeName)) {
        missingImports.push(importStatement);
      }
    }

    return missingImports;
  }

  /**
   * Automatically inject missing imports into code
   */
  injectMissingImports(code: string): string {
    const missingImports = this.detectMissingImports(code);

    if (missingImports.length === 0) {
      return code;
    }

    // Add imports at the beginning of the code
    const importsSection = missingImports.join('\n') + '\n\n';
    return importsSection + code;
  }

  private buildBasePrompt(context: ProblemContext): string {
    return `
// eslint-disable-next-line max-len
        You are an expert Python programmer solving coding challenges. Your task is to implement a solution for the following problem:

**Problem ID**: ${context.problemId}
**Benchmark**: ${context.benchmark}

**Problem Description**:
${context.description}

${context.functionSignature ? `**Function Signature**:
\`\`\`python
${context.functionSignature}
\`\`\`` : ''}

**Requirements**:
1. Provide a complete, working Python solution
2. Include all necessary imports at the top of your code
3. Follow Python best practices and PEP 8 style guidelines
4. Ensure your solution handles edge cases appropriately
5. Write efficient, readable code with clear logic

`;
  }

  private addImportGuidance(): string {
    return `
**Import Guidelines**:
- Always include necessary imports at the top of your code
- For type hints, use: \`from typing import List, Dict, Optional, Union, Tuple, Set, Any\`
- For mathematical operations, consider: \`import math, statistics, itertools\`
- For data structures, consider: \`from collections import defaultdict, Counter, deque\`
- For regular expressions: \`import re\`

`;
  }

  private addContextualHints(context: ProblemContext): string {
    let hints = '\n**Contextual Hints**:\n';

    // Add benchmark-specific hints
    switch (context.benchmark) {
      case 'humaneval':
        hints += '- This is a HumanEval problem focusing on algorithmic thinking\n';
        hints += '- Pay attention to function signatures and type hints\n';
        break;
      case 'mbpp':
        hints += '- This is an MBPP problem testing basic programming skills\n';
        hints += '- Focus on correctness and handling of edge cases\n';
        break;
      case 'swe-bench':
        hints += '- This is a software engineering problem from real repositories\n';
        hints += '- Consider existing code patterns and maintain consistency\n';
        break;
    }

    return hints + '\n';
  }

  private buildImportErrorPrompt(errorContext: ErrorContext): string {
    const missingImports = this.detectMissingImports(errorContext.failedCode);

    return `
**IMPORT ERROR DETECTED**:
The previous solution failed with: "${errorContext.errorMessage}"

**Issue**: Missing import statements
**Missing Imports**: ${missingImports.join(', ')}

**Corrective Action**:
Please provide a corrected solution that includes all necessary imports at the top of the code.

**Previous Failed Code**:
\`\`\`python
${errorContext.failedCode}
    // eslint-disable-next-line max-len
\`\`\`

**Your Task**: Fix the import issues and provide a complete, working solution.

`;
  }

  private buildSyntaxErrorPrompt(errorContext: ErrorContext): string {
    return `
**SYNTAX ERROR DETECTED**:
The previous solution failed with: "${errorContext.errorMessage}"

**Issue**: Python syntax error
**Attempt**: ${errorContext.attemptNumber}/${this.config.maxRetryAttempts}

**Previous Failed Code**:
\`\`\`python
${errorContext.failedCode}
\`\`\`

**Your Task**: Fix the syntax error and provide a corrected solution.

`;
  }

  private buildRuntimeErrorPrompt(errorContext: ErrorContext): string {
    return `
**RUNTIME ERROR DETECTED**:
The previous solution failed during execution with: "${errorContext.errorMessage}"

**Issue**: Runtime error during test execution
**Attempt**: ${errorContext.attemptNumber}/${this.config.maxRetryAttempts}

**Previous Failed Code**:
\`\`\`python
${errorContext.failedCode}
\`\`\`

**Your Task**: Analyze the runtime error and provide a corrected solution that handles the problematic case.

`;
  }

  private buildAlgorithmicErrorPrompt(errorContext: ErrorContext): string {
    return `
**ALGORITHMIC ERROR DETECTED**:
The previous solution executed but failed test cases.

**Issue**: Incorrect algorithm or logic
**Attempt**: ${errorContext.attemptNumber}/${this.config.maxRetryAttempts}

**Previous Failed Code**:
\`\`\`python
${errorContext.failedCode}
\`\`\`

**Your Task**: 
1. Analyze why the algorithm failed
2. Consider edge cases that might not be handled
3. Provide a corrected solution with improved logic

`;
  }

  private codeUsesType(code: string, typeName: string): boolean {
    // Check for type annotations using the type
    const typePattern = new RegExp(`\\b${typeName}\\s*\\[`, 'g');
    return typePattern.test(code);
  }

  private codeHasImport(code: string, typeName: string): boolean {
    // Check if the import statement is already present
    const importPattern = new RegExp(`from\\s+typing\\s+import\\s+.*\\b${typeName}\\b`, 'g');
    return importPattern.test(code);
  }

  /**
   * Analyze error type from error message
   */
  static analyzeErrorType(errorMessage: string): 'import' | 'syntax' | 'runtime' | 'algorithmic' {
    if (errorMessage.includes('is not defined') || errorMessage.includes('ImportError') || errorMessage.includes('ModuleNotFoundError')) {
      return 'import';
    }

    if (errorMessage.includes('SyntaxError') || errorMessage.includes('IndentationError')) {
      return 'syntax';
    }

    if (errorMessage.includes('TypeError') || errorMessage.includes('ValueError') || errorMessage.includes('AttributeError')) {
      return 'runtime';
    }

    return 'algorithmic';
  }
}

export default PromptSystem;

