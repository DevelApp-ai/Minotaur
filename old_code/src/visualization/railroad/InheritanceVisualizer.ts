/**
 * Inheritance Visualization Component
 *
 * This module provides specialized visualization for grammar inheritance
 * relationships, including inheritance trees, override indicators, and
 * inheritance flow diagrams.
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
 * Inheritance tree node
 */
export interface InheritanceTreeNode {
    id: string;
    grammarName: string;
    ruleName?: string;
    level: number;
    children: InheritanceTreeNode[];
    parent?: InheritanceTreeNode;
    bounds: Rectangle;
    metadata: {
        isBaseGrammar: boolean;
        isOverridden: boolean;
        isInherited: boolean;
        confidence: number;
        ruleCount: number;
    };
}

/**
 * Inheritance flow connection
 */
export interface InheritanceFlow {
    id: string;
    from: InheritanceTreeNode;
    to: InheritanceTreeNode;
    type: 'inherits' | 'overrides' | 'extends';
    strength: number;
    path: Point[];
    style: {
        color: string;
        width: number;
        dashArray?: string;
        opacity: number;
    };
}

/**
 * Inheritance visualization configuration
 */
export interface InheritanceVisualizationConfig {
    showTree: boolean;
    showFlow: boolean;
    showOverrides: boolean;
    showMetrics: boolean;
    treeLayout: 'vertical' | 'horizontal' | 'radial';
    nodeSpacing: number;
    levelSpacing: number;
    showRuleDetails: boolean;
    highlightPath: boolean;
    animateFlow: boolean;
}

/**
 * Inheritance Visualizer class
 */
export class InheritanceVisualizer {
  private config: InheritanceVisualizationConfig;
  private treeCache: Map<string, InheritanceTreeNode>;
  private flowCache: Map<string, InheritanceFlow[]>;

  constructor(config: Partial<InheritanceVisualizationConfig> = {}) {
    this.config = {
      showTree: true,
      showFlow: true,
      showOverrides: true,
      showMetrics: true,
      treeLayout: 'vertical',
      nodeSpacing: 80,
      levelSpacing: 120,
      showRuleDetails: false,
      highlightPath: true,
      animateFlow: true,
      ...config,
    };

    this.treeCache = new Map();
    this.flowCache = new Map();
  }

  /**
     * Create inheritance visualization for a diagram
     */
  public createInheritanceVisualization(
    diagram: RailroadDiagram,
    analyzedGrammar: AnalyzedGrammar,
  ): {
        tree: InheritanceTreeNode;
        flows: InheritanceFlow[];
        elements: RailroadElement[];
        connections: RailroadConnection[];
    } {
    // Build inheritance tree
    const tree = this.buildInheritanceTree(analyzedGrammar);

    // Create inheritance flows
    const flows = this.createInheritanceFlows(tree, analyzedGrammar);

    // Convert to railroad elements
    const elements = this.convertTreeToElements(tree);

    // Convert flows to connections
    const connections = this.convertFlowsToConnections(flows);

    return { tree, flows, elements, connections };
  }

  /**
     * Build inheritance tree from analyzed grammar
     */
  private buildInheritanceTree(analyzedGrammar: AnalyzedGrammar): InheritanceTreeNode {
    const cacheKey = `${analyzedGrammar.name}_tree`;

    if (this.treeCache.has(cacheKey)) {
      return this.treeCache.get(cacheKey)!;
    }

    // Create root node for current grammar
    const rootNode: InheritanceTreeNode = {
      id: `tree_${analyzedGrammar.name}`,
      grammarName: analyzedGrammar.name,
      level: 0,
      children: [],
      bounds: { x: 0, y: 0, width: 0, height: 0 },
      metadata: {
        isBaseGrammar: false,
        isOverridden: false,
        isInherited: false,
        confidence: 1.0,
        ruleCount: analyzedGrammar.statistics.totalRules,
      },
    };

    // Add base grammar nodes
    const baseGrammars = analyzedGrammar.inheritance.baseGrammars;
    for (let i = 0; i < baseGrammars.length; i++) {
      const baseGrammar = baseGrammars[i];
      const baseNode = this.createBaseGrammarNode(baseGrammar, i + 1, rootNode);
      rootNode.children.push(baseNode);
    }

    // Add rule-specific nodes if configured
    if (this.config.showRuleDetails) {
      this.addRuleNodes(rootNode, analyzedGrammar);
    }

    // Calculate tree layout
    this.calculateTreeLayout(rootNode);

    this.treeCache.set(cacheKey, rootNode);
    return rootNode;
  }

