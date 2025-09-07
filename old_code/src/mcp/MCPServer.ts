/**
 * MCP Server implementation for Minotaur AI agent support.
 * Handles multiple AI agent connections and coordinates operations.
 */

import { MCPProtocolHandler, MCPMessage, MCPResponse, MCPTransport } from './MCPProtocol';
import { MCPWebSocketTransport } from './MCPTransports';
import { EventEmitter } from 'events';

/**
 * AI agent connection information.
 */
export interface AgentConnection {
  id: string;
  name: string;
  capabilities: string[];
  transport: MCPTransport;
  lastActivity: number;
  isActive: boolean;
}

/**
 * Server configuration for MCP.
 */
export interface MCPServerConfig {
  port: number;
  host: string;
  maxConnections: number;
  heartbeatInterval: number;
  connectionTimeout: number;
  enableLogging: boolean;
  logLevel: string;
}

/**
 * Server statistics.
 */
export interface ServerStats {
  totalConnections: number;
  activeConnections: number;
  messagesProcessed: number;
  operationsPerformed: number;
  uptime: number;
  startTime: number;
}

/**
 * MCP Server for managing AI agent connections and operations.
 */
export class MCPServer extends EventEmitter {
  private config: MCPServerConfig;
  private protocolHandler: MCPProtocolHandler;
  private connections: Map<string, AgentConnection>;
  private server: any; // WebSocket server or HTTP server
  private isRunning: boolean;
  private stats: ServerStats;
  private heartbeatTimer: ReturnType<typeof setInterval> | null;

  constructor(config: Partial<MCPServerConfig> = {}) {
    super();

    this.config = {
      port: 8080,
      host: 'localhost',
      maxConnections: 10,
      heartbeatInterval: 30000, // 30 seconds
      connectionTimeout: 300000, // 5 minutes
      enableLogging: true,
      logLevel: 'info',
      ...config,
    };

    this.protocolHandler = new MCPProtocolHandler();
    this.connections = new Map();
    this.isRunning = false;
    this.heartbeatTimer = null;

    this.stats = {
      totalConnections: 0,
      activeConnections: 0,
      messagesProcessed: 0,
      operationsPerformed: 0,
      uptime: 0,
      startTime: Date.now(),
    };

    this.setupEventHandlers();
  }

  /**
   * Starts the MCP server.
   */
  public async start(): Promise<void> {
    if (this.isRunning) {
      throw new Error('Server is already running');
    }

    try {
      await this.initializeServer();
      this.startHeartbeat();
      this.isRunning = true;
      this.stats.startTime = Date.now();

      this.log('info', `MCP Server started on ${this.config.host}:${this.config.port}`);
      this.emit('server_started', { config: this.config });
    } catch (error) {
      this.log('error', `Failed to start server: ${error instanceof Error ? error.message : String(error)}`);
      throw error;
    }
  }

  /**
   * Stops the MCP server.
   */
  public async stop(): Promise<void> {
    if (!this.isRunning) {
      return;
    }

    try {
      // Disconnect all agents
      for (const [id, connection] of this.connections) {
        await this.disconnectAgent(id, 'Server shutdown');
      }

      // Stop heartbeat
      if (this.heartbeatTimer) {
        clearInterval(this.heartbeatTimer);
        this.heartbeatTimer = null;
      }

      // Close server
      if (this.server) {
        await this.closeServer();
      }

      this.isRunning = false;
      this.log('info', 'MCP Server stopped');
      this.emit('server_stopped');
    } catch (error) {
      this.log('error', `Error stopping server: ${error instanceof Error ? error.message : String(error)}`);
      throw error;
    }
  }

  /**
   * Initializes the server based on transport type.
   */
  private async initializeServer(): Promise<void> {
    // For now, implementing WebSocket server
    // In a real implementation, this would use a WebSocket library like 'ws'
    this.log('info', 'Initializing WebSocket server...');

    // Mock server initialization
    this.server = {
      listen: () => Promise.resolve(),
      close: () => Promise.resolve(),
    };

    await this.server.listen();
  }

  /**
   * Closes the server.
   */
  private async closeServer(): Promise<void> {
    if (this.server && this.server.close) {
      await this.server.close();
    }
  }

  /**
   * Sets up event handlers for the protocol handler.
   */
  private setupEventHandlers(): void {
    // Handle protocol events
    const eventHandler = this.protocolHandler as any;
    eventHandler.on?.('message_received', this.handleProtocolMessage.bind(this));
    eventHandler.on?.('error', this.handleProtocolError.bind(this));
  }

