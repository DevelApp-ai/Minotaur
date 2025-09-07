/**
 * Code operations for real-time surgical modifications in Minotaur.
 * Provides atomic code operations with grammar-level precision and rollback support.
 */

import { CodeChange, ChangeOperationType } from './RefactoringEngine';
import { CodePosition } from '../context/ContextAwareParser';
import { EventEmitter } from 'events';

/**
 * Atomic operation types for code modifications.
 */
export enum AtomicOperationType {
  INSERT_TEXT = 'insert_text',
  DELETE_TEXT = 'delete_text',
  REPLACE_TEXT = 'replace_text',
  MOVE_TEXT = 'move_text',
  INSERT_LINE = 'insert_line',
  DELETE_LINE = 'delete_line',
  INDENT_BLOCK = 'indent_block',
  UNINDENT_BLOCK = 'unindent_block',
  WRAP_BLOCK = 'wrap_block',
  UNWRAP_BLOCK = 'unwrap_block'
}

/**
 * Atomic code operation.
 */
export interface AtomicOperation {
  id: string;
  type: AtomicOperationType;
  file: string;
  position: CodePosition;
  endPosition?: CodePosition;
  text?: string;
  newText?: string;
  indentLevel?: number;
  wrapperStart?: string;
  wrapperEnd?: string;
  metadata: any;
}

/**
 * Operation batch for atomic execution.
 */
export interface OperationBatch {
  id: string;
  operations: AtomicOperation[];
  description: string;
  timestamp: number;
  rollbackInfo?: RollbackInfo;
}

/**
 * Rollback information for operation batches.
 */
export interface RollbackInfo {
  batchId: string;
  reverseOperations: AtomicOperation[];
  originalState: FileState[];
  timestamp: number;
}

/**
 * File state snapshot for rollback.
 */
export interface FileState {
  file: string;
  content: string;
  checksum: string;
  timestamp: number;
}

/**
 * Operation execution result.
 */
export interface OperationResult {
  success: boolean;
  operationsExecuted: number;
  errors: OperationError[];
  warnings: OperationWarning[];
  executionTime: number;
  rollbackInfo?: RollbackInfo;
}

/**
 * Operation error.
 */
export interface OperationError {
  operationId: string;
  message: string;
  position: CodePosition;
  code: string;
}

/**
 * Operation warning.
 */
export interface OperationWarning {
  operationId: string;
  message: string;
  position: CodePosition;
  severity: 'low' | 'medium' | 'high';
}

/**
 * Text manipulation utilities.
 */
class TextManipulator {
  /**
   * Inserts text at a specific position.
   */
  public static insertText(content: string, position: CodePosition, text: string): string {
    const lines = content.split('\n');

    if (position.line < 1 || position.line > lines.length) {
      throw new Error(`Invalid line number: ${position.line}`);
    }

    const lineIndex = position.line - 1;
    const line = lines[lineIndex];

    if (position.column < 0 || position.column > line.length) {
      throw new Error(`Invalid column number: ${position.column}`);
    }

    const newLine = line.slice(0, position.column) + text + line.slice(position.column);
    lines[lineIndex] = newLine;

    return lines.join('\n');
  }

  /**
   * Deletes text in a specific range.
   */
  public static deleteText(
    content: string,
    startPos: CodePosition,
    endPos: CodePosition,
  ): string {
    const lines = content.split('\n');

    if (startPos.line === endPos.line) {
      // Single line deletion
      const lineIndex = startPos.line - 1;
      const line = lines[lineIndex];
      const newLine = line.slice(0, startPos.column) + line.slice(endPos.column);
      lines[lineIndex] = newLine;
    } else {
      // Multi-line deletion
      const startLineIndex = startPos.line - 1;
      const endLineIndex = endPos.line - 1;

      const startLine = lines[startLineIndex];
      const endLine = lines[endLineIndex];

      const newLine = startLine.slice(0, startPos.column) + endLine.slice(endPos.column);

      // Replace the range with the merged line
      lines.splice(startLineIndex, endLineIndex - startLineIndex + 1, newLine);
    }

    return lines.join('\n');
  }

