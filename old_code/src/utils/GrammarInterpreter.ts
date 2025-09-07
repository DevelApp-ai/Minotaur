/**
 * Builds grammar objects from loaded grammar files.
 */
export class GrammarInterpreter implements IParserLexerSourceContainer {
  private grammarName: string;
  private tokenSplitter: TokenSplitterType;
  private regexTokenSplitter: string;
  private sourceLines: IParserLexerSourceLine[];
  private grammarContainer: GrammarContainer;

  // Inheritance support properties
  private inheritable: boolean;
  private formatType: GrammarFormatType;
  private baseGrammars: string[];
  private importSemantics: boolean;
  private coordinateTokens: boolean;
  private inheritanceResolver: InheritanceResolver;

  /**
   * Creates a new GrammarInterpreter instance.
   * @param grammarContainer The grammar container
   */
  constructor(grammarContainer: GrammarContainer) {
    this.grammarName = '';
    this.tokenSplitter = TokenSplitterType.None;
    this.regexTokenSplitter = '';
    this.sourceLines = [];
    this.grammarContainer = grammarContainer;

    // Initialize inheritance properties
    this.inheritable = false;
    this.formatType = GrammarFormatType.CEBNF;
    this.baseGrammars = [];
    this.importSemantics = false;
    this.coordinateTokens = false;
    this.inheritanceResolver = new InheritanceResolver(grammarContainer);
  }

  /**
   * Gets the grammar name.
   */
  public getGrammarName(): string {
    return this.grammarName;
  }

  /**
   * Sets the grammar name.
   * @param name The new grammar name
   */
  public setGrammarName(name: string): void {
    this.grammarName = name;
  }

  /**
   * Gets the token splitter type.
   */
  public getTokenSplitter(): TokenSplitterType {
    return this.tokenSplitter;
  }

  /**
   * Sets the token splitter type.
   * @param splitter The new token splitter type
   */
  public setTokenSplitter(splitter: TokenSplitterType): void {
    this.tokenSplitter = splitter;
  }

  /**
   * Gets the regex token splitter.
   */
  public getRegexTokenSplitter(): string {
    return this.regexTokenSplitter;
  }

