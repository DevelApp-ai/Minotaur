/**
 * MCP message router for handling different message types and routing them to appropriate handlers.
 * Provides message validation, routing, and response coordination for AI agent communication.
 */

import {
  MCPMessage,
  MCPRequest,
  MCPResponse,
  MCPNotification,
  MCPMessageType,
  MCPTransport,
} from './MCPProtocol';
import { MCPRequestHandler } from './MCPRequestHandler';
import { ContextManager } from '../context/ContextManager';
import { EventEmitter } from 'events';

/**
 * Message routing configuration.
 */
export interface MessageRouterConfig {
  enableValidation: boolean;
  enableLogging: boolean;
  maxMessageSize: number;
  messageTimeout: number;
  enableMetrics: boolean;
  enableMessageHistory: boolean;
  maxHistorySize: number;
}

/**
 * Message routing result.
 */
export interface MessageRoutingResult {
  success: boolean;
  response?: MCPResponse;
  error?: string;
  routingTime: number;
  handlerUsed: string;
}

/**
 * Message handler registration.
 */
export interface MessageHandler {
  type: MCPMessageType;
  handler: (message: MCPMessage) => Promise<MCPResponse | void>;
  priority: number;
  enabled: boolean;
}

/**
 * Message routing metrics.
 */
export interface MessageRoutingMetrics {
  totalMessages: number;
  messagesByType: Map<MCPMessageType, number>;
  successfulRoutes: number;
  failedRoutes: number;
  averageRoutingTime: number;
  lastMessageTime: number;
}

/**
 * Message history entry.
 */
interface MessageHistoryEntry {
  id: string;
  timestamp: number;
  type: MCPMessageType;
  source: string;
  message: MCPMessage;
  response?: MCPResponse;
  error?: string;
  routingTime: number;
}

/**
 * MCP message router for handling and routing messages between AI agents and Minotaur.
 */
export class MCPMessageRouter extends EventEmitter {
  private config: MessageRouterConfig;
  private requestHandler: MCPRequestHandler;
  private contextManager: ContextManager;
  private messageHandlers: Map<MCPMessageType, MessageHandler>;
  private messageHistory: MessageHistoryEntry[];
  private metrics: MessageRoutingMetrics;
  private activeTransports: Map<string, MCPTransport>;

  constructor(
    contextManager: ContextManager,
    requestHandler: MCPRequestHandler,
    config: Partial<MessageRouterConfig> = {},
  ) {
    super();

    this.contextManager = contextManager;
    this.requestHandler = requestHandler;
    this.config = {
      enableValidation: true,
      enableLogging: true,
      maxMessageSize: 1024 * 1024, // 1MB
      messageTimeout: 30000, // 30 seconds
      enableMetrics: true,
      enableMessageHistory: true,
      maxHistorySize: 1000,
      ...config,
    };

    this.messageHandlers = new Map();
    this.messageHistory = [];
    this.activeTransports = new Map();
    this.metrics = {
      totalMessages: 0,
      messagesByType: new Map(),
      successfulRoutes: 0,
      failedRoutes: 0,
      averageRoutingTime: 0,
      lastMessageTime: 0,
    };

    this.initializeDefaultHandlers();
    this.setupEventHandlers();
  }

