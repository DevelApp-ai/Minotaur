/**
 * GrammarDrivenASTMapper - Production Trigger-Based Error Mapping
 *
 * Maps semantic errors to AST transformations using grammar production triggers.
 * Replaces language-specific mapping with grammar-driven approach.
 */

import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';
import { Grammar } from '../core/grammar/Grammar';
import { StructuredValidationError, ErrorType } from './StructuredValidationError';
import { ProductionContext, SemanticValidator as SemanticValidator } from './SemanticValidator';

export interface TransformationCandidate {
  id: string;
  type: TransformationType;
  description: string;
  confidence: number;
  priority: 'HIGH' | 'MEDIUM' | 'LOW';
  astTransformation: ASTTransformation;
  grammarProduction: string;
  validationResult?: ValidationResult;
}

export interface ASTTransformation {
  operation: 'INSERT' | 'DELETE' | 'REPLACE' | 'MOVE';
  targetNode: ZeroCopyASTNode;
  newContent?: string;
  newNode?: ZeroCopyASTNode;
  position?: InsertPosition;
  metadata: TransformationMetadata;
}

export interface TransformationMetadata {
  grammarRule: string;
  semanticContext: string;
  affectedScope: string;
  preserveFormatting: boolean;
  requiresValidation: boolean;
}

export interface InsertPosition {
  type: 'BEFORE' | 'AFTER' | 'CHILD_START' | 'CHILD_END';
  referenceNode: ZeroCopyASTNode;
  index?: number;
}

export interface ValidationResult {
  isValid: boolean;
  grammarCompliant: boolean;
  semanticErrors: StructuredValidationError[];
  warnings: string[];
}

export enum TransformationType {
  VARIABLE_DECLARATION = 'variable_declaration',
  SCOPE_CORRECTION = 'scope_correction',
  IMPORT_ADDITION = 'import_addition',
  SYNTAX_CORRECTION = 'syntax_correction',
  CONTROL_FLOW_FIX = 'control_flow_fix',
  TYPE_ANNOTATION = 'type_annotation',
  INDENTATION_FIX = 'indentation_fix',
  STATEMENT_REMOVAL = 'statement_removal',
  STATEMENT_WRAPPING = 'statement_wrapping'
}

export interface ASTContext {
  sourceCode: string;
  ast: ZeroCopyASTNode;
  errorNode: ZeroCopyASTNode;
  scopeStack: any[];
  typeEnvironment: any;
  controlFlowState: any;
  grammarProductions: string[];
}

/**
 * GrammarDrivenASTMapper - Maps errors to transformations using grammar productions
 */
export class GrammarDrivenASTMapper {
  private grammar: Grammar;
  private semanticValidator: SemanticValidator;
  private transformationStrategies: Map<ErrorType, TransformationStrategy[]>;

  constructor(grammar: Grammar, semanticValidator: SemanticValidator) {
    this.grammar = grammar;
    this.semanticValidator = semanticValidator;
    this.transformationStrategies = new Map();
    this.initializeTransformationStrategies();
  }