  /**
   * Sets the regex token splitter.
   * @param regex The new regex token splitter
   */
  public setRegexTokenSplitter(regex: string): void {
    this.regexTokenSplitter = regex;
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
   * Adds a source line.
   * @param line The line to add
   */
  public addSourceLine(line: IParserLexerSourceLine): void {
    this.sourceLines.push(line);
  }

  /**
   * Parses a grammar file.
   * @param content The content of the grammar file
   * @param fileName The name of the file
   * @returns The parsed grammar
   */
  public parseGrammar(content: string, fileName: string): Grammar {
    // Clear source lines
    this.sourceLines = [];

    // Split content into lines
    const lines = content.split('\n');

    // Add lines to source lines
    for (let i = 0; i < lines.length; i++) {
      this.addSourceLine(new GrammarSourceLine(lines[i], i + 1, fileName));
    }

    // Parse grammar header
    this.parseGrammarHeader();

    // Create grammar
    const grammar = new Grammar(this.grammarName);
    grammar.setTokenSplitterType(this.tokenSplitter);
    grammar.setRegexTokenSplitter(this.regexTokenSplitter);

    // Set inheritance properties
    grammar.setInheritable(this.inheritable);
    grammar.setFormatType(this.formatType);
    grammar.setBaseGrammars(this.baseGrammars);
    grammar.setImportSemantics(this.importSemantics);
    grammar.setCoordinateTokens(this.coordinateTokens);

    // Parse productions
    this.parseProductions(grammar);

    // If this grammar has inheritance, resolve it
    if (this.baseGrammars.length > 0) {
      return this.inheritanceResolver.resolveInheritance(this.grammarName);
    }

    return grammar;
  }

  /**
   * Parses the grammar header.
   */
  private parseGrammarHeader(): void {
    if (this.sourceLines.length === 0) {
      throw new GrammarError('Empty grammar file');
    }

    let lineIndex = 0;

    // Parse grammar name (first line)
    const firstLine = this.sourceLines[lineIndex].getContent().trim();
    if (firstLine.startsWith('Grammar:')) {
      this.grammarName = firstLine.substring('Grammar:'.length).trim();
    } else if (firstLine.startsWith('Extends Grammar:')) {
      this.grammarName = firstLine.substring('Extends Grammar:'.length).trim();
      // Handle grammar extension (legacy support)
      this.handleGrammarExtension();
    } else {
      throw new GrammarError('Invalid grammar header', lineIndex + 1, this.sourceLines[lineIndex].getFileName());
    }

    lineIndex++;

    // Parse header directives
    while (lineIndex < this.sourceLines.length) {
      const line = this.sourceLines[lineIndex].getContent().trim();

      // Skip empty lines and comments
      if (line === '' || line.startsWith('//') || line.startsWith('/*')) {
        lineIndex++;
        continue;
      }

      // Check if we've reached the productions section
      if (line.includes('::=') || line.startsWith('<')) {
        break;
      }

      // Parse directive
      if (line.startsWith('TokenSplitter:')) {
        const splitterValue = line.substring('TokenSplitter:'.length).trim();
        this.parseTokenSplitter(splitterValue, lineIndex);
      } else if (line.startsWith('Inheritable:')) {
        const value = line.substring('Inheritable:'.length).trim();
        this.inheritable = value.toLowerCase() === 'true';
      } else if (line.startsWith('FormatType:')) {
        const value = line.substring('FormatType:'.length).trim();
        this.parseFormatType(value, lineIndex);
      } else if (line.startsWith('Inherits:')) {
        const value = line.substring('Inherits:'.length).trim();
        this.parseBaseGrammars(value, lineIndex);
      } else if (line.startsWith('ImportSemantics:')) {
        const value = line.substring('ImportSemantics:'.length).trim();
        this.importSemantics = value.toLowerCase() === 'true';
      } else if (line.startsWith('CoordinateTokens:')) {
        const value = line.substring('CoordinateTokens:'.length).trim();
        this.coordinateTokens = value.toLowerCase() === 'true';
      } else if (line.startsWith('Include:')) {
        // Legacy support for Include directive
        const value = line.substring('Include:'.length).trim();
        this.parseBaseGrammars(value, lineIndex);
      } else {
        // Unknown directive - could be a production, so break
        break;
      }

      lineIndex++;
    }
  }

  /**
   * Parses the token splitter.
   * @param splitterValue The token splitter value
   * @param lineIndex The line index for error reporting
   */
  private parseTokenSplitter(splitterValue: string, lineIndex: number): void {
    if (splitterValue === 'None') {
      this.tokenSplitter = TokenSplitterType.None;
    } else if (splitterValue === 'Space') {
      this.tokenSplitter = TokenSplitterType.Space;
    } else if (splitterValue.startsWith('"') && splitterValue.endsWith('"')) {
      this.tokenSplitter = TokenSplitterType.Regex;
      this.regexTokenSplitter = splitterValue.substring(1, splitterValue.length - 1);
    } else {
      // eslint-disable-next-line max-len
      throw new GrammarError(`Invalid token splitter: ${splitterValue}`, lineIndex + 1, this.sourceLines[lineIndex].getFileName());
    }
  }

  /**
   * Parses the format type.
   * @param formatValue The format type value
   * @param lineIndex The line index for error reporting
   */
  private parseFormatType(formatValue: string, lineIndex: number): void {
    switch (formatValue.toUpperCase()) {
      case 'CEBNF':
        this.formatType = GrammarFormatType.CEBNF;
        break;
      case 'ANTLR4':
        this.formatType = GrammarFormatType.ANTLR4;
        break;
      case 'BISON':
        this.formatType = GrammarFormatType.Bison;
        break;
      case 'FLEX':
        this.formatType = GrammarFormatType.Flex;
        break;
      case 'YACC':
        this.formatType = GrammarFormatType.Yacc;
        break;
      case 'LEX':
        this.formatType = GrammarFormatType.Lex;
        break;
      case 'MINOTAUR':
        this.formatType = GrammarFormatType.Minotaur;
        break;
      default:
        // eslint-disable-next-line max-len
        throw new GrammarError(`Invalid format type: ${formatValue}`, lineIndex + 1, this.sourceLines[lineIndex].getFileName());
    }
  }

  /**
   * Parses the base grammars to inherit from.
   * @param baseGrammarsValue The base grammars value
   * @param lineIndex The line index for error reporting
   */
  private parseBaseGrammars(baseGrammarsValue: string, lineIndex: number): void {
    const grammars = baseGrammarsValue.split(',').map(g => g.trim()).filter(g => g.length > 0);

    for (const grammarName of grammars) {
      if (!this.baseGrammars.includes(grammarName)) {
        this.baseGrammars.push(grammarName);
      }
    }

    // Validate that base grammars exist
    for (const grammarName of grammars) {
      if (!this.grammarContainer.hasGrammar(grammarName)) {
        // eslint-disable-next-line max-len
        throw new GrammarError(`Base grammar not found: ${grammarName}`, lineIndex + 1, this.sourceLines[lineIndex].getFileName());
      }
    }
  }

  /**
   * Handles grammar extension.
   */
  private handleGrammarExtension(): void {
    // Look for Include: line
    for (let i = 1; i < this.sourceLines.length; i++) {
      const line = this.sourceLines[i].getContent().trim();
      if (line.startsWith('Include:')) {
        const includeValue = line.substring('Include:'.length).trim();
        const includedGrammars = includeValue.split(',').map(g => g.trim());

        // Process included grammars
        for (const grammarName of includedGrammars) {
          if (!this.grammarContainer.hasGrammar(grammarName)) {
            // eslint-disable-next-line max-len
            throw new GrammarError(`Included grammar not found: ${grammarName}`, i + 1, this.sourceLines[i].getFileName());
          }
        }

        break;
      }
    }
  }

  /**
   * Parses productions from the grammar file.
   * @param grammar The grammar to add productions to
   */
  private parseProductions(grammar: Grammar): void {
    let inProduction = false;
    let currentProduction: Production | null = null;
    let startLine = 0;

    // Skip header lines
    let lineIndex = this.tokenSplitter !== TokenSplitterType.None ? 2 : 1;

    // Process remaining lines
    for (; lineIndex < this.sourceLines.length; lineIndex++) {
      const line = this.sourceLines[lineIndex].getContent().trim();

      // Skip empty lines and comments
      if (line === '' || line.startsWith('//')) {
        continue;
      }

      // Check for production definition
      if (line.includes('::=')) {
        // If we were in a production, finalize it
        if (inProduction && currentProduction) {
          grammar.addProduction(currentProduction);
        }

        // Parse new production
        const parts = line.split('::=');
        if (parts.length !== 2) {
          // eslint-disable-next-line max-len
          throw new GrammarError(`Invalid production definition: ${line}`, lineIndex + 1, this.sourceLines[lineIndex].getFileName());
        }

        const productionName = parts[0].trim();
        const productionExpression = parts[1].trim();

        // Check for context
        let context: string | null = null;
        const contextMatch = productionName.match(/<([^>]+)\s*\(([^)]+)\)>/);
        if (contextMatch) {
          const name = contextMatch[1].trim();
          context = contextMatch[2].trim();
          currentProduction = new Production(name, context);
        } else {
          currentProduction = new Production(productionName.replace(/[<>]/g, ''));
        }

        // Check for callback
        const callbackMatch = productionExpression.match(/(.+)\s*=>\s*\{(.+)\}/);
        if (callbackMatch) {
          const expression = callbackMatch[1].trim();
          const callback = callbackMatch[2].trim();

          // Parse expression
          this.parseProductionExpression(expression, currentProduction);

          // Create callback function
          currentProduction.setCallback(this.createCallbackFunction(callback));
        } else {
          // Parse expression without callback
          this.parseProductionExpression(productionExpression, currentProduction);
        }

        inProduction = true;
        startLine = lineIndex;
      } else if (inProduction && currentProduction) {
        // Continuation of production
        // Check for callback
        const callbackMatch = line.match(/(.+)\s*=>\s*\{(.+)\}/);
        if (callbackMatch) {
          const expression = callbackMatch[1].trim();
          const callback = callbackMatch[2].trim();

          // Parse expression
          this.parseProductionExpression(expression, currentProduction);

          // Create callback function
          currentProduction.setCallback(this.createCallbackFunction(callback));
        } else {
          // Parse expression without callback
          this.parseProductionExpression(line, currentProduction);
        }
      } else {
        throw new GrammarError(`Unexpected line: ${line}`, lineIndex + 1, this.sourceLines[lineIndex].getFileName());
      }
    }

    // Finalize last production
    if (inProduction && currentProduction) {
      grammar.addProduction(currentProduction);
    }
  }

