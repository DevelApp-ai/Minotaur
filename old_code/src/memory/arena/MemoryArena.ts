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
 * MemoryArena - Cap'n Proto inspired arena allocation system
 *
 * Provides efficient memory allocation with automatic cleanup and
 * zero-copy serialization capabilities.
 */

export interface MemorySegment {
    readonly id: number;
    readonly data: ArrayBuffer;
    readonly view: DataView;
    readonly size: number;
    offset: number;
}

export class OutOfMemoryError extends Error {
  constructor(requestedSize: number, availableSize: number) {
    super(`Out of memory: requested ${requestedSize} bytes, available ${availableSize} bytes`);
    this.name = 'OutOfMemoryError';
  }
}

export class MemoryArena {
  private segments: MemorySegment[] = [];
  private currentSegment: MemorySegment | null = null;
  private nextSegmentId: number = 0;
  private readonly initialSegmentSize: number;
  private readonly maxSegmentSize: number;
  private readonly growthFactor: number;

  // Statistics
  private totalAllocated: number = 0;
  private totalRequested: number = 0;
  private allocationCount: number = 0;

  constructor(
    initialSize: number = 64 * 1024, // 64KB default
    maxSize: number = 16 * 1024 * 1024, // 16MB max
    growthFactor: number = 2.0,
  ) {
    this.initialSegmentSize = initialSize;
    this.maxSegmentSize = maxSize;
    this.growthFactor = growthFactor;

    // Create initial segment
    this.addSegment(this.initialSegmentSize);
  }

  /**
     * Allocate memory with specified size and alignment
     */
  allocate(size: number, alignment: number = 8): MemoryPointer {
    if (size <= 0) {
      throw new Error('Allocation size must be positive');
    }

    if (alignment <= 0 || (alignment & (alignment - 1)) !== 0) {
      throw new Error('Alignment must be a positive power of 2');
    }

    this.allocationCount++;
    this.totalRequested += size;

    // Align size to the specified boundary
    const alignedSize = this.alignSize(size, alignment);

    // Try to allocate in current segment
    if (this.currentSegment && this.canAllocateInSegment(this.currentSegment, alignedSize, alignment)) {
      return this.allocateInSegment(this.currentSegment, alignedSize, alignment);
    }

    // Need a new segment
    const newSegmentSize = this.calculateNewSegmentSize(alignedSize);
    this.addSegment(newSegmentSize);

    if (!this.currentSegment || !this.canAllocateInSegment(this.currentSegment, alignedSize, alignment)) {
      throw new OutOfMemoryError(alignedSize, this.currentSegment?.size ?? 0);
    }

    return this.allocateInSegment(this.currentSegment, alignedSize, alignment);
  }

  /**
     * Allocate multiple objects of the same size efficiently
     */
  allocateBatch(objectSize: number, count: number, alignment: number = 8): MemoryPointer[] {
    const alignedSize = this.alignSize(objectSize, alignment);
    const totalSize = alignedSize * count;

    // Try to allocate all objects in a contiguous block
    try {
      const basePointer = this.allocate(totalSize, alignment);
      const pointers: MemoryPointer[] = [];

      for (let i = 0; i < count; i++) {
        const offset = basePointer.offset + (i * alignedSize);
        pointers.push(new MemoryPointer(basePointer.segment, offset));
      }

      return pointers;
    } catch (OutOfMemoryError) {
      // Fall back to individual allocations
      const pointers: MemoryPointer[] = [];
      for (let i = 0; i < count; i++) {
        pointers.push(this.allocate(objectSize, alignment));
      }
      return pointers;
    }
  }

  /**
     * Get memory usage statistics
     */
  getStatistics(): MemoryArenaStatistics {
    const totalCapacity = this.segments.reduce((sum, segment) => sum + segment.size, 0);
    const totalUsed = this.segments.reduce((sum, segment) => sum + segment.offset, 0);

    return {
      totalCapacity,
      totalUsed,
      totalAllocated: this.totalAllocated,
      totalRequested: this.totalRequested,
      allocationCount: this.allocationCount,
      segmentCount: this.segments.length,
      fragmentationRatio: totalCapacity > 0 ? (totalCapacity - totalUsed) / totalCapacity : 0,
      averageAllocationSize: this.allocationCount > 0 ? this.totalRequested / this.allocationCount : 0,
    };
  }

  /**
     * Reset the arena, freeing all allocated memory
     */
  reset(): void {
    this.segments = [];
    this.currentSegment = null;
    this.nextSegmentId = 0;
    this.totalAllocated = 0;
    this.totalRequested = 0;
    this.allocationCount = 0;

    // Create new initial segment
    this.addSegment(this.initialSegmentSize);
  }

