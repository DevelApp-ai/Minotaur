/**
 * Golem Quality Testing System
 *
 * This system evaluates Golem's transformation capabilities using patch-based testing
 * methodology similar to how LLMs are evaluated. It tests Golem's ability to generate
 * patches that resolve issues and pass existing test suites.
 */

import { exec } from 'child_process';
import { promises as fs } from 'fs';
import * as path from 'path';
import { promisify } from 'util';

const execAsync = promisify(exec);

export interface BugReport {
  id: string;
  title: string;
  description: string;
  reproductionSteps: string[];
  expectedBehavior: string;
  actualBehavior: string;
  affectedFiles: string[];
  testFiles: string[];
  severity: 'low' | 'medium' | 'high' | 'critical';
  category: 'compilation' | 'runtime' | 'logic' | 'performance' | 'ui';
}

export interface TestCase {
  id: string;
  bugReport: BugReport;
  beforeState: {
    commitHash: string;
    files: Record<string, string>; // filename -> content
    failingTests: string[];
  };
  afterState: {
    commitHash: string;
    files: Record<string, string>; // filename -> content
    passingTests: string[];
  };
  humanPatch: string;
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
}

export interface GolemEvaluationResult {
  testCaseId: string;
  success: boolean;
  generatedPatch: string;
  appliedSuccessfully: boolean;
  testsPassedBefore: string[];
  testsPassedAfter: string[];
  testsFailedBefore: string[];
  testsFailedAfter: string[];
  compilationSuccess: boolean;
  executionTime: number;
  confidence: number;
  errorMessages: string[];
  metrics: {
    linesChanged: number;
    filesModified: number;
    testCoverage: number;
    codeQuality: number;
  };
}

export interface EvaluationSummary {
  totalTests: number;
  passedTests: number;
  failedTests: number;
  passRate: number;
  averageConfidence: number;
  averageExecutionTime: number;
  byDifficulty: Record<string, { passed: number; total: number; rate: number }>;
  byCategory: Record<string, { passed: number; total: number; rate: number }>;
  bySeverity: Record<string, { passed: number; total: number; rate: number }>;
}

export class GolemQualityTestingSystem {
  private testCases: TestCase[] = [];
  private workingDirectory: string;
  private backupDirectory: string;

  constructor(workingDir: string = process.cwd()) {
    this.workingDirectory = workingDir;
    this.backupDirectory = path.join(workingDir, '.golem-testing-backup');
  }

  /**
   * Initialize the testing system and load test cases
   */
  async initialize(): Promise<void> {
    await this.ensureBackupDirectory();
    await this.loadTestCases();
    // eslint-disable-next-line no-console
    console.log(`Initialized Golem Quality Testing System with ${this.testCases.length} test cases`);
  }

  /**
   * Create a backup of the current repository state
   */
  private async ensureBackupDirectory(): Promise<void> {
    try {
      await fs.access(this.backupDirectory);
    } catch {
      await fs.mkdir(this.backupDirectory, { recursive: true });
    }
  }

  /**
   * Load test cases from the benchmark database
   */
  private async loadTestCases(): Promise<void> {
    // For now, we'll create some sample test cases based on the project history
    this.testCases = await this.generateTestCasesFromHistory();
  }

