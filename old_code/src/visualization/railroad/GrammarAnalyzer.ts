/**
 * Grammar Analysis Engine for Railroad Diagram Generation
 *
 * This module provides comprehensive analysis of Minotaur grammars
 * for railroad diagram visualization, including inheritance resolution,
 * context-sensitive parsing support, and multi-format grammar handling.
 */

import { Grammar } from '../../core/grammar/Grammar';
import { InheritanceResolver } from '../../utils/InheritanceResolver';
import { ContextManager } from '../../context/ContextManager';
import { GrammarContainer } from '../../utils/GrammarContainer';

/**
 * Represents a grammar rule for railroad diagram visualization
 */
export interface RailroadRule {
    name: string;
    type: 'terminal' | 'non-terminal' | 'choice' | 'sequence' | 'optional' | 'repetition' | 'group';
    content?: string;
    children?: RailroadRule[];
    metadata?: {
        inherited?: boolean;
        overridden?: boolean;
        contextSensitive?: boolean;
        formatSpecific?: string;
        sourceGrammar?: string;
        confidence?: number;
    };
}

/**
 * Represents the complete analyzed grammar structure
 */
export interface AnalyzedGrammar {
    name: string;
    format: string;
    rules: Map<string, RailroadRule>;
    startRule: string;
    inheritance: {
        baseGrammars: string[];
        inheritedRules: string[];
        overriddenRules: string[];
        newRules: string[];
    };
    contextSensitive: {
        contextRules: string[];
        symbolTable: Map<string, any>;
        scopeHierarchy: any[];
    };
    statistics: {
        totalRules: number;
        terminalRules: number;
        nonTerminalRules: number;
        inheritanceDepth: number;
        complexity: number;
    };
}

/**
 * Configuration options for grammar analysis
 */
export interface AnalysisOptions {
    includeInheritance?: boolean;
    resolveContextSensitive?: boolean;
    includeMetadata?: boolean;
    optimizeForVisualization?: boolean;
    maxInheritanceDepth?: number;
    simplifyComplexRules?: boolean;
}

/**
 * Core Grammar Analysis Engine
 */
export class GrammarAnalyzer {
  private inheritanceResolver: InheritanceResolver;
  private contextManager: ContextManager;
  private analysisCache: Map<string, AnalyzedGrammar>;

  constructor() {
    const grammarContainer = new GrammarContainer(); // Create a new grammar container
    this.inheritanceResolver = new InheritanceResolver(grammarContainer);
    this.contextManager = new ContextManager();
    this.analysisCache = new Map();
  }

  /**
     * Analyze a grammar for railroad diagram generation
     */
  public async analyzeGrammar(
    grammar: Grammar,
    options: AnalysisOptions = {},
  ): Promise<AnalyzedGrammar> {
    const cacheKey = this.generateCacheKey(grammar, options);

    if (this.analysisCache.has(cacheKey)) {
      return this.analysisCache.get(cacheKey)!;
    }

    const analyzed = await this.performAnalysis(grammar, options);
    this.analysisCache.set(cacheKey, analyzed);

    return analyzed;
  }

  /**
     * Perform the actual grammar analysis
     */
  private async performAnalysis(
    grammar: Grammar,
    options: AnalysisOptions,
  ): Promise<AnalyzedGrammar> {
    const startTime = Date.now();

    // Step 1: Basic grammar parsing and rule extraction
    const basicRules = await this.extractBasicRules(grammar);

    // Step 2: Resolve inheritance if enabled
    let inheritanceInfo = {
      baseGrammars: [] as string[],
      inheritedRules: [] as string[],
      overriddenRules: [] as string[],
      newRules: [] as string[],
    };

    if (options.includeInheritance) {
      inheritanceInfo = await this.resolveInheritance(grammar, basicRules);
    }

    // Step 3: Analyze context-sensitive features
    let contextInfo = {
      contextRules: [] as string[],
      symbolTable: new Map(),
      scopeHierarchy: [] as any[],
    };

    if (options.resolveContextSensitive) {
      contextInfo = await this.analyzeContextSensitive(grammar, basicRules);
    }

    // Step 4: Optimize for visualization if requested
    if (options.optimizeForVisualization) {
      this.optimizeForVisualization(basicRules, options);
    }

    // Step 5: Calculate statistics
    const statistics = this.calculateStatistics(basicRules, inheritanceInfo);

    const analyzed: AnalyzedGrammar = {
      name: grammar.name || 'Unknown',
      format: grammar.format || 'Minotaur',
      rules: basicRules,
      startRule: this.findStartRule(basicRules),
      inheritance: inheritanceInfo,
      contextSensitive: contextInfo,
      statistics,
    };

    // eslint-disable-next-line no-console
    console.log(`Grammar analysis completed in ${Date.now() - startTime}ms`);
    return analyzed;
  }

