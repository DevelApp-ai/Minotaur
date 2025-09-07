/**
 * Correction Feedback Loop
 *
 * Implements the iterative correction process that validates code,
 * identifies errors, applies AST-guided fixes, and re-validates
 * until the code works or maximum attempts are reached.
 */

import { BenchmarkProblem } from './BenchmarkDatasetManager';
import { GolemSolution } from './GolemBenchmarkSolver';
import { StructuredBenchmarkValidator, StructuredValidationConfig } from './StructuredBenchmarkValidator';
import { ASTErrorCorrector, CorrectionConfig, CorrectionResult } from './ASTErrorCorrector';
import {
  StructuredValidationError,
  ValidationResult as StructuredValidationResult,
  ErrorSeverity,
  ErrorType,
} from './StructuredValidationError';

export interface FeedbackLoopConfig {
  maxIterations: number;
  maxErrorsPerIteration: number;
  enableProgressiveCorrection: boolean;
  enableErrorPrioritization: boolean;
  enableLearningFromFailures: boolean;
  timeoutPerIteration: number;
  confidenceThreshold: number;
}

export interface FeedbackLoopResult {
  success: boolean;
  finalSolution: GolemSolution;
  iterations: IterationResult[];
  totalTime: number;
  totalCorrections: number;
  finalValidation: StructuredValidationResult;
  learningInsights: LearningInsight[];
}

export interface IterationResult {
  iterationNumber: number;
  inputSolution: GolemSolution;
  validationResult: StructuredValidationResult;
  correctionResult: CorrectionResult | null;
  outputSolution: GolemSolution;
  iterationTime: number;
  errorsFixed: number;
  errorsRemaining: number;
  progressMade: boolean;
}

export interface LearningInsight {
  errorPattern: string;
  correctionStrategy: string;
  successRate: number;
  averageTime: number;
  complexity: number;
}

export interface ErrorPriorityScore {
  error: StructuredValidationError;
  priority: number;
  fixability: number;
  impact: number;
}

/**
 * Correction Feedback Loop
 *
 * Orchestrates the iterative correction process with learning and optimization
 */
export class CorrectionFeedbackLoop {
  private validator: StructuredBenchmarkValidator;
  private corrector: ASTErrorCorrector;
  private learningHistory: Map<string, LearningInsight>;

  constructor(
    validator: StructuredBenchmarkValidator,
    corrector: ASTErrorCorrector,
  ) {
    this.validator = validator;
    this.corrector = corrector;
    this.learningHistory = new Map();
  }

