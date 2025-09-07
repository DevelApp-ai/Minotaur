/**
 * SyntacticValidator - Ensures AST manipulations maintain syntactic correctness
 *
 * This system validates AST modifications before they are applied, ensuring that
 * AI-driven changes maintain the structural integrity and syntactic validity of
 * the abstract syntax tree.
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { Grammar, Rule } from '../core/grammar/Grammar';
import { ScopeInfo, VariableInfo, FunctionInfo } from '../context/ContextAwareParser';

export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  suggestions: string[];
}

export interface ValidationError {
  code: string;
  message: string;
  severity: 'error' | 'warning' | 'info';
  position?: {
    line: number;
    column: number;
    offset: number;
  };
  context?: string;
}

export interface ValidationWarning {
  code: string;
  message: string;
  suggestion?: string;
  position?: {
    line: number;
    column: number;
    offset: number;
  };
}

export interface ASTManipulation {
  type: ManipulationType;
  targetNode: ZeroCopyASTNode;
  newNode?: ZeroCopyASTNode;
  newValue?: any;
  position?: number;
  metadata?: any;
}

export enum ManipulationType {
  INSERT_CHILD = 'insert_child',
  REMOVE_CHILD = 'remove_child',
  REPLACE_NODE = 'replace_node',
  MODIFY_VALUE = 'modify_value',
  MOVE_NODE = 'move_node',
  DUPLICATE_NODE = 'duplicate_node'
}

export class SyntacticValidator {
  private grammar: Grammar;
  private contextInfo: Map<number, ScopeInfo>;
  private validationRules: Map<ASTNodeType, ValidationRule[]>;

  constructor(grammar: Grammar) {
    this.grammar = grammar;
    this.contextInfo = new Map();
    this.validationRules = new Map();
    this.initializeValidationRules();
  }

  /**
   * Validates a proposed AST manipulation before it's applied
   */
  public validateManipulation(manipulation: ASTManipulation): ValidationResult {
    const result: ValidationResult = {
      isValid: true,
      errors: [],
      warnings: [],
      suggestions: [],
    };

    try {
      // 1. Structural validation
      this.validateStructuralIntegrity(manipulation, result);

      // 2. Grammar rule compliance
      this.validateGrammarCompliance(manipulation, result);

      // 3. Semantic consistency
      this.validateSemanticConsistency(manipulation, result);

      // 4. Context-aware validation
      this.validateContextualCorrectness(manipulation, result);

      // 5. Type system validation
      this.validateTypeConsistency(manipulation, result);

      // Set overall validity
      result.isValid = result.errors.length === 0;

    } catch (error) {
      result.isValid = false;
      result.errors.push({
        code: 'VALIDATION_ERROR',
        message: `Validation failed: ${error instanceof Error ? error.message : String(error)}`,
        severity: 'error',
      });
    }

    return result;
  }

  /**
   * Validates that the manipulation maintains AST structural integrity
   */
  private validateStructuralIntegrity(manipulation: ASTManipulation, result: ValidationResult): void {
    const { type, targetNode, newNode } = manipulation;

    switch (type) {
      case ManipulationType.INSERT_CHILD:
        this.validateChildInsertion(targetNode, newNode!, result);
        break;

      case ManipulationType.REMOVE_CHILD:
        this.validateChildRemoval(targetNode, result);
        break;

      case ManipulationType.REPLACE_NODE:
        this.validateNodeReplacement(targetNode, newNode!, result);
        break;

      case ManipulationType.MODIFY_VALUE:
        this.validateValueModification(targetNode, manipulation.newValue, result);
        break;

      case ManipulationType.MOVE_NODE:
        this.validateNodeMove(targetNode, result);
        break;

      case ManipulationType.DUPLICATE_NODE:
        this.validateNodeDuplication(targetNode, result);
        break;
    }
  }

  /**
   * Validates that the manipulation complies with grammar rules
   */
  private validateGrammarCompliance(manipulation: ASTManipulation, result: ValidationResult): void {
    const { targetNode, newNode } = manipulation;

    // Get applicable grammar rules for the target node
    const rules = this.getApplicableRules(targetNode);

    for (const rule of rules) {
      if (!this.checkRuleCompliance(manipulation, rule)) {
        result.errors.push({
          code: 'GRAMMAR_VIOLATION',
          message: `Manipulation violates grammar rule: ${rule.name}`,
          severity: 'error',
          context: rule.definition,
        });
      }
    }

    // Validate production rules
    if (newNode) {
      this.validateProductionRules(newNode, result);
    }
  }

  /**
   * Validates semantic consistency of the manipulation
   */
  private validateSemanticConsistency(manipulation: ASTManipulation, result: ValidationResult): void {
    const { targetNode, newNode } = manipulation;

    // Check for semantic conflicts
    if (newNode) {
      const conflicts = this.findSemanticConflicts(targetNode, newNode);
      for (const conflict of conflicts) {
        result.errors.push({
          code: 'SEMANTIC_CONFLICT',
          message: conflict.message,
          severity: 'error',
          context: conflict.context,
        });
      }
    }

    // Validate identifier scoping
    this.validateIdentifierScoping(manipulation, result);

    // Check for undefined references
    this.validateReferences(manipulation, result);
  }

  /**
   * Validates contextual correctness based on surrounding code
   */
  private validateContextualCorrectness(manipulation: ASTManipulation, result: ValidationResult): void {
    const { targetNode } = manipulation;
    const context = this.contextInfo.get(targetNode.nodeId);

    if (context) {
      // Validate scope rules
      this.validateScopeRules(manipulation, context, result);

      // Check accessibility rules
      this.validateAccessibilityRules(manipulation, context, result);

      // Validate control flow integrity
      this.validateControlFlowIntegrity(manipulation, context, result);
    }
  }

  /**
   * Validates type system consistency
   */
  private validateTypeConsistency(manipulation: ASTManipulation, result: ValidationResult): void {
    const { targetNode, newNode } = manipulation;

    if (newNode) {
      // Check type compatibility
      const targetType = this.inferNodeType(targetNode);
      const newType = this.inferNodeType(newNode);

      if (!this.areTypesCompatible(targetType, newType)) {
        result.errors.push({
          code: 'TYPE_MISMATCH',
          message: `Type mismatch: cannot assign ${newType} to ${targetType}`,
          severity: 'error',
        });
      }
    }

    // Validate expression types
    this.validateExpressionTypes(manipulation, result);
  }

  /**
   * Initialize validation rules for different node types
   */
  private initializeValidationRules(): void {
    // Function declaration rules
    this.validationRules.set(ASTNodeType.FUNCTION_DECLARATION, [
      {
        name: 'function_signature',
        validate: (node: ZeroCopyASTNode) => this.validateFunctionSignature(node),
        message: 'Invalid function signature',
      },
      {
        name: 'return_type_consistency',
        validate: (node: ZeroCopyASTNode) => this.validateReturnTypeConsistency(node),
        message: 'Return type inconsistency',
      },
    ]);

    // Variable declaration rules
    this.validationRules.set(ASTNodeType.VARIABLE_DECLARATION, [
      {
        name: 'variable_naming',
        validate: (node: ZeroCopyASTNode) => this.validateVariableNaming(node),
        message: 'Invalid variable name',
      },
      {
        name: 'initialization_consistency',
        validate: (node: ZeroCopyASTNode) => this.validateInitializationConsistency(node),
        message: 'Initialization type mismatch',
      },
    ]);

    // Expression rules
    this.validationRules.set(ASTNodeType.EXPRESSION, [
      {
        name: 'operator_precedence',
        validate: (node: ZeroCopyASTNode) => this.validateOperatorPrecedence(node),
        message: 'Invalid operator precedence',
      },
      {
        name: 'operand_compatibility',
        validate: (node: ZeroCopyASTNode) => this.validateOperandCompatibility(node),
        message: 'Incompatible operands',
      },
    ]);

    // Control flow rules
    this.validationRules.set(ASTNodeType.IF_STATEMENT, [
      {
        name: 'condition_type',
        validate: (node: ZeroCopyASTNode) => this.validateConditionType(node),
        message: 'Condition must be boolean expression',
      },
    ]);

    this.validationRules.set(ASTNodeType.WHILE_LOOP, [
      {
        name: 'loop_condition',
        validate: (node: ZeroCopyASTNode) => this.validateLoopCondition(node),
        message: 'Invalid loop condition',
      },
    ]);
  }

  // Helper methods for specific validations
  private validateChildInsertion(parent: ZeroCopyASTNode, child: ZeroCopyASTNode, result: ValidationResult): void {
    // Check if parent can accept this type of child
    const parentType = parent.nodeType;
    const childType = child.nodeType;

    if (!this.canParentAcceptChild(parentType, childType)) {
      result.errors.push({
        code: 'INVALID_CHILD_TYPE',
        message: `Node of type ${ASTNodeType[parentType]} cannot have child of type ${ASTNodeType[childType]}`,
        severity: 'error',
      });
    }

    // Check for maximum children constraints
    const maxChildren = this.getMaxChildrenForNodeType(parentType);
    if (maxChildren > 0 && parent.childCount >= maxChildren) {
      result.errors.push({
        code: 'MAX_CHILDREN_EXCEEDED',
        message: `Node of type ${ASTNodeType[parentType]} cannot have more than ${maxChildren} children`,
        severity: 'error',
      });
    }
  }

  private validateChildRemoval(parent: ZeroCopyASTNode, result: ValidationResult): void {
    const minChildren = this.getMinChildrenForNodeType(parent.nodeType);
    if (parent.childCount <= minChildren) {
      result.errors.push({
        code: 'MIN_CHILDREN_REQUIRED',
        message: `Node of type ${ASTNodeType[parent.nodeType]} requires at least ${minChildren} children`,
        severity: 'error',
      });
    }
  }

  private validateNodeReplacement(oldNode: ZeroCopyASTNode, newNode: ZeroCopyASTNode, result: ValidationResult): void {
    // Check if replacement maintains structural constraints
    const parent = oldNode.getParent();
    if (parent && !this.canParentAcceptChild(parent.nodeType, newNode.nodeType)) {
      result.errors.push({
        code: 'INVALID_REPLACEMENT',
        message: `Cannot replace ${ASTNodeType[oldNode.nodeType]} with ${ASTNodeType[newNode.nodeType]}`,
        severity: 'error',
      });
    }
  }

  private validateValueModification(node: ZeroCopyASTNode, newValue: any, result: ValidationResult): void {
    // Validate that the new value is appropriate for the node type
    if (!this.isValueValidForNodeType(node.nodeType, newValue)) {
      result.errors.push({
        code: 'INVALID_VALUE',
        message: `Value ${newValue} is not valid for node type ${ASTNodeType[node.nodeType]}`,
        severity: 'error',
      });
    }
  }

  private validateNodeMove(node: ZeroCopyASTNode, result: ValidationResult): void {
    // Check if moving the node would break dependencies
    const dependencies = this.findNodeDependencies(node);
    for (const dep of dependencies) {
      if (!this.canMoveDependency(node, dep)) {
        result.errors.push({
          code: 'DEPENDENCY_VIOLATION',
          message: `Moving node would break dependency: ${dep.description}`,
          severity: 'error',
        });
      }
    }
  }

  private validateNodeDuplication(node: ZeroCopyASTNode, result: ValidationResult): void {
    // Check if duplication would create naming conflicts
    if (this.wouldCreateNamingConflict(node)) {
      result.warnings.push({
        code: 'NAMING_CONFLICT',
        message: 'Duplicating this node may create naming conflicts',
        suggestion: 'Consider renaming the duplicated node',
      });
    }
  }

  // Grammar compliance helpers
  private getApplicableRules(node: ZeroCopyASTNode): Rule[] {
    // Return grammar rules that apply to this node type
    return this.grammar.rules.filter(rule =>
      this.ruleAppliesTo(rule, node.nodeType),
    );
  }

  private checkRuleCompliance(manipulation: ASTManipulation, rule: Rule): boolean {
    // Check if the manipulation complies with the given grammar rule
    // This would involve parsing the rule definition and checking constraints
    return true; // Simplified for now
  }

  private validateProductionRules(node: ZeroCopyASTNode, result: ValidationResult): void {
    // Validate that the node follows production rules from the grammar
    const nodeType = node.nodeType;
    const rules = this.validationRules.get(nodeType);

    if (rules) {
      for (const rule of rules) {
        if (!rule.validate(node)) {
          result.errors.push({
            code: rule.name.toUpperCase(),
            message: rule.message,
            severity: 'error',
          });
        }
      }
    }
  }

  // Semantic validation helpers
  // eslint-disable-next-line max-len
  private findSemanticConflicts(targetNode: ZeroCopyASTNode, newNode: ZeroCopyASTNode): Array<{message: string, context: string}> {
    const conflicts: Array<{message: string, context: string}> = [];

    // Check for variable redeclaration
    if (this.isVariableRedeclaration(targetNode, newNode)) {
      conflicts.push({
        message: 'Variable redeclaration detected',
        context: 'Same scope variable declaration',
      });
    }

    // Check for function signature conflicts
    if (this.isFunctionSignatureConflict(targetNode, newNode)) {
      conflicts.push({
        message: 'Function signature conflict',
        context: 'Overloading not allowed',
      });
    }

    return conflicts;
  }

  private validateIdentifierScoping(manipulation: ASTManipulation, result: ValidationResult): void {
    // Validate that identifiers are properly scoped
    const { newNode } = manipulation;
    if (newNode && this.containsIdentifiers(newNode)) {
      const identifiers = this.extractIdentifiers(newNode);
      for (const identifier of identifiers) {
        if (!this.isIdentifierInScope(identifier)) {
          result.errors.push({
            code: 'UNDEFINED_IDENTIFIER',
            message: `Identifier '${identifier.name}' is not in scope`,
            severity: 'error',
          });
        }
      }
    }
  }

  private validateReferences(manipulation: ASTManipulation, result: ValidationResult): void {
    // Check for undefined references
    const references = this.findReferences(manipulation);
    for (const ref of references) {
      if (!this.isReferenceDefined(ref)) {
        result.errors.push({
          code: 'UNDEFINED_REFERENCE',
          message: `Reference to '${ref.name}' is undefined`,
          severity: 'error',
        });
      }
    }
  }

  // Context validation helpers
  private validateScopeRules(manipulation: ASTManipulation, context: ScopeInfo, result: ValidationResult): void {
    // Validate scope-specific rules
    if (context.type === 'function' && manipulation.type === ManipulationType.INSERT_CHILD) {
      // Check if inserting return statement in void function
      if (this.isReturnStatement(manipulation.newNode!) && this.isFunctionVoid(context)) {
        result.warnings.push({
          code: 'VOID_FUNCTION_RETURN',
          message: 'Return statement in void function',
          suggestion: 'Consider removing the return value',
        });
      }
    }
  }

  // eslint-disable-next-line max-len
  private validateAccessibilityRules(manipulation: ASTManipulation, context: ScopeInfo, result: ValidationResult): void {
    // Validate accessibility modifiers
    const { newNode } = manipulation;
    if (newNode && this.hasAccessibilityModifier(newNode)) {
      const modifier = this.getAccessibilityModifier(newNode);
      if (!this.isAccessibilityValidInContext(modifier, context)) {
        result.errors.push({
          code: 'INVALID_ACCESSIBILITY',
          message: `${modifier} modifier not valid in this context`,
          severity: 'error',
        });
      }
    }
  }

  // eslint-disable-next-line max-len
  private validateControlFlowIntegrity(manipulation: ASTManipulation, context: ScopeInfo, result: ValidationResult): void {
    // Validate that control flow remains intact
    if (this.breaksControlFlow(manipulation, context)) {
      result.errors.push({
        code: 'CONTROL_FLOW_BREAK',
        message: 'Manipulation breaks control flow integrity',
        severity: 'error',
      });
    }
  }

  // Type system helpers
  private inferNodeType(node: ZeroCopyASTNode): string {
    // Infer the type of a node based on its structure and context
    switch (node.nodeType) {
      case ASTNodeType.LITERAL:
        return this.inferLiteralType(node);
      case ASTNodeType.IDENTIFIER:
        return this.inferIdentifierType(node);
      case ASTNodeType.BINARY_OP:
        return this.inferBinaryOpType(node);
      case ASTNodeType.EXPRESSION:
        // For expressions, try to infer from children
        if (node.childCount > 0) {
          const firstChild = node.getChild(0);
          if (firstChild) {
            return this.inferNodeType(firstChild);
          }
        }
        return 'unknown';
      default:
        return 'unknown';
    }
  }

  private areTypesCompatible(type1: string, type2: string): boolean {
    // Check if two types are compatible
    if (type1 === type2) {
      return true;
    }

    // Check for implicit conversions
    const compatibilityMap: Record<string, string[]> = {
      'number': ['int', 'float', 'double'],
      'string': ['char[]', 'text'],
      'boolean': ['bool'],
    };

    return compatibilityMap[type1]?.includes(type2) ||
           compatibilityMap[type2]?.includes(type1) ||
           false;
  }

  private validateExpressionTypes(manipulation: ASTManipulation, result: ValidationResult): void {
    // Validate expression type consistency
    const { newNode } = manipulation;
    if (newNode && newNode.nodeType === ASTNodeType.EXPRESSION) {
      const expressionType = this.inferExpressionType(newNode);
      const expectedType = this.getExpectedTypeInContext(newNode);

      if (expectedType && !this.areTypesCompatible(expressionType, expectedType)) {
        result.errors.push({
          code: 'EXPRESSION_TYPE_MISMATCH',
          message: `Expression type ${expressionType} does not match expected type ${expectedType}`,
          severity: 'error',
        });
      }
    }
  }

  // Placeholder implementations for helper methods
  private canParentAcceptChild(parentType: ASTNodeType, childType: ASTNodeType): boolean {
    // Define valid parent-child relationships
    const validRelationships: Record<ASTNodeType, ASTNodeType[]> = {
      // eslint-disable-next-line max-len
      [ASTNodeType.PROGRAM]: [ASTNodeType.STATEMENT, ASTNodeType.DECLARATION, ASTNodeType.FUNCTION_DECLARATION, ASTNodeType.CLASS_DECLARATION, ASTNodeType.VARIABLE_DECLARATION],
      [ASTNodeType.STATEMENT]: [ASTNodeType.EXPRESSION, ASTNodeType.ASSIGNMENT, ASTNodeType.RETURN_STATEMENT],
      // eslint-disable-next-line max-len
      [ASTNodeType.EXPRESSION]: [ASTNodeType.IDENTIFIER, ASTNodeType.LITERAL, ASTNodeType.BINARY_OP, ASTNodeType.UNARY_OP, ASTNodeType.FUNCTION_CALL, ASTNodeType.MEMBER_ACCESS, ASTNodeType.ARRAY_ACCESS],
      [ASTNodeType.DECLARATION]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      [ASTNodeType.IDENTIFIER]: [], // Leaf node
      [ASTNodeType.LITERAL]: [], // Leaf node
      [ASTNodeType.BINARY_OP]: [ASTNodeType.EXPRESSION, ASTNodeType.IDENTIFIER, ASTNodeType.LITERAL],
      [ASTNodeType.UNARY_OP]: [ASTNodeType.EXPRESSION, ASTNodeType.IDENTIFIER, ASTNodeType.LITERAL],
      [ASTNodeType.FUNCTION_CALL]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      [ASTNodeType.BLOCK]: [ASTNodeType.STATEMENT, ASTNodeType.DECLARATION, ASTNodeType.EXPRESSION],
      [ASTNodeType.IF_STATEMENT]: [ASTNodeType.EXPRESSION, ASTNodeType.STATEMENT, ASTNodeType.BLOCK],
      [ASTNodeType.WHILE_LOOP]: [ASTNodeType.EXPRESSION, ASTNodeType.STATEMENT, ASTNodeType.BLOCK],
      // eslint-disable-next-line max-len
      [ASTNodeType.FOR_LOOP]: [ASTNodeType.EXPRESSION, ASTNodeType.STATEMENT, ASTNodeType.DECLARATION, ASTNodeType.BLOCK],
      [ASTNodeType.RETURN_STATEMENT]: [ASTNodeType.EXPRESSION],
      [ASTNodeType.ASSIGNMENT]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      [ASTNodeType.MEMBER_ACCESS]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      [ASTNodeType.ARRAY_ACCESS]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      [ASTNodeType.FUNCTION_DECLARATION]: [ASTNodeType.IDENTIFIER, ASTNodeType.STATEMENT, ASTNodeType.BLOCK],
      [ASTNodeType.VARIABLE_DECLARATION]: [ASTNodeType.IDENTIFIER, ASTNodeType.EXPRESSION],
      // eslint-disable-next-line max-len
      [ASTNodeType.CLASS_DECLARATION]: [ASTNodeType.IDENTIFIER, ASTNodeType.FUNCTION_DECLARATION, ASTNodeType.VARIABLE_DECLARATION],
      [ASTNodeType.IMPORT_STATEMENT]: [ASTNodeType.IDENTIFIER, ASTNodeType.LITERAL],
      [ASTNodeType.ERROR]: [], // Error nodes typically don't have children
    };

    return validRelationships[parentType]?.includes(childType) ?? false;
  }

  private getMaxChildrenForNodeType(nodeType: ASTNodeType): number {
    // Define maximum children constraints
    switch (nodeType) {
      case ASTNodeType.BINARY_OP:
        return 2; // Binary operations have exactly 2 operands
      case ASTNodeType.UNARY_OP:
        return 1; // Unary operations have exactly 1 operand
      case ASTNodeType.IF_STATEMENT:
        return 3; // condition, then-branch, else-branch
      case ASTNodeType.WHILE_LOOP:
        return 2; // condition, body
      case ASTNodeType.FOR_LOOP:
        return 4; // init, condition, increment, body
      case ASTNodeType.FUNCTION_CALL:
        return -1; // Variable number of arguments
      case ASTNodeType.PROGRAM:
      case ASTNodeType.BLOCK:
      case ASTNodeType.FUNCTION_DECLARATION:
        return -1; // No limit
      default:
        return -1; // No limit by default
    }
  }

  private getMinChildrenForNodeType(nodeType: ASTNodeType): number {
    // Define minimum children constraints
    switch (nodeType) {
      case ASTNodeType.BINARY_OP:
        return 2; // Binary operations require 2 operands
      case ASTNodeType.UNARY_OP:
        return 1; // Unary operations require 1 operand
      case ASTNodeType.IF_STATEMENT:
        return 2; // At least condition and then-branch
      case ASTNodeType.WHILE_LOOP:
        return 2; // condition and body
      case ASTNodeType.FOR_LOOP:
        return 4; // All components required
      case ASTNodeType.FUNCTION_CALL:
        return 1; // At least the function name/reference
      case ASTNodeType.ASSIGNMENT:
        return 2; // left side and right side
      default:
        return 0;
    }
  }

  private isValueValidForNodeType(nodeType: ASTNodeType, value: any): boolean {
    // Validate node values based on type
    switch (nodeType) {
      case ASTNodeType.LITERAL:
        return value !== null && value !== undefined;
      case ASTNodeType.IDENTIFIER:
        return typeof value === 'string' && /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(value);
      case ASTNodeType.BINARY_OP:
      case ASTNodeType.UNARY_OP: {
        const validOperators = ['+', '-', '*', '/', '%', '=', '==', '!=', '<', '>', '<=', '>=', '&&', '||', '!'];
        return validOperators.includes(value);
      }
      default:
        return true; // Allow any value for unknown types
    }
  }

  private findNodeDependencies(node: ZeroCopyASTNode, visited: Set<number> = new Set()): Array<{description: string}> {
    const dependencies: Array<{description: string}> = [];

    // Prevent infinite recursion by tracking visited nodes
    if (visited.has(node.nodeId)) {
      return dependencies;
    }
    visited.add(node.nodeId);

    // Find identifiers that this node depends on
    if (node.nodeType === ASTNodeType.IDENTIFIER && node.value) {
      dependencies.push({ description: `Depends on identifier: ${node.value}` });
    }

    // Find function calls
    if (node.nodeType === ASTNodeType.FUNCTION_CALL && node.value) {
      dependencies.push({ description: `Depends on function: ${node.value}` });
    }

    // Recursively check children
    for (const child of node.getChildren()) {
      dependencies.push(...this.findNodeDependencies(child, visited));
    }

    return dependencies;
  }

  private canMoveDependency(node: ZeroCopyASTNode, dep: {description: string}): boolean {
    // Simple heuristic - can move if it's not a function dependency
    return !dep.description.includes('function');
  }

  private wouldCreateNamingConflict(node: ZeroCopyASTNode): boolean {
    // Check if adding this node would create naming conflicts
    if (node.nodeType === ASTNodeType.IDENTIFIER ||
        node.nodeType === ASTNodeType.FUNCTION_DECLARATION ||
        node.nodeType === ASTNodeType.VARIABLE_DECLARATION) {
      // In a real implementation, would check against symbol table
      // For now, assume no conflicts
      return false;
    }
    return false;
  }

  private ruleAppliesTo(rule: Rule, nodeType: ASTNodeType): boolean {
    // Check if a grammar rule applies to a specific node type
    // This would need actual rule implementation details
    return true; // Simplified - assume all rules apply
  }

  private validateFunctionSignature(node: ZeroCopyASTNode): boolean {
    // Validate function signature consistency
    if (node.nodeType !== ASTNodeType.FUNCTION_DECLARATION) {
      return true;
    }

    // Check that function has a name
    if (!node.value || typeof node.value !== 'string') {
      return false;
    }

    // Check that any child parameters are valid (simplified)
    for (const child of node.getChildren()) {
      if (child.nodeType === ASTNodeType.IDENTIFIER) {
        // Parameter validation
        if (!child.value || typeof child.value !== 'string') {
          return false;
        }
      }
    }

    return true;
  }

  private validateParameterList(parameterList: ZeroCopyASTNode): boolean {
    // Validate that all parameters are properly formed
    if (parameterList.childCount === 0) {
      return true; // Empty parameter list is valid
    }

    for (const param of parameterList.getChildren()) {
      // Parameters are typically identifiers in this system
      if (param.nodeType !== ASTNodeType.IDENTIFIER) {
        return false;
      }

      // Check parameter has a name
      if (!param.value || typeof param.value !== 'string') {
        return false;
      }
    }

    return true;
  }

  private validateReturnTypeConsistency(node: ZeroCopyASTNode): boolean {
    // Validate that return statements match function return type
    if (node.nodeType === ASTNodeType.FUNCTION_DECLARATION) {
      const returnStatements = this.findReturnStatements(node);
      if (returnStatements.length > 0) {
        // In a real implementation, would check type consistency
        return true;
      }
    }
    return true;
  }

  private findReturnStatements(node: ZeroCopyASTNode, visited: Set<number> = new Set()): ZeroCopyASTNode[] {
    const returnStatements: ZeroCopyASTNode[] = [];

    // Prevent infinite recursion by tracking visited nodes
    if (visited.has(node.nodeId)) {
      return returnStatements;
    }
    visited.add(node.nodeId);

    if (node.nodeType === ASTNodeType.RETURN_STATEMENT) {
      returnStatements.push(node);
    }

    for (const child of node.getChildren()) {
      returnStatements.push(...this.findReturnStatements(child, visited));
    }

    return returnStatements;
  }

  private validateVariableNaming(node: ZeroCopyASTNode): boolean {
    // Validate variable naming conventions
    if (node.nodeType === ASTNodeType.VARIABLE_DECLARATION ||
        node.nodeType === ASTNodeType.IDENTIFIER) {
      const name = node.value;
      if (typeof name === 'string') {
        // Check for valid identifier pattern
        return /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(name);
      }
    }
    return true;
  }

  private validateInitializationConsistency(node: ZeroCopyASTNode): boolean {
    // Validate that variables are properly initialized
    if (node.nodeType === ASTNodeType.VARIABLE_DECLARATION) {
      // Check if variable has an initializer
      const hasInitializer = node.getChildren().some(child =>
        child.nodeType === ASTNodeType.ASSIGNMENT ||
        child.nodeType === ASTNodeType.EXPRESSION,
      );

      // For const declarations, initialization is required
      if (node.value && String(node.value).includes('const')) {
        return hasInitializer || false;
      }
    }
    return true;
  }

  private validateOperatorPrecedence(node: ZeroCopyASTNode): boolean {
    // Validate operator precedence in expressions
    if (node.nodeType === ASTNodeType.BINARY_OP) {
      // In a real implementation, would check precedence rules
      return true;
    }
    return true;
  }

  private validateOperandCompatibility(node: ZeroCopyASTNode): boolean {
    // Validate that operands are compatible with operators
    if (node.nodeType === ASTNodeType.BINARY_OP && node.childCount >= 2) {
      const leftChild = node.getChild(0);
      const rightChild = node.getChild(1);

      if (leftChild && rightChild) {
        const leftType = this.inferNodeType(leftChild);
        const rightType = this.inferNodeType(rightChild);

        // Check basic compatibility
        return this.areTypesCompatible(leftType, rightType);
      }
    }
    return true;
  }

  private validateConditionType(node: ZeroCopyASTNode): boolean {
    // Validate that conditions evaluate to boolean
    if (node.nodeType === ASTNodeType.IF_STATEMENT ||
        node.nodeType === ASTNodeType.WHILE_LOOP) {
      const condition = node.getChild(0);
      if (condition) {
        const conditionType = this.inferNodeType(condition);
        return conditionType === 'boolean' || conditionType === 'unknown';
      }
    }
    return true;
  }

  private validateLoopCondition(node: ZeroCopyASTNode): boolean {
    // Validate loop condition structure
    if (node.nodeType === ASTNodeType.FOR_LOOP) {
      // Validate for loop structure (init; condition; increment)
      return node.childCount === 4; // init, condition, increment, body
    }

    if (node.nodeType === ASTNodeType.WHILE_LOOP) {
      // Validate while loop structure
      return node.childCount === 2; // condition, body
    }

    return true;
  }

  // Helper methods for type inference
  private inferLiteralType(node: ZeroCopyASTNode): string {
    const value = node.value;
    if (typeof value === 'number') {
      return 'number';
    } else if (typeof value === 'string') {
      return 'string';
    } else if (typeof value === 'boolean') {
      return 'boolean';
    }
    return 'unknown';
  }

  private inferIdentifierType(node: ZeroCopyASTNode): string {
    // In a real implementation, would look up in symbol table
    return 'unknown';
  }

  private inferBinaryOpType(node: ZeroCopyASTNode): string {
    if (node.childCount < 2) {
      return 'unknown';
    }

    const leftChild = node.getChild(0);
    const rightChild = node.getChild(1);

    if (!leftChild || !rightChild) {
      return 'unknown';
    }

    const leftType = this.inferNodeType(leftChild);
    const rightType = this.inferNodeType(rightChild);
    const operator = node.value;

    // Handle arithmetic operators
    if (['+', '-', '*', '/', '%'].includes(String(operator))) {
      if (leftType === 'number' && rightType === 'number') {
        return 'number';
      }
      if (operator === '+' && (leftType === 'string' || rightType === 'string')) {
        return 'string'; // String concatenation
      }
    }

    // Handle comparison operators
    if (['==', '!=', '<', '>', '<=', '>='].includes(String(operator))) {
      return 'boolean';
    }

    // Handle logical operators
    if (['&&', '||'].includes(String(operator))) {
      return 'boolean';
    }

    return 'unknown';
  }

  private inferExpressionType(node: ZeroCopyASTNode): string {
    return this.inferNodeType(node);
  }

  private getExpectedTypeInContext(node: ZeroCopyASTNode): string | null {
    // In a real implementation, would determine expected type from context
    return null;
  }
  private isVariableRedeclaration(targetNode: ZeroCopyASTNode, newNode: ZeroCopyASTNode): boolean {
    return false;
  }
  private isFunctionSignatureConflict(targetNode: ZeroCopyASTNode, newNode: ZeroCopyASTNode): boolean {
    return false;
  }
  private containsIdentifiers(node: ZeroCopyASTNode): boolean {
    return false;
  }
  private extractIdentifiers(node: ZeroCopyASTNode): Array<{name: string}> {
    return [];
  }
  private isIdentifierInScope(identifier: {name: string}): boolean {
    return true;
  }
  private findReferences(manipulation: ASTManipulation): Array<{name: string}> {
    return [];
  }
  private isReferenceDefined(ref: {name: string}): boolean {
    return true;
  }
  private isReturnStatement(node: ZeroCopyASTNode): boolean {
    return node.nodeType === ASTNodeType.RETURN_STATEMENT;
  }
  private isFunctionVoid(context: ScopeInfo): boolean {
    return false;
  }
  private hasAccessibilityModifier(node: ZeroCopyASTNode): boolean {
    return false;
  }
  private getAccessibilityModifier(node: ZeroCopyASTNode): string {
    return 'public';
  }
  private isAccessibilityValidInContext(modifier: string, context: ScopeInfo): boolean {
    return true;
  }
  private breaksControlFlow(manipulation: ASTManipulation, context: ScopeInfo): boolean {
    return false;
  }
}

interface ValidationRule {
  name: string;
  validate: (node: ZeroCopyASTNode) => boolean;
  message: string;
}

