/**
 * Minotaur Command-Line Interface with dual MCP/CLI support.
 * Provides both interactive command-line usage and programmatic MCP access for AI agents.
 */

import { program } from 'commander';
import { LanguageManager, SupportedLanguage } from '../languages/LanguageManager';
import { CrossLanguageOperations, CrossLanguageOperationType, CrossLanguageParameters } from '../languages/CrossLanguageOperations';
import { RefactoringEngine } from '../refactoring/RefactoringEngine';
import { ContextManager } from '../context/ContextManager';
import { MCPServer } from '../mcp/MCPServer';
import { MCPConnectionManager } from '../mcp/MCPConnectionManager';
import { MCPMessageRouter } from '../mcp/MCPMessageRouter';
import { MCPRequestHandler } from '../mcp/MCPRequestHandler';
import * as fs from 'fs';
import * as path from 'path';

/**
 * CLI configuration options.
 */
export interface CLIConfig {
  enableMCP: boolean;
  mcpPort: number;
  mcpHost: string;
  verbose: boolean;
  outputFormat: 'json' | 'text' | 'yaml';
  enableColors: boolean;
  logLevel: 'error' | 'warn' | 'info' | 'debug';
  workingDirectory: string;
  configFile?: string;
}

/**
 * CLI operation result.
 */
export interface CLIResult {
  success: boolean;
  operation: string;
  result?: any;
  error?: string;
  warnings?: string[];
  executionTime: number;
  outputFormat: string;
}

/**
 * Minotaur CLI with dual MCP/CLI support.
 */
export class MinotaurCLI {
  private program: any;
  private config: CLIConfig;
  private languageManager: LanguageManager;
  private crossLanguageOps: CrossLanguageOperations;
  private refactoringEngine: RefactoringEngine;
  private contextManager: ContextManager;
  private mcpServer?: MCPServer;
  private mcpConnectionManager?: MCPConnectionManager;
  private mcpMessageRouter?: MCPMessageRouter;
  private mcpRequestHandler?: MCPRequestHandler;

  constructor(config: Partial<CLIConfig> = {}) {
    this.config = {
      enableMCP: false,
      mcpPort: 8080,
      mcpHost: 'localhost',
      verbose: false,
      outputFormat: 'text',
      enableColors: true,
      logLevel: 'info',
      workingDirectory: process.cwd(),
      ...config,
    };

    this.program = program;
    this.initializeComponents();
    this.setupCommands();
  }

  /**
   * Initializes core components.
   */
  private initializeComponents(): void {
    // Initialize context manager
    this.contextManager = new ContextManager();

    // Initialize refactoring engine
    this.refactoringEngine = new RefactoringEngine(this.contextManager);

    // Initialize language manager
    this.languageManager = new LanguageManager(this.contextManager, this.refactoringEngine);

    // Initialize cross-language operations
    this.crossLanguageOps = new CrossLanguageOperations(
      this.languageManager,
      this.refactoringEngine,
      this.contextManager,
    );

    // Initialize MCP components if enabled
    if (this.config.enableMCP) {
      this.initializeMCPComponents();
    }
  }

  /**
   * Initializes MCP components.
   */
  private initializeMCPComponents(): void {
    // Initialize MCP request handler
    this.mcpRequestHandler = new MCPRequestHandler(
      this.contextManager,
    );

    // Initialize MCP message router
    this.mcpMessageRouter = new MCPMessageRouter(
      this.contextManager,
      this.mcpRequestHandler,
    );

    // Initialize MCP connection manager
    this.mcpConnectionManager = new MCPConnectionManager(this.mcpMessageRouter);

    // Initialize MCP server
    this.mcpServer = new MCPServer({
      port: this.config.mcpPort,
      host: this.config.mcpHost,
    });
  }

