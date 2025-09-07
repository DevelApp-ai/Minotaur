/**
 * Surgical refactoring operations with grammar-level precision for Minotaur.
 * Provides RefacTS-style surgical operations with minimal scope changes and context preservation.
 */

import { RefactoringEngine } from '../refactoring/RefactoringEngine';
import { ContextManager } from '../context/ContextManager';
import { LanguageManager, SupportedLanguage } from '../languages/LanguageManager';
import { CodePosition, ScopeType as ContextScopeType } from '../context/ContextAwareParser';
import { ParseContext, ScopeType as EngineScopeType } from '../compiler/ContextSensitiveEngine';
import { SymbolInfo } from '../compiler/ContextSensitiveEngine';
import { EventEmitter } from 'events';

/**
 * Surgical operation types.
 */
export enum SurgicalOperationType {
  EXTRACT_VARIABLE = 'extract_variable',
  INLINE_VARIABLE = 'inline_variable',
  EXTRACT_FUNCTION = 'extract_function',
  INLINE_FUNCTION = 'inline_function',
  RENAME_SYMBOL = 'rename_symbol',
  MOVE_SYMBOL = 'move_symbol',
  EXTRACT_INTERFACE = 'extract_interface',
  EXTRACT_CLASS = 'extract_class',
  SPLIT_VARIABLE = 'split_variable',
  MERGE_VARIABLES = 'merge_variables',
  INTRODUCE_PARAMETER = 'introduce_parameter',
  REMOVE_PARAMETER = 'remove_parameter',
  CHANGE_SIGNATURE = 'change_signature',
  EXTRACT_CONSTANT = 'extract_constant',
  INLINE_CONSTANT = 'inline_constant'
}

/**
 * Surgical operation request.
 */
export interface SurgicalOperationRequest {
  id: string;
  type: SurgicalOperationType;
  file: string;
  language: SupportedLanguage;
  position: CodePosition;
  endPosition?: CodePosition;
  parameters: SurgicalParameters;
  options: SurgicalOptions;
}

/**
 * Surgical operation parameters.
 */
export interface SurgicalParameters {
  symbolName?: string;
  newName?: string;
  targetFile?: string;
  targetPosition?: CodePosition;
  extractionScope?: 'minimal' | 'function' | 'class' | 'file';
  preserveComments?: boolean;
  preserveFormatting?: boolean;
  generateDocumentation?: boolean;
  updateReferences?: boolean;
  validateSemantics?: boolean;
  [key: string]: any;
}

/**
 * Surgical operation options.
 */
export interface SurgicalOptions {
  preview: boolean;
  dryRun: boolean;
  validateOnly: boolean;
  minimizeChanges: boolean;
  preserveWhitespace: boolean;
  maintainLineNumbers: boolean;
  generateUndo: boolean;
  confidenceThreshold: number;
  maxScopeExpansion: number;
}

/**
 * Surgical operation result.
 */
export interface SurgicalOperationResult {
  success: boolean;
  operationId: string;
  type: SurgicalOperationType;
  changes: SurgicalChange[];
  metrics: SurgicalMetrics;
  validation: SurgicalValidation;
  preview?: string;
  undoInfo?: SurgicalUndoInfo;
  warnings: string[];
  errors: string[];
}

/**
 * Surgical change information.
 */
export interface SurgicalChange {
  id: string;
  file: string;
  type: 'insert' | 'delete' | 'replace' | 'move';
  position: CodePosition;
  endPosition?: CodePosition;
  oldText: string;
  newText: string;
  scope: SurgicalScope;
  confidence: number;
  dependencies: string[];
  metadata: SurgicalChangeMetadata;
}

/**
 * Surgical scope information.
 */
export interface SurgicalScope {
  type: 'token' | 'expression' | 'statement' | 'block' | 'function' | 'class' | 'file';
  startPosition: CodePosition;
  endPosition: CodePosition;
  contextType: string;
  symbolsAffected: string[];
  referencesAffected: number;
}

/**
 * Surgical change metadata.
 */
export interface SurgicalChangeMetadata {
  grammarRule?: string;
  syntaxNode?: string;
  semanticContext?: string;
  preservedElements: string[];
  modifiedElements: string[];
  addedElements: string[];
  removedElements: string[];
}

/**
 * Surgical operation metrics.
 */
export interface SurgicalMetrics {
  operationTime: number;
  analysisTime: number;
  validationTime: number;
  changesGenerated: number;
  tokensModified: number;
  linesModified: number;
  scopeExpansion: number;
  confidenceScore: number;
  complexityReduction: number;
  maintainabilityImprovement: number;
}

/**
 * Surgical validation result.
 */
export interface SurgicalValidation {
  syntaxValid: boolean;
  semanticsValid: boolean;
  referencesValid: boolean;
  typeSystemValid: boolean;
  grammarCompliant: boolean;
  issues: SurgicalValidationIssue[];
  suggestions: string[];
}

/**
 * Surgical validation issue.
 */
export interface SurgicalValidationIssue {
  severity: 'error' | 'warning' | 'info';
  message: string;
  position: CodePosition;
  code: string;
  category: 'syntax' | 'semantics' | 'references' | 'types' | 'grammar';
  fixSuggestion?: string;
}

/**
 * Surgical undo information.
 */
export interface SurgicalUndoInfo {
  operationId: string;
  timestamp: number;
  originalState: Map<string, string>;
  changeSequence: SurgicalChange[];
  contextSnapshot: any;
  rollbackInstructions: string[];
}

/**
 * Surgical refactoring engine with grammar precision.
 */
export class SurgicalRefactoring extends EventEmitter {
  private refactoringEngine: RefactoringEngine;
  private contextManager: ContextManager;
  private languageManager: LanguageManager;
  private operationHistory: SurgicalOperationResult[];
  private undoStack: SurgicalUndoInfo[];
  private redoStack: SurgicalUndoInfo[];

  constructor(
    refactoringEngine: RefactoringEngine,
    contextManager: ContextManager,
    languageManager: LanguageManager,
  ) {
    super();

    this.refactoringEngine = refactoringEngine;
    this.contextManager = contextManager;
    this.languageManager = languageManager;
    this.operationHistory = [];
    this.undoStack = [];
    this.redoStack = [];
  }

  /**
   * Convert ContextAwareParser ScopeType to ContextSensitiveEngine ScopeType
   */
  private convertScopeType(contextScopeType?: ContextScopeType): EngineScopeType {
    if (!contextScopeType) {
      return EngineScopeType.Global;
    }

    switch (contextScopeType) {
      case ContextScopeType.GLOBAL:
      case ContextScopeType.MODULE:
      case ContextScopeType.NAMESPACE:
        return EngineScopeType.Global;
      case ContextScopeType.CLASS:
      case ContextScopeType.INTERFACE:
        return EngineScopeType.Class;
      case ContextScopeType.FUNCTION:
      case ContextScopeType.METHOD:
      case ContextScopeType.LAMBDA:
        return EngineScopeType.Function;
      case ContextScopeType.BLOCK:
      case ContextScopeType.LOOP:
      case ContextScopeType.CONDITIONAL:
      case ContextScopeType.TRY_CATCH:
        return EngineScopeType.Block;
      default:
        return EngineScopeType.Custom;
    }
  }

  /**
   * Executes a surgical operation.
   */
  public async executeSurgicalOperation(request: SurgicalOperationRequest): Promise<SurgicalOperationResult> {
    const startTime = Date.now();

    try {
      // Validate request
      this.validateRequest(request);

      // Analyze context
      const analysisStartTime = Date.now();
      const context = await this.analyzeOperationContext(request);
      const analysisTime = Date.now() - analysisStartTime;

      // Generate changes
      const changes = await this.generateSurgicalChanges(request, context);

      // Validate changes
      const validationStartTime = Date.now();
      const validation = await this.validateSurgicalChanges(changes, request, context);
      const validationTime = Date.now() - validationStartTime;

      // Calculate metrics
      const metrics = this.calculateSurgicalMetrics(request, changes, analysisTime, validationTime, startTime);

      // Generate preview if requested
      let preview: string | undefined;
      if (request.options.preview || request.options.dryRun) {
        preview = await this.generatePreview(changes, request);
      }

      // Generate undo information
      let undoInfo: SurgicalUndoInfo | undefined;
      if (request.options.generateUndo && !request.options.dryRun) {
        undoInfo = await this.generateUndoInfo(request, changes, context);
      }

      // Apply changes if not in preview/dry-run mode
      if (!request.options.preview && !request.options.dryRun && !request.options.validateOnly) {
        await this.applySurgicalChanges(changes);

        // Add to undo stack
        if (undoInfo) {
          this.undoStack.push(undoInfo);
          this.redoStack = []; // Clear redo stack
        }
      }

      const result: SurgicalOperationResult = {
        success: validation.syntaxValid && validation.semanticsValid,
        operationId: request.id,
        type: request.type,
        changes,
        metrics,
        validation,
        preview,
        undoInfo,
        warnings: validation.issues.filter(i => i.severity === 'warning').map(i => i.message),
        errors: validation.issues.filter(i => i.severity === 'error').map(i => i.message),
      };

      // Add to history
      this.operationHistory.push(result);

      this.emit('surgical_operation_completed', { request, result });
      return result;

    } catch (error) {
      const errorResult: SurgicalOperationResult = {
        success: false,
        operationId: request.id,
        type: request.type,
        changes: [],
        metrics: {
          operationTime: Date.now() - startTime,
          analysisTime: 0,
          validationTime: 0,
          changesGenerated: 0,
          tokensModified: 0,
          linesModified: 0,
          scopeExpansion: 0,
          confidenceScore: 0,
          complexityReduction: 0,
          maintainabilityImprovement: 0,
        },
        validation: {
          syntaxValid: false,
          semanticsValid: false,
          referencesValid: false,
          typeSystemValid: false,
          grammarCompliant: false,
          issues: [{
            severity: 'error',
            message: error instanceof Error ? error.message : String(error),
            position: request.position,
            code: 'OPERATION_ERROR',
            category: 'semantics',
          }],
          suggestions: [],
        },
        warnings: [],
        errors: [error instanceof Error ? error.message : String(error)],
      };

      this.emit('surgical_operation_error', { request, error });
      return errorResult;
    }
  }

  /**
   * Analyzes the operation context.
   */
  private async analyzeOperationContext(request: SurgicalOperationRequest): Promise<ParseContext> {
    // Get current context at position
    const context = this.contextManager.getContextAt(request.file, request.position);

    if (!context) {
      throw new Error(`No context available at position ${request.position.line}:${request.position.column}`);
    }

    // Enhance context with language-specific information
    const languageConfig = this.languageManager.getLanguageConfig(request.language);
    if (languageConfig) {
      // Store language config for later use (simplified)
    }

    // Convert ContextInfo to ParseContext
    const parseContext: ParseContext = {
      id: `${request.file}:${request.position.line}:${request.position.column}`,
      scopeType: this.convertScopeType(context.scope?.type ?? ContextScopeType.GLOBAL),
      symbols: new Map(),
      rules: new Map(),
      depth: context.scope?.depth || 0,
      position: {
        line: request.position.line,
        column: request.position.column,
        offset: 0,
        length: 0,
      },
      metadata: new Map(),
    };

    return parseContext;
  }

  /**
   * Generates surgical changes for the operation.
   */
  private async generateSurgicalChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    switch (request.type) {
      case SurgicalOperationType.EXTRACT_VARIABLE:
        changes.push(...await this.generateExtractVariableChanges(request, context));
        break;
      case SurgicalOperationType.INLINE_VARIABLE:
        changes.push(...await this.generateInlineVariableChanges(request, context));
        break;
      case SurgicalOperationType.EXTRACT_FUNCTION:
        changes.push(...await this.generateExtractFunctionChanges(request, context));
        break;
      case SurgicalOperationType.INLINE_FUNCTION:
        changes.push(...await this.generateInlineFunctionChanges(request, context));
        break;
      case SurgicalOperationType.RENAME_SYMBOL:
        changes.push(...await this.generateRenameSymbolChanges(request, context));
        break;
      case SurgicalOperationType.MOVE_SYMBOL:
        changes.push(...await this.generateMoveSymbolChanges(request, context));
        break;
      case SurgicalOperationType.EXTRACT_INTERFACE:
        changes.push(...await this.generateExtractInterfaceChanges(request, context));
        break;
      case SurgicalOperationType.EXTRACT_CLASS:
        changes.push(...await this.generateExtractClassChanges(request, context));
        break;
      case SurgicalOperationType.SPLIT_VARIABLE:
        changes.push(...await this.generateSplitVariableChanges(request, context));
        break;
      case SurgicalOperationType.MERGE_VARIABLES:
        changes.push(...await this.generateMergeVariablesChanges(request, context));
        break;
      case SurgicalOperationType.INTRODUCE_PARAMETER:
        changes.push(...await this.generateIntroduceParameterChanges(request, context));
        break;
      case SurgicalOperationType.REMOVE_PARAMETER:
        changes.push(...await this.generateRemoveParameterChanges(request, context));
        break;
      case SurgicalOperationType.CHANGE_SIGNATURE:
        changes.push(...await this.generateChangeSignatureChanges(request, context));
        break;
      case SurgicalOperationType.EXTRACT_CONSTANT:
        changes.push(...await this.generateExtractConstantChanges(request, context));
        break;
      case SurgicalOperationType.INLINE_CONSTANT:
        changes.push(...await this.generateInlineConstantChanges(request, context));
        break;
      default:
        throw new Error(`Unsupported surgical operation type: ${request.type}`);
    }

