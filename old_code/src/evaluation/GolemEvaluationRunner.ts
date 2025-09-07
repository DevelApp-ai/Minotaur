/**
 * Golem Evaluation Runner
 *
 * This system orchestrates the complete evaluation pipeline for Golem against
 * standard LLM coding benchmarks. It coordinates dataset loading, solution
 * generation, validation, and comprehensive reporting.
 */
import { StepLexer } from '../utils/StepLexer';

import { BenchmarkDatasetManager, BenchmarkProblem } from './BenchmarkDatasetManager';
import { BenchmarkSolver } from './BenchmarkSolver';
import { MistralAPIClient } from './MistralAPIClient';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { BenchmarkValidator, ValidationResult, PassAtKResult, BenchmarkEvaluationSummary, ValidationConfig } from './BenchmarkValidator';
import { promises as fs } from 'fs';
import * as path from 'path';

export interface EvaluationConfig {
  // Dataset configuration
  benchmarks: string[]; // Which benchmarks to run
  problemsPerBenchmark?: number; // Limit problems per benchmark (for testing)
  difficultyFilter?: string[]; // Filter by difficulty
  categoryFilter?: string[]; // Filter by category

  // Solution generation configuration
  solutionConfig: any; // Partial<SolutionGenerationConfig>;

  // Validation configuration
  validationConfig: any; // Partial<ValidationConfig>;

  // Evaluation configuration
  calculatePassAtK: number[]; // Which k values to calculate (e.g., [1, 5, 10])
  generateDetailedReports: boolean;
  exportResults: boolean;
  outputDirectory: string;

  // Performance configuration
  parallelProcessing: boolean;
  maxConcurrentEvaluations: number;
  enableProgressReporting: boolean;
  saveIntermediateResults: boolean;
}

export interface EvaluationRun {
  runId: string;
  startTime: number;
  endTime?: number;
  config: EvaluationConfig;
  status: 'running' | 'completed' | 'failed' | 'cancelled';
  progress: {
    currentBenchmark?: string;
    currentPhase: 'initialization' | 'dataset_loading' | 'solution_generation' | 'validation' | 'analysis' | 'reporting';
    problemsProcessed: number;
    totalProblems: number;
    solutionsGenerated: number;
    validationsCompleted: number;
    estimatedTimeRemaining?: number;
  };
  results: {
    benchmarkResults: Map<string, BenchmarkEvaluationSummary>;
    passAtKResults: Map<string, PassAtKResult[]>;
    overallStats: {
      totalProblems: number;
      totalSolutions: number;
      overallSuccessRate: number;
      averageConfidence: number;
      averageGenerationTime: number;
      averageValidationTime: number;
    };
  };
  errors: string[];
  warnings: string[];
}

export interface ComparativeAnalysis {
  golemResults: {
    passAt1: number;
    passAt5: number;
    passAt10: number;
    overallScore: number;
    byBenchmark: Record<string, number>;
  };
  publishedBaselines: {
    gpt4: Record<string, number>;
    claude: Record<string, number>;
    codex: Record<string, number>;
    [model: string]: Record<string, number>;
  };
  analysis: {
    golemRanking: number;
    strengthBenchmarks: string[];
    weaknessBenchmarks: string[];
    recommendations: string[];
  };
}

export class GolemEvaluationRunner {
  private datasetManager: BenchmarkDatasetManager;
  private solver: BenchmarkSolver | null = null;
  private mistralClient: MistralAPIClient | null = null;
  private grammar: Grammar | null = null;
  private parser: StepParser | null = null;
  private validator: BenchmarkValidator;
  private workingDirectory: string;
  private currentRun: EvaluationRun | null = null;
  private evaluationHistory: Map<string, EvaluationRun> = new Map();

  constructor(workingDir: string = process.cwd()) {
    this.workingDirectory = workingDir;
    this.datasetManager = new BenchmarkDatasetManager(path.join(workingDir, 'benchmark_data'));
    this.validator = new BenchmarkValidator(workingDir);
  }

  /**
   * Initialize the evaluation runner
   */
  async initialize(): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('🚀 Initializing Project Golem Evaluation Runner...');

