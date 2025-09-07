/**
 * MistralAgenticIntegration - Hybrid Correction System
 *
 * Integrates existing MistralAPIClient with Project Golem's agentic system
 * to provide hybrid deterministic-probabilistic error correction.
 *
 * Correction Flow:
 * 1. Grammar Rules (Most Deterministic)
 * 2. Pattern Matching (Mostly Deterministic)
 * 3. ML Prediction (Partially Deterministic)
 * 4. Mistral Codestral (LLM Fallback)
 */

import { MistralAPIClient, MistralRequest } from './MistralAPIClient';
import { AgenticCorrectionInterface, AgenticCorrectionResult, CorrectionStep, CorrectionStepType, DeterminismLevel } from './AgenticCorrectionInterface';
import { 
  StructuredValidationError, 
  ErrorType, 
  ErrorSeverity,
  ErrorFix,
  FixType,
} from './StructuredValidationError';
import { Grammar } from '../core/grammar/Grammar';
import { StepParser } from '../utils/StepParser';
import { StepLexer } from '../utils/StepLexer';
import { LexerOptions } from '../utils/LexerOptions';
import { IParserLexerSourceContainer } from '../utils/IParserLexerSource';
import { AgenticSystem } from './AgenticSystem';

export interface MistralIntegrationConfig {
  // Mistral configuration
  enableMistralFallback: boolean;
  mistralConfidenceThreshold: number;
  mistralTimeoutMs: number;
  mistralMaxRetries: number;

  // Hybrid correction configuration
  deterministicThreshold: number; // When to fallback to Mistral
  hybridMode: 'fallback' | 'parallel' | 'validation';

  // Performance configuration
  enableMistralCaching: boolean;
  enablePerformanceComparison: boolean;
  logMistralUsage: boolean;
}

export interface SolutionGenerationResult {
  success: boolean;
  generatedCode: string;
  confidence: number;
  generationTime: number;
  error?: string;
  metadata: {
    model?: string;
    tokens?: number;
    approach: string;
  };
}

export interface CorrectionAttemptResult {
  success: boolean;
  correctedCode: string;
  confidence: number;
  executionTime: number;
  errorsFixed: number;
  remainingErrors: number;
}

export interface HybridCorrectionResult extends AgenticCorrectionResult {
  mistralUsed: boolean;
  mistralConfidence: number;
  mistralResponseTime: number;
  hybridApproach: string;
  performanceComparison?: {
    deterministicTime: number;
    mistralTime: number;
    hybridTime: number;
    accuracyComparison: number;
  };
}

export interface MistralCorrectionAttempt {
  sourceCode: string;
  error: StructuredValidationError;
  mistralSuggestion: string;
  confidence: number;
  responseTime: number;
  success: boolean;
}

/**
 * MistralAgenticIntegration - Core hybrid correction system
 */
export class MistralAgenticIntegration {
  private config: MistralIntegrationConfig;
  private mistralCache: Map<string, MistralCorrectionAttempt>;
  private agenticSystem: AgenticSystem; // Properly typed
  private performanceMetrics: {
    totalCorrections: number;
    mistralUsageCount: number;
    hybridSuccessRate: number;
    averageResponseTime: number;
  };

  constructor(
    private mistralClient: MistralAPIClient,
    config: Partial<MistralIntegrationConfig> = {},
  ) {
    this.config = {
      enableMistralFallback: true,
      mistralConfidenceThreshold: 0.7,
      mistralTimeoutMs: 10000,
      mistralMaxRetries: 2,
      deterministicThreshold: 0.5,
      hybridMode: 'fallback',
      enableMistralCaching: true,
      enablePerformanceComparison: true,
      logMistralUsage: true,
      ...config,
    };

    this.mistralCache = new Map();
    
    // Initialize the agentic system properly with required arguments
    // Create minimal grammar, parser, and lexer instances for the agentic system
    const grammar = new Grammar('minotaur-agentic');
    const stepParser = new StepParser();
    const lexerOptions = new LexerOptions(false, false);
    const sourceLinesContainer: IParserLexerSourceContainer = { 
      getSourceLines: () => [],
      getCount: () => 0,
      getLine: (fileName: string, lineNumber: number) => ({
        getContent: () => '',
        getLength: () => 0,
        getLineNumber: () => lineNumber,
        getFileName: () => fileName,
      }),
    };
    const stepLexer = new StepLexer(stepParser, lexerOptions, sourceLinesContainer);
    
    this.agenticSystem = new AgenticSystem(
      grammar,
      stepParser, 
      stepLexer,
      {
        enableFullIntegration: true,
        enableRealTimeOptimization: true,
        enableAdvancedPatternMatching: true,
        enableAdaptiveLearning: false, // Disable for now to avoid complexity
        maxConcurrentCorrections: 1,
        responseTimeTarget: 5000,
        memoryLimitMB: 512,
        cacheOptimization: true,
        enableManualTesting: false,
        enableBenchmarkTesting: true,
        enablePerformanceProfiling: false,
        testDataPath: '',
        agenticConfig: {},
        learningConfig: {},
        patternConfig: {},
        enableProductionMode: true,
        enableDetailedLogging: false,
        enableMetricsCollection: true,
        enableErrorReporting: true,
      },
    );
    
    this.performanceMetrics = {
      totalCorrections: 0,
      mistralUsageCount: 0,
      hybridSuccessRate: 0,
      averageResponseTime: 0,
    };
    
    // Initialize the agentic system
    this.initializeAgenticSystem();
  }

  /**
   * Initialize the agentic system asynchronously
   */
  private async initializeAgenticSystem(): Promise<void> {
    try {
      await this.agenticSystem.initialize();
    } catch (error) {
      console.warn('Failed to initialize agentic system:', error);
      // Create a fallback null object to prevent crashes
      this.agenticSystem = {
        correctErrors: async () => ({
          success: false,
          correctedCode: '',
          confidence: 0,
          correctionSteps: [],
          deterministicLevel: 'none' as DeterminismLevel,
          executionTime: 0,
          metadata: { approach: 'fallback', rulesApplied: [], patternsMatched: [] },
        }),
      } as any;
    }
  }

  /**
   * Generate initial solution from problem prompt (Step 1)
   */
  async generateSolution(
    problemPrompt: string,
    userId?: string,
    sessionId?: string,
  ): Promise<SolutionGenerationResult> {
    const startTime = Date.now();
    
    try {
      // Create solution generation prompt
      const prompt = this.createSolutionPrompt(problemPrompt);
      const solutionRequest: MistralRequest = {
        model: 'codestral-latest',
        messages: [{ role: 'user', content: prompt }],
        temperature: 0.1,
        max_tokens: 4000,
      };

      // Generate initial solution
      const mistralResult = await Promise.race([
        this.mistralClient.generateCompletion(solutionRequest),
        this.createTimeoutPromise(this.config.mistralTimeoutMs),
      ]);

      const generatedCode = mistralResult.response?.choices[0]?.message?.content || '';
      const executionTime = Date.now() - startTime;

      return {
        success: generatedCode.length > 0,
        generatedCode: this.cleanGeneratedCode(generatedCode),
        confidence: this.evaluateMistralConfidence(generatedCode, problemPrompt),
        generationTime: executionTime,
        metadata: {
          model: 'codestral-latest',
          tokens: mistralResult.response?.usage?.total_tokens || 0,
          approach: 'initial_generation',
        },
      };

    } catch (error) {
      return {
        success: false,
        generatedCode: '',
        confidence: 0,
        generationTime: Date.now() - startTime,
        error: error instanceof Error ? error.message : 'Unknown error',
        metadata: { approach: 'failed_generation' },
      };
    }
  }

