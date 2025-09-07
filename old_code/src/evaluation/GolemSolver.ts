/**
 * Enhanced Golem Solver with AST-Guided Error Correction
 * 
 * This is the main integration point that combines:
 * 1. Real LLM solution generation (Mistral Codestral)
 * 2. Structured error validation 
 * 3. AST-guided error correction
 * 4. Multi-solution generation and selection
 * 
 * This implements the core Project Golem vision of hybrid LLM + AST cooperation.
 */

import { GolemBenchmarkSolver, GolemSolution } from './GolemBenchmarkSolver';
import { BenchmarkProblem, HumanEvalProblem, MBPPProblem } from './BenchmarkDatasetManager';
import { StructuredBenchmarkValidator, StructuredValidationConfig } from './StructuredBenchmarkValidator';
import { ASTErrorCorrector, CorrectionConfig, CorrectionResult } from './ASTErrorCorrector';
import { MistralAPIClient, MistralAPIConfig } from './MistralAPIClient';
import { CorrectionFeedbackLoop, FeedbackLoopConfig, FeedbackLoopResult } from './CorrectionFeedbackLoop';
import { 
  StructuredValidationError, 
  ValidationResult as StructuredValidationResult,
  ErrorSeverity, 
} from './StructuredValidationError';

export interface EnhancedSolutionAttempt {
  attemptNumber: number;
  originalSolution: GolemSolution;
  correctionResults: CorrectionResult[];
  finalSolution: GolemSolution;
  validationResult: StructuredValidationResult;
  success: boolean;
  confidence: number;
  totalCorrectionTime: number;
  astTransformationsApplied: number;
}

export interface MultiSolutionResult {
  problemId: string;
  benchmark: string;
  attempts: EnhancedSolutionAttempt[];
  workingSolutions: GolemSolution[];
  selectedSolution: GolemSolution | null;
  selectionReason: string;
  totalTime: number;
  totalLLMCalls: number;
  totalCorrectionAttempts: number;
  success: boolean;
}

export interface EnhancedSolverConfig {
  maxSolutionAttempts: number;
  targetWorkingSolutions: number;
  maxCorrectionAttempts: number;
  enableASTCorrection: boolean;
  enableLLMCorrection: boolean;
  enableFeedbackLoop: boolean;
  selectionCriteria: 'least_impact' | 'highest_confidence' | 'fastest_execution';
  timeoutPerProblem: number;
  mistralConfig: MistralAPIConfig;
  validationConfig: Partial<StructuredValidationConfig>;
  correctionConfig: Partial<CorrectionConfig>;
  feedbackLoopConfig: Partial<FeedbackLoopConfig>;
}

/**
 * Enhanced Golem Solver
 * 
 * Implements the full Project Golem vision:
 * - Generate multiple LLM solutions
 * - Use AST-guided correction for failed solutions
 * - Select optimal solution based on impact analysis
 */
export class GolemSolver extends GolemBenchmarkSolver {
  private structuredValidator: StructuredBenchmarkValidator;
  private astCorrector: ASTErrorCorrector;
  private feedbackLoop: CorrectionFeedbackLoop;
  private config: EnhancedSolverConfig;

  constructor(config: EnhancedSolverConfig) {
    super();
    this.config = config;
    
    // Initialize validation and correction systems
    this.structuredValidator = new StructuredBenchmarkValidator();
    this.astCorrector = new ASTErrorCorrector(config.mistralConfig);
    this.feedbackLoop = new CorrectionFeedbackLoop(this.structuredValidator, this.astCorrector);
  }

