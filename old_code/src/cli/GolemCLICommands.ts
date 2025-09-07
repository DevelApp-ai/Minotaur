/**
 * GolemCLICommands - CLI Integration for Project Golem
 * 
 * Provides command-line interface for Project Golem's agentic correction system.
 * Integrates with existing MinotaurCLI to provide seamless user experience.
 * 
 * Commands:
 * - golem-correct: Correct code files using agentic system
 * - golem-validate: Run validation benchmarks
 * - golem-benchmark: Run comparative benchmarks
 * - golem-interactive: Interactive correction mode
 */

import { Command, Option } from 'commander';
import * as fs from 'fs/promises';
import * as path from 'path';
import { AgenticSystem } from '../evaluation/AgenticSystem';
import { MistralAPIClient } from '../evaluation/MistralAPIClient';
import { BenchmarkSolver } from '../evaluation/BenchmarkSolver';
import { ComparativeBenchmarkRunner } from '../evaluation/ComparativeBenchmarkRunner';
import { PromptFileProcessor } from './PromptFileProcessor';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';

export interface GolemCLIConfig {
  // System configuration
  grammarPath: string;
  mistralApiKey?: string;
  mistralApiBase?: string;
  
  // Default settings
  defaultOutputSuffix: string;
  defaultBenchmarkSuite: string;
  enableProgressLogging: boolean;
  enablePerformanceMetrics: boolean;
  
  // Interactive mode settings
  enableInteractiveMode: boolean;
  interactivePrompt: string;
  maxInteractiveAttempts: number;
}

/**
 * GolemCLICommands - Main CLI command handler
 */
export class GolemCLICommands {
  private agenticSystem: AgenticSystem | null = null;
  private mistralClient: MistralAPIClient | null = null;
  private enhancedSolver: BenchmarkSolver | null = null;
  private benchmarkRunner: ComparativeBenchmarkRunner | null = null;
  private promptProcessor: PromptFileProcessor | null = null;
  private config: GolemCLIConfig;

  constructor(config: Partial<GolemCLIConfig> = {}) {
    this.config = {
      grammarPath: '/home/ubuntu/Minotaur/grammar/Python311.grammar',
      mistralApiKey: process.env.MISTRAL_API_KEY,
      mistralApiBase: process.env.MISTRAL_API_BASE,
      defaultOutputSuffix: '_corrected',
      defaultBenchmarkSuite: 'python-errors',
      enableProgressLogging: true,
      enablePerformanceMetrics: true,
      enableInteractiveMode: true,
      interactivePrompt: 'golem> ',
      maxInteractiveAttempts: 5,
      ...config,
    };
  }

  /**
   * Initialize Golem CLI system
   */
  async initialize(): Promise<void> {
    console.log('🤖 Initializing Project Golem CLI...');
    
    try {
      // Initialize Mistral client
      if (!this.config.mistralApiKey) {
        throw new Error('MISTRAL_API_KEY environment variable not set');
      }
      
      this.mistralClient = new MistralAPIClient({
        apiKey: this.config.mistralApiKey,
        baseURL: this.config.mistralApiBase,
        rateLimit: {
          requestsPerMinute: 60,
          requestsPerHour: 3600,
          tokensPerMinute: 100000,
          tokensPerHour: 6000000,
          burstLimit: 10,
          adaptiveThrottling: true,
        },
        enableRequestQueuing: true,
        enableAdaptiveBackoff: true,
        enableCostTracking: false,
        logLevel: 'info',
      });
      
      // Load grammar
      const grammarContent = await fs.readFile(this.config.grammarPath, 'utf-8');
      const grammar = new Grammar(grammarContent);
      const stepParser = new StepParser();
      
      // Initialize agentic system
      this.agenticSystem = new AgenticSystem(
        grammar,
        stepParser,
        null, // stepLexer - not available, pass null
        {
          enableFullIntegration: true,
          enableRealTimeOptimization: true,
          enableAdvancedPatternMatching: true,
          enableAdaptiveLearning: true,
          maxConcurrentCorrections: 3,
          responseTimeTarget: 5000,
          memoryLimitMB: 512,
          cacheOptimization: true,
          enableManualTesting: false,
          enableBenchmarkTesting: false,
          enablePerformanceProfiling: false,
          testDataPath: '',
          agenticConfig: {},
          learningConfig: {},
          patternConfig: {},
          enableProductionMode: true,
          enableDetailedLogging: false,
          enableMetricsCollection: this.config.enablePerformanceMetrics,
        },
      );
      
      await this.agenticSystem.initialize();
      
      // Initialize enhanced solver
      this.enhancedSolver = new BenchmarkSolver(
        this.mistralClient,
        {
          enableProgressLogging: this.config.enableProgressLogging,
          enablePerformanceComparison: true,
        },
      );
      
      // Initialize benchmark runner
      this.benchmarkRunner = new ComparativeBenchmarkRunner(
        this.mistralClient,
      );
      
      // Initialize prompt processor
      this.promptProcessor = new PromptFileProcessor();
      
      console.log('✅ Project Golem CLI initialized successfully');
      
    } catch (error) {
      console.error('❌ Failed to initialize Golem CLI:', error);
      throw error;
    }
  }

