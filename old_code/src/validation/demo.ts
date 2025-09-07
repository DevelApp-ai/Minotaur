/**
 * Demonstration of Minotaur's syntactic validation capabilities
 *
 * This script shows how Minotaur can restrict AST manipulations to syntactically
 * correct operations through AI integration with validation guards.
 */

import { SyntacticValidator, ASTManipulation, ManipulationType } from './SyntacticValidator';
import { ASTGuard, ASTGuardFactory } from './ASTGuard';
import { SafeAIAgent, AIManipulationRequest } from '../agents/SafeAIAgent';
import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { Grammar } from '../core/grammar/Grammar';

// Mock implementations for demonstration
class DemoASTNode {
  private nodeType: ASTNodeType;
  private nodeId: number;
  private children: DemoASTNode[] = [];
  private parent: DemoASTNode | null = null;
  private value: any;

  constructor(nodeType: ASTNodeType, value?: any) {
    this.nodeType = nodeType;
    this.nodeId = Math.floor(Math.random() * 1000);
    this.value = value;
  }

  getNodeType(): ASTNodeType {
    return this.nodeType;
  }
  getNodeId(): number {
    return this.nodeId;
  }
  getChildCount(): number {
    return this.children.length;
  }
  getChildren(): DemoASTNode[] {
    return this.children;
  }
  getParent(): DemoASTNode | null {
    return this.parent;
  }
  getValue(): any {
    return this.value;
  }
  setValue(value: any): void {
    this.value = value;
  }

  addChild(child: DemoASTNode, position?: number): boolean {
    if (position !== undefined) {
      this.children.splice(position, 0, child);
    } else {
      this.children.push(child);
    }
    child.parent = this;
    return true;
  }

  removeChild(index: number): boolean {
    if (index >= 0 && index < this.children.length) {
      const child = this.children[index];
      this.children.splice(index, 1);
      child.parent = null;
      return true;
    }
    return false;
  }

  replaceWith(newNode: DemoASTNode): boolean {
    if (this.parent) {
      const index = this.parent.children.indexOf(this);
      if (index !== -1) {
        this.parent.children[index] = newNode;
        newNode.parent = this.parent;
        this.parent = null;
        return true;
      }
    }
    return false;
  }

  toString(): string {
    const typeName = ASTNodeType[this.nodeType];
    const valueStr = this.value ? ` (${this.value})` : '';
    return `${typeName}${valueStr}`;
  }

  toTreeString(indent: string = ''): string {
    let result = `${indent}${this.toString()}\n`;
    for (const child of this.children) {
      result += child.toTreeString(indent + '  ');
    }
    return result;
  }
}

class DemoGrammar {
  name: string = 'DemoGrammar';
  rules: any[] = [
    { name: 'program', definition: 'statement*' },
    { name: 'statement', definition: 'expression | declaration | control_flow' },
    { name: 'expression', definition: 'identifier | literal | binary_op | function_call' },
    { name: 'declaration', definition: 'var_decl | func_decl | class_decl' },
    { name: 'control_flow', definition: 'if_stmt | while_loop | for_loop' },
  ];

  getRule(name: string): any {
    return this.rules.find(rule => rule.name === name);
  }
}

export class SyntacticValidationDemo {
  private validator: SyntacticValidator;
  private guard: ASTGuard;
  private safeAgent: SafeAIAgent;
  private grammar: DemoGrammar;

  constructor() {
    this.grammar = new DemoGrammar();
    this.validator = new SyntacticValidator(this.grammar as any);
    this.guard = ASTGuardFactory.createDevelopmentGuard(this.grammar as any);
    this.safeAgent = new SafeAIAgent(this.grammar as any, {
      safetyMode: 'balanced',
      autoCorrection: true,
      strictValidation: false,
    });
  }