  /**
   * Enhanced solution generation with proper 3-step workflow
   */
  async enhancedSolutionWithCorrection(
    problemPrompt: string,
    userId?: string,
    sessionId?: string,
  ): Promise<HybridCorrectionResult> {
    const startTime = Date.now();
    
    try {
      // Check if this is a Java problem - skip for now
      if (this.isJavaProblem(problemPrompt)) {
        return this.createFailedResult('Java problems not supported yet - Python only', startTime);
      }

      // Detect if this is existing code correction vs new code generation
      const isExistingCode = this.isExistingCodeCorrection(problemPrompt);
      
      let solutionResult: SolutionGenerationResult;
      let initialCode: string;
      
      if (isExistingCode) {
        // Skip Step 1 - we have existing code to correct
        // Extract the existing code from the prompt
        initialCode = this.extractExistingCode(problemPrompt);
        solutionResult = {
          success: true,
          generatedCode: initialCode,
          confidence: 1.0, // High confidence since it's existing code
          generationTime: 0,
          metadata: {
            approach: 'existing_code_correction',
          },
        };
      } else {
        // Step 1: Generate initial solution for new code
        solutionResult = await this.generateSolution(problemPrompt, userId, sessionId);
        
        if (!solutionResult.success || !solutionResult.generatedCode) {
          return this.createFailedResult('Solution generation failed', startTime);
        }
        initialCode = solutionResult.generatedCode;
      }

      // Step 2a: Validate the code using Step-Lexer, Step-Parser, AST (syntax only, no execution)
      const validationErrors = await this.validateGeneratedSolution(initialCode);
      
      if (validationErrors.length === 0) {
        // Code is syntactically valid, no correction needed
        return this.createSuccessResult(solutionResult, [], startTime);
      }

      // Step 2b: Attempt corrections using MultiSolutionCorrector & BenchmarkSolver
      const golemResult = await this.attemptGolemCorrections(initialCode, validationErrors);
      
      if (golemResult.success) {
        // Re-validate the corrected code
        const remainingErrors = await this.validateGeneratedSolution(golemResult.correctedCode);
        if (remainingErrors.length === 0) {
          // Golem correction succeeded
          const correctionStep: CorrectionStep = {
            stepNumber: 1,
            stepType: CorrectionStepType.GRAMMAR_RULE_APPLICATION,
            description: 'Golem agentic system correction',
            input: initialCode,
            output: golemResult.correctedCode,
            confidence: golemResult.confidence,
            executionTime: 0,
            determinismLevel: DeterminismLevel.MOSTLY_DETERMINISTIC,
            reasoning: 'Applied deterministic corrections via agentic system',
          };
          
          return this.createSuccessResult(solutionResult, [correctionStep], startTime);
        }
      }

      // Step 3: Iterative LLM-assisted correction (up to 3 attempts)
      let currentCode = golemResult.correctedCode || initialCode;
      const correctionSteps: CorrectionStep[] = [];
      let currentErrors = validationErrors;
      let attempt = 0;
      const maxAttempts = 3;

      while (attempt < maxAttempts && currentErrors.length > 0) {
        attempt++;
        
        const correctionResult = await this.correctSolutionErrors(
          currentCode,
          currentErrors,
          problemPrompt,
          attempt,
        );

        correctionSteps.push({
          stepNumber: attempt,
          stepType: CorrectionStepType.LLM_GENERATION,
          description: `LLM error correction attempt ${attempt}`,
          input: currentCode,
          output: correctionResult.correctedCode,
          confidence: correctionResult.confidence,
          executionTime: correctionResult.executionTime,
          determinismLevel: DeterminismLevel.NON_DETERMINISTIC,
          reasoning: `Correcting ${currentErrors.length} validation errors with LLM assistance`,
        });

        if (correctionResult.success && correctionResult.errorsFixed > 0) {
          currentCode = correctionResult.correctedCode;
          currentErrors = await this.validateGeneratedSolution(currentCode);
          
          if (currentErrors.length === 0) {
            break; // Success!
          }
        } else {
          break; // Correction failed or no progress
        }
      }

      return this.createCorrectionResult(
        solutionResult,
        currentCode,
        correctionSteps,
        currentErrors.length === 0,
        startTime,
      );

    } catch (error) {
      return this.createFailedResult(
        error instanceof Error ? error.message : 'Unknown error',
        startTime,
      );
    }
  }

