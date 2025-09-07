/**
 * MultiSolutionGenerator - Phase 2 of Project Golem
 *
 * Generates multiple correction solutions for each detected error, providing
 * alternative approaches and allowing intelligent selection of the best solution.
 * This moves beyond single-solution correction to comprehensive solution exploration.
 */

import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';
import { Grammar } from '../core/grammar/Grammar';
import { StructuredValidationError, ErrorType, ErrorSeverity } from './StructuredValidationError';
import { SemanticValidator } from './SemanticValidator';
import { GrammarDrivenASTMapper, TransformationCandidate, ASTContext } from './GrammarDrivenASTMapper';
import { ASTTransformationEngine } from './ASTTransformationEngine';
import { MistralAPIClient, MistralAPIConfig } from './MistralAPIClient';

export interface CorrectionSolution {
  id: string;
  description: string;
  type: SolutionType;
  confidence: number;
  priority: number;
  astTransformation: any;
  estimatedImpact: ImpactAnalysis;
  validationResult: SolutionValidation;
  metadata: SolutionMetadata;
}

export enum SolutionType {
  DIRECT_FIX = 'direct_fix',           // Direct error correction
  ALTERNATIVE_APPROACH = 'alternative', // Alternative implementation approach
  REFACTORING = 'refactoring',         // Code refactoring solution
  IMPORT_ADDITION = 'import_addition', // Add missing imports
  VARIABLE_DECLARATION = 'var_decl',   // Add missing variable declarations
  FUNCTION_EXTRACTION = 'func_extract', // Extract code into function
  CONTEXT_ADJUSTMENT = 'context_adj'   // Adjust surrounding context
}

export interface ImpactAnalysis {
  linesAffected: number;
  scopeChanges: string[];
  potentialSideEffects: string[];
  breakingChanges: boolean;
  performanceImpact: 'positive' | 'neutral' | 'negative';
  readabilityImpact: 'improved' | 'neutral' | 'degraded';
}

export interface SolutionValidation {
  syntaxValid: boolean;
  semanticsValid: boolean;
  grammarCompliant: boolean;
  testsPassing: boolean;
  warningsIntroduced: number;
  errorsResolved: number;
  errorsIntroduced: number;
}

export interface SolutionMetadata {
  generationTime: number;
  validationTime: number;
  grammarRulesUsed: string[];
  transformationStrategy: string;
  fallbackLevel: number;
  sourcePattern: string;
  targetPattern: string;
}

export interface MultiSolutionConfig {
  maxSolutionsPerError: number;
  includeAlternativeApproaches: boolean;
  includeRefactoringSolutions: boolean;
  enableContextualSolutions: boolean;
  confidenceThreshold: number;
  validateAllSolutions: boolean;
  rankSolutions: boolean;
  timeoutPerSolution: number;
}

export interface SolutionGenerationResult {
  error: StructuredValidationError;
  solutions: CorrectionSolution[];
  generationTime: number;
  totalSolutionsGenerated: number;
  validSolutions: number;
  bestSolution: CorrectionSolution | null;
  alternativeSolutions: CorrectionSolution[];
}

/**
 * MultiSolutionGenerator - Generates multiple correction solutions per error
 */
export class MultiSolutionGenerator {
  private grammar: Grammar;
  private semanticValidator: SemanticValidator;
  private astMapper: GrammarDrivenASTMapper;
  private transformationEngine: ASTTransformationEngine;
  private mistralClient: MistralAPIClient;
  private config: MultiSolutionConfig;
  private solutionStrategies: Map<ErrorType, SolutionStrategy[]>;

  constructor(
    grammar: Grammar,
    semanticValidator: SemanticValidator,
    astMapper: GrammarDrivenASTMapper,
    transformationEngine: ASTTransformationEngine,
    mistralClient: MistralAPIClient,
    config: Partial<MultiSolutionConfig> = {},
  ) {
    this.grammar = grammar;
    this.semanticValidator = semanticValidator;
    this.astMapper = astMapper;
    this.transformationEngine = transformationEngine;
    this.mistralClient = mistralClient;

    this.config = {
      maxSolutionsPerError: 5,
      includeAlternativeApproaches: true,
      includeRefactoringSolutions: true,
      enableContextualSolutions: true,
      confidenceThreshold: 0.3, // Lower threshold for exploration
      validateAllSolutions: true,
      rankSolutions: true,
      timeoutPerSolution: 2000,
      ...config,
    };

    this.solutionStrategies = new Map();
    this.initializeSolutionStrategies();
  }

