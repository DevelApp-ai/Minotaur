/**
 * Represents a grammar definition with productions and terminals.
 */
export class Grammar {
  private name: string;
  private productions: Production[];
  private validStartTerminals: Terminal[];
  private startProductions: Production[];
  private tokenSplitterType: TokenSplitterType;
  private regexTokenSplitter: string;

  // Inheritance support properties
  private inheritable: boolean;
  private formatType: GrammarFormatType;
  private baseGrammars: string[];
  private importSemantics: boolean;
  private coordinateTokens: boolean;
  private precedenceRules: PrecedenceRule[];
  private errorRecoveryStrategy: ErrorRecoveryStrategy;
  private semanticActionTemplates: Map<string, SemanticActionTemplate>;

  // Additional compatibility properties
  private _content: string;
  private _inheritedRules: string[];
  private _overriddenRules: string[];

  /**
   * Creates a new Grammar instance.
   * @param name The name of the grammar
   */
  constructor(name: string) {
    this.name = name;
    this.productions = [];
    this.validStartTerminals = [];
    this.startProductions = [];
    this.tokenSplitterType = TokenSplitterType.None;
    this.regexTokenSplitter = '';

    // Initialize inheritance properties
    this.inheritable = false;
    this.formatType = GrammarFormatType.CEBNF;
    this.baseGrammars = [];
    this.importSemantics = false;
    this.coordinateTokens = false;
    this.precedenceRules = [];
    this.errorRecoveryStrategy = new ErrorRecoveryStrategy();
    this.semanticActionTemplates = new Map<string, SemanticActionTemplate>();

    // Initialize compatibility properties
    this._content = '';
    this._inheritedRules = [];
    this._overriddenRules = [];
  }

  /**
   * Check if we're running in a Node.js environment
   */
  private static isNodeEnvironment(): boolean {
    return typeof process !== 'undefined' && process.versions && !!process.versions.node;
  }

  /**
   * Load a grammar from a file (Node.js only)
   * @param filePath Path to the grammar file
   * @returns Promise<Grammar> The loaded grammar
   */
  public static async loadFromFile(filePath: string): Promise<Grammar> {
    // Check if we're in a Node.js environment
    if (!Grammar.isNodeEnvironment()) {
      // eslint-disable-next-line no-console
      console.warn('Warning: loadFromFile is not supported in browser environment, using basic grammar');
      const grammar = new Grammar('Python311');
      return grammar;
    }

    try {
      // Only require fs and path in Node.js environment
      const fsModule = await import('fs');
      const fsPromises = fsModule.promises;
      const pathModule = await import('path');

      // Read the grammar file content
      const fileName = pathModule.basename(filePath);
      const content = await fsPromises.readFile(filePath, 'utf-8');

      // Use GrammarInterpreter to parse the content
      const { GrammarContainer } = require('./GrammarContainer');
      const { GrammarInterpreter } = require('./GrammarInterpreter');
      
      const container = new GrammarContainer();
      const interpreter = new GrammarInterpreter(container);
      
      // Parse the grammar content
      const grammar = interpreter.parseGrammar(content, fileName);
      
      return grammar;
    } catch (error) {
      // If file loading fails, return a basic grammar for now
      // eslint-disable-next-line no-console
      console.warn(`Warning: Could not load grammar from ${filePath}, using basic grammar`);
      const grammar = new Grammar('Python311');
      return grammar;
    }
  }

  /**
   * Load a grammar from content string (browser-compatible)
   * @param content The grammar content as string
   * @param fileName Optional filename for reference
   * @returns Promise<Grammar> The loaded grammar
   */
  public static async loadFromContent(content: string, fileName: string = 'grammar'): Promise<Grammar> {
    try {
      // Use GrammarInterpreter to parse the content
      const { GrammarContainer } = require('./GrammarContainer');
      const { GrammarInterpreter } = require('./GrammarInterpreter');
      
      const container = new GrammarContainer();
      const interpreter = new GrammarInterpreter(container);
      
      // Parse the grammar content
      const grammar = interpreter.parseGrammar(content, fileName);
      
      return grammar;
    } catch (error) {
      // If parsing fails, return a basic grammar
      // eslint-disable-next-line no-console
      console.warn('Warning: Could not parse grammar content, using basic grammar');
      const grammar = new Grammar('Python311');
      return grammar;
    }
  }

