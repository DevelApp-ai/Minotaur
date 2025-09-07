/**
 * Real-time refactoring engine with grammar-level precision for Minotaur.
 * Provides surgical code modifications using grammar inheritance and context awareness.
 */

import { ContextManager } from '../context/ContextManager';
import { ContextInfo, CodePosition, SymbolInfo, ScopeInfo } from '../context/ContextAwareParser';
import { Grammar, GrammarFormatType } from '../utils/Grammar';
import { Interpreter } from '../utils/Interpreter';
import { EventEmitter } from 'events';

/**
 * Code change operation types.
 */
export enum ChangeOperationType {
  INSERT = 'insert',
  DELETE = 'delete',
  REPLACE = 'replace',
  MOVE = 'move',
  REORDER = 'reorder'
}

/**
 * Code change operation.
 */
export interface CodeChange {
  id: string;
  type: ChangeOperationType;
  file: string;
  position: CodePosition;
  endPosition?: CodePosition;
  oldText?: string;
  newText?: string;
  description: string;
  grammarRule?: string;
  confidence: number;
  dependencies: string[];
  metadata: any;
}

/**
 * Refactoring operation result.
 */
export interface RefactoringResult {
  success: boolean;
  changes: CodeChange[];
  warnings: RefactoringWarning[];
  errors: RefactoringError[];
  preview?: string;
  undoInfo?: UndoInformation;
  metrics: RefactoringMetrics;
}

/**
 * Refactoring warning.
 */
export interface RefactoringWarning {
  message: string;
  position: CodePosition;
  severity: 'low' | 'medium' | 'high';
  suggestion?: string;
}

/**
 * Refactoring error.
 */
export interface RefactoringError {
  message: string;
  position: CodePosition;
  code: string;
  category: 'syntax' | 'semantic' | 'grammar' | 'context';
}

/**
 * Undo information for refactoring operations.
 */
export interface UndoInformation {
  operationId: string;
  timestamp: number;
  changes: CodeChange[];
  contextSnapshot: ContextInfo;
  description: string;
}

/**
 * Refactoring metrics.
 */
export interface RefactoringMetrics {
  operationTime: number;
  changesCount: number;
  linesAffected: number;
  symbolsAffected: number;
  complexityReduction: number;
  confidenceScore: number;
}

/**
 * Refactoring operation configuration.
 */
export interface RefactoringConfig {
  enablePreview: boolean;
  enableUndo: boolean;
  enableValidation: boolean;
  maxChanges: number;
  confidenceThreshold: number;
  enableMetrics: boolean;
  preserveComments: boolean;
  preserveFormatting: boolean;
}

/**
 * Real-time refactoring engine with grammar-level precision.
 */
export class RefactoringEngine extends EventEmitter {
  private contextManager: ContextManager;
  private interpreter: Interpreter;
  private config: RefactoringConfig;
  private undoStack: UndoInformation[];
  private redoStack: UndoInformation[];
  private activeOperations: Map<string, Promise<RefactoringResult>>;

  constructor(contextManager: ContextManager, config: Partial<RefactoringConfig> = {}) {
    super();

    this.contextManager = contextManager;
    this.interpreter = contextManager['interpreter']; // Access private interpreter
    this.config = {
      enablePreview: true,
      enableUndo: true,
      enableValidation: true,
      maxChanges: 1000,
      confidenceThreshold: 0.7,
      enableMetrics: true,
      preserveComments: true,
      preserveFormatting: true,
      ...config,
    };

    this.undoStack = [];
    this.redoStack = [];
    this.activeOperations = new Map();

    this.setupEventHandlers();
  }

  /**
   * Sets up event handlers for context manager.
   */
  private setupEventHandlers(): void {
    this.contextManager.on('context_changed', this.handleContextChanged.bind(this));
    this.contextManager.on('file_updated', this.handleFileUpdated.bind(this));
  }

