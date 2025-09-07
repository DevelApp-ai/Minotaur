/**
 * Callback-based semantic action implementation.
 */
export class CallbackSemanticActionImplementation implements SemanticActionImplementation {
  private callback: Function;
  private callbackName: string;

  /**
   * Creates a new CallbackSemanticActionImplementation.
   * @param callback The callback function
   * @param callbackName The name of the callback
   */
  constructor(callback: Function, callbackName: string) {
    this.callback = callback;
    this.callbackName = callbackName;
  }

  /**
   * Executes the callback.
   * @param context The execution context
   * @param args The arguments
   * @returns The result of the callback
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    try {
      return this.callback.apply(context, args);
    } catch (error) {
      throw new SemanticActionError(
        // eslint-disable-next-line max-len
        `Callback execution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
        this.callbackName,
        context.getGrammarName(),
        context.getProductionName(),
      );
    }
  }

  /**
   * Gets the callback function.
   */
  public getCallback(): Function {
    return this.callback;
  }

  /**
   * Gets the callback name.
   */
  public getCallbackName(): string {
    return this.callbackName;
  }
}

/**
 * Template-based semantic action implementation.
 */
export class TemplateSemanticActionImplementation implements SemanticActionImplementation {
  private template: string;
  private templateEngine: TemplateEngine;

  /**
   * Creates a new TemplateSemanticActionImplementation.
   * @param template The template string
   * @param templateEngine The template engine
   */
  constructor(template: string, templateEngine: TemplateEngine) {
    this.template = template;
    this.templateEngine = templateEngine;
  }

  /**
   * Executes the template.
   * @param context The execution context
   * @param args The arguments
   * @returns The result of the template execution
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    try {
      const templateContext = this.createTemplateContext(context, args);
      return this.templateEngine.render(this.template, templateContext);
    } catch (error) {
      throw new SemanticActionError(
        // eslint-disable-next-line max-len
        `Template execution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
        this.template,
        context.getGrammarName(),
        context.getProductionName(),
      );
    }
  }

  /**
   * Creates the template context from the execution context and arguments.
   * @param context The execution context
   * @param args The arguments
   * @returns The template context
   */
  private createTemplateContext(context: SemanticActionContext, args: any[]): any {
    return {
      grammar: context.getGrammarName(),
      production: context.getProductionName(),
      tokens: context.getMatchedTokens(),
      symbols: Object.fromEntries(context.getSymbolTable()),
      args: args,
      parseState: context.getParseState(),
      metadata: new Map(), // Cannot call getMetadata() without key, so using empty Map
    };
  }

  /**
   * Gets the template string.
   */
  public getTemplate(): string {
    return this.template;
  }
}

/**
 * Script-based semantic action implementation.
 */
export class ScriptSemanticActionImplementation implements SemanticActionImplementation {
  private script: string;
  private scriptLanguage: ScriptLanguage;
  private scriptEngine: ScriptEngine;

  /**
   * Creates a new ScriptSemanticActionImplementation.
   * @param script The script code
   * @param scriptLanguage The script language
   * @param scriptEngine The script engine
   */
  constructor(script: string, scriptLanguage: ScriptLanguage, scriptEngine: ScriptEngine) {
    this.script = script;
    this.scriptLanguage = scriptLanguage;
    this.scriptEngine = scriptEngine;
  }

  /**
   * Executes the script.
   * @param context The execution context
   * @param args The arguments
   * @returns The result of the script execution
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    try {
      const scriptContext = this.createScriptContext(context, args);
      return this.scriptEngine.execute(this.script, scriptContext, this.scriptLanguage);
    } catch (error) {
      throw new SemanticActionError(
        // eslint-disable-next-line max-len
        `Script execution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
        this.script,
        context.getGrammarName(),
        context.getProductionName(),
      );
    }
  }

  /**
   * Creates the script context from the execution context and arguments.
   * @param context The execution context
   * @param args The arguments
   * @returns The script context
   */
  private createScriptContext(context: SemanticActionContext, args: any[]): any {
    return {
      grammar: context.getGrammarName(),
      production: context.getProductionName(),
      tokens: context.getMatchedTokens(),
      symbols: context.getSymbolTable(),
      args: args,
      parseState: context.getParseState(),
      metadata: new Map(), // Cannot call getMetadata() without key
      // Helper functions
      setSymbol: (name: string, value: any) => context.setSymbol(name, value),
      getSymbol: (name: string) => context.getSymbol(name),
      setMetadata: (key: string, value: any) => context.setMetadata(key, value),
      getMetadata: (key: string) => context.getMetadata(key),
    };
  }

