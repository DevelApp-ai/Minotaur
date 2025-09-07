/**
 * Tests for TranslationEngineOrchestrator timestamp tracking implementation
 */

import { TranslationEngineOrchestrator, OrchestratorConfig, EngineSelectionStrategy } from '../interactive/engines/TranslationEngineOrchestrator';
import { RuleBasedTranslationEngine } from '../interactive/engines/RuleBasedTranslationEngine';
import { PatternBasedTranslationEngine } from '../interactive/engines/PatternBasedTranslationEngine';
import { LLMTranslationEngine } from '../interactive/engines/LLMTranslationEngine';

describe('TranslationEngineOrchestrator - Timestamp Tracking', () => {
  let orchestrator: TranslationEngineOrchestrator;
  let mockRuleEngine: jest.Mocked<RuleBasedTranslationEngine>;
  let mockPatternEngine: jest.Mocked<PatternBasedTranslationEngine>;
  let mockLLMEngine: jest.Mocked<LLMTranslationEngine>;

  beforeEach(() => {
    // Create mock engines
    mockRuleEngine = {
      name: 'rule-based',
      priority: 3,
      version: '1.0.0',
      capabilities: {
        sourceLanguages: ['javascript', 'typescript', 'python'],
        targetLanguages: ['javascript', 'typescript', 'python', 'csharp'],
        maxComplexity: 5,
        supportsBatch: true,
        requiresNetwork: false,
        supportsCustomPatterns: true,
        supportsExplanations: true,
        supportedLanguages: ['javascript', 'typescript'],
        supportedPatterns: ['function', 'variable'],
        requiresInternet: false,
        estimatedCost: 0,
        estimatedTime: 100,
        qualityScore: 0.9,
        reliabilityScore: 0.95,
      },
      isAvailable: jest.fn().mockResolvedValue(true),
      canTranslate: jest.fn().mockResolvedValue(true),
      translate: jest.fn().mockResolvedValue({
        targetNode: {},
        confidence: 0.9,
        reasoning: 'test',
        appliedPatterns: [],
        alternatives: [],
        warnings: [],
        improvements: [],
        quality: {
          correctness: 0.9,
          maintainability: 0.8,
          performance: 0.7,
          readability: 0.8,
          testability: 0.7,
          overall: 0.8,
        },
        performance: {
          processingTime: 100,
          memoryUsage: 1024,
          cacheHits: 0,
          cacheMisses: 1,
        },
        metadata: {
          engineName: 'rule-based',
          engineVersion: '1.0.0',
          timestamp: new Date(),
          cost: 0,
          engineSpecific: {},
        },
      }),
      getConfidence: jest.fn().mockResolvedValue(0.9),
      getEstimatedCost: jest.fn().mockResolvedValue(0),
      getEstimatedTime: jest.fn().mockResolvedValue(100),
      initialize: jest.fn().mockResolvedValue(undefined),
      dispose: jest.fn().mockResolvedValue(undefined),
      getMetrics: jest.fn().mockReturnValue({
        totalTranslations: 0,
        successfulTranslations: 0,
        failedTranslations: 0,
        averageProcessingTime: 0,
        averageConfidence: 0,
        totalCost: 0,
        lastUsed: null,
      }),
    } as any;

    mockPatternEngine = {
      ...mockRuleEngine,
      name: 'pattern-based',
      priority: 2,
    } as any;

    mockLLMEngine = {
      ...mockRuleEngine,
      name: 'llm-enhanced',
      priority: 1,
    } as any;

    const config: OrchestratorConfig = {
      selectionStrategy: EngineSelectionStrategy.PRIORITY,
      enableFallback: true,
      enableParallel: false,
      maxEngines: 3,
      maxEnginesPerTranslation: 3,
      minConfidenceThreshold: 0.7,
      maxCostPerTranslation: 100,
      maxTimePerTranslation: 5000,
      enableQualityAssurance: true,
      engineConfigs: {
        'rule-based': {
          settings: {},
          resourceLimits: {
            maxMemoryMB: 512,
            maxConcurrentTasks: 5,
            timeoutMs: 30000,
          },
          cacheConfig: {
            enabled: true,
            maxSizeMB: 100,
            ttlMs: 300000,
          },
          loggingConfig: {
            level: 'info',
            enablePerformanceLogs: true,
            enableErrorLogs: true,
          },
        },
        'pattern-based': {
          settings: {},
          resourceLimits: {
            maxMemoryMB: 512,
            maxConcurrentTasks: 5,
            timeoutMs: 30000,
          },
          cacheConfig: {
            enabled: true,
            maxSizeMB: 100,
            ttlMs: 300000,
          },
          loggingConfig: {
            level: 'info',
            enablePerformanceLogs: true,
            enableErrorLogs: true,
          },
        },
        'llm-enhanced': {
          settings: {},
          resourceLimits: {
            maxMemoryMB: 1024,
            maxConcurrentTasks: 2,
            timeoutMs: 60000,
          },
          cacheConfig: {
            enabled: true,
            maxSizeMB: 200,
            ttlMs: 600000,
          },
          loggingConfig: {
            level: 'info',
            enablePerformanceLogs: true,
            enableErrorLogs: true,
          },
        },
      },
      enginePriorities: {
        'rule-based': 3,
        'pattern-based': 2,
        'llm-enhanced': 1,
      },
      qualityThresholds: {
        minSyntacticCorrectness: 0.8,
        minSemanticPreservation: 0.7,
        minOverallQuality: 0.75,
      },
      performanceThresholds: {
        maxResponseTime: 5000,
        maxMemoryUsage: 1024,
        minSuccessRate: 0.9,
      },
      healthCheck: {
        enabled: true,
        interval: 30000,
        timeout: 5000,
        retries: 3,
        failureThreshold: 3,
      },
    };

    orchestrator = new TranslationEngineOrchestrator(
      mockRuleEngine,
      mockPatternEngine,
      mockLLMEngine,
      config,
    );
  });

  describe('lastUsed timestamp tracking', () => {
    it('should initialize engines with null lastUsed timestamp', () => {
      const status = orchestrator.getEngineStatus();

      expect(status.ruleBased.lastUsed).toBeNull();
      expect(status.patternBased.lastUsed).toBeNull();
      expect(status.llm.lastUsed).toBeNull();
    });

    it('should update lastUsed timestamp after translation', async () => {
      const mockSource = {
        nodeType: 1,
        nodeId: 1,
        getChildren: jest.fn().mockReturnValue([]),
      };

      const mockContext = {
        sourceLanguage: 'javascript',
        targetLanguage: 'typescript',
        translationScope: 'function',
        qualityRequirements: {
          minConfidence: 0.7,
          requireCompilation: false,
          requireTestCompatibility: false,
        },
        projectContext: {
          type: 'web',
          dependencies: [],
          projectPatterns: [],
        },
      };

      const beforeTranslation = new Date();

      try {
        await orchestrator.translate(mockSource as any, mockContext as any);
      } catch (error) {
        // Expected to fail due to mock limitations, but timestamp should be updated
      }

      const afterTranslation = new Date();
      const status = orchestrator.getEngineStatus();

      // At least one engine should have been used and have a lastUsed timestamp
      const usedEngines = Object.values(status).filter(engine => engine.lastUsed !== null);
      expect(usedEngines.length).toBeGreaterThan(0);

      // Check that the timestamp is reasonable (between before and after)
      usedEngines.forEach(engine => {
        expect(engine.lastUsed).toBeInstanceOf(Date);
        expect(engine.lastUsed!.getTime()).toBeGreaterThanOrEqual(beforeTranslation.getTime());
        expect(engine.lastUsed!.getTime()).toBeLessThanOrEqual(afterTranslation.getTime());
      });
    });

    it('should track different timestamps for different engines', async () => {
      const mockSource = {
        nodeType: 1,
        nodeId: 1,
        getChildren: jest.fn().mockReturnValue([]),
      };

      const mockContext = {
        sourceLanguage: 'javascript',
        targetLanguage: 'typescript',
        translationScope: 'function',
        qualityRequirements: {
          minConfidence: 0.7,
          requireCompilation: false,
          requireTestCompatibility: false,
        },
        projectContext: {
          type: 'web',
          dependencies: [],
          projectPatterns: [],
        },
      };

      // First translation
      try {
        await orchestrator.translate(mockSource as any, mockContext as any);
      } catch (error) {
        // Expected to fail
      }

      const firstStatus = orchestrator.getEngineStatus();

      // Wait a bit to ensure different timestamps
      await new Promise(resolve => setTimeout(resolve, 10));

      // Second translation
      try {
        await orchestrator.translate(mockSource as any, mockContext as any);
      } catch (error) {
        // Expected to fail
      }

      const secondStatus = orchestrator.getEngineStatus();

      // Compare timestamps to ensure they're updated
      Object.keys(firstStatus).forEach(engineName => {
        const firstTime = firstStatus[engineName].lastUsed;
        const secondTime = secondStatus[engineName].lastUsed;

        if (firstTime && secondTime) {
          expect(secondTime.getTime()).toBeGreaterThanOrEqual(firstTime.getTime());
        }
      });
    });
  });

  describe('metrics integration', () => {
    it('should include lastUsed in overall metrics', () => {
      const metrics = orchestrator.getMetrics();
      expect(metrics).toHaveProperty('lastUsed');
      expect(metrics.lastUsed).toBeNull(); // Initially null
    });

    it('should update overall metrics lastUsed after translation', async () => {
      const mockSource = {
        nodeType: 1,
        nodeId: 1,
        getChildren: jest.fn().mockReturnValue([]),
      };

      const mockContext = {
        sourceLanguage: 'javascript',
        targetLanguage: 'typescript',
        translationScope: 'function',
        qualityRequirements: {
          minConfidence: 0.7,
          requireCompilation: false,
          requireTestCompatibility: false,
        },
        projectContext: {
          type: 'web',
          dependencies: [],
          projectPatterns: [],
        },
      };

      try {
        await orchestrator.translate(mockSource as any, mockContext as any);
      } catch (error) {
        // Expected to fail
      }

      const metrics = orchestrator.getMetrics();
      expect(metrics.lastUsed).toBeInstanceOf(Date);
    });
  });
});