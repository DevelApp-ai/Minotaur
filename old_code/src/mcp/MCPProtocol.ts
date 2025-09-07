/**
 * Model Context Protocol (MCP) implementation for Minotaur AI agent support.
 * Provides bidirectional communication between Minotaur and AI agents.
 */

/**
 * MCP message types for communication with AI agents.
 */
export enum MCPMessageType {
  // Request types
  REQUEST_CONTEXT = 'request_context',
  REQUEST_REFACTOR = 'request_refactor',
  REQUEST_ANALYZE = 'request_analyze',
  REQUEST_VALIDATE = 'request_validate',

  // Response types
  CONTEXT_RESPONSE = 'context_response',
  REFACTOR_RESPONSE = 'refactor_response',
  ANALYZE_RESPONSE = 'analyze_response',
  VALIDATE_RESPONSE = 'validate_response',

  // Notification types
  CONTEXT_CHANGED = 'context_changed',
  ERROR_OCCURRED = 'error_occurred',
  OPERATION_COMPLETE = 'operation_complete',

  // Capability types
  CAPABILITY_REQUEST = 'capability_request',
  CAPABILITY_RESPONSE = 'capability_response'
}

/**
 * Base interface for all MCP messages.
 */
export interface MCPMessage {
  id: string;
  type: MCPMessageType;
  timestamp: number;
  source: string;
  target?: string;
}

/**
 * Request message interface.
 */
export interface MCPRequest extends MCPMessage {
  payload: any;
  expectResponse: boolean;
  timeout?: number;
}

/**
 * Response message interface.
 */
export interface MCPResponse extends MCPMessage {
  requestId: string;
  success: boolean;
  payload?: any;
  error?: MCPError;
}

/**
 * Notification message interface.
 */
export interface MCPNotification extends MCPMessage {
  payload: any;
}

/**
 * Error information for MCP responses.
 */
export interface MCPError {
  code: string;
  message: string;
  details?: any;
}

/**
 * Context information shared with AI agents.
 */
export interface MCPContext {
  file: string;
  language: string;
  position: CodePosition;
  scope: ScopeInfo;
  symbols: SymbolInfo[];
  grammar: GrammarInfo;
  parseState: ParseStateInfo;
}

/**
 * Code position information.
 */
export interface CodePosition {
  line: number;
  column: number;
  offset: number;
}

/**
 * Scope information for context awareness.
 */
export interface ScopeInfo {
  type: string; // function, class, block, global
  name?: string;
  startPosition: CodePosition;
  endPosition: CodePosition;
  parent?: ScopeInfo;
  children: ScopeInfo[];
  variables: VariableInfo[];
}

/**
 * Symbol information for AI agents.
 */
export interface SymbolInfo {
  name: string;
  type: string;
  kind: string; // variable, function, class, interface, etc.
  scope: string;
  position: CodePosition;
  references: CodePosition[];
  definition?: CodePosition;
}

/**
 * Variable information within scopes.
 */
export interface VariableInfo {
  name: string;
  type: string;
  position: CodePosition;
  isParameter: boolean;
  isLocal: boolean;
  references: CodePosition[];
}

/**
 * Grammar information for AI agents.
 */
export interface GrammarInfo {
  name: string;
  formatType: string;
  baseGrammars: string[];
  rules: GrammarRuleInfo[];
  activeContexts: string[];
}

/**
 * Grammar rule information.
 */
export interface GrammarRuleInfo {
  name: string;
  pattern: string;
  semanticAction?: string;
  precedence?: number;
  associativity?: string;
}

/**
 * Parse state information.
 */
export interface ParseStateInfo {
  currentRule: string;
  position: CodePosition;
  contextStack: string[];
  validTerminals: string[];
  errors: ParseErrorInfo[];
}

/**
 * Parse error information.
 */
export interface ParseErrorInfo {
  message: string;
  position: CodePosition;
  severity: string; // error, warning, info
  code: string;
}

/**
 * Refactoring operation request.
 */
export interface RefactorRequest {
  operation: string; // extract_variable, inline_variable, rename, etc.
  target: CodeLocation;
  parameters: { [key: string]: any };
  options: RefactorOptions;
}

/**
 * Code location for targeting operations.
 */
export interface CodeLocation {
  file: string;
  startLine: number;
  startColumn: number;
  endLine: number;
  endColumn: number;
  context?: string;
}

