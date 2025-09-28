# MAUI + Blazor Hybrid Design Specification

## Executive Summary

This document provides a comprehensive design specification for implementing Minotaur's UI using a MAUI + Blazor Hybrid approach with cross-platform support including a dedicated Electron client for Linux. This hybrid solution combines the native performance and platform integration of MAUI with the rich web ecosystem capabilities of Blazor, offering the best of both worlds.

## Architecture Overview

### Multi-Platform Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    Platform Coverage                            │
├─────────────────┬─────────────────┬─────────────────────────────┤
│    Windows      │      macOS      │           Linux             │
│   MAUI Native   │   MAUI Native   │    Electron + Blazor        │
├─────────────────┼─────────────────┼─────────────────────────────┤
│    Android      │       iOS       │         Web Browser         │
│   MAUI Native   │   MAUI Native   │      Blazor Server          │
└─────────────────┴─────────────────┴─────────────────────────────┘
```

### Core Architecture Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    Presentation Layer                          │
├─────────────────┬─────────────────┬─────────────────────────────┤
│  MAUI Shell     │ Blazor WebView  │    Electron Wrapper         │
│  (Navigation)   │ (Visualizations)│    (Linux Only)             │
├─────────────────┼─────────────────┼─────────────────────────────┤
│                 │   Blazor Components                           │
│                 │   ├── RailroadDiagramViewer                   │
│                 │   ├── ParseTreeViewer                        │
│                 │   ├── GrammarGraphViewer                     │
│                 │   ├── CodeEditor (Monaco)                    │
│                 │   └── DebugVisualizers                       │
├─────────────────────────────────────────────────────────────────┤
│                    Business Logic Layer                        │
│   ├── MVVM ViewModels                                          │
│   ├── Services (DI Container)                                  │
│   ├── State Management                                         │
│   └── Platform Abstraction                                     │
├─────────────────────────────────────────────────────────────────┤
│                    Core Integration Layer                      │
│   ├── Direct Minotaur.Core Access                             │
│   ├── Grammar Services                                         │
│   ├── Parsing Engine Integration                               │
│   └── Export/Import Services                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

### Solution Organization

```
src/
├── Minotaur.UI.MAUI/                    # Main MAUI project
│   ├── Platforms/
│   │   ├── Windows/
│   │   ├── MacCatalyst/
│   │   ├── iOS/
│   │   └── Android/
│   ├── Views/                           # MAUI XAML pages
│   ├── ViewModels/                      # MVVM ViewModels
│   ├── Services/                        # Platform services
│   ├── wwwroot/                         # Blazor assets
│   └── MauiProgram.cs
├── Minotaur.UI.Blazor/                  # Shared Blazor components
│   ├── Components/
│   │   ├── Visualization/
│   │   │   ├── RailroadDiagramViewer.razor
│   │   │   ├── ParseTreeViewer.razor
│   │   │   └── GrammarGraphViewer.razor
│   │   ├── Editors/
│   │   │   ├── GrammarEditor.razor
│   │   │   └── SourceCodeEditor.razor
│   │   └── Debug/
│   │       ├── CharacterInspector.razor
│   │       ├── TokenStreamViewer.razor
│   │       └── StateInspector.razor
│   ├── Services/
│   └── wwwroot/
├── Minotaur.UI.Electron/                # Linux Electron wrapper
│   ├── main.js
│   ├── preload.js
│   ├── package.json
│   └── src/
│       └── blazor-host.html
├── Minotaur.UI.Shared/                  # Cross-platform shared code
│   ├── Models/
│   ├── Services/
│   │   ├── IGrammarService.cs
│   │   ├── IVisualizationService.cs
│   │   └── IPlatformService.cs
│   └── ViewModels/
└── Minotaur.UI.Web/                     # Blazor Server (optional)
    ├── Controllers/
    ├── Hubs/
    └── wwwroot/
```

## Component Design Specifications

### 1. MAUI Shell Architecture

#### MainShell.xaml
```xml
<Shell x:Class="Minotaur.UI.MAUI.MainShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    
    <TabBar>
        <ShellContent Title="Editor" 
                      Icon="editor.png"
                      ContentTemplate="{DataTemplate views:EditorPage}" />
        
        <ShellContent Title="Visualization" 
                      Icon="chart.png"
                      ContentTemplate="{DataTemplate views:VisualizationPage}" />
        
        <ShellContent Title="Debugging" 
                      Icon="debug.png"
                      ContentTemplate="{DataTemplate views:DebuggingPage}" />
        
        <ShellContent Title="Settings" 
                      Icon="settings.png"
                      ContentTemplate="{DataTemplate views:SettingsPage}" />
    </TabBar>