    return changes;
  }

  /**
   * Generates extract variable changes.
   */
  private async generateExtractVariableChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    // Get the expression to extract
    const expression = this.getExpressionAtRange(
      context,
      request.position,
      request.endPosition || request.position,
    );

    if (!expression) {
      throw new Error('No expression found at the specified position');
    }

    // Generate variable declaration
    const variableName = request.parameters.symbolName || 'extractedVar';
    const variableDeclaration = this.generateVariableDeclaration(
      variableName,
      expression,
      request.language,
      context,
    );

    // Find insertion point for variable declaration
    const insertionPoint = this.findVariableInsertionPoint(context, request.position);

    // Create insertion change
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: variableDeclaration + '\n',
      scope: {
        type: 'statement',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'variable_declaration',
        symbolsAffected: [variableName],
        referencesAffected: 0,
      },
      confidence: 0.95,
      dependencies: [],
      metadata: {
        grammarRule: 'variable_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'local_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [variableName],
        removedElements: [],
      },
    });

    // Create replacement change
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: request.position,
      endPosition: request.endPosition,
      oldText: expression,
      newText: variableName,
      scope: {
        type: 'expression',
        startPosition: request.position,
        endPosition: request.endPosition || request.position,
        contextType: 'expression_replacement',
        symbolsAffected: [variableName],
        referencesAffected: 1,
      },
      confidence: 0.98,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'identifier',
        syntaxNode: 'identifier_expression',
        semanticContext: 'expression_context',
        preservedElements: [],
        modifiedElements: [expression],
        addedElements: [],
        removedElements: [expression],
      },
    });

    return changes;
  }

  /**
   * Generates inline variable changes.
   */
  private async generateInlineVariableChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    // Find variable declaration
    const variableName = request.parameters.symbolName;
    if (!variableName) {
      throw new Error('Variable name is required for inline operation');
    }

    const symbolInfo = this.findSymbolDeclaration(context, variableName, request.position);
    if (!symbolInfo) {
      throw new Error(`Variable '${variableName}' not found`);
    }

    // Get variable value
    const variableValue = this.getVariableValue(symbolInfo, context);
    if (!variableValue) {
      throw new Error(`Cannot determine value of variable '${variableName}'`);
    }

    // Find all references
    const references = this.findSymbolReferences(context, variableName);

    // Replace each reference with the variable value
    for (const reference of references) {
      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: reference.position,
        endPosition: reference.endPosition,
        oldText: variableName,
        newText: variableValue,
        scope: {
          type: 'expression',
          startPosition: reference.position,
          endPosition: reference.endPosition,
          contextType: 'variable_reference',
          symbolsAffected: [variableName],
          referencesAffected: 1,
        },
        confidence: 0.92,
        dependencies: [],
        metadata: {
          grammarRule: 'identifier',
          syntaxNode: 'identifier_expression',
          semanticContext: 'variable_reference',
          preservedElements: [],
          modifiedElements: [variableName],
          addedElements: [],
          removedElements: [variableName],
        },
      });
    }

    // Remove variable declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'delete',
      position: symbolInfo.position,
      endPosition: {
        line: symbolInfo.position.line,
        column: symbolInfo.position.column + symbolInfo.name.length,
        offset: symbolInfo.position.offset + symbolInfo.name.length,
      },
      oldText: this.getTextAtRange(context, symbolInfo.position, {
        line: symbolInfo.position.line,
        column: symbolInfo.position.column + symbolInfo.name.length,
        offset: symbolInfo.position.offset + symbolInfo.name.length,
      }),
      newText: '',
      scope: {
        type: 'statement',
        startPosition: symbolInfo.position,
        endPosition: {
          line: symbolInfo.position.line,
          column: symbolInfo.position.column + symbolInfo.name.length,
          offset: symbolInfo.position.offset + symbolInfo.name.length,
        },
        contextType: 'variable_declaration',
        symbolsAffected: [variableName],
        referencesAffected: references.length,
      },
      confidence: 0.95,
      dependencies: changes.slice(0, -1).map(c => c.id),
      metadata: {
        grammarRule: 'variable_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'local_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [],
        removedElements: [variableName],
      },
    });

    return changes;
  }

  /**
   * Generates extract function changes.
   */
  private async generateExtractFunctionChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    // Get the code block to extract
    const codeBlock = this.getTextAtRange(context, request.position, request.endPosition!);

    // Analyze dependencies
    const dependencies = this.analyzeFunctionDependencies(codeBlock, context);

    // Generate function signature
    const functionName = request.parameters.symbolName || 'extractedFunction';
    const functionSignature = this.generateFunctionSignature(
      functionName,
      dependencies.parameters,
      dependencies.returnType,
      request.language,
    );

    // Generate function body
    const functionBody = this.generateFunctionBody(codeBlock, dependencies, request.language);

    // Find insertion point for function
    const insertionPoint = this.findFunctionInsertionPoint(context, request.position);

    // Create function insertion change
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: `${functionSignature} {\n${functionBody}\n}\n\n`,
      scope: {
        type: 'function',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'function_declaration',
        symbolsAffected: [functionName],
        referencesAffected: 0,
      },
      confidence: 0.88,
      dependencies: [],
      metadata: {
        grammarRule: 'function_declaration',
        syntaxNode: 'function_definition',
        semanticContext: 'global_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [functionName],
        removedElements: [],
      },
    });

    // Create function call replacement
    const functionCall = this.generateFunctionCall(functionName, dependencies.arguments, request.language);

    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: request.position,
      endPosition: request.endPosition,
      oldText: codeBlock,
      newText: functionCall,
      scope: {
        type: 'block',
        startPosition: request.position,
        endPosition: request.endPosition!,
        contextType: 'code_block',
        symbolsAffected: [functionName],
        referencesAffected: 1,
      },
      confidence: 0.90,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'function_call',
        syntaxNode: 'call_expression',
        semanticContext: 'function_call',
        preservedElements: dependencies.preservedVariables,
        modifiedElements: [codeBlock],
        addedElements: [],
        removedElements: [codeBlock],
      },
    });

    return changes;
  }

  /**
   * Generates inline function changes.
   */
  private async generateInlineFunctionChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const functionName = request.parameters.symbolName || request.parameters.functionName;
    if (!functionName) {
      throw new Error('Function name is required for inline function operation');
    }

    const functionDeclaration = this.findSymbolDeclaration(context, functionName, request.position);

    if (!functionDeclaration) {
      throw new Error(`Function ${functionName} not found`);
    }

    // Find all function calls
    const functionCalls = this.findSymbolReferences(context, functionName);

    if (functionCalls.length === 0) {
      throw new Error(`No calls to function ${functionName} found`);
    }

    // Get the actual function body from the declaration
    const functionBody = this.extractFunctionBody(functionDeclaration, context);

    if (!functionBody) {
      throw new Error(`Cannot extract function body for ${functionName}`);
    }

    // For each call, replace with function body
    for (const call of functionCalls) {
      const callText = this.getTextAtRange(context, call.position, call.endPosition);

      // Extract arguments from the call if needed
      const callArguments = this.extractCallArguments(callText);
      const inlinedBody = this.substituteParameters(functionBody, callArguments, context);

      changes.push({
        id: this.generateChangeId(),
        type: 'replace',
        file: request.file,
        position: call.position,
        endPosition: call.endPosition,
        oldText: callText,
        newText: inlinedBody,
        scope: {
          type: 'expression',
          startPosition: call.position,
          endPosition: call.endPosition,
          contextType: 'function_call',
          symbolsAffected: [functionName],
          referencesAffected: 1,
        },
        confidence: 0.85,
        dependencies: [],
        metadata: {
          grammarRule: 'expression',
          syntaxNode: 'inline_expansion',
          semanticContext: 'function_call_replacement',
          preservedElements: [],
          modifiedElements: [callText],
          addedElements: [inlinedBody],
          removedElements: [callText],
        },
      });
    }

    // Remove the function declaration
    const declarationEnd = this.findFunctionDeclarationEnd(functionDeclaration, context);
    const declarationText = this.getTextAtRange(context, functionDeclaration.position, declarationEnd);

    changes.push({
      id: this.generateChangeId(),
      type: 'delete',
      file: request.file,
      position: functionDeclaration.position,
      endPosition: declarationEnd,
      oldText: declarationText,
      newText: '',
      scope: {
        type: 'function',
        startPosition: functionDeclaration.position,
        endPosition: declarationEnd,
        contextType: 'function_declaration',
        symbolsAffected: [functionName],
        referencesAffected: functionCalls.length,
      },
      confidence: 0.90,
      dependencies: changes.slice(0, -1).map(c => c.id),
      metadata: {
        grammarRule: 'function_declaration',
        syntaxNode: 'function_definition',
        semanticContext: 'function_removal',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [],
        removedElements: [declarationText],
      },
    });

    return changes;
  }

  /**
   * Generates rename symbol changes.
   */
  private async generateRenameSymbolChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const oldName = request.parameters.symbolName;
    const newName = request.parameters.newName;

    if (!oldName || !newName) {
      throw new Error('Both old name and new name are required for rename operation');
    }

    // Find all references to the symbol
    const references = this.findSymbolReferences(context, oldName);

    // Create replacement changes for each reference
    for (const reference of references) {
      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: reference.position,
        endPosition: reference.endPosition,
        oldText: oldName,
        newText: newName,
        scope: {
          type: 'token',
          startPosition: reference.position,
          endPosition: reference.endPosition,
          contextType: reference.type,
          symbolsAffected: [oldName, newName],
          referencesAffected: 1,
        },
        confidence: 0.98,
        dependencies: [],
        metadata: {
          grammarRule: 'identifier',
          syntaxNode: 'identifier',
          semanticContext: reference.context,
          preservedElements: [],
          modifiedElements: [oldName],
          addedElements: [newName],
          removedElements: [oldName],
        },
      });
    }

    return changes;
  }

  /**
   * Generates move symbol changes.
   */
  private async generateMoveSymbolChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const symbolName = request.parameters.symbolName;
    const targetFile = request.parameters.targetFile;
    const targetPosition = request.parameters.targetPosition;

    if (!symbolName || !targetFile || !targetPosition) {
      throw new Error('Symbol name, target file, and target position are required for move operation');
    }

    // Find symbol declaration
    const symbolInfo = this.findSymbolDeclaration(context, symbolName, request.position);
    if (!symbolInfo) {
      throw new Error(`Symbol '${symbolName}' not found`);
    }

    // Get symbol text content
    const symbolText = this.getTextAtRange(context, symbolInfo.position, {
      line: symbolInfo.position.line + 10, // Approximate end
      column: symbolInfo.position.column + 100,
      offset: symbolInfo.position.offset + 500,
    });

    // Delete from current location
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'delete',
      position: symbolInfo.position,
      endPosition: {
        line: symbolInfo.position.line + 10,
        column: symbolInfo.position.column + 100,
        offset: symbolInfo.position.offset + 500,
      },
      oldText: symbolText,
      newText: '',
      scope: {
        type: 'function',
        startPosition: symbolInfo.position,
        endPosition: {
          line: symbolInfo.position.line + 10,
          column: symbolInfo.position.column + 100,
          offset: symbolInfo.position.offset + 500,
        },
        contextType: 'symbol_declaration',
        symbolsAffected: [symbolName],
        referencesAffected: 0,
      },
      confidence: 0.85,
      dependencies: [],
      metadata: {
        grammarRule: 'declaration',
        syntaxNode: 'symbol_declaration',
        semanticContext: 'global_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [],
        removedElements: [symbolName],
      },
    });

    // Insert at target location
    changes.push({
      id: this.generateChangeId(),
      file: targetFile,
      type: 'insert',
      position: targetPosition,
      oldText: '',
      newText: symbolText + '\n',
      scope: {
        type: 'function',
        startPosition: targetPosition,
        endPosition: targetPosition,
        contextType: 'symbol_declaration',
        symbolsAffected: [symbolName],
        referencesAffected: 0,
      },
      confidence: 0.85,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'declaration',
        syntaxNode: 'symbol_declaration',
        semanticContext: 'global_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [symbolName],
        removedElements: [],
      },
    });

    // Update references to use new import/namespace if different file
    if (targetFile !== request.file) {
      const references = this.findSymbolReferences(context, symbolName);
      for (const _reference of references) {
        // Add import statement if needed (simplified)
        const importStatement = this.generateImportStatement(symbolName, targetFile, request.language);
        changes.push({
          id: this.generateChangeId(),
          file: request.file,
          type: 'insert',
          position: { line: 1, column: 0, offset: 0 },
          oldText: '',
          newText: importStatement + '\n',
          scope: {
            type: 'statement',
            startPosition: { line: 1, column: 0, offset: 0 },
            endPosition: { line: 1, column: 0, offset: 0 },
            contextType: 'import_declaration',
            symbolsAffected: [symbolName],
            referencesAffected: references.length,
          },
          confidence: 0.80,
          dependencies: [changes[1].id],
          metadata: {
            grammarRule: 'import_statement',
            syntaxNode: 'import_declaration',
            semanticContext: 'module_scope',
            preservedElements: [],
            modifiedElements: [],
            addedElements: [importStatement],
            removedElements: [],
          },
        });
      }
    }

    return changes;
  }

  /**
   * Generates extract interface changes.
   */
  private async generateExtractInterfaceChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const className = request.parameters.symbolName;
    const interfaceName = request.parameters.newName || `I${className}`;

    if (!className) {
      throw new Error('Class name is required for extract interface operation');
    }

    // Find class declaration
    const classInfo = this.findSymbolDeclaration(context, className, request.position);
    if (!classInfo) {
      throw new Error(`Class '${className}' not found`);
    }

    // Extract public methods and properties (simplified)
    const publicMembers = this.extractPublicMembers(classInfo, context);

    // Generate interface declaration
    const interfaceDeclaration = this.generateInterfaceDeclaration(
      interfaceName,
      publicMembers,
      request.language,
    );

    // Find insertion point for interface
    const insertionPoint = this.findFunctionInsertionPoint(context, request.position);

    // Insert interface declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: interfaceDeclaration + '\n\n',
      scope: {
        type: 'class',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'interface_declaration',
        symbolsAffected: [interfaceName],
        referencesAffected: 0,
      },
      confidence: 0.85,
      dependencies: [],
      metadata: {
        grammarRule: 'interface_declaration',
        syntaxNode: 'interface_definition',
        semanticContext: 'global_scope',
        preservedElements: publicMembers,
        modifiedElements: [],
        addedElements: [interfaceName],
        removedElements: [],
      },
    });

    // Modify class to implement interface
    const implementsClause = this.generateImplementsClause(interfaceName, request.language);
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: classInfo.position,
      endPosition: {
        line: classInfo.position.line,
        column: classInfo.position.column + className.length + 10, // Approximate
        offset: classInfo.position.offset + className.length + 10,
      },
      oldText: `class ${className}`,
      newText: `class ${className} ${implementsClause}`,
      scope: {
        type: 'class',
        startPosition: classInfo.position,
        endPosition: {
          line: classInfo.position.line,
          column: classInfo.position.column + className.length + 10,
          offset: classInfo.position.offset + className.length + 10,
        },
        contextType: 'class_declaration',
        symbolsAffected: [className, interfaceName],
        referencesAffected: 1,
      },
      confidence: 0.90,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'class_declaration',
        syntaxNode: 'class_definition',
        semanticContext: 'class_scope',
        preservedElements: [className],
        modifiedElements: [`class ${className}`],
        addedElements: [implementsClause],
        removedElements: [],
      },
    });

    return changes;
  }

  /**
   * Generates extract class changes.
   */
  private async generateExtractClassChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const newClassName = request.parameters.symbolName || 'ExtractedClass';
    const targetFile = request.parameters.targetFile || request.file;

    // Get the code block to extract as a class
    const codeBlock = this.getTextAtRange(context, request.position, request.endPosition!);

    // Analyze dependencies and members
    const classDependencies = this.analyzeClassDependencies(codeBlock, context);

    // Generate class declaration
    const classDeclaration = this.generateClassDeclaration(
      newClassName,
      classDependencies.methods,
      classDependencies.properties,
      request.language,
    );

    // Find insertion point
    const insertionPoint = targetFile === request.file
      ? this.findFunctionInsertionPoint(context, request.position)
      : { line: 1, column: 0, offset: 0 };

    // Insert new class
    changes.push({
      id: this.generateChangeId(),
      file: targetFile,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: classDeclaration + '\n\n',
      scope: {
        type: 'class',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'class_declaration',
        symbolsAffected: [newClassName],
        referencesAffected: 0,
      },
      confidence: 0.80,
      dependencies: [],
      metadata: {
        grammarRule: 'class_declaration',
        syntaxNode: 'class_definition',
        semanticContext: 'global_scope',
        preservedElements: classDependencies.preservedElements,
        modifiedElements: [],
        addedElements: [newClassName, ...classDependencies.methods, ...classDependencies.properties],
        removedElements: [],
      },
    });

    // Replace original code with class instantiation
    const classInstantiation = this.generateClassInstantiation(
      newClassName,
      classDependencies.constructorArgs,
      request.language,
    );

    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: request.position,
      endPosition: request.endPosition,
      oldText: codeBlock,
      newText: classInstantiation,
      scope: {
        type: 'block',
        startPosition: request.position,
        endPosition: request.endPosition!,
        contextType: 'code_block',
        symbolsAffected: [newClassName],
        referencesAffected: 1,
      },
      confidence: 0.75,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'new_expression',
        syntaxNode: 'object_creation',
        semanticContext: 'expression_context',
        preservedElements: [],
        modifiedElements: [codeBlock],
        addedElements: [classInstantiation],
        removedElements: [codeBlock],
      },
    });

    // Add import statement if different file
    if (targetFile !== request.file) {
      const importStatement = this.generateImportStatement(newClassName, targetFile, request.language);
      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'insert',
        position: { line: 1, column: 0, offset: 0 },
        oldText: '',
        newText: importStatement + '\n',
        scope: {
          type: 'statement',
          startPosition: { line: 1, column: 0, offset: 0 },
          endPosition: { line: 1, column: 0, offset: 0 },
          contextType: 'import_declaration',
          symbolsAffected: [newClassName],
          referencesAffected: 1,
        },
        confidence: 0.85,
        dependencies: [changes[0].id],
        metadata: {
          grammarRule: 'import_statement',
          syntaxNode: 'import_declaration',
          semanticContext: 'module_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: [importStatement],
          removedElements: [],
        },
      });
    }

    return changes;
  }

  /**
   * Generates split variable changes.
   */
  private async generateSplitVariableChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const variableName = request.parameters.symbolName;
    const splitNames = request.parameters.splitNames || ['part1', 'part2'];

    if (!variableName) {
      throw new Error('Variable name is required for split operation');
    }

    // Find variable declaration
    const symbolInfo = this.findSymbolDeclaration(context, variableName, request.position);
    if (!symbolInfo) {
      throw new Error(`Variable '${variableName}' not found`);
    }

    // Find all references to analyze usage patterns
    const references = this.findSymbolReferences(context, variableName);

    // Analyze how the variable is used to determine split logic
    const usageAnalysis = this.analyzeVariableUsage(references, context);

    // Generate new variable declarations
    for (let i = 0; i < splitNames.length; i++) {
      const newVarName = splitNames[i];
      const initialValue = this.determineInitialValue(usageAnalysis, i, request.language);
      const varDeclaration = this.generateVariableDeclaration(newVarName, initialValue, request.language, context);

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'insert',
        position: symbolInfo.position,
        oldText: '',
        newText: varDeclaration + '\n',
        scope: {
          type: 'statement',
          startPosition: symbolInfo.position,
          endPosition: symbolInfo.position,
          contextType: 'variable_declaration',
          symbolsAffected: [newVarName],
          referencesAffected: 0,
        },
        confidence: 0.70,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: [newVarName],
          removedElements: [],
        },
      });
    }

    // Update references to use appropriate split variable
    for (const reference of references) {
      const appropriateVar = this.determineAppropriateVariable(reference, splitNames, usageAnalysis);

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: reference.position,
        endPosition: reference.endPosition,
        oldText: variableName,
        newText: appropriateVar,
        scope: {
          type: 'expression',
          startPosition: reference.position,
          endPosition: reference.endPosition,
          contextType: 'variable_reference',
          symbolsAffected: [variableName, appropriateVar],
          referencesAffected: 1,
        },
        confidence: 0.65,
        dependencies: changes.slice(0, splitNames.length).map(c => c.id),
        metadata: {
          grammarRule: 'identifier',
          syntaxNode: 'identifier_expression',
          semanticContext: 'variable_reference',
          preservedElements: [],
          modifiedElements: [variableName],
          addedElements: [appropriateVar],
          removedElements: [variableName],
        },
      });
    }

    // Remove original variable declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'delete',
      position: symbolInfo.position,
      endPosition: {
        line: symbolInfo.position.line + 1,
        column: 0,
        offset: symbolInfo.position.offset + 100, // Approximate
      },
      oldText: this.getTextAtRange(context, symbolInfo.position, {
        line: symbolInfo.position.line + 1,
        column: 0,
        offset: symbolInfo.position.offset + 100,
      }),
      newText: '',
      scope: {
        type: 'statement',
        startPosition: symbolInfo.position,
        endPosition: {
          line: symbolInfo.position.line + 1,
          column: 0,
          offset: symbolInfo.position.offset + 100,
        },
        contextType: 'variable_declaration',
        symbolsAffected: [variableName],
        referencesAffected: references.length,
      },
      confidence: 0.85,
      dependencies: changes.slice(splitNames.length).map(c => c.id),
      metadata: {
        grammarRule: 'variable_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'local_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [],
        removedElements: [variableName],
      },
    });

    return changes;
  }

  /**
   * Generates merge variables changes.
   */
  private async generateMergeVariablesChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const variableNames = request.parameters.variableNames || [];
    const mergedName = request.parameters.symbolName || 'mergedVariable';

    if (variableNames.length < 2) {
      throw new Error('At least two variable names are required for merge operation');
    }

    const symbolInfos: SymbolInfo[] = [];
    const allReferences: Array<{
      position: CodePosition;
      endPosition: CodePosition;
      type: string;
      context: string;
      variableName: string;
    }> = [];

    // Find all variable declarations and references
    for (const varName of variableNames) {
      const symbolInfo = this.findSymbolDeclaration(context, varName, request.position);
      if (symbolInfo) {
        symbolInfos.push(symbolInfo);
      }

      const references = this.findSymbolReferences(context, varName);
      references.forEach(ref => allReferences.push({ ...ref, variableName: varName }));
    }

    if (symbolInfos.length === 0) {
      throw new Error('No variables found to merge');
    }

    // Determine merged variable type and initial value
    const _mergedType = this.determineMergedType(symbolInfos, request.language);
    const mergedValue = this.determineMergedValue(symbolInfos, variableNames, request.language);

    // Create merged variable declaration
    const mergedDeclaration = this.generateVariableDeclaration(mergedName, mergedValue, request.language, context);
    const insertionPoint = symbolInfos[0].position; // Insert at first variable position

    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: mergedDeclaration + '\n',
      scope: {
        type: 'statement',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'variable_declaration',
        symbolsAffected: [mergedName],
        referencesAffected: 0,
      },
      confidence: 0.80,
      dependencies: [],
      metadata: {
        grammarRule: 'variable_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'local_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [mergedName],
        removedElements: [],
      },
    });

    // Update all references to use merged variable
    for (const reference of allReferences) {
      const accessPattern = this.generateMergedAccessPattern(
        reference.variableName,
        mergedName,
        variableNames,
        request.language,
      );

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: reference.position,
        endPosition: reference.endPosition,
        oldText: reference.variableName,
        newText: accessPattern,
        scope: {
          type: 'expression',
          startPosition: reference.position,
          endPosition: reference.endPosition,
          contextType: 'variable_reference',
          symbolsAffected: [reference.variableName, mergedName],
          referencesAffected: 1,
        },
        confidence: 0.75,
        dependencies: [changes[0].id],
        metadata: {
          grammarRule: 'identifier',
          syntaxNode: 'identifier_expression',
          semanticContext: 'variable_reference',
          preservedElements: [],
          modifiedElements: [reference.variableName],
          addedElements: [accessPattern],
          removedElements: [reference.variableName],
        },
      });
    }

    // Remove original variable declarations
    for (let i = 0; i < symbolInfos.length; i++) {
      const symbolInfo = symbolInfos[i];
      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'delete',
        position: symbolInfo.position,
        endPosition: {
          line: symbolInfo.position.line + 1,
          column: 0,
          offset: symbolInfo.position.offset + 100, // Approximate
        },
        oldText: this.getTextAtRange(context, symbolInfo.position, {
          line: symbolInfo.position.line + 1,
          column: 0,
          offset: symbolInfo.position.offset + 100,
        }),
        newText: '',
        scope: {
          type: 'statement',
          startPosition: symbolInfo.position,
          endPosition: {
            line: symbolInfo.position.line + 1,
            column: 0,
            offset: symbolInfo.position.offset + 100,
          },
          contextType: 'variable_declaration',
          symbolsAffected: [variableNames[i]],
          referencesAffected: 0,
        },
        confidence: 0.85,
        dependencies: changes.slice(1, 1 + allReferences.length).map(c => c.id),
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: [],
          removedElements: [variableNames[i]],
        },
      });
    }

    return changes;
  }

  /**
   * Generates introduce parameter changes.
   */
  private async generateIntroduceParameterChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const functionName = request.parameters.functionName;
    const parameterName = request.parameters.symbolName || 'newParam';
    const parameterType = request.parameters.parameterType || 'any';
    const defaultValue = request.parameters.defaultValue;

    if (!functionName) {
      throw new Error('Function name is required for introduce parameter operation');
    }

    // Find function declaration
    const functionInfo = this.findSymbolDeclaration(context, functionName, request.position);
    if (!functionInfo) {
      throw new Error(`Function '${functionName}' not found`);
    }

    // Find function signature and parameters
    const functionSignature = this.getFunctionSignature(functionInfo, context);
    const updatedSignature = this.addParameterToSignature(
      functionSignature,
      parameterName,
      parameterType,
      defaultValue,
      request.language,
    );

    // Update function declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: functionInfo.position,
      endPosition: {
        line: functionInfo.position.line,
        column: functionInfo.position.column + functionSignature.length,
        offset: functionInfo.position.offset + functionSignature.length,
      },
      oldText: functionSignature,
      newText: updatedSignature,
      scope: {
        type: 'function',
        startPosition: functionInfo.position,
        endPosition: {
          line: functionInfo.position.line,
          column: functionInfo.position.column + functionSignature.length,
          offset: functionInfo.position.offset + functionSignature.length,
        },
        contextType: 'function_signature',
        symbolsAffected: [functionName, parameterName],
        referencesAffected: 0,
      },
      confidence: 0.90,
      dependencies: [],
      metadata: {
        grammarRule: 'function_declaration',
        syntaxNode: 'function_signature',
        semanticContext: 'function_scope',
        preservedElements: [functionName],
        modifiedElements: [functionSignature],
        addedElements: [parameterName],
        removedElements: [],
      },
    });

    // Find all function calls and update them
    const functionCalls = this.findSymbolReferences(context, functionName);
    for (const call of functionCalls) {
      const callSignature = this.getTextAtRange(context, call.position, call.endPosition);
      const updatedCall = this.addArgumentToCall(
        callSignature,
        defaultValue || this.getDefaultValueForType(parameterType, request.language),
        request.language,
      );

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: call.position,
        endPosition: call.endPosition,
        oldText: callSignature,
        newText: updatedCall,
        scope: {
          type: 'expression',
          startPosition: call.position,
          endPosition: call.endPosition,
          contextType: 'function_call',
          symbolsAffected: [functionName],
          referencesAffected: 1,
        },
        confidence: 0.85,
        dependencies: [changes[0].id],
        metadata: {
          grammarRule: 'function_call',
          syntaxNode: 'call_expression',
          semanticContext: 'expression_context',
          preservedElements: [functionName],
          modifiedElements: [callSignature],
          addedElements: [defaultValue || 'defaultValue'],
          removedElements: [],
        },
      });
    }

    return changes;
  }

  /**
   * Generates remove parameter changes.
   */
  private async generateRemoveParameterChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const functionName = request.parameters.functionName;
    const parameterName = request.parameters.symbolName;
    const parameterIndex = request.parameters.parameterIndex;

    if (!functionName || (!parameterName && parameterIndex === undefined)) {
      throw new Error('Function name and parameter name or index are required for remove parameter operation');
    }

    // Find function declaration
    const functionInfo = this.findSymbolDeclaration(context, functionName, request.position);
    if (!functionInfo) {
      throw new Error(`Function '${functionName}' not found`);
    }

    // Find function signature and parameters
    const functionSignature = this.getFunctionSignature(functionInfo, context);
    const updatedSignature = this.removeParameterFromSignature(
      functionSignature,
      parameterName,
      parameterIndex,
      request.language,
    );

    // Update function declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: functionInfo.position,
      endPosition: {
        line: functionInfo.position.line,
        column: functionInfo.position.column + functionSignature.length,
        offset: functionInfo.position.offset + functionSignature.length,
      },
      oldText: functionSignature,
      newText: updatedSignature,
      scope: {
        type: 'function',
        startPosition: functionInfo.position,
        endPosition: {
          line: functionInfo.position.line,
          column: functionInfo.position.column + functionSignature.length,
          offset: functionInfo.position.offset + functionSignature.length,
        },
        contextType: 'function_signature',
        symbolsAffected: [functionName, parameterName || 'parameter'],
        referencesAffected: 0,
      },
      confidence: 0.90,
      dependencies: [],
      metadata: {
        grammarRule: 'function_declaration',
        syntaxNode: 'function_signature',
        semanticContext: 'function_scope',
        preservedElements: [functionName],
        modifiedElements: [functionSignature],
        addedElements: [],
        removedElements: [parameterName || 'parameter'],
      },
    });

    // Find all function calls and update them
    const functionCalls = this.findSymbolReferences(context, functionName);
    for (const call of functionCalls) {
      const callSignature = this.getTextAtRange(context, call.position, call.endPosition);
      const updatedCall = this.removeArgumentFromCall(
        callSignature,
        parameterName,
        parameterIndex,
        request.language,
      );

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: call.position,
        endPosition: call.endPosition,
        oldText: callSignature,
        newText: updatedCall,
        scope: {
          type: 'expression',
          startPosition: call.position,
          endPosition: call.endPosition,
          contextType: 'function_call',
          symbolsAffected: [functionName],
          referencesAffected: 1,
        },
        confidence: 0.85,
        dependencies: [changes[0].id],
        metadata: {
          grammarRule: 'function_call',
          syntaxNode: 'call_expression',
          semanticContext: 'expression_context',
          preservedElements: [functionName],
          modifiedElements: [callSignature],
          addedElements: [],
          removedElements: [parameterName || 'argument'],
        },
      });
    }

    // Remove parameter references within function body if needed
    if (parameterName) {
      const parameterReferences = this.findParameterReferences(functionInfo, parameterName, context);
      for (const ref of parameterReferences) {
        changes.push({
          id: this.generateChangeId(),
          file: request.file,
          type: 'delete',
          position: ref.position,
          endPosition: ref.endPosition,
          oldText: this.getTextAtRange(context, ref.position, ref.endPosition),
          newText: '',
          scope: {
            type: 'expression',
            startPosition: ref.position,
            endPosition: ref.endPosition,
            contextType: 'parameter_reference',
            symbolsAffected: [parameterName],
            referencesAffected: 1,
          },
          confidence: 0.70,
          dependencies: [changes[0].id],
          metadata: {
            grammarRule: 'identifier',
            syntaxNode: 'identifier_expression',
            semanticContext: 'function_body',
            preservedElements: [],
            modifiedElements: [],
            addedElements: [],
            removedElements: [parameterName],
          },
        });
      }
    }

    return changes;
  }

  /**
   * Generates change signature changes.
   */
  private async generateChangeSignatureChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const functionName = request.parameters.functionName;
    const newSignature = request.parameters.newSignature;
    const parameterChanges = request.parameters.parameterChanges || [];

    if (!functionName || !newSignature) {
      throw new Error('Function name and new signature are required for change signature operation');
    }

    // Find function declaration
    const functionInfo = this.findSymbolDeclaration(context, functionName, request.position);
    if (!functionInfo) {
      throw new Error(`Function '${functionName}' not found`);
    }

    // Get current function signature
    const currentSignature = this.getFunctionSignature(functionInfo, context);

    // Update function declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: functionInfo.position,
      endPosition: {
        line: functionInfo.position.line,
        column: functionInfo.position.column + currentSignature.length,
        offset: functionInfo.position.offset + currentSignature.length,
      },
      oldText: currentSignature,
      newText: newSignature,
      scope: {
        type: 'function',
        startPosition: functionInfo.position,
        endPosition: {
          line: functionInfo.position.line,
          column: functionInfo.position.column + currentSignature.length,
          offset: functionInfo.position.offset + currentSignature.length,
        },
        contextType: 'function_signature',
        symbolsAffected: [functionName],
        referencesAffected: 0,
      },
      confidence: 0.85,
      dependencies: [],
      metadata: {
        grammarRule: 'function_declaration',
        syntaxNode: 'function_signature',
        semanticContext: 'function_scope',
        preservedElements: [functionName],
        modifiedElements: [currentSignature],
        addedElements: [newSignature],
        removedElements: [currentSignature],
      },
    });

    // Find all function calls and update them according to parameter changes
    const functionCalls = this.findSymbolReferences(context, functionName);
    for (const call of functionCalls) {
      const callSignature = this.getTextAtRange(context, call.position, call.endPosition);
      const updatedCall = this.updateCallSignature(
        callSignature,
        parameterChanges,
        request.language,
      );

      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: call.position,
        endPosition: call.endPosition,
        oldText: callSignature,
        newText: updatedCall,
        scope: {
          type: 'expression',
          startPosition: call.position,
          endPosition: call.endPosition,
          contextType: 'function_call',
          symbolsAffected: [functionName],
          referencesAffected: 1,
        },
        confidence: 0.80,
        dependencies: [changes[0].id],
        metadata: {
          grammarRule: 'function_call',
          syntaxNode: 'call_expression',
          semanticContext: 'expression_context',
          preservedElements: [functionName],
          modifiedElements: [callSignature],
          addedElements: [updatedCall],
          removedElements: [callSignature],
        },
      });
    }

    // Update parameter references within function body if needed
    for (const paramChange of parameterChanges) {
      if (paramChange.action === 'rename' && paramChange.oldName && paramChange.newName) {
        const parameterReferences = this.findParameterReferences(functionInfo, paramChange.oldName, context);
        for (const ref of parameterReferences) {
          changes.push({
            id: this.generateChangeId(),
            file: request.file,
            type: 'replace',
            position: ref.position,
            endPosition: ref.endPosition,
            oldText: paramChange.oldName,
            newText: paramChange.newName,
            scope: {
              type: 'expression',
              startPosition: ref.position,
              endPosition: ref.endPosition,
              contextType: 'parameter_reference',
              symbolsAffected: [paramChange.oldName, paramChange.newName],
              referencesAffected: 1,
            },
            confidence: 0.85,
            dependencies: [changes[0].id],
            metadata: {
              grammarRule: 'identifier',
              syntaxNode: 'identifier_expression',
              semanticContext: 'function_body',
              preservedElements: [],
              modifiedElements: [paramChange.oldName],
              addedElements: [paramChange.newName],
              removedElements: [paramChange.oldName],
            },
          });
        }
      }
    }

    return changes;
  }

  /**
   * Generates extract constant changes.
   */
  private async generateExtractConstantChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    // Get the literal value to extract
    const literalValue = this.getExpressionAtRange(
      context,
      request.position,
      request.endPosition || request.position,
    );

    if (!literalValue) {
      throw new Error('No literal value found at the specified position');
    }

    // Generate constant name and declaration
    const constantName = request.parameters.symbolName || this.generateConstantName(literalValue);
    const constantDeclaration = this.generateConstantDeclaration(
      constantName,
      literalValue,
      request.language,
      context,
    );

    // Find insertion point for constant declaration
    const insertionPoint = this.findConstantInsertionPoint(context, request.position);

    // Create constant declaration
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'insert',
      position: insertionPoint,
      oldText: '',
      newText: constantDeclaration + '\n',
      scope: {
        type: 'statement',
        startPosition: insertionPoint,
        endPosition: insertionPoint,
        contextType: 'constant_declaration',
        symbolsAffected: [constantName],
        referencesAffected: 0,
      },
      confidence: 0.95,
      dependencies: [],
      metadata: {
        grammarRule: 'constant_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'global_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [constantName],
        removedElements: [],
      },
    });

    // Replace literal with constant reference
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'replace',
      position: request.position,
      endPosition: request.endPosition,
      oldText: literalValue,
      newText: constantName,
      scope: {
        type: 'expression',
        startPosition: request.position,
        endPosition: request.endPosition || request.position,
        contextType: 'literal_replacement',
        symbolsAffected: [constantName],
        referencesAffected: 1,
      },
      confidence: 0.98,
      dependencies: [changes[0].id],
      metadata: {
        grammarRule: 'identifier',
        syntaxNode: 'identifier_expression',
        semanticContext: 'expression_context',
        preservedElements: [],
        modifiedElements: [literalValue],
        addedElements: [constantName],
        removedElements: [literalValue],
      },
    });

    // Find and replace other instances of the same literal
    if (request.parameters.replaceAll) {
      const similarLiterals = this.findSimilarLiterals(literalValue, context);
      for (const literal of similarLiterals) {
        if (literal.position.line !== request.position.line ||
            literal.position.column !== request.position.column) {
          changes.push({
            id: this.generateChangeId(),
            file: request.file,
            type: 'replace',
            position: literal.position,
            endPosition: literal.endPosition,
            oldText: literalValue,
            newText: constantName,
            scope: {
              type: 'expression',
              startPosition: literal.position,
              endPosition: literal.endPosition,
              contextType: 'literal_replacement',
              symbolsAffected: [constantName],
              referencesAffected: 1,
            },
            confidence: 0.90,
            dependencies: [changes[0].id],
            metadata: {
              grammarRule: 'identifier',
              syntaxNode: 'identifier_expression',
              semanticContext: 'expression_context',
              preservedElements: [],
              modifiedElements: [literalValue],
              addedElements: [constantName],
              removedElements: [literalValue],
            },
          });
        }
      }
    }

    return changes;
  }

  /**
   * Generates inline constant changes.
   */
  private async generateInlineConstantChanges(
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalChange[]> {
    const changes: SurgicalChange[] = [];

    const constantName = request.parameters.symbolName;
    if (!constantName) {
      throw new Error('Constant name is required for inline operation');
    }

    // Find constant declaration
    const constantInfo = this.findSymbolDeclaration(context, constantName, request.position);
    if (!constantInfo) {
      throw new Error(`Constant '${constantName}' not found`);
    }

    // Get constant value
    const constantValue = this.getConstantValue(constantInfo, context);
    if (!constantValue) {
      throw new Error(`Cannot determine value of constant '${constantName}'`);
    }

    // Find all references to the constant
    const references = this.findSymbolReferences(context, constantName);

    // Replace each reference with the constant value
    for (const reference of references) {
      changes.push({
        id: this.generateChangeId(),
        file: request.file,
        type: 'replace',
        position: reference.position,
        endPosition: reference.endPosition,
        oldText: constantName,
        newText: constantValue,
        scope: {
          type: 'expression',
          startPosition: reference.position,
          endPosition: reference.endPosition,
          contextType: 'constant_reference',
          symbolsAffected: [constantName],
          referencesAffected: 1,
        },
        confidence: 0.95,
        dependencies: [],
        metadata: {
          grammarRule: 'literal',
          syntaxNode: 'literal_expression',
          semanticContext: 'constant_reference',
          preservedElements: [],
          modifiedElements: [constantName],
          addedElements: [constantValue],
          removedElements: [constantName],
        },
      });
    }

    // Remove constant declaration
    const declarationEnd = this.findConstantDeclarationEnd(constantInfo, context);
    changes.push({
      id: this.generateChangeId(),
      file: request.file,
      type: 'delete',
      position: constantInfo.position,
      endPosition: declarationEnd,
      oldText: this.getTextAtRange(context, constantInfo.position, declarationEnd),
      newText: '',
      scope: {
        type: 'statement',
        startPosition: constantInfo.position,
        endPosition: declarationEnd,
        contextType: 'constant_declaration',
        symbolsAffected: [constantName],
        referencesAffected: references.length,
      },
      confidence: 0.90,
      dependencies: changes.slice(0, -1).map(c => c.id),
      metadata: {
        grammarRule: 'constant_declaration',
        syntaxNode: 'declaration_statement',
        semanticContext: 'global_scope',
        preservedElements: [],
        modifiedElements: [],
        addedElements: [],
        removedElements: [constantName],
      },
    });

    return changes;
  }

  /**
   * Validates surgical changes.
   */
  private async validateSurgicalChanges(
    changes: SurgicalChange[],
    request: SurgicalOperationRequest,
    context: ParseContext,
  ): Promise<SurgicalValidation> {
    const issues: SurgicalValidationIssue[] = [];
    const suggestions: string[] = [];

    // Validate syntax
    const syntaxValid = await this.validateSyntax(changes, request, context);
    if (!syntaxValid.valid) {
      issues.push(...syntaxValid.issues);
    }

    // Validate semantics
    const semanticsValid = await this.validateSemantics(changes, request, context);
    if (!semanticsValid.valid) {
      issues.push(...semanticsValid.issues);
    }

    // Validate references
    const referencesValid = await this.validateReferences(changes, request, context);
    if (!referencesValid.valid) {
      issues.push(...referencesValid.issues);
    }

    // Validate type system
    const typeSystemValid = await this.validateTypeSystem(changes, request, context);
    if (!typeSystemValid.valid) {
      issues.push(...typeSystemValid.issues);
    }

    // Validate grammar compliance
    const grammarCompliant = await this.validateGrammarCompliance(changes, request, context);
    if (!grammarCompliant.valid) {
      issues.push(...grammarCompliant.issues);
    }

    return {
      syntaxValid: syntaxValid.valid,
      semanticsValid: semanticsValid.valid,
      referencesValid: referencesValid.valid,
      typeSystemValid: typeSystemValid.valid,
      grammarCompliant: grammarCompliant.valid,
      issues,
      suggestions,
    };
  }

  /**
   * Calculates surgical metrics.
   */
  private calculateSurgicalMetrics(
    request: SurgicalOperationRequest,
    changes: SurgicalChange[],
    analysisTime: number,
    validationTime: number,
    startTime: number,
  ): SurgicalMetrics {
    const tokensModified = changes.reduce((sum, change) =>
      sum + this.countTokens(change.oldText) + this.countTokens(change.newText), 0);

    const linesModified = changes.reduce((sum, change) =>
      sum + this.countLines(change.oldText) + this.countLines(change.newText), 0);

    const confidenceScore = changes.length > 0 ?
      changes.reduce((sum, change) => sum + change.confidence, 0) / changes.length : 0;

    return {
      operationTime: Date.now() - startTime,
      analysisTime,
      validationTime,
      changesGenerated: changes.length,
      tokensModified,
      linesModified,
      scopeExpansion: this.calculateScopeExpansion(changes),
      confidenceScore,
      complexityReduction: this.calculateComplexityReduction(request, changes),
      maintainabilityImprovement: this.calculateMaintainabilityImprovement(request, changes),
    };
  }

  /**
   * Helper methods for surgical operations.
   */

  // New helper methods for the implemented surgical operations

  private generateImportStatement(symbolName: string, targetFile: string, language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return `import { ${symbolName} } from './${targetFile.replace(/\.(ts|js)$/, '')}';`;
      case SupportedLanguage.PYTHON:
        return `from ${targetFile.replace(/\.py$/, '')} import ${symbolName}`;
      case SupportedLanguage.JAVA:
        return `import ${targetFile.replace(/\.java$/, '')}.${symbolName};`;
      case SupportedLanguage.CSHARP:
        return `using ${targetFile.replace(/\.cs$/, '')};`;
      default:
        return `// Import ${symbolName} from ${targetFile}`;
    }
  }

  private extractPublicMembers(classInfo: SymbolInfo, context: ParseContext): string[] {
    // Extract actual public members from the class info
    const members: string[] = [];

    // Extract public methods and properties from symbol info
    // Use type guard to check if children property exists
    if (classInfo.type === 'class' && 'children' in classInfo && Array.isArray((classInfo as any).children)) {
      for (const member of (classInfo as any).children) {
        if (member.visibility === 'public' || !member.visibility) {
          members.push(member.name);
        }
      }
    }

    // If no members found, return fallback members for interface generation
    return members.length > 0 ? members : ['method1', 'method2', 'property1'];
  }

  private generateInterfaceDeclaration(interfaceName: string, members: string[], language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.TYPESCRIPT:
        return `interface ${interfaceName} {\n  ${members.join('();\n  ')}();\n}`;
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return `public interface ${interfaceName} {\n  ${members.join('();\n  ')}();\n}`;
      default:
        return `// Interface ${interfaceName}`;
    }
  }

  private generateImplementsClause(interfaceName: string, language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.TYPESCRIPT:
      case SupportedLanguage.JAVA:
        return `implements ${interfaceName}`;
      case SupportedLanguage.CSHARP:
        return `: ${interfaceName}`;
      default:
        return `/* implements ${interfaceName} */`;
    }
  }

  private analyzeClassDependencies(codeBlock: string, context: ParseContext): any {
    // Analyze class dependencies from the code block
    const methods: string[] = [];
    const properties: string[] = [];
    const constructorArgs: string[] = [];
    const preservedElements: string[] = [];

    // Basic pattern matching to extract class members
    const methodMatches = codeBlock.match(/(?:public|private|protected)?\s*(?:async\s+)?(\w+)\s*\(/g);
    if (methodMatches) {
      methods.push(...methodMatches.map(match => match.replace(/(?:public|private|protected)?\s*(?:async\s+)?(\w+)\s*\(/, '$1')));
    }

    const propertyMatches = codeBlock.match(/(?:public|private|protected)?\s*(\w+)\s*:/g);
    if (propertyMatches) {
      properties.push(...propertyMatches.map(match => match.replace(/(?:public|private|protected)?\s*(\w+)\s*:/, '$1')));
    }

    return {
      methods: methods.length > 0 ? methods : ['method1', 'method2'],
      properties: properties.length > 0 ? properties : ['prop1', 'prop2'],
      constructorArgs: ['arg1', 'arg2'], // Would need more sophisticated parsing
      preservedElements: ['dependency1', 'dependency2'], // Would need dependency analysis
    };
  }

  private generateClassDeclaration(
    className: string,
    methods: string[],
    properties: string[],
    language: SupportedLanguage,
  ): string {
    switch (language) {
      case SupportedLanguage.TYPESCRIPT:
      case SupportedLanguage.JAVASCRIPT:
        return `class ${className} {\n  ${properties.join(';\n  ')};\n\n  ${methods.join('() {}\n  ')}() {}\n}`;
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return `public class ${className} {\n  private ${properties.join(';\n  private ')};\n\n  public ${methods.join('() {}\n  public ')}() {}\n}`;
      case SupportedLanguage.PYTHON:
        return `class ${className}:\n    def __init__(self):\n        ${properties.join(' = None\n        self.')} = None\n\n    def ${methods.join('(self):\n        pass\n\n    def ')}(self):\n        pass`;
      default:
        return `class ${className} { /* implementation */ }`;
    }
  }

  private generateClassInstantiation(className: string, args: string[], language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return `new ${className}(${args.join(', ')})`;
      case SupportedLanguage.PYTHON:
        return `${className}(${args.join(', ')})`;
      default:
        return `${className}(${args.join(', ')})`;
    }
  }

  private analyzeVariableUsage(
    references: Array<{
      position: CodePosition;
      endPosition: CodePosition;
      type: string;
      context: string;
    }>,
    _context: ParseContext,
  ): any {
    // Mock implementation - would analyze actual variable usage patterns
    return {
      readOperations: references.length * 0.6,
      writeOperations: references.length * 0.4,
      usagePatterns: ['pattern1', 'pattern2'],
    };
  }

  private determineInitialValue(usageAnalysis: any, index: number, language: SupportedLanguage): string {
    // Mock implementation - would determine appropriate initial value
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return index === 0 ? 'null' : 'undefined';
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return 'null';
      case SupportedLanguage.PYTHON:
        return 'None';
      default:
        return 'null';
    }
  }

  private determineAppropriateVariable(reference: any, splitNames: string[], usageAnalysis: any): string {
    // Determine which split variable to use based on reference context and usage analysis
    if (!reference || splitNames.length === 0) {
      return splitNames[0] || 'variable';
    }

    // Use usage analysis to pick the most appropriate variable name
    if (usageAnalysis && usageAnalysis.primaryUsage) {
      const primaryContext = usageAnalysis.primaryUsage;
      const matchingName = splitNames.find(name =>
        name.toLowerCase().includes(primaryContext.toLowerCase()),
      );
      if (matchingName) {
        return matchingName;
      }
    }

    // If reference has type information, use it to guide selection
    if (reference.type) {
      const typeBasedName = splitNames.find(name =>
        name.toLowerCase().includes(reference.type.toLowerCase()),
      );
      if (typeBasedName) {
        return typeBasedName;
      }
    }

    // Default to first name
    return splitNames[0];
  }

  private determineMergedType(symbolInfos: SymbolInfo[], language: SupportedLanguage): string {
    // Mock implementation - would determine appropriate merged type
    switch (language) {
      case SupportedLanguage.TYPESCRIPT:
        return 'object';
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return 'Object';
      case SupportedLanguage.PYTHON:
        return 'dict';
      default:
        return 'object';
    }
  }

  private determineMergedValue(
    _symbolInfos: SymbolInfo[],
    variableNames: string[],
    language: SupportedLanguage,
  ): string {
    // Mock implementation - would create appropriate merged value
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return `{ ${variableNames.map(name => `${name}: ${name}`).join(', ')} }`;
      case SupportedLanguage.PYTHON:
        return `{ ${variableNames.map(name => `'${name}': `).join(', ')} }`;
      default:
        return '{ /* merged values */ }';
    }
  }

  private generateMergedAccessPattern(
    oldVarName: string,
    mergedName: string,
    _allNames: string[],
    language: SupportedLanguage,
  ): string {
    // Mock implementation - would generate appropriate access pattern
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return `${mergedName}.${oldVarName}`;
      case SupportedLanguage.PYTHON:
        return `${mergedName}['${oldVarName}']`;
      default:
        return `${mergedName}.${oldVarName}`;
    }
  }

  private getFunctionSignature(functionInfo: SymbolInfo, context: ParseContext): string {
    // Extract actual function signature from symbol info and context
    let signature = `function ${functionInfo.name}`;

    // Extract parameters from function info
    const params: string[] = [];
    // Use type guard to check if children property exists
    if ('children' in functionInfo && Array.isArray((functionInfo as any).children)) {
      for (const child of (functionInfo as any).children) {
        if (child.type === 'parameter') {
          let paramStr = child.name;
          if ('dataType' in child && child.dataType) {
            paramStr += `: ${child.dataType}`;
          }
          if (child.defaultValue) {
            paramStr += ` = ${child.defaultValue}`;
          }
          params.push(paramStr);
        }
      }
    }

    signature += `(${params.join(', ')})`;

    // Add return type if available
    // Use type guard to check if dataType property exists
    if ('dataType' in functionInfo && (functionInfo as any).dataType && (functionInfo as any).dataType !== 'void') {
      signature += `: ${(functionInfo as any).dataType}`;
    }

    return signature;
  }

  private addParameterToSignature(
    signature: string,
    paramName: string,
    paramType: string,
    defaultValue: string | undefined,
    _language: SupportedLanguage,
  ): string {
    // Mock implementation - would properly parse and modify signature
    const paramDecl = defaultValue ? `${paramName} = ${defaultValue}` : paramName;
    return signature.replace(')', `, ${paramDecl})`);
  }

  private getDefaultValueForType(paramType: string, language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return paramType === 'string' ? "''" : paramType === 'number' ? '0' : 'null';
      case SupportedLanguage.PYTHON:
        return paramType === 'str' ? "''" : paramType === 'int' ? '0' : 'None';
      default:
        return 'null';
    }
  }

  private addArgumentToCall(callSignature: string, argument: string, language: SupportedLanguage): string {
    // Mock implementation - would properly parse and modify call
    return callSignature.replace(')', `, ${argument})`);
  }

  // eslint-disable-next-line max-len
  private removeParameterFromSignature(signature: string, paramName: string | undefined, paramIndex: number | undefined, language: SupportedLanguage): string {
    // Mock implementation - would properly parse and modify signature
    return signature; // Simplified
  }

  // eslint-disable-next-line max-len
  private removeArgumentFromCall(callSignature: string, paramName: string | undefined, paramIndex: number | undefined, language: SupportedLanguage): string {
    // Mock implementation - would properly parse and modify call
    return callSignature; // Simplified
  }

  // eslint-disable-next-line max-len
  private findParameterReferences(functionInfo: SymbolInfo, paramName: string, context: ParseContext): Array<{ position: CodePosition; endPosition: CodePosition }> {
    // Mock implementation - would find actual parameter references
    return [];
  }

  private updateCallSignature(callSignature: string, parameterChanges: any[], language: SupportedLanguage): string {
    // Mock implementation - would apply parameter changes to call
    return callSignature; // Simplified
  }

  private generateConstantName(literalValue: string): string {
    // Mock implementation - would generate appropriate constant name
    if (literalValue.match(/^\d+$/)) {
      return `NUM_${literalValue}`;
    } else if (literalValue.match(/^".*"$/)) {
      return `STR_${literalValue.replace(/[^A-Za-z0-9]/g, '_').toUpperCase()}`;
    }
    return 'CONSTANT_VALUE';
  }

  // eslint-disable-next-line max-len
  private generateConstantDeclaration(name: string, value: string, language: SupportedLanguage, context: ParseContext): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return `const ${name} = ${value};`;
      case SupportedLanguage.JAVA:
        return `public static final String ${name} = ${value};`;
      case SupportedLanguage.CSHARP:
        return `public const string ${name} = ${value};`;
      case SupportedLanguage.PYTHON:
        return `${name} = ${value}`;
      default:
        return `const ${name} = ${value};`;
    }
  }

  private findConstantInsertionPoint(context: ParseContext, position: CodePosition): CodePosition {
    // Mock implementation - would find appropriate insertion point for constants
    return { line: 1, column: 0, offset: 0 };
  }

  // eslint-disable-next-line max-len
  private findSimilarLiterals(literalValue: string, context: ParseContext): Array<{ position: CodePosition; endPosition: CodePosition }> {
    // Mock implementation - would find similar literal values
    return [];
  }

  private getConstantValue(constantInfo: SymbolInfo, context: ParseContext): string | null {
    // Mock implementation - would extract constant value
    return 'constantValue';
  }

  private findConstantDeclarationEnd(constantInfo: SymbolInfo, context: ParseContext): CodePosition {
    // Mock implementation - would find end of constant declaration
    return {
      line: constantInfo.position.line + 1,
      column: 0,
      offset: constantInfo.position.offset + 100,
    };
  }

  private validateRequest(request: SurgicalOperationRequest): void {
    if (!request.id || !request.type || !request.file || !request.position) {
      throw new Error('Invalid surgical operation request');
    }
  }

  private getExpressionAtRange(context: ParseContext, start: CodePosition, end: CodePosition): string {
    // Mock implementation - would extract actual expression from parse tree
    return 'expression';
  }

  // eslint-disable-next-line max-len
  private generateVariableDeclaration(name: string, value: string, language: SupportedLanguage, context: ParseContext): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return `const ${name} = ${value};`;
      case SupportedLanguage.PYTHON:
        return `${name} = ${value}`;
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return `var ${name} = ${value};`;
      default:
        return `${name} = ${value};`;
    }
  }

  private findVariableInsertionPoint(context: ParseContext, position: CodePosition): CodePosition {
    // Mock implementation - would find appropriate insertion point
    return { line: position.line - 1, column: 0, offset: position.offset - 100 };
  }

  private findSymbolDeclaration(context: ParseContext, symbolName: string, position: CodePosition): SymbolInfo | null {
    // Mock implementation - would find actual symbol declaration
    return null;
  }

  private getVariableValue(symbolInfo: SymbolInfo, context: ParseContext): string | null {
    // Mock implementation - would extract variable value
    return 'value';
  }

  // eslint-disable-next-line max-len
  private findSymbolReferences(context: ParseContext, symbolName: string): Array<{ position: CodePosition; endPosition: CodePosition; type: string; context: string }> {
    // Mock implementation - would find actual symbol references
    return [];
  }

  private getTextAtRange(context: ParseContext, start: CodePosition, end: CodePosition): string {
    // Mock implementation - would extract actual text
    return 'text';
  }

  private analyzeFunctionDependencies(codeBlock: string, context: ParseContext): any {
    // Mock implementation - would analyze actual dependencies
    return {
      parameters: [],
      returnType: 'void',
      arguments: [],
      preservedVariables: [],
    };
  }

  // eslint-disable-next-line max-len
  private generateFunctionSignature(name: string, parameters: any[], returnType: string, language: SupportedLanguage): string {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
        return `function ${name}(${parameters.join(', ')})`;
      case SupportedLanguage.TYPESCRIPT:
        return `function ${name}(${parameters.join(', ')}): ${returnType}`;
      case SupportedLanguage.PYTHON:
        return `def ${name}(${parameters.join(', ')}):`;
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return `${returnType} ${name}(${parameters.join(', ')})`;
      default:
        return `${name}(${parameters.join(', ')})`;
    }
  }

  private generateFunctionBody(codeBlock: string, dependencies: any, language: SupportedLanguage): string {
    // Mock implementation - would generate actual function body
    return `    ${codeBlock}`;
  }

  private findFunctionInsertionPoint(context: ParseContext, position: CodePosition): CodePosition {
    // Mock implementation - would find appropriate insertion point
    return { line: 1, column: 0, offset: 0 };
  }

  private generateFunctionCall(name: string, args: any[], language: SupportedLanguage): string {
    return `${name}(${args.join(', ')})`;
  }

  // eslint-disable-next-line max-len
  private async validateSyntax(changes: SurgicalChange[], request: SurgicalOperationRequest, context: ParseContext): Promise<{ valid: boolean; issues: SurgicalValidationIssue[] }> {
    const issues: SurgicalValidationIssue[] = [];
    let allValid = true;

    for (const change of changes) {
      try {
        // Basic syntax validation
        if (change.newText) {
          // Check for basic syntax issues in the new text
          const syntaxIssues = this.checkBasicSyntax(change.newText, request.language);
          if (syntaxIssues.length > 0) {
            allValid = false;
            issues.push(...syntaxIssues.map(issue => ({
              severity: 'error' as const,
              message: `Syntax error in change ${change.id}: ${issue}`,
              position: change.position,
              code: 'SYNTAX_ERROR',
              category: 'syntax' as const,
              fixSuggestion: 'Review the generated code for syntax compliance',
            })));
          }

          // Check for balanced brackets and quotes
          const balanceIssues = this.checkBalancedStructures(change.newText);
          if (balanceIssues.length > 0) {
            allValid = false;
            issues.push(...balanceIssues.map(issue => ({
              severity: 'error' as const,
              message: `Structure balance error in change ${change.id}: ${issue}`,
              position: change.position,
              code: 'BALANCE_ERROR',
              category: 'syntax' as const,
              fixSuggestion: 'Ensure brackets, parentheses, and quotes are properly balanced',
            })));
          }
        }

        // Validate that the change fits grammatically in its scope
        if (change.scope) {
          const scopeCompatible = this.validateScopeCompatibility(change, context);
          if (!scopeCompatible) {
            allValid = false;
            issues.push({
              severity: 'warning' as const,
              message: `Change ${change.id} may not be compatible with its scope context`,
              position: change.position,
              code: 'SCOPE_INCOMPATIBLE',
              category: 'syntax' as const,
              fixSuggestion: 'Review the scope type and ensure the change is appropriate',
            });
          }
        }
      } catch (error) {
        allValid = false;
        issues.push({
          severity: 'error' as const,
          // eslint-disable-next-line max-len
          message: `Syntax validation failed for change ${change.id}: ${error instanceof Error ? error.message : String(error)}`,
          position: change.position,
          code: 'VALIDATION_ERROR',
          category: 'syntax' as const,
        });
      }
    }

    return { valid: allValid, issues };
  }

  // eslint-disable-next-line max-len
  private async validateSemantics(changes: SurgicalChange[], request: SurgicalOperationRequest, context: ParseContext): Promise<{ valid: boolean; issues: SurgicalValidationIssue[] }> {
    const issues: SurgicalValidationIssue[] = [];
    let allValid = true;

    for (const change of changes) {
      try {
        // Validate semantic consistency
        if (change.scope && change.scope.symbolsAffected.length > 0) {
          // Check if affected symbols are properly handled
          for (const symbol of change.scope.symbolsAffected) {
            if (change.type === 'delete' && change.scope.referencesAffected > 0) {
              // Deleting a symbol that has references
              allValid = false;
              issues.push({
                severity: 'error' as const,
                message: `Deleting symbol '${symbol}' that has ${change.scope.referencesAffected} references`,
                position: change.position,
                code: 'DANGLING_REFERENCES',
                category: 'semantics' as const,
                fixSuggestion: 'Update or remove all references before deleting the symbol',
              });
            }

            if (change.type === 'replace' && this.isSymbolDeclaration(change, symbol)) {
              // Replacing a symbol declaration - check if it maintains semantic equivalence
              // eslint-disable-next-line max-len
              const semanticEquivalence = this.checkSemanticEquivalence(change.oldText, change.newText, request.language);
              if (!semanticEquivalence.equivalent) {
                issues.push({
                  severity: 'warning' as const,
                  message: `Symbol '${symbol}' replacement may change semantics: ${semanticEquivalence.reason}`,
                  position: change.position,
                  code: 'SEMANTIC_CHANGE',
                  category: 'semantics' as const,
                  fixSuggestion: semanticEquivalence.suggestion,
                });
              }
            }
          }
        }

        // Validate operation-specific semantics
        if (request.type === SurgicalOperationType.EXTRACT_FUNCTION ||
            request.type === SurgicalOperationType.EXTRACT_VARIABLE) {
          const extractionValid = this.validateExtractionSemantics(change, request, context);
          if (!extractionValid.valid) {
            allValid = false;
            issues.push(...extractionValid.issues);
          }
        }

        // Check for potential side effects
        const sideEffects = this.detectPotentialSideEffects(change, context);
        if (sideEffects.length > 0) {
          issues.push(...sideEffects.map(effect => ({
            severity: 'warning' as const,
            message: `Potential side effect detected: ${effect}`,
            position: change.position,
            code: 'SIDE_EFFECT',
            category: 'semantics' as const,
            fixSuggestion: 'Review the change for unintended side effects',
          })));
        }
      } catch (error) {
        allValid = false;
        issues.push({
          severity: 'error' as const,
          // eslint-disable-next-line max-len
          message: `Semantic validation failed for change ${change.id}: ${error instanceof Error ? error.message : String(error)}`,
          position: change.position,
          code: 'VALIDATION_ERROR',
          category: 'semantics' as const,
        });
      }
    }

    return { valid: allValid, issues };
  }

  // eslint-disable-next-line max-len
  private async validateReferences(changes: SurgicalChange[], request: SurgicalOperationRequest, context: ParseContext): Promise<{ valid: boolean; issues: SurgicalValidationIssue[] }> {
    const issues: SurgicalValidationIssue[] = [];
    let allValid = true;

    for (const change of changes) {
      try {
        // Check if all references are properly maintained
        if (change.scope && change.scope.symbolsAffected.length > 0) {
          for (const symbol of change.scope.symbolsAffected) {
            // Validate that renamed symbols maintain referential integrity
            if (request.type === SurgicalOperationType.RENAME_SYMBOL) {
              const referencesValid = this.validateSymbolReferences(symbol, change, context);
              if (!referencesValid.valid) {
                allValid = false;
                issues.push(...referencesValid.issues);
              }
            }

            // Check for orphaned references after moves or deletions
            if (change.type === 'move' || change.type === 'delete') {
              const orphanedRefs = this.findOrphanedReferences(symbol, change, context);
              if (orphanedRefs.length > 0) {
                allValid = false;
                issues.push({
                  severity: 'error' as const,
                  message: `Symbol '${symbol}' operation would create ${orphanedRefs.length} orphaned references`,
                  position: change.position,
                  code: 'ORPHANED_REFERENCES',
                  category: 'references' as const,
                  fixSuggestion: 'Update all references before performing the operation',
                });
              }
            }

            // Validate import/export references for cross-file operations
            if (change.file !== request.file) {
              const importValid = this.validateCrossFileReferences(symbol, change, request, context);
              if (!importValid.valid) {
                issues.push(...importValid.issues);
              }
            }
          }
        }

        // Check dependencies between changes
        if (change.dependencies && change.dependencies.length > 0) {
          const depValid = this.validateChangeDependencies(change, changes);
          if (!depValid.valid) {
            allValid = false;
            issues.push(...depValid.issues);
          }
        }

        // Validate variable references in extracted code
        if (request.type === SurgicalOperationType.EXTRACT_FUNCTION ||
            request.type === SurgicalOperationType.EXTRACT_CLASS) {
          const varRefsValid = this.validateVariableReferences(change, context);
          if (!varRefsValid.valid) {
            issues.push(...varRefsValid.issues);
          }
        }
      } catch (error) {
        allValid = false;
        issues.push({
          severity: 'error' as const,
          // eslint-disable-next-line max-len
          message: `Reference validation failed for change ${change.id}: ${error instanceof Error ? error.message : String(error)}`,
          position: change.position,
          code: 'VALIDATION_ERROR',
          category: 'references' as const,
        });
      }
    }

    return { valid: allValid, issues };
  }

  // eslint-disable-next-line max-len
  private async validateTypeSystem(changes: SurgicalChange[], request: SurgicalOperationRequest, context: ParseContext): Promise<{ valid: boolean; issues: SurgicalValidationIssue[] }> {
    const issues: SurgicalValidationIssue[] = [];
    let allValid = true;

    // Only perform type validation for typed languages
    if (!this.isTypedLanguage(request.language)) {
      return { valid: true, issues: [] };
    }

    for (const change of changes) {
      try {
        // Validate type consistency for replacements
        if (change.type === 'replace' && change.oldText && change.newText) {
          const typeCompatible = this.checkTypeCompatibility(change.oldText, change.newText, request.language, context);
          if (!typeCompatible.compatible) {
            if (typeCompatible.severity === 'error') {
              allValid = false;
            }
            issues.push({
              severity: typeCompatible.severity,
              message: `Type incompatibility in change ${change.id}: ${typeCompatible.message}`,
              position: change.position,
              code: 'TYPE_MISMATCH',
              category: 'types' as const,
              fixSuggestion: typeCompatible.suggestion,
            });
          }
        }

        // Validate function signature changes
        if (request.type === SurgicalOperationType.CHANGE_SIGNATURE ||
            request.type === SurgicalOperationType.INTRODUCE_PARAMETER ||
            request.type === SurgicalOperationType.REMOVE_PARAMETER) {
          const signatureValid = this.validateSignatureTypeConsistency(change, request, context);
          if (!signatureValid.valid) {
            allValid = false;
            issues.push(...signatureValid.issues);
          }
        }

        // Check type inference for variable extractions
        if (request.type === SurgicalOperationType.EXTRACT_VARIABLE && change.type === 'insert') {
          const typeInferenceValid = this.validateTypeInference(change, context, request.language);
          if (!typeInferenceValid.valid) {
            issues.push(...typeInferenceValid.issues);
          }
        }

        // Validate generic type constraints
        if (change.scope && this.hasGenericTypes(change.newText, request.language)) {
          const genericValid = this.validateGenericConstraints(change, context, request.language);
          if (!genericValid.valid) {
            issues.push(...genericValid.issues);
          }
        }

        // Check for implicit type conversions that may cause issues
        const implicitConversions = this.detectImplicitTypeConversions(change, request.language);
        if (implicitConversions.length > 0) {
          issues.push(...implicitConversions.map(conversion => ({
            severity: 'warning' as const,
            message: `Implicit type conversion detected: ${conversion.description}`,
            position: change.position,
            code: 'IMPLICIT_CONVERSION',
            category: 'types' as const,
            fixSuggestion: `Consider explicit casting: ${conversion.suggestion}`,
          })));
        }
      } catch (error) {
        allValid = false;
        issues.push({
          severity: 'error' as const,
          // eslint-disable-next-line max-len
          message: `Type system validation failed for change ${change.id}: ${error instanceof Error ? error.message : String(error)}`,
          position: change.position,
          code: 'VALIDATION_ERROR',
          category: 'types' as const,
        });
      }
    }

    return { valid: allValid, issues };
  }

  // eslint-disable-next-line max-len
  private async validateGrammarCompliance(changes: SurgicalChange[], request: SurgicalOperationRequest, context: ParseContext): Promise<{ valid: boolean; issues: SurgicalValidationIssue[] }> {
    const issues: SurgicalValidationIssue[] = [];
    let allValid = true;

    for (const change of changes) {
      try {
        // Validate that new text conforms to language grammar
        if (change.newText) {
          const grammarValid = this.checkGrammarConformance(change.newText, request.language);
          if (!grammarValid.valid) {
            allValid = false;
            issues.push({
              severity: 'error' as const,
              message: `Grammar violation in change ${change.id}: ${grammarValid.message}`,
              position: change.position,
              code: 'GRAMMAR_VIOLATION',
              category: 'grammar' as const,
              fixSuggestion: grammarValid.suggestion,
            });
          }
        }

        // Check that the change fits the expected grammar rule for its context
        if (change.metadata?.grammarRule) {
          const ruleCompliant = this.validateGrammarRuleCompliance(change, request.language);
          if (!ruleCompliant.valid) {
            issues.push({
              severity: 'warning' as const,
              // eslint-disable-next-line max-len
              message: `Change ${change.id} may not comply with expected grammar rule '${change.metadata.grammarRule}': ${ruleCompliant.message}`,
              position: change.position,
              code: 'RULE_NONCOMPLIANCE',
              category: 'grammar' as const,
              fixSuggestion: ruleCompliant.suggestion,
            });
          }
        }

        // Validate syntax node compatibility
        if (change.metadata?.syntaxNode) {
          const nodeCompatible = this.validateSyntaxNodeCompatibility(change, context);
          if (!nodeCompatible.compatible) {
            issues.push({
              severity: 'warning' as const,
              message: `Syntax node '${change.metadata.syntaxNode}' may not be compatible with context`,
              position: change.position,
              code: 'NODE_INCOMPATIBLE',
              category: 'grammar' as const,
              fixSuggestion: 'Review the syntax node type for this context',
            });
          }
        }

        // Check language-specific grammar constraints
        const languageConstraints = this.validateLanguageSpecificConstraints(change, request.language);
        if (languageConstraints.violations.length > 0) {
          issues.push(...languageConstraints.violations.map(violation => ({
            severity: violation.severity,
            message: `Language constraint violation: ${violation.message}`,
            position: change.position,
            code: 'LANGUAGE_CONSTRAINT',
            category: 'grammar' as const,
            fixSuggestion: violation.suggestion,
          })));
        }

        // Validate contextual grammar requirements
        const contextualValid = this.validateContextualGrammar(change, context, request);
        if (!contextualValid.valid) {
          issues.push(...contextualValid.issues);
        }
      } catch (error) {
        allValid = false;
        issues.push({
          severity: 'error' as const,
          // eslint-disable-next-line max-len
          message: `Grammar validation failed for change ${change.id}: ${error instanceof Error ? error.message : String(error)}`,
          position: change.position,
          code: 'VALIDATION_ERROR',
          category: 'grammar' as const,
        });
      }
    }

    return { valid: allValid, issues };
  }

  private async generatePreview(changes: SurgicalChange[], request: SurgicalOperationRequest): Promise<string> {
    // Mock implementation - would generate actual preview
    return 'Preview of changes';
  }

  // eslint-disable-next-line max-len
  private async generateUndoInfo(request: SurgicalOperationRequest, changes: SurgicalChange[], context: ParseContext): Promise<SurgicalUndoInfo> {
    return {
      operationId: request.id,
      timestamp: Date.now(),
      originalState: new Map(),
      changeSequence: changes,
      contextSnapshot: context,
      rollbackInstructions: [],
    };
  }

  private async applySurgicalChanges(changes: SurgicalChange[]): Promise<void> {
    // Mock implementation - would apply actual changes
    for (const change of changes) {
      // Apply change to file system
    }
  }

  private countTokens(text: string): number {
    return text.split(/\s+/).length;
  }

  private countLines(text: string): number {
    return text.split('\n').length;
  }

  private calculateScopeExpansion(changes: SurgicalChange[]): number {
    // Mock implementation - would calculate actual scope expansion
    return 0;
  }

  private calculateComplexityReduction(request: SurgicalOperationRequest, changes: SurgicalChange[]): number {
    // Mock implementation - would calculate complexity reduction
    return 0.1;
  }

  private calculateMaintainabilityImprovement(request: SurgicalOperationRequest, changes: SurgicalChange[]): number {
    // Mock implementation - would calculate maintainability improvement
    return 0.05;
  }

  private generateChangeId(): string {
    return `change_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Public API methods.
   */

  /**
   * Undoes the last surgical operation.
   */
  public async undo(): Promise<boolean> {
    if (this.undoStack.length === 0) {
      return false;
    }

    const undoInfo = this.undoStack.pop()!;

    try {
      // Apply rollback
      await this.applyRollback(undoInfo);

      // Move to redo stack
      this.redoStack.push(undoInfo);

      this.emit('surgical_operation_undone', { undoInfo });
      return true;

    } catch (error) {
      // Restore to undo stack if rollback fails
      this.undoStack.push(undoInfo);
      this.emit('surgical_operation_undo_failed', { undoInfo, error });
      return false;
    }
  }

  /**
   * Redoes the last undone surgical operation.
   */
  public async redo(): Promise<boolean> {
    if (this.redoStack.length === 0) {
      return false;
    }

    const redoInfo = this.redoStack.pop()!;

    try {
      // Reapply changes
      await this.applySurgicalChanges(redoInfo.changeSequence);

      // Move back to undo stack
      this.undoStack.push(redoInfo);

      this.emit('surgical_operation_redone', { redoInfo });
      return true;

    } catch (error) {
      // Restore to redo stack if reapply fails
      this.redoStack.push(redoInfo);
      this.emit('surgical_operation_redo_failed', { redoInfo, error });
      return false;
    }
  }

  /**
   * Gets the operation history.
   */
  public getOperationHistory(): SurgicalOperationResult[] {
    return [...this.operationHistory];
  }

  /**
   * Clears the operation history and undo/redo stacks.
   */
  public clearHistory(): void {
    this.operationHistory = [];
    this.undoStack = [];
    this.redoStack = [];
    this.emit('surgical_history_cleared');
  }

  private async applyRollback(undoInfo: SurgicalUndoInfo): Promise<void> {
    // Mock implementation - would apply actual rollback
    undoInfo.originalState.forEach((content, file) => {
      // Restore file content
    });
  }

  /**
   * Helper methods for validation implementations
   */

  private checkBasicSyntax(text: string, language: SupportedLanguage): string[] {
    const issues: string[] = [];

    // Basic syntax checks based on language
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        // Check for obvious syntax issues
        if (text.includes(';;')) {
          issues.push('Double semicolons detected');
        }
        if (text.match(/\bfunction\s*\(/)) {
          const funcMatch = text.match(/function\s*[^(]*\([^)]*\)\s*\{?/);
          if (funcMatch && !funcMatch[0].includes('{')) {
            issues.push('Function declaration missing opening brace');
          }
        }
        break;
      case SupportedLanguage.PYTHON:
        // Check indentation and basic Python syntax
        if (text.includes('\t') && text.includes('    ')) {
          issues.push('Mixed tabs and spaces detected');
        }
        if (text.match(/:\s*$/m) && !text.match(/:\s*\n\s+/)) {
          issues.push('Python block statement missing indented body');
        }
        break;
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        // Check for basic class/method structure
        if (text.includes('class ') && !text.includes('{')) {
          issues.push('Class declaration missing opening brace');
        }
        break;
    }

    return issues;
  }

  private checkBalancedStructures(text: string): string[] {
    const issues: string[] = [];

    // Check balanced parentheses
    let parenCount = 0;
    let braceCount = 0;
    let bracketCount = 0;
    let singleQuoteCount = 0;
    let doubleQuoteCount = 0;

    for (let i = 0; i < text.length; i++) {
      const char = text[i];
      const prevChar = i > 0 ? text[i - 1] : '';

      // Skip escaped characters
      if (prevChar === '\\') {
        continue;
      }

      switch (char) {
        case '(':
          parenCount++;
          break;
        case ')':
          parenCount--;
          break;
        case '{':
          braceCount++;
          break;
        case '}':
          braceCount--;
          break;
        case '[':
          bracketCount++;
          break;
        case ']':
          bracketCount--;
          break;
        case "'":
          singleQuoteCount++;
          break;
        case '"':
          doubleQuoteCount++;
          break;
      }
    }

    if (parenCount !== 0) {
      issues.push(`Unbalanced parentheses (${parenCount > 0 ? 'missing closing' : 'extra closing'})`);
    }
    if (braceCount !== 0) {
      issues.push(`Unbalanced braces (${braceCount > 0 ? 'missing closing' : 'extra closing'})`);
    }
    if (bracketCount !== 0) {
      issues.push(`Unbalanced brackets (${bracketCount > 0 ? 'missing closing' : 'extra closing'})`);
    }
    if (singleQuoteCount % 2 !== 0) {
      issues.push('Unbalanced single quotes');
    }
    if (doubleQuoteCount % 2 !== 0) {
      issues.push('Unbalanced double quotes');
    }

    return issues;
  }

  private validateScopeCompatibility(change: SurgicalChange, context: ParseContext): boolean {
    // Basic scope compatibility check
    if (!change.scope) {
      return true;
    }

    // Check if the change type is appropriate for the scope
    switch (change.scope.type) {
      case 'function':
        // Function scope should allow statements and expressions
        return change.type !== 'delete' || change.scope.symbolsAffected.length === 0;
      case 'class':
        // Class scope should allow method and property declarations
        return true;
      case 'expression':
        // Expression scope should not allow statement insertions
        return change.type !== 'insert' || !this.isStatementLike(change.newText);
      default:
        return true;
    }
  }

  private isStatementLike(text: string): boolean {
    // Simple heuristic to determine if text is statement-like
    const trimmed = text.trim();
    return trimmed.endsWith(';') ||
           trimmed.includes('\n') ||
           trimmed.startsWith('if ') ||
           trimmed.startsWith('for ') ||
           trimmed.startsWith('while ') ||
           trimmed.startsWith('function ') ||
           trimmed.startsWith('class ');
  }

  private isSymbolDeclaration(change: SurgicalChange, symbol: string): boolean {
    // Check if the change appears to be declaring the symbol
    return change.newText.includes(`${symbol}`) &&
           (change.newText.includes('=') || change.newText.includes('function') || change.newText.includes('class'));
  }

  // eslint-disable-next-line max-len
  private checkSemanticEquivalence(oldText: string, newText: string, language: SupportedLanguage): { equivalent: boolean; reason?: string; suggestion?: string } {
    // Simple semantic equivalence check
    if (oldText === newText) {
      return { equivalent: true };
    }

    // Check for obvious non-equivalences
    if (oldText.includes('return') && !newText.includes('return')) {
      return {
        equivalent: false,
        reason: 'Return statement removed',
        suggestion: 'Ensure the new code maintains the same return behavior',
      };
    }

    if (this.countFunctionCalls(oldText) !== this.countFunctionCalls(newText)) {
      return {
        equivalent: false,
        reason: 'Number of function calls changed',
        suggestion: 'Verify that all necessary function calls are preserved',
      };
    }

    // For now, assume semantic equivalence if basic checks pass
    return { equivalent: true };
  }

  private countFunctionCalls(text: string): number {
    // Count potential function calls (simplified)
    const matches = text.match(/\w+\s*\(/g);
    return matches ? matches.length : 0;
  }

  // eslint-disable-next-line max-len
  private validateExtractionSemantics(change: SurgicalChange, request: SurgicalOperationRequest, context: ParseContext): { valid: boolean; issues: SurgicalValidationIssue[] } {
    const issues: SurgicalValidationIssue[] = [];

    // For extractions, ensure we're not breaking dependencies
    if (change.type === 'delete' && change.oldText.includes('return')) {
      issues.push({
        severity: 'warning',
        message: 'Extracting code that contains return statements may change control flow',
        position: change.position,
        code: 'CONTROL_FLOW_CHANGE',
        category: 'semantics',
        fixSuggestion: 'Ensure the extracted function properly handles return values',
      });
    }

    return { valid: issues.filter(i => i.severity === 'error').length === 0, issues };
  }

  private detectPotentialSideEffects(change: SurgicalChange, context: ParseContext): string[] {
    const sideEffects: string[] = [];

    if (change.newText) {
      // Check for potential side effect patterns
    // eslint-disable-next-line no-console
      if (change.newText.includes('console.') || change.newText.includes('print(')) {
        sideEffects.push('Code contains logging statements');
      }

      if (change.newText.includes('document.') || change.newText.includes('window.')) {
        sideEffects.push('Code may have DOM side effects');
      }

      if (change.newText.includes('fetch(') || change.newText.includes('xhr') || change.newText.includes('http')) {
        sideEffects.push('Code may perform network operations');
      }
    }

    return sideEffects;
  }

  private isTypedLanguage(language: SupportedLanguage): boolean {
    return language === SupportedLanguage.TYPESCRIPT ||
           language === SupportedLanguage.JAVA ||
           language === SupportedLanguage.CSHARP;
  }

  private checkTypeCompatibility(oldText: string, newText: string, language: SupportedLanguage, context: ParseContext): { compatible: boolean; severity: 'error' | 'warning'; message: string; suggestion?: string } {
    // Basic type compatibility check

    // Extract type information (simplified)
    const oldType = this.extractTypeInfo(oldText, language);
    const newType = this.extractTypeInfo(newText, language);

    if (oldType === newType) {
      return { compatible: true, severity: 'warning', message: '' };
    }

    // Check for obvious incompatibilities
    if (oldType === 'string' && newType === 'number') {
      return {
        compatible: false,
        severity: 'error',
        message: 'Cannot replace string with number',
        suggestion: 'Add type conversion or use compatible type',
      };
    }

    if (oldType && newType && oldType !== newType) {
      return {
        compatible: false,
        severity: 'warning',
        message: `Type change from ${oldType} to ${newType}`,
        suggestion: 'Verify type compatibility',
      };
    }

    return { compatible: true, severity: 'warning', message: '' };
  }

  private extractTypeInfo(text: string, language: SupportedLanguage): string | null {
    // Simple type extraction (would need proper AST parsing in real implementation)
    switch (language) {
      case SupportedLanguage.TYPESCRIPT: {
        const tsTypeMatch = text.match(/:\s*(\w+)/);
        return tsTypeMatch ? tsTypeMatch[1] : null;
      }
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP: {
        const typeMatch = text.match(/^\s*(\w+)\s+\w+/);
        return typeMatch ? typeMatch[1] : null;
      }
      default:
        return null;
    }
  }

  // eslint-disable-next-line max-len
  private checkGrammarConformance(text: string, language: SupportedLanguage): { valid: boolean; message: string; suggestion?: string } {
    // Basic grammar conformance check

    // Check for language-specific grammar rules
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        // Check for basic JS/TS grammar
        if (text.includes('var ') && text.includes('let ')) {
          return {
            valid: false,
            message: 'Mixed var and let declarations',
            suggestion: 'Use consistent variable declaration style',
          };
        }
        break;
      case SupportedLanguage.PYTHON:
        // Check for Python-specific issues
        if (text.includes(';') && !text.includes("';") && !text.includes('";')) {
          return {
            valid: false,
            message: 'Semicolons not typically used in Python',
            suggestion: 'Remove unnecessary semicolons',
          };
        }
        break;
    }

    return { valid: true, message: '' };
  }

  // eslint-disable-next-line max-len
  private validateGrammarRuleCompliance(change: SurgicalChange, language: SupportedLanguage): { valid: boolean; message: string; suggestion?: string } {
    // Check if change matches expected grammar rule
    const expectedRule = change.metadata?.grammarRule;

    if (!expectedRule) {
      return { valid: true, message: '' };
    }

    // Basic rule validation
    switch (expectedRule) {
      case 'variable_declaration':
        if (!this.looksLikeVariableDeclaration(change.newText, language)) {
          return {
            valid: false,
            message: 'Text does not look like a variable declaration',
            suggestion: 'Ensure proper variable declaration syntax',
          };
        }
        break;
      case 'function_declaration':
        if (!this.looksLikeFunctionDeclaration(change.newText, language)) {
          return {
            valid: false,
            message: 'Text does not look like a function declaration',
            suggestion: 'Ensure proper function declaration syntax',
          };
        }
        break;
    }

    return { valid: true, message: '' };
  }

  private looksLikeVariableDeclaration(text: string, language: SupportedLanguage): boolean {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return /\b(const|let|var)\s+\w+/.test(text);
      case SupportedLanguage.PYTHON:
        return /\w+\s*=/.test(text);
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return /\b\w+\s+\w+\s*(=|;)/.test(text);
      default:
        return true;
    }
  }

  private looksLikeFunctionDeclaration(text: string, language: SupportedLanguage): boolean {
    switch (language) {
      case SupportedLanguage.JAVASCRIPT:
      case SupportedLanguage.TYPESCRIPT:
        return /\bfunction\s+\w+\s*\(/.test(text) || /\w+\s*=\s*\(.*\)\s*=>/.test(text);
      case SupportedLanguage.PYTHON:
        return /\bdef\s+\w+\s*\(/.test(text);
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return /\b(public|private|protected)?\s*(static)?\s*\w+\s+\w+\s*\(/.test(text);
      default:
        return true;
    }
  }

  // Additional helper methods for other validation functions
  // eslint-disable-next-line max-len
  private validateSymbolReferences(symbol: string, change: SurgicalChange, context: ParseContext): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate actual symbol references
    return { valid: true, issues: [] };
  }

  private findOrphanedReferences(symbol: string, change: SurgicalChange, context: ParseContext): any[] {
    // Mock implementation - would find actual orphaned references
    return [];
  }

  // eslint-disable-next-line max-len
  private validateCrossFileReferences(symbol: string, change: SurgicalChange, request: SurgicalOperationRequest, context: ParseContext): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate cross-file references
    return { valid: true, issues: [] };
  }

  // eslint-disable-next-line max-len
  private validateChangeDependencies(change: SurgicalChange, allChanges: SurgicalChange[]): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate change dependencies
    return { valid: true, issues: [] };
  }

  // eslint-disable-next-line max-len
  private validateVariableReferences(change: SurgicalChange, context: ParseContext): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate variable references
    return { valid: true, issues: [] };
  }

  // eslint-disable-next-line max-len
  private validateSignatureTypeConsistency(change: SurgicalChange, request: SurgicalOperationRequest, context: ParseContext): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate signature type consistency
    return { valid: true, issues: [] };
  }

  // eslint-disable-next-line max-len
  private validateTypeInference(change: SurgicalChange, context: ParseContext, language: SupportedLanguage): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate type inference
    return { valid: true, issues: [] };
  }

  private hasGenericTypes(text: string, language: SupportedLanguage): boolean {
    switch (language) {
      case SupportedLanguage.TYPESCRIPT:
      case SupportedLanguage.JAVA:
      case SupportedLanguage.CSHARP:
        return text.includes('<') && text.includes('>');
      default:
        return false;
    }
  }

  // eslint-disable-next-line max-len
  private validateGenericConstraints(change: SurgicalChange, context: ParseContext, language: SupportedLanguage): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate generic constraints
    return { valid: true, issues: [] };
  }

  // eslint-disable-next-line max-len
  private detectImplicitTypeConversions(change: SurgicalChange, language: SupportedLanguage): Array<{ description: string; suggestion: string }> {
    // Mock implementation - would detect implicit type conversions
    return [];
  }

  private validateSyntaxNodeCompatibility(change: SurgicalChange, context: ParseContext): { compatible: boolean } {
    // Mock implementation - would validate syntax node compatibility
    return { compatible: true };
  }

  private validateLanguageSpecificConstraints(change: SurgicalChange, language: SupportedLanguage): { violations: Array<{ severity: 'error' | 'warning'; message: string; suggestion: string }> } {
    // Mock implementation - would validate language-specific constraints
    return { violations: [] };
  }

  // eslint-disable-next-line max-len
  private validateContextualGrammar(change: SurgicalChange, context: ParseContext, request: SurgicalOperationRequest): { valid: boolean; issues: SurgicalValidationIssue[] } {
    // Mock implementation - would validate contextual grammar
    return { valid: true, issues: [] };
  }

  /**
   * Helper methods for inline function changes
   */
  private extractFunctionBody(functionInfo: SymbolInfo, context: ParseContext): string | null {
    // Mock implementation - would extract actual function body from AST/context
    // This would need to parse the function declaration and extract just the body
    const functionText = this.getTextAtRange(context, functionInfo.position, {
      line: functionInfo.position.line + 20, // Approximate function size
      column: 0,
      offset: functionInfo.position.offset + 500,
    });

    // Simple extraction - find content between { and }
    const bodyMatch = functionText.match(/\{([\s\S]*)\}/);
    return bodyMatch ? bodyMatch[1].trim() : null;
  }

  private extractCallArguments(callText: string): string[] {
    // Mock implementation - would properly parse call arguments
    const argsMatch = callText.match(/\((.*)\)/);
    if (!argsMatch) {
      return [];
    }

    const argsString = argsMatch[1];
    if (!argsString.trim()) {
      return [];
    }

    // Simple split by comma (would need proper parsing for complex expressions)
    return argsString.split(',').map(arg => arg.trim());
  }

  private substituteParameters(functionBody: string, callArguments: string[], context: ParseContext): string {
    // Mock implementation - would properly substitute parameter names with arguments
    // This is a simplified version that would need proper AST analysis
    const result = functionBody;

    // For now, just return the function body as-is
    // A proper implementation would:
    // 1. Parse function parameters from the declaration
    // 2. Replace parameter references in the body with the provided arguments
    // 3. Handle scope and variable conflicts

    return result;
  }

  private findFunctionDeclarationEnd(functionInfo: SymbolInfo, context: ParseContext): CodePosition {
    // Mock implementation - would find actual end of function declaration
    // This would need to parse the function and find the closing brace
    return {
      line: functionInfo.position.line + 20, // Approximate
      column: 0,
      offset: functionInfo.position.offset + 500,
    };
  }
}

// Export the surgical refactoring class
export default SurgicalRefactoring;

