/**
 * Minotaur Embedded Language Context Manager
 *
 * Manages context switching between embedded languages in multi-language documents.
 * Extends the existing ContextSensitiveEngine to support embedded language parsing.
 */

import { Grammar, SymbolDefinition, ReferenceDefinition, ValidationRule, SymbolTableSharingStrategy, ValidationSeverity } from '../core/grammar/Grammar';
import { ContextSensitiveEngine, ParseContext, SymbolInfo, ContextRule } from './ContextSensitiveEngine';

export interface EmbeddedLanguageContext extends ParseContext {
    language: string;
    parentLanguage?: string;
    embeddedGrammars: Map<string, Grammar>;
    contextSwitchStack: ContextSwitchFrame[];
    symbolTableHierarchy: SymbolTableLevel[];
    crossLanguageReferences: CrossLanguageReference[];
    validationQueue: ValidationQueueItem[];
}

export interface ContextSwitchFrame {
    fromLanguage: string;
    toLanguage: string;
    switchPosition: ParsePosition;
    switchTrigger: string;
    expectedEndTrigger: string;
    nestingLevel: number;
    switchTime: number;
}

export interface SymbolTableLevel {
    language: string;
    level: number;
    symbols: Map<string, SymbolInfo>;
    parentLevel?: SymbolTableLevel;
    childLevels: SymbolTableLevel[];
    sharingStrategy: SymbolTableSharingStrategy;
}

export interface CrossLanguageReference {
    id: string;
    sourceLanguage: string;
    targetLanguage: string;
    sourceSymbol: string;
    targetSymbol: string;
    sourceType?: string;
    sourcePosition: ParsePosition;
    resolved: boolean;
    validationResult?: ValidationResult;
}

export interface ValidationQueueItem {
    rule: ValidationRule;
    reference: CrossLanguageReference;
    priority: number;
    scheduled: boolean;
    result?: ValidationResult;
}

export interface ValidationResult {
    valid: boolean;
    severity: ValidationSeverity;
    message: string;
    suggestions: string[];
    relatedReferences: string[];
}

export interface ParsePosition {
    line: number;
    column: number;
    offset: number;
    length: number;
}


export interface EmbeddedLanguageParseResult {
    success: boolean;
    ast: any;
    contexts: EmbeddedLanguageContext[];
    crossLanguageReferences: CrossLanguageReference[];
    validationResults: ValidationResult[];
    contextSwitches: ContextSwitchFrame[];
    performance: EmbeddedLanguagePerformanceMetrics;
}

export interface EmbeddedLanguagePerformanceMetrics {
    totalParseTime: number;
    contextSwitchTime: number;
    validationTime: number;
    symbolResolutionTime: number;
    contextSwitchCount: number;
    crossLanguageReferenceCount: number;
    validationRuleCount: number;
    memoryUsage: number;
}

export class EmbeddedLanguageContextManager {
  private primaryGrammar: Grammar;
  private embeddedGrammars: Map<string, Grammar>;
  private contextStack: EmbeddedLanguageContext[];
  private globalSymbolHierarchy: SymbolTableLevel;
  private crossLanguageReferences: Map<string, CrossLanguageReference>;
  private validationQueue: ValidationQueueItem[];
  private contextSwitchPatterns: Map<string, ContextSwitchPattern>;
  private performanceMetrics: EmbeddedLanguagePerformanceMetrics;

  constructor(primaryGrammar: Grammar) {
    this.primaryGrammar = primaryGrammar;
    this.embeddedGrammars = new Map();
    this.contextStack = [];
    this.crossLanguageReferences = new Map();
    this.validationQueue = [];
    this.contextSwitchPatterns = new Map();
    this.performanceMetrics = this.initializeMetrics();

    this.initializeEmbeddedGrammars();
    this.initializeContextSwitchPatterns();
    this.initializeSymbolHierarchy();
  }

