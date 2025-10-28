// Renderer process script for Electron integration
// This file provides integration between Blazor and Electron

// Check if we're running in Electron
const isElectron = typeof window !== 'undefined' && window.electron !== undefined;

if (isElectron) {
    console.log('Running in Electron environment');
    
    // Setup menu event handlers
    window.electron.onMenuEvent('menu-new-grammar', () => {
        console.log('New Grammar triggered from menu');
        // Trigger Blazor component event
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnNewGrammar');
        }
    });
    
    window.electron.onMenuEvent('menu-open-grammar', (data) => {
        console.log('Open Grammar triggered from menu', data);
        if (window.DotNet && data) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnOpenGrammar', data.filePath, data.content);
        }
    });
    
    window.electron.onMenuEvent('menu-save-grammar', () => {
        console.log('Save Grammar triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnSaveGrammar');
        }
    });
    
    window.electron.onMenuEvent('menu-save-grammar-as', () => {
        console.log('Save Grammar As triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnSaveGrammarAs');
        }
    });
    
    window.electron.onMenuEvent('menu-load-sample', (sampleType) => {
        console.log('Load Sample triggered from menu', sampleType);
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnLoadSample', sampleType);
        }
    });
    
    window.electron.onMenuEvent('menu-export-svg', () => {
        console.log('Export SVG triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnExportSVG');
        }
    });
    
    window.electron.onMenuEvent('menu-export-png', () => {
        console.log('Export PNG triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnExportPNG');
        }
    });
    
    window.electron.onMenuEvent('menu-export-html', () => {
        console.log('Export HTML triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnExportHTML');
        }
    });
    
    window.electron.onMenuEvent('menu-show-tab', (tabName) => {
        console.log('Show Tab triggered from menu', tabName);
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnShowTab', tabName);
        }
    });
    
    window.electron.onMenuEvent('menu-toggle-theme', () => {
        console.log('Toggle Theme triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnToggleTheme');
        }
    });
    
    window.electron.onMenuEvent('menu-parse-grammar', () => {
        console.log('Parse Grammar triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnParseGrammar');
        }
    });
    
    window.electron.onMenuEvent('menu-validate-grammar', () => {
        console.log('Validate Grammar triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnValidateGrammar');
        }
    });
    
    window.electron.onMenuEvent('menu-generate-railroad', () => {
        console.log('Generate Railroad triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnGenerateRailroad');
        }
    });
    
    window.electron.onMenuEvent('menu-generate-parsetree', () => {
        console.log('Generate Parse Tree triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnGenerateParseTree');
        }
    });
    
    window.electron.onMenuEvent('menu-format-grammar', () => {
        console.log('Format Grammar triggered from menu');
        if (window.DotNet) {
            window.DotNet.invokeMethodAsync('Minotaur.UI.Blazor', 'OnFormatGrammar');
        }
    });
    
    // Utility functions for Blazor
    window.electronHelpers = {
        async showSaveDialog(defaultPath, title) {
            if (!isElectron) return null;
            
            const result = await window.electron.showSaveDialog({
                defaultPath: defaultPath,
                title: title
            });
            
            return result.canceled ? null : result.filePath;
        },
        
        async showOpenDialog(title) {
            if (!isElectron) return null;
            
            const result = await window.electron.showOpenDialog({
                title: title
            });
            
            return result.canceled ? null : result.filePaths[0];
        },
        
        async getAppInfo() {
            if (!isElectron) return { version: 'Web', name: 'Minotaur', isDev: false };
            
            const [version, name, isDev] = await Promise.all([
                window.electron.getAppVersion(),
                window.electron.getAppName(),
                window.electron.isDevelopment()
            ]);
            
            return { version, name, isDev };
        }
    };
    
    console.log('Electron renderer integration loaded');
} else {
    console.log('Running in web browser mode');
    
    // Provide stub implementations for web mode
    window.electronHelpers = {
        async showSaveDialog(defaultPath, title) {
            // Fallback for web - could use File System Access API
            return null;
        },
        
        async showOpenDialog(title) {
            // Fallback for web - could use File System Access API
            return null;
        },
        
        async getAppInfo() {
            return { version: 'Web 1.0.0', name: 'Minotaur Web', isDev: false };
        }
    };
}
