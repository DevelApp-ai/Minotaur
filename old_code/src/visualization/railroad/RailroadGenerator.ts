/**
 * Main Railroad Diagram Generator
 *
 * This module orchestrates all components to generate complete railroad diagrams
 * from Minotaur grammars, including analysis, layout, rendering, and export.
 */

import { Grammar } from '../../core/grammar/Grammar';
import { GrammarAnalyzer, AnalyzedGrammar, AnalysisOptions } from './GrammarAnalyzer';
import { LayoutEngine } from './LayoutEngine';
import { SVGRenderer, SVGRenderOptions } from './SVGRenderer';
import { ThemeManager } from './ThemeManager';
import {
  RailroadDiagram,
  RailroadElement,
  RailroadConnection,
  RailroadLayout,
  RailroadTheme,
  RailroadGenerationOptions,
  RailroadExportResult,
  RailroadMetrics,
  ValidationResult,
  Point,
  Rectangle,
} from './RailroadTypes';

/**
 * Generation result with comprehensive information
 */
export interface GenerationResult {
    diagram: RailroadDiagram;
    exportResult: RailroadExportResult;
    metrics: RailroadMetrics;
    validation: ValidationResult;
}

/**
 * Main Railroad Diagram Generator
 */
export class RailroadGenerator {
  private grammarAnalyzer: GrammarAnalyzer;
  private layoutEngine: LayoutEngine;
  private svgRenderer: SVGRenderer;
  private themeManager: ThemeManager;
  private generationCache: Map<string, GenerationResult>;

  constructor() {
    this.grammarAnalyzer = new GrammarAnalyzer();
    this.themeManager = new ThemeManager();

    // Initialize with default layout and theme
    const defaultLayout = this.createDefaultLayout();
    const defaultTheme = this.themeManager.getCurrentTheme();

    this.layoutEngine = new LayoutEngine(defaultLayout);
    this.svgRenderer = new SVGRenderer(defaultTheme);
    this.generationCache = new Map();
  }

  /**
     * Generate a complete railroad diagram from a grammar
     */
  public async generateDiagram(
    grammar: Grammar,
    options: RailroadGenerationOptions = {},
  ): Promise<GenerationResult> {
    const startTime = Date.now();

    // Check cache first
    const cacheKey = this.generateCacheKey(grammar, options);
    if (this.generationCache.has(cacheKey)) {
      return this.generationCache.get(cacheKey)!;
    }

    try {
      // Step 1: Analyze grammar
      const analysisOptions: AnalysisOptions = {
        includeInheritance: options.includeInheritance ?? true,
        resolveContextSensitive: options.includeContextSensitive ?? true,
        includeMetadata: options.includeMetadata ?? true,
        optimizeForVisualization: options.optimizeForReadability ?? true,
        simplifyComplexRules: options.simplifyComplexRules ?? false,
        maxInheritanceDepth: options.maxDepth ?? 10,
      };

      const analyzedGrammar = await this.grammarAnalyzer.analyzeGrammar(grammar, analysisOptions);

      // Step 2: Convert to railroad elements
      const elements = this.convertToRailroadElements(analyzedGrammar, options);

      // Step 3: Create diagram structure
      const diagram = this.createDiagramStructure(analyzedGrammar, elements, options);

      // Step 4: Calculate layout
      const layoutResult = this.layoutEngine.calculateLayout(diagram);

      // Step 5: Update diagram with layout
      this.updateDiagramWithLayout(diagram, layoutResult);

      // Step 6: Render to SVG
      const renderOptions: SVGRenderOptions = {
        includeStyles: options.embedStyles ?? true,
        includeInteractivity: options.includeInteractivity ?? true,
        includeAnimations: true,
        optimizeOutput: options.optimizeForSize ?? true,
        includeDebugInfo: options.showDebugInfo ?? false,
      };

      const exportResult = this.svgRenderer.renderDiagram(diagram);

      // Step 7: Calculate metrics
      const metrics = this.calculateMetrics(diagram, layoutResult, startTime);

      // Step 8: Validate result
      const validation = this.validateDiagram(diagram);

      const result: GenerationResult = {
        diagram,
        exportResult,
        metrics,
        validation,
      };

      // Cache result
      this.generationCache.set(cacheKey, result);

      return result;

    } catch (error) {
      throw new Error(`Failed to generate railroad diagram: ${error}`);
    }
  }