</Shell>
```

#### Native Features Integration
```csharp
// Platform-specific implementations
public interface IPlatformService
{
    Task<string> PickFileAsync();
    Task SaveFileAsync(string content, string filename);
    Task ShareAsync(string content);
    Task<bool> HasPermissionAsync<T>() where T : Permissions.BasePermission;
}

// Windows implementation
public class WindowsPlatformService : IPlatformService
{
    public async Task<string> PickFileAsync()
    {
        var result = await FilePicker.PickAsync(new PickOptions
        {
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".g4", ".antlr", ".txt" } }
            })
        });
        return result?.FullPath;
    }
}
```

### 2. Blazor WebView Integration

#### VisualizationPage.xaml
```xml
<ContentPage x:Class="Minotaur.UI.MAUI.Views.VisualizationPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:blazor="clr-namespace:Microsoft.AspNetCore.Components.WebView.Maui;assembly=Microsoft.AspNetCore.Components.WebView.Maui">
    
    <Grid>
        <blazor:BlazorWebView x:Name="blazorWebView" HostPage="wwwroot/index.html">
            <blazor:BlazorWebView.RootComponents>
                <blazor:RootComponent Selector="#app" ComponentType="{x:Type components:VisualizationHost}" />
            </blazor:BlazorWebView.RootComponents>
        </blazor:BlazorWebView>
    </Grid>
</ContentPage>
```

#### VisualizationHost.razor
```razor
@using Minotaur.UI.Blazor.Components.Visualization
@inject IJSRuntime JSRuntime
@inject GrammarService GrammarService

<div class="visualization-container">
    <div class="visualization-tabs">
        <button class="tab @(activeTab == "railroad" ? "active" : "")" 
                @onclick="() => SetActiveTab(\"railroad\")">
            Railroad Diagram
        </button>
        <button class="tab @(activeTab == "parseTree" ? "active" : "")" 
                @onclick="() => SetActiveTab(\"parseTree\")">
            Parse Tree
        </button>
        <button class="tab @(activeTab == "grammarGraph" ? "active" : "")" 
                @onclick="() => SetActiveTab(\"grammarGraph\")">
            Grammar Graph
        </button>
    </div>
    
    <div class="visualization-content">
        @if (activeTab == "railroad")
        {
            <RailroadDiagramViewer GrammarCode="@currentGrammar" 
                                   Options="@railroadOptions"
                                   OnDiagramGenerated="@OnDiagramGenerated" />
        }
        else if (activeTab == "parseTree")
        {
            <ParseTreeViewer ParseTree="@currentParseTree" />
        }
        else if (activeTab == "grammarGraph")
        {
            <GrammarGraphViewer GrammarCode="@currentGrammar" />
        }
    </div>
</div>

