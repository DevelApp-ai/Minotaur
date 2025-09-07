/**
 * Tests for RefactoringEngine to ensure undo/redo functionality works correctly
 */

import { RefactoringEngine } from '../RefactoringEngine';
import { ChangeOperationType } from '../RefactoringEngine';
import { ContextManager } from '../../context/ContextManager';
import { EventEmitter } from 'events';

// Mock ContextManager for testing
class MockContextManager extends EventEmitter {
  interpreter: any;

  constructor() {
    super();
    this.interpreter = {
      evaluate: jest.fn().mockResolvedValue(true),
    };
  }

  getContextAt(_file: string, _position: any) {
    return {
      scope: 'global',
      variables: new Map(),
      functions: new Map(),
      imports: new Map(),
    };
  }

  updateContext(_file: string, _changes: any) {
    return true;
  }
}

describe('RefactoringEngine', () => {
  let engine: RefactoringEngine;
  let mockContextManager: MockContextManager;

  beforeEach(() => {
    mockContextManager = new MockContextManager();
    const config = {
      enableUndo: true,
      maxUndoSteps: 10,
      enablePreview: false,
      enableValidation: true,
    };
    engine = new RefactoringEngine(mockContextManager as any, config);
  });

  describe('Undo/Redo Functionality', () => {
    it('should return false when undoing with empty stack', async () => {
      const result = await engine.undo();
      expect(result).toBe(false);
    });

    it('should return false when redoing with empty stack', async () => {
      const result = await engine.redo();
      expect(result).toBe(false);
    });

    it('should handle basic refactoring with undo capability', async () => {
      // Directly test undo/redo by adding items to the stack
      const mockUndoInfo = {
        operationId: 'test-op',
        timestamp: Date.now(),
        changes: [{
          id: 'test-change',
          type: ChangeOperationType.REPLACE,
          file: 'test.ts',
          position: { line: 1, column: 1 },
          oldText: 'old',
          newText: 'new',
          description: 'Test change',
        }],
        contextSnapshot: {},
        description: 'Test operation',
      };

      // Add to undo stack directly
      (engine as any).undoStack.push(mockUndoInfo);

      // Mock applyChanges to avoid actual file operations
      const originalApplyChanges = (engine as any).applyChanges;
      (engine as any).applyChanges = jest.fn().mockResolvedValue(true);

      // Test undo
      const undoResult = await engine.undo();
      expect(undoResult).toBe(true);

      // Test redo
      const redoResult = await engine.redo();
      expect(redoResult).toBe(true);

      // Restore original method
      (engine as any).applyChanges = originalApplyChanges;
    });

    it('should emit events during undo/redo operations', async () => {
      let eventEmitted = false;
      engine.on('operation_undone', () => {
        eventEmitted = true;
      });

      // Add item to undo stack directly
      const mockUndoInfo = {
        operationId: 'test-op-2',
        timestamp: Date.now(),
        changes: [{
          id: 'test-change-2',
          type: ChangeOperationType.INSERT,
          file: 'test.ts',
          position: { line: 1, column: 1 },
          oldText: '',
          newText: 'inserted',
          description: 'Test insert',
        }],
        contextSnapshot: {},
        description: 'Test operation 2',
      };

      (engine as any).undoStack.push(mockUndoInfo);

      // Mock applyChanges
      (engine as any).applyChanges = jest.fn().mockResolvedValue(true);

      // Undo should emit event
      await engine.undo();
      expect(eventEmitted).toBe(true);
    });
  });

  describe('Change Validation', () => {
    it('should validate changes before applying', async () => {
      const invalidChanges = [{
        id: 'invalid-change',
        type: ChangeOperationType.REPLACE,
        file: '', // Invalid: empty file
        position: { line: 1, column: 1 },
        oldText: 'old',
        newText: 'new',
        description: 'Invalid change',
      }];

      await expect(engine.applyChanges(invalidChanges)).rejects.toThrow();
    });

    it('should sort changes by position for safe application', () => {
      const changes = [
        {
          id: 'change-1',
          type: ChangeOperationType.REPLACE,
          file: 'test.ts',
          position: { line: 1, column: 1 },
          oldText: 'old1',
          newText: 'new1',
          description: 'Change 1',
        },
        {
          id: 'change-2',
          type: ChangeOperationType.REPLACE,
          file: 'test.ts',
          position: { line: 2, column: 1 },
          oldText: 'old2',
          newText: 'new2',
          description: 'Change 2',
        },
      ];

      const sorted = (engine as any).sortChangesForApplication(changes);
      expect(sorted[0].position.line).toBeGreaterThanOrEqual(sorted[1].position.line);
    });
  });

  describe('Error Handling', () => {
    it('should handle undo errors gracefully', async () => {
      // Add item to undo stack
      const mockUndoInfo = {
        operationId: 'test-op-3',
        timestamp: Date.now(),
        changes: [{
          id: 'test-change-3',
          type: ChangeOperationType.DELETE,
          file: 'test.ts',
          position: { line: 1, column: 1 },
          oldText: 'delete me',
          newText: '',
          description: 'Test delete',
        }],
        contextSnapshot: {},
        description: 'Test operation 3',
      };

      (engine as any).undoStack.push(mockUndoInfo);

      // Mock applyChanges to fail
      (engine as any).applyChanges = jest.fn().mockRejectedValue(new Error('Undo failed'));

      // Undo should handle error
      await expect(engine.undo()).rejects.toThrow('Undo failed');
    });
  });
});