  /**
   * Generate test cases from git history by analyzing bug fix commits
   */
  private async generateTestCasesFromHistory(): Promise<TestCase[]> {
    const testCases: TestCase[] = [];

    try {
      // Get commits that likely contain bug fixes
      const { stdout } = await execAsync(
        'git log --oneline --grep="fix\\|Fix\\|bug\\|Bug\\|error\\|Error" --since="1 year ago"',
        { cwd: this.workingDirectory },
      );

      const commits = stdout.trim().split('\n').slice(0, 20); // Limit to 20 most recent

      for (let i = 0; i < commits.length; i++) {
        const commit = commits[i];
        if (!commit) {
          continue;
        }

        const [hash, ...messageParts] = commit.split(' ');
        const message = messageParts.join(' ');

        try {
          const testCase = await this.createTestCaseFromCommit(hash, message, i);
          if (testCase) {
            testCases.push(testCase);
          }
        } catch (error) {
    // eslint-disable-next-line no-console
          console.warn(`Failed to create test case from commit ${hash}:`, error);
        }
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to generate test cases from history:', error);
    }

    return testCases;
  }

  /**
   * Create a test case from a specific commit
   */
  private async createTestCaseFromCommit(
    commitHash: string,
    commitMessage: string,
    index: number,
  ): Promise<TestCase | null> {
    try {
      // Get the parent commit (before the fix)
      const { stdout: parentHash } = await execAsync(
        `git rev-parse ${commitHash}^`,
        { cwd: this.workingDirectory },
      );

      // Get changed files in this commit
      const { stdout: changedFiles } = await execAsync(
        `git diff --name-only ${parentHash.trim()} ${commitHash}`,
        { cwd: this.workingDirectory },
      );

      const files = changedFiles.trim().split('\n').filter(f => f.length > 0);

      if (files.length === 0) {
        return null;
      }

      // Get the diff to understand what was changed
      const { stdout: diff } = await execAsync(
        `git diff ${parentHash.trim()} ${commitHash}`,
        { cwd: this.workingDirectory },
      );

      // Create bug report from commit message and diff
      const bugReport: BugReport = {
        id: `bug-${index + 1}`,
        title: commitMessage,
        description: `Issue resolved in commit ${commitHash}: ${commitMessage}`,
        reproductionSteps: ['Run the affected code', 'Observe the issue'],
        expectedBehavior: 'Code should work correctly',
        actualBehavior: 'Code has issues that need fixing',
        affectedFiles: files.filter(f => f.endsWith('.ts') || f.endsWith('.tsx') || f.endsWith('.js')),
        testFiles: files.filter(f => f.includes('test') || f.includes('spec')),
        severity: this.determineSeverity(commitMessage, diff),
        category: this.determineCategory(commitMessage, diff, files),
      };

      // Get file contents before and after
      const beforeFiles: Record<string, string> = {};
      const afterFiles: Record<string, string> = {};

      for (const file of bugReport.affectedFiles) {
        try {
          // Get file content before fix
          const { stdout: beforeContent } = await execAsync(
            `git show ${parentHash.trim()}:${file}`,
            { cwd: this.workingDirectory },
          );
          beforeFiles[file] = beforeContent;

          // Get file content after fix
          const { stdout: afterContent } = await execAsync(
            `git show ${commitHash}:${file}`,
            { cwd: this.workingDirectory },
          );
          afterFiles[file] = afterContent;
        } catch (error) {
          // File might not exist in one of the states, skip it
          continue;
        }
      }

      if (Object.keys(beforeFiles).length === 0) {
        return null;
      }

      const testCase: TestCase = {
        id: `test-case-${index + 1}`,
        bugReport,
        beforeState: {
          commitHash: parentHash.trim(),
          files: beforeFiles,
          failingTests: [], // We'll determine this when running tests
        },
        afterState: {
          commitHash: commitHash,
          files: afterFiles,
          passingTests: [], // We'll determine this when running tests
        },
        humanPatch: diff,
        difficulty: this.determineDifficulty(diff, files.length),
      };

      return testCase;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn(`Failed to create test case from commit ${commitHash}:`, error);
      return null;
    }
  }

  /**
   * Determine the severity of a bug based on commit message and diff
   */
  private determineSeverity(message: string, diff: string): BugReport['severity'] {
    const lowerMessage = message.toLowerCase();
    const lowerDiff = diff.toLowerCase();

    if (lowerMessage.includes('critical') || lowerMessage.includes('crash') ||
        lowerMessage.includes('security') || lowerDiff.includes('throw') ||
        lowerDiff.includes('error')) {
      return 'critical';
    }

    if (lowerMessage.includes('important') || lowerMessage.includes('major') ||
        lowerDiff.includes('null') || lowerDiff.includes('undefined')) {
      return 'high';
    }

    if (lowerMessage.includes('minor') || lowerMessage.includes('small')) {
      return 'low';
    }

    return 'medium';
  }

  /**
   * Determine the category of a bug based on commit message, diff, and files
   */
  private determineCategory(
    message: string,
    diff: string,
    files: string[],
  ): BugReport['category'] {
    const lowerMessage = message.toLowerCase();
    const lowerDiff = diff.toLowerCase();

    if (lowerMessage.includes('compil') || lowerDiff.includes('import') ||
        lowerDiff.includes('syntax') || files.some(f => f.includes('tsconfig'))) {
      return 'compilation';
    }

    if (lowerMessage.includes('ui') || lowerMessage.includes('component') ||
        files.some(f => f.endsWith('.tsx') || f.endsWith('.css'))) {
      return 'ui';
    }

    if (lowerMessage.includes('performance') || lowerMessage.includes('slow') ||
        lowerMessage.includes('memory') || lowerMessage.includes('optimization')) {
      return 'performance';
    }

    if (lowerMessage.includes('runtime') || lowerDiff.includes('throw') ||
        lowerDiff.includes('catch') || lowerDiff.includes('try')) {
      return 'runtime';
    }

    return 'logic';
  }

  /**
   * Determine the difficulty of a test case based on the diff complexity
   */
  private determineDifficulty(diff: string, fileCount: number): TestCase['difficulty'] {
    const lines = diff.split('\n').length;
    const complexity = lines + (fileCount * 10);

    if (complexity < 20) {
      return 'easy';
    }
    if (complexity < 50) {
      return 'medium';
    }
    if (complexity < 100) {
      return 'hard';
    }
    return 'expert';
  }

  /**
   * Run a single test case against Golem
   */
  // eslint-disable-next-line max-len
  async runTestCase(testCase: TestCase, golemTransformFunction: (bugReport: BugReport, files: Record<string, string>) => Promise<string>): Promise<GolemEvaluationResult> {
    const startTime = Date.now();

    // eslint-disable-next-line no-console
    console.log(`Running test case: ${testCase.id} - ${testCase.bugReport.title}`);

    try {
      // Create a backup of current state
      await this.createBackup();

      // Apply the "before" state
      await this.applyFileState(testCase.beforeState.files);

      // Run tests to confirm they fail (if any)
      const beforeTestResults = await this.runTests();

      // Ask Golem to generate a patch
      const generatedPatch = await golemTransformFunction(testCase.bugReport, testCase.beforeState.files);

      // Apply Golem's patch
      const patchApplied = await this.applyPatch(generatedPatch);

      let afterTestResults = { passed: [], failed: [] };
      let compilationSuccess = false;

      if (patchApplied) {
        // Check if code compiles
        compilationSuccess = await this.checkCompilation();

        if (compilationSuccess) {
          // Run tests to see if they pass
          afterTestResults = await this.runTests();
        }
      }

      // Calculate metrics
      const metrics = await this.calculateMetrics(testCase, generatedPatch);

      const result: GolemEvaluationResult = {
        testCaseId: testCase.id,
        success: patchApplied && compilationSuccess && afterTestResults.failed.length === 0,
        generatedPatch,
        appliedSuccessfully: patchApplied,
        testsPassedBefore: beforeTestResults.passed,
        testsPassedAfter: afterTestResults.passed,
        testsFailedBefore: beforeTestResults.failed,
        testsFailedAfter: afterTestResults.failed,
        compilationSuccess,
        executionTime: Date.now() - startTime,
        confidence: this.calculateConfidence(testCase, generatedPatch, afterTestResults),
        errorMessages: [],
        metrics,
      };

      // Restore from backup
      await this.restoreFromBackup();

      return result;
    } catch (error) {
      // Restore from backup on error
      await this.restoreFromBackup();

      return {
        testCaseId: testCase.id,
        success: false,
        generatedPatch: '',
        appliedSuccessfully: false,
        testsPassedBefore: [],
        testsPassedAfter: [],
        testsFailedBefore: [],
        testsFailedAfter: [],
        compilationSuccess: false,
        executionTime: Date.now() - startTime,
        confidence: 0,
        // eslint-disable-next-line max-len
        errorMessages: [error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)],
        metrics: { linesChanged: 0, filesModified: 0, testCoverage: 0, codeQuality: 0 },
      };
    }
  }

