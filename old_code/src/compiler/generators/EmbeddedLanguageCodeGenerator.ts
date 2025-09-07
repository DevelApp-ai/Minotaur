/**
 * Minotaur Embedded Language Code Generator
 *
 * Advanced code generator that supports embedded language parsing,
 * context switching, cross-language validation, and multi-language AST generation.
 */

import { Grammar, SymbolDefinition, ReferenceDefinition, ValidationRule } from '../../core/grammar/Grammar';
import { EmbeddedLanguageContext, CrossLanguageReference } from '../EmbeddedLanguageContextManager';
import { CrossLanguageValidationResult } from '../CrossLanguageValidator';

export interface EmbeddedLanguageGenerationConfig {
    targetLanguage: string;
    outputFormat: 'single_file' | 'multi_file' | 'modular';
    enableContextSwitching: boolean;
    enableCrossLanguageValidation: boolean;
    enableSymbolTableSharing: boolean;
    optimizationLevel: 'debug' | 'release' | 'production';
    generateTests: boolean;
    generateDocumentation: boolean;
    targetLanguageOptions: { [key: string]: any };
}

export interface EmbeddedLanguageGeneratedCode {
    success: boolean;
    sourceFiles: Map<string, string>;
    headerFiles: Map<string, string>;
    buildFiles: Map<string, string>;
    testFiles: Map<string, string>;
    documentationFiles: Map<string, string>;
    metadata: GenerationMetadata;
    errors: GenerationError[];
    warnings: GenerationWarning[];
}

export interface GenerationMetadata {
    generationTime: number;
    linesOfCode: number;
    filesGenerated: number;
    embeddedLanguagesSupported: string[];
    contextSwitchesGenerated: number;
    crossLanguageReferencesGenerated: number;
    validationRulesGenerated: number;
    targetLanguage: string;
    generatorVersion: string;
}

export interface GenerationError {
    message: string;
    severity: 'error' | 'warning';
    component: string;
    suggestion: string;
}

export interface GenerationWarning {
    message: string;
    component: string;
    suggestion: string;
}

export abstract class EmbeddedLanguageCodeGenerator {
  protected primaryGrammar: Grammar;
  protected embeddedGrammars: Map<string, Grammar>;
  protected config: EmbeddedLanguageGenerationConfig;
  protected metadata: GenerationMetadata;
  protected errors: GenerationError[];
  protected warnings: GenerationWarning[];

  constructor(
    primaryGrammar: Grammar,
    embeddedGrammars: Map<string, Grammar>,
    config: EmbeddedLanguageGenerationConfig,
  ) {
    this.primaryGrammar = primaryGrammar;
    this.embeddedGrammars = embeddedGrammars;
    this.config = config;
    this.metadata = this.initializeMetadata();
    this.errors = [];
    this.warnings = [];
  }

  /**
     * Generate code for embedded language support
     */
  public async generateEmbeddedLanguageCode(): Promise<EmbeddedLanguageGeneratedCode> {
    const startTime = performance.now();

    try {
      // Reset state
      this.resetGenerator();

      // Generate core parser components
      const sourceFiles = await this.generateSourceFiles();

      // Generate header files (for languages that need them)
      const headerFiles = await this.generateHeaderFiles();

      // Generate build system files
      const buildFiles = await this.generateBuildFiles();

      // Generate test files
      const testFiles = this.config.generateTests ? await this.generateTestFiles() : new Map();

      // Generate documentation
      const documentationFiles = this.config.generateDocumentation ?
        await this.generateDocumentationFiles() : new Map();

      // Update metadata
      this.metadata.generationTime = performance.now() - startTime;
      this.metadata.filesGenerated = sourceFiles.size + headerFiles.size + buildFiles.size +
                                         testFiles.size + documentationFiles.size;
      this.metadata.linesOfCode = this.calculateLinesOfCode(sourceFiles, headerFiles);

      return {
        success: this.errors.length === 0,
        sourceFiles: sourceFiles,
        headerFiles: headerFiles,
        buildFiles: buildFiles,
        testFiles: testFiles,
        documentationFiles: documentationFiles,
        metadata: { ...this.metadata },
        errors: [...this.errors],
        warnings: [...this.warnings],
      };

    } catch (error) {
      this.addError(`Code generation failed: ${error instanceof Error ? error.message : String(error)}`, 'generator', 'Check grammar definitions and configuration');

      return {
        success: false,
        sourceFiles: new Map(),
        headerFiles: new Map(),
        buildFiles: new Map(),
        testFiles: new Map(),
        documentationFiles: new Map(),
        metadata: { ...this.metadata },
        errors: [...this.errors],
        warnings: [...this.warnings],
      };
    }
  }

  /**
     * Generate source files for embedded language support
     */
  protected async generateSourceFiles(): Promise<Map<string, string>> {
    const sourceFiles = new Map<string, string>();

    // Generate main parser file
    sourceFiles.set(this.getMainParserFileName(), await this.generateMainParser());

    // Generate lexer file
    sourceFiles.set(this.getLexerFileName(), await this.generateLexer());

    // Generate AST files
    sourceFiles.set(this.getASTFileName(), await this.generateAST());

    // Generate context manager
    if (this.config.enableContextSwitching) {
      sourceFiles.set(this.getContextManagerFileName(), await this.generateContextManager());
    }

    // Generate embedded language parsers
    for (const [language, grammar] of this.embeddedGrammars) {
      sourceFiles.set(
        this.getEmbeddedParserFileName(language),
        await this.generateEmbeddedParser(language, grammar),
      );
    }

    // Generate cross-language validator
    if (this.config.enableCrossLanguageValidation) {
      sourceFiles.set(this.getValidatorFileName(), await this.generateValidator());
    }

    // Generate symbol table manager
    if (this.config.enableSymbolTableSharing) {
      sourceFiles.set(this.getSymbolTableFileName(), await this.generateSymbolTableManager());
    }

    return sourceFiles;
  }

