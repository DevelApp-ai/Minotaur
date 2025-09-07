/**
 * Minotaur-Specific Extensions for Railroad Diagram Visualization
 *
 * This module provides specialized features that showcase Minotaur's unique
 * capabilities including inheritance visualization, context-sensitive parsing
 * representation, and multi-language grammar composition.
 */

import {
  RailroadDiagram,
  RailroadElement,
  RailroadConnection,
  RailroadTheme,
  Point,
  Rectangle,
} from './RailroadTypes';
import { AnalyzedGrammar } from './GrammarAnalyzer';

/**
 * Inheritance visualization configuration
 */
export interface InheritanceVisualization {
    showInheritanceTree: boolean;
    highlightInheritedRules: boolean;
    showOverrideIndicators: boolean;
    displayInheritanceDepth: boolean;
    groupByInheritanceLevel: boolean;
    showInheritanceConnections: boolean;
}

/**
 * Context-sensitive visualization configuration
 */
export interface ContextSensitiveVisualization {
    showContextBoundaries: boolean;
    highlightContextSwitches: boolean;
    displaySymbolTable: boolean;
    showScopeHierarchy: boolean;
    visualizeContextFlow: boolean;
    showContextDependencies: boolean;
}

/**
 * Multi-language composition visualization
 */
export interface MultiLanguageVisualization {
    showLanguageBoundaries: boolean;
    highlightEmbeddedGrammars: boolean;
    displayLanguageTransitions: boolean;
    showCrossLanguageReferences: boolean;
    groupByLanguage: boolean;
    visualizeComposition: boolean;
}

/**
 * Minotaur extension configuration
 */
export interface MinotaurExtensionConfig {
    inheritance: InheritanceVisualization;
    contextSensitive: ContextSensitiveVisualization;
    multiLanguage: MultiLanguageVisualization;
    enableAdvancedFeatures: boolean;
    showMetadata: boolean;
    enableComparison: boolean;
}

/**
 * Inheritance relationship data
 */
export interface InheritanceRelationship {
    baseRule: string;
    derivedRule: string;
    relationshipType: 'inherits' | 'overrides' | 'extends';
    depth: number;
    confidence: number;
}

/**
 * Context boundary information
 */
export interface ContextBoundary {
    id: string;
    type: 'scope' | 'function' | 'class' | 'block' | 'namespace';
    name: string;
    bounds: Rectangle;
    parentContext?: string;
    symbols: string[];
    rules: string[];
}

/**
 * Language composition information
 */
export interface LanguageComposition {
    primaryLanguage: string;
    embeddedLanguages: {
        language: string;
        bounds: Rectangle[];
        transitionPoints: Point[];
        rules: string[];
    }[];
    crossReferences: {
        fromLanguage: string;
        toLanguage: string;
        fromRule: string;
        toRule: string;
        referenceType: 'call' | 'embed' | 'include';
    }[];
}

/**
 * Main Minotaur Extensions class
 */
export class MinotaurExtensions {
  private config: MinotaurExtensionConfig;
  private inheritanceCache: Map<string, InheritanceRelationship[]>;
  private contextCache: Map<string, ContextBoundary[]>;
  private compositionCache: Map<string, LanguageComposition>;

  constructor(config: Partial<MinotaurExtensionConfig> = {}) {
    this.config = {
      inheritance: {
        showInheritanceTree: true,
        highlightInheritedRules: true,
        showOverrideIndicators: true,
        displayInheritanceDepth: true,
        groupByInheritanceLevel: false,
        showInheritanceConnections: true,
      },
      contextSensitive: {
        showContextBoundaries: true,
        highlightContextSwitches: true,
        displaySymbolTable: false,
        showScopeHierarchy: true,
        visualizeContextFlow: true,
        showContextDependencies: true,
      },
      multiLanguage: {
        showLanguageBoundaries: true,
        highlightEmbeddedGrammars: true,
        displayLanguageTransitions: true,
        showCrossLanguageReferences: true,
        groupByLanguage: false,
        visualizeComposition: true,
      },
      enableAdvancedFeatures: true,
      showMetadata: true,
      enableComparison: false,
      ...config,
    };

    this.inheritanceCache = new Map();
    this.contextCache = new Map();
    this.compositionCache = new Map();
  }

