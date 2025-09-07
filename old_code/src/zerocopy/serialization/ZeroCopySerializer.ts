/**
 * ZeroCopySerializer - Cap'n Proto inspired serialization framework
 *
 * Provides efficient serialization and deserialization of zero-copy data structures
 * with cross-language compatibility and minimal memory overhead.
 */

import { MemoryArena, MemorySegment } from '../../memory/arena/MemoryArena';
import { StringInterner } from '../../memory/strings/StringInterner';
import { AlignedToken, AlignedTokenArray } from '../tokens/AlignedToken';
import { ZeroCopyASTNode, ZeroCopyAST } from '../ast/ZeroCopyASTNode';

// Add missing type definition
export class MemoryPointer {
    constructor(public segment: MemorySegment, public offset: number) {}
}

export interface SerializationHeader {
    magic: number;
    version: number;
    segmentCount: number;
    stringTableSize: number;
    rootOffset: number;
    flags: number;
}

export interface SerializationMetadata {
    timestamp: number;
    generator: string;
    sourceFile: string;
    checksum: number;
}

export class ZeroCopySerializer {
  private static readonly MAGIC_NUMBER = 0x47464D43; // 'GFMC' (Minotaur Memory Compact)
  private static readonly VERSION = 1;
  private static readonly HEADER_SIZE = 64; // 64 bytes for header + metadata

  /**
     * Serialize an AST to a binary format
     */
  static serializeAST(ast: ZeroCopyAST, metadata?: Partial<SerializationMetadata>): ArrayBuffer {
    const root = ast.getRoot();
    if (!root) {
      throw new Error('Cannot serialize empty AST');
    }

    // Get arena segments and string table
    const arena = (root as any).arena as MemoryArena;
    const stringInterner = (root as any).stringInterner as StringInterner;
    const segments = arena.getSegments();
    const stringTableData = stringInterner.serialize();

    // Calculate total size
    const headerSize = ZeroCopySerializer.HEADER_SIZE;
    const segmentIndexSize = segments.length * 8; // 8 bytes per segment (size + offset)
    const stringTableSize = stringTableData.byteLength;
    const segmentDataSize = segments.reduce((sum, seg) => sum + seg.offset, 0);
    const totalSize = headerSize + segmentIndexSize + stringTableSize + segmentDataSize;

    // Create output buffer
    const buffer = new ArrayBuffer(totalSize);
    const view = new DataView(buffer);
    let offset = 0;

    // Write header
    const header: SerializationHeader = {
      magic: ZeroCopySerializer.MAGIC_NUMBER,
      version: ZeroCopySerializer.VERSION,
      segmentCount: segments.length,
      stringTableSize: stringTableSize,
      rootOffset: root.memoryPointer.offset,
      flags: 0,
    };

    view.setUint32(offset, header.magic, true);
    offset += 4;
    view.setUint32(offset, header.version, true);
    offset += 4;
    view.setUint32(offset, header.segmentCount, true);
    offset += 4;
    view.setUint32(offset, header.stringTableSize, true);
    offset += 4;
    view.setUint32(offset, header.rootOffset, true);
    offset += 4;
    view.setUint32(offset, header.flags, true);
    offset += 4;

    // Write metadata
    const meta: SerializationMetadata = {
      timestamp: Date.now(),
      generator: 'Minotaur',
      sourceFile: '',
      checksum: 0,
      ...metadata,
    };

    view.setBigUint64(offset, BigInt(meta.timestamp), true);
    offset += 8;

    // Write generator string (16 bytes, null-padded)
    const generatorBytes = new TextEncoder().encode(meta.generator.substring(0, 15));
    const generatorArray = new Uint8Array(buffer, offset, 16);
    generatorArray.set(generatorBytes);
    offset += 16;

    // Write source file string (16 bytes, null-padded)
    const sourceBytes = new TextEncoder().encode(meta.sourceFile.substring(0, 15));
    const sourceArray = new Uint8Array(buffer, offset, 16);
    sourceArray.set(sourceBytes);
    offset += 16;

    view.setUint32(offset, meta.checksum, true);
    offset += 4;

    // Pad to header size
    while (offset < headerSize) {
      view.setUint8(offset++, 0);
    }

    // Write segment index
    let dataOffset = headerSize + segmentIndexSize + stringTableSize;
    for (const segment of segments) {
      view.setUint32(offset, segment.offset, true); // Used size
      offset += 4;
      view.setUint32(offset, dataOffset, true); // Offset in file
      offset += 4;
      dataOffset += segment.offset;
    }

    // Write string table
    const stringTableArray = new Uint8Array(buffer, offset, stringTableSize);
    stringTableArray.set(new Uint8Array(stringTableData));
    offset += stringTableSize;

    // Write segment data
    for (const segment of segments) {
      const segmentArray = new Uint8Array(buffer, offset, segment.offset);
      segmentArray.set(new Uint8Array(segment.data, 0, segment.offset));
      offset += segment.offset;
    }

    // Calculate and update checksum
    const checksum = ZeroCopySerializer.calculateChecksum(buffer, headerSize);
    view.setUint32(60, checksum, true); // Update checksum in header

    return buffer;
  }

