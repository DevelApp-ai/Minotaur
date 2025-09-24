# Minotaur Examples

This directory contains various examples and test cases for Minotaur functionality. These examples demonstrate different aspects of the compiler-compiler system and may intentionally contain errors for testing purposes.

## Directory Structure

### Data Formats
- **data_formats/**: Examples of parsing various data formats including JSON, XML, CSV, and other structured data representations

### Embedded Grammars
- **embedded-grammars/**: Examples of grammars embedded within other languages, particularly HTML with JavaScript, CSS, and other scripting languages
- **embedded_test_*.html**: Test files demonstrating embedded grammar parsing in HTML documents

### Programming Languages
- **programming/**: Examples of various programming language constructs and their grammar definitions

### Postal Code Systems
- **postal/**: Examples of postal code validation and parsing systems
- **hyperlambda/**: Hyperlambda language examples and constructs

## Testing and Quality

**Note**: These examples may deliberately contain syntax errors, incomplete code, or malformed grammar definitions for testing error handling and grammar generation capabilities. They are excluded from standard code quality checks.

## Usage

These examples serve multiple purposes:

1. **Demonstration**: Show how Minotaur handles different language constructs
2. **Testing**: Provide test cases for grammar generation and parsing
3. **Documentation**: Illustrate best practices and common patterns
4. **Error Handling**: Test error detection and correction mechanisms

## Grammar Generation Testing

Many examples are designed to work with Minotaur's grammar generation system:

```bash
# Generate grammar from example files
minotaur-grammar generate --language TestLang --output testlang.grammar examples/programming/testlang/*.txt

# Validate generated grammar
minotaur-grammar validate testlang.grammar

# Test with examples that may contain errors
minotaur-grammar test testlang.grammar examples/programming/testlang/error_cases/*.txt
```

## Contributing

When adding new examples:

1. Include a README in subdirectories explaining the examples
2. Mark files that intentionally contain errors with `.error` extension or in `error_cases/` subdirectories
3. Provide both valid and invalid examples where appropriate
4. Document the expected behavior and any special parsing requirements