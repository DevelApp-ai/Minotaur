/**
 * MinotaurCLIIntegration - Integration with Existing MinotaurCLI
 * 
 * Integrates Project Golem commands with the existing MinotaurCLI system
 * to provide seamless user experience and maintain backward compatibility.
 */

import { Command } from 'commander';
import { GolemCLICommands } from './GolemCLICommands';
import { AgenticSystem } from '../evaluation/AgenticSystem';
import { MistralAPIClient } from '../evaluation/MistralAPIClient';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StepLexer } from '../utils/StepLexer';
import { LexerOptions } from '../utils/LexerOptions';
import * as fs from 'fs/promises';

export interface MinotaurCLIConfig {
  // Existing MinotaurCLI configuration
  grammarPath: string;
  outputDir: string;
  verbose: boolean;
  
  // Project Golem extensions
  enableGolemCommands: boolean;
  mistralApiKey?: string;
  mistralApiBase?: string;
  golemCacheDir?: string;
  
  // Integration settings
  enableLegacyCompatibility: boolean;
  enableProgressReporting: boolean;
  enablePerformanceMetrics: boolean;
}

/**
 * MinotaurCLIIntegration - Main integration class
 */
export class MinotaurCLIIntegration {
  private config: MinotaurCLIConfig;
  private golemCommands: GolemCLICommands | null = null;
  private agenticSystem: AgenticSystem | null = null;
  private isInitialized: boolean = false;

  constructor(config: Partial<MinotaurCLIConfig> = {}) {
    this.config = {
      grammarPath: '/home/ubuntu/Minotaur/grammar/Python311.grammar',
      outputDir: './output',
      verbose: false,
      enableGolemCommands: true,
      mistralApiKey: process.env.MISTRAL_API_KEY,
      mistralApiBase: process.env.MISTRAL_API_BASE,
      golemCacheDir: '/tmp/golem-cache',
      enableLegacyCompatibility: true,
      enableProgressReporting: true,
      enablePerformanceMetrics: true,
      ...config,
    };
  }

  /**
   * Initialize the integration system
   */
  async initialize(): Promise<void> {
    if (this.isInitialized) {
      return;
    }

    try {
      console.log('🤖 Initializing Minotaur CLI with Project Golem integration...');
      
      if (this.config.enableGolemCommands) {
        // Initialize Golem CLI commands
        this.golemCommands = new GolemCLICommands({
          grammarPath: this.config.grammarPath,
          mistralApiKey: this.config.mistralApiKey,
          mistralApiBase: this.config.mistralApiBase,
          enableProgressLogging: this.config.enableProgressReporting,
          enablePerformanceMetrics: this.config.enablePerformanceMetrics,
        });
        
        await this.golemCommands.initialize();
        console.log('✅ Project Golem CLI commands initialized');
      }
      
      // Create cache directory if needed
      if (this.config.golemCacheDir) {
        await fs.mkdir(this.config.golemCacheDir, { recursive: true });
      }
      
      this.isInitialized = true;
      console.log('✅ Minotaur CLI integration complete');
      
    } catch (error) {
      console.error('❌ Failed to initialize Minotaur CLI integration:', error);
      throw error;
    }
  }

  /**
   * Extend existing MinotaurCLI program with Golem commands
   */
  extendCLIProgram(program: Command): Command {
    if (!this.isInitialized) {
      throw new Error('MinotaurCLIIntegration must be initialized before extending CLI program');
    }

    // Add version information
    const currentVersion = '1.0.0'; // Use fixed version instead of getter
    program.version(`${currentVersion} (with Project Golem)`, '-v, --version');

    // Add global Golem options
    program
      .option('--golem-cache <dir>', 'Golem cache directory', this.config.golemCacheDir)
      .option('--golem-metrics', 'Enable Golem performance metrics', this.config.enablePerformanceMetrics)
      .option('--golem-verbose', 'Enable verbose Golem logging', this.config.verbose);

    // Register Golem commands if enabled
    if (this.config.enableGolemCommands && this.golemCommands) {
      this.golemCommands.registerCommands(program);
      console.log('✅ Project Golem commands registered with MinotaurCLI');
    }

    // Add enhanced existing commands
    this.enhanceExistingCommands(program);

    return program;
  }

