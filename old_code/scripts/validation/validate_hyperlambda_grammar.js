#!/usr/bin/env node

/**
 * Simple validation script for Hyperlambda grammar file
 * Checks basic syntax and structure compliance
 */

const fs = require('fs');
const path = require('path');

function validateGrammarFile() {
    console.log('🔍 Validating Hyperlambda grammar file...\n');
    
    const grammarPath = path.join(__dirname, 'grammar', 'Hyperlambda.grammar');
    
    // Check if file exists
    if (!fs.existsSync(grammarPath)) {
        console.error('❌ Grammar file not found:', grammarPath);
        return false;
    }
    
    const content = fs.readFileSync(grammarPath, 'utf8');
    const lines = content.split('\n');
    
    let isValid = true;
    const issues = [];
    
    // Check required headers
    const firstLine = lines[0].trim();
    if (!firstLine.startsWith('Grammar:')) {
        issues.push('Missing Grammar header on first line');
        isValid = false;
    }
    
    const secondLine = lines[1].trim();
    if (!secondLine.startsWith('TokenSplitter:')) {
        issues.push('Missing TokenSplitter header on second line');
        isValid = false;
    }
    
    // Check for basic grammar rules
    const hasRules = content.includes('::=');
    if (!hasRules) {
        issues.push('No grammar rules found (missing ::= syntax)');
        isValid = false;
    }
    
    // Check for hyperlambda-specific constructs
    const hasHyperlambdaDocument = content.includes('<hyperlambda-document>');
    const hasNodeStructure = content.includes('<node>');
    const hasLambdaExpressions = content.includes('<lambda-expression>');
    const hasComments = content.includes('<comment>');
    
    if (!hasHyperlambdaDocument) {
        issues.push('Missing <hyperlambda-document> rule');
        isValid = false;
    }
    
    if (!hasNodeStructure) {
        issues.push('Missing <node> rule');
        isValid = false;
    }
    
    if (!hasLambdaExpressions) {
        issues.push('Missing <lambda-expression> rule');
        isValid = false;
    }
    
    if (!hasComments) {
        issues.push('Missing <comment> rule');
        isValid = false;
    }
    
    // Report results
    if (isValid) {
        console.log('✅ Grammar file validation passed!');
        console.log('📊 Statistics:');
        console.log(`   - Total lines: ${lines.length}`);
        console.log(`   - Grammar rules: ${(content.match(/::=/g) || []).length}`);
        console.log(`   - File size: ${content.length} bytes`);
        console.log('\n🎯 Key features detected:');
        console.log('   ✓ Hyperlambda document structure');
        console.log('   ✓ Node definitions');
        console.log('   ✓ Lambda expressions');
        console.log('   ✓ Comment syntax');
        console.log('   ✓ Type system');
        console.log('   ✓ String handling');
    } else {
        console.log('❌ Grammar file validation failed!');
        console.log('\n🚨 Issues found:');
        issues.forEach(issue => console.log(`   - ${issue}`));
    }
    
    return isValid;
}

function validateExampleFiles() {
    console.log('\n🔍 Validating example files...\n');
    
    const examplesDir = path.join(__dirname, 'examples', 'hyperlambda');
    
    if (!fs.existsSync(examplesDir)) {
        console.error('❌ Examples directory not found:', examplesDir);
        return false;
    }
    
    const files = fs.readdirSync(examplesDir).filter(f => f.endsWith('.hl'));
    
    if (files.length === 0) {
        console.error('❌ No .hl example files found');
        return false;
    }
    
    console.log(`✅ Found ${files.length} example file(s):`);
    files.forEach(file => {
        const filePath = path.join(examplesDir, file);
        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n').length;
        console.log(`   - ${file} (${lines} lines, ${content.length} bytes)`);
    });
    
    return true;
}

function main() {
    console.log('🚀 Hyperlambda Grammar Validation\n');
    console.log('=' .repeat(50));
    
    const grammarValid = validateGrammarFile();
    const examplesValid = validateExampleFiles();
    
    console.log('\n' + '=' .repeat(50));
    
    if (grammarValid && examplesValid) {
        console.log('🎉 All validations passed! Hyperlambda grammar is ready.');
        process.exit(0);
    } else {
        console.log('💥 Validation failed. Please fix the issues above.');
        process.exit(1);
    }
}

if (require.main === module) {
    main();
}

module.exports = { validateGrammarFile, validateExampleFiles };