  /**
     * Deserialize an AST from binary format
     */
  static deserializeAST(buffer: ArrayBuffer): { ast: ZeroCopyAST; metadata: SerializationMetadata } {
    const view = new DataView(buffer);
    let offset = 0;

    // Read and validate header
    const header: SerializationHeader = {
      magic: view.getUint32(offset, true),
      version: view.getUint32(offset + 4, true),
      segmentCount: view.getUint32(offset + 8, true),
      stringTableSize: view.getUint32(offset + 12, true),
      rootOffset: view.getUint32(offset + 16, true),
      flags: view.getUint32(offset + 20, true),
    };
    offset += 24;

    if (header.magic !== ZeroCopySerializer.MAGIC_NUMBER) {
      throw new Error(`Invalid magic number: expected ${ZeroCopySerializer.MAGIC_NUMBER}, got ${header.magic}`);
    }

    if (header.version !== ZeroCopySerializer.VERSION) {
      throw new Error(`Unsupported version: ${header.version}`);
    }

    // Read metadata
    const metadata: SerializationMetadata = {
      timestamp: Number(view.getBigUint64(offset, true)),
      generator: '',
      sourceFile: '',
      checksum: 0,
    };
    offset += 8;

    // Read generator string
    const generatorArray = new Uint8Array(buffer, offset, 16);
    const generatorEnd = generatorArray.indexOf(0);
    metadata.generator = new TextDecoder().decode(
      generatorArray.subarray(0, generatorEnd >= 0 ? generatorEnd : 16),
    );
    offset += 16;

    // Read source file string
    const sourceArray = new Uint8Array(buffer, offset, 16);
    const sourceEnd = sourceArray.indexOf(0);
    metadata.sourceFile = new TextDecoder().decode(
      sourceArray.subarray(0, sourceEnd >= 0 ? sourceEnd : 16),
    );
    offset += 16;

    metadata.checksum = view.getUint32(offset, true);
    offset += 4;

    // Skip to end of header
    offset = ZeroCopySerializer.HEADER_SIZE;

    // Verify checksum
    const calculatedChecksum = ZeroCopySerializer.calculateChecksum(buffer, ZeroCopySerializer.HEADER_SIZE);
    if (calculatedChecksum !== metadata.checksum) {
      throw new Error(`Checksum mismatch: expected ${metadata.checksum}, got ${calculatedChecksum}`);
    }

    // Read segment index
    const segmentInfo: Array<{ size: number; offset: number }> = [];
    for (let i = 0; i < header.segmentCount; i++) {
      segmentInfo.push({
        size: view.getUint32(offset, true),
        offset: view.getUint32(offset + 4, true),
      });
      offset += 8;
    }

    // Read string table
    const stringTableBuffer = buffer.slice(offset, offset + header.stringTableSize);
    offset += header.stringTableSize;

    // Create arena and load segments
    const arena = new MemoryArena(0); // Don't create initial segment
    const segmentBuffers: ArrayBuffer[] = [];

    for (const info of segmentInfo) {
      const segmentBuffer = buffer.slice(info.offset, info.offset + info.size);
      segmentBuffers.push(segmentBuffer);
    }

    const restoredArena = MemoryArena.fromSegments(segmentBuffers);

    // Restore string interner
    const stringInterner = StringInterner.deserialize(stringTableBuffer, restoredArena);

    // Create AST and restore root
    const ast = new ZeroCopyAST(restoredArena, stringInterner);

    // Find root node in first segment
    const firstSegment = restoredArena.getSegments()[0];
    const rootPointer = new (MemoryPointer as any)(firstSegment, header.rootOffset);
    const root = ZeroCopyASTNode.fromMemory(rootPointer, restoredArena, stringInterner);

    (ast as any).root = root;

    return { ast, metadata };
  }

