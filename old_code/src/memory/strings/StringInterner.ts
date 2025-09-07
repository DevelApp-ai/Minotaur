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
 * StringInterner - Efficient string deduplication and storage system
 *
 * Provides string interning with zero-copy access and cross-language compatibility.
 * Inspired by Cap'n Proto's string handling but optimized for parser use cases.
 */

import { MemoryArena, MemoryPointer } from '../arena/MemoryArena';

export interface StringTableEntry {
    id: number;
    length: number;
    offset: number;
    hash: number;
}

export class StringInterner {
  private arena: MemoryArena;
  private stringMap: Map<string, number> = new Map();
  private stringTable: StringTableEntry[] = [];
  private stringData: MemoryPointer[] = [];
  private nextStringId: number = 1; // 0 reserved for null/empty

  // String table header in memory
  private tableHeader: MemoryPointer | null = null;

  // Statistics
  private totalStrings: number = 0;
  private totalBytes: number = 0;
  private duplicatesAvoided: number = 0;

  constructor(arena: MemoryArena) {
    this.arena = arena;
    this.initializeStringTable();
  }

  /**
     * Intern a string, returning its unique ID
     */
  intern(value: string): number {
    if (value === '') {
      return 0; // Empty string always has ID 0
    }

    // Check if string is already interned
    const existingId = this.stringMap.get(value);
    if (existingId !== undefined) {
      this.duplicatesAvoided++;
      return existingId;
    }

    // Intern new string
    const stringId = this.nextStringId++;
    const hash = this.hashString(value);
    const encodedBytes = new TextEncoder().encode(value);

    // Allocate memory for string data (with null terminator for C compatibility)
    const stringPointer = this.arena.allocate(encodedBytes.length + 1, 4);

    // Write string data
    const stringArray = new Uint8Array(
      stringPointer.segment.data,
      stringPointer.offset,
      encodedBytes.length + 1,
    );
    stringArray.set(encodedBytes);
    stringArray[encodedBytes.length] = 0; // Null terminator

    // Create string table entry
    const entry: StringTableEntry = {
      id: stringId,
      length: encodedBytes.length,
      offset: stringPointer.offset,
      hash: hash,
    };

    // Store in data structures
    this.stringTable.push(entry);
    this.stringData.push(stringPointer);
    this.stringMap.set(value, stringId);

    // Update statistics
    this.totalStrings++;
    this.totalBytes += encodedBytes.length;

    return stringId;
  }

  /**
     * Get string by ID
     */
  getString(id: number): string {
    if (id === 0) {
      return '';
    }

    if (id < 1 || id >= this.nextStringId) {
      throw new Error(`Invalid string ID: ${id}`);
    }

    const entry = this.stringTable[id - 1];
    const pointer = this.stringData[id - 1];

    // Read string data
    const stringArray = new Uint8Array(
      pointer.segment.data,
      pointer.offset,
      entry.length,
    );

    return new TextDecoder().decode(stringArray);
  }

  /**
     * Get string pointer for zero-copy access
     */
  getStringPointer(id: number): MemoryPointer | null {
    if (id === 0) {
      return null; // Empty string
    }

    if (id < 1 || id >= this.nextStringId) {
      throw new Error(`Invalid string ID: ${id}`);
    }

    return this.stringData[id - 1];
  }

  /**
     * Get string table entry
     */
  getStringEntry(id: number): StringTableEntry | null {
    if (id === 0) {
      return null; // Empty string
    }

    if (id < 1 || id >= this.nextStringId) {
      throw new Error(`Invalid string ID: ${id}`);
    }

    return this.stringTable[id - 1];
  }

  /**
     * Check if a string is already interned
     */
  isInterned(value: string): boolean {
    return this.stringMap.has(value);
  }

  /**
     * Get the ID of an interned string (without interning it)
     */
  getStringId(value: string): number | undefined {
    return this.stringMap.get(value);
  }

  /**
     * Get string interner statistics
     */
  getStatistics(): StringInternerStatistics {
    const uniqueStrings = this.stringMap.size;
    const totalRequests = this.totalStrings + this.duplicatesAvoided;
    const deduplicationRatio = totalRequests > 0 ? this.duplicatesAvoided / totalRequests : 0;
    const averageStringLength = this.totalStrings > 0 ? this.totalBytes / this.totalStrings : 0;

    return {
      uniqueStrings,
      totalStrings: this.totalStrings,
      totalBytes: this.totalBytes,
      duplicatesAvoided: this.duplicatesAvoided,
      deduplicationRatio,
      averageStringLength,
      memoryEfficiency: this.calculateMemoryEfficiency(),
    };
  }

