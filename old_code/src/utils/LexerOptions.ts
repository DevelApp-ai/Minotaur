/**
 * Enum representing the token splitter type.
 */
export enum TokenSplitterType {
  None,
  Space,
  Regex
}

/**
 * Options for the lexer.
 */
export class LexerOptions {
  private returnLexerPathTokens: boolean;
  private returnIndentTokens: boolean;

  /**
   * Creates a new LexerOptions instance.
   * @param returnLexerPathTokens Whether to return lexer path tokens
   * @param returnIndentTokens Whether to return indent tokens
   */
  constructor(returnLexerPathTokens: boolean = false, returnIndentTokens: boolean = false) {
    this.returnLexerPathTokens = returnLexerPathTokens;
    this.returnIndentTokens = returnIndentTokens;
  }

  /**
   * Gets whether to return lexer path tokens.
   */
  public getReturnLexerPathTokens(): boolean {
    return this.returnLexerPathTokens;
  }

  /**
   * Sets whether to return lexer path tokens.
   * @param value The new value
   */
  public setReturnLexerPathTokens(value: boolean): void {
    this.returnLexerPathTokens = value;
  }

  /**
   * Gets whether to return indent tokens.
   */
  public getReturnIndentTokens(): boolean {
    return this.returnIndentTokens;
  }

  /**
   * Sets whether to return indent tokens.
   * @param value The new value
   */
  public setReturnIndentTokens(value: boolean): void {
    this.returnIndentTokens = value;
  }
}
