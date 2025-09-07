/**
 * Translation Engine Orchestrator
 *
 * This orchestrator manages multiple translation engines and implements the
 * LLM-agnostic architecture with intelligent fallback strategies. It ensures
 * the system works reliably regardless of external service availability.
 *
 * Key Features:
 * - Multi-engine coordination with priority-based selection
 * - Intelligent fallback strategies
 * - Cost-aware engine selection
 * - Performance optimization through engine caching
 * - Quality-based result selection
 * - Comprehensive error handling and recovery
 * - Configuration-driven engine management
 * - Real-time engine health monitoring
 *
 * @author Minotaur Team
 * @since 2024
 */

import {
  TranslationEngine,
  EngineCapabilities,
  TranslationContext,
  TranslationResult,
  EngineConfig,
  EngineMetrics,
  ASTNode,
  AlternativeTranslation,
  TranslationMetadata,
  QualityMetrics,
  PerformanceMetrics,
  TranslationWarning,
  TranslationImprovement,
} from './TranslationEngineInterface';
import { RuleBasedTranslationEngine } from './RuleBasedTranslationEngine';
import { PatternBasedTranslationEngine } from './PatternBasedTranslationEngine';
import { LLMTranslationEngine } from './LLMTranslationEngine';

/**
 * Engine selection strategy
 */
export enum EngineSelectionStrategy {
    /** Always use highest priority available engine */
    PRIORITY = 'priority',

    /** Use fastest available engine */
    SPEED = 'speed',

    /** Use most cost-effective engine */
    COST = 'cost',

    /** Use highest quality engine */
    QUALITY = 'quality',

    /** Use most reliable engine */
    RELIABILITY = 'reliability',

    /** Try all engines and pick best result */
    BEST_RESULT = 'best_result'
}

/**
 * Orchestrator configuration
 */
export interface OrchestratorConfig {
    /** Engine selection strategy */
    selectionStrategy: EngineSelectionStrategy;

    /** Engine selection strategy (alias for compatibility) */
    strategy?: EngineSelectionStrategy;

    /** Enable rule-based engine */
    enableRuleBased?: boolean;

    /** Enable pattern-based engine */
    enablePatternBased?: boolean;

    /** Enable LLM engine */
    enableLLM?: boolean;

    /** Maximum cost per translation */
    maxCost?: number;

    /** Minimum confidence threshold */
    minConfidence?: number;

    /** Enable health checks */
    enableHealthChecks?: boolean;

    /** Enable engine fallback */
    enableFallback: boolean;

    /** Maximum engines to try */
    maxEnginesPerTranslation: number;

    /** Minimum confidence threshold */
    minConfidenceThreshold: number;

    /** Maximum cost per translation */
    maxCostPerTranslation: number;

    /** Maximum time per translation (ms) */
    maxTimePerTranslation: number;

    /** Engine configurations */
    engineConfigs: Record<string, EngineConfig>;

    /** Engine priorities */
    enginePriorities: Record<string, number>;

    /** Quality thresholds */
    qualityThresholds: QualityThresholds;

    /** Performance thresholds */
    performanceThresholds: PerformanceThresholds;

    /** Health check configuration */
    healthCheck: HealthCheckConfig;
}

/**
 * Quality thresholds for engine selection
 */
interface QualityThresholds {
    /** Minimum syntactic correctness */
    minSyntacticCorrectness: number;

    /** Minimum semantic preservation */
    minSemanticPreservation: number;

    /** Minimum overall quality */
    minOverallQuality: number;
}

/**
 * Performance thresholds
 */
interface PerformanceThresholds {
    /** Maximum response time (ms) */
    maxResponseTime: number;

    /** Maximum memory usage (MB) */
    maxMemoryUsage: number;

    /** Minimum success rate */
    minSuccessRate: number;
}

/**
 * Health check configuration
 */
interface HealthCheckConfig {
    /** Health check interval (ms) */
    interval: number;

    /** Health check timeout (ms) */
    timeout: number;

    /** Number of failures before marking unhealthy */
    failureThreshold: number;

    /** Recovery check interval (ms) */
    recoveryInterval: number;
}

/**
 * Engine health status
 */
interface EngineHealth {
    /** Is engine healthy */
    isHealthy: boolean;

    /** Last health check time */
    lastCheck: Date;

    /** Consecutive failures */
    consecutiveFailures: number;

    /** Average response time */
    averageResponseTime: number;

    /** Success rate */
    successRate: number;

    /** Last error */
    lastError?: string;
}

/**
 * Translation attempt result
 */
interface TranslationAttempt {
    /** Engine used */
    engine: TranslationEngine;

    /** Translation result */
    result?: TranslationResult;

