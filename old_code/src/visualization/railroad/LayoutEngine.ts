/**
 * Layout Engine for Railroad Diagram Positioning
 *
 * This module provides sophisticated layout algorithms for positioning
 * railroad diagram elements with optimal spacing, alignment, and visual flow.
 */

import {
  RailroadElement,
  RailroadConnection,
  RailroadDiagram,
  RailroadLayout,
  Point,
  Size,
  Rectangle,
  RailroadElementType,
} from './RailroadTypes';

/**
 * Layout calculation result
 */
interface LayoutResult {
    elements: Map<string, Rectangle>;
    connections: RailroadConnection[];
    totalBounds: Rectangle;
    metrics: {
        layoutTime: number;
        elementCount: number;
        connectionCount: number;
        maxDepth: number;
    };
}

/**
 * Layout context for recursive calculations
 */
interface LayoutContext {
    currentX: number;
    currentY: number;
    maxWidth: number;
    maxHeight: number;
    depth: number;
    parentElement?: RailroadElement;
}

/**
 * Core Layout Engine
 */
export class LayoutEngine {
  private layout: RailroadLayout;
  private elementSizes: Map<string, Size>;
  private layoutCache: Map<string, LayoutResult>;

  constructor(layout: RailroadLayout) {
    this.layout = layout;
    this.elementSizes = new Map();
    this.layoutCache = new Map();
  }

  /**
     * Calculate layout for a complete railroad diagram
     */
  public calculateLayout(diagram: RailroadDiagram): LayoutResult {
    const startTime = Date.now();

    // Check cache first
    const cacheKey = this.generateCacheKey(diagram);
    if (this.layoutCache.has(cacheKey)) {
      return this.layoutCache.get(cacheKey)!;
    }

    // Initialize layout context
    const context: LayoutContext = {
      currentX: this.layout.horizontalSpacing,
      currentY: this.layout.verticalSpacing,
      maxWidth: 0,
      maxHeight: 0,
      depth: 0,
    };

    // Calculate element sizes first
    this.calculateElementSizes(diagram.elements);

    // Perform layout calculation
    const elementPositions = new Map<string, Rectangle>();
    const connections: RailroadConnection[] = [];

    // Layout root elements
    for (const element of diagram.elements) {
      if (!element.parent) {
        this.layoutElement(element, context, elementPositions, connections);
      }
    }

    // Calculate total bounds
    const totalBounds = this.calculateTotalBounds(elementPositions);

    // Create result
    const result: LayoutResult = {
      elements: elementPositions,
      connections,
      totalBounds,
      metrics: {
        layoutTime: Date.now() - startTime,
        elementCount: diagram.elements.length,
        connectionCount: connections.length,
        maxDepth: this.calculateMaxDepth(diagram.elements),
      },
    };

    // Cache result
    this.layoutCache.set(cacheKey, result);

    return result;
  }

  /**
     * Layout a single element and its children
     */
  private layoutElement(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): Rectangle {
    const size = this.elementSizes.get(element.id) || { width: 100, height: 30 };

    // Calculate position based on element type
    const bounds = this.calculateElementBounds(element, size, context);
    positions.set(element.id, bounds);

    // Update element bounds
    element.bounds = bounds;
    element.entryPoint = { x: bounds.x, y: bounds.y + bounds.height / 2 };
    element.exitPoint = { x: bounds.x + bounds.width, y: bounds.y + bounds.height / 2 };

    // Layout children based on element type
    switch (element.type) {
      case 'sequence':
        this.layoutSequence(element, context, positions, connections);
        break;
      case 'choice':
        this.layoutChoice(element, context, positions, connections);
        break;
      case 'optional':
        this.layoutOptional(element, context, positions, connections);
        break;
      case 'repetition':
        this.layoutRepetition(element, context, positions, connections);
        break;
      case 'group':
        this.layoutGroup(element, context, positions, connections);
        break;
      default:
        // Terminal and non-terminal elements don't have special child layout
        break;
    }

    return bounds;
  }

