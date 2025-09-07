/**
 * MultiSolutionCorrector - Phase 2 Integration
 *
 * Integrates multi-solution generation with intelligent selection to provide
 * the complete Phase 2 correction pipeline. Replaces single-solution approach
 * with comprehensive multi-solution analysis and smart selection.
 */

import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StructuredValidationError } from './StructuredValidationError';
import { SemanticValidator } from './SemanticValidator';
import { GrammarDrivenASTMapper, ASTContext } from './GrammarDrivenASTMapper';
import { ASTTransformationEngine } from './ASTTransformationEngine';
import { CodeGenerationEngine } from './CodeGenerationEngine';
import { MultiSolutionGenerator, MultiSolutionConfig, SolutionGenerationResult, CorrectionSolution } from './MultiSolutionGenerator';
import { SolutionSelectionEngine, SelectionCriteria, SelectionContext, SelectionResult, SelectionAlgorithm } from './SolutionSelectionEngine';
import { MistralAPIClient } from './MistralAPIClient';

export interface EnhancedCorrectionConfig {
  // Multi-solution generation config
  multiSolution: Partial<MultiSolutionConfig>;

  // Selection config
  selectionCriteria: Partial<SelectionCriteria>;
  selectionContext: Partial<SelectionContext>;
  selectionAlgorithm: SelectionAlgorithm;

  // General correction config
  maxAttempts: number;
  maxIterations: number;
  timeoutMs: number;
  enableLearning: boolean;
  validateTransformations: boolean;
  preserveFormatting: boolean;
}

export interface EnhancedCorrectionResult {
  success: boolean;
  correctedCode?: string;
  solutionResults: Map<string, SolutionGenerationResult>;
  selectionResults: Map<string, SelectionResult>;
  appliedSolutions: CorrectionSolution[];
  remainingErrors: StructuredValidationError[];
  metrics: EnhancedCorrectionMetrics;
  session: CorrectionSession;
}

export interface EnhancedCorrectionMetrics {
  totalErrors: number;
  errorsProcessed: number;
  errorsResolved: number;
  totalSolutionsGenerated: number;
  averageSolutionsPerError: number;
  selectionAccuracy: number;
  totalCorrectionTime: number;
  generationTime: number;
  selectionTime: number;
  validationTime: number;
  iterationCount: number;
  algorithmUsage: Map<SelectionAlgorithm, number>;
}

export interface CorrectionSession {
  sessionId: string;
  startTime: Date;
  endTime?: Date;
  sourceCode: string;
  finalCode?: string;
  errorHistory: StructuredValidationError[];
  solutionHistory: CorrectionSolution[];
  userFeedback: any[];
}

/**
 * MultiSolutionCorrector - Complete Phase 2 correction system
 */
export class MultiSolutionCorrector {
  private grammar: Grammar;
  private stepParser: StepParser;
  private semanticValidator: SemanticValidator;
  private astMapper: GrammarDrivenASTMapper;
  private transformationEngine: ASTTransformationEngine;
  private codeGenerator: CodeGenerationEngine;
  private multiSolutionGenerator: MultiSolutionGenerator;
  private selectionEngine: SolutionSelectionEngine;
  private config: EnhancedCorrectionConfig;

  constructor(
    grammar: Grammar,
    stepParser: StepParser,
    config: Partial<EnhancedCorrectionConfig> = {},
  ) {
    this.grammar = grammar;
    this.stepParser = stepParser;

    // Initialize config with defaults
    this.config = {
      multiSolution: {
        maxSolutionsPerError: 5,
        includeAlternativeApproaches: true,
        includeRefactoringSolutions: true,
        enableContextualSolutions: true,
        confidenceThreshold: 0.3,
        validateAllSolutions: true,
        rankSolutions: true,
        timeoutPerSolution: 2000,
      },
      selectionCriteria: {
        prioritizeMinimalChanges: true,
        prioritizeReadability: true,
        prioritizePerformance: false,
        allowBreakingChanges: false,
        preferDirectFixes: true,
        confidenceWeight: 0.4,
        impactWeight: 0.3,
        validationWeight: 0.2,
        contextWeight: 0.1,
      },
      selectionContext: {},
      selectionAlgorithm: SelectionAlgorithm.HYBRID,
      maxAttempts: 5,
      maxIterations: 3,
      timeoutMs: 30000,
      enableLearning: true,
      validateTransformations: true,
      preserveFormatting: true,
      ...config,
    };

    // Initialize components
    this.semanticValidator = new SemanticValidator(grammar, stepParser);
    this.astMapper = new GrammarDrivenASTMapper(grammar, this.semanticValidator);
    this.transformationEngine = new ASTTransformationEngine();
    this.codeGenerator = new CodeGenerationEngine();
    this.multiSolutionGenerator = new MultiSolutionGenerator(
      grammar,
      this.semanticValidator,
      this.astMapper,
      this.transformationEngine,
      new MistralAPIClient({
        apiKey: process.env.MISTRAL_API_KEY || '',
        rateLimit: {
          requestsPerMinute: 10,
          requestsPerHour: 100,
          tokensPerMinute: 1000,
          tokensPerHour: 10000,
          burstLimit: 3,
          adaptiveThrottling: true,
        },
        enableRequestQueuing: true,
        enableAdaptiveBackoff: true,
        enableCostTracking: false,
        logLevel: 'info',
      }),
      config.multiSolution || {},
    );

    this.selectionEngine = new SolutionSelectionEngine();
  }