/**
 * Refactoring options.
 */
export interface RefactorOptions {
  preserveComments: boolean;
  updateReferences: boolean;
  validateResult: boolean;
  dryRun: boolean;
  scope?: string; // function, class, file, project
}

/**
 * Refactoring operation result.
 */
export interface RefactorResult {
  success: boolean;
  changes: CodeChange[];
  warnings: string[];
  errors: string[];
  preview?: string;
}

/**
 * Code change information.
 */
export interface CodeChange {
  file: string;
  type: string; // insert, delete, replace
  position: CodePosition;
  oldText?: string;
  newText?: string;
  description: string;
}

/**
 * Analysis request for AI agents.
 */
export interface AnalysisRequest {
  type: string; // complexity, quality, dependencies, etc.
  target: CodeLocation;
  options: AnalysisOptions;
}

/**
 * Analysis options.
 */
export interface AnalysisOptions {
  includeMetrics: boolean;
  includeSuggestions: boolean;
  depth: string; // shallow, medium, deep
  scope: string; // function, class, file, project
}

/**
 * Analysis result.
 */
export interface AnalysisResult {
  type: string;
  target: CodeLocation;
  metrics: { [key: string]: any };
  issues: AnalysisIssue[];
  suggestions: AnalysisSuggestion[];
  summary: string;
}

/**
 * Analysis issue information.
 */
export interface AnalysisIssue {
  type: string;
  severity: string;
  message: string;
  position: CodePosition;
  suggestion?: string;
}

/**
 * Analysis suggestion information.
 */
export interface AnalysisSuggestion {
  type: string;
  description: string;
  target: CodeLocation;
  operation: string;
  parameters: { [key: string]: any };
  confidence: number; // 0.0 to 1.0
}

/**
 * MCP capability information.
 */
export interface MCPCapability {
  name: string;
  version: string;
  description: string;
  operations: string[];
  languages: string[];
  features: string[];
}

/**
 * Main MCP protocol handler for Minotaur.
 */
export class MCPProtocolHandler {
  private messageHandlers: Map<MCPMessageType, (message: MCPMessage) => Promise<MCPResponse | void>>;
  private pendingRequests: Map<string, PendingRequest>;
  private capabilities: MCPCapability[];
  private isConnected: boolean;
  private connectionId: string;

  constructor() {
    this.messageHandlers = new Map();
    this.pendingRequests = new Map();
    this.capabilities = [];
    this.isConnected = false;
    this.connectionId = this.generateId();

    this.initializeHandlers();
    this.initializeCapabilities();
  }

  /**
   * Initializes message handlers for different MCP message types.
   */
  private initializeHandlers(): void {
    this.messageHandlers.set(MCPMessageType.REQUEST_CONTEXT, this.handleContextRequest.bind(this));
    this.messageHandlers.set(MCPMessageType.REQUEST_REFACTOR, this.handleRefactorRequest.bind(this));
    this.messageHandlers.set(MCPMessageType.REQUEST_ANALYZE, this.handleAnalysisRequest.bind(this));
    this.messageHandlers.set(MCPMessageType.REQUEST_VALIDATE, this.handleValidationRequest.bind(this));
    this.messageHandlers.set(MCPMessageType.CAPABILITY_REQUEST, this.handleCapabilityRequest.bind(this));
  }

  /**
   * Initializes Minotaur capabilities for AI agents.
   */
  private initializeCapabilities(): void {
    this.capabilities = [
      {
        name: 'context_awareness',
        version: '1.0.0',
        description: 'Grammar-aware context analysis and management',
        operations: ['get_context', 'analyze_scope', 'track_symbols'],
        languages: ['*'], // All languages with defined grammars
        features: ['hierarchical_context', 'multi_language', 'real_time'],
      },
      {
        name: 'surgical_refactoring',
        version: '1.0.0',
        description: 'Precise refactoring operations with grammar guidance',
        operations: ['extract_variable', 'inline_variable', 'rename', 'extract_function', 'inline_function'],
        languages: ['*'],
        features: ['scope_aware', 'cross_language', 'validation'],
      },
      {
        name: 'multi_language_support',
        version: '1.0.0',
        description: 'Support for multiple languages and embedded languages',
        operations: ['switch_grammar', 'analyze_embedded', 'cross_language_refactor'],
        languages: ['typescript', 'javascript', 'python', 'csharp', 'java', 'sql', 'html', 'css'],
        features: ['embedded_languages', 'grammar_switching', 'cross_language_operations'],
      },
      {
        name: 'real_time_analysis',
        version: '1.0.0',
        description: 'Real-time parsing and analysis with incremental updates',
        operations: ['incremental_parse', 'real_time_validate', 'live_suggestions'],
        languages: ['*'],
        features: ['incremental_parsing', 'live_feedback', 'performance_optimized'],
      },
    ];
  }

