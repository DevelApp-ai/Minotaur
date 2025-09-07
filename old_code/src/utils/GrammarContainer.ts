/**
 * Represents a container for multiple grammar definitions.
 */
export class GrammarContainer {
  private grammarMap: Map<string, Grammar>;
  private grammarInterpreterMap: Map<string, GrammarInterpreter>;
  private grammarErrors: GrammarError[];

  // Inheritance support properties
  private inheritanceGraph: Map<string, string[]>; // grammar -> base grammars
  private dependentsGraph: Map<string, string[]>; // grammar -> dependent grammars
  private grammarVersions: Map<string, number>; // grammar -> version number
  private loadTimestamps: Map<string, number>; // grammar -> load timestamp

  /**
   * Creates a new GrammarContainer instance.
   */
  constructor() {
    this.grammarMap = new Map<string, Grammar>();
    this.grammarInterpreterMap = new Map<string, GrammarInterpreter>();
    this.grammarErrors = [];

    // Initialize inheritance support
    this.inheritanceGraph = new Map<string, string[]>();
    this.dependentsGraph = new Map<string, string[]>();
    this.grammarVersions = new Map<string, number>();
    this.loadTimestamps = new Map<string, number>();
  }

  /**
   * Gets the grammar errors.
   */
  public getGrammarErrors(): GrammarError[] {
    return this.grammarErrors;
  }

  /**
   * Adds a grammar error.
   * @param error The error to add
   */
  public addGrammarError(error: GrammarError): void {
    this.grammarErrors.push(error);
  }

  /**
   * Clears all grammar errors.
   */
  public clearGrammarErrors(): void {
    this.grammarErrors = [];
  }

  /**
   * Adds a grammar to the container.
   * @param grammar The grammar to add
   */
  public addGrammar(grammar: Grammar): void {
    const grammarName = grammar.getName();

    // Update version number
    const currentVersion = this.grammarVersions.get(grammarName) || 0;
    this.grammarVersions.set(grammarName, currentVersion + 1);

    // Update load timestamp
    this.loadTimestamps.set(grammarName, Date.now());

    // Update inheritance graph
    const baseGrammars = grammar.getBaseGrammars();
    this.inheritanceGraph.set(grammarName, [...baseGrammars]);

    // Update dependents graph
    this.updateDependentsGraph(grammarName, baseGrammars);

    // Add to grammar map
    this.grammarMap.set(grammarName, grammar);
  }

  /**
   * Updates the dependents graph when a grammar is added.
   * @param grammarName The name of the grammar
   * @param baseGrammars The base grammars it depends on
   */
  private updateDependentsGraph(grammarName: string, baseGrammars: string[]): void {
    // Remove old dependencies
    for (const [baseName, dependents] of this.dependentsGraph) {
      const index = dependents.indexOf(grammarName);
      if (index !== -1) {
        dependents.splice(index, 1);
      }
    }

    // Add new dependencies
    for (const baseName of baseGrammars) {
      if (!this.dependentsGraph.has(baseName)) {
        this.dependentsGraph.set(baseName, []);
      }
      const dependents = this.dependentsGraph.get(baseName)!;
      if (!dependents.includes(grammarName)) {
        dependents.push(grammarName);
      }
    }
  }

  /**
   * Gets a grammar by name.
   * @param grammarName The name of the grammar
   * @returns The grammar or null if not found
   */
  public getGrammar(grammarName: string): Grammar | null {
    return this.grammarMap.get(grammarName) || null;
  }

  /**
   * Removes a grammar from the container.
   * @param grammarName The name of the grammar to remove
   * @returns Whether the grammar was removed
   */
  public removeGrammar(grammarName: string): boolean {
    if (!this.grammarMap.has(grammarName)) {
      return false;
    }

    // Remove from all maps
    this.grammarMap.delete(grammarName);
    this.grammarInterpreterMap.delete(grammarName);
    this.inheritanceGraph.delete(grammarName);
    this.dependentsGraph.delete(grammarName);
    this.grammarVersions.delete(grammarName);
    this.loadTimestamps.delete(grammarName);

    // Remove from dependents graph
    for (const dependents of this.dependentsGraph.values()) {
      const index = dependents.indexOf(grammarName);
      if (index !== -1) {
        dependents.splice(index, 1);
      }
    }

    return true;
  }

  /**
   * Adds a grammar interpreter to the container.
   * @param interpreter The interpreter to add
   */
  public addGrammarInterpreter(interpreter: GrammarInterpreter): void {
    this.grammarInterpreterMap.set(interpreter.getGrammarName(), interpreter);
  }

  /**
   * Gets a grammar interpreter by name.
   * @param grammarName The name of the grammar
   * @returns The interpreter or null if not found
   */
  public getGrammarInterpreter(grammarName: string): GrammarInterpreter | null {
    return this.grammarInterpreterMap.get(grammarName) || null;
  }

