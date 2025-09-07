/**
 * AdaptiveLearningSystem - Phase 3 Advanced Learning
 *
 * Real-time adaptive learning system that continuously improves correction
 * accuracy based on user feedback, correction outcomes, and pattern analysis.
 * Integrates with the agentic interface to optimize deterministic progression.
 */

import { AgenticCorrectionInterface, CorrectionStep, DeterminismLevel, CorrectionStepType } from './AgenticCorrectionInterface';
import { StructuredValidationError, ErrorType } from './StructuredValidationError';
import { CorrectionSolution, SolutionType } from './MultiSolutionGenerator';
import { ASTContext } from './GrammarDrivenASTMapper';
import { CorrectionOutcome } from './PatternRecognitionEngine';

export interface OutcomePattern {
  patternId: string;
  errorType: ErrorType;
  correctionApproach: CorrectionStepType;
  successRate: number;
  averageTime: number;
  confidence: number;
}

export interface AdaptiveLearningConfig {
  enableRealTimeAdaptation: boolean;
  learningRate: number;
  feedbackWeight: number;
  outcomeWeight: number;
  patternDecayRate: number;
  adaptationThreshold: number;
  maxLearningHistory: number;
  enableUserFeedbackLearning: boolean;
  enableOutcomeBasedLearning: boolean;
  enablePatternEvolution: boolean;
}

export interface UserFeedback {
  correctionId: string;
  userId: string;
  timestamp: Date;
  rating: number; // 1-5 scale
  feedbackType: FeedbackType;
  comments: string;
  suggestedImprovement?: string;
  wouldUseAgain: boolean;
  timeToReview: number;
}

export enum FeedbackType {
  CORRECTION_QUALITY = 'correction_quality',
  APPROACH_PREFERENCE = 'approach_preference',
  PERFORMANCE_FEEDBACK = 'performance',
  SUGGESTION = 'suggestion',
  BUG_REPORT = 'bug_report'
}

export interface LearningEvent {
  id: string;
  timestamp: Date;
  eventType: LearningEventType;
  errorType: ErrorType;
  correctionApproach: CorrectionStepType;
  determinismLevel: DeterminismLevel;
  outcome: CorrectionOutcome;
  userFeedback?: UserFeedback;
  adaptationApplied: AdaptationAction[];
  learningMetrics: LearningEventMetrics;
}

export enum LearningEventType {
  CORRECTION_SUCCESS = 'correction_success',
  CORRECTION_FAILURE = 'correction_failure',
  USER_FEEDBACK_RECEIVED = 'user_feedback',
  PATTERN_DISCOVERED = 'pattern_discovered',
  APPROACH_OPTIMIZED = 'approach_optimized',
  THRESHOLD_ADJUSTED = 'threshold_adjusted'
}

export interface AdaptationAction {
  actionType: AdaptationActionType;
  target: string;
  oldValue: any;
  newValue: any;
  confidence: number;
  reasoning: string;
}

export enum AdaptationActionType {
  THRESHOLD_ADJUSTMENT = 'threshold_adjustment',
  PATTERN_WEIGHT_UPDATE = 'pattern_weight_update',
  APPROACH_PREFERENCE_UPDATE = 'approach_preference',
  RULE_CONFIDENCE_UPDATE = 'rule_confidence_update',
  LEARNING_RATE_ADJUSTMENT = 'learning_rate_adjustment'
}

export interface LearningEventMetrics {
  accuracyImprovement: number;
  performanceImpact: number;
  userSatisfactionChange: number;
  deterministicRatioChange: number;
  adaptationEffectiveness: number;
}

export interface AdaptationRecommendation {
  priority: number;
  actionType: AdaptationActionType;
  target: string;
  recommendedValue: any;
  expectedImpact: number;
  reasoning: string;
  confidence: number;
}

export interface LearningAnalytics {
  totalLearningEvents: number;
  learningVelocity: number;
  adaptationSuccess: number;
  userSatisfactionTrend: number[];
  accuracyTrend: number[];
  deterministicProgressionTrend: number[];
  topPerformingApproaches: Map<ErrorType, CorrectionStepType>;
  improvementOpportunities: AdaptationRecommendation[];
}

