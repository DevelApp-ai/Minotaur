/**
 * Enum representing the type of production part.
 */
export enum ProductionPartType {
  Terminal,
  NonTerminal
}

/**
 * Interface for production parts (terminals and non-terminals).
 */
export interface IProductionPart {
  /**
   * Gets the name of the production part.
   */
  getName(): string;

  /**
   * Gets the type of the production part.
   */
  getType(): ProductionPartType;

  /**
   * Creates a string representation of this production part.
   */
  toString(): string;
}
