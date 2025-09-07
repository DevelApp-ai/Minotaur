/**
 * Represents a source code container that can be used by the parser and lexer.
 */
export class SourceCodeContainer implements IParserLexerSourceContainer {
  private sourceLines: IParserLexerSourceLine[];

  /**
   * Creates a new SourceCodeContainer instance.
   */
  constructor() {
    this.sourceLines = [];
  }

  /**
   * Gets the source lines.
   */
  public getSourceLines(): IParserLexerSourceLine[] {
    return this.sourceLines;
  }

  /**
   * Gets the number of source lines.
   */
  public getCount(): number {
    return this.sourceLines.length;
  }

  /**
   * Gets a specific line.
   * @param fileName The file name
   * @param lineNumber The line number
   */
  public getLine(fileName: string, lineNumber: number): IParserLexerSourceLine {
    if (lineNumber < 0 || lineNumber >= this.sourceLines.length) {
      throw new Error(`Line number ${lineNumber} out of range`);
    }
    return this.sourceLines[lineNumber];
  }

  /**
   * Loads source code from a string.
   * @param content The source code content
   * @param fileName The name of the file
   */
  public loadFromString(content: string, fileName: string): void {
    // Clear source lines
    this.sourceLines = [];

    // Split content into lines
    const lines = content.split('\n');

    // Add lines to source lines
    for (let i = 0; i < lines.length; i++) {
      this.addSourceLine(new SourceCodeLine(lines[i], i + 1, fileName));
    }
  }

  /**
   * Adds a source line.
   * @param line The line to add
   */
  public addSourceLine(line: IParserLexerSourceLine): void {
    this.sourceLines.push(line);
  }
}

/**
 * Represents a source line in a source code file.
 */
class SourceCodeLine implements IParserLexerSourceLine {
  private content: string;
  private lineNumber: number;
  private fileName: string;

  /**
   * Creates a new SourceCodeLine instance.
   * @param content The content of the line
   * @param lineNumber The line number
   * @param fileName The file name
   */
  constructor(content: string, lineNumber: number, fileName: string) {
    this.content = content;
    this.lineNumber = lineNumber;
    this.fileName = fileName;
  }

  /**
   * Gets the content of the line.
   */
  public getContent(): string {
    return this.content;
  }

  /**
   * Gets the length of the line.
   */
  public getLength(): number {
    return this.content.length;
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
}

// Import required interfaces
import { IParserLexerSourceContainer, IParserLexerSourceLine } from './IParserLexerSource';
