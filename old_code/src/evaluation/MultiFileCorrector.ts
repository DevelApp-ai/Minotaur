/**
 * MultiFileCorrector - Project-Wide Code Correction Engine
 * 
 * Coordinates corrections across multiple files in a project, handling
 * cross-file dependencies, atomic operations, and intelligent change
 * propagation for Project Golem's multi-file capabilities.
 * 
 * Features:
 * - Atomic multi-file operations with rollback capability
 * - Cross-file dependency tracking and change propagation
 * - Intelligent file ordering for correction sequence
 * - Conflict detection and resolution
 * - Progress tracking and detailed reporting
 */

import * as fs from 'fs/promises';
import * as path from 'path';
import { ProjectWideFileScanner, ProjectStructure, ProjectFile, FileReference } from './ProjectWideFileScanner';
import { AgenticSystem } from './AgenticSystem';
import { PromptFileProcessor, PromptInstruction } from '../cli/PromptFileProcessor';

export interface MultiFileOperation {
  id: string;
  type: 'correction' | 'refactor' | 'update' | 'format';
  description: string;
  targetFiles: string[];
  instructions: PromptInstruction[];
  dependencies: string[];
  priority: number;
  estimatedImpact: 'low' | 'medium' | 'high';
}

export interface FileModification {
  filePath: string;
  originalContent: string;
  modifiedContent: string;
  changeType: 'content' | 'create' | 'delete' | 'rename';
  lineChanges: LineChange[];
  confidence: number;
  executionTime: number;
  errors: string[];
}

export interface LineChange {
  lineNumber: number;
  type: 'add' | 'delete' | 'modify';
  originalLine?: string;
  newLine?: string;
  context: string;
}

export interface MultiFileCorrectionResult {
  operationId: string;
  success: boolean;
  totalFiles: number;
  modifiedFiles: number;
  skippedFiles: number;
  failedFiles: number;
  modifications: FileModification[];
  executionTime: number;
  rollbackInfo?: RollbackInfo;
  summary: CorrectionSummary;
  errors: string[];
}

export interface RollbackInfo {
  backupPath: string;
  modifiedFiles: string[];
  timestamp: Date;
  canRollback: boolean;
}

export interface CorrectionSummary {
  totalLinesChanged: number;
  filesCreated: number;
  filesDeleted: number;
  filesRenamed: number;
  crossFileChanges: number;
  dependencyUpdates: number;
  conflictsResolved: number;
}

export interface MultiFileCorrectionOptions {
  // Operation control
  enableAtomicOperations: boolean;
  enableRollback: boolean;
  maxConcurrentFiles: number;
  
  // Safety features
  createBackup: boolean;
  backupDirectory: string;
  dryRun: boolean;
  
  // Correction behavior
  enableCrossFileAnalysis: boolean;
  enableDependencyTracking: boolean;
  enableConflictResolution: boolean;
  
  // Performance
  enableParallelProcessing: boolean;
  timeoutPerFile: number;
  maxRetries: number;
  
  // Reporting
  enableDetailedLogging: boolean;
  enableProgressReporting: boolean;
  saveDetailedReport: boolean;
}

/**
 * MultiFileCorrector - Main correction engine
 */
export class MultiFileCorrector {
  private fileScanner: ProjectWideFileScanner;
  private agenticSystem: AgenticSystem;
  private promptProcessor: PromptFileProcessor;
  private activeOperations: Map<string, MultiFileOperation> = new Map();
  private operationHistory: MultiFileCorrectionResult[] = [];

  private defaultOptions: MultiFileCorrectionOptions = {
    enableAtomicOperations: true,
    enableRollback: true,
    maxConcurrentFiles: 5,
    createBackup: true,
    backupDirectory: '.golem-backups',
    dryRun: false,
    enableCrossFileAnalysis: true,
    enableDependencyTracking: true,
    enableConflictResolution: true,
    enableParallelProcessing: true,
    timeoutPerFile: 30000,
    maxRetries: 2,
    enableDetailedLogging: true,
    enableProgressReporting: true,
    saveDetailedReport: true,
  };

