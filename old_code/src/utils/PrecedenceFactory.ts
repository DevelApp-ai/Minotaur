/**
 * Factory for creating precedence and associativity rules from various sources.
 */
export class PrecedenceFactory {
  private precedenceTemplates: Map<string, PrecedenceTemplate>;
  private associativityTemplates: Map<string, AssociativityTemplate>;

  /**
   * Creates a new PrecedenceFactory instance.
   */
  constructor() {
    this.precedenceTemplates = new Map<string, PrecedenceTemplate>();
    this.associativityTemplates = new Map<string, AssociativityTemplate>();

    // Initialize with common templates
    this.initializeCommonTemplates();
  }

  /**
   * Creates a precedence rule.
   * @param operatorName The operator name
   * @param level The precedence level
   * @param grammarName The grammar name
   * @param description Optional description
   * @returns The precedence rule
   */
  public createPrecedenceRule(
    operatorName: string,
    level: number,
    grammarName: string,
    description?: string,
  ): PrecedenceRule {
    const rule = new PrecedenceRule(operatorName, level, grammarName);
    if (description) {
      rule.setDescription(description);
    }
    return rule;
  }

  /**
   * Creates an associativity rule.
   * @param operatorName The operator name
   * @param associativity The associativity
   * @param grammarName The grammar name
   * @param description Optional description
   * @returns The associativity rule
   */
  public createAssociativityRule(
    operatorName: string,
    associativity: Associativity,
    grammarName: string,
    description?: string,
  ): AssociativityRule {
    const rule = new AssociativityRule(operatorName, associativity, grammarName);
    if (description) {
      rule.setDescription(description);
    }
    return rule;
  }

  /**
   * Creates precedence and associativity rules from format-specific definitions.
   * @param grammarName The grammar name
   * @param formatType The grammar format type
   * @param definitions The rule definitions
   * @returns Object containing precedence and associativity rules
   */
  public createFromFormatSpecificDefinitions(
    grammarName: string,
    formatType: GrammarFormatType,
    definitions: Map<string, string>,
  ): { precedenceRules: PrecedenceRule[], associativityRules: AssociativityRule[] } {
    const precedenceRules: PrecedenceRule[] = [];
    const associativityRules: AssociativityRule[] = [];

    for (const [operatorName, definition] of definitions) {
      const rules = this.parseFormatSpecificDefinition(operatorName, definition, grammarName, formatType);
      if (rules.precedenceRule) {
        precedenceRules.push(rules.precedenceRule);
      }
      if (rules.associativityRule) {
        associativityRules.push(rules.associativityRule);
      }
    }

    return { precedenceRules, associativityRules };
  }

  /**
   * Parses a format-specific definition into precedence and associativity rules.
   * @param operatorName The operator name
   * @param definition The definition string
   * @param grammarName The grammar name
   * @param formatType The format type
   * @returns Object containing parsed rules
   */
  private parseFormatSpecificDefinition(
    operatorName: string,
    definition: string,
    grammarName: string,
    formatType: GrammarFormatType,
  ): { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } {
    switch (formatType) {
      case GrammarFormatType.ANTLR4:
        return this.parseANTLR4Definition(operatorName, definition, grammarName);

      case GrammarFormatType.Bison:
        return this.parseBisonDefinition(operatorName, definition, grammarName);

      case GrammarFormatType.Yacc:
        return this.parseYaccDefinition(operatorName, definition, grammarName);

      case GrammarFormatType.Flex:
      case GrammarFormatType.Lex:
        // Flex and Lex don't typically have precedence/associativity
        return {};

      default:
        return this.parseGenericDefinition(operatorName, definition, grammarName);
    }
  }