  /**
   * Initialize the corrector
   */
  async initialize(): Promise<void> {
    await this.semanticValidator.initialize('python.grammar');
  }

  /**
   * Correct errors using multi-solution generation and intelligent selection
   */
  async correctErrors(sourceCode: string): Promise<EnhancedCorrectionResult> {
    const session = this.createCorrectionSession(sourceCode);
    const startTime = Date.now();

    try {
      let currentCode = sourceCode;
      let iteration = 0;
      const solutionResults = new Map<string, SolutionGenerationResult>();
      const selectionResults = new Map<string, SelectionResult>();
      const appliedSolutions: CorrectionSolution[] = [];
      const algorithmUsage = new Map<SelectionAlgorithm, number>();

      let totalGenerationTime = 0;
      let totalSelectionTime = 0;
      let totalValidationTime = 0;

      while (iteration < this.config.maxIterations) {
        iteration++;

        // Parse and validate current code
        const validationStart = Date.now();
        // Create a simple source container for the parser
        const sourceContainer = {
          getSourceLines: () => currentCode.split('\n'),
          getSourceText: () => currentCode,
        };

        const ast = await this.stepParser.parse('python', sourceContainer as any);

        // Convert ProductionMatch[] to ZeroCopyASTNode (simplified for now)
        const astNode = ast.length > 0 ? ast[0] as any : null;

        const validationResult = await this.semanticValidator.validateSemantics(astNode, currentCode);
        totalValidationTime += Date.now() - validationStart;

        if (validationResult.success) {
          // No more errors - correction complete
          break;
        }

        const errors = validationResult.errors;
        session.errorHistory.push(...errors);

        // Generate multiple solutions for each error
        const generationStart = Date.now();
        const context: ASTContext = {
          sourceCode: currentCode,
          ast: astNode,
          errorNode: null,
          scopeStack: [],
          typeEnvironment: {},
          controlFlowState: {},
          grammarProductions: [],
        };

        for (const error of errors) {
          const solutionResult = await this.multiSolutionGenerator.generateSolutions(error, context);
          solutionResults.set(error.id, solutionResult);
        }
        totalGenerationTime += Date.now() - generationStart;

        // Select best solution for each error
        const selectionStart = Date.now();
        for (const [errorId, solutionResult] of solutionResults) {
          if (solutionResult.solutions.length > 0) {
            const error = errors.find(e => e.id === errorId)!;
            const selectionResult = await this.selectionEngine.selectBestSolution(
              solutionResult.solutions,
              error,
              context,
            );

            selectionResults.set(errorId, selectionResult);

            // Track algorithm usage
            const algorithm = this.config.selectionAlgorithm;
            algorithmUsage.set(algorithm, (algorithmUsage.get(algorithm) || 0) + 1);
          }
        }
        totalSelectionTime += Date.now() - selectionStart;

        // Apply selected solutions
        const iterationSolutions: CorrectionSolution[] = [];
        for (const [errorId, selectionResult] of selectionResults) {
          iterationSolutions.push(selectionResult.selectedSolution);
        }

        if (iterationSolutions.length === 0) {
          // No solutions found - break to avoid infinite loop
          break;
        }

        // Apply solutions to code
        currentCode = await this.applySolutions(currentCode, iterationSolutions);
        appliedSolutions.push(...iterationSolutions);
        session.solutionHistory.push(...iterationSolutions);

        // Check timeout
        if (Date.now() - startTime > this.config.timeoutMs) {
          console.warn('Correction timeout reached');
          break;
        }
      }

      // Final validation
      const finalValidationStart = Date.now();
      // Create a simple source container for the parser
      const finalSourceContainer = {
        getSourceLines: () => currentCode.split('\n'),
        getSourceText: () => currentCode,
      };

      const finalAst = await this.stepParser.parse('python', finalSourceContainer as any);

      // Convert ProductionMatch[] to ZeroCopyASTNode (simplified for now)
      const finalAstNode = finalAst.length > 0 ? finalAst[0] as any : null;

      const finalValidation = await this.semanticValidator.validateSemantics(finalAstNode, currentCode);
      totalValidationTime += Date.now() - finalValidationStart;

      // Calculate metrics
      const totalTime = Date.now() - startTime;
      const totalSolutionsGenerated = Array.from(solutionResults.values())
        .reduce((sum, result) => sum + result.totalSolutionsGenerated, 0);

      const metrics: EnhancedCorrectionMetrics = {
        totalErrors: session.errorHistory.length,
        errorsProcessed: solutionResults.size,
        errorsResolved: appliedSolutions.length,
        totalSolutionsGenerated,
        averageSolutionsPerError: solutionResults.size > 0 ? totalSolutionsGenerated / solutionResults.size : 0,
        selectionAccuracy: this.calculateSelectionAccuracy(selectionResults),
        totalCorrectionTime: totalTime,
        generationTime: totalGenerationTime,
        selectionTime: totalSelectionTime,
        validationTime: totalValidationTime,
        iterationCount: iteration,
        algorithmUsage,
      };

      // Complete session
      session.endTime = new Date();
      session.finalCode = currentCode;

      return {
        success: finalValidation.success,
        correctedCode: finalValidation.success ? currentCode : undefined,
        solutionResults,
        selectionResults,
        appliedSolutions,
        remainingErrors: finalValidation.errors,
        metrics,
        session,
      };

    } catch (error) {
      console.error('Enhanced correction failed:', error);

      return {
        success: false,
        solutionResults: new Map(),
        selectionResults: new Map(),
        appliedSolutions: [],
        remainingErrors: [],
        metrics: this.createEmptyMetrics(),
        session,
      };
    }
  }