/**
 * AdaptiveLearningSystem - Continuous improvement through learning
 */
export class AdaptiveLearningSystem {
  private config: AdaptiveLearningConfig;
  private agenticInterface: AgenticCorrectionInterface;
  private patternEngine: any; // PatternRecognitionEngine - will be properly typed later
  private learningHistory: LearningEvent[];
  private userFeedbackHistory: UserFeedback[];
  private adaptationThresholds: Map<string, number>;
  private approachPreferences: Map<ErrorType, Map<CorrectionStepType, number>>;
  private outcomePatterns: Map<string, OutcomePattern>;
  private learningAnalytics: LearningAnalytics;

  constructor(
    agenticInterface: AgenticCorrectionInterface,
    config: Partial<AdaptiveLearningConfig> = {},
  ) {
    this.agenticInterface = agenticInterface;
    this.patternEngine = null; // Initialize as null for now

    this.config = {
      enableRealTimeAdaptation: true,
      learningRate: 0.1,
      feedbackWeight: 0.4,
      outcomeWeight: 0.6,
      patternDecayRate: 0.95,
      adaptationThreshold: 0.7,
      maxLearningHistory: 1000,
      enableUserFeedbackLearning: true,
      enableOutcomeBasedLearning: true,
      enablePatternEvolution: true,
      ...config,
    };

    this.learningHistory = [];
    this.userFeedbackHistory = [];
    this.adaptationThresholds = new Map();
    this.approachPreferences = new Map();
    this.learningAnalytics = this.initializeLearningAnalytics();

    this.initializeAdaptationThresholds();
  }

  /**
   * Learn from a correction event and adapt the system
   */
  async learnFromCorrectionEvent(
    error: StructuredValidationError,
    context: ASTContext,
    correctionSteps: CorrectionStep[],
    outcome: CorrectionOutcome,
    userFeedback?: UserFeedback,
  ): Promise<LearningEvent> {

    const learningEvent = await this.createLearningEvent(
      error,
      context,
      correctionSteps,
      outcome,
      userFeedback,
    );

    // Store the learning event
    this.learningHistory.push(learningEvent);
    if (userFeedback) {
      this.userFeedbackHistory.push(userFeedback);
    }

    // Apply real-time adaptations if enabled
    if (this.config.enableRealTimeAdaptation) {
      await this.applyRealTimeAdaptations(learningEvent);
    }

    // Update learning analytics
    await this.updateLearningAnalytics(learningEvent);

    // Prune old learning history
    this.pruneLearningHistory();

    return learningEvent;
  }

  /**
   * Process user feedback and adapt system preferences
   */
  async processUserFeedback(feedback: UserFeedback): Promise<AdaptationAction[]> {
    if (!this.config.enableUserFeedbackLearning) {
      return [];
    }

    const adaptations: AdaptationAction[] = [];

    // Find the related learning event
    const relatedEvent = this.learningHistory.find(event =>
      event.id === feedback.correctionId,
    );

    if (!relatedEvent) {
      console.warn(`No learning event found for feedback ${feedback.correctionId}`);
      return adaptations;
    }

    // Adapt based on feedback type and rating
    switch (feedback.feedbackType) {
      case FeedbackType.CORRECTION_QUALITY:
        adaptations.push(...await this.adaptCorrectionQuality(feedback, relatedEvent));
        break;

      case FeedbackType.APPROACH_PREFERENCE:
        adaptations.push(...await this.adaptApproachPreference(feedback, relatedEvent));
        break;

      case FeedbackType.PERFORMANCE_FEEDBACK:
        adaptations.push(...await this.adaptPerformanceSettings(feedback, relatedEvent));
        break;

      case FeedbackType.SUGGESTION:
        adaptations.push(...await this.processSuggestion(feedback, relatedEvent));
        break;
    }

    // Apply the adaptations
    for (const adaptation of adaptations) {
      await this.applyAdaptation(adaptation);
    }

    return adaptations;
  }

