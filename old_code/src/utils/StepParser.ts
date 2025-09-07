/**
 * Unified StepParser with integrated zero-copy, context-aware, and optimized capabilities.
 * Maintains backward compatibility with the original StepParser API while providing
 * advanced features through zero-copy memory management, deep context integration,
 * and built-in optimizations.
 */

// Memory infrastructure imports
import { MemoryArena } from '../memory/arena/MemoryArena';
import { StringInterner } from '../memory/strings/StringInterner';
import { ObjectPool, PoolableObject, ObjectFactory } from '../memory/pools/ObjectPool';

// Context integration imports
import { ContextInfo, ScopeInfo, SymbolInfo } from '../context/ContextAwareParser';
import { SymbolTable } from '../context/SymbolTable';
import { StepParsingContextAdapter, StepParsingContextSnapshot } from './ContextIntegration';
import { ZeroCopyASTNodeFactory, PoolableZeroCopyASTNode } from './ZeroCopyASTNodeFactory';

// Optimization imports
import { IncrementalParser } from '../optimization/incremental/IncrementalParser';
import { ContextCache } from '../optimization/caching/ContextCache';
import { PathPredictor } from '../optimization/PathPredictor';
import { MemoizationCache } from '../optimization/memoization/Memoization';

// Standard API imports (preserved for compatibility)
import { Grammar } from './Grammar';
import { LexerPath } from './LexerPath';
import { ParserPath } from './ParserPath';
import { Terminal } from './Terminal';
import { ProductionMatch } from './ProductionMatch';
import { Token } from './Token';
import { IProductionPart, ProductionPartType } from './IProductionPart';
import { Production } from './Production';
import { IParserLexerSourceContainer } from './IParserLexerSource';
import { StepLexer } from './StepLexer';
import { LexerOptions } from './LexerOptions';

/**
 * Re-export context snapshot from integration layer
 */
type ContextSnapshot = StepParsingContextSnapshot;

/**
 * Pooled parser path with context awareness and zero-copy optimization
 */
class ContextAwareParserPath extends ParserPath implements PoolableObject {
  private _inUse: boolean = false;

  // Context integration
  private _contextSnapshot: ContextSnapshot | null = null;
  private _scopeStack: ScopeInfo[] = [];
  private _symbolContext: Map<string, SymbolInfo> = new Map();

  // Zero-copy optimization
  private _astNodes: PoolableZeroCopyASTNode[] = [];
  private _score: number = 0;
  private _confidence: number = 1.0;

  constructor() {
    super(0, 0, 0); // Call parent constructor with default values
  }

  // PoolableObject implementation
  reset(): void {
    this.setParserPathId(0);
    this.setLexerPathId(0);
    this.setPosition(0);
    this.getActiveProductions().length = 0; // Clear the array
    this.getActiveMatches().length = 0; // Clear the array
    this._contextSnapshot = null;
    this._scopeStack = [];
    this._symbolContext.clear();
    this._astNodes = [];
    this._score = 0;
    this._confidence = 1.0;
    this._inUse = false;
  }

  isInUse(): boolean {
    return this._inUse;
  }

  setInUse(inUse: boolean): void {
    this._inUse = inUse;
  }

  // Context-aware extensions
  getContextSnapshot(): ContextSnapshot | null {
    return this._contextSnapshot;
  }

  setContextSnapshot(snapshot: ContextSnapshot): void {
    this._contextSnapshot = snapshot;
  }

  getScopeStack(): ScopeInfo[] {
    return this._scopeStack;
  }

  getSymbolContext(): Map<string, SymbolInfo> {
    return this._symbolContext;
  }

  getScore(): number {
    return this._score;
  }

  setScore(score: number): void {
    this._score = score;
  }

  getConfidence(): number {
    return this._confidence;
  }

  setConfidence(confidence: number): void {
    this._confidence = confidence;
  }
}

/**
 * Factory for creating pooled parser paths
 */
class ParserPathFactory implements ObjectFactory<ContextAwareParserPath> {
  create(): ContextAwareParserPath {
    return new ContextAwareParserPath();
  }

