/**
 * Represents a terminal symbol in the grammar.
 */
export class Terminal implements IProductionPart {
  private name: string;
  private orderImportant: boolean;
  private terminalOrder: number;
  private pattern: RegExp;

  /**
   * Creates a new Terminal instance.
   * @param name The name of the terminal
   * @param pattern The regular expression pattern for matching
   * @param orderImportant Whether the order of this terminal is important
   * @param terminalOrder The order of this terminal
   */
  constructor(name: string, pattern: string, orderImportant: boolean = false, terminalOrder: number = 0) {
    this.name = name;
    this.orderImportant = orderImportant;
    this.terminalOrder = terminalOrder;
    this.pattern = new RegExp(`^(${pattern})`, 'g');
  }

  /**
   * Gets the name of the terminal.
   */
  public getName(): string {
    return this.name;
  }

  /**
   * Checks if the order of this terminal is important.
   */
  public isOrderImportant(): boolean {
    return this.orderImportant;
  }

  /**
   * Gets the order of this terminal.
   */
  public getTerminalOrder(): number {
    return this.terminalOrder;
  }

  /**
   * Matches the input against this terminal's pattern.
   * @param input The input string to match
   * @returns The match result or null if no match
   */
  public match(input: string): RegExpMatchArray | null {
    this.pattern.lastIndex = 0;
    return this.pattern.exec(input);
  }

  /**
   * Gets the type of this production part.
   */
  public getType(): ProductionPartType {
    return ProductionPartType.Terminal;
  }

  /**
   * Creates a string representation of this terminal.
   */
  public toString(): string {
    return `Terminal(${this.name})`;
  }
}

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
  getName(): string;
  getType(): ProductionPartType;
  toString(): string;
}