  /**
     * Generate header files (for C/C++, etc.)
     */
  protected async generateHeaderFiles(): Promise<Map<string, string>> {
    const headerFiles = new Map<string, string>();

    if (this.needsHeaderFiles()) {
      headerFiles.set(this.getMainHeaderFileName(), await this.generateMainHeader());
      headerFiles.set(this.getTypesHeaderFileName(), await this.generateTypesHeader());

      if (this.config.enableContextSwitching) {
        headerFiles.set(this.getContextHeaderFileName(), await this.generateContextHeader());
      }
    }

    return headerFiles;
  }

  /**
     * Generate build system files
     */
  protected async generateBuildFiles(): Promise<Map<string, string>> {
    const buildFiles = new Map<string, string>();

    buildFiles.set(this.getBuildFileName(), await this.generateBuildFile());
    buildFiles.set(this.getConfigFileName(), await this.generateConfigFile());

    if (this.supportsCMake()) {
      buildFiles.set('CMakeLists.txt', await this.generateCMakeFile());
    }

    if (this.supportsMakefile()) {
      buildFiles.set('Makefile', await this.generateMakefile());
    }

    return buildFiles;
  }

  /**
     * Generate test files
     */
  protected async generateTestFiles(): Promise<Map<string, string>> {
    const testFiles = new Map<string, string>();

    // Generate unit tests for each component
    testFiles.set(this.getParserTestFileName(), await this.generateParserTests());
    testFiles.set(this.getLexerTestFileName(), await this.generateLexerTests());
    testFiles.set(this.getASTTestFileName(), await this.generateASTTests());

    // Generate embedded language tests
    for (const [language, grammar] of this.embeddedGrammars) {
      testFiles.set(
        this.getEmbeddedTestFileName(language),
        await this.generateEmbeddedLanguageTests(language, grammar),
      );
    }

    // Generate integration tests
    testFiles.set(this.getIntegrationTestFileName(), await this.generateIntegrationTests());

    // Generate cross-language validation tests
    if (this.config.enableCrossLanguageValidation) {
      testFiles.set(this.getValidationTestFileName(), await this.generateValidationTests());
    }

    return testFiles;
  }

  /**
     * Generate documentation files
     */
  protected async generateDocumentationFiles(): Promise<Map<string, string>> {
    const documentationFiles = new Map<string, string>();

    documentationFiles.set('README.md', await this.generateReadme());
    documentationFiles.set('API.md', await this.generateAPIDocumentation());
    documentationFiles.set('GRAMMAR.md', await this.generateGrammarDocumentation());

    if (this.embeddedGrammars.size > 0) {
      documentationFiles.set('EMBEDDED_LANGUAGES.md', await this.generateEmbeddedLanguageDocumentation());
    }

    if (this.config.enableCrossLanguageValidation) {
      documentationFiles.set('VALIDATION.md', await this.generateValidationDocumentation());
    }

    return documentationFiles;
  }

  // Abstract methods to be implemented by specific language generators

    protected abstract getMainParserFileName(): string;
    protected abstract getLexerFileName(): string;
    protected abstract getASTFileName(): string;
    protected abstract getContextManagerFileName(): string;
    protected abstract getValidatorFileName(): string;
    protected abstract getSymbolTableFileName(): string;
    protected abstract getEmbeddedParserFileName(language: string): string;

    protected abstract generateMainParser(): Promise<string>;
    protected abstract generateLexer(): Promise<string>;
    protected abstract generateAST(): Promise<string>;
    protected abstract generateContextManager(): Promise<string>;
    protected abstract generateValidator(): Promise<string>;
    protected abstract generateSymbolTableManager(): Promise<string>;
    protected abstract generateEmbeddedParser(language: string, grammar: Grammar): Promise<string>;

    protected abstract needsHeaderFiles(): boolean;
    protected abstract getMainHeaderFileName(): string;
    protected abstract getTypesHeaderFileName(): string;
    protected abstract getContextHeaderFileName(): string;
    protected abstract generateMainHeader(): Promise<string>;
    protected abstract generateTypesHeader(): Promise<string>;
    protected abstract generateContextHeader(): Promise<string>;

    protected abstract getBuildFileName(): string;
    protected abstract getConfigFileName(): string;
    protected abstract generateBuildFile(): Promise<string>;
    protected abstract generateConfigFile(): Promise<string>;
    protected abstract supportsCMake(): boolean;
    protected abstract supportsMakefile(): boolean;
    protected abstract generateCMakeFile(): Promise<string>;
    protected abstract generateMakefile(): Promise<string>;

