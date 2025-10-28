const { app, BrowserWindow, Menu, ipcMain, dialog, shell } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
const log = require('electron-log');

// Configure logging
log.transports.file.level = 'info';
log.transports.console.level = 'debug';

class MinotaurElectronApp {
    constructor() {
        this.mainWindow = null;
        this.blazorProcess = null;
        this.blazorPort = 5000;
        this.isDevelopment = !app.isPackaged;
    }

    async createWindow() {
        // Create the browser window
        this.mainWindow = new BrowserWindow({
            width: 1400,
            height: 900,
            minWidth: 800,
            minHeight: 600,
            webPreferences: {
                nodeIntegration: false,
                contextIsolation: true,
                enableRemoteModule: false,
                preload: path.join(__dirname, 'preload.js'),
                webSecurity: true
            },
            icon: path.join(__dirname, 'assets/icon.png'),
            title: 'Minotaur Grammar Tool',
            show: false,
            titleBarStyle: 'default'
        });

        // Start Blazor Server
        await this.startBlazorServer();

        // Load the application
        const startUrl = `http://localhost:${this.blazorPort}`;
        log.info(`Loading application from: ${startUrl}`);
        
        await this.mainWindow.loadURL(startUrl);

        // Show window when ready
        this.mainWindow.once('ready-to-show', () => {
            this.mainWindow.show();
            
            if (this.isDevelopment) {
                this.mainWindow.webContents.openDevTools();
            }
        });

        // Handle window closed
        this.mainWindow.on('closed', () => {
            this.mainWindow = null;
            this.cleanup();
        });

        // Setup application menu
        this.setupMenu();
        
        // Setup IPC handlers
        this.setupIpcHandlers();

        // Handle external links
        this.mainWindow.webContents.setWindowOpenHandler(({ url }) => {
            shell.openExternal(url);
            return { action: 'deny' };
        });

        log.info('Main window created successfully');
    }

    async startBlazorServer() {
        return new Promise((resolve, reject) => {
            log.info('Starting Blazor server...');
            
            const blazorProjectPath = path.join(__dirname, '../Minotaur.UI.Blazor');
            
            this.blazorProcess = spawn('dotnet', [
                'run',
                '--project', blazorProjectPath,
                '--urls', `http://localhost:${this.blazorPort}`,
                '--environment', this.isDevelopment ? 'Development' : 'Production'
            ], {
                stdio: ['pipe', 'pipe', 'pipe'],
                cwd: blazorProjectPath
            });

            let resolved = false;

            this.blazorProcess.stdout.on('data', (data) => {
                const output = data.toString();
                log.info(`Blazor Server: ${output}`);
                
                if (!resolved && (output.includes('Application started') || output.includes('Now listening on'))) {
                    resolved = true;
                    log.info('Blazor server started successfully');
                    resolve();
                }
            });

            this.blazorProcess.stderr.on('data', (data) => {
                const error = data.toString();
                log.error(`Blazor Server Error: ${error}`);
                
                if (!resolved && error.includes('EADDRINUSE')) {
                    log.warn('Port in use, trying alternative...');
                    this.blazorPort = 5001;
                    this.blazorProcess.kill();
                    this.startBlazorServer().then(resolve).catch(reject);
                }
            });

            this.blazorProcess.on('error', (error) => {
                log.error('Failed to start Blazor Server:', error);
                if (!resolved) {
                    reject(error);
                }
            });

            this.blazorProcess.on('exit', (code, signal) => {
                log.info(`Blazor server exited with code ${code} and signal ${signal}`);
                if (!resolved && code !== 0) {
                    reject(new Error(`Blazor server failed to start (exit code: ${code})`));
                }
            });

            // Timeout fallback
            setTimeout(() => {
                if (!resolved) {
                    log.warn('Blazor server startup timeout, proceeding anyway...');
                    resolved = true;
                    resolve();
                }
            }, 15000);
        });
    }

