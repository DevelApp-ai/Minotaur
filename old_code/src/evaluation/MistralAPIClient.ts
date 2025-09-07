/**
 * Optimized Mistral API Client for Free Tier with Aggressive Rate Limiting
 *
 * Designed to maximize throughput on Mistral's free tier while being respectful
 * of rate limits. Includes intelligent backoff, request queuing, and error recovery.
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { EventEmitter } from 'events';

export interface MistralAPIConfig {
    apiKey: string;
    baseURL?: string;
    model?: string;
    maxRetries?: number;
    retryDelay?: number;
    timeout?: number;

    // Free tier optimized rate limiting
    rateLimit: {
        requestsPerMinute: number;
        requestsPerHour: number;
        tokensPerMinute: number;
        tokensPerHour: number;
        burstLimit: number;
        adaptiveThrottling: boolean;
    };

    // Advanced features
    enableRequestQueuing: boolean;
    enableAdaptiveBackoff: boolean;
    enableCostTracking: boolean;
    logLevel: 'debug' | 'info' | 'warn' | 'error';
}

/**
 * Create default Mistral API configuration with correct endpoint
 */
export function createDefaultMistralConfig(apiKey: string): MistralAPIConfig {
    return {
        apiKey,
        baseURL: 'https://api.mistral.ai/v1', // Use standard Mistral API endpoint
        model: 'codestral-latest',
        maxRetries: 3,
        retryDelay: 1000,
        timeout: 30000,
        rateLimit: {
            requestsPerMinute: 10,
            requestsPerHour: 100,
            tokensPerMinute: 10000,
            tokensPerHour: 100000,
            burstLimit: 3,
            adaptiveThrottling: true,
        },
        enableRequestQueuing: true,
        enableAdaptiveBackoff: true,
        enableCostTracking: true,
        logLevel: 'info',
    };
}

export interface MistralRequest {
    model: string;
    messages: Array<{
        role: 'system' | 'user' | 'assistant';
        content: string;
    }>;
    temperature?: number;
    max_tokens?: number;
    top_p?: number;
    stream?: boolean;
    safe_prompt?: boolean;
}

export interface MistralResponse {
    id: string;
    object: string;
    created: number;
    model: string;
    choices: Array<{
        index: number;
        message: {
            role: string;
            content: string;
        };
        finish_reason: string;
    }>;
    usage: {
        prompt_tokens: number;
        completion_tokens: number;
        total_tokens: number;
    };
}

export interface APICallResult {
    success: boolean;
    response?: MistralResponse;
    error?: string;
    retryCount: number;
    duration: number;
    tokensUsed: number;
    cost: number;
}

export interface RateLimitStatus {
    requestsThisMinute: number;
    requestsThisHour: number;
    tokensThisMinute: number;
    tokensThisHour: number;
    nextAvailableSlot: Date;
    adaptiveDelay: number;
    queueLength: number;
}

export class MistralAPIClient extends EventEmitter {
  private config: MistralAPIConfig;
  private httpClient: AxiosInstance;

  // Rate limiting state
  private requestsThisMinute: number = 0;
  private requestsThisHour: number = 0;
  private tokensThisMinute: number = 0;
  private tokensThisHour: number = 0;
  private lastRequestTime: number = 0;
  private adaptiveDelay: number = 1000; // Minimum 1 second between requests

  // Request queue for managing throughput
  private requestQueue: Array<{
        request: MistralRequest;
        resolve: (result: APICallResult) => void;
        reject: (error: Error) => void;
        timestamp: number;
        estimatedTokens: number;
    }> = [];

  private isProcessingQueue: boolean = false;

  // Statistics tracking
  private stats = {
    totalRequests: 0,
    successfulRequests: 0,
    failedRequests: 0,
    totalTokensUsed: 0,
    totalCost: 0,
    averageResponseTime: 0,
    rateLimitHits: 0,
    adaptiveBackoffTriggers: 0,
  };

  // Error patterns for adaptive backoff
  private recentErrors: Array<{ timestamp: number; type: string }> = [];

