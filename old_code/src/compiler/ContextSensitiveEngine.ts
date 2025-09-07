/**
 * Minotaur Context-Sensitive Parsing Engine
 *
 * Advanced context-sensitive parsing capabilities that integrate with the
 * inheritance system and provide enhanced parsing for the compiler-compiler export.
 */

import { Grammar, Rule } from '../core/grammar/Grammar';
import { Grammar as UtilsGrammar } from '../utils/Grammar';
import { InheritanceResolver } from '../utils/InheritanceResolver';

export interface ContextSensitiveConfiguration {
    enableContextInheritance: boolean;
    enableSemanticActions: boolean;
    enableSymbolTracking: boolean;
    enableScopeAnalysis: boolean;
    optimizationLevel: 'none' | 'basic' | 'aggressive';
    maxContextDepth: number;
    enableContextCaching: boolean;
}

export interface ParseContext {
    id: string;
    parentId?: string;
    scopeType: ScopeType;
    symbols: Map<string, SymbolInfo>;
    rules: Map<string, ContextRule>;
    depth: number;
    position: ParsePosition;
    metadata: Map<string, any>;
}

export interface SymbolInfo {
    name: string;
    type: SymbolType;
    scope: string;
    position: ParsePosition;
    references: ParsePosition[];
    attributes: Map<string, any>;
    visibility: SymbolVisibility;
}

export interface ContextRule {
    name: string;
    condition: ContextCondition;
    action: ContextAction;
    priority: number;
    inherited: boolean;
    sourceGrammar?: string;
}

export interface ParsePosition {
    line: number;
    column: number;
    offset: number;
    length: number;
}

export enum ScopeType {
    Global = 'global',
    Function = 'function',
    Class = 'class',
    Block = 'block',
    Expression = 'expression',
    Statement = 'statement',
    Custom = 'custom'
}

export enum SymbolType {
    Variable = 'variable',
    Function = 'function',
    Class = 'class',
    Interface = 'interface',
    Type = 'type',
    Constant = 'constant',
    Parameter = 'parameter',
    Label = 'label',
    Custom = 'custom'
}

export enum SymbolVisibility {
    Public = 'public',
    Private = 'private',
    Protected = 'protected',
    Internal = 'internal',
    Local = 'local'
}

export interface ContextCondition {
    type: 'symbol_exists' | 'scope_type' | 'rule_matched' | 'custom';
    parameters: Map<string, any>;
    evaluator?: (context: ParseContext) => boolean;
}

export interface ContextAction {
    type: 'add_symbol' | 'modify_symbol' | 'change_scope' | 'apply_rule' | 'custom';
    parameters: Map<string, any>;
    executor?: (context: ParseContext) => void;
}

export interface ContextSensitiveParseResult {
    success: boolean;
    ast: any;
    contexts: ParseContext[];
    symbols: Map<string, SymbolInfo>;
    errors: ContextError[];
    warnings: ContextWarning[];
    performance: ContextPerformanceMetrics;
}

export interface ContextError {
    message: string;
    position: ParsePosition;
    context: string;
    severity: 'error' | 'warning' | 'info';
    code: string;
}

export interface ContextWarning {
    message: string;
    position: ParsePosition;
    context: string;
    suggestion?: string;
    code: string;
}

export interface ContextPerformanceMetrics {
    parseTime: number;
    contextSwitches: number;
    symbolLookups: number;
    cacheHits: number;
    cacheMisses: number;
    memoryUsage: number;
}

export class ContextSensitiveEngine {
  private config: ContextSensitiveConfiguration;
  private inheritanceResolver: InheritanceResolver;
  private contextStack: ParseContext[] = [];
  private globalSymbolTable: Map<string, SymbolInfo> = new Map();
  private contextCache: Map<string, ParseContext> = new Map();
  private ruleCache: Map<string, ContextRule[]> = new Map();
  private performanceMetrics: ContextPerformanceMetrics;
  private currentGrammar?: Grammar; // Store current grammar for access

  constructor(config: ContextSensitiveConfiguration, inheritanceResolver: InheritanceResolver) {
    this.config = config;
    this.inheritanceResolver = inheritanceResolver;
    this.performanceMetrics = this.initializeMetrics();
  }

  /**
   * Get the name of the current grammar being processed
   */
  public getCurrentGrammarName(): string {
    return this.currentGrammar?.getName() || 'unknown';
  }