  /**
     * Extract basic rules from the grammar
     */
  private async extractBasicRules(grammar: Grammar): Promise<Map<string, RailroadRule>> {
    const rules = new Map<string, RailroadRule>();

    // Parse grammar content based on format
    const grammarContent = grammar.content || '';
    const format = grammar.format || 'Minotaur';

    switch (format.toLowerCase()) {
      case 'antlr4':
        this.parseANTLR4Rules(grammarContent, rules);
        break;
      case 'bison':
      case 'yacc':
        this.parseBisonYaccRules(grammarContent, rules);
        break;
      case 'lex':
      case 'flex':
        this.parseLexFlexRules(grammarContent, rules);
        break;
      default:
        this.parseMinotaurRules(grammarContent, rules);
        break;
    }

    return rules;
  }

  /**
     * Parse ANTLR4 grammar rules
     */
  private parseANTLR4Rules(content: string, rules: Map<string, RailroadRule>): void {
    // ANTLR4 rule pattern: ruleName : alternative1 | alternative2 | ... ;
    const rulePattern = /(\w+)\s*:\s*([^;]+);/g;
    let match;

    while ((match = rulePattern.exec(content)) !== null) {
      const [, ruleName, ruleBody] = match;
      const rule = this.parseRuleBody(ruleBody, 'antlr4');
      rule.name = ruleName;
      rule.metadata = { formatSpecific: 'antlr4' };
      rules.set(ruleName, rule);
    }
  }

  /**
     * Parse Bison/Yacc grammar rules
     */
  private parseBisonYaccRules(content: string, rules: Map<string, RailroadRule>): void {
    // Bison/Yacc rule pattern: ruleName: alternative1 | alternative2 | ... ;
    const rulePattern = /(\w+)\s*:\s*([^;]+);/g;
    let match;

    while ((match = rulePattern.exec(content)) !== null) {
      const [, ruleName, ruleBody] = match;
      const rule = this.parseRuleBody(ruleBody, 'bison');
      rule.name = ruleName;
      rule.metadata = { formatSpecific: 'bison' };
      rules.set(ruleName, rule);
    }
  }

  /**
     * Parse Lex/Flex grammar rules
     */
  private parseLexFlexRules(content: string, rules: Map<string, RailroadRule>): void {
    // Lex/Flex pattern: pattern { action }
    const rulePattern = /^([^\s{]+)\s*\{([^}]*)\}/gm;
    let match;

    while ((match = rulePattern.exec(content)) !== null) {
      const [, pattern, action] = match;
      const rule: RailroadRule = {
        name: `lex_${rules.size + 1}`,
        type: 'terminal',
        content: pattern,
        metadata: { formatSpecific: 'lex' },
      };
      rules.set(rule.name, rule);
    }
  }

  /**
     * Parse Minotaur grammar rules
     */
  private parseMinotaurRules(content: string, rules: Map<string, RailroadRule>): void {
    // Minotaur rule pattern with inheritance support
    const rulePattern = /(\w+)\s*(?:Inherits\s+(\w+))?\s*:\s*([^;]+);/g;
    let match;

    while ((match = rulePattern.exec(content)) !== null) {
      const [, ruleName, inheritsFrom, ruleBody] = match;
      const rule = this.parseRuleBody(ruleBody, 'minotaur');
      rule.name = ruleName;
      rule.metadata = {
        formatSpecific: 'minotaur',
        inherited: !!inheritsFrom,
      };
      rules.set(ruleName, rule);
    }
  }