  /**
   * Map error to transformation candidates using grammar productions
   */
  async mapErrorToTransformation(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {
    // Get applicable transformation strategies for this error type
    const strategies = this.transformationStrategies.get(error.type) || [];

    // Generate candidates from each strategy
    const candidates: TransformationCandidate[] = [];

    for (const strategy of strategies) {
      const strategyCandidates = await this.generateCandidatesFromStrategy(
        strategy,
        error,
        context,
      );
      candidates.push(...strategyCandidates);
    }

    // Validate candidates against grammar
    const validatedCandidates = await this.validateCandidatesAgainstGrammar(
      candidates,
      context,
    );

    // Rank candidates by confidence and grammar compliance
    const rankedCandidates = this.rankCandidates(validatedCandidates);

    return rankedCandidates;
  }

  /**
   * Generate transformation candidates from a strategy
   */
  private async generateCandidatesFromStrategy(
    strategy: TransformationStrategy,
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    const candidates: TransformationCandidate[] = [];

    switch (strategy.type) {
      case TransformationType.VARIABLE_DECLARATION:
        candidates.push(...await this.generateVariableDeclarationCandidates(error, context));
        break;

      case TransformationType.SCOPE_CORRECTION:
        candidates.push(...await this.generateScopeCorrectionCandidates(error, context));
        break;

      case TransformationType.IMPORT_ADDITION:
        candidates.push(...await this.generateImportAdditionCandidates(error, context));
        break;

      case TransformationType.SYNTAX_CORRECTION:
        candidates.push(...await this.generateSyntaxCorrectionCandidates(error, context));
        break;

      case TransformationType.CONTROL_FLOW_FIX:
        candidates.push(...await this.generateControlFlowFixCandidates(error, context));
        break;

      case TransformationType.STATEMENT_REMOVAL:
        candidates.push(...await this.generateStatementRemovalCandidates(error, context));
        break;

      case TransformationType.STATEMENT_WRAPPING:
        candidates.push(...await this.generateStatementWrappingCandidates(error, context));
        break;
    }

    return candidates;
  }

  /**
   * Generate variable declaration candidates
   */
  private async generateVariableDeclarationCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {
    if (error.type !== ErrorType.NAME_ERROR) {
      return [];
    }

    const variableName = this.extractVariableNameFromError(error);
    if (!variableName) {
      return [];
    }

    const candidates: TransformationCandidate[] = [];

    // Strategy 1: Insert variable declaration at function start
    candidates.push({
      id: `var-decl-func-start-${Date.now()}`,
      type: TransformationType.VARIABLE_DECLARATION,
      description: `Declare '${variableName}' at function start`,
      confidence: 0.8,
      priority: 'HIGH',
      grammarProduction: 'simple_stmt',
      astTransformation: {
        operation: 'INSERT',
        targetNode: this.findFunctionStart(context.errorNode),
        newContent: `${variableName} = None  # Auto-generated variable`,
        position: {
          type: 'CHILD_START',
          referenceNode: this.findFunctionStart(context.errorNode),
        },
        metadata: {
          grammarRule: 'expr_stmt',
          semanticContext: 'variable_declaration',
          affectedScope: 'function',
          preserveFormatting: true,
          requiresValidation: true,
        },
      },
    });

    // Strategy 2: Insert variable declaration before first use
    candidates.push({
      id: `var-decl-before-use-${Date.now()}`,
      type: TransformationType.VARIABLE_DECLARATION,
      description: `Declare '${variableName}' before first use`,
      confidence: 0.9,
      priority: 'HIGH',
      grammarProduction: 'simple_stmt',
      astTransformation: {
        operation: 'INSERT',
        targetNode: context.errorNode,
        newContent: `${variableName} = None  # Auto-generated variable`,
        position: {
          type: 'BEFORE',
          referenceNode: this.findStatementContaining(context.errorNode),
        },
        metadata: {
          grammarRule: 'expr_stmt',
          semanticContext: 'variable_declaration',
          affectedScope: 'local',
          preserveFormatting: true,
          requiresValidation: true,
        },
      },
    });

    return candidates;
  }

  /**
   * Generate scope correction candidates
   */
  private async generateScopeCorrectionCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    const candidates: TransformationCandidate[] = [];

    if (error.type === ErrorType.SYNTAX_ERROR && error.message.includes("'return' outside function")) {
      // Remove return statement
      candidates.push({
        id: `remove-return-${Date.now()}`,
        type: TransformationType.STATEMENT_REMOVAL,
        description: "Remove 'return' statement outside function",
        confidence: 0.9,
        priority: 'HIGH',
        grammarProduction: 'return_stmt',
        astTransformation: {
          operation: 'DELETE',
          targetNode: this.findReturnStatement(context.errorNode),
          metadata: {
            grammarRule: 'return_stmt',
            semanticContext: 'control_flow',
            affectedScope: 'statement',
            preserveFormatting: true,
            requiresValidation: true,
          },
        },
      });

      // Wrap in function
      candidates.push({
        id: `wrap-in-function-${Date.now()}`,
        type: TransformationType.STATEMENT_WRAPPING,
        description: 'Wrap code in function definition',
        confidence: 0.6,
        priority: 'LOW',
        grammarProduction: 'funcdef',
        astTransformation: {
          operation: 'REPLACE',
          targetNode: this.findTopLevelStatement(context.errorNode),
          newContent: `def generated_function():\n    ${this.getStatementText(context.errorNode)}`,
          metadata: {
            grammarRule: 'funcdef',
            semanticContext: 'function_wrapping',
            affectedScope: 'global',
            preserveFormatting: false,
            requiresValidation: true,
          },
        },
      });
    }

    if (error.type === ErrorType.SYNTAX_ERROR &&
        (error.message.includes("'break' outside loop") || error.message.includes("'continue' outside loop"))) {

      const statementType = error.message.includes("'break'") ? 'break' : 'continue';

      // Remove break/continue statement
      candidates.push({
        id: `remove-${statementType}-${Date.now()}`,
        type: TransformationType.STATEMENT_REMOVAL,
        description: `Remove '${statementType}' statement outside loop`,
        confidence: 0.9,
        priority: 'HIGH',
        grammarProduction: `${statementType}_stmt`,
        astTransformation: {
          operation: 'DELETE',
          targetNode: this.findBreakContinueStatement(context.errorNode),
          metadata: {
            grammarRule: `${statementType}_stmt`,
            semanticContext: 'control_flow',
            affectedScope: 'statement',
            preserveFormatting: true,
            requiresValidation: true,
          },
        },
      });
    }

    return candidates;
  }