  /**
   * Parses a production expression.
   * @param expression The expression to parse
   * @param production The production to add parts to
   */
  private parseProductionExpression(expression: string, production: Production): void {
    // Split expression into parts
    const parts = this.splitExpression(expression);

    for (const part of parts) {
      if (part.startsWith('<') && part.endsWith('>')) {
        // Non-terminal
        const name = part.substring(1, part.length - 1);

        // Check for context
        const contextMatch = name.match(/([^(]+)\s*\(([^)]+)\)/);
        if (contextMatch) {
          const nonTerminalName = contextMatch[1].trim();
          const context = contextMatch[2].trim();
          production.addPart(new NonTerminal(nonTerminalName, context));
        } else {
          production.addPart(new NonTerminal(name));
        }
      } else if (part.startsWith('"') && part.endsWith('"')) {
        // Terminal (literal)
        const value = part.substring(1, part.length - 1);
        production.addPart(new Terminal(value, this.escapeRegex(value)));
      } else if (part.startsWith('/') && part.endsWith('/')) {
        // Terminal (regex)
        const pattern = part.substring(1, part.length - 1);
        production.addPart(new Terminal(`REGEX(${pattern})`, pattern));
      } else {
        // Terminal (identifier)
        production.addPart(new Terminal(part, this.escapeRegex(part)));
      }
    }
  }

