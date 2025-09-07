/**
 * Multi-language support manager for cross-language operations in Minotaur.
 * Provides language detection, grammar mapping, and cross-language refactoring capabilities.
 */

import { Grammar, GrammarFormatType } from '../utils/Grammar';
import { ContextManager } from '../context/ContextManager';
import { RefactoringEngine } from '../refactoring/RefactoringEngine';
import { CodePosition } from '../context/ContextAwareParser';
import { EventEmitter } from 'events';

/**
 * Supported programming languages.
 */
export enum SupportedLanguage {
  JAVASCRIPT = 'javascript',
  TYPESCRIPT = 'typescript',
  PYTHON = 'python',
  JAVA = 'java',
  CSHARP = 'csharp',
  CPP = 'cpp',
  C = 'c',
  RUST = 'rust',
  GO = 'go',
  PHP = 'php',
  RUBY = 'ruby',
  SWIFT = 'swift',
  KOTLIN = 'kotlin',
  SCALA = 'scala',
  HTML = 'html',
  CSS = 'css',
  SQL = 'sql',
  JSON = 'json',
  XML = 'xml',
  YAML = 'yaml',
  MARKDOWN = 'markdown'
}

/**
 * Language detection result.
 */
export interface LanguageDetectionResult {
  language: SupportedLanguage;
  confidence: number;
  grammarFormat: GrammarFormatType;
  embeddedLanguages: EmbeddedLanguageInfo[];
  metadata: any;
}

/**
 * Embedded language information.
 */
export interface EmbeddedLanguageInfo {
  language: SupportedLanguage;
  startPosition: CodePosition;
  endPosition: CodePosition;
  context: string; // e.g., 'script_tag', 'template_literal', 'code_block'
  confidence: number;
}

/**
 * Language configuration.
 */
export interface LanguageConfig {
  language: SupportedLanguage;
  grammarFile: string;
  grammarFormat: GrammarFormatType;
  fileExtensions: string[];
  mimeTypes: string[];
  commentPatterns: CommentPattern[];
  stringDelimiters: string[];
  identifierPattern: RegExp;
  keywordPattern: RegExp;
  embeddingSupport: EmbeddingSupport;
}

/**
 * Comment pattern configuration.
 */
export interface CommentPattern {
  type: 'line' | 'block';
  start: string;
  end?: string;
  escape?: string;
}

/**
 * Embedding support configuration.
 */
export interface EmbeddingSupport {
  canEmbed: SupportedLanguage[];
  canBeEmbedded: boolean;
  embeddingPatterns: EmbeddingPattern[];
}

/**
 * Embedding pattern for detecting embedded languages.
 */
export interface EmbeddingPattern {
  hostLanguage: SupportedLanguage;
  embeddedLanguage: SupportedLanguage;
  startPattern: RegExp;
  endPattern: RegExp;
  context: string;
  extractContent: (match: RegExpMatchArray, fullText: string) => string;
}

/**
 * Cross-language operation request.
 */
export interface CrossLanguageOperation {
  id: string;
  type: 'refactor' | 'analyze' | 'validate' | 'transform';
  sourceLanguage: SupportedLanguage;
  targetLanguage?: SupportedLanguage;
  operation: string;
  parameters: any;
  scope: 'file' | 'project' | 'selection';
}

/**
 * Cross-language operation result.
 */
export interface CrossLanguageOperationResult {
  success: boolean;
  operationId: string;
  sourceLanguage: SupportedLanguage;
  targetLanguage?: SupportedLanguage;
  changes: any[];
  warnings: string[];
  errors: string[];
  metadata: any;
}

/**
 * Multi-language support manager.
 */
export class LanguageManager extends EventEmitter {
  private languageConfigs: Map<SupportedLanguage, LanguageConfig>;
  private embeddingPatterns: EmbeddingPattern[];
  private contextManager: ContextManager;
  private refactoringEngine: RefactoringEngine;
  private grammarCache: Map<string, Grammar>;

  constructor(contextManager: ContextManager, refactoringEngine: RefactoringEngine) {
    super();

    this.contextManager = contextManager;
    this.refactoringEngine = refactoringEngine;
    this.languageConfigs = new Map();
    this.embeddingPatterns = [];
    this.grammarCache = new Map();

    this.initializeLanguageConfigs();
    this.initializeEmbeddingPatterns();
  }