  reset(path: ContextAwareParserPath): void {
    path.reset();
  }

  validate(path: ContextAwareParserPath): boolean {
    return path.getParserPathId() !== 0;
  }
}

/**
 * Unified StepParser class with zero-copy, context-aware, and optimized capabilities.
 * Maintains the same public API as the original StepParser for backward compatibility.
 */
export class StepParser {
  // Zero-copy infrastructure (private, hidden from users)
  private arena: MemoryArena;
  private stringInterner: StringInterner;
  private pathPool: ObjectPool<ContextAwareParserPath>;
  private nodePool: ObjectPool<PoolableZeroCopyASTNode>;

  // Context integration (private, hidden from users)
  private contextAdapter: StepParsingContextAdapter;
  private symbolTable: SymbolTable;
  private contextCache: ContextCache;

  // Optimization components (private, hidden from users)
  private incrementalEngine: IncrementalParser;
  private pathPredictor: PathPredictor;
  private memoization: MemoizationCache<any>;

  // Public API preserved for backward compatibility
  private activeGrammarName: string;
  private grammar: Grammar | null;
  private activeProductionPartsForLexerPath: Map<number, IProductionPart[]>;
  private parserPaths: Map<number, ContextAwareParserPath>;
  private maxParserPathId: number;
  private contextStates: Map<string, boolean>;

  // Callback support (integrated from CallbackStepParser)
  private callbackRegistry: Map<string, Function>;
  private callbackContext: Record<string, any>;

  /**
   * Creates a new StepParser instance with unified capabilities.
   * Maintains the same constructor signature for backward compatibility.
   */
  constructor() {
    // Initialize zero-copy infrastructure
    this.initializeZeroCopyInfrastructure();

    // Initialize context integration
    this.initializeContextIntegration();

    // Initialize optimization components
    this.initializeOptimizationComponents();

    // Preserve existing initialization logic
    this.activeGrammarName = '';
    this.grammar = null;
    this.activeProductionPartsForLexerPath = new Map<number, IProductionPart[]>();
    this.parserPaths = new Map<number, ContextAwareParserPath>();
    this.maxParserPathId = 0;
    this.contextStates = new Map<string, boolean>();

    // Initialize callback support
    this.callbackRegistry = new Map<string, Function>();
    this.callbackContext = {};

    this.reset();
  }

  /**
   * Initializes the zero-copy infrastructure.
   * Creates memory arena, string interner, and object pools.
   */
  private initializeZeroCopyInfrastructure(): void {
    // Create memory arena with 4MB initial size (proven beneficial for complex parsing)
    this.arena = new MemoryArena(4 * 1024 * 1024);

    // Create string interner for deduplication (proven beneficial)
    this.stringInterner = new StringInterner(this.arena);

    // Create object pools for parser paths and AST nodes
    const pathFactory = new ParserPathFactory();
    this.pathPool = new ObjectPool(pathFactory, this.arena, 200, 2000);

    // Create AST node pool with proper factory
    const nodeFactory = new ZeroCopyASTNodeFactory(this.arena, this.stringInterner);
    this.nodePool = new ObjectPool(nodeFactory, this.arena, 500, 5000);
  }

  /**
   * Initializes context integration components.
   */
  private initializeContextIntegration(): void {
    // Create context adapter for step-by-step parsing
    this.contextAdapter = new StepParsingContextAdapter();
    
    // Create symbol table with zero-copy optimization
    this.symbolTable = new SymbolTable();
    
    // Create context cache with appropriate configuration
    this.contextCache = new ContextCache({
      maxCacheSize: 2000,
      maxMemoryUsage: 100 * 1024 * 1024, // 100MB for parsing contexts
      enableLRU: true,
      enableCompression: true,
    });
  }

  /**
   * Initializes optimization components.
   */
  private initializeOptimizationComponents(): void {
    // Create incremental parsing engine
    this.incrementalEngine = new IncrementalParser(this);

    // Create path predictor for optimization
    this.pathPredictor = new PathPredictor();

    // Create advanced memoization with context awareness
    this.memoization = new MemoizationCache<any>();
  }