  /**
     * Serialize a token array to binary format
     */
  static serializeTokens(tokens: AlignedTokenArray, stringInterner: StringInterner): ArrayBuffer {
    const stats = tokens.getStatistics();
    const stringTableData = stringInterner.serialize();

    // Calculate total size
    const headerSize = 32; // Simplified header for tokens
    const stringTableSize = stringTableData.byteLength;
    const tokenDataSize = stats.memoryUsed;
    const totalSize = headerSize + stringTableSize + tokenDataSize;

    const buffer = new ArrayBuffer(totalSize);
    const view = new DataView(buffer);
    let offset = 0;

    // Write header
    view.setUint32(offset, ZeroCopySerializer.MAGIC_NUMBER, true);
    offset += 4;
    view.setUint32(offset, ZeroCopySerializer.VERSION, true);
    offset += 4;
    view.setUint32(offset, stats.used, true); // Token count
    offset += 4;
    view.setUint32(offset, stringTableSize, true);
    offset += 4;
    view.setUint32(offset, tokenDataSize, true);
    offset += 4;

    // Pad header
    while (offset < headerSize) {
      view.setUint32(offset, 0, true);
      offset += 4;
    }

    // Write string table
    const stringTableArray = new Uint8Array(buffer, offset, stringTableSize);
    stringTableArray.set(new Uint8Array(stringTableData));
    offset += stringTableSize;

    // Write token data
    const tokenArray = new Uint8Array(buffer, offset, tokenDataSize);
    for (let i = 0; i < stats.used; i++) {
      const token = tokens.get(i);
      const tokenData = new Uint8Array(
        token.memoryPointer.segment.data,
        token.memoryPointer.offset,
        AlignedToken.getByteSize(),
      );
      tokenArray.set(tokenData, i * AlignedToken.getByteSize());
    }

    return buffer;
  }

  /**
     * Deserialize tokens from binary format
     */
  static deserializeTokens(buffer: ArrayBuffer): { tokens: AlignedTokenArray; stringInterner: StringInterner } {
    const view = new DataView(buffer);
    let offset = 0;

    // Read header
    const magic = view.getUint32(offset, true);
    if (magic !== ZeroCopySerializer.MAGIC_NUMBER) {
      throw new Error(`Invalid magic number: ${magic}`);
    }
    offset += 4;

    const version = view.getUint32(offset, true);
    if (version !== ZeroCopySerializer.VERSION) {
      throw new Error(`Unsupported version: ${version}`);
    }
    offset += 4;

    const tokenCount = view.getUint32(offset, true);
    offset += 4;

    const stringTableSize = view.getUint32(offset, true);
    offset += 4;

    const tokenDataSize = view.getUint32(offset, true);
    offset += 4;

    // Skip to end of header
    offset = 32;

    // Read string table
    const stringTableBuffer = buffer.slice(offset, offset + stringTableSize);
    offset += stringTableSize;

    // Create arena and string interner
    const arena = new MemoryArena();
    const stringInterner = StringInterner.deserialize(stringTableBuffer, arena);

    // Create token array
    const tokens = new AlignedTokenArray(arena, stringInterner, tokenCount);

    // Read token data
    const tokenDataArray = new Uint8Array(buffer, offset, tokenDataSize);
    for (let i = 0; i < tokenCount; i++) {
      const tokenOffset = i * AlignedToken.getByteSize();
      const tokenData = tokenDataArray.slice(tokenOffset, tokenOffset + AlignedToken.getByteSize());

      // Allocate memory for token and copy data
      const tokenPointer = arena.allocate(AlignedToken.getByteSize(), AlignedToken.getAlignment());
      const targetArray = new Uint8Array(
        tokenPointer.segment.data,
        tokenPointer.offset,
        AlignedToken.getByteSize(),
      );
      targetArray.set(tokenData);

      // Token is automatically added to array through memory layout
    }

    return { tokens, stringInterner };
  }

  /**
     * Calculate CRC32 checksum
     */
  private static calculateChecksum(buffer: ArrayBuffer, startOffset: number = 0): number {
    const data = new Uint8Array(buffer, startOffset);
    let crc = 0xFFFFFFFF;

    for (let i = 0; i < data.length; i++) {
      crc ^= data[i];
      for (let j = 0; j < 8; j++) {
        crc = (crc >>> 1) ^ (0xEDB88320 & (-(crc & 1)));
      }
    }

    return (crc ^ 0xFFFFFFFF) >>> 0;
  }

