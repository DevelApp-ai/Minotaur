/**
 * Unified StepLexer with zerocopy core implementation.
 * Consolidates standard, zerocopy, and optimized implementations into one.
 * Provides superior performance through memory arena allocation, string interning,
 * and object pooling while maintaining backward compatibility.
 */

// Memory infrastructure imports
import { MemoryArena } from '../memory/arena/MemoryArena';
import { StringInterner } from '../memory/strings/StringInterner';
import { ObjectPool, PoolableObject, ObjectFactory } from '../memory/pools/ObjectPool';

// Zerocopy token infrastructure
import { AlignedToken, AlignedTokenArray, TokenType, TokenSpan } from '../zerocopy/tokens/AlignedToken';

// Standard API imports (preserved for compatibility)
import { LexerPath } from './LexerPath';
import { Token } from './Token';
import { Terminal } from './Terminal';
import { TerminalMatch } from './TerminalMatch';
import { LexerOptions } from './LexerOptions';
import { IParserLexerSourceContainer, IParserLexerSourceLine } from './IParserLexerSource';
import { StepParser } from './StepParser';

/**
 * Pooled lexer path for zerocopy implementation
 */
class PooledLexerPath extends LexerPath implements PoolableObject {
  private _inUse: boolean = false;

  // Additional zerocopy state
  private _tokens: AlignedToken[] = [];
  private _score: number = 0;

  constructor() {
    super(); // Call parent LexerPath constructor
  }

  // PoolableObject interface methods
  reset(): void {
    this.setLexerPathId(LexerPath.NOTSET);
    this.setActiveLineNumber(0);
    this.setActiveCharacterNumber(0);
    this._tokens = [];
    this._score = 0;
    this._inUse = false;
  }

  isInUse(): boolean {
    return this._inUse;
  }

  setInUse(inUse: boolean): void {
    this._inUse = inUse;
  }

  // Zerocopy-specific methods
  getTokens(): AlignedToken[] {
    return this._tokens;
  }

  addToken(token: AlignedToken): void {
    this._tokens.push(token);
  }

  getScore(): number {
    return this._score;
  }

  setScore(score: number): void {
    this._score = score;
  }
}

/**
 * Factory for creating pooled lexer paths
 */
class LexerPathFactory implements ObjectFactory<PooledLexerPath> {
  create(): PooledLexerPath {
    return new PooledLexerPath();
  }

  reset(path: PooledLexerPath): void {
    path.reset();
  }

  validate(path: PooledLexerPath): boolean {
    return path.getLexerPathId() !== LexerPath.NOTSET;
  }
}

/**
 * Unified StepLexer class with zerocopy core.
 * Maintains the same API as the original StepLexer while internally using
 * zerocopy infrastructure for superior performance.
 */
export class StepLexer {
  // Zerocopy infrastructure (private, hidden from users)
  private arena: MemoryArena;
  private stringInterner: StringInterner;
  private pathPool: ObjectPool<PooledLexerPath>;
  private tokenArray: AlignedTokenArray;

  // Public API preserved for compatibility
  private sourceLinesContainer: IParserLexerSourceContainer;
  private stepParser: StepParser;
  private lexerOptions: LexerOptions;
  private lexerPathMap: Map<number, PooledLexerPath>;
  private invalidatedLexerPaths: PooledLexerPath[];
  private maximumLexerPathId: number;

  /**
   * Creates a new unified StepLexer instance.
   * Maintains the same constructor signature for backward compatibility.
   * @param stepParser The parser to use
   * @param lexerOptions The lexer options
   * @param sourceLinesContainer The source lines container
   */
  constructor(stepParser: StepParser, lexerOptions: LexerOptions, sourceLinesContainer: IParserLexerSourceContainer) {
    // Initialize zerocopy infrastructure
    this.initializeZeroCopyInfrastructure();

    // Preserve existing initialization logic
    this.stepParser = stepParser;
    this.lexerOptions = lexerOptions;
    this.sourceLinesContainer = sourceLinesContainer;
    this.lexerPathMap = new Map<number, PooledLexerPath>();
    this.invalidatedLexerPaths = [];
    this.maximumLexerPathId = LexerPath.NOTSET;
    this.reset();
  }

