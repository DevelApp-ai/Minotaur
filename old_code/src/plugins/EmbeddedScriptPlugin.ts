/**
 * Plugin system for testing embedded scripts in grammar files.
 * Supports Bison/Yacc C code actions, ANTLR v4 actions, and Minotaur function calls.
 */
export class EmbeddedScriptPluginManager {
  private plugins: Map<string, EmbeddedScriptPlugin>;
  private executionEnvironments: Map<ScriptLanguage, ExecutionEnvironment>;
  private testResults: Map<string, ScriptTestResult>;

  /**
   * Creates a new EmbeddedScriptPluginManager instance.
   */
  constructor() {
    this.plugins = new Map<string, EmbeddedScriptPlugin>();
    this.executionEnvironments = new Map<ScriptLanguage, ExecutionEnvironment>();
    this.testResults = new Map<string, ScriptTestResult>();

    // Initialize default execution environments
    this.initializeDefaultEnvironments();
  }

  /**
   * Registers a plugin.
   * @param name The plugin name
   * @param plugin The plugin instance
   */
  public registerPlugin(name: string, plugin: EmbeddedScriptPlugin): void {
    this.plugins.set(name, plugin);
  }

  /**
   * Gets a plugin by name.
   * @param name The plugin name
   * @returns The plugin or null if not found
   */
  public getPlugin(name: string): EmbeddedScriptPlugin | null {
    return this.plugins.get(name) || null;
  }

  /**
   * Registers an execution environment.
   * @param language The script language
   * @param environment The execution environment
   */
  public registerExecutionEnvironment(language: ScriptLanguage, environment: ExecutionEnvironment): void {
    this.executionEnvironments.set(language, environment);
  }

  /**
   * Tests embedded scripts in a grammar.
   * @param grammarName The grammar name
   * @param grammarContent The grammar content
   * @param formatType The grammar format type
   * @returns The test results
   */
  public testEmbeddedScripts(
    grammarName: string,
    grammarContent: string,
    formatType: GrammarFormatType,
  ): ScriptTestResults {
    const results = new ScriptTestResults(grammarName);

    // Extract embedded scripts based on format type
    const scripts = this.extractEmbeddedScripts(grammarContent, formatType);

    for (const script of scripts) {
      try {
        const testResult = this.testScript(script);
        results.addResult(script.getId(), testResult);
        this.testResults.set(`${grammarName}.${script.getId()}`, testResult);
      } catch (error) {
        const errorResult = new ScriptTestResult(
          script.getId(),
          ScriptTestStatus.Error,
          `Script test failed: ${error instanceof Error ? error.message : String(error)}`,
          0,
          null,
        );
        results.addResult(script.getId(), errorResult);
        this.testResults.set(`${grammarName}.${script.getId()}`, errorResult);
      }
    }

    return results;
  }

  /**
   * Tests a single embedded script.
   * @param script The embedded script
   * @returns The test result
   */
  public testScript(script: EmbeddedScript): ScriptTestResult {
    const startTime = Date.now();

    try {
      // Get appropriate execution environment
      const environment = this.executionEnvironments.get(script.getLanguage());
      if (!environment) {
        throw new Error(`No execution environment found for language: ${script.getLanguage()}`);
      }

      // Execute the script
      const executionResult = environment.execute(script);
      const endTime = Date.now();

      const status = executionResult.isSuccess() ? ScriptTestStatus.Passed : ScriptTestStatus.Failed;
      const message = executionResult.isSuccess() ? 'Script executed successfully' : executionResult.getErrorMessage();

      return new ScriptTestResult(
        script.getId(),
        status,
        message,
        endTime - startTime,
        executionResult.getOutput(),
      );
    } catch (error) {
      const endTime = Date.now();
      return new ScriptTestResult(
        script.getId(),
        ScriptTestStatus.Error,
        `Script execution error: ${error instanceof Error ? error.message : String(error)}`,
        endTime - startTime,
        null,
      );
    }
  }

  /**
   * Extracts embedded scripts from grammar content.
   * @param grammarContent The grammar content
   * @param formatType The grammar format type
   * @returns Array of embedded scripts
   */
  private extractEmbeddedScripts(grammarContent: string, formatType: GrammarFormatType): EmbeddedScript[] {
    const scripts: EmbeddedScript[] = [];

    switch (formatType) {
      case GrammarFormatType.ANTLR4:
        scripts.push(...this.extractANTLR4Scripts(grammarContent));
        break;

      case GrammarFormatType.Bison:
        scripts.push(...this.extractBisonScripts(grammarContent));
        break;

      case GrammarFormatType.Yacc:
        scripts.push(...this.extractYaccScripts(grammarContent));
        break;

      case GrammarFormatType.Minotaur:
        scripts.push(...this.extractMinotaurScripts(grammarContent));
        break;

      default:
        // Try to extract generic scripts
        scripts.push(...this.extractGenericScripts(grammarContent));
        break;
    }

    return scripts;
  }

