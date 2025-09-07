/**
 * Context-Sensitive Parsing Visualization Component
 *
 * This module provides specialized visualization for context-sensitive
 * parsing features, including context boundaries, symbol tables, scope
 * hierarchies, and context flow diagrams.
 */

import {
  RailroadDiagram,
  RailroadElement,
  RailroadConnection,
  Point,
  Rectangle,
} from './RailroadTypes';
import { AnalyzedGrammar } from './GrammarAnalyzer';

/**
 * Context scope information
 */
export interface ContextScope {
    id: string;
    name: string;
    type: 'global' | 'function' | 'class' | 'block' | 'namespace' | 'module';
    level: number;
    bounds: Rectangle;
    parent?: ContextScope;
    children: ContextScope[];
    symbols: SymbolInfo[];
    rules: string[];
    entryPoints: Point[];
    exitPoints: Point[];
}

/**
 * Symbol information
 */
export interface SymbolInfo {
    name: string;
    type: 'variable' | 'function' | 'class' | 'type' | 'constant' | 'parameter';
    scope: string;
    declarationPoint: Point;
    usagePoints: Point[];
    visibility: 'public' | 'private' | 'protected' | 'internal';
    metadata: {
        dataType?: string;
        isStatic?: boolean;
        isReadonly?: boolean;
        isOptional?: boolean;
    };
}

/**
 * Context transition information
 */
export interface ContextTransition {
    id: string;
    from: ContextScope;
    to: ContextScope;
    trigger: string;
    type: 'enter' | 'exit' | 'switch' | 'call' | 'return';
    point: Point;
    conditions: string[];
    effects: string[];
}

/**
 * Context flow path
 */
export interface ContextFlowPath {
    id: string;
    scopes: ContextScope[];
    transitions: ContextTransition[];
    startPoint: Point;
    endPoint: Point;
    path: Point[];
    complexity: number;
}

/**
 * Context-sensitive visualization configuration
 */
export interface ContextSensitiveVisualizationConfig {
    showScopes: boolean;
    showSymbolTable: boolean;
    showContextFlow: boolean;
    showTransitions: boolean;
    showSymbolUsage: boolean;
    scopeLayout: 'nested' | 'hierarchical' | 'flat';
    symbolTablePosition: 'left' | 'right' | 'bottom' | 'floating';
    highlightActiveScope: boolean;
    showScopeMetrics: boolean;
    animateTransitions: boolean;
    colorCodeScopes: boolean;
}

/**
 * Context-Sensitive Visualizer class
 */
export class ContextSensitiveVisualizer {
  private config: ContextSensitiveVisualizationConfig;
  private scopeCache: Map<string, ContextScope[]>;
  private symbolCache: Map<string, SymbolInfo[]>;
  private transitionCache: Map<string, ContextTransition[]>;
  private flowCache: Map<string, ContextFlowPath[]>;

  constructor(config: Partial<ContextSensitiveVisualizationConfig> = {}) {
    this.config = {
      showScopes: true,
      showSymbolTable: true,
      showContextFlow: true,
      showTransitions: true,
      showSymbolUsage: false,
      scopeLayout: 'nested',
      symbolTablePosition: 'right',
      highlightActiveScope: true,
      showScopeMetrics: true,
      animateTransitions: true,
      colorCodeScopes: true,
      ...config,
    };

    this.scopeCache = new Map();
    this.symbolCache = new Map();
    this.transitionCache = new Map();
    this.flowCache = new Map();
  }