  /**
   * Sets up CLI commands.
   */
  private setupCommands(): void {
    this.program
      .name('minotaur')
      .description('Minotaur - Grammar-aware code refactoring with AI agent support')
      .version('1.0.0')
      .option('-v, --verbose', 'Enable verbose output')
      .option('-f, --format <format>', 'Output format (json|text|yaml)', 'text')
      .option('--no-colors', 'Disable colored output')
      .option('--config <file>', 'Configuration file path')
      .option('--mcp', 'Enable MCP server mode')
      .option('--mcp-port <port>', 'MCP server port', '8080')
      .option('--mcp-host <host>', 'MCP server host', 'localhost');

    // Language detection commands
    this.setupLanguageCommands();

    // Refactoring commands
    this.setupRefactoringCommands();

    // Cross-language operation commands
    this.setupCrossLanguageCommands();

    // Analysis commands
    this.setupAnalysisCommands();

    // MCP server commands
    this.setupMCPCommands();

    // Utility commands
    this.setupUtilityCommands();
  }

  /**
   * Sets up language detection commands.
   */
  private setupLanguageCommands(): void {
    const langCmd = this.program
      .command('language')
      .alias('lang')
      .description('Language detection and analysis commands');

    langCmd
      .command('detect <file>')
      .description('Detect the programming language of a file')
      .option('-c, --content', 'Analyze content in addition to file extension')
      .option('-e, --embedded', 'Detect embedded languages')
      .action(async (file, options) => {
        await this.executeCommand('language:detect', { file, ...options });
      });

    langCmd
      .command('list')
      .description('List all supported languages')
      .option('-d, --detailed', 'Show detailed language information')
      .action(async (options) => {
        await this.executeCommand('language:list', options);
      });

    langCmd
      .command('config <language>')
      .description('Show configuration for a specific language')
      .action(async (language, options) => {
        await this.executeCommand('language:config', { language, ...options });
      });
  }

  /**
   * Sets up refactoring commands.
   */
  private setupRefactoringCommands(): void {
    const refactorCmd = this.program
      .command('refactor')
      .alias('ref')
      .description('Code refactoring commands');

    refactorCmd
      .command('extract-variable <file> <line> <column> <endLine> <endColumn> <name>')
      .description('Extract a variable from an expression')
      .option('-p, --preview', 'Preview changes without applying')
      .action(async (file, line, column, endLine, endColumn, name, options) => {
        await this.executeCommand('refactor:extract-variable', {
          file,
          position: { line: parseInt(line), column: parseInt(column) },
          endPosition: { line: parseInt(endLine), column: parseInt(endColumn) },
          variableName: name,
          ...options,
        });
      });

    refactorCmd
      .command('inline-variable <file> <line> <column> <name>')
      .description('Inline a variable by replacing references with its value')
      .option('-p, --preview', 'Preview changes without applying')
      .action(async (file, line, column, name, options) => {
        await this.executeCommand('refactor:inline-variable', {
          file,
          position: { line: parseInt(line), column: parseInt(column) },
          variableName: name,
          ...options,
        });
      });

    refactorCmd
      .command('rename <file> <line> <column> <oldName> <newName>')
      .description('Rename a symbol across all references')
      .option('-s, --scope <scope>', 'Scope of rename (file|project)', 'file')
      .option('-p, --preview', 'Preview changes without applying')
      .action(async (file, line, column, oldName, newName, options) => {
        await this.executeCommand('refactor:rename', {
          file,
          position: { line: parseInt(line), column: parseInt(column) },
          oldName,
          newName,
          ...options,
        });
      });

    refactorCmd
      .command('extract-function <file> <startLine> <startColumn> <endLine> <endColumn> <name>')
      .description('Extract a function from a code block')
      .option('-p, --preview', 'Preview changes without applying')
      .action(async (file, startLine, startColumn, endLine, endColumn, name, options) => {
        await this.executeCommand('refactor:extract-function', {
          file,
          startPosition: { line: parseInt(startLine), column: parseInt(startColumn) },
          endPosition: { line: parseInt(endLine), column: parseInt(endColumn) },
          functionName: name,
          ...options,
        });
      });
  }

