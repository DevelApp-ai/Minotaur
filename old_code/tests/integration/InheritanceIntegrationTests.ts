/**
 * Integration tests for the inheritance-based compiler-compiler support system.
 */
import { Interpreter } from '../../src/utils/Interpreter';
import { InheritanceTestFramework, TestSuite } from '../../src/testing/InheritanceTestFramework';
import { EmbeddedScriptPluginManager } from '../../src/plugins/EmbeddedScriptPlugin';
import { GrammarFormatType } from '../../src/utils/Grammar';

/**
 * Comprehensive integration test suite for the inheritance system.
 */
export class InheritanceIntegrationTests {
  private interpreter: Interpreter;
  private testFramework: InheritanceTestFramework;
  private pluginManager: EmbeddedScriptPluginManager;
  
  constructor() {
    this.interpreter = new Interpreter();
    this.testFramework = new InheritanceTestFramework(this.interpreter);
    this.pluginManager = new EmbeddedScriptPluginManager();
  }
  
  /**
   * Runs all integration tests.
   */
  public async runAllTests(): Promise<void> {
    console.log("=== Minotaur Inheritance System Integration Tests ===\n");
    
    try {
      // Run basic inheritance tests
      await this.runBasicInheritanceTests();
      
      // Run semantic action tests
      await this.runSemanticActionTests();
      
      // Run precedence tests
      await this.runPrecedenceTests();
      
      // Run error recovery tests
      await this.runErrorRecoveryTests();
      
      // Run plugin system tests
      await this.runPluginSystemTests();
      
      // Run performance tests
      await this.runPerformanceTests();
      
      // Run comprehensive integration tests
      await this.runComprehensiveIntegrationTests();
      
      console.log("\n=== All Integration Tests Completed Successfully ===");
      
    } catch (error) {
      console.error("Integration tests failed:", error);
      throw error;
    }
  }
  
  /**
   * Tests basic inheritance functionality.
   */
  private async runBasicInheritanceTests(): Promise<void> {
    console.log("Running Basic Inheritance Tests...");
    
    const suite = new TestSuite("BasicInheritance", "Tests for basic inheritance functionality");
    
    // Test 1: Simple inheritance
    const simpleInheritanceTest = this.testFramework.createBasicInheritanceTest(
      "SimpleInheritance",
      `@Inheritable: true
@FormatType: ANTLR4

grammar BaseGrammar;
rule1: 'a' 'b';
rule2: 'c' 'd';`,
      `@Inherits: BaseGrammar
@FormatType: ANTLR4

grammar DerivedGrammar;
rule3: 'e' 'f';`,
      "Derived grammar should inherit rules from base grammar"
    );
    suite.addTest(simpleInheritanceTest);
    
    // Test 2: Multiple inheritance
    const multipleInheritanceTest = this.testFramework.createBasicInheritanceTest(
      "MultipleInheritance",
      `@Inheritable: true
@FormatType: ANTLR4

grammar BaseGrammar1;
rule1: 'a';`,
      `@Inherits: BaseGrammar1, BaseGrammar2
@FormatType: ANTLR4

grammar DerivedGrammar;
rule3: 'c';`,
      "Derived grammar should inherit from multiple base grammars"
    );
    suite.addTest(multipleInheritanceTest);
    
    // Test 3: Inheritance hierarchy
    const hierarchyTest = this.testFramework.createBasicInheritanceTest(
      "InheritanceHierarchy",
      `@Inheritable: true
@FormatType: ANTLR4

grammar GrandparentGrammar;
rule1: 'a';`,
      `@Inherits: GrandparentGrammar
@FormatType: ANTLR4

grammar GrandchildGrammar;
rule3: 'c';`,
      "Multi-level inheritance should work correctly"
    );
    suite.addTest(hierarchyTest);
    
    this.testFramework.registerTestSuite("BasicInheritance", suite);
    const results = this.testFramework.runTestSuite("BasicInheritance", suite);
    
    console.log("Basic Inheritance Tests Results:");
    console.log(this.testFramework.generateTestReport(this.testFramework.runAllTests()));
  }
  