    protected abstract getParserTestFileName(): string;
    protected abstract getLexerTestFileName(): string;
    protected abstract getASTTestFileName(): string;
    protected abstract getEmbeddedTestFileName(language: string): string;
    protected abstract getIntegrationTestFileName(): string;
    protected abstract getValidationTestFileName(): string;
    protected abstract generateParserTests(): Promise<string>;
    protected abstract generateLexerTests(): Promise<string>;
    protected abstract generateASTTests(): Promise<string>;
    protected abstract generateEmbeddedLanguageTests(language: string, grammar: Grammar): Promise<string>;
    protected abstract generateIntegrationTests(): Promise<string>;
    protected abstract generateValidationTests(): Promise<string>;

    // Common utility methods

    protected generateReadme(): Promise<string> {
      return Promise.resolve(`# ${this.primaryGrammar.getName()} Parser

Generated by Minotaur with embedded language support.

## Supported Languages

- **Primary Language**: ${this.primaryGrammar.getName()}
${Array.from(this.embeddedGrammars.keys()).map(lang => `- **Embedded Language**: ${lang}`).join('\n')}

## Features

- Context-sensitive parsing with embedded language support
- Cross-language reference validation
- Symbol table sharing across language boundaries
- High-performance parsing with optimization
- Comprehensive error reporting and suggestions

## Usage

\`\`\`${this.config.targetLanguage.toLowerCase()}
// Example usage will be generated based on target language
\`\`\`

## Generated Files

${this.metadata.filesGenerated} files generated with ${this.metadata.linesOfCode} lines of code.

## Build Instructions

See build files for compilation instructions.
`);
    }

    protected generateAPIDocumentation(): Promise<string> {
      return Promise.resolve(`# API Documentation

## Parser API

### Main Parser Class

The main parser class provides methods for parsing ${this.primaryGrammar.getName()} with embedded language support.

### Context Manager

Manages context switching between embedded languages.

### Validator

Validates cross-language references and semantic consistency.

### Symbol Table Manager

Manages symbol tables with hierarchical sharing across languages.
`);
    }

    protected generateGrammarDocumentation(): Promise<string> {
      const embeddedLanguagesList = Array.from(this.embeddedGrammars.keys()).join(', ');

      return Promise.resolve(`# Grammar Documentation

## Primary Grammar: ${this.primaryGrammar.getName()}

### Embedded Languages

${embeddedLanguagesList}

### Context Switching

The grammar supports context switching using @CONTEXT[language] directives.

### Cross-Language References

References between languages are supported using @REFERENCE[language.symbol] syntax.

### Validation Rules

${this.primaryGrammar.getValidationRules().length} validation rules defined for cross-language consistency.
`);
    }

    protected generateEmbeddedLanguageDocumentation(): Promise<string> {
      let documentation = '# Embedded Language Support\n\n';

      for (const [language, grammar] of this.embeddedGrammars) {
        documentation += `## ${language}\n\n`;
        documentation += `- Grammar: ${grammar.getName()}\n`;
        documentation += `- Context Sensitive: ${grammar.getIsContextSensitive()}\n`;
        documentation += `- Symbol Table Sharing: ${grammar.getSymbolTableSharing()}\n`;
        documentation += `- Cross-Language Validation: ${grammar.getCrossLanguageValidation()}\n\n`;
      }

      return Promise.resolve(documentation);
    }

    protected generateValidationDocumentation(): Promise<string> {
      const validationRules = this.primaryGrammar.getValidationRules();
      let documentation = '# Cross-Language Validation\n\n';

      documentation += '## Validation Rules\n\n';
      documentation += `${validationRules.length} validation rules defined:\n\n`;

      for (const [ruleName, rule] of validationRules) {
        documentation += `### ${ruleName}\n\n`;
        documentation += `- Source Language: ${rule.sourceLanguage}\n`;
        documentation += `- Target Language: ${rule.targetLanguage}\n`;
        documentation += `- Severity: ${rule.severity}\n`;
        documentation += `- Source Pattern: \`${rule.sourcePattern}\`\n`;
        documentation += `- Target Pattern: \`${rule.targetPattern}\`\n\n`;
      }

      return Promise.resolve(documentation);
    }

    protected sanitizeIdentifier(name: string): string {
      return name.replace(/[^a-zA-Z0-9_]/g, '_').replace(/^[0-9]/, '_$&');
    }

    protected generateContextSwitchCode(language: string): string {
      return `// Context switch to ${language}
if (isContextSwitch("${language}")) {
    switchContext("${language}");
    return parse${language}Content();
}`;
    }

    protected generateCrossLanguageReferenceCode(reference: ReferenceDefinition): string {
      return `// Cross-language reference: ${reference.target}
if (isReference("${reference.target}", "${reference.target}")) {
    return resolveReference("${reference.target}", "${reference.resolution}");
}`;
    }

    protected generateValidationRuleCode(rule: ValidationRule): string {
      return `// Validation rule: ${rule.name}
if (matches("${rule.rule}")) {
    validateTarget("${rule.targetLanguage}", "${rule.rule}");
}`;
    }

    protected calculateLinesOfCode(sourceFiles: Map<string, string>, headerFiles: Map<string, string>): number {
      let totalLines = 0;

      for (const content of sourceFiles.values()) {
        totalLines += content.split('\n').length;
      }

      for (const content of headerFiles.values()) {
        totalLines += content.split('\n').length;
      }

      return totalLines;
    }

    protected addError(message: string, component: string, suggestion: string): void {
      this.errors.push({
        message: message,
        severity: 'error',
        component: component,
        suggestion: suggestion,
      });
    }

