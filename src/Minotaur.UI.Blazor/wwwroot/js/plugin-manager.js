// Plugin Manager JavaScript Module
window.pluginManagerModule = {
    loadedPlugins: new Map(),
    
    // Initialize plugin manager
    initializePluginManager: function() {
        console.log('Plugin Manager initialized');
        this.setupEventListeners();
    },
    
    // Setup event listeners
    setupEventListeners: function() {
        // Listen for plugin events
        document.addEventListener('pluginLoaded', (e) => {
            this.onPluginLoaded(e.detail);
        });
        
        document.addEventListener('pluginUnloaded', (e) => {
            this.onPluginUnloaded(e.detail);
        });
    },
    
    // Handle plugin loaded event
    onPluginLoaded: function(pluginInfo) {
        this.loadedPlugins.set(pluginInfo.id, pluginInfo);
        console.log(`Plugin loaded: ${pluginInfo.name}`);
        
        // Update UI indicators
        this.updatePluginStatusIndicators(pluginInfo.id, 'loaded');
    },
    
    // Handle plugin unloaded event
    onPluginUnloaded: function(pluginInfo) {
        this.loadedPlugins.delete(pluginInfo.id);
        console.log(`Plugin unloaded: ${pluginInfo.name}`);
        
        // Update UI indicators
        this.updatePluginStatusIndicators(pluginInfo.id, 'unloaded');
    },
    
    // Update plugin status indicators in UI
    updatePluginStatusIndicators: function(pluginId, status) {
        const indicators = document.querySelectorAll(`[data-plugin-id="${pluginId}"] .plugin-status`);
        indicators.forEach(indicator => {
            if (status === 'loaded') {
                indicator.innerHTML = '<i class="bi bi-check-circle text-success" title="Loaded"></i>';
            } else {
                indicator.innerHTML = '<i class="bi bi-circle text-muted" title="Not loaded"></i>';
            }
        });
    },
    
    // Simulate plugin loading with visual feedback
    simulatePluginLoading: function(pluginId, callback) {
        const loadingSteps = [
            'Validating plugin assembly...',
            'Checking dependencies...',
            'Loading plugin metadata...',
            'Initializing plugin...',
            'Plugin loaded successfully!'
        ];
        
        let currentStep = 0;
        const interval = setInterval(() => {
            if (currentStep < loadingSteps.length) {
                console.log(`${pluginId}: ${loadingSteps[currentStep]}`);
                currentStep++;
            } else {
                clearInterval(interval);
                if (callback) callback();
            }
        }, 500);
    },
    
    // Get plugin performance metrics (simulated)
    getPluginMetrics: function(pluginId) {
        const plugin = this.loadedPlugins.get(pluginId);
        if (!plugin) return null;
        
        return {
            memoryUsage: Math.floor(Math.random() * 100) + 10,
            cpuUsage: Math.floor(Math.random() * 20),
            callCount: Math.floor(Math.random() * 1000),
            uptime: Date.now() - (plugin.loadedAt || Date.now())
        };
    },
    
    // Monitor plugin health
    monitorPluginHealth: function() {
        this.loadedPlugins.forEach((plugin, pluginId) => {
            const metrics = this.getPluginMetrics(pluginId);
            if (metrics) {
                // Update metrics in UI
                this.updatePluginMetrics(pluginId, metrics);
                
                // Check for issues
                if (metrics.memoryUsage > 80) {
                    console.warn(`Plugin ${pluginId} has high memory usage: ${metrics.memoryUsage}MB`);
                }
                
                if (metrics.cpuUsage > 15) {
                    console.warn(`Plugin ${pluginId} has high CPU usage: ${metrics.cpuUsage}%`);
                }
            }
        });
    },
    
    // Update plugin metrics in UI
    updatePluginMetrics: function(pluginId, metrics) {
        const metricsElements = document.querySelectorAll(`[data-plugin-id="${pluginId}"] .plugin-metrics`);
        metricsElements.forEach(element => {
            const memoryElement = element.querySelector('.memory-usage');
            const cpuElement = element.querySelector('.cpu-usage');
            const callsElement = element.querySelector('.call-count');
            
            if (memoryElement) memoryElement.textContent = `${metrics.memoryUsage} MB`;
            if (cpuElement) cpuElement.textContent = `${metrics.cpuUsage}%`;
            if (callsElement) callsElement.textContent = metrics.callCount;
        });
    },
    
    // Export plugin configuration
    exportPluginConfig: function(pluginId) {
        const plugin = this.loadedPlugins.get(pluginId);
        if (!plugin) return null;
        
        const config = {
            pluginId: pluginId,
            name: plugin.name,
            version: plugin.version,
            configuration: plugin.configuration || {},
            exportedAt: new Date().toISOString()
        };
        
        // Create download link
        const blob = new Blob([JSON.stringify(config, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${pluginId}-config.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        
        return config;
    },
    
    // Import plugin configuration
    importPluginConfig: function(file, callback) {
        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const config = JSON.parse(e.target.result);
                if (callback) callback(config);
            } catch (error) {
                console.error('Failed to parse plugin configuration:', error);
                if (callback) callback(null, error);
            }
        };
        reader.readAsText(file);
    }
};

// Start monitoring when module loads
setInterval(() => {
    window.pluginManagerModule.monitorPluginHealth();
}, 5000);

// Global functions for Blazor interop
window.initializePluginManager = function() {
    window.pluginManagerModule.initializePluginManager();
};

window.simulatePluginLoading = function(pluginId, callback) {
    window.pluginManagerModule.simulatePluginLoading(pluginId, callback);
};

window.getPluginMetrics = function(pluginId) {
    return window.pluginManagerModule.getPluginMetrics(pluginId);
};

window.exportPluginConfig = function(pluginId) {
    return window.pluginManagerModule.exportPluginConfig(pluginId);
};

window.importPluginConfig = function(file, callback) {
    window.pluginManagerModule.importPluginConfig(file, callback);
};