    /** Error if failed */
    error?: Error;

    /** Attempt duration */
    duration: number;

    /** Cost incurred */
    cost: number;
}

/**
 * Engine selection result
 */
interface EngineSelection {
    /** Selected engines in order */
    engines: TranslationEngine[];

    /** Selection reasoning */
    reasoning: string;

    /** Estimated total cost */
    estimatedCost: number;

    /** Estimated total time */
    estimatedTime: number;
}

/**
 * Translation Engine Orchestrator Implementation
 */
export class TranslationEngineOrchestrator implements TranslationEngine {
  public readonly name = 'orchestrator';
  public readonly priority = 1000; // Highest priority as coordinator
  public readonly version = '1.0.0';

  public readonly capabilities: EngineCapabilities = {
    sourceLanguages: [], // Will be populated from engines
    targetLanguages: [], // Will be populated from engines
    maxComplexity: 0, // Will be set to maximum of engines
    supportsBatch: true,
    requiresNetwork: false, // Can work offline with rule/pattern engines
    supportsCustomPatterns: true,
    supportsExplanations: true,
    supportsAlternatives: true,
    memoryRequirement: 0, // Will be calculated from engines
    cpuIntensity: 0, // Will be calculated from engines
  };

  private engines: Map<string, TranslationEngine> = new Map();
  private engineHealth: Map<string, EngineHealth> = new Map();
  private engineMetrics: Map<string, EngineMetrics> = new Map();
  private config: OrchestratorConfig;
  private metrics: EngineMetrics;
  private healthCheckInterval: ReturnType<typeof setInterval> | null = null;

  constructor(
    ruleBasedEngine: RuleBasedTranslationEngine,
    patternBasedEngine: PatternBasedTranslationEngine,
    llmEngine: LLMTranslationEngine,
    config: OrchestratorConfig,
  ) {
    this.config = config;
    this.initializeMetrics();
    this.initializeEngines(ruleBasedEngine, patternBasedEngine, llmEngine);
    this.startHealthChecks();
  }

  /**
     * Initialize orchestrator metrics
     */
  private initializeMetrics(): void {
    this.metrics = {
      totalTranslations: 0,
      successfulTranslations: 0,
      failedTranslations: 0,
      averageConfidence: 0,
      averageProcessingTime: 0,
      averageMemoryUsage: 0,
      totalCost: 0,
      lastUsed: null,
      cacheStats: {
        totalRequests: 0,
        hits: 0,
        misses: 0,
        hitRate: 0,
        currentSize: 0,
        evictions: 0,
      },
      qualityStats: {
        averageQuality: 0,
        qualityDistribution: {},
        userSatisfaction: 0,
        patternSuccessRate: 0,
      },
      performanceStats: {
        averageSpeed: 0,
        peakMemoryUsage: 0,
        averageCpuUtilization: 0,
      },
      errorStats: {
        errorsByType: {},
        errorsBySeverity: {},
        commonErrors: [],
        resolutionRate: 0,
      },
    };
  }

