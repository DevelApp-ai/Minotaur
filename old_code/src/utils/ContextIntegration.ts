/**
 * Context Integration Layer for StepParser
 *
 * This module provides the integration layer between the StepParser
 * and the existing ContextAwareParser/ContextManager system. It adapts
 * the file-based context management to work with step-by-step parsing.
 */

import { ContextManager } from '../context/ContextManager';
import { ContextAwareParser, ContextInfo, ScopeInfo, SymbolInfo, ParseStateInfo, CodePosition } from '../context/ContextAwareParser';
import { GrammarFormatType } from './Grammar';
import { SymbolTable } from '../context/SymbolTable';
import { Grammar } from './Grammar';
import { Terminal } from './Terminal';
import { Production } from './Production';
import { Token } from './Token';
import { IParserLexerSourceContainer } from './IParserLexerSource';

/**
 * Context snapshot for step-by-step parsing
 */
export interface StepParsingContextSnapshot {
  scopeStack: ScopeInfo[];
  symbolContext: Map<string, SymbolInfo>;
  parseState: ParseStateInfo;
  position: CodePosition;
  hash: number;
}

/**
 * Error recovery strategy
 */
export interface ErrorRecoveryStrategy {
  canRecover: boolean;
  strategy: 'skip' | 'insert' | 'replace' | 'backtrack';
  suggestion?: string;
  confidence: number;
}

/**
 * Context integration adapter that bridges step-by-step parsing with context management
 */
export class StepParsingContextAdapter {
  private contextManager: ContextManager;
  private symbolTable: SymbolTable;
  private currentFile: string = 'step-parsing-session';
  private currentCode: string = '';
  private currentPosition: CodePosition = { line: 1, column: 1, offset: 0 };
  private currentContext: ContextInfo | null = null;
  private activeGrammar: Grammar | null = null;
  
  // Static flag to prevent circular instantiation
  private static _isInitializing: boolean = false;

  constructor() {
    // Guard against circular instantiation
    if (StepParsingContextAdapter._isInitializing) {
      throw new Error('Circular StepParsingContextAdapter instantiation detected');
    }
    StepParsingContextAdapter._isInitializing = true;
    try {
      // Pass null to avoid circular dependency - ContextManager will create minimal interpreter
      this.contextManager = new ContextManager(null);
      this.symbolTable = new SymbolTable();
    } finally {
      // Clear the initialization flag
      StepParsingContextAdapter._isInitializing = false;
    }
  }

  /**
   * Sets the active grammar for context-aware parsing
   */
  public setActiveGrammar(grammar: Grammar): void {
    this.activeGrammar = grammar;
    // Initialize context with grammar information
    this.initializeGrammarContext(grammar);
  }

  /**
   * Initializes context for a parsing session
   */
  public async initializeForParsing(sourceLinesContainer: IParserLexerSourceContainer): Promise<void> {
    // Extract code from source lines container
    this.currentCode = this.extractCodeFromContainer(sourceLinesContainer);

    // Initialize context manager with the code
    this.currentContext = await this.contextManager.parseFile(
      this.currentFile,
      this.currentCode,
      'minotaur', // Use a generic language identifier
    );

    // Reset position
    this.currentPosition = { line: 1, column: 1, offset: 0 };
  }

  /**
   * Updates context with token information during parsing
   */
  public updateContext(token: Token, contextSnapshot?: StepParsingContextSnapshot): void {
    // Update current position based on token
    this.updatePositionWithToken(token);

    // If we have a context snapshot, use it to maintain consistency
    if (contextSnapshot) {
      this.synchronizeWithSnapshot(contextSnapshot);
    }

    // Update the context manager with the new position
    this.updateContextManager();
  }

  /**
   * Gets the current context information
   */
  public getCurrentContext(): ContextInfo {
    if (!this.currentContext) {
      // Return a default context if none exists
      return this.createDefaultContext();
    }
    return this.currentContext;
  }

  /**
   * Gets current scopes for context snapshot
   */
  public getCurrentScopes(): ScopeInfo[] {
    const context = this.getCurrentContext();
    // Use scopeStack for multiple scopes, fall back to single scope if available
    if (context.scopeStack && context.scopeStack.length > 0) {
      return context.scopeStack;
    }
    return context.scope ? [context.scope] : [];
  }

  /**
   * Gets current parse state
   */
  public getCurrentParseState(): ParseStateInfo {
    const context = this.getCurrentContext();
    return context.parseState || this.createDefaultParseState();
  }

  /**
   * Gets current position
   */
  public getCurrentPosition(): CodePosition {
    return this.currentPosition;
  }

