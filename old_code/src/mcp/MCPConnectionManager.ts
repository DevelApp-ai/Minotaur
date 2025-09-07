/// <reference types="node" />

/**
 * MCP connection manager for handling multiple AI agent connections.
 * Manages connection lifecycle, authentication, and coordination between multiple agents.
 */

import { MCPTransport, MCPMessage, MCPMessageType } from './MCPProtocol';
import { MCPMessageRouter } from './MCPMessageRouter';
import { MCPTransportFactory, MCPTransportManager } from './MCPTransports';
import { EventEmitter } from 'events';

/**
 * Connection configuration.
 */
export interface ConnectionConfig {
  maxConnections: number;
  connectionTimeout: number;
  heartbeatInterval: number;
  enableAuthentication: boolean;
  enableEncryption: boolean;
  allowedOrigins: string[];
  rateLimiting: {
    enabled: boolean;
    maxRequestsPerMinute: number;
    maxRequestsPerHour: number;
  };
}

/**
 * AI agent connection information.
 */
export interface AgentConnection {
  id: string;
  agentId: string;
  agentName: string;
  agentVersion: string;
  transport: MCPTransport;
  capabilities: AgentCapabilities;
  connectionTime: number;
  lastActivity: number;
  messageCount: number;
  isAuthenticated: boolean;
  metadata: any;
}

/**
 * AI agent capabilities.
 */
export interface AgentCapabilities {
  supportedOperations: string[];
  supportedLanguages: string[];
  maxFileSize: number;
  realTimeSupport: boolean;
  batchSupport: boolean;
  contextAware: boolean;
  version: string;
}

/**
 * Connection statistics.
 */
export interface ConnectionStatistics {
  totalConnections: number;
  activeConnections: number;
  totalMessages: number;
  messagesPerSecond: number;
  averageConnectionTime: number;
  connectionsByAgent: Map<string, number>;
  lastConnectionTime: number;
}

/**
 * Rate limiting information.
 */
interface RateLimitInfo {
  requestsThisMinute: number;
  requestsThisHour: number;
  lastMinuteReset: number;
  lastHourReset: number;
  isLimited: boolean;
}

/**
 * Connection event types.
 */
export enum ConnectionEventType {
  CONNECTED = 'connected',
  DISCONNECTED = 'disconnected',
  AUTHENTICATED = 'authenticated',
  MESSAGE_RECEIVED = 'message_received',
  MESSAGE_SENT = 'message_sent',
  ERROR = 'error',
  RATE_LIMITED = 'rate_limited'
}

/**
 * MCP connection manager for handling multiple AI agent connections.
 */
export class MCPConnectionManager extends EventEmitter {
  private config: ConnectionConfig;
  private messageRouter: MCPMessageRouter;
  private transportManager: MCPTransportManager;
  private connections: Map<string, AgentConnection>;
  private rateLimits: Map<string, RateLimitInfo>;
  private statistics: ConnectionStatistics;
  private heartbeatTimer: ReturnType<typeof setInterval> | null;
  private cleanupTimer: ReturnType<typeof setInterval> | null;
  private isActive: boolean;

  constructor(messageRouter: MCPMessageRouter, config: Partial<ConnectionConfig> = {}) {
    super();

    this.messageRouter = messageRouter;
    this.config = {
      maxConnections: 100,
      connectionTimeout: 30000, // 30 seconds
      heartbeatInterval: 30000, // 30 seconds
      enableAuthentication: false,
      enableEncryption: false,
      allowedOrigins: ['*'],
      rateLimiting: {
        enabled: true,
        maxRequestsPerMinute: 60,
        maxRequestsPerHour: 1000,
      },
      ...config,
    };

    this.transportManager = new MCPTransportManager();
    this.connections = new Map();
    this.rateLimits = new Map();
    this.heartbeatTimer = null;
    this.cleanupTimer = null;
    this.isActive = false;

    this.statistics = {
      totalConnections: 0,
      activeConnections: 0,
      totalMessages: 0,
      messagesPerSecond: 0,
      averageConnectionTime: 0,
      connectionsByAgent: new Map(),
      lastConnectionTime: 0,
    };

    this.setupEventHandlers();
  }

