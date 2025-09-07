#!/usr/bin/env node

/**
 * Project Golem Complete CLI - Command Line Interface for Complete Agentic System
 * 
 * Provides comprehensive command-line access to all Project Golem capabilities:
 * - Interactive error correction testing
 * - Benchmark testing and evaluation
 * - System performance monitoring
 * - Learning analytics and insights
 * - Manual testing interface
 */

import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import { AgenticSystem, CompleteAgenticConfig } from '../evaluation/AgenticSystem';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StepLexer } from '../utils/StepLexer';
import { LexerOptions } from '../utils/LexerOptions';

const program = new Command();

// Global system instance
let agenticSystem: AgenticSystem | null = null;

/**
 * Initialize the agentic system
 */
async function initializeSystem(options: any): Promise<AgenticSystem> {
  if (agenticSystem) {
    return agenticSystem;
  }

  console.log('🤖 Initializing Project Golem Complete Agentic System...');
  
  // Load Python 3.11 grammar
  const grammarPath = path.join(__dirname, '../../grammar/Python311.grammar');
  const grammar = new Grammar('Python311'); // Use core Grammar constructor
  
  // Initialize parser components
  const stepParser = new StepParser();
  const sourceContainer = {
    getSourceLines: () => [],
    getCount: () => 0,
    getLine: (fileName: string, lineNumber: number) => null,
  };
  const stepLexer = new StepLexer(stepParser, new LexerOptions(false, false), sourceContainer);
  
  // Configure system
  const config: Partial<CompleteAgenticConfig> = {
    enableFullIntegration: true,
    enableRealTimeOptimization: true,
    enableAdvancedPatternMatching: true,
    enableAdaptiveLearning: true,
    enableManualTesting: true,
    enableBenchmarkTesting: true,
    enablePerformanceProfiling: true,
    responseTimeTarget: options.timeout || 2000,
    memoryLimitMB: options.memory || 512,
    enableDetailedLogging: options.verbose || false,
    testDataPath: options.testData || './test-data',
  };
  
  // Create and initialize system
  agenticSystem = new AgenticSystem(grammar, stepParser, stepLexer, config);
  await agenticSystem.initialize();
  
  console.log('✅ Project Golem Complete Agentic System ready!');
  return agenticSystem;
}

/**
 * Test error correction interactively
 */
async function testErrorCorrection(errorCode: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  console.log('\\n🧪 INTERACTIVE ERROR CORRECTION TEST');
  console.log('=' .repeat(60));
  
  try {
    const result = await system.testError(errorCode, options.expected);
    
    if (options.output) {
      const outputData = {
        input: errorCode,
        expected: options.expected,
        result: result,
        timestamp: new Date().toISOString(),
      };
      
      fs.writeFileSync(options.output, JSON.stringify(outputData, null, 2));
      console.log(`\\n📄 Results saved to: ${options.output}`);
    }
    
  } catch (error) {
    console.error('❌ Error correction failed:', error);
    process.exit(1);
  }
}

/**
 * Step through correction process
 */
async function stepThroughCorrection(errorCode: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  try {
    const steps = await system.stepThroughCorrection(errorCode);
    
    if (options.output) {
      const outputData = {
        input: errorCode,
        steps: steps,
        timestamp: new Date().toISOString(),
      };
      
      fs.writeFileSync(options.output, JSON.stringify(outputData, null, 2));
      console.log(`\\n📄 Step analysis saved to: ${options.output}`);
    }
    
  } catch (error) {
    console.error('❌ Step-through analysis failed:', error);
    process.exit(1);
  }
}

/**
 * Compare correction approaches
 */
async function compareApproaches(errorCode: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  try {
    const comparison = await system.compareApproaches(errorCode);
    
    if (options.output) {
      const outputData = {
        input: errorCode,
        comparison: comparison,
        timestamp: new Date().toISOString(),
      };
      
      fs.writeFileSync(options.output, JSON.stringify(outputData, null, 2));
      console.log(`\\n📄 Approach comparison saved to: ${options.output}`);
    }
    
  } catch (error) {
    console.error('❌ Approach comparison failed:', error);
    process.exit(1);
  }
}