  /**
   * Generate import addition candidates
   */
  private async generateImportAdditionCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    if (error.type !== ErrorType.NAME_ERROR && error.type !== ErrorType.IMPORT_ERROR) {
      return [];
    }

    const moduleName = this.extractModuleNameFromError(error);
    if (!moduleName) {
      return [];
    }

    const candidates: TransformationCandidate[] = [];

    // Add import statement at file start
    candidates.push({
      id: `add-import-${moduleName}-${Date.now()}`,
      type: TransformationType.IMPORT_ADDITION,
      description: `Add import statement for '${moduleName}'`,
      confidence: 0.7,
      priority: 'MEDIUM',
      grammarProduction: 'import_name',
      astTransformation: {
        operation: 'INSERT',
        targetNode: context.ast,
        newContent: `import ${moduleName}`,
        position: {
          type: 'CHILD_START',
          referenceNode: context.ast,
        },
        metadata: {
          grammarRule: 'import_name',
          semanticContext: 'module_import',
          affectedScope: 'global',
          preserveFormatting: true,
          requiresValidation: true,
        },
      },
    });

    return candidates;
  }

  /**
   * Generate syntax correction candidates
   */
  private async generateSyntaxCorrectionCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    const candidates: TransformationCandidate[] = [];

    // Handle assignment vs comparison errors
    if (error.message.includes('invalid syntax') && context.sourceCode.includes('=')) {
      const line = error.context.errorLine;
      if (line.includes('if ') && line.includes(' = ')) {
        candidates.push({
          id: `fix-assignment-comparison-${Date.now()}`,
          type: TransformationType.SYNTAX_CORRECTION,
          description: 'Change assignment (=) to comparison (==) in if statement',
          confidence: 0.95,
          priority: 'HIGH',
          grammarProduction: 'comparison',
          astTransformation: {
            operation: 'REPLACE',
            targetNode: context.errorNode,
            newContent: line.replace(' = ', ' == '),
            metadata: {
              grammarRule: 'comparison',
              semanticContext: 'operator_correction',
              affectedScope: 'expression',
              preserveFormatting: true,
              requiresValidation: true,
            },
          },
        });
      }
    }

    return candidates;
  }

  /**
   * Generate control flow fix candidates
   */
  private async generateControlFlowFixCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    // Handled in scope correction candidates
    return [];
  }

  /**
   * Generate statement removal candidates
   */
  private async generateStatementRemovalCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    // Handled in scope correction candidates
    return [];
  }

  /**
   * Generate statement wrapping candidates
   */
  private async generateStatementWrappingCandidates(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    // Handled in scope correction candidates
    return [];
  }

  /**
   * Validate candidates against grammar rules
   */
  private async validateCandidatesAgainstGrammar(
    candidates: TransformationCandidate[],
    context: ASTContext,
  ): Promise<TransformationCandidate[]> {

    const validatedCandidates: TransformationCandidate[] = [];

    for (const candidate of candidates) {
      try {
        // Apply transformation to a copy of the AST
        const testAST = this.cloneAST(context.ast);
        const transformedAST = await this.applyTransformationToAST(
          candidate.astTransformation,
          testAST,
        );

        // Validate against grammar
        const validationResult = await this.validateASTAgainstGrammar(transformedAST);

        candidate.validationResult = validationResult;

        if (validationResult.grammarCompliant) {
          validatedCandidates.push(candidate);
        }
      } catch (error) {
        console.warn(`Failed to validate candidate ${candidate.id}:`, error);
      }
    }

    return validatedCandidates;
  }

  /**
   * Rank candidates by confidence and grammar compliance
   */
  private rankCandidates(candidates: TransformationCandidate[]): TransformationCandidate[] {
    return candidates.sort((a, b) => {
      // Primary sort: grammar compliance
      const aCompliant = a.validationResult?.grammarCompliant ? 1 : 0;
      const bCompliant = b.validationResult?.grammarCompliant ? 1 : 0;

      if (aCompliant !== bCompliant) {
        return bCompliant - aCompliant;
      }

      // Secondary sort: confidence
      if (a.confidence !== b.confidence) {
        return b.confidence - a.confidence;
      }

      // Tertiary sort: priority
      const priorityOrder = { 'HIGH': 3, 'MEDIUM': 2, 'LOW': 1 };
      return priorityOrder[b.priority] - priorityOrder[a.priority];
    });
  }

  /**
   * Initialize transformation strategies for different error types
   */
  private initializeTransformationStrategies(): void {
    this.transformationStrategies.set(ErrorType.NAME_ERROR, [
      { type: TransformationType.VARIABLE_DECLARATION, priority: 'HIGH' },
      { type: TransformationType.IMPORT_ADDITION, priority: 'MEDIUM' },
    ]);

    this.transformationStrategies.set(ErrorType.SYNTAX_ERROR, [
      { type: TransformationType.SYNTAX_CORRECTION, priority: 'HIGH' },
      { type: TransformationType.SCOPE_CORRECTION, priority: 'HIGH' },
      { type: TransformationType.STATEMENT_REMOVAL, priority: 'MEDIUM' },
      { type: TransformationType.STATEMENT_WRAPPING, priority: 'LOW' },
    ]);

    this.transformationStrategies.set(ErrorType.IMPORT_ERROR, [
      { type: TransformationType.IMPORT_ADDITION, priority: 'HIGH' },
    ]);

    this.transformationStrategies.set(ErrorType.INDENTATION_ERROR, [
      { type: TransformationType.INDENTATION_FIX, priority: 'HIGH' },
    ]);

    this.transformationStrategies.set(ErrorType.TYPE_ERROR, [
      { type: TransformationType.TYPE_ANNOTATION, priority: 'MEDIUM' },
      { type: TransformationType.SYNTAX_CORRECTION, priority: 'HIGH' },
    ]);
  }

  // Helper methods for AST navigation and manipulation
  private extractVariableNameFromError(error: StructuredValidationError): string | null {
    const match = error.message.match(/name '([^']+)' is not defined/);
    return match ? match[1] : null;
  }

  private extractModuleNameFromError(error: StructuredValidationError): string | null {
    const match = error.message.match(/No module named '([^']+)'/);
    return match ? match[1] : null;
  }

  private findFunctionStart(node: ZeroCopyASTNode): ZeroCopyASTNode {
    // Navigate up to find function definition
    let current = node;
    while (current.getParent() && current.nodeType.toString() !== 'funcdef') {
      current = current.getParent();
    }
    return current.nodeType.toString() === 'funcdef' ? current : node;
  }

  private findStatementContaining(node: ZeroCopyASTNode): ZeroCopyASTNode {
    // Navigate up to find statement node
    let current = node;
    while (current.getParent() && !this.isStatement(current)) {
      current = current.getParent();
    }
    return current;
  }

  private findReturnStatement(node: ZeroCopyASTNode): ZeroCopyASTNode {
    // Find return statement node
    if (node.nodeType.toString() === 'return_stmt') {
      return node;
    }
    return this.findStatementContaining(node);
  }

  private findBreakContinueStatement(node: ZeroCopyASTNode): ZeroCopyASTNode {
    // Find break or continue statement node
    if (node.nodeType.toString() === 'break_stmt' || node.nodeType.toString() === 'continue_stmt') {
      return node;
    }
    return this.findStatementContaining(node);
  }

  private findTopLevelStatement(node: ZeroCopyASTNode): ZeroCopyASTNode {
    // Navigate up to find top-level statement
    let current = node;
    while (current.getParent() && current.getParent().nodeType.toString() !== 'file_input') {
      current = current.getParent();
    }
    return current;
  }

  private isStatement(node: ZeroCopyASTNode): boolean {
    const statementTypes = [
      'expr_stmt', 'simple_stmt', 'compound_stmt', 'if_stmt', 'while_stmt',
      'for_stmt', 'try_stmt', 'with_stmt', 'funcdef', 'classdef', 'return_stmt',
      'break_stmt', 'continue_stmt', 'import_name', 'import_from',
    ];
    return statementTypes.includes(node.nodeType.toString());
  }

  private getStatementText(node: ZeroCopyASTNode): string {
    // Extract text content of statement - simplified approach
    return node.toString() || '';
  }

  private cloneAST(ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Create a deep copy of the AST
    return JSON.parse(JSON.stringify(ast));
  }

  private async applyTransformationToAST(
    transformation: ASTTransformation,
    ast: ZeroCopyASTNode,
  ): Promise<ZeroCopyASTNode> {

    // Apply the transformation to the AST
    switch (transformation.operation) {
      case 'INSERT':
        return this.insertNodeInAST(transformation, ast);
      case 'DELETE':
        return this.deleteNodeFromAST(transformation, ast);
      case 'REPLACE':
        return this.replaceNodeInAST(transformation, ast);
      case 'MOVE':
        return this.moveNodeInAST(transformation, ast);
      default:
        return ast;
    }
  }

  private insertNodeInAST(transformation: ASTTransformation, ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Implementation for inserting nodes
    return ast;
  }

  private deleteNodeFromAST(transformation: ASTTransformation, ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Implementation for deleting nodes
    return ast;
  }

  private replaceNodeInAST(transformation: ASTTransformation, ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Implementation for replacing nodes
    return ast;
  }

  private moveNodeInAST(transformation: ASTTransformation, ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Implementation for moving nodes
    return ast;
  }

  private async validateASTAgainstGrammar(ast: ZeroCopyASTNode): Promise<ValidationResult> {
    // Validate the AST against grammar rules
    // This would integrate with Minotaur's grammar validation
    return {
      isValid: true,
      grammarCompliant: true,
      semanticErrors: [],
      warnings: [],
    };
  }
}

interface TransformationStrategy {
  type: TransformationType;
  priority: 'HIGH' | 'MEDIUM' | 'LOW';
}