  /**
     * Initialize embedded grammars from primary grammar metadata
     */
  private initializeEmbeddedGrammars(): void {
    const embeddedLanguages = this.primaryGrammar.getEmbeddedLanguages();

    for (const language of embeddedLanguages) {
      const importedGrammars = this.primaryGrammar.getImportedGrammars();
      const grammarEntry = Array.from(importedGrammars).find(entry => entry.startsWith(`${language}:`));
      const grammarPath = grammarEntry ? grammarEntry.split(':')[1] : null;

      if (grammarPath) {
        // In a real implementation, this would load the grammar from file
        // For now, we'll create a placeholder
        const embeddedGrammar = this.createPlaceholderGrammar(language);
        this.embeddedGrammars.set(language, embeddedGrammar);
      }
    }
  }

  /**
     * Initialize context switch patterns based on grammar rules
     */
  private initializeContextSwitchPatterns(): void {
    // Extract context switch patterns from grammar productions
    const productions = this.primaryGrammar.rules;

    for (const production of productions) {
      const contextSwitches = this.extractContextSwitchPatterns(production);
      for (const pattern of contextSwitches) {
        this.contextSwitchPatterns.set(pattern.trigger, pattern);
      }
    }
  }

  /**
     * Initialize symbol table hierarchy
     */
  private initializeSymbolHierarchy(): void {
    this.globalSymbolHierarchy = {
      language: this.primaryGrammar.getName(),
      level: 0,
      symbols: new Map(),
      childLevels: [],
      sharingStrategy: this.primaryGrammar.getSymbolTableSharing(),
    };

    // Create child levels for embedded languages
    for (const [language, grammar] of this.embeddedGrammars) {
      const childLevel: SymbolTableLevel = {
        language: language,
        level: 1,
        symbols: new Map(),
        parentLevel: this.globalSymbolHierarchy,
        childLevels: [],
        sharingStrategy: grammar.getSymbolTableSharing(),
      };

      this.globalSymbolHierarchy.childLevels.push(childLevel);
    }
  }

  /**
     * Parse input with embedded language support
     */
  public async parseWithEmbeddedLanguages(
    input: string,
    options: { [key: string]: any } = {},
  ): Promise<EmbeddedLanguageParseResult> {
    const startTime = performance.now();

    try {
      // Initialize root context
      const rootContext = this.createRootEmbeddedContext();
      this.contextStack = [rootContext];

      // Tokenize input with context awareness
      const tokens = await this.tokenizeWithContextAwareness(input);

      // Parse with context switching
      const parseResult = await this.parseWithContextSwitching(tokens, options);

      // Resolve cross-language references
      await this.resolveCrossLanguageReferences();

      // Validate cross-language references
      const validationResults = await this.validateCrossLanguageReferences();

      // Finalize metrics
      this.performanceMetrics.totalParseTime = performance.now() - startTime;

      return {
        success: parseResult.success,
        ast: parseResult.ast,
        contexts: [...this.contextStack],
        crossLanguageReferences: Array.from(this.crossLanguageReferences.values()),
        validationResults: validationResults,
        contextSwitches: this.extractContextSwitches(),
        performance: { ...this.performanceMetrics },
      };

    } catch (error) {
      return {
        success: false,
        ast: null,
        contexts: [...this.contextStack],
        crossLanguageReferences: [],
        validationResults: [{
          valid: false,
          severity: ValidationSeverity.ERROR,
          message: `Embedded language parsing failed: ${error instanceof Error ? error.message : String(error)}`,
          suggestions: ['Check grammar syntax', 'Verify embedded language imports'],
          relatedReferences: [],
        }],
        contextSwitches: [],
        performance: { ...this.performanceMetrics },
      };
    }
  }

  /**
     * Create root embedded language context
     */
  private createRootEmbeddedContext(): EmbeddedLanguageContext {
    return {
      id: 'root',
      language: this.primaryGrammar.getName(),
      scopeType: 'global' as any,
      symbols: new Map(),
      rules: new Map(),
      depth: 0,
      position: { line: 1, column: 1, offset: 0, length: 0 },
      metadata: new Map([
        ['grammar', this.primaryGrammar.getName()],
        ['created', Date.now().toString()],
      ]),
      embeddedGrammars: new Map(this.embeddedGrammars),
      contextSwitchStack: [],
      symbolTableHierarchy: [this.globalSymbolHierarchy],
      crossLanguageReferences: [],
      validationQueue: [],
    };
  }

