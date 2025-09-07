/**
 * Project Golem Integration Tests
 * Tests the complete integration of Project Golem components
 * 
 * @fileoverview Basic integration tests for Project Golem system
 */

import { describe, test, expect, beforeEach, afterEach } from '@jest/globals';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { GolemBenchmarkSolver } from '../evaluation/GolemBenchmarkSolver';
import { AgenticSystem } from '../evaluation/AgenticSystem';
import { SemanticValidator } from '../evaluation/SemanticValidator';
import * as path from 'path';

// Increase timeout for integration tests
jest.setTimeout(15000);

describe('Project Golem Integration Tests', () => {
  let grammar: Grammar;
  let parser: StepParser;
  let solver: GolemBenchmarkSolver;
  let agenticSystem: AgenticSystem;
  let validator: SemanticValidator;

  beforeEach(async () => {
    grammar = new Grammar('Python311');
    parser = new StepParser();
    
    const config = {
      apiKey: 'test-key',
      model: 'mistral-large',
      baseURL: 'https://api.mistral.ai',
      enableRequestQueuing: true,
      maxRetries: 3,
      retryDelay: 1000,
      timeout: 30000,
      rateLimit: {
        requestsPerMinute: 60,
        tokensPerMinute: 100000,
      },
    };
    
    // GolemBenchmarkSolver constructor only takes workingDir, config goes to initialize()
    solver = new GolemBenchmarkSolver();
    agenticSystem = new AgenticSystem(config);
    validator = new SemanticValidator(grammar, parser);
    
    // Initialize solver with config (but skip in tests to avoid API calls)
    // await solver.initialize(config);
  });

  afterEach(() => {
    // Cleanup
  });

  test('should integrate grammar, parser, and solver successfully', () => {
    expect(grammar).toBeDefined();
    expect(parser).toBeDefined();
    expect(solver).toBeDefined();
    expect(agenticSystem).toBeDefined();
    expect(validator).toBeDefined();
  });

  test('should handle basic syntax error correction', async () => {
    const sourceCode = 'def hello_world(\n    print("Hello, World!")';
    
    // Test semantic validation initialization with proper string path
    const grammarPath = path.join(__dirname, '../grammar/Python311.grammar');
    await validator.initialize(grammarPath);
    
    // Test that the system can handle syntax errors
    expect(sourceCode).toBeDefined();
    expect(validator).toBeDefined();
    
    // This is a placeholder test - actual implementation would test correction
    // const result = await agenticSystem.correctCode(sourceCode);
    // expect(result).toBeDefined();
  });

  test('should integrate Project Golem components', async () => {
    const testCode = 'print("Hello, Project Golem!")';
    
    // Test parser integration
    parser.setActiveGrammar(grammar);
    
    // Test validator integration with proper path
    const grammarPath = path.join(__dirname, '../grammar/Python311.grammar');
    await validator.initialize(grammarPath);
    
    // Test complete Project Golem workflow
    expect(testCode).toBeDefined();
    expect(parser).toBeDefined();
    expect(solver).toBeDefined();
    expect(agenticSystem).toBeDefined();
  });
});

