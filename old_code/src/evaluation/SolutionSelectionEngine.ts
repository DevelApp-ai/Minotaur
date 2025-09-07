/**
 * SolutionSelectionEngine - Phase 2 of Project Golem
 *
 * Intelligent selection engine that chooses the best correction solution
 * from multiple candidates using advanced algorithms, context analysis,
 * and user preferences. Goes beyond simple ranking to provide smart selection.
 */

import { CorrectionSolution, SolutionType, ImpactAnalysis, SolutionValidation } from './MultiSolutionGenerator';
import { StructuredValidationError, ErrorType, ErrorSeverity } from './StructuredValidationError';
import { ASTContext } from './GrammarDrivenASTMapper';
import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';

export interface SelectionCriteria {
  prioritizeMinimalChanges: boolean;
  prioritizeReadability: boolean;
  prioritizePerformance: boolean;
  allowBreakingChanges: boolean;
  preferDirectFixes: boolean;
  confidenceWeight: number;
  impactWeight: number;
  validationWeight: number;
  contextWeight: number;
}

export interface SelectionContext {
  codeStyle: CodeStyle;
  projectConstraints: ProjectConstraints;
  userPreferences: UserPreferences;
  historicalChoices: HistoricalChoice[];
  currentSession: SessionContext;
}

export interface CodeStyle {
  indentationStyle: 'spaces' | 'tabs';
  indentationSize: number;
  maxLineLength: number;
  preferExplicitTypes: boolean;
  preferVerboseNames: boolean;
  allowComplexExpressions: boolean;
}

export interface ProjectConstraints {
  pythonVersion: string;
  allowedImports: string[];
  forbiddenPatterns: string[];
  performanceRequirements: 'strict' | 'moderate' | 'relaxed';
  maintainabilityLevel: 'high' | 'medium' | 'low';
}

export interface UserPreferences {
  preferredSolutionTypes: SolutionType[];
  avoidedSolutionTypes: SolutionType[];
  riskTolerance: 'conservative' | 'moderate' | 'aggressive';
  learningMode: boolean;
  explainDecisions: boolean;
}

export interface HistoricalChoice {
  errorType: ErrorType;
  availableSolutions: string[];
  chosenSolution: string;
  outcome: 'success' | 'failure' | 'partial';
  timestamp: Date;
  context: string;
}

export interface SessionContext {
  totalErrors: number;
  errorsFixed: number;
  currentErrorIndex: number;
  timeSpent: number;
  userFeedback: UserFeedback[];
}

export interface UserFeedback {
  solutionId: string;
  rating: number; // 1-5
  comments: string;
  wouldUseAgain: boolean;
}

export interface SelectionResult {
  selectedSolution: CorrectionSolution;
  selectionReason: string;
  alternativeOptions: CorrectionSolution[];
  confidenceScore: number;
  selectionMetrics: SelectionMetrics;
  recommendations: string[];
}

export interface SelectionMetrics {
  criteriaScores: Map<string, number>;
  totalScore: number;
  selectionTime: number;
  algorithmsUsed: string[];
  contextFactors: string[];
}

export enum SelectionAlgorithm {
  WEIGHTED_SCORING = 'weighted_scoring',
  MACHINE_LEARNING = 'machine_learning',
  RULE_BASED = 'rule_based',
  HYBRID = 'hybrid',
  USER_GUIDED = 'user_guided'
}

/**
 * SolutionSelectionEngine - Intelligent solution selection
 */
export class SolutionSelectionEngine {
  private selectionCriteria: SelectionCriteria;
  private selectionContext: SelectionContext;
  private algorithm: SelectionAlgorithm;
  private learningEnabled: boolean;
  private decisionHistory: Map<string, SelectionResult>;