  /**
     * Initialize translation engines
     */
  private async initializeEngines(
    ruleBasedEngine: RuleBasedTranslationEngine,
    patternBasedEngine: PatternBasedTranslationEngine,
    llmEngine: LLMTranslationEngine,
  ): Promise<void> {
    // eslint-disable-next-line no-console
    // eslint-disable-next-line no-console
    console.log('🔧 Orchestrator: Initializing engines...');

    // Initialize rule-based engine (always available)
    this.engines.set('rule-based', ruleBasedEngine);
    this.engineHealth.set('rule-based', {
      isHealthy: true,
      lastCheck: new Date(),
      consecutiveFailures: 0,
      averageResponseTime: 50,
      successRate: 0.95,
    });
    // eslint-disable-next-line no-console
    console.log('✅ Rule-based engine initialized');

    this.engineMetrics.set('rule-based', {
      totalTranslations: 0,
      successfulTranslations: 0,
      failedTranslations: 0,
      averageConfidence: 0,
      averageProcessingTime: 0,
      averageMemoryUsage: 0,
      totalCost: 0,
      lastUsed: null,
      cacheStats: {
        totalRequests: 0,
        hits: 0,
        misses: 0,
        hitRate: 0,
        currentSize: 0,
        evictions: 0,
      },
      qualityStats: {
        averageQuality: 0,
        qualityDistribution: {},
        userSatisfaction: 0,
        patternSuccessRate: 0,
      },
      performanceStats: {
        averageSpeed: 0,
        peakMemoryUsage: 0,
        averageCpuUtilization: 0,
      },
      errorStats: {
        errorsByType: {},
        errorsBySeverity: {},
        commonErrors: [],
        resolutionRate: 0,
      },
    });

    // Initialize pattern-based engine (always available)
    this.engines.set('pattern-based', patternBasedEngine);
    this.engineHealth.set('pattern-based', {
      isHealthy: true,
      lastCheck: new Date(),
      consecutiveFailures: 0,
      averageResponseTime: 200,
      successRate: 0.90,
    });
    // eslint-disable-next-line no-console
    console.log('✅ Pattern-based engine initialized');
    this.engineMetrics.set('pattern-based', {
      totalTranslations: 0,
      successfulTranslations: 0,
      failedTranslations: 0,
      averageConfidence: 0,
      averageProcessingTime: 0,
      averageMemoryUsage: 0,
      totalCost: 0,
      lastUsed: null,
      cacheStats: {
        totalRequests: 0,
        hits: 0,
        misses: 0,
        hitRate: 0,
        currentSize: 0,
        evictions: 0,
      },
      qualityStats: {
        averageQuality: 0,
        qualityDistribution: {},
        userSatisfaction: 0,
        patternSuccessRate: 0,
      },
      performanceStats: {
        averageSpeed: 0,
        peakMemoryUsage: 0,
        averageCpuUtilization: 0,
      },
      errorStats: {
        errorsByType: {},
        errorsBySeverity: {},
        commonErrors: [],
        resolutionRate: 0,
      },
    });

    // Initialize LLM engine (optional)
    // eslint-disable-next-line no-console
    console.log('🔍 Checking for LLM engine config...');
    // eslint-disable-next-line no-console
    console.log(`🔍 Config keys: ${Object.keys(this.config.engineConfigs)}`);
    // eslint-disable-next-line no-console
    console.log('🔍 Looking for: llm-enhanced');

    if (this.config.engineConfigs['llm-enhanced']) {
    // eslint-disable-next-line no-console
      console.log('✅ LLM engine config found, initializing...');
      this.engines.set('llm-enhanced', llmEngine);
      this.engineHealth.set('llm-enhanced', {
        isHealthy: true,
        lastCheck: new Date(),
        consecutiveFailures: 0,
        averageResponseTime: 1500,
        successRate: 0.85,
      });

      this.engineMetrics.set('llm-enhanced', {
        totalTranslations: 0,
        successfulTranslations: 0,
        failedTranslations: 0,
        averageConfidence: 0,
        averageProcessingTime: 0,
        averageMemoryUsage: 0,
        totalCost: 0,
        lastUsed: null,
        cacheStats: {
          totalRequests: 0,
          hits: 0,
          misses: 0,
          hitRate: 0,
          currentSize: 0,
          evictions: 0,
        },
        qualityStats: {
          averageQuality: 0,
          qualityDistribution: {},
          userSatisfaction: 0,
          patternSuccessRate: 0,
        },
        performanceStats: {
          averageSpeed: 0,
          peakMemoryUsage: 0,
          averageCpuUtilization: 0,
        },
        errorStats: {
          errorsByType: {},
          errorsBySeverity: {},
          commonErrors: [],
          resolutionRate: 0,
        },
      });

    // eslint-disable-next-line no-console
      console.log('✅ LLM engine initialized and registered');
    } else {
    // eslint-disable-next-line no-console
      console.log('❌ LLM engine config not found, skipping initialization');
    }

    // eslint-disable-next-line no-console
    console.log(`🔧 Total engines registered: ${this.engines.size}`);

    // Update capabilities based on available engines
    this.updateCapabilities();
  }

  /**
     * Update orchestrator capabilities based on available engines
     */
  private updateCapabilities(): void {
    const sourceLanguages = new Set<string>();
    const targetLanguages = new Set<string>();
    let maxComplexity = 0;
    let totalMemory = 0;
    let maxCpuIntensity = 0;

    for (const engine of this.engines.values()) {
      // Aggregate source languages
      engine.capabilities.sourceLanguages.forEach(lang => sourceLanguages.add(lang));

      // Aggregate target languages
      engine.capabilities.targetLanguages.forEach(lang => targetLanguages.add(lang));

      // Take maximum complexity
      maxComplexity = Math.max(maxComplexity, engine.capabilities.maxComplexity);

      // Sum memory requirements
      totalMemory += engine.capabilities.memoryRequirement;

      // Take maximum CPU intensity
      maxCpuIntensity = Math.max(maxCpuIntensity, engine.capabilities.cpuIntensity);
    }

    // Update capabilities
    (this.capabilities as any).sourceLanguages = Array.from(sourceLanguages);
    (this.capabilities as any).targetLanguages = Array.from(targetLanguages);
    (this.capabilities as any).maxComplexity = maxComplexity;
    (this.capabilities as any).memoryRequirement = totalMemory;
    (this.capabilities as any).cpuIntensity = maxCpuIntensity;

    // Network requirement is true if any engine requires network
    (this.capabilities as any).requiresNetwork = Array.from(this.engines.values())
      .some(engine => engine.capabilities.requiresNetwork);
  }