  /**
   * Sets up cross-language operation commands.
   */
  private setupCrossLanguageCommands(): void {
    const crossCmd = this.program
      .command('cross-lang')
      .alias('xl')
      .description('Cross-language operation commands');

    crossCmd
      .command('convert <file> <targetLanguage>')
      .description('Convert code from one language to another')
      .option('-o, --output <file>', 'Output file path')
      .option('-s, --style <style>', 'Conversion style (literal|idiomatic|optimized)', 'idiomatic')
      .option('-p, --preview', 'Preview conversion without saving')
      .action(async (file, targetLanguage, options) => {
        await this.executeCommand('cross-lang:convert', {
          file,
          targetLanguage,
          ...options,
        });
      });

    crossCmd
      .command('extract-embedded <file> <line> <column>')
      .description('Extract embedded code to a separate file')
      .option('-o, --output <file>', 'Output file path')
      .option('-t, --type <type>', 'Extraction type (auto|script|style|template)')
      .action(async (file, line, column, options) => {
        await this.executeCommand('cross-lang:extract-embedded', {
          file,
          position: { line: parseInt(line), column: parseInt(column) },
          ...options,
        });
      });

    crossCmd
      .command('inline-embedded <file> <embeddedFile>')
      .description('Inline embedded code from a separate file')
      .option('-p, --position <line:column>', 'Position to inline at')
      .action(async (file, embeddedFile, options) => {
        await this.executeCommand('cross-lang:inline-embedded', {
          file,
          embeddedFile,
          ...options,
        });
      });

    crossCmd
      .command('sync-symbols <file1> <file2>')
      .description('Synchronize symbols between two files')
      .option('-b, --bidirectional', 'Bidirectional synchronization')
      .option('-t, --types', 'Synchronize type information')
      .action(async (file1, file2, options) => {
        await this.executeCommand('cross-lang:sync-symbols', {
          file1,
          file2,
          ...options,
        });
      });
  }

  /**
   * Sets up analysis commands.
   */
  private setupAnalysisCommands(): void {
    const analyzeCmd = this.program
      .command('analyze')
      .alias('an')
      .description('Code analysis commands');

    analyzeCmd
      .command('complexity <file>')
      .description('Analyze code complexity')
      .option('-m, --metrics <metrics>', 'Metrics to calculate (cyclomatic|cognitive|halstead)', 'cyclomatic')
      .action(async (file, options) => {
        await this.executeCommand('analyze:complexity', { file, ...options });
      });

    analyzeCmd
      .command('quality <file>')
      .description('Analyze code quality')
      .option('-r, --rules <rules>', 'Quality rules to apply')
      .action(async (file, options) => {
        await this.executeCommand('analyze:quality', { file, ...options });
      });

    analyzeCmd
      .command('dependencies <file>')
      .description('Analyze code dependencies')
      .option('-d, --depth <depth>', 'Analysis depth', '3')
      .action(async (file, options) => {
        await this.executeCommand('analyze:dependencies', { file, ...options });
      });

    analyzeCmd
      .command('context <file> <line> <column>')
      .description('Analyze context at a specific position')
      .option('-s, --scope <scope>', 'Context scope (local|function|class|file)', 'function')
      .action(async (file, line, column, options) => {
        await this.executeCommand('analyze:context', {
          file,
          position: { line: parseInt(line), column: parseInt(column) },
          ...options,
        });
      });
  }

  /**
   * Sets up MCP server commands.
   */
  private setupMCPCommands(): void {
    const mcpCmd = this.program
      .command('mcp')
      .description('MCP server commands');

    mcpCmd
      .command('start')
      .description('Start MCP server for AI agent communication')
      .option('-p, --port <port>', 'Server port', this.config.mcpPort.toString())
      .option('-h, --host <host>', 'Server host', this.config.mcpHost)
      .option('-d, --daemon', 'Run as daemon')
      .action(async (options) => {
        await this.executeCommand('mcp:start', options);
      });

    mcpCmd
      .command('stop')
      .description('Stop MCP server')
      .action(async (options) => {
        await this.executeCommand('mcp:stop', options);
      });

    mcpCmd
      .command('status')
      .description('Show MCP server status')
      .action(async (options) => {
        await this.executeCommand('mcp:status', options);
      });

    mcpCmd
      .command('clients')
      .description('List connected AI agent clients')
      .action(async (options) => {
        await this.executeCommand('mcp:clients', options);
      });
  }

