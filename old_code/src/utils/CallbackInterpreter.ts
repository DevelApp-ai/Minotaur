/**
 * Extends the Interpreter class with enhanced callback functionality.
 */
export class CallbackInterpreter extends Interpreter {
  private enhancedCallbackRegistry: CallbackRegistry;

  /**
   * Creates a new CallbackInterpreter instance.
   */
  constructor() {
    super();
    this.enhancedCallbackRegistry = new CallbackRegistry();
  }

  /**
   * Gets the callback registry.
   */
  public getCallbackRegistry(): CallbackRegistry {
    return this.enhancedCallbackRegistry;
  }

  /**
   * Registers a callback function.
   * @param name The name of the callback
   * @param callback The callback function
   */
  public registerCallback(name: string, callback: Function): void {
    this.enhancedCallbackRegistry.registerCallback(name, callback);
  }

  /**
   * Parses a grammar file with callback support.
   * @param content The content of the grammar file
   * @param fileName The name of the file
   * @returns The parsed grammar with callbacks
   */
  public loadGrammarWithCallbacksFromString(content: string, fileName: string): Grammar {
    const grammar = this.loadGrammarFromString(content, fileName);

    // Process productions for callbacks
    this.processGrammarCallbacks(grammar, content);

    return grammar;
  }

  /**
   * Processes grammar productions to extract and set callbacks.
   * @param grammar The grammar to process
   * @param content The original grammar content
   */
  private processGrammarCallbacks(grammar: Grammar, content: string): void {
    const lines = content.split('\n');

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();

      // Look for callback definitions
      const callbackMatch = line.match(/(.+?)\s*::=\s*(.+?)\s*=>\s*\{(.+?)\}/);
      if (callbackMatch) {
        const productionName = callbackMatch[1].trim().replace(/[<>]/g, '');
        const callbackCode = callbackMatch[3].trim();

        // Find the production in the grammar
        const production = grammar.getProductionByName(productionName);
        if (production) {
          // Create a callback function that uses the registry
          const callback = (match: string, context: any, position: number, captures: string[] = []) => {
            // eslint-disable-next-line max-len
            return this.enhancedCallbackRegistry.parseAndExecuteCallback(callbackCode, match, context, position, captures);
          };

          // Set the callback on the production
          production.setCallback(callback);
        }
      }
    }
  }

  /**
   * Parses source code using the specified grammar with callback support.
   * @param grammarName The name of the grammar to use
   * @param sourceCode The source code to parse
   * @param fileName The name of the file
   * @param context Optional initial context for callbacks
   * @returns The parse results and final context
   */
  public async parseSourceCodeWithCallbacks(
    grammarName: string,
    sourceCode: string,
    fileName: string,
    context: any = {},
  ): Promise<{ results: ProductionMatch[], context: any }> {
    // Create a context object if not provided
    const callbackContext = context || {};

    // Set the context in the parser
    const parser = this.getStepParser();
    parser.setCallbackContext(callbackContext);

    // Parse the source code
    const results = await this.parseSourceCode(grammarName, sourceCode, fileName);

    // Return both results and final context
    return {
      results,
      context: callbackContext,
    };
  }

  /**
   * Gets the step parser.
   */
  private getStepParser(): StepParser {
    return (this as any).stepParser;
  }
}

// Import required classes
import { Interpreter } from './Interpreter';
import { CallbackRegistry } from './CallbackRegistry';
import { Grammar } from './Grammar';
import { ProductionMatch } from './ProductionMatch';
import { StepParser } from './StepParser';