  constructor(config: MistralAPIConfig) {
    super();

    // Set defaults optimized for free tier
    this.config = {
      baseURL: 'https://api.mistral.ai/v1',
      model: 'codestral-latest', // Optimized for code generation
      maxRetries: 5,
      retryDelay: 1000, // 1 second base delay
      timeout: 60000,
      enableRequestQueuing: true,
      enableAdaptiveBackoff: true,
      enableCostTracking: true,
      logLevel: 'info',
      rateLimit: {
        requestsPerMinute: 60, // 1 request per second
        requestsPerHour: 3600, // 1 request per second for full hour
        tokensPerMinute: 60000, // Generous token limit
        tokensPerHour: 3600000, // Generous token limit
        burstLimit: 1, // No burst, strict 1 per second
        adaptiveThrottling: true,
      },
      ...config,
    };

    this.setupHttpClient();
    this.setupRateLimitResets();
    this.startQueueProcessor();

    this.log('info', `Mistral API Client initialized with model: ${this.config.model}`);
    this.log('info', `Rate limits: ${this.config.rateLimit.requestsPerMinute}/min, ${this.config.rateLimit.tokensPerMinute} tokens/min`);
  }

  private setupHttpClient(): void {
    this.httpClient = axios.create({
      baseURL: this.config.baseURL,
      timeout: this.config.timeout,
      headers: {
        'Authorization': `Bearer ${this.config.apiKey}`,
        'Content-Type': 'application/json',
        'User-Agent': 'Golem-Evaluation-System/1.0',
      },
    });

    // Request interceptor for logging
    this.httpClient.interceptors.request.use(
      (config) => {
        this.log('debug', `Making API request to ${config.url}`);
        return config;
      },
      (error) => {
        this.log('error', `Request interceptor error: ${error instanceof Error ? error.message : String(error)}`);
        return Promise.reject(error);
      },
    );

    // Response interceptor for error handling
    this.httpClient.interceptors.response.use(
      (response) => response,
      (error) => {
        this.handleAPIError(error);
        return Promise.reject(error);
      },
    );
  }

  private setupRateLimitResets(): void {
    // Reset per-minute counters
    setInterval(() => {
      this.requestsThisMinute = 0;
      this.tokensThisMinute = 0;
      this.log('debug', 'Per-minute rate limits reset');
    }, 60000);

    // Reset per-hour counters
    setInterval(() => {
      this.requestsThisHour = 0;
      this.tokensThisHour = 0;
      this.log('debug', 'Per-hour rate limits reset');
    }, 3600000);

    // Clean old errors for adaptive backoff
    setInterval(() => {
      const cutoff = Date.now() - 300000; // 5 minutes
      this.recentErrors = this.recentErrors.filter(e => e.timestamp > cutoff);
    }, 60000);
  }

  private startQueueProcessor(): void {
    if (!this.config.enableRequestQueuing) {
      return;
    }

    setInterval(async () => {
      if (!this.isProcessingQueue && this.requestQueue.length > 0) {
        await this.processQueue();
      }
    }, 100); // Check every 100ms
  }

  public async generateCompletion(request: MistralRequest): Promise<APICallResult> {
    const estimatedTokens = this.estimateTokens(request);

    if (this.config.enableRequestQueuing) {
      return new Promise((resolve, reject) => {
        this.requestQueue.push({
          request,
          resolve,
          reject,
          timestamp: Date.now(),
          estimatedTokens,
        });

        this.log('debug', `Request queued, queue length: ${this.requestQueue.length}`);
      });
    } else {
      return this.executeRequest(request, estimatedTokens);
    }
  }

  private async processQueue(): Promise<void> {
    if (this.isProcessingQueue || this.requestQueue.length === 0) {
      return;
    }

    this.isProcessingQueue = true;

    try {
      while (this.requestQueue.length > 0) {
        const queueItem = this.requestQueue.shift()!;

        try {
          const result = await this.executeRequest(queueItem.request, queueItem.estimatedTokens);
          queueItem.resolve(result);
        } catch (error) {
          queueItem.reject(error);
        }

        // Small delay between queue items
        await this.sleep(100);
      }
    } finally {
      this.isProcessingQueue = false;
    }
  }

