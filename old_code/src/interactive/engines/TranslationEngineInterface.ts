/**
 * Translation Engine Interface and Abstraction Layer
 *
 * This module provides the core abstraction layer for translation engines,
 * enabling LLM-agnostic translation with multiple fallback strategies.
 *
 * Key Features:
 * - Engine abstraction for pluggable translation strategies
 * - Configuration-driven engine selection
 * - Fallback and error handling
 * - Performance monitoring and metrics
 * - Quality assessment and confidence scoring
 *
 * @author Minotaur Team
 * @since 2024
 */

import { ZeroCopyASTNode } from '../../zerocopy/ast/ZeroCopyASTNode';

/**
 * Core translation engine interface
 */
export interface TranslationEngine {
    /** Unique engine identifier */
    readonly name: string;

    /** Engine priority (higher = preferred) */
    readonly priority: number;

    /** Engine version for compatibility tracking */
    readonly version: string;

    /** Engine capabilities and limitations */
    readonly capabilities: EngineCapabilities;

    /**
     * Check if engine is currently available
     */
    isAvailable(): Promise<boolean>;

    /**
     * Check if engine can handle specific translation
     */
    canTranslate(source: ASTNode, context: TranslationContext): Promise<boolean>;

    /**
     * Perform the actual translation
     */
    translate(source: ASTNode, context: TranslationContext): Promise<TranslationResult>;

    /**
     * Get confidence score for potential translation
     */
    getConfidence(source: ASTNode, context: TranslationContext): Promise<number>;

    /**
     * Get estimated cost for translation (in arbitrary units)
     */
    getEstimatedCost(source: ASTNode, context: TranslationContext): Promise<number>;

    /**
     * Get estimated time for translation (in milliseconds)
     */
    getEstimatedTime(source: ASTNode, context: TranslationContext): Promise<number>;

    /**
     * Initialize engine with configuration
     */
    initialize(config: EngineConfig): Promise<void>;

    /**
     * Cleanup engine resources
     */
    dispose(): Promise<void>;

    /**
     * Get engine statistics and metrics
     */
    getMetrics(): EngineMetrics;
}

/**
 * Engine capabilities descriptor
 */
export interface EngineCapabilities {
    /** Supported source languages */
    sourceLanguages: string[];

    /** Supported target languages */
    targetLanguages: string[];

    /** Maximum AST node complexity */
    maxComplexity: number;

    /** Supports batch processing */
    supportsBatch: boolean;

    /** Requires internet connectivity */
    requiresNetwork: boolean;

    /** Supports custom patterns */
    supportsCustomPatterns: boolean;

    /** Supports explanation generation */
    supportsExplanations: boolean;

    /** Supports alternative suggestions */
    supportsAlternatives: boolean;

    /** Memory requirements (in MB) */
    memoryRequirement: number;

    /** CPU intensity (1-10 scale) */
    cpuIntensity: number;
}

/**
 * Translation context information
 */
export interface TranslationContext {
    /** Source language identifier */
    sourceLanguage: string;

    /** Target language identifier */
    targetLanguage: string;

    /** Translation session ID */
    sessionId: string;

    /** User preferences */
    userPreferences: UserPreferences;

    /** Project context */
    projectContext?: ProjectContext;

    /** Previous translation steps */
    previousSteps: TranslationStep[];

    /** Available patterns */
    availablePatterns: TranslationPattern[];

    /** Quality requirements */
    qualityRequirements: QualityRequirements;

    /** Performance constraints */
    performanceConstraints: PerformanceConstraints;
}

/**
 * User preferences for translation
 */
export interface UserPreferences {
    /** Preferred translation style */
    style: 'conservative' | 'modern' | 'aggressive';

    /** Code formatting preferences */
    formatting: FormattingPreferences;

    /** Framework preferences */
    frameworks: string[];

    /** Security level requirements */
    securityLevel: 'basic' | 'enhanced' | 'strict';

    /** Performance optimization level */
    optimizationLevel: 'none' | 'basic' | 'aggressive';

    /** Comment preservation */
    preserveComments: boolean;

    /** Variable naming style */
    namingStyle: 'preserve' | 'camelCase' | 'snake_case' | 'PascalCase';
}

/**
 * Project context information
 */
export interface ProjectContext {
    /** Project type */
    type: 'web' | 'desktop' | 'mobile' | 'library' | 'service';

