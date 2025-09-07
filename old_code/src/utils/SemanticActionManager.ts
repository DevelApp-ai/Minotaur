/**
 * Manages semantic actions and their inheritance relationships.
 */
export class SemanticActionManager {
  private actionRegistry: Map<string, SemanticAction>;
  private inheritanceResolver: InheritanceResolver;
  private grammarContainer: GrammarContainer;
  private actionCache: Map<string, Map<string, SemanticAction>>;

  /**
   * Creates a new SemanticActionManager instance.
   * @param grammarContainer The grammar container
   * @param inheritanceResolver The inheritance resolver
   */
  constructor(grammarContainer: GrammarContainer, inheritanceResolver: InheritanceResolver) {
    this.actionRegistry = new Map<string, SemanticAction>();
    this.inheritanceResolver = inheritanceResolver;
    this.grammarContainer = grammarContainer;
    this.actionCache = new Map<string, Map<string, SemanticAction>>();
  }

  /**
   * Registers a semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param action The semantic action
   */
  public registerAction(grammarName: string, actionName: string, action: SemanticAction): void {
    const key = this.createActionKey(grammarName, actionName);
    this.actionRegistry.set(key, action);

    // Clear cache for affected grammars
    this.clearCacheForGrammar(grammarName);
  }

  /**
   * Gets a semantic action with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns The resolved semantic action or null if not found
   */
  public getAction(grammarName: string, actionName: string): SemanticAction | null {
    // Check cache first
    const grammarCache = this.actionCache.get(grammarName);
    if (grammarCache && grammarCache.has(actionName)) {
      return grammarCache.get(actionName) || null;
    }

    // Resolve action with inheritance
    const resolvedAction = this.resolveAction(grammarName, actionName);

    // Cache the result
    if (!this.actionCache.has(grammarName)) {
      this.actionCache.set(grammarName, new Map<string, SemanticAction>());
    }
    this.actionCache.get(grammarName)!.set(actionName, resolvedAction);

    return resolvedAction;
  }

  /**
   * Resolves a semantic action using inheritance hierarchy.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns The resolved semantic action or null if not found
   */
  private resolveAction(grammarName: string, actionName: string): SemanticAction | null {
    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Look for action in reverse order (derived to base)
    for (let i = hierarchy.length - 1; i >= 0; i--) {
      const currentGrammar = hierarchy[i];
      const key = this.createActionKey(currentGrammar, actionName);
      const action = this.actionRegistry.get(key);

      if (action) {
        return action;
      }
    }

    return null;
  }

  /**
   * Gets all actions for a grammar with inheritance.
   * @param grammarName The name of the grammar
   * @returns Map of action names to semantic actions
   */
  public getAllActions(grammarName: string): Map<string, SemanticAction> {
    const result = new Map<string, SemanticAction>();

    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Collect actions from base to derived (derived actions override base actions)
    for (const currentGrammar of hierarchy) {
      const grammarActions = this.getDirectActions(currentGrammar);
      for (const [actionName, action] of grammarActions) {
        result.set(actionName, action);
      }
    }

    return result;
  }

  /**
   * Gets direct actions for a grammar (without inheritance).
   * @param grammarName The name of the grammar
   * @returns Map of action names to semantic actions
   */
  public getDirectActions(grammarName: string): Map<string, SemanticAction> {
    const result = new Map<string, SemanticAction>();

    for (const [key, action] of this.actionRegistry) {
      const [keyGrammar, actionName] = this.parseActionKey(key);
      if (keyGrammar === grammarName) {
        result.set(actionName, action);
      }
    }

    return result;
  }

  /**
   * Checks if an action exists for a grammar (with inheritance).
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns Whether the action exists
   */
  public hasAction(grammarName: string, actionName: string): boolean {
    return this.getAction(grammarName, actionName) !== null;
  }

  /**
   * Removes an action from a grammar.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns Whether the action was removed
   */
  public removeAction(grammarName: string, actionName: string): boolean {
    const key = this.createActionKey(grammarName, actionName);
    const removed = this.actionRegistry.delete(key);

    if (removed) {
      this.clearCacheForGrammar(grammarName);
    }

    return removed;
  }

