# Embedded Grammar Implementation

This directory contains the implementation of the enhanced `.grammar` and `.extension` format with embedded language support for Minotaur.

## Overview

The embedded grammar system allows parsing of multi-language documents (like HTML with embedded CSS and JavaScript) using a single, unified grammar format. This eliminates the need for separate `.gf` files and provides sophisticated cross-language validation capabilities.

## Files Structure

### Grammar Files
- **`HTMLEmbedded.grammar`** - Main HTML grammar with embedded language support
- **`CSS.grammar`** - CSS grammar for embedded parsing within HTML
- **`JavaScript.grammar`** - JavaScript grammar for embedded parsing within HTML

### Extension Files
- **`HTMLEmbedded.extension`** - Context-aware token definitions with cross-language validation

### Test Files
- **`embedded_test_simple.html`** - Simple test case demonstrating basic embedded grammar features
- **`embedded_test_complex.html`** - Complex test case with advanced features and modern syntax
- **`embedded_validation_test.html`** - Validation test case specifically for cross-language reference testing

## Key Features Implemented

### 1. Enhanced Grammar Format

The new grammar format includes these extensions:

```
Grammar: CEBNF
TokenSplitter: Space
EmbeddedLanguages: JavaScript, CSS
ContextSensitive: true
SymbolTableSharing: hierarchical
CrossLanguageValidation: true
```

### 2. Context Switching Syntax

```
<style-element> ::= "<style" <attributes>? ">" 
                    @CONTEXT[CSS] <css-stylesheet> @ENDCONTEXT 
                    "</style>"

<script-element> ::= "<script" <attributes>? ">" 
                     @CONTEXT[JavaScript] <js-program> @ENDCONTEXT 
                     "</script>"
```

### 3. Cross-Language References

```
<id-attribute> ::= "id=\"" @SYMBOL[id] <identifier> "\""
<css-id-selector> ::= "#" @REFERENCE[HTML.id] <identifier>
<js-dom-call> ::= "document.getElementById(\"" @REFERENCE[HTML.id] <identifier> "\")"
```

### 4. Context-Aware Extensions

```
@CONTEXT[CSS] {
    CSS_ID_SELECTOR = #[a-zA-Z][a-zA-Z0-9_-]*
    CSS_CLASS_SELECTOR = \.[a-zA-Z][a-zA-Z0-9_-]*
}

@CONTEXT[JavaScript] {
    JS_GET_ELEMENT_BY_ID = document\.getElementById\s*\(\s*["'][^"']*["']\s*\)
}

@VALIDATE {
    css_id_selector_validation: CSS_ID_SELECTOR -> HTML_ID_ATTR
    js_get_element_by_id_validation: JS_GET_ELEMENT_BY_ID -> HTML_ID_ATTR
}
```

## Test Cases

### Simple Test (`embedded_test_simple.html`)

Demonstrates:
- Basic HTML structure with embedded CSS and JavaScript
- CSS ID and class selectors referencing HTML elements
- JavaScript DOM manipulation with `getElementById()`
- HTML `onclick` attributes referencing JavaScript functions
- Form label-input associations with `for`/`id` relationships

**Expected Validations:**
- ✅ CSS `#main-container` → HTML `id="main-container"`
- ✅ CSS `.highlight` → HTML `class="highlight"`
- ✅ JS `getElementById('output')` → HTML `id="output"`
- ✅ HTML `onclick="showAlert()"` → JS `function showAlert()`

### Complex Test (`embedded_test_complex.html`)

Demonstrates:
- Modern ES6+ JavaScript with classes and async/await
- Advanced CSS with custom properties, grid, and animations
- Complex DOM manipulation and event handling
- Form validation with cross-language references
- Performance monitoring and dynamic content generation

**Expected Validations:**
- ✅ Advanced CSS selectors with HTML element references
- ✅ JavaScript class methods referencing DOM elements
- ✅ Event listeners with function references
- ✅ Template literals and modern JavaScript syntax

### Validation Test (`embedded_validation_test.html`)

Specifically tests cross-language validation by including both valid and invalid references:

**Valid References (should pass validation):**
- CSS `#validation-container` → HTML `id="validation-container"`
- JS `getElementById('test-element-1')` → HTML `id="test-element-1"`
- HTML `onclick="validButtonClick()"` → JS `function validButtonClick()`

**Invalid References (should trigger validation errors):**
- CSS `#nonexistent-id` → No matching HTML element
- JS `getElementById('missing-element')` → No matching HTML element
- HTML `onclick="missingFunction()"` → No matching JavaScript function

## Usage Instructions

### 1. Grammar Composition

To use the embedded grammar system:

1. Define your primary grammar (e.g., HTML) with embedded language metadata
2. Create supporting grammars for embedded languages (CSS, JavaScript)
3. Use `@CONTEXT` and `@ENDCONTEXT` directives for context switching
4. Define cross-language references with `@SYMBOL` and `@REFERENCE`

### 2. Extension Configuration

Configure context-aware tokens in the extension file:

1. Use `@CONTEXT[language]` blocks for language-specific tokens
2. Define validation rules in `@VALIDATE` blocks
3. Specify cross-language reference patterns

### 3. Testing and Validation

Use the provided test files to validate your implementation:

1. **Simple Test**: Basic functionality verification
2. **Complex Test**: Advanced features and modern syntax
3. **Validation Test**: Cross-language reference validation

## Expected Compiler-Compiler Changes

The following changes will be needed in the Minotaur compiler-compiler:

### 1. Grammar Parser Extensions
- Parse new header fields (`EmbeddedLanguages`, `ContextSensitive`, etc.)
- Recognize `@CONTEXT`, `@SYMBOL`, `@REFERENCE` directives
- Handle `@IMPORT` and `@COMPOSE` directives

### 2. Context Management
- Implement context stack for language switching
- Manage symbol table hierarchy across languages
- Handle context-sensitive token recognition

### 3. Cross-Language Validation
- Build symbol tables for each language context
- Resolve cross-language references
- Generate validation errors for unresolved references

### 4. Code Generation
- Generate parsers that support context switching
- Implement runtime context management
- Support cross-language symbol resolution

## Benefits

This implementation provides:

1. **Format Unification**: Single grammar format for all use cases
2. **Backward Compatibility**: Existing grammars continue to work
3. **Cross-Language Validation**: Real-time validation of references across languages
4. **Performance**: Single-pass parsing instead of multiple tools
5. **Developer Experience**: Better IDE support and error messages

## Next Steps

1. **Implement Parser Extensions**: Update the grammar parser to recognize new syntax
2. **Add Context Management**: Implement context switching and symbol table hierarchy
3. **Build Validation Engine**: Create cross-language reference validation
4. **Generate Test Parsers**: Create parsers for the test cases to validate functionality
5. **Performance Optimization**: Optimize for embedded language scenarios

## Validation Checklist

When implementing the compiler-compiler changes, verify:

- [ ] All new header fields are parsed correctly
- [ ] Context switching directives work properly
- [ ] Symbol tables are shared hierarchically
- [ ] Cross-language references are validated
- [ ] Error messages include cross-language context
- [ ] Generated parsers handle embedded languages correctly
- [ ] Performance is acceptable for complex documents
- [ ] Backward compatibility is maintained

This implementation demonstrates the full potential of the embedded grammar system and provides a solid foundation for extending Minotaur to handle complex, multi-language parsing scenarios.

