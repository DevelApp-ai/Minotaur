/**
 * Manages error recovery strategies with inheritance support.
 */
export class ErrorRecoveryManager {
  private recoveryRegistry: Map<string, ErrorRecoveryStrategy>;
  private inheritanceResolver: InheritanceResolver;
  private grammarContainer: GrammarContainer;
  private recoveryCache: Map<string, Map<string, ErrorRecoveryStrategy>>;

  /**
   * Creates a new ErrorRecoveryManager instance.
   * @param grammarContainer The grammar container
   * @param inheritanceResolver The inheritance resolver
   */
  constructor(grammarContainer: GrammarContainer, inheritanceResolver: InheritanceResolver) {
    this.recoveryRegistry = new Map<string, ErrorRecoveryStrategy>();
    this.inheritanceResolver = inheritanceResolver;
    this.grammarContainer = grammarContainer;
    this.recoveryCache = new Map<string, Map<string, ErrorRecoveryStrategy>>();
  }

  /**
   * Registers an error recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param strategy The error recovery strategy
   */
  public registerStrategy(grammarName: string, strategyName: string, strategy: ErrorRecoveryStrategy): void {
    const key = this.createStrategyKey(grammarName, strategyName);
    this.recoveryRegistry.set(key, strategy);

    // Clear cache for affected grammars
    this.clearCacheForGrammar(grammarName);
  }

  /**
   * Gets an error recovery strategy with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @returns The resolved error recovery strategy or null if not found
   */
  public getStrategy(grammarName: string, strategyName: string): ErrorRecoveryStrategy | null {
    // Check cache first
    const grammarCache = this.recoveryCache.get(grammarName);
    if (grammarCache && grammarCache.has(strategyName)) {
      return grammarCache.get(strategyName) || null;
    }

    // Resolve strategy with inheritance
    const resolvedStrategy = this.resolveStrategy(grammarName, strategyName);

    // Cache the result
    if (!this.recoveryCache.has(grammarName)) {
      this.recoveryCache.set(grammarName, new Map<string, ErrorRecoveryStrategy>());
    }
    this.recoveryCache.get(grammarName)!.set(strategyName, resolvedStrategy);

    return resolvedStrategy;
  }

  /**
   * Resolves an error recovery strategy using inheritance hierarchy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @returns The resolved error recovery strategy or null if not found
   */
  private resolveStrategy(grammarName: string, strategyName: string): ErrorRecoveryStrategy | null {
    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Look for strategy in reverse order (derived to base)
    for (let i = hierarchy.length - 1; i >= 0; i--) {
      const currentGrammar = hierarchy[i];
      const key = this.createStrategyKey(currentGrammar, strategyName);
      const strategy = this.recoveryRegistry.get(key);

      if (strategy) {
        return strategy;
      }
    }

    return null;
  }

  /**
   * Gets all error recovery strategies for a grammar with inheritance.
   * @param grammarName The name of the grammar
   * @returns Map of strategy names to error recovery strategies
   */
  public getAllStrategies(grammarName: string): Map<string, ErrorRecoveryStrategy> {
    const result = new Map<string, ErrorRecoveryStrategy>();

    // Get inheritance hierarchy (base to derived)
    const hierarchy = this.grammarContainer.getInheritanceHierarchy(grammarName);

    // Collect strategies from base to derived (derived strategies override base strategies)
    for (const currentGrammar of hierarchy) {
      const grammarStrategies = this.getDirectStrategies(currentGrammar);
      for (const [strategyName, strategy] of grammarStrategies) {
        result.set(strategyName, strategy);
      }
    }

    return result;
  }

  /**
   * Gets direct error recovery strategies for a grammar (without inheritance).
   * @param grammarName The name of the grammar
   * @returns Map of strategy names to error recovery strategies
   */
  public getDirectStrategies(grammarName: string): Map<string, ErrorRecoveryStrategy> {
    const result = new Map<string, ErrorRecoveryStrategy>();

    for (const [key, strategy] of this.recoveryRegistry) {
      const [keyGrammar, strategyName] = this.parseStrategyKey(key);
      if (keyGrammar === grammarName) {
        result.set(strategyName, strategy);
      }
    }

    return result;
  }

