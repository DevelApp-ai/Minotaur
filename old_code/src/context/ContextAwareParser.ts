/**
 * Context-aware parsing engine for Minotaur AI agent support.
 * Provides real-time parsing with hierarchical context management and symbol tracking.
 */

import { Interpreter } from '../utils/Interpreter';
import { GrammarContainer } from '../utils/GrammarContainer';
import { Grammar, GrammarFormatType } from '../utils/Grammar';
import { EventEmitter } from 'events';

/**
 * Code position information with enhanced context.
 */
export interface CodePosition {
  line: number;
  column: number;
  offset: number;
  file?: string;
}

/**
 * Enhanced scope information with hierarchical context.
 */
export interface ScopeInfo {
  id: string;
  type: ScopeType;
  name?: string;
  startPosition: CodePosition;
  endPosition: CodePosition;
  parent?: ScopeInfo;
  children: ScopeInfo[];
  variables: VariableInfo[];
  functions: FunctionInfo[];
  classes: ClassInfo[];
  depth: number;
  grammarRule?: string;
  context: string[];
}

/**
 * Scope types for different code constructs.
 */
export enum ScopeType {
  GLOBAL = 'global',
  MODULE = 'module',
  NAMESPACE = 'namespace',
  CLASS = 'class',
  INTERFACE = 'interface',
  FUNCTION = 'function',
  METHOD = 'method',
  BLOCK = 'block',
  LOOP = 'loop',
  CONDITIONAL = 'conditional',
  TRY_CATCH = 'try_catch',
  LAMBDA = 'lambda',
  CLOSURE = 'closure'
}

/**
 * Variable information with enhanced context.
 */
export interface VariableInfo {
  name: string;
  type: string;
  kind: VariableKind;
  scope: string;
  position: CodePosition;
  references: CodePosition[];
  definition?: CodePosition;
  isParameter: boolean;
  isLocal: boolean;
  isConstant: boolean;
  accessibility?: AccessibilityModifier;
  grammarRule?: string;
}

/**
 * Variable kinds.
 */
export enum VariableKind {
  VAR = 'var',
  LET = 'let',
  CONST = 'const',
  PARAMETER = 'parameter',
  FIELD = 'field',
  PROPERTY = 'property',
  STATIC = 'static'
}

/**
 * Accessibility modifiers.
 */
export enum AccessibilityModifier {
  PUBLIC = 'public',
  PRIVATE = 'private',
  PROTECTED = 'protected',
  INTERNAL = 'internal'
}

/**
 * Function information with enhanced context.
 */
export interface FunctionInfo {
  name: string;
  signature: string;
  returnType: string;
  parameters: ParameterInfo[];
  scope: string;
  position: CodePosition;
  references: CodePosition[];
  isAsync: boolean;
  isGenerator: boolean;
  isStatic: boolean;
  accessibility?: AccessibilityModifier;
  grammarRule?: string;
}

/**
 * Parameter information.
 */
export interface ParameterInfo {
  name: string;
  type: string;
  isOptional: boolean;
  defaultValue?: string;
  position: CodePosition;
}

/**
 * Class information with enhanced context.
 */
export interface ClassInfo {
  name: string;
  baseClasses: string[];
  interfaces: string[];
  scope: string;
  position: CodePosition;
  references: CodePosition[];
  isAbstract: boolean;
  isStatic: boolean;
  accessibility?: AccessibilityModifier;
  grammarRule?: string;
}

/**
 * Symbol information for AI agents.
 */
export interface SymbolInfo {
  name: string;
  type: string;
  kind: SymbolKind;
  scope: string;
  position: CodePosition;
  references: CodePosition[];
  definition?: CodePosition;
  documentation?: string;
  grammarRule?: string;
  context: string[];
}

/**
 * Symbol kinds.
 */
export enum SymbolKind {
  VARIABLE = 'variable',
  FUNCTION = 'function',
  CLASS = 'class',
  INTERFACE = 'interface',
  ENUM = 'enum',
  NAMESPACE = 'namespace',
  MODULE = 'module',
  TYPE = 'type',
  CONSTANT = 'constant',
  PROPERTY = 'property',
  METHOD = 'method',
  FIELD = 'field'
}

/**
 * Parse state information with enhanced context.
 */