  /**
     * Tokenize input with context awareness for embedded languages
     */
  private async tokenizeWithContextAwareness(input: string): Promise<EmbeddedLanguageToken[]> {
    const tokens: EmbeddedLanguageToken[] = [];
    let currentPosition = 0;
    let currentLine = 1;
    let currentColumn = 1;
    let currentLanguage = this.primaryGrammar.getName();

    while (currentPosition < input.length) {
      // Check for context switch triggers
      const contextSwitch = this.detectContextSwitch(input, currentPosition, currentLanguage);

      if (contextSwitch) {
        // Create context switch token
        const switchToken = this.createContextSwitchToken(contextSwitch, currentPosition, currentLine, currentColumn);
        tokens.push(switchToken);

        // Update position and language
        currentPosition = contextSwitch.endPosition;
        currentLanguage = contextSwitch.targetLanguage;

        // Update line and column tracking
        const switchText = input.substring(contextSwitch.startPosition, contextSwitch.endPosition);
        const lines = switchText.split('\n');
        if (lines.length > 1) {
          currentLine += lines.length - 1;
          currentColumn = lines[lines.length - 1].length + 1;
        } else {
          currentColumn += switchText.length;
        }

        continue;
      }

      // Tokenize current character/sequence in current language context
      const token = await this.tokenizeInLanguageContext(
        input,
        currentPosition,
        currentLine,
        currentColumn,
        currentLanguage,
      );

      if (token) {
        tokens.push(token);
        currentPosition = token.position.offset + token.position.length;

        // Update line and column
        if (token.value.includes('\n')) {
          const lines = token.value.split('\n');
          currentLine += lines.length - 1;
          currentColumn = lines[lines.length - 1].length + 1;
        } else {
          currentColumn += token.position.length;
        }
      } else {
        // Skip unknown character
        currentPosition++;
        currentColumn++;
      }
    }

    return tokens;
  }

  /**
     * Parse tokens with context switching support
     */
  private async parseWithContextSwitching(
    tokens: EmbeddedLanguageToken[],
    options: { [key: string]: any },
  ): Promise<any> {
    const ast = { type: 'EmbeddedLanguageProgram', children: [], metadata: {} };
    let currentTokenIndex = 0;

    while (currentTokenIndex < tokens.length) {
      const token = tokens[currentTokenIndex];

      if (token.type === 'CONTEXT_SWITCH') {
        // Handle context switch
        await this.handleContextSwitch(token);
        this.performanceMetrics.contextSwitchCount++;
      } else {
        // Parse token in current context
        const parseResult = await this.parseTokenInCurrentContext(token);

        if (parseResult.success) {
          ast.children.push(parseResult.node);

          // Check for symbol definitions and references
          await this.processSymbolInformation(token, parseResult.node);
        } else {
          // Handle parse error
    // eslint-disable-next-line no-console
          console.warn(`Failed to parse token: ${token.value}`);
        }
      }

      currentTokenIndex++;
    }

    return {
      success: true,
      ast: ast,
    };
  }

  /**
     * Handle context switch between languages
     */
  private async handleContextSwitch(token: EmbeddedLanguageToken): Promise<void> {
    const switchInfo = token.contextSwitchInfo;
    if (!switchInfo) {
      return;
    }

    const currentContext = this.getCurrentContext();
    const switchStartTime = performance.now();

    // Create context switch frame
    const switchFrame: ContextSwitchFrame = {
      fromLanguage: currentContext.language,
      toLanguage: switchInfo.targetLanguage,
      switchPosition: token.position,
      switchTrigger: switchInfo.trigger,
      expectedEndTrigger: switchInfo.endTrigger,
      nestingLevel: currentContext.contextSwitchStack.length,
      switchTime: switchStartTime,
    };

    // Push switch frame to stack
    currentContext.contextSwitchStack.push(switchFrame);

    // Create new context for target language
    const targetGrammar = this.embeddedGrammars.get(switchInfo.targetLanguage);
    if (targetGrammar) {
      const newContext = this.createEmbeddedContext(
        currentContext,
        switchInfo.targetLanguage,
        targetGrammar,
        token.position,
      );

      this.contextStack.push(newContext);
    }

    // Update performance metrics
    this.performanceMetrics.contextSwitchTime += performance.now() - switchStartTime;
  }