  /**
   * Detect if this is a Java problem (skip for now - Python only)
   */
  private isJavaProblem(prompt: string): boolean {
    const javaPatterns = [
      /\bjava\b/i,
      /\.java\b/,
      /public\s+class\s+\w+/,
      /public\s+static\s+void\s+main/,
      /System\.out\.print/,
      /import\s+java\./,
      /package\s+\w+/,
      /```java/i,
      /write.*java/i,
      /implement.*java/i,
    ];
    
    return javaPatterns.some(pattern => pattern.test(prompt));
  }

  /**
   * Detect if this is existing code correction vs new code generation
   */
  private isExistingCodeCorrection(prompt: string): boolean {
    // Look for patterns that indicate existing code to be corrected
    const correctionPatterns = [
      /fix.*error/i,
      /correct.*code/i,
      /debug.*following/i,
      /there.*error.*in/i,
      /the.*code.*has.*error/i,
      /```python[\s\S]*```/,  // Contains code blocks
      /def\s+\w+\s*\(/,       // Contains function definitions
      /class\s+\w+/,          // Contains class definitions
    ];
    
    return correctionPatterns.some(pattern => pattern.test(prompt));
  }

  /**
   * Extract existing code from correction prompts
   */
  private extractExistingCode(prompt: string): string {
    // Try to extract code from markdown blocks first
    const codeBlockMatch = prompt.match(/```python\s*([\s\S]*?)\s*```/);
    if (codeBlockMatch) {
      return codeBlockMatch[1].trim();
    }
    
    // Try to extract code from indented blocks
    const lines = prompt.split('\n');
    const codeLines: string[] = [];
    let inCodeBlock = false;
    
    for (const line of lines) {
      // Detect start of code block (function/class definition or significant indentation)
      if (/^\s*(def|class|import|from)\s/.test(line) || (line.trim().length > 0 && line.startsWith('    '))) {
        inCodeBlock = true;
      }
      
      if (inCodeBlock) {
        codeLines.push(line);
        // Stop if we hit a line that looks like explanation text
        if (line.trim().length > 0 && !line.startsWith('    ') && !line.startsWith('\t') && 
            !/^\s*(def|class|import|from|if|for|while|try|except|with|return|print)/.test(line)) {
          break;
        }
      }
    }
    
    return codeLines.join('\n').trim() || prompt; // Fallback to full prompt if extraction fails
  }

  /**
   * Create solution generation prompt (Step 1) - improved to prevent JavaScript-like syntax
   */
  private createSolutionPrompt(problemPrompt: string): string {
    return `${problemPrompt}

IMPORTANT: You are a Python code generator. Follow these rules strictly:

1. Write ONLY Python code - no JavaScript, TypeScript, or other languages
2. Use Python syntax: 'None' not 'undefined', 'True/False' not 'true/false'
3. Use Python conventions: snake_case for variables, not camelCase
4. Do not include explanations, comments about the task, or markdown formatting
5. Return only executable Python code that can be run directly
6. Ensure all variables are properly defined before use
7. Use proper Python indentation (4 spaces)

Provide the complete, working Python implementation:`;
  }

  /**
   * Clean generated code by removing markdown and extra text
   */
  private cleanGeneratedCode(generatedCode: string): string {
    // Remove markdown code blocks
    let cleaned = generatedCode.replace(/```python\s*/g, '').replace(/```\s*/g, '');
    
    // Remove common prefixes/suffixes
    cleaned = cleaned.replace(/^Here's the solution:?\s*/i, '');
    cleaned = cleaned.replace(/^The solution is:?\s*/i, '');
    cleaned = cleaned.replace(/^Here's.*implementation:?\s*/i, '');
    
    // Fix JavaScript-like syntax to Python
    cleaned = cleaned.replace(/\bundefined\b/g, 'None');
    cleaned = cleaned.replace(/\btrue\b/g, 'True');
    cleaned = cleaned.replace(/\bfalse\b/g, 'False');
    cleaned = cleaned.replace(/\bnull\b/g, 'None');
    
    // Fix common JavaScript patterns
    cleaned = cleaned.replace(/let\s+(\w+)/g, '$1');
    cleaned = cleaned.replace(/const\s+(\w+)/g, '$1');
    cleaned = cleaned.replace(/var\s+(\w+)/g, '$1');
    
    // Fix indentation issues
    cleaned = this.fixIndentation(cleaned);
    
    // Trim whitespace
    return cleaned.trim();
  }

  /**
   * Fix Python indentation issues
   */
  private fixIndentation(code: string): string {
    const lines = code.split('\n');
    const fixedLines: string[] = [];
    let currentIndentLevel = 0;
    let insideFunction = false;
    let insideClass = false;
    
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmedLine = line.trim();
      
      // Skip empty lines
      if (trimmedLine === '') {
        fixedLines.push('');
        continue;
      }
      
      // Detect function/class definitions
      if (trimmedLine.startsWith('def ')) {
        insideFunction = true;
        currentIndentLevel = 0;
        fixedLines.push(trimmedLine);
        continue;
      }
      
      if (trimmedLine.startsWith('class ')) {
        insideClass = true;
        currentIndentLevel = 0;
        fixedLines.push(trimmedLine);
        continue;
      }
      
      // Handle control structures that increase indentation
      if (trimmedLine.endsWith(':') && 
          (trimmedLine.startsWith('if ') || trimmedLine.startsWith('elif ') || 
           trimmedLine.startsWith('else:') || trimmedLine.startsWith('for ') || 
           trimmedLine.startsWith('while ') || trimmedLine.startsWith('try:') || 
           trimmedLine.startsWith('except') || trimmedLine.startsWith('finally:') || 
           trimmedLine.startsWith('with '))) {
        
        const indent = '    '.repeat(currentIndentLevel);
        fixedLines.push(indent + trimmedLine);
        currentIndentLevel++;
        continue;
      }
      
      // Handle return, break, continue, pass statements
      if (trimmedLine.startsWith('return ') || trimmedLine.startsWith('break') || 
          trimmedLine.startsWith('continue') || trimmedLine.startsWith('pass') ||
          trimmedLine.startsWith('raise ')) {
        
        const indent = '    '.repeat(Math.max(0, currentIndentLevel));
        fixedLines.push(indent + trimmedLine);
        continue;
      }
      
      // Handle dedentation keywords
      if (trimmedLine.startsWith('elif ') || trimmedLine.startsWith('else:') || 
          trimmedLine.startsWith('except') || trimmedLine.startsWith('finally:')) {
        currentIndentLevel = Math.max(0, currentIndentLevel - 1);
        const indent = '    '.repeat(currentIndentLevel);
        fixedLines.push(indent + trimmedLine);
        currentIndentLevel++;
        continue;
      }
      
      // Regular statements inside functions/classes
      if (insideFunction || insideClass) {
        const indent = '    '.repeat(Math.max(1, currentIndentLevel));
        fixedLines.push(indent + trimmedLine);
      } else {
        // Top-level statements
        fixedLines.push(trimmedLine);
      }
    }
    
    return fixedLines.join('\n');
  }

  /**
   * Validate generated solution using Step-Lexer, Step-Parser, AST (Step 2a)
   * IMPORTANT: Only validates syntax, does NOT execute the code
   * Enhanced to catch indentation, syntax, and other Python errors
   */
  private async validateGeneratedSolution(code: string): Promise<StructuredValidationError[]> {
    try {
      // Use the detected Python command from BenchmarkValidator
      const pythonCmd = 'python3'; // Will be updated to use detected command
      const { spawn } = require('child_process');
      
      return new Promise((resolve) => {
        // Enhanced validation script that catches more error types
        const validationScript = `
import ast
import sys
import traceback
import tokenize
import io

def validate_python_code(code_str):
    errors = []
    
    try:
        # First, try tokenization to catch basic syntax issues
        try:
            tokens = list(tokenize.generate_tokens(io.StringIO(code_str).readline))
        except tokenize.TokenError as e:
            errors.append(f"TOKENIZE_ERROR:1:1:{str(e)}")
            return errors
        except IndentationError as e:
            errors.append(f"INDENTATION_ERROR:{e.lineno or 1}:{e.offset or 1}:{e.msg or 'Indentation error'}")
            return errors
        
        # Then try AST parsing for deeper syntax validation
        try:
            tree = ast.parse(code_str)
            
            # Additional semantic checks
            class CodeValidator(ast.NodeVisitor):
                def __init__(self):
                    self.errors = []
                    self.defined_names = set()
                    self.used_names = set()
                
                def visit_FunctionDef(self, node):
                    self.defined_names.add(node.name)
                    # Check for empty function bodies
                    if not node.body or (len(node.body) == 1 and isinstance(node.body[0], ast.Pass)):
                        pass  # Empty functions are okay for templates
                    self.generic_visit(node)
                
                def visit_Name(self, node):
                    if isinstance(node.ctx, ast.Load):
                        self.used_names.add(node.id)
                    elif isinstance(node.ctx, ast.Store):
                        self.defined_names.add(node.id)
                    self.generic_visit(node)
                
                def visit_Return(self, node):
                    # Check for return statements outside functions
                    # This is a simplified check - more sophisticated analysis needed
                    self.generic_visit(node)
            
            validator = CodeValidator()
            validator.visit(tree)
            
            # Check for undefined variables (basic check)
            undefined_vars = validator.used_names - validator.defined_names - {
                'len', 'range', 'str', 'int', 'float', 'list', 'dict', 'set', 'tuple',
                'print', 'input', 'open', 'max', 'min', 'sum', 'abs', 'round',
                'True', 'False', 'None', 'type', 'isinstance', 'hasattr', 'getattr',
                'enumerate', 'zip', 'map', 'filter', 'sorted', 'reversed'
            }
            
            for var in undefined_vars:
                if not var.startswith('__'):  # Skip dunder variables
                    errors.append(f"UNDEFINED_VAR:1:1:Name '{var}' may not be defined")
            
        except SyntaxError as e:
            errors.append(f"SYNTAX_ERROR:{e.lineno or 1}:{e.offset or 1}:{e.msg or 'Syntax error'}")
        except IndentationError as e:
            errors.append(f"INDENTATION_ERROR:{e.lineno or 1}:{e.offset or 1}:{e.msg or 'Indentation error'}")
        except Exception as e:
            errors.append(f"PARSE_ERROR:1:1:{str(e)}")
    
    except Exception as e:
        errors.append(f"VALIDATION_ERROR:1:1:{str(e)}")
    
    return errors

# Main validation
code = '''${code.replace(/'/g, "\\'")}'''
validation_errors = validate_python_code(code)

if not validation_errors:
    print("SYNTAX_VALID")
else:
    for error in validation_errors:
        print(error)
`;
        
        const python = spawn(pythonCmd, ['-c', validationScript]);
        
        let output = '';
        let errorOutput = '';
        
        python.stdout.on('data', (data: any) => {
          output += data.toString();
        });
        
        python.stderr.on('data', (data: any) => {
          errorOutput += data.toString();
        });
        
        python.on('close', (exitCode) => {
          if (output.includes('SYNTAX_VALID')) {
            resolve([]); // No syntax errors
          } else {
            const lines = (output + errorOutput).trim().split('\n');
            const errors: StructuredValidationError[] = [];
            
            for (const line of lines) {
              if (line.includes('_ERROR:')) {
                const parts = line.split(':');
                const errorType = parts[0];
                const lineNum = parseInt(parts[1]) || 1;
                const colNum = parseInt(parts[2]) || 1;
                const message = parts.slice(3).join(':') || 'Unknown error';
                
                let severity = ErrorSeverity.CRITICAL;
                let type = ErrorType.SYNTAX_ERROR;
                
                if (errorType.includes('INDENTATION')) {
                  type = ErrorType.SYNTAX_ERROR;
                  severity = ErrorSeverity.CRITICAL;
                } else if (errorType.includes('UNDEFINED_VAR')) {
                  type = ErrorType.SEMANTIC_ERROR;
                  severity = ErrorSeverity.HIGH;
                } else if (errorType.includes('TOKENIZE')) {
                  type = ErrorType.SYNTAX_ERROR;
                  severity = ErrorSeverity.CRITICAL;
                }
                
                errors.push({
                  id: `${errorType.toLowerCase()}-${Date.now()}-${Math.random()}`,
                  type: type,
                  severity: severity,
                  message: message,
                  originalMessage: message,
                  location: {
                    line: lineNum,
                    column: colNum,
                  },
                  context: {
                    sourceCode: code,
                    errorLine: code.split('\n')[lineNum - 1] || '',
                    surroundingLines: this.getSurroundingLines(code, lineNum),
                  },
                  suggestedFixes: this.generateSuggestedFixes(errorType, message),
                  timestamp: new Date(),
                });
              }
            }
            
            resolve(errors.length > 0 ? errors : [{
              id: `validation-${Date.now()}`,
              type: ErrorType.SYNTAX_ERROR,
              severity: ErrorSeverity.CRITICAL,
              message: 'Unknown syntax validation error',
              originalMessage: 'Unknown syntax validation error',
              location: {
                line: 1,
                column: 1,
              },
              context: {
                sourceCode: code,
                errorLine: '',
                surroundingLines: [],
              },
              suggestedFixes: [],
              timestamp: new Date(),
            }]);
          }
        });
      });
    } catch (error) {
      return [{
        id: `validation-error-${Date.now()}`,
        type: ErrorType.SYNTAX_ERROR,
        severity: ErrorSeverity.CRITICAL,
        message: error instanceof Error ? error.message : 'Validation failed',
        originalMessage: error instanceof Error ? error.message : 'Validation failed',
        location: {
          line: 1,
          column: 1,
        },
        context: {
          sourceCode: code,
          errorLine: '',
          surroundingLines: [],
        },
        suggestedFixes: [],
        timestamp: new Date(),
      }];
    }
  }

  /**
   * Get surrounding lines for error context
   */
  private getSurroundingLines(code: string, errorLine: number): string[] {
    const lines = code.split('\n');
    const start = Math.max(0, errorLine - 3);
    const end = Math.min(lines.length, errorLine + 2);
    return lines.slice(start, end);
  }

  /**
   * Generate suggested fixes based on error type
   */
  private generateSuggestedFixes(errorType: string, message: string): ErrorFix[] {
    const fixes: ErrorFix[] = [];
    
    if (errorType.includes('INDENTATION')) {
      fixes.push({
        id: `indentation_fix_${Date.now()}`,
        description: 'Fix indentation - use 4 spaces per level',
        confidence: 0.9,
        fixType: FixType.INDENTATION_FIX,
        estimatedImpact: 0.1,
      });
      fixes.push({
        id: `indentation_consistency_${Date.now()}`,
        description: 'Ensure consistent indentation throughout the code',
        confidence: 0.8,
        fixType: FixType.INDENTATION_FIX,
        estimatedImpact: 0.2,
      });
      fixes.push({
        id: `tab_space_fix_${Date.now()}`,
        description: 'Check for mixing tabs and spaces',
        confidence: 0.7,
        fixType: FixType.INDENTATION_FIX,
        estimatedImpact: 0.1,
      });
    } else if (errorType.includes('UNDEFINED_VAR')) {
      fixes.push({
        id: `variable_declaration_${Date.now()}`,
        description: 'Define the variable before using it',
        confidence: 0.8,
        fixType: FixType.VARIABLE_DECLARATION,
        estimatedImpact: 0.3,
      });
      fixes.push({
        id: `typo_check_${Date.now()}`,
        description: 'Check for typos in variable names',
        confidence: 0.6,
        fixType: FixType.SYNTAX_CORRECTION,
        estimatedImpact: 0.2,
      });
      fixes.push({
        id: `import_module_${Date.now()}`,
        description: 'Import required modules if using external functions',
        confidence: 0.7,
        fixType: FixType.IMPORT_ADDITION,
        estimatedImpact: 0.2,
      });
    } else if (errorType.includes('SYNTAX')) {
      fixes.push({
        id: `syntax_punctuation_${Date.now()}`,
        description: 'Check for missing colons, parentheses, or quotes',
        confidence: 0.8,
        fixType: FixType.SYNTAX_CORRECTION,
        estimatedImpact: 0.3,
      });
      fixes.push({
        id: `python_syntax_${Date.now()}`,
        description: 'Verify proper Python syntax',
        confidence: 0.7,
        fixType: FixType.SYNTAX_CORRECTION,
        estimatedImpact: 0.4,
      });
      fixes.push({
        id: `invalid_chars_${Date.now()}`,
        description: 'Check for invalid characters or operators',
        confidence: 0.6,
        fixType: FixType.SYNTAX_CORRECTION,
        estimatedImpact: 0.2,
      });
    }
    
    return fixes;
  }

  /**
   * Attempt corrections using MultiSolutionCorrector & BenchmarkSolver (Step 2b)
   */
  private async attemptGolemCorrections(
    code: string,
    errors: StructuredValidationError[],
  ): Promise<{ success: boolean; correctedCode: string; confidence: number }> {
    try {
      // Try agentic system correction first
      if (this.agenticSystem) {
        const result = await this.agenticSystem.correctErrors(code, 'system', 'validation');
        if (result.success && result.correctedCode) {
          return {
            success: true,
            correctedCode: result.correctedCode,
            confidence: 0.8, // Default confidence since AgenticCorrectionResult doesn't have confidence
          };
        }
      }

      // Fallback: return original code
      return {
        success: false,
        correctedCode: code,
        confidence: 0,
      };
    } catch (error) {
      return {
        success: false,
        correctedCode: code,
        confidence: 0,
      };
    }
  }

  /**
   * LLM-assisted iterative correction (Step 3)
   */
  private async correctSolutionErrors(
    code: string,
    errors: StructuredValidationError[],
    originalPrompt: string,
    attempt: number,
  ): Promise<CorrectionAttemptResult> {
    const startTime = Date.now();
    
    try {
      const errorDescriptions = errors.map(e => 
        `Line ${e.location.line}: ${e.message} (${e.type})`,
      ).join('\n');

      const correctionPrompt = `The following Python code has errors:

\`\`\`python
${code}
\`\`\`

Errors found:
${errorDescriptions}

Original problem: ${originalPrompt}

Please fix these errors and return only the corrected Python code. Maintain the original logic and structure.`;

      const correctionRequest: MistralRequest = {
        model: 'codestral-latest',
        messages: [{ role: 'user', content: correctionPrompt }],
        temperature: 0.1,
        max_tokens: 4000,
      };

      const mistralResult = await Promise.race([
        this.mistralClient.generateCompletion(correctionRequest),
        this.createTimeoutPromise(this.config.mistralTimeoutMs),
      ]);

      const correctedCode = this.cleanGeneratedCode(
        mistralResult.response?.choices[0]?.message?.content || '',
      );

      // Validate the corrected code
      const remainingErrors = await this.validateGeneratedSolution(correctedCode);
      const errorsFixed = Math.max(0, errors.length - remainingErrors.length);

      return {
        success: correctedCode.length > 0 && remainingErrors.length < errors.length,
        correctedCode: correctedCode || code,
        confidence: this.evaluateMistralConfidence(correctedCode, originalPrompt),
        executionTime: Date.now() - startTime,
        errorsFixed,
        remainingErrors: remainingErrors.length,
      };

    } catch (error) {
      return {
        success: false,
        correctedCode: code,
        confidence: 0,
        executionTime: Date.now() - startTime,
        errorsFixed: 0,
        remainingErrors: errors.length,
      };
    }
  }

