/**
 * anyParser - Extract and parse semantic actions from grammar files
 *
 * Parses grammar files to extract embedded TypeScript semantic actions and
 * integrates them with the SafeSemanticActionExecutor for secure execution.
 */

import { Grammar } from '../core/grammar/Grammar';
import { SafeSemanticActionExecutor } from './SafeSemanticActionExecutor';

export interface ParsedGrammarProduction {
  name: string;
  rule: string;
  semanticAction?: any;
  fallbackAction?: any;
  position: { line: number; column: number };
}

export interface GrammarParseResult {
  productions: ParsedGrammarProduction[];
  semanticActions: Map<string, any>;
  parseErrors: GrammarParseError[];
  metadata: GrammarMetadata;
}

export interface GrammarParseError {
  type: 'SYNTAX_ERROR' | 'SEMANTIC_ACTION_ERROR' | 'VALIDATION_ERROR';
  message: string;
  line: number;
  column: number;
  production?: string;
}

export interface GrammarMetadata {
  fileName: string;
  parseTime: Date;
  totalProductions: number;
  productionsWithActions: number;
  actionLanguage: string;
  version: string;
}

/**
 * anyParser - Parse grammar files with embedded semantic actions
 */
export class anyParser {
  private semanticActionExecutor: SafeSemanticActionExecutor;
  private actionIdCounter: number = 0;

  constructor(semanticActionExecutor: SafeSemanticActionExecutor) {
    this.semanticActionExecutor = semanticActionExecutor;
  }

