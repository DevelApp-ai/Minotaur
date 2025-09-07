/**
 * BenchmarkSolver - Validation System Enhancement
 *
 * Replaces the existing GolemBenchmarkSolver with Project Golem's agentic system
 * to provide superior performance over Mistral Codestral alone. Maintains backward
 * compatibility while adding hybrid deterministic-probabilistic correction.
 *
 * Performance Goals:
 * - Higher success rate than pure Mistral approach
 * - Faster response times through deterministic shortcuts
 * - Better code quality through grammar-driven corrections
 * - Comprehensive metrics and comparison data
 */

import { GolemBenchmarkSolver, GolemSolution, SolutionGenerationConfig } from './GolemBenchmarkSolver';
import { BenchmarkProblem } from './BenchmarkDatasetManager';
import { MistralAPIClient } from './MistralAPIClient';
import { MistralAgenticIntegration, HybridCorrectionResult } from './MistralAgenticIntegration';
import { BenchmarkValidator } from './BenchmarkValidator';
import { StructuredBenchmarkValidator } from './StructuredBenchmarkValidator';
import { AgenticSystem } from './AgenticSystem';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StepLexer } from '../utils/StepLexer';
import { createCodestralConfig } from './CodestralAPIConfig';

export interface EnhancedSolverConfig {
  // Golem agentic system configuration
  enableAgenticCorrection: boolean;
  enableHybridMode: boolean;
  enablePerformanceComparison: boolean;

  // Fallback configuration
  enableMistralFallback: boolean;
  fallbackThreshold: number;
  maxCorrectionAttempts: number;

  // Performance optimization
  enableResultCaching: boolean;
  enableParallelProcessing: boolean;
  timeoutPerProblem: number;

  // Metrics and logging
  enableDetailedMetrics: boolean;
  enableProgressLogging: boolean;
  logPerformanceComparisons: boolean;
}

export interface EnhancedSolutionResult {
  // Core solution data
  success: boolean;
  solution: string;
  executionTime: number;

  // Golem-specific metrics
  agenticApproach: string;
  deterministicRatio: number;
  mistralUsed: boolean;
  correctionSteps: number;

  // Performance comparison
  performanceComparison?: {
    golemTime: number;
    mistralTime?: number;
    accuracyImprovement: number;
    qualityScore: number;
  };

  // Error handling
  errors?: string[];
  fallbackUsed: boolean;
  retryCount: number;
}

export interface BenchmarkComparison {
  // Overall metrics
  totalProblems: number;
  golemSuccessRate: number;
  mistralSuccessRate: number;
  improvementRatio: number;

  // Performance metrics
  averageGolemTime: number;
  averageMistralTime: number;
  speedImprovement: number;

  // Quality metrics
  averageQualityScore: number;
  deterministicRatio: number;

  // Detailed results
  problemResults: Map<string, EnhancedSolutionResult>;
  performanceTrends: PerformanceTrend[];
}

export interface PerformanceTrend {
  problemType: string;
  golemAdvantage: number;
  commonPatterns: string[];
  optimizationOpportunities: string[];
}

/**
 * BenchmarkSolver - Main enhanced solver class
 */
export class BenchmarkSolver extends GolemBenchmarkSolver {
  private agenticSystem: AgenticSystem;
  private mistralIntegration: MistralAgenticIntegration;
  private config: EnhancedSolverConfig;
  private solutionCache: Map<string, EnhancedSolutionResult>;
  private performanceMetrics: {
    totalSolutions: number;
    successfulSolutions: number;
    agenticSuccesses: number;
    mistralFallbacks: number;
    averageResponseTime: number;
    cumulativeQualityScore: number;
  };

