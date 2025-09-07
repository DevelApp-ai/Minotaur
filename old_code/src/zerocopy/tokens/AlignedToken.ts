/**
 * AlignedToken - Zero-copy token representation with memory alignment
 *
 * Provides efficient token storage and access using Cap'n Proto inspired
 * memory layout for optimal cache performance and zero-copy serialization.
 */

import { MemoryArena, MemoryPointer } from '../../memory/arena/MemoryArena';
import { StringInterner } from '../../memory/strings/StringInterner';
import { PointerUtils, MemorySerializer } from '../../memory/pointers/PointerUtils';

export enum TokenType {
    EOF = 0,
    IDENTIFIER = 1,
    NUMBER = 2,
    STRING = 3,
    OPERATOR = 4,
    KEYWORD = 5,
    WHITESPACE = 6,
    COMMENT = 7,
    PUNCTUATION = 8,
    NEWLINE = 9,
    ERROR = 10
}

export interface TokenPosition {
    line: number;
    column: number;
    offset: number;
}

export interface TokenSpan {
    start: TokenPosition;
    end: TokenPosition;
}

/**
 * Memory layout for AlignedToken (32 bytes, cache-line friendly):
 *
 * Offset | Size | Field
 * -------|------|-------
 * 0      | 4    | type (TokenType)
 * 4      | 4    | valueId (string interner ID)
 * 8      | 4    | startLine
 * 12     | 4    | startColumn
 * 16     | 4    | startOffset
 * 20     | 4    | endLine
 * 24     | 4    | endColumn
 * 28     | 4    | endOffset
 */
export class AlignedToken {
  private static readonly BYTE_SIZE = 32;
  private static readonly ALIGNMENT = 8;

  private pointer: MemoryPointer;
  private stringInterner: StringInterner;

  constructor(
    pointer: MemoryPointer,
    stringInterner: StringInterner,
    type: TokenType = TokenType.EOF,
    value: string = '',
    span?: TokenSpan,
  ) {
    this.pointer = pointer;
    this.stringInterner = stringInterner;

    if (type !== TokenType.EOF || value !== '' || span) {
      this.initialize(type, value, span);
    }
  }

  /**
     * Create a new AlignedToken in the arena
     */
  static create(
    arena: MemoryArena,
    stringInterner: StringInterner,
    type: TokenType,
    value: string,
    span?: TokenSpan,
  ): AlignedToken {
    const pointer = arena.allocate(AlignedToken.BYTE_SIZE, AlignedToken.ALIGNMENT);
    return new AlignedToken(pointer, stringInterner, type, value, span);
  }

  /**
     * Create an AlignedToken from existing memory
     */
  static fromMemory(
    pointer: MemoryPointer,
    stringInterner: StringInterner,
  ): AlignedToken {
    return new AlignedToken(pointer, stringInterner);
  }

  /**
     * Get the memory size required for a token
     */
  static getByteSize(): number {
    return AlignedToken.BYTE_SIZE;
  }

  /**
     * Get the memory alignment requirement
     */
  static getAlignment(): number {
    return AlignedToken.ALIGNMENT;
  }

  /**
     * Initialize token data
     */
  private initialize(type: TokenType, value: string, span?: TokenSpan): void {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    // Intern the string value
    const valueId = this.stringInterner.intern(value);

    // Write token data
    view.setUint32(offset, type, true);
    view.setUint32(offset + 4, valueId, true);

    if (span) {
      view.setUint32(offset + 8, span.start.line, true);
      view.setUint32(offset + 12, span.start.column, true);
      view.setUint32(offset + 16, span.start.offset, true);
      view.setUint32(offset + 20, span.end.line, true);
      view.setUint32(offset + 24, span.end.column, true);
      view.setUint32(offset + 28, span.end.offset, true);
    } else {
      // Zero out position data
      for (let i = 8; i < 32; i += 4) {
        view.setUint32(offset + i, 0, true);
      }
    }
  }

  /**
     * Get token type
     */
  get type(): TokenType {
    return this.pointer.segment.view.getUint32(this.pointer.offset, true) as TokenType;
  }

  /**
     * Set token type
     */
  set type(value: TokenType) {
    this.pointer.segment.view.setUint32(this.pointer.offset, value, true);
  }

  /**
     * Get token value string
     */
  get value(): string {
    const valueId = this.pointer.segment.view.getUint32(this.pointer.offset + 4, true);
    return this.stringInterner.getString(valueId);
  }

