/**
 * Manages precedence and associativity rules with inheritance support.
 */
export class PrecedenceManager {
  private precedenceRegistry: Map<string, PrecedenceRule>;
  private associativityRegistry: Map<string, AssociativityRule>;
  private inheritanceResolver: InheritanceResolver;
  private grammarContainer: GrammarContainer;
  private precedenceCache: Map<string, Map<string, PrecedenceRule>>;
  private associativityCache: Map<string, Map<string, AssociativityRule>>;

  /**
   * Creates a new PrecedenceManager instance.
   * @param grammarContainer The grammar container
   * @param inheritanceResolver The inheritance resolver
   */
  constructor(grammarContainer: GrammarContainer, inheritanceResolver: InheritanceResolver) {
    this.precedenceRegistry = new Map<string, PrecedenceRule>();
    this.associativityRegistry = new Map<string, AssociativityRule>();
    this.inheritanceResolver = inheritanceResolver;
    this.grammarContainer = grammarContainer;
    this.precedenceCache = new Map<string, Map<string, PrecedenceRule>>();
    this.associativityCache = new Map<string, Map<string, AssociativityRule>>();
  }

  // ============================================================================
  // PRECEDENCE RULE MANAGEMENT
  // ============================================================================

  /**
   * Registers a precedence rule.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @param rule The precedence rule
   */
  public registerPrecedenceRule(grammarName: string, operatorName: string, rule: PrecedenceRule): void {
    const key = this.createPrecedenceKey(grammarName, operatorName);
    this.precedenceRegistry.set(key, rule);

    // Clear cache for affected grammars
    this.clearPrecedenceCacheForGrammar(grammarName);
  }

  /**
   * Gets a precedence rule with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The resolved precedence rule or null if not found
   */
  public getPrecedenceRule(grammarName: string, operatorName: string): PrecedenceRule | null {
    // Check cache first
    const grammarCache = this.precedenceCache.get(grammarName);
    if (grammarCache && grammarCache.has(operatorName)) {
      return grammarCache.get(operatorName) || null;
    }

    // Resolve rule with inheritance
    const resolvedRule = this.resolvePrecedenceRule(grammarName, operatorName);

    // Cache the result
    if (!this.precedenceCache.has(grammarName)) {
      this.precedenceCache.set(grammarName, new Map<string, PrecedenceRule>());
    }
    this.precedenceCache.get(grammarName)!.set(operatorName, resolvedRule);

    return resolvedRule;
  }

  /**
   * Resolves a precedence rule using inheritance hierarchy.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The resolved precedence rule or null if not found
   */
  private resolvePrecedenceRule(grammarName: string, operatorName: string): PrecedenceRule | null {
    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Look for rule in reverse order (derived to base)
    for (let i = hierarchy.length - 1; i >= 0; i--) {
      const currentGrammar = hierarchy[i];
      const key = this.createPrecedenceKey(currentGrammar, operatorName);
      const rule = this.precedenceRegistry.get(key);

      if (rule) {
        return rule;
      }
    }

    return null;
  }

  /**
   * Gets all precedence rules for a grammar with inheritance.
   * @param grammarName The name of the grammar
   * @returns Map of operator names to precedence rules
   */
  public getAllPrecedenceRules(grammarName: string): Map<string, PrecedenceRule> {
    const result = new Map<string, PrecedenceRule>();

    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Collect rules from base to derived (derived rules override base rules)
    for (const currentGrammar of hierarchy) {
      const grammarRules = this.getDirectPrecedenceRules(currentGrammar);
      for (const [operatorName, rule] of grammarRules) {
        result.set(operatorName, rule);
      }
    }

    return result;
  }

  /**
   * Gets direct precedence rules for a grammar (without inheritance).
   * @param grammarName The name of the grammar
   * @returns Map of operator names to precedence rules
   */
  public getDirectPrecedenceRules(grammarName: string): Map<string, PrecedenceRule> {
    const result = new Map<string, PrecedenceRule>();

    for (const [key, rule] of this.precedenceRegistry) {
      const [keyGrammar, operatorName] = this.parsePrecedenceKey(key);
      if (keyGrammar === grammarName) {
        result.set(operatorName, rule);
      }
    }

    return result;
  }