  /**
   * Apply multiple solutions to source code
   */
  private async applySolutions(sourceCode: string, solutions: CorrectionSolution[]): Promise<string> {
    let modifiedCode = sourceCode;

    // Sort solutions by line number to apply from bottom to top (avoid offset issues)
    const sortedSolutions = solutions.sort((a, b) => {
      const aLine = a.astTransformation.targetLocation?.line || 0;
      const bLine = b.astTransformation.targetLocation?.line || 0;
      return bLine - aLine;
    });

    for (const solution of sortedSolutions) {
      try {
        // Apply transformation
        const sourceContainer = {
          getSourceLines: () => modifiedCode.split('\n'),
          getSourceText: () => modifiedCode,
        };

        const ast = await this.stepParser.parse('python', sourceContainer as any);
        const transformResult = await this.transformationEngine.applyTransformations(
          modifiedCode,
          [solution.astTransformation],
        );

        // Generate code from transformed AST
        modifiedCode = transformResult.generatedCode;

      } catch (error) {
        console.warn(`Failed to apply solution ${solution.id}:`, error);
        // Continue with other solutions
      }
    }

    return modifiedCode;
  }

  /**
   * Calculate selection accuracy based on validation results
   */
  private calculateSelectionAccuracy(selectionResults: Map<string, SelectionResult>): number {
    if (selectionResults.size === 0) {
return 0;
}

    let successfulSelections = 0;
    for (const result of selectionResults.values()) {
      if (result.selectedSolution.validationResult.syntaxValid &&
          result.selectedSolution.validationResult.semanticsValid) {
        successfulSelections++;
      }
    }

    return successfulSelections / selectionResults.size;
  }

