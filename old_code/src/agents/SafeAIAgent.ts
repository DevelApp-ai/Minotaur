/**
 * SafeAIAgent - AI agent with built-in syntactic correctness enforcement
 *
 * This agent wraps AI-driven AST manipulations with validation guards to ensure
 * that all AI-generated changes maintain syntactic correctness and structural integrity.
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { ASTGuard, GuardedASTNode, ASTGuardFactory } from '../validation/ASTGuard';
import { SyntacticValidator, ASTManipulation, ManipulationType, ValidationResult } from '../validation/SyntacticValidator';
import { Grammar } from '../core/grammar/Grammar';
import { ContextAwareParser, ScopeInfo, ScopeType } from '../context/ContextAwareParser';
import { EventEmitter } from 'events';

export interface AIAgentConfiguration {
  maxRetries: number;
  autoCorrection: boolean;
  strictValidation: boolean;
  learningEnabled: boolean;
  confidenceThreshold: number;
  safetyMode: 'strict' | 'balanced' | 'permissive';
}

export interface AIManipulationRequest {
  description: string;
  targetNode: ZeroCopyASTNode;
  context?: ScopeInfo;
  constraints?: string[];
  expectedType?: string;
  metadata?: any;
}

export interface AIManipulationResult {
  success: boolean;
  manipulations: ASTManipulation[];
  validationResults: ValidationResult[];
  confidence: number;
  reasoning: string;
  alternatives?: ASTManipulation[];
  errors?: string[];
  warnings?: string[];
}

export interface LearningData {
  _request: AIManipulationRequest;
  result: AIManipulationResult;
  userFeedback?: 'accept' | 'reject' | 'modify';
  timestamp: number;
}

export class SafeAIAgent extends EventEmitter {
  private guard: ASTGuard;
  private validator: SyntacticValidator;
  private contextParser: ContextAwareParser;
  private config: AIAgentConfiguration;
  private learningHistory: LearningData[];
  private grammar: Grammar;

  constructor(grammar: Grammar, config: Partial<AIAgentConfiguration> = {}) {
    super();

    this.grammar = grammar;
    this.config = {
      maxRetries: 3,
      autoCorrection: true,
      strictValidation: false,
      learningEnabled: true,
      confidenceThreshold: 0.7,
      safetyMode: 'balanced',
      ...config,
    };

    // Initialize components based on safety mode
    this.guard = this.createGuardForSafetyMode(grammar, this.config.safetyMode);
    this.validator = new SyntacticValidator(grammar);
    this.contextParser = new ContextAwareParser();
    this.learningHistory = [];

    this.setupEventHandlers();
  }

  /**
   * Safely performs AI-driven AST manipulation with validation
   */
  public async performSafeManipulation(request: AIManipulationRequest): Promise<AIManipulationResult> {
    this.emit('manipulation_requested', { request, timestamp: Date.now() });

    try {
      // 1. Analyze context and constraints
      const analysisResult = await this.analyzeManipulationContext(request);

      // 2. Generate manipulation candidates
      const candidates = await this.generateManipulationCandidates(request, analysisResult);

      // 3. Validate and filter candidates
      const validatedCandidates = this.validateCandidates(candidates);

      // 4. Select best candidate
      const selectedManipulation = this.selectBestCandidate(validatedCandidates, request);

      // 5. Apply manipulation with guard protection
      const result = await this.applyGuardedManipulation(selectedManipulation, request);

      // 6. Learn from the result
      if (this.config.learningEnabled) {
        this.recordLearningData(request, result);
      }

      this.emit('manipulation_completed', { request, result, timestamp: Date.now() });
      return result;

    } catch (error) {
      const errorResult: AIManipulationResult = {
        success: false,
        manipulations: [],
        validationResults: [],
        confidence: 0,
        reasoning: `Failed to perform manipulation: ${error instanceof Error ? error.message : String(error)}`,
        errors: [error instanceof Error ? error.message : String(error)],
      };

      this.emit('manipulation_failed', { request, error: errorResult, timestamp: Date.now() });
      return errorResult;
    }
  }

  /**
   * Batch processing of multiple manipulation requests
   */
  public async performBatchManipulations(requests: AIManipulationRequest[]): Promise<AIManipulationResult[]> {
    const results: AIManipulationResult[] = [];

    for (const request of requests) {
      const result = await this.performSafeManipulation(request);
      results.push(result);

      // Stop on first failure in strict mode
      if (!result.success && this.config.safetyMode === 'strict') {
        break;
      }
    }

    return results;
  }

  /**
   * Suggests safe manipulations for a given context
   */
  public async suggestSafeManipulations(
    node: ZeroCopyASTNode,
    context?: ScopeInfo,
  ): Promise<AIManipulationSuggestion[]> {
    const suggestions: AIManipulationSuggestion[] = [];

    // Analyze the node and context
    const nodeType = node.nodeType;
    const availableManipulations = this.getAvailableManipulations(nodeType);

    for (const manipulationType of availableManipulations) {
      const suggestion = await this.generateManipulationSuggestion(node, manipulationType, context);
      if (suggestion && suggestion.confidence >= this.config.confidenceThreshold) {
        suggestions.push(suggestion);
      }
    }

    // Sort by confidence and safety score
    suggestions.sort((a, b) => (b.confidence * b.safetyScore) - (a.confidence * a.safetyScore));

    return suggestions.slice(0, 10); // Return top 10 suggestions
  }

  /**
   * Validates a proposed manipulation without applying it
   */
  public validateProposedManipulation(manipulation: ASTManipulation): ValidationResult {
    return this.validator.validateManipulation(manipulation);
  }

  /**
   * Creates a safe wrapper around an AST node
   */
  public createSafeNode(node: ZeroCopyASTNode): SafeASTNode {
    return new SafeASTNode(node, this);
  }

  /**
   * Updates agent configuration
   */
  public updateConfiguration(newConfig: Partial<AIAgentConfiguration>): void {
    const oldConfig = { ...this.config };
    this.config = { ...this.config, ...newConfig };

    // Recreate guard if safety mode changed
    if (oldConfig.safetyMode !== this.config.safetyMode) {
      this.guard = this.createGuardForSafetyMode(this.grammar, this.config.safetyMode);
    }

    this.emit('configuration_updated', { oldConfig, newConfig: this.config });
  }

  /**
   * Gets learning statistics
   */
  public getLearningStats(): LearningStats {
    const totalRequests = this.learningHistory.length;
    const successfulRequests = this.learningHistory.filter(l => l.result.success).length;
    const averageConfidence = this.learningHistory.reduce((sum, l) => sum + l.result.confidence, 0) / totalRequests;

    const manipulationTypes = this.learningHistory.reduce((acc, l) => {
      l.result.manipulations.forEach(m => {
        acc[m.type] = (acc[m.type] || 0) + 1;
      });
      return acc;
    }, {} as Record<ManipulationType, number>);

    return {
      totalRequests,
      successfulRequests,
      successRate: successfulRequests / totalRequests,
      averageConfidence,
      manipulationTypes,
      recentHistory: this.learningHistory.slice(-20),
    };
  }

  /**
   * Analyzes the context for a manipulation request
   */
  private async analyzeManipulationContext(request: AIManipulationRequest): Promise<ContextAnalysis> {
    const { targetNode, context, constraints } = request;

    // Parse surrounding context if not provided
    let effectiveContext: ScopeInfo;
    if (context) {
      effectiveContext = context;
    } else {
      const _parseResult = await this.contextParser.parseCode(
        targetNode.value || targetNode.name || '',
        'javascript',  // Default language since ZeroCopyASTNode doesn't have language property
      );
      // Extract or create a basic ScopeInfo from ParseStateInfo
      effectiveContext = {
        id: 'generated-scope',
        type: ScopeType.BLOCK,
        startPosition: { line: 1, column: 1, offset: 0 },
        endPosition: {
          line: 1,
          column: (targetNode.value || targetNode.name || '').length + 1,
          offset: (targetNode.value || targetNode.name || '').length,
        },
        variables: [],
        functions: [],
        classes: [],
        parent: undefined,
        children: [],
        depth: 0,
        context: [],
      };
    }

    // Analyze constraints
    const parsedConstraints = this.parseConstraints(constraints || []);

    // Determine available manipulation types
    const availableTypes = this.getAvailableManipulations(targetNode.nodeType);

    // Assess risk factors
    const riskFactors = this.assessRiskFactors(targetNode, effectiveContext);

    return {
      _context: effectiveContext,
      constraints: parsedConstraints,
      availableTypes,
      riskFactors,
      complexity: this.calculateComplexity(targetNode, effectiveContext),
    };
  }

  /**
   * Generates multiple manipulation candidates
   */
  private async generateManipulationCandidates(
    request: AIManipulationRequest,
    analysis: ContextAnalysis,
  ): Promise<ManipulationCandidate[]> {
    const candidates: ManipulationCandidate[] = [];

    // Generate candidates based on description and context
    for (const manipulationType of analysis.availableTypes) {
      const candidate = await this.generateCandidateForType(request, manipulationType, analysis);
      if (candidate) {
        candidates.push(candidate);
      }
    }

    // Generate alternative approaches
    const alternatives = await this.generateAlternativeApproaches(request, analysis);
    candidates.push(...alternatives);

    return candidates;
  }

  /**
   * Validates manipulation candidates
   */
  private validateCandidates(candidates: ManipulationCandidate[]): ValidatedCandidate[] {
    return candidates.map(candidate => {
      const validationResult = this.validator.validateManipulation(candidate.manipulation);
      const guardResult = this.guard.guardManipulation(candidate.manipulation);

      return {
        ...candidate,
        validationResult,
        isGuardApproved: guardResult,
        overallValid: validationResult.isValid && guardResult,
      };
    });
  }

  /**
   * Selects the best candidate from validated options
   */
  private selectBestCandidate(
    candidates: ValidatedCandidate[],
    request: AIManipulationRequest,
  ): ValidatedCandidate | null {
    // Filter to only valid candidates
    const validCandidates = candidates.filter(c => c.overallValid);

    if (validCandidates.length === 0) {
      return null;
    }

    // Score candidates based on multiple factors
    const scoredCandidates = validCandidates.map(candidate => ({
      ...candidate,
      score: this.calculateCandidateScore(candidate, request),
    }));

    // Sort by score and return the best
    scoredCandidates.sort((a, b) => b.score - a.score);
    return scoredCandidates[0];
  }

  /**
   * Applies manipulation with guard protection
   */
  private async applyGuardedManipulation(
    candidate: ValidatedCandidate | null,
    request: AIManipulationRequest,
  ): Promise<AIManipulationResult> {
    if (!candidate) {
      return {
        success: false,
        manipulations: [],
        validationResults: [],
        confidence: 0,
        reasoning: 'No valid manipulation candidates found',
        errors: ['No valid manipulation candidates could be generated'],
      };
    }

    try {
      // Create guarded node
      const guardedNode = this.guard.createGuardedNode(request.targetNode);

      // Apply the manipulation
      const success = await this.executeManipulation(candidate.manipulation, guardedNode);

      return {
        success,
        manipulations: [candidate.manipulation],
        validationResults: [candidate.validationResult],
        confidence: candidate.confidence,
        reasoning: candidate.reasoning,
        alternatives: candidate.alternatives?.map(c => c.manipulation),
      };

    } catch (error) {
      return {
        success: false,
        manipulations: [candidate.manipulation],
        validationResults: [candidate.validationResult],
        confidence: candidate.confidence,
        reasoning: `Execution failed: ${error instanceof Error ? error.message : String(error)}`,
        errors: [error instanceof Error ? error.message : String(error)],
      };
    }
  }

  /**
   * Executes a specific manipulation
   */
  private async executeManipulation(manipulation: ASTManipulation, guardedNode: GuardedASTNode): Promise<boolean> {
    switch (manipulation.type) {
      case ManipulationType.INSERT_CHILD:
        return guardedNode.addChild(manipulation.newNode!, manipulation.position);

      case ManipulationType.REMOVE_CHILD:
        return guardedNode.removeChild(manipulation.position!);

      case ManipulationType.REPLACE_NODE:
        return guardedNode.replaceWith(manipulation.newNode!);

      case ManipulationType.MODIFY_VALUE:
        return guardedNode.setValue(manipulation.newValue);

      default:
        throw new Error(`Unsupported manipulation type: ${manipulation.type}`);
    }
  }

  /**
   * Records learning data for future improvements
   */
  private recordLearningData(request: AIManipulationRequest, result: AIManipulationResult): void {
    const learningData: LearningData = {
      _request: request,
      result,
      timestamp: Date.now(),
    };

    this.learningHistory.push(learningData);

    // Limit history size
    if (this.learningHistory.length > 1000) {
      this.learningHistory = this.learningHistory.slice(-800);
    }

    this.emit('learning_data_recorded', learningData);
  }

  /**
   * Creates appropriate guard based on safety mode
   */
  private createGuardForSafetyMode(grammar: Grammar, safetyMode: string): ASTGuard {
    switch (safetyMode) {
      case 'strict':
        return ASTGuardFactory.createStrictGuard(grammar);
      case 'permissive':
        return ASTGuardFactory.createPermissiveGuard(grammar);
      case 'balanced':
      default:
        return ASTGuardFactory.createDevelopmentGuard(grammar);
    }
  }

  /**
   * Sets up event handlers for monitoring
   */
  private setupEventHandlers(): void {
    this.guard.on('validation_failed', (event) => {
      this.emit('guard_validation_failed', event);
    });

    this.guard.on('manipulation_allowed', (event) => {
      this.emit('guard_manipulation_allowed', event);
    });

    this.guard.on('auto_correction', (event) => {
      this.emit('guard_auto_correction', event);
    });
  }

  // Helper methods (simplified implementations)
  private getAvailableManipulations(nodeType: ASTNodeType): ManipulationType[] {
    // Return available manipulation types based on node type
    const baseTypes = [ManipulationType.MODIFY_VALUE, ManipulationType.REPLACE_NODE];

    if (this.canHaveChildren(nodeType)) {
      baseTypes.push(ManipulationType.INSERT_CHILD, ManipulationType.REMOVE_CHILD);
    }

    return baseTypes;
  }

  private canHaveChildren(nodeType: ASTNodeType): boolean {
    return [
      ASTNodeType.PROGRAM,
      ASTNodeType.BLOCK,
      ASTNodeType.FUNCTION_DECLARATION,
      ASTNodeType.CLASS_DECLARATION,
      ASTNodeType.IF_STATEMENT,
      ASTNodeType.WHILE_LOOP,
      ASTNodeType.FOR_LOOP,
    ].includes(nodeType);
  }

  private parseConstraints(constraints: string[]): ParsedConstraint[] {
    return constraints.map(constraint => ({
      type: 'user_defined',
      description: constraint,
      validator: () => true, // Simplified
    }));
  }

  private assessRiskFactors(node: ZeroCopyASTNode, context: ScopeInfo): RiskFactor[] {
    const risks: RiskFactor[] = [];

    // Check for critical nodes
    if (this.isCriticalNode(node)) {
      risks.push({
        type: 'critical_node',
        severity: 'high',
        description: 'Modifying critical system node',
      });
    }

    // Check for complex dependencies
    if (this.hasComplexDependencies(node, context)) {
      risks.push({
        type: 'complex_dependencies',
        severity: 'medium',
        description: 'Node has complex dependencies',
      });
    }

    return risks;
  }

  private calculateComplexity(node: ZeroCopyASTNode, context: ScopeInfo): number {
    let complexity = 1;
    complexity += node.childCount * 0.1;
    complexity += context.depth * 0.05;
    complexity += context.variables.length * 0.02;
    return Math.min(complexity, 10); // Cap at 10
  }

  private async generateCandidateForType(
    request: AIManipulationRequest,
    type: ManipulationType,
    _analysis: ContextAnalysis,
  ): Promise<ManipulationCandidate | null> {
    // Simplified candidate generation
    return {
      manipulation: {
        type,
        targetNode: request.targetNode,
        newNode: undefined, // Would be generated based on request
        newValue: undefined,
        position: undefined,
      },
      confidence: 0.8,
      reasoning: `Generated ${type} manipulation based on request`,
      alternatives: [],
    };
  }

  private async generateAlternativeApproaches(
    _request: AIManipulationRequest,
    _analysis: ContextAnalysis,
  ): Promise<ManipulationCandidate[]> {
    // Generate alternative approaches
    return [];
  }

  private calculateCandidateScore(candidate: ValidatedCandidate, _request: AIManipulationRequest): number {
    let score = candidate.confidence;

    // Boost score for safer manipulations
    if (candidate.validationResult.warnings.length === 0) {
      score += 0.2;
    }

    // Reduce score for risky manipulations
    if (candidate.validationResult.errors.length > 0) {
      score -= 0.5;
    }

    return Math.max(0, Math.min(1, score));
  }

  private async generateManipulationSuggestion(
    node: ZeroCopyASTNode,
    type: ManipulationType,
    _context?: ScopeInfo,
  ): Promise<AIManipulationSuggestion | null> {
    // Generate suggestion for the given manipulation type
    return {
      type,
      description: `Suggested ${type} manipulation`,
      confidence: 0.7,
      safetyScore: 0.8,
      manipulation: {
        type,
        targetNode: node,
      },
    };
  }

  private isCriticalNode(node: ZeroCopyASTNode): boolean {
    return [ASTNodeType.PROGRAM, ASTNodeType.FUNCTION_DECLARATION].includes(node.nodeType);
  }

  private hasComplexDependencies(node: ZeroCopyASTNode, context: ScopeInfo): boolean {
    return context.variables.length > 10 || context.functions.length > 5;
  }
}

