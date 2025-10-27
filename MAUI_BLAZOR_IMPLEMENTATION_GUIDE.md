# MAUI + Blazor Hybrid Implementation Guide

## Quick Start Implementation

This guide provides step-by-step instructions to immediately begin implementing the MAUI + Blazor Hybrid solution for Minotaur, including the Linux Electron client.

## Prerequisites

### Development Environment Setup
```bash
# Install .NET 8 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest
export PATH="$HOME/.dotnet:$PATH"

# Install MAUI workloads
dotnet workload install maui

# Install Node.js (for Electron)
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install Electron globally
npm install -g electron
```

### Visual Studio Setup
- **Windows**: Visual Studio 2022 with MAUI workload
- **macOS**: Visual Studio for Mac with MAUI workload  
- **Linux**: Visual Studio Code with C# extension

## Phase 1: Project Creation (Day 1-2)

### Step 1: Create Solution Structure
```bash
# Navigate to src directory
cd /home/runner/work/Minotaur/Minotaur/src

# Create new solution
dotnet new sln -n Minotaur.UI

# Create MAUI project
dotnet new maui -n Minotaur.UI.MAUI -f net8.0
cd Minotaur.UI.MAUI

# Add necessary NuGet packages
dotnet add package Microsoft.AspNetCore.Components.WebView.Maui --version 8.0.0
dotnet add package CommunityToolkit.Mvvm --version 8.2.0
dotnet add package SkiaSharp.Views.Maui.Controls --version 2.88.6

# Add reference to existing Minotaur core
dotnet add reference ../Minotaur/Minotaur.csproj

cd ..

# Create Blazor component library
dotnet new blazorserver -n Minotaur.UI.Blazor -f net8.0
cd Minotaur.UI.Blazor

# Add Blazor packages
dotnet add package Microsoft.AspNetCore.Components.Web --version 8.0.0
dotnet add package Microsoft.JSInterop --version 8.0.0

# Add reference to Minotaur core
dotnet add reference ../Minotaur/Minotaur.csproj

cd ..

# Create shared library
dotnet new classlib -n Minotaur.UI.Shared -f net8.0
cd Minotaur.UI.Shared
dotnet add reference ../Minotaur/Minotaur.csproj
cd ..

# Add projects to solution
dotnet sln Minotaur.UI.sln add Minotaur.UI.MAUI/Minotaur.UI.MAUI.csproj
dotnet sln Minotaur.UI.sln add Minotaur.UI.Blazor/Minotaur.UI.Blazor.csproj
dotnet sln Minotaur.UI.sln add Minotaur.UI.Shared/Minotaur.UI.Shared.csproj

# Create Electron project
mkdir Minotaur.UI.Electron
cd Minotaur.UI.Electron

# Initialize Electron package
npm init -y
npm install electron@latest electron-builder@latest concurrently@latest wait-on@latest
```

### Step 2: Configure MAUI Project Structure

#### Update Minotaur.UI.MAUI.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst</TargetFrameworks>
        <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net8.0-windows10.0.19041.0</TargetFrameworks>
        <OutputType>Exe</OutputType>
        <RootNamespace>Minotaur.UI.MAUI</RootNamespace>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
        <ImplicitUsings>enable</ImplicitUsings>

        <ApplicationTitle>Minotaur Grammar Tool</ApplicationTitle>
        <ApplicationId>com.develapp.minotaur</ApplicationId>
        <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
        <ApplicationVersion>1</ApplicationVersion>

        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">11.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">13.1</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
        <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
    </PropertyGroup>

    <ItemGroup>
        <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
        <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />
        <MauiImage Include="Resources\Images\*" />
        <MauiFont Include="Resources\Fonts\*" />
        <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
        <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.0" />
        <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.0" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
        <PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="2.88.6" />
        <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Minotaur.UI.Shared\Minotaur.UI.Shared.csproj" />
        <ProjectReference Include="..\Minotaur.UI.Blazor\Minotaur.UI.Blazor.csproj" />
        <ProjectReference Include="..\Minotaur\Minotaur.csproj" />
    </ItemGroup>