  /**
     * Layout sequence elements horizontally
     */
  private layoutSequence(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): void {
    if (!element.children.length) {
      return;
    }

    let currentX = element.bounds.x + this.layout.elementSpacing;
    const baseY = element.bounds.y;

    for (let i = 0; i < element.children.length; i++) {
      const child = element.children[i];
      const childContext: LayoutContext = {
        ...context,
        currentX,
        currentY: baseY,
        depth: context.depth + 1,
        parentElement: element,
      };

      const childBounds = this.layoutElement(child, childContext, positions, connections);

      // Create connection from previous element
      if (i > 0) {
        const prevChild = element.children[i - 1];
        this.createConnection(prevChild, child, connections, 'normal');
      }

      currentX = childBounds.x + childBounds.width + this.layout.elementSpacing;
    }

    // Update element bounds to encompass children
    this.updateElementBoundsForChildren(element, positions);
  }

  /**
     * Layout choice elements vertically with branches
     */
  private layoutChoice(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): void {
    if (!element.children.length) {
      return;
    }

    const choiceSpacing = this.layout.verticalSpacing * 2;
    const currentY = element.bounds.y;

    // Calculate total height needed
    const totalHeight = element.children.length * choiceSpacing;
    const startY = currentY - totalHeight / 2;

    for (let i = 0; i < element.children.length; i++) {
      const child = element.children[i];
      const childY = startY + i * choiceSpacing;

      const childContext: LayoutContext = {
        ...context,
        currentX: element.bounds.x + element.bounds.width + this.layout.horizontalSpacing,
        currentY: childY,
        depth: context.depth + 1,
        parentElement: element,
      };

      const childBounds = this.layoutElement(child, childContext, positions, connections);

      // Create branch connection
      this.createBranchConnection(element, child, connections, i, element.children.length);
    }

    // Update element bounds
    this.updateElementBoundsForChildren(element, positions);
  }

  /**
     * Layout optional elements with skip path
     */
  private layoutOptional(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): void {
    if (!element.children.length) {
      return;
    }

    const child = element.children[0];
    const childContext: LayoutContext = {
      ...context,
      currentX: element.bounds.x + this.layout.horizontalSpacing,
      currentY: element.bounds.y,
      depth: context.depth + 1,
      parentElement: element,
    };

    const childBounds = this.layoutElement(child, childContext, positions, connections);

    // Create main path connection
    this.createConnection(element, child, connections, 'normal');

    // Create skip path connection
    this.createSkipConnection(element, child, connections);

    // Update element bounds
    this.updateElementBoundsForChildren(element, positions);
  }

  /**
     * Layout repetition elements with loop path
     */
  private layoutRepetition(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): void {
    if (!element.children.length) {
      return;
    }

    const child = element.children[0];
    const childContext: LayoutContext = {
      ...context,
      currentX: element.bounds.x + this.layout.horizontalSpacing,
      currentY: element.bounds.y,
      depth: context.depth + 1,
      parentElement: element,
    };

    const childBounds = this.layoutElement(child, childContext, positions, connections);

    // Create forward connection
    this.createConnection(element, child, connections, 'normal');

    // Create loop back connection
    this.createLoopConnection(child, element, connections);

    // Update element bounds
    this.updateElementBoundsForChildren(element, positions);
  }

  /**
     * Layout group elements with visual grouping
     */
  private layoutGroup(
    element: RailroadElement,
    context: LayoutContext,
    positions: Map<string, Rectangle>,
    connections: RailroadConnection[],
  ): void {
    if (!element.children.length) {
      return;
    }

    // Groups are laid out like sequences but with visual grouping
    this.layoutSequence(element, context, positions, connections);

    // Add visual grouping indicators
    element.style = {
      ...element.style,
      borderWidth: 2,
      borderColor: '#cccccc',
      backgroundColor: 'rgba(240, 240, 240, 0.5)',
    };
  }

