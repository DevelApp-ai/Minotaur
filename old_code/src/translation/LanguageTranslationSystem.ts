/**
 * Language Translation System Architecture
 *
 * This system provides a comprehensive framework for translating between different
 * programming languages, with specific focus on ASP to C# translation.
 *
 * Key Features:
 * - Abstract syntax tree (AST) based translation
 * - Semantic mapping between language constructs
 * - Type system conversion
 * - Framework and library mapping
 * - Code style and convention adaptation
 * - Extensible architecture for multiple language pairs
 */

import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';
import { SyntacticValidator } from '../validation/SyntacticValidator';
import { Grammar } from '../core/grammar/Grammar';

// ============================================================================
// CORE TRANSLATION INTERFACES
// ============================================================================

/**
 * Represents a language translation capability
 */
export interface ILanguageTranslator {
    readonly sourceLanguage: string;
    readonly targetLanguage: string;
    readonly version: string;

    /**
     * Translates source code from one language to another
     */
    translate(sourceCode: string, options?: TranslationOptions): Promise<TranslationResult>;

    /**
     * Validates if the source code can be translated
     */
    canTranslate(sourceCode: string): boolean;

    /**
     * Gets translation capabilities and limitations
     */
    getCapabilities(): TranslationCapabilities;
}

/**
 * Translation configuration options
 */
export interface TranslationOptions {
    /** Target framework version (e.g., ".NET 6.0", ".NET Framework 4.8") */
    targetFramework?: string;

    /** Code style preferences */
    codeStyle?: CodeStyleOptions;

    /** Translation strategy */
    strategy?: TranslationStrategy;

    /** Include comments and documentation */
    preserveComments?: boolean;

    /** Generate migration notes */
    generateMigrationNotes?: boolean;

    /** Validation level */
    validationLevel?: 'strict' | 'balanced' | 'permissive';

    /** Custom type mappings */
    customTypeMappings?: Map<string, string>;

    /** Custom library mappings */
    customLibraryMappings?: Map<string, string>;
}

/**
 * Code style configuration
 */
export interface CodeStyleOptions {
    /** Naming convention (PascalCase, camelCase, snake_case) */
    namingConvention?: 'pascal' | 'camel' | 'snake';

    /** Indentation style */
    indentation?: 'spaces' | 'tabs';

    /** Number of spaces for indentation */
    indentSize?: number;

    /** Line ending style */
    lineEnding?: 'crlf' | 'lf';

    /** Maximum line length */
    maxLineLength?: number;

    /** Brace style */
    braceStyle?: 'allman' | 'k&r' | '1tbs';
}

/**
 * Translation strategy
 */
export enum TranslationStrategy {
    /** Direct 1:1 translation where possible */
    DIRECT = 'direct',

    /** Idiomatic translation using target language best practices */
    IDIOMATIC = 'idiomatic',

    /** Conservative translation preserving original structure */
    CONSERVATIVE = 'conservative',

    /** Modern translation using latest language features */
    MODERN = 'modern'
}

/**
 * Translation result
 */
export interface TranslationResult {
    /** Translated source code */
    translatedCode: string;

    /** Translation success status */
    success: boolean;

    /** Translation warnings */
    warnings: TranslationWarning[];

    /** Translation errors */
    errors: TranslationError[];

    /** Migration notes and recommendations */
    migrationNotes: MigrationNote[];

    /** Translation statistics */
    statistics: TranslationStatistics;

    /** Source map for debugging */
    sourceMap?: SourceMap;
}

/**
 * Translation warning
 */
export interface TranslationWarning {
    message: string;
    sourceLocation: SourceLocation;
    targetLocation?: SourceLocation;
    category: 'syntax' | 'semantic' | 'performance' | 'compatibility';
    severity: 'info' | 'warning' | 'error';
}

/**
 * Translation error
 */
export interface TranslationError {
    message: string;
    sourceLocation: SourceLocation;
    category: 'syntax' | 'semantic' | 'unsupported' | 'configuration';
    suggestions: string[];
}

/**
 * Migration note
 */
export interface MigrationNote {
    title: string;
    description: string;
    category: 'manual_review' | 'testing_required' | 'performance_impact' | 'breaking_change';
    priority: 'high' | 'medium' | 'low';
    sourceLocation?: SourceLocation;
    recommendations: string[];
}

/**
 * Source location information
 */
export interface SourceLocation {
    line: number;
    column: number;
    length?: number;
    fileName?: string;
}

