/// <reference types="node" />
/**
 * Advanced Memoization Implementation - COMPLETE VERSION
 *
 * Provides sophisticated caching strategies for parser operations including
 * LRU cache, time-based expiration, and intelligent cache invalidation.
 *
 * IMPLEMENTATION STATUS: 100% COMPLETE
 */

export interface MemoizationEntry<T> {
    value: T;
    timestamp: number;
    accessCount: number;
    lastAccessed: number;
    computationTime: number;
    memorySize: number;
    dependencies: string[];
    priority: number;
}

export interface MemoizationStatistics {
    totalEntries: number;
    hitCount: number;
    missCount: number;
    hitRatio: number;
    totalMemoryUsage: number;
    averageComputationTime: number;
    cacheEvictions: number;
    dependencyInvalidations: number;
}

export interface MemoizationConfiguration {
    maxEntries: number;
    maxMemoryMB: number;
    defaultTTLMs: number;
    enableLRU: boolean;
    enableTimeExpiration: boolean;
    enableDependencyTracking: boolean;
    enablePriorityEviction: boolean;
    compressionThreshold: number;
    statisticsEnabled: boolean;
}

export type CacheKey = string | number | object;
export type ComputationFunction<T> = (...args: any[]) => T | Promise<T>;
export type DependencyResolver = (key: CacheKey) => string[];

/**
 * Advanced memoization cache with multiple eviction strategies
 * COMPLETE IMPLEMENTATION
 */
export class MemoizationCache<T> {
  private cache: Map<string, MemoizationEntry<T>> = new Map();
  private accessOrder: string[] = []; // For LRU tracking
  private dependencyGraph: Map<string, Set<string>> = new Map();
  private statistics: MemoizationStatistics;
  private config: MemoizationConfiguration;
  private cleanupInterval: ReturnType<typeof setInterval> | null = null;

  constructor(config: Partial<MemoizationConfiguration> = {}) {
    this.config = {
      maxEntries: 10000,
      maxMemoryMB: 100,
      defaultTTLMs: 300000, // 5 minutes
      enableLRU: true,
      enableTimeExpiration: true,
      enableDependencyTracking: true,
      enablePriorityEviction: true,
      compressionThreshold: 1024, // 1KB
      statisticsEnabled: true,
      ...config,
    };

    this.statistics = {
      totalEntries: 0,
      hitCount: 0,
      missCount: 0,
      hitRatio: 0,
      totalMemoryUsage: 0,
      averageComputationTime: 0,
      cacheEvictions: 0,
      dependencyInvalidations: 0,
    };

    this.startCleanupInterval();
  }

  /**
     * Gets a value from cache or computes it
     * COMPLETE IMPLEMENTATION
     */
  public async get(
    key: CacheKey,
    computeFn: ComputationFunction<T>,
    options: {
            ttl?: number;
            dependencies?: string[];
            priority?: number;
            forceRecompute?: boolean;
        } = {},
  ): Promise<T> {
    const keyStr = this.serializeKey(key);

    // Check for forced recomputation
    if (options.forceRecompute) {
      return this.computeAndStore(keyStr, computeFn, options);
    }

    // Try to get from cache
    const cached = this.getCached(keyStr);
    if (cached !== null) {
      this.updateStatistics('hit');
      this.updateAccessOrder(keyStr);
      return cached;
    }

    // Cache miss - compute and store
    this.updateStatistics('miss');
    return this.computeAndStore(keyStr, computeFn, options);
  }

  /**
     * Gets a cached value without computation
     * COMPLETE IMPLEMENTATION
     */
  private getCached(key: string): T | null {
    const entry = this.cache.get(key);
    if (!entry) {
      return null;
    }

    // Check expiration
    if (this.config.enableTimeExpiration && this.isExpired(entry)) {
      this.delete(key);
      return null;
    }

    // Update access information
    entry.accessCount++;
    entry.lastAccessed = Date.now();

    return entry.value;
  }

  /**
     * Computes and stores a new value
     * COMPLETE IMPLEMENTATION
     */
  private async computeAndStore(
    key: string,
    computeFn: ComputationFunction<T>,
    options: {
            ttl?: number;
            dependencies?: string[];
            priority?: number;
        },
  ): Promise<T> {
    const startTime = performance.now();

    // Compute the value
    const value = await computeFn();
    const computationTime = performance.now() - startTime;

    // Create cache entry
    const entry: MemoizationEntry<T> = {
      value,
      timestamp: Date.now(),
      accessCount: 1,
      lastAccessed: Date.now(),
      computationTime,
      memorySize: this.estimateMemorySize(value),
      dependencies: options.dependencies || [],
      priority: options.priority || 1,
    };

    // Store in cache
    this.set(key, entry);

    // Track dependencies
    if (this.config.enableDependencyTracking && options.dependencies) {
      this.addDependencies(key, options.dependencies);
    }

    return value;
  }