  // ============================================================================
  // ASSOCIATIVITY RULE MANAGEMENT
  // ============================================================================

  /**
   * Registers an associativity rule.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @param rule The associativity rule
   */
  public registerAssociativityRule(grammarName: string, operatorName: string, rule: AssociativityRule): void {
    const key = this.createAssociativityKey(grammarName, operatorName);
    this.associativityRegistry.set(key, rule);

    // Clear cache for affected grammars
    this.clearAssociativityCacheForGrammar(grammarName);
  }

  /**
   * Gets an associativity rule with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The resolved associativity rule or null if not found
   */
  public getAssociativityRule(grammarName: string, operatorName: string): AssociativityRule | null {
    // Check cache first
    const grammarCache = this.associativityCache.get(grammarName);
    if (grammarCache && grammarCache.has(operatorName)) {
      return grammarCache.get(operatorName) || null;
    }

    // Resolve rule with inheritance
    const resolvedRule = this.resolveAssociativityRule(grammarName, operatorName);

    // Cache the result
    if (!this.associativityCache.has(grammarName)) {
      this.associativityCache.set(grammarName, new Map<string, AssociativityRule>());
    }
    this.associativityCache.get(grammarName)!.set(operatorName, resolvedRule);

    return resolvedRule;
  }

  /**
   * Resolves an associativity rule using inheritance hierarchy.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The resolved associativity rule or null if not found
   */
  private resolveAssociativityRule(grammarName: string, operatorName: string): AssociativityRule | null {
    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Look for rule in reverse order (derived to base)
    for (let i = hierarchy.length - 1; i >= 0; i--) {
      const currentGrammar = hierarchy[i];
      const key = this.createAssociativityKey(currentGrammar, operatorName);
      const rule = this.associativityRegistry.get(key);

      if (rule) {
        return rule;
      }
    }

    return null;
  }

  /**
   * Gets all associativity rules for a grammar with inheritance.
   * @param grammarName The name of the grammar
   * @returns Map of operator names to associativity rules
   */
  public getAllAssociativityRules(grammarName: string): Map<string, AssociativityRule> {
    const result = new Map<string, AssociativityRule>();

    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Collect rules from base to derived (derived rules override base rules)
    for (const currentGrammar of hierarchy) {
      const grammarRules = this.getDirectAssociativityRules(currentGrammar);
      for (const [operatorName, rule] of grammarRules) {
        result.set(operatorName, rule);
      }
    }

    return result;
  }

  /**
   * Gets direct associativity rules for a grammar (without inheritance).
   * @param grammarName The name of the grammar
   * @returns Map of operator names to associativity rules
   */
  public getDirectAssociativityRules(grammarName: string): Map<string, AssociativityRule> {
    const result = new Map<string, AssociativityRule>();

    for (const [key, rule] of this.associativityRegistry) {
      const [keyGrammar, operatorName] = this.parseAssociativityKey(key);
      if (keyGrammar === grammarName) {
        result.set(operatorName, rule);
      }
    }

    return result;
  }

  // ============================================================================
  // PRECEDENCE COMPARISON AND RESOLUTION
  // ============================================================================

  /**
   * Compares the precedence of two operators.
   * @param grammarName The name of the grammar
   * @param operator1 The first operator
   * @param operator2 The second operator
   * @returns Comparison result (-1, 0, 1) or null if comparison not possible
   */
  public comparePrecedence(grammarName: string, operator1: string, operator2: string): number | null {
    const rule1 = this.getPrecedenceRule(grammarName, operator1);
    const rule2 = this.getPrecedenceRule(grammarName, operator2);

    if (!rule1 || !rule2) {
      return null;
    }

    const level1 = rule1.getLevel();
    const level2 = rule2.getLevel();

    if (level1 < level2) {
      return -1;
    }
    if (level1 > level2) {
      return 1;
    }
    return 0;
  }