  /**
     * Parse rule body into railroad structure
     */
  private parseRuleBody(body: string, format: string): RailroadRule {
    // Remove whitespace and split by alternatives
    const alternatives = body.split('|').map(alt => alt.trim());

    if (alternatives.length === 1) {
      return this.parseSequence(alternatives[0], format);
    } else {
      return {
        name: '',
        type: 'choice',
        children: alternatives.map(alt => this.parseSequence(alt, format)),
      };
    }
  }

  /**
     * Parse a sequence of grammar elements
     */
  private parseSequence(sequence: string, format: string): RailroadRule {
    const elements = this.tokenizeSequence(sequence);

    if (elements.length === 1) {
      return this.parseElement(elements[0], format);
    } else {
      return {
        name: '',
        type: 'sequence',
        children: elements.map(elem => this.parseElement(elem, format)),
      };
    }
  }

  /**
     * Parse individual grammar element
     */
  private parseElement(element: string, format: string): RailroadRule {
    element = element.trim();

    // Handle optional elements: element?
    if (element.endsWith('?')) {
      return {
        name: '',
        type: 'optional',
        children: [this.parseElement(element.slice(0, -1), format)],
      };
    }

    // Handle repetition: element+ or element*
    if (element.endsWith('+') || element.endsWith('*')) {
      const isOneOrMore = element.endsWith('+');
      return {
        name: '',
        type: 'repetition',
        children: [this.parseElement(element.slice(0, -1), format)],
        metadata: { confidence: isOneOrMore ? 1 : 0 },
      };
    }

    // Handle grouped elements: (...)
    if (element.startsWith('(') && element.endsWith(')')) {
      const innerContent = element.slice(1, -1);
      return {
        name: '',
        type: 'group',
        children: [this.parseRuleBody(innerContent, format)],
      };
    }

    // Handle terminal vs non-terminal
    const isTerminal = this.isTerminal(element, format);
    return {
      name: element,
      type: isTerminal ? 'terminal' : 'non-terminal',
      content: isTerminal ? element : undefined,
    };
  }

  /**
     * Tokenize a sequence into individual elements
     */
  private tokenizeSequence(sequence: string): string[] {
    const tokens: string[] = [];
    let current = '';
    let parenDepth = 0;
    let inQuotes = false;
    let quoteChar = '';

    for (let i = 0; i < sequence.length; i++) {
      const char = sequence[i];

      if (!inQuotes && (char === '"' || char === "'")) {
        inQuotes = true;
        quoteChar = char;
        current += char;
      } else if (inQuotes && char === quoteChar) {
        inQuotes = false;
        current += char;
      } else if (!inQuotes && char === '(') {
        parenDepth++;
        current += char;
      } else if (!inQuotes && char === ')') {
        parenDepth--;
        current += char;
      } else if (!inQuotes && parenDepth === 0 && /\s/.test(char)) {
        if (current.trim()) {
          tokens.push(current.trim());
          current = '';
        }
      } else {
        current += char;
      }
    }

    if (current.trim()) {
      tokens.push(current.trim());
    }

    return tokens;
  }

  /**
     * Determine if an element is a terminal
     */
  private isTerminal(element: string, format: string): boolean {
    // Quoted strings are always terminals
    if ((element.startsWith('"') && element.endsWith('"')) ||
            (element.startsWith("'") && element.endsWith("'"))) {
      return true;
    }

    // Format-specific terminal detection
    switch (format) {
      case 'antlr4':
        // ANTLR4: uppercase names are typically lexer rules (terminals)
        return /^[A-Z][A-Z0-9_]*$/.test(element);
      case 'bison':
      case 'yacc':
        // Bison/Yacc: tokens are typically uppercase or quoted
        return /^[A-Z][A-Z0-9_]*$/.test(element) || element.includes("'");
      default:
        // Minotaur: assume lowercase are non-terminals, uppercase are terminals
        return /^[A-Z]/.test(element);
    }
  }