  /**
   * Analyze learning trends and generate recommendations
   */
  async analyzeLearningTrends(): Promise<{
    trends: LearningAnalytics;
    recommendations: AdaptationRecommendation[];
    insights: string[];
  }> {

    const trends = await this.calculateLearningTrends();
    const recommendations = await this.generateAdaptationRecommendations();
    const insights = await this.generateLearningInsights();

    return { trends, recommendations, insights };
  }

  /**
   * Optimize system thresholds based on learning data
   */
  async optimizeThresholds(): Promise<Map<string, number>> {
    const optimizedThresholds = new Map<string, number>();

    // Analyze success rates for different threshold values
    const thresholdAnalysis = await this.analyzeThresholdPerformance();

    for (const [threshold, analysis] of thresholdAnalysis) {
      const currentValue = this.adaptationThresholds.get(threshold) || 0.5;
      const optimalValue = this.findOptimalThreshold(analysis);

      if (Math.abs(optimalValue - currentValue) > 0.05) {
        optimizedThresholds.set(threshold, optimalValue);
        this.adaptationThresholds.set(threshold, optimalValue);
      }
    }

    return optimizedThresholds;
  }

  /**
   * Get personalized recommendations for a specific error type
   */
  async getPersonalizedRecommendations(
    errorType: ErrorType,
    context: ASTContext,
    userId?: string,
  ): Promise<{
    recommendedApproach: CorrectionStepType;
    confidence: number;
    reasoning: string;
    alternatives: Array<{ approach: CorrectionStepType; confidence: number }>;
  }> {

    // Get approach preferences for this error type
    const preferences = this.approachPreferences.get(errorType) || new Map();

    // Consider user-specific feedback if available
    let userSpecificPreferences = new Map<CorrectionStepType, number>();
    if (userId) {
      userSpecificPreferences = await this.getUserSpecificPreferences(userId, errorType);
    }

    // Combine general and user-specific preferences
    const combinedPreferences = new Map<CorrectionStepType, number>();

    for (const approach of Object.values(CorrectionStepType)) {
      const generalPref = preferences.get(approach) || 0.5;
      const userPref = userSpecificPreferences.get(approach) || 0.5;
      const combined = generalPref * 0.6 + userPref * 0.4; // Weight general preferences more
      combinedPreferences.set(approach, combined);
    }

    // Sort by preference score
    const sortedApproaches = Array.from(combinedPreferences.entries())
      .sort(([, a], [, b]) => b - a);

    const recommendedApproach = sortedApproaches[0][0];
    const confidence = sortedApproaches[0][1];

    const alternatives = sortedApproaches.slice(1, 4).map(([approach, conf]) => ({
      approach,
      confidence: conf,
    }));

    const reasoning = await this.generateRecommendationReasoning(
      errorType,
      recommendedApproach,
      confidence,
      userId,
    );

    return {
      recommendedApproach,
      confidence,
      reasoning,
      alternatives,
    };
  }

  /**
   * Export learning data for analysis or backup
   */
  exportLearningData(): any {
    return {
      config: this.config,
      learningHistory: this.learningHistory.slice(-100), // Last 100 events
      userFeedbackHistory: this.userFeedbackHistory.slice(-50), // Last 50 feedback items
      adaptationThresholds: Array.from(this.adaptationThresholds.entries()),
      approachPreferences: Array.from(this.approachPreferences.entries()).map(([errorType, prefs]) => [
        errorType,
        Array.from(prefs.entries()),
      ]),
      learningAnalytics: this.learningAnalytics,
    };
  }

  /**
   * Import learning data from backup
   */
  importLearningData(data: any): void {
    if (data.config) {
      this.config = { ...this.config, ...data.config };
    }

    if (data.learningHistory) {
      this.learningHistory = data.learningHistory;
    }

    if (data.userFeedbackHistory) {
      this.userFeedbackHistory = data.userFeedbackHistory;
    }

    if (data.adaptationThresholds) {
      this.adaptationThresholds = new Map(data.adaptationThresholds);
    }

    if (data.approachPreferences) {
      this.approachPreferences = new Map(
        data.approachPreferences.map(([errorType, prefs]: [ErrorType, any[]]) => [
          errorType,
          new Map(prefs),
        ]),
      );
    }
  }

  // Private helper methods

