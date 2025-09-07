# PROJECT GOLEM - TECHNICAL DESIGN SPECIFICATION
## Grammar-Driven AST Error Correction System

**Version:** 1.0  
**Date:** August 2024  
**Status:** Phase 1 Implementation Complete  

---

## 🎯 EXECUTIVE SUMMARY

Project Golem implements a fully generic, grammar-driven AST error correction system that leverages Minotaur's StepLexer/StepParser infrastructure to provide intelligent code correction across any supported language. The system eliminates language-specific components by encoding all correction logic within grammar rules and semantic constraints.

### Key Innovation
- **Grammar as Single Source of Truth**: All syntax rules, semantic constraints, and valid transformations defined in grammar files
- **Zero Language-Specific Code**: Pure grammar-driven approach eliminates hardcoded language logic
- **Real-Time Validation**: StepLexer/StepParser provide structured error detection with precise positioning
- **Iterative Correction**: Feedback loop system with learning and optimization capabilities

---

## 🏗️ SYSTEM ARCHITECTURE

### Core Principle: Grammar Production Triggers
The system operates on the principle that semantic validation should be triggered by specific grammar productions during parsing, leveraging Minotaur's existing StepParser semantic action capabilities.

```
Grammar Definition Layer
├── Syntax Rules (existing in Minotaur)
├── Production Triggers (NEW - semantic actions on grammar productions)
└── Semantic Actions (NEW - handlers fired by production triggers)

Minotaur Parser Layer  
├── StepLexer (existing)
├── StepParser (existing - enhanced with semantic triggers)
└── Production Trigger System (NEW - integrated with StepParser)

Golem Correction Layer
├── SemanticValidator (production trigger-based)
├── GrammarDrivenASTMapper  
├── ASTTransformationEngine
└── CorrectionFeedbackLoop
```

### Component Responsibilities

#### **Grammar Definition Layer**
- **Syntax Rules**: Define valid language constructs (existing Minotaur capability)
- **Production Triggers**: Register semantic actions on specific grammar productions
- **Semantic Actions**: Handler functions that execute when productions are matched

#### **Minotaur Parser Layer**
- **StepLexer**: Tokenization with precise position tracking
- **StepParser**: AST construction with integrated semantic trigger firing
- **Production Trigger System**: Manages and executes semantic actions during parsing

#### **Golem Correction Layer**
- **SemanticValidator**: Production trigger-based semantic validation
- **GrammarDrivenASTMapper**: Maps errors to grammar-defined correction strategies
- **ASTTransformationEngine**: Applies AST modifications with grammar validation
- **CorrectionFeedbackLoop**: Iterative correction with learning capabilities

---

## 🔍 PRODUCTION TRIGGER FRAMEWORK

### 1. VARIABLE SCOPE AND BINDING TRIGGERS

#### NAME Production Trigger
```typescript
// Triggered when NAME token is matched in grammar
registerTrigger('NAME', 'on_match', {
  name: 'validate_variable_reference',
  handler: async (context: ProductionContext) => {
    // Check if variable is defined in current scope stack
    // Return NameError if undefined variable is referenced
  }
});
```

#### Function Definition Triggers
```typescript
// Triggered when entering function definition
registerTrigger('funcdef', 'on_enter', {
  name: 'enter_function_scope',
  handler: async (context: ProductionContext) => {
    // Create new function scope
    // Register function signature in type environment
    // Update control flow state
  }
});

registerTrigger('funcdef', 'on_exit', {
  name: 'exit_function_scope', 
  handler: async (context: ProductionContext) => {
    // Validate all return paths
    // Exit function scope
    // Update control flow state
  }
});
```

### 2. CONTROL FLOW VALIDATION TRIGGERS

#### Return Statement Trigger
```typescript
registerTrigger('return_stmt', 'on_match', {
  name: 'validate_return_statement',
  handler: async (context: ProductionContext) => {
    // Check if return is within function scope
    // Return SyntaxError if return outside function
  }
});
```

#### Break/Continue Statement Triggers
```typescript
registerTrigger('break_stmt', 'on_match', {
  name: 'validate_break_statement',
  handler: async (context: ProductionContext) => {
    // Check if break is within loop scope
    // Return SyntaxError if break outside loop
  }
});

registerTrigger('continue_stmt', 'on_match', {
  name: 'validate_continue_statement',
  handler: async (context: ProductionContext) => {
    // Check if continue is within loop scope
    // Return SyntaxError if continue outside loop
  }
});
```

### 3. IMPORT RESOLUTION TRIGGERS