  /**
   * Register CLI commands with Commander
   */
  registerCommands(program: Command): void {
    // Main golem command group
    const golemCommand = program
      .command('golem')
      .description('Project Golem agentic code correction system');

    // golem-correct command
    golemCommand
      .command('correct')
      .description('Correct code files using agentic system')
      .requiredOption('-f, --file <path>', 'Source file to correct')
      .option('-p, --prompt <path>', 'Prompt file for corrections')
      .option('-o, --output <path>', 'Output file path')
      .option('--interactive', 'Enable interactive correction mode')
      .option('--dry-run', 'Show corrections without applying them')
      .option('--verbose', 'Enable verbose logging')
      .action(async (options) => {
        await this.handleCorrectCommand(options);
      });

    // golem-validate command
    golemCommand
      .command('validate')
      .description('Run validation benchmarks')
      .option('-b, --benchmark <suite>', 'Benchmark suite to run', this.config.defaultBenchmarkSuite)
      .option('--compare', 'Compare against Mistral baseline')
      .option('--detailed', 'Show detailed results')
      .option('--save-report', 'Save benchmark report to file')
      .action(async (options) => {
        await this.handleValidateCommand(options);
      });

    // golem-benchmark command
    golemCommand
      .command('benchmark')
      .description('Run comparative benchmarks')
      .option('-s, --suite <name>', 'Test suite name', this.config.defaultBenchmarkSuite)
      .option('--parallel', 'Run parallel comparison with Mistral')
      .option('--iterations <count>', 'Number of benchmark iterations', '1')
      .option('--output <path>', 'Output directory for reports')
      .action(async (options) => {
        await this.handleBenchmarkCommand(options);
      });

    // golem-interactive command
    golemCommand
      .command('interactive')
      .description('Interactive correction mode')
      .option('-f, --file <path>', 'Initial file to load')
      .option('--max-attempts <count>', 'Maximum correction attempts', this.config.maxInteractiveAttempts.toString())
      .action(async (options) => {
        await this.handleInteractiveCommand(options);
      });

    // golem-status command
    golemCommand
      .command('status')
      .description('Show Golem system status and metrics')
      .option('--detailed', 'Show detailed system information')
      .action(async (options) => {
        await this.handleStatusCommand(options);
      });
  }

