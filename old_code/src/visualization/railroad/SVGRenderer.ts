/**
 * SVG Rendering Engine for Railroad Diagrams
 *
 * This module provides comprehensive SVG generation for railroad diagrams,
 * including advanced styling, animations, and interactive features.
 */

import {
  RailroadDiagram,
  RailroadElement,
  RailroadConnection,
  RailroadTheme,
  RailroadStyle,
  Point,
  Rectangle,
  RailroadExportResult,
} from './RailroadTypes';

/**
 * SVG generation options
 */
export interface SVGRenderOptions {
    // Output options
    includeStyles?: boolean;
    embedFonts?: boolean;
    includeInteractivity?: boolean;
    includeAnimations?: boolean;

    // Quality options
    precision?: number;
    optimizeOutput?: boolean;
    minifyOutput?: boolean;

    // Debug options
    includeDebugInfo?: boolean;
    showBounds?: boolean;
    showConnectionPoints?: boolean;

    // Accessibility options
    includeAriaLabels?: boolean;
    includeDescriptions?: boolean;
    includeKeyboardNavigation?: boolean;
}

/**
 * SVG element builder for creating SVG markup
 */
class SVGBuilder {
  private elements: string[] = [];
  private defs: string[] = [];
  private styles: string[] = [];
  private scripts: string[] = [];

  /**
     * Add an SVG element
     */
  public addElement(tag: string, attributes: Record<string, any> = {}, content?: string): void {
    const attrs = Object.entries(attributes)
      .map(([key, value]) => `${key}="${this.escapeAttribute(value)}"`)
      .join(' ');

    if (content !== undefined) {
      this.elements.push(`<${tag} ${attrs}>${this.escapeContent(content)}</${tag}>`);
    } else {
      this.elements.push(`<${tag} ${attrs}/>`);
    }
  }

  /**
     * Add a definition (for gradients, patterns, etc.)
     */
  public addDef(content: string): void {
    this.defs.push(content);
  }

  /**
     * Add CSS styles
     */
  public addStyle(css: string): void {
    this.styles.push(css);
  }

  /**
     * Add JavaScript
     */
  public addScript(js: string): void {
    this.scripts.push(js);
  }

  /**
     * Build the complete SVG
     */
  public build(width: number, height: number, options: SVGRenderOptions = {}): string {
    const svg: string[] = [];

    // SVG opening tag
    svg.push(`<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">`);

    // Add definitions
    if (this.defs.length > 0) {
      svg.push('<defs>');
      svg.push(...this.defs);
      svg.push('</defs>');
    }

    // Add styles
    if (this.styles.length > 0 && options.includeStyles) {
      svg.push('<style type="text/css">');
      svg.push('<![CDATA[');
      svg.push(...this.styles);
      svg.push(']]>');
      svg.push('</style>');
    }

    // Add elements
    svg.push(...this.elements);

    // Add scripts
    if (this.scripts.length > 0 && options.includeInteractivity) {
      svg.push('<script type="text/javascript">');
      svg.push('<![CDATA[');
      svg.push(...this.scripts);
      svg.push(']]>');
      svg.push('</script>');
    }

    svg.push('</svg>');

    let result = svg.join('\n');

    // Optimize output if requested
    if (options.optimizeOutput) {
      result = this.optimizeSVG(result);
    }

    // Minify output if requested
    if (options.minifyOutput) {
      result = this.minifySVG(result);
    }

    return result;
  }

  /**
     * Escape attribute values
     */
  private escapeAttribute(value: any): string {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }

  /**
     * Escape content
     */
  private escapeContent(content: string): string {
    return content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }

  /**
     * Optimize SVG output
     */
  private optimizeSVG(svg: string): string {
    // Remove unnecessary whitespace
    svg = svg.replace(/>\s+</g, '><');

    // Round numbers to reduce precision
    svg = svg.replace(/(\d+\.\d{3,})/g, (match) => {
      return parseFloat(match).toFixed(2);
    });

    return svg;
  }

  /**
     * Minify SVG output
     */
  private minifySVG(svg: string): string {
    // Remove comments
    svg = svg.replace(/<!--[\s\S]*?-->/g, '');

    // Remove unnecessary whitespace
    svg = svg.replace(/\s+/g, ' ').trim();

    return svg;
  }
}