  /**
     * Create a cross-language compatible binary format
     */
  static createCrossLanguageFormat(
    ast: ZeroCopyAST,
    targetLanguage: string,
    metadata?: Partial<SerializationMetadata>,
  ): ArrayBuffer {
    // Add language-specific metadata
    const languageMetadata = {
      ...metadata,
      generator: `Minotaur-${targetLanguage}`,
      flags: ZeroCopySerializer.getLanguageFlags(targetLanguage),
    };

    return ZeroCopySerializer.serializeAST(ast, languageMetadata);
  }

  /**
     * Get language-specific flags for cross-language compatibility
     */
  private static getLanguageFlags(language: string): number {
    const flags = {
      'typescript': 0x01,
      'javascript': 0x02,
      'go': 0x04,
      'rust': 0x08,
      'cpp': 0x10,
      'java': 0x20,
      'python': 0x40,
      'csharp': 0x80,
      'dart': 0x100,
      'wasm': 0x200,
    };

    return flags[language.toLowerCase()] || 0;
  }

  /**
     * Validate serialized data integrity
     */
  static validateIntegrity(buffer: ArrayBuffer): boolean {
    try {
      const view = new DataView(buffer);

      // Check magic number
      const magic = view.getUint32(0, true);
      if (magic !== ZeroCopySerializer.MAGIC_NUMBER) {
        return false;
      }

      // Check version
      const version = view.getUint32(4, true);
      if (version !== ZeroCopySerializer.VERSION) {
        return false;
      }

      // Verify checksum
      const storedChecksum = view.getUint32(60, true);
      const calculatedChecksum = ZeroCopySerializer.calculateChecksum(buffer, ZeroCopySerializer.HEADER_SIZE);

      return storedChecksum === calculatedChecksum;
    } catch (error) {
      return false;
    }
  }

  /**
     * Get serialization format information
     */
  static getFormatInfo(buffer: ArrayBuffer): SerializationHeader & SerializationMetadata {
    const view = new DataView(buffer);

    const header: SerializationHeader = {
      magic: view.getUint32(0, true),
      version: view.getUint32(4, true),
      segmentCount: view.getUint32(8, true),
      stringTableSize: view.getUint32(12, true),
      rootOffset: view.getUint32(16, true),
      flags: view.getUint32(20, true),
    };

    const metadata: SerializationMetadata = {
      timestamp: Number(view.getBigUint64(24, true)),
      generator: '',
      sourceFile: '',
      checksum: view.getUint32(60, true),
    };

    // Read generator string
    const generatorArray = new Uint8Array(buffer, 32, 16);
    const generatorEnd = generatorArray.indexOf(0);
    metadata.generator = new TextDecoder().decode(
      generatorArray.subarray(0, generatorEnd >= 0 ? generatorEnd : 16),
    );

    // Read source file string
    const sourceArray = new Uint8Array(buffer, 48, 16);
    const sourceEnd = sourceArray.indexOf(0);
    metadata.sourceFile = new TextDecoder().decode(
      sourceArray.subarray(0, sourceEnd >= 0 ? sourceEnd : 16),
    );

    return { ...header, ...metadata };
  }
}

/**
 * Streaming serializer for large datasets
 */
export class StreamingZeroCopySerializer {
  private buffer: ArrayBuffer;
  private view: DataView;
  private offset: number = 0;

  constructor(initialSize: number = 1024 * 1024) { // 1MB default
    this.buffer = new ArrayBuffer(initialSize);
    this.view = new DataView(this.buffer);
  }

  /**
     * Write data to the stream
     */
  write(data: ArrayBuffer): void {
    this.ensureCapacity(data.byteLength);

    const sourceArray = new Uint8Array(data);
    const targetArray = new Uint8Array(this.buffer, this.offset, data.byteLength);
    targetArray.set(sourceArray);

    this.offset += data.byteLength;
  }

  /**
     * Get the final serialized data
     */
  finalize(): ArrayBuffer {
    return this.buffer.slice(0, this.offset);
  }

  /**
     * Ensure buffer has enough capacity
     */
  private ensureCapacity(additionalBytes: number): void {
    const requiredSize = this.offset + additionalBytes;

    if (requiredSize > this.buffer.byteLength) {
      const newSize = Math.max(requiredSize, this.buffer.byteLength * 2);
      const newBuffer = new ArrayBuffer(newSize);

      // Copy existing data
      const oldArray = new Uint8Array(this.buffer, 0, this.offset);
      const newArray = new Uint8Array(newBuffer);
      newArray.set(oldArray);

      this.buffer = newBuffer;
      this.view = new DataView(this.buffer);
    }
  }
}