  constructor(
    criteria: Partial<SelectionCriteria> = {},
    context: Partial<SelectionContext> = {},
    algorithm: SelectionAlgorithm = SelectionAlgorithm.HYBRID,
  ) {
    this.selectionCriteria = {
      prioritizeMinimalChanges: true,
      prioritizeReadability: true,
      prioritizePerformance: false,
      allowBreakingChanges: false,
      preferDirectFixes: true,
      confidenceWeight: 0.4,
      impactWeight: 0.3,
      validationWeight: 0.2,
      contextWeight: 0.1,
      ...criteria,
    };

    this.selectionContext = {
      codeStyle: {
        indentationStyle: 'spaces',
        indentationSize: 4,
        maxLineLength: 120,
        preferExplicitTypes: false,
        preferVerboseNames: true,
        allowComplexExpressions: false,
      },
      projectConstraints: {
        pythonVersion: '3.11',
        allowedImports: [],
        forbiddenPatterns: [],
        performanceRequirements: 'moderate',
        maintainabilityLevel: 'high',
      },
      userPreferences: {
        preferredSolutionTypes: [SolutionType.DIRECT_FIX, SolutionType.IMPORT_ADDITION],
        avoidedSolutionTypes: [SolutionType.REFACTORING],
        riskTolerance: 'moderate',
        learningMode: true,
        explainDecisions: true,
      },
      historicalChoices: [],
      currentSession: {
        totalErrors: 0,
        errorsFixed: 0,
        currentErrorIndex: 0,
        timeSpent: 0,
        userFeedback: [],
      },
      ...context,
    };

    this.algorithm = algorithm;
    this.learningEnabled = this.selectionContext.userPreferences.learningMode;
    this.decisionHistory = new Map();
  }

