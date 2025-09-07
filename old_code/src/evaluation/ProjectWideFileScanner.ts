/**
 * ProjectWideFileScanner - Multi-File Project Analysis
 *
 * Provides comprehensive project-wide file discovery, analysis, and dependency
 * tracking for Project Golem's multi-file correction capabilities.
 *
 * Features:
 * - Recursive file discovery with intelligent filtering
 * - File type detection and language identification
 * - Dependency graph analysis and cross-file relationships
 * - Error message parsing and file:line resolution
 * - Pattern matching and fuzzy file name resolution
 */

import * as fs from 'fs/promises';
import * as path from 'path';
import { glob } from 'glob';

export interface ProjectFile {
  absolutePath: string;
  relativePath: string;
  fileName: string;
  extension: string;
  language: string;
  size: number;
  lastModified: Date;
  content?: string;
  dependencies?: string[];
  exports?: string[];
  imports?: string[];
}

export interface FileReference {
  file: string;
  line?: number;
  column?: number;
  function?: string;
  class?: string;
  variable?: string;
  context?: string;
}

export interface ProjectStructure {
  rootPath: string;
  totalFiles: number;
  filesByLanguage: Map<string, ProjectFile[]>;
  filesByExtension: Map<string, ProjectFile[]>;
  dependencyGraph: Map<string, string[]>;
  exportMap: Map<string, string[]>;
  importMap: Map<string, string[]>;
}

export interface ScanOptions {
  // File filtering
  includePatterns: string[];
  excludePatterns: string[];
  maxFileSize: number;
  followSymlinks: boolean;

  // Content analysis
  loadFileContent: boolean;
  analyzeDependencies: boolean;
  extractExports: boolean;
  extractImports: boolean;

  // Performance
  maxFiles: number;
  parallelProcessing: boolean;
  cacheResults: boolean;
}

export interface FileSearchResult {
  matches: ProjectFile[];
  confidence: number;
  searchMethod: 'exact' | 'fuzzy' | 'pattern' | 'content';
  executionTime: number;
}

/**
 * ProjectWideFileScanner - Main scanner class
 */
export class ProjectWideFileScanner {
  private cache: Map<string, ProjectStructure> = new Map();
  private languagePatterns = new Map([
    ['typescript', ['.ts', '.tsx']],
    ['javascript', ['.js', '.jsx', '.mjs']],
    ['python', ['.py', '.pyw', '.pyi']],
    ['java', ['.java']],
    ['csharp', ['.cs']],
    ['cpp', ['.cpp', '.cxx', '.cc', '.c++', '.hpp', '.hxx', '.h++']],
    ['c', ['.c', '.h']],
    ['rust', ['.rs']],
    ['go', ['.go']],
    ['php', ['.php']],
    ['ruby', ['.rb']],
    ['swift', ['.swift']],
    ['kotlin', ['.kt', '.kts']],
    ['scala', ['.scala']],
    ['dart', ['.dart']],
    ['html', ['.html', '.htm']],
    ['css', ['.css', '.scss', '.sass', '.less']],
    ['json', ['.json']],
    ['yaml', ['.yaml', '.yml']],
    ['xml', ['.xml']],
    ['markdown', ['.md', '.markdown']],
    ['shell', ['.sh', '.bash', '.zsh', '.fish']],
    ['sql', ['.sql']],
    ['dockerfile', ['Dockerfile', '.dockerfile']],
    ['makefile', ['Makefile', 'makefile', '.mk']],
  ]);