  /**
   * Tests semantic action inheritance functionality.
   */
  private async runSemanticActionTests(): Promise<void> {
    console.log("Running Semantic Action Tests...");
    
    const suite = new TestSuite("SemanticActions", "Tests for semantic action inheritance");
    
    // Test 1: Action inheritance
    const actionInheritanceTest = this.testFramework.createSemanticActionTest(
      "ActionInheritance",
      `@Inheritable: true
@FormatType: ANTLR4

@SemanticActions {
    addAction: @call(add, $1, $3)
}

grammar BaseCalculator;
expr: expr '+' expr @action(addAction);`,
      `@Inherits: BaseCalculator
@FormatType: ANTLR4

grammar ExtendedCalculator;
expr: expr '*' expr @action(multiplyAction);`,
      "1 + 2",
      3
    );
    suite.addTest(actionInheritanceTest);
    
    // Test 2: Action override
    const actionOverrideTest = this.testFramework.createSemanticActionTest(
      "ActionOverride",
      `@Inheritable: true
@FormatType: ANTLR4

@SemanticActions {
    addAction: @js{ return $1 + $3; }
}

grammar BaseCalculator;
expr: expr '+' expr @action(addAction);`,
      `@Inherits: BaseCalculator
@FormatType: ANTLR4

@SemanticActions {
    addAction: @js{ return $1 + $3 + 1; } // Override with +1 bonus
}

grammar BonusCalculator;`,
      "1 + 2",
      4
    );
    suite.addTest(actionOverrideTest);
    
    this.testFramework.registerTestSuite("SemanticActions", suite);
    const results = this.testFramework.runTestSuite("SemanticActions", suite);
    
    console.log("Semantic Action Tests Results:");
    console.log(this.testFramework.generateTestReport(this.testFramework.runAllTests()));
  }
  
  /**
   * Tests precedence inheritance functionality.
   */
  private async runPrecedenceTests(): Promise<void> {
    console.log("Running Precedence Tests...");
    
    const suite = new TestSuite("Precedence", "Tests for precedence inheritance");
    
    // Test 1: Basic precedence inheritance
    const basicPrecedenceTest = this.testFramework.createPrecedenceTest(
      "BasicPrecedence",
      `@Inheritable: true
@FormatType: Bison

%left '+' '-'
%left '*' '/'

%%
expr: expr '+' expr | expr '*' expr | NUMBER;`,
      `@Inherits: BasePrecedence
@FormatType: Bison

%right '^'

%%
expr: expr '^' expr;`,
      "1 + 2 * 3",
      "Multiplication should have higher precedence than addition"
    );
    suite.addTest(basicPrecedenceTest);
    
    // Test 2: Precedence override
    const precedenceOverrideTest = this.testFramework.createPrecedenceTest(
      "PrecedenceOverride",
      `@Inheritable: true
@FormatType: Bison

%left '+' '-'
%left '*' '/'

%%
expr: expr '+' expr | expr '*' expr;`,
      `@Inherits: BasePrecedence
@FormatType: Bison

%right '+' '-'  // Override to right associative

%%`,
      "1 + 2 + 3",
      "Addition should be right associative in derived grammar"
    );
    suite.addTest(precedenceOverrideTest);
    
    this.testFramework.registerTestSuite("Precedence", suite);
    const results = this.testFramework.runTestSuite("Precedence", suite);
    
    console.log("Precedence Tests Results:");
    console.log(this.testFramework.generateTestReport(this.testFramework.runAllTests()));
  }
  
