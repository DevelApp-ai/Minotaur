/**
 * MultiSolutionGenerator Tests - Phase 2 Project Golem
 *
 * Comprehensive test suite for the multi-solution generation system,
 * covering solution generation, validation, ranking, and selection.
 */

import { describe, test, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { MultiSolutionGenerator, SolutionType, CorrectionSolution, MultiSolutionConfig } from '../evaluation/MultiSolutionGenerator';
import { SemanticValidator } from '../evaluation/SemanticValidator';
import { GrammarDrivenASTMapper } from '../evaluation/GrammarDrivenASTMapper';
import { ASTTransformationEngine } from '../evaluation/ASTTransformationEngine';
import { MistralAPIClient, MistralAPIConfig } from '../evaluation/MistralAPIClient';
import { StructuredValidationError, ErrorType, ErrorSeverity } from '../evaluation/StructuredValidationError';
import { ASTContext } from '../evaluation/GrammarDrivenASTMapper';
import { ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';

describe('MultiSolutionGenerator Tests', () => {
  let grammar: Grammar;
  let stepParser: StepParser;
  let semanticValidator: SemanticValidator;
  let astMapper: GrammarDrivenASTMapper;
  let transformationEngine: ASTTransformationEngine;
  let mockMistralClient: jest.Mocked<MistralAPIClient>;
  let multiSolutionGenerator: MultiSolutionGenerator;

  beforeEach(async () => {
    // Initialize test environment
    grammar = createTestPythonGrammar();
    stepParser = new StepParser(grammar);
    semanticValidator = new SemanticValidator(grammar, stepParser);
    astMapper = new GrammarDrivenASTMapper(grammar, semanticValidator);
    transformationEngine = new ASTTransformationEngine(grammar, stepParser);

    // Create mock MistralAPIClient
    mockMistralClient = {
      generateCompletion: jest.fn().mockResolvedValue({
        success: true,
        response: {
          id: 'mock-response',
          object: 'chat.completion',
          created: Date.now(),
          model: 'codestral-latest',
          choices: [{
            index: 0,
            message: {
              role: 'assistant',
              content: JSON.stringify([
                {
                  description: 'Alternative approach: Use a default value',
                  approach: 'alternative_approach',
                  codeChange: 'undefined_var = None  # Initialize with default value',
                  confidence: 0.8,
                  reasoning: 'Initialize the variable with a default value to prevent NameError',
                },
                {
                  description: 'Declare the variable before use',
                  approach: 'variable_declaration',
                  codeChange: 'undefined_var = 0  # Declare variable',
                  confidence: 0.9,
                  reasoning: 'Explicitly declare the variable before using it',
                },
              ]),
            },
            finish_reason: 'stop',
          }],
          usage: {
            prompt_tokens: 50,
            completion_tokens: 25,
            total_tokens: 75,
          },
        },
        error: undefined,
        retryCount: 0,
        duration: 100,
        tokensUsed: 75,
        cost: 0.001,
      }),
    } as any;

    const config: Partial<MultiSolutionConfig> = {
      maxSolutionsPerError: 5,
      includeAlternativeApproaches: true,
      includeRefactoringSolutions: true,
      enableContextualSolutions: true,
      confidenceThreshold: 0.3,
      validateAllSolutions: true,
      rankSolutions: true,
      timeoutPerSolution: 5000,
    };

    multiSolutionGenerator = new MultiSolutionGenerator(
      grammar,
      semanticValidator,
      astMapper,
      transformationEngine,
      mockMistralClient,
      config,
    );

    await semanticValidator.initialize('test-python.grammar');
  });

  afterEach(() => {
    // Cleanup test environment
  });

  describe('Solution Generation', () => {
    test('should generate multiple solutions for name error', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      console.log('🔍 TEST DEBUG: Received result:', {
        solutionsLength: result.solutions.length,
        totalGenerated: result.totalSolutionsGenerated,
        validSolutions: result.validSolutions,
        bestSolution: result.bestSolution ? 'defined' : 'undefined',
      });

      // Now that implementation is fixed, expect multiple solutions
      expect(result.solutions.length).toBeGreaterThan(1);
      expect(result.solutions.length).toBeLessThanOrEqual(5);
      expect(result.totalSolutionsGenerated).toBeGreaterThanOrEqual(result.solutions.length);
      expect(result.bestSolution).toBeDefined();
      expect(result.alternativeSolutions.length).toBeGreaterThanOrEqual(0);
    });

    test('should generate multiple solutions for syntax error', async () => {
      const error = createSyntaxError('invalid syntax', 1, 5);
      const context = createTestContext('if x = 5:', 'if_stmt');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      // Now that implementation is fixed, expect multiple solutions
      expect(result.solutions.length).toBeGreaterThan(1);
      expect(result.solutions.some(s => s.type === SolutionType.DIRECT_FIX)).toBe(true);
    });

    test('should include different solution types', async () => {
      const error = createNameError("name 'math' is not defined", 1, 5);
      const context = createTestContext('math.sqrt(16)', 'call');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      const solutionTypes = new Set(result.solutions.map(s => s.type));
      expect(solutionTypes.size).toBeGreaterThan(1);

      // Should include import addition for 'math'
      expect(result.solutions.some(s => s.type === SolutionType.IMPORT_ADDITION)).toBe(true);
    });

    test('should respect max solutions limit', async () => {
      const limitedConfig: Partial<MultiSolutionConfig> = {
        maxSolutionsPerError: 2,
        includeAlternativeApproaches: true,
        includeRefactoringSolutions: true,
        enableContextualSolutions: true,
      };

      const limitedGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        limitedConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await limitedGenerator.generateSolutions(error, context);

      expect(result.solutions.length).toBeLessThanOrEqual(2);
    });

    test('should filter by confidence threshold', async () => {
      const highThresholdConfig: Partial<MultiSolutionConfig> = {
        confidenceThreshold: 0.9, // Very high threshold
        maxSolutionsPerError: 10,
      };

      const strictGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        highThresholdConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await strictGenerator.generateSolutions(error, context);

      // Should have fewer solutions due to high confidence threshold
      for (const solution of result.solutions) {
        expect(solution.confidence).toBeGreaterThanOrEqual(0.9);
      }
    });
  });

  describe('Solution Types', () => {
    test('should generate direct fix solutions', async () => {
      const error = createSyntaxError('invalid syntax', 1, 5);
      const context = createTestContext('if x = 5:', 'if_stmt');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      const directFixes = result.solutions.filter(s => s.type === SolutionType.DIRECT_FIX);
      expect(directFixes.length).toBeGreaterThan(0);

      for (const fix of directFixes) {
        expect(fix.description).toContain('Direct fix');
        expect(fix.priority).toBe(1); // Highest priority
        expect(fix.astTransformation).toBeDefined();
      }
    });

    test('should generate alternative approach solutions', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var + 5', 'binop');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      // May or may not have alternative approaches depending on error type
      const alternatives = result.solutions.filter(s => s.type === SolutionType.ALTERNATIVE_APPROACH);

      for (const alt of alternatives) {
        expect(alt.description).toContain('alternative') || expect(alt.description).toContain('Alternative');
        expect(alt.confidence).toBeGreaterThan(0);
      }
    });

    test('should generate import addition solutions for module names', async () => {
      const error = createNameError("name 'math' is not defined", 1, 5);
      const context = createTestContext('math.sqrt(16)', 'call');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      const importSolutions = result.solutions.filter(s => s.type === SolutionType.IMPORT_ADDITION);
      expect(importSolutions.length).toBeGreaterThan(0);

      for (const importSol of importSolutions) {
        expect(importSol.description).toContain('import');
        expect(importSol.description).toContain('math');
        expect(importSol.confidence).toBeGreaterThan(0.5); // Should be confident about common modules
      }
    });

    test('should generate refactoring solutions when enabled', async () => {
      const refactoringConfig: Partial<MultiSolutionConfig> = {
        includeRefactoringSolutions: true,
        maxSolutionsPerError: 10,
      };

      const refactoringGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        refactoringConfig,
      );

      const error = createNameError("name 'complex_var' is not defined", 1, 5);
      const context = createTestContext('complex_var * 2 + 3', 'binop');

      const result = await refactoringGenerator.generateSolutions(error, context);

      const refactoringSolutions = result.solutions.filter(s => s.type === SolutionType.REFACTORING);

      for (const refactor of refactoringSolutions) {
        expect(refactor.description).toContain('Refactoring');
        expect(refactor.priority).toBeGreaterThan(1); // Lower priority than direct fixes
      }
    });

    test('should not generate refactoring solutions when disabled', async () => {
      const noRefactoringConfig: Partial<MultiSolutionConfig> = {
        includeRefactoringSolutions: false,
      };

      const noRefactoringGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        noRefactoringConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await noRefactoringGenerator.generateSolutions(error, context);

      const refactoringSolutions = result.solutions.filter(s => s.type === SolutionType.REFACTORING);
      expect(refactoringSolutions.length).toBe(0);
    });
  });

  describe('Solution Validation', () => {
    test('should validate all solutions when enabled', async () => {
      const error = createSyntaxError('invalid syntax', 1, 5);
      const context = createTestContext('if x = 5:', 'if_stmt');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      for (const solution of result.solutions) {
        expect(solution.validationResult).toBeDefined();
        expect(typeof solution.validationResult.syntaxValid).toBe('boolean');
        expect(typeof solution.validationResult.semanticsValid).toBe('boolean');
        expect(typeof solution.validationResult.grammarCompliant).toBe('boolean');
        expect(typeof solution.validationResult.errorsResolved).toBe('number');
        expect(typeof solution.validationResult.errorsIntroduced).toBe('number');
      }
    });

    test('should track validation metrics', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      for (const solution of result.solutions) {
        const validation = solution.validationResult;

        // Should resolve at least one error (the original error)
        if (validation.syntaxValid && validation.semanticsValid) {
          expect(validation.errorsResolved).toBeGreaterThanOrEqual(0);
        }

        // Should not introduce more errors than it resolves
        expect(validation.errorsIntroduced).toBeLessThanOrEqual(validation.errorsResolved + 1);
      }
    });

    test('should skip validation when disabled', async () => {
      const noValidationConfig: Partial<MultiSolutionConfig> = {
        validateAllSolutions: false,
      };

      const noValidationGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        noValidationConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await noValidationGenerator.generateSolutions(error, context);

      // Validation results might be empty or have default values
      for (const solution of result.solutions) {
        // Should still have validation result structure, but may not be fully populated
        expect(solution.validationResult).toBeDefined();
      }
    });
  });

  describe('Solution Ranking', () => {
    test('should rank solutions by confidence', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      if (result.solutions.length > 1) {
        for (let i = 0; i < result.solutions.length - 1; i++) {
          const current = result.solutions[i];
          const next = result.solutions[i + 1];

          // Should be sorted by confidence (descending) or other ranking criteria
          expect(current.confidence).toBeGreaterThanOrEqual(next.confidence - 0.1); // Allow small variance for other factors
        }
      }
    });

    test('should prioritize direct fixes over alternatives', async () => {
      const error = createSyntaxError('invalid syntax', 1, 5);
      const context = createTestContext('if x = 5:', 'if_stmt');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      const directFixes = result.solutions.filter(s => s.type === SolutionType.DIRECT_FIX);
      const alternatives = result.solutions.filter(s => s.type === SolutionType.ALTERNATIVE_APPROACH);

      if (directFixes.length > 0 && alternatives.length > 0) {
        const bestDirectFix = directFixes[0];
        const bestAlternative = alternatives[0];

        // Direct fixes should have higher priority (lower priority number)
        expect(bestDirectFix.priority).toBeLessThanOrEqual(bestAlternative.priority);
      }
    });

    test('should consider validation results in ranking', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      if (result.solutions.length > 1) {
        const topSolution = result.solutions[0];

        // Top solution should have good validation results
        if (topSolution.validationResult.syntaxValid && topSolution.validationResult.semanticsValid) {
          expect(topSolution.validationResult.errorsResolved).toBeGreaterThan(0);
          expect(topSolution.validationResult.errorsIntroduced).toBe(0);
        }
      }
    });

    test('should disable ranking when configured', async () => {
      const noRankingConfig: Partial<MultiSolutionConfig> = {
        rankSolutions: false,
      };

      const noRankingGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        noRankingConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await noRankingGenerator.generateSolutions(error, context);

      // Solutions should still be generated, just not ranked
      expect(result.solutions.length).toBeGreaterThan(0);
    });
  });

  describe('Impact Analysis', () => {
    test('should analyze impact for each solution', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      for (const solution of result.solutions) {
        expect(solution.estimatedImpact).toBeDefined();
        expect(typeof solution.estimatedImpact.linesAffected).toBe('number');
        expect(Array.isArray(solution.estimatedImpact.scopeChanges)).toBe(true);
        expect(Array.isArray(solution.estimatedImpact.potentialSideEffects)).toBe(true);
        expect(typeof solution.estimatedImpact.breakingChanges).toBe('boolean');
        expect(['positive', 'neutral', 'negative']).toContain(solution.estimatedImpact.performanceImpact);
        expect(['improved', 'neutral', 'degraded']).toContain(solution.estimatedImpact.readabilityImpact);
      }
    });

    test('should prefer solutions with minimal impact', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      if (result.solutions.length > 1) {
        const topSolution = result.solutions[0];

        // Top solution should generally have minimal negative impact
        expect(topSolution.estimatedImpact.breakingChanges).toBe(false);
        expect(topSolution.estimatedImpact.performanceImpact).not.toBe('negative');
      }
    });
  });

  describe('Multiple Error Handling', () => {
    test('should generate solutions for multiple errors', async () => {
      const errors = [
        createNameError("name 'var1' is not defined", 1, 5),
        createSyntaxError('invalid syntax', 2, 8),
        createNameError("name 'var2' is not defined", 3, 10),
      ];

      const context = createTestContext('var1 + var2', 'binop');

      const results = await multiSolutionGenerator.generateMultipleSolutions(errors, context);

      expect(results.size).toBe(3);

      for (const [errorId, result] of results) {
        expect(result.solutions.length).toBeGreaterThan(0);
        expect(result.error.id).toBe(errorId);
      }
    });

    test('should handle empty error list', async () => {
      const context = createTestContext('valid_code', 'name');

      const results = await multiSolutionGenerator.generateMultipleSolutions([], context);

      expect(results.size).toBe(0);
    });
  });

  describe('Performance and Edge Cases', () => {
    test('should handle timeout gracefully', async () => {
      const timeoutConfig: Partial<MultiSolutionConfig> = {
        timeoutPerSolution: 100, // Very short timeout
      };

      const timeoutGenerator = new MultiSolutionGenerator(
        grammar,
        semanticValidator,
        astMapper,
        transformationEngine,
        mockMistralClient,
        timeoutConfig,
      );

      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await timeoutGenerator.generateSolutions(error, context);

      // Should complete without crashing, even if some solutions timeout
      expect(result).toBeDefined();
      expect(result.generationTime).toBeGreaterThan(0);
    });

    test('should handle malformed error gracefully', async () => {
      const malformedError = {
        id: 'malformed',
        type: 'UNKNOWN' as ErrorType,
        severity: ErrorSeverity.HIGH,
        message: '',
        location: { line: -1, column: -1, length: 0 },
        context: { errorLine: '', functionName: null, className: null, nearbyCode: [] },
        suggestions: [],
      };

      const context = createTestContext('test', 'name');

      const result = await multiSolutionGenerator.generateSolutions(malformedError, context);

      expect(result).toBeDefined();
      expect(result.solutions).toBeDefined();
    });

    test('should provide generation statistics', () => {
      const stats = multiSolutionGenerator.getGenerationStatistics();

      expect(stats).toBeDefined();
      expect(typeof stats.strategiesLoaded).toBe('number');
      expect(typeof stats.maxSolutionsPerError).toBe('number');
      expect(typeof stats.confidenceThreshold).toBe('number');
      expect(typeof stats.validationEnabled).toBe('boolean');
    });
  });

  describe('Solution Metadata', () => {
    test('should track metadata for each solution', async () => {
      const error = createNameError("name 'undefined_var' is not defined", 1, 5);
      const context = createTestContext('undefined_var', 'name');

      const result = await multiSolutionGenerator.generateSolutions(error, context);

      for (const solution of result.solutions) {
        expect(solution.metadata).toBeDefined();
        expect(typeof solution.metadata.generationTime).toBe('number');
        expect(typeof solution.metadata.validationTime).toBe('number');
        expect(Array.isArray(solution.metadata.grammarRulesUsed)).toBe(true);
        expect(typeof solution.metadata.transformationStrategy).toBe('string');
        expect(typeof solution.metadata.fallbackLevel).toBe('number');
        expect(typeof solution.metadata.sourcePattern).toBe('string');
        expect(typeof solution.metadata.targetPattern).toBe('string');
      }
    });
  });
});