  /**
   * Demonstrates basic validation of AST manipulations
   */
  public demonstrateBasicValidation(): void {
    // eslint-disable-next-line no-console
    console.log('=== Basic Syntactic Validation Demo ===\n');

    // Create a simple AST structure
    const program = new DemoASTNode(ASTNodeType.PROGRAM);
    const block = new DemoASTNode(ASTNodeType.BLOCK);
    const statement = new DemoASTNode(ASTNodeType.STATEMENT);

    program.addChild(block);
    block.addChild(statement);

    // eslint-disable-next-line no-console
    console.log('Initial AST structure:');
    // eslint-disable-next-line no-console
    console.log(program.toTreeString());

    // Test 1: Valid manipulation - adding a statement to a block
    // eslint-disable-next-line no-console
    console.log('Test 1: Adding a statement to a block (VALID)');
    const validManipulation: ASTManipulation = {
      type: ManipulationType.INSERT_CHILD,
      targetNode: block as any,
      newNode: new DemoASTNode(ASTNodeType.STATEMENT, 'new statement') as any,
    };

    const validResult = this.validator.validateManipulation(validManipulation);
    // eslint-disable-next-line no-console
    console.log(`Result: ${validResult.isValid ? 'ALLOWED' : 'BLOCKED'}`);
    // eslint-disable-next-line no-console
    console.log(`Errors: ${validResult.errors.length}`);
    // eslint-disable-next-line no-console
    console.log(`Warnings: ${validResult.warnings.length}\n`);

    // Test 2: Invalid manipulation - adding a block to a literal
    // eslint-disable-next-line no-console
    console.log('Test 2: Adding a block to a literal (INVALID)');
    const literal = new DemoASTNode(ASTNodeType.LITERAL, 42);
    const invalidManipulation: ASTManipulation = {
      type: ManipulationType.INSERT_CHILD,
      targetNode: literal as any,
      newNode: new DemoASTNode(ASTNodeType.BLOCK) as any,
    };

    const invalidResult = this.validator.validateManipulation(invalidManipulation);
    // eslint-disable-next-line no-console
    console.log(`Result: ${invalidResult.isValid ? 'ALLOWED' : 'BLOCKED'}`);
    // eslint-disable-next-line no-console
    console.log(`Errors: ${invalidResult.errors.length}`);
    if (invalidResult.errors.length > 0) {
    // eslint-disable-next-line no-console
      console.log(`Error: ${invalidResult.errors[0].message}`);
    }
    // eslint-disable-next-line no-console
    console.log();
  }

  /**
   * Demonstrates guard protection in action
   */
  public demonstrateGuardProtection(): void {
    // eslint-disable-next-line no-console
    console.log('=== Guard Protection Demo ===\n');

    // Create AST structure
    const program = new DemoASTNode(ASTNodeType.PROGRAM);
    const functionDecl = new DemoASTNode(ASTNodeType.FUNCTION_DECLARATION, 'myFunction');
    const block = new DemoASTNode(ASTNodeType.BLOCK);

    program.addChild(functionDecl);
    functionDecl.addChild(block);

    // eslint-disable-next-line no-console
    console.log('Initial AST structure:');
    // eslint-disable-next-line no-console
    console.log(program.toTreeString());

    // Create guarded node
    const guardedBlock = this.guard.createGuardedNode(block as any);

    // Test 1: Safe operation through guard
    // eslint-disable-next-line no-console
    console.log('Test 1: Adding statement through guard (should succeed)');
    const statement = new DemoASTNode(ASTNodeType.STATEMENT, 'return 42;');
    const success1 = guardedBlock.addChild(statement as any);
    // eslint-disable-next-line no-console
    console.log(`Result: ${success1 ? 'SUCCESS' : 'BLOCKED'}`);
    // eslint-disable-next-line no-console
    console.log('Updated structure:');
    // eslint-disable-next-line no-console
    console.log(program.toTreeString());

    // Test 2: Unsafe operation through guard
    // eslint-disable-next-line no-console
    console.log('Test 2: Adding invalid child through guard (should fail)');
    const invalidChild = new DemoASTNode(ASTNodeType.PROGRAM); // Program can't be child of block
    const success2 = guardedBlock.addChild(invalidChild as any);
    // eslint-disable-next-line no-console
    console.log(`Result: ${success2 ? 'SUCCESS' : 'BLOCKED'}`);
    // eslint-disable-next-line no-console
    console.log('Structure unchanged:');
    // eslint-disable-next-line no-console
    console.log(program.toTreeString());
  }

  /**
   * Demonstrates AI-driven safe manipulations
   */
  public async demonstrateAIIntegration(): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('=== AI Integration Demo ===\n');

    // Create a function with parameters
    const functionDecl = new DemoASTNode(ASTNodeType.FUNCTION_DECLARATION, 'calculateSum');
    const parameterList = new DemoASTNode(ASTNodeType.BLOCK); // Simplified parameter list
    const functionBody = new DemoASTNode(ASTNodeType.BLOCK);

    functionDecl.addChild(parameterList);
    functionDecl.addChild(functionBody);