  /**
     * Create context-sensitive visualization for a diagram
     */
  public createContextSensitiveVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): {
        scopes: ContextScope[];
        symbols: SymbolInfo[];
        transitions: ContextTransition[];
        flows: ContextFlowPath[];
        elements: RailroadElement[];
        connections: RailroadConnection[];
    } {
    // Extract context information
    const scopes = this.extractContextScopes(analyzedGrammar);
    const symbols = this.extractSymbolInformation(analyzedGrammar);
    const transitions = this.extractContextTransitions(analyzedGrammar, scopes);
    const flows = this.createContextFlows(scopes, transitions);

    // Calculate layout
    this.calculateScopeLayout(scopes);

    // Convert to railroad elements
    const elements = this.convertToRailroadElements(scopes, symbols, diagram);
    const connections = this.convertToRailroadConnections(transitions, flows);

    return { scopes, symbols, transitions, flows, elements, connections };
  }

  /**
     * Extract context scopes from analyzed grammar
     */
  private extractContextScopes(analyzedGrammar: AnalyzedGrammar): ContextScope[] {
    const cacheKey = `${analyzedGrammar.name}_scopes`;

    if (this.scopeCache.has(cacheKey)) {
      return this.scopeCache.get(cacheKey)!;
    }

    const scopes: ContextScope[] = [];
    const contextInfo = analyzedGrammar.contextSensitive;

    // Create global scope
    const globalScope: ContextScope = {
      id: 'global_scope',
      name: 'Global',
      type: 'global',
      level: 0,
      bounds: { x: 0, y: 0, width: 0, height: 0 },
      children: [],
      symbols: [],
      rules: [],
      entryPoints: [],
      exitPoints: [],
    };
    scopes.push(globalScope);

    // Extract scopes from context rules
    for (let i = 0; i < contextInfo.contextRules.length; i++) {
      const ruleName = contextInfo.contextRules[i];
      const scopeInfo = contextInfo.scopeHierarchy[i] || {};

      const scope = this.createScopeFromRule(ruleName, scopeInfo, i + 1);
      scopes.push(scope);

      // Build parent-child relationships
      if (scopeInfo.parent) {
        const parentScope = scopes.find(s => s.name === scopeInfo.parent);
        if (parentScope) {
          scope.parent = parentScope;
          parentScope.children.push(scope);
        }
      } else {
        scope.parent = globalScope;
        globalScope.children.push(scope);
      }
    }

    this.scopeCache.set(cacheKey, scopes);
    return scopes;
  }

  /**
     * Create scope from rule information
     */
  private createScopeFromRule(ruleName: string, scopeInfo: any, level: number): ContextScope {
    return {
      id: `scope_${ruleName}`,
      name: ruleName,
      type: this.inferScopeType(ruleName, scopeInfo),
      level,
      bounds: { x: 0, y: 0, width: 0, height: 0 },
      children: [],
      symbols: [],
      rules: [ruleName],
      entryPoints: [],
      exitPoints: [],
    };
  }

  /**
     * Infer scope type from rule name and information
     */
  private inferScopeType(ruleName: string, scopeInfo: any): ContextScope['type'] {
    const lowerName = ruleName.toLowerCase();

    if (lowerName.includes('function') || lowerName.includes('method')) {
      return 'function';
    }
    if (lowerName.includes('class') || lowerName.includes('struct')) {
      return 'class';
    }
    if (lowerName.includes('namespace') || lowerName.includes('package')) {
      return 'namespace';
    }
    if (lowerName.includes('module') || lowerName.includes('unit')) {
      return 'module';
    }
    if (lowerName.includes('block') || lowerName.includes('{')) {
      return 'block';
    }

    return 'block';
  }

  /**
     * Extract symbol information from analyzed grammar
     */
  private extractSymbolInformation(analyzedGrammar: AnalyzedGrammar): SymbolInfo[] {
    const cacheKey = `${analyzedGrammar.name}_symbols`;

    if (this.symbolCache.has(cacheKey)) {
      return this.symbolCache.get(cacheKey)!;
    }

    const symbols: SymbolInfo[] = [];
    const contextInfo = analyzedGrammar.contextSensitive;

    // Extract symbols from symbol table
    if (contextInfo.symbolTable) {
      for (const [symbolName, symbolData] of Object.entries(contextInfo.symbolTable)) {
        const symbol = this.createSymbolInfo(symbolName, symbolData as any);
        symbols.push(symbol);
      }
    }

    // Extract symbols from scope hierarchy
    for (const scopeInfo of contextInfo.scopeHierarchy) {
      if (scopeInfo.symbols) {
        for (const symbolName of scopeInfo.symbols) {
          if (!symbols.find(s => s.name === symbolName)) {
            const symbol = this.createSymbolInfo(symbolName, { scope: scopeInfo.name });
            symbols.push(symbol);
          }
        }
      }
    }

    this.symbolCache.set(cacheKey, symbols);
    return symbols;
  }

  /**
     * Create symbol information
     */
  private createSymbolInfo(name: string, data: any): SymbolInfo {
    return {
      name,
      type: this.inferSymbolType(name, data),
      scope: data.scope || 'global',
      declarationPoint: data.declarationPoint || { x: 0, y: 0 },
      usagePoints: data.usagePoints || [],
      visibility: data.visibility || 'public',
      metadata: {
        dataType: data.dataType,
        isStatic: data.isStatic || false,
        isReadonly: data.isReadonly || false,
        isOptional: data.isOptional || false,
      },
    };
  }

  /**
     * Infer symbol type from name and data
     */
  private inferSymbolType(name: string, data: any): SymbolInfo['type'] {
    if (data.type) {
      return data.type;
    }

    const lowerName = name.toLowerCase();

    if (lowerName.includes('function') || lowerName.includes('method')) {
      return 'function';
    }
    if (lowerName.includes('class') || lowerName.includes('type')) {
      return 'class';
    }
    if (lowerName.includes('const') || lowerName.includes('constant')) {
      return 'constant';
    }
    if (lowerName.includes('param') || lowerName.includes('arg')) {
      return 'parameter';
    }

    return 'variable';
  }

  /**
     * Extract context transitions
     */
  private extractContextTransitions(
    analyzedGrammar: AnalyzedGrammar,
    scopes: ContextScope[],
  ): ContextTransition[] {
    const cacheKey = `${analyzedGrammar.name}_transitions`;

    if (this.transitionCache.has(cacheKey)) {
      return this.transitionCache.get(cacheKey)!;
    }

    const transitions: ContextTransition[] = [];
    const contextInfo = analyzedGrammar.contextSensitive;

    // Create transitions between parent and child scopes
    for (const scope of scopes) {
      for (const child of scope.children) {
        // Enter transition
        const enterTransition: ContextTransition = {
          id: `enter_${scope.id}_${child.id}`,
          from: scope,
          to: child,
          trigger: `enter_${child.name}`,
          type: 'enter',
          point: { x: 0, y: 0 }, // Will be calculated during layout
          conditions: [],
          effects: [`push_scope(${child.name})`],
        };
        transitions.push(enterTransition);

        // Exit transition
        const exitTransition: ContextTransition = {
          id: `exit_${child.id}_${scope.id}`,
          from: child,
          to: scope,
          trigger: `exit_${child.name}`,
          type: 'exit',
          point: { x: 0, y: 0 }, // Will be calculated during layout
          conditions: [],
          effects: [`pop_scope(${child.name})`],
        };
        transitions.push(exitTransition);
      }
    }

    this.transitionCache.set(cacheKey, transitions);
    return transitions;
  }

  /**
     * Create context flows
     */
  private createContextFlows(
    scopes: ContextScope[],
    transitions: ContextTransition[],
  ): ContextFlowPath[] {
    const flows: ContextFlowPath[] = [];

    // Create flows for common execution paths
    for (const scope of scopes) {
      if (scope.children.length > 0) {
        const flow = this.createScopeFlow(scope, transitions);
        flows.push(flow);
      }
    }

    return flows;
  }

  /**
     * Create flow for a scope
     */
  private createScopeFlow(scope: ContextScope, transitions: ContextTransition[]): ContextFlowPath {
    const flowScopes = [scope, ...scope.children];
    const flowTransitions = transitions.filter(t =>
      flowScopes.includes(t.from) && flowScopes.includes(t.to),
    );

    return {
      id: `flow_${scope.id}`,
      scopes: flowScopes,
      transitions: flowTransitions,
      startPoint: { x: scope.bounds.x, y: scope.bounds.y },
      endPoint: { x: scope.bounds.x + scope.bounds.width, y: scope.bounds.y + scope.bounds.height },
      path: this.calculateFlowPath(flowScopes),
      complexity: this.calculateFlowComplexity(flowScopes, flowTransitions),
    };
  }

  /**
     * Calculate flow path through scopes
     */
  private calculateFlowPath(scopes: ContextScope[]): Point[] {
    const path: Point[] = [];

    for (const scope of scopes) {
      const centerPoint = {
        x: scope.bounds.x + scope.bounds.width / 2,
        y: scope.bounds.y + scope.bounds.height / 2,
      };
      path.push(centerPoint);
    }

    return path;
  }

  /**
     * Calculate flow complexity
     */
  private calculateFlowComplexity(scopes: ContextScope[], transitions: ContextTransition[]): number {
    let complexity = scopes.length;
    complexity += transitions.length * 0.5;
    complexity += scopes.reduce((sum, scope) => sum + scope.symbols.length, 0) * 0.1;
    return complexity;
  }

  /**
     * Calculate scope layout
     */
  private calculateScopeLayout(scopes: ContextScope[]): void {
    switch (this.config.scopeLayout) {
      case 'nested':
        this.calculateNestedLayout(scopes);
        break;
      case 'hierarchical':
        this.calculateHierarchicalLayout(scopes);
        break;
      case 'flat':
        this.calculateFlatLayout(scopes);
        break;
    }
  }

  /**
     * Calculate nested layout (scopes inside their parents)
     */
  private calculateNestedLayout(scopes: ContextScope[]): void {
    const globalScope = scopes.find(s => s.type === 'global');
    if (!globalScope) {
      return;
    }

    // Start with global scope
    globalScope.bounds = { x: 0, y: 0, width: 800, height: 600 };

    // Layout children recursively
    this.layoutScopeChildren(globalScope, 20);
  }

  /**
     * Layout children of a scope
     */
  private layoutScopeChildren(scope: ContextScope, padding: number): void {
    if (scope.children.length === 0) {
      return;
    }

    const availableWidth = scope.bounds.width - 2 * padding;
    const availableHeight = scope.bounds.height - 2 * padding;

    // Calculate child dimensions
    const childWidth = Math.max(150, availableWidth / Math.ceil(Math.sqrt(scope.children.length)));
    // eslint-disable-next-line max-len
    const childHeight = Math.max(100, availableHeight / Math.ceil(scope.children.length / Math.ceil(availableWidth / childWidth)));

    // Position children
    let x = scope.bounds.x + padding;
    let y = scope.bounds.y + padding;

    for (let i = 0; i < scope.children.length; i++) {
      const child = scope.children[i];

      child.bounds = {
        x,
        y,
        width: childWidth - 10,
        height: childHeight - 10,
      };

      // Calculate entry and exit points
      child.entryPoints = [{ x: child.bounds.x, y: child.bounds.y + child.bounds.height / 2 }];
      child.exitPoints = [{ x: child.bounds.x + child.bounds.width, y: child.bounds.y + child.bounds.height / 2 }];

      // Move to next position
      x += childWidth;
      if (x + childWidth > scope.bounds.x + scope.bounds.width - padding) {
        x = scope.bounds.x + padding;
        y += childHeight;
      }

      // Layout grandchildren
      this.layoutScopeChildren(child, 10);
    }
  }

  /**
     * Calculate hierarchical layout (tree structure)
     */
  private calculateHierarchicalLayout(scopes: ContextScope[]): void {
    const globalScope = scopes.find(s => s.type === 'global');
    if (!globalScope) {
      return;
    }

    // Position global scope at top
    globalScope.bounds = { x: 400, y: 50, width: 200, height: 80 };

    // Layout levels
    this.layoutScopeLevel(globalScope, 0, 150);
  }

  /**
     * Layout a level of scopes
     */
  private layoutScopeLevel(scope: ContextScope, level: number, ySpacing: number): void {
    if (scope.children.length === 0) {
      return;
    }

    const totalWidth = scope.children.length * 220;
    const startX = scope.bounds.x + scope.bounds.width / 2 - totalWidth / 2;
    const y = scope.bounds.y + scope.bounds.height + ySpacing;

    for (let i = 0; i < scope.children.length; i++) {
      const child = scope.children[i];

      child.bounds = {
        x: startX + i * 220,
        y,
        width: 200,
        height: 80,
      };

      // Calculate entry and exit points
      child.entryPoints = [{ x: child.bounds.x + child.bounds.width / 2, y: child.bounds.y }];
      child.exitPoints = [{ x: child.bounds.x + child.bounds.width / 2, y: child.bounds.y + child.bounds.height }];

      // Layout next level
      this.layoutScopeLevel(child, level + 1, ySpacing);
    }
  }

  /**
     * Calculate flat layout (all scopes at same level)
     */
  private calculateFlatLayout(scopes: ContextScope[]): void {
    const scopesPerRow = Math.ceil(Math.sqrt(scopes.length));
    const scopeWidth = 200;
    const scopeHeight = 100;
    const spacing = 20;

    for (let i = 0; i < scopes.length; i++) {
      const row = Math.floor(i / scopesPerRow);
      const col = i % scopesPerRow;

      const scope = scopes[i];
      scope.bounds = {
        x: col * (scopeWidth + spacing),
        y: row * (scopeHeight + spacing),
        width: scopeWidth,
        height: scopeHeight,
      };

      // Calculate entry and exit points
      scope.entryPoints = [{ x: scope.bounds.x, y: scope.bounds.y + scope.bounds.height / 2 }];
      scope.exitPoints = [{ x: scope.bounds.x + scope.bounds.width, y: scope.bounds.y + scope.bounds.height / 2 }];
    }
  }

  /**
     * Convert to railroad elements
     */
  private convertToRailroadElements(
    scopes: ContextScope[],
    symbols: SymbolInfo[],
    diagram: RailroadDiagram,
  ): RailroadElement[] {
    const elements: RailroadElement[] = [];

    // Convert scopes to elements
    for (const scope of scopes) {
      const element = this.createScopeElement(scope);
      elements.push(element);
    }

    // Add symbol table if configured
    if (this.config.showSymbolTable) {
      const symbolTableElement = this.createSymbolTableElement(symbols, diagram);
      elements.push(symbolTableElement);
    }

    return elements;
  }

  /**
     * Create scope element
     */
  private createScopeElement(scope: ContextScope): RailroadElement {
    return {
      id: scope.id,
      type: 'group',
      name: scope.name,
      content: `${scope.type} scope`,
      parent: undefined,
      children: [],
      bounds: scope.bounds,
      style: this.getScopeStyle(scope),
      entryPoint: scope.entryPoints[0] || { x: scope.bounds.x, y: scope.bounds.y + scope.bounds.height / 2 },
      // eslint-disable-next-line max-len
      exitPoint: scope.exitPoints[0] || { x: scope.bounds.x + scope.bounds.width, y: scope.bounds.y + scope.bounds.height / 2 },
      connectionPoints: [...scope.entryPoints, ...scope.exitPoints],
      metadata: {
        contextScope: scope,
        scopeType: scope.type,
        scopeLevel: scope.level,
        symbolCount: scope.symbols.length,
        ruleCount: scope.rules.length,
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
  }

  /**
     * Get scope style based on type and configuration
     */
  private getScopeStyle(scope: ContextScope): any {
    const baseStyle = {
      borderWidth: 2,
      borderRadius: 8,
      opacity: 0.8,
      padding: { top: 8, right: 8, bottom: 8, left: 8 },
    };

    if (!this.config.colorCodeScopes) {
      return {
        ...baseStyle,
        backgroundColor: 'rgba(108, 117, 125, 0.1)',
        borderColor: '#6c757d',
        textColor: '#495057',
      };
    }

    // Color-coded styles by scope type
    switch (scope.type) {
      case 'global':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(0, 123, 255, 0.1)',
          borderColor: '#007bff',
          textColor: '#0056b3',
        };
      case 'function':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(40, 167, 69, 0.1)',
          borderColor: '#28a745',
          textColor: '#1e7e34',
        };
      case 'class':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(111, 66, 193, 0.1)',
          borderColor: '#6f42c1',
          textColor: '#59359a',
        };
      case 'namespace':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(253, 126, 20, 0.1)',
          borderColor: '#fd7e14',
          textColor: '#e55a00',
        };
      case 'module':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(220, 53, 69, 0.1)',
          borderColor: '#dc3545',
          textColor: '#bd2130',
        };
      case 'block':
        return {
          ...baseStyle,
          backgroundColor: 'rgba(255, 193, 7, 0.1)',
          borderColor: '#ffc107',
          textColor: '#e0a800',
        };
      default:
        return {
          ...baseStyle,
          backgroundColor: 'rgba(108, 117, 125, 0.1)',
          borderColor: '#6c757d',
          textColor: '#495057',
        };
    }
  }

  /**
     * Create symbol table element
     */
  private createSymbolTableElement(symbols: SymbolInfo[], diagram: RailroadDiagram): RailroadElement {
    const tableWidth = 300;
    const tableHeight = Math.min(400, symbols.length * 25 + 50);

    // Position based on configuration
    let x = 0, y = 0;
    switch (this.config.symbolTablePosition) {
      case 'right':
        x = diagram.bounds.width - tableWidth - 20;
        y = 20;
        break;
      case 'left':
        x = 20;
        y = 20;
        break;
      case 'bottom':
        x = (diagram.bounds.width - tableWidth) / 2;
        y = diagram.bounds.height - tableHeight - 20;
        break;
      case 'floating':
        x = 50;
        y = 50;
        break;
    }

    return {
      id: 'symbol_table',
      type: 'comment',
      name: 'Symbol Table',
      content: this.formatSymbolTable(symbols),
      parent: undefined,
      children: [],
      bounds: { x, y, width: tableWidth, height: tableHeight },
      style: {
        backgroundColor: '#f8f9fa',
        borderColor: '#dee2e6',
        textColor: '#495057',
        borderWidth: 1,
        borderRadius: 4,
        fontSize: 10,
        padding: { top: 10, right: 10, bottom: 10, left: 10 },
      },
      entryPoint: { x, y: y + tableHeight / 2 },
      exitPoint: { x: x + tableWidth, y: y + tableHeight / 2 },
      connectionPoints: [],
      metadata: {
        isSymbolTable: true,
        symbolCount: symbols.length,
        symbols,
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
  }

  /**
     * Format symbol table for display
     */
  private formatSymbolTable(symbols: SymbolInfo[]): string {
    const header = 'Symbol Table\n' + '='.repeat(20) + '\n';

    const symbolLines = symbols.map(symbol => {
      const type = symbol.type.charAt(0).toUpperCase() + symbol.type.slice(1);
      const visibility = symbol.visibility.charAt(0).toUpperCase();
      return `${visibility} ${type}: ${symbol.name} (${symbol.scope})`;
    });

    return header + symbolLines.join('\n');
  }

  /**
     * Convert to railroad connections
     */
  private convertToRailroadConnections(
    transitions: ContextTransition[],
    flows: ContextFlowPath[],
  ): RailroadConnection[] {
    const connections: RailroadConnection[] = [];

    // Convert transitions to connections
    for (const transition of transitions) {
      const connection = this.createTransitionConnection(transition);
      connections.push(connection);
    }

    // Convert flows to connections
    for (const flow of flows) {
      const connection = this.createFlowConnection(flow);
      connections.push(connection);
    }

    return connections;
  }

  /**
     * Create transition connection
     */
  private createTransitionConnection(transition: ContextTransition): RailroadConnection {
    return {
      id: transition.id,
      from: {
        element: { id: transition.from.id } as RailroadElement,
        point: transition.point,
      },
      to: {
        element: { id: transition.to.id } as RailroadElement,
        point: transition.point,
      },
      path: this.calculateTransitionPath(transition),
      style: this.getTransitionStyle(transition),
      type: 'branch',
    };
  }

  /**
     * Calculate transition path
     */
  private calculateTransitionPath(transition: ContextTransition): Point[] {
    const fromCenter = {
      x: transition.from.bounds.x + transition.from.bounds.width / 2,
      y: transition.from.bounds.y + transition.from.bounds.height / 2,
    };

    const toCenter = {
      x: transition.to.bounds.x + transition.to.bounds.width / 2,
      y: transition.to.bounds.y + transition.to.bounds.height / 2,
    };

    return [fromCenter, toCenter];
  }

  /**
     * Get transition style
     */
  private getTransitionStyle(transition: ContextTransition): any {
    const baseStyle = {
      strokeWidth: 2,
      opacity: 0.7,
    };

    switch (transition.type) {
      case 'enter':
        return {
          ...baseStyle,
          lineColor: '#28a745',
          strokeDashArray: undefined,
        };
      case 'exit':
        return {
          ...baseStyle,
          lineColor: '#dc3545',
          strokeDashArray: '4,4',
        };
      case 'switch':
        return {
          ...baseStyle,
          lineColor: '#ffc107',
          strokeDashArray: '8,4',
        };
      case 'call':
        return {
          ...baseStyle,
          lineColor: '#007bff',
          strokeDashArray: '2,2',
        };
      case 'return':
        return {
          ...baseStyle,
          lineColor: '#6f42c1',
          strokeDashArray: '6,2',
        };
      default:
        return {
          ...baseStyle,
          lineColor: '#6c757d',
        };
    }
  }

  /**
     * Create flow connection
     */
  private createFlowConnection(flow: ContextFlowPath): RailroadConnection {
    return {
      id: flow.id,
      from: {
        element: { id: flow.scopes[0].id } as RailroadElement,
        point: flow.startPoint,
      },
      to: {
        element: { id: flow.scopes[flow.scopes.length - 1].id } as RailroadElement,
        point: flow.endPoint,
      },
      path: flow.path,
      style: {
        lineColor: '#17a2b8',
        strokeWidth: Math.max(1, Math.min(5, flow.complexity)),
        opacity: 0.6,
        strokeDashArray: '10,5',
      },
      type: 'normal',
    };
  }

  /**
     * Update configuration
     */
  public updateConfig(config: Partial<ContextSensitiveVisualizationConfig>): void {
    this.config = { ...this.config, ...config };
    this.clearCache();
  }

  /**
     * Clear all caches
     */
  public clearCache(): void {
    this.scopeCache.clear();
    this.symbolCache.clear();
    this.transitionCache.clear();
    this.flowCache.clear();
  }

  /**
     * Get visualization statistics
     */
  public getStats(): {
        scopeCacheSize: number;
        symbolCacheSize: number;
        transitionCacheSize: number;
        flowCacheSize: number;
        config: ContextSensitiveVisualizationConfig;
        } {
    return {
      scopeCacheSize: this.scopeCache.size,
      symbolCacheSize: this.symbolCache.size,
      transitionCacheSize: this.transitionCache.size,
      flowCacheSize: this.flowCache.size,
      config: this.config,
    };
  }
}

