/**
 * ASP to C# Translator
 *
 * This translator converts Classic ASP code (with VBScript/JScript) to modern C# ASP.NET Core.
 * It handles server-side scripting, HTML generation, database operations, and session management.
 *
 * Key Features:
 * - VBScript to C# conversion
 * - JScript to C# conversion
 * - ASP built-in objects mapping
 * - HTML generation patterns
 * - Database connectivity (ADO to Entity Framework)
 * - Session and state management
 * - Error handling modernization
 * - Security improvements
 */

import {
  AbstractLanguageTranslator,
  TranslationOptions,
  TranslationResult,
  TranslationCapabilities,
  TranslationWarning,
  TranslationError,
  MigrationNote,
  SemanticMappingEngine,
  TypeMapping,
  LibraryMapping,
  PatternMapping,
  DEFAULT_TRANSLATION_OPTIONS,
} from './LanguageTranslationSystem';
import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import { MemoryArena } from '../memory/arena/MemoryArena';
import { StringInterner } from '../memory/strings/StringInterner';

// ============================================================================
// ASP TO C# TRANSLATOR IMPLEMENTATION
// ============================================================================

export class AspToCSharpTranslator extends AbstractLanguageTranslator {
  private semanticEngine: SemanticMappingEngine;
  private aspObjectMappings: Map<string, AspObjectMapping>;
  private vbScriptPatterns: Map<string, string>;
  private jScriptPatterns: Map<string, string>;
  private arena: MemoryArena;
  private stringInterner: StringInterner;

  constructor() {
    super('Classic ASP', 'C#', '1.0.0');

    // Initialize memory infrastructure for ZeroCopy operations
    this.arena = new MemoryArena(1024 * 1024); // 1MB arena
    this.stringInterner = new StringInterner(this.arena);

    this.semanticEngine = new SemanticMappingEngine();
    this.aspObjectMappings = new Map();
    this.vbScriptPatterns = new Map();
    this.jScriptPatterns = new Map();

    this.initializeSemanticMappings();
    this.initializeAspObjectMappings();
    this.initializeScriptPatterns();
  }

  /**
   * Helper method to create ZeroCopyASTNode instances
   */
  private createASTNode(name: string, nodeType: ASTNodeType = ASTNodeType.STATEMENT): ZeroCopyASTNode {
    return ZeroCopyASTNode.create(this.arena, this.stringInterner, nodeType, name);
  }

  // ========================================================================
  // PUBLIC API IMPLEMENTATION
  // ========================================================================

  async translate(sourceCode: string, options?: TranslationOptions): Promise<TranslationResult> {
    const startTime = Date.now();
    const memoryStart = process.memoryUsage().heapUsed;

    try {
      // Merge with default options
      const translationOptions = { ...DEFAULT_TRANSLATION_OPTIONS, ...options };

      // Parse ASP source code
      const sourceAST = this.parseSourceCode(sourceCode);

      // Transform to C# AST
      const targetAST = this.transformAST(sourceAST, translationOptions);

      // Generate C# code
      const translatedCode = this.generateTargetCode(targetAST, translationOptions);

      // Validate translation
      const warnings = this.validateTranslation(sourceCode, translatedCode, translationOptions);

      // Generate migration notes
      const migrationNotes = this.generateMigrationNotes(sourceAST, targetAST, translationOptions);

      // Calculate statistics
      const memoryEnd = process.memoryUsage().heapUsed;
      const statistics = this.calculateStatistics(sourceCode, translatedCode, startTime, memoryEnd - memoryStart);

      return {
        translatedCode,
        success: warnings.filter(w => w.severity === 'error').length === 0,
        warnings: warnings.filter(w => w.severity !== 'error'),
        errors: warnings.filter(w => w.severity === 'error').map(w => ({
          message: w.message,
          sourceLocation: w.sourceLocation,
          category: w.category as any,
          suggestions: [],
        })),
        migrationNotes,
        statistics,
      };

    } catch (error) {
      return {
        translatedCode: '',
        success: false,
        warnings: [],
        errors: [{
          message: error instanceof Error ? error instanceof Error ? error.message : String(error) : 'Unknown translation error',
          sourceLocation: { line: 1, column: 1 },
          category: 'syntax',
          suggestions: ['Check ASP syntax and try again'],
        }],
        migrationNotes: [],
        statistics: this.calculateStatistics(sourceCode, '', startTime, 0),
      };
    }
  }

  canTranslate(sourceCode: string): boolean {
    // Check for ASP indicators
    const aspIndicators = [
      /<%.*%>/,           // ASP code blocks
      /<%=.*%>/,          // ASP expressions
      /<%@.*%>/,          // ASP directives
      /Response\.Write/i,  // ASP Response object
      /Request\./i,       // ASP Request object
      /Server\./i,        // ASP Server object
      /Session\./i,       // ASP Session object
      /Application\./i,    // ASP Application object
    ];

    return aspIndicators.some(pattern => pattern.test(sourceCode));
  }