  /**
   * Extracts ANTLR4 embedded scripts.
   * @param grammarContent The grammar content
   * @returns Array of embedded scripts
   */
  private extractANTLR4Scripts(grammarContent: string): EmbeddedScript[] {
    const scripts: EmbeddedScript[] = [];
    let scriptId = 0;

    // Extract Java actions: @members { ... }
    const memberMatches = grammarContent.matchAll(/@members\s*\{([^}]*)\}/g);
    for (const match of memberMatches) {
      const code = match[1].trim();
      if (code) {
        scripts.push(new EmbeddedScript(
          `antlr4_members_${scriptId++}`,
          ScriptLanguage.Java,
          code,
          ScriptType.MemberAction,
          match.index || 0,
        ));
      }
    }

    // Extract rule actions: { ... }
    const actionMatches = grammarContent.matchAll(/\{\s*([^}]+)\s*\}/g);
    for (const match of actionMatches) {
      const code = match[1].trim();
      if (code && !code.startsWith('@')) { // Skip @members and similar
        scripts.push(new EmbeddedScript(
          `antlr4_action_${scriptId++}`,
          ScriptLanguage.Java,
          code,
          ScriptType.RuleAction,
          match.index || 0,
        ));
      }
    }

    return scripts;
  }

  /**
   * Extracts Bison embedded scripts.
   * @param grammarContent The grammar content
   * @returns Array of embedded scripts
   */
  private extractBisonScripts(grammarContent: string): EmbeddedScript[] {
    const scripts: EmbeddedScript[] = [];
    let scriptId = 0;

    // Extract C code blocks: %{ ... %}
    const codeMatches = grammarContent.matchAll(/%\{([^%]*?)%\}/g);
    for (const match of codeMatches) {
      const code = match[1].trim();
      if (code) {
        scripts.push(new EmbeddedScript(
          `bison_code_${scriptId++}`,
          ScriptLanguage.C,
          code,
          ScriptType.HeaderCode,
          match.index || 0,
        ));
      }
    }

    // Extract rule actions: { ... }
    const actionMatches = grammarContent.matchAll(/\{\s*([^}]+)\s*\}/g);
    for (const match of actionMatches) {
      const code = match[1].trim();
      if (code) {
        scripts.push(new EmbeddedScript(
          `bison_action_${scriptId++}`,
          ScriptLanguage.C,
          code,
          ScriptType.RuleAction,
          match.index || 0,
        ));
      }
    }

    return scripts;
  }

  /**
   * Extracts Yacc embedded scripts.
   * @param grammarContent The grammar content
   * @returns Array of embedded scripts
   */
  private extractYaccScripts(grammarContent: string): EmbeddedScript[] {
    // Yacc is similar to Bison
    return this.extractBisonScripts(grammarContent);
  }

  /**
   * Extracts Minotaur embedded scripts.
   * @param grammarContent The grammar content
   * @returns Array of embedded scripts
   */
  private extractMinotaurScripts(grammarContent: string): EmbeddedScript[] {
    const scripts: EmbeddedScript[] = [];
    let scriptId = 0;

    // Extract function calls: @call(functionName, args)
    const callMatches = grammarContent.matchAll(/@call\s*\(\s*([^,)]+)(?:\s*,\s*([^)]*))?\s*\)/g);
    for (const match of callMatches) {
      const functionName = match[1].trim();
      const args = match[2] ? match[2].trim() : '';
      const code = `${functionName}(${args})`;

      scripts.push(new EmbeddedScript(
        `minotaur_call_${scriptId++}`,
        ScriptLanguage.JavaScript,
        code,
        ScriptType.FunctionCall,
        match.index || 0,
      ));
    }

    // Extract JavaScript blocks: @js{ ... }
    const jsMatches = grammarContent.matchAll(/@js\s*\{([^}]*)\}/g);
    for (const match of jsMatches) {
      const code = match[1].trim();
      if (code) {
        scripts.push(new EmbeddedScript(
          `minotaur_js_${scriptId++}`,
          ScriptLanguage.JavaScript,
          code,
          ScriptType.ScriptBlock,
          match.index || 0,
        ));
      }
    }

    return scripts;
  }

  /**
   * Extracts generic embedded scripts.
   * @param grammarContent The grammar content
   * @returns Array of embedded scripts
   */
  private extractGenericScripts(grammarContent: string): EmbeddedScript[] {
    const scripts: EmbeddedScript[] = [];
    let scriptId = 0;

    // Extract generic code blocks: { ... }
    const codeMatches = grammarContent.matchAll(/\{\s*([^}]+)\s*\}/g);
    for (const match of codeMatches) {
      const code = match[1].trim();
      if (code) {
        scripts.push(new EmbeddedScript(
          `generic_code_${scriptId++}`,
          ScriptLanguage.JavaScript, // Default to JavaScript
          code,
          ScriptType.GenericCode,
          match.index || 0,
        ));
      }
    }

    return scripts;
  }

  /**
   * Initializes default execution environments.
   */
  private initializeDefaultEnvironments(): void {
    // JavaScript environment
    this.executionEnvironments.set(ScriptLanguage.JavaScript, new JavaScriptExecutionEnvironment());

    // C environment (mock for testing)
    this.executionEnvironments.set(ScriptLanguage.C, new CExecutionEnvironment());

    // Java environment (mock for testing)
    this.executionEnvironments.set(ScriptLanguage.Java, new JavaExecutionEnvironment());

    // Python environment (mock for testing)
    this.executionEnvironments.set(ScriptLanguage.Python, new PythonExecutionEnvironment());

    // C# environment (mock for testing)
    this.executionEnvironments.set(ScriptLanguage.CSharp, new CSharpExecutionEnvironment());
  }

  /**
   * Gets test results.
   * @param scriptId The script ID (optional)
   * @returns The test result or all results
   */
  public getTestResults(scriptId?: string): ScriptTestResult | Map<string, ScriptTestResult> {
    if (scriptId) {
      return this.testResults.get(scriptId) || null;
    }
    return new Map(this.testResults);
  }

  /**
   * Generates a test report for embedded scripts.
   * @param results The test results
   * @returns The test report
   */
  public generateTestReport(results: ScriptTestResults): string {
    let report = `=== Embedded Script Test Report for ${results.getGrammarName()} ===\n\n`;

    const summary = results.getSummary();
    report += `Total Scripts: ${summary.totalScripts}\n`;
    report += `Passed: ${summary.passedScripts}\n`;
    report += `Failed: ${summary.failedScripts}\n`;
    report += `Errors: ${summary.errorScripts}\n`;
    report += `Success Rate: ${(summary.passedScripts / summary.totalScripts * 100).toFixed(2)}%\n`;
    report += `Total Time: ${summary.totalTime}ms\n\n`;

    for (const [scriptId, result] of results.getResults()) {
      const status = result.getStatus();
      const statusSymbol = status === ScriptTestStatus.Passed ? '✓' :
        status === ScriptTestStatus.Failed ? '✗' : '!';

      report += `${statusSymbol} ${scriptId}: ${result.getMessage()} (${result.getExecutionTime()}ms)\n`;

      if (result.getOutput()) {
        report += `  Output: ${result.getOutput()}\n`;
      }
    }

    return report;
  }
}