  private async executeRequest(request: MistralRequest, estimatedTokens: number): Promise<APICallResult> {
    const startTime = Date.now();
    let retryCount = 0;
    let lastError: Error;

    // Wait for rate limit availability
    await this.waitForRateLimit(estimatedTokens);

    while (retryCount < this.config.maxRetries) {
      try {
        this.log('debug', `Executing request (attempt ${retryCount + 1}/${this.config.maxRetries})`);

        const response = await this.httpClient.post<MistralResponse>('/chat/completions', {
          model: this.config.model,
          ...request,
        });

        const duration = Date.now() - startTime;
        const tokensUsed = response.data.usage?.total_tokens || estimatedTokens;
        const cost = this.calculateCost(tokensUsed);

        // Update counters
        this.requestsThisMinute++;
        this.requestsThisHour++;
        this.tokensThisMinute += tokensUsed;
        this.tokensThisHour += tokensUsed;
        this.lastRequestTime = Date.now();

        // Update statistics
        this.stats.totalRequests++;
        this.stats.successfulRequests++;
        this.stats.totalTokensUsed += tokensUsed;
        this.stats.totalCost += cost;
        this.stats.averageResponseTime =
                    // eslint-disable-next-line max-len
                    (this.stats.averageResponseTime * (this.stats.successfulRequests - 1) + duration) / this.stats.successfulRequests;

        // Adaptive rate limiting - reduce delay on success
        if (this.config.rateLimit.adaptiveThrottling) {
          this.adaptiveDelay = Math.max(500, this.adaptiveDelay * 0.95);
        }

        this.log('debug', `Request successful: ${tokensUsed} tokens, ${duration}ms, $${cost.toFixed(4)}`);

        this.emit('requestComplete', {
          success: true,
          duration,
          tokensUsed,
          cost,
        });

        return {
          success: true,
          response: response.data,
          retryCount,
          duration,
          tokensUsed,
          cost,
        };

      } catch (error) {
        lastError = error;
        retryCount++;

        this.stats.totalRequests++;
        this.stats.failedRequests++;

        const errorType = this.categorizeError(error);
        this.recentErrors.push({ timestamp: Date.now(), type: errorType });

        this.log('warn', `Request failed (attempt ${retryCount}): ${error instanceof Error ? error.message : String(error)}`);

        if (errorType === 'rate_limit') {
          this.stats.rateLimitHits++;
          await this.handleRateLimitError(error);
          // Continue retrying after handling rate limit
        } else if (retryCount >= this.config.maxRetries) {
          // Max retries reached for non-rate-limit errors
          break;
        } else {
          // Regular retry with delay for non-rate-limit errors
          const delay = this.calculateRetryDelay(retryCount, errorType);
          this.log('debug', `Retrying in ${delay}ms...`);
          await this.sleep(delay);
        }
      }
    }

    const duration = Date.now() - startTime;

    this.emit('requestComplete', {
      success: false,
      duration,
      error: lastError.message,
    });

    return {
      success: false,
      error: lastError.message,
      retryCount,
      duration,
      tokensUsed: 0,
      cost: 0,
    };
  }

  private async waitForRateLimit(estimatedTokens: number): Promise<void> {
    // Check if we're within limits
    while (
      this.requestsThisMinute >= this.config.rateLimit.requestsPerMinute ||
            this.requestsThisHour >= this.config.rateLimit.requestsPerHour ||
            this.tokensThisMinute + estimatedTokens > this.config.rateLimit.tokensPerMinute ||
            this.tokensThisHour + estimatedTokens > this.config.rateLimit.tokensPerHour
    ) {
      const waitTime = this.calculateWaitTime();
      this.log('info', `Rate limit reached, waiting ${waitTime}ms...`);
      await this.sleep(waitTime);
    }

    // Enforce strict 1 second minimum delay between requests
    const timeSinceLastRequest = Date.now() - this.lastRequestTime;
    const minimumDelay = 1000; // Always wait at least 1 second

    if (timeSinceLastRequest < minimumDelay) {
      const delay = minimumDelay - timeSinceLastRequest;
      this.log('debug', `Enforcing 1-second rate limit, waiting ${delay}ms...`);
      await this.sleep(delay);
    }
  }