  /**
   * Checks if a grammar exists.
   * @param grammarName The name of the grammar
   * @returns Whether the grammar exists
   */
  public hasGrammar(grammarName: string): boolean {
    return this.grammarMap.has(grammarName);
  }

  /**
   * Gets all grammar names.
   * @returns The grammar names
   */
  public getGrammarNames(): string[] {
    return Array.from(this.grammarMap.keys());
  }

  /**
   * Gets all grammars.
   * @returns Array of all grammars
   */
  public getAllGrammars(): Grammar[] {
    return Array.from(this.grammarMap.values());
  }

  // ============================================================================
  // INHERITANCE SUPPORT METHODS
  // ============================================================================

  /**
   * Gets the base grammars for a given grammar.
   * @param grammarName The name of the grammar
   * @returns Array of base grammar names
   */
  public getBaseGrammars(grammarName: string): string[] {
    return this.inheritanceGraph.get(grammarName) || [];
  }

  /**
   * Gets the dependent grammars for a given grammar.
   * @param grammarName The name of the grammar
   * @returns Array of dependent grammar names
   */
  public getDependentGrammars(grammarName: string): string[] {
    return this.dependentsGraph.get(grammarName) || [];
  }

  /**
   * Gets all grammars that inherit from a given grammar (recursively).
   * @param grammarName The name of the base grammar
   * @returns Array of all dependent grammar names
   */
  public getAllDependentGrammars(grammarName: string): string[] {
    const result = new Set<string>();
    const visited = new Set<string>();

    const collectDependents = (name: string) => {
      if (visited.has(name)) {
        return;
      }
      visited.add(name);

      const directDependents = this.getDependentGrammars(name);
      for (const dependent of directDependents) {
        result.add(dependent);
        collectDependents(dependent);
      }
    };

    collectDependents(grammarName);
    return Array.from(result);
  }

  /**
   * Gets the inheritance hierarchy for a grammar.
   * @param grammarName The name of the grammar
   * @returns Array of grammar names in inheritance order (base to derived)
   */
  public getInheritanceHierarchy(grammarName: string): string[] {
    const hierarchy: string[] = [];
    const visited = new Set<string>();

    const buildHierarchy = (name: string) => {
      if (visited.has(name)) {
        return;
      }
      visited.add(name);

      const baseGrammars = this.getBaseGrammars(name);
      for (const baseName of baseGrammars) {
        buildHierarchy(baseName);
      }

      hierarchy.push(name);
    };

    buildHierarchy(grammarName);
    return hierarchy;
  }

  /**
   * Checks if a grammar inherits from another grammar (directly or indirectly).
   * @param derivedGrammar The derived grammar name
   * @param baseGrammar The base grammar name
   * @returns Whether the derived grammar inherits from the base grammar
   */
  public inheritsFrom(derivedGrammar: string, baseGrammar: string): boolean {
    const visited = new Set<string>();

    const checkInheritance = (current: string): boolean => {
      if (visited.has(current)) {
        return false;
      }
      visited.add(current);

      const bases = this.getBaseGrammars(current);
      if (bases.includes(baseGrammar)) {
        return true;
      }

      return bases.some(baseName => checkInheritance(baseName));
    };

    return checkInheritance(derivedGrammar);
  }

  /**
   * Detects circular inheritance dependencies.
   * @returns Array of grammar names involved in circular dependencies
   */
  public detectCircularDependencies(): string[] {
    const circularGrammars = new Set<string>();
    const visited = new Set<string>();
    const recursionStack = new Set<string>();

    const detectCycle = (grammarName: string): boolean => {
      if (recursionStack.has(grammarName)) {
        circularGrammars.add(grammarName);
        return true;
      }

      if (visited.has(grammarName)) {
        return false;
      }

      visited.add(grammarName);
      recursionStack.add(grammarName);

      const baseGrammars = this.getBaseGrammars(grammarName);
      for (const baseName of baseGrammars) {
        if (detectCycle(baseName)) {
          circularGrammars.add(grammarName);
        }
      }

      recursionStack.delete(grammarName);
      return false;
    };

    for (const grammarName of this.getGrammarNames()) {
      if (!visited.has(grammarName)) {
        detectCycle(grammarName);
      }
    }

    return Array.from(circularGrammars);
  }

  /**
   * Gets the version number of a grammar.
   * @param grammarName The name of the grammar
   * @returns The version number or 0 if not found
   */
  public getGrammarVersion(grammarName: string): number {
    return this.grammarVersions.get(grammarName) || 0;
  }

