/**
 * Transport layer implementations for MCP communication.
 * Supports WebSocket, HTTP, and local transport methods.
 */

import { MCPMessage, MCPTransport } from './MCPProtocol';
import { EventEmitter } from 'events';
import WS from 'ws';
import { v4 as uuidv4 } from 'uuid';

/**
 * WebSocket transport implementation for MCP.
 */
class MCPWebSocketTransport extends EventEmitter implements MCPTransport {
  private websocket: WS | null = null;
  private url: string;
  private messageQueue: MCPMessage[] = [];
  private isConnectedFlag: boolean = false;
  private reconnectAttempts: number = 0;
  private maxReconnectAttempts: number = 5;
  private reconnectDelay: number = 1000;
  private heartbeatInterval: ReturnType<typeof setInterval> | null = null;
  private connectionId: string;

  constructor(url: string, options: WebSocketTransportOptions = {}) {
    super();
    this.url = url;
    this.maxReconnectAttempts = options.maxReconnectAttempts || 5;
    this.reconnectDelay = options.reconnectDelay || 1000;
    this.connectionId = uuidv4();
  }

  public async connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.websocket = new WS(this.url);

        this.websocket.on('open', () => {
          this.isConnectedFlag = true;
          this.reconnectAttempts = 0;
          this.startHeartbeat();
          this.processMessageQueue();
          this.emit('connected');
          resolve();
        });

        this.websocket.on('error', (error) => {
          this.isConnectedFlag = false;
          this.emit('error', error);
          if (this.reconnectAttempts === 0) {
            reject(error);
          }
        });

        this.websocket.on('close', (code, reason) => {
          this.isConnectedFlag = false;
          this.stopHeartbeat();
          this.emit('disconnected', { code, reason });

          if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.attemptReconnect();
          }
        });

        this.websocket.on('message', (data) => {
          try {
            const message = JSON.parse(data.toString()) as MCPMessage;
            this.emit('message', message);
          } catch (error) {
            this.emit('error', new Error(`Failed to parse message: ${error instanceof Error ? error.message : String(error)}`));
          }
        });

        this.websocket.on('ping', () => {
          if (this.websocket) {
            this.websocket.pong();
          }
        });

      } catch (error) {
        reject(error);
      }
    });
  }

  public async disconnect(): Promise<void> {
    this.maxReconnectAttempts = 0; // Prevent reconnection
    this.stopHeartbeat();

    if (this.websocket) {
      this.websocket.close(1000, 'Normal closure');
      this.websocket = null;
    }

    this.isConnectedFlag = false;
    this.emit('disconnected', { code: 1000, reason: 'Normal closure' });
  }

  public async send(message: MCPMessage): Promise<void> {
    if (!this.isConnectedFlag || !this.websocket) {
      this.messageQueue.push(message);
      throw new Error('Not connected - message queued');
    }

    try {
      const data = JSON.stringify(message);
      this.websocket.send(data);
      this.emit('message_sent', message);
    } catch (error) {
      this.emit('error', error);
      throw error;
    }
  }

  public async receive(): Promise<MCPMessage> {
    return new Promise((resolve, reject) => {
      if (!this.websocket) {
        reject(new Error('Not connected'));
        return;
      }

      const messageHandler = (message: MCPMessage) => {
        this.removeListener('message', messageHandler);
        resolve(message);
      };

      this.once('message', messageHandler);

      // Set timeout for receive operation
      setTimeout(() => {
        this.removeListener('message', messageHandler);
        reject(new Error('Receive timeout'));
      }, 30000);
    });
  }

  public isConnected(): boolean {
    return this.isConnectedFlag && this.websocket?.readyState === WebSocket.OPEN;
  }

  public getConnectionId(): string {
    return this.connectionId;
  }

  private processMessageQueue(): void {
    while (this.messageQueue.length > 0 && this.isConnectedFlag) {
      const message = this.messageQueue.shift();
      if (message) {
        this.send(message).catch(error => {
          this.emit('error', error);
        });
      }
    }
  }

  private attemptReconnect(): void {
    this.reconnectAttempts++;
    const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);

    setTimeout(() => {
      if (this.reconnectAttempts <= this.maxReconnectAttempts) {
        this.emit('reconnecting', { attempt: this.reconnectAttempts });
        this.connect().catch(error => {
          this.emit('reconnect_failed', { attempt: this.reconnectAttempts, error });
        });
      }
    }, delay);
  }

  private startHeartbeat(): void {
    this.heartbeatInterval = setInterval(() => {
      if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
        this.websocket.ping();
      }
    }, 30000); // 30 seconds
  }

  private stopHeartbeat(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
  }
}