  /**
     * Resolve inheritance relationships
     */
  private async resolveInheritance(
    grammar: Grammar,
    rules: Map<string, RailroadRule>,
  ): Promise<any> {
    // Use the existing inheritance resolver
    const resolvedGrammar = this.inheritanceResolver.resolveInheritance(grammar.name);

    // Create inheritance info structure (mock implementation for now)
    const inheritanceInfo = {
      baseGrammars: resolvedGrammar.getBaseGrammars() || [],
      inheritedRules: [], // Would be populated by actual inheritance analysis
      overriddenRules: [], // Would be populated by actual inheritance analysis
    };

    // Mark inherited and overridden rules
    for (const [ruleName, rule] of rules) {
      if (inheritanceInfo.inheritedRules?.includes(ruleName)) {
        rule.metadata = { ...rule.metadata, inherited: true };
      }
      if (inheritanceInfo.overriddenRules?.includes(ruleName)) {
        rule.metadata = { ...rule.metadata, overridden: true };
      }
    }

    return {
      baseGrammars: inheritanceInfo.baseGrammars || [],
      inheritedRules: inheritanceInfo.inheritedRules || [],
      overriddenRules: inheritanceInfo.overriddenRules || [],
      newRules: Array.from(rules.keys()).filter(name =>
        !inheritanceInfo.inheritedRules?.includes(name) &&
                !inheritanceInfo.overriddenRules?.includes(name),
      ),
    };
  }

  /**
     * Analyze context-sensitive features
     */
  private async analyzeContextSensitive(
    grammar: Grammar,
    rules: Map<string, RailroadRule>,
  ): Promise<any> {
    // Use the existing context manager
    const contextInfo = this.contextManager.analyzeContext('grammar', { line: 1, column: 1, offset: 0 });

    // Mark context-sensitive rules
    for (const [ruleName, rule] of rules) {
      if (contextInfo.contextRules?.includes(ruleName)) {
        rule.metadata = { ...rule.metadata, contextSensitive: true };
      }
    }

    return {
      contextRules: contextInfo.contextRules || [],
      symbolTable: contextInfo.symbolTable || new Map(),
      scopeHierarchy: contextInfo.scopeHierarchy || [],
    };
  }

  /**
     * Optimize rules for visualization
     */
  private optimizeForVisualization(
    rules: Map<string, RailroadRule>,
    options: AnalysisOptions,
  ): void {
    if (options.simplifyComplexRules) {
      for (const [ruleName, rule] of rules) {
        this.simplifyComplexRule(rule);
      }
    }
  }

  /**
     * Simplify complex rules for better visualization
     */
  private simplifyComplexRule(rule: RailroadRule): void {
    // Flatten deeply nested sequences
    if (rule.type === 'sequence' && rule.children) {
      const flattenedChildren: RailroadRule[] = [];
      for (const child of rule.children) {
        if (child.type === 'sequence' && child.children) {
          flattenedChildren.push(...child.children);
        } else {
          flattenedChildren.push(child);
        }
      }
      rule.children = flattenedChildren;
    }

    // Recursively simplify children
    if (rule.children) {
      rule.children.forEach(child => this.simplifyComplexRule(child));
    }
  }

  /**
     * Find the start rule of the grammar
     */
  private findStartRule(rules: Map<string, RailroadRule>): string {
    // Look for common start rule names
    const commonStartNames = ['start', 'program', 'compilation_unit', 'document', 'root'];

    for (const name of commonStartNames) {
      if (rules.has(name)) {
        return name;
      }
    }

    // Return the first rule if no common start rule found
    return rules.keys().next().value || '';
  }

  /**
     * Calculate grammar statistics
     */
  private calculateStatistics(
    rules: Map<string, RailroadRule>,
    inheritanceInfo: any,
  ): any {
    let terminalCount = 0;
    let nonTerminalCount = 0;
    let maxDepth = 0;

    for (const rule of rules.values()) {
      if (rule.type === 'terminal') {
        terminalCount++;
      } else {
        nonTerminalCount++;
      }

      const depth = this.calculateRuleDepth(rule);
      maxDepth = Math.max(maxDepth, depth);
    }

    return {
      totalRules: rules.size,
      terminalRules: terminalCount,
      nonTerminalRules: nonTerminalCount,
      inheritanceDepth: inheritanceInfo.baseGrammars.length,
      complexity: this.calculateComplexity(rules),
    };
  }

  /**
     * Calculate the depth of a rule
     */
  private calculateRuleDepth(rule: RailroadRule): number {
    if (!rule.children || rule.children.length === 0) {
      return 1;
    }

    return 1 + Math.max(...rule.children.map(child => this.calculateRuleDepth(child)));
  }