  /**
   * Applies error recovery for a specific error type.
   * @param grammarName The name of the grammar
   * @param errorType The type of error
   * @param context The error context
   * @returns The recovery result
   */
  public applyRecovery(grammarName: string, errorType: ErrorType, context: ErrorContext): RecoveryResult {
    const strategy = this.getStrategy(grammarName, errorType.toString());

    if (strategy) {
      return strategy.recover(context);
    }

    // Try default recovery strategies
    const defaultStrategy = this.getDefaultStrategy(errorType);
    if (defaultStrategy) {
      return defaultStrategy.recover(context);
    }

    // No recovery strategy found
    return new RecoveryResult(RecoveryAction.Fail, 'No recovery strategy available', null);
  }

  /**
   * Gets the default recovery strategy for an error type.
   * @param errorType The error type
   * @returns The default recovery strategy or null
   */
  private getDefaultStrategy(errorType: ErrorType): ErrorRecoveryStrategy | null {
    switch (errorType) {
      case ErrorType.SyntaxError:
        return new SynchronizationRecoveryStrategy();

      case ErrorType.SemanticError:
        return new SkipRecoveryStrategy();

      case ErrorType.LexicalError:
        return new CharacterSkipRecoveryStrategy();

      default:
        return null;
    }
  }

  /**
   * Creates a strategy key from grammar name and strategy name.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @returns The strategy key
   */
  private createStrategyKey(grammarName: string, strategyName: string): string {
    return `${grammarName}::recovery::${strategyName}`;
  }

  /**
   * Parses a strategy key into grammar name and strategy name.
   * @param key The strategy key
   * @returns Array containing grammar name and strategy name
   */
  private parseStrategyKey(key: string): [string, string] {
    const parts = key.split('::recovery::');
    return [parts[0], parts[1]];
  }

  /**
   * Clears the cache for a grammar and its dependents.
   * @param grammarName The name of the grammar
   */
  private clearCacheForGrammar(grammarName: string): void {
    // Clear cache for this grammar
    this.recoveryCache.delete(grammarName);

    // Clear cache for all dependent grammars
    const dependents = this.grammarContainer.getAllDependentGrammars(grammarName);
    for (const dependent of dependents) {
      this.recoveryCache.delete(dependent);
    }
  }

  /**
   * Clears all cached strategies.
   */
  public clearCache(): void {
    this.recoveryCache.clear();
  }

  /**
   * Removes all strategies for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of strategies removed
   */
  public removeAllStrategies(grammarName: string): number {
    let removedCount = 0;

    for (const key of this.recoveryRegistry.keys()) {
      const [keyGrammar] = this.parseStrategyKey(key);
      if (keyGrammar === grammarName) {
        this.recoveryRegistry.delete(key);
        removedCount++;
      }
    }

    if (removedCount > 0) {
      this.clearCacheForGrammar(grammarName);
    }

    return removedCount;
  }

  /**
   * Gets statistics about the error recovery manager.
   */
  public getStatistics(): ErrorRecoveryStatistics {
    const totalStrategies = this.recoveryRegistry.size;
    const grammarStrategyCounts = new Map<string, number>();

    for (const key of this.recoveryRegistry.keys()) {
      const [grammarName] = this.parseStrategyKey(key);
      const currentCount = grammarStrategyCounts.get(grammarName) || 0;
      grammarStrategyCounts.set(grammarName, currentCount + 1);
    }

    const cacheSize = Array.from(this.recoveryCache.values())
      .reduce((total, cache) => total + cache.size, 0);

    return new ErrorRecoveryStatistics(
      totalStrategies,
      grammarStrategyCounts.size,
      cacheSize,
      this.recoveryCache.size,
    );
  }
}

/**
 * Interface for error recovery strategies.
 */
export interface ErrorRecoveryStrategy {
  recover(context: ErrorContext): RecoveryResult;
  getStrategyType(): RecoveryStrategyType;
  getDescription(): string;
}

/**
 * Represents an error context for recovery.
 */
export class ErrorContext {
  private grammarName: string;
  private errorType: ErrorType;
  private errorMessage: string;
  private position: number;
  private lineNumber: number;
  private columnNumber: number;
  private tokens: any[];
  private parseStack: any[];
  private metadata: Map<string, any>;

