import { AIAgentIntegration } from './AIAgentIntegration';
// Mock fetch for API calls
(global as any).fetch = jest.fn();

describe('AIAgentIntegration', () => {
  let aiAgent: AIAgentIntegration;

  beforeEach(() => {
    aiAgent = new AIAgentIntegration();
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.resetAllMocks();
  });

  describe('Grammar Generation', () => {
    test('generates grammar from natural language description', async () => {
      const mockResponse = {
        ok: true,
        json: async () => ({
          grammar: 'grammar Calculator { start: expression; expression: term (("+" | "-") term)*; }',
          confidence: 0.95,
          suggestions: ['Add more operators', 'Consider precedence rules'],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const description = 'Create a grammar for a simple calculator with addition and subtraction';
      const result = await aiAgent.generateGrammarFromDescription(description);

      expect(result.success).toBe(true);
      expect(result.grammar).toContain('Calculator');
      expect(result.confidence).toBe(0.95);
      expect(result.suggestions).toHaveLength(2);
    });

    test('handles API errors gracefully', async () => {
      (fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const description = 'Create a grammar for JSON parsing';
      const result = await aiAgent.generateGrammarFromDescription(description);

      expect(result.success).toBe(false);
      expect(result.error).toContain('Network error');
    });

    test('validates generated grammar syntax', async () => {
      const mockResponse = {
        ok: true,
        json: async () => ({
          grammar: 'invalid grammar syntax {{{',
          confidence: 0.8,
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const description = 'Create a simple grammar';
      const result = await aiAgent.generateGrammarFromDescription(description);

      expect(result.success).toBe(false);
      expect(result.error).toContain('syntax');
    });
  });

  describe('Grammar Optimization', () => {
    test('optimizes grammar for performance', async () => {
      const originalGrammar = `
        grammar Inefficient {
          start: rule1 | rule1 | rule1;
          rule1: 'a' rule2 'b';
          rule2: 'c' | 'c' | 'c';
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          optimizedGrammar: `
            grammar Efficient {
              start: rule1;
              rule1: 'a' rule2 'b';
              rule2: 'c';
            }
          `,
          optimizations: [
            'Removed duplicate alternatives',
            'Simplified rule structure',
          ],
          performanceGain: 0.35,
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.optimizeGrammar(originalGrammar);

      expect(result.success).toBe(true);
      expect(result.optimizedGrammar).not.toContain('rule1 | rule1 | rule1');
      expect(result.optimizations).toHaveLength(2);
      expect(result.performanceGain).toBe(0.35);
    });

    test('suggests grammar improvements', async () => {
      const grammar = `
        grammar Simple {
          start: expression;
          expression: NUMBER;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          suggestions: [
            {
              type: 'enhancement',
              description: 'Add support for operators',
              priority: 'high',
              implementation: 'expression: term (("+" | "-") term)*;',
            },
            {
              type: 'optimization',
              description: 'Use left recursion for better performance',
              priority: 'medium',
              implementation: 'expression: expression "+" term | term;',
            },
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.suggestImprovements(grammar);

      expect(result.success).toBe(true);
      expect(result.suggestions).toHaveLength(2);
      expect(result.suggestions[0].type).toBe('enhancement');
      expect(result.suggestions[1].type).toBe('optimization');
    });
  });

  describe('Code Generation', () => {
    test('generates parser code in multiple languages', async () => {
      const grammar = `
        grammar TestGrammar {
          start: expression;
          expression: NUMBER ('+' NUMBER)*;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          javascript: 'class TestGrammarParser { parse(input) { /* JS implementation */ } }',
          python: 'class TestGrammarParser:\n    def parse(self, input):\n        # Python implementation',
          java: 'public class TestGrammarParser { public void parse(String input) { /* Java implementation */ } }',
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.generateParserCode(grammar, ['javascript', 'python', 'java']);

      expect(result.success).toBe(true);
      expect(result.code.javascript).toContain('TestGrammarParser');
      expect(result.code.python).toContain('TestGrammarParser');
      expect(result.code.java).toContain('TestGrammarParser');
    });

    test('generates test cases for grammar', async () => {
      const grammar = `
        grammar Calculator {
          start: expression;
          expression: term ('+' term)*;
          term: NUMBER;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          testCases: [
            { input: '5', expected: true, description: 'Single number' },
            { input: '5+3', expected: true, description: 'Simple addition' },
            { input: '5+3+2', expected: true, description: 'Multiple additions' },
            { input: '5+', expected: false, description: 'Incomplete expression' },
            { input: '+5', expected: false, description: 'Invalid start' },
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.generateTestCases(grammar);

      expect(result.success).toBe(true);
      expect(result.testCases).toHaveLength(5);
      expect(result.testCases[0].input).toBe('5');
      expect(result.testCases[0].expected).toBe(true);
    });
  });

  describe('Error Analysis and Debugging', () => {
    test('analyzes parsing errors and suggests fixes', async () => {
      const grammar = `
        grammar BuggyGrammar {
          start: expression;
          expression: term ('+' term)*;
          term: NUMBER | IDENTIFIER;
        }
      `;

      const errorInput = '5 + + 3';
      const errorMessage = 'Unexpected token "+" at position 4';

      const mockResponse = {
        ok: true,
        json: async () => ({
          analysis: {
            errorType: 'syntax_error',
            location: { line: 1, column: 4 },
            cause: 'Consecutive operators without operand',
            suggestions: [
              'Check for missing operands between operators',
              'Consider adding error recovery rules',
            ],
          },
          fixes: [
            {
              description: 'Add error recovery for missing operands',
              grammarChange: 'term: NUMBER | IDENTIFIER | ERROR_TOKEN;',
            },
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.analyzeParsingError(grammar, errorInput, errorMessage);

      expect(result.success).toBe(true);
      expect(result.analysis.errorType).toBe('syntax_error');
      expect(result.analysis.suggestions).toHaveLength(2);
      expect(result.fixes).toHaveLength(1);
    });

    test('provides grammar debugging assistance', async () => {
      const grammar = `
        grammar AmbiguousGrammar {
          start: statement;
          statement: if_statement | expression;
          if_statement: 'if' expression 'then' statement;
          if_statement: 'if' expression 'then' statement 'else' statement;
          expression: IDENTIFIER;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          issues: [
            {
              type: 'ambiguity',
              description: 'Dangling else problem',
              severity: 'warning',
              location: 'if_statement rules',
              resolution: 'Use precedence or rewrite rules to eliminate ambiguity',
            },
          ],
          recommendations: [
            'Combine if_statement rules with optional else clause',
            'Add precedence declarations',
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.debugGrammar(grammar);

      expect(result.success).toBe(true);
      expect(result.issues).toHaveLength(1);
      expect(result.issues[0].type).toBe('ambiguity');
      expect(result.recommendations).toHaveLength(2);
    });
  });

  describe('Learning and Adaptation', () => {
    test('learns from user feedback', async () => {
      const feedback = {
        grammarId: 'test-grammar-123',
        rating: 4,
        comments: 'Good but could use better error handling',
        improvements: ['Add error recovery', 'Improve error messages'],
      };

      const mockResponse = {
        ok: true,
        json: async () => ({
          acknowledged: true,
          learningUpdate: 'Feedback incorporated into model',
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.submitFeedback(feedback);

      expect(result.success).toBe(true);
      expect(result.acknowledged).toBe(true);
    });

    test('adapts suggestions based on user preferences', async () => {
      const userPreferences = {
        preferredLanguages: ['javascript', 'typescript'],
        complexity: 'intermediate',
        style: 'functional',
      };

      await aiAgent.updateUserPreferences(userPreferences);

      const grammar = 'grammar Simple { start: expression; expression: NUMBER; }';

      const mockResponse = {
        ok: true,
        json: async () => ({
          suggestions: [
            {
              type: 'enhancement',
              description: 'Add TypeScript type annotations',
              priority: 'high',
              targetLanguage: 'typescript',
            },
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.suggestImprovements(grammar);

      expect(result.suggestions[0].targetLanguage).toBe('typescript');
    });
  });

  describe('Integration with External Tools', () => {
    test('integrates with external grammar validators', async () => {
      const grammar = `
        grammar TestGrammar {
          start: expression;
          expression: NUMBER;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          validationResults: [
            {
              tool: 'ANTLR',
              status: 'valid',
              warnings: [],
            },
            {
              tool: 'Yacc',
              status: 'valid',
              warnings: ['Consider left recursion'],
            },
          ],
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.validateWithExternalTools(grammar);

      expect(result.success).toBe(true);
      expect(result.validationResults).toHaveLength(2);
      expect(result.validationResults[0].tool).toBe('ANTLR');
    });

    test('exports grammar to different formats', async () => {
      const grammar = `
        grammar TestGrammar {
          start: expression;
          expression: NUMBER;
        }
      `;

      const mockResponse = {
        ok: true,
        json: async () => ({
          formats: {
            antlr: 'grammar TestGrammar; start: expression; expression: NUMBER;',
            yacc: '%start start\n%%\nstart: expression;\nexpression: NUMBER;',
            ebnf: 'start = expression;\nexpression = NUMBER;',
          },
        }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce(mockResponse);

      const result = await aiAgent.exportGrammar(grammar, ['antlr', 'yacc', 'ebnf']);

      expect(result.success).toBe(true);
      expect(result.formats.antlr).toContain('TestGrammar');
      expect(result.formats.yacc).toContain('%start');
      expect(result.formats.ebnf).toContain('=');
    });
  });
});

