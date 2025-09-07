/**
 * ProductionTriggerASTCorrector - Production Trigger-Based Error Correction
 *
 * Orchestrates the complete error correction pipeline using grammar production triggers.
 * Integrates SemanticValidator, GrammarDrivenASTMapper, and ASTTransformationEngine.
 */

import { ZeroCopyASTNode } from '../zerocopy/ast/ZeroCopyASTNode';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StructuredValidationError, ErrorType, ErrorSeverity } from './StructuredValidationError';
import { SemanticValidator, ProductionContext, SemanticValidationResult } from './SemanticValidator';
import { GrammarDrivenASTMapper, TransformationCandidate, ASTContext } from './GrammarDrivenASTMapper';
import { ASTTransformationEngine } from './ASTTransformationEngine';
import { CodeGenerationEngine } from './CodeGenerationEngine';

export interface CorrectionAttempt {
  id: string;
  originalError: StructuredValidationError;
  transformationCandidate: TransformationCandidate;
  correctedCode: string;
  correctedAST: ZeroCopyASTNode;
  validationResult: SemanticValidationResult;
  success: boolean;
  timestamp: Date;
}

export interface CorrectionSession {
  sessionId: string;
  originalCode: string;
  originalAST: ZeroCopyASTNode;
  attempts: CorrectionAttempt[];
  finalCode?: string;
  finalAST?: ZeroCopyASTNode;
  totalErrors: number;
  correctedErrors: number;
  success: boolean;
  duration: number;
  startTime: Date;
  endTime?: Date;
}

export interface CorrectionConfig {
  maxAttempts: number;
  maxIterations: number;
  confidenceThreshold: number;
  enableLearning: boolean;
  preserveFormatting: boolean;
  validateTransformations: boolean;
  timeoutMs: number;
}

export interface CorrectionResult {
  success: boolean;
  correctedCode?: string;
  correctedAST?: ZeroCopyASTNode;
  session: CorrectionSession;
  remainingErrors: StructuredValidationError[];
  appliedCorrections: CorrectionAttempt[];
  metrics: CorrectionMetrics;
}

export interface CorrectionMetrics {
  totalAttempts: number;
  successfulCorrections: number;
  failedCorrections: number;
  averageConfidence: number;
  correctionTime: number;
  errorTypes: Map<ErrorType, number>;
  transformationTypes: Map<string, number>;
}

/**
 * ProductionTriggerASTCorrector - Main correction orchestrator
 */
export class ProductionTriggerASTCorrector {
  private grammar: Grammar;
  private stepParser: StepParser;
  private semanticValidator: SemanticValidator;
  private astMapper: GrammarDrivenASTMapper;
  private transformationEngine: ASTTransformationEngine;
  private codeGenerator: CodeGenerationEngine;
  private config: CorrectionConfig;

  constructor(
    grammar: Grammar,
    stepParser: StepParser,
    config: Partial<CorrectionConfig> = {},
  ) {
    this.grammar = grammar;
    this.stepParser = stepParser;

    // Initialize components with production trigger system
    this.semanticValidator = new SemanticValidator(grammar, stepParser);
    this.astMapper = new GrammarDrivenASTMapper(null as any, null as any);
    this.transformationEngine = new ASTTransformationEngine();
    this.codeGenerator = new CodeGenerationEngine();

    this.config = {
      maxAttempts: 5,
      maxIterations: 3,
      confidenceThreshold: 0.7,
      enableLearning: true,
      preserveFormatting: true,
      validateTransformations: true,
      timeoutMs: 30000,
      ...config,
    };
  }