  getCapabilities(): TranslationCapabilities {
    return {
      supportedConstructs: [
        'ASP Directives',
        'VBScript Variables and Functions',
        'JScript Variables and Functions',
        'ASP Built-in Objects (Request, Response, Server, Session, Application)',
        'HTML Generation',
        'Database Operations (ADO)',
        'Include Files',
        'Error Handling',
        'Control Structures (If, For, While, Select)',
        'COM Object Creation',
        'File System Operations',
      ],
      unsupportedConstructs: [
        'Legacy COM Components (requires manual migration)',
        'Custom ISAPI Extensions',
        'Advanced ADO Recordset Features',
        'Server-side ActiveX Controls',
        'Legacy Authentication Methods',
      ],
      supportedFrameworks: [
        'ASP.NET Core 6.0+',
        'ASP.NET Core MVC',
        'Entity Framework Core',
        'ASP.NET Core Identity',
      ],
      typeMappingSupport: true,
      libraryMappingSupport: true,
      maxFileSize: 10 * 1024 * 1024, // 10MB
      performance: {
        averageSpeed: 1000, // lines per second
        memoryPerLine: 512, // bytes per line
        scalabilityLimits: {
          maxLines: 50000,
          maxFunctions: 1000,
          maxClasses: 100,
        },
      },
    };
  }

  // ========================================================================
  // AST PROCESSING IMPLEMENTATION
  // ========================================================================

  protected parseSourceCode(sourceCode: string): ZeroCopyASTNode {
    // Create a simplified AST representation for ASP code
    const rootNode = ZeroCopyASTNode.create(
      this.arena,
      this.stringInterner,
      ASTNodeType.PROGRAM,
      'asp_document',
    );

    // Parse ASP directives
    this.parseAspDirectives(sourceCode, rootNode);

    // Parse ASP code blocks
    this.parseAspCodeBlocks(sourceCode, rootNode);

    // Parse HTML content
    this.parseHtmlContent(sourceCode, rootNode);

    return rootNode;
  }

  protected transformAST(sourceAST: ZeroCopyASTNode, options?: TranslationOptions): ZeroCopyASTNode {
    // Create target C# AST structure
    const targetAST = this.createASTNode('csharp_controller');

    // Transform ASP structure to C# Controller/Razor Pages
    this.transformToController(sourceAST, targetAST, options);

    return targetAST;
  }

  protected generateTargetCode(targetAST: ZeroCopyASTNode, options?: TranslationOptions): string {
    const codeBuilder = new CSharpCodeBuilder(options);

    // Generate using statements
    codeBuilder.addUsings([
      'Microsoft.AspNetCore.Mvc',
      'Microsoft.AspNetCore.Http',
      'Microsoft.Extensions.Logging',
      'System.Threading.Tasks',
      'System.Text',
      'Microsoft.EntityFrameworkCore',
    ]);

    // Generate namespace and class
    codeBuilder.addNamespace('ConvertedAspApplication');
    codeBuilder.addController('HomeController');

    // Process AST nodes
    this.generateFromASTNode(targetAST, codeBuilder);

    return codeBuilder.toString();
  }

  // ========================================================================
  // ASP PARSING METHODS
  // ========================================================================

  private parseAspDirectives(sourceCode: string, rootNode: ZeroCopyASTNode): void {
    const directiveRegex = /<%@\s*(\w+)\s*=\s*["']([^"']*)["']\s*%>/gi;
    let match;

    while ((match = directiveRegex.exec(sourceCode)) !== null) {
      const directiveNode = this.createASTNode('asp_directive');
      // Store directive type and value in node attributes
      directiveNode.setAttribute('directive_type', match[1]);
      directiveNode.setAttribute('directive_value', match[2]);
      rootNode.appendChild(directiveNode);
    }
  }

  private parseAspCodeBlocks(sourceCode: string, rootNode: ZeroCopyASTNode): void {
    // Parse server-side code blocks
    const codeBlockRegex = /<%([^=@].*?)%>/gis;
    let match;

    while ((match = codeBlockRegex.exec(sourceCode)) !== null) {
      const codeNode = this.createASTNode('asp_code_block');
      const codeContent = match[1].trim();

      // Determine script language and parse accordingly
      if (this.isVbScriptCode(codeContent)) {
        this.parseVbScriptCode(codeContent, codeNode);
      } else if (this.isJScriptCode(codeContent)) {
        this.parseJScriptCode(codeContent, codeNode);
      } else {
        // Generic code block
        codeNode.setAttribute('code_content', codeContent);
      }

      rootNode.appendChild(codeNode);
    }

    // Parse expression blocks
    const exprRegex = /<%=\s*(.*?)\s*%>/gis;
    while ((match = exprRegex.exec(sourceCode)) !== null) {
      const exprNode = this.createASTNode('asp_expression');
      exprNode.setAttribute('expression', match[1].trim());
      rootNode.appendChild(exprNode);
    }
  }

  private parseHtmlContent(sourceCode: string, rootNode: ZeroCopyASTNode): void {
    // Remove ASP code blocks to get HTML content
    const htmlContent = sourceCode.replace(/<%.*?%>/gis, '');

    if (htmlContent.trim()) {
      const htmlNode = this.createASTNode('html_content');
      htmlNode.setAttribute('html', htmlContent);
      rootNode.appendChild(htmlNode);
    }
  }

  private isVbScriptCode(code: string): boolean {
    const vbKeywords = /\b(Dim|Set|If|Then|End|Function|Sub|Call|Response\.Write)\b/i;
    return vbKeywords.test(code);
  }

  private isJScriptCode(code: string): boolean {
    const jsKeywords = /\b(var|function|if|for|while|Response\.Write)\b/;
    return jsKeywords.test(code);
  }

  private parseVbScriptCode(code: string, parentNode: ZeroCopyASTNode): void {
    const vbNode = this.createASTNode('vbscript_code');

    // Parse VBScript statements
    const statements = this.parseVbScriptStatements(code);
    statements.forEach(stmt => vbNode.appendChild(stmt));

    parentNode.appendChild(vbNode);
  }

