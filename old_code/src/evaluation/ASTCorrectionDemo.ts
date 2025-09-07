/**
 * AST Correction System Demonstration
 * 
 * This script demonstrates the complete AST-guided error correction system
 * in action, showing how it can automatically fix common Python errors.
 */

import { StructuredBenchmarkValidator } from './StructuredBenchmarkValidator';
import { ASTErrorCorrector } from './ASTErrorCorrector';
import { CorrectionFeedbackLoop } from './CorrectionFeedbackLoop';
import { GolemSolver } from './GolemSolver';
import { 
  StructuredValidationError, 
  ErrorType, 
  ErrorSeverity, 
} from './StructuredValidationError';
import { BenchmarkProblem } from './BenchmarkDatasetManager';
import { GolemSolution } from './GolemBenchmarkSolver';

export interface DemoResult {
  testName: string;
  originalCode: string;
  errors: StructuredValidationError[];
  correctedCode: string;
  success: boolean;
  appliedFixes: number;
  astTransformations: number;
  correctionTime: number;
}

export class ASTCorrectionDemo {
  private validator: StructuredBenchmarkValidator;
  private corrector: ASTErrorCorrector;
  private feedbackLoop: CorrectionFeedbackLoop;

  constructor() {
    // Initialize with mock configuration for demo
    const mockMistralConfig = {
      apiKey: 'demo-key',
      baseURL: 'https://api.mistral.ai',
      model: 'codestral-latest',
      rateLimit: {
        requestsPerMinute: 60,
        requestsPerHour: 1000,
        tokensPerMinute: 100000,
        tokensPerHour: 1000000,
        burstLimit: 10,
        adaptiveThrottling: true,
      },
      enableRequestQueuing: true,
      enableAdaptiveBackoff: true,
      enableCostTracking: false,
      logLevel: 'info' as const,
    };

    this.validator = new StructuredBenchmarkValidator();
    this.corrector = new ASTErrorCorrector(mockMistralConfig);
    this.feedbackLoop = new CorrectionFeedbackLoop(this.validator, this.corrector);
  }

  /**
   * Run all demonstration scenarios
   */
  async runAllDemos(): Promise<DemoResult[]> {
    console.log('🎯 Starting AST Error Correction System Demonstration\n');

    const demos = [
      this.demoSyntaxErrorCorrection(),
      this.demoNameErrorCorrection(),
      this.demoImportErrorCorrection(),
      this.demoIndentationErrorCorrection(),
      this.demoMultipleErrorCorrection(),
      this.demoComplexErrorScenario(),
    ];

    const results: DemoResult[] = [];
    
    for (const demo of demos) {
      try {
        const result = await demo;
        results.push(result);
        this.printDemoResult(result);
      } catch (error) {
        console.error(`Demo failed: ${error}`);
      }
    }

    this.printSummary(results);
    return results;
  }