#### Import Statement Triggers
```typescript
registerTrigger('import_name', 'on_match', {
  name: 'process_import_statement',
  handler: async (context: ProductionContext) => {
    // Extract module name from AST
    // Check if module exists in standard library
    // Return ImportError if module not found
  }
});

registerTrigger('import_from', 'on_match', {
  name: 'process_from_import',
  handler: async (context: ProductionContext) => {
    // Extract module and imported names
    // Validate module exists and exports requested names
    // Update scope with imported bindings
  }
});
```

### 4. ASSIGNMENT AND BINDING TRIGGERS

#### Assignment Expression Trigger
```typescript
registerTrigger('expr_stmt', 'on_match', {
  name: 'process_assignment',
  handler: async (context: ProductionContext) => {
    // Check if this is an assignment statement
    // Extract variable name and add to current scope
    // Validate assignment target is valid
  }
});
```

### 5. LOOP SCOPE MANAGEMENT TRIGGERS

#### Loop Entry/Exit Triggers
```typescript
registerTrigger('for_stmt', 'on_enter', {
  name: 'enter_loop_scope',
  handler: async (context: ProductionContext) => {
    // Update control flow state to indicate in loop
    // Set loop type for break/continue validation
  }
});

registerTrigger('for_stmt', 'on_exit', {
  name: 'exit_loop_scope',
  handler: async (context: ProductionContext) => {
    // Update control flow state to indicate out of loop
    // Clear loop type
  }
});
```

---

## 🛠️ IMPLEMENTATION COMPONENTS
### 1. SemanticValidator (Production Trigger-Based)

```typescript
interface ProductionTrigger {
  productionName: string;
  triggerType: 'on_enter' | 'on_exit' | 'on_match';
  action: SemanticAction;
  priority: number;
  enabled: boolean;
}

class SemanticValidator {
  constructor(grammar: Grammar, stepParser: StepParser) {
    // Register production triggers for semantic validation
    this.registerProductionTriggers();
  }
  
  async validateSemantics(
    ast: ZeroCopyASTNode,
    sourceCode: string
  ): Promise<SemanticValidationResult> {
    // Traverse AST and fire production triggers
    // Collect semantic errors and warnings
    // Return structured validation result
  }
  
  private registerTrigger(
    production: string,
    triggerType: 'on_enter' | 'on_exit' | 'on_match',
    action: SemanticAction,
    priority: number = 50
  ): void {
    // Register semantic action on grammar production
  }
}
```

### 2. GrammarDrivenASTMapper (Updated)

```typescript
class GrammarDrivenASTMapper {
  constructor(private grammar: Grammar) {}
  
  mapErrorToTransformation(
    error: StructuredValidationError,
    context: ASTContext
  ): TransformationCandidate[] {
    
    // Use grammar productions to determine valid transformations
    const applicableProductions = this.grammar.getProductionsForErrorType(error.type);
    
    // Generate transformation candidates based on production triggers
    const candidates = this.generateTransformationsFromProductions(
      applicableProductions, 
      error, 
      context
    );
    
    // Validate transformations against grammar rules
    return this.validateAndRankCandidates(candidates, context);
  }
}
```

### 3. Production Trigger Integration with StepParser

The SemanticValidator integrates directly with Minotaur's StepParser to fire semantic actions during parsing:

```typescript
// Integration approach - extending StepParser capabilities
class EnhancedStepParser extends StepParser {
  private semanticValidator: SemanticValidator;
  
  constructor(grammar: Grammar, semanticValidator: SemanticValidator) {
    super(grammar);
    this.semanticValidator = semanticValidator;
  }
  
  protected onProductionMatched(
    production: string, 
    node: ZeroCopyASTNode,
    context: ParseContext
  ): void {
    // Fire semantic triggers when productions are matched
    this.semanticValidator.fireTriggers(production, node, context);
  }
}
```

---

## 🔄 CORRECTION WORKFLOW

### Production Trigger-Based Error Detection and Correction

1. **Parse with Semantic Triggers**: StepParser processes source code and fires production triggers
2. **Collect Semantic Errors**: Production triggers validate semantics and collect errors
3. **Map Errors to Transformations**: Grammar-driven mapper generates correction candidates
4. **Apply AST Transformations**: Engine applies corrections with grammar validation
5. **Generate Corrected Code**: Convert transformed AST back to source code
6. **Iterative Correction**: Repeat until code is valid or max attempts reached

---

## 🎯 PYTHON 3.11 IMPLEMENTATION FOCUS

#### Python: Indentation Consistency
```grammar
semantic_rule python_indentation {
  constraint: block_statement -> consistent_indentation_level
  error_type: INDENTATION_ERROR
  fix_strategy: normalize_indentation
  confidence: 0.98
  language: python
}
```