    protected addWarning(message: string, component: string, suggestion: string): void {
      this.warnings.push({
        message: message,
        component: component,
        suggestion: suggestion,
      });
    }

    protected resetGenerator(): void {
      this.metadata = this.initializeMetadata();
      this.errors = [];
      this.warnings = [];
    }

    protected initializeMetadata(): GenerationMetadata {
      return {
        generationTime: 0,
        linesOfCode: 0,
        filesGenerated: 0,
        embeddedLanguagesSupported: Array.from(this.embeddedGrammars.keys()),
        contextSwitchesGenerated: 0,
        crossLanguageReferencesGenerated: 0,
        validationRulesGenerated: this.primaryGrammar.getValidationRules().length,
        targetLanguage: this.config.targetLanguage,
        generatorVersion: '1.0.0',
      };
    }
}

/**
 * TypeScript/JavaScript Code Generator for Embedded Languages
 */
export class TypeScriptEmbeddedLanguageCodeGenerator extends EmbeddedLanguageCodeGenerator {

  protected getMainParserFileName(): string {
    return 'parser.ts';
  }

  protected getLexerFileName(): string {
    return 'lexer.ts';
  }

  protected getASTFileName(): string {
    return 'ast.ts';
  }

  protected getContextManagerFileName(): string {
    return 'context-manager.ts';
  }

  protected getValidatorFileName(): string {
    return 'validator.ts';
  }

  protected getSymbolTableFileName(): string {
    return 'symbol-table.ts';
  }

  protected getEmbeddedParserFileName(language: string): string {
    return `${language.toLowerCase()}-parser.ts`;
  }

  protected async generateMainParser(): Promise<string> {
    const embeddedLanguages = Array.from(this.embeddedGrammars.keys());

    return `/**
 * Main Parser for ${this.primaryGrammar.getName()} with Embedded Language Support
 * Generated by Minotaur
 */

import { Lexer } from './lexer';
import { AST, ASTNode } from './ast';
${this.config.enableContextSwitching ? "import { ContextManager } from './context-manager';" : ''}
${this.config.enableCrossLanguageValidation ? "import { Validator } from './validator';" : ''}
${this.config.enableSymbolTableSharing ? "import { SymbolTableManager } from './symbol-table';" : ''}
${embeddedLanguages.map(lang => `import { ${lang}Parser } from './${lang.toLowerCase()}-parser';`).join('\n')}

export interface ParseOptions {
    enableContextSwitching?: boolean;
    enableValidation?: boolean;
    enableSymbolTableSharing?: boolean;
}

export interface ParseResult {
    success: boolean;
    ast: AST | null;
    errors: ParseError[];
    warnings: ParseWarning[];
    contextSwitches: ContextSwitch[];
    validationResults: ValidationResult[];
}

export interface ParseError {
    message: string;
    line: number;
    column: number;
    severity: 'error' | 'warning';
}

export interface ParseWarning {
    message: string;
    line: number;
    column: number;
    suggestion: string;
}

export interface ContextSwitch {
    fromLanguage: string;
    toLanguage: string;
    position: { line: number; column: number };
}

export interface ValidationResult {
    valid: boolean;
    message: string;
    severity: 'error' | 'warning' | 'info';
}

export class ${this.primaryGrammar.getName()}Parser {
    private lexer: Lexer;
    ${this.config.enableContextSwitching ? 'private contextManager: ContextManager;' : ''}
    ${this.config.enableCrossLanguageValidation ? 'private validator: Validator;' : ''}
    ${this.config.enableSymbolTableSharing ? 'private symbolTableManager: SymbolTableManager;' : ''}
    ${embeddedLanguages.map(lang => `private ${lang.toLowerCase()}Parser: ${lang}Parser;`).join('\n    ')}

    constructor() {
        this.lexer = new Lexer();
        ${this.config.enableContextSwitching ? 'this.contextManager = new ContextManager();' : ''}
        ${this.config.enableCrossLanguageValidation ? 'this.validator = new Validator();' : ''}
        ${this.config.enableSymbolTableSharing ? 'this.symbolTableManager = new SymbolTableManager();' : ''}
        ${embeddedLanguages.map(lang => `this.${lang.toLowerCase()}Parser = new ${lang}Parser();`).join('\n        ')}
    }

    public async parse(input: string, options: ParseOptions = {}): Promise<ParseResult> {
        const errors: ParseError[] = [];
        const warnings: ParseWarning[] = [];
        const contextSwitches: ContextSwitch[] = [];
        const validationResults: ValidationResult[] = [];

        try {
            // Tokenize input
            const tokens = await this.lexer.tokenize(input);
            
            // Parse with context switching support
            const ast = await this.parseTokens(tokens, options);
            
            ${this.config.enableCrossLanguageValidation ? `
            // Validate cross-language references
            if (options.enableValidation !== false) {
                const validation = await this.validator.validate(ast);
                validationResults.push(...validation.results);
            }` : ''}

            return {
                success: errors.length === 0,
                ast: ast,
                errors: errors,
                warnings: warnings,
                contextSwitches: contextSwitches,
                validationResults: validationResults
            };

        } catch (error) {
            errors.push({
                message: \`Parse error: \${error instanceof Error ? error.message : String(error)}\`,
                line: 0,
                column: 0,
                severity: 'error'
            });

            return {
                success: false,
                ast: null,
                errors: errors,
                warnings: warnings,
                contextSwitches: contextSwitches,
                validationResults: validationResults
            };
        }
    }

    private async parseTokens(tokens: Token[], options: ParseOptions): Promise<AST> {
        const ast = new AST();
        let currentContext = '${this.primaryGrammar.getName()}';
        
        for (let i = 0; i < tokens.length; i++) {
            const token = tokens[i];
            
            ${this.config.enableContextSwitching ? `
            // Check for context switches
            if (options.enableContextSwitching !== false) {
                const contextSwitch = this.contextManager.checkContextSwitch(token);
                if (contextSwitch) {
                    currentContext = contextSwitch.targetLanguage;
                    // Parse embedded content with appropriate parser
                    const embeddedContent = await this.parseEmbeddedContent(
                        tokens.slice(i), 
                        contextSwitch.targetLanguage
                    );
                    ast.addNode(embeddedContent);
                    continue;
                }
            }` : ''}
            
            // Parse token in current context
            const node = await this.parseToken(token, currentContext);
            ast.addNode(node);
        }
        
        return ast;
    }

    private async parseToken(token: Token, context: string): Promise<ASTNode> {
        // Token parsing logic based on current context
        switch (context) {
            case '${this.primaryGrammar.getName()}':
                return this.parsePrimaryLanguageToken(token);
            ${embeddedLanguages.map(lang => `
            case '${lang}':
                return this.${lang.toLowerCase()}Parser.parseToken(token);`).join('')}
            default:
                throw new Error(\`Unknown context: \${context}\`);
        }
    }

    private async parseEmbeddedContent(tokens: Token[], language: string): Promise<ASTNode> {
        switch (language) {
            ${embeddedLanguages.map(lang => `
            case '${lang}':
                return this.${lang.toLowerCase()}Parser.parseTokens(tokens);`).join('')}
            default:
                throw new Error(\`Unknown embedded language: \${language}\`);
        }
    }

    private parsePrimaryLanguageToken(token: Token): ASTNode {
        // Primary language token parsing logic
        return new ASTNode(token.type, token.value);
    }
}`;
  }