  private defaultOptions: ScanOptions = {
    includePatterns: ['**/*'],
    excludePatterns: [
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      '**/target/**',
      '**/.git/**',
      '**/.svn/**',
      '**/.hg/**',
      '**/coverage/**',
      '**/__pycache__/**',
      '**/*.pyc',
      '**/*.pyo',
      '**/*.class',
      '**/*.o',
      '**/*.so',
      '**/*.dylib',
      '**/*.dll',
      '**/.DS_Store',
      '**/Thumbs.db',
    ],
    maxFileSize: 10 * 1024 * 1024, // 10MB
    followSymlinks: false,
    loadFileContent: false,
    analyzeDependencies: true,
    extractExports: true,
    extractImports: true,
    maxFiles: 10000,
    parallelProcessing: true,
    cacheResults: true,
  };

  constructor() {}

  /**
   * Scan project directory and build comprehensive structure
   */
  async scanProject(
    projectPath: string,
    options: Partial<ScanOptions> = {},
  ): Promise<ProjectStructure> {

    const fullOptions = { ...this.defaultOptions, ...options };
    const absoluteProjectPath = path.resolve(projectPath);

    // Check cache first
    const cacheKey = this.createCacheKey(absoluteProjectPath, fullOptions);
    if (fullOptions.cacheResults && this.cache.has(cacheKey)) {
      console.log(`📋 Using cached project structure for ${projectPath}`);
      return this.cache.get(cacheKey)!;
    }

    console.log(`🔍 Scanning project: ${projectPath}`);
    const startTime = Date.now();

    try {
      // Validate project path
      await this.validateProjectPath(absoluteProjectPath);

      // Discover files
      const files = await this.discoverFiles(absoluteProjectPath, fullOptions);
      console.log(`📁 Discovered ${files.length} files`);

      // Analyze files
      const analyzedFiles = await this.analyzeFiles(files, fullOptions);
      console.log(`🔬 Analyzed ${analyzedFiles.length} files`);

      // Build project structure
      const structure = await this.buildProjectStructure(
        absoluteProjectPath,
        analyzedFiles,
        fullOptions,
      );

      const scanTime = Date.now() - startTime;
      console.log(`✅ Project scan complete in ${scanTime}ms`);

      // Cache results
      if (fullOptions.cacheResults) {
        this.cache.set(cacheKey, structure);
      }

      return structure;

    } catch (error) {
      console.error(`❌ Project scan failed: ${error instanceof Error ? error.message : String(error)}`);
      throw error;
    }
  }

  /**
   * Search for files by various criteria
   */
  async searchFiles(
    projectStructure: ProjectStructure,
    query: string,
    searchType: 'name' | 'path' | 'content' | 'function' | 'class' | 'variable' = 'name',
  ): Promise<FileSearchResult> {

    const startTime = Date.now();
    let matches: ProjectFile[] = [];
    let searchMethod: FileSearchResult['searchMethod'] = 'exact';

    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();

    switch (searchType) {
      case 'name':
        matches = await this.searchByFileName(allFiles, query);
        searchMethod = matches.length > 0 ? 'exact' : 'fuzzy';
        if (matches.length === 0) {
          matches = await this.fuzzySearchByFileName(allFiles, query);
        }
        break;

      case 'path':
        matches = await this.searchByFilePath(allFiles, query);
        searchMethod = 'pattern';
        break;

      case 'content':
        matches = await this.searchByContent(allFiles, query);
        searchMethod = 'content';
        break;

      case 'function':
        matches = await this.searchByFunction(allFiles, query);
        searchMethod = 'content';
        break;

      case 'class':
        matches = await this.searchByClass(allFiles, query);
        searchMethod = 'content';
        break;

      case 'variable':
        matches = await this.searchByVariable(allFiles, query);
        searchMethod = 'content';
        break;
    }

    const executionTime = Date.now() - startTime;
    const confidence = this.calculateSearchConfidence(matches, query, searchMethod);

    return {
      matches,
      confidence,
      searchMethod,
      executionTime,
    };
  }

