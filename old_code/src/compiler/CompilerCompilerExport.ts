/**
 * Minotaur Compiler-Compiler Export System
 *
 * Interfaces and types for code generation with embedded language support
 */

import { Grammar } from '../core/grammar/Grammar';
import { CrossLanguageReference } from './EmbeddedLanguageContextManager';
import { CrossLanguageValidationResult } from './CrossLanguageValidator';

export interface ExportConfiguration {
    targetLanguage: string;
    outputDirectory: string;
    optimizationLevel: 'debug' | 'release' | 'production';
    buildSystemIntegration: boolean;
    generateTests: boolean;
    generateDocumentation: boolean;
    enableEmbeddedLanguages: boolean;
    enableContextSwitching: boolean;
    enableCrossLanguageValidation: boolean;
    enableSymbolTableSharing: boolean;
    contextSensitive?: {
        enabled: boolean;
        symbolTableGeneration: boolean;
        scopeAnalysis: boolean;
        contextValidation: boolean;
        optimizeForTarget: boolean;
        contextCount: number;
        inheritanceDepth: number;
    };

    // Language-specific options
    go?: GoOptions;
    rust?: RustOptions;
    webassembly?: WebAssemblyOptions;
    typescript?: TypeScriptOptions;
    cpp?: CppOptions;
    java?: JavaOptions;
    python?: PythonOptions;
    csharp?: CSharpOptions;
    swift?: SwiftOptions;
}

export interface GoOptions {
    version: string;
    enableConcurrency: boolean;
    enableGC: boolean;
    packageName: string;
    moduleName: string;
}

export interface RustOptions {
    edition: string;
    enableUnsafe: boolean;
    crateType: 'bin' | 'lib' | 'dylib' | 'staticlib';
    crateName: string;
}

export interface WebAssemblyOptions {
    target: 'web' | 'node' | 'standalone';
    optimizeSize: boolean;
    enableThreads: boolean;
    memorySize: number;
}

export interface TypeScriptOptions {
    target: string;
    module: string;
    strict: boolean;
    generateDeclarations: boolean;
}

export interface CppOptions {
    standard: string;
    enableExceptions: boolean;
    enableRTTI: boolean;
    optimizationLevel: string;
}

export interface JavaOptions {
    version: string;
    packageName: string;
    enableGenerics: boolean;
    enableAnnotations: boolean;
}

export interface PythonOptions {
    version: string;
    enableTypeHints: boolean;
    enableAsyncio: boolean;
}

export interface CSharpOptions {
    version: string;
    namespace: string;
    enableNullable: boolean;
    enableAsync: boolean;
}

export interface SwiftOptions {
    version: string;
    enableConcurrency: boolean;
    enableGenerics: boolean;
}

export interface GeneratedCode {
    success: boolean;
    sourceFiles: Map<string, string>;
    headerFiles: Map<string, string>;
    buildFiles: Map<string, string>;
    testFiles: Map<string, string>;
    documentationFiles: Map<string, string>;
    metadata: CodeGenerationMetadata;
    errors: CodeGenerationError[];
    warnings: CodeGenerationWarning[];
}

export interface CodeGenerationMetadata {
    generationTime: number;
    linesOfCode: number;
    filesGenerated: number;
    targetLanguage: string;
    embeddedLanguagesSupported: string[];
    contextSwitchesGenerated: number;
    crossLanguageReferencesGenerated: number;
    validationRulesGenerated: number;
    optimizationLevel: string;
    generatorVersion: string;
    grammarComplexity: GrammarComplexity;
    contextSensitiveEnhancements?: string[];
}

export interface GrammarComplexity {
    nonTerminals: number;
    terminals: number;
    productions: number;
    embeddedLanguages: number;
    crossLanguageReferences: number;
    validationRules: number;
    cyclomaticComplexity: number;
}

export interface CodeGenerationError {
    message: string;
    severity: 'error' | 'warning';
    component: string;
    suggestion: string;
    position?: { line: number; column: number };
}

export interface CodeGenerationWarning {
    message: string;
    component: string;
    suggestion: string;
    position?: { line: number; column: number };
}

export interface ContextAnalysisResult {
    contextRequired: boolean;
    symbols?: Map<string, any>;
    contexts?: any[];
    inheritanceChain?: string[];
    embeddedLanguages: string[];
    contextSwitches: ContextSwitchInfo[];
    crossLanguageReferences: CrossLanguageReference[];
    symbolTableSharing: boolean;
    validationRequired: boolean;
    complexity: ContextComplexity;
}

export interface ContextSwitchInfo {
    fromLanguage: string;
    toLanguage: string;
    trigger: string;
    endTrigger: string;
    position: { line: number; column: number };
    nestingLevel: number;
}

