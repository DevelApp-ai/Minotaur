/**
 * Minotaur Compiler-Compiler Test Suites
 *
 * Predefined test suites for comprehensive testing of the compiler-compiler export functionality.
 */

import { TestConfiguration, TestSuite, TestCase, TestGrammar, TestInput, ExpectedOutput, ValidationRule, ValidationResult, TestResult } from './CompilerTestFramework';

export class TestSuites {
  /**
     * Get comprehensive test configuration
     */
  public static getComprehensiveTests(): TestConfiguration {
    return {
      name: 'Minotaur Comprehensive Compiler-Compiler Tests',
      description: 'Complete testing suite for all compiler-compiler functionality',
      targetLanguages: ['c', 'c++', 'java', 'c#', 'python', 'javascript', 'rust', 'go', 'webassembly'],
      testSuites: [
        this.getUnitTestSuite(),
        this.getIntegrationTestSuite(),
        this.getSystemTestSuite(),
        this.getPerformanceTestSuite(),
        this.getStressTestSuite(),
      ],
      validationLevel: 'comprehensive',
      enablePerformanceTests: true,
      enableCompilationTests: true,
      enableRuntimeTests: true,
      enableCrossLanguageTests: true,
      outputDirectory: './test-output',
      timeoutMs: 300000, // 5 minutes
      maxMemoryMB: 2048,
    };
  }

  /**
     * Get quick test configuration for CI/CD
     */
  public static getQuickTests(): TestConfiguration {
    return {
      name: 'Minotaur Quick Tests',
      description: 'Fast test suite for continuous integration',
      targetLanguages: ['c', 'javascript', 'python'],
      testSuites: [
        this.getUnitTestSuite(),
        this.getBasicIntegrationTestSuite(),
      ],
      validationLevel: 'standard',
      enablePerformanceTests: false,
      enableCompilationTests: true,
      enableRuntimeTests: true,
      enableCrossLanguageTests: false,
      outputDirectory: './test-output-quick',
      timeoutMs: 60000, // 1 minute
      maxMemoryMB: 1024,
    };
  }

  /**
     * Get language-specific test configuration
     */
  public static getLanguageSpecificTests(language: string): TestConfiguration {
    return {
      name: `Minotaur ${language} Tests`,
      description: `Focused testing for ${language} code generation`,
      targetLanguages: [language],
      testSuites: [
        this.getUnitTestSuite(),
        this.getIntegrationTestSuite(),
        this.getLanguageSpecificTestSuite(language),
      ],
      validationLevel: 'comprehensive',
      enablePerformanceTests: true,
      enableCompilationTests: true,
      enableRuntimeTests: true,
      enableCrossLanguageTests: false,
      outputDirectory: `./test-output-${language}`,
      timeoutMs: 180000, // 3 minutes
      maxMemoryMB: 1536,
    };
  }