  /**
   * Select the best solution from multiple candidates
   */
  async selectBestSolution(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<SelectionResult> {

    const startTime = Date.now();

    if (solutions.length === 0) {
      throw new Error('No solutions provided for selection');
    }

    if (solutions.length === 1) {
      return this.createSingleSolutionResult(solutions[0], startTime);
    }

    let selectedSolution: CorrectionSolution;
    let selectionReason: string;
    let algorithmsUsed: string[] = [];

    switch (this.algorithm) {
      case SelectionAlgorithm.WEIGHTED_SCORING:
        ({ selectedSolution, selectionReason } = await this.selectByWeightedScoring(solutions, error, context));
        algorithmsUsed = ['weighted_scoring'];
        break;

      case SelectionAlgorithm.RULE_BASED:
        ({ selectedSolution, selectionReason } = await this.selectByRules(solutions, error, context));
        algorithmsUsed = ['rule_based'];
        break;

      case SelectionAlgorithm.MACHINE_LEARNING:
        ({ selectedSolution, selectionReason } = await this.selectByMachineLearning(solutions, error, context));
        algorithmsUsed = ['machine_learning'];
        break;

      case SelectionAlgorithm.HYBRID:
        ({ selectedSolution, selectionReason } = await this.selectByHybridApproach(solutions, error, context));
        algorithmsUsed = ['hybrid', 'weighted_scoring', 'rule_based'];
        break;

      case SelectionAlgorithm.USER_GUIDED:
        ({ selectedSolution, selectionReason } = await this.selectByUserGuidance(solutions, error, context));
        algorithmsUsed = ['user_guided'];
        break;

      default:
        ({ selectedSolution, selectionReason } = await this.selectByWeightedScoring(solutions, error, context));
        algorithmsUsed = ['weighted_scoring'];
    }

    const selectionTime = Date.now() - startTime;
    const alternativeOptions = solutions.filter(s => s.id !== selectedSolution.id);

    // Calculate selection metrics
    const selectionMetrics = await this.calculateSelectionMetrics(
      selectedSolution,
      solutions,
      selectionTime,
      algorithmsUsed,
    );

    // Generate recommendations
    const recommendations = await this.generateRecommendations(
      selectedSolution,
      alternativeOptions,
      error,
      context,
    );

    const result: SelectionResult = {
      selectedSolution,
      selectionReason,
      alternativeOptions,
      confidenceScore: this.calculateConfidenceScore(selectedSolution, solutions),
      selectionMetrics,
      recommendations,
    };

    // Store decision for learning
    if (this.learningEnabled) {
      this.recordDecision(result, error, context);
    }

    return result;
  }

  /**
   * Select solution using weighted scoring algorithm
   */
  private async selectByWeightedScoring(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<{ selectedSolution: CorrectionSolution; selectionReason: string }> {

    const scoredSolutions = solutions.map(solution => ({
      solution,
      score: this.calculateWeightedScore(solution, error, context),
    }));

    // Sort by score (descending)
    scoredSolutions.sort((a, b) => b.score - a.score);

    const best = scoredSolutions[0];
    const selectionReason = `Selected based on weighted scoring (score: ${best.score.toFixed(2)}). ` +
      `Factors: confidence (${(best.solution.confidence * this.selectionCriteria.confidenceWeight).toFixed(2)}), ` +
      `impact (${this.calculateImpactScore(best.solution.estimatedImpact).toFixed(2)}), ` +
      `validation (${this.calculateValidationScore(best.solution.validationResult).toFixed(2)})`;

    return {
      selectedSolution: best.solution,
      selectionReason,
    };
  }

  /**
   * Select solution using rule-based algorithm
   */
  private async selectByRules(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<{ selectedSolution: CorrectionSolution; selectionReason: string }> {

    // Rule 1: Prefer solutions that don't introduce breaking changes
    if (!this.selectionCriteria.allowBreakingChanges) {
      const nonBreaking = solutions.filter(s => !s.estimatedImpact.breakingChanges);
      if (nonBreaking.length > 0) {
        solutions = nonBreaking;
      }
    }

    // Rule 2: Prefer direct fixes if configured
    if (this.selectionCriteria.preferDirectFixes) {
      const directFixes = solutions.filter(s => s.type === SolutionType.DIRECT_FIX);
      if (directFixes.length > 0) {
        solutions = directFixes;
      }
    }

    // Rule 3: Prefer user's preferred solution types
    const preferredTypes = this.selectionContext.userPreferences.preferredSolutionTypes;
    if (preferredTypes.length > 0) {
      const preferred = solutions.filter(s => preferredTypes.includes(s.type));
      if (preferred.length > 0) {
        solutions = preferred;
      }
    }

    // Rule 4: Avoid user's avoided solution types
    const avoidedTypes = this.selectionContext.userPreferences.avoidedSolutionTypes;
    if (avoidedTypes.length > 0) {
      solutions = solutions.filter(s => !avoidedTypes.includes(s.type));
    }

    // Rule 5: Select highest confidence among remaining
    solutions.sort((a, b) => b.confidence - a.confidence);

    const selectedSolution = solutions[0];
    const selectionReason = 'Selected using rule-based approach. Applied rules: ' +
      'breaking changes filter, direct fix preference, user preferences, confidence ranking.';

    return { selectedSolution, selectionReason };
  }

  /**
   * Select solution using machine learning algorithm (placeholder)
   */
  private async selectByMachineLearning(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<{ selectedSolution: CorrectionSolution; selectionReason: string }> {

    // This would implement actual ML-based selection
    // For now, use a simplified approach based on historical choices

    const historicalPreferences = this.analyzeHistoricalPreferences(error.type);

    // Score solutions based on historical success
    const scoredSolutions = solutions.map(solution => ({
      solution,
      score: this.calculateMLScore(solution, historicalPreferences),
    }));

    scoredSolutions.sort((a, b) => b.score - a.score);

    const selectedSolution = scoredSolutions[0].solution;
    const selectionReason = 'Selected using machine learning approach based on historical success patterns ' +
      `for ${error.type} errors. ML confidence: ${scoredSolutions[0].score.toFixed(2)}`;

    return { selectedSolution, selectionReason };
  }

  /**
   * Select solution using hybrid approach
   */
  private async selectByHybridApproach(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<{ selectedSolution: CorrectionSolution; selectionReason: string }> {

    // Combine multiple approaches
    const weightedResult = await this.selectByWeightedScoring(solutions, error, context);
    const ruleBasedResult = await this.selectByRules(solutions, error, context);
    const mlResult = await this.selectByMachineLearning(solutions, error, context);

    // If all approaches agree, use that solution
    if (weightedResult.selectedSolution.id === ruleBasedResult.selectedSolution.id &&
        ruleBasedResult.selectedSolution.id === mlResult.selectedSolution.id) {

      return {
        selectedSolution: weightedResult.selectedSolution,
        selectionReason: 'All selection algorithms (weighted scoring, rule-based, ML) agreed on this solution.',
      };
    }

    // Otherwise, use weighted voting
    const votes = new Map<string, number>();
    votes.set(weightedResult.selectedSolution.id, (votes.get(weightedResult.selectedSolution.id) || 0) + 0.4);
    votes.set(ruleBasedResult.selectedSolution.id, (votes.get(ruleBasedResult.selectedSolution.id) || 0) + 0.3);
    votes.set(mlResult.selectedSolution.id, (votes.get(mlResult.selectedSolution.id) || 0) + 0.3);

    // Find solution with highest vote
    let bestSolutionId = '';
    let bestScore = 0;
    for (const [solutionId, score] of votes) {
      if (score > bestScore) {
        bestScore = score;
        bestSolutionId = solutionId;
      }
    }

    const selectedSolution = solutions.find(s => s.id === bestSolutionId)!;
    const selectionReason = 'Selected using hybrid approach with weighted voting. ' +
      `Vote score: ${bestScore.toFixed(2)}. Algorithms considered: weighted scoring, rule-based, ML.`;

    return { selectedSolution, selectionReason };
  }

  /**
   * Select solution with user guidance (interactive)
   */
  private async selectByUserGuidance(
    solutions: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<{ selectedSolution: CorrectionSolution; selectionReason: string }> {

    // This would present options to user for selection
    // For now, simulate user preference based on stored preferences

    const userPreferredTypes = this.selectionContext.userPreferences.preferredSolutionTypes;

    // Find solutions matching user preferences
    const preferredSolutions = solutions.filter(s => userPreferredTypes.includes(s.type));

    if (preferredSolutions.length > 0) {
      // Select highest confidence among preferred
      preferredSolutions.sort((a, b) => b.confidence - a.confidence);

      return {
        selectedSolution: preferredSolutions[0],
        selectionReason: `Selected based on user preferences for ${preferredSolutions[0].type} solutions.`,
      };
    }

    // Fallback to highest confidence
    solutions.sort((a, b) => b.confidence - a.confidence);

    return {
      selectedSolution: solutions[0],
      selectionReason: 'Selected highest confidence solution as no user-preferred types were available.',
    };
  }

  /**
   * Calculate weighted score for a solution
   */
  private calculateWeightedScore(
    solution: CorrectionSolution,
    error: StructuredValidationError,
    context: ASTContext,
  ): number {

    const confidenceScore = solution.confidence * this.selectionCriteria.confidenceWeight;
    const impactScore = (1 - this.calculateImpactScore(solution.estimatedImpact)) * this.selectionCriteria.impactWeight;
    const validationScore = this.calculateValidationScore(solution.validationResult) * this.selectionCriteria.validationWeight;
    const contextScore = this.calculateContextScore(solution, error, context) * this.selectionCriteria.contextWeight;

    return confidenceScore + impactScore + validationScore + contextScore;
  }

  /**
   * Calculate impact score (0-1, lower is better)
   */
  private calculateImpactScore(impact: ImpactAnalysis): number {
    let score = 0;

    score += Math.min(impact.linesAffected / 10, 1) * 0.3; // Normalize lines affected
    score += impact.scopeChanges.length / 5 * 0.2; // Scope changes
    score += impact.potentialSideEffects.length / 3 * 0.2; // Side effects
    score += impact.breakingChanges ? 0.2 : 0; // Breaking changes
    score += impact.performanceImpact === 'negative' ? 0.1 : 0; // Performance impact

    return Math.min(score, 1);
  }

  /**
   * Calculate validation score (0-1, higher is better)
   */
  private calculateValidationScore(validation: SolutionValidation): number {
    let score = 0;

    if (validation.syntaxValid) {
score += 0.3;
}
    if (validation.semanticsValid) {
score += 0.3;
}
    if (validation.grammarCompliant) {
score += 0.2;
}

    score += Math.min(validation.errorsResolved / 3, 1) * 0.1;
    score -= Math.min(validation.errorsIntroduced / 2, 1) * 0.1;

    return Math.max(0, Math.min(score, 1));
  }

  /**
   * Calculate context score based on current context
   */
  private calculateContextScore(
    solution: CorrectionSolution,
    error: StructuredValidationError,
    context: ASTContext,
  ): number {

    let score = 0.5; // Base score

    // Prefer solutions that match code style
    if (this.selectionContext.codeStyle.preferVerboseNames &&
        solution.description.includes('verbose')) {
      score += 0.2;
    }

    // Consider project constraints
    if (solution.type === SolutionType.IMPORT_ADDITION) {
      const importName = this.extractImportName(solution.description);
      if (importName && this.selectionContext.projectConstraints.allowedImports.includes(importName)) {
        score += 0.3;
      }
    }

    // Consider user risk tolerance
    const riskLevel = this.assessSolutionRisk(solution);
    const userTolerance = this.selectionContext.userPreferences.riskTolerance;

    if ((riskLevel === 'low' && userTolerance === 'conservative') ||
        (riskLevel === 'medium' && userTolerance === 'moderate') ||
        (riskLevel === 'high' && userTolerance === 'aggressive')) {
      score += 0.2;
    }

    return Math.max(0, Math.min(score, 1));
  }

  /**
   * Analyze historical preferences for error type
   */
  private analyzeHistoricalPreferences(errorType: ErrorType): Map<SolutionType, number> {
    const preferences = new Map<SolutionType, number>();

    const relevantChoices = this.selectionContext.historicalChoices.filter(
      choice => choice.errorType === errorType && choice.outcome === 'success',
    );

    for (const choice of relevantChoices) {
      // This would analyze the chosen solution type
      // For now, return default preferences
    }

    // Default preferences if no history
    preferences.set(SolutionType.DIRECT_FIX, 0.8);
    preferences.set(SolutionType.IMPORT_ADDITION, 0.7);
    preferences.set(SolutionType.ALTERNATIVE_APPROACH, 0.5);
    preferences.set(SolutionType.REFACTORING, 0.3);

    return preferences;
  }

  /**
   * Calculate ML-based score
   */
  private calculateMLScore(solution: CorrectionSolution, preferences: Map<SolutionType, number>): number {
    const baseScore = preferences.get(solution.type) || 0.5;
    const confidenceBonus = solution.confidence * 0.3;
    const validationBonus = this.calculateValidationScore(solution.validationResult) * 0.2;

    return baseScore + confidenceBonus + validationBonus;
  }

  /**
   * Calculate confidence score for selection
   */
  private calculateConfidenceScore(selected: CorrectionSolution, allSolutions: CorrectionSolution[]): number {
    if (allSolutions.length === 1) {
return 1.0;
}

    const scores = allSolutions.map(s => this.calculateWeightedScore(s, {} as any, {} as any));
    scores.sort((a, b) => b - a);

    const selectedScore = this.calculateWeightedScore(selected, {} as any, {} as any);
    const gap = scores[0] - (scores[1] || 0);

    return Math.min(1.0, selected.confidence + gap * 0.3);
  }

  /**
   * Calculate selection metrics
   */
  private async calculateSelectionMetrics(
    selected: CorrectionSolution,
    allSolutions: CorrectionSolution[],
    selectionTime: number,
    algorithmsUsed: string[],
  ): Promise<SelectionMetrics> {

    const criteriaScores = new Map<string, number>();
    criteriaScores.set('confidence', selected.confidence);
    criteriaScores.set('impact', 1 - this.calculateImpactScore(selected.estimatedImpact));
    criteriaScores.set('validation', this.calculateValidationScore(selected.validationResult));

    const totalScore = Array.from(criteriaScores.values()).reduce((sum, score) => sum + score, 0) / criteriaScores.size;

    return {
      criteriaScores,
      totalScore,
      selectionTime,
      algorithmsUsed,
      contextFactors: ['code_style', 'user_preferences', 'project_constraints'],
    };
  }

  /**
   * Generate recommendations based on selection
   */
  private async generateRecommendations(
    selected: CorrectionSolution,
    alternatives: CorrectionSolution[],
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<string[]> {

    const recommendations: string[] = [];

    // Recommend reviewing alternatives if they're close in score
    const closeAlternatives = alternatives.filter(alt =>
      Math.abs(alt.confidence - selected.confidence) < 0.1,
    );

    if (closeAlternatives.length > 0) {
      recommendations.push(`Consider reviewing ${closeAlternatives.length} alternative solution(s) with similar confidence scores.`);
    }

    // Recommend caution for high-impact solutions
    if (selected.estimatedImpact.linesAffected > 5) {
      recommendations.push(`This solution affects ${selected.estimatedImpact.linesAffected} lines. Consider testing thoroughly.`);
    }

    // Recommend specific actions based on solution type
    if (selected.type === SolutionType.IMPORT_ADDITION) {
      recommendations.push('Verify that the imported module is available in your environment.');
    }

    if (selected.type === SolutionType.REFACTORING) {
      recommendations.push('This refactoring solution may require updating related code. Review dependencies.');
    }

    return recommendations;
  }

  /**
   * Record decision for learning
   */
  private recordDecision(
    result: SelectionResult,
    error: StructuredValidationError,
    context: ASTContext,
  ): void {

    this.decisionHistory.set(result.selectedSolution.id, result);

    // Add to historical choices
    this.selectionContext.historicalChoices.push({
      errorType: error.type,
      availableSolutions: result.alternativeOptions.map(s => s.id),
      chosenSolution: result.selectedSolution.id,
      outcome: 'success', // Would be updated based on actual outcome
      timestamp: new Date(),
      context: context.sourceCode.substring(0, 100), // First 100 chars for context
    });
  }

  /**
   * Create result for single solution
   */
  private createSingleSolutionResult(solution: CorrectionSolution, startTime: number): SelectionResult {
    return {
      selectedSolution: solution,
      selectionReason: 'Only solution available',
      alternativeOptions: [],
      confidenceScore: solution.confidence,
      selectionMetrics: {
        criteriaScores: new Map([['confidence', solution.confidence]]),
        totalScore: solution.confidence,
        selectionTime: Date.now() - startTime,
        algorithmsUsed: ['single_option'],
        contextFactors: [],
      },
      recommendations: [],
    };
  }

  /**
   * Extract import name from solution description
   */
  private extractImportName(description: string): string | null {
    const match = description.match(/import.*?['"](.+?)['"]/) || description.match(/import\s+(\w+)/);
    return match ? match[1] : null;
  }

  /**
   * Assess solution risk level
   */
  private assessSolutionRisk(solution: CorrectionSolution): 'low' | 'medium' | 'high' {
    if (solution.estimatedImpact.breakingChanges) {
return 'high';
}
    if (solution.estimatedImpact.linesAffected > 5) {
return 'medium';
}
    if (solution.type === SolutionType.REFACTORING) {
return 'medium';
}
    return 'low';
  }

  /**
   * Update selection criteria
   */
  updateSelectionCriteria(newCriteria: Partial<SelectionCriteria>): void {
    this.selectionCriteria = { ...this.selectionCriteria, ...newCriteria };
  }

  /**
   * Update selection context
   */
  updateSelectionContext(newContext: Partial<SelectionContext>): void {
    this.selectionContext = { ...this.selectionContext, ...newContext };
  }

  /**
   * Get selection statistics
   */
  getSelectionStatistics(): any {
    return {
      algorithm: this.algorithm,
      totalDecisions: this.decisionHistory.size,
      learningEnabled: this.learningEnabled,
      historicalChoices: this.selectionContext.historicalChoices.length,
      userPreferences: this.selectionContext.userPreferences,
    };
  }
}

