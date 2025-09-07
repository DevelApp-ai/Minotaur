/**
 * Performance Benchmark Framework
 *
 * Provides comprehensive benchmarking capabilities to measure the impact
 * of all implemented optimizations and compare performance across different
 * configurations and input scenarios.
 */

import { StepParser } from '../../utils/StepParser';
import { StepLexer } from '../../utils/StepLexer';
import { LexerOptions } from '../../utils/LexerOptions';
import { IParserLexerSourceContainer, IParserLexerSourceLine } from '../../utils/IParserLexerSource';
import { IncrementalParser as _IncrementalParser } from '../incremental/IncrementalParser';
import { StreamingParser as _StreamingParser } from '../StreamingParser';
import { ParallelPathProcessor as _ParallelPathProcessor } from '../parallel/ParallelPathProcessor';
import { ContextCache as _ContextCache } from '../caching/ContextCache';
import { MemoizationCache as _MemoizationCache } from '../memoization/Memoization';
import { PathPredictor as _PathPredictor } from '../PathPredictor';
import { GrammarOptimizer as _GrammarOptimizer } from '../GrammarOptimizer';

export interface BenchmarkConfiguration {
    name: string;
    description: string;
    optimizations: OptimizationConfig;
    testCases: TestCase[];
    iterations: number;
    warmupIterations: number;
    timeoutMs?: number;
    memoryLimit?: number;
    enableProfiling?: boolean;
    enableComparison?: boolean;
    targetLanguages?: string[];
    testSuites?: any[];
}

export interface OptimizationConfig {
    incrementalParsing: boolean;
    objectPooling: boolean;
    regexPrecompilation: boolean;
    parallelProcessing: boolean;
    streamingParser: boolean;
    contextCaching: boolean;
    advancedMemoization: boolean;
    pathPrediction: boolean;
    grammarOptimization: boolean;
}

export interface TestCase {
    name: string;
    input: string;
    expectedComplexity: 'low' | 'medium' | 'high' | 'extreme';
    grammarType: string;
    description: string;
}

export interface BenchmarkResult {
    configuration: string;
    testCase: string;
    metrics: PerformanceMetrics;
    optimizationImpact: OptimizationImpact;
    memoryUsage: MemoryMetrics;
    timestamp: number;
}

export interface PerformanceMetrics {
    parseTime: number;
    lexTime: number;
    totalTime: number;
    tokensPerSecond: number;
    rulesPerSecond: number;
    throughput: number;
    latency: number;
    cpuUsage: number;
}

export interface OptimizationImpact {
    baselineTime: number;
    optimizedTime: number;
    speedupFactor: number;
    percentageImprovement: number;
    optimizationsUsed: string[];
}

export interface MemoryMetrics {
    peakMemoryUsage: number;
    averageMemoryUsage: number;
    memoryEfficiency: number;
    gcCollections: number;
    gcTime: number;
}

export class PerformanceBenchmark {
  private configurations: Map<string, BenchmarkConfiguration> = new Map();
  private results: BenchmarkResult[] = [];
  private baselineResults: Map<string, PerformanceMetrics> = new Map();

  constructor() {
    this.initializeStandardConfigurations();
  }

  /**
     * Runs a comprehensive benchmark suite
     */
  public async runBenchmarkSuite(): Promise<BenchmarkResult[]> {
    const allResults: BenchmarkResult[] = [];

    // eslint-disable-next-line no-console
    console.log('Starting comprehensive performance benchmark suite...');

    // Run baseline tests first
    await this.runBaselineTests();

    // Run optimized configurations
    for (const [configName, config] of this.configurations) {
    // eslint-disable-next-line no-console
      console.log(`Running benchmark configuration: ${configName}`);

      const configResults = await this.runConfiguration(config);
      allResults.push(...configResults);

      // Add delay between configurations to allow GC
      await this.delay(1000);
    }

    this.results.push(...allResults);
    return allResults;
  }