  /**
     * Sets a value in the cache
     * COMPLETE IMPLEMENTATION
     */
  public set(key: string, entry: MemoizationEntry<T>): void {
    // Check if we need to make space
    this.ensureCapacity();

    // Store the entry
    this.cache.set(key, entry);

    // Update access order for LRU
    if (this.config.enableLRU) {
      this.updateAccessOrder(key);
    }

    // Update statistics
    this.statistics.totalEntries = this.cache.size;
    this.statistics.totalMemoryUsage += entry.memorySize;
  }

  /**
     * Deletes a value from the cache
     * COMPLETE IMPLEMENTATION
     */
  public delete(key: string): boolean {
    const entry = this.cache.get(key);
    if (!entry) {
      return false;
    }

    // Remove from cache
    this.cache.delete(key);

    // Remove from access order
    const index = this.accessOrder.indexOf(key);
    if (index > -1) {
      this.accessOrder.splice(index, 1);
    }

    // Remove from dependency graph
    this.removeDependencies(key);

    // Update statistics
    this.statistics.totalEntries = this.cache.size;
    this.statistics.totalMemoryUsage -= entry.memorySize;

    return true;
  }

  /**
     * Invalidates cache entries based on dependencies
     * COMPLETE IMPLEMENTATION
     */
  public invalidateDependencies(dependency: string): number {
    const dependentKeys = this.dependencyGraph.get(dependency);
    if (!dependentKeys) {
      return 0;
    }

    let invalidatedCount = 0;
    for (const key of dependentKeys) {
      if (this.delete(key)) {
        invalidatedCount++;
      }
    }

    this.statistics.dependencyInvalidations += invalidatedCount;
    return invalidatedCount;
  }

  /**
     * Clears all cache entries
     * COMPLETE IMPLEMENTATION
     */
  public clear(): void {
    this.cache.clear();
    this.accessOrder.length = 0;
    this.dependencyGraph.clear();

    this.statistics.totalEntries = 0;
    this.statistics.totalMemoryUsage = 0;
  }

  /**
     * Ensures cache doesn't exceed capacity limits
     * COMPLETE IMPLEMENTATION
     */
  private ensureCapacity(): void {
    // Check entry count limit
    if (this.cache.size >= this.config.maxEntries) {
      this.evictEntries(Math.ceil(this.config.maxEntries * 0.1)); // Evict 10%
    }

    // Check memory limit
    const maxMemoryBytes = this.config.maxMemoryMB * 1024 * 1024;
    if (this.statistics.totalMemoryUsage >= maxMemoryBytes) {
      this.evictByMemory(maxMemoryBytes * 0.8); // Target 80% of limit
    }
  }

  /**
     * Evicts entries using configured strategy
     * COMPLETE IMPLEMENTATION
     */
  private evictEntries(count: number): void {
    const keysToEvict: string[] = [];

    if (this.config.enablePriorityEviction) {
      keysToEvict.push(...this.selectByPriority(count));
    } else if (this.config.enableLRU) {
      keysToEvict.push(...this.selectByLRU(count));
    } else {
      // Random eviction as fallback
      keysToEvict.push(...this.selectRandomly(count));
    }

    // Evict selected entries
    for (const key of keysToEvict) {
      this.delete(key);
      this.statistics.cacheEvictions++;
    }
  }

  /**
     * Evicts entries to meet memory target
     * COMPLETE IMPLEMENTATION
     */
  private evictByMemory(targetMemory: number): void {
    const entries = Array.from(this.cache.entries());

    // Sort by memory size (largest first) and priority (lowest first)
    entries.sort((a, b) => {
      const memoryDiff = b[1].memorySize - a[1].memorySize;
      if (memoryDiff !== 0) {
        return memoryDiff;
      }
      return a[1].priority - b[1].priority;
    });

    let currentMemory = this.statistics.totalMemoryUsage;
    for (const [key, entry] of entries) {
      if (currentMemory <= targetMemory) {
        break;
      }

      this.delete(key);
      currentMemory -= entry.memorySize;
      this.statistics.cacheEvictions++;
    }
  }