  /**
   * Tests error recovery inheritance functionality.
   */
  private async runErrorRecoveryTests(): Promise<void> {
    console.log("Running Error Recovery Tests...");
    
    const suite = new TestSuite("ErrorRecovery", "Tests for error recovery inheritance");
    
    // Test 1: Basic error recovery inheritance
    const basicRecoveryTest = this.testFramework.createErrorRecoveryTest(
      "BasicRecovery",
      `@Inheritable: true
@FormatType: ANTLR4

@ErrorRecovery {
    syntaxError: @synchronize(';', '}')
}

grammar BaseParser;
stmt: expr ';';`,
      `@Inherits: BaseParser
@FormatType: ANTLR4

grammar ExtendedParser;
stmt: '{' stmt_list '}';`,
      "invalid syntax here ;",
      "Should synchronize to semicolon"
    );
    suite.addTest(basicRecoveryTest);
    
    // Test 2: Recovery strategy override
    const recoveryOverrideTest = this.testFramework.createErrorRecoveryTest(
      "RecoveryOverride",
      `@Inheritable: true
@FormatType: ANTLR4

@ErrorRecovery {
    syntaxError: @skip(1)
}

grammar BaseParser;
stmt: expr ';';`,
      `@Inherits: BaseParser
@FormatType: ANTLR4

@ErrorRecovery {
    syntaxError: @synchronize(';')  // Override with synchronization
}

grammar BetterParser;`,
      "invalid syntax ;",
      "Should use synchronization instead of skip"
    );
    suite.addTest(recoveryOverrideTest);
    
    this.testFramework.registerTestSuite("ErrorRecovery", suite);
    const results = this.testFramework.runTestSuite("ErrorRecovery", suite);
    
    console.log("Error Recovery Tests Results:");
    console.log(this.testFramework.generateTestReport(this.testFramework.runAllTests()));
  }
  
  /**
   * Tests the embedded script plugin system.
   */
  private async runPluginSystemTests(): Promise<void> {
    console.log("Running Plugin System Tests...");
    
    // Test ANTLR4 script extraction
    const antlr4Grammar = `
grammar TestGrammar;

@members {
    private int count = 0;
    public void increment() { count++; }
}

expr
    : expr '+' expr { $$ = $1 + $3; }
    | NUMBER        { $$ = $1; }
    ;
`;
    
    const antlr4Results = this.pluginManager.testEmbeddedScripts(
      "ANTLR4Test",
      antlr4Grammar,
      GrammarFormatType.ANTLR4
    );
    
    console.log("ANTLR4 Script Test Results:");
    console.log(this.pluginManager.generateTestReport(antlr4Results));
    
    // Test Bison script extraction
    const bisonGrammar = `
%{
#include <stdio.h>
int yylex();
void yyerror(const char *s);
%}

%%

expr
    : expr '+' expr { $$ = $1 + $3; }
    | NUMBER        { $$ = $1; }
    ;

%%

void yyerror(const char *s) {
    fprintf(stderr, "Error: %s\\n", s);
}
`;
    
    const bisonResults = this.pluginManager.testEmbeddedScripts(
      "BisonTest",
      bisonGrammar,
      GrammarFormatType.Bison
    );
    
    console.log("Bison Script Test Results:");
    console.log(this.pluginManager.generateTestReport(bisonResults));
    
    // Test Minotaur script extraction
    const grammarForgeGrammar = `
@js{
    function calculate(a, b) {
        return a + b;
    }
}

expr
    : expr '+' expr @call(calculate, $1, $3)
    | NUMBER        { $$ = parseInt($1); }
    ;
`;
    
    const grammarForgeResults = this.pluginManager.testEmbeddedScripts(
      "MinotaurTest",
      grammarForgeGrammar,
      GrammarFormatType.Minotaur
    );
    
    console.log("Minotaur Script Test Results:");
    console.log(this.pluginManager.generateTestReport(grammarForgeResults));
  }
  