/**
 * Translation statistics
 */
export interface TranslationStatistics {
    /** Total lines of source code */
    totalLines: number;

    /** Lines successfully translated */
    translatedLines: number;

    /** Translation coverage percentage */
    coverage: number;

    /** Number of functions/methods translated */
    functionsTranslated: number;

    /** Number of classes translated */
    classesTranslated: number;

    /** Translation time in milliseconds */
    translationTime: number;

    /** Memory usage in bytes */
    memoryUsage: number;
}

/**
 * Source map for debugging
 */
export interface SourceMap {
    version: number;
    sources: string[];
    mappings: string;
    names: string[];
}

/**
 * Translation capabilities
 */
export interface TranslationCapabilities {
    /** Supported language constructs */
    supportedConstructs: string[];

    /** Unsupported language constructs */
    unsupportedConstructs: string[];

    /** Supported frameworks */
    supportedFrameworks: string[];

    /** Type mapping capabilities */
    typeMappingSupport: boolean;

    /** Library mapping capabilities */
    libraryMappingSupport: boolean;

    /** Maximum file size supported */
    maxFileSize: number;

    /** Performance characteristics */
    performance: PerformanceCharacteristics;
}

/**
 * Performance characteristics
 */
export interface PerformanceCharacteristics {
    /** Average translation speed (lines per second) */
    averageSpeed: number;

    /** Memory usage per line */
    memoryPerLine: number;

    /** Scalability limits */
    scalabilityLimits: {
        maxLines: number;
        maxFunctions: number;
        maxClasses: number;
    };
}

// ============================================================================
// ABSTRACT TRANSLATION ENGINE
// ============================================================================

/**
 * Abstract base class for language translators
 */
export abstract class AbstractLanguageTranslator implements ILanguageTranslator {
  protected validator: SyntacticValidator;

  constructor(
        public readonly sourceLanguage: string,
        public readonly targetLanguage: string,
        public readonly version: string,
  ) {
    // Create a basic grammar for validation purposes
    const defaultGrammar = new Grammar(`${sourceLanguage}-${targetLanguage}-grammar`);
    this.validator = new SyntacticValidator(defaultGrammar);
  }

    abstract translate(sourceCode: string, options?: TranslationOptions): Promise<TranslationResult>;
    abstract canTranslate(sourceCode: string): boolean;
    abstract getCapabilities(): TranslationCapabilities;

    /**
     * Parses source code into AST
     */
    protected abstract parseSourceCode(sourceCode: string): ZeroCopyASTNode;

    /**
     * Transforms AST from source to target language
     */
    protected abstract transformAST(sourceAST: ZeroCopyASTNode, options?: TranslationOptions): ZeroCopyASTNode;

    /**
     * Generates target code from transformed AST
     */
    protected abstract generateTargetCode(targetAST: ZeroCopyASTNode, options?: TranslationOptions): string;

    /**
     * Validates the translation result
     */
    protected validateTranslation(
      sourceCode: string,
      targetCode: string,
      options?: TranslationOptions,
    ): TranslationWarning[] {
      const warnings: TranslationWarning[] = [];

      // Basic validation
      if (!targetCode || targetCode.trim().length === 0) {
        warnings.push({
          message: 'Translation resulted in empty code',
          sourceLocation: { line: 1, column: 1 },
          category: 'syntax',
          severity: 'error',
        });
      }

      // Use syntactic validator if available
      if (this.validator && options?.validationLevel !== 'permissive') {
        try {
          // Validate target code syntax using AST manipulation validation
          // Since the SyntacticValidator validates AST manipulations, we'll do basic validation here
          if (!targetCode.trim()) {
            warnings.push({
              message: 'Translated code is empty',
              sourceLocation: { line: 1, column: 1 },
              category: 'syntax',
              severity: 'error',
            });
          }

          // Basic syntax checks that can be done without full AST parsing
          if (this.hasBasicSyntaxErrors(targetCode)) {
            warnings.push({
              message: 'Basic syntax validation failed',
              sourceLocation: { line: 1, column: 1 },
              category: 'syntax',
              severity: 'warning',
            });
          }
        } catch (error) {
          warnings.push({
            message: `Validation error: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : 'Unknown error'}`,
            sourceLocation: { line: 1, column: 1 },
            category: 'syntax',
            severity: 'warning',
          });
        }
      }

      return warnings;
    }