  /**
   * Sets up utility commands.
   */
  private setupUtilityCommands(): void {
    this.program
      .command('init [directory]')
      .description('Initialize Minotaur in a directory')
      .option('-t, --template <template>', 'Project template')
      .action(async (directory, options) => {
        await this.executeCommand('init', { directory: directory || '.', ...options });
      });

    this.program
      .command('config')
      .description('Show current configuration')
      .option('-g, --global', 'Show global configuration')
      .action(async (options) => {
        await this.executeCommand('config', options);
      });

    this.program
      .command('validate <file>')
      .description('Validate a file using grammar rules')
      .option('-g, --grammar <grammar>', 'Specific grammar to use')
      .action(async (file, options) => {
        await this.executeCommand('validate', { file, ...options });
      });
  }

  /**
   * Executes a CLI command.
   */
  private async executeCommand(command: string, args: any): Promise<CLIResult> {
    const startTime = Date.now();

    try {
      let result: any;

      switch (command) {
        case 'language:detect':
          result = await this.detectLanguage(args);
          break;
        case 'language:list':
          result = await this.listLanguages(args);
          break;
        case 'language:config':
          result = await this.showLanguageConfig(args);
          break;
        case 'refactor:extract-variable':
          result = await this.extractVariable(args);
          break;
        case 'refactor:inline-variable':
          result = await this.inlineVariable(args);
          break;
        case 'refactor:rename':
          result = await this.renameSymbol(args);
          break;
        case 'refactor:extract-function':
          result = await this.extractFunction(args);
          break;
        case 'cross-lang:convert':
          result = await this.convertLanguage(args);
          break;
        case 'cross-lang:extract-embedded':
          result = await this.extractEmbedded(args);
          break;
        case 'cross-lang:inline-embedded':
          result = await this.inlineEmbedded(args);
          break;
        case 'cross-lang:sync-symbols':
          result = await this.syncSymbols(args);
          break;
        case 'analyze:complexity':
          result = await this.analyzeComplexity(args);
          break;
        case 'analyze:quality':
          result = await this.analyzeQuality(args);
          break;
        case 'analyze:dependencies':
          result = await this.analyzeDependencies(args);
          break;
        case 'analyze:context':
          result = await this.analyzeContext(args);
          break;
        case 'mcp:start':
          result = await this.startMCPServer(args);
          break;
        case 'mcp:stop':
          result = await this.stopMCPServer(args);
          break;
        case 'mcp:status':
          result = await this.getMCPStatus(args);
          break;
        case 'mcp:clients':
          result = await this.getMCPClients(args);
          break;
        case 'init':
          result = await this.initializeProject(args);
          break;
        case 'config':
          result = await this.showConfig(args);
          break;
        case 'validate':
          result = await this.validateFile(args);
          break;
        default:
          throw new Error(`Unknown command: ${command}`);
      }

      const cliResult: CLIResult = {
        success: true,
        operation: command,
        result,
        executionTime: Date.now() - startTime,
        outputFormat: this.config.outputFormat,
      };

      this.outputResult(cliResult);
      return cliResult;

    } catch (error) {
      const cliResult: CLIResult = {
        success: false,
        operation: command,
        error: error instanceof Error ? error.message : String(error),
        executionTime: Date.now() - startTime,
        outputFormat: this.config.outputFormat,
      };

      this.outputResult(cliResult);
      return cliResult;
    }
  }

  /**
   * Command implementations.
   */

  private async detectLanguage(args: any): Promise<any> {
    const content = fs.readFileSync(args.file, 'utf-8');
    const detection = this.languageManager.detectLanguage(args.file, content);

    if (args.embedded) {
      const embedded = this.languageManager.detectEmbeddedLanguages(content, detection.language);
      return { ...detection, embeddedLanguages: embedded };
    }

    return detection;
  }