  /**
   * Replaces text in a specific range.
   */
  public static replaceText(
    content: string,
    startPos: CodePosition,
    endPos: CodePosition,
    newText: string,
  ): string {
    const afterDeletion = this.deleteText(content, startPos, endPos);
    return this.insertText(afterDeletion, startPos, newText);
  }

  /**
   * Moves text from one position to another.
   */
  public static moveText(
    content: string,
    sourceStart: CodePosition,
    sourceEnd: CodePosition,
    targetPos: CodePosition,
  ): string {
    // Extract the text to move
    const textToMove = this.extractText(content, sourceStart, sourceEnd);

    // Delete from source
    const afterDeletion = this.deleteText(content, sourceStart, sourceEnd);

    // Adjust target position if it's after the deleted text
    const adjustedTargetPos = this.adjustPositionAfterDeletion(
      targetPos,
      sourceStart,
      sourceEnd,
    );

    // Insert at target
    return this.insertText(afterDeletion, adjustedTargetPos, textToMove);
  }

  /**
   * Extracts text from a specific range.
   */
  public static extractText(
    content: string,
    startPos: CodePosition,
    endPos: CodePosition,
  ): string {
    const lines = content.split('\n');

    if (startPos.line === endPos.line) {
      // Single line extraction
      const line = lines[startPos.line - 1];
      return line.slice(startPos.column, endPos.column);
    } else {
      // Multi-line extraction
      const result: string[] = [];

      // First line
      const firstLine = lines[startPos.line - 1];
      result.push(firstLine.slice(startPos.column));

      // Middle lines
      for (let i = startPos.line; i < endPos.line - 1; i++) {
        result.push(lines[i]);
      }

      // Last line
      const lastLine = lines[endPos.line - 1];
      result.push(lastLine.slice(0, endPos.column));

      return result.join('\n');
    }
  }

  /**
   * Inserts a new line at a specific position.
   */
  public static insertLine(content: string, position: CodePosition, text: string = ''): string {
    const lines = content.split('\n');
    lines.splice(position.line - 1, 0, text);
    return lines.join('\n');
  }

  /**
   * Deletes a line at a specific position.
   */
  public static deleteLine(content: string, lineNumber: number): string {
    const lines = content.split('\n');

    if (lineNumber < 1 || lineNumber > lines.length) {
      throw new Error(`Invalid line number: ${lineNumber}`);
    }

    lines.splice(lineNumber - 1, 1);
    return lines.join('\n');
  }

  /**
   * Indents a block of text.
   */
  public static indentBlock(
    content: string,
    startLine: number,
    endLine: number,
    indentString: string = '  ',
  ): string {
    const lines = content.split('\n');

    for (let i = startLine - 1; i < endLine && i < lines.length; i++) {
      if (lines[i].trim().length > 0) { // Don't indent empty lines
        lines[i] = indentString + lines[i];
      }
    }

    return lines.join('\n');
  }

  /**
   * Unindents a block of text.
   */
  public static unindentBlock(
    content: string,
    startLine: number,
    endLine: number,
    indentString: string = '  ',
  ): string {
    const lines = content.split('\n');

    for (let i = startLine - 1; i < endLine && i < lines.length; i++) {
      if (lines[i].startsWith(indentString)) {
        lines[i] = lines[i].slice(indentString.length);
      }
    }

    return lines.join('\n');
  }

  /**
   * Wraps a block of text with specified delimiters.
   */
  public static wrapBlock(
    content: string,
    startPos: CodePosition,
    endPos: CodePosition,
    wrapperStart: string,
    wrapperEnd: string,
  ): string {
    // Insert wrapper end first (to avoid position adjustment)
    const afterEndInsert = this.insertText(content, endPos, wrapperEnd);

    // Insert wrapper start
    return this.insertText(afterEndInsert, startPos, wrapperStart);
  }

  /**
   * Unwraps a block of text by removing specified delimiters.
   */
  public static unwrapBlock(
    content: string,
    startPos: CodePosition,
    endPos: CodePosition,
    wrapperStart: string,
    wrapperEnd: string,
  ): string {
    // Calculate end position of wrapper start
    const wrapperStartEnd: CodePosition = {
      line: startPos.line,
      column: startPos.column + wrapperStart.length,
      offset: startPos.offset + wrapperStart.length,
    };

    // Calculate start position of wrapper end
    const wrapperEndStart: CodePosition = {
      line: endPos.line,
      column: endPos.column - wrapperEnd.length,
      offset: endPos.offset - wrapperEnd.length,
    };

    // Remove wrapper end first
    const afterEndRemoval = this.deleteText(content, wrapperEndStart, endPos);

    // Remove wrapper start
    return this.deleteText(afterEndRemoval, startPos, wrapperStartEnd);
  }