  constructor(
    grammarName: string,
    errorType: ErrorType,
    errorMessage: string,
    position: number,
    lineNumber: number,
    columnNumber: number,
  ) {
    this.grammarName = grammarName;
    this.errorType = errorType;
    this.errorMessage = errorMessage;
    this.position = position;
    this.lineNumber = lineNumber;
    this.columnNumber = columnNumber;
    this.tokens = [];
    this.parseStack = [];
    this.metadata = new Map<string, any>();
  }

  public getGrammarName(): string {
    return this.grammarName;
  }

  public getErrorType(): ErrorType {
    return this.errorType;
  }

  public getErrorMessage(): string {
    return this.errorMessage;
  }

  public getPosition(): number {
    return this.position;
  }

  public getLineNumber(): number {
    return this.lineNumber;
  }

  public getColumnNumber(): number {
    return this.columnNumber;
  }

  public getTokens(): any[] {
    return [...this.tokens];
  }

  public setTokens(tokens: any[]): void {
    this.tokens = [...tokens];
  }

  public getParseStack(): any[] {
    return [...this.parseStack];
  }

  public setParseStack(stack: any[]): void {
    this.parseStack = [...stack];
  }

  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }
}

/**
 * Represents the result of error recovery.
 */
export class RecoveryResult {
  private action: RecoveryAction;
  private message: string;
  private recoveredTokens: any[] | null;
  private newPosition: number | null;
  private metadata: Map<string, any>;

  constructor(action: RecoveryAction, message: string, recoveredTokens: any[] | null, newPosition?: number) {
    this.action = action;
    this.message = message;
    this.recoveredTokens = recoveredTokens;
    this.newPosition = newPosition || null;
    this.metadata = new Map<string, any>();
  }

  public getAction(): RecoveryAction {
    return this.action;
  }

  public getMessage(): string {
    return this.message;
  }

  public getRecoveredTokens(): any[] | null {
    return this.recoveredTokens ? [...this.recoveredTokens] : null;
  }

  public getNewPosition(): number | null {
    return this.newPosition;
  }

  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }

  public isSuccessful(): boolean {
    return this.action !== RecoveryAction.Fail;
  }
}

/**
 * Enum representing error types.
 */
export enum ErrorType {
  SyntaxError = 'syntax',
  SemanticError = 'semantic',
  LexicalError = 'lexical',
  TypeError = 'type',
  ReferenceError = 'reference'
}

/**
 * Enum representing recovery actions.
 */
export enum RecoveryAction {
  Continue = 'continue',
  Skip = 'skip',
  Insert = 'insert',
  Delete = 'delete',
  Replace = 'replace',
  Synchronize = 'synchronize',
  Fail = 'fail'
}

/**
 * Enum representing recovery strategy types.
 */
export enum RecoveryStrategyType {
  Synchronization = 'synchronization',
  Skip = 'skip',
  Insert = 'insert',
  Delete = 'delete',
  Replace = 'replace',
  Panic = 'panic',
  Custom = 'custom'
}

/**
 * Synchronization-based error recovery strategy.
 */
export class SynchronizationRecoveryStrategy implements ErrorRecoveryStrategy {
  private synchronizationTokens: string[];

  constructor(synchronizationTokens: string[] = [';', '}', 'EOF']) {
    this.synchronizationTokens = synchronizationTokens;
  }

  public recover(context: ErrorContext): RecoveryResult {
    const tokens = context.getTokens();
    const currentPosition = context.getPosition();

    // Find the next synchronization token
    for (let i = currentPosition; i < tokens.length; i++) {
      const token = tokens[i];
      if (this.synchronizationTokens.includes(token.type || token.value)) {
        return new RecoveryResult(
          RecoveryAction.Synchronize,
          `Synchronized at token: ${token.type || token.value}`,
          tokens.slice(currentPosition, i + 1),
          i + 1,
        );
      }
    }

    // No synchronization token found, skip to end
    return new RecoveryResult(
      RecoveryAction.Skip,
      'No synchronization token found, skipping to end',
      tokens.slice(currentPosition),
      tokens.length,
    );
  }

  public getStrategyType(): RecoveryStrategyType {
    return RecoveryStrategyType.Synchronization;
  }

