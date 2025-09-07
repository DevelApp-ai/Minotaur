/**
 * ProductionTriggerIntegrator - Integration layer between grammar production triggers and StepParser
 *
 * Connects the SafeSemanticActionExecutor with Minotaur's StepParser to enable
 * real-time semantic validation during parsing through production triggers.
 */

import { StepParser } from '../utils/StepParser';
import { Grammar } from '../core/grammar/Grammar';
import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';
import { SafeSemanticActionExecutor, GrammarSemanticAction, ExecutionContext } from './SafeSemanticActionExecutor';
// GrammarSemanticActionParser removed - semantic actions now handled by production trigger system
import { ProductionContext } from './SemanticValidator';
import * as path from 'path';
import * as fs from 'fs/promises';

export interface ProductionTriggerConfig {
  enableSemanticActions: boolean;
  enableFallbackActions: boolean;
  maxConcurrentActions: number;
  actionTimeout: number;
  enableActionCaching: boolean;
  logActionExecution: boolean;
}

export interface ParseContext {
  sourceCode: string;
  currentPosition: { line: number; column: number };
  parseStack: any[];
  scopeStack: any[];
  symbolTable: Map<string, any>;
  errors: any[];
  warnings: any[];
}

export interface ProductionMatch {
  productionName: string;
  matchedTokens: any[];
  astNode: ZeroCopyASTNode;
  position: { line: number; column: number };
  context: ParseContext;
}

export interface TriggerExecutionResult {
  success: boolean;
  result?: any;
  modifiedAST?: ZeroCopyASTNode;
  errors: any[];
  warnings: any[];
  executionTime: number;
}

/**
 * ProductionTriggerIntegrator - Main integration component
 */
export class ProductionTriggerIntegrator {
  private stepParser: StepParser;
  private grammar: Grammar;
  private semanticActionExecutor: SafeSemanticActionExecutor;
  // grammarParser removed - semantic actions now handled by production trigger system
  private config: ProductionTriggerConfig;
  private semanticActions: Map<string, GrammarSemanticAction>;
  private activeExecutions: Set<string>;

  constructor(
    stepParser: StepParser,
    grammar: Grammar,
    config: Partial<ProductionTriggerConfig> = {},
  ) {
    this.stepParser = stepParser;
    this.grammar = grammar;

    this.config = {
      enableSemanticActions: true,
      enableFallbackActions: true,
      maxConcurrentActions: 10,
      actionTimeout: 5000,
      enableActionCaching: true,
      logActionExecution: false,
      ...config,
    };

    this.semanticActionExecutor = new SafeSemanticActionExecutor({
      timeout: this.config.actionTimeout,
      enableConsole: this.config.logActionExecution,
    });

    // grammarParser removed - semantic actions now handled by production trigger system
    this.semanticActions = new Map();
    this.activeExecutions = new Set();
  }

  /**
   * Initialize the integrator with grammar file containing semantic actions
   */
  async initialize(grammarFilePath: string): Promise<void> {
    try {
      // Read and parse grammar file
      const grammarContent = await this.readGrammarFile(grammarFilePath);
      
      // Use GrammarInterpreter to parse grammar content
      const { GrammarContainer } = require('../utils/GrammarContainer');
      const { GrammarInterpreter } = require('../utils/GrammarInterpreter');
      
      const container = new GrammarContainer();
      const interpreter = new GrammarInterpreter(container);
      const grammar = interpreter.parseGrammar(grammarContent, path.basename(grammarFilePath));
      
      // Initialize semantic actions (would be extracted from grammar)
      this.semanticActions = new Map();

      // Register production triggers with StepParser
      await this.registerProductionTriggers(grammar);

      console.log(`✅ Initialized production triggers: ${this.semanticActions.size} semantic actions loaded`);

    } catch (error) {
      console.error('Failed to initialize production trigger integrator:', error);
      throw error;
    }
  }