/**
 * Interface for embedded script plugins.
 */
export interface EmbeddedScriptPlugin {
  getName(): string;
  getDescription(): string;
  getSupportedLanguages(): ScriptLanguage[];
  canHandle(script: EmbeddedScript): boolean;
  process(script: EmbeddedScript): ProcessedScript;
}

/**
 * Represents an embedded script extracted from grammar content.
 */
export class EmbeddedScript {
  private id: string;
  private language: ScriptLanguage;
  private code: string;
  private type: ScriptType;
  private position: number;
  private metadata: Map<string, any>;

  constructor(id: string, language: ScriptLanguage, code: string, type: ScriptType, position: number) {
    this.id = id;
    this.language = language;
    this.code = code;
    this.type = type;
    this.position = position;
    this.metadata = new Map<string, any>();
  }

  public getId(): string {
    return this.id;
  }

  public getLanguage(): ScriptLanguage {
    return this.language;
  }

  public getCode(): string {
    return this.code;
  }

  public getType(): ScriptType {
    return this.type;
  }

  public getPosition(): number {
    return this.position;
  }

  public getMetadata(key: string): any {
    return this.metadata.get(key);
  }

  public setMetadata(key: string, value: any): void {
    this.metadata.set(key, value);
  }
}

/**
 * Represents a processed script ready for execution.
 */
export class ProcessedScript {
  private originalScript: EmbeddedScript;
  private processedCode: string;
  private dependencies: string[];
  private executionContext: Map<string, any>;

  constructor(originalScript: EmbeddedScript, processedCode: string) {
    this.originalScript = originalScript;
    this.processedCode = processedCode;
    this.dependencies = [];
    this.executionContext = new Map<string, any>();
  }