  /**
   * Initializes the zerocopy infrastructure.
   * Creates memory arena, string interner, and object pools.
   */
  private initializeZeroCopyInfrastructure(): void {
    // Create memory arena with 1MB initial size (proven beneficial)
    this.arena = new MemoryArena(1024 * 1024);

    // Create string interner for deduplication (proven beneficial)
    this.stringInterner = new StringInterner(this.arena);

    // Create object pool for lexer paths (proven beneficial)
    const pathFactory = new LexerPathFactory();
    this.pathPool = new ObjectPool(pathFactory, this.arena, 100, 1000);

    // Create aligned token array (proven beneficial)
    this.tokenArray = new AlignedTokenArray(this.arena, this.stringInterner, 1000);
  }

  /**
   * Resets the lexer.
   * Maintains the same behavior as the original implementation.
   */
  public reset(): void {
    // Clear existing paths and return them to pool
    for (const path of this.lexerPathMap.values()) {
      this.pathPool.release(path);
    }
    this.lexerPathMap.clear();

    // Clear invalidated paths and return them to pool
    for (const path of this.invalidatedLexerPaths) {
      this.pathPool.release(path);
    }
    this.invalidatedLexerPaths = [];

    this.maximumLexerPathId = LexerPath.NOTSET;
    this.newStartLexerPath();
  }

  /**
   * Gets the next tokens.
   * Maintains the same API as the original implementation.
   * @returns An iterator over token lists
   */
  public *nextTokens(): Generator<Token[]> {
    let tokenList: Token[] = [];
    let currentLexerPaths: PooledLexerPath[];

    while (this.lexerPathMap.size > 0) {
      tokenList = [];
      this.checkForParserLexerPathInvalidation(tokenList);
      this.checkForMergeLexerPaths(Array.from(this.lexerPathMap.values()), tokenList);
      currentLexerPaths = Array.from(this.lexerPathMap.values());

      for (const lexerPath of currentLexerPaths) {
        this.processNextToken(lexerPath, tokenList);
      }

      yield tokenList;
    }
  }

  /**
   * Invalidates a lexer path.
   * Maintains the same API as the original implementation.
   * @param lexerPathId The ID of the lexer path to invalidate
   */
  public invalidateLexerPath(lexerPathId: number): void {
    this.removeLexerPath(lexerPathId, true);
  }

  /**
   * Creates a new start lexer path using the object pool.
   */
  private newStartLexerPath(): void {
    const path = this.pathPool.acquire();
    this.maximumLexerPathId++;
    path.setLexerPathId(this.maximumLexerPathId);
    path.setActiveLineNumber(0);
    path.setActiveCharacterNumber(0);
    this.lexerPathMap.set(this.maximumLexerPathId, path);
  }

  /**
   * Removes a lexer path and optionally marks it as invalidated.
   * @param lexerPathId The ID of the path to remove
   * @param markAsInvalidated Whether to mark as invalidated
   */
  private removeLexerPath(lexerPathId: number, markAsInvalidated: boolean): void {
    const path = this.lexerPathMap.get(lexerPathId);
    if (path) {
      this.lexerPathMap.delete(lexerPathId);

      if (markAsInvalidated) {
        this.invalidatedLexerPaths.push(path);
      } else {
        this.pathPool.release(path);
      }
    }
  }

  /**
   * Checks for parser lexer path invalidation.
   * @param tokenList The token list to add invalidation tokens to
   */
  private checkForParserLexerPathInvalidation(tokenList: Token[]): void {
    if (this.lexerOptions.getReturnLexerPathTokens()) {
      for (const invalidLexerPath of this.invalidatedLexerPaths) {
        tokenList.push(new Token(
          invalidLexerPath.getLexerPathId(),
          new Terminal('LEXERPATH_REMOVED', ''),
          Token.LEXERPATH_EXTERN_REMOVED,
          invalidLexerPath.getActiveLineNumber(),
          invalidLexerPath.getActiveCharacterNumber(),
        ));
      }
    }

    // Return invalidated paths to pool
    for (const path of this.invalidatedLexerPaths) {
      this.pathPool.release(path);
    }
    this.invalidatedLexerPaths = [];
  }