@code {
    private string activeTab = "railroad";
    private string currentGrammar = "";
    private object currentParseTree;
    private RailroadOptions railroadOptions = new();

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to grammar changes from MAUI
        await JSRuntime.InvokeVoidAsync("window.blazorInterop.subscribeToGrammarChanges", 
            DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public async Task UpdateGrammar(string grammarCode)
    {
        currentGrammar = grammarCode;
        await InvokeAsync(StateHasChanged);
    }

    private void SetActiveTab(string tab)
    {
        activeTab = tab;
        StateHasChanged();
    }

    private async Task OnDiagramGenerated(DiagramResult result)
    {
        // Notify MAUI of diagram generation completion
        await JSRuntime.InvokeVoidAsync("window.blazorInterop.notifyDiagramGenerated", result);
    }
}
```

### 3. Advanced Visualization Components

#### RailroadDiagramViewer.razor
```razor
@using Minotaur.Core
@inject IJSRuntime JSRuntime
@inject ILogger<RailroadDiagramViewer> Logger

<div class="railroad-diagram-container">
    <div class="railroad-controls">
        <div class="control-group">
            <button class="btn btn-primary" @onclick="GenerateDiagram" disabled="@isGenerating">
                @if (isGenerating)
                {
                    <span class="spinner"></span>
                    <text>Generating...</text>
                }
                else
                {
                    <text>Generate Diagram</text>
                }
            </button>
            
            @if (currentDiagram != null)
            {
                <button class="btn btn-secondary" @onclick="ExportSVG">Export SVG</button>
                <button class="btn btn-secondary" @onclick="ExportPNG">Export PNG</button>
            }
        </div>
        
        <div class="theme-selector">
            <select @bind="selectedTheme" @bind:after="OnThemeChanged">
                <option value="default">Default</option>
                <option value="dark">Dark</option>
                <option value="minimal">Minimal</option>
                <option value="academic">Academic</option>
            </select>
        </div>
    </div>
    
    <div class="diagram-viewport" @ref="viewportElement">
        @if (currentDiagram != null)
        {
            <div class="svg-container" @onwheel="OnWheel" @onmousedown="OnMouseDown">
                @((MarkupString)currentDiagram.SvgContent)
            </div>
        }
        else if (errorMessage != null)
        {
            <div class="error-message">
                <span class="error-icon">⚠️</span>
                <p>@errorMessage</p>
            </div>
        }
        else
        {
            <div class="empty-state">
                <span class="empty-icon">📊</span>
                <p>Enter grammar code and click Generate to see railroad diagram</p>
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public string GrammarCode { get; set; } = "";
    [Parameter] public RailroadOptions Options { get; set; } = new();
    [Parameter] public EventCallback<DiagramResult> OnDiagramGenerated { get; set; }

    private ElementReference viewportElement;
    private RailroadDiagram? currentDiagram;
    private bool isGenerating = false;
    private string? errorMessage;
    private string selectedTheme = "default";

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(GrammarCode) && currentDiagram == null)
        {
            await GenerateDiagram();
        }
    }

    private async Task GenerateDiagram()
    {
        if (string.IsNullOrWhiteSpace(GrammarCode))
            return;

        isGenerating = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Use the existing Minotaur.Core directly
            var generator = new RailroadGenerator();
            var grammar = ParseGrammar(GrammarCode);
            
            var options = new RailroadGenerationOptions
            {
                Theme = selectedTheme,
                Layout = new RailroadLayout { Direction = "horizontal" },
                IncludeInheritance = true,
                IncludeContextSensitive = true
            };

            var result = await generator.GenerateDiagramAsync(grammar, options);
            currentDiagram = result.Diagram;

            await OnDiagramGenerated.InvokeAsync(new DiagramResult
            {
                Success = true,
                Diagram = currentDiagram,
                Metrics = result.Metrics
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to generate diagram: {ex.Message}";
            Logger.LogError(ex, "Failed to generate railroad diagram");
            
            await OnDiagramGenerated.InvokeAsync(new DiagramResult
            {
                Success = false,
                Error = ex.Message
            });
        }
        finally
        {
            isGenerating = false;
            StateHasChanged();
        }
    }

    private async Task ExportSVG()
    {
        if (currentDiagram?.SvgContent != null)
        {
            await JSRuntime.InvokeVoidAsync("downloadFile", 
                currentDiagram.SvgContent, 
                "railroad-diagram.svg", 
                "image/svg+xml");
        }
    }

    private async Task ExportPNG()
    {
        if (currentDiagram?.SvgContent != null)
        {
            await JSRuntime.InvokeVoidAsync("exportSvgToPng", 
                currentDiagram.SvgContent, 
                "railroad-diagram.png");
        }
    }

    private Grammar ParseGrammar(string grammarCode)
    {
        // Integration with Minotaur.Core grammar parsing
        // This would use the existing parsing infrastructure
        throw new NotImplementedException("Grammar parsing integration");
    }

    private async Task OnThemeChanged()
    {
        if (currentDiagram != null)
        {
            await GenerateDiagram();
        }
    }

    private async Task OnWheel(WheelEventArgs e)
    {
        // Implement zoom functionality
        await JSRuntime.InvokeVoidAsync("handleZoom", viewportElement, e.DeltaY);
    }

    private async Task OnMouseDown(MouseEventArgs e)
    {
        // Implement pan functionality
        await JSRuntime.InvokeVoidAsync("handlePan", viewportElement, e.ClientX, e.ClientY);
    }
}
```

## Linux Electron Client Design

### Electron Main Process (main.js)
```javascript
const { app, BrowserWindow, Menu, ipcMain, dialog } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

class MinotaurElectronApp {
    constructor() {
        this.mainWindow = null;
        this.blazorProcess = null;
        this.blazorPort = 5000;
    }

    async createWindow() {
        // Create the browser window
        this.mainWindow = new BrowserWindow({
            width: 1400,
            height: 900,
            webPreferences: {
                nodeIntegration: false,
                contextIsolation: true,
                enableRemoteModule: false,
                preload: path.join(__dirname, 'preload.js')
            },
            icon: path.join(__dirname, 'assets/icon.png'),
            title: 'Minotaur Grammar Tool',
            show: false
        });

        // Start Blazor Server
        await this.startBlazorServer();

        // Load Blazor application
        await this.mainWindow.loadURL(`http://localhost:${this.blazorPort}`);

        this.mainWindow.once('ready-to-show', () => {
            this.mainWindow.show();
        });

        this.setupMenus();
        this.setupIpcHandlers();
    }

