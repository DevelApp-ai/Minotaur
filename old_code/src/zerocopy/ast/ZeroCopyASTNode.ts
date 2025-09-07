/**
 * ZeroCopyASTNode - Zero-copy Abstract Syntax Tree node representation
 *
 * Provides efficient AST storage and traversal using Cap'n Proto inspired
 * memory layout with pointer-based child relationships for zero-copy operations.
 */

import { MemoryArena, MemoryPointer } from '../../memory/arena/MemoryArena';
import { StringInterner } from '../../memory/strings/StringInterner';
import { PointerUtils, ListPointer as _ListPointer } from '../../memory/pointers/PointerUtils';
import { AlignedToken as _AlignedToken, TokenSpan } from '../tokens/AlignedToken';

export enum ASTNodeType {
    PROGRAM = 0,
    STATEMENT = 1,
    EXPRESSION = 2,
    DECLARATION = 3,
    IDENTIFIER = 4,
    LITERAL = 5,
    BINARY_OP = 6,
    UNARY_OP = 7,
    FUNCTION_CALL = 8,
    BLOCK = 9,
    IF_STATEMENT = 10,
    WHILE_LOOP = 11,
    FOR_LOOP = 12,
    RETURN_STATEMENT = 13,
    ASSIGNMENT = 14,
    MEMBER_ACCESS = 15,
    ARRAY_ACCESS = 16,
    FUNCTION_DECLARATION = 17,
    VARIABLE_DECLARATION = 18,
    CLASS_DECLARATION = 19,
    IMPORT_STATEMENT = 20,
    ERROR = 255
}

export interface ASTNodeMetadata {
    nodeId: number;
    parentId: number;
    depth: number;
    flags: number;
}

/**
 * Memory layout for ZeroCopyASTNode header (72 bytes, cache-line aligned):
 *
 * Offset | Size | Field
 * -------|------|-------
 * 0      | 4    | nodeType (ASTNodeType)
 * 4      | 4    | nodeId
 * 8      | 4    | parentId
 * 12     | 4    | depth
 * 16     | 4    | flags
 * 20     | 4    | nameId (string interner ID)
 * 24     | 4    | valueId (string interner ID)
 * 28     | 4    | childCount
 * 32     | 8    | childrenPointer (encoded pointer)
 * 40     | 8    | attributesPointer (encoded pointer)
 * 48     | 24   | span (TokenSpan as 6 x uint32: start.line, start.column, start.offset,
 *        |      |       end.line, end.column, end.offset)
 */
export class ZeroCopyASTNode {
  private static readonly HEADER_SIZE = 72;
  private static readonly ALIGNMENT = 8;

  private pointer: MemoryPointer;
  private arena: MemoryArena;
  private stringInterner: StringInterner;

  constructor(
    pointer: MemoryPointer,
    arena: MemoryArena,
    stringInterner: StringInterner,
    nodeType?: ASTNodeType,
    name?: string,
    value?: string,
    span?: TokenSpan,
  ) {
    this.pointer = pointer;
    this.arena = arena;
    this.stringInterner = stringInterner;

    if (nodeType !== undefined) {
      this.initialize(nodeType, name, value, span);
    }
  }

  /**
     * Create a new ZeroCopyASTNode in the arena
     */
  static create(
    arena: MemoryArena,
    stringInterner: StringInterner,
    nodeType: ASTNodeType,
    name?: string,
    value?: string,
    span?: TokenSpan,
  ): ZeroCopyASTNode {
    const pointer = arena.allocate(ZeroCopyASTNode.HEADER_SIZE, ZeroCopyASTNode.ALIGNMENT);
    return new ZeroCopyASTNode(pointer, arena, stringInterner, nodeType, name, value, span);
  }

  /**
     * Create a ZeroCopyASTNode from existing memory
     */
  static fromMemory(
    pointer: MemoryPointer,
    arena: MemoryArena,
    stringInterner: StringInterner,
  ): ZeroCopyASTNode {
    return new ZeroCopyASTNode(pointer, arena, stringInterner);
  }

  /**
     * Get the memory size required for the node header
     */
  static getHeaderSize(): number {
    return ZeroCopyASTNode.HEADER_SIZE;
  }

  /**
     * Get the memory alignment requirement
     */
  static getAlignment(): number {
    return ZeroCopyASTNode.ALIGNMENT;
  }