  /**
     * Start health check monitoring
     */
  private startHealthChecks(): void {
    if (this.config.healthCheck.interval > 0) {
      this.healthCheckInterval = setInterval(
        () => this.performHealthChecks(),
        this.config.healthCheck.interval,
      );
    }
  }

  /**
     * Perform health checks on all engines
     */
  private async performHealthChecks(): Promise<void> {
    for (const [engineName, engine] of this.engines.entries()) {
      try {
        const startTime = Date.now();
        const isAvailable = await Promise.race([
          engine.isAvailable(),
          new Promise<boolean>((_, reject) =>
            setTimeout(() => reject(new Error('Health check timeout')),
              this.config.healthCheck.timeout),
          ),
        ]);

        const responseTime = Date.now() - startTime;
        const health = this.engineHealth.get(engineName)!;

        if (isAvailable) {
          health.isHealthy = true;
          health.consecutiveFailures = 0;
          health.averageResponseTime = (health.averageResponseTime + responseTime) / 2;
        } else {
          this.markEngineUnhealthy(engineName, 'Engine reported unavailable');
        }

        health.lastCheck = new Date();

      } catch (error) {
        this.markEngineUnhealthy(engineName, error instanceof Error ? error.message : String(error));
      }
    }
  }

  /**
     * Mark engine as unhealthy
     */
  private markEngineUnhealthy(engineName: string, error: string): void {
    const health = this.engineHealth.get(engineName)!;
    health.consecutiveFailures++;
    health.lastError = error;

    if (health.consecutiveFailures >= this.config.healthCheck.failureThreshold) {
      health.isHealthy = false;
    }
  }

  /**
     * Check if orchestrator is available
     */
  async isAvailable(): Promise<boolean> {
    // Orchestrator is available if at least one engine is healthy
    return Array.from(this.engineHealth.values()).some(health => health.isHealthy);
  }

  /**
     * Get status of all engines
     */
  // eslint-disable-next-line max-len
  getEngineStatus(): { [key: string]: { available: boolean; healthy: boolean; lastUsed: Date | null; successRate: number; avgResponseTime: number } } {
    const status: { [key: string]: any } = {};

    // Map internal engine names to expected test names
    const engineNameMap: { [key: string]: string } = {
      'rule-based': 'ruleBased',
      'pattern-based': 'patternBased',
      'llm-enhanced': 'llm',
    };

    for (const [internalName, health] of this.engineHealth.entries()) {
      const externalName = engineNameMap[internalName] || internalName;
      const metrics = this.engineMetrics.get(internalName);

      status[externalName] = {
        available: health.isHealthy,
        healthy: health.isHealthy,
        lastUsed: metrics ? metrics.lastUsed : null,
        successRate: metrics ? (metrics.successfulTranslations / Math.max(metrics.totalTranslations, 1)) : 0,
        avgResponseTime: metrics ? metrics.averageProcessingTime : 0,
      };
    }

    return status;
  }

  /**
     * Check if orchestrator can handle translation
     */
  async canTranslate(source: ASTNode, context: TranslationContext): Promise<boolean> {
    const availableEngines = await this.getAvailableEngines(source, context);
    return availableEngines.length > 0;
  }

  /**
     * Get available engines for translation
     */
  private async getAvailableEngines(source: ASTNode, context: TranslationContext): Promise<TranslationEngine[]> {
    const availableEngines: TranslationEngine[] = [];

    // eslint-disable-next-line no-console
    console.log('🔍 Orchestrator: Checking available engines...');
    // eslint-disable-next-line no-console
    console.log(`🔍 Total engines registered: ${this.engines.size}`);

    for (const [engineName, engine] of this.engines.entries()) {
      const health = this.engineHealth.get(engineName)!;

    // eslint-disable-next-line no-console
      console.log(`🔍 Engine: ${engineName}`);
    // eslint-disable-next-line no-console
      console.log(`  - Health: ${health.isHealthy ? '✅ Healthy' : '❌ Unhealthy'}`);

      if (health.isHealthy) {
        const canTranslate = await engine.canTranslate(source, context);
    // eslint-disable-next-line no-console
        console.log(`  - Can Translate: ${canTranslate ? '✅ Yes' : '❌ No'}`);

        if (canTranslate) {
          availableEngines.push(engine);
    // eslint-disable-next-line no-console
          console.log('  - ✅ Added to available engines');
        }
      } else {
    // eslint-disable-next-line no-console
        console.log('  - ❌ Skipped due to health check failure');
      }
    }

    // eslint-disable-next-line no-console
    console.log(`🔍 Total available engines: ${availableEngines.length}`);

    return availableEngines;
  }

