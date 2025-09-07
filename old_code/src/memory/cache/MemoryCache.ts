/*
 * Copyright 2025 DevelApp.ai
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * MemoryCache - High-performance caching system for zero-copy data
 *
 * Provides efficient caching of frequently accessed memory regions
 * with LRU eviction and cache-friendly memory layouts.
 */

import { MemoryArena, MemoryPointer } from '../arena/MemoryArena';
import { StringInterner } from '../strings/StringInterner';

export interface CacheEntry<T> {
    key: string;
    value: T;
    pointer: MemoryPointer;
    accessCount: number;
    lastAccessed: number;
    size: number;
}

export interface CacheStatistics {
    totalEntries: number;
    totalSize: number;
    hitCount: number;
    missCount: number;
    evictionCount: number;
    hitRate: number;
    averageAccessCount: number;
    memoryEfficiency: number;
}

export interface CacheConfig {
    maxEntries: number;
    maxMemorySize: number;
    ttlMs?: number;
    enableLRU: boolean;
    enableAccessCounting: boolean;
    prefetchThreshold: number;
}

/**
 * LRU Cache with zero-copy memory management
 */
export class MemoryCache<T> {
  private entries: Map<string, CacheEntry<T>> = new Map();
  private accessOrder: string[] = []; // For LRU tracking
  private arena: MemoryArena;
  private config: CacheConfig;
  private serializer: CacheSerializer<T>;

  // Statistics
  private hitCount: number = 0;
  private missCount: number = 0;
  private evictionCount: number = 0;
  private totalSize: number = 0;

  constructor(
    arena: MemoryArena,
    serializer: CacheSerializer<T>,
    config: Partial<CacheConfig> = {},
  ) {
    this.arena = arena;
    this.serializer = serializer;
    this.config = {
      maxEntries: 1000,
      maxMemorySize: 64 * 1024 * 1024, // 64MB
      enableLRU: true,
      enableAccessCounting: true,
      prefetchThreshold: 10,
      ...config,
    };
  }

  /**
     * Get a value from the cache
     */
  get(key: string): T | undefined {
    const entry = this.entries.get(key);

    if (!entry) {
      this.missCount++;
      return undefined;
    }

    // Check TTL if configured
    if (this.config.ttlMs && Date.now() - entry.lastAccessed > this.config.ttlMs) {
      this.delete(key);
      this.missCount++;
      return undefined;
    }

    // Update access statistics
    this.hitCount++;
    entry.accessCount++;
    entry.lastAccessed = Date.now();

    // Update LRU order
    if (this.config.enableLRU) {
      this.updateAccessOrder(key);
    }

    // Deserialize from memory
    return this.serializer.deserialize(entry.pointer, entry.size);
  }

  /**
     * Put a value in the cache
     */
  put(key: string, value: T): void {
    // Check if key already exists
    if (this.entries.has(key)) {
      this.delete(key);
    }

    // Serialize value to memory
    const size = this.serializer.getSize(value);
    const pointer = this.arena.allocate(size, 8);
    this.serializer.serialize(value, pointer, size);

    // Create cache entry
    const entry: CacheEntry<T> = {
      key,
      value, // Keep a reference for fast access
      pointer,
      accessCount: 1,
      lastAccessed: Date.now(),
      size,
    };

    // Check capacity constraints
    this.ensureCapacity(size);

    // Add to cache
    this.entries.set(key, entry);
    this.totalSize += size;

    // Update access order
    if (this.config.enableLRU) {
      this.accessOrder.push(key);
    }
  }

  /**
     * Delete a value from the cache
     */
  delete(key: string): boolean {
    const entry = this.entries.get(key);
    if (!entry) {
      return false;
    }

    // Remove from data structures
    this.entries.delete(key);
    this.totalSize -= entry.size;

    // Remove from access order
    if (this.config.enableLRU) {
      const index = this.accessOrder.indexOf(key);
      if (index >= 0) {
        this.accessOrder.splice(index, 1);
      }
    }

    return true;
  }