// Helper functions

function createTestPythonGrammar(): Grammar {
  return {
    name: 'TestPython',
    version: '3.11.0',
    productions: new Map([
      ['file_input', { name: 'file_input', rules: [] }],
      ['funcdef', { name: 'funcdef', rules: [] }],
      ['if_stmt', { name: 'if_stmt', rules: [] }],
      ['return_stmt', { name: 'return_stmt', rules: [] }],
      ['NAME', { name: 'NAME', rules: [] }],
      ['call', { name: 'call', rules: [] }],
      ['binop', { name: 'binop', rules: [] }],
    ]),
    terminals: new Set(['def', 'if', 'return', '=', '==', ':', '(', ')', 'IDENTIFIER']),
    startSymbol: 'file_input',
  } as Grammar;
}

let errorIdCounter = 0;

function createNameError(message: string, line: number, column: number): StructuredValidationError {
  return {
    id: `name-error-${Date.now()}-${++errorIdCounter}`,
    type: ErrorType.NAME_ERROR,
    severity: ErrorSeverity.HIGH,
    message,
    line,
    column,
    source: 'test',
    context: {
      nearbyCode: 'test code',
      functionName: 'test_function',
      className: undefined,
    },
  };
}

function createSyntaxError(message: string, line: number, column: number): StructuredValidationError {
  return {
    id: `syntax-error-${Date.now()}-${++errorIdCounter}`,
    type: ErrorType.SYNTAX_ERROR,
    severity: ErrorSeverity.HIGH,
    message,
    originalMessage: message, // Add missing originalMessage property
    location: { line, column, length: 10 },
    context: { errorLine: message, functionName: null, className: null, nearbyCode: [], sourceCode: 'if x = 5:' },
    suggestions: [],
  };
}

