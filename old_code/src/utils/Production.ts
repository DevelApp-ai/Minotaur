/**
 * Represents a production rule in the grammar.
 */
export class Production {
  private name: string;
  private parts: IProductionPart[];
  private context: string | null;
  private callback: Function | null;

  /**
   * Creates a new Production instance.
   * @param name The name of the production
   * @param context Optional context for context-sensitive parsing
   */
  constructor(name: string, context: string | null = null) {
    this.name = name;
    this.parts = [];
    this.context = context;
    this.callback = null;
  }

  /**
   * Gets the name of the production.
   */
  public getName(): string {
    return this.name;
  }

  /**
   * Gets the parts of the production.
   */
  public getParts(): IProductionPart[] {
    return this.parts;
  }

  /**
   * Adds a part to the production.
   * @param part The part to add
   */
  public addPart(part: IProductionPart): void {
    this.parts.push(part);
  }

  /**
   * Gets the context of the production.
   */
  public getContext(): string | null {
    return this.context;
  }

  /**
   * Sets the context of the production.
   * @param context The new context
   */
  public setContext(context: string | null): void {
    this.context = context;
  }

  /**
   * Sets the callback function for this production.
   * @param callback The callback function
   */
  public setCallback(callback: Function): void {
    this.callback = callback;
  }

  /**
   * Gets the callback function for this production.
   */
  public getCallback(): Function | null {
    return this.callback;
  }

  /**
   * Executes the callback function if it exists.
   * @param match The matched text
   * @param context The parsing context
   * @param position The position in the source code
   */
  public executeCallback(match: string, context: any, position: number): void {
    if (this.callback) {
      this.callback(match, context, position);
    }
  }

  /**
   * Creates a string representation of this production.
   */
  public toString(): string {
    const partsStr = this.parts.map(p => p.toString()).join(' ');
    const contextStr = this.context ? ` (context: ${this.context})` : '';
    const callbackStr = this.callback ? ' => {callback}' : '';
    return `Production(${this.name}${contextStr}): ${partsStr}${callbackStr}`;
  }
}

// Import required interfaces
import { IProductionPart } from './IProductionPart';
