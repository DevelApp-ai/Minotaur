/*
 * CognitiveGraph Visualization with PackedNode Ambiguity Support
 *
 * This visualization directly renders CognitiveGraph's SymbolNode and PackedNode
 * structure, preserving all ambiguity. Each SymbolNode can have multiple PackedNodes,
 * each representing a different interpretation.
 */

// Global namespace
window.CognitiveGraphVisualizer = window.CognitiveGraphVisualizer || {};

// Visualizer class
class CognitiveGraphVisualizer {
    constructor(svgElement, options = {}) {
        this.svg = d3.select(svgElement);
        this.options = {
            width: 800,
            height: 600,
            nodeRadius: 12,
            linkDistance: 60,
            charge: -150,
            duration: 750,
            showAllAlternatives: true,
            highlightAmbiguities: true,
            ...options
        };

        this.zoom = d3.zoom()
            .scaleExtent([0.1, 4])
            .on('zoom', (event) => this.zoomed(event));

        this.svg.call(this.zoom);
        this.g = this.svg.append('g');
        
        // State
        this.graphData = null;
        this.ambiguities = {};
        this.activeInterpretation = null;
        this.showAllAlternatives = this.options.showAllAlternatives;
        this.simulation = null;
        this.nodeElements = null;
        this.linkElements = null;
        this.dotNetReference = null;
        
        this.addZoomControls();
        this.addLegend();
    }

    // ==================== UI Controls ====================

    addZoomControls() {
        const controls = this.svg.append('g')
            .attr('class', 'zoom-controls')
            .attr('transform', 'translate(10, 10)');

        controls.append('rect')
            .attr('x', 0).attr('y', 0).attr('width', 30).attr('height', 20)
            .attr('fill', '#f0f0f0').attr('stroke', '#ccc').attr('cursor', 'pointer')
            .on('click', () => this.zoomIn());
        controls.append('text').attr('x', 15).attr('y', 15)
            .attr('text-anchor', 'middle').attr('font-size', 10).text('+');

        controls.append('rect')
            .attr('x', 35).attr('y', 0).attr('width', 30).attr('height', 20)
            .attr('fill', '#f0f0f0').attr('stroke', '#ccc').attr('cursor', 'pointer')
            .on('click', () => this.zoomOut());
        controls.append('text').attr('x', 50).attr('y', 15)
            .attr('text-anchor', 'middle').attr('font-size', 10).text('-');

        controls.append('rect')
            .attr('x', 70).attr('y', 0).attr('width', 40).attr('height', 20)
            .attr('fill', '#f0f0f0').attr('stroke', '#ccc').attr('cursor', 'pointer')
            .on('click', () => this.resetZoom());
        controls.append('text').attr('x', 90).attr('y', 15)
            .attr('text-anchor', 'middle').attr('font-size', 10).text('Reset');

        controls.append('rect')
            .attr('x', 115).attr('y', 0).attr('width', 80).attr('height', 20)
            .attr('fill', this.showAllAlternatives ? '#e3f2fd' : '#f0f0f0')
            .attr('stroke', '#ccc').attr('cursor', 'pointer')
            .attr('class', 'toggle-alternatives')
            .on('click', () => this.toggleAlternatives());
        controls.append('text').attr('x', 155).attr('y', 15)
            .attr('text-anchor', 'middle').attr('font-size', 10).text('All Paths');
    }

    addLegend() {
        const legend = this.svg.append('g')
            .attr('class', 'legend')
            .attr('transform', `translate(${this.options.width - 220}, 20)`);

        legend.append('circle').attr('cx', 0).attr('cy', 0).attr('r', 8)
            .attr('fill', '#4CAF50').attr('stroke', '#333');
        legend.append('text').attr('x', 15).attr('y', 5)
            .attr('font-size', 10).text('Normal Node');

        legend.append('circle').attr('cx', 0).attr('cy', 20).attr('r', 8)
            .attr('fill', '#FF9800').attr('stroke', '#333').attr('stroke-width', 2);
        legend.append('text').attr('x', 15).attr('y', 25)
            .attr('font-size', 10).text('Ambiguous Node');

        legend.append('line').attr('x1', 0).attr('y1', 40).attr('x2', 15).attr('y2', 40)
            .attr('stroke', '#999').attr('stroke-width', 1.5);
        legend.append('text').attr('x', 20).attr('y', 45)
            .attr('font-size', 10).text('Normal Edge');

        legend.append('line').attr('x1', 0).attr('y1', 55).attr('x2', 15).attr('y2', 55)
            .attr('stroke', '#2196F3').attr('stroke-width', 1.5).attr('stroke-dasharray', '3,3');
        legend.append('text').attr('x', 20).attr('y', 60)
            .attr('font-size', 10).text('PackedNode Edge');
    }

    // ==================== Zoom ====================

    zoomIn() {
        this.svg.transition().duration(this.options.duration)
            .call(this.zoom.scaleBy, 1.2);
    }

