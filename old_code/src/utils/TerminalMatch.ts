/**
 * Represents a terminal match result.
 */
export class TerminalMatch {
  private terminal: Terminal;
  private value: string;
  private length: number;

  /**
   * Creates a new TerminalMatch instance.
   * @param terminal The terminal that matched
   * @param value The matched value
   * @param length The length of the match
   */
  constructor(terminal: Terminal, value: string, length: number) {
    this.terminal = terminal;
    this.value = value;
    this.length = length;
  }

  /**
   * Gets the terminal.
   */
  public getTerminal(): Terminal {
    return this.terminal;
  }

  /**
   * Gets the matched value.
   */
  public getValue(): string {
    return this.value;
  }

  /**
   * Gets the length of the match.
   */
  public getLength(): number {
    return this.length;
  }

  /**
   * Creates a string representation of this terminal match.
   */
  public toString(): string {
    return `TerminalMatch(${this.terminal.getName()}, "${this.value}", length: ${this.length})`;
  }
}

// Import the Terminal class
import { Terminal } from './Terminal';
