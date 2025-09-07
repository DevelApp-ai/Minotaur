import { EmbeddedGrammarParser } from './EmbeddedGrammarParser';

describe('EmbeddedGrammarParser', () => {
  let parser: EmbeddedGrammarParser;

  beforeEach(() => {
    parser = new EmbeddedGrammarParser();
  });

  describe('Basic Grammar Parsing', () => {
    test('parses simple grammar successfully', async () => {
      const grammarText = `
        grammar SimpleGrammar {
          start: expression;
          expression: term ('+' term)*;
          term: factor ('*' factor)*;
          factor: NUMBER | '(' expression ')';
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.grammar).not.toBeNull();
      expect(result.errors).toHaveLength(0);
      expect(result.metadata.linesProcessed).toBeGreaterThan(0);
    });

    test('handles empty grammar input', async () => {
      const result = await parser.parseGrammar('');

      expect(result.success).toBe(false);
      expect(result.errors).toHaveLength(1);
      expect(result.errors[0].message).toContain('empty');
    });

    test('reports syntax errors with line and column information', async () => {
      const grammarText = `
        grammar InvalidGrammar {
          start: expression
          expression: term +;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0]).toHaveProperty('line');
      expect(result.errors[0]).toHaveProperty('column');
      expect(result.errors[0]).toHaveProperty('code');
    });
  });

  describe('Embedded Language Support', () => {
    test('parses @CONTEXT directive correctly', async () => {
      const grammarText = `
        grammar EmbeddedGrammar {
          @CONTEXT(language="javascript", scope="expression")
          start: jsExpression;
          jsExpression: IDENTIFIER | NUMBER | STRING;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.directivesFound).toBeGreaterThan(0);
      expect(result.metadata.embeddedLanguagesDetected).toContain('javascript');
    });

    test('parses @SYMBOL directive correctly', async () => {
      const grammarText = `
        grammar SymbolGrammar {
          @SYMBOL(name="variable", type="identifier", scope="global")
          start: declaration;
          declaration: 'var' IDENTIFIER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.symbolsDefinedCount).toBeGreaterThan(0);
    });

    test('parses @REFERENCE directive correctly', async () => {
      const grammarText = `
        grammar ReferenceGrammar {
          @REFERENCE(target="variable", resolution="lexical")
          start: usage;
          usage: IDENTIFIER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.directivesFound).toBeGreaterThan(0);
    });

    test('parses @VALIDATE directive correctly', async () => {
      const grammarText = `
        grammar ValidationGrammar {
          @VALIDATE(rule="type_check", severity="error")
          start: expression;
          expression: IDENTIFIER | NUMBER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.directivesFound).toBeGreaterThan(0);
    });

    test('handles multiple embedded languages', async () => {
      const grammarText = `
        grammar MultiLanguageGrammar {
          @CONTEXT(language="javascript", scope="js_block")
          @CONTEXT(language="css", scope="css_block")
          start: document;
          document: js_block | css_block;
          js_block: 'script' JS_CODE 'endscript';
          css_block: 'style' CSS_CODE 'endstyle';
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.embeddedLanguagesDetected).toContain('javascript');
      expect(result.metadata.embeddedLanguagesDetected).toContain('css');
      expect(result.metadata.contextSwitchesFound).toBeGreaterThan(0);
    });
  });

  describe('Error Handling and Validation', () => {
    test('validates symbol definitions', async () => {
      const grammarText = `
        grammar InvalidSymbolGrammar {
          @SYMBOL(name="", type="invalid_type")
          start: expression;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(false);
      expect(result.errors.some(e => e.message.includes('Symbol name cannot be empty'))).toBe(true);
    });

    test('validates reference targets', async () => {
      const grammarText = `
        grammar InvalidReferenceGrammar {
          @REFERENCE(target="nonexistent_symbol")
          start: expression;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.warnings.some(w => w.message.includes('Reference target') && w.message.includes('may not exist'))).toBe(true);
    });

    test('provides helpful error suggestions', async () => {
      const grammarText = `
        grammar TypoGrammar {
          start: expresion;  // typo in 'expression'
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.warnings.some(w => w.suggestion.length > 0)).toBe(true);
    });

    test('handles deeply nested grammar structures', async () => {
      const grammarText = `
        grammar DeepGrammar {
          start: level1;
          level1: level2 | 'a';
          level2: level3 | 'b';
          level3: level4 | 'c';
          level4: level5 | 'd';
          level5: 'e';
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.linesProcessed).toBeGreaterThan(5);
    });
  });

  describe('Performance and Metadata', () => {
    test('tracks parsing performance', async () => {
      const grammarText = `
        grammar PerformanceGrammar {
          start: expression;
          expression: term ('+' term)*;
          term: factor ('*' factor)*;
          factor: NUMBER | IDENTIFIER | '(' expression ')';
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.metadata.parseTime).toBeGreaterThanOrEqual(0);
      expect(result.metadata.linesProcessed).toBeGreaterThan(0);
    });

    test('provides comprehensive metadata', async () => {
      const grammarText = `
        grammar MetadataGrammar {
          @CONTEXT(language="javascript")
          @SYMBOL(name="var1", type="variable")
          @SYMBOL(name="var2", type="variable")
          start: declaration+;
          declaration: 'var' IDENTIFIER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.metadata).toHaveProperty('parseTime');
      expect(result.metadata).toHaveProperty('linesProcessed');
      expect(result.metadata).toHaveProperty('directivesFound');
      expect(result.metadata).toHaveProperty('embeddedLanguagesDetected');
      expect(result.metadata).toHaveProperty('contextSwitchesFound');
      expect(result.metadata).toHaveProperty('symbolsDefinedCount');
      expect(result.metadata.symbolsDefinedCount).toBe(2);
    });

    test('handles large grammar files efficiently', async () => {
      // Generate a large grammar
      let grammarText = 'grammar LargeGrammar {\n  start: rule1;\n';
      for (let i = 1; i <= 100; i++) {
        grammarText += `  rule${i}: 'token${i}' | rule${i + 1};\n`;
      }
      grammarText += '  rule101: "end";\n}';

      const startTime = Date.now();
      const result = await parser.parseGrammar(grammarText);
      const endTime = Date.now();

      expect(result.success).toBe(true);
      expect(endTime - startTime).toBeLessThan(5000); // Should parse within 5 seconds
      expect(result.metadata.linesProcessed).toBeGreaterThan(100);
    });
  });

  describe('Advanced Features', () => {
    test('supports grammar inheritance', async () => {
      const grammarText = `
        grammar ExtendedGrammar extends BaseGrammar {
          start: enhanced_expression;
          enhanced_expression: base_expression | new_feature;
          new_feature: 'new' IDENTIFIER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      // Should handle inheritance syntax without errors
    });

    test('supports parameterized rules', async () => {
      const grammarText = `
        grammar ParameterizedGrammar {
          start: list<IDENTIFIER>;
          list<T>: T (',' T)*;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      // Should handle parameterized syntax
    });

    test('supports conditional compilation', async () => {
      const grammarText = `
        grammar ConditionalGrammar {
          @IF(target="javascript")
          start: js_specific_rule;
          @ELSE
          start: generic_rule;
          @ENDIF
          
          js_specific_rule: 'function' IDENTIFIER;
          generic_rule: IDENTIFIER;
        }
      `;

      const result = await parser.parseGrammar(grammarText);

      expect(result.success).toBe(true);
      expect(result.metadata.directivesFound).toBeGreaterThan(0);
    });
  });
});

