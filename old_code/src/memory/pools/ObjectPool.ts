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
 * ObjectPool - High-performance object pooling system
 *
 * Provides efficient object reuse to reduce garbage collection pressure
 * and improve memory allocation performance for frequently used objects.
 */

import { MemoryArena, MemoryPointer } from '../arena/MemoryArena';

export interface PoolableObject {
    reset(): void;
    isInUse(): boolean;
    setInUse(inUse: boolean): void;
}

export interface ObjectFactory<T extends PoolableObject> {
    create(arena: MemoryArena): T;
    reset(obj: T): void;
    validate(obj: T): boolean;
}

export interface PoolStatistics {
    totalObjects: number;
    availableObjects: number;
    inUseObjects: number;
    totalAllocations: number;
    totalDeallocations: number;
    hitRate: number;
    memoryUsage: number;
    peakUsage: number;
}

export class ObjectPool<T extends PoolableObject> {
  private available: T[] = [];
  private inUse: Set<T> = new Set();
  private factory: ObjectFactory<T>;
  private arena: MemoryArena;
  private maxSize: number;
  private minSize: number;

  // Statistics
  private totalAllocations: number = 0;
  private totalDeallocations: number = 0;
  private hits: number = 0;
  private misses: number = 0;
  private peakUsage: number = 0;

  constructor(
    factory: ObjectFactory<T>,
    arena: MemoryArena,
    minSize: number = 10,
    maxSize: number = 1000,
  ) {
    this.factory = factory;
    this.arena = arena;
    this.minSize = minSize;
    this.maxSize = maxSize;

    // Pre-populate with minimum objects
    this.preallocate(minSize);
  }

  /**
     * Acquire an object from the pool
     */
  acquire(): T {
    this.totalAllocations++;

    let obj: T;

    if (this.available.length > 0) {
      // Reuse existing object
      obj = this.available.pop()!;
      this.hits++;
    } else {
      // Create new object
      obj = this.factory.create(this.arena);
      this.misses++;
    }

    // Reset object state
    this.factory.reset(obj);
    obj.setInUse(true);

    // Track usage
    this.inUse.add(obj);
    this.peakUsage = Math.max(this.peakUsage, this.inUse.size);

    return obj;
  }

  /**
     * Release an object back to the pool
     */
  release(obj: T): void {
    if (!this.inUse.has(obj)) {
      throw new Error('Object is not from this pool or already released');
    }

    this.totalDeallocations++;

    // Validate object state
    if (!this.factory.validate(obj)) {
    // eslint-disable-next-line no-console
      console.warn('Invalid object returned to pool, discarding');
      this.inUse.delete(obj);
      return;
    }

    // Reset and mark as available
    obj.reset();
    obj.setInUse(false);
    this.inUse.delete(obj);

    // Return to pool if under max size
    if (this.available.length < this.maxSize) {
      this.available.push(obj);
    }
    // Otherwise let it be garbage collected
  }

  /**
     * Pre-allocate objects to reach minimum pool size
     */
  private preallocate(count: number): void {
    for (let i = 0; i < count; i++) {
      const obj = this.factory.create(this.arena);
      obj.setInUse(false);
      this.available.push(obj);
    }
  }

  /**
     * Trim pool to minimum size
     */
  trim(): void {
    while (this.available.length > this.minSize) {
      this.available.pop();
    }
  }

  /**
     * Clear all objects from the pool
     */
  clear(): void {
    this.available = [];
    this.inUse.clear();
  }

  /**
     * Get pool statistics
     */
  getStatistics(): PoolStatistics {
    const totalRequests = this.hits + this.misses;
    const hitRate = totalRequests > 0 ? this.hits / totalRequests : 0;

    return {
      totalObjects: this.available.length + this.inUse.size,
      availableObjects: this.available.length,
      inUseObjects: this.inUse.size,
      totalAllocations: this.totalAllocations,
      totalDeallocations: this.totalDeallocations,
      hitRate,
      memoryUsage: this.estimateMemoryUsage(),
      peakUsage: this.peakUsage,
    };
  }

  /**
     * Estimate memory usage of the pool
     */
  private estimateMemoryUsage(): number {
    // This is a rough estimate - in practice you'd need more sophisticated tracking
    return (this.available.length + this.inUse.size) * 64; // Assume 64 bytes per object
  }

  /**
     * Force garbage collection of unused objects
     */
  forceGC(): void {
    // Remove objects that are no longer valid
    this.available = this.available.filter(obj => this.factory.validate(obj));

    // Remove invalid in-use objects (shouldn't happen in normal operation)
    const inUseArray = Array.from(this.inUse);
    for (const obj of inUseArray) {
      if (!this.factory.validate(obj)) {
        this.inUse.delete(obj);
      }
    }
  }
}

