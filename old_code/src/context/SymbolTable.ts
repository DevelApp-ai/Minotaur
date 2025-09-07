/**
 * Enhanced symbol table for context-aware parsing in Minotaur.
 * Provides hierarchical symbol management with scope awareness and real-time updates.
 */

import { SymbolInfo, SymbolKind, ScopeInfo, CodePosition, VariableInfo, FunctionInfo, ClassInfo } from './ContextAwareParser';
import { EventEmitter } from 'events';

/**
 * Symbol reference information.
 */
export interface SymbolReference {
  position: CodePosition;
  type: ReferenceType;
  context: string;
  timestamp: number;
}

/**
 * Reference types.
 */
export enum ReferenceType {
  READ = 'read',
  WRITE = 'write',
  CALL = 'call',
  DECLARATION = 'declaration',
  DEFINITION = 'definition',
  TYPE_REFERENCE = 'type_reference'
}

/**
 * Symbol resolution result.
 */
export interface SymbolResolution {
  symbol: SymbolInfo | null;
  scope: ScopeInfo | null;
  confidence: number;
  alternatives: SymbolInfo[];
  resolutionPath: string[];
}

/**
 * Symbol search criteria.
 */
export interface SymbolSearchCriteria {
  name?: string;
  kind?: SymbolKind;
  scope?: string;
  type?: string;
  position?: CodePosition;
  includeInherited?: boolean;
  includePrivate?: boolean;
}

/**
 * Symbol table statistics.
 */
export interface SymbolTableStats {
  totalSymbols: number;
  symbolsByKind: Map<SymbolKind, number>;
  symbolsByScope: Map<string, number>;
  totalReferences: number;
  averageReferencesPerSymbol: number;
  lastUpdateTime: number;
}

/**
 * Enhanced symbol table with hierarchical scope management.
 */
export class SymbolTable extends EventEmitter {
  private symbols: Map<string, SymbolInfo>;
  private references: Map<string, SymbolReference[]>;
  private scopeHierarchy: Map<string, ScopeInfo>;
  private symbolsByScope: Map<string, Set<string>>;
  private symbolsByKind: Map<SymbolKind, Set<string>>;
  private symbolsByType: Map<string, Set<string>>;
  private lastUpdateTime: number;
  private isRealTimeMode: boolean;

  constructor() {
    super();

    this.symbols = new Map();
    this.references = new Map();
    this.scopeHierarchy = new Map();
    this.symbolsByScope = new Map();
    this.symbolsByKind = new Map();
    this.symbolsByType = new Map();
    this.lastUpdateTime = Date.now();
    this.isRealTimeMode = false;

    this.initializeKindMaps();
  }

  /**
   * Initializes the symbol kind maps.
   */
  private initializeKindMaps(): void {
    for (const kind of Object.values(SymbolKind)) {
      this.symbolsByKind.set(kind, new Set());
    }
  }

  /**
   * Declares a new symbol in the table.
   */
  public declareSymbol(symbol: SymbolInfo): void {
    const key = this.getSymbolKey(symbol.scope, symbol.name);

    // Check for duplicate declarations in the same scope
    if (this.symbols.has(key)) {
      const existing = this.symbols.get(key)!;
      this.emit('symbol_redeclared', { existing, new: symbol });
    }

    // Add symbol to main table
    this.symbols.set(key, symbol);

    // Update indexes
    this.updateIndexes(symbol, 'add');

    // Add declaration reference
    this.addReference(symbol.scope, symbol.name, {
      position: symbol.position,
      type: ReferenceType.DECLARATION,
      context: symbol.scope,
      timestamp: Date.now(),
    });

    this.lastUpdateTime = Date.now();
    this.emit('symbol_declared', { symbol });
  }

  /**
   * Updates an existing symbol.
   */
  public updateSymbol(scope: string, name: string, updates: Partial<SymbolInfo>): boolean {
    const key = this.getSymbolKey(scope, name);
    const existing = this.symbols.get(key);

    if (!existing) {
      return false;
    }

    const oldSymbol = { ...existing };
    const updatedSymbol = { ...existing, ...updates };

    // Update main table
    this.symbols.set(key, updatedSymbol);

    // Update indexes if necessary
    if (updates.kind && updates.kind !== existing.kind) {
      this.updateIndexes(existing, 'remove');
      this.updateIndexes(updatedSymbol, 'add');
    }

    this.lastUpdateTime = Date.now();
    this.emit('symbol_updated', { oldSymbol, newSymbol: updatedSymbol });

    return true;
  }