  /**
     * Perform translation with orchestration
     */
  async translate(source: ASTNode, context: TranslationContext): Promise<TranslationResult> {
    const startTime = Date.now();
    const startMemory = process.memoryUsage().heapUsed;

    try {
      // Select engines for translation
      const selection = await this.selectEngines(source, context);

      if (selection.engines.length === 0) {
        throw new Error('No available engines for translation');
      }

      // Attempt translation with selected engines
      const attempts = await this.attemptTranslations(source, context, selection.engines);

      // Select best result
      const bestResult = this.selectBestResult(attempts);

      if (!bestResult) {
        throw new Error('All translation attempts failed');
      }

      // Update engine health based on results
      this.updateEngineHealthFromAttempts(attempts);

      // Update engine metrics including lastUsed timestamps
      this.updateEngineMetricsFromAttempts(attempts);

      // Calculate orchestrator metrics
      const processingTime = Date.now() - startTime;
      const memoryUsage = (process.memoryUsage().heapUsed - startMemory) / 1024 / 1024;
      const totalCost = attempts.reduce((sum, attempt) => sum + attempt.cost, 0);

      // Update orchestrator metrics
      this.updateMetrics(true, processingTime, memoryUsage, bestResult.result!.confidence, totalCost);

      // Enhance result with orchestrator metadata
      const enhancedResult = this.enhanceResult(bestResult.result!, selection, attempts);

      return enhancedResult;

    } catch (error) {
      const processingTime = Date.now() - startTime;
      this.updateMetrics(false, processingTime, 0, 0, 0);
      throw error;
    }
  }

  /**
     * Select engines for translation based on strategy
     */
  private async selectEngines(source: ASTNode, context: TranslationContext): Promise<EngineSelection> {
    const availableEngines = await this.getAvailableEngines(source, context);

    if (availableEngines.length === 0) {
      return {
        engines: [],
        reasoning: 'No available engines',
        estimatedCost: 0,
        estimatedTime: 0,
      };
    }

    let selectedEngines: TranslationEngine[] = [];
    let reasoning = '';

    switch (this.config.selectionStrategy) {
      case EngineSelectionStrategy.PRIORITY:
        selectedEngines = this.selectByPriority(availableEngines);
        reasoning = 'Selected by priority order';
        break;

      case EngineSelectionStrategy.SPEED:
        selectedEngines = await this.selectBySpeed(availableEngines, source, context);
        reasoning = 'Selected by estimated speed';
        break;

      case EngineSelectionStrategy.COST:
        selectedEngines = await this.selectByCost(availableEngines, source, context);
        reasoning = 'Selected by estimated cost';
        break;

      case EngineSelectionStrategy.QUALITY:
        selectedEngines = await this.selectByQuality(availableEngines, source, context);
        reasoning = 'Selected by expected quality';
        break;

      case EngineSelectionStrategy.RELIABILITY:
        selectedEngines = this.selectByReliability(availableEngines);
        reasoning = 'Selected by reliability metrics';
        break;

      case EngineSelectionStrategy.BEST_RESULT:
        selectedEngines = availableEngines.slice(0, this.config.maxEnginesPerTranslation);
        reasoning = 'Selected all available engines for best result';
        break;

      default:
        selectedEngines = this.selectByPriority(availableEngines);
        reasoning = 'Selected by default priority order';
    }

    // Limit to maximum engines per translation
    selectedEngines = selectedEngines.slice(0, this.config.maxEnginesPerTranslation);

    // Calculate estimates
    const estimatedCost = await this.calculateEstimatedCost(selectedEngines, source, context);
    const estimatedTime = await this.calculateEstimatedTime(selectedEngines, source, context);

    return {
      engines: selectedEngines,
      reasoning,
      estimatedCost,
      estimatedTime,
    };
  }

  /**
     * Select engines by priority
     */
  private selectByPriority(engines: TranslationEngine[]): TranslationEngine[] {
    return engines.sort((a, b) => {
      const priorityA = this.config.enginePriorities[a.name] || a.priority;
      const priorityB = this.config.enginePriorities[b.name] || b.priority;
      return priorityB - priorityA;
    });
  }

  /**
     * Select engines by speed
     */
  // eslint-disable-next-line max-len
  private async selectBySpeed(engines: TranslationEngine[], source: ASTNode, context: TranslationContext): Promise<TranslationEngine[]> {
    const engineTimes: Array<{ engine: TranslationEngine, time: number }> = [];

    for (const engine of engines) {
      const estimatedTime = await engine.getEstimatedTime(source, context);
      engineTimes.push({ engine, time: estimatedTime });
    }

    return engineTimes
      .sort((a, b) => a.time - b.time)
      .map(item => item.engine);
  }