  public getDescription(): string {
    return `Synchronization recovery using tokens: ${this.synchronizationTokens.join(', ')}`;
  }
}

/**
 * Skip-based error recovery strategy.
 */
export class SkipRecoveryStrategy implements ErrorRecoveryStrategy {
  private skipCount: number;

  constructor(skipCount: number = 1) {
    this.skipCount = skipCount;
  }

  public recover(context: ErrorContext): RecoveryResult {
    const tokens = context.getTokens();
    const currentPosition = context.getPosition();
    const newPosition = Math.min(currentPosition + this.skipCount, tokens.length);

    return new RecoveryResult(
      RecoveryAction.Skip,
      `Skipped ${newPosition - currentPosition} token(s)`,
      tokens.slice(currentPosition, newPosition),
      newPosition,
    );
  }

  public getStrategyType(): RecoveryStrategyType {
    return RecoveryStrategyType.Skip;
  }

  public getDescription(): string {
    return `Skip ${this.skipCount} token(s)`;
  }
}

/**
 * Character skip error recovery strategy for lexical errors.
 */
export class CharacterSkipRecoveryStrategy implements ErrorRecoveryStrategy {
  public recover(context: ErrorContext): RecoveryResult {
    const position = context.getPosition();

    return new RecoveryResult(
      RecoveryAction.Skip,
      'Skipped invalid character',
      null,
      position + 1,
    );
  }

  public getStrategyType(): RecoveryStrategyType {
    return RecoveryStrategyType.Skip;
  }

  public getDescription(): string {
    return 'Skip invalid character';
  }
}

/**
 * Insert-based error recovery strategy.
 */
export class InsertRecoveryStrategy implements ErrorRecoveryStrategy {
  private insertToken: any;

  constructor(insertToken: any) {
    this.insertToken = insertToken;
  }

  public recover(context: ErrorContext): RecoveryResult {
    return new RecoveryResult(
      RecoveryAction.Insert,
      `Inserted token: ${this.insertToken.type || this.insertToken.value}`,
      [this.insertToken],
      context.getPosition(),
    );
  }

  public getStrategyType(): RecoveryStrategyType {
    return RecoveryStrategyType.Insert;
  }

  public getDescription(): string {
    return `Insert token: ${this.insertToken.type || this.insertToken.value}`;
  }
}

/**
 * Replace-based error recovery strategy.
 */
export class ReplaceRecoveryStrategy implements ErrorRecoveryStrategy {
  private replacementToken: any;

  constructor(replacementToken: any) {
    this.replacementToken = replacementToken;
  }

  public recover(context: ErrorContext): RecoveryResult {
    const tokens = context.getTokens();
    const currentPosition = context.getPosition();

    return new RecoveryResult(
      RecoveryAction.Replace,
      `Replaced token with: ${this.replacementToken.type || this.replacementToken.value}`,
      [this.replacementToken],
      currentPosition + 1,
    );
  }

  public getStrategyType(): RecoveryStrategyType {
    return RecoveryStrategyType.Replace;
  }

  public getDescription(): string {
    return `Replace with token: ${this.replacementToken.type || this.replacementToken.value}`;
  }
}

/**
 * Statistics about error recovery.
 */
export class ErrorRecoveryStatistics {
  private totalStrategies: number;
  private grammarCount: number;
  private cacheSize: number;
  private cacheGrammarCount: number;

  constructor(totalStrategies: number, grammarCount: number, cacheSize: number, cacheGrammarCount: number) {
    this.totalStrategies = totalStrategies;
    this.grammarCount = grammarCount;
    this.cacheSize = cacheSize;
    this.cacheGrammarCount = cacheGrammarCount;
  }

  public getTotalStrategies(): number {
    return this.totalStrategies;
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
    return `Error Recovery Statistics:
  Total Strategies: ${this.totalStrategies}
  Grammars with Strategies: ${this.grammarCount}
  Cached Strategies: ${this.cacheSize}
  Grammars in Cache: ${this.cacheGrammarCount}`;
  }
}

// Import required classes and interfaces
import { InheritanceResolver } from './InheritanceResolver';
import { GrammarContainer } from './GrammarContainer';

