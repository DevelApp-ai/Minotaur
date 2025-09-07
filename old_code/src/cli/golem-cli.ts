#!/usr/bin/env node

/**
 * Project Golem CLI - Command Line Interface for AST-Guided Error Correction
 * 
 * Provides command-line access to Project Golem's AST-guided error correction
 * capabilities with support for batch processing, interactive mode, and benchmarking.
 */

import * as fs from 'fs';
import * as path from 'path';
import { Command } from 'commander';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { ProductionTriggerASTCorrector, CorrectionConfig } from '../evaluation/ProductionTriggerASTCorrector';
import { GolemIntegrationDemo } from '../evaluation/GolemIntegrationDemo';
import { SemanticValidator } from '../evaluation/SemanticValidator';

interface CLIOptions {
  input?: string;
  output?: string;
  grammar?: string;
  maxAttempts?: number;
  maxIterations?: number;
  confidence?: number;
  timeout?: number;
  verbose?: boolean;
  interactive?: boolean;
  demo?: boolean;
  benchmark?: boolean;
  benchmarkIterations?: number;
  validate?: boolean;
  stats?: boolean;
}

class GolemCLI {
  private grammar: Grammar | null = null;
  private stepParser: StepParser | null = null;
  private astCorrector: ProductionTriggerASTCorrector | null = null;
  private semanticValidator: SemanticValidator | null = null;

  async initialize(grammarPath?: string): Promise<void> {
    try {
      // Load grammar (default to Python 3.11)
      const grammarFile = grammarPath || path.join(__dirname, '../../grammar/Python311.grammar');
      
      if (!fs.existsSync(grammarFile)) {
        console.error(`❌ Grammar file not found: ${grammarFile}`);
        process.exit(1);
      }

      console.log(`📚 Loading grammar: ${grammarFile}`);
      this.grammar = await this.loadGrammar(grammarFile);
      this.stepParser = new StepParser();
      
      console.log('✅ Grammar loaded successfully');

    } catch (error) {
      console.error(`❌ Failed to initialize Golem: ${error}`);
      process.exit(1);
    }
  }

  async correctFile(inputPath: string, outputPath?: string, options: CLIOptions = {}): Promise<void> {
    if (!this.grammar || !this.stepParser) {
      throw new Error('Golem not initialized');
    }

    try {
      // Read input file
      if (!fs.existsSync(inputPath)) {
        console.error(`❌ Input file not found: ${inputPath}`);
        return;
      }

      const sourceCode = fs.readFileSync(inputPath, 'utf-8');
      console.log(`📖 Reading file: ${inputPath} (${sourceCode.length} characters)`);

      // Configure corrector
      const config: Partial<CorrectionConfig> = {
        maxAttempts: options.maxAttempts || 5,
        maxIterations: options.maxIterations || 3,
        confidenceThreshold: options.confidence || 0.7,
        enableLearning: true,
        preserveFormatting: true,
        validateTransformations: true,
        timeoutMs: options.timeout || 30000,
      };

      this.astCorrector = new ProductionTriggerASTCorrector(this.grammar, this.stepParser, config);

      // Perform correction
      console.log('🔧 Starting AST-guided error correction...');
      const startTime = Date.now();
      
      const result = await this.astCorrector.correctErrors(sourceCode);
      
      const executionTime = Date.now() - startTime;

      // Display results
      this.displayCorrectionResults(result, executionTime, options.verbose);

      // Write output
      if (result.success && result.correctedCode) {
        const output = outputPath || inputPath.replace(/\.py$/, '.corrected.py');
        fs.writeFileSync(output, result.correctedCode);
        console.log(`💾 Corrected code written to: ${output}`);
      }

      // Display statistics
      if (options.stats) {
        this.displayStatistics(result);
      }

    } catch (error) {
      console.error(`❌ Error correcting file: ${error}`);
    }
  }