  protected async generateLexer(): Promise<string> {
    return `/**
 * Lexer for ${this.primaryGrammar.getName()} with Embedded Language Support
 * Generated by Minotaur
 */

export interface Token {
    type: string;
    value: string;
    line: number;
    column: number;
    language: string;
}

export class Lexer {
    private currentLine: number = 1;
    private currentColumn: number = 1;
    private currentLanguage: string = '${this.primaryGrammar.getName()}';

    public async tokenize(input: string): Promise<Token[]> {
        const tokens: Token[] = [];
        let position = 0;

        while (position < input.length) {
            const token = this.nextToken(input, position);
            if (token) {
                tokens.push(token);
                position += token.value.length;
                this.updatePosition(token.value);
            } else {
                position++;
                this.currentColumn++;
            }
        }

        return tokens;
    }

    private nextToken(input: string, position: number): Token | null {
        const remaining = input.substring(position);
        
        // Check for context switch patterns
        ${Array.from(this.embeddedGrammars.keys()).map(lang => `
        if (remaining.startsWith('@CONTEXT[${lang}]')) {
            this.currentLanguage = '${lang}';
            return {
                type: 'CONTEXT_SWITCH',
                value: '@CONTEXT[${lang}]',
                line: this.currentLine,
                column: this.currentColumn,
                language: this.currentLanguage
            };
        }`).join('')}
        
        // Tokenize based on current language
        return this.tokenizeInLanguage(remaining, this.currentLanguage);
    }

    private tokenizeInLanguage(input: string, language: string): Token | null {
        // Language-specific tokenization logic
        switch (language) {
            case '${this.primaryGrammar.getName()}':
                return this.tokenizePrimaryLanguage(input);
            ${Array.from(this.embeddedGrammars.keys()).map(lang => `
            case '${lang}':
                return this.tokenize${lang}(input);`).join('')}
            default:
                return null;
        }
    }

    private tokenizePrimaryLanguage(input: string): Token | null {
        // Primary language tokenization logic
        const patterns = [
            { type: 'IDENTIFIER', pattern: /^[a-zA-Z_][a-zA-Z0-9_]*/ },
            { type: 'NUMBER', pattern: /^\\d+/ },
            { type: 'STRING', pattern: /^"[^"]*"/ },
            { type: 'WHITESPACE', pattern: /^\\s+/ }
        ];

        for (const { type, pattern } of patterns) {
            const match = input.match(pattern);
            if (match) {
                return {
                    type: type,
                    value: match[0],
                    line: this.currentLine,
                    column: this.currentColumn,
                    language: this.currentLanguage
                };
            }
        }

        return null;
    }

    ${Array.from(this.embeddedGrammars.keys()).map(lang => `
    private tokenize${lang}(input: string): Token | null {
        // ${lang} tokenization logic
        // This would be generated based on the ${lang} grammar
        return null;
    }`).join('')}

    private updatePosition(value: string): void {
        for (const char of value) {
            if (char === '\\n') {
                this.currentLine++;
                this.currentColumn = 1;
            } else {
                this.currentColumn++;
            }
        }
    }
}`;
  }