  /**
     * Calculate overall grammar complexity
     */
  private calculateComplexity(rules: Map<string, RailroadRule>): number {
    let complexity = 0;

    for (const rule of rules.values()) {
      complexity += this.calculateRuleComplexity(rule);
    }

    return complexity;
  }

  /**
     * Calculate complexity of a single rule
     */
  private calculateRuleComplexity(rule: RailroadRule): number {
    let complexity = 1;

    if (rule.children) {
      for (const child of rule.children) {
        complexity += this.calculateRuleComplexity(child);
      }
    }

    // Add complexity for different rule types
    switch (rule.type) {
      case 'choice':
        complexity += (rule.children?.length || 0) * 2;
        break;
      case 'repetition':
        complexity += 3;
        break;
      case 'optional':
        complexity += 1;
        break;
    }

    return complexity;
  }

  /**
     * Generate cache key for analysis results
     */
  private generateCacheKey(grammar: Grammar, options: AnalysisOptions): string {
    const grammarHash = this.hashString(grammar.content || '');
    const optionsHash = this.hashString(JSON.stringify(options));
    return `${grammar.name || 'unknown'}_${grammarHash}_${optionsHash}`;
  }

  /**
     * Simple string hashing function
     */
  private hashString(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash.toString(36);
  }

  /**
     * Clear analysis cache
     */
  public clearCache(): void {
    this.analysisCache.clear();
  }

  /**
     * Get cache statistics
     */
  public getCacheStats(): { size: number; keys: string[] } {
    return {
      size: this.analysisCache.size,
      keys: Array.from(this.analysisCache.keys()),
    };
  }
}

/**
 * Utility functions for grammar analysis
 */
export class GrammarAnalysisUtils {
  /**
     * Compare two analyzed grammars for differences
     */
  static compareGrammars(
    grammar1: AnalyzedGrammar,
    grammar2: AnalyzedGrammar,
  ): {
        addedRules: string[];
        removedRules: string[];
        modifiedRules: string[];
        identical: boolean;
    } {
    const rules1 = new Set(grammar1.rules.keys());
    const rules2 = new Set(grammar2.rules.keys());

    const addedRules = Array.from(rules2).filter(rule => !rules1.has(rule));
    const removedRules = Array.from(rules1).filter(rule => !rules2.has(rule));
    const modifiedRules: string[] = [];

    // Check for modified rules
    for (const ruleName of rules1) {
      if (rules2.has(ruleName)) {
        const rule1 = grammar1.rules.get(ruleName)!;
        const rule2 = grammar2.rules.get(ruleName)!;

        if (JSON.stringify(rule1) !== JSON.stringify(rule2)) {
          modifiedRules.push(ruleName);
        }
      }
    }

    return {
      addedRules,
      removedRules,
      modifiedRules,
      identical: addedRules.length === 0 && removedRules.length === 0 && modifiedRules.length === 0,
    };
  }

  /**
     * Extract rule dependencies
     */
  static extractDependencies(rule: RailroadRule): string[] {
    const dependencies: string[] = [];

    function traverse(node: RailroadRule) {
      if (node.type === 'non-terminal' && node.name) {
        dependencies.push(node.name);
      }

      if (node.children) {
        node.children.forEach(traverse);
      }
    }

    traverse(rule);
    return [...new Set(dependencies)]; // Remove duplicates
  }

  /**
     * Calculate rule metrics
     */
  static calculateRuleMetrics(rule: RailroadRule): {
        depth: number;
        breadth: number;
        terminals: number;
        nonTerminals: number;
        choices: number;
        repetitions: number;
    } {
    let depth = 0;
    let breadth = 0;
    let terminals = 0;
    let nonTerminals = 0;
    let choices = 0;
    let repetitions = 0;

    function traverse(node: RailroadRule, currentDepth: number) {
      depth = Math.max(depth, currentDepth);

      switch (node.type) {
        case 'terminal':
          terminals++;
          break;
        case 'non-terminal':
          nonTerminals++;
          break;
        case 'choice':
          choices++;
          breadth = Math.max(breadth, node.children?.length || 0);
          break;
        case 'repetition':
          repetitions++;
          break;
      }

      if (node.children) {
        node.children.forEach(child => traverse(child, currentDepth + 1));
      }
    }

    traverse(rule, 1);

    return { depth, breadth, terminals, nonTerminals, choices, repetitions };
  }
}

