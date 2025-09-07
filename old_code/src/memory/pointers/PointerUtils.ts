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
 * PointerUtils - Utility functions for memory pointer operations
 *
 * Provides helper functions for pointer arithmetic, alignment, and validation
 * following Cap'n Proto conventions for memory layout.
 */

import { MemoryPointer, MemorySegment } from '../arena/MemoryArena';

export enum PointerType {
    NULL = 0,
    STRUCT = 1,
    LIST = 2,
    FAR = 3
}

export interface StructPointer {
    type: PointerType.STRUCT;
    offset: number;
    dataWords: number;
    pointerWords: number;
}

export interface ListPointer {
    type: PointerType.LIST;
    offset: number;
    elementSize: number;
    elementCount: number;
}

export interface FarPointer {
    type: PointerType.FAR;
    segmentId: number;
    offset: number;
    landingPad: boolean;
}

export type TypedPointerUnion = StructPointer | ListPointer | FarPointer;

export class PointerUtils {
  /**
     * Align size to the specified boundary
     */
  static alignSize(size: number, alignment: number): number {
    if (alignment <= 0 || (alignment & (alignment - 1)) !== 0) {
      throw new Error('Alignment must be a positive power of 2');
    }
    return Math.ceil(size / alignment) * alignment;
  }

  /**
     * Align offset to the specified boundary
     */
  static alignOffset(offset: number, alignment: number): number {
    if (alignment <= 0 || (alignment & (alignment - 1)) !== 0) {
      throw new Error('Alignment must be a positive power of 2');
    }
    return Math.ceil(offset / alignment) * alignment;
  }

  /**
     * Check if an offset is aligned to the specified boundary
     */
  static isAligned(offset: number, alignment: number): boolean {
    return (offset % alignment) === 0;
  }

  /**
     * Calculate the number of words needed for a given byte size
     */
  static bytesToWords(bytes: number): number {
    return Math.ceil(bytes / 8); // 8 bytes per word
  }

  /**
     * Calculate the number of bytes for a given word count
     */
  static wordsToBytes(words: number): number {
    return words * 8; // 8 bytes per word
  }

  /**
     * Encode a struct pointer
     */
  static encodeStructPointer(
    offset: number,
    dataWords: number,
    pointerWords: number,
  ): bigint {
    // Cap'n Proto struct pointer format:
    // Bit 0-1: Type (01 for struct)
    // Bit 2-31: Offset (30 bits, signed)
    // Bit 32-47: Data section size in words (16 bits)
    // Bit 48-63: Pointer section size in words (16 bits)

    if (offset < -0x20000000 || offset >= 0x20000000) {
      throw new Error('Struct pointer offset out of range');
    }

    if (dataWords < 0 || dataWords >= 0x10000) {
      throw new Error('Data words count out of range');
    }

    if (pointerWords < 0 || pointerWords >= 0x10000) {
      throw new Error('Pointer words count out of range');
    }

    const offsetBits = BigInt(offset & 0x3FFFFFFF) << BigInt(2);
    const typeBits = BigInt(PointerType.STRUCT);
    const dataBits = BigInt(dataWords) << BigInt(32);
    const pointerBits = BigInt(pointerWords) << BigInt(48);

    return typeBits | offsetBits | dataBits | pointerBits;
  }

  /**
     * Decode a struct pointer
     */
  static decodeStructPointer(encoded: bigint): StructPointer {
    const type = Number(encoded & BigInt(0x3));
    if (type !== PointerType.STRUCT) {
      throw new Error(`Expected struct pointer, got type ${type}`);
    }

    // Extract signed offset (30 bits)
    let offset = Number((encoded >> BigInt(2)) & BigInt(0x3FFFFFFF));
    if (offset >= 0x20000000) {
      offset -= 0x40000000; // Convert to signed
    }

    const dataWords = Number((encoded >> BigInt(32)) & BigInt(0xFFFF));
    const pointerWords = Number((encoded >> BigInt(48)) & BigInt(0xFFFF));

    return {
      type: PointerType.STRUCT,
      offset,
      dataWords,
      pointerWords,
    };
  }