  protected async generateAST(): Promise<string> {
    return `/**
 * AST Classes for ${this.primaryGrammar.getName()} with Embedded Language Support
 * Generated by Minotaur
 */

export interface ASTVisitor {
    visitNode(node: ASTNode): void;
    visitEmbeddedNode(node: EmbeddedASTNode): void;
}

export class ASTNode {
    public type: string;
    public value: string;
    public children: ASTNode[] = [];
    public language: string = '${this.primaryGrammar.getName()}';
    public position: { line: number; column: number };

    constructor(type: string, value: string, position?: { line: number; column: number }) {
        this.type = type;
        this.value = value;
        this.position = position || { line: 0, column: 0 };
    }

    public addChild(child: ASTNode): void {
        this.children.push(child);
    }

    public accept(visitor: ASTVisitor): void {
        visitor.visitNode(this);
        for (const child of this.children) {
            child.accept(visitor);
        }
    }
}

export class EmbeddedASTNode extends ASTNode {
    public embeddedLanguage: string;
    public contextSwitchInfo: ContextSwitchInfo;

    constructor(
        type: string, 
        value: string, 
        embeddedLanguage: string,
        contextSwitchInfo: ContextSwitchInfo,
        position?: { line: number; column: number }
    ) {
        super(type, value, position);
        this.embeddedLanguage = embeddedLanguage;
        this.contextSwitchInfo = contextSwitchInfo;
        this.language = embeddedLanguage;
    }

    public accept(visitor: ASTVisitor): void {
        visitor.visitEmbeddedNode(this);
        for (const child of this.children) {
            child.accept(visitor);
        }
    }
}

export interface ContextSwitchInfo {
    fromLanguage: string;
    toLanguage: string;
    switchTrigger: string;
    endTrigger: string;
}

export class AST {
    public root: ASTNode;
    public embeddedNodes: EmbeddedASTNode[] = [];

    constructor() {
        this.root = new ASTNode('Program', '');
    }

    public addNode(node: ASTNode): void {
        this.root.addChild(node);
        
        if (node instanceof EmbeddedASTNode) {
            this.embeddedNodes.push(node);
        }
    }

    public accept(visitor: ASTVisitor): void {
        this.root.accept(visitor);
    }

    public getEmbeddedNodes(language?: string): EmbeddedASTNode[] {
        if (language) {
            return this.embeddedNodes.filter(node => node.embeddedLanguage === language);
        }
        return this.embeddedNodes;
    }
}`;
  }

  protected async generateContextManager(): Promise<string> {
    return `/**
 * Context Manager for Embedded Language Support
 * Generated by Minotaur
 */

export interface ContextSwitchPattern {
    trigger: string;
    targetLanguage: string;
    endTrigger: string;
}

export class ContextManager {
    private contextSwitchPatterns: Map<string, ContextSwitchPattern> = new Map();
    private contextStack: string[] = ['${this.primaryGrammar.getName()}'];

    constructor() {
        this.initializeContextSwitchPatterns();
    }

    private initializeContextSwitchPatterns(): void {
        ${Array.from(this.embeddedGrammars.keys()).map(lang => `
        this.contextSwitchPatterns.set('@CONTEXT[${lang}]', {
            trigger: '@CONTEXT[${lang}]',
            targetLanguage: '${lang}',
            endTrigger: '@ENDCONTEXT'
        });`).join('')}
    }

    public checkContextSwitch(token: Token): ContextSwitchInfo | null {
        const pattern = this.contextSwitchPatterns.get(token.value);
        if (pattern) {
            return {
                fromLanguage: this.getCurrentContext(),
                toLanguage: pattern.targetLanguage,
                switchTrigger: pattern.trigger,
                endTrigger: pattern.endTrigger
            };
        }
        return null;
    }

    public switchContext(language: string): void {
        this.contextStack.push(language);
    }

    public exitContext(): string | null {
        if (this.contextStack.length > 1) {
            return this.contextStack.pop() || null;
        }
        return null;
    }

    public getCurrentContext(): string {
        return this.contextStack[this.contextStack.length - 1];
    }

    public getContextDepth(): number {
        return this.contextStack.length;
    }
}`;
  }

  protected async generateValidator(): Promise<string> {
    return `/**
 * Cross-Language Validator
 * Generated by Minotaur
 */

export class Validator {
    private validationRules: ValidationRule[] = [];

    constructor() {
        this.initializeValidationRules();
    }

    private initializeValidationRules(): void {
        ${Array.from(this.primaryGrammar.getValidationRules().values()).map(rule => `
        this.validationRules.push({
            name: '${rule.name}',
            sourceLanguage: '${rule.sourceLanguage}',
            targetLanguage: '${rule.targetLanguage}',
            sourcePattern: '${rule.sourcePattern}',
            targetPattern: '${rule.targetPattern}',
            severity: '${rule.severity}'
        });`).join('')}
    }

    public async validate(ast: AST): Promise<ValidationResult> {
        const results: ValidationResult[] = [];
        
        for (const rule of this.validationRules) {
            const ruleResults = await this.applyValidationRule(rule, ast);
            results.push(...ruleResults);
        }

        return {
            success: results.every(result => result.severity !== 'error'),
            results: results
        };
    }

    private async applyValidationRule(rule: ValidationRule, ast: AST): Promise<ValidationResult[]> {
        const results: ValidationResult[] = [];
        
        // Apply validation rule logic
        // This would involve pattern matching and cross-language reference checking
        
        return results;
    }
}`;
  }