  /**
   * Initializes default message handlers.
   */
  private initializeDefaultHandlers(): void {
    // Request handlers
    this.registerHandler(MCPMessageType.REQUEST_CONTEXT, this.handleContextRequest.bind(this), 10);
    this.registerHandler(MCPMessageType.REQUEST_REFACTOR, this.handleRefactorRequest.bind(this), 10);
    this.registerHandler(MCPMessageType.REQUEST_ANALYZE, this.handleAnalysisRequest.bind(this), 10);
    this.registerHandler(MCPMessageType.REQUEST_VALIDATE, this.handleValidationRequest.bind(this), 10);

    // Notification handlers
    this.registerHandler(MCPMessageType.CONTEXT_CHANGED, this.handleContextChangedNotification.bind(this), 5);
    // Note: NOTIFY_FILE_CHANGED not defined in enum, skipping or replace with appropriate type

    // Response handlers
    this.registerHandler(MCPMessageType.CONTEXT_RESPONSE, this.handleContextResponse.bind(this), 1);
    this.registerHandler(MCPMessageType.REFACTOR_RESPONSE, this.handleRefactorResponse.bind(this), 1);
    this.registerHandler(MCPMessageType.ANALYZE_RESPONSE, this.handleAnalysisResponse.bind(this), 1);
    this.registerHandler(MCPMessageType.VALIDATE_RESPONSE, this.handleValidationResponse.bind(this), 1);

    // Capability handlers
    this.registerHandler(MCPMessageType.CAPABILITY_REQUEST, this.handleCapabilityNegotiation.bind(this), 15);
    this.registerHandler(MCPMessageType.CAPABILITY_RESPONSE, this.handleCapabilityResponse.bind(this), 1);
  }

  /**
   * Sets up event handlers.
   */
  private setupEventHandlers(): void {
    this.requestHandler.on('request_error', this.handleRequestError.bind(this));
    this.contextManager.on('context_changed', this.handleContextManagerEvent.bind(this));
    this.contextManager.on('file_parsed', this.handleContextManagerEvent.bind(this));
    this.contextManager.on('file_updated', this.handleContextManagerEvent.bind(this));
  }

  /**
   * Registers a message handler for a specific message type.
   */
  public registerHandler(
    type: MCPMessageType,
    handler: (message: MCPMessage) => Promise<MCPResponse | void>,
    priority: number = 5,
    enabled: boolean = true,
  ): void {
    this.messageHandlers.set(type, {
      type,
      handler,
      priority,
      enabled,
    });

    this.emit('handler_registered', { type, priority, enabled });
  }

  /**
   * Unregisters a message handler.
   */
  public unregisterHandler(type: MCPMessageType): void {
    this.messageHandlers.delete(type);
    this.emit('handler_unregistered', { type });
  }

  /**
   * Enables or disables a message handler.
   */
  public setHandlerEnabled(type: MCPMessageType, enabled: boolean): void {
    const handler = this.messageHandlers.get(type);
    if (handler) {
      handler.enabled = enabled;
      this.emit('handler_toggled', { type, enabled });
    }
  }

  /**
   * Registers a transport for message routing.
   */
  public registerTransport(id: string, transport: MCPTransport): void {
    this.activeTransports.set(id, transport);

    // Set up transport event handlers
    const eventTransport = transport as any;
    eventTransport.on?.('message', (message: MCPMessage) => {
      this.routeMessage(message, id);
    });

    eventTransport.on?.('error', (error: Error) => {
      this.emit('transport_error', { transportId: id, error });
    });

    this.emit('transport_registered', { transportId: id });
  }

  /**
   * Unregisters a transport.
   */
  public unregisterTransport(id: string): void {
    const transport = this.activeTransports.get(id);
    if (transport) {
      const eventTransport = transport as any;
      eventTransport.removeAllListeners?.();
      this.activeTransports.delete(id);
      this.emit('transport_unregistered', { transportId: id });
    }
  }