    async startBlazorServer() {
        return new Promise((resolve, reject) => {
            // Start the Blazor Server process
            this.blazorProcess = spawn('dotnet', [
                'run',
                '--project', path.join(__dirname, '../Minotaur.UI.Web'),
                '--urls', `http://localhost:${this.blazorPort}`
            ], {
                stdio: 'pipe'
            });

            this.blazorProcess.stdout.on('data', (data) => {
                console.log(`Blazor Server: ${data}`);
                if (data.toString().includes('Application started')) {
                    resolve();
                }
            });

            this.blazorProcess.stderr.on('data', (data) => {
                console.error(`Blazor Server Error: ${data}`);
            });

            this.blazorProcess.on('error', (error) => {
                console.error('Failed to start Blazor Server:', error);
                reject(error);
            });

            // Timeout fallback
            setTimeout(() => resolve(), 5000);
        });
    }

    setupMenus() {
        const template = [
            {
                label: 'File',
                submenu: [
                    {
                        label: 'New Grammar',
                        accelerator: 'CmdOrCtrl+N',
                        click: () => this.mainWindow.webContents.send('menu-new-grammar')
                    },
                    {
                        label: 'Open Grammar...',
                        accelerator: 'CmdOrCtrl+O',
                        click: async () => {
                            const result = await dialog.showOpenDialog(this.mainWindow, {
                                properties: ['openFile'],
                                filters: [
                                    { name: 'Grammar Files', extensions: ['g4', 'antlr', 'ebnf'] },
                                    { name: 'All Files', extensions: ['*'] }
                                ]
                            });

                            if (!result.canceled && result.filePaths.length > 0) {
                                this.mainWindow.webContents.send('menu-open-grammar', result.filePaths[0]);
                            }
                        }
                    },
                    {
                        label: 'Save Grammar',
                        accelerator: 'CmdOrCtrl+S',
                        click: () => this.mainWindow.webContents.send('menu-save-grammar')
                    },
                    { type: 'separator' },
                    {
                        label: 'Export Diagram...',
                        submenu: [
                            {
                                label: 'Export as SVG',
                                click: () => this.mainWindow.webContents.send('menu-export-svg')
                            },
                            {
                                label: 'Export as PNG',
                                click: () => this.mainWindow.webContents.send('menu-export-png')
                            }
                        ]
                    },
                    { type: 'separator' },
                    { role: 'quit' }
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
                    { role: 'selectall' }
                ]
            },
            {
                label: 'View',
                submenu: [
                    {
                        label: 'Editor',
                        accelerator: 'CmdOrCtrl+1',
                        click: () => this.mainWindow.webContents.send('menu-show-editor')
                    },
                    {
                        label: 'Visualization',
                        accelerator: 'CmdOrCtrl+2',
                        click: () => this.mainWindow.webContents.send('menu-show-visualization')
                    },
                    {
                        label: 'Debugging',
                        accelerator: 'CmdOrCtrl+3',
                        click: () => this.mainWindow.webContents.send('menu-show-debugging')
                    },
                    { type: 'separator' },
                    {
                        label: 'Toggle Theme',
                        accelerator: 'CmdOrCtrl+T',
                        click: () => this.mainWindow.webContents.send('menu-toggle-theme')
                    },
                    { type: 'separator' },
                    { role: 'reload' },
                    { role: 'forceReload' },
                    { role: 'toggleDevTools' }
                ]
            },
            {
                label: 'Grammar',
                submenu: [
                    {
                        label: 'Parse Grammar',
                        accelerator: 'F5',
                        click: () => this.mainWindow.webContents.send('menu-parse-grammar')
                    },
                    {
                        label: 'Generate Railroad Diagram',
                        accelerator: 'F6',
                        click: () => this.mainWindow.webContents.send('menu-generate-diagram')
                    },
                    { type: 'separator' },
                    {
                        label: 'Validate Grammar',
                        accelerator: 'F7',
                        click: () => this.mainWindow.webContents.send('menu-validate-grammar')
                    }
                ]
            },
            {
                label: 'Help',
                submenu: [
                    {
                        label: 'About Minotaur',
                        click: () => this.showAboutDialog()
                    },
                    {
                        label: 'Documentation',
                        click: () => {
                            require('electron').shell.openExternal('https://github.com/DevelApp-ai/Minotaur');
                        }
                    }
                ]
            }
        ];

        const menu = Menu.buildFromTemplate(template);
        Menu.setApplicationMenu(menu);
    }