  /**
     * Runs a specific benchmark configuration
     */
  public async runConfiguration(config: BenchmarkConfiguration): Promise<BenchmarkResult[]> {
    const results: BenchmarkResult[] = [];

    for (const testCase of config.testCases) {
    // eslint-disable-next-line no-console
      console.log(`  Running test case: ${testCase.name}`);

      // Warmup iterations
      for (let i = 0; i < config.warmupIterations; i++) {
        await this.runSingleTest(config, testCase, false);
      }

      // Actual benchmark iterations
      const iterationResults: PerformanceMetrics[] = [];

      for (let i = 0; i < config.iterations; i++) {
        const metrics = await this.runSingleTest(config, testCase, true);
        iterationResults.push(metrics);
      }

      // Calculate average metrics
      const avgMetrics = this.calculateAverageMetrics(iterationResults);

      // Calculate optimization impact
      const baselineKey = `${testCase.name}_baseline`;
      const baseline = this.baselineResults.get(baselineKey);
      const optimizationImpact = baseline
        ? this.calculateOptimizationImpact(baseline, avgMetrics, config.optimizations)
        : this.createEmptyOptimizationImpact();

      // Create result
      const result: BenchmarkResult = {
        configuration: config.name,
        testCase: testCase.name,
        metrics: avgMetrics,
        optimizationImpact,
        memoryUsage: this.getCurrentMemoryMetrics(),
        timestamp: Date.now(),
      };

      results.push(result);
    }

    return results;
  }

  /**
     * Runs baseline tests for comparison
     */
  private async runBaselineTests(): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('Running baseline tests...');

    const baselineConfig = this.createBaselineConfiguration();