  /**
   * Resolves operator precedence conflicts.
   * @param grammarName The name of the grammar
   * @param operators Array of operators in conflict
   * @returns Resolved operator order
   */
  public resolvePrecedenceConflicts(grammarName: string, operators: string[]): string[] {
    const operatorRules = operators.map(op => ({
      operator: op,
      rule: this.getPrecedenceRule(grammarName, op),
    })).filter(item => item.rule !== null);

    // Sort by precedence level (higher level = higher precedence)
    operatorRules.sort((a, b) => {
      const levelA = a.rule!.getLevel();
      const levelB = b.rule!.getLevel();
      return levelB - levelA;
    });

    return operatorRules.map(item => item.operator);
  }

  /**
   * Gets the effective precedence table for a grammar.
   * @param grammarName The name of the grammar
   * @returns Precedence table with operators grouped by level
   */
  public getPrecedenceTable(grammarName: string): Map<number, OperatorGroup> {
    const allRules = this.getAllPrecedenceRules(grammarName);
    const allAssocRules = this.getAllAssociativityRules(grammarName);
    const table = new Map<number, OperatorGroup>();

    for (const [operatorName, rule] of allRules) {
      const level = rule.getLevel();
      const assocRule = allAssocRules.get(operatorName);
      const associativity = assocRule ? assocRule.getAssociativity() : Associativity.Left;

      if (!table.has(level)) {
        table.set(level, new OperatorGroup(level, associativity));
      }

      table.get(level)!.addOperator(operatorName, rule);
    }

    return table;
  }

  // ============================================================================
  // UTILITY METHODS
  // ============================================================================

  /**
   * Creates a precedence key from grammar name and operator name.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The precedence key
   */
  private createPrecedenceKey(grammarName: string, operatorName: string): string {
    return `${grammarName}::prec::${operatorName}`;
  }

  /**
   * Parses a precedence key into grammar name and operator name.
   * @param key The precedence key
   * @returns Array containing grammar name and operator name
   */
  private parsePrecedenceKey(key: string): [string, string] {
    const parts = key.split('::prec::');
    return [parts[0], parts[1]];
  }

  /**
   * Creates an associativity key from grammar name and operator name.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The associativity key
   */
  private createAssociativityKey(grammarName: string, operatorName: string): string {
    return `${grammarName}::assoc::${operatorName}`;
  }

  /**
   * Parses an associativity key into grammar name and operator name.
   * @param key The associativity key
   * @returns Array containing grammar name and operator name
   */
  private parseAssociativityKey(key: string): [string, string] {
    const parts = key.split('::assoc::');
    return [parts[0], parts[1]];
  }

  /**
   * Clears the precedence cache for a grammar and its dependents.
   * @param grammarName The name of the grammar
   */
  private clearPrecedenceCacheForGrammar(grammarName: string): void {
    // Clear cache for this grammar
    this.precedenceCache.delete(grammarName);

    // Clear cache for all dependent grammars
    const dependents = this.grammarContainer.getAllDependentGrammars(grammarName);
    for (const dependent of dependents) {
      this.precedenceCache.delete(dependent);
    }
  }

  /**
   * Clears the associativity cache for a grammar and its dependents.
   * @param grammarName The name of the grammar
   */
  private clearAssociativityCacheForGrammar(grammarName: string): void {
    // Clear cache for this grammar
    this.associativityCache.delete(grammarName);

    // Clear cache for all dependent grammars
    const dependents = this.grammarContainer.getAllDependentGrammars(grammarName);
    for (const dependent of dependents) {
      this.associativityCache.delete(dependent);
    }
  }

  /**
   * Clears all caches.
   */
  public clearCache(): void {
    this.precedenceCache.clear();
    this.associativityCache.clear();
  }

  /**
   * Removes all rules for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of rules removed
   */
  public removeAllRules(grammarName: string): number {
    let removedCount = 0;

    // Remove precedence rules
    for (const key of this.precedenceRegistry.keys()) {
      const [keyGrammar] = this.parsePrecedenceKey(key);
      if (keyGrammar === grammarName) {
        this.precedenceRegistry.delete(key);
        removedCount++;
      }
    }

    // Remove associativity rules
    for (const key of this.associativityRegistry.keys()) {
      const [keyGrammar] = this.parseAssociativityKey(key);
      if (keyGrammar === grammarName) {
        this.associativityRegistry.delete(key);
        removedCount++;
      }
    }

    if (removedCount > 0) {
      this.clearPrecedenceCacheForGrammar(grammarName);
      this.clearAssociativityCacheForGrammar(grammarName);
    }

    return removedCount;
  }