    /** Target framework */
    framework?: string;

    /** Dependencies */
    dependencies: string[];

    /** Build configuration */
    buildConfig?: any;

    /** Existing patterns in project */
    projectPatterns: TranslationPattern[];

    /** Code style guide */
    styleGuide?: any;
}

/**
 * Quality requirements
 */
export interface QualityRequirements {
    /** Minimum confidence threshold */
    minConfidence: number;

    /** Require compilation success */
    requireCompilation: boolean;

    /** Require test compatibility */
    requireTestCompatibility: boolean;

    /** Performance requirements */
    performanceRequirements?: PerformanceRequirements;

    /** Security requirements */
    securityRequirements?: SecurityRequirements;
}

/**
 * Performance constraints
 */
export interface PerformanceConstraints {
    /** Maximum translation time (ms) */
    maxTime: number;

    /** Maximum memory usage (MB) */
    maxMemory: number;

    /** Maximum cost (arbitrary units) */
    maxCost: number;

    /** Allow network calls */
    allowNetwork: boolean;

    /** Parallel processing allowed */
    allowParallel: boolean;
}

/**
 * Translation result
 */
export interface TranslationResult {
    /** Translated AST node */
    targetNode: ASTNode;

    /** Confidence score (0-1) */
    confidence: number;

    /** Human-readable reasoning */
    reasoning: string;

    /** Alternative translations */
    alternatives: AlternativeTranslation[];

    /** Applied patterns */
    appliedPatterns: TranslationPattern[];

    /** Translation metadata */
    metadata: TranslationMetadata;

    /** Quality metrics */
    quality: QualityMetrics;

    /** Performance metrics */
    performance: PerformanceMetrics;

    /** Warnings and issues */
    warnings: TranslationWarning[];

    /** Suggested improvements */
    improvements: TranslationImprovement[];
}

/**
 * Alternative translation option
 */
export interface AlternativeTranslation {
    /** Alternative AST node */
    targetNode: ASTNode;

    /** Confidence score */
    confidence: number;

    /** Description of alternative */
    description: string;

    /** Reasoning for alternative */
    reasoning: string;

    /** Trade-offs compared to primary */
    tradeoffs: string[];

    /** Applied patterns */
    appliedPatterns: TranslationPattern[];
}

/**
 * Translation metadata
 */
export interface TranslationMetadata {
    /** Engine that performed translation */
    engineName: string;

    /** Engine version */
    engineVersion: string;

    /** Translation timestamp */
    timestamp: Date;

    /** Processing time (ms) */
    processingTime: number;

    /** Memory usage (MB) */
    memoryUsage: number;

    /** Cost (arbitrary units) */
    cost: number;

    /** Network calls made */
    networkCalls: number;

    /** Cache hits */
    cacheHits: number;

    /** Additional engine-specific data */
    engineSpecific: Record<string, any>;
}

/**
 * Quality metrics
 */
export interface QualityMetrics {
    /** Syntactic correctness score */
    syntacticCorrectness: number;

    /** Semantic preservation score */
    semanticPreservation: number;

    /** Idiomatic quality score */
    idiomaticQuality: number;

    /** Performance impact score */
    performanceImpact: number;

    /** Security improvement score */
    securityImprovement: number;

    /** Maintainability score */
    maintainability: number;

    /** Overall quality score */
    overallQuality: number;
}

/**
 * Performance metrics
 */
export interface PerformanceMetrics {
    /** Translation speed (nodes/second) */
    translationSpeed: number;

    /** Memory efficiency (MB/node) */
    memoryEfficiency: number;

    /** CPU utilization (%) */
    cpuUtilization: number;

    /** Cache hit rate (%) */
    cacheHitRate: number;

    /** Network latency (ms) */
    networkLatency?: number;
}

/**
 * Translation warning
 */
export interface TranslationWarning {
    /** Warning severity */
    severity: 'info' | 'warning' | 'error';

    /** Warning message */
    message: string;

    /** Source location */
    sourceLocation?: SourceLocation;

    /** Target location */
    targetLocation?: SourceLocation;

    /** Suggested fix */
    suggestedFix?: string;

    /** Warning category */
    category: 'syntax' | 'semantics' | 'performance' | 'security' | 'style';
}

/**
 * Translation improvement suggestion
 */