    setupIpcHandlers() {
        ipcMain.handle('show-save-dialog', async (event, options) => {
            const result = await dialog.showSaveDialog(this.mainWindow, options);
            return result;
        });

        ipcMain.handle('show-open-dialog', async (event, options) => {
            const result = await dialog.showOpenDialog(this.mainWindow, options);
            return result;
        });

        ipcMain.handle('get-app-version', () => {
            return app.getVersion();
        });
    }

    showAboutDialog() {
        dialog.showMessageBox(this.mainWindow, {
            type: 'info',
            title: 'About Minotaur',
            message: 'Minotaur Grammar Tool',
            detail: `Version: ${app.getVersion()}\nA comprehensive grammar development and visualization tool.`
        });
    }

    cleanup() {
        if (this.blazorProcess) {
            this.blazorProcess.kill();
        }
    }
}

// Application lifecycle
const minotaurApp = new MinotaurElectronApp();

app.whenReady().then(() => {
    minotaurApp.createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            minotaurApp.createWindow();
        }
    });
});

app.on('window-all-closed', () => {
    minotaurApp.cleanup();
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('before-quit', () => {
    minotaurApp.cleanup();
});
```

### Electron Preload Script (preload.js)
```javascript
const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
    // File operations
    showSaveDialog: (options) => ipcRenderer.invoke('show-save-dialog', options),
    showOpenDialog: (options) => ipcRenderer.invoke('show-open-dialog', options),
    
    // App info
    getAppVersion: () => ipcRenderer.invoke('get-app-version'),
    
    // Menu event listeners
    onMenuAction: (callback) => {
        const menuEvents = [
            'menu-new-grammar',
            'menu-open-grammar',
            'menu-save-grammar',
            'menu-export-svg',
            'menu-export-png',
            'menu-show-editor',
            'menu-show-visualization',
            'menu-show-debugging',
            'menu-toggle-theme',
            'menu-parse-grammar',
            'menu-generate-diagram',
            'menu-validate-grammar'
        ];

        menuEvents.forEach(event => {
            ipcRenderer.on(event, (e, data) => callback(event, data));
        });
    },
    
    // Remove listeners
    removeAllListeners: (channel) => ipcRenderer.removeAllListeners(channel)
});

// Platform detection
contextBridge.exposeInMainWorld('platform', {
    isElectron: true,
    isLinux: process.platform === 'linux',
    isWindows: process.platform === 'win32',
    isMac: process.platform === 'darwin'
});
```

## State Management & Communication

### MVVM Architecture for MAUI

#### GrammarViewModel.cs
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Minotaur.UI.MAUI.ViewModels
{
    public partial class GrammarViewModel : ObservableObject
    {
        private readonly IGrammarService _grammarService;
        private readonly IVisualizationService _visualizationService;
        private readonly IPlatformService _platformService;

        [ObservableProperty]
        private string grammarCode = "";

        [ObservableProperty]
        private string sourceCode = "";

        [ObservableProperty]
        private bool isParseInProgress = false;

        [ObservableProperty]
        private ParseResult? lastParseResult;

        [ObservableProperty]
        private ObservableCollection<DiagnosticMessage> diagnostics = new();

        public GrammarViewModel(
            IGrammarService grammarService,
            IVisualizationService visualizationService,
            IPlatformService platformService)
        {
            _grammarService = grammarService;
            _visualizationService = visualizationService;
            _platformService = platformService;
        }

        [RelayCommand]
        private async Task ParseGrammarAsync()
        {
            if (string.IsNullOrWhiteSpace(GrammarCode))
                return;

            IsParseInProgress = true;
            Diagnostics.Clear();

            try
            {
                var result = await _grammarService.ParseGrammarAsync(GrammarCode);
                LastParseResult = result;

                if (result.Diagnostics.Any())
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Diagnostics.Add(diagnostic);
                    }
                }

                // Notify Blazor components of grammar changes
                await NotifyBlazorOfGrammarChange();
            }
            catch (Exception ex)
            {
                Diagnostics.Add(new DiagnosticMessage
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Parse failed: {ex.Message}",
                    Location = new SourceLocation(0, 0)
                });
            }
            finally
            {
                IsParseInProgress = false;
            }
        }

        [RelayCommand]
        private async Task OpenGrammarFileAsync()
        {
            try
            {
                var filePath = await _platformService.PickFileAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    GrammarCode = content;
                }
            }
            catch (Exception ex)
            {
                // Handle error
                Diagnostics.Add(new DiagnosticMessage
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Failed to open file: {ex.Message}",
                    Location = new SourceLocation(0, 0)
                });
            }
        }

        [RelayCommand]
        private async Task SaveGrammarFileAsync()
        {
            try
            {
                await _platformService.SaveFileAsync(GrammarCode, "grammar.g4");
            }
            catch (Exception ex)
            {
                Diagnostics.Add(new DiagnosticMessage
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Failed to save file: {ex.Message}",
                    Location = new SourceLocation(0, 0)
                });
            }
        }

        private async Task NotifyBlazorOfGrammarChange()
        {
            // This will be called from the platform-specific implementation
            // to communicate with the Blazor WebView
            if (Application.Current?.MainPage is Shell shell &&
                shell.CurrentPage is ContentPage page &&
                page.Content is Grid grid)
            {
                var blazorWebView = grid.Children.OfType<BlazorWebView>().FirstOrDefault();
                if (blazorWebView != null)
                {
                    await blazorWebView.InvokeJavaScriptAsync(
                        $"window.blazorInterop.updateGrammar('{GrammarCode.Replace("'", "\\'")}')");
                }
            }
        }

        partial void OnGrammarCodeChanged(string value)
        {
            // Auto-save or validation logic
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Debounce
                if (GrammarCode == value) // Still the same
                {
                    await NotifyBlazorOfGrammarChange();
                }
            });
        }
    }
}
```

