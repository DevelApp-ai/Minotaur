/**
 * ASTGuard - Runtime guards for AST manipulation safety
 *
 * Provides runtime protection against invalid AST manipulations by intercepting
 * and validating all modification operations before they are applied to the tree.
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { SyntacticValidator, ASTManipulation, ManipulationType, ValidationResult } from './SyntacticValidator';
import { Grammar } from '../core/grammar/Grammar';
import { EventEmitter } from 'events';

export interface GuardConfiguration {
  strictMode: boolean;
  allowWarnings: boolean;
  autoCorrect: boolean;
  logViolations: boolean;
  throwOnError: boolean;
}

export interface GuardEvent {
  type: 'validation_failed' | 'manipulation_blocked' | 'auto_correction' | 'warning_issued';
  manipulation: ASTManipulation;
  result: ValidationResult;
  timestamp: number;
}

export class ASTGuard extends EventEmitter {
  private validator: SyntacticValidator;
  private config: GuardConfiguration;
  private violationLog: GuardEvent[];
  private isEnabled: boolean;

  constructor(grammar: Grammar, config: Partial<GuardConfiguration> = {}) {
    super();

    this.validator = new SyntacticValidator(grammar);
    this.config = {
      strictMode: false,
      allowWarnings: true,
      autoCorrect: false,
      logViolations: true,
      throwOnError: false,
      ...config,
    };

    this.violationLog = [];
    this.isEnabled = true;
  }

  /**
   * Guards an AST manipulation operation
   */
  public guardManipulation(manipulation: ASTManipulation): boolean {
    if (!this.isEnabled) {
      return true; // Pass through if guard is disabled
    }

    const validationResult = this.validator.validateManipulation(manipulation);

    // Log the event
    const event: GuardEvent = {
      type: validationResult.isValid ? 'manipulation_blocked' : 'validation_failed',
      manipulation,
      result: validationResult,
      timestamp: Date.now(),
    };

    if (this.config.logViolations) {
      this.violationLog.push(event);
    }

    // Handle validation result
    if (!validationResult.isValid) {
      return this.handleValidationFailure(manipulation, validationResult);
    }

    // Handle warnings in strict mode
    if (this.config.strictMode && validationResult.warnings.length > 0) {
      return this.handleWarnings(manipulation, validationResult);
    }

    // Emit success event
    this.emit('manipulation_allowed', event);
    return true;
  }

  /**
   * Creates a guarded version of an AST node that validates all manipulations
   */
  public createGuardedNode(node: ZeroCopyASTNode): GuardedASTNode {
    return new GuardedASTNode(node, this);
  }

  /**
   * Batch validation for multiple manipulations
   */
  public guardBatchManipulations(manipulations: ASTManipulation[]): BatchValidationResult {
    const results: ValidationResult[] = [];
    const allowedManipulations: ASTManipulation[] = [];
    const blockedManipulations: ASTManipulation[] = [];

    for (const manipulation of manipulations) {
      const result = this.validator.validateManipulation(manipulation);
      results.push(result);

      if (result.isValid || (this.config.allowWarnings && result.errors.length === 0)) {
        allowedManipulations.push(manipulation);
      } else {
        blockedManipulations.push(manipulation);
      }
    }

    return {
      overallValid: blockedManipulations.length === 0,
      results,
      allowedManipulations,
      blockedManipulations,
      totalCount: manipulations.length,
      allowedCount: allowedManipulations.length,
      blockedCount: blockedManipulations.length,
    };
  }

  /**
   * Enables or disables the guard
   */
  public setEnabled(enabled: boolean): void {
    this.isEnabled = enabled;
    this.emit('guard_toggled', { enabled, timestamp: Date.now() });
  }

  /**
   * Updates guard configuration
   */
  public updateConfiguration(newConfig: Partial<GuardConfiguration>): void {
    const oldConfig = { ...this.config };
    this.config = { ...this.config, ...newConfig };

    this.emit('configuration_updated', {
      oldConfig,
      newConfig: this.config,
      timestamp: Date.now(),
    });
  }

  /**
   * Gets violation statistics
   */
  public getViolationStats(): ViolationStats {
    const totalViolations = this.violationLog.length;
    const errorCount = this.violationLog.filter(e => e.result.errors.length > 0).length;
    const warningCount = this.violationLog.filter(e => e.result.warnings.length > 0).length;

    const violationsByType = this.violationLog.reduce((acc, event) => {
      const type = event.manipulation.type;
      acc[type] = (acc[type] || 0) + 1;
      return acc;
    }, {} as Record<ManipulationType, number>);

    return {
      totalViolations,
      errorCount,
      warningCount,
      violationsByType,
      recentViolations: this.violationLog.slice(-10),
    };
  }

  /**
   * Clears violation log
   */
  public clearViolationLog(): void {
    this.violationLog = [];
    this.emit('log_cleared', { timestamp: Date.now() });
  }

  /**
   * Handles validation failures
   */
  private handleValidationFailure(manipulation: ASTManipulation, result: ValidationResult): boolean {
    const event: GuardEvent = {
      type: 'validation_failed',
      manipulation,
      result,
      timestamp: Date.now(),
    };

    this.emit('validation_failed', event);

    // Try auto-correction if enabled
    if (this.config.autoCorrect) {
      const correctedManipulation = this.attemptAutoCorrection(manipulation, result);
      if (correctedManipulation) {
        const correctionResult = this.validator.validateManipulation(correctedManipulation);
        if (correctionResult.isValid) {
          this.emit('auto_correction', {
            type: 'auto_correction',
            manipulation: correctedManipulation,
            result: correctionResult,
            timestamp: Date.now(),
          });
          return true;
        }
      }
    }

    // Throw error if configured to do so
    if (this.config.throwOnError) {
      throw new ASTValidationError(
        `AST manipulation blocked: ${result.errors.map(e => e.message).join(', ')}`,
        manipulation,
        result,
      );
    }

    return false;
  }

  /**
   * Handles warnings in strict mode
   */
  private handleWarnings(manipulation: ASTManipulation, result: ValidationResult): boolean {
    const event: GuardEvent = {
      type: 'warning_issued',
      manipulation,
      result,
      timestamp: Date.now(),
    };

    this.emit('warning_issued', event);

    if (this.config.strictMode) {
      return false; // Block manipulation in strict mode
    }

    return this.config.allowWarnings;
  }

  /**
   * Attempts to auto-correct invalid manipulations
   */
  private attemptAutoCorrection(manipulation: ASTManipulation, result: ValidationResult): ASTManipulation | null {
    // Simple auto-correction strategies
    for (const error of result.errors) {
      switch (error.code) {
        case 'TYPE_MISMATCH':
          return this.correctTypeMismatch(manipulation);

        case 'INVALID_CHILD_TYPE':
          return this.correctInvalidChildType(manipulation);

        case 'UNDEFINED_IDENTIFIER':
          return this.correctUndefinedIdentifier(manipulation);

        default:
          continue;
      }
    }

    return null;
  }

  private correctTypeMismatch(manipulation: ASTManipulation): ASTManipulation | null {
    // Attempt to insert type conversion
    if (manipulation.newNode) {
      // Create a type conversion wrapper
      const conversionNode = this.createTypeConversionNode(manipulation.newNode);
      if (conversionNode) {
        return {
          ...manipulation,
          newNode: conversionNode,
        };
      }
    }
    return null;
  }

  private correctInvalidChildType(manipulation: ASTManipulation): ASTManipulation | null {
    // Attempt to wrap the child in a valid container
    if (manipulation.newNode) {
      const wrapperNode = this.createWrapperNode(manipulation.newNode, manipulation.targetNode);
      if (wrapperNode) {
        return {
          ...manipulation,
          newNode: wrapperNode,
        };
      }
    }
    return null;
  }

  private correctUndefinedIdentifier(manipulation: ASTManipulation): ASTManipulation | null {
    // Attempt to create a declaration for the undefined identifier
    // This is a simplified example - real implementation would be more sophisticated
    return null;
  }

  private createTypeConversionNode(node: ZeroCopyASTNode): ZeroCopyASTNode | null {
    // Create a type conversion node wrapper
    // Implementation would depend on the specific AST structure
    return null;
  }

  private createWrapperNode(child: ZeroCopyASTNode, parent: ZeroCopyASTNode): ZeroCopyASTNode | null {
    // Create an appropriate wrapper node
    // Implementation would depend on the specific AST structure
    return null;
  }
}

