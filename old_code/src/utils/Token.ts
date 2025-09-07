/**
 * Represents a token produced by the lexer.
 */
export class Token {
  private lexerPathId: number;
  private terminal: Terminal;
  private value: string;
  private lineNumber: number;
  private characterNumber: number;

  // Constants for special token positions
  public static readonly LINENUMBER_FIRSTLINE = 1;
  public static readonly CHARPOSITION_START = 0;
  public static readonly LEXERPATH_INTERN_REMOVED = 'INTERNAL';
  public static readonly LEXERPATH_EXTERN_REMOVED = 'EXTERNAL';

  /**
   * Creates a new Token instance.
   * @param lexerPathId The ID of the lexer path that produced this token
   * @param terminal The terminal that matched this token
   * @param value The value of the token
   * @param lineNumber The line number where the token was found
   * @param characterNumber The character position where the token was found
   */
  constructor(lexerPathId: number, terminal: Terminal, value: string, lineNumber: number, characterNumber: number) {
    this.lexerPathId = lexerPathId;
    this.terminal = terminal;
    this.value = value;
    this.lineNumber = lineNumber;
    this.characterNumber = characterNumber;
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
   * Gets the terminal.
   */
  public getTerminal(): Terminal {
    return this.terminal;
  }

  /**
   * Gets the token value.
   */
  public getValue(): string {
    return this.value;
  }

  /**
   * Gets the line number.
   */
  public getLineNumber(): number {
    return this.lineNumber;
  }

  /**
   * Gets the character number.
   */
  public getCharacterNumber(): number {
    return this.characterNumber;
  }

  /**
   * Checks if this token is a beginning of file token.
   */
  public get isBOF(): boolean {
    return this.terminal.getName() === 'BOF';
  }

  /**
   * Checks if this token is an end of file token.
   */
  public get isEOF(): boolean {
    return this.terminal.getName() === 'EOF';
  }

  /**
   * Checks if this token is a beginning of line token.
   */
  public get isBOL(): boolean {
    return this.terminal.getName() === 'BOL';
  }

  /**
   * Checks if this token is an end of line token.
   */
  public get isEOL(): boolean {
    return this.terminal.getName() === 'EOL';
  }

  /**
   * Checks if this token is a null token.
   */
  public get isNULL(): boolean {
    return this.terminal.getName() === 'NULL';
  }

  /**
   * Checks if this token is a built-in token.
   */
  public get isBuiltIn(): boolean {
    const name = this.terminal.getName();
    return name === 'BOF' || name === 'EOF' || name === 'BOL' || name === 'EOL';
  }

  /**
   * Creates a string representation of this token.
   */
  public toString(): string {
    // eslint-disable-next-line max-len
    return `Token(${this.terminal.getName()}, "${this.value}", line: ${this.lineNumber}, char: ${this.characterNumber})`;
  }
}

// Import the Terminal class
import { Terminal } from './Terminal';
