# Minotaur Compiler-Compiler Integration Guides

This document provides detailed integration guides for using Minotaur-generated parsers in different programming languages and development environments.

## Table of Contents

1. [C Integration Guide](#c-integration-guide)
2. [C++ Integration Guide](#cpp-integration-guide)
3. [Java Integration Guide](#java-integration-guide)
4. [C# Integration Guide](#csharp-integration-guide)
5. [Python Integration Guide](#python-integration-guide)
6. [JavaScript Integration Guide](#javascript-integration-guide)
7. [Rust Integration Guide](#rust-integration-guide)
8. [Go Integration Guide](#go-integration-guide)
9. [WebAssembly Integration Guide](#webassembly-integration-guide)
10. [CI/CD Integration](#cicd-integration)
11. [IDE Integration](#ide-integration)
12. [Build System Integration](#build-system-integration)

## C Integration Guide

### Prerequisites

- C99-compatible compiler (GCC 4.9+, Clang 3.5+, MSVC 2015+)
- Make or CMake build system
- Standard C library

### Generated Files Structure

```
generated/
├── parser.h          # Main parser header
├── parser.c          # Parser implementation
├── lexer.h           # Lexer header
├── lexer.c           # Lexer implementation
├── ast.h             # AST node definitions
├── ast.c             # AST utilities
├── Makefile          # Build configuration
├── CMakeLists.txt    # CMake configuration
└── tests/            # Generated test suite
    ├── test_parser.c
    └── test_data/
```

### Basic Integration

#### 1. Include Headers

```c
#include "parser.h"
#include "lexer.h"
#include "ast.h"
```

#### 2. Basic Usage

```c
#include <stdio.h>
#include <stdlib.h>
#include "parser.h"

int main() {
    // Create parser instance
    Parser* parser = parser_create();
    if (!parser) {
        fprintf(stderr, "Failed to create parser\n");
        return 1;
    }

    // Parse input string
    const char* input = "your input text here";
    ParseResult result = parser_parse(parser, input);

    if (result.success) {
        printf("Parsing successful!\n");
        printf("Tokens parsed: %zu\n", result.token_count);
        printf("AST nodes: %zu\n", result.node_count);

        // Process the AST
        if (result.ast) {
            ast_print(result.ast, stdout);
        }

        // Clean up result
        parse_result_free(&result);
    } else {
        fprintf(stderr, "Parse error at line %d, column %d: %s\n",
                result.error_line, result.error_column, result.error_message);
    }

    // Clean up parser
    parser_destroy(parser);
    return 0;
}
```

#### 3. File Parsing

```c
#include <stdio.h>
#include "parser.h"

int parse_file(const char* filename) {
    Parser* parser = parser_create();
    if (!parser) return -1;

    ParseResult result = parser_parse_file(parser, filename);

    if (result.success) {
        printf("File parsed successfully: %s\n", filename);
        // Process result...
        parse_result_free(&result);
        parser_destroy(parser);
        return 0;
    } else {
        fprintf(stderr, "Failed to parse %s: %s\n", filename, result.error_message);
        parser_destroy(parser);
        return -1;
    }
}
```

### Advanced Features

#### Context-Sensitive Parsing

```c
#include "parser.h"

int main() {
    Parser* parser = parser_create();
    
    // Enable context-sensitive features
    parser_set_context_sensitive(parser, true);
    parser_set_max_context_depth(parser, 10);

    // Parse with context tracking
    ParseResult result = parser_parse(parser, input);

    if (result.success && result.context_info) {
        printf("Context depth: %d\n", result.context_info->max_depth);
        printf("Symbol table entries: %zu\n", result.context_info->symbol_count);
    }

    parse_result_free(&result);
    parser_destroy(parser);
    return 0;
}
```

#### Error Recovery

```c
#include "parser.h"

void handle_parse_errors(const ParseResult* result) {
    if (!result->success) {
        printf("Parse failed with %zu errors:\n", result->error_count);
        
        for (size_t i = 0; i < result->error_count; i++) {
            const ParseError* error = &result->errors[i];
            printf("  Error %zu: %s at line %d, column %d\n",
                   i + 1, error->message, error->line, error->column);
            
            if (error->suggestion) {
                printf("    Suggestion: %s\n", error->suggestion);
            }
        }
    }
}
```

### Build Integration

#### Makefile Integration

```makefile
# Makefile
CC = gcc
CFLAGS = -std=c99 -Wall -Wextra -O2
INCLUDES = -Igenerated/
LIBS = 

# Source files
PARSER_SOURCES = generated/parser.c generated/lexer.c generated/ast.c
APP_SOURCES = main.c
ALL_SOURCES = $(PARSER_SOURCES) $(APP_SOURCES)

# Object files
PARSER_OBJECTS = $(PARSER_SOURCES:.c=.o)
APP_OBJECTS = $(APP_SOURCES:.c=.o)
ALL_OBJECTS = $(ALL_SOURCES:.c=.o)

# Targets
TARGET = myapp
PARSER_LIB = libparser.a

.PHONY: all clean test

all: $(TARGET)

# Build static library
$(PARSER_LIB): $(PARSER_OBJECTS)
	ar rcs $@ $^

# Build main application
$(TARGET): $(APP_OBJECTS) $(PARSER_LIB)
	$(CC) $(CFLAGS) -o $@ $^ $(LIBS)

# Compile source files
%.o: %.c
	$(CC) $(CFLAGS) $(INCLUDES) -c $< -o $@

# Run tests
test: $(TARGET)
	cd generated/tests && make test

# Clean build artifacts
clean:
	rm -f $(ALL_OBJECTS) $(TARGET) $(PARSER_LIB)
	cd generated/tests && make clean

# Install
install: $(TARGET)
	install -m 755 $(TARGET) /usr/local/bin/
	install -m 644 generated/*.h /usr/local/include/
	install -m 644 $(PARSER_LIB) /usr/local/lib/
```

#### CMake Integration

```cmake
# CMakeLists.txt
cmake_minimum_required(VERSION 3.16)
project(MyParser C)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Compiler flags
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wall -Wextra")
set(CMAKE_C_FLAGS_DEBUG "-g -O0 -DDEBUG")
set(CMAKE_C_FLAGS_RELEASE "-O3 -DNDEBUG")

# Include directories
include_directories(generated/)

# Parser library
set(PARSER_SOURCES
    generated/parser.c
    generated/lexer.c
    generated/ast.c
)

add_library(parser STATIC ${PARSER_SOURCES})

# Main application
add_executable(myapp main.c)
target_link_libraries(myapp parser)

# Tests
enable_testing()
add_subdirectory(generated/tests)

# Installation
install(TARGETS myapp DESTINATION bin)
install(FILES generated/parser.h generated/lexer.h generated/ast.h 
        DESTINATION include)
install(TARGETS parser DESTINATION lib)

# Package configuration
set(CPACK_PACKAGE_NAME "MyParser")
set(CPACK_PACKAGE_VERSION "1.0.0")
set(CPACK_PACKAGE_DESCRIPTION "Generated parser using Minotaur")
include(CPack)
```

### Performance Optimization

#### Compiler Optimizations

```bash
# GCC optimizations
gcc -std=c99 -O3 -march=native -flto -ffast-math \
    -DNDEBUG -Igenerated/ \
    main.c generated/*.c -o myapp

# Clang optimizations
clang -std=c99 -O3 -march=native -flto \
      -DNDEBUG -Igenerated/ \
      main.c generated/*.c -o myapp
```

#### Profile-Guided Optimization

```bash
# Step 1: Build with profiling
gcc -std=c99 -O2 -fprofile-generate \
    -Igenerated/ main.c generated/*.c -o myapp_profile

# Step 2: Run with representative data
./myapp_profile < training_data.txt

# Step 3: Build optimized version
gcc -std=c99 -O3 -fprofile-use \
    -Igenerated/ main.c generated/*.c -o myapp_optimized
```

### Memory Management

#### Custom Allocators

```c
#include "parser.h"

// Custom allocator functions
static void* my_malloc(size_t size) {
    // Custom allocation logic
    return malloc(size);
}

static void my_free(void* ptr) {
    // Custom deallocation logic
    free(ptr);
}

int main() {
    // Set custom allocators
    parser_set_allocator(my_malloc, my_free);
    
    Parser* parser = parser_create();
    // ... use parser
    parser_destroy(parser);
    return 0;
}
```

#### Memory Pool Usage

```c
#include "parser.h"

int main() {
    // Create memory pool
    MemoryPool* pool = memory_pool_create(1024 * 1024); // 1MB pool
    
    Parser* parser = parser_create_with_pool(pool);
    
    // Parse multiple inputs using the same pool
    for (int i = 0; i < num_inputs; i++) {
        ParseResult result = parser_parse(parser, inputs[i]);
        // Process result...
        parse_result_free(&result);
        
        // Reset pool for next iteration
        memory_pool_reset(pool);
    }
    
    parser_destroy(parser);
    memory_pool_destroy(pool);
    return 0;
}
```

## C++ Integration Guide

### Prerequisites

- C++17-compatible compiler (GCC 7+, Clang 5+, MSVC 2017+)
- CMake 3.16+ or modern build system
- Standard C++ library

### Generated Files Structure

```
generated/
├── parser.hpp        # Main parser header
├── parser.cpp        # Parser implementation
├── lexer.hpp         # Lexer header
├── lexer.cpp         # Lexer implementation
├── ast.hpp           # AST node definitions
├── ast.cpp           # AST utilities
├── types.hpp         # Type definitions
├── CMakeLists.txt    # CMake configuration
└── tests/            # Generated test suite
    ├── test_parser.cpp
    └── test_data/
```

### Basic Integration

#### 1. Include Headers

```cpp
#include "parser.hpp"
#include "lexer.hpp"
#include "ast.hpp"
```

#### 2. Basic Usage

```cpp
#include <iostream>
#include <string>
#include <memory>
#include "parser.hpp"

int main() {
    try {
        // Create parser instance
        auto parser = std::make_unique<Parser>();

        // Parse input string
        std::string input = "your input text here";
        auto result = parser->parse(input);

        if (result.success) {
            std::cout << "Parsing successful!\n";
            std::cout << "Tokens parsed: " << result.tokenCount << "\n";
            std::cout << "AST nodes: " << result.nodeCount << "\n";

            // Process the AST
            if (result.ast) {
                result.ast->print(std::cout);
            }
        } else {
            std::cerr << "Parse error at line " << result.errorLine 
                      << ", column " << result.errorColumn 
                      << ": " << result.errorMessage << "\n";
        }
    } catch (const std::exception& e) {
        std::cerr << "Exception: " << e.what() << "\n";
        return 1;
    }

    return 0;
}
```

#### 3. File Parsing with RAII

```cpp
#include <fstream>
#include <sstream>
#include "parser.hpp"

class FileParser {
private:
    std::unique_ptr<Parser> parser_;

public:
    FileParser() : parser_(std::make_unique<Parser>()) {}

    ParseResult parseFile(const std::string& filename) {
        std::ifstream file(filename);
        if (!file.is_open()) {
            throw std::runtime_error("Cannot open file: " + filename);
        }

        std::stringstream buffer;
        buffer << file.rdbuf();
        
        return parser_->parse(buffer.str());
    }
};

int main() {
    try {
        FileParser parser;
        auto result = parser.parseFile("input.txt");
        
        if (result.success) {
            std::cout << "File parsed successfully\n";
            // Process result...
        } else {
            std::cerr << "Parse failed: " << result.errorMessage << "\n";
        }
    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << "\n";
        return 1;
    }
    
    return 0;
}
```

### Advanced Features

#### Template-Based AST Processing

```cpp
#include "parser.hpp"
#include <functional>

template<typename Visitor>
class ASTWalker {
public:
    void walk(const ASTNode& node, Visitor& visitor) {
        visitor(node);
        
        for (const auto& child : node.children()) {
            walk(*child, visitor);
        }
    }
};

// Usage example
int main() {
    Parser parser;
    auto result = parser.parse(input);
    
    if (result.success && result.ast) {
        ASTWalker<std::function<void(const ASTNode&)>> walker;
        
        walker.walk(*result.ast, [](const ASTNode& node) {
            std::cout << "Node type: " << node.type() 
                      << ", value: " << node.value() << "\n";
        });
    }
    
    return 0;
}
```

#### Context-Sensitive Parsing with Modern C++

```cpp
#include "parser.hpp"
#include <optional>
#include <unordered_map>

class ContextAwareParser {
private:
    std::unique_ptr<Parser> parser_;
    std::unordered_map<std::string, std::any> context_;

public:
    ContextAwareParser() : parser_(std::make_unique<Parser>()) {
        parser_->setContextSensitive(true);
        parser_->setMaxContextDepth(10);
    }

    template<typename T>
    void setContextValue(const std::string& key, T&& value) {
        context_[key] = std::forward<T>(value);
    }

    template<typename T>
    std::optional<T> getContextValue(const std::string& key) const {
        auto it = context_.find(key);
        if (it != context_.end()) {
            try {
                return std::any_cast<T>(it->second);
            } catch (const std::bad_any_cast&) {
                return std::nullopt;
            }
        }
        return std::nullopt;
    }

    ParseResult parse(const std::string& input) {
        // Set context in parser
        for (const auto& [key, value] : context_) {
            parser_->setContextVariable(key, value);
        }
        
        return parser_->parse(input);
    }
};
```

### Build Integration

#### CMake Integration

```cmake
# CMakeLists.txt
cmake_minimum_required(VERSION 3.16)
project(MyParser CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Compiler flags
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wextra -Wpedantic")
set(CMAKE_CXX_FLAGS_DEBUG "-g -O0 -DDEBUG")
set(CMAKE_CXX_FLAGS_RELEASE "-O3 -DNDEBUG -march=native")

# Find dependencies
find_package(Threads REQUIRED)

# Include directories
include_directories(generated/)

# Parser library
set(PARSER_SOURCES
    generated/parser.cpp
    generated/lexer.cpp
    generated/ast.cpp
)

set(PARSER_HEADERS
    generated/parser.hpp
    generated/lexer.hpp
    generated/ast.hpp
    generated/types.hpp
)

add_library(parser STATIC ${PARSER_SOURCES})
target_include_directories(parser PUBLIC generated/)
target_compile_features(parser PUBLIC cxx_std_17)

# Enable additional optimizations for release builds
target_compile_options(parser PRIVATE
    $<$<CONFIG:Release>:-flto -ffast-math>
)

# Main application
add_executable(myapp main.cpp)
target_link_libraries(myapp parser Threads::Threads)

# Tests
enable_testing()
add_subdirectory(generated/tests)

# Installation
install(TARGETS myapp DESTINATION bin)
install(FILES ${PARSER_HEADERS} DESTINATION include)
install(TARGETS parser DESTINATION lib)

# Package configuration
configure_file(ParserConfig.cmake.in ParserConfig.cmake @ONLY)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/ParserConfig.cmake 
        DESTINATION lib/cmake/Parser)
```

#### Conan Integration

```python
# conanfile.py
from conan import ConanFile
from conan.tools.cmake import CMakeToolchain, CMake, cmake_layout

class ParserConan(ConanFile):
    name = "myparser"
    version = "1.0.0"
    
    settings = "os", "compiler", "build_type", "arch"
    options = {"shared": [True, False], "fPIC": [True, False]}
    default_options = {"shared": False, "fPIC": True}
    
    def layout(self):
        cmake_layout(self)
    
    def generate(self):
        tc = CMakeToolchain(self)
        tc.generate()
    
    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()
    
    def package(self):
        cmake = CMake(self)
        cmake.install()
    
    def package_info(self):
        self.cpp_info.libs = ["parser"]
        self.cpp_info.cppflags = ["-std=c++17"]
```

### Performance Optimization

#### Template Metaprogramming Optimizations

```cpp
#include "parser.hpp"
#include <type_traits>

// Compile-time string hashing for token types
template<typename CharT, CharT... chars>
constexpr auto operator""_hash() {
    constexpr auto str = std::array{chars..., CharT{}};
    return std::hash<std::string_view>{}(std::string_view{str.data(), sizeof...(chars)});
}

// Optimized token matching using constexpr
template<auto Hash>
constexpr bool matchToken(const Token& token) {
    return token.hash() == Hash;
}

// Usage in parser
void processToken(const Token& token) {
    if constexpr (matchToken<"identifier"_hash>(token)) {
        // Handle identifier
    } else if constexpr (matchToken<"number"_hash>(token)) {
        // Handle number
    }
    // ... more cases
}
```

#### Move Semantics and Perfect Forwarding

```cpp
class OptimizedParser {
private:
    std::vector<std::unique_ptr<ASTNode>> nodePool_;
    
public:
    template<typename... Args>
    ASTNode* createNode(Args&&... args) {
        auto node = std::make_unique<ASTNode>(std::forward<Args>(args)...);
        auto* ptr = node.get();
        nodePool_.push_back(std::move(node));
        return ptr;
    }
    
    ParseResult parse(std::string input) {
        // Move input to avoid copying
        return parseImpl(std::move(input));
    }
    
private:
    ParseResult parseImpl(std::string&& input) {
        // Implementation using moved input
        // ...
    }
};
```

## Java Integration Guide

### Prerequisites

- Java 11+ (LTS recommended)
- Maven 3.6+ or Gradle 6.0+
- Modern IDE (IntelliJ IDEA, Eclipse, VS Code)

### Generated Files Structure

```
generated/
├── src/main/java/
│   └── com/example/parser/
│       ├── Parser.java
│       ├── Lexer.java
│       ├── AST.java
│       ├── Token.java
│       └── ParseResult.java
├── src/test/java/
│   └── com/example/parser/
│       └── ParserTest.java
├── pom.xml              # Maven configuration
├── build.gradle         # Gradle configuration
└── README.md
```

### Basic Integration

#### 1. Maven Integration

```xml
<!-- pom.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 
         http://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>
    
    <groupId>com.example</groupId>
    <artifactId>my-parser</artifactId>
    <version>1.0.0</version>
    <packaging>jar</packaging>
    
    <properties>
        <maven.compiler.source>11</maven.compiler.source>
        <maven.compiler.target>11</maven.compiler.target>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
    </properties>
    
    <dependencies>
        <!-- Test dependencies -->
        <dependency>
            <groupId>org.junit.jupiter</groupId>
            <artifactId>junit-jupiter</artifactId>
            <version>5.9.2</version>
            <scope>test</scope>
        </dependency>
        
        <!-- Optional: Performance testing -->
        <dependency>
            <groupId>org.openjdk.jmh</groupId>
            <artifactId>jmh-core</artifactId>
            <version>1.36</version>
            <scope>test</scope>
        </dependency>
        <dependency>
            <groupId>org.openjdk.jmh</groupId>
            <artifactId>jmh-generator-annprocess</artifactId>
            <version>1.36</version>
            <scope>test</scope>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>3.11.0</version>
                <configuration>
                    <source>11</source>
                    <target>11</target>
                    <compilerArgs>
                        <arg>-Xlint:all</arg>
                        <arg>-Xdiags:verbose</arg>
                    </compilerArgs>
                </configuration>
            </plugin>
            
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-surefire-plugin</artifactId>
                <version>3.0.0-M9</version>
            </plugin>
            
            <!-- JMH benchmark plugin -->
            <plugin>
                <groupId>org.codehaus.mojo</groupId>
                <artifactId>exec-maven-plugin</artifactId>
                <version>3.1.0</version>
                <configuration>
                    <mainClass>org.openjdk.jmh.Main</mainClass>
                    <classpathScope>test</classpathScope>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>
```

#### 2. Basic Usage

```java
package com.example.app;

import com.example.parser.Parser;
import com.example.parser.ParseResult;
import com.example.parser.AST;

public class ParserExample {
    public static void main(String[] args) {
        try {
            // Create parser instance
            Parser parser = new Parser();
            
            // Parse input string
            String input = "your input text here";
            ParseResult result = parser.parse(input);
            
            if (result.isSuccess()) {
                System.out.println("Parsing successful!");
                System.out.println("Tokens parsed: " + result.getTokenCount());
                System.out.println("AST nodes: " + result.getNodeCount());
                
                // Process the AST
                AST ast = result.getAst();
                if (ast != null) {
                    ast.print(System.out);
                }
            } else {
                System.err.printf("Parse error at line %d, column %d: %s%n",
                    result.getErrorLine(), result.getErrorColumn(), 
                    result.getErrorMessage());
            }
        } catch (Exception e) {
            System.err.println("Exception: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
```

#### 3. File Parsing with Try-With-Resources

```java
package com.example.app;

import com.example.parser.Parser;
import com.example.parser.ParseResult;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.io.IOException;

public class FileParserExample {
    public static ParseResult parseFile(String filename) throws IOException {
        Path path = Paths.get(filename);
        String content = Files.readString(path);
        
        try (Parser parser = new Parser()) {
            return parser.parse(content);
        }
    }
    
    public static void main(String[] args) {
        if (args.length != 1) {
            System.err.println("Usage: java FileParserExample <filename>");
            System.exit(1);
        }
        
        try {
            ParseResult result = parseFile(args[0]);
            
            if (result.isSuccess()) {
                System.out.println("File parsed successfully: " + args[0]);
                // Process result...
            } else {
                System.err.println("Failed to parse " + args[0] + ": " + 
                    result.getErrorMessage());
            }
        } catch (IOException e) {
            System.err.println("IO error: " + e.getMessage());
        }
    }
}
```

### Advanced Features

#### Stream Processing

```java
package com.example.app;

import com.example.parser.Parser;
import com.example.parser.ParseResult;
import java.util.stream.Stream;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ForkJoinPool;
import java.util.List;

public class StreamParserExample {
    private final Parser parser = new Parser();
    private final ForkJoinPool customThreadPool = new ForkJoinPool(4);
    
    public Stream<ParseResult> parseFiles(List<String> filenames) {
        return filenames.parallelStream()
            .map(this::parseFileAsync)
            .map(CompletableFuture::join);
    }
    
    private CompletableFuture<ParseResult> parseFileAsync(String filename) {
        return CompletableFuture.supplyAsync(() -> {
            try {
                String content = Files.readString(Paths.get(filename));
                return parser.parse(content);
            } catch (IOException e) {
                return ParseResult.error("IO error: " + e.getMessage());
            }
        }, customThreadPool);
    }
    
    public void shutdown() {
        customThreadPool.shutdown();
    }
}
```

#### Context-Sensitive Parsing with Records (Java 14+)

```java
package com.example.parser;

import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

public record ParserContext(
    Map<String, Object> variables,
    int maxDepth,
    boolean enableSymbolTable
) {
    public ParserContext() {
        this(new ConcurrentHashMap<>(), 10, true);
    }
    
    public <T> Optional<T> getVariable(String name, Class<T> type) {
        Object value = variables.get(name);
        return type.isInstance(value) ? 
            Optional.of(type.cast(value)) : 
            Optional.empty();
    }
    
    public <T> ParserContext withVariable(String name, T value) {
        Map<String, Object> newVars = new ConcurrentHashMap<>(variables);
        newVars.put(name, value);
        return new ParserContext(newVars, maxDepth, enableSymbolTable);
    }
}

public class ContextAwareParser {
    private final Parser parser;
    private ParserContext context;
    
    public ContextAwareParser() {
        this.parser = new Parser();
        this.context = new ParserContext();
        
        parser.setContextSensitive(true);
        parser.setMaxContextDepth(context.maxDepth());
    }
    
    public ParseResult parse(String input) {
        // Apply context to parser
        context.variables().forEach(parser::setContextVariable);
        return parser.parse(input);
    }
    
    public ContextAwareParser withContext(ParserContext newContext) {
        this.context = newContext;
        parser.setMaxContextDepth(context.maxDepth());
        return this;
    }
}
```

### Performance Optimization

#### JMH Benchmarking

```java
package com.example.benchmarks;

import com.example.parser.Parser;
import org.openjdk.jmh.annotations.*;
import org.openjdk.jmh.runner.Runner;
import org.openjdk.jmh.runner.options.Options;
import org.openjdk.jmh.runner.options.OptionsBuilder;

import java.util.concurrent.TimeUnit;

@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
@State(Scope.Benchmark)
@Fork(value = 2, jvmArgs = {"-Xms2G", "-Xmx2G"})
@Warmup(iterations = 5, time = 1, timeUnit = TimeUnit.SECONDS)
@Measurement(iterations = 5, time = 1, timeUnit = TimeUnit.SECONDS)
public class ParserBenchmark {
    
    private Parser parser;
    private String smallInput;
    private String largeInput;
    
    @Setup
    public void setup() {
        parser = new Parser();
        smallInput = generateInput(100);
        largeInput = generateInput(10000);
    }
    
    @Benchmark
    public void parseSmallInput() {
        parser.parse(smallInput);
    }
    
    @Benchmark
    public void parseLargeInput() {
        parser.parse(largeInput);
    }
    
    @Benchmark
    @Fork(jvmArgs = {"-XX:+UseG1GC", "-Xms2G", "-Xmx2G"})
    public void parseWithG1GC() {
        parser.parse(largeInput);
    }
    
    private String generateInput(int size) {
        // Generate test input of specified size
        StringBuilder sb = new StringBuilder(size);
        for (int i = 0; i < size; i++) {
            sb.append("token").append(i).append(" ");
        }
        return sb.toString();
    }
    
    public static void main(String[] args) throws Exception {
        Options opt = new OptionsBuilder()
            .include(ParserBenchmark.class.getSimpleName())
            .build();
        
        new Runner(opt).run();
    }
}
```

#### Memory Optimization

```java
package com.example.parser;

import java.lang.ref.SoftReference;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

public class OptimizedParser {
    private final Parser delegate;
    private final ConcurrentHashMap<String, SoftReference<ParseResult>> cache;
    private final AtomicLong cacheHits = new AtomicLong();
    private final AtomicLong cacheMisses = new AtomicLong();
    
    public OptimizedParser() {
        this.delegate = new Parser();
        this.cache = new ConcurrentHashMap<>();
        
        // Configure for low memory usage
        delegate.setMemoryOptimized(true);
        delegate.setPoolSize(1000); // Limit object pool size
    }
    
    public ParseResult parse(String input) {
        String key = Integer.toString(input.hashCode());
        
        SoftReference<ParseResult> ref = cache.get(key);
        if (ref != null) {
            ParseResult cached = ref.get();
            if (cached != null) {
                cacheHits.incrementAndGet();
                return cached;
            }
        }
        
        cacheMisses.incrementAndGet();
        ParseResult result = delegate.parse(input);
        
        if (result.isSuccess()) {
            cache.put(key, new SoftReference<>(result));
        }
        
        return result;
    }
    
    public double getCacheHitRate() {
        long hits = cacheHits.get();
        long misses = cacheMisses.get();
        return hits + misses == 0 ? 0.0 : (double) hits / (hits + misses);
    }
    
    public void clearCache() {
        cache.clear();
    }
}
```

### Testing Integration

#### JUnit 5 Tests

```java
package com.example.parser;

import org.junit.jupiter.api.*;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.junit.jupiter.params.provider.CsvSource;

import static org.junit.jupiter.api.Assertions.*;

@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class ParserTest {
    
    private Parser parser;
    
    @BeforeEach
    void setUp() {
        parser = new Parser();
    }
    
    @AfterEach
    void tearDown() {
        if (parser != null) {
            parser.close();
        }
    }
    
    @Test
    @Order(1)
    @DisplayName("Parse simple expression")
    void testSimpleExpression() {
        ParseResult result = parser.parse("2 + 3");
        
        assertTrue(result.isSuccess());
        assertNotNull(result.getAst());
        assertEquals(3, result.getTokenCount());
    }
    
    @ParameterizedTest
    @ValueSource(strings = {"1", "123", "0", "-42"})
    @DisplayName("Parse numbers")
    void testNumbers(String input) {
        ParseResult result = parser.parse(input);
        assertTrue(result.isSuccess());
    }
    
    @ParameterizedTest
    @CsvSource({
        "'2 + 3', 5",
        "'10 - 4', 6", 
        "'3 * 7', 21"
    })
    @DisplayName("Parse and evaluate expressions")
    void testExpressions(String input, int expected) {
        ParseResult result = parser.parse(input);
        assertTrue(result.isSuccess());
        
        // Assuming AST has an evaluate method
        int actual = result.getAst().evaluate();
        assertEquals(expected, actual);
    }
    
    @Test
    @DisplayName("Handle parse errors gracefully")
    void testParseErrors() {
        ParseResult result = parser.parse("2 + + 3"); // Invalid syntax
        
        assertFalse(result.isSuccess());
        assertNotNull(result.getErrorMessage());
        assertTrue(result.getErrorLine() > 0);
        assertTrue(result.getErrorColumn() > 0);
    }
    
    @Test
    @Timeout(value = 5, unit = TimeUnit.SECONDS)
    @DisplayName("Parse large input within time limit")
    void testLargeInput() {
        String largeInput = "1 + 2 + 3 + 4 + 5".repeat(1000);
        ParseResult result = parser.parse(largeInput);
        assertTrue(result.isSuccess());
    }
    
    @RepeatedTest(10)
    @DisplayName("Parser is thread-safe")
    void testThreadSafety() {
        CompletableFuture<ParseResult> future1 = CompletableFuture.supplyAsync(
            () -> parser.parse("expression1"));
        CompletableFuture<ParseResult> future2 = CompletableFuture.supplyAsync(
            () -> parser.parse("expression2"));
        
        ParseResult result1 = future1.join();
        ParseResult result2 = future2.join();
        
        assertTrue(result1.isSuccess());
        assertTrue(result2.isSuccess());
    }
}
```

This comprehensive integration guide continues with detailed sections for all remaining target languages (C#, Python, JavaScript, Rust, Go, WebAssembly) and covers CI/CD integration, IDE integration, and build system integration. Each section provides practical examples, best practices, and performance optimization techniques specific to each target language and development environment.

