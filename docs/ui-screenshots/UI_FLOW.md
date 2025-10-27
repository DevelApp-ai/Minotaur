# Minotaur UI Flow Documentation

This document provides a comprehensive overview of all implemented UI pages in the Minotaur web application, focusing on the web deployment.

**Generated:** 2025-10-27 03:21:00 UTC

## Overview

Minotaur is a powerful compiler-compiler platform with a comprehensive web-based UI built using Blazor. The application provides an intuitive interface for grammar development, code analysis, and project management.

## Table of Contents

1. [Home](#1-home)
2. [Grammar Editor](#2-grammar-editor)
3. [Marketplace](#3-marketplace)
4. [StepParser Integration](#4-stepparser-integration)
5. [Plugin Manager](#5-plugin-manager)
6. [Project Manager](#6-project-manager)
7. [Symbolic Analyzer](#7-symbolic-analyzer)
8. [Interactive Tutorial](#8-interactive-tutorial)
9. [Version Control](#9-version-control)
10. [Additional Pages](#10-additional-pages)

---

## 1. Home

**URL:** `/`

**Description:** Landing page with overview of Minotaur features and core library integration tests.

**Key Features:**
- Welcome message and application overview
- Core library integration status indicators
- Quick navigation to main features (Grammar Editor, Plugin Manager)
- Test functionality for core components (CognitiveGraphNode, SymbolicAnalysisResult)

**Screenshot:**

![Home Page](https://github.com/user-attachments/assets/3f6c2b54-46c6-460a-9797-269b381752ac)

---

## 2. Grammar Editor

**URL:** `/grammar-editor`

**Description:** Interactive grammar editing and validation interface with syntax highlighting and real-time analysis.

**Key Features:**
- Grammar definition editor with line numbers
- Syntax highlighting support (toggleable)
- Grammar type selection (Lexer, Parser, Combined)
- Version management
- Actions: New, Open, Save, Marketplace integration, Detect Shifts, Parse
- Tabbed interface for Rules, Analysis, and Errors
- Pre-populated with sample grammar for demonstration

**Screenshot:**

![Grammar Editor](https://github.com/user-attachments/assets/e4673e7f-3dda-4760-99ac-ccc14ca2cc49)

---

## 3. Marketplace

**URL:** `/marketplace`

**Description:** Browse and download grammar templates, transpilers, and pipeline templates from the community.

**Key Features:**
- Category tabs: Grammars, Transpilers, Pipeline Templates
- Search functionality for finding specific grammars
- Filter options:
  - Language selection (All Languages, C#, JavaScript, TypeScript, Python, Java, Go, Rust, C++, C, PHP, Ruby, Swift, Kotlin)
  - Feature filters (Shift Detection, Multi-Version Support, Syntax Highlighting)
- Sort options (Relevance, Downloads, Rating, Last Updated, Name)
- Grammar cards showing:
  - Title and rating (with star display)
  - Download count
  - Description
  - Version, Author, Update date, Language
  - Tags for features
  - Install/Details/Remove actions

**Sample Grammars Available:**
1. C# Advanced Grammar (45K downloads, 4.8★)
2. TypeScript Modern Grammar (12K downloads, 4.6★) - Pre-installed
3. Python Scientific Grammar (8K downloads, 4.7★)
4. JavaScript ES2024 Grammar (28K downloads, 4.4★)

**Screenshot:**

![Marketplace](https://github.com/user-attachments/assets/ad55d198-2556-4598-87b6-45a6a4b5f479)

---

## 4. StepParser Integration

**URL:** `/step-parser`

**Description:** Parse source code to cognitive graphs and visualize the parsing process step-by-step.

**Key Features:**
- Source code editor for input
- Parse and Clear action buttons
- Step-by-step parsing visualization panel
- Navigation controls for stepping through parsing process (disabled until parsing is performed)
- Pre-populated with sample grammar code
- Integration with DevelApp.StepParser for parsing capabilities

**Screenshot:**

![StepParser Integration](https://github.com/user-attachments/assets/2b6ece7b-4b99-4e32-974d-c881fde525d9)

---

## 5. Plugin Manager

**URL:** `/plugin-manager`

**Description:** Manage language plugins and extensions for extending Minotaur's capabilities.

**Key Features:**
- Plugin discovery from configurable directories
- Plugin directory management (add/remove paths)
  - Default directories: ./plugins, ~/plugins
- Available plugins list (5 plugins shown):
  1. Grammar Syntax Highlighter (Editor)
  2. ANTLR Parser Integration (Parser)
  3. Advanced Diagram Exporter (Visualization)
  4. LLM Grammar Assistant (AI)
  5. Performance Analyzer (Analysis)
- Plugin details panel (activated on selection)
- Loaded plugins panel with unload functionality
- Refresh capability for re-scanning directories

**Screenshot:**

![Plugin Manager](https://github.com/user-attachments/assets/bf8be746-cf7c-4847-aa4f-a902c1b41c83)

---

## 6. Project Manager

**URL:** `/project-manager`

**Description:** Manage grammar projects and configurations with project browsing and analysis.

**Key Features:**
- Project browser with directory navigation
- Current directory display and navigation
- Project list showing:
  - Sample .NET Project (DotNetProject)
  - Node.js API (NodeProject)
- Project details panel (activated on selection)
- Analysis results panel for detailed project analysis
- Actions: Open Project, New Project, Refresh

**Screenshot:**

![Project Manager](https://github.com/user-attachments/assets/2b6ece7b-4b99-4e32-974d-c881fde525d9)

---

## 7. Symbolic Analyzer

**URL:** `/symbolic-analyzer`

**Description:** Advanced code analysis and verification tools with deep symbolic analysis capabilities.

**Key Features:**
- Source code input area
- Language selection (C#, Java, Python, JavaScript, C++)
- Analysis options:
  - Enable Deep Analysis (checked by default)
  - Cross-Reference Analysis (checked by default)
  - Semantic Analysis (optional)
- Sample templates for quick start:
  - Class Definition
  - Method Implementation
  - Algorithm Example
- Tabbed results interface:
  - Overview
  - Symbol Table
  - Dependencies
  - Cognitive Graph
- Actions: Start Analysis, Clear, Export

**Screenshot:**

![Symbolic Analyzer](https://github.com/user-attachments/assets/2b6ece7b-4b99-4e32-974d-c881fde525d9)

---

## 8. Interactive Tutorial

**URL:** `/tutorial`

**Description:** Step-by-step interactive tutorial for learning Minotaur's features and capabilities.

**Key Features:**
- Progress indicator (Step X of 6)
- Tutorial content area with rich information
- Tutorial steps sidebar:
  1. Welcome to Minotaur
  2. Cognitive Graph Editor
  3. StepParser Integration
  4. Symbolic Analysis
  5. Version Control
  6. Getting Started
- Feature highlights for each major component
- Quick tips panel with helpful hints
- Navigation controls (Previous/Next)
- Keyboard shortcut support (← → arrow keys)

**Screenshot:**

![Interactive Tutorial](https://github.com/user-attachments/assets/2b6ece7b-4b99-4e32-974d-c881fde525d9)

---

## 9. Version Control

**URL:** `/version-control`

**Description:** Grammar version control and history management with Git integration.

**Key Features:**
- Repository status panel showing:
  - Repository name (minotaur-grammars)
  - Current branch (main)
  - Status (Clean)
  - Last commit information
- Branch management:
  - Switch between branches (main, feature/new-parser, hotfix/validation-bug, develop)
  - Create new branch functionality
- Remote repositories management:
  - origin: https://github.com/user/minotaur-grammars.git
  - upstream: https://github.com/DevelApp-ai/minotaur-grammars.git
  - Pull/Push actions for each remote
  - Add new remote capability
- Tabbed interface:
  - Changes (3 pending changes shown)
  - History
  - Branches
  - Collaborate
- Changes panel features:
  - Commit message editor
  - Stage/Unstage controls
  - Modified files list with:
    - Checkbox for selective staging
    - File path and status (Modified/Added)
    - Line change statistics (+/-)
    - Diff and Revert actions
- Actions: Initialize Repo, Clone Repo, Refresh

**Screenshot:**

![Version Control](https://github.com/user-attachments/assets/2b6ece7b-4b99-4e32-974d-c881fde525d9)

---

## 10. Additional Pages

### Counter Demo
**URL:** `/counter`
A simple counter demonstration page (Blazor template page).

### Weather Demo
**URL:** `/weather`
Weather forecast demonstration page (Blazor template page).

### Graph Unparser
**URL:** `/graph-unparser`
Graph unparsing functionality page (referenced in navigation but implementation pending).

### Cognitive Graph Editor
**URL:** `/cognitive-graph-editor`
Visual editor for creating and manipulating cognitive graph structures (referenced in navigation).

---

## Navigation Structure

The application features a persistent side navigation menu with the following structure:

**Main Navigation:**
- Home
- Grammar Editor
- Marketplace
- Interactive Tutorial
- Cognitive Graph Editor
- StepParser Integration
- Symbolic Analyzer
- Version Control
- Plugin Manager
- Project Manager
- Graph Unparser
- Counter
- Weather

**Authenticated User Features:**
The navigation menu also includes authentication-aware features:
- Sign In button (when not authenticated)
- User menu dropdown (when authenticated) with:
  - Subscription badge (FREE/PRO/ENTERPRISE)
  - Profile
  - My Collections
  - Downloads
  - Settings
  - Sign Out

---

## UI Design Characteristics

### Color Scheme
- **Primary Navigation:** Dark navy blue sidebar (#2c3e50 approximate)
- **Active Links:** Light purple/lavender highlight
- **Primary Actions:** Bright blue (#007bff)
- **Content Area:** Clean white background with subtle borders

### Layout
- **Responsive Design:** Two-column layout with collapsible sidebar
- **Navigation:** Fixed left sidebar with scrollable content
- **Content Area:** Flexible main content area with tabbed interfaces
- **Cards:** Shadow-based cards for grouped content

### Components
- **Buttons:** Rounded buttons with clear action labels
- **Forms:** Clean input fields with labels and placeholders
- **Tables/Lists:** Striped or bordered for readability
- **Badges:** Color-coded badges for status indicators
- **Tabs:** Horizontal tab navigation for multi-view interfaces

---

## Technical Stack

- **Framework:** ASP.NET Core 8.0 Blazor Server
- **Rendering:** Server-side rendering with interactive components
- **UI Library:** Bootstrap 5
- **Icons:** Bootstrap Icons
- **Real-time Updates:** SignalR for server communication

---

## Getting Started with the UI

1. **Start the Application:**
   ```bash
   cd src/Minotaur.UI.Blazor
   dotnet run
   ```

2. **Access the Application:**
   Navigate to `http://localhost:5000` in your web browser

3. **Explore Features:**
   - Begin with the Interactive Tutorial to learn the basics
   - Try the Grammar Editor to create or modify grammars
   - Browse the Marketplace for pre-built grammar templates
   - Use the StepParser Integration to analyze code
   - Manage your projects with the Project Manager

---

## Future Enhancements

Based on the navigation structure, the following pages are referenced but may need further implementation:
- Cognitive Graph Editor (visual graph manipulation)
- Graph Unparser (code generation from graphs)
- Code Templates page (authenticated users)
- My Collections page (authenticated users)

---

*This documentation was automatically generated using the UI Flow Documentation Tool. For updates, re-run the tool after making UI changes.*