  /**
   * Routes an incoming message to the appropriate handler.
   */
  public async routeMessage(message: MCPMessage, transportId?: string): Promise<MessageRoutingResult> {
    const startTime = Date.now();

    try {
      // Validate message
      if (this.config.enableValidation) {
        this.validateMessage(message);
      }

      // Log message if enabled
      if (this.config.enableLogging) {
        this.logMessage(message, 'incoming', transportId);
      }

      // Find appropriate handler
      const handler = this.findHandler(message.type);
      if (!handler) {
        throw new Error(`No handler found for message type: ${message.type}`);
      }

      // Execute handler
      const response = await this.executeHandler(handler, message);

      // Send response if applicable
      if (response && transportId) {
        await this.sendResponse(response, transportId);
      }

      // Update metrics
      const routingTime = Date.now() - startTime;
      this.updateMetrics(message.type, routingTime, true);

      // Add to history
      if (this.config.enableMessageHistory) {
        this.addToHistory(message, response || null, undefined, routingTime, transportId);
      }

      const result: MessageRoutingResult = {
        success: true,
        response: response || undefined,
        routingTime,
        handlerUsed: handler.type,
      };

      this.emit('message_routed', { message, result, transportId });
      return result;

    } catch (error) {
      // Update metrics
      const routingTime = Date.now() - startTime;
      this.updateMetrics(message.type, routingTime, false);

      // Add to history
      if (this.config.enableMessageHistory) {
        // eslint-disable-next-line max-len
        this.addToHistory(message, undefined, error instanceof Error ? error.message : String(error), routingTime, transportId);
      }

      const result: MessageRoutingResult = {
        success: false,
        error: error instanceof Error ? error.message : String(error),
        routingTime,
        handlerUsed: 'none',
      };

      this.emit('routing_error', { message, error, transportId });
      return result;
    }
  }

  /**
   * Sends a message through a specific transport.
   */
  public async sendMessage(message: MCPMessage, transportId: string): Promise<void> {
    const transport = this.activeTransports.get(transportId);
    if (!transport) {
      throw new Error(`Transport not found: ${transportId}`);
    }

    try {
      // Validate message
      if (this.config.enableValidation) {
        this.validateMessage(message);
      }

      // Log message if enabled
      if (this.config.enableLogging) {
        this.logMessage(message, 'outgoing', transportId);
      }

      // Send message
      await transport.send(message);

      this.emit('message_sent', { message, transportId });

    } catch (error) {
      this.emit('send_error', { message, transportId, error });
      throw error;
    }
  }

  /**
   * Broadcasts a message to all registered transports.
   */
  public async broadcastMessage(message: MCPMessage): Promise<void> {
    const promises: Promise<void>[] = [];

    for (const [transportId, transport] of this.activeTransports) {
      promises.push(
        this.sendMessage(message, transportId).catch(error => {
          this.emit('broadcast_error', { message, transportId, error });
        }),
      );
    }

    await Promise.allSettled(promises);
    this.emit('message_broadcasted', { message, transportCount: this.activeTransports.size });
  }

  /**
   * Default message handlers.
   */

  private async handleContextRequest(message: MCPMessage): Promise<MCPResponse> {
    return this.requestHandler.processRequest(message as MCPRequest);
  }

  private async handleRefactorRequest(message: MCPMessage): Promise<MCPResponse> {
    return this.requestHandler.processRequest(message as MCPRequest);
  }

  private async handleAnalysisRequest(message: MCPMessage): Promise<MCPResponse> {
    return this.requestHandler.processRequest(message as MCPRequest);
  }

  private async handleValidationRequest(message: MCPMessage): Promise<MCPResponse> {
    return this.requestHandler.processRequest(message as MCPRequest);
  }

  private async handleContextChangedNotification(message: MCPMessage): Promise<void> {
    const notification = message as MCPNotification;
    this.emit('context_changed_notification', { notification });

    // Process context change if needed
    // This could trigger updates to connected AI agents
  }

  private async handleFileChangedNotification(message: MCPMessage): Promise<void> {
    const notification = message as MCPNotification;
    this.emit('file_changed_notification', { notification });

    // Process file change if needed
    // This could trigger re-parsing or context updates
  }

  private async handleContextResponse(message: MCPMessage): Promise<void> {
    const response = message as MCPResponse;
    this.emit('context_response_received', { response });
  }

  private async handleRefactorResponse(message: MCPMessage): Promise<void> {
    const response = message as MCPResponse;
    this.emit('refactor_response_received', { response });
  }