  protected async generateSymbolTableManager(): Promise<string> {
    return `/**
 * Symbol Table Manager for Cross-Language Symbol Sharing
 * Generated by Minotaur
 */

export interface Symbol {
    name: string;
    type: string;
    language: string;
    scope: string;
    position: { line: number; column: number };
}

export class SymbolTableManager {
    private globalSymbolTable: Map<string, Symbol> = new Map();
    private languageSymbolTables: Map<string, Map<string, Symbol>> = new Map();

    constructor() {
        this.initializeLanguageTables();
    }

    private initializeLanguageTables(): void {
        this.languageSymbolTables.set('${this.primaryGrammar.getName()}', new Map());
        ${Array.from(this.embeddedGrammars.keys()).map(lang => `
        this.languageSymbolTables.set('${lang}', new Map());`).join('')}
    }

    public addSymbol(symbol: Symbol): void {
        const languageTable = this.languageSymbolTables.get(symbol.language);
        if (languageTable) {
            languageTable.set(symbol.name, symbol);
        }

        // Add to global table if sharing is enabled
        if (this.isSymbolShared(symbol)) {
            this.globalSymbolTable.set(\`\${symbol.language}.\${symbol.name}\`, symbol);
        }
    }

    public resolveSymbol(name: string, language: string): Symbol | null {
        // First check language-specific table
        const languageTable = this.languageSymbolTables.get(language);
        if (languageTable && languageTable.has(name)) {
            return languageTable.get(name) || null;
        }

        // Then check global table
        const globalKey = \`\${language}.\${name}\`;
        if (this.globalSymbolTable.has(globalKey)) {
            return this.globalSymbolTable.get(globalKey) || null;
        }

        return null;
    }

    private isSymbolShared(symbol: Symbol): boolean {
        // Determine if symbol should be shared based on grammar configuration
        return true; // Simplified logic
    }
}`;
  }

  protected async generateEmbeddedParser(language: string, grammar: Grammar): Promise<string> {
    return `/**
 * ${language} Parser for Embedded Language Support
 * Generated by Minotaur
 */

export class ${language}Parser {
    public async parseTokens(tokens: Token[]): Promise<ASTNode> {
        // ${language}-specific parsing logic
        const node = new ASTNode('${language}Content', '');
        
        for (const token of tokens) {
            const childNode = this.parseToken(token);
            node.addChild(childNode);
        }
        
        return node;
    }

    public parseToken(token: Token): ASTNode {
        // ${language}-specific token parsing
        return new ASTNode(token.type, token.value, {
            line: token.line,
            column: token.column
        });
    }
}`;
  }

  // Header file methods (not needed for TypeScript)
  protected needsHeaderFiles(): boolean {
    return false;
  }
  protected getMainHeaderFileName(): string {
    return '';
  }
  protected getTypesHeaderFileName(): string {
    return '';
  }
  protected getContextHeaderFileName(): string {
    return '';
  }
  protected async generateMainHeader(): Promise<string> {
    return '';
  }
  protected async generateTypesHeader(): Promise<string> {
    return '';
  }
  protected async generateContextHeader(): Promise<string> {
    return '';
  }

  // Build file methods
  protected getBuildFileName(): string {
    return 'package.json';
  }
  protected getConfigFileName(): string {
    return 'tsconfig.json';
  }
  protected supportsCMake(): boolean {
    return false;
  }
  protected supportsMakefile(): boolean {
    return true;
  }

  protected async generateBuildFile(): Promise<string> {
    return `{
  "name": "${this.primaryGrammar.getName().toLowerCase()}-parser",
  "version": "1.0.0",
  "description": "Generated parser for ${this.primaryGrammar.getName()} with embedded language support",
  "main": "dist/parser.js",
  "types": "dist/parser.d.ts",
  "scripts": {
    "build": "tsc",
    "test": "jest",
    "lint": "eslint src/**/*.ts"
  },
  "dependencies": {},
  "devDependencies": {
    "typescript": "^5.0.0",
    "jest": "^29.0.0",
    "@types/jest": "^29.0.0",
    "eslint": "^8.0.0",
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "@typescript-eslint/parser": "^6.0.0"
  }
}`;
  }

  protected async generateConfigFile(): Promise<string> {
    return `{
  "compilerOptions": {
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist", "**/*.test.ts"]
}`;
  }

  protected async generateCMakeFile(): Promise<string> {
    return '';
  }

  protected async generateMakefile(): Promise<string> {
    return `# Makefile for ${this.primaryGrammar.getName()} Parser

.PHONY: build test clean install

build:
\tnpm run build

test:
\tnpm test

clean:
\trm -rf dist node_modules

install:
\tnpm install

lint:
\tnpm run lint

all: install build test
`;
  }

  // Test file methods
  protected getParserTestFileName(): string {
    return 'parser.test.ts';
  }
  protected getLexerTestFileName(): string {
    return 'lexer.test.ts';
  }
  protected getASTTestFileName(): string {
    return 'ast.test.ts';
  }
  protected getEmbeddedTestFileName(language: string): string {
    return `${language.toLowerCase()}-parser.test.ts`;
  }
  protected getIntegrationTestFileName(): string {
    return 'integration.test.ts';
  }
  protected getValidationTestFileName(): string {
    return 'validation.test.ts';
  }