export interface TranslationImprovement {
    /** Improvement type */
    type: 'performance' | 'security' | 'style' | 'maintainability';

    /** Description */
    description: string;

    /** Suggested change */
    suggestedChange: string;

    /** Expected benefit */
    expectedBenefit: string;

    /** Implementation effort */
    effort: 'low' | 'medium' | 'high';

    /** Priority */
    priority: 'low' | 'medium' | 'high';
}

/**
 * Engine configuration
 */
export interface EngineConfig {
    /** Engine-specific settings */
    settings: Record<string, any>;

    /** Resource limits */
    resourceLimits: ResourceLimits;

    /** Cache configuration */
    cacheConfig: CacheConfig;

    /** Logging configuration */
    loggingConfig: LoggingConfig;

    /** Network configuration */
    networkConfig?: NetworkConfig;
}

/**
 * Resource limits
 */
export interface ResourceLimits {
    /** Maximum memory (MB) */
    maxMemory: number;

    /** Maximum CPU time (ms) */
    maxCpuTime: number;

    /** Maximum network calls */
    maxNetworkCalls: number;

    /** Maximum concurrent operations */
    maxConcurrency: number;
}

/**
 * Cache configuration
 */
export interface CacheConfig {
    /** Enable caching */
    enabled: boolean;

    /** Cache size (MB) */
    maxSize: number;

    /** Cache TTL (seconds) */
    ttl: number;

    /** Cache strategy */
    strategy: 'lru' | 'lfu' | 'fifo';

    /** Persistent cache */
    persistent: boolean;
}

/**
 * Logging configuration
 */
export interface LoggingConfig {
    /** Log level */
    level: 'debug' | 'info' | 'warn' | 'error';

    /** Log performance metrics */
    logPerformance: boolean;

    /** Log quality metrics */
    logQuality: boolean;

    /** Log user interactions */
    logUserInteractions: boolean;
}

/**
 * Network configuration
 */
export interface NetworkConfig {
    /** API endpoints */
    endpoints: Record<string, string>;

    /** Authentication */
    authentication: AuthConfig;

    /** Timeout settings */
    timeouts: TimeoutConfig;

    /** Retry configuration */
    retryConfig: RetryConfig;
}

/**
 * Engine metrics
 */
export interface EngineMetrics {
    /** Total translations performed */
    totalTranslations: number;

    /** Successful translations */
    successfulTranslations: number;

    /** Failed translations */
    failedTranslations: number;

    /** Average confidence score */
    averageConfidence: number;

    /** Average processing time */
    averageProcessingTime: number;

    /** Average memory usage */
    averageMemoryUsage: number;

    /** Total cost */
    totalCost: number;

    /** Last time this engine was used */
    lastUsed: Date | null;

    /** Cache statistics */
    cacheStats: CacheStats;

    /** Quality statistics */
    qualityStats: QualityStats;

    /** Performance statistics */
    performanceStats: PerformanceStats;

    /** Error statistics */
    errorStats: ErrorStats;
}

/**
 * Cache statistics
 */
export interface CacheStats {
    /** Total cache requests */
    totalRequests: number;

    /** Cache hits */
    hits: number;

    /** Cache misses */
    misses: number;

    /** Hit rate */
    hitRate: number;

    /** Cache size */
    currentSize: number;

    /** Evictions */
    evictions: number;
}

/**
 * Quality statistics
 */
export interface QualityStats {
    /** Average quality score */
    averageQuality: number;

    /** Quality distribution */
    qualityDistribution: Record<string, number>;

    /** User satisfaction score */
    userSatisfaction: number;

    /** Pattern success rate */
    patternSuccessRate: number;
}

/**
 * Performance statistics
 */
export interface PerformanceStats {
    /** Average translation speed */
    averageSpeed: number;

    /** Peak memory usage */
    peakMemoryUsage: number;

    /** Average CPU utilization */
    averageCpuUtilization: number;

    /** Network latency statistics */
    networkLatencyStats?: LatencyStats;
}

/**
 * Error statistics
 */
export interface ErrorStats {
    /** Error count by type */
    errorsByType: Record<string, number>;

    /** Error count by severity */
    errorsBySeverity: Record<string, number>;

    /** Most common errors */
    commonErrors: string[];

    /** Error resolution rate */
    resolutionRate: number;
}

