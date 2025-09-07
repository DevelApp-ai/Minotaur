/**
 * Tests for ZeroCopyASTNode position methods implementation
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { MemoryArena } from '../memory/arena/MemoryArena';
import { StringInterner } from '../memory/strings/StringInterner';

describe('ZeroCopyASTNode Position Methods', () => {
  let arena: MemoryArena;
  let stringInterner: StringInterner;
  let sourceNode: ZeroCopyASTNode;
  let targetNode: ZeroCopyASTNode;

  beforeEach(() => {
    arena = new MemoryArena(1024 * 1024); // 1MB
    stringInterner = new StringInterner(arena);

    sourceNode = ZeroCopyASTNode.create(
      arena,
      stringInterner,
      ASTNodeType.STATEMENT,
      'source',
      'test',
      {
        start: { line: 10, column: 5, offset: 100 },
        end: { line: 10, column: 15, offset: 110 },
      },
    );

    targetNode = ZeroCopyASTNode.create(
      arena,
      stringInterner,
      ASTNodeType.EXPRESSION,
      'target',
    );
  });

  describe('position getters', () => {
    it('should return correct start position', () => {
      const startPos = sourceNode.getStartPosition();
      expect(startPos.line).toBe(10);
      expect(startPos.column).toBe(5);
      expect(startPos.offset).toBe(100);
    });

    it('should return correct end position', () => {
      const endPos = sourceNode.getEndPosition();
      expect(endPos.line).toBe(10);
      expect(endPos.column).toBe(15);
      expect(endPos.offset).toBe(110);
    });
  });

  describe('position setters', () => {
    it('should set start position correctly', () => {
      const newStartPos = { line: 20, column: 10, offset: 200 };
      targetNode.setStartPosition(newStartPos);

      const retrievedPos = targetNode.getStartPosition();
      expect(retrievedPos.line).toBe(20);
      expect(retrievedPos.column).toBe(10);
      expect(retrievedPos.offset).toBe(200);
    });

    it('should set end position correctly', () => {
      const newEndPos = { line: 25, column: 20, offset: 250 };
      targetNode.setEndPosition(newEndPos);

      const retrievedPos = targetNode.getEndPosition();
      expect(retrievedPos.line).toBe(25);
      expect(retrievedPos.column).toBe(20);
      expect(retrievedPos.offset).toBe(250);
    });

    it('should preserve other position when setting start', () => {
      // Set initial end position
      targetNode.setEndPosition({ line: 30, column: 25, offset: 300 });

      // Set new start position
      targetNode.setStartPosition({ line: 25, column: 15, offset: 250 });

      // End position should be preserved
      const endPos = targetNode.getEndPosition();
      expect(endPos.line).toBe(30);
      expect(endPos.column).toBe(25);
      expect(endPos.offset).toBe(300);

      // Start position should be updated
      const startPos = targetNode.getStartPosition();
      expect(startPos.line).toBe(25);
      expect(startPos.column).toBe(15);
      expect(startPos.offset).toBe(250);
    });
  });

  describe('copyPositionFrom', () => {
    it('should copy position from source to target', () => {
      targetNode.copyPositionFrom(sourceNode);

      const targetStart = targetNode.getStartPosition();
      const targetEnd = targetNode.getEndPosition();
      const sourceStart = sourceNode.getStartPosition();
      const sourceEnd = sourceNode.getEndPosition();

      expect(targetStart.line).toBe(sourceStart.line);
      expect(targetStart.column).toBe(sourceStart.column);
      expect(targetStart.offset).toBe(sourceStart.offset);

      expect(targetEnd.line).toBe(sourceEnd.line);
      expect(targetEnd.column).toBe(sourceEnd.column);
      expect(targetEnd.offset).toBe(sourceEnd.offset);
    });

    it('should handle copying from node with different initial position', () => {
      // Set different initial position on target
      targetNode.setStartPosition({ line: 1, column: 1, offset: 0 });
      targetNode.setEndPosition({ line: 1, column: 10, offset: 9 });

      // Copy from source
      targetNode.copyPositionFrom(sourceNode);

      // Should now match source position
      const targetStart = targetNode.getStartPosition();
      const targetEnd = targetNode.getEndPosition();

      expect(targetStart.line).toBe(10);
      expect(targetStart.column).toBe(5);
      expect(targetStart.offset).toBe(100);

      expect(targetEnd.line).toBe(10);
      expect(targetEnd.column).toBe(15);
      expect(targetEnd.offset).toBe(110);
    });
  });

  describe('span property integration', () => {
    it('should maintain consistency between span and position methods', () => {
      const newSpan = {
        start: { line: 50, column: 30, offset: 500 },
        end: { line: 55, column: 40, offset: 600 },
      };

      targetNode.span = newSpan;

      const startPos = targetNode.getStartPosition();
      const endPos = targetNode.getEndPosition();

      expect(startPos).toEqual(newSpan.start);
      expect(endPos).toEqual(newSpan.end);
    });

    it('should update span when using position setters', () => {
      targetNode.setStartPosition({ line: 100, column: 50, offset: 1000 });
      targetNode.setEndPosition({ line: 105, column: 60, offset: 1100 });

      const span = targetNode.span;

      expect(span.start.line).toBe(100);
      expect(span.start.column).toBe(50);
      expect(span.start.offset).toBe(1000);

      expect(span.end.line).toBe(105);
      expect(span.end.column).toBe(60);
      expect(span.end.offset).toBe(1100);
    });
  });
});