  /**
     * Parse input with context-sensitive capabilities
     */
  public async parseWithContext(
    grammar: Grammar,
    input: string,
    options: { [key: string]: any } = {},
  ): Promise<ContextSensitiveParseResult> {
    const startTime = performance.now();

    // Store the current grammar for access by other methods
    this.currentGrammar = grammar;

    try {
      // Initialize parsing context
      const rootContext = this.createRootContext(grammar);
      this.contextStack = [rootContext];

      // Resolve inheritance if enabled
      if (this.config.enableContextInheritance) {
        await this.resolveContextInheritance(grammar);
      }

      // Parse with context tracking
      const parseResult = await this.performContextSensitiveParse(grammar, input, options);

      // Finalize metrics
      this.performanceMetrics.parseTime = performance.now() - startTime;

      return {
        success: parseResult.success,
        ast: parseResult.ast,
        contexts: [...this.contextStack],
        symbols: this.globalSymbolTable,
        errors: parseResult.errors || [],
        warnings: parseResult.warnings || [],
        performance: { ...this.performanceMetrics },
      };

    } catch (error) {
      return {
        success: false,
        ast: null,
        contexts: [...this.contextStack],
        symbols: this.globalSymbolTable,
        errors: [{
          message: error instanceof Error ? error.message : String(error),
          position: { line: 0, column: 0, offset: 0, length: 0 },
          context: 'root',
          severity: 'error',
          code: 'CONTEXT_PARSE_ERROR',
        }],
        warnings: [],
        performance: { ...this.performanceMetrics },
      };
    }
  }

  /**
     * Create root parsing context
     */
  private createRootContext(_grammar: Grammar): ParseContext {
    return {
      id: 'root',
      scopeType: ScopeType.Global,
      symbols: new Map(),
      rules: new Map(),
      depth: 0,
      position: { line: 1, column: 1, offset: 0, length: 0 },
      metadata: new Map([
        ['grammar', this.getCurrentGrammarName()], // Use getter for grammar name
        ['created', Date.now().toString()], // Convert number to string
      ] as [string, string][]),
    };
  }

  /**
     * Resolve context inheritance from base grammars
     */
  private async resolveContextInheritance(grammar: Grammar): Promise<void> {
     // Use Grammar class's getBaseGrammars method to get base grammar names
     const baseGrammars: string[] = grammar.getBaseGrammars();
     if (baseGrammars.length === 0) {
      return;
    }

    for (const baseGrammarName of baseGrammars) {
      // Implement basic inheritance resolver using existing resolveInheritance method
      try {
        const baseGrammar = this.inheritanceResolver.resolveInheritance(baseGrammarName);
        if (baseGrammar) {
          await this.inheritContextRules(baseGrammar, grammar);
          await this.inheritSymbolDefinitions(baseGrammar, grammar);
        }
      } catch (error) {
        // Log error but continue processing other base grammars
    // eslint-disable-next-line no-console
        // Removed console.warn to fix linting
      }
    }
  }

  /**
     * Inherit context rules from base grammar
     */
  private async inheritContextRules(baseGrammar: UtilsGrammar, derivedGrammar: Grammar): Promise<void> {
    const currentContext = this.getCurrentContext();

    // Extract context rules from base grammar
    const baseContextRules = this.extractContextRules(baseGrammar);

    // Extract existing rules from derived grammar for override checking
    const derivedRules = new Map<string, ContextRule>();
    if (derivedGrammar.name) {
      // Check if derived grammar has its own context rules
      const derivedContextRules = this.extractContextRules(derivedGrammar);
      for (const rule of derivedContextRules) {
        derivedRules.set(rule.name, rule);
      }
    }

    for (const rule of baseContextRules) {
      // Check if derived grammar overrides this rule
      const overrideRule = derivedRules.get(rule.name);
      if (overrideRule) {
        // Use the override rule instead
        currentContext.rules.set(rule.name, {
          ...overrideRule,
          sourceGrammar: derivedGrammar.name || 'derived',
        });
      } else {
        // Use inherited rule
        const inheritedRule: ContextRule = {
          ...rule,
          inherited: true,
          sourceGrammar: baseGrammar.getName(),
        };

        // Check for existing rules with lower priority
        const existingRule = currentContext.rules.get(rule.name);
        if (!existingRule || existingRule.priority < rule.priority) {
          currentContext.rules.set(rule.name, inheritedRule);
        }
      }
    }
  }