export interface ParseStateInfo {
  currentRule: string;
  position: CodePosition;
  contextStack: string[];
  scopeStack: ScopeInfo[];
  validTerminals: string[];
  errors: ParseErrorInfo[];
  warnings: ParseWarningInfo[];
  suggestions: ParseSuggestionInfo[];
  grammarState: GrammarStateInfo;
}

/**
 * Parse error information.
 */
export interface ParseErrorInfo {
  message: string;
  position: CodePosition;
  severity: ErrorSeverity;
  code: string;
  category: ErrorCategory;
  suggestions?: string[];
  grammarRule?: string;
}

/**
 * Parse warning information.
 */
export interface ParseWarningInfo {
  message: string;
  position: CodePosition;
  code: string;
  category: WarningCategory;
  suggestions?: string[];
}

/**
 * Parse suggestion information.
 */
export interface ParseSuggestionInfo {
  message: string;
  position: CodePosition;
  type: SuggestionType;
  confidence: number;
  action?: string;
  parameters?: any;
}

/**
 * Error severity levels.
 */
export enum ErrorSeverity {
  ERROR = 'error',
  WARNING = 'warning',
  INFO = 'info',
  HINT = 'hint'
}

/**
 * Error categories.
 */
export enum ErrorCategory {
  SYNTAX = 'syntax',
  SEMANTIC = 'semantic',
  TYPE = 'type',
  REFERENCE = 'reference',
  SCOPE = 'scope',
  GRAMMAR = 'grammar'
}

/**
 * Warning categories.
 */
export enum WarningCategory {
  UNUSED = 'unused',
  DEPRECATED = 'deprecated',
  PERFORMANCE = 'performance',
  STYLE = 'style',
  BEST_PRACTICE = 'best_practice'
}

/**
 * Suggestion types.
 */
export enum SuggestionType {
  COMPLETION = 'completion',
  REFACTORING = 'refactoring',
  OPTIMIZATION = 'optimization',
  FIX = 'fix',
  ENHANCEMENT = 'enhancement'
}

/**
 * Grammar state information.
 */
export interface GrammarStateInfo {
  activeGrammar: string;
  formatType: GrammarFormatType;
  baseGrammars: string[];
  activeRules: string[];
  contextModifiers: string[];
  inheritanceChain: string[];
}

/**
 * Context change information.
 */
export interface ContextChangeInfo {
  type: ContextChangeType;
  oldContext?: any;
  newContext: any;
  position: CodePosition;
  timestamp: number;
}

/**
 * Context change types.
 */
export enum ContextChangeType {
  SCOPE_ENTERED = 'scope_entered',
  SCOPE_EXITED = 'scope_exited',
  VARIABLE_DECLARED = 'variable_declared',
  FUNCTION_DECLARED = 'function_declared',
  CLASS_DECLARED = 'class_declared',
  REFERENCE_ADDED = 'reference_added',
  GRAMMAR_SWITCHED = 'grammar_switched',
  CONTEXT_MODIFIED = 'context_modified'
}

/**
 * Context-aware parser for real-time analysis.
 */
export class ContextAwareParser extends EventEmitter {
  private interpreter: Interpreter;
  private grammarContainer: GrammarContainer;
  private currentScope: ScopeInfo | null;
  private scopeStack: ScopeInfo[];
  private symbolTable: Map<string, SymbolInfo>;
  private contextStack: string[];
  private parseState: ParseStateInfo;
  private activeGrammar: string | null;
  private isRealTimeMode: boolean;
  private changeBuffer: ContextChangeInfo[];
  private lastParseTime: number;

  constructor(interpreter?: Interpreter) {
    super();

    this.interpreter = interpreter || new Interpreter();
    this.grammarContainer = this.interpreter.getGrammarContainer();
    this.currentScope = null;
    this.scopeStack = [];
    this.symbolTable = new Map();
    this.contextStack = [];
    this.activeGrammar = null;
    this.isRealTimeMode = false;
    this.changeBuffer = [];
    this.lastParseTime = 0;

    this.parseState = {
      currentRule: '',
      position: { line: 1, column: 1, offset: 0 },
      contextStack: [],
      scopeStack: [],
      validTerminals: [],
      errors: [],
      warnings: [],
      suggestions: [],
      grammarState: {
        activeGrammar: '',
        formatType: GrammarFormatType.Minotaur,
        baseGrammars: [],
        activeRules: [],
        contextModifiers: [],
        inheritanceChain: [],
      },
    };

    this.initializeGlobalScope();
    this.setupEventHandlers();
  }

