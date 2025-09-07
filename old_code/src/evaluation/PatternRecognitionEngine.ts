/**
 * PatternRecognitionEngine - Phase 3 of Project Golem
 *
 * Advanced pattern recognition system that learns from correction patterns,
 * code structures, and user feedback to improve correction accuracy over time.
 * Uses machine learning techniques to identify recurring error patterns and
 * optimal correction strategies.
 */

import { StructuredValidationError, ErrorType } from './StructuredValidationError';
import { CorrectionSolution, SolutionType } from './MultiSolutionGenerator';
import { SelectionResult } from './SolutionSelectionEngine';
import { ASTContext } from './GrammarDrivenASTMapper';
import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';

export interface ErrorPattern {
  id: string;
  errorType: ErrorType;
  contextPattern: CodePattern;
  frequency: number;
  successfulSolutions: SolutionPattern[];
  failedSolutions: SolutionPattern[];
  confidence: number;
  lastSeen: Date;
  metadata: PatternMetadata;
}

export interface CodePattern {
  syntacticPattern: string;
  semanticPattern: string;
  structuralFeatures: StructuralFeature[];
  contextFeatures: ContextFeature[];
  astSignature: string;
  complexity: number;
}

export interface StructuralFeature {
  type: 'function' | 'class' | 'loop' | 'conditional' | 'assignment' | 'call' | 'import';
  depth: number;
  position: 'before' | 'after' | 'inside' | 'parent';
  properties: Map<string, any>;
}

export interface ContextFeature {
  type: 'variable_scope' | 'import_context' | 'function_context' | 'class_context' | 'module_context';
  value: string;
  relevance: number;
}

export interface SolutionPattern {
  solutionType: SolutionType;
  transformationPattern: string;
  successRate: number;
  averageConfidence: number;
  contextRequirements: string[];
  sideEffects: string[];
  performanceImpact: number;
}

export interface PatternMetadata {
  sourceFiles: string[];
  userProfiles: string[];
  projectTypes: string[];
  pythonVersions: string[];
  correctionOutcomes: CorrectionOutcome[];
}

export interface CorrectionOutcome {
  timestamp: Date;
  success: boolean;
  userSatisfaction: number; // 1-5 scale
  timeToFix: number;
  subsequentErrors: number;
  userFeedback: string;
}

export interface PatternLearningConfig {
  enableRealTimeLearning: boolean;
  patternExtractionThreshold: number;
  minimumOccurrences: number;
  confidenceDecayRate: number;
  maxPatternsPerErrorType: number;
  learningRate: number;
  featureWeights: Map<string, number>;
}

export interface PatternMatchResult {
  pattern: ErrorPattern;
  matchConfidence: number;
  recommendedSolutions: SolutionPattern[];
  contextAlignment: number;
  adaptationSuggestions: string[];
}

export interface LearningMetrics {
  totalPatterns: number;
  patternsLearned: number;
  patternAccuracy: number;
  predictionAccuracy: number;
  learningEfficiency: number;
  adaptationRate: number;
  userSatisfactionTrend: number;
}

/**
 * PatternRecognitionEngine - Advanced pattern learning and recognition
 */
export class PatternRecognitionEngine {
  private patterns: Map<string, ErrorPattern>;
  private config: PatternLearningConfig;
  private learningHistory: CorrectionOutcome[];
  private featureExtractor: FeatureExtractor;
  private patternMatcher: PatternMatcher;
  private learningMetrics: LearningMetrics;

  constructor(config: Partial<PatternLearningConfig> = {}) {
    this.config = {
      enableRealTimeLearning: true,
      patternExtractionThreshold: 0.7,
      minimumOccurrences: 3,
      confidenceDecayRate: 0.95,
      maxPatternsPerErrorType: 20,
      learningRate: 0.1,
      featureWeights: new Map([
        ['syntactic', 0.3],
        ['semantic', 0.3],
        ['structural', 0.2],
        ['contextual', 0.2],
      ]),
      ...config,
    };

    this.patterns = new Map();
    this.learningHistory = [];
    this.featureExtractor = new FeatureExtractor();
    this.patternMatcher = new PatternMatcher(this.config);
    this.learningMetrics = this.initializeLearningMetrics();
  }

