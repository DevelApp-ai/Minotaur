/**
 * Integration tests for compiler components using advanced Grammar methods
 * Tests that previously IMPLEMENTED-marked functionality now works correctly
 */

import { Grammar } from '../core/grammar/Grammar';

describe('Compiler Integration with Advanced Grammar Methods', () => {
  let grammar: Grammar;

  beforeEach(() => {
    grammar = new Grammar('IntegrationTestGrammar');

    // Set up a realistic grammar scenario
    grammar.addRule({
      name: 'program',
      definition: 'statement*',
      type: 'production',
    });

    grammar.addRule({
      name: 'statement',
      definition: 'assignment | expression | block',
      type: 'production',
    });

    grammar.addRule({
      name: 'assignment',
      definition: 'IDENTIFIER ASSIGN expression',
      type: 'production',
    });

    // Add embedded languages to test cross-language validation
    grammar.addEmbeddedLanguage('javascript');
    grammar.addEmbeddedLanguage('css');

    // Add symbol definitions
    grammar.addSymbolDefinition('globalVar', { type: 'variable', scope: 'global' });
    grammar.addSymbolDefinition('mainFunction', { type: 'function', scope: 'global' });
  });

  describe('CompilerCompilerExport Integration', () => {
    test('getProductions() method works for compiler export', () => {
      // This tests the IMPLEMENTED: Implement getProductions() method in Grammar class
      const productions = grammar.getProductions();

      expect(productions).toBeDefined();
      expect(Array.isArray(productions)).toBe(true);
      expect(productions.length).toBeGreaterThan(0);

      // Verify structure expected by compiler
      productions.forEach(production => {
        expect(production).toHaveProperty('name');
        expect(production).toHaveProperty('definition');
        expect(production).toHaveProperty('type');
      });
    });

    test('getSymbols() method works for compiler export', () => {
      // This tests the IMPLEMENTED: Implement getSymbols() method in Grammar class
      const symbols = grammar.getSymbols();

      expect(symbols).toBeDefined();
      expect(symbols instanceof Map).toBe(true);
      expect(symbols.size).toBeGreaterThan(0);

      // Should include both rule symbols and explicit symbol definitions
      expect(symbols.has('program')).toBe(true);
      expect(symbols.has('statement')).toBe(true);
      expect(symbols.has('globalVar')).toBe(true);
      expect(symbols.has('mainFunction')).toBe(true);
    });

    test('getValidationRules() method works for compiler export', () => {
      // This tests the IMPLEMENTED: Implement getValidationRules() method in Grammar class
      const validationRules = grammar.getValidationRules();

      expect(validationRules).toBeDefined();
      expect(Array.isArray(validationRules)).toBe(true);
      expect(validationRules.length).toBeGreaterThan(0);

      // Verify structure expected by compiler
      validationRules.forEach(rule => {
        expect(rule).toHaveProperty('name');
        expect(rule).toHaveProperty('rule');
        expect(rule).toHaveProperty('sourceLanguage');
        expect(rule).toHaveProperty('targetLanguage');
        expect(rule).toHaveProperty('severity');
      });
    });

    test('getCrossLanguageValidation() method works for compiler export', () => {
      // This tests the IMPLEMENTED: Implement getCrossLanguageValidation() method in Grammar class
      const crossLangValidation = grammar.getCrossLanguageValidation();

      expect(typeof crossLangValidation).toBe('boolean');
      expect(crossLangValidation).toBe(true); // Should be true because we have embedded languages

      // Test with grammar without embedded languages
      const simpleGrammar = new Grammar('SimpleGrammar');
      expect(simpleGrammar.getCrossLanguageValidation()).toBe(false);
    });
  });

  describe('EmbeddedGrammarParser Integration', () => {
    test('getTokenSplitter() method works for parser', () => {
      // This tests the IMPLEMENTED: Implement token splitter support in Grammar class
      const tokenSplitter = grammar.getTokenSplitter();

      expect(tokenSplitter).not.toBeNull();
      expect(tokenSplitter).toHaveProperty('split');
      expect(typeof tokenSplitter!.split).toBe('function');

      // Test with realistic code input
      const codeInput = 'var x = 10; function test() { return x + 5; }';
      const tokens = tokenSplitter!.split(codeInput);

      expect(Array.isArray(tokens)).toBe(true);
      expect(tokens.length).toBeGreaterThan(0);
      expect(tokens).toContain('var');
      expect(tokens).toContain('x');
      expect(tokens).toContain('10');
      expect(tokens).toContain('function');
      expect(tokens).toContain('test');
    });
  });

  describe('ContextSensitiveEngine Integration', () => {
    test('grammar name access works correctly', () => {
      // This tests the IMPLEMENTED: Add public getter for grammar name
      expect(grammar.getName()).toBe('IntegrationTestGrammar');
      expect(grammar.getGrammarName()).toBe('IntegrationTestGrammar');

      // Both methods should return the same value
      expect(grammar.getName()).toBe(grammar.getGrammarName());
    });

    test('symbol extraction from rules works', () => {
      // This tests the IMPLEMENTED: Define GrammarRule type functionality
      const symbols = grammar.getSymbols();

      // Should extract symbols from all rules
      expect(symbols.has('program')).toBe(true);
      expect(symbols.has('statement')).toBe(true);
      expect(symbols.has('assignment')).toBe(true);

      // Check symbol metadata
      const programSymbol = symbols.get('program');
      expect(programSymbol).toHaveProperty('type');
      expect(programSymbol).toHaveProperty('scope', 'grammar');
      expect(programSymbol).toHaveProperty('definition');
    });
  });

  describe('CrossLanguageValidator Integration', () => {
    test('validation rules integration works', () => {
      // This tests the IMPLEMENTED: Implement getValidationRules method in Grammar class
      const validationRules = grammar.getValidationRules();

      expect(validationRules.length).toBeGreaterThan(0);

      // Should have proper structure for cross-language validation
      const rule = validationRules[0];
      expect(rule.sourceLanguage).toBe('IntegrationTestGrammar');
      expect(['error', 'warning', 'info']).toContain(rule.severity);
    });
  });

  describe('Real-world Scenario Simulation', () => {
    test('complex multi-language grammar scenario', () => {
      // Create a complex scenario similar to what Golem verification might encounter
      const webGrammar = new Grammar('WebApplicationGrammar');

      // Add HTML-like rules
      webGrammar.addRule({
        name: 'htmlDocument',
        definition: 'htmlTag*',
        type: 'production',
      });

      webGrammar.addRule({
        name: 'htmlTag',
        definition: 'OPEN_TAG attributes? CLOSE_TAG content? END_TAG',
        type: 'production',
      });

      // Add embedded languages (typical web scenario)
      webGrammar.addEmbeddedLanguage('javascript');
      webGrammar.addEmbeddedLanguage('css');
      webGrammar.addEmbeddedLanguage('json');

      // Add symbol definitions for web components
      webGrammar.addSymbolDefinition('documentElement', { type: 'variable', scope: 'global' });
      webGrammar.addSymbolDefinition('eventHandler', { type: 'function', scope: 'local' });
      webGrammar.addSymbolDefinition('styleSheet', { type: 'variable', scope: 'global' });

      // Test all advanced methods work together
      expect(webGrammar.getProductions().length).toBe(2);
      expect(webGrammar.getSymbols().size).toBeGreaterThanOrEqual(5); // 2 rules + 3 symbols
      expect(webGrammar.getCrossLanguageValidation()).toBe(true);
      expect(webGrammar.getValidationRules().length).toBeGreaterThan(0);

      // Test tokenization with web content
      const tokenSplitter = webGrammar.getTokenSplitter();
      const webContent = '<div class="container">Hello World</div>';
      const tokens = tokenSplitter!.split(webContent);

      expect(tokens.length).toBeGreaterThan(0);
      // The tokenizer splits on delimiters, so '<div' becomes one token
      expect(tokens.some(token => token.includes('div'))).toBe(true);
      expect(tokens.some(token => token.includes('class'))).toBe(true);
      expect(tokens.some(token => token.includes('container'))).toBe(true);
      expect(tokens.some(token => token.includes('Hello'))).toBe(true);
      expect(tokens.some(token => token.includes('World'))).toBe(true);
    });

    test('compiler pipeline integration', () => {
      // Simulate a complete compiler pipeline using the new methods
      const pipelineGrammar = new Grammar('PipelineTestGrammar');

      // Add rules
      pipelineGrammar.addRule({
        name: 'compilationUnit',
        definition: 'declaration*',
        type: 'production',
      });

      // Add embedded language
      pipelineGrammar.addEmbeddedLanguage('assembly');

      // Step 1: Get productions (used by compiler export)
      const productions = pipelineGrammar.getProductions();
      expect(productions.length).toBe(1);

      // Step 2: Get symbols (used by symbol table generation)
      const symbols = pipelineGrammar.getSymbols();
      expect(symbols.size).toBeGreaterThan(0);

      // Step 3: Get validation rules (used by validator)
      const validationRules = pipelineGrammar.getValidationRules();
      expect(validationRules.length).toBeGreaterThan(0);

      // Step 4: Check cross-language validation (used by multi-language scenarios)
      const crossLangValidation = pipelineGrammar.getCrossLanguageValidation();
      expect(crossLangValidation).toBe(true);

      // Step 5: Get token splitter (used by parser)
      const tokenSplitter = pipelineGrammar.getTokenSplitter();
      expect(tokenSplitter).not.toBeNull();

      // All steps should complete without errors
      expect(true).toBe(true); // Pipeline completed successfully
    });
  });
});