  /**
   * Gets statistics about the precedence manager.
   */
  public getStatistics(): PrecedenceStatistics {
    const totalPrecedenceRules = this.precedenceRegistry.size;
    const totalAssociativityRules = this.associativityRegistry.size;

    const precedenceCacheSize = Array.from(this.precedenceCache.values())
      .reduce((total, cache) => total + cache.size, 0);
    const associativityCacheSize = Array.from(this.associativityCache.values())
      .reduce((total, cache) => total + cache.size, 0);

    return new PrecedenceStatistics(
      totalPrecedenceRules,
      totalAssociativityRules,
      precedenceCacheSize,
      associativityCacheSize,
      this.precedenceCache.size,
      this.associativityCache.size,
    );
  }
}

/**
 * Represents a precedence rule for an operator.
 */
export class PrecedenceRule {
  private operatorName: string;
  private level: number;
  private grammarName: string;
  private description: string;
  private metadata: Map<string, any>;

  /**
   * Creates a new PrecedenceRule instance.
   * @param operatorName The name of the operator
   * @param level The precedence level (higher = higher precedence)
   * @param grammarName The name of the grammar
   */
  constructor(operatorName: string, level: number, grammarName: string) {
    this.operatorName = operatorName;
    this.level = level;
    this.grammarName = grammarName;
    this.description = '';
    this.metadata = new Map<string, any>();
  }

  /**
   * Gets the operator name.
   */
  public getOperatorName(): string {
    return this.operatorName;
  }

  /**
   * Gets the precedence level.
   */
  public getLevel(): number {
    return this.level;
  }

  /**
   * Sets the precedence level.
   * @param level The precedence level
   */
  public setLevel(level: number): void {
    this.level = level;
  }

  /**
   * Gets the grammar name.
   */
  public getGrammarName(): string {
    return this.grammarName;
  }

  /**
   * Gets the description.
   */
  public getDescription(): string {
    return this.description;
  }

  /**
   * Sets the description.
   * @param description The description
   */
  public setDescription(description: string): void {
    this.description = description;
  }

  /**
   * Gets metadata value.
   * @param key The metadata key
   */
  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  /**
   * Sets metadata value.
   * @param key The metadata key
   * @param value The metadata value
   */
  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }

  /**
   * Creates a copy of this rule.
   * @returns A new precedence rule
   */
  public copy(): PrecedenceRule {
    const newRule = new PrecedenceRule(this.operatorName, this.level, this.grammarName);
    newRule.description = this.description;
    newRule.metadata = new Map(this.metadata);
    return newRule;
  }
}

/**
 * Represents an associativity rule for an operator.
 */
export class AssociativityRule {
  private operatorName: string;
  private associativity: Associativity;
  private grammarName: string;
  private description: string;
  private metadata: Map<string, any>;

  /**
   * Creates a new AssociativityRule instance.
   * @param operatorName The name of the operator
   * @param associativity The associativity
   * @param grammarName The name of the grammar
   */
  constructor(operatorName: string, associativity: Associativity, grammarName: string) {
    this.operatorName = operatorName;
    this.associativity = associativity;
    this.grammarName = grammarName;
    this.description = '';
    this.metadata = new Map<string, any>();
  }

  /**
   * Gets the operator name.
   */
  public getOperatorName(): string {
    return this.operatorName;
  }

  /**
   * Gets the associativity.
   */
  public getAssociativity(): Associativity {
    return this.associativity;
  }

  /**
   * Sets the associativity.
   * @param associativity The associativity
   */
  public setAssociativity(associativity: Associativity): void {
    this.associativity = associativity;
  }

  /**
   * Gets the grammar name.
   */
  public getGrammarName(): string {
    return this.grammarName;
  }

  /**
   * Gets the description.
   */
  public getDescription(): string {
    return this.description;
  }