  /**
   * Create result helper methods
   */
  private createSuccessResult(
    solutionResult: SolutionGenerationResult,
    correctionSteps: CorrectionStep[],
    startTime: number,
  ): HybridCorrectionResult {
    return {
      success: true,
      correctedCode: solutionResult.generatedCode,
      correctionSteps,
      finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
      totalLLMCalls: 1 + correctionSteps.length,
      totalExecutionTime: Date.now() - startTime,
      deterministicRatio: 0,
      enhancedResult: {} as any,
      agenticMetrics: {} as any,
      mistralUsed: true,
      mistralConfidence: solutionResult.confidence,
      mistralResponseTime: solutionResult.generationTime,
      hybridApproach: 'solution_generation_success',
    };
  }

  private createCorrectionResult(
    solutionResult: SolutionGenerationResult,
    finalCode: string,
    correctionSteps: CorrectionStep[],
    success: boolean,
    startTime: number,
  ): HybridCorrectionResult {
    return {
      success,
      correctedCode: finalCode,
      correctionSteps,
      finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
      totalLLMCalls: 1 + correctionSteps.length,
      totalExecutionTime: Date.now() - startTime,
      deterministicRatio: 0,
      enhancedResult: {} as any,
      agenticMetrics: {} as any,
      mistralUsed: true,
      mistralConfidence: solutionResult.confidence,
      mistralResponseTime: solutionResult.generationTime,
      hybridApproach: success ? 'solution_with_corrections' : 'solution_correction_failed',
    };
  }