/**
 * Guarded AST Node that validates all manipulations
 */
export class GuardedASTNode {
  private node: ZeroCopyASTNode;
  private guard: ASTGuard;

  constructor(node: ZeroCopyASTNode, guard: ASTGuard) {
    this.node = node;
    this.guard = guard;
  }

  /**
   * Safely adds a child node with validation
   */
  public addChild(child: ZeroCopyASTNode, position?: number): boolean {
    const manipulation: ASTManipulation = {
      type: ManipulationType.INSERT_CHILD,
      targetNode: this.node,
      newNode: child,
      position,
    };

    if (this.guard.guardManipulation(manipulation)) {
      // Use appendChild since addChild doesn't exist
      this.node.appendChild(child);
      return true;
    }

    return false;
  }

  /**
   * Safely removes a child node with validation
   */
  public removeChild(index: number): boolean {
    const manipulation: ASTManipulation = {
      type: ManipulationType.REMOVE_CHILD,
      targetNode: this.node,
      position: index,
    };

    if (this.guard.guardManipulation(manipulation)) {
      // removeChild method doesn't exist - not supported yet
      return false;
    }

    return false;
  }

  /**
   * Safely replaces this node with another
   */
  public replaceWith(newNode: ZeroCopyASTNode): boolean {
    const manipulation: ASTManipulation = {
      type: ManipulationType.REPLACE_NODE,
      targetNode: this.node,
      newNode,
    };

    if (this.guard.guardManipulation(manipulation)) {
      // replaceWith method doesn't exist - not supported yet
      return false;
    }

    return false;
  }

