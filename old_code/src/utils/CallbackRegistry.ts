/**
 * Manages callback functions for grammar rule activations.
 */
export class CallbackRegistry {
  private callbacks: Map<string, Function>;
  private defaultCallbacks: Map<string, Function>;

  /**
   * Creates a new CallbackRegistry instance.
   */
  constructor() {
    this.callbacks = new Map<string, Function>();
    this.defaultCallbacks = new Map<string, Function>();
    this.initializeDefaultCallbacks();
  }

  /**
   * Initializes default callbacks for common operations.
   */
  private initializeDefaultCallbacks(): void {
    // Default callback for logging
    this.defaultCallbacks.set('log', (match: string, context: any, position: number) => {
    // eslint-disable-next-line no-console
      console.log(`Match: "${match}" at position ${position}`, context);
      return match;
    });

    // Default callback for collecting values
    this.defaultCallbacks.set('collect', (match: string, context: any, position: number) => {
      if (!context.collected) {
        context.collected = [];
      }
      context.collected.push(match);
      return match;
    });

    // Default callback for setting variables
    this.defaultCallbacks.set('set', (match: string, context: any, position: number, varName: string) => {
      if (!context.variables) {
        context.variables = {};
      }
      context.variables[varName] = match;
      return match;
    });

    // Default callback for conditional execution
    this.defaultCallbacks.set('when', (match: string, context: any, position: number, condition: string, thenCallback: string) => {
      let conditionMet = false;

      // Evaluate condition
      try {
        // Simple condition evaluation
        if (condition.includes('==')) {
          const [left, right] = condition.split('==').map(s => s.trim());
          const leftValue = context.variables?.[left] || left;
          const rightValue = context.variables?.[right] || right;
          conditionMet = leftValue === rightValue;
        } else if (condition.includes('!=')) {
          const [left, right] = condition.split('!=').map(s => s.trim());
          const leftValue = context.variables?.[left] || left;
          const rightValue = context.variables?.[right] || right;
          conditionMet = leftValue !== rightValue;
        } else if (condition.includes('>')) {
          const [left, right] = condition.split('>').map(s => s.trim());
          const leftValue = parseFloat(context.variables?.[left] || left);
          const rightValue = parseFloat(context.variables?.[right] || right);
          conditionMet = leftValue > rightValue;
        } else if (condition.includes('<')) {
          const [left, right] = condition.split('<').map(s => s.trim());
          const leftValue = parseFloat(context.variables?.[left] || left);
          const rightValue = parseFloat(context.variables?.[right] || right);
          conditionMet = leftValue < rightValue;
        } else {
          // Treat as boolean variable
          conditionMet = Boolean(context.variables?.[condition] || condition === 'true');
        }
      } catch (error) {
    // eslint-disable-next-line no-console
        console.error('Error evaluating condition:', error);
        conditionMet = false;
      }

      // Execute callback if condition is met
      if (conditionMet && this.callbacks.has(thenCallback)) {
        return this.callbacks.get(thenCallback)!(match, context, position);
      }

      return match;
    });
  }

  /**
   * Registers a callback function.
   * @param name The name of the callback
   * @param callback The callback function
   */
  public registerCallback(name: string, callback: Function): void {
    this.callbacks.set(name, callback);
  }

  /**
   * Gets a callback function by name.
   * @param name The name of the callback
   * @returns The callback function or null if not found
   */
  public getCallback(name: string): Function | null {
    return this.callbacks.get(name) || this.defaultCallbacks.get(name) || null;
  }

  /**
   * Executes a callback function by name.
   * @param name The name of the callback
   * @param match The matched text
   * @param context The parsing context
   * @param position The position in the source code
   * @param args Additional arguments for the callback
   * @returns The result of the callback
   */
  public executeCallback(name: string, match: string, context: any, position: number, ...args: any[]): any {
    const callback = this.getCallback(name);
    if (callback) {
      return callback(match, context, position, ...args);
    }
    return match;
  }

  /**
   * Parses a callback string and executes the corresponding function.
   * @param callbackStr The callback string (e.g., "functionName($1, $2)")
   * @param match The matched text
   * @param context The parsing context
   * @param position The position in the source code
   * @param captures Captured groups from the match
   * @returns The result of the callback
   */
  // eslint-disable-next-line max-len
  public parseAndExecuteCallback(callbackStr: string, match: string, context: any, position: number, captures: string[] = []): any {
    // Parse function name and arguments
    const functionMatch = callbackStr.match(/([a-zA-Z0-9_]+)\s*\((.*)\)/);
    if (!functionMatch) {
      // Simple function name without arguments
      return this.executeCallback(callbackStr, match, context, position);
    }

    const functionName = functionMatch[1];
    const argsStr = functionMatch[2];

    // Parse arguments
    const args: any[] = [];
    const argMatches = argsStr.match(/(?:[^,()]|\([^()]*\))+/g) || [];

    for (const argStr of argMatches) {
      const trimmedArg = argStr.trim();

      if (trimmedArg.startsWith('$')) {
        // Capture group reference
        const index = parseInt(trimmedArg.substring(1), 10);
        if (index >= 0 && index < captures.length) {
          args.push(captures[index]);
        } else {
          args.push(null);
        }
      } else if (trimmedArg.startsWith('"') && trimmedArg.endsWith('"')) {
        // String literal
        args.push(trimmedArg.substring(1, trimmedArg.length - 1));
      } else if (trimmedArg === 'true' || trimmedArg === 'false') {
        // Boolean literal
        args.push(trimmedArg === 'true');
      } else if (!isNaN(Number(trimmedArg))) {
        // Number literal
        args.push(Number(trimmedArg));
      } else {
        // Variable reference or function name
        args.push(context.variables?.[trimmedArg] || trimmedArg);
      }
    }

    return this.executeCallback(functionName, match, context, position, ...args);
  }
}