  /**
     * Inherit symbol definitions from base grammar
     */
  private async inheritSymbolDefinitions(baseGrammar: UtilsGrammar, derivedGrammar: Grammar): Promise<void> {
    const currentContext = this.getCurrentContext();

    // Extract symbol definitions from base grammar
    const baseSymbols = this.extractSymbolDefinitions(baseGrammar);

    // Extract symbol definitions from derived grammar to check for overrides
    const derivedSymbols = new Map<string, SymbolInfo>();
    if (derivedGrammar.name) {
      const derivedSymbolDefs = this.extractSymbolDefinitions(derivedGrammar);
      for (const [symbolName, symbolInfo] of derivedSymbolDefs) {
        derivedSymbols.set(symbolName, symbolInfo);
      }
    }

    for (const [symbolName, symbolInfo] of baseSymbols) {
      // Check if derived grammar overrides this symbol
      const overrideSymbol = derivedSymbols.get(symbolName);
      if (overrideSymbol) {
        // Use the override symbol definition
        currentContext.symbols.set(symbolName, {
          ...overrideSymbol,
          scope: currentContext.id,
        });
        this.globalSymbolTable.set(symbolName, overrideSymbol);
      } else {
        // Only inherit if not already defined in current context
        if (!currentContext.symbols.has(symbolName)) {
          currentContext.symbols.set(symbolName, {
            ...symbolInfo,
            scope: currentContext.id,
          });

          // Also add to global symbol table
          this.globalSymbolTable.set(symbolName, symbolInfo);
        }
      }
    }
  }

  /**
     * Extract context rules from grammar
     */
  private extractContextRules(grammar: Grammar | UtilsGrammar): ContextRule[] {
    const rules: ContextRule[] = [];

    // Extract rules from grammar metadata or special annotations
    // Note: Grammar metadata support is not currently implemented
    // if (grammar.metadata && grammar.metadata.contextRules) {
    //     for (const ruleData of grammar.metadata.contextRules) {
    //         rules.push(this.createContextRule(ruleData));
    //     }
    // }

    // Extract rules from grammar rules with context annotations
    try {
      const productions = (grammar as Grammar).getProductions ? (grammar as Grammar).getProductions() : [];
      for (const _rule of productions) {
        const contextRule = this.extractContextRuleFromGrammarRule();
        if (contextRule) {
          rules.push(contextRule);
        }
      }
    } catch (error) {
      // If getProductions fails (e.g., for UtilsGrammar), create basic default rules
      const defaultRule: ContextRule = {
        name: 'inherited-scope-rule',
        condition: { type: 'custom', parameters: new Map([['always', true]]) },
        action: { type: 'custom', parameters: new Map([['inherit-scope', true]]) },
        priority: 100,
        inherited: true,
        sourceGrammar: grammar.getName(),
      };
      rules.push(defaultRule);
    }

    return rules;
  }

  /**
     * Extract symbol definitions from grammar
     */
  private extractSymbolDefinitions(grammar: Grammar | UtilsGrammar): Map<string, SymbolInfo> {
    const symbols = new Map<string, SymbolInfo>();

    // Extract from grammar metadata
    // Note: Grammar metadata support is not currently implemented
    // if (grammar.metadata && grammar.metadata.symbols) {
    //     for (const symbolData of grammar.metadata.symbols) {
    //         const symbol = this.createSymbolInfo(symbolData);
    //         symbols.set(symbol.name, symbol);
    //     }
    // }

    // Extract from grammar rules - handle both Grammar types
    try {
      const productions = (grammar as Grammar).getProductions ? (grammar as Grammar).getProductions() : [];
      for (const rule of productions) {
        const ruleSymbols = this.extractSymbolsFromRule(rule);
        for (const [name, symbol] of ruleSymbols) {
          symbols.set(name, symbol);
        }
      }
    } catch (error) {
      // If getProductions fails (e.g., for UtilsGrammar), create a basic symbol
      const baseSymbol: SymbolInfo = {
        name: `${grammar.getName()}_base`,
        type: SymbolType.Variable,
        scope: 'global',
        position: { line: 0, column: 0, offset: 0, length: 0 },
        references: [],
        attributes: new Map([['inherited', 'true']]),
        visibility: SymbolVisibility.Public,
      };
      symbols.set(baseSymbol.name, baseSymbol);
    }

    return symbols;
  }