  /**
   * Gets the script code.
   */
  public getScript(): string {
    return this.script;
  }

  /**
   * Gets the script language.
   */
  public getScriptLanguage(): ScriptLanguage {
    return this.scriptLanguage;
  }
}

/**
 * Native code semantic action implementation.
 */
export class NativeSemanticActionImplementation implements SemanticActionImplementation {
  private nativeFunction: Function;
  private functionName: string;

  /**
   * Creates a new NativeSemanticActionImplementation.
   * @param nativeFunction The native function
   * @param functionName The name of the function
   */
  constructor(nativeFunction: Function, functionName: string) {
    this.nativeFunction = nativeFunction;
    this.functionName = functionName;
  }

  /**
   * Executes the native function.
   * @param context The execution context
   * @param args The arguments
   * @returns The result of the native function
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    try {
      return this.nativeFunction(context, ...args);
    } catch (error) {
      throw new SemanticActionError(
        // eslint-disable-next-line max-len
        `Native function execution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`,
        this.functionName,
        context.getGrammarName(),
        context.getProductionName(),
      );
    }
  }

  /**
   * Gets the native function.
   */
  public getNativeFunction(): Function {
    return this.nativeFunction;
  }

  /**
   * Gets the function name.
   */
  public getFunctionName(): string {
    return this.functionName;
  }
}

/**
 * Composite semantic action implementation that can chain multiple implementations.
 */
export class CompositeSemanticActionImplementation implements SemanticActionImplementation {
  private implementations: SemanticActionImplementation[];
  private combineStrategy: CombineStrategy;

  /**
   * Creates a new CompositeSemanticActionImplementation.
   * @param implementations The implementations to combine
   * @param combineStrategy The strategy for combining results
   */
  // eslint-disable-next-line max-len
  constructor(implementations: SemanticActionImplementation[], combineStrategy: CombineStrategy = CombineStrategy.LastResult) {
    this.implementations = [...implementations];
    this.combineStrategy = combineStrategy;
  }

  /**
   * Executes all implementations according to the combine strategy.
   * @param context The execution context
   * @param args The arguments
   * @returns The combined result
   */
  public execute(context: SemanticActionContext, args: any[]): any {
    if (this.implementations.length === 0) {
      return null;
    }

    const results: any[] = [];

    for (const implementation of this.implementations) {
      try {
        const result = implementation.execute(context, args);
        results.push(result);
      } catch (error) {
        if (this.combineStrategy === CombineStrategy.FailFast) {
          throw error;
        }
        results.push(error);
      }
    }

    return this.combineResults(results);
  }

  /**
   * Combines the results according to the strategy.
   * @param results The results to combine
   * @returns The combined result
   */
  private combineResults(results: any[]): any {
    switch (this.combineStrategy) {
      case CombineStrategy.FirstResult:
        return results[0];

      case CombineStrategy.LastResult:
        return results[results.length - 1];

      case CombineStrategy.AllResults:
        return results;

      case CombineStrategy.MergeObjects:
        return results.reduce((merged, result) => {
          if (typeof result === 'object' && result !== null) {
            return { ...merged, ...result };
          }
          return merged;
        }, {});

      case CombineStrategy.ConcatenateStrings:
        return results.map(r => String(r)).join('');

      case CombineStrategy.SumNumbers:
        return results.reduce((sum, result) => {
          const num = Number(result);
          return isNaN(num) ? sum : sum + num;
        }, 0);

      default:
        return results[results.length - 1];
    }
  }

  /**
   * Adds an implementation to the composite.
   * @param implementation The implementation to add
   */
  public addImplementation(implementation: SemanticActionImplementation): void {
    this.implementations.push(implementation);
  }