  /**
   * Run all test cases and generate a comprehensive evaluation report
   */
  // eslint-disable-next-line max-len
  async runFullEvaluation(golemTransformFunction: (bugReport: BugReport, files: Record<string, string>) => Promise<string>): Promise<EvaluationSummary> {
    // eslint-disable-next-line no-console
    console.log(`Starting full evaluation with ${this.testCases.length} test cases...`);

    const results: GolemEvaluationResult[] = [];

    for (const testCase of this.testCases) {
      const result = await this.runTestCase(testCase, golemTransformFunction);
      results.push(result);

    // eslint-disable-next-line no-console
      console.log(`Test ${testCase.id}: ${result.success ? 'PASSED' : 'FAILED'} (${result.executionTime}ms)`);
    }

    return this.generateEvaluationSummary(results);
  }

  /**
   * Generate evaluation summary from results
   */
  private generateEvaluationSummary(results: GolemEvaluationResult[]): EvaluationSummary {
    const totalTests = results.length;
    const passedTests = results.filter(r => r.success).length;
    const failedTests = totalTests - passedTests;

    const summary: EvaluationSummary = {
      totalTests,
      passedTests,
      failedTests,
      passRate: totalTests > 0 ? passedTests / totalTests : 0,
      averageConfidence: results.reduce((sum, r) => sum + r.confidence, 0) / totalTests,
      averageExecutionTime: results.reduce((sum, r) => sum + r.executionTime, 0) / totalTests,
      byDifficulty: {},
      byCategory: {},
      bySeverity: {},
    };

    // Group by difficulty
    for (const testCase of this.testCases) {
      const difficulty = testCase.difficulty;
      if (!summary.byDifficulty[difficulty]) {
        summary.byDifficulty[difficulty] = { passed: 0, total: 0, rate: 0 };
      }
      summary.byDifficulty[difficulty].total++;

      const result = results.find(r => r.testCaseId === testCase.id);
      if (result?.success) {
        summary.byDifficulty[difficulty].passed++;
      }

      summary.byDifficulty[difficulty].rate =
        summary.byDifficulty[difficulty].passed / summary.byDifficulty[difficulty].total;
    }

    // Group by category and severity
    for (const testCase of this.testCases) {
      const category = testCase.bugReport.category;
      const severity = testCase.bugReport.severity;

      if (!summary.byCategory[category]) {
        summary.byCategory[category] = { passed: 0, total: 0, rate: 0 };
      }
      if (!summary.bySeverity[severity]) {
        summary.bySeverity[severity] = { passed: 0, total: 0, rate: 0 };
      }

      summary.byCategory[category].total++;
      summary.bySeverity[severity].total++;

      const result = results.find(r => r.testCaseId === testCase.id);
      if (result?.success) {
        summary.byCategory[category].passed++;
        summary.bySeverity[severity].passed++;
      }

      summary.byCategory[category].rate =
        summary.byCategory[category].passed / summary.byCategory[category].total;
      summary.bySeverity[severity].rate =
        summary.bySeverity[severity].passed / summary.bySeverity[severity].total;
    }

    return summary;
  }

