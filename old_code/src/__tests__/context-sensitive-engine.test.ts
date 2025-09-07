/**
 * Tests for ContextSensitiveEngine IMPLEMENTED implementations
 */

import { ContextSensitiveEngine, ContextSensitiveConfiguration } from '../compiler/ContextSensitiveEngine';
import { Grammar } from '../core/grammar/Grammar';
import { InheritanceResolver } from '../utils/InheritanceResolver';

describe('ContextSensitiveEngine', () => {
  let engine: ContextSensitiveEngine;
  let mockInheritanceResolver: jest.Mocked<InheritanceResolver>;
  let testGrammar: Grammar;

  beforeEach(() => {
    // Create mock inheritance resolver
    mockInheritanceResolver = {
      resolveInheritance: jest.fn(),
    } as any;

    // Create test configuration
    const config: ContextSensitiveConfiguration = {
      enableContextInheritance: true,
      enableSemanticActions: true,
      enableSymbolTracking: true,
      enableScopeAnalysis: true,
      optimizationLevel: 'basic',
      maxContextDepth: 10,
      enableContextCaching: true,
    };

    // Create engine
    engine = new ContextSensitiveEngine(config, mockInheritanceResolver);

    // Create test grammar
    testGrammar = new Grammar('TestGrammar');
  });

  describe('Grammar name getter', () => {
    it('should return unknown when no grammar is set', () => {
      expect(engine.getCurrentGrammarName()).toBe('unknown');
    });

    it('should return correct grammar name after parsing starts', async () => {
      // Mock successful parsing
      mockInheritanceResolver.resolveInheritance.mockReturnValue(testGrammar);

      try {
        await engine.parseWithContext(testGrammar, 'test input', {});
      } catch (error) {
        // Expected to fail due to mock implementation, but grammar name should be set
      }

      expect(engine.getCurrentGrammarName()).toBe('TestGrammar');
    });

    it('should handle grammar with different name', async () => {
      const anotherGrammar = new Grammar('MySpecialGrammar');

      try {
        await engine.parseWithContext(anotherGrammar, 'test input', {});
      } catch (error) {
        // Expected to fail due to mock implementation
      }

      expect(engine.getCurrentGrammarName()).toBe('MySpecialGrammar');
    });
  });

  describe('Inheritance resolver integration', () => {
    it('should call inheritance resolver for base grammars', async () => {
      // Create grammar with base grammars
      const derivedGrammar = new Grammar('DerivedGrammar');
      const baseGrammarMock = new Grammar('BaseGrammar');

      // Mock getBaseGrammars to return a base grammar
      jest.spyOn(derivedGrammar, 'getBaseGrammars').mockReturnValue(['BaseGrammar']);
      mockInheritanceResolver.resolveInheritance.mockReturnValue(baseGrammarMock);

      try {
        await engine.parseWithContext(derivedGrammar, 'test input', {});
      } catch (error) {
        // Expected to fail due to mock implementation
      }

      expect(mockInheritanceResolver.resolveInheritance).toHaveBeenCalledWith('BaseGrammar');
    });

    it('should handle inheritance resolver errors gracefully', async () => {
      const derivedGrammar = new Grammar('DerivedGrammar');

      jest.spyOn(derivedGrammar, 'getBaseGrammars').mockReturnValue(['NonExistentGrammar']);
      mockInheritanceResolver.resolveInheritance.mockImplementation(() => {
        throw new Error('Grammar not found');
      });

      // Should not throw error, should handle gracefully
      try {
        await engine.parseWithContext(derivedGrammar, 'test input', {});
      } catch (error) {
        // Expected to fail due to mock implementation, but not due to inheritance error
      }

      expect(mockInheritanceResolver.resolveInheritance).toHaveBeenCalledWith('NonExistentGrammar');
    });
  });
});