  /**
   * Parses ANTLR4-specific precedence/associativity definition.
   * @param operatorName The operator name
   * @param definition The definition string
   * @param grammarName The grammar name
   * @returns Parsed rules
   */
  private parseANTLR4Definition(
    operatorName: string,
    definition: string,
    grammarName: string,
  ): { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } {
    // ANTLR4 uses options like: options { assoc=right; }
    const result: { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } = {};

    // Extract associativity
    const assocMatch = definition.match(/assoc\s*=\s*(left|right|none)/i);
    if (assocMatch) {
      const assocValue = assocMatch[1].toLowerCase();
      const associativity = assocValue === 'left' ? Associativity.Left :
        assocValue === 'right' ? Associativity.Right :
          Associativity.None;
      result.associativityRule = this.createAssociativityRule(operatorName, associativity, grammarName);
    }

    // Extract precedence level (if specified)
    const precMatch = definition.match(/prec\s*=\s*(\d+)/i);
    if (precMatch) {
      const level = parseInt(precMatch[1]);
      result.precedenceRule = this.createPrecedenceRule(operatorName, level, grammarName);
    }

    return result;
  }

  /**
   * Parses Bison-specific precedence/associativity definition.
   * @param operatorName The operator name
   * @param definition The definition string
   * @param grammarName The grammar name
   * @returns Parsed rules
   */
  private parseBisonDefinition(
    operatorName: string,
    definition: string,
    grammarName: string,
  ): { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } {
    // Bison uses %left, %right, %nonassoc with precedence levels
    const result: { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } = {};

    // Parse %left, %right, %nonassoc
    const assocMatch = definition.match(/%(left|right|nonassoc)\s+(.+)/i);
    if (assocMatch) {
      const assocType = assocMatch[1].toLowerCase();
      const associativity = assocType === 'left' ? Associativity.Left :
        assocType === 'right' ? Associativity.Right :
          Associativity.None;
      result.associativityRule = this.createAssociativityRule(operatorName, associativity, grammarName);
    }

    // Parse %prec
    const precMatch = definition.match(/%prec\s+(\w+)/i);
    if (precMatch) {
      // In Bison, %prec refers to another token's precedence
      // For now, we'll assign a default level
      result.precedenceRule = this.createPrecedenceRule(operatorName, 1, grammarName);
    }

    return result;
  }

  /**
   * Parses Yacc-specific precedence/associativity definition.
   * @param operatorName The operator name
   * @param definition The definition string
   * @param grammarName The grammar name
   * @returns Parsed rules
   */
  private parseYaccDefinition(
    operatorName: string,
    definition: string,
    grammarName: string,
  ): { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } {
    // Yacc is similar to Bison
    return this.parseBisonDefinition(operatorName, definition, grammarName);
  }

  /**
   * Parses generic precedence/associativity definition.
   * @param operatorName The operator name
   * @param definition The definition string
   * @param grammarName The grammar name
   * @returns Parsed rules
   */
  private parseGenericDefinition(
    operatorName: string,
    definition: string,
    grammarName: string,
  ): { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } {
    const result: { precedenceRule?: PrecedenceRule, associativityRule?: AssociativityRule } = {};

    // Try to parse level:associativity format (e.g., "5:left")
    const match = definition.match(/(\d+)\s*:\s*(left|right|none)/i);
    if (match) {
      const level = parseInt(match[1]);
      const assocValue = match[2].toLowerCase();
      const associativity = assocValue === 'left' ? Associativity.Left :
        assocValue === 'right' ? Associativity.Right :
          Associativity.None;

      result.precedenceRule = this.createPrecedenceRule(operatorName, level, grammarName);
      result.associativityRule = this.createAssociativityRule(operatorName, associativity, grammarName);
    } else {
      // Try to parse just level
      const levelMatch = definition.match(/^\d+$/);
      if (levelMatch) {
        const level = parseInt(definition);
        result.precedenceRule = this.createPrecedenceRule(operatorName, level, grammarName);
      }

      // Try to parse just associativity
      const assocMatch = definition.match(/^(left|right|none)$/i);
      if (assocMatch) {
        const assocValue = assocMatch[1].toLowerCase();
        const associativity = assocValue === 'left' ? Associativity.Left :
          assocValue === 'right' ? Associativity.Right :
            Associativity.None;
        result.associativityRule = this.createAssociativityRule(operatorName, associativity, grammarName);
      }
    }

    return result;
  }

  /**
   * Creates rules from a precedence template.
   * @param templateName The template name
   * @param grammarName The grammar name
   * @param operators The operators to apply the template to
   * @returns Array of precedence rules
   */
  public createFromPrecedenceTemplate(
    templateName: string,
    grammarName: string,
    operators: string[],
  ): PrecedenceRule[] {
    const template = this.precedenceTemplates.get(templateName);
    if (!template) {
      throw new Error(`Precedence template '${templateName}' not found`);
    }

    return template.createRules(grammarName, operators);
  }

