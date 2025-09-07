/**
 * Comprehensive test suite for Minotaur's advanced memory management system
 * 
 * Tests all components of the zero-copy memory management implementation
 * including arena allocation, string interning, object pooling, caching,
 * and the integrated parsing system.
 */

import { MemoryArena } from './src/memory/arena/MemoryArena';
import { StringInterner } from './src/memory/strings/StringInterner';
import { ObjectPool, PooledMemoryPointer } from './src/memory/pools/ObjectPool';
import { MemoryCache, StringCache } from './src/memory/cache/MemoryCache';
import { AlignedToken, AlignedTokenArray, TokenType } from './src/zerocopy/tokens/AlignedToken';
import { ZeroCopyASTNode, ASTNodeType } from './src/zerocopy/ast/ZeroCopyASTNode';
import { ZeroCopySerializer } from './src/zerocopy/serialization/ZeroCopySerializer';
import { ZeroCopyStepLexer } from './src/zerocopy/parser/ZeroCopyStepLexer';
import { ZeroCopyStepParser } from './src/zerocopy/parser/ZeroCopyStepParser';
import { ZeroCopyParsingSystem, BenchmarkTestCase } from './src/zerocopy/ZeroCopyIntegration';

interface TestResult {
    name: string;
    passed: boolean;
    duration: number;
    error?: string;
    details?: any;
}

interface TestSuite {
    name: string;
    tests: TestResult[];
    totalTests: number;
    passedTests: number;
    totalDuration: number;
}

class AdvancedMemoryManagementTester {
    private testSuites: TestSuite[] = [];
    
    /**
     * Run all test suites
     */
    async runAllTests(): Promise<void> {
        console.log('🚀 Starting Advanced Memory Management Test Suite');
        console.log('=' .repeat(60));
        
        // Run individual component tests
        await this.testMemoryArena();
        await this.testStringInterner();
        await this.testObjectPooling();
        await this.testMemoryCache();
        await this.testAlignedTokens();
        await this.testZeroCopyAST();
        await this.testSerialization();
        await this.testZeroCopyLexer();
        await this.testZeroCopyParser();
        await this.testIntegratedSystem();
        await this.testPerformanceBenchmarks();
        
        // Print summary
        this.printTestSummary();
    }
    