  /**
     * Set token value string
     */
  set value(str: string) {
    const valueId = this.stringInterner.intern(str);
    this.pointer.segment.view.setUint32(this.pointer.offset + 4, valueId, true);
  }

  /**
     * Get token value ID (for zero-copy operations)
     */
  get valueId(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 4, true);
  }

  /**
     * Get token span
     */
  get span(): TokenSpan {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    return {
      start: {
        line: view.getUint32(offset + 8, true),
        column: view.getUint32(offset + 12, true),
        offset: view.getUint32(offset + 16, true),
      },
      end: {
        line: view.getUint32(offset + 20, true),
        column: view.getUint32(offset + 24, true),
        offset: view.getUint32(offset + 28, true),
      },
    };
  }

  /**
     * Set token span
     */
  set span(value: TokenSpan) {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    view.setUint32(offset + 8, value.start.line, true);
    view.setUint32(offset + 12, value.start.column, true);
    view.setUint32(offset + 16, value.start.offset, true);
    view.setUint32(offset + 20, value.end.line, true);
    view.setUint32(offset + 24, value.end.column, true);
    view.setUint32(offset + 28, value.end.offset, true);
  }

  /**
     * Get start position
     */
  get startPosition(): TokenPosition {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    return {
      line: view.getUint32(offset + 8, true),
      column: view.getUint32(offset + 12, true),
      offset: view.getUint32(offset + 16, true),
    };
  }

  /**
     * Get end position
     */
  get endPosition(): TokenPosition {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    return {
      line: view.getUint32(offset + 20, true),
      column: view.getUint32(offset + 24, true),
      offset: view.getUint32(offset + 28, true),
    };
  }

  /**
     * Get the memory pointer for this token
     */
  get memoryPointer(): MemoryPointer {
    return this.pointer;
  }

  /**
     * Check if this is an EOF token
     */
  isEOF(): boolean {
    return this.type === TokenType.EOF;
  }

  /**
     * Check if this is an error token
     */
  isError(): boolean {
    return this.type === TokenType.ERROR;
  }

  /**
     * Check if this token has a specific value
     */
  hasValue(value: string): boolean {
    // Fast path: compare string IDs if the string is already interned
    const valueId = this.stringInterner.getStringId(value);
    if (valueId !== undefined) {
      return this.valueId === valueId;
    }

    // Slow path: compare actual strings
    return this.value === value;
  }

  /**
     * Check if this token is of a specific type
     */
  isType(type: TokenType): boolean {
    return this.type === type;
  }

  /**
     * Check if this token is whitespace or comment
     */
  isTrivia(): boolean {
    return this.type === TokenType.WHITESPACE ||
               this.type === TokenType.COMMENT ||
               this.type === TokenType.NEWLINE;
  }

  /**
     * Get the length of the token in characters
     */
  get length(): number {
    const span = this.span;
    return span.end.offset - span.start.offset;
  }

  /**
     * Clone this token to a new memory location
     */
  clone(arena: MemoryArena): AlignedToken {
    const newPointer = arena.allocate(AlignedToken.BYTE_SIZE, AlignedToken.ALIGNMENT);

    // Copy memory directly
    PointerUtils.copyMemory(this.pointer, newPointer, AlignedToken.BYTE_SIZE);

    return new AlignedToken(newPointer, this.stringInterner);
  }

  /**
     * Serialize token to a plain object
     */
  toPlainObject(): PlainToken {
    return {
      type: this.type,
      value: this.value,
      span: this.span,
    };
  }

  /**
     * Create token from a plain object
     */
  static fromPlainObject(
    arena: MemoryArena,
    stringInterner: StringInterner,
    plain: PlainToken,
  ): AlignedToken {
    return AlignedToken.create(arena, stringInterner, plain.type, plain.value, plain.span);
  }

  /**
     * Get a string representation of the token
     */
  toString(): string {
    const typeStr = TokenType[this.type] || `Unknown(${this.type})`;
    const span = this.span;
    return `${typeStr}("${this.value}") at ${span.start.line}:${span.start.column}-${span.end.line}:${span.end.column}`;
  }

  /**
     * Compare two tokens for equality
     */
  equals(other: AlignedToken): boolean {
    if (this.type !== other.type) {
      return false;
    }
    if (this.valueId !== other.valueId) {
      return false;
    }

    const thisSpan = this.span;
    const otherSpan = other.span;

    return thisSpan.start.line === otherSpan.start.line &&
               thisSpan.start.column === otherSpan.start.column &&
               thisSpan.start.offset === otherSpan.start.offset &&
               thisSpan.end.line === otherSpan.end.line &&
               thisSpan.end.column === otherSpan.end.column &&
               thisSpan.end.offset === otherSpan.end.offset;
  }
}