  /**
   * Correct errors in source code using production trigger-based approach
   */
  async correctErrors(
    sourceCode: string,
    ast?: ZeroCopyASTNode,
  ): Promise<CorrectionResult> {

    const session = this.createCorrectionSession(sourceCode, ast);

    try {
      // Parse source code if AST not provided
      if (!ast) {
        ast = await this.parseSourceCode(sourceCode);
      }
      session.originalAST = ast;

      // Detect errors using production triggers
      const validationResult = await this.semanticValidator.validateSemantics(ast, sourceCode);
      session.totalErrors = validationResult.errors.length;

      if (validationResult.errors.length === 0) {
        return this.createSuccessResult(session, sourceCode, ast);
      }

      // Iterative correction process
      let currentCode = sourceCode;
      let currentAST = ast;
      let remainingErrors = validationResult.errors;
      let iteration = 0;

      while (remainingErrors.length > 0 &&
             iteration < this.config.maxIterations &&
             session.attempts.length < this.config.maxAttempts) {

        iteration++;
        console.log(`🔄 Correction iteration ${iteration}, ${remainingErrors.length} errors remaining`);

        // Process errors in priority order
        const prioritizedErrors = this.prioritizeErrors(remainingErrors);
        let correctionMade = false;

        for (const error of prioritizedErrors) {
          if (session.attempts.length >= this.config.maxAttempts) {
            break;
          }

          const correctionAttempt = await this.attemptErrorCorrection(
            error,
            currentCode,
            currentAST,
            session,
          );

          if (correctionAttempt.success) {
            currentCode = correctionAttempt.correctedCode;
            currentAST = correctionAttempt.correctedAST;
            correctionMade = true;
            session.correctedErrors++;

            console.log(`✅ Successfully corrected: ${error.type} - ${error.message}`);
            break; // Process one error at a time for stability
          } else {
            console.log(`❌ Failed to correct: ${error.type} - ${error.message}`);
          }
        }

        if (!correctionMade) {
          console.log(`⚠️ No corrections made in iteration ${iteration}, stopping`);
          break;
        }

        // Re-validate after corrections
        const revalidationResult = await this.semanticValidator.validateSemantics(
          currentAST,
          currentCode,
        );
        remainingErrors = revalidationResult.errors;
      }

      // Finalize session
      session.finalCode = currentCode;
      session.finalAST = currentAST;
      session.success = remainingErrors.length === 0;
      session.endTime = new Date();
      session.duration = session.endTime.getTime() - session.startTime.getTime();

      return this.createCorrectionResult(session, remainingErrors);

    } catch (error) {
      console.error('Error during correction process:', error);
      session.endTime = new Date();
      session.duration = session.endTime.getTime() - session.startTime.getTime();

      return this.createFailureResult(session, error as Error);
    }
  }

  /**
   * Attempt to correct a single error
   */
  private async attemptErrorCorrection(
    error: StructuredValidationError,
    sourceCode: string,
    ast: ZeroCopyASTNode,
    session: CorrectionSession,
  ): Promise<CorrectionAttempt> {

    const attemptId = `attempt-${session.attempts.length + 1}-${Date.now()}`;

    try {
      // Create AST context for error mapping
      const context: ASTContext = {
        sourceCode,
        ast,
        errorNode: this.findErrorNode(error, ast),
        scopeStack: [], // Would be populated from semantic validator context
        typeEnvironment: {},
        controlFlowState: {},
        grammarProductions: [],
      };

      // Map error to transformation candidates
      const candidates = await this.astMapper.mapErrorToTransformation(error, context);

      if (candidates.length === 0) {
        return this.createFailedAttempt(attemptId, error, 'No transformation candidates found');
      }

      // Try the best candidate
      const bestCandidate = candidates[0];

      if (bestCandidate.confidence < this.config.confidenceThreshold) {
        return this.createFailedAttempt(
          attemptId,
          error,
          `Confidence too low: ${bestCandidate.confidence}`,
        );
      }

      // Apply transformation
      const astTransformation = {
        nodeType: bestCandidate.astTransformation.targetNode?.nodeType?.toString() || 'unknown',
        operation: bestCandidate.astTransformation.operation.toLowerCase() as 'insert' | 'delete' | 'modify' | 'replace',
        targetPath: ['root'], // Simplified path
        newValue: bestCandidate.astTransformation.newContent,
      };

      const transformResult = await this.transformationEngine.applyTransformations(
        sourceCode,
        [astTransformation],
      );

      // Generate corrected code
      const correctedCode = transformResult.generatedCode;

      // Validate the correction
      const validationResult = await this.semanticValidator.validateSemantics(
        ast,
        correctedCode,
      );

      const attempt: CorrectionAttempt = {
        id: attemptId,
        originalError: error,
        transformationCandidate: bestCandidate,
        correctedCode,
        correctedAST: transformResult.transformedAST,
        validationResult,
        success: validationResult.success && !this.hasErrorOfSameType(validationResult.errors, error),
        timestamp: new Date(),
      };

      session.attempts.push(attempt);
      return attempt;

    } catch (error) {
      return this.createFailedAttempt(attemptId, error as StructuredValidationError, (error as Error).message);
    }
  }