  /**
   * Creates rules from an associativity template.
   * @param templateName The template name
   * @param grammarName The grammar name
   * @param operators The operators to apply the template to
   * @returns Array of associativity rules
   */
  public createFromAssociativityTemplate(
    templateName: string,
    grammarName: string,
    operators: string[],
  ): AssociativityRule[] {
    const template = this.associativityTemplates.get(templateName);
    if (!template) {
      throw new Error(`Associativity template '${templateName}' not found`);
    }

    return template.createRules(grammarName, operators);
  }

  /**
   * Creates inherited precedence rules.
   * @param grammarName The derived grammar name
   * @param baseGrammarName The base grammar name
   * @param precedenceManager The precedence manager
   * @returns Array of inherited precedence rules
   */
  public createInheritedPrecedenceRules(
    grammarName: string,
    baseGrammarName: string,
    precedenceManager: PrecedenceManager,
  ): PrecedenceRule[] {
    const inheritedRules: PrecedenceRule[] = [];
    const baseRules = precedenceManager.getDirectPrecedenceRules(baseGrammarName);

    for (const [operatorName, baseRule] of baseRules) {
      const inheritedRule = baseRule.copy();
      // Update grammar name but keep other properties
      const newRule = new PrecedenceRule(operatorName, inheritedRule.getLevel(), grammarName);
      newRule.setDescription(`Inherited from ${baseGrammarName}: ${inheritedRule.getDescription()}`);

      // Copy metadata
      const metadata = inheritedRule.getMetadata('all');
      if (metadata) {
        newRule.setMetadata('all', metadata);
      }

      inheritedRules.push(newRule);
    }

    return inheritedRules;
  }

  /**
   * Creates inherited associativity rules.
   * @param grammarName The derived grammar name
   * @param baseGrammarName The base grammar name
   * @param precedenceManager The precedence manager
   * @returns Array of inherited associativity rules
   */
  public createInheritedAssociativityRules(
    grammarName: string,
    baseGrammarName: string,
    precedenceManager: PrecedenceManager,
  ): AssociativityRule[] {
    const inheritedRules: AssociativityRule[] = [];
    const baseRules = precedenceManager.getDirectAssociativityRules(baseGrammarName);

    for (const [operatorName, baseRule] of baseRules) {
      const inheritedRule = baseRule.copy();
      // Update grammar name but keep other properties
      const newRule = new AssociativityRule(operatorName, inheritedRule.getAssociativity(), grammarName);
      newRule.setDescription(`Inherited from ${baseGrammarName}: ${inheritedRule.getDescription()}`);

      // Copy metadata
      const metadata = inheritedRule.getMetadata('all');
      if (metadata) {
        newRule.setMetadata('all', metadata);
      }

      inheritedRules.push(newRule);
    }

    return inheritedRules;
  }

  /**
   * Registers a precedence template.
   * @param name The template name
   * @param template The precedence template
   */
  public registerPrecedenceTemplate(name: string, template: PrecedenceTemplate): void {
    this.precedenceTemplates.set(name, template);
  }

  /**
   * Registers an associativity template.
   * @param name The template name
   * @param template The associativity template
   */
  public registerAssociativityTemplate(name: string, template: AssociativityTemplate): void {
    this.associativityTemplates.set(name, template);
  }

  /**
   * Initializes common precedence and associativity templates.
   */
  private initializeCommonTemplates(): void {
    // Arithmetic operators template
    this.registerPrecedenceTemplate('arithmetic', new ArithmeticPrecedenceTemplate());
    this.registerAssociativityTemplate('arithmetic', new ArithmeticAssociativityTemplate());

    // Logical operators template
    this.registerPrecedenceTemplate('logical', new LogicalPrecedenceTemplate());
    this.registerAssociativityTemplate('logical', new LogicalAssociativityTemplate());

    // Comparison operators template
    this.registerPrecedenceTemplate('comparison', new ComparisonPrecedenceTemplate());
    this.registerAssociativityTemplate('comparison', new ComparisonAssociativityTemplate());

    // C-style operators template
    this.registerPrecedenceTemplate('c_style', new CStylePrecedenceTemplate());
    this.registerAssociativityTemplate('c_style', new CStyleAssociativityTemplate());
  }