  /**
   * Execute the correction feedback loop
   */
  async executeFeedbackLoop(
    problem: BenchmarkProblem,
    initialSolution: GolemSolution,
    config: FeedbackLoopConfig,
    validationConfig?: Partial<StructuredValidationConfig>,
    correctionConfig?: Partial<CorrectionConfig>,
  ): Promise<FeedbackLoopResult> {

    const startTime = Date.now();
    const iterations: IterationResult[] = [];
    const learningInsights: LearningInsight[] = [];

    let currentSolution = initialSolution;
    let totalCorrections = 0;

    console.log(`🔄 Starting correction feedback loop for problem: ${problem.id}`);

    // Main correction loop
    for (let iteration = 1; iteration <= config.maxIterations; iteration++) {
      const iterationStartTime = Date.now();

      console.log(`📍 Iteration ${iteration}/${config.maxIterations}`);

      // Step 1: Validate current solution
      const validationResult = await this.validator.validateWithStructuredErrors(
        problem,
        currentSolution,
        validationConfig,
      );

      // Step 2: Check if we're done
      if (validationResult.success) {
        console.log(`✅ Solution validated successfully in iteration ${iteration}`);

        const iterationResult: IterationResult = {
          iterationNumber: iteration,
          inputSolution: currentSolution,
          validationResult,
          correctionResult: null,
          outputSolution: currentSolution,
          iterationTime: Date.now() - iterationStartTime,
          errorsFixed: 0,
          errorsRemaining: 0,
          progressMade: true,
        };

        iterations.push(iterationResult);
        break;
      }

      // Step 3: Prioritize errors for correction
      const prioritizedErrors = this.prioritizeErrors(
        validationResult.errors,
        config,
      );

      // Step 4: Select errors to fix in this iteration
      const errorsToFix = prioritizedErrors
        .slice(0, config.maxErrorsPerIteration)
        .map(scored => scored.error);

      console.log(`🎯 Fixing ${errorsToFix.length} prioritized errors`);

      // Step 5: Apply corrections
      const correctionResult = await this.corrector.correctErrors(
        currentSolution.solutionCode,
        errorsToFix,
        correctionConfig,
      );

      totalCorrections++;

      // Step 6: Update solution if correction was successful
      let outputSolution = currentSolution;
      let progressMade = false;

      if (correctionResult.success && correctionResult.correctedCode !== currentSolution.solutionCode) {
        outputSolution = {
          ...currentSolution,
          solutionCode: correctionResult.correctedCode,
          confidence: Math.min(currentSolution.confidence, correctionResult.confidence),
          metadata: {
            attempts: currentSolution.metadata?.attempts || 1,
            fallbackUsed: currentSolution.metadata?.fallbackUsed || false,
            engineHealth: currentSolution.metadata?.engineHealth || {},
            transformationSteps: [
              ...(currentSolution.metadata?.transformationSteps || []),
              `Iteration ${iteration}: Applied ${correctionResult.astTransformations.length} AST transformations`,
            ],
          },
        };

        progressMade = true;
        currentSolution = outputSolution;

        console.log(`🔧 Applied ${correctionResult.appliedFixes.length} fixes`);
      } else {
        console.log('❌ Correction failed or produced no changes');
      }

      // Step 7: Record iteration results
      const iterationResult: IterationResult = {
        iterationNumber: iteration,
        inputSolution: currentSolution,
        validationResult,
        correctionResult,
        outputSolution,
        iterationTime: Date.now() - iterationStartTime,
        errorsFixed: correctionResult.appliedFixes.length,
        errorsRemaining: correctionResult.remainingErrors.length,
        progressMade,
      };

      iterations.push(iterationResult);

      // Step 8: Learn from this iteration
      if (config.enableLearningFromFailures) {
        this.updateLearningHistory(errorsToFix, correctionResult, iterationResult);
      }

      // Step 9: Check for timeout
      if (Date.now() - startTime > config.timeoutPerIteration * config.maxIterations) {
        console.log(`⏰ Feedback loop timed out after ${iteration} iterations`);
        break;
      }

      // Step 10: Check if we're making progress
      if (!progressMade && iteration > 1) {
        console.log(`🚫 No progress made in iteration ${iteration}, may need different approach`);

        // Try progressive correction if enabled
        if (config.enableProgressiveCorrection) {
          const progressiveResult = await this.tryProgressiveCorrection(
            currentSolution,
            validationResult.errors,
            correctionConfig,
          );

          if (progressiveResult.success) {
            currentSolution = {
              ...currentSolution,
              solutionCode: progressiveResult.correctedCode,
              confidence: Math.min(currentSolution.confidence, progressiveResult.confidence),
            };
            console.log('🔄 Progressive correction applied');
          }
        }
      }
    }

    // Final validation
    const finalValidation = await this.validator.validateWithStructuredErrors(
      problem,
      currentSolution,
      validationConfig,
    );

    // Extract learning insights
    if (config.enableLearningFromFailures) {
      learningInsights.push(...Array.from(this.learningHistory.values()));
    }

    const totalTime = Date.now() - startTime;
    const success = finalValidation.success;

    console.log(`🏁 Feedback loop completed: ${success ? 'SUCCESS' : 'FAILED'} (${totalTime}ms, ${iterations.length} iterations)`);

    return {
      success,
      finalSolution: currentSolution,
      iterations,
      totalTime,
      totalCorrections,
      finalValidation,
      learningInsights,
    };
  }

  /**
   * Prioritize errors based on severity, fixability, and impact
   */
  private prioritizeErrors(
    errors: StructuredValidationError[],
    config: FeedbackLoopConfig,
  ): ErrorPriorityScore[] {

    if (!config.enableErrorPrioritization) {
      return errors.map(error => ({
        error,
        priority: 1,
        fixability: 1,
        impact: 1,
      }));
    }

    return errors.map(error => {
      const priority = this.calculateErrorPriority(error);
      const fixability = this.calculateErrorFixability(error);
      const impact = this.calculateErrorImpact(error);

      return {
        error,
        priority,
        fixability,
        impact,
      };
    }).sort((a, b) => {
      // Sort by combined score (higher is better)
      const scoreA = a.priority * 0.4 + a.fixability * 0.4 + a.impact * 0.2;
      const scoreB = b.priority * 0.4 + b.fixability * 0.4 + b.impact * 0.2;
      return scoreB - scoreA;
    });
  }

  /**
   * Calculate error priority based on severity
   */
  private calculateErrorPriority(error: StructuredValidationError): number {
    switch (error.severity) {
      case ErrorSeverity.CRITICAL: return 1.0;
      case ErrorSeverity.HIGH: return 0.8;
      case ErrorSeverity.MEDIUM: return 0.6;
      case ErrorSeverity.LOW: return 0.4;
      default: return 0.5;
    }
  }

  /**
   * Calculate error fixability based on type and available fixes
   */
  private calculateErrorFixability(error: StructuredValidationError): number {
    // Errors with suggested fixes are more fixable
    const baseFix = error.suggestedFixes.length > 0 ? 0.8 : 0.4;

    // Some error types are easier to fix
    const typeBonus = this.getFixabilityBonus(error.type);

    return Math.min(1.0, baseFix + typeBonus);
  }