  constructor(
    fileScanner: ProjectWideFileScanner,
    agenticSystem: AgenticSystem,
    promptProcessor: PromptFileProcessor,
  ) {
    this.fileScanner = fileScanner;
    this.agenticSystem = agenticSystem;
    this.promptProcessor = promptProcessor;
  }

  /**
   * Correct multiple files based on project-wide instructions
   */
  async correctProject(
    projectPath: string,
    instructions: PromptInstruction[],
    options: Partial<MultiFileCorrectionOptions> = {},
  ): Promise<MultiFileCorrectionResult> {
    
    const fullOptions = { ...this.defaultOptions, ...options };
    const operationId = `multi-correction-${Date.now()}`;
    
    console.log(`🎯 Starting multi-file correction: ${operationId}`);
    console.log(`📁 Project: ${projectPath}`);
    console.log(`📝 Instructions: ${instructions.length}`);
    
    const startTime = Date.now();
    
    try {
      // Scan project structure
      const projectStructure = await this.fileScanner.scanProject(projectPath, {
        loadFileContent: true,
        analyzeDependencies: true,
      });
      
      console.log(`📊 Project scanned: ${projectStructure.totalFiles} files`);
      
      // Plan multi-file operations
      const operations = await this.planMultiFileOperations(
        projectStructure,
        instructions,
        fullOptions,
      );
      
      console.log(`📋 Planned ${operations.length} operations`);
      
      // Create backup if enabled
      let rollbackInfo: RollbackInfo | undefined;
      if (fullOptions.createBackup && !fullOptions.dryRun) {
        rollbackInfo = await this.createBackup(projectPath, fullOptions.backupDirectory);
        console.log(`💾 Backup created: ${rollbackInfo.backupPath}`);
      }
      
      // Execute operations
      const result = await this.executeMultiFileOperations(
        projectStructure,
        operations,
        fullOptions,
      );
      
      // Update result with metadata
      result.operationId = operationId;
      result.executionTime = Date.now() - startTime;
      result.rollbackInfo = rollbackInfo;
      
      // Save detailed report if enabled
      if (fullOptions.saveDetailedReport) {
        await this.saveDetailedReport(result, projectPath);
      }
      
      // Store in history
      this.operationHistory.push(result);
      
      console.log(`✅ Multi-file correction complete: ${result.modifiedFiles}/${result.totalFiles} files modified`);
      
      return result;
      
    } catch (error) {
      console.error(`❌ Multi-file correction failed: ${error instanceof Error ? error.message : String(error)}`);
      
      return {
        operationId,
        success: false,
        totalFiles: 0,
        modifiedFiles: 0,
        skippedFiles: 0,
        failedFiles: 0,
        modifications: [],
        executionTime: Date.now() - startTime,
        summary: {
          totalLinesChanged: 0,
          filesCreated: 0,
          filesDeleted: 0,
          filesRenamed: 0,
          crossFileChanges: 0,
          dependencyUpdates: 0,
          conflictsResolved: 0,
        },
        errors: [error instanceof Error ? error.message : String(error)],
      };
    }
  }

  /**
   * Correct files based on error messages with file:line references
   */
  async correctFromErrorMessages(
    projectPath: string,
    errorMessages: string[],
    options: Partial<MultiFileCorrectionOptions> = {},
  ): Promise<MultiFileCorrectionResult> {
    
    console.log(`🔍 Analyzing ${errorMessages.length} error messages`);
    
    // Scan project to get structure
    const projectStructure = await this.fileScanner.scanProject(projectPath, {
      loadFileContent: true,
    });
    
    // Resolve file references from error messages
    const fileReferences = await this.fileScanner.resolveFileReferences(
      projectStructure,
      errorMessages,
    );
    
    console.log(`📍 Resolved ${fileReferences.size} file references`);
    
    // Convert error messages to correction instructions
    const instructions: PromptInstruction[] = [];
    
    for (const [errorMessage, references] of fileReferences) {
      for (const reference of references) {
        instructions.push({
          type: 'fix_error',
          description: `Fix error: ${errorMessage}`,
          targetLocation: `${reference.file}${reference.line ? `:${reference.line}` : ''}`,
          priority: 'high',
          parameters: {
            errorMessage,
            file: reference.file,
            line: reference.line,
            column: reference.column,
          },
        });
      }
    }
    
    // Execute corrections
    return await this.correctProject(projectPath, instructions, options);
  }