  /**
   * Adjusts a position after a deletion operation.
   */
  private static adjustPositionAfterDeletion(
    position: CodePosition,
    deletionStart: CodePosition,
    deletionEnd: CodePosition,
  ): CodePosition {
    if (position.line < deletionStart.line) {
      return position; // Position is before deletion, no adjustment needed
    }

    if (position.line > deletionEnd.line) {
      // Position is after deletion, adjust line number
      const linesDeleted = deletionEnd.line - deletionStart.line;
      return {
        line: position.line - linesDeleted,
        column: position.column,
        offset: position.offset,
      };
    }

    // Position is within the deleted range, move to deletion start
    return deletionStart;
  }
}

/**
 * Code operations manager for atomic operations and batching.
 */
export class CodeOperations extends EventEmitter {
  private fileContents: Map<string, string>;
  private operationHistory: OperationBatch[];
  private maxHistorySize: number;

  constructor(maxHistorySize: number = 1000) {
    super();

    this.fileContents = new Map();
    this.operationHistory = [];
    this.maxHistorySize = maxHistorySize;
  }

  /**
   * Loads file content for operations.
   */
  public loadFile(file: string, content: string): void {
    this.fileContents.set(file, content);
    this.emit('file_loaded', { file, contentLength: content.length });
  }

  /**
   * Gets current file content.
   */
  public getFileContent(file: string): string | null {
    return this.fileContents.get(file) || null;
  }

  /**
   * Executes a batch of atomic operations.
   */
  public async executeBatch(batch: OperationBatch): Promise<OperationResult> {
    const startTime = Date.now();
    const errors: OperationError[] = [];
    const warnings: OperationWarning[] = [];
    let operationsExecuted = 0;

    try {
      // Create rollback information
      const rollbackInfo = this.createRollbackInfo(batch);

      // Execute operations in order
      for (const operation of batch.operations) {
        try {
          await this.executeAtomicOperation(operation);
          operationsExecuted++;
        } catch (error) {
          errors.push({
            operationId: operation.id,
            message: error instanceof Error ? error.message : String(error),
            position: operation.position,
            code: 'EXECUTION_ERROR',
          });

          // Rollback on error
          await this.rollbackBatch(rollbackInfo);
          break;
        }
      }

      // Add to history if successful
      if (errors.length === 0) {
        batch.rollbackInfo = rollbackInfo;
        this.addToHistory(batch);
      }

      const result: OperationResult = {
        success: errors.length === 0,
        operationsExecuted,
        errors,
        warnings,
        executionTime: Date.now() - startTime,
        rollbackInfo: errors.length === 0 ? rollbackInfo : undefined,
      };

      this.emit('batch_executed', { batch, result });
      return result;

    } catch (error) {
      this.emit('batch_error', { batch, error });
      throw error;
    }
  }