</Project>
```

## Phase 2: Core Implementation (Day 3-10)

### Step 3: Create Basic MAUI Structure

#### MauiProgram.cs
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

        builder.Services.AddMauiBlazorWebView();

        // Register ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<GrammarViewModel>();
        builder.Services.AddTransient<VisualizationViewModel>();

        // Register Services
        builder.Services.AddSingleton<IGrammarService, GrammarService>();
        builder.Services.AddSingleton<IVisualizationService, VisualizationService>();
        
        // Platform services
        builder.Services.AddSingleton<IPlatformService>(serviceProvider =>
        {
#if WINDOWS
            return new WindowsPlatformService();
#elif MACCATALYST
            return new MacCatalystPlatformService();
#elif ANDROID
            return new AndroidPlatformService();
#elif IOS
            return new IOSPlatformService();
#else
            return new DefaultPlatformService();
#endif
        });

        // Register Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

#### AppShell.xaml
```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="Minotaur.UI.MAUI.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:local="clr-namespace:Minotaur.UI.MAUI"
    Shell.FlyoutBehavior="Disabled"
    Title="Minotaur Grammar Tool">

    <TabBar>
        <ShellContent
            Title="Editor"
            ContentTemplate="{DataTemplate local:EditorPage}"
            Route="EditorPage" />

        <ShellContent
            Title="Visualization"
            ContentTemplate="{DataTemplate local:VisualizationPage}"
            Route="VisualizationPage" />

        <ShellContent
            Title="Debugging"
            ContentTemplate="{DataTemplate local:DebuggingPage}"
            Route="DebuggingPage" />

        <ShellContent
            Title="Settings"
            ContentTemplate="{DataTemplate local:SettingsPage}"
            Route="SettingsPage" />
    </TabBar>

</Shell>
```

### Step 4: Create Shared Services

#### Minotaur.UI.Shared/Services/IGrammarService.cs
```csharp
using Minotaur.Core;

namespace Minotaur.UI.Shared.Services;

public interface IGrammarService
{
    Task<ParseResult> ParseGrammarAsync(string grammarCode);
    Task<ValidationResult> ValidateGrammarAsync(string grammarCode);
    Task<List<GrammarSample>> GetSamplesAsync();
    event EventHandler<GrammarChangedEventArgs> GrammarChanged;
}

public class ParseResult
{
    public bool Success { get; set; }
    public object? ParseTree { get; set; }
    public List<DiagnosticMessage> Diagnostics { get; set; } = new();
    public TimeSpan ParseTime { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<DiagnosticMessage> Errors { get; set; } = new();
    public List<DiagnosticMessage> Warnings { get; set; } = new();
}

public class DiagnosticMessage
{
    public DiagnosticSeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public SourceLocation Location { get; set; }
}

public class SourceLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
    
    public SourceLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public class GrammarSample
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Content { get; set; } = "";
}

public class GrammarChangedEventArgs : EventArgs
{
    public string GrammarCode { get; set; } = "";
    public string Source { get; set; } = "";
}
```

#### Minotaur.UI.Shared/Services/GrammarService.cs
```csharp
using Minotaur.Core;
using Minotaur.Parser;
using Microsoft.Extensions.Logging;

namespace Minotaur.UI.Shared.Services;

public class GrammarService : IGrammarService
{
    private readonly ILogger<GrammarService> _logger;
    
    public event EventHandler<GrammarChangedEventArgs>? GrammarChanged;

    public GrammarService(ILogger<GrammarService> logger)
    {
        _logger = logger;
    }

    public async Task<ParseResult> ParseGrammarAsync(string grammarCode)
    {
        try
        {
            _logger.LogInformation("Parsing grammar code of length {Length}", grammarCode.Length);
            
            var startTime = DateTime.UtcNow;
            
            // Integration with existing Minotaur.Core
            // This would use the StepParser integration
            using var integration = StepParserIntegrationFactory.CreateForLanguage("antlr");
            
            var validation = await integration.ValidateSourceAsync(grammarCode);
            if (!validation.IsValid)
            {
                return new ParseResult
                {
                    Success = false,
                    Diagnostics = validation.Errors.Select(e => new DiagnosticMessage
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = e.Message,
                        Location = new SourceLocation(0, 0) // Would extract from error
                    }).ToList(),
                    ParseTime = DateTime.UtcNow - startTime
                };
            }

            using var editor = await integration.ParseToEditableGraphAsync(grammarCode);
            
            var result = new ParseResult
            {
                Success = true,
                ParseTree = editor.Root,
                ParseTime = DateTime.UtcNow - startTime
            };

            GrammarChanged?.Invoke(this, new GrammarChangedEventArgs 
            { 
                GrammarCode = grammarCode, 
                Source = "Parse" 
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse grammar");
            return new ParseResult
            {
                Success = false,
                Diagnostics = new List<DiagnosticMessage>
                {
                    new DiagnosticMessage
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = ex.Message,
                        Location = new SourceLocation(0, 0)
                    }
                }
            };
        }
    }

