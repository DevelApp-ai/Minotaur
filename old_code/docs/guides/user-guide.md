# DSL Designer - User Guide

## Introduction

DSL Designer is a comprehensive tool for creating, testing, and deploying domain-specific languages. This application allows you to define custom languages using an enhanced Backus-Naur format with regex tokenization, visualize grammar structures, and debug parsing processes at a character-by-character level.

## Getting Started

### Installation

#### Web Version
- Access the web version directly at [dsl-designer.example.com](https://dsl-designer.example.com)
- No installation required, works in any modern browser

#### Desktop Version
- Download the appropriate installer for your platform:
  - Windows: `DSL-Designer-Setup-1.0.0.exe`
  - macOS: `DSL-Designer-1.0.0.dmg`
  - Linux: `dsl-designer-1.0.0.AppImage` or `dsl-designer_1.0.0_amd64.deb`
- Run the installer and follow the on-screen instructions

### Main Interface

The application is divided into five main sections, accessible via tabs:

1. **Editor** - Text-based editors for grammar and source code
2. **Visual Editor** - Blockly-based visual grammar editor
3. **Callbacks** - Define and manage rule activation callbacks
4. **Visualization** - View parse trees and grammar graphs
5. **Debugging** - Debug parsing at character level

## Creating a Grammar

### Text-Based Grammar Editor

1. Navigate to the **Editor** tab
2. Select the "Grammar Editor" sub-tab
3. Define your grammar using the enhanced BNF syntax:

```
Grammar: MyGrammar
TokenSplitter: Space

<expression> ::= <term> | <expression> "+" <term> | <expression> "-" <term>
<term> ::= <factor> | <term> "*" <factor> | <term> "/" <factor>
<factor> ::= <number> | "(" <expression> ")"
<number> ::= /[0-9]+/
```

### Visual Grammar Editor

1. Navigate to the **Visual Editor** tab
2. Drag and drop grammar elements from the toolbox
3. Connect elements to define your grammar structure
4. The corresponding grammar code will be generated automatically

### Grammar Syntax

- `Grammar: Name` - Defines the grammar name
- `TokenSplitter: Type` - Defines how input is split into tokens (None, Space, or Regex)
- `<rule> ::= expression` - Defines a grammar rule
- `"literal"` - Defines a literal terminal
- `/regex/` - Defines a regex-based terminal
- `<non-terminal>` - References another rule
- `|` - Defines alternatives
- `<rule(context)>` - Defines a context-sensitive rule
- `expression => {callback}` - Attaches a callback to a rule

## Testing Your Grammar

1. Navigate to the **Editor** tab
2. Select the "Source Code Editor" sub-tab
3. Enter source code to test against your grammar
4. Click "Parse" to analyze the source code

## Visualizing Results

1. Navigate to the **Visualization** tab
2. View the parse tree generated from your source code
3. Switch to "Grammar Graph" to visualize your grammar structure

## Debugging

1. Navigate to the **Debugging** tab
2. Click "Start" to begin debugging
3. Use the controls to step through parsing character by character
4. View the lexer and parser states at each step

## Using Callbacks

1. Navigate to the **Callbacks** tab
2. Define custom callbacks for your grammar rules
3. Use the syntax `<rule> ::= expression => {callbackName}` in your grammar
4. Test your callbacks by parsing source code with the "Parse with Callbacks" button

## Desktop Features

When using the desktop version, additional features are available:

1. Click "Show Desktop Tools" to access file operations
2. Use "Open Grammar" and "Save Grammar" to work with grammar files
3. Use "Open Source" and "Save Source" to work with source code files
4. Use "Export Parser" to generate a standalone parser from your grammar

## Example Workflows

### Creating a Simple Calculator Language

1. Define a grammar for arithmetic expressions
2. Test with expressions like `42 + 10 * (5 - 3)`
3. Visualize the parse tree to verify operator precedence
4. Add callbacks to evaluate expressions

### Creating a Domain-Specific Language

1. Define grammar rules for your domain concepts
2. Add context-sensitive rules for special cases
3. Test with sample code from your domain
4. Add callbacks to perform domain-specific actions
5. Export a parser for integration into other applications

## Troubleshooting

- If parsing fails, check your grammar for ambiguities
- Use the debugging tools to identify where parsing fails
- Ensure your grammar and source code match in terms of token splitting
- Check callback syntax and implementation if callbacks aren't executing

## Additional Resources

- [Grammar Syntax Reference](https://example.com/grammar-syntax)
- [Callback API Documentation](https://example.com/callback-api)
- [Example Grammars Repository](https://example.com/example-grammars)

## Support

For issues, questions, or feature requests, please contact:
- Email: support@dsl-designer.example.com
- GitHub: https://github.com/example/dsl-designer/issues