  /**
     * Convert analyzed grammar to railroad elements
     */
  private convertToRailroadElements(
    analyzedGrammar: AnalyzedGrammar,
    options: RailroadGenerationOptions,
  ): RailroadElement[] {
    const elements: RailroadElement[] = [];
    let elementId = 0;

    // Add start element
    elements.push(this.createElement(
      `start_${elementId++}`,
      'start',
      'START',
      undefined,
      { grammarRule: analyzedGrammar.startRule },
    ));

    // Convert grammar rules to elements
    for (const [ruleName, rule] of analyzedGrammar.rules) {
      const element = this.convertRuleToElement(rule, elementId++, analyzedGrammar);
      elements.push(element);
    }

    // Add end element
    elements.push(this.createElement(
      `end_${elementId++}`,
      'end',
      'END',
      undefined,
      { grammarRule: analyzedGrammar.startRule },
    ));

    return elements;
  }

  /**
     * Convert a single rule to railroad element
     */
  private convertRuleToElement(
    rule: any,
    elementId: number,
    analyzedGrammar: AnalyzedGrammar,
    visited: Set<any> = new Set(),
  ): RailroadElement {
    // Prevent infinite recursion by tracking visited rules
    if (visited.has(rule)) {
      // Return a reference element to break the cycle
      return this.createElement(
        `ref_${elementId}`,
        'reference',
        rule.name || 'circular_ref',
        `Reference to ${rule.name || 'rule'}`,
        {
          isReference: true,
          originalRule: rule.name,
        },
      );
    }
    visited.add(rule);

    const element = this.createElement(
      `element_${elementId}`,
      rule.type,
      rule.name,
      rule.content,
      {
        grammarRule: rule.name,
        inherited: rule.metadata?.inherited,
        overridden: rule.metadata?.overridden,
        contextSensitive: rule.metadata?.contextSensitive,
        formatSpecific: rule.metadata?.formatSpecific,
        sourceGrammar: rule.metadata?.sourceGrammar,
        confidence: rule.metadata?.confidence,
        complexity: this.calculateElementComplexity(rule),
      },
    );

    // Convert children recursively
    if (rule.children) {
      element.children = rule.children.map((child: any, index: number) =>
        this.convertRuleToElement(child, elementId * 100 + index, analyzedGrammar, visited),
      );

      // Set parent references
      element.children.forEach(child => {
        child.parent = element;
      });
    }

    // Remove from visited set after processing (to allow legitimate re-use in different branches)
    visited.delete(rule);

    return element;
  }

  /**
     * Create a railroad element
     */
  private createElement(
    id: string,
    type: any,
    name?: string,
    content?: string,
    metadata: any = {},
  ): RailroadElement {
    return {
      id,
      type,
      name,
      content,
      parent: undefined,
      children: [],
      bounds: { x: 0, y: 0, width: 0, height: 0 },
      style: this.getElementStyle(type, metadata),
      entryPoint: { x: 0, y: 0 },
      exitPoint: { x: 0, y: 0 },
      connectionPoints: [],
      metadata,
      interactive: true,
      selectable: true,
      hoverable: true,
      state: {
        visible: true,
        selected: false,
        highlighted: false,
        collapsed: false,
      },
    };
  }

  /**
     * Get style for element based on type and metadata
     */
  private getElementStyle(type: string, metadata: any): any {
    const theme = this.themeManager.getCurrentTheme();
    let style = { ...theme.elements[type as keyof typeof theme.elements] };

    // Apply Minotaur-specific styles
    if (metadata.inherited) {
      style = { ...style, ...theme.grammarForge.inherited };
    }
    if (metadata.overridden) {
      style = { ...style, ...theme.grammarForge.overridden };
    }
    if (metadata.contextSensitive) {
      style = { ...style, ...theme.grammarForge.contextSensitive };
    }
    if (metadata.grammarRule && !metadata.inherited && !metadata.overridden) {
      style = { ...style, ...theme.grammarForge.newRule };
    }

    return style;
  }

