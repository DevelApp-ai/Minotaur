/**
 * ZeroCopyIntegration - Complete integration of zero-copy memory management
 *
 * Provides a unified interface for using all advanced memory management
 * components together in a cohesive, high-performance parsing system.
 */

import { MemoryArena, MemoryArenaStatistics } from '../memory/arena/MemoryArena';
import { StringInterner, StringInternerStatistics } from '../memory/strings/StringInterner';
import { ObjectPool as _ObjectPool, PoolManager, PoolStatistics } from '../memory/pools/ObjectPool';
import { MemoryCache as _MemoryCache, CacheManager, CacheStatistics } from '../memory/cache/MemoryCache';
import { AlignedToken, AlignedTokenArray, TokenType } from './tokens/AlignedToken';
import { ZeroCopyASTNode, ASTNodeType, ZeroCopyAST } from './ast/ZeroCopyASTNode';
import { ZeroCopySerializer, SerializationMetadata } from './serialization/ZeroCopySerializer';
// Fixed: Use the unified StepLexer and StepParser instead of non-existent zero-copy variants
import { StepLexer } from '../utils/StepLexer';
import { LexerOptions } from '../utils/LexerOptions';
import { StepParser } from '../utils/StepParser';
import { IParserLexerSourceContainer } from '../utils/IParserLexerSource';
import { ProductionMatch } from '../utils/ProductionMatch';

// Add missing type definitions
export interface LexingResult {
    tokens: AlignedToken[];
    statistics: {
        tokensGenerated: number;
        timeMs: number;
        memoryUsed: number;
    };
}

export interface ParsingResult {
    ast: ZeroCopyASTNode;
    statistics: {
        nodesCreated: number;
        timeMs: number;
        memoryUsed: number;
    };
}

export interface ZeroCopyConfig {
    arenaInitialSize?: number;
    arenaMaxSize?: number;
    stringCacheSize?: number;
    tokenCacheSize?: number;
    pathPoolSize?: number;
    enableProfiling?: boolean;
    enableOptimizations?: boolean;
}

export interface ZeroCopyStatistics {
    arena: MemoryArenaStatistics;
    stringInterner: StringInternerStatistics;
    pools: Map<string, PoolStatistics>;
    caches: Map<string, CacheStatistics>;
    lexing: LexingStatistics; // Use proper type
    parsing: ParsingStatistics; // Use proper type
    totalMemoryUsed: number;
    totalProcessingTime: number;
    performanceScore: number;
}

export interface LexingStatistics {
    tokensGenerated: number;
    timeMs: number;
    memoryUsed: number;
}

export interface ParsingStatistics {
    nodesCreated: number;
    timeMs: number;
    memoryUsed: number;
}

export interface ParsedDocument {
    ast: ZeroCopyASTNode;
    tokens: AlignedTokenArray;
    metadata: SerializationMetadata;
    statistics: ZeroCopyStatistics;
}

/**
 * Complete zero-copy parsing system
 */
export class ZeroCopyParsingSystem {
  private arena: MemoryArena;
  private stringInterner: StringInterner;
  private poolManager: PoolManager;
  private cacheManager: CacheManager;
  private lexer: StepLexer;
  private parser: StepParser;
  private config: ZeroCopyConfig;

  // Performance tracking
  private totalDocuments: number = 0;
  private totalProcessingTime: number = 0;
  private performanceHistory: number[] = [];

  constructor(config: ZeroCopyConfig = {}) {
    this.config = {
      arenaInitialSize: 64 * 1024, // 64KB
      arenaMaxSize: 256 * 1024 * 1024, // 256MB
      stringCacheSize: 10000,
      tokenCacheSize: 50000,
      pathPoolSize: 1000,
      enableProfiling: true,
      enableOptimizations: true,
      ...config,
    };

    this.initializeComponents();
  }