    zoomOut() {
        this.svg.transition().duration(this.options.duration)
            .call(this.zoom.scaleBy, 0.8);
    }

    resetZoom() {
        this.svg.transition().duration(this.options.duration)
            .call(this.zoom.transform, d3.zoomIdentity);
    }

    zoomed(event) {
        this.g.attr('transform', event.transform);
    }

    toggleAlternatives() {
        this.showAllAlternatives = !this.showAllAlternatives;
        this.svg.selectAll('.toggle-alternatives rect')
            .attr('fill', this.showAllAlternatives ? '#e3f2fd' : '#f0f0f0');
        
        if (this.graphData) {
            this.render(this.graphData);
        }
    }

    // ==================== Rendering ====================

    render(graphData) {
        this.graphData = graphData;
        this.clear();

        if (!graphData || !graphData.nodes || graphData.nodes.length === 0) {
            return;
        }

        // Store ambiguities
        this.ambiguities = graphData.ambiguities || {};

        // Create force simulation
        this.simulation = d3.forceSimulation(graphData.nodes)
            .force('link', d3.forceLink(this.getVisibleEdges()).id(d => d.id)
                .distance(this.options.linkDistance))
            .force('charge', d3.forceManyBody().strength(this.options.charge))
            .force('center', d3.forceCenter(
                (this.svg.node().clientWidth || this.options.width) / 2,
                (this.svg.node().clientHeight || this.options.height) / 2
            ));

        // Create links
        this.linkElements = this.g.append('g')
            .selectAll('line')
            .data(this.getVisibleEdges())
            .enter().append('line')
            .attr('stroke', d => this.getEdgeColor(d))
            .attr('stroke-width', d => d.isAlternative ? 1.5 : 2)
            .attr('stroke-dasharray', d => d.isAlternative ? '3,3' : 'none')
            .attr('class', d => d.isAlternative ? 'alternative-edge' : 'normal-edge');

        // Create node groups
        this.nodeElements = this.g.append('g')
            .selectAll('g')
            .data(graphData.nodes)
            .enter().append('g')
            .call(d3.drag()
                .on('start', this.dragstarted.bind(this))
                .on('drag', this.dragged.bind(this))
                .on('end', this.dragended.bind(this)));

        // Add circles
        this.nodeElements.append('circle')
            .attr('r', d => this.getNodeRadius(d))
            .attr('fill', d => this.getNodeColor(d))
            .attr('stroke', '#333')
            .attr('stroke-width', d => d.isAmbiguous ? 3 : 1)
            .attr('class', d => d.isAmbiguous ? 'ambiguous-node' : 'normal-node')
            .attr('cursor', 'pointer')
            .on('click', (event, d) => this.handleNodeClick(event, d))
            .on('mouseover', (event, d) => this.handleNodeMouseOver(event, d))
            .on('mouseout', (event, d) => this.handleNodeMouseOut(event, d));

        // Add labels
        this.nodeElements.append('text')
            .text(d => this.getNodeLabel(d))
            .attr('y', d => this.getNodeRadius(d) + 15)
            .attr('text-anchor', 'middle')
            .attr('font-size', 11);

        // Add ambiguity indicators
        this.nodeElements.filter(d => d.isAmbiguous)
            .append('text')
            .text(d => `x${d.alternativeCount}`)
            .attr('y', d => this.getNodeRadius(d) + 30)
            .attr('text-anchor', 'middle')
            .attr('font-size', 10)
            .attr('fill', '#fff')
            .attr('font-weight', 'bold');

        // Update positions
        this.simulation.on('tick', () => {
            this.linkElements
                .attr('x1', d => d.source.x)
                .attr('y1', d => d.source.y)
                .attr('x2', d => d.target.x)
                .attr('y2', d => d.target.y);

            this.nodeElements
                .attr('transform', d => `translate(${d.x},${d.y})`);
        });
    }

    getVisibleEdges() {
        if (!this.graphData) return [];
        
        if (this.showAllAlternatives) {
            return this.graphData.edges;
        }
        
        return this.graphData.edges.filter(e => !e.isAlternative);
    }

    getNodeRadius(node) {
        const baseRadius = this.options.nodeRadius;
        if (node.isAmbiguous) {
            return baseRadius + 4;
        }
        return baseRadius;
    }

    getNodeColor(node) {
        if (node.isAmbiguous) {
            return '#FF9800';
        }
        
        const colors = {
            'compilation_unit': '#4CAF50',
            'class_declaration': '#2196F3',
            'method_declaration': '#FFC107',
            'function_expression': '#9C27B0',
            'expression': '#00BCD4',
            'statement': '#795548',
            'declaration': '#4CAF50',
            'identifier': '#E91E63',
            'block': '#607D8B'
        };
        
        return colors[node.type] || colors[node.group] || '#9E9E9E';
    }

    getEdgeColor(edge) {
        if (edge.isAlternative) {
            return '#2196F3';
        }
        return '#999';
    }

