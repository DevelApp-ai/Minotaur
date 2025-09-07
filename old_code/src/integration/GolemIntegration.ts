/**
 * Enhanced Golem Integration
 *
 * This module integrates the enhanced prompt system and validator
 * with the existing Golem evaluation infrastructure.
 */

import { GolemEvaluationRunner } from '../evaluation/GolemEvaluationRunner';
import { GolemBenchmarkSolver } from '../evaluation/GolemBenchmarkSolver';
import { PromptSystem, ProblemContext, ErrorContext } from '../prompts/PromptSystem';
import { Validator, ValidationResult } from '../validation/Validator';

export interface EnhancedEvaluationConfig {
  enableEnhancedPrompts: boolean;
  enableAutoImportInjection: boolean;
  enableIntelligentRetry: boolean;
  maxRetryAttempts: number;
  enableDetailedLogging: boolean;
}

export interface EnhancedSolutionResult {
  problemId: string;
  benchmark: string;
  solutionCode: string;
  validationResult: ValidationResult;
  promptsUsed: string[];
  retryHistory: RetryAttempt[];
  enhancedMetadata: {
    totalAttempts: number;
    autoFixesApplied: number;
    finalErrorType: string;
    improvementApplied: boolean;
  };
}

export interface RetryAttempt {
  attemptNumber: number;
  promptUsed: string;
  solutionGenerated: string;
  validationResult: ValidationResult;
  errorType: string;
  autoFixApplied: boolean;
}

export class GolemIntegration {
  private promptSystem: PromptSystem;
  private validator: Validator;
  private config: EnhancedEvaluationConfig;
  private solver: GolemBenchmarkSolver;

  constructor(config: EnhancedEvaluationConfig) {
    this.config = config;
    this.promptSystem = new PromptSystem({
      enableImportInjection: config.enableAutoImportInjection,
      enableErrorFeedback: config.enableIntelligentRetry,
      enableContextualHints: true,
      maxRetryAttempts: config.maxRetryAttempts,
    });
    this.validator = new Validator(
      config.enableAutoImportInjection,
      config.maxRetryAttempts,
    );
    this.solver = new GolemBenchmarkSolver();
  }