### Blazor-MAUI Communication Bridge

#### JavaScript Interop (blazor-interop.js)
```javascript
window.blazorInterop = {
    // Communication from MAUI to Blazor
    updateGrammar: function(grammarCode) {
        if (window.blazorComponentRef) {
            window.blazorComponentRef.invokeMethodAsync('UpdateGrammar', grammarCode);
        }
    },

    subscribeToGrammarChanges: function(componentRef) {
        window.blazorComponentRef = componentRef;
    },

    // Communication from Blazor to MAUI
    notifyDiagramGenerated: function(result) {
        // This would trigger a custom event that MAUI can listen to
        if (window.webkit && window.webkit.messageHandlers) {
            // iOS/macOS
            window.webkit.messageHandlers.diagramGenerated.postMessage(result);
        } else if (window.chrome && window.chrome.webview) {
            // Windows WebView2
            window.chrome.webview.postMessage({
                type: 'diagramGenerated',
                data: result
            });
        } else if (window.electronAPI) {
            // Electron (Linux)
            // Handle through Electron IPC
        }
    },

    // File operations for web context
    downloadFile: function(content, filename, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // SVG to PNG conversion
    exportSvgToPng: function(svgContent, filename) {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        const img = new Image();
        
        img.onload = function() {
            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0);
            
            canvas.toBlob(function(blob) {
                window.blazorInterop.downloadFile(blob, filename, 'image/png');
            }, 'image/png');
        };
        
        const svgBlob = new Blob([svgContent], { type: 'image/svg+xml' });
        const url = URL.createObjectURL(svgBlob);
        img.src = url;
    },

    // Zoom and pan functionality
    handleZoom: function(element, deltaY) {
        const container = element.querySelector('.svg-container');
        if (container) {
            const currentScale = parseFloat(container.dataset.scale || '1');
            const newScale = deltaY > 0 ? currentScale * 0.9 : currentScale * 1.1;
            container.style.transform = `scale(${newScale})`;
            container.dataset.scale = newScale;
        }
    },

    handlePan: function(element, startX, startY) {
        const container = element.querySelector('.svg-container');
        if (container) {
            let isDragging = true;
            const startTransformX = parseFloat(container.dataset.translateX || '0');
            const startTransformY = parseFloat(container.dataset.translateY || '0');

            function onMouseMove(e) {
                if (isDragging) {
                    const deltaX = e.clientX - startX;
                    const deltaY = e.clientY - startY;
                    const newX = startTransformX + deltaX;
                    const newY = startTransformY + deltaY;
                    
                    container.style.transform = `translate(${newX}px, ${newY}px) scale(${container.dataset.scale || '1'})`;
                    container.dataset.translateX = newX;
                    container.dataset.translateY = newY;
                }
            }

            function onMouseUp() {
                isDragging = false;
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        }
    }
};
```

## Implementation Startup Guide

### Phase 1: Project Setup (Week 1-2)

#### 1.1 Create Solution Structure
```bash
# Create the main solution
dotnet new sln -n Minotaur.UI

# Create MAUI project
dotnet new maui -n Minotaur.UI.MAUI
dotnet sln add Minotaur.UI.MAUI

# Create Blazor component library
dotnet new blazorserver -n Minotaur.UI.Blazor
dotnet sln add Minotaur.UI.Blazor

# Create shared library
dotnet new classlib -n Minotaur.UI.Shared
dotnet sln add Minotaur.UI.Shared

# Create Electron project
mkdir Minotaur.UI.Electron
cd Minotaur.UI.Electron
npm init -y
npm install electron electron-builder
```