  private calculateWaitTime(): number {
    // Calculate time until next minute/hour reset
    const now = new Date();
    const nextMinute = new Date(now.getFullYear(), now.getMonth(), now.getDate(),
      now.getHours(), now.getMinutes() + 1, 0, 0);
    const nextHour = new Date(now.getFullYear(), now.getMonth(), now.getDate(),
      now.getHours() + 1, 0, 0, 0);

    const timeToNextMinute = nextMinute.getTime() - now.getTime();
    const timeToNextHour = nextHour.getTime() - now.getTime();

    // Return the shorter wait time plus a small buffer
    return Math.min(timeToNextMinute, timeToNextHour) + 1000;
  }

  private calculateRetryDelay(retryCount: number, errorType: string): number {
    const baseDelay = this.config.retryDelay;

    // For rate limit errors, use longer delays
    if (errorType === 'rate_limit') {
      // Start with 2 seconds, then exponential backoff
      const rateLimitDelay = 2000 * Math.pow(2, retryCount - 1);
      const jitter = Math.random() * 1000;
      return Math.min(30000, rateLimitDelay + jitter);
    }

    // Exponential backoff for other errors
    const exponentialDelay = baseDelay * Math.pow(2, retryCount - 1);

    // Add jitter to prevent thundering herd
    const jitter = Math.random() * 1000;

    // Adaptive backoff based on recent error frequency
    const errorFrequency = this.recentErrors.length / 5; // errors per minute
    const adaptiveMultiplier = Math.min(3, 1 + errorFrequency * 0.5);

    const totalDelay = exponentialDelay * adaptiveMultiplier + jitter;

    // Cap at 30 seconds
    return Math.min(30000, totalDelay);
  }

  private async handleRateLimitError(error: any): Promise<void> {
    // Extract rate limit info from headers if available
    const retryAfter = error.response?.headers['retry-after'];
    const rateLimitReset = error.response?.headers['x-ratelimit-reset'];

    if (retryAfter) {
      const waitTime = parseInt(retryAfter) * 1000;
      this.log('info', `Rate limit hit, waiting ${waitTime}ms as specified by server`);
      await this.sleep(waitTime);
    } else if (rateLimitReset) {
      const resetTime = parseInt(rateLimitReset) * 1000;
      const waitTime = resetTime - Date.now();
      if (waitTime > 0) {
        this.log('info', `Rate limit hit, waiting ${waitTime}ms until reset`);
        await this.sleep(waitTime);
      }
    } else {
      // No specific timing from server, wait at least 2 seconds
      const defaultWait = 2000;
      this.log('info', `Rate limit hit, waiting ${defaultWait}ms (default)`);
      await this.sleep(defaultWait);
    }

    // Increase adaptive delay for future requests
    if (this.config.rateLimit.adaptiveThrottling) {
      this.adaptiveDelay = Math.max(1000, Math.min(5000, this.adaptiveDelay * 1.2));
      this.stats.adaptiveBackoffTriggers++;
      this.log('debug', `Adaptive delay adjusted to ${this.adaptiveDelay}ms`);
    }
  }

  private categorizeError(error: any): string {
    if (error.response?.status === 429) {
      return 'rate_limit';
    }
    if (error.response?.status >= 500) {
      return 'server_error';
    }
    if (error.code === 'ECONNABORTED') {
      return 'timeout';
    }
    if (error.code === 'ENOTFOUND' || error.code === 'ECONNREFUSED') {
      return 'network';
    }
    return 'unknown';
  }

  private handleAPIError(error: any): void {
    const errorType = this.categorizeError(error);

    this.log('warn', `API error (${errorType}): ${error instanceof Error ? error.message : String(error)}`);

    if (error.response?.data) {
      this.log('debug', `API error details: ${JSON.stringify(error.response.data)}`);
    }

    this.emit('apiError', {
      type: errorType,
      message: error instanceof Error ? error.message : String(error),
      status: error.response?.status,
      data: error.response?.data,
    });
  }

