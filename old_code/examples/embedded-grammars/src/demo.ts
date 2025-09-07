#!/usr/bin/env node

/**
 * Minotaur Embedded Grammar Parser Generation Demo
 * 
 * This demonstration script showcases the Minotaur compiler-compiler system's
 * ability to generate parsers for multiple target languages from embedded grammar
 * compositions (HTML with CSS and JavaScript).
 * 
 * Features demonstrated:
 * - Multi-target parser generation (9 languages)
 * - Embedded grammar composition
 * - Context-sensitive parsing
 * - Cross-language validation
 * - Performance optimization
 * - Build system integration
 */

import { ParserGenerator } from './ParserGenerator';
import { readFileSync, existsSync } from 'fs';
import { join } from 'path';

class EmbeddedGrammarDemo {
    private generator: ParserGenerator;
    private startTime: number;
    
    constructor() {
        this.generator = new ParserGenerator('./generated-parsers');
        this.startTime = 0;
    }
    
    /**
     * Run the complete demonstration
     */
    async runDemo(): Promise<void> {
        console.log('🚀 Minotaur Embedded Grammar Parser Generation Demo');
        console.log('=' .repeat(60));
        console.log();
        
        this.startTime = Date.now();
        
        try {
            // Display system information
            await this.displaySystemInfo();
            
            // Show grammar composition details
            await this.showGrammarComposition();
            
            // Show HTML examples
            await this.showHTMLExamples();
            
            // Generate parsers for all languages
            const results = await this.generator.generateAllParsers();
            
            // Display results
            await this.displayResults(results);
            
            // Show performance comparison
            await this.showPerformanceComparison(results);
            
            // Show usage examples
            await this.showUsageExamples(results);
            
            console.log('\n🎉 Demo completed successfully!');
            console.log(`⏱️  Total time: ${Date.now() - this.startTime}ms`);
            
        } catch (error) {
            console.error('\n❌ Demo failed:', error);
            process.exit(1);
        }
    }
    
    /**
     * Display system information
     */
    private async displaySystemInfo(): Promise<void> {
        console.log('📋 System Information');
        console.log('-'.repeat(30));
        console.log(`Node.js Version: ${process.version}`);
        console.log(`Platform: ${process.platform}`);
        console.log(`Architecture: ${process.arch}`);
        console.log(`Memory: ${Math.round(process.memoryUsage().heapUsed / 1024 / 1024)}MB`);
        console.log(`Working Directory: ${process.cwd()}`);
        console.log();
    }
    
    /**
     * Show grammar composition details
     */
    private async showGrammarComposition(): Promise<void> {
        console.log('📚 Grammar Composition');
        console.log('-'.repeat(30));
        
        const grammars = [
            { name: 'HTML Base', file: './grammars/html_base.gf' },
            { name: 'JavaScript', file: './grammars/javascript.gf' },
            { name: 'CSS', file: './grammars/css.gf' },
            { name: 'HTML Embedded', file: './grammars/html_embedded.gf' }
        ];
        
        for (const grammar of grammars) {
            const exists = existsSync(grammar.file);
            const size = exists ? readFileSync(grammar.file, 'utf8').length : 0;
            
            console.log(`${exists ? '✅' : '❌'} ${grammar.name}: ${grammar.file}`);
            if (exists) {
                console.log(`   📏 Size: ${(size / 1024).toFixed(2)}KB`);
                console.log(`   📊 Lines: ${readFileSync(grammar.file, 'utf8').split('\\n').length}`);
            }
        }
        
        console.log();
        console.log('🔗 Composition Features:');
        console.log('   • Context-sensitive parsing');
        console.log('   • Symbol table sharing');
        console.log('   • Cross-language validation');
        console.log('   • Optimized context transitions');
        console.log();
    }
    
