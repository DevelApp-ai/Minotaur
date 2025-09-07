/**
 * Handles grammar inheritance resolution and merging.
 */
export class InheritanceResolver {
  private grammarContainer: GrammarContainer;
  private resolvedGrammars: Map<string, Grammar>;
  private resolutionStack: string[];

  /**
   * Creates a new InheritanceResolver instance.
   * @param grammarContainer The grammar container
   */
  constructor(grammarContainer: GrammarContainer) {
    this.grammarContainer = grammarContainer;
    this.resolvedGrammars = new Map<string, Grammar>();
    this.resolutionStack = [];
  }

  /**
   * Resolves inheritance for a grammar.
   * @param grammarName The name of the grammar to resolve
   * @returns The resolved grammar with inherited properties
   */
  public resolveInheritance(grammarName: string): Grammar {
    // Check if already resolved
    if (this.resolvedGrammars.has(grammarName)) {
      return this.resolvedGrammars.get(grammarName)!;
    }

    // Check for circular dependencies
    if (this.resolutionStack.includes(grammarName)) {
      throw new GrammarError(`Circular inheritance dependency detected: ${this.resolutionStack.join(' -> ')} -> ${grammarName}`);
    }

    // Get the grammar
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      throw new GrammarError(`Grammar not found: ${grammarName}`);
    }

    // If no inheritance, return as-is
    if (grammar.getBaseGrammars().length === 0) {
      this.resolvedGrammars.set(grammarName, grammar);
      return grammar;
    }

    // Add to resolution stack
    this.resolutionStack.push(grammarName);