  /**
   * Safely modifies a node value
   */
  public setValue(value: any): boolean {
    const manipulation: ASTManipulation = {
      type: ManipulationType.MODIFY_VALUE,
      targetNode: this.node,
      newValue: value,
    };

    if (this.guard.guardManipulation(manipulation)) {
      // Use the value property setter
      this.node.value = value;
      return true;
    }

    return false;
  }

  /**
   * Gets the underlying node (read-only access)
   */
  public getNode(): ZeroCopyASTNode {
    return this.node;
  }

  /**
   * Creates guarded children
   */
  public getGuardedChildren(): GuardedASTNode[] {
    return this.node.getChildren().map(child =>
      new GuardedASTNode(child, this.guard),
    );
  }
}

/**
 * Custom error for AST validation failures
 */
export class ASTValidationError extends Error {
  public manipulation: ASTManipulation;
  public validationResult: ValidationResult;

  constructor(message: string, manipulation: ASTManipulation, result: ValidationResult) {
    super(message);
    this.name = 'ASTValidationError';
    this.manipulation = manipulation;
    this.validationResult = result;
  }
}

/**
 * Result of batch validation
 */
export interface BatchValidationResult {
  overallValid: boolean;
  results: ValidationResult[];
  allowedManipulations: ASTManipulation[];
  blockedManipulations: ASTManipulation[];
  totalCount: number;
  allowedCount: number;
  blockedCount: number;
}

/**
 * Violation statistics
 */
export interface ViolationStats {
  totalViolations: number;
  errorCount: number;
  warningCount: number;
  violationsByType: Record<ManipulationType, number>;
  recentViolations: GuardEvent[];
}

/**
 * Factory for creating configured guards
 */
export class ASTGuardFactory {
  /**
   * Creates a strict guard that blocks all invalid manipulations
   */
  public static createStrictGuard(grammar: Grammar): ASTGuard {
    return new ASTGuard(grammar, {
      strictMode: true,
      allowWarnings: false,
      autoCorrect: false,
      logViolations: true,
      throwOnError: true,
    });
  }

  /**
   * Creates a permissive guard that allows warnings
   */
  public static createPermissiveGuard(grammar: Grammar): ASTGuard {
    return new ASTGuard(grammar, {
      strictMode: false,
      allowWarnings: true,
      autoCorrect: true,
      logViolations: true,
      throwOnError: false,
    });
  }

  /**
   * Creates a development guard with auto-correction
   */
  public static createDevelopmentGuard(grammar: Grammar): ASTGuard {
    return new ASTGuard(grammar, {
      strictMode: false,
      allowWarnings: true,
      autoCorrect: true,
      logViolations: true,
      throwOnError: false,
    });
  }

  /**
   * Creates a production guard with strict validation
   */
  public static createProductionGuard(grammar: Grammar): ASTGuard {
    return new ASTGuard(grammar, {
      strictMode: true,
      allowWarnings: false,
      autoCorrect: false,
      logViolations: true,
      throwOnError: true,
    });
  }
}

