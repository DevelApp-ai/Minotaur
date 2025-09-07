import { GrammarDrivenASTMapper } from '../evaluation/GrammarDrivenASTMapper';
import { StructuredValidationError, ErrorType, ErrorSeverity } from '../evaluation/StructuredValidationError';
import { SemanticValidator } from '../evaluation/SemanticValidator';
import { Grammar } from '../core/grammar/Grammar';
import { ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';

describe('GrammarDrivenASTMapper', () => {
  test('should generate transformation candidates for NAME_ERROR', async () => {
    // Create components
    const grammar = new Grammar('python');
    const validator = new SemanticValidator();
    const astMapper = new GrammarDrivenASTMapper(grammar, validator);

    // Create the exact same error as in the integration test
    const error: StructuredValidationError = {
      id: 'test_error_1',
      type: ErrorType.NAME_ERROR,
      severity: ErrorSeverity.HIGH,
      message: "name 'undefined_var' is not defined",
      originalMessage: "NameError: name 'undefined_var' is not defined",
      location: {
        line: 1,
        column: 6,
      },
      context: {
        sourceCode: 'print(undefined_var)',
        errorLine: 'print(undefined_var)',
        surroundingLines: ['1: print(undefined_var)'],
        variablesInScope: [],
      },
      suggestedFixes: [],
      timestamp: new Date(),
    };

    // Create minimal ASTContext with proper nodeType
    const astContext = {
      sourceCode: 'print(undefined_var)',
      ast: {
        type: 'Module',
        nodeType: ASTNodeType.PROGRAM,
        getParent: () => null,
        getChildren: () => [],
        getText: () => 'print(undefined_var)',
        getType: () => 'Module',
      } as any,
      errorNode: {
        type: 'Name',
        nodeType: ASTNodeType.IDENTIFIER,
        getParent: () => ({
          nodeType: ASTNodeType.PROGRAM,
          getParent: () => null,
          getChildren: () => [],
          getText: () => 'print(undefined_var)',
          getType: () => 'Module',
        }),
        getChildren: () => [],
        getText: () => 'undefined_var',
        getType: () => 'Name',
      } as any,
      scopeStack: [],
      typeEnvironment: {},
      controlFlowState: {},
      grammarProductions: [],
    };

    // Test the mapErrorToTransformation method
    const transformations = await astMapper.mapErrorToTransformation(error, astContext);
    
    // Verify transformations are generated
    expect(transformations).toBeDefined();
    expect(transformations.length).toBeGreaterThan(0);
    
    // Verify transformation structure
    const firstTransformation = transformations[0];
    expect(firstTransformation).toHaveProperty('id');
    expect(firstTransformation).toHaveProperty('type');
    expect(firstTransformation).toHaveProperty('description');
    expect(firstTransformation).toHaveProperty('confidence');
    expect(firstTransformation.description).toContain('undefined_var');
  });
});