  /**
     * Enhance diagram with Minotaur-specific features
     */
  public enhanceDiagram(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): RailroadDiagram {
    const enhancedDiagram = { ...diagram };

    // Apply inheritance enhancements
    if (this.config.inheritance.showInheritanceTree) {
      this.addInheritanceVisualization(enhancedDiagram, analyzedGrammar);
    }

    // Apply context-sensitive enhancements
    if (this.config.contextSensitive.showContextBoundaries) {
      this.addContextSensitiveVisualization(enhancedDiagram, analyzedGrammar);
    }

    // Apply multi-language enhancements
    if (this.config.multiLanguage.showLanguageBoundaries) {
      this.addMultiLanguageVisualization(enhancedDiagram, analyzedGrammar);
    }

    // Add metadata enhancements
    if (this.config.showMetadata) {
      this.addMetadataVisualization(enhancedDiagram, analyzedGrammar);
    }

    return enhancedDiagram;
  }

  /**
     * Add inheritance visualization to the diagram
     */
  private addInheritanceVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    const relationships = this.extractInheritanceRelationships(analyzedGrammar);

    // Add inheritance indicators to elements
    for (const element of diagram.elements) {
      this.enhanceElementWithInheritance(element, relationships, analyzedGrammar);
    }

    // Add inheritance connections
    if (this.config.inheritance.showInheritanceConnections) {
      const inheritanceConnections = this.createInheritanceConnections(relationships, diagram);
      diagram.connections.push(...inheritanceConnections);
    }

    // Group elements by inheritance level
    if (this.config.inheritance.groupByInheritanceLevel) {
      this.groupElementsByInheritanceLevel(diagram, relationships);
    }