  /**
   * Removes all actions for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of actions removed
   */
  public removeAllActions(grammarName: string): number {
    let removedCount = 0;

    for (const key of this.actionRegistry.keys()) {
      const [keyGrammar] = this.parseActionKey(key);
      if (keyGrammar === grammarName) {
        this.actionRegistry.delete(key);
        removedCount++;
      }
    }

    if (removedCount > 0) {
      this.clearCacheForGrammar(grammarName);
    }

    return removedCount;
  }

  /**
   * Creates an action key from grammar name and action name.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns The action key
   */
  private createActionKey(grammarName: string, actionName: string): string {
    return `${grammarName}::${actionName}`;
  }

  /**
   * Parses an action key into grammar name and action name.
   * @param key The action key
   * @returns Array containing grammar name and action name
   */
  private parseActionKey(key: string): [string, string] {
    const parts = key.split('::');
    return [parts[0], parts[1]];
  }

  /**
   * Clears the cache for a grammar and its dependents.
   * @param grammarName The name of the grammar
   */
  private clearCacheForGrammar(grammarName: string): void {
    // Clear cache for this grammar
    this.actionCache.delete(grammarName);

    // Clear cache for all dependent grammars
    const dependents = this.grammarContainer.getAllDependentGrammars(grammarName);
    for (const dependent of dependents) {
      this.actionCache.delete(dependent);
    }
  }

  /**
   * Clears all cached actions.
   */
  public clearCache(): void {
    this.actionCache.clear();
  }

  /**
   * Gets statistics about the semantic action manager.
   */
  public getStatistics(): SemanticActionStatistics {
    const totalActions = this.actionRegistry.size;
    const grammarActionCounts = new Map<string, number>();

    for (const key of this.actionRegistry.keys()) {
      const [grammarName] = this.parseActionKey(key);
      const currentCount = grammarActionCounts.get(grammarName) || 0;
      grammarActionCounts.set(grammarName, currentCount + 1);
    }

    const cacheSize = Array.from(this.actionCache.values())
      .reduce((total, cache) => total + cache.size, 0);

    return new SemanticActionStatistics(
      totalActions,
      grammarActionCounts.size,
      cacheSize,
      this.actionCache.size,
    );
  }
}

/**
 * Represents a semantic action.
 */
export class SemanticAction {
  private name: string;
  private grammarName: string;
  private actionType: SemanticActionType;
  private implementation: SemanticActionImplementation;
  private parameters: SemanticActionParameter[];
  private returnType: string;
  private description: string;
  private metadata: Map<string, any>;

  /**
   * Creates a new SemanticAction instance.
   * @param name The name of the action
   * @param grammarName The name of the grammar
   * @param actionType The type of the action
   * @param implementation The implementation of the action
   */
  constructor(
    name: string,
    grammarName: string,
    actionType: SemanticActionType,
    implementation: SemanticActionImplementation,
  ) {
    this.name = name;
    this.grammarName = grammarName;
    this.actionType = actionType;
    this.implementation = implementation;
    this.parameters = [];
    this.returnType = 'void';
    this.description = '';
    this.metadata = new Map<string, any>();
  }

  /**
   * Gets the name of the action.
   */
  public getName(): string {
    return this.name;
  }

  /**
   * Gets the grammar name.
   */
  public getGrammarName(): string {
    return this.grammarName;
  }

  /**
   * Gets the action type.
   */
  public getActionType(): SemanticActionType {
    return this.actionType;
  }

  /**
   * Gets the implementation.
   */
  public getImplementation(): SemanticActionImplementation {
    return this.implementation;
  }

  /**
   * Sets the implementation.
   * @param implementation The new implementation
   */
  public setImplementation(implementation: SemanticActionImplementation): void {
    this.implementation = implementation;
  }

  /**
   * Gets the parameters.
   */
  public getParameters(): SemanticActionParameter[] {
    return [...this.parameters];
  }

  /**
   * Adds a parameter.
   * @param parameter The parameter to add
   */
  public addParameter(parameter: SemanticActionParameter): void {
    this.parameters.push(parameter);
  }

  /**
   * Gets the return type.
   */
  public getReturnType(): string {
    return this.returnType;
  }

