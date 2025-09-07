/**
 * Testing framework for inheritance-based compiler-compiler support.
 */
export class InheritanceTestFramework {
  private interpreter: Interpreter;
  private testResults: Map<string, TestResult>;
  private testSuites: Map<string, TestSuite>;

  /**
   * Creates a new InheritanceTestFramework instance.
   * @param interpreter The interpreter instance to test
   */
  constructor(interpreter: Interpreter) {
    this.interpreter = interpreter;
    this.testResults = new Map<string, TestResult>();
    this.testSuites = new Map<string, TestSuite>();
  }

  /**
   * Registers a test suite.
   * @param name The name of the test suite
   * @param suite The test suite
   */
  public registerTestSuite(name: string, suite: TestSuite): void {
    this.testSuites.set(name, suite);
  }

  /**
   * Runs all test suites.
   * @returns The overall test results
   */
  public runAllTests(): TestResults {
    const results = new TestResults();

    for (const [suiteName, suite] of this.testSuites) {
      const suiteResult = this.runTestSuite(suiteName, suite);
      results.addSuiteResult(suiteName, suiteResult);
    }

    return results;
  }

  /**
   * Runs a specific test suite.
   * @param suiteName The name of the test suite
   * @param suite The test suite
   * @returns The test suite results
   */
  public runTestSuite(suiteName: string, suite: TestSuite): TestSuiteResult {
    const suiteResult = new TestSuiteResult(suiteName);

    for (const test of suite.getTests()) {
      try {
        const testResult = this.runTest(test);
        suiteResult.addTestResult(test.getName(), testResult);
        this.testResults.set(`${suiteName}.${test.getName()}`, testResult);
      } catch (error) {
        const errorResult = new TestResult(
          test.getName(),
          TestStatus.Error,
          `Test execution failed: ${error instanceof Error ? error.message : String(error)}`,
          0,
        );
        suiteResult.addTestResult(test.getName(), errorResult);
        this.testResults.set(`${suiteName}.${test.getName()}`, errorResult);
      }
    }

    return suiteResult;
  }

  /**
   * Runs a single test.
   * @param test The test to run
   * @returns The test result
   */
  public runTest(test: InheritanceTest): TestResult {
    const startTime = Date.now();

    try {
      // Setup test environment
      this.setupTestEnvironment(test);

      // Execute test
      const success = test.execute(this.interpreter);
      const endTime = Date.now();

      const status = success ? TestStatus.Passed : TestStatus.Failed;
      const message = success ? 'Test passed' : test.getFailureMessage();

      return new TestResult(test.getName(), status, message, endTime - startTime);
    } catch (error) {
      const endTime = Date.now();
      return new TestResult(
        test.getName(),
        TestStatus.Error,
        `Test error: ${error instanceof Error ? error.message : String(error)}`,
        endTime - startTime,
      );
    } finally {
      // Cleanup test environment
      this.cleanupTestEnvironment(test);
    }
  }

  /**
   * Sets up the test environment for a test.
   * @param test The test to setup for
   */
  private setupTestEnvironment(test: InheritanceTest): void {
    // Clear existing grammars
    this.interpreter.clearGrammars();

    // Load test grammars
    const testGrammars = test.getTestGrammars();
    for (const [grammarName, grammarContent] of testGrammars) {
      this.interpreter.loadGrammarFromString(grammarContent, grammarName);
    }

    // Setup test callbacks
    const testCallbacks = test.getTestCallbacks();
    for (const [callbackName, callback] of testCallbacks) {
      this.interpreter.registerCallback(callbackName, callback);
    }
  }

  /**
   * Cleans up the test environment after a test.
   * @param test The test to cleanup after
   */
  private cleanupTestEnvironment(test: InheritanceTest): void {
    // Clear grammars and caches
    this.interpreter.clearGrammars();
    this.interpreter.clearSemanticActions('');
    this.interpreter.clearPrecedenceCache();
    this.interpreter.clearErrorRecoveryCache();
  }

