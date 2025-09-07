#!/usr/bin/env node

/**
 * Golem Evaluation CLI Runner
 *
 * Command-line interface for running Golem evaluations against coding benchmarks.
 * This script provides the missing npm scripts that the PowerShell setup expects.
 */

import { GolemEvaluationRunner, EvaluationConfig } from '../evaluation/GolemEvaluationRunner';
import { promises as fs } from 'fs';
import * as path from 'path';

interface CLIOptions {
  config?: string;
  benchmarks?: string[];
  problems?: number;
  output?: string;
  resume?: boolean;
  verbose?: boolean;
  dryRun?: boolean;
}

class EvaluationCLI {
  private runner: GolemEvaluationRunner;

  constructor() {
    this.runner = new GolemEvaluationRunner();
  }

  /**
   * Load evaluation configuration from file or create default
   */
  private async loadConfig(configPath?: string): Promise<EvaluationConfig> {
    // Check environment variables first
    const baselineTests = process.env.BASELINE_TESTS ? parseInt(process.env.BASELINE_TESTS, 10) : null;
    const evaluationMode = process.env.EVALUATION_MODE || 'default';
    
    const defaultConfig: EvaluationConfig = {
      benchmarks: ['humaneval', 'mbpp', 'swe-bench', 'quixbugs', 'fim'],
      problemsPerBenchmark: baselineTests, // Respect BASELINE_TESTS environment variable
      solutionConfig: {
        maxAttempts: evaluationMode === 'full' ? 3 : 3,
        timeoutMs: evaluationMode === 'full' ? 300000 : 300000, // 5 minutes per problem
        enableRuleBased: true,
        enablePatternBased: true,
        enableLLM: true,
        strategy: 'quality',
        maxCostPerProblem: 1.0,
        minConfidence: 0.7,
        parallelGeneration: true,
        retryOnFailure: true,
      },
      validationConfig: {
        timeoutMs: 60000, // 1 minute per validation
        memoryLimitMB: 512,
        maxRetries: 2,
        enableSandbox: true,
        validateSyntax: true,
        validateSemantics: true,
        runPerformanceTests: false,
        captureOutput: true,
        parallelValidation: true,
        strictMode: false,
      },
      calculatePassAtK: [1, 5, 10],
      generateDetailedReports: true,
      exportResults: true,
      outputDirectory: './evaluation-results',
      parallelProcessing: evaluationMode === 'full',
      maxConcurrentEvaluations: evaluationMode === 'full' ? 4 : 2,
      enableProgressReporting: true,
      saveIntermediateResults: true,
    };

    if (configPath && await this.fileExists(configPath)) {
      try {
        const configContent = await fs.readFile(configPath, 'utf-8');
        const userConfig = JSON.parse(configContent);
        const mergedConfig = { ...defaultConfig, ...userConfig };
        
        // Environment variables override file config
        if (baselineTests !== null) {
          mergedConfig.problemsPerBenchmark = baselineTests;
        }
        
        return mergedConfig;
      } catch (error) {
    // eslint-disable-next-line no-console
        console.warn(`Warning: Could not load config from ${configPath}, using defaults`);
    // eslint-disable-next-line no-console
        console.warn(`Error: ${error instanceof Error ? error.message : 'Unknown error'}`);
      }
    }

    return defaultConfig;
  }