  /**
   * Resolve file references from error messages or prompts
   */
  async resolveFileReferences(
    projectStructure: ProjectStructure,
    references: string[],
  ): Promise<Map<string, FileReference[]>> {

    const resolvedReferences = new Map<string, FileReference[]>();

    for (const reference of references) {
      const parsed = await this.parseFileReference(reference);
      const resolved = await this.resolveReference(projectStructure, parsed);

      if (resolved.length > 0) {
        resolvedReferences.set(reference, resolved);
      }
    }

    return resolvedReferences;
  }

  /**
   * Parse file reference from text (e.g., "UserService.ts:45:12", "src/utils/helper.py line 23")
   */
  private async parseFileReference(reference: string): Promise<FileReference> {
    const patterns = [
      // TypeScript/JavaScript style: "file.ts:45:12"
      /^(.+?):(\d+):(\d+)$/,
      // Simple line reference: "file.py:45"
      /^(.+?):(\d+)$/,
      // Natural language: "file.js line 45"
      /^(.+?)\s+line\s+(\d+)$/i,
      // Natural language: "line 45 in file.js"
      /^line\s+(\d+)\s+in\s+(.+?)$/i,
      // Function reference: "function myFunc in file.js"
      /^function\s+(\w+)\s+in\s+(.+?)$/i,
      // Class reference: "class MyClass in file.py"
      /^class\s+(\w+)\s+in\s+(.+?)$/i,
      // Variable reference: "variable myVar in file.ts"
      /^variable\s+(\w+)\s+in\s+(.+?)$/i,
    ];

    for (const pattern of patterns) {
      const match = reference.match(pattern);
      if (match) {
        if (pattern.source.includes('function')) {
          return {
            file: match[2],
            function: match[1],
          };
        } else if (pattern.source.includes('class')) {
          return {
            file: match[2],
            class: match[1],
          };
        } else if (pattern.source.includes('variable')) {
          return {
            file: match[2],
            variable: match[1],
          };
        } else if (pattern.source.includes('line.*in')) {
          return {
            file: match[2],
            line: parseInt(match[1]),
          };
        } else {
          return {
            file: match[1],
            line: match[2] ? parseInt(match[2]) : undefined,
            column: match[3] ? parseInt(match[3]) : undefined,
          };
        }
      }
    }

    // Default: treat as file name
    return { file: reference };
  }

  /**
   * Resolve parsed reference to actual project files
   */
  private async resolveReference(
    projectStructure: ProjectStructure,
    reference: FileReference,
  ): Promise<FileReference[]> {

    const allFiles = Array.from(projectStructure.filesByLanguage.values()).flat();
    const resolved: FileReference[] = [];

    // Search for matching files
    const fileMatches = await this.searchByFileName(allFiles, reference.file);

    if (fileMatches.length === 0) {
      // Try fuzzy search
      const fuzzyMatches = await this.fuzzySearchByFileName(allFiles, reference.file);
      fileMatches.push(...fuzzyMatches);
    }

    for (const file of fileMatches) {
      const resolvedRef: FileReference = {
        file: file.absolutePath,
        line: reference.line,
        column: reference.column,
        function: reference.function,
        class: reference.class,
        variable: reference.variable,
        context: reference.context,
      };

      // If searching for function/class/variable, verify it exists in the file
      if (reference.function || reference.class || reference.variable) {
        const exists = await this.verifySymbolInFile(file, reference);
        if (exists) {
          resolved.push(resolvedRef);
        }
      } else {
        resolved.push(resolvedRef);
      }
    }

    return resolved;
  }

  /**
   * Discover files in project directory
   */
  private async discoverFiles(
    projectPath: string,
    options: ScanOptions,
  ): Promise<string[]> {

    const files: string[] = [];

    for (const pattern of options.includePatterns) {
      const globPattern = path.join(projectPath, pattern);
      const matches = await glob(globPattern, {
        ignore: options.excludePatterns.map(p => path.join(projectPath, p)),
        follow: options.followSymlinks,
        nodir: true,
      });

      files.push(...matches);
    }

    // Remove duplicates and apply file size filter
    const uniqueFiles = [...new Set(files)];
    const filteredFiles: string[] = [];

    for (const file of uniqueFiles) {
      try {
        const stats = await fs.stat(file);
        if (stats.size <= options.maxFileSize) {
          filteredFiles.push(file);
        }
      } catch (error) {
        // Skip files that can't be accessed
        continue;
      }

      if (filteredFiles.length >= options.maxFiles) {
        break;
      }
    }

    return filteredFiles;
  }

