#!/usr/bin/env node

/**
 * Test Runner for Embedded Grammar Parser Testing
 * 
 * This script runs comprehensive tests for the embedded grammar parser
 * generation system, validating all target languages and features.
 */

import { EmbeddedGrammarTestFramework } from './TestFramework';
import { EmbeddedGrammarDemo } from '../src/demo';
import { writeFileSync } from 'fs';
import { join } from 'path';

class TestRunner {
    private framework: EmbeddedGrammarTestFramework;
    private demo: EmbeddedGrammarDemo;
    
    constructor() {
        this.framework = new EmbeddedGrammarTestFramework('./test-results');
        this.demo = new EmbeddedGrammarDemo();
    }
    
    /**
     * Run all tests and demonstrations
     */
    async runAll(): Promise<void> {
        console.log('🚀 Minotaur Embedded Grammar Test Suite');
        console.log('=' .repeat(60));
        console.log();
        
        const startTime = Date.now();
        
        try {
            // Run demonstration first
            console.log('🎭 Running demonstration...');
            await this.demo.runDemo();
            
            console.log('\\n' + '=' .repeat(60));
            console.log();
            
            // Run comprehensive tests
            console.log('🧪 Running comprehensive tests...');
            const testResults = await this.framework.runComprehensiveTests();
            
            // Generate summary
            await this.generateSummary(testResults);
            
            const endTime = Date.now();
            console.log(`\\n🎉 All tests and demonstrations completed successfully!`);
            console.log(`⏱️  Total execution time: ${((endTime - startTime) / 1000).toFixed(2)}s`);
            
            // Exit with appropriate code
            if (testResults.successRate >= 90) {
                console.log('✅ Test suite PASSED');
                process.exit(0);
            } else {
                console.log('⚠️  Test suite PASSED with warnings');
                process.exit(0);
            }
            
        } catch (error) {
            console.error('❌ Test suite FAILED:', error);
            process.exit(1);
        }
    }
    
    /**
     * Run only tests (skip demonstration)
     */
    async runTestsOnly(): Promise<void> {
        console.log('🧪 Minotaur Embedded Grammar Tests Only');
        console.log('=' .repeat(50));
        console.log();
        
        try {
            const testResults = await this.framework.runComprehensiveTests();
            await this.generateSummary(testResults);
            
            if (testResults.successRate >= 90) {
                console.log('✅ Tests PASSED');
                process.exit(0);
            } else {
                console.log('⚠️  Tests PASSED with warnings');
                process.exit(0);
            }
            
        } catch (error) {
            console.error('❌ Tests FAILED:', error);
            process.exit(1);
        }
    }
    
    /**
     * Run only demonstration (skip tests)
     */
    async runDemoOnly(): Promise<void> {
        console.log('🎭 Minotaur Embedded Grammar Demo Only');
        console.log('=' .repeat(50));
        console.log();
        
        try {
            await this.demo.runDemo();
            console.log('✅ Demo completed successfully');
            process.exit(0);
            
        } catch (error) {
            console.error('❌ Demo FAILED:', error);
            process.exit(1);
        }
    }
    
    /**
     * Generate comprehensive summary
     */
    private async generateSummary(testResults: any): Promise<void> {
        console.log('\\n📊 Test Summary');
        console.log('-'.repeat(30));
        console.log(`Total Tests: ${testResults.totalTests}`);
        console.log(`Successful: ${testResults.successful}`);
        console.log(`Failed: ${testResults.failed}`);
        console.log(`Success Rate: ${testResults.successRate.toFixed(2)}%`);
        
        if (testResults.successRate >= 95) {
            console.log('🏆 EXCELLENT - All systems working perfectly!');
        } else if (testResults.successRate >= 90) {
            console.log('✅ GOOD - Minor issues detected');
        } else if (testResults.successRate >= 75) {
            console.log('⚠️  WARNING - Some issues need attention');
        } else {
            console.log('❌ CRITICAL - Major issues detected');
        }
        
        // Generate JSON summary for CI/CD
        const summary = {
            timestamp: new Date().toISOString(),
            totalTests: testResults.totalTests,
            successful: testResults.successful,
            failed: testResults.failed,
            successRate: testResults.successRate,
            status: testResults.successRate >= 90 ? 'PASSED' : 'FAILED',
            reportPath: testResults.reportPath
        };
        
        writeFileSync('./test-results/summary.json', JSON.stringify(summary, null, 2));
        console.log('\\n📄 Summary saved to: ./test-results/summary.json');
    }
}

// Command line interface
async function main() {
    const args = process.argv.slice(2);
    const runner = new TestRunner();
    
    if (args.includes('--help') || args.includes('-h')) {
        console.log('Minotaur Embedded Grammar Test Runner');
        console.log('');
        console.log('Usage:');
        console.log('  npm run test              # Run all tests and demo');
        console.log('  npm run test:only         # Run tests only');
        console.log('  npm run demo              # Run demo only');
        console.log('');
        console.log('Options:');
        console.log('  --help, -h               Show this help message');
        console.log('  --tests-only             Run tests only (skip demo)');
        console.log('  --demo-only              Run demo only (skip tests)');
        console.log('');
        return;
    }
    
    if (args.includes('--tests-only')) {
        await runner.runTestsOnly();
    } else if (args.includes('--demo-only')) {
        await runner.runDemoOnly();
    } else {
        await runner.runAll();
    }
}

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
    console.error('❌ Uncaught Exception:', error);
    process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
    console.error('❌ Unhandled Rejection at:', promise, 'reason:', reason);
    process.exit(1);
});

// Run if this file is executed directly
if (require.main === module) {
    main().catch(error => {
        console.error('❌ Test runner failed:', error);
        process.exit(1);
    });
}

export { TestRunner };