    public async Task<ValidationResult> ValidateGrammarAsync(string grammarCode)
    {
        try
        {
            using var integration = StepParserIntegrationFactory.CreateForLanguage("antlr");
            var validation = await integration.ValidateSourceAsync(grammarCode);
            
            return new ValidationResult
            {
                IsValid = validation.IsValid,
                Errors = validation.Errors.Select(e => new DiagnosticMessage
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = e.Message,
                    Location = new SourceLocation(0, 0)
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate grammar");
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<DiagnosticMessage>
                {
                    new DiagnosticMessage
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = ex.Message,
                        Location = new SourceLocation(0, 0)
                    }
                }
            };
        }
    }

    public async Task<List<GrammarSample>> GetSamplesAsync()
    {
        // Return sample grammars
        await Task.Delay(1); // Simulate async operation
        
        return new List<GrammarSample>
        {
            new GrammarSample
            {
                Name = "Simple Expression",
                Description = "Basic arithmetic expression grammar",
                Content = @"grammar Expr;

expr:   expr ('*'|'/') expr
    |   expr ('+'|'-') expr
    |   INT
    |   '(' expr ')'
    ;

INT :   [0-9]+ ;
WS  :   [ \t\r\n]+ -> skip ;"
            },
            new GrammarSample
            {
                Name = "JSON Grammar",
                Description = "Simple JSON parser grammar",
                Content = @"grammar JSON;

json:   value ;

value:  object
     |  array
     |  STRING
     |  NUMBER
     |  'true'
     |  'false'
     |  'null'
     ;

object: '{' pair (',' pair)* '}'
      | '{' '}'
      ;

pair: STRING ':' value ;

array: '[' value (',' value)* ']'
     | '[' ']'
     ;

STRING: '""' (ESC | ~[""\\])* '""' ;
NUMBER: '-'? INT ('.' [0-9]+)? EXP? ;

fragment INT: '0' | [1-9] [0-9]* ;
fragment EXP: [Ee] [+\-]? [0-9]+ ;
fragment ESC: '\\' ([""\\\/bfnrt] | UNICODE) ;
fragment UNICODE: 'u' HEX HEX HEX HEX ;
fragment HEX: [0-9a-fA-F] ;

WS: [ \t\n\r]+ -> skip ;"
            }
        };
    }
}
```

### Step 5: Create First Blazor Component

#### Minotaur.UI.Blazor/Components/GrammarEditor.razor
```razor
@using Microsoft.JSInterop
@inject IJSRuntime JSRuntime
@implements IDisposable