  /**
   * Creates a basic inheritance test.
   * @param name The test name
   * @param baseGrammar The base grammar content
   * @param derivedGrammar The derived grammar content
   * @param expectedBehavior The expected behavior description
   * @returns The inheritance test
   */
  public createBasicInheritanceTest(
    name: string,
    baseGrammar: string,
    derivedGrammar: string,
    expectedBehavior: string,
  ): BasicInheritanceTest {
    return new BasicInheritanceTest(name, baseGrammar, derivedGrammar, expectedBehavior);
  }

  /**
   * Creates a semantic action inheritance test.
   * @param name The test name
   * @param baseGrammar The base grammar content
   * @param derivedGrammar The derived grammar content
   * @param testInput The input to test with
   * @param expectedOutput The expected output
   * @returns The semantic action test
   */
  public createSemanticActionTest(
    name: string,
    baseGrammar: string,
    derivedGrammar: string,
    testInput: string,
    expectedOutput: any,
  ): SemanticActionInheritanceTest {
    return new SemanticActionInheritanceTest(name, baseGrammar, derivedGrammar, testInput, expectedOutput);
  }

  /**
   * Creates a precedence inheritance test.
   * @param name The test name
   * @param baseGrammar The base grammar content
   * @param derivedGrammar The derived grammar content
   * @param testExpression The expression to test
   * @param expectedPrecedence The expected precedence behavior
   * @returns The precedence test
   */
  public createPrecedenceTest(
    name: string,
    baseGrammar: string,
    derivedGrammar: string,
    testExpression: string,
    expectedPrecedence: string,
  ): PrecedenceInheritanceTest {
    return new PrecedenceInheritanceTest(name, baseGrammar, derivedGrammar, testExpression, expectedPrecedence);
  }

  /**
   * Creates an error recovery inheritance test.
   * @param name The test name
   * @param baseGrammar The base grammar content
   * @param derivedGrammar The derived grammar content
   * @param errorInput The input that should cause an error
   * @param expectedRecovery The expected recovery behavior
   * @returns The error recovery test
   */
  public createErrorRecoveryTest(
    name: string,
    baseGrammar: string,
    derivedGrammar: string,
    errorInput: string,
    expectedRecovery: string,
  ): ErrorRecoveryInheritanceTest {
    return new ErrorRecoveryInheritanceTest(name, baseGrammar, derivedGrammar, errorInput, expectedRecovery);
  }

  /**
   * Gets test results.
   * @param testName The name of the test (optional)
   * @returns The test result or all results
   */
  public getTestResults(testName?: string): TestResult | Map<string, TestResult> {
    if (testName) {
      return this.testResults.get(testName) || null;
    }
    return new Map(this.testResults);
  }

  /**
   * Generates a test report.
   * @param results The test results
   * @returns The test report
   */
  public generateTestReport(results: TestResults): string {
    let report = '=== Inheritance Test Framework Report ===\n\n';

    const summary = results.getSummary();
    report += `Total Tests: ${summary.totalTests}\n`;
    report += `Passed: ${summary.passedTests}\n`;
    report += `Failed: ${summary.failedTests}\n`;
    report += `Errors: ${summary.errorTests}\n`;
    report += `Success Rate: ${(summary.passedTests / summary.totalTests * 100).toFixed(2)}%\n`;
    report += `Total Time: ${summary.totalTime}ms\n\n`;

    for (const [suiteName, suiteResult] of results.getSuiteResults()) {
      report += `=== Test Suite: ${suiteName} ===\n`;

      for (const [testName, testResult] of suiteResult.getTestResults()) {
        const status = testResult.getStatus();
        const statusSymbol = status === TestStatus.Passed ? '✓' :
          status === TestStatus.Failed ? '✗' : '!';

        report += `${statusSymbol} ${testName}: ${testResult.getMessage()} (${testResult.getExecutionTime()}ms)\n`;
      }

      report += '\n';
    }

    return report;
  }
}

/**
 * Interface for inheritance tests.
 */