  // PUBLIC API - Maintained for backward compatibility

  /**
   * Gets the active grammar name.
   * Maintains exact same signature as original StepParser.
   */
  public getActiveGrammarName(): string {
    return this.activeGrammarName;
  }

  /**
   * Sets the active grammar for parsing.
   * Maintains exact same signature as original StepParser.
   * Enhanced with context-aware terminal filtering.
   */
  public setActiveGrammar(grammar: Grammar): void {
    this.grammar = grammar;
    this.activeGrammarName = grammar.getName ? grammar.getName() : 'Unknown Grammar';

    // Update context adapter with new grammar (if available)
    if (this.contextAdapter) {
      this.contextAdapter.setActiveGrammar(grammar);
    }

    // Clear existing parser paths as they're specific to the previous grammar
    this.reset();
  }

  /**
   * Gets valid terminals for a lexer path.
   * Maintains exact same signature as original StepParser.
   * Enhanced with context-aware terminal filtering.
   * @param lexerPathId The ID of the lexer path
   * @returns The valid terminals
   */
  public getValidTerminalsForLexerPath(lexerPathId: number): Terminal[] {
    if (!this.activeProductionPartsForLexerPath.has(lexerPathId)) {
      if (this.grammar) {
        const startTerminals = this.grammar.getValidStartTerminals();

        // Enhanced: Filter terminals based on current context
        return this.filterTerminalsWithContext(startTerminals, lexerPathId);
      }
      return [];
    }

    const parts = this.activeProductionPartsForLexerPath.get(lexerPathId) || [];
    const terminals: Terminal[] = [];

    for (const part of parts) {
      if (part.getType() === ProductionPartType.Terminal) {
        terminals.push(part as Terminal);
      }
    }

    // Enhanced: Filter terminals based on current context
    return this.filterTerminalsWithContext(terminals, lexerPathId);
  }

  // ===== CALLBACK SUPPORT METHODS =====
  // Integrated from CallbackStepParser for unified functionality

  /**
   * Registers a callback for a specific production.
   * @param productionName The name of the production
   * @param callback The callback function to execute
   */
  public registerCallback(productionName: string, callback: Function): void {
    this.callbackRegistry.set(productionName, callback);
  }

  /**
   * Removes a callback for a specific production.
   * @param productionName The name of the production
   */
  public unregisterCallback(productionName: string): void {
    this.callbackRegistry.delete(productionName);
  }

  /**
   * Sets the callback context.
   * @param context The context to set
   */
  public setCallbackContext(context: Record<string, any>): void {
    this.callbackContext = { ...this.callbackContext, ...context };
  }

  /**
   * Gets the callback context.
   * @returns The current callback context
   */
  public getCallbackContext(): Record<string, any> {
    return { ...this.callbackContext };
  }

  /**
   * Clears all registered callbacks.
   */
  public clearCallbacks(): void {
    this.callbackRegistry.clear();
  }

  /**
   * Clears the callback context.
   */
  public clearCallbackContext(): void {
    this.callbackContext = {};
  }

  /**
   * Parses input using the specified grammar.
   * Maintains exact same signature as original StepParser.
   * Enhanced with context awareness and optimization.
   * @param grammarName The name of the grammar to use
   * @param sourceLinesContainer The source lines container
   * @returns The parse results
   */
  public async parse(
    grammarName: string,
    sourceLinesContainer: IParserLexerSourceContainer,
  ): Promise<ProductionMatch[]> {
    if (!this.grammar || this.grammar.getName() !== grammarName) {
      throw new Error(`Grammar ${grammarName} not set as active`);
    }

    this.reset();

    // Enhanced: Initialize context for parsing
    await this.contextAdapter.initializeForParsing(sourceLinesContainer);

    // Create lexer (using the unified StepLexer)
    const lexerOptions = new LexerOptions();
    const lexer = new StepLexer(this, lexerOptions, sourceLinesContainer);

    // Enhanced: Process tokens with context awareness and optimization
    const tokenGenerator = lexer.nextTokens();
    let result = tokenGenerator.next();

    while (!result.done) {
      const tokens = result.value;
      this.processTokensWithContext(tokens);
      result = tokenGenerator.next();
    }

    // Collect results (maintaining backward compatibility)
    const matches: ProductionMatch[] = [];
    for (const path of this.parserPaths.values()) {
      matches.push(...path.getActiveMatches());
    }

    return matches;
  }