  /**
   * Initializes the global scope.
   */
  private initializeGlobalScope(): void {
    this.currentScope = {
      id: 'global',
      type: ScopeType.GLOBAL,
      name: 'global',
      startPosition: { line: 1, column: 1, offset: 0 },
      endPosition: { line: Number.MAX_SAFE_INTEGER, column: Number.MAX_SAFE_INTEGER, offset: Number.MAX_SAFE_INTEGER },
      children: [],
      variables: [],
      functions: [],
      classes: [],
      depth: 0,
      context: ['global'],
    };

    this.scopeStack.push(this.currentScope);
    this.contextStack.push('global');
  }

  /**
   * Sets up event handlers for the interpreter.
   */
  private setupEventHandlers(): void {
    // Listen for grammar events from the interpreter
    // Note: Interpreter currently doesn't implement EventEmitter
    // this.interpreter.on?.('grammar_loaded', this.handleGrammarLoaded.bind(this));
    // this.interpreter.on?.('grammar_switched', this.handleGrammarSwitched.bind(this));
    // this.interpreter.on?.('parse_error', this.handleParseError.bind(this));
    // this.interpreter.on?.('rule_matched', this.handleRuleMatched.bind(this));
  }

  /**
   * Parses code with context awareness.
   */
  public async parseCode(code: string, language: string, file?: string): Promise<ParseStateInfo> {
    const startTime = Date.now();

    try {
      // Reset state for new parse
      this.resetParseState();

      // Set active grammar
      await this.setActiveGrammar(language);

      // Parse the code
      const result = await this.performContextAwareParse(code, file);

      // Update parse state
      this.updateParseState(result);

      // Emit parse complete event
      this.emit('parse_complete', {
        parseState: this.parseState,
        duration: Date.now() - startTime,
        file,
      });

      this.lastParseTime = Date.now();
      return this.parseState;

    } catch (error) {
      this.handleParseError(error, { line: 1, column: 1, offset: 0 });
      throw error;
    }
  }

  /**
   * Performs incremental parsing for real-time updates.
   */
  public async parseIncremental(change: TextChange, file?: string): Promise<ParseStateInfo> {
    if (!this.isRealTimeMode) {
      throw new Error('Incremental parsing requires real-time mode');
    }

    try {
      // Apply the change to the current state
      const affectedScope = this.findAffectedScope(change.position);

      // Re-parse only the affected scope
      const result = await this.reparseScope(affectedScope, change, file);

      // Update parse state incrementally
      this.updateParseStateIncremental(result, change);

      // Emit incremental update event
      this.emit('incremental_update', {
        change,
        affectedScope,
        parseState: this.parseState,
        file,
      });

      return this.parseState;

    } catch (error) {
      // Fall back to full parse on error
      return this.parseCode(change.fullText, this.activeGrammar || 'unknown', file);
    }
  }

  /**
   * Enables or disables real-time parsing mode.
   */
  public setRealTimeMode(enabled: boolean): void {
    this.isRealTimeMode = enabled;

    if (enabled) {
      this.emit('real_time_enabled');
    } else {
      this.emit('real_time_disabled');
    }
  }

  /**
   * Gets the current context information.
   */
  public getCurrentContext(): ContextInfo {
    return {
      scope: this.currentScope,
      scopeStack: [...this.scopeStack],
      contextStack: [...this.contextStack],
      symbols: Array.from(this.symbolTable.values()),
      parseState: { ...this.parseState },
      activeGrammar: this.activeGrammar,
      timestamp: Date.now(),
    };
  }

  /**
   * Gets context information for a specific position.
   */
  public getContextAt(position: CodePosition): ContextInfo {
    const scope = this.findScopeAt(position);
    const symbols = this.getSymbolsInScope(scope?.id || 'global');

    return {
      scope,
      scopeStack: this.getScopeStackAt(position),
      contextStack: this.getContextStackAt(position),
      symbols,
      parseState: { ...this.parseState },
      activeGrammar: this.activeGrammar,
      timestamp: Date.now(),
    };
  }

  /**
   * Finds the scope containing a specific position.
   */
  public findScopeAt(position: CodePosition): ScopeInfo | null {
    return this.findScopeInTree(this.scopeStack[0], position);
  }