export interface InheritanceTest {
  getName(): string;
  getDescription(): string;
  getTestGrammars(): Map<string, string>;
  getTestCallbacks(): Map<string, Function>;
  execute(interpreter: Interpreter): boolean;
  getFailureMessage(): string;
}

/**
 * Basic inheritance test implementation.
 */
export class BasicInheritanceTest implements InheritanceTest {
  private name: string;
  private baseGrammar: string;
  private derivedGrammar: string;
  private expectedBehavior: string;
  private failureMessage: string;

  constructor(name: string, baseGrammar: string, derivedGrammar: string, expectedBehavior: string) {
    this.name = name;
    this.baseGrammar = baseGrammar;
    this.derivedGrammar = derivedGrammar;
    this.expectedBehavior = expectedBehavior;
    this.failureMessage = '';
  }

  public getName(): string {
    return this.name;
  }

  public getDescription(): string {
    return `Basic inheritance test: ${this.expectedBehavior}`;
  }

  public getTestGrammars(): Map<string, string> {
    const grammars = new Map<string, string>();
    grammars.set('base', this.baseGrammar);
    grammars.set('derived', this.derivedGrammar);
    return grammars;
  }

  public getTestCallbacks(): Map<string, Function> {
    return new Map<string, Function>();
  }

  public execute(interpreter: Interpreter): boolean {
    try {
      // Check that derived grammar inherits from base
      const derivedGrammar = interpreter.getGrammarContainer().getGrammar('derived');
      if (!derivedGrammar) {
        this.failureMessage = 'Derived grammar not found';
        return false;
      }

      const baseGrammars = derivedGrammar.getBaseGrammars();
      if (!baseGrammars.includes('base')) {
        this.failureMessage = 'Derived grammar does not inherit from base grammar';
        return false;
      }

      // Check inheritance hierarchy
      const hierarchy = interpreter.getGrammarContainer().getInheritanceHierarchy('derived');
      if (!hierarchy.includes('base')) {
        this.failureMessage = 'Base grammar not found in inheritance hierarchy';
        return false;
      }

      return true;
    } catch (error) {
      this.failureMessage = `Execution error: ${error instanceof Error ? error.message : String(error)}`;
      return false;
    }
  }

  public getFailureMessage(): string {
    return this.failureMessage;
  }
}

/**
 * Semantic action inheritance test implementation.
 */
export class SemanticActionInheritanceTest implements InheritanceTest {
  private name: string;
  private baseGrammar: string;
  private derivedGrammar: string;
  private testInput: string;
  private expectedOutput: any;
  private failureMessage: string;

  constructor(name: string, baseGrammar: string, derivedGrammar: string, testInput: string, expectedOutput: any) {
    this.name = name;
    this.baseGrammar = baseGrammar;
    this.derivedGrammar = derivedGrammar;
    this.testInput = testInput;
    this.expectedOutput = expectedOutput;
    this.failureMessage = '';
  }

  public getName(): string {
    return this.name;
  }

  public getDescription(): string {
    return `Semantic action inheritance test with input: ${this.testInput}`;
  }

  public getTestGrammars(): Map<string, string> {
    const grammars = new Map<string, string>();
    grammars.set('base', this.baseGrammar);
    grammars.set('derived', this.derivedGrammar);
    return grammars;
  }

  public getTestCallbacks(): Map<string, Function> {
    const callbacks = new Map<string, Function>();
    callbacks.set('testCallback', (args: any[]) => {
      return args.join(' ');
    });
    return callbacks;
  }

  public execute(interpreter: Interpreter): boolean {
    try {
      // Process inherited semantic actions
      interpreter.processInheritedSemanticActions('derived');

      // Check that semantic actions are inherited
      const actions = interpreter.getAllSemanticActions('derived');
      if (actions.size === 0) {
        this.failureMessage = 'No semantic actions found in derived grammar';
        return false;
      }

      // Test semantic action execution (simplified)
      // In a real implementation, this would parse the input and execute actions
      return true;
    } catch (error) {
      this.failureMessage = `Semantic action test error: ${error instanceof Error ? error.message : String(error)}`;
      return false;
    }
  }