/**
 * Core SVG Rendering Engine
 */
export class SVGRenderer {
  private theme: RailroadTheme;
  private options: SVGRenderOptions;

  constructor(theme: RailroadTheme, options: SVGRenderOptions = {}) {
    this.theme = theme;
    this.options = {
      includeStyles: true,
      embedFonts: false,
      includeInteractivity: true,
      includeAnimations: true,
      precision: 2,
      optimizeOutput: true,
      minifyOutput: false,
      includeDebugInfo: false,
      showBounds: false,
      showConnectionPoints: false,
      includeAriaLabels: true,
      includeDescriptions: true,
      includeKeyboardNavigation: true,
      ...options,
    };
  }

  /**
     * Render a complete railroad diagram to SVG
     */
  public renderDiagram(diagram: RailroadDiagram): RailroadExportResult {
    const startTime = Date.now();
    const builder = new SVGBuilder();

    // Add theme styles
    this.addThemeStyles(builder);

    // Add gradients and patterns
    this.addDefinitions(builder);

    // Render elements
    for (const element of diagram.elements) {
      this.renderElement(element, builder);
    }

    // Render connections
    for (const connection of diagram.connections) {
      this.renderConnection(connection, builder);
    }

    // Add interactivity if enabled
    if (this.options.includeInteractivity) {
      this.addInteractivity(builder, diagram);
    }

    // Add animations if enabled
    if (this.options.includeAnimations) {
      this.addAnimations(builder, diagram);
    }

    // Add debug information if enabled
    if (this.options.includeDebugInfo) {
      this.addDebugInfo(builder, diagram);
    }

    // Build final SVG
    const svg = builder.build(diagram.bounds.width, diagram.bounds.height, this.options);

    return {
      content: svg,
      format: 'svg',
      metadata: {
        size: { width: diagram.bounds.width, height: diagram.bounds.height },
        elementCount: diagram.elements.length,
        connectionCount: diagram.connections.length,
        generationTime: Date.now() - startTime,
        theme: this.theme.name,
        interactive: this.options.includeInteractivity || false,
        animated: this.options.includeAnimations || false,
      },
    };
  }

  /**
     * Add theme styles to the SVG
     */
  private addThemeStyles(builder: SVGBuilder): void {
    const css: string[] = [];

    // Global styles
    css.push(`
            .railroad-diagram {
                font-family: ${this.theme.global.fontFamily};
                font-size: ${this.theme.global.fontSize}px;
                background-color: ${this.theme.global.backgroundColor};
                color: ${this.theme.global.textColor};
            }
        `);

    // Element styles
    for (const [elementType, style] of Object.entries(this.theme.elements)) {
      css.push(this.generateElementCSS(elementType, style));
    }

    // State styles
    for (const [state, style] of Object.entries(this.theme.states)) {
      css.push(this.generateStateCSS(state, style));
    }

    // Minotaur-specific styles
    for (const [type, style] of Object.entries(this.theme.grammarForge)) {
      css.push(this.generateMinotaurCSS(type, style));
    }

    builder.addStyle(css.join('\n'));
  }

  /**
     * Generate CSS for element types
     */
  private generateElementCSS(elementType: string, style: RailroadStyle): string {
    const className = `.railroad-${elementType}`;
    const properties: string[] = [];

    if (style.backgroundColor) {
      properties.push(`fill: ${style.backgroundColor}`);
    }
    if (style.borderColor) {
      properties.push(`stroke: ${style.borderColor}`);
    }
    if (style.textColor) {
      properties.push(`color: ${style.textColor}`);
    }
    if (style.borderWidth) {
      properties.push(`stroke-width: ${style.borderWidth}`);
    }
    if (style.opacity) {
      properties.push(`opacity: ${style.opacity}`);
    }

    return `${className} { ${properties.join('; ')} }`;
  }

  /**
     * Generate CSS for states
     */
  private generateStateCSS(state: string, style: Partial<RailroadStyle>): string {
    const className = `.railroad-element:${state}`;
    const properties: string[] = [];

    if (style.backgroundColor) {
      properties.push(`fill: ${style.backgroundColor}`);
    }
    if (style.borderColor) {
      properties.push(`stroke: ${style.borderColor}`);
    }
    if (style.opacity) {
      properties.push(`opacity: ${style.opacity}`);
    }

    return `${className} { ${properties.join('; ')} }`;
  }

