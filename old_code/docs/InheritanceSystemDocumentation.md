# Minotaur Inheritance-Based Compiler-Compiler Support System

**Author:** Manus AI  
**Version:** 1.0.0  
**Date:** December 2024

## Table of Contents

1. [Introduction](#introduction)
2. [System Architecture](#system-architecture)
3. [Core Components](#core-components)
4. [Inheritance Mechanisms](#inheritance-mechanisms)
5. [Semantic Action Framework](#semantic-action-framework)
6. [Precedence and Associativity System](#precedence-and-associativity-system)
7. [Error Recovery Framework](#error-recovery-framework)
8. [Testing and Validation](#testing-and-validation)
9. [Plugin System](#plugin-system)
10. [Usage Examples](#usage-examples)
11. [API Reference](#api-reference)
12. [Best Practices](#best-practices)
13. [Troubleshooting](#troubleshooting)
14. [Future Enhancements](#future-enhancements)

## Introduction

The Minotaur Inheritance-Based Compiler-Compiler Support System represents a revolutionary approach to grammar development and maintenance in the field of compiler construction. This comprehensive system enables grammar developers to create sophisticated inheritance hierarchies that promote code reuse, maintainability, and consistency across different grammar formats including ANTLR v4, Bison/Flex, and Yacc/Lex.

Traditional grammar development often suffers from code duplication, inconsistent semantic actions, and maintenance challenges when dealing with multiple related grammars. The inheritance system addresses these fundamental issues by providing a robust framework that allows grammars to inherit rules, semantic actions, precedence definitions, and error recovery strategies from base grammars, while maintaining the flexibility to override and extend inherited behavior as needed.

The system's design philosophy centers around the principle of "write once, inherit everywhere," enabling developers to define common grammar patterns, semantic behaviors, and parsing strategies in base grammars that can be seamlessly inherited by derived grammars. This approach not only reduces development time but also ensures consistency across related language implementations and simplifies maintenance tasks.

## System Architecture

The inheritance system is built upon a modular architecture that separates concerns while maintaining tight integration between components. The architecture consists of several key layers, each responsible for specific aspects of the inheritance functionality.

### Core Layer

The core layer provides the fundamental infrastructure for grammar inheritance, including the `Grammar` class extensions, `InheritanceResolver`, and `GrammarContainer` enhancements. This layer handles the basic mechanics of inheritance relationships, dependency resolution, and circular dependency detection.

The `Grammar` class has been extended with inheritance-specific properties and methods that track base grammars, inheritance directives, and format-specific information. The inheritance resolver implements sophisticated algorithms for resolving inheritance hierarchies, ensuring that dependencies are loaded in the correct order and that circular dependencies are detected and reported.

### Management Layer

The management layer consists of specialized managers for different aspects of inheritance: `SemanticActionManager`, `PrecedenceManager`, and `ErrorRecoveryManager`. Each manager provides inheritance-aware functionality for its respective domain, implementing caching mechanisms, conflict resolution strategies, and performance optimizations.

These managers work in concert to provide a unified inheritance experience while maintaining separation of concerns. They implement common patterns for inheritance resolution, caching, and cleanup, ensuring consistent behavior across different types of inherited elements.

### Integration Layer

The integration layer, primarily represented by the enhanced `Interpreter` class, provides a unified interface for accessing all inheritance functionality. This layer abstracts the complexity of the underlying managers and provides convenient methods for grammar developers to work with inherited elements.

The interpreter serves as the primary entry point for most inheritance operations, coordinating between different managers and ensuring that inheritance relationships are properly maintained and resolved.

### Plugin Layer

The plugin layer enables extensibility through the embedded script plugin system, allowing for testing and validation of embedded scripts within grammar files. This layer supports multiple execution environments and provides a framework for adding new script types and execution contexts.

## Core Components

### Grammar Class Extensions

The `Grammar` class has been significantly enhanced to support inheritance relationships. The key additions include:

**Inheritance Properties:**
- `baseGrammars`: Array of base grammar names from which this grammar inherits
- `isInheritable`: Boolean flag indicating whether this grammar can be inherited from
- `formatType`: Enumeration specifying the grammar format (ANTLR4, Bison, Yacc, etc.)
- `inheritanceDirectives`: Map of inheritance-specific directives and their values

**Inheritance Methods:**
- `addBaseGrammar(grammarName: string)`: Adds a base grammar to the inheritance hierarchy
- `removeBaseGrammar(grammarName: string)`: Removes a base grammar from the inheritance hierarchy
- `getBaseGrammars()`: Returns the list of base grammars
- `isInheritableGrammar()`: Checks if the grammar can be inherited from
- `getFormatType()`: Returns the grammar format type

The grammar class also includes validation methods that ensure inheritance relationships are valid and that format compatibility is maintained across the inheritance hierarchy.

### InheritanceResolver

The `InheritanceResolver` is responsible for resolving complex inheritance relationships and ensuring that all dependencies are properly satisfied. This component implements several critical algorithms:

**Dependency Resolution Algorithm:**
The resolver uses a topological sorting algorithm to determine the correct loading order for grammars in an inheritance hierarchy. This ensures that base grammars are always loaded before derived grammars, preventing dependency-related errors.

**Circular Dependency Detection:**
The resolver implements a depth-first search algorithm with cycle detection to identify circular dependencies in inheritance hierarchies. When circular dependencies are detected, detailed error messages are generated to help developers identify and resolve the issues.

**Compatibility Validation:**
The resolver validates that grammars in an inheritance hierarchy are compatible in terms of format types, version requirements, and other constraints. This prevents runtime errors and ensures that inheritance relationships are semantically meaningful.

### GrammarContainer Enhancements

The `GrammarContainer` has been enhanced with inheritance-aware functionality that provides efficient access to inheritance hierarchies and dependency information:

**Hierarchy Management:**
- `getInheritanceHierarchy(grammarName: string)`: Returns the complete inheritance hierarchy for a grammar
- `getAllDependentGrammars(grammarName: string)`: Returns all grammars that depend on the specified grammar
- `validateInheritanceHierarchy(grammarName: string)`: Validates the entire inheritance hierarchy

**Performance Optimizations:**
The container implements sophisticated caching mechanisms that store resolved inheritance hierarchies and dependency information. This significantly improves performance when working with complex inheritance structures, as hierarchy resolution is an expensive operation that benefits greatly from caching.

**Statistics and Monitoring:**
The enhanced container provides detailed statistics about inheritance relationships, including the number of inheritance levels, dependency counts, and cache hit rates. This information is valuable for performance monitoring and optimization.

## Inheritance Mechanisms

### Basic Inheritance

Basic inheritance in Minotaur follows object-oriented principles adapted for grammar development. When a grammar inherits from a base grammar, it automatically gains access to all inheritable elements defined in the base grammar, including rules, semantic actions, precedence definitions, and error recovery strategies.

The inheritance mechanism supports multiple inheritance, allowing a grammar to inherit from multiple base grammars. When conflicts arise due to multiple inheritance, the system provides configurable resolution strategies, including override precedence based on inheritance order and explicit conflict resolution directives.

**Inheritance Declaration:**
Grammars declare their inheritance relationships using special directives within the grammar file:

```
@Inheritable: true
@FormatType: ANTLR4
@Inherits: BaseArithmeticGrammar, BaseExpressionGrammar
```

These directives are parsed during grammar loading and used to establish the inheritance hierarchy. The system validates that all referenced base grammars exist and are compatible with the inheriting grammar.

### Rule Inheritance

Rule inheritance allows derived grammars to inherit production rules from base grammars while maintaining the ability to override or extend inherited rules. The system supports several inheritance patterns for rules:

**Direct Inheritance:**
Rules defined in base grammars are automatically available in derived grammars unless explicitly overridden. This allows for the creation of comprehensive base grammars that define common language constructs.

**Rule Override:**
Derived grammars can override inherited rules by defining rules with the same name. The overriding rule completely replaces the inherited rule, providing full flexibility for customization.

**Rule Extension:**
The system supports rule extension patterns where derived grammars can add alternatives to inherited rules without completely overriding them. This is particularly useful for extending language constructs with additional syntax options.

### Template-Based Inheritance

Template-based inheritance provides a powerful mechanism for creating reusable grammar patterns that can be instantiated with different parameters. This feature is particularly useful for creating grammar families that share common structures but differ in specific details.

Templates can define parameterized rules, semantic actions, and precedence definitions that are instantiated when the template is inherited. The template system supports type-safe parameter substitution and validation, ensuring that template instantiations are syntactically and semantically correct.

## Semantic Action Framework

The semantic action framework provides comprehensive support for inheriting and managing semantic actions across grammar hierarchies. This framework addresses one of the most challenging aspects of grammar inheritance: ensuring that semantic actions remain consistent and functional across inheritance boundaries.

### Action Types and Categories

The framework supports multiple types of semantic actions, each with specific inheritance behaviors:

**Callback Actions:**
Callback actions reference external functions that are registered with the interpreter. These actions inherit by reference, meaning that derived grammars automatically gain access to callback actions defined in base grammars. The framework provides mechanisms for overriding callback actions and for defining action-specific inheritance policies.

**Template Actions:**
Template actions use string templates with parameter substitution to generate code or perform computations. These actions support parameterized inheritance, where derived grammars can provide different parameter values while inheriting the template structure from base grammars.

**Script Actions:**
Script actions contain embedded code in various languages (JavaScript, C, Java, Python, C#). The framework provides language-specific inheritance mechanisms that handle code composition, variable scoping, and execution context management.

**Native Actions:**
Native actions are implemented directly in TypeScript and provide the highest performance for complex semantic operations. These actions support full object-oriented inheritance patterns, including method overriding and super calls.

### Action Resolution and Caching

The semantic action framework implements sophisticated resolution algorithms that efficiently locate and instantiate inherited actions. The resolution process considers the inheritance hierarchy, action override policies, and caching strategies to provide optimal performance.

**Resolution Algorithm:**
The action resolution algorithm traverses the inheritance hierarchy from derived to base grammars, looking for action definitions. When multiple definitions are found, the framework applies resolution policies to determine which action should be used. The algorithm supports both early and late binding strategies, depending on the action type and configuration.

**Caching Strategy:**
Resolved actions are cached using a multi-level caching strategy that balances memory usage with performance. The cache is automatically invalidated when inheritance relationships change or when base grammars are modified, ensuring that cached actions remain consistent with the current grammar state.

**Performance Optimizations:**
The framework includes several performance optimizations, including action pre-compilation, lazy loading of action dependencies, and batch processing of action resolution requests. These optimizations significantly improve the performance of grammars with complex inheritance hierarchies and large numbers of semantic actions.

### Action Composition and Combination

The framework supports advanced action composition patterns that allow multiple inherited actions to be combined in sophisticated ways:

**Sequential Composition:**
Actions can be composed sequentially, where the output of one action becomes the input to the next action. This pattern is useful for creating processing pipelines that transform parse results through multiple stages.

**Parallel Composition:**
Multiple actions can be executed in parallel, with their results combined using configurable combination strategies. This pattern is useful for performing multiple independent computations on the same parse result.

**Conditional Composition:**
Actions can be composed conditionally, where the execution of subsequent actions depends on the results of previous actions. This pattern enables the creation of complex decision trees within semantic actions.

## Precedence and Associativity System

The precedence and associativity system provides comprehensive support for inheriting and managing operator precedence and associativity rules across grammar hierarchies. This system is crucial for ensuring that mathematical and logical expressions are parsed correctly in derived grammars.

### Precedence Rule Management

The precedence management system supports multiple types of precedence rules and provides flexible mechanisms for inheritance and override:

**Level-Based Precedence:**
Operators are assigned numeric precedence levels, with higher numbers indicating higher precedence. The system supports both absolute and relative precedence specifications, allowing for flexible precedence hierarchies.

**Template-Based Precedence:**
The system includes predefined precedence templates for common operator sets, including arithmetic operators, logical operators, comparison operators, and C-style operators. These templates can be inherited and customized as needed.

**Format-Specific Precedence:**
Different grammar formats (ANTLR4, Bison, Yacc) have different conventions for specifying precedence. The system provides format-specific parsers that can extract precedence information from grammar files and convert it to the internal representation.

### Associativity Rule Management

Associativity rules determine how operators of the same precedence level are grouped during parsing. The system supports left, right, and non-associative operators:

**Inheritance Patterns:**
Associativity rules inherit according to the same patterns as precedence rules, with derived grammars able to override inherited associativity specifications. The system validates that associativity changes are semantically meaningful and do not introduce parsing ambiguities.

**Conflict Resolution:**
When multiple inheritance paths provide different associativity rules for the same operator, the system applies configurable conflict resolution strategies. These strategies can prioritize based on inheritance order, explicit precedence declarations, or user-defined resolution policies.

### Precedence Table Generation

The system automatically generates comprehensive precedence tables that show the complete precedence and associativity relationships for all operators in a grammar hierarchy:

**Table Structure:**
Precedence tables are organized by precedence level, with operators grouped by level and annotated with associativity information. The tables include inheritance information, showing which operators are inherited from which base grammars.

**Validation and Analysis:**
The system performs extensive validation of precedence tables, checking for inconsistencies, ambiguities, and potential parsing conflicts. Detailed analysis reports are generated that help developers understand the precedence relationships in their grammars.

**Export and Documentation:**
Precedence tables can be exported in various formats for documentation and analysis purposes. The system supports export to Markdown, HTML, and JSON formats, making it easy to integrate precedence information into project documentation.

## Error Recovery Framework

The error recovery framework provides sophisticated mechanisms for inheriting and managing error recovery strategies across grammar hierarchies. This framework is essential for creating robust parsers that can gracefully handle syntax errors and continue parsing.

### Recovery Strategy Types

The framework supports multiple types of error recovery strategies, each optimized for different types of parsing errors:

**Synchronization Recovery:**
Synchronization recovery strategies skip tokens until a synchronization point is reached. These strategies are highly effective for recovering from syntax errors in structured languages. The framework supports configurable synchronization token sets and provides mechanisms for inheriting and customizing synchronization strategies.

**Insertion Recovery:**
Insertion recovery strategies attempt to recover from errors by inserting missing tokens. These strategies are useful for handling common omission errors, such as missing semicolons or closing brackets. The framework provides template-based insertion strategies that can be customized for different language constructs.

**Deletion Recovery:**
Deletion recovery strategies attempt to recover from errors by removing unexpected tokens. These strategies are effective for handling extra tokens that don't fit the expected syntax. The framework includes sophisticated algorithms for determining which tokens to delete while minimizing the impact on subsequent parsing.

**Replacement Recovery:**
Replacement recovery strategies attempt to recover from errors by replacing incorrect tokens with expected tokens. These strategies are useful for handling common substitution errors, such as using the wrong operator or keyword.

### Recovery Strategy Inheritance

Error recovery strategies inherit according to sophisticated patterns that ensure consistency across grammar hierarchies while allowing for customization:

**Strategy Override:**
Derived grammars can override inherited recovery strategies by defining strategies with the same name. The overriding strategy completely replaces the inherited strategy, providing full flexibility for customization.

**Strategy Composition:**
Multiple recovery strategies can be composed to create more sophisticated recovery behaviors. The framework supports sequential composition, where strategies are tried in order until one succeeds, and parallel composition, where multiple strategies are evaluated and the best result is selected.

**Context-Sensitive Recovery:**
The framework supports context-sensitive recovery strategies that adapt their behavior based on the current parsing context. These strategies can provide more accurate recovery by considering the syntactic and semantic context of the error.

### Recovery Result Analysis

The framework provides comprehensive analysis of recovery results, helping developers understand the effectiveness of their recovery strategies:

**Recovery Statistics:**
The system tracks detailed statistics about recovery operations, including success rates, recovery times, and the types of errors encountered. These statistics help developers optimize their recovery strategies and identify common error patterns.

**Recovery Traces:**
Detailed traces of recovery operations are maintained, showing the sequence of recovery attempts and their outcomes. These traces are invaluable for debugging recovery strategies and understanding parser behavior during error conditions.

**Performance Analysis:**
The framework includes performance analysis tools that measure the impact of recovery operations on parsing performance. This information helps developers balance recovery effectiveness with parsing speed.

## Testing and Validation

The testing and validation framework provides comprehensive tools for verifying the correctness and performance of inheritance-based grammars. This framework is essential for ensuring that inherited grammars behave correctly and that inheritance relationships are properly maintained.

### Test Framework Architecture

The testing framework is built around a modular architecture that supports different types of tests and provides flexible execution and reporting capabilities:

**Test Suite Management:**
The framework supports hierarchical test suites that can be organized by grammar, inheritance level, or functional area. Test suites can be executed independently or as part of larger test runs, providing flexibility for different testing scenarios.

**Test Execution Engine:**
The test execution engine provides parallel test execution, timeout management, and resource cleanup. The engine supports both synchronous and asynchronous test execution patterns, accommodating different types of test scenarios.

**Result Collection and Analysis:**
Test results are collected in a structured format that supports detailed analysis and reporting. The framework provides statistical analysis of test results, including success rates, execution times, and failure patterns.

### Inheritance-Specific Testing

The framework includes specialized testing capabilities for inheritance-specific functionality:

**Basic Inheritance Tests:**
These tests verify that inheritance relationships are correctly established and that inherited elements are properly accessible in derived grammars. The tests check inheritance hierarchy construction, dependency resolution, and basic inheritance semantics.

**Semantic Action Tests:**
Semantic action tests verify that inherited actions behave correctly and that action resolution follows the expected patterns. These tests include action execution tests, parameter passing tests, and action composition tests.

**Precedence and Associativity Tests:**
These tests verify that precedence and associativity rules are correctly inherited and that operator precedence behaves as expected in derived grammars. The tests include precedence comparison tests, associativity validation tests, and precedence table generation tests.

**Error Recovery Tests:**
Error recovery tests verify that recovery strategies are correctly inherited and that error recovery behaves appropriately in derived grammars. These tests include recovery strategy execution tests, recovery result validation tests, and recovery performance tests.

### Automated Test Generation

The framework includes automated test generation capabilities that can create comprehensive test suites based on grammar specifications:

**Grammar-Based Test Generation:**
Tests can be automatically generated based on grammar rules and inheritance relationships. The generator creates tests that exercise all inherited elements and verify that inheritance semantics are correctly implemented.

**Property-Based Testing:**
The framework supports property-based testing approaches that generate random test inputs and verify that invariant properties hold across all test cases. This approach is particularly effective for finding edge cases and unexpected interactions in complex inheritance hierarchies.

**Regression Test Generation:**
When bugs are fixed or changes are made to inheritance behavior, the framework can automatically generate regression tests that prevent the reintroduction of similar issues.

## Plugin System

The plugin system provides extensible support for testing and validating embedded scripts within grammar files. This system is crucial for ensuring that semantic actions and other embedded code function correctly across different execution environments.

### Plugin Architecture

The plugin system is designed around a flexible architecture that supports multiple script languages and execution environments:

**Plugin Interface:**
All plugins implement a common interface that defines methods for script extraction, processing, and execution. This interface ensures consistency across different plugin implementations while allowing for language-specific optimizations.

**Execution Environment Abstraction:**
The system abstracts execution environments behind a common interface, allowing plugins to work with different runtime environments without being tightly coupled to specific implementations. This abstraction supports both local and remote execution environments.

**Script Lifecycle Management:**
The plugin system manages the complete lifecycle of embedded scripts, from extraction through execution to cleanup. This includes dependency resolution, resource allocation, and error handling.

### Supported Script Types

The plugin system supports multiple types of embedded scripts commonly found in grammar files:

**Bison/Yacc C Code Actions:**
The system can extract and validate C code embedded in Bison and Yacc grammar files. This includes both header code blocks and rule actions. The C code validation includes syntax checking and basic semantic analysis.

**ANTLR v4 Actions:**
ANTLR v4 actions in Java, C#, and Python are supported through language-specific plugins. These plugins can extract actions from grammar files and execute them in appropriate runtime environments.

**Minotaur Function Calls:**
The system supports Minotaur-specific function calls and JavaScript code blocks. These scripts are executed in a controlled JavaScript environment with access to parsing context and utility functions.

**Generic Code Blocks:**
The system can handle generic code blocks that don't fit into specific categories. These blocks are processed using configurable rules and can be validated using pluggable validation engines.

### Execution Environment Management

The plugin system includes sophisticated execution environment management capabilities:

**Environment Discovery:**
The system can automatically discover available execution environments and their capabilities. This includes checking for required runtime dependencies and validating environment configurations.

**Environment Isolation:**
Each script execution occurs in an isolated environment that prevents interference between different scripts and protects the host system from potentially malicious code.

**Resource Management:**
The system manages computational resources used by script execution, including memory limits, execution timeouts, and CPU usage constraints. This ensures that script execution doesn't negatively impact system performance.

### Testing and Validation Integration

The plugin system is tightly integrated with the testing and validation framework:

**Automated Script Testing:**
Scripts extracted from grammar files are automatically tested using appropriate execution environments. Test results are integrated with the overall test reporting system.

**Cross-Platform Validation:**
Scripts can be validated across multiple execution environments to ensure portability and consistency. This is particularly important for grammars that target multiple platforms or runtime environments.

**Performance Profiling:**
The system includes performance profiling capabilities that measure script execution times and resource usage. This information helps developers optimize their embedded scripts and identify performance bottlenecks.

## Usage Examples

This section provides comprehensive examples of how to use the inheritance system in various scenarios. These examples demonstrate both basic and advanced usage patterns and provide practical guidance for grammar developers.

### Basic Inheritance Example

Consider a scenario where we want to create a family of arithmetic expression grammars with different levels of complexity. We start by defining a base grammar that includes basic arithmetic operations:

```
// BaseArithmetic.grammar
@Inheritable: true
@FormatType: ANTLR4

grammar BaseArithmetic;

expression
    : expression '+' expression   { $$ = $1 + $3; }
    | expression '-' expression   { $$ = $1 - $3; }
    | expression '*' expression   { $$ = $1 * $3; }
    | expression '/' expression   { $$ = $1 / $3; }
    | '(' expression ')'          { $$ = $2; }
    | NUMBER                      { $$ = $1; }
    ;

NUMBER : [0-9]+ ;
```

Now we can create a derived grammar that extends the base arithmetic grammar with additional operations:

```
// ExtendedArithmetic.grammar
@Inherits: BaseArithmetic
@FormatType: ANTLR4

grammar ExtendedArithmetic;

expression
    : expression '^' expression   { $$ = Math.pow($1, $3); }
    | expression '%' expression   { $$ = $1 % $3; }
    ;

// Inherited rules from BaseArithmetic are automatically available
```

### Semantic Action Inheritance Example

This example demonstrates how semantic actions can be inherited and customized:

```
// BaseCalculator.grammar
@Inheritable: true
@FormatType: ANTLR4

grammar BaseCalculator;

@SemanticActions {
    addOperation: @call(performAddition, $1, $3)
    subtractOperation: @call(performSubtraction, $1, $3)
    multiplyOperation: @call(performMultiplication, $1, $3)
    divideOperation: @call(performDivision, $1, $3)
}

expression
    : expression '+' expression   @action(addOperation)
    | expression '-' expression   @action(subtractOperation)
    | expression '*' expression   @action(multiplyOperation)
    | expression '/' expression   @action(divideOperation)
    | NUMBER                      { $$ = parseFloat($1); }
    ;
```

A derived grammar can override specific semantic actions while inheriting others:

```
// ScientificCalculator.grammar
@Inherits: BaseCalculator
@FormatType: ANTLR4

grammar ScientificCalculator;

@SemanticActions {
    // Override division to handle division by zero
    divideOperation: @js{
        if ($3 === 0) {
            throw new Error("Division by zero");
        }
        return $1 / $3;
    }
    
    // Add new semantic actions
    powerOperation: @call(performPower, $1, $3)
    sqrtOperation: @call(performSquareRoot, $1)
}

expression
    : expression '^' expression   @action(powerOperation)
    | 'sqrt' '(' expression ')'   @action(sqrtOperation)
    ;
```

### Precedence Inheritance Example

This example shows how precedence and associativity rules can be inherited and extended:

```
// BasePrecedence.grammar
@Inheritable: true
@FormatType: Bison

%left '+' '-'
%left '*' '/'
%right '^'

%%

expression
    : expression '+' expression
    | expression '-' expression
    | expression '*' expression
    | expression '/' expression
    | expression '^' expression
    | '(' expression ')'
    | NUMBER
    ;
```

A derived grammar can inherit these precedence rules and add new ones:

```
// ExtendedPrecedence.grammar
@Inherits: BasePrecedence
@FormatType: Bison

// Inherit all precedence rules from BasePrecedence
%left '%'           // Add modulo with same precedence as * and /
%right UNARY_MINUS  // Add unary minus with high precedence

%%

expression
    : expression '%' expression
    | '-' expression %prec UNARY_MINUS
    ;
```

### Error Recovery Inheritance Example

This example demonstrates how error recovery strategies can be inherited and customized:

```
// BaseErrorRecovery.grammar
@Inheritable: true
@FormatType: ANTLR4

@ErrorRecovery {
    syntaxError: @synchronize(';', '}', 'EOF')
    missingOperand: @insert('0')
    extraToken: @skip(1)
}

grammar BaseErrorRecovery;

statement
    : expression ';'
    | '{' statement_list '}'
    ;

statement_list
    : statement*
    ;
```

A derived grammar can inherit these recovery strategies and add domain-specific ones:

```
// RobustParser.grammar
@Inherits: BaseErrorRecovery
@FormatType: ANTLR4

@ErrorRecovery {
    // Override syntax error recovery for better user experience
    syntaxError: @js{
        console.log("Syntax error at line " + context.getLineNumber());
        return synchronizeToNext([';', '}', 'EOF']);
    }
    
    // Add new recovery strategies
    missingBrace: @insert('}')
    unbalancedParens: @synchronize(')')
}

grammar RobustParser;

// Additional rules that benefit from enhanced error recovery
```

### Multiple Inheritance Example

This example shows how a grammar can inherit from multiple base grammars:

```
// ArithmeticBase.grammar
@Inheritable: true
@FormatType: ANTLR4

grammar ArithmeticBase;

arithmetic_expression
    : arithmetic_expression '+' arithmetic_expression
    | arithmetic_expression '-' arithmetic_expression
    | NUMBER
    ;
```

```
// LogicalBase.grammar
@Inheritable: true
@FormatType: ANTLR4

grammar LogicalBase;

logical_expression
    : logical_expression '&&' logical_expression
    | logical_expression '||' logical_expression
    | '!' logical_expression
    | BOOLEAN
    ;
```

```
// CombinedLanguage.grammar
@Inherits: ArithmeticBase, LogicalBase
@FormatType: ANTLR4

grammar CombinedLanguage;

// This grammar inherits both arithmetic and logical expressions
expression
    : arithmetic_expression
    | logical_expression
    | arithmetic_expression '>' arithmetic_expression  // Comparison
    | arithmetic_expression '<' arithmetic_expression
    ;

// Rules from both base grammars are available
```

## API Reference

This section provides a comprehensive reference for the inheritance system API. The API is organized by component, with detailed descriptions of classes, methods, and their parameters.

### Interpreter Class

The `Interpreter` class serves as the primary interface for working with inheritance functionality. It provides methods for managing grammars, semantic actions, precedence rules, and error recovery strategies.

#### Grammar Management Methods

```typescript
public loadGrammar(grammarName: string, grammarContent: string): void
```
Loads a grammar with inheritance support. The method automatically processes inheritance directives and establishes inheritance relationships.

**Parameters:**
- `grammarName`: The name of the grammar to load
- `grammarContent`: The grammar content as a string

**Throws:**
- `GrammarError`: If the grammar contains syntax errors or invalid inheritance directives
- `InheritanceError`: If inheritance relationships cannot be resolved

```typescript
public loadGrammarFromFile(grammarName: string, filePath: string): void
```
Loads a grammar from a file with inheritance support.

**Parameters:**
- `grammarName`: The name of the grammar to load
- `filePath`: The path to the grammar file

```typescript
public getGrammar(grammarName: string): Grammar | null
```
Retrieves a loaded grammar by name.

**Parameters:**
- `grammarName`: The name of the grammar to retrieve

**Returns:**
- The grammar object or null if not found

#### Semantic Action Methods

```typescript
public registerSemanticAction(grammarName: string, actionName: string, action: SemanticAction): void
```
Registers a semantic action for a specific grammar.

**Parameters:**
- `grammarName`: The name of the grammar
- `actionName`: The name of the action
- `action`: The semantic action object

```typescript
public getSemanticAction(grammarName: string, actionName: string): SemanticAction | null
```
Retrieves a semantic action with inheritance resolution.

**Parameters:**
- `grammarName`: The name of the grammar
- `actionName`: The name of the action

**Returns:**
- The resolved semantic action or null if not found

```typescript
public executeSemanticAction(grammarName: string, actionName: string, productionName: string, args: any[]): any
```
Executes a semantic action with the given arguments.

**Parameters:**
- `grammarName`: The name of the grammar
- `actionName`: The name of the action
- `productionName`: The name of the production rule
- `args`: The arguments to pass to the action

**Returns:**
- The result of the action execution

#### Precedence Management Methods

```typescript
public registerPrecedenceRule(grammarName: string, operatorName: string, level: number, description?: string): void
```
Registers a precedence rule for an operator.

**Parameters:**
- `grammarName`: The name of the grammar
- `operatorName`: The name of the operator
- `level`: The precedence level (higher numbers = higher precedence)
- `description`: Optional description of the rule

```typescript
public registerAssociativityRule(grammarName: string, operatorName: string, associativity: Associativity, description?: string): void
```
Registers an associativity rule for an operator.

**Parameters:**
- `grammarName`: The name of the grammar
- `operatorName`: The name of the operator
- `associativity`: The associativity (Left, Right, or None)
- `description`: Optional description of the rule

```typescript
public comparePrecedence(grammarName: string, operator1: string, operator2: string): number | null
```
Compares the precedence of two operators.

**Parameters:**
- `grammarName`: The name of the grammar
- `operator1`: The first operator
- `operator2`: The second operator

**Returns:**
- -1 if operator1 has lower precedence, 1 if higher, 0 if equal, null if comparison not possible

#### Error Recovery Methods

```typescript
public registerErrorRecoveryStrategy(grammarName: string, strategyName: string, strategy: ErrorRecoveryStrategy): void
```
Registers an error recovery strategy for a grammar.

**Parameters:**
- `grammarName`: The name of the grammar
- `strategyName`: The name of the strategy
- `strategy`: The error recovery strategy object

```typescript
public applyErrorRecovery(grammarName: string, errorType: ErrorType, context: ErrorContext): RecoveryResult
```
Applies error recovery for a specific error.

**Parameters:**
- `grammarName`: The name of the grammar
- `errorType`: The type of error
- `context`: The error context

**Returns:**
- The recovery result

### Grammar Class

The `Grammar` class represents a grammar with inheritance support.

#### Properties

```typescript
public readonly name: string
```
The name of the grammar.

```typescript
public readonly content: string
```
The content of the grammar.

#### Inheritance Methods

```typescript
public addBaseGrammar(grammarName: string): void
```
Adds a base grammar to the inheritance hierarchy.

```typescript
public removeBaseGrammar(grammarName: string): boolean
```
Removes a base grammar from the inheritance hierarchy.

```typescript
public getBaseGrammars(): string[]
```
Returns the list of base grammars.

```typescript
public isInheritableGrammar(): boolean
```
Checks if the grammar can be inherited from.

```typescript
public getFormatType(): GrammarFormatType
```
Returns the grammar format type.

### SemanticAction Interface

The `SemanticAction` interface defines the contract for semantic actions.

```typescript
interface SemanticAction {
    getName(): string;
    getGrammarName(): string;
    execute(context: SemanticActionContext, args: any[]): any;
    getActionType(): SemanticActionType;
    getDescription(): string;
}
```

### ErrorRecoveryStrategy Interface

The `ErrorRecoveryStrategy` interface defines the contract for error recovery strategies.

```typescript
interface ErrorRecoveryStrategy {
    recover(context: ErrorContext): RecoveryResult;
    getStrategyType(): RecoveryStrategyType;
    getDescription(): string;
}
```

## Best Practices

This section outlines best practices for using the inheritance system effectively. Following these practices will help ensure that your grammars are maintainable, performant, and robust.

### Grammar Design Principles

When designing grammar hierarchies, consider the following principles:

**Single Responsibility Principle:**
Each base grammar should have a single, well-defined responsibility. For example, create separate base grammars for arithmetic expressions, logical expressions, and control structures rather than combining them into a single monolithic base grammar.

**Composition Over Deep Inheritance:**
Prefer composition patterns using multiple inheritance over deep inheritance hierarchies. Deep inheritance can lead to complex dependency chains and make debugging difficult. Instead, create focused base grammars that can be combined as needed.

**Interface Segregation:**
Design base grammars with clear, minimal interfaces. Avoid creating base grammars that force derived grammars to inherit unnecessary rules or semantic actions. Use the `@Inheritable` directive judiciously to control what can be inherited.

**Dependency Inversion:**
Design base grammars to depend on abstractions rather than concrete implementations. Use callback-based semantic actions and configurable precedence templates to allow derived grammars to customize behavior without modifying base grammars.

### Performance Optimization

The inheritance system includes several performance optimization opportunities:

**Caching Strategy:**
Take advantage of the built-in caching mechanisms by structuring your inheritance hierarchies to maximize cache effectiveness. Avoid frequently changing inheritance relationships, as this invalidates caches and can impact performance.

**Lazy Loading:**
Use lazy loading patterns for semantic actions and other inherited elements that may not be needed immediately. The system supports lazy loading through the plugin architecture and callback mechanisms.

**Batch Operations:**
When working with multiple grammars or performing bulk operations, use batch processing methods where available. This reduces the overhead of individual operations and improves overall performance.

**Memory Management:**
Be mindful of memory usage when working with large inheritance hierarchies. Use the cleanup methods provided by the system to release resources when grammars are no longer needed.

### Error Handling and Debugging

Effective error handling and debugging strategies are crucial for working with inheritance systems:

**Comprehensive Error Messages:**
The system provides detailed error messages that include inheritance context. When debugging issues, pay attention to the inheritance hierarchy information included in error messages.

**Validation Early and Often:**
Use the validation methods provided by the system to check inheritance relationships and grammar consistency early in the development process. This helps catch issues before they become difficult to debug.

**Testing Strategy:**
Implement comprehensive testing strategies that cover inheritance-specific functionality. Use the provided testing framework to create tests that verify inheritance behavior at multiple levels of the hierarchy.

**Logging and Monitoring:**
Enable logging and monitoring features to track inheritance resolution performance and identify potential issues. The system provides detailed statistics that can help optimize performance and identify problems.

### Security Considerations

When working with embedded scripts and semantic actions, consider the following security practices:

**Script Sandboxing:**
Always execute embedded scripts in sandboxed environments, especially when processing untrusted grammar files. The plugin system provides isolation mechanisms that should be used consistently.

**Input Validation:**
Validate all inputs to semantic actions and embedded scripts. The system provides validation frameworks that can help ensure that inputs are safe and well-formed.

**Resource Limits:**
Set appropriate resource limits for script execution, including memory limits, execution timeouts, and CPU usage constraints. This prevents malicious or poorly written scripts from impacting system performance.

**Access Control:**
Implement appropriate access controls for grammar loading and modification operations. Consider the security implications of allowing dynamic grammar loading in production environments.

## Troubleshooting

This section provides guidance for diagnosing and resolving common issues with the inheritance system.

### Common Issues and Solutions

**Circular Dependency Errors:**
Circular dependencies occur when grammars form a cycle in their inheritance relationships. The system detects these automatically and provides detailed error messages.

*Symptoms:*
- Error messages indicating circular dependency
- Grammars failing to load
- Infinite loops during inheritance resolution

*Solutions:*
- Review inheritance relationships to identify the cycle
- Refactor grammars to break the circular dependency
- Use composition instead of inheritance where appropriate

**Missing Base Grammar Errors:**
These errors occur when a grammar tries to inherit from a base grammar that hasn't been loaded or doesn't exist.

*Symptoms:*
- Error messages indicating missing base grammar
- Inheritance resolution failures
- Incomplete inheritance hierarchies

*Solutions:*
- Ensure all base grammars are loaded before derived grammars
- Check grammar names for typos
- Verify that base grammars are marked as inheritable

**Semantic Action Resolution Failures:**
These issues occur when semantic actions cannot be resolved in the inheritance hierarchy.

*Symptoms:*
- Null or undefined semantic actions
- Action execution failures
- Inconsistent action behavior

*Solutions:*
- Verify that actions are properly defined in base grammars
- Check action names for typos
- Ensure that callback functions are registered
- Review action override patterns

**Precedence Conflicts:**
Precedence conflicts occur when multiple inheritance paths provide conflicting precedence information for the same operator.

*Symptoms:*
- Parsing ambiguities
- Unexpected operator precedence behavior
- Warning messages about precedence conflicts

*Solutions:*
- Review precedence definitions in base grammars
- Use explicit precedence declarations to resolve conflicts
- Consider refactoring inheritance hierarchy to avoid conflicts

### Debugging Techniques

**Inheritance Hierarchy Visualization:**
Use the system's hierarchy visualization tools to understand complex inheritance relationships. These tools can help identify issues with inheritance structure and dependencies.

**Step-by-Step Resolution Tracing:**
Enable detailed tracing of inheritance resolution to understand how elements are resolved in complex hierarchies. This can help identify where resolution is failing or producing unexpected results.

**Cache Analysis:**
Use cache analysis tools to understand cache behavior and identify performance issues. Cache misses or frequent invalidations can indicate problems with inheritance structure or usage patterns.

**Performance Profiling:**
Use the built-in performance profiling tools to identify bottlenecks in inheritance resolution and semantic action execution. This information can guide optimization efforts.

### Diagnostic Tools

The system includes several diagnostic tools that can help identify and resolve issues:

**Grammar Validator:**
The grammar validator checks inheritance relationships, semantic action definitions, and precedence rules for consistency and correctness.

**Inheritance Analyzer:**
The inheritance analyzer provides detailed reports on inheritance hierarchies, including dependency graphs, resolution paths, and potential issues.

**Performance Monitor:**
The performance monitor tracks system performance metrics and can help identify performance bottlenecks and optimization opportunities.

**Test Framework Integration:**
The testing framework includes diagnostic capabilities that can help identify issues with inheritance behavior and provide detailed failure analysis.

## Future Enhancements

This section outlines planned enhancements and potential future directions for the inheritance system.

### Planned Features

**Advanced Template System:**
Future versions will include an enhanced template system that supports more sophisticated parameterization and template composition patterns. This will enable the creation of highly reusable grammar components.

**Visual Grammar Designer:**
A visual grammar designer tool is planned that will provide graphical interfaces for creating and managing inheritance hierarchies. This tool will make the system more accessible to developers who prefer visual design approaches.

**Enhanced Plugin Architecture:**
The plugin architecture will be expanded to support more execution environments and script types. This includes support for additional programming languages and integration with external tools and services.

**Performance Optimizations:**
Ongoing performance optimization efforts will focus on reducing memory usage, improving cache effectiveness, and optimizing inheritance resolution algorithms.

### Research Directions

**Machine Learning Integration:**
Research is ongoing into integrating machine learning techniques for automatic grammar optimization, error recovery strategy selection, and performance tuning.

**Distributed Grammar Processing:**
Investigation into distributed processing approaches for large-scale grammar hierarchies and parallel inheritance resolution.

**Formal Verification:**
Research into formal verification techniques for ensuring the correctness of inheritance relationships and semantic action behavior.

**Domain-Specific Languages:**
Development of domain-specific languages for describing inheritance relationships and grammar patterns more concisely and expressively.

### Community Contributions

The inheritance system is designed to be extensible and welcomes community contributions:

**Plugin Development:**
Community members are encouraged to develop plugins for additional script languages and execution environments.

**Template Libraries:**
Contributions of reusable grammar templates for common language patterns and constructs are welcomed.

**Testing and Validation:**
Community testing and validation efforts help ensure the robustness and reliability of the system across different use cases and environments.

**Documentation and Examples:**
Contributions to documentation and example collections help make the system more accessible to new users and promote best practices.

---

## References

[1] Aho, A. V., Lam, M. S., Sethi, R., & Ullman, J. D. (2006). *Compilers: Principles, Techniques, and Tools* (2nd ed.). Addison-Wesley.

[2] Parr, T. (2013). *The Definitive ANTLR 4 Reference*. Pragmatic Bookshelf.

[3] Levine, J. (2009). *flex & bison: Text Processing Tools*. O'Reilly Media.

[4] Grune, D., & Jacobs, C. J. H. (2008). *Parsing Techniques: A Practical Guide* (2nd ed.). Springer.

[5] Cooper, K. D., & Torczon, L. (2011). *Engineering a Compiler* (2nd ed.). Morgan Kaufmann.

[6] Appel, A. W., & Palsberg, J. (2002). *Modern Compiler Implementation in Java* (2nd ed.). Cambridge University Press.

[7] Wilhelm, R., & Maurer, D. (1995). *Compiler Design*. Addison-Wesley.

[8] Mogensen, T. Æ. (2017). *Introduction to Compiler Design* (2nd ed.). Springer.

---

*This documentation is maintained by the Minotaur development team. For questions, issues, or contributions, please visit the project repository or contact the development team.*