#### JavaScript: Hoisting Rules
```grammar
semantic_rule javascript_hoisting {
  constraint: variable_declaration -> hoisting_compatible
  error_type: REFERENCE_ERROR
  fix_strategy: reorder_declarations | use_let_const
  language: javascript
}
```

#### Java: Access Modifier Consistency
```grammar
semantic_rule java_access_modifiers {
  constraint: class_member -> access_modifier_valid(context, visibility)
  error_type: ACCESS_ERROR
  fix_strategy: adjust_access_modifier | move_to_appropriate_scope
  language: java
}
```

---

## 🛠️ IMPLEMENTATION COMPONENTS

### 1. GrammarDrivenErrorDetector

```typescript
interface ErrorDetectionResult {
  lexicalErrors: LexicalError[];
  syntaxErrors: SyntaxError[];
  semanticErrors: SemanticError[];
  allErrors: StructuredValidationError[];
}

class GrammarDrivenErrorDetector {
  constructor(
    private grammar: Grammar,
    private stepLexer: StepLexer,
    private stepParser: StepParser,
    private semanticValidator: SemanticValidator
  ) {}
  
  detectErrors(sourceCode: string): ErrorDetectionResult {
    // Phase 1: Lexical Analysis
    const tokenResult = this.stepLexer.tokenize(sourceCode);
    const lexicalErrors = this.extractLexicalErrors(tokenResult);
    
    // Phase 2: Syntax Analysis
    const parseResult = this.stepParser.parse(tokenResult.tokens);
    const syntaxErrors = this.extractSyntaxErrors(parseResult);
    
    // Phase 3: Semantic Analysis
    const semanticErrors = parseResult.ast ? 
      this.semanticValidator.validateSemantics(parseResult.ast) : [];
    
    // Phase 4: Unify Error Representation
    const allErrors = this.unifyErrors(lexicalErrors, syntaxErrors, semanticErrors);
    
    return {
      lexicalErrors,
      syntaxErrors,
      semanticErrors,
      allErrors
    };
  }
  
  private extractLexicalErrors(tokenResult: TokenizationResult): LexicalError[] {
    return tokenResult.errors.map(error => ({
      type: ErrorType.LEXICAL_ERROR,
      position: error.position,
      message: error.message,
      context: this.extractContext(error.position),
      grammarRule: error.expectedTokenTypes
    }));
  }
  
  private extractSyntaxErrors(parseResult: ParseResult): SyntaxError[] {
    return parseResult.errors.map(error => ({
      type: ErrorType.SYNTAX_ERROR,
      position: error.position,
      message: error.message,
      context: this.extractContext(error.position),
      expectedRules: error.expectedGrammarRules,
      actualToken: error.actualToken
    }));
  }
}
```

### 2. GrammarDrivenASTMapper

```typescript
interface TransformationCandidate {
  transformation: ASTTransformation;
  confidence: number;
  priority: number;
  grammarRule: string;
  fixStrategy: string;
}

class GrammarDrivenASTMapper {
  constructor(private grammar: Grammar) {}
  
  mapErrorToTransformation(
    error: StructuredValidationError,
    context: ASTContext
  ): TransformationCandidate[] {
    
    // Step 1: Find applicable semantic rules
    const applicableRules = this.grammar.getSemanticRulesForError(error.type);
    
    // Step 2: Generate transformation candidates
    const candidates: TransformationCandidate[] = [];
    
    for (const rule of applicableRules) {
      if (this.ruleApplies(rule, error, context)) {
        const transformations = this.generateTransformationsFromRule(rule, error, context);
        candidates.push(...transformations);
      }
    }
    
    // Step 3: Validate transformations against grammar
    const validCandidates = candidates.filter(candidate => 
      this.validateTransformation(candidate.transformation, context)
    );
    
    // Step 4: Rank by confidence and priority
    return validCandidates.sort((a, b) => {
      const scoreA = a.confidence * 0.7 + (1 - a.priority / 10) * 0.3;
      const scoreB = b.confidence * 0.7 + (1 - b.priority / 10) * 0.3;
      return scoreB - scoreA;
    });
  }
  
  private generateTransformationsFromRule(
    rule: SemanticRule,
    error: StructuredValidationError,
    context: ASTContext
  ): TransformationCandidate[] {
    
    const candidates: TransformationCandidate[] = [];
    
    for (const strategy of rule.fixStrategies) {
      switch (strategy) {
        case 'insert_declaration':
          candidates.push(this.createInsertDeclarationTransformation(error, context, rule));
          break;
        case 'suggest_alternatives':
          candidates.push(...this.createSuggestionTransformations(error, context, rule));
          break;
        case 'type_conversion':
          candidates.push(this.createTypeConversionTransformation(error, context, rule));
          break;
        case 'reorder_statements':
          candidates.push(this.createReorderTransformation(error, context, rule));
          break;
        case 'normalize_formatting':
          candidates.push(this.createFormattingTransformation(error, context, rule));
          break;
      }
    }
    
    return candidates;
  }
}
```

