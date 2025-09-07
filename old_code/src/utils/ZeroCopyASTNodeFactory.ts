/**
 * Factory for creating and managing ZeroCopyASTNode instances in object pools
 */

import { ObjectFactory, PoolableObject } from '../memory/pools/ObjectPool';
import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { MemoryArena } from '../memory/arena/MemoryArena';
import { StringInterner } from '../memory/strings/StringInterner';

/**
 * Poolable wrapper for ZeroCopyASTNode
 */
export class PoolableZeroCopyASTNode implements PoolableObject {
  private node: ZeroCopyASTNode | null = null;
  private _inUse: boolean = false;

  constructor(private arena: MemoryArena, private stringInterner: StringInterner) {}

  getNode(): ZeroCopyASTNode | null {
    return this.node;
  }

  setNode(node: ZeroCopyASTNode): void {
    this.node = node;
  }

  reset(): void {
    if (this.node) {
      // Reset the node to a clean state
      // The actual memory is managed by the arena
      this.node = null;
    }
    this._inUse = false;
  }

  isInUse(): boolean {
    return this._inUse;
  }

  setInUse(inUse: boolean): void {
    this._inUse = inUse;
  }

  /**
   * Initialize the node with basic properties
   */
  initialize(nodeType: ASTNodeType, nodeId: number): void {
    if (!this.node) {
      // Create a new ZeroCopyASTNode using the static create method
      this.node = ZeroCopyASTNode.create(this.arena, this.stringInterner, nodeType);
      this.node.nodeId = nodeId;
    } else {
      // Reuse existing node but reset its properties
      this.node.nodeType = nodeType;
      this.node.nodeId = nodeId;
      // Note: ZeroCopyASTNode doesn't have clearChildren/clearTokens methods
      // The node will be reset through the arena memory management
    }
    this._inUse = true;
  }
}

/**
 * Factory for creating PoolableZeroCopyASTNode instances
 */
export class ZeroCopyASTNodeFactory implements ObjectFactory<PoolableZeroCopyASTNode> {
  constructor(private arena: MemoryArena, private stringInterner: StringInterner) {}

  create(): PoolableZeroCopyASTNode {
    return new PoolableZeroCopyASTNode(this.arena, this.stringInterner);
  }

  reset(node: PoolableZeroCopyASTNode): void {
    node.reset();
  }

  validate(node: PoolableZeroCopyASTNode): boolean {
    return node.getNode() !== null;
  }
}

