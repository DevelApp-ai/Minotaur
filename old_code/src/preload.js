const { contextBridge, ipcRenderer } = require('electron');
const path = require('path');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld(
  'electron', {
    fileSystem: {
      saveFile: (content, defaultPath, filters) => {
        return ipcRenderer.invoke('save-file', { content, defaultPath, filters });
      },
      joinPath: (defaultPath, filters) => path.join(defaultPath, filters),
      openFile: (defaultPath, filters) => {
        return ipcRenderer.invoke('open-file', { defaultPath, filters });
      },
    },
    parser: {
      exportParser: (grammar, outputDir) => {
        return ipcRenderer.invoke('export-parser', { grammar, outputDir });
      },
    },
    app: {
      getVersion: () => {
        return process.env.npm_package_version || '1.0.0';
      },
    },
  },
);