  /**
     * Encode a list pointer
     */
  static encodeListPointer(
    offset: number,
    elementSize: number,
    elementCount: number,
  ): bigint {
    // Cap'n Proto list pointer format:
    // Bit 0-1: Type (10 for list)
    // Bit 2-31: Offset (30 bits, signed)
    // Bit 32-34: Element size (3 bits)
    // Bit 35-63: Element count (29 bits)

    if (offset < -0x20000000 || offset >= 0x20000000) {
      throw new Error('List pointer offset out of range');
    }

    if (elementSize < 0 || elementSize > 7) {
      throw new Error('Element size out of range (0-7)');
    }

    if (elementCount < 0 || elementCount >= 0x20000000) {
      throw new Error('Element count out of range');
    }

    const offsetBits = BigInt(offset & 0x3FFFFFFF) << BigInt(2);
    const typeBits = BigInt(PointerType.LIST);
    const sizeBits = BigInt(elementSize) << BigInt(32);
    const countBits = BigInt(elementCount) << BigInt(35);

    return typeBits | offsetBits | sizeBits | countBits;
  }

  /**
     * Decode a list pointer
     */
  static decodeListPointer(encoded: bigint): ListPointer {
    const type = Number(encoded & BigInt(0x3));
    if (type !== PointerType.LIST) {
      throw new Error(`Expected list pointer, got type ${type}`);
    }

    // Extract signed offset (30 bits)
    let offset = Number((encoded >> BigInt(2)) & BigInt(0x3FFFFFFF));
    if (offset >= 0x20000000) {
      offset -= 0x40000000; // Convert to signed
    }

    const elementSize = Number((encoded >> BigInt(32)) & BigInt(0x7));
    const elementCount = Number((encoded >> BigInt(35)) & BigInt(0x1FFFFFFF));

    return {
      type: PointerType.LIST,
      offset,
      elementSize,
      elementCount,
    };
  }

  /**
     * Encode a far pointer
     */
  static encodeFarPointer(
    segmentId: number,
    offset: number,
    landingPad: boolean = false,
  ): bigint {
    // Cap'n Proto far pointer format:
    // Bit 0-1: Type (11 for far)
    // Bit 2: Landing pad flag
    // Bit 3-31: Offset (29 bits)
    // Bit 32-63: Segment ID (32 bits)

    if (offset < 0 || offset >= 0x20000000) {
      throw new Error('Far pointer offset out of range');
    }

    if (segmentId < 0 || segmentId >= 0x100000000) {
      throw new Error('Segment ID out of range');
    }

    const typeBits = BigInt(PointerType.FAR);
    const landingPadBit = landingPad ? BigInt(0x4) : BigInt(0);
    const offsetBits = BigInt(offset) << BigInt(3);
    const segmentBits = BigInt(segmentId) << BigInt(32);

    return typeBits | landingPadBit | offsetBits | segmentBits;
  }

  /**
     * Decode a far pointer
     */
  static decodeFarPointer(encoded: bigint): FarPointer {
    const type = Number(encoded & BigInt(0x3));
    if (type !== PointerType.FAR) {
      throw new Error(`Expected far pointer, got type ${type}`);
    }

    const landingPad = (encoded & BigInt(0x4)) !== BigInt(0);
    const offset = Number((encoded >> BigInt(3)) & BigInt(0x1FFFFFFF));
    const segmentId = Number((encoded >> BigInt(32)) & BigInt(0xFFFFFFFF));

    return {
      type: PointerType.FAR,
      segmentId,
      offset,
      landingPad,
    };
  }

  /**
     * Check if a pointer is null
     */
  static isNullPointer(encoded: bigint): boolean {
    return encoded === BigInt(0);
  }

  /**
     * Get the type of an encoded pointer
     */
  static getPointerType(encoded: bigint): PointerType {
    return Number(encoded & BigInt(0x3)) as PointerType;
  }

  /**
     * Validate a memory pointer
     */
  static validatePointer(pointer: MemoryPointer): boolean {
    return pointer.offset >= 0 &&
               pointer.offset < pointer.segment.size &&
               pointer.segment.data.byteLength === pointer.segment.size;
  }

