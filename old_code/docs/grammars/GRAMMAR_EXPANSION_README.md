# Minotaur Grammar Expansion

This document describes the comprehensive grammar expansion implemented in Minotaur, including new ANTLR v4 grammars, Bison/Flex grammars, enhanced postal code support, and performance evaluation framework.

## Overview

The grammar expansion adds:
- **5 ANTLR v4 grammars** from the official grammars-v4 repository
- **3 Bison/Flex grammar pairs** for performance comparison
- **Enhanced postal code support** with Danish format and international framework
- **100+ example files** across multiple categories
- **Performance evaluation framework** for ANTLR vs Bison/Flex comparison
- **Comprehensive documentation** and testing tools

## New Grammar Library Structure

```
grammar/
├── antlr4/                    # ANTLR v4 grammars
│   ├── JSON.g4               # JSON parser grammar
│   ├── CSV.g4                # CSV parser grammar
│   └── arithmetic.g4         # Arithmetic expression grammar
├── bison-flex/               # Bison/Flex grammars
│   ├── calculator.y/.l       # Calculator parser/lexer
│   ├── expression.y/.l       # Expression parser/lexer
│   ├── config.y/.l           # Configuration file parser/lexer
│   └── Makefile              # Build system for grammars
├── PostalCoding_Enhanced.grammar  # Enhanced postal grammar
└── [existing base grammars]
```

## Enhanced Examples Library

```
examples/
├── postal/                   # Postal address examples
│   ├── danish_addresses.txt  # Danish postal format examples
│   ├── us_addresses.txt      # Enhanced US address examples
│   └── international_addresses.txt  # Global address formats
├── programming/              # Programming language examples
│   ├── java_samples/         # Java code examples
│   ├── c_samples/            # C language examples
│   └── arithmetic/           # Arithmetic expression examples
├── data_formats/             # Data format examples
│   ├── json/                 # JSON examples (simple to complex)
│   ├── csv/                  # CSV examples with edge cases
│   └── sql/                  # SQL query examples
└── performance/              # Performance testing data
    ├── small_files/          # < 1KB test files
    ├── medium_files/         # 1KB - 100KB test files
    ├── large_files/          # 100KB - 1MB test files
    └── stress_test/          # > 1MB test files
```

## Enhanced Extensions

```
extensions/
├── PostalTokens_Enhanced.extension     # Enhanced postal patterns
├── InternationalPostal.extension       # Global postal code patterns
├── [existing extensions]
```

## Performance Evaluation Framework

```
src/benchmarking/
├── PerformanceBenchmark.ts   # Main benchmarking framework
├── BenchmarkSuites.ts        # Test suite definitions (includes grammar expansion)
└── [other benchmarking files]

tests/performance/
└── results/                 # Benchmark results and reports
    └── [generated reports]   # TypeScript-generated benchmark data
```

## ANTLR v4 Grammars

### JSON Grammar (`grammar/antlr4/JSON.g4`)
- **Source**: Official ANTLR grammars-v4 repository
- **License**: BSD (compatible)
- **Purpose**: Parse JSON data structures
- **Complexity**: Medium
- **Features**: Complete JSON specification support

### CSV Grammar (`grammar/antlr4/CSV.g4`)
- **Source**: Official ANTLR grammars-v4 repository
- **License**: BSD (compatible)
- **Purpose**: Parse CSV files with various formats
- **Complexity**: Low-Medium
- **Features**: Quoted fields, escape sequences, custom delimiters

### Arithmetic Grammar (`grammar/antlr4/arithmetic.g4`)
- **Source**: Official ANTLR grammars-v4 repository
- **License**: Public Domain
- **Purpose**: Parse mathematical expressions
- **Complexity**: Low
- **Features**: Basic arithmetic operations, parentheses, precedence

## Bison/Flex Grammars

### Calculator Grammar (`grammar/bison-flex/calculator.y/.l`)
- **Purpose**: Classic calculator with operator precedence
- **Features**: Basic arithmetic, unary minus, exponentiation
- **Complexity**: Low-Medium
- **Build**: `make calculator` in bison-flex directory

### Expression Grammar (`grammar/bison-flex/expression.y/.l`)
- **Purpose**: Infix expression evaluator with error handling
- **Features**: Integer/double support, division by zero checking
- **Complexity**: Medium
- **Build**: `make expression` in bison-flex directory

### Configuration Grammar (`grammar/bison-flex/config.y/.l`)
- **Purpose**: INI-style configuration file parser
- **Features**: Sections, key-value pairs, comments
- **Complexity**: Low-Medium
- **Build**: `make config` in bison-flex directory

## Enhanced Postal Code Support

### Danish Postal Code Format
- **4-digit format**: DDDD (e.g., 2100, 5000)
- **3-digit special codes**: DDD (e.g., 999 for business mail)
- **International format**: DK-DDDD (e.g., DK-2100)
- **Address structure**: Street Name + Number, Apartment, Postal Code + City

### International Framework
- **Extensible country support**: US, DK, DE, FR, UK, CA, AU
- **Flexible postal patterns**: Numeric, alphanumeric, mixed formats
- **Validation rules**: Length constraints, format validation
- **Address formats**: European, North American, Asian styles

### Enhanced Grammar Features
```cebnf
<postal-address> ::= <us-address> | <danish-address> | <international-address>

<danish-address> ::= <name-part> <danish-street-address> <danish-zip-part>
<danish-zip-part> ::= <opt-whitespace><danish-zip-code> <whitespace> <danish-city-name> <EOL>
<danish-zip-code> ::= <danish-4digit> | <danish-3digit> | "DK-"<danish-4digit>
```