  /**
   * Handles incoming protocol messages.
   */
  private async handleProtocolMessage(message: MCPMessage, connectionId: string): Promise<void> {
    this.stats.messagesProcessed++;

    try {
      const response = await this.protocolHandler.processMessage(message);

      if (response) {
        await this.sendToAgent(connectionId, response);
      }

      this.updateConnectionActivity(connectionId);
      this.emit('message_processed', { message, connectionId, response });
    } catch (error) {
      this.log('error', `Error processing message: ${error instanceof Error ? error.message : String(error)}`);
      this.emit('message_error', { message, connectionId, error });
    }
  }

  /**
   * Handles protocol errors.
   */
  private handleProtocolError(error: Error, connectionId: string): void {
    this.log('error', `Protocol error for connection ${connectionId}: ${error instanceof Error ? error.message : String(error)}`);
    this.emit('protocol_error', { error, connectionId });
  }

  /**
   * Connects a new AI agent.
   */
  // eslint-disable-next-line max-len
  public async connectAgent(agentInfo: { name: string; capabilities: string[] }, transport: MCPTransport): Promise<string> {
    if (this.connections.size >= this.config.maxConnections) {
      throw new Error('Maximum connections reached');
    }

    const connectionId = this.generateConnectionId();

    const connection: AgentConnection = {
      id: connectionId,
      name: agentInfo.name,
      capabilities: agentInfo.capabilities,
      transport,
      lastActivity: Date.now(),
      isActive: true,
    };

    this.connections.set(connectionId, connection);
    this.stats.totalConnections++;
    this.stats.activeConnections++;

    this.log('info', `Agent connected: ${agentInfo.name} (${connectionId})`);
    this.emit('agent_connected', { connection });

    return connectionId;
  }

  /**
   * Disconnects an AI agent.
   */
  public async disconnectAgent(connectionId: string, reason: string = 'Disconnected'): Promise<void> {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      return;
    }

    try {
      await connection.transport.disconnect();
    } catch (error) {
      this.log('warn', `Error disconnecting transport: ${error instanceof Error ? error.message : String(error)}`);
    }

    connection.isActive = false;
    this.connections.delete(connectionId);
    this.stats.activeConnections--;