    /**
     * Generates migration notes
     */
    protected generateMigrationNotes(
      sourceAST: ZeroCopyASTNode,
      targetAST: ZeroCopyASTNode,
      options?: TranslationOptions,
    ): MigrationNote[] {
      const notes: MigrationNote[] = [];

      // Add framework-specific migration notes
      if (options?.targetFramework) {
        notes.push({
          title: 'Framework Migration',
          description: `Code has been translated to target ${options.targetFramework}`,
          category: 'manual_review',
          priority: 'medium',
          recommendations: [
            'Review framework-specific APIs and dependencies',
            'Test thoroughly in target environment',
            'Update project configuration files',
          ],
        });
      }

      return notes;
    }

    /**
     * Calculates translation statistics
     */
    protected calculateStatistics(
      sourceCode: string,
      targetCode: string,
      startTime: number,
      memoryUsage: number,
    ): TranslationStatistics {
      const sourceLines = sourceCode.split('\n').length;
      const targetLines = targetCode.split('\n').length;

      return {
        totalLines: sourceLines,
        translatedLines: targetLines,
        coverage: targetLines > 0 ? 100 : 0,
        functionsTranslated: this.countFunctions(targetCode),
        classesTranslated: this.countClasses(targetCode),
        translationTime: Date.now() - startTime,
        memoryUsage: memoryUsage,
      };
    }

    /**
     * Counts functions in code (basic implementation)
     */
    protected countFunctions(code: string): number {
      // Basic regex-based counting - should be overridden by specific translators
      const functionMatches = code.match(/function\s+\w+|def\s+\w+|public\s+\w+\s+\w+\s*\(/g);
      return functionMatches ? functionMatches.length : 0;
    }

    /**
     * Counts classes in code (basic implementation)
     */
    protected countClasses(code: string): number {
      // Basic regex-based counting - should be overridden by specific translators
      const classMatches = code.match(/class\s+\w+|public\s+class\s+\w+/g);
      return classMatches ? classMatches.length : 0;
    }

    /**
     * Performs basic syntax validation on translated code
     */
    protected hasBasicSyntaxErrors(code: string): boolean {
      // Basic checks for common syntax issues
      const openBraces = (code.match(/\{/g) || []).length;
      const closeBraces = (code.match(/\}/g) || []).length;
      const openParens = (code.match(/\(/g) || []).length;
      const closeParens = (code.match(/\)/g) || []).length;

      // Check for mismatched braces and parentheses
      return openBraces !== closeBraces || openParens !== closeParens;
    }
}

// ============================================================================
// SEMANTIC MAPPING SYSTEM
// ============================================================================

/**
 * Manages semantic mappings between language constructs
 */
export class SemanticMappingEngine {
  private typeMappings: Map<string, TypeMapping> = new Map();
  private libraryMappings: Map<string, LibraryMapping> = new Map();
  private patternMappings: Map<string, PatternMapping> = new Map();

  /**
     * Adds a type mapping
     */
  addTypeMapping(sourceType: string, mapping: TypeMapping): void {
    this.typeMappings.set(sourceType, mapping);
  }

  /**
     * Gets type mapping
     */
  getTypeMapping(sourceType: string): TypeMapping | undefined {
    return this.typeMappings.get(sourceType);
  }

  /**
     * Adds a library mapping
     */
  addLibraryMapping(sourceLibrary: string, mapping: LibraryMapping): void {
    this.libraryMappings.set(sourceLibrary, mapping);
  }

  /**
     * Gets library mapping
     */
  getLibraryMapping(sourceLibrary: string): LibraryMapping | undefined {
    return this.libraryMappings.get(sourceLibrary);
  }

  /**
     * Adds a pattern mapping
     */
  addPatternMapping(sourcePattern: string, mapping: PatternMapping): void {
    this.patternMappings.set(sourcePattern, mapping);
  }

  /**
     * Gets pattern mapping
     */
  getPatternMapping(sourcePattern: string): PatternMapping | undefined {
    return this.patternMappings.get(sourcePattern);
  }
}

/**
 * Type mapping definition
 */
export interface TypeMapping {
    /** Target type name */
    targetType: string;

    /** Namespace or module for target type */
    targetNamespace?: string;

    /** Required imports/using statements */
    requiredImports?: string[];

    /** Conversion function if needed */
    conversionFunction?: string;

    /** Notes about the mapping */
    notes?: string;

    /** Compatibility level */
    compatibility: 'exact' | 'similar' | 'approximate' | 'manual';
}

/**
 * Library mapping definition
 */
export interface LibraryMapping {
    /** Target library name */
    targetLibrary: string;