  /**
   * Gets the load timestamp of a grammar.
   * @param grammarName The name of the grammar
   * @returns The load timestamp or 0 if not found
   */
  public getLoadTimestamp(grammarName: string): number {
    return this.loadTimestamps.get(grammarName) || 0;
  }

  /**
   * Gets grammars sorted by dependency order (base grammars first).
   * @returns Array of grammar names in dependency order
   */
  public getGrammarsByDependencyOrder(): string[] {
    const result: string[] = [];
    const visited = new Set<string>();

    const visit = (grammarName: string) => {
      if (visited.has(grammarName)) {
        return;
      }
      visited.add(grammarName);

      // Visit base grammars first
      const baseGrammars = this.getBaseGrammars(grammarName);
      for (const baseName of baseGrammars) {
        if (this.hasGrammar(baseName)) {
          visit(baseName);
        }
      }

      result.push(grammarName);
    };

    for (const grammarName of this.getGrammarNames()) {
      visit(grammarName);
    }

    return result;
  }

  /**
   * Clears all grammars and interpreters.
   */
  public clear(): void {
    this.grammarMap.clear();
    this.grammarInterpreterMap.clear();
    this.grammarErrors = [];
    this.inheritanceGraph.clear();
    this.dependentsGraph.clear();
    this.grammarVersions.clear();
    this.loadTimestamps.clear();
  }

  /**
   * Gets statistics about the grammar container.
   */
  public getStatistics(): GrammarContainerStatistics {
    const totalGrammars = this.grammarMap.size;
    const inheritableGrammars = Array.from(this.grammarMap.values())
      .filter(g => g.isInheritable()).length;
    const grammarsWithInheritance = Array.from(this.inheritanceGraph.values())
      .filter(bases => bases.length > 0).length;
    const circularDependencies = this.detectCircularDependencies().length;

    return new GrammarContainerStatistics(
      totalGrammars,
      inheritableGrammars,
      grammarsWithInheritance,
      circularDependencies,
      this.grammarErrors.length,
    );
  }
}

/**
 * Represents statistics about a grammar container.
 */
export class GrammarContainerStatistics {
  private totalGrammars: number;
  private inheritableGrammars: number;
  private grammarsWithInheritance: number;
  private circularDependencies: number;
  private totalErrors: number;

  constructor(
    totalGrammars: number,
    inheritableGrammars: number,
    grammarsWithInheritance: number,
    circularDependencies: number,
    totalErrors: number,
  ) {
    this.totalGrammars = totalGrammars;
    this.inheritableGrammars = inheritableGrammars;
    this.grammarsWithInheritance = grammarsWithInheritance;
    this.circularDependencies = circularDependencies;
    this.totalErrors = totalErrors;
  }

  public getTotalGrammars(): number {
    return this.totalGrammars;
  }

  public getInheritableGrammars(): number {
    return this.inheritableGrammars;
  }

  public getGrammarsWithInheritance(): number {
    return this.grammarsWithInheritance;
  }

  public getCircularDependencies(): number {
    return this.circularDependencies;
  }

  public getTotalErrors(): number {
    return this.totalErrors;
  }

  public toString(): string {
    return `Grammar Container Statistics:
  Total Grammars: ${this.totalGrammars}
  Inheritable Grammars: ${this.inheritableGrammars}
  Grammars with Inheritance: ${this.grammarsWithInheritance}
  Circular Dependencies: ${this.circularDependencies}
  Total Errors: ${this.totalErrors}`;
  }
}

/**
 * Represents a grammar error.
 */
export class GrammarError {
  private message: string;
  private lineNumber: number;
  private fileName: string;

  /**
   * Creates a new GrammarError instance.
   * @param message The error message
   * @param lineNumber The line number where the error occurred
   * @param fileName The file name where the error occurred
   */
  constructor(message: string, lineNumber: number = -1, fileName: string = '') {
    this.message = message;
    this.lineNumber = lineNumber;
    this.fileName = fileName;
  }

  /**
   * Gets the error message.
   */
  public getMessage(): string {
    return this.message;
  }

  /**
   * Gets the line number.
   */
  public getLineNumber(): number {
    return this.lineNumber;
  }

  /**
   * Gets the file name.
   */
  public getFileName(): string {
    return this.fileName;
  }

  /**
   * Creates a string representation of this error.
   */
  public toString(): string {
    if (this.lineNumber !== -1 && this.fileName !== '') {
      return `Error in ${this.fileName} at line ${this.lineNumber}: ${this.message}`;
    } else if (this.lineNumber !== -1) {
      return `Error at line ${this.lineNumber}: ${this.message}`;
    } else {
      return `Error: ${this.message}`;
    }
  }
}

// Import required classes
import { Grammar } from './Grammar';
import { GrammarInterpreter } from './GrammarInterpreter';