// Type aliases for commonly used types
export type ASTNode = ZeroCopyASTNode;
export type TranslationStep = any; // Import from InteractiveASTTranslator
export type TranslationPattern = any; // Import from PatternRecognitionEngine
export type SourceLocation = any; // Define based on AST implementation
export type FormattingPreferences = any; // Define based on requirements
export type PerformanceRequirements = any; // Define based on requirements
export type SecurityRequirements = any; // Define based on requirements
export type AuthConfig = any; // Define based on network requirements
export type TimeoutConfig = any; // Define based on network requirements
export type RetryConfig = any; // Define based on network requirements
export type LatencyStats = any; // Define based on performance requirements

/**
 * Engine factory interface
 */
export interface TranslationEngineFactory {
    /** Create engine instance */
    createEngine(config: EngineConfig): Promise<TranslationEngine>;

    /** Get engine information */
    getEngineInfo(): EngineInfo;

    /** Validate configuration */
    validateConfig(config: EngineConfig): ValidationResult;
}

/**
 * Engine information
 */
export interface EngineInfo {
    /** Engine name */
    name: string;

    /** Engine version */
    version: string;

    /** Engine description */
    description: string;

    /** Engine capabilities */
    capabilities: EngineCapabilities;

    /** Configuration schema */
    configSchema: any; // JSON Schema

    /** Documentation URL */
    documentationUrl?: string;

    /** License information */
    license: string;

    /** Author information */
    author: string;
}

/**
 * Configuration validation result
 */
export interface ValidationResult {
    /** Is configuration valid */
    isValid: boolean;

    /** Validation errors */
    errors: ValidationError[];

    /** Validation warnings */
    warnings: ValidationWarning[];

    /** Suggested fixes */
    suggestedFixes: ValidationFix[];
}

/**
 * Validation error
 */
export interface ValidationError {
    /** Error path */
    path: string;

    /** Error message */
    message: string;

    /** Error code */
    code: string;

    /** Suggested fix */
    suggestedFix?: string;
}

/**
 * Validation warning
 */
export interface ValidationWarning {
    /** Warning path */
    path: string;

    /** Warning message */
    message: string;

    /** Warning code */
    code: string;

    /** Suggested improvement */
    suggestedImprovement?: string;
}

/**
 * Validation fix
 */
export interface ValidationFix {
    /** Fix description */
    description: string;

    /** Fix path */
    path: string;

    /** Fix value */
    value: any;

    /** Fix confidence */
    confidence: number;
}

/**
 * Engine registry interface
 */
export interface TranslationEngineRegistry {
    /** Register engine factory */
    registerFactory(factory: TranslationEngineFactory): void;

    /** Unregister engine factory */
    unregisterFactory(engineName: string): void;

    /** Get available engine factories */
    getAvailableFactories(): TranslationEngineFactory[];

    /** Create engine instance */
    createEngine(engineName: string, config: EngineConfig): Promise<TranslationEngine>;

    /** Get engine information */
    getEngineInfo(engineName: string): EngineInfo | undefined;

    /** Find best engine for translation */
    findBestEngine(
        source: ASTNode,
        context: TranslationContext,
        constraints?: EngineConstraints
    ): Promise<TranslationEngine | undefined>;

    /** Get engine recommendations */
    getEngineRecommendations(
        source: ASTNode,
        context: TranslationContext
    ): Promise<EngineRecommendation[]>;
}

/**
 * Engine constraints for selection
 */
export interface EngineConstraints {
    /** Required capabilities */
    requiredCapabilities?: Partial<EngineCapabilities>;

    /** Maximum cost */
    maxCost?: number;

    /** Maximum time */
    maxTime?: number;

    /** Minimum confidence */
    minConfidence?: number;

    /** Allowed engines */
    allowedEngines?: string[];

    /** Excluded engines */
    excludedEngines?: string[];

    /** Network availability */
    networkAvailable?: boolean;
}

/**
 * Engine recommendation
 */
export interface EngineRecommendation {
    /** Engine name */
    engineName: string;

    /** Recommendation score */
    score: number;

    /** Expected confidence */
    expectedConfidence: number;

    /** Expected cost */
    expectedCost: number;

    /** Expected time */
    expectedTime: number;

    /** Reasoning */
    reasoning: string;

    /** Pros and cons */
    tradeoffs: {
        pros: string[];
        cons: string[];
    };
}

export default TranslationEngine;