  private async handleAnalysisResponse(message: MCPMessage): Promise<void> {
    const response = message as MCPResponse;
    this.emit('analysis_response_received', { response });
  }

  private async handleValidationResponse(message: MCPMessage): Promise<void> {
    const response = message as MCPResponse;
    this.emit('validation_response_received', { response });
  }

  private async handleCapabilityNegotiation(message: MCPMessage): Promise<MCPResponse> {
    const request = message as MCPRequest;

    // Return Minotaur capabilities
    const capabilities = {
      parsing: {
        languages: ['javascript', 'typescript', 'python', 'java', 'c', 'cpp'],
        realTime: true,
        incremental: true,
        multiFile: true,
      },
      refactoring: {
        operations: ['extract_variable', 'inline_variable', 'rename', 'extract_function', 'inline_function'],
        crossLanguage: true,
        undoSupport: true,
        previewMode: true,
      },
      analysis: {
        types: ['complexity', 'quality', 'dependencies', 'performance'],
        realTime: true,
        suggestions: true,
        metrics: true,
      },
      context: {
        hierarchical: true,
        realTime: true,
        symbolTracking: true,
        scopeAware: true,
      },
      grammar: {
        inheritance: true,
        multiFormat: true,
        customGrammars: true,
        semanticActions: true,
      },
    };

    return {
      id: this.generateId(),
      type: MCPMessageType.CAPABILITY_RESPONSE,
      timestamp: Date.now(),
      source: 'minotaur',
      requestId: request.id,
      success: true,
      payload: capabilities,
    };
  }

  private async handleCapabilityResponse(message: MCPMessage): Promise<void> {
    const response = message as MCPResponse;
    this.emit('capability_response_received', { response });
  }

  /**
   * Helper methods.
   */

  private validateMessage(message: MCPMessage): void {
    if (!message.id || !message.type || !message.timestamp || !message.source) {
      throw new Error('Invalid message format: missing required fields');
    }

    if (this.config.maxMessageSize > 0) {
      const messageSize = JSON.stringify(message).length;
      if (messageSize > this.config.maxMessageSize) {
        throw new Error(`Message size exceeds limit: ${messageSize} > ${this.config.maxMessageSize}`);
      }
    }
  }

  private findHandler(type: MCPMessageType): MessageHandler | null {
    const handler = this.messageHandlers.get(type);
    return handler && handler.enabled ? handler : null;
  }

