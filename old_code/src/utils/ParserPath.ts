/**
 * Represents a parser path during parsing.
 */
export class ParserPath {
  private parserPathId: number;
  private lexerPathId: number;
  private activeProductions: Production[];
  private activeMatches: ProductionMatch[];
  private position: number;

  /**
   * Creates a new ParserPath instance.
   * @param parserPathId The ID of the parser path
   * @param lexerPathId The ID of the associated lexer path
   * @param position The current position in the input
   */
  constructor(parserPathId: number, lexerPathId: number, position: number) {
    this.parserPathId = parserPathId;
    this.lexerPathId = lexerPathId;
    this.activeProductions = [];
    this.activeMatches = [];
    this.position = position;
  }

  /**
   * Gets the parser path ID.
   */
  public getParserPathId(): number {
    return this.parserPathId;
  }

  /**
   * Gets the lexer path ID.
   */
  public getLexerPathId(): number {
    return this.lexerPathId;
  }

  /**
   * Gets the active productions.
   */
  public getActiveProductions(): Production[] {
    return this.activeProductions;
  }

  /**
   * Adds an active production.
   * @param production The production to add
   */
  public addActiveProduction(production: Production): void {
    this.activeProductions.push(production);
  }

  /**
   * Gets the active matches.
   */
  public getActiveMatches(): ProductionMatch[] {
    return this.activeMatches;
  }

  /**
   * Adds an active match.
   * @param match The match to add
   */
  public addActiveMatch(match: ProductionMatch): void {
    this.activeMatches.push(match);
  }

  /**
   * Gets the current position.
   */
  public getPosition(): number {
    return this.position;
  }

  /**
   * Sets the current position.
   * @param position The new position
   */
  public setPosition(position: number): void {
    this.position = position;
  }

  /**
   * Sets the parser path ID.
   * @param id The new parser path ID
   */
  public setParserPathId(id: number): void {
    this.parserPathId = id;
  }

  /**
   * Sets the lexer path ID.
   * @param id The new lexer path ID
   */
  public setLexerPathId(id: number): void {
    this.lexerPathId = id;
  }

  /**
   * Creates a string representation of this parser path.
   */
  public toString(): string {
    return `ParserPath(id: ${this.parserPathId}, lexerPath: ${this.lexerPathId}, position: ${this.position})`;
  }
}

// Import required classes
import { Production } from './Production';
import { ProductionMatch } from './ProductionMatch';