/**
 * Safe wrapper around AST nodes with AI-driven manipulation capabilities
 */
export class SafeASTNode {
  private node: ZeroCopyASTNode;
  private agent: SafeAIAgent;

  constructor(node: ZeroCopyASTNode, agent: SafeAIAgent) {
    this.node = node;
    this.agent = agent;
  }

  /**
   * Requests AI-driven manipulation with natural language
   */
  public async requestManipulation(description: string, constraints?: string[]): Promise<AIManipulationResult> {
    const _request: AIManipulationRequest = {
      description,
      targetNode: this.node,
      constraints,
    };

    return this.agent.performSafeManipulation(_request);
  }

  /**
   * Gets safe manipulation suggestions
   */
  public async getSuggestions(): Promise<AIManipulationSuggestion[]> {
    return this.agent.suggestSafeManipulations(this.node);
  }

  /**
   * Gets the underlying node
   */
  public getNode(): ZeroCopyASTNode {
    return this.node;
  }
}

// Supporting interfaces
interface ContextAnalysis {
  _context: ScopeInfo;
  constraints: ParsedConstraint[];
  availableTypes: ManipulationType[];
  riskFactors: RiskFactor[];
  complexity: number;
}

interface ParsedConstraint {
  type: string;
  description: string;
  validator: () => boolean;
}

interface RiskFactor {
  type: string;
  severity: 'low' | 'medium' | 'high';
  description: string;
}

interface ManipulationCandidate {
  manipulation: ASTManipulation;
  confidence: number;
  reasoning: string;
  alternatives: ManipulationCandidate[];
}

interface ValidatedCandidate extends ManipulationCandidate {
  validationResult: ValidationResult;
  isGuardApproved: boolean;
  overallValid: boolean;
  score?: number;
}

export interface AIManipulationSuggestion {
  type: ManipulationType;
  description: string;
  confidence: number;
  safetyScore: number;
  manipulation: ASTManipulation;
}

interface LearningStats {
  totalRequests: number;
  successfulRequests: number;
  successRate: number;
  averageConfidence: number;
  manipulationTypes: Record<ManipulationType, number>;
  recentHistory: LearningData[];
}