  /**
     * Calculate element bounds based on type and context
     */
  private calculateElementBounds(
    element: RailroadElement,
    size: Size,
    context: LayoutContext,
  ): Rectangle {
    const x = context.currentX;
    let y = context.currentY;

    // Adjust position based on element type
    switch (element.type) {
      case 'choice':
        // Center choice elements vertically
        y = context.currentY - size.height / 2;
        break;
      case 'optional':
      case 'repetition':
        // Add extra spacing for optional and repetition elements
        y = context.currentY + this.layout.verticalSpacing;
        break;
    }

    // Apply minimum dimensions
    const width = Math.max(size.width, this.layout.minElementWidth);
    const height = Math.max(size.height, this.layout.minElementHeight);

    return { x, y, width, height };
  }

  /**
     * Calculate sizes for all elements
     */
  private calculateElementSizes(elements: RailroadElement[]): void {
    for (const element of elements) {
      const size = this.calculateElementSize(element);
      this.elementSizes.set(element.id, size);
    }
  }

  /**
     * Calculate size for a single element
     */
  private calculateElementSize(element: RailroadElement): Size {
    const baseWidth = this.layout.minElementWidth;
    const baseHeight = this.layout.minElementHeight;

    // Calculate text dimensions
    const textWidth = this.estimateTextWidth(element.name || element.content || '', element.style);
    const textHeight = this.estimateTextHeight(element.style);

    // Add padding
    const padding = element.style.padding || { top: 8, right: 16, bottom: 8, left: 16 };
    const width = Math.max(baseWidth, textWidth + padding.left + padding.right);
    const height = Math.max(baseHeight, textHeight + padding.top + padding.bottom);

    // Apply type-specific adjustments
    switch (element.type) {
      case 'choice':
        return { width: width * 1.2, height: height * 1.5 };
      case 'optional':
        return { width: width * 1.1, height: height * 1.3 };
      case 'repetition':
        return { width: width * 1.3, height: height * 1.4 };
      case 'group':
        return { width: width * 1.5, height: height * 1.2 };
      default:
        return { width, height };
    }
  }

  /**
     * Estimate text width (simplified calculation)
     */
  private estimateTextWidth(text: string, style: any): number {
    const fontSize = style.fontSize || 14;
    const avgCharWidth = fontSize * 0.6; // Approximate character width
    return text.length * avgCharWidth;
  }

  /**
     * Estimate text height
     */
  private estimateTextHeight(style: any): number {
    return (style.fontSize || 14) * 1.2; // Add line height
  }

  /**
     * Create a connection between two elements
     */
  private createConnection(
    from: RailroadElement,
    to: RailroadElement,
    connections: RailroadConnection[],
    type: 'normal' | 'skip' | 'loop' | 'branch',
  ): void {
    const connection: RailroadConnection = {
      id: `${from.id}-${to.id}-${type}`,
      from: {
        element: from,
        point: from.exitPoint,
      },
      to: {
        element: to,
        point: to.entryPoint,
      },
      path: this.calculateConnectionPath(from.exitPoint, to.entryPoint, type),
      style: this.getConnectionStyle(type),
      type,
    };

    connections.push(connection);
  }

  /**
     * Create a branch connection for choice elements
     */
  private createBranchConnection(
    choice: RailroadElement,
    branch: RailroadElement,
    connections: RailroadConnection[],
    branchIndex: number,
    totalBranches: number,
  ): void {
    // Calculate branch point
    const branchPoint: Point = {
      x: choice.bounds.x + choice.bounds.width,
      y: choice.bounds.y + choice.bounds.height / 2,
    };

    const connection: RailroadConnection = {
      id: `${choice.id}-${branch.id}-branch-${branchIndex}`,
      from: {
        element: choice,
        point: branchPoint,
      },
      to: {
        element: branch,
        point: branch.entryPoint,
      },
      path: this.calculateBranchPath(branchPoint, branch.entryPoint, branchIndex, totalBranches),
      style: this.getConnectionStyle('branch'),
      type: 'branch',
    };

    connections.push(connection);
  }