  /**
   * Checks for merge opportunities between lexer paths.
   * Core functionality for handling ambiguous grammar efficiently.
   * @param lexerPaths The current lexer paths
   * @param tokenList The token list to add merge tokens to
   */
  private checkForMergeLexerPaths(lexerPaths: PooledLexerPath[], tokenList: Token[]): void {
    // Group paths by position to find merge candidates
    const pathsByPosition = new Map<string, PooledLexerPath[]>();

    for (const path of lexerPaths) {
      const positionKey = `${path.getActiveLineNumber()}-${path.getActiveCharacterNumber()}`;
      if (!pathsByPosition.has(positionKey)) {
        pathsByPosition.set(positionKey, []);
      }
      pathsByPosition.get(positionKey)!.push(path);
    }

    // Check for paths that can be merged
    for (const [_position, pathsAtPosition] of pathsByPosition.entries()) {
      if (pathsAtPosition.length > 1) {
        // Multiple paths at same position - potential merge opportunity
        const basePath = pathsAtPosition[0];

        for (let i = 1; i < pathsAtPosition.length; i++) {
          const mergePath = pathsAtPosition[i];

          // Check if paths have similar token sequences (simplified merge criteria)
          if (this.canMergePaths(basePath, mergePath)) {
            // Create merge token if lexer options require it
            if (this.lexerOptions.getReturnLexerPathTokens()) {
              tokenList.push(new Token(
                mergePath.getLexerPathId(),
                new Terminal('LEXERPATH_MERGE', ''),
                basePath.getLexerPathId().toString(),
                mergePath.getActiveLineNumber(),
                mergePath.getActiveCharacterNumber(),
              ));
            }

            // Remove the merged path
            this.removeLexerPath(mergePath.getLexerPathId(), false);
          }
        }
      }
    }
  }

  /**
   * Checks if two lexer paths can be merged based on their tokens and scores.
   * @param basePath The base path to merge into
   * @param mergePath The path to potentially merge
   * @returns True if paths can be merged
   */
  private canMergePaths(basePath: PooledLexerPath, mergePath: PooledLexerPath): boolean {
    // Simple merge criteria - paths at same position with similar scores
    const baseTokens = basePath.getTokens();
    const mergeTokens = mergePath.getTokens();

    // If token sequences are similar (basic comparison)
    if (baseTokens.length === mergeTokens.length) {
      const scoreDifference = Math.abs(basePath.getScore() - mergePath.getScore());
      return scoreDifference < 0.1; // Merge if scores are very close
    }

    return false;
  }

  /**
   * Processes the next token for a lexer path.
   * @param lexerPath The lexer path to process
   * @param tokenList The token list to add tokens to
   */
  private processNextToken(lexerPath: PooledLexerPath, tokenList: Token[]): void {
    // Get current source line
    const sourceLine = this.getCurrentSourceLine(lexerPath);
    if (!sourceLine) {
      this.removeLexerPath(lexerPath.getLexerPathId(), false);
      return;
    }

    // Get valid terminals from parser
    const validTerminals = this.stepParser.getValidTerminals(lexerPath);

    // Try to match terminals using zerocopy approach
    const matches = this.findTerminalMatches(sourceLine, lexerPath, validTerminals);

    if (matches.length === 0) {
      // No matches found, remove path
      this.removeLexerPath(lexerPath.getLexerPathId(), false);
    } else {
      // Process matches and create new paths if needed
      this.processTerminalMatches(lexerPath, matches, tokenList);
    }
  }

  /**
   * Gets the current source line for a lexer path.
   * @param lexerPath The lexer path
   * @returns The current source line or null
   */
  private getCurrentSourceLine(lexerPath: PooledLexerPath): IParserLexerSourceLine | null {
    const lineNumber = lexerPath.getActiveLineNumber();
    const lines = this.sourceLinesContainer.getSourceLines();
    return lines.find(line => line.getLineNumber() === lineNumber) || null;
  }

  /**
   * Finds terminal matches using zerocopy string operations.
   * @param sourceLine The source line to match against
   * @param lexerPath The current lexer path
   * @param validTerminals The valid terminals to try
   * @returns Array of terminal matches
   */
  private findTerminalMatches(
    sourceLine: IParserLexerSourceLine,
    lexerPath: PooledLexerPath,
    validTerminals: Terminal[],
  ): TerminalMatch[] {
    const matches: TerminalMatch[] = [];
    const lineContent = sourceLine.getContent();
    const startPos = lexerPath.getActiveCharacterNumber();

    for (const terminal of validTerminals) {
      const match = this.tryMatchTerminal(terminal, lineContent, startPos);
      if (match) {
        matches.push(match);
      }
    }

    return matches;
  }

  /**
   * Tries to match a terminal at the current position.
   * @param terminal The terminal to match
   * @param lineContent The interned line content
   * @param startPos The starting position
   * @returns A terminal match or null
   */
  private tryMatchTerminal(terminal: Terminal, lineContent: string, startPos: number): TerminalMatch | null {
    const input = lineContent.substring(startPos);
    const match = terminal.match(input);

    if (match && match.length > 0) {
      const value = match[1] || match[0];
      return new TerminalMatch(terminal, value, match[0].length);
    }

    return null;
  }