  /**
   * Removes a symbol from the table.
   */
  public removeSymbol(scope: string, name: string): boolean {
    const key = this.getSymbolKey(scope, name);
    const symbol = this.symbols.get(key);

    if (!symbol) {
      return false;
    }

    // Remove from main table
    this.symbols.delete(key);

    // Update indexes
    this.updateIndexes(symbol, 'remove');

    // Remove references
    this.references.delete(key);

    this.lastUpdateTime = Date.now();
    this.emit('symbol_removed', { symbol });

    return true;
  }

  /**
   * Gets all symbols as a map for compatibility.
   */
  public getAllSymbols(): Map<string, SymbolInfo> {
    return new Map(this.symbols);
  }

  /**
   * Defines a symbol (alias for declareSymbol for compatibility).
   */
  public defineSymbol(symbol: SymbolInfo): void {
    this.declareSymbol(symbol);
  }

  /**
   * Looks up a symbol by scope and name.
   */
  public lookupSymbol(scope: string, name: string): SymbolInfo | null {
    const key = this.getSymbolKey(scope, name);
    return this.symbols.get(key) || null;
  }

  /**
   * Resolves a symbol with scope hierarchy traversal.
   */
  public resolveSymbol(name: string, currentScope: string, position?: CodePosition): SymbolResolution {
    const resolutionPath: string[] = [];
    const alternatives: SymbolInfo[] = [];
    let bestMatch: SymbolInfo | null = null;
    let bestScope: ScopeInfo | null = null;
    let confidence = 0;

    // Start from current scope and traverse up the hierarchy
    let scope = currentScope;
    while (scope) {
      resolutionPath.push(scope);

      const symbol = this.lookupSymbol(scope, name);
      if (symbol) {
        if (!bestMatch) {
          bestMatch = symbol;
          bestScope = this.scopeHierarchy.get(scope) || null;
          confidence = this.calculateConfidence(symbol, position);
        } else {
          alternatives.push(symbol);
        }
      }

      // Move to parent scope
      const scopeInfo = this.scopeHierarchy.get(scope);
      scope = scopeInfo?.parent?.id || '';
      if (!scope) {
        break;
      }
    }

    return {
      symbol: bestMatch,
      scope: bestScope,
      confidence,
      alternatives,
      resolutionPath,
    };
  }

  /**
   * Finds all symbols matching the given criteria.
   */
  public findSymbols(criteria: SymbolSearchCriteria): SymbolInfo[] {
    let candidates: Set<string> = new Set(this.symbols.keys());

    // Filter by kind
    if (criteria.kind) {
      const kindSymbols = this.symbolsByKind.get(criteria.kind);
      if (kindSymbols) {
        candidates = new Set([...candidates].filter(key => kindSymbols.has(key)));
      } else {
        return [];
      }
    }

    // Filter by scope
    if (criteria.scope) {
      const scopeSymbols = this.symbolsByScope.get(criteria.scope);
      if (scopeSymbols) {
        candidates = new Set([...candidates].filter(key => scopeSymbols.has(key)));
      } else {
        return [];
      }
    }

    // Filter by type
    if (criteria.type) {
      const typeSymbols = this.symbolsByType.get(criteria.type);
      if (typeSymbols) {
        candidates = new Set([...candidates].filter(key => typeSymbols.has(key)));
      } else {
        return [];
      }
    }

    // Convert to symbols and apply additional filters
    const results: SymbolInfo[] = [];
    for (const key of candidates) {
      const symbol = this.symbols.get(key);
      if (symbol && this.matchesAdditionalCriteria(symbol, criteria)) {
        results.push(symbol);
      }
    }

    return results;
  }

  /**
   * Gets all symbols in a specific scope.
   */
  public getSymbolsInScope(scope: string, includeInherited: boolean = false): SymbolInfo[] {
    const scopeSymbols = this.symbolsByScope.get(scope);
    if (!scopeSymbols) {
      return [];
    }

    const results: SymbolInfo[] = [];
    for (const key of scopeSymbols) {
      const symbol = this.symbols.get(key);
      if (symbol) {
        results.push(symbol);
      }
    }

    // Include inherited symbols if requested
    if (includeInherited) {
      const scopeInfo = this.scopeHierarchy.get(scope);
      if (scopeInfo?.parent) {
        results.push(...this.getSymbolsInScope(scopeInfo.parent.id, true));
      }
    }

    return results;
  }