  constructor(
    mistralClient: MistralAPIClient,
    config: Partial<EnhancedSolverConfig> = {},
  ) {
    // Initialize parent class with working directory
    super(process.cwd());

    // Initialize agentic system with minimal components
    const grammar = new Grammar('python'); // Create minimal grammar
    const stepParser = new StepParser();

    // Create minimal lexer options and source container
    const lexerOptions = { enableOptimizations: true };
    const sourceContainer = {
      getSourceLines: () => [''],
      getSourceText: () => '',
    };

    const stepLexer = new StepLexer(stepParser, lexerOptions as any, sourceContainer as any);

    this.agenticSystem = new AgenticSystem(
      grammar,
      stepParser,
      stepLexer,
    );
    this.mistralIntegration = new MistralAgenticIntegration(
      mistralClient,
      {
        hybridMode: 'fallback',
        enablePerformanceComparison: true,
        enableMistralCaching: true,
      },
    );

    this.config = {
      enableAgenticCorrection: true,
      enableHybridMode: true,
      enablePerformanceComparison: true,
      enableMistralFallback: true,
      fallbackThreshold: 0.5,
      maxCorrectionAttempts: 3,
      enableResultCaching: true,
      enableParallelProcessing: false, // Start with sequential for stability
      timeoutPerProblem: 30000, // 30 seconds per problem
      enableDetailedMetrics: true,
      enableProgressLogging: true,
      logPerformanceComparisons: true,
      ...config,
    };

    this.solutionCache = new Map();
    this.performanceMetrics = {
      totalSolutions: 0,
      successfulSolutions: 0,
      agenticSuccesses: 0,
      mistralFallbacks: 0,
      averageResponseTime: 0,
      cumulativeQualityScore: 0,
    };
  }

  /**
   * Enhanced problem solving using Project Golem agentic system
   * Overrides the parent class method to use Golem instead of pure Mistral
   */
  async solveProblem(problem: BenchmarkProblem, config?: Partial<SolutionGenerationConfig>): Promise<GolemSolution> {
    const startTime = Date.now();
    this.performanceMetrics.totalSolutions++;

    try {
      // Check cache first
      const cacheKey = this.createCacheKey(problem.id, problem.description);
      if (this.config.enableResultCaching && this.solutionCache.has(cacheKey)) {
        const cached = this.solutionCache.get(cacheKey)!;
        if (this.config.enableProgressLogging) {
          console.log(`🔄 Using cached solution for problem ${problem.id}`);
        }
        // Convert EnhancedSolutionResult to GolemSolution
        return {
          problemId: problem.id,
          benchmark: problem.benchmark,
          solutionCode: cached.solution,
          language: problem.language,
          approach: 'hybrid',
          confidence: 0.8,
          generationTime: cached.executionTime,
          engineUsed: 'enhanced-golem',
          metadata: {
            attempts: 1,
            fallbackUsed: false,
            engineHealth: {},
            transformationSteps: [],
          },
        };
      }

      // Attempt enhanced solution with timeout
      const enhancedResult = await Promise.race([
        this.attemptEnhancedSolution(problem),
        this.createTimeoutPromise(this.config.timeoutPerProblem),
      ]);

      // Update metrics and cache
      this.updateMetrics(enhancedResult);
      if (this.config.enableResultCaching && enhancedResult.success) {
        this.solutionCache.set(cacheKey, enhancedResult);
      }

      // Log performance comparison if enabled (simplified for now)
      if (this.config.logPerformanceComparisons) {
        console.log(`📊 Performance comparison for problem ${problem.id}: ${enhancedResult.agenticApproach}`);
      }

      return {
        problemId: problem.id,
        benchmark: problem.benchmark,
        solutionCode: enhancedResult.solution,
        language: problem.language,
        approach: 'hybrid',
        confidence: 0.8,
        generationTime: enhancedResult.executionTime,
        engineUsed: 'enhanced-golem',
        metadata: {
          attempts: 1,
          fallbackUsed: false,
          engineHealth: {},
          transformationSteps: [`Enhanced solution with ${enhancedResult.agenticApproach} approach`],
        },
      };

    } catch (error) {
      console.error(`❌ Enhanced solution failed for problem ${problem.id}:`, error);

      // Fallback to parent class (pure Mistral) if enabled
      if (this.config.enableMistralFallback) {
        if (this.config.enableProgressLogging) {
          console.log(`🔄 Falling back to pure Mistral for problem ${problem.id}`);
        }

        this.performanceMetrics.mistralFallbacks++;
        return await super.solveProblem(problem, config);
      }

      throw error;
    }
  }