  /**
   * Sets a context state.
   * Maintains exact same signature as original StepParser.
   * Enhanced with deep context integration.
   * @param contextName The name of the context
   * @param active Whether the context is active
   */
  public setContextState(contextName: string, active: boolean): void {
    this.contextStates.set(contextName, active);

    // Enhanced: Update context adapter with state change
    this.contextAdapter.updateContextState(contextName, active);
  }

  /**
   * Gets a context state.
   * Maintains exact same signature as original StepParser.
   * Enhanced with deep context integration.
   * @param contextName The name of the context
   * @returns Whether the context is active
   */
  public getContextState(contextName: string): boolean {
    return this.contextStates.get(contextName) || false;
  }

  // PRIVATE METHODS - Enhanced implementations

  /**
   * Resets the parser state.
   * Enhanced with zero-copy cleanup and context reset.
   */
  private reset(): void {
    // Clear existing paths and return them to pool
    for (const path of this.parserPaths.values()) {
      this.pathPool.release(path);
    }
    this.parserPaths.clear();

    this.activeProductionPartsForLexerPath.clear();
    this.maxParserPathId = 0;
    this.contextStates.clear();

    // Enhanced: Reset context and optimization components (with null checks)
    if (this.contextAdapter) {
      this.contextAdapter.reset();
    }
    this.contextCache.clearCache();
    this.memoization.clear();
  }

  /**
   * Filters terminals based on current context and lexer path.
   * Uses lexer path to maintain path-specific context for ambiguous grammar handling.
   */
  private filterTerminalsWithContext(terminals: Terminal[], lexerPathId: number): Terminal[] {
    const currentContext = this.contextAdapter.getCurrentContext();

    // Get the parser path associated with this lexer path for path-specific filtering
    const associatedParserPath = Array.from(this.parserPaths.values())
      .find(path => path.getLexerPathId() === lexerPathId);

    return terminals.filter(terminal => {
      // Use basic context validation
      const isValidInContext = this.contextAdapter.isTerminalValidInContext(terminal, currentContext);

      // Add path-specific validation if we have an associated parser path
      if (associatedParserPath && associatedParserPath.getActiveProductions().length > 0) {
        // Check if terminal is compatible with current productions in this path
        const activeProductions = associatedParserPath.getActiveProductions();
        const isCompatibleWithPath = activeProductions.some(prod =>
          this.contextAdapter.isProductionValidInContext(prod, currentContext, terminal),
        );
        return isValidInContext && isCompatibleWithPath;
      }

      return isValidInContext;
    });
  }

  /**
   * Processes tokens with context awareness and optimization.
   * Enhanced version of the original processTokens method.
   */
  private processTokensWithContext(tokens: Token[]): void {
    for (const token of tokens) {
      this.processTokenWithContext(token);
    }
  }

  /**
   * Processes a single token with context awareness.
   * Enhanced version of the original processToken method.
   */
  private processTokenWithContext(token: Token): void {
    const lexerPathId = token.getLexerPathId();

    // Handle special lexer tokens (preserved from original)
    if (token.getTerminal().getName() === 'LEXERPATH_REMOVED') {
      this.handleLexerPathRemoval(lexerPathId, token.getValue());
      return;
    }

    if (token.getTerminal().getName() === 'LEXERPATH_MERGE') {
      this.handleLexerPathMerge(lexerPathId, parseInt(token.getValue()));
      return;
    }

    // Get parser paths for this lexer path
    const parserPathsToProcess: ContextAwareParserPath[] = [];

    for (const path of this.parserPaths.values()) {
      if (path.getLexerPathId() === lexerPathId) {
        parserPathsToProcess.push(path);
      }
    }

    // If no parser paths exist for this lexer path, create one
    if (parserPathsToProcess.length === 0) {
      const newPath = this.createContextAwareParserPath(lexerPathId, 0);
      parserPathsToProcess.push(newPath);
    }

    // Enhanced: Process token for each parser path with context awareness
    for (const path of parserPathsToProcess) {
      this.processTokenForContextAwarePath(path, token);
    }
  }