  /**
   * Gets all symbols visible from a specific position.
   */
  public getVisibleSymbols(position: CodePosition, currentScope: string): SymbolInfo[] {
    const visibleSymbols: SymbolInfo[] = [];
    const visitedScopes = new Set<string>();

    // Traverse scope hierarchy
    let scope = currentScope;
    while (scope && !visitedScopes.has(scope)) {
      visitedScopes.add(scope);

      const scopeSymbols = this.getSymbolsInScope(scope);
      for (const symbol of scopeSymbols) {
        // Check if symbol is visible (declared before current position)
        if (this.isSymbolVisibleAt(symbol, position)) {
          visibleSymbols.push(symbol);
        }
      }

      // Move to parent scope
      const scopeInfo = this.scopeHierarchy.get(scope);
      scope = scopeInfo?.parent?.id || '';
    }

    return visibleSymbols;
  }

  /**
   * Adds a reference to a symbol.
   */
  public addReference(scope: string, name: string, reference: SymbolReference): void {
    const key = this.getSymbolKey(scope, name);

    if (!this.references.has(key)) {
      this.references.set(key, []);
    }

    this.references.get(key)!.push(reference);

    // Update symbol's reference list
    const symbol = this.symbols.get(key);
    if (symbol) {
      symbol.references.push(reference.position);
      this.emit('reference_added', { symbol, reference });
    }
  }

  /**
   * Gets all references to a symbol.
   */
  public getReferences(scope: string, name: string): SymbolReference[] {
    const key = this.getSymbolKey(scope, name);
    return this.references.get(key) || [];
  }

  /**
   * Finds all references to a symbol across all scopes.
   */
  public findAllReferences(name: string): SymbolReference[] {
    const allReferences: SymbolReference[] = [];

    for (const [key, references] of this.references) {
      const [, symbolName] = this.parseSymbolKey(key);
      if (symbolName === name) {
        allReferences.push(...references);
      }
    }

    return allReferences;
  }

  /**
   * Registers a scope in the hierarchy.
   */
  public registerScope(scope: ScopeInfo): void {
    this.scopeHierarchy.set(scope.id, scope);

    if (!this.symbolsByScope.has(scope.id)) {
      this.symbolsByScope.set(scope.id, new Set());
    }

    this.emit('scope_registered', { scope });
  }

  /**
   * Unregisters a scope and removes all its symbols.
   */
  public unregisterScope(scopeId: string): void {
    const scope = this.scopeHierarchy.get(scopeId);
    if (!scope) {
      return;
    }

    // Remove all symbols in this scope
    const scopeSymbols = this.symbolsByScope.get(scopeId);
    if (scopeSymbols) {
      for (const key of scopeSymbols) {
        const symbol = this.symbols.get(key);
        if (symbol) {
          this.removeSymbol(symbol.scope, symbol.name);
        }
      }
    }

    // Remove scope from hierarchy
    this.scopeHierarchy.delete(scopeId);
    this.symbolsByScope.delete(scopeId);

    this.emit('scope_unregistered', { scope });
  }

  /**
   * Clears all symbols and scopes.
   */
  public clear(): void {
    this.symbols.clear();
    this.references.clear();
    this.scopeHierarchy.clear();
    this.symbolsByScope.clear();
    this.symbolsByType.clear();

    // Reset kind maps
    for (const kindSet of this.symbolsByKind.values()) {
      kindSet.clear();
    }

    this.lastUpdateTime = Date.now();
    this.emit('table_cleared');
  }

  /**
   * Gets symbol table statistics.
   */
  public getStatistics(): SymbolTableStats {
    const symbolsByKind = new Map<SymbolKind, number>();
    for (const [kind, symbolSet] of this.symbolsByKind) {
      symbolsByKind.set(kind, symbolSet.size);
    }

    const symbolsByScope = new Map<string, number>();
    for (const [scope, symbolSet] of this.symbolsByScope) {
      symbolsByScope.set(scope, symbolSet.size);
    }

    const totalReferences = Array.from(this.references.values())
      .reduce((sum, refs) => sum + refs.length, 0);

    return {
      totalSymbols: this.symbols.size,
      symbolsByKind,
      symbolsByScope,
      totalReferences,
      averageReferencesPerSymbol: this.symbols.size > 0 ? totalReferences / this.symbols.size : 0,
      lastUpdateTime: this.lastUpdateTime,
    };
  }

  /**
   * Enables or disables real-time mode.
   */
  public setRealTimeMode(enabled: boolean): void {
    this.isRealTimeMode = enabled;
    this.emit('real_time_mode_changed', { enabled });
  }