  /**
     * Initialize node data
     */
  private initialize(
    nodeType: ASTNodeType,
    name?: string,
    value?: string,
    span?: TokenSpan,
  ): void {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    // Intern strings
    const nameId = name ? this.stringInterner.intern(name) : 0;
    const valueId = value ? this.stringInterner.intern(value) : 0;

    // Write header data
    view.setUint32(offset, nodeType, true);
    view.setUint32(offset + 4, 0, true); // nodeId (set by parent)
    view.setUint32(offset + 8, 0, true); // parentId (set by parent)
    view.setUint32(offset + 12, 0, true); // depth (set by parent)
    view.setUint32(offset + 16, 0, true); // flags
    view.setUint32(offset + 20, nameId, true);
    view.setUint32(offset + 24, valueId, true);
    view.setUint32(offset + 28, 0, true); // childCount
    view.setBigUint64(offset + 32, BigInt(0), true); // childrenPointer
    view.setBigUint64(offset + 40, BigInt(0), true); // attributesPointer

    // Write span data
    if (span) {
      view.setUint32(offset + 48, span.start.line, true);
      view.setUint32(offset + 52, span.start.column, true);
      view.setUint32(offset + 56, span.start.offset, true);
      view.setUint32(offset + 60, span.end.line, true);
      view.setUint32(offset + 64, span.end.column, true);
      view.setUint32(offset + 68, span.end.offset, true);
    } else {
      // Zero out span data (24 bytes total)
      for (let i = 48; i < 72; i += 4) {
        view.setUint32(offset + i, 0, true);
      }
    }
  }

  /**
     * Get node type
     */
  get nodeType(): ASTNodeType {
    return this.pointer.segment.view.getUint32(this.pointer.offset, true) as ASTNodeType;
  }

  /**
     * Set node type
     */
  set nodeType(value: ASTNodeType) {
    this.pointer.segment.view.setUint32(this.pointer.offset, value, true);
  }