  /**
     * Create a skip connection for optional elements
     */
  private createSkipConnection(
    optional: RailroadElement,
    child: RailroadElement,
    connections: RailroadConnection[],
  ): void {
    const skipStartPoint: Point = {
      x: optional.bounds.x + optional.bounds.width,
      y: optional.bounds.y + optional.bounds.height / 2 - this.layout.verticalSpacing,
    };

    const skipEndPoint: Point = {
      x: child.bounds.x + child.bounds.width,
      y: child.bounds.y + child.bounds.height / 2 - this.layout.verticalSpacing,
    };

    const connection: RailroadConnection = {
      id: `${optional.id}-skip`,
      from: {
        element: optional,
        point: skipStartPoint,
      },
      to: {
        element: child,
        point: skipEndPoint,
      },
      path: this.calculateSkipPath(skipStartPoint, skipEndPoint),
      style: this.getConnectionStyle('skip'),
      type: 'skip',
    };

    connections.push(connection);
  }

  /**
     * Create a loop connection for repetition elements
     */
  private createLoopConnection(
    child: RailroadElement,
    repetition: RailroadElement,
    connections: RailroadConnection[],
  ): void {
    const loopStartPoint: Point = {
      x: child.bounds.x + child.bounds.width,
      y: child.bounds.y + child.bounds.height / 2 + this.layout.verticalSpacing,
    };

    const loopEndPoint: Point = {
      x: repetition.bounds.x,
      y: repetition.bounds.y + repetition.bounds.height / 2 + this.layout.verticalSpacing,
    };

    const connection: RailroadConnection = {
      id: `${child.id}-loop`,
      from: {
        element: child,
        point: loopStartPoint,
      },
      to: {
        element: repetition,
        point: loopEndPoint,
      },
      path: this.calculateLoopPath(loopStartPoint, loopEndPoint),
      style: this.getConnectionStyle('loop'),
      type: 'loop',
    };

    connections.push(connection);
  }

  /**
     * Calculate connection path between two points
     */
  private calculateConnectionPath(
    from: Point,
    to: Point,
    type: 'normal' | 'skip' | 'loop' | 'branch',
  ): Point[] {
    switch (this.layout.lineRouting) {
      case 'straight':
        return [from, to];
      case 'curved':
        return this.calculateCurvedPath(from, to);
      case 'orthogonal':
        return this.calculateOrthogonalPath(from, to);
      default:
        return [from, to];
    }
  }

  /**
     * Calculate curved path between two points
     */
  private calculateCurvedPath(from: Point, to: Point): Point[] {
    const midX = (from.x + to.x) / 2;
    const controlPoint1: Point = { x: midX, y: from.y };
    const controlPoint2: Point = { x: midX, y: to.y };

    return [from, controlPoint1, controlPoint2, to];
  }

  /**
     * Calculate orthogonal path between two points
     */
  private calculateOrthogonalPath(from: Point, to: Point): Point[] {
    const midX = from.x + (to.x - from.x) / 2;

    return [
      from,
      { x: midX, y: from.y },
      { x: midX, y: to.y },
      to,
    ];
  }

  /**
     * Calculate branch path for choice elements
     */
  private calculateBranchPath(
    from: Point,
    to: Point,
    branchIndex: number,
    totalBranches: number,
  ): Point[] {
    // Create curved branch paths
    const branchOffset = (branchIndex - (totalBranches - 1) / 2) * this.layout.verticalSpacing;
    const midX = from.x + this.layout.horizontalSpacing / 2;

    return [
      from,
      { x: midX, y: from.y },
      { x: midX, y: from.y + branchOffset },
      { x: to.x - this.layout.horizontalSpacing / 2, y: to.y },
      to,
    ];
  }

  /**
     * Calculate skip path for optional elements
     */
  private calculateSkipPath(from: Point, to: Point): Point[] {
    const arcHeight = this.layout.verticalSpacing;
    const midX = (from.x + to.x) / 2;

    return [
      from,
      { x: from.x + 20, y: from.y - arcHeight },
      { x: midX, y: from.y - arcHeight },
      { x: to.x - 20, y: to.y - arcHeight },
      to,
    ];
  }

  /**
     * Calculate loop path for repetition elements
     */
  private calculateLoopPath(from: Point, to: Point): Point[] {
    const loopHeight = this.layout.verticalSpacing * 1.5;

    return [
      from,
      { x: from.x + 20, y: from.y + loopHeight },
      { x: to.x - 20, y: to.y + loopHeight },
      to,
    ];
  }