/**
 * Validate deterministic progression
 */
async function validateProgression(errorCode: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  try {
    const analysis = await system.validateDeterministicProgression(errorCode);
    
    if (options.output) {
      const outputData = {
        input: errorCode,
        analysis: analysis,
        timestamp: new Date().toISOString(),
      };
      
      fs.writeFileSync(options.output, JSON.stringify(outputData, null, 2));
      console.log(`\\n📄 Progression analysis saved to: ${options.output}`);
    }
    
  } catch (error) {
    console.error('❌ Progression validation failed:', error);
    process.exit(1);
  }
}

/**
 * Run benchmark tests
 */
async function runBenchmarks(testSuite: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  console.log('\\n🏁 RUNNING BENCHMARK TESTS');
  console.log('=' .repeat(60));
  
  try {
    const results = await system.runBenchmarkTests(testSuite);
    
    console.log('\\n📊 FINAL BENCHMARK SUMMARY:');
    console.log(`📋 Test Suite: ${results.benchmarkName}`);
    console.log(`🧪 Test Cases: ${results.testCases}`);
    console.log(`✅ Success Rate: ${(results.successRate * 100).toFixed(1)}%`);
    console.log(`⏱️  Average Response Time: ${results.averageResponseTime.toFixed(0)}ms`);
    console.log(`🎯 Deterministic Ratio: ${(results.deterministicRatio * 100).toFixed(1)}%`);
    console.log(`📈 Baseline Comparison: ${results.comparisonToBaseline > 0 ? '+' : ''}${(results.comparisonToBaseline * 100).toFixed(1)}%`);
    
    if (options.output) {
      fs.writeFileSync(options.output, JSON.stringify(results, null, 2));
      console.log(`\\n📄 Benchmark results saved to: ${options.output}`);
    }
    
    if (options.detailed) {
      console.log('\\n📋 DETAILED RESULTS:');
      for (const testCase of results.detailedResults) {
        console.log(`  ${testCase.success ? '✅' : '❌'} ${testCase.testId}: ${testCase.responseTime}ms (${testCase.approach})`);
      }
    }
    
  } catch (error) {
    console.error('❌ Benchmark testing failed:', error);
    process.exit(1);
  }
}

/**
 * Provide user feedback
 */
async function provideFeedback(correctionId: string, rating: number, comments: string, options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  try {
    await system.provideFeedback(
      correctionId,
      options.userId || 'cli-user',
      rating,
      comments,
      options.type || 'correction_quality',
    );
    
    console.log('✅ Feedback provided successfully');
    
  } catch (error) {
    console.error('❌ Failed to provide feedback:', error);
    process.exit(1);
  }
}

/**
 * Show system status
 */
async function showSystemStatus(options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  const status = system.getSystemStatus();
  const metrics = system.getPerformanceMetrics();
  const learning = system.getLearningAnalytics();
  
  console.log('\\n📊 SYSTEM STATUS REPORT');
  console.log('=' .repeat(60));
  
  console.log(`\\n🏥 Health Status: ${status.status.toUpperCase()}`);
  console.log(`⏱️  Uptime: ${status.uptime} seconds`);
  console.log(`🔧 Total Corrections: ${status.totalCorrections}`);
  
  console.log('\\n📈 Performance Metrics:');
  console.log(`  Average Response Time: ${metrics.averageResponseTime.toFixed(0)}ms`);
  console.log(`  Success Rate: ${(metrics.successRate * 100).toFixed(1)}%`);
  console.log(`  Deterministic Ratio: ${(metrics.deterministicRatio * 100).toFixed(1)}%`);
  console.log(`  Memory Usage: ${metrics.memoryUsage.toFixed(1)}MB`);
  console.log(`  Cache Hit Rate: ${(metrics.cacheHitRate * 100).toFixed(1)}%`);
  
  if (learning) {
    console.log('\\n🧠 Learning Analytics:');
    console.log(`  Total Learning Events: ${learning.totalLearningEvents}`);
    console.log(`  Learning Velocity: ${learning.learningVelocity.toFixed(2)} events/day`);
    console.log(`  Adaptation Success: ${(learning.adaptationSuccess * 100).toFixed(1)}%`);
  }
  
  console.log('\\n🔧 Component Status:');
  for (const [component, componentStatus] of status.componentStatus) {
    const statusIcon = componentStatus === 'online' ? '✅' : componentStatus === 'degraded' ? '⚠️' : '❌';
    console.log(`  ${statusIcon} ${component}: ${componentStatus}`);
  }
  
  if (status.recentErrors.length > 0) {
    console.log('\\n⚠️  Recent Errors:');
    for (const error of status.recentErrors.slice(-5)) {
      console.log(`  • ${error}`);
    }
  }
  
  if (options.output) {
    const statusData = {
      status,
      metrics,
      learning,
      timestamp: new Date().toISOString(),
    };
    
    fs.writeFileSync(options.output, JSON.stringify(statusData, null, 2));
    console.log(`\\n📄 Status report saved to: ${options.output}`);
  }
}