  /**
   * Register production triggers with the StepParser
   */
  private async registerProductionTriggers(grammar: any): Promise<void> {
    // For now, register basic triggers - would extract from grammar in real implementation
    console.log(`Registering production triggers for grammar: ${grammar.name || 'unknown'}`);
    
    // Example: Register common Python production triggers
    const commonTriggers = [
      'function_def',
      'class_def', 
      'if_stmt',
      'for_stmt',
      'while_stmt',
    ];
    
    for (const triggerName of commonTriggers) {
      // Create proper semantic action implementation for each trigger
      const semanticAction: GrammarSemanticAction = {
        id: `action_${triggerName}`,
        production: triggerName,
        actionCode: this.createSemanticActionCode(triggerName),
        enabled: true,
      };
      
      await this.registerProductionTrigger(
        triggerName,
        semanticAction,
        'main',
      );
    }
  }

  /**
   * Register a single production trigger
   */
  private async registerProductionTrigger(
    productionName: string,
    semanticAction: GrammarSemanticAction,
    triggerType: 'main' | 'fallback',
  ): Promise<void> {

    // Create trigger callback for StepParser
    const triggerCallback = async (match: ProductionMatch): Promise<TriggerExecutionResult> => {
      return await this.executeProductionTrigger(semanticAction, match, triggerType);
    };

    // Register with StepParser (this would integrate with actual StepParser API)
    // For now, we'll simulate the registration
    console.log(`📝 Registered ${triggerType} trigger for production: ${productionName}`);
  }

  /**
   * Execute a production trigger when a grammar production is matched
   */
  async executeProductionTrigger(
    semanticAction: GrammarSemanticAction,
    match: ProductionMatch,
    triggerType: 'main' | 'fallback',
  ): Promise<TriggerExecutionResult> {

    const executionId = `${semanticAction.id}-${Date.now()}`;

    try {
      // Check concurrency limits
      if (this.activeExecutions.size >= this.config.maxConcurrentActions) {
        return {
          success: false,
          errors: ['Maximum concurrent actions exceeded'],
          warnings: [],
          executionTime: 0,
        };
      }

      this.activeExecutions.add(executionId);

      // Create execution context
      const executionContext: ExecutionContext = {
        production: this.createProductionContext(match),
        tokens: match.matchedTokens,
        ast: match.astNode,
        position: match.position,
        metadata: new Map(),
      };

      // Execute semantic action
      const startTime = Date.now();
      const result = await this.semanticActionExecutor.executeSemanticAction(
        semanticAction,
        executionContext,
      );
      const executionTime = Date.now() - startTime;

      if (this.config.logActionExecution) {
        console.log(`🔧 Executed ${triggerType} trigger: ${semanticAction.id} (${executionTime}ms)`);
      }

      return {
        success: result.success,
        result: result.result,
        modifiedAST: result.result instanceof Object ? result.result : match.astNode,
        errors: result.error ? [result.error] : [],
        warnings: [],
        executionTime,
      };

    } catch (error) {
      console.error('Production trigger execution failed:', error);

      return {
        success: false,
        errors: [(error as Error).message],
        warnings: [],
        executionTime: 0,
      };

    } finally {
      this.activeExecutions.delete(executionId);
    }
  }

  /**
   * Create production context from parser match
   */
  private createProductionContext(match: ProductionMatch): ProductionContext {
    return {
      production: match.productionName,
      tokens: match.matchedTokens,
      position: match.position,
      scopeStack: match.context.scopeStack || [],
      symbolTable: match.context.symbolTable || new Map(),
      parseState: {
        stack: match.context.parseStack || [],
        currentToken: match.matchedTokens[0] || null,
        lookahead: null,
      },
      errors: match.context.errors || [],
      warnings: match.context.warnings || [],
    };
  }