  /**
   * Gets the grammar content.
   */
  public get content(): string {
    return this._content;
  }

  /**
   * Sets the grammar content.
   */
  public set content(value: string) {
    this._content = value;
  }

  /**
   * Gets the format type.
   */
  public get format(): GrammarFormatType {
    return this.formatType;
  }

  /**
   * Sets the format type.
   */
  public set format(value: GrammarFormatType) {
    this.formatType = value;
  }

  /**
   * Gets the inherited rules.
   */
  public get inheritedRules(): string[] {
    return [...this._inheritedRules];
  }

  /**
   * Sets the inherited rules.
   */
  public set inheritedRules(rules: string[]) {
    this._inheritedRules = [...rules];
  }

  /**
   * Gets the overridden rules.
   */
  public get overriddenRules(): string[] {
    return [...this._overriddenRules];
  }

  /**
   * Sets the overridden rules.
   */
  public set overriddenRules(rules: string[]) {
    this._overriddenRules = [...rules];
  }

  /**
   * Gets the name of the grammar.
   */
  public getName(): string {
    return this.name;
  }

  /**
   * Gets the productions in the grammar.
   */
  public getProductions(): Production[] {
    return this.productions;
  }

  /**
   * Adds a production to the grammar.
   * @param production The production to add
   */
  public addProduction(production: Production): void {
    this.productions.push(production);
  }

  /**
   * Gets the valid start terminals.
   */
  public getValidStartTerminals(): Terminal[] {
    return this.validStartTerminals;
  }

  /**
   * Adds a valid start terminal.
   * @param terminal The terminal to add
   */
  public addValidStartTerminal(terminal: Terminal): void {
    this.validStartTerminals.push(terminal);
  }

  /**
   * Gets the start productions.
   */
  public getStartProductions(): Production[] {
    return this.startProductions;
  }

  /**
   * Adds a start production.
   * @param production The production to add
   */
  public addStartProduction(production: Production): void {
    this.startProductions.push(production);
  }

  /**
   * Gets the token splitter type.
   */
  public getTokenSplitterType(): TokenSplitterType {
    return this.tokenSplitterType;
  }

  /**
   * Sets the token splitter type.
   * @param type The token splitter type
   */
  public setTokenSplitterType(type: TokenSplitterType): void {
    this.tokenSplitterType = type;
  }

  /**
   * Gets the regex token splitter.
   */
  public getRegexTokenSplitter(): string {
    return this.regexTokenSplitter;
  }

  /**
   * Sets the regex token splitter.
   * @param regex The regex token splitter
   */
  public setRegexTokenSplitter(regex: string): void {
    this.regexTokenSplitter = regex;
  }

  // ============================================================================
  // INHERITANCE SUPPORT METHODS
  // ============================================================================

  /**
   * Gets whether this grammar is inheritable.
   */
  public isInheritable(): boolean {
    return this.inheritable;
  }

  /**
   * Sets whether this grammar is inheritable.
   * @param inheritable Whether the grammar is inheritable
   */
  public setInheritable(inheritable: boolean): void {
    this.inheritable = inheritable;
  }

  /**
   * Gets the format type of this grammar.
   */
  public getFormatType(): GrammarFormatType {
    return this.formatType;
  }

  /**
   * Sets the format type of this grammar.
   * @param formatType The format type
   */
  public setFormatType(formatType: GrammarFormatType): void {
    this.formatType = formatType;
  }

  /**
   * Gets the base grammars this grammar inherits from.
   */
  public getBaseGrammars(): string[] {
    return [...this.baseGrammars];
  }

