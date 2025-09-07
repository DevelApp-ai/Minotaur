/**
 * Represents a non-terminal symbol in the grammar.
 */
export class NonTerminal implements IProductionPart {
  private name: string;
  private context: string | null;

  /**
   * Creates a new NonTerminal instance.
   * @param name The name of the non-terminal
   * @param context Optional context for context-sensitive parsing
   */
  constructor(name: string, context: string | null = null) {
    this.name = name;
    this.context = context;
  }

  /**
   * Gets the name of the non-terminal.
   */
  public getName(): string {
    return this.name;
  }

  /**
   * Gets the context of the non-terminal.
   */
  public getContext(): string | null {
    return this.context;
  }

  /**
   * Sets the context of the non-terminal.
   * @param context The new context
   */
  public setContext(context: string | null): void {
    this.context = context;
  }

  /**
   * Gets the type of this production part.
   */
  public getType(): ProductionPartType {
    return ProductionPartType.NonTerminal;
  }

  /**
   * Creates a string representation of this non-terminal.
   */
  public toString(): string {
    return this.context
      ? `NonTerminal(${this.name}, context: ${this.context})`
      : `NonTerminal(${this.name})`;
  }
}

// Import required interfaces and enums
import { IProductionPart, ProductionPartType } from './IProductionPart';