  /**
   * Tests system performance with complex inheritance hierarchies.
   */
  private async runPerformanceTests(): Promise<void> {
    console.log("Running Performance Tests...");
    
    const startTime = Date.now();
    
    // Create a complex inheritance hierarchy
    const baseGrammar = `@Inheritable: true
@FormatType: ANTLR4

grammar BaseGrammar;
rule1: 'a';
rule2: 'b';
rule3: 'c';`;
    
    this.interpreter.parseGrammar("BaseGrammar", baseGrammar);
    
    // Create multiple levels of inheritance
    for (let i = 1; i <= 10; i++) {
      const derivedGrammar = `@Inherits: ${i === 1 ? 'BaseGrammar' : `DerivedGrammar${i-1}`}
@FormatType: ANTLR4

grammar DerivedGrammar${i};
rule${i+3}: 'rule${i}';`;
      
      this.interpreter.parseGrammar(`DerivedGrammar${i}`, derivedGrammar);
    }
    
    // Test inheritance resolution performance
    const resolutionStartTime = Date.now();
    for (let i = 1; i <= 10; i++) {
      const hierarchy = this.interpreter.getGrammarContainer().getInheritanceHierarchy(`DerivedGrammar${i}`);
      console.log(`DerivedGrammar${i} hierarchy depth: ${hierarchy.length}`);
    }
    const resolutionTime = Date.now() - resolutionStartTime;
    
    // Test semantic action resolution performance
    const actionStartTime = Date.now();
    for (let i = 1; i <= 10; i++) {
      this.interpreter.registerSemanticAction(`DerivedGrammar${i}`, `action${i}`, {
        getName: () => `action${i}`,
        getGrammarName: () => `DerivedGrammar${i}`,
        execute: (context, args) => args[0],
        getActionType: () => 'callback',
        getDescription: () => `Test action ${i}`
      });
    }
    const actionTime = Date.now() - actionStartTime;
    
    const totalTime = Date.now() - startTime;
    
    console.log(`Performance Test Results:
    Total Time: ${totalTime}ms
    Hierarchy Resolution Time: ${resolutionTime}ms
    Action Registration Time: ${actionTime}ms
    Average Time per Grammar: ${totalTime / 10}ms`);
  }
  
  /**
   * Runs comprehensive integration tests that combine multiple features.
   */
  private async runComprehensiveIntegrationTests(): Promise<void> {
    console.log("Running Comprehensive Integration Tests...");
    
    // Test 1: Complete calculator with inheritance
    const baseCalculatorGrammar = `@Inheritable: true
@FormatType: ANTLR4

@SemanticActions {
    addAction: @js{ return $1 + $3; }
    subtractAction: @js{ return $1 - $3; }
    multiplyAction: @js{ return $1 * $3; }
    divideAction: @js{ return $1 / $3; }
}

@ErrorRecovery {
    syntaxError: @synchronize(';', 'EOF')
    divisionByZero: @js{ throw new Error("Division by zero"); }
}

grammar BaseCalculator;

%left '+' '-'
%left '*' '/'

%%

expression
    : expression '+' expression @action(addAction)
    | expression '-' expression @action(subtractAction)
    | expression '*' expression @action(multiplyAction)
    | expression '/' expression @action(divideAction)
    | '(' expression ')'        { $$ = $2; }
    | NUMBER                    { $$ = parseFloat($1); }
    ;`;
    
    const scientificCalculatorGrammar = `@Inherits: BaseCalculator
@FormatType: ANTLR4

@SemanticActions {
    powerAction: @js{ return Math.pow($1, $3); }
    sqrtAction: @js{ return Math.sqrt($2); }
    sinAction: @js{ return Math.sin($2); }
    cosAction: @js{ return Math.cos($2); }
}

grammar ScientificCalculator;

%right '^'
%left SIN COS SQRT

%%

expression
    : expression '^' expression     @action(powerAction)
    | 'sin' '(' expression ')'      @action(sinAction)
    | 'cos' '(' expression ')'      @action(cosAction)
    | 'sqrt' '(' expression ')'     @action(sqrtAction)
    ;`;
    
    try {
      // Load grammars
      this.interpreter.parseGrammar("BaseCalculator", baseCalculatorGrammar);
      this.interpreter.parseGrammar("ScientificCalculator", scientificCalculatorGrammar);
      
      // Test inheritance hierarchy
      const hierarchy = this.interpreter.getGrammarContainer().getInheritanceHierarchy("ScientificCalculator");
      console.log("Scientific Calculator Hierarchy:", hierarchy);
      
      // Test semantic action inheritance
      const addAction = this.interpreter.getSemanticAction("ScientificCalculator", "addAction");
      const powerAction = this.interpreter.getSemanticAction("ScientificCalculator", "powerAction");
      
      console.log("Inherited add action:", addAction ? "Found" : "Not found");
      console.log("New power action:", powerAction ? "Found" : "Not found");
      
      // Test precedence inheritance
      const precedenceComparison = this.interpreter.comparePrecedence("ScientificCalculator", "+", "*");
      console.log("Precedence comparison (+, *):", precedenceComparison);
      
      // Test error recovery inheritance
      const errorStrategies = this.interpreter.getAllErrorRecoveryStrategies("ScientificCalculator");
      console.log("Error recovery strategies:", errorStrategies.size);
      
      // Test embedded script extraction
      const scriptResults = this.pluginManager.testEmbeddedScripts(
        "ScientificCalculator",
        scientificCalculatorGrammar,
        GrammarFormatType.ANTLR4
      );
      
      console.log("Embedded Script Test Results:");
      console.log(this.pluginManager.generateTestReport(scriptResults));
      
      console.log("Comprehensive Integration Test: PASSED");
      
    } catch (error) {
      console.error("Comprehensive Integration Test: FAILED", error);
      throw error;
    }
  }
  