  /**
   * Extracts a variable from an expression.
   */
  public async extractVariable(
    file: string,
    position: CodePosition,
    endPosition: CodePosition,
    variableName: string,
    options: any = {},
  ): Promise<RefactoringResult> {
    const operationId = this.generateOperationId('extract_variable');

    try {
      const startTime = Date.now();

      // Get context information
      const context = this.contextManager.getContextAt(file, position);
      if (!context) {
        throw new Error('No context available for refactoring');
      }

      // Analyze the expression to extract
      const expressionAnalysis = await this.analyzeExpression(file, position, endPosition, context);

      // Validate the extraction
      const validation = await this.validateVariableExtraction(
        expressionAnalysis,
        variableName,
        context,
        options,
      );

      if (!validation.valid) {
        return this.createErrorResult(operationId, validation.errors, startTime);
      }

      // Generate the changes
      const changes = await this.generateVariableExtractionChanges(
        expressionAnalysis,
        variableName,
        context,
        options,
      );

      // Create result
      const result = await this.createRefactoringResult(
        operationId,
        changes,
        validation.warnings,
        [],
        startTime,
        context,
      );

      this.emit('refactoring_completed', { operation: 'extract_variable', result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { operation: 'extract_variable', error });
      throw error;
    }
  }

  /**
   * Inlines a variable by replacing all references with its value.
   */
  public async inlineVariable(
    file: string,
    position: CodePosition,
    variableName: string,
    options: any = {},
  ): Promise<RefactoringResult> {
    const operationId = this.generateOperationId('inline_variable');

    try {
      const startTime = Date.now();

      // Get context information
      const context = this.contextManager.getContextAt(file, position);
      if (!context) {
        throw new Error('No context available for refactoring');
      }

      // Find the variable symbol
      const symbol = this.contextManager.resolveSymbol(file, position, variableName);
      if (!symbol) {
        throw new Error(`Variable '${variableName}' not found`);
      }

      // Analyze the variable for inlining
      const variableAnalysis = await this.analyzeVariableForInlining(symbol, context);

      // Validate the inlining
      const validation = await this.validateVariableInlining(variableAnalysis, context, options);

      if (!validation.valid) {
        return this.createErrorResult(operationId, validation.errors, startTime);
      }

      // Generate the changes
      const changes = await this.generateVariableInliningChanges(
        variableAnalysis,
        context,
        options,
      );

      // Create result
      const result = await this.createRefactoringResult(
        operationId,
        changes,
        validation.warnings,
        [],
        startTime,
        context,
      );

      this.emit('refactoring_completed', { operation: 'inline_variable', result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { operation: 'inline_variable', error });
      throw error;
    }
  }

  /**
   * Renames a symbol across all references.
   */
  public async renameSymbol(
    file: string,
    position: CodePosition,
    oldName: string,
    newName: string,
    options: any = {},
  ): Promise<RefactoringResult> {
    const operationId = this.generateOperationId('rename_symbol');

    try {
      const startTime = Date.now();

      // Get context information
      const context = this.contextManager.getContextAt(file, position);
      if (!context) {
        throw new Error('No context available for refactoring');
      }

      // Find the symbol to rename
      const symbol = this.contextManager.resolveSymbol(file, position, oldName);
      if (!symbol) {
        throw new Error(`Symbol '${oldName}' not found`);
      }

      // Find all references to the symbol
      const references = await this.findAllReferences(symbol, context);

      // Validate the rename
      const validation = await this.validateSymbolRename(
        symbol,
        newName,
        references,
        context,
        options,
      );

      if (!validation.valid) {
        return this.createErrorResult(operationId, validation.errors, startTime);
      }

      // Generate the changes
      const changes = await this.generateSymbolRenameChanges(
        symbol,
        newName,
        references,
        context,
        options,
      );

      // Create result
      const result = await this.createRefactoringResult(
        operationId,
        changes,
        validation.warnings,
        [],
        startTime,
        context,
      );

      this.emit('refactoring_completed', { operation: 'rename_symbol', result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { operation: 'rename_symbol', error });
      throw error;
    }
  }

  /**
   * Extracts a function from a code block.
   */
  public async extractFunction(
    file: string,
    startPosition: CodePosition,
    endPosition: CodePosition,
    functionName: string,
    options: any = {},
  ): Promise<RefactoringResult> {
    const operationId = this.generateOperationId('extract_function');

    try {
      const startTime = Date.now();

      // Get context information
      const context = this.contextManager.getContextAt(file, startPosition);
      if (!context) {
        throw new Error('No context available for refactoring');
      }

      // Analyze the code block to extract
      const blockAnalysis = await this.analyzeCodeBlock(file, startPosition, endPosition, context);

      // Validate the extraction
      const validation = await this.validateFunctionExtraction(
        blockAnalysis,
        functionName,
        context,
        options,
      );

      if (!validation.valid) {
        return this.createErrorResult(operationId, validation.errors, startTime);
      }

      // Generate the changes
      const changes = await this.generateFunctionExtractionChanges(
        blockAnalysis,
        functionName,
        context,
        options,
      );

      // Create result
      const result = await this.createRefactoringResult(
        operationId,
        changes,
        validation.warnings,
        [],
        startTime,
        context,
      );

      this.emit('refactoring_completed', { operation: 'extract_function', result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { operation: 'extract_function', error });
      throw error;
    }
  }

  /**
   * Inlines a function by replacing calls with the function body.
   */
  public async inlineFunction(
    file: string,
    position: CodePosition,
    functionName: string,
    options: any = {},
  ): Promise<RefactoringResult> {
    const operationId = this.generateOperationId('inline_function');

    try {
      const startTime = Date.now();

      // Get context information
      const context = this.contextManager.getContextAt(file, position);
      if (!context) {
        throw new Error('No context available for refactoring');
      }

      // Find the function symbol
      const symbol = this.contextManager.resolveSymbol(file, position, functionName);
      if (!symbol) {
        throw new Error(`Function '${functionName}' not found`);
      }

      // Analyze the function for inlining
      const functionAnalysis = await this.analyzeFunctionForInlining(symbol, context);

      // Validate the inlining
      const validation = await this.validateFunctionInlining(functionAnalysis, context, options);

      if (!validation.valid) {
        return this.createErrorResult(operationId, validation.errors, startTime);
      }

      // Generate the changes
      const changes = await this.generateFunctionInliningChanges(
        functionAnalysis,
        context,
        options,
      );

      // Create result
      const result = await this.createRefactoringResult(
        operationId,
        changes,
        validation.warnings,
        [],
        startTime,
        context,
      );

      this.emit('refactoring_completed', { operation: 'inline_function', result });
      return result;

    } catch (error) {
      this.emit('refactoring_error', { operation: 'inline_function', error });
      throw error;
    }
  }

  /**
   * Applies a set of code changes to files.
   */
  public async applyChanges(changes: CodeChange[]): Promise<boolean> {
    try {
      // Validate changes
      const validation = await this.validateChanges(changes);
      if (!validation.valid) {
        throw new Error(`Invalid changes: ${validation.errors.join(', ')}`);
      }

      // Sort changes by position (reverse order for safe application)
      const sortedChanges = this.sortChangesForApplication(changes);

      // Apply changes one by one
      for (const change of sortedChanges) {
        await this.applyChange(change);
      }

      this.emit('changes_applied', { changes });
      return true;

    } catch (error) {
      this.emit('apply_error', { changes, error });
      throw error;
    }
  }

  /**
   * Undoes the last refactoring operation.
   */
  public async undo(): Promise<boolean> {
    if (this.undoStack.length === 0) {
      return false;
    }

    try {
      const undoInfo = this.undoStack.pop()!;

      // Apply reverse changes
      const reverseChanges = this.generateReverseChanges(undoInfo.changes);
      await this.applyChanges(reverseChanges);

      // Move to redo stack
      this.redoStack.push(undoInfo);

      this.emit('operation_undone', { undoInfo });
      return true;

    } catch (error) {
      this.emit('undo_error', { error });
      throw error;
    }
  }

  /**
   * Redoes the last undone refactoring operation.
   */
  public async redo(): Promise<boolean> {
    if (this.redoStack.length === 0) {
      return false;
    }

    try {
      const redoInfo = this.redoStack.pop()!;

      // Apply original changes
      await this.applyChanges(redoInfo.changes);

      // Move back to undo stack
      this.undoStack.push(redoInfo);

      this.emit('operation_redone', { redoInfo });
      return true;

    } catch (error) {
      this.emit('redo_error', { error });
      throw error;
    }
  }

  /**
   * Analysis methods for different refactoring operations.
   */

  private async analyzeExpression(
    file: string,
    startPos: CodePosition,
    endPos: CodePosition,
    context: ContextInfo,
  ): Promise<any> {
    // Mock implementation - would integrate with actual grammar parsing
    return {
      file,
      startPosition: startPos,
      endPosition: endPos,
      text: 'expression_text',
      type: 'expression',
      dependencies: [],
      complexity: 1,
      sideEffects: false,
      context,
    };
  }

  private async analyzeVariableForInlining(symbol: SymbolInfo, context: ContextInfo): Promise<any> {
    return {
      symbol,
      definition: symbol.definition,
      references: symbol.references,
      value: 'variable_value',
      canInline: true,
      sideEffects: false,
      context,
    };
  }

  private async analyzeCodeBlock(
    file: string,
    startPos: CodePosition,
    endPos: CodePosition,
    context: ContextInfo,
  ): Promise<any> {
    return {
      file,
      startPosition: startPos,
      endPosition: endPos,
      text: 'code_block_text',
      variables: [],
      parameters: [],
      returnValue: null,
      sideEffects: false,
      context,
    };
  }

  private async analyzeFunctionForInlining(symbol: SymbolInfo, context: ContextInfo): Promise<any> {
    return {
      symbol,
      body: 'function_body',
      parameters: [],
      returnType: 'void',
      callSites: symbol.references,
      canInline: true,
      context,
    };
  }

  /**
   * Validation methods for refactoring operations.
   */

  private async validateVariableExtraction(
    analysis: any,
    variableName: string,
    context: ContextInfo,
    options: any,
  ): Promise<{ valid: boolean; errors: RefactoringError[]; warnings: RefactoringWarning[] }> {
    const errors: RefactoringError[] = [];
    const warnings: RefactoringWarning[] = [];

    // Check if variable name is valid
    if (!this.isValidIdentifier(variableName)) {
      errors.push({
        message: `Invalid variable name: ${variableName}`,
        position: analysis.startPosition,
        code: 'INVALID_IDENTIFIER',
        category: 'syntax',
      });
    }

    // Check for name conflicts
    const existingSymbol = this.contextManager.resolveSymbol(
      analysis.file,
      analysis.startPosition,
      variableName,
    );

    if (existingSymbol) {
      errors.push({
        message: `Variable name '${variableName}' already exists in scope`,
        position: analysis.startPosition,
        code: 'NAME_CONFLICT',
        category: 'semantic',
      });
    }

    return { valid: errors.length === 0, errors, warnings };
  }

  private async validateVariableInlining(
    analysis: any,
    context: ContextInfo,
    options: any,
  ): Promise<{ valid: boolean; errors: RefactoringError[]; warnings: RefactoringWarning[] }> {
    const errors: RefactoringError[] = [];
    const warnings: RefactoringWarning[] = [];

    // Check if variable can be safely inlined
    if (analysis.sideEffects) {
      errors.push({
        message: 'Cannot inline variable with side effects',
        position: analysis.symbol.position,
        code: 'SIDE_EFFECTS',
        category: 'semantic',
      });
    }

    return { valid: errors.length === 0, errors, warnings };
  }

  private async validateSymbolRename(
    symbol: SymbolInfo,
    newName: string,
    references: CodePosition[],
    context: ContextInfo,
    options: any,
  ): Promise<{ valid: boolean; errors: RefactoringError[]; warnings: RefactoringWarning[] }> {
    const errors: RefactoringError[] = [];
    const warnings: RefactoringWarning[] = [];

    // Check if new name is valid
    if (!this.isValidIdentifier(newName)) {
      errors.push({
        message: `Invalid identifier: ${newName}`,
        position: symbol.position,
        code: 'INVALID_IDENTIFIER',
        category: 'syntax',
      });
    }

    return { valid: errors.length === 0, errors, warnings };
  }

  private async validateFunctionExtraction(
    analysis: any,
    functionName: string,
    context: ContextInfo,
    options: any,
  ): Promise<{ valid: boolean; errors: RefactoringError[]; warnings: RefactoringWarning[] }> {
    const errors: RefactoringError[] = [];
    const warnings: RefactoringWarning[] = [];

    // Check if function name is valid
    if (!this.isValidIdentifier(functionName)) {
      errors.push({
        message: `Invalid function name: ${functionName}`,
        position: analysis.startPosition,
        code: 'INVALID_IDENTIFIER',
        category: 'syntax',
      });
    }

    return { valid: errors.length === 0, errors, warnings };
  }

  private async validateFunctionInlining(
    analysis: any,
    context: ContextInfo,
    options: any,
  ): Promise<{ valid: boolean; errors: RefactoringError[]; warnings: RefactoringWarning[] }> {
    const errors: RefactoringError[] = [];
    const warnings: RefactoringWarning[] = [];

    // Check if function can be safely inlined
    if (!analysis.canInline) {
      errors.push({
        message: 'Function cannot be safely inlined',
        position: analysis.symbol.position,
        code: 'CANNOT_INLINE',
        category: 'semantic',
      });
    }

    return { valid: errors.length === 0, errors, warnings };
  }

  /**
   * Change generation methods.
   */

  private async generateVariableExtractionChanges(
    analysis: any,
    variableName: string,
    context: ContextInfo,
    options: any,
  ): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    // Insert variable declaration
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.INSERT,
      file: analysis.file,
      position: this.findInsertionPoint(analysis.startPosition, context),
      newText: `const ${variableName} = ${analysis.text};\n`,
      description: `Extract variable ${variableName}`,
      confidence: 0.9,
      dependencies: [],
      metadata: { operation: 'extract_variable', variableName },
    });

    // Replace original expression
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.REPLACE,
      file: analysis.file,
      position: analysis.startPosition,
      endPosition: analysis.endPosition,
      oldText: analysis.text,
      newText: variableName,
      description: `Replace expression with ${variableName}`,
      confidence: 0.95,
      dependencies: [changes[0].id],
      metadata: { operation: 'extract_variable', variableName },
    });