  /**
     * Generate CSS for Minotaur-specific features
     */
  private generateMinotaurCSS(type: string, style: Partial<RailroadStyle>): string {
    const className = `.railroad-${type}`;
    const properties: string[] = [];

    if (style.backgroundColor) {
      properties.push(`fill: ${style.backgroundColor}`);
    }
    if (style.borderColor) {
      properties.push(`stroke: ${style.borderColor}`);
    }
    if (style.borderWidth) {
      properties.push(`stroke-width: ${style.borderWidth}`);
    }

    return `${className} { ${properties.join('; ')} }`;
  }

  /**
     * Add SVG definitions (gradients, patterns, etc.)
     */
  private addDefinitions(builder: SVGBuilder): void {
    // Add gradient definitions
    builder.addDef(`
            <linearGradient id="terminalGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" style="stop-color:#f0f0f0;stop-opacity:1" />
                <stop offset="100%" style="stop-color:#e0e0e0;stop-opacity:1" />
            </linearGradient>
        `);

    builder.addDef(`
            <linearGradient id="nonTerminalGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" style="stop-color:#e8f4fd;stop-opacity:1" />
                <stop offset="100%" style="stop-color:#d1e7f7;stop-opacity:1" />
            </linearGradient>
        `);

    // Add arrow marker for connections
    builder.addDef(`
            <marker id="arrowhead" markerWidth="10" markerHeight="7" 
                    refX="9" refY="3.5" orient="auto">
                <polygon points="0 0, 10 3.5, 0 7" fill="${this.theme.global.lineColor}" />
            </marker>
        `);

    // Add patterns for special elements
    builder.addDef(`
            <pattern id="inheritedPattern" patternUnits="userSpaceOnUse" width="4" height="4">
                <rect width="4" height="4" fill="#f0f8ff"/>
                <path d="M 0,4 l 4,-4 M -1,1 l 2,-2 M 3,5 l 2,-2" stroke="#4169e1" stroke-width="0.5"/>
            </pattern>
        `);
  }

  /**
     * Render a single element
     */
  private renderElement(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;
    const classes = this.getElementClasses(element);
    const style = this.getElementStyle(element);

    // Create group for element
    builder.addElement('g', {
      id: element.id,
      class: classes.join(' '),
      transform: `translate(${bounds.x}, ${bounds.y})`,
    });

    // Render based on element type
    switch (element.type) {
      case 'terminal':
        this.renderTerminal(element, builder);
        break;
      case 'non-terminal':
        this.renderNonTerminal(element, builder);
        break;
      case 'choice':
        this.renderChoice(element, builder);
        break;
      case 'sequence':
        this.renderSequence(element, builder);
        break;
      case 'optional':
        this.renderOptional(element, builder);
        break;
      case 'repetition':
        this.renderRepetition(element, builder);
        break;
      case 'group':
        this.renderGroup(element, builder);
        break;
      case 'start':
        this.renderStart(element, builder);
        break;
      case 'end':
        this.renderEnd(element, builder);
        break;
    }

    // Add accessibility attributes
    if (this.options.includeAriaLabels) {
      this.addAccessibilityAttributes(element, builder);
    }

    // Close group
    builder.addElement('/g');
  }

  /**
     * Render terminal element
     */
  private renderTerminal(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;
    const radius = Math.min(bounds.width, bounds.height) * 0.2;

    // Background rectangle with rounded corners
    builder.addElement('rect', {
      x: 0,
      y: 0,
      width: bounds.width,
      height: bounds.height,
      rx: radius,
      ry: radius,
      fill: 'url(#terminalGradient)',
      stroke: this.theme.elements.terminal.borderColor,
      'stroke-width': this.theme.elements.terminal.borderWidth || 1,
    });

    // Text content
    if (element.content || element.name) {
      builder.addElement('text', {
        x: bounds.width / 2,
        y: bounds.height / 2,
        'text-anchor': 'middle',
        'dominant-baseline': 'central',
        fill: this.theme.elements.terminal.textColor,
      }, element.content || element.name);
    }
  }