  private async executeHandler(handler: MessageHandler, message: MCPMessage): Promise<MCPResponse | void> {
    try {
      return await Promise.race([
        handler.handler(message),
        new Promise<never>((_, reject) => {
          setTimeout(() => reject(new Error('Handler timeout')), this.config.messageTimeout);
        }),
      ]);
    } catch (error) {
      throw new Error(`Handler execution failed: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private async sendResponse(response: MCPResponse, transportId: string): Promise<void> {
    await this.sendMessage(response, transportId);
  }

  private logMessage(message: MCPMessage, direction: 'incoming' | 'outgoing', transportId?: string): void {
    const logEntry = {
      timestamp: Date.now(),
      direction,
      transportId,
      messageId: message.id,
      messageType: message.type,
      source: message.source,
    };

    this.emit('message_logged', logEntry);
  }

  private updateMetrics(type: MCPMessageType, routingTime: number, success: boolean): void {
    if (!this.config.enableMetrics) {
      return;
    }

    this.metrics.totalMessages++;
    this.metrics.lastMessageTime = Date.now();

    if (success) {
      this.metrics.successfulRoutes++;
    } else {
      this.metrics.failedRoutes++;
    }

    // Update average routing time
    this.metrics.averageRoutingTime =
      (this.metrics.averageRoutingTime * (this.metrics.totalMessages - 1) + routingTime) /
      this.metrics.totalMessages;

    // Update messages by type
    const currentCount = this.metrics.messagesByType.get(type) || 0;
    this.metrics.messagesByType.set(type, currentCount + 1);
  }

  private addToHistory(
    message: MCPMessage,
    response?: MCPResponse,
    error?: string,
    routingTime?: number,
    transportId?: string,
  ): void {
    const entry: MessageHistoryEntry = {
      id: this.generateId(),
      timestamp: Date.now(),
      type: message.type,
      source: message.source,
      message,
      response,
      error,
      routingTime: routingTime || 0,
    };

    this.messageHistory.push(entry);

    // Limit history size
    if (this.messageHistory.length > this.config.maxHistorySize) {
      this.messageHistory.shift();
    }
  }

  private generateId(): string {
    return `msg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Event handlers.
   */

  private handleRequestError(event: any): void {
    this.emit('request_handler_error', event);
  }

  private handleContextManagerEvent(event: any): void {
    // Forward context manager events to connected transports
    const notification: MCPNotification = {
      id: this.generateId(),
      type: MCPMessageType.CONTEXT_CHANGED,
      timestamp: Date.now(),
      source: 'minotaur',
      payload: event,
    };

    this.broadcastMessage(notification).catch(error => {
      this.emit('broadcast_error', { notification, error });
    });
  }

  /**
   * Public API methods.
   */

  /**
   * Gets routing metrics.
   */
  public getMetrics(): MessageRoutingMetrics {
    return { ...this.metrics };
  }

  /**
   * Gets message history.
   */
  public getMessageHistory(limit?: number): MessageHistoryEntry[] {
    const history = [...this.messageHistory];
    return limit ? history.slice(-limit) : history;
  }

  /**
   * Gets registered handlers.
   */
  public getHandlers(): Map<MCPMessageType, MessageHandler> {
    return new Map(this.messageHandlers);
  }

  /**
   * Gets active transports.
   */
  public getActiveTransports(): string[] {
    return Array.from(this.activeTransports.keys());
  }

  /**
   * Clears message history.
   */
  public clearHistory(): void {
    this.messageHistory = [];
    this.emit('history_cleared');
  }

  /**
   * Gets router statistics.
   */
  public getStatistics(): any {
    return {
      metrics: this.getMetrics(),
      handlers: this.messageHandlers.size,
      transports: this.activeTransports.size,
      historySize: this.messageHistory.length,
      config: this.config,
    };
  }
}

/**
 * Message router factory for creating specialized routers.
 */
export class MessageRouterFactory {
  /**
   * Creates a router optimized for real-time operations.
   */
  public static createRealTimeRouter(
    contextManager: ContextManager,
    requestHandler: MCPRequestHandler,
  ): MCPMessageRouter {
    return new MCPMessageRouter(contextManager, requestHandler, {
      enableValidation: true,
      enableLogging: true,
      messageTimeout: 5000, // 5 seconds for real-time
      enableMetrics: true,
      enableMessageHistory: true,
      maxHistorySize: 500,
    });
  }

  /**
   * Creates a router for batch processing.
   */
  public static createBatchRouter(
    contextManager: ContextManager,
    requestHandler: MCPRequestHandler,
  ): MCPMessageRouter {
    return new MCPMessageRouter(contextManager, requestHandler, {
      enableValidation: true,
      enableLogging: false,
      messageTimeout: 60000, // 1 minute for batch
      enableMetrics: false,
      enableMessageHistory: false,
    });
  }

  /**
   * Creates a router for testing.
   */
  public static createTestRouter(
    contextManager: ContextManager,
    requestHandler: MCPRequestHandler,
  ): MCPMessageRouter {
    return new MCPMessageRouter(contextManager, requestHandler, {
      enableValidation: true,
      enableLogging: true,
      messageTimeout: 1000, // 1 second for testing
      enableMetrics: true,
      enableMessageHistory: true,
      maxHistorySize: 100,
    });
  }
}

// Export the main message router class as default
export default MCPMessageRouter;