  /**
   * Recursively finds scope in the scope tree.
   */
  private findScopeInTree(scope: ScopeInfo, position: CodePosition): ScopeInfo | null {
    if (!this.isPositionInScope(position, scope)) {
      return null;
    }

    // Check children first (more specific scopes)
    for (const child of scope.children) {
      const result = this.findScopeInTree(child, position);
      if (result) {
        return result;
      }
    }

    // Return this scope if no child contains the position
    return scope;
  }

  /**
   * Checks if a position is within a scope.
   */
  private isPositionInScope(position: CodePosition, scope: ScopeInfo): boolean {
    const start = scope.startPosition;
    const end = scope.endPosition;

    if (position.line < start.line || position.line > end.line) {
      return false;
    }

    if (position.line === start.line && position.column < start.column) {
      return false;
    }

    if (position.line === end.line && position.column > end.column) {
      return false;
    }

    return true;
  }

  /**
   * Gets symbols in a specific scope.
   */
  public getSymbolsInScope(scopeId: string): SymbolInfo[] {
    return Array.from(this.symbolTable.values()).filter(symbol => symbol.scope === scopeId);
  }

  /**
   * Gets all symbols visible from a specific position.
   */
  public getVisibleSymbols(position: CodePosition): SymbolInfo[] {
    const scope = this.findScopeAt(position);
    if (!scope) {
      return [];
    }

    const visibleSymbols: SymbolInfo[] = [];
    let currentScope: ScopeInfo | undefined = scope;

    // Collect symbols from current scope and all parent scopes
    while (currentScope) {
      visibleSymbols.push(...this.getSymbolsInScope(currentScope.id));
      currentScope = currentScope.parent;
    }

    return visibleSymbols;
  }

  /**
   * Adds a symbol to the symbol table.
   */
  public addSymbol(symbol: SymbolInfo): void {
    const key = `${symbol.scope}:${symbol.name}`;
    this.symbolTable.set(key, symbol);

    this.emit('symbol_added', { symbol });
    this.recordContextChange({
      type: this.getContextChangeTypeForSymbol(symbol.kind),
      newContext: symbol,
      position: symbol.position,
      timestamp: Date.now(),
    });
  }

  /**
   * Updates a symbol in the symbol table.
   */
  public updateSymbol(symbol: SymbolInfo): void {
    const key = `${symbol.scope}:${symbol.name}`;
    const oldSymbol = this.symbolTable.get(key);

    this.symbolTable.set(key, symbol);

    this.emit('symbol_updated', { oldSymbol, newSymbol: symbol });
  }

  /**
   * Removes a symbol from the symbol table.
   */
  public removeSymbol(scopeId: string, name: string): void {
    const key = `${scopeId}:${name}`;
    const symbol = this.symbolTable.get(key);

    if (symbol) {
      this.symbolTable.delete(key);
      this.emit('symbol_removed', { symbol });
    }
  }

  /**
   * Enters a new scope.
   */
  public enterScope(scope: ScopeInfo): void {
    // Set parent relationship
    if (this.currentScope) {
      scope.parent = this.currentScope;
      this.currentScope.children.push(scope);
    }

    // Update current scope and stack
    this.currentScope = scope;
    this.scopeStack.push(scope);
    this.contextStack.push(scope.type);

    this.emit('scope_entered', { scope });
    this.recordContextChange({
      type: ContextChangeType.SCOPE_ENTERED,
      newContext: scope,
      position: scope.startPosition,
      timestamp: Date.now(),
    });
  }

  /**
   * Exits the current scope.
   */
  public exitScope(): ScopeInfo | null {
    if (this.scopeStack.length <= 1) {
      return null; // Don't exit global scope
    }

    const exitedScope = this.scopeStack.pop();
    this.contextStack.pop();
    this.currentScope = this.scopeStack[this.scopeStack.length - 1];

    if (exitedScope) {
      this.emit('scope_exited', { scope: exitedScope });
      this.recordContextChange({
        type: ContextChangeType.SCOPE_EXITED,
        oldContext: exitedScope,
        newContext: this.currentScope,
        position: exitedScope.endPosition,
        timestamp: Date.now(),
      });
    }

    return exitedScope;
  }

