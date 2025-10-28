# Blazor-Electron Integration Guide

This document explains how to integrate the Minotaur Blazor UI with the Electron desktop application.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Electron (main.js)                       │
│  - Manages application lifecycle                           │
│  - Starts/stops Blazor Server                              │
│  - Provides native menus and dialogs                       │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                 Blazor Server (ASP.NET Core)                │
│  - Serves UI components                                     │
│  - Handles business logic                                   │
│  - Manages state                                            │
└─────────────────────────────────────────────────────────────┘
                            ↑
┌─────────────────────────────────────────────────────────────┐
│              Browser Window (Chromium via Electron)         │
│  - Renders Blazor UI                                        │
│  - Executes renderer.js for integration                    │
│  - Communicates via IPC through preload.js                 │
└─────────────────────────────────────────────────────────────┘
```

## Integration Points

### 1. Renderer Script in Blazor

Add the renderer.js script to your Blazor layout:

**File: `Minotaur.UI.Blazor/Components/Layout/MainLayout.razor`**

```html
<script src="/electron/renderer.js"></script>
```

### 2. Electron Helper JavaScript Functions

The `renderer.js` exposes `window.electronHelpers` with these functions:

```javascript
// Show native save dialog
const filePath = await window.electronHelpers.showSaveDialog('grammar.g4', 'Save Grammar');

// Show native open dialog
const filePath = await window.electronHelpers.showOpenDialog('Open Grammar');

// Get app information
const { version, name, isDev } = await window.electronHelpers.getAppInfo();
```

### 3. Blazor Component Integration

Create a service to handle Electron integration in Blazor:

**File: `Minotaur.UI.Blazor/Services/ElectronIntegrationService.cs`**

```csharp
using Microsoft.JSInterop;

namespace Minotaur.UI.Blazor.Services;

public class ElectronIntegrationService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isElectron;
    
    public ElectronIntegrationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task<bool> IsRunningInElectronAsync()
    {
        if (!_isElectron)
        {
            try
            {
                var info = await _jsRuntime.InvokeAsync<AppInfo>(
                    "electronHelpers.getAppInfo");
                _isElectron = info != null && info.Name.Contains("Electron");
            }
            catch
            {
                _isElectron = false;
            }
        }
        return _isElectron;
    }
    
    public async Task<string?> ShowSaveDialogAsync(string defaultPath, string title)
    {
        if (!await IsRunningInElectronAsync())
            return null;
            
        return await _jsRuntime.InvokeAsync<string?>(
            "electronHelpers.showSaveDialog", defaultPath, title);
    }
    
    public async Task<string?> ShowOpenDialogAsync(string title)
    {
        if (!await IsRunningInElectronAsync())
            return null;
            
        return await _jsRuntime.InvokeAsync<string?>(
            "electronHelpers.showOpenDialog", title);
    }
    
    public async Task<AppInfo> GetAppInfoAsync()
    {
        return await _jsRuntime.InvokeAsync<AppInfo>(
            "electronHelpers.getAppInfo");
    }
    
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    
    public class AppInfo
    {
        public string Version { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsDev { get; set; }
    }
}
```

### 4. Register Service in Program.cs

**File: `Minotaur.UI.Blazor/Program.cs`**

```csharp
// Add this line
builder.Services.AddScoped<ElectronIntegrationService>();
```

### 5. Menu Event Handlers

Create JSInvokable methods in your components to handle menu events:

**Example: GrammarEditor.razor**

```csharp
@inject IJSRuntime JSRuntime
@implements IAsyncDisposable

