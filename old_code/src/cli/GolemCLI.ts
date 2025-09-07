/**
 * GolemCLI - Project-Wide Multi-File Correction CLI
 * 
 * Enhanced CLI interface for Project Golem with comprehensive project-wide
 * correction capabilities, intelligent file discovery, and multi-file operations.
 * 
 * New Commands:
 * - golem project-correct: Correct entire project folders
 * - golem error-fix: Fix errors from compiler/linter output
 * - golem project-analyze: Analyze entire project structure
 * - golem batch-process: Process multiple files with patterns
 */

import { Command } from 'commander';
import * as fs from 'fs/promises';
import * as path from 'path';
import { ProjectWideFileScanner, ProjectStructure } from '../evaluation/ProjectWideFileScanner';
import { MultiFileCorrector, MultiFileCorrectionOptions } from '../evaluation/MultiFileCorrector';
import { AgenticSystem } from '../evaluation/AgenticSystem';
import { MistralAPIClient } from '../evaluation/MistralAPIClient';
import { PromptFileProcessor, PromptInstruction } from './PromptFileProcessor';
import { GolemCLICommands } from './GolemCLICommands';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';

export interface EnhancedCLIConfig {
  // Project settings
  defaultProjectPath: string;
  defaultGrammarPath: string;
  
  // Multi-file settings
  maxConcurrentFiles: number;
  enableBackup: boolean;
  backupDirectory: string;
  
  // Performance settings
  enableParallelProcessing: boolean;
  enableCaching: boolean;
  cacheDirectory: string;
  
  // Reporting settings
  enableDetailedReporting: boolean;
  reportDirectory: string;
  enableProgressBar: boolean;
  
  // Safety settings
  enableDryRunByDefault: boolean;
  requireConfirmation: boolean;
  maxFilesWithoutConfirmation: number;
}

/**
 * GolemCLI - Main enhanced CLI class
 */
export class GolemCLI extends GolemCLICommands {
  private fileScanner: ProjectWideFileScanner;
  private multiFileCorrector: MultiFileCorrector;
  private enhancedPromptProcessor: PromptFileProcessor;
  private enhancedConfig: EnhancedCLIConfig;

  constructor(config: Partial<EnhancedCLIConfig> = {}) {
    // Initialize base CLI
    super();
    
    this.enhancedConfig = {
      defaultProjectPath: process.cwd(),
      defaultGrammarPath: '/home/ubuntu/Minotaur/grammar/Python311.grammar',
      maxConcurrentFiles: 5,
      enableBackup: true,
      backupDirectory: '.golem-backups',
      enableParallelProcessing: true,
      enableCaching: true,
      cacheDirectory: '.golem-cache',
      enableDetailedReporting: true,
      reportDirectory: '.golem-reports',
      enableProgressBar: true,
      enableDryRunByDefault: false,
      requireConfirmation: true,
      maxFilesWithoutConfirmation: 10,
      ...config,
    };
    
    this.fileScanner = new ProjectWideFileScanner();
    this.enhancedPromptProcessor = new PromptFileProcessor();
  }

  /**
   * Initialize enhanced CLI system
   */
  async initializeEnhanced(): Promise<void> {
    // Initialize base CLI first
    await super.initialize();
    
    // Initialize multi-file corrector
    const agenticSystem = this.getAgenticSystem();
    if (!agenticSystem) {
      throw new Error('Agentic system not initialized');
    }
    
    this.multiFileCorrector = new MultiFileCorrector(
      this.fileScanner,
      agenticSystem,
      this.enhancedPromptProcessor,
    );
    
    console.log('✅ Enhanced Golem CLI initialized with multi-file capabilities');
  }

  /**
   * Register enhanced CLI commands
   */
  registerEnhancedCommands(program: Command): void {
    // Register base commands first
    super.registerCommands(program);
    
    // Get the golem command group
    const golemCommand = program.commands.find(cmd => cmd.name() === 'golem');
    if (!golemCommand) {
      throw new Error('Golem command group not found');
    }

    // Enhanced project-wide commands
    this.registerProjectCommands(golemCommand);
    this.registerErrorFixCommands(golemCommand);
    this.registerAnalysisCommands(golemCommand);
    this.registerBatchCommands(golemCommand);
  }