/**
 * Optimize system performance
 */
async function optimizeSystem(options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  console.log('\\n🚀 OPTIMIZING SYSTEM PERFORMANCE');
  console.log('=' .repeat(60));
  
  try {
    await system.optimizePerformance();
    
    const newMetrics = system.getPerformanceMetrics();
    console.log('\\n📈 Post-Optimization Metrics:');
    console.log(`  Average Response Time: ${newMetrics.averageResponseTime.toFixed(0)}ms`);
    console.log(`  Success Rate: ${(newMetrics.successRate * 100).toFixed(1)}%`);
    console.log(`  Deterministic Ratio: ${(newMetrics.deterministicRatio * 100).toFixed(1)}%`);
    console.log(`  Memory Usage: ${newMetrics.memoryUsage.toFixed(1)}MB`);
    
  } catch (error) {
    console.error('❌ System optimization failed:', error);
    process.exit(1);
  }
}

/**
 * Interactive mode
 */
async function interactiveMode(options: any): Promise<void> {
  const system = await initializeSystem(options);
  
  console.log('\\n🎮 INTERACTIVE MODE - Project Golem Complete Agentic System');
  console.log('=' .repeat(70));
  console.log('Commands:');
  console.log('  test <code>           - Test error correction');
  console.log('  step <code>           - Step through correction');
  console.log('  compare <code>        - Compare approaches');
  console.log('  validate <code>       - Validate progression');
  console.log('  feedback <id> <rating> <comments> - Provide feedback');
  console.log('  status                - Show system status');
  console.log('  optimize              - Optimize performance');
  console.log('  benchmark <suite>     - Run benchmarks');
  console.log('  help                  - Show this help');
  console.log('  exit                  - Exit interactive mode');
  console.log('');
  
  const readline = require('readline');
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: 'golem> ',
  });
  
  rl.prompt();
  
  rl.on('line', async (line: string) => {
    const args = line.trim().split(' ');
    const command = args[0];
    
    try {
      switch (command) {
        case 'test':
          if (args.length < 2) {
            console.log('Usage: test <code>');
          } else {
            const code = args.slice(1).join(' ');
            await system.testError(code);
          }
          break;
          
        case 'step':
          if (args.length < 2) {
            console.log('Usage: step <code>');
          } else {
            const code = args.slice(1).join(' ');
            await system.stepThroughCorrection(code);
          }
          break;
          
        case 'compare':
          if (args.length < 2) {
            console.log('Usage: compare <code>');
          } else {
            const code = args.slice(1).join(' ');
            await system.compareApproaches(code);
          }
          break;
          
        case 'validate':
          if (args.length < 2) {
            console.log('Usage: validate <code>');
          } else {
            const code = args.slice(1).join(' ');
            await system.validateDeterministicProgression(code);
          }
          break;
          
        case 'feedback':
          if (args.length < 4) {
            console.log('Usage: feedback <correctionId> <rating> <comments>');
          } else {
            await system.provideFeedback(args[1], 'cli-user', parseInt(args[2]), args.slice(3).join(' '));
          }
          break;
          
        case 'status':
          await showSystemStatus(options);
          break;
          
        case 'optimize':
          await system.optimizePerformance();
          console.log('✅ System optimization complete');
          break;
          
        case 'benchmark': {
          const suite = args[1] || 'python-errors';
          await system.runBenchmarkTests(suite);
          break;
        }
          
        case 'help':
          console.log('Available commands: test, step, compare, validate, feedback, status, optimize, benchmark, help, exit');
          break;
          
        case 'exit':
          console.log('👋 Goodbye!');
          await system.shutdown();
          rl.close();
          return;
          
        case '':
          break;
          
        default:
          console.log(`Unknown command: ${command}. Type 'help' for available commands.`);
      }
    } catch (error) {
      console.error(`❌ Command failed: ${error}`);
    }
    
    rl.prompt();
  });
  
  rl.on('close', async () => {
    if (system) {
      await system.shutdown();
    }
    process.exit(0);
  });
}