    for (const testCase of baselineConfig.testCases) {
      const iterationResults: PerformanceMetrics[] = [];

      // Warmup
      for (let i = 0; i < baselineConfig.warmupIterations; i++) {
        await this.runSingleTest(baselineConfig, testCase, false);
      }

      // Actual measurements
      for (let i = 0; i < baselineConfig.iterations; i++) {
        const metrics = await this.runSingleTest(baselineConfig, testCase, true);
        iterationResults.push(metrics);
      }

      const avgMetrics = this.calculateAverageMetrics(iterationResults);
      this.baselineResults.set(`${testCase.name}_baseline`, avgMetrics);
    }
  }

  /**
     * Runs a single test iteration
     */
  private async runSingleTest(
    config: BenchmarkConfiguration,
    testCase: TestCase,
    measureMemory: boolean,
  ): Promise<PerformanceMetrics> {
    const startTime = performance.now();
    let _memoryBefore = 0;
    let _memoryAfter = 0;

    if (measureMemory && (performance as any).measureUserAgentSpecificMemory) {
      _memoryBefore = await (performance as any).measureUserAgentSpecificMemory();
    }

    // Create parser with optimizations
    const parser = this.createOptimizedParser(config.optimizations);

    // Measure lexing time
    const lexStartTime = performance.now();
    const sourceLinesContainer = this.createSourceContainer(testCase.input);
    const lexerOptions = new LexerOptions();
    const lexer = new StepLexer(parser, lexerOptions, sourceLinesContainer);
    const tokens = Array.from(lexer.nextTokens());
    const lexTime = performance.now() - lexStartTime;

    // Measure parsing time
    const parseStartTime = performance.now();
    const results = await parser.parse('benchmark-grammar', sourceLinesContainer);
    const parseTime = performance.now() - parseStartTime;

    const totalTime = performance.now() - startTime;

    if (measureMemory && (performance as any).measureUserAgentSpecificMemory) {
      _memoryAfter = await (performance as any).measureUserAgentSpecificMemory();
    }

    // Calculate metrics
    const metrics: PerformanceMetrics = {
      parseTime,
      lexTime,
      totalTime,
      tokensPerSecond: tokens.length / (lexTime / 1000),
      rulesPerSecond: results.length / (parseTime / 1000),
      throughput: testCase.input.length / (totalTime / 1000), // Characters per second
      latency: totalTime,
      cpuUsage: this.estimateCpuUsage(totalTime),
    };

    return metrics;
  }

  /**
     * Creates an optimized parser based on configuration
     */
  private createOptimizedParser(optimizations: OptimizationConfig): StepParser {
    // This would create a parser with the specified optimizations enabled
    // For now, return a standard parser
    const parser = new StepParser();

    // Apply optimizations based on configuration
    if (optimizations.incrementalParsing) {
      // Enable incremental parsing
    }

    if (optimizations.objectPooling) {
      // Enable object pooling
    }

    if (optimizations.regexPrecompilation) {
      // Enable regex pre-compilation
    }

    // ... other optimizations

    return parser;
  }

  /**
     * Creates a token generator from an array of tokens
     */
  private *createTokenGenerator(tokens: any[]): Generator<any> {
    for (const token of tokens) {
      yield token;
    }
  }

  /**
     * Calculates average metrics from multiple iterations
     */
  private calculateAverageMetrics(results: PerformanceMetrics[]): PerformanceMetrics {
    const count = results.length;

    return {
      parseTime: results.reduce((sum, r) => sum + r.parseTime, 0) / count,
      lexTime: results.reduce((sum, r) => sum + r.lexTime, 0) / count,
      totalTime: results.reduce((sum, r) => sum + r.totalTime, 0) / count,
      tokensPerSecond: results.reduce((sum, r) => sum + r.tokensPerSecond, 0) / count,
      rulesPerSecond: results.reduce((sum, r) => sum + r.rulesPerSecond, 0) / count,
      throughput: results.reduce((sum, r) => sum + r.throughput, 0) / count,
      latency: results.reduce((sum, r) => sum + r.latency, 0) / count,
      cpuUsage: results.reduce((sum, r) => sum + r.cpuUsage, 0) / count,
    };
  }

  /**
     * Calculates optimization impact
     */
  private calculateOptimizationImpact(
    baseline: PerformanceMetrics,
    optimized: PerformanceMetrics,
    optimizations: OptimizationConfig,
  ): OptimizationImpact {
    const speedupFactor = baseline.totalTime / optimized.totalTime;
    const percentageImprovement = ((baseline.totalTime - optimized.totalTime) / baseline.totalTime) * 100;

    const optimizationsUsed = Object.entries(optimizations)
      .filter(([_key, value]) => value)
      .map(([key]) => key);

    return {
      baselineTime: baseline.totalTime,
      optimizedTime: optimized.totalTime,
      speedupFactor,
      percentageImprovement,
      optimizationsUsed,
    };
  }

  /**
     * Creates empty optimization impact for baseline tests
     */
  private createEmptyOptimizationImpact(): OptimizationImpact {
    return {
      baselineTime: 0,
      optimizedTime: 0,
      speedupFactor: 1,
      percentageImprovement: 0,
      optimizationsUsed: [],
    };
  }

  /**
     * Gets current memory metrics
     */
  private getCurrentMemoryMetrics(): MemoryMetrics {
    // Simplified memory metrics - would use actual memory measurement APIs
    return {
      peakMemoryUsage: 0,
      averageMemoryUsage: 0,
      memoryEfficiency: 1,
      gcCollections: 0,
      gcTime: 0,
    };
  }

  /**
     * Estimates CPU usage based on execution time
     */
  private estimateCpuUsage(executionTime: number): number {
    // Simple estimation - would use actual CPU monitoring in production
    return Math.min(100, executionTime / 10);
  }

  /**
     * Initializes standard benchmark configurations
     */
  private initializeStandardConfigurations(): void {
    // All optimizations enabled
    this.configurations.set('all_optimizations', {
      name: 'All Optimizations',
      description: 'All performance optimizations enabled',
      optimizations: {
        incrementalParsing: true,
        objectPooling: true,
        regexPrecompilation: true,
        parallelProcessing: true,
        streamingParser: true,
        contextCaching: true,
        advancedMemoization: true,
        pathPrediction: true,
        grammarOptimization: true,
      },
      testCases: this.createStandardTestCases(),
      iterations: 10,
      warmupIterations: 3,
    });

    // High-priority optimizations only
    this.configurations.set('high_priority', {
      name: 'High Priority Optimizations',
      description: 'Only high-impact optimizations enabled',
      optimizations: {
        incrementalParsing: true,
        objectPooling: true,
        regexPrecompilation: true,
        parallelProcessing: false,
        streamingParser: false,
        contextCaching: false,
        advancedMemoization: false,
        pathPrediction: false,
        grammarOptimization: false,
      },
      testCases: this.createStandardTestCases(),
      iterations: 10,
      warmupIterations: 3,
    });

    // Memory optimizations focus
    this.configurations.set('memory_optimized', {
      name: 'Memory Optimizations',
      description: 'Focus on memory efficiency optimizations',
      optimizations: {
        incrementalParsing: false,
        objectPooling: true,
        regexPrecompilation: false,
        parallelProcessing: false,
        streamingParser: true,
        contextCaching: true,
        advancedMemoization: false,
        pathPrediction: false,
        grammarOptimization: false,
      },
      testCases: this.createLargeInputTestCases(),
      iterations: 5,
      warmupIterations: 2,
    });
  }

  /**
     * Creates baseline configuration (no optimizations)
     */
  private createBaselineConfiguration(): BenchmarkConfiguration {
    return {
      name: 'Baseline',
      description: 'No optimizations enabled',
      optimizations: {
        incrementalParsing: false,
        objectPooling: false,
        regexPrecompilation: false,
        parallelProcessing: false,
        streamingParser: false,
        contextCaching: false,
        advancedMemoization: false,
        pathPrediction: false,
        grammarOptimization: false,
      },
      testCases: this.createStandardTestCases(),
      iterations: 10,
      warmupIterations: 3,
    };
  }

  /**
     * Creates standard test cases
     */
  private createStandardTestCases(): TestCase[] {
    return [
      {
        name: 'simple_json',
        input: '{"name": "test", "value": 123, "nested": {"array": [1, 2, 3]}}',
        expectedComplexity: 'low',
        grammarType: 'JSON',
        description: 'Simple JSON object with nested structure',
      },
      {
        name: 'arithmetic_expression',
        input: '(3 + 4) * 2 - 5 / (1 + 1)',
        expectedComplexity: 'medium',
        grammarType: 'Arithmetic',
        description: 'Complex arithmetic expression with precedence',
      },
      {
        name: 'embedded_html',
    // eslint-disable-next-line no-console
        input: '<html><head><style>body { color: red; }</style></head><body><script>console.log("test");</script></body></html>',
        expectedComplexity: 'high',
        grammarType: 'HTMLEmbedded',
        description: 'HTML with embedded CSS and JavaScript',
      },
    ];
  }

  /**
     * Creates test cases with large inputs for memory testing
     */
  private createLargeInputTestCases(): TestCase[] {
    const largeJson = JSON.stringify({
      data: Array.from({length: 1000}, (_, i) => ({
        id: i,
        name: `item_${i}`,
        values: Array.from({length: 10}, (_, j) => j * i),
      })),
    });

    return [
      {
        name: 'large_json',
        input: largeJson,
        expectedComplexity: 'extreme',
        grammarType: 'JSON',
        description: 'Large JSON with 1000 objects and nested arrays',
      },
    ];
  }

  /**
     * Utility method to add delay
     */
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Creates a source container from input text for lexer initialization
   */
  private createSourceContainer(input: string): IParserLexerSourceContainer {
    const lines = input.split('\n');
    const sourceLines: IParserLexerSourceLine[] = lines.map((content, index) => ({
      getContent: () => content,
      getLength: () => content.length,
      getLineNumber: () => index + 1,
      getFileName: () => 'benchmark-input',
    }));

    return {
      getSourceLines: () => sourceLines,
      getCount: () => sourceLines.length,
      getLine: (_fileName: string, lineNumber: number) => sourceLines[lineNumber - 1],
    };
  }

  /**
     * Gets all benchmark results
     */
  public getResults(): BenchmarkResult[] {
    return [...this.results];
  }

  /**
     * Generates a performance report
     */
  public generateReport(): string {
    const report = ['Performance Benchmark Report', '='.repeat(30), ''];

    // Group results by configuration
    const resultsByConfig = new Map<string, BenchmarkResult[]>();
    for (const result of this.results) {
      if (!resultsByConfig.has(result.configuration)) {
        resultsByConfig.set(result.configuration, []);
      }
            resultsByConfig.get(result.configuration)!.push(result);
    }

    // Generate report for each configuration
    for (const [configName, configResults] of resultsByConfig) {
      report.push(`Configuration: ${configName}`);
      report.push('-'.repeat(20));

      for (const result of configResults) {
        report.push(`Test Case: ${result.testCase}`);
        report.push(`  Total Time: ${result.metrics.totalTime.toFixed(2)}ms`);
        report.push(`  Parse Time: ${result.metrics.parseTime.toFixed(2)}ms`);
        report.push(`  Lex Time: ${result.metrics.lexTime.toFixed(2)}ms`);
        report.push(`  Throughput: ${result.metrics.throughput.toFixed(0)} chars/sec`);

        if (result.optimizationImpact.speedupFactor > 1) {
          report.push(`  Speedup: ${result.optimizationImpact.speedupFactor.toFixed(2)}x`);
          report.push(`  Improvement: ${result.optimizationImpact.percentageImprovement.toFixed(1)}%`);
        }

        report.push('');
      }
    }

    return report.join('\n');
  }
}