### 3. SemanticValidator (NEW)

```typescript
interface SemanticValidationContext {
  scopeStack: Scope[];
  typeEnvironment: TypeEnvironment;
  controlFlowState: ControlFlowState;
  importGraph: ImportGraph;
}

class SemanticValidator {
  constructor(private grammar: Grammar) {}
  
  validateSemantics(ast: ZeroCopyASTNode): SemanticError[] {
    const context: SemanticValidationContext = {
      scopeStack: [new GlobalScope()],
      typeEnvironment: new TypeEnvironment(),
      controlFlowState: new ControlFlowState(),
      importGraph: new ImportGraph()
    };
    
    const errors: SemanticError[] = [];
    this.validateNode(ast, context, errors);
    return errors;
  }
  
  private validateNode(
    node: ZeroCopyASTNode,
    context: SemanticValidationContext,
    errors: SemanticError[]
  ): void {
    
    // Apply all semantic rules for this node type
    const applicableRules = this.grammar.getSemanticRulesForNodeType(node.type);
    
    for (const rule of applicableRules) {
      const violation = this.checkRule(rule, node, context);
      if (violation) {
        errors.push(this.createSemanticError(rule, violation, node));
      }
    }
    
    // Update context based on node
    this.updateContext(node, context);
    
    // Recursively validate children
    for (const child of node.children) {
      this.validateNode(child, context, errors);
    }
    
    // Restore context after processing node
    this.restoreContext(node, context);
  }
  
  private checkRule(
    rule: SemanticRule,
    node: ZeroCopyASTNode,
    context: SemanticValidationContext
  ): RuleViolation | null {
    
    switch (rule.constraintType) {
      case 'scope_binding':
        return this.checkScopeBinding(rule, node, context);
      case 'type_compatibility':
        return this.checkTypeCompatibility(rule, node, context);
      case 'control_flow':
        return this.checkControlFlow(rule, node, context);
      case 'import_resolution':
        return this.checkImportResolution(rule, node, context);
      default:
        return null;
    }
  }
}
```

### 4. Enhanced Grammar Definition Format

```grammar
// Extended grammar syntax for semantic rules
grammar PythonWithSemantics extends Python {
  
  // Existing syntax rules remain unchanged
  
  // New semantic rule definitions
  semantic_rules {
    
    variable_declaration_before_use {
      pattern: identifier_reference
      constraint: requires_prior_declaration
      scope: current_and_parent_scopes
      error_type: NAME_ERROR
      message: "name '{identifier}' is not defined"
      fix_strategies: [
        {
          strategy: insert_declaration
          template: "{identifier} = None  # Auto-generated variable"
          confidence: 0.8
          priority: HIGH
        },
        {
          strategy: suggest_alternatives
          source: available_names_in_scope
          confidence: 0.9
          priority: MEDIUM
        }
      ]
    }
    
    function_return_type {
      pattern: return_statement
      constraint: within_function_scope
      error_type: SYNTAX_ERROR
      message: "'return' outside function"
      fix_strategies: [
        {
          strategy: wrap_in_function
          template: "def function():\n    {original_code}\n    return {return_value}"
          confidence: 0.7
          priority: LOW
        },
        {
          strategy: remove_return
          template: "{return_value}"
          confidence: 0.9
          priority: HIGH
        }
      ]
    }
    
    import_module_exists {
      pattern: import_statement
      constraint: module_resolvable
      error_type: IMPORT_ERROR
      message: "No module named '{module_name}'"
      fix_strategies: [
        {
          strategy: suggest_similar_modules
          source: available_modules
          similarity_threshold: 0.8
          confidence: 0.7
        },
        {
          strategy: create_module_stub
          template: "# {module_name}.py\n# Auto-generated module stub"
          confidence: 0.5
        }
      ]
    }
  }
  
  // Type system definitions
  type_system {
    primitive_types: [int, float, str, bool, None]
    collection_types: [list, dict, set, tuple]
    
    type_compatibility_rules {
      int -> float: implicit_conversion
      str -> int: explicit_conversion_required
      list[T] -> list[U]: requires_element_compatibility
    }
  }
  
  // Scope system definitions
  scope_system {
    global_scope: module_level
    function_scope: function_parameters_and_locals
    class_scope: class_members
    block_scope: not_supported  // Python doesn't have block scope
    
    name_resolution_order: [local, enclosing, global, builtin]
  }
}
```