  public getFailureMessage(): string {
    return this.failureMessage;
  }
}

/**
 * Precedence inheritance test implementation.
 */
export class PrecedenceInheritanceTest implements InheritanceTest {
  private name: string;
  private baseGrammar: string;
  private derivedGrammar: string;
  private testExpression: string;
  private expectedPrecedence: string;
  private failureMessage: string;

  // eslint-disable-next-line max-len
  constructor(name: string, baseGrammar: string, derivedGrammar: string, testExpression: string, expectedPrecedence: string) {
    this.name = name;
    this.baseGrammar = baseGrammar;
    this.derivedGrammar = derivedGrammar;
    this.testExpression = testExpression;
    this.expectedPrecedence = expectedPrecedence;
    this.failureMessage = '';
  }

  public getName(): string {
    return this.name;
  }

  public getDescription(): string {
    return `Precedence inheritance test for expression: ${this.testExpression}`;
  }

  public getTestGrammars(): Map<string, string> {
    const grammars = new Map<string, string>();
    grammars.set('base', this.baseGrammar);
    grammars.set('derived', this.derivedGrammar);
    return grammars;
  }

  public getTestCallbacks(): Map<string, Function> {
    return new Map<string, Function>();
  }

  public execute(interpreter: Interpreter): boolean {
    try {
      // Process inherited precedence rules
      interpreter.processInheritedPrecedenceRules('derived');

      // Check that precedence rules are inherited
      const precedenceRules = interpreter.getAllPrecedenceRules('derived');
      if (precedenceRules.size === 0) {
        this.failureMessage = 'No precedence rules found in derived grammar';
        return false;
      }

      // Test precedence comparison (simplified)
      const comparison = interpreter.comparePrecedence('derived', '+', '*');
      if (comparison === null) {
        this.failureMessage = 'Could not compare precedence of operators';
        return false;
      }

      return true;
    } catch (error) {
      this.failureMessage = `Precedence test error: ${error instanceof Error ? error.message : String(error)}`;
      return false;
    }
  }

  public getFailureMessage(): string {
    return this.failureMessage;
  }
}

/**
 * Error recovery inheritance test implementation.
 */
export class ErrorRecoveryInheritanceTest implements InheritanceTest {
  private name: string;
  private baseGrammar: string;
  private derivedGrammar: string;
  private errorInput: string;
  private expectedRecovery: string;
  private failureMessage: string;

  constructor(name: string, baseGrammar: string, derivedGrammar: string, errorInput: string, expectedRecovery: string) {
    this.name = name;
    this.baseGrammar = baseGrammar;
    this.derivedGrammar = derivedGrammar;
    this.errorInput = errorInput;
    this.expectedRecovery = expectedRecovery;
    this.failureMessage = '';
  }

  public getName(): string {
    return this.name;
  }

  public getDescription(): string {
    return `Error recovery inheritance test for input: ${this.errorInput}`;
  }

  public getTestGrammars(): Map<string, string> {
    const grammars = new Map<string, string>();
    grammars.set('base', this.baseGrammar);
    grammars.set('derived', this.derivedGrammar);
    return grammars;
  }

  public getTestCallbacks(): Map<string, Function> {
    return new Map<string, Function>();
  }

  public execute(interpreter: Interpreter): boolean {
    try {
      // Process inherited error recovery strategies
      interpreter.processInheritedErrorRecoveryStrategies('derived');

      // Check that error recovery strategies are inherited
      const strategies = interpreter.getAllErrorRecoveryStrategies('derived');
      if (strategies.size === 0) {
        this.failureMessage = 'No error recovery strategies found in derived grammar';
        return false;
      }

      // Test error recovery (simplified)
      const context = new ErrorContext('derived', ErrorType.SyntaxError, 'Test error', 0, 1, 1);
      const result = interpreter.applyErrorRecovery('derived', ErrorType.SyntaxError, context);

      if (!result.isSuccessful()) {
        this.failureMessage = 'Error recovery failed';
        return false;
      }

      return true;
    } catch (error) {
      this.failureMessage = `Error recovery test error: ${error instanceof Error ? error.message : String(error)}`;
      return false;
    }
  }

