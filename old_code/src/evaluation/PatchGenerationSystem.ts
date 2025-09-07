/**
 * Patch Generation and Application System
 *
 * This system generates patches using Golem transformations and applies them
 * to validate against the existing test suite. The core idea is:
 * 1. Find failing tests or create scenarios where tests should fail
 * 2. Ask Golem to generate a fix/transformation
 * 3. Apply the transformation and see if tests pass
 * 4. Use test results as ground truth for Golem's effectiveness
 */

import { exec } from 'child_process';
import { promises as fs } from 'fs';
import * as path from 'path';
import { promisify } from 'util';
import { TranslationEngineOrchestrator, EngineSelectionStrategy, OrchestratorConfig } from '../interactive/engines/TranslationEngineOrchestrator';
import { RuleBasedTranslationEngine } from '../interactive/engines/RuleBasedTranslationEngine';
import { PatternBasedTranslationEngine } from '../interactive/engines/PatternBasedTranslationEngine';
import { LLMTranslationEngine } from '../interactive/engines/LLMTranslationEngine';
import { EngineConfig } from '../interactive/engines/TranslationEngineInterface';
import { InteractiveASTTranslator } from '../interactive/InteractiveASTTranslator';

const execAsync = promisify(exec);

export interface TestFailure {
  testName: string;
  testFile: string;
  errorMessage: string;
  stackTrace: string;
  affectedFiles: string[];
  failureType: 'compilation' | 'runtime' | 'assertion' | 'timeout';
}

export interface GolemPatchRequest {
  id: string;
  description: string;
  failingTest: TestFailure;
  sourceFiles: Record<string, string>; // filename -> content
  expectedOutcome: string;
  context: {
    projectType: string;
    language: string;
    framework: string;
  };
}

export interface GeneratedPatch {
  id: string;
  requestId: string;
  patchContent: string;
  modifiedFiles: Record<string, string>; // filename -> new content
  confidence: number;
  reasoning: string;
  estimatedImpact: {
    linesChanged: number;
    filesModified: number;
    riskLevel: 'low' | 'medium' | 'high';
  };
}

export interface PatchValidationResult {
  patchId: string;
  applied: boolean;
  compilationSuccess: boolean;
  testResults: {
    totalTests: number;
    passedTests: number;
    failedTests: number;
    newFailures: string[];
    fixedFailures: string[];
    regressions: string[];
  };
  performance: {
    applicationTime: number;
    testExecutionTime: number;
    memoryUsage: number;
  };
  quality: {
    codeQuality: number;
    maintainability: number;
    testCoverage: number;
  };
  success: boolean;
  errorMessages: string[];
}

export class PatchGenerationSystem {
  private orchestrator: TranslationEngineOrchestrator;
  private translator: InteractiveASTTranslator;
  private workingDirectory: string;
  private testCommand: string;
  private buildCommand: string;

  constructor(
    workingDir: string = process.cwd(),
    testCmd: string = 'npm test',
    buildCmd: string = 'npm run build',
  ) {
    this.workingDirectory = workingDir;
    this.testCommand = testCmd;
    this.buildCommand = buildCmd;

    // Create individual translation engines
    const ruleBasedEngine = new RuleBasedTranslationEngine();
    const patternBasedEngine = new PatternBasedTranslationEngine();
    const llmEngine = new LLMTranslationEngine();

    // Create orchestrator config
    const orchestratorConfig = {
      strategy: EngineSelectionStrategy.BEST_RESULT,
      selectionStrategy: EngineSelectionStrategy.BEST_RESULT,
      enableRuleBased: true,
      enablePatternBased: true,
      enableLLM: false, // Start with offline-only for reliability
      maxCostPerTranslation: 0,
      maxTimePerTranslation: 30000,
      minConfidenceThreshold: 0.7,
      enableFallback: true,
      maxEnginesPerTranslation: 2, // Only rule-based and pattern-based
      engineConfigs: {
        'rule-based': {
          settings: {
            strictMode: false,
            enableOptimizations: true,
          },
        } as unknown as EngineConfig,
        'pattern-based': {
          settings: {
            similarityThreshold: 0.7,
            maxPatterns: 1000,
            learningEnabled: true,
          },
        } as unknown as EngineConfig,
        'llm': {
          settings: {
            provider: 'mock',
            defaultModel: 'test-model',
          },
        } as unknown as EngineConfig,
      },
      enginePriorities: {
        'rule-based': 100,
        'pattern-based': 80,
        'llm': 60,
      },
      qualityThresholds: {
        minSyntacticCorrectness: 0.8,
        minSemanticPreservation: 0.7,
        minOverallQuality: 0.75,
      },
      performanceThresholds: {
        maxResponseTime: 30000,
        maxMemoryUsage: 100,
        minSuccessRate: 0.9,
      },
      healthCheck: {
        interval: 30000,
        timeout: 5000,
        failureThreshold: 3,
        recoveryInterval: 60000,
      },
    } as OrchestratorConfig;

    // Initialize Golem components
    this.orchestrator = new TranslationEngineOrchestrator(
      ruleBasedEngine,
      patternBasedEngine,
      llmEngine,
      orchestratorConfig,
    );

    this.translator = new InteractiveASTTranslator(
      {} as any, // validator (mock)
      {} as any, // translation system (mock)
      orchestratorConfig,
    );
  }