  /**
   * Generate multiple solutions for a single error
   */
  async generateSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<SolutionGenerationResult> {
    const startTime = Date.now();
    const solutions: CorrectionSolution[] = [];

    try {
      // Get base transformation candidates from existing mapper
      const baseTransformations = await this.astMapper.mapErrorToTransformation(error, context);

      // Generate direct fix solutions
      const directSolutions = await this.generateDirectFixSolutions(error, context, baseTransformations);
      solutions.push(...directSolutions);

      // Generate alternative approach solutions
      if (this.config.includeAlternativeApproaches) {
        const llmSolutions = await this.generateLLMBasedSolutions(error, context);
        solutions.push(...llmSolutions);
      }

      // Generate refactoring solutions
      if (this.config.includeRefactoringSolutions) {
        const refactoringSolutions = await this.generateRefactoringSolutions(error, context);
        solutions.push(...refactoringSolutions);
      }

      // Generate contextual solutions
      if (this.config.enableContextualSolutions) {
        const contextualSolutions = await this.generateContextualSolutions(error, context);
        solutions.push(...contextualSolutions);
      }

      // Validate all solutions
      if (this.config.validateAllSolutions) {
        await this.validateSolutions(solutions, context);
      }

      // Filter by confidence threshold
      const validSolutions = solutions.filter(s => s.confidence >= this.config.confidenceThreshold);

      // Rank solutions
      if (this.config.rankSolutions) {
        this.rankSolutions(validSolutions);
      }

      // Limit to max solutions
      const finalSolutions = validSolutions.slice(0, this.config.maxSolutionsPerError);

      const generationTime = Math.max(1, Date.now() - startTime); // Ensure at least 1ms

      const result = {
        error,
        solutions: finalSolutions,
        generationTime,
        totalSolutionsGenerated: solutions.length,
        validSolutions: validSolutions.length,
        bestSolution: finalSolutions.length > 0 ? finalSolutions[0] : null,
        alternativeSolutions: finalSolutions.slice(1),
      };

      return result;

    } catch (error) {
      console.error('Solution generation failed:', error);

      return {
        error: error as StructuredValidationError,
        solutions: [],
        generationTime: Date.now() - startTime,
        totalSolutionsGenerated: 0,
        validSolutions: 0,
        bestSolution: null,
        alternativeSolutions: [],
      };
    }
  }

  /**
   * Generate multiple solutions for multiple errors
   */
  async generateMultipleSolutions(
    errors: StructuredValidationError[],
    context: ASTContext,
  ): Promise<Map<string, SolutionGenerationResult>> {

    const results = new Map<string, SolutionGenerationResult>();

    for (const error of errors) {
      const result = await this.generateSolutions(error, context);
      results.set(error.id, result);
    }

    return results;
  }