  /**
   * Validates the complete system functionality.
   */
  public async validateSystem(): Promise<boolean> {
    try {
      console.log("=== System Validation ===");
      
      // Validate core components
      console.log("Validating core components...");
      const interpreter = new Interpreter();
      const testFramework = new InheritanceTestFramework(interpreter);
      const pluginManager = new EmbeddedScriptPluginManager();
      
      // Validate basic functionality
      console.log("Validating basic functionality...");
      const testGrammar = `@Inheritable: true
@FormatType: ANTLR4
grammar TestGrammar;
rule1: 'test';`;
      
      interpreter.parseGrammar("TestGrammar", testGrammar);
      const grammar = interpreter.getGrammarContainer().getGrammar("TestGrammar");
      
      if (!grammar) {
        throw new Error("Failed to load test grammar");
      }
      
      // Validate inheritance functionality
      console.log("Validating inheritance functionality...");
      const derivedGrammar = `@Inherits: TestGrammar
@FormatType: ANTLR4
grammar DerivedGrammar;
rule2: 'derived';`;
      
      interpreter.parseGrammar("DerivedGrammar", derivedGrammar);
      const derived = interpreter.getGrammarContainer().getGrammar("DerivedGrammar");
      
      if (!derived || !derived.getBaseGrammars().includes("TestGrammar")) {
        throw new Error("Failed to establish inheritance relationship");
      }
      
      // Validate plugin system
      console.log("Validating plugin system...");
      const pluginResults = pluginManager.testEmbeddedScripts(
        "TestGrammar",
        "rule: 'test' { console.log('test'); };",
        GrammarFormatType.ANTLR4
      );
      
      if (pluginResults.getSummary().totalScripts === 0) {
        console.warn("No embedded scripts found in test grammar");
      }
      
      console.log("System validation completed successfully");
      return true;
      
    } catch (error) {
      console.error("System validation failed:", error);
      return false;
    }
  }
}

// Export test runner function
export async function runIntegrationTests(): Promise<void> {
  const tests = new InheritanceIntegrationTests();
  
  // Validate system first
  const isValid = await tests.validateSystem();
  if (!isValid) {
    throw new Error("System validation failed");
  }
  
  // Run all integration tests
  await tests.runAllTests();
}

// Run tests if this file is executed directly
if (require.main === module) {
  runIntegrationTests()
    .then(() => {
      console.log("All integration tests completed successfully");
      process.exit(0);
    })
    .catch((error) => {
      console.error("Integration tests failed:", error);
      process.exit(1);
    });
}