  /**
     * Initialize all zero-copy components
     */
  private initializeComponents(): void {
    // Initialize memory arena
    this.arena = new MemoryArena(
            this.config.arenaInitialSize!,
            this.config.arenaMaxSize!,
    );

    // Initialize string interner
    this.stringInterner = new StringInterner(this.arena);

    // Initialize pool manager
    this.poolManager = new PoolManager(this.arena);

    // Initialize cache manager
    this.cacheManager = new CacheManager(this.arena);

    // Initialize parser first (required by lexer)
    this.parser = new StepParser();

    // Create default lexer options
    const lexerOptions = new LexerOptions(false, false);

    // Create a basic source container
    const sourceContainer: IParserLexerSourceContainer = {
      getSourceLines: () => [],
      getCount: () => 0,
      getLine: (_fileName: string, _lineNumber: number) => ({
        getContent: () => '',
        getLength: () => 0,
        getLineNumber: () => 0,
        getFileName: () => '',
      }),
    };

    // Initialize lexer (using the unified StepLexer)
    this.lexer = new StepLexer(this.parser, lexerOptions, sourceContainer);
  }

  /**
     * Parse a complete document from source text
     */
  async parseDocument(
    sourceText: string,
    sourceFile: string = 'unknown',
  ): Promise<ParsedDocument> {
    const startTime = Date.now();
    const startMemory = this.arena.getStatistics().totalUsed;

    try {
      // Step 1: Tokenize the input using StepLexer
      // Create a source container from the original text
      const lines = sourceText.split('\n');
      const sourceContainer: IParserLexerSourceContainer = {
        getSourceLines: () => lines.map((content, index) => ({
          getContent: () => content,
          getLength: () => content.length,
          getLineNumber: () => index,
          getFileName: () => 'inline',
        })),
        getCount: () => lines.length,
        getLine: (_fileName: string, lineNumber: number) => ({
          getContent: () => lines[lineNumber] || '',
          getLength: () => lines[lineNumber]?.length || 0,
          getLineNumber: () => lineNumber,
          getFileName: () => 'inline',
        }),
      };

      // Use StepLexer's nextTokens method to get tokens
      const tokens: AlignedToken[] = [];
      for (const tokenBatch of this.lexer.nextTokens()) {
        // Convert Token[] to AlignedToken[]
        for (const token of tokenBatch) {
          // Allocate memory for the token
          const tokenPointer = this.arena.allocate(
            AlignedToken.getByteSize(),
            AlignedToken.getAlignment(),
          );
          const alignedToken = new AlignedToken(
            tokenPointer,
            this.stringInterner,
            TokenType.IDENTIFIER, // Default type, should be mapped properly
            token.getValue() || '',
            {
              start: { line: 0, column: 0, offset: 0 },
              end: { line: 0, column: 0, offset: token.getValue()?.length || 0 },
            },
          );
          tokens.push(alignedToken);
        }
      }

      // Create AlignedTokenArray from tokens
      const tokenArray = new AlignedTokenArray(this.arena, this.stringInterner);
      for (const token of tokens) {
        tokenArray.push(
          TokenType.IDENTIFIER, // Default type, should be mapped properly
          token.value || '',
          {
            start: { line: 0, column: 0, offset: 0 },
            end: { line: 0, column: 0, offset: token.value?.length || 0 },
          },
        );
      }

      const lexingEndTime = Date.now();
      const lexingMemory = this.arena.getStatistics().totalUsed - startMemory;

      const lexingResult: LexingResult = {
        tokens: tokens,
        statistics: {
          tokensGenerated: tokens.length,
          timeMs: lexingEndTime - startTime,
          memoryUsed: lexingMemory,
        },
      };

      // Step 2: Parse the tokens
      const parseMatches = await this.parser.parse('default', sourceContainer);

      const parseEndTime = Date.now();
      const totalMemory = this.arena.getStatistics().totalUsed - startMemory;
      const parsingMemory = totalMemory - lexingMemory;

      // Create a proper ParsingResult structure
      const parsingResult: ParsingResult = {
        ast: this.createASTFromMatches(parseMatches),
        statistics: {
          nodesCreated: parseMatches.length,
          timeMs: parseEndTime - lexingEndTime,
          memoryUsed: parsingMemory,
        },
      };

      if (!parsingResult.ast) {
        throw new Error('Failed to parse document - no valid AST generated');
      }

      // Step 3: Create metadata
      const metadata: SerializationMetadata = {
        timestamp: Date.now(),
        generator: 'Minotaur-ZeroCopy',
        sourceFile,
        checksum: this.calculateChecksum(sourceText),
      };

      // Step 4: Collect statistics
      const statistics = this.collectStatistics(lexingResult, parsingResult);

      // Step 5: Update performance tracking
      const processingTime = Date.now() - startTime;
      this.updatePerformanceTracking(processingTime);

      return {
        ast: parsingResult.ast,
        tokens: tokenArray,
        metadata,
        statistics,
      };

    } catch (error) {
      throw new Error(`Document parsing failed: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
     * Parse multiple documents in parallel
     */
  async parseDocuments(
    documents: Array<{ text: string; file: string }>,
  ): Promise<ParsedDocument[]> {
    const results: ParsedDocument[] = [];

    // Process documents in batches to manage memory
    const batchSize = 10;
    for (let i = 0; i < documents.length; i += batchSize) {
      const batch = documents.slice(i, i + batchSize);

      const batchPromises = batch.map(doc =>
        this.parseDocument(doc.text, doc.file),
      );

      const batchResults = await Promise.all(batchPromises);
      results.push(...batchResults);

      // Optional: trigger garbage collection between batches
      if (this.config.enableOptimizations) {
        this.optimizeMemory();
      }
    }

    return results;
  }

  /**
   * Create a ZeroCopyASTNode from ProductionMatch array
   */
  private createASTFromMatches(matches: ProductionMatch[]): ZeroCopyASTNode {
    // Create a root node using static create method
    const root = ZeroCopyASTNode.create(
      this.arena,
      this.stringInterner,
      ASTNodeType.PROGRAM,
      'document',
    );

    // Convert matches to child nodes
    for (const match of matches) {
      const node = ZeroCopyASTNode.create(
        this.arena,
        this.stringInterner,
        ASTNodeType.STATEMENT,
        match.getProduction()?.getName() || 'unknown',
        match.getValue() || '',
      );
      root.appendChild(node);
    }

    return root;
  }

  /**
     * Serialize a parsed document to binary format
     */
  serializeDocument(document: ParsedDocument): ArrayBuffer {
    // Create a temporary AST wrapper for serialization
    const astWrapper = new ZeroCopyAST(this.arena, this.stringInterner);
    // Use the public setRoot method instead of accessing private property
    astWrapper.setRoot(document.ast);

    return ZeroCopySerializer.serializeAST(astWrapper, document.metadata);
  }

  /**
     * Deserialize a document from binary format
     */
  deserializeDocument(buffer: ArrayBuffer): ParsedDocument {
    const { ast: astWrapper, metadata } = ZeroCopySerializer.deserializeAST(buffer);
    const ast = astWrapper.getRoot();

    if (!ast) {
      throw new Error('Failed to deserialize document - no root AST node');
    }

    // Create empty token array (tokens not preserved in serialization)
    const tokens = new AlignedTokenArray(this.arena, this.stringInterner);

    // Create minimal statistics
    const statistics = this.createMinimalStatistics();

    return {
      ast,
      tokens,
      metadata,
      statistics,
    };
  }

  /**
     * Optimize memory usage
     */
  optimizeMemory(): void {
    // Trim all object pools
    this.poolManager.trimAll();

    // Clear least recently used cache entries
    // (This would be implemented in the cache manager)

    // Force garbage collection on pools
    this.poolManager.forceGCAll();

    // Clear string interner duplicates (if implemented)
    // this.stringInterner.optimize();
  }

  /**
     * Reset the parsing system
     */
  reset(): void {
    // Clear all caches and pools
    this.poolManager.clearAll();
    this.cacheManager.clearAll();

    // Reset arena
    this.arena.reset();

    // Reinitialize string interner
    this.stringInterner = new StringInterner(this.arena);

    // Reset performance tracking
    this.totalDocuments = 0;
    this.totalProcessingTime = 0;
    this.performanceHistory = [];
  }

  /**
     * Get comprehensive system statistics
     */
  getSystemStatistics(): ZeroCopyStatistics {
    return this.createMinimalStatistics();
  }

  /**
     * Get performance metrics
     */
  getPerformanceMetrics(): PerformanceMetrics {
    const avgProcessingTime = this.totalDocuments > 0 ?
      this.totalProcessingTime / this.totalDocuments : 0;

    const recentPerformance = this.performanceHistory.slice(-10);
    const recentAvg = recentPerformance.length > 0 ?
      recentPerformance.reduce((sum, time) => sum + time, 0) / recentPerformance.length : 0;

    return {
      totalDocuments: this.totalDocuments,
      totalProcessingTime: this.totalProcessingTime,
      averageProcessingTime: avgProcessingTime,
      recentAverageProcessingTime: recentAvg,
      memoryEfficiency: this.calculateMemoryEfficiency(),
      throughput: this.calculateThroughput(),
    };
  }

  /**
     * Validate system integrity
     */
  validateIntegrity(): ValidationResult {
    const issues: string[] = [];

    // Check arena integrity
    const arenaStats = this.arena.getStatistics();
    if (arenaStats.fragmentationRatio > 0.5) {
      issues.push('High memory fragmentation detected');
    }

    // Check string interner efficiency
    const stringStats = this.stringInterner.getStatistics();
    if (stringStats.deduplicationRatio < 0.3) {
      issues.push('Low string deduplication efficiency');
    }

    // Check pool utilization
    const poolStats = this.poolManager.getAllStatistics();
    for (const [name, stats] of poolStats) {
      if (stats.hitRate < 0.7) {
        issues.push(`Low hit rate for pool: ${name}`);
      }
    }

    return {
      isValid: issues.length === 0,
      issues,
      score: this.calculateIntegrityScore(issues.length),
    };
  }

  /**
     * Create benchmark test
     */
  async runBenchmark(testCases: BenchmarkTestCase[]): Promise<BenchmarkResult> {
    const results: BenchmarkCaseResult[] = [];

    for (const testCase of testCases) {
      const startTime = Date.now();
      const startMemory = this.arena.getStatistics().totalUsed;

      try {
        const document = await this.parseDocument(testCase.input, testCase.name);
        const endTime = Date.now();
        const endMemory = this.arena.getStatistics().totalUsed;

        results.push({
          name: testCase.name,
          success: true,
          processingTime: endTime - startTime,
          memoryUsed: endMemory - startMemory,
          tokensGenerated: document.tokens.length,
          nodesCreated: this.countASTNodes(document.ast),
          error: null,
        });
      } catch (error) {
        results.push({
          name: testCase.name,
          success: false,
          processingTime: Date.now() - startTime,
          memoryUsed: 0,
          tokensGenerated: 0,
          nodesCreated: 0,
          error: error instanceof Error ? error.message : String(error),
        });
      }
    }

    return {
      testCases: results,
      totalTime: results.reduce((sum, r) => sum + r.processingTime, 0),
      totalMemory: results.reduce((sum, r) => sum + r.memoryUsed, 0),
      successRate: results.filter(r => r.success).length / results.length,
      averageProcessingTime: results.reduce((sum, r) => sum + r.processingTime, 0) / results.length,
    };
  }

  /**
     * Collect comprehensive statistics
     */
  private collectStatistics(
    lexingResult: LexingResult,
    parsingResult: ParsingResult,
  ): ZeroCopyStatistics {
    const totalProcessingTime = lexingResult.statistics.timeMs +
                                  parsingResult.statistics.timeMs;

    return {
      arena: this.arena.getStatistics(),
      stringInterner: this.stringInterner.getStatistics(),
      pools: this.poolManager.getAllStatistics(),
      caches: this.cacheManager.getAllStatistics(),
      lexing: lexingResult.statistics,
      parsing: parsingResult.statistics,
      totalMemoryUsed: this.arena.getStatistics().totalUsed,
      totalProcessingTime,
      performanceScore: this.calculatePerformanceScore(totalProcessingTime),
    };
  }

  /**
     * Create minimal statistics for deserialized documents
     */
  private createMinimalStatistics(): ZeroCopyStatistics {
    return {
      arena: this.arena.getStatistics(),
      stringInterner: this.stringInterner.getStatistics(),
      pools: this.poolManager.getAllStatistics(),
      caches: this.cacheManager.getAllStatistics(),
      lexing: {
        tokensGenerated: 0,
        timeMs: 0,
        memoryUsed: 0,
      },
      parsing: {
        nodesCreated: 0,
        timeMs: 0,
        memoryUsed: 0,
      },
      totalMemoryUsed: this.arena.getStatistics().totalUsed,
      totalProcessingTime: 0,
      performanceScore: 0,
    };
  }

  /**
     * Update performance tracking
     */
  private updatePerformanceTracking(processingTime: number): void {
    this.totalDocuments++;
    this.totalProcessingTime += processingTime;
    this.performanceHistory.push(processingTime);

    // Keep only recent history
    if (this.performanceHistory.length > 100) {
      this.performanceHistory.shift();
    }
  }

  /**
     * Calculate checksum for source text
     */
  private calculateChecksum(text: string): number {
    let hash = 0;
    for (let i = 0; i < text.length; i++) {
      const char = text.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash >>> 0; // Convert to unsigned
  }

  /**
     * Calculate performance score
     */
  private calculatePerformanceScore(processingTime: number): number {
    // Simple scoring: lower time = higher score
    const baseScore = 1000;
    const timePenalty = Math.min(processingTime, 1000);
    return Math.max(0, baseScore - timePenalty);
  }

  /**
     * Calculate memory efficiency
     */
  private calculateMemoryEfficiency(): number {
    const arenaStats = this.arena.getStatistics();
    return arenaStats.totalCapacity > 0 ?
      arenaStats.totalUsed / arenaStats.totalCapacity : 0;
  }

  /**
     * Calculate throughput (documents per second)
     */
  private calculateThroughput(): number {
    return this.totalProcessingTime > 0 ?
      (this.totalDocuments * 1000) / this.totalProcessingTime : 0;
  }

  /**
     * Calculate integrity score
     */
  private calculateIntegrityScore(issueCount: number): number {
    return Math.max(0, 100 - (issueCount * 10));
  }

  /**
     * Count AST nodes recursively
     */
  private countASTNodes(node: ZeroCopyASTNode): number {
    let count = 1;
    for (const child of node.getChildren()) {
      count += this.countASTNodes(child);
    }
    return count;
  }
}

/**
 * Supporting interfaces
 */
export interface PerformanceMetrics {
    totalDocuments: number;
    totalProcessingTime: number;
    averageProcessingTime: number;
    recentAverageProcessingTime: number;
    memoryEfficiency: number;
    throughput: number;
}

export interface ValidationResult {
    isValid: boolean;
    issues: string[];
    score: number;
}

export interface BenchmarkTestCase {
    name: string;
    input: string;
    expectedTokens?: number;
    expectedNodes?: number;
}

export interface BenchmarkCaseResult {
    name: string;
    success: boolean;
    processingTime: number;
    memoryUsed: number;
    tokensGenerated: number;
    nodesCreated: number;
    error: string | null;
}

export interface BenchmarkResult {
    testCases: BenchmarkCaseResult[];
    totalTime: number;
    totalMemory: number;
    successRate: number;
    averageProcessingTime: number;
}