  /**
   * Analyze discovered files
   */
  private async analyzeFiles(
    filePaths: string[],
    options: ScanOptions,
  ): Promise<ProjectFile[]> {

    const analyzedFiles: ProjectFile[] = [];

    if (options.parallelProcessing) {
      // Process files in parallel batches
      const batchSize = 50;
      const batches = this.createBatches(filePaths, batchSize);

      for (const batch of batches) {
        const batchPromises = batch.map(filePath => this.analyzeFile(filePath, options));
        const batchResults = await Promise.allSettled(batchPromises);

        for (const result of batchResults) {
          if (result.status === 'fulfilled' && result.value) {
            analyzedFiles.push(result.value);
          }
        }
      }
    } else {
      // Process files sequentially
      for (const filePath of filePaths) {
        try {
          const analyzed = await this.analyzeFile(filePath, options);
          if (analyzed) {
            analyzedFiles.push(analyzed);
          }
        } catch (error) {
          // Skip files that can't be analyzed
          continue;
        }
      }
    }

    return analyzedFiles;
  }

  /**
   * Analyze individual file
   */
  private async analyzeFile(
    filePath: string,
    options: ScanOptions,
  ): Promise<ProjectFile | null> {

    try {
      const stats = await fs.stat(filePath);
      const fileName = path.basename(filePath);
      const extension = path.extname(filePath);
      const language = this.detectLanguage(filePath);

      const projectFile: ProjectFile = {
        absolutePath: filePath,
        relativePath: filePath, // Will be updated in buildProjectStructure
        fileName,
        extension,
        language,
        size: stats.size,
        lastModified: stats.mtime,
      };

      // Load content if requested
      if (options.loadFileContent) {
        try {
          projectFile.content = await fs.readFile(filePath, 'utf-8');
        } catch (error) {
          // Skip binary files or files with encoding issues
          return null;
        }
      }

      // Analyze dependencies if requested
      if (options.analyzeDependencies && projectFile.content) {
        const analysis = await this.analyzeDependencies(projectFile.content, language);
        projectFile.dependencies = analysis.dependencies;
        projectFile.imports = analysis.imports;
        projectFile.exports = analysis.exports;
      }

      return projectFile;

    } catch (error) {
      return null;
    }
  }

  /**
   * Build comprehensive project structure
   */
  private async buildProjectStructure(
    rootPath: string,
    files: ProjectFile[],
    options: ScanOptions,
  ): Promise<ProjectStructure> {

    // Update relative paths
    for (const file of files) {
      file.relativePath = path.relative(rootPath, file.absolutePath);
    }

    // Group files by language and extension
    const filesByLanguage = new Map<string, ProjectFile[]>();
    const filesByExtension = new Map<string, ProjectFile[]>();

    for (const file of files) {
      // Group by language
      if (!filesByLanguage.has(file.language)) {
        filesByLanguage.set(file.language, []);
      }
      filesByLanguage.get(file.language)!.push(file);

      // Group by extension
      if (!filesByExtension.has(file.extension)) {
        filesByExtension.set(file.extension, []);
      }
      filesByExtension.get(file.extension)!.push(file);
    }

    // Build dependency graph
    const dependencyGraph = new Map<string, string[]>();
    const exportMap = new Map<string, string[]>();
    const importMap = new Map<string, string[]>();

    for (const file of files) {
      if (file.dependencies) {
        dependencyGraph.set(file.relativePath, file.dependencies);
      }
      if (file.exports) {
        exportMap.set(file.relativePath, file.exports);
      }
      if (file.imports) {
        importMap.set(file.relativePath, file.imports);
      }
    }

    return {
      rootPath,
      totalFiles: files.length,
      filesByLanguage,
      filesByExtension,
      dependencyGraph,
      exportMap,
      importMap,
    };
  }

