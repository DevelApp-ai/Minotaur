# Grammar File Creation Guide for DSL Developers

This guide explains how to create grammar files for the DevelApp.StepLexer and DevelApp.StepParser system, enabling you to define Domain-Specific Languages (DSLs) with comprehensive parsing and CognitiveGraph integration.

## Overview

The grammar system supports a two-phase parsing approach:
1. **StepLexer**: Tokenization with regex pattern parsing and disambiguation
2. **StepParser**: GLR-style parsing with CognitiveGraph integration for semantic analysis

## Grammar File Structure

### Basic Template

```
Grammar: YourDSLName
TokenSplitter: Space

# Token Rules (Lexical Analysis)
<TOKEN_NAME> ::= pattern
<ANOTHER_TOKEN> ::= pattern

# Production Rules (Syntax Analysis)  
<rule_name> ::= <TOKEN1> <TOKEN2>
             | <alternative_pattern>
```

### Header Section

```
Grammar: MyDSL
TokenSplitter: Space
FormatType: EBNF
Inheritable: true
```

**Header Options:**
- `Grammar: name` - Required. Name of your DSL
- `TokenSplitter: strategy` - Optional. Default: "Space". How to split tokens
- `FormatType: type` - Optional. Grammar format (EBNF, ANTLR, etc.)
- `Inheritable: bool` - Optional. Allow other grammars to inherit from this one

## Token Rules (Lexical Analysis)

Token rules define how to recognize basic lexical elements. They use the pattern `<TOKEN_NAME> ::= pattern`.

### Pattern Types

**1. Regular Expressions (recommended)**
```
<NUMBER> ::= /[0-9]+/
<IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
<STRING> ::= /"[^"]*"/
<WHITESPACE> ::= /[ \t\r\n]+/
```

**2. Literal Strings**
```
<PLUS> ::= '+'
<EQUALS> ::= '='
<IF> ::= 'if'
<WHILE> ::= 'while'
```

**3. Double-Quoted Strings**
```
<KEYWORD_CLASS> ::= "class"
<SEMICOLON> ::= ";"
```

### Token Rule Examples

```
# Programming Language Tokens
<NUMBER> ::= /[0-9]+(\.[0-9]+)?/
<IDENTIFIER> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
<STRING_LITERAL> ::= /"([^"\\]|\\.)*"/
<COMMENT> ::= /\/\/[^\r\n]*/

# Operators
<PLUS> ::= '+'
<MINUS> ::= '-'
<MULTIPLY> ::= '*'
<DIVIDE> ::= '/'
<ASSIGN> ::= '='

# Keywords (higher priority than IDENTIFIER)
<IF> ::= 'if'
<ELSE> ::= 'else'
<WHILE> ::= 'while'
<FUNCTION> ::= 'function'

# Delimiters
<LPAREN> ::= '('
<RPAREN> ::= ')'
<LBRACE> ::= '{'
<RBRACE> ::= '}'

# Whitespace (typically skipped)
<WS> ::= /[ \t\r\n]+/ => { skip }
```

## Production Rules (Syntax Analysis)

Production rules define the grammar structure. They reference token rules and other production rules.

### Basic Production Rules

```
<program> ::= <statement_list>

<statement_list> ::= <statement>
                  | <statement_list> <statement>

<statement> ::= <assignment>
             | <if_statement>  
             | <while_statement>

<assignment> ::= <IDENTIFIER> <ASSIGN> <expression>

<expression> ::= <term>
              | <expression> <PLUS> <term>
              | <expression> <MINUS> <term>

<term> ::= <factor>
        | <term> <MULTIPLY> <factor>
        | <term> <DIVIDE> <factor>

<factor> ::= <NUMBER>
          | <IDENTIFIER>
          | <LPAREN> <expression> <RPAREN>
```

### Multi-line Production Rules

For complex rules, you can use continuation lines with `|`:

```
<if_statement> ::= <IF> <LPAREN> <expression> <RPAREN> <statement>
                | <IF> <LPAREN> <expression> <RPAREN> <statement> <ELSE> <statement>

<function_call> ::= <IDENTIFIER> <LPAREN> <RPAREN>
                 | <IDENTIFIER> <LPAREN> <argument_list> <RPAREN>
```

## Context-Sensitive Rules

The system supports context-sensitive parsing for advanced scenarios:

```
# Different rules in different contexts
<string_content[string]> ::= /[^"]*/
<string_start> ::= '"' => { enter_context: string }
<string_end[string]> ::= '"' => { exit_context }
```

## Precedence and Associativity

Control operator precedence and associativity:

```
# Precedence declarations (higher numbers = higher precedence)
%precedence <MULTIPLY> 10
%precedence <DIVIDE> 10  
%precedence <PLUS> 5
%precedence <MINUS> 5

# Associativity
%left <PLUS> <MINUS>
%left <MULTIPLY> <DIVIDE>
%right <ASSIGN>
```

## Complete Example: Simple Calculator DSL