  /**
   * Learn from a correction outcome
   */
  async learnFromCorrection(
    error: StructuredValidationError,
    context: ASTContext,
    appliedSolution: CorrectionSolution,
    selectionResult: SelectionResult,
    outcome: CorrectionOutcome,
  ): Promise<void> {

    if (!this.config.enableRealTimeLearning) {
      return;
    }

    try {
      // Extract patterns from the error and context
      const codePattern = await this.featureExtractor.extractCodePattern(error, context);
      const patternId = this.generatePatternId(error.type, codePattern);

      // Get or create error pattern
      let errorPattern = this.patterns.get(patternId);
      if (!errorPattern) {
        errorPattern = this.createNewPattern(patternId, error.type, codePattern);
        this.patterns.set(patternId, errorPattern);
      }

      // Update pattern with new outcome
      await this.updatePatternWithOutcome(errorPattern, appliedSolution, outcome);

      // Update learning metrics
      this.updateLearningMetrics(outcome);

      // Store learning history
      this.learningHistory.push(outcome);

      // Prune old or low-confidence patterns
      await this.prunePatterns();

    } catch (error) {
      console.error('Pattern learning failed:', error);
    }
  }

  /**
   * Find matching patterns for an error
   */
  async findMatchingPatterns(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<PatternMatchResult[]> {

    const codePattern = await this.featureExtractor.extractCodePattern(error, context);
    const matchResults: PatternMatchResult[] = [];

    // Find patterns for this error type
    const relevantPatterns = Array.from(this.patterns.values())
      .filter(pattern => pattern.errorType === error.type);

    for (const pattern of relevantPatterns) {
      const matchConfidence = await this.patternMatcher.calculateMatchConfidence(
        codePattern,
        pattern.contextPattern,
      );

      if (matchConfidence >= this.config.patternExtractionThreshold) {
        const contextAlignment = await this.calculateContextAlignment(context, pattern);

        const matchResult: PatternMatchResult = {
          pattern,
          matchConfidence,
          recommendedSolutions: pattern.successfulSolutions.slice(0, 3), // Top 3
          contextAlignment,
          adaptationSuggestions: await this.generateAdaptationSuggestions(pattern, context),
        };

        matchResults.push(matchResult);
      }
    }

    // Sort by match confidence
    matchResults.sort((a, b) => b.matchConfidence - a.matchConfidence);

    return matchResults;
  }

  /**
   * Predict best solution based on learned patterns
   */
  async predictBestSolution(
    error: StructuredValidationError,
    context: ASTContext,
    availableSolutions: CorrectionSolution[],
  ): Promise<{ solution: CorrectionSolution; confidence: number; reasoning: string }> {

    const matchingPatterns = await this.findMatchingPatterns(error, context);

    if (matchingPatterns.length === 0) {
      // No patterns found - fallback to highest confidence solution
      const bestSolution = availableSolutions.reduce((best, current) =>
        current.confidence > best.confidence ? current : best,
      );

      return {
        solution: bestSolution,
        confidence: bestSolution.confidence * 0.5, // Reduced confidence due to no pattern match
        reasoning: 'No matching patterns found. Selected highest confidence solution.',
      };
    }

    // Score solutions based on pattern recommendations
    const solutionScores = new Map<string, number>();
    const solutionReasons = new Map<string, string[]>();

    for (const solution of availableSolutions) {
      let totalScore = solution.confidence * 0.3; // Base confidence
      const reasons: string[] = [];

      for (const matchResult of matchingPatterns) {
        const patternWeight = matchResult.matchConfidence * matchResult.contextAlignment;

        // Find matching solution patterns
        const matchingSolutionPattern = matchResult.recommendedSolutions.find(
          sp => sp.solutionType === solution.type,
        );

        if (matchingSolutionPattern) {
          const patternScore = matchingSolutionPattern.successRate * patternWeight;
          totalScore += patternScore;
          reasons.push(
            `Pattern match (${(matchResult.matchConfidence * 100).toFixed(1)}% confidence) ` +
            `recommends ${solution.type} with ${(matchingSolutionPattern.successRate * 100).toFixed(1)}% success rate`,
          );
        }
      }

      solutionScores.set(solution.id, totalScore);
      solutionReasons.set(solution.id, reasons);
    }

    // Find best scoring solution
    let bestSolution = availableSolutions[0];
    let bestScore = solutionScores.get(bestSolution.id) || 0;

    for (const solution of availableSolutions) {
      const score = solutionScores.get(solution.id) || 0;
      if (score > bestScore) {
        bestScore = score;
        bestSolution = solution;
      }
    }

    const reasoning = solutionReasons.get(bestSolution.id)?.join('. ') || 'Pattern-based selection';

    return {
      solution: bestSolution,
      confidence: Math.min(bestScore, 1.0),
      reasoning,
    };
  }

  /**
   * Analyze correction trends and patterns
   */
  async analyzeCorrectionTrends(): Promise<{
    errorTypeFrequency: Map<ErrorType, number>;
    solutionTypeEffectiveness: Map<SolutionType, number>;
    contextPatternSuccess: Map<string, number>;
    userSatisfactionTrends: number[];
    improvementAreas: string[];
  }> {

    const errorTypeFrequency = new Map<ErrorType, number>();
    const solutionTypeEffectiveness = new Map<SolutionType, number>();
    const contextPatternSuccess = new Map<string, number>();
    const userSatisfactionTrends: number[] = [];

    // Analyze patterns
    for (const pattern of this.patterns.values()) {
      // Error type frequency
      errorTypeFrequency.set(
        pattern.errorType,
        (errorTypeFrequency.get(pattern.errorType) || 0) + pattern.frequency,
      );

      // Solution type effectiveness
      for (const solutionPattern of pattern.successfulSolutions) {
        solutionTypeEffectiveness.set(
          solutionPattern.solutionType,
          Math.max(
            solutionTypeEffectiveness.get(solutionPattern.solutionType) || 0,
            solutionPattern.successRate,
          ),
        );
      }

      // Context pattern success
      contextPatternSuccess.set(
        pattern.contextPattern.syntacticPattern,
        pattern.confidence,
      );
    }

    // User satisfaction trends (last 30 outcomes)
    const recentOutcomes = this.learningHistory.slice(-30);
    for (let i = 0; i < recentOutcomes.length; i += 5) {
      const batch = recentOutcomes.slice(i, i + 5);
      const avgSatisfaction = batch.reduce((sum, outcome) => sum + outcome.userSatisfaction, 0) / batch.length;
      userSatisfactionTrends.push(avgSatisfaction);
    }

    // Identify improvement areas
    const improvementAreas: string[] = [];

    // Low success rate solution types
    for (const [solutionType, effectiveness] of solutionTypeEffectiveness) {
      if (effectiveness < 0.6) {
        improvementAreas.push(`Improve ${solutionType} solution effectiveness (currently ${(effectiveness * 100).toFixed(1)}%)`);
      }
    }

    // Declining user satisfaction
    if (userSatisfactionTrends.length >= 2) {
      const recent = userSatisfactionTrends[userSatisfactionTrends.length - 1];
      const previous = userSatisfactionTrends[userSatisfactionTrends.length - 2];
      if (recent < previous - 0.2) {
        improvementAreas.push('User satisfaction declining - review recent corrections');
      }
    }

    return {
      errorTypeFrequency,
      solutionTypeEffectiveness,
      contextPatternSuccess,
      userSatisfactionTrends,
      improvementAreas,
    };
  }

  /**
   * Export learned patterns for analysis or backup
   */
  exportPatterns(): any {
    return {
      patterns: Array.from(this.patterns.entries()),
      config: this.config,
      metrics: this.learningMetrics,
      learningHistory: this.learningHistory.slice(-100), // Last 100 outcomes
    };
  }

  /**
   * Import patterns from backup or external source
   */
  importPatterns(data: any): void {
    if (data.patterns) {
      this.patterns = new Map(data.patterns);
    }
    if (data.config) {
      this.config = { ...this.config, ...data.config };
    }
    if (data.learningHistory) {
      this.learningHistory = data.learningHistory;
    }
  }

  /**
   * Get learning statistics
   */
  getLearningStatistics(): LearningMetrics {
    return { ...this.learningMetrics };
  }

  // Private helper methods

  private generatePatternId(errorType: ErrorType, codePattern: CodePattern): string {
    const hash = this.hashCodePattern(codePattern);
    return `${errorType}-${hash}`;
  }

  private hashCodePattern(pattern: CodePattern): string {
    const combined = pattern.syntacticPattern + pattern.semanticPattern + pattern.astSignature;
    return combined.split('').reduce((hash, char) => {
      const charCode = char.charCodeAt(0);
      hash = ((hash << 5) - hash) + charCode;
      return hash & hash; // Convert to 32-bit integer
    }, 0).toString(36);
  }

  private createNewPattern(id: string, errorType: ErrorType, codePattern: CodePattern): ErrorPattern {
    return {
      id,
      errorType,
      contextPattern: codePattern,
      frequency: 1,
      successfulSolutions: [],
      failedSolutions: [],
      confidence: 0.5,
      lastSeen: new Date(),
      metadata: {
        sourceFiles: [],
        userProfiles: [],
        projectTypes: [],
        pythonVersions: [],
        correctionOutcomes: [],
      },
    };
  }

  private async updatePatternWithOutcome(
    pattern: ErrorPattern,
    solution: CorrectionSolution,
    outcome: CorrectionOutcome,
  ): Promise<void> {

    pattern.frequency++;
    pattern.lastSeen = new Date();
    pattern.metadata.correctionOutcomes.push(outcome);

    // Find or create solution pattern
    let solutionPattern = pattern.successfulSolutions.find(sp => sp.solutionType === solution.type);
    if (!solutionPattern) {
      solutionPattern = {
        solutionType: solution.type,
        transformationPattern: solution.astTransformation.nodeType || 'unknown',
        successRate: 0,
        averageConfidence: 0,
        contextRequirements: [],
        sideEffects: [],
        performanceImpact: 0,
      };
    }

    // Update solution pattern based on outcome
    if (outcome.success) {
      if (!pattern.successfulSolutions.includes(solutionPattern)) {
        pattern.successfulSolutions.push(solutionPattern);
      }

      // Update success rate using exponential moving average
      const alpha = this.config.learningRate;
      solutionPattern.successRate = alpha * 1.0 + (1 - alpha) * solutionPattern.successRate;
      solutionPattern.averageConfidence = alpha * solution.confidence + (1 - alpha) * solutionPattern.averageConfidence;
    } else {
      if (!pattern.failedSolutions.includes(solutionPattern)) {
        pattern.failedSolutions.push(solutionPattern);
      }

      // Decrease success rate
      const alpha = this.config.learningRate;
      solutionPattern.successRate = (1 - alpha) * solutionPattern.successRate;
    }

    // Update pattern confidence based on recent outcomes
    const recentOutcomes = pattern.metadata.correctionOutcomes.slice(-10);
    const successRate = recentOutcomes.filter(o => o.success).length / recentOutcomes.length;
    pattern.confidence = successRate;
  }

  private async calculateContextAlignment(context: ASTContext, pattern: ErrorPattern): Promise<number> {
    // Calculate how well the current context aligns with the pattern's context
    let alignment = 0.5; // Base alignment

    // Compare structural features
    const currentFeatures = await this.featureExtractor.extractStructuralFeatures(context);
    const patternFeatures = pattern.contextPattern.structuralFeatures;

    const matchingFeatures = currentFeatures.filter(cf =>
      patternFeatures.some(pf => pf.type === cf.type && pf.position === cf.position),
    );

    if (patternFeatures.length > 0) {
      alignment += (matchingFeatures.length / patternFeatures.length) * 0.3;
    }

    // Compare contextual features
    const currentContextFeatures = await this.featureExtractor.extractContextFeatures(context);
    const patternContextFeatures = pattern.contextPattern.contextFeatures;

    const matchingContextFeatures = currentContextFeatures.filter(ccf =>
      patternContextFeatures.some(pcf => pcf.type === ccf.type && pcf.value === ccf.value),
    );

    if (patternContextFeatures.length > 0) {
      alignment += (matchingContextFeatures.length / patternContextFeatures.length) * 0.2;
    }

    return Math.min(alignment, 1.0);
  }

  private async generateAdaptationSuggestions(pattern: ErrorPattern, context: ASTContext): Promise<string[]> {
    const suggestions: string[] = [];

    // Suggest based on successful solutions
    if (pattern.successfulSolutions.length > 0) {
      const bestSolution = pattern.successfulSolutions.reduce((best, current) =>
        current.successRate > best.successRate ? current : best,
      );

      suggestions.push(`Consider ${bestSolution.solutionType} approach (${(bestSolution.successRate * 100).toFixed(1)}% success rate)`);
    }

    // Suggest based on context requirements
    for (const solution of pattern.successfulSolutions) {
      if (solution.contextRequirements.length > 0) {
        suggestions.push(`Ensure context requirements: ${solution.contextRequirements.join(', ')}`);
      }
    }

    return suggestions;
  }

  private updateLearningMetrics(outcome: CorrectionOutcome): void {
    this.learningMetrics.patternsLearned = this.patterns.size;

    // Update user satisfaction trend
    const recentOutcomes = this.learningHistory.slice(-20);
    if (recentOutcomes.length > 0) {
      this.learningMetrics.userSatisfactionTrend =
        recentOutcomes.reduce((sum, o) => sum + o.userSatisfaction, 0) / recentOutcomes.length;
    }

    // Update prediction accuracy (simplified)
    const successfulOutcomes = recentOutcomes.filter(o => o.success).length;
    this.learningMetrics.predictionAccuracy = successfulOutcomes / Math.max(recentOutcomes.length, 1);
  }

  private async prunePatterns(): Promise<void> {
    // Remove patterns with very low confidence or frequency
    const patternsToRemove: string[] = [];

    for (const [id, pattern] of this.patterns) {
      if (pattern.confidence < 0.1 || pattern.frequency < this.config.minimumOccurrences) {
        patternsToRemove.push(id);
      }
    }

    for (const id of patternsToRemove) {
      this.patterns.delete(id);
    }

    // Limit patterns per error type
    const patternsByType = new Map<ErrorType, ErrorPattern[]>();
    for (const pattern of this.patterns.values()) {
      if (!patternsByType.has(pattern.errorType)) {
        patternsByType.set(pattern.errorType, []);
      }
      patternsByType.get(pattern.errorType)!.push(pattern);
    }

    for (const [errorType, patterns] of patternsByType) {
      if (patterns.length > this.config.maxPatternsPerErrorType) {
        // Keep only the best patterns
        patterns.sort((a, b) => b.confidence - a.confidence);
        const toRemove = patterns.slice(this.config.maxPatternsPerErrorType);

        for (const pattern of toRemove) {
          this.patterns.delete(pattern.id);
        }
      }
    }
  }

  private initializeLearningMetrics(): LearningMetrics {
    return {
      totalPatterns: 0,
      patternsLearned: 0,
      patternAccuracy: 0,
      predictionAccuracy: 0,
      learningEfficiency: 0,
      adaptationRate: 0,
      userSatisfactionTrend: 0,
    };
  }
}

/**
 * FeatureExtractor - Extracts features from code and context
 */
class FeatureExtractor {
  async extractCodePattern(error: StructuredValidationError, context: ASTContext): Promise<CodePattern> {
    const syntacticPattern = await this.extractSyntacticPattern(error, context);
    const semanticPattern = await this.extractSemanticPattern(error, context);
    const structuralFeatures = await this.extractStructuralFeatures(context);
    const contextFeatures = await this.extractContextFeatures(context);
    const astSignature = this.generateASTSignature(context.ast);
    const complexity = this.calculateComplexity(context);

    return {
      syntacticPattern,
      semanticPattern,
      structuralFeatures,
      contextFeatures,
      astSignature,
      complexity,
    };
  }