  public getFailureMessage(): string {
    return this.failureMessage;
  }
}

/**
 * Test suite container.
 */
export class TestSuite {
  private name: string;
  private tests: InheritanceTest[];
  private description: string;

  constructor(name: string, description: string = '') {
    this.name = name;
    this.tests = [];
    this.description = description;
  }

  public getName(): string {
    return this.name;
  }

  public getDescription(): string {
    return this.description;
  }

  public addTest(test: InheritanceTest): void {
    this.tests.push(test);
  }

  public getTests(): InheritanceTest[] {
    return [...this.tests];
  }
}

/**
 * Test result container.
 */
export class TestResult {
  private name: string;
  private status: TestStatus;
  private message: string;
  private executionTime: number;

  constructor(name: string, status: TestStatus, message: string, executionTime: number) {
    this.name = name;
    this.status = status;
    this.message = message;
    this.executionTime = executionTime;
  }

  public getName(): string {
    return this.name;
  }

  public getStatus(): TestStatus {
    return this.status;
  }

  public getMessage(): string {
    return this.message;
  }

  public getExecutionTime(): number {
    return this.executionTime;
  }
}

/**
 * Test suite result container.
 */
export class TestSuiteResult {
  private suiteName: string;
  private testResults: Map<string, TestResult>;

  constructor(suiteName: string) {
    this.suiteName = suiteName;
    this.testResults = new Map<string, TestResult>();
  }

  public getSuiteName(): string {
    return this.suiteName;
  }

  public addTestResult(testName: string, result: TestResult): void {
    this.testResults.set(testName, result);
  }

  public getTestResults(): Map<string, TestResult> {
    return new Map(this.testResults);
  }
}

/**
 * Overall test results container.
 */
export class TestResults {
  private suiteResults: Map<string, TestSuiteResult>;

  constructor() {
    this.suiteResults = new Map<string, TestSuiteResult>();
  }

  public addSuiteResult(suiteName: string, result: TestSuiteResult): void {
    this.suiteResults.set(suiteName, result);
  }

  public getSuiteResults(): Map<string, TestSuiteResult> {
    return new Map(this.suiteResults);
  }

  public getSummary(): TestSummary {
    let totalTests = 0;
    let passedTests = 0;
    let failedTests = 0;
    let errorTests = 0;
    let totalTime = 0;

    for (const suiteResult of this.suiteResults.values()) {
      for (const testResult of suiteResult.getTestResults().values()) {
        totalTests++;
        totalTime += testResult.getExecutionTime();

        switch (testResult.getStatus()) {
          case TestStatus.Passed:
            passedTests++;
            break;
          case TestStatus.Failed:
            failedTests++;
            break;
          case TestStatus.Error:
            errorTests++;
            break;
        }
      }
    }

    return new TestSummary(totalTests, passedTests, failedTests, errorTests, totalTime);
  }
}

/**
 * Test summary container.
 */
export class TestSummary {
  public totalTests: number;
  public passedTests: number;
  public failedTests: number;
  public errorTests: number;
  public totalTime: number;

  constructor(totalTests: number, passedTests: number, failedTests: number, errorTests: number, totalTime: number) {
    this.totalTests = totalTests;
    this.passedTests = passedTests;
    this.failedTests = failedTests;
    this.errorTests = errorTests;
    this.totalTime = totalTime;
  }
}

/**
 * Test status enumeration.
 */
export enum TestStatus {
  Passed = 'passed',
  Failed = 'failed',
  Error = 'error'
}

// Import required classes and interfaces
import { Interpreter } from '../utils/Interpreter';
import { ErrorContext, ErrorType } from '../utils/ErrorRecoveryManager';