    setupMenu() {
        const template = [
            {
                label: 'File',
                submenu: [
                    {
                        label: 'New Grammar',
                        accelerator: 'CmdOrCtrl+N',
                        click: () => this.sendToRenderer('menu-new-grammar')
                    },
                    {
                        label: 'Open Grammar...',
                        accelerator: 'CmdOrCtrl+O',
                        click: () => this.openGrammarFile()
                    },
                    {
                        label: 'Save Grammar',
                        accelerator: 'CmdOrCtrl+S',
                        click: () => this.sendToRenderer('menu-save-grammar')
                    },
                    {
                        label: 'Save Grammar As...',
                        accelerator: 'CmdOrCtrl+Shift+S',
                        click: () => this.sendToRenderer('menu-save-grammar-as')
                    },
                    { type: 'separator' },
                    {
                        label: 'Import Sample',
                        submenu: [
                            {
                                label: 'Simple Expression',
                                click: () => this.sendToRenderer('menu-load-sample', 'simple-expression')
                            },
                            {
                                label: 'JSON Grammar',
                                click: () => this.sendToRenderer('menu-load-sample', 'json')
                            }
                        ]
                    },
                    { type: 'separator' },
                    {
                        label: 'Export',
                        submenu: [
                            {
                                label: 'Export Railroad Diagram as SVG',
                                click: () => this.sendToRenderer('menu-export-svg')
                            },
                            {
                                label: 'Export Railroad Diagram as PNG',
                                click: () => this.sendToRenderer('menu-export-png')
                            },
                            {
                                label: 'Export Grammar as HTML',
                                click: () => this.sendToRenderer('menu-export-html')
                            }
                        ]
                    },
                    { type: 'separator' },
                    {
                        label: 'Quit',
                        accelerator: process.platform === 'darwin' ? 'Cmd+Q' : 'Ctrl+Q',
                        click: () => app.quit()
                    }
                ]
            },
            {
                label: 'Edit',
                submenu: [
                    { role: 'undo' },
                    { role: 'redo' },
                    { type: 'separator' },
                    { role: 'cut' },
                    { role: 'copy' },
                    { role: 'paste' },
                    { role: 'selectall' },
                    { type: 'separator' },
                    {
                        label: 'Find',
                        accelerator: 'CmdOrCtrl+F',
                        click: () => this.sendToRenderer('menu-find')
                    },
                    {
                        label: 'Replace',
                        accelerator: 'CmdOrCtrl+H',
                        click: () => this.sendToRenderer('menu-replace')
                    }
                ]
            },
            {
                label: 'View',
                submenu: [
                    {
                        label: 'Editor',
                        accelerator: 'CmdOrCtrl+1',
                        click: () => this.sendToRenderer('menu-show-tab', 'editor')
                    },
                    {
                        label: 'Visualization',
                        accelerator: 'CmdOrCtrl+2',
                        click: () => this.sendToRenderer('menu-show-tab', 'visualization')
                    },
                    {
                        label: 'Debugging',
                        accelerator: 'CmdOrCtrl+3',
                        click: () => this.sendToRenderer('menu-show-tab', 'debugging')
                    },
                    {
                        label: 'Settings',
                        accelerator: 'CmdOrCtrl+4',
                        click: () => this.sendToRenderer('menu-show-tab', 'settings')
                    },
                    { type: 'separator' },
                    {
                        label: 'Toggle Dark Theme',
                        accelerator: 'CmdOrCtrl+T',
                        click: () => this.sendToRenderer('menu-toggle-theme')
                    },
                    { type: 'separator' },
                    { role: 'reload' },
                    { role: 'forceReload' },
                    { role: 'toggleDevTools' },
                    { type: 'separator' },
                    { role: 'resetZoom' },
                    { role: 'zoomIn' },
                    { role: 'zoomOut' },
                    { type: 'separator' },
                    { role: 'togglefullscreen' }
                ]
            },
            {
                label: 'Grammar',
                submenu: [
                    {
                        label: 'Parse Grammar',
                        accelerator: 'F5',
                        click: () => this.sendToRenderer('menu-parse-grammar')
                    },
                    {
                        label: 'Validate Grammar',
                        accelerator: 'F7',
                        click: () => this.sendToRenderer('menu-validate-grammar')
                    },
                    { type: 'separator' },
                    {
                        label: 'Generate Railroad Diagram',
                        accelerator: 'F6',
                        click: () => this.sendToRenderer('menu-generate-railroad')
                    },
                    {
                        label: 'Generate Parse Tree',
                        accelerator: 'F8',
                        click: () => this.sendToRenderer('menu-generate-parsetree')
                    },
                    { type: 'separator' },
                    {
                        label: 'Format Grammar',
                        accelerator: 'CmdOrCtrl+Shift+F',
                        click: () => this.sendToRenderer('menu-format-grammar')
                    }
                ]
            },
            {
                label: 'Help',
                submenu: [
                    {
                        label: 'Documentation',
                        click: () => shell.openExternal('https://github.com/DevelApp-ai/Minotaur/wiki')
                    },
                    {
                        label: 'Report Issue',
                        click: () => shell.openExternal('https://github.com/DevelApp-ai/Minotaur/issues')
                    },
                    { type: 'separator' },
                    {
                        label: 'About Minotaur',
                        click: () => this.showAboutDialog()
                    }
                ]
            }
        ];

        const menu = Menu.buildFromTemplate(template);
        Menu.setApplicationMenu(menu);
    }