## Performance Evaluation

### Benchmarking Framework
The grammar expansion grammars are integrated into Minotaur's comprehensive TypeScript benchmarking framework, providing sophisticated performance comparison between Minotaur's compiler-compiler and traditional parser generators:

#### Metrics Measured
- **Parse Time**: Microseconds per input size
- **Compile Time**: Code generation performance
- **Memory Usage**: Peak memory consumption during parsing
- **Generated Code Quality**: Lines of code, file count, executable size
- **Runtime Performance**: Parse speed, memory efficiency, throughput, latency
- **Context-Sensitive Features**: Symbol lookups, inheritance resolutions
- **Success Rate**: Percentage of successful parses

#### Grammar Expansion Test Suites
- **ANTLR v4 Grammar Expansion Suite**: Performance testing of JSON, CSV, and arithmetic grammars
- **Bison/Flex Comparison Suite**: Direct comparison with traditional parser generators

#### Running Performance Tests
```typescript
// Import the benchmark framework
import { PerformanceBenchmark } from './src/benchmarking/PerformanceBenchmark';
import { BenchmarkSuites } from './src/benchmarking/BenchmarkSuites';

// Run grammar expansion benchmarks
const benchmark = new PerformanceBenchmark(compilerExport, contextEngine, inheritanceResolver);
const config = BenchmarkSuites.getGrammarExpansionBenchmark();
const results = await benchmark.runBenchmark(config);
```

#### Generated Reports
- **TypeScript Interfaces**: Structured benchmark data with comprehensive metrics
- **Performance Comparisons**: Direct comparison with ANTLR v4 and Bison/Flex
- **Quality Scoring**: Automated quality assessment of generated parsers
- **Comprehensive Analytics**: Memory profiling, CPU usage, and optimization insights

## Usage Examples

### Testing ANTLR Grammars
```bash
# Test JSON grammar with example data
antlr4 grammar/antlr4/JSON.g4
javac *.java
echo '{"name": "test", "value": 123}' | java JSONParser

# Test arithmetic grammar
echo "2 + 3 * 4" | java ArithmeticParser
```

### Testing Bison/Flex Grammars
```bash
cd grammar/bison-flex

# Build all grammars
make all

# Test calculator
echo "2 + 3 * 4" | ./calculator

# Test expression parser
echo "10 + 5 * 2" | ./expression

# Test configuration parser
echo -e "[database]\nhost = localhost\nport = 3306" | ./config
```

### Testing Postal Code Grammar
```bash
# Test with Danish address
echo "Lars Nielsen
Vestergade 15, 2. th
2100 København Ø" | minotaur-test PostalCoding_Enhanced.grammar

# Test with international format
echo "John Smith
123 Main Street
US-12345 Springfield" | minotaur-test PostalCoding_Enhanced.grammar
```

## Development Guidelines

### Adding New Grammars
1. **Choose appropriate directory**: `antlr4/` for ANTLR, `bison-flex/` for Bison/Flex
2. **Add attribution**: Update `ATTRIBUTIONS.md` with source and license
3. **Create examples**: Add test cases in `examples/` directory
4. **Update documentation**: Add grammar description to this README
5. **Add performance tests**: Include in benchmark suite if applicable

### Creating Examples
1. **Follow naming convention**: Use descriptive, lowercase names with underscores
2. **Include variety**: Simple, complex, and edge case examples
3. **Add comments**: Explain complex examples with inline comments
4. **Test validity**: Ensure examples parse correctly with their grammars

### Performance Testing
1. **Add test cases**: Include new grammars in `benchmark_runner.py`
2. **Provide test data**: Create representative input samples
3. **Document results**: Update performance documentation with findings
4. **Compare fairly**: Use equivalent functionality between ANTLR and Bison/Flex

## License Compliance

All included grammars comply with the project's licensing requirements:

- **ANTLR v4 grammars**: BSD License (compatible)
- **Bison/Flex examples**: MIT/Public Domain (compatible)
- **Danish postal specifications**: Public information (no restrictions)
- **Custom implementations**: Project license applies

See `ATTRIBUTIONS.md` for detailed attribution information.

## Future Expansion

### Planned Additions
- **More programming languages**: Python, JavaScript, Go, Rust
- **Additional data formats**: XML, YAML, TOML, Protocol Buffers
- **More international postal formats**: European, Asian, global coverage
- **Advanced performance metrics**: Compilation time, code size analysis
- **Grammar analysis tools**: Complexity metrics, ambiguity detection

### Contributing
1. **Follow existing patterns**: Use established directory structure and naming
2. **Ensure license compatibility**: Only add permissively licensed content
3. **Provide comprehensive examples**: Include variety of test cases
4. **Update documentation**: Keep README and attribution files current
5. **Test thoroughly**: Verify grammar correctness and performance impact

## Troubleshooting

### Common Issues
1. **Build failures**: Ensure Bison/Flex are installed (`sudo apt install bison flex`)
2. **Python dependencies**: Install required packages (`pip3 install matplotlib numpy`)
3. **Permission errors**: Make scripts executable (`chmod +x scripts/performance/run_tests.sh`)
4. **Memory issues**: Reduce test data size for large file testing

### Getting Help
- **Documentation**: Check grammar-specific README files
- **Examples**: Review example files for usage patterns
- **Performance**: Run benchmark suite to identify issues
- **Community**: Contribute improvements and report issues

---

*Last updated: July 31, 2025*
*Version: 1.0.0*