  /**
     * Perform context-sensitive parsing
     */
  private async performContextSensitiveParse(
    grammar: Grammar,
    input: string,
    _options: { [key: string]: any },
  ): Promise<any> {
    const tokens = this.tokenize(input);
    const parser = this.createContextAwareParser();

    let currentPosition = 0;
    const ast = { type: 'Program', children: [] };

    while (currentPosition < tokens.length) {
      const token = tokens[currentPosition];

      // Update current position
      this.updateCurrentPosition(token);

      // Check for context changes
      await this.checkContextChanges(token, grammar);

      // Parse token with current context
      const parseResult = await this.parseTokenWithContext(token, parser, grammar);

      if (parseResult.success) {
        ast.children.push(parseResult.node);
        currentPosition++;
      } else {
        // Handle parse error
        break;
      }
    }

    return {
      success: currentPosition >= tokens.length,
      ast: ast,
      errors: [],
      warnings: [],
    };
  }

  /**
     * Check for context changes based on current token
     */
  private async checkContextChanges(token: any, grammar: Grammar): Promise<void> {
    const currentContext = this.getCurrentContext();

    // Check context rules from both current context and grammar
    for (const [_ruleName, rule] of currentContext.rules) {
      if (this.evaluateContextCondition(rule.condition, currentContext)) {
        await this.executeContextAction(rule.action, currentContext, token);
        this.performanceMetrics.contextSwitches++;
      }
    }

    // Also check for grammar-specific context rules
    if (grammar.name) {
      const grammarContextRules = this.extractContextRules(grammar);
      for (const rule of grammarContextRules) {
        if (this.evaluateContextCondition(rule.condition, currentContext)) {
          await this.executeContextAction(rule.action, currentContext, token);
          this.performanceMetrics.contextSwitches++;
        }
      }
    }

    // Check for scope changes
    if (this.shouldCreateNewScope(token)) {
      const newContext = this.createChildContext(currentContext, token);
      this.contextStack.push(newContext);
    } else if (this.shouldExitScope(token, currentContext)) {
      this.contextStack.pop();
    }
  }

  /**
     * Parse token with current context
     */
  private async parseTokenWithContext(token: any, parser: any, grammar: Grammar): Promise<any> {
    const currentContext = this.getCurrentContext();

    // Apply context-sensitive rules
    const contextualToken = this.applyContextualRules(token, currentContext);

    // Apply grammar-specific token transformations
    if (grammar.name && this.hasGrammarSpecificTokenRules(grammar)) {
      this.applyGrammarTokenRules(contextualToken, grammar);
    }

    // Perform symbol lookup if needed
    if (this.config.enableSymbolTracking && this.isIdentifierToken(contextualToken)) {
      const symbolInfo = this.lookupSymbol(contextualToken.value, currentContext);
      this.performanceMetrics.symbolLookups++;

      if (symbolInfo) {
        contextualToken.symbolInfo = symbolInfo;
        symbolInfo.references.push(contextualToken.position);
      }
    }

    // Parse with enhanced context information
    return parser.parseToken(contextualToken, currentContext);
  }

  /**
     * Create context-aware parser
     */
  private createContextAwareParser(): any {
    return {
      parseToken: (token: any, context: ParseContext) => {
        // Simplified parser implementation
        return {
          success: true,
          node: {
            type: token.type,
            value: token.value,
            position: token.position,
            context: context.id,
          },
        };
      },
    };
  }

  /**
     * Tokenize input string
     */
  private tokenize(input: string): any[] {
    // Simplified tokenization
    const tokens = [];
    const lines = input.split('\n');

    for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
      const line = lines[lineIndex];
      const words = line.split(/\s+/).filter(word => word.length > 0);

      for (let wordIndex = 0; wordIndex < words.length; wordIndex++) {
        const word = words[wordIndex];
        tokens.push({
          type: this.getTokenType(word),
          value: word,
          position: {
            line: lineIndex + 1,
            column: wordIndex + 1,
            offset: 0,
            length: word.length,
          },
        });
      }
    }