  /**
     * Create diagram structure
     */
  private createDiagramStructure(
    analyzedGrammar: AnalyzedGrammar,
    elements: RailroadElement[],
    options: RailroadGenerationOptions,
  ): RailroadDiagram {
    return {
      id: `diagram_${Date.now()}`,
      name: analyzedGrammar.name,
      description: `Railroad diagram for ${analyzedGrammar.name} grammar`,
      elements,
      connections: [], // Will be populated by layout engine
      bounds: { x: 0, y: 0, width: 0, height: 0 }, // Will be calculated by layout engine
      layout: this.createLayoutFromOptions(options),
      theme: this.getThemeFromOptions(options),
      metadata: {
        grammarName: analyzedGrammar.name,
        grammarFormat: analyzedGrammar.format,
        generatedAt: new Date(),
        version: '1.0.0',
        inheritanceDepth: analyzedGrammar.inheritance.baseGrammars.length,
        totalRules: analyzedGrammar.statistics.totalRules,
        complexity: analyzedGrammar.statistics.complexity,
      },
      config: {
        interactive: options.includeInteractivity ?? true,
        zoomable: true,
        pannable: true,
        exportable: true,
      },
    };
  }

  /**
     * Create layout configuration from options
     */
  private createLayoutFromOptions(options: RailroadGenerationOptions): RailroadLayout {
    const defaultLayout = this.createDefaultLayout();

    if (options.layout) {
      return { ...defaultLayout, ...options.layout };
    }

    return defaultLayout;
  }

  /**
     * Create default layout configuration
     */
  private createDefaultLayout(): RailroadLayout {
    return {
      horizontalSpacing: 40,
      verticalSpacing: 30,
      elementSpacing: 20,
      minElementWidth: 60,
      minElementHeight: 30,
      maxElementWidth: 200,
      textAlignment: 'center',
      verticalAlignment: 'middle',
      direction: 'horizontal',
      choiceLayout: 'branched',
      repetitionStyle: 'loop',
      lineRouting: 'curved',
      optimizeLayout: true,
      compactMode: false,
    };
  }

  /**
     * Get theme from options
     */
  private getThemeFromOptions(options: RailroadGenerationOptions): RailroadTheme {
    if (typeof options.theme === 'string') {
      return this.themeManager.getTheme(options.theme) || this.themeManager.getCurrentTheme();
    } else if (options.theme) {
      return options.theme;
    }

    return this.themeManager.getCurrentTheme();
  }

  /**
     * Update diagram with layout results
     */
  private updateDiagramWithLayout(diagram: RailroadDiagram, layoutResult: any): void {
    // Update element positions
    for (const element of diagram.elements) {
      const bounds = layoutResult.elements.get(element.id);
      if (bounds) {
        element.bounds = bounds;
        element.entryPoint = { x: bounds.x, y: bounds.y + bounds.height / 2 };
        element.exitPoint = { x: bounds.x + bounds.width, y: bounds.y + bounds.height / 2 };
      }
    }

    // Update connections
    diagram.connections = layoutResult.connections;

    // Update total bounds
    diagram.bounds = layoutResult.totalBounds;
  }

  /**
     * Calculate comprehensive metrics
     */
  private calculateMetrics(
    diagram: RailroadDiagram,
    layoutResult: any,
    startTime: number,
  ): RailroadMetrics {
    return {
      generation: {
        analysisTime: 0, // Would be tracked separately
        layoutTime: layoutResult.metrics.layoutTime,
        renderTime: 0, // Would be tracked separately
        totalTime: Date.now() - startTime,
      },

      diagram: {
        elementCount: diagram.elements.length,
        connectionCount: diagram.connections.length,
        maxDepth: layoutResult.metrics.maxDepth,
        complexity: this.calculateDiagramComplexity(diagram),
      },

      memory: {
        peakUsage: 0, // Would require memory monitoring
        currentUsage: 0,
      },

      performance: {
        fps: undefined,
        renderTime: undefined,
        interactionLatency: undefined,
      },
    };
  }

  /**
     * Calculate diagram complexity
     */
  private calculateDiagramComplexity(diagram: RailroadDiagram): number {
    let complexity = 0;

    for (const element of diagram.elements) {
      complexity += this.calculateElementComplexity(element);
    }

    // Add connection complexity
    complexity += diagram.connections.length * 0.5;

    return complexity;
  }