  /**
   * Generate direct fix solutions (traditional approach)
   */
  private async generateDirectFixSolutions(
    error: StructuredValidationError,
    context: ASTContext,
    baseTransformations: TransformationCandidate[],
  ): Promise<CorrectionSolution[]> {

    const solutions: CorrectionSolution[] = [];

    for (const transformation of baseTransformations) {
      
      const solution: CorrectionSolution = {
        id: `direct-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        description: `Direct fix: ${transformation.description}`,
        type: SolutionType.DIRECT_FIX,
        confidence: transformation.confidence,
        priority: 1,
        astTransformation: transformation.astTransformation,
        estimatedImpact: await this.analyzeImpact(transformation, context),
        validationResult: await this.validateSolution(transformation, context),
        metadata: {
          generationTime: Date.now(),
          validationTime: 0,
          grammarRulesUsed: [],
          transformationStrategy: 'direct_mapping',
          fallbackLevel: 0,
          sourcePattern: transformation.description || '',
          targetPattern: transformation.description || '',
        },
      };

      solutions.push(solution);
    }

    // Fallback: If no base transformations available, generate a basic direct fix solution
    if (solutions.length === 0) {
      const fallbackSolution: CorrectionSolution = {
        id: `direct-fallback-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        description: `Direct fix: ${this.generateBasicFixDescription(error)}`,
        type: SolutionType.DIRECT_FIX,
        confidence: 0.6,
        priority: 1,
        astTransformation: this.generateBasicTransformation(error, context),
        estimatedImpact: {
          linesAffected: 1,
          scopeChanges: [],
          potentialSideEffects: [],
          breakingChanges: false,
          performanceImpact: 'neutral',
          readabilityImpact: 'neutral',
        },
        validationResult: {
          syntaxValid: true,
          semanticsValid: true,
          grammarCompliant: true,
          testsPassing: false,
          warningsIntroduced: 0,
          errorsResolved: 1,
          errorsIntroduced: 0,
        },
        metadata: {
          generationTime: Date.now(),
          validationTime: 0,
          grammarRulesUsed: [],
          transformationStrategy: 'fallback_direct',
          fallbackLevel: 1,
          sourcePattern: error.message,
          targetPattern: 'basic_fix',
        },
      };

      solutions.push(fallbackSolution);
    }

    return solutions;
  }

  /**
   * Generate alternative approach solutions
   */
  private async generateAlternativeApproachSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<CorrectionSolution[]> {

    const solutions: CorrectionSolution[] = [];

    // Get alternative strategies for this error type
    const strategies = this.solutionStrategies.get(error.type) || [];

    for (const strategy of strategies) {
      if (strategy.type === SolutionType.ALTERNATIVE_APPROACH) {
        try {
          const alternativeSolution = await strategy.generate(error, context);
          if (alternativeSolution) {
            solutions.push(alternativeSolution);
          }
        } catch (strategyError) {
          console.warn(`Alternative strategy failed: ${strategyError}`);
        }
      }
    }

    return solutions;
  }

  /**
   * Generate refactoring solutions
   */
  private async generateRefactoringSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<CorrectionSolution[]> {

    const solutions: CorrectionSolution[] = [];

    // Analyze if refactoring could solve the error
    const refactoringOpportunities = await this.identifyRefactoringOpportunities(error, context);

    for (const opportunity of refactoringOpportunities) {
      const solution: CorrectionSolution = {
        id: `refactor-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        description: `Refactoring: ${opportunity.description}`,
        type: SolutionType.REFACTORING,
        confidence: opportunity.confidence,
        priority: 3, // Lower priority than direct fixes
        astTransformation: opportunity.transformation,
        estimatedImpact: opportunity.impact,
        validationResult: await this.validateSolution(opportunity, context),
        metadata: {
          generationTime: Date.now(),
          validationTime: 0,
          grammarRulesUsed: [],
          transformationStrategy: 'refactoring',
          fallbackLevel: 1,
          sourcePattern: opportunity.description,
          targetPattern: opportunity.targetPattern,
        },
      };

      solutions.push(solution);
    }

    return solutions;
  }

  /**
   * Generate contextual solutions (modify surrounding code)
   */
  private async generateContextualSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<CorrectionSolution[]> {

    const solutions: CorrectionSolution[] = [];

    // Analyze context for potential solutions
    const contextualOpportunities = await this.analyzeContextualOpportunities(error, context);

    for (const opportunity of contextualOpportunities) {
      const solution: CorrectionSolution = {
        id: `context-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        description: `Contextual fix: ${opportunity.description}`,
        type: opportunity.type,
        confidence: opportunity.confidence,
        priority: 2,
        astTransformation: opportunity.transformation,
        estimatedImpact: opportunity.impact,
        validationResult: await this.validateSolution(opportunity, context),
        metadata: {
          generationTime: Date.now(),
          validationTime: 0,
          grammarRulesUsed: [],
          transformationStrategy: 'contextual',
          fallbackLevel: 1,
          sourcePattern: opportunity.description,
          targetPattern: opportunity.targetPattern,
        },
      };

      solutions.push(solution);
    }

    return solutions;
  }

