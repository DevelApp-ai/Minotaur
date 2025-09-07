/**
 * Code Generation Engine
 *
 * Converts transformed AST back to source code with proper formatting.
 * This is the final step in the AST-guided correction process.
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import {
  ASTTransformation,
  ErrorFix,
} from './StructuredValidationError';

export interface CodeGenerationOptions {
  language: 'python' | 'javascript' | 'typescript' | 'java' | 'csharp';
  indentSize: number;
  indentType: 'spaces' | 'tabs';
  preserveComments: boolean;
  preserveWhitespace: boolean;
  addTypeAnnotations: boolean;
  formatOutput: boolean;
}

export interface GenerationResult {
  success: boolean;
  generatedCode: string;
  sourceMap?: SourceMap;
  warnings: string[];
  generationTime: number;
  linesGenerated: number;
}

export interface SourceMap {
  version: number;
  sources: string[];
  mappings: string;
  names: string[];
}

export interface CodeTemplate {
  name: string;
  pattern: string;
  variables: string[];
  language: string;
}

/**
 * Code Generation Engine
 *
 * Generates clean, formatted source code from AST transformations
 */
export class CodeGenerationEngine {
  private templates: Map<string, CodeTemplate>;
  private formatters: Map<string, (code: string) => string>;

  constructor() {
    this.templates = new Map();
    this.formatters = new Map();
    this.initializeTemplates();
    this.initializeFormatters();
  }

  /**
   * Generate code from AST with applied transformations
   */
  async generateCode(
    ast: ZeroCopyASTNode | null,
    transformations: ASTTransformation[],
    options?: Partial<CodeGenerationOptions>,
  ): Promise<GenerationResult> {

    const startTime = Date.now();
    const defaultOptions: CodeGenerationOptions = {
      language: 'python',
      indentSize: 4,
      indentType: 'spaces',
      preserveComments: true,
      preserveWhitespace: false,
      addTypeAnnotations: false,
      formatOutput: true,
    };

    const finalOptions = { ...defaultOptions, ...options };
    const warnings: string[] = [];

    try {
      // If no AST provided, generate from transformations only
      if (!ast) {
        return this.generateFromTransformations(transformations, finalOptions, startTime);
      }

      // Generate code from AST
      const codeLines = await this.traverseAndGenerate(ast, finalOptions, 0);
      let generatedCode = codeLines.join('\n');

      // Apply transformations that couldn't be applied to AST
      generatedCode = this.applyDirectTransformations(generatedCode, transformations, finalOptions);

      // Format the output if requested
      if (finalOptions.formatOutput) {
        generatedCode = this.formatCode(generatedCode, finalOptions);
      }

      // Validate the generated code
      const validationWarnings = this.validateGeneratedCode(generatedCode, finalOptions);
      warnings.push(...validationWarnings);

      return {
        success: true,
        generatedCode,
        warnings,
        generationTime: Date.now() - startTime,
        linesGenerated: generatedCode.split('\n').length,
      };

    } catch (error) {
      console.warn('Code generation failed:', error);

      return {
        success: false,
        generatedCode: '',
        warnings: [`Code generation failed: ${error}`],
        generationTime: Date.now() - startTime,
        linesGenerated: 0,
      };
    }
  }

  /**
   * Generate code from transformations only (when AST is not available)
   */
  private async generateFromTransformations(
    transformations: ASTTransformation[],
    options: CodeGenerationOptions,
    startTime: number,
  ): Promise<GenerationResult> {

    const codeLines: string[] = [];
    const warnings: string[] = [];

    try {
      // Sort transformations by priority
      const sortedTransformations = this.sortTransformationsByPriority(transformations);

      // Generate code for each transformation
      for (const transformation of sortedTransformations) {
        const generatedLines = this.generateFromSingleTransformation(transformation, options);
        if (generatedLines.length > 0) {
          codeLines.push(...generatedLines);
        } else {
          warnings.push(`Failed to generate code for transformation: ${transformation.nodeType}`);
        }
      }

      let generatedCode = codeLines.join('\n');

      // Format the output
      if (options.formatOutput) {
        generatedCode = this.formatCode(generatedCode, options);
      }

      return {
        success: codeLines.length > 0,
        generatedCode,
        warnings,
        generationTime: Date.now() - startTime,
        linesGenerated: codeLines.length,
      };

    } catch (error) {
      return {
        success: false,
        generatedCode: '',
        warnings: [`Transformation-based generation failed: ${error}`],
        generationTime: Date.now() - startTime,
        linesGenerated: 0,
      };
    }
  }