// CLI Command Definitions

program
  .name('golem-complete')
  .description('Project Golem Complete Agentic System CLI')
  .version('1.0.0');

// Global options
program
  .option('-v, --verbose', 'Enable verbose logging')
  .option('-t, --timeout <ms>', 'Response timeout in milliseconds', '2000')
  .option('-m, --memory <mb>', 'Memory limit in MB', '512')
  .option('--test-data <path>', 'Path to test data directory', './test-data');

// Test error correction
program
  .command('test <errorCode>')
  .description('Test error correction with the complete agentic system')
  .option('-e, --expected <correction>', 'Expected correction for validation')
  .option('-o, --output <file>', 'Save results to file')
  .action(testErrorCorrection);

// Step through correction
program
  .command('step <errorCode>')
  .description('Step through the correction process for debugging')
  .option('-o, --output <file>', 'Save step analysis to file')
  .action(stepThroughCorrection);

// Compare approaches
program
  .command('compare <errorCode>')
  .description('Compare different correction approaches')
  .option('-o, --output <file>', 'Save comparison to file')
  .action(compareApproaches);

// Validate progression
program
  .command('validate <errorCode>')
  .description('Validate deterministic progression')
  .option('-o, --output <file>', 'Save analysis to file')
  .action(validateProgression);

// Run benchmarks
program
  .command('benchmark [testSuite]')
  .description('Run benchmark tests')
  .option('-o, --output <file>', 'Save results to file')
  .option('-d, --detailed', 'Show detailed results')
  .action(runBenchmarks);

// Provide feedback
program
  .command('feedback <correctionId> <rating> <comments>')
  .description('Provide feedback for a correction')
  .option('-u, --user-id <id>', 'User ID', 'cli-user')
  .option('--type <type>', 'Feedback type', 'correction_quality')
  .action(provideFeedback);

// System status
program
  .command('status')
  .description('Show system status and metrics')
  .option('-o, --output <file>', 'Save status to file')
  .action(showSystemStatus);

// Optimize system
program
  .command('optimize')
  .description('Optimize system performance')
  .action(optimizeSystem);

// Interactive mode
program
  .command('interactive')
  .description('Start interactive mode')
  .action(interactiveMode);

// Error handling
program.on('command:*', () => {
  console.error('Invalid command: %s\\nSee --help for a list of available commands.', program.args.join(' '));
  process.exit(1);
});

// Parse command line arguments
if (process.argv.length === 2) {
  // No arguments provided, show help
  program.help();
} else {
  program.parse(process.argv);
}

// Handle uncaught errors
process.on('uncaughtException', async (error) => {
  console.error('❌ Uncaught exception:', error);
  if (agenticSystem) {
    await agenticSystem.shutdown();
  }
  process.exit(1);
});

process.on('unhandledRejection', async (reason, promise) => {
  console.error('❌ Unhandled rejection at:', promise, 'reason:', reason);
  if (agenticSystem) {
    await agenticSystem.shutdown();
  }
  process.exit(1);
});