  protected async generateParserTests(): Promise<string> {
    return `/**
 * Parser Tests
 */

import { ${this.primaryGrammar.getName()}Parser } from './parser';

describe('${this.primaryGrammar.getName()}Parser', () => {
    let parser: ${this.primaryGrammar.getName()}Parser;

    beforeEach(() => {
        parser = new ${this.primaryGrammar.getName()}Parser();
    });

    test('should parse simple input', async () => {
        const input = 'test input';
        const result = await parser.parse(input);
        
        expect(result.success).toBe(true);
        expect(result.ast).not.toBeNull();
    });

    ${Array.from(this.embeddedGrammars.keys()).map(lang => `
    test('should parse ${lang} embedded content', async () => {
        const input = '@CONTEXT[${lang}] embedded content @ENDCONTEXT';
        const result = await parser.parse(input, { enableContextSwitching: true });
        
        expect(result.success).toBe(true);
        expect(result.contextSwitches).toHaveLength(1);
        expect(result.contextSwitches[0].toLanguage).toBe('${lang}');
    });`).join('')}
});`;
  }

  protected async generateLexerTests(): Promise<string> {
    return `/**
 * Lexer Tests
 */

import { Lexer } from './lexer';

describe('Lexer', () => {
    let lexer: Lexer;

    beforeEach(() => {
        lexer = new Lexer();
    });

    test('should tokenize simple input', async () => {
        const input = 'identifier 123 "string"';
        const tokens = await lexer.tokenize(input);
        
        expect(tokens).toHaveLength(3);
        expect(tokens[0].type).toBe('IDENTIFIER');
        expect(tokens[1].type).toBe('NUMBER');
        expect(tokens[2].type).toBe('STRING');
    });

    test('should handle context switches', async () => {
        const input = '@CONTEXT[CSS] body { color: red; } @ENDCONTEXT';
        const tokens = await lexer.tokenize(input);
        
        expect(tokens[0].type).toBe('CONTEXT_SWITCH');
        expect(tokens[0].value).toBe('@CONTEXT[CSS]');
    });
});`;
  }

  protected async generateASTTests(): Promise<string> {
    return `/**
 * AST Tests
 */

import { AST, ASTNode, EmbeddedASTNode } from './ast';

describe('AST', () => {
    test('should create AST with root node', () => {
        const ast = new AST();
        
        expect(ast.root).toBeDefined();
        expect(ast.root.type).toBe('Program');
    });

    test('should add nodes to AST', () => {
        const ast = new AST();
        const node = new ASTNode('TestNode', 'test');
        
        ast.addNode(node);
        
        expect(ast.root.children).toHaveLength(1);
        expect(ast.root.children[0]).toBe(node);
    });

    test('should track embedded nodes', () => {
        const ast = new AST();
        const embeddedNode = new EmbeddedASTNode(
            'EmbeddedNode', 
            'content', 
            'CSS',
            // eslint-disable-next-line max-len
            { fromLanguage: '${this.primaryGrammar.getName()}', toLanguage: 'CSS', switchTrigger: '@CONTEXT[CSS]', endTrigger: '@ENDCONTEXT' }
        );
        
        ast.addNode(embeddedNode);
        
        expect(ast.embeddedNodes).toHaveLength(1);
        expect(ast.embeddedNodes[0]).toBe(embeddedNode);
    });
});`;
  }

  protected async generateEmbeddedLanguageTests(language: string, grammar: Grammar): Promise<string> {
    return `/**
 * ${language} Parser Tests
 */

import { ${language}Parser } from './${language.toLowerCase()}-parser';

describe('${language}Parser', () => {
    let parser: ${language}Parser;

    beforeEach(() => {
        parser = new ${language}Parser();
    });

    test('should parse ${language} tokens', async () => {
        const tokens = [
            { type: '${language.toUpperCase()}_TOKEN', value: 'test', line: 1, column: 1, language: '${language}' }
        ];
        
        const result = await parser.parseTokens(tokens);
        
        expect(result).toBeDefined();
        expect(result.type).toBe('${language}Content');
    });
});`;
  }

  protected async generateIntegrationTests(): Promise<string> {
    return `/**
 * Integration Tests
 */

import { ${this.primaryGrammar.getName()}Parser } from './parser';

describe('Integration Tests', () => {
    let parser: ${this.primaryGrammar.getName()}Parser;

    beforeEach(() => {
        parser = new ${this.primaryGrammar.getName()}Parser();
    });

    test('should parse complex multi-language document', async () => {
        const input = \`
            <html>
                <head>
                    @CONTEXT[CSS]
                    body { color: red; }
                    @ENDCONTEXT
                </head>
                <body>
                    @CONTEXT[JavaScript]
    // eslint-disable-next-line no-console
                    console.log('Hello World');
                    @ENDCONTEXT
                </body>
            </html>
        \`;
        
        const result = await parser.parse(input, {
            enableContextSwitching: true,
            enableValidation: true
        });
        
        expect(result.success).toBe(true);
        expect(result.contextSwitches.length).toBeGreaterThan(0);
    });
});`;
  }

  protected async generateValidationTests(): Promise<string> {
    return `/**
 * Validation Tests
 */

import { Validator } from './validator';
import { AST } from './ast';

describe('Validator', () => {
    let validator: Validator;

    beforeEach(() => {
        validator = new Validator();
    });

    test('should validate cross-language references', async () => {
        const ast = new AST();
        // Add test nodes with cross-language references
        
        const result = await validator.validate(ast);
        
        expect(result).toBeDefined();
        expect(result.success).toBeDefined();
    });
});`;
  }

}