  /**
   * Processes incoming MCP messages.
   * @param message The incoming MCP message
   * @returns Promise resolving to response or void for notifications
   */
  public async processMessage(message: MCPMessage): Promise<MCPResponse | void> {
    try {
      const handler = this.messageHandlers.get(message.type);
      if (!handler) {
        throw new Error(`No handler found for message type: ${message.type}`);
      }

      return await handler(message);
    } catch (error) {
      return this.createErrorResponse(message.id, 'HANDLER_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Sends a message to AI agents.
   * @param message The message to send
   * @returns Promise resolving to response if expected
   */
  public async sendMessage(message: MCPMessage): Promise<MCPResponse | void> {
    // Implementation would depend on the transport layer (WebSocket, HTTP, etc.)
    // For now, this is a placeholder for the interface
    // eslint-disable-next-line no-console
    console.log('Sending MCP message:', message);

    if (message.type.includes('REQUEST') && (message as MCPRequest).expectResponse) {
      return new Promise((resolve, reject) => {
        const timeout = (message as MCPRequest).timeout || 30000;
        const timeoutId = setTimeout(() => {
          this.pendingRequests.delete(message.id);
          reject(new Error('Request timeout'));
        }, timeout);

        this.pendingRequests.set(message.id, {
          resolve,
          reject,
          timeoutId,
          timestamp: Date.now(),
        });
      });
    }
  }

  /**
   * Handles context request from AI agents.
   */
  private async handleContextRequest(message: MCPMessage): Promise<MCPResponse> {
    const request = message as MCPRequest;
    const { file, position } = request.payload;

    try {
      // Get context from Minotaur interpreter
      const context = await this.getContextInfo(file, position);

      return this.createSuccessResponse(message.id, context);
    } catch (error) {
      return this.createErrorResponse(message.id, 'CONTEXT_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Handles refactoring request from AI agents.
   */
  private async handleRefactorRequest(message: MCPMessage): Promise<MCPResponse> {
    const request = message as MCPRequest;
    const refactorRequest: RefactorRequest = request.payload;

    try {
      // Perform refactoring operation
      const result = await this.performRefactoring(refactorRequest);

      return this.createSuccessResponse(message.id, result);
    } catch (error) {
      return this.createErrorResponse(message.id, 'REFACTOR_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Handles analysis request from AI agents.
   */
  private async handleAnalysisRequest(message: MCPMessage): Promise<MCPResponse> {
    const request = message as MCPRequest;
    const analysisRequest: AnalysisRequest = request.payload;

    try {
      // Perform code analysis
      const result = await this.performAnalysis(analysisRequest);

      return this.createSuccessResponse(message.id, result);
    } catch (error) {
      return this.createErrorResponse(message.id, 'ANALYSIS_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Handles validation request from AI agents.
   */
  private async handleValidationRequest(message: MCPMessage): Promise<MCPResponse> {
    const request = message as MCPRequest;
    const { code, language, context } = request.payload;

    try {
      // Validate code using grammar
      const result = await this.validateCode(code, language, context);

      return this.createSuccessResponse(message.id, result);
    } catch (error) {
      return this.createErrorResponse(message.id, 'VALIDATION_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Handles capability request from AI agents.
   */
  private async handleCapabilityRequest(message: MCPMessage): Promise<MCPResponse> {
    try {
      return this.createSuccessResponse(message.id, {
        capabilities: this.capabilities,
        version: '1.0.0',
        connectionId: this.connectionId,
      });
    } catch (error) {
      return this.createErrorResponse(message.id, 'CAPABILITY_ERROR', error instanceof Error ? error.message : String(error));
    }
  }

  /**
   * Gets context information for a specific location.
   */
  private async getContextInfo(file: string, position: CodePosition): Promise<MCPContext> {
    // This would integrate with the Minotaur interpreter
    // For now, returning a mock context
    return {
      file,
      language: 'typescript',
      position,
      scope: {
        type: 'function',
        name: 'exampleFunction',
        startPosition: { line: 1, column: 1, offset: 0 },
        endPosition: { line: 10, column: 1, offset: 200 },
        children: [],
        variables: [],
      },
      symbols: [],
      grammar: {
        name: 'TypeScript',
        formatType: 'ANTLR4',
        baseGrammars: ['BaseJavaScript'],
        rules: [],
        activeContexts: ['function'],
      },
      parseState: {
        currentRule: 'function-declaration',
        position,
        contextStack: ['global', 'function'],
        validTerminals: ['identifier', 'keyword'],
        errors: [],
      },
    };
  }

  /**
   * Performs refactoring operation.
   */
  private async performRefactoring(request: RefactorRequest): Promise<RefactorResult> {
    // This would integrate with the refactoring engine
    // For now, returning a mock result
    return {
      success: true,
      changes: [
        {
          file: request.target.file,
          type: 'replace',
          position: { line: request.target.startLine, column: request.target.startColumn, offset: 0 },
          oldText: 'oldCode',
          newText: 'newCode',
          description: `Applied ${request.operation} operation`,
        },
      ],
      warnings: [],
      errors: [],
    };
  }

  /**
   * Performs code analysis.
   */
  private async performAnalysis(request: AnalysisRequest): Promise<AnalysisResult> {
    // This would integrate with the analysis engine
    // For now, returning a mock result
    return {
      type: request.type,
      target: request.target,
      metrics: {
        complexity: 5,
        lines: 20,
        functions: 2,
      },
      issues: [],
      suggestions: [],
      summary: `Analysis of ${request.type} completed`,
    };
  }

  /**
   * Validates code using grammar.
   */
  private async validateCode(code: string, language: string, context?: any): Promise<any> {
    // This would integrate with the grammar validation
    // For now, returning a mock result
    return {
      valid: true,
      errors: [],
      warnings: [],
      suggestions: [],
    };
  }

  /**
   * Creates a success response.
   */
  private createSuccessResponse(requestId: string, payload: any): MCPResponse {
    return {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_RESPONSE, // This would be dynamic based on request type
      timestamp: Date.now(),
      source: 'minotaur',
      requestId,
      success: true,
      payload,
    };
  }

  /**
   * Creates an error response.
   */
  private createErrorResponse(requestId: string, code: string, message: string): MCPResponse {
    return {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_RESPONSE, // This would be dynamic based on request type
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

  /**
   * Generates a unique ID for messages.
   */
  private generateId(): string {
    return `mcp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Notifies AI agents of context changes.
   */
  public async notifyContextChanged(context: MCPContext): Promise<void> {
    const notification: MCPNotification = {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_CHANGED,
      timestamp: Date.now(),
      source: 'minotaur',
      payload: context,
    };

    await this.sendMessage(notification);
  }

  /**
   * Notifies AI agents of operation completion.
   */
  public async notifyOperationComplete(operation: string, result: any): Promise<void> {
    const notification: MCPNotification = {
      id: this.generateId(),
      type: MCPMessageType.OPERATION_COMPLETE,
      timestamp: Date.now(),
      source: 'minotaur',
      payload: {
        operation,
        result,
        timestamp: Date.now(),
      },
    };

    await this.sendMessage(notification);
  }

  /**
   * Gets current capabilities.
   */
  public getCapabilities(): MCPCapability[] {
    return [...this.capabilities];
  }

  /**
   * Checks if connected to AI agents.
   */
  public isConnectedToAgents(): boolean {
    return this.isConnected;
  }

  /**
   * Sets connection status.
   */
  public setConnectionStatus(connected: boolean): void {
    this.isConnected = connected;
  }
}

/**
 * Pending request information.
 */
interface PendingRequest {
  resolve: (response: MCPResponse) => void;
  reject: (error: Error) => void;
  timeoutId: ReturnType<typeof setTimeout>;
  timestamp: number;
}

/**
 * MCP transport layer interface.
 */
export interface MCPTransport {
  send(message: MCPMessage): Promise<void>;
  receive(): Promise<MCPMessage>;
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  isConnected(): boolean;
}

// Export the main protocol handler as default
export default MCPProtocolHandler;