  /**
     * Get node ID
     */
  get nodeId(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 4, true);
  }

  /**
     * Set node ID
     */
  set nodeId(value: number) {
    this.pointer.segment.view.setUint32(this.pointer.offset + 4, value, true);
  }

  /**
     * Get parent node ID
     */
  get parentId(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 8, true);
  }

  /**
     * Set parent node ID
     */
  set parentId(value: number) {
    this.pointer.segment.view.setUint32(this.pointer.offset + 8, value, true);
  }

  /**
     * Get node depth in the tree
     */
  get depth(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 12, true);
  }

  /**
     * Set node depth
     */
  set depth(value: number) {
    this.pointer.segment.view.setUint32(this.pointer.offset + 12, value, true);
  }

  /**
     * Get node flags
     */
  get flags(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 16, true);
  }

  /**
     * Set node flags
     */
  set flags(value: number) {
    this.pointer.segment.view.setUint32(this.pointer.offset + 16, value, true);
  }

  /**
     * Get node name
     */
  get name(): string {
    const nameId = this.pointer.segment.view.getUint32(this.pointer.offset + 20, true);
    return this.stringInterner.getString(nameId);
  }

  /**
     * Set node name
     */
  set name(value: string) {
    const nameId = this.stringInterner.intern(value);
    this.pointer.segment.view.setUint32(this.pointer.offset + 20, nameId, true);
  }

  /**
     * Get node value
     */
  get value(): string {
    const valueId = this.pointer.segment.view.getUint32(this.pointer.offset + 24, true);
    return this.stringInterner.getString(valueId);
  }

  /**
     * Set node value
     */
  set value(str: string) {
    const valueId = this.stringInterner.intern(str);
    this.pointer.segment.view.setUint32(this.pointer.offset + 24, valueId, true);
  }

  /**
     * Get child count
     */
  get childCount(): number {
    return this.pointer.segment.view.getUint32(this.pointer.offset + 28, true);
  }

  /**
     * Get node span
     */
  get span(): TokenSpan {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    return {
      start: {
        line: view.getUint32(offset + 48, true),
        column: view.getUint32(offset + 52, true),
        offset: view.getUint32(offset + 56, true),
      },
      end: {
        line: view.getUint32(offset + 60, true),
        column: view.getUint32(offset + 64, true), // Note: this goes beyond the documented 16 bytes
        offset: view.getUint32(offset + 68, true), // We need 24 bytes total for full span
      },
    };
  }

  /**
     * Set node span
     */
  set span(value: TokenSpan) {
    const view = this.pointer.segment.view;
    const offset = this.pointer.offset;

    view.setUint32(offset + 48, value.start.line, true);
    view.setUint32(offset + 52, value.start.column, true);
    view.setUint32(offset + 56, value.start.offset, true);
    view.setUint32(offset + 60, value.end.line, true);
    view.setUint32(offset + 64, value.end.column, true);
    view.setUint32(offset + 68, value.end.offset, true);
  }

  /**
   * Copy position information from another node
   */
  copyPositionFrom(sourceNode: ZeroCopyASTNode): void {
    this.span = sourceNode.span;
  }

  /**
   * Get start position
   */
  getStartPosition(): { line: number; column: number; offset: number } {
    return this.span.start;
  }

  /**
   * Get end position
   */
  getEndPosition(): { line: number; column: number; offset: number } {
    return this.span.end;
  }

  /**
   * Set start position
   */
  setStartPosition(position: { line: number; column: number; offset: number }): void {
    const currentSpan = this.span;
    this.span = {
      start: position,
      end: currentSpan.end,
    };
  }

  /**
   * Set end position
   */
  setEndPosition(position: { line: number; column: number; offset: number }): void {
    const currentSpan = this.span;
    this.span = {
      start: currentSpan.start,
      end: position,
    };
  }

  /**
     * Get the memory pointer for this node
     */
  get memoryPointer(): MemoryPointer {
    return this.pointer;
  }

  /**
     * Add a child node
     */
  appendChild(child: ZeroCopyASTNode): void {
    const currentCount = this.childCount;

    // Allocate or expand children array
    if (currentCount === 0) {
      // First child - allocate new array
      const childrenPointer = this.arena.allocate(8, 8); // Start with space for 1 pointer
      const listPointer = PointerUtils.encodeListPointer(
        PointerUtils.pointerDistance(this.pointer, childrenPointer),
        3, // 8-byte elements (pointers)
        1,   // 1 element
      );

      this.pointer.segment.view.setBigUint64(this.pointer.offset + 32, listPointer, true);

      // Store child pointer
      childrenPointer.segment.view.setBigUint64(
        childrenPointer.offset,
        BigInt(child.pointer.offset),
        true,
      );
    } else {
      // Expand existing array (simplified - in practice, you'd use a growth strategy)
      const childrenListPointer = this.pointer.segment.view.getBigUint64(this.pointer.offset + 32, true);
      const listInfo = PointerUtils.decodeListPointer(childrenListPointer);

      // For simplicity, allocate a new larger array and copy
      const newSize = currentCount + 1;
      const newChildrenPointer = this.arena.allocate(newSize * 8, 8);

      // Copy existing children
      const oldChildrenPointer = this.pointer.add(listInfo.offset);
      PointerUtils.copyMemory(oldChildrenPointer, newChildrenPointer, currentCount * 8);

      // Add new child
      newChildrenPointer.segment.view.setBigUint64(
        newChildrenPointer.offset + (currentCount * 8),
        BigInt(child.pointer.offset),
        true,
      );

      // Update list pointer
      const newListPointer = PointerUtils.encodeListPointer(
        PointerUtils.pointerDistance(this.pointer, newChildrenPointer),
        3, // 8-byte elements
        newSize,
      );

      this.pointer.segment.view.setBigUint64(this.pointer.offset + 32, newListPointer, true);
    }

    // Update child count
    this.pointer.segment.view.setUint32(this.pointer.offset + 28, currentCount + 1, true);

    // Set child metadata
    child.parentId = this.nodeId;
    child.depth = this.depth + 1;
  }

  /**
     * Get a child node by index
     */
  getChild(index: number): ZeroCopyASTNode | null {
    if (index < 0 || index >= this.childCount) {
      return null;
    }

    const childrenListPointer = this.pointer.segment.view.getBigUint64(this.pointer.offset + 32, true);
    if (childrenListPointer === BigInt(0)) {
      return null;
    }

    const listInfo = PointerUtils.decodeListPointer(childrenListPointer);
    const childrenPointer = this.pointer.add(listInfo.offset);

    // Get child pointer offset
    const childOffset = childrenPointer.segment.view.getBigUint64(
      childrenPointer.offset + (index * 8),
      true,
    );

    const childPointer = new MemoryPointer(this.pointer.segment, Number(childOffset));
    return ZeroCopyASTNode.fromMemory(childPointer, this.arena, this.stringInterner);
  }

  /**
     * Get all children
     */
  getChildren(): ZeroCopyASTNode[] {
    const children: ZeroCopyASTNode[] = [];
    for (let i = 0; i < this.childCount; i++) {
      const child = this.getChild(i);
      if (child) {
        children.push(child);
      }
    }
    return children;
  }

  /**
     * Find the first child matching a predicate
     */
  findChild(predicate: (node: ZeroCopyASTNode) => boolean): ZeroCopyASTNode | null {
    for (let i = 0; i < this.childCount; i++) {
      const child = this.getChild(i);
      if (child && predicate(child)) {
        return child;
      }
    }
    return null;
  }

  /**
     * Find all children matching a predicate
     */
  findChildren(predicate: (node: ZeroCopyASTNode) => boolean): ZeroCopyASTNode[] {
    const result: ZeroCopyASTNode[] = [];
    for (let i = 0; i < this.childCount; i++) {
      const child = this.getChild(i);
      if (child && predicate(child)) {
        result.push(child);
      }
    }
    return result;
  }

  /**
     * Traverse the tree depth-first
     */
  *traverse(): Generator<ZeroCopyASTNode> {
    yield this;

    for (let i = 0; i < this.childCount; i++) {
      const child = this.getChild(i);
      if (child) {
        // Use Array.from to convert generator to array for better compatibility
        const childNodes = Array.from(child.traverse());
        for (const descendant of childNodes) {
          yield descendant;
        }
      }
    }
  }

  /**
     * Check if this is a leaf node
     */
  isLeaf(): boolean {
    return this.childCount === 0;
  }

  /**
     * Check if this is a root node
     */
  isRoot(): boolean {
    return this.parentId === 0;
  }

  /**
     * Check if this node has a specific type
     */
  isType(type: ASTNodeType): boolean {
    return this.nodeType === type;
  }

  /**
     * Check if this node has a specific name
     */
  hasName(name: string): boolean {
    // Fast path: compare string IDs if the string is already interned
    const nameId = this.stringInterner.getStringId(name);
    if (nameId !== undefined) {
      const thisNameId = this.pointer.segment.view.getUint32(this.pointer.offset + 20, true);
      return thisNameId === nameId;
    }

    // Slow path: compare actual strings
    return this.name === name;
  }

  /**
     * Clone this node (shallow copy)
     */
  clone(): ZeroCopyASTNode {
    const newPointer = this.arena.allocate(ZeroCopyASTNode.HEADER_SIZE, ZeroCopyASTNode.ALIGNMENT);

    // Copy header data
    PointerUtils.copyMemory(this.pointer, newPointer, ZeroCopyASTNode.HEADER_SIZE);

    // Reset parent/child relationships
    const view = newPointer.segment.view;
    view.setUint32(newPointer.offset + 4, 0, true); // nodeId
    view.setUint32(newPointer.offset + 8, 0, true); // parentId
    view.setUint32(newPointer.offset + 28, 0, true); // childCount
    view.setBigUint64(newPointer.offset + 32, BigInt(0), true); // childrenPointer

    return new ZeroCopyASTNode(newPointer, this.arena, this.stringInterner);
  }

  /**
     * Deep clone this node and all its children
     */
  deepClone(): ZeroCopyASTNode {
    const cloned = this.clone();

    // Clone all children
    for (let i = 0; i < this.childCount; i++) {
      const child = this.getChild(i);
      if (child) {
        const clonedChild = child.deepClone();
        cloned.appendChild(clonedChild);
      }
    }

    return cloned;
  }

  /**
     * Serialize node to a plain object
     */
  toPlainObject(): PlainASTNode {
    return {
      nodeType: this.nodeType,
      nodeId: this.nodeId,
      parentId: this.parentId,
      depth: this.depth,
      flags: this.flags,
      name: this.name,
      value: this.value,
      span: this.span,
      attributes: this.getAttributes(),
      children: this.getChildren().map(child => child.toPlainObject()),
    };
  }

  /**
     * Create node from a plain object
     */
  static fromPlainObject(
    arena: MemoryArena,
    stringInterner: StringInterner,
    plain: PlainASTNode,
  ): ZeroCopyASTNode {
    const node = ZeroCopyASTNode.create(
      arena,
      stringInterner,
      plain.nodeType,
      plain.name,
      plain.value,
      plain.span,
    );

    node.nodeId = plain.nodeId;
    node.parentId = plain.parentId;
    node.depth = plain.depth;
    node.flags = plain.flags;

    // Set attributes
    if (plain.attributes) {
      node.setAttributes(plain.attributes);
    }

    // Add children
    for (const childPlain of plain.children) {
      const child = ZeroCopyASTNode.fromPlainObject(arena, stringInterner, childPlain);
      node.appendChild(child);
    }

    return node;
  }

  /**
     * Get a string representation of the node
     */
  toString(): string {
    const typeStr = ASTNodeType[this.nodeType] || `Unknown(${this.nodeType})`;
    const name = this.name ? ` "${this.name}"` : '';
    const value = this.value ? ` = "${this.value}"` : '';
    return `${typeStr}${name}${value} (${this.childCount} children)`;
  }

  /**
     * Set an attribute value
     */
  setAttribute(key: string, value: string): void {
    // Store attributes as a simple map in the attributesPointer location
    // For simplicity, we'll use a JSON-encoded approach
    const attributes = this.getAttributes();
    attributes[key] = value;
    this.setAttributes(attributes);
  }

  /**
   * Get an attribute value
   */
  getAttribute(key: string): string | undefined {
    const attributes = this.getAttributes();
    return attributes[key];
  }

  /**
   * Get all attributes as an object
   */
  private getAttributes(): Record<string, string> {
    const attributesPointer = this.pointer.segment.view.getBigUint64(this.pointer.offset + 40, true);
    if (attributesPointer === BigInt(0)) {
      return {};
    }

    try {
      // For simplicity, we'll store attributes as a JSON string
      const strId = Number(attributesPointer);
      const jsonStr = this.stringInterner.getString(strId);
      return jsonStr ? JSON.parse(jsonStr) : {};
    } catch {
      return {};
    }
  }

  /**
   * Set all attributes from an object
   */
  setAttributes(attributes: Record<string, string>): void {
    const jsonStr = JSON.stringify(attributes);
    const strId = this.stringInterner.intern(jsonStr);
    this.pointer.segment.view.setBigUint64(this.pointer.offset + 40, BigInt(strId), true);
  }

  /**
     * Get parent node (requires traversal to find parent by parentId)
     */
  getParent(): ZeroCopyASTNode | null {
    if (this.parentId === 0) {
      return null;
    }

    // This is a simplified implementation - in practice, you'd need
    // a more efficient way to find parent by ID, such as maintaining
    // a node registry or parent pointer
    return null; // Placeholder - would need proper implementation
  }
}