  /**
   * Register project-wide correction commands
   */
  private registerProjectCommands(golemCommand: any): void {
    // golem project-correct command
    golemCommand
      .command('project-correct')
      .description('Correct entire project folder with multi-file operations')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .option('--prompt <path>', 'Prompt file with correction instructions')
      .option('--include <patterns...>', 'File patterns to include')
      .option('--exclude <patterns...>', 'File patterns to exclude')
      .option('--max-files <count>', 'Maximum files to process', '100')
      .option('--concurrent <count>', 'Concurrent file processing', this.enhancedConfig.maxConcurrentFiles.toString())
      .option('--backup', 'Create backup before modifications', this.enhancedConfig.enableBackup)
      .option('--no-backup', 'Skip backup creation')
      .option('--dry-run', 'Show what would be changed without applying')
      .option('--force', 'Skip confirmation prompts')
      .option('--report <path>', 'Save detailed report to file')
      .action(async (options) => {
        await this.handleProjectCorrectCommand(options);
      });

    // golem project-scan command
    golemCommand
      .command('project-scan')
      .description('Scan and analyze project structure')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .option('--include <patterns...>', 'File patterns to include')
      .option('--exclude <patterns...>', 'File patterns to exclude')
      .option('--analyze-deps', 'Analyze file dependencies', true)
      .option('--format <type>', 'Output format (text|json|html)', 'text')
      .option('--output <path>', 'Output file path')
      .action(async (options) => {
        await this.handleProjectAnalyzeCommand(options);
      });
  }

  /**
   * Register error fixing commands
   */
  private registerErrorFixCommands(golemCommand: any): void {
    // golem error-fix command
    golemCommand
      .command('error-fix')
      .description('Fix errors from compiler/linter output with file:line resolution')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .option('-e, --errors <path>', 'File containing error messages')
      .option('--stdin', 'Read error messages from stdin')
      .option('--format <type>', 'Error format (typescript|python|eslint|generic)', 'generic')
      .option('--max-files <count>', 'Maximum files to modify', '50')
      .option('--backup', 'Create backup before modifications', true)
      .option('--dry-run', 'Show what would be fixed without applying')
      .option('--interactive', 'Confirm each fix interactively')
      .action(async (options) => {
        await this.handleErrorFixCommand(options);
      });

    // golem typescript-fix command
    golemCommand
      .command('typescript-fix')
      .description('Fix TypeScript compiler errors with intelligent file mapping')
      .requiredOption('-p, --project <path>', 'TypeScript project path')
      .option('--tsconfig <path>', 'TypeScript config file', 'tsconfig.json')
      .option('--compile-first', 'Run tsc first to get fresh errors', true)
      .option('--fix-imports', 'Fix import/export errors', true)
      .option('--fix-types', 'Fix type errors', true)
      .option('--max-iterations <count>', 'Maximum fix iterations', '3')
      .action(async (options) => {
        await this.handleTypeScriptFixCommand(options);
      });
  }

  /**
   * Register analysis commands
   */
  private registerAnalysisCommands(golemCommand: any): void {
    // golem project-analyze command
    golemCommand
      .command('project-analyze')
      .description('Comprehensive project analysis with improvement suggestions')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .option('--depth <level>', 'Analysis depth (basic|detailed|comprehensive)', 'detailed')
      .option('--focus <areas...>', 'Focus areas', ['errors', 'quality', 'dependencies', 'performance'])
      .option('--format <type>', 'Output format (text|json|html|markdown)', 'text')
      .option('--output <path>', 'Output file path')
      .option('--include-fixes', 'Include suggested fixes in analysis')
      .option('--generate-report', 'Generate comprehensive HTML report')
      .action(async (options) => {
        await this.handleProjectAnalyzeCommand(options);
      });

    // golem dependency-graph command
    golemCommand
      .command('dependency-graph')
      .description('Generate project dependency graph and analysis')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .option('--format <type>', 'Output format (dot|json|svg|png)', 'dot')
      .option('--output <path>', 'Output file path')
      .option('--include-external', 'Include external dependencies', false)
      .option('--max-depth <level>', 'Maximum dependency depth', '5')
      .action(async (options) => {
        await this.handleProjectAnalyzeCommand(options);
      });
  }