    try {
      // Resolve base grammars first
      const baseGrammars: Grammar[] = [];
      for (const baseGrammarName of grammar.getBaseGrammars()) {
        const baseGrammar = this.resolveInheritance(baseGrammarName);
        baseGrammars.push(baseGrammar);
      }

      // Create resolved grammar by merging with base grammars
      const resolvedGrammar = this.mergeGrammars(grammar, baseGrammars);

      // Cache the resolved grammar
      this.resolvedGrammars.set(grammarName, resolvedGrammar);

      return resolvedGrammar;
    } finally {
      // Remove from resolution stack
      this.resolutionStack.pop();
    }
  }

  /**
   * Merges a grammar with its base grammars.
   * @param derivedGrammar The derived grammar
   * @param baseGrammars The base grammars to inherit from
   * @returns The merged grammar
   */
  private mergeGrammars(derivedGrammar: Grammar, baseGrammars: Grammar[]): Grammar {
    // Create a new grammar for the merged result
    const mergedGrammar = new Grammar(derivedGrammar.getName());

    // Copy basic properties from derived grammar
    mergedGrammar.setTokenSplitterType(derivedGrammar.getTokenSplitterType());
    mergedGrammar.setRegexTokenSplitter(derivedGrammar.getRegexTokenSplitter());
    mergedGrammar.setInheritable(derivedGrammar.isInheritable());
    mergedGrammar.setFormatType(derivedGrammar.getFormatType());
    mergedGrammar.setImportSemantics(derivedGrammar.getImportSemantics());
    mergedGrammar.setCoordinateTokens(derivedGrammar.getCoordinateTokens());

    // Merge precedence rules
    this.mergePrecedenceRules(mergedGrammar, baseGrammars, derivedGrammar);

    // Merge error recovery strategies
    this.mergeErrorRecoveryStrategies(mergedGrammar, baseGrammars, derivedGrammar);

    // Merge semantic action templates
    this.mergeSemanticActionTemplates(mergedGrammar, baseGrammars, derivedGrammar);

    // Merge productions
    this.mergeProductions(mergedGrammar, baseGrammars, derivedGrammar);

    // Merge terminals
    this.mergeTerminals(mergedGrammar, baseGrammars, derivedGrammar);

    return mergedGrammar;
  }

  /**
   * Merges precedence rules from base grammars and derived grammar.
   */
  private mergePrecedenceRules(mergedGrammar: Grammar, baseGrammars: Grammar[], derivedGrammar: Grammar): void {
    const precedenceMap = new Map<number, PrecedenceRule>();

    // Add precedence rules from base grammars (in order)
    for (const baseGrammar of baseGrammars) {
      for (const rule of baseGrammar.getPrecedenceRules()) {
        precedenceMap.set(rule.getLevel(), rule);
      }
    }

    // Override with derived grammar precedence rules
    for (const rule of derivedGrammar.getPrecedenceRules()) {
      precedenceMap.set(rule.getLevel(), rule);
    }

    // Set merged precedence rules
    const sortedRules = Array.from(precedenceMap.values()).sort((a, b) => a.getLevel() - b.getLevel());
    mergedGrammar.setPrecedenceRules(sortedRules);
  }

  /**
   * Merges error recovery strategies from base grammars and derived grammar.
   */
  private mergeErrorRecoveryStrategies(mergedGrammar: Grammar, baseGrammars: Grammar[], derivedGrammar: Grammar): void {
    const strategy = new ErrorRecoveryStrategy();

    // Merge strategies from base grammars
    for (const baseGrammar of baseGrammars) {
      const baseStrategy = baseGrammar.getErrorRecoveryStrategy();

      // Merge sync tokens
      for (const token of baseStrategy.getSyncTokens()) {
        strategy.addSyncToken(token);
      }

      // Use the most recent strategy type
      strategy.setStrategy(baseStrategy.getStrategy());
      strategy.setReportingLevel(baseStrategy.getReportingLevel());
    }

    // Override with derived grammar strategy
    const derivedStrategy = derivedGrammar.getErrorRecoveryStrategy();
    if (derivedStrategy.getStrategy() !== 'automatic') {
      strategy.setStrategy(derivedStrategy.getStrategy());
    }
    if (derivedStrategy.getReportingLevel() !== 'basic') {
      strategy.setReportingLevel(derivedStrategy.getReportingLevel());
    }

    // Add derived sync tokens
    for (const token of derivedStrategy.getSyncTokens()) {
      strategy.addSyncToken(token);
    }

    mergedGrammar.setErrorRecoveryStrategy(strategy);
  }

  /**
   * Merges semantic action templates from base grammars and derived grammar.
   */
  private mergeSemanticActionTemplates(mergedGrammar: Grammar, baseGrammars: Grammar[], derivedGrammar: Grammar): void {
    // Add templates from base grammars
    for (const baseGrammar of baseGrammars) {
      for (const [name, template] of baseGrammar.getSemanticActionTemplates()) {
        mergedGrammar.addSemanticActionTemplate(name, template);
      }
    }

    // Override with derived grammar templates
    for (const [name, template] of derivedGrammar.getSemanticActionTemplates()) {
      mergedGrammar.addSemanticActionTemplate(name, template);
    }
  }

  /**
   * Merges productions from base grammars and derived grammar.
   */
  private mergeProductions(mergedGrammar: Grammar, baseGrammars: Grammar[], derivedGrammar: Grammar): void {
    const productionMap = new Map<string, Production>();

    // Add productions from base grammars
    for (const baseGrammar of baseGrammars) {
      for (const production of baseGrammar.getProductions()) {
        productionMap.set(production.getName(), production);
      }
    }

    // Override with derived grammar productions
    for (const production of derivedGrammar.getProductions()) {
      productionMap.set(production.getName(), production);
    }

    // Add all merged productions
    for (const production of productionMap.values()) {
      mergedGrammar.addProduction(production);
    }
  }

  /**
   * Merges terminals from base grammars and derived grammar.
   */
  private mergeTerminals(mergedGrammar: Grammar, baseGrammars: Grammar[], derivedGrammar: Grammar): void {
    const terminalMap = new Map<string, Terminal>();

    // Add terminals from base grammars
    for (const baseGrammar of baseGrammars) {
      for (const terminal of baseGrammar.getValidStartTerminals()) {
        terminalMap.set(terminal.getName(), terminal);
      }
    }

    // Override with derived grammar terminals
    for (const terminal of derivedGrammar.getValidStartTerminals()) {
      terminalMap.set(terminal.getName(), terminal);
    }

    // Add all merged terminals
    for (const terminal of terminalMap.values()) {
      mergedGrammar.addValidStartTerminal(terminal);
    }
  }

  /**
   * Validates inheritance relationships.
   * @param grammarName The grammar to validate
   */
  public validateInheritance(grammarName: string): InheritanceValidationResult {
    const result = new InheritanceValidationResult();

    try {
      this.validateInheritanceRecursive(grammarName, new Set<string>(), result);
    } catch (error) {
      result.addError(error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error));
    }

    return result;
  }

  /**
   * Recursively validates inheritance relationships.
   */
  // eslint-disable-next-line max-len
  private validateInheritanceRecursive(grammarName: string, visited: Set<string>, result: InheritanceValidationResult): void {
    if (visited.has(grammarName)) {
      result.addError(`Circular inheritance dependency detected involving: ${grammarName}`);
      return;
    }

    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      result.addError(`Grammar not found: ${grammarName}`);
      return;
    }

    visited.add(grammarName);

    // Validate base grammars
    for (const baseGrammarName of grammar.getBaseGrammars()) {
      const baseGrammar = this.grammarContainer.getGrammar(baseGrammarName);
      if (!baseGrammar) {
        result.addError(`Base grammar not found: ${baseGrammarName} (required by ${grammarName})`);
        continue;
      }

      if (!baseGrammar.isInheritable()) {
        result.addWarning(`Base grammar ${baseGrammarName} is not marked as inheritable`);
      }

      // Recursively validate base grammar
      this.validateInheritanceRecursive(baseGrammarName, new Set(visited), result);
    }

    visited.delete(grammarName);
  }

  /**
   * Clears the resolution cache.
   */
  public clearCache(): void {
    this.resolvedGrammars.clear();
  }

  /**
   * Gets the resolution cache size.
   */
  public getCacheSize(): number {
    return this.resolvedGrammars.size;
  }
}

/**
 * Represents the result of inheritance validation.
 */
export class InheritanceValidationResult {
  private errors: string[];
  private warnings: string[];

  constructor() {
    this.errors = [];
    this.warnings = [];
  }

  public addError(error: string): void {
    this.errors.push(error);
  }

  public addWarning(warning: string): void {
    this.warnings.push(warning);
  }

  public getErrors(): string[] {
    return [...this.errors];
  }

  public getWarnings(): string[] {
    return [...this.warnings];
  }

  public hasErrors(): boolean {
    return this.errors.length > 0;
  }

  public hasWarnings(): boolean {
    return this.warnings.length > 0;
  }

  public isValid(): boolean {
    return this.errors.length === 0;
  }

  public toString(): string {
    let result = '';

    if (this.errors.length > 0) {
      result += 'Errors:\n';
      for (const error of this.errors) {
        result += `  - ${error}\n`;
      }
    }

    if (this.warnings.length > 0) {
      result += 'Warnings:\n';
      for (const warning of this.warnings) {
        result += `  - ${warning}\n`;
      }
    }

    if (this.errors.length === 0 && this.warnings.length === 0) {
      result = 'Inheritance validation passed successfully.';
    }

    return result;
  }
}

// Import required classes and interfaces
import { Grammar, PrecedenceRule, ErrorRecoveryStrategy, SemanticActionTemplate } from './Grammar';
import { GrammarContainer, GrammarError } from './GrammarContainer';
import { Production } from './Production';
import { Terminal } from './Terminal';