  /**
   * Creates a new context-aware parser path using object pooling.
   */
  private createContextAwareParserPath(lexerPathId: number, position: number): ContextAwareParserPath {
    const path = this.pathPool.acquire();
    path.setInUse(true);
    path.setParserPathId(++this.maxParserPathId);
    path.setLexerPathId(lexerPathId);
    path.setPosition(position);

    // Initialize context snapshot
    const contextSnapshot: ContextSnapshot = {
      scopeStack: this.contextAdapter.getCurrentScopes(),
      symbolContext: new Map(this.symbolTable.getAllSymbols()),
      parseState: this.contextAdapter.getCurrentParseState(),
      position: this.contextAdapter.getCurrentPosition(),
      hash: this.contextAdapter.computeContextHash(),
    };
    path.setContextSnapshot(contextSnapshot);

    this.parserPaths.set(path.getParserPathId(), path);
    return path;
  }

  /**
   * Processes a token for a specific context-aware parser path.
   * Enhanced version with context awareness and optimization.
   */
  private processTokenForContextAwarePath(path: ContextAwareParserPath, token: Token): void {
    // Update context before processing
    this.contextAdapter.updateContext(token, path.getContextSnapshot());

    // Get context-informed parsing options
    const contextInfo = this.contextAdapter.getCurrentContext();
    const terminal = token.getTerminal();
    const activeProductions = path.getActiveProductions();

    // If no active productions, start with grammar's start productions
    if (activeProductions.length === 0) {
      if (this.grammar) {
        const startProductions = this.grammar.getStartProductions();
        for (const prod of startProductions) {
          path.addActiveProduction(prod);
        }
      }
    }

    // Enhanced: Try to match token against active productions with context awareness
    const matchingProductions: Production[] = [];

    for (const prod of activeProductions) {
      if (this.isProductionValidInContext(prod, contextInfo, terminal)) {
        const parts = prod.getParts();
        if (parts.length > 0) {
          const firstPart = parts[0];
          if (firstPart.getType() === ProductionPartType.Terminal) {
            const terminalPart = firstPart as Terminal;
            if (terminalPart.getName() === terminal.getName()) {
              matchingProductions.push(prod);
            }
          }
        }
      }
    }

    // Enhanced: Handle results with context-aware optimization
    if (matchingProductions.length === 0) {
      this.handleContextAwareParsingError(path, token, contextInfo);
      return;
    }

    if (matchingProductions.length > 1) {
      this.handleContextAwareAmbiguity(path, token, matchingProductions, contextInfo);
      return;
    }

    // Process the single matching production
    this.processMatchingProductionWithContext(path, token, matchingProductions[0], contextInfo);
  }

  /**
   * Checks if a production is valid in the current context.
   */
  private isProductionValidInContext(production: Production, contextInfo: ContextInfo, terminal: Terminal): boolean {
    // Use context adapter to validate production
    return this.contextAdapter.isProductionValidInContext(production, contextInfo, terminal);
  }

  /**
   * Handles parsing errors with context awareness.
   */
  private handleContextAwareParsingError(
    path: ContextAwareParserPath,
    token: Token,
    contextInfo: ContextInfo,
  ): void {
    // Enhanced error recovery using context information
    const recoveryStrategy = this.contextAdapter.getErrorRecoveryStrategy(token, contextInfo);

    if (recoveryStrategy.canRecover) {
      // Apply recovery strategy
      this.applyErrorRecovery(path, token, recoveryStrategy);
    } else {
      // Fallback to original behavior
      this.parserPaths.delete(path.getParserPathId());
      this.pathPool.release(path);
    }
  }