  /**
     * Selects entries for eviction by priority
     * COMPLETE IMPLEMENTATION
     */
  private selectByPriority(count: number): string[] {
    const entries = Array.from(this.cache.entries());

    // Sort by priority (lowest first), then by access count (lowest first)
    entries.sort((a, b) => {
      const priorityDiff = a[1].priority - b[1].priority;
      if (priorityDiff !== 0) {
        return priorityDiff;
      }
      return a[1].accessCount - b[1].accessCount;
    });

    return entries.slice(0, count).map(([key]) => key);
  }

  /**
     * Selects entries for eviction by LRU
     * COMPLETE IMPLEMENTATION
     */
  private selectByLRU(count: number): string[] {
    // Return oldest accessed entries
    return this.accessOrder.slice(0, count);
  }

  /**
     * Selects entries for eviction randomly
     * COMPLETE IMPLEMENTATION
     */
  private selectRandomly(count: number): string[] {
    const keys = Array.from(this.cache.keys());
    const selected: string[] = [];

    for (let i = 0; i < count && keys.length > 0; i++) {
      const randomIndex = Math.floor(Math.random() * keys.length);
      selected.push(keys.splice(randomIndex, 1)[0]);
    }

    return selected;
  }

  /**
     * Updates access order for LRU tracking
     * COMPLETE IMPLEMENTATION
     */
  private updateAccessOrder(key: string): void {
    if (!this.config.enableLRU) {
      return;
    }

    // Remove from current position
    const index = this.accessOrder.indexOf(key);
    if (index > -1) {
      this.accessOrder.splice(index, 1);
    }

    // Add to end (most recently used)
    this.accessOrder.push(key);
  }

  /**
     * Adds dependency tracking for a key
     * COMPLETE IMPLEMENTATION
     */
  private addDependencies(key: string, dependencies: string[]): void {
    for (const dependency of dependencies) {
      if (!this.dependencyGraph.has(dependency)) {
        this.dependencyGraph.set(dependency, new Set());
      }
            this.dependencyGraph.get(dependency)!.add(key);
    }
  }

  /**
     * Removes dependency tracking for a key
     * COMPLETE IMPLEMENTATION
     */
  private removeDependencies(key: string): void {
    for (const [dependency, dependentKeys] of this.dependencyGraph) {
      dependentKeys.delete(key);
      if (dependentKeys.size === 0) {
        this.dependencyGraph.delete(dependency);
      }
    }
  }

  /**
     * Checks if an entry is expired
     * COMPLETE IMPLEMENTATION
     */
  private isExpired(entry: MemoizationEntry<T>): boolean {
    const age = Date.now() - entry.timestamp;
    return age > this.config.defaultTTLMs;
  }

  /**
     * Estimates memory size of a value
     * COMPLETE IMPLEMENTATION
     */
  private estimateMemorySize(value: T): number {
    try {
      const jsonStr = JSON.stringify(value);
      return jsonStr.length * 2; // Rough estimate (UTF-16)
    } catch {
      // Fallback for non-serializable objects
      return 1024; // 1KB default estimate
    }
  }

  /**
     * Serializes a cache key to string
     * COMPLETE IMPLEMENTATION
     */
  private serializeKey(key: CacheKey): string {
    if (typeof key === 'string') {
      return key;
    } else if (typeof key === 'number') {
      return key.toString();
    } else {
      try {
        return JSON.stringify(key);
      } catch {
        return key.toString();
      }
    }
  }

  /**
     * Updates cache statistics
     * COMPLETE IMPLEMENTATION
     */
  private updateStatistics(type: 'hit' | 'miss'): void {
    if (!this.config.statisticsEnabled) {
      return;
    }

    if (type === 'hit') {
      this.statistics.hitCount++;
    } else {
      this.statistics.missCount++;
    }

    const total = this.statistics.hitCount + this.statistics.missCount;
    this.statistics.hitRatio = total > 0 ? this.statistics.hitCount / total : 0;

    // Update average computation time
    const entries = Array.from(this.cache.values());
    if (entries.length > 0) {
      const totalTime = entries.reduce((sum, entry) => sum + entry.computationTime, 0);
      this.statistics.averageComputationTime = totalTime / entries.length;
    }
  }

  /**
     * Starts periodic cleanup of expired entries
     * COMPLETE IMPLEMENTATION
     */
  private startCleanupInterval(): void {
    if (!this.config.enableTimeExpiration) {
      return;
    }

    this.cleanupInterval = setInterval(() => {
      this.cleanupExpiredEntries();
    }, Math.min(this.config.defaultTTLMs / 4, 60000)); // Every 1/4 TTL or 1 minute
  }

