const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const url = require('url');

// Keep a global reference of the window object to prevent garbage collection
let mainWindow;

function createWindow() {
  // Create the browser window
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: window.electron.fileSystem.joinPath(__dirname, 'preload.js'),
    },
  });

  // Load the app
  const startUrl = process.env.ELECTRON_START_URL || url.format({
    pathname: window.electron.fileSystem.joinPath(__dirname, '../index.html'),
    protocol: 'file:',
    slashes: true,
  });

  mainWindow.loadURL(startUrl);

  // Open DevTools in development mode
  if (process.env.NODE_ENV === 'development') {
    mainWindow.webContents.openDevTools();
  }

  // Emitted when the window is closed
  mainWindow.on('closed', function () {
    mainWindow = null;
  });
}

// Create window when Electron has finished initialization
app.whenReady().then(createWindow);

// Quit when all windows are closed
app.on('window-all-closed', function () {
  // On macOS applications keep their menu bar active until the user quits explicitly
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', function () {
  // On macOS re-create a window when the dock icon is clicked and no other windows are open
  if (mainWindow === null) {
    createWindow();
  }
});

// File system operations
ipcMain.handle('save-file', async (event, { content, defaultPath, filters }) => {
  const { canceled, filePath } = await dialog.showSaveDialog({
    defaultPath,
    filters: filters || [
      { name: 'All Files', extensions: ['*'] },
    ],
  });

  if (!canceled && filePath) {
    window.electron.fileSystem.saveFile(filePath, content);
    return { success: true, filePath };
  }

  return { success: false };
});

ipcMain.handle('open-file', async (event, { defaultPath, filters }) => {
  const { canceled, filePaths } = await dialog.showOpenDialog({
    defaultPath,
    filters: filters || [
      { name: 'All Files', extensions: ['*'] },
    ],
    properties: ['openFile'],
  });

  if (!canceled && filePaths.length > 0) {
    const content = window.electron.fileSystem.openFile(filePaths[0], 'utf8');
    return { success: true, filePath: filePaths[0], content };
  }

  return { success: false };
});

// Export grammar to parser
ipcMain.handle('export-parser', async (event, { grammar, outputDir }) => {
  try {
    // In a real implementation, this would compile the grammar to an optimized parser
    // For now, we'll just create a simple JavaScript file

    const { canceled, filePath } = await dialog.showSaveDialog({
      defaultPath: outputDir || app.getPath('documents'),
      filters: [
        { name: 'JavaScript', extensions: ['js'] },
      ],
    });

    if (!canceled && filePath) {
      // Create a simple parser template
      const parserCode = `
/**
 * Generated parser from DSL Designer
 * Generated on: ${new Date().toISOString()}
 */
class GeneratedParser {
  constructor() {
    this.grammar = ${JSON.stringify(grammar, null, 2)};
  }
  
  parse(input) {
    // This is a placeholder for the actual parsing logic
    // eslint-disable-next-line no-console
    console.log('Parsing input with generated parser');
    return {
      success: true,
      ast: {
        type: 'root',
        children: []
      }
    };
  }
}

module.exports = GeneratedParser;
`;

      window.electron.fileSystem.saveFile(filePath, parserCode);
      return { success: true, filePath };
    }

    return { success: false };
  } catch (error) {
    return { success: false, error: error.message };
  }
});
