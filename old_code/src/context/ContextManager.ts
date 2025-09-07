/// <reference types="node" />

/**
 * Context manager for coordinating context-aware parsing and analysis in Minotaur.
 * Integrates with the MCP protocol to provide real-time context information to AI agents.
 */

import { ContextAwareParser, ContextInfo, ScopeInfo, SymbolInfo, ParseStateInfo, TextChange, CodePosition } from './ContextAwareParser';
import { SymbolTable } from './SymbolTable';
import { MCPProtocolHandler, MCPContext, MCPMessage, MCPNotification } from '../mcp/MCPProtocol';
import { Interpreter } from '../utils/Interpreter';
import { EventEmitter } from 'events';

/**
 * Context manager configuration.
 */
export interface ContextManagerConfig {
  enableRealTime: boolean;
  enableMCPIntegration: boolean;
  maxContextHistory: number;
  contextUpdateInterval: number;
  enablePerformanceMonitoring: boolean;
  enableContextCaching: boolean;
}

/**
 * Context snapshot for history tracking.
 */
export interface ContextSnapshot {
  id: string;
  timestamp: number;
  file: string;
  position: CodePosition;
  context: ContextInfo;
  parseState: ParseStateInfo;
  changesSinceLastSnapshot: number;
}

/**
 * Context analysis result.
 */
export interface ContextAnalysisResult {
  complexity: number;
  depth: number;
  symbolDensity: number;
  errorCount: number;
  warningCount: number;
  suggestions: string[];
  performance: PerformanceMetrics;
  contextRules: string[]; // Added for compatibility
  symbolTable?: Map<string, any>; // Added for compatibility
  scopeHierarchy?: any[]; // Added for compatibility
}

/**
 * Performance metrics for context operations.
 */
export interface PerformanceMetrics {
  parseTime: number;
  analysisTime: number;
  memoryUsage: number;
  cacheHitRate: number;
  totalOperations: number;
}

/**
 * Context cache entry.
 */
interface ContextCacheEntry {
  key: string;
  context: ContextInfo;
  timestamp: number;
  accessCount: number;
  lastAccess: number;
}

/**
 * Context manager for coordinating parsing and analysis operations.
 */
export class ContextManager extends EventEmitter {
  private config: ContextManagerConfig;
  private parser: ContextAwareParser;
  private symbolTable: SymbolTable;
  private mcpHandler: MCPProtocolHandler | null;
  private interpreter: Interpreter;

  private contextHistory: ContextSnapshot[];
  private contextCache: Map<string, ContextCacheEntry>;
  private activeFiles: Map<string, ContextInfo>;
  private performanceMetrics: PerformanceMetrics;

  private updateTimer: ReturnType<typeof setTimeout> | null;
  private isActive: boolean;
  private lastUpdateTime: number;
  
  // Static flag to prevent circular instantiation
  private static _isInitializing: boolean = false;

  constructor(config?: Partial<ContextManagerConfig>, interpreter?: Interpreter) {
    // Call super constructor first
    super();
    
    // Guard against circular instantiation
    if (ContextManager._isInitializing) {
      throw new Error('Circular ContextManager instantiation detected');
    }
    ContextManager._isInitializing = true;

    this.config = {
      enableRealTime: true,
      enableMCPIntegration: true,
      maxContextHistory: 100,
      contextUpdateInterval: 1000, // 1 second
      enablePerformanceMonitoring: true,
      enableContextCaching: true,
      ...config,
    };

    // Use provided interpreter or create a minimal one to avoid circular dependency
    if (interpreter) {
      this.interpreter = interpreter;
    } else {
      // Create a minimal interpreter without full initialization to avoid circular dependency
      this.interpreter = Object.create(Interpreter.prototype);
      // Initialize only essential properties
      // Note: grammarContainer and sourceCodeContainer are private, skip initialization
    }
    
    this.parser = new ContextAwareParser(this.interpreter);
    this.symbolTable = new SymbolTable();
    this.mcpHandler = null;

    this.contextHistory = [];
    this.contextCache = new Map();
    this.activeFiles = new Map();
    this.performanceMetrics = {
      parseTime: 0,
      analysisTime: 0,
      memoryUsage: 0,
      cacheHitRate: 0,
      totalOperations: 0,
    };

    this.updateTimer = null;
    this.isActive = false;
    this.lastUpdateTime = Date.now();
    
    // Clear the initialization flag
    ContextManager._isInitializing = false;
    
    this.setupEventHandlers();
    this.initializeRealTimeMode();
  }