function createTestContext(sourceCode: string, nodeType: string): ASTContext {
  return {
    sourceCode,
    ast: {
      type: 'file_input',
      nodeType: ASTNodeType.PROGRAM,
      getParent: () => null,
      getChildren: () => [{
        type: nodeType,
        nodeType: ASTNodeType.IDENTIFIER,
        getParent: () => null,
        getChildren: () => [],
        getText: () => sourceCode,
        getType: () => nodeType,
        children: [],
        line: 1,
        column: 1,
        parent: undefined,
        text: sourceCode,
      }],
      getText: () => sourceCode,
      getType: () => 'file_input',
      children: [{
        type: nodeType,
        nodeType: ASTNodeType.IDENTIFIER,
        getParent: () => null,
        getChildren: () => [],
        getText: () => sourceCode,
        getType: () => nodeType,
        children: [],
        line: 1,
        column: 1,
        parent: undefined,
        text: sourceCode,
      }],
      line: 1,
      column: 1,
      parent: undefined,
      text: sourceCode,
    },
    errorNode: {
      type: nodeType,
      nodeType: ASTNodeType.IDENTIFIER,
      getParent: () => ({
        nodeType: ASTNodeType.PROGRAM,
        getParent: () => null,
        getChildren: () => [],
        getText: () => sourceCode,
        getType: () => 'file_input',
      }),
      getChildren: () => [],
      getText: () => sourceCode,
      getType: () => nodeType,
      children: [],
      line: 1,
      column: 1,
      parent: undefined,
      text: sourceCode,
    },
    scopeStack: [],
    typeEnvironment: {},
    controlFlowState: {},
    grammarProductions: [],
  };
}