  private createFailedResult(error: string, startTime: number): HybridCorrectionResult {
    return {
      success: false,
      correctedCode: '',
      correctionSteps: [],
      finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
      totalLLMCalls: 0,
      totalExecutionTime: Date.now() - startTime,
      deterministicRatio: 0,
      enhancedResult: {} as any,
      agenticMetrics: {} as any,
      mistralUsed: false,
      mistralConfidence: 0,
      mistralResponseTime: 0,
      hybridApproach: 'failed',
    };
  }

  /**
   * Enhanced correction with Mistral integration - now uses proper 3-step workflow
   */
  async enhancedCorrection(
    sourceCode: string,
    userId?: string,
    sessionId?: string,
  ): Promise<HybridCorrectionResult> {
    // Use the new 3-step workflow
    return this.enhancedSolutionWithCorrection(sourceCode, userId, sessionId);
  }

  /**
   * Legacy enhanced correction method - kept for backward compatibility

    const startTime = Date.now();
    this.performanceMetrics.totalCorrections++;

    try {
      // Ensure agentic system is initialized
      if (!this.agenticSystem) {
        console.warn('Agentic system not initialized, using Mistral-only approach');
        return await this.mistralOnlyCorrection(sourceCode, startTime);
      }

      // Step 1: Try deterministic approaches first (Golem agentic system)
      const deterministicResult = await this.agenticSystem.correctErrors(
        sourceCode,
        userId,
        sessionId,
      );

      const deterministicTime = Date.now() - startTime;

      // Step 2: Evaluate if Mistral fallback is needed
      const needsMistralFallback = this.shouldUseMistralFallback(deterministicResult);

      if (!needsMistralFallback) {
        // Deterministic approach succeeded
        return this.createHybridResult(
          deterministicResult,
          false,
          0,
          0,
          'deterministic_only',
          { deterministicTime, mistralTime: 0, hybridTime: deterministicTime, accuracyComparison: 1.0 },
        );
      }

      // Step 3: Apply Mistral fallback or hybrid approach
      const hybridResult = await this.applyMistralIntegration(
        sourceCode,
        deterministicResult,
        deterministicTime,
        userId,
      );

      // Update performance metrics
      this.updatePerformanceMetrics(hybridResult);

      return hybridResult;

    } catch (error) {
      console.error('❌ Hybrid correction failed:', error);

      // Fallback to deterministic result if available
      try {
        const fallbackResult = await this.agenticSystem.correctErrors(sourceCode, userId, sessionId);
        return this.createHybridResult(
          fallbackResult,
          false,
          0,
          0,
          'error_fallback',
        );
      } catch (fallbackError) {
        throw new Error(`Hybrid correction completely failed: ${error.message}`);
      }
    }
  }

  /**
   * Determine if Mistral fallback is needed
   */
  private shouldUseMistralFallback(result: AgenticCorrectionResult): boolean {
    // Use Mistral if:
    // 1. Correction failed completely
    if (!result.success) {
return true;
}

    // 2. Deterministic ratio is too low
    if (result.deterministicRatio < this.config.deterministicThreshold) {
return true;
}

    // 3. No corrected code produced
    if (!result.correctedCode) {
return true;
}

    // 4. Too many LLM calls already (indicates deterministic failure)
    if (result.totalLLMCalls > 3) {
return true;
}

    return false;
  }

  /**
   * Apply Mistral integration based on hybrid mode
   */
  private async applyMistralIntegration(
    sourceCode: string,
    deterministicResult: AgenticCorrectionResult,
    deterministicTime: number,
    userId?: string,
  ): Promise<HybridCorrectionResult> {

    switch (this.config.hybridMode) {
      case 'fallback':
        return await this.applyMistralFallback(sourceCode, deterministicResult, deterministicTime, userId);

      case 'parallel':
        return await this.applyParallelCorrection(sourceCode, deterministicResult, deterministicTime, userId);

      case 'validation':
        return await this.applyMistralValidation(sourceCode, deterministicResult, deterministicTime, userId);

      default:
        return await this.applyMistralFallback(sourceCode, deterministicResult, deterministicTime, userId);
    }
  }

  /**
   * Mistral fallback mode - use Mistral when deterministic approaches fail
   */
  private async applyMistralFallback(
    sourceCode: string,
    deterministicResult: AgenticCorrectionResult,
    deterministicTime: number,
    userId?: string,
  ): Promise<HybridCorrectionResult> {

    const mistralStartTime = Date.now();

    try {
      // Get Mistral correction
      const mistralAttempt = await this.getMistralCorrection(sourceCode, userId);
      const mistralTime = Date.now() - mistralStartTime;

      if (mistralAttempt.success && mistralAttempt.confidence >= this.config.mistralConfidenceThreshold) {
        // Mistral succeeded - create hybrid result
        const hybridResult = this.mergeCorrectionResults(
          deterministicResult,
          mistralAttempt,
          'mistral_fallback',
        );

        return this.createHybridResult(
          hybridResult,
          true,
          mistralAttempt.confidence,
          mistralTime,
          'mistral_fallback',
          {
            deterministicTime,
            mistralTime,
            hybridTime: deterministicTime + mistralTime,
            accuracyComparison: this.calculateAccuracyComparison(deterministicResult, mistralAttempt),
          },
        );
      } else {
        // Mistral also failed - return best available result
        return this.createHybridResult(
          deterministicResult,
          true,
          mistralAttempt.confidence,
          mistralTime,
          'both_failed',
          {
            deterministicTime,
            mistralTime,
            hybridTime: deterministicTime + mistralTime,
            accuracyComparison: 0.5,
          },
        );
      }

    } catch (error) {
      console.error('❌ Mistral fallback failed:', error);

      return this.createHybridResult(
        deterministicResult,
        false,
        0,
        Date.now() - mistralStartTime,
        'mistral_error',
      );
    }
  }