    /**
     * Show HTML examples
     */
    private async showHTMLExamples(): Promise<void> {
        console.log('📄 HTML Examples');
        console.log('-'.repeat(30));
        
        const examples = [
            { name: 'Simple Example', file: './html-examples/simple-example.html' },
            { name: 'Complex Example', file: './html-examples/complex-example.html' }
        ];
        
        for (const example of examples) {
            if (existsSync(example.file)) {
                const content = readFileSync(example.file, 'utf8');
                const lines = content.split('\\n').length;
                const size = content.length;
                
                // Count embedded languages
                const cssMatches = content.match(/<style[^>]*>([\\s\\S]*?)<\\/style>/gi) || [];
                const jsMatches = content.match(/<script[^>]*>([\\s\\S]*?)<\\/script>/gi) || [];
                
                console.log(`✅ ${example.name}: ${example.file}`);
                console.log(`   📏 Size: ${(size / 1024).toFixed(2)}KB`);
                console.log(`   📊 Lines: ${lines}`);
                console.log(`   🎨 CSS Blocks: ${cssMatches.length}`);
                console.log(`   ⚡ JavaScript Blocks: ${jsMatches.length}`);
                
                // Analyze complexity
                const htmlElements = (content.match(/<[^/][^>]*>/g) || []).length;
                const cssRules = cssMatches.reduce((count, block) => {
                    return count + (block.match(/[^{}]+\\{[^}]*\\}/g) || []).length;
                }, 0);
                const jsFunctions = jsMatches.reduce((count, block) => {
                    return count + (block.match(/function\\s+\\w+|\\w+\\s*=>|\\w+\\s*:\\s*function/g) || []).length;
                }, 0);
                
                console.log(`   🏗️  HTML Elements: ${htmlElements}`);
                console.log(`   🎯 CSS Rules: ${cssRules}`);
                console.log(`   🔧 JS Functions: ${jsFunctions}`);
            } else {
                console.log(`❌ ${example.name}: ${example.file} (not found)`);
            }
        }
        console.log();
    }
    
    /**
     * Display generation results
     */
    private async displayResults(results: Map<string, any>): Promise<void> {
        console.log('📊 Generation Results');
        console.log('-'.repeat(30));
        
        const successful = Array.from(results.values()).filter(r => r.success);
        const failed = Array.from(results.values()).filter(r => !r.success);
        
        console.log(`✅ Successful: ${successful.length}/${results.size}`);
        console.log(`❌ Failed: ${failed.length}/${results.size}`);
        console.log();
        
        // Show successful generations
        if (successful.length > 0) {
            console.log('🎯 Successful Generations:');
            for (const result of successful) {
                console.log(`   ${result.language.toUpperCase().padEnd(12)} | ${result.metrics.generationTime.toString().padStart(6)}ms | ${result.generatedFiles.length.toString().padStart(2)} files | ${(result.metrics.codeSize / 1024).toFixed(1).padStart(6)}KB`);
            }
            console.log();
        }
        
        // Show failed generations
        if (failed.length > 0) {
            console.log('❌ Failed Generations:');
            for (const result of failed) {
                console.log(`   ${result.language.toUpperCase()}: ${result.error}`);
            }
            console.log();
        }
    }
    
    /**
     * Show performance comparison
     */
    private async showPerformanceComparison(results: Map<string, any>): Promise<void> {
        console.log('⚡ Performance Comparison');
        console.log('-'.repeat(30));
        
        const successful = Array.from(results.values()).filter(r => r.success);
        
        if (successful.length === 0) {
            console.log('No successful generations to compare.');
            return;
        }
        
        // Sort by generation time
        const sortedByTime = [...successful].sort((a, b) => a.metrics.generationTime - b.metrics.generationTime);
        
        console.log('🏃 Generation Speed Ranking:');
        sortedByTime.forEach((result, index) => {
            const rank = (index + 1).toString().padStart(2);
            const language = result.language.toUpperCase().padEnd(12);
            const time = result.metrics.generationTime.toString().padStart(6);
            const speedRatio = (result.metrics.generationTime / sortedByTime[0].metrics.generationTime).toFixed(2);
            
            console.log(`   ${rank}. ${language} | ${time}ms | ${speedRatio}x`);
        });
        console.log();
        
        // Sort by code size
        const sortedBySize = [...successful].sort((a, b) => a.metrics.codeSize - b.metrics.codeSize);
        
        console.log('📏 Generated Code Size Ranking:');
        sortedBySize.forEach((result, index) => {
            const rank = (index + 1).toString().padStart(2);
            const language = result.language.toUpperCase().padEnd(12);
            const size = (result.metrics.codeSize / 1024).toFixed(1).padStart(6);
            const sizeRatio = (result.metrics.codeSize / sortedBySize[0].metrics.codeSize).toFixed(2);
            
            console.log(`   ${rank}. ${language} | ${size}KB | ${sizeRatio}x`);
        });
        console.log();
        
        // Calculate averages
        const avgTime = successful.reduce((sum, r) => sum + r.metrics.generationTime, 0) / successful.length;
        const avgSize = successful.reduce((sum, r) => sum + r.metrics.codeSize, 0) / successful.length;
        const totalTime = successful.reduce((sum, r) => sum + r.metrics.generationTime, 0);
        const totalSize = successful.reduce((sum, r) => sum + r.metrics.codeSize, 0);
        
        console.log('📈 Summary Statistics:');
        console.log(`   Average Generation Time: ${avgTime.toFixed(2)}ms`);
        console.log(`   Average Code Size: ${(avgSize / 1024).toFixed(2)}KB`);
        console.log(`   Total Generation Time: ${totalTime}ms`);
        console.log(`   Total Generated Code: ${(totalSize / 1024).toFixed(2)}KB`);
        console.log();
    }
    
    /**
     * Show usage examples
     */
    private async showUsageExamples(results: Map<string, any>): Promise<void> {
        console.log('💡 Usage Examples');
        console.log('-'.repeat(30));
        
        const successful = Array.from(results.values()).filter(r => r.success);
        
        if (successful.length === 0) {
            console.log('No successful generations to show examples for.');
            return;
        }
        
        // Show examples for a few key languages
        const exampleLanguages = ['c', 'javascript', 'python', 'rust'];
        
        for (const language of exampleLanguages) {
            const result = results.get(language);
            if (result && result.success) {
                console.log(`🔧 ${language.toUpperCase()} Usage:`);
                console.log(`   cd ${result.outputPath}`);
                
                switch (language) {
                    case 'c':
                        console.log(`   make`);
                        console.log(`   ./html_parser tests/simple-test.html`);
                        break;
                    case 'javascript':
                        console.log(`   npm install`);
                        console.log(`   node example.js`);
                        break;
                    case 'python':
                        console.log(`   pip install -e .`);
                        console.log(`   python example.py`);
                        break;
                    case 'rust':
                        console.log(`   cargo build --release`);
                        console.log(`   cargo run --example parse_html`);
                        break;
                }
                console.log();
            }
        }
        
        console.log('📚 Documentation:');
        console.log('   • Generated parsers include comprehensive documentation');
        console.log('   • Build configurations are provided for each language');
        console.log('   • Test suites demonstrate parsing capabilities');
        console.log('   • Example usage shows integration patterns');
        console.log();
        
        console.log('🎯 Key Features Demonstrated:');
        console.log('   • Multi-target parser generation (9 languages)');
        console.log('   • Embedded grammar composition (HTML + CSS + JavaScript)');
        console.log('   • Context-sensitive parsing with symbol tables');
        console.log('   • Cross-language validation and optimization');
        console.log('   • Language-specific performance optimizations');
        console.log('   • Production-ready build system integration');
        console.log();
    }
}

// Main execution
async function main() {
    const demo = new EmbeddedGrammarDemo();
    await demo.runDemo();
}

// Run demo if this file is executed directly
if (require.main === module) {
    main().catch(error => {
        console.error('Demo failed:', error);
        process.exit(1);
    });
}

export { EmbeddedGrammarDemo };