  /**
     * Create embedded language context
     */
  private createEmbeddedContext(
    parentContext: EmbeddedLanguageContext,
    language: string,
    grammar: Grammar,
    position: ParsePosition,
  ): EmbeddedLanguageContext {
    const contextId = `${parentContext.id}_${language}_${Date.now()}`;

    return {
      id: contextId,
      parentId: parentContext.id,
      language: language,
      parentLanguage: parentContext.language,
      scopeType: 'block' as any,
      symbols: new Map(),
      rules: new Map(),
      depth: parentContext.depth + 1,
      position: position,
      metadata: new Map([
        ['parent', parentContext.id],
        ['language', language],
        ['grammar', grammar.getName()],
      ]),
      embeddedGrammars: new Map(parentContext.embeddedGrammars),
      contextSwitchStack: [...parentContext.contextSwitchStack],
      symbolTableHierarchy: this.createSymbolTableHierarchy(parentContext, language),
      crossLanguageReferences: [],
      validationQueue: [],
    };
  }

  /**
     * Resolve cross-language references
     */
  private async resolveCrossLanguageReferences(): Promise<void> {
    const resolutionStartTime = performance.now();

    for (const [_refId, reference] of this.crossLanguageReferences) {
      if (!reference.resolved) {
        const resolved = await this.resolveReference(reference);
        reference.resolved = resolved;

        if (resolved) {
          // Schedule validation
          this.scheduleValidation(reference);
        }
      }
    }

    this.performanceMetrics.symbolResolutionTime = performance.now() - resolutionStartTime;
  }

  /**
     * Validate cross-language references
     */
  private async validateCrossLanguageReferences(): Promise<ValidationResult[]> {
    const validationStartTime = performance.now();
    const results: ValidationResult[] = [];

    // Process validation queue
    for (const queueItem of this.validationQueue) {
      if (queueItem.scheduled && !queueItem.result) {
        const result = await this.validateReference(queueItem.reference, queueItem.rule);
        queueItem.result = result;
        results.push(result);
      }
    }

    this.performanceMetrics.validationTime = performance.now() - validationStartTime;
    this.performanceMetrics.validationRuleCount = this.validationQueue.length;

    return results;
  }

  /**
     * Validate a cross-language reference
     */
  private async validateReference(
    reference: CrossLanguageReference,
    rule: ValidationRule,
  ): Promise<ValidationResult> {
    // Find target symbol in target language context
    const targetSymbol = await this.findSymbolInLanguage(
      reference.targetSymbol,
      reference.targetLanguage,
    );

    if (!targetSymbol) {
      return {
        valid: false,
        severity: ValidationSeverity.ERROR,
        message: `Symbol '${reference.targetSymbol}' not found in ${reference.targetLanguage}`,
        suggestions: [
          `Check if '${reference.targetSymbol}' is defined in ${reference.targetLanguage}`,
          'Verify the symbol name spelling',
          'Ensure the symbol is in scope',
        ],
        relatedReferences: [],
      };
    }

    // Validate according to rule
    const isValid = this.evaluateValidationRule(rule, reference, targetSymbol);

    return {
      valid: isValid,
      severity: isValid ? ValidationSeverity.INFO : rule.severity,
      message: isValid
        ? `Cross-language reference '${reference.sourceSymbol}' -> '${reference.targetSymbol}' is valid`
        : `Cross-language reference validation failed: ${rule.name}`,
      suggestions: isValid ? [] : this.generateValidationSuggestions(rule, reference),
      relatedReferences: this.findRelatedReferences(reference),
    };
  }

  // Helper methods and utilities

  private createPlaceholderGrammar(language: string): Grammar {
    const grammar = new Grammar(language);
    grammar.setIsEmbeddedLanguage(true);
    grammar.setParentContext(this.primaryGrammar.getName());
    return grammar;
  }