  /**
     * Get all segments for serialization
     */
  getSegments(): readonly MemorySegment[] {
    return this.segments;
  }

  /**
     * Create a new arena from existing segments (for deserialization)
     */
  static fromSegments(segments: ArrayBuffer[]): MemoryArena {
    const arena = new MemoryArena(0); // Don't create initial segment
    arena.segments = [];
    arena.currentSegment = null;
    arena.nextSegmentId = 0;

    for (const buffer of segments) {
      const segment: MemorySegment = {
        id: arena.nextSegmentId++,
        data: buffer,
        view: new DataView(buffer),
        size: buffer.byteLength,
        offset: buffer.byteLength, // Mark as fully used
      };
      arena.segments.push(segment);
    }

    // Set the last segment as current if it has space
    if (arena.segments.length > 0) {
      const lastSegment = arena.segments[arena.segments.length - 1];
      if (lastSegment.offset < lastSegment.size) {
        arena.currentSegment = lastSegment;
      }
    }

    return arena;
  }

  private addSegment(size: number): void {
    const buffer = new ArrayBuffer(size);
    const segment: MemorySegment = {
      id: this.nextSegmentId++,
      data: buffer,
      view: new DataView(buffer),
      size: size,
      offset: 0,
    };

    this.segments.push(segment);
    this.currentSegment = segment;
  }

  private canAllocateInSegment(segment: MemorySegment, size: number, alignment: number): boolean {
    const alignedOffset = this.alignOffset(segment.offset, alignment);
    return alignedOffset + size <= segment.size;
  }

  private allocateInSegment(segment: MemorySegment, size: number, alignment: number): MemoryPointer {
    const alignedOffset = this.alignOffset(segment.offset, alignment);
    const pointer = new MemoryPointer(segment, alignedOffset);

    segment.offset = alignedOffset + size;
    this.totalAllocated += size;

    return pointer;
  }

  private calculateNewSegmentSize(minimumSize: number): number {
    const lastSegmentSize = this.currentSegment?.size ?? this.initialSegmentSize;
    const nextSize = Math.min(
      Math.max(lastSegmentSize * this.growthFactor, minimumSize),
      this.maxSegmentSize,
    );

    return Math.ceil(nextSize);
  }

  private alignSize(size: number, alignment: number): number {
    return Math.ceil(size / alignment) * alignment;
  }

  private alignOffset(offset: number, alignment: number): number {
    return Math.ceil(offset / alignment) * alignment;
  }
}

export class MemoryPointer {
  constructor(
        public readonly segment: MemorySegment,
        public readonly offset: number,
  ) {
    if (offset < 0 || offset >= segment.size) {
      throw new Error(`Invalid pointer offset: ${offset} (segment size: ${segment.size})`);
    }
  }

  /**
     * Dereference pointer to read typed data
     */
  deref<T>(): T {
    // This is a simplified implementation - in practice, you'd need
    // proper type checking and serialization format handling
    switch (typeof {} as T) {
      case 'number':
        return this.segment.view.getFloat64(this.offset, true) as unknown as T;
      case 'bigint':
        return this.segment.view.getBigUint64(this.offset, true) as unknown as T;
      default:
        throw new Error('Unsupported type for dereference');
    }
  }

  /**
     * Write typed data to pointer location
     */
  write<T>(value: T): void {
    switch (typeof value) {
      case 'number':
        this.segment.view.setFloat64(this.offset, value as number, true);
        break;
      case 'bigint':
        this.segment.view.setBigUint64(this.offset, value as bigint, true);
        break;
      default:
        throw new Error('Unsupported type for write');
    }
  }

  /**
     * Create a new pointer with an offset from this one
     */
  add(offset: number): MemoryPointer {
    return new MemoryPointer(this.segment, this.offset + offset);
  }

  /**
     * Check if this pointer is valid
     */
  isValid(): boolean {
    return this.offset >= 0 && this.offset < this.segment.size;
  }

  /**
     * Get remaining bytes from this pointer to end of segment
     */
  remainingBytes(): number {
    return this.segment.size - this.offset;
  }
}

export interface MemoryArenaStatistics {
    totalCapacity: number;
    totalUsed: number;
    totalAllocated: number;
    totalRequested: number;
    allocationCount: number;
    segmentCount: number;
    fragmentationRatio: number;
    averageAllocationSize: number;
}
