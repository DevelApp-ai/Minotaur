/**
 * Represents a production match result.
 */
export class ProductionMatch {
  private production: Production;
  private matches: ProductionMatch[];
  private value: string;
  private startPosition: number;
  private endPosition: number;

  /**
   * Creates a new ProductionMatch instance.
   * @param production The production that matched
   * @param value The matched value
   * @param startPosition The start position of the match
   * @param endPosition The end position of the match
   */
  constructor(production: Production, value: string, startPosition: number, endPosition: number) {
    this.production = production;
    this.matches = [];
    this.value = value;
    this.startPosition = startPosition;
    this.endPosition = endPosition;
  }

  /**
   * Gets the production.
   */
  public getProduction(): Production {
    return this.production;
  }

  /**
   * Gets the child matches.
   */
  public getMatches(): ProductionMatch[] {
    return this.matches;
  }

  /**
   * Adds a child match.
   * @param match The match to add
   */
  public addMatch(match: ProductionMatch): void {
    this.matches.push(match);
  }

  /**
   * Gets the matched value.
   */
  public getValue(): string {
    return this.value;
  }

  /**
   * Gets the start position of the match.
   */
  public getStartPosition(): number {
    return this.startPosition;
  }

  /**
   * Gets the end position of the match.
   */
  public getEndPosition(): number {
    return this.endPosition;
  }

  /**
   * Creates a string representation of this production match.
   */
  public toString(): string {
    return `ProductionMatch(${this.production.getName()}, "${this.value}", ${this.startPosition}-${this.endPosition})`;
  }
}

// Import required classes
import { Production } from './Production';