  private async createLearningEvent(
    error: StructuredValidationError,
    context: ASTContext,
    correctionSteps: CorrectionStep[],
    outcome: CorrectionOutcome,
    userFeedback?: UserFeedback,
  ): Promise<LearningEvent> {

    const successfulStep = correctionSteps.find(step => step.output.correctedCode);
    const eventType = outcome.success ?
      LearningEventType.CORRECTION_SUCCESS :
      LearningEventType.CORRECTION_FAILURE;

    const adaptationActions = await this.identifyPotentialAdaptations(
      error,
      correctionSteps,
      outcome,
      userFeedback,
    );

    const learningMetrics = await this.calculateLearningEventMetrics(
      correctionSteps,
      outcome,
      userFeedback,
    );

    return {
      id: `learning-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      timestamp: new Date(),
      eventType,
      errorType: error.type,
      correctionApproach: successfulStep?.stepType || CorrectionStepType.LLM_GENERATION,
      determinismLevel: successfulStep?.determinismLevel || DeterminismLevel.NON_DETERMINISTIC,
      outcome,
      userFeedback,
      adaptationApplied: adaptationActions,
      learningMetrics,
    };
  }

  private async applyRealTimeAdaptations(learningEvent: LearningEvent): Promise<void> {
    for (const adaptation of learningEvent.adaptationApplied) {
      if (adaptation.confidence >= this.config.adaptationThreshold) {
        await this.applyAdaptation(adaptation);
      }
    }
  }

  private async applyAdaptation(adaptation: AdaptationAction): Promise<void> {
    switch (adaptation.actionType) {
      case AdaptationActionType.THRESHOLD_ADJUSTMENT:
        this.adaptationThresholds.set(adaptation.target, adaptation.newValue);
        break;

      case AdaptationActionType.APPROACH_PREFERENCE_UPDATE:
        await this.updateApproachPreference(adaptation);
        break;

      case AdaptationActionType.PATTERN_WEIGHT_UPDATE:
        // Would update pattern weights in pattern engine
        break;

      case AdaptationActionType.RULE_CONFIDENCE_UPDATE:
        // Would update rule confidence in grammar system
        break;
    }
  }

  private async adaptCorrectionQuality(
    feedback: UserFeedback,
    relatedEvent: LearningEvent,
  ): Promise<AdaptationAction[]> {

    const adaptations: AdaptationAction[] = [];

    // If user rated correction poorly, reduce confidence in that approach
    if (feedback.rating < 3) {
      const approach = relatedEvent.correctionApproach;
      const errorType = relatedEvent.errorType;

      adaptations.push({
        actionType: AdaptationActionType.APPROACH_PREFERENCE_UPDATE,
        target: `${errorType}-${approach}`,
        oldValue: this.getApproachPreference(errorType, approach),
        newValue: this.getApproachPreference(errorType, approach) * 0.9,
        confidence: 0.8,
        reasoning: `User rated correction quality as ${feedback.rating}/5`,
      });
    }

    return adaptations;
  }

  private async adaptApproachPreference(
    feedback: UserFeedback,
    relatedEvent: LearningEvent,
  ): Promise<AdaptationAction[]> {

    const adaptations: AdaptationAction[] = [];

    // Adjust approach preferences based on user feedback
    if (feedback.rating >= 4) {
      const approach = relatedEvent.correctionApproach;
      const errorType = relatedEvent.errorType;

      adaptations.push({
        actionType: AdaptationActionType.APPROACH_PREFERENCE_UPDATE,
        target: `${errorType}-${approach}`,
        oldValue: this.getApproachPreference(errorType, approach),
        newValue: Math.min(1.0, this.getApproachPreference(errorType, approach) * 1.1),
        confidence: 0.9,
        reasoning: `User preferred this approach (rating: ${feedback.rating}/5)`,
      });
    }

    return adaptations;
  }

  private async adaptPerformanceSettings(
    feedback: UserFeedback,
    relatedEvent: LearningEvent,
  ): Promise<AdaptationAction[]> {

    const adaptations: AdaptationAction[] = [];

    // Adjust performance-related thresholds based on feedback
    if (feedback.comments.toLowerCase().includes('slow')) {
      adaptations.push({
        actionType: AdaptationActionType.THRESHOLD_ADJUSTMENT,
        target: 'performance_timeout',
        oldValue: this.adaptationThresholds.get('performance_timeout') || 5000,
        newValue: (this.adaptationThresholds.get('performance_timeout') || 5000) * 0.8,
        confidence: 0.7,
        reasoning: 'User reported performance issues',
      });
    }

    return adaptations;
  }

  private async processSuggestion(
    feedback: UserFeedback,
    relatedEvent: LearningEvent,
  ): Promise<AdaptationAction[]> {

    const adaptations: AdaptationAction[] = [];

    // Process user suggestions for system improvement
    if (feedback.suggestedImprovement) {
      // This would analyze the suggestion and create appropriate adaptations
      // For now, create a placeholder adaptation
      adaptations.push({
        actionType: AdaptationActionType.APPROACH_PREFERENCE_UPDATE,
        target: 'user_suggestion',
        oldValue: null,
        newValue: feedback.suggestedImprovement,
        confidence: 0.5,
        reasoning: `User suggestion: ${feedback.suggestedImprovement}`,
      });
    }

    return adaptations;
  }

  private async identifyPotentialAdaptations(
    error: StructuredValidationError,
    correctionSteps: CorrectionStep[],
    outcome: CorrectionOutcome,
    userFeedback?: UserFeedback,
  ): Promise<AdaptationAction[]> {

    const adaptations: AdaptationAction[] = [];

    // Identify adaptations based on correction outcome
    if (!outcome.success) {
      // If correction failed, consider adjusting thresholds
      adaptations.push({
        actionType: AdaptationActionType.THRESHOLD_ADJUSTMENT,
        target: 'confidence_threshold',
        oldValue: this.adaptationThresholds.get('confidence_threshold') || 0.7,
        newValue: (this.adaptationThresholds.get('confidence_threshold') || 0.7) * 0.95,
        confidence: 0.6,
        reasoning: 'Correction failed - lowering confidence threshold',
      });
    }

    return adaptations;
  }

  private async calculateLearningEventMetrics(
    correctionSteps: CorrectionStep[],
    outcome: CorrectionOutcome,
    userFeedback?: UserFeedback,
  ): Promise<LearningEventMetrics> {

    return {
      accuracyImprovement: outcome.success ? 0.1 : -0.1,
      performanceImpact: 0, // Would calculate based on execution time
      userSatisfactionChange: userFeedback ? (userFeedback.rating - 3) * 0.1 : 0,
      deterministicRatioChange: 0, // Would calculate based on approach used
      adaptationEffectiveness: 0.5, // Placeholder
    };
  }

  private getApproachPreference(errorType: ErrorType, approach: CorrectionStepType): number {
    if (!this.approachPreferences.has(errorType)) {
      this.approachPreferences.set(errorType, new Map());
    }

    return this.approachPreferences.get(errorType)!.get(approach) || 0.5;
  }

  private async updateApproachPreference(adaptation: AdaptationAction): Promise<void> {
    const [errorType, approach] = adaptation.target.split('-') as [ErrorType, CorrectionStepType];

    if (!this.approachPreferences.has(errorType)) {
      this.approachPreferences.set(errorType, new Map());
    }

    this.approachPreferences.get(errorType)!.set(approach, adaptation.newValue);
  }

  private async calculateLearningTrends(): Promise<LearningAnalytics> {
    // Calculate various learning analytics
    const recentEvents = this.learningHistory.slice(-50);

    const accuracyTrend = this.calculateAccuracyTrend(recentEvents);
    const userSatisfactionTrend = this.calculateUserSatisfactionTrend();
    const deterministicProgressionTrend = this.calculateDeterministicTrend(recentEvents);

    return {
      totalLearningEvents: this.learningHistory.length,
      learningVelocity: this.calculateLearningVelocity(),
      adaptationSuccess: this.calculateAdaptationSuccess(),
      userSatisfactionTrend,
      accuracyTrend,
      deterministicProgressionTrend,
      topPerformingApproaches: this.calculateTopPerformingApproaches(),
      improvementOpportunities: await this.generateAdaptationRecommendations(),
    };
  }

  private calculateAccuracyTrend(events: LearningEvent[]): number[] {
    const windowSize = 10;
    const trend: number[] = [];

    for (let i = windowSize; i <= events.length; i++) {
      const window = events.slice(i - windowSize, i);
      const accuracy = window.filter(e => e.outcome.success).length / window.length;
      trend.push(accuracy);
    }

    return trend;
  }

  private calculateUserSatisfactionTrend(): number[] {
    const recentFeedback = this.userFeedbackHistory.slice(-20);
    const windowSize = 5;
    const trend: number[] = [];

    for (let i = windowSize; i <= recentFeedback.length; i++) {
      const window = recentFeedback.slice(i - windowSize, i);
      const avgSatisfaction = window.reduce((sum, f) => sum + f.rating, 0) / window.length;
      trend.push(avgSatisfaction);
    }

    return trend;
  }

  private calculateDeterministicTrend(events: LearningEvent[]): number[] {
    const windowSize = 10;
    const trend: number[] = [];

    for (let i = windowSize; i <= events.length; i++) {
      const window = events.slice(i - windowSize, i);
      const deterministicCount = window.filter(e =>
        e.determinismLevel === DeterminismLevel.FULLY_DETERMINISTIC ||
        e.determinismLevel === DeterminismLevel.MOSTLY_DETERMINISTIC,
      ).length;
      trend.push(deterministicCount / window.length);
    }

    return trend;
  }

  private calculateLearningVelocity(): number {
    // Calculate how quickly the system is learning (events per day)
    const recentEvents = this.learningHistory.slice(-30);
    if (recentEvents.length < 2) {
return 0;
}

    const timeSpan = recentEvents[recentEvents.length - 1].timestamp.getTime() -
                   recentEvents[0].timestamp.getTime();
    const days = timeSpan / (1000 * 60 * 60 * 24);

    return recentEvents.length / Math.max(days, 1);
  }

  private calculateAdaptationSuccess(): number {
    const recentEvents = this.learningHistory.slice(-20);
    const adaptationsApplied = recentEvents.filter(e => e.adaptationApplied.length > 0);

    if (adaptationsApplied.length === 0) {
return 0;
}

    const successfulAdaptations = adaptationsApplied.filter(e => e.outcome.success);
    return successfulAdaptations.length / adaptationsApplied.length;
  }

  private calculateTopPerformingApproaches(): Map<ErrorType, CorrectionStepType> {
    const performanceMap = new Map<ErrorType, Map<CorrectionStepType, number>>();

    for (const event of this.learningHistory) {
      if (!performanceMap.has(event.errorType)) {
        performanceMap.set(event.errorType, new Map());
      }

      const approachMap = performanceMap.get(event.errorType)!;
      const currentScore = approachMap.get(event.correctionApproach) || 0;
      const newScore = event.outcome.success ? currentScore + 1 : currentScore;
      approachMap.set(event.correctionApproach, newScore);
    }

    const topPerforming = new Map<ErrorType, CorrectionStepType>();

    for (const [errorType, approaches] of performanceMap) {
      let bestApproach = CorrectionStepType.LLM_GENERATION;
      let bestScore = 0;

      for (const [approach, score] of approaches) {
        if (score > bestScore) {
          bestScore = score;
          bestApproach = approach;
        }
      }

      topPerforming.set(errorType, bestApproach);
    }

    return topPerforming;
  }

  private async generateAdaptationRecommendations(): Promise<AdaptationRecommendation[]> {
    const recommendations: AdaptationRecommendation[] = [];

    // Analyze threshold performance
    const thresholdAnalysis = await this.analyzeThresholdPerformance();

    for (const [threshold, analysis] of thresholdAnalysis) {
      if (analysis.improvementPotential > 0.1) {
        recommendations.push({
          priority: Math.floor(analysis.improvementPotential * 10),
          actionType: AdaptationActionType.THRESHOLD_ADJUSTMENT,
          target: threshold,
          recommendedValue: analysis.optimalValue,
          expectedImpact: analysis.improvementPotential,
          reasoning: `Threshold optimization could improve performance by ${(analysis.improvementPotential * 100).toFixed(1)}%`,
          confidence: analysis.confidence,
        });
      }
    }

    return recommendations.sort((a, b) => b.priority - a.priority);
  }

  private async analyzeThresholdPerformance(): Promise<Map<string, any>> {
    // Analyze performance of different thresholds
    const analysis = new Map<string, any>();

    for (const [threshold, value] of this.adaptationThresholds) {
      analysis.set(threshold, {
        currentValue: value,
        optimalValue: value, // Would calculate optimal value
        improvementPotential: 0.05, // Placeholder
        confidence: 0.7,
      });
    }

    return analysis;
  }

  private findOptimalThreshold(analysis: any): number {
    // Find optimal threshold value based on analysis
    return analysis.optimalValue || 0.7;
  }

  private async getUserSpecificPreferences(
    userId: string,
    errorType: ErrorType,
  ): Promise<Map<CorrectionStepType, number>> {

    const userFeedback = this.userFeedbackHistory.filter(f => f.userId === userId);
    const preferences = new Map<CorrectionStepType, number>();

    // Analyze user's feedback patterns
    for (const feedback of userFeedback) {
      const relatedEvent = this.learningHistory.find(e => e.id === feedback.correctionId);
      if (relatedEvent && relatedEvent.errorType === errorType) {
        const approach = relatedEvent.correctionApproach;
        const currentPref = preferences.get(approach) || 0.5;
        const adjustment = (feedback.rating - 3) * 0.1; // -0.2 to +0.2
        preferences.set(approach, Math.max(0, Math.min(1, currentPref + adjustment)));
      }
    }

    return preferences;
  }

  private async generateRecommendationReasoning(
    errorType: ErrorType,
    approach: CorrectionStepType,
    confidence: number,
    userId?: string,
  ): Promise<string> {

    const reasons: string[] = [];

    reasons.push(`${approach} has ${(confidence * 100).toFixed(1)}% success rate for ${errorType} errors`);

    if (userId) {
      reasons.push('Based on your previous feedback preferences');
    }

    const recentEvents = this.learningHistory
      .filter(e => e.errorType === errorType && e.correctionApproach === approach)
      .slice(-5);

    if (recentEvents.length > 0) {
      const recentSuccess = recentEvents.filter(e => e.outcome.success).length;
      reasons.push(`Recent success rate: ${(recentSuccess / recentEvents.length * 100).toFixed(1)}%`);
    }

    return reasons.join('. ');
  }

  private async updateLearningAnalytics(learningEvent: LearningEvent): Promise<void> {
    this.learningAnalytics.totalLearningEvents++;

    // Update other analytics based on the learning event
    // This would include more sophisticated analytics calculations
  }

  private pruneLearningHistory(): void {
    if (this.learningHistory.length > this.config.maxLearningHistory) {
      this.learningHistory = this.learningHistory.slice(-this.config.maxLearningHistory);
    }

    if (this.userFeedbackHistory.length > this.config.maxLearningHistory / 2) {
      this.userFeedbackHistory = this.userFeedbackHistory.slice(-this.config.maxLearningHistory / 2);
    }
  }

  private initializeAdaptationThresholds(): void {
    this.adaptationThresholds.set('confidence_threshold', 0.7);
    this.adaptationThresholds.set('pattern_match_threshold', 0.7);
    this.adaptationThresholds.set('llm_fallback_threshold', 0.5);
    this.adaptationThresholds.set('performance_timeout', 5000);
  }

  private initializeLearningAnalytics(): LearningAnalytics {
    return {
      totalLearningEvents: 0,
      learningVelocity: 0,
      adaptationSuccess: 0,
      userSatisfactionTrend: [],
      accuracyTrend: [],
      deterministicProgressionTrend: [],
      topPerformingApproaches: new Map(),
      improvementOpportunities: [],
    };
  }

  /**
   * Get current learning statistics
   */
  getLearningStatistics(): LearningAnalytics {
    return { ...this.learningAnalytics };
  }

  /**
   * Generate learning insights (placeholder implementation)
   */
  generateLearningInsights(): any {
    return {
      totalLearningEvents: this.learningHistory.length,
      averageConfidence: 0.8,
      topPatterns: [],
      recommendations: [],
    };
  }
}