  public getOriginalScript(): EmbeddedScript {
    return this.originalScript;
  }

  public getProcessedCode(): string {
    return this.processedCode;
  }

  public getDependencies(): string[] {
    return [...this.dependencies];
  }

  public addDependency(dependency: string): void {
    this.dependencies.push(dependency);
  }

  public getExecutionContext(): Map<string, any> {
    return new Map(this.executionContext);
  }

  public setContextValue(key: string, value: any): void {
    this.executionContext.set(key, value);
  }
}

/**
 * Interface for script execution environments.
 */
export interface ExecutionEnvironment {
  getLanguage(): ScriptLanguage;
  execute(script: EmbeddedScript): ExecutionResult;
  isAvailable(): boolean;
  getVersion(): string;
}

/**
 * JavaScript execution environment.
 */
export class JavaScriptExecutionEnvironment implements ExecutionEnvironment {
  public getLanguage(): ScriptLanguage {
    return ScriptLanguage.JavaScript;
  }

  public execute(script: EmbeddedScript): ExecutionResult {
    try {
      // Create a safe execution context
      const context = {
        console: {
          log: (...args: any[]) => args.join(' '),
        },
        Math: Math,
        Date: Date,
        JSON: JSON,
      };

      // Execute the script in the context
      const func = new Function(...Object.keys(context), `return (function() { ${script.getCode()} })();`);
      const result = func(...Object.values(context));

      return new ExecutionResult(true, result, null);
    } catch (error) {
      return new ExecutionResult(false, null, error instanceof Error ? error.message : String(error));
    }
  }

  public isAvailable(): boolean {
    return true; // Always available in Node.js/browser
  }

  public getVersion(): string {
    return 'ES2020';
  }
}

/**
 * C execution environment (mock implementation).
 */
export class CExecutionEnvironment implements ExecutionEnvironment {
  public getLanguage(): ScriptLanguage {
    return ScriptLanguage.C;
  }