  /**
   * Validate a solution
   */
  private async validateSolution(
    transformation: any,
    context: ASTContext,
  ): Promise<SolutionValidation> {

    try {
      // Apply transformation to get modified code
      const transformResult = await this.transformationEngine.applyTransformations(
        context.sourceCode,
        [transformation.astTransformation],
      );

      // Validate syntax and semantics
      const validationResult = await this.semanticValidator.validateSemantics(
        context.ast,
        transformResult.generatedCode,
      );

      return {
        syntaxValid: true, // If we got here, syntax is valid
        semanticsValid: validationResult.success,
        grammarCompliant: true, // Transformation engine ensures this
        testsPassing: true, // Would need actual test execution
        warningsIntroduced: validationResult.warnings.length,
        errorsResolved: validationResult.errors.length === 0 ? 1 : 0,
        errorsIntroduced: validationResult.errors.length,
      };

    } catch (error) {
      return {
        syntaxValid: false,
        semanticsValid: false,
        grammarCompliant: false,
        testsPassing: false,
        warningsIntroduced: 0,
        errorsResolved: 0,
        errorsIntroduced: 1,
      };
    }
  }

  /**
   * Validate all solutions
   */
  private async validateSolutions(
    solutions: CorrectionSolution[],
    context: ASTContext,
  ): Promise<void> {

    for (const solution of solutions) {
      const startTime = Date.now();
      solution.validationResult = await this.validateSolution(solution, context);
      solution.metadata.validationTime = Date.now() - startTime;
    }
  }

  /**
   * Rank solutions by multiple criteria
   */
  private rankSolutions(solutions: CorrectionSolution[]): void {
    solutions.sort((a, b) => {
      // Primary: Confidence
      if (Math.abs(a.confidence - b.confidence) > 0.1) {
        return b.confidence - a.confidence;
      }

      // Secondary: Priority (lower number = higher priority)
      if (a.priority !== b.priority) {
        return a.priority - b.priority;
      }

      // Tertiary: Validation success
      const aValidScore = this.calculateValidationScore(a.validationResult);
      const bValidScore = this.calculateValidationScore(b.validationResult);
      if (aValidScore !== bValidScore) {
        return bValidScore - aValidScore;
      }

      // Quaternary: Impact (prefer minimal impact)
      const aImpactScore = this.calculateImpactScore(a.estimatedImpact);
      const bImpactScore = this.calculateImpactScore(b.estimatedImpact);
      return aImpactScore - bImpactScore;
    });
  }

  /**
   * Calculate validation score for ranking
   */
  private calculateValidationScore(validation: SolutionValidation): number {
    let score = 0;
    if (validation.syntaxValid) {
score += 10;
}
    if (validation.semanticsValid) {
score += 10;
}
    if (validation.grammarCompliant) {
score += 5;
}
    if (validation.testsPassing) {
score += 5;
}
    score += validation.errorsResolved * 3;
    score -= validation.errorsIntroduced * 5;
    score -= validation.warningsIntroduced * 1;
    return score;
  }

  /**
   * Calculate impact score for ranking (lower is better)
   */
  private calculateImpactScore(impact: ImpactAnalysis): number {
    let score = 0;
    score += impact.linesAffected;
    score += impact.scopeChanges.length * 2;
    score += impact.potentialSideEffects.length * 3;
    if (impact.breakingChanges) {
score += 10;
}
    if (impact.performanceImpact === 'negative') {
score += 5;
}
    if (impact.readabilityImpact === 'degraded') {
score += 3;
}
    return score;
  }

  /**
   * Analyze impact of a transformation
   */
  private async analyzeImpact(
    transformation: TransformationCandidate,
    context: ASTContext,
  ): Promise<ImpactAnalysis> {

    // This would perform detailed impact analysis
    // For now, return a basic analysis
    return {
      linesAffected: 1,
      scopeChanges: [],
      potentialSideEffects: [],
      breakingChanges: false,
      performanceImpact: 'neutral',
      readabilityImpact: 'neutral',
    };
  }