  /**
   * Exports the symbol table to a serializable format.
   */
  public export(): any {
    return {
      symbols: Array.from(this.symbols.entries()),
      references: Array.from(this.references.entries()),
      scopeHierarchy: Array.from(this.scopeHierarchy.entries()),
      lastUpdateTime: this.lastUpdateTime,
    };
  }

  /**
   * Imports symbol table data from a serialized format.
   */
  public import(data: any): void {
    this.clear();

    // Import symbols
    for (const [key, symbol] of data.symbols) {
      this.symbols.set(key, symbol);
      this.updateIndexes(symbol, 'add');
    }

    // Import references
    for (const [key, references] of data.references) {
      this.references.set(key, references);
    }

    // Import scope hierarchy
    for (const [id, scope] of data.scopeHierarchy) {
      this.scopeHierarchy.set(id, scope);
    }

    this.lastUpdateTime = data.lastUpdateTime || Date.now();
    this.emit('table_imported', { symbolCount: this.symbols.size });
  }

  /**
   * Private helper methods.
   */

  private getSymbolKey(scope: string, name: string): string {
    return `${scope}:${name}`;
  }

  private parseSymbolKey(key: string): [string, string] {
    const colonIndex = key.indexOf(':');
    return [key.substring(0, colonIndex), key.substring(colonIndex + 1)];
  }

  private updateIndexes(symbol: SymbolInfo, operation: 'add' | 'remove'): void {
    const key = this.getSymbolKey(symbol.scope, symbol.name);

    // Update scope index
    if (!this.symbolsByScope.has(symbol.scope)) {
      this.symbolsByScope.set(symbol.scope, new Set());
    }
    const scopeSet = this.symbolsByScope.get(symbol.scope)!;

    // Update kind index
    const kindSet = this.symbolsByKind.get(symbol.kind)!;

    // Update type index
    if (!this.symbolsByType.has(symbol.type)) {
      this.symbolsByType.set(symbol.type, new Set());
    }
    const typeSet = this.symbolsByType.get(symbol.type)!;

    if (operation === 'add') {
      scopeSet.add(key);
      kindSet.add(key);
      typeSet.add(key);
    } else {
      scopeSet.delete(key);
      kindSet.delete(key);
      typeSet.delete(key);
    }
  }

  private matchesAdditionalCriteria(symbol: SymbolInfo, criteria: SymbolSearchCriteria): boolean {
    // Filter by name
    if (criteria.name && symbol.name !== criteria.name) {
      return false;
    }

    // Filter by position (if symbol is visible at that position)
    if (criteria.position && !this.isSymbolVisibleAt(symbol, criteria.position)) {
      return false;
    }

    return true;
  }

  private isSymbolVisibleAt(symbol: SymbolInfo, position: CodePosition): boolean {
    // Check if symbol is declared before the given position
    if (symbol.position.line > position.line) {
      return false;
    }

    if (symbol.position.line === position.line && symbol.position.column > position.column) {
      return false;
    }

    return true;
  }

  private calculateConfidence(symbol: SymbolInfo, position?: CodePosition): number {
    let confidence = 1.0;

    // Reduce confidence based on distance from position
    if (position) {
      const distance = Math.abs(symbol.position.line - position.line);
      confidence *= Math.max(0.1, 1.0 - (distance * 0.01));
    }

    // Increase confidence for frequently referenced symbols
    const referenceCount = symbol.references.length;
    confidence *= Math.min(1.5, 1.0 + (referenceCount * 0.1));

    return Math.min(1.0, confidence);
  }
}

/**
 * Symbol table factory for creating specialized symbol tables.
 */
export class SymbolTableFactory {
  /**
   * Creates a symbol table optimized for real-time parsing.
   */
  public static createRealTimeTable(): SymbolTable {
    const table = new SymbolTable();
    table.setRealTimeMode(true);
    return table;
  }

  /**
   * Creates a symbol table for batch processing.
   */
  public static createBatchTable(): SymbolTable {
    const table = new SymbolTable();
    table.setRealTimeMode(false);
    return table;
  }

  /**
   * Creates a symbol table with predefined global symbols.
   */
  public static createWithGlobals(globalSymbols: SymbolInfo[]): SymbolTable {
    const table = new SymbolTable();

    // Add global scope
    const globalScope: ScopeInfo = {
      id: 'global',
      type: 'global' as any,
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

    table.registerScope(globalScope);

    // Add global symbols
    for (const symbol of globalSymbols) {
      table.declareSymbol(symbol);
    }

    return table;
  }
}

// Export the main symbol table class as default
export default SymbolTable;

