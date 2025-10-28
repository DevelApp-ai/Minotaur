const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electron', {
    // File operations
    showSaveDialog: (options) => ipcRenderer.invoke('show-save-dialog', options),
    showOpenDialog: (options) => ipcRenderer.invoke('show-open-dialog', options),
    
    // App information
    getAppVersion: () => ipcRenderer.invoke('get-app-version'),
    getAppName: () => ipcRenderer.invoke('get-app-name'),
    isDevelopment: () => ipcRenderer.invoke('is-development'),
    
    // Window operations
    windowMinimize: () => ipcRenderer.invoke('window-minimize'),
    windowMaximize: () => ipcRenderer.invoke('window-maximize'),
    windowClose: () => ipcRenderer.invoke('window-close'),
    
    // Menu event listeners
    onMenuEvent: (channel, callback) => {
        const validChannels = [
            'menu-new-grammar',
            'menu-open-grammar',
            'menu-save-grammar',
            'menu-save-grammar-as',
            'menu-load-sample',
            'menu-export-svg',
            'menu-export-png',
            'menu-export-html',
            'menu-find',
            'menu-replace',
            'menu-show-tab',
            'menu-toggle-theme',
            'menu-parse-grammar',
            'menu-validate-grammar',
            'menu-generate-railroad',
            'menu-generate-parsetree',
            'menu-format-grammar'
        ];
        
        if (validChannels.includes(channel)) {
            ipcRenderer.on(channel, (event, ...args) => callback(...args));
        }
    },
    
    // Remove event listeners
    removeMenuListener: (channel) => {
        ipcRenderer.removeAllListeners(channel);
    }
});

// Log that preload script has been loaded
console.log('Electron preload script loaded successfully');