    // Add inheritance tree visualization
    if (this.config.inheritance.showInheritanceTree) {
      this.addInheritanceTreeVisualization(diagram, relationships);
    }
  }

  /**
     * Extract inheritance relationships from analyzed grammar
     */
  private extractInheritanceRelationships(analyzedGrammar: AnalyzedGrammar): InheritanceRelationship[] {
    const cacheKey = `${analyzedGrammar.name}_inheritance`;

    if (this.inheritanceCache.has(cacheKey)) {
      return this.inheritanceCache.get(cacheKey)!;
    }

    const relationships: InheritanceRelationship[] = [];

    // Process inherited rules
    for (const ruleName of analyzedGrammar.inheritance.inheritedRules) {
      const baseGrammars = analyzedGrammar.inheritance.baseGrammars;
      for (let i = 0; i < baseGrammars.length; i++) {
        relationships.push({
          baseRule: `${baseGrammars[i]}.${ruleName}`,
          derivedRule: ruleName,
          relationshipType: 'inherits',
          depth: i + 1,
          confidence: 1.0,
        });
      }
    }

    // Process overridden rules
    for (const ruleName of analyzedGrammar.inheritance.overriddenRules) {
      const baseGrammars = analyzedGrammar.inheritance.baseGrammars;
      for (let i = 0; i < baseGrammars.length; i++) {
        relationships.push({
          baseRule: `${baseGrammars[i]}.${ruleName}`,
          derivedRule: ruleName,
          relationshipType: 'overrides',
          depth: i + 1,
          confidence: 0.9,
        });
      }
    }

    this.inheritanceCache.set(cacheKey, relationships);
    return relationships;
  }

  /**
     * Enhance element with inheritance information
     */
  private enhanceElementWithInheritance(
    element: RailroadElement,
    relationships: InheritanceRelationship[],
    _analyzedGrammar: AnalyzedGrammar,
  ): void {
    const ruleName = element.metadata.grammarRule;
    if (!ruleName) {
      return;
    }

    // Find inheritance relationships for this rule
    const elementRelationships = relationships.filter(r =>
      r.derivedRule === ruleName || r.baseRule.endsWith(`.${ruleName}`),
    );

    if (elementRelationships.length > 0) {
      // Add inheritance metadata
      element.metadata.inheritanceRelationships = elementRelationships;
      element.metadata.inheritanceDepth = Math.max(...elementRelationships.map(r => r.depth));

      // Add visual indicators
      if (this.config.inheritance.highlightInheritedRules) {
        this.addInheritanceHighlight(element, elementRelationships);
      }

      if (this.config.inheritance.showOverrideIndicators) {
        this.addOverrideIndicators(element, elementRelationships);
      }

      if (this.config.inheritance.displayInheritanceDepth) {
        this.addInheritanceDepthIndicator(element, elementRelationships);
      }
    }
  }

  /**
     * Add inheritance highlight to element
     */
  private addInheritanceHighlight(
    element: RailroadElement,
    relationships: InheritanceRelationship[],
  ): void {
    const hasInherited = relationships.some(r => r.relationshipType === 'inherits');
    const hasOverridden = relationships.some(r => r.relationshipType === 'overrides');

    if (hasInherited) {
      element.style = {
        ...element.style,
        borderColor: '#4169e1',
        borderWidth: 2,
        backgroundColor: '#f1f8ff',
      };
    }

    if (hasOverridden) {
      element.style = {
        ...element.style,
        borderColor: '#d73a49',
        borderWidth: 3,
        strokeDashArray: '4,2',
      };
    }
  }

  /**
     * Add override indicators to element
     */
  private addOverrideIndicators(
    element: RailroadElement,
    relationships: InheritanceRelationship[],
  ): void {
    const overrideCount = relationships.filter(r => r.relationshipType === 'overrides').length;

    if (overrideCount > 0) {
      // Add override badge
      element.metadata.overrideBadge = {
        text: `⚡${overrideCount}`,
        position: 'top-right',
        style: {
          backgroundColor: '#d73a49',
          textColor: '#ffffff',
          fontSize: '10px',
          borderRadius: '50%',
        },
      };
    }
  }

  /**
     * Add inheritance depth indicator
     */
  private addInheritanceDepthIndicator(
    element: RailroadElement,
    relationships: InheritanceRelationship[],
  ): void {
    const maxDepth = Math.max(...relationships.map(r => r.depth));

    if (maxDepth > 0) {
      element.metadata.depthIndicator = {
        depth: maxDepth,
        visual: '●'.repeat(Math.min(maxDepth, 5)),
        position: 'bottom-left',
        style: {
          textColor: '#6f42c1',
          fontSize: '8px',
        },
      };
    }
  }

  /**
     * Create inheritance connections
     */
  private createInheritanceConnections(
    relationships: InheritanceRelationship[],
    diagram: RailroadDiagram,
  ): RailroadConnection[] {
    const connections: RailroadConnection[] = [];
    const elementMap = new Map<string, RailroadElement>();

    // Build element map
    for (const element of diagram.elements) {
      if (element.metadata.grammarRule) {
        elementMap.set(element.metadata.grammarRule, element);
      }
    }

    // Create connections for inheritance relationships
    for (const relationship of relationships) {
      const derivedElement = elementMap.get(relationship.derivedRule);
      if (!derivedElement) {
        continue;
      }

      // Create inheritance connection
      const connection: RailroadConnection = {
        id: `inheritance_${relationship.baseRule}_${relationship.derivedRule}`,
        from: {
          element: derivedElement,
          point: { x: derivedElement.bounds.x, y: derivedElement.bounds.y - 10 },
        },
        to: {
          element: derivedElement,
          point: { x: derivedElement.bounds.x + derivedElement.bounds.width, y: derivedElement.bounds.y - 10 },
        },
        path: this.createInheritancePath(relationship, derivedElement),
        style: this.getInheritanceConnectionStyle(relationship),
        type: 'branch',
      };

      connections.push(connection);
    }

    return connections;
  }

  /**
     * Create inheritance path
     */
  private createInheritancePath(
    relationship: InheritanceRelationship,
    element: RailroadElement,
  ): Point[] {
    const startX = element.bounds.x + element.bounds.width / 2;
    const startY = element.bounds.y - 10;
    const endX = startX;
    const endY = startY - (relationship.depth * 20);

    return [
      { x: startX, y: startY },
      { x: endX, y: endY },
    ];
  }

  /**
     * Get inheritance connection style
     */
  private getInheritanceConnectionStyle(relationship: InheritanceRelationship): any {
    const baseStyle = {
      strokeWidth: 2,
      opacity: 0.7,
    };

    switch (relationship.relationshipType) {
      case 'inherits':
        return {
          ...baseStyle,
          lineColor: '#4169e1',
          strokeDashArray: '8,4',
        };
      case 'overrides':
        return {
          ...baseStyle,
          lineColor: '#d73a49',
          strokeDashArray: '4,2',
        };
      default:
        return {
          ...baseStyle,
          lineColor: '#6f42c1',
        };
    }
  }

  /**
     * Add context-sensitive visualization
     */
  private addContextSensitiveVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    const contextBoundaries = this.extractContextBoundaries(analyzedGrammar);

    // Add context boundaries to diagram
    for (const boundary of contextBoundaries) {
      this.addContextBoundaryVisualization(diagram, boundary);
    }

    // Enhance elements with context information
    for (const element of diagram.elements) {
      this.enhanceElementWithContext(element, contextBoundaries, analyzedGrammar);
    }

    // Add context flow visualization
    if (this.config.contextSensitive.visualizeContextFlow) {
      this.addContextFlowVisualization(diagram, contextBoundaries);
    }

    // Add symbol table visualization
    if (this.config.contextSensitive.displaySymbolTable) {
      this.addSymbolTableVisualization(diagram, analyzedGrammar);
    }
  }

  /**
     * Extract context boundaries from analyzed grammar
     */
  private extractContextBoundaries(analyzedGrammar: AnalyzedGrammar): ContextBoundary[] {
    const cacheKey = `${analyzedGrammar.name}_context`;

    if (this.contextCache.has(cacheKey)) {
      return this.contextCache.get(cacheKey)!;
    }

    const boundaries: ContextBoundary[] = [];

    // Extract from context-sensitive information
    const contextRules = analyzedGrammar.contextSensitive.contextRules;
    const scopeHierarchy = analyzedGrammar.contextSensitive.scopeHierarchy;

    for (let i = 0; i < contextRules.length; i++) {
      const ruleName = contextRules[i];
      const scope = scopeHierarchy[i] || {};

      boundaries.push({
        id: `context_${i}`,
        type: this.inferContextType(ruleName, scope),
        name: ruleName,
        bounds: { x: 0, y: 0, width: 0, height: 0 }, // Will be calculated later
        parentContext: scope.parent,
        symbols: scope.symbols || [],
        rules: [ruleName],
      });
    }

    this.contextCache.set(cacheKey, boundaries);
    return boundaries;
  }

  /**
     * Infer context type from rule name and scope
     */
  private inferContextType(ruleName: string, scope: any): 'scope' | 'function' | 'class' | 'block' | 'namespace' {
    const lowerName = ruleName.toLowerCase();

    if (lowerName.includes('function') || lowerName.includes('method')) {
      return 'function';
    }
    if (lowerName.includes('class') || lowerName.includes('struct')) {
      return 'class';
    }
    if (lowerName.includes('namespace') || lowerName.includes('module')) {
      return 'namespace';
    }
    if (lowerName.includes('block') || lowerName.includes('{')) {
      return 'block';
    }

    return 'scope';
  }

  /**
     * Add context boundary visualization
     */
  private addContextBoundaryVisualization(
    diagram: RailroadDiagram,
    boundary: ContextBoundary,
  ): void {
    // Create context boundary element
    const boundaryElement: RailroadElement = {
      id: `boundary_${boundary.id}`,
      type: 'group',
      name: `${boundary.type}: ${boundary.name}`,
      parent: undefined,
      children: [],
      bounds: boundary.bounds,
      style: this.getContextBoundaryStyle(boundary.type),
      entryPoint: { x: boundary.bounds.x, y: boundary.bounds.y + boundary.bounds.height / 2 },
      exitPoint: { x: boundary.bounds.x + boundary.bounds.width, y: boundary.bounds.y + boundary.bounds.height / 2 },
      connectionPoints: [],
      metadata: {
        contextBoundary: boundary,
        contextType: boundary.type,
        symbols: boundary.symbols,
      },
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

    diagram.elements.push(boundaryElement);
  }

  /**
     * Get context boundary style
     */
  private getContextBoundaryStyle(type: string): any {
    const baseStyle = {
      borderWidth: 1,
      opacity: 0.3,
      strokeDashArray: '5,5',
    };

    switch (type) {
      case 'function':
        return {
          ...baseStyle,
          borderColor: '#28a745',
          backgroundColor: 'rgba(40, 167, 69, 0.1)',
        };
      case 'class':
        return {
          ...baseStyle,
          borderColor: '#007bff',
          backgroundColor: 'rgba(0, 123, 255, 0.1)',
        };
      case 'namespace':
        return {
          ...baseStyle,
          borderColor: '#6f42c1',
          backgroundColor: 'rgba(111, 66, 193, 0.1)',
        };
      case 'block':
        return {
          ...baseStyle,
          borderColor: '#fd7e14',
          backgroundColor: 'rgba(253, 126, 20, 0.1)',
        };
      default:
        return {
          ...baseStyle,
          borderColor: '#6c757d',
          backgroundColor: 'rgba(108, 117, 125, 0.1)',
        };
    }
  }

  /**
     * Enhance element with context information
     */
  private enhanceElementWithContext(
    element: RailroadElement,
    boundaries: ContextBoundary[],
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    const ruleName = element.metadata.grammarRule;
    if (!ruleName) {
      return;
    }

    // Find context boundaries that contain this element
    const containingBoundaries = boundaries.filter(b => b.rules.includes(ruleName));

    if (containingBoundaries.length > 0) {
      element.metadata.contextBoundaries = containingBoundaries;

      // Add context indicators
      if (this.config.contextSensitive.highlightContextSwitches) {
        this.addContextSwitchIndicators(element, containingBoundaries);
      }
    }

    // Check if this is a context-sensitive rule
    if (analyzedGrammar.contextSensitive.contextRules.includes(ruleName)) {
      element.metadata.contextSensitive = true;

      // Add context-sensitive styling
      element.style = {
        ...element.style,
        borderColor: '#6f42c1',
        backgroundColor: '#f5f0ff',
        borderWidth: 2,
      };
    }
  }

  /**
     * Add context switch indicators
     */
  private addContextSwitchIndicators(
    element: RailroadElement,
    boundaries: ContextBoundary[],
  ): void {
    const contextTypes = boundaries.map(b => b.type);
    const uniqueTypes = [...new Set(contextTypes)];

    if (uniqueTypes.length > 1) {
      element.metadata.contextSwitchIndicator = {
        types: uniqueTypes,
        visual: '⚡',
        position: 'top-left',
        style: {
          backgroundColor: '#ffc107',
          textColor: '#212529',
          fontSize: '10px',
        },
      };
    }
  }

  /**
     * Add multi-language visualization
     */
  private addMultiLanguageVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    const composition = this.extractLanguageComposition(analyzedGrammar);

    // Add language boundaries
    for (const embedded of composition.embeddedLanguages) {
      this.addLanguageBoundaryVisualization(diagram, embedded);
    }

    // Add cross-language references
    if (this.config.multiLanguage.showCrossLanguageReferences) {
      this.addCrossLanguageReferences(diagram, composition);
    }

    // Group elements by language
    if (this.config.multiLanguage.groupByLanguage) {
      this.groupElementsByLanguage(diagram, composition);
    }
  }

  /**
     * Extract language composition information
     */
  private extractLanguageComposition(analyzedGrammar: AnalyzedGrammar): LanguageComposition {
    const cacheKey = `${analyzedGrammar.name}_composition`;

    if (this.compositionCache.has(cacheKey)) {
      return this.compositionCache.get(cacheKey)!;
    }

    const composition: LanguageComposition = {
      primaryLanguage: analyzedGrammar.format,
      embeddedLanguages: [],
      crossReferences: [],
    };

    // Extract embedded languages from metadata
    for (const [ruleName, rule] of analyzedGrammar.rules) {
      if (rule.metadata?.formatSpecific && rule.metadata.formatSpecific !== analyzedGrammar.format) {
        const language = rule.metadata.formatSpecific;
        let embeddedLang = composition.embeddedLanguages.find(e => e.language === language);

        if (!embeddedLang) {
          embeddedLang = {
            language,
            bounds: [],
            transitionPoints: [],
            rules: [],
          };
          composition.embeddedLanguages.push(embeddedLang);
        }

        embeddedLang.rules.push(ruleName);
      }
    }

    this.compositionCache.set(cacheKey, composition);
    return composition;
  }

  /**
     * Add language boundary visualization
     */
  private addLanguageBoundaryVisualization(
    diagram: RailroadDiagram,
    embedded: any,
  ): void {
    // Create language boundary element
    const boundaryElement: RailroadElement = {
      id: `language_${embedded.language}`,
      type: 'group',
      name: `Language: ${embedded.language}`,
      parent: undefined,
      children: [],
      bounds: { x: 0, y: 0, width: 0, height: 0 }, // Will be calculated
      style: this.getLanguageBoundaryStyle(embedded.language),
      entryPoint: { x: 0, y: 0 },
      exitPoint: { x: 0, y: 0 },
      connectionPoints: [],
      metadata: {
        language: embedded.language,
        embeddedRules: embedded.rules,
      },
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

    diagram.elements.push(boundaryElement);
  }

  /**
     * Get language boundary style
     */
  private getLanguageBoundaryStyle(language: string): any {
    const colors = {
      'javascript': '#f7df1e',
      'css': '#1572b6',
      'html': '#e34f26',
      'python': '#3776ab',
      'java': '#ed8b00',
      'csharp': '#239120',
      'cpp': '#00599c',
      'rust': '#000000',
    };

    const color = colors[language.toLowerCase() as keyof typeof colors] || '#6c757d';

    return {
      borderColor: color,
      backgroundColor: `${color}20`,
      borderWidth: 2,
      strokeDashArray: '10,5',
      opacity: 0.7,
    };
  }

  /**
     * Add metadata visualization
     */
  private addMetadataVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    // Add metadata panel
    const metadataElement: RailroadElement = {
      id: 'metadata_panel',
      type: 'comment',
      name: 'Grammar Metadata',
      content: this.formatMetadata(analyzedGrammar),
      parent: undefined,
      children: [],
      bounds: { x: 10, y: 10, width: 200, height: 100 },
      style: {
        backgroundColor: '#f8f9fa',
        borderColor: '#dee2e6',
        textColor: '#495057',
        fontSize: 10,
        padding: { top: 8, right: 8, bottom: 8, left: 8 },
      },
      entryPoint: { x: 10, y: 60 },
      exitPoint: { x: 210, y: 60 },
      connectionPoints: [],
      metadata: {
        isMetadataPanel: true,
        grammarStatistics: analyzedGrammar.statistics,
      },
      interactive: true,
      selectable: false,
      hoverable: true,
      state: {
        visible: true,
        selected: false,
        highlighted: false,
        collapsed: false,
      },
    };

    diagram.elements.unshift(metadataElement);
  }

  /**
     * Format metadata for display
     */
  private formatMetadata(analyzedGrammar: AnalyzedGrammar): string {
    const stats = analyzedGrammar.statistics;
    const inheritance = analyzedGrammar.inheritance;

    return [
      `Grammar: ${analyzedGrammar.name}`,
      `Format: ${analyzedGrammar.format}`,
      `Rules: ${stats.totalRules}`,
      `Terminals: ${stats.terminalRules}`,
      `Non-terminals: ${stats.nonTerminalRules}`,
      `Inheritance Depth: ${stats.inheritanceDepth}`,
      `Complexity: ${stats.complexity}`,
      `Base Grammars: ${inheritance.baseGrammars.length}`,
      `Inherited Rules: ${inheritance.inheritedRules.length}`,
      `Overridden Rules: ${inheritance.overriddenRules.length}`,
    ].join('\n');
  }

  /**
     * Group elements by inheritance level
     */
  private groupElementsByInheritanceLevel(
    diagram: RailroadDiagram,
    relationships: InheritanceRelationship[],
  ): void {
    // Implementation for grouping elements by inheritance level
    // This would reorganize the layout to group related elements
  }

  /**
     * Add inheritance tree visualization
     */
  private addInheritanceTreeVisualization(
    diagram: RailroadDiagram,
    relationships: InheritanceRelationship[],
  ): void {
    // Implementation for inheritance tree visualization
    // This would add a separate tree view showing inheritance hierarchy
  }

  /**
     * Add context flow visualization
     */
  private addContextFlowVisualization(
    diagram: RailroadDiagram,
    boundaries: ContextBoundary[],
  ): void {
    // Implementation for context flow visualization
    // This would show how context changes through the grammar
  }

  /**
     * Add symbol table visualization
     */
  private addSymbolTableVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): void {
    // Implementation for symbol table visualization
    // This would show the symbol table as a separate panel
  }

  /**
     * Add cross-language references
     */
  private addCrossLanguageReferences(
    diagram: RailroadDiagram,
    composition: LanguageComposition,
  ): void {
    // Implementation for cross-language reference visualization
    // This would show connections between different language elements
  }

  /**
     * Group elements by language
     */
  private groupElementsByLanguage(
    diagram: RailroadDiagram,
    composition: LanguageComposition,
  ): void {
    // Implementation for grouping elements by language
    // This would reorganize the layout to group language-specific elements
  }

  /**
     * Update configuration
     */
  public updateConfig(config: Partial<MinotaurExtensionConfig>): void {
    this.config = { ...this.config, ...config };
    this.clearCache();
  }

  /**
     * Clear all caches
     */
  public clearCache(): void {
    this.inheritanceCache.clear();
    this.contextCache.clear();
    this.compositionCache.clear();
  }

  /**
     * Get extension statistics
     */
  public getStats(): {
        inheritanceCacheSize: number;
        contextCacheSize: number;
        compositionCacheSize: number;
        config: MinotaurExtensionConfig;
        } {
    return {
      inheritanceCacheSize: this.inheritanceCache.size,
      contextCacheSize: this.contextCache.size,
      compositionCacheSize: this.compositionCache.size,
      config: this.config,
    };
  }
}

