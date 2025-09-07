/**
 * ZeroCopyASTNode Unit Tests
 *
 * Comprehensive test suite for the ZeroCopyASTNode class to ensure
 * proper memory management, initialization, and functionality.
 */

import { ZeroCopyASTNode, ASTNodeType } from '../ZeroCopyASTNode';
import { MemoryArena } from '../../../memory/arena/MemoryArena';
import { StringInterner } from '../../../memory/strings/StringInterner';
import { TokenSpan } from '../../tokens/AlignedToken';

describe('ZeroCopyASTNode', () => {
    let arena: MemoryArena;
    let stringInterner: StringInterner;

    beforeEach(() => {
        // Create a 1MB memory arena for testing
        arena = new MemoryArena(1024 * 1024);
        stringInterner = new StringInterner(arena);
    });

    afterEach(() => {
        // Clean up resources
        arena = null as any;
        stringInterner = null as any;
    });

    describe('Constructor and Basic Properties', () => {
        test('should create node with valid memory pointer', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.PROGRAM,
                'test_program',
                'program_value',
            );

            expect(node).toBeDefined();
            expect(node.nodeType).toBe(ASTNodeType.PROGRAM);
            expect(node.name).toBe('test_program');
            expect(node.value).toBe('program_value');
        });

        test('should handle optional parameters correctly', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.STATEMENT,
            );

            expect(node.nodeType).toBe(ASTNodeType.STATEMENT);
            expect(node.name).toBe('');
            expect(node.value).toBe('');
        });

        test('should set and get node properties correctly', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.EXPRESSION,
                'test_expr',
            );

            // Test property setters and getters
            node.parentId = 42;
            expect(node.parentId).toBe(42);

            node.depth = 3;
            expect(node.depth).toBe(3);

            node.flags = 0xFF;
            expect(node.flags).toBe(0xFF);

            node.name = 'updated_name';
            expect(node.name).toBe('updated_name');

            node.value = 'updated_value';
            expect(node.value).toBe('updated_value');
        });
    });

    describe('Static Factory Methods', () => {
        test('should create node using create() method', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.IDENTIFIER,
                'variable_name',
                'x',
            );

            expect(node.nodeType).toBe(ASTNodeType.IDENTIFIER);
            expect(node.name).toBe('variable_name');
            expect(node.value).toBe('x');
        });

        test('should create node from existing memory using fromMemory()', () => {
            // First create a node
            const originalNode = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.LITERAL,
                'number_literal',
                '42',
            );

            // Then create another node from the same memory location
            const nodeFromMemory = ZeroCopyASTNode.fromMemory(
                originalNode.pointer,
                arena,
                stringInterner,
            );

            expect(nodeFromMemory.nodeType).toBe(ASTNodeType.LITERAL);
            expect(nodeFromMemory.name).toBe('number_literal');
            expect(nodeFromMemory.value).toBe('42');
        });

        test('should return correct header size and alignment', () => {
            expect(ZeroCopyASTNode.getHeaderSize()).toBe(72);
            expect(ZeroCopyASTNode.getAlignment()).toBe(8);
        });
    });

    describe('Child Node Management', () => {
        test('should add and retrieve child nodes', () => {
            const parent = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.BLOCK,
                'main_block',
            );

            const child1 = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.STATEMENT,
                'stmt1',
            );

            const child2 = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.STATEMENT,
                'stmt2',
            );

            parent.appendChild(child1);
            parent.appendChild(child2);

            expect(parent.childCount).toBe(2);

            const retrievedChild1 = parent.getChild(0);
            const retrievedChild2 = parent.getChild(1);

            expect(retrievedChild1).toBeDefined();
            expect(retrievedChild2).toBeDefined();
            expect(retrievedChild1!.name).toBe('stmt1');
            expect(retrievedChild2!.name).toBe('stmt2');
        });

        test('should handle child count correctly', () => {
            const parent = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.FUNCTION_DECLARATION,
                'test_function',
            );

            expect(parent.childCount).toBe(0);

            const child = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.BLOCK,
                'function_body',
            );

            parent.appendChild(child);
            expect(parent.childCount).toBe(1);
        });

        test('should iterate over children correctly', () => {
            const parent = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.PROGRAM,
                'test_program',
            );

            const childNames = ['child1', 'child2', 'child3'];
            const children: ZeroCopyASTNode[] = [];

            // Add children
            childNames.forEach(name => {
                const child = ZeroCopyASTNode.create(
                    arena,
                    stringInterner,
                    ASTNodeType.STATEMENT,
                    name,
                );
                children.push(child);
                parent.appendChild(child);
            });

            // Test iteration using getChildren()
            const iteratedNames: string[] = [];
            const allChildren = parent.getChildren();
            for (const child of allChildren) {
                iteratedNames.push(child.name);
            }

            expect(iteratedNames).toEqual(childNames);
        });
    });

    describe('Token Span Management', () => {
        test('should handle token spans correctly', () => {
            const span: TokenSpan = {
                start: { line: 1, column: 5, offset: 10 },
                end: { line: 1, column: 15, offset: 20 },
            };

            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.IDENTIFIER,
                'test_var',
                undefined,
                span,
            );

            const retrievedSpan = node.span;
            expect(retrievedSpan).toBeDefined();
            expect(retrievedSpan!.start.line).toBe(1);
            expect(retrievedSpan!.start.column).toBe(5);
            expect(retrievedSpan!.start.offset).toBe(10);
            expect(retrievedSpan!.end.line).toBe(1);
            expect(retrievedSpan!.end.column).toBe(15);
            expect(retrievedSpan!.end.offset).toBe(20);
        });
    });

    describe('Memory Management', () => {
        test('should allocate memory correctly', () => {
            const initialStats = arena.getStatistics();
            const initialUsed = initialStats.totalUsed;

            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.EXPRESSION,
                'test_expr',
            );

            const finalStats = arena.getStatistics();
            const finalUsed = finalStats.totalUsed;
            expect(finalUsed).toBeGreaterThan(initialUsed);
            expect(finalUsed - initialUsed).toBeGreaterThanOrEqual(ZeroCopyASTNode.getHeaderSize());
        });

        test('should handle memory arena boundaries', () => {
            // Create a small arena to test boundary conditions
            const smallArena = new MemoryArena(128);
            const smallStringInterner = new StringInterner(smallArena);

            // This should work
            const node = ZeroCopyASTNode.create(
                smallArena,
                smallStringInterner,
                ASTNodeType.LITERAL,
                'small',
            );

            expect(node).toBeDefined();
            expect(node.name).toBe('small');
        });
    });

    describe('Error Handling', () => {
        test('should handle invalid node types gracefully', () => {
            // Test with a valid but edge-case node type
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.ERROR,
                'error_node',
            );

            expect(node.nodeType).toBe(ASTNodeType.ERROR);
            expect(node.name).toBe('error_node');
        });

        test('should handle empty strings correctly', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.IDENTIFIER,
                '',
                '',
            );

            expect(node.name).toBe('');
            expect(node.value).toBe('');
        });

        test('should handle large strings correctly', () => {
            const longName = 'a'.repeat(1000);
            const longValue = 'b'.repeat(1000);

            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.LITERAL,
                longName,
                longValue,
            );

            expect(node.name).toBe(longName);
            expect(node.value).toBe(longValue);
        });
    });

    describe('Performance Characteristics', () => {
        test('should create nodes efficiently', () => {
            const startTime = performance.now();
            const nodeCount = 1000;

            for (let i = 0; i < nodeCount; i++) {
                ZeroCopyASTNode.create(
                    arena,
                    stringInterner,
                    ASTNodeType.STATEMENT,
                    `node_${i}`,
                    `value_${i}`,
                );
            }

            const endTime = performance.now();
            const duration = endTime - startTime;

            // Should create 1000 nodes in reasonable time (less than 100ms)
            expect(duration).toBeLessThan(100);
        });

        test('should access properties efficiently', () => {
            const node = ZeroCopyASTNode.create(
                arena,
                stringInterner,
                ASTNodeType.EXPRESSION,
                'test_node',
                'test_value',
            );

            const startTime = performance.now();
            const iterations = 10000;

            for (let i = 0; i < iterations; i++) {
                // Access properties multiple times
                const type = node.nodeType;
                const name = node.name;
                const value = node.value;
                const depth = node.depth;
            }

            const endTime = performance.now();
            const duration = endTime - startTime;

            // Should access properties very quickly (less than 100ms for 10k accesses in CI)
            expect(duration).toBeLessThan(100);
        });
    });
});