  /**
     * Render non-terminal element
     */
  private renderNonTerminal(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;

    // Background rectangle
    builder.addElement('rect', {
      x: 0,
      y: 0,
      width: bounds.width,
      height: bounds.height,
      fill: 'url(#nonTerminalGradient)',
      stroke: this.theme.elements.nonTerminal.borderColor,
      'stroke-width': this.theme.elements.nonTerminal.borderWidth || 1,
    });

    // Text content
    if (element.name) {
      builder.addElement('text', {
        x: bounds.width / 2,
        y: bounds.height / 2,
        'text-anchor': 'middle',
        'dominant-baseline': 'central',
        fill: this.theme.elements.nonTerminal.textColor,
        'font-style': 'italic',
      }, element.name);
    }
  }

  /**
     * Render choice element
     */
  private renderChoice(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;

    // Choice diamond shape
    const centerX = bounds.width / 2;
    const centerY = bounds.height / 2;
    const points = [
      `${centerX},0`,
      `${bounds.width},${centerY}`,
      `${centerX},${bounds.height}`,
      `0,${centerY}`,
    ].join(' ');

    builder.addElement('polygon', {
      points,
      fill: this.theme.elements.choice.backgroundColor,
      stroke: this.theme.elements.choice.borderColor,
      'stroke-width': this.theme.elements.choice.borderWidth || 1,
    });

    // Choice indicator
    builder.addElement('text', {
      x: centerX,
      y: centerY,
      'text-anchor': 'middle',
      'dominant-baseline': 'central',
      fill: this.theme.elements.choice.textColor,
      'font-size': '12px',
    }, '?');
  }

  /**
     * Render sequence element
     */
  private renderSequence(element: RailroadElement, builder: SVGBuilder): void {
    // Sequences are typically rendered as their children
    // Add a subtle background if needed
    if (element.children.length > 1) {
      const bounds = element.bounds;
      builder.addElement('rect', {
        x: 0,
        y: 0,
        width: bounds.width,
        height: bounds.height,
        fill: 'none',
        stroke: this.theme.elements.sequence.borderColor,
        'stroke-width': 0.5,
        'stroke-dasharray': '2,2',
        opacity: 0.3,
      });
    }
  }

  /**
     * Render optional element
     */
  private renderOptional(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;

    // Optional bracket shape
    builder.addElement('path', {
      d: `M 0,${bounds.height/4} L 0,0 L ${bounds.width},0 L ${bounds.width},${bounds.height/4}
                // eslint-disable-next-line max-len
                M 0,${bounds.height*3/4} L 0,${bounds.height} L ${bounds.width},${bounds.height} L ${bounds.width},${bounds.height*3/4}`,
      fill: 'none',
      stroke: this.theme.elements.optional.borderColor,
      'stroke-width': this.theme.elements.optional.borderWidth || 2,
    });

    // Optional indicator
    builder.addElement('text', {
      x: bounds.width / 2,
      y: bounds.height / 2,
      'text-anchor': 'middle',
      'dominant-baseline': 'central',
      fill: this.theme.elements.optional.textColor,
      'font-size': '10px',
    }, 'opt');
  }

  /**
     * Render repetition element
     */
  private renderRepetition(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;

    // Repetition loop shape
    builder.addElement('ellipse', {
      cx: bounds.width / 2,
      cy: bounds.height / 2,
      rx: bounds.width / 2,
      ry: bounds.height / 2,
      fill: this.theme.elements.repetition.backgroundColor,
      stroke: this.theme.elements.repetition.borderColor,
      'stroke-width': this.theme.elements.repetition.borderWidth || 1,
    });

    // Repetition indicator
    const isOneOrMore = element.metadata.confidence === 1;
    builder.addElement('text', {
      x: bounds.width / 2,
      y: bounds.height / 2,
      'text-anchor': 'middle',
      'dominant-baseline': 'central',
      fill: this.theme.elements.repetition.textColor,
      'font-size': '12px',
    }, isOneOrMore ? '+' : '*');
  }

  /**
     * Render group element
     */
  private renderGroup(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;

    // Group background
    builder.addElement('rect', {
      x: 0,
      y: 0,
      width: bounds.width,
      height: bounds.height,
      fill: this.theme.elements.group.backgroundColor,
      stroke: this.theme.elements.group.borderColor,
      'stroke-width': this.theme.elements.group.borderWidth || 1,
      rx: 5,
      ry: 5,
      opacity: 0.8,
    });
  }