/**
 * Specialized pool for memory pointers
 */
export class MemoryPointerPool {
  private pool: ObjectPool<PooledMemoryPointer>;

  constructor(arena: MemoryArena, minSize: number = 100, maxSize: number = 10000) {
    const factory: ObjectFactory<PooledMemoryPointer> = {
      create: (arena: MemoryArena) => new PooledMemoryPointer(arena),
      reset: (obj: PooledMemoryPointer) => obj.reset(),
      validate: (obj: PooledMemoryPointer) => obj.isValid(),
    };

    this.pool = new ObjectPool(factory, arena, minSize, maxSize);
  }

  /**
     * Acquire a memory pointer
     */
  acquire(size: number, alignment: number = 8): PooledMemoryPointer {
    const pointer = this.pool.acquire();
    pointer.allocate(size, alignment);
    return pointer;
  }

  /**
     * Release a memory pointer
     */
  release(pointer: PooledMemoryPointer): void {
    this.pool.release(pointer);
  }

  /**
     * Get pool statistics
     */
  getStatistics(): PoolStatistics {
    return this.pool.getStatistics();
  }
}

/**
 * Pooled memory pointer implementation
 */
export class PooledMemoryPointer implements PoolableObject {
  private arena: MemoryArena;
  private pointer: MemoryPointer | null = null;
  private inUse: boolean = false;

  constructor(arena: MemoryArena) {
    this.arena = arena;
  }

  /**
     * Allocate memory for this pointer
     */
  allocate(size: number, alignment: number = 8): void {
    if (this.pointer) {
      throw new Error('Pointer already allocated');
    }

    this.pointer = this.arena.allocate(size, alignment);
  }

  /**
     * Get the underlying memory pointer
     */
  getPointer(): MemoryPointer {
    if (!this.pointer) {
      throw new Error('Pointer not allocated');
    }
    return this.pointer;
  }

  /**
     * Reset the pooled pointer
     */
  reset(): void {
    this.pointer = null;
    this.inUse = false;
  }

  /**
     * Check if the pointer is in use
     */
  isInUse(): boolean {
    return this.inUse;
  }

  /**
     * Set the in-use status
     */
  setInUse(inUse: boolean): void {
    this.inUse = inUse;
  }

  /**
     * Check if the pointer is valid
     */
  isValid(): boolean {
    return this.pointer === null || (
      this.pointer.offset >= 0 &&
            this.pointer.offset < this.pointer.segment.size
    );
  }
}

/**
 * Pool manager for multiple object types
 */
export class PoolManager {
  private pools: Map<string, ObjectPool<any>> = new Map();
  private arena: MemoryArena;

  constructor(arena: MemoryArena) {
    this.arena = arena;
  }

  /**
     * Register a new pool
     */
  registerPool<T extends PoolableObject>(
    name: string,
    factory: ObjectFactory<T>,
    minSize: number = 10,
    maxSize: number = 1000,
  ): ObjectPool<T> {
    const pool = new ObjectPool(factory, this.arena, minSize, maxSize);
    this.pools.set(name, pool);
    return pool;
  }

  /**
     * Get a pool by name
     */
  getPool<T extends PoolableObject>(name: string): ObjectPool<T> | undefined {
    return this.pools.get(name);
  }

  /**
     * Acquire an object from a named pool
     */
  acquire<T extends PoolableObject>(poolName: string): T {
    const pool = this.pools.get(poolName);
    if (!pool) {
      throw new Error(`Pool '${poolName}' not found`);
    }
    return pool.acquire();
  }

  /**
     * Release an object to a named pool
     */
  release<T extends PoolableObject>(poolName: string, obj: T): void {
    const pool = this.pools.get(poolName);
    if (!pool) {
      throw new Error(`Pool '${poolName}' not found`);
    }
    pool.release(obj);
  }

  /**
     * Get statistics for all pools
     */
  getAllStatistics(): Map<string, PoolStatistics> {
    const stats = new Map<string, PoolStatistics>();
    const poolEntries = Array.from(this.pools.entries());
    for (const [name, pool] of poolEntries) {
      stats.set(name, pool.getStatistics());
    }
    return stats;
  }

  /**
     * Trim all pools to minimum size
     */
  trimAll(): void {
    const poolValues = Array.from(this.pools.values());
    for (const pool of poolValues) {
      pool.trim();
    }
  }

  /**
     * Clear all pools
     */
  clearAll(): void {
    const poolValues = Array.from(this.pools.values());
    for (const pool of poolValues) {
      pool.clear();
    }
    this.pools.clear();
  }

  /**
     * Force garbage collection on all pools
     */
  forceGCAll(): void {
    const poolValues = Array.from(this.pools.values());
    for (const pool of poolValues) {
      pool.forceGC();
    }
  }
}