  /**
   * Solve problem with full Golem approach:
   * 1. Generate multiple solutions
   * 2. Correct errors using AST guidance
   * 3. Select optimal solution
   */
  async solveWithGolemApproach(problem: BenchmarkProblem): Promise<MultiSolutionResult> {
    const startTime = Date.now();
    const attempts: EnhancedSolutionAttempt[] = [];
    const workingSolutions: GolemSolution[] = [];
    let totalLLMCalls = 0;
    let totalCorrectionAttempts = 0;

    console.log(`🎯 Starting Golem approach for problem: ${problem.id}`);

    // Phase 1: Generate multiple solutions until we have enough working ones
    let attemptNumber = 0;
    while (
      workingSolutions.length < this.config.targetWorkingSolutions &&
      attemptNumber < this.config.maxSolutionAttempts &&
      (Date.now() - startTime) < this.config.timeoutPerProblem
    ) {
      attemptNumber++;
      
      console.log(`🔄 Attempt ${attemptNumber}: Generating solution...`);
      
      const attempt = await this.performSolutionAttempt(
        problem, 
        attemptNumber, 
        totalLLMCalls,
        totalCorrectionAttempts,
      );
      
      attempts.push(attempt);
      totalLLMCalls += 1; // Initial LLM call
      totalCorrectionAttempts += attempt.correctionResults.length;
      
      if (attempt.success) {
        workingSolutions.push(attempt.finalSolution);
        console.log(`✅ Working solution ${workingSolutions.length}/${this.config.targetWorkingSolutions} found`);
      } else {
        console.log(`❌ Attempt ${attemptNumber} failed after corrections`);
      }
    }

    // Phase 2: Select optimal solution
    const selectedSolution = workingSolutions.length > 0 
      ? this.selectOptimalSolution(workingSolutions, problem)
      : null;

    const totalTime = Date.now() - startTime;
    const success = selectedSolution !== null;

    const result: MultiSolutionResult = {
      problemId: problem.id,
      benchmark: problem.benchmark,
      attempts,
      workingSolutions,
      selectedSolution,
      selectionReason: this.getSelectionReason(selectedSolution, workingSolutions),
      totalTime,
      totalLLMCalls,
      totalCorrectionAttempts,
      success,
    };

    console.log(`🏁 Golem approach completed: ${success ? 'SUCCESS' : 'FAILED'} (${totalTime}ms)`);
    console.log(`📊 Stats: ${workingSolutions.length} working solutions, ${totalLLMCalls} LLM calls, ${totalCorrectionAttempts} corrections`);

    return result;
  }

  /**
   * Perform a single solution attempt with correction loop
   */
  private async performSolutionAttempt(
    problem: BenchmarkProblem,
    attemptNumber: number,
    currentLLMCalls: number,
    currentCorrectionAttempts: number,
  ): Promise<EnhancedSolutionAttempt> {
    
    const startTime = Date.now();
    
    // Step 1: Generate initial solution using parent class (real LLM)
    const originalSolution = await this.generateSingleSolution(problem, attemptNumber);
    
    let currentSolution = originalSolution;
    let validationResult: StructuredValidationResult;
    const correctionResults: CorrectionResult[] = [];
    let astTransformationsApplied = 0;
    
    // Step 2: Use feedback loop if enabled, otherwise use legacy correction
    if (this.config.enableFeedbackLoop) {
      console.log('🔄 Using feedback loop for correction');
      
      const feedbackLoopConfig: FeedbackLoopConfig = {
        maxIterations: this.config.maxCorrectionAttempts,
        maxErrorsPerIteration: 3,
        enableProgressiveCorrection: true,
        enableErrorPrioritization: true,
        enableLearningFromFailures: true,
        timeoutPerIteration: 30000,
        confidenceThreshold: 0.7,
        ...this.config.feedbackLoopConfig,
      };
      
      const feedbackResult = await this.feedbackLoop.executeFeedbackLoop(
        problem,
        originalSolution,
        feedbackLoopConfig,
        this.config.validationConfig,
        this.config.correctionConfig,
      );
      
      currentSolution = feedbackResult.finalSolution;
      validationResult = feedbackResult.finalValidation;
      astTransformationsApplied = feedbackResult.iterations.reduce(
        (sum, iter) => sum + (iter.correctionResult?.astTransformations.length || 0), 0,
      );
      
      // Convert feedback loop results to correction results for compatibility
      for (const iteration of feedbackResult.iterations) {
        if (iteration.correctionResult) {
          correctionResults.push(iteration.correctionResult);
        }
      }
      
    } else {
      console.log('🔧 Using legacy correction approach');
      
      // Step 2: Validate with structured error reporting
      validationResult = await this.structuredValidator.validateWithStructuredErrors(
        problem,
        originalSolution,
        this.config.validationConfig,
      );
      
      // Step 3: Legacy correction loop - fix errors using AST guidance
      let correctionAttempt = 0;
      while (
        !validationResult.success &&
        validationResult.errors.length > 0 &&
        correctionAttempt < this.config.maxCorrectionAttempts
      ) {
        correctionAttempt++;
        
        console.log(`🔧 Correction attempt ${correctionAttempt}: Fixing ${validationResult.errors.length} errors`);
        
        // Use AST-guided correction
        const correctionResult = await this.astCorrector.correctErrors(
          currentSolution.solutionCode,
          validationResult.errors,
          this.config.correctionConfig,
        );
        
        correctionResults.push(correctionResult);
        
        if (correctionResult.success) {
          // Update solution with corrected code
          currentSolution = {
            ...currentSolution,
            solutionCode: correctionResult.correctedCode,
            confidence: Math.min(currentSolution.confidence, correctionResult.confidence),
            metadata: {
              ...currentSolution.metadata,
              transformationSteps: [
                ...currentSolution.metadata.transformationSteps,
                `Correction attempt ${correctionAttempt}: ${correctionResult.astTransformations.length} AST transformations`,
              ],
            },
          };
          
          astTransformationsApplied += correctionResult.astTransformations.length;
          
          // Re-validate corrected solution
          validationResult = await this.structuredValidator.validateWithStructuredErrors(
            problem,
            currentSolution,
            this.config.validationConfig,
          );
          
          if (validationResult.success) {
            console.log(`✅ Correction successful after ${correctionAttempt} attempts`);
            break;
          }
        } else {
          console.log(`❌ Correction attempt ${correctionAttempt} failed`);
        }
      }
    }
    
    const totalCorrectionTime = correctionResults.reduce(
      (sum, result) => sum + result.correctionTime, 0,
    );
    
    return {
      attemptNumber,
      originalSolution,
      correctionResults,
      finalSolution: currentSolution,
      validationResult,
      success: validationResult.success,
      confidence: currentSolution.confidence,
      totalCorrectionTime,
      astTransformationsApplied,
    };
  }