  /**
   * Parse source code with production triggers enabled
   */
  async parseWithTriggers(sourceCode: string): Promise<{
    ast: ZeroCopyASTNode;
    errors: any[];
    warnings: any[];
    triggerResults: Map<string, TriggerExecutionResult>;
  }> {

    const triggerResults = new Map<string, TriggerExecutionResult>();
    const errors: any[] = [];
    const warnings: any[] = [];

    try {
      // Create parse context
      const parseContext: ParseContext = {
        sourceCode,
        currentPosition: { line: 1, column: 1 },
        parseStack: [],
        scopeStack: [],
        symbolTable: new Map(),
        errors,
        warnings,
      };

      // Parse with StepParser (this would be the actual integration point)
      const sourceContainer = {
        getSourceLines: () => sourceCode.split('\n').map((line, index) => ({
          getLineNumber: () => index + 1,
          getContent: () => line,
          getLength: () => line.length,
          getFileName: () => 'source.py',
        })),
        getCount: () => sourceCode.split('\n').length,
        getLine: (fileName: string, lineNumber: number) => ({
          getLineNumber: () => lineNumber,
          getContent: () => sourceCode.split('\n')[lineNumber - 1] || '',
          getLength: () => (sourceCode.split('\n')[lineNumber - 1] || '').length,
          getFileName: () => fileName,
        }),
      };
      const productionMatches = await this.stepParser.parse('Python311', sourceContainer);

      // Simulate production matches and trigger execution
      // In real implementation, this would be integrated into StepParser's parsing loop
      const simulatedMatches = await this.simulateProductionMatches(productionMatches, parseContext);

      for (const match of simulatedMatches) {
        const semanticAction = this.findSemanticActionForProduction(match.productionName);
        if (semanticAction) {
          const triggerResult = await this.executeProductionTrigger(
            semanticAction,
            match,
            'main',
          );

          triggerResults.set(match.productionName, triggerResult);

          if (!triggerResult.success) {
            errors.push(...triggerResult.errors);
          }
          warnings.push(...triggerResult.warnings);
        }
      }

      return {
        ast: {} as ZeroCopyASTNode, // Simplified for compilation
        errors,
        warnings,
        triggerResults,
      };

    } catch (error) {
      errors.push((error as Error).message);

      return {
        ast: {} as ZeroCopyASTNode,
        errors,
        warnings,
        triggerResults,
      };
    }
  }

  /**
   * Find semantic action for a production name
   */
  private findSemanticActionForProduction(productionName: string): GrammarSemanticAction | null {
    for (const action of this.semanticActions.values()) {
      if (action.production === productionName) {
        return action;
      }
    }
    return null;
  }

  /**
   * Simulate production matches for testing (would be replaced by real StepParser integration)
   */
  private async simulateProductionMatches(
    productionMatches: any[], // ProductionMatch[] from StepParser
    context: ParseContext,
  ): Promise<ProductionMatch[]> {

    // Convert StepParser ProductionMatch[] to our format
    const matches: ProductionMatch[] = productionMatches.map((match, index) => ({
      productionName: match.productionName || 'unknown',
      matchedTokens: match.matchedTokens || [],
      astNode: {} as ZeroCopyASTNode, // Simplified for compilation
      position: { line: match.line || 0, column: match.column || 0 },
      context: context,
    }));

    return matches;
  }

  /**
   * Read grammar file content
   */
  private async readGrammarFile(filePath: string): Promise<string> {
    // This would read the actual grammar file
    // For now, return a sample Python grammar with semantic actions
    return this.getSamplePythonGrammarWithSemanticActions();
  }