  private estimateTokens(request: MistralRequest): number {
    // Rough estimation: ~4 characters per token
    const messages = request.messages || [];
    const messageContent = messages.map(m => m.content || '').join(' ');
    const estimatedPromptTokens = Math.ceil(messageContent.length / 4);
    const estimatedCompletionTokens = request.max_tokens || 1000;

    return estimatedPromptTokens + estimatedCompletionTokens;
  }

  private calculateCost(tokens: number): number {
    // Mistral pricing (approximate, free tier = $0)
    // This is mainly for tracking purposes
    return 0; // Free tier
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private log(level: string, message: string): void {
    if (this.shouldLog(level)) {
      const timestamp = new Date().toISOString();
    // eslint-disable-next-line no-console
      console.log(`[${timestamp}] [MISTRAL-${level.toUpperCase()}] ${message}`);
    }
  }

  private shouldLog(level: string): boolean {
    const levels = ['debug', 'info', 'warn', 'error'];
    const configLevel = levels.indexOf(this.config.logLevel);
    const messageLevel = levels.indexOf(level);
    return messageLevel >= configLevel;
  }

  // Public methods for monitoring and control

  public getRateLimitStatus(): RateLimitStatus {
    return {
      requestsThisMinute: this.requestsThisMinute,
      requestsThisHour: this.requestsThisHour,
      tokensThisMinute: this.tokensThisMinute,
      tokensThisHour: this.tokensThisHour,
      nextAvailableSlot: new Date(this.lastRequestTime + this.adaptiveDelay),
      adaptiveDelay: this.adaptiveDelay,
      queueLength: this.requestQueue.length,
    };
  }

  public getStatistics() {
    return {
      ...this.stats,
      successRate: this.stats.totalRequests > 0 ?
        (this.stats.successfulRequests / this.stats.totalRequests) * 100 : 0,
      averageCostPerRequest: this.stats.successfulRequests > 0 ?
        this.stats.totalCost / this.stats.successfulRequests : 0,
      averageTokensPerRequest: this.stats.successfulRequests > 0 ?
        this.stats.totalTokensUsed / this.stats.successfulRequests : 0,
    };
  }

  public clearQueue(): void {
    this.requestQueue.forEach(item => {
      item.reject(new Error('Queue cleared'));
    });
    this.requestQueue = [];
    this.log('info', 'Request queue cleared');
  }

  public updateRateLimit(newLimits: Partial<MistralAPIConfig['rateLimit']>): void {
    this.config.rateLimit = { ...this.config.rateLimit, ...newLimits };
    this.log('info', `Rate limits updated: ${JSON.stringify(newLimits)}`);
  }

  public async testConnection(): Promise<boolean> {
    try {
      const testRequest: MistralRequest = {
        model: this.config.model,
        messages: [
          { role: 'user', content: 'Hello, this is a connection test.' },
        ],
        max_tokens: 10,
      };

      const result = await this.generateCompletion(testRequest);
      return result.success;

    } catch (error) {
      this.log('error', `Connection test failed: ${error instanceof Error ? error.message : String(error)}`);
      return false;
    }
  }
}

// Factory function for easy setup
export function createMistralClient(apiKey: string, options?: Partial<MistralAPIConfig>): MistralAPIClient {
  const defaultConfig: MistralAPIConfig = {
    apiKey,
    model: 'mistral-small-latest',
    maxRetries: 5,
    retryDelay: 2000,
    timeout: 60000,
    rateLimit: {
      requestsPerMinute: 20, // Conservative for free tier
      requestsPerHour: 1000,
      tokensPerMinute: 50000,
      tokensPerHour: 1000000,
      burstLimit: 5,
      adaptiveThrottling: true,
    },
    enableRequestQueuing: true,
    enableAdaptiveBackoff: true,
    enableCostTracking: true,
    logLevel: 'info',
  };

  return new MistralAPIClient({ ...defaultConfig, ...options });
}