  /**
   * Handles ambiguity with context awareness.
   */
  private handleContextAwareAmbiguity(
    path: ContextAwareParserPath,
    token: Token,
    matchingProductions: Production[],
    contextInfo: ContextInfo,
  ): void {
    // Enhanced: Use context to rank productions
    const rankedProductions = this.contextAdapter.rankProductionsByContext(matchingProductions, contextInfo);

    // Use the highest-ranked production for the current path
    this.processMatchingProductionWithContext(path, token, rankedProductions[0], contextInfo);

    // Create new paths for other high-ranking productions
    for (let i = 1; i < Math.min(rankedProductions.length, 3); i++) { // Limit to top 3 to avoid explosion
      const newPath = this.createContextAwareParserPath(path.getLexerPathId(), path.getPosition());

      // Copy context from original path
      newPath.setContextSnapshot(path.getContextSnapshot());

      // Copy active productions from original path
      for (const prod of path.getActiveProductions()) {
        if (prod !== rankedProductions[i]) {
          newPath.addActiveProduction(prod);
        }
      }

      // Copy active matches from original path
      for (const match of path.getActiveMatches()) {
        newPath.addActiveMatch(match);
      }

      // Process the production for this new path
      this.processMatchingProductionWithContext(newPath, token, rankedProductions[i], contextInfo);
    }
  }

  /**
   * Processes a matching production with context awareness.
   */
  private processMatchingProductionWithContext(
    path: ContextAwareParserPath,
    token: Token,
    production: Production,
    contextInfo: ContextInfo,
  ): void {
    // Create a match for this token
    const match = new ProductionMatch(
      production,
      token.getValue(),
      path.getPosition(),
      path.getPosition() + token.getValue().length,
    );

    // Update path position
    path.setPosition(path.getPosition() + token.getValue().length);

    // Add match to path
    path.addActiveMatch(match);

    // Enhanced: Update context with production information
    this.contextAdapter.updateContextWithProduction(production, token, contextInfo);

    // Enhanced: Update symbol table if this production defines symbols
    this.updateSymbolTableWithProduction(production, token, contextInfo);

    // Enhanced: Execute callbacks with rich context (integrated from CallbackStepParser)
    this.executeCallbacksForProduction(production, token, path, contextInfo);

    // Execute legacy callback if exists (preserved for backward compatibility)
    if (production.getCallback()) {
      production.executeCallback(token.getValue(), this.contextStates, path.getPosition());
    }

    // Update active productions
    path.getActiveProductions().splice(path.getActiveProductions().indexOf(production), 1);

    // If production has more parts, add them to active parts for this lexer path
    const parts = production.getParts();
    if (parts.length > 1) {
      const remainingParts = parts.slice(1);
      this.activeProductionPartsForLexerPath.set(path.getLexerPathId(), remainingParts);
    } else {
      // Production completed, remove from active parts
      this.activeProductionPartsForLexerPath.delete(path.getLexerPathId());
    }

    // Enhanced: Update path score based on context confidence
    const confidence = this.contextAdapter.computeProductionConfidence(production, contextInfo);
    path.setConfidence(confidence);
    path.setScore(path.getScore() + confidence);
  }

  /**
   * Gets valid terminals for the current lexer path context.
   */
  public getValidTerminals(lexerPath: LexerPath): Terminal[] {
    // Get active productions from the parser path
    const path = Array.from(this.parserPaths.values()).find(p => p.getLexerPathId() === lexerPath.getLexerPathId());
    if (!path || !path.getActiveProductions()) {
      return [];
    }

    const validTerminals: Terminal[] = [];
    for (const production of path.getActiveProductions()) {
      const parts = production.getParts();
      if (parts.length > 0) {
        const firstPart = parts[0];
        if (firstPart.getType() === ProductionPartType.Terminal) {
          validTerminals.push(firstPart as Terminal);
        }
      }
    }

    return validTerminals;
  }

  /**
   * Updates the symbol table with production information.
   */
  private updateSymbolTableWithProduction(
    production: Production,
    token: Token,
    contextInfo: ContextInfo,
  ): void {
    const symbolInfo = this.contextAdapter.extractSymbolInfo(production, token, contextInfo);
    if (symbolInfo) {
      this.symbolTable.defineSymbol(symbolInfo);
    }
  }