---

## 🔄 CORRECTION WORKFLOW

### Phase 1: Error Detection
1. **Lexical Analysis**: StepLexer tokenizes source code and reports lexical errors with precise positions
2. **Syntax Analysis**: StepParser builds AST and reports syntax errors with grammar rule context
3. **Semantic Analysis**: SemanticValidator applies grammar-defined semantic rules and reports violations
4. **Error Unification**: All errors converted to StructuredValidationError format with consistent metadata

### Phase 2: Transformation Planning
1. **Rule Matching**: GrammarDrivenASTMapper finds applicable semantic rules for each error
2. **Candidate Generation**: Multiple transformation candidates generated per error based on fix strategies
3. **Grammar Validation**: Each transformation validated against grammar constraints
4. **Ranking**: Candidates ranked by confidence scores and priority levels from grammar rules

### Phase 3: AST Transformation
1. **Transformation Application**: ASTTransformationEngine applies selected transformations to AST
2. **Real-Time Validation**: StepParser validates each transformation against grammar rules
3. **Constraint Enforcement**: Grammar rules prevent invalid AST modifications
4. **Code Generation**: Corrected source code generated from transformed AST

### Phase 4: Iterative Refinement
1. **Re-Validation**: CorrectionFeedbackLoop re-validates corrected code for remaining errors
2. **Progressive Correction**: Additional corrections applied if needed, with error prioritization
3. **Learning Integration**: Correction patterns analyzed and stored for future improvements
4. **Termination**: Process terminates when code is valid or maximum iterations reached

---

## 📊 GRAMMAR EXTENSION SPECIFICATIONS

### Semantic Rule Definition Syntax

```grammar
semantic_rule <rule_name> {
  pattern: <ast_node_pattern>
  constraint: <semantic_condition>
  scope: <scope_specification>
  error_type: <error_classification>
  message: <error_message_template>
  fix_strategies: [
    {
      strategy: <strategy_name>
      template: <code_template>
      confidence: <confidence_score>
      priority: <priority_level>
      conditions: <application_conditions>
    }
  ]
}
```

### Pattern Matching Syntax
```grammar
// Node type patterns
pattern: identifier_reference
pattern: function_call
pattern: assignment_expression

// Structural patterns
pattern: function_definition -> return_statement
pattern: loop_statement -> break_statement | continue_statement

// Contextual patterns
pattern: identifier_reference[name="undefined_var"]
pattern: function_call[name="print", arg_count > 1]
```

### Constraint Types
```grammar
// Scope constraints
constraint: requires_prior_declaration
constraint: within_function_scope
constraint: within_loop_scope
constraint: creates_local_binding

// Type constraints
constraint: type_compatible(lhs, rhs)
constraint: argument_count_matches(function_signature)
constraint: operand_types_compatible(operator, lhs, rhs)

// Flow constraints
constraint: reachable_code
constraint: all_paths_return
constraint: no_circular_dependency
```

### Fix Strategy Framework
```grammar
// Code insertion strategies
strategy: insert_declaration
strategy: insert_import
strategy: insert_return

// Code modification strategies
strategy: fix_assignment_operator
strategy: normalize_indentation
strategy: add_type_conversion

// Code restructuring strategies
strategy: reorder_statements
strategy: extract_function
strategy: wrap_in_block

// Suggestion strategies
strategy: suggest_alternatives
strategy: suggest_similar_names
strategy: suggest_compatible_types
```

---

## 🎯 IMPLEMENTATION ROADMAP

### Phase 1: Foundation (COMPLETED)
- [x] Basic AST transformation engine
- [x] Error detection framework
- [x] Correction feedback loop
- [x] Integration with existing Minotaur components

### Phase 2: Grammar Integration (IN PROGRESS)
- [ ] Semantic rule definition syntax in grammar files
- [ ] SemanticValidator implementation
- [ ] Grammar-driven error mapping
- [ ] Real-time transformation validation

### Phase 3: Semantic Rule Library (NEXT)
- [ ] Comprehensive Python semantic rules
- [ ] Scope and binding rule implementations
- [ ] Type consistency rule implementations
- [ ] Control flow rule implementations

### Phase 4: Advanced Features (FUTURE)
- [ ] Multi-language semantic rule libraries
- [ ] Machine learning for fix strategy optimization
- [ ] Performance optimization for real-time correction
- [ ] IDE integration capabilities