  /**
   * Sets the return type.
   * @param returnType The return type
   */
  public setReturnType(returnType: string): void {
    this.returnType = returnType;
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
   * Executes the semantic action.
   * @param context The execution context
   * @param args The arguments
   * @returns The result of the action
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    return this.implementation.execute(context, args);
  }

  /**
   * Creates a copy of this action with a new implementation.
   * @param newImplementation The new implementation
   * @returns A new semantic action
   */
  public override(newImplementation: SemanticActionImplementation): SemanticAction {
    const newAction = new SemanticAction(
      this.name,
      this.grammarName,
      this.actionType,
      newImplementation,
    );

    newAction.parameters = [...this.parameters];
    newAction.returnType = this.returnType;
    newAction.description = this.description;
    newAction.metadata = new Map(this.metadata);

    return newAction;
  }
}

/**
 * Enum representing semantic action types.
 */
export enum SemanticActionType {
  Callback = 'callback',
  Template = 'template',
  Script = 'script',
  Native = 'native'
}

/**
 * Interface for semantic action implementations.
 */
export interface SemanticActionImplementation {
  execute(context: SemanticActionContext, args: any[]): any;
}

/**
 * Represents a semantic action parameter.
 */
export class SemanticActionParameter {
  private name: string;
  private type: string;
  private optional: boolean;
  private defaultValue: any;
  private description: string;

  constructor(name: string, type: string, optional: boolean = false, defaultValue: any = null, description: string = '') {
    this.name = name;
    this.type = type;
    this.optional = optional;
    this.defaultValue = defaultValue;
    this.description = description;
  }

  public getName(): string {
    return this.name;
  }

  public getType(): string {
    return this.type;
  }

  public isOptional(): boolean {
    return this.optional;
  }

  public getDefaultValue(): any {
    return this.defaultValue;
  }

  public getDescription(): string {
    return this.description;
  }
}

/**
 * Represents the execution context for semantic actions.
 */
export class SemanticActionContext {
  private grammarName: string;
  private productionName: string;
  private matchedTokens: any[];
  private symbolTable: Map<string, any>;
  private parseState: any;
  private metadata: Map<string, any>;

  constructor(grammarName: string, productionName: string) {
    this.grammarName = grammarName;
    this.productionName = productionName;
    this.matchedTokens = [];
    this.symbolTable = new Map<string, any>();
    this.parseState = null;
    this.metadata = new Map<string, any>();
  }

  public getGrammarName(): string {
    return this.grammarName;
  }

  public getProductionName(): string {
    return this.productionName;
  }

  public getMatchedTokens(): any[] {
    return [...this.matchedTokens];
  }

  public setMatchedTokens(tokens: any[]): void {
    this.matchedTokens = [...tokens];
  }

  public getSymbolTable(): Map<string, any> {
    return new Map(this.symbolTable);
  }

  public setSymbol(name: string, value: any): void {
    this.symbolTable.set(name, value);
  }

  public getSymbol(name: string): any {
    return this.symbolTable.get(name);
  }

  public getParseState(): any {
    return this.parseState;
  }

  public setParseState(state: any): void {
    this.parseState = state;
  }

  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }
}

/**
 * Statistics about semantic actions.
 */
export class SemanticActionStatistics {
  private totalActions: number;
  private grammarCount: number;
  private cacheSize: number;
  private cacheGrammarCount: number;

  constructor(totalActions: number, grammarCount: number, cacheSize: number, cacheGrammarCount: number) {
    this.totalActions = totalActions;
    this.grammarCount = grammarCount;
    this.cacheSize = cacheSize;
    this.cacheGrammarCount = cacheGrammarCount;
  }

  public getTotalActions(): number {
    return this.totalActions;
  }

  public getGrammarCount(): number {
    return this.grammarCount;
  }

  public getCacheSize(): number {
    return this.cacheSize;
  }

  public getCacheGrammarCount(): number {
    return this.cacheGrammarCount;
  }

  public toString(): string {
    return `Semantic Action Statistics:
  Total Actions: ${this.totalActions}
  Grammars with Actions: ${this.grammarCount}
  Cached Actions: ${this.cacheSize}
  Grammars in Cache: ${this.cacheGrammarCount}`;
  }
}

// Import required classes and interfaces
import { InheritanceResolver } from './InheritanceResolver';
import { GrammarContainer } from './GrammarContainer';

