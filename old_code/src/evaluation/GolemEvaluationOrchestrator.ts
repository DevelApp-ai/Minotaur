/**
 * Golem Evaluation Orchestrator
 * 
 * Main entry point for the complete Project Golem evaluation system.
 * Orchestrates the full pipeline:
 * 1. Multi-solution generation per problem
 * 2. AST-guided error correction
 * 3. Impact-based solution selection
 * 4. Comprehensive reporting
 */

import { GolemSolver, EnhancedSolverConfig, MultiSolutionResult } from './GolemSolver';
import { BenchmarkDatasetManager, BenchmarkProblem } from './BenchmarkDatasetManager';
import { MistralAPIConfig } from './MistralAPIClient';
import { StructuredValidationConfig } from './StructuredBenchmarkValidator';
import { CorrectionConfig } from './ASTErrorCorrector';

export interface GolemEvaluationConfig {
  // Dataset configuration
  benchmarks: string[];
  problemsPerBenchmark?: number;
  
  // Solution generation
  maxSolutionAttempts: number;
  targetWorkingSolutions: number;
  
  // Error correction
  maxCorrectionAttempts: number;
  enableASTCorrection: boolean;
  enableLLMCorrection: boolean;
  
  // Solution selection
  selectionCriteria: 'least_impact' | 'highest_confidence' | 'fastest_execution';
  
  // Performance limits
  timeoutPerProblem: number;
  maxCostPerProblem: number;
  
  // API configuration
  mistralApiKey: string;
  mistralModel: string;
  
  // Output configuration
  outputDirectory: string;
  saveIntermediateResults: boolean;
  verboseLogging: boolean;
}

export interface GolemEvaluationResults {
  config: GolemEvaluationConfig;
  startTime: Date;
  endTime: Date;
  totalDuration: number;
  
  // Problem-level results
  problemResults: MultiSolutionResult[];
  
  // Aggregate statistics
  totalProblems: number;
  successfulProblems: number;
  successRate: number;
  
  // Solution statistics
  totalSolutionAttempts: number;
  totalWorkingSolutions: number;
  avgSolutionsPerProblem: number;
  
  // Correction statistics
  totalCorrectionAttempts: number;
  successfulCorrections: number;
  correctionSuccessRate: number;
  
  // Performance statistics
  totalLLMCalls: number;
  totalCost: number;
  avgTimePerProblem: number;
  
  // Quality metrics
  avgConfidence: number;
  avgImpactScore: number;
  
  // Benchmark breakdown
  benchmarkResults: Record<string, {
    problems: number;
    successful: number;
    successRate: number;
    avgTime: number;
  }>;
}

/**
 * Main Golem Evaluation Orchestrator
 */
export class GolemEvaluationOrchestrator {
  private config: GolemEvaluationConfig;
  private datasetManager: BenchmarkDatasetManager;
  private solver: GolemSolver;
  
  constructor(config: GolemEvaluationConfig) {
    this.config = config;
    this.datasetManager = new BenchmarkDatasetManager();
    
    // Configure enhanced solver
    const solverConfig: EnhancedSolverConfig = {
      maxSolutionAttempts: config.maxSolutionAttempts,
      targetWorkingSolutions: config.targetWorkingSolutions,
      maxCorrectionAttempts: config.maxCorrectionAttempts,
      enableASTCorrection: config.enableASTCorrection,
      enableLLMCorrection: config.enableLLMCorrection,
      enableFeedbackLoop: true,
      selectionCriteria: config.selectionCriteria,
      timeoutPerProblem: config.timeoutPerProblem,
      
      mistralConfig: {
        apiKey: config.mistralApiKey,
        model: config.mistralModel,
        rateLimit: {
          requestsPerMinute: 60,
          requestsPerHour: 3600,
          tokensPerMinute: 60000,
          tokensPerHour: 3600000,
          burstLimit: 1,
          adaptiveThrottling: true,
        },
      } as MistralAPIConfig,
      
      validationConfig: {
        timeoutMs: 30000,
        enableStructuredErrors: true,
        captureStdout: true,
        captureStderr: true,
      } as Partial<StructuredValidationConfig>,
      
      correctionConfig: {
        maxAttempts: config.maxCorrectionAttempts,
        useASTTransformation: config.enableASTCorrection,
        useLLMAssistance: config.enableLLMCorrection,
        preserveCodeStyle: true,
        enableAggressiveCorrection: false,
        timeoutMs: 30000,
      } as Partial<CorrectionConfig>,
      
      feedbackLoopConfig: {
        maxIterations: config.maxCorrectionAttempts,
        maxErrorsPerIteration: 3,
        enableProgressiveCorrection: true,
        enableErrorPrioritization: true,
        enableLearningFromFailures: true,
        timeoutPerIteration: 30000,
        confidenceThreshold: 0.7,
      },
    };
    
    this.solver = new GolemSolver(solverConfig);
  }