  /**
   * Adds a base grammar to inherit from.
   * @param baseGrammarName The name of the base grammar
   */
  public addBaseGrammar(baseGrammarName: string): void {
    if (!this.baseGrammars.includes(baseGrammarName)) {
      this.baseGrammars.push(baseGrammarName);
    }
  }

  /**
   * Sets the base grammars to inherit from.
   * @param baseGrammars Array of base grammar names
   */
  public setBaseGrammars(baseGrammars: string[]): void {
    this.baseGrammars = [...baseGrammars];
  }

  /**
   * Gets whether semantic actions should be imported from base grammars.
   */
  public getImportSemantics(): boolean {
    return this.importSemantics;
  }

  /**
   * Sets whether semantic actions should be imported from base grammars.
   * @param importSemantics Whether to import semantics
   */
  public setImportSemantics(importSemantics: boolean): void {
    this.importSemantics = importSemantics;
  }

  /**
   * Gets whether tokens should be coordinated between lexer and parser.
   */
  public getCoordinateTokens(): boolean {
    return this.coordinateTokens;
  }

  /**
   * Sets whether tokens should be coordinated between lexer and parser.
   * @param coordinateTokens Whether to coordinate tokens
   */
  public setCoordinateTokens(coordinateTokens: boolean): void {
    this.coordinateTokens = coordinateTokens;
  }

  /**
   * Gets the precedence rules for this grammar.
   */
  public getPrecedenceRules(): PrecedenceRule[] {
    return [...this.precedenceRules];
  }

  /**
   * Adds a precedence rule.
   * @param rule The precedence rule to add
   */
  public addPrecedenceRule(rule: PrecedenceRule): void {
    this.precedenceRules.push(rule);
  }

  /**
   * Sets the precedence rules.
   * @param rules The precedence rules
   */
  public setPrecedenceRules(rules: PrecedenceRule[]): void {
    this.precedenceRules = [...rules];
  }

  /**
   * Gets the error recovery strategy.
   */
  public getErrorRecoveryStrategy(): ErrorRecoveryStrategy {
    return this.errorRecoveryStrategy;
  }

  /**
   * Sets the error recovery strategy.
   * @param strategy The error recovery strategy
   */
  public setErrorRecoveryStrategy(strategy: ErrorRecoveryStrategy): void {
    this.errorRecoveryStrategy = strategy;
  }

  /**
   * Gets a semantic action template by name.
   * @param name The template name
   */
  public getSemanticActionTemplate(name: string): SemanticActionTemplate | undefined {
    return this.semanticActionTemplates.get(name);
  }

  /**
   * Adds a semantic action template.
   * @param name The template name
   * @param template The template
   */
  public addSemanticActionTemplate(name: string, template: SemanticActionTemplate): void {
    this.semanticActionTemplates.set(name, template);
  }

  /**
   * Gets all semantic action templates.
   */
  public getSemanticActionTemplates(): Map<string, SemanticActionTemplate> {
    return new Map(this.semanticActionTemplates);
  }

  /**
   * Checks if this grammar inherits from another grammar.
   * @param grammarName The name of the grammar to check
   */
  public inheritsFrom(grammarName: string): boolean {
    return this.baseGrammars.includes(grammarName);
  }

  /**
   * Gets a production by name.
   * @param name The name of the production
   * @returns The production or null if not found
   */
  public getProductionByName(name: string): Production | null {
    for (const production of this.productions) {
      if (production.getName() === name) {
        return production;
      }
    }
    return null;
  }

  /**
   * Creates a string representation of this grammar.
   */
  public toString(): string {
    let result = `Grammar: ${this.name}\n`;
    result += `TokenSplitter: ${TokenSplitterType[this.tokenSplitterType]}`;

    if (this.tokenSplitterType === TokenSplitterType.Regex && this.regexTokenSplitter) {
      result += ` "${this.regexTokenSplitter}"`;
    }

    if (this.inheritable) {
      result += '\nInheritable: true';
    }

    if (this.formatType !== GrammarFormatType.CEBNF) {
      result += `\nFormatType: ${GrammarFormatType[this.formatType]}`;
    }

    if (this.baseGrammars.length > 0) {
      result += `\nInherits: ${this.baseGrammars.join(', ')}`;
    }

    if (this.importSemantics) {
      result += '\nImportSemantics: true';
    }

    if (this.coordinateTokens) {
      result += '\nCoordinateTokens: true';
    }

    result += `\n\n${this.productions.length} productions`;

    return result;
  }
}