### Phase 5: Production Readiness (FUTURE)
- [ ] Comprehensive testing framework
- [ ] Performance benchmarking
- [ ] Documentation and examples
- [ ] Build pipeline integration

---

## 🚀 SUCCESS METRICS

### Accuracy Metrics
- **Error Detection Rate**: >95% of actual errors detected
- **Fix Success Rate**: >90% of detected errors correctly fixed
- **False Positive Rate**: <5% incorrect error reports
- **False Negative Rate**: <5% missed actual errors

### Performance Metrics
- **Detection Time**: <50ms for typical source files
- **Correction Time**: <100ms for typical errors
- **Memory Usage**: <100MB for large source files
- **Scalability**: Linear performance with file size

### Genericity Metrics
- **Language Independence**: Zero language-specific code components
- **Grammar Coverage**: Support for all grammar-defined constructs
- **Extensibility**: New languages supported via grammar files only
- **Rule Coverage**: >80% of common error patterns covered by grammar rules

### Quality Metrics
- **Code Quality**: Corrected code maintains original semantics
- **Style Preservation**: Original code style and formatting preserved
- **Readability**: Generated code is human-readable and maintainable
- **Correctness**: All corrections result in syntactically and semantically valid code

---

## 🔗 INTEGRATION COMPONENTS (MISSING - PHASE 4)

### 4.1 Mistral Codestral Integration
**Status**: MISSING - Critical for production deployment

#### MistralAgenticIntegration
```typescript
class MistralAgenticIntegration {
  constructor(
    private mistralClient: MistralAPIClient,
    private agenticSystem: CompleteAgenticSystem
  ) {}
  
  async enhancedCorrection(sourceCode: string): Promise<AgenticCorrectionResult> {
    // Step 1: Try deterministic approaches first
    const deterministicResult = await this.agenticSystem.correctErrors(sourceCode);
    
    // Step 2: Use Mistral for LLM fallback if needed
    if (!deterministicResult.success || deterministicResult.deterministicRatio < 0.7) {
      const mistralSuggestion = await this.mistralClient.generateCorrection(sourceCode);
      return this.hybridCorrection(deterministicResult, mistralSuggestion);
    }
    
    return deterministicResult;
  }
}
```

#### Enhanced Validation System Integration
```typescript
class EnhancedGolemBenchmarkSolver extends GolemBenchmarkSolver {
  constructor(
    private agenticSystem: CompleteAgenticSystem,
    mistralClient: MistralAPIClient
  ) {
    super(mistralClient);
  }
  
  async solveProblem(problem: string): Promise<string> {
    // Use Project Golem instead of pure Mistral
    const result = await this.agenticSystem.correctErrors(problem);
    
    // Fallback to Mistral if Golem fails
    if (!result.success) {
      return super.solveProblem(problem);
    }
    
    return result.correctedCode || '';
  }
}
```

### 4.2 Minotaur CLI Integration
**Status**: MISSING - Required for CLI-based correction

#### Enhanced MinotaurCLI Commands
```typescript
// Add to existing MinotaurCLI
class MinotaurCLI {
  // Existing commands...
  
  @Command('golem-correct')
  async golemCorrect(
    @Option('--file', { description: 'Source file to correct' }) filePath: string,
    @Option('--prompt', { description: 'Prompt file for corrections' }) promptPath?: string,
    @Option('--output', { description: 'Output file path' }) outputPath?: string
  ): Promise<void> {
    // Load source code
    const sourceCode = await fs.readFile(filePath, 'utf-8');
    
    // Load prompt if provided
    const prompt = promptPath ? await fs.readFile(promptPath, 'utf-8') : undefined;
    
    // Initialize agentic system
    const agenticSystem = await this.initializeAgenticSystem();
    
    // Perform correction
    const result = await agenticSystem.correctErrors(sourceCode, 'cli-user');
    
    // Save result
    const outputFile = outputPath || filePath.replace(/\.py$/, '_corrected.py');
    await fs.writeFile(outputFile, result.correctedCode || sourceCode);
    
    console.log(`✅ Correction complete: ${outputFile}`);
  }
  
  @Command('golem-validate')
  async golemValidate(
    @Option('--benchmark', { description: 'Benchmark suite to run' }) benchmark: string = 'python-errors'
  ): Promise<void> {
    const agenticSystem = await this.initializeAgenticSystem();
    const results = await agenticSystem.runBenchmarkTests(benchmark);
    
    console.log(`📊 Benchmark Results:`);
    console.log(`  Success Rate: ${(results.successRate * 100).toFixed(1)}%`);
    console.log(`  Avg Response Time: ${results.averageResponseTime.toFixed(0)}ms`);
    console.log(`  Deterministic Ratio: ${(results.deterministicRatio * 100).toFixed(1)}%`);
  }
}
```

