import { contextBridge, ipcRenderer } from 'electron';

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld(
  'api', {
    fileOpen: async (options: any) => {
      return await ipcRenderer.invoke('file-open', options);
    },
    fileSave: async (data: any, options: any) => {
      return await ipcRenderer.invoke('file-save', { data, options });
    },
    // Add more API methods as needed
  },
);