export interface ContextComplexity {
    maxNestingDepth: number;
    totalContextSwitches: number;
    uniqueLanguagePairs: number;
    cyclicReferences: boolean;
}

export abstract class CodeGenerator {
  protected grammar: Grammar;
  protected embeddedGrammars: Map<string, Grammar>;
  protected config: ExportConfiguration;
  protected contextInfo: ContextAnalysisResult;
  protected validationResult: CrossLanguageValidationResult | null;

  constructor(
    grammar: Grammar,
    embeddedGrammars: Map<string, Grammar> = new Map(),
    config: ExportConfiguration,
  ) {
    this.grammar = grammar;
    this.embeddedGrammars = embeddedGrammars;
    this.config = config;
    this.contextInfo = this.analyzeContext();
    this.validationResult = null;
  }

    /**
     * Generate code for the grammar with embedded language support
     */
    public abstract generate(
        grammar: Grammar,
        contextInfo: ContextAnalysisResult,
        config: ExportConfiguration
    ): Promise<GeneratedCode>;

    /**
     * Analyze context requirements for embedded languages
     */
    protected analyzeContext(): ContextAnalysisResult {
      const embeddedLanguages = Array.from(this.embeddedGrammars.keys());
      const contextSwitches: ContextSwitchInfo[] = [];
      const crossLanguageReferences: CrossLanguageReference[] = [];

      // Analyze grammar for context switches and cross-language references
      // This would involve parsing the grammar definitions

      return {
        contextRequired: embeddedLanguages.length > 0,
        embeddedLanguages: embeddedLanguages,
        contextSwitches: contextSwitches,
        crossLanguageReferences: crossLanguageReferences,
        symbolTableSharing: this.grammar.getSymbolTableSharing() === 'hierarchical',
        validationRequired: this.grammar.getCrossLanguageValidation(),
        complexity: {
          maxNestingDepth: this.calculateMaxNestingDepth(),
          totalContextSwitches: contextSwitches.length,
          uniqueLanguagePairs: this.calculateUniqueLanguagePairs(contextSwitches),
          cyclicReferences: this.detectCyclicReferences(crossLanguageReferences),
        },
      };
    }

    /**
     * Generate embedded language support code
     */
    protected generateEmbeddedLanguageSupport(): string {
      if (!this.config.enableEmbeddedLanguages || this.embeddedGrammars.size === 0) {
        return '';
      }

      let code = '// Embedded Language Support\n\n';

      // Generate context switching logic
      if (this.config.enableContextSwitching) {
        code += this.generateContextSwitchingCode();
      }

      // Generate cross-language validation
      if (this.config.enableCrossLanguageValidation) {
        code += this.generateCrossLanguageValidationCode();
      }

      // Generate symbol table sharing
      if (this.config.enableSymbolTableSharing) {
        code += this.generateSymbolTableSharingCode();
      }

      return code;
    }

    /**
     * Generate context switching code
     */
    protected abstract generateContextSwitchingCode(): string;

    /**
     * Generate cross-language validation code
     */
    protected abstract generateCrossLanguageValidationCode(): string;

    /**
     * Generate symbol table sharing code
     */
    protected abstract generateSymbolTableSharingCode(): string;

    /**
     * Generate parser for embedded language
     */
    protected abstract generateEmbeddedLanguageParser(language: string, grammar: Grammar): string;

    /**
     * Generate test cases for embedded language features
     */
    protected generateEmbeddedLanguageTests(): Map<string, string> {
      const testFiles = new Map<string, string>();

      if (!this.config.generateTests) {
        return testFiles;
      }

      // Generate context switching tests
      if (this.config.enableContextSwitching) {
        testFiles.set('context_switching_test', this.generateContextSwitchingTests());
      }

      // Generate cross-language validation tests
      if (this.config.enableCrossLanguageValidation) {
        testFiles.set('cross_language_validation_test', this.generateCrossLanguageValidationTests());
      }

      // Generate embedded language parser tests
      for (const [language, grammar] of this.embeddedGrammars) {
        testFiles.set(
          `${language.toLowerCase()}_parser_test`,
          this.generateEmbeddedLanguageParserTests(language, grammar),
        );
      }

      return testFiles;
    }

    /**
     * Generate context switching tests
     */
    protected abstract generateContextSwitchingTests(): string;

    /**
     * Generate cross-language validation tests
     */
    protected abstract generateCrossLanguageValidationTests(): string;

    /**
     * Generate embedded language parser tests
     */
    protected abstract generateEmbeddedLanguageParserTests(language: string, grammar: Grammar): string;