  /**
   * Parallel correction mode - run both approaches simultaneously
   */
  private async applyParallelCorrection(
    sourceCode: string,
    deterministicResult: AgenticCorrectionResult,
    deterministicTime: number,
    userId?: string,
  ): Promise<HybridCorrectionResult> {

    const mistralStartTime = Date.now();

    try {
      // Run Mistral in parallel (deterministic already completed)
      const mistralAttempt = await this.getMistralCorrection(sourceCode, userId);
      const mistralTime = Date.now() - mistralStartTime;

      // Compare and select best result
      const bestResult = this.selectBestResult(deterministicResult, mistralAttempt);
      const approach = bestResult === deterministicResult ? 'deterministic_selected' : 'mistral_selected';

      return this.createHybridResult(
        bestResult,
        true,
        mistralAttempt.confidence,
        mistralTime,
        approach,
        {
          deterministicTime,
          mistralTime,
          hybridTime: Math.max(deterministicTime, mistralTime), // Parallel execution
          accuracyComparison: this.calculateAccuracyComparison(deterministicResult, mistralAttempt),
        },
      );

    } catch (error) {
      console.error('❌ Parallel correction failed:', error);

      return this.createHybridResult(
        deterministicResult,
        false,
        0,
        Date.now() - mistralStartTime,
        'parallel_error',
      );
    }
  }

  /**
   * Validation mode - use Mistral to validate deterministic results
   */
  private async applyMistralValidation(
    sourceCode: string,
    deterministicResult: AgenticCorrectionResult,
    deterministicTime: number,
    userId?: string,
  ): Promise<HybridCorrectionResult> {

    const mistralStartTime = Date.now();

    try {
      // Use Mistral to validate the deterministic correction
      const validationPrompt = this.createValidationPrompt(sourceCode, deterministicResult.correctedCode || '');
      const validationRequest: MistralRequest = {
        model: 'codestral-latest',
        messages: [{ role: 'user', content: validationPrompt }],
        temperature: 0.1,
        max_tokens: 1000,
      };
      const validationResult = await this.mistralClient.generateCompletion(validationRequest);
      const mistralTime = Date.now() - mistralStartTime;

      // Parse validation result
      const isValid = this.parseValidationResult(validationResult.response?.choices[0]?.message?.content || '');
      const confidence = isValid ? 0.9 : 0.3;

      if (isValid) {
        // Validation passed - return enhanced deterministic result
        return this.createHybridResult(
          deterministicResult,
          true,
          confidence,
          mistralTime,
          'validation_passed',
          {
            deterministicTime,
            mistralTime,
            hybridTime: deterministicTime + mistralTime,
            accuracyComparison: 1.0,
          },
        );
      } else {
        // Validation failed - try Mistral correction
        return await this.applyMistralFallback(sourceCode, deterministicResult, deterministicTime, userId);
      }

    } catch (error) {
      console.error('❌ Mistral validation failed:', error);

      return this.createHybridResult(
        deterministicResult,
        false,
        0,
        Date.now() - mistralStartTime,
        'validation_error',
      );
    }
  }

  /**
   * Get Mistral correction with caching and error handling
   */
  private async getMistralCorrection(
    sourceCode: string,
    userId?: string,
  ): Promise<MistralCorrectionAttempt> {

    // Check cache first
    const cacheKey = this.createCacheKey(sourceCode, userId);
    if (this.config.enableMistralCaching && this.mistralCache.has(cacheKey)) {
      const cached = this.mistralCache.get(cacheKey)!;
      if (this.config.logMistralUsage) {
        console.log('🔄 Using cached Mistral result');
      }
      return cached;
    }

    const startTime = Date.now();
    let attempt = 0;

    while (attempt < this.config.mistralMaxRetries) {
      try {
        // Create correction prompt
        const prompt = this.createCorrectionPrompt(sourceCode);
        
        // Use higher token limit for code generation vs error correction
        const maxTokens = sourceCode.includes('Complete the following') || 
                         sourceCode.includes('Write a function') || 
                         sourceCode.includes('Implement') ? 4000 : 2000;
        
        const correctionRequest: MistralRequest = {
          model: 'codestral-latest',
          messages: [{ role: 'user', content: prompt }],
          temperature: 0.1, // Lower temperature for more consistent code generation
          max_tokens: maxTokens,
        };

        // Call Mistral with timeout
        const mistralResult = await Promise.race([
          this.mistralClient.generateCompletion(correctionRequest),
          this.createTimeoutPromise(this.config.mistralTimeoutMs),
        ]);

        const responseTime = Date.now() - startTime;

        // Evaluate Mistral result
        const correctionAttempt: MistralCorrectionAttempt = {
          sourceCode,
          error: { type: ErrorType.SYNTAX_ERROR } as StructuredValidationError, // Placeholder
          mistralSuggestion: mistralResult.response?.choices[0]?.message?.content || '',
          confidence: this.evaluateMistralConfidence(mistralResult.response?.choices[0]?.message?.content || '', sourceCode),
          responseTime,
          success: this.evaluateMistralSuccess(mistralResult.response?.choices[0]?.message?.content || '', sourceCode),
        };

        // Cache successful results
        if (this.config.enableMistralCaching && correctionAttempt.success) {
          this.mistralCache.set(cacheKey, correctionAttempt);
        }

        // Log usage
        if (this.config.logMistralUsage) {
          console.log(`🤖 Mistral correction: ${correctionAttempt.success ? '✅' : '❌'} (${responseTime}ms, confidence: ${(correctionAttempt.confidence * 100).toFixed(1)}%)`);
        }

        this.performanceMetrics.mistralUsageCount++;

        return correctionAttempt;

      } catch (error) {
        attempt++;
        console.warn(`⚠️  Mistral attempt ${attempt} failed:`, error);

        if (attempt >= this.config.mistralMaxRetries) {
          return {
            sourceCode,
            error: { type: ErrorType.SYNTAX_ERROR } as StructuredValidationError,
            mistralSuggestion: '',
            confidence: 0,
            responseTime: Date.now() - startTime,
            success: false,
          };
        }

        // Wait before retry
        await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
      }
    }

    // Should never reach here, but TypeScript requires it
    throw new Error('Mistral correction failed after all retries');
  }

  /**
   * Create correction prompt for Mistral
   */
  private createCorrectionPrompt(sourceCode: string): string {
    // Check if this is a code generation request (contains prompts like "Complete the following")
    if (sourceCode.includes('Complete the following') || 
        sourceCode.includes('Write a function') || 
        sourceCode.includes('Implement') ||
        sourceCode.includes('```python') && sourceCode.includes('def ')) {
      
      // This is a code generation request, not error correction
      return `${sourceCode}

Please provide only the complete, working Python code implementation. ` +
        'Do not include explanations, comments about the task, or markdown formatting. ' +
        'Return only executable Python code.';
    }
    
    // This is error correction for existing code
    return `Please fix the errors in this Python code and return only the corrected code:

\`\`\`python
${sourceCode}
\`\`\`

Requirements:
- Fix syntax errors
- Fix undefined variables by adding appropriate declarations
- Fix import errors by adding missing imports
- Maintain original code structure and logic
- Return only the corrected Python code, no explanations`;
  }