  /**
   * Sets up event handlers for parser and symbol table.
   */
  private setupEventHandlers(): void {
    // Parser events
    this.parser.on('parse_complete', this.handleParseComplete.bind(this));
    this.parser.on('incremental_update', this.handleIncrementalUpdate.bind(this));
    this.parser.on('context_changed', this.handleContextChanged.bind(this));
    this.parser.on('scope_entered', this.handleScopeEntered.bind(this));
    this.parser.on('scope_exited', this.handleScopeExited.bind(this));
    this.parser.on('symbol_added', this.handleSymbolAdded.bind(this));

    // Symbol table events
    this.symbolTable.on('symbol_declared', this.handleSymbolDeclared.bind(this));
    this.symbolTable.on('symbol_updated', this.handleSymbolUpdated.bind(this));
    this.symbolTable.on('reference_added', this.handleReferenceAdded.bind(this));
  }

  /**
   * Initializes real-time mode if enabled.
   */
  private initializeRealTimeMode(): void {
    if (this.config.enableRealTime) {
      this.startRealTimeUpdates();
    }
  }

  /**
   * Starts the context manager.
   */
  public async start(): Promise<void> {
    if (this.isActive) {
      return;
    }

    try {
      // Initialize MCP integration if enabled
      if (this.config.enableMCPIntegration) {
        await this.initializeMCPIntegration();
      }

      // Enable real-time mode on parser and symbol table
      this.parser.setRealTimeMode(this.config.enableRealTime);
      this.symbolTable.setRealTimeMode(this.config.enableRealTime);

      this.isActive = true;
      this.emit('manager_started');

    } catch (error) {
      this.emit('manager_error', { error, operation: 'start' });
      throw error;
    }
  }

  /**
   * Stops the context manager.
   */
  public async stop(): Promise<void> {
    if (!this.isActive) {
      return;
    }

    try {
      // Stop real-time updates
      this.stopRealTimeUpdates();

      // Clear caches
      this.clearCaches();

      this.isActive = false;
      this.emit('manager_stopped');

    } catch (error) {
      this.emit('manager_error', { error, operation: 'stop' });
      throw error;
    }
  }

  /**
   * Parses a file and creates context information.
   */
  public async parseFile(file: string, code: string, language: string): Promise<ContextInfo> {
    const startTime = Date.now();

    try {
      // Parse the code
      const parseState = await this.parser.parseCode(code, language, file);

      // Get current context
      const context = this.parser.getCurrentContext();

      // Store in active files
      this.activeFiles.set(file, context);

      // Create snapshot
      if (this.config.maxContextHistory > 0) {
        this.createContextSnapshot(file, context, parseState);
      }

      // Update performance metrics
      this.updatePerformanceMetrics('parse', Date.now() - startTime);

      // Notify MCP if enabled
      if (this.mcpHandler) {
        await this.notifyMCPContextChanged(file, context);
      }

      this.emit('file_parsed', { file, context, parseState });
      return context;

    } catch (error) {
      this.emit('parse_error', { file, error });
      throw error;
    }
  }

  /**
   * Performs incremental parsing for a file change.
   */
  public async updateFile(file: string, change: TextChange): Promise<ContextInfo> {
    const startTime = Date.now();

    try {
      // Perform incremental parsing
      const parseState = await this.parser.parseIncremental(change, file);

      // Get updated context
      const context = this.parser.getCurrentContext();

      // Update active files
      this.activeFiles.set(file, context);

      // Update cache if enabled
      if (this.config.enableContextCaching) {
        this.updateContextCache(file, context);
      }

      // Update performance metrics
      this.updatePerformanceMetrics('incremental', Date.now() - startTime);

      // Notify MCP if enabled
      if (this.mcpHandler) {
        await this.notifyMCPContextChanged(file, context);
      }

      this.emit('file_updated', { file, change, context, parseState });
      return context;

    } catch (error) {
      this.emit('update_error', { file, change, error });
      throw error;
    }
  }