  /**
   * Sets up event handlers.
   */
  private setupEventHandlers(): void {
    // Transport manager events
    this.transportManager.on('transport_connected', this.handleTransportConnected.bind(this));
    this.transportManager.on('transport_disconnected', this.handleTransportDisconnected.bind(this));
    this.transportManager.on('transport_error', this.handleTransportError.bind(this));
    this.transportManager.on('message', this.handleMessage.bind(this));

    // Message router events
    this.messageRouter.on('message_routed', this.handleMessageRouted.bind(this));
    this.messageRouter.on('routing_error', this.handleRoutingError.bind(this));
  }

  /**
   * Starts the connection manager.
   */
  public async start(): Promise<void> {
    if (this.isActive) {
      return;
    }

    try {
      // Start transport manager
      await this.transportManager.connect();

      // Start heartbeat timer
      this.startHeartbeat();

      // Start cleanup timer
      this.startCleanup();

      this.isActive = true;
      this.emit('manager_started');

    } catch (error) {
      this.emit('manager_error', { error, operation: 'start' });
      throw error;
    }
  }

  /**
   * Stops the connection manager.
   */
  public async stop(): Promise<void> {
    if (!this.isActive) {
      return;
    }

    try {
      // Stop timers
      this.stopHeartbeat();
      this.stopCleanup();

      // Disconnect all connections
      await this.disconnectAll();

      // Stop transport manager
      await this.transportManager.disconnect();

      this.isActive = false;
      this.emit('manager_stopped');

    } catch (error) {
      this.emit('manager_error', { error, operation: 'stop' });
      throw error;
    }
  }

  /**
   * Accepts a new AI agent connection.
   */
  public async acceptConnection(
    transport: MCPTransport,
    agentInfo: {
      agentId: string;
      agentName: string;
      agentVersion: string;
      capabilities: AgentCapabilities;
      metadata?: any;
    },
  ): Promise<string> {
    try {
      // Check connection limit
      if (this.connections.size >= this.config.maxConnections) {
        throw new Error('Maximum connections exceeded');
      }

      // Generate connection ID
      const connectionId = this.generateConnectionId();

      // Create connection
      const connection: AgentConnection = {
        id: connectionId,
        agentId: agentInfo.agentId,
        agentName: agentInfo.agentName,
        agentVersion: agentInfo.agentVersion,
        transport,
        capabilities: agentInfo.capabilities,
        connectionTime: Date.now(),
        lastActivity: Date.now(),
        messageCount: 0,
        isAuthenticated: !this.config.enableAuthentication, // Auto-authenticate if disabled
        metadata: agentInfo.metadata || {},
      };

      // Register transport with message router
      this.messageRouter.registerTransport(connectionId, transport);

      // Add to connections
      this.connections.set(connectionId, connection);

      // Initialize rate limiting
      if (this.config.rateLimiting.enabled) {
        this.initializeRateLimit(connectionId);
      }

      // Update statistics
      this.updateConnectionStatistics(connection, 'connected');

      // Set up connection event handlers
      this.setupConnectionHandlers(connection);

      this.emit('connection_accepted', { connection });
      return connectionId;

    } catch (error) {
      this.emit('connection_error', { error, operation: 'accept' });
      throw error;
    }
  }

  /**
   * Disconnects a specific AI agent connection.
   */
  public async disconnectConnection(connectionId: string, reason?: string): Promise<void> {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      return;
    }

