// Mock Grammar module for testing
import { Production } from '../../utils/Production';

export interface Grammar {
  name: string;
  rules: Rule[];
  parseTree?: any;
}

export class Grammar {
  name: string;
  rules: Rule[];
  parseTree?: any;
  private embeddedLanguages: Set<string> = new Set();
  private importedGrammars: Set<string> = new Set();
  private references?: Array<{ target: string; type: string }>;
  private symbolDefinitions?: Map<string, any>;
  
  // Additional properties for compatibility with utils/Grammar
  format?: string;
  content?: string;
  private _baseGrammars: string[] = [];

  constructor(name: string) {
    this.name = name;
    this.rules = [];
  }

  addRule(rule: Rule): void {
    this.rules.push(rule);
  }

  getRule(name: string): Rule | undefined {
    return this.rules.find(rule => rule.name === name);
  }

  getName(): string {
    return this.name;
  }

  getGrammarName(): string {
    return this.name;
  }

  setName(name: string): void {
    this.name = name;
  }

  getEmbeddedLanguages(): Set<string> {
    return this.embeddedLanguages;
  }

  getImportedGrammars(): Set<string> {
    return this.importedGrammars;
  }

  addEmbeddedLanguage(language: string): void {
    this.embeddedLanguages.add(language);
  }

  addImportedGrammar(grammar: string): void {
    this.importedGrammars.add(grammar);
  }

  getValidStartTerminals(): any[] {
    return [];
  }

  getReferences(): Map<string, any> {
    return new Map();
  }

  getSymbolDefinitions(): Map<string, any> {
    return new Map();
  }

  setEmbeddedLanguages(languages: string[]): void {
    this.embeddedLanguages = new Set(languages);
  }

  setIsContextSensitive(value: boolean): void {
    // Implementation for context sensitivity
  }

  setIsEmbeddedLanguage(value: boolean): void {
    // Implementation for embedded language flag
  }

  setParentContext(context: string): void {
    // Implementation for parent context
  }

  setSymbolTableSharing(strategy: any): void {
    // Implementation for symbol table sharing
  }

  setCrossLanguageValidation(value: boolean): void {
    // Implementation for cross-language validation
  }

  hasEmbeddedLanguage(language: string): boolean {
    return this.embeddedLanguages.has(language);
  }

  addValidationRule(name: string, rule: any): void {
    // Implementation for validation rules
  }

  setCompositionStrategy(strategy: any): void {
    // Implementation for composition strategy
  }

  getSymbolTableSharing(): any {
    return null;
  }

  getProductions(): Rule[] {
    // Return the rules as productions
    return this.rules;
  }

  getSymbols(): Map<string, any> {
    // Extract symbols from rules and symbol definitions
    const symbols = new Map<string, any>();
    
    // Add symbols from symbol definitions
    if (this.symbolDefinitions) {
      for (const [name, definition] of this.symbolDefinitions) {
        symbols.set(name, definition);
      }
    }
    
    // Extract symbols from rules
    this.rules.forEach(rule => {
      // Simple symbol extraction from rule names
      symbols.set(rule.name, {
        type: rule.type || 'rule',
        scope: 'grammar',
        definition: rule.definition
      });
    });
    
    return symbols;
  }

  getValidationRules(): any[] {
    // Return validation rules with proper structure
    return [
      {
        name: 'syntax_check',
        rule: 'validate_syntax',
        sourceLanguage: this.name,
        targetLanguage: 'any',
        severity: ValidationSeverity.ERROR
      }
    ];
  }

  getBaseGrammars(): string[] {
    // Return base grammars for inheritance - placeholder implementation
    return [];
  }

  getIsContextSensitive(): boolean {
    // Return context sensitivity flag - placeholder implementation
    return false;
  }

  getCrossLanguageValidation(): boolean {
    // Return cross-language validation flag - check if we have embedded languages
    return this.embeddedLanguages.size > 0;
  }
  
  /**
   * Gets the base grammars for inheritance (private property access).
   */
  get baseGrammars(): string[] {
    return [...this._baseGrammars];
  }

  getTokenSplitter(): TokenSplitter | null {
    // Return a basic token splitter implementation
    return {
      split: (input: string): string[] => {
        // Basic tokenization by whitespace and common delimiters
        return input.split(/[\s,;(){}[\]]+/).filter(token => token.length > 0);
      }
    };
  }

  addReference(target: string, options: { type: string }): void {
    // Add reference tracking
    if (!this.references) {
      this.references = [];
    }
    this.references.push({ target, type: options.type });
  }

  addSymbolDefinition(name: string, options: { type: string; scope: string }): void {
    // Add symbol definition tracking
    if (!this.symbolDefinitions) {
      this.symbolDefinitions = new Map();
    }
    this.symbolDefinitions.set(name, options);
  }
}

export interface Rule {
  name: string;
  definition: string;
  type: string;
}

export interface GrammarRule {
  name: string;
  definition: string;
  type: string;
  symbols?: string[];
  productions?: string[];
  metadata?: Map<string, any>;
}

export interface TokenSplitter {
  split(input: string): string[];
}

export enum SymbolTableSharingStrategy {
  GLOBAL = 'global',
  LOCAL = 'local',
  HIERARCHICAL = 'hierarchical',
}

export enum CompositionStrategy {
  MERGE = 'merge',
  OVERRIDE = 'override',
  EXTEND = 'extend',
}

export interface SymbolDefinition {
  name: string;
  type: SymbolType;
  scope: string;
}

export interface ReferenceDefinition {
  target: string;
  resolution: string;
}

export interface ValidationRule {
  name: string;
  rule: string;
  sourceLanguage: string;
  targetLanguage: string;
  severity: ValidationSeverity;
}

export enum SymbolType {
  VARIABLE = 'variable',
  FUNCTION = 'function',
  CLASS = 'class',
  IDENTIFIER = 'identifier',
}

export enum ValidationSeverity {
  ERROR = 'error',
  WARNING = 'warning',
  INFO = 'info',
}