  /**
     * Render start element
     */
  private renderStart(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;
    const radius = Math.min(bounds.width, bounds.height) / 2;

    builder.addElement('circle', {
      cx: bounds.width / 2,
      cy: bounds.height / 2,
      r: radius,
      fill: this.theme.elements.start.backgroundColor,
      stroke: this.theme.elements.start.borderColor,
      'stroke-width': this.theme.elements.start.borderWidth || 2,
    });

    builder.addElement('text', {
      x: bounds.width / 2,
      y: bounds.height / 2,
      'text-anchor': 'middle',
      'dominant-baseline': 'central',
      fill: this.theme.elements.start.textColor,
      'font-weight': 'bold',
    }, 'START');
  }

  /**
     * Render end element
     */
  private renderEnd(element: RailroadElement, builder: SVGBuilder): void {
    const bounds = element.bounds;
    const radius = Math.min(bounds.width, bounds.height) / 2;

    builder.addElement('circle', {
      cx: bounds.width / 2,
      cy: bounds.height / 2,
      r: radius,
      fill: this.theme.elements.end.backgroundColor,
      stroke: this.theme.elements.end.borderColor,
      'stroke-width': this.theme.elements.end.borderWidth || 2,
    });

    builder.addElement('circle', {
      cx: bounds.width / 2,
      cy: bounds.height / 2,
      r: radius - 4,
      fill: this.theme.elements.end.borderColor,
    });

    builder.addElement('text', {
      x: bounds.width / 2,
      y: bounds.height / 2,
      'text-anchor': 'middle',
      'dominant-baseline': 'central',
      fill: this.theme.elements.end.textColor,
      'font-weight': 'bold',
      'font-size': '10px',
    }, 'END');
  }

  /**
     * Render a connection
     */
  private renderConnection(connection: RailroadConnection, builder: SVGBuilder): void {
    const path = this.generateConnectionPath(connection);
    const style = connection.style;

    builder.addElement('path', {
      id: connection.id,
      d: path,
      fill: 'none',
      stroke: style.lineColor || this.theme.global.lineColor,
      'stroke-width': style.strokeWidth || 2,
      'stroke-dasharray': style.strokeDashArray || 'none',
      'marker-end': connection.type === 'normal' ? 'url(#arrowhead)' : 'none',
      class: `railroad-connection railroad-connection-${connection.type}`,
    });
  }

  /**
     * Generate SVG path for connection
     */
  private generateConnectionPath(connection: RailroadConnection): string {
    const points = connection.path;
    if (points.length < 2) {
      return '';
    }

    let path = `M ${points[0].x} ${points[0].y}`;

    for (let i = 1; i < points.length; i++) {
      const point = points[i];

      if (points.length === 2) {
        // Straight line
        path += ` L ${point.x} ${point.y}`;
      } else if (points.length === 4) {
        // Curved line with control points
        const cp1 = points[1];
        const cp2 = points[2];
        path += ` C ${cp1.x} ${cp1.y}, ${cp2.x} ${cp2.y}, ${point.x} ${point.y}`;
        break;
      } else {
        // Multi-segment line
        path += ` L ${point.x} ${point.y}`;
      }
    }

    return path;
  }

  /**
     * Get CSS classes for an element
     */
  private getElementClasses(element: RailroadElement): string[] {
    const classes = ['railroad-element', `railroad-${element.type}`];

    // Add metadata-based classes
    if (element.metadata.inherited) {
      classes.push('railroad-inherited');
    }
    if (element.metadata.overridden) {
      classes.push('railroad-overridden');
    }
    if (element.metadata.contextSensitive) {
      classes.push('railroad-context-sensitive');
    }
    if (element.metadata.formatSpecific) {
      classes.push(`railroad-${element.metadata.formatSpecific}`);
    }

    // Add state classes
    if (element.state.selected) {
      classes.push('selected');
    }
    if (element.state.highlighted) {
      classes.push('highlighted');
    }
    if (!element.state.visible) {
      classes.push('hidden');
    }

    return classes;
  }

