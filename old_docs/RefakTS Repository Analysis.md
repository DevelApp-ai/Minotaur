# RefakTS Repository Analysis

## Core Philosophy
RefakTS is a TypeScript refactoring tool built by AI coding agents for AI coding agents. Its key principle is **surgical refactoring operations** that avoid error-prone text generation.

## Key Approach: Clear Path and Small Context

### 1. Location-Based Workflow
- **Find then refactor**: Use `select` command to locate code elements first
- **Precise targeting**: Operations target specific locations like `[src/example.ts 5:8-5:18]`
- **Preview before action**: Shows content preview before performing operations

### 2. Surgical Operations (Not Regeneration)
- **Extract variable**: Convert expressions to variables without touching surrounding code
- **Inline variable**: Replace variable usage with its value
- **Rename**: Change variable names and all references across codebase
- **Find usages**: Locate all symbol references

### 3. Context Management
- **Minimal scope**: Operations affect only what needs to change
- **Preserve cognitive capacity**: AI agents don't need to regenerate entire functions
- **Token efficiency**: Changes only specific elements rather than entire code blocks

### 4. Selection Modes for Context Control
- **Range mode**: Select between start and end patterns
- **Structural mode**: Select based on code structure (methods, fields)
- **Boundary mode**: Limit selection to specific boundaries (functions, classes)
- **Regex mode**: Pattern-based selection

## Benefits for AI Code Generation
1. **Precision**: Exact location targeting prevents unintended changes
2. **Efficiency**: Saves tokens by changing only what's necessary
3. **Reliability**: Avoids regeneration errors and maintains code quality
4. **Composability**: Operations can be chained for complex refactoring
5. **Context preservation**: Maintains surrounding code structure



## Detailed RefakTS Approach Analysis

### Core Problem Addressed
RefakTS addresses the cognitive limitations of AI agents when making code changes:
- **Context Window Limitations**: Effective context gets filled with repetitive code
- **High Cognitive Load**: Tracking multiple simultaneous changes
- **Error-Prone Manual Edits**: Across multiple locations
- **Token Waste**: On unchanged code during regeneration

### Key Technical Implementation
- **AST Manipulation**: Uses ts-morph for reliable TypeScript-aware operations
- **Node Selection**: Uses @phenomnomnominal/tsquery for precise targeting
- **Approval Testing**: Validates refactoring operations against expected outputs
- **Location-Based Workflow**: Find-then-refactor approach with precise coordinates

### Selection Modes for Context Control
1. **Basic Regex**: Pattern-based selection
2. **Range Mode**: Select between start and end patterns
3. **Structural Mode**: Select based on code structure (methods, fields, classes)
4. **Boundary Mode**: Limit selection to specific boundaries (functions, classes)

### Quality Automation Approach
RefakTS demonstrates "Automated Quality Habits" - programmatic quality checks that:
- Detect code quality triggers automatically
- Prompt AI agents for corrective action
- Mimic instinctive responses of experienced developers
- Include checks for: comments, duplication, unused code, feature envy, large changes

### AI-First Development Philosophy
- Built by AI agents, for AI agents
- Roadmap managed collaboratively by Claude instances
- Development workflow optimized for AI agent capabilities
- Comprehensive guidance for AI agents in CLAUDE.md