  /**
   * Gets context information for a specific file and position.
   */
  public getContextAt(file: string, position: CodePosition): ContextInfo | null {
    const startTime = Date.now();

    try {
      // Check cache first
      if (this.config.enableContextCaching) {
        const cached = this.getFromContextCache(file, position);
        if (cached) {
          this.updatePerformanceMetrics('cache_hit', Date.now() - startTime);
          return cached;
        }
      }

      // Get context from parser
      const context = this.parser.getContextAt(position);

      // Cache the result
      if (this.config.enableContextCaching && context) {
        this.addToContextCache(file, position, context);
      }

      this.updatePerformanceMetrics('context_lookup', Date.now() - startTime);
      return context;

    } catch (error) {
      this.emit('context_error', { file, position, error });
      return null;
    }
  }

  /**
   * Gets all symbols visible from a specific position.
   */
  public getVisibleSymbols(file: string, position: CodePosition): SymbolInfo[] {
    try {
      const context = this.getContextAt(file, position);
      if (!context || !context.scope) {
        return [];
      }

      return this.symbolTable.getVisibleSymbols(position, context.scope.id);

    } catch (error) {
      this.emit('symbols_error', { file, position, error });
      return [];
    }
  }

  /**
   * Resolves a symbol at a specific position.
   */
  public resolveSymbol(file: string, position: CodePosition, name: string): SymbolInfo | null {
    try {
      const context = this.getContextAt(file, position);
      if (!context || !context.scope) {
        return null;
      }

      const resolution = this.symbolTable.resolveSymbol(name, context.scope.id, position);
      return resolution.symbol;

    } catch (error) {
      this.emit('resolution_error', { file, position, name, error });
      return null;
    }
  }

  /**
   * Analyzes the context at a specific position.
   */
  public analyzeContext(file: string, position: CodePosition): ContextAnalysisResult {
    const startTime = Date.now();

    try {
      const context = this.getContextAt(file, position);
      if (!context) {
        throw new Error('No context available for analysis');
      }

      const analysis: ContextAnalysisResult = {
        complexity: this.calculateComplexity(context),
        depth: context.scopeStack.length,
        symbolDensity: this.calculateSymbolDensity(context),
        errorCount: context.parseState.errors.length,
        warningCount: context.parseState.warnings.length,
        suggestions: this.generateSuggestions(context),
        performance: { ...this.performanceMetrics },
        contextRules: [], // Initialize empty context rules
        symbolTable: new Map(), // Initialize empty symbol table
        scopeHierarchy: [], // Initialize empty scope hierarchy
      };

      this.updatePerformanceMetrics('analysis', Date.now() - startTime);
      this.emit('context_analyzed', { file, position, analysis });

      return analysis;

    } catch (error) {
      this.emit('analysis_error', { file, position, error });
      throw error;
    }
  }

  /**
   * Gets the context history for a file.
   */
  public getContextHistory(file: string): ContextSnapshot[] {
    return this.contextHistory.filter(snapshot => snapshot.file === file);
  }

  /**
   * Gets performance metrics.
   */
  public getPerformanceMetrics(): PerformanceMetrics {
    return { ...this.performanceMetrics };
  }

  /**
   * Clears all context data for a file.
   */
  public clearFile(file: string): void {
    // Remove from active files
    this.activeFiles.delete(file);

    // Remove from cache
    for (const key of this.contextCache.keys()) {
      if (key.startsWith(`${file}:`)) {
        this.contextCache.delete(key);
      }
    }

    // Remove from history
    this.contextHistory = this.contextHistory.filter(snapshot => snapshot.file !== file);

    this.emit('file_cleared', { file });
  }

  /**
   * Sets the MCP protocol handler.
   */
  public setMCPHandler(handler: MCPProtocolHandler): void {
    this.mcpHandler = handler;
    this.emit('mcp_handler_set', { handler });
  }

  /**
   * Private helper methods.
   */

  private async initializeMCPIntegration(): Promise<void> {
    if (!this.mcpHandler) {
      this.mcpHandler = new MCPProtocolHandler();
    }

    // Set up MCP event handlers
    // Note: MCPProtocolHandler currently doesn't implement EventEmitter
    // this.mcpHandler.on?.('context_request', this.handleMCPContextRequest.bind(this));
  }

  private startRealTimeUpdates(): void {
    if (this.updateTimer) {
      return;
    }

    this.updateTimer = setInterval(() => {
      this.performRealTimeUpdate();
    }, this.config.contextUpdateInterval);
  }

  private stopRealTimeUpdates(): void {
    if (this.updateTimer) {
      clearInterval(this.updateTimer);
      this.updateTimer = null;
    }
  }