  /**
   * Traverse AST and generate code
   */
  private async traverseAndGenerate(
    node: ZeroCopyASTNode,
    options: CodeGenerationOptions,
    depth: number,
  ): Promise<string[]> {

    const lines: string[] = [];
    const indent = this.createIndent(depth, options);

    try {
      // Generate code for current node
      const nodeCode = this.generateNodeCode(node, options, depth);
      if (nodeCode) {
        lines.push(indent + nodeCode);
      }

      // Recursively generate code for child nodes
      // Note: This is a simplified implementation
      // In a full implementation, this would traverse actual child nodes

      return lines;

    } catch (error) {
      console.warn(`Failed to generate code for node at depth ${depth}:`, error);
      return [];
    }
  }

  /**
   * Generate code for a single AST node
   */
  private generateNodeCode(
    node: ZeroCopyASTNode,
    options: CodeGenerationOptions,
    depth: number,
  ): string | null {

    // This would be implemented based on the actual ZeroCopyASTNode structure
    // For now, return a placeholder
    return null;
  }

  /**
   * Generate code from a single transformation
   */
  private generateFromSingleTransformation(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    const lines: string[] = [];

    try {
      switch (transformation.nodeType) {
        case 'ImportStatement':
          lines.push(...this.generateImportStatement(transformation, options));
          break;
        case 'VariableDeclaration':
          lines.push(...this.generateVariableDeclaration(transformation, options));
          break;
        case 'FunctionDeclaration':
          lines.push(...this.generateFunctionDeclaration(transformation, options));
          break;
        case 'Statement':
          lines.push(...this.generateStatement(transformation, options));
          break;
        default:
          lines.push(...this.generateGenericNode(transformation, options));
      }

    } catch (error) {
      console.warn(`Failed to generate code for transformation ${transformation.nodeType}:`, error);
    }

    return lines;
  }

  /**
   * Generate import statement
   */
  private generateImportStatement(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    if (transformation.newValue) {
      return [transformation.newValue];
    }

    if (transformation.newNode && typeof transformation.newNode === 'object') {
      const node = transformation.newNode;
      if (node.module) {
        return [`import ${node.module}`];
      }
    }

    return ['import sys  # Auto-generated import'];
  }

  /**
   * Generate variable declaration
   */
  private generateVariableDeclaration(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    if (transformation.newValue) {
      return [transformation.newValue];
    }

    if (transformation.newNode && typeof transformation.newNode === 'object') {
      const node = transformation.newNode;
      if (node.name && node.value) {
        return [`${node.name} = ${node.value}`];
      }
    }

    return ['variable = None  # Auto-generated variable'];
  }

  /**
   * Generate function declaration
   */
  private generateFunctionDeclaration(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    const lines: string[] = [];

    if (transformation.newNode && typeof transformation.newNode === 'object') {
      const node = transformation.newNode;
      const funcName = node.name || 'function';
      const params = node.parameters || [];
      const paramStr = Array.isArray(params) ? params.join(', ') : '';

      lines.push(`def ${funcName}(${paramStr}):`);

      if (node.docstring) {
        lines.push(`    """${node.docstring}"""`);
      }

      if (node.body && Array.isArray(node.body)) {
        for (const statement of node.body) {
          lines.push(`    ${statement}`);
        }
      } else {
        lines.push('    pass');
      }
    }

    return lines;
  }

  /**
   * Generate generic statement
   */
  private generateStatement(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    if (transformation.newValue) {
      return [transformation.newValue];
    }

    return ['pass  # Auto-generated statement'];
  }

  /**
   * Generate generic node
   */
  private generateGenericNode(
    transformation: ASTTransformation,
    options: CodeGenerationOptions,
  ): string[] {

    if (transformation.newValue) {
      return [transformation.newValue];
    }

    return [`# Auto-generated ${transformation.nodeType}`];
  }

  /**
   * Apply direct transformations to generated code
   */
  private applyDirectTransformations(
    code: string,
    transformations: ASTTransformation[],
    options: CodeGenerationOptions,
  ): string {

    let modifiedCode = code;

    for (const transformation of transformations) {
      if (transformation.operation === 'modify' && transformation.newValue) {
        // Apply simple string replacements for modifications
        modifiedCode = this.applyStringTransformation(modifiedCode, transformation);
      }
    }

    return modifiedCode;
  }

  /**
   * Apply string-based transformation
   */
  private applyStringTransformation(
    code: string,
    transformation: ASTTransformation,
  ): string {

    const lines = code.split('\n');

    // Find target line based on path
    const targetPath = transformation.targetPath || [];
    const lineMatch = targetPath.find(p => p.startsWith('line_'));

    if (lineMatch && transformation.newValue) {
      const lineNumber = parseInt(lineMatch.replace('line_', '')) - 1;
      if (lineNumber >= 0 && lineNumber < lines.length) {
        lines[lineNumber] = transformation.newValue;
      }
    }

    return lines.join('\n');
  }

