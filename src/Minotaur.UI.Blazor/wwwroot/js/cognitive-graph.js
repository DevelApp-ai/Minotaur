// Cognitive Graph Visualization JavaScript Module
window.cognitiveGraphModule = {
    graphs: new Map(),
    
    // Render a cognitive graph in the specified container
    renderCognitiveGraph: function(container, graphData, isEditMode) {
        if (!container || !graphData) return;
        
        const containerId = this.getContainerId(container);
        
        // Clear existing content
        container.innerHTML = '';
        
        // Create SVG container
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('width', '100%');
        svg.setAttribute('height', '100%');
        svg.setAttribute('viewBox', '-500 -300 1000 600');
        svg.style.cursor = isEditMode ? 'crosshair' : 'default';
        
        // Add definitions for markers (arrows)
        const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
        const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
        marker.setAttribute('id', 'arrowhead');
        marker.setAttribute('markerWidth', '10');
        marker.setAttribute('markerHeight', '7');
        marker.setAttribute('refX', '9');
        marker.setAttribute('refY', '3.5');
        marker.setAttribute('orient', 'auto');
        
        const polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        polygon.setAttribute('points', '0 0, 10 3.5, 0 7');
        polygon.setAttribute('fill', '#666');
        
        marker.appendChild(polygon);
        defs.appendChild(marker);
        svg.appendChild(defs);
        
        // Create groups for edges and nodes
        const edgeGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        edgeGroup.setAttribute('class', 'edges');
        svg.appendChild(edgeGroup);
        
        const nodeGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        nodeGroup.setAttribute('class', 'nodes');
        svg.appendChild(nodeGroup);
        
        // Render edges first (so they appear behind nodes)
        graphData.edges.forEach(edge => {
            const sourceNode = graphData.nodes.find(n => n.id === edge.source);
            const targetNode = graphData.nodes.find(n => n.id === edge.target);
            
            if (sourceNode && targetNode) {
                const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
                line.setAttribute('x1', sourceNode.x);
                line.setAttribute('y1', sourceNode.y);
                line.setAttribute('x2', targetNode.x);
                line.setAttribute('y2', targetNode.y);
                line.setAttribute('stroke', '#666');
                line.setAttribute('stroke-width', '2');
                line.setAttribute('marker-end', 'url(#arrowhead)');
                
                edgeGroup.appendChild(line);
            }
        });
        
        // Render nodes
        graphData.nodes.forEach(node => {
            const nodeElement = this.createNodeElement(node, isEditMode);
            nodeGroup.appendChild(nodeElement);
        });
        
        container.appendChild(svg);
        
        // Store graph reference
        this.graphs.set(containerId, {
            svg: svg,
            data: graphData,
            scale: 1,
            panX: 0,
            panY: 0
        });
        
        // Add pan and zoom functionality
        this.addPanZoomBehavior(container, svg);
    },
    
    // Create a node element
    createNodeElement: function(node, isEditMode) {
        const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        group.setAttribute('class', 'node');
        group.setAttribute('data-node-id', node.id);
        group.setAttribute('transform', `translate(${node.x}, ${node.y})`);
        
        // Node background
        const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        const textLength = node.label.length * 8; // Approximate text width
        const width = Math.max(80, textLength + 20);
        const height = 40;
        
        rect.setAttribute('x', -width/2);
        rect.setAttribute('y', -height/2);
        rect.setAttribute('width', width);
        rect.setAttribute('height', height);
        rect.setAttribute('rx', '8');
        
        // Style based on node type
        const colors = this.getNodeColors(node.type);
        rect.setAttribute('fill', node.isSelected ? colors.selectedFill : colors.fill);
        rect.setAttribute('stroke', node.isSelected ? colors.selectedStroke : colors.stroke);
        rect.setAttribute('stroke-width', node.isSelected ? '3' : '2');
        
        group.appendChild(rect);
        
        // Node label
        const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        text.setAttribute('x', '0');
        text.setAttribute('y', '5');
        text.setAttribute('text-anchor', 'middle');
        text.setAttribute('font-family', 'monospace');
        text.setAttribute('font-size', '12');
        text.setAttribute('fill', '#333');
        text.textContent = node.label;
        
        group.appendChild(text);
        
        // Add node type indicator
        const typeIndicator = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        typeIndicator.setAttribute('cx', width/2 - 8);
        typeIndicator.setAttribute('cy', -height/2 + 8);
        typeIndicator.setAttribute('r', '4');
        typeIndicator.setAttribute('fill', colors.indicator);
        
        group.appendChild(typeIndicator);
        
        // Add click behavior for edit mode
        if (isEditMode) {
            group.style.cursor = 'pointer';
            group.addEventListener('click', (e) => {
                e.stopPropagation();
                this.selectNode(node.id);
            });
            
            // Add hover effects
            group.addEventListener('mouseenter', () => {
                rect.setAttribute('stroke-width', '3');
                rect.setAttribute('filter', 'drop-shadow(2px 2px 4px rgba(0,0,0,0.3))');
            });
            
            group.addEventListener('mouseleave', () => {
                if (!node.isSelected) {
                    rect.setAttribute('stroke-width', '2');
                    rect.removeAttribute('filter');
                }
            });
        }
        
        return group;
    },
    
    // Get colors for different node types
    getNodeColors: function(nodeType) {
        const colorSchemes = {
            'TerminalNode': {
                fill: '#e3f2fd',
                stroke: '#1976d2',
                selectedFill: '#bbdefb',
                selectedStroke: '#0d47a1',
                indicator: '#2196f3'
            },
            'NonTerminalNode': {
                fill: '#f3e5f5',
                stroke: '#7b1fa2',
                selectedFill: '#ce93d8',
                selectedStroke: '#4a148c',
                indicator: '#9c27b0'
            },
            'IdentifierNode': {
                fill: '#e8f5e8',
                stroke: '#388e3c',
                selectedFill: '#c8e6c9',
                selectedStroke: '#1b5e20',
                indicator: '#4caf50'
            },
            'LiteralNode': {
                fill: '#fff3e0',
                stroke: '#f57c00',
                selectedFill: '#ffcc02',
                selectedStroke: '#e65100',
                indicator: '#ff9800'
            }
        };
        
        return colorSchemes[nodeType] || colorSchemes['NonTerminalNode'];
    },
    
    // Select a node
    selectNode: function(nodeId) {
        // Call back to Blazor component
        if (window.blazorComponent) {
            window.blazorComponent.invokeMethodAsync('OnNodeSelected', nodeId);
        }
    },
    
    // Add pan and zoom behavior
    addPanZoomBehavior: function(container, svg) {
        let isPanning = false;
        let startX, startY, startViewBoxX, startViewBoxY;
        let currentScale = 1;
        
        svg.addEventListener('mousedown', (e) => {
            if (e.button === 0) { // Left mouse button
                isPanning = true;
                startX = e.clientX;
                startY = e.clientY;
                
                const viewBox = svg.viewBox.baseVal;
                startViewBoxX = viewBox.x;
                startViewBoxY = viewBox.y;
                
                svg.style.cursor = 'grabbing';
            }
        });
        
        svg.addEventListener('mousemove', (e) => {
            if (isPanning) {
                const dx = (startX - e.clientX) / currentScale;
                const dy = (startY - e.clientY) / currentScale;
                
                svg.setAttribute('viewBox', 
                    `${startViewBoxX + dx} ${startViewBoxY + dy} ${1000/currentScale} ${600/currentScale}`);
            }
        });
        
        svg.addEventListener('mouseup', () => {
            isPanning = false;
            svg.style.cursor = 'default';
        });
        
        svg.addEventListener('mouseleave', () => {
            isPanning = false;
            svg.style.cursor = 'default';
        });
        
        // Zoom with mouse wheel
        svg.addEventListener('wheel', (e) => {
            e.preventDefault();
            
            const rect = svg.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;
            
            const viewBox = svg.viewBox.baseVal;
            const svgX = viewBox.x + (mouseX / rect.width) * viewBox.width;
            const svgY = viewBox.y + (mouseY / rect.height) * viewBox.height;
            
            const zoomFactor = e.deltaY > 0 ? 1.1 : 0.9;
            currentScale *= (1 / zoomFactor);
            
            const newWidth = 1000 / currentScale;
            const newHeight = 600 / currentScale;
            
            const newX = svgX - (mouseX / rect.width) * newWidth;
            const newY = svgY - (mouseY / rect.height) * newHeight;
            
            svg.setAttribute('viewBox', `${newX} ${newY} ${newWidth} ${newHeight}`);
        });
    },
    
    // Zoom functions
    zoomCognitiveGraph: function(container, factor) {
        const containerId = this.getContainerId(container);
        const graph = this.graphs.get(containerId);
        
        if (graph) {
            const svg = graph.svg;
            const viewBox = svg.viewBox.baseVal;
            
            const newWidth = viewBox.width / factor;
            const newHeight = viewBox.height / factor;
            const newX = viewBox.x + (viewBox.width - newWidth) / 2;
            const newY = viewBox.y + (viewBox.height - newHeight) / 2;
            
            svg.setAttribute('viewBox', `${newX} ${newY} ${newWidth} ${newHeight}`);
        }
    },
    
    // Reset view
    resetCognitiveGraphView: function(container) {
        const containerId = this.getContainerId(container);
        const graph = this.graphs.get(containerId);
        
        if (graph) {
            graph.svg.setAttribute('viewBox', '-500 -300 1000 600');
        }
    },
    
    // Helper to get container ID
    getContainerId: function(container) {
        if (!container.id) {
            container.id = 'cognitive-graph-' + Math.random().toString(36).substr(2, 9);
        }
        return container.id;
    }
};

// Global functions for Blazor interop
window.renderCognitiveGraph = function(container, graphData, isEditMode) {
    window.cognitiveGraphModule.renderCognitiveGraph(container, graphData, isEditMode);
};

window.zoomCognitiveGraph = function(container, factor) {
    window.cognitiveGraphModule.zoomCognitiveGraph(container, factor);
};

window.resetCognitiveGraphView = function(container) {
    window.cognitiveGraphModule.resetCognitiveGraphView(container);
};

// Set up Blazor component reference
window.setCognitiveGraphComponent = function(componentRef) {
    window.blazorComponent = componentRef;
};