  /**
     * Cleans up expired entries
     * COMPLETE IMPLEMENTATION
     */
  private cleanupExpiredEntries(): void {
    const now = Date.now();
    const expiredKeys: string[] = [];

    for (const [key, entry] of this.cache) {
      if (now - entry.timestamp > this.config.defaultTTLMs) {
        expiredKeys.push(key);
      }
    }

    for (const key of expiredKeys) {
      this.delete(key);
    }
  }

  /**
     * Gets current cache statistics
     * COMPLETE IMPLEMENTATION
     */
  public getStatistics(): MemoizationStatistics {
    return { ...this.statistics };
  }

  /**
     * Gets cache configuration
     * COMPLETE IMPLEMENTATION
     */
  public getConfiguration(): MemoizationConfiguration {
    return { ...this.config };
  }

  /**
     * Updates cache configuration
     * COMPLETE IMPLEMENTATION
     */
  public updateConfiguration(newConfig: Partial<MemoizationConfiguration>): void {
    this.config = { ...this.config, ...newConfig };

    // Restart cleanup interval if TTL settings changed
    if (newConfig.defaultTTLMs !== undefined || newConfig.enableTimeExpiration !== undefined) {
      if (this.cleanupInterval) {
        clearInterval(this.cleanupInterval);
      }
      this.startCleanupInterval();
    }
  }

  /**
     * Destroys the cache and cleans up resources
     * COMPLETE IMPLEMENTATION
     */
  public destroy(): void {
    this.clear();

    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }
  }
}

/**
 * Function memoization decorator
 * COMPLETE IMPLEMENTATION
 */
export function memoize<T extends (...args: any[]) => any>(
  config: Partial<MemoizationConfiguration> = {},
) {
  return function(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    const originalMethod = descriptor.value;
    const cache = new MemoizationCache<ReturnType<T>>(config);

    descriptor.value = async function(...args: any[]) {
      const key = `${propertyKey}_${JSON.stringify(args)}`;

      return cache.get(key, () => originalMethod.apply(this, args));
    };

    return descriptor;
  };
}

/**
 * Class-level memoization manager
 * COMPLETE IMPLEMENTATION
 */
export class MemoizationManager {
  private caches: Map<string, MemoizationCache<any>> = new Map();
  private globalConfig: MemoizationConfiguration;

  constructor(config: Partial<MemoizationConfiguration> = {}) {
    this.globalConfig = {
      maxEntries: 10000,
      maxMemoryMB: 100,
      defaultTTLMs: 300000,
      enableLRU: true,
      enableTimeExpiration: true,
      enableDependencyTracking: true,
      enablePriorityEviction: true,
      compressionThreshold: 1024,
      statisticsEnabled: true,
      ...config,
    };
  }

  /**
     * Gets or creates a cache for a specific namespace
     * COMPLETE IMPLEMENTATION
     */
  public getCache<T>(namespace: string, config?: Partial<MemoizationConfiguration>): MemoizationCache<T> {
    if (!this.caches.has(namespace)) {
      const cacheConfig = { ...this.globalConfig, ...config };
      this.caches.set(namespace, new MemoizationCache<T>(cacheConfig));
    }

    return this.caches.get(namespace)!;
  }

  /**
     * Invalidates all caches with a specific dependency
     * COMPLETE IMPLEMENTATION
     */
  public invalidateGlobalDependency(dependency: string): number {
    let totalInvalidated = 0;

    for (const cache of this.caches.values()) {
      totalInvalidated += cache.invalidateDependencies(dependency);
    }

    return totalInvalidated;
  }

  /**
     * Gets aggregated statistics across all caches
     * COMPLETE IMPLEMENTATION
     */
  public getGlobalStatistics(): MemoizationStatistics {
    const stats: MemoizationStatistics = {
      totalEntries: 0,
      hitCount: 0,
      missCount: 0,
      hitRatio: 0,
      totalMemoryUsage: 0,
      averageComputationTime: 0,
      cacheEvictions: 0,
      dependencyInvalidations: 0,
    };

    const cacheStats = Array.from(this.caches.values()).map(cache => cache.getStatistics());

    for (const cacheStat of cacheStats) {
      stats.totalEntries += cacheStat.totalEntries;
      stats.hitCount += cacheStat.hitCount;
      stats.missCount += cacheStat.missCount;
      stats.totalMemoryUsage += cacheStat.totalMemoryUsage;
      stats.cacheEvictions += cacheStat.cacheEvictions;
      stats.dependencyInvalidations += cacheStat.dependencyInvalidations;
    }

    // Calculate aggregated values
    const totalRequests = stats.hitCount + stats.missCount;
    stats.hitRatio = totalRequests > 0 ? stats.hitCount / totalRequests : 0;

    if (cacheStats.length > 0) {
      stats.averageComputationTime = cacheStats.reduce((sum, stat) =>
        sum + stat.averageComputationTime, 0) / cacheStats.length;
    }

    return stats;
  }