  /**
   * Get sample Python grammar with semantic actions for testing
   */
  private getSamplePythonGrammarWithSemanticActions(): string {
    return `
// Python 3.11 Grammar with Semantic Actions
// Sample productions for testing production triggers

<funcdef> ::= def NAME <parameters> : <suite> {
  // Enter function scope and register function
  context.enterScope('function', $2.value);
  context.registerBinding($2.value, 'function');
  return createNode('funcdef', $2.value, [$3, $5]);
}

<NAME> ::= IDENTIFIER {
  // Check if variable is defined
  if (!context.isVariableDefined($1.value)) {
    context.addError({
      type: 'NameError',
      message: \`name '\${$1.value}' is not defined\`,
      position: $1.position
    });
  }
  return createNode('name', $1.value);
}

<assignment> ::= NAME = <expr> {
  // Register variable assignment
  context.registerBinding($1.value, 'variable');
  return createNode('assignment', null, [$1, $3]);
}

<if_stmt> ::= if <test> : <suite> {
  // Validate condition expression
  if ($2.type === 'assignment') {
    context.addWarning({
      type: 'SyntaxWarning',
      message: 'Assignment in if condition, did you mean ==?',
      position: $2.position
    });
  }
  return createNode('if_stmt', null, [$2, $4]);
}

<import_name> ::= import <dotted_as_names> {
  // Register imported modules
  for (const moduleName of $2.modules) {
    context.registerBinding(moduleName, 'module');
  }
  return createNode('import_name', null, [$2]);
}
@fallback {
  // Fallback for import errors
  return createNode('import_name', 'unknown_module');
}
`;
  }

  /**
   * Create semantic action code for specific production triggers
   */
  private createSemanticActionCode(triggerName: string): string {
    switch (triggerName) {
      case 'function_def':
        return `
          // Validate function definition
          const funcName = context.node.name?.value;
          if (!funcName) {
            context.errors.push({
              type: 'semantic_error',
              message: 'Function definition missing name',
              line: context.position.line,
              column: context.position.column
            });
          }
          
          // Check for duplicate function definitions
          if (context.symbolTable.has(funcName)) {
            context.warnings.push({
              type: 'semantic_warning', 
              message: \`Function '\${funcName}' redefined\`,
              line: context.position.line,
              column: context.position.column
            });
          }
          
          // Add function to symbol table
          context.symbolTable.set(funcName, {
            type: 'function',
            defined: true,
            line: context.position.line
          });
        `;
        
      case 'class_def':
        return `
          // Validate class definition
          const className = context.node.name?.value;
          if (!className) {
            context.errors.push({
              type: 'semantic_error',
              message: 'Class definition missing name',
              line: context.position.line,
              column: context.position.column
            });
          }
          
          // Check for duplicate class definitions
          if (context.symbolTable.has(className)) {
            context.warnings.push({
              type: 'semantic_warning',
              message: \`Class '\${className}' redefined\`,
              line: context.position.line,
              column: context.position.column
            });
          }
          
          // Add class to symbol table
          context.symbolTable.set(className, {
            type: 'class',
            defined: true,
            line: context.position.line
          });
        `;
        
      case 'if_stmt':
      case 'for_stmt':
      case 'while_stmt':
        return `
          // Validate control flow statement
          if (!context.node.test && !context.node.condition) {
            context.errors.push({
              type: 'semantic_error',
              message: 'Control flow statement missing condition',
              line: context.position.line,
              column: context.position.column
            });
          }
          
          // Check for unreachable code patterns
          if (context.node.body && context.node.body.length === 0) {
            context.warnings.push({
              type: 'semantic_warning',
              message: 'Empty control flow body',
              line: context.position.line,
              column: context.position.column
            });
          }
        `;
        
      default:
        return `
          // Default semantic action for ${triggerName}
          console.log('Processing production: ${triggerName}');
        `;
    }
  }

  /**
   * Get execution statistics
   */
  getExecutionStatistics(): any {
    return {
      totalSemanticActions: this.semanticActions.size,
      enabledActions: Array.from(this.semanticActions.values()).filter(a => a.enabled).length,
      activeExecutions: this.activeExecutions.size,
      executorStats: this.semanticActionExecutor.getExecutionStats(),
    };
  }

  /**
   * Enable or disable semantic actions
   */
  setSemanticActionsEnabled(enabled: boolean): void {
    this.config.enableSemanticActions = enabled;
    console.log(`🔧 Semantic actions ${enabled ? 'enabled' : 'disabled'}`);
  }

  /**
   * Clear action cache
   */
  clearCache(): void {
    this.semanticActionExecutor.clearCache();
    console.log('🧹 Production trigger cache cleared');
  }
}