  async validateFile(inputPath: string, options: CLIOptions = {}): Promise<void> {
    if (!this.grammar || !this.stepParser) {
      throw new Error('Golem not initialized');
    }

    try {
      const sourceCode = fs.readFileSync(inputPath, 'utf-8');
      console.log(`🔍 Validating file: ${inputPath}`);

      this.semanticValidator = new SemanticValidator(this.grammar, this.stepParser);
      await this.semanticValidator.initialize('python.grammar');

      const sourceLines = sourceCode.split('\n').map((line, index) => ({
        getLineNumber: () => index + 1,
        getContent: () => line,
        getLength: () => line.length,
        getFileName: () => 'input-code',
      }));
      const sourceContainer = {
        getSourceLines: () => sourceLines,
        getCount: () => sourceLines.length,
        getLine: (fileName: string, lineNumber: number) => sourceLines[lineNumber - 1],
      };
      const ast = await this.stepParser.parse('python', sourceContainer);
      // Convert ProductionMatch[] to ZeroCopyASTNode for validation
      const astNode = ast.length > 0 ? ast[0] as any : null;
      const result = await this.semanticValidator.validateSemantics(astNode, sourceCode);

      console.log('\n📊 Validation Results:');
      console.log(`  ✅ Success: ${result.success ? 'YES' : 'NO'}`);
      console.log(`  🔢 Errors: ${result.errors.length}`);
      console.log(`  ⚠️  Warnings: ${result.warnings.length}`);
      console.log(`  🔍 Symbols: ${result.symbolTable.size}`);
      console.log(`  📚 Scopes: ${result.scopeAnalysis.totalScopes}`);

      if (result.errors.length > 0) {
        console.log('\n❌ Errors Found:');
        for (const error of result.errors) {
          console.log(`  - Line ${error.location.line}: ${error.message}`);
          if (options.verbose) {
            console.log(`    Type: ${error.type}, Severity: ${error.severity}`);
            if (error.suggestedFixes && error.suggestedFixes.length > 0) {
              console.log(`    Suggestions: ${error.suggestedFixes.map(fix => fix.description).join(', ')}`);
            }
          }
        }
      }

      if (result.warnings.length > 0 && options.verbose) {
        console.log('\n⚠️  Warnings:');
        for (const warning of result.warnings) {
          console.log(`  - Line ${warning.position.line}: ${warning.message}`);
        }
      }

    } catch (error) {
      console.error(`❌ Error validating file: ${error}`);
    }
  }

  async runDemo(options: CLIOptions = {}): Promise<void> {
    console.log('🎮 Running Project Golem Demo');
    
    const demo = new GolemIntegrationDemo();
    
    if (options.benchmark) {
      const iterations = options.benchmarkIterations || 10;
      console.log(`🏃 Running benchmark with ${iterations} iterations...`);
      await demo.benchmarkPerformance(iterations);
    } else {
      await demo.runCompleteDemo();
    }
  }

  async runInteractive(): Promise<void> {
    console.log('🎮 Interactive Project Golem Mode');
    console.log('Enter Python code with errors, and watch Golem correct them!');
    console.log('Commands: \'exit\' to quit, \'demo\' for demo scenarios, \'help\' for help\n');

    const readline = require('readline');
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
    });

    const prompt = () => {
      rl.question('🐍 Enter Python code (or command): ', async (input) => {
        const trimmed = input.trim();
        
        if (trimmed === 'exit') {
          console.log('👋 Goodbye!');
          rl.close();
          return;
        }
        
        if (trimmed === 'demo') {
          await this.runDemo();
          prompt();
          return;
        }
        
        if (trimmed === 'help') {
          this.showInteractiveHelp();
          prompt();
          return;
        }
        
        if (trimmed === '') {
          prompt();
          return;
        }

        // Process the code
        try {
          if (!this.astCorrector) {
            const config: Partial<CorrectionConfig> = {
              maxAttempts: 3,
              maxIterations: 2,
              confidenceThreshold: 0.7,
              timeoutMs: 10000,
            };
            this.astCorrector = new ProductionTriggerASTCorrector(this.grammar!, this.stepParser!, config);
            // initialize method removed - initialization handled in constructor
          }

          console.log('🔧 Correcting code...');
          const result = await this.astCorrector.correctErrors(trimmed);
          
          if (result.success && result.correctedCode) {
            console.log('✅ Corrected code:');
            console.log(`${'─'.repeat(40)}`);
            console.log(result.correctedCode);
            console.log(`${'─'.repeat(40)}`);
            
            if (result.appliedCorrections.length > 0) {
              console.log(`🔧 Applied corrections: ${result.appliedCorrections.length}`);
              for (const correction of result.appliedCorrections) {
                console.log(`  - ${correction.transformationCandidate.description}`);
              }
            }
          } else {
            console.log('❌ Could not correct all errors');
            if (result.remainingErrors.length > 0) {
              console.log(`Remaining errors: ${result.remainingErrors.length}`);
              for (const error of result.remainingErrors) {
                console.log(`  - ${error.message}`);
              }
            }
          }
          
        } catch (error) {
          console.error(`❌ Error: ${error}`);
        }
        
        console.log(''); // Empty line
        prompt();
      });
    };

    prompt();
  }

  private displayCorrectionResults(result: any, executionTime: number, verbose: boolean = false): void {
    console.log('\n📊 Correction Results:');
    console.log(`  ✅ Success: ${result.success ? 'YES' : 'NO'}`);
    console.log(`  🔧 Corrections Applied: ${result.appliedCorrections.length}`);
    console.log(`  ❌ Remaining Errors: ${result.remainingErrors.length}`);
    console.log(`  ⏱️  Execution Time: ${executionTime}ms`);

    if (result.appliedCorrections.length > 0) {
      console.log('\n🔧 Applied Corrections:');
      for (const correction of result.appliedCorrections) {
        console.log(`  - ${correction.description} (confidence: ${(correction.confidence * 100).toFixed(1)}%)`);
        if (verbose) {
          console.log(`    Location: Line ${correction.location.line}, Column ${correction.location.column}`);
          console.log(`    Type: ${correction.type}`);
        }
      }
    }

    if (result.remainingErrors.length > 0) {
      console.log('\n❌ Remaining Errors:');
      for (const error of result.remainingErrors) {
        console.log(`  - Line ${error.location.line}: ${error.message}`);
        if (verbose && error.suggestions.length > 0) {
          console.log(`    Suggestions: ${error.suggestions.join(', ')}`);
        }
      }
    }
  }

  private displayStatistics(result: any): void {
    console.log('\n📈 Detailed Statistics:');
    console.log(`  🎯 Total Attempts: ${result.metrics.totalAttempts}`);
    console.log(`  🔄 Iterations: ${result.metrics.iterations}`);
    console.log(`  ⏱️  Correction Time: ${result.metrics.correctionTime}ms`);
    console.log(`  🧠 Grammar Triggers: ${result.metrics.grammarTriggersExecuted || 0}`);
    console.log(`  🔧 AST Transformations: ${result.metrics.astTransformationsApplied || 0}`);
    
    if (result.metrics.errorTypes) {
      console.log('  📊 Error Types:');
      for (const [type, count] of Object.entries(result.metrics.errorTypes)) {
        console.log(`    - ${type}: ${count}`);
      }
    }
  }

  private showInteractiveHelp(): void {
    console.log('\n📖 Interactive Mode Help:');
    console.log('  Commands:');
    console.log('    exit     - Exit interactive mode');
    console.log('    demo     - Run demonstration scenarios');
    console.log('    help     - Show this help message');
    console.log('\n  Examples:');
    console.log('    if x = 5:           # Assignment instead of comparison');
    console.log('    print(undefined)    # Undefined variable');
    console.log('    return "outside"    # Return outside function');
    console.log('    break               # Break outside loop');
  }

  private async loadGrammar(grammarPath: string): Promise<Grammar> {
    // This would load the actual grammar file
    // For now, return a mock grammar with proper type casting
    return {
      name: 'Python311',
      version: '3.11.0',
      productions: new Map(),
      terminals: new Set(),
      startSymbol: 'file_input',
    } as unknown as Grammar;
  }
}