  /**
   * Parse source code using StepParser
   */
  private async parseSourceCode(sourceCode: string): Promise<ZeroCopyASTNode> {
    try {
      // StepParser returns ProductionMatch[], we need to convert to ZeroCopyASTNode
      const lines = sourceCode.split('\n');
      const sourceContainer = {
        getSourceLines: () => lines.map((line, index) => ({
          getLineNumber: () => index + 1,
          getContent: () => line,
          getLength: () => line.length,
          getFileName: () => 'source.py',
        })),
        getCount: () => lines.length,
        getLine: (fileName: string, lineNumber: number) => ({
          getLineNumber: () => lineNumber,
          getContent: () => lines[lineNumber - 1] || '',
          getLength: () => (lines[lineNumber - 1] || '').length,
          getFileName: () => fileName,
        }),
      };
      const productionMatches = await this.stepParser.parse('Python311', sourceContainer);

      // Create a simple ZeroCopyASTNode from ProductionMatch[]
      // This is a simplified conversion - in a full implementation,
      // you'd properly convert the production matches to an AST
      return {
        nodeType: 'Module',
        children: [],
        toString: () => sourceCode,
      } as any; // Simplified for compilation
    } catch (error) {
      throw new Error(`Failed to parse source code: ${error}`);
    }
  }

  /**
   * Prioritize errors for correction order
   */
  private prioritizeErrors(errors: StructuredValidationError[]): StructuredValidationError[] {
    return errors.sort((a, b) => {
      // Priority order: SYNTAX_ERROR > NAME_ERROR > IMPORT_ERROR > others
      const priorityOrder = {
        [ErrorType.SYNTAX_ERROR]: 4,
        [ErrorType.NAME_ERROR]: 3,
        [ErrorType.IMPORT_ERROR]: 2,
        [ErrorType.TYPE_ERROR]: 1,
        [ErrorType.INDENTATION_ERROR]: 1,
      };

      const aPriority = priorityOrder[a.type] || 0;
      const bPriority = priorityOrder[b.type] || 0;

      if (aPriority !== bPriority) {
        return bPriority - aPriority;
      }

      // Secondary sort by severity
      const severityOrder = {
        [ErrorSeverity.HIGH]: 3,
        [ErrorSeverity.MEDIUM]: 2,
        [ErrorSeverity.LOW]: 1,
      };

      return severityOrder[b.severity] - severityOrder[a.severity];
    });
  }

  /**
   * Find the AST node corresponding to an error
   */
  private findErrorNode(error: StructuredValidationError, ast: ZeroCopyASTNode): ZeroCopyASTNode {
    // Navigate AST to find node at error location
    return this.findNodeAtPosition(ast, error.location.line, error.location.column) || ast;
  }

  /**
   * Find AST node at specific position
   */
  private findNodeAtPosition(
    node: ZeroCopyASTNode,
    line: number,
    column: number,
  ): ZeroCopyASTNode | null {

    // ZeroCopyASTNode doesn't have line/column properties directly
    // This is a simplified check - in a full implementation, you'd use proper position tracking
    const nodePosition = (node as any).position || { line: 0, column: 0 };
    if (nodePosition.line === line && nodePosition.column === column) {
      return node;
    }

    // Search children
    for (const child of node.getChildren() || []) {
      const found = this.findNodeAtPosition(child, line, column);
      if (found) {
        return found;
      }
    }

    return null;
  }