  private performRealTimeUpdate(): void {
    // Update performance metrics
    if (this.config.enablePerformanceMonitoring) {
      this.updateMemoryUsage();
      this.updateCacheHitRate();
    }

    // Emit real-time update event
    this.emit('real_time_update', {
      timestamp: Date.now(),
      activeFiles: this.activeFiles.size,
      cacheSize: this.contextCache.size,
      historySize: this.contextHistory.length,
      performance: this.performanceMetrics,
    });
  }

  private createContextSnapshot(file: string, context: ContextInfo, parseState: ParseStateInfo): void {
    const snapshot: ContextSnapshot = {
      id: `${file}_${Date.now()}`,
      timestamp: Date.now(),
      file,
      position: parseState.position,
      context: { ...context },
      parseState: { ...parseState },
      changesSinceLastSnapshot: 0,
    };

    this.contextHistory.push(snapshot);

    // Limit history size
    if (this.contextHistory.length > this.config.maxContextHistory) {
      this.contextHistory.shift();
    }
  }

  private updateContextCache(file: string, context: ContextInfo): void {
    const key = `${file}:${context.timestamp}`;
    const entry: ContextCacheEntry = {
      key,
      context,
      timestamp: Date.now(),
      accessCount: 1,
      lastAccess: Date.now(),
    };

    this.contextCache.set(key, entry);

    // Limit cache size (simple LRU)
    if (this.contextCache.size > 1000) {
      const oldestKey = Array.from(this.contextCache.entries())
        .sort((a, b) => a[1].lastAccess - b[1].lastAccess)[0][0];
      this.contextCache.delete(oldestKey);
    }
  }

  private getFromContextCache(file: string, position: CodePosition): ContextInfo | null {
    // Simple cache lookup - in practice, this would be more sophisticated
    for (const [key, entry] of this.contextCache) {
      if (key.startsWith(`${file}:`)) {
        entry.accessCount++;
        entry.lastAccess = Date.now();
        return entry.context;
      }
    }
    return null;
  }

  private addToContextCache(file: string, position: CodePosition, context: ContextInfo): void {
    const key = `${file}:${position.line}:${position.column}`;
    const entry: ContextCacheEntry = {
      key,
      context,
      timestamp: Date.now(),
      accessCount: 1,
      lastAccess: Date.now(),
    };

    this.contextCache.set(key, entry);
  }

  private clearCaches(): void {
    this.contextCache.clear();
    this.contextHistory = [];
  }

  private calculateComplexity(context: ContextInfo): number {
    // Simple complexity calculation based on scope depth and symbol count
    const scopeComplexity = context.scopeStack.length * 2;
    const symbolComplexity = context.symbols.length * 0.5;
    const errorComplexity = context.parseState.errors.length * 3;

    return scopeComplexity + symbolComplexity + errorComplexity;
  }

  private calculateSymbolDensity(context: ContextInfo): number {
    if (!context.scope) {
      return 0;
    }

    const scopeSize = (context.scope.endPosition.line - context.scope.startPosition.line) + 1;
    return context.symbols.length / Math.max(1, scopeSize);
  }

  private generateSuggestions(context: ContextInfo): string[] {
    const suggestions: string[] = [];

    // Add suggestions based on context analysis
    if (context.parseState.errors.length > 0) {
      suggestions.push('Fix syntax errors to improve code quality');
    }

    if (context.scopeStack.length > 5) {
      suggestions.push('Consider refactoring deeply nested code');
    }

    if (context.symbols.length > 20) {
      suggestions.push('Consider breaking down large scopes');
    }

    return suggestions;
  }

  private updatePerformanceMetrics(operation: string, duration: number): void {
    this.performanceMetrics.totalOperations++;

    switch (operation) {
      case 'parse':
        this.performanceMetrics.parseTime = duration;
        break;
      case 'analysis':
        this.performanceMetrics.analysisTime = duration;
        break;
      case 'cache_hit':
        // Update cache hit rate
        break;
    }
  }

  private updateMemoryUsage(): void {
    // Simple memory usage estimation
    const symbolTableSize = this.symbolTable.getStatistics().totalSymbols * 100; // bytes per symbol
    const cacheSize = this.contextCache.size * 500; // bytes per cache entry
    const historySize = this.contextHistory.length * 1000; // bytes per snapshot

    this.performanceMetrics.memoryUsage = symbolTableSize + cacheSize + historySize;
  }