    return changes;
  }

  private async generateVariableInliningChanges(
    analysis: any,
    context: ContextInfo,
    options: any,
  ): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    // Replace all references with the variable value
    for (const reference of analysis.references) {
      changes.push({
        id: this.generateChangeId(),
        type: ChangeOperationType.REPLACE,
        file: context.parseState.position.file || 'unknown',
        position: reference,
        oldText: analysis.symbol.name,
        newText: analysis.value,
        description: `Inline variable ${analysis.symbol.name}`,
        confidence: 0.9,
        dependencies: [],
        metadata: { operation: 'inline_variable', variableName: analysis.symbol.name },
      });
    }

    // Remove variable declaration
    if (analysis.definition) {
      changes.push({
        id: this.generateChangeId(),
        type: ChangeOperationType.DELETE,
        file: context.parseState.position.file || 'unknown',
        position: analysis.definition,
        oldText: `const ${analysis.symbol.name} = ${analysis.value};`,
        description: `Remove variable declaration ${analysis.symbol.name}`,
        confidence: 0.95,
        dependencies: changes.map(c => c.id),
        metadata: { operation: 'inline_variable', variableName: analysis.symbol.name },
      });
    }

    return changes;
  }

  private async generateSymbolRenameChanges(
    symbol: SymbolInfo,
    newName: string,
    references: CodePosition[],
    context: ContextInfo,
    options: any,
  ): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    // Rename the symbol definition
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.REPLACE,
      file: context.parseState.position.file || 'unknown',
      position: symbol.position,
      oldText: symbol.name,
      newText: newName,
      description: `Rename ${symbol.name} to ${newName}`,
      confidence: 0.95,
      dependencies: [],
      metadata: { operation: 'rename_symbol', oldName: symbol.name, newName },
    });

    // Rename all references
    for (const reference of references) {
      changes.push({
        id: this.generateChangeId(),
        type: ChangeOperationType.REPLACE,
        file: context.parseState.position.file || 'unknown',
        position: reference,
        oldText: symbol.name,
        newText: newName,
        description: `Rename reference ${symbol.name} to ${newName}`,
        confidence: 0.9,
        dependencies: [changes[0].id],
        metadata: { operation: 'rename_symbol', oldName: symbol.name, newName },
      });
    }

    return changes;
  }

  private async generateFunctionExtractionChanges(
    analysis: any,
    functionName: string,
    context: ContextInfo,
    options: any,
  ): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    // Insert function definition
    const functionText = this.generateFunctionDefinition(functionName, analysis);
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.INSERT,
      file: analysis.file,
      position: this.findFunctionInsertionPoint(analysis.startPosition, context),
      newText: functionText,
      description: `Extract function ${functionName}`,
      confidence: 0.85,
      dependencies: [],
      metadata: { operation: 'extract_function', functionName },
    });

    // Replace code block with function call
    const callText = this.generateFunctionCall(functionName, analysis);
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.REPLACE,
      file: analysis.file,
      position: analysis.startPosition,
      endPosition: analysis.endPosition,
      oldText: analysis.text,
      newText: callText,
      description: `Replace code block with ${functionName}()`,
      confidence: 0.9,
      dependencies: [changes[0].id],
      metadata: { operation: 'extract_function', functionName },
    });

    return changes;
  }

  private async generateFunctionInliningChanges(
    analysis: any,
    context: ContextInfo,
    options: any,
  ): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    // Replace function calls with function body
    for (const callSite of analysis.callSites) {
      const inlinedBody = this.generateInlinedFunctionBody(analysis, callSite);
      changes.push({
        id: this.generateChangeId(),
        type: ChangeOperationType.REPLACE,
        file: context.parseState.position.file || 'unknown',
        position: callSite,
        oldText: `${analysis.symbol.name}()`,
        newText: inlinedBody,
        description: `Inline function ${analysis.symbol.name}`,
        confidence: 0.8,
        dependencies: [],
        metadata: { operation: 'inline_function', functionName: analysis.symbol.name },
      });
    }

    // Remove function definition
    changes.push({
      id: this.generateChangeId(),
      type: ChangeOperationType.DELETE,
      file: context.parseState.position.file || 'unknown',
      position: analysis.symbol.position,
      oldText: analysis.body,
      description: `Remove function definition ${analysis.symbol.name}`,
      confidence: 0.9,
      dependencies: changes.slice(0, -1).map(c => c.id),
      metadata: { operation: 'inline_function', functionName: analysis.symbol.name },
    });

    return changes;
  }

  /**
   * Helper methods.
   */

  private async findAllReferences(symbol: SymbolInfo, context: ContextInfo): Promise<CodePosition[]> {
    // In practice, this would use the symbol table to find all references
    return symbol.references;
  }

  private isValidIdentifier(name: string): boolean {
    return /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(name);
  }

  private findInsertionPoint(position: CodePosition, context: ContextInfo): CodePosition {
    // Find appropriate insertion point for variable declaration
    return { line: position.line, column: 1, offset: 0 };
  }

  private findFunctionInsertionPoint(position: CodePosition, context: ContextInfo): CodePosition {
    // Find appropriate insertion point for function definition
    return { line: Math.max(1, position.line - 5), column: 1, offset: 0 };
  }

  private generateFunctionDefinition(name: string, analysis: any): string {
    const params = analysis.parameters.map((p: any) => p.name).join(', ');
    return `function ${name}(${params}) {\n${analysis.text}\n}\n\n`;
  }

  private generateFunctionCall(name: string, analysis: any): string {
    const args = analysis.parameters.map((p: any) => p.name).join(', ');
    return `${name}(${args})`;
  }

  private generateInlinedFunctionBody(analysis: any, callSite: CodePosition): string {
    // Generate inlined function body with parameter substitution
    return `{\n${analysis.body}\n}`;
  }

  private async validateChanges(changes: CodeChange[]): Promise<{ valid: boolean; errors: string[] }> {
    const errors: string[] = [];

    // Basic validation
    for (const change of changes) {
      if (!change.file || !change.position) {
        errors.push('Invalid change: missing file or position');
      }
    }

    return { valid: errors.length === 0, errors };
  }

  private sortChangesForApplication(changes: CodeChange[]): CodeChange[] {
    // Sort changes by position (reverse order for safe application)
    return changes.sort((a, b) => {
      if (a.position.line !== b.position.line) {
        return b.position.line - a.position.line;
      }
      return b.position.column - a.position.column;
    });
  }

  private async applyChange(change: CodeChange): Promise<void> {
    // Mock implementation - would integrate with actual file system
    this.emit('change_applied', { change });
  }

  private generateReverseChanges(changes: CodeChange[]): CodeChange[] {
    return changes.map(change => ({
      ...change,
      id: this.generateChangeId(),
      type: this.getReverseOperationType(change.type),
      oldText: change.newText,
      newText: change.oldText,
      description: `Undo: ${change.description}`,
    }));
  }

  private getReverseOperationType(type: ChangeOperationType): ChangeOperationType {
    switch (type) {
      case ChangeOperationType.INSERT:
        return ChangeOperationType.DELETE;
      case ChangeOperationType.DELETE:
        return ChangeOperationType.INSERT;
      case ChangeOperationType.REPLACE:
        return ChangeOperationType.REPLACE;
      default:
        return type;
    }
  }

  private async createRefactoringResult(
    operationId: string,
    changes: CodeChange[],
    warnings: RefactoringWarning[],
    errors: RefactoringError[],
    startTime: number,
    context: ContextInfo,
  ): Promise<RefactoringResult> {
    const operationTime = Date.now() - startTime;

    const metrics: RefactoringMetrics = {
      operationTime,
      changesCount: changes.length,
      linesAffected: this.calculateLinesAffected(changes),
      symbolsAffected: this.calculateSymbolsAffected(changes),
      complexityReduction: 0, // Would be calculated based on actual analysis
      confidenceScore: this.calculateConfidenceScore(changes),
    };

    const result: RefactoringResult = {
      success: errors.length === 0,
      changes,
      warnings,
      errors,
      metrics,
    };

    // Add preview if enabled
    if (this.config.enablePreview) {
      result.preview = await this.generatePreview(changes);
    }

    // Add undo information if enabled
    if (this.config.enableUndo && result.success) {
      const undoInfo: UndoInformation = {
        operationId,
        timestamp: Date.now(),
        changes,
        contextSnapshot: context,
        description: `Refactoring operation ${operationId}`,
      };

      this.undoStack.push(undoInfo);
      result.undoInfo = undoInfo;

      // Clear redo stack
      this.redoStack = [];
    }

    return result;
  }

  private createErrorResult(
    operationId: string,
    errors: RefactoringError[],
    startTime: number,
  ): RefactoringResult {
    return {
      success: false,
      changes: [],
      warnings: [],
      errors,
      metrics: {
        operationTime: Date.now() - startTime,
        changesCount: 0,
        linesAffected: 0,
        symbolsAffected: 0,
        complexityReduction: 0,
        confidenceScore: 0,
      },
    };
  }

  private calculateLinesAffected(changes: CodeChange[]): number {
    const lines = new Set<string>();
    for (const change of changes) {
      const key = `${change.file}:${change.position.line}`;
      lines.add(key);
      if (change.endPosition) {
        for (let line = change.position.line; line <= change.endPosition.line; line++) {
          lines.add(`${change.file}:${line}`);
        }
      }
    }
    return lines.size;
  }

  private calculateSymbolsAffected(changes: CodeChange[]): number {
    const symbols = new Set<string>();
    for (const change of changes) {
      if (change.metadata?.variableName) {
        symbols.add(change.metadata.variableName);
      }
      if (change.metadata?.functionName) {
        symbols.add(change.metadata.functionName);
      }
    }
    return symbols.size;
  }

  private calculateConfidenceScore(changes: CodeChange[]): number {
    if (changes.length === 0) {
      return 0;
    }

    const totalConfidence = changes.reduce((sum, change) => sum + change.confidence, 0);
    return totalConfidence / changes.length;
  }

  private async generatePreview(changes: CodeChange[]): Promise<string> {
    // Generate a preview of the changes
    return changes.map(change =>
      `${change.type.toUpperCase()}: ${change.description} at ${change.position.line}:${change.position.column}`,
    ).join('\n');
  }

  private generateOperationId(operation: string): string {
    return `${operation}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private generateChangeId(): string {
    return `change_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Event handlers.
   */

  private handleContextChanged(event: any): void {
    // Invalidate any cached analysis when context changes
    this.emit('context_invalidated', event);
  }

  private handleFileUpdated(event: any): void {
    // Handle file updates that might affect ongoing refactoring operations
    this.emit('file_updated', event);
  }

  /**
   * Public API methods.
   */

  /**
   * Gets the undo stack.
   */
  public getUndoStack(): UndoInformation[] {
    return [...this.undoStack];
  }

  /**
   * Gets the redo stack.
   */
  public getRedoStack(): UndoInformation[] {
    return [...this.redoStack];
  }

  /**
   * Clears the undo and redo stacks.
   */
  public clearHistory(): void {
    this.undoStack = [];
    this.redoStack = [];
    this.emit('history_cleared');
  }

  /**
   * Gets active operations.
   */
  public getActiveOperations(): string[] {
    return Array.from(this.activeOperations.keys());
  }

  /**
   * Gets refactoring engine configuration.
   */
  public getConfig(): RefactoringConfig {
    return { ...this.config };
  }

  /**
   * Updates refactoring engine configuration.
   */
  public updateConfig(config: Partial<RefactoringConfig>): void {
    this.config = { ...this.config, ...config };
    this.emit('config_updated', { config: this.config });
  }
}

// Export the main refactoring engine class as default
export default RefactoringEngine;