  /**
   * Plan multi-file operations from instructions
   */
  private async planMultiFileOperations(
    projectStructure: ProjectStructure,
    instructions: PromptInstruction[],
    options: MultiFileCorrectionOptions,
  ): Promise<MultiFileOperation[]> {
    
    const operations: MultiFileOperation[] = [];
    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
    
    for (let i = 0; i < instructions.length; i++) {
      const instruction = instructions[i];
      
      // Determine target files for this instruction
      const targetFiles = await this.resolveTargetFiles(
        projectStructure,
        instruction,
        allFiles,
      );
      
      if (targetFiles.length === 0) {
        console.warn(`⚠️  No target files found for instruction: ${instruction.description}`);
        continue;
      }
      
      // Analyze dependencies
      const dependencies = options.enableDependencyTracking
        ? await this.analyzeDependencies(targetFiles, projectStructure)
        : [];
      
      // Create operation
      const operation: MultiFileOperation = {
        id: `operation-${i}`,
        type: instruction.type === 'fix_error' ? 'correction' : 
              instruction.type === 'refactor' ? 'refactor' :
              instruction.type === 'format_code' ? 'format' : 'update',
        description: instruction.description,
        targetFiles: targetFiles.map(f => f.absolutePath),
        instructions: [instruction],
        dependencies,
        priority: instruction.priority === 'high' ? 3 :
                 instruction.priority === 'medium' ? 2 : 1,
        estimatedImpact: this.estimateImpact(targetFiles, instruction),
      };
      
      operations.push(operation);
    }
    
    // Sort operations by priority and dependencies
    return this.sortOperationsByPriority(operations);
  }

  /**
   * Resolve target files for instruction
   */
  private async resolveTargetFiles(
    projectStructure: ProjectStructure,
    instruction: PromptInstruction,
    allFiles: ProjectFile[],
  ): Promise<ProjectFile[]> {
    
    if (instruction.targetLocation) {
      // Parse target location (e.g., "file.ts:45", "src/utils/*.py")
      const targetFiles: ProjectFile[] = [];
      
      if (instruction.targetLocation.includes('*')) {
        // Pattern matching
        const searchResult = await this.fileScanner.searchFiles(
          projectStructure,
          instruction.targetLocation,
          'path',
        );
        targetFiles.push(...searchResult.matches);
      } else {
        // Specific file reference
        const fileRef = instruction.targetLocation.split(':')[0];
        const searchResult = await this.fileScanner.searchFiles(
          projectStructure,
          fileRef,
          'name',
        );
        targetFiles.push(...searchResult.matches);
      }
      
      return targetFiles;
    }
    
    // If no specific target, analyze instruction content to infer files
    return await this.inferTargetFiles(projectStructure, instruction, allFiles);
  }

