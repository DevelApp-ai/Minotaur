/**
 * Represents a path through the input being processed by the lexer.
 */
export class LexerPath {
  // Constants
  public static readonly NOTSET = -1;

  private lexerPathId: number;
  private parentLexerPathId: number;
  private currentToken: Token;
  private activeLineNumber: number;
  private activeCharacterNumber: number;
  private activeIndentNumber: number;
  private activeFileName: string;

  /**
   * Creates a new LexerPath instance.
   */
  constructor() {
    this.lexerPathId = LexerPath.NOTSET;
    this.parentLexerPathId = LexerPath.NOTSET;
    this.currentToken = new Token(LexerPath.NOTSET, new Terminal('NULL', ''), '', LexerPath.NOTSET, LexerPath.NOTSET);
    this.activeLineNumber = LexerPath.NOTSET;
    this.activeCharacterNumber = LexerPath.NOTSET;
    this.activeIndentNumber = 0;
    this.activeFileName = '';
  }

  /**
   * Creates a start lexer path.
   */
  public static startLexerPath(): LexerPath {
    const lexerPath = new LexerPath();
    lexerPath.activeLineNumber = 0;
    lexerPath.activeCharacterNumber = 0;
    return lexerPath;
  }

  /**
   * Gets the lexer path ID.
   */
  public getLexerPathId(): number {
    return this.lexerPathId;
  }

  /**
   * Sets the lexer path ID.
   * @param id The new lexer path ID
   */
  public setLexerPathId(id: number): void {
    this.lexerPathId = id;
  }

  /**
   * Gets the parent lexer path ID.
   */
  public getParentLexerPathId(): number {
    return this.parentLexerPathId;
  }

  /**
   * Sets the parent lexer path ID.
   * @param id The new parent lexer path ID
   */
  public setParentLexerPathId(id: number): void {
    this.parentLexerPathId = id;
  }

  /**
   * Gets the current token.
   */
  public getCurrentToken(): Token {
    return this.currentToken;
  }

  /**
   * Sets the current token.
   * @param token The new current token
   */
  public setCurrentToken(token: Token): void {
    this.currentToken = token;
  }

  /**
   * Gets the active line number.
   */
  public getActiveLineNumber(): number {
    return this.activeLineNumber;
  }

  /**
   * Sets the active line number.
   * @param lineNumber The new active line number
   */
  public setActiveLineNumber(lineNumber: number): void {
    this.activeLineNumber = lineNumber;
  }

  /**
   * Gets the active character number.
   */
  public getActiveCharacterNumber(): number {
    return this.activeCharacterNumber;
  }

  /**
   * Sets the active character number.
   * @param characterNumber The new active character number
   */
  public setActiveCharacterNumber(characterNumber: number): void {
    this.activeCharacterNumber = characterNumber;
  }

  /**
   * Gets the active indent number.
   */
  public getActiveIndentNumber(): number {
    return this.activeIndentNumber;
  }

  /**
   * Sets the active indent number.
   * @param indentNumber The new active indent number
   */
  public setActiveIndentNumber(indentNumber: number): void {
    this.activeIndentNumber = indentNumber;
  }

  /**
   * Gets the active file name.
   */
  public getActiveFileName(): string {
    return this.activeFileName;
  }

  /**
   * Sets the active file name.
   * @param fileName The new active file name
   */
  public setActiveFileName(fileName: string): void {
    this.activeFileName = fileName;
  }

  /**
   * Creates a string representation of this lexer path.
   */
  public toString(): string {
    // eslint-disable-next-line max-len
    return `LexerPath(id: ${this.lexerPathId}, parent: ${this.parentLexerPathId}, line: ${this.activeLineNumber}, char: ${this.activeCharacterNumber})`;
  }
}

// Import required classes
import { Token } from './Token';
import { Terminal } from './Terminal';