  /**
   * Demo 1: Syntax Error Correction
   */
  private async demoSyntaxErrorCorrection(): Promise<DemoResult> {
    const testName = 'Syntax Error Correction';
    console.log(`📍 ${testName}`);

    const originalCode = `
def check_number(x):
    if x = 5:  # Should be == not =
        return "Five"
    elif x > 10:
        return "Big"
    else:
        return "Small"

result = check_number(5)
print(result)
`.trim();

    const errors: StructuredValidationError[] = [{
      id: 'syntax-error-1',
      type: ErrorType.SYNTAX_ERROR,
      severity: ErrorSeverity.HIGH,
      message: 'invalid syntax',
      originalMessage: 'invalid syntax',
      location: { line: 2, column: 8 },
      context: {
        sourceCode: originalCode,
        errorLine: '    if x = 5:',
        surroundingLines: ['def check_number(x):', '    if x = 5:', '        return "Five"'],
        functionName: 'check_number',
      },
      suggestedFixes: [],
      timestamp: new Date(),
    }];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Demo 2: Name Error Correction
   */
  private async demoNameErrorCorrection(): Promise<DemoResult> {
    const testName = 'Name Error Correction';
    console.log(`📍 ${testName}`);

    const originalCode = `
def calculate_area():
    length = 10
    width = 5
    area = length * width
    print(f"Area: {area}")
    print(f"Perimeter: {perimeter}")  # perimeter is not defined

calculate_area()
`.trim();

    const errors: StructuredValidationError[] = [{
      id: 'name-error-1',
      type: ErrorType.NAME_ERROR,
      severity: ErrorSeverity.HIGH,
      message: "name 'perimeter' is not defined",
      originalMessage: "name 'perimeter' is not defined",
      location: { line: 6, column: 25 },
      context: {
        sourceCode: originalCode,
        errorLine: '    print(f"Perimeter: {perimeter}")',
        surroundingLines: ['    print(f"Area: {area}")', '    print(f"Perimeter: {perimeter}")'],
        functionName: 'calculate_area',
      },
      suggestedFixes: [],
      timestamp: new Date(),
    }];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Demo 3: Import Error Correction
   */
  private async demoImportErrorCorrection(): Promise<DemoResult> {
    const testName = 'Import Error Correction';
    console.log(`📍 ${testName}`);

    const originalCode = `
def calculate_circle_area(radius):
    area = math.pi * radius ** 2  # math module not imported
    return area

def main():
    radius = 5
    area = calculate_circle_area(radius)
    print(f"Circle area: {area}")

main()
`.trim();

    const errors: StructuredValidationError[] = [{
      id: 'name-error-1',
      type: ErrorType.NAME_ERROR,
      severity: ErrorSeverity.HIGH,
      message: "name 'math' is not defined",
      originalMessage: "name 'math' is not defined",
      location: { line: 2, column: 11 },
      context: {
        sourceCode: originalCode,
        errorLine: '    area = math.pi * radius ** 2',
        surroundingLines: ['def calculate_circle_area(radius):', '    area = math.pi * radius ** 2', '    return area'],
        functionName: 'calculate_circle_area',
      },
      suggestedFixes: [],
      timestamp: new Date(),
    }];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Demo 4: Indentation Error Correction
   */
  private async demoIndentationErrorCorrection(): Promise<DemoResult> {
    const testName = 'Indentation Error Correction';
    console.log(`📍 ${testName}`);

    const originalCode = `
def process_numbers(numbers):
    for num in numbers:
        if num > 0:
            print(f"Positive: {num}")
        elif num < 0:
        print(f"Negative: {num}")  # Incorrect indentation
        else:
            print("Zero")

process_numbers([1, -2, 0, 3])
`.trim();

    const errors: StructuredValidationError[] = [{
      id: 'indentation-error-1',
      type: ErrorType.INDENTATION_ERROR,
      severity: ErrorSeverity.HIGH,
      message: 'expected an indented block',
      originalMessage: 'expected an indented block',
      location: { line: 7, column: 1 },
      context: {
        sourceCode: originalCode,
        errorLine: '        print(f"Negative: {num}")',
        surroundingLines: ['        elif num < 0:', '        print(f"Negative: {num}")', '        else:'],
        functionName: 'process_numbers',
      },
      suggestedFixes: [],
      timestamp: new Date(),
    }];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Demo 5: Multiple Error Correction
   */
  private async demoMultipleErrorCorrection(): Promise<DemoResult> {
    const testName = 'Multiple Error Correction';
    console.log(`📍 ${testName}`);

    const originalCode = `
def fibonacci(n):
    if n <= 1:
        return n
    else:
        return fibonacci(n-1) + fibonacci(n-2)

# Test the function
print(fibonacci(undefined_var))  # NameError: undefined_var not defined
if x = 5:  # SyntaxError: invalid syntax
    print("x is 5")
`.trim();

    const errors: StructuredValidationError[] = [
      {
        id: 'name-error-1',
        type: ErrorType.NAME_ERROR,
        severity: ErrorSeverity.HIGH,
        message: "name 'undefined_var' is not defined",
        originalMessage: "name 'undefined_var' is not defined",
        location: { line: 8, column: 20 },
        context: {
          sourceCode: originalCode,
          errorLine: 'print(fibonacci(undefined_var))',
          surroundingLines: ['# Test the function', 'print(fibonacci(undefined_var))'],
          functionName: null,
        },
        suggestedFixes: [],
        timestamp: new Date(),
      },
      {
        id: 'syntax-error-1',
        type: ErrorType.SYNTAX_ERROR,
        severity: ErrorSeverity.HIGH,
        message: 'invalid syntax',
        originalMessage: 'invalid syntax',
        location: { line: 9, column: 5 },
        context: {
          sourceCode: originalCode,
          errorLine: 'if x = 5:',
          surroundingLines: ['print(fibonacci(undefined_var))', 'if x = 5:', 'print("x is 5")'],
          functionName: null,
        },
        suggestedFixes: [],
        timestamp: new Date(),
      },
    ];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Demo 6: Complex Error Scenario
   */
  private async demoComplexErrorScenario(): Promise<DemoResult> {
    const testName = 'Complex Error Scenario';
    console.log(`📍 ${testName}`);

    const originalCode = `
class Calculator:
    def __init__(self):
        self.history = []
    
    def add(self, a, b):
        result = a + b
        self.history.append(f"{a} + {b} = {result}")
        return result
    
    def divide(self, a, b):
        if b = 0:  # Syntax error: should be ==
            raise ValueError("Cannot divide by zero")
        result = a / b
        self.history.append(f"{a} / {b} = {result}")
        return result
    
    def get_last_operation(self):
        if len(self.history) > 0:
            return self.history[-1]
        else:
            return "No operations yet"

calc = Calculator()
print(calc.add(5, 3))
print(calc.divide(10, 2))
print(calc.divide(10, zero_value))  # NameError: zero_value not defined
print(calc.get_last_operation())
`.trim();

    const errors: StructuredValidationError[] = [
      {
        id: 'syntax-error-1',
        type: ErrorType.SYNTAX_ERROR,
        severity: ErrorSeverity.HIGH,
        message: 'invalid syntax',
        originalMessage: 'invalid syntax',
        location: { line: 11, column: 13 },
        context: {
          sourceCode: originalCode,
          errorLine: '        if b = 0:',
          surroundingLines: ['    def divide(self, a, b):', '        if b = 0:', '            raise ValueError("Cannot divide by zero")'],
          functionName: 'divide',
        },
        suggestedFixes: [],
        timestamp: new Date(),
      },
      {
        id: 'name-error-1',
        type: ErrorType.NAME_ERROR,
        severity: ErrorSeverity.HIGH,
        message: "name 'zero_value' is not defined",
        originalMessage: "name 'zero_value' is not defined",
        location: { line: 24, column: 25 },
        context: {
          sourceCode: originalCode,
          errorLine: 'print(calc.divide(10, zero_value))',
          surroundingLines: ['print(calc.divide(10, 2))', 'print(calc.divide(10, zero_value))', 'print(calc.get_last_operation())'],
          functionName: null,
        },
        suggestedFixes: [],
        timestamp: new Date(),
      },
    ];

    return await this.runCorrectionDemo(testName, originalCode, errors);
  }

  /**
   * Run a single correction demonstration
   */
  private async runCorrectionDemo(
    testName: string,
    originalCode: string,
    errors: StructuredValidationError[],
  ): Promise<DemoResult> {
    
    const startTime = Date.now();
    
    try {
      const correctionConfig = {
        maxAttempts: 3,
        enableASTCorrection: true,
        enableLLMCorrection: false,
        timeoutMs: 10000,
      };

      const result = await this.corrector.correctErrors(originalCode, errors, correctionConfig);
      const correctionTime = Date.now() - startTime;

      return {
        testName,
        originalCode,
        errors,
        correctedCode: result.correctedCode,
        success: result.success,
        appliedFixes: result.appliedFixes.length,
        astTransformations: result.astTransformations.length,
        correctionTime,
      };

    } catch (error) {
      console.error(`Error in ${testName}:`, error);
      
      return {
        testName,
        originalCode,
        errors,
        correctedCode: originalCode,
        success: false,
        appliedFixes: 0,
        astTransformations: 0,
        correctionTime: Date.now() - startTime,
      };
    }
  }

  /**
   * Print the result of a single demo
   */
  private printDemoResult(result: DemoResult): void {
    console.log(`\n📊 ${result.testName} Results:`);
    console.log(`   Success: ${result.success ? '✅' : '❌'}`);
    console.log(`   Applied Fixes: ${result.appliedFixes}`);
    console.log(`   AST Transformations: ${result.astTransformations}`);
    console.log(`   Correction Time: ${result.correctionTime}ms`);
    
    if (result.success) {
      console.log('\n📝 Original Code:');
      console.log(this.formatCode(result.originalCode));
      console.log('\n✨ Corrected Code:');
      console.log(this.formatCode(result.correctedCode));
    }
    
    console.log('\n' + '─'.repeat(80) + '\n');
  }

  /**
   * Print summary of all demos
   */
  private printSummary(results: DemoResult[]): void {
    const successful = results.filter(r => r.success).length;
    const totalFixes = results.reduce((sum, r) => sum + r.appliedFixes, 0);
    const totalTransformations = results.reduce((sum, r) => sum + r.astTransformations, 0);
    const avgTime = results.reduce((sum, r) => sum + r.correctionTime, 0) / results.length;

    console.log('🎯 AST Error Correction System Demo Summary');
    console.log('═'.repeat(50));
    console.log(`📈 Success Rate: ${successful}/${results.length} (${(successful/results.length*100).toFixed(1)}%)`);
    console.log(`🔧 Total Fixes Applied: ${totalFixes}`);
    console.log(`🌳 Total AST Transformations: ${totalTransformations}`);
    console.log(`⏱️  Average Correction Time: ${avgTime.toFixed(0)}ms`);
    console.log('═'.repeat(50));

    if (successful === results.length) {
      console.log('🎉 All demonstrations completed successfully!');
      console.log('✅ The AST-guided error correction system is working correctly.');
    } else {
      console.log(`⚠️  ${results.length - successful} demonstrations failed.`);
      console.log('🔍 Review the failed cases for potential improvements.');
    }
  }

  /**
   * Format code for display
   */
  private formatCode(code: string): string {
    return code.split('\n').map((line, index) => 
      `${(index + 1).toString().padStart(2, ' ')}: ${line}`,
    ).join('\n');
  }

  /**
   * Demonstrate feedback loop integration
   */
  async demoFeedbackLoop(): Promise<void> {
    console.log('\n🔄 Feedback Loop Integration Demo\n');

    const problem: BenchmarkProblem = {
      id: 'feedback-demo-1',
      benchmark: 'humaneval',
      title: 'Feedback Loop Demo',
      description: 'Demo problem for testing feedback loop',
      prompt: 'Write a function that demonstrates error correction',
      language: 'python',
      difficulty: 'medium',
      category: 'demo',
      testCases: [],
      metadata: {
        originalId: 'feedback-demo-1',
        tags: ['feedback', 'demo'],
        timeLimit: 5000,
        memoryLimit: 128,
      },
    };

    const initialSolution: GolemSolution = {
      problemId: 'feedback-demo-1',
      benchmark: 'humaneval',
      language: 'python',
      solutionCode: `
def factorial(n):
    if n <= 1:
        return 1
    else:
        return n * factorial(n-1)

print(factorial(undefined_var))  # Name error
`.trim(),
      approach: 'hybrid',
      confidence: 0.8,
      engineUsed: 'demo-engine',
      generationTime: 1000,
      metadata: {
        attempts: 1,
        fallbackUsed: false,
        engineHealth: { 'demo-engine': 1.0 },
        transformationSteps: ['Initial solution generated'],
      },
    };

    const feedbackConfig = {
      maxIterations: 5,
      maxErrorsPerIteration: 2,
      enableProgressiveCorrection: true,
      enableErrorPrioritization: true,
      enableLearningFromFailures: true,
      timeoutPerIteration: 10000,
      confidenceThreshold: 0.7,
    };

    try {
      console.log('🚀 Starting feedback loop...');
      console.log('📝 Initial solution:');
      console.log(this.formatCode(initialSolution.solutionCode));

      const result = await this.feedbackLoop.executeFeedbackLoop(
        problem,
        initialSolution,
        feedbackConfig,
      );

      console.log('\n📊 Feedback Loop Results:');
      console.log(`   Success: ${result.success ? '✅' : '❌'}`);
      console.log(`   Iterations: ${result.iterations.length}`);
      console.log(`   Total Corrections: ${result.totalCorrections}`);
      console.log(`   Total Time: ${result.totalTime}ms`);

      if (result.success) {
        console.log('\n✨ Final corrected solution:');
        console.log(this.formatCode(result.finalSolution.solutionCode));
      }

      // Show iteration details
      console.log('\n📋 Iteration Details:');
      for (const iteration of result.iterations) {
        console.log(`   Iteration ${iteration.iterationNumber}:`);
        console.log(`     Errors Fixed: ${iteration.errorsFixed}`);
        console.log(`     Errors Remaining: ${iteration.errorsRemaining}`);
        console.log(`     Progress Made: ${iteration.progressMade ? '✅' : '❌'}`);
        console.log(`     Time: ${iteration.iterationTime}ms`);
      }

    } catch (error) {
      console.error('Feedback loop demo failed:', error);
    }
  }
}

/**
 * Main demo runner
 */
export async function runASTCorrectionDemo(): Promise<void> {
  const demo = new ASTCorrectionDemo();
  
  try {
    // Run all basic demos
    await demo.runAllDemos();
    
    // Run feedback loop demo
    await demo.demoFeedbackLoop();
    
  } catch (error) {
    console.error('Demo execution failed:', error);
  }
}

// Export for use in other modules
// Export removed to avoid redeclaration - class already exported above