  /**
     * Select engines by cost
     */
  // eslint-disable-next-line max-len
  private async selectByCost(engines: TranslationEngine[], source: ASTNode, context: TranslationContext): Promise<TranslationEngine[]> {
    const engineCosts: Array<{ engine: TranslationEngine, cost: number }> = [];

    for (const engine of engines) {
      const estimatedCost = await engine.getEstimatedCost(source, context);
      engineCosts.push({ engine, cost: estimatedCost });
    }

    return engineCosts
      .sort((a, b) => a.cost - b.cost)
      .map(item => item.engine);
  }

  /**
     * Select engines by expected quality
     */
  // eslint-disable-next-line max-len
  private async selectByQuality(engines: TranslationEngine[], source: ASTNode, context: TranslationContext): Promise<TranslationEngine[]> {
    const engineQualities: Array<{ engine: TranslationEngine, quality: number }> = [];

    for (const engine of engines) {
      const confidence = await engine.getConfidence(source, context);
      const health = this.engineHealth.get(engine.name)!;
      const quality = confidence * health.successRate;
      engineQualities.push({ engine, quality });
    }

    return engineQualities
      .sort((a, b) => b.quality - a.quality)
      .map(item => item.engine);
  }

  /**
     * Select engines by reliability
     */
  private selectByReliability(engines: TranslationEngine[]): TranslationEngine[] {
    return engines.sort((a, b) => {
      const healthA = this.engineHealth.get(a.name)!;
      const healthB = this.engineHealth.get(b.name)!;

      const reliabilityA = healthA.successRate * (1 - healthA.consecutiveFailures * 0.1);
      const reliabilityB = healthB.successRate * (1 - healthB.consecutiveFailures * 0.1);

      return reliabilityB - reliabilityA;
    });
  }

  /**
     * Calculate estimated cost for selected engines
     */
  // eslint-disable-next-line max-len
  private async calculateEstimatedCost(engines: TranslationEngine[], source: ASTNode, context: TranslationContext): Promise<number> {
    let totalCost = 0;

    for (const engine of engines) {
      const cost = await engine.getEstimatedCost(source, context);
      totalCost += cost;

      // For fallback strategy, only count first engine cost
      if (!this.config.enableFallback) {
        break;
      }
    }

    return totalCost;
  }

  /**
     * Calculate estimated time for selected engines
     */
  // eslint-disable-next-line max-len
  private async calculateEstimatedTime(engines: TranslationEngine[], source: ASTNode, context: TranslationContext): Promise<number> {
    if (engines.length === 0) {
      return 0;
    }

    if (this.config.selectionStrategy === EngineSelectionStrategy.BEST_RESULT) {
      // Parallel execution - return max time
      let maxTime = 0;
      for (const engine of engines) {
        const time = await engine.getEstimatedTime(source, context);
        maxTime = Math.max(maxTime, time);
      }
      return maxTime;
    } else {
      // Sequential execution - return first engine time
      return await engines[0].getEstimatedTime(source, context);
    }
  }

  /**
     * Attempt translations with selected engines
     */
  private async attemptTranslations(
    source: ASTNode,
    context: TranslationContext,
    engines: TranslationEngine[],
  ): Promise<TranslationAttempt[]> {
    const attempts: TranslationAttempt[] = [];

    if (this.config.selectionStrategy === EngineSelectionStrategy.BEST_RESULT) {
      // Try all engines in parallel
      const promises = engines.map(engine => this.attemptTranslation(source, context, engine));
      const results = await Promise.allSettled(promises);

      for (let i = 0; i < results.length; i++) {
        const result = results[i];
        if (result.status === 'fulfilled') {
          attempts.push(result.value);
        } else {
          attempts.push({
            engine: engines[i],
            error: result.reason,
            duration: 0,
            cost: 0,
          });
        }
      }
    } else {
      // Try engines sequentially with fallback
      for (const engine of engines) {
        const attempt = await this.attemptTranslation(source, context, engine);
        attempts.push(attempt);

        // If successful and meets quality threshold, stop
        if (attempt.result &&
                    attempt.result.confidence >= this.config.minConfidenceThreshold &&
                    this.meetsQualityThresholds(attempt.result.quality)) {
          break;
        }

        // If fallback is disabled, stop after first attempt
        if (!this.config.enableFallback) {
          break;
        }
      }
    }

    return attempts;
  }