  /**
   * Format generated code
   */
  private formatCode(code: string, options: CodeGenerationOptions): string {
    const formatter = this.formatters.get(options.language);
    if (formatter) {
      return formatter(code);
    }

    // Default formatting
    return this.defaultFormat(code, options);
  }

  /**
   * Default code formatting
   */
  private defaultFormat(code: string, options: CodeGenerationOptions): string {
    const lines = code.split('\n');
    const formattedLines: string[] = [];
    let currentIndent = 0;

    for (const line of lines) {
      const trimmedLine = line.trim();

      if (!trimmedLine) {
        formattedLines.push('');
        continue;
      }

      // Adjust indentation for Python-like languages
      if (options.language === 'python') {
        if (trimmedLine.endsWith(':')) {
          formattedLines.push(this.createIndent(currentIndent, options) + trimmedLine);
          currentIndent++;
        } else if (trimmedLine === 'pass' || trimmedLine.startsWith('return')) {
          formattedLines.push(this.createIndent(currentIndent, options) + trimmedLine);
          if (currentIndent > 0) {
currentIndent--;
}
        } else {
          formattedLines.push(this.createIndent(currentIndent, options) + trimmedLine);
        }
      } else {
        formattedLines.push(this.createIndent(currentIndent, options) + trimmedLine);
      }
    }

    return formattedLines.join('\n');
  }

  /**
   * Create indentation string
   */
  private createIndent(depth: number, options: CodeGenerationOptions): string {
    const unit = options.indentType === 'tabs' ? '\t' : ' '.repeat(options.indentSize);
    return unit.repeat(depth);
  }

  /**
   * Sort transformations by priority
   */
  private sortTransformationsByPriority(transformations: ASTTransformation[]): ASTTransformation[] {
    return transformations.sort((a, b) => {
      // Imports first
      if (a.nodeType === 'ImportStatement' && b.nodeType !== 'ImportStatement') {
return -1;
}
      if (b.nodeType === 'ImportStatement' && a.nodeType !== 'ImportStatement') {
return 1;
}

      // Variable declarations next
      if (a.nodeType === 'VariableDeclaration' && b.nodeType !== 'VariableDeclaration') {
return -1;
}
      if (b.nodeType === 'VariableDeclaration' && a.nodeType !== 'VariableDeclaration') {
return 1;
}

      return 0;
    });
  }

  /**
   * Validate generated code
   */
  private validateGeneratedCode(code: string, options: CodeGenerationOptions): string[] {
    const warnings: string[] = [];

    // Basic validation checks
    if (!code.trim()) {
      warnings.push('Generated code is empty');
    }

    if (options.language === 'python') {
      // Python-specific validation
      const lines = code.split('\n');
      for (let i = 0; i < lines.length; i++) {
        const line = lines[i];

        // Check for basic syntax issues
        if (line.trim().endsWith(':') && i === lines.length - 1) {
          warnings.push(`Line ${i + 1}: Statement ending with ':' should have a body`);
        }

        // Check for indentation consistency
        const leadingSpaces = line.match(/^(\s*)/)?.[1] || '';
        if (leadingSpaces.length % options.indentSize !== 0 && line.trim()) {
          warnings.push(`Line ${i + 1}: Inconsistent indentation`);
        }
      }
    }

    return warnings;
  }

  /**
   * Initialize code templates
   */
  private initializeTemplates(): void {
    // Python templates
    this.templates.set('python_import', {
      name: 'Python Import',
      pattern: 'import {module}',
      variables: ['module'],
      language: 'python',
    });

    this.templates.set('python_function', {
      name: 'Python Function',
      pattern: 'def {name}({params}):\n    """{docstring}"""\n    {body}',
      variables: ['name', 'params', 'docstring', 'body'],
      language: 'python',
    });

    this.templates.set('python_variable', {
      name: 'Python Variable',
      pattern: '{name} = {value}',
      variables: ['name', 'value'],
      language: 'python',
    });
  }

  /**
   * Initialize code formatters
   */
  private initializeFormatters(): void {
    // Python formatter
    this.formatters.set('python', (code: string) => {
      // Simple Python formatting
      return code
        .split('\n')
        .map(line => line.trimRight())
        .join('\n')
        .replace(/\n{3,}/g, '\n\n'); // Limit consecutive empty lines
    });

    // JavaScript formatter
    this.formatters.set('javascript', (code: string) => {
      // Simple JavaScript formatting
      return code
        .split('\n')
        .map(line => line.trimRight())
        .join('\n');
    });
  }

  /**
   * Get available templates for a language
   */
  getTemplates(language: string): CodeTemplate[] {
    return Array.from(this.templates.values()).filter(t => t.language === language);
  }

  /**
   * Get template by name
   */
  getTemplate(name: string): CodeTemplate | undefined {
    return this.templates.get(name);
  }
}