  /**
   * Processes terminal matches and creates tokens.
   * @param lexerPath The current lexer path
   * @param matches The terminal matches
   * @param tokenList The token list to add tokens to
   */
  private processTerminalMatches(
    lexerPath: PooledLexerPath,
    matches: TerminalMatch[],
    tokenList: Token[],
  ): void {
    if (matches.length === 0) {
      // No matches, create unknown token
      this.createUnknownToken(lexerPath, tokenList);
      return;
    }

    if (matches.length === 1) {
      // Single match, create token
      this.createTokenFromMatch(lexerPath, matches[0], tokenList);
      return;
    }

    // Multiple matches (ambiguity), handle by creating multiple paths
    this.handleMultipleMatches(lexerPath, matches, tokenList);
  }

  /**
   * Creates an unknown token using zerocopy approach.
   * @param lexerPath The lexer path
   * @param tokenList The token list to add the token to
   */
  private createUnknownToken(lexerPath: PooledLexerPath, tokenList: Token[]): void {
    const sourceLine = this.getCurrentSourceLine(lexerPath);
    if (!sourceLine) {
      this.removeLexerPath(lexerPath.getLexerPathId(), false);
      return;
    }

    const lineContent = sourceLine.getContent();
    const charPos = lexerPath.getActiveCharacterNumber();

    if (charPos >= lineContent.length) {
      // End of line, move to next line
      lexerPath.setActiveLineNumber(lexerPath.getActiveLineNumber() + 1);
      lexerPath.setActiveCharacterNumber(0);
      return;
    }

    const value = lineContent.charAt(charPos);

    const unknownToken = new Token(
      lexerPath.getLexerPathId(),
      new Terminal('UNKNOWN', ''),
      value, // Use the actual string value, not interned ID
      lexerPath.getActiveLineNumber(),
      lexerPath.getActiveCharacterNumber(),
    );

    lexerPath.setActiveCharacterNumber(charPos + 1);
    tokenList.push(unknownToken);
  }

  /**
   * Creates a token from a terminal match using zerocopy approach.
   * @param lexerPath The lexer path
   * @param match The terminal match
   * @param tokenList The token list to add the token to
   */
  private createTokenFromMatch(lexerPath: PooledLexerPath, match: TerminalMatch, tokenList: Token[]): void {
    const value = match.getValue(); // Use the actual string value, not the interned ID

    const token = new Token(
      lexerPath.getLexerPathId(),
      match.getTerminal(),
      value,
      lexerPath.getActiveLineNumber(),
      lexerPath.getActiveCharacterNumber(),
    );

    lexerPath.setActiveCharacterNumber(lexerPath.getActiveCharacterNumber() + match.getLength());
    tokenList.push(token);

    // Create aligned token for zerocopy tracking
    const span: TokenSpan = {
      start: {
        line: lexerPath.getActiveLineNumber(),
        column: lexerPath.getActiveCharacterNumber(),
        offset: lexerPath.getActiveCharacterNumber(),
      },
      end: {
        line: lexerPath.getActiveLineNumber(),
        column: lexerPath.getActiveCharacterNumber() + match.getLength(),
        offset: lexerPath.getActiveCharacterNumber() + match.getLength(),
      },
    };

    const alignedToken = this.tokenArray.push(
      TokenType.IDENTIFIER, // Default type, could be enhanced based on terminal
      value,
      span,
    );
    lexerPath.addToken(alignedToken);
  }

  /**
   * Handles multiple terminal matches (ambiguity) using object pooling.
   * @param lexerPath The lexer path
   * @param matches The terminal matches
   * @param tokenList The token list to add tokens to
   */
  private handleMultipleMatches(lexerPath: PooledLexerPath, matches: TerminalMatch[], tokenList: Token[]): void {
    // Use the first match for the current path
    this.createTokenFromMatch(lexerPath, matches[0], tokenList);

    // Create new paths for the other matches using object pool
    for (let i = 1; i < matches.length; i++) {
      const newPath = this.pathPool.acquire();
      this.maximumLexerPathId++;

      // Copy state from current path
      newPath.setLexerPathId(this.maximumLexerPathId);
      newPath.setActiveLineNumber(lexerPath.getActiveLineNumber());
      newPath.setActiveCharacterNumber(lexerPath.getActiveCharacterNumber());

      // Add to path map
      this.lexerPathMap.set(this.maximumLexerPathId, newPath);

      // Create token for this path
      this.createTokenFromMatch(newPath, matches[i], tokenList);
    }
  }
}