/**
 * Plain object representation for serialization
 */
export interface PlainASTNode {
    nodeType: ASTNodeType;
    nodeId: number;
    parentId: number;
    depth: number;
    flags: number;
    name: string;
    value: string;
    span: TokenSpan;
    attributes: Record<string, string>;
    children: PlainASTNode[];
}

/**
 * AST tree for managing collections of nodes
 */
export class ZeroCopyAST {
  private arena: MemoryArena;
  private stringInterner: StringInterner;
  private root: ZeroCopyASTNode | null = null;
  private nodeIdCounter: number = 1;

  constructor(arena: MemoryArena, stringInterner: StringInterner) {
    this.arena = arena;
    this.stringInterner = stringInterner;
  }

  /**
     * Create the root node
     */
  createRoot(nodeType: ASTNodeType = ASTNodeType.PROGRAM, name?: string): ZeroCopyASTNode {
    this.root = ZeroCopyASTNode.create(this.arena, this.stringInterner, nodeType, name);
    this.root.nodeId = this.nodeIdCounter++;
    return this.root;
  }

  /**
     * Get the root node
     */
  getRoot(): ZeroCopyASTNode | null {
    return this.root;
  }

  /**
     * Set the root node
     */
  setRoot(node: ZeroCopyASTNode): void {
    this.root = node;
  }

