/**
 * Main interpreter class that coordinates the overall parsing and interpretation process.
 */
export class Interpreter {
  private grammarContainer: GrammarContainer;
  private sourceCodeContainer: SourceCodeContainer | null;
  private stepParser: StepParser;
  private callbackRegistry: Map<string, Function>;
  private inheritanceResolver: InheritanceResolver;
  private grammarLoadOrder: string[];

  // Semantic action support
  private semanticActionManager: SemanticActionManager;
  private semanticActionFactory: SemanticActionFactory;

  // Precedence and associativity support
  private precedenceManager: PrecedenceManager;
  private precedenceFactory: PrecedenceFactory;

  // Error recovery support
  private errorRecoveryManager: ErrorRecoveryManager;

  /**
   * Creates a new Interpreter instance.
   */
  constructor() {
    this.grammarContainer = new GrammarContainer();
    this.sourceCodeContainer = null;
    this.stepParser = new StepParser();
    this.callbackRegistry = new Map<string, Function>();
    this.inheritanceResolver = new InheritanceResolver(this.grammarContainer);
    this.grammarLoadOrder = [];

    // Initialize semantic action support
    this.semanticActionManager = new SemanticActionManager(this.grammarContainer, this.inheritanceResolver);
    this.semanticActionFactory = new SemanticActionFactory();

    // Initialize precedence support
    this.precedenceManager = new PrecedenceManager(this.grammarContainer, this.inheritanceResolver);
    this.precedenceFactory = new PrecedenceFactory();

    // Initialize error recovery support
    this.errorRecoveryManager = new ErrorRecoveryManager(this.grammarContainer, this.inheritanceResolver);
  }

  /**
   * Gets the grammar container.
   */
  public getGrammarContainer(): GrammarContainer {
    return this.grammarContainer;
  }

  /**
   * Gets the inheritance resolver.
   */
  public getInheritanceResolver(): InheritanceResolver {
    return this.inheritanceResolver;
  }

  /**
   * Gets the semantic action manager.
   */
  public getSemanticActionManager(): SemanticActionManager {
    return this.semanticActionManager;
  }

  /**
   * Gets the semantic action factory.
   */
  public getSemanticActionFactory(): SemanticActionFactory {
    return this.semanticActionFactory;
  }

  /**
   * Gets the precedence manager.
   */
  public getPrecedenceManager(): PrecedenceManager {
    return this.precedenceManager;
  }

  /**
   * Gets the precedence factory.
   */
  public getPrecedenceFactory(): PrecedenceFactory {
    return this.precedenceFactory;
  }

  /**
   * Gets the error recovery manager.
   */
  public getErrorRecoveryManager(): ErrorRecoveryManager {
    return this.errorRecoveryManager;
  }

  /**
   * Gets the grammar errors.
   */
  public getGrammarErrors(): GrammarError[] {
    return this.grammarContainer.getGrammarErrors();
  }