  /**
   * Create validation prompt for Mistral
   */
  private createValidationPrompt(originalCode: string, correctedCode: string): string {
    return `Please validate if this code correction is accurate and maintains the original intent:

Original code:
\`\`\`python
${originalCode}
\`\`\`

Corrected code:
\`\`\`python
${correctedCode}
\`\`\`

Respond with "VALID" if the correction is accurate and maintains original intent, or "INVALID" if there are issues.`;
  }

  /**
   * Parse Mistral validation result
   */
  private parseValidationResult(validationResult: string): boolean {
    return validationResult.toUpperCase().includes('VALID') && !validationResult.toUpperCase().includes('INVALID');
  }

  /**
   * Evaluate Mistral correction confidence
   */
  private evaluateMistralConfidence(mistralResult: string, originalCode: string): number {
    // Simple heuristic-based confidence evaluation
    let confidence = 0.5; // Base confidence

    // Check if result looks like valid Python code
    if (mistralResult.includes('def ') || mistralResult.includes('import ') || mistralResult.includes('print(')) {
      confidence += 0.2;
    }

    // Check if result is significantly different from input (indicates actual correction)
    const similarity = this.calculateStringSimilarity(originalCode, mistralResult);
    if (similarity > 0.7 && similarity < 0.95) {
      confidence += 0.2;
    }

    // Check for common error patterns
    if (!mistralResult.includes('undefined') && !mistralResult.includes('NameError')) {
      confidence += 0.1;
    }

    return Math.min(confidence, 1.0);
  }

  /**
   * Evaluate if Mistral correction was successful
   */
  private evaluateMistralSuccess(mistralResult: string, originalCode: string): boolean {
    // Basic success criteria
    return mistralResult.length > 0 &&
           mistralResult !== originalCode &&
           !mistralResult.includes('Error:') &&
           !mistralResult.includes('I cannot');
  }

  /**
   * Calculate string similarity between two strings
   */
  private calculateStringSimilarity(str1: string, str2: string): number {
    const longer = str1.length > str2.length ? str1 : str2;
    const shorter = str1.length > str2.length ? str2 : str1;

    if (longer.length === 0) {
return 1.0;
}

    const editDistance = this.calculateEditDistance(longer, shorter);
    return (longer.length - editDistance) / longer.length;
  }

  /**
   * Calculate edit distance between two strings
   */
  private calculateEditDistance(str1: string, str2: string): number {
    const matrix = Array(str2.length + 1).fill(null).map(() => Array(str1.length + 1).fill(null));

    for (let i = 0; i <= str1.length; i++) {
matrix[0][i] = i;
}
    for (let j = 0; j <= str2.length; j++) {
matrix[j][0] = j;
}

    for (let j = 1; j <= str2.length; j++) {
      for (let i = 1; i <= str1.length; i++) {
        const indicator = str1[i - 1] === str2[j - 1] ? 0 : 1;
        matrix[j][i] = Math.min(
          matrix[j][i - 1] + 1,     // deletion
          matrix[j - 1][i] + 1,     // insertion
          matrix[j - 1][i - 1] + indicator, // substitution
        );
      }
    }

    return matrix[str2.length][str1.length];
  }

  /**
   * Merge deterministic and Mistral correction results
   */
  private mergeCorrectionResults(
    deterministicResult: AgenticCorrectionResult,
    mistralAttempt: MistralCorrectionAttempt,
    approach: string,
  ): AgenticCorrectionResult {

    // Create merged correction steps
    const mergedSteps: CorrectionStep[] = [
      ...deterministicResult.correctionSteps,
      {
        stepNumber: deterministicResult.correctionSteps.length + 1,
        stepType: CorrectionStepType.LLM_GENERATION,
        determinismLevel: DeterminismLevel.NON_DETERMINISTIC,
        confidence: mistralAttempt.confidence,
        executionTime: mistralAttempt.responseTime,
        description: 'Mistral Codestral LLM correction',
        reasoning: 'Deterministic approaches insufficient, using LLM fallback',
        input: {
          sourceCode: mistralAttempt.sourceCode,
          error: mistralAttempt.error,
        },
        output: {
          correctedCode: mistralAttempt.mistralSuggestion,
          confidence: mistralAttempt.confidence,
          transformationsApplied: ['llm_correction'],
        },
      },
    ];

    return {
      ...deterministicResult,
      success: mistralAttempt.success,
      correctedCode: mistralAttempt.mistralSuggestion,
      correctionSteps: mergedSteps,
      totalLLMCalls: deterministicResult.totalLLMCalls + 1,
      totalExecutionTime: deterministicResult.totalExecutionTime + mistralAttempt.responseTime,
      deterministicRatio: deterministicResult.correctionSteps.length / mergedSteps.length,
      finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
    };
  }

  /**
   * Select best result between deterministic and Mistral approaches
   */
  private selectBestResult(
    deterministicResult: AgenticCorrectionResult,
    mistralAttempt: MistralCorrectionAttempt,
  ): AgenticCorrectionResult {

    // Scoring criteria
    const deterministicScore = this.calculateResultScore(deterministicResult);
    const mistralScore = this.calculateMistralScore(mistralAttempt);

    if (deterministicScore >= mistralScore) {
      return deterministicResult;
    } else {
      // Convert Mistral attempt to AgenticCorrectionResult format
      return this.convertMistralToAgenticResult(mistralAttempt);
    }
  }

  /**
   * Calculate score for deterministic result
   */
  private calculateResultScore(result: AgenticCorrectionResult): number {
    let score = 0;

    // Success bonus
    if (result.success) {
score += 50;
}

    // Deterministic ratio bonus
    score += result.deterministicRatio * 30;

    // Speed bonus (inverse of execution time)
    score += Math.max(0, 20 - (result.totalExecutionTime / 100));

    return score;
  }

  /**
   * Calculate score for Mistral result
   */
  private calculateMistralScore(attempt: MistralCorrectionAttempt): number {
    let score = 0;

    // Success bonus
    if (attempt.success) {
score += 50;
}

    // Confidence bonus
    score += attempt.confidence * 30;

    // Speed penalty (LLM is typically slower)
    score += Math.max(0, 10 - (attempt.responseTime / 200));

    return score;
  }

  /**
   * Convert Mistral attempt to AgenticCorrectionResult format
   */
  private convertMistralToAgenticResult(attempt: MistralCorrectionAttempt): AgenticCorrectionResult {
    return {
      success: attempt.success,
      correctedCode: attempt.mistralSuggestion,
      correctionSteps: [{
        stepNumber: 1,
        stepType: CorrectionStepType.LLM_GENERATION,
        determinismLevel: DeterminismLevel.NON_DETERMINISTIC,
        confidence: attempt.confidence,
        executionTime: attempt.responseTime,
        description: 'Mistral Codestral correction',
        reasoning: 'Direct LLM correction approach',
        input: {
          sourceCode: attempt.sourceCode,
          error: attempt.error,
        },
        output: {
          correctedCode: attempt.mistralSuggestion,
          confidence: attempt.confidence,
          transformationsApplied: ['llm_correction'],
        },
      }],
      totalLLMCalls: 1,
      totalExecutionTime: attempt.responseTime,
      deterministicRatio: 0,
      finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
      enhancedResult: {} as any, // Placeholder for EnhancedCorrectionResult
      agenticMetrics: {
        stepsByType: new Map([[CorrectionStepType.LLM_GENERATION, 1]]),
        stepsByDeterminism: new Map([[DeterminismLevel.NON_DETERMINISTIC, 1]]),
        averageConfidenceByType: new Map([[CorrectionStepType.LLM_GENERATION, attempt.confidence]]),
        progressionEfficiency: 0.5,
        llmUsageOptimization: 0.3,
      },
    };
  }

