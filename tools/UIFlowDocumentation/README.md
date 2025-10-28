# UI Flow Documentation Tool

This tool automatically generates comprehensive documentation with screenshots for all UI pages in the Minotaur web application.

## Prerequisites

- .NET 8.0 SDK
- Playwright browsers (installed automatically by the tool)

## Usage

1. Start the Minotaur.UI.Blazor application:
   ```bash
   cd ../../src/Minotaur.UI.Blazor
   dotnet run
   ```

2. In a separate terminal, run the documentation tool:
   ```bash
   cd UIFlowDocumentation
   dotnet run
   ```

   Or specify a custom URL and output directory:
   ```bash
   dotnet run http://localhost:5000 ../../docs/ui-screenshots
   ```

## Output

The tool generates:
- **UI_FLOW.md**: Comprehensive markdown documentation with descriptions of each page
- **Screenshots**: PNG images of each page in full-page mode

## Arguments

- **Argument 1** (optional): Base URL of the running application (default: `http://localhost:5000`)
- **Argument 2** (optional): Output directory for screenshots and documentation (default: `docs/ui-screenshots`)

## Pages Documented

The tool captures the following pages:

1. Home (/)
2. Grammar Editor (/grammar-editor)
3. Marketplace (/marketplace)
4. Interactive Tutorial (/tutorial)
5. StepParser Integration (/step-parser)
6. Symbolic Analyzer (/symbolic-analyzer)
7. Version Control (/version-control)
8. Plugin Manager (/plugin-manager)
9. Project Manager (/project-manager)
10. Counter Demo (/counter)
11. Weather Demo (/weather)