  private async listLanguages(args: any): Promise<any> {
    const languages = this.languageManager.getSupportedLanguages();

    if (args.detailed) {
      return languages.map(lang => ({
        language: lang,
        config: this.languageManager.getLanguageConfig(lang),
      }));
    }

    return { supportedLanguages: languages };
  }

  private async showLanguageConfig(args: any): Promise<any> {
    const config = this.languageManager.getLanguageConfig(args.language as SupportedLanguage);
    if (!config) {
      throw new Error(`Language not supported: ${args.language}`);
    }
    return config;
  }

  private async extractVariable(args: any): Promise<any> {
    const result = await this.refactoringEngine.extractVariable(
      args.file,
      args.position,
      args.endPosition,
      args.variableName,
      { preview: args.preview },
    );

    if (!args.preview && result.success) {
      await this.refactoringEngine.applyChanges(result.changes);
    }

    return result;
  }

  private async inlineVariable(args: any): Promise<any> {
    const result = await this.refactoringEngine.inlineVariable(
      args.file,
      args.position,
      args.variableName,
      { preview: args.preview },
    );

    if (!args.preview && result.success) {
      await this.refactoringEngine.applyChanges(result.changes);
    }

    return result;
  }

  private async renameSymbol(args: any): Promise<any> {
    const result = await this.refactoringEngine.renameSymbol(
      args.file,
      args.position,
      args.oldName,
      args.newName,
      { scope: args.scope, preview: args.preview },
    );

    if (!args.preview && result.success) {
      await this.refactoringEngine.applyChanges(result.changes);
    }

    return result;
  }

  private async extractFunction(args: any): Promise<any> {
    const result = await this.refactoringEngine.extractFunction(
      args.file,
      args.startPosition,
      args.endPosition,
      args.functionName,
      { preview: args.preview },
    );

    if (!args.preview && result.success) {
      await this.refactoringEngine.applyChanges(result.changes);
    }

    return result;
  }

  private async convertLanguage(args: any): Promise<any> {
    const content = fs.readFileSync(args.file, 'utf-8');
    const sourceLanguage = this.languageManager.detectLanguage(args.file, content).language;

    const request = {
      id: this.generateId(),
      type: CrossLanguageOperationType.CONVERT_LANGUAGE,
      sourceFile: args.file,
      sourceLanguage,
      targetLanguage: args.targetLanguage as SupportedLanguage,
      position: { line: 1, column: 1, offset: 0 },
      parameters: {
        conversionOptions: {
          targetLanguage: args.targetLanguage,
          conversionStyle: args.style,
          preserveStructure: true,
          handleUnsupportedFeatures: 'comment' as 'error' | 'comment' | 'approximate',
          addTypeAnnotations: true,
          modernizeSyntax: true,
        },
      },
      scope: { type: 'file' as 'function' | 'file' | 'class' | 'project' | 'selection', includeEmbedded: true, includeReferences: false, crossFileReferences: false },
    };

    const result = await this.crossLanguageOps.executeOperation(request);

    if (args.output && result.success) {
      // Save converted code to output file
      const convertedCode = result.changes[0]?.newText || '';
      fs.writeFileSync(args.output, convertedCode);
    }

    return result;
  }

  private async extractEmbedded(args: any): Promise<any> {
    const content = fs.readFileSync(args.file, 'utf-8');
    const sourceLanguage = this.languageManager.detectLanguage(args.file, content).language;

    const request = {
      id: this.generateId(),
      type: CrossLanguageOperationType.EXTRACT_EMBEDDED_CODE,
      sourceFile: args.file,
      sourceLanguage,
      position: args.position,
      parameters: {
        newFileName: args.output,
        extractionOptions: {
          extractionType: args.type || 'auto',
          targetFile: args.output || '',
          includeImports: true,
          includeDependencies: true,
          generateInterface: false,
        },
      },
      scope: { type: 'selection' as 'function' | 'file' | 'class' | 'project' | 'selection', includeEmbedded: true, includeReferences: false, crossFileReferences: false },
    };

    return await this.crossLanguageOps.executeOperation(request);
  }