/**
 * Enum representing the grammar format type.
 */
export enum GrammarFormatType {
  CEBNF = 'CEBNF',
  ANTLR4 = 'ANTLR4',
  Bison = 'Bison',
  Flex = 'Flex',
  Yacc = 'Yacc',
  Lex = 'Lex',
  Minotaur = 'Minotaur'
}

/**
 * Represents a precedence rule for operators.
 */
export class PrecedenceRule {
  private level: number;
  private operators: string[];
  private associativity: AssociativityType;
  private description: string;

  constructor(level: number, operators: string[], associativity: AssociativityType, description: string = '') {
    this.level = level;
    this.operators = [...operators];
    this.associativity = associativity;
    this.description = description;
  }

  public getLevel(): number {
    return this.level;
  }

  public getOperators(): string[] {
    return [...this.operators];
  }

  public getAssociativity(): AssociativityType {
    return this.associativity;
  }

  public getDescription(): string {
    return this.description;
  }

  public hasOperator(operator: string): boolean {
    return this.operators.includes(operator);
  }
}

/**
 * Enum representing associativity types.
 */
export enum AssociativityType {
  Left = 'left',
  Right = 'right',
  None = 'none'
}

/**
 * Represents an error recovery strategy.
 */
export class ErrorRecoveryStrategy {
  private strategy: string;
  private syncTokens: string[];
  private recoveryActions: Map<string, string>;
  private reportingLevel: string;

  constructor() {
    this.strategy = 'automatic';
    this.syncTokens = [];
    this.recoveryActions = new Map<string, string>();
    this.reportingLevel = 'basic';
  }

  public getStrategy(): string {
    return this.strategy;
  }

  public setStrategy(strategy: string): void {
    this.strategy = strategy;
  }

  public getSyncTokens(): string[] {
    return [...this.syncTokens];
  }

  public setSyncTokens(tokens: string[]): void {
    this.syncTokens = [...tokens];
  }

  public addSyncToken(token: string): void {
    if (!this.syncTokens.includes(token)) {
      this.syncTokens.push(token);
    }
  }

  public getRecoveryAction(errorType: string): string | undefined {
    return this.recoveryActions.get(errorType);
  }

  public setRecoveryAction(errorType: string, action: string): void {
    this.recoveryActions.set(errorType, action);
  }

  public getReportingLevel(): string {
    return this.reportingLevel;
  }

  public setReportingLevel(level: string): void {
    this.reportingLevel = level;
  }
}

/**
 * Represents a semantic action template.
 */
export class SemanticActionTemplate {
  private name: string;
  private template: string;
  private parameters: string[];
  private returnType: string;
  private description: string;

  constructor(name: string, template: string, parameters: string[] = [], returnType: string = 'void', description: string = '') {
    this.name = name;
    this.template = template;
    this.parameters = [...parameters];
    this.returnType = returnType;
    this.description = description;
  }

  public getName(): string {
    return this.name;
  }

  public getTemplate(): string {
    return this.template;
  }

  public getParameters(): string[] {
    return [...this.parameters];
  }

  public getReturnType(): string {
    return this.returnType;
  }

  public getDescription(): string {
    return this.description;
  }

  public instantiate(args: string[]): string {
    let result = this.template;
    for (let i = 0; i < args.length && i < this.parameters.length; i++) {
      const paramPattern = new RegExp(`\\$\\{${this.parameters[i]}\\}`, 'g');
      result = result.replace(paramPattern, args[i]);
    }
    return result;
  }
}

// Import required classes and interfaces
import { Production } from './Production';
import { Terminal } from './Terminal';
import { TokenSplitterType } from './LexerOptions';