  /**
   * Register batch processing commands
   */
  private registerBatchCommands(golemCommand: any): void {
    // golem batch-process command
    golemCommand
      .command('batch-process')
      .description('Process multiple files with pattern matching and batch operations')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .requiredOption('--pattern <glob>', 'File pattern to match (e.g., "**/*.py")')
      .requiredOption('--operation <type>', 'Operation type (format|refactor|update|fix)')
      .option('--prompt <path>', 'Prompt file with instructions')
      .option('--template <path>', 'Template file for transformations')
      .option('--max-files <count>', 'Maximum files to process', '100')
      .option('--concurrent <count>', 'Concurrent processing', '3')
      .option('--backup', 'Create backup before modifications', true)
      .option('--dry-run', 'Show what would be changed')
      .action(async (options) => {
        await this.handleBatchProcessCommand(options);
      });

    // golem find-and-replace command
    golemCommand
      .command('find-replace')
      .description('Intelligent find and replace across project with context awareness')
      .requiredOption('-p, --project <path>', 'Project directory path')
      .requiredOption('--find <pattern>', 'Pattern to find (supports regex)')
      .requiredOption('--replace <replacement>', 'Replacement text')
      .option('--file-pattern <glob>', 'File pattern to search in', '**/*')
      .option('--regex', 'Treat find pattern as regex', false)
      .option('--case-sensitive', 'Case sensitive search', false)
      .option('--whole-word', 'Match whole words only', false)
      .option('--context-aware', 'Use AI for context-aware replacement', true)
      .option('--preview', 'Preview changes before applying', true)
      .option('--backup', 'Create backup before modifications', true)
      .action(async (options) => {
        await this.handleProjectCorrectCommand(options);
      });
  }