  /**
   * Helper methods for file operations and testing
   */

  private async createBackup(): Promise<void> {
    // Create backup of current git state
    await execAsync('git stash push -m "golem-testing-backup"', { cwd: this.workingDirectory });
  }

  private async restoreFromBackup(): Promise<void> {
    try {
      // Restore from git stash
      await execAsync('git stash pop', { cwd: this.workingDirectory });
    } catch {
      // If no stash exists, just reset to HEAD
      await execAsync('git reset --hard HEAD', { cwd: this.workingDirectory });
    }
  }

  private async applyFileState(files: Record<string, string>): Promise<void> {
    for (const [filePath, content] of Object.entries(files)) {
      const fullPath = path.join(this.workingDirectory, filePath);
      await fs.mkdir(path.dirname(fullPath), { recursive: true });
      await fs.writeFile(fullPath, content, 'utf8');
    }
  }

  private async applyPatch(patch: string): Promise<boolean> {
    try {
      // Write patch to temporary file
      const patchFile = path.join(this.workingDirectory, '.golem-patch.patch');
      await fs.writeFile(patchFile, patch, 'utf8');

      // Apply patch using git
      await execAsync(`git apply ${patchFile}`, { cwd: this.workingDirectory });

      // Clean up patch file
      await fs.unlink(patchFile);

      return true;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn('Failed to apply patch:', error);
      return false;
    }
  }