  /**
     * Attempt translation with single engine
     */
  private async attemptTranslation(
    source: ASTNode,
    context: TranslationContext,
    engine: TranslationEngine,
  ): Promise<TranslationAttempt> {
    const startTime = Date.now();

    try {
      const result = await Promise.race([
        engine.translate(source, context),
        new Promise<never>((_, reject) =>
          setTimeout(() => reject(new Error('Translation timeout')),
            this.config.maxTimePerTranslation),
        ),
      ]);

      const duration = Date.now() - startTime;
      const cost = result.metadata.cost;

      return {
        engine,
        result,
        duration,
        cost,
      };

    } catch (error) {
      const duration = Date.now() - startTime;

      return {
        engine,
        error: error as Error,
        duration,
        cost: 0,
      };
    }
  }

  /**
     * Select best result from attempts
     */
  private selectBestResult(attempts: TranslationAttempt[]): TranslationAttempt | null {
    const successfulAttempts = attempts.filter(attempt => attempt.result);

    if (successfulAttempts.length === 0) {
      return null;
    }

    // Score each result
    const scoredAttempts = successfulAttempts.map(attempt => ({
      attempt,
      score: this.calculateResultScore(attempt.result!),
    }));

    // Sort by score and return best
    scoredAttempts.sort((a, b) => b.score - a.score);
    return scoredAttempts[0].attempt;
  }

  /**
     * Calculate result score for comparison
     */
  private calculateResultScore(result: TranslationResult): number {
    const quality = result.quality;
    const confidence = result.confidence;
    const cost = result.metadata.cost;
    const time = result.metadata.processingTime;

    // Weighted score combining multiple factors
    const qualityScore = quality.overallQuality * 0.4;
    const confidenceScore = confidence * 0.3;
    const costScore = Math.max(0, 1 - cost / this.config.maxCostPerTranslation) * 0.2;
    const timeScore = Math.max(0, 1 - time / this.config.maxTimePerTranslation) * 0.1;

    return qualityScore + confidenceScore + costScore + timeScore;
  }

  /**
     * Check if result meets quality thresholds
     */
  private meetsQualityThresholds(quality: QualityMetrics): boolean {
    return quality.syntacticCorrectness >= this.config.qualityThresholds.minSyntacticCorrectness &&
               quality.semanticPreservation >= this.config.qualityThresholds.minSemanticPreservation &&
               quality.overallQuality >= this.config.qualityThresholds.minOverallQuality;
  }

  /**
     * Update engine health based on translation attempts
     */
  private updateEngineHealthFromAttempts(attempts: TranslationAttempt[]): void {
    for (const attempt of attempts) {
      const health = this.engineHealth.get(attempt.engine.name)!;

      if (attempt.result) {
        // Successful translation
        health.consecutiveFailures = 0;
        health.successRate = (health.successRate * 0.9) + (1.0 * 0.1); // Exponential moving average
        health.averageResponseTime = (health.averageResponseTime * 0.9) + (attempt.duration * 0.1);
      } else {
        // Failed translation
        health.consecutiveFailures++;
        health.successRate = (health.successRate * 0.9) + (0.0 * 0.1);
        health.lastError = attempt.error?.message;

        if (health.consecutiveFailures >= this.config.healthCheck.failureThreshold) {
          health.isHealthy = false;
        }
      }

      health.lastCheck = new Date();
    }
  }

  /**
   * Update engine metrics from translation attempts
   */
  private updateEngineMetricsFromAttempts(attempts: TranslationAttempt[]): void {
    for (const attempt of attempts) {
      const metrics = this.engineMetrics.get(attempt.engine.name);
      if (!metrics) {
continue;
}

      // Update lastUsed timestamp for this engine
      metrics.lastUsed = new Date();

      // Update other metrics
      metrics.totalTranslations++;
      if (attempt.result) {
        metrics.successfulTranslations++;
        // Update average confidence
        metrics.averageConfidence =
          (metrics.averageConfidence * (metrics.successfulTranslations - 1) + attempt.result.confidence) /
          metrics.successfulTranslations;
      } else {
        metrics.failedTranslations++;
      }

      // Update average processing time
      metrics.averageProcessingTime =
        (metrics.averageProcessingTime * (metrics.totalTranslations - 1) + attempt.duration) /
        metrics.totalTranslations;

      // Update total cost
      metrics.totalCost += attempt.cost;
    }
  }