    /**
     * Test MemoryArena functionality
     */
    private async testMemoryArena(): Promise<void> {
        const suite: TestSuite = {
            name: 'Memory Arena',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic allocation
        suite.tests.push(await this.runTest('Basic Allocation', () => {
            const arena = new MemoryArena(1024);
            const pointer1 = arena.allocate(64, 8);
            const pointer2 = arena.allocate(128, 16);
            
            if (pointer1.offset !== 0) throw new Error('First allocation should start at offset 0');
            if (pointer2.offset !== 64) throw new Error('Second allocation should start at offset 64');
            
            return { allocatedBytes: 192 };
        }));
        
        // Test 2: Alignment
        suite.tests.push(await this.runTest('Memory Alignment', () => {
            const arena = new MemoryArena(1024);
            const pointer1 = arena.allocate(10, 8); // Should be aligned to 8 bytes
            const pointer2 = arena.allocate(10, 16); // Should be aligned to 16 bytes
            
            if (pointer1.offset % 8 !== 0) throw new Error('Pointer not aligned to 8 bytes');
            if (pointer2.offset % 16 !== 0) throw new Error('Pointer not aligned to 16 bytes');
            
            return { pointer1Offset: pointer1.offset, pointer2Offset: pointer2.offset };
        }));
        
        // Test 3: Batch allocation
        suite.tests.push(await this.runTest('Batch Allocation', () => {
            const arena = new MemoryArena(4096);
            const pointers = arena.allocateBatch(64, 10, 8);
            
            if (pointers.length !== 10) throw new Error('Should allocate 10 pointers');
            
            // Check contiguous allocation
            for (let i = 1; i < pointers.length; i++) {
                const expectedOffset = pointers[0].offset + (i * 64);
                if (pointers[i].offset !== expectedOffset) {
                    throw new Error(`Pointer ${i} not contiguous`);
                }
            }
            
            return { batchSize: pointers.length };
        }));
        
        // Test 4: Statistics
        suite.tests.push(await this.runTest('Arena Statistics', () => {
            const arena = new MemoryArena(2048);
            arena.allocate(512, 8);
            arena.allocate(256, 8);
            
            const stats = arena.getStatistics();
            if (stats.totalUsed < 768) throw new Error('Total used should be at least 768 bytes');
            if (stats.allocationCount !== 2) throw new Error('Should have 2 allocations');
            
            return stats;
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test StringInterner functionality
     */
    private async testStringInterner(): Promise<void> {
        const suite: TestSuite = {
            name: 'String Interner',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic interning
        suite.tests.push(await this.runTest('Basic String Interning', () => {
            const arena = new MemoryArena(4096);
            const interner = new StringInterner(arena);
            
            const id1 = interner.intern('hello');
            const id2 = interner.intern('world');
            const id3 = interner.intern('hello'); // Duplicate
            
            if (id1 === 0) throw new Error('String ID should not be 0');
            if (id1 !== id3) throw new Error('Duplicate strings should have same ID');
            if (id1 === id2) throw new Error('Different strings should have different IDs');
            
            return { id1, id2, id3 };
        }));
        
        // Test 2: String retrieval
        suite.tests.push(await this.runTest('String Retrieval', () => {
            const arena = new MemoryArena(4096);
            const interner = new StringInterner(arena);
            
            const original = 'test string';
            const id = interner.intern(original);
            const retrieved = interner.getString(id);
            
            if (retrieved !== original) throw new Error('Retrieved string does not match original');
            
            return { original, retrieved };
        }));
        
        // Test 3: Deduplication statistics
        suite.tests.push(await this.runTest('Deduplication Statistics', () => {
            const arena = new MemoryArena(4096);
            const interner = new StringInterner(arena);
            
            // Intern some strings with duplicates
            interner.intern('duplicate');
            interner.intern('unique1');
            interner.intern('duplicate'); // Duplicate
            interner.intern('unique2');
            interner.intern('duplicate'); // Another duplicate
            
            const stats = interner.getStatistics();
            if (stats.duplicatesAvoided !== 2) throw new Error('Should have avoided 2 duplicates');
            if (stats.uniqueStrings !== 3) throw new Error('Should have 3 unique strings');
            
            return stats;
        }));
        
        // Test 4: Serialization
        suite.tests.push(await this.runTest('String Table Serialization', () => {
            const arena = new MemoryArena(4096);
            const interner = new StringInterner(arena);
            
            interner.intern('first');
            interner.intern('second');
            interner.intern('third');
            
            const serialized = interner.serialize();
            const newArena = new MemoryArena(4096);
            const newInterner = StringInterner.deserialize(serialized, newArena);
            
            // Verify strings are preserved
            if (newInterner.getString(1) !== 'first') throw new Error('First string not preserved');
            if (newInterner.getString(2) !== 'second') throw new Error('Second string not preserved');
            if (newInterner.getString(3) !== 'third') throw new Error('Third string not preserved');
            
            return { serializedSize: serialized.byteLength };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test ObjectPool functionality
     */
    private async testObjectPooling(): Promise<void> {
        const suite: TestSuite = {
            name: 'Object Pooling',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic pooling
        suite.tests.push(await this.runTest('Basic Object Pooling', () => {
            const arena = new MemoryArena(4096);
            const pool = new ObjectPool(
                {
                    create: () => new PooledMemoryPointer(arena),
                    reset: (obj) => obj.reset(),
                    validate: (obj) => obj.isValid()
                },
                arena,
                5, // min size
                100 // max size
            );
            
            // Acquire objects
            const obj1 = pool.acquire();
            const obj2 = pool.acquire();
            
            if (!obj1.isInUse()) throw new Error('Object should be marked as in use');
            if (!obj2.isInUse()) throw new Error('Object should be marked as in use');
            
            // Release objects
            pool.release(obj1);
            pool.release(obj2);
            
            if (obj1.isInUse()) throw new Error('Object should not be in use after release');
            
            const stats = pool.getStatistics();
            return stats;
        }));
        
        // Test 2: Pool reuse
        suite.tests.push(await this.runTest('Pool Object Reuse', () => {
            const arena = new MemoryArena(4096);
            const pool = new ObjectPool(
                {
                    create: () => new PooledMemoryPointer(arena),
                    reset: (obj) => obj.reset(),
                    validate: (obj) => obj.isValid()
                },
                arena,
                2,
                10
            );
            
            // Acquire and release to populate pool
            const obj1 = pool.acquire();
            pool.release(obj1);
            
            // Acquire again - should reuse the same object
            const obj2 = pool.acquire();
            
            const stats = pool.getStatistics();
            if (stats.hitRate <= 0) throw new Error('Should have hit rate > 0 for reused object');
            
            return stats;
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test MemoryCache functionality
     */
    private async testMemoryCache(): Promise<void> {
        const suite: TestSuite = {
            name: 'Memory Cache',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic caching
        suite.tests.push(await this.runTest('Basic Caching', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            const cache = new StringCache(arena, stringInterner);
            
            cache.put('key1', 'value1');
            cache.put('key2', 'value2');
            
            const value1 = cache.get('key1');
            const value2 = cache.get('key2');
            const missing = cache.get('missing');
            
            if (value1 !== 'value1') throw new Error('Cached value1 incorrect');
            if (value2 !== 'value2') throw new Error('Cached value2 incorrect');
            if (missing !== undefined) throw new Error('Missing key should return undefined');
            
            return { value1, value2 };
        }));
        
        // Test 2: Cache statistics
        suite.tests.push(await this.runTest('Cache Statistics', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            const cache = new StringCache(arena, stringInterner);
            
            cache.put('test', 'value');
            cache.get('test'); // Hit
            cache.get('missing'); // Miss
            
            const stats = cache.getStatistics();
            if (stats.hitCount !== 1) throw new Error('Should have 1 hit');
            if (stats.missCount !== 1) throw new Error('Should have 1 miss');
            if (stats.hitRate !== 0.5) throw new Error('Hit rate should be 0.5');
            
            return stats;
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test AlignedToken functionality
     */
    private async testAlignedTokens(): Promise<void> {
        const suite: TestSuite = {
            name: 'Aligned Tokens',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Token creation
        suite.tests.push(await this.runTest('Token Creation', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            
            const token = AlignedToken.create(
                arena,
                stringInterner,
                TokenType.IDENTIFIER,
                'myVariable',
                {
                    start: { line: 1, column: 1, offset: 0 },
                    end: { line: 1, column: 11, offset: 10 }
                }
            );
            
            if (token.type !== TokenType.IDENTIFIER) throw new Error('Token type incorrect');
            if (token.value !== 'myVariable') throw new Error('Token value incorrect');
            if (token.span.start.line !== 1) throw new Error('Token span incorrect');
            
            return { type: token.type, value: token.value };
        }));
        
        // Test 2: Token array
        suite.tests.push(await this.runTest('Token Array', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            const tokenArray = new AlignedTokenArray(arena, stringInterner, 100);
            
            tokenArray.push(TokenType.KEYWORD, 'function');
            tokenArray.push(TokenType.IDENTIFIER, 'test');
            tokenArray.push(TokenType.PUNCTUATION, '(');
            
            if (tokenArray.length !== 3) throw new Error('Token array should have 3 tokens');
            
            const firstToken = tokenArray.get(0);
            if (firstToken.type !== TokenType.KEYWORD) throw new Error('First token should be keyword');
            if (firstToken.value !== 'function') throw new Error('First token value incorrect');
            
            return { length: tokenArray.length };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test ZeroCopyASTNode functionality
     */
    private async testZeroCopyAST(): Promise<void> {
        const suite: TestSuite = {
            name: 'Zero-Copy AST',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Node creation
        suite.tests.push(await this.runTest('AST Node Creation', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.FUNCTION_DECLARATION,
                'function',
                'test'
            );
            
            if (node.nodeType !== ASTNodeType.FUNCTION_DECLARATION) throw new Error('Node type incorrect');
            if (node.name !== 'function') throw new Error('Node name incorrect');
            if (node.value !== 'test') throw new Error('Node value incorrect');
            
            return { nodeType: node.nodeType, name: node.name };
        }));
        
        // Test 2: Parent-child relationships
        suite.tests.push(await this.runTest('Parent-Child Relationships', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            
            const parent = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.PROGRAM, 'program');
            const child1 = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.STATEMENT, 'statement1');
            const child2 = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.STATEMENT, 'statement2');
            
            parent.appendChild(child1);
            parent.appendChild(child2);
            
            if (parent.childCount !== 2) throw new Error('Parent should have 2 children');
            
            const retrievedChild1 = parent.getChild(0);
            const retrievedChild2 = parent.getChild(1);
            
            if (!retrievedChild1 || retrievedChild1.name !== 'statement1') {
                throw new Error('First child incorrect');
            }
            if (!retrievedChild2 || retrievedChild2.name !== 'statement2') {
                throw new Error('Second child incorrect');
            }
            
            return { childCount: parent.childCount };
        }));
        
        // Test 3: Tree traversal
        suite.tests.push(await this.runTest('Tree Traversal', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            
            const root = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.PROGRAM, 'root');
            const child1 = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.STATEMENT, 'child1');
            const child2 = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.STATEMENT, 'child2');
            const grandchild = ZeroCopyASTNode.create(arena, stringInterner, ASTNodeType.EXPRESSION, 'grandchild');
            
            root.appendChild(child1);
            root.appendChild(child2);
            child1.appendChild(grandchild);
            
            const nodes = Array.from(root.traverse());
            if (nodes.length !== 4) throw new Error('Should traverse 4 nodes');
            
            const names = nodes.map(node => node.name);
            return { nodeCount: nodes.length, names };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test serialization functionality
     */
    private async testSerialization(): Promise<void> {
        const suite: TestSuite = {
            name: 'Serialization',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Token serialization
        suite.tests.push(await this.runTest('Token Serialization', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            const tokenArray = new AlignedTokenArray(arena, stringInterner, 10);
            
            tokenArray.push(TokenType.KEYWORD, 'function');
            tokenArray.push(TokenType.IDENTIFIER, 'test');
            
            const serialized = ZeroCopySerializer.serializeTokens(tokenArray, stringInterner);
            const { tokens: deserializedTokens } = ZeroCopySerializer.deserializeTokens(serialized);
            
            if (deserializedTokens.length !== 2) throw new Error('Should deserialize 2 tokens');
            
            return { serializedSize: serialized.byteLength };
        }));
        
        // Test 2: Data integrity validation
        suite.tests.push(await this.runTest('Data Integrity Validation', () => {
            const arena = new MemoryArena(4096);
            const stringInterner = new StringInterner(arena);
            const tokenArray = new AlignedTokenArray(arena, stringInterner, 5);
            
            tokenArray.push(TokenType.NUMBER, '42');
            
            const serialized = ZeroCopySerializer.serializeTokens(tokenArray, stringInterner);
            const isValid = ZeroCopySerializer.validateIntegrity(serialized);
            
            if (!isValid) throw new Error('Serialized data should be valid');
            
            // Corrupt the data
            const corruptedData = new Uint8Array(serialized);
            corruptedData[10] = 255; // Corrupt a byte
            
            const isCorruptedValid = ZeroCopySerializer.validateIntegrity(corruptedData.buffer);
            if (isCorruptedValid) throw new Error('Corrupted data should be invalid');
            
            return { originalValid: isValid, corruptedValid: isCorruptedValid };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test ZeroCopyStepLexer functionality
     */
    private async testZeroCopyLexer(): Promise<void> {
        const suite: TestSuite = {
            name: 'Zero-Copy Lexer',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic tokenization
        suite.tests.push(await this.runTest('Basic Tokenization', () => {
            const arena = new MemoryArena(8192);
            const stringInterner = new StringInterner(arena);
            const lexer = new ZeroCopyStepLexer(arena, stringInterner);
            
            const input = 'function test() { return 42; }';
            const result = lexer.tokenize(input);
            
            if (result.tokens.length === 0) throw new Error('Should generate tokens');
            
            // Find non-whitespace tokens
            const nonTriviaTokens = result.tokens.getNonTriviaTokens();
            if (nonTriviaTokens.length < 6) throw new Error('Should have at least 6 meaningful tokens');
            
            return { 
                totalTokens: result.tokens.length,
                nonTriviaTokens: nonTriviaTokens.length,
                lexingTime: result.statistics.lexingTimeMs
            };
        }));
        
        // Test 2: Multiple paths
        suite.tests.push(await this.runTest('Multiple Lexer Paths', () => {
            const arena = new MemoryArena(8192);
            const stringInterner = new StringInterner(arena);
            const lexer = new ZeroCopyStepLexer(arena, stringInterner);
            
            // Input that could be ambiguous
            const input = '"string" + 123';
            const result = lexer.tokenize(input);
            
            if (result.paths.length === 0) throw new Error('Should have lexer paths');
            if (result.statistics.totalPaths === 0) throw new Error('Should track path statistics');
            
            return {
                pathCount: result.paths.length,
                totalPaths: result.statistics.totalPaths
            };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test ZeroCopyStepParser functionality
     */
    private async testZeroCopyParser(): Promise<void> {
        const suite: TestSuite = {
            name: 'Zero-Copy Parser',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Basic parsing
        suite.tests.push(await this.runTest('Basic Parsing', () => {
            const arena = new MemoryArena(8192);
            const stringInterner = new StringInterner(arena);
            const lexer = new ZeroCopyStepLexer(arena, stringInterner);
            const parser = new ZeroCopyStepParser(arena, stringInterner);
            
            const input = 'test;';
            const lexResult = lexer.tokenize(input);
            const parseResult = parser.parse(lexResult.tokens);
            
            if (!parseResult.ast) throw new Error('Should generate AST');
            if (parseResult.statistics.nodesCreated === 0) throw new Error('Should create AST nodes');
            
            return {
                hasAST: !!parseResult.ast,
                nodesCreated: parseResult.statistics.nodesCreated,
                parsingTime: parseResult.statistics.parsingTimeMs
            };
        }));
        
        // Test 2: Parser paths
        suite.tests.push(await this.runTest('Parser Path Management', () => {
            const arena = new MemoryArena(8192);
            const stringInterner = new StringInterner(arena);
            const lexer = new ZeroCopyStepLexer(arena, stringInterner);
            const parser = new ZeroCopyStepParser(arena, stringInterner);
            
            const input = 'a; b;';
            const lexResult = lexer.tokenize(input);
            const parseResult = parser.parse(lexResult.tokens);
            
            if (parseResult.statistics.totalPaths === 0) throw new Error('Should have parser paths');
            
            return {
                totalPaths: parseResult.statistics.totalPaths,
                activePaths: parseResult.statistics.activePaths
            };
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test integrated parsing system
     */
    private async testIntegratedSystem(): Promise<void> {
        const suite: TestSuite = {
            name: 'Integrated System',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: End-to-end parsing
        suite.tests.push(await this.runTest('End-to-End Parsing', () => {
            const system = new ZeroCopyParsingSystem({
                arenaInitialSize: 8192,
                enableOptimizations: true
            });
            
            const sourceCode = `
                function fibonacci(n) {
                    if (n <= 1) return n;
                    return fibonacci(n - 1) + fibonacci(n - 2);
                }
            `;
            
            return system.parseDocument(sourceCode, 'fibonacci.js').then(document => {
                if (!document.ast) throw new Error('Should generate AST');
                if (document.tokens.length === 0) throw new Error('Should generate tokens');
                
                return {
                    hasAST: !!document.ast,
                    tokenCount: document.tokens.length,
                    processingTime: document.statistics.totalProcessingTime
                };
            });
        }));
        
        // Test 2: Multiple document parsing
        suite.tests.push(await this.runTest('Multiple Document Parsing', () => {
            const system = new ZeroCopyParsingSystem();
            
            const documents = [
                { text: 'var x = 1;', file: 'test1.js' },
                { text: 'var y = 2;', file: 'test2.js' },
                { text: 'var z = 3;', file: 'test3.js' }
            ];
            
            return system.parseDocuments(documents).then(results => {
                if (results.length !== 3) throw new Error('Should parse 3 documents');
                
                for (const result of results) {
                    if (!result.ast) throw new Error('Each document should have AST');
                }
                
                return { documentCount: results.length };
            });
        }));
        
        // Test 3: Serialization round-trip
        suite.tests.push(await this.runTest('Serialization Round-Trip', () => {
            const system = new ZeroCopyParsingSystem();
            
            const sourceCode = 'function test() { return "hello"; }';
            
            return system.parseDocument(sourceCode, 'test.js').then(document => {
                // Serialize
                const serialized = system.serializeDocument(document);
                
                // Deserialize
                const deserialized = system.deserializeDocument(serialized);
                
                if (!deserialized.ast) throw new Error('Deserialized document should have AST');
                if (deserialized.metadata.sourceFile !== 'test.js') {
                    throw new Error('Metadata not preserved');
                }
                
                return {
                    serializedSize: serialized.byteLength,
                    hasDeserializedAST: !!deserialized.ast
                };
            });
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Test performance benchmarks
     */
    private async testPerformanceBenchmarks(): Promise<void> {
        const suite: TestSuite = {
            name: 'Performance Benchmarks',
            tests: [],
            totalTests: 0,
            passedTests: 0,
            totalDuration: 0
        };
        
        // Test 1: Small document benchmark
        suite.tests.push(await this.runTest('Small Document Benchmark', () => {
            const system = new ZeroCopyParsingSystem();
            
            const testCases: BenchmarkTestCase[] = [
                { name: 'simple_expression', input: 'x + y' },
                { name: 'function_call', input: 'foo(a, b, c)' },
                { name: 'assignment', input: 'var x = 42;' }
            ];
            
            return system.runBenchmark(testCases).then(result => {
                if (result.successRate < 1.0) throw new Error('All test cases should succeed');
                if (result.averageProcessingTime > 100) throw new Error('Processing should be fast');
                
                return {
                    successRate: result.successRate,
                    averageTime: result.averageProcessingTime,
                    totalMemory: result.totalMemory
                };
            });
        }));
        
        // Test 2: Memory efficiency test
        suite.tests.push(await this.runTest('Memory Efficiency', () => {
            const system = new ZeroCopyParsingSystem({
                arenaInitialSize: 4096,
                enableOptimizations: true
            });
            
            // Parse multiple documents to test memory reuse
            const documents = Array.from({ length: 10 }, (_, i) => ({
                text: `var variable${i} = ${i};`,
                file: `test${i}.js`
            }));
            
            return system.parseDocuments(documents).then(results => {
                const metrics = system.getPerformanceMetrics();
                
                if (metrics.memoryEfficiency > 0.9) {
                    throw new Error('Memory efficiency seems too high (possible error)');
                }
                
                return {
                    memoryEfficiency: metrics.memoryEfficiency,
                    throughput: metrics.throughput
                };
            });
        }));
        
        this.testSuites.push(this.finalizeTestSuite(suite));
    }
    
    /**
     * Run a single test
     */
    private async runTest(name: string, testFn: () => any): Promise<TestResult> {
        const startTime = Date.now();
        
        try {
            const result = await testFn();
            const duration = Date.now() - startTime;
            
            return {
                name,
                passed: true,
                duration,
                details: result
            };
        } catch (error) {
            const duration = Date.now() - startTime;
            
            return {
                name,
                passed: false,
                duration,
                error: error.message
            };
        }
    }
    
    /**
     * Finalize a test suite
     */
    private finalizeTestSuite(suite: TestSuite): TestSuite {
        suite.totalTests = suite.tests.length;
        suite.passedTests = suite.tests.filter(test => test.passed).length;
        suite.totalDuration = suite.tests.reduce((sum, test) => sum + test.duration, 0);
        
        console.log(`\n📋 ${suite.name}`);
        console.log('-'.repeat(40));
        
        for (const test of suite.tests) {
            const status = test.passed ? '✅' : '❌';
            const duration = `${test.duration}ms`;
            console.log(`${status} ${test.name} (${duration})`);
            
            if (!test.passed && test.error) {
                console.log(`   Error: ${test.error}`);
            }
        }
        
        const passRate = ((suite.passedTests / suite.totalTests) * 100).toFixed(1);
        console.log(`\n📊 Results: ${suite.passedTests}/${suite.totalTests} passed (${passRate}%)`);
        console.log(`⏱️  Total time: ${suite.totalDuration}ms`);
        
        return suite;
    }
    
    /**
     * Print overall test summary
     */
    private printTestSummary(): void {
        console.log('\n' + '='.repeat(60));
        console.log('🎯 ADVANCED MEMORY MANAGEMENT TEST SUMMARY');
        console.log('='.repeat(60));
        
        let totalTests = 0;
        let totalPassed = 0;
        let totalDuration = 0;
        
        for (const suite of this.testSuites) {
            totalTests += suite.totalTests;
            totalPassed += suite.passedTests;
            totalDuration += suite.totalDuration;
            
            const passRate = ((suite.passedTests / suite.totalTests) * 100).toFixed(1);
            const status = suite.passedTests === suite.totalTests ? '✅' : '❌';
            
            console.log(`${status} ${suite.name}: ${suite.passedTests}/${suite.totalTests} (${passRate}%)`);
        }
        
        console.log('-'.repeat(60));
        
        const overallPassRate = ((totalPassed / totalTests) * 100).toFixed(1);
        const overallStatus = totalPassed === totalTests ? '🎉' : '⚠️';
        
        console.log(`${overallStatus} OVERALL: ${totalPassed}/${totalTests} tests passed (${overallPassRate}%)`);
        console.log(`⏱️  Total execution time: ${totalDuration}ms`);
        
        if (totalPassed === totalTests) {
            console.log('\n🚀 All tests passed! Advanced memory management system is working correctly.');
        } else {
            console.log('\n⚠️  Some tests failed. Please review the implementation.');
        }
        
        console.log('='.repeat(60));
    }
}

/**
 * Run the test suite
 */
async function main() {
    const tester = new AdvancedMemoryManagementTester();
    await tester.runAllTests();
}

// Export for use as a module
export { AdvancedMemoryManagementTester };

// Run tests if this file is executed directly
if (require.main === module) {
    main().catch(console.error);
}