    try {
      // Disconnect transport
      await connection.transport.disconnect();

      // Unregister from message router
      this.messageRouter.unregisterTransport(connectionId);

      // Remove from connections
      this.connections.delete(connectionId);

      // Remove rate limiting
      this.rateLimits.delete(connectionId);

      // Update statistics
      this.updateConnectionStatistics(connection, 'disconnected');

      this.emit('connection_disconnected', { connection, reason });

    } catch (error) {
      this.emit('connection_error', { connectionId, error, operation: 'disconnect' });
      throw error;
    }
  }

  /**
   * Disconnects all connections.
   */
  public async disconnectAll(reason?: string): Promise<void> {
    const disconnectPromises: Promise<void>[] = [];

    for (const connectionId of this.connections.keys()) {
      disconnectPromises.push(
        this.disconnectConnection(connectionId, reason).catch(error => {
          this.emit('disconnect_error', { connectionId, error });
        }),
      );
    }

    await Promise.allSettled(disconnectPromises);
    this.emit('all_disconnected', { reason });
  }

  /**
   * Sends a message to a specific connection.
   */
  public async sendMessage(connectionId: string, message: MCPMessage): Promise<void> {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      throw new Error(`Connection not found: ${connectionId}`);
    }

    try {
      // Check rate limiting
      if (this.config.rateLimiting.enabled) {
        this.checkRateLimit(connectionId);
      }

      // Send message through router
      await this.messageRouter.sendMessage(message, connectionId);

      // Update connection activity
      connection.lastActivity = Date.now();
      connection.messageCount++;

      this.emit('message_sent', { connectionId, message });

    } catch (error) {
      this.emit('send_error', { connectionId, message, error });
      throw error;
    }
  }

  /**
   * Broadcasts a message to all connections.
   */
  public async broadcastMessage(message: MCPMessage, filter?: (connection: AgentConnection) => boolean): Promise<void> {
    const promises: Promise<void>[] = [];

    for (const [connectionId, connection] of this.connections) {
      if (!filter || filter(connection)) {
        promises.push(
          this.sendMessage(connectionId, message).catch(error => {
            this.emit('broadcast_error', { connectionId, message, error });
          }),
        );
      }
    }

    await Promise.allSettled(promises);
    this.emit('message_broadcasted', { message, connectionCount: promises.length });
  }

  /**
   * Authenticates a connection.
   */
  public async authenticateConnection(connectionId: string, credentials: any): Promise<boolean> {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      throw new Error(`Connection not found: ${connectionId}`);
    }

    try {
      // Perform authentication (mock implementation)
      const isValid = await this.validateCredentials(credentials);

      if (isValid) {
        connection.isAuthenticated = true;
        this.emit('connection_authenticated', { connection });
        return true;
      } else {
        this.emit('authentication_failed', { connectionId, credentials });
        return false;
      }

    } catch (error) {
      this.emit('authentication_error', { connectionId, error });
      throw error;
    }
  }

  /**
   * Gets connection information.
   */
  public getConnection(connectionId: string): AgentConnection | null {
    return this.connections.get(connectionId) || null;
  }

  /**
   * Gets all active connections.
   */
  public getActiveConnections(): AgentConnection[] {
    return Array.from(this.connections.values());
  }

  /**
   * Gets connections by agent ID.
   */
  public getConnectionsByAgent(agentId: string): AgentConnection[] {
    return Array.from(this.connections.values()).filter(conn => conn.agentId === agentId);
  }

  /**
   * Gets connection statistics.
   */
  public getStatistics(): ConnectionStatistics {
    // Update real-time statistics
    this.statistics.activeConnections = this.connections.size;

    // Calculate messages per second
    const now = Date.now();
    const timeWindow = 60000; // 1 minute
    const recentMessages = Array.from(this.connections.values())
      .filter(conn => now - conn.lastActivity < timeWindow)
      .reduce((sum, conn) => sum + conn.messageCount, 0);

    this.statistics.messagesPerSecond = recentMessages / 60;

    return { ...this.statistics };
  }

  /**
   * Private helper methods.
   */

  private setupConnectionHandlers(connection: AgentConnection): void {
    const transport = connection.transport;

    // Cast to EventEmitter to access event methods
    const eventTransport = transport as any;

    eventTransport.on?.('message', (message: MCPMessage) => {
      this.handleConnectionMessage(connection.id, message);
    });

    eventTransport.on?.('error', (error: Error) => {
      this.handleConnectionError(connection.id, error);
    });

    eventTransport.on?.('disconnected', () => {
      this.handleConnectionDisconnected(connection.id);
    });
  }

  private handleConnectionMessage(connectionId: string, message: MCPMessage): void {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      return;
    }

    // Update activity
    connection.lastActivity = Date.now();
    connection.messageCount++;

    // Check authentication
    if (this.config.enableAuthentication && !connection.isAuthenticated) {
      this.emit('unauthenticated_message', { connectionId, message });
      return;
    }

    // Check rate limiting
    if (this.config.rateLimiting.enabled) {
      try {
        this.checkRateLimit(connectionId);
      } catch (error) {
        this.emit('rate_limit_exceeded', { connectionId, message });
        return;
      }
    }

    // Update statistics
    this.statistics.totalMessages++;

    this.emit('message_received', { connectionId, message });
  }

  private handleConnectionError(connectionId: string, error: Error): void {
    this.emit('connection_error', { connectionId, error });
  }

  private handleConnectionDisconnected(connectionId: string): void {
    this.disconnectConnection(connectionId, 'Transport disconnected').catch(error => {
      this.emit('disconnect_error', { connectionId, error });
    });
  }

  private initializeRateLimit(connectionId: string): void {
    const now = Date.now();
    this.rateLimits.set(connectionId, {
      requestsThisMinute: 0,
      requestsThisHour: 0,
      lastMinuteReset: now,
      lastHourReset: now,
      isLimited: false,
    });
  }

  private checkRateLimit(connectionId: string): void {
    const rateLimit = this.rateLimits.get(connectionId);
    if (!rateLimit) {
      return;
    }

    const now = Date.now();

    // Reset minute counter if needed
    if (now - rateLimit.lastMinuteReset >= 60000) {
      rateLimit.requestsThisMinute = 0;
      rateLimit.lastMinuteReset = now;
    }

    // Reset hour counter if needed
    if (now - rateLimit.lastHourReset >= 3600000) {
      rateLimit.requestsThisHour = 0;
      rateLimit.lastHourReset = now;
    }

    // Check limits
    if (rateLimit.requestsThisMinute >= this.config.rateLimiting.maxRequestsPerMinute ||
        rateLimit.requestsThisHour >= this.config.rateLimiting.maxRequestsPerHour) {
      rateLimit.isLimited = true;
      throw new Error('Rate limit exceeded');
    }

    // Increment counters
    rateLimit.requestsThisMinute++;
    rateLimit.requestsThisHour++;
    rateLimit.isLimited = false;
  }

  private async validateCredentials(credentials: any): Promise<boolean> {
    // Mock authentication - in practice, this would validate against a real auth system
    return credentials && credentials.token === 'valid_token';
  }

  private updateConnectionStatistics(connection: AgentConnection, event: 'connected' | 'disconnected'): void {
    if (event === 'connected') {
      this.statistics.totalConnections++;
      this.statistics.lastConnectionTime = Date.now();

      // Update connections by agent
      const agentCount = this.statistics.connectionsByAgent.get(connection.agentId) || 0;
      this.statistics.connectionsByAgent.set(connection.agentId, agentCount + 1);

    } else if (event === 'disconnected') {
      // Calculate connection duration for average
      const duration = Date.now() - connection.connectionTime;
      this.statistics.averageConnectionTime =
        (this.statistics.averageConnectionTime * (this.statistics.totalConnections - 1) + duration) /
        this.statistics.totalConnections;
    }
  }

  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      this.performHeartbeat();
    }, this.config.heartbeatInterval);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private performHeartbeat(): void {
    const now = Date.now();
    const timeoutThreshold = now - this.config.connectionTimeout;

    // Check for inactive connections
    for (const [connectionId, connection] of this.connections) {
      if (connection.lastActivity < timeoutThreshold) {
        this.disconnectConnection(connectionId, 'Heartbeat timeout').catch(error => {
          this.emit('heartbeat_error', { connectionId, error });
        });
      }
    }

    this.emit('heartbeat_performed', {
      activeConnections: this.connections.size,
      timestamp: now,
    });
  }

  private startCleanup(): void {
    this.cleanupTimer = setInterval(() => {
      this.performCleanup();
    }, 300000); // 5 minutes
  }

  private stopCleanup(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = null;
    }
  }

  private performCleanup(): void {
    // Clean up old rate limit entries
    const now = Date.now();
    for (const [connectionId, rateLimit] of this.rateLimits) {
      if (!this.connections.has(connectionId) ||
          now - rateLimit.lastHourReset > 7200000) { // 2 hours
        this.rateLimits.delete(connectionId);
      }
    }

    this.emit('cleanup_performed', {
      rateLimitEntries: this.rateLimits.size,
      timestamp: now,
    });
  }

  private generateConnectionId(): string {
    return `conn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Event handlers for transport manager and message router.
   */

  private handleTransportConnected(event: any): void {
    this.emit('transport_connected', event);
  }

  private handleTransportDisconnected(event: any): void {
    this.emit('transport_disconnected', event);
  }

  private handleTransportError(event: any): void {
    this.emit('transport_error', event);
  }

  private handleMessage(message: MCPMessage): void {
    // Messages are handled through individual connection handlers
  }

  private handleMessageRouted(event: any): void {
    this.emit('message_routed', event);
  }

  private handleRoutingError(event: any): void {
    this.emit('routing_error', event);
  }
}

/**
 * Connection manager factory for creating specialized managers.
 */
export class ConnectionManagerFactory {
  /**
   * Creates a connection manager for production use.
   */
  public static createProductionManager(messageRouter: MCPMessageRouter): MCPConnectionManager {
    return new MCPConnectionManager(messageRouter, {
      maxConnections: 1000,
      connectionTimeout: 60000, // 1 minute
      heartbeatInterval: 30000, // 30 seconds
      enableAuthentication: true,
      enableEncryption: true,
      allowedOrigins: [], // Specific origins only
      rateLimiting: {
        enabled: true,
        maxRequestsPerMinute: 100,
        maxRequestsPerHour: 5000,
      },
    });
  }

  /**
   * Creates a connection manager for development use.
   */
  public static createDevelopmentManager(messageRouter: MCPMessageRouter): MCPConnectionManager {
    return new MCPConnectionManager(messageRouter, {
      maxConnections: 10,
      connectionTimeout: 300000, // 5 minutes
      heartbeatInterval: 60000, // 1 minute
      enableAuthentication: false,
      enableEncryption: false,
      allowedOrigins: ['*'],
      rateLimiting: {
        enabled: false,
        maxRequestsPerMinute: 1000,
        maxRequestsPerHour: 10000,
      },
    });
  }

  /**
   * Creates a connection manager for testing.
   */
  public static createTestManager(messageRouter: MCPMessageRouter): MCPConnectionManager {
    return new MCPConnectionManager(messageRouter, {
      maxConnections: 5,
      connectionTimeout: 10000, // 10 seconds
      heartbeatInterval: 5000, // 5 seconds
      enableAuthentication: false,
      enableEncryption: false,
      allowedOrigins: ['*'],
      rateLimiting: {
        enabled: false,
        maxRequestsPerMinute: 10000,
        maxRequestsPerHour: 100000,
      },
    });
  }
}

// Export the main connection manager class as default
export default MCPConnectionManager;

