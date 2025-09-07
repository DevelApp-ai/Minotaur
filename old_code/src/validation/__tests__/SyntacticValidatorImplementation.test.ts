/**
 * Tests for SyntacticValidator implementations
 */

import { SyntacticValidator, ASTManipulation, ManipulationType, ValidationResult } from '../SyntacticValidator';
import { ZeroCopyASTNode, ASTNodeType } from '../../zerocopy/ast/ZeroCopyASTNode';
import { Grammar } from '../../core/grammar/Grammar';

describe('SyntacticValidator', () => {
  let validator: SyntacticValidator;
  let mockGrammar: jest.Mocked<Grammar>;

  // Helper function to create properly mocked AST nodes
  const createMockNode = (nodeType: ASTNodeType, value?: any, children: any[] = [], nodeId: number = Date.now()) => ({
    nodeType,
    nodeId,
    value,
    children,
    childCount: children.length,
    getChildren: () => children,
    getChild: (index: number) => children[index] || null,
  });

  beforeEach(() => {
    // Create a mock grammar
    mockGrammar = {
      // Add minimal mock methods as needed
    } as jest.Mocked<Grammar>;

    validator = new SyntacticValidator(mockGrammar);
  });

  describe('helper method implementations', () => {
    describe('canParentAcceptChild', () => {
      it('should validate valid parent-child relationships', () => {
        // Access private method for testing
        const canAccept = (validator as any).canParentAcceptChild(
          ASTNodeType.PROGRAM,
          ASTNodeType.FUNCTION_DECLARATION,
        );
        expect(canAccept).toBe(true);
      });

      it('should reject invalid parent-child relationships', () => {
        const canAccept = (validator as any).canParentAcceptChild(
          ASTNodeType.LITERAL,
          ASTNodeType.FUNCTION_DECLARATION,
        );
        expect(canAccept).toBe(false);
      });
    });

    describe('getMaxChildrenForNodeType', () => {
      it('should return correct max children for binary operations', () => {
        const maxChildren = (validator as any).getMaxChildrenForNodeType(ASTNodeType.BINARY_OP);
        expect(maxChildren).toBe(2);
      });

      it('should return unlimited children for blocks', () => {
        const maxChildren = (validator as any).getMaxChildrenForNodeType(ASTNodeType.BLOCK);
        expect(maxChildren).toBe(-1);
      });
    });

    describe('getMinChildrenForNodeType', () => {
      it('should return correct min children for binary operations', () => {
        const minChildren = (validator as any).getMinChildrenForNodeType(ASTNodeType.BINARY_OP);
        expect(minChildren).toBe(2);
      });

      it('should return correct min children for assignments', () => {
        const minChildren = (validator as any).getMinChildrenForNodeType(ASTNodeType.ASSIGNMENT);
        expect(minChildren).toBe(2);
      });
    });

    describe('isValueValidForNodeType', () => {
      it('should validate identifier names', () => {
        const isValid = (validator as any).isValueValidForNodeType(ASTNodeType.IDENTIFIER, 'validName');
        expect(isValid).toBe(true);

        const isInvalid = (validator as any).isValueValidForNodeType(ASTNodeType.IDENTIFIER, '123invalid');
        expect(isInvalid).toBe(false);
      });

      it('should validate operators in binary operations', () => {
        const isValid = (validator as any).isValueValidForNodeType(ASTNodeType.BINARY_OP, '+');
        expect(isValid).toBe(true);

        const isInvalid = (validator as any).isValueValidForNodeType(ASTNodeType.BINARY_OP, 'invalidOp');
        expect(isInvalid).toBe(false);
      });

      it('should validate literal values', () => {
        const isValid = (validator as any).isValueValidForNodeType(ASTNodeType.LITERAL, 'test value');
        expect(isValid).toBe(true);

        const isInvalid = (validator as any).isValueValidForNodeType(ASTNodeType.LITERAL, null);
        expect(isInvalid).toBe(false);
      });
    });

    describe('areTypesCompatible', () => {
      it('should consider same types compatible', () => {
        const compatible = (validator as any).areTypesCompatible('string', 'string');
        expect(compatible).toBe(true);
      });

      it('should consider compatible types compatible', () => {
        const compatible = (validator as any).areTypesCompatible('number', 'int');
        expect(compatible).toBe(true);
      });

      it('should consider incompatible types incompatible', () => {
        const compatible = (validator as any).areTypesCompatible('string', 'boolean');
        expect(compatible).toBe(false);
      });
    });

    describe('validateFunctionSignature', () => {
      it('should validate function with valid name', () => {
        const mockFuncNode = createMockNode(ASTNodeType.FUNCTION_DECLARATION, 'testFunction', [], 3);

        const isValid = (validator as any).validateFunctionSignature(mockFuncNode);
        expect(isValid).toBe(true);
      });

      it('should reject function without name', () => {
        const mockFuncNode = createMockNode(ASTNodeType.FUNCTION_DECLARATION, null, [], 4);

        const isValid = (validator as any).validateFunctionSignature(mockFuncNode);
        expect(isValid).toBe(false);
      });

      it('should validate function with parameter list', () => {
        const mockParam = createMockNode(ASTNodeType.IDENTIFIER, 'param1', [], 5);
        const mockFuncNode = createMockNode(ASTNodeType.FUNCTION_DECLARATION, 'testFunction', [mockParam], 6);

        const isValid = (validator as any).validateFunctionSignature(mockFuncNode);
        expect(isValid).toBe(true);
      });
    });

    describe('validateVariableNaming', () => {
      it('should validate correct variable names', () => {
        const mockVarNode = {
          nodeType: ASTNodeType.VARIABLE_DECLARATION,
          value: 'validName',
        } as any;

        const isValid = (validator as any).validateVariableNaming(mockVarNode);
        expect(isValid).toBe(true);
      });

      it('should reject invalid variable names', () => {
        const mockVarNode = {
          nodeType: ASTNodeType.VARIABLE_DECLARATION,
          value: '123invalid',
        } as any;

        const isValid = (validator as any).validateVariableNaming(mockVarNode);
        expect(isValid).toBe(false);
      });
    });

    describe('type inference', () => {
      it('should infer correct type for number literals', () => {
        const mockNumberNode = {
          nodeType: ASTNodeType.LITERAL,
          value: 42,
        } as any;

        const type = (validator as any).inferNodeType(mockNumberNode);
        expect(type).toBe('number');
      });

      it('should infer correct type for string literals', () => {
        const mockStringNode = {
          nodeType: ASTNodeType.LITERAL,
          value: 'hello',
        } as any;

        const type = (validator as any).inferNodeType(mockStringNode);
        expect(type).toBe('string');
      });

      it('should infer correct type for boolean literals', () => {
        const mockBoolNode = {
          nodeType: ASTNodeType.LITERAL,
          value: true,
        } as any;

        const type = (validator as any).inferNodeType(mockBoolNode);
        expect(type).toBe('boolean');
      });

      it('should infer number type for arithmetic operations', () => {
        const mockLeftNode = createMockNode(ASTNodeType.LITERAL, 5);
        const mockRightNode = createMockNode(ASTNodeType.LITERAL, 3);
        const mockBinaryOp = createMockNode(ASTNodeType.BINARY_OP, '+', [mockLeftNode, mockRightNode]);

        const type = (validator as any).inferNodeType(mockBinaryOp);
        expect(type).toBe('number');
      });

      it('should infer string type for string concatenation', () => {
        const mockLeftNode = createMockNode(ASTNodeType.LITERAL, 'hello');
        const mockRightNode = createMockNode(ASTNodeType.LITERAL, 'world');
        const mockBinaryOp = createMockNode(ASTNodeType.BINARY_OP, '+', [mockLeftNode, mockRightNode]);

        const type = (validator as any).inferNodeType(mockBinaryOp);
        expect(type).toBe('string');
      });

      it('should infer boolean type for comparison operations', () => {
        const mockLeftNode = createMockNode(ASTNodeType.LITERAL, 5);
        const mockRightNode = createMockNode(ASTNodeType.LITERAL, 3);
        const mockBinaryOp = createMockNode(ASTNodeType.BINARY_OP, '>', [mockLeftNode, mockRightNode]);

        const type = (validator as any).inferNodeType(mockBinaryOp);
        expect(type).toBe('boolean');
      });
    });

    describe('validateInitializationConsistency', () => {
      it('should require initialization for const declarations', () => {
        const mockConstNode = createMockNode(ASTNodeType.VARIABLE_DECLARATION, 'const x', []);

        const isValid = (validator as any).validateInitializationConsistency(mockConstNode);
        expect(isValid).toBe(false);
      });

      it('should allow uninitialized let/var declarations', () => {
        const mockLetNode = createMockNode(ASTNodeType.VARIABLE_DECLARATION, 'let x', []);

        const isValid = (validator as any).validateInitializationConsistency(mockLetNode);
        expect(isValid).toBe(true);
      });

      it('should allow initialized const declarations', () => {
        const mockInitializer = createMockNode(ASTNodeType.ASSIGNMENT);
        const mockConstNode = createMockNode(ASTNodeType.VARIABLE_DECLARATION, 'const x', [mockInitializer]);

        const isValid = (validator as any).validateInitializationConsistency(mockConstNode);
        expect(isValid).toBe(true);
      });
    });

    describe('findNodeDependencies', () => {
      it('should find identifier dependencies', () => {
        const mockNode = createMockNode(ASTNodeType.IDENTIFIER, 'myVariable', [], 1);

        const dependencies = (validator as any).findNodeDependencies(mockNode);
        expect(dependencies).toHaveLength(1);
        expect(dependencies[0].description).toContain('myVariable');
      });

      it('should find function call dependencies', () => {
        const mockNode = createMockNode(ASTNodeType.FUNCTION_CALL, 'myFunction', [], 2);

        const dependencies = (validator as any).findNodeDependencies(mockNode);
        expect(dependencies).toHaveLength(1);
        expect(dependencies[0].description).toContain('myFunction');
      });

      it('should handle circular references without stack overflow', () => {
        // Create nodes with circular references
        const mockChild = createMockNode(ASTNodeType.IDENTIFIER, 'circularRef', [], 11);
        const mockParent = createMockNode(ASTNodeType.BLOCK, null, [mockChild], 10);

        // Create circular reference by having child point back to parent
        mockChild.getChildren = () => [mockParent];

        // This should not cause a stack overflow
        const dependencies = (validator as any).findNodeDependencies(mockParent);

        // Should find the identifier dependency without infinite recursion
        expect(dependencies).toHaveLength(1);
        expect(dependencies[0].description).toContain('circularRef');
      });
    });
  });

  describe('integration validation', () => {
    it('should provide basic validation for simple manipulations', () => {
      // Mock nodes for testing
      const mockTargetNode = {
        nodeType: ASTNodeType.BLOCK,
        value: null,
        children: [],
      } as any;

      const mockNewNode = {
        nodeType: ASTNodeType.STATEMENT,
    // eslint-disable-next-line no-console
        value: 'console.log("test");',
        children: [],
      } as any;

      const manipulation: ASTManipulation = {
        type: ManipulationType.INSERT_CHILD,
        targetNode: mockTargetNode,
        newNode: mockNewNode,
      };

      const result: ValidationResult = validator.validateManipulation(manipulation);

      // The validation should complete without throwing errors
      expect(result).toBeDefined();
      expect(result.isValid).toBeDefined();
      expect(result.errors).toBeDefined();
      expect(result.warnings).toBeDefined();
      expect(result.suggestions).toBeDefined();
    });
  });
});