  /**
     * Serialize string table to binary format
     */
  serialize(): ArrayBuffer {
    // Calculate total size needed
    const headerSize = 16; // Magic + version + count + total size
    const indexSize = this.stringTable.length * 16; // 16 bytes per entry
    const dataSize = this.totalBytes + this.stringTable.length; // Include null terminators
    const totalSize = headerSize + indexSize + dataSize;

    const buffer = new ArrayBuffer(totalSize);
    const view = new DataView(buffer);
    let offset = 0;

    // Write header
    view.setUint32(offset, 0x53545254, true); // 'STRT' magic
    offset += 4;
    view.setUint32(offset, 1, true); // Version
    offset += 4;
    view.setUint32(offset, this.stringTable.length, true); // Count
    offset += 4;
    view.setUint32(offset, dataSize, true); // Data size
    offset += 4;

    // Write string table index
    let dataOffset = headerSize + indexSize;
    for (const entry of this.stringTable) {
      view.setUint32(offset, entry.id, true);
      offset += 4;
      view.setUint32(offset, entry.length, true);
      offset += 4;
      view.setUint32(offset, dataOffset, true); // Offset in serialized data
      offset += 4;
      view.setUint32(offset, entry.hash, true);
      offset += 4;

      dataOffset += entry.length + 1; // Include null terminator
    }

    // Write string data
    const dataArray = new Uint8Array(buffer, headerSize + indexSize);
    let dataIndex = 0;

    for (let i = 0; i < this.stringTable.length; i++) {
      const entry = this.stringTable[i];
      const pointer = this.stringData[i];

      // Copy string data including null terminator
      const sourceArray = new Uint8Array(
        pointer.segment.data,
        pointer.offset,
        entry.length + 1,
      );
      dataArray.set(sourceArray, dataIndex);
      dataIndex += entry.length + 1;
    }

    return buffer;
  }

  /**
     * Deserialize string table from binary format
     */
  static deserialize(buffer: ArrayBuffer, arena: MemoryArena): StringInterner {
    const view = new DataView(buffer);
    let offset = 0;

    // Read header
    const magic = view.getUint32(offset, true);
    if (magic !== 0x53545254) {
      throw new Error('Invalid string table magic number');
    }
    offset += 4;

    const version = view.getUint32(offset, true);
    if (version !== 1) {
      throw new Error(`Unsupported string table version: ${version}`);
    }
    offset += 4;

    const count = view.getUint32(offset, true);
    offset += 4;

    const dataSize = view.getUint32(offset, true);
    offset += 4;

    // Create new interner
    const interner = new StringInterner(arena);
    interner.stringTable = [];
    interner.stringData = [];
    interner.stringMap.clear();
    interner.nextStringId = 1;

    // Read string table entries
    const indexOffset = offset;
    const dataOffset = indexOffset + (count * 16);

    for (let i = 0; i < count; i++) {
      const entryOffset = indexOffset + (i * 16);
      const id = view.getUint32(entryOffset, true);
      const length = view.getUint32(entryOffset + 4, true);
      const stringDataOffset = view.getUint32(entryOffset + 8, true);
      const hash = view.getUint32(entryOffset + 12, true);

      // Read string data
      const stringArray = new Uint8Array(buffer, dataOffset + stringDataOffset, length);
      const value = new TextDecoder().decode(stringArray);

      // Allocate memory in arena and copy string
      const stringPointer = arena.allocate(length + 1, 4);
      const targetArray = new Uint8Array(
        stringPointer.segment.data,
        stringPointer.offset,
        length + 1,
      );
      targetArray.set(stringArray);
      targetArray[length] = 0; // Null terminator

      // Create entry
      const entry: StringTableEntry = { id, length, offset: stringPointer.offset, hash };

      // Store in data structures
      interner.stringTable.push(entry);
      interner.stringData.push(stringPointer);
      interner.stringMap.set(value, id);
      interner.nextStringId = Math.max(interner.nextStringId, id + 1);
    }

    // Update statistics
    interner.totalStrings = count;
    interner.totalBytes = interner.stringTable.reduce((sum, entry) => sum + entry.length, 0);

    return interner;
  }

  private initializeStringTable(): void {
    // Reserve space for string table header
    this.tableHeader = this.arena.allocate(32, 8); // 32 bytes aligned to 8-byte boundary

    // Initialize header with magic number and version
    const view = this.tableHeader.segment.view;
    view.setUint32(this.tableHeader.offset, 0x53545254, true); // 'STRT' magic
    view.setUint32(this.tableHeader.offset + 4, 1, true); // Version
  }

  private hashString(value: string): number {
    // Simple FNV-1a hash
    let hash = 2166136261;
    for (let i = 0; i < value.length; i++) {
      hash ^= value.charCodeAt(i);
      hash = Math.imul(hash, 16777619);
    }
    return hash >>> 0; // Convert to unsigned 32-bit
  }

  private calculateMemoryEfficiency(): number {
    // Calculate memory saved through deduplication
    const totalRequestedBytes = (this.totalStrings + this.duplicatesAvoided) *
            (this.totalStrings > 0 ? this.totalBytes / this.totalStrings : 0);
    const actualBytes = this.totalBytes;

    return totalRequestedBytes > 0 ? (totalRequestedBytes - actualBytes) / totalRequestedBytes : 0;
  }
}

export interface StringInternerStatistics {
    uniqueStrings: number;
    totalStrings: number;
    totalBytes: number;
    duplicatesAvoided: number;
    deduplicationRatio: number;
    averageStringLength: number;
    memoryEfficiency: number;
}
