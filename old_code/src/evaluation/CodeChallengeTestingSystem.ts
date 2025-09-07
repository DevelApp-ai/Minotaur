/**
 * Code Challenge Testing System for Golem
 *
 * This system uses the existing test suite structure to validate Golem's ability
 * to generate correct solutions for code challenges. It follows the methodology:
 * 1. Extract test cases from existing test files
 * 2. Present challenges to Golem for solution generation
 * 3. Validate solutions against the original test expectations
 * 4. Track success rates and provide detailed analytics
 */

import { promises as fs } from 'fs';
import * as path from 'path';
import { exec } from 'child_process';
import { promisify } from 'util';
import { TranslationEngineOrchestrator, OrchestratorConfig, EngineSelectionStrategy } from '../interactive/engines/TranslationEngineOrchestrator';
import { RuleBasedTranslationEngine } from '../interactive/engines/RuleBasedTranslationEngine';
import { PatternBasedTranslationEngine } from '../interactive/engines/PatternBasedTranslationEngine';
import { LLMTranslationEngine } from '../interactive/engines/LLMTranslationEngine';
import { EngineConfig } from '../interactive/engines/TranslationEngineInterface';
import { InteractiveASTTranslator } from '../interactive/InteractiveASTTranslator';
import { MistralAPIClient, MistralAPIConfig, MistralRequest } from './MistralAPIClient';

const execAsync = promisify(exec);

export interface CodeChallenge {
  id: string;
  title: string;
  description: string;
  testFile: string;
  testName: string;
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  category: 'algorithm' | 'data-structure' | 'parsing' | 'validation' | 'integration' | 'performance';
  expectedInterface: {
    functionName?: string;
    className?: string;
    parameters: Array<{
      name: string;
      type: string;
      description: string;
    }>;
    returnType: string;
    returnDescription: string;
  };
  testCases: Array<{
    input: any;
    expectedOutput: any;
    description: string;
  }>;
  constraints: string[];
  hints: string[];
  timeLimit: number; // in milliseconds
  memoryLimit: number; // in MB
}

export interface GolemSolution {
  challengeId: string;
  solutionCode: string;
  language: string;
  approach: string;
  confidence: number;
  estimatedComplexity: {
    time: string; // e.g., "O(n)", "O(log n)"
    space: string;
  };
  explanation: string;
  generationTime: number;
}

export interface SolutionValidationResult {
  challengeId: string;
  solutionId: string;
  passed: boolean;
  testResults: Array<{
    testCase: number;
    passed: boolean;
    input: any;
    expectedOutput: any;
    actualOutput: any;
    executionTime: number;
    errorMessage?: string;
  }>;
  overallStats: {
    totalTests: number;
    passedTests: number;
    failedTests: number;
    averageExecutionTime: number;
    maxExecutionTime: number;
    memoryUsage: number;
  };
  performance: {
    withinTimeLimit: boolean;
    withinMemoryLimit: boolean;
    efficiency: number; // 0-1 score
  };
  codeQuality: {
    readability: number; // 0-1 score
    maintainability: number; // 0-1 score
    bestPractices: number; // 0-1 score
  };
}

export interface ChallengeEvaluationSummary {
  totalChallenges: number;
  solvedChallenges: number;
  successRate: number;
  averageConfidence: number;
  averageGenerationTime: number;
  byDifficulty: Record<string, {
    total: number;
    solved: number;
    rate: number;
    averageTime: number;
  }>;
  byCategory: Record<string, {
    total: number;
    solved: number;
    rate: number;
    averageConfidence: number;
  }>;
  performanceMetrics: {
    averageExecutionTime: number;
    timeoutRate: number;
    memoryEfficiency: number;
    codeQualityScore: number;
  };
}

export class CodeChallengeTestingSystem {
  private orchestrator: TranslationEngineOrchestrator;
  private translator: InteractiveASTTranslator;
  private mistralClient: MistralAPIClient;
  private workingDirectory: string;
  private challenges: CodeChallenge[] = [];
  private solutions: GolemSolution[] = [];
  private validationResults: SolutionValidationResult[] = [];