  private async inlineEmbedded(args: any): Promise<any> {
    const content = fs.readFileSync(args.file, 'utf-8');
    const sourceLanguage = this.languageManager.detectLanguage(args.file, content).language;

    const request = {
      id: this.generateId(),
      type: CrossLanguageOperationType.INLINE_EMBEDDED_CODE,
      sourceFile: args.file,
      sourceLanguage,
      position: args.position || { line: 1, column: 1, offset: 0 },
      parameters: {
        newFileName: args.embeddedFile,
        preserveComments: true,
        preserveFormatting: true,
        createBackup: false,
      } as CrossLanguageParameters,
      scope: { type: 'file' as 'function' | 'file' | 'class' | 'project' | 'selection', includeEmbedded: true, includeReferences: false, crossFileReferences: false },
    };

    return await this.crossLanguageOps.executeOperation(request);
  }

  private async syncSymbols(args: any): Promise<any> {
    const content1 = fs.readFileSync(args.file1, 'utf-8');
    const sourceLanguage = this.languageManager.detectLanguage(args.file1, content1).language;

    const request = {
      id: this.generateId(),
      type: CrossLanguageOperationType.SYNCHRONIZE_SYMBOLS,
      sourceFile: args.file1,
      sourceLanguage,
      position: { line: 1, column: 1, offset: 0 },
      parameters: {
        targetFile: args.file2,
        synchronizationOptions: {
          synchronizeNames: true,
          synchronizeTypes: args.types,
          synchronizeComments: true,
          bidirectional: args.bidirectional,
          conflictResolution: 'merge' as 'source' | 'merge' | 'target' | 'prompt',
        },
      },
      scope: { type: 'file' as 'function' | 'file' | 'class' | 'project' | 'selection', includeEmbedded: true, includeReferences: true, crossFileReferences: true },
    };

    return await this.crossLanguageOps.executeOperation(request);
  }

  private async analyzeComplexity(args: any): Promise<any> {
    // Mock implementation - would integrate with actual complexity analysis
    return {
      file: args.file,
      metrics: args.metrics,
      complexity: {
        cyclomatic: 5,
        cognitive: 3,
        halstead: { volume: 100, difficulty: 2 },
      },
    };
  }

  private async analyzeQuality(args: any): Promise<any> {
    // Mock implementation - would integrate with actual quality analysis
    return {
      file: args.file,
      quality: {
        score: 85,
        issues: [],
        suggestions: [],
      },
    };
  }

  private async analyzeDependencies(args: any): Promise<any> {
    // Mock implementation - would integrate with actual dependency analysis
    return {
      file: args.file,
      dependencies: {
        direct: [],
        transitive: [],
        circular: [],
      },
    };
  }

  private async analyzeContext(args: any): Promise<any> {
    const context = this.contextManager.getContextAt(args.file, args.position);
    return {
      file: args.file,
      position: args.position,
      context: context || { message: 'No context available' },
    };
  }

  private async startMCPServer(args: any): Promise<any> {
    if (!this.mcpServer) {
      this.config.enableMCP = true;
      this.config.mcpPort = parseInt(args.port) || this.config.mcpPort;
      this.config.mcpHost = args.host || this.config.mcpHost;
      this.initializeMCPComponents();
    }

    if (this.mcpServer) {
      await this.mcpServer.start();
      return {
        status: 'started',
        host: this.config.mcpHost,
        port: this.config.mcpPort,
        daemon: args.daemon,
      };
    }

    throw new Error('Failed to initialize MCP server');
  }

  private async stopMCPServer(_args: any): Promise<any> {
    if (this.mcpServer) {
      await this.mcpServer.stop();
      return { status: 'stopped' };
    }

    return { status: 'not_running' };
  }

  private async getMCPStatus(_args: any): Promise<any> {
    if (this.mcpServer && this.mcpConnectionManager) {
      const stats = this.mcpConnectionManager.getStatistics();
      return {
        status: 'running',
        host: this.config.mcpHost,
        port: this.config.mcpPort,
        connections: stats.activeConnections,
        totalMessages: stats.totalMessages,
        uptime: Date.now() - (stats.lastConnectionTime || Date.now()),
      };
    }

    return { status: 'not_running' };
  }

