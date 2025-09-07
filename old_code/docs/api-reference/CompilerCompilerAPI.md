# Minotaur Compiler-Compiler API Reference

This document provides a comprehensive reference for the Minotaur compiler-compiler API, including all classes, interfaces, methods, and configuration options.

## Table of Contents

1. [Core Classes](#core-classes)
2. [Configuration Interfaces](#configuration-interfaces)
3. [Result Types](#result-types)
4. [Code Generation](#code-generation)
5. [Context-Sensitive Parsing](#context-sensitive-parsing)
6. [Performance Benchmarking](#performance-benchmarking)
7. [Testing Framework](#testing-framework)
8. [Error Handling](#error-handling)
9. [Utilities](#utilities)

## Core Classes

### CompilerCompilerExport

The main class for exporting grammars to target languages.

```typescript
class CompilerCompilerExport {
    constructor(
        grammarContainer: GrammarContainer,
        contextEngine: ContextSensitiveEngine,
        performanceBenchmark: PerformanceBenchmark
    );

    /**
     * Export a grammar to the specified target language
     */
    async exportGrammar(
        grammar: Grammar,
        config: ExportConfiguration
    ): Promise<ExportResult>;

    /**
     * Get supported target languages
     */
    getSupportedLanguages(): string[];

    /**
     * Get language capabilities
     */
    getLanguageCapabilities(language: string): LanguageCapabilities;

    /**
     * Validate export configuration
     */
    validateConfiguration(config: ExportConfiguration): ValidationResult;

    /**
     * Get export statistics
     */
    getExportStatistics(): ExportStatistics;
}
```

### CodeGenerationPipeline

Handles the multi-stage code generation process.

```typescript
class CodeGenerationPipeline {
    constructor(templateSystem: TemplateSystem);

    /**
     * Generate code for the specified target language
     */
    async generateCode(
        ir: IntermediateRepresentation,
        config: ExportConfiguration
    ): Promise<GeneratedCode>;

    /**
     * Optimize intermediate representation
     */
    optimizeIR(
        ir: IntermediateRepresentation,
        optimizationLevel: OptimizationLevel
    ): IntermediateRepresentation;

    /**
     * Validate generated code
     */
    validateGeneratedCode(
        code: GeneratedCode,
        language: string
    ): ValidationResult;
}
```

### TemplateSystem

Manages code generation templates for different target languages.

```typescript
class TemplateSystem {
    /**
     * Register a template for a target language
     */
    registerTemplate(
        language: string,
        component: string,
        template: Template
    ): void;

    /**
     * Render a template with the given context
     */
    renderTemplate(
        language: string,
        component: string,
        context: TemplateContext
    ): string;

    /**
     * Get available templates for a language
     */
    getAvailableTemplates(language: string): string[];

    /**
     * Validate template syntax
     */
    validateTemplate(template: Template): ValidationResult;
}
```

## Configuration Interfaces

### ExportConfiguration

Main configuration interface for grammar export.

```typescript
interface ExportConfiguration {
    /** Target programming language */
    targetLanguage: string;

    /** Optimization level (0-3) */
    optimizationLevel: OptimizationLevel;

    /** Enable context-sensitive parsing */
    enableContextSensitive: boolean;

    /** Enable grammar inheritance features */
    enableInheritance: boolean;

    /** Generate test suite */
    generateTests: boolean;

    /** Generate documentation */
    generateDocumentation: boolean;

    /** Output directory for generated files */
    outputDirectory: string;

    /** Custom template overrides */
    templateOverrides?: TemplateOverrides;

    /** Language-specific options */
    languageOptions?: LanguageOptions;

    /** Performance profile */
    performanceProfile?: PerformanceProfile;

    /** Context-sensitive options */
    contextSensitiveOptions?: ContextSensitiveOptions;

    /** Inheritance options */
    inheritanceOptions?: InheritanceOptions;
}
```

### LanguageOptions

Language-specific configuration options.

```typescript
interface LanguageOptions {
    /** C/C++ specific options */
    c?: {
        standard: 'c99' | 'c11' | 'c17';
        enableSIMD: boolean;
        memoryAlignment: number;
        useInlineAssembly: boolean;
    };

    cpp?: {
        standard: 'cpp11' | 'cpp14' | 'cpp17' | 'cpp20';
        enableTemplateMetaprogramming: boolean;
        useConstexpr: boolean;
        enableCoroutines: boolean;
    };

    /** Java specific options */
    java?: {
        version: '8' | '11' | '17' | '21';
        enableRecords: boolean;
        useVarHandles: boolean;
        generateModuleInfo: boolean;
    };

    /** C# specific options */
    csharp?: {
        version: '7.0' | '8.0' | '9.0' | '10.0' | '11.0';
        enableNullableReferenceTypes: boolean;
        useRecords: boolean;
        enableUnsafeCode: boolean;
    };

    /** Python specific options */
    python?: {
        version: '3.7' | '3.8' | '3.9' | '3.10' | '3.11';
        useTypeHints: boolean;
        enableDataclasses: boolean;
        useSlots: boolean;
    };

    /** JavaScript specific options */
    javascript?: {
        target: 'es2018' | 'es2019' | 'es2020' | 'es2021' | 'es2022';
        generateTypeScript: boolean;
        useModules: boolean;
        enableWorkers: boolean;
    };

    /** Rust specific options */
    rust?: {
        edition: '2018' | '2021';
        enableSIMD: boolean;
        useNoStd: boolean;
        enableAsync: boolean;
    };

    /** Go specific options */
    go?: {
        version: '1.18' | '1.19' | '1.20' | '1.21';
        enableGenerics: boolean;
        useChannels: boolean;
        enableCGO: boolean;
    };

    /** WebAssembly specific options */
    webassembly?: {
        target: 'wasm32' | 'wasm64';
        enableSIMD: boolean;
        useLinearMemory: boolean;
        generateJSBindings: boolean;
    };
}
```

### ContextSensitiveOptions

Configuration for context-sensitive parsing features.

```typescript
interface ContextSensitiveOptions {
    /** Maximum context nesting depth */
    maxContextDepth: number;

    /** Enable symbol table tracking */
    enableSymbolTable: boolean;

    /** Enable scope-based context tracking */
    enableScopeTracking: boolean;

    /** Optimize context switching performance */
    optimizeContextSwitching: boolean;

    /** Context cache size for performance */
    contextCacheSize: number;

    /** Context resolution strategy */
    resolutionStrategy: 'eager' | 'lazy' | 'hybrid';

    /** Enable context inheritance */
    enableContextInheritance: boolean;

    /** Context validation level */
    validationLevel: 'none' | 'basic' | 'strict';
}
```

### InheritanceOptions

Configuration for grammar inheritance features.

```typescript
interface InheritanceOptions {
    /** Maximum inheritance chain depth */
    maxInheritanceDepth: number;

    /** Enable multiple inheritance */
    enableMultipleInheritance: boolean;

    /** Conflict resolution strategy */
    resolveConflicts: 'priority' | 'merge' | 'error';

    /** Optimize inherited rules */
    optimizeInheritedRules: boolean;

    /** Generate inheritance documentation */
    generateInheritanceMap: boolean;

    /** Inheritance validation level */
    validationLevel: 'none' | 'basic' | 'strict';

    /** Enable inheritance caching */
    enableCaching: boolean;
}
```

## Result Types

### ExportResult

Result of a grammar export operation.

```typescript
interface ExportResult {
    /** Whether the export was successful */
    success: boolean;

    /** Generated source files */
    sourceFiles?: Map<string, string>;

    /** Generated header files */
    headerFiles?: Map<string, string>;

    /** Generated build files */
    buildFiles?: Map<string, string>;

    /** Generated test files */
    testFiles?: Map<string, string>;

    /** Generated documentation files */
    documentationFiles?: Map<string, string>;

    /** Export statistics */
    statistics: ExportStatistics;

    /** Performance metrics */
    performance: PerformanceMetrics;

    /** Validation results */
    validation: ValidationResult[];

    /** Error messages */
    errors: string[];

    /** Warning messages */
    warnings: string[];

    /** Export metadata */
    metadata: ExportMetadata;
}
```

### ExportStatistics

Statistics about the export process.

```typescript
interface ExportStatistics {
    /** Total export time in milliseconds */
    totalTime: number;

    /** Grammar parsing time */
    parseTime: number;

    /** IR generation time */
    irGenerationTime: number;

    /** Optimization time */
    optimizationTime: number;

    /** Code generation time */
    codeGenerationTime: number;

    /** Validation time */
    validationTime: number;

    /** Number of grammar rules processed */
    rulesProcessed: number;

    /** Number of tokens processed */
    tokensProcessed: number;

    /** Generated code size in bytes */
    generatedCodeSize: number;

    /** Number of optimizations applied */
    optimizationsApplied: number;

    /** Memory usage during export */
    memoryUsage: number;
}
```

### PerformanceMetrics

Performance metrics for the generated parser.

```typescript
interface PerformanceMetrics {
    /** Estimated parsing speed (tokens/second) */
    estimatedParseSpeed: number;

    /** Estimated memory usage (bytes) */
    estimatedMemoryUsage: number;

    /** Code complexity metrics */
    complexity: ComplexityMetrics;

    /** Optimization effectiveness */
    optimizationEffectiveness: number;

    /** Performance score (0-100) */
    performanceScore: number;

    /** Benchmark results */
    benchmarkResults?: BenchmarkResult[];
}
```

## Code Generation

### IntermediateRepresentation

Intermediate representation of the grammar.

```typescript
interface IntermediateRepresentation {
    /** Grammar metadata */
    metadata: GrammarMetadata;

    /** Production rules */
    productions: ProductionRule[];

    /** Lexical rules */
    lexicalRules: LexicalRule[];

    /** Parse states */
    parseStates: ParseState[];

    /** Symbol table */
    symbolTable: SymbolTable;

    /** Context information */
    contextInfo: ContextInfo;

    /** Inheritance information */
    inheritanceInfo: InheritanceInfo;

    /** Optimization hints */
    optimizationHints: OptimizationHint[];
}
```

### ProductionRule

Represents a grammar production rule.

```typescript
interface ProductionRule {
    /** Rule name */
    name: string;

    /** Rule alternatives */
    alternatives: RuleAlternative[];

    /** Rule attributes */
    attributes: RuleAttributes;

    /** Semantic actions */
    semanticActions: SemanticAction[];

    /** Precedence information */
    precedence: PrecedenceInfo;

    /** Context requirements */
    contextRequirements: ContextRequirement[];

    /** Inheritance information */
    inheritanceInfo: RuleInheritanceInfo;
}
```

### GeneratedCode

Represents generated code for a target language.

```typescript
interface GeneratedCode {
    /** Target language */
    language: string;

    /** Generated files */
    files: GeneratedFile[];

    /** Build configuration */
    buildConfig: BuildConfiguration;

    /** Dependencies */
    dependencies: Dependency[];

    /** Code metrics */
    metrics: CodeMetrics;

    /** Validation results */
    validation: ValidationResult[];
}
```

## Context-Sensitive Parsing

### ContextSensitiveEngine

Engine for handling context-sensitive parsing.

```typescript
class ContextSensitiveEngine {
    constructor(options: ContextSensitiveOptions);

    /**
     * Analyze context requirements for a grammar
     */
    analyzeContextRequirements(grammar: Grammar): ContextAnalysis;

    /**
     * Generate context-sensitive parsing code
     */
    generateContextCode(
        ir: IntermediateRepresentation,
        language: string
    ): ContextCode;

    /**
     * Optimize context switching
     */
    optimizeContextSwitching(
        contextCode: ContextCode,
        optimizationLevel: OptimizationLevel
    ): ContextCode;

    /**
     * Validate context-sensitive features
     */
    validateContextFeatures(
        grammar: Grammar,
        options: ContextSensitiveOptions
    ): ValidationResult;
}
```

### ContextInfo

Information about context-sensitive parsing requirements.

```typescript
interface ContextInfo {
    /** Context types used */
    contextTypes: ContextType[];

    /** Context transitions */
    transitions: ContextTransition[];

    /** Symbol table requirements */
    symbolTableRequirements: SymbolTableRequirement[];

    /** Scope tracking requirements */
    scopeRequirements: ScopeRequirement[];

    /** Context validation rules */
    validationRules: ContextValidationRule[];
}
```

## Performance Benchmarking

### PerformanceBenchmark

Class for benchmarking parser performance.

```typescript
class PerformanceBenchmark {
    constructor(config: BenchmarkConfiguration);

    /**
     * Run performance benchmark
     */
    async runBenchmark(
        grammar: Grammar,
        targetLanguage: string,
        testInputs: TestInput[]
    ): Promise<BenchmarkResult>;

    /**
     * Compare with other parser generators
     */
    async compareWithOthers(
        grammar: Grammar,
        targetLanguage: string,
        competitors: string[]
    ): Promise<ComparisonResult>;

    /**
     * Generate performance report
     */
    generateReport(
        results: BenchmarkResult[],
        format: 'html' | 'json' | 'csv'
    ): string;
}
```

### BenchmarkResult

Result of a performance benchmark.

```typescript
interface BenchmarkResult {
    /** Target language */
    language: string;

    /** Parse time metrics */
    parseTime: TimeMetrics;

    /** Memory usage metrics */
    memoryUsage: MemoryMetrics;

    /** Throughput metrics */
    throughput: ThroughputMetrics;

    /** Code size metrics */
    codeSize: CodeSizeMetrics;

    /** Compilation time */
    compilationTime: number;

    /** Test configuration */
    testConfiguration: TestConfiguration;

    /** Environment information */
    environment: EnvironmentInfo;
}
```

## Testing Framework

### CompilerTestFramework

Framework for testing generated parsers.

```typescript
class CompilerTestFramework {
    constructor(
        compilerExport: CompilerCompilerExport,
        contextEngine: ContextSensitiveEngine,
        performanceBenchmark: PerformanceBenchmark
    );

    /**
     * Run comprehensive test suite
     */
    async runTests(config: TestConfiguration): Promise<TestReport>;

    /**
     * Run specific test case
     */
    async runTestCase(
        testCase: TestCase,
        targetLanguage: string,
        config: TestConfiguration
    ): Promise<TestResult>;

    /**
     * Generate test report
     */
    generateTestReport(
        config: TestConfiguration,
        results: TestResult[],
        totalTime: number
    ): Promise<TestReport>;

    /**
     * Export test results
     */
    exportTestReport(
        report: TestReport,
        format: 'json' | 'html' | 'junit'
    ): string;
}
```

### TestConfiguration

Configuration for test execution.

```typescript
interface TestConfiguration {
    /** Test name */
    name: string;

    /** Test description */
    description: string;

    /** Target languages to test */
    targetLanguages: string[];

    /** Test suites to run */
    testSuites: TestSuite[];

    /** Validation level */
    validationLevel: 'basic' | 'standard' | 'comprehensive' | 'exhaustive';

    /** Enable performance tests */
    enablePerformanceTests: boolean;

    /** Enable compilation tests */
    enableCompilationTests: boolean;

    /** Enable runtime tests */
    enableRuntimeTests: boolean;

    /** Enable cross-language tests */
    enableCrossLanguageTests: boolean;

    /** Output directory */
    outputDirectory: string;

    /** Timeout in milliseconds */
    timeoutMs: number;

    /** Maximum memory usage in MB */
    maxMemoryMB: number;
}
```

## Error Handling

### ValidationResult

Result of a validation operation.

```typescript
interface ValidationResult {
    /** Whether validation passed */
    passed: boolean;

    /** Validation rule that was applied */
    rule: string;

    /** Validation message */
    message: string;

    /** Severity level */
    severity: 'info' | 'warning' | 'error' | 'critical';

    /** Additional details */
    details?: any;

    /** Source location (if applicable) */
    location?: SourceLocation;

    /** Suggested fixes */
    suggestions?: string[];
}
```

### CompilerError

Error thrown during compilation.

```typescript
class CompilerError extends Error {
    constructor(
        message: string,
        code: string,
        location?: SourceLocation,
        suggestions?: string[]
    );

    /** Error code */
    readonly code: string;

    /** Source location */
    readonly location?: SourceLocation;

    /** Suggested fixes */
    readonly suggestions: string[];

    /** Error category */
    readonly category: ErrorCategory;
}
```

### ErrorCategory

Categories of compilation errors.

```typescript
enum ErrorCategory {
    GRAMMAR_SYNTAX = 'grammar_syntax',
    SEMANTIC_ANALYSIS = 'semantic_analysis',
    CODE_GENERATION = 'code_generation',
    OPTIMIZATION = 'optimization',
    VALIDATION = 'validation',
    CONFIGURATION = 'configuration',
    SYSTEM = 'system'
}
```

## Utilities

### GrammarAnalyzer

Utility for analyzing grammar properties.

```typescript
class GrammarAnalyzer {
    /**
     * Analyze grammar complexity
     */
    static analyzeComplexity(grammar: Grammar): ComplexityAnalysis;

    /**
     * Detect context-sensitive features
     */
    static detectContextSensitiveFeatures(grammar: Grammar): ContextFeature[];

    /**
     * Analyze inheritance relationships
     */
    static analyzeInheritance(grammar: Grammar): InheritanceAnalysis;

    /**
     * Estimate performance characteristics
     */
    static estimatePerformance(grammar: Grammar): PerformanceEstimate;

    /**
     * Validate grammar for target language
     */
    static validateForLanguage(
        grammar: Grammar,
        language: string
    ): ValidationResult[];
}
```

### CodeOptimizer

Utility for optimizing generated code.

```typescript
class CodeOptimizer {
    /**
     * Apply optimization passes
     */
    static optimize(
        code: GeneratedCode,
        level: OptimizationLevel,
        language: string
    ): GeneratedCode;

    /**
     * Analyze optimization opportunities
     */
    static analyzeOptimizations(
        ir: IntermediateRepresentation,
        language: string
    ): OptimizationOpportunity[];

    /**
     * Apply specific optimization
     */
    static applyOptimization(
        code: GeneratedCode,
        optimization: OptimizationPass
    ): GeneratedCode;
}
```

## Type Definitions

### Basic Types

```typescript
type OptimizationLevel = 0 | 1 | 2 | 3;

type PerformanceProfile = 'memory' | 'speed' | 'balanced' | 'size';

interface SourceLocation {
    file: string;
    line: number;
    column: number;
    length?: number;
}

interface TimeMetrics {
    min: number;
    max: number;
    average: number;
    median: number;
    standardDeviation: number;
}

interface MemoryMetrics {
    peak: number;
    average: number;
    allocations: number;
    deallocations: number;
}
```

### Advanced Types

```typescript
interface ComplexityMetrics {
    cyclomaticComplexity: number;
    cognitiveComplexity: number;
    maintainabilityIndex: number;
    linesOfCode: number;
    halsteadMetrics: HalsteadMetrics;
}

interface HalsteadMetrics {
    vocabulary: number;
    length: number;
    calculatedLength: number;
    volume: number;
    difficulty: number;
    effort: number;
    timeRequired: number;
    bugsDelivered: number;
}

interface LanguageCapabilities {
    supportsContextSensitive: boolean;
    supportsInheritance: boolean;
    supportsMultithreading: boolean;
    supportsStreaming: boolean;
    memoryManagement: 'manual' | 'automatic' | 'hybrid';
    performanceTier: 'high' | 'medium' | 'low';
    optimizationFeatures: string[];
}
```

## Usage Examples

### Basic Export

```typescript
import { CompilerCompilerExport, ExportConfiguration } from 'minotaur';

const exporter = new CompilerCompilerExport();

const config: ExportConfiguration = {
    targetLanguage: 'c++',
    optimizationLevel: 2,
    enableContextSensitive: true,
    enableInheritance: true,
    generateTests: true,
    generateDocumentation: false,
    outputDirectory: './generated'
};

const result = await exporter.exportGrammar(grammar, config);

if (result.success) {
    console.log('Export successful!');
    console.log(`Generated ${result.sourceFiles?.size} source files`);
} else {
    console.error('Export failed:', result.errors);
}
```

### Performance Benchmarking

```typescript
import { PerformanceBenchmark, BenchmarkConfiguration } from 'minotaur';

const benchmark = new PerformanceBenchmark({
    iterations: 1000,
    warmupIterations: 100,
    timeout: 30000
});

const result = await benchmark.runBenchmark(
    grammar,
    'c++',
    testInputs
);

console.log(`Average parse time: ${result.parseTime.average}ms`);
console.log(`Peak memory usage: ${result.memoryUsage.peak}MB`);
```

### Testing

```typescript
import { CompilerTestFramework, TestSuites } from 'minotaur';

const testFramework = new CompilerTestFramework(
    compilerExport,
    contextEngine,
    performanceBenchmark
);

const config = TestSuites.getComprehensiveTests();
const report = await testFramework.runTests(config);

console.log(`Tests passed: ${report.summary.passedTests}/${report.summary.totalTests}`);
console.log(`Success rate: ${report.summary.successRate}%`);
```

This API reference provides comprehensive documentation for all aspects of the Minotaur compiler-compiler system. For more examples and tutorials, please refer to the [User Guide](../user-guide/CompilerCompilerUserGuide.md).