  /**
     * Create base grammar node
     */
  private createBaseGrammarNode(
    grammarName: string,
    level: number,
    parent: InheritanceTreeNode,
  ): InheritanceTreeNode {
    return {
      id: `tree_${grammarName}_${level}`,
      grammarName,
      level,
      children: [],
      parent,
      bounds: { x: 0, y: 0, width: 0, height: 0 },
      metadata: {
        isBaseGrammar: true,
        isOverridden: false,
        isInherited: true,
        confidence: 1.0 - (level * 0.1),
        ruleCount: 0, // Would be populated from actual grammar analysis
      },
    };
  }

  /**
     * Add rule-specific nodes to the tree
     */
  private addRuleNodes(rootNode: InheritanceTreeNode, analyzedGrammar: AnalyzedGrammar): void {
    // Add inherited rules
    for (const ruleName of analyzedGrammar.inheritance.inheritedRules) {
      const ruleNode: InheritanceTreeNode = {
        id: `rule_${ruleName}_inherited`,
        grammarName: rootNode.grammarName,
        ruleName,
        level: rootNode.level + 1,
        children: [],
        parent: rootNode,
        bounds: { x: 0, y: 0, width: 0, height: 0 },
        metadata: {
          isBaseGrammar: false,
          isOverridden: false,
          isInherited: true,
          confidence: 0.9,
          ruleCount: 1,
        },
      };
      rootNode.children.push(ruleNode);
    }

    // Add overridden rules
    for (const ruleName of analyzedGrammar.inheritance.overriddenRules) {
      const ruleNode: InheritanceTreeNode = {
        id: `rule_${ruleName}_overridden`,
        grammarName: rootNode.grammarName,
        ruleName,
        level: rootNode.level + 1,
        children: [],
        parent: rootNode,
        bounds: { x: 0, y: 0, width: 0, height: 0 },
        metadata: {
          isBaseGrammar: false,
          isOverridden: true,
          isInherited: false,
          confidence: 1.0,
          ruleCount: 1,
        },
      };
      rootNode.children.push(ruleNode);
    }
  }

  /**
     * Calculate tree layout based on configuration
     */
  private calculateTreeLayout(rootNode: InheritanceTreeNode): void {
    switch (this.config.treeLayout) {
      case 'vertical':
        this.calculateVerticalLayout(rootNode);
        break;
      case 'horizontal':
        this.calculateHorizontalLayout(rootNode);
        break;
      case 'radial':
        this.calculateRadialLayout(rootNode);
        break;
    }
  }

  /**
     * Calculate vertical tree layout
     */
  private calculateVerticalLayout(node: InheritanceTreeNode, x: number = 0, y: number = 0): void {
    // Calculate node size
    const nodeWidth = this.calculateNodeWidth(node);
    const nodeHeight = this.calculateNodeHeight(node);

    // Set node bounds
    node.bounds = { x, y, width: nodeWidth, height: nodeHeight };

    // Calculate children positions
    if (node.children.length > 0) {
      const totalChildrenWidth = node.children.length * this.config.nodeSpacing;
      const startX = x - totalChildrenWidth / 2 + this.config.nodeSpacing / 2;
      const childY = y + this.config.levelSpacing;

      for (let i = 0; i < node.children.length; i++) {
        const childX = startX + i * this.config.nodeSpacing;
        this.calculateVerticalLayout(node.children[i], childX, childY);
      }
    }
  }