/**
 * Plain object representation for serialization
 */
export interface PlainToken {
    type: TokenType;
    value: string;
    span: TokenSpan;
}

/**
 * Token array for efficient batch operations
 */
export class AlignedTokenArray {
  private arena: MemoryArena;
  private stringInterner: StringInterner;
  private tokens: MemoryPointer[] = [];
  private count: number = 0;

  constructor(arena: MemoryArena, stringInterner: StringInterner, initialCapacity: number = 1024) {
    this.arena = arena;
    this.stringInterner = stringInterner;

    // Pre-allocate token pointers for better performance
    this.tokens = arena.allocateBatch(
      AlignedToken.getByteSize(),
      initialCapacity,
      AlignedToken.getAlignment(),
    );
  }

  /**
     * Add a token to the array
     */
  push(type: TokenType, value: string, span?: TokenSpan): AlignedToken {
    if (this.count >= this.tokens.length) {
      // Grow the array
      const newTokens = this.arena.allocateBatch(
        AlignedToken.getByteSize(),
        this.tokens.length,
        AlignedToken.getAlignment(),
      );
      this.tokens.push(...newTokens);
    }

    const token = new AlignedToken(
      this.tokens[this.count],
      this.stringInterner,
      type,
      value,
      span,
    );

    this.count++;
    return token;
  }

  /**
     * Get a token by index
     */
  get(index: number): AlignedToken {
    if (index < 0 || index >= this.count) {
      throw new Error(`Token index out of bounds: ${index}`);
    }

    return new AlignedToken(this.tokens[index], this.stringInterner);
  }

  /**
     * Get the number of tokens
     */
  get length(): number {
    return this.count;
  }

  /**
     * Iterate over all tokens
     */
  *[Symbol.iterator](): Iterator<AlignedToken> {
    for (let i = 0; i < this.count; i++) {
      yield this.get(i);
    }
  }

  /**
     * Find the first token matching a predicate
     */
  find(predicate: (token: AlignedToken) => boolean): AlignedToken | undefined {
    for (let i = 0; i < this.count; i++) {
      const token = this.get(i);
      if (predicate(token)) {
        return token;
      }
    }
    return undefined;
  }

  /**
     * Filter tokens by a predicate
     */
  filter(predicate: (token: AlignedToken) => boolean): AlignedToken[] {
    const result: AlignedToken[] = [];
    for (let i = 0; i < this.count; i++) {
      const token = this.get(i);
      if (predicate(token)) {
        result.push(token);
      }
    }
    return result;
  }

  /**
     * Get all non-trivia tokens
     */
  getNonTriviaTokens(): AlignedToken[] {
    return this.filter(token => !token.isTrivia());
  }

  /**
     * Clear all tokens
     */
  clear(): void {
    this.count = 0;
  }

  /**
     * Get memory usage statistics
     */
  getStatistics(): TokenArrayStatistics {
    const capacity = this.tokens.length;
    const used = this.count;
    const memoryUsed = used * AlignedToken.getByteSize();
    const memoryCapacity = capacity * AlignedToken.getByteSize();

    return {
      capacity,
      used,
      memoryUsed,
      memoryCapacity,
      utilizationRatio: capacity > 0 ? used / capacity : 0,
      averageTokenSize: AlignedToken.getByteSize(),
    };
  }
}

export interface TokenArrayStatistics {
    capacity: number;
    used: number;
    memoryUsed: number;
    memoryCapacity: number;
    utilizationRatio: number;
    averageTokenSize: number;
}

/**
 * Memory serializer for AlignedToken
 */
export class AlignedTokenSerializer implements MemorySerializer<AlignedToken> {
  constructor(private stringInterner: StringInterner) {}

  serialize(token: AlignedToken, pointer: MemoryPointer): void {
    // Copy token data directly
    PointerUtils.copyMemory(token.memoryPointer, pointer, AlignedToken.getByteSize());
  }

  deserialize(pointer: MemoryPointer): AlignedToken {
    return AlignedToken.fromMemory(pointer, this.stringInterner);
  }

  getSize(): number {
    return AlignedToken.getByteSize();
  }
}