    // Initialize Mistral API client
    const apiKey = process.env.MISTRAL_API_KEY || process.env.OPENAI_API_KEY;
    const apiBase = process.env.MISTRAL_API_BASE || process.env.OPENAI_API_BASE || 'https://api.mistral.ai/v1';

    if (!apiKey) {
      throw new Error('MISTRAL_API_KEY or OPENAI_API_KEY environment variable is required');
    }

    this.mistralClient = new MistralAPIClient({
      apiKey: apiKey,
      baseURL: apiBase,
      rateLimit: {
        requestsPerMinute: 60,
        requestsPerHour: 3600,
        tokensPerMinute: 60000,
        tokensPerHour: 3600000,
        burstLimit: 1,
        adaptiveThrottling: true,
      },
      enableRequestQueuing: true,
      enableAdaptiveBackoff: true,
      enableCostTracking: true,
      logLevel: 'info',
    });
    console.log('✅ Mistral API client initialized');

    // Load Python grammar for evaluation
    const grammarPath = path.join(__dirname, '../../grammar/Python311.grammar');
    try {
      const grammarContent = await fs.readFile(grammarPath, 'utf-8');
      this.grammar = new Grammar('Python311');
      this.parser = new StepParser();
      console.log('✅ Python 3.11 grammar loaded');
    } catch (error) {
      console.warn('⚠️  Could not load Python grammar, using fallback');
      // Create minimal grammar for evaluation
      this.grammar = new Grammar('Fallback'); // Fallback
      this.parser = new StepParser();
    }

    // Initialize enhanced benchmark solver with Project Golem capabilities
    this.solver = new BenchmarkSolver(this.mistralClient);
    console.log('✅ Enhanced Golem benchmark solver initialized');

    // Initialize other components
    await Promise.all([
      this.datasetManager.initialize(),
      this.validator.initialize(),
    ]);