  /**
     * Check if a key exists in the cache
     */
  has(key: string): boolean {
    const entry = this.entries.get(key);
    if (!entry) {
      return false;
    }

    // Check TTL
    if (this.config.ttlMs && Date.now() - entry.lastAccessed > this.config.ttlMs) {
      this.delete(key);
      return false;
    }

    return true;
  }

  /**
     * Clear all entries from the cache
     */
  clear(): void {
    this.entries.clear();
    this.accessOrder = [];
    this.totalSize = 0;
  }

  /**
     * Get cache statistics
     */
  getStatistics(): CacheStatistics {
    const totalRequests = this.hitCount + this.missCount;
    const hitRate = totalRequests > 0 ? this.hitCount / totalRequests : 0;

    const totalAccessCount = Array.from(this.entries.values())
      .reduce((sum, entry) => sum + entry.accessCount, 0);
    const averageAccessCount = this.entries.size > 0 ? totalAccessCount / this.entries.size : 0;

    const memoryEfficiency = this.config.maxMemorySize > 0 ?
      this.totalSize / this.config.maxMemorySize : 0;

    return {
      totalEntries: this.entries.size,
      totalSize: this.totalSize,
      hitCount: this.hitCount,
      missCount: this.missCount,
      evictionCount: this.evictionCount,
      hitRate,
      averageAccessCount,
      memoryEfficiency,
    };
  }

  /**
     * Get all keys in the cache
     */
  keys(): string[] {
    return Array.from(this.entries.keys());
  }

  /**
     * Get all values in the cache
     */
  values(): T[] {
    return Array.from(this.entries.values()).map(entry => entry.value);
  }

  /**
     * Get entries sorted by access count (most accessed first)
     */
  getHotEntries(limit: number = 10): Array<{ key: string; accessCount: number }> {
    return Array.from(this.entries.entries())
      .map(([key, entry]) => ({ key, accessCount: entry.accessCount }))
      .sort((a, b) => b.accessCount - a.accessCount)
      .slice(0, limit);
  }

  /**
     * Prefetch related entries based on access patterns
     */
  prefetch(keys: string[]): void {
    // This is a simplified prefetch - in practice you'd use more sophisticated algorithms
    for (const key of keys) {
      if (!this.has(key)) {
        // Trigger a miss to potentially load the data
        this.get(key);
      }
    }
  }

  /**
     * Ensure cache capacity constraints are met
     */
  private ensureCapacity(newEntrySize: number): void {
    // Check memory constraint
    while (this.totalSize + newEntrySize > this.config.maxMemorySize && this.entries.size > 0) {
      this.evictLRU();
    }

    // Check entry count constraint
    while (this.entries.size >= this.config.maxEntries && this.entries.size > 0) {
      this.evictLRU();
    }
  }

  /**
     * Evict the least recently used entry
     */
  private evictLRU(): void {
    if (this.accessOrder.length === 0) {
      // Fallback: evict any entry
      const firstKey = this.entries.keys().next().value;
      if (firstKey) {
        this.delete(firstKey);
        this.evictionCount++;
      }
      return;
    }

    const lruKey = this.accessOrder[0];
    this.delete(lruKey);
    this.evictionCount++;
  }

  /**
     * Update the access order for LRU tracking
     */
  private updateAccessOrder(key: string): void {
    const index = this.accessOrder.indexOf(key);
    if (index >= 0) {
      this.accessOrder.splice(index, 1);
    }
    this.accessOrder.push(key);
  }
}

/**
 * Interface for cache serialization
 */
export interface CacheSerializer<T> {
    serialize(value: T, pointer: MemoryPointer, size: number): void;
    deserialize(pointer: MemoryPointer, size: number): T;
    getSize(value: T): number;
}

/**
 * String cache with string interner integration
 */