  /**
   * Check if file exists
   */
  private async fileExists(filePath: string): Promise<boolean> {
    try {
      await fs.access(filePath);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Run full evaluation
   */
  async runFullEvaluation(options: CLIOptions = {}): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('🚀 Starting Golem Full Evaluation');
    // eslint-disable-next-line no-console
    console.log('==================================');

    try {
      // Load configuration
      const config = await this.loadConfig(options.config);

      // Apply CLI overrides
      if (options.benchmarks) {
        config.benchmarks = options.benchmarks;
      }

      if (options.problems) {
        config.problemsPerBenchmark = options.problems;
      }

      if (options.output) {
        config.outputDirectory = options.output;
      }

      if (options.verbose) {
        config.enableProgressReporting = true;
      }

      // Create output directory
      await fs.mkdir(config.outputDirectory, { recursive: true });

    // eslint-disable-next-line no-console
      console.log('📋 Configuration:');
    // eslint-disable-next-line no-console
      console.log(`   Benchmarks: ${config.benchmarks.join(', ')}`);
    // eslint-disable-next-line no-console
      console.log(`   Problems per benchmark: ${config.problemsPerBenchmark || 'all'}`);
    // eslint-disable-next-line no-console
      console.log(`   Output directory: ${config.outputDirectory}`);
    // eslint-disable-next-line no-console
      console.log(`   Parallel processing: ${config.parallelProcessing}`);
    // eslint-disable-next-line no-console
      console.log('');

      if (options.dryRun) {
    // eslint-disable-next-line no-console
        console.log('🔍 Dry run mode - configuration validated successfully');
        return;
      }

      // Initialize the evaluation runner
    // eslint-disable-next-line no-console
      console.log('🔧 Initializing evaluation system...');
      await this.runner.initialize();

      // Run evaluation
      const startTime = Date.now();
      const results = await this.runner.runEvaluation(config);
      const duration = Date.now() - startTime;

    // eslint-disable-next-line no-console
      console.log('');
    // eslint-disable-next-line no-console
      console.log('✅ Evaluation completed successfully!');
    // eslint-disable-next-line no-console
      console.log(`⏱️  Total time: ${Math.round(duration / 1000)}s`);
    // eslint-disable-next-line no-console
      console.log(`📊 Results saved to: ${config.outputDirectory}`);

      // Print summary
      if (results.results?.overallStats) {
    // eslint-disable-next-line no-console
        console.log('');
    // eslint-disable-next-line no-console
        console.log('📈 Summary:');
    // eslint-disable-next-line no-console
        console.log(`   Total problems: ${results.results.overallStats.totalProblems}`);
    // eslint-disable-next-line no-console
        console.log(`   Total solutions: ${results.results.overallStats.totalSolutions}`);
    // eslint-disable-next-line no-console
        console.log(`   Success rate: ${(results.results.overallStats.overallSuccessRate * 100).toFixed(1)}%`);
      }

    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('❌ Evaluation failed:');
    // eslint-disable-next-line no-console
      console.error(error instanceof Error ? error.message : 'Unknown error');
      process.exit(1);
    }
  }

  /**
   * Resume evaluation from checkpoint
   */
  async resumeEvaluation(options: CLIOptions = {}): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('🔄 Resuming Golem Evaluation');
    // eslint-disable-next-line no-console
    console.log('============================');

    try {
      const config = await this.loadConfig(options.config);

      if (options.output) {
        config.outputDirectory = options.output;
      }

    // eslint-disable-next-line no-console
      console.log(`📂 Looking for checkpoint in: ${config.outputDirectory}`);

      // Check for existing checkpoint
      const checkpointPath = path.join(config.outputDirectory, 'checkpoint.json');
      if (!await this.fileExists(checkpointPath)) {
    // eslint-disable-next-line no-console
        console.log('⚠️  No checkpoint found, starting fresh evaluation');
        return this.runFullEvaluation(options);
      }

    // eslint-disable-next-line no-console
      console.log('✅ Checkpoint found, resuming...');

      // For now, just restart evaluation since resumeEvaluation doesn't exist yet
      // Note: Proper checkpoint resumption requires implementation in GolemEvaluationRunner
    // eslint-disable-next-line no-console
      console.log('⚠️  Resume functionality not yet implemented, starting fresh evaluation');
      return this.runFullEvaluation(options);

    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('❌ Resume failed:');
    // eslint-disable-next-line no-console
      console.error(error instanceof Error ? error.message : 'Unknown error');
      process.exit(1);
    }
  }

  /**
   * Show evaluation status
   */
  async showStatus(options: CLIOptions = {}): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('📊 Golem Evaluation Status');
    // eslint-disable-next-line no-console
    console.log('==========================');