  /**
   * Switches to a different grammar.
   */
  public async switchGrammar(grammarName: string): Promise<void> {
    const oldGrammar = this.activeGrammar;

    try {
      await this.setActiveGrammar(grammarName);

      this.emit('grammar_switched', { oldGrammar, newGrammar: grammarName });
      this.recordContextChange({
        type: ContextChangeType.GRAMMAR_SWITCHED,
        oldContext: oldGrammar,
        newContext: grammarName,
        position: this.parseState.position,
        timestamp: Date.now(),
      });

    } catch (error) {
      // eslint-disable-next-line max-len
      throw new Error(`Failed to switch grammar to ${grammarName}: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Gets suggestions for a specific position.
   */
  public getSuggestions(position: CodePosition): ParseSuggestionInfo[] {
    const context = this.getContextAt(position);
    const suggestions: ParseSuggestionInfo[] = [];

    // Add completion suggestions
    suggestions.push(...this.getCompletionSuggestions(context, position));

    // Add refactoring suggestions
    suggestions.push(...this.getRefactoringSuggestions(context, position));

    // Add optimization suggestions
    suggestions.push(...this.getOptimizationSuggestions(context, position));

    return suggestions.sort((a, b) => b.confidence - a.confidence);
  }

  /**
   * Gets completion suggestions for a position.
   */
  private getCompletionSuggestions(context: ContextInfo, position: CodePosition): ParseSuggestionInfo[] {
    const suggestions: ParseSuggestionInfo[] = [];
    const visibleSymbols = this.getVisibleSymbols(position);

    // Add symbol completions
    for (const symbol of visibleSymbols) {
      suggestions.push({
        message: `Complete with ${symbol.name}`,
        position,
        type: SuggestionType.COMPLETION,
        confidence: 0.8,
        action: 'complete_symbol',
        parameters: { symbol: symbol.name, type: symbol.type },
      });
    }

    // Add keyword completions based on grammar
    const validTerminals = this.parseState.validTerminals;
    for (const terminal of validTerminals) {
      if (this.isKeyword(terminal)) {
        suggestions.push({
          message: `Complete with keyword ${terminal}`,
          position,
          type: SuggestionType.COMPLETION,
          confidence: 0.6,
          action: 'complete_keyword',
          parameters: { keyword: terminal },
        });
      }
    }

    return suggestions;
  }

  /**
   * Gets refactoring suggestions for a position.
   */
  private getRefactoringSuggestions(context: ContextInfo, position: CodePosition): ParseSuggestionInfo[] {
    const suggestions: ParseSuggestionInfo[] = [];

    // Add extract variable suggestions for complex expressions
    if (this.shouldSuggestExtractVariable(context, position)) {
      suggestions.push({
        message: 'Extract variable from complex expression',
        position,
        type: SuggestionType.REFACTORING,
        confidence: 0.7,
        action: 'extract_variable',
        parameters: { target: position },
      });
    }

    // Add extract function suggestions for large blocks
    if (this.shouldSuggestExtractFunction(context, position)) {
      suggestions.push({
        message: 'Extract function from code block',
        position,
        type: SuggestionType.REFACTORING,
        confidence: 0.6,
        action: 'extract_function',
        parameters: { target: position },
      });
    }

    return suggestions;
  }

  /**
   * Gets optimization suggestions for a position.
   */
  private getOptimizationSuggestions(context: ContextInfo, position: CodePosition): ParseSuggestionInfo[] {
    const suggestions: ParseSuggestionInfo[] = [];

    // Add performance optimization suggestions
    if (this.shouldSuggestOptimization(context, position)) {
      suggestions.push({
        message: 'Optimize performance of this code block',
        position,
        type: SuggestionType.OPTIMIZATION,
        confidence: 0.5,
        action: 'optimize_performance',
        parameters: { target: position },
      });
    }

    return suggestions;
  }

  /**
   * Resets the parse state for a new parse.
   */
  private resetParseState(): void {
    this.currentScope = null;
    this.scopeStack = [];
    this.symbolTable.clear();
    this.contextStack = [];
    this.changeBuffer = [];

    this.parseState = {
      currentRule: '',
      position: { line: 1, column: 1, offset: 0 },
      contextStack: [],
      scopeStack: [],
      validTerminals: [],
      errors: [],
      warnings: [],
      suggestions: [],
      grammarState: {
        activeGrammar: this.activeGrammar || '',
        formatType: GrammarFormatType.Minotaur,
        baseGrammars: [],
        activeRules: [],
        contextModifiers: [],
        inheritanceChain: [],
      },
    };

    this.initializeGlobalScope();
  }

  /**
   * Sets the active grammar for parsing.
   */
  private async setActiveGrammar(grammarName: string): Promise<void> {
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      throw new Error(`Grammar not found: ${grammarName}`);
    }

    this.activeGrammar = grammarName;

    // Update grammar state
    this.parseState.grammarState = {
      activeGrammar: grammarName,
      formatType: grammar.getFormatType(),
      baseGrammars: grammar.getBaseGrammars(),
      activeRules: [], // Will be populated during parsing
      contextModifiers: [], // Will be populated during parsing
      inheritanceChain: this.grammarContainer.getInheritanceHierarchy(grammarName),
    };
  }

  /**
   * Performs context-aware parsing.
   */
  private async performContextAwareParse(code: string, file?: string): Promise<any> {
    // This would integrate with the actual Minotaur parsing engine
    // For now, returning a mock result
    return {
      success: true,
      scopes: [],
      symbols: [],
      errors: [],
      warnings: [],
    };
  }

  /**
   * Updates the parse state with results.
   */
  private updateParseState(result: any): void {
    // Update parse state based on parsing results
    this.parseState.errors = result.errors || [];
    this.parseState.warnings = result.warnings || [];
    this.parseState.scopeStack = [...this.scopeStack];
    this.parseState.contextStack = [...this.contextStack];
  }

  /**
   * Updates parse state incrementally.
   */
  private updateParseStateIncremental(result: any, change: TextChange): void {
    // Merge incremental results with existing state
    this.updateParseState(result);
  }

  /**
   * Finds the scope affected by a text change.
   */
  private findAffectedScope(position: CodePosition): ScopeInfo | null {
    return this.findScopeAt(position);
  }

  /**
   * Re-parses a specific scope after a change.
   */
  private async reparseScope(scope: ScopeInfo | null, change: TextChange, file?: string): Promise<any> {
    // This would re-parse only the affected scope
    // For now, returning a mock result
    return {
      success: true,
      scope,
      change,
    };
  }

  /**
   * Gets the scope stack at a specific position.
   */
  private getScopeStackAt(position: CodePosition): ScopeInfo[] {
    const stack: ScopeInfo[] = [];
    let scope = this.findScopeAt(position);

    while (scope) {
      stack.unshift(scope);
      scope = scope.parent;
    }

    return stack;
  }

  /**
   * Gets the context stack at a specific position.
   */
  private getContextStackAt(position: CodePosition): string[] {
    const scopeStack = this.getScopeStackAt(position);
    return scopeStack.map(scope => scope.type);
  }

  /**
   * Records a context change for later analysis.
   */
  private recordContextChange(change: ContextChangeInfo): void {
    this.changeBuffer.push(change);

    // Limit buffer size
    if (this.changeBuffer.length > 1000) {
      this.changeBuffer.shift();
    }

    this.emit('context_changed', change);
  }

  /**
   * Gets the context change type for a symbol kind.
   */
  private getContextChangeTypeForSymbol(kind: SymbolKind): ContextChangeType {
    switch (kind) {
      case SymbolKind.VARIABLE:
        return ContextChangeType.VARIABLE_DECLARED;
      case SymbolKind.FUNCTION:
      case SymbolKind.METHOD:
        return ContextChangeType.FUNCTION_DECLARED;
      case SymbolKind.CLASS:
        return ContextChangeType.CLASS_DECLARED;
      default:
        return ContextChangeType.REFERENCE_ADDED;
    }
  }

  /**
   * Event handlers for interpreter events.
   */
  private handleGrammarLoaded(grammarName: string): void {
    this.emit('grammar_loaded', { grammarName });
  }

  private handleGrammarSwitched(oldGrammar: string, newGrammar: string): void {
    this.activeGrammar = newGrammar;
  }

  private handleParseError(error: any, position?: CodePosition): void {
    const errorInfo: ParseErrorInfo = {
      message: error instanceof Error ? error.message : String(error) || error.toString(),
      position: position || { line: 1, column: 1, offset: 0 },
      severity: ErrorSeverity.ERROR,
      code: error.code || 'PARSE_ERROR',
      category: ErrorCategory.SYNTAX,
    };

    this.parseState.errors.push(errorInfo);
    this.emit('parse_error', errorInfo);
  }

  private handleRuleMatched(rule: string, position: CodePosition): void {
    this.parseState.currentRule = rule;
    this.parseState.position = position;
    this.emit('rule_matched', { rule, position });
  }

  /**
   * Helper methods for suggestions.
   */
  private isKeyword(terminal: string): boolean {
    const keywords = ['function', 'class', 'if', 'else', 'for', 'while', 'return', 'var', 'let', 'const'];
    return keywords.includes(terminal.toLowerCase());
  }

  private shouldSuggestExtractVariable(context: ContextInfo, position: CodePosition): boolean {
    // Suggest extract variable if we have complex expressions or repeated patterns
    if (!context.scope) {
      return false;
    }

    // Check if we're in a method/function scope where variable extraction makes sense
    if (context.scope.type === ScopeType.METHOD || context.scope.type === ScopeType.FUNCTION) {
      // Look for symbols that might indicate complex expressions
      const complexExpressionIndicators = context.symbols.filter(symbol =>
        symbol.type === 'expression' || symbol.type === 'operation',
      );

      // Suggest if we have multiple complex expressions
      return complexExpressionIndicators.length > 2;
    }

    return false;
  }

  private shouldSuggestExtractFunction(context: ContextInfo, position: CodePosition): boolean {
    // Suggest extract function for large blocks or repeated code patterns
    if (!context.scope) {
      return false;
    }

    // Check if we're in a large method with many statements
    if (context.scope.type === ScopeType.METHOD || context.scope.type === ScopeType.FUNCTION) {
      // Count statements/symbols in current scope
      const statementsInScope = context.symbols.filter(symbol =>
        symbol.scope === context.scope?.name,
      );

      // Suggest if method is getting large (more than 15 statements)
      if (statementsInScope.length > 15) {
        return true;
      }

      // Also suggest if we have nested control structures
      const controlStructures = context.symbols.filter(symbol =>
        symbol.type === 'if' || symbol.type === 'for' || symbol.type === 'while',
      );

      return controlStructures.length > 3;
    }

    return false;
  }

  private shouldSuggestOptimization(context: ContextInfo, position: CodePosition): boolean {
    // Suggest optimizations based on code patterns and context
    if (!context.scope || !context.parseState) {
      return false;
    }

    // Look for potential optimization opportunities
    const hasLoops = context.symbols.some(symbol =>
      symbol.type === 'for' || symbol.type === 'while',
    );

    const hasNestedLoops = context.scopeStack.filter(scope =>
      scope.type === ScopeType.LOOP,
    ).length > 1;

    // Suggest optimization for nested loops
    if (hasNestedLoops) {
      return true;
    }

    // Suggest for methods with many variables (potential for optimization)
    const variables = context.symbols.filter(symbol =>
      symbol.type === 'variable' || symbol.type === 'identifier',
    );

    if (variables.length > 10 && hasLoops) {
      return true;
    }

    return false;
  }

  /**
   * Gets the current parse statistics.
   */
  public getParseStatistics(): ParseStatistics {
    return {
      totalScopes: this.scopeStack.length,
      totalSymbols: this.symbolTable.size,
      totalErrors: this.parseState.errors.length,
      totalWarnings: this.parseState.warnings.length,
      lastParseTime: this.lastParseTime,
      isRealTimeMode: this.isRealTimeMode,
      activeGrammar: this.activeGrammar,
      contextChanges: this.changeBuffer.length,
    };
  }
}

/**
 * Context information interface.
 */
export interface ContextInfo {
  scope: ScopeInfo | null;
  scopeStack: ScopeInfo[];
  contextStack: string[];
  symbols: SymbolInfo[];
  parseState: ParseStateInfo;
  activeGrammar: string | null;
  timestamp: number;
}

/**
 * Text change information for incremental parsing.
 */
export interface TextChange {
  position: CodePosition;
  oldText: string;
  newText: string;
  fullText: string;
}

/**
 * Parse statistics interface.
 */
export interface ParseStatistics {
  totalScopes: number;
  totalSymbols: number;
  totalErrors: number;
  totalWarnings: number;
  lastParseTime: number;
  isRealTimeMode: boolean;
  activeGrammar: string | null;
  contextChanges: number;
}

// Export the main parser class as default
export default ContextAwareParser;