    /** Target library version */
    targetVersion?: string;

    /** Package manager information */
    packageInfo?: {
        packageManager: 'npm' | 'nuget' | 'pip' | 'maven' | 'cargo';
        packageName: string;
        version?: string;
    };

    /** API mappings */
    apiMappings?: Map<string, string>;

    /** Migration notes */
    migrationNotes?: string[];

    /** Compatibility level */
    compatibility: 'exact' | 'similar' | 'approximate' | 'manual';
}

/**
 * Pattern mapping definition
 */
export interface PatternMapping {
    /** Target pattern template */
    targetPattern: string;

    /** Pattern variables */
    variables?: Map<string, string>;

    /** Transformation function */
    transform?: (match: RegExpMatchArray) => string;

    /** Notes about the pattern */
    notes?: string;

    /** Examples */
    examples?: {
        source: string;
        target: string;
    }[];
}

// ============================================================================
// TRANSLATION REGISTRY
// ============================================================================

/**
 * Registry for managing language translators
 */
export class TranslationRegistry {
  private translators: Map<string, ILanguageTranslator> = new Map();

  /**
     * Registers a translator
     */
  register(translator: ILanguageTranslator): void {
    const key = `${translator.sourceLanguage}->${translator.targetLanguage}`;
    this.translators.set(key, translator);
  }

  /**
     * Gets a translator for the specified language pair
     */
  getTranslator(sourceLanguage: string, targetLanguage: string): ILanguageTranslator | undefined {
    const key = `${sourceLanguage}->${targetLanguage}`;
    return this.translators.get(key);
  }

  /**
     * Gets all available translators
     */
  getAllTranslators(): ILanguageTranslator[] {
    return Array.from(this.translators.values());
  }

  /**
     * Gets supported source languages
     */
  getSupportedSourceLanguages(): string[] {
    const languages = new Set<string>();
    for (const translator of this.translators.values()) {
      languages.add(translator.sourceLanguage);
    }
    return Array.from(languages);
  }

  /**
     * Gets supported target languages for a source language
     */
  getSupportedTargetLanguages(sourceLanguage: string): string[] {
    const languages = new Set<string>();
    for (const translator of this.translators.values()) {
      if (translator.sourceLanguage === sourceLanguage) {
        languages.add(translator.targetLanguage);
      }
    }
    return Array.from(languages);
  }

  /**
     * Checks if translation is supported
     */
  isTranslationSupported(sourceLanguage: string, targetLanguage: string): boolean {
    return this.getTranslator(sourceLanguage, targetLanguage) !== undefined;
  }
}

// ============================================================================
// TRANSLATION PIPELINE
// ============================================================================

/**
 * Translation pipeline for processing multiple files
 */
export class TranslationPipeline {
  private registry: TranslationRegistry;
  private preprocessors: TranslationPreprocessor[] = [];
  private postprocessors: TranslationPostprocessor[] = [];

  constructor(registry: TranslationRegistry) {
    this.registry = registry;
  }

  /**
     * Adds a preprocessor
     */
  addPreprocessor(preprocessor: TranslationPreprocessor): void {
    this.preprocessors.push(preprocessor);
  }

  /**
     * Adds a postprocessor
     */
  addPostprocessor(postprocessor: TranslationPostprocessor): void {
    this.postprocessors.push(postprocessor);
  }

  /**
     * Processes a batch of files
     */
  async processBatch(
    files: TranslationFile[],
    sourceLanguage: string,
    targetLanguage: string,
    options?: TranslationOptions,
  ): Promise<BatchTranslationResult> {
    const translator = this.registry.getTranslator(sourceLanguage, targetLanguage);
    if (!translator) {
      throw new Error(`No translator available for ${sourceLanguage} -> ${targetLanguage}`);
    }

    const results: TranslationResult[] = [];
    const errors: BatchTranslationError[] = [];

    for (const file of files) {
      try {
        // Preprocess
        let content = file.content;
        for (const preprocessor of this.preprocessors) {
          content = await preprocessor.process(content, file.fileName, options);
        }

        // Translate
        const result = await translator.translate(content, options);

        // Postprocess
        let translatedCode = result.translatedCode;
        for (const postprocessor of this.postprocessors) {
          translatedCode = await postprocessor.process(translatedCode, file.fileName, options);
        }

        results.push({
          ...result,
          translatedCode,
        });

      } catch (error) {
        errors.push({
          fileName: file.fileName,
          error: error instanceof Error ? error.message : 'Unknown error',
        });
      }
    }

    return {
      results,
      errors,
      summary: this.generateBatchSummary(results, errors),
    };
  }