  /**
     * Unit test suite
     */
  private static getUnitTestSuite(): TestSuite {
    return {
      name: 'Unit Tests',
      description: 'Basic functionality tests for individual components',
      category: 'unit',
      tests: [
        {
          name: 'simple_grammar_compilation',
          description: 'Test compilation of simple arithmetic grammar',
          grammar: {
            name: 'SimpleArithmetic',
            source: `
grammar SimpleArithmetic;

expr: expr '+' term
    | expr '-' term
    | term
    ;

term: term '*' factor
    | term '/' factor
    | factor
    ;

factor: '(' expr ')'
      | NUMBER
      ;

NUMBER: [0-9]+;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'antlr4',
            features: ['arithmetic', 'precedence'],
            complexity: 'simple',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'simple_addition',
              content: '2 + 3',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 3,
              expectedNodes: 3,
            },
            {
              name: 'complex_expression',
              content: '(2 + 3) * 4',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 7,
              expectedNodes: 7,
            },
            {
              name: 'invalid_syntax',
              content: '2 + + 3',
              type: 'invalid',
              expectedResult: 'failure',
            },
          ],
          expectedOutputs: [
            {
              targetLanguage: 'c',
              compilationSuccess: true,
              runtimeSuccess: true,
              expectedFiles: ['parser.c', 'parser.h', 'lexer.c', 'lexer.h'],
              expectedFunctions: ['parse', 'lex', 'expr', 'term', 'factor'],
              expectedStructures: ['Token', 'ASTNode'],
              performanceThresholds: {
                maxCompileTimeMs: 1000,
                maxRuntimeMs: 100,
                maxMemoryMB: 10,
                minParseSpeed: 1000,
                maxCodeSizeKB: 50,
              },
            },
          ],
          validations: [
            {
              type: 'compilation',
              description: 'Generated code must compile without errors',
              validator: (result: TestResult) => ({
                rule: 'compilation',
                passed: result.compilation.success,
                message: result.compilation.success ? 'Compilation successful' : 'Compilation failed',
                severity: result.compilation.success ? 'info' : 'critical',
              }),
            },
            {
              type: 'syntax',
              description: 'Generated code must follow language syntax rules',
              validator: (result: TestResult) => ({
                rule: 'syntax',
                passed: result.compilation.compilerErrors.length === 0,
                message: `Syntax validation: ${result.compilation.compilerErrors.length} errors`,
                severity: result.compilation.compilerErrors.length === 0 ? 'info' : 'error',
              }),
            },
          ],
          tags: ['unit', 'basic', 'arithmetic'],
        },
        {
          name: 'json_grammar_compilation',
          description: 'Test compilation of JSON grammar',
          grammar: {
            name: 'JSON',
            source: `
grammar JSON;

json: value;

value: object
     | array
     | STRING
     | NUMBER
     | 'true'
     | 'false'
     | 'null'
     ;

object: '{' (pair (',' pair)*)? '}';
pair: STRING ':' value;

array: '[' (value (',' value)*)? ']';

STRING: '"' (~[\\r\\n"])* '"';
NUMBER: '-'? [0-9]+ ('.' [0-9]+)?;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'antlr4',
            features: ['json', 'nested_structures'],
            complexity: 'medium',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'simple_object',
              content: '{"name": "test", "value": 42}',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 9,
              expectedNodes: 7,
            },
            {
              name: 'nested_structure',
              content: '{"users": [{"id": 1, "name": "Alice"}, {"id": 2, "name": "Bob"}]}',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 25,
              expectedNodes: 19,
            },
            {
              name: 'invalid_json',
              content: '{"name": test}',
              type: 'invalid',
              expectedResult: 'failure',
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'compilation',
              description: 'JSON grammar must compile successfully',
              validator: (result: TestResult) => ({
                rule: 'compilation',
                passed: result.compilation.success,
                message: result.compilation.success ? 'JSON grammar compiled' : 'JSON grammar compilation failed',
                severity: result.compilation.success ? 'info' : 'critical',
              }),
            },
          ],
          tags: ['unit', 'json', 'nested'],
        },
      ],
    };
  }

  /**
     * Integration test suite
     */
  private static getIntegrationTestSuite(): TestSuite {
    return {
      name: 'Integration Tests',
      description: 'Test integration between components and inheritance features',
      category: 'integration',
      tests: [
        {
          name: 'inheritance_grammar_compilation',
          description: 'Test compilation of grammar with inheritance',
          grammar: {
            name: 'InheritedExpression',
            source: `
grammar InheritedExpression inherits BaseExpression;

// Extend primary to include variables and function calls
primary: NUMBER 
       | IDENTIFIER
       | function_call
       | '(' expression ')'
       ;

function_call: IDENTIFIER '(' argument_list? ')';
argument_list: expression (',' expression)*;

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
                        `,
            type: 'minotaur',
            features: ['inheritance', 'functions', 'variables'],
            complexity: 'medium',
            contextSensitive: false,
            inheritanceDepth: 1,
          },
          inputs: [
            {
              name: 'function_call',
              content: 'add(2, 3)',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 7,
              expectedNodes: 5,
            },
            {
              name: 'nested_calls',
              content: 'sqrt(add(2, 3))',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 9,
              expectedNodes: 7,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'compilation',
              description: 'Inherited grammar must compile with base features',
              validator: (result: TestResult) => ({
                rule: 'inheritance_compilation',
                passed: result.compilation.success,
                message: result.compilation.success ? 'Inheritance compilation successful' : 'Inheritance compilation failed',
                severity: result.compilation.success ? 'info' : 'critical',
              }),
            },
          ],
          tags: ['integration', 'inheritance', 'functions'],
        },
        {
          name: 'context_sensitive_compilation',
          description: 'Test compilation of context-sensitive grammar',
          grammar: {
            name: 'ContextSensitive',
            source: `
grammar ContextSensitive;

program: declaration*;

declaration: type_declaration | variable_declaration;

type_declaration: 'type' IDENTIFIER '=' type ';';
variable_declaration: type IDENTIFIER ';';

type: 'int' | 'string' | IDENTIFIER;

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'minotaur',
            features: ['context_sensitive', 'types', 'scoping'],
            complexity: 'complex',
            contextSensitive: true,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'type_definition',
              content: 'type Point = int; Point x;',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 8,
              expectedNodes: 6,
            },
            {
              name: 'undefined_type',
              content: 'UndefinedType x;',
              type: 'invalid',
              expectedResult: 'failure',
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'semantic',
              description: 'Context-sensitive features must work correctly',
              validator: (result: TestResult) => ({
                rule: 'context_sensitive',
                passed: result.runtime.success,
                message: result.runtime.success ? 'Context-sensitive parsing works' : 'Context-sensitive parsing failed',
                severity: result.runtime.success ? 'info' : 'error',
              }),
            },
          ],
          tags: ['integration', 'context_sensitive', 'types'],
        },
      ],
    };
  }

  /**
     * Basic integration test suite for quick tests
     */
  private static getBasicIntegrationTestSuite(): TestSuite {
    return {
      name: 'Basic Integration Tests',
      description: 'Essential integration tests for CI/CD',
      category: 'integration',
      tests: [
        {
          name: 'basic_inheritance_test',
          description: 'Basic test of inheritance functionality',
          grammar: {
            name: 'BasicInheritance',
            source: `
grammar BasicInheritance inherits SimpleBase;

extended_rule: base_rule | 'extended';
                        `,
            type: 'minotaur',
            features: ['inheritance'],
            complexity: 'simple',
            contextSensitive: false,
            inheritanceDepth: 1,
          },
          inputs: [
            {
              name: 'basic_input',
              content: 'extended',
              type: 'valid',
              expectedResult: 'success',
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'compilation',
              description: 'Basic inheritance must work',
              validator: (result: TestResult) => ({
                rule: 'basic_inheritance',
                passed: result.compilation.success,
                message: 'Basic inheritance test',
                severity: result.compilation.success ? 'info' : 'critical',
              }),
            },
          ],
          tags: ['integration', 'basic', 'inheritance'],
        },
      ],
    };
  }

  /**
     * System test suite
     */
  private static getSystemTestSuite(): TestSuite {
    return {
      name: 'System Tests',
      description: 'End-to-end system testing with real-world scenarios',
      category: 'system',
      tests: [
        {
          name: 'programming_language_compiler',
          description: 'Test compilation of a complete programming language grammar',
          grammar: {
            name: 'MiniLanguage',
            source: `
grammar MiniLanguage;

program: statement*;

statement: assignment
         | if_statement
         | while_statement
         | function_declaration
         | expression_statement
         ;

assignment: IDENTIFIER '=' expression ';';
if_statement: 'if' '(' expression ')' block ('else' block)?;
while_statement: 'while' '(' expression ')' block;
function_declaration: 'function' IDENTIFIER '(' parameter_list? ')' block;
expression_statement: expression ';';

block: '{' statement* '}';

parameter_list: IDENTIFIER (',' IDENTIFIER)*;

expression: logical_or;
logical_or: logical_and ('||' logical_and)*;
logical_and: equality ('&&' equality)*;
equality: comparison (('==' | '!=') comparison)*;
comparison: addition (('<' | '>' | '<=' | '>=') addition)*;
addition: multiplication (('+' | '-') multiplication)*;
multiplication: unary (('*' | '/') unary)*;
unary: ('!' | '-')? primary;
primary: IDENTIFIER | NUMBER | STRING | '(' expression ')' | function_call;

function_call: IDENTIFIER '(' argument_list? ')';
argument_list: expression (',' expression)*;

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
NUMBER: [0-9]+ ('.' [0-9]+)?;
STRING: '"' (~[\\r\\n"])* '"';
WS: [ \\t\\r\\n]+ -> skip;
COMMENT: '//' ~[\\r\\n]* -> skip;
                        `,
            type: 'antlr4',
            features: ['functions', 'control_flow', 'expressions', 'complete_language'],
            complexity: 'complex',
            contextSensitive: true,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'simple_program',
              content: `
function add(a, b) {
    return a + b;
}

x = 10;
y = 20;
result = add(x, y);
                            `,
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 30,
              expectedNodes: 20,
            },
            {
              name: 'complex_program',
              content: `
function fibonacci(n) {
    if (n <= 1) {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}

function main() {
    i = 0;
    while (i < 10) {
        result = fibonacci(i);
        print(result);
        i = i + 1;
    }
}
                            `,
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 60,
              expectedNodes: 40,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'compilation',
              description: 'Complete language grammar must compile',
              validator: (result: TestResult) => ({
                rule: 'complete_language',
                passed: result.compilation.success,
                message: result.compilation.success ? 'Complete language compiled' : 'Complete language compilation failed',
                severity: result.compilation.success ? 'info' : 'critical',
              }),
            },
            {
              type: 'performance',
              description: 'Performance must meet thresholds',
              validator: (result: TestResult) => ({
                rule: 'performance',
                passed: result.performance.compileTime < 5000,
                message: `Compile time: ${result.performance.compileTime}ms`,
                severity: result.performance.compileTime < 5000 ? 'info' : 'warning',
              }),
            },
          ],
          tags: ['system', 'complete_language', 'complex'],
        },
      ],
    };
  }

  /**
     * Performance test suite
     */
  private static getPerformanceTestSuite(): TestSuite {
    return {
      name: 'Performance Tests',
      description: 'Performance and optimization testing',
      category: 'performance',
      tests: [
        {
          name: 'large_grammar_performance',
          description: 'Test performance with large grammar',
          grammar: {
            name: 'LargeGrammar',
            source: this.generateLargeGrammar(),
            type: 'antlr4',
            features: ['large_grammar', 'performance'],
            complexity: 'extreme',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'large_input',
              content: this.generateLargeInput(1000),
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 5000,
              expectedNodes: 3000,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'performance',
              description: 'Large grammar must compile within time limit',
              validator: (result: TestResult) => ({
                rule: 'large_grammar_performance',
                passed: result.performance.compileTime < 10000,
                message: `Large grammar compile time: ${result.performance.compileTime}ms`,
                severity: result.performance.compileTime < 10000 ? 'info' : 'warning',
              }),
            },
          ],
          tags: ['performance', 'large', 'optimization'],
          timeout: 30000,
        },
      ],
    };
  }

  /**
     * Stress test suite
     */
  private static getStressTestSuite(): TestSuite {
    return {
      name: 'Stress Tests',
      description: 'High-load and edge case testing',
      category: 'stress',
      tests: [
        {
          name: 'memory_stress_test',
          description: 'Test memory usage under stress',
          grammar: {
            name: 'MemoryStress',
            source: `
grammar MemoryStress;

program: declaration*;
declaration: 'data' IDENTIFIER '[' NUMBER ']' ';';

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
NUMBER: [0-9]+;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'antlr4',
            features: ['memory_stress'],
            complexity: 'medium',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'many_declarations',
              content: this.generateManyDeclarations(10000),
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 50000,
              expectedNodes: 30000,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'performance',
              description: 'Memory usage must stay within limits',
              validator: (result: TestResult) => ({
                rule: 'memory_stress',
                passed: result.performance.memoryUsage < 100 * 1024 * 1024, // 100MB
                message: `Memory usage: ${(result.performance.memoryUsage / 1024 / 1024).toFixed(1)}MB`,
                severity: result.performance.memoryUsage < 100 * 1024 * 1024 ? 'info' : 'warning',
              }),
            },
          ],
          tags: ['stress', 'memory', 'large_input'],
          timeout: 60000,
        },
      ],
    };
  }

  /**
     * Language-specific test suite
     */
  private static getLanguageSpecificTestSuite(language: string): TestSuite {
    return {
      name: `${language} Specific Tests`,
      description: `Tests specific to ${language} code generation`,
      category: 'system',
      tests: [
        {
          name: `${language}_optimization_test`,
          description: `Test ${language}-specific optimizations`,
          grammar: {
            name: `${language}OptimizedGrammar`,
            source: `
grammar OptimizedGrammar;

expr: expr '+' term | term;
term: term '*' factor | factor;
factor: NUMBER | '(' expr ')';

NUMBER: [0-9]+;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'antlr4',
            features: ['optimization', language],
            complexity: 'medium',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'optimization_test',
              content: '1 + 2 * 3',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 5,
              expectedNodes: 5,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'compilation',
              description: `${language} code must compile and run`,
              validator: (result: TestResult) => ({
                rule: `${language}_optimization`,
                passed: result.compilation.success && result.runtime.success,
                message: `${language} optimization test`,
                severity: (result.compilation.success && result.runtime.success) ? 'info' : 'error',
              }),
            },
          ],
          tags: ['language_specific', language, 'optimization'],
        },
      ],
    };
  }

  /**
     * Generate large grammar for performance testing
     */
  private static generateLargeGrammar(): string {
    let grammar = `
grammar LargeGrammar;

program: statement*;
        `;

    // Generate many rules
    for (let i = 0; i < 100; i++) {
      grammar += `
rule${i}: 'keyword${i}' IDENTIFIER ';'
        | 'alt${i}' NUMBER ';'
        ;
            `;
    }

    grammar += `
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
NUMBER: [0-9]+;
WS: [ \\t\\r\\n]+ -> skip;
        `;

    return grammar;
  }

  /**
     * Generate large input for testing
     */
  private static generateLargeInput(lines: number): string {
    let content = '';
    for (let i = 0; i < lines; i++) {
      content += `keyword${i % 10} var${i};\n`;
    }
    return content;
  }

  /**
     * Generate many declarations for stress testing
     */
  private static generateManyDeclarations(count: number): string {
    let content = '';
    for (let i = 0; i < count; i++) {
      content += `data array${i}[${i + 1}];\n`;
    }
    return content;
  }

  /**
     * Get regression test configuration
     */
  public static getRegressionTests(): TestConfiguration {
    return {
      name: 'Minotaur Regression Tests',
      description: 'Regression testing to ensure no functionality breaks',
      targetLanguages: ['c', 'c++', 'java', 'python', 'javascript'],
      testSuites: [
        this.getUnitTestSuite(),
        this.getIntegrationTestSuite(),
      ],
      validationLevel: 'standard',
      enablePerformanceTests: true,
      enableCompilationTests: true,
      enableRuntimeTests: true,
      enableCrossLanguageTests: false,
      outputDirectory: './test-output-regression',
      timeoutMs: 180000, // 3 minutes
      maxMemoryMB: 1536,
    };
  }

  /**
     * Get cross-language compatibility tests
     */
  public static getCrossLanguageTests(): TestConfiguration {
    return {
      name: 'Cross-Language Compatibility Tests',
      description: 'Test compatibility and consistency across target languages',
      targetLanguages: ['c', 'c++', 'java', 'c#', 'python', 'javascript', 'rust', 'go', 'webassembly'],
      testSuites: [
        this.getCrossLanguageTestSuite(),
      ],
      validationLevel: 'comprehensive',
      enablePerformanceTests: true,
      enableCompilationTests: true,
      enableRuntimeTests: true,
      enableCrossLanguageTests: true,
      outputDirectory: './test-output-cross-language',
      timeoutMs: 600000, // 10 minutes
      maxMemoryMB: 4096,
    };
  }

  /**
     * Cross-language test suite
     */
  private static getCrossLanguageTestSuite(): TestSuite {
    return {
      name: 'Cross-Language Tests',
      description: 'Ensure consistent behavior across all target languages',
      category: 'system',
      tests: [
        {
          name: 'cross_language_consistency',
          description: 'Test that all languages produce equivalent results',
          grammar: {
            name: 'ConsistencyTest',
            source: `
grammar ConsistencyTest;

expr: expr '+' term | term;
term: term '*' factor | factor;
factor: NUMBER | '(' expr ')';

NUMBER: [0-9]+;
WS: [ \\t\\r\\n]+ -> skip;
                        `,
            type: 'antlr4',
            features: ['cross_language', 'consistency'],
            complexity: 'simple',
            contextSensitive: false,
            inheritanceDepth: 0,
          },
          inputs: [
            {
              name: 'consistency_test',
              content: '1 + 2 * 3',
              type: 'valid',
              expectedResult: 'success',
              expectedTokens: 5,
              expectedNodes: 5,
            },
          ],
          expectedOutputs: [],
          validations: [
            {
              type: 'cross_language',
              description: 'All languages must produce consistent results',
              validator: (result: TestResult) => ({
                rule: 'cross_language_consistency',
                passed: result.compilation.success && result.runtime.success,
                message: `Cross-language consistency for ${result.targetLanguage}`,
                severity: (result.compilation.success && result.runtime.success) ? 'info' : 'error',
              }),
            },
          ],
          tags: ['cross_language', 'consistency', 'all_languages'],
        },
      ],
    };
  }
}