  /**
   * Initializes language configurations.
   */
  private initializeLanguageConfigs(): void {
    // JavaScript configuration
    this.languageConfigs.set(SupportedLanguage.JAVASCRIPT, {
      language: SupportedLanguage.JAVASCRIPT,
      grammarFile: 'javascript.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.js', '.mjs', '.jsx'],
      mimeTypes: ['application/javascript', 'text/javascript'],
      commentPatterns: [
        { type: 'line', start: '//' },
        { type: 'block', start: '/*', end: '*/' },
      ],
      stringDelimiters: ['"', "'", '`'],
      identifierPattern: /^[a-zA-Z_$][a-zA-Z0-9_$]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(var|let|const|function|class|if|else|for|while|do|switch|case|default|break|continue|return|try|catch|finally|throw|new|this|super|import|export|from|as|async|await|yield|typeof|instanceof|in|of|delete|void)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.HTML, SupportedLanguage.CSS, SupportedLanguage.SQL],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // TypeScript configuration
    this.languageConfigs.set(SupportedLanguage.TYPESCRIPT, {
      language: SupportedLanguage.TYPESCRIPT,
      grammarFile: 'typescript.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.ts', '.tsx'],
      mimeTypes: ['application/typescript', 'text/typescript'],
      commentPatterns: [
        { type: 'line', start: '//' },
        { type: 'block', start: '/*', end: '*/' },
      ],
      stringDelimiters: ['"', "'", '`'],
      identifierPattern: /^[a-zA-Z_$][a-zA-Z0-9_$]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(var|let|const|function|class|interface|type|enum|namespace|module|declare|abstract|implements|extends|public|private|protected|readonly|static|async|await|yield|typeof|instanceof|in|of|keyof|infer|is|asserts)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.HTML, SupportedLanguage.CSS, SupportedLanguage.SQL],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // Python configuration
    this.languageConfigs.set(SupportedLanguage.PYTHON, {
      language: SupportedLanguage.PYTHON,
      grammarFile: 'python.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.py', '.pyw', '.pyi'],
      mimeTypes: ['application/python', 'text/x-python'],
      commentPatterns: [
        { type: 'line', start: '#' },
        { type: 'block', start: '"""', end: '"""' },
        { type: 'block', start: "'''", end: "'''" },
      ],
      stringDelimiters: ['"', "'", '"""', "'''"],
      identifierPattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(and|as|assert|break|class|continue|def|del|elif|else|except|exec|finally|for|from|global|if|import|in|is|lambda|not|or|pass|print|raise|return|try|while|with|yield|async|await|nonlocal)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.SQL, SupportedLanguage.HTML, SupportedLanguage.JSON],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // Java configuration
    this.languageConfigs.set(SupportedLanguage.JAVA, {
      language: SupportedLanguage.JAVA,
      grammarFile: 'java.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.java'],
      mimeTypes: ['application/java', 'text/x-java'],
      commentPatterns: [
        { type: 'line', start: '//' },
        { type: 'block', start: '/*', end: '*/' },
        { type: 'block', start: '/**', end: '*/' },
      ],
      stringDelimiters: ['"'],
      identifierPattern: /^[a-zA-Z_$][a-zA-Z0-9_$]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|void|volatile|while)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.SQL, SupportedLanguage.XML, SupportedLanguage.JSON],
        canBeEmbedded: false,
        embeddingPatterns: [],
      },
    });

    // C# configuration
    this.languageConfigs.set(SupportedLanguage.CSHARP, {
      language: SupportedLanguage.CSHARP,
      grammarFile: 'csharp.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.cs'],
      mimeTypes: ['application/csharp', 'text/x-csharp'],
      commentPatterns: [
        { type: 'line', start: '//' },
        { type: 'block', start: '/*', end: '*/' },
        { type: 'block', start: '///', end: '\n' },
      ],
      stringDelimiters: ['"', "'", '@"'],
      identifierPattern: /^[a-zA-Z_@][a-zA-Z0-9_]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while|yield|async|await|var|dynamic|nameof|when|where)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.SQL, SupportedLanguage.XML, SupportedLanguage.JSON],
        canBeEmbedded: false,
        embeddingPatterns: [],
      },
    });

    // HTML configuration
    this.languageConfigs.set(SupportedLanguage.HTML, {
      language: SupportedLanguage.HTML,
      grammarFile: 'html.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.html', '.htm', '.xhtml'],
      mimeTypes: ['text/html', 'application/xhtml+xml'],
      commentPatterns: [
        { type: 'block', start: '<!--', end: '-->' },
      ],
      stringDelimiters: ['"', "'"],
      identifierPattern: /^[a-zA-Z][a-zA-Z0-9-]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(html|head|body|title|meta|link|script|style|div|span|p|a|img|ul|ol|li|table|tr|td|th|form|input|button|select|option|textarea|label|h1|h2|h3|h4|h5|h6|br|hr|strong|em|b|i|u|small|sub|sup|code|pre|blockquote|cite|abbr|address|article|aside|details|figcaption|figure|footer|header|main|mark|nav|section|summary|time)$/,
      embeddingSupport: {
        canEmbed: [SupportedLanguage.JAVASCRIPT, SupportedLanguage.CSS],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // CSS configuration
    this.languageConfigs.set(SupportedLanguage.CSS, {
      language: SupportedLanguage.CSS,
      grammarFile: 'css.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.css', '.scss', '.sass', '.less'],
      mimeTypes: ['text/css'],
      commentPatterns: [
        { type: 'block', start: '/*', end: '*/' },
      ],
      stringDelimiters: ['"', "'"],
      identifierPattern: /^[a-zA-Z_-][a-zA-Z0-9_-]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(color|background|border|margin|padding|width|height|font|text|display|position|top|right|bottom|left|float|clear|overflow|visibility|z-index|opacity|transform|transition|animation|flex|grid|align|justify|content|items|self|order|grow|shrink|basis|gap|auto|none|inherit|initial|unset|important)$/,
      embeddingSupport: {
        canEmbed: [],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // SQL configuration
    this.languageConfigs.set(SupportedLanguage.SQL, {
      language: SupportedLanguage.SQL,
      grammarFile: 'sql.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.sql'],
      mimeTypes: ['application/sql', 'text/sql'],
      commentPatterns: [
        { type: 'line', start: '--' },
        { type: 'block', start: '/*', end: '*/' },
      ],
      stringDelimiters: ["'", '"'],
      identifierPattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/,
      // eslint-disable-next-line max-len
      keywordPattern: /^(SELECT|FROM|WHERE|JOIN|INNER|LEFT|RIGHT|FULL|OUTER|ON|GROUP|BY|HAVING|ORDER|ASC|DESC|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|TABLE|INDEX|VIEW|PROCEDURE|FUNCTION|TRIGGER|DATABASE|SCHEMA|ALTER|DROP|TRUNCATE|GRANT|REVOKE|COMMIT|ROLLBACK|TRANSACTION|BEGIN|END|IF|ELSE|WHILE|FOR|DECLARE|EXEC|EXECUTE|RETURN|CASE|WHEN|THEN|ELSE|END|UNION|ALL|DISTINCT|EXISTS|IN|LIKE|BETWEEN|IS|NULL|NOT|AND|OR|AS|WITH|RECURSIVE)$/i,
      embeddingSupport: {
        canEmbed: [],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });

    // JSON configuration
    this.languageConfigs.set(SupportedLanguage.JSON, {
      language: SupportedLanguage.JSON,
      grammarFile: 'json.grammar',
      grammarFormat: GrammarFormatType.ANTLR4,
      fileExtensions: ['.json', '.jsonc'],
      mimeTypes: ['application/json', 'text/json'],
      commentPatterns: [], // Standard JSON doesn't support comments
      stringDelimiters: ['"'],
      identifierPattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/,
      keywordPattern: /^(true|false|null)$/,
      embeddingSupport: {
        canEmbed: [],
        canBeEmbedded: true,
        embeddingPatterns: [],
      },
    });
  }

  /**
   * Initializes embedding patterns for detecting embedded languages.
   */
  private initializeEmbeddingPatterns(): void {
    // JavaScript in HTML
    this.embeddingPatterns.push({
      hostLanguage: SupportedLanguage.HTML,
      embeddedLanguage: SupportedLanguage.JAVASCRIPT,
      startPattern: /<script(?:\s+[^>]*)?>/i,
      endPattern: /<\/script>/i,
      context: 'script_tag',
      extractContent: (match, fullText) => {
        const startIndex = match.index! + match[0].length;
        const endMatch = fullText.slice(startIndex).match(/<\/script>/i);
        if (endMatch) {
          return fullText.slice(startIndex, startIndex + endMatch.index!);
        }
        return '';
      },
    });

    // CSS in HTML
    this.embeddingPatterns.push({
      hostLanguage: SupportedLanguage.HTML,
      embeddedLanguage: SupportedLanguage.CSS,
      startPattern: /<style(?:\s+[^>]*)?>/i,
      endPattern: /<\/style>/i,
      context: 'style_tag',
      extractContent: (match, fullText) => {
        const startIndex = match.index! + match[0].length;
        const endMatch = fullText.slice(startIndex).match(/<\/style>/i);
        if (endMatch) {
          return fullText.slice(startIndex, startIndex + endMatch.index!);
        }
        return '';
      },
    });

    // Template literals in JavaScript/TypeScript
    this.embeddingPatterns.push({
      hostLanguage: SupportedLanguage.JAVASCRIPT,
      embeddedLanguage: SupportedLanguage.HTML,
      startPattern: /html`/,
      endPattern: /`/,
      context: 'template_literal',
      extractContent: (match, fullText) => {
        const startIndex = match.index! + match[0].length;
        let depth = 1;
        let i = startIndex;

        while (i < fullText.length && depth > 0) {
          if (fullText[i] === '`') {
            depth--;
          } else if (fullText[i] === '$' && fullText[i + 1] === '{') {
            depth++;
            i++; // Skip the '{'
          }
          i++;
        }

        return fullText.slice(startIndex, i - 1);
      },
    });

    // SQL in string literals
    this.embeddingPatterns.push({
      hostLanguage: SupportedLanguage.JAVASCRIPT,
      embeddedLanguage: SupportedLanguage.SQL,
      startPattern: /sql`/,
      endPattern: /`/,
      context: 'sql_template',
      extractContent: (match, fullText) => {
        const startIndex = match.index! + match[0].length;
        const endIndex = fullText.indexOf('`', startIndex);
        return endIndex !== -1 ? fullText.slice(startIndex, endIndex) : '';
      },
    });
  }

  /**
   * Detects the language of a file based on content and filename.
   */
  public detectLanguage(filename: string, content: string): LanguageDetectionResult {
    const results: Array<{ language: SupportedLanguage; confidence: number }> = [];

    // Check file extension
    const extension = this.getFileExtension(filename);
    for (const [language, config] of this.languageConfigs) {
      if (config.fileExtensions.includes(extension)) {
        results.push({ language, confidence: 0.8 });
      }
    }

    // Analyze content patterns
    for (const [language, config] of this.languageConfigs) {
      const contentConfidence = this.analyzeContentForLanguage(content, config);
      if (contentConfidence > 0.3) {
        results.push({ language, confidence: contentConfidence });
      }
    }

    // Find the best match
    const bestMatch = results.reduce((best, current) =>
      current.confidence > best.confidence ? current : best,
    { language: SupportedLanguage.JAVASCRIPT, confidence: 0 },
    );

    // Detect embedded languages
    const embeddedLanguages = this.detectEmbeddedLanguages(content, bestMatch.language);

    // Get grammar format
    const config = this.languageConfigs.get(bestMatch.language);
    const grammarFormat = config?.grammarFormat || GrammarFormatType.ANTLR4;

    return {
      language: bestMatch.language,
      confidence: bestMatch.confidence,
      grammarFormat,
      embeddedLanguages,
      metadata: {
        filename,
        extension,
        alternativeLanguages: results.filter(r => r.language !== bestMatch.language),
      },
    };
  }

  /**
   * Detects embedded languages within content.
   */
  public detectEmbeddedLanguages(content: string, hostLanguage: SupportedLanguage): EmbeddedLanguageInfo[] {
    const embeddedLanguages: EmbeddedLanguageInfo[] = [];

    // Find patterns for the host language
    const patterns = this.embeddingPatterns.filter(p => p.hostLanguage === hostLanguage);

    for (const pattern of patterns) {
      const matches = this.findEmbeddingMatches(content, pattern);
      embeddedLanguages.push(...matches);
    }

    return embeddedLanguages;
  }

  /**
   * Gets the appropriate grammar for a language.
   */
  public async getGrammarForLanguage(language: SupportedLanguage): Promise<Grammar | null> {
    const config = this.languageConfigs.get(language);
    if (!config) {
      return null;
    }

    // Check cache first
    const cacheKey = `${language}_${config.grammarFormat}`;
    if (this.grammarCache.has(cacheKey)) {
      return this.grammarCache.get(cacheKey)!;
    }

    try {
      // Load grammar from file
      const grammar = await this.loadGrammar(config.grammarFile, config.grammarFormat);

      // Cache the grammar
      this.grammarCache.set(cacheKey, grammar);

      return grammar;

    } catch (error) {
      this.emit('grammar_load_error', { language, error });
      return null;
    }
  }

  /**
   * Performs cross-language operations.
   */
  public async performCrossLanguageOperation(
    operation: CrossLanguageOperation,
  ): Promise<CrossLanguageOperationResult> {
    try {
      const startTime = Date.now();

      // Get source language grammar
      const sourceGrammar = await this.getGrammarForLanguage(operation.sourceLanguage);
      if (!sourceGrammar) {
        throw new Error(`Grammar not available for language: ${operation.sourceLanguage}`);
      }

      // Get target language grammar if specified
      let targetGrammar: Grammar | null = null;
      if (operation.targetLanguage) {
        targetGrammar = await this.getGrammarForLanguage(operation.targetLanguage);
        if (!targetGrammar) {
          throw new Error(`Grammar not available for target language: ${operation.targetLanguage}`);
        }
      }

      // Perform the operation based on type
      let result: any;
      switch (operation.type) {
        case 'refactor':
          result = await this.performCrossLanguageRefactoring(operation, sourceGrammar, targetGrammar);
          break;
        case 'analyze':
          result = await this.performCrossLanguageAnalysis(operation, sourceGrammar);
          break;
        case 'validate':
          result = await this.performCrossLanguageValidation(operation, sourceGrammar);
          break;
        case 'transform':
          result = await this.performCrossLanguageTransformation(operation, sourceGrammar, targetGrammar);
          break;
        default:
          throw new Error(`Unsupported operation type: ${operation.type}`);
      }

      const operationResult: CrossLanguageOperationResult = {
        success: true,
        operationId: operation.id,
        sourceLanguage: operation.sourceLanguage,
        targetLanguage: operation.targetLanguage,
        changes: result.changes || [],
        warnings: result.warnings || [],
        errors: [],
        metadata: {
          operationTime: Date.now() - startTime,
          ...result.metadata,
        },
      };

      this.emit('cross_language_operation_completed', { operation, result: operationResult });
      return operationResult;

    } catch (error) {
      const errorResult: CrossLanguageOperationResult = {
        success: false,
        operationId: operation.id,
        sourceLanguage: operation.sourceLanguage,
        targetLanguage: operation.targetLanguage,
        changes: [],
        warnings: [],
        errors: [error instanceof Error ? error.message : String(error)],
        metadata: { error: error instanceof Error ? error.message : String(error) },
      };

      this.emit('cross_language_operation_error', { operation, error });
      return errorResult;
    }
  }

  /**
   * Gets language configuration.
   */
  public getLanguageConfig(language: SupportedLanguage): LanguageConfig | null {
    return this.languageConfigs.get(language) || null;
  }

  /**
   * Gets all supported languages.
   */
  public getSupportedLanguages(): SupportedLanguage[] {
    return Array.from(this.languageConfigs.keys());
  }

  /**
   * Checks if a language can embed another language.
   */
  public canEmbed(hostLanguage: SupportedLanguage, embeddedLanguage: SupportedLanguage): boolean {
    const config = this.languageConfigs.get(hostLanguage);
    return config?.embeddingSupport.canEmbed.includes(embeddedLanguage) || false;
  }

  /**
   * Checks if a language can be embedded in another language.
   */
  public canBeEmbedded(language: SupportedLanguage): boolean {
    const config = this.languageConfigs.get(language);
    return config?.embeddingSupport.canBeEmbedded || false;
  }

  /**
   * Private helper methods.
   */

  private getFileExtension(filename: string): string {
    const lastDot = filename.lastIndexOf('.');
    return lastDot !== -1 ? filename.slice(lastDot) : '';
  }

  private analyzeContentForLanguage(content: string, config: LanguageConfig): number {
    let confidence = 0;

    // Check for keywords
    const words = content.match(/\b\w+\b/g) || [];
    const keywordMatches = words.filter(word => config.keywordPattern.test(word));
    confidence += Math.min(keywordMatches.length / words.length, 0.5);

    // Check for comment patterns
    for (const pattern of config.commentPatterns) {
      if (pattern.type === 'line' && content.includes(pattern.start)) {
        confidence += 0.1;
      } else if (pattern.type === 'block' && content.includes(pattern.start) && pattern.end && content.includes(pattern.end)) {
        confidence += 0.1;
      }
    }

    // Check for string delimiters
    for (const delimiter of config.stringDelimiters) {
      if (content.includes(delimiter)) {
        confidence += 0.05;
      }
    }

    return Math.min(confidence, 1.0);
  }

  private findEmbeddingMatches(content: string, pattern: EmbeddingPattern): EmbeddedLanguageInfo[] {
    const matches: EmbeddedLanguageInfo[] = [];
    let searchIndex = 0;

    while (searchIndex < content.length) {
      const startMatch = content.slice(searchIndex).match(pattern.startPattern);
      if (!startMatch) {
        break;
      }

      const absoluteStartIndex = searchIndex + startMatch.index!;
      const embeddedContent = pattern.extractContent(startMatch, content.slice(absoluteStartIndex));

      if (embeddedContent) {
        const startPosition = this.indexToPosition(content, absoluteStartIndex + startMatch[0].length);
        // eslint-disable-next-line max-len
        const endPosition = this.indexToPosition(content, absoluteStartIndex + startMatch[0].length + embeddedContent.length);

        matches.push({
          language: pattern.embeddedLanguage,
          startPosition,
          endPosition,
          context: pattern.context,
          confidence: 0.9,
        });
      }

      searchIndex = absoluteStartIndex + startMatch[0].length;
    }

    return matches;
  }

  private indexToPosition(content: string, index: number): CodePosition {
    const lines = content.slice(0, index).split('\n');
    return {
      line: lines.length,
      column: lines[lines.length - 1].length,
      offset: index,
    };
  }

  private async loadGrammar(grammarFile: string, format: GrammarFormatType): Promise<Grammar> {
    // Mock implementation - would load actual grammar files
    const grammarName = grammarFile.replace('.grammar', '');
    const grammar = new Grammar(grammarName);
    grammar.setFormatType(format);
    return grammar;
  }

  private async performCrossLanguageRefactoring(
    operation: CrossLanguageOperation,
    sourceGrammar: Grammar,
    targetGrammar: Grammar | null,
  ): Promise<any> {
    // Delegate to refactoring engine with language-specific context
    return {
      changes: [],
      warnings: [],
      metadata: { operation: 'refactoring' },
    };
  }

  private async performCrossLanguageAnalysis(
    operation: CrossLanguageOperation,
    sourceGrammar: Grammar,
  ): Promise<any> {
    // Perform language-specific analysis
    return {
      analysis: {},
      warnings: [],
      metadata: { operation: 'analysis' },
    };
  }

  private async performCrossLanguageValidation(
    operation: CrossLanguageOperation,
    sourceGrammar: Grammar,
  ): Promise<any> {
    // Perform language-specific validation
    return {
      valid: true,
      errors: [],
      warnings: [],
      metadata: { operation: 'validation' },
    };
  }

  private async performCrossLanguageTransformation(
    operation: CrossLanguageOperation,
    sourceGrammar: Grammar,
    targetGrammar: Grammar | null,
  ): Promise<any> {
    // Perform language transformation
    return {
      transformedCode: '',
      changes: [],
      warnings: [],
      metadata: { operation: 'transformation' },
    };
  }
}

// Export the main language manager class as default
export default LanguageManager;