  /**
   * Gets all implementations.
   */
  public getImplementations(): SemanticActionImplementation[] {
    return [...this.implementations];
  }
}

/**
 * Enum representing combine strategies for composite implementations.
 */
export enum CombineStrategy {
  FirstResult = 'first',
  LastResult = 'last',
  AllResults = 'all',
  MergeObjects = 'merge',
  ConcatenateStrings = 'concat',
  SumNumbers = 'sum',
  FailFast = 'failfast'
}

/**
 * Enum representing script languages.
 */
export enum ScriptLanguage {
  JavaScript = 'javascript',
  TypeScript = 'typescript',
  Python = 'python',
  CSharp = 'csharp',
  Java = 'java',
  C = 'c'
}

/**
 * Interface for template engines.
 */
export interface TemplateEngine {
  render(template: string, context: any): string;
}

/**
 * Interface for script engines.
 */
export interface ScriptEngine {
  execute(script: string, context: any, language: ScriptLanguage): any;
}

/**
 * Simple template engine implementation.
 */
export class SimpleTemplateEngine implements TemplateEngine {
  /**
   * Renders a template with simple variable substitution.
   * @param template The template string
   * @param context The context object
   * @returns The rendered string
   */
  public render(template: string, context: any): string {
    let result = template;

    // Replace ${variable} patterns
    result = result.replace(/\$\{([^}]+)\}/g, (match, variable) => {
      const value = this.getNestedProperty(context, variable.trim());
      return value !== undefined ? String(value) : match;
    });

    // Replace {{variable}} patterns
    result = result.replace(/\{\{([^}]+)\}\}/g, (match, variable) => {
      const value = this.getNestedProperty(context, variable.trim());
      return value !== undefined ? String(value) : match;
    });

    return result;
  }

  /**
   * Gets a nested property from an object using dot notation.
   * @param obj The object
   * @param path The property path
   * @returns The property value
   */
  private getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => {
      return current && current[key] !== undefined ? current[key] : undefined;
    }, obj);
  }
}

/**
 * JavaScript script engine implementation.
 */
export class JavaScriptScriptEngine implements ScriptEngine {
  /**
   * Executes JavaScript code.
   * @param script The script code
   * @param context The context object
   * @param language The script language (must be JavaScript)
   * @returns The result of the script execution
   */
  public execute(script: string, context: any, language: ScriptLanguage): any {
    if (language !== ScriptLanguage.JavaScript) {
      throw new Error(`Unsupported script language: ${language}`);
    }

    try {
      // Create a function with the context as parameters
      const contextKeys = Object.keys(context);
      const contextValues = contextKeys.map(key => context[key]);

      const func = new Function(...contextKeys, script);
      return func.apply(null, contextValues);
    } catch (error) {
      // eslint-disable-next-line max-len
      throw new Error(`JavaScript execution failed: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`);
    }
  }
}

/**
 * Represents an error in semantic action execution.
 */
export class SemanticActionError extends Error {
  private actionIdentifier: string;
  private grammarName: string;
  private productionName: string;

  /**
   * Creates a new SemanticActionError.
   * @param message The error message
   * @param actionIdentifier The action identifier (callback name, template, etc.)
   * @param grammarName The grammar name
   * @param productionName The production name
   */
  constructor(message: string, actionIdentifier: string, grammarName: string, productionName: string) {
    super(message);
    this.name = 'SemanticActionError';
    this.actionIdentifier = actionIdentifier;
    this.grammarName = grammarName;
    this.productionName = productionName;
  }

  /**
   * Gets the action identifier.
   */
  public getActionIdentifier(): string {
    return this.actionIdentifier;
  }

  /**
   * Gets the grammar name.
   */
  public getGrammarName(): string {
    return this.grammarName;
  }

  /**
   * Gets the production name.
   */
  public getProductionName(): string {
    return this.productionName;
  }

  /**
   * Creates a detailed error message.
   */
  public toString(): string {
    return `${this.name} in ${this.grammarName}.${this.productionName} (${this.actionIdentifier}): ${this.message}`;
  }
}

// Import required classes and interfaces
import { SemanticActionImplementation, SemanticActionContext } from './SemanticActionManager';