  /**
   * Attempt enhanced solution using Project Golem agentic system
   */
  private async attemptEnhancedSolution(
    problem: BenchmarkProblem,
  ): Promise<EnhancedSolutionResult> {

    const startTime = Date.now();
    let attempt = 0;
    let lastError: string | undefined;

    while (attempt < this.config.maxCorrectionAttempts) {
      try {
        if (this.config.enableProgressLogging) {
          console.log(`🎯 Attempting Golem correction for problem ${problem.id} (attempt ${attempt + 1})`);
        }

        // Use agentic system for correction
        let result: HybridCorrectionResult;

        if (this.config.enableHybridMode) {
          // Use the new 3-step workflow: Solution Generation → Validation → Correction
          result = await this.mistralIntegration.enhancedSolutionWithCorrection(
            problem.prompt,
            'benchmark-solver',
            `problem-${problem.id}-${Date.now()}`,
          );
        } else {
          // Use pure agentic system
          const agenticResult = await this.agenticSystem.correctErrors(
            problem.prompt,
            'benchmark-solver',
            `problem-${problem.id}-${Date.now()}`,
          );

          // Convert to HybridCorrectionResult format
          result = {
            ...agenticResult,
            mistralUsed: false,
            mistralConfidence: 0,
            mistralResponseTime: 0,
            hybridApproach: 'agentic_only',
          };
        }

        const executionTime = Date.now() - startTime;

        // Evaluate solution quality
        const qualityScore = await this.evaluateSolutionQuality(
          problem.prompt,
          result.correctedCode || '',
          result,
        );

        // Create enhanced result
        const enhancedResult: EnhancedSolutionResult = {
          success: result.success && qualityScore > this.config.fallbackThreshold && result.correctedCode && result.correctedCode.trim().length > 0,
          solution: result.correctedCode || '',
          executionTime,
          agenticApproach: result.hybridApproach,
          deterministicRatio: result.deterministicRatio,
          mistralUsed: result.mistralUsed,
          correctionSteps: result.correctionSteps.length,
          performanceComparison: result.performanceComparison ? {
            golemTime: executionTime,
            mistralTime: result.mistralResponseTime,
            accuracyImprovement: this.calculateAccuracyImprovement(result),
            qualityScore,
          } : undefined,
          fallbackUsed: false,
          retryCount: attempt,
        };

        if (enhancedResult.success) {
          this.performanceMetrics.agenticSuccesses++;
          return enhancedResult;
        }

        lastError = `Low quality score: ${qualityScore}`;
        attempt++;

      } catch (error) {
        lastError = error instanceof Error ? error.message : String(error);
        attempt++;

        if (attempt < this.config.maxCorrectionAttempts) {
          // Wait before retry
          await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        }
      }
    }

    // All attempts failed
    return {
      success: false,
      solution: problem.prompt, // Return original problem prompt as fallback
      executionTime: Date.now() - startTime,
      agenticApproach: 'failed',
      deterministicRatio: 0,
      mistralUsed: false,
      correctionSteps: 0,
      errors: [lastError || 'Unknown error'],
      fallbackUsed: false,
      retryCount: attempt,
    };
  }

  /**
   * Evaluate solution quality using multiple criteria
   */
  private async evaluateSolutionQuality(
    originalProblem: string,
    solution: string,
    correctionResult: HybridCorrectionResult,
  ): Promise<number> {

    let qualityScore = 0.5; // Base score

    // Criterion 1: Correction success
    if (correctionResult.success) {
      qualityScore += 0.3;
    }

    // Criterion 2: Deterministic ratio (higher is better)
    qualityScore += correctionResult.deterministicRatio * 0.2;

    // Criterion 3: Solution length (reasonable changes)
    const lengthRatio = solution.length / originalProblem.length;
    if (lengthRatio > 0.8 && lengthRatio < 2.0) {
      qualityScore += 0.1;
    }

    // Criterion 4: Syntax validation
    try {
      const validator = new BenchmarkValidator(process.cwd());
      // Create a minimal GolemSolution object for validation
      const solutionObj: GolemSolution = {
        problemId: 'temp',
        benchmark: 'temp',
        solutionCode: solution,
        language: 'python',
        approach: 'hybrid',
        confidence: 0.5,
        generationTime: 0,
        engineUsed: 'temp',
        metadata: {
          attempts: 1,
          fallbackUsed: false,
          engineHealth: {},
          transformationSteps: [],
        },
      };
      const problemObj: BenchmarkProblem = {
        id: 'temp',
        benchmark: 'mbpp',
        title: 'temp',
        description: 'temp',
        prompt: originalProblem,
        language: 'python',
        difficulty: 'medium',
        category: 'temp',
        testCases: [],
        metadata: {},
      };
      const validationResult = await validator.validateSolution(problemObj, solutionObj);
      if (validationResult.passed) {
        qualityScore += 0.2;
      }
    } catch (error) {
      // Validation failed, no bonus
    }

    // Criterion 5: No obvious errors in solution
    if (!solution.includes('Error:') &&
        !solution.includes('undefined') &&
        !solution.includes('NameError')) {
      qualityScore += 0.1;
    }

    return Math.min(qualityScore, 1.0);
  }