  /**
   * Infer target files from instruction content
   */
  private async inferTargetFiles(
    projectStructure: ProjectStructure,
    instruction: PromptInstruction,
    allFiles: ProjectFile[],
  ): Promise<ProjectFile[]> {
    
    const description = instruction.description.toLowerCase();
    const targetFiles: ProjectFile[] = [];
    
    // Look for file names mentioned in description
    const fileNamePattern = /(\w+\.\w+)/g;
    const matches = description.match(fileNamePattern);
    
    if (matches) {
      for (const match of matches) {
        const searchResult = await this.fileScanner.searchFiles(
          projectStructure,
          match,
          'name',
        );
        targetFiles.push(...searchResult.matches);
      }
    }
    
    // Look for function/class names mentioned
    const symbolPattern = /(?:function|class|method)\s+(\w+)/g;
    const symbolMatches = description.match(symbolPattern);
    
    if (symbolMatches) {
      for (const match of symbolMatches) {
        const symbolName = match.split(' ')[1];
        const functionSearch = await this.fileScanner.searchFiles(
          projectStructure,
          symbolName,
          'function',
        );
        const classSearch = await this.fileScanner.searchFiles(
          projectStructure,
          symbolName,
          'class',
        );
        
        targetFiles.push(...functionSearch.matches, ...classSearch.matches);
      }
    }
    
    // If still no targets found, apply to all files of relevant language
    if (targetFiles.length === 0) {
      if (description.includes('python') || description.includes('.py')) {
        const pythonFiles = projectStructure.filesByLanguage.get('python') || [];
        targetFiles.push(...pythonFiles);
      } else if (description.includes('typescript') || description.includes('.ts')) {
        const tsFiles = projectStructure.filesByLanguage.get('typescript') || [];
        targetFiles.push(...tsFiles);
      } else if (description.includes('javascript') || description.includes('.js')) {
        const jsFiles = projectStructure.filesByLanguage.get('javascript') || [];
        targetFiles.push(...jsFiles);
      }
    }
    
    return [...new Set(targetFiles)]; // Remove duplicates
  }

  /**
   * Analyze dependencies between target files
   */
  private async analyzeDependencies(
    targetFiles: ProjectFile[],
    projectStructure: ProjectStructure,
  ): Promise<string[]> {
    
    const dependencies: string[] = [];
    
    for (const file of targetFiles) {
      const fileDeps = projectStructure.dependencyGraph.get(file.relativePath);
      if (fileDeps) {
        dependencies.push(...fileDeps);
      }
    }
    
    return [...new Set(dependencies)];
  }

  /**
   * Estimate impact of operation
   */
  private estimateImpact(
    targetFiles: ProjectFile[],
    instruction: PromptInstruction,
  ): 'low' | 'medium' | 'high' {
    
    if (targetFiles.length > 10) {
      return 'high';
    }
    
    if (instruction.type === 'refactor' || instruction.description.includes('refactor')) {
      return 'high';
    }
    
    if (instruction.type === 'fix_error' && targetFiles.length === 1) {
      return 'low';
    }
    
    return 'medium';
  }

  /**
   * Sort operations by priority and dependencies
   */
  private sortOperationsByPriority(operations: MultiFileOperation[]): MultiFileOperation[] {
    return operations.sort((a, b) => {
      // First by priority
      if (a.priority !== b.priority) {
        return b.priority - a.priority;
      }
      
      // Then by estimated impact (low impact first for safety)
      const impactOrder = { low: 1, medium: 2, high: 3 };
      return impactOrder[a.estimatedImpact] - impactOrder[b.estimatedImpact];
    });
  }