  /**
     * Get inline style for an element
     */
  private getElementStyle(element: RailroadElement): string {
    const styles: string[] = [];
    const style = element.style;

    if (style.opacity !== undefined) {
      styles.push(`opacity: ${style.opacity}`);
    }
    if (style.transition) {
      styles.push(`transition: ${style.transition}`);
    }

    return styles.join('; ');
  }

  /**
     * Add interactivity to the SVG
     */
  private addInteractivity(builder: SVGBuilder, diagram: RailroadDiagram): void {
    const script = `
            // Railroad diagram interactivity
            document.addEventListener('DOMContentLoaded', function() {
                const elements = document.querySelectorAll('.railroad-element');
                
                elements.forEach(element => {
                    element.addEventListener('click', function(e) {
                        // Handle element click
                        const elementId = this.id;
    // eslint-disable-next-line no-console
                        console.log('Clicked element:', elementId);
                        
                        // Toggle selection
                        this.classList.toggle('selected');
                    });
                    
                    element.addEventListener('mouseenter', function(e) {
                        // Handle hover
                        this.classList.add('hover');
                    });
                    
                    element.addEventListener('mouseleave', function(e) {
                        // Handle hover end
                        this.classList.remove('hover');
                    });
                });
            });
        `;

    builder.addScript(script);
  }

  /**
     * Add animations to the SVG
     */
  private addAnimations(builder: SVGBuilder, diagram: RailroadDiagram): void {
    // Add CSS animations
    const animations = `
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
            
            @keyframes pulse {
                0%, 100% { opacity: 1; }
                50% { opacity: 0.7; }
            }
            
            .railroad-element {
                animation: fadeIn 0.5s ease-in-out;
            }
            
            .railroad-element.selected {
                animation: pulse 1s infinite;
            }
            
            .railroad-connection {
                stroke-dasharray: 1000;
                stroke-dashoffset: 1000;
                animation: drawLine 2s ease-in-out forwards;
            }
            
            @keyframes drawLine {
                to {
                    stroke-dashoffset: 0;
                }
            }
        `;

    builder.addStyle(animations);
  }

  /**
     * Add debug information to the SVG
     */
  private addDebugInfo(builder: SVGBuilder, diagram: RailroadDiagram): void {
    if (this.options.showBounds) {
      for (const element of diagram.elements) {
        builder.addElement('rect', {
          x: element.bounds.x,
          y: element.bounds.y,
          width: element.bounds.width,
          height: element.bounds.height,
          fill: 'none',
          stroke: 'red',
          'stroke-width': 1,
          'stroke-dasharray': '2,2',
          opacity: 0.5,
        });
      }
    }

    if (this.options.showConnectionPoints) {
      for (const element of diagram.elements) {
        // Entry point
        builder.addElement('circle', {
          cx: element.entryPoint.x,
          cy: element.entryPoint.y,
          r: 3,
          fill: 'green',
          opacity: 0.7,
        });

        // Exit point
        builder.addElement('circle', {
          cx: element.exitPoint.x,
          cy: element.exitPoint.y,
          r: 3,
          fill: 'blue',
          opacity: 0.7,
        });
      }
    }
  }

  /**
     * Add accessibility attributes
     */
  private addAccessibilityAttributes(element: RailroadElement, builder: SVGBuilder): void {
    const description = this.generateElementDescription(element);

    builder.addElement('title', {}, description);

    if (this.options.includeDescriptions) {
      builder.addElement('desc', {}, `${element.type} element: ${element.name || element.content || 'unnamed'}`);
    }
  }

  /**
     * Generate description for accessibility
     */
  private generateElementDescription(element: RailroadElement): string {
    let description = `${element.type}`;

    if (element.name) {
      description += ` named "${element.name}"`;
    }
    if (element.content) {
      description += ` containing "${element.content}"`;
    }

    if (element.metadata.inherited) {
      description += ' (inherited)';
    }
    if (element.metadata.overridden) {
      description += ' (overridden)';
    }
    if (element.metadata.contextSensitive) {
      description += ' (context-sensitive)';
    }

    return description;
  }

  /**
     * Update theme
     */
  public updateTheme(theme: RailroadTheme): void {
    this.theme = theme;
  }

  /**
     * Update options
     */
  public updateOptions(options: Partial<SVGRenderOptions>): void {
    this.options = { ...this.options, ...options };
  }
}