  private parseJScriptCode(code: string, parentNode: ZeroCopyASTNode): void {
    const jsNode = this.createASTNode('jscript_code');

    // Parse JScript statements
    const statements = this.parseJScriptStatements(code);
    statements.forEach(stmt => jsNode.appendChild(stmt));

    parentNode.appendChild(jsNode);
  }

  private parseVbScriptStatements(code: string): ZeroCopyASTNode[] {
    const statements: ZeroCopyASTNode[] = [];
    const lines = code.split('\n');

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();
      if (!line || line.startsWith("'")) {
        continue;
      } // Skip empty lines and comments

      const stmt = this.createASTNode('vb_statement');
      stmt.setAttribute('statement_type', this.getVbStatementType(line));
      stmt.setAttribute('content', line);
      statements.push(stmt);
    }

    return statements;
  }

  private parseJScriptStatements(code: string): ZeroCopyASTNode[] {
    const statements: ZeroCopyASTNode[] = [];
    const lines = code.split('\n');

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();
      if (!line || line.startsWith('//')) {
        continue;
      } // Skip empty lines and comments

      const stmt = this.createASTNode('js_statement');
      stmt.setAttribute('statement_type', this.getJsStatementType(line));
      stmt.setAttribute('content', line);
      statements.push(stmt);
    }

    return statements;
  }

  private getVbStatementType(line: string): string {
    if (line.match(/^\s*Dim\b/i)) {
      return 'variable_declaration';
    }
    if (line.match(/^\s*Set\b/i)) {
      return 'object_assignment';
    }
    if (line.match(/^\s*If\b/i)) {
      return 'if_statement';
    }
    if (line.match(/^\s*For\b/i)) {
      return 'for_loop';
    }
    if (line.match(/^\s*While\b/i)) {
      return 'while_loop';
    }
    if (line.match(/^\s*Function\b/i)) {
      return 'function_declaration';
    }
    if (line.match(/^\s*Sub\b/i)) {
      return 'subroutine_declaration';
    }
    if (line.match(/Response\.Write/i)) {
      return 'response_write';
    }
    if (line.match(/=.*$/)) {
      return 'assignment';
    }
    return 'expression';
  }

  private getJsStatementType(line: string): string {
    if (line.match(/^\s*var\b/)) {
      return 'variable_declaration';
    }
    if (line.match(/^\s*function\b/)) {
      return 'function_declaration';
    }
    if (line.match(/^\s*if\b/)) {
      return 'if_statement';
    }
    if (line.match(/^\s*for\b/)) {
      return 'for_loop';
    }
    if (line.match(/^\s*while\b/)) {
      return 'while_loop';
    }
    if (line.match(/Response\.Write/)) {
      return 'response_write';
    }
    if (line.match(/=.*$/)) {
      return 'assignment';
    }
    return 'expression';
  }

  // ========================================================================
  // AST TRANSFORMATION METHODS
  // ========================================================================

  // eslint-disable-next-line max-len
  private transformToController(sourceAST: ZeroCopyASTNode, targetAST: ZeroCopyASTNode, options?: TranslationOptions): void {
    // Create controller structure
    const controllerNode = this.createASTNode('controller_class');
    controllerNode.setAttribute('class_name', 'HomeController');
    controllerNode.setAttribute('base_class', 'Controller');

    // Create action method
    const actionNode = this.createASTNode('action_method');
    actionNode.setAttribute('method_name', 'Index');
    actionNode.setAttribute('return_type', 'IActionResult');

    // Transform ASP code to C# method body
    this.transformAspCode(sourceAST, actionNode, options);

    controllerNode.appendChild(actionNode);
    targetAST.appendChild(controllerNode);
  }

  // eslint-disable-next-line max-len
  private transformAspCode(sourceAST: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    for (const child of sourceAST.getChildren()) {
      switch (child.name) {
        case 'asp_code_block':
          this.transformCodeBlock(child, targetMethod, options);
          break;
        case 'asp_expression':
          this.transformExpression(child, targetMethod, options);
          break;
        case 'html_content':
          this.transformHtmlContent(child, targetMethod, options);
          break;
      }
    }
  }

  // eslint-disable-next-line max-len
  private transformCodeBlock(codeBlock: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    for (const child of codeBlock.getChildren()) {
      if (child.name === 'vbscript_code') {
        this.transformVbScriptCode(child, targetMethod, options);
      } else if (child.name === 'jscript_code') {
        this.transformJScriptCode(child, targetMethod, options);
      }
    }
  }

  // eslint-disable-next-line max-len
  private transformVbScriptCode(vbCode: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    for (const statement of vbCode.getChildren()) {
      const stmtType = statement.getAttribute('statement_type') || 'unknown';
      const content = statement.getAttribute('content') || statement.value;

      const csharpStmt = this.convertVbScriptToCSharp(stmtType, content);
      if (csharpStmt) {
        const csharpNode = this.createASTNode('csharp_statement');
        csharpNode.setAttribute('content', csharpStmt);
        targetMethod.appendChild(csharpNode);
      }
    }
  }

  // eslint-disable-next-line max-len
  private transformJScriptCode(jsCode: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    for (const statement of jsCode.getChildren()) {
      const stmtType = statement.getAttribute('statement_type') || 'unknown';
      const content = statement.getAttribute('content') || statement.value;

      const csharpStmt = this.convertJScriptToCSharp(stmtType, content);
      if (csharpStmt) {
        const csharpNode = this.createASTNode('csharp_statement');
        csharpNode.setAttribute('content', csharpStmt);
        targetMethod.appendChild(csharpNode);
      }
    }
  }

  // eslint-disable-next-line max-len
  private transformExpression(expression: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    const expr = expression.getAttribute('expression') || expression.value;
    const csharpExpr = this.convertExpressionToCSharp(expr);

    const outputNode = this.createASTNode('csharp_statement');
    outputNode.setAttribute('content', `await Response.WriteAsync(${csharpExpr});`);
    targetMethod.appendChild(outputNode);
  }

  // eslint-disable-next-line max-len
  private transformHtmlContent(htmlContent: ZeroCopyASTNode, targetMethod: ZeroCopyASTNode, options?: TranslationOptions): void {
    const html = htmlContent.getAttribute('html') || htmlContent.value;
    if (html && html.trim()) {
      const outputNode = this.createASTNode('csharp_statement');
      const escapedHtml = html.replace(/"/g, '\\"').replace(/\n/g, '\\n');
      outputNode.setAttribute('content', `await Response.WriteAsync("${escapedHtml}");`);
      targetMethod.appendChild(outputNode);
    }
  }

  // ========================================================================
  // SCRIPT CONVERSION METHODS
  // ========================================================================

  private convertVbScriptToCSharp(statementType: string, content: string): string {
    switch (statementType) {
      case 'variable_declaration':
        return this.convertVbVariableDeclaration(content);
      case 'object_assignment':
        return this.convertVbObjectAssignment(content);
      case 'assignment':
        return this.convertVbAssignment(content);
      case 'response_write':
        return this.convertVbResponseWrite(content);
      case 'if_statement':
        return this.convertVbIfStatement(content);
      case 'for_loop':
        return this.convertVbForLoop(content);
      case 'function_declaration':
        return this.convertVbFunctionDeclaration(content);
      default:
        return this.convertVbGenericStatement(content);
    }
  }

  private convertJScriptToCSharp(statementType: string, content: string): string {
    switch (statementType) {
      case 'variable_declaration':
        return this.convertJsVariableDeclaration(content);
      case 'assignment':
        return this.convertJsAssignment(content);
      case 'response_write':
        return this.convertJsResponseWrite(content);
      case 'if_statement':
        return this.convertJsIfStatement(content);
      case 'for_loop':
        return this.convertJsForLoop(content);
      case 'function_declaration':
        return this.convertJsFunctionDeclaration(content);
      default:
        return this.convertJsGenericStatement(content);
    }
  }

  // VBScript to C# conversion methods
  private convertVbVariableDeclaration(content: string): string {
    const match = content.match(/Dim\s+(\w+)(?:\s+As\s+(\w+))?/i);
    if (match) {
      const varName = this.convertVbIdentifier(match[1]);
      const varType = match[2] ? this.convertVbType(match[2]) : 'var';
      return `${varType} ${varName};`;
    }

    // Handle multiple variable declarations: Dim a, b, c
    const multiMatch = content.match(/Dim\s+([\w\s,]+)/i);
    if (multiMatch) {
      const vars = multiMatch[1].split(',').map(v => v.trim());
      const declarations = vars.map(varName => `var ${this.convertVbIdentifier(varName)};`);
      return declarations.join('\n');
    }

    return `// Failed to convert VB variable declaration: ${content}`;
  }

  private convertVbObjectAssignment(content: string): string {
    const match = content.match(/Set\s+(\w+)\s*=\s*(.*)/i);
    if (match) {
      const varName = this.convertVbIdentifier(match[1]);
      let value = this.convertVbExpression(match[2]);

      // Handle common VB object creation patterns
      if (value.includes('CreateObject')) {
        const createMatch = value.match(/CreateObject\s*\(\s*["']([^"']+)["']\s*\)/i);
        if (createMatch) {
          const comClass = createMatch[1];
          // Convert common COM objects to .NET equivalents
          switch (comClass.toLowerCase()) {
            case 'adodb.connection':
              value = 'new SqlConnection(connectionString)';
              break;
            case 'adodb.recordset':
              value = 'new DataTable()';
              break;
            case 'scripting.filesystemobject':
              value = 'new FileInfo("")';
              break;
            case 'scripting.dictionary':
              value = 'new Dictionary<string, object>()';
              break;
            default:
              value = `new ${comClass.replace(/\./g, '_')}()`;
          }
        }
      }

      return `${varName} = ${value};`;
    }
    return `// Failed to convert VB object assignment: ${content}`;
  }

  private convertVbAssignment(content: string): string {
    const match = content.match(/(\w+)\s*=\s*(.*)/);
    if (match) {
      const varName = this.convertVbIdentifier(match[1]);
      const value = this.convertVbExpression(match[2]);
      return `${varName} = ${value};`;
    }
    return `// Failed to convert VB assignment: ${content}`;
  }

  private convertVbResponseWrite(content: string): string {
    const match = content.match(/Response\.Write\s*\((.*)?\)/i) || content.match(/Response\.Write\s+(.*)/i);
    if (match) {
      const expr = this.convertVbExpression(match[1]);
      return `await Response.WriteAsync(${expr});`;
    }
    return `// Failed to convert VB Response.Write: ${content}`;
  }

  private convertVbIfStatement(content: string): string {
    const match = content.match(/If\s+(.*)\s+Then/i);
    if (match) {
      const condition = this.convertVbExpression(match[1]);
      return `if (${condition}) {`;
    }

    // Handle End If
    if (content.match(/End\s+If/i)) {
      return '}';
    }

    // Handle ElseIf
    const elseIfMatch = content.match(/ElseIf\s+(.*)\s+Then/i);
    if (elseIfMatch) {
      const condition = this.convertVbExpression(elseIfMatch[1]);
      return `} else if (${condition}) {`;
    }

    // Handle Else
    if (content.match(/^\s*Else\s*$/i)) {
      return '} else {';
    }

    return `// Failed to convert VB If statement: ${content}`;
  }

  private convertVbForLoop(content: string): string {
    const match = content.match(/For\s+(\w+)\s*=\s*(.*)\s+To\s+(.*?)(?:\s+Step\s+(.*))?/i);
    if (match) {
      const varName = this.convertVbIdentifier(match[1]);
      const startValue = this.convertVbExpression(match[2]);
      const endValue = this.convertVbExpression(match[3]);
      const step = match[4] ? this.convertVbExpression(match[4]) : '1';

      if (step === '1') {
        return `for (int ${varName} = ${startValue}; ${varName} <= ${endValue}; ${varName}++) {`;
      } else {
        return `for (int ${varName} = ${startValue}; ${varName} <= ${endValue}; ${varName} += ${step}) {`;
      }
    }

    // Handle For Each
    const forEachMatch = content.match(/For\s+Each\s+(\w+)\s+In\s+(.*)/i);
    if (forEachMatch) {
      const varName = this.convertVbIdentifier(forEachMatch[1]);
      const collection = this.convertVbExpression(forEachMatch[2]);
      return `foreach (var ${varName} in ${collection}) {`;
    }

    // Handle Next
    if (content.match(/^\s*Next\s*(\w+)?\s*$/i)) {
      return '}';
    }

    return `// Failed to convert VB For loop: ${content}`;
  }

  private convertVbFunctionDeclaration(content: string): string {
    const match = content.match(/Function\s+(\w+)\(([^)]*)\)(?:\s+As\s+(\w+))?/i);
    if (match) {
      const funcName = this.convertVbIdentifier(match[1]);
      const params = match[2] ? this.convertVbParameters(match[2]) : '';
      const returnType = match[3] ? this.convertVbType(match[3]) : 'object';
      return `public ${returnType} ${funcName}(${params}) {`;
    }

    // Handle Sub declaration
    const subMatch = content.match(/Sub\s+(\w+)\(([^)]*)\)/i);
    if (subMatch) {
      const subName = this.convertVbIdentifier(subMatch[1]);
      const params = subMatch[2] ? this.convertVbParameters(subMatch[2]) : '';
      return `public void ${subName}(${params}) {`;
    }

    // Handle End Function/Sub
    if (content.match(/End\s+(Function|Sub)/i)) {
      return '}';
    }

    return `// Failed to convert VB Function: ${content}`;
  }

  private convertVbGenericStatement(content: string): string {
    // Apply VBScript patterns
    let converted = content;
    for (const [pattern, replacement] of this.vbScriptPatterns) {
      converted = converted.replace(new RegExp(pattern, 'gi'), replacement);
    }
    return converted;
  }

  // JScript to C# conversion methods
  private convertJsVariableDeclaration(content: string): string {
    const match = content.match(/var\s+(\w+)(?:\s*=\s*(.*))?/);
    if (match) {
      const varName = match[1];
      const value = match[2] ? this.convertJsExpression(match[2]) : null;
      return value ? `var ${varName} = ${value};` : `var ${varName};`;
    }

    // Handle let/const declarations
    const letMatch = content.match(/(let|const)\s+(\w+)(?:\s*=\s*(.*))?/);
    if (letMatch) {
      const varName = letMatch[2];
      const value = letMatch[3] ? this.convertJsExpression(letMatch[3]) : null;
      return value ? `var ${varName} = ${value};` : `var ${varName};`;
    }

    return `// Failed to convert JS variable declaration: ${content}`;
  }

  private convertJsAssignment(content: string): string {
    const match = content.match(/(\w+)\s*=\s*(.*)/);
    if (match) {
      const varName = match[1];
      const value = this.convertJsExpression(match[2]);
      return `${varName} = ${value};`;
    }

    // Handle compound assignments
    const compoundMatch = content.match(/(\w+)\s*([+\-*/])=\s*(.*)/);
    if (compoundMatch) {
      const varName = compoundMatch[1];
      const operator = compoundMatch[2];
      const value = this.convertJsExpression(compoundMatch[3]);
      return `${varName} ${operator}= ${value};`;
    }

    return `// Failed to convert JS assignment: ${content}`;
  }

  private convertJsResponseWrite(content: string): string {
    const match = content.match(/Response\.Write\s*\((.*)\)/) || content.match(/Response\.Write\s+(.*)/);
    if (match) {
      const expr = this.convertJsExpression(match[1]);
      return `await Response.WriteAsync(${expr});`;
    }
    return `// Failed to convert JS Response.Write: ${content}`;
  }

  private convertJsIfStatement(content: string): string {
    const match = content.match(/if\s*\((.*)\)/);
    if (match) {
      const condition = this.convertJsExpression(match[1]);
      return `if (${condition}) {`;
    }

    // Handle else if
    const elseIfMatch = content.match(/else\s+if\s*\((.*)\)/);
    if (elseIfMatch) {
      const condition = this.convertJsExpression(elseIfMatch[1]);
      return `} else if (${condition}) {`;
    }

    // Handle else
    if (content.match(/^\s*else\s*$/)) {
      return '} else {';
    }

    return `// Failed to convert JS If statement: ${content}`;
  }

  private convertJsForLoop(content: string): string {
    const match = content.match(/for\s*\(([^;]*);([^;]*);([^)]*)\)/);
    if (match) {
      const init = this.convertJsExpression(match[1].trim());
      const condition = this.convertJsExpression(match[2].trim());
      const increment = this.convertJsExpression(match[3].trim());
      return `for (${init}; ${condition}; ${increment}) {`;
    }

    // Handle for...in loops
    const forInMatch = content.match(/for\s*\((\w+)\s+in\s+(.+)\)/);
    if (forInMatch) {
      const varName = forInMatch[1];
      const collection = this.convertJsExpression(forInMatch[2]);
      return `foreach (var ${varName} in ${collection}) {`;
    }

    return `// Failed to convert JS For loop: ${content}`;
  }

  private convertJsFunctionDeclaration(content: string): string {
    const match = content.match(/function\s+(\w+)\s*\(([^)]*)\)/);
    if (match) {
      const funcName = match[1];
      const params = match[2] ? this.convertJsParameters(match[2]) : '';
      return `public object ${funcName}(${params}) {`;
    }

    // Handle anonymous functions assigned to variables
    const anonMatch = content.match(/(\w+)\s*=\s*function\s*\(([^)]*)\)/);
    if (anonMatch) {
      const funcName = anonMatch[1];
      const params = anonMatch[2] ? this.convertJsParameters(anonMatch[2]) : '';
      return `public object ${funcName}(${params}) {`;
    }

    return `// Failed to convert JS Function: ${content}`;
  }

  private convertJsGenericStatement(content: string): string {
    // Apply JScript patterns
    let converted = content;
    for (const [pattern, replacement] of this.jScriptPatterns) {
      converted = converted.replace(new RegExp(pattern, 'g'), replacement);
    }
    return converted;
  }

  // ========================================================================
  // EXPRESSION CONVERSION HELPERS
  // ========================================================================

  private convertExpressionToCSharp(expression: string): string {
    // Convert ASP objects and methods
    let converted = expression;

    // Convert ASP built-in objects
    for (const [aspObject, mapping] of this.aspObjectMappings) {
      const regex = new RegExp(`\\b${aspObject}\\b`, 'gi');
      converted = converted.replace(regex, mapping.csharpEquivalent);
    }

    return converted;
  }

  private convertVbExpression(expression: string): string {
    let converted = expression.trim();

    // Convert VBScript operators
    converted = converted.replace(/\\bAnd\\b/gi, '&&');
    converted = converted.replace(/\\bOr\\b/gi, '||');
    converted = converted.replace(/\\bNot\\b/gi, '!');
    converted = converted.replace(/<>/g, '!=');

    // Convert VBScript functions
    converted = converted.replace(/\bLen\(/gi, '.Length');
    converted = converted.replace(/\bUCase\(/gi, '.ToUpper(');
    converted = converted.replace(/\bLCase\(/gi, '.ToLower(');

    // Convert string concatenation
    converted = converted.replace(/&/g, '+');

    return converted;
  }

  private convertJsExpression(expression: string): string {
    let converted = expression.trim();

    // JScript is closer to C#, fewer conversions needed
    // Convert typeof to GetType() where appropriate
    converted = converted.replace(/typeof\\s+(\\w+)/g, '$1.GetType()');

    return converted;
  }

  private convertVbIdentifier(identifier: string): string {
    // Convert VBScript naming to C# conventions
    return identifier.charAt(0).toLowerCase() + identifier.slice(1);
  }

  private convertVbType(vbType: string): string {
    const typeMap: Record<string, string> = {
      'String': 'string',
      'Integer': 'int',
      'Long': 'long',
      'Boolean': 'bool',
      'Double': 'double',
      'Single': 'float',
      'Variant': 'object',
      'Object': 'object',
    };

    return typeMap[vbType] || 'object';
  }

  private convertVbParameters(params: string): string {
    return params.split(',').map(param => {
      const trimmed = param.trim();
      const match = trimmed.match(/(\\w+)(?:\\s+As\\s+(\\w+))?/);
      if (match) {
        const paramName = this.convertVbIdentifier(match[1]);
        const paramType = match[2] ? this.convertVbType(match[2]) : 'object';
        return `${paramType} ${paramName}`;
      }
      return `object ${trimmed}`;
    }).join(', ');
  }

  private convertJsParameters(params: string): string {
    return params.split(',').map(param => {
      const paramName = param.trim();
      return `object ${paramName}`;
    }).join(', ');
  }

  // ========================================================================
  // CODE GENERATION METHODS
  // ========================================================================

  private generateFromASTNode(node: ZeroCopyASTNode, codeBuilder: CSharpCodeBuilder): void {
    for (const child of node.getChildren()) {
      switch (child.name) {
        case 'controller_class':
          this.generateController(child, codeBuilder);
          break;
        case 'action_method':
          this.generateActionMethod(child, codeBuilder);
          break;
        case 'csharp_statement':
          codeBuilder.addStatement(child.getAttribute('content') || child.value);
          break;
      }
    }
  }

  private generateController(controllerNode: ZeroCopyASTNode, codeBuilder: CSharpCodeBuilder): void {
    const className = controllerNode.getAttribute('class_name') || 'HomeController';
    const baseClass = controllerNode.getAttribute('base_class') || 'Controller';

    codeBuilder.startClass(className, baseClass);

    // Generate constructor
    codeBuilder.addConstructor(className, [
      'ILogger<HomeController> logger',
    ], [
      '_logger = logger;',
    ]);

    // Add logger field
    codeBuilder.addField('private readonly ILogger<HomeController> _logger;');

    // Generate child methods
    for (const child of controllerNode.getChildren()) {
      if (child.name === 'action_method') {
        this.generateActionMethod(child, codeBuilder);
      }
    }

    codeBuilder.endClass();
  }

  private generateActionMethod(methodNode: ZeroCopyASTNode, codeBuilder: CSharpCodeBuilder): void {
    const methodName = methodNode.getAttribute('method_name') || 'Index';
    const returnType = methodNode.getAttribute('return_type') || 'IActionResult';

    codeBuilder.startMethod(methodName, returnType, [], 'public async');

    // Generate method body
    for (const child of methodNode.getChildren()) {
      if (child.name === 'csharp_statement') {
        codeBuilder.addStatement(child.getAttribute('content') || child.value);
      }
    }

    codeBuilder.addStatement('return View();');
    codeBuilder.endMethod();
  }

  // ========================================================================
  // INITIALIZATION METHODS
  // ========================================================================

  private initializeSemanticMappings(): void {
    // Type mappings
    this.semanticEngine.addTypeMapping('String', {
      targetType: 'string',
      compatibility: 'exact',
    });

    this.semanticEngine.addTypeMapping('Integer', {
      targetType: 'int',
      compatibility: 'exact',
    });

    this.semanticEngine.addTypeMapping('Boolean', {
      targetType: 'bool',
      compatibility: 'exact',
    });

    this.semanticEngine.addTypeMapping('Variant', {
      targetType: 'object',
      compatibility: 'approximate',
      notes: 'VBScript Variant maps to C# object - may need casting',
    });

    // Library mappings
    this.semanticEngine.addLibraryMapping('ADODB', {
      targetLibrary: 'Entity Framework Core',
      targetVersion: '8.0',
      packageInfo: {
        packageManager: 'nuget',
        packageName: 'Microsoft.EntityFrameworkCore',
        version: '8.0.0',
      },
      compatibility: 'approximate',
      migrationNotes: [
        'ADO recordsets should be replaced with Entity Framework entities',
        'Connection strings need to be updated for modern providers',
        'SQL queries should use LINQ or raw SQL with parameterization',
      ],
    });
  }

  private initializeAspObjectMappings(): void {
    this.aspObjectMappings.set('Request', {
      csharpEquivalent: 'Request',
      properties: new Map([
        ['Form', 'Request.Form'],
        ['QueryString', 'Request.Query'],
        ['ServerVariables', 'Request.Headers'],
        ['Cookies', 'Request.Cookies'],
      ]),
      methods: new Map([
        ['BinaryRead', 'Request.Body.ReadAsync'],
        ['Item', 'Request.Form'],
      ]),
      notes: 'ASP.NET Core Request object provides similar functionality',
    });

    this.aspObjectMappings.set('Response', {
      csharpEquivalent: 'Response',
      properties: new Map([
        ['Buffer', 'Response.BufferOutput'],
        ['ContentType', 'Response.ContentType'],
        ['Status', 'Response.StatusCode'],
      ]),
      methods: new Map([
        ['Write', 'Response.WriteAsync'],
        ['Redirect', 'Response.Redirect'],
        ['End', 'Response.CompleteAsync'],
        ['Clear', 'Response.Clear'],
        ['Flush', 'Response.FlushAsync'],
      ]),
      notes: 'ASP.NET Core Response object with async methods',
    });

    this.aspObjectMappings.set('Server', {
      csharpEquivalent: 'Server',
      properties: new Map(),
      methods: new Map([
        ['MapPath', 'Path.Combine(Environment.ContentRootPath, ...)'],
        ['CreateObject', 'new ...()'],
        ['URLEncode', 'Uri.EscapeDataString'],
        ['HTMLEncode', 'HtmlEncoder.Default.Encode'],
      ]),
      notes: 'Server functionality distributed across multiple .NET classes',
    });

    this.aspObjectMappings.set('Session', {
      csharpEquivalent: 'HttpContext.Session',
      properties: new Map([
        ['SessionID', 'HttpContext.Session.Id'],
        ['Timeout', 'HttpContext.Session.IdleTimeout'],
      ]),
      methods: new Map([
        ['Item', 'HttpContext.Session.GetString/SetString'],
        ['Abandon', 'HttpContext.Session.Clear'],
      ]),
      notes: 'ASP.NET Core Session with different API',
    });

    this.aspObjectMappings.set('Application', {
      csharpEquivalent: 'Application',
      properties: new Map(),
      methods: new Map([
        ['Item', 'Application state - use dependency injection instead'],
        ['Lock', 'Use concurrent collections or locking mechanisms'],
        ['Unlock', 'Use concurrent collections or locking mechanisms'],
      ]),
      notes: 'Application state should be replaced with proper dependency injection',
    });
  }

  private initializeScriptPatterns(): void {
    // VBScript patterns
    this.vbScriptPatterns.set('Response\\.Write', 'await Response.WriteAsync');
    this.vbScriptPatterns.set('Request\\.Form\\("([^"]*)"\\)', 'Request.Form["$1"]');
    this.vbScriptPatterns.set('Request\\.QueryString\\("([^"]*)"\\)', 'Request.Query["$1"]');
    this.vbScriptPatterns.set('Session\\("([^"]*)"\\)', 'HttpContext.Session.GetString("$1")');
    this.vbScriptPatterns.set('Server\\.MapPath\\("([^"]*)"\\)', 'Path.Combine(Environment.ContentRootPath, "$1")');

    // JScript patterns
    this.jScriptPatterns.set('Response\\.Write', 'await Response.WriteAsync');
    this.jScriptPatterns.set('Request\\.Form\\("([^"]*)"\\)', 'Request.Form["$1"]');
    this.jScriptPatterns.set('Request\\.QueryString\\("([^"]*)"\\)', 'Request.Query["$1"]');
    this.jScriptPatterns.set('Session\\("([^"]*)"\\)', 'HttpContext.Session.GetString("$1")');
    this.jScriptPatterns.set('Server\\.MapPath\\("([^"]*)"\\)', 'Path.Combine(Environment.ContentRootPath, "$1")');
  }

  // ========================================================================
  // OVERRIDE METHODS
  // ========================================================================

  protected generateMigrationNotes(
    sourceAST: ZeroCopyASTNode,
    targetAST: ZeroCopyASTNode,
    options?: TranslationOptions,
  ): MigrationNote[] {
    const notes = super.generateMigrationNotes(sourceAST, targetAST, options);

    // Add ASP-specific migration notes
    notes.push({
      title: 'ASP.NET Core Migration',
      description: 'Classic ASP has been converted to ASP.NET Core MVC',
      category: 'manual_review',
      priority: 'high',
      recommendations: [
        'Review all database connections and update connection strings',
        'Test session state functionality thoroughly',
        'Verify file path operations work correctly',
        'Update any COM component usage',
        'Review security and authentication mechanisms',
        'Test error handling and logging',
      ],
    });

    notes.push({
      title: 'Async/Await Pattern',
      description: 'Response.Write calls have been converted to async Response.WriteAsync',
      category: 'manual_review',
      priority: 'medium',
      recommendations: [
        'Ensure all controller actions are marked as async',
        'Review exception handling in async contexts',
        'Test performance under load',
      ],
    });

    return notes;
  }

  protected countFunctions(code: string): number {
    const functionMatches = code.match(/public\s+\w+\s+\w+\s*\(/g);
    return functionMatches ? functionMatches.length : 0;
  }

  protected countClasses(code: string): number {
    const classMatches = code.match(/public\\s+class\\s+\\w+/g);
    return classMatches ? classMatches.length : 0;
  }
}

// ============================================================================
// SUPPORTING CLASSES AND INTERFACES
// ============================================================================

/**
 * ASP object mapping definition
 */
interface AspObjectMapping {
    csharpEquivalent: string;
    properties: Map<string, string>;
    methods: Map<string, string>;
    notes: string;
}

/**
 * C# code builder helper class
 */
class CSharpCodeBuilder {
  private code: string[] = [];
  private indentLevel = 0;
  private options: TranslationOptions;

  constructor(options?: TranslationOptions) {
    this.options = options || DEFAULT_TRANSLATION_OPTIONS;
  }

  addUsings(usings: string[]): void {
    usings.forEach(using => {
      this.code.push(`using ${using};`);
    });
    this.code.push('');
  }

  addNamespace(namespace: string): void {
    this.code.push(`namespace ${namespace}`);
    this.code.push('{');
    this.indentLevel++;
  }

  addController(name: string): void {
    // Controller will be added via startClass
  }

  startClass(className: string, baseClass?: string): void {
    const inheritance = baseClass ? ` : ${baseClass}` : '';
    this.addLine(`public class ${className}${inheritance}`);
    this.addLine('{');
    this.indentLevel++;
  }

  endClass(): void {
    this.indentLevel--;
    this.addLine('}');
  }

  addField(field: string): void {
    this.addLine(field);
    this.addLine('');
  }

  addConstructor(className: string, parameters: string[], body: string[]): void {
    const params = parameters.join(', ');
    this.addLine(`public ${className}(${params})`);
    this.addLine('{');
    this.indentLevel++;
    body.forEach(stmt => this.addStatement(stmt));
    this.indentLevel--;
    this.addLine('}');
    this.addLine('');
  }

  startMethod(name: string, returnType: string, parameters: string[], modifiers = 'public'): void {
    const params = parameters.join(', ');
    this.addLine(`${modifiers} ${returnType} ${name}(${params})`);
    this.addLine('{');
    this.indentLevel++;
  }

  endMethod(): void {
    this.indentLevel--;
    this.addLine('}');
    this.addLine('');
  }

  addStatement(statement: string): void {
    this.addLine(statement);
  }

  private addLine(line: string): void {
    const indent = this.getIndent();
    this.code.push(indent + line);
  }

  private getIndent(): string {
    const indentChar = this.options.codeStyle?.indentation === 'tabs' ? '\\t' : ' ';
    const indentSize = this.options.codeStyle?.indentSize || 4;
    const indentUnit = this.options.codeStyle?.indentation === 'tabs' ? indentChar : indentChar.repeat(indentSize);
    return indentUnit.repeat(this.indentLevel);
  }

  toString(): string {
    // Close namespace
    this.indentLevel--;
    this.addLine('}');

    return this.code.join(this.options.codeStyle?.lineEnding === 'lf' ? '\\n' : '\\r\\n');
  }
}