  /**
   * Gets all available precedence template names.
   */
  public getPrecedenceTemplateNames(): string[] {
    return Array.from(this.precedenceTemplates.keys());
  }

  /**
   * Gets all available associativity template names.
   */
  public getAssociativityTemplateNames(): string[] {
    return Array.from(this.associativityTemplates.keys());
  }
}

/**
 * Interface for precedence templates.
 */
export interface PrecedenceTemplate {
  createRules(grammarName: string, operators: string[]): PrecedenceRule[];
}

/**
 * Interface for associativity templates.
 */
export interface AssociativityTemplate {
  createRules(grammarName: string, operators: string[]): AssociativityRule[];
}

/**
 * Arithmetic operators precedence template.
 */
export class ArithmeticPrecedenceTemplate implements PrecedenceTemplate {
  private operatorLevels: Map<string, number> = new Map([
    ['(', 7], [')', 7],
    ['*', 6], ['/', 6], ['%', 6],
    ['+', 5], ['-', 5],
    ['<<', 4], ['>>', 4],
    ['<', 3], ['<=', 3], ['>', 3], ['>=', 3],
    ['==', 2], ['!=', 2],
    ['=', 1],
  ]);

  public createRules(grammarName: string, operators: string[]): PrecedenceRule[] {
    const rules: PrecedenceRule[] = [];

    for (const operator of operators) {
      const level = this.operatorLevels.get(operator) || 1;
      const rule = new PrecedenceRule(operator, level, grammarName);
      rule.setDescription('Arithmetic operator precedence');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * Arithmetic operators associativity template.
 */
export class ArithmeticAssociativityTemplate implements AssociativityTemplate {
  private operatorAssociativity: Map<string, Associativity> = new Map([
    ['*', Associativity.Left], ['/', Associativity.Left], ['%', Associativity.Left],
    ['+', Associativity.Left], ['-', Associativity.Left],
    ['<<', Associativity.Left], ['>>', Associativity.Left],
    ['<', Associativity.Left], ['<=', Associativity.Left],
    ['>', Associativity.Left], ['>=', Associativity.Left],
    ['==', Associativity.Left], ['!=', Associativity.Left],
    ['=', Associativity.Right],
  ]);

  public createRules(grammarName: string, operators: string[]): AssociativityRule[] {
    const rules: AssociativityRule[] = [];

    for (const operator of operators) {
      const associativity = this.operatorAssociativity.get(operator) || Associativity.Left;
      const rule = new AssociativityRule(operator, associativity, grammarName);
      rule.setDescription('Arithmetic operator associativity');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * Logical operators precedence template.
 */
export class LogicalPrecedenceTemplate implements PrecedenceTemplate {
  private operatorLevels: Map<string, number> = new Map([
    ['!', 6],
    ['&&', 3],
    ['||', 2],
    ['?', 1], [':', 1],
  ]);

  public createRules(grammarName: string, operators: string[]): PrecedenceRule[] {
    const rules: PrecedenceRule[] = [];

    for (const operator of operators) {
      const level = this.operatorLevels.get(operator) || 1;
      const rule = new PrecedenceRule(operator, level, grammarName);
      rule.setDescription('Logical operator precedence');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * Logical operators associativity template.
 */
export class LogicalAssociativityTemplate implements AssociativityTemplate {
  private operatorAssociativity: Map<string, Associativity> = new Map([
    ['!', Associativity.Right],
    ['&&', Associativity.Left],
    ['||', Associativity.Left],
    ['?', Associativity.Right], [':', Associativity.Right],
  ]);

  public createRules(grammarName: string, operators: string[]): AssociativityRule[] {
    const rules: AssociativityRule[] = [];

    for (const operator of operators) {
      const associativity = this.operatorAssociativity.get(operator) || Associativity.Left;
      const rule = new AssociativityRule(operator, associativity, grammarName);
      rule.setDescription('Logical operator associativity');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * Comparison operators precedence template.
 */
export class ComparisonPrecedenceTemplate implements PrecedenceTemplate {
  public createRules(grammarName: string, operators: string[]): PrecedenceRule[] {
    const rules: PrecedenceRule[] = [];

    for (const operator of operators) {
      const rule = new PrecedenceRule(operator, 3, grammarName);
      rule.setDescription('Comparison operator precedence');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * Comparison operators associativity template.
 */
export class ComparisonAssociativityTemplate implements AssociativityTemplate {
  public createRules(grammarName: string, operators: string[]): AssociativityRule[] {
    const rules: AssociativityRule[] = [];

    for (const operator of operators) {
      const rule = new AssociativityRule(operator, Associativity.Left, grammarName);
      rule.setDescription('Comparison operator associativity');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * C-style operators precedence template.
 */
export class CStylePrecedenceTemplate implements PrecedenceTemplate {
  private operatorLevels: Map<string, number> = new Map([
    ['(', 16], [')', 16], ['[', 16], [']', 16], ['.', 16], ['->', 16],
    ['++', 15], ['--', 15], ['!', 15], ['~', 15], ['+', 15], ['-', 15], ['*', 15], ['&', 15],
    ['*', 14], ['/', 14], ['%', 14],
    ['+', 13], ['-', 13],
    ['<<', 12], ['>>', 12],
    ['<', 11], ['<=', 11], ['>', 11], ['>=', 11],
    ['==', 10], ['!=', 10],
    ['&', 9],
    ['^', 8],
    ['|', 7],
    ['&&', 6],
    ['||', 5],
    ['?', 4], [':', 4],
    ['=', 3], ['+=', 3], ['-=', 3], ['*=', 3], ['/=', 3], ['%=', 3],
    [',', 2],
  ]);

  public createRules(grammarName: string, operators: string[]): PrecedenceRule[] {
    const rules: PrecedenceRule[] = [];

    for (const operator of operators) {
      const level = this.operatorLevels.get(operator) || 1;
      const rule = new PrecedenceRule(operator, level, grammarName);
      rule.setDescription('C-style operator precedence');
      rules.push(rule);
    }

    return rules;
  }
}

/**
 * C-style operators associativity template.
 */
export class CStyleAssociativityTemplate implements AssociativityTemplate {
  private operatorAssociativity: Map<string, Associativity> = new Map([
    ['(', Associativity.Left], [')', Associativity.Left], ['[', Associativity.Left], [']', Associativity.Left],
    ['.', Associativity.Left], ['->', Associativity.Left],
    ['++', Associativity.Right], ['--', Associativity.Right], ['!', Associativity.Right], ['~', Associativity.Right],
    ['*', Associativity.Left], ['/', Associativity.Left], ['%', Associativity.Left],
    ['+', Associativity.Left], ['-', Associativity.Left],
    ['<<', Associativity.Left], ['>>', Associativity.Left],
    ['<', Associativity.Left], ['<=', Associativity.Left], ['>', Associativity.Left], ['>=', Associativity.Left],
    ['==', Associativity.Left], ['!=', Associativity.Left],
    ['&', Associativity.Left], ['^', Associativity.Left], ['|', Associativity.Left],
    ['&&', Associativity.Left], ['||', Associativity.Left],
    ['?', Associativity.Right], [':', Associativity.Right],
    ['=', Associativity.Right], ['+=', Associativity.Right], ['-=', Associativity.Right],
    ['*=', Associativity.Right], ['/=', Associativity.Right], ['%=', Associativity.Right],
    [',', Associativity.Left],
  ]);

  public createRules(grammarName: string, operators: string[]): AssociativityRule[] {
    const rules: AssociativityRule[] = [];

    for (const operator of operators) {
      const associativity = this.operatorAssociativity.get(operator) || Associativity.Left;
      const rule = new AssociativityRule(operator, associativity, grammarName);
      rule.setDescription('C-style operator associativity');
      rules.push(rule);
    }

    return rules;
  }
}

// Import required classes and interfaces
import {
  PrecedenceRule,
  AssociativityRule,
  Associativity,
  PrecedenceManager,
} from './PrecedenceManager';
import { GrammarFormatType } from './Grammar';