#### 1.2 Configure MAUI Project
```xml
<!-- Minotaur.UI.MAUI.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">net8.0-windows10.0.19041.0</TargetFrameworks>
        <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">net8.0-maccatalyst</TargetFrameworks>
        <TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('windows')) and !$([MSBuild]::IsOSPlatform('osx'))">net8.0</TargetFrameworks>
        
        <OutputType>Exe</OutputType>
        <RootNamespace>Minotaur.UI.MAUI</RootNamespace>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
        <ImplicitUsings>enable</ImplicitUsings>

        <!-- Display Version Info -->
        <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
        <ApplicationVersion>1</ApplicationVersion>

        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">13.1</SupportedOSPlatformVersion>
    </PropertyGroup>

    <ItemGroup>
        <!-- App Icon -->
        <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />

        <!-- Splash Screen -->
        <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />

        <!-- Images -->
        <MauiImage Include="Resources\Images\*" />
        <MauiImage Update="Resources\Images\dotnet_bot.svg" BaseSize="168,208" />

        <!-- Custom Fonts -->
        <MauiFont Include="Resources\Fonts\*" />

        <!-- Raw Assets (also remove the "Resources\Raw" prefix) -->
        <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
        <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.0" />
        <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.0" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
        <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Minotaur.UI.Shared\Minotaur.UI.Shared.csproj" />
        <ProjectReference Include="..\Minotaur.UI.Blazor\Minotaur.UI.Blazor.csproj" />
        <ProjectReference Include="..\..\src\Minotaur\Minotaur.csproj" />
    </ItemGroup>
</Project>
```

#### 1.3 Configure Dependency Injection (MauiProgram.cs)
```csharp
using Microsoft.Extensions.Logging;
using Minotaur.UI.MAUI.ViewModels;
using Minotaur.UI.MAUI.Services;
using Minotaur.UI.Shared.Services;

namespace Minotaur.UI.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Add Blazor WebView
        builder.Services.AddMauiBlazorWebView();

        // Add ViewModels
        builder.Services.AddTransient<GrammarViewModel>();
        builder.Services.AddTransient<VisualizationViewModel>();
        builder.Services.AddTransient<DebuggingViewModel>();

        // Add Services
        builder.Services.AddSingleton<IGrammarService, GrammarService>();
        builder.Services.AddSingleton<IVisualizationService, VisualizationService>();

        // Platform-specific services
#if WINDOWS
        builder.Services.AddSingleton<IPlatformService, WindowsPlatformService>();
#elif MACCATALYST
        builder.Services.AddSingleton<IPlatformService, MacCatalystPlatformService>();
#elif ANDROID
        builder.Services.AddSingleton<IPlatformService, AndroidPlatformService>();
#elif IOS
        builder.Services.AddSingleton<IPlatformService, iOSPlatformService>();
#endif

        // Add Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<EditorPage>();
        builder.Services.AddTransient<VisualizationPage>();
        builder.Services.AddTransient<DebuggingPage>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

### Phase 2: Core Implementation (Week 3-8)

#### 2.1 Service Implementation Priority
1. **IGrammarService** - Core grammar parsing and validation
2. **IVisualizationService** - Railroad diagram generation
3. **IPlatformService** - File operations and platform integration
4. **Communication layer** - MAUI ↔ Blazor interaction

#### 2.2 Component Development Order
1. **Basic MAUI Shell** - Navigation and layout
2. **Grammar Editor (Blazor)** - Monaco Editor integration
3. **Railroad Diagram Viewer (Blazor)** - SVG generation and display
4. **Parse Tree Viewer (Blazor)** - Tree visualization
5. **Grammar Graph Viewer (Blazor)** - Graph visualization

### Phase 3: Linux Electron Integration (Week 9-12)

#### 3.1 Electron Setup Steps
```bash
# Initialize Electron project
cd Minotaur.UI.Electron
npm install electron electron-builder concurrently wait-on

# Install additional dependencies
npm install electron-is electron-log electron-updater