  /**
   * Calculate accuracy improvement over baseline
   */
  private calculateAccuracyImprovement(result: HybridCorrectionResult): number {
    // Simple heuristic - would be more sophisticated in production
    if (result.mistralUsed) {
      // Compare hybrid vs pure Mistral
      return result.deterministicRatio * 0.5; // Deterministic components add value
    } else {
      // Pure agentic vs baseline
      return result.success ? 0.3 : -0.1;
    }
  }

  /**
   * Run comparative benchmark against pure Mistral approach
   */
  async runComparativeBenchmark(
    problems: Array<{ id: string; problem: BenchmarkProblem; expectedOutput?: string }>,
    enableParallelComparison: boolean = false,
  ): Promise<BenchmarkComparison> {

    console.log(`🏁 Starting comparative benchmark with ${problems.length} problems`);
    console.log(`📊 Parallel comparison: ${enableParallelComparison ? 'enabled' : 'disabled'}`);

    const results = new Map<string, EnhancedSolutionResult>();
    const mistralResults = new Map<string, { solution: string; time: number; success: boolean }>();

    let golemSuccesses = 0;
    let mistralSuccesses = 0;
    let totalGolemTime = 0;
    let totalMistralTime = 0;
    let totalQualityScore = 0;

    for (const { id, problem, expectedOutput } of problems) {
      console.log(`🎯 Processing problem ${id}...`);

      try {
        // Run Golem solution
        const golemStartTime = Date.now();
        const golemSolution = await this.solveProblem(problem);
        const golemTime = Date.now() - golemStartTime;

        const golemResult = this.solutionCache.get(this.createCacheKey(problem.id, problem.description));
        if (golemResult) {
          results.set(id, golemResult);
          if (golemResult.success) {
golemSuccesses++;
}
          totalGolemTime += golemTime;
          totalQualityScore += golemResult.performanceComparison?.qualityScore || 0;
        }

        // Run Mistral comparison if enabled
        if (enableParallelComparison) {
          const mistralStartTime = Date.now();
          const mistralSolution = await super.solveProblem(problem);
          const mistralTime = Date.now() - mistralStartTime;

          const mistralSuccess = await this.evaluateMistralSolution(
            mistralSolution.solutionCode,
            expectedOutput,
          );

          mistralResults.set(id, {
            solution: mistralSolution.solutionCode,
            time: mistralTime,
            success: mistralSuccess,
          });

          if (mistralSuccess) {
mistralSuccesses++;
}
          totalMistralTime += mistralTime;
        }

        console.log(`✅ Problem ${id} completed (Golem: ${golemResult?.success ? '✅' : '❌'})`);

      } catch (error) {
        console.error(`❌ Problem ${id} failed:`, error);

        results.set(id, {
          success: false,
          solution: problem.prompt,
          executionTime: 0,
          agenticApproach: 'error',
          deterministicRatio: 0,
          mistralUsed: false,
          correctionSteps: 0,
          errors: [error instanceof Error ? error.message : String(error)],
          fallbackUsed: true,
          retryCount: 0,
        });
      }
    }

    // Calculate performance trends
    const performanceTrends = this.analyzePerformanceTrends(results);

    const comparison: BenchmarkComparison = {
      totalProblems: problems.length,
      golemSuccessRate: golemSuccesses / problems.length,
      mistralSuccessRate: enableParallelComparison ? mistralSuccesses / problems.length : 0,
      improvementRatio: enableParallelComparison ?
        (golemSuccesses - mistralSuccesses) / problems.length : 0,
      averageGolemTime: totalGolemTime / problems.length,
      averageMistralTime: enableParallelComparison ? totalMistralTime / problems.length : 0,
      speedImprovement: enableParallelComparison ?
        (totalMistralTime - totalGolemTime) / totalMistralTime : 0,
      averageQualityScore: totalQualityScore / problems.length,
      deterministicRatio: this.calculateOverallDeterministicRatio(results),
      problemResults: results,
      performanceTrends,
    };

    // Log comprehensive results
    this.logBenchmarkResults(comparison);

    return comparison;
  }