    /**
     * Calculate grammar complexity metrics
     */
    protected calculateGrammarComplexity(): GrammarComplexity {
      const productions = this.grammar.getProductions();
      const symbols = Array.from(this.grammar.getSymbols().values());
      const validationRules = this.grammar.getValidationRules();

      return {
        nonTerminals: symbols.filter(symbol => symbol.type === 'NonTerminal').length,
        terminals: symbols.filter(symbol => symbol.type === 'Terminal').length,
        productions: productions.length, // Changed from .size to .length since productions is an array
        embeddedLanguages: this.embeddedGrammars.size,
        crossLanguageReferences: this.contextInfo.crossLanguageReferences.length,
        validationRules: validationRules.length, // Changed from .size to .length since validationRules is an array
        cyclomaticComplexity: this.calculateCyclomaticComplexity(),
      };
    }

    /**
     * Calculate cyclomatic complexity of the grammar
     */
    protected calculateCyclomaticComplexity(): number {
      // Simplified cyclomatic complexity calculation
      const productions = this.grammar.getProductions();
      let complexity = 1; // Base complexity

      for (const production of productions.values()) {
        // Count decision points by parsing alternatives in rule definition
        // Split by "|" to count alternatives in the production rule
        const alternatives = production.definition.split('|').length;
        complexity += alternatives - 1;
      }

      return complexity;
    }

    /**
     * Calculate maximum nesting depth for embedded languages
     */
    protected calculateMaxNestingDepth(): number {
      // This would analyze the grammar for maximum nesting depth
      return this.embeddedGrammars.size > 0 ? 3 : 0; // Simplified
    }

    /**
     * Calculate unique language pairs for context switching
     */
    protected calculateUniqueLanguagePairs(contextSwitches: ContextSwitchInfo[]): number {
      const pairs = new Set<string>();

      for (const contextSwitch of contextSwitches) {
        pairs.add(`${contextSwitch.fromLanguage}->${contextSwitch.toLanguage}`);
      }

      return pairs.size;
    }

    /**
     * Detect cyclic references in cross-language dependencies
     */
    protected detectCyclicReferences(crossLanguageReferences: CrossLanguageReference[]): boolean {
      // Simplified cycle detection
      const dependencies = new Map<string, Set<string>>();

      for (const reference of crossLanguageReferences) {
        if (!dependencies.has(reference.sourceLanguage)) {
          dependencies.set(reference.sourceLanguage, new Set());
        }
            dependencies.get(reference.sourceLanguage)!.add(reference.targetLanguage);
      }

      // Check for cycles using DFS
      const visited = new Set<string>();
      const recursionStack = new Set<string>();

      for (const language of dependencies.keys()) {
        if (this.hasCycle(language, dependencies, visited, recursionStack)) {
          return true;
        }
      }

      return false;
    }

    /**
     * Helper method for cycle detection
     */
    private hasCycle(
      language: string,
      dependencies: Map<string, Set<string>>,
      visited: Set<string>,
      recursionStack: Set<string>,
    ): boolean {
      visited.add(language);
      recursionStack.add(language);

      const deps = dependencies.get(language);
      if (deps) {
        for (const dep of deps) {
          if (!visited.has(dep)) {
            if (this.hasCycle(dep, dependencies, visited, recursionStack)) {
              return true;
            }
          } else if (recursionStack.has(dep)) {
            return true;
          }
        }
      }

      recursionStack.delete(language);
      return false;
    }

    /**
     * Sanitize identifier for target language
     */
    protected sanitizeIdentifier(name: string): string {
      return name.replace(/[^a-zA-Z0-9_]/g, '_').replace(/^[0-9]/, '_$&');
    }

    /**
     * Generate file header comment
     */
    protected generateFileHeader(fileName: string, description: string): string {
      return `/**
 * ${fileName}
 * 
 * ${description}
 * 
 * Generated by Minotaur Compiler-Compiler
 * Grammar: ${this.grammar.getName()}
 * Target Language: ${this.config.targetLanguage}
 * Generation Time: ${new Date().toISOString()}
 * 
 * Embedded Languages: ${Array.from(this.embeddedGrammars.keys()).join(', ') || 'None'}
 * Context Switching: ${this.config.enableContextSwitching ? 'Enabled' : 'Disabled'}
 * Cross-Language Validation: ${this.config.enableCrossLanguageValidation ? 'Enabled' : 'Disabled'}
 * Symbol Table Sharing: ${this.config.enableSymbolTableSharing ? 'Enabled' : 'Disabled'}
 */

`;
    }