  /**
   * Executes a single atomic operation.
   */
  private async executeAtomicOperation(operation: AtomicOperation): Promise<void> {
    const content = this.fileContents.get(operation.file);
    if (!content) {
      throw new Error(`File not loaded: ${operation.file}`);
    }

    let newContent: string;

    switch (operation.type) {
      case AtomicOperationType.INSERT_TEXT:
        if (!operation.text) {
          throw new Error('Insert operation requires text');
        }
        newContent = TextManipulator.insertText(content, operation.position, operation.text);
        break;

      case AtomicOperationType.DELETE_TEXT:
        if (!operation.endPosition) {
          throw new Error('Delete operation requires end position');
        }
        newContent = TextManipulator.deleteText(content, operation.position, operation.endPosition);
        break;

      case AtomicOperationType.REPLACE_TEXT:
        if (!operation.endPosition || !operation.newText) {
          throw new Error('Replace operation requires end position and new text');
        }
        newContent = TextManipulator.replaceText(
          content,
          operation.position,
          operation.endPosition,
          operation.newText,
        );
        break;

      case AtomicOperationType.MOVE_TEXT:
        if (!operation.endPosition || !operation.metadata?.targetPosition) {
          throw new Error('Move operation requires end position and target position');
        }
        newContent = TextManipulator.moveText(
          content,
          operation.position,
          operation.endPosition,
          operation.metadata.targetPosition,
        );
        break;

      case AtomicOperationType.INSERT_LINE:
        newContent = TextManipulator.insertLine(content, operation.position, operation.text || '');
        break;

      case AtomicOperationType.DELETE_LINE:
        newContent = TextManipulator.deleteLine(content, operation.position.line);
        break;

      case AtomicOperationType.INDENT_BLOCK: {
        if (!operation.endPosition || operation.indentLevel === undefined) {
          throw new Error('Indent operation requires end position and indent level');
        }
        const indentString = '  '.repeat(operation.indentLevel);
        newContent = TextManipulator.indentBlock(
          content,
          operation.position.line,
          operation.endPosition.line,
          indentString,
        );
        break;
      }

      case AtomicOperationType.UNINDENT_BLOCK: {
        if (!operation.endPosition || operation.indentLevel === undefined) {
          throw new Error('Unindent operation requires end position and indent level');
        }
        const unindentString = '  '.repeat(operation.indentLevel);
        newContent = TextManipulator.unindentBlock(
          content,
          operation.position.line,
          operation.endPosition.line,
          unindentString,
        );
        break;
      }

      case AtomicOperationType.WRAP_BLOCK:
        if (!operation.endPosition || !operation.wrapperStart || !operation.wrapperEnd) {
          throw new Error('Wrap operation requires end position and wrapper strings');
        }
        newContent = TextManipulator.wrapBlock(
          content,
          operation.position,
          operation.endPosition,
          operation.wrapperStart,
          operation.wrapperEnd,
        );
        break;

      case AtomicOperationType.UNWRAP_BLOCK:
        if (!operation.endPosition || !operation.wrapperStart || !operation.wrapperEnd) {
          throw new Error('Unwrap operation requires end position and wrapper strings');
        }
        newContent = TextManipulator.unwrapBlock(
          content,
          operation.position,
          operation.endPosition,
          operation.wrapperStart,
          operation.wrapperEnd,
        );
        break;

      default:
        throw new Error(`Unsupported operation type: ${operation.type}`);
    }

    // Update file content
    this.fileContents.set(operation.file, newContent);

    this.emit('operation_executed', { operation, oldContent: content, newContent });
  }

  /**
   * Creates rollback information for a batch.
   */
  private createRollbackInfo(batch: OperationBatch): RollbackInfo {
    const originalState: FileState[] = [];
    const affectedFiles = new Set<string>();

    // Collect affected files
    for (const operation of batch.operations) {
      affectedFiles.add(operation.file);
    }

    // Create file state snapshots
    for (const file of affectedFiles) {
      const content = this.fileContents.get(file);
      if (content) {
        originalState.push({
          file,
          content,
          checksum: this.calculateChecksum(content),
          timestamp: Date.now(),
        });
      }
    }

    // Generate reverse operations
    const reverseOperations = this.generateReverseOperations(batch.operations);

    return {
      batchId: batch.id,
      reverseOperations,
      originalState,
      timestamp: Date.now(),
    };
  }

  /**
   * Generates reverse operations for rollback.
   */
  private generateReverseOperations(operations: AtomicOperation[]): AtomicOperation[] {
    const reverseOps: AtomicOperation[] = [];

    // Process operations in reverse order
    for (let i = operations.length - 1; i >= 0; i--) {
      const op = operations[i];
      const reverseOp = this.createReverseOperation(op);
      if (reverseOp) {
        reverseOps.push(reverseOp);
      }
    }

    return reverseOps;
  }