  private extractContextSwitchPatterns(production: any): ContextSwitchPattern[] {
    // Extract @CONTEXT[language] patterns from production rules
    const patterns: ContextSwitchPattern[] = [];
    // Implementation would parse production rules for context switch patterns
    return patterns;
  }

  private detectContextSwitch(
    input: string,
    position: number,
    currentLanguage: string,
  ): ContextSwitchInfo | null {
    // Detect context switch triggers in input
    for (const [trigger, pattern] of this.contextSwitchPatterns) {
      if (input.substring(position).startsWith(trigger)) {
        return {
          trigger: trigger,
          targetLanguage: pattern.targetLanguage,
          endTrigger: pattern.endTrigger,
          startPosition: position,
          endPosition: position + trigger.length,
        };
      }
    }
    return null;
  }

  private createContextSwitchToken(
    contextSwitch: ContextSwitchInfo,
    position: number,
    line: number,
    column: number,
  ): EmbeddedLanguageToken {
    return {
      type: 'CONTEXT_SWITCH',
      value: contextSwitch.trigger,
      position: {
        line: line,
        column: column,
        offset: position,
        length: contextSwitch.trigger.length,
      },
      language: contextSwitch.targetLanguage,
      contextSwitchInfo: contextSwitch,
    };
  }

  private async tokenizeInLanguageContext(
    input: string,
    position: number,
    line: number,
    column: number,
    language: string,
  ): Promise<EmbeddedLanguageToken | null> {
    // Tokenize based on current language context
    const grammar = language === this.primaryGrammar.getName()
      ? this.primaryGrammar
      : this.embeddedGrammars.get(language);

    if (!grammar) {
      return null;
    }

    // Simple tokenization - in real implementation, use grammar-specific tokenizer
    const char = input[position];
    if (/\s/.test(char)) {
      return {
        type: 'WHITESPACE',
        value: char,
        position: { line, column, offset: position, length: 1 },
        language: language,
      };
    }

    // Extract word or symbol
    let tokenEnd = position + 1;
    while (tokenEnd < input.length && /[a-zA-Z0-9_]/.test(input[tokenEnd])) {
      tokenEnd++;
    }

    const tokenValue = input.substring(position, tokenEnd);
    return {
      type: this.getTokenTypeForLanguage(tokenValue, language),
      value: tokenValue,
      position: { line, column, offset: position, length: tokenValue.length },
      language: language,
    };
  }