  /**
   * Enhance existing MinotaurCLI commands with Golem capabilities
   */
  private enhanceExistingCommands(program: Command): void {
    // Enhance 'parse' command with correction capabilities
    const parseCommand = program.commands.find(cmd => cmd.name() === 'parse');
    if (parseCommand) {
      parseCommand
        .option('--auto-correct', 'Automatically correct errors using Golem')
        .option('--golem-fallback', 'Use Golem as fallback for parse errors')
        .option('--correction-output <file>', 'Output file for corrected code');
    }

    // Enhance 'validate' command with Golem validation
    const validateCommand = program.commands.find(cmd => cmd.name() === 'validate');
    if (validateCommand) {
      validateCommand
        .option('--golem-enhance', 'Use Golem enhanced validation')
        .option('--benchmark-comparison', 'Compare with Mistral baseline')
        .option('--detailed-metrics', 'Show detailed Golem metrics');
    }

    // Add enhanced 'compile' command with correction
    program
      .command('compile-enhanced')
      .description('Compile with automatic error correction using Golem')
      .requiredOption('-f, --file <path>', 'Source file to compile')
      .option('-o, --output <path>', 'Output file path')
      .option('--max-corrections <count>', 'Maximum correction attempts', '3')
      .option('--correction-log <file>', 'Log correction attempts to file')
      .action(async (options) => {
        await this.handleEnhancedCompile(options);
      });

    // Add 'analyze' command for code analysis
    program
      .command('analyze')
      .description('Analyze code quality and suggest improvements using Golem')
      .requiredOption('-f, --file <path>', 'Source file to analyze')
      .option('--format <type>', 'Output format (text|json|html)', 'text')
      .option('--include-fixes', 'Include suggested fixes in analysis')
      .option('--performance-analysis', 'Include performance analysis')
      .action(async (options) => {
        await this.handleAnalyzeCommand(options);
      });
  }