  /**
     * Get connection style based on type
     */
  private getConnectionStyle(type: 'normal' | 'skip' | 'loop' | 'branch'): any {
    const baseStyle = {
      strokeWidth: 2,
      lineColor: '#333333',
    };

    switch (type) {
      case 'skip':
        return {
          ...baseStyle,
          strokeDashArray: '5,5',
          lineColor: '#666666',
        };
      case 'loop':
        return {
          ...baseStyle,
          lineColor: '#0066cc',
        };
      case 'branch':
        return {
          ...baseStyle,
          lineColor: '#cc6600',
        };
      default:
        return baseStyle;
    }
  }

  /**
     * Update element bounds to encompass children
     */
  private updateElementBoundsForChildren(
    element: RailroadElement,
    positions: Map<string, Rectangle>,
  ): void {
    if (!element.children.length) {
      return;
    }

    let minX = element.bounds.x;
    let minY = element.bounds.y;
    let maxX = element.bounds.x + element.bounds.width;
    let maxY = element.bounds.y + element.bounds.height;

    for (const child of element.children) {
      const childBounds = positions.get(child.id);
      if (childBounds) {
        minX = Math.min(minX, childBounds.x);
        minY = Math.min(minY, childBounds.y);
        maxX = Math.max(maxX, childBounds.x + childBounds.width);
        maxY = Math.max(maxY, childBounds.y + childBounds.height);
      }
    }

    // Add padding around children
    const padding = this.layout.elementSpacing;
    element.bounds = {
      x: minX - padding,
      y: minY - padding,
      width: maxX - minX + 2 * padding,
      height: maxY - minY + 2 * padding,
    };

    // Update connection points
    element.entryPoint = { x: element.bounds.x, y: element.bounds.y + element.bounds.height / 2 };
    element.exitPoint = { x: element.bounds.x + element.bounds.width, y: element.bounds.y + element.bounds.height / 2 };
  }

  /**
     * Calculate total bounds encompassing all elements
     */
  private calculateTotalBounds(positions: Map<string, Rectangle>): Rectangle {
    if (positions.size === 0) {
      return { x: 0, y: 0, width: 0, height: 0 };
    }

    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;

    for (const bounds of positions.values()) {
      minX = Math.min(minX, bounds.x);
      minY = Math.min(minY, bounds.y);
      maxX = Math.max(maxX, bounds.x + bounds.width);
      maxY = Math.max(maxY, bounds.y + bounds.height);
    }

    return {
      x: minX,
      y: minY,
      width: maxX - minX,
      height: maxY - minY,
    };
  }

  /**
     * Calculate maximum depth of element hierarchy
     */
  private calculateMaxDepth(elements: RailroadElement[]): number {
    let maxDepth = 0;

    function calculateDepth(element: RailroadElement, currentDepth: number): number {
      let depth = currentDepth;
      for (const child of element.children) {
        depth = Math.max(depth, calculateDepth(child, currentDepth + 1));
      }
      return depth;
    }

    for (const element of elements) {
      if (!element.parent) {
        maxDepth = Math.max(maxDepth, calculateDepth(element, 1));
      }
    }

    return maxDepth;
  }

  /**
     * Generate cache key for layout result
     */
  private generateCacheKey(diagram: RailroadDiagram): string {
    const elementIds = diagram.elements.map(e => e.id).sort().join(',');
    const layoutHash = JSON.stringify(this.layout);
    return `${diagram.id}_${elementIds}_${this.hashString(layoutHash)}`;
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
     * Clear layout cache
     */
  public clearCache(): void {
    this.layoutCache.clear();
  }

  /**
     * Update layout configuration
     */
  public updateLayout(newLayout: Partial<RailroadLayout>): void {
    this.layout = { ...this.layout, ...newLayout };
    this.clearCache(); // Clear cache when layout changes
  }

  /**
     * Get layout statistics
     */
  public getLayoutStats(): {
        cacheSize: number;
        elementSizesCached: number;
        } {
    return {
      cacheSize: this.layoutCache.size,
      elementSizesCached: this.elementSizes.size,
    };
  }
}