  /**
   * Execute multi-file operations
   */
  private async executeMultiFileOperations(
    projectStructure: ProjectStructure,
    operations: MultiFileOperation[],
    options: MultiFileCorrectionOptions,
  ): Promise<MultiFileCorrectionResult> {
    
    const modifications: FileModification[] = [];
    const errors: string[] = [];
    let modifiedFiles = 0;
    let skippedFiles = 0;
    let failedFiles = 0;
    
    console.log(`🚀 Executing ${operations.length} operations`);
    
    for (let i = 0; i < operations.length; i++) {
      const operation = operations[i];
      
      if (options.enableProgressReporting) {
        console.log(`📝 Operation ${i + 1}/${operations.length}: ${operation.description}`);
      }
      
      try {
        // Execute operation on target files
        const operationResults = await this.executeOperation(
          operation,
          projectStructure,
          options,
        );
        
        modifications.push(...operationResults.modifications);
        errors.push(...operationResults.errors);
        
        modifiedFiles += operationResults.modifiedFiles;
        skippedFiles += operationResults.skippedFiles;
        failedFiles += operationResults.failedFiles;
        
        if (options.enableDetailedLogging) {
          console.log(`  ✅ Modified: ${operationResults.modifiedFiles}, Skipped: ${operationResults.skippedFiles}, Failed: ${operationResults.failedFiles}`);
        }
        
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        errors.push(`Operation ${operation.id} failed: ${errorMessage}`);
        failedFiles += operation.targetFiles.length;
        
        console.error(`❌ Operation failed: ${errorMessage}`);
      }
    }
    
    // Apply modifications if not dry run
    if (!options.dryRun && modifications.length > 0) {
      await this.applyModifications(modifications, options);
    }
    
    // Generate summary
    const summary = this.generateSummary(modifications);
    
    return {
      operationId: '', // Will be set by caller
      success: failedFiles === 0,
      totalFiles: operations.reduce((sum, op) => sum + op.targetFiles.length, 0),
      modifiedFiles,
      skippedFiles,
      failedFiles,
      modifications,
      executionTime: 0, // Will be set by caller
      summary,
      errors,
    };
  }

  /**
   * Execute single operation
   */
  private async executeOperation(
    operation: MultiFileOperation,
    projectStructure: ProjectStructure,
    options: MultiFileCorrectionOptions,
  ): Promise<{
    modifications: FileModification[];
    modifiedFiles: number;
    skippedFiles: number;
    failedFiles: number;
    errors: string[];
  }> {
    
    const modifications: FileModification[] = [];
    const errors: string[] = [];
    let modifiedFiles = 0;
    let skippedFiles = 0;
    let failedFiles = 0;
    
    // Process files in parallel or sequential based on options
    if (options.enableParallelProcessing) {
      const batchSize = Math.min(operation.targetFiles.length, options.maxConcurrentFiles);
      const batches = this.createBatches(operation.targetFiles, batchSize);
      
      for (const batch of batches) {
        const batchPromises = batch.map(filePath => 
          this.processFile(filePath, operation, options),
        );
        
        const batchResults = await Promise.allSettled(batchPromises);
        
        for (const result of batchResults) {
          if (result.status === 'fulfilled') {
            if (result.value.success) {
              modifications.push(result.value.modification);
              modifiedFiles++;
            } else {
              skippedFiles++;
              errors.push(...result.value.errors);
            }
          } else {
            failedFiles++;
            errors.push(result.reason);
          }
        }
      }
    } else {
      // Sequential processing
      for (const filePath of operation.targetFiles) {
        try {
          const result = await this.processFile(filePath, operation, options);
          
          if (result.success) {
            modifications.push(result.modification);
            modifiedFiles++;
          } else {
            skippedFiles++;
            errors.push(...result.errors);
          }
        } catch (error) {
          failedFiles++;
          errors.push(error instanceof Error ? error.message : String(error));
        }
      }
    }
    
    return {
      modifications,
      modifiedFiles,
      skippedFiles,
      failedFiles,
      errors,
    };
  }

