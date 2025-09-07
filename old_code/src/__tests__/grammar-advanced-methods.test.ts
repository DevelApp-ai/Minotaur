/**
 * Tests for advanced Grammar class methods
 * Ensures newly implemented methods work correctly for Golem verification
 */

import { Grammar, _Rule, _GrammarRule, _SymbolType, _ValidationSeverity } from '../core/grammar/Grammar';

describe('Grammar Advanced Methods', () => {
  let grammar: Grammar;

  beforeEach(() => {
    grammar = new Grammar('TestGrammar');

    // Add some test rules
    grammar.addRule({
      name: 'expression',
      definition: 'term ((PLUS | MINUS) term)*',
      type: 'production',
    });

    grammar.addRule({
      name: 'term',
      definition: 'factor ((MULTIPLY | DIVIDE) factor)*',
      type: 'production',
    });

    grammar.addRule({
      name: 'factor',
      definition: 'NUMBER | IDENTIFIER | LPAREN expression RPAREN',
      type: 'production',
    });
  });

  describe('getSymbols()', () => {
    test('returns symbols from rules', () => {
      const symbols = grammar.getSymbols();

      expect(symbols).toBeInstanceOf(Map);
      expect(symbols.size).toBeGreaterThan(0);

      // Check that rule names are included as symbols
      expect(symbols.has('expression')).toBe(true);
      expect(symbols.has('term')).toBe(true);
      expect(symbols.has('factor')).toBe(true);

      // Check symbol structure
      const expressionSymbol = symbols.get('expression');
      expect(expressionSymbol).toHaveProperty('type');
      expect(expressionSymbol).toHaveProperty('scope', 'grammar');
      expect(expressionSymbol).toHaveProperty('definition');
    });

    test('includes symbol definitions when present', () => {
      // Add a symbol definition
      grammar.addSymbolDefinition('testSymbol', {
        type: 'variable',
        scope: 'global',
      });

      const symbols = grammar.getSymbols();
      expect(symbols.has('testSymbol')).toBe(true);

      const testSymbol = symbols.get('testSymbol');
      expect(testSymbol.type).toBe('variable');
      expect(testSymbol.scope).toBe('global');
    });
  });

  describe('getProductions()', () => {
    test('returns all grammar rules', () => {
      const productions = grammar.getProductions();

      expect(Array.isArray(productions)).toBe(true);
      expect(productions.length).toBe(3);

      const ruleNames = productions.map(rule => rule.name);
      expect(ruleNames).toContain('expression');
      expect(ruleNames).toContain('term');
      expect(ruleNames).toContain('factor');
    });

    test('productions have correct structure', () => {
      const productions = grammar.getProductions();

      productions.forEach(production => {
        expect(production).toHaveProperty('name');
        expect(production).toHaveProperty('definition');
        expect(production).toHaveProperty('type');
        expect(typeof production.name).toBe('string');
        expect(typeof production.definition).toBe('string');
      });
    });
  });

  describe('getValidationRules()', () => {
    test('returns validation rules array', () => {
      const validationRules = grammar.getValidationRules();

      expect(Array.isArray(validationRules)).toBe(true);
      expect(validationRules.length).toBeGreaterThan(0);
    });

    test('validation rules have correct structure', () => {
      const validationRules = grammar.getValidationRules();

      validationRules.forEach(rule => {
        expect(rule).toHaveProperty('name');
        expect(rule).toHaveProperty('rule');
        expect(rule).toHaveProperty('sourceLanguage');
        expect(rule).toHaveProperty('targetLanguage');
        expect(rule).toHaveProperty('severity');

        expect(typeof rule.name).toBe('string');
        expect(typeof rule.rule).toBe('string');
        expect(typeof rule.sourceLanguage).toBe('string');
        expect(typeof rule.targetLanguage).toBe('string');
      });
    });
  });

  describe('getCrossLanguageValidation()', () => {
    test('returns false when no embedded languages', () => {
      expect(grammar.getCrossLanguageValidation()).toBe(false);
    });

    test('returns true when embedded languages are present', () => {
      grammar.addEmbeddedLanguage('javascript');
      expect(grammar.getCrossLanguageValidation()).toBe(true);
    });

    test('returns true with multiple embedded languages', () => {
      grammar.addEmbeddedLanguage('javascript');
      grammar.addEmbeddedLanguage('css');
      grammar.addEmbeddedLanguage('html');
      expect(grammar.getCrossLanguageValidation()).toBe(true);
    });
  });

  describe('getTokenSplitter()', () => {
    test('returns a token splitter object', () => {
      const tokenSplitter = grammar.getTokenSplitter();

      expect(tokenSplitter).not.toBeNull();
      expect(tokenSplitter).toHaveProperty('split');
      expect(typeof tokenSplitter!.split).toBe('function');
    });

    test('token splitter splits input correctly', () => {
      const tokenSplitter = grammar.getTokenSplitter();

      const input = 'hello world, test; (example) {block} [array]';
      const tokens = tokenSplitter!.split(input);

      expect(Array.isArray(tokens)).toBe(true);
      expect(tokens.length).toBeGreaterThan(0);

      // Should split on whitespace and delimiters
      expect(tokens).toContain('hello');
      expect(tokens).toContain('world');
      expect(tokens).toContain('test');
      expect(tokens).toContain('example');
      expect(tokens).toContain('block');
      expect(tokens).toContain('array');

      // Should not contain empty strings
      expect(tokens.every(token => token.length > 0)).toBe(true);
    });

    test('token splitter handles empty input', () => {
      const tokenSplitter = grammar.getTokenSplitter();
      const tokens = tokenSplitter!.split('');

      expect(Array.isArray(tokens)).toBe(true);
      expect(tokens.length).toBe(0);
    });

    test('token splitter handles whitespace-only input', () => {
      const tokenSplitter = grammar.getTokenSplitter();
      const tokens = tokenSplitter!.split('   \t\n  ');

      expect(Array.isArray(tokens)).toBe(true);
      expect(tokens.length).toBe(0);
    });
  });

  describe('getGrammarName()', () => {
    test('returns the grammar name', () => {
      expect(grammar.getGrammarName()).toBe('TestGrammar');
    });

    test('returns updated name after setName', () => {
      grammar.setName('UpdatedGrammar');
      expect(grammar.getGrammarName()).toBe('UpdatedGrammar');
      expect(grammar.getName()).toBe('UpdatedGrammar');
    });
  });

  describe('Symbol and Reference Management', () => {
    test('addSymbolDefinition works correctly', () => {
      grammar.addSymbolDefinition('myVariable', {
        type: 'variable',
        scope: 'local',
      });

      const symbols = grammar.getSymbols();
      expect(symbols.has('myVariable')).toBe(true);

      const symbol = symbols.get('myVariable');
      expect(symbol.type).toBe('variable');
      expect(symbol.scope).toBe('local');
    });

    test('addReference works correctly', () => {
      grammar.addReference('externalGrammar', { type: 'import' });

      // References are tracked internally
      // This test ensures no errors are thrown
      expect(() => {
        grammar.addReference('anotherGrammar', { type: 'extend' });
      }).not.toThrow();
    });
  });

  describe('Embedded Language Support', () => {
    test('embedded language management works', () => {
      expect(grammar.getEmbeddedLanguages().size).toBe(0);

      grammar.addEmbeddedLanguage('javascript');
      expect(grammar.getEmbeddedLanguages().size).toBe(1);
      expect(grammar.hasEmbeddedLanguage('javascript')).toBe(true);
      expect(grammar.hasEmbeddedLanguage('python')).toBe(false);

      grammar.addEmbeddedLanguage('css');
      expect(grammar.getEmbeddedLanguages().size).toBe(2);
      expect(grammar.hasEmbeddedLanguage('css')).toBe(true);
    });

    test('setEmbeddedLanguages replaces existing languages', () => {
      grammar.addEmbeddedLanguage('javascript');
      grammar.addEmbeddedLanguage('css');
      expect(grammar.getEmbeddedLanguages().size).toBe(2);

      grammar.setEmbeddedLanguages(['python', 'ruby']);
      expect(grammar.getEmbeddedLanguages().size).toBe(2);
      expect(grammar.hasEmbeddedLanguage('python')).toBe(true);
      expect(grammar.hasEmbeddedLanguage('ruby')).toBe(true);
      expect(grammar.hasEmbeddedLanguage('javascript')).toBe(false);
      expect(grammar.hasEmbeddedLanguage('css')).toBe(false);
    });
  });

  describe('Integration with Complex Scenarios', () => {
    test('handles complex grammar with multiple features', () => {
      // Create a complex grammar scenario
      const complexGrammar = new Grammar('ComplexLanguage');

      // Add multiple rules
      for (let i = 0; i < 10; i++) {
        complexGrammar.addRule({
          name: `rule${i}`,
          definition: `definition${i}`,
          type: 'production',
        });
      }

      // Add embedded languages
      complexGrammar.addEmbeddedLanguage('javascript');
      complexGrammar.addEmbeddedLanguage('css');
      complexGrammar.addEmbeddedLanguage('html');

      // Add symbol definitions
      complexGrammar.addSymbolDefinition('globalVar', { type: 'variable', scope: 'global' });
      complexGrammar.addSymbolDefinition('localFunc', { type: 'function', scope: 'local' });

      // Test all methods work together
      expect(complexGrammar.getProductions().length).toBe(10);
      expect(complexGrammar.getSymbols().size).toBeGreaterThanOrEqual(12); // 10 rules + 2 symbols
      expect(complexGrammar.getCrossLanguageValidation()).toBe(true);
      expect(complexGrammar.getValidationRules().length).toBeGreaterThan(0);
      expect(complexGrammar.getTokenSplitter()).not.toBeNull();

      // Test tokenization with complex input
      const tokenSplitter = complexGrammar.getTokenSplitter();
      const complexInput = 'function test() { return x + y; }';
      const tokens = tokenSplitter!.split(complexInput);
      expect(tokens.length).toBeGreaterThan(5);
      expect(tokens).toContain('function');
      expect(tokens).toContain('test');
      expect(tokens).toContain('return');
    });
  });
});