  /**
   * Loads a grammar from a string with inheritance support.
   * @param content The grammar content
   * @param fileName The name of the file
   * @param resolveInheritance Whether to resolve inheritance immediately
   * @returns The loaded grammar
   */
  public loadGrammarFromString(content: string, fileName: string, resolveInheritance: boolean = true): Grammar {
    const interpreter = new GrammarInterpreter(this.grammarContainer);
    const grammar = interpreter.parseGrammar(content, fileName);

    // Add to container first (needed for inheritance resolution)
    this.grammarContainer.addGrammar(grammar);
    this.grammarContainer.addGrammarInterpreter(interpreter);

    // Track load order for dependency resolution
    if (!this.grammarLoadOrder.includes(grammar.getName())) {
      this.grammarLoadOrder.push(grammar.getName());
    }

    // Resolve inheritance if requested and grammar has base grammars
    if (resolveInheritance && grammar.getBaseGrammars().length > 0) {
      try {
        const resolvedGrammar = this.inheritanceResolver.resolveInheritance(grammar.getName());
        this.grammarContainer.addGrammar(resolvedGrammar); // Update with resolved version
        return resolvedGrammar;
      } catch (error) {
        const grammarError = new GrammarError(
          // eslint-disable-next-line max-len
          `Inheritance resolution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
          0,
          fileName,
        );
        this.grammarContainer.addGrammarError(grammarError);
        return grammar; // Return unresolved grammar
      }
    }

    return grammar;
  }

  /**
   * Loads multiple grammars with proper dependency ordering.
   * @param grammars Array of grammar content and file name pairs
   * @returns Array of loaded grammars
   */
  public loadGrammarsWithDependencies(grammars: Array<{content: string, fileName: string}>): Grammar[] {
    const loadedGrammars: Grammar[] = [];
    const pendingGrammars = [...grammars];
    const maxIterations = grammars.length * 2; // Prevent infinite loops
    let iterations = 0;

    while (pendingGrammars.length > 0 && iterations < maxIterations) {
      const initialCount = pendingGrammars.length;

      for (let i = pendingGrammars.length - 1; i >= 0; i--) {
        const grammarInfo = pendingGrammars[i];

        try {
          // Try to load without resolving inheritance first
          const grammar = this.loadGrammarFromString(grammarInfo.content, grammarInfo.fileName, false);

          // Check if all base grammars are available
          const baseGrammars = grammar.getBaseGrammars();
          const allBasesAvailable = baseGrammars.every(baseName =>
            this.grammarContainer.hasGrammar(baseName),
          );

          if (allBasesAvailable || baseGrammars.length === 0) {
            // Resolve inheritance now that dependencies are available
            if (baseGrammars.length > 0) {
              const resolvedGrammar = this.inheritanceResolver.resolveInheritance(grammar.getName());
              this.grammarContainer.addGrammar(resolvedGrammar);
              loadedGrammars.push(resolvedGrammar);
            } else {
              loadedGrammars.push(grammar);
            }

            // Remove from pending
            pendingGrammars.splice(i, 1);
          }
        } catch (error) {
          // If loading fails, add error and remove from pending
          const grammarError = new GrammarError(
            // eslint-disable-next-line max-len
            `Failed to load grammar: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
            0,
            grammarInfo.fileName,
          );
          this.grammarContainer.addGrammarError(grammarError);
          pendingGrammars.splice(i, 1);
        }
      }

      // If no progress was made, we have unresolvable dependencies
      if (pendingGrammars.length === initialCount) {
        for (const grammarInfo of pendingGrammars) {
          const grammarError = new GrammarError(
            `Unresolvable grammar dependencies in: ${grammarInfo.fileName}`,
            0,
            grammarInfo.fileName,
          );
          this.grammarContainer.addGrammarError(grammarError);
        }
        break;
      }

      iterations++;
    }