    getNodeLabel(node) {
        if (node.name && node.name.length > 0) {
            return node.name.length > 20 ? node.name.substring(0, 20) + '...' : node.name;
        }
        return node.type.length > 20 ? node.type.substring(0, 20) + '...' : node.type;
    }

    // ==================== Events ====================

    handleNodeClick(event, d) {
        event.stopPropagation();
        
        if (d.isAmbiguous && this.ambiguities[d.id]) {
            this.showAmbiguityMenu(d, this.ambiguities[d.id]);
        }
    }

    handleNodeMouseOver(event, d) {
        this.g.selectAll('line')
            .filter(l => l.source.id === d.id || l.target.id === d.id)
            .attr('stroke-width', 3)
            .attr('stroke', '#FF5722');
    }

    handleNodeMouseOut(event, d) {
        this.g.selectAll('line')
            .attr('stroke-width', l => l.isAlternative ? 1.5 : 2)
            .attr('stroke', l => this.getEdgeColor(l));
    }

    showAmbiguityMenu(node, ambiguity) {
        this.hideAmbiguityMenu();
        
        const menu = this.svg.append('g')
            .attr('class', 'ambiguity-menu')
            .attr('transform', `translate(${node.x},${node.y - 60})`);

        menu.append('rect')
            .attr('x', -100).attr('y', -10)
            .attr('width', 200)
            .attr('height', 20 + (ambiguity.packedNodes.length * 25))
            .attr('fill', 'white').attr('stroke', '#ccc').attr('rx', 5);

        menu.append('text')
            .text(`Ambiguity: ${ambiguity.alternativeCount} PackedNodes`)
            .attr('x', 0).attr('y', 10)
            .attr('text-anchor', 'middle').attr('font-size', 12).attr('font-weight', 'bold');

        ambiguity.packedNodes.forEach((packedNode, index) => {
            const y = 30 + (index * 25);
            
            menu.append('rect')
                .attr('x', -90).attr('y', y - 10).attr('width', 180).attr('height', 20)
                .attr('fill', '#f5f5f5').attr('stroke', '#ddd').attr('cursor', 'pointer')
                .on('click', () => this.selectPackedNode(node.id, index))
                .on('mouseover', function() { d3.select(this).attr('fill', '#e3f2fd'); })
                .on('mouseout', function() { d3.select(this).attr('fill', '#f5f5f5'); });

            menu.append('text')
                .text(`${index + 1}. ${packedNode.ruleName} (Rule ${packedNode.ruleId})`)
                .attr('x', 0).attr('y', y + 5)
                .attr('text-anchor', 'middle').attr('font-size', 11);
        });
    }

    hideAmbiguityMenu() {
        this.svg.selectAll('.ambiguity-menu').remove();
    }

    selectPackedNode(nodeId, packedNodeIndex) {
        this.hideAmbiguityMenu();
        
        // Notify Blazor
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('HandlePackedNodeSelected', nodeId, packedNodeIndex);
        }
    }

    // ==================== Drag ====================

    dragstarted(event, d) {
        if (!event.active) this.simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
    }

    dragged(event, d) {
        d.fx = event.x;
        d.fy = event.y;
    }

    dragended(event, d) {
        if (!event.active) this.simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
    }

    // ==================== Utility ====================

    clear() {
        this.g.selectAll('*').remove();
        if (this.simulation) {
            this.simulation.stop();
            this.simulation = null;
        }
        this.hideAmbiguityMenu();
    }

    updateDimensions(width, height) {
        this.svg
            .attr('width', width)
            .attr('height', height);

        if (this.simulation) {
            this.simulation.force('center', d3.forceCenter(width / 2, height / 2));
        }
    }
}

// ==================== Public API ====================

window.CognitiveGraphVisualizer.render = function(canvasId, jsonData, mode, dotNetReference) {
    const container = document.getElementById(canvasId);
    if (!container) {
        console.error(`Canvas element ${canvasId} not found`);
        return;
    }

    // Create SVG if it doesn't exist
    let svg = d3.select(`#${canvasId}`);
    if (svg.empty()) {
        svg = d3.select(container).append('svg')
            .attr('id', canvasId)
            .attr('width', '100%')
            .attr('height', '100%');
    }

    // Parse data
    let graphData;
    try {
        graphData = JSON.parse(jsonData);
    } catch (e) {
        console.error('Error parsing visualization data:', e);
        return;
    }

    // Set mode
    const showAll = mode === 'ShowAllInterpretations';
    
    // Create or get visualizer
    let visualizer = new CognitiveGraphVisualizer(svg.node(), {
        showAllAlternatives: showAll
    });
    visualizer.dotNetReference = dotNetReference;

    // Update mode
    visualizer.showAllAlternatives = showAll;
    visualizer.svg.selectAll('.toggle-alternatives rect')
        .attr('fill', showAll ? '#e3f2fd' : '#f0f0f0');

    // Render
    visualizer.render(graphData);
};

// Ensure D3 is loaded
if (typeof d3 === 'undefined') {
    console.error('D3.js is not loaded. CognitiveGraphVisualizer requires D3.js.');
}
