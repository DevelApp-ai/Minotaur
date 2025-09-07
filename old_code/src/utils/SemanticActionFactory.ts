/**
 * Factory for creating semantic actions from various sources.
 */
export class SemanticActionFactory {
  private templateEngine: TemplateEngine;
  private scriptEngine: ScriptEngine;
  private callbackRegistry: Map<string, Function>;

  /**
   * Creates a new SemanticActionFactory.
   * @param templateEngine The template engine to use
   * @param scriptEngine The script engine to use
   */
  constructor(templateEngine?: TemplateEngine, scriptEngine?: ScriptEngine) {
    this.templateEngine = templateEngine || new SimpleTemplateEngine();
    this.scriptEngine = scriptEngine || new JavaScriptScriptEngine();
    this.callbackRegistry = new Map<string, Function>();
  }

  /**
   * Creates a callback-based semantic action.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param callback The callback function
   * @param callbackName The callback name
   * @returns The semantic action
   */
  public createCallbackAction(
    name: string,
    grammarName: string,
    callback: Function,
    callbackName: string,
  ): SemanticAction {
    const implementation = new CallbackSemanticActionImplementation(callback, callbackName);
    const action = new SemanticAction(name, grammarName, SemanticActionType.Callback, implementation);
    action.setDescription(`Callback action: ${callbackName}`);
    return action;
  }

  /**
   * Creates a template-based semantic action.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param template The template string
   * @returns The semantic action
   */
  public createTemplateAction(
    name: string,
    grammarName: string,
    template: string,
  ): SemanticAction {
    const implementation = new TemplateSemanticActionImplementation(template, this.templateEngine);
    const action = new SemanticAction(name, grammarName, SemanticActionType.Template, implementation);
    action.setDescription('Template action');
    return action;
  }

  /**
   * Creates a script-based semantic action.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param script The script code
   * @param language The script language
   * @returns The semantic action
   */
  public createScriptAction(
    name: string,
    grammarName: string,
    script: string,
    language: ScriptLanguage = ScriptLanguage.JavaScript,
  ): SemanticAction {
    const implementation = new ScriptSemanticActionImplementation(script, language, this.scriptEngine);
    const action = new SemanticAction(name, grammarName, SemanticActionType.Script, implementation);
    action.setDescription(`Script action (${language})`);
    return action;
  }

  /**
   * Creates a native function-based semantic action.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param nativeFunction The native function
   * @param functionName The function name
   * @returns The semantic action
   */
  public createNativeAction(
    name: string,
    grammarName: string,
    nativeFunction: Function,
    functionName: string,
  ): SemanticAction {
    const implementation = new NativeSemanticActionImplementation(nativeFunction, functionName);
    const action = new SemanticAction(name, grammarName, SemanticActionType.Native, implementation);
    action.setDescription(`Native action: ${functionName}`);
    return action;
  }

  /**
   * Creates a composite semantic action.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param actions The actions to combine
   * @param combineStrategy The combine strategy
   * @returns The semantic action
   */
  public createCompositeAction(
    name: string,
    grammarName: string,
    actions: SemanticAction[],
    combineStrategy: CombineStrategy = CombineStrategy.LastResult,
  ): SemanticAction {
    const implementations = actions.map(action => action.getImplementation());
    const implementation = new CompositeSemanticActionImplementation(implementations, combineStrategy);
    const action = new SemanticAction(name, grammarName, SemanticActionType.Callback, implementation);
    action.setDescription(`Composite action (${actions.length} actions, ${combineStrategy} strategy)`);
    return action;
  }

  /**
   * Creates a semantic action from a grammar template.
   * @param name The name of the action
   * @param grammarName The grammar name
   * @param template The semantic action template
   * @param args The template arguments
   * @returns The semantic action
   */
  public createFromTemplate(
    name: string,
    grammarName: string,
    template: SemanticActionTemplate,
    args: string[],
  ): SemanticAction {
    // Create a template-based implementation using the template's content
    return this.createTemplateAction(name, grammarName, template.getTemplate());
  }

  /**
   * Creates semantic actions from grammar format-specific patterns.
   * @param grammarName The grammar name
   * @param formatType The grammar format type
   * @param actionDefinitions The action definitions
   * @returns Array of semantic actions
   */
  public createFromFormatSpecificPatterns(
    grammarName: string,
    formatType: GrammarFormatType,
    actionDefinitions: Map<string, string>,
  ): SemanticAction[] {
    const actions: SemanticAction[] = [];

    for (const [actionName, definition] of actionDefinitions) {
      let action: SemanticAction;

      switch (formatType) {
        case GrammarFormatType.ANTLR4:
          action = this.createANTLR4Action(actionName, grammarName, definition);
          break;

        case GrammarFormatType.Bison:
          action = this.createBisonAction(actionName, grammarName, definition);
          break;

        case GrammarFormatType.Flex:
          action = this.createFlexAction(actionName, grammarName, definition);
          break;

        case GrammarFormatType.Yacc:
          action = this.createYaccAction(actionName, grammarName, definition);
          break;

        case GrammarFormatType.Lex:
          action = this.createLexAction(actionName, grammarName, definition);
          break;

        default:
          action = this.createTemplateAction(actionName, grammarName, definition);
          break;
      }

      actions.push(action);
    }

    return actions;
  }

  /**
   * Creates an ANTLR4-specific semantic action.
   * @param name The action name
   * @param grammarName The grammar name
   * @param definition The action definition
   * @returns The semantic action
   */
  private createANTLR4Action(name: string, grammarName: string, definition: string): SemanticAction {
    // ANTLR4 actions are typically Java/C#/Python code
    const language = this.detectScriptLanguage(definition);
    return this.createScriptAction(name, grammarName, definition, language);
  }