  /**
   * Creates a reverse operation for rollback.
   */
  private createReverseOperation(operation: AtomicOperation): AtomicOperation | null {
    switch (operation.type) {
      case AtomicOperationType.INSERT_TEXT:
        return {
          id: this.generateOperationId(),
          type: AtomicOperationType.DELETE_TEXT,
          file: operation.file,
          position: operation.position,
          endPosition: {
            line: operation.position.line,
            column: operation.position.column + (operation.text?.length || 0),
            offset: operation.position.offset + (operation.text?.length || 0),
          },
          metadata: { reverseOf: operation.id },
        };

      case AtomicOperationType.DELETE_TEXT:
        // Would need to store the deleted text to reverse this
        return null; // Simplified for this implementation

      case AtomicOperationType.REPLACE_TEXT:
        // Would need to store the original text to reverse this
        return null; // Simplified for this implementation

      default:
        return null;
    }
  }

  /**
   * Rolls back a batch using rollback information.
   */
  private async rollbackBatch(rollbackInfo: RollbackInfo): Promise<void> {
    try {
      // Restore original file states
      for (const fileState of rollbackInfo.originalState) {
        this.fileContents.set(fileState.file, fileState.content);
      }

      this.emit('batch_rolled_back', { rollbackInfo });

    } catch (error) {
      this.emit('rollback_error', { rollbackInfo, error });
      throw error;
    }
  }

  /**
   * Converts high-level code changes to atomic operations.
   */
  public convertToAtomicOperations(changes: CodeChange[]): AtomicOperation[] {
    const operations: AtomicOperation[] = [];

    for (const change of changes) {
      const atomicOps = this.convertChangeToAtomicOperations(change);
      operations.push(...atomicOps);
    }

    return operations;
  }

  /**
   * Converts a single code change to atomic operations.
   */
  private convertChangeToAtomicOperations(change: CodeChange): AtomicOperation[] {
    const operations: AtomicOperation[] = [];

    switch (change.type) {
      case ChangeOperationType.INSERT:
        operations.push({
          id: this.generateOperationId(),
          type: AtomicOperationType.INSERT_TEXT,
          file: change.file,
          position: change.position,
          text: change.newText || '',
          metadata: { changeId: change.id },
        });
        break;

      case ChangeOperationType.DELETE:
        operations.push({
          id: this.generateOperationId(),
          type: AtomicOperationType.DELETE_TEXT,
          file: change.file,
          position: change.position,
          endPosition: change.endPosition,
          metadata: { changeId: change.id },
        });
        break;

      case ChangeOperationType.REPLACE:
        operations.push({
          id: this.generateOperationId(),
          type: AtomicOperationType.REPLACE_TEXT,
          file: change.file,
          position: change.position,
          endPosition: change.endPosition,
          newText: change.newText || '',
          metadata: { changeId: change.id },
        });
        break;

      case ChangeOperationType.MOVE:
        if (change.metadata?.targetPosition) {
          operations.push({
            id: this.generateOperationId(),
            type: AtomicOperationType.MOVE_TEXT,
            file: change.file,
            position: change.position,
            endPosition: change.endPosition,
            metadata: {
              changeId: change.id,
              targetPosition: change.metadata.targetPosition,
            },
          });
        }
        break;
    }

    return operations;
  }

  /**
   * Creates an operation batch from atomic operations.
   */
  public createBatch(
    operations: AtomicOperation[],
    description: string,
  ): OperationBatch {
    return {
      id: this.generateBatchId(),
      operations,
      description,
      timestamp: Date.now(),
    };
  }

  /**
   * Gets operation history.
   */
  public getHistory(): OperationBatch[] {
    return [...this.operationHistory];
  }

  /**
   * Clears operation history.
   */
  public clearHistory(): void {
    this.operationHistory = [];
    this.emit('history_cleared');
  }

  /**
   * Helper methods.
   */

  private addToHistory(batch: OperationBatch): void {
    this.operationHistory.push(batch);

    // Limit history size
    if (this.operationHistory.length > this.maxHistorySize) {
      this.operationHistory.shift();
    }
  }

  private calculateChecksum(content: string): string {
    // Simple checksum calculation - in practice, use a proper hash function
    let hash = 0;
    for (let i = 0; i < content.length; i++) {
      const char = content.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash.toString(16);
  }

  private generateOperationId(): string {
    return `op_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private generateBatchId(): string {
    return `batch_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

// Export classes and interfaces
export {
  TextManipulator,
};

// Export the main code operations class as default
export default CodeOperations;