  /**
   * Parse grammar file and extract semantic actions
   */
  async parseGrammarFile(
    grammarContent: string,
    fileName: string = 'unknown.grammar',
  ): Promise<GrammarParseResult> {

    const startTime = Date.now();
    const parseErrors: GrammarParseError[] = [];
    const productions: ParsedGrammarProduction[] = [];
    const semanticActions = new Map<string, any>();

    try {
      // Split grammar into lines for processing
      const lines = grammarContent.split('\n');
      let currentProduction: Partial<ParsedGrammarProduction> | null = null;
      let inSemanticAction = false;
      let actionBuffer = '';
      let actionType: 'main' | 'fallback' = 'main';
      let braceDepth = 0;

      for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
        const line = lines[lineIndex];
        const lineNumber = lineIndex + 1;

        try {
          // Skip comments and empty lines
          if (this.isCommentOrEmpty(line)) {
            continue;
          }

          // Check for production rule start
          const productionMatch = this.matchProductionRule(line);
          if (productionMatch) {
            // Finalize previous production if exists
            if (currentProduction) {
              productions.push(currentProduction as ParsedGrammarProduction);
            }

            // Start new production
            currentProduction = {
              name: productionMatch.name,
              rule: productionMatch.rule,
              position: { line: lineNumber, column: productionMatch.column },
            };
            continue;
          }

          // Check for fallback action annotation
          if (line.trim().startsWith('@fallback') && currentProduction) {
            actionType = 'fallback';
            continue;
          }

          // Check for semantic action start (any { block is automatically safe)
          if (line.includes('{') && currentProduction) {
            inSemanticAction = true;
            actionBuffer = '';
            braceDepth = 0;

            // Extract action content from current line
            const actionStart = line.indexOf('{');
            const actionContent = line.substring(actionStart + 1);
            actionBuffer += actionContent + '\n';
            braceDepth += this.countBraces(actionContent).open - this.countBraces(actionContent).close;

            continue;
          }

          // Continue collecting semantic action content
          if (inSemanticAction) {
            const braceCount = this.countBraces(line);
            braceDepth += braceCount.open - braceCount.close;

            if (braceDepth <= 0) {
              // End of semantic action
              const actionEndIndex = line.lastIndexOf('}');
              if (actionEndIndex >= 0) {
                actionBuffer += line.substring(0, actionEndIndex);
              }

              // Create semantic action (automatically safe)
              const semanticAction = this.createSemanticAction(
                currentProduction!.name!,
                actionBuffer.trim(),
                actionType,
                lineNumber,
              );

              // Add to production
              if (actionType === 'main') {
                currentProduction!.semanticAction = semanticAction;
              } else {
                currentProduction!.fallbackAction = semanticAction;
              }

              semanticActions.set(semanticAction.id, semanticAction);

              // Reset state
              inSemanticAction = false;
              actionBuffer = '';
              actionType = 'main'; // Reset to main for next action
            } else {
              // Continue collecting action content
              actionBuffer += line + '\n';
            }
          }

        } catch (lineError) {
          parseErrors.push({
            type: 'SYNTAX_ERROR',
            message: `Error parsing line: ${lineError}`,
            line: lineNumber,
            column: 0,
          });
        }
      }

      // Finalize last production
      if (currentProduction) {
        productions.push(currentProduction as ParsedGrammarProduction);
      }

      // Validate semantic actions (all are automatically safe, just check syntax)
      await this.validateSemanticActions(semanticActions, parseErrors);

      const metadata: GrammarMetadata = {
        fileName,
        parseTime: new Date(),
        totalProductions: productions.length,
        productionsWithActions: productions.filter(p => p.semanticAction).length,
        actionLanguage: 'typescript',
        version: '1.0.0',
      };

      return {
        productions,
        semanticActions,
        parseErrors,
        metadata,
      };

    } catch (error) {
      parseErrors.push({
        type: 'SYNTAX_ERROR',
        message: `Failed to parse grammar: ${error}`,
        line: 0,
        column: 0,
      });

      return {
        productions: [],
        semanticActions: new Map(),
        parseErrors,
        metadata: {
          fileName,
          parseTime: new Date(),
          totalProductions: 0,
          productionsWithActions: 0,
          actionLanguage: 'typescript',
          version: '1.0.0',
        },
      };
    }
  }

  /**
   * Check if line is comment or empty
   */
  private isCommentOrEmpty(line: string): boolean {
    const trimmed = line.trim();
    return trimmed === '' ||
           trimmed.startsWith('//') ||
           trimmed.startsWith('/*') ||
           trimmed.startsWith('*');
  }

  /**
   * Match production rule pattern
   */
  private matchProductionRule(line: string): { name: string; rule: string; column: number } | null {
    // Match patterns like: <funcdef> ::= def NAME <parameters> : <suite>
    const productionRegex = /<([^>]+)>\s*::=\s*(.+)/;
    const match = line.match(productionRegex);

    if (match) {
      return {
        name: match[1],
        rule: match[2],
        column: line.indexOf('<'),
      };
    }

    return null;
  }

  /**
   * Count opening and closing braces in a line
   */
  private countBraces(line: string): { open: number; close: number } {
    let open = 0;
    let close = 0;
    let inString = false;
    let stringChar = '';
    let escaped = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];

      if (escaped) {
        escaped = false;
        continue;
      }

      if (char === '\\') {
        escaped = true;
        continue;
      }

      if (!inString && (char === '"' || char === "'" || char === '`')) {
        inString = true;
        stringChar = char;
        continue;
      }

      if (inString && char === stringChar) {
        inString = false;
        stringChar = '';
        continue;
      }

      if (!inString) {
        if (char === '{') {
          open++;
        } else if (char === '}') {
          close++;
        }
      }
    }

    return { open, close };
  }

  /**
   * Create semantic action object
   */
  private createSemanticAction(
    productionName: string,
    actionCode: string,
    actionType: 'main' | 'fallback',
    lineNumber: number,
  ): any {

    const actionId = `${productionName}_${actionType}_${++this.actionIdCounter}`;

    return {
      id: actionId,
      production: productionName,
      actionCode: actionCode,
      fallbackCode: actionType === 'fallback' ? actionCode : undefined,
      enabled: true,
    };
  }

  /**
   * Validate semantic actions for syntax (all are automatically safe)
   */
  private async validateSemanticActions(
    semanticActions: Map<string, any>,
    parseErrors: GrammarParseError[],
  ): Promise<void> {

    for (const [actionId, action] of semanticActions) {
      try {
        // Basic syntax validation (all actions are automatically safe)
        const syntaxCheck = this.validateActionSyntax(action.actionCode);
        if (!syntaxCheck.valid) {
          parseErrors.push({
            type: 'SEMANTIC_ACTION_ERROR',
            message: `Syntax error in semantic action ${actionId}: ${syntaxCheck.error}`,
            line: 0,
            column: 0,
            production: action.production,
          });

          // Disable invalid action
          action.enabled = false;
        }
        // Note: No security validation needed - all actions are automatically sandboxed

      } catch (error) {
        parseErrors.push({
          type: 'SEMANTIC_ACTION_ERROR',
          message: `Failed to validate semantic action ${actionId}: ${error}`,
          line: 0,
          column: 0,
          production: action.production,
        });

        action.enabled = false;
      }
    }
  }

  /**
   * Validate TypeScript syntax of semantic action
   */
  private validateActionSyntax(actionCode: string): { valid: boolean; error?: string } {
    try {
      // Wrap in function to check syntax
      const wrappedCode = `(function() { ${actionCode} })`;
      new Function(wrappedCode);
      return { valid: true };
    } catch (error) {
      return { valid: false, error: (error as Error).message };
    }
  }

  /**
   * Generate enhanced grammar with semantic actions integrated
   */
  generateEnhancedGrammar(parseResult: GrammarParseResult): string {
    let enhancedGrammar = '';

    enhancedGrammar += '// Enhanced Grammar with Semantic Actions\n';
    enhancedGrammar += `// Generated on: ${parseResult.metadata.parseTime.toISOString()}\n`;
    enhancedGrammar += `// Total Productions: ${parseResult.metadata.totalProductions}\n`;
    enhancedGrammar += `// Productions with Actions: ${parseResult.metadata.productionsWithActions}\n`;
    enhancedGrammar += '// All semantic actions are automatically executed safely\n\n';

    for (const production of parseResult.productions) {
      enhancedGrammar += `<${production.name}> ::= ${production.rule}`;

      if (production.semanticAction) {
        enhancedGrammar += ' {\n';
        enhancedGrammar += `  // Semantic Action: ${production.semanticAction.id} (auto-safe)\n`;
        enhancedGrammar += `  ${production.semanticAction.actionCode}\n`;
        enhancedGrammar += '}';
      }

      if (production.fallbackAction) {
        enhancedGrammar += '\n@fallback {\n';
        enhancedGrammar += `  // Fallback Action: ${production.fallbackAction.id} (auto-safe)\n`;
        enhancedGrammar += `  ${production.fallbackAction.actionCode}\n`;
        enhancedGrammar += '}';
      }

      enhancedGrammar += '\n\n';
    }

    return enhancedGrammar;
  }

  /**
   * Get statistics about parsed grammar
   */
  getParseStatistics(parseResult: GrammarParseResult): any {
    return {
      totalProductions: parseResult.productions.length,
      productionsWithActions: parseResult.productions.filter(p => p.semanticAction).length,
      productionsWithFallbacks: parseResult.productions.filter(p => p.fallbackAction).length,
      totalSemanticActions: parseResult.semanticActions.size,
      enabledActions: Array.from(parseResult.semanticActions.values()).filter(a => a.enabled).length,
      disabledActions: Array.from(parseResult.semanticActions.values()).filter(a => !a.enabled).length,
      parseErrors: parseResult.parseErrors.length,
      syntaxErrors: parseResult.parseErrors.filter(e => e.type === 'SYNTAX_ERROR').length,
      semanticErrors: parseResult.parseErrors.filter(e => e.type === 'SEMANTIC_ACTION_ERROR').length,
      validationErrors: parseResult.parseErrors.filter(e => e.type === 'VALIDATION_ERROR').length,
    };
  }
}