  /**
   * Handle golem-correct command
   */
  private async handleCorrectCommand(options: any): Promise<void> {
    if (!this.agenticSystem || !this.promptProcessor) {
      await this.initialize();
    }

    try {
      console.log(`🎯 Correcting file: ${options.file}`);
      
      // Read source file
      const sourceCode = await fs.readFile(options.file, 'utf-8');
      console.log(`📄 Loaded ${sourceCode.split('\n').length} lines of code`);
      
      // Process prompt file if provided
      let promptInstructions: any[] = [];
      if (options.prompt) {
        console.log(`📋 Processing prompt file: ${options.prompt}`);
        const processedPrompt = await this.promptProcessor!.processPromptFile(options.prompt);
        promptInstructions = processedPrompt.instructions || [];
        console.log(`📝 Loaded ${promptInstructions.length} prompt instructions`);
      }
      
      // Determine output path
      const outputPath = options.output || this.generateOutputPath(options.file);
      
      if (options.interactive) {
        // Interactive correction mode
        await this.runInteractiveCorrection(sourceCode, options.file, outputPath);
      } else {
        // Batch correction mode
        const result = await this.agenticSystem!.correctErrors(
          sourceCode,
          'cli-user',
          `correction-${Date.now()}`,
        );
        
        if (options.dryRun) {
          // Show corrections without applying
          console.log('\n🔍 CORRECTION PREVIEW:');
          console.log('=' .repeat(50));
          console.log(result.correctedCode || 'No corrections applied');
          console.log('=' .repeat(50));
          
          console.log('\n📊 CORRECTION METRICS:');
          console.log(`Success: ${result.success ? '✅' : '❌'}`);
          console.log(`Deterministic Ratio: ${(result.deterministicRatio * 100).toFixed(1)}%`);
          console.log(`Execution Time: ${result.totalExecutionTime}ms`);
          console.log(`Correction Steps: ${result.correctionSteps.length}`);
        } else {
          // Apply corrections
          const finalCode = result.correctedCode || sourceCode;
          await fs.writeFile(outputPath, finalCode);
          
          console.log(`✅ Correction complete: ${outputPath}`);
          console.log(`📊 Success: ${result.success ? '✅' : '❌'}`);
          console.log(`🎯 Deterministic: ${(result.deterministicRatio * 100).toFixed(1)}%`);
          console.log(`⚡ Time: ${result.totalExecutionTime}ms`);
        }
      }
      
    } catch (error) {
      console.error('❌ Correction failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle golem-validate command
   */
  private async handleValidateCommand(options: any): Promise<void> {
    if (!this.enhancedSolver || !this.benchmarkRunner) {
      await this.initialize();
    }

    try {
      console.log(`🧪 Running validation benchmark: ${options.benchmark}`);
      
      if (options.compare) {
        // Run comparative benchmark
        const report = await this.benchmarkRunner!.runComprehensiveBenchmark(options.benchmark);
        
        if (options.detailed) {
          // Detailed results already logged by benchmark runner
        } else {
          // Summary results
          console.log('\n📊 VALIDATION SUMMARY:');
          console.log(`Success Rate: ${(report.golemSuccessRate * 100).toFixed(1)}%`);
          console.log(`Improvement over Mistral: ${(report.improvementRatio * 100).toFixed(1)}%`);
          console.log(`Average Response Time: ${report.golemAvgTime.toFixed(0)}ms`);
        }
        
        if (options.saveReport) {
          console.log('💾 Report saved to benchmark_reports/ directory');
        }
      } else {
        // Run basic validation
        const testCases = await this.loadBasicTestCases(options.benchmark);
        let successCount = 0;
        
        for (const testCase of testCases) {
          try {
            const benchmarkProblem = {
              id: testCase.id,
              benchmark: 'custom' as any,
              title: testCase.id,
              description: testCase.code,
              prompt: testCase.code,
              language: 'python',
              difficulty: 'medium' as any,
              category: 'custom',
              testCases: [],
              expectedOutput: '',
              metadata: {},
            };
            const solution = await this.enhancedSolver!.solveProblem(benchmarkProblem);
            const success = solution.solutionCode !== testCase.code;
            if (success) {
successCount++;
}
            
            console.log(`${testCase.id}: ${success ? '✅' : '❌'}`);
          } catch (error) {
            console.log(`${testCase.id}: ❌ (${error})`);
          }
        }
        
        console.log(`\n📊 Results: ${successCount}/${testCases.length} (${(successCount / testCases.length * 100).toFixed(1)}%)`);
      }
      
    } catch (error) {
      console.error('❌ Validation failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle golem-benchmark command
   */
  private async handleBenchmarkCommand(options: any): Promise<void> {
    if (!this.benchmarkRunner) {
      await this.initialize();
    }

    try {
      console.log(`🏁 Running benchmark suite: ${options.suite}`);
      
      const iterations = parseInt(options.iterations);
      const reports = [];
      
      for (let i = 0; i < iterations; i++) {
        console.log(`\n🔄 Iteration ${i + 1}/${iterations}`);
        
        const report = await this.benchmarkRunner!.runComprehensiveBenchmark(options.suite);
        reports.push(report);
        
        if (iterations > 1) {
          console.log(`Iteration ${i + 1} Success Rate: ${(report.golemSuccessRate * 100).toFixed(1)}%`);
        }
      }
      
      // Calculate aggregate results if multiple iterations
      if (iterations > 1) {
        const avgSuccessRate = reports.reduce((sum, r) => sum + r.golemSuccessRate, 0) / iterations;
        const avgImprovement = reports.reduce((sum, r) => sum + r.improvementRatio, 0) / iterations;
        const avgTime = reports.reduce((sum, r) => sum + r.golemAvgTime, 0) / iterations;
        
        console.log('\n🏆 AGGREGATE RESULTS:');
        console.log(`Average Success Rate: ${(avgSuccessRate * 100).toFixed(1)}%`);
        console.log(`Average Improvement: ${(avgImprovement * 100).toFixed(1)}%`);
        console.log(`Average Response Time: ${avgTime.toFixed(0)}ms`);
      }
      
    } catch (error) {
      console.error('❌ Benchmark failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle golem-interactive command
   */
  private async handleInteractiveCommand(options: any): Promise<void> {
    if (!this.agenticSystem) {
      await this.initialize();
    }

    console.log('🤖 Starting Golem Interactive Mode');
    console.log('Type "help" for commands, "exit" to quit');
    
    let currentCode = '';
    
    // Load initial file if provided
    if (options.file) {
      currentCode = await fs.readFile(options.file, 'utf-8');
      console.log(`📄 Loaded: ${options.file}`);
    }
    
    // Interactive loop
    const readline = require('readline');
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
    });
    
    const askQuestion = (question: string): Promise<string> => {
      return new Promise((resolve) => {
        rl.question(question, resolve);
      });
    };
    
    // eslint-disable-next-line no-constant-condition
    while (true) {
      try {
        const input = await askQuestion(this.config.interactivePrompt);
        const command = input.trim().toLowerCase();
        
        if (command === 'exit' || command === 'quit') {
          break;
        } else if (command === 'help') {
          this.showInteractiveHelp();
        } else if (command === 'status') {
          this.showCurrentStatus(currentCode);
        } else if (command.startsWith('load ')) {
          const filePath = command.substring(5).trim();
          currentCode = await fs.readFile(filePath, 'utf-8');
          console.log(`📄 Loaded: ${filePath}`);
        } else if (command.startsWith('save ')) {
          const filePath = command.substring(5).trim();
          await fs.writeFile(filePath, currentCode);
          console.log(`💾 Saved: ${filePath}`);
        } else if (command === 'correct') {
          console.log('🎯 Applying corrections...');
          const result = await this.agenticSystem!.correctErrors(
            currentCode,
            'interactive-user',
            `interactive-${Date.now()}`,
          );
          
          if (result.success && result.correctedCode) {
            currentCode = result.correctedCode;
            console.log('✅ Corrections applied');
            console.log(`📊 Deterministic: ${(result.deterministicRatio * 100).toFixed(1)}%`);
          } else {
            console.log('❌ No corrections applied');
          }
        } else if (command === 'show') {
          console.log('\n📄 CURRENT CODE:');
          console.log('-' .repeat(40));
          console.log(currentCode);
          console.log('-' .repeat(40));
        } else {
          console.log('❓ Unknown command. Type "help" for available commands.');
        }
        
      } catch (error) {
        console.error('❌ Error:', error);
      }
    }
    
    rl.close();
    console.log('👋 Goodbye!');
  }

  /**
   * Handle golem-status command
   */
  private async handleStatusCommand(options: any): Promise<void> {
    if (!this.agenticSystem) {
      await this.initialize();
    }

    console.log('🤖 PROJECT GOLEM STATUS');
    console.log('=' .repeat(40));
    
    // System status
    console.log('📊 SYSTEM STATUS:');
    console.log(`  Agentic System: ${this.agenticSystem ? '✅ Ready' : '❌ Not initialized'}`);
    console.log(`  Mistral Client: ${this.mistralClient ? '✅ Ready' : '❌ Not initialized'}`);
    console.log(`  Enhanced Solver: ${this.enhancedSolver ? '✅ Ready' : '❌ Not initialized'}`);
    console.log(`  Benchmark Runner: ${this.benchmarkRunner ? '✅ Ready' : '❌ Not initialized'}`);
    
    // Configuration
    console.log('\n⚙️  CONFIGURATION:');
    console.log(`  Grammar Path: ${this.config.grammarPath}`);
    console.log(`  Progress Logging: ${this.config.enableProgressLogging ? '✅' : '❌'}`);
    console.log(`  Performance Metrics: ${this.config.enablePerformanceMetrics ? '✅' : '❌'}`);
    console.log(`  Interactive Mode: ${this.config.enableInteractiveMode ? '✅' : '❌'}`);
    
    if (options.detailed && this.enhancedSolver) {
      // Performance metrics
      const metrics = this.enhancedSolver.getPerformanceMetrics();
      console.log('\n📈 PERFORMANCE METRICS:');
      console.log(`  Total Solutions: ${metrics.totalSolutions}`);
      console.log(`  Successful Solutions: ${metrics.successfulSolutions}`);
      console.log(`  Success Rate: ${metrics.totalSolutions > 0 ? (metrics.successfulSolutions / metrics.totalSolutions * 100).toFixed(1) : 0}%`);
      console.log(`  Average Response Time: ${metrics.averageResponseTime.toFixed(0)}ms`);
      
      // Cache statistics
      const cacheStats = this.enhancedSolver.getCacheStatistics();
      console.log('\n💾 CACHE STATISTICS:');
      console.log(`  Cache Size: ${cacheStats.cacheSize}`);
      console.log(`  Cache Enabled: ${cacheStats.cacheEnabled ? '✅' : '❌'}`);
    }
  }

  /**
   * Run interactive correction mode
   */
  private async runInteractiveCorrection(
    sourceCode: string,
    inputFile: string,
    outputPath: string,
  ): Promise<void> {
    
    console.log('\n🤖 Interactive Correction Mode');
    console.log('Press Enter to apply corrections, Ctrl+C to cancel');
    
    const readline = require('readline');
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
    });
    
    await new Promise<void>((resolve) => {
      rl.question('Press Enter to continue...', () => {
        rl.close();
        resolve();
      });
    });
    
    const result = await this.agenticSystem!.correctErrors(
      sourceCode,
      'interactive-cli-user',
      `interactive-correction-${Date.now()}`,
    );
    
    console.log('\n📊 CORRECTION RESULTS:');
    console.log(`Success: ${result.success ? '✅' : '❌'}`);
    console.log(`Deterministic Ratio: ${(result.deterministicRatio * 100).toFixed(1)}%`);
    console.log(`Execution Time: ${result.totalExecutionTime}ms`);
    console.log(`Correction Steps: ${result.correctionSteps.length}`);
    
    if (result.success && result.correctedCode) {
      await fs.writeFile(outputPath, result.correctedCode);
      console.log(`✅ Corrected code saved to: ${outputPath}`);
    } else {
      console.log('❌ No corrections were applied');
    }
  }

  /**
   * Show interactive help
   */
  private showInteractiveHelp(): void {
    console.log('\n🤖 GOLEM INTERACTIVE COMMANDS:');
    console.log('  help          - Show this help message');
    console.log('  status        - Show current code status');
    console.log('  load <file>   - Load code from file');
    console.log('  save <file>   - Save current code to file');
    console.log('  correct       - Apply corrections to current code');
    console.log('  show          - Display current code');
    console.log('  exit/quit     - Exit interactive mode');
  }

  /**
   * Show current status in interactive mode
   */
  private showCurrentStatus(currentCode: string): void {
    console.log('\n📊 CURRENT STATUS:');
    console.log(`Code Length: ${currentCode.length} characters`);
    console.log(`Lines: ${currentCode.split('\n').length}`);
    console.log(`Has Content: ${currentCode.trim().length > 0 ? '✅' : '❌'}`);
  }

  /**
   * Generate output path for corrected file
   */
  private generateOutputPath(inputFile: string): string {
    const ext = path.extname(inputFile);
    const base = path.basename(inputFile, ext);
    const dir = path.dirname(inputFile);
    
    return path.join(dir, `${base}${this.config.defaultOutputSuffix}${ext}`);
  }

  /**
   * Load basic test cases for validation
   */
  private async loadBasicTestCases(suite: string): Promise<Array<{ id: string; code: string }>> {
    // Simple test cases for validation
    return [
      {
        id: 'test-001',
        code: 'if x = 5:\n    print("hello")',
      },
      {
        id: 'test-002',
        code: 'print(undefined_var)',
      },
      {
        id: 'test-003',
        code: 'result = math.sqrt(16)',
      },
    ];
  }
}