  /**
   * Handle enhanced compile command
   */
  private async handleEnhancedCompile(options: any): Promise<void> {
    if (!this.golemCommands) {
      throw new Error('Golem commands not initialized');
    }

    try {
      console.log(`🔧 Enhanced compilation with Golem: ${options.file}`);
      
      // Read source file
      const sourceCode = await fs.readFile(options.file, 'utf-8');
      
      // Attempt compilation with correction
      let correctedCode = sourceCode;
      let correctionAttempts = 0;
      const maxCorrections = parseInt(options.maxCorrections) || 3;
      const correctionLog: string[] = [];
      
      while (correctionAttempts < maxCorrections) {
        try {
          // Try to parse/compile the code
          const grammar = await this.loadGrammar();
          const stepParser = new StepParser();
          
          // Attempt to parse
          const sourceLines = correctedCode.split('\n').map((line, index) => ({
            getLineNumber: () => index + 1,
            getContent: () => line,
            getLength: () => line.length,
            getFileName: () => 'inline-code',
          }));
          const sourceContainer = {
            getSourceLines: () => sourceLines,
            getCount: () => sourceLines.length,
            getLine: (fileName: string, lineNumber: number) => sourceLines[lineNumber - 1],
          };
          const parseResult = await stepParser.parse('python', sourceContainer);
          
          if (parseResult && parseResult.length > 0) {
            console.log(`✅ Compilation successful after ${correctionAttempts} corrections`);
            break;
          } else {
            // Apply Golem correction
            console.log(`🎯 Applying Golem correction (attempt ${correctionAttempts + 1})`);
            
            if (!this.agenticSystem) {
              await this.initializeAgenticSystem();
            }
            
            const correctionResult = await this.agenticSystem!.correctErrors(
              correctedCode,
              'enhanced-compile-user',
              `compile-correction-${correctionAttempts}`,
            );
            
            if (correctionResult.success && correctionResult.correctedCode) {
              correctedCode = correctionResult.correctedCode;
              correctionLog.push(`Attempt ${correctionAttempts + 1}: ${correctionResult.correctionSteps.length} corrections applied`);
            } else {
              correctionLog.push(`Attempt ${correctionAttempts + 1}: No corrections applied`);
              break;
            }
          }
          
        } catch (error) {
          correctionLog.push(`Attempt ${correctionAttempts + 1}: Error - ${error instanceof Error ? error.message : String(error)}`);
        }
        
        correctionAttempts++;
      }
      
      // Save corrected code
      const outputPath = options.output || options.file.replace(/(\.[^.]+)$/, '_corrected$1');
      await fs.writeFile(outputPath, correctedCode);
      
      // Save correction log if requested
      if (options.correctionLog) {
        await fs.writeFile(options.correctionLog, correctionLog.join('\n'));
      }
      
      console.log(`📄 Enhanced compilation complete: ${outputPath}`);
      console.log(`🔧 Correction attempts: ${correctionAttempts}`);
      
    } catch (error) {
      console.error('❌ Enhanced compilation failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle analyze command
   */
  private async handleAnalyzeCommand(options: any): Promise<void> {
    if (!this.golemCommands) {
      throw new Error('Golem commands not initialized');
    }

    try {
      console.log(`🔍 Analyzing code with Golem: ${options.file}`);
      
      // Read source file
      const sourceCode = await fs.readFile(options.file, 'utf-8');
      
      // Initialize agentic system if needed
      if (!this.agenticSystem) {
        await this.initializeAgenticSystem();
      }
      
      // Perform analysis
      const analysisResult = await this.agenticSystem!.correctErrors(
        sourceCode,
        'analysis-user',
        `analysis-${Date.now()}`,
      );
      
      // Generate analysis report
      const analysis = {
        file: options.file,
        timestamp: new Date().toISOString(),
        codeMetrics: {
          lines: sourceCode.split('\n').length,
          characters: sourceCode.length,
          complexity: this.calculateComplexity(sourceCode),
        },
        golemAnalysis: {
          success: analysisResult.success,
          deterministicRatio: analysisResult.deterministicRatio,
          correctionSteps: analysisResult.correctionSteps.length,
          executionTime: analysisResult.totalExecutionTime,
        },
        issues: this.extractIssues(analysisResult),
        suggestions: this.generateSuggestions(analysisResult),
        fixes: options.includeFixes ? analysisResult.correctedCode : undefined,
      };
      
      // Output analysis
      if (options.format === 'json') {
        console.log(JSON.stringify(analysis, null, 2));
      } else if (options.format === 'html') {
        const htmlReport = this.generateHTMLReport(analysis);
        console.log(htmlReport);
      } else {
        this.printTextAnalysis(analysis);
      }
      
    } catch (error) {
      console.error('❌ Code analysis failed:', error);
      process.exit(1);
    }
  }

  /**
   * Initialize agentic system
   */
  private async initializeAgenticSystem(): Promise<void> {
    const grammar = await this.loadGrammar();
    const stepParser = new StepParser();
    const mistralClient = new MistralAPIClient({
      apiKey: this.config.mistralApiKey!,
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
    
    // Create a minimal source container for StepLexer
    const sourceContainer = {
      getSourceLines: () => [],
      getCount: () => 0,
      getLine: (fileName: string, lineNumber: number) => null,
    };
    const stepLexer = new StepLexer(stepParser, new LexerOptions(false, false), sourceContainer);
    
    this.agenticSystem = new AgenticSystem(
      grammar,
      stepParser,
      stepLexer,
      {
        enableFullIntegration: true,
        enableAdvancedPatternMatching: true,
      },
    );
    
    await this.agenticSystem.initialize();
  }

  /**
   * Load grammar file
   */
  private async loadGrammar(): Promise<Grammar> {
    const grammarContent = await fs.readFile(this.config.grammarPath, 'utf-8');
    return new Grammar(grammarContent);
  }

  /**
   * Calculate code complexity (simple heuristic)
   */
  private calculateComplexity(code: string): number {
    const complexityKeywords = ['if', 'for', 'while', 'try', 'except', 'def', 'class'];
    let complexity = 1; // Base complexity
    
    for (const keyword of complexityKeywords) {
      const matches = code.match(new RegExp(`\\b${keyword}\\b`, 'g'));
      if (matches) {
        complexity += matches.length;
      }
    }
    
    return complexity;
  }

  /**
   * Extract issues from correction result
   */
  private extractIssues(result: any): string[] {
    const issues: string[] = [];
    
    if (result.correctionSteps) {
      for (const step of result.correctionSteps) {
        if (step.errorType) {
          issues.push(`${step.errorType}: ${step.description || 'Unknown issue'}`);
        }
      }
    }
    
    return issues;
  }

  /**
   * Generate suggestions from correction result
   */
  private generateSuggestions(result: any): string[] {
    const suggestions: string[] = [];
    
    if (result.deterministicRatio < 0.5) {
      suggestions.push('Consider refactoring complex logic for better maintainability');
    }
    
    if (result.correctionSteps && result.correctionSteps.length > 5) {
      suggestions.push('Multiple issues detected - consider code review');
    }
    
    if (result.totalExecutionTime > 5000) {
      suggestions.push('Code analysis took longer than expected - consider simplification');
    }
    
    return suggestions;
  }

  /**
   * Generate HTML report
   */
  private generateHTMLReport(analysis: any): string {
    return `
<!DOCTYPE html>
<html>
<head>
    <title>Golem Code Analysis Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #f0f0f0; padding: 10px; border-radius: 5px; }
        .section { margin: 20px 0; }
        .metric { display: inline-block; margin: 10px; padding: 10px; background: #e8f4f8; border-radius: 3px; }
        .issue { color: #d32f2f; }
        .suggestion { color: #1976d2; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🤖 Project Golem Code Analysis</h1>
        <p><strong>File:</strong> ${analysis.file}</p>
        <p><strong>Analyzed:</strong> ${analysis.timestamp}</p>
    </div>
    
    <div class="section">
        <h2>📊 Code Metrics</h2>
        <div class="metric">Lines: ${analysis.codeMetrics.lines}</div>
        <div class="metric">Characters: ${analysis.codeMetrics.characters}</div>
        <div class="metric">Complexity: ${analysis.codeMetrics.complexity}</div>
    </div>
    
    <div class="section">
        <h2>🎯 Golem Analysis</h2>
        <div class="metric">Success: ${analysis.golemAnalysis.success ? '✅' : '❌'}</div>
        <div class="metric">Deterministic Ratio: ${(analysis.golemAnalysis.deterministicRatio * 100).toFixed(1)}%</div>
        <div class="metric">Corrections: ${analysis.golemAnalysis.correctionSteps}</div>
        <div class="metric">Time: ${analysis.golemAnalysis.executionTime}ms</div>
    </div>
    
    ${analysis.issues.length > 0 ? `
    <div class="section">
        <h2>⚠️ Issues Found</h2>
        <ul>
            ${analysis.issues.map(issue => `<li class="issue">${issue}</li>`).join('')}
        </ul>
    </div>
    ` : ''}
    
    ${analysis.suggestions.length > 0 ? `
    <div class="section">
        <h2>💡 Suggestions</h2>
        <ul>
            ${analysis.suggestions.map(suggestion => `<li class="suggestion">${suggestion}</li>`).join('')}
        </ul>
    </div>
    ` : ''}
</body>
</html>`;
  }

  /**
   * Print text analysis
   */
  private printTextAnalysis(analysis: any): void {
    console.log('\n🤖 PROJECT GOLEM CODE ANALYSIS');
    console.log('=' .repeat(50));
    console.log(`📄 File: ${analysis.file}`);
    console.log(`📅 Analyzed: ${analysis.timestamp}`);
    
    console.log('\n📊 CODE METRICS:');
    console.log(`  Lines: ${analysis.codeMetrics.lines}`);
    console.log(`  Characters: ${analysis.codeMetrics.characters}`);
    console.log(`  Complexity: ${analysis.codeMetrics.complexity}`);
    
    console.log('\n🎯 GOLEM ANALYSIS:');
    console.log(`  Success: ${analysis.golemAnalysis.success ? '✅' : '❌'}`);
    console.log(`  Deterministic Ratio: ${(analysis.golemAnalysis.deterministicRatio * 100).toFixed(1)}%`);
    console.log(`  Correction Steps: ${analysis.golemAnalysis.correctionSteps}`);
    console.log(`  Execution Time: ${analysis.golemAnalysis.executionTime}ms`);
    
    if (analysis.issues.length > 0) {
      console.log('\n⚠️  ISSUES FOUND:');
      analysis.issues.forEach((issue: string) => {
        console.log(`  ❌ ${issue}`);
      });
    }
    
    if (analysis.suggestions.length > 0) {
      console.log('\n💡 SUGGESTIONS:');
      analysis.suggestions.forEach((suggestion: string) => {
        console.log(`  🎯 ${suggestion}`);
      });
    }
    
    console.log('=' .repeat(50));
  }

  /**
   * Get integration status
   */
  getStatus(): {
    initialized: boolean;
    golemEnabled: boolean;
    mistralConfigured: boolean;
    cacheDirectory: string | undefined;
  } {
    return {
      initialized: this.isInitialized,
      golemEnabled: this.config.enableGolemCommands,
      mistralConfigured: !!this.config.mistralApiKey,
      cacheDirectory: this.config.golemCacheDir,
    };
  }

  /**
   * Cleanup resources
   */
  async cleanup(): Promise<void> {
    if (this.agenticSystem) {
      // Cleanup agentic system resources if needed
      this.agenticSystem = null;
    }
    
    this.isInitialized = false;
    console.log('🧹 Minotaur CLI integration cleanup complete');
  }
}

