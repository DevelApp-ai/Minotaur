// StepParser Integration JavaScript Module
window.stepParserModule = {
    currentVisualization: null,
    
    // Initialize step parser functionality
    initializeStepParser: function() {
        console.log('StepParser module initialized');
    },
    
    // Render a parsing step visualization
    renderParseStep: function(container, stepData) {
        if (!container || !stepData) return;
        
        // Clear previous content
        container.innerHTML = '';
        
        // Create step visualization
        const stepViz = document.createElement('div');
        stepViz.className = 'step-visualization-content';
        
        // Create step header
        const header = document.createElement('div');
        header.className = 'step-header';
        header.innerHTML = `
            <div class="step-operation">
                <span class="operation-badge operation-${stepData.operation.toLowerCase()}">${stepData.operation}</span>
                <span class="step-description">${stepData.description}</span>
            </div>
            ${stepData.ruleName ? `<span class="rule-badge">Rule: ${stepData.ruleName}</span>` : ''}
        `;
        
        // Create visual representation
        const visual = document.createElement('div');
        visual.className = 'step-visual';
        
        if (stepData.operation === 'SCAN') {
            visual.innerHTML = this.createScanVisualization(stepData);
        } else if (stepData.operation === 'REDUCE') {
            visual.innerHTML = this.createReduceVisualization(stepData);
        } else {
            visual.innerHTML = this.createGenericVisualization(stepData);
        }
        
        stepViz.appendChild(header);
        stepViz.appendChild(visual);
        container.appendChild(stepViz);
        
        this.currentVisualization = stepData;
    },
    
    // Create scan operation visualization
    createScanVisualization: function(stepData) {
        return `
            <div class="scan-visualization">
                <div class="scan-arrow">
                    <i class="bi bi-arrow-right text-primary"></i>
                    <span>Scanning position ${stepData.position}</span>
                </div>
                <div class="scan-progress">
                    <div class="progress-bar" style="width: ${(stepData.position / 100) * 100}%"></div>
                </div>
            </div>
        `;
    },
    
    // Create reduce operation visualization
    createReduceVisualization: function(stepData) {
        return `
            <div class="reduce-visualization">
                <div class="reduce-action">
                    <i class="bi bi-arrow-up text-success"></i>
                    <span>Reducing to ${stepData.ruleName || 'rule'}</span>
                </div>
                <div class="reduce-stack">
                    ${stepData.stack ? stepData.stack.map(item => 
                        `<div class="stack-item">${item}</div>`
                    ).join('') : ''}
                </div>
            </div>
        `;
    },
    
    // Create generic operation visualization
    createGenericVisualization: function(stepData) {
        return `
            <div class="generic-visualization">
                <div class="operation-info">
                    <i class="bi bi-gear text-secondary"></i>
                    <span>${stepData.description}</span>
                </div>
            </div>
        `;
    },
    
    // Focus an element (helper function)
    focusElement: function(element) {
        if (element) {
            element.focus();
        }
    }
};

// Global functions for Blazor interop
window.initializeStepParser = function() {
    window.stepParserModule.initializeStepParser();
};

window.renderParseStep = function(container, stepData) {
    window.stepParserModule.renderParseStep(container, stepData);
};

window.focusElement = function(element) {
    window.stepParserModule.focusElement(element);
};