### 4.3 Frontend Integration
**Status**: MISSING - Required for web-based agentic interface

#### Agentic Correction Web Interface
```typescript
// React component for agentic code correction
interface AgenticCorrectionPanelProps {
  initialCode: string;
  onCodeChange: (code: string) => void;
}

const AgenticCorrectionPanel: React.FC<AgenticCorrectionPanelProps> = ({
  initialCode,
  onCodeChange
}) => {
  const [prompt, setPrompt] = useState('');
  const [correctionResult, setCorrectionResult] = useState<AgenticCorrectionResult | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  
  const handleCorrection = async () => {
    setIsProcessing(true);
    try {
      const result = await agenticAPI.correctWithPrompt(initialCode, prompt);
      setCorrectionResult(result);
      if (result.success && result.correctedCode) {
        onCodeChange(result.correctedCode);
      }
    } catch (error) {
      console.error('Correction failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };
  
  return (
    <div className="agentic-correction-panel">
      <div className="prompt-section">
        <textarea
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          placeholder="Describe the changes you want to make..."
          className="prompt-input"
        />
        <button onClick={handleCorrection} disabled={isProcessing}>
          {isProcessing ? 'Processing...' : 'Apply Correction'}
        </button>
      </div>
      
      {correctionResult && (
        <div className="correction-results">
          <div className="metrics">
            <span>Success: {correctionResult.success ? '✅' : '❌'}</span>
            <span>Deterministic: {(correctionResult.deterministicRatio * 100).toFixed(1)}%</span>
            <span>Time: {correctionResult.totalExecutionTime}ms</span>
          </div>
          
          <div className="correction-steps">
            {correctionResult.correctionSteps.map((step, index) => (
              <div key={index} className="correction-step">
                <strong>Step {step.stepNumber}:</strong> {step.description}
                <span className="confidence">({(step.confidence * 100).toFixed(1)}%)</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
```

### 4.4 Prompt File Processing
**Status**: MISSING - Required for file-based prompt handling

#### Prompt File Processor
```typescript
interface PromptInstruction {
  type: 'fix_error' | 'refactor' | 'optimize' | 'add_feature';
  description: string;
  targetLocation?: string;
  parameters?: Record<string, any>;
}

class PromptFileProcessor {
  async processPromptFile(promptPath: string): Promise<PromptInstruction[]> {
    const content = await fs.readFile(promptPath, 'utf-8');
    
    // Support multiple prompt formats
    if (promptPath.endsWith('.json')) {
      return this.parseJSONPrompt(content);
    } else if (promptPath.endsWith('.yaml') || promptPath.endsWith('.yml')) {
      return this.parseYAMLPrompt(content);
    } else {
      return this.parseTextPrompt(content);
    }
  }
  
  private parseTextPrompt(content: string): PromptInstruction[] {
    // Parse natural language prompts
    const lines = content.split('\n').filter(line => line.trim());
    
    return lines.map(line => ({
      type: this.inferPromptType(line),
      description: line.trim(),
      parameters: this.extractParameters(line)
    }));
  }
  
  async applyPromptInstructions(
    sourceCode: string,
    instructions: PromptInstruction[],
    agenticSystem: CompleteAgenticSystem
  ): Promise<string> {
    
    let currentCode = sourceCode;
    
    for (const instruction of instructions) {
      const result = await agenticSystem.correctErrors(
        currentCode,
        'prompt-user',
        `prompt-session-${Date.now()}`
      );
      
      if (result.success && result.correctedCode) {
        currentCode = result.correctedCode;
      }
    }
    
    return currentCode;
  }
}
```

### 4.5 Performance Benchmarking Integration
**Status**: MISSING - Required to prove superiority over Mistral alone