  /**
   * Generate a single solution using the real LLM (parent class method)
   */
  private async generateSingleSolution(
    problem: BenchmarkProblem, 
    attemptNumber: number,
  ): Promise<GolemSolution> {
    
    // Use parent class methods that now call real Mistral API
    switch (problem.benchmark) {
      case 'humaneval':
        return await this.solveProblem(problem);
        
      case 'mbpp':
        return await this.solveProblem(problem);
        
      default:
        // For other benchmarks, use generic approach
        return {
          problemId: problem.id,
          benchmark: problem.benchmark,
          language: problem.language,
          approach: 'hybrid',
          engineUsed: 'codestral-latest',
          solutionCode: await this.generateGenericSolution(problem, attemptNumber),
          confidence: 0.7,
          generationTime: 0,
          metadata: {
            attempts: attemptNumber,
            fallbackUsed: false,
            engineHealth: { 'codestral-latest': 1.0 },
            transformationSteps: [`Generated solution using codestral-latest (attempt ${attemptNumber})`],
          },
        };
    }
  }

  /**
   * Generate generic solution for non-standard benchmarks
   */
  private async generateGenericSolution(
    problem: BenchmarkProblem, 
    attemptNumber: number,
  ): Promise<string> {
    
    const prompt = `Solve this programming problem:

Problem: ${problem.id}
Description: ${(problem as any).description || (problem as any).prompt || 'No description available'}

Provide a complete Python solution that solves the problem correctly.

Solution:`;

    const response = await this.mistralClient.generateCompletion({
      model: 'codestral-latest',
      messages: [
        {
          role: 'user',
          content: prompt,
        },
      ],
      max_tokens: 2000,
      temperature: 0.1 + (attemptNumber - 1) * 0.1, // Increase temperature for variety
    });

    return response.response.choices[0]?.message?.content || '# No solution generated';
  }

