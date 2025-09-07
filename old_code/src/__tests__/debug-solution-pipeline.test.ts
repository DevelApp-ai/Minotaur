import { MultiSolutionGenerator } from '../evaluation/MultiSolutionGenerator';
import { MistralAPIClient } from '../evaluation/MistralAPIClient';
import { SemanticValidator } from '../evaluation/SemanticValidator';
import { GrammarDrivenASTMapper } from '../evaluation/GrammarDrivenASTMapper';
import { StructuredValidationError, ErrorType, ErrorSeverity } from '../evaluation/StructuredValidationError';
import { Grammar } from '../core/grammar/Grammar';
import { ASTTransformationEngine } from '../evaluation/ASTTransformationEngine';
import { ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';

describe('Debug Solution Pipeline', () => {
  test('should debug why solutions are not generated', async () => {
    console.log('=== STARTING DEBUG TEST ===');
    
    // Create minimal mock
    const mockMistralClient = {
      generateCompletion: jest.fn().mockResolvedValue({
        choices: [{
          message: {
            content: JSON.stringify({
              solutions: [{
                type: 'VARIABLE_DECLARATION',
                code: 'undefined_var = 42',
                reasoning: 'Add variable declaration',
                confidence: 0.9,
              }],
            }),
          },
        }],
        usage: { total_tokens: 100 },
      }),
    } as any;

    console.log('=== MOCK CREATED ===');

    // Create minimal components
    const grammar = new Grammar('python');
    const validator = new SemanticValidator();
    const astMapper = new GrammarDrivenASTMapper(grammar, validator);
    const transformationEngine = new ASTTransformationEngine();
    
    console.log('=== COMPONENTS CREATED ===');

    // Create generator with all required parameters
    const generator = new MultiSolutionGenerator(
      grammar,
      validator,
      astMapper,
      transformationEngine,
      mockMistralClient,
      {
        maxSolutionsPerError: 5,
        confidenceThreshold: 0.1, // Very low threshold
        includeAlternativeApproaches: true, // CRITICAL: Enable LLM generation
        includeRefactoringSolutions: false, // Disable to isolate issue
        enableContextualSolutions: false, // Disable to isolate issue
        validateAllSolutions: false, // Disable validation to isolate issue
        rankSolutions: false, // Disable ranking to isolate issue
      },
    );

    console.log('=== GENERATOR CREATED ===');

    // Create proper StructuredValidationError
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

    // Create minimal ASTContext (we'll use minimal mock objects)
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

    console.log('=== ERROR AND CONTEXT CREATED ===');
    console.log('Error:', JSON.stringify(error, null, 2));

    // Generate solutions
    console.log('=== CALLING GENERATE SOLUTIONS ===');
    let result;
    try {
      result = await generator.generateSolutions(error, astContext);
    } catch (e) {
      console.log('=== EXCEPTION CAUGHT ===');
      console.log('Exception:', e);
      throw e;
    }
    
    console.log('=== RESULT RECEIVED ===');
    console.log('Result:', JSON.stringify(result, null, 2));
    console.log('Mock was called:', mockMistralClient.generateCompletion.mock.calls.length);
    console.log('Mock call args:', mockMistralClient.generateCompletion.mock.calls);

    // Let's inspect the internal solutions array to see what's being filtered
    console.log('=== DETAILED SOLUTION ANALYSIS ===');
    console.log('Total solutions in result:', result.solutions.length);
    console.log('Solutions array:', result.solutions);
    
    // Check if there are any solutions with details
    if (result.solutions && result.solutions.length > 0) {
      result.solutions.forEach((solution, index) => {
        console.log(`Solution ${index}:`, {
          id: solution.id,
          confidence: solution.confidence,
          type: solution.type,
          description: solution.description,
        });
      });
    } else {
      console.log('No solutions in result.solutions array');
    }

    console.log('=== DEBUGGING COMPLETE ===');
    console.log('Total solutions generated:', result.totalSolutionsGenerated);
    console.log('Ranked solutions length:', result.solutions.length);

    console.log('=== DEBUG TEST COMPLETE ===');
    
    // Don't fail the test, just log everything for debugging
  });
});