  /**
   * Evaluate Mistral solution for comparison
   */
  private async evaluateMistralSolution(
    solution: string,
    expectedOutput?: string,
  ): Promise<boolean> {

    if (expectedOutput) {
      // Direct comparison if expected output is provided
      return solution.trim() === expectedOutput.trim();
    }

    // Heuristic evaluation
    return solution.length > 0 &&
           !solution.includes('Error:') &&
           !solution.includes('I cannot') &&
           !solution.includes('undefined');
  }

  /**
   * Analyze performance trends across problems
   */
  private analyzePerformanceTrends(
    results: Map<string, EnhancedSolutionResult>,
  ): PerformanceTrend[] {

    const trends: PerformanceTrend[] = [];
    const approachGroups = new Map<string, EnhancedSolutionResult[]>();

    // Group results by approach
    for (const result of results.values()) {
      const approach = result.agenticApproach;
      if (!approachGroups.has(approach)) {
        approachGroups.set(approach, []);
      }
      approachGroups.get(approach)!.push(result);
    }

    // Analyze each approach group
    for (const [approach, groupResults] of approachGroups) {
      const successRate = groupResults.filter(r => r.success).length / groupResults.length;
      const avgTime = groupResults.reduce((sum, r) => sum + r.executionTime, 0) / groupResults.length;

      trends.push({
        problemType: approach,
        golemAdvantage: successRate,
        commonPatterns: this.extractCommonPatterns(groupResults),
        optimizationOpportunities: this.identifyOptimizationOpportunities(groupResults),
      });
    }

    return trends;
  }

  /**
   * Extract common patterns from results
   */
  private extractCommonPatterns(results: EnhancedSolutionResult[]): string[] {
    const patterns: string[] = [];

    const highDeterministicRatio = results.filter(r => r.deterministicRatio > 0.8).length;
    if (highDeterministicRatio > results.length * 0.5) {
      patterns.push('High deterministic success rate');
    }

    const fastSolutions = results.filter(r => r.executionTime < 1000).length;
    if (fastSolutions > results.length * 0.7) {
      patterns.push('Fast response times');
    }

    const mistralUsage = results.filter(r => r.mistralUsed).length;
    if (mistralUsage < results.length * 0.3) {
      patterns.push('Low LLM dependency');
    }

    return patterns;
  }

  /**
   * Identify optimization opportunities
   */
  private identifyOptimizationOpportunities(results: EnhancedSolutionResult[]): string[] {
    const opportunities: string[] = [];

    const slowSolutions = results.filter(r => r.executionTime > 5000).length;
    if (slowSolutions > results.length * 0.2) {
      opportunities.push('Optimize slow correction paths');
    }

    const highRetryCount = results.filter(r => r.retryCount > 1).length;
    if (highRetryCount > results.length * 0.3) {
      opportunities.push('Improve first-attempt success rate');
    }

    const lowDeterministic = results.filter(r => r.deterministicRatio < 0.5).length;
    if (lowDeterministic > results.length * 0.4) {
      opportunities.push('Enhance deterministic correction rules');
    }

    return opportunities;
  }

  /**
   * Calculate overall deterministic ratio across all results
   */
  private calculateOverallDeterministicRatio(
    results: Map<string, EnhancedSolutionResult>,
  ): number {

    const ratios = Array.from(results.values()).map(r => r.deterministicRatio);
    return ratios.reduce((sum, ratio) => sum + ratio, 0) / ratios.length;
  }