  /**
     * Calculate element complexity
     */
  private calculateElementComplexity(element: any): number {
    let complexity = 1;

    // Base complexity by type
    switch (element.type) {
      case 'choice':
        complexity += (element.children?.length || 0) * 2;
        break;
      case 'repetition':
        complexity += 3;
        break;
      case 'optional':
        complexity += 1;
        break;
      case 'sequence':
        complexity += (element.children?.length || 0) * 0.5;
        break;
    }

    // Add children complexity
    if (element.children) {
      for (const child of element.children) {
        complexity += this.calculateElementComplexity(child);
      }
    }

    return complexity;
  }

  /**
     * Validate diagram
     */
  private validateDiagram(diagram: RailroadDiagram): ValidationResult {
    const errors: any[] = [];

    // Check for required elements
    if (diagram.elements.length === 0) {
      errors.push({
        type: 'error',
        message: 'Diagram has no elements',
        location: { x: 0, y: 0 },
      });
    }

    // Check for disconnected elements
    const connectedElements = new Set<string>();
    for (const connection of diagram.connections) {
      connectedElements.add(connection.from.element.id);
      connectedElements.add(connection.to.element.id);
    }

    for (const element of diagram.elements) {
      if (!connectedElements.has(element.id) && element.type !== 'start' && element.type !== 'end') {
        errors.push({
          type: 'warning',
          message: `Element ${element.id} is not connected`,
          element,
          location: element.bounds,
        });
      }
    }

    // Check for overlapping elements
    for (let i = 0; i < diagram.elements.length; i++) {
      for (let j = i + 1; j < diagram.elements.length; j++) {
        const elem1 = diagram.elements[i];
        const elem2 = diagram.elements[j];

        if (this.elementsOverlap(elem1.bounds, elem2.bounds)) {
          errors.push({
            type: 'warning',
            message: `Elements ${elem1.id} and ${elem2.id} overlap`,
            location: elem1.bounds,
          });
        }
      }
    }

    return {
      valid: errors.filter(e => e.type === 'error').length === 0,
      errors,
      metrics: this.calculateMetrics(diagram, { metrics: { layoutTime: 0, maxDepth: 0 } }, Date.now()),
    };
  }

  /**
     * Check if two rectangles overlap
     */
  private elementsOverlap(rect1: Rectangle, rect2: Rectangle): boolean {
    return !(rect1.x + rect1.width < rect2.x ||
                rect2.x + rect2.width < rect1.x ||
                rect1.y + rect1.height < rect2.y ||
                rect2.y + rect2.height < rect1.y);
  }

  /**
     * Generate cache key
     */
  private generateCacheKey(grammar: Grammar, options: RailroadGenerationOptions): string {
    const grammarHash = this.hashString(grammar.content || '');
    const optionsHash = this.hashString(JSON.stringify(options));
    return `${grammar.name || 'unknown'}_${grammarHash}_${optionsHash}`;
  }

  /**
     * Simple string hashing function
     */
  private hashString(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash.toString(36);
  }

  /**
     * Generate multiple diagrams for different rules
     */
  public async generateMultipleDiagrams(
    grammar: Grammar,
    ruleNames: string[],
    options: RailroadGenerationOptions = {},
  ): Promise<Map<string, GenerationResult>> {
    const results = new Map<string, GenerationResult>();

    for (const ruleName of ruleNames) {
      try {
        // Create a focused grammar for this rule
        const focusedGrammar = this.createFocusedGrammar(grammar, ruleName);
        const result = await this.generateDiagram(focusedGrammar, options);
        results.set(ruleName, result);
      } catch (error) {
    // eslint-disable-next-line no-console
        console.error(`Failed to generate diagram for rule ${ruleName}:`, error);
      }
    }

    return results;
  }

  /**
     * Create a focused grammar for a specific rule
     */
  private createFocusedGrammar(grammar: Grammar, ruleName: string): Grammar {
    // This would extract just the specified rule and its dependencies
    // For now, return the original grammar
    return grammar;
  }