    try {
      const config = await this.loadConfig(options.config);
      const outputDir = options.output || config.outputDirectory;

      // Check for running evaluation
      const statusPath = path.join(outputDir, 'status.json');
      const checkpointPath = path.join(outputDir, 'checkpoint.json');
      const resultsPath = path.join(outputDir, 'results.json');

      if (await this.fileExists(statusPath)) {
        const status = JSON.parse(await fs.readFile(statusPath, 'utf-8'));
    // eslint-disable-next-line no-console
        console.log(`🏃 Status: ${status.status}`);
    // eslint-disable-next-line no-console
        console.log(`📈 Progress: ${status.progress.problemsProcessed} problems processed`);
    // eslint-disable-next-line no-console
        console.log(`⏱️  Started: ${new Date(status.startTime).toLocaleString()}`);

        if (status.progress.currentBenchmark) {
    // eslint-disable-next-line no-console
          console.log(`🎯 Current benchmark: ${status.progress.currentBenchmark}`);
        }

        if (status.progress.currentPhase) {
    // eslint-disable-next-line no-console
          console.log(`🔄 Current phase: ${status.progress.currentPhase}`);
        }
      } else if (await this.fileExists(checkpointPath)) {
    // eslint-disable-next-line no-console
        console.log('⏸️  Status: Paused (checkpoint available)');
    // eslint-disable-next-line no-console
        console.log('💡 Use --resume to continue evaluation');
      } else if (await this.fileExists(resultsPath)) {
    // eslint-disable-next-line no-console
        console.log('✅ Status: Completed');
        const results = JSON.parse(await fs.readFile(resultsPath, 'utf-8'));
        if (results.results?.overallStats) {
          // eslint-disable-next-line max-len
    // eslint-disable-next-line no-console
          console.log(`📊 Success rate: ${(results.results.overallStats.overallSuccessRate * 100).toFixed(1)}%`);
    // eslint-disable-next-line no-console
          // eslint-disable-next-line max-len
          console.log(`🎯 Problems solved: ${results.results.overallStats.totalSolutions}/${results.results.overallStats.totalProblems}`);
        }
      } else {
    // eslint-disable-next-line no-console
        console.log('💤 Status: No evaluation found');
    // eslint-disable-next-line no-console
        console.log('💡 Use npm run eval:full to start evaluation');
      }

    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('❌ Status check failed:');
    // eslint-disable-next-line no-console
      console.error(error instanceof Error ? error.message : 'Unknown error');
    }
  }
}

// CLI interface
async function main() {
  const args = process.argv.slice(2);
  const command = args[0];

  const cli = new EvaluationCLI();

  // Parse options
  const options: CLIOptions = {};
  for (let i = 1; i < args.length; i++) {
    const arg = args[i];
    if (arg === '--config' && i + 1 < args.length) {
      options.config = args[++i];
    } else if (arg === '--benchmarks' && i + 1 < args.length) {
      options.benchmarks = args[++i].split(',');
    } else if (arg === '--problems' && i + 1 < args.length) {
      options.problems = parseInt(args[++i]);
    } else if (arg === '--output' && i + 1 < args.length) {
      options.output = args[++i];
    } else if (arg === '--resume') {
      options.resume = true;
    } else if (arg === '--verbose') {
      options.verbose = true;
    } else if (arg === '--dry-run') {
      options.dryRun = true;
    }
  }

  switch (command) {
    case 'full':
      await cli.runFullEvaluation(options);
      break;

    case 'resume':
      await cli.resumeEvaluation(options);
      break;

    case 'status':
      await cli.showStatus(options);
      break;

    case 'help':
    case '--help':
    case '-h':
    // eslint-disable-next-line no-console
      console.log('Golem Evaluation CLI');
    // eslint-disable-next-line no-console
      console.log('');
    // eslint-disable-next-line no-console
      console.log('Usage:');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:full     - Run full evaluation');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:resume   - Resume from checkpoint');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:status   - Show evaluation status');
    // eslint-disable-next-line no-console
      console.log('');
    // eslint-disable-next-line no-console
      console.log('Options:');
    // eslint-disable-next-line no-console
      console.log('  --config <file>       - Configuration file path');
    // eslint-disable-next-line no-console
      console.log('  --benchmarks <list>   - Comma-separated benchmark list');
    // eslint-disable-next-line no-console
      console.log('  --problems <number>   - Limit problems per benchmark');
    // eslint-disable-next-line no-console
      console.log('  --output <dir>        - Output directory');
    // eslint-disable-next-line no-console
      console.log('  --resume              - Resume from checkpoint');
    // eslint-disable-next-line no-console
      console.log('  --verbose             - Verbose output');
    // eslint-disable-next-line no-console
      console.log('  --dry-run             - Validate configuration only');
    // eslint-disable-next-line no-console
      console.log('');
    // eslint-disable-next-line no-console
      console.log('Examples:');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:full -- --benchmarks humaneval,mbpp --problems 10');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:resume -- --output ./my-results');
    // eslint-disable-next-line no-console
      console.log('  npm run eval:status -- --verbose');
      break;

    default:
    // eslint-disable-next-line no-console
      console.error('❌ Unknown command:', command);
    // eslint-disable-next-line no-console
      console.log('💡 Use "npm run eval:help" for usage information');
      process.exit(1);
  }
}

// Run CLI if called directly
if (require.main === module) {
  main().catch(error => {
    // eslint-disable-next-line no-console
    console.error('❌ CLI error:', error);
    process.exit(1);
  });
}

export { EvaluationCLI };

