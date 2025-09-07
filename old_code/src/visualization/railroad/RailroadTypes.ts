/**
 * Type Definitions and Interfaces for Railroad Diagram System
 *
 * This module provides comprehensive type definitions for the railroad diagram
 * visualization system, including layout, styling, and interaction types.
 */

/**
 * Basic geometric types
 */
export interface Point {
    x: number;
    y: number;
}

export interface Size {
    width: number;
    height: number;
}

export interface Rectangle extends Point, Size {}

export interface Margin {
    top: number;
    right: number;
    bottom: number;
    left: number;
}

/**
 * Railroad diagram element types
 */
export type RailroadElementType =
    | 'terminal'
    | 'non-terminal'
    | 'choice'
    | 'sequence'
    | 'optional'
    | 'repetition'
    | 'group'
    | 'start'
    | 'end'
    | 'skip'
    | 'comment';

/**
 * Visual styling for railroad elements
 */
export interface RailroadStyle {
    // Colors
    backgroundColor?: string;
    borderColor?: string;
    textColor?: string;
    lineColor?: string;

    // Typography
    fontFamily?: string;
    fontSize?: number;
    fontWeight?: 'normal' | 'bold' | 'lighter' | 'bolder';
    fontStyle?: 'normal' | 'italic' | 'oblique';

    // Dimensions
    padding?: Margin;
    margin?: Margin;
    borderWidth?: number;
    borderRadius?: number;

    // Line styling
    strokeWidth?: number;
    strokeDashArray?: string;

    // Effects
    opacity?: number;
    shadow?: boolean;

    // Animation
    transition?: string;

    // Custom properties
    [key: string]: any;
}

/**
 * Theme definitions for railroad diagrams
 */
export interface RailroadTheme {
    name: string;
    description?: string;

    // Global styles
    global: {
        backgroundColor: string;
        fontFamily: string;
        fontSize: number;
        lineColor: string;
        textColor: string;
    };

    // Element-specific styles
    elements: {
        terminal: RailroadStyle;
        nonTerminal: RailroadStyle;
        choice: RailroadStyle;
        sequence: RailroadStyle;
        optional: RailroadStyle;
        repetition: RailroadStyle;
        group: RailroadStyle;
        start: RailroadStyle;
        end: RailroadStyle;
        skip: RailroadStyle;
        comment: RailroadStyle;
    };

    // State-specific styles
    states: {
        hover: Partial<RailroadStyle>;
        selected: Partial<RailroadStyle>;
        highlighted: Partial<RailroadStyle>;
        disabled: Partial<RailroadStyle>;
    };

    // Minotaur-specific styles
    grammarForge: {
        inherited: Partial<RailroadStyle>;
        overridden: Partial<RailroadStyle>;
        contextSensitive: Partial<RailroadStyle>;
        newRule: Partial<RailroadStyle>;
    };
}

/**
 * Layout configuration for railroad diagrams
 */
export interface RailroadLayout {
    // Spacing
    horizontalSpacing: number;
    verticalSpacing: number;
    elementSpacing: number;

    // Dimensions
    minElementWidth: number;
    minElementHeight: number;
    maxElementWidth: number;

    // Alignment
    textAlignment: 'left' | 'center' | 'right';
    verticalAlignment: 'top' | 'middle' | 'bottom';

    // Flow direction
    direction: 'horizontal' | 'vertical';

    // Choice layout
    choiceLayout: 'stacked' | 'branched' | 'compact';

    // Repetition layout
    repetitionStyle: 'loop' | 'arrow' | 'compact';

    // Line routing
    lineRouting: 'straight' | 'curved' | 'orthogonal';

    // Optimization
    optimizeLayout: boolean;
    compactMode: boolean;
}

/**
 * Railroad diagram element with layout information
 */
export interface RailroadElement {
    // Basic properties
    id: string;
    type: RailroadElementType;
    name?: string;
    content?: string;

    // Hierarchy
    parent?: RailroadElement;
    children: RailroadElement[];

    // Layout
    bounds: Rectangle;
    style: RailroadStyle;

    // Connections
    entryPoint: Point;
    exitPoint: Point;
    connectionPoints: Point[];

    // Metadata
    metadata: {
        grammarRule?: string;
        inherited?: boolean;
        overridden?: boolean;
        contextSensitive?: boolean;
        formatSpecific?: string;
        sourceGrammar?: string;
        confidence?: number;
        complexity?: number;
        [key: string]: any;
    };

    // Interaction
    interactive: boolean;
    selectable: boolean;
    hoverable: boolean;

    // State
    state: {
        visible: boolean;
        selected: boolean;
        highlighted: boolean;
        collapsed: boolean;
        [key: string]: any;
    };
}

/**
 * Connection between railroad elements
 */
export interface RailroadConnection {
    id: string;
    from: {
        element: RailroadElement;
        point: Point;
    };
    to: {
        element: RailroadElement;
        point: Point;
    };
    path: Point[];
    style: RailroadStyle;
    type: 'normal' | 'skip' | 'loop' | 'branch';
}

/**
 * Complete railroad diagram
 */
export interface RailroadDiagram {
    // Basic properties
    id: string;
    name: string;
    description?: string;

    // Content
    elements: RailroadElement[];
    connections: RailroadConnection[];

    // Layout
    bounds: Rectangle;
    layout: RailroadLayout;
    theme: RailroadTheme;