  private getTokenTypeForLanguage(value: string, language: string): string {
    // Determine token type based on language context
    if (/^\d+$/.test(value)) {
      return 'NUMBER';
    }
    if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(value)) {
      return 'IDENTIFIER';
    }
    return 'SYMBOL';
  }

  private getCurrentContext(): EmbeddedLanguageContext {
    return this.contextStack[this.contextStack.length - 1];
  }

  private async parseTokenInCurrentContext(token: EmbeddedLanguageToken): Promise<any> {
    // Parse token in current language context
    return {
      success: true,
      node: {
        type: token.type,
        value: token.value,
        position: token.position,
        language: token.language,
      },
    };
  }

  private async processSymbolInformation(token: EmbeddedLanguageToken, node: any): Promise<void> {
    // Process symbol definitions and references
    if (token.type === 'IDENTIFIER') {
      // Check if this is a symbol definition or reference
      const currentContext = this.getCurrentContext();

      // For simplicity, assume all identifiers are potential cross-language references
      if (this.isPotentialCrossLanguageReference(token, currentContext)) {
        const reference = this.createCrossLanguageReference(token, currentContext);
        this.crossLanguageReferences.set(reference.id, reference);
        this.performanceMetrics.crossLanguageReferenceCount++;
      }
    }
  }

  private isPotentialCrossLanguageReference(
    token: EmbeddedLanguageToken,
    context: EmbeddedLanguageContext,
  ): boolean {
    // Determine if token might be a cross-language reference
    return context.language !== this.primaryGrammar.getName() ||
               context.contextSwitchStack.length > 0;
  }

  private createCrossLanguageReference(
    token: EmbeddedLanguageToken,
    context: EmbeddedLanguageContext,
  ): CrossLanguageReference {
    return {
      id: `ref_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      sourceLanguage: context.language,
      targetLanguage: context.parentLanguage || this.primaryGrammar.getName(),
      sourceSymbol: token.value,
      targetSymbol: token.value,
      sourcePosition: token.position,
      resolved: false,
    };
  }

  private createSymbolTableHierarchy(
    parentContext: EmbeddedLanguageContext,
    language: string,
  ): SymbolTableLevel[] {
    // Create symbol table hierarchy for embedded context
    return [...parentContext.symbolTableHierarchy];
  }

  private async resolveReference(reference: CrossLanguageReference): Promise<boolean> {
    // Resolve cross-language reference
    const targetSymbol = await this.findSymbolInLanguage(
      reference.targetSymbol,
      reference.targetLanguage,
    );
    return targetSymbol !== null;
  }

  private async findSymbolInLanguage(symbolName: string, language: string): Promise<SymbolInfo | null> {
    // Find symbol in specific language context
    for (const context of this.contextStack) {
      if (context.language === language && context.symbols.has(symbolName)) {
        return context.symbols.get(symbolName) || null;
      }
    }
    return null;
  }

  private scheduleValidation(reference: CrossLanguageReference): void {
    // Schedule validation for resolved reference
    const validationRules = this.primaryGrammar.getValidationRules();

    for (const [_ruleName, rule] of validationRules) {
      if (this.ruleAppliesTo(rule, reference)) {
        this.validationQueue.push({
          rule: rule,
          reference: reference,
          priority: this.getValidationPriority(rule),
          scheduled: true,
        });
      }
    }
  }

  private ruleAppliesTo(rule: ValidationRule, reference: CrossLanguageReference): boolean {
    return rule.sourceLanguage === reference.sourceLanguage &&
               rule.targetLanguage === reference.targetLanguage;
  }

  private getValidationPriority(rule: ValidationRule): number {
    switch (rule.severity) {
      case 'error': return 1;
      case 'warning': return 2;
      case 'info': return 3;
      default: return 4;
    }
  }

  private evaluateValidationRule(
    rule: ValidationRule,
    reference: CrossLanguageReference,
    targetSymbol: SymbolInfo,
  ): boolean {
    // Evaluate validation rule against reference and target symbol
    // Simplified implementation
    return true;
  }

  private generateValidationSuggestions(
    rule: ValidationRule,
    reference: CrossLanguageReference,
  ): string[] {
    return [
      `Check ${rule.targetLanguage} for symbol '${reference.targetSymbol}'`,
      'Verify symbol visibility and scope',
      'Ensure proper import/export declarations',
    ];
  }

  private findRelatedReferences(reference: CrossLanguageReference): string[] {
    const related: string[] = [];

    for (const [refId, ref] of this.crossLanguageReferences) {
      if (ref.id !== reference.id &&
                (ref.targetSymbol === reference.targetSymbol ||
                 ref.sourceSymbol === reference.sourceSymbol)) {
        related.push(refId);
      }
    }

    return related;
  }

  private extractContextSwitches(): ContextSwitchFrame[] {
    const switches: ContextSwitchFrame[] = [];

    for (const context of this.contextStack) {
      switches.push(...context.contextSwitchStack);
    }

    return switches;
  }

  private initializeMetrics(): EmbeddedLanguagePerformanceMetrics {
    return {
      totalParseTime: 0,
      contextSwitchTime: 0,
      validationTime: 0,
      symbolResolutionTime: 0,
      contextSwitchCount: 0,
      crossLanguageReferenceCount: 0,
      validationRuleCount: 0,
      memoryUsage: 0,
    };
  }
}

// Supporting interfaces and types

export interface EmbeddedLanguageToken {
    type: string;
    value: string;
    position: ParsePosition;
    language: string;
    contextSwitchInfo?: ContextSwitchInfo;
}

export interface ContextSwitchInfo {
    trigger: string;
    targetLanguage: string;
    endTrigger: string;
    startPosition: number;
    endPosition: number;
}

export interface ContextSwitchPattern {
    trigger: string;
    targetLanguage: string;
    endTrigger: string;
    nestingAllowed: boolean;
    priority: number;
}