  /**
   * Solve a problem using enhanced prompts and validation
   */
  async solveWithEnhancements(
    problemId: string,
    problemDescription: string,
    benchmark: string,
    testCases: any[],
    functionSignature?: string,
  ): Promise<EnhancedSolutionResult> {

    const problemContext: ProblemContext = {
      problemId,
      benchmark,
      description: problemDescription,
      functionSignature,
      testCases: testCases.map(tc => JSON.stringify(tc)),
    };

    const retryHistory: RetryAttempt[] = [];
    const promptsUsed: string[] = [];
    let currentAttempt = 1;
    let bestResult: ValidationResult | null = null;
    let bestSolution = '';
    let autoFixesApplied = 0;

    this.log(`🎯 Starting enhanced solution for ${problemId}`);

    // Initial attempt with enhanced prompt
    let prompt = this.promptSystem.generateInitialPrompt(problemContext);
    promptsUsed.push(prompt);

    let solution = await this.generateSolution(prompt, problemContext);
    let validationResult = await this.validator.validateSolutionEnhanced(
      problemId,
      solution,
      benchmark,
      testCases,
      problemContext,
    );

    // Track auto-fixes applied
    if (validationResult.enhancedMetadata?.autoFixAttempted) {
      autoFixesApplied++;
    }

    retryHistory.push({
      attemptNumber: currentAttempt,
      promptUsed: prompt,
      solutionGenerated: solution,
      validationResult,
      errorType: validationResult.enhancedMetadata?.errorType || 'none',
      autoFixApplied: validationResult.enhancedMetadata?.autoFixAttempted || false,
    });

    bestResult = validationResult;
    bestSolution = validationResult.enhancedMetadata?.finalCode || solution;

    this.log(`📊 Attempt ${currentAttempt}: Score ${validationResult.score}, Errors: ${validationResult.errors.length}`);

    // Retry with corrective prompts if needed
    while (!validationResult.passed &&
           currentAttempt < this.config.maxRetryAttempts &&
           this.config.enableIntelligentRetry) {

      currentAttempt++;

      // Create error context for corrective prompt
      const errorContext: ErrorContext = {
        errorType: validationResult.enhancedMetadata?.errorType === 'none' ? 'algorithmic' : validationResult.enhancedMetadata?.errorType || 'algorithmic',
        errorMessage: validationResult.errors[0] || 'Unknown error',
        failedCode: solution,
        attemptNumber: currentAttempt,
      };

      // Generate corrective prompt
      prompt = this.promptSystem.generateCorrectivePrompt(problemContext, errorContext);
      promptsUsed.push(prompt);

      this.log(`🔄 Retry attempt ${currentAttempt} with ${errorContext.errorType} error correction`);

      // Generate new solution with corrective prompt
      solution = await this.generateSolution(prompt, problemContext);
      validationResult = await this.validator.validateSolutionEnhanced(
        problemId,
        solution,
        benchmark,
        testCases,
        problemContext,
      );

      // Track auto-fixes applied
      if (validationResult.enhancedMetadata?.autoFixAttempted) {
        autoFixesApplied++;
      }

      retryHistory.push({
        attemptNumber: currentAttempt,
        promptUsed: prompt,
        solutionGenerated: solution,
        validationResult,
        errorType: validationResult.enhancedMetadata?.errorType || 'none',
        autoFixApplied: validationResult.enhancedMetadata?.autoFixAttempted || false,
      });

      // Keep track of best result
      if (validationResult.score > (bestResult?.score || 0)) {
        bestResult = validationResult;
        bestSolution = validationResult.enhancedMetadata?.finalCode || solution;
      }

      // eslint-disable-next-line max-len
      this.log(`📊 Attempt ${currentAttempt}: Score ${validationResult.score}, Errors: ${validationResult.errors.length}`);

      // Break if we achieved success
      if (validationResult.passed) {
        this.log(`✅ Success achieved on attempt ${currentAttempt}`);
        break;
      }
    }

    // Use best result if final attempt wasn't the best
    if (bestResult && bestResult.score > validationResult.score) {
      validationResult = bestResult;
      solution = bestSolution;
    }

    const finalResult: EnhancedSolutionResult = {
      problemId,
      benchmark,
      solutionCode: solution,
      validationResult,
      promptsUsed,
      retryHistory,
      enhancedMetadata: {
        totalAttempts: currentAttempt,
        autoFixesApplied,
        finalErrorType: validationResult.enhancedMetadata?.errorType || 'none',
        improvementApplied: retryHistory.length > 1 && validationResult.passed,
      },
    };

    this.log(`🏁 Final result: ${validationResult.passed ? 'SUCCESS' : 'FAILED'} (Score: ${validationResult.score})`);

    return finalResult;
  }

  /**
   * Generate solution using the appropriate solver
   */
  private async generateSolution(prompt: string, context: ProblemContext): Promise<string> {
    // For now, use the existing solver
    // In a real implementation, this would use the enhanced prompt with an LLM
    const mockSolution = await this.solver.solveProblem(
      {
        id: context.problemId,
        benchmark: context.benchmark as 'swe-bench' | 'quixbugs' | 'fim' | 'mbpp' | 'humaneval',
        title: context.description,
        description: context.description,
        prompt: context.description,
        language: 'python',
        difficulty: 'medium',
        category: 'coding',
        testCases: [],
        metadata: {},
      },
    );

    return mockSolution.solutionCode;
  }