<div class="grammar-editor-container">
    <div class="editor-toolbar">
        <div class="toolbar-group">
            <button class="btn btn-primary" @onclick="OnParseClicked" disabled="@IsLoading">
                @if (IsLoading)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Parse Grammar
            </button>
            <button class="btn btn-secondary" @onclick="OnValidateClicked">
                Validate
            </button>
        </div>
        
        <div class="toolbar-group">
            <select class="form-select" @bind="SelectedSample" @bind:after="OnSampleChanged">
                <option value="">Select sample...</option>
                @foreach (var sample in Samples)
                {
                    <option value="@sample.Name">@sample.Name</option>
                }
            </select>
        </div>
    </div>
    
    <div class="editor-main">
        <div class="monaco-editor-container" @ref="editorContainer"></div>
    </div>
    
    @if (Diagnostics.Any())
    {
        <div class="diagnostics-panel">
            <h6>Diagnostics (@Diagnostics.Count)</h6>
            <div class="diagnostics-list">
                @foreach (var diagnostic in Diagnostics)
                {
                    <div class="diagnostic-item @GetDiagnosticClass(diagnostic.Severity)">
                        <span class="diagnostic-icon">@GetDiagnosticIcon(diagnostic.Severity)</span>
                        <span class="diagnostic-message">@diagnostic.Message</span>
                        @if (diagnostic.Location.Line > 0)
                        {
                            <span class="diagnostic-location">Line @diagnostic.Location.Line</span>
                        }
                    </div>
                }
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public string InitialContent { get; set; } = "";
    [Parameter] public EventCallback<string> OnContentChanged { get; set; }
    [Parameter] public EventCallback<ParseResult> OnParsed { get; set; }
    [Parameter] public List<GrammarSample> Samples { get; set; } = new();

    private ElementReference editorContainer;
    private IJSObjectReference? monacoEditor;
    private bool IsLoading = false;
    private string SelectedSample = "";
    private List<DiagnosticMessage> Diagnostics = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Initialize Monaco Editor
                monacoEditor = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "initializeMonacoEditor", 
                    editorContainer, 
                    InitialContent,
                    DotNetObjectReference.Create(this));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Monaco Editor: {ex.Message}");
            }
        }
    }

    [JSInvokable]
    public async Task OnEditorContentChanged(string content)
    {
        await OnContentChanged.InvokeAsync(content);
    }

    private async Task OnParseClicked()
    {
        if (monacoEditor == null) return;

        IsLoading = true;
        Diagnostics.Clear();
        StateHasChanged();

        try
        {
            var content = await monacoEditor.InvokeAsync<string>("getValue");
            
            // This would integrate with your GrammarService
            // For now, we'll simulate the parsing
            await Task.Delay(1000); // Simulate parsing time
            
            var result = new ParseResult
            {
                Success = true,
                ParseTime = TimeSpan.FromMilliseconds(100)
            };

            await OnParsed.InvokeAsync(result);
        }
        catch (Exception ex)
        {
            Diagnostics.Add(new DiagnosticMessage
            {
                Severity = DiagnosticSeverity.Error,
                Message = ex.Message,
                Location = new SourceLocation(0, 0)
            });
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnValidateClicked()
    {
        if (monacoEditor == null) return;

        var content = await monacoEditor.InvokeAsync<string>("getValue");
        // Implement validation logic
    }

    private async Task OnSampleChanged()
    {
        if (string.IsNullOrEmpty(SelectedSample) || monacoEditor == null) return;

        var sample = Samples.FirstOrDefault(s => s.Name == SelectedSample);
        if (sample != null)
        {
            await monacoEditor.InvokeVoidAsync("setValue", sample.Content);
        }
    }

    private string GetDiagnosticClass(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "diagnostic-error",
        DiagnosticSeverity.Warning => "diagnostic-warning",
        _ => "diagnostic-info"
    };

    private string GetDiagnosticIcon(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "❌",
        DiagnosticSeverity.Warning => "⚠️",
        _ => "ℹ️"
    };

    public void Dispose()
    {
        monacoEditor?.DisposeAsync();
    }
}

<style>
    .grammar-editor-container {
        display: flex;
        flex-direction: column;
        height: 100%;
        background: white;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }

    .editor-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px 16px;
        background: #f8f9fa;
        border-bottom: 1px solid #dee2e6;
    }

    .toolbar-group {
        display: flex;
        gap: 8px;
        align-items: center;
    }

    .editor-main {
        flex: 1;
        position: relative;
    }

    .monaco-editor-container {
        width: 100%;
        height: 100%;
        min-height: 400px;
    }

    .diagnostics-panel {
        max-height: 200px;
        overflow-y: auto;
        background: #f8f9fa;
        border-top: 1px solid #dee2e6;
        padding: 12px 16px;
    }

    .diagnostics-list {
        display: flex;
        flex-direction: column;
        gap: 4px;
    }

    .diagnostic-item {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 4px 8px;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .diagnostic-error {
        background: #f8d7da;
        color: #721c24;
    }

    .diagnostic-warning {
        background: #fff3cd;
        color: #856404;
    }

    .diagnostic-info {
        background: #d1ecf1;
        color: #0c5460;
    }

    .diagnostic-location {
        margin-left: auto;
        font-size: 0.75rem;
        opacity: 0.7;
    }

    .btn {
        padding: 6px 12px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
        transition: all 0.2s;
    }

    .btn-primary {
        background: #0d6efd;
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        background: #0b5ed7;
    }

    .btn-secondary {
        background: #6c757d;
        color: white;
    }

    .btn-secondary:hover {
        background: #5c636a;
    }

    .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .form-select {
        padding: 4px 8px;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .spinner-border {
        width: 1rem;
        height: 1rem;
        border: 0.125rem solid currentColor;
        border-right-color: transparent;
        border-radius: 50%;
        animation: spinner-border 0.75s linear infinite;
    }

    @keyframes spinner-border {
        to { transform: rotate(360deg); }
    }
</style>
```

### Step 6: Create JavaScript Interop

#### Minotaur.UI.Blazor/wwwroot/js/monaco-editor.js
```javascript
// Monaco Editor integration
window.initializeMonacoEditor = async (container, initialContent, dotNetRef) => {
    // Load Monaco Editor from CDN
    if (!window.monaco) {
        await loadMonacoEditor();
    }

    // Create editor instance
    const editor = monaco.editor.create(container, {
        value: initialContent || '',
        language: 'antlr',
        theme: 'vs-light',
        automaticLayout: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 14,
        lineNumbers: 'on',
        roundedSelection: false,
        scrollbar: {
            vertical: 'visible',
            horizontal: 'visible'
        },
        folding: true,
        links: true,
        wordWrap: 'on'
    });

    // Setup ANTLR language support
    setupAntlrLanguage();

    // Listen for content changes
    editor.onDidChangeModelContent((event) => {
        const content = editor.getValue();
        dotNetRef.invokeMethodAsync('OnEditorContentChanged', content);
    });

    // Return editor API
    return {
        getValue: () => editor.getValue(),
        setValue: (value) => editor.setValue(value),
        focus: () => editor.focus(),
        dispose: () => editor.dispose(),
        updateOptions: (options) => editor.updateOptions(options),
        setTheme: (theme) => monaco.editor.setTheme(theme)
    };
};

async function loadMonacoEditor() {
    return new Promise((resolve, reject) => {
        // Load Monaco Editor from CDN
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js';
        script.onload = () => {
            require.config({ 
                paths: { 
                    'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' 
                } 
            });
            require(['vs/editor/editor.main'], () => {
                resolve();
            });
        };
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

function setupAntlrLanguage() {
    // Register ANTLR language
    monaco.languages.register({ id: 'antlr' });

    // Set language configuration
    monaco.languages.setLanguageConfiguration('antlr', {
        comments: {
            lineComment: '//',
            blockComment: ['/*', '*/']
        },
        brackets: [
            ['{', '}'],
            ['[', ']'],
            ['(', ')']
        ],
        autoClosingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"' },
            { open: "'", close: "'" }
        ],
        surroundingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"' },
            { open: "'", close: "'" }
        ]
    });

    // Set tokenization rules
    monaco.languages.setMonarchTokenizer('antlr', {
        tokenizer: {
            root: [
                // Grammar name
                [/^grammar\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*;/, ['keyword', 'type.identifier']],
                
                // Keywords
                [/\b(grammar|fragment|lexer|parser|options|tokens|channels|mode|returns|locals|throws|catch|finally|import)\b/, 'keyword'],
                
                // Rule names
                [/^[a-z][a-zA-Z0-9_]*\s*:/, 'entity.name.function'],
                [/^[A-Z][A-Z0-9_]*\s*:/, 'entity.name.type'],
                
                // Strings
                [/'([^'\\]|\\.)*'/, 'string'],
                [/"([^"\\]|\\.)*"/, 'string'],
                
                // Comments
                [/\/\/.*$/, 'comment'],
                [/\/\*/, 'comment', '@comment'],
                
                // Operators
                [/[{}()\[\]]/, 'delimiter.bracket'],
                [/[;|*+?]/, 'delimiter'],
                [/[:=]/, 'operator'],
                
                // Whitespace
                [/\s+/, 'white']
            ],
            
            comment: [
                [/[^\/*]+/, 'comment'],
                [/\*\//, 'comment', '@pop'],
                [/[\/*]/, 'comment']
            ]
        }
    });

    // Set theme
    monaco.editor.defineTheme('antlr-theme', {
        base: 'vs',
        inherit: true,
        rules: [
            { token: 'keyword', foreground: '0000FF', fontStyle: 'bold' },
            { token: 'entity.name.function', foreground: '267F99' },
            { token: 'entity.name.type', foreground: '267F99', fontStyle: 'bold' },
            { token: 'string', foreground: 'A31515' },
            { token: 'comment', foreground: '008000', fontStyle: 'italic' }
        ],
        colors: {}
    });
}
```

## Phase 3: Linux Electron Setup (Day 11-14)

### Step 7: Create Electron Application

#### Minotaur.UI.Electron/package.json
```json
{
  "name": "minotaur-grammar-tool",
  "version": "1.0.0",
  "description": "Minotaur Grammar Development Tool for Linux",
  "main": "main.js",
  "scripts": {
    "start": "concurrently \"npm run start-blazor\" \"wait-on http://localhost:5000 && electron .\"",
    "start-blazor": "cd ../Minotaur.UI.Blazor && dotnet run --urls=http://localhost:5000",
    "dev": "npm run start",
    "build": "electron-builder",
    "build-linux": "electron-builder --linux",
    "pack": "electron-builder --dir",
    "dist": "npm run build",
    "postinstall": "electron-builder install-app-deps"
  },
  "keywords": ["grammar", "parser", "antlr", "railroad", "diagram"],
  "author": "DevelApp AI",
  "license": "AGPL-3.0-or-later",
  "devDependencies": {
    "electron": "^28.0.0",
    "electron-builder": "^24.6.4",
    "concurrently": "^8.2.2",
    "wait-on": "^7.2.0"
  },
  "dependencies": {
    "electron-log": "^5.0.1",
    "electron-updater": "^6.1.7"
  },
  "build": {
    "appId": "com.develapp.minotaur",
    "productName": "Minotaur Grammar Tool",
    "directories": {
      "output": "dist",
      "buildResources": "build"
    },
    "files": [
      "main.js",
      "preload.js",
      "renderer.js",
      "assets/**/*",
      "node_modules/**/*"
    ],
    "linux": {
      "target": [
        {
          "target": "AppImage",
          "arch": ["x64", "arm64"]
        },
        {
          "target": "deb",
          "arch": ["x64", "arm64"]
        },
        {
          "target": "rpm",
          "arch": ["x64", "arm64"]
        }
      ],
      "category": "Development",
      "icon": "assets/icon.png",
      "desktop": {
        "StartupWMClass": "minotaur-grammar-tool"
      }
    },
    "publish": {
      "provider": "github",
      "repo": "Minotaur",
      "owner": "DevelApp-ai"
    }
  }
}
```

#### Minotaur.UI.Electron/main.js
```javascript
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
```

## Quick Testing & Validation

### Test Script (test-setup.sh)
```bash
#!/bin/bash

echo "🚀 Testing Minotaur MAUI + Blazor Hybrid Setup"

# Test .NET installation
echo "📋 Checking .NET SDK..."
dotnet --version || { echo "❌ .NET SDK not found"; exit 1; }

# Test MAUI workload
echo "📋 Checking MAUI workload..."
dotnet workload list | grep maui || { echo "❌ MAUI workload not installed"; exit 1; }

# Test Node.js
echo "📋 Checking Node.js..."
node --version || { echo "❌ Node.js not found"; exit 1; }

# Test Electron
echo "📋 Checking Electron..."
npx electron --version || { echo "❌ Electron not found"; exit 1; }

echo "✅ All prerequisites satisfied!"

# Build test
echo "🔨 Testing build process..."
cd src
dotnet build Minotaur.UI.sln --configuration Debug

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fi

# Electron test
echo "🔨 Testing Electron setup..."
cd Minotaur.UI.Electron
npm install
npm run pack

if [ $? -eq 0 ]; then
    echo "✅ Electron setup successful!"
else
    echo "❌ Electron setup failed!"
    exit 1
fi

echo "🎉 All tests passed! Ready for development."
```

This implementation guide provides everything needed to start building the MAUI + Blazor Hybrid solution immediately, with specific focus on the Linux Electron client and comprehensive gap coverage from the old_code analysis.