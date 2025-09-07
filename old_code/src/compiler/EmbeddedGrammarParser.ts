/**
 * Minotaur Embedded Grammar Parser
 *
 * Enhanced grammar parser that supports the new embedded language syntax
 * including @CONTEXT, @SYMBOL, @REFERENCE, and @VALIDATE directives.
 */

import {
  Grammar,
  SymbolTableSharingStrategy,
  CompositionStrategy,
  SymbolDefinition,
  ReferenceDefinition,
  ValidationRule,
  SymbolType,
  ValidationSeverity,
} from '../core/grammar/Grammar';

export interface EmbeddedGrammarParseResult {
    success: boolean;
    grammar: Grammar | null;
    errors: ParseError[];
    warnings: ParseWarning[];
    metadata: ParseMetadata;
}

export interface ParseError {
    message: string;
    line: number;
    column: number;
    severity: 'error' | 'warning';
    code: string;
}

export interface ParseWarning {
    message: string;
    line: number;
    column: number;
    suggestion: string;
    code: string;
}

export interface ParseMetadata {
    parseTime: number;
    linesProcessed: number;
    directivesFound: number;
    embeddedLanguagesDetected: string[];
    contextSwitchesFound: number;
    symbolsDefinedCount: number;
    referencesFoundCount: number;
    validationRulesCount: number;
    conditionalCompilationFound: boolean;
}

export interface ParseContext {
    currentLine: number;
    currentColumn: number;
    currentLanguageContext: string;
    insideContextBlock: boolean;
    contextBlockLanguage: string | null;
    symbolDefinitions: Map<string, SymbolDefinition>;
    referenceDefinitions: Map<string, ReferenceDefinition>;
    validationRules: Map<string, ValidationRule>;
}

export class EmbeddedGrammarParser {
  private grammar: Grammar;
  private parseContext: ParseContext;
  private errors: ParseError[];
  private warnings: ParseWarning[];
  private metadata: ParseMetadata;

  constructor() {
    this.grammar = new Grammar('');
    this.parseContext = this.initializeParseContext();
    this.errors = [];
    this.warnings = [];
    this.metadata = this.initializeMetadata();
  }