export class StringCache extends MemoryCache<string> {
  constructor(arena: MemoryArena, stringInterner: StringInterner, config?: Partial<CacheConfig>) {
    const serializer: CacheSerializer<string> = {
      serialize(value: string, pointer: MemoryPointer): void {
        const stringId = stringInterner.intern(value);
        pointer.segment.view.setUint32(pointer.offset, stringId, true);
      },

      deserialize(pointer: MemoryPointer): string {
        const stringId = pointer.segment.view.getUint32(pointer.offset, true);
        return stringInterner.getString(stringId);
      },

      getSize(): number {
        return 4; // String ID is 4 bytes
      },
    };

    super(arena, serializer, config);
  }
}

/**
 * Multi-level cache with L1 and L2 levels
 */
export class MultiLevelCache<T> {
  private l1Cache: MemoryCache<T>;
  private l2Cache: MemoryCache<T>;

  constructor(
    arena: MemoryArena,
    serializer: CacheSerializer<T>,
    l1Config: Partial<CacheConfig> = {},
    l2Config: Partial<CacheConfig> = {},
  ) {
    // L1: Small, fast cache
    this.l1Cache = new MemoryCache(arena, serializer, {
      maxEntries: 100,
      maxMemorySize: 1024 * 1024, // 1MB
      enableLRU: true,
      ...l1Config,
    });

    // L2: Larger, slower cache
    this.l2Cache = new MemoryCache(arena, serializer, {
      maxEntries: 10000,
      maxMemorySize: 64 * 1024 * 1024, // 64MB
      enableLRU: true,
      ...l2Config,
    });
  }

  /**
     * Get a value from the multi-level cache
     */
  get(key: string): T | undefined {
    // Try L1 first
    let value = this.l1Cache.get(key);
    if (value !== undefined) {
      return value;
    }

    // Try L2
    value = this.l2Cache.get(key);
    if (value !== undefined) {
      // Promote to L1
      this.l1Cache.put(key, value);
      return value;
    }

    return undefined;
  }

  /**
     * Put a value in the multi-level cache
     */
  put(key: string, value: T): void {
    // Put in both levels
    this.l1Cache.put(key, value);
    this.l2Cache.put(key, value);
  }

  /**
     * Delete from both cache levels
     */
  delete(key: string): boolean {
    const l1Result = this.l1Cache.delete(key);
    const l2Result = this.l2Cache.delete(key);
    return l1Result || l2Result;
  }

  /**
     * Check if key exists in either cache level
     */
  has(key: string): boolean {
    return this.l1Cache.has(key) || this.l2Cache.has(key);
  }

  /**
     * Get combined statistics
     */
  getStatistics(): { l1: CacheStatistics; l2: CacheStatistics } {
    return {
      l1: this.l1Cache.getStatistics(),
      l2: this.l2Cache.getStatistics(),
    };
  }

  /**
     * Clear both cache levels
     */
  clear(): void {
    this.l1Cache.clear();
    this.l2Cache.clear();
  }
}

/**
 * Cache manager for multiple cache instances
 */
export class CacheManager {
  private caches: Map<string, MemoryCache<any>> = new Map();
  private arena: MemoryArena;

  constructor(arena: MemoryArena) {
    this.arena = arena;
  }

  /**
     * Create a new cache
     */
  createCache<T>(
    name: string,
    serializer: CacheSerializer<T>,
    config?: Partial<CacheConfig>,
  ): MemoryCache<T> {
    const cache = new MemoryCache(this.arena, serializer, config);
    this.caches.set(name, cache);
    return cache;
  }

  /**
     * Get a cache by name
     */
  getCache<T>(name: string): MemoryCache<T> | undefined {
    return this.caches.get(name);
  }

  /**
     * Delete a cache
     */
  deleteCache(name: string): boolean {
    const cache = this.caches.get(name);
    if (cache) {
      cache.clear();
      this.caches.delete(name);
      return true;
    }
    return false;
  }

  /**
     * Get statistics for all caches
     */
  getAllStatistics(): Map<string, CacheStatistics> {
    const stats = new Map<string, CacheStatistics>();
    for (const [name, cache] of this.caches) {
      stats.set(name, cache.getStatistics());
    }
    return stats;
  }

  /**
     * Clear all caches
     */
  clearAll(): void {
    for (const cache of this.caches.values()) {
      cache.clear();
    }
  }
}