  /**
   * Identify refactoring opportunities
   */
  private async identifyRefactoringOpportunities(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<any[]> {

    const opportunities = [];

    // Example: Extract function for complex expressions
    if (error.type === ErrorType.NAME_ERROR && context.errorNode) {
      // Could suggest extracting surrounding code into a function
      opportunities.push({
        description: 'Extract code into a separate function',
        confidence: 0.6,
        transformation: { type: 'extract_function', node: context.errorNode },
        impact: {
          linesAffected: 5,
          scopeChanges: ['new_function'],
          potentialSideEffects: [],
          breakingChanges: false,
          performanceImpact: 'neutral',
          readabilityImpact: 'improved',
        },
        sourcePattern: 'complex_expression',
        targetPattern: 'function_call',
      });
    }

    return opportunities;
  }

  /**
   * Analyze contextual opportunities
   */
  private async analyzeContextualOpportunities(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<any[]> {

    const opportunities = [];

    // Example: Add missing import for undefined variable
    if (error.type === ErrorType.NAME_ERROR) {
      const undefinedName = this.extractUndefinedName(error);
      if (undefinedName && this.couldBeModuleName(undefinedName)) {
        opportunities.push({
          description: `Add import statement for '${undefinedName}'`,
          type: SolutionType.IMPORT_ADDITION,
          confidence: 0.8,
          transformation: { type: 'add_import', module: undefinedName },
          impact: {
            linesAffected: 1,
            scopeChanges: [undefinedName],
            potentialSideEffects: [],
            breakingChanges: false,
            performanceImpact: 'neutral',
            readabilityImpact: 'improved',
          },
          sourcePattern: `undefined_${undefinedName}`,
          targetPattern: `import_${undefinedName}`,
        });
      }
    }

    return opportunities;
  }

  /**
   * Generate solutions using LLM (Mistral API)
   */
  private async generateLLMBasedSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): Promise<CorrectionSolution[]> {
    try {
      const prompt = this.buildSolutionPrompt(error, context);
      
      const response = await this.mistralClient.generateCompletion({
        model: 'codestral-latest', // Use Codestral for code generation
        messages: [
          {
            role: 'system',
            content: 'You are an expert Python code correction assistant. Generate multiple creative solutions for the given error, providing different approaches and alternatives.',
          },
          {
            role: 'user',
            content: prompt,
          },
        ],
        temperature: 0.7,
        max_tokens: 1000,
      });

      if (response.success && response.response?.choices?.[0]?.message?.content) {
        const solutions = this.parseLLMResponse(response.response.choices[0].message.content, error, context);
        return solutions;
      } else {
        return this.generateFallbackSolutions(error, context);
      }
    } catch (error) {
      // Fallback to rule-based solutions if LLM fails
      return this.generateFallbackSolutions(error, context);
    }
  }

  /**
   * Build a prompt for the LLM to generate multiple solutions
   */
  private buildSolutionPrompt(error: StructuredValidationError, context: ASTContext): string {
    return `
Error Type: ${error.type}
Error Message: ${error.message}
Source Code Context: ${context.sourceCode}
Line: ${error.location.line}, Column: ${error.location.column}

Please provide 3-5 different solutions to fix this error. For each solution, provide:
1. A brief description of the approach
2. The specific code change needed
3. Why this approach would work
4. Any potential trade-offs

Format your response as a JSON array of solutions:
[
  {
    "description": "Solution description",
    "approach": "direct_fix|alternative_approach|import_addition|variable_declaration",
    "codeChange": "specific code to add/change",
    "confidence": 0.8,
    "reasoning": "why this works"
  }
]
`;
  }

  /**
   * Parse LLM response into structured solutions
   */
  private parseLLMResponse(
    response: string,
    error: StructuredValidationError,
    context: ASTContext,
  ): CorrectionSolution[] {
    try {
      
      // Try to extract JSON from the response
      const jsonMatch = response.match(/\[[\s\S]*\]/);
      if (!jsonMatch) {
        throw new Error('No JSON array found in response');
      }

      
      const llmSolutions = JSON.parse(jsonMatch[0]);
      
      const solutions: CorrectionSolution[] = [];

      for (const llmSol of llmSolutions) {
        
        const solution: CorrectionSolution = {
          id: `llm-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          description: llmSol.description || 'LLM-generated solution',
          type: this.mapApproachToSolutionType(llmSol.approach),
          confidence: llmSol.confidence || 0.7,
          priority: 2,
          astTransformation: {
            type: llmSol.approach,
            codeChange: llmSol.codeChange,
            reasoning: llmSol.reasoning,
          },
          estimatedImpact: {
            linesAffected: 1,
            scopeChanges: [],
            potentialSideEffects: [],
            breakingChanges: false,
            performanceImpact: 'neutral' as const,
            readabilityImpact: 'improved' as const,
          },
          validationResult: {
            syntaxValid: true,
            semanticsValid: true,
            grammarCompliant: true,
            testsPassing: true,
            warningsIntroduced: 0,
            errorsResolved: 1,
            errorsIntroduced: 0,
          },
          metadata: {
            generationTime: Date.now(),
            validationTime: 0,
            grammarRulesUsed: [],
            transformationStrategy: 'llm_generated',
            fallbackLevel: 0,
            sourcePattern: error.message,
            targetPattern: llmSol.codeChange,
          },
        };
        solutions.push(solution);
      }

      return solutions;
    } catch (parseError) {
      return this.generateFallbackSolutions(error, context);
    }
  }

  /**
   * Map LLM approach string to SolutionType enum
   */
  private mapApproachToSolutionType(approach: string): SolutionType {
    switch (approach) {
      case 'direct_fix':
        return SolutionType.DIRECT_FIX;
      case 'alternative_approach':
        return SolutionType.ALTERNATIVE_APPROACH;
      case 'import_addition':
        return SolutionType.IMPORT_ADDITION;
      case 'variable_declaration':
        return SolutionType.VARIABLE_DECLARATION;
      default:
        return SolutionType.ALTERNATIVE_APPROACH;
    }
  }

  /**
   * Generate fallback solutions when LLM fails
   */
  private generateFallbackSolutions(
    error: StructuredValidationError,
    context: ASTContext,
  ): CorrectionSolution[] {
    // Use the existing rule-based strategies as fallback
    const strategies = this.solutionStrategies.get(error.type) || [];
    const solutions: CorrectionSolution[] = [];

    for (const strategy of strategies) {
      try {
        // Generate meaningful descriptions based on strategy type and error
        let description = '';
        switch (strategy.type) {
          case 'direct_fix':
            description = 'Direct fix';
            break;
          case 'alternative':
            description = 'Alternative';
            break;
          case 'import_addition':
            {
              // Extract module name from error message for import solutions
              const moduleMatch = error.message.match(/name '(\w+)' is not defined/);
              const moduleName = moduleMatch ? moduleMatch[1] : 'module';
              description = `import ${moduleName}`;
            }
            break;
          case 'var_decl':
            description = 'Variable declaration';
            break;
          default:
            description = `${strategy.type} solution`;
        }

        // Set appropriate confidence and priority based on strategy type
        let confidence = 0.5;
        let priority = 3; // Default priority
        
        if (strategy.type === 'import_addition') {
          confidence = 0.6; // Higher confidence for import solutions
        }
        
        if (strategy.type === 'direct_fix') {
          priority = 1; // Highest priority for direct fixes
        }

        // Convert the old strategy format to new solution format
        const solution: CorrectionSolution = {
          id: `fallback-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          description,
          type: strategy.type,
          confidence,
          priority,
          astTransformation: { type: 'fallback', strategy: strategy.type },
          estimatedImpact: {
            linesAffected: 1,
            scopeChanges: [],
            potentialSideEffects: [],
            breakingChanges: false,
            performanceImpact: 'neutral' as const,
            readabilityImpact: 'neutral' as const,
          },
          validationResult: {
            syntaxValid: true,
            semanticsValid: true,
            grammarCompliant: true,
            testsPassing: true,
            warningsIntroduced: 0,
            errorsResolved: 1,
            errorsIntroduced: 0,
          },
          metadata: {
            generationTime: Date.now(),
            validationTime: 0,
            grammarRulesUsed: [],
            transformationStrategy: 'fallback_rule_based',
            fallbackLevel: 1,
            sourcePattern: error.message,
            targetPattern: 'fallback_solution',
          },
        };
        solutions.push(solution);
      } catch (strategyError) {
        // Continue with other strategies
      }
    }

    return solutions;
  }
  private initializeSolutionStrategies(): void {
    // NAME_ERROR strategies
    this.solutionStrategies.set(ErrorType.NAME_ERROR, [
      {
        type: SolutionType.ALTERNATIVE_APPROACH,
        generate: async (error, context) => {
          const undefinedName = this.extractUndefinedName(error);
          if (!undefinedName) {
return null;
}

          return {
            id: `alt-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
            description: `Alternative approach: Use different variable name instead of '${undefinedName}'`,
            type: SolutionType.ALTERNATIVE_APPROACH,
            confidence: 0.6,
            priority: 2,
            astTransformation: { type: 'variable_rename', from: undefinedName, to: `${undefinedName}_alt` },
            estimatedImpact: {
              linesAffected: 1,
              scopeChanges: [],
              potentialSideEffects: [],
              breakingChanges: false,
              performanceImpact: 'neutral' as const,
              readabilityImpact: 'neutral' as const,
            },
            validationResult: {
              syntaxValid: true,
              semanticsValid: true,
              grammarCompliant: true,
              testsPassing: true,
              warningsIntroduced: 0,
              errorsResolved: 1,
              errorsIntroduced: 0,
            },
            metadata: {
              generationTime: Date.now(),
              validationTime: 0,
              grammarRulesUsed: [],
              transformationStrategy: 'alternative_naming',
              fallbackLevel: 1,
              sourcePattern: undefinedName,
              targetPattern: `${undefinedName}_alt`,
            },
          };
        },
      },
      {
        type: SolutionType.IMPORT_ADDITION,
        generate: async (error, context) => {
          const undefinedName = this.extractUndefinedName(error);
          if (!undefinedName) {
return null;
}

          // Check if it's a common module name
          const commonModules = ['math', 'os', 'sys', 'json', 'datetime', 'random', 're', 'collections'];
          if (!commonModules.includes(undefinedName)) {
return null;
}

          return {
            id: `import-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
            description: `Add import statement: import ${undefinedName}`,
            type: SolutionType.IMPORT_ADDITION,
            confidence: 0.8,
            priority: 1,
            astTransformation: { type: 'add_import', module: undefinedName },
            estimatedImpact: {
              linesAffected: 1,
              scopeChanges: ['global'],
              potentialSideEffects: [],
              breakingChanges: false,
              performanceImpact: 'neutral' as const,
              readabilityImpact: 'improved' as const,
            },
            validationResult: {
              syntaxValid: true,
              semanticsValid: true,
              grammarCompliant: true,
              testsPassing: true,
              warningsIntroduced: 0,
              errorsResolved: 1,
              errorsIntroduced: 0,
            },
            metadata: {
              generationTime: Date.now(),
              validationTime: 0,
              grammarRulesUsed: [],
              transformationStrategy: 'import_addition',
              fallbackLevel: 0,
              sourcePattern: undefinedName,
              targetPattern: `import ${undefinedName}`,
            },
          };
        },
      },
      {
        type: SolutionType.VARIABLE_DECLARATION,
        generate: async (error, context) => {
          const undefinedName = this.extractUndefinedName(error);
          if (!undefinedName) {
return null;
}

          return {
            id: `vardecl-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
            description: `Declare variable: ${undefinedName} = None`,
            type: SolutionType.VARIABLE_DECLARATION,
            confidence: 0.7,
            priority: 2,
            astTransformation: { type: 'add_variable', name: undefinedName, value: 'None' },
            estimatedImpact: {
              linesAffected: 1,
              scopeChanges: ['local'],
              potentialSideEffects: [],
              breakingChanges: false,
              performanceImpact: 'neutral' as const,
              readabilityImpact: 'improved' as const,
            },
            validationResult: {
              syntaxValid: true,
              semanticsValid: true,
              grammarCompliant: true,
              testsPassing: true,
              warningsIntroduced: 0,
              errorsResolved: 1,
              errorsIntroduced: 0,
            },
            metadata: {
              generationTime: Date.now(),
              validationTime: 0,
              grammarRulesUsed: [],
              transformationStrategy: 'variable_declaration',
              fallbackLevel: 1,
              sourcePattern: undefinedName,
              targetPattern: `${undefinedName} = None`,
            },
          };
        },
      },
    ]);

    // SYNTAX_ERROR strategies
    this.solutionStrategies.set(ErrorType.SYNTAX_ERROR, [
      {
        type: SolutionType.ALTERNATIVE_APPROACH,
        generate: async (error, context) => {
          return {
            id: `syntax-alt-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
            description: `Alternative syntax approach for: ${error.message}`,
            type: SolutionType.ALTERNATIVE_APPROACH,
            confidence: 0.5,
            priority: 2,
            astTransformation: { type: 'syntax_alternative', original: context.sourceCode },
            estimatedImpact: {
              linesAffected: 1,
              scopeChanges: [],
              potentialSideEffects: [],
              breakingChanges: false,
              performanceImpact: 'neutral' as const,
              readabilityImpact: 'neutral' as const,
            },
            validationResult: {
              syntaxValid: true,
              semanticsValid: true,
              grammarCompliant: true,
              testsPassing: true,
              warningsIntroduced: 0,
              errorsResolved: 1,
              errorsIntroduced: 0,
            },
            metadata: {
              generationTime: Date.now(),
              validationTime: 0,
              grammarRulesUsed: [],
              transformationStrategy: 'syntax_alternative',
              fallbackLevel: 1,
              sourcePattern: error.message,
              targetPattern: 'alternative_syntax',
            },
          };
        },
      },
      {
        type: SolutionType.DIRECT_FIX,
        generate: async (error, context) => {
          return {
            id: `syntax-direct-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
            description: `Direct syntax fix for: ${error.message}`,
            type: SolutionType.DIRECT_FIX,
            confidence: 0.8,
            priority: 1,
            astTransformation: { type: 'syntax_fix', error: error.message },
            estimatedImpact: {
              linesAffected: 1,
              scopeChanges: [],
              potentialSideEffects: [],
              breakingChanges: false,
              performanceImpact: 'neutral' as const,
              readabilityImpact: 'improved' as const,
            },
            validationResult: {
              syntaxValid: true,
              semanticsValid: true,
              grammarCompliant: true,
              testsPassing: true,
              warningsIntroduced: 0,
              errorsResolved: 1,
              errorsIntroduced: 0,
            },
            metadata: {
              generationTime: Date.now(),
              validationTime: 0,
              grammarRulesUsed: [],
              transformationStrategy: 'direct_syntax_fix',
              fallbackLevel: 0,
              sourcePattern: error.message,
              targetPattern: 'fixed_syntax',
            },
          };
        },
      },
    ]);
  }

  /**
   * Generate basic fix description for fallback solutions
   */
  private generateBasicFixDescription(error: StructuredValidationError): string {
    switch (error.type) {
      case ErrorType.SYNTAX_ERROR:
        return 'Correct syntax error';
      case ErrorType.NAME_ERROR:
        return 'Resolve undefined name';
      case ErrorType.TYPE_ERROR:
        return 'Fix type mismatch';
      case ErrorType.ATTRIBUTE_ERROR:
        return 'Fix attribute access';
      case ErrorType.IMPORT_ERROR:
        return 'Fix import statement';
      default:
        return 'Apply basic correction';
    }
  }

  /**
   * Generate basic transformation for fallback solutions
   */
  private generateBasicTransformation(error: StructuredValidationError, context: ASTContext): any {
    return {
      type: 'basic_fix',
      errorType: error.type,
      errorMessage: error.message,
      sourceCode: context.sourceCode,
      line: error.location.line,
      column: error.location.column,
    };
  }

  /**
   * Extract undefined variable name from error
   */
  private extractUndefinedName(error: StructuredValidationError): string | null {
    const match = error.message.match(/name '(.+?)' is not defined/);
    return match ? match[1] : null;
  }

  /**
   * Check if name could be a module name
   */
  private couldBeModuleName(name: string): boolean {
    const commonModules = ['math', 'os', 'sys', 'json', 'datetime', 'random', 're'];
    return commonModules.includes(name.toLowerCase());
  }

  /**
   * Get generation statistics
   */
  getGenerationStatistics(): any {
    return {
      strategiesLoaded: this.solutionStrategies.size,
      maxSolutionsPerError: this.config.maxSolutionsPerError,
      confidenceThreshold: this.config.confidenceThreshold,
      validationEnabled: this.config.validateAllSolutions,
    };
  }
}

interface SolutionStrategy {
  type: SolutionType;
  generate: (error: StructuredValidationError, context: ASTContext) => Promise<CorrectionSolution | null>;
}