    // eslint-disable-next-line no-console
    console.log('Initial function structure:');
    // eslint-disable-next-line no-console
    console.log(functionDecl.toTreeString());

    // Test 1: AI request to add a parameter
    // eslint-disable-next-line no-console
    console.log('Test 1: AI request to add function body statements');
    const request1: AIManipulationRequest = {
      description: 'Add a variable declaration and return statement to calculate sum',
      targetNode: functionBody as any,
      constraints: ['must be syntactically valid', 'preserve function structure'],
    };

    try {
      const result1 = await this.safeAgent.performSafeManipulation(request1);
    // eslint-disable-next-line no-console
      console.log(`AI Result: ${result1.success ? 'SUCCESS' : 'FAILED'}`);
    // eslint-disable-next-line no-console
      console.log(`Confidence: ${result1.confidence}`);
    // eslint-disable-next-line no-console
      console.log(`Reasoning: ${result1.reasoning}`);
      if (result1.errors && result1.errors.length > 0) {
    // eslint-disable-next-line no-console
        console.log(`Errors: ${result1.errors.join(', ')}`);
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      // eslint-disable-next-line max-len
      console.log(`AI Error: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`);
    }

    // Test 2: AI request for invalid operation
    // eslint-disable-next-line no-console
    console.log('\nTest 2: AI request for invalid operation');
    const literal = new DemoASTNode(ASTNodeType.LITERAL, 'hello');
    const request2: AIManipulationRequest = {
      description: 'Add a function declaration inside this string literal',
      targetNode: literal as any,
    };

    try {
      const result2 = await this.safeAgent.performSafeManipulation(request2);
    // eslint-disable-next-line no-console
      console.log(`AI Result: ${result2.success ? 'SUCCESS' : 'FAILED'}`);
    // eslint-disable-next-line no-console
      console.log(`Reasoning: ${result2.reasoning}`);
      if (result2.errors && result2.errors.length > 0) {
    // eslint-disable-next-line no-console
        console.log(`Errors: ${result2.errors.join(', ')}`);
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      // eslint-disable-next-line max-len
      console.log(`AI Error: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`);
    }
    // eslint-disable-next-line no-console
    console.log();
  }

  /**
   * Demonstrates batch validation and complex scenarios
   */
  public demonstrateAdvancedFeatures(): void {
    // eslint-disable-next-line no-console
    console.log('=== Advanced Features Demo ===\n');

    // Create complex AST structure
    const program = new DemoASTNode(ASTNodeType.PROGRAM);
    const classDecl = new DemoASTNode(ASTNodeType.CLASS_DECLARATION, 'Calculator');
    const methodDecl = new DemoASTNode(ASTNodeType.FUNCTION_DECLARATION, 'add');
    const methodBody = new DemoASTNode(ASTNodeType.BLOCK);

    program.addChild(classDecl);
    classDecl.addChild(methodDecl);
    methodDecl.addChild(methodBody);

    // eslint-disable-next-line no-console
    console.log('Complex AST structure:');
    // eslint-disable-next-line no-console
    console.log(program.toTreeString());

    // Test batch validation
    // eslint-disable-next-line no-console
    console.log('Test: Batch validation of multiple manipulations');
    const manipulations: ASTManipulation[] = [
      {
        type: ManipulationType.INSERT_CHILD,
        targetNode: methodBody as any,
        newNode: new DemoASTNode(ASTNodeType.VARIABLE_DECLARATION, 'result') as any,
      },
      {
        type: ManipulationType.INSERT_CHILD,
        targetNode: methodBody as any,
        newNode: new DemoASTNode(ASTNodeType.RETURN_STATEMENT) as any,
      },
      {
        type: ManipulationType.INSERT_CHILD,
        targetNode: program as any, // Invalid: can't add statement directly to program
        newNode: new DemoASTNode(ASTNodeType.STATEMENT) as any,
      },
    ];

    const batchResult = this.guard.guardBatchManipulations(manipulations);
    // eslint-disable-next-line no-console
    console.log('Batch validation result:');
    // eslint-disable-next-line no-console
    console.log(`  Total manipulations: ${batchResult.totalCount}`);
    // eslint-disable-next-line no-console
    console.log(`  Allowed: ${batchResult.allowedCount}`);
    // eslint-disable-next-line no-console
    console.log(`  Blocked: ${batchResult.blockedCount}`);
    // eslint-disable-next-line no-console
    console.log(`  Overall valid: ${batchResult.overallValid}`);

    // Show violation statistics
    const stats = this.guard.getViolationStats();
    // eslint-disable-next-line no-console
    console.log('\nGuard statistics:');
    // eslint-disable-next-line no-console
    console.log(`  Total violations: ${stats.totalViolations}`);
    // eslint-disable-next-line no-console
    console.log(`  Error count: ${stats.errorCount}`);
    // eslint-disable-next-line no-console
    console.log(`  Warning count: ${stats.warningCount}`);
    // eslint-disable-next-line no-console
    console.log();
  }

  /**
   * Demonstrates type safety and semantic validation
   */
  public demonstrateTypeSafety(): void {
    // eslint-disable-next-line no-console
    console.log('=== Type Safety Demo ===\n');

    // Create expression with type constraints
    const assignment = new DemoASTNode(ASTNodeType.ASSIGNMENT);
    const identifier = new DemoASTNode(ASTNodeType.IDENTIFIER, 'count');
    const literal = new DemoASTNode(ASTNodeType.LITERAL, 42);

    assignment.addChild(identifier);
    assignment.addChild(literal);

    // eslint-disable-next-line no-console
    console.log('Assignment expression:');
    // eslint-disable-next-line no-console
    console.log(assignment.toTreeString());

    // Test 1: Valid type assignment
    // eslint-disable-next-line no-console
    console.log('Test 1: Assigning number to number variable (VALID)');
    const validTypeManipulation: ASTManipulation = {
      type: ManipulationType.REPLACE_NODE,
      targetNode: literal as any,
      newNode: new DemoASTNode(ASTNodeType.LITERAL, 100) as any,
    };

    const typeResult1 = this.validator.validateManipulation(validTypeManipulation);
    // eslint-disable-next-line no-console
    console.log(`Result: ${typeResult1.isValid ? 'ALLOWED' : 'BLOCKED'}`);

    // Test 2: Invalid type assignment (conceptual - would need full type system)
    // eslint-disable-next-line no-console
    console.log('\nTest 2: Type mismatch detection (conceptual)');
    const stringLiteral = new DemoASTNode(ASTNodeType.LITERAL, '"hello"');
    const invalidTypeManipulation: ASTManipulation = {
      type: ManipulationType.REPLACE_NODE,
      targetNode: literal as any,
      newNode: stringLiteral as any,
    };

    const typeResult2 = this.validator.validateManipulation(invalidTypeManipulation);
    // eslint-disable-next-line no-console
    console.log(`Result: ${typeResult2.isValid ? 'ALLOWED' : 'BLOCKED'}`);
    if (typeResult2.warnings.length > 0) {
    // eslint-disable-next-line no-console
      console.log(`Warnings: ${typeResult2.warnings.map(w => w.message).join(', ')}`);
    }
    // eslint-disable-next-line no-console
    console.log();
  }

  /**
   * Runs the complete demonstration
   */
  public async runCompleteDemo(): Promise<void> {
    // eslint-disable-next-line no-console
    console.log('🚀 Minotaur Syntactic Validation System Demo\n');
    // eslint-disable-next-line no-console
    console.log('This demonstration shows how Minotaur restricts AST manipulations');
    // eslint-disable-next-line no-console
    console.log('to syntactically correct operations through AI integration.\n');

    this.demonstrateBasicValidation();
    this.demonstrateGuardProtection();
    await this.demonstrateAIIntegration();
    this.demonstrateAdvancedFeatures();
    this.demonstrateTypeSafety();

    // eslint-disable-next-line no-console
    console.log('✅ Demo completed successfully!');
    // eslint-disable-next-line no-console
    console.log('\nKey capabilities demonstrated:');
    // eslint-disable-next-line no-console
    console.log('• Syntactic validation of AST manipulations');
    // eslint-disable-next-line no-console
    console.log('• Guard protection against invalid operations');
    // eslint-disable-next-line no-console
    console.log('• AI-driven safe manipulations with validation');
    // eslint-disable-next-line no-console
    console.log('• Batch validation and error reporting');
    // eslint-disable-next-line no-console
    console.log('• Type safety and semantic consistency');
    // eslint-disable-next-line no-console
    console.log('• Real-time violation monitoring and statistics');
  }
}

// Run the demo if this file is executed directly
if (require.main === module) {
  const demo = new SyntacticValidationDemo();
    // eslint-disable-next-line no-console
  demo.runCompleteDemo().catch(console.error);
}