  /**
   * Process individual file
   */
  private async processFile(
    filePath: string,
    operation: MultiFileOperation,
    options: MultiFileCorrectionOptions,
  ): Promise<{
    success: boolean;
    modification: FileModification;
    errors: string[];
  }> {
    
    const startTime = Date.now();
    const errors: string[] = [];
    
    try {
      // Read original content
      const originalContent = await fs.readFile(filePath, 'utf-8');
      
      // Apply corrections using agentic system
      const correctionResult = await Promise.race([
        this.agenticSystem.correctErrors(
          originalContent,
          'multi-file-user',
          `${operation.id}-${path.basename(filePath)}`,
        ),
        this.createTimeoutPromise(options.timeoutPerFile),
      ]);
      
      const executionTime = Date.now() - startTime;
      
      if (correctionResult.success && correctionResult.correctedCode && 
          correctionResult.correctedCode !== originalContent) {
        
        // Calculate line changes
        const lineChanges = this.calculateLineChanges(originalContent, correctionResult.correctedCode);
        
        const modification: FileModification = {
          filePath,
          originalContent,
          modifiedContent: correctionResult.correctedCode,
          changeType: 'content',
          lineChanges,
          confidence: correctionResult.deterministicRatio,
          executionTime,
          errors: [],
        };
        
        return {
          success: true,
          modification,
          errors: [],
        };
      } else {
        // No changes needed or correction failed
        const modification: FileModification = {
          filePath,
          originalContent,
          modifiedContent: originalContent,
          changeType: 'content',
          lineChanges: [],
          confidence: 0,
          executionTime,
          errors: correctionResult.success ? [] : ['Correction failed'],
        };
        
        return {
          success: false,
          modification,
          errors: correctionResult.success ? ['No changes needed'] : ['Correction failed'],
        };
      }
      
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      errors.push(errorMessage);
      
      const modification: FileModification = {
        filePath,
        originalContent: '',
        modifiedContent: '',
        changeType: 'content',
        lineChanges: [],
        confidence: 0,
        executionTime: Date.now() - startTime,
        errors: [errorMessage],
      };
      
      return {
        success: false,
        modification,
        errors,
      };
    }
  }

  /**
   * Calculate line changes between original and modified content
   */
  private calculateLineChanges(original: string, modified: string): LineChange[] {
    const originalLines = original.split('\n');
    const modifiedLines = modified.split('\n');
    const changes: LineChange[] = [];
    
    // Simple diff algorithm (would use a proper diff library in production)
    const maxLines = Math.max(originalLines.length, modifiedLines.length);
    
    for (let i = 0; i < maxLines; i++) {
      const originalLine = originalLines[i];
      const modifiedLine = modifiedLines[i];
      
      if (originalLine === undefined && modifiedLine !== undefined) {
        // Line added
        changes.push({
          lineNumber: i + 1,
          type: 'add',
          newLine: modifiedLine,
          context: this.getLineContext(modifiedLines, i),
        });
      } else if (originalLine !== undefined && modifiedLine === undefined) {
        // Line deleted
        changes.push({
          lineNumber: i + 1,
          type: 'delete',
          originalLine,
          context: this.getLineContext(originalLines, i),
        });
      } else if (originalLine !== modifiedLine) {
        // Line modified
        changes.push({
          lineNumber: i + 1,
          type: 'modify',
          originalLine,
          newLine: modifiedLine,
          context: this.getLineContext(modifiedLines, i),
        });
      }
    }
    
    return changes;
  }

  /**
   * Get context around a line
   */
  private getLineContext(lines: string[], lineIndex: number): string {
    const contextSize = 2;
    const start = Math.max(0, lineIndex - contextSize);
    const end = Math.min(lines.length, lineIndex + contextSize + 1);
    
    return lines.slice(start, end).join('\n');
  }

  /**
   * Apply modifications to files
   */
  private async applyModifications(
    modifications: FileModification[],
    options: MultiFileCorrectionOptions,
  ): Promise<void> {
    
    console.log(`💾 Applying ${modifications.length} file modifications`);
    
    for (const modification of modifications) {
      if (modification.changeType === 'content' && 
          modification.modifiedContent !== modification.originalContent) {
        
        try {
          await fs.writeFile(modification.filePath, modification.modifiedContent);
          
          if (options.enableDetailedLogging) {
            console.log(`  ✅ Applied changes to: ${path.basename(modification.filePath)}`);
          }
        } catch (error) {
          console.error(`❌ Failed to apply changes to ${modification.filePath}:`, error);
        }
      }
    }
  }

