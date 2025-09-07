import { app, BrowserWindow, ipcMain } from 'electron';
import * as url from 'url';

//Suttle import
let path: typeof import('path') | undefined;

if (typeof process !== 'undefined' && process.versions && process.versions.node) {
  path = require('path');
}

let mainWindow: BrowserWindow | null;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  const startUrl = process.env.ELECTRON_START_URL || url.format({
    pathname: path.join(__dirname, '../dist/index.html'),
    protocol: 'file:',
    slashes: true,
  });

  mainWindow.loadURL(startUrl);

  // Open DevTools in development mode
  if (process.env.NODE_ENV === 'development') {
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.on('ready', createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (mainWindow === null) {
    createWindow();
  }
});

// IPC handlers for communication between renderer and main process
ipcMain.handle('file-open', async (event, args) => {
  // File open implementation will go here
  return { success: true, data: 'File open placeholder' };
});

ipcMain.handle('file-save', async (event, args) => {
  // File save implementation will go here
  return { success: true, data: 'File save placeholder' };
});