# Configure package.json
cat > package.json << EOF
{
  "name": "minotaur-grammar-tool",
  "version": "1.0.0",
  "description": "Minotaur Grammar Development Tool",
  "main": "main.js",
  "scripts": {
    "start": "concurrently \"npm run start-blazor\" \"wait-on http://localhost:5000 && electron .\"",
    "start-blazor": "dotnet run --project ../Minotaur.UI.Web",
    "build": "electron-builder",
    "build-linux": "electron-builder --linux",
    "pack": "electron-builder --dir",
    "dist": "npm run build"
  },
  "build": {
    "appId": "com.develapp.minotaur",
    "productName": "Minotaur Grammar Tool",
    "directories": {
      "output": "dist"
    },
    "files": [
      "main.js",
      "preload.js",
      "assets/**/*"
    ],
    "linux": {
      "target": [
        {
          "target": "AppImage",
          "arch": ["x64"]
        },
        {
          "target": "deb",
          "arch": ["x64"]
        }
      ],
      "category": "Development"
    }
  }
}
EOF
```

## Testing Strategy

### Unit Testing
```csharp
// Test for GrammarViewModel
[Test]
public async Task ParseGrammarAsync_ValidGrammar_ShouldSucceed()
{
    // Arrange
    var mockGrammarService = new Mock<IGrammarService>();
    var mockVisualizationService = new Mock<IVisualizationService>();
    var mockPlatformService = new Mock<IPlatformService>();
    
    var viewModel = new GrammarViewModel(
        mockGrammarService.Object,
        mockVisualizationService.Object,
        mockPlatformService.Object);
    
    viewModel.GrammarCode = "grammar Test; rule: 'hello';";
    
    mockGrammarService.Setup(x => x.ParseGrammarAsync(It.IsAny<string>()))
                     .ReturnsAsync(new ParseResult { Success = true });
    
    // Act
    await viewModel.ParseGrammarCommand.ExecuteAsync(null);
    
    // Assert
    Assert.That(viewModel.LastParseResult?.Success, Is.True);
    Assert.That(viewModel.IsParseInProgress, Is.False);
}
```

### Integration Testing
```csharp
[Test]
public async Task BlazorMAUIIntegration_GrammarChange_ShouldUpdateVisualization()
{
    // Test the communication between MAUI and Blazor components
    // This would involve creating a test harness that can simulate
    // the WebView interaction
}
```

### E2E Testing with Playwright
```javascript
// Test for Electron app
const { test, expect } = require('@playwright/test');

test('should load Minotaur application', async ({ page }) => {
  await page.goto('http://localhost:5000');
  
  await expect(page.locator('h1')).toContainText('Minotaur Grammar Tool');
  await expect(page.locator('.grammar-editor')).toBeVisible();
  await expect(page.locator('.visualization-container')).toBeVisible();
});

test('should generate railroad diagram', async ({ page }) => {
  await page.goto('http://localhost:5000');
  
  await page.fill('.grammar-editor textarea', 'grammar Test; rule: "hello";');
  await page.click('button:has-text("Generate Diagram")');
  
  await expect(page.locator('.railroad-diagram svg')).toBeVisible();
});
```

## Deployment & Distribution

### Windows/macOS MAUI App
```xml
<!-- For Windows -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <WindowsPackageType>None</WindowsPackageType>
    <UseWinUI>true</UseWinUI>
</PropertyGroup>
```

### Linux Electron Distribution
```bash
# Build for Linux
npm run build-linux

# This creates:
# - AppImage for universal Linux distribution
# - .deb package for Debian/Ubuntu
# - Tar.gz archive for manual installation
```

### CI/CD Pipeline (GitHub Actions)
```yaml
name: Build and Release

on:
  push:
    tags: ['v*']

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Build MAUI Windows
      run: dotnet publish Minotaur.UI.MAUI -f net8.0-windows10.0.19041.0 -c Release
    
  build-macos:
    runs-on: macos-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Build MAUI macOS
      run: dotnet publish Minotaur.UI.MAUI -f net8.0-maccatalyst -c Release
    
  build-linux:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Build Electron Linux
      run: |
        cd Minotaur.UI.Electron
        npm install
        npm run build-linux
```

## Performance Considerations

### Memory Management
- **Blazor WebView**: Monitor memory usage, implement component disposal
- **SVG Rendering**: Cache generated diagrams, limit concurrent operations
- **Grammar Parsing**: Implement parsing debouncing and cancellation tokens

### Startup Performance
- **Lazy Loading**: Load Blazor components on demand
- **Pre-compilation**: Use AOT compilation for Blazor components
- **Asset Optimization**: Minimize JavaScript and CSS bundles

### Cross-Platform Optimization
- **Responsive Design**: Adapt UI for different screen sizes
- **Touch Support**: Implement touch gestures for mobile platforms
- **Keyboard Shortcuts**: Platform-specific accelerators

This comprehensive design specification provides a complete roadmap for implementing the MAUI + Blazor Hybrid solution with full Linux Electron support, addressing all aspects from architecture to deployment.