  /**
   * Creates a Bison-specific semantic action.
   * @param name The action name
   * @param grammarName The grammar name
   * @param definition The action definition
   * @returns The semantic action
   */
  private createBisonAction(name: string, grammarName: string, definition: string): SemanticAction {
    // Bison actions are C code
    return this.createScriptAction(name, grammarName, definition, ScriptLanguage.C);
  }

  /**
   * Creates a Flex-specific semantic action.
   * @param name The action name
   * @param grammarName The grammar name
   * @param definition The action definition
   * @returns The semantic action
   */
  private createFlexAction(name: string, grammarName: string, definition: string): SemanticAction {
    // Flex actions are C code
    return this.createScriptAction(name, grammarName, definition, ScriptLanguage.C);
  }

  /**
   * Creates a Yacc-specific semantic action.
   * @param name The action name
   * @param grammarName The grammar name
   * @param definition The action definition
   * @returns The semantic action
   */
  private createYaccAction(name: string, grammarName: string, definition: string): SemanticAction {
    // Yacc actions are C code
    return this.createScriptAction(name, grammarName, definition, ScriptLanguage.C);
  }

  /**
   * Creates a Lex-specific semantic action.
   * @param name The action name
   * @param grammarName The grammar name
   * @param definition The action definition
   * @returns The semantic action
   */
  private createLexAction(name: string, grammarName: string, definition: string): SemanticAction {
    // Lex actions are C code
    return this.createScriptAction(name, grammarName, definition, ScriptLanguage.C);
  }

  /**
   * Detects the script language from code content.
   * @param code The code content
   * @returns The detected script language
   */
  private detectScriptLanguage(code: string): ScriptLanguage {
    // Simple heuristics for language detection
    if (code.includes('public class') || code.includes('System.out.println')) {
      return ScriptLanguage.Java;
    }
    if (code.includes('using System') || code.includes('Console.WriteLine')) {
      return ScriptLanguage.CSharp;
    }
    if (code.includes('def ') || code.includes('print(')) {
      return ScriptLanguage.Python;
    }
    if (code.includes('#include') || code.includes('printf(')) {
      return ScriptLanguage.C;
    }

    // Default to JavaScript
    return ScriptLanguage.JavaScript;
  }

  /**
   * Registers a callback function.
   * @param name The callback name
   * @param callback The callback function
   */
  public registerCallback(name: string, callback: Function): void {
    this.callbackRegistry.set(name, callback);
  }

  /**
   * Gets a registered callback.
   * @param name The callback name
   * @returns The callback function or null if not found
   */
  public getCallback(name: string): Function | null {
    return this.callbackRegistry.get(name) || null;
  }

  /**
   * Creates a semantic action from a registered callback.
   * @param actionName The action name
   * @param grammarName The grammar name
   * @param callbackName The callback name
   * @returns The semantic action or null if callback not found
   */
  public createFromCallback(actionName: string, grammarName: string, callbackName: string): SemanticAction | null {
    const callback = this.getCallback(callbackName);
    if (!callback) {
      return null;
    }

    return this.createCallbackAction(actionName, grammarName, callback, callbackName);
  }

  /**
   * Creates semantic actions from inheritance templates.
   * @param grammarName The grammar name
   * @param baseGrammarName The base grammar name
   * @param actionManager The semantic action manager
   * @returns Array of inherited semantic actions
   */
  public createInheritedActions(
    grammarName: string,
    baseGrammarName: string,
    actionManager: SemanticActionManager,
  ): SemanticAction[] {
    const inheritedActions: SemanticAction[] = [];
    const baseActions = actionManager.getDirectActions(baseGrammarName);

    for (const [actionName, baseAction] of baseActions) {
      // Create a copy of the base action for the derived grammar
      const inheritedAction = new SemanticAction(
        actionName,
        grammarName,
        baseAction.getActionType(),
        baseAction.getImplementation(),
      );

      // Copy properties
      inheritedAction.setReturnType(baseAction.getReturnType());
      inheritedAction.setDescription(`Inherited from ${baseGrammarName}: ${baseAction.getDescription()}`);

      // Copy parameters
      for (const param of baseAction.getParameters()) {
        inheritedAction.addParameter(param);
      }

      inheritedActions.push(inheritedAction);
    }

    return inheritedActions;
  }

  /**
   * Sets the template engine.
   * @param templateEngine The template engine
   */
  public setTemplateEngine(templateEngine: TemplateEngine): void {
    this.templateEngine = templateEngine;
  }

  /**
   * Sets the script engine.
   * @param scriptEngine The script engine
   */
  public setScriptEngine(scriptEngine: ScriptEngine): void {
    this.scriptEngine = scriptEngine;
  }

  /**
   * Gets the template engine.
   */
  public getTemplateEngine(): TemplateEngine {
    return this.templateEngine;
  }

  /**
   * Gets the script engine.
   */
  public getScriptEngine(): ScriptEngine {
    return this.scriptEngine;
  }
}

// Import required classes and interfaces
import {
  SemanticAction,
  SemanticActionType,
  SemanticActionManager,
  SemanticActionImplementation,
} from './SemanticActionManager';
import { SemanticActionTemplate } from './Grammar';
import {
  CallbackSemanticActionImplementation,
  TemplateSemanticActionImplementation,
  ScriptSemanticActionImplementation,
  NativeSemanticActionImplementation,
  CompositeSemanticActionImplementation,
  CombineStrategy,
  ScriptLanguage,
  TemplateEngine,
  ScriptEngine,
  SimpleTemplateEngine,
  JavaScriptScriptEngine,
} from './SemanticActionImplementations';
import { GrammarFormatType } from './Grammar';