  /**
     * Calculate horizontal tree layout
     */
  private calculateHorizontalLayout(node: InheritanceTreeNode, x: number = 0, y: number = 0): void {
    // Calculate node size
    const nodeWidth = this.calculateNodeWidth(node);
    const nodeHeight = this.calculateNodeHeight(node);

    // Set node bounds
    node.bounds = { x, y, width: nodeWidth, height: nodeHeight };

    // Calculate children positions
    if (node.children.length > 0) {
      const totalChildrenHeight = node.children.length * this.config.nodeSpacing;
      const startY = y - totalChildrenHeight / 2 + this.config.nodeSpacing / 2;
      const childX = x + this.config.levelSpacing;

      for (let i = 0; i < node.children.length; i++) {
        const childY = startY + i * this.config.nodeSpacing;
        this.calculateHorizontalLayout(node.children[i], childX, childY);
      }
    }
  }

  /**
     * Calculate radial tree layout
     */
  // eslint-disable-next-line max-len
  private calculateRadialLayout(node: InheritanceTreeNode, centerX: number = 0, centerY: number = 0, radius: number = 100): void {
    // Calculate node size
    const nodeWidth = this.calculateNodeWidth(node);
    const nodeHeight = this.calculateNodeHeight(node);

    // Set root node at center
    if (node.level === 0) {
      node.bounds = {
        x: centerX - nodeWidth / 2,
        y: centerY - nodeHeight / 2,
        width: nodeWidth,
        height: nodeHeight,
      };
    }

    // Calculate children positions in a circle
    if (node.children.length > 0) {
      const angleStep = (2 * Math.PI) / node.children.length;
      const childRadius = radius + this.config.levelSpacing;

      for (let i = 0; i < node.children.length; i++) {
        const angle = i * angleStep;
        const childX = centerX + Math.cos(angle) * childRadius;
        const childY = centerY + Math.sin(angle) * childRadius;

        const child = node.children[i];
        const childNodeWidth = this.calculateNodeWidth(child);
        const childNodeHeight = this.calculateNodeHeight(child);

        child.bounds = {
          x: childX - childNodeWidth / 2,
          y: childY - childNodeHeight / 2,
          width: childNodeWidth,
          height: childNodeHeight,
        };

        // Recursively layout grandchildren
        this.calculateRadialLayout(child, childX, childY, childRadius);
      }
    }
  }

  /**
     * Calculate node width based on content
     */
  private calculateNodeWidth(node: InheritanceTreeNode): number {
    const baseWidth = 120;
    const textLength = node.grammarName.length + (node.ruleName?.length || 0);
    return Math.max(baseWidth, textLength * 8 + 20);
  }

  /**
     * Calculate node height based on content
     */
  private calculateNodeHeight(node: InheritanceTreeNode): number {
    const baseHeight = 40;
    const hasRuleName = !!node.ruleName;
    const hasMetadata = this.config.showMetrics;

    let height = baseHeight;
    if (hasRuleName) {
      height += 20;
    }
    if (hasMetadata) {
      height += 30;
    }

    return height;
  }

  /**
     * Create inheritance flows
     */
  private createInheritanceFlows(
    tree: InheritanceTreeNode,
    analyzedGrammar: AnalyzedGrammar,
  ): InheritanceFlow[] {
    const cacheKey = `${analyzedGrammar.name}_flows`;

    if (this.flowCache.has(cacheKey)) {
      return this.flowCache.get(cacheKey)!;
    }

    const flows: InheritanceFlow[] = [];

    // Create flows between parent and children
    this.createNodeFlows(tree, flows);

    this.flowCache.set(cacheKey, flows);
    return flows;
  }

  /**
     * Create flows for a node and its children
     */
  private createNodeFlows(node: InheritanceTreeNode, flows: InheritanceFlow[]): void {
    for (const child of node.children) {
      const flow = this.createFlow(node, child);
      flows.push(flow);

      // Recursively create flows for children
      this.createNodeFlows(child, flows);
    }
  }