  /**
   * Executes registered callbacks for a production with rich context.
   * Integrated from CallbackStepParser functionality.
   */
  private executeCallbacksForProduction(
    production: Production,
    token: Token,
    path: ContextAwareParserPath,
    contextInfo: ContextInfo,
  ): void {
    const productionName = production.getName();
    const callback = this.callbackRegistry.get(productionName);

    if (callback) {
      try {
        // Extract captures from token if available
        const captures = (token as any).getCaptures ? (token as any).getCaptures() : [];

        // Create rich callback context
        const callbackContextInfo = {
          token: token.getValue(),
          position: path.getPosition(),
          captures,
          contextInfo,
          symbolTable: this.symbolTable,
          customContext: this.callbackContext,
          production: productionName,
          grammarName: this.activeGrammarName,
        };

        // Execute callback with rich context
        callback(callbackContextInfo);

      } catch (error) {
    // eslint-disable-next-line no-console
        console.error(`Callback error for production ${productionName}:`, error);
      }
    }
  }

  /**
   * Applies error recovery strategy.
   */
  private applyErrorRecovery(path: ContextAwareParserPath, token: Token, recoveryStrategy: any): void {
    // Apply recovery strategy based on the strategy type
    if (recoveryStrategy.strategy === 'skip') {
    // eslint-disable-next-line no-console
      console.warn(`Skipping token ${token.getValue()} at position ${path.getPosition()}`);
      // Skip the token by advancing position
      path.setPosition(path.getPosition() + token.getValue().length);
    } else if (recoveryStrategy.strategy === 'backtrack') {
    // eslint-disable-next-line no-console
      console.warn(`Backtracking from token ${token.getValue()} at position ${path.getPosition()}`);
      // Simple backtrack - remove this path
      this.parserPaths.delete(path.getParserPathId());
      this.pathPool.release(path);
    } else {
      // Default behavior for other strategies
    // eslint-disable-next-line no-console
      console.warn(
        `Applying ${recoveryStrategy.strategy} recovery for token ${token.getValue()} ` +
        `at position ${path.getPosition()}`,
      );
    }
  }

  /**
   * Handles lexer path removal (preserved from original).
   */
  private handleLexerPathRemoval(lexerPathId: number, reason: string): void {
    // eslint-disable-next-line no-console
    console.debug(`Removing lexer path ${lexerPathId} - reason: ${reason}`);

    const pathsToRemove: number[] = [];

    for (const [id, path] of this.parserPaths.entries()) {
      if (path.getLexerPathId() === lexerPathId) {
        pathsToRemove.push(id);
      }
    }

    for (const [id, path] of this.parserPaths.entries()) {
      if (path.getLexerPathId() === lexerPathId) {
        pathsToRemove.push(id);
      }
    }

    for (const id of pathsToRemove) {
      const path = this.parserPaths.get(id);
      if (path) {
        this.parserPaths.delete(id);
        this.pathPool.release(path);
      }
    }

    this.activeProductionPartsForLexerPath.delete(lexerPathId);
  }

  /**
   * Handles lexer path merge (preserved from original).
   */
  private handleLexerPathMerge(lexerPathId: number, targetLexerPathId: number): void {
    // Update all parser paths that reference the merged lexer path
    for (const path of this.parserPaths.values()) {
      if (path.getLexerPathId() === lexerPathId) {
        path.setLexerPathId(targetLexerPathId);
      }
    }

    // Merge active production parts
    const sourceParts = this.activeProductionPartsForLexerPath.get(lexerPathId);
    const targetParts = this.activeProductionPartsForLexerPath.get(targetLexerPathId);

    if (sourceParts && targetParts) {
      // Merge the parts (simple concatenation for now)
      this.activeProductionPartsForLexerPath.set(targetLexerPathId, [...targetParts, ...sourceParts]);
    } else if (sourceParts) {
      this.activeProductionPartsForLexerPath.set(targetLexerPathId, sourceParts);
    }

    this.activeProductionPartsForLexerPath.delete(lexerPathId);
  }
}

// Export the unified parser as the default StepParser for backward compatibility
// Export as default for different import styles
export default StepParser;