  /**
   * Get fixability bonus based on error type
   */
  private getFixabilityBonus(errorType: ErrorType): number {
    switch (errorType) {
      case ErrorType.IMPORT_ERROR:
      case ErrorType.MODULE_NOT_FOUND_ERROR:
      case ErrorType.INDENTATION_ERROR:
        return 0.2; // Easy to fix
      case ErrorType.NAME_ERROR:
      case ErrorType.SYNTAX_ERROR:
        return 0.1; // Moderately easy
      case ErrorType.TYPE_ERROR:
      case ErrorType.ATTRIBUTE_ERROR:
        return 0.0; // Harder to fix
      default:
        return 0.0;
    }
  }

  /**
   * Calculate error impact on overall solution
   */
  private calculateErrorImpact(error: StructuredValidationError): number {
    // Critical errors that prevent execution have high impact
    if (error.severity === ErrorSeverity.CRITICAL) {
return 1.0;
}

    // Errors that affect correctness have medium impact
    if (error.severity === ErrorSeverity.HIGH) {
return 0.7;
}

    // Style and performance errors have low impact
    return 0.3;
  }

  /**
   * Try progressive correction (fix one error at a time)
   */
  private async tryProgressiveCorrection(
    solution: GolemSolution,
    errors: StructuredValidationError[],
    correctionConfig?: Partial<CorrectionConfig>,
  ): Promise<CorrectionResult> {

    console.log(`🔄 Attempting progressive correction on ${errors.length} errors`);

    let currentCode = solution.solutionCode;
    let totalAppliedFixes = 0;
    let totalTransformations = 0;

    // Try to fix errors one by one
    for (const error of errors.slice(0, 3)) { // Limit to first 3 errors
      const singleErrorResult = await this.corrector.correctErrors(
        currentCode,
        [error],
        correctionConfig,
      );

      if (singleErrorResult.success) {
        currentCode = singleErrorResult.correctedCode;
        totalAppliedFixes += singleErrorResult.appliedFixes.length;
        totalTransformations += singleErrorResult.astTransformations.length;

        console.log(`✅ Progressive fix applied for ${error.nodeType}`);
      }
    }

    return {
      success: currentCode !== solution.solutionCode,
      correctedCode: currentCode,
      appliedFixes: [], // Simplified for progressive correction
      remainingErrors: [],
      astTransformations: [],
      confidence: totalAppliedFixes > 0 ? 0.6 : 0,
      correctionTime: 0,
    };
  }

  /**
   * Update learning history from iteration results
   */
  private updateLearningHistory(
    errorsAttempted: StructuredValidationError[],
    correctionResult: CorrectionResult,
    iterationResult: IterationResult,
  ): void {

    for (const error of errorsAttempted) {
      const pattern = `${error.nodeType}_${error.severity}`;

      const existing = this.learningHistory.get(pattern);
      if (existing) {
        // Update existing insight
        existing.successRate = (existing.successRate + (correctionResult.success ? 1 : 0)) / 2;
        existing.averageTime = (existing.averageTime + iterationResult.iterationTime) / 2;
      } else {
        // Create new insight
        this.learningHistory.set(pattern, {
          errorPattern: pattern,
          correctionStrategy: correctionResult.appliedFixes[0]?.fixType || 'unknown',
          successRate: correctionResult.success ? 1 : 0,
          averageTime: iterationResult.iterationTime,
          complexity: this.calculateErrorComplexity(error),
        });
      }
    }
  }

  /**
   * Calculate error complexity for learning
   */
  private calculateErrorComplexity(error: StructuredValidationError): number {
    let complexity = 0.5; // Base complexity

    // Severity affects complexity
    switch (error.severity) {
      case ErrorSeverity.CRITICAL: complexity += 0.3; break;
      case ErrorSeverity.HIGH: complexity += 0.2; break;
      case ErrorSeverity.MEDIUM: complexity += 0.1; break;
    }

    // Context affects complexity
    if (error.context.surroundingLines.length > 5) {
complexity += 0.1;
}
    if (error.context.functionName) {
complexity += 0.1;
}

    return Math.min(1.0, complexity);
  }

  /**
   * Get learning insights for a specific error pattern
   */
  getLearningInsight(errorType: ErrorType, severity: ErrorSeverity): LearningInsight | null {
    const pattern = `${errorType}_${severity}`;
    return this.learningHistory.get(pattern) || null;
  }

  /**
   * Get all learning insights
   */
  getAllLearningInsights(): LearningInsight[] {
    return Array.from(this.learningHistory.values());
  }

  /**
   * Clear learning history
   */
  clearLearningHistory(): void {
    this.learningHistory.clear();
  }
}

