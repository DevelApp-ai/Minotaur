/**
 * Tests for surgical refactoring validation implementations
 */

import SurgicalRefactoring, {
  SurgicalOperationType,
  SurgicalOperationRequest,
  SurgicalChange,
  SurgicalValidationIssue,
} from '../SurgicalRefactoring';
import { RefactoringEngine } from '../../refactoring/RefactoringEngine';
import { ContextManager } from '../../context/ContextManager';
import { LanguageManager, SupportedLanguage } from '../../languages/LanguageManager';
import { ParseContext, ScopeType } from '../../compiler/ContextSensitiveEngine';
import { CodePosition } from '../../context/ContextAwareParser';

describe('SurgicalRefactoring Validation', () => {
  let surgicalRefactoring: SurgicalRefactoring;
  let mockRefactoringEngine: jest.Mocked<RefactoringEngine>;
  let mockContextManager: jest.Mocked<ContextManager>;
  let mockLanguageManager: jest.Mocked<LanguageManager>;

  beforeEach(() => {
    // Create mock instances
    mockRefactoringEngine = {
      // Add minimal mock methods as needed
    } as jest.Mocked<RefactoringEngine>;

    mockContextManager = {
      getContextAt: jest.fn(),
    } as jest.Mocked<ContextManager>;

    mockLanguageManager = {
      getLanguageConfig: jest.fn(),
    } as jest.Mocked<LanguageManager>;

    surgicalRefactoring = new SurgicalRefactoring(
      mockRefactoringEngine,
      mockContextManager,
      mockLanguageManager,
    );
  });

  describe('validateSyntax', () => {
    it('should detect unbalanced brackets', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-1',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'replace',
        position,
        oldText: 'originalCode',
    // eslint-disable-next-line no-console
        newText: 'function test() { console.log("missing closing brace"',
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'function_declaration',
          symbolsAffected: ['test'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'function_declaration',
          syntaxNode: 'function_definition',
          semanticContext: 'global_scope',
          preservedElements: [],
          modifiedElements: ['originalCode'],
          addedElements: ['function test'],
          removedElements: ['originalCode'],
        },
      }];

      // Access the private method via array notation
      const result = await (surgicalRefactoring as any).validateSyntax(changes, request, mockContext);

      expect(result.valid).toBe(false);
      expect(result.issues).toHaveLength(2); // Missing closing brace + unbalanced quotes
      expect(result.issues[0].category).toBe('syntax');
      expect(result.issues[0].message).toContain('Structure balance error');
    });

    it('should pass for valid syntax', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-2',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'replace',
        position,
        oldText: 'originalCode',
        newText: 'const validVariable = "hello world";',
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: ['validVariable'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: ['originalCode'],
          addedElements: ['validVariable'],
          removedElements: ['originalCode'],
        },
      }];

      const result = await (surgicalRefactoring as any).validateSyntax(changes, request, mockContext);

      expect(result.valid).toBe(true);
      expect(result.issues).toHaveLength(0);
    });
  });

  describe('validateSemantics', () => {
    it('should detect semantic issues with deletions that have references', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-3',
        type: SurgicalOperationType.INLINE_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'delete',
        position,
        oldText: 'const myVariable = 42;',
        newText: '',
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: ['myVariable'],
          referencesAffected: 3, // Has references
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: [],
          removedElements: ['myVariable'],
        },
      }];

      const result = await (surgicalRefactoring as any).validateSemantics(changes, request, mockContext);

      expect(result.valid).toBe(false);
      expect(result.issues).toHaveLength(1);
      expect(result.issues[0].code).toBe('DANGLING_REFERENCES');
      expect(result.issues[0].severity).toBe('error');
    });

    it('should detect potential side effects', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-4',
        type: SurgicalOperationType.EXTRACT_FUNCTION,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'insert',
        position,
        oldText: '',
    // eslint-disable-next-line no-console
        newText: 'function newFunction() { console.log("debug"); fetch("/api/data"); }',
        scope: {
          type: 'function',
          startPosition: position,
          endPosition: position,
          contextType: 'function_declaration',
          symbolsAffected: ['newFunction'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'function_declaration',
          syntaxNode: 'function_definition',
          semanticContext: 'global_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: ['newFunction'],
          removedElements: [],
        },
      }];

      const result = await (surgicalRefactoring as any).validateSemantics(changes, request, mockContext);

      expect(result.valid).toBe(true); // Side effects are warnings, not errors
      expect(result.issues.length).toBeGreaterThan(0);
      expect(result.issues.some(issue => issue.code === 'SIDE_EFFECT')).toBe(true);
    });
  });

  describe('validateGrammarCompliance', () => {
    it('should detect grammar violations', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-5',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.py',
        language: SupportedLanguage.PYTHON,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.py',
        type: 'replace',
        position,
        oldText: 'x = 1',
        newText: 'x = 1; y = 2;', // Invalid Python (unnecessary semicolons)
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: ['x'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: ['x = 1'],
          addedElements: ['x = 1; y = 2;'],
          removedElements: ['x = 1'],
        },
      }];

      const result = await (surgicalRefactoring as any).validateGrammarCompliance(changes, request, mockContext);

      expect(result.valid).toBe(false);
      expect(result.issues).toHaveLength(1);
      expect(result.issues[0].code).toBe('GRAMMAR_VIOLATION');
      expect(result.issues[0].message).toContain('Semicolons not typically used in Python');
    });

    it('should validate grammar rule compliance', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-6',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'insert',
        position,
        oldText: '',
        newText: 'if (true) { return; }', // Not a variable declaration
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: [],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration', // Expects variable declaration
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: [],
          addedElements: ['if (true) { return; }'],
          removedElements: [],
        },
      }];

      const result = await (surgicalRefactoring as any).validateGrammarCompliance(changes, request, mockContext);

      expect(result.valid).toBe(true); // Rule noncompliance is warning, not error
      expect(result.issues.length).toBeGreaterThan(0);
      expect(result.issues[0].code).toBe('RULE_NONCOMPLIANCE');
      expect(result.issues[0].severity).toBe('warning');
    });
  });

  describe('validateTypeSystem', () => {
    it('should skip validation for non-typed languages', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-7',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT, // Non-typed language
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.js',
        type: 'replace',
        position,
        oldText: 'const x = "hello"',
        newText: 'const x = 42',
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: ['x'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: ['const x = "hello"'],
          addedElements: ['const x = 42'],
          removedElements: ['const x = "hello"'],
        },
      }];

      const result = await (surgicalRefactoring as any).validateTypeSystem(changes, request, mockContext);

      expect(result.valid).toBe(true);
      expect(result.issues).toHaveLength(0);
    });

    it('should detect type incompatibilities in typed languages', async () => {
      const mockContext: ParseContext = {
        id: 'test',
        scopeType: ScopeType.Global,
        symbols: new Map(),
        rules: new Map(),
        depth: 0,
        position: { line: 1, column: 1, offset: 0, length: 0 },
        metadata: new Map(),
      };

      const position: CodePosition = { line: 1, column: 1, offset: 0 };

      const request: SurgicalOperationRequest = {
        id: 'test-8',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.ts',
        language: SupportedLanguage.TYPESCRIPT, // Typed language
        position,
        parameters: {},
        options: {
          preview: false,
          dryRun: false,
          validateOnly: true,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: false,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2,
        },
      };

      const changes: SurgicalChange[] = [{
        id: 'change-1',
        file: 'test.ts',
        type: 'replace',
        position,
        oldText: 'const x: string = "hello"',
        newText: 'const x: number = 42',
        scope: {
          type: 'statement',
          startPosition: position,
          endPosition: position,
          contextType: 'variable_declaration',
          symbolsAffected: ['x'],
          referencesAffected: 0,
        },
        confidence: 0.9,
        dependencies: [],
        metadata: {
          grammarRule: 'variable_declaration',
          syntaxNode: 'declaration_statement',
          semanticContext: 'local_scope',
          preservedElements: [],
          modifiedElements: ['const x: string = "hello"'],
          addedElements: ['const x: number = 42'],
          removedElements: ['const x: string = "hello"'],
        },
      }];

      const result = await (surgicalRefactoring as any).validateTypeSystem(changes, request, mockContext);

      // Our implementation correctly detects string->number as a serious type incompatibility (error)
      expect(result.valid).toBe(false); // Should fail due to type error
      expect(result.issues.length).toBeGreaterThan(0);
      expect(result.issues[0].code).toBe('TYPE_MISMATCH');
      expect(result.issues[0].severity).toBe('error');
      expect(result.issues[0].message).toContain('Cannot replace string with number');
    });
  });

  describe('integration test', () => {
    it('should perform complete validation workflow', async () => {
      // Mock the context manager to return a context
      mockContextManager.getContextAt.mockReturnValue({
        position: { line: 1, column: 1, offset: 0 },
        scope: { type: 'GLOBAL' as any, depth: 0 },
      } as any);

      const request: SurgicalOperationRequest = {
        id: 'integration-test',
        type: SurgicalOperationType.EXTRACT_VARIABLE,
        file: 'test.js',
        language: SupportedLanguage.JAVASCRIPT,
        position: { line: 1, column: 1, offset: 0 },
        parameters: { symbolName: 'extractedVar' },
        options: {
          preview: true,
          dryRun: false,
          validateOnly: false,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: true,
          confidenceThreshold: 0.85,
          maxScopeExpansion: 3,
        },
      };

      const result = await surgicalRefactoring.executeSurgicalOperation(request);

      expect(result.success).toBe(true);
      expect(result.validation).toBeDefined();
      expect(result.validation.syntaxValid).toBe(true);
      expect(result.validation.semanticsValid).toBe(true);
      expect(result.validation.referencesValid).toBe(true);
      expect(result.validation.grammarCompliant).toBe(true);
    });
  });
});