    setupIpcHandlers() {
        // File operations
        ipcMain.handle('show-save-dialog', async (event, options) => {
            const result = await dialog.showSaveDialog(this.mainWindow, {
                title: 'Save Grammar File',
                defaultPath: 'grammar.g4',
                filters: [
                    { name: 'ANTLR Grammar Files', extensions: ['g4'] },
                    { name: 'Grammar Files', extensions: ['antlr', 'ebnf'] },
                    { name: 'All Files', extensions: ['*'] }
                ],
                ...options
            });
            return result;
        });

        ipcMain.handle('show-open-dialog', async (event, options) => {
            const result = await dialog.showOpenDialog(this.mainWindow, {
                title: 'Open Grammar File',
                filters: [
                    { name: 'Grammar Files', extensions: ['g4', 'antlr', 'ebnf'] },
                    { name: 'All Files', extensions: ['*'] }
                ],
                properties: ['openFile'],
                ...options
            });
            return result;
        });

        // App information
        ipcMain.handle('get-app-version', () => app.getVersion());
        ipcMain.handle('get-app-name', () => app.getName());
        ipcMain.handle('is-development', () => this.isDevelopment);

        // Window operations
        ipcMain.handle('window-minimize', () => this.mainWindow?.minimize());
        ipcMain.handle('window-maximize', () => {
            if (this.mainWindow?.isMaximized()) {
                this.mainWindow.unmaximize();
            } else {
                this.mainWindow?.maximize();
            }
        });
        ipcMain.handle('window-close', () => this.mainWindow?.close());
    }

    async openGrammarFile() {
        try {
            const result = await dialog.showOpenDialog(this.mainWindow, {
                title: 'Open Grammar File',
                filters: [
                    { name: 'Grammar Files', extensions: ['g4', 'antlr', 'ebnf'] },
                    { name: 'All Files', extensions: ['*'] }
                ],
                properties: ['openFile']
            });

            if (!result.canceled && result.filePaths.length > 0) {
                const fs = require('fs').promises;
                const content = await fs.readFile(result.filePaths[0], 'utf8');
                this.sendToRenderer('menu-open-grammar', {
                    filePath: result.filePaths[0],
                    content: content
                });
            }
        } catch (error) {
            log.error('Failed to open grammar file:', error);
            dialog.showErrorBox('Error', `Failed to open file: ${error.message}`);
        }
    }

    sendToRenderer(channel, data) {
        if (this.mainWindow && !this.mainWindow.isDestroyed()) {
            this.mainWindow.webContents.send(channel, data);
        }
    }

    showAboutDialog() {
        dialog.showMessageBox(this.mainWindow, {
            type: 'info',
            title: 'About Minotaur Grammar Tool',
            message: 'Minotaur Grammar Tool',
            detail: `Version: ${app.getVersion()}\n\nA comprehensive grammar development and visualization tool with railroad diagram generation, parse tree visualization, and advanced debugging capabilities.\n\nDeveloped by DevelApp AI`,
            buttons: ['OK'],
            icon: path.join(__dirname, 'assets/icon.png')
        });
    }

    cleanup() {
        log.info('Cleaning up application...');
        
        if (this.blazorProcess && !this.blazorProcess.killed) {
            log.info('Terminating Blazor server...');
            this.blazorProcess.kill('SIGTERM');
            
            setTimeout(() => {
                if (!this.blazorProcess.killed) {
                    log.warn('Force killing Blazor server...');
                    this.blazorProcess.kill('SIGKILL');
                }
            }, 5000);
        }
    }
}

// Application instance
const minotaurApp = new MinotaurElectronApp();

// Application event handlers
app.whenReady().then(async () => {
    try {
        await minotaurApp.createWindow();
        log.info('Application started successfully');
    } catch (error) {
        log.error('Failed to start application:', error);
        app.quit();
    }
});

app.on('window-all-closed', () => {
    log.info('All windows closed');
    minotaurApp.cleanup();
    app.quit();
});

app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        await minotaurApp.createWindow();
    }
});

app.on('before-quit', () => {
    log.info('Application quitting...');
    minotaurApp.cleanup();
});

// Handle certificate errors
app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
    if (url.startsWith('http://localhost')) {
        // Allow localhost connections
        event.preventDefault();
        callback(true);
    } else {
        callback(false);
    }
});

// Security: Prevent new window creation
app.on('web-contents-created', (event, contents) => {
    contents.on('new-window', (navigationEvent, navigationUrl) => {
        event.preventDefault();
        shell.openExternal(navigationUrl);
    });
});