  private async getMCPClients(_args: any): Promise<any> {
    if (this.mcpConnectionManager) {
      const connections = this.mcpConnectionManager.getActiveConnections();
      return {
        clients: connections.map(conn => ({
          id: conn.id,
          agentId: conn.agentId,
          agentName: conn.agentName,
          connectionTime: conn.connectionTime,
          messageCount: conn.messageCount,
          isAuthenticated: conn.isAuthenticated,
        })),
      };
    }

    return { clients: [] };
  }

  private async initializeProject(args: any): Promise<any> {
    const directory = path.resolve(args.directory);

    // Create .minotaur directory
    const configDir = path.join(directory, '.minotaur');
    if (!fs.existsSync(configDir)) {
      fs.mkdirSync(configDir, { recursive: true });
    }

    // Create default configuration
    const defaultConfig = {
      version: '1.0.0',
      languages: this.languageManager.getSupportedLanguages(),
      refactoring: {
        enablePreview: true,
        enableUndo: true,
        confidenceThreshold: 0.7,
      },
      mcp: {
        enabled: false,
        port: 8080,
        host: 'localhost',
      },
    };

    const configFile = path.join(configDir, 'config.json');
    fs.writeFileSync(configFile, JSON.stringify(defaultConfig, null, 2));

    return {
      directory,
      configFile,
      template: args.template,
      initialized: true,
    };
  }

  private async showConfig(_args: any): Promise<any> {
    return {
      config: this.config,
      supportedLanguages: this.languageManager.getSupportedLanguages(),
      mcpEnabled: !!this.mcpServer,
    };
  }

  private async validateFile(args: any): Promise<any> {
    const content = fs.readFileSync(args.file, 'utf-8');
    const detection = this.languageManager.detectLanguage(args.file, content);

    // Mock validation - would integrate with actual grammar validation
    return {
      file: args.file,
      language: detection.language,
      valid: true,
      errors: [],
      warnings: [],
    };
  }

  /**
   * Outputs the result in the specified format.
   */
  private outputResult(result: CLIResult): void {
    switch (this.config.outputFormat) {
      case 'json':
    // eslint-disable-next-line no-console
        console.log(JSON.stringify(result, null, 2));
        break;
      case 'yaml':
        // Would use a YAML library in practice
    // eslint-disable-next-line no-console
        console.log('# YAML output not implemented');
    // eslint-disable-next-line no-console
        console.log(JSON.stringify(result, null, 2));
        break;
      default:
        this.outputText(result);
        break;
    }
  }

  /**
   * Outputs the result in text format.
   */
  private outputText(result: CLIResult): void {
    if (result.success) {
    // eslint-disable-next-line no-console
      console.log(`✓ ${result.operation} completed in ${result.executionTime}ms`);
      if (result.result) {
    // eslint-disable-next-line no-console
        console.log(JSON.stringify(result.result, null, 2));
      }
      if (result.warnings && result.warnings.length > 0) {
    // eslint-disable-next-line no-console
        console.log('Warnings:');
    // eslint-disable-next-line no-console
        result.warnings.forEach(warning => console.log(`  ⚠ ${warning}`));
      }
    } else {
    // eslint-disable-next-line no-console
      console.error(`✗ ${result.operation} failed: ${result.error}`);
    }
  }

  /**
   * Runs the CLI with the provided arguments.
   */
  public async run(argv: string[]): Promise<void> {
    try {
      await this.program.parseAsync(argv);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('CLI Error:', error instanceof Error ? error.message : String(error));
      process.exit(1);
    }
  }

  /**
   * Helper methods.
   */

  private generateId(): string {
    return `cli_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

// Export the CLI class
export default MinotaurCLI;

// CLI entry point
if (require.main === module) {
  const cli = new MinotaurCLI();
  cli.run(process.argv).catch(error => {
    // eslint-disable-next-line no-console
    console.error('Fatal error:', error);
    process.exit(1);
  });
}