@code {
    [JSInvokable]
    public static Task OnNewGrammar()
    {
        // Handle new grammar
        return Task.CompletedTask;
    }
    
    [JSInvokable]
    public static Task OnOpenGrammar(string filePath, string content)
    {
        // Handle open grammar
        return Task.CompletedTask;
    }
    
    [JSInvokable]
    public static Task OnSaveGrammar()
    {
        // Handle save grammar
        return Task.CompletedTask;
    }
    
    // ... more handlers
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Register this component for menu callbacks
            await JSRuntime.InvokeVoidAsync(
                "eval", 
                "window.DotNet = DotNet");
        }
    }
}
```

### 6. Static Asset Configuration

Ensure renderer.js is accessible as a static asset:

**File: `Minotaur.UI.Blazor/wwwroot/electron/renderer.js`**

Copy the renderer.js file to this location, or add a symbolic link:

```bash
cd src/Minotaur.UI.Blazor/wwwroot
mkdir -p electron
cp ../../Minotaur.UI.Electron/renderer.js electron/
```

## Menu Integration Examples

### File Operations

```csharp
// In your GrammarEditor component
private async Task SaveGrammar()
{
    var electronService = /* inject ElectronIntegrationService */;
    
    if (await electronService.IsRunningInElectronAsync())
    {
        var filePath = await electronService.ShowSaveDialogAsync(
            "grammar.g4", 
            "Save Grammar File");
            
        if (filePath != null)
        {
            // Save file using System.IO
            await File.WriteAllTextAsync(filePath, grammarContent);
        }
    }
    else
    {
        // Fallback for web: use browser download
        await JSRuntime.InvokeVoidAsync("downloadFile", 
            "grammar.g4", grammarContent);
    }
}
```

### Navigation

```csharp
[JSInvokable]
public static Task OnShowTab(string tabName)
{
    // Navigate to the specified tab
    // This could use NavigationManager or state management
    return Task.CompletedTask;
}
```

### Theme Toggle

```csharp
[JSInvokable]
public static Task OnToggleTheme()
{
    // Toggle between light and dark theme
    // Update app state and CSS classes
    return Task.CompletedTask;
}
```

## Testing the Integration

### 1. Run Blazor Server Standalone

```bash
cd src/Minotaur.UI.Blazor
dotnet run
```

Visit http://localhost:5000 to test the web UI.

### 2. Run with Electron

```bash
cd src/Minotaur.UI.Electron
npm install  # First time only
npm run dev
```

This will start both the Blazor Server and Electron app.

### 3. Verify Integration

- Check that menu items work (File > New Grammar, etc.)
- Verify keyboard shortcuts (Ctrl+N, Ctrl+S, etc.)
- Test file dialogs (Open/Save)
- Confirm app info displays correctly in About dialog

## Deployment

### Build Electron App

```bash
cd src/Minotaur.UI.Electron
npm run build-linux
```

This creates distributable packages in `dist/`:
- AppImage (universal Linux)
- .deb (Debian/Ubuntu)
- .rpm (RedHat/Fedora)

### Package Blazor UI

The Blazor UI should be published in Release mode:

```bash
cd src/Minotaur.UI.Blazor
dotnet publish -c Release
```

### Production Considerations

1. **Port Management**: Ensure the Blazor port (5000) is available
2. **Security**: All communication is localhost-only
3. **Updates**: Use electron-updater for automatic updates
4. **Logging**: Check electron-log files for debugging

## Troubleshooting

### Menu Events Not Working

- Verify renderer.js is loaded in browser console
- Check that DotNet is available globally
- Ensure JSInvokable methods are static or properly registered

### File Dialogs Not Showing

- Confirm running in Electron (check window.electron exists)
- Verify preload.js is loaded
- Check IPC handlers are registered in main.js

### Blazor Server Connection Issues

- Check server is running on correct port
- Verify CORS settings if needed
- Review electron-log for server startup messages

## Best Practices

1. **Graceful Degradation**: Always provide web fallbacks
2. **Error Handling**: Wrap Electron calls in try-catch
3. **State Management**: Use Blazor's state management, not Electron's
4. **Security**: Never expose full IPC to renderer, use preload.js
5. **Testing**: Test both web and Electron modes regularly

## Additional Resources

- [Electron Documentation](https://www.electronjs.org/docs)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [Electron Security Best Practices](https://www.electronjs.org/docs/tutorial/security)