  public execute(script: EmbeddedScript): ExecutionResult {
    // Mock implementation - in reality, this would compile and execute C code
    try {
      // Simulate C code validation
      const code = script.getCode();

      // Basic syntax checks
      if (!code.includes(';') && !code.includes('{')) {
        return new ExecutionResult(false, null, 'C code appears to be incomplete');
      }

      // Mock successful execution
      return new ExecutionResult(true, 'C code validated successfully', null);
    } catch (error) {
      // eslint-disable-next-line max-len
      return new ExecutionResult(false, null, `C execution error: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  public isAvailable(): boolean {
    // In a real implementation, check if GCC or similar is available
    return false; // Mock: not available
  }

  public getVersion(): string {
    return 'GCC 9.4.0 (mock)';
  }
}

/**
 * Java execution environment (mock implementation).
 */
export class JavaExecutionEnvironment implements ExecutionEnvironment {
  public getLanguage(): ScriptLanguage {
    return ScriptLanguage.Java;
  }

  public execute(script: EmbeddedScript): ExecutionResult {
    // Mock implementation
    try {
      const code = script.getCode();

      // Basic Java syntax validation
      if (code.includes('System.out.println') || code.includes('return') || code.includes('=')) {
        return new ExecutionResult(true, 'Java code validated successfully', null);
      }

      return new ExecutionResult(false, null, 'Java code appears to be incomplete');
    } catch (error) {
      // eslint-disable-next-line max-len
      return new ExecutionResult(false, null, `Java execution error: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  public isAvailable(): boolean {
    return false; // Mock: not available
  }

  public getVersion(): string {
    return 'OpenJDK 11 (mock)';
  }
}

/**
 * Python execution environment (mock implementation).
 */
export class PythonExecutionEnvironment implements ExecutionEnvironment {
  public getLanguage(): ScriptLanguage {
    return ScriptLanguage.Python;
  }

  public execute(script: EmbeddedScript): ExecutionResult {
    // Mock implementation
    try {
      const code = script.getCode();

      // Basic Python syntax validation
      if (code.includes('print(') || code.includes('return') || code.includes('=')) {
        return new ExecutionResult(true, 'Python code validated successfully', null);
      }

      return new ExecutionResult(false, null, 'Python code appears to be incomplete');
    } catch (error) {
      // eslint-disable-next-line max-len
      return new ExecutionResult(false, null, `Python execution error: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  public isAvailable(): boolean {
    return false; // Mock: not available
  }

  public getVersion(): string {
    return 'Python 3.9 (mock)';
  }
}

/**
 * C# execution environment (mock implementation).
 */
export class CSharpExecutionEnvironment implements ExecutionEnvironment {
  public getLanguage(): ScriptLanguage {
    return ScriptLanguage.CSharp;
  }

  public execute(script: EmbeddedScript): ExecutionResult {
    // Mock implementation
    try {
      const code = script.getCode();

      // Basic C# syntax validation
      if (code.includes('Console.WriteLine') || code.includes('return') || code.includes('=')) {
        return new ExecutionResult(true, 'C# code validated successfully', null);
      }

      return new ExecutionResult(false, null, 'C# code appears to be incomplete');
    } catch (error) {
      // eslint-disable-next-line max-len
      return new ExecutionResult(false, null, `C# execution error: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  public isAvailable(): boolean {
    return false; // Mock: not available
  }

  public getVersion(): string {
    return '.NET 6.0 (mock)';
  }
}

/**
 * Execution result container.
 */
export class ExecutionResult {
  private success: boolean;
  private output: any;
  private errorMessage: string | null;

  constructor(success: boolean, output: any, errorMessage: string | null) {
    this.success = success;
    this.output = output;
    this.errorMessage = errorMessage;
  }

  public isSuccess(): boolean {
    return this.success;
  }

  public getOutput(): any {
    return this.output;
  }

  public getErrorMessage(): string | null {
    return this.errorMessage;
  }
}

/**
 * Script test result container.
 */
export class ScriptTestResult {
  private scriptId: string;
  private status: ScriptTestStatus;
  private message: string;
  private executionTime: number;
  private output: any;

  constructor(scriptId: string, status: ScriptTestStatus, message: string, executionTime: number, output: any) {
    this.scriptId = scriptId;
    this.status = status;
    this.message = message;
    this.executionTime = executionTime;
    this.output = output;
  }

  public getScriptId(): string {
    return this.scriptId;
  }

  public getStatus(): ScriptTestStatus {
    return this.status;
  }

  public getMessage(): string {
    return this.message;
  }

  public getExecutionTime(): number {
    return this.executionTime;
  }

  public getOutput(): any {
    return this.output;
  }
}

/**
 * Script test results container.
 */
export class ScriptTestResults {
  private grammarName: string;
  private results: Map<string, ScriptTestResult>;

  constructor(grammarName: string) {
    this.grammarName = grammarName;
    this.results = new Map<string, ScriptTestResult>();
  }

  public getGrammarName(): string {
    return this.grammarName;
  }

  public addResult(scriptId: string, result: ScriptTestResult): void {
    this.results.set(scriptId, result);
  }

  public getResults(): Map<string, ScriptTestResult> {
    return new Map(this.results);
  }

  public getSummary(): ScriptTestSummary {
    let totalScripts = 0;
    let passedScripts = 0;
    let failedScripts = 0;
    let errorScripts = 0;
    let totalTime = 0;

    for (const result of this.results.values()) {
      totalScripts++;
      totalTime += result.getExecutionTime();

      switch (result.getStatus()) {
        case ScriptTestStatus.Passed:
          passedScripts++;
          break;
        case ScriptTestStatus.Failed:
          failedScripts++;
          break;
        case ScriptTestStatus.Error:
          errorScripts++;
          break;
      }
    }

    return new ScriptTestSummary(totalScripts, passedScripts, failedScripts, errorScripts, totalTime);
  }
}

/**
 * Script test summary container.
 */
export class ScriptTestSummary {
  public totalScripts: number;
  public passedScripts: number;
  public failedScripts: number;
  public errorScripts: number;
  public totalTime: number;

  // eslint-disable-next-line max-len
  constructor(totalScripts: number, passedScripts: number, failedScripts: number, errorScripts: number, totalTime: number) {
    this.totalScripts = totalScripts;
    this.passedScripts = passedScripts;
    this.failedScripts = failedScripts;
    this.errorScripts = errorScripts;
    this.totalTime = totalTime;
  }
}

/**
 * Script language enumeration.
 */
export enum ScriptLanguage {
  JavaScript = 'javascript',
  C = 'c',
  Java = 'java',
  Python = 'python',
  CSharp = 'csharp'
}

/**
 * Script type enumeration.
 */
export enum ScriptType {
  MemberAction = 'member_action',
  RuleAction = 'rule_action',
  HeaderCode = 'header_code',
  FunctionCall = 'function_call',
  ScriptBlock = 'script_block',
  GenericCode = 'generic_code'
}

/**
 * Script test status enumeration.
 */
export enum ScriptTestStatus {
  Passed = 'passed',
  Failed = 'failed',
  Error = 'error'
}

// Import required classes and interfaces
import { GrammarFormatType } from '../utils/Grammar';