    // Metadata
    metadata: {
        grammarName: string;
        grammarFormat: string;
        generatedAt: Date;
        version: string;
        [key: string]: any;
    };

    // Configuration
    config: {
        interactive: boolean;
        zoomable: boolean;
        pannable: boolean;
        exportable: boolean;
        [key: string]: any;
    };
}

/**
 * Railroad diagram generation options
 */
export interface RailroadGenerationOptions {
    // Layout options
    layout?: Partial<RailroadLayout>;
    theme?: string | RailroadTheme;

    // Content options
    includeInheritance?: boolean;
    includeContextSensitive?: boolean;
    includeMetadata?: boolean;

    // Optimization options
    optimizeForSize?: boolean;
    optimizeForReadability?: boolean;
    simplifyComplexRules?: boolean;
    maxDepth?: number;

    // Export options
    format?: 'svg' | 'png' | 'pdf' | 'html';
    embedStyles?: boolean;
    includeInteractivity?: boolean;

    // Debug options
    showDebugInfo?: boolean;
    includeMetrics?: boolean;
}

/**
 * Railroad diagram export result
 */
export interface RailroadExportResult {
    content: string | Buffer;
    format: string;
    metadata: {
        size: Size;
        elementCount: number;
        connectionCount: number;
        generationTime: number;
        [key: string]: any;
    };
}

/**
 * Interactive event types
 */
export type RailroadEventType =
    | 'click'
    | 'doubleclick'
    | 'hover'
    | 'select'
    | 'deselect'
    | 'expand'
    | 'collapse'
    | 'zoom'
    | 'pan';

/**
 * Interactive event data
 */
export interface RailroadEvent {
    type: RailroadEventType;
    target: RailroadElement | RailroadConnection | null;
    position: Point;
    timestamp: number;
    data?: any;
}

/**
 * Event handler function type
 */
export type RailroadEventHandler = (event: RailroadEvent) => void;

/**
 * Animation configuration
 */
export interface RailroadAnimation {
    duration: number;
    easing: 'linear' | 'ease' | 'ease-in' | 'ease-out' | 'ease-in-out';
    delay?: number;
    repeat?: number | 'infinite';
    direction?: 'normal' | 'reverse' | 'alternate';
}

/**
 * Parsing path for debugging
 */
export interface ParsingPath {
    steps: {
        element: RailroadElement;
        input: string;
        position: number;
        success: boolean;
        timestamp: number;
    }[];
    totalTime: number;
    success: boolean;
    errorMessage?: string;
}

/**
 * Grammar comparison result
 */
export interface GrammarComparison {
    added: RailroadElement[];
    removed: RailroadElement[];
    modified: {
        element: RailroadElement;
        changes: string[];
    }[];
    unchanged: RailroadElement[];
}

/**
 * Performance metrics
 */
export interface RailroadMetrics {
    generation: {
        analysisTime: number;
        layoutTime: number;
        renderTime: number;
        totalTime: number;
    };

    diagram: {
        elementCount: number;
        connectionCount: number;
        maxDepth: number;
        complexity: number;
    };

    memory: {
        peakUsage: number;
        currentUsage: number;
    };

    performance: {
        fps?: number;
        renderTime?: number;
        interactionLatency?: number;
    };
}

/**
 * Validation result
 */
export interface ValidationResult {
    valid: boolean;
    errors: {
        type: 'error' | 'warning' | 'info';
        message: string;
        element?: RailroadElement;
        location?: Point;
    }[];
    metrics: RailroadMetrics;
}

/**
 * Plugin interface for extending railroad diagram functionality
 */
export interface RailroadPlugin {
    name: string;
    version: string;
    description?: string;

    // Lifecycle hooks
    onInit?(diagram: RailroadDiagram): void;
    onElementCreate?(element: RailroadElement): void;
    onElementUpdate?(element: RailroadElement): void;
    onElementDelete?(element: RailroadElement): void;
    onRender?(diagram: RailroadDiagram): void;
    onExport?(result: RailroadExportResult): void;

    // Custom functionality
    customElements?: {
        [type: string]: {
            render: (element: RailroadElement) => string;
            layout: (element: RailroadElement) => Rectangle;
        };
    };

    customThemes?: {
        [name: string]: RailroadTheme;
    };

    customExporters?: {
        [format: string]: (diagram: RailroadDiagram) => RailroadExportResult;
    };
}

/**
 * Configuration for the railroad diagram system
 */
export interface RailroadConfig {
    // Default settings
    defaultTheme: string;
    defaultLayout: RailroadLayout;

    // Feature flags
    features: {
        inheritance: boolean;
        contextSensitive: boolean;
        interactivity: boolean;
        animation: boolean;
        export: boolean;
        debugging: boolean;
    };

    // Performance settings
    performance: {
        maxElements: number;
        maxDepth: number;
        enableCaching: boolean;
        lazyLoading: boolean;
    };

    // Plugin settings
    plugins: {
        enabled: string[];
        config: { [pluginName: string]: any };
    };

    // Debug settings
    debug: {
        enabled: boolean;
        showMetrics: boolean;
        showBounds: boolean;
        logEvents: boolean;
    };
}

/**
 * Utility type for partial deep updates
 */
export type DeepPartial<T> = {
    [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

/**
 * Utility type for required fields
 */
export type RequiredFields<T, K extends keyof T> = T & Required<Pick<T, K>>;

/**
 * Utility type for optional fields
 */
export type OptionalFields<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