    /**
     * Generate build metadata
     */
    protected generateBuildMetadata(): CodeGenerationMetadata {
      return {
        generationTime: 0, // Will be set by caller
        linesOfCode: 0, // Will be calculated
        filesGenerated: 0, // Will be calculated
        targetLanguage: this.config.targetLanguage,
        embeddedLanguagesSupported: Array.from(this.embeddedGrammars.keys()),
        contextSwitchesGenerated: this.contextInfo.contextSwitches.length,
        crossLanguageReferencesGenerated: this.contextInfo.crossLanguageReferences.length,
        validationRulesGenerated: this.grammar.getValidationRules().length,
        optimizationLevel: this.config.optimizationLevel,
        generatorVersion: '1.0.0',
        grammarComplexity: this.calculateGrammarComplexity(),
      };
    }
}

// Add missing type definitions
export interface EmbeddedGrammar {
  name: string;
  grammar: Grammar;
  context: string;
}

export interface CompilerConfiguration {
  targetLanguage: string;
  outputDirectory: string;
  outputFormat: 'single_file' | 'multi_file' | 'modular';
  optimizationLevel: 'debug' | 'release' | 'production';
  buildSystemIntegration: boolean;
  generateTests: boolean;
  generateDocumentation: boolean;
  enableEmbeddedLanguages: boolean;
  enableContextSwitching: boolean;
  enableCrossLanguageValidation: boolean;
  enableSymbolTableSharing: boolean;
  targetLanguageOptions: { [key: string]: any };
}

/**
 * Factory for creating code generators
 */
export class CodeGeneratorFactory {
  private static async createCodeGenerator(
    targetLanguage: string,
    grammar: Grammar,
    embeddedGrammars: EmbeddedGrammar[],
    config: CompilerConfiguration,
  ): Promise<any> {
    // Convert EmbeddedGrammar[] to Map<string, Grammar> for constructor compatibility
    const embeddedGrammarMap = new Map<string, Grammar>();
    embeddedGrammars.forEach(embedded => {
      embeddedGrammarMap.set(embedded.name, embedded.grammar);
    });

    // Convert CompilerConfiguration to ExportConfiguration
    const exportConfig: ExportConfiguration = {
      targetLanguage: config.targetLanguage,
      outputDirectory: config.outputDirectory,
      optimizationLevel: config.optimizationLevel,
      buildSystemIntegration: config.buildSystemIntegration,
      generateTests: config.generateTests,
      generateDocumentation: config.generateDocumentation,
      enableEmbeddedLanguages: config.enableEmbeddedLanguages,
      enableContextSwitching: config.enableContextSwitching,
      enableCrossLanguageValidation: config.enableCrossLanguageValidation,
      enableSymbolTableSharing: config.enableSymbolTableSharing,
      // Add language-specific options from targetLanguageOptions
      ...(config.targetLanguageOptions || {}),
    };

    switch (targetLanguage.toLowerCase()) {
      case 'go': {
        const { GoCodeGenerator } = await import('./generators/GoCodeGenerator');
        return new GoCodeGenerator(grammar, embeddedGrammarMap, exportConfig);
      }
      case 'rust': {
        const { RustCodeGenerator } = await import('./generators/RustCodeGenerator');
        return new RustCodeGenerator(grammar, embeddedGrammarMap, exportConfig);
      }
      case 'webassembly':
      case 'wasm': {
        const { WebAssemblyCodeGenerator } = await import('./generators/WebAssemblyCodeGenerator');
        return new WebAssemblyCodeGenerator(grammar, embeddedGrammarMap, exportConfig);
      }
      case 'typescript':
      case 'ts': {
        const { TypeScriptEmbeddedLanguageCodeGenerator } = await import('./generators/EmbeddedLanguageCodeGenerator');
        // Convert ExportConfiguration to EmbeddedLanguageGenerationConfig
        const embeddedConfig = {
          targetLanguage: exportConfig.targetLanguage,
          outputFormat: config.outputFormat || 'multi_file' as const,
          enableContextSwitching: exportConfig.enableContextSwitching,
          enableCrossLanguageValidation: exportConfig.enableCrossLanguageValidation,
          enableSymbolTableSharing: exportConfig.enableSymbolTableSharing,
          optimizationLevel: exportConfig.optimizationLevel,
          generateTests: exportConfig.generateTests,
          generateDocumentation: exportConfig.generateDocumentation,
          targetLanguageOptions: config.targetLanguageOptions || {},
        };
        return new TypeScriptEmbeddedLanguageCodeGenerator(grammar, embeddedGrammarMap, embeddedConfig);
      }
      default:
        throw new Error(`Unsupported target language: ${targetLanguage}`);
    }
  }

  public static getSupportedLanguages(): string[] {
    return ['go', 'rust', 'webassembly', 'typescript', 'cpp', 'java', 'python', 'csharp', 'swift'];
  }
}