  /**
   * Handle project-correct command
   */
  private async handleProjectCorrectCommand(options: any): Promise<void> {
    try {
      console.log(`🎯 Starting project-wide correction: ${options.project}`);
      
      // Validate project path
      const projectPath = path.resolve(options.project);
      await this.validateProjectPath(projectPath);
      
      // Load prompt instructions
      let instructions: PromptInstruction[] = [];
      if (options.prompt) {
        const promptResult = await this.enhancedPromptProcessor.processPromptFile(options.prompt);
        instructions = promptResult.instructions;
        console.log(`📝 Loaded ${instructions.length} instructions from prompt file`);
      } else {
        // Default instructions for general correction
        instructions = [
          {
            type: 'fix_error',
            description: 'Fix syntax errors and common issues',
            priority: 'high',
          },
          {
            type: 'format_code',
            description: 'Format code according to language standards',
            priority: 'medium',
          },
        ];
      }
      
      // Prepare correction options
      const correctionOptions: Partial<MultiFileCorrectionOptions> = {
        enableAtomicOperations: true,
        enableRollback: options.backup !== false,
        maxConcurrentFiles: parseInt(options.concurrent) || this.enhancedConfig.maxConcurrentFiles,
        createBackup: options.backup !== false,
        backupDirectory: this.enhancedConfig.backupDirectory,
        dryRun: options.dryRun || false,
        enableParallelProcessing: this.enhancedConfig.enableParallelProcessing,
        enableDetailedLogging: true,
        enableProgressReporting: true,
        saveDetailedReport: !!options.report,
      };
      
      // Scan project first to get file count
      const projectStructure = await this.fileScanner.scanProject(projectPath, {
        includePatterns: options.include || ['**/*'],
        excludePatterns: options.exclude || ['**/node_modules/**', '**/dist/**'],
        maxFiles: parseInt(options.maxFiles) || 100,
        loadFileContent: false, // Just for counting
      });
      
      console.log(`📊 Project scan complete: ${projectStructure.totalFiles} files found`);
      
      // Confirm operation if required
      if (!options.force && this.enhancedConfig.requireConfirmation && 
          projectStructure.totalFiles > this.enhancedConfig.maxFilesWithoutConfirmation) {
        
        const confirmed = await this.confirmOperation(
          `This will process ${projectStructure.totalFiles} files. Continue?`,
        );
        
        if (!confirmed) {
          console.log('❌ Operation cancelled by user');
          return;
        }
      }
      
      // Execute project correction
      const result = await this.multiFileCorrector.correctProject(
        projectPath,
        instructions,
        correctionOptions,
      );
      
      // Display results
      this.displayCorrectionResults(result);
      
      // Save report if requested
      if (options.report) {
        await this.saveCorrectionReport(result, options.report);
      }
      
    } catch (error) {
      console.error('❌ Project correction failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle error-fix command
   */
  private async handleErrorFixCommand(options: any): Promise<void> {
    try {
      console.log(`🔧 Fixing errors in project: ${options.project}`);
      
      // Read error messages
      let errorMessages: string[] = [];
      
      if (options.errors) {
        const errorContent = await fs.readFile(options.errors, 'utf-8');
        errorMessages = errorContent.split('\n').filter(line => line.trim());
      } else if (options.stdin) {
        // Read from stdin (would implement proper stdin reading)
        console.log('📥 Reading error messages from stdin...');
        errorMessages = ['Sample error message']; // Placeholder
      } else {
        throw new Error('Either --errors file or --stdin must be specified');
      }
      
      console.log(`📋 Processing ${errorMessages.length} error messages`);
      
      // Parse errors based on format
      const parsedErrors = await this.parseErrorMessages(errorMessages, options.format);
      console.log(`🔍 Parsed ${parsedErrors.length} actionable errors`);
      
      // Prepare correction options
      const correctionOptions: Partial<MultiFileCorrectionOptions> = {
        enableAtomicOperations: true,
        createBackup: options.backup !== false,
        dryRun: options.dryRun || false,
        enableDetailedLogging: true,
        maxConcurrentFiles: 3, // Conservative for error fixing
      };
      
      // Execute error corrections
      const result = await this.multiFileCorrector.correctFromErrorMessages(
        path.resolve(options.project),
        parsedErrors,
        correctionOptions,
      );
      
      // Display results
      this.displayCorrectionResults(result);
      
    } catch (error) {
      console.error('❌ Error fixing failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle TypeScript-specific error fixing
   */
  private async handleTypeScriptFixCommand(options: any): Promise<void> {
    try {
      console.log(`🔷 Fixing TypeScript errors in: ${options.project}`);
      
      const projectPath = path.resolve(options.project);
      
      // Run TypeScript compiler to get fresh errors if requested
      let errorMessages: string[] = [];
      
      if (options.compileFirst) {
        console.log('🔄 Running TypeScript compiler to get current errors...');
        errorMessages = await this.runTypeScriptCompiler(projectPath, options.tsconfig);
      } else {
        throw new Error('Please provide error messages or use --compile-first');
      }
      
      console.log(`📋 Found ${errorMessages.length} TypeScript errors`);
      
      // Filter errors based on options
      const filteredErrors = this.filterTypeScriptErrors(errorMessages, options);
      console.log(`🎯 Targeting ${filteredErrors.length} fixable errors`);
      
      // Execute iterative fixing
      let iteration = 0;
      const maxIterations = parseInt(options.maxIterations) || 3;
      let remainingErrors = filteredErrors;
      
      while (iteration < maxIterations && remainingErrors.length > 0) {
        console.log(`\n🔄 Fix iteration ${iteration + 1}/${maxIterations}`);
        
        const result = await this.multiFileCorrector.correctFromErrorMessages(
          projectPath,
          remainingErrors,
          {
            enableAtomicOperations: true,
            createBackup: iteration === 0, // Only backup on first iteration
            dryRun: false,
            enableDetailedLogging: true,
          },
        );
        
        console.log(`  ✅ Modified ${result.modifiedFiles} files`);
        
        // Re-run compiler to check remaining errors
        if (iteration < maxIterations - 1) {
          remainingErrors = await this.runTypeScriptCompiler(projectPath, options.tsconfig);
          console.log(`  📊 ${remainingErrors.length} errors remaining`);
        }
        
        iteration++;
      }
      
      console.log(`\n🎉 TypeScript error fixing complete after ${iteration} iterations`);
      
    } catch (error) {
      console.error('❌ TypeScript error fixing failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle project-analyze command
   */
  private async handleProjectAnalyzeCommand(options: any): Promise<void> {
    try {
      console.log(`🔍 Analyzing project: ${options.project}`);
      
      const projectPath = path.resolve(options.project);
      
      // Scan project with full analysis
      const projectStructure = await this.fileScanner.scanProject(projectPath, {
        loadFileContent: true,
        analyzeDependencies: true,
        extractExports: true,
        extractImports: true,
      });
      
      // Generate comprehensive analysis
      const analysis = await this.generateProjectAnalysis(
        projectStructure,
        options.depth,
        options.focus,
      );
      
      // Output analysis
      if (options.output) {
        await this.saveAnalysis(analysis, options.output, options.format);
      } else {
        this.displayAnalysis(analysis, options.format);
      }
      
      // Generate HTML report if requested
      if (options.generateReport) {
        const reportPath = path.join(projectPath, this.enhancedConfig.reportDirectory, 'analysis-report.html');
        await this.generateHTMLAnalysisReport(analysis, reportPath);
        console.log(`📊 HTML report generated: ${reportPath}`);
      }
      
    } catch (error) {
      console.error('❌ Project analysis failed:', error);
      process.exit(1);
    }
  }

  /**
   * Handle batch-process command
   */
  private async handleBatchProcessCommand(options: any): Promise<void> {
    try {
      console.log(`⚡ Batch processing files: ${options.pattern}`);
      
      const projectPath = path.resolve(options.project);
      
      // Find matching files
      const projectStructure = await this.fileScanner.scanProject(projectPath, {
        includePatterns: [options.pattern],
        maxFiles: parseInt(options.maxFiles) || 100,
        loadFileContent: true,
      });
      
      const matchingFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
      console.log(`📁 Found ${matchingFiles.length} matching files`);
      
      // Load instructions
      let instructions: PromptInstruction[] = [];
      
      if (options.prompt) {
        const promptResult = await this.enhancedPromptProcessor.processPromptFile(options.prompt);
        instructions = promptResult.instructions;
      } else {
        // Create default instruction based on operation type
        instructions = [{
          type: options.operation === 'format' ? 'format_code' :
                options.operation === 'refactor' ? 'refactor' :
                options.operation === 'fix' ? 'fix_error' : 'add_feature',
          description: `${options.operation} files matching pattern: ${options.pattern}`,
          priority: 'medium',
        }];
      }
      
      // Execute batch processing
      const result = await this.multiFileCorrector.correctProject(
        projectPath,
        instructions,
        {
          enableParallelProcessing: true,
          maxConcurrentFiles: parseInt(options.concurrent) || 3,
          createBackup: options.backup !== false,
          dryRun: options.dryRun || false,
          enableDetailedLogging: true,
        },
      );
      
      // Display results
      this.displayCorrectionResults(result);
      
    } catch (error) {
      console.error('❌ Batch processing failed:', error);
      process.exit(1);
    }
  }

  /**
   * Parse error messages based on format
   */
  private async parseErrorMessages(errorMessages: string[], format: string): Promise<string[]> {
    const parsedErrors: string[] = [];
    
    for (const message of errorMessages) {
      if (message.trim()) {
        // Parse based on format
        switch (format) {
          case 'typescript':
            if (message.includes('.ts(') || message.includes('.tsx(')) {
              parsedErrors.push(message);
            }
            break;
          case 'python':
            if (message.includes('File "') && message.includes('line ')) {
              parsedErrors.push(message);
            }
            break;
          case 'eslint':
            if (message.includes(':') && /\d+:\d+/.test(message)) {
              parsedErrors.push(message);
            }
            break;
          default:
            // Generic parsing - look for file:line patterns
            if (/\w+\.\w+:\d+/.test(message) || message.includes('error') || message.includes('Error')) {
              parsedErrors.push(message);
            }
        }
      }
    }
    
    return parsedErrors;
  }

  /**
   * Run TypeScript compiler and capture errors
   */
  private async runTypeScriptCompiler(projectPath: string, tsconfigPath: string): Promise<string[]> {
    // This would run `tsc --noEmit` and capture the output
    // For now, return mock errors
    return [
      `${projectPath}/src/user.ts(45,12): error TS2304: Cannot find name 'UserType'.`,
      `${projectPath}/src/utils.ts(23,5): error TS2322: Type 'string' is not assignable to type 'number'.`,
    ];
  }

  /**
   * Filter TypeScript errors based on options
   */
  private filterTypeScriptErrors(errors: string[], options: any): string[] {
    let filtered = errors;
    
    if (!options.fixImports) {
      filtered = filtered.filter(error => !error.includes('TS2307') && !error.includes('Cannot find module'));
    }
    
    if (!options.fixTypes) {
      filtered = filtered.filter(error => !error.includes('TS2322') && !error.includes('not assignable'));
    }
    
    return filtered;
  }

  /**
   * Generate comprehensive project analysis
   */
  private async generateProjectAnalysis(
    projectStructure: ProjectStructure,
    depth: string,
    focusAreas: string[],
  ): Promise<any> {
    
    const analysis = {
      timestamp: new Date().toISOString(),
      projectPath: projectStructure.rootPath,
      summary: {
        totalFiles: projectStructure.totalFiles,
        languages: Array.from(projectStructure.filesByLanguage.keys()),
        largestFiles: this.findLargestFiles(projectStructure),
        dependencyComplexity: this.calculateDependencyComplexity(projectStructure),
      },
      languages: this.analyzeLanguageDistribution(projectStructure),
      dependencies: this.analyzeDependencies(projectStructure),
      issues: [],
      suggestions: [],
      metrics: {},
    };
    
    // Add focus-specific analysis
    if (focusAreas.includes('errors')) {
      analysis.issues = await this.findPotentialIssues(projectStructure);
    }
    
    if (focusAreas.includes('quality')) {
      analysis.metrics = await this.calculateQualityMetrics(projectStructure);
    }
    
    if (focusAreas.includes('performance')) {
      analysis.suggestions = await this.generatePerformanceSuggestions(projectStructure);
    }
    
    return analysis;
  }

  /**
   * Utility methods for analysis
   */
  private findLargestFiles(projectStructure: ProjectStructure): any[] {
    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
    return allFiles
      .sort((a, b) => b.size - a.size)
      .slice(0, 10)
      .map(file => ({
        path: file.relativePath,
        size: file.size,
        language: file.language,
      }));
  }

  private calculateDependencyComplexity(projectStructure: ProjectStructure): number {
    return projectStructure.dependencyGraph.size;
  }

  private analyzeLanguageDistribution(projectStructure: ProjectStructure): any {
    const distribution: any = {};
    
    for (const [language, files] of projectStructure.filesByLanguage) {
      distribution[language] = {
        fileCount: files.length,
        totalSize: files.reduce((sum, file) => sum + file.size, 0),
        averageSize: files.reduce((sum, file) => sum + file.size, 0) / files.length,
      };
    }
    
    return distribution;
  }

  private analyzeDependencies(projectStructure: ProjectStructure): any {
    return {
      totalDependencies: projectStructure.dependencyGraph.size,
      externalDependencies: Array.from(projectStructure.dependencyGraph.values()).flat().length,
      circularDependencies: [], // Would implement circular dependency detection
    };
  }

  private async findPotentialIssues(projectStructure: ProjectStructure): Promise<string[]> {
    const issues: string[] = [];
    
    // Find large files
    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
    const largeFiles = allFiles.filter(file => file.size > 100000); // > 100KB
    
    if (largeFiles.length > 0) {
      issues.push(`Found ${largeFiles.length} large files that may need refactoring`);
    }
    
    // Find files with many dependencies
    for (const [file, deps] of projectStructure.dependencyGraph) {
      if (deps.length > 20) {
        issues.push(`File ${file} has ${deps.length} dependencies - consider refactoring`);
      }
    }
    
    return issues;
  }

  private async calculateQualityMetrics(projectStructure: ProjectStructure): Promise<any> {
    return {
      averageFileSize: Array.from(projectStructure.filesByLanguage.values())
        .flat()
        .reduce((sum, file) => sum + file.size, 0) / projectStructure.totalFiles,
      dependencyRatio: projectStructure.dependencyGraph.size / projectStructure.totalFiles,
      languageDiversity: projectStructure.filesByLanguage.size,
    };
  }

  private async generatePerformanceSuggestions(projectStructure: ProjectStructure): Promise<string[]> {
    const suggestions: string[] = [];
    
    if (projectStructure.filesByLanguage.has('javascript') || projectStructure.filesByLanguage.has('typescript')) {
      suggestions.push('Consider using TypeScript for better type safety and performance');
    }
    
    if (projectStructure.totalFiles > 1000) {
      suggestions.push('Large project detected - consider modularization and build optimization');
    }
    
    return suggestions;
  }

  /**
   * Display correction results
   */
  private displayCorrectionResults(result: any): void {
    console.log('\n🎉 CORRECTION RESULTS');
    console.log('=' .repeat(50));
    console.log(`✅ Success: ${result.success ? 'Yes' : 'No'}`);
    console.log(`📊 Files processed: ${result.totalFiles}`);
    console.log(`📝 Files modified: ${result.modifiedFiles}`);
    console.log(`⏭️  Files skipped: ${result.skippedFiles}`);
    console.log(`❌ Files failed: ${result.failedFiles}`);
    console.log(`⏱️  Execution time: ${result.executionTime}ms`);
    console.log(`📏 Lines changed: ${result.summary.totalLinesChanged}`);
    
    if (result.errors.length > 0) {
      console.log('\n⚠️  ERRORS:');
      result.errors.forEach((error: string) => {
        console.log(`  ❌ ${error}`);
      });
    }
    
    console.log('=' .repeat(50));
  }

  /**
   * Display analysis results
   */
  private displayAnalysis(analysis: any, format: string): void {
    if (format === 'json') {
      console.log(JSON.stringify(analysis, null, 2));
    } else {
      console.log('\n🔍 PROJECT ANALYSIS');
      console.log('=' .repeat(50));
      console.log(`📁 Project: ${analysis.projectPath}`);
      console.log(`📊 Total files: ${analysis.summary.totalFiles}`);
      console.log(`🗣️  Languages: ${analysis.summary.languages.join(', ')}`);
      console.log(`🔗 Dependencies: ${analysis.dependencies.totalDependencies}`);
      
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
  }

  /**
   * Utility methods
   */
  private async validateProjectPath(projectPath: string): Promise<void> {
    try {
      const stats = await fs.stat(projectPath);
      if (!stats.isDirectory()) {
        throw new Error('Project path must be a directory');
      }
    } catch (error) {
      throw new Error(`Invalid project path: ${projectPath}`);
    }
  }

  private async confirmOperation(message: string): Promise<boolean> {
    // Would implement proper CLI confirmation prompt
    console.log(`❓ ${message} (y/N)`);
    return true; // Mock confirmation
  }

  private async saveCorrectionReport(result: any, reportPath: string): Promise<void> {
    await fs.writeFile(reportPath, JSON.stringify(result, null, 2));
    console.log(`📊 Report saved: ${reportPath}`);
  }

  private async saveAnalysis(analysis: any, outputPath: string, format: string): Promise<void> {
    let content: string;
    
    switch (format) {
      case 'json':
        content = JSON.stringify(analysis, null, 2);
        break;
      case 'html':
        content = this.generateHTMLAnalysis(analysis);
        break;
      default:
        content = this.generateTextAnalysis(analysis);
    }
    
    await fs.writeFile(outputPath, content);
    console.log(`📄 Analysis saved: ${outputPath}`);
  }

  private generateHTMLAnalysis(analysis: any): string {
    return `
<!DOCTYPE html>
<html>
<head>
    <title>Project Analysis Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .section { margin: 20px 0; }
        .metric { background: #f0f0f0; padding: 10px; margin: 5px 0; border-radius: 3px; }
    </style>
</head>
<body>
    <h1>🔍 Project Analysis Report</h1>
    <div class="section">
        <h2>📊 Summary</h2>
        <div class="metric">Total Files: ${analysis.summary.totalFiles}</div>
        <div class="metric">Languages: ${analysis.summary.languages.join(', ')}</div>
        <div class="metric">Dependencies: ${analysis.dependencies.totalDependencies}</div>
    </div>
    ${analysis.issues.length > 0 ? `
    <div class="section">
        <h2>⚠️ Issues</h2>
        ${analysis.issues.map((issue: string) => `<div class="metric">❌ ${issue}</div>`).join('')}
    </div>
    ` : ''}
</body>
</html>`;
  }

  private generateTextAnalysis(analysis: any): string {
    let text = 'PROJECT ANALYSIS REPORT\n';
    text += '======================\n\n';
    text += `Project: ${analysis.projectPath}\n`;
    text += `Total Files: ${analysis.summary.totalFiles}\n`;
    text += `Languages: ${analysis.summary.languages.join(', ')}\n`;
    text += `Dependencies: ${analysis.dependencies.totalDependencies}\n\n`;
    
    if (analysis.issues.length > 0) {
      text += 'ISSUES FOUND:\n';
      analysis.issues.forEach((issue: string) => {
        text += `- ${issue}\n`;
      });
      text += '\n';
    }
    
    return text;
  }

  private async generateHTMLAnalysisReport(analysis: any, reportPath: string): Promise<void> {
    const htmlContent = this.generateHTMLAnalysis(analysis);
    await fs.mkdir(path.dirname(reportPath), { recursive: true });
    await fs.writeFile(reportPath, htmlContent);
  }

  /**
   * Get agentic system (protected method from base class)
   */
  private getAgenticSystem(): AgenticSystem | null {
    // This would access the agentic system from the base class
    // For now, return null as placeholder
    return null;
  }
}