#### Comparative Benchmarking System
```typescript
class ComparativeBenchmarkRunner {
  constructor(
    private agenticSystem: CompleteAgenticSystem,
    private mistralClient: MistralAPIClient
  ) {}
  
  async runComparativeBenchmark(testSuite: string): Promise<BenchmarkComparison> {
    const testCases = await this.loadTestCases(testSuite);
    
    // Run Golem agentic system
    const golemResults = await this.runGolemBenchmark(testCases);
    
    // Run pure Mistral
    const mistralResults = await this.runMistralBenchmark(testCases);
    
    // Compare results
    return this.compareResults(golemResults, mistralResults);
  }
  
  private async runGolemBenchmark(testCases: TestCase[]): Promise<BenchmarkResult> {
    const results = [];
    
    for (const testCase of testCases) {
      const startTime = Date.now();
      const result = await this.agenticSystem.correctErrors(testCase.inputCode);
      const responseTime = Date.now() - startTime;
      
      results.push({
        testId: testCase.id,
        success: result.success,
        responseTime,
        deterministicRatio: result.deterministicRatio,
        approach: 'golem_agentic'
      });
    }
    
    return this.aggregateResults(results, 'golem');
  }
  
  private async runMistralBenchmark(testCases: TestCase[]): Promise<BenchmarkResult> {
    const results = [];
    
    for (const testCase of testCases) {
      const startTime = Date.now();
      const result = await this.mistralClient.generateCorrection(testCase.inputCode);
      const responseTime = Date.now() - startTime;
      
      results.push({
        testId: testCase.id,
        success: this.evaluateCorrection(result, testCase.expectedOutput),
        responseTime,
        deterministicRatio: 0, // Pure LLM is non-deterministic
        approach: 'mistral_pure'
      });
    }
    
    return this.aggregateResults(results, 'mistral');
  }
}
```

## 📚 REFERENCES AND DEPENDENCIES

### Minotaur Framework Components
- **StepLexer**: Tokenization with position tracking
- **StepParser**: Interactive AST construction
- **Grammar System**: Language definition framework
- **ZeroCopyASTNode**: Memory-efficient AST representation
- **MistralAPIClient**: Existing Mistral Codestral integration
- **GolemBenchmarkSolver**: Current validation system
- **MinotaurCLI**: Existing command-line interface

### External Dependencies
- **TypeScript**: Primary implementation language
- **Jest**: Testing framework
- **ESLint**: Code quality enforcement
- **Node.js**: Runtime environment
- **React**: Frontend framework (for web interface)
- **Commander**: CLI framework enhancement

### Related Documentation
- [Minotaur Grammar System Documentation](./grammars/)
- [AST Transformation Guide](./TRANSFORMATION_RULE_SYSTEM_OVERVIEW.md)
- [Interactive Parser Documentation](./TECHNICAL_SPECIFICATIONS.md)
- [Code Interpreter Technical Specification](./CodeInterpreter_TechnicalDesignSpec_Updated.md)
- [Mistral API Integration Guide](./MISTRAL_INTEGRATION.md)

---

## 📝 APPENDICES

### Appendix A: Error Type Classifications
```typescript
enum ErrorType {
  // Lexical errors
  LEXICAL_ERROR = 'lexical_error',
  INVALID_TOKEN = 'invalid_token',
  
  // Syntax errors
  SYNTAX_ERROR = 'syntax_error',
  MISSING_TOKEN = 'missing_token',
  UNEXPECTED_TOKEN = 'unexpected_token',
  
  // Semantic errors
  NAME_ERROR = 'name_error',
  TYPE_ERROR = 'type_error',
  SCOPE_ERROR = 'scope_error',
  
  // Import errors
  IMPORT_ERROR = 'import_error',
  MODULE_NOT_FOUND_ERROR = 'module_not_found_error',
  
  // Control flow errors
  UNREACHABLE_CODE = 'unreachable_code',
  MISSING_RETURN = 'missing_return',
  
  // Style errors
  INDENTATION_ERROR = 'indentation_error',
  FORMATTING_ERROR = 'formatting_error'
}
```

### Appendix B: Transformation Strategy Catalog
```typescript
enum TransformationStrategy {
  // Insertion strategies
  INSERT_DECLARATION = 'insert_declaration',
  INSERT_IMPORT = 'insert_import',
  INSERT_RETURN = 'insert_return',
  
  // Modification strategies
  FIX_OPERATOR = 'fix_operator',
  FIX_INDENTATION = 'fix_indentation',
  ADD_TYPE_ANNOTATION = 'add_type_annotation',
  
  // Replacement strategies
  REPLACE_IDENTIFIER = 'replace_identifier',
  REPLACE_EXPRESSION = 'replace_expression',
  
  // Restructuring strategies
  REORDER_STATEMENTS = 'reorder_statements',
  EXTRACT_FUNCTION = 'extract_function',
  WRAP_IN_BLOCK = 'wrap_in_block',
  
  // Suggestion strategies
  SUGGEST_ALTERNATIVES = 'suggest_alternatives',
  SUGGEST_SIMILAR_NAMES = 'suggest_similar_names'
}
```

### Appendix C: Grammar Rule Examples
See the implementation files for complete examples of grammar rule definitions and semantic constraint specifications.

---

**Document Status**: Living Document - Updated as implementation progresses  
**Next Review**: Upon completion of Phase 2 (Grammar Integration)  
**Maintainer**: Project Golem Development Team

