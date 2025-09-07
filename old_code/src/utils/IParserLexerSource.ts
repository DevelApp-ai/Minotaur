/**
 * Interface for a source line that can be used by the parser and lexer.
 */
export interface IParserLexerSourceLine {
  /**
   * Gets the content of the line.
   */
  getContent(): string;

  /**
   * Gets the length of the line.
   */
  getLength(): number;

  /**
   * Gets the line number.
   */
  getLineNumber(): number;

  /**
   * Gets the file name.
   */
  getFileName(): string;
}

/**
 * Interface for a container of source lines that can be used by the parser and lexer.
 */
export interface IParserLexerSourceContainer {
  /**
   * Gets the source lines.
   */
  getSourceLines(): IParserLexerSourceLine[];

  /**
   * Gets the number of source lines.
   */
  getCount(): number;

  /**
   * Gets a specific line.
   * @param fileName The file name
   * @param lineNumber The line number
   */
  getLine(fileName: string, lineNumber: number): IParserLexerSourceLine;
}