  private generateBatchSummary(results: TranslationResult[], errors: BatchTranslationError[]): BatchSummary {
    const totalFiles = results.length + errors.length;
    const successfulFiles = results.length;
    const failedFiles = errors.length;

    const totalWarnings = results.reduce((sum, result) => sum + result.warnings.length, 0);
    const totalErrors = results.reduce((sum, result) => sum + result.errors.length, 0);

    return {
      totalFiles,
      successfulFiles,
      failedFiles,
      successRate: totalFiles > 0 ? (successfulFiles / totalFiles) * 100 : 0,
      totalWarnings,
      totalErrors,
    };
  }
}

/**
 * Translation file input
 */
export interface TranslationFile {
    fileName: string;
    content: string;
    metadata?: Record<string, any>;
}

/**
 * Batch translation result
 */
export interface BatchTranslationResult {
    results: TranslationResult[];
    errors: BatchTranslationError[];
    summary: BatchSummary;
}

/**
 * Batch translation error
 */
export interface BatchTranslationError {
    fileName: string;
    error: string;
}

/**
 * Batch summary
 */
export interface BatchSummary {
    totalFiles: number;
    successfulFiles: number;
    failedFiles: number;
    successRate: number;
    totalWarnings: number;
    totalErrors: number;
}

/**
 * Translation preprocessor interface
 */
export interface TranslationPreprocessor {
    process(content: string, fileName: string, options?: TranslationOptions): Promise<string>;
}

/**
 * Translation postprocessor interface
 */
export interface TranslationPostprocessor {
    process(content: string, fileName: string, options?: TranslationOptions): Promise<string>;
}

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Creates a default translation registry with common translators
 */
export function createDefaultTranslationRegistry(): TranslationRegistry {
  const registry = new TranslationRegistry();

  // Translators will be registered here as they are implemented
  // registry.register(new AspToCSharpTranslator());
  // registry.register(new VbScriptToCSharpTranslator());
  // registry.register(new JScriptToTypeScriptTranslator());

  return registry;
}

/**
 * Creates a default semantic mapping engine
 */
export function createDefaultSemanticMappingEngine(): SemanticMappingEngine {
  const engine = new SemanticMappingEngine();

  // Add common type mappings
  engine.addTypeMapping('String', {
    targetType: 'string',
    compatibility: 'exact',
  });

  engine.addTypeMapping('Integer', {
    targetType: 'int',
    compatibility: 'exact',
  });

  engine.addTypeMapping('Boolean', {
    targetType: 'bool',
    compatibility: 'exact',
  });

  return engine;
}

/**
 * Validates translation options
 */
export function validateTranslationOptions(options: TranslationOptions): string[] {
  const errors: string[] = [];

  if (options.targetFramework && !isValidFramework(options.targetFramework)) {
    errors.push(`Invalid target framework: ${options.targetFramework}`);
  }

  if (options.codeStyle?.indentSize && (options.codeStyle.indentSize < 1 || options.codeStyle.indentSize > 8)) {
    errors.push('Indent size must be between 1 and 8');
  }

  if (options.codeStyle?.maxLineLength && options.codeStyle.maxLineLength < 80) {
    errors.push('Maximum line length must be at least 80 characters');
  }

  return errors;
}

/**
 * Checks if a framework identifier is valid
 */
function isValidFramework(framework: string): boolean {
  const validFrameworks = [
    '.NET 6.0', '.NET 7.0', '.NET 8.0',
    '.NET Framework 4.8', '.NET Framework 4.7.2',
    '.NET Core 3.1', '.NET 5.0',
  ];

  return validFrameworks.includes(framework);
}

/**
 * Default translation options
 */
export const DEFAULT_TRANSLATION_OPTIONS: TranslationOptions = {
  targetFramework: '.NET 8.0',
  strategy: TranslationStrategy.IDIOMATIC,
  preserveComments: true,
  generateMigrationNotes: true,
  validationLevel: 'balanced',
  codeStyle: {
    namingConvention: 'pascal',
    indentation: 'spaces',
    indentSize: 4,
    lineEnding: 'crlf',
    maxLineLength: 120,
    braceStyle: 'allman',
  },
};