  /**
   * Create correction session
   */
  private createCorrectionSession(sourceCode: string): CorrectionSession {
    return {
      sessionId: `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      startTime: new Date(),
      sourceCode,
      errorHistory: [],
      solutionHistory: [],
      userFeedback: [],
    };
  }

  /**
   * Create empty metrics for error cases
   */
  private createEmptyMetrics(): EnhancedCorrectionMetrics {
    return {
      totalErrors: 0,
      errorsProcessed: 0,
      errorsResolved: 0,
      totalSolutionsGenerated: 0,
      averageSolutionsPerError: 0,
      selectionAccuracy: 0,
      totalCorrectionTime: 0,
      generationTime: 0,
      selectionTime: 0,
      validationTime: 0,
      iterationCount: 0,
      algorithmUsage: new Map(),
    };
  }

  /**
   * Update configuration
   */
  updateConfig(newConfig: Partial<EnhancedCorrectionConfig>): void {
    this.config = { ...this.config, ...newConfig };

    // Update component configurations
    if (newConfig.selectionCriteria) {
      this.selectionEngine.updateSelectionCriteria(newConfig.selectionCriteria);
    }

    if (newConfig.selectionContext) {
      this.selectionEngine.updateSelectionContext(newConfig.selectionContext);
    }
  }

  /**
   * Get correction statistics
   */
  getCorrectionStatistics(): any {
    return {
      multiSolutionStats: this.multiSolutionGenerator.getGenerationStatistics(),
      selectionStats: this.selectionEngine.getSelectionStatistics(),
      config: this.config,
    };
  }

  /**
   * Batch correct multiple files
   */
  async correctMultipleFiles(files: { path: string; content: string }[]): Promise<Map<string, EnhancedCorrectionResult>> {
    const results = new Map<string, EnhancedCorrectionResult>();

    for (const file of files) {
      try {
        const result = await this.correctErrors(file.content);
        results.set(file.path, result);
      } catch (error) {
        console.error(`Failed to correct file ${file.path}:`, error);
        results.set(file.path, {
          success: false,
          solutionResults: new Map(),
          selectionResults: new Map(),
          appliedSolutions: [],
          remainingErrors: [],
          metrics: this.createEmptyMetrics(),
          session: this.createCorrectionSession(file.content),
        });
      }
    }

    return results;
  }

  /**
   * Get detailed correction report
   */
  generateCorrectionReport(result: EnhancedCorrectionResult): string {
    const report = [];

    report.push('='.repeat(60));
    report.push('PROJECT GOLEM PHASE 2 - CORRECTION REPORT');
    report.push('='.repeat(60));

    report.push('\n📊 CORRECTION SUMMARY:');
    report.push(`  ✅ Success: ${result.success ? 'YES' : 'NO'}`);
    report.push(`  🔢 Total Errors: ${result.metrics.totalErrors}`);
    report.push(`  🔧 Errors Resolved: ${result.metrics.errorsResolved}`);
    report.push(`  ⏱️  Total Time: ${result.metrics.totalCorrectionTime}ms`);
    report.push(`  🔄 Iterations: ${result.metrics.iterationCount}`);

    report.push('\n🎯 MULTI-SOLUTION ANALYSIS:');
    report.push(`  📈 Solutions Generated: ${result.metrics.totalSolutionsGenerated}`);
    report.push(`  📊 Avg Solutions/Error: ${result.metrics.averageSolutionsPerError.toFixed(1)}`);
    report.push(`  🎯 Selection Accuracy: ${(result.metrics.selectionAccuracy * 100).toFixed(1)}%`);

    report.push('\n⏱️  PERFORMANCE BREAKDOWN:');
    report.push(`  🔍 Validation Time: ${result.metrics.validationTime}ms`);
    report.push(`  🏭 Generation Time: ${result.metrics.generationTime}ms`);
    report.push(`  🧠 Selection Time: ${result.metrics.selectionTime}ms`);

    if (result.appliedSolutions.length > 0) {
      report.push('\n🔧 APPLIED SOLUTIONS:');
      for (const solution of result.appliedSolutions) {
        report.push(`  - ${solution.type}: ${solution.description}`);
        report.push(`    Confidence: ${(solution.confidence * 100).toFixed(1)}%`);
      }
    }

    if (result.remainingErrors.length > 0) {
      report.push('\n❌ REMAINING ERRORS:');
      for (const error of result.remainingErrors) {
        report.push(`  - Line ${error.location.line}: ${error.message}`);
      }
    }

    report.push('\n' + '='.repeat(60));

    return report.join('\n');
  }
}