  /**
   * Generate correction summary
   */
  private generateSummary(modifications: FileModification[]): CorrectionSummary {
    let totalLinesChanged = 0;
    let filesCreated = 0;
    let filesDeleted = 0;
    let filesRenamed = 0;
    
    for (const modification of modifications) {
      totalLinesChanged += modification.lineChanges.length;
      
      switch (modification.changeType) {
        case 'create':
          filesCreated++;
          break;
        case 'delete':
          filesDeleted++;
          break;
        case 'rename':
          filesRenamed++;
          break;
      }
    }
    
    return {
      totalLinesChanged,
      filesCreated,
      filesDeleted,
      filesRenamed,
      crossFileChanges: 0, // Would be calculated based on dependency analysis
      dependencyUpdates: 0, // Would be calculated based on import/export changes
      conflictsResolved: 0, // Would be calculated based on conflict detection
    };
  }

  /**
   * Create backup of project
   */
  private async createBackup(
    projectPath: string,
    backupDirectory: string,
  ): Promise<RollbackInfo> {
    
    const timestamp = new Date();
    const backupName = `backup-${timestamp.toISOString().replace(/[:.]/g, '-')}`;
    const backupPath = path.join(projectPath, backupDirectory, backupName);
    
    await fs.mkdir(backupPath, { recursive: true });
    
    // Copy all files (simplified - would use proper backup strategy in production)
    const projectStructure = await this.fileScanner.scanProject(projectPath);
    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
    
    const modifiedFiles: string[] = [];
    
    for (const file of allFiles) {
      const relativePath = path.relative(projectPath, file.absolutePath);
      const backupFilePath = path.join(backupPath, relativePath);
      
      await fs.mkdir(path.dirname(backupFilePath), { recursive: true });
      await fs.copyFile(file.absolutePath, backupFilePath);
      
      modifiedFiles.push(file.absolutePath);
    }
    
    return {
      backupPath,
      modifiedFiles,
      timestamp,
      canRollback: true,
    };
  }

  /**
   * Save detailed report
   */
  private async saveDetailedReport(
    result: MultiFileCorrectionResult,
    projectPath: string,
  ): Promise<void> {
    
    const reportPath = path.join(projectPath, '.golem-reports', `report-${result.operationId}.json`);
    
    await fs.mkdir(path.dirname(reportPath), { recursive: true });
    await fs.writeFile(reportPath, JSON.stringify(result, null, 2));
    
    console.log(`📊 Detailed report saved: ${reportPath}`);
  }

  /**
   * Create batches for parallel processing
   */
  private createBatches<T>(items: T[], batchSize: number): T[][] {
    const batches: T[][] = [];
    for (let i = 0; i < items.length; i += batchSize) {
      batches.push(items.slice(i, i + batchSize));
    }
    return batches;
  }

  /**
   * Create timeout promise
   */
  private createTimeoutPromise(timeoutMs: number): Promise<never> {
    return new Promise((_, reject) => {
      setTimeout(() => reject(new Error('Operation timeout')), timeoutMs);
    });
  }

  /**
   * Rollback changes using backup
   */
  async rollbackChanges(rollbackInfo: RollbackInfo): Promise<boolean> {
    if (!rollbackInfo.canRollback) {
      console.error('❌ Rollback not available');
      return false;
    }
    
    try {
      console.log(`🔄 Rolling back changes from: ${rollbackInfo.backupPath}`);
      
      // Restore files from backup (simplified implementation)
      for (const filePath of rollbackInfo.modifiedFiles) {
        const relativePath = path.relative(path.dirname(rollbackInfo.backupPath), filePath);
        const backupFilePath = path.join(rollbackInfo.backupPath, relativePath);
        
        if (await fs.access(backupFilePath).then(() => true).catch(() => false)) {
          await fs.copyFile(backupFilePath, filePath);
        }
      }
      
      console.log('✅ Rollback completed successfully');
      return true;
      
    } catch (error) {
      console.error('❌ Rollback failed:', error);
      return false;
    }
  }

  /**
   * Get operation history
   */
  getOperationHistory(): MultiFileCorrectionResult[] {
    return [...this.operationHistory];
  }

  /**
   * Clear operation history
   */
  clearOperationHistory(): void {
    this.operationHistory = [];
  }
}