  /**
   * Check if validation result contains error of same type
   */
  private hasErrorOfSameType(
    errors: StructuredValidationError[],
    originalError: StructuredValidationError,
  ): boolean {

    return errors.some(error =>
      error.type === originalError.type &&
      error.location.line === originalError.location.line,
    );
  }

  /**
   * Create correction session
   */
  private createCorrectionSession(sourceCode: string, ast?: ZeroCopyASTNode): CorrectionSession {
    return {
      sessionId: `session-${Date.now()}`,
      originalCode: sourceCode,
      originalAST: ast!,
      attempts: [],
      totalErrors: 0,
      correctedErrors: 0,
      success: false,
      duration: 0,
      startTime: new Date(),
    };
  }

  /**
   * Create failed correction attempt
   */
  private createFailedAttempt(
    attemptId: string,
    error: StructuredValidationError,
    reason: string,
  ): CorrectionAttempt {

    return {
      id: attemptId,
      originalError: error,
      transformationCandidate: {} as TransformationCandidate,
      correctedCode: '',
      correctedAST: {} as ZeroCopyASTNode,
      validationResult: {
        errors: [],
        warnings: [],
        success: false,
        symbolTable: new Map(),
        scopeAnalysis: {
          totalScopes: 0,
          undefinedVariables: [],
          unusedVariables: [],
          shadowedVariables: [],
          scopeTree: {} as any, // Simplified for compilation
        },
      },
      success: false,
      timestamp: new Date(),
    };
  }

  /**
   * Create success result
   */
  private createSuccessResult(
    session: CorrectionSession,
    code: string,
    ast: ZeroCopyASTNode,
  ): CorrectionResult {

    session.success = true;
    session.finalCode = code;
    session.finalAST = ast;
    session.endTime = new Date();
    session.duration = session.endTime.getTime() - session.startTime.getTime();

    return {
      success: true,
      correctedCode: code,
      correctedAST: ast,
      session,
      remainingErrors: [],
      appliedCorrections: [],
      metrics: this.calculateMetrics(session),
    };
  }

  /**
   * Create correction result
   */
  private createCorrectionResult(
    session: CorrectionSession,
    remainingErrors: StructuredValidationError[],
  ): CorrectionResult {

    const appliedCorrections = session.attempts.filter(attempt => attempt.success);

    return {
      success: session.success,
      correctedCode: session.finalCode,
      correctedAST: session.finalAST,
      session,
      remainingErrors,
      appliedCorrections,
      metrics: this.calculateMetrics(session),
    };
  }

  /**
   * Create failure result
   */
  private createFailureResult(session: CorrectionSession, error: Error): CorrectionResult {
    return {
      success: false,
      session,
      remainingErrors: [],
      appliedCorrections: [],
      metrics: this.calculateMetrics(session),
    };
  }

  /**
   * Calculate correction metrics
   */
  private calculateMetrics(session: CorrectionSession): CorrectionMetrics {
    const successfulCorrections = session.attempts.filter(attempt => attempt.success);
    const failedCorrections = session.attempts.filter(attempt => !attempt.success);

    const errorTypes = new Map<ErrorType, number>();
    const transformationTypes = new Map<string, number>();

    for (const attempt of session.attempts) {
      const errorType = attempt.originalError.type;
      errorTypes.set(errorType, (errorTypes.get(errorType) || 0) + 1);

      if (attempt.success) {
        const transformationType = attempt.transformationCandidate.type;
        transformationTypes.set(transformationType, (transformationTypes.get(transformationType) || 0) + 1);
      }
    }

    const averageConfidence = session.attempts.length > 0
      ? session.attempts.reduce((sum, attempt) =>
          sum + (attempt.transformationCandidate.confidence || 0), 0) / session.attempts.length
      : 0;

    return {
      totalAttempts: session.attempts.length,
      successfulCorrections: successfulCorrections.length,
      failedCorrections: failedCorrections.length,
      averageConfidence,
      correctionTime: session.duration,
      errorTypes,
      transformationTypes,
    };
  }
}