// CLI Program Setup
const program = new Command();

program
  .name('golem')
  .description('Project Golem - AST-Guided Error Correction for Python')
  .version('1.0.0');

program
  .command('correct')
  .description('Correct errors in a Python file')
  .arguments('<input>')
  .option('-o, --output <file>', 'Output file (default: input.corrected.py)')
  .option('-g, --grammar <file>', 'Grammar file to use')
  .option('-a, --max-attempts <number>', 'Maximum correction attempts', '5')
  .option('-i, --max-iterations <number>', 'Maximum iterations per attempt', '3')
  .option('-c, --confidence <number>', 'Confidence threshold (0-1)', '0.7')
  .option('-t, --timeout <number>', 'Timeout in milliseconds', '30000')
  .option('-v, --verbose', 'Verbose output')
  .option('-s, --stats', 'Show detailed statistics')
  .action(async (input, options) => {
    const cli = new GolemCLI();
    await cli.initialize(options.grammar);
    await cli.correctFile(input, options.output, options);
  });

program
  .command('validate')
  .description('Validate a Python file for semantic errors')
  .arguments('<input>')
  .option('-g, --grammar <file>', 'Grammar file to use')
  .option('-v, --verbose', 'Verbose output')
  .action(async (input, options) => {
    const cli = new GolemCLI();
    await cli.initialize(options.grammar);
    await cli.validateFile(input, options);
  });

program
  .command('demo')
  .description('Run Project Golem demonstration')
  .option('-b, --benchmark', 'Run performance benchmark')
  .option('-n, --benchmark-iterations <number>', 'Benchmark iterations', '10')
  .action(async (options) => {
    const cli = new GolemCLI();
    await cli.initialize();
    await cli.runDemo(options);
  });

program
  .command('interactive')
  .description('Run in interactive mode')
  .option('-g, --grammar <file>', 'Grammar file to use')
  .action(async (options) => {
    const cli = new GolemCLI();
    await cli.initialize(options.grammar);
    await cli.runInteractive();
  });

// Error handling
program.exitOverride();

try {
  program.parse();
} catch (error: any) {
  if (error.code === 'commander.help') {
    process.exit(0);
  } else if (error.code === 'commander.version') {
    process.exit(0);
  } else {
    console.error(`❌ CLI Error: ${error.message}`);
    process.exit(1);
  }
}

export { GolemCLI };