  async extractSyntacticPattern(error: StructuredValidationError, context: ASTContext): Promise<string> {
    // Extract syntactic pattern around the error
    const errorLine = error.location.line;
    const lines = context.sourceCode.split('\n');
    const contextLines = lines.slice(Math.max(0, errorLine - 2), errorLine + 3);

    // Normalize the pattern (remove specific identifiers, keep structure)
    return contextLines.map(line => this.normalizeLine(line)).join('\n');
  }

  async extractSemanticPattern(error: StructuredValidationError, context: ASTContext): Promise<string> {
    // Extract semantic pattern (variable types, function calls, etc.)
    return `${error.type}:${error.message.replace(/['"]\w+['"]/, '"VAR"')}`;
  }

  async extractStructuralFeatures(context: ASTContext): Promise<StructuralFeature[]> {
    const features: StructuralFeature[] = [];

    // Analyze AST structure around error
    if (context.errorNode) {
      features.push({
        type: this.mapASTNodeToFeatureType(context.errorNode.nodeType.toString()),
        depth: this.calculateNodeDepth(context.errorNode),
        position: 'inside',
        properties: new Map([['nodeType', context.errorNode.nodeType]]),
      });
    }

    return features;
  }

  async extractContextFeatures(context: ASTContext): Promise<ContextFeature[]> {
    const features: ContextFeature[] = [];

    // Extract scope information
    for (const scope of context.scopeStack) {
      features.push({
        type: 'variable_scope',
        value: scope,
        relevance: 0.8,
      });
    }

    return features;
  }