  /**
     * Calculate the distance between two pointers in the same segment
     */
  static pointerDistance(from: MemoryPointer, to: MemoryPointer): number {
    if (from.segment !== to.segment) {
      throw new Error('Pointers must be in the same segment');
    }
    return to.offset - from.offset;
  }

  /**
     * Check if two pointers are in the same segment
     */
  static sameSegment(a: MemoryPointer, b: MemoryPointer): boolean {
    return a.segment === b.segment;
  }

  /**
     * Create a bounds-checked view of memory
     */
  static createBoundsCheckedView(
    pointer: MemoryPointer,
    size: number,
  ): DataView {
    if (pointer.offset + size > pointer.segment.size) {
      throw new Error('View extends beyond segment bounds');
    }

    return new DataView(
      pointer.segment.data,
      pointer.offset,
      size,
    );
  }

  /**
     * Copy data between memory pointers
     */
  static copyMemory(
    source: MemoryPointer,
    destination: MemoryPointer,
    size: number,
  ): void {
    if (source.offset + size > source.segment.size) {
      throw new Error('Source copy extends beyond segment bounds');
    }

    if (destination.offset + size > destination.segment.size) {
      throw new Error('Destination copy extends beyond segment bounds');
    }

    const sourceArray = new Uint8Array(
      source.segment.data,
      source.offset,
      size,
    );

    const destArray = new Uint8Array(
      destination.segment.data,
      destination.offset,
      size,
    );

    destArray.set(sourceArray);
  }

  /**
     * Zero out memory at a pointer location
     */
  static zeroMemory(pointer: MemoryPointer, size: number): void {
    if (pointer.offset + size > pointer.segment.size) {
      throw new Error('Zero operation extends beyond segment bounds');
    }

    const array = new Uint8Array(
      pointer.segment.data,
      pointer.offset,
      size,
    );

    array.fill(0);
  }
}

/**
 * Typed pointer wrapper for type-safe memory access
 */
export class TypedPointerClass<T> {
  constructor(
        private pointer: MemoryPointer,
        private size: number,
        private serializer: MemorySerializer<T>,
  ) {
    if (!PointerUtils.validatePointer(pointer)) {
      throw new Error('Invalid memory pointer');
    }

    if (pointer.remainingBytes() < size) {
      throw new Error('Insufficient memory for typed pointer');
    }
  }

  read(): T {
    return this.serializer.deserialize(this.pointer, this.size);
  }

  write(value: T): void {
    this.serializer.serialize(value, this.pointer, this.size);
  }

  get memoryPointer(): MemoryPointer {
    return this.pointer;
  }

  get byteSize(): number {
    return this.size;
  }
}

/**
 * Interface for memory serialization
 */
export interface MemorySerializer<T> {
    serialize(value: T, pointer: MemoryPointer, size: number): void;
    deserialize(pointer: MemoryPointer, size: number): T;
    getSize(value: T): number;
}

/**
 * Basic serializers for primitive types
 */
export class PrimitiveSerializers {
  static readonly uint32: MemorySerializer<number> = {
    serialize(value: number, pointer: MemoryPointer): void {
      pointer.segment.view.setUint32(pointer.offset, value, true);
    },

    deserialize(pointer: MemoryPointer): number {
      return pointer.segment.view.getUint32(pointer.offset, true);
    },

    getSize(): number {
      return 4;
    },
  };

  static readonly uint64: MemorySerializer<bigint> = {
    serialize(value: bigint, pointer: MemoryPointer): void {
      pointer.segment.view.setBigUint64(pointer.offset, value, true);
    },

    deserialize(pointer: MemoryPointer): bigint {
      return pointer.segment.view.getBigUint64(pointer.offset, true);
    },

    getSize(): number {
      return 8;
    },
  };

  static readonly float64: MemorySerializer<number> = {
    serialize(value: number, pointer: MemoryPointer): void {
      pointer.segment.view.setFloat64(pointer.offset, value, true);
    },

    deserialize(pointer: MemoryPointer): number {
      return pointer.segment.view.getFloat64(pointer.offset, true);
    },

    getSize(): number {
      return 8;
    },
  };
}