  /**
   * Run complete Golem evaluation
   */
  async runEvaluation(): Promise<GolemEvaluationResults> {
    const startTime = new Date();
    
    console.log('🚀 Starting Project Golem Evaluation');
    console.log(`📊 Configuration: ${this.config.benchmarks.join(', ')}`);
    console.log(`🎯 Target: ${this.config.targetWorkingSolutions} solutions per problem`);
    console.log(`🔧 Correction: AST=${this.config.enableASTCorrection}, LLM=${this.config.enableLLMCorrection}`);
    
    // Initialize solver
    await this.solver.initialize();
    
    // Load problems from all benchmarks
    const allProblems = await this.loadProblems();
    console.log(`📚 Loaded ${allProblems.length} problems from ${this.config.benchmarks.length} benchmarks`);
    
    // Process each problem with full Golem approach
    const problemResults: MultiSolutionResult[] = [];
    let totalCost = 0;
    
    for (let i = 0; i < allProblems.length; i++) {
      const problem = allProblems[i];
      
      console.log(`\n🎯 Problem ${i + 1}/${allProblems.length}: ${problem.id} (${problem.benchmark})`);
      
      try {
        const result = await this.solver.solveWithGolemApproach(problem);
        problemResults.push(result);
        
        // Estimate cost (rough approximation)
        const estimatedCost = result.totalLLMCalls * 0.01; // $0.01 per call estimate
        totalCost += estimatedCost;
        
        if (result.success) {
          console.log(`✅ SUCCESS: ${result.workingSolutions.length} working solutions found`);
          console.log(`🏆 Selected: ${result.selectionReason}`);
        } else {
          console.log(`❌ FAILED: No working solutions after ${result.totalCorrectionAttempts} corrections`);
        }
        
        // Check cost limit
        if (totalCost > this.config.maxCostPerProblem * allProblems.length) {
          console.log(`💰 Cost limit reached (${totalCost.toFixed(2)}), stopping evaluation`);
          break;
        }
        
        // Save intermediate results if configured
        if (this.config.saveIntermediateResults) {
          await this.saveIntermediateResult(result, i + 1);
        }
        
      } catch (error) {
        console.error(`💥 Error processing problem ${problem.id}:`, error);
        
        // Create failed result
        const failedResult: MultiSolutionResult = {
          problemId: problem.id,
          benchmark: problem.benchmark,
          attempts: [],
          workingSolutions: [],
          selectedSolution: null,
          selectionReason: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`,
          totalTime: 0,
          totalLLMCalls: 0,
          totalCorrectionAttempts: 0,
          success: false,
        };
        
        problemResults.push(failedResult);
      }
    }
    
    const endTime = new Date();
    const totalDuration = endTime.getTime() - startTime.getTime();
    
    // Calculate comprehensive results
    const results = this.calculateResults(
      problemResults,
      startTime,
      endTime,
      totalDuration,
      totalCost,
    );
    
    // Save final results
    await this.saveFinalResults(results);
    
    console.log('\n🏁 Project Golem Evaluation Complete!');
    console.log(`📊 Success Rate: ${(results.successRate * 100).toFixed(1)}%`);
    console.log(`⏱️  Total Time: ${(totalDuration / 1000 / 60).toFixed(1)} minutes`);
    console.log(`💰 Total Cost: $${results.totalCost.toFixed(2)}`);
    console.log(`🎯 Avg Solutions: ${results.avgSolutionsPerProblem.toFixed(1)} per problem`);
    
    return results;
  }

  /**
   * Load problems from configured benchmarks
   */
  private async loadProblems(): Promise<BenchmarkProblem[]> {
    const allProblems: BenchmarkProblem[] = [];
    
    for (const benchmark of this.config.benchmarks) {
      let problems: BenchmarkProblem[] = [];
      
      switch (benchmark.toLowerCase()) {
        case 'humaneval':
          problems = this.datasetManager.getBenchmarkProblems('humaneval');
          break;
        case 'mbpp':
          problems = this.datasetManager.getBenchmarkProblems('mbpp');
          break;
        case 'swe-bench':
          problems = this.datasetManager.getBenchmarkProblems('swe-bench');
          break;
        case 'quixbugs':
          problems = this.datasetManager.getBenchmarkProblems('quixbugs');
          break;
        case 'fim':
          problems = this.datasetManager.getBenchmarkProblems('fim');
          break;
        default:
          console.warn(`Unknown benchmark: ${benchmark}`);
          continue;
      }
      
      // Limit problems per benchmark if configured
      if (this.config.problemsPerBenchmark && this.config.problemsPerBenchmark > 0) {
        problems = problems.slice(0, this.config.problemsPerBenchmark);
      }
      
      allProblems.push(...problems);
    }
    
    return allProblems;
  }

  /**
   * Calculate comprehensive evaluation results
   */
  private calculateResults(
    problemResults: MultiSolutionResult[],
    startTime: Date,
    endTime: Date,
    totalDuration: number,
    totalCost: number,
  ): GolemEvaluationResults {
    
    const totalProblems = problemResults.length;
    const successfulProblems = problemResults.filter(r => r.success).length;
    const successRate = totalProblems > 0 ? successfulProblems / totalProblems : 0;
    
    const totalSolutionAttempts = problemResults.reduce(
      (sum, r) => sum + r.attempts.length, 0,
    );
    
    const totalWorkingSolutions = problemResults.reduce(
      (sum, r) => sum + r.workingSolutions.length, 0,
    );
    
    const avgSolutionsPerProblem = totalProblems > 0 
      ? totalWorkingSolutions / totalProblems : 0;
    
    const totalCorrectionAttempts = problemResults.reduce(
      (sum, r) => sum + r.totalCorrectionAttempts, 0,
    );
    
    const successfulCorrections = problemResults.reduce(
      (sum, r) => sum + r.attempts.filter(a => a.correctionResults.some(cr => cr.success)).length, 0,
    );
    
    const correctionSuccessRate = totalCorrectionAttempts > 0 
      ? successfulCorrections / totalCorrectionAttempts : 0;
    
    const totalLLMCalls = problemResults.reduce(
      (sum, r) => sum + r.totalLLMCalls, 0,
    );
    
    const avgTimePerProblem = totalProblems > 0 
      ? problemResults.reduce((sum, r) => sum + r.totalTime, 0) / totalProblems : 0;
    
    const successfulResults = problemResults.filter(r => r.success);
    const avgConfidence = successfulResults.length > 0
      ? successfulResults.reduce((sum, r) => sum + (r.selectedSolution?.confidence || 0), 0) / successfulResults.length
      : 0;
    
    // Calculate average impact score from successful solutions
    const avgImpactScore = successfulResults.length > 0
      ? successfulResults.reduce((sum, r) => {
          if (r.selectedSolution) {
            // Calculate impact score for the selected solution
            const solution = r.selectedSolution;
            const code = solution.solutionCode;
            
            // Same impact calculation as in GolemSolver.calculateSolutionImpact()
            const linesOfCode = code.split('\n').filter(line => line.trim().length > 0).length;
            const complexity = this.calculateComplexity(code);
            const transformationSteps = solution.metadata?.transformationSteps?.length || 0;
            const engineHealth = Object.values(solution.metadata?.engineHealth || {}).reduce((a, b) => a + b, 0);
            
            // Weighted impact score (lower is better)
            const impactScore = (
              linesOfCode * 0.1 +           // Prefer shorter solutions
              complexity * 0.3 +            // Prefer simpler solutions  
              transformationSteps * 0.2 +   // Prefer solutions that needed fewer transformation steps
              (1 - engineHealth) * 0.4     // Prefer solutions from healthier engines
            );
            
            return sum + impactScore;
          }
          return sum;
        }, 0) / successfulResults.length
      : 0;
    
    // Calculate benchmark breakdown
    const benchmarkResults: Record<string, any> = {};
    for (const benchmark of this.config.benchmarks) {
      const benchmarkProblems = problemResults.filter(r => r.benchmark === benchmark);
      const benchmarkSuccessful = benchmarkProblems.filter(r => r.success).length;
      
      benchmarkResults[benchmark] = {
        problems: benchmarkProblems.length,
        successful: benchmarkSuccessful,
        successRate: benchmarkProblems.length > 0 ? benchmarkSuccessful / benchmarkProblems.length : 0,
        avgTime: benchmarkProblems.length > 0 
          ? benchmarkProblems.reduce((sum, r) => sum + r.totalTime, 0) / benchmarkProblems.length
          : 0,
      };
    }
    
    return {
      config: this.config,
      startTime,
      endTime,
      totalDuration,
      problemResults,
      totalProblems,
      successfulProblems,
      successRate,
      totalSolutionAttempts,
      totalWorkingSolutions,
      avgSolutionsPerProblem,
      totalCorrectionAttempts,
      successfulCorrections,
      correctionSuccessRate,
      totalLLMCalls,
      totalCost,
      avgTimePerProblem,
      avgConfidence,
      avgImpactScore, // Now calculated from successful solutions
      benchmarkResults,
    };
  }

  /**
   * Save intermediate result for a single problem
   */
  private async saveIntermediateResult(result: MultiSolutionResult, problemNumber: number): Promise<void> {
    try {
      const fs = await import('fs/promises');
      const path = await import('path');
      
      const filename = `problem_${problemNumber.toString().padStart(4, '0')}_${result.problemId}.json`;
      const filepath = path.join(this.config.outputDirectory, 'intermediate', filename);
      
      // Ensure directory exists
      await fs.mkdir(path.dirname(filepath), { recursive: true });
      
      await fs.writeFile(filepath, JSON.stringify(result, null, 2));
      
      if (this.config.verboseLogging) {
        console.log(`💾 Saved intermediate result: ${filename}`);
      }
    } catch (error) {
      console.warn('Failed to save intermediate result:', error);
    }
  }

  /**
   * Save final evaluation results
   */
  private async saveFinalResults(results: GolemEvaluationResults): Promise<void> {
    try {
      const fs = await import('fs/promises');
      const path = await import('path');
      
      // Ensure output directory exists
      await fs.mkdir(this.config.outputDirectory, { recursive: true });
      
      // Save complete results
      const resultsPath = path.join(this.config.outputDirectory, 'golem_evaluation_results.json');
      await fs.writeFile(resultsPath, JSON.stringify(results, null, 2));
      
      // Save summary report
      const summaryPath = path.join(this.config.outputDirectory, 'golem_evaluation_summary.md');
      const summaryReport = this.generateSummaryReport(results);
      await fs.writeFile(summaryPath, summaryReport);
      
      console.log(`💾 Results saved to: ${this.config.outputDirectory}`);
      
    } catch (error) {
      console.error('Failed to save final results:', error);
    }
  }

  /**
   * Generate human-readable summary report
   */
  private generateSummaryReport(results: GolemEvaluationResults): string {
    const duration = results.totalDuration / 1000 / 60; // minutes
    
    return `# Project Golem Evaluation Report

## Overview
- **Start Time**: ${results.startTime.toISOString()}
- **End Time**: ${results.endTime.toISOString()}
- **Duration**: ${duration.toFixed(1)} minutes
- **Benchmarks**: ${results.config.benchmarks.join(', ')}

## Results Summary
- **Total Problems**: ${results.totalProblems}
- **Successful**: ${results.successfulProblems}
- **Success Rate**: ${(results.successRate * 100).toFixed(1)}%
- **Average Confidence**: ${(results.avgConfidence * 100).toFixed(1)}%

## Solution Generation
- **Total Solution Attempts**: ${results.totalSolutionAttempts}
- **Working Solutions Found**: ${results.totalWorkingSolutions}
- **Average Solutions per Problem**: ${results.avgSolutionsPerProblem.toFixed(1)}

## Error Correction
- **Total Correction Attempts**: ${results.totalCorrectionAttempts}
- **Successful Corrections**: ${results.successfulCorrections}
- **Correction Success Rate**: ${(results.correctionSuccessRate * 100).toFixed(1)}%

## Performance
- **Total LLM Calls**: ${results.totalLLMCalls}
- **Estimated Cost**: $${results.totalCost.toFixed(2)}
- **Average Time per Problem**: ${(results.avgTimePerProblem / 1000).toFixed(1)} seconds

## Benchmark Breakdown
${Object.entries(results.benchmarkResults).map(([benchmark, stats]) => 
  `- **${benchmark}**: ${stats.successful}/${stats.problems} (${(stats.successRate * 100).toFixed(1)}%) - ${(stats.avgTime / 1000).toFixed(1)}s avg`,
).join('\n')}

## Configuration
- **Max Solution Attempts**: ${results.config.maxSolutionAttempts}
- **Target Working Solutions**: ${results.config.targetWorkingSolutions}
- **Max Correction Attempts**: ${results.config.maxCorrectionAttempts}
- **AST Correction**: ${results.config.enableASTCorrection ? 'Enabled' : 'Disabled'}
- **LLM Correction**: ${results.config.enableLLMCorrection ? 'Enabled' : 'Disabled'}
- **Selection Criteria**: ${results.config.selectionCriteria}
`;
  }

  /**
   * Calculate code complexity score (same as GolemSolver.calculateComplexity)
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
}