  private normalizeLine(line: string): string {
    return line
      .replace(/\b[a-zA-Z_][a-zA-Z0-9_]*\b/g, 'VAR') // Replace identifiers
      .replace(/\b\d+\b/g, 'NUM') // Replace numbers
      .replace(/["'].*?["']/g, 'STR') // Replace strings
      .trim();
  }

  private generateASTSignature(ast: ZeroCopyASTNode): string {
    // Generate a signature representing the AST structure
    return this.generateNodeSignature(ast);
  }

  private generateNodeSignature(node: ZeroCopyASTNode): string {
    const childSignatures = node.getChildren()?.map(child => this.generateNodeSignature(child)).join(',') || '';
    return `${node.nodeType}(${childSignatures})`;
  }

  private calculateComplexity(context: ASTContext): number {
    // Calculate code complexity around the error
    let complexity = 1;

    if (context.errorNode) {
      complexity += this.calculateNodeDepth(context.errorNode);
      complexity += context.errorNode.getChildren()?.length || 0;
    }

    return complexity;
  }

  private calculateNodeDepth(node: ZeroCopyASTNode): number {
    let depth = 0;
    let current = node.getParent();

    while (current) {
      depth++;
      current = current.getParent();
    }

    return depth;
  }

  private mapASTNodeToFeatureType(nodeType: string): StructuralFeature['type'] {
    const mapping: Record<string, StructuralFeature['type']> = {
      'function_def': 'function',
      'class_def': 'class',
      'for_stmt': 'loop',
      'while_stmt': 'loop',
      'if_stmt': 'conditional',
      'assign': 'assignment',
      'call': 'call',
      'import_stmt': 'import',
    };

    return mapping[nodeType] || 'assignment';
  }
}

/**
 * PatternMatcher - Matches patterns with confidence scoring
 */
class PatternMatcher {
  constructor(private config: PatternLearningConfig) {}

  async calculateMatchConfidence(
    currentPattern: CodePattern,
    storedPattern: CodePattern,
  ): Promise<number> {

    let confidence = 0;

    // Syntactic similarity
    const syntacticSimilarity = this.calculateStringSimilarity(
      currentPattern.syntacticPattern,
      storedPattern.syntacticPattern,
    );
    confidence += syntacticSimilarity * (this.config.featureWeights.get('syntactic') || 0.3);

    // Semantic similarity
    const semanticSimilarity = this.calculateStringSimilarity(
      currentPattern.semanticPattern,
      storedPattern.semanticPattern,
    );
    confidence += semanticSimilarity * (this.config.featureWeights.get('semantic') || 0.3);

    // Structural similarity
    const structuralSimilarity = this.calculateStructuralSimilarity(
      currentPattern.structuralFeatures,
      storedPattern.structuralFeatures,
    );
    confidence += structuralSimilarity * (this.config.featureWeights.get('structural') || 0.2);

    // Contextual similarity
    const contextualSimilarity = this.calculateContextualSimilarity(
      currentPattern.contextFeatures,
      storedPattern.contextFeatures,
    );
    confidence += contextualSimilarity * (this.config.featureWeights.get('contextual') || 0.2);

    return Math.min(confidence, 1.0);
  }

  private calculateStringSimilarity(str1: string, str2: string): number {
    // Simple Levenshtein distance-based similarity
    const distance = this.levenshteinDistance(str1, str2);
    const maxLength = Math.max(str1.length, str2.length);
    return maxLength === 0 ? 1 : 1 - (distance / maxLength);
  }

  private calculateStructuralSimilarity(
    features1: StructuralFeature[],
    features2: StructuralFeature[],
  ): number {

    if (features1.length === 0 && features2.length === 0) {
return 1;
}
    if (features1.length === 0 || features2.length === 0) {
return 0;
}

    const matches = features1.filter(f1 =>
      features2.some(f2 => f1.type === f2.type && f1.position === f2.position),
    );

    return matches.length / Math.max(features1.length, features2.length);
  }

  private calculateContextualSimilarity(
    features1: ContextFeature[],
    features2: ContextFeature[],
  ): number {

    if (features1.length === 0 && features2.length === 0) {
return 1;
}
    if (features1.length === 0 || features2.length === 0) {
return 0;
}

    const matches = features1.filter(f1 =>
      features2.some(f2 => f1.type === f2.type && f1.value === f2.value),
    );

    return matches.length / Math.max(features1.length, features2.length);
  }

  private levenshteinDistance(str1: string, str2: string): number {
    const matrix = Array(str2.length + 1).fill(null).map(() => Array(str1.length + 1).fill(null));

    for (let i = 0; i <= str1.length; i++) {
matrix[0][i] = i;
}
    for (let j = 0; j <= str2.length; j++) {
matrix[j][0] = j;
}

    for (let j = 1; j <= str2.length; j++) {
      for (let i = 1; i <= str1.length; i++) {
        const indicator = str1[i - 1] === str2[j - 1] ? 0 : 1;
        matrix[j][i] = Math.min(
          matrix[j][i - 1] + 1,     // deletion
          matrix[j - 1][i] + 1,     // insertion
          matrix[j - 1][i - 1] + indicator, // substitution
        );
      }
    }

    return matrix[str2.length][str1.length];
  }
}