  /**
     * Export diagram in different formats
     */
  public async exportDiagram(
    diagram: RailroadDiagram,
    format: 'svg' | 'png' | 'pdf' | 'html',
    options: any = {},
  ): Promise<RailroadExportResult> {
    switch (format) {
      case 'svg':
        return this.svgRenderer.renderDiagram(diagram);
      case 'png':
        return this.exportToPNG(diagram);
      case 'pdf':
        return this.exportToPDF(diagram);
      case 'html':
        return this.exportToHTML(diagram, options);
      default:
        throw new Error(`Unsupported export format: ${format}`);
    }
  }

  /**
     * Export to PNG (basic implementation)
     */
  private async exportToPNG(diagram: RailroadDiagram): Promise<RailroadExportResult> {
    // Basic implementation: return error result indicating PNG export needs additional dependencies
    // Note: Real implementation would use canvas or puppeteer to generate PNG
    const errorContent = 'PNG export requires additional dependencies (canvas or puppeteer) not currently installed';

    return {
      content: errorContent,
      format: 'png',
      metadata: {
        size: {
          width: 800,
          height: 600,
        },
        elementCount: diagram.elements.length,
        connectionCount: diagram.connections.length,
        generationTime: Date.now(),
        format: 'png',
        created: new Date().toISOString(),
        generator: 'Minotaur Railroad Generator',
        error: errorContent,
      },
    };
  }

  /**
     * Export to PDF (basic implementation)
     */
  private async exportToPDF(diagram: RailroadDiagram): Promise<RailroadExportResult> {
    // Basic implementation: return error result indicating PDF export needs additional dependencies
    // Note: Real implementation would use jsPDF or puppeteer to generate PDF
    const errorContent = 'PDF export requires additional dependencies (jsPDF or puppeteer) not currently installed';

    return {
      content: errorContent,
      format: 'pdf',
      metadata: {
        size: {
          width: 800,
          height: 600,
        },
        elementCount: diagram.elements.length,
        connectionCount: diagram.connections.length,
        generationTime: Date.now(),
        format: 'pdf',
        created: new Date().toISOString(),
        generator: 'Minotaur Railroad Generator',
        error: errorContent,
      },
    };
  }

  /**
     * Export to HTML (placeholder implementation)
     */
  private async exportToHTML(diagram: RailroadDiagram, options: any): Promise<RailroadExportResult> {
    const svgResult = this.svgRenderer.renderDiagram(diagram);

    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${diagram.name} - Railroad Diagram</title>
    <style>
        body { margin: 0; padding: 20px; font-family: Arial, sans-serif; }
        .diagram-container { text-align: center; }
        .diagram-title { font-size: 24px; margin-bottom: 20px; }
        .diagram-metadata { margin-top: 20px; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class="diagram-container">
        <h1 class="diagram-title">${diagram.name}</h1>
        ${svgResult.content}
        <div class="diagram-metadata">
            Generated on ${diagram.metadata.generatedAt}<br>
            Grammar: ${diagram.metadata.grammarName} (${diagram.metadata.grammarFormat})<br>
            Elements: ${diagram.elements.length}, Connections: ${diagram.connections.length}
        </div>
    </div>
</body>
</html>`;

    return {
      content: html,
      format: 'html',
      metadata: svgResult.metadata,
    };
  }

  /**
     * Update theme
     */
  public updateTheme(themeName: string): boolean {
    if (this.themeManager.setCurrentTheme(themeName)) {
      this.svgRenderer.updateTheme(this.themeManager.getCurrentTheme());
      this.clearCache();
      return true;
    }
    return false;
  }

  /**
     * Update layout
     */
  public updateLayout(layout: Partial<RailroadLayout>): void {
    this.layoutEngine.updateLayout(layout);
    this.clearCache();
  }

  /**
     * Clear generation cache
     */
  public clearCache(): void {
    this.generationCache.clear();
    this.grammarAnalyzer.clearCache();
    this.layoutEngine.clearCache();
  }

  /**
     * Get generation statistics
     */
  public getStats(): {
        cacheSize: number;
        availableThemes: string[];
        currentTheme: string;
        } {
    return {
      cacheSize: this.generationCache.size,
      availableThemes: this.themeManager.getAvailableThemes(),
      currentTheme: this.themeManager.getCurrentTheme().name,
    };
  }
}