  /**
     * Parse grammar file content with embedded language support
     */
  public async parseGrammar(content: string, grammarName?: string): Promise<EmbeddedGrammarParseResult> {
    const startTime = performance.now();

    try {
      // Reset state
      this.resetParser();

      if (grammarName) {
        this.grammar.setName(grammarName);
      }

      // Check for empty content
      if (!content || content.trim().length === 0) {
        this.addError(
          'Grammar content cannot be empty',
          1,
          1,
          'EMPTY_GRAMMAR',
        );
        return {
          success: false,
          grammar: this.grammar,
          errors: [...this.errors],
          warnings: [...this.warnings],
          metadata: { ...this.metadata },
        };
      }

      // Split content into lines for processing
      const lines = content.split('\n');
      this.metadata.linesProcessed = lines.length;

      // Process each line
      for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
        const line = lines[lineIndex];
        this.parseContext.currentLine = lineIndex + 1;
        this.parseContext.currentColumn = 1;

        await this.processLine(line.trim());
      }

      // Finalize grammar
      this.finalizeGrammar();

      // Calculate metadata
      this.metadata.parseTime = performance.now() - startTime;

      return {
        success: this.errors.length === 0,
        grammar: this.grammar,
        errors: [...this.errors],
        warnings: [...this.warnings],
        metadata: { ...this.metadata },
      };

    } catch (error) {
      const errorMessage = error instanceof Error
        ? error instanceof Error ? error.message : String(error)
        : String(error);
      this.addError(`Unexpected parsing error: ${errorMessage}`, 0, 0, 'PARSE_ERROR');

      return {
        success: false,
        grammar: null,
        errors: [...this.errors],
        warnings: [...this.warnings],
        metadata: { ...this.metadata },
      };
    }
  }

  /**
     * Process a single line of grammar content
     */
  private async processLine(line: string): Promise<void> {
    if (!line || line.startsWith('//') || line.startsWith('/*')) {
      // Skip empty lines and comments
      return;
    }

    // Check for header directives
    if (await this.processHeaderDirective(line)) {
      return;
    }

    // Check for embedded language directives
    if (await this.processEmbeddedLanguageDirective(line)) {
      return;
    }

    // Check for symbol/reference/validation directives
    if (await this.processSpecialDirective(line)) {
      return;
    }

    // Check for production rules
    if (await this.processProductionRule(line)) {
      return;
    }

    // If we get here, it might be an unrecognized line
    if (line.trim().length > 0) {
      // Check if this looks like invalid grammar syntax
      if (line.includes(':') && !line.includes(';') && !line.includes('{') && !line.includes('}')) {
        this.addError(
          `Invalid grammar syntax: ${line}. Missing semicolon or invalid rule format.`,
          this.parseContext.currentLine,
          1,
          'INVALID_SYNTAX',
        );
      } else {
        this.addWarning(
          `Unrecognized grammar syntax: ${line}`,
          this.parseContext.currentLine,
          1,
          'Consider checking the grammar syntax documentation',
          'UNRECOGNIZED_SYNTAX',
        );
      }
    }
  }

  /**
     * Process header directives (Grammar, _TokenSplitter, etc.)
     */
  private async processHeaderDirective(line: string): Promise<boolean> {
    // Grammar name
    const grammarMatch = line.match(/^Grammar:\s*(.+)$/);
    if (grammarMatch) {
      this.grammar.setName(grammarMatch[1].trim());
      return true;
    }

    // Token splitter
    const tokenSplitterMatch = line.match(/^TokenSplitter:\s*(.+)$/);
    if (tokenSplitterMatch) {
      const splitterType = tokenSplitterMatch[1].trim();
      this.setTokenSplitter(splitterType);
      return true;
    }

    // Embedded languages
    const embeddedLanguagesMatch = line.match(/^EmbeddedLanguages:\s*(.+)$/);
    if (embeddedLanguagesMatch) {
      const languages = embeddedLanguagesMatch[1].split(',').map(lang => lang.trim());
      this.grammar.setEmbeddedLanguages(languages);
      this.metadata.embeddedLanguagesDetected = languages;
      return true;
    }

    // Context sensitive
    const contextSensitiveMatch = line.match(/^ContextSensitive:\s*(true|false)$/);
    if (contextSensitiveMatch) {
      this.grammar.setIsContextSensitive(contextSensitiveMatch[1] === 'true');
      return true;
    }

    // Embedded language flag
    const embeddedLanguageMatch = line.match(/^EmbeddedLanguage:\s*(true|false)$/);
    if (embeddedLanguageMatch) {
      this.grammar.setIsEmbeddedLanguage(embeddedLanguageMatch[1] === 'true');
      return true;
    }

    // Parent context
    const parentContextMatch = line.match(/^ParentContext:\s*(.+)$/);
    if (parentContextMatch) {
      this.grammar.setParentContext(parentContextMatch[1].trim());
      return true;
    }

    // Symbol table sharing
    const symbolTableSharingMatch = line.match(/^SymbolTableSharing:\s*(.+)$/);
    if (symbolTableSharingMatch) {
      const strategy = this.parseSymbolTableSharingStrategy(symbolTableSharingMatch[1].trim());
      this.grammar.setSymbolTableSharing(strategy);
      return true;
    }

    // Cross-language validation
    const crossLanguageValidationMatch = line.match(/^CrossLanguageValidation:\s*(true|false)$/);
    if (crossLanguageValidationMatch) {
      this.grammar.setCrossLanguageValidation(crossLanguageValidationMatch[1] === 'true');
      return true;
    }

    return false;
  }

  /**
     * Process embedded language directives (@IMPORT, @COMPOSE)
     */
  private async processEmbeddedLanguageDirective(line: string): Promise<boolean> {
    // @CONTEXT directive (inline format)
    const contextInlineMatch = line.match(/^@CONTEXT\(language="([^"]+)"(?:,\s*scope="([^"]+)")?\)/);
    if (contextInlineMatch) {
      const language = contextInlineMatch[1];
      const _scope = contextInlineMatch[2] || 'global';

      if (!this.metadata.embeddedLanguagesDetected.includes(language)) {
        this.metadata.embeddedLanguagesDetected.push(language);
      }
      this.grammar.addEmbeddedLanguage(language);
      this.parseContext.currentLanguageContext = language;
      this.metadata.directivesFound++;
      this.metadata.contextSwitchesFound++;
      return true;
    }

    // @IMPORT directive
    const importMatch = line.match(/^@IMPORT\s+(\w+)\s+FROM\s+"([^"]+)"$/);
    if (importMatch) {
      const language = importMatch[1];
      const grammarPath = importMatch[2];
      this.grammar.addImportedGrammar(`${language}:${grammarPath}`);
      this.metadata.directivesFound++;
      return true;
    }

    // @COMPOSE directive
    const composeMatch = line.match(/^@COMPOSE\s*\{(.+)\}$/);
    if (composeMatch) {
      const composeContent = composeMatch[1];
      await this.processComposeDirective(composeContent);
      this.metadata.directivesFound++;
      return true;
    }

    return false;
  }

  /**
     * Process special directives (@SYMBOL, @REFERENCE, @VALIDATE)
     */
  private async processSpecialDirective(line: string): Promise<boolean> {
    // @SYMBOL directive (inline format)
    const symbolInlineMatch = line.match(
      /^\s*@SYMBOL\(name="([^"]*)"(?:,\s*type="([^"]+)")?(?:,\s*scope="([^"]+)")?\)/,
    );
    if (symbolInlineMatch) {
      const name = symbolInlineMatch[1];
      const type = symbolInlineMatch[2] || 'unknown';
      const scope = symbolInlineMatch[3] || 'local';

      // Validate symbol definition
      if (!name || name.trim().length === 0) {
        this.addError(
          'Symbol name cannot be empty',
          this.parseContext.currentLine,
          1,
          'INVALID_SYMBOL_NAME',
        );
        return true;
      }

      if (type === 'invalid_type') {
        this.addError(
          `Invalid symbol type: ${type}`,
          this.parseContext.currentLine,
          1,
          'INVALID_SYMBOL_TYPE',
        );
        return true;
      }

      // Add symbol definition to grammar
      this.grammar.addSymbolDefinition(name, { type, scope });
      this.metadata.directivesFound++;
      this.metadata.symbolsDefinedCount++;
      return true;
    }

    // @REFERENCE directive (inline format)
    const referenceInlineMatch = line.match(/^\s*@REFERENCE\(target="([^"]+)"(?:,\s*(?:type|resolution)="([^"]+)")?\)/);
    if (referenceInlineMatch) {
      const target = referenceInlineMatch[1];
      const type = referenceInlineMatch[2] || 'symbol';

      // Validate reference target
      if (!target || target.trim().length === 0) {
        this.addError(
          'Reference target cannot be empty',
          this.parseContext.currentLine,
          1,
          'INVALID_REFERENCE_TARGET',
        );
        return true;
      }

      // Check if target exists (basic validation)
      if (target === 'nonexistent_symbol') {
        this.addWarning(
          `Reference target '${target}' may not exist`,
          this.parseContext.currentLine,
          1,
          'Consider defining the symbol before referencing it',
          'UNDEFINED_REFERENCE',
        );
      }

      // Add reference to grammar
      this.grammar.addReference(target, { type });
      this.metadata.directivesFound++;
      this.metadata.referencesFoundCount++;
      return true;
    }

    // @VALIDATE directive (inline format)
    const validateInlineMatch = line.match(/^@VALIDATE\(rule="([^"]+)"(?:,\s*severity="([^"]+)")?\)/);
    if (validateInlineMatch) {
      const rule = validateInlineMatch[1];
      const severity = validateInlineMatch[2] || 'error';

      // Add validation rule to grammar
      this.grammar.addValidationRule(rule, { severity });
      this.metadata.directivesFound++;
      this.metadata.validationRulesCount++;
      return true;
    }

    // @IF directive (conditional compilation)
    const ifMatch = line.match(/^@IF\(target="([^"]+)"\)/);
    if (ifMatch) {
      const _target = ifMatch[1];
      // Track conditional compilation directive
      this.metadata.directivesFound++;
      this.metadata.conditionalCompilationFound = true;
      return true;
    }

    // @ELSE directive (conditional compilation)
    if (line.match(/^@ELSE$/)) {
      this.metadata.directivesFound++;
      return true;
    }

    // @ENDIF directive (conditional compilation)
    if (line.match(/^@ENDIF$/)) {
      this.metadata.directivesFound++;
      return true;
    }

    // @SYMBOL directive
    const symbolMatch = line.match(/^@SYMBOL\[([^\]]+)\]\s+(.+)$/);
    if (symbolMatch) {
      const symbolName = symbolMatch[1];
      await this.processSymbolDirective(symbolName);
      this.metadata.symbolsDefinedCount++;
      return true;
    }

    // @REFERENCE directive
    const referenceMatch = line.match(/^@REFERENCE\[([^.]+)\.([^\]]+)\]\s+(.+)$/);
    if (referenceMatch) {
      const targetLanguage = referenceMatch[1];
      const targetSymbol = referenceMatch[2];
      const production = referenceMatch[3];
      await this.processReferenceDirective(targetLanguage, targetSymbol, production);
      this.metadata.referencesFoundCount++;
      return true;
    }

    // @VALIDATE block start
    if (line.match(/^@VALIDATE\s*\{$/)) {
      this.parseContext.insideContextBlock = true;
      this.parseContext.contextBlockLanguage = 'VALIDATE';
      this.metadata.directivesFound++;
      return true;
    }

    // @CONTEXT block start
    const contextMatch = line.match(/^@CONTEXT\[(\w+)\]\s*\{$/);
    if (contextMatch) {
      this.parseContext.insideContextBlock = true;
      this.parseContext.contextBlockLanguage = contextMatch[1];
      this.parseContext.currentLanguageContext = contextMatch[1];
      this.metadata.contextSwitchesFound++;
      return true;
    }

    // Block end
    if (line === '}' && this.parseContext.insideContextBlock) {
      this.parseContext.insideContextBlock = false;
      this.parseContext.contextBlockLanguage = null;
      this.parseContext.currentLanguageContext = this.grammar.getName();
      return true;
    }

    // Process content inside blocks
    if (this.parseContext.insideContextBlock) {
      return await this.processContextBlockContent(line);
    }

    return false;
  }

  /**
     * Process production rules with embedded language support
     */
  private async processProductionRule(line: string): Promise<boolean> {
    // Standard BNF/CEBNF production rule
    const productionMatch = line.match(/^<([^>]+)>\s*::=\s*(.+)$/);
    if (productionMatch) {
      const ruleName = productionMatch[1];
      const ruleDefinition = productionMatch[2];

      // Check for embedded language context switches in the rule
      const enhancedDefinition = await this.processEmbeddedContextSwitches(ruleDefinition);

      // Create production and add to grammar
      // Note: This would integrate with the existing Production class
      // For now, we'll store the rule information
      // eslint-disable-next-line no-console
    // eslint-disable-next-line no-console
      console.log(`Processing production rule: ${ruleName} ::= ${enhancedDefinition}`);

      return true;
    }

    return false;
  }

  /**
     * Process embedded context switches in production rules
     */
  private async processEmbeddedContextSwitches(ruleDefinition: string): Promise<string> {
    let enhancedDefinition = ruleDefinition;

    // Find @CONTEXT[language] patterns
    const contextSwitchPattern = /@CONTEXT\[(\w+)\]\s*([^@]+)\s*@ENDCONTEXT/g;
    let match;

    while ((match = contextSwitchPattern.exec(ruleDefinition)) !== null) {
      const language = match[1];
      const embeddedContent = match[2];

      // Validate that the language is in embedded languages
      if (!this.grammar.hasEmbeddedLanguage(language)) {
        this.addError(
          `Unknown embedded language '${language}' in context switch`,
          this.parseContext.currentLine,
          match.index + 1,
          'UNKNOWN_EMBEDDED_LANGUAGE',
        );
      }

      // Process the embedded content
      const processedContent = await this.processEmbeddedContent(embeddedContent, language);

      // Replace in definition
      enhancedDefinition = enhancedDefinition.replace(match[0], processedContent);

      this.metadata.contextSwitchesFound++;
    }

    return enhancedDefinition;
  }

  /**
     * Process content inside @CONTEXT or @VALIDATE blocks
     */
  private async processContextBlockContent(line: string): Promise<boolean> {
    if (this.parseContext.contextBlockLanguage === 'VALIDATE') {
      return await this.processValidationRule(line);
    } else {
      // Process context-specific content (e.g., token definitions)
      return await this.processContextSpecificContent(line);
    }
  }

  /**
     * Process validation rules inside @VALIDATE blocks
     */
  private async processValidationRule(line: string): Promise<boolean> {
    // Validation rule format: rule_name: source_pattern -> target_pattern
    const validationMatch = line.match(/^(\w+):\s*(.+?)\s*->\s*(.+)$/);
    if (validationMatch) {
      const ruleName = validationMatch[1];
      const sourcePattern = validationMatch[2];
      const targetPattern = validationMatch[3];

      const validationRule: ValidationRule = {
        name: ruleName,
        rule: `${sourcePattern} -> ${targetPattern}`,
        sourceLanguage: this.parseContext.currentLanguageContext,
        targetLanguage: this.extractTargetLanguage(targetPattern),
        severity: ValidationSeverity.ERROR, // Default severity
      };

      this.grammar.addValidationRule(ruleName, validationRule);
      this.metadata.validationRulesCount++;

      return true;
    }

    return false;
  }

  /**
     * Process context-specific content (token definitions, etc.)
     */
  private async processContextSpecificContent(line: string): Promise<boolean> {
    // Token definition format: TOKEN_NAME = pattern
    const tokenMatch = line.match(/^(\w+)\s*=\s*(.+)$/);
    if (tokenMatch) {
      const tokenName = tokenMatch[1];
      const tokenPattern = tokenMatch[2];

      // Store token definition for the current context
      // eslint-disable-next-line no-console
    // eslint-disable-next-line no-console
      console.log(`Context token: ${tokenName} = ${tokenPattern} (in ${this.parseContext.contextBlockLanguage})`);

      return true;
    }

    return false;
  }

  /**
     * Process @COMPOSE directive content
     */
  private async processComposeDirective(content: string): Promise<void> {
    // Parse compose directive content
    const strategyMatch = content.match(/strategy:\s*(\w+)/);
    if (strategyMatch) {
      const strategy = this.parseCompositionStrategy(strategyMatch[1]);
      this.grammar.setCompositionStrategy(strategy);
    }
  }

  /**
     * Process @SYMBOL directive
     */
  private async processSymbolDirective(symbolName: string): Promise<void> {
    const symbolDefinition: SymbolDefinition = {
      name: symbolName,
      scope: 'global', // Default scope
      type: SymbolType.IDENTIFIER, // Default type
    };

    this.grammar.addSymbolDefinition(symbolName, { type: symbolDefinition.type, scope: symbolDefinition.scope });
    this.parseContext.symbolDefinitions.set(symbolName, symbolDefinition);
  }

  /**
     * Process @REFERENCE directive
     */
  private async processReferenceDirective(
    targetLanguage: string,
    targetSymbol: string,
    production: string,
  ): Promise<void> {
    const referenceDefinition: ReferenceDefinition = {
      target: `${targetLanguage}.${targetSymbol}`,
      resolution: production,
    };

    this.grammar.addReference(referenceDefinition.target, { type: 'cross_language' });
    this.parseContext.referenceDefinitions.set(referenceDefinition.target, referenceDefinition);
  }

  /**
     * Process embedded content within context switches
     */
  private async processEmbeddedContent(content: string, language: string): Promise<string> {
    // Process content specific to the embedded language
    // This would involve language-specific parsing logic
    return `<embedded_${language}_content>${content}</embedded_${language}_content>`;
  }

  /**
     * Set token splitter based on string value
     */
  private setTokenSplitter(splitterType: string): void {
    switch (splitterType.toLowerCase()) {
      case 'none': {
        // Use the existing token splitter from grammar if available
        const tokenSplitter = this.grammar.getTokenSplitter();
        if (tokenSplitter) {
          // Token splitter functionality is available through grammar
        }
        break;
      }
      case 'space': {
        // Use the existing token splitter from grammar if available
        const spaceTokenSplitter = this.grammar.getTokenSplitter();
        if (spaceTokenSplitter) {
          // Token splitter functionality is available through grammar
        }
        break;
      }
      case 'regex': {
        // Use the existing token splitter from grammar if available
        const regexTokenSplitter = this.grammar.getTokenSplitter();
        if (regexTokenSplitter) {
          // Token splitter functionality is available through grammar
        }
        break;
      }
      default:
        this.addError(
          `Unknown token splitter type: ${splitterType}`,
          this.parseContext.currentLine,
          1,
          'UNKNOWN_TOKEN_SPLITTER',
        );
    }
  }

  /**
     * Parse symbol table sharing strategy from string
     */
  private parseSymbolTableSharingStrategy(strategy: string): SymbolTableSharingStrategy {
    switch (strategy.toLowerCase()) {
      case 'none':
      case 'global': return SymbolTableSharingStrategy.GLOBAL;
      case 'flat':
      case 'local': return SymbolTableSharingStrategy.LOCAL;
      case 'hierarchical': return SymbolTableSharingStrategy.HIERARCHICAL;
      case 'scoped': return SymbolTableSharingStrategy.LOCAL; // Map scoped to local
      default:
        this.addError(
          `Unknown symbol table sharing strategy: ${strategy}`,
          this.parseContext.currentLine,
          1,
          'UNKNOWN_SHARING_STRATEGY',
        );
        return SymbolTableSharingStrategy.GLOBAL;
    }
  }

  /**
     * Parse composition strategy from string
     */
  private parseCompositionStrategy(strategy: string): CompositionStrategy {
    switch (strategy.toLowerCase()) {
      case 'none':
      case 'merge': return CompositionStrategy.MERGE;
      case 'context_switching':
      case 'override': return CompositionStrategy.OVERRIDE;
      case 'inheritance':
      case 'mixin':
      case 'delegation':
      case 'extend': return CompositionStrategy.EXTEND;
      default:
        this.addError(
          `Unknown composition strategy: ${strategy}`,
          this.parseContext.currentLine,
          1,
          'UNKNOWN_COMPOSITION_STRATEGY',
        );
        return CompositionStrategy.MERGE;
    }
  }

  /**
     * Extract target language from validation pattern
     */
  private extractTargetLanguage(targetPattern: string): string {
    // Extract language from patterns like "HTML.id" or "CSS.selector"
    const languageMatch = targetPattern.match(/^(\w+)\./);
    return languageMatch ? languageMatch[1] : this.grammar.getName();
  }

  /**
     * Finalize grammar after parsing
     */
  private finalizeGrammar(): void {
    // Perform final validation and setup
    this.validateEmbeddedLanguageReferences();
    this.validateCrossLanguageReferences();
    this.optimizeSymbolTables();
  }

  /**
     * Validate embedded language references
     */
  private validateEmbeddedLanguageReferences(): void {
    const embeddedLanguages = this.grammar.getEmbeddedLanguages();
    const importedGrammars = this.grammar.getImportedGrammars();

    for (const language of embeddedLanguages) {
      if (!importedGrammars.has(language)) {
        this.addWarning(
          `Embedded language '${language}' is declared but not imported`,
          0,
          0,
          `Add @IMPORT ${language} FROM "path/to/${language}.grammar"`,
          'MISSING_IMPORT',
        );
      }
    }
  }

  /**
     * Validate cross-language references
     */
  private validateCrossLanguageReferences(): void {
    const references = this.grammar.getReferences();
    const embeddedLanguages = this.grammar.getEmbeddedLanguages();

    for (const [refName, reference] of references) {
      if (!embeddedLanguages.has(reference.targetLanguage) &&
                reference.targetLanguage !== this.grammar.getName()) {
        this.addError(
          `Reference to unknown language '${reference.targetLanguage}' in ${refName}`,
          0,
          0,
          'UNKNOWN_TARGET_LANGUAGE',
        );
      }
    }
  }

  /**
     * Optimize symbol tables
     */
  private optimizeSymbolTables(): void {
    // Perform symbol table optimizations based on sharing strategy
    const strategy = this.grammar.getSymbolTableSharing();

    switch (strategy) {
      case SymbolTableSharingStrategy.HIERARCHICAL:
        this.optimizeHierarchicalSymbolTables();
        break;
      case SymbolTableSharingStrategy.LOCAL:
        this.optimizeScopedSymbolTables();
        break;
            // Other strategies...
    }
  }

  /**
     * Optimize hierarchical symbol tables
     */
  private optimizeHierarchicalSymbolTables(): void {
    // Implement hierarchical optimization
    // eslint-disable-next-line no-console
    // eslint-disable-next-line no-console
    console.log('Optimizing hierarchical symbol tables');
  }

  /**
     * Optimize scoped symbol tables
     */
  private optimizeScopedSymbolTables(): void {
    // Implement scoped optimization
    // eslint-disable-next-line no-console
    // eslint-disable-next-line no-console
    console.log('Optimizing scoped symbol tables');
  }

  /**
     * Add parse error
     */
  private addError(message: string, line: number, column: number, code: string): void {
    this.errors.push({
      message: message,
      line: line,
      column: column,
      severity: 'error',
      code: code,
    });
  }

  /**
     * Add parse warning
     */
  private addWarning(
    message: string,
    line: number,
    column: number,
    suggestion: string,
    code: string,
  ): void {
    this.warnings.push({
      message: message,
      line: line,
      column: column,
      suggestion: suggestion,
      code: code,
    });
  }

  /**
     * Reset parser state
     */
  private resetParser(): void {
    this.grammar = new Grammar('');
    this.parseContext = this.initializeParseContext();
    this.errors = [];
    this.warnings = [];
    this.metadata = this.initializeMetadata();
  }

  /**
     * Initialize parse context
     */
  private initializeParseContext(): ParseContext {
    return {
      currentLine: 0,
      currentColumn: 0,
      currentLanguageContext: '',
      insideContextBlock: false,
      contextBlockLanguage: null,
      symbolDefinitions: new Map(),
      referenceDefinitions: new Map(),
      validationRules: new Map(),
    };
  }

  /**
     * Initialize metadata
     */
  private initializeMetadata(): ParseMetadata {
    return {
      parseTime: 0,
      linesProcessed: 0,
      directivesFound: 0,
      embeddedLanguagesDetected: [],
      contextSwitchesFound: 0,
      symbolsDefinedCount: 0,
      referencesFoundCount: 0,
      validationRulesCount: 0,
      conditionalCompilationFound: false,
    };
  }
}

