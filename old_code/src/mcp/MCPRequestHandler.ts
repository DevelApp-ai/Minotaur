/**
 * MCP request handler for processing specific AI agent operations.
 * Handles context requests, refactoring operations, analysis requests, and validation.
 */

import {
  MCPMessage,
  MCPRequest,
  MCPResponse,
  MCPMessageType,
  RefactorRequest,
  RefactorResult,
  AnalysisRequest,
  AnalysisResult,
  CodeLocation,
  MCPContext,
} from './MCPProtocol';
import { ContextManager } from '../context/ContextManager';
import { ContextInfo, CodePosition } from '../context/ContextAwareParser';
import { EventEmitter } from 'events';

/**
 * Request processing result.
 */
export interface RequestProcessingResult {
  success: boolean;
  data?: any;
  error?: string;
  processingTime: number;
  requestId: string;
}

/**
 * Request handler configuration.
 */
export interface RequestHandlerConfig {
  enableValidation: boolean;
  enableCaching: boolean;
  maxConcurrentRequests: number;
  requestTimeout: number;
  enableMetrics: boolean;
}

/**
 * Request metrics.
 */
export interface RequestMetrics {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  averageProcessingTime: number;
  requestsByType: Map<MCPMessageType, number>;
  lastRequestTime: number;
}

/**
 * Cached request result.
 */
interface CachedResult {
  requestHash: string;
  result: any;
  timestamp: number;
  accessCount: number;
}

/**
 * MCP request handler for processing AI agent operations.
 */
export class MCPRequestHandler extends EventEmitter {
  private contextManager: ContextManager;
  private config: RequestHandlerConfig;
  private activeRequests: Map<string, Promise<any>>;
  private requestCache: Map<string, CachedResult>;
  private metrics: RequestMetrics;

  constructor(contextManager: ContextManager, config: Partial<RequestHandlerConfig> = {}) {
    super();

    this.contextManager = contextManager;
    this.config = {
      enableValidation: true,
      enableCaching: true,
      maxConcurrentRequests: 10,
      requestTimeout: 30000, // 30 seconds
      enableMetrics: true,
      ...config,
    };

    this.activeRequests = new Map();
    this.requestCache = new Map();
    this.metrics = {
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      averageProcessingTime: 0,
      requestsByType: new Map(),
      lastRequestTime: 0,
    };

    this.setupEventHandlers();
  }

  /**
   * Sets up event handlers for the context manager.
   */
  private setupEventHandlers(): void {
    this.contextManager.on('context_changed', this.handleContextChanged.bind(this));
    this.contextManager.on('file_parsed', this.handleFileParsed.bind(this));
    this.contextManager.on('file_updated', this.handleFileUpdated.bind(this));
  }

  /**
   * Processes an MCP request from an AI agent.
   */
  public async processRequest(request: MCPRequest): Promise<MCPResponse> {
    const startTime = Date.now();

    try {
      // Validate request
      if (this.config.enableValidation) {
        this.validateRequest(request);
      }

      // Check concurrent request limit
      if (this.activeRequests.size >= this.config.maxConcurrentRequests) {
        throw new Error('Maximum concurrent requests exceeded');
      }

      // Check cache
      if (this.config.enableCaching) {
        const cached = this.getCachedResult(request);
        if (cached) {
          this.updateMetrics(request.type, Date.now() - startTime, true);
          return this.createSuccessResponse(request.id, cached.result);
        }
      }

      // Process request
      const processingPromise = this.executeRequest(request);
      this.activeRequests.set(request.id, processingPromise);

      // Set timeout
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Request timeout')), this.config.requestTimeout);
      });

      const result = await Promise.race([processingPromise, timeoutPromise]);

      // Cache result
      if (this.config.enableCaching && result) {
        this.cacheResult(request, result);
      }

      // Update metrics
      this.updateMetrics(request.type, Date.now() - startTime, true);

      // Clean up
      this.activeRequests.delete(request.id);