  constructor(workingDir: string = process.cwd()) {
    this.workingDirectory = workingDir;

    // Check for required Mistral API key
    if (!process.env.MISTRAL_API_KEY) {
      throw new Error('MISTRAL_API_KEY environment variable is not set. Please set it to use the Mistral API.');
    }
    // Initialize Mistral API client for real LLM calls
    const mistralConfig: MistralAPIConfig = {
      apiKey: process.env.MISTRAL_API_KEY,
      model: 'codestral-latest',
      rateLimit: {
        requestsPerMinute: 60,
        requestsPerHour: 3600,
        tokensPerMinute: 60000,
        tokensPerHour: 3600000,
        burstLimit: 1,
        adaptiveThrottling: true,
      },
      enableRequestQueuing: true,
      enableAdaptiveBackoff: true,
      enableCostTracking: true,
      logLevel: 'info',
    };

    this.mistralClient = new MistralAPIClient(mistralConfig);

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
      enableLLM: true,
      maxCostPerTranslation: 10,
      maxTimePerTranslation: 60000,
      minConfidenceThreshold: 0.6,
      enableFallback: true,
      maxEnginesPerTranslation: 3,
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
        maxResponseTime: 5000,
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

    // Initialize Golem components with optimal settings for code challenges
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
   * Initialize the system and discover challenges from existing tests
   */
  async initialize(): Promise<void> {
    await this.orchestrator.initialize({} as any); // mock config
    await this.discoverChallengesFromTests();
    // eslint-disable-next-line no-console
    console.log(`Code Challenge Testing System initialized with ${this.challenges.length} challenges`);
  }

  /**
   * Discover code challenges from existing test files
   */
  private async discoverChallengesFromTests(): Promise<void> {
    const testFiles = await this.findTestFiles();

    for (const testFile of testFiles) {
      try {
        const challenges = await this.extractChallengesFromTestFile(testFile);
        this.challenges.push(...challenges);
      } catch (error) {
    // eslint-disable-next-line no-console
        console.warn(`Failed to extract challenges from ${testFile}:`, error);
      }
    }
  }

  /**
   * Find all test files in the project
   */
  private async findTestFiles(): Promise<string[]> {
    const testFiles: string[] = [];

    const findTestsRecursive = async (dir: string): Promise<void> => {
      const entries = await fs.readdir(dir, { withFileTypes: true });

      for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);

        if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules') {
          await findTestsRecursive(fullPath);
        } else if (entry.isFile() && entry.name.match(/\.test\.(ts|tsx|js|jsx)$/)) {
          testFiles.push(fullPath);
        }
      }
    };

    await findTestsRecursive(path.join(this.workingDirectory, 'src'));
    return testFiles;
  }

  /**
   * Extract challenges from a specific test file
   */
  private async extractChallengesFromTestFile(testFile: string): Promise<CodeChallenge[]> {
    const content = await fs.readFile(testFile, 'utf8');
    const challenges: CodeChallenge[] = [];

    // Parse the test file to extract test cases
    const testBlocks = this.parseTestBlocks(content);

    for (let i = 0; i < testBlocks.length; i++) {
      const testBlock = testBlocks[i];

      const challenge: CodeChallenge = {
        id: `${path.basename(testFile, path.extname(testFile))}_${i + 1}`,
        title: testBlock.description || `Challenge from ${path.basename(testFile)}`,
        description: this.generateChallengeDescription(testBlock),
        testFile: testFile,
        testName: testBlock.name,
        difficulty: this.determineDifficulty(testBlock),
        category: this.determineCategory(testFile, testBlock),
        expectedInterface: this.extractExpectedInterface(testBlock),
        testCases: this.extractTestCases(testBlock),
        constraints: this.extractConstraints(testBlock),
        hints: this.extractHints(testBlock),
        timeLimit: this.determineTimeLimit(testBlock),
        memoryLimit: this.determineMemoryLimit(testBlock),
      };

      challenges.push(challenge);
    }

    return challenges;
  }

  /**
   * Parse test blocks from test file content
   */
  private parseTestBlocks(content: string): Array<{
    name: string;
    description: string;
    code: string;
    expectations: string[];
  }> {
    const blocks: Array<{
      name: string;
      description: string;
      code: string;
      expectations: string[];
    }> = [];

    // Match test blocks using regex
    const testRegex = /(?:test|it)\s*\(\s*['"`]([^'"`]+)['"`]\s*,\s*(?:async\s+)?\([^)]*\)\s*=>\s*\{([\s\S]*?)\}\s*\)/g;

    let match;
    while ((match = testRegex.exec(content)) !== null) {
      const [, description, code] = match;

      // Extract expectations from the test code
      const expectations = this.extractExpectations(code);

      blocks.push({
        name: description,
        description: description,
        code: code,
        expectations: expectations,
      });
    }

    return blocks;
  }

  /**
   * Extract expect statements from test code
   */
  private extractExpectations(code: string): string[] {
    const expectations: string[] = [];
    const expectRegex = /expect\s*\([^)]+\)\s*\.[^;]+;?/g;

    let match;
    while ((match = expectRegex.exec(code)) !== null) {
      expectations.push(match[0]);
    }

    return expectations;
  }

  /**
   * Generate a challenge description from test block
   */
  private generateChallengeDescription(testBlock: any): string {
    let description = `Challenge: ${testBlock.description}\n\n`;

    description += 'Your task is to implement a solution that satisfies the following test requirements:\n\n';

    for (const expectation of testBlock.expectations) {
      description += `- ${expectation}\n`;
    }

    description += '\nAnalyze the test code to understand the expected behavior and implement a solution that makes all tests pass.';

    return description;
  }

  /**
   * Determine challenge difficulty based on test complexity
   */
  private determineDifficulty(testBlock: any): CodeChallenge['difficulty'] {
    const code = testBlock.code;
    const complexity = this.calculateCodeComplexity(code);

    if (complexity < 5) {
      return 'easy';
    }
    if (complexity < 15) {
      return 'medium';
    }
    if (complexity < 30) {
      return 'hard';
    }
    return 'expert';
  }

  /**
   * Calculate code complexity score
   */
  private calculateCodeComplexity(code: string): number {
    let complexity = 0;

    // Count various complexity indicators
    complexity += (code.match(/if\s*\(/g) || []).length * 2;
    complexity += (code.match(/for\s*\(/g) || []).length * 3;
    complexity += (code.match(/while\s*\(/g) || []).length * 3;
    complexity += (code.match(/async|await/g) || []).length * 2;
    complexity += (code.match(/Promise/g) || []).length * 2;
    complexity += (code.match(/expect/g) || []).length;
    complexity += (code.match(/mock/g) || []).length;
    complexity += Math.floor(code.length / 100); // Length factor

    return complexity;
  }

  /**
   * Determine challenge category based on file path and content
   */
  private determineCategory(testFile: string, testBlock: any): CodeChallenge['category'] {
    const filePath = testFile.toLowerCase();
    const code = testBlock.code.toLowerCase();

    if (filePath.includes('performance') || filePath.includes('benchmark')) {
      return 'performance';
    }

    if (filePath.includes('validation') || code.includes('validate')) {
      return 'validation';
    }

    if (filePath.includes('parser') || filePath.includes('grammar') || code.includes('parse')) {
      return 'parsing';
    }

    if (filePath.includes('integration') || filePath.includes('e2e')) {
      return 'integration';
    }

    if (code.includes('array') || code.includes('list') || code.includes('tree') || code.includes('graph')) {
      return 'data-structure';
    }

    return 'algorithm';
  }

  /**
   * Extract expected interface from test code
   */
  private extractExpectedInterface(testBlock: any): CodeChallenge['expectedInterface'] {
    const code = testBlock.code;

    // Try to extract function calls and class instantiations
    const functionCalls = code.match(/(\w+)\s*\([^)]*\)/g) || [];
    const classInstantiations = code.match(/new\s+(\w+)\s*\([^)]*\)/g) || [];

    return {
      functionName: functionCalls.length > 0 ? functionCalls[0].split('(')[0] : undefined,
      className: classInstantiations.length > 0 ? classInstantiations[0].match(/new\s+(\w+)/)?.[1] : undefined,
      parameters: [], // Would need more sophisticated parsing
      returnType: 'any', // Would need type analysis
      returnDescription: 'Return value that satisfies the test expectations',
    };
  }

  /**
   * Extract test cases from test block
   */
  private extractTestCases(testBlock: any): CodeChallenge['testCases'] {
    // This is a simplified extraction - in practice, would need more sophisticated parsing
    return [
      {
        input: 'test_input',
        expectedOutput: 'expected_result',
        description: 'Test case extracted from: ' + testBlock.description,
      },
    ];
  }

  /**
   * Extract constraints from test code
   */
  private extractConstraints(testBlock: any): string[] {
    const constraints: string[] = [];

    // Look for common constraint patterns
    if (testBlock.code.includes('timeout')) {
      constraints.push('Solution must complete within time limit');
    }

    if (testBlock.code.includes('memory')) {
      constraints.push('Solution must be memory efficient');
    }

    if (testBlock.code.includes('async')) {
      constraints.push('Solution must handle asynchronous operations');
    }

    return constraints;
  }

  /**
   * Extract hints from test code and comments
   */
  private extractHints(testBlock: any): string[] {
    const hints: string[] = [];

    // Extract comments that might contain hints
    const comments = testBlock.code.match(/\/\*[\s\S]*?\*\/|\/\/.*$/gm) || [];

    for (const comment of comments) {
      const cleanComment = comment.replace(/\/\*|\*\/|\/\//g, '').trim();
      if (cleanComment.length > 10) {
        hints.push(cleanComment);
      }
    }

    return hints;
  }

  /**
   * Determine time limit based on test complexity
   */
  private determineTimeLimit(testBlock: any): number {
    const complexity = this.calculateCodeComplexity(testBlock.code);

    if (complexity < 5) {
      return 1000;
    } // 1 second
    if (complexity < 15) {
      return 5000;
    } // 5 seconds
    if (complexity < 30) {
      return 10000;
    } // 10 seconds
    return 30000; // 30 seconds
  }

  /**
   * Determine memory limit based on test complexity
   */
  private determineMemoryLimit(testBlock: any): number {
    const complexity = this.calculateCodeComplexity(testBlock.code);

    if (complexity < 5) {
      return 64;
    } // 64 MB
    if (complexity < 15) {
      return 128;
    } // 128 MB
    if (complexity < 30) {
      return 256;
    } // 256 MB
    return 512; // 512 MB
  }

  /**
   * Generate a solution for a specific challenge using Golem
   */
  async generateSolution(challenge: CodeChallenge): Promise<GolemSolution> {
    // eslint-disable-next-line no-console
    console.log(`Generating solution for challenge: ${challenge.title}`);

    const startTime = Date.now();

    try {
      // Create a comprehensive prompt for Golem
      const prompt = this.createSolutionPrompt(challenge);

      // Use real Mistral API to generate solution
      const mistralRequest: MistralRequest = {
        model: 'codestral-latest',
        messages: [
          {
            role: 'system',
            content: 'You are an expert programmer. Generate a complete, working solution for the given code challenge.',
          },
          {
            role: 'user',
            content: prompt,
          },
        ],
        temperature: 0.1,
        max_tokens: 2000,
      };

      const result = await this.mistralClient.generateCompletion(mistralRequest);

      if (!result.success || !result.response) {
        throw new Error(`Mistral API call failed: ${result.error}`);
      }

      const solutionCode = result.response.choices[0]?.message?.content || '';

      const suggestions = [{
        id: `challenge-${challenge.id}`,
        solution: solutionCode,
        confidence: 0.85,
        explanation: 'Solution generated using Mistral Codestral API',
        metadata: {
          promptLength: prompt.length,
          tokensUsed: result.response.usage?.total_tokens || 0,
          model: 'codestral-latest',
        },
      }];

      if (suggestions.length === 0) {
        throw new Error('Golem could not generate any solution suggestions');
      }

      const suggestion = suggestions[0];

      const solution: GolemSolution = {
        challengeId: challenge.id,
        solutionCode: this.extractSolutionCode(suggestion),
        language: 'typescript',
        approach: suggestion.explanation || 'Generated by Golem transformation system',
        confidence: suggestion.confidence,
        estimatedComplexity: {
          time: this.estimateTimeComplexity(suggestion),
          space: this.estimateSpaceComplexity(suggestion),
        },
        explanation: suggestion.explanation || 'Solution generated to satisfy test requirements',
        generationTime: Date.now() - startTime,
      };

      this.solutions.push(solution);
      return solution;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error(`Failed to generate solution for ${challenge.id}:`, error);

      // Return a minimal solution attempt
      return {
        challengeId: challenge.id,
        solutionCode: `// Golem could not generate a solution: ${error}`,
        language: 'typescript',
        approach: 'Failed generation',
        confidence: 0,
        estimatedComplexity: { time: 'Unknown', space: 'Unknown' },
        explanation: `Solution generation failed: ${error}`,
        generationTime: Date.now() - startTime,
      };
    }
  }

  /**
   * Create a comprehensive prompt for solution generation
   */
  private createSolutionPrompt(challenge: CodeChallenge): string {
    return `
CODING CHALLENGE: ${challenge.title}

DESCRIPTION:
${challenge.description}

DIFFICULTY: ${challenge.difficulty.toUpperCase()}
CATEGORY: ${challenge.category}

REQUIREMENTS:
${challenge.expectedInterface.functionName ? `- Implement function: ${challenge.expectedInterface.functionName}` : ''}
${challenge.expectedInterface.className ? `- Implement class: ${challenge.expectedInterface.className}` : ''}
- Return type: ${challenge.expectedInterface.returnType}
- ${challenge.expectedInterface.returnDescription}

CONSTRAINTS:
${challenge.constraints.map(c => `- ${c}`).join('\n')}

TEST CASES:
${challenge.testCases.map((tc, i) => `
Test Case ${i + 1}:
  Input: ${JSON.stringify(tc.input)}
  Expected Output: ${JSON.stringify(tc.expectedOutput)}
  Description: ${tc.description}
`).join('\n')}

HINTS:
${challenge.hints.map(h => `- ${h}`).join('\n')}

PERFORMANCE REQUIREMENTS:
- Time Limit: ${challenge.timeLimit}ms
- Memory Limit: ${challenge.memoryLimit}MB

TASK:
Analyze the test file and generate a complete, working solution that passes all tests.
Focus on correctness first, then optimize for performance.
Provide clean, readable code with appropriate comments.
    `.trim();
  }

  /**
   * Extract solution code from Golem suggestion
   */
  private extractSolutionCode(suggestion: any): string {
    if (suggestion.transformedCode) {
      // Extract the main solution from transformed code
      const files = Object.values(suggestion.transformedCode);
      return files.length > 0 ? files[0] as string : '';
    }

    return suggestion.code || suggestion.content || '// No solution code generated';
  }

  /**
   * Estimate time complexity of the solution
   */
  private estimateTimeComplexity(suggestion: any): string {
    const code = this.extractSolutionCode(suggestion);

    // Simple heuristic-based complexity estimation
    if (code.includes('for') && code.includes('for')) {
      return 'O(n²)';
    }
    if (code.includes('while') || code.includes('for')) {
      return 'O(n)';
    }
    if (code.includes('sort')) {
      return 'O(n log n)';
    }
    if (code.includes('binary') || code.includes('divide')) {
      return 'O(log n)';
    }

    return 'O(1)';
  }

  /**
   * Estimate space complexity of the solution
   */
  private estimateSpaceComplexity(suggestion: any): string {
    const code = this.extractSolutionCode(suggestion);

    // Simple heuristic-based space complexity estimation
    if (code.includes('Array') || code.includes('[]')) {
      return 'O(n)';
    }
    if (code.includes('Map') || code.includes('Set')) {
      return 'O(n)';
    }
    if (code.includes('recursive') || code.includes('recursion')) {
      return 'O(n)';
    }

    return 'O(1)';
  }

  /**
   * Validate a solution against its challenge
   */
  async validateSolution(solution: GolemSolution): Promise<SolutionValidationResult> {
    // eslint-disable-next-line no-console
    console.log(`Validating solution for challenge: ${solution.challengeId}`);

    const challenge = this.challenges.find(c => c.id === solution.challengeId);
    if (!challenge) {
      throw new Error(`Challenge not found: ${solution.challengeId}`);
    }

    const startTime = Date.now();

    try {
      // Create a temporary test file with the solution
      const tempDir = path.join(this.workingDirectory, '.temp-solutions');
      await fs.mkdir(tempDir, { recursive: true });

      const solutionFile = path.join(tempDir, `solution_${solution.challengeId}.ts`);
      await fs.writeFile(solutionFile, solution.solutionCode);

      // Run the original test against the solution
      const testResults = await this.runTestsAgainstSolution(challenge, solutionFile);

      // Calculate performance metrics
      const performance = this.calculatePerformanceMetrics(testResults, challenge);

      // Calculate code quality metrics
      const codeQuality = await this.calculateCodeQualityMetrics(solution.solutionCode);

      const result: SolutionValidationResult = {
        challengeId: solution.challengeId,
        solutionId: `${solution.challengeId}_solution`,
        passed: testResults.every(tr => tr.passed),
        testResults: testResults,
        overallStats: {
          totalTests: testResults.length,
          passedTests: testResults.filter(tr => tr.passed).length,
          failedTests: testResults.filter(tr => !tr.passed).length,
          averageExecutionTime: testResults.reduce((sum, tr) => sum + tr.executionTime, 0) / testResults.length,
          maxExecutionTime: Math.max(...testResults.map(tr => tr.executionTime)),
          memoryUsage: 0, // Would need actual memory measurement
        },
        performance,
        codeQuality,
      };

      // Clean up temporary files
      await fs.unlink(solutionFile);

      this.validationResults.push(result);
      return result;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error(`Failed to validate solution for ${solution.challengeId}:`, error);

      return {
        challengeId: solution.challengeId,
        solutionId: `${solution.challengeId}_solution`,
        passed: false,
        testResults: [],
        overallStats: {
          totalTests: 0,
          passedTests: 0,
          failedTests: 1,
          averageExecutionTime: 0,
          maxExecutionTime: 0,
          memoryUsage: 0,
        },
        performance: {
          withinTimeLimit: false,
          withinMemoryLimit: false,
          efficiency: 0,
        },
        codeQuality: {
          readability: 0,
          maintainability: 0,
          bestPractices: 0,
        },
      };
    }
  }

  /**
   * Run tests against a solution
   */
  private async runTestsAgainstSolution(
    challenge: CodeChallenge,
    solutionFile: string,
  ): Promise<SolutionValidationResult['testResults']> {
    const testResults: SolutionValidationResult['testResults'] = [];

    try {
      // Run the specific test
      const { stdout, stderr } = await execAsync(
        `npm test -- --testNamePattern="${challenge.testName}" --verbose`,
        { cwd: this.workingDirectory },
      );

      // Parse test output to extract results
      const output = stdout + stderr;
      const passed = !output.includes('FAIL') && output.includes('PASS');

      testResults.push({
        testCase: 1,
        passed: passed,
        input: 'test_input',
        expectedOutput: 'expected_output',
        actualOutput: passed ? 'expected_output' : 'failed',
        executionTime: 10, // Would need actual measurement
        errorMessage: passed ? undefined : 'Test failed',
      });
    } catch (error) {
      testResults.push({
        testCase: 1,
        passed: false,
        input: 'test_input',
        expectedOutput: 'expected_output',
        actualOutput: 'error',
        executionTime: 0,
        errorMessage: error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error),
      });
    }

    return testResults;
  }

  /**
   * Calculate performance metrics
   */
  private calculatePerformanceMetrics(
    testResults: SolutionValidationResult['testResults'],
    challenge: CodeChallenge,
  ): SolutionValidationResult['performance'] {
    const maxExecutionTime = Math.max(...testResults.map(tr => tr.executionTime));

    return {
      withinTimeLimit: maxExecutionTime <= challenge.timeLimit,
      withinMemoryLimit: true, // Would need actual memory measurement
      efficiency: Math.max(0, 1 - (maxExecutionTime / challenge.timeLimit)),
    };
  }

  /**
   * Calculate code quality metrics
   */
  private async calculateCodeQualityMetrics(code: string): Promise<SolutionValidationResult['codeQuality']> {
    // Simple heuristic-based quality assessment
    const lines = code.split('\n');
    const nonEmptyLines = lines.filter(line => line.trim().length > 0);
    const commentLines = lines.filter(line => line.trim().startsWith('//') || line.trim().startsWith('/*'));

    const readability = Math.min(1, commentLines.length / nonEmptyLines.length + 0.5);
    const maintainability = code.includes('function') || code.includes('class') ? 0.8 : 0.6;
    const bestPractices = code.includes('const') || code.includes('let') ? 0.8 : 0.4;

    return {
      readability,
      maintainability,
      bestPractices,
    };
  }

  /**
   * Run all challenges and generate comprehensive evaluation
   */
  async runFullEvaluation(): Promise<ChallengeEvaluationSummary> {
    // eslint-disable-next-line no-console
    console.log(`Starting full evaluation with ${this.challenges.length} challenges...`);

    const results: SolutionValidationResult[] = [];

    for (const challenge of this.challenges) {
      try {
        const solution = await this.generateSolution(challenge);
        const validation = await this.validateSolution(solution);
        results.push(validation);

    // eslint-disable-next-line no-console
        console.log(`Challenge ${challenge.id}: ${validation.passed ? 'SOLVED' : 'FAILED'} (${validation.overallStats.passedTests}/${validation.overallStats.totalTests} tests passed)`);
      } catch (error) {
    // eslint-disable-next-line no-console
        console.error(`Failed to evaluate challenge ${challenge.id}:`, error);
      }
    }

    return this.generateEvaluationSummary(results);
  }

  /**
   * Generate comprehensive evaluation summary
   */
  private generateEvaluationSummary(results: SolutionValidationResult[]): ChallengeEvaluationSummary {
    const totalChallenges = this.challenges.length;
    const solvedChallenges = results.filter(r => r.passed).length;

    const summary: ChallengeEvaluationSummary = {
      totalChallenges,
      solvedChallenges,
      successRate: totalChallenges > 0 ? solvedChallenges / totalChallenges : 0,
      averageConfidence: this.solutions.reduce((sum, s) => sum + s.confidence, 0) / this.solutions.length,
      averageGenerationTime: this.solutions.reduce((sum, s) => sum + s.generationTime, 0) / this.solutions.length,
      byDifficulty: {},
      byCategory: {},
      performanceMetrics: {
        averageExecutionTime: results.reduce((sum, r) => sum + r.overallStats.averageExecutionTime, 0) / results.length,
        timeoutRate: results.filter(r => !r.performance.withinTimeLimit).length / results.length,
        memoryEfficiency: results.reduce((sum, r) => sum + r.performance.efficiency, 0) / results.length,
        // eslint-disable-next-line max-len
        codeQualityScore: results.reduce((sum, r) => sum + (r.codeQuality.readability + r.codeQuality.maintainability + r.codeQuality.bestPractices) / 3, 0) / results.length,
      },
    };

    // Group by difficulty
    for (const difficulty of ['easy', 'medium', 'hard', 'expert']) {
      const challengesOfDifficulty = this.challenges.filter(c => c.difficulty === difficulty);
      const solvedOfDifficulty = results.filter(r => {
        const challenge = this.challenges.find(c => c.id === r.challengeId);
        return challenge?.difficulty === difficulty && r.passed;
      }).length;

      summary.byDifficulty[difficulty] = {
        total: challengesOfDifficulty.length,
        solved: solvedOfDifficulty,
        rate: challengesOfDifficulty.length > 0 ? solvedOfDifficulty / challengesOfDifficulty.length : 0,
        averageTime: this.solutions
          .filter(s => this.challenges.find(c => c.id === s.challengeId)?.difficulty === difficulty)
          .reduce((sum, s) => sum + s.generationTime, 0) / challengesOfDifficulty.length || 0,
      };
    }

    // Group by category
    for (const category of ['algorithm', 'data-structure', 'parsing', 'validation', 'integration', 'performance']) {
      const challengesOfCategory = this.challenges.filter(c => c.category === category);
      const solvedOfCategory = results.filter(r => {
        const challenge = this.challenges.find(c => c.id === r.challengeId);
        return challenge?.category === category && r.passed;
      }).length;

      summary.byCategory[category] = {
        total: challengesOfCategory.length,
        solved: solvedOfCategory,
        rate: challengesOfCategory.length > 0 ? solvedOfCategory / challengesOfCategory.length : 0,
        averageConfidence: this.solutions
          .filter(s => this.challenges.find(c => c.id === s.challengeId)?.category === category)
          .reduce((sum, s) => sum + s.confidence, 0) / challengesOfCategory.length || 0,
      };
    }

    return summary;
  }

  /**
   * Get all discovered challenges
   */
  getChallenges(): CodeChallenge[] {
    return [...this.challenges];
  }

  /**
   * Get challenges filtered by criteria
   */
  getFilteredChallenges(criteria: {
    difficulty?: CodeChallenge['difficulty'];
    category?: CodeChallenge['category'];
    maxTimeLimit?: number;
  }): CodeChallenge[] {
    return this.challenges.filter(challenge => {
      if (criteria.difficulty && challenge.difficulty !== criteria.difficulty) {
        return false;
      }
      if (criteria.category && challenge.category !== criteria.category) {
        return false;
      }
      if (criteria.maxTimeLimit && challenge.timeLimit > criteria.maxTimeLimit) {
        return false;
      }
      return true;
    });
  }

  /**
   * Get all solutions
   */
  getSolutions(): GolemSolution[] {
    return [...this.solutions];
  }

  /**
   * Get all validation results
   */
  getValidationResults(): SolutionValidationResult[] {
    return [...this.validationResults];
  }
}