  /**
   * Splits an expression into parts.
   * @param expression The expression to split
   * @returns The parts of the expression
   */
  private splitExpression(expression: string): string[] {
    const parts: string[] = [];
    let current = '';
    let inQuotes = false;
    let inRegex = false;
    let inNonTerminal = false;

    for (let i = 0; i < expression.length; i++) {
      const char = expression[i];

      if (char === '"' && !inRegex && !inNonTerminal) {
        inQuotes = !inQuotes;
        current += char;
      } else if (char === '/' && !inQuotes && !inNonTerminal) {
        inRegex = !inRegex;
        current += char;
      } else if (char === '<' && !inQuotes && !inRegex && !inNonTerminal) {
        inNonTerminal = true;
        current += char;
      } else if (char === '>' && !inQuotes && !inRegex && inNonTerminal) {
        inNonTerminal = false;
        current += char;
      } else if (char === ' ' && !inQuotes && !inRegex && !inNonTerminal) {
        if (current !== '') {
          parts.push(current);
          current = '';
        }
      } else if (char === '|' && !inQuotes && !inRegex && !inNonTerminal) {
        if (current !== '') {
          parts.push(current);
          current = '';
        }
        // Handle alternation
        parts.push('|');
      } else {
        current += char;
      }
    }

    if (current !== '') {
      parts.push(current);
    }

    return parts;
  }

  /**
   * Escapes special characters in a regex pattern.
   * @param value The value to escape
   * @returns The escaped value
   */
  private escapeRegex(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  /**
   * Creates a callback function from a string.
   * @param callbackStr The callback string
   * @returns The callback function
   */
  private createCallbackFunction(callbackStr: string): Function {
    // For now, just return a function that logs the callback
    return (match: string, context: any, position: number) => {
    // eslint-disable-next-line no-console
      console.log(`Callback: ${callbackStr}`, { match, context, position });
    };
  }
}

/**
 * Represents a source line in a grammar file.
 */
class GrammarSourceLine implements IParserLexerSourceLine {
  private content: string;
  private lineNumber: number;
  private fileName: string;

  /**
   * Creates a new GrammarSourceLine instance.
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

// Import required classes and interfaces
import { Grammar, GrammarFormatType } from './Grammar';
import { GrammarContainer, GrammarError } from './GrammarContainer';
import { InheritanceResolver } from './InheritanceResolver';
import { IParserLexerSourceContainer, IParserLexerSourceLine } from './IParserLexerSource';
import { TokenSplitterType } from './LexerOptions';
import { Terminal } from './Terminal';
import { Production } from './Production';
import { NonTerminal } from './NonTerminal';