      return this.createSuccessResponse(request.id, result);

    } catch (error) {
      // Update metrics
      this.updateMetrics(request.type, Date.now() - startTime, false);

      // Clean up
      this.activeRequests.delete(request.id);

      this.emit('request_error', { request, error });
      return this.createErrorResponse(request.id, 'REQUEST_PROCESSING_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Executes a specific request based on its type.
   */
  private async executeRequest(request: MCPRequest): Promise<any> {
    switch (request.type) {
      case MCPMessageType.REQUEST_CONTEXT:
        return this.handleContextRequest(request);

      case MCPMessageType.REQUEST_REFACTOR:
        return this.handleRefactorRequest(request);

      case MCPMessageType.REQUEST_ANALYZE:
        return this.handleAnalysisRequest(request);

      case MCPMessageType.REQUEST_VALIDATE:
        return this.handleValidationRequest(request);

      default:
        throw new Error(`Unsupported request type: ${request.type}`);
    }
  }

  /**
   * Handles context information requests.
   */
  private async handleContextRequest(request: MCPRequest): Promise<MCPContext> {
    const { file, position, includeSymbols, includeHistory } = request.payload;

    try {
      // Get context information
      const context = this.contextManager.getContextAt(file, position);
      if (!context) {
        throw new Error(`No context available for ${file} at ${position.line}:${position.column}`);
      }

      // Get visible symbols if requested
      let symbols = [];
      if (includeSymbols) {
        symbols = this.contextManager.getVisibleSymbols(file, position);
      }

      // Get context history if requested
      let history = [];
      if (includeHistory) {
        history = this.contextManager.getContextHistory(file);
      }

      // Convert to MCP context format
      const mcpContext: MCPContext = {
        file,
        language: context.activeGrammar || 'unknown',
        position,
        scope: this.convertScopeToMCP(context.scope),
        symbols: symbols.map(symbol => ({
          name: symbol.name,
          type: symbol.type,
          kind: symbol.kind,
          scope: symbol.scope,
          position: symbol.position,
          references: symbol.references,
          definition: symbol.definition,
          documentation: symbol.documentation,
        })),
        grammar: {
          name: context.parseState.grammarState.activeGrammar,
          formatType: context.parseState.grammarState.formatType,
          baseGrammars: context.parseState.grammarState.baseGrammars,
          rules: [], // Would be populated with actual grammar rules
          activeContexts: context.parseState.grammarState.contextModifiers,
        },
        parseState: context.parseState,
      };

      this.emit('context_provided', { file, position, context: mcpContext });
      return mcpContext;

    } catch (error) {
      this.emit('context_error', { file, position, error });
      throw error;
    }
  }

  /**
   * Handles refactoring operation requests.
   */
  private async handleRefactorRequest(request: MCPRequest): Promise<RefactorResult> {
    const refactorRequest: RefactorRequest = request.payload;

    try {
      // Validate refactoring request
      this.validateRefactorRequest(refactorRequest);

      // Get context for the target location
      const context = this.contextManager.getContextAt(
        refactorRequest.target.file,
        {
          line: refactorRequest.target.startLine,
          column: refactorRequest.target.startColumn,
          offset: 0,
        },
      );

      if (!context) {
        throw new Error('No context available for refactoring target');
      }

      // Execute refactoring operation
      const result = await this.executeRefactoringOperation(refactorRequest, context);

      this.emit('refactoring_completed', { request: refactorRequest, result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { request: refactorRequest, error });
      throw error;
    }
  }

  /**
   * Handles code analysis requests.
   */
  private async handleAnalysisRequest(request: MCPRequest): Promise<AnalysisResult> {
    const analysisRequest: AnalysisRequest = request.payload;

    try {
      // Validate analysis request
      this.validateAnalysisRequest(analysisRequest);

      // Perform analysis
      const result = await this.performCodeAnalysis(analysisRequest);

      this.emit('analysis_completed', { request: analysisRequest, result });
      return result;

    } catch (error) {
      this.emit('analysis_error', { request: analysisRequest, error });
      throw error;
    }
  }

  /**
   * Handles code validation requests.
   */
  private async handleValidationRequest(request: MCPRequest): Promise<any> {
    const { code, language, file, options } = request.payload;

    try {
      // Parse and validate the code
      const context = await this.contextManager.parseFile(file || 'temp.code', code, language);

      // Analyze for errors and warnings
      const analysis = this.contextManager.analyzeContext(file || 'temp.code', { line: 1, column: 1, offset: 0 });

      const validationResult = {
        valid: context.parseState.errors.length === 0,
        errors: context.parseState.errors,
        warnings: context.parseState.warnings,
        suggestions: context.parseState.suggestions,
        analysis: options?.includeAnalysis ? analysis : undefined,
      };

      this.emit('validation_completed', { code, language, result: validationResult });
      return validationResult;

    } catch (error) {
      this.emit('validation_error', { code, language, error });
      throw error;
    }
  }

  /**
   * Executes a specific refactoring operation.
   */
  private async executeRefactoringOperation(request: RefactorRequest, context: ContextInfo): Promise<RefactorResult> {
    const { operation, target, parameters, options } = request;

    switch (operation) {
      case 'extract_variable':
        return this.extractVariable(target, parameters, context, options);

      case 'inline_variable':
        return this.inlineVariable(target, parameters, context, options);

      case 'rename':
        return this.renameSymbol(target, parameters, context, options);

      case 'extract_function':
        return this.extractFunction(target, parameters, context, options);

      case 'inline_function':
        return this.inlineFunction(target, parameters, context, options);

      default:
        throw new Error(`Unsupported refactoring operation: ${operation}`);
    }
  }

  /**
   * Performs code analysis based on the request.
   */
  private async performCodeAnalysis(request: AnalysisRequest): Promise<AnalysisResult> {
    const { type, target, options } = request;

    // Get context for analysis
    const position: CodePosition = {
      line: target.startLine,
      column: target.startColumn,
      offset: 0,
    };

    const analysis = this.contextManager.analyzeContext(target.file, position);

    // Perform specific analysis based on type
    switch (type) {
      case 'complexity':
        return this.analyzeComplexity(target, analysis, options);

      case 'quality':
        return this.analyzeQuality(target, analysis, options);

      case 'dependencies':
        return this.analyzeDependencies(target, analysis, options);

      case 'performance':
        return this.analyzePerformance(target, analysis, options);

      default:
        return this.performGeneralAnalysis(target, analysis, options);
    }
  }

  /**
   * Refactoring operation implementations.
   */

  // eslint-disable-next-line max-len
  private async extractVariable(target: CodeLocation, parameters: any, context: ContextInfo, options: any): Promise<RefactorResult> {
    // Implementation for extract variable refactoring
    const variableName = parameters.name || 'extractedVariable';

    // Mock implementation - would integrate with actual refactoring engine
    return {
      success: true,
      changes: [
        {
          file: target.file,
          type: 'insert',
          position: { line: target.startLine, column: 1, offset: 0 },
          newText: `const ${variableName} = `,
          description: `Extract variable ${variableName}`,
        },
        {
          file: target.file,
          type: 'replace',
          position: { line: target.startLine, column: target.startColumn, offset: 0 },
          oldText: 'original expression',
          newText: variableName,
          description: `Replace expression with ${variableName}`,
        },
      ],
      warnings: [],
      errors: [],
    };
  }

  // eslint-disable-next-line max-len
  private async inlineVariable(target: CodeLocation, parameters: any, context: ContextInfo, options: any): Promise<RefactorResult> {
    // Implementation for inline variable refactoring
    const variableName = parameters.name;

    // Find variable definition and all references
    const symbol = this.contextManager.resolveSymbol(target.file, {
      line: target.startLine,
      column: target.startColumn,
      offset: 0,
    }, variableName);

    if (!symbol) {
      throw new Error(`Variable ${variableName} not found`);
    }

    // Mock implementation
    return {
      success: true,
      changes: [
        {
          file: target.file,
          type: 'delete',
          position: symbol.position,
          oldText: `const ${variableName} = value;`,
          description: `Remove variable declaration ${variableName}`,
        },
      ],
      warnings: [],
      errors: [],
    };
  }

  // eslint-disable-next-line max-len
  private async renameSymbol(target: CodeLocation, parameters: any, context: ContextInfo, options: any): Promise<RefactorResult> {
    // Implementation for rename symbol refactoring
    const oldName = parameters.oldName;
    const newName = parameters.newName;

    // Find all references to the symbol
    const symbol = this.contextManager.resolveSymbol(target.file, {
      line: target.startLine,
      column: target.startColumn,
      offset: 0,
    }, oldName);

    if (!symbol) {
      throw new Error(`Symbol ${oldName} not found`);
    }

    // Mock implementation - would find all references and rename them
    const changes = symbol.references.map(ref => ({
      file: target.file,
      type: 'replace' as const,
      position: ref,
      oldText: oldName,
      newText: newName,
      description: `Rename ${oldName} to ${newName}`,
    }));

    return {
      success: true,
      changes,
      warnings: [],
      errors: [],
    };
  }

  // eslint-disable-next-line max-len
  private async extractFunction(target: CodeLocation, parameters: any, context: ContextInfo, options: any): Promise<RefactorResult> {
    // Implementation for extract function refactoring
    const functionName = parameters.name || 'extractedFunction';

    // Mock implementation
    return {
      success: true,
      changes: [
        {
          file: target.file,
          type: 'insert',
          position: { line: target.startLine - 1, column: 1, offset: 0 },
          newText: `function ${functionName}() {\n  // extracted code\n}\n\n`,
          description: `Extract function ${functionName}`,
        },
        {
          file: target.file,
          type: 'replace',
          position: { line: target.startLine, column: target.startColumn, offset: 0 },
          oldText: 'original code block',
          newText: `${functionName}();`,
          description: 'Replace code block with function call',
        },
      ],
      warnings: [],
      errors: [],
    };
  }

  // eslint-disable-next-line max-len
  private async inlineFunction(target: CodeLocation, parameters: any, context: ContextInfo, options: any): Promise<RefactorResult> {
    // Implementation for inline function refactoring
    const functionName = parameters.name;

    // Mock implementation
    return {
      success: true,
      changes: [
        {
          file: target.file,
          type: 'replace',
          position: { line: target.startLine, column: target.startColumn, offset: 0 },
          oldText: `${functionName}();`,
          newText: 'inlined function body',
          description: `Inline function ${functionName}`,
        },
      ],
      warnings: [],
      errors: [],
    };
  }

  /**
   * Analysis operation implementations.
   */

  private analyzeComplexity(target: CodeLocation, analysis: any, options: any): AnalysisResult {
    return {
      type: 'complexity',
      target,
      metrics: {
        cyclomaticComplexity: analysis.complexity,
        cognitiveComplexity: analysis.complexity * 1.2,
        linesOfCode: 50, // Mock value
        nestingDepth: analysis.depth,
      },
      issues: analysis.complexity > 10 ? [
        {
          type: 'complexity',
          severity: 'warning',
          message: 'High complexity detected',
          position: { line: target.startLine, column: target.startColumn, offset: 0 },
          suggestion: 'Consider breaking down into smaller functions',
        },
      ] : [],
      suggestions: analysis.suggestions.map(s => ({
        type: 'refactoring',
        description: s,
        target,
        operation: 'extract_function',
        parameters: {},
        confidence: 0.7,
      })),
      summary: `Complexity analysis completed. Cyclomatic complexity: ${analysis.complexity}`,
    };
  }

  private analyzeQuality(target: CodeLocation, analysis: any, options: any): AnalysisResult {
    return {
      type: 'quality',
      target,
      metrics: {
        maintainabilityIndex: Math.max(0, 100 - analysis.complexity * 5),
        technicalDebt: analysis.errorCount * 2 + analysis.warningCount,
        codeSmells: analysis.suggestions.length,
      },
      issues: [],
      suggestions: [],
      summary: 'Quality analysis completed',
    };
  }

  private analyzeDependencies(target: CodeLocation, analysis: any, options: any): AnalysisResult {
    return {
      type: 'dependencies',
      target,
      metrics: {
        dependencies: analysis.symbolDensity * 10,
        coupling: analysis.depth,
        cohesion: 0.8,
      },
      issues: [],
      suggestions: [],
      summary: 'Dependency analysis completed',
    };
  }

  private analyzePerformance(target: CodeLocation, analysis: any, options: any): AnalysisResult {
    return {
      type: 'performance',
      target,
      metrics: {
        estimatedExecutionTime: analysis.complexity * 10,
        memoryUsage: analysis.symbolDensity * 100,
        optimizationPotential: 0.3,
      },
      issues: [],
      suggestions: [],
      summary: 'Performance analysis completed',
    };
  }

  private performGeneralAnalysis(target: CodeLocation, analysis: any, options: any): AnalysisResult {
    return {
      type: 'general',
      target,
      metrics: {
        overall: analysis.complexity + analysis.depth + analysis.symbolDensity,
      },
      issues: [],
      suggestions: [],
      summary: 'General analysis completed',
    };
  }

  /**
   * Helper methods.
   */

  private validateRequest(request: MCPRequest): void {
    if (!request.id || !request.type || !request.payload) {
      throw new Error('Invalid request format');
    }
  }

  private validateRefactorRequest(request: RefactorRequest): void {
    if (!request.operation || !request.target) {
      throw new Error('Invalid refactoring request');
    }
  }

  private validateAnalysisRequest(request: AnalysisRequest): void {
    if (!request.type || !request.target) {
      throw new Error('Invalid analysis request');
    }
  }

  private convertScopeToMCP(scope: any): any {
    if (!scope) {
      return {
        type: 'global',
        startPosition: { line: 1, column: 1, offset: 0 },
        endPosition: { line: 1, column: 1, offset: 0 },
        children: [],
        variables: [],
      };
    }

    return {
      type: scope.type,
      name: scope.name,
      startPosition: scope.startPosition,
      endPosition: scope.endPosition,
      parent: scope.parent ? this.convertScopeToMCP(scope.parent) : undefined,
      children: scope.children.map((child: any) => this.convertScopeToMCP(child)),
      variables: scope.variables,
    };
  }

  private getCachedResult(request: MCPRequest): CachedResult | null {
    const hash = this.generateRequestHash(request);
    const cached = this.requestCache.get(hash);

    if (cached && Date.now() - cached.timestamp < 300000) { // 5 minutes
      cached.accessCount++;
      return cached;
    }

    return null;
  }

  private cacheResult(request: MCPRequest, result: any): void {
    const hash = this.generateRequestHash(request);
    this.requestCache.set(hash, {
      requestHash: hash,
      result,
      timestamp: Date.now(),
      accessCount: 1,
    });

    // Limit cache size
    if (this.requestCache.size > 1000) {
      const oldestKey = Array.from(this.requestCache.entries())
        .sort((a, b) => a[1].timestamp - b[1].timestamp)[0][0];
      this.requestCache.delete(oldestKey);
    }
  }

  private generateRequestHash(request: MCPRequest): string {
    return `${request.type}_${JSON.stringify(request.payload)}`;
  }

  private updateMetrics(type: MCPMessageType, processingTime: number, success: boolean): void {
    if (!this.config.enableMetrics) {
      return;
    }

    this.metrics.totalRequests++;
    this.metrics.lastRequestTime = Date.now();

    if (success) {
      this.metrics.successfulRequests++;
    } else {
      this.metrics.failedRequests++;
    }

    // Update average processing time
    this.metrics.averageProcessingTime =
      (this.metrics.averageProcessingTime * (this.metrics.totalRequests - 1) + processingTime) /
      this.metrics.totalRequests;

    // Update requests by type
    const currentCount = this.metrics.requestsByType.get(type) || 0;
    this.metrics.requestsByType.set(type, currentCount + 1);
  }

  private createSuccessResponse(requestId: string, payload: any): MCPResponse {
    return {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_RESPONSE, // Would be dynamic based on request type
      timestamp: Date.now(),
      source: 'minotaur',
      requestId,
      success: true,
      payload,
    };
  }

  private createErrorResponse(requestId: string, code: string, message: string): MCPResponse {
    return {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_RESPONSE, // Would be dynamic based on request type
      timestamp: Date.now(),
      source: 'minotaur',
      requestId,
      success: false,
      error: {
        code,
        message,
      },
    };
  }

  private generateId(): string {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Event handlers.
   */

  private handleContextChanged(event: any): void {
    // Invalidate relevant cache entries
    if (this.config.enableCaching) {
      this.invalidateCache(event.file);
    }
  }

  private handleFileParsed(event: any): void {
    this.emit('file_parsed', event);
  }

  private handleFileUpdated(event: any): void {
    // Invalidate cache for updated file
    if (this.config.enableCaching) {
      this.invalidateCache(event.file);
    }
  }

  private invalidateCache(file: string): void {
    for (const [key, cached] of this.requestCache) {
      if (cached.result && cached.result.file === file) {
        this.requestCache.delete(key);
      }
    }
  }

  /**
   * Gets request handler metrics.
   */
  public getMetrics(): RequestMetrics {
    return { ...this.metrics };
  }

  /**
   * Gets active request count.
   */
  public getActiveRequestCount(): number {
    return this.activeRequests.size;
  }

  /**
   * Clears the request cache.
   */
  public clearCache(): void {
    this.requestCache.clear();
  }

  /**
   * Gets cache statistics.
   */
  public getCacheStats(): any {
    return {
      size: this.requestCache.size,
      totalAccesses: Array.from(this.requestCache.values()).reduce((sum, cached) => sum + cached.accessCount, 0),
      // eslint-disable-next-line max-len
      averageAge: Date.now() - (Array.from(this.requestCache.values()).reduce((sum, cached) => sum + cached.timestamp, 0) / this.requestCache.size),
    };
  }
}

// Export the main request handler class as default
export default MCPRequestHandler;