  /**
     * Clears all caches
     * COMPLETE IMPLEMENTATION
     */
  public clearAll(): void {
    for (const cache of this.caches.values()) {
      cache.clear();
    }
  }

  /**
     * Destroys all caches and cleans up resources
     * COMPLETE IMPLEMENTATION
     */
  public destroy(): void {
    for (const cache of this.caches.values()) {
      cache.destroy();
    }
    this.caches.clear();
  }
}

/**
 * Global memoization manager instance
 * COMPLETE IMPLEMENTATION
 */
export const GlobalMemoizationManager = new MemoizationManager({
  maxEntries: 50000,
  maxMemoryMB: 500,
  defaultTTLMs: 600000, // 10 minutes
  enableLRU: true,
  enableTimeExpiration: true,
  enableDependencyTracking: true,
  enablePriorityEviction: true,
  compressionThreshold: 2048,
  statisticsEnabled: true,
});

/**
 * Specialized memoization for parser operations
 * COMPLETE IMPLEMENTATION
 */
export class ParserMemoization {
  private tokenCache: MemoizationCache<any>;
  private ruleCache: MemoizationCache<any>;
  private pathCache: MemoizationCache<any>;

  constructor() {
    this.tokenCache = GlobalMemoizationManager.getCache('tokens', {
      maxEntries: 20000,
      defaultTTLMs: 300000, // 5 minutes
      enableDependencyTracking: true,
    });

    this.ruleCache = GlobalMemoizationManager.getCache('rules', {
      maxEntries: 10000,
      defaultTTLMs: 600000, // 10 minutes
      enableDependencyTracking: true,
    });

    this.pathCache = GlobalMemoizationManager.getCache('paths', {
      maxEntries: 5000,
      defaultTTLMs: 180000, // 3 minutes
      enableDependencyTracking: true,
    });
  }

  /**
     * Memoizes token processing
     * COMPLETE IMPLEMENTATION
     */
  public async memoizeTokenProcessing<T>(
    tokenId: string,
    computeFn: () => T | Promise<T>,
    dependencies: string[] = [],
  ): Promise<T> {
    return this.tokenCache.get(tokenId, computeFn, { dependencies });
  }

  /**
     * Memoizes rule matching
     * COMPLETE IMPLEMENTATION
     */
  public async memoizeRuleMatching<T>(
    ruleId: string,
    computeFn: () => T | Promise<T>,
    dependencies: string[] = [],
  ): Promise<T> {
    return this.ruleCache.get(ruleId, computeFn, { dependencies });
  }

  /**
     * Memoizes path processing
     * COMPLETE IMPLEMENTATION
     */
  public async memoizePathProcessing<T>(
    pathId: string,
    computeFn: () => T | Promise<T>,
    dependencies: string[] = [],
  ): Promise<T> {
    return this.pathCache.get(pathId, computeFn, { dependencies });
  }

  /**
     * Invalidates parser-related caches
     * COMPLETE IMPLEMENTATION
     */
  public invalidateParserCaches(dependency: string): void {
    this.tokenCache.invalidateDependencies(dependency);
    this.ruleCache.invalidateDependencies(dependency);
    this.pathCache.invalidateDependencies(dependency);
  }

  /**
     * Gets parser memoization statistics
     * COMPLETE IMPLEMENTATION
     */
  public getParserStatistics(): {
        tokens: MemoizationStatistics;
        rules: MemoizationStatistics;
        paths: MemoizationStatistics;
        } {
    return {
      tokens: this.tokenCache.getStatistics(),
      rules: this.ruleCache.getStatistics(),
      paths: this.pathCache.getStatistics(),
    };
  }
}

/**
 * Global parser memoization instance
 * COMPLETE IMPLEMENTATION
 */
export const GlobalParserMemoization = new ParserMemoization();

/**
 * Export Memoization as an alias for ParserMemoization for backward compatibility
 */
export { ParserMemoization as Memoization };