    this.log('info', `Agent disconnected: ${connection.name} (${connectionId}) - ${reason}`);
    this.emit('agent_disconnected', { connection, reason });
  }

  /**
   * Sends a message to a specific agent.
   */
  public async sendToAgent(connectionId: string, message: MCPMessage): Promise<void> {
    const connection = this.connections.get(connectionId);
    if (!connection || !connection.isActive) {
      throw new Error(`No active connection found for agent: ${connectionId}`);
    }

    try {
      await connection.transport.send(message);
      this.updateConnectionActivity(connectionId);
    } catch (error) {
      this.log('error', `Failed to send message to agent ${connectionId}: ${error instanceof Error ? error.message : String(error)}`);
      await this.disconnectAgent(connectionId, 'Send error');
      throw error;
    }
  }

  /**
   * Broadcasts a message to all connected agents.
   */
  // eslint-disable-next-line max-len
  public async broadcastToAgents(message: MCPMessage, filter?: (connection: AgentConnection) => boolean): Promise<void> {
    const promises: Promise<void>[] = [];

    for (const [id, connection] of this.connections) {
      if (connection.isActive && (!filter || filter(connection))) {
        promises.push(this.sendToAgent(id, message).catch(error => {
          this.log('warn', `Failed to broadcast to agent ${id}: ${error instanceof Error ? error.message : String(error)}`);
        }));
      }
    }

    await Promise.allSettled(promises);
  }

  /**
   * Updates connection activity timestamp.
   */
  private updateConnectionActivity(connectionId: string): void {
    const connection = this.connections.get(connectionId);
    if (connection) {
      connection.lastActivity = Date.now();
    }
  }

  /**
   * Starts the heartbeat timer for connection monitoring.
   */
  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      this.checkConnectionHealth();
    }, this.config.heartbeatInterval);
  }

  /**
   * Checks the health of all connections and removes inactive ones.
   */
  private checkConnectionHealth(): void {
    const now = Date.now();
    const timeoutThreshold = now - this.config.connectionTimeout;

    for (const [id, connection] of this.connections) {
      if (connection.lastActivity < timeoutThreshold) {
        this.disconnectAgent(id, 'Connection timeout');
      }
    }

    // Update uptime
    this.stats.uptime = now - this.stats.startTime;
  }

  /**
   * Gets server statistics.
   */
  public getStats(): ServerStats {
    return {
      ...this.stats,
      uptime: Date.now() - this.stats.startTime,
    };
  }

  /**
   * Gets information about connected agents.
   */
  public getConnectedAgents(): AgentConnection[] {
    return Array.from(this.connections.values()).filter(conn => conn.isActive);
  }

  /**
   * Gets a specific agent connection.
   */
  public getAgent(connectionId: string): AgentConnection | undefined {
    return this.connections.get(connectionId);
  }

  /**
   * Checks if the server is running.
   */
  public isServerRunning(): boolean {
    return this.isRunning;
  }

  /**
   * Gets server configuration.
   */
  public getConfig(): MCPServerConfig {
    return { ...this.config };
  }

  /**
   * Updates server configuration (some settings require restart).
   */
  public updateConfig(newConfig: Partial<MCPServerConfig>): void {
    this.config = { ...this.config, ...newConfig };
    this.emit('config_updated', { config: this.config });
  }

  /**
   * Generates a unique connection ID.
   */
  private generateConnectionId(): string {
    return `agent_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Logs messages based on configuration.
   */
  private log(level: string, message: string, data?: any): void {
    if (!this.config.enableLogging) {
      return;
    }

    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] [${level.toUpperCase()}] ${message}`;

    switch (level) {
      case 'error':
    // eslint-disable-next-line no-console
        console.error(logMessage, data || '');
        break;
      case 'warn':
    // eslint-disable-next-line no-console
        console.warn(logMessage, data || '');
        break;
      case 'info':
    // eslint-disable-next-line no-console
        console.info(logMessage, data || '');
        break;
      case 'debug':
        if (this.config.logLevel === 'debug') {
    // eslint-disable-next-line no-console
          console.debug(logMessage, data || '');
        }
        break;
      default:
    // eslint-disable-next-line no-console
        console.log(logMessage, data || '');
    }

    this.emit('log', { level, message, data, timestamp });
  }

  /**
   * Performs a refactoring operation requested by an agent.
   */
  public async performRefactoring(connectionId: string, operation: string, parameters: any): Promise<any> {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      throw new Error(`No connection found for agent: ${connectionId}`);
    }

    this.stats.operationsPerformed++;

    try {
      // This would integrate with the actual refactoring engine
      const result = await this.executeRefactoringOperation(operation, parameters);

      this.log('info', `Refactoring operation completed: ${operation} for agent ${connection.name}`);
      this.emit('operation_completed', { connectionId, operation, parameters, result });

      return result;
    } catch (error) {
      this.log('error', `Refactoring operation failed: ${operation} for agent ${connection.name}: ${error instanceof Error ? error.message : String(error)}`);
      this.emit('operation_failed', { connectionId, operation, parameters, error });
      throw error;
    }
  }

  /**
   * Executes a refactoring operation (placeholder for actual implementation).
   */
  private async executeRefactoringOperation(operation: string, parameters: any): Promise<any> {
    // This would integrate with the actual Minotaur refactoring engine
    // For now, returning a mock result
    return {
      success: true,
      operation,
      parameters,
      changes: [],
      timestamp: Date.now(),
    };
  }

  /**
   * Validates agent capabilities against operation requirements.
   */
  public validateAgentCapabilities(connectionId: string, requiredCapabilities: string[]): boolean {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      return false;
    }

    return requiredCapabilities.every(cap => connection.capabilities.includes(cap));
  }

  /**
   * Gets agents with specific capabilities.
   */
  public getAgentsWithCapabilities(capabilities: string[]): AgentConnection[] {
    return this.getConnectedAgents().filter(agent =>
      capabilities.every(cap => agent.capabilities.includes(cap)),
    );
  }

  /**
   * Shuts down the server gracefully.
   */
  public async shutdown(): Promise<void> {
    this.log('info', 'Initiating graceful shutdown...');

    try {
      await this.stop();
      this.removeAllListeners();
      this.log('info', 'Graceful shutdown completed');
    } catch (error) {
      this.log('error', `Error during shutdown: ${error instanceof Error ? error.message : String(error)}`);
      throw error;
    }
  }
}

/**
 * Factory for creating MCP servers with different configurations.
 */
export class MCPServerFactory {
  /**
   * Creates a development server with relaxed settings.
   */
  public static createDevelopmentServer(): MCPServer {
    return new MCPServer({
      port: 8080,
      host: 'localhost',
      maxConnections: 5,
      heartbeatInterval: 60000, // 1 minute
      connectionTimeout: 600000, // 10 minutes
      enableLogging: true,
      logLevel: 'debug',
    });
  }

  /**
   * Creates a production server with optimized settings.
   */
  public static createProductionServer(): MCPServer {
    return new MCPServer({
      port: 8080,
      host: '0.0.0.0',
      maxConnections: 50,
      heartbeatInterval: 30000, // 30 seconds
      connectionTimeout: 300000, // 5 minutes
      enableLogging: true,
      logLevel: 'info',
    });
  }

  /**
   * Creates a testing server with minimal settings.
   */
  public static createTestingServer(): MCPServer {
    return new MCPServer({
      port: 0, // Random port
      host: 'localhost',
      maxConnections: 2,
      heartbeatInterval: 10000, // 10 seconds
      connectionTimeout: 60000, // 1 minute
      enableLogging: false,
      logLevel: 'error',
    });
  }
}

// Export the main server class as default
export default MCPServer;