  /**
   * Sets the description.
   * @param description The description
   */
  public setDescription(description: string): void {
    this.description = description;
  }

  /**
   * Gets metadata value.
   * @param key The metadata key
   */
  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  /**
   * Sets metadata value.
   * @param key The metadata key
   * @param value The metadata value
   */
  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }

  /**
   * Creates a copy of this rule.
   * @returns A new associativity rule
   */
  public copy(): AssociativityRule {
    const newRule = new AssociativityRule(this.operatorName, this.associativity, this.grammarName);
    newRule.description = this.description;
    newRule.metadata = new Map(this.metadata);
    return newRule;
  }
}

/**
 * Enum representing operator associativity.
 */
export enum Associativity {
  Left = 'left',
  Right = 'right',
  None = 'none'
}

/**
 * Represents a group of operators at the same precedence level.
 */
export class OperatorGroup {
  private level: number;
  private associativity: Associativity;
  private operators: Map<string, PrecedenceRule>;

  /**
   * Creates a new OperatorGroup instance.
   * @param level The precedence level
   * @param associativity The associativity for this group
   */
  constructor(level: number, associativity: Associativity) {
    this.level = level;
    this.associativity = associativity;
    this.operators = new Map<string, PrecedenceRule>();
  }

  /**
   * Gets the precedence level.
   */
  public getLevel(): number {
    return this.level;
  }

  /**
   * Gets the associativity.
   */
  public getAssociativity(): Associativity {
    return this.associativity;
  }

  /**
   * Adds an operator to the group.
   * @param operatorName The operator name
   * @param rule The precedence rule
   */
  public addOperator(operatorName: string, rule: PrecedenceRule): void {
    this.operators.set(operatorName, rule);
  }

  /**
   * Gets all operators in the group.
   */
  public getOperators(): Map<string, PrecedenceRule> {
    return new Map(this.operators);
  }

  /**
   * Gets the operator names.
   */
  public getOperatorNames(): string[] {
    return Array.from(this.operators.keys());
  }

  /**
   * Checks if the group contains an operator.
   * @param operatorName The operator name
   */
  public hasOperator(operatorName: string): boolean {
    return this.operators.has(operatorName);
  }
}

/**
 * Statistics about precedence and associativity rules.
 */
export class PrecedenceStatistics {
  private totalPrecedenceRules: number;
  private totalAssociativityRules: number;
  private precedenceCacheSize: number;
  private associativityCacheSize: number;
  private precedenceCacheGrammarCount: number;
  private associativityCacheGrammarCount: number;

  constructor(
    totalPrecedenceRules: number,
    totalAssociativityRules: number,
    precedenceCacheSize: number,
    associativityCacheSize: number,
    precedenceCacheGrammarCount: number,
    associativityCacheGrammarCount: number,
  ) {
    this.totalPrecedenceRules = totalPrecedenceRules;
    this.totalAssociativityRules = totalAssociativityRules;
    this.precedenceCacheSize = precedenceCacheSize;
    this.associativityCacheSize = associativityCacheSize;
    this.precedenceCacheGrammarCount = precedenceCacheGrammarCount;
    this.associativityCacheGrammarCount = associativityCacheGrammarCount;
  }

  public getTotalPrecedenceRules(): number {
    return this.totalPrecedenceRules;
  }

  public getTotalAssociativityRules(): number {
    return this.totalAssociativityRules;
  }

  public getPrecedenceCacheSize(): number {
    return this.precedenceCacheSize;
  }

  public getAssociativityCacheSize(): number {
    return this.associativityCacheSize;
  }

  public toString(): string {
    return `Precedence Statistics:
  Precedence Rules: ${this.totalPrecedenceRules}
  Associativity Rules: ${this.totalAssociativityRules}
  Precedence Cache: ${this.precedenceCacheSize} rules in ${this.precedenceCacheGrammarCount} grammars
  Associativity Cache: ${this.associativityCacheSize} rules in ${this.associativityCacheGrammarCount} grammars`;
  }
}

// Import required classes and interfaces
import { InheritanceResolver } from './InheritanceResolver';
import { GrammarContainer } from './GrammarContainer';