/**
 * HTTP transport implementation for MCP.
 */
class MCPHTTPTransport extends EventEmitter implements MCPTransport {
  private baseUrl: string;
  private sessionId: string;
  private isConnectedFlag: boolean = false;
  private pollingInterval: ReturnType<typeof setInterval> | null = null;
  private pollingDelay: number = 1000;

  constructor(baseUrl: string, options: HTTPTransportOptions = {}) {
    super();
    this.baseUrl = baseUrl.replace(/\/$/, ''); // Remove trailing slash
    this.sessionId = uuidv4();
    this.pollingDelay = options.pollingDelay || 1000;
  }

  public async connect(): Promise<void> {
    try {
      // Establish session with server
      const response = await fetch(`${this.baseUrl}/mcp/connect`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          sessionId: this.sessionId,
          timestamp: Date.now(),
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const result = await response.json();
      if (result.success) {
        this.isConnectedFlag = true;
        this.startPolling();
        this.emit('connected');
      } else {
        throw new Error(result.error || 'Connection failed');
      }
    } catch (error) {
      this.emit('error', error);
      throw error;
    }
  }

  public async disconnect(): Promise<void> {
    this.stopPolling();

    try {
      await fetch(`${this.baseUrl}/mcp/disconnect`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          sessionId: this.sessionId,
          timestamp: Date.now(),
        }),
      });
    } catch (error) {
      // Ignore disconnect errors
    }

    this.isConnectedFlag = false;
    this.emit('disconnected');
  }

  public async send(message: MCPMessage): Promise<void> {
    if (!this.isConnectedFlag) {
      throw new Error('Not connected');
    }

    try {
      const response = await fetch(`${this.baseUrl}/mcp/send`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': this.sessionId,
        },
        body: JSON.stringify(message),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      this.emit('message_sent', message);
    } catch (error) {
      this.emit('error', error);
      throw error;
    }
  }

  public async receive(): Promise<MCPMessage> {
    if (!this.isConnectedFlag) {
      throw new Error('Not connected');
    }

    try {
      const response = await fetch(`${this.baseUrl}/mcp/receive`, {
        method: 'GET',
        headers: {
          'X-Session-ID': this.sessionId,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const message = await response.json() as MCPMessage;
      return message;
    } catch (error) {
      this.emit('error', error);
      throw error;
    }
  }

  public isConnected(): boolean {
    return this.isConnectedFlag;
  }

  private startPolling(): void {
    this.pollingInterval = setInterval(async () => {
      try {
        const message = await this.receive();
        if (message) {
          this.emit('message', message);
        }
      } catch (error) {
        // Ignore polling errors unless connection is lost
        if (error instanceof Error ? error.message : String(error).includes('404') || error instanceof Error ? error.message : String(error).includes('session')) {
          this.isConnectedFlag = false;
          this.stopPolling();
          this.emit('disconnected');
        }
      }
    }, this.pollingDelay);
  }

  private stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }
}

/**
 * Local transport implementation for MCP (for same-process communication).
 */
class MCPLocalTransport extends EventEmitter implements MCPTransport {
  private isConnectedFlag: boolean = false;
  private messageQueue: MCPMessage[] = [];
  private peer: MCPLocalTransport | null = null;
  private connectionId: string;

  constructor() {
    super();
    this.connectionId = uuidv4();
  }

  public async connect(): Promise<void> {
    this.isConnectedFlag = true;
    this.emit('connected');
    this.processMessageQueue();
  }

  public async disconnect(): Promise<void> {
    this.isConnectedFlag = false;
    if (this.peer) {
      this.peer.handlePeerDisconnect();
      this.peer = null;
    }
    this.emit('disconnected');
  }

  public async send(message: MCPMessage): Promise<void> {
    if (!this.isConnectedFlag) {
      this.messageQueue.push(message);
      throw new Error('Not connected - message queued');
    }

    if (this.peer) {
      // Simulate async delivery
      setImmediate(() => {
        this.peer?.emit('message', message);
      });
    }

    this.emit('message_sent', message);
  }

  public async receive(): Promise<MCPMessage> {
    return new Promise((resolve, reject) => {
      if (!this.isConnectedFlag) {
        reject(new Error('Not connected'));
        return;
      }

      const messageHandler = (message: MCPMessage) => {
        this.removeListener('message', messageHandler);
        resolve(message);
      };

      this.once('message', messageHandler);

      // Set timeout for receive operation
      setTimeout(() => {
        this.removeListener('message', messageHandler);
        reject(new Error('Receive timeout'));
      }, 30000);
    });
  }

  public isConnected(): boolean {
    return this.isConnectedFlag;
  }

  public getConnectionId(): string {
    return this.connectionId;
  }

  /**
   * Connects this transport to another local transport for bidirectional communication.
   */
  public connectToPeer(peer: MCPLocalTransport): void {
    this.peer = peer;
    peer.peer = this;
  }

  private handlePeerDisconnect(): void {
    this.peer = null;
    if (this.isConnectedFlag) {
      this.isConnectedFlag = false;
      this.emit('disconnected');
    }
  }

  private processMessageQueue(): void {
    while (this.messageQueue.length > 0 && this.isConnectedFlag) {
      const message = this.messageQueue.shift();
      if (message) {
        this.send(message).catch(error => {
          this.emit('error', error);
        });
      }
    }
  }
}

/**
 * Transport factory for creating appropriate transport instances.
 */
class MCPTransportFactory {
  /**
   * Creates a transport instance based on the URL scheme.
   */
  public static createTransport(url: string, options: any = {}): MCPTransport {
    const urlObj = new URL(url);

    switch (urlObj.protocol) {
      case 'ws:':
      case 'wss:':
        return new MCPWebSocketTransport(url, options);

      case 'http:':
      case 'https:':
        return new MCPHTTPTransport(url, options);

      case 'local:':
        return new MCPLocalTransport();

      default:
        throw new Error(`Unsupported transport protocol: ${urlObj.protocol}`);
    }
  }

  /**
   * Creates a WebSocket transport with specific options.
   */
  public static createWebSocketTransport(url: string, options: WebSocketTransportOptions = {}): MCPWebSocketTransport {
    return new MCPWebSocketTransport(url, options);
  }

  /**
   * Creates an HTTP transport with specific options.
   */
  public static createHTTPTransport(baseUrl: string, options: HTTPTransportOptions = {}): MCPHTTPTransport {
    return new MCPHTTPTransport(baseUrl, options);
  }

  /**
   * Creates a local transport for same-process communication.
   */
  public static createLocalTransport(): MCPLocalTransport {
    return new MCPLocalTransport();
  }

  /**
   * Creates a pair of connected local transports for testing.
   */
  public static createLocalTransportPair(): [MCPLocalTransport, MCPLocalTransport] {
    const transport1 = new MCPLocalTransport();
    const transport2 = new MCPLocalTransport();

    transport1.connectToPeer(transport2);

    return [transport1, transport2];
  }
}

/**
 * WebSocket transport options.
 */
export interface WebSocketTransportOptions {
  maxReconnectAttempts?: number;
  reconnectDelay?: number;
  heartbeatInterval?: number;
}

/**
 * HTTP transport options.
 */
export interface HTTPTransportOptions {
  pollingDelay?: number;
  timeout?: number;
  retryAttempts?: number;
}

/**
 * Transport connection manager for handling multiple transports.
 */
class MCPTransportManager extends EventEmitter {
  private transports: Map<string, MCPTransport>;
  private activeTransport: MCPTransport | null;
  private failoverTransports: MCPTransport[];

  constructor() {
    super();
    this.transports = new Map();
    this.activeTransport = null;
    this.failoverTransports = [];
  }

  /**
   * Adds a transport to the manager.
   */
  public addTransport(id: string, transport: MCPTransport, isPrimary: boolean = false): void {
    this.transports.set(id, transport);

    if (isPrimary || !this.activeTransport) {
      this.activeTransport = transport;
    } else {
      this.failoverTransports.push(transport);
    }

    // Set up event forwarding
    const eventTransport = transport as any;
    eventTransport.on('connected', () => this.emit('transport_connected', { id, transport }));
    eventTransport.on('disconnected', () => this.handleTransportDisconnected(id, transport));
    eventTransport.on('error', (error: any) => this.emit('transport_error', { id, transport, error }));
    eventTransport.on('message', (message: any) => this.emit('message', message));
  }

  /**
   * Removes a transport from the manager.
   */
  public removeTransport(id: string): void {
    const transport = this.transports.get(id);
    if (transport) {
      const eventTransport = transport as any;
      eventTransport.removeAllListeners?.();
      this.transports.delete(id);

      if (this.activeTransport === transport) {
        this.activeTransport = this.failoverTransports.shift() || null;
      } else {
        const index = this.failoverTransports.indexOf(transport);
        if (index >= 0) {
          this.failoverTransports.splice(index, 1);
        }
      }
    }
  }

  /**
   * Connects using the active transport.
   */
  public async connect(): Promise<void> {
    if (!this.activeTransport) {
      throw new Error('No active transport available');
    }

    try {
      await this.activeTransport.connect();
    } catch (error) {
      await this.tryFailover();
      throw error;
    }
  }

  /**
   * Disconnects all transports.
   */
  public async disconnect(): Promise<void> {
    const promises: Promise<void>[] = [];

    for (const transport of this.transports.values()) {
      promises.push(transport.disconnect().catch(() => {})); // Ignore errors
    }

    await Promise.allSettled(promises);
  }

  /**
   * Sends a message using the active transport.
   */
  public async send(message: MCPMessage): Promise<void> {
    if (!this.activeTransport || !this.activeTransport.isConnected()) {
      throw new Error('No active transport connection');
    }

    return this.activeTransport.send(message);
  }

  /**
   * Receives a message from the active transport.
   */
  public async receive(): Promise<MCPMessage> {
    if (!this.activeTransport || !this.activeTransport.isConnected()) {
      throw new Error('No active transport connection');
    }

    return this.activeTransport.receive();
  }

  /**
   * Checks if any transport is connected.
   */
  public isConnected(): boolean {
    return this.activeTransport?.isConnected() || false;
  }

  /**
   * Gets the active transport.
   */
  public getActiveTransport(): MCPTransport | null {
    return this.activeTransport;
  }

  /**
   * Gets all registered transports.
   */
  public getAllTransports(): Map<string, MCPTransport> {
    return new Map(this.transports);
  }

  private handleTransportDisconnected(id: string, transport: MCPTransport): void {
    this.emit('transport_disconnected', { id, transport });

    if (this.activeTransport === transport) {
      this.tryFailover();
    }
  }

  private async tryFailover(): Promise<void> {
    if (this.failoverTransports.length === 0) {
      this.activeTransport = null;
      this.emit('all_transports_disconnected');
      return;
    }

    const nextTransport = this.failoverTransports.shift()!;
    this.activeTransport = nextTransport;

    try {
      await nextTransport.connect();
      this.emit('failover_successful', { transport: nextTransport });
    } catch (error) {
      this.emit('failover_failed', { transport: nextTransport, error });
      await this.tryFailover(); // Try next transport
    }
  }
}

// Export all transport classes
export {
  MCPWebSocketTransport,
  MCPHTTPTransport,
  MCPLocalTransport,
  MCPTransportFactory,
  MCPTransportManager,
};