  private async checkCompilation(): Promise<boolean> {
    try {
      await execAsync('npm run build', { cwd: this.workingDirectory });
      return true;
    } catch {
      return false;
    }
  }

  private async runTests(): Promise<{ passed: string[]; failed: string[] }> {
    try {
      const { stdout } = await execAsync('npm test -- --passWithNoTests --json', {
        cwd: this.workingDirectory,
      });

      // Parse Jest output to get test results
      const lines = stdout.trim().split('\n');
      const jsonLine = lines.find(line => line.startsWith('{'));

      if (jsonLine) {
        const results = JSON.parse(jsonLine);
        return {
          passed: results.testResults?.filter((t: any) => t.status === 'passed').map((t: any) => t.name) || [],
          failed: results.testResults?.filter((t: any) => t.status === 'failed').map((t: any) => t.name) || [],
        };
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn('Test execution failed:', error);
    }

    return { passed: [], failed: [] };
  }

  private async calculateMetrics(testCase: TestCase, patch: string): Promise<GolemEvaluationResult['metrics']> {
    const lines = patch.split('\n');
    const changedLines = lines.filter(line => line.startsWith('+') || line.startsWith('-')).length;
    const modifiedFiles = new Set(
      lines
        .filter(line => line.startsWith('+++') || line.startsWith('---'))
        .map(line => line.split('\t')[0].replace(/^[+-]{3}\s*/, '')),
    ).size;

    return {
      linesChanged: changedLines,
      filesModified: modifiedFiles,
      testCoverage: 0.8, // Placeholder - would need actual coverage analysis
      codeQuality: 0.7,   // Placeholder - would need actual quality analysis
    };
  }

  private calculateConfidence(
    testCase: TestCase,
    patch: string,
    testResults: { passed: string[]; failed: string[] },
  ): number {
    let confidence = 0.5; // Base confidence

    // Increase confidence if tests pass
    if (testResults.failed.length === 0) {
      confidence += 0.3;
    }

    // Increase confidence if patch is reasonable size
    const patchSize = patch.split('\n').length;
    if (patchSize > 5 && patchSize < 50) {
      confidence += 0.1;
    }

    // Decrease confidence for very complex changes
    if (patchSize > 100) {
      confidence -= 0.2;
    }

    return Math.max(0, Math.min(1, confidence));
  }

  /**
   * Get all available test cases
   */
  getTestCases(): TestCase[] {
    return [...this.testCases];
  }

  /**
   * Get test cases filtered by criteria
   */
  getFilteredTestCases(criteria: {
    difficulty?: TestCase['difficulty'];
    category?: BugReport['category'];
    severity?: BugReport['severity'];
  }): TestCase[] {
    return this.testCases.filter(testCase => {
      if (criteria.difficulty && testCase.difficulty !== criteria.difficulty) {
        return false;
      }
      if (criteria.category && testCase.bugReport.category !== criteria.category) {
        return false;
      }
      if (criteria.severity && testCase.bugReport.severity !== criteria.severity) {
        return false;
      }
      return true;
    });
  }
}