  /**
   * Fallback to Mistral-only correction when agentic system fails
   */
  private async mistralOnlyCorrection(sourceCode: string, startTime: number): Promise<HybridCorrectionResult> {
    try {
      const mistralAttempt = await this.getMistralCorrection(sourceCode, 'fallback-user');
      
      const executionTime = Date.now() - startTime;
      
      return {
        success: mistralAttempt.success,
        correctedCode: mistralAttempt.mistralSuggestion,
        correctionSteps: [{
          stepNumber: 1,
          stepType: CorrectionStepType.LLM_GENERATION,
          description: 'Mistral-only correction (agentic system unavailable)',
          input: sourceCode,
          output: mistralAttempt.mistralSuggestion,
          confidence: mistralAttempt.confidence,
          executionTime: mistralAttempt.responseTime,
          determinismLevel: DeterminismLevel.NON_DETERMINISTIC,
          reasoning: 'Agentic system unavailable, using Mistral fallback',
        }],
        finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
        totalLLMCalls: 1,
        totalExecutionTime: executionTime,
        deterministicRatio: 0,
        enhancedResult: {
          success: mistralAttempt.success,
          correctedCode: mistralAttempt.mistralSuggestion,
          solutionResults: new Map(),
          selectionResults: new Map(),
          appliedSolutions: [],
          remainingErrors: [],
          metrics: {
            totalErrors: 0,
            errorsProcessed: 1,
            errorsResolved: mistralAttempt.success ? 1 : 0,
            totalSolutionsGenerated: 1,
            averageSolutionsPerError: 1,
            selectionAccuracy: mistralAttempt.success ? 1 : 0,
            totalCorrectionTime: mistralAttempt.responseTime,
            generationTime: mistralAttempt.responseTime,
            selectionTime: 0,
            validationTime: 0,
            iterationCount: 1,
            algorithmUsage: new Map(),
          },
          session: {
            sessionId: 'fallback-session',
            startTime: new Date(startTime),
            endTime: new Date(),
            sourceCode,
            finalCode: mistralAttempt.mistralSuggestion,
            errorHistory: [],
            solutionHistory: [],
            userFeedback: [],
          },
        },
        agenticMetrics: {
          stepsByType: new Map([[CorrectionStepType.LLM_GENERATION, 1]]),
          stepsByDeterminism: new Map([[DeterminismLevel.NON_DETERMINISTIC, 1]]),
          averageConfidenceByType: new Map([[CorrectionStepType.LLM_GENERATION, mistralAttempt.confidence]]),
          progressionEfficiency: 0.5,
          llmUsageOptimization: 0.3,
        },
        mistralUsed: true,
        mistralConfidence: mistralAttempt.confidence,
        mistralResponseTime: mistralAttempt.responseTime,
        hybridApproach: 'mistral_only_fallback',
      };
    } catch (error) {
      // Complete fallback - return unsuccessful result
      const executionTime = Date.now() - startTime;
      return {
        success: false,
        correctedCode: sourceCode, // Return original code
        correctionSteps: [],
        finalDeterminismLevel: DeterminismLevel.NON_DETERMINISTIC,
        totalLLMCalls: 0,
        totalExecutionTime: executionTime,
        deterministicRatio: 0,
        enhancedResult: {
          success: false,
          correctedCode: sourceCode,
          solutionResults: new Map(),
          selectionResults: new Map(),
          appliedSolutions: [],
          remainingErrors: [],
          metrics: {
            totalErrors: 1,
            errorsProcessed: 1,
            errorsResolved: 0,
            totalSolutionsGenerated: 0,
            averageSolutionsPerError: 0,
            selectionAccuracy: 0,
            totalCorrectionTime: executionTime,
            generationTime: 0,
            selectionTime: 0,
            validationTime: 0,
            iterationCount: 1,
            algorithmUsage: new Map(),
          },
          session: {
            sessionId: 'failed-fallback-session',
            startTime: new Date(startTime),
            endTime: new Date(),
            sourceCode,
            finalCode: sourceCode,
            errorHistory: [],
            solutionHistory: [],
            userFeedback: [],
          },
        },
        agenticMetrics: {
          stepsByType: new Map(),
          stepsByDeterminism: new Map(),
          averageConfidenceByType: new Map(),
          progressionEfficiency: 0,
          llmUsageOptimization: 0,
        },
        mistralUsed: false,
        mistralConfidence: 0,
        mistralResponseTime: 0,
        hybridApproach: 'failed_fallback',
      };
    }
  }

  /**
   * Calculate accuracy comparison between approaches
   */
  private calculateAccuracyComparison(
    deterministicResult: AgenticCorrectionResult,
    mistralAttempt: MistralCorrectionAttempt,
  ): number {

    // Simple heuristic - would be more sophisticated in production
    const deterministicScore = deterministicResult.success ? deterministicResult.deterministicRatio : 0;
    const mistralScore = mistralAttempt.success ? mistralAttempt.confidence : 0;

    if (deterministicScore === 0 && mistralScore === 0) {
return 0.5;
}
    if (deterministicScore === 0) {
return 0;
}
    if (mistralScore === 0) {
return 1;
}

    return deterministicScore / (deterministicScore + mistralScore);
  }

  /**
   * Create hybrid correction result
   */
  private createHybridResult(
    baseResult: AgenticCorrectionResult,
    mistralUsed: boolean,
    mistralConfidence: number,
    mistralResponseTime: number,
    hybridApproach: string,
    performanceComparison?: any,
  ): HybridCorrectionResult {

    return {
      ...baseResult,
      mistralUsed,
      mistralConfidence,
      mistralResponseTime,
      hybridApproach,
      performanceComparison,
    };
  }

  /**
   * Update performance metrics
   */
  private updatePerformanceMetrics(result: HybridCorrectionResult): void {
    const total = this.performanceMetrics.totalCorrections;

    // Update success rate
    this.performanceMetrics.hybridSuccessRate =
      (this.performanceMetrics.hybridSuccessRate * (total - 1) + (result.success ? 1 : 0)) / total;

    // Update average response time
    this.performanceMetrics.averageResponseTime =
      (this.performanceMetrics.averageResponseTime * (total - 1) + result.totalExecutionTime) / total;
  }

  /**
   * Create cache key for Mistral results
   */
  private createCacheKey(sourceCode: string, userId?: string): string {
    const hash = this.simpleHash(sourceCode + (userId || ''));
    return `mistral_${hash}`;
  }

  /**
   * Simple hash function for cache keys
   */
  private simpleHash(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash).toString(36);
  }

  /**
   * Create timeout promise for Mistral calls
   */
  private createTimeoutPromise(timeoutMs: number): Promise<never> {
    return new Promise((_, reject) => {
      setTimeout(() => reject(new Error('Mistral timeout')), timeoutMs);
    });
  }

  /**
   * Get performance metrics
   */
  getPerformanceMetrics() {
    return { ...this.performanceMetrics };
  }

  /**
   * Clear Mistral cache
   */
  clearCache(): void {
    this.mistralCache.clear();
  }

  /**
   * Get cache statistics
   */
  getCacheStatistics() {
    return {
      cacheSize: this.mistralCache.size,
      cacheEnabled: this.config.enableMistralCaching,
    };
  }
}