  /**
   * Computes a hash for the current context
   */
  public computeContextHash(): number {
    const context = this.getCurrentContext();
    const currentScopes = this.getCurrentScopes();

    // Get symbol information from the symbol table
    const symbolTableStats = this.symbolTable.getStatistics();
    const symbolNames = Array.from(symbolTableStats.symbolsByScope.keys());

    const contextString = JSON.stringify({
      scopes: currentScopes.map(s => s.id),
      symbols: symbolNames,
      position: this.currentPosition,
    });

    // Simple hash function
    let hash = 0;
    for (let i = 0; i < contextString.length; i++) {
      const char = contextString.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash;
  }

  /**
   * Checks if a terminal is valid in the current context
   */
  public isTerminalValidInContext(terminal: Terminal, contextInfo: ContextInfo): boolean {
    // Basic implementation - can be enhanced with more sophisticated logic
    const currentScopes = this.getScopesFromContext(contextInfo);
    const terminalName = terminal.getName();

    // Check if terminal is appropriate for current scope
    for (const scope of currentScopes) {
      if (this.isTerminalValidInScope(terminalName, scope)) {
        return true;
      }
    }

    // If no specific scope validation, allow by default
    return true;
  }

  /**
   * Checks if a production is valid in the current context
   */
  public isProductionValidInContext(production: Production, contextInfo: ContextInfo, terminal: Terminal): boolean {
    // Basic implementation - can be enhanced with grammar-specific logic
    const currentScopes = this.getScopesFromContext(contextInfo);
    const productionName = production.getName ? production.getName() : 'unknown';

    // Check if production is appropriate for current scope
    for (const scope of currentScopes) {
      if (this.isProductionValidInScope(productionName, scope)) {
        return true;
      }
    }

    return true; // Allow by default
  }

  /**
   * Ranks productions by context relevance
   */
  public rankProductionsByContext(productions: Production[], contextInfo: ContextInfo): Production[] {
    // Sort productions by context relevance
    return productions.sort((a, b) => {
      const scoreA = this.computeProductionContextScore(a, contextInfo);
      const scoreB = this.computeProductionContextScore(b, contextInfo);
      return scoreB - scoreA; // Higher scores first
    });
  }

  /**
   * Gets error recovery strategy for a token
   */
  public getErrorRecoveryStrategy(token: Token, contextInfo: ContextInfo): ErrorRecoveryStrategy {
    const currentScopes = this.getScopesFromContext(contextInfo);
    const tokenValue = token.getValue();

    // Analyze context to determine recovery strategy
    if (currentScopes.length > 0) {
      const currentScope = currentScopes[currentScopes.length - 1];

      // If we're in a block scope and encounter unexpected token, try to skip
      if (currentScope.type === 'block' && this.isLikelyTypo(tokenValue)) {
        return {
          canRecover: true,
          strategy: 'skip',
          suggestion: `Skip unexpected token '${tokenValue}'`,
          confidence: 0.7,
        };
      }

      // If we're missing a closing brace, suggest insertion
      if (this.isMissingClosingBrace(currentScope, tokenValue)) {
        return {
          canRecover: true,
          strategy: 'insert',
          suggestion: 'Insert missing closing brace',
          confidence: 0.8,
        };
      }
    }

    // Default: cannot recover
    return {
      canRecover: false,
      strategy: 'skip',
      confidence: 0.0,
    };
  }

  /**
   * Updates context with production information
   */
  public updateContextWithProduction(production: Production, token: Token, contextInfo: ContextInfo): void {
    // Extract semantic information from production
    const productionName = production.getName ? production.getName() : 'unknown';

    // Update scope if production represents a scope change
    if (this.isProductionScopeChange(productionName)) {
      this.handleScopeChange(production, token, contextInfo);
    }

    // Update symbol information if production defines symbols
    const symbolInfo = this.extractSymbolInfo(production, token, contextInfo);
    if (symbolInfo) {
      this.symbolTable.declareSymbol(symbolInfo);
    }
  }

  /**
   * Extracts symbol information from production
   */
  public extractSymbolInfo(production: Production, token: Token, contextInfo: ContextInfo): SymbolInfo | null {
    const productionName = production.getName ? production.getName() : 'unknown';
    const tokenValue = token.getValue();

    // Check if this production defines a symbol
    if (this.isSymbolDefiningProduction(productionName)) {
      return {
        name: tokenValue,
        type: this.inferSymbolType(productionName, tokenValue),
        kind: this.inferSymbolKind(productionName),
        scope: this.getCurrentScopeId(contextInfo),
        position: this.currentPosition,
        references: [],
        definition: this.currentPosition,
        context: this.getScopesFromContext(contextInfo).map(s => s.name || s.id),
      };
    }

    return null;
  }

  /**
   * Computes confidence for a production in context
   */
  public computeProductionConfidence(production: Production, contextInfo: ContextInfo): number {
    const productionName = production.getName ? production.getName() : 'unknown';
    const currentScopes = this.getScopesFromContext(contextInfo);

    let confidence = 0.5; // Base confidence

    // Increase confidence if production matches current scope
    for (const scope of currentScopes) {
      if (this.isProductionExpectedInScope(productionName, scope)) {
        confidence += 0.3;
      }
    }

    // Increase confidence if production follows expected patterns
    if (this.isProductionFollowingPattern(production, contextInfo)) {
      confidence += 0.2;
    }

    return Math.min(confidence, 1.0);
  }

  /**
   * Updates context state
   */
  public updateContextState(contextName: string, active: boolean): void {
    // This could be used to maintain additional context state
    // For now, we'll just log it
    // eslint-disable-next-line no-console
    console.debug(`Context state updated: ${contextName} = ${active}`);
  }

  /**
   * Resets the context adapter
   */
  public reset(): void {
    this.currentContext = null;
    this.currentPosition = { line: 1, column: 1, offset: 0 };
    this.currentCode = '';
    this.symbolTable.clear();
  }

  // PRIVATE HELPER METHODS

  private initializeGrammarContext(grammar: Grammar): void {
    // Initialize context based on grammar rules
    // This is a placeholder for grammar-specific context initialization
  }

  private extractCodeFromContainer(container: IParserLexerSourceContainer): string {
    // Extract code from the source lines container
    let code = '';
    const lines = container.getSourceLines();
    for (const line of lines) {
      code += line.getContent() + '\n';
    }
    return code;
  }

  private updatePositionWithToken(token: Token): void {
    const tokenValue = token.getValue();

    // Update position based on token content
    for (const char of tokenValue) {
      if (char === '\n') {
        this.currentPosition.line++;
        this.currentPosition.column = 1;
      } else {
        this.currentPosition.column++;
      }
      this.currentPosition.offset++;
    }
  }

  private synchronizeWithSnapshot(snapshot: StepParsingContextSnapshot): void {
    // Synchronize current state with the provided snapshot
    this.currentPosition = { ...snapshot.position };
  }

  private updateContextManager(): void {
    // Update the context manager with current position
    // This is a simplified implementation
  }

  private createDefaultContext(): ContextInfo {
    return {
      scope: null,
      scopeStack: [],
      contextStack: [],
      symbols: [],
      parseState: this.createDefaultParseState(),
      activeGrammar: null,
      timestamp: Date.now(),
    };
  }

  private createDefaultParseState(): ParseStateInfo {
    return {
      currentRule: 'start',
      position: this.currentPosition,
      contextStack: [],
      scopeStack: [],
      validTerminals: [],
      errors: [],
      warnings: [],
      suggestions: [],
      grammarState: {
        activeGrammar: 'minotaur',
        formatType: GrammarFormatType.CEBNF,
        baseGrammars: [],
        activeRules: [],
        contextModifiers: [],
        inheritanceChain: [],
      },
    };
  }

  private isTerminalValidInScope(terminalName: string, scope: ScopeInfo): boolean {
    // Basic scope validation logic
    return true; // Allow by default
  }

  private isProductionValidInScope(productionName: string, scope: ScopeInfo): boolean {
    // Basic scope validation logic
    return true; // Allow by default
  }

  private computeProductionContextScore(production: Production, contextInfo: ContextInfo): number {
    // Compute relevance score for production in current context
    return 0.5; // Default score
  }

  private isLikelyTypo(tokenValue: string): boolean {
    // Simple heuristic for detecting typos
    return tokenValue.length === 1 && /[^a-zA-Z0-9\s]/.test(tokenValue);
  }

  private isMissingClosingBrace(scope: ScopeInfo, tokenValue: string): boolean {
    // Check if we're missing a closing brace
    return scope.type === 'block' && tokenValue === '}';
  }

  private isProductionScopeChange(productionName: string): boolean {
    // Check if production represents a scope change
    return ['block_start', 'function_start', 'class_start'].includes(productionName);
  }

  private handleScopeChange(production: Production, token: Token, contextInfo: ContextInfo): void {
    // Handle scope changes
    // This is a placeholder for scope management logic
  }

  private isSymbolDefiningProduction(productionName: string): boolean {
    // Check if production defines a symbol
    return ['variable_declaration', 'function_declaration', 'class_declaration'].includes(productionName);
  }

  private inferSymbolType(productionName: string, tokenValue: string): string {
    // Infer symbol type from production and token
    if (productionName.includes('function')) {
      return 'function';
    }
    if (productionName.includes('class')) {
      return 'class';
    }
    return 'variable';
  }

  private inferSymbolKind(productionName: string): any {
    // Infer symbol kind from production
    if (productionName.includes('function')) {
      return 'FUNCTION';
    }
    if (productionName.includes('class')) {
      return 'CLASS';
    }
    return 'VARIABLE';
  }

  private getCurrentScopeId(contextInfo: ContextInfo): string {
    const scopes = this.getScopesFromContext(contextInfo);
    return scopes.length > 0 ? scopes[scopes.length - 1].id : 'global';
  }

  /**
   * Helper method to get scopes from context info, handling both scopeStack and single scope
   */
  private getScopesFromContext(contextInfo: ContextInfo): ScopeInfo[] {
    // Prefer scopeStack for multiple scopes, fall back to single scope
    if (contextInfo.scopeStack && contextInfo.scopeStack.length > 0) {
      return contextInfo.scopeStack;
    }
    return contextInfo.scope ? [contextInfo.scope] : [];
  }

  private isProductionExpectedInScope(productionName: string, scope: ScopeInfo): boolean {
    // Check if production is expected in the given scope
    return true; // Allow by default
  }

  private isProductionFollowingPattern(production: Production, contextInfo: ContextInfo): boolean {
    // Check if production follows expected patterns
    return true; // Allow by default
  }
}