    // eslint-disable-next-line no-console
    console.log('✅ Project Golem Evaluation Runner initialized successfully');
    // eslint-disable-next-line no-console
    console.log(`📊 Available benchmarks: ${this.datasetManager.getAvailableBenchmarks().join(', ')}`);
    // eslint-disable-next-line no-console
    console.log(`📈 Total problems available: ${this.datasetManager.getTotalProblems()}`);
    // eslint-disable-next-line no-console
    console.log('🤖 Enhanced with Project Golem AST correction and multi-solution generation');
  }

  /**
   * Run a complete evaluation
   */
  async runEvaluation(config: EvaluationConfig): Promise<EvaluationRun> {
    const runId = `eval_${Date.now()}`;
    const startTime = Date.now();

    // eslint-disable-next-line no-console
    console.log(`🎯 Starting Golem evaluation run: ${runId}`);
    // eslint-disable-next-line no-console
    console.log(`📋 Benchmarks: ${config.benchmarks.join(', ')}`);

    // Create evaluation run
    this.currentRun = {
      runId,
      startTime,
      config,
      status: 'running',
      progress: {
        currentPhase: 'initialization',
        problemsProcessed: 0,
        totalProblems: 0,
        solutionsGenerated: 0,
        validationsCompleted: 0,
      },
      results: {
        benchmarkResults: new Map(),
        passAtKResults: new Map(),
        overallStats: {
          totalProblems: 0,
          totalSolutions: 0,
          overallSuccessRate: 0,
          averageConfidence: 0,
          averageGenerationTime: 0,
          averageValidationTime: 0,
        },
      },
      errors: [],
      warnings: [],
    };

    try {
      // Phase 1: Dataset Loading
      await this.loadDatasetsPhase();

      // Phase 2: Solution Generation
      await this.solutionGenerationPhase();

      // Phase 3: Validation
      await this.validationPhase();

      // Phase 4: Analysis
      await this.analysisPhase();

      // Phase 5: Reporting
      await this.reportingPhase();

      // Complete the run
      this.currentRun.status = 'completed';
      this.currentRun.endTime = Date.now();

    // eslint-disable-next-line no-console
      console.log(`🎉 Evaluation run ${runId} completed successfully!`);
    // eslint-disable-next-line no-console
      console.log(`⏱️  Total time: ${((this.currentRun.endTime - startTime) / 1000).toFixed(2)}s`);
    // eslint-disable-next-line no-console
      console.log(`📊 Overall success rate: ${this.currentRun.results.overallStats.overallSuccessRate.toFixed(2)}%`);

    } catch (error) {
      this.currentRun.status = 'failed';
      this.currentRun.endTime = Date.now();
      this.currentRun.errors.push(`Evaluation failed: ${error}`);

    // eslint-disable-next-line no-console
      console.error(`❌ Evaluation run ${runId} failed:`, error);
      throw error;
    } finally {
      // Store run in history
      this.evaluationHistory.set(runId, this.currentRun);

      // Save intermediate results if configured
      if (config.saveIntermediateResults) {
        await this.saveEvaluationRun(this.currentRun);
      }
    }

    return this.currentRun;
  }

  /**
   * Phase 1: Load datasets and filter problems
   */
  private async loadDatasetsPhase(): Promise<void> {
    if (!this.currentRun) {
      throw new Error('No active evaluation run');
    }

    // eslint-disable-next-line no-console
    console.log('📚 Phase 1: Loading datasets...');
    this.currentRun.progress.currentPhase = 'dataset_loading';

    const config = this.currentRun.config;
    const allProblems: BenchmarkProblem[] = [];

    for (const benchmark of config.benchmarks) {
      try {
    // eslint-disable-next-line no-console
        console.log(`  Loading ${benchmark} benchmark...`);

        let problems = this.datasetManager.getBenchmarkProblems(benchmark);

        // Apply filters
        if (config.difficultyFilter && config.difficultyFilter.length > 0) {
          problems = problems.filter(p => config.difficultyFilter!.includes(p.difficulty));
        }

        if (config.categoryFilter && config.categoryFilter.length > 0) {
          problems = problems.filter(p => config.categoryFilter!.includes(p.category));
        }

        // Limit problems per benchmark if specified
        if (config.problemsPerBenchmark && config.problemsPerBenchmark > 0) {
          problems = problems.slice(0, config.problemsPerBenchmark);
        }

        allProblems.push(...problems);
    // eslint-disable-next-line no-console
        console.log(`  ✅ Loaded ${problems.length} problems from ${benchmark}`);

      } catch (error) {
        const errorMsg = `Failed to load ${benchmark} benchmark: ${error}`;
        this.currentRun.errors.push(errorMsg);
    // eslint-disable-next-line no-console
        console.error(`  ❌ ${errorMsg}`);
      }
    }

    this.currentRun.progress.totalProblems = allProblems.length;
    this.currentRun.results.overallStats.totalProblems = allProblems.length;

    // Store problems for subsequent phases
    (this.currentRun as any).problems = allProblems;

    // eslint-disable-next-line no-console
    console.log(`📊 Dataset loading complete: ${allProblems.length} total problems`);
  }

  /**
   * Phase 2: Generate solutions for all problems
   */
  private async solutionGenerationPhase(): Promise<void> {
    if (!this.currentRun) {
      throw new Error('No active evaluation run');
    }

    // eslint-disable-next-line no-console
    console.log('🧠 Phase 2: Generating solutions...');
    this.currentRun.progress.currentPhase = 'solution_generation';

    const problems = (this.currentRun as any).problems as BenchmarkProblem[];
    const config = this.currentRun.config;
    const allSolutions: any[] = [];

    // Group problems by benchmark for batch processing
    const problemsByBenchmark = new Map<string, BenchmarkProblem[]>();
    for (const problem of problems) {
      if (!problemsByBenchmark.has(problem.benchmark)) {
        problemsByBenchmark.set(problem.benchmark, []);
      }
      problemsByBenchmark.get(problem.benchmark)!.push(problem);
    }

    // Process each benchmark
    for (const [benchmark, benchmarkProblems] of problemsByBenchmark) {
    // eslint-disable-next-line no-console
      console.log(`  🎯 Generating solutions for ${benchmark} (${benchmarkProblems.length} problems)...`);
      this.currentRun.progress.currentBenchmark = benchmark;

      try {
        if (config.parallelProcessing) {
          // Parallel processing (for future implementation)
    // eslint-disable-next-line no-console
          console.log('    Using parallel processing...');
          // Would implement parallel solution generation here
        }

        // Sequential processing
        for (let i = 0; i < benchmarkProblems.length; i++) {
          const problem = benchmarkProblems[i];

          try {
    // eslint-disable-next-line no-console
            console.log(`    Solving problem ${i + 1}/${benchmarkProblems.length}: ${problem.id}`);

            const solution = await this.solver.solveProblem(problem, config.solutionConfig);
            allSolutions.push(solution);

            this.currentRun.progress.solutionsGenerated++;
            this.currentRun.progress.problemsProcessed++;

            // Progress reporting
            if (config.enableProgressReporting && (i + 1) % 10 === 0) {
              // eslint-disable-next-line max-len
              const progress = ((this.currentRun.progress.problemsProcessed / this.currentRun.progress.totalProblems) * 100).toFixed(1);
    // eslint-disable-next-line no-console
              // eslint-disable-next-line max-len
              console.log(`    Progress: ${progress}% (${this.currentRun.progress.problemsProcessed}/${this.currentRun.progress.totalProblems})`);
            }

          } catch (error) {
            const errorMsg = `Failed to solve problem ${problem.id}: ${error}`;
            this.currentRun.errors.push(errorMsg);
    // eslint-disable-next-line no-console
            console.warn(`    ⚠️  ${errorMsg}`);
          }
        }

    // eslint-disable-next-line no-console
        console.log(`  ✅ Generated ${benchmarkProblems.length} solutions for ${benchmark}`);

      } catch (error) {
        const errorMsg = `Failed to process ${benchmark} benchmark: ${error}`;
        this.currentRun.errors.push(errorMsg);
    // eslint-disable-next-line no-console
        console.error(`  ❌ ${errorMsg}`);
      }
    }

    // Store solutions for subsequent phases
    (this.currentRun as any).solutions = allSolutions;
    this.currentRun.results.overallStats.totalSolutions = allSolutions.length;

    // Calculate generation statistics
    const generationTimes = allSolutions.map(s => s.generationTime);
    const confidences = allSolutions.map(s => s.confidence);

    this.currentRun.results.overallStats.averageGenerationTime =
      generationTimes.reduce((sum, time) => sum + time, 0) / generationTimes.length;
    this.currentRun.results.overallStats.averageConfidence =
      confidences.reduce((sum, conf) => sum + conf, 0) / confidences.length;

    // eslint-disable-next-line no-console
    console.log(`🧠 Solution generation complete: ${allSolutions.length} solutions generated`);
    // eslint-disable-next-line no-console
    console.log(`📈 Average confidence: ${this.currentRun.results.overallStats.averageConfidence.toFixed(2)}`);
    // eslint-disable-next-line no-console
    // eslint-disable-next-line max-len
    console.log(`⏱️  Average generation time: ${this.currentRun.results.overallStats.averageGenerationTime.toFixed(0)}ms`);
  }

  /**
   * Phase 3: Validate all solutions
   */
  private async validationPhase(): Promise<void> {
    if (!this.currentRun) {
      throw new Error('No active evaluation run');
    }

    // eslint-disable-next-line no-console
    console.log('✅ Phase 3: Validating solutions...');
    this.currentRun.progress.currentPhase = 'validation';

    const problems = (this.currentRun as any).problems as BenchmarkProblem[];
    const solutions = (this.currentRun as any).solutions as any[];
    const config = this.currentRun.config;

    // Create problem lookup for efficient access
    const problemLookup = new Map<string, BenchmarkProblem>();
    for (const problem of problems) {
      problemLookup.set(problem.id, problem);
    }

    const validationResults: ValidationResult[] = [];

    // Validate each solution
    for (let i = 0; i < solutions.length; i++) {
      const solution = solutions[i];
      const problem = problemLookup.get(solution.problemId);

      if (!problem) {
        this.currentRun.warnings.push(`Problem not found for solution: ${solution.problemId}`);
        continue;
      }

      try {
    // eslint-disable-next-line no-console
        console.log(`  Validating solution ${i + 1}/${solutions.length}: ${solution.problemId}`);

        const validationResult = await this.validator.validateSolution(
          problem,
          solution,
          config.validationConfig,
        );

        validationResults.push(validationResult);
        this.currentRun.progress.validationsCompleted++;

        // Progress reporting
        if (config.enableProgressReporting && (i + 1) % 10 === 0) {
          const progress = ((i + 1) / solutions.length * 100).toFixed(1);
    // eslint-disable-next-line no-console
          console.log(`    Validation progress: ${progress}% (${i + 1}/${solutions.length})`);
        }

      } catch (error) {
        const errorMsg = `Failed to validate solution ${solution.problemId}: ${error}`;
        this.currentRun.errors.push(errorMsg);
    // eslint-disable-next-line no-console
        console.warn(`    ⚠️  ${errorMsg}`);
      }
    }

    // Store validation results
    (this.currentRun as any).validationResults = validationResults;

    // Calculate validation statistics
    const validationTimes = validationResults.map(r => r.executionTime);
    const passedValidations = validationResults.filter(r => r.passed).length;

    this.currentRun.results.overallStats.averageValidationTime =
      validationTimes.reduce((sum, time) => sum + time, 0) / validationTimes.length;
    this.currentRun.results.overallStats.overallSuccessRate =
      validationResults.length > 0 ? (passedValidations / validationResults.length) * 100 : 0;

    // eslint-disable-next-line no-console
    console.log(`✅ Validation complete: ${validationResults.length} solutions validated`);
    // eslint-disable-next-line no-console
    console.log(`📊 Success rate: ${this.currentRun.results.overallStats.overallSuccessRate.toFixed(2)}%`);
    // eslint-disable-next-line no-console
    // eslint-disable-next-line max-len
    console.log(`⏱️  Average validation time: ${this.currentRun.results.overallStats.averageValidationTime.toFixed(0)}ms`);
  }

  /**
   * Phase 4: Analyze results and calculate metrics
   */
  private async analysisPhase(): Promise<void> {
    if (!this.currentRun) {
      throw new Error('No active evaluation run');
    }

    // eslint-disable-next-line no-console
    console.log('📊 Phase 4: Analyzing results...');
    this.currentRun.progress.currentPhase = 'analysis';

    const problems = (this.currentRun as any).problems as BenchmarkProblem[];
    const solutions = (this.currentRun as any).solutions as any[];
    const config = this.currentRun.config;

    // Group problems and solutions by benchmark
    const problemsByBenchmark = new Map<string, BenchmarkProblem[]>();
    const solutionsByBenchmark = new Map<string, any[]>();

    for (const problem of problems) {
      if (!problemsByBenchmark.has(problem.benchmark)) {
        problemsByBenchmark.set(problem.benchmark, []);
      }
      problemsByBenchmark.get(problem.benchmark)!.push(problem);
    }

    for (const solution of solutions) {
      const problem = problems.find(p => p.id === solution.problemId);
      if (problem) {
        if (!solutionsByBenchmark.has(problem.benchmark)) {
          solutionsByBenchmark.set(problem.benchmark, []);
        }
        solutionsByBenchmark.get(problem.benchmark)!.push(solution);
      }
    }

    // Analyze each benchmark
    for (const benchmark of config.benchmarks) {
      const benchmarkProblems = problemsByBenchmark.get(benchmark) || [];
      const benchmarkSolutions = solutionsByBenchmark.get(benchmark) || [];

      if (benchmarkProblems.length === 0) {
    // eslint-disable-next-line no-console
        console.log(`  ⚠️  No problems found for ${benchmark}, skipping analysis`);
        continue;
      }

    // eslint-disable-next-line no-console
      // eslint-disable-next-line max-len
      console.log(`  📈 Analyzing ${benchmark} (${benchmarkProblems.length} problems, ${benchmarkSolutions.length} solutions)...`);

      try {
        // Generate benchmark evaluation summary
        const summary = await this.validator.generateEvaluationSummary(
          benchmark,
          benchmarkProblems,
          benchmarkSolutions,
        );

        this.currentRun.results.benchmarkResults.set(benchmark, summary);

        // Calculate pass@k for different k values
        const passAtKResults: PassAtKResult[] = [];
        for (const k of config.calculatePassAtK) {
          const passAtK = await this.validator.calculatePassAtK(
            benchmarkProblems,
            benchmarkSolutions,
            k,
          );
          passAtKResults.push(passAtK);
        }

        this.currentRun.results.passAtKResults.set(benchmark, passAtKResults);

    // eslint-disable-next-line no-console
        console.log(`    ✅ ${benchmark} analysis complete`);
    // eslint-disable-next-line no-console
        console.log(`    📊 Pass@1: ${summary.passAt1.toFixed(2)}%`);
    // eslint-disable-next-line no-console
        console.log(`    📊 Pass@5: ${summary.passAt5.toFixed(2)}%`);
    // eslint-disable-next-line no-console
        console.log(`    📊 Pass@10: ${summary.passAt10.toFixed(2)}%`);

      } catch (error) {
        const errorMsg = `Failed to analyze ${benchmark}: ${error}`;
        this.currentRun.errors.push(errorMsg);
    // eslint-disable-next-line no-console
        console.error(`    ❌ ${errorMsg}`);
      }
    }

    // eslint-disable-next-line no-console
    console.log('📊 Analysis phase complete');
  }

  /**
   * Phase 5: Generate reports and export results
   */
  private async reportingPhase(): Promise<void> {
    if (!this.currentRun) {
      throw new Error('No active evaluation run');
    }

    // eslint-disable-next-line no-console
    console.log('📝 Phase 5: Generating reports...');
    this.currentRun.progress.currentPhase = 'reporting';

    const config = this.currentRun.config;

    if (config.exportResults) {
      // Ensure output directory exists
      await fs.mkdir(config.outputDirectory, { recursive: true });

      // Export detailed results
      if (config.generateDetailedReports) {
        await this.generateDetailedReports();
      }

      // Export summary results
      await this.generateSummaryReport();

      // Export raw data
      await this.exportRawData();
    }

    // eslint-disable-next-line no-console
    console.log('📝 Reporting phase complete');
  }

  /**
   * Generate detailed reports
   */
  private async generateDetailedReports(): Promise<void> {
    if (!this.currentRun) {
      return;
    }

    const outputDir = this.currentRun.config.outputDirectory;

    // Generate benchmark-specific reports
    for (const [benchmark, summary] of this.currentRun.results.benchmarkResults) {
      const reportPath = path.join(outputDir, `${benchmark}_detailed_report.json`);
      await fs.writeFile(reportPath, JSON.stringify(summary, null, 2));
    // eslint-disable-next-line no-console
      console.log(`  📄 Generated detailed report: ${reportPath}`);
    }

    // Generate pass@k reports
    for (const [benchmark, passAtKResults] of this.currentRun.results.passAtKResults) {
      const reportPath = path.join(outputDir, `${benchmark}_pass_at_k.json`);
      await fs.writeFile(reportPath, JSON.stringify(passAtKResults, null, 2));
    // eslint-disable-next-line no-console
      console.log(`  📄 Generated pass@k report: ${reportPath}`);
    }
  }

  /**
   * Generate summary report
   */
  private async generateSummaryReport(): Promise<void> {
    if (!this.currentRun) {
      return;
    }

    const outputDir = this.currentRun.config.outputDirectory;

    // Create comprehensive summary
    const summary = {
      evaluationRun: {
        runId: this.currentRun.runId,
        startTime: new Date(this.currentRun.startTime).toISOString(),
        endTime: this.currentRun.endTime ? new Date(this.currentRun.endTime).toISOString() : null,
        duration: this.currentRun.endTime ? this.currentRun.endTime - this.currentRun.startTime : null,
        status: this.currentRun.status,
      },
      configuration: this.currentRun.config,
      overallResults: this.currentRun.results.overallStats,
      benchmarkSummaries: Object.fromEntries(this.currentRun.results.benchmarkResults),
      passAtKSummary: Object.fromEntries(
        Array.from(this.currentRun.results.passAtKResults.entries()).map(([benchmark, results]) => [
          benchmark,
          results.reduce((acc, result) => {
            acc[`pass@${result.k}`] = result.passAtK;
            return acc;
          }, {} as Record<string, number>),
        ]),
      ),
      errors: this.currentRun.errors,
      warnings: this.currentRun.warnings,
    };

    const summaryPath = path.join(outputDir, 'evaluation_summary.json');
    await fs.writeFile(summaryPath, JSON.stringify(summary, null, 2));

    // eslint-disable-next-line no-console
    console.log(`  📄 Generated summary report: ${summaryPath}`);
  }

  /**
   * Export raw data
   */
  private async exportRawData(): Promise<void> {
    if (!this.currentRun) {
      return;
    }

    const outputDir = this.currentRun.config.outputDirectory;

    // Export solutions
    const solutionsPath = path.join(outputDir, 'solutions.json');
    await this.solver.exportSolutions(solutionsPath);

    // Export validation results
    const validationPath = path.join(outputDir, 'validation_results.json');
    await this.validator.exportResults(validationPath);

    // eslint-disable-next-line no-console
    console.log(`  💾 Exported raw data to ${outputDir}`);
  }

  /**
   * Save evaluation run to disk
   */
  private async saveEvaluationRun(run: EvaluationRun): Promise<void> {
    const runPath = path.join(this.workingDirectory, 'evaluation_runs', `${run.runId}.json`);
    await fs.mkdir(path.dirname(runPath), { recursive: true });
    await fs.writeFile(runPath, JSON.stringify(run, null, 2));
  }

  /**
   * Get current evaluation run
   */
  getCurrentRun(): EvaluationRun | null {
    return this.currentRun;
  }

  /**
   * Get evaluation run by ID
   */
  getEvaluationRun(runId: string): EvaluationRun | undefined {
    return this.evaluationHistory.get(runId);
  }

  /**
   * Get all evaluation runs
   */
  getAllEvaluationRuns(): EvaluationRun[] {
    return Array.from(this.evaluationHistory.values());
  }

  /**
   * Cancel current evaluation run
   */
  async cancelCurrentRun(): Promise<void> {
    if (this.currentRun && this.currentRun.status === 'running') {
      this.currentRun.status = 'cancelled';
      this.currentRun.endTime = Date.now();
    // eslint-disable-next-line no-console
      console.log(`🛑 Evaluation run ${this.currentRun.runId} cancelled`);
    }
  }

  /**
   * Generate comparative analysis against published baselines
   */
  async generateComparativeAnalysis(): Promise<ComparativeAnalysis> {
    if (!this.currentRun || this.currentRun.status !== 'completed') {
      throw new Error('No completed evaluation run available for analysis');
    }

    // Extract Golem results
    const golemResults = {
      passAt1: 0,
      passAt5: 0,
      passAt10: 0,
      overallScore: this.currentRun.results.overallStats.overallSuccessRate,
      byBenchmark: {} as Record<string, number>,
    };

    // Calculate average pass@k across benchmarks
    let totalPassAt1 = 0, totalPassAt5 = 0, totalPassAt10 = 0;
    let benchmarkCount = 0;

    for (const [benchmark, summary] of this.currentRun.results.benchmarkResults) {
      golemResults.byBenchmark[benchmark] = summary.overallSuccessRate;
      totalPassAt1 += summary.passAt1;
      totalPassAt5 += summary.passAt5;
      totalPassAt10 += summary.passAt10;
      benchmarkCount++;
    }

    if (benchmarkCount > 0) {
      golemResults.passAt1 = totalPassAt1 / benchmarkCount;
      golemResults.passAt5 = totalPassAt5 / benchmarkCount;
      golemResults.passAt10 = totalPassAt10 / benchmarkCount;
    }

    // Published baselines (updated with Codestral and ambitious Golem targets)
    const publishedBaselines = {
      gpt4: {
        'humaneval': 67.0,
        'mbpp': 76.1,
        'swe-bench': 12.3,
        'overall': 51.8,
      },
      claude: {
        'humaneval': 73.0,
        'mbpp': 78.0,
        'swe-bench': 14.0,
        'overall': 55.0,
      },
      codex: {
        'humaneval': 72.3,
        'mbpp': 70.1,
        'swe-bench': 11.0,
        'overall': 51.1,
      },
      codestral: {
        'humaneval': 81.1,
        'mbpp': 78.2,
        'swe-bench': 14.0,
        'overall': 57.8,
      },
      golem_target: {
        'humaneval': 83.5,  // Target: 82-85% (exceed Codestral)
        'mbpp': 82.5,       // Target: 80-85% (exceed Codestral)
        'swe-bench': 17.5,  // Target: 15-20% (exceed current best)
        'overall': 61.2,     // Ambitious overall target
      },
    };

    // Simple ranking analysis
    const models = Object.keys(publishedBaselines);
    const golemOverallScore = golemResults.overallScore;
    let ranking = 1;

    for (const model of models) {
      if (publishedBaselines[model].overall > golemOverallScore) {
        ranking++;
      }
    }

    // Identify strengths and weaknesses
    const strengthBenchmarks: string[] = [];
    const weaknessBenchmarks: string[] = [];

    for (const [benchmark, score] of Object.entries(golemResults.byBenchmark)) {
      const avgBaseline = models.reduce((sum, model) => {
        return sum + (publishedBaselines[model][benchmark] || 0);
      }, 0) / models.length;

      if (score > avgBaseline) {
        strengthBenchmarks.push(benchmark);
      } else {
        weaknessBenchmarks.push(benchmark);
      }
    }

    // Generate recommendations
    const recommendations = [
      `Golem ranks #${ranking} among evaluated models`,
      strengthBenchmarks.length > 0 ? `Strong performance on: ${strengthBenchmarks.join(', ')}` : null,
      weaknessBenchmarks.length > 0 ? `Improvement needed on: ${weaknessBenchmarks.join(', ')}` : null,
      golemResults.passAt1 < 50 ? 'Consider improving solution generation accuracy' : null,
      'Focus on benchmark-specific optimization strategies',
    ].filter(Boolean) as string[];

    return {
      golemResults,
      publishedBaselines,
      analysis: {
        golemRanking: ranking,
        strengthBenchmarks,
        weaknessBenchmarks,
        recommendations,
      },
    };
  }

  /**
   * Create a quick evaluation configuration for testing
   */
  static createQuickEvaluationConfig(outputDir: string): EvaluationConfig {
    // Check for BASELINE_TESTS environment variable
    const baselineTests = process.env.BASELINE_TESTS ? parseInt(process.env.BASELINE_TESTS, 10) : 10;
    const evaluationMode = process.env.EVALUATION_MODE || 'quick';
    
    // Use full configuration if EVALUATION_MODE is 'full' or BASELINE_TESTS is set
    const problemsPerBenchmark = (evaluationMode === 'full' || process.env.BASELINE_TESTS) ? baselineTests : 10;
    
    return {
      benchmarks: ['humaneval', 'mbpp'],
      problemsPerBenchmark: problemsPerBenchmark, // Respect environment variables
      solutionConfig: {
        maxAttempts: evaluationMode === 'full' ? 3 : 1,
        timeoutMs: evaluationMode === 'full' ? 60000 : 30000,
        strategy: 'best-result',
      },
      validationConfig: {
        timeoutMs: 15000,
        maxRetries: 1,
      },
      calculatePassAtK: [1, 5],
      generateDetailedReports: true,
      exportResults: true,
      outputDirectory: outputDir,
      parallelProcessing: evaluationMode === 'full',
      maxConcurrentEvaluations: evaluationMode === 'full' ? 3 : 1,
      enableProgressReporting: true,
      saveIntermediateResults: true,
    };
  }

  /**
   * Create a comprehensive evaluation configuration
   */
  static createComprehensiveEvaluationConfig(outputDir: string): EvaluationConfig {
    return {
      benchmarks: ['swe-bench', 'quixbugs', 'fim', 'mbpp', 'humaneval'],
      solutionConfig: {
        maxAttempts: 3,
        timeoutMs: 60000,
        strategy: 'best-result',
        enableRuleBased: true,
        enablePatternBased: true,
        enableLLM: true,
      },
      validationConfig: {
        timeoutMs: 30000,
        maxRetries: 2,
        enableSandbox: true,
        validateSyntax: true,
        validateSemantics: true,
      },
      calculatePassAtK: [1, 5, 10],
      generateDetailedReports: true,
      exportResults: true,
      outputDirectory: outputDir,
      parallelProcessing: false,
      maxConcurrentEvaluations: 4,
      enableProgressReporting: true,
      saveIntermediateResults: true,
    };
  }
}