  /**
     * Create a new node with auto-assigned ID
     */
  createNode(
    nodeType: ASTNodeType,
    name?: string,
    value?: string,
    span?: TokenSpan,
  ): ZeroCopyASTNode {
    const node = ZeroCopyASTNode.create(this.arena, this.stringInterner, nodeType, name, value, span);
    node.nodeId = this.nodeIdCounter++;
    return node;
  }

  /**
     * Get tree statistics
     */
  getStatistics(): ASTStatistics {
    if (!this.root) {
      return {
        nodeCount: 0,
        maxDepth: 0,
        averageChildCount: 0,
        memoryUsed: 0,
        leafNodeCount: 0,
      };
    }

    let nodeCount = 0;
    let maxDepth = 0;
    let totalChildren = 0;
    let leafNodeCount = 0;

    // Use Array.from to convert generator to array for better compatibility
    const nodes = Array.from(this.root.traverse());
    for (const node of nodes) {
      nodeCount++;
      maxDepth = Math.max(maxDepth, node.depth);
      totalChildren += node.childCount;
      if (node.isLeaf()) {
        leafNodeCount++;
      }
    }

    return {
      nodeCount,
      maxDepth,
      averageChildCount: nodeCount > 0 ? totalChildren / nodeCount : 0,
      memoryUsed: nodeCount * ZeroCopyASTNode.getHeaderSize(),
      leafNodeCount,
    };
  }
}

export interface ASTStatistics {
    nodeCount: number;
    maxDepth: number;
    averageChildCount: number;
    memoryUsed: number;
    leafNodeCount: number;
}