  /**
   * Initialize the patch generation system
   */
  async initialize(): Promise<void> {
    await this.orchestrator.initialize({} as any); // mock config
    // eslint-disable-next-line no-console
    console.log('Patch Generation System initialized');
  }

  /**
   * Discover failing tests in the current codebase
   */
  async discoverFailingTests(): Promise<TestFailure[]> {
    // eslint-disable-next-line no-console
    console.log('Discovering failing tests...');

    try {
      // Run tests and capture output
      const { stdout, stderr } = await execAsync(
        `${this.testCommand} --verbose --no-coverage 2>&1 || true`,
        { cwd: this.workingDirectory, maxBuffer: 1024 * 1024 * 10 },
      );

      const output = stdout + stderr;
      return this.parseTestFailures(output);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to run tests:', error);
      return [];
    }
  }

  /**
   * Parse test output to extract failure information
   */
  private parseTestFailures(testOutput: string): TestFailure[] {
    const failures: TestFailure[] = [];
    const lines = testOutput.split('\n');

    let currentFailure: Partial<TestFailure> | null = null;
    let inStackTrace = false;
    let stackTrace = '';

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Detect test failure start
      if (line.includes('FAIL') && line.includes('.test.')) {
        if (currentFailure) {
          // Save previous failure
          if (currentFailure.testName && currentFailure.testFile) {
            failures.push({
              testName: currentFailure.testName,
              testFile: currentFailure.testFile,
              errorMessage: currentFailure.errorMessage || '',
              stackTrace: stackTrace.trim(),
              affectedFiles: currentFailure.affectedFiles || [],
              failureType: this.determineFailureType(currentFailure.errorMessage || '', stackTrace),
            });
          }
        }

        // Start new failure
        currentFailure = {
          testFile: this.extractTestFile(line),
          affectedFiles: [],
        };
        stackTrace = '';
        inStackTrace = false;
      }

      // Extract test name
      if (line.trim().startsWith('✕') || line.trim().startsWith('×')) {
        if (currentFailure) {
          currentFailure.testName = this.extractTestName(line);
        }
      }

      // Extract error message
      if (line.trim().startsWith('Error:') || line.trim().startsWith('TypeError:') ||
          line.trim().startsWith('ReferenceError:') || line.includes('Expected:')) {
        if (currentFailure) {
          currentFailure.errorMessage = line.trim();
          inStackTrace = true;
        }
      }

      // Collect stack trace
      if (inStackTrace && (line.includes('at ') || line.includes('src/'))) {
        stackTrace += line + '\n';

        // Extract affected files from stack trace
        const fileMatch = line.match(/at.*\(([^)]+)\)/);
        if (fileMatch && currentFailure) {
          const filePath = fileMatch[1].split(':')[0];
          if (filePath.includes('src/') && !currentFailure.affectedFiles?.includes(filePath)) {
            currentFailure.affectedFiles = currentFailure.affectedFiles || [];
            currentFailure.affectedFiles.push(filePath);
          }
        }
      }
    }

    // Don't forget the last failure
    if (currentFailure && currentFailure.testName && currentFailure.testFile) {
      failures.push({
        testName: currentFailure.testName,
        testFile: currentFailure.testFile,
        errorMessage: currentFailure.errorMessage || '',
        stackTrace: stackTrace.trim(),
        affectedFiles: currentFailure.affectedFiles || [],
        failureType: this.determineFailureType(currentFailure.errorMessage || '', stackTrace),
      });
    }

    // eslint-disable-next-line no-console
    console.log(`Discovered ${failures.length} failing tests`);
    return failures;
  }

  /**
   * Extract test file path from FAIL line
   */
  private extractTestFile(line: string): string {
    const match = line.match(/FAIL\s+(.+\.test\.[jt]sx?)/);
    return match ? match[1] : '';
  }

  /**
   * Extract test name from failure line
   */
  private extractTestName(line: string): string {
    const cleaned = line.replace(/^[✕×]\s*/, '').trim();
    return cleaned;
  }

  /**
   * Determine the type of test failure
   */
  private determineFailureType(errorMessage: string, stackTrace: string): TestFailure['failureType'] {
    const combined = (errorMessage + stackTrace).toLowerCase();

    if (combined.includes('syntaxerror') || combined.includes('unexpected token') ||
        combined.includes('cannot resolve module')) {
      return 'compilation';
    }

    if (combined.includes('timeout') || combined.includes('exceeded')) {
      return 'timeout';
    }

    if (combined.includes('referenceerror') || combined.includes('typeerror') ||
        combined.includes('cannot read property')) {
      return 'runtime';
    }

    return 'assertion';
  }

  /**
   * Create a patch request from a failing test
   */
  async createPatchRequest(failure: TestFailure): Promise<GolemPatchRequest> {
    const sourceFiles: Record<string, string> = {};

    // Read the test file
    try {
      const testContent = await fs.readFile(
        path.join(this.workingDirectory, failure.testFile),
        'utf8',
      );
      sourceFiles[failure.testFile] = testContent;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn(`Could not read test file ${failure.testFile}:`, error);
    }

    // Read affected source files
    for (const filePath of failure.affectedFiles) {
      try {
        const content = await fs.readFile(
          path.join(this.workingDirectory, filePath),
          'utf8',
        );
        sourceFiles[filePath] = content;
      } catch (error) {
    // eslint-disable-next-line no-console
        console.warn(`Could not read affected file ${filePath}:`, error);
      }
    }

    return {
      id: `patch-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      description: `Fix failing test: ${failure.testName}`,
      failingTest: failure,
      sourceFiles,
      expectedOutcome: `The test "${failure.testName}" should pass without errors`,
      context: {
        projectType: 'typescript-react',
        language: 'typescript',
        framework: 'jest',
      },
    };
  }

  /**
   * Generate a patch using Golem's transformation capabilities
   */
  async generatePatch(request: GolemPatchRequest): Promise<GeneratedPatch> {
    // eslint-disable-next-line no-console
    console.log(`Generating patch for: ${request.description}`);

    try {
      // Create a comprehensive prompt for Golem
      const prompt = this.createGolemPrompt(request);

      // Use Golem's translation system to generate a fix
      const suggestions = [{
        id: 'mock-suggestion',
        solution: '// Generated mock patch solution',
        confidence: 0.8,
        explanation: 'Mock solution generated for testing',
        engineUsed: 'mock-engine',
        approach: 'rule-based',
        metadata: {},
      }];

      if (suggestions.length === 0) {
        throw new Error('Golem could not generate any suggestions');
      }

      const suggestion = suggestions[0];

      // Convert suggestion to patch format
      const patch = await this.convertSuggestionToPatch(suggestion, request);

      return {
        id: `patch-${Date.now()}`,
        requestId: request.id,
        patchContent: patch.content,
        modifiedFiles: patch.modifiedFiles,
        confidence: suggestion.confidence,
        reasoning: suggestion.explanation || 'Golem transformation applied',
        estimatedImpact: {
          linesChanged: this.countChangedLines(patch.content),
          filesModified: Object.keys(patch.modifiedFiles).length,
          riskLevel: this.assessRiskLevel(patch.content, Object.keys(patch.modifiedFiles).length),
        },
      };
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to generate patch:', error);

      // Return a minimal patch that at least attempts to address the issue
      return {
        id: `patch-${Date.now()}`,
        requestId: request.id,
        patchContent: `// Golem could not generate a patch: ${error}`,
        modifiedFiles: {},
        confidence: 0,
        reasoning: `Failed to generate patch: ${error}`,
        estimatedImpact: {
          linesChanged: 0,
          filesModified: 0,
          riskLevel: 'low',
        },
      };
    }
  }

  /**
   * Create a comprehensive prompt for Golem
   */
  private createGolemPrompt(request: GolemPatchRequest): string {
    return `
Fix the failing test: ${request.failingTest.testName}

Error Details:
- Test File: ${request.failingTest.testFile}
- Error Message: ${request.failingTest.errorMessage}
- Failure Type: ${request.failingTest.failureType}

Stack Trace:
${request.failingTest.stackTrace}

Affected Files:
${request.failingTest.affectedFiles.join(', ')}

Expected Outcome:
${request.expectedOutcome}

Please analyze the error and provide a fix that will make the test pass.
Focus on minimal changes that address the root cause of the failure.
    `.trim();
  }

  /**
   * Convert Golem suggestion to patch format
   */
  private async convertSuggestionToPatch(
    suggestion: any,
    request: GolemPatchRequest,
  ): Promise<{ content: string; modifiedFiles: Record<string, string> }> {
    const modifiedFiles: Record<string, string> = {};
    let patchContent = '';

    // If suggestion contains transformed code, create patches for each file
    if (suggestion.transformedCode) {
      for (const [filePath, originalContent] of Object.entries(request.sourceFiles)) {
        if (suggestion.transformedCode[filePath]) {
          const newContent = suggestion.transformedCode[filePath];
          modifiedFiles[filePath] = newContent;

          // Generate unified diff
          const diff = await this.generateUnifiedDiff(
            filePath,
            originalContent,
            newContent,
          );
          patchContent += diff + '\n';
        }
      }
    }

    return { content: patchContent, modifiedFiles };
  }

  /**
   * Generate unified diff between old and new content
   */
  private async generateUnifiedDiff(
    filePath: string,
    oldContent: string,
    newContent: string,
  ): Promise<string> {
    try {
      // Write temporary files
      const tempDir = path.join(this.workingDirectory, '.temp-patch');
      await fs.mkdir(tempDir, { recursive: true });

      const oldFile = path.join(tempDir, 'old');
      const newFile = path.join(tempDir, 'new');

      await fs.writeFile(oldFile, oldContent);
      await fs.writeFile(newFile, newContent);

      // Generate diff
      const { stdout } = await execAsync(`diff -u ${oldFile} ${newFile} || true`);

      // Clean up
      await fs.unlink(oldFile);
      await fs.unlink(newFile);
      await fs.rmdir(tempDir);

      // Format as proper patch
      return stdout
        .replace(/^--- .*$/m, `--- a/${filePath}`)
        .replace(/^\+\+\+ .*$/m, `+++ b/${filePath}`);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn('Failed to generate diff:', error);
      return `--- a/${filePath}\n+++ b/${filePath}\n@@ -1,1 +1,1 @@\n-${oldContent}\n+${newContent}`;
    }
  }

  /**
   * Apply a generated patch to the codebase
   */
  async applyPatch(patch: GeneratedPatch): Promise<PatchValidationResult> {
    // eslint-disable-next-line no-console
    console.log(`Applying patch: ${patch.id}`);

    const startTime = Date.now();
    const result: PatchValidationResult = {
      patchId: patch.id,
      applied: false,
      compilationSuccess: false,
      testResults: {
        totalTests: 0,
        passedTests: 0,
        failedTests: 0,
        newFailures: [],
        fixedFailures: [],
        regressions: [],
      },
      performance: {
        applicationTime: 0,
        testExecutionTime: 0,
        memoryUsage: 0,
      },
      quality: {
        codeQuality: 0,
        maintainability: 0,
        testCoverage: 0,
      },
      success: false,
      errorMessages: [],
    };

    try {
      // Create backup
      await this.createBackup();

      // Apply file changes
      for (const [filePath, newContent] of Object.entries(patch.modifiedFiles)) {
        const fullPath = path.join(this.workingDirectory, filePath);
        await fs.writeFile(fullPath, newContent, 'utf8');
      }

      result.applied = true;
      result.performance.applicationTime = Date.now() - startTime;

      // Check compilation
      const compilationStart = Date.now();
      try {
        await execAsync(this.buildCommand, { cwd: this.workingDirectory });
        result.compilationSuccess = true;
      } catch (error) {
        result.errorMessages.push(`Compilation failed: ${error}`);
      }

      // Run tests if compilation succeeded
      if (result.compilationSuccess) {
        const testStart = Date.now();
        const testResults = await this.runTestsAndAnalyze();
        result.testResults = testResults;
        result.performance.testExecutionTime = Date.now() - testStart;

        // Determine overall success
        result.success = testResults.failedTests === 0 || testResults.fixedFailures.length > 0;
      }

      // Calculate quality metrics
      result.quality = await this.calculateQualityMetrics(patch);

    } catch (error) {
      result.errorMessages.push(`Patch application failed: ${error}`);
    } finally {
      // Always restore backup
      await this.restoreBackup();
      result.performance.applicationTime = Date.now() - startTime;
    }

    return result;
  }

  /**
   * Run tests and analyze results
   */
  private async runTestsAndAnalyze(): Promise<PatchValidationResult['testResults']> {
    try {
      const { stdout, stderr } = await execAsync(
        `${this.testCommand} --verbose --passWithNoTests 2>&1 || true`,
        { cwd: this.workingDirectory, maxBuffer: 1024 * 1024 * 10 },
      );

      const output = stdout + stderr;

      // Parse test results
      const totalMatch = output.match(/Tests:\s+(\d+)\s+total/);
      const passedMatch = output.match(/(\d+)\s+passed/);
      const failedMatch = output.match(/(\d+)\s+failed/);

      return {
        totalTests: totalMatch ? parseInt(totalMatch[1]) : 0,
        passedTests: passedMatch ? parseInt(passedMatch[1]) : 0,
        failedTests: failedMatch ? parseInt(failedMatch[1]) : 0,
        newFailures: [], // Would need more sophisticated parsing
        fixedFailures: [], // Would need comparison with previous run
        regressions: [],
      };
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to run tests:', error);
      return {
        totalTests: 0,
        passedTests: 0,
        failedTests: 1,
        newFailures: ['Test execution failed'],
        fixedFailures: [],
        regressions: [],
      };
    }
  }

  /**
   * Helper methods
   */

  private countChangedLines(patchContent: string): number {
    return patchContent.split('\n').filter(line =>
      line.startsWith('+') || line.startsWith('-'),
    ).length;
  }

  private assessRiskLevel(patchContent: string, filesModified: number): 'low' | 'medium' | 'high' {
    const linesChanged = this.countChangedLines(patchContent);

    if (filesModified > 5 || linesChanged > 100) {
      return 'high';
    }
    if (filesModified > 2 || linesChanged > 20) {
      return 'medium';
    }
    return 'low';
  }

  private async createBackup(): Promise<void> {
    await execAsync('git stash push -m "golem-patch-backup"', { cwd: this.workingDirectory });
  }

  private async restoreBackup(): Promise<void> {
    try {
      await execAsync('git stash pop', { cwd: this.workingDirectory });
    } catch {
      await execAsync('git reset --hard HEAD', { cwd: this.workingDirectory });
    }
  }

  private async calculateQualityMetrics(patch: GeneratedPatch): Promise<PatchValidationResult['quality']> {
    // Placeholder implementation - would integrate with actual quality tools
    return {
      codeQuality: patch.confidence * 0.8,
      maintainability: patch.estimatedImpact.riskLevel === 'low' ? 0.9 : 0.6,
      testCoverage: 0.8, // Would need actual coverage analysis
    };
  }
}