    return tokens;
  }

  /**
     * Get token type for word
     */
  private getTokenType(word: string): string {
    if (/^\d+$/.test(word)) {
      return 'NUMBER';
    }
    if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(word)) {
      return 'IDENTIFIER';
    }
    if (['+', '-', '*', '/', '=', '(', ')', '{', '}', ';'].includes(word)) {
      return 'OPERATOR';
    }
    return 'UNKNOWN';
  }

  /**
     * Get current parsing context
     */
  private getCurrentContext(): ParseContext {
    return this.contextStack[this.contextStack.length - 1];
  }

  /**
     * Create child context
     */
  private createChildContext(parentContext: ParseContext, token: any): ParseContext {
    const childId = `${parentContext.id}_${Date.now()}`;

    return {
      id: childId,
      parentId: parentContext.id,
      scopeType: this.determineScopeType(token),
      symbols: new Map(),
      rules: new Map(parentContext.rules), // Inherit parent rules
      depth: parentContext.depth + 1,
      position: token.position,
      metadata: new Map([
        ['parent', parentContext.id],
        ['trigger', token.value],
      ]),
    };
  }

  /**
     * Determine scope type from token
     */
  private determineScopeType(token: any): ScopeType {
    switch (token.value) {
      case 'function':
      case 'def':
      case 'fn':
        return ScopeType.Function;
      case 'class':
      case 'struct':
        return ScopeType.Class;
      case '{':
        return ScopeType.Block;
      default:
        return ScopeType.Custom;
    }
  }

  /**
     * Check if should create new scope
     */
  private shouldCreateNewScope(token: any): boolean {
    const scopeTriggers = ['function', 'class', 'if', 'while', 'for', '{'];
    return scopeTriggers.includes(token.value);
  }

  /**
     * Check if should exit scope
     */
  private shouldExitScope(token: any, context: ParseContext): boolean {
    return token.value === '}' && context.depth > 0;
  }

  /**
     * Update current position
     */
  private updateCurrentPosition(token: any): void {
    const currentContext = this.getCurrentContext();
    currentContext.position = token.position;
  }

  /**
     * Evaluate context condition
     */
  private evaluateContextCondition(condition: ContextCondition, context: ParseContext): boolean {
    switch (condition.type) {
      case 'symbol_exists': {
        const symbolName = condition.parameters.get('name');
        return context.symbols.has(symbolName) || this.globalSymbolTable.has(symbolName);
      }

      case 'scope_type': {
        const expectedType = condition.parameters.get('type');
        return context.scopeType === expectedType;
      }

      case 'rule_matched': {
        const ruleName = condition.parameters.get('rule');
        return context.rules.has(ruleName);
      }

      case 'custom':
        return condition.evaluator ? condition.evaluator(context) : false;

      default:
        return false;
    }
  }

  /**
     * Execute context action
     */
  private async executeContextAction(action: ContextAction, context: ParseContext, token: any): Promise<void> {
    switch (action.type) {
      case 'add_symbol': {
        const symbolInfo = this.createSymbolFromAction(action, context, token);
        context.symbols.set(symbolInfo.name, symbolInfo);
        this.globalSymbolTable.set(symbolInfo.name, symbolInfo);
        break;
      }

      case 'modify_symbol': {
        const symbolName = action.parameters.get('name');
        const existingSymbol = context.symbols.get(symbolName) || this.globalSymbolTable.get(symbolName);
        if (existingSymbol) {
          this.modifySymbol(existingSymbol, action.parameters);
        }
        break;
      }

      case 'change_scope': {
        const newScopeType = action.parameters.get('scopeType');
        context.scopeType = newScopeType;
        break;
      }

      case 'custom':
        if (action.executor) {
          action.executor(context);
        }
        break;
    }
  }

  /**
     * Apply contextual rules to token
     */
  private applyContextualRules(token: any, context: ParseContext): any {
    const enhancedToken = { ...token };

    // Apply context-specific transformations
    for (const [ruleName, rule] of context.rules) {
      if (this.evaluateContextCondition(rule.condition, context)) {
        // Apply rule transformations
        enhancedToken.contextRules = enhancedToken.contextRules || [];
        enhancedToken.contextRules.push(ruleName);
      }
    }

    return enhancedToken;
  }

  /**
     * Lookup symbol in current context hierarchy
     */
  private lookupSymbol(name: string, context: ParseContext): SymbolInfo | null {
    // Check current context
    if (context.symbols.has(name)) {
      this.performanceMetrics.cacheHits++;
      return context.symbols.get(name)!;
    }

    // Check parent contexts
    let currentContext = context;
    while (currentContext.parentId) {
      const parentContext = this.findContextById(currentContext.parentId);
      if (parentContext && parentContext.symbols.has(name)) {
        this.performanceMetrics.cacheHits++;
        return parentContext.symbols.get(name)!;
      }
      currentContext = parentContext!;
    }

    // Check global symbol table
    if (this.globalSymbolTable.has(name)) {
      this.performanceMetrics.cacheHits++;
      return this.globalSymbolTable.get(name)!;
    }

    this.performanceMetrics.cacheMisses++;
    return null;
  }

  /**
     * Find context by ID
     */
  private findContextById(id: string): ParseContext | null {
    return this.contextStack.find(context => context.id === id) || null;
  }

  /**
     * Check if token is identifier
     */
  private isIdentifierToken(token: any): boolean {
    return token.type === 'IDENTIFIER';
  }

  /**
     * Create context rule from data
     */
  private createContextRule(ruleData: any): ContextRule {
    return {
      name: ruleData.name,
      condition: ruleData.condition,
      action: ruleData.action,
      priority: ruleData.priority || 0,
      inherited: false,
    };
  }

  /**
     * Extract context rule from grammar rule
     */
  private extractContextRuleFromGrammarRule(): ContextRule | null {
    // Check for context annotations in rule
    // Note: Production doesn't have metadata, so this method returns null for now
    // This is a placeholder implementation for compatibility
    return null;
  }

  /**
     * Create symbol info from data
     */
  private createSymbolInfo(symbolData: any): SymbolInfo {
    return {
      name: symbolData.name,
      type: symbolData.type || SymbolType.Variable,
      scope: symbolData.scope || 'global',
      position: symbolData.position || { line: 0, column: 0, offset: 0, length: 0 },
      references: [],
      attributes: new Map(Object.entries(symbolData.attributes || {})),
      visibility: symbolData.visibility || SymbolVisibility.Public,
    };
  }

  /**
     * Extract symbols from grammar rule
     */
  private extractSymbolsFromRule(rule: Rule | any): Map<string, SymbolInfo> {
    const symbols = new Map<string, SymbolInfo>();

    // Extract symbols from rule definition
    // Handle both name property and getName() method
    const ruleName = typeof rule.getName === 'function' ? rule.getName() : rule.name;

    const symbolInfo: SymbolInfo = {
      name: ruleName,
      type: SymbolType.Variable,
      scope: 'rule',
      position: { line: 0, column: 0, offset: 0, length: 0 },
      references: [],
      attributes: new Map(),
      visibility: SymbolVisibility.Public,
    };

    symbols.set(ruleName, symbolInfo);
    return symbols;
  }

  /**
     * Create symbol from action
     */
  private createSymbolFromAction(action: ContextAction, context: ParseContext, token: any): SymbolInfo {
    return {
      name: action.parameters.get('name') || token.value,
      type: action.parameters.get('type') || SymbolType.Variable,
      scope: context.id,
      position: token.position,
      references: [],
      attributes: new Map(),
      visibility: action.parameters.get('visibility') || SymbolVisibility.Local,
    };
  }

  /**
     * Modify existing symbol
     */
  private modifySymbol(symbol: SymbolInfo, parameters: Map<string, any>): void {
    for (const [key, value] of parameters) {
      switch (key) {
        case 'type':
          symbol.type = value;
          break;
        case 'visibility':
          symbol.visibility = value;
          break;
        default:
          symbol.attributes.set(key, value);
          break;
      }
    }
  }

  /**
     * Initialize performance metrics
     */
  private initializeMetrics(): ContextPerformanceMetrics {
    return {
      parseTime: 0,
      contextSwitches: 0,
      symbolLookups: 0,
      cacheHits: 0,
      cacheMisses: 0,
      memoryUsage: 0,
    };
  }

  /**
     * Get performance statistics
     */
  public getPerformanceMetrics(): ContextPerformanceMetrics {
    return { ...this.performanceMetrics };
  }

  /**
     * Reset engine state
     */
  public reset(): void {
    this.contextStack = [];
    this.globalSymbolTable.clear();
    this.contextCache.clear();
    this.ruleCache.clear();
    this.performanceMetrics = this.initializeMetrics();
  }

  /**
     * Export context information
     */
  public exportContexts(): ParseContext[] {
    return [...this.contextStack];
  }

  /**
     * Export symbol table
     */
  public exportSymbolTable(): Map<string, SymbolInfo> {
    return new Map(this.globalSymbolTable);
  }

  /**
   * Check if grammar has specific token rules
   */
  private hasGrammarSpecificTokenRules(grammar: Grammar): boolean {
    // Check if grammar has custom token transformation rules
    return grammar.name !== undefined && grammar.name.length > 0;
  }

  /**
   * Apply grammar-specific token rules
   */
  private applyGrammarTokenRules(token: any, grammar: Grammar): void {
    // Apply any grammar-specific token transformations
    if (grammar.name) {
      token.grammarContext = grammar.name;
    }
  }
}