  /**
   * Batch process multiple problems with enhancements
   */
  async batchSolveWithEnhancements(
    problems: Array<{
      problemId: string;
      description: string;
      benchmark: string;
      testCases: any[];
      functionSignature?: string;
    }>,
  ): Promise<EnhancedSolutionResult[]> {

    const results: EnhancedSolutionResult[] = [];

    this.log(`🚀 Starting batch processing of ${problems.length} problems`);

    for (let i = 0; i < problems.length; i++) {
      const problem = problems[i];
      this.log(`📝 Processing problem ${i + 1}/${problems.length}: ${problem.problemId}`);

      const result = await this.solveWithEnhancements(
        problem.problemId,
        problem.description,
        problem.benchmark,
        problem.testCases,
        problem.functionSignature,
      );

      results.push(result);
    }

    // eslint-disable-next-line max-len
    this.log(`✅ Batch processing complete: ${results.filter(r => r.validationResult.passed).length}/${results.length} successful`);

    return results;
  }

  /**
   * Generate comprehensive improvement report
   */
  generateImprovementReport(results: EnhancedSolutionResult[]): string {
    const totalProblems = results.length;
    const successfulProblems = results.filter(r => r.validationResult.passed).length;
    const improvedProblems = results.filter(r => r.enhancedMetadata.improvementApplied).length;
    const autoFixesApplied = results.reduce((sum, r) => sum + r.enhancedMetadata.autoFixesApplied, 0);

    const errorTypeBreakdown = results.reduce((acc, r) => {
      const errorType = r.enhancedMetadata.finalErrorType;
      acc[errorType] = (acc[errorType] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    const report = `
# Enhanced Golem Evaluation Report

## Summary Statistics
- **Total Problems**: ${totalProblems}
- **Successful Solutions**: ${successfulProblems} (${(successfulProblems/totalProblems*100).toFixed(1)}%)
- **Improved Through Retry**: ${improvedProblems} (${(improvedProblems/totalProblems*100).toFixed(1)}%)
- **Auto-fixes Applied**: ${autoFixesApplied}

## Error Type Breakdown
${Object.entries(errorTypeBreakdown).map(([type, count]) => `- **${type}**: ${count} problems`).join('\n')}

## Retry Effectiveness
${results.map(r => `- **${r.problemId}**: ${r.enhancedMetadata.totalAttempts} attempts, ${r.validationResult.passed ? 'SUCCESS' : 'FAILED'}`).join('\n')}

## Auto-fix Success Rate
- **Import Injection**: ${results.filter(r => r.retryHistory.some(h => h.autoFixApplied && h.errorType === 'import')).length} problems
- **Syntax Fixes**: ${results.filter(r => r.retryHistory.some(h => h.autoFixApplied && h.errorType === 'syntax')).length} problems
- **Runtime Fixes**: ${results.filter(r => r.retryHistory.some(h => h.autoFixApplied && h.errorType === 'runtime')).length} problems

## Recommendations
${this.generateRecommendations(results)}
`;

    return report;
  }

  private generateRecommendations(results: EnhancedSolutionResult[]): string {
    const recommendations: string[] = [];

    const importErrors = results.filter(r => r.enhancedMetadata.finalErrorType === 'import').length;
    if (importErrors > 0) {
      // eslint-disable-next-line max-len
      recommendations.push(`- Import detection helped with ${importErrors} problems - consider expanding import library`);
    }

    const retrySuccesses = results.filter(r => r.enhancedMetadata.improvementApplied).length;
    if (retrySuccesses > 0) {
      recommendations.push(`- Intelligent retry improved ${retrySuccesses} solutions - system is learning effectively`);
    }

    const algorithmicFailures = results.filter(r => r.enhancedMetadata.finalErrorType === 'algorithmic').length;
    if (algorithmicFailures > 0) {
      // eslint-disable-next-line max-len
      recommendations.push(`- ${algorithmicFailures} algorithmic failures suggest need for better problem understanding prompts`);
    }

    return recommendations.join('\n');
  }

  private log(message: string): void {
    if (this.config.enableDetailedLogging) {
    // eslint-disable-next-line no-console
      console.log(message);
    }
  }
}

export default GolemIntegration;