```
Grammar: Calculator
TokenSplitter: Space
FormatType: EBNF

# Token Rules
<NUMBER> ::= /[0-9]+(\.[0-9]+)?/
<IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
<PLUS> ::= '+'
<MINUS> ::= '-'
<MULTIPLY> ::= '*'
<DIVIDE> ::= '/'
<LPAREN> ::= '('
<RPAREN> ::= ')'
<ASSIGN> ::= '='
<NEWLINE> ::= /\r?\n/
<WS> ::= /[ \t]+/ => { skip }

# Production Rules
<program> ::= <statement_list>

<statement_list> ::= <statement>
                  | <statement_list> <NEWLINE> <statement>

<statement> ::= <assignment>
             | <expression>

<assignment> ::= <IDENTIFIER> <ASSIGN> <expression>

<expression> ::= <term>
              | <expression> <PLUS> <term>
              | <expression> <MINUS> <term>

<term> ::= <factor>
        | <term> <MULTIPLY> <factor>
        | <term> <DIVIDE> <factor>

<factor> ::= <NUMBER>
          | <IDENTIFIER>
          | <LPAREN> <expression> <RPAREN>

# Precedence (optional)
%precedence <MULTIPLY> <DIVIDE> 10
%precedence <PLUS> <MINUS> 5
%left <PLUS> <MINUS> <MULTIPLY> <DIVIDE>
%right <ASSIGN>
```

## Advanced Features

### Semantic Actions

Add semantic actions to production rules:

```
<assignment> ::= <IDENTIFIER> <ASSIGN> <expression> => {
    symbol_table.declare($1.value, $3.type);
    cognitive_graph.add_assignment_node($1, $3);
}
```

### Error Recovery

Define error recovery rules:

```
<statement> ::= <assignment>
             | <expression>
             | error <NEWLINE> => { report_error("Invalid statement"); }
```

### Inheritance

Create reusable grammar components:

```
Grammar: BaseLanguage
Inheritable: true

<IDENTIFIER> ::= /[a-zA-Z][a-zA-Z0-9]*/
<NUMBER> ::= /[0-9]+/
```

```
Grammar: ExtendedLanguage
Inherits: BaseLanguage

<FLOAT> ::= /[0-9]+\.[0-9]+/
<STRING> ::= /"[^"]*"/
```

## Testing Your Grammar

Use the test framework to validate your grammar:

```csharp
[Fact]
public void MyDSL_Should_ParseCorrectly()
{
    var grammar = @"
Grammar: TestDSL
<NUMBER> ::= /[0-9]+/
<PLUS> ::= '+'
<expr> ::= <NUMBER> | <expr> <PLUS> <expr>
";

    var engine = new StepParserEngine();
    engine.LoadGrammarFromContent(grammar);
    
    Assert.NotNull(engine.CurrentGrammar);
    Assert.True(engine.CurrentGrammar.TokenRules.Count >= 2);
    Assert.True(engine.CurrentGrammar.ProductionRules.Count >= 1);
}
```

## Integration with CognitiveGraph

The parser automatically integrates with CognitiveGraph for semantic analysis:

```csharp
// Parse source code into CognitiveGraph
var result = stepParser.Parse(sourceCode);
var cognitiveGraph = result.CognitiveGraph;

// Query the semantic structure
var assignmentNodes = cognitiveGraph.Query("assignment");
var variableDeclarations = cognitiveGraph.Query("variable_declaration");
```

## Best Practices

1. **Token Priority**: Define keywords before general identifiers
2. **Whitespace Handling**: Use `=> { skip }` for whitespace tokens
3. **Left Recursion**: Use left-recursive rules for operators (`<expr> ::= <expr> <OP> <term>`)
4. **Error Handling**: Include error recovery rules for robustness
5. **Testing**: Write comprehensive tests for your grammar rules
6. **Documentation**: Comment complex patterns and rules

## Common Patterns

### Expression Parsing with Precedence
```
<expression> ::= <logical_or>
<logical_or> ::= <logical_and> | <logical_or> <OR> <logical_and>
<logical_and> ::= <equality> | <logical_and> <AND> <equality>
<equality> ::= <comparison> | <equality> <EQ> <comparison>
<comparison> ::= <addition> | <comparison> <LT> <addition>
<addition> ::= <multiplication> | <addition> <PLUS> <multiplication>
<multiplication> ::= <unary> | <multiplication> <MULT> <unary>
<unary> ::= <primary> | <MINUS> <unary> | <NOT> <unary>
<primary> ::= <NUMBER> | <IDENTIFIER> | <LPAREN> <expression> <RPAREN>
```

### Statement Lists
```
<statement_list> ::= <statement>
                  | <statement_list> <statement>

<block> ::= <LBRACE> <statement_list> <RBRACE>
         | <LBRACE> <RBRACE>
```

### Optional Elements
```
<function_declaration> ::= <FUNCTION> <IDENTIFIER> <LPAREN> <parameter_list> <RPAREN> <block>
                        | <FUNCTION> <IDENTIFIER> <LPAREN> <RPAREN> <block>

<parameter_list> ::= <parameter>
                  | <parameter_list> <COMMA> <parameter>
```

This grammar system provides a powerful foundation for creating sophisticated DSLs with full semantic analysis capabilities through CognitiveGraph integration.