  /**
   * Detect programming language from file path
   */
  private detectLanguage(filePath: string): string {
    const fileName = path.basename(filePath);
    const extension = path.extname(filePath).toLowerCase();

    // Check special file names first
    if (fileName === 'Dockerfile' || fileName.includes('.dockerfile')) {
      return 'dockerfile';
    }
    if (fileName === 'Makefile' || fileName === 'makefile' || extension === '.mk') {
      return 'makefile';
    }

    // Check by extension
    for (const [language, extensions] of this.languagePatterns) {
      if (extensions.includes(extension) || extensions.includes(fileName)) {
        return language;
      }
    }

    return 'unknown';
  }

  /**
   * Analyze file dependencies, imports, and exports
   */
  private async analyzeDependencies(
    content: string,
    language: string,
  ): Promise<{ dependencies: string[]; imports: string[]; exports: string[] }> {

    const dependencies: string[] = [];
    const imports: string[] = [];
    const exports: string[] = [];

    const lines = content.split('\n');

    switch (language) {
      case 'typescript':
      case 'javascript':
        for (const line of lines) {
          const trimmed = line.trim();

          // Import statements
          const importMatch = trimmed.match(/^import\s+.*?\s+from\s+['"]([^'"]+)['"]/);
          if (importMatch) {
            imports.push(importMatch[1]);
            if (!importMatch[1].startsWith('.')) {
              dependencies.push(importMatch[1]);
            }
          }

          // Require statements
          const requireMatch = trimmed.match(/require\(['"]([^'"]+)['"]\)/);
          if (requireMatch) {
            imports.push(requireMatch[1]);
            if (!requireMatch[1].startsWith('.')) {
              dependencies.push(requireMatch[1]);
            }
          }

          // Export statements
          if (trimmed.startsWith('export ')) {
            const exportMatch = trimmed.match(/export\s+(?:default\s+)?(?:function|class|const|let|var)\s+(\w+)/);
            if (exportMatch) {
              exports.push(exportMatch[1]);
            }
          }
        }
        break;

      case 'python':
        for (const line of lines) {
          const trimmed = line.trim();

          // Import statements
          const importMatch = trimmed.match(/^(?:from\s+(\S+)\s+)?import\s+(.+)/);
          if (importMatch) {
            const module = importMatch[1] || importMatch[2].split(',')[0].trim();
            imports.push(module);
            if (!module.startsWith('.')) {
              dependencies.push(module);
            }
          }

          // Function/class definitions (exports)
          const defMatch = trimmed.match(/^(?:def|class)\s+(\w+)/);
          if (defMatch) {
            exports.push(defMatch[1]);
          }
        }
        break;

      case 'java':
        for (const line of lines) {
          const trimmed = line.trim();

          // Import statements
          const importMatch = trimmed.match(/^import\s+(?:static\s+)?([^;]+);/);
          if (importMatch) {
            imports.push(importMatch[1]);
            const packageName = importMatch[1].split('.')[0];
            if (packageName !== 'java') {
              dependencies.push(packageName);
            }
          }

          // Public class/interface definitions
          const classMatch = trimmed.match(/^public\s+(?:class|interface)\s+(\w+)/);
          if (classMatch) {
            exports.push(classMatch[1]);
          }
        }
        break;
    }

    return {
      dependencies: [...new Set(dependencies)],
      imports: [...new Set(imports)],
      exports: [...new Set(exports)],
    };
  }

  /**
   * Search files by name (exact match)
   */
  private async searchByFileName(files: ProjectFile[], query: string): Promise<ProjectFile[]> {
    return files.filter(file =>
      file.fileName === query ||
      file.fileName.toLowerCase() === query.toLowerCase(),
    );
  }

  /**
   * Fuzzy search files by name
   */
  private async fuzzySearchByFileName(files: ProjectFile[], query: string): Promise<ProjectFile[]> {
    const matches: Array<{ file: ProjectFile; score: number }> = [];

    for (const file of files) {
      const score = this.calculateFuzzyScore(file.fileName, query);
      if (score > 0.3) { // Minimum similarity threshold
        matches.push({ file, score });
      }
    }

    // Sort by score (descending) and return top matches
    return matches
      .sort((a, b) => b.score - a.score)
      .slice(0, 10)
      .map(match => match.file);
  }

  /**
   * Search files by path pattern
   */
  private async searchByFilePath(files: ProjectFile[], pattern: string): Promise<ProjectFile[]> {
    const regex = new RegExp(pattern.replace(/\*/g, '.*'), 'i');
    return files.filter(file => regex.test(file.relativePath));
  }

  /**
   * Search files by content
   */
  private async searchByContent(files: ProjectFile[], query: string): Promise<ProjectFile[]> {
    const matches: ProjectFile[] = [];

    for (const file of files) {
      if (file.content && file.content.includes(query)) {
        matches.push(file);
      }
    }

    return matches;
  }

  /**
   * Search files by function name
   */
  private async searchByFunction(files: ProjectFile[], functionName: string): Promise<ProjectFile[]> {
    const matches: ProjectFile[] = [];

    for (const file of files) {
      if (file.content) {
        const patterns = [
          new RegExp(`function\\s+${functionName}\\s*\\(`, 'i'),
          new RegExp(`def\\s+${functionName}\\s*\\(`, 'i'),
          new RegExp(`${functionName}\\s*=\\s*function`, 'i'),
          new RegExp(`${functionName}\\s*:\\s*function`, 'i'),
        ];

        if (patterns.some(pattern => pattern.test(file.content!))) {
          matches.push(file);
        }
      }
    }

    return matches;
  }

  /**
   * Search files by class name
   */
  private async searchByClass(files: ProjectFile[], className: string): Promise<ProjectFile[]> {
    const matches: ProjectFile[] = [];

    for (const file of files) {
      if (file.content) {
        const patterns = [
          new RegExp(`class\\s+${className}\\s*[{(:]`, 'i'),
          new RegExp(`interface\\s+${className}\\s*[{]`, 'i'),
          new RegExp(`type\\s+${className}\\s*=`, 'i'),
        ];

        if (patterns.some(pattern => pattern.test(file.content!))) {
          matches.push(file);
        }
      }
    }

    return matches;
  }

  /**
   * Search files by variable name
   */
  private async searchByVariable(files: ProjectFile[], variableName: string): Promise<ProjectFile[]> {
    const matches: ProjectFile[] = [];

    for (const file of files) {
      if (file.content) {
        const patterns = [
          new RegExp(`\\b${variableName}\\s*=`, 'i'),
          new RegExp(`\\bconst\\s+${variableName}\\b`, 'i'),
          new RegExp(`\\blet\\s+${variableName}\\b`, 'i'),
          new RegExp(`\\bvar\\s+${variableName}\\b`, 'i'),
        ];

        if (patterns.some(pattern => pattern.test(file.content!))) {
          matches.push(file);
        }
      }
    }

    return matches;
  }

  /**
   * Verify symbol exists in file
   */
  private async verifySymbolInFile(file: ProjectFile, reference: FileReference): Promise<boolean> {
    if (!file.content) {
      return false;
    }

    if (reference.function) {
      return await this.searchByFunction([file], reference.function).then(matches => matches.length > 0);
    }

    if (reference.class) {
      return await this.searchByClass([file], reference.class).then(matches => matches.length > 0);
    }

    if (reference.variable) {
      return await this.searchByVariable([file], reference.variable).then(matches => matches.length > 0);
    }

    return true;
  }

  /**
   * Calculate fuzzy matching score
   */
  private calculateFuzzyScore(text: string, query: string): number {
    const textLower = text.toLowerCase();
    const queryLower = query.toLowerCase();

    // Exact match
    if (textLower === queryLower) {
      return 1.0;
    }

    // Contains match
    if (textLower.includes(queryLower)) {
      return 0.8;
    }

    // Levenshtein distance based similarity
    const distance = this.levenshteinDistance(textLower, queryLower);
    const maxLength = Math.max(text.length, query.length);

    return Math.max(0, 1 - distance / maxLength);
  }

  /**
   * Calculate Levenshtein distance
   */
  private levenshteinDistance(str1: string, str2: string): number {
    const matrix = Array(str2.length + 1).fill(null).map(() => Array(str1.length + 1).fill(null));

    for (let i = 0; i <= str1.length; i++) {
      matrix[0][i] = i;
    }

    for (let j = 0; j <= str2.length; j++) {
      matrix[j][0] = j;
    }

    for (let j = 1; j <= str2.length; j++) {
      for (let i = 1; i <= str1.length; i++) {
        const indicator = str1[i - 1] === str2[j - 1] ? 0 : 1;
        matrix[j][i] = Math.min(
          matrix[j][i - 1] + 1,
          matrix[j - 1][i] + 1,
          matrix[j - 1][i - 1] + indicator,
        );
      }
    }

    return matrix[str2.length][str1.length];
  }

  /**
   * Calculate search confidence
   */
  private calculateSearchConfidence(
    matches: ProjectFile[],
    query: string,
    method: FileSearchResult['searchMethod'],
  ): number {

    if (matches.length === 0) {
      return 0;
    }

    let baseConfidence = 0.5;

    switch (method) {
      case 'exact':
        baseConfidence = 0.95;
        break;
      case 'fuzzy':
        baseConfidence = 0.7;
        break;
      case 'pattern':
        baseConfidence = 0.8;
        break;
      case 'content':
        baseConfidence = 0.6;
        break;
    }

    // Adjust based on number of matches
    if (matches.length === 1) {
      baseConfidence += 0.05;
    } else if (matches.length > 10) {
      baseConfidence -= 0.1;
    }

    return Math.min(baseConfidence, 1.0);
  }

  /**
   * Create batches for parallel processing
   */
  private createBatches<T>(items: T[], batchSize: number): T[][] {
    const batches: T[][] = [];
    for (let i = 0; i < items.length; i += batchSize) {
      batches.push(items.slice(i, i + batchSize));
    }
    return batches;
  }

  /**
   * Create cache key
   */
  private createCacheKey(projectPath: string, options: ScanOptions): string {
    const optionsHash = JSON.stringify(options);
    return `${projectPath}:${this.simpleHash(optionsHash)}`;
  }

  /**
   * Simple hash function
   */
  private simpleHash(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash;
    }
    return Math.abs(hash).toString(36);
  }

  /**
   * Validate project path
   */
  private async validateProjectPath(projectPath: string): Promise<void> {
    try {
      const stats = await fs.stat(projectPath);
      if (!stats.isDirectory()) {
        throw new Error('Project path must be a directory');
      }
    } catch (error) {
      if (error instanceof Error && error.message.includes('ENOENT')) {
        throw new Error('Project path does not exist');
      }
      throw error;
    }
  }

  /**
   * Clear cache
   */
  clearCache(): void {
    this.cache.clear();
  }

  /**
   * Get cache statistics
   */
  getCacheStatistics(): { cacheSize: number; cachedProjects: string[] } {
    return {
      cacheSize: this.cache.size,
      cachedProjects: Array.from(this.cache.keys()),
    };
  }
}