  /**
     * Create a single inheritance flow
     */
  private createFlow(from: InheritanceTreeNode, to: InheritanceTreeNode): InheritanceFlow {
    const flowType = this.determineFlowType(from, to);
    const path = this.calculateFlowPath(from, to);
    const style = this.getFlowStyle(flowType, to.metadata.confidence);

    return {
      id: `flow_${from.id}_${to.id}`,
      from,
      to,
      type: flowType,
      strength: to.metadata.confidence,
      path,
      style,
    };
  }

  /**
     * Determine flow type between nodes
     */
  private determineFlowType(from: InheritanceTreeNode, to: InheritanceTreeNode): 'inherits' | 'overrides' | 'extends' {
    if (to.metadata.isOverridden) {
      return 'overrides';
    }
    if (to.metadata.isInherited) {
      return 'inherits';
    }
    return 'extends';
  }

  /**
     * Calculate flow path between nodes
     */
  private calculateFlowPath(from: InheritanceTreeNode, to: InheritanceTreeNode): Point[] {
    const fromCenter = {
      x: from.bounds.x + from.bounds.width / 2,
      y: from.bounds.y + from.bounds.height / 2,
    };

    const toCenter = {
      x: to.bounds.x + to.bounds.width / 2,
      y: to.bounds.y + to.bounds.height / 2,
    };

    // Create curved path for better visual appeal
    const midX = (fromCenter.x + toCenter.x) / 2;
    const midY = (fromCenter.y + toCenter.y) / 2;

    // Add curve control points
    const controlPoint1 = {
      x: fromCenter.x + (midX - fromCenter.x) * 0.5,
      y: fromCenter.y,
    };

    const controlPoint2 = {
      x: toCenter.x - (toCenter.x - midX) * 0.5,
      y: toCenter.y,
    };

    return [fromCenter, controlPoint1, controlPoint2, toCenter];
  }

  /**
     * Get flow style based on type and strength
     */
  private getFlowStyle(type: 'inherits' | 'overrides' | 'extends', strength: number): any {
    const baseStyle = {
      width: Math.max(1, strength * 3),
      opacity: Math.max(0.3, strength),
    };

    switch (type) {
      case 'inherits':
        return {
          ...baseStyle,
          color: '#4169e1',
          dashArray: '8,4',
        };
      case 'overrides':
        return {
          ...baseStyle,
          color: '#d73a49',
          dashArray: '4,2',
        };
      case 'extends':
        return {
          ...baseStyle,
          color: '#28a745',
          dashArray: undefined,
        };
    }
  }

  /**
     * Convert tree to railroad elements
     */
  private convertTreeToElements(tree: InheritanceTreeNode): RailroadElement[] {
    const elements: RailroadElement[] = [];
    this.convertNodeToElement(tree, elements);
    return elements;
  }