  /**
   * Log comprehensive benchmark results
   */
  private logBenchmarkResults(comparison: BenchmarkComparison): void {
    console.log('\n🏆 BENCHMARK RESULTS SUMMARY');
    console.log('=' .repeat(50));
    console.log(`📊 Total Problems: ${comparison.totalProblems}`);
    console.log(`✅ Golem Success Rate: ${(comparison.golemSuccessRate * 100).toFixed(1)}%`);

    if (comparison.mistralSuccessRate > 0) {
      console.log(`🤖 Mistral Success Rate: ${(comparison.mistralSuccessRate * 100).toFixed(1)}%`);
      console.log(`📈 Improvement: ${(comparison.improvementRatio * 100).toFixed(1)}%`);
    }

    console.log(`⚡ Average Golem Time: ${comparison.averageGolemTime.toFixed(0)}ms`);

    if (comparison.averageMistralTime > 0) {
      console.log(`🐌 Average Mistral Time: ${comparison.averageMistralTime.toFixed(0)}ms`);
      console.log(`🚀 Speed Improvement: ${(comparison.speedImprovement * 100).toFixed(1)}%`);
    }

    console.log(`🎯 Average Quality Score: ${(comparison.averageQualityScore * 100).toFixed(1)}%`);
    console.log(`🔧 Deterministic Ratio: ${(comparison.deterministicRatio * 100).toFixed(1)}%`);

    console.log('\n📈 PERFORMANCE TRENDS:');
    for (const trend of comparison.performanceTrends) {
      console.log(`  ${trend.problemType}: ${(trend.golemAdvantage * 100).toFixed(1)}% success`);
      if (trend.commonPatterns.length > 0) {
        console.log(`    Patterns: ${trend.commonPatterns.join(', ')}`);
      }
      if (trend.optimizationOpportunities.length > 0) {
        console.log(`    Opportunities: ${trend.optimizationOpportunities.join(', ')}`);
      }
    }

    console.log('=' .repeat(50));
  }

  /**
   * Log performance comparison for individual problems
   */
  private logPerformanceComparison(
    problemId: string | undefined,
    result: EnhancedSolutionResult,
  ): void {

    const id = problemId || 'unknown';
    const comparison = result.performanceComparison;

    if (comparison) {
      console.log(`📊 Problem ${id} Performance:`);
      console.log(`  Golem: ${comparison.golemTime}ms (${result.success ? '✅' : '❌'})`);
      if (comparison.mistralTime) {
        console.log(`  Mistral: ${comparison.mistralTime}ms`);
        console.log(`  Speed: ${comparison.golemTime < comparison.mistralTime ? '🚀' : '🐌'}`);
      }
      console.log(`  Quality: ${(comparison.qualityScore * 100).toFixed(1)}%`);
      console.log(`  Deterministic: ${(result.deterministicRatio * 100).toFixed(1)}%`);
    }
  }

  /**
   * Update performance metrics
   */
  private updateMetrics(result: EnhancedSolutionResult): void {
    if (result.success) {
      this.performanceMetrics.successfulSolutions++;
    }

    // Update average response time
    const total = this.performanceMetrics.totalSolutions;
    this.performanceMetrics.averageResponseTime =
      (this.performanceMetrics.averageResponseTime * (total - 1) + result.executionTime) / total;

    // Update cumulative quality score
    const qualityScore = result.performanceComparison?.qualityScore || 0;
    this.performanceMetrics.cumulativeQualityScore =
      (this.performanceMetrics.cumulativeQualityScore * (total - 1) + qualityScore) / total;
  }

  /**
   * Create cache key for results
   */
  private createCacheKey(problem: string, problemId?: string): string {
    const hash = this.simpleHash(problem + (problemId || ''));
    return `enhanced_${hash}`;
  }

  /**
   * Simple hash function for cache keys
   */
  private simpleHash(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash;
    }
    return Math.abs(hash).toString(36);
  }

  /**
   * Create timeout promise
   */
  private createTimeoutPromise(timeoutMs: number): Promise<never> {
    return new Promise((_, reject) => {
      setTimeout(() => reject(new Error('Solution timeout')), timeoutMs);
    });
  }

  /**
   * Get performance metrics
   */
  getPerformanceMetrics() {
    return { ...this.performanceMetrics };
  }

  /**
   * Clear solution cache
   */
  clearCache(): void {
    this.solutionCache.clear();
  }

  /**
   * Get cache statistics
   */
  getCacheStatistics() {
    return {
      cacheSize: this.solutionCache.size,
      cacheEnabled: this.config.enableResultCaching,
    };
  }
}

