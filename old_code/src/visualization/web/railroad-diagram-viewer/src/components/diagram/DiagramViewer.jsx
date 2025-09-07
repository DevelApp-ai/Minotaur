import { useState, useEffect, useRef, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ZoomIn,
  ZoomOut,
  RotateCcw,
  Maximize,
  Move,
  MousePointer,
  Hand,
  Grid,
  Layers,
  FileText,
  GitBranch,
  Target,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Card } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Separator } from '@/components/ui/separator.jsx';

const DiagramViewer = ({
  diagram,
  theme = 'default',
  viewMode = 'diagram',
  zoomLevel = 100,
  showGrid = false,
  isPlaying = false,
  selectedElement,
  onElementSelect,
}) => {
  const svgRef = useRef(null);
  const containerRef = useRef(null);
  const [isPanning, setIsPanning] = useState(false);
  const [panStart, setPanStart] = useState({ x: 0, y: 0 });
  const [panOffset, setPanOffset] = useState({ x: 0, y: 0 });
  const [hoveredElement, setHoveredElement] = useState(null);
  const [animationProgress, setAnimationProgress] = useState(0);

  // Animation effect for playing mode
  useEffect(() => {
    if (!isPlaying) {
      return;
    }

    const interval = setInterval(() => {
      setAnimationProgress(prev => (prev + 1) % 100);
    }, 50);

    return () => clearInterval(interval);
  }, [isPlaying]);

  // Handle mouse events for panning
  const handleMouseDown = useCallback((e) => {
    if (e.button === 1 || (e.button === 0 && e.ctrlKey)) { // Middle mouse or Ctrl+click
      setIsPanning(true);
      setPanStart({ x: e.clientX - panOffset.x, y: e.clientY - panOffset.y });
      e.preventDefault();
    }
  }, [panOffset]);

  const handleMouseMove = useCallback((e) => {
    if (isPanning) {
      setPanOffset({
        x: e.clientX - panStart.x,
        y: e.clientY - panStart.y,
      });
    }
  }, [isPanning, panStart]);

  const handleMouseUp = useCallback(() => {
    setIsPanning(false);
  }, []);

  // Add event listeners
  useEffect(() => {
    const container = containerRef.current;
    if (!container) {
      return;
    }

    container.addEventListener('mousemove', handleMouseMove);
    container.addEventListener('mouseup', handleMouseUp);
    container.addEventListener('mouseleave', handleMouseUp);

    return () => {
      container.removeEventListener('mousemove', handleMouseMove);
      container.removeEventListener('mouseup', handleMouseUp);
      container.removeEventListener('mouseleave', handleMouseUp);
    };
  }, [handleMouseMove, handleMouseUp]);

  // Handle element click
  const handleElementClick = (element) => {
    onElementSelect?.(element);
  };

  // Handle element hover
  const handleElementHover = (element) => {
    setHoveredElement(element);
  };

  // Get theme colors
  const getThemeColors = (themeName) => {
    const themes = {
      default: {
        background: '#ffffff',
        terminal: { fill: '#f0f8ff', stroke: '#4169e1', text: '#000080' },
        nonTerminal: { fill: '#f5f5f5', stroke: '#666666', text: '#333333' },
        connection: '#333333',
        grid: '#f0f0f0',
      },
      dark: {
        background: '#1e1e1e',
        terminal: { fill: '#2d3748', stroke: '#4299e1', text: '#90cdf4' },
        nonTerminal: { fill: '#2d3748', stroke: '#718096', text: '#e2e8f0' },
        connection: '#cccccc',
        grid: '#333333',
      },
      minimal: {
        background: '#ffffff',
        terminal: { fill: 'transparent', stroke: '#333333', text: '#333333' },
        nonTerminal: { fill: 'transparent', stroke: '#666666', text: '#666666' },
        connection: '#666666',
        grid: '#eeeeee',
      },
    };
    return themes[themeName] || themes.default;
  };

  const themeColors = getThemeColors(theme);

  // Render railroad element
  const renderElement = (element, index) => {
    const isSelected = selectedElement?.id === element.id;
    const isHovered = hoveredElement?.id === element.id;
    const animationDelay = index * 0.1;

    const elementProps = {
      key: element.id,
      className: `diagram-element ${element.type} ${isSelected ? 'selected' : ''} ${isHovered ? 'hovered' : ''}`,
      style: {
        cursor: 'pointer',
        transition: 'all 0.2s ease',
      },
      onClick: () => handleElementClick(element),
      onMouseEnter: () => handleElementHover(element),
      onMouseLeave: () => setHoveredElement(null),
    };

    const x = element.x || 0;
    const y = element.y || 0;
    const width = element.width || 120;
    const height = element.height || 40;

    switch (element.type) {
      case 'start':
        return (
          <motion.g
            {...elementProps}
            initial={{ opacity: 0, scale: 0 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: animationDelay }}
          >
            <circle
              cx={x + width/2}
              cy={y + height/2}
              r={height/2}
              fill={themeColors.terminal.fill}
              stroke={themeColors.terminal.stroke}
              strokeWidth={isSelected ? 3 : 2}
              opacity={isHovered ? 0.8 : 1}
            />
            <text
              x={x + width/2}
              y={y + height/2}
              textAnchor="middle"
              dominantBaseline="central"
              fill={themeColors.terminal.text}
              fontSize="12"
              fontWeight="bold"
            >
              START
            </text>
          </motion.g>
        );

      case 'end':
        return (
          <motion.g
            {...elementProps}
            initial={{ opacity: 0, scale: 0 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: animationDelay }}
          >
            <circle
              cx={x + width/2}
              cy={y + height/2}
              r={height/2}
              fill={themeColors.terminal.fill}
              stroke={themeColors.terminal.stroke}
              strokeWidth={isSelected ? 3 : 2}
              opacity={isHovered ? 0.8 : 1}
            />
            <circle
              cx={x + width/2}
              cy={y + height/2}
              r={height/2 - 4}
              fill={themeColors.terminal.stroke}
            />
            <text
              x={x + width/2}
              y={y + height/2}
              textAnchor="middle"
              dominantBaseline="central"
              fill={themeColors.background}
              fontSize="10"
              fontWeight="bold"
            >
              END
            </text>
          </motion.g>
        );

      case 'terminal':
        return (
          <motion.g
            {...elementProps}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: animationDelay }}
          >
            <rect
              x={x}
              y={y}
              width={width}
              height={height}
              rx={8}
              ry={8}
              fill={themeColors.terminal.fill}
              stroke={themeColors.terminal.stroke}
              strokeWidth={isSelected ? 3 : 2}
              opacity={isHovered ? 0.8 : 1}
            />
            <text
              x={x + width/2}
              y={y + height/2}
              textAnchor="middle"
              dominantBaseline="central"
              fill={themeColors.terminal.text}
              fontSize="12"
              fontWeight="bold"
            >
              {element.name || element.content}
            </text>
          </motion.g>
        );

      case 'non-terminal':
        return (
          <motion.g
            {...elementProps}
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: animationDelay }}
          >
            <rect
              x={x}
              y={y}
              width={width}
              height={height}
              fill={themeColors.nonTerminal.fill}
              stroke={themeColors.nonTerminal.stroke}
              strokeWidth={isSelected ? 3 : 1}
              opacity={isHovered ? 0.8 : 1}
            />
            <text
              x={x + width/2}
              y={y + height/2}
              textAnchor="middle"
              dominantBaseline="central"
              fill={themeColors.nonTerminal.text}
              fontSize="12"
              fontStyle="italic"
            >
              {element.name}
            </text>
          </motion.g>
        );

      default:
        return null;
    }
  };

  // Render connections
  const renderConnections = () => {
    if (!diagram?.connections) {
      return null;
    }

    return diagram.connections.map((connection, index) => {
      const fromElement = diagram.elements.find(el => el.id === connection.from);
      const toElement = diagram.elements.find(el => el.id === connection.to);

      if (!fromElement || !toElement) {
        return null;
      }

      const fromX = (fromElement.x || 0) + (fromElement.width || 120);
      const fromY = (fromElement.y || 0) + (fromElement.height || 40) / 2;
      const toX = toElement.x || 0;
      const toY = (toElement.y || 0) + (toElement.height || 40) / 2;

      const animationOffset = isPlaying ? (animationProgress * 2) % 100 : 0;

      return (
        <motion.g
          key={`connection-${index}`}
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ delay: index * 0.1, duration: 0.5 }}
        >
          <path
            d={`M ${fromX} ${fromY} L ${toX} ${toY}`}
            stroke={themeColors.connection}
            strokeWidth="2"
            fill="none"
            markerEnd="url(#arrowhead)"
            strokeDasharray={isPlaying ? '5,5' : 'none'}
            strokeDashoffset={isPlaying ? -animationOffset : 0}
            style={{ transition: 'stroke-dashoffset 0.1s linear' }}
          />
        </motion.g>
      );
    });
  };

  // Render grid
  const renderGrid = () => {
    if (!showGrid) {
      return null;
    }

    const gridSize = 20;
    const viewBox = diagram?.bounds || { width: 1000, height: 600 };
    const lines = [];

    // Vertical lines
    for (let x = 0; x <= viewBox.width; x += gridSize) {
      lines.push(
        <line
          key={`v-${x}`}
          x1={x}
          y1={0}
          x2={x}
          y2={viewBox.height}
          stroke={themeColors.grid}
          strokeWidth="1"
          opacity="0.5"
        />,
      );
    }

    // Horizontal lines
    for (let y = 0; y <= viewBox.height; y += gridSize) {
      lines.push(
        <line
          key={`h-${y}`}
          x1={0}
          y1={y}
          x2={viewBox.width}
          y2={y}
          stroke={themeColors.grid}
          strokeWidth="1"
          opacity="0.5"
        />,
      );
    }

    return <g className="grid">{lines}</g>;
  };

  if (!diagram) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground">
        <div className="text-center">
          <Layers className="h-16 w-16 mx-auto mb-4 opacity-50" />
          <p>No diagram to display</p>
        </div>
      </div>
    );
  }

  const viewBox = diagram.bounds || { width: 1000, height: 600 };
  const scale = zoomLevel / 100;

  return (
    <div
      ref={containerRef}
      className="relative h-full bg-background overflow-hidden"
      onMouseDown={handleMouseDown}
      style={{ cursor: isPanning ? 'grabbing' : 'grab' }}
    >
      {/* Toolbar */}
      <div className="absolute top-4 left-4 z-10">
        <Card className="p-2">
          <div className="flex items-center space-x-2">
            <Badge variant="secondary">
              {zoomLevel}%
            </Badge>
            <Separator orientation="vertical" className="h-6" />
            <div className="flex items-center space-x-1">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPanOffset({ x: 0, y: 0 })}
                title="Reset view"
              >
                <RotateCcw className="h-4 w-4" />
              </Button>
              {showGrid && (
                <Badge variant="outline" className="text-xs">
                  <Grid className="h-3 w-3 mr-1" />
                  Grid
                </Badge>
              )}
              {isPlaying && (
                <Badge variant="outline" className="text-xs animate-pulse">
                  <div className="h-2 w-2 bg-green-500 rounded-full mr-1" />
                  Playing
                </Badge>
              )}
            </div>
          </div>
        </Card>
      </div>

      {/* Element info tooltip */}
      <AnimatePresence>
        {hoveredElement && (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 10 }}
            className="absolute top-4 right-4 z-10"
          >
            <Card className="p-3 max-w-xs">
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Badge variant="secondary">{hoveredElement.type}</Badge>
                  <span className="font-medium">{hoveredElement.name}</span>
                </div>
                {hoveredElement.content && (
                  <p className="text-sm text-muted-foreground">
                    {hoveredElement.content}
                  </p>
                )}
              </div>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* SVG Diagram */}
      <div className="h-full flex items-center justify-center">
        <motion.svg
          ref={svgRef}
          width={viewBox.width * scale}
          height={viewBox.height * scale}
          viewBox={`0 0 ${viewBox.width} ${viewBox.height}`}
          style={{
            transform: `translate(${panOffset.x}px, ${panOffset.y}px)`,
            transition: isPanning ? 'none' : 'transform 0.2s ease',
          }}
          className="border border-border rounded-lg shadow-sm"
        >
          {/* Definitions */}
          <defs>
            <marker
              id="arrowhead"
              markerWidth="10"
              markerHeight="7"
              refX="9"
              refY="3.5"
              orient="auto"
            >
              <polygon
                points="0 0, 10 3.5, 0 7"
                fill={themeColors.connection}
              />
            </marker>
          </defs>

          {/* Background */}
          <rect
            width="100%"
            height="100%"
            fill={themeColors.background}
          />

          {/* Grid */}
          {renderGrid()}

          {/* Connections */}
          {renderConnections()}

          {/* Elements */}
          {diagram.elements?.map((element, index) => renderElement(element, index))}
        </motion.svg>
      </div>

      {/* View mode indicator */}
      <div className="absolute bottom-4 left-4 z-10">
        <Card className="p-2">
          <div className="flex items-center space-x-2 text-sm">
            <Badge variant="outline">
              {viewMode === 'diagram' && <FileText className="h-3 w-3 mr-1" />}
              {viewMode === 'inheritance' && <GitBranch className="h-3 w-3 mr-1" />}
              {viewMode === 'context' && <Target className="h-3 w-3 mr-1" />}
              {viewMode.charAt(0).toUpperCase() + viewMode.slice(1)} View
            </Badge>
            <span className="text-muted-foreground">
              {diagram.elements?.length || 0} elements
            </span>
          </div>
        </Card>
      </div>
    </div>
  );
};

export default DiagramViewer;