  /**
     * Convert a single node to railroad element
     */
  private convertNodeToElement(node: InheritanceTreeNode, elements: RailroadElement[]): void {
    const element: RailroadElement = {
      id: node.id,
      type: node.metadata.isBaseGrammar ? 'non-terminal' : 'terminal',
      name: node.grammarName,
      content: node.ruleName,
      parent: undefined,
      children: [],
      bounds: node.bounds,
      style: this.getNodeStyle(node),
      entryPoint: { x: node.bounds.x, y: node.bounds.y + node.bounds.height / 2 },
      exitPoint: { x: node.bounds.x + node.bounds.width, y: node.bounds.y + node.bounds.height / 2 },
      connectionPoints: [],
      metadata: {
        inheritanceNode: node,
        grammarName: node.grammarName,
        ruleName: node.ruleName,
        inheritanceLevel: node.level,
        isBaseGrammar: node.metadata.isBaseGrammar,
        isOverridden: node.metadata.isOverridden,
        isInherited: node.metadata.isInherited,
        confidence: node.metadata.confidence,
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

    elements.push(element);

    // Convert children
    for (const child of node.children) {
      this.convertNodeToElement(child, elements);
    }
  }

  /**
     * Get node style based on metadata
     */
  private getNodeStyle(node: InheritanceTreeNode): any {
    const baseStyle = {
      borderWidth: 2,
      borderRadius: 8,
      padding: { top: 8, right: 12, bottom: 8, left: 12 },
    };

    if (node.metadata.isBaseGrammar) {
      return {
        ...baseStyle,
        backgroundColor: '#e3f2fd',
        borderColor: '#1976d2',
        textColor: '#0d47a1',
      };
    }

    if (node.metadata.isOverridden) {
      return {
        ...baseStyle,
        backgroundColor: '#ffebee',
        borderColor: '#d32f2f',
        textColor: '#b71c1c',
        strokeDashArray: '4,2',
      };
    }

    if (node.metadata.isInherited) {
      return {
        ...baseStyle,
        backgroundColor: '#f3e5f5',
        borderColor: '#7b1fa2',
        textColor: '#4a148c',
        strokeDashArray: '8,4',
      };
    }

    return {
      ...baseStyle,
      backgroundColor: '#e8f5e8',
      borderColor: '#388e3c',
      textColor: '#1b5e20',
    };
  }

  /**
     * Convert flows to railroad connections
     */
  private convertFlowsToConnections(flows: InheritanceFlow[]): RailroadConnection[] {
    return flows.map(flow => ({
      id: flow.id,
      from: {
        element: { id: flow.from.id } as RailroadElement,
        point: flow.path[0],
      },
      to: {
        element: { id: flow.to.id } as RailroadElement,
        point: flow.path[flow.path.length - 1],
      },
      path: flow.path,
      style: {
        lineColor: flow.style.color,
        strokeWidth: flow.style.width,
        strokeDashArray: flow.style.dashArray,
        opacity: flow.style.opacity,
      },
      type: 'branch',
    }));
  }

  /**
     * Highlight inheritance path for a specific rule
     */
  public highlightInheritancePath(
    tree: InheritanceTreeNode,
    ruleName: string,
  ): { nodes: InheritanceTreeNode[]; flows: InheritanceFlow[] } {
    const pathNodes: InheritanceTreeNode[] = [];
    const pathFlows: InheritanceFlow[] = [];

    // Find the rule node
    const ruleNode = this.findNodeByRule(tree, ruleName);
    if (!ruleNode) {
      return { nodes: pathNodes, flows: pathFlows };
    }

    // Trace path to root
    let currentNode: InheritanceTreeNode | undefined = ruleNode;
    while (currentNode) {
      pathNodes.unshift(currentNode);
      currentNode = currentNode.parent;
    }

    // Find flows between path nodes
    for (let i = 0; i < pathNodes.length - 1; i++) {
      const flow = this.findFlowBetweenNodes(pathNodes[i], pathNodes[i + 1]);
      if (flow) {
        pathFlows.push(flow);
      }
    }

    return { nodes: pathNodes, flows: pathFlows };
  }

  /**
     * Find node by rule name
     */
  private findNodeByRule(node: InheritanceTreeNode, ruleName: string): InheritanceTreeNode | null {
    if (node.ruleName === ruleName) {
      return node;
    }

    for (const child of node.children) {
      const found = this.findNodeByRule(child, ruleName);
      if (found) {
        return found;
      }
    }

    return null;
  }

  /**
     * Find flow between two nodes
     */
  private findFlowBetweenNodes(from: InheritanceTreeNode, to: InheritanceTreeNode): InheritanceFlow | null {
    // This would search through the flows cache
    // For now, return null as placeholder
    return null;
  }

  /**
     * Update configuration
     */
  public updateConfig(config: Partial<InheritanceVisualizationConfig>): void {
    this.config = { ...this.config, ...config };
    this.clearCache();
  }

  /**
     * Clear caches
     */
  public clearCache(): void {
    this.treeCache.clear();
    this.flowCache.clear();
  }

  /**
     * Get visualization statistics
     */
  public getStats(): {
        treeCacheSize: number;
        flowCacheSize: number;
        config: InheritanceVisualizationConfig;
        } {
    return {
      treeCacheSize: this.treeCache.size,
      flowCacheSize: this.flowCache.size,
      config: this.config,
    };
  }
}