  private updateCacheHitRate(): void {
    // Calculate cache hit rate based on access patterns
    let totalAccesses = 0;
    let hits = 0;

    for (const entry of this.contextCache.values()) {
      totalAccesses += entry.accessCount;
      if (entry.accessCount > 1) {
        hits += entry.accessCount - 1;
      }
    }

    this.performanceMetrics.cacheHitRate = totalAccesses > 0 ? hits / totalAccesses : 0;
  }

  private async notifyMCPContextChanged(file: string, context: ContextInfo): Promise<void> {
    if (!this.mcpHandler) {
      return;
    }

    const mcpContext: MCPContext = {
      file,
      language: context.activeGrammar || 'unknown',
      position: context.parseState.position,
      scope: context.scope ? {
        type: context.scope.type,
        name: context.scope.name,
        startPosition: context.scope.startPosition,
        endPosition: context.scope.endPosition,
        parent: context.scope.parent ? {
          type: context.scope.parent.type,
          name: context.scope.parent.name,
          startPosition: context.scope.parent.startPosition,
          endPosition: context.scope.parent.endPosition,
          children: [],
          variables: [],
        } : undefined,
        children: [],
        variables: context.scope.variables,
      } : {
        type: 'global',
        startPosition: { line: 1, column: 1, offset: 0 },
        endPosition: { line: 1, column: 1, offset: 0 },
        children: [],
        variables: [],
      },
      symbols: context.symbols,
      grammar: {
        name: context.parseState.grammarState.activeGrammar,
        formatType: context.parseState.grammarState.formatType,
        baseGrammars: context.parseState.grammarState.baseGrammars,
        rules: [],
        activeContexts: context.parseState.grammarState.contextModifiers,
      },
      parseState: context.parseState,
    };

    await this.mcpHandler.notifyContextChanged(mcpContext);
  }

  /**
   * Event handlers.
   */

  private handleParseComplete(event: any): void {
    this.emit('parse_complete', event);
  }

  private handleIncrementalUpdate(event: any): void {
    this.emit('incremental_update', event);
  }

  private handleContextChanged(event: any): void {
    this.emit('context_changed', event);
  }

  private handleScopeEntered(event: any): void {
    this.symbolTable.registerScope(event.scope);
    this.emit('scope_entered', event);
  }

  private handleScopeExited(event: any): void {
    this.emit('scope_exited', event);
  }

  private handleSymbolAdded(event: any): void {
    this.symbolTable.declareSymbol(event.symbol);
    this.emit('symbol_added', event);
  }

  private handleSymbolDeclared(event: any): void {
    this.emit('symbol_declared', event);
  }

  private handleSymbolUpdated(event: any): void {
    this.emit('symbol_updated', event);
  }

  private handleReferenceAdded(event: any): void {
    this.emit('reference_added', event);
  }

  private async handleMCPContextRequest(message: MCPMessage): Promise<void> {
    // Handle MCP context requests from AI agents
    const { file, position } = (message as any).payload;

    try {
      const context = this.getContextAt(file, position);
      if (context && this.mcpHandler) {
        await this.notifyMCPContextChanged(file, context);
      }
    } catch (error) {
      this.emit('mcp_error', { message, error });
    }
  }
}

/**
 * Context manager factory for creating specialized managers.
 */
export class ContextManagerFactory {
  /**
   * Creates a context manager optimized for real-time operations.
   */
  public static createRealTimeManager(interpreter?: Interpreter): ContextManager {
    return new ContextManager({
      enableRealTime: true,
      enableMCPIntegration: true,
      contextUpdateInterval: 500, // 0.5 seconds
      enablePerformanceMonitoring: true,
      enableContextCaching: true,
    }, interpreter);
  }

  /**
   * Creates a context manager for batch processing.
   */
  public static createBatchManager(interpreter?: Interpreter): ContextManager {
    return new ContextManager({
      enableRealTime: false,
      enableMCPIntegration: false,
      maxContextHistory: 0,
      enablePerformanceMonitoring: false,
      enableContextCaching: false,
    }, interpreter);
  }

  /**
   * Creates a context manager for testing.
   */
  public static createTestManager(interpreter?: Interpreter): ContextManager {
    return new ContextManager({
      enableRealTime: false,
      enableMCPIntegration: false,
      maxContextHistory: 10,
      enablePerformanceMonitoring: true,
      enableContextCaching: true,
    }, interpreter);
  }
}

// Export the main context manager class as default
export default ContextManager;