    return loadedGrammars;
  }

  /**
   * Validates all loaded grammars for inheritance consistency.
   * @returns Validation results for all grammars
   */
  public validateAllGrammarInheritance(): Map<string, InheritanceValidationResult> {
    const results = new Map<string, InheritanceValidationResult>();

    for (const grammarName of this.grammarContainer.getGrammarNames()) {
      const result = this.inheritanceResolver.validateInheritance(grammarName);
      results.set(grammarName, result);
    }

    return results;
  }

  /**
   * Reloads a grammar and resolves its inheritance.
   * @param grammarName The name of the grammar to reload
   * @returns The reloaded grammar or null if not found
   */
  public reloadGrammar(grammarName: string): Grammar | null {
    const interpreter = this.grammarContainer.getGrammarInterpreter(grammarName);
    if (!interpreter) {
      return null;
    }

    // Clear inheritance cache to force re-resolution
    this.inheritanceResolver.clearCache();

    // Re-resolve inheritance
    try {
      const resolvedGrammar = this.inheritanceResolver.resolveInheritance(grammarName);
      this.grammarContainer.addGrammar(resolvedGrammar);
      return resolvedGrammar;
    } catch (error) {
      const grammarError = new GrammarError(
        // eslint-disable-next-line max-len
        `Grammar reload failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
        0,
        `${grammarName}.grammar`,
      );
      this.grammarContainer.addGrammarError(grammarError);
      return null;
    }
  }

  /**
   * Gets the grammar load order.
   */
  public getGrammarLoadOrder(): string[] {
    return [...this.grammarLoadOrder];
  }

  /**
   * Clears the inheritance cache.
   */
  public clearInheritanceCache(): void {
    this.inheritanceResolver.clearCache();
  }

  /**
   * Parses source code using the specified grammar.
   * @param grammarName The name of the grammar to use
   * @param sourceCode The source code to parse
   * @param fileName The name of the file
   * @returns The parse results
   */
  public async parseSourceCode(grammarName: string, sourceCode: string, fileName: string): Promise<ProductionMatch[]> {
    // Get the grammar
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      throw new Error(`Grammar ${grammarName} not found`);
    }

    // Set active grammar in parser
    this.stepParser.setActiveGrammar(grammar);

    // Create source code container
    this.sourceCodeContainer = new SourceCodeContainer();
    this.sourceCodeContainer.loadFromString(sourceCode, fileName);

    // Parse the source code
    return await this.stepParser.parse(grammarName, this.sourceCodeContainer);
  }

  /**
   * Parses a source code file using the specified grammar.
   * @param grammarName The name of the grammar to use
   * @param sourceCodeFile The path to the source code file
   * @returns The parse results
   */
  public parseSourceCodeFile(grammarName: string, sourceCodeFile: string): ProductionMatch[] {
    // In a browser environment, file system access is not available
    // This method should be used with the File API or by providing content directly
    throw new Error('File system access is not available in browser environment. Use parseSourceCode() with file content instead.');
  }

  /**
   * Registers a callback function.
   * @param name The name of the callback
   * @param callback The callback function
   */
  public registerCallback(name: string, callback: Function): void {
    this.callbackRegistry.set(name, callback);
    // Also register with the semantic action factory
    this.semanticActionFactory.registerCallback(name, callback);
  }

  /**
   * Gets a callback function.
   * @param name The name of the callback
   * @returns The callback function or null if not found
   */
  public getCallback(name: string): Function | null {
    return this.callbackRegistry.get(name) || null;
  }

  // ============================================================================
  // SEMANTIC ACTION METHODS
  // ============================================================================

  /**
   * Registers a semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param action The semantic action
   */
  public registerSemanticAction(grammarName: string, actionName: string, action: SemanticAction): void {
    this.semanticActionManager.registerAction(grammarName, actionName, action);
  }

  /**
   * Creates and registers a callback-based semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param callbackName The name of the callback
   * @returns Whether the action was created successfully
   */
  public createCallbackSemanticAction(grammarName: string, actionName: string, callbackName: string): boolean {
    const action = this.semanticActionFactory.createFromCallback(actionName, grammarName, callbackName);
    if (action) {
      this.semanticActionManager.registerAction(grammarName, actionName, action);
      return true;
    }
    return false;
  }

  /**
   * Creates and registers a template-based semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param template The template string
   */
  public createTemplateSemanticAction(grammarName: string, actionName: string, template: string): void {
    const action = this.semanticActionFactory.createTemplateAction(actionName, grammarName, template);
    this.semanticActionManager.registerAction(grammarName, actionName, action);
  }

  /**
   * Creates and registers a script-based semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param script The script code
   * @param language The script language
   */
  public createScriptSemanticAction(
    grammarName: string,
    actionName: string,
    script: string,
    language: ScriptLanguage = ScriptLanguage.JavaScript,
  ): void {
    const action = this.semanticActionFactory.createScriptAction(actionName, grammarName, script, language);
    this.semanticActionManager.registerAction(grammarName, actionName, action);
  }

  /**
   * Gets a semantic action with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns The semantic action or null if not found
   */
  public getSemanticAction(grammarName: string, actionName: string): SemanticAction | null {
    return this.semanticActionManager.getAction(grammarName, actionName);
  }

  /**
   * Executes a semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @param productionName The name of the production
   * @param args The arguments
   * @returns The result of the action execution
   */
  public executeSemanticAction(
    grammarName: string,
    actionName: string,
    productionName: string,
    args: any[],
  ): any {
    const action = this.semanticActionManager.getAction(grammarName, actionName);
    if (!action) {
      throw new Error(`Semantic action '${actionName}' not found for grammar '${grammarName}'`);
    }

    const context = new SemanticActionContext(grammarName, productionName);
    return action.execute(context, args);
  }

  /**
   * Gets all semantic actions for a grammar.
   * @param grammarName The name of the grammar
   * @returns Map of action names to semantic actions
   */
  public getAllSemanticActions(grammarName: string): Map<string, SemanticAction> {
    return this.semanticActionManager.getAllActions(grammarName);
  }

  /**
   * Removes a semantic action.
   * @param grammarName The name of the grammar
   * @param actionName The name of the action
   * @returns Whether the action was removed
   */
  public removeSemanticAction(grammarName: string, actionName: string): boolean {
    return this.semanticActionManager.removeAction(grammarName, actionName);
  }

  /**
   * Clears all semantic actions for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of actions removed
   */
  public clearSemanticActions(grammarName: string): number {
    return this.semanticActionManager.removeAllActions(grammarName);
  }

  /**
   * Processes semantic actions from grammar inheritance.
   * @param grammarName The name of the grammar
   */
  public processInheritedSemanticActions(grammarName: string): void {
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      return;
    }

    // Process semantic actions from base grammars
    const baseGrammars = grammar.getBaseGrammars();
    for (const baseGrammarName of baseGrammars) {
      const inheritedActions = this.semanticActionFactory.createInheritedActions(
        grammarName,
        baseGrammarName,
        this.semanticActionManager,
      );

      for (const action of inheritedActions) {
        // Only add if not already overridden
        if (!this.semanticActionManager.hasAction(grammarName, action.getName())) {
          this.semanticActionManager.registerAction(grammarName, action.getName(), action);
        }
      }
    }
  }

  // ============================================================================
  // PRECEDENCE AND ASSOCIATIVITY METHODS
  // ============================================================================

  /**
   * Registers a precedence rule.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @param level The precedence level
   * @param description Optional description
   */
  public registerPrecedenceRule(
    grammarName: string,
    operatorName: string,
    level: number,
    description?: string,
  ): void {
    const rule = this.precedenceFactory.createPrecedenceRule(operatorName, level, grammarName, description);
    this.precedenceManager.registerPrecedenceRule(grammarName, operatorName, rule);
  }

  /**
   * Registers an associativity rule.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @param associativity The associativity
   * @param description Optional description
   */
  public registerAssociativityRule(
    grammarName: string,
    operatorName: string,
    associativity: Associativity,
    description?: string,
  ): void {
    const rule = this.precedenceFactory.createAssociativityRule(operatorName, associativity, grammarName, description);
    this.precedenceManager.registerAssociativityRule(grammarName, operatorName, rule);
  }

  /**
   * Gets a precedence rule with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The precedence rule or null if not found
   */
  public getPrecedenceRule(grammarName: string, operatorName: string): PrecedenceRule | null {
    return this.precedenceManager.getPrecedenceRule(grammarName, operatorName);
  }

  /**
   * Gets an associativity rule with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param operatorName The name of the operator
   * @returns The associativity rule or null if not found
   */
  public getAssociativityRule(grammarName: string, operatorName: string): AssociativityRule | null {
    return this.precedenceManager.getAssociativityRule(grammarName, operatorName);
  }

  /**
   * Gets all precedence rules for a grammar.
   * @param grammarName The name of the grammar
   * @returns Map of operator names to precedence rules
   */
  public getAllPrecedenceRules(grammarName: string): Map<string, PrecedenceRule> {
    return this.precedenceManager.getAllPrecedenceRules(grammarName);
  }

  /**
   * Gets all associativity rules for a grammar.
   * @param grammarName The name of the grammar
   * @returns Map of operator names to associativity rules
   */
  public getAllAssociativityRules(grammarName: string): Map<string, AssociativityRule> {
    return this.precedenceManager.getAllAssociativityRules(grammarName);
  }

  /**
   * Compares the precedence of two operators.
   * @param grammarName The name of the grammar
   * @param operator1 The first operator
   * @param operator2 The second operator
   * @returns Comparison result (-1, 0, 1) or null if comparison not possible
   */
  public comparePrecedence(grammarName: string, operator1: string, operator2: string): number | null {
    return this.precedenceManager.comparePrecedence(grammarName, operator1, operator2);
  }

  /**
   * Gets the precedence table for a grammar.
   * @param grammarName The name of the grammar
   * @returns Precedence table with operators grouped by level
   */
  public getPrecedenceTable(grammarName: string): Map<number, OperatorGroup> {
    return this.precedenceManager.getPrecedenceTable(grammarName);
  }

  /**
   * Creates precedence rules from a template.
   * @param grammarName The name of the grammar
   * @param templateName The template name
   * @param operators The operators to apply the template to
   */
  public createPrecedenceRulesFromTemplate(
    grammarName: string,
    templateName: string,
    operators: string[],
  ): void {
    const rules = this.precedenceFactory.createFromPrecedenceTemplate(templateName, grammarName, operators);
    for (const rule of rules) {
      this.precedenceManager.registerPrecedenceRule(grammarName, rule.getOperatorName(), rule);
    }
  }

  /**
   * Creates associativity rules from a template.
   * @param grammarName The name of the grammar
   * @param templateName The template name
   * @param operators The operators to apply the template to
   */
  public createAssociativityRulesFromTemplate(
    grammarName: string,
    templateName: string,
    operators: string[],
  ): void {
    const rules = this.precedenceFactory.createFromAssociativityTemplate(templateName, grammarName, operators);
    for (const rule of rules) {
      this.precedenceManager.registerAssociativityRule(grammarName, rule.getOperatorName(), rule);
    }
  }

  /**
   * Processes precedence and associativity rules from grammar inheritance.
   * @param grammarName The name of the grammar
   */
  public processInheritedPrecedenceRules(grammarName: string): void {
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      return;
    }

    // Process precedence and associativity rules from base grammars
    const baseGrammars = grammar.getBaseGrammars();
    for (const baseGrammarName of baseGrammars) {
      // Inherit precedence rules
      const inheritedPrecedenceRules = this.precedenceFactory.createInheritedPrecedenceRules(
        grammarName,
        baseGrammarName,
        this.precedenceManager,
      );

      for (const rule of inheritedPrecedenceRules) {
        // Only add if not already overridden
        if (!this.precedenceManager.getPrecedenceRule(grammarName, rule.getOperatorName())) {
          this.precedenceManager.registerPrecedenceRule(grammarName, rule.getOperatorName(), rule);
        }
      }

      // Inherit associativity rules
      const inheritedAssociativityRules = this.precedenceFactory.createInheritedAssociativityRules(
        grammarName,
        baseGrammarName,
        this.precedenceManager,
      );

      for (const rule of inheritedAssociativityRules) {
        // Only add if not already overridden
        if (!this.precedenceManager.getAssociativityRule(grammarName, rule.getOperatorName())) {
          this.precedenceManager.registerAssociativityRule(grammarName, rule.getOperatorName(), rule);
        }
      }
    }
  }

  /**
   * Clears all precedence and associativity rules for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of rules removed
   */
  public clearPrecedenceRules(grammarName: string): number {
    return this.precedenceManager.removeAllRules(grammarName);
  }

  /**
   * Clears the precedence cache.
   */
  public clearPrecedenceCache(): void {
    this.precedenceManager.clearCache();
  }

  // ============================================================================
  // ERROR RECOVERY METHODS
  // ============================================================================

  /**
   * Registers an error recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param strategy The error recovery strategy
   */
  public registerErrorRecoveryStrategy(
    grammarName: string,
    strategyName: string,
    strategy: ErrorRecoveryStrategy,
  ): void {
    this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
  }

  /**
   * Gets an error recovery strategy with inheritance resolution.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @returns The error recovery strategy or null if not found
   */
  public getErrorRecoveryStrategy(grammarName: string, strategyName: string): ErrorRecoveryStrategy | null {
    return this.errorRecoveryManager.getStrategy(grammarName, strategyName);
  }

  /**
   * Gets all error recovery strategies for a grammar.
   * @param grammarName The name of the grammar
   * @returns Map of strategy names to error recovery strategies
   */
  public getAllErrorRecoveryStrategies(grammarName: string): Map<string, ErrorRecoveryStrategy> {
    return this.errorRecoveryManager.getAllStrategies(grammarName);
  }

  /**
   * Applies error recovery for a specific error.
   * @param grammarName The name of the grammar
   * @param errorType The type of error
   * @param context The error context
   * @returns The recovery result
   */
  public applyErrorRecovery(grammarName: string, errorType: ErrorType, context: ErrorContext): RecoveryResult {
    return this.errorRecoveryManager.applyRecovery(grammarName, errorType, context);
  }

  /**
   * Creates a synchronization recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param synchronizationTokens The tokens to synchronize on
   */
  public createSynchronizationRecoveryStrategy(
    grammarName: string,
    strategyName: string,
    synchronizationTokens: string[],
  ): void {
    const strategy = new SynchronizationRecoveryStrategy(synchronizationTokens);
    this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
  }

  /**
   * Creates a skip recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param skipCount The number of tokens to skip
   */
  public createSkipRecoveryStrategy(
    grammarName: string,
    strategyName: string,
    skipCount: number = 1,
  ): void {
    const strategy = new SkipRecoveryStrategy(skipCount);
    this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
  }

  /**
   * Creates an insert recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param insertToken The token to insert
   */
  public createInsertRecoveryStrategy(
    grammarName: string,
    strategyName: string,
    insertToken: any,
  ): void {
    const strategy = new InsertRecoveryStrategy(insertToken);
    this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
  }

  /**
   * Creates a replace recovery strategy.
   * @param grammarName The name of the grammar
   * @param strategyName The name of the strategy
   * @param replacementToken The replacement token
   */
  public createReplaceRecoveryStrategy(
    grammarName: string,
    strategyName: string,
    replacementToken: any,
  ): void {
    const strategy = new ReplaceRecoveryStrategy(replacementToken);
    this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
  }

  /**
   * Processes error recovery strategies from grammar inheritance.
   * @param grammarName The name of the grammar
   */
  public processInheritedErrorRecoveryStrategies(grammarName: string): void {
    const grammar = this.grammarContainer.getGrammar(grammarName);
    if (!grammar) {
      return;
    }

    // Process error recovery strategies from base grammars
    const baseGrammars = grammar.getBaseGrammars();
    for (const baseGrammarName of baseGrammars) {
      const baseStrategies = this.errorRecoveryManager.getDirectStrategies(baseGrammarName);

      for (const [strategyName, strategy] of baseStrategies) {
        // Only add if not already overridden
        if (!this.errorRecoveryManager.getStrategy(grammarName, strategyName)) {
          this.errorRecoveryManager.registerStrategy(grammarName, strategyName, strategy);
        }
      }
    }
  }

  /**
   * Clears all error recovery strategies for a grammar.
   * @param grammarName The name of the grammar
   * @returns Number of strategies removed
   */
  public clearErrorRecoveryStrategies(grammarName: string): number {
    return this.errorRecoveryManager.removeAllStrategies(grammarName);
  }

  /**
   * Clears the error recovery cache.
   */
  public clearErrorRecoveryCache(): void {
    this.errorRecoveryManager.clearCache();
  }

  /**
   * Sets a context state in the parser.
   * @param contextName The name of the context
   * @param active Whether the context is active
   */
  public setContextState(contextName: string, active: boolean): void {
    this.stepParser.setContextState(contextName, active);
  }

  /**
   * Gets a context state from the parser.
   * @param contextName The name of the context
   * @returns Whether the context is active
   */
  public getContextState(contextName: string): boolean {
    return this.stepParser.getContextState(contextName);
  }

  /**
   * Clears all loaded grammars.
   */
  public clearGrammars(): void {
    this.grammarContainer.clear();
  }
}

// Import required classes and interfaces
import { GrammarContainer, GrammarError } from './GrammarContainer';
import { GrammarInterpreter } from './GrammarInterpreter';
import { Grammar } from './Grammar';
import { InheritanceResolver, InheritanceValidationResult } from './InheritanceResolver';
import { SemanticActionManager, SemanticAction, SemanticActionContext } from './SemanticActionManager';
import { SemanticActionFactory } from './SemanticActionFactory';
import { ScriptLanguage } from './SemanticActionImplementations';
import { PrecedenceManager, PrecedenceRule, AssociativityRule, Associativity, OperatorGroup } from './PrecedenceManager';
import { PrecedenceFactory } from './PrecedenceFactory';
import {
  ErrorRecoveryManager,
  ErrorRecoveryStrategy,
  ErrorContext,
  RecoveryResult,
  ErrorType,
  SynchronizationRecoveryStrategy,
  SkipRecoveryStrategy,
  InsertRecoveryStrategy,
  ReplaceRecoveryStrategy,
} from './ErrorRecoveryManager';
import { StepParser } from './StepParser';
import { ProductionMatch } from './ProductionMatch';
import { SourceCodeContainer } from './SourceCodeContainer';