  /**
     * Enhance result with orchestrator metadata
     */
  private enhanceResult(
    result: TranslationResult,
    selection: EngineSelection,
    attempts: TranslationAttempt[],
  ): TranslationResult {
    // Add orchestrator-specific metadata
    result.metadata.engineSpecific = {
      ...result.metadata.engineSpecific,
      orchestrator: {
        selectionStrategy: this.config.selectionStrategy,
        selectionReasoning: selection.reasoning,
        enginesAttempted: attempts.length,
        enginesSuccessful: attempts.filter(a => a.result).length,
        totalCost: attempts.reduce((sum, a) => sum + a.cost, 0),
        totalTime: attempts.reduce((sum, a) => sum + a.duration, 0),
        fallbackUsed: attempts.length > 1,
        engineUsed: result.metadata.engineName,
      },
    };

    // Add warnings if fallback was used
    if (attempts.length > 1) {
      result.warnings.push({
        severity: 'info',
        message: `Used fallback strategy: ${attempts.length} engines attempted`,
        category: 'performance',
        sourceLocation: undefined,
        targetLocation: undefined,
        suggestedFix: 'Consider optimizing primary engine configuration',
      });
    }

    // Add alternatives from other successful attempts
    const otherSuccessful = attempts.filter(a => a.result && a.engine.name !== result.metadata.engineName);
    for (const attempt of otherSuccessful.slice(0, 2)) { // Limit to 2 additional alternatives
      result.alternatives.push({
        targetNode: attempt.result!.targetNode,
        confidence: attempt.result!.confidence,
        description: `Alternative from ${attempt.engine.name} engine`,
        reasoning: attempt.result!.reasoning,
        tradeoffs: [`Generated by ${attempt.engine.name}`, `Cost: ${attempt.cost}`, `Time: ${attempt.duration}ms`],
        appliedPatterns: attempt.result!.appliedPatterns,
      });
    }

    return result;
  }

  /**
     * Update orchestrator metrics
     */
  // eslint-disable-next-line max-len
  private updateMetrics(success: boolean, processingTime: number, memoryUsage: number, confidence: number, cost: number): void {
    this.metrics.totalTranslations++;
    this.metrics.totalCost += cost;
    this.metrics.lastUsed = new Date(); // Update timestamp when metrics are updated

    if (success) {
      this.metrics.successfulTranslations++;
      this.metrics.averageConfidence =
                (this.metrics.averageConfidence * (this.metrics.successfulTranslations - 1) + confidence) /
                this.metrics.successfulTranslations;
    } else {
      this.metrics.failedTranslations++;
    }

    this.metrics.averageProcessingTime =
            (this.metrics.averageProcessingTime * (this.metrics.totalTranslations - 1) + processingTime) /
            this.metrics.totalTranslations;

    this.metrics.averageMemoryUsage =
            (this.metrics.averageMemoryUsage * (this.metrics.totalTranslations - 1) + memoryUsage) /
            this.metrics.totalTranslations;
  }

  /**
     * Engine interface methods
     */
  async getConfidence(source: ASTNode, context: TranslationContext): Promise<number> {
    const availableEngines = await this.getAvailableEngines(source, context);

    if (availableEngines.length === 0) {
      return 0;
    }

    // Return confidence of best available engine
    const selection = await this.selectEngines(source, context);
    if (selection.engines.length > 0) {
      return await selection.engines[0].getConfidence(source, context);
    }

    return 0;
  }

  async getEstimatedCost(source: ASTNode, context: TranslationContext): Promise<number> {
    const selection = await this.selectEngines(source, context);
    return selection.estimatedCost;
  }

  async getEstimatedTime(source: ASTNode, context: TranslationContext): Promise<number> {
    const selection = await this.selectEngines(source, context);
    return selection.estimatedTime;
  }

  async initialize(config: EngineConfig): Promise<void> {
    // Orchestrator initialization is handled in constructor
    // This method is here for interface compliance
  }

  async dispose(): Promise<void> {
    // Stop health checks
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
      this.healthCheckInterval = null;
    }

    // Dispose all engines
    for (const engine of this.engines.values()) {
      await engine.dispose();
    }

    this.engines.clear();
    this.engineHealth.clear();
  }

  getMetrics(): EngineMetrics {
    return { ...this.metrics };
  }

  /**
     * Get engine health status
     */
  getEngineHealth(): Record<string, EngineHealth> {
    const health: Record<string, EngineHealth> = {};
    for (const [name, engineHealth] of this.engineHealth.entries()) {
      health[name] = { ...engineHealth };
    }
    return health;
  }

  /**
     * Get available engines
     */
  getAvailableEngineNames(): string[] {
    return Array.from(this.engines.keys()).filter(name =>
      this.engineHealth.get(name)?.isHealthy,
    );
  }

  /**
     * Force health check on all engines
     */
  async forceHealthCheck(): Promise<void> {
    await this.performHealthChecks();
  }

  /**
     * Update orchestrator configuration
     */
  updateConfiguration(config: Partial<OrchestratorConfig>): void {
    this.config = { ...this.config, ...config };
  }
}