  /**
   * Select optimal solution based on configured criteria
   */
  private selectOptimalSolution(
    workingSolutions: GolemSolution[],
    problem: BenchmarkProblem,
  ): GolemSolution | null {
    
    if (workingSolutions.length === 0) {
return null;
}
    if (workingSolutions.length === 1) {
return workingSolutions[0];
}

    switch (this.config.selectionCriteria) {
      case 'least_impact':
        return this.selectLeastImpactSolution(workingSolutions);
        
      case 'highest_confidence':
        return this.selectHighestConfidenceSolution(workingSolutions);
        
      case 'fastest_execution':
        return this.selectFastestSolution(workingSolutions);
        
      default:
        return workingSolutions[0];
    }
  }

  /**
   * Select solution with least impact (minimal complexity/changes)
   */
  private selectLeastImpactSolution(solutions: GolemSolution[]): GolemSolution {
    return solutions.reduce((best, current) => {
      const bestImpact = this.calculateSolutionImpact(best);
      const currentImpact = this.calculateSolutionImpact(current);
      return currentImpact < bestImpact ? current : best;
    });
  }

  /**
   * Select solution with highest confidence
   */
  private selectHighestConfidenceSolution(solutions: GolemSolution[]): GolemSolution {
    return solutions.reduce((best, current) => 
      current.confidence > best.confidence ? current : best,
    );
  }

  /**
   * Select solution with fastest execution (based on generation time)
   */
  private selectFastestSolution(solutions: GolemSolution[]): GolemSolution {
    return solutions.reduce((best, current) => 
      current.generationTime < best.generationTime ? current : best,
    );
  }

  /**
   * Calculate solution impact score (lower is better)
   */
  private calculateSolutionImpact(solution: GolemSolution): number {
    const code = solution.solutionCode;
    
    // Factors that increase impact:
    const linesOfCode = code.split('\n').filter(line => line.trim().length > 0).length;
    const complexity = this.calculateComplexity(code);
    const transformationSteps = solution.metadata?.transformationSteps?.length || 0;
    const engineHealth = Object.values(solution.metadata?.engineHealth || {}).reduce((a, b) => a + b, 0);
    
    // Weighted impact score
    return (
      linesOfCode * 0.1 +           // Prefer shorter solutions
      complexity * 0.3 +            // Prefer simpler solutions  
      transformationSteps * 0.2 +   // Prefer solutions that needed fewer transformation steps
      (1 - engineHealth) * 0.4     // Prefer solutions from healthier engines
    );
  }

  /**
   * Calculate code complexity (simplified)
   */
  private calculateComplexity(code: string): number {
    // Count decision points and nested structures
    const decisionKeywords = ['if', 'elif', 'while', 'for', 'try', 'except'];
    let complexity = 1;
    
    for (const keyword of decisionKeywords) {
      const matches = code.match(new RegExp(`\\b${keyword}\\b`, 'g'));
      if (matches) {
complexity += matches.length;
}
    }
    
    // Add penalty for nested structures (rough approximation)
    const indentationLevels = code.split('\n').map(line => {
      const match = line.match(/^(\s*)/);
      return match ? Math.floor(match[1].length / 4) : 0;
    });
    
    const maxNesting = Math.max(...indentationLevels);
    complexity += maxNesting * 2;
    
    return complexity;
  }

  /**
   * Get human-readable selection reason
   */
  private getSelectionReason(
    selectedSolution: GolemSolution | null, 
    workingSolutions: GolemSolution[],
  ): string {
    
    if (!selectedSolution) {
return 'No working solutions found';
}
    if (workingSolutions.length === 1) {
return 'Only working solution';
}
    
    const impact = this.calculateSolutionImpact(selectedSolution);
    const avgImpact = workingSolutions.reduce(
      (sum, sol) => sum + this.calculateSolutionImpact(sol), 0,
    ) / workingSolutions.length;
    
    switch (this.config.selectionCriteria) {
      case 'least_impact':
        return `Selected for minimal impact (score: ${impact.toFixed(2)} vs avg: ${avgImpact.toFixed(2)})`;
      case 'highest_confidence':
        return `Selected for highest confidence (${(selectedSolution.confidence * 100).toFixed(1)}%)`;
      case 'fastest_execution':
        return `Selected for fastest generation (${selectedSolution.generationTime}ms)`;
      default:
        return 'Selected by default criteria';
    }
  }
}

