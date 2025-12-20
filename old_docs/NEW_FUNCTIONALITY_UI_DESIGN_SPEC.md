# New Functionality UI Design Specification

## Overview

This document specifies the UI design for **new Minotaur functionality** that doesn't exist in the old_code, and describes the **adaptations required** for old_code UI components to work with the new cognitive graph system.

## New Functionality Analysis

### 🆕 Major New Features Not in Old Code

| Feature | Description | UI Requirements | Priority |
|---------|-------------|-----------------|----------|
| **Cognitive Graph Editor** | Zero-copy graph editing with CognitiveGraphNode | Interactive graph editor with node manipulation | 🔴 Critical |
| **StepParser Integration** | Advanced parsing with external parser support | Parser configuration UI, integration status | 🟡 Important |
| **Plugin System** | Extensible language plugins (C#, JS, Python, LLVM) | Plugin management interface, language selection | 🟡 Important |
| **Symbolic Analysis Engine** | KLEE-like symbolic analysis for error detection | Analysis results viewer, constraint visualization | 🟢 Nice-to-have |
| **Graph Unparser** | Code generation from cognitive graphs | Code generation controls, output preview | 🔴 Critical |
| **Multi-Language Support** | Built-in support for multiple target languages | Language switcher, syntax highlighting per language | 🟡 Important |

### 🔄 Required Adaptations from Old Code

| Old Component | Required Changes | New Integration | Status |
|---------------|------------------|-----------------|--------|
| **GrammarGraphViewer** | Adapt to CognitiveGraphNode instead of simple grammar rules | Direct cognitive graph rendering | ⚠️ Major Changes |
| **ParseTreeViewer** | Use StepParser results instead of mock data | CognitiveGraphNode tree structure | ⚠️ Major Changes |
| **EditorPanel** | Add cognitive graph editing capabilities | GraphEditor integration | ⚠️ Major Changes |
| **RailroadDiagramViewer** | Generate from cognitive graph structure | CognitiveGraphNode → Railroad conversion | ⚠️ Moderate Changes |
| **DebuggingPanel** | Add symbolic analysis integration | SymbolicAnalysisEngine results | ⚠️ Major Changes |

## Cognitive Graph UI Components Design

### 1. Cognitive Graph Editor Component

#### CognitiveGraphEditor.razor
```razor
@using Minotaur.Core
@using Minotaur.Editor
@inject IJSRuntime JSRuntime

<div class="cognitive-graph-editor">
    <div class="graph-toolbar">
        <div class="toolbar-section">
            <h5>Graph Structure</h5>
            <button class="btn btn-primary" @onclick="AddNode">Add Node</button>
            <button class="btn btn-secondary" @onclick="DeleteSelectedNode" disabled="@(selectedNodeId == null)">Delete Node</button>
            <button class="btn btn-info" @onclick="ToggleEditMode">@(isEditMode ? "View Mode" : "Edit Mode")</button>
        </div>
        
        <div class="toolbar-section">
            <h5>Graph Operations</h5>
            <button class="btn btn-success" @onclick="SaveGraph">Save</button>
            <button class="btn btn-warning" @onclick="UndoLastOperation" disabled="@(!canUndo)">Undo</button>
            <button class="btn btn-warning" @onclick="RedoLastOperation" disabled="@(!canRedo)">Redo</button>
        </div>
        
        <div class="toolbar-section">
            <h5>Node Properties</h5>
            @if (selectedNode != null)
            {
                <div class="node-properties">
                    <label>Node Type:</label>
                    <input @bind="selectedNode.NodeType" class="form-control" />
                    
                    <label>Node ID:</label>
                    <input value="@selectedNode.Id" readonly class="form-control" />
                    
                    @if (selectedNode is NonTerminalNode ntNode)
                    {
                        <label>Children Count:</label>
                        <span class="badge badge-info">@ntNode.Children.Count</span>
                    }
                    
                    @if (selectedNode is TerminalNode tNode)
                    {
                        <label>Value:</label>
                        <input @bind="tNode.Value" class="form-control" />
                    }
                </div>
            }
        </div>
    </div>
    
    <div class="graph-canvas-container">
        <div class="graph-canvas" @ref="canvasElement">
            <!-- Cognitive graph will be rendered here via JavaScript -->
        </div>
    </div>
    
    <div class="graph-status">
        <span class="status-item">Nodes: @totalNodes</span>
        <span class="status-item">Connections: @totalConnections</span>
        <span class="status-item">Selected: @(selectedNode?.NodeType ?? "None")</span>
        <span class="status-item">Mode: @(isEditMode ? "Edit" : "View")</span>
    </div>
</div>

@code {
    [Parameter] public CognitiveGraphNode RootNode { get; set; }
    [Parameter] public EventCallback<CognitiveGraphNode> OnGraphChanged { get; set; }
    [Parameter] public EventCallback<CognitiveGraphNode> OnNodeSelected { get; set; }

    private ElementReference canvasElement;
    private GraphEditor graphEditor;
    private CognitiveGraphNode selectedNode;
    private Guid? selectedNodeId;
    private bool isEditMode = false;
    private bool canUndo = false;
    private bool canRedo = false;
    private int totalNodes = 0;
    private int totalConnections = 0;

    protected override async Task OnInitializedAsync()
    {
        if (RootNode != null)
        {
            graphEditor = new GraphEditor(RootNode);
            await UpdateGraphStats();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RootNode != null)
        {
            await RenderCognitiveGraph();
        }
    }

    private async Task RenderCognitiveGraph()
    {
        var graphData = ConvertCognitiveGraphToVisualizationData(RootNode);
        await JSRuntime.InvokeVoidAsync("renderCognitiveGraph", canvasElement, graphData);
    }

    private async Task AddNode()
    {
        if (selectedNode != null && selectedNode is NonTerminalNode parentNode)
        {
            var newNode = new TerminalNode("NewNode", "terminal");
            graphEditor.InsertNode(parentNode.Id, newNode);
            
            await UpdateGraphStats();
            await RenderCognitiveGraph();
            await OnGraphChanged.InvokeAsync(RootNode);
        }
    }

    private async Task DeleteSelectedNode()
    {
        if (selectedNodeId.HasValue)
        {
            graphEditor.DeleteNode(selectedNodeId.Value);
            selectedNode = null;
            selectedNodeId = null;
            
            await UpdateGraphStats();
            await RenderCognitiveGraph();
            await OnGraphChanged.InvokeAsync(RootNode);
        }
    }

    private async Task UndoLastOperation()
    {
        if (graphEditor.CanUndo)
        {
            graphEditor.Undo();
            await UpdateGraphStats();
            await RenderCognitiveGraph();
            await OnGraphChanged.InvokeAsync(RootNode);
        }
    }

    private async Task RedoLastOperation()
    {
        if (graphEditor.CanRedo)
        {
            graphEditor.Redo();
            await UpdateGraphStats();
            await RenderCognitiveGraph();
            await OnGraphChanged.InvokeAsync(RootNode);
        }
    }

    private async Task ToggleEditMode()
    {
        isEditMode = !isEditMode;
        await JSRuntime.InvokeVoidAsync("setCognitiveGraphEditMode", canvasElement, isEditMode);
    }

    private async Task SaveGraph()
    {
        // Implementation for saving the graph
        await OnGraphChanged.InvokeAsync(RootNode);
    }

    private async Task UpdateGraphStats()
    {
        totalNodes = CountNodes(RootNode);
        totalConnections = CountConnections(RootNode);
        canUndo = graphEditor?.CanUndo ?? false;
        canRedo = graphEditor?.CanRedo ?? false;
        StateHasChanged();
    }

    private int CountNodes(CognitiveGraphNode node)
    {
        if (node == null) return 0;
        
        int count = 1;
        if (node is NonTerminalNode ntNode)
        {
            foreach (var child in ntNode.Children)
            {
                count += CountNodes(child);
            }
        }
        return count;
    }

    private int CountConnections(CognitiveGraphNode node)
    {
        if (node == null || !(node is NonTerminalNode ntNode)) return 0;
        
        int count = ntNode.Children.Count;
        foreach (var child in ntNode.Children)
        {
            count += CountConnections(child);
        }
        return count;
    }

    private object ConvertCognitiveGraphToVisualizationData(CognitiveGraphNode node)
    {
        var nodes = new List<object>();
        var edges = new List<object>();
        
        ConvertNodeRecursive(node, nodes, edges, 0, 0);
        
        return new { nodes, edges };
    }

    private void ConvertNodeRecursive(CognitiveGraphNode node, List<object> nodes, List<object> edges, int x, int y)
    {
        if (node == null) return;

        nodes.Add(new
        {
            id = node.Id.ToString(),
            label = $"{node.NodeType}: {GetNodeDisplayText(node)}",
            x = x,
            y = y,
            type = node.GetType().Name,
            nodeType = node.NodeType,
            metadata = node.Metadata
        });

        if (node is NonTerminalNode ntNode)
        {
            int childIndex = 0;
            foreach (var child in ntNode.Children)
            {
                var childX = x + (childIndex - ntNode.Children.Count / 2) * 150;
                var childY = y + 100;
                
                ConvertNodeRecursive(child, nodes, edges, childX, childY);
                
                edges.Add(new
                {
                    id = $"{node.Id}-{child.Id}",
                    source = node.Id.ToString(),
                    target = child.Id.ToString(),
                    type = "cognitive"
                });
                
                childIndex++;
            }
        }
    }

    private string GetNodeDisplayText(CognitiveGraphNode node)
    {
        return node switch
        {
            TerminalNode terminal => terminal.Value,
            IdentifierNode identifier => identifier.Name,
            LiteralNode literal => literal.Value?.ToString() ?? "",
            _ => node.NodeType
        };
    }
}
```

### 2. StepParser Integration Component

#### StepParserIntegrationPanel.razor
```razor
@using Minotaur.Parser
@using Minotaur.Plugins
@inject LanguagePluginManager PluginManager

<div class="stepparser-integration-panel">
    <div class="integration-header">
        <h4>StepParser Integration</h4>
        <div class="integration-status">
            <span class="status-badge @(isIntegrationActive ? "status-active" : "status-inactive")">
                @(isIntegrationActive ? "Active" : "Inactive")
            </span>
        </div>
    </div>
    
    <div class="integration-config">
        <div class="config-section">
            <h5>Parser Configuration</h5>
            <div class="form-group">
                <label>Target Language:</label>
                <select @bind="selectedLanguage" class="form-control">
                    @foreach (var plugin in availablePlugins)
                    {
                        <option value="@plugin.LanguageId">@plugin.DisplayName</option>
                    }
                </select>
            </div>
            
            <div class="form-group">
                <label>Include Location Info:</label>
                <input type="checkbox" @bind="parserConfig.IncludeLocationInfo" />
            </div>
            
            <div class="form-group">
                <label>Preserve Comments:</label>
                <input type="checkbox" @bind="parserConfig.PreserveComments" />
            </div>
            
            <div class="form-group">
                <label>Include Whitespace:</label>
                <input type="checkbox" @bind="parserConfig.IncludeWhitespace" />
            </div>
        </div>
        
        <div class="config-section">
            <h5>Integration Actions</h5>
            <button class="btn btn-primary" @onclick="TestIntegration">Test Integration</button>
            <button class="btn btn-success" @onclick="ParseWithStepParser" disabled="@(string.IsNullOrEmpty(inputCode))">Parse Code</button>
            <button class="btn btn-warning" @onclick="ValidateIntegration">Validate Setup</button>
        </div>
    </div>
    
    <div class="integration-input">
        <h5>Input Code</h5>
        <textarea @bind="inputCode" class="form-control code-input" rows="10" 
                  placeholder="Enter source code to parse..."></textarea>
    </div>
    
    <div class="integration-results">
        <h5>Results</h5>
        <div class="results-tabs">
            <button class="tab @(activeResultTab == "graph" ? "active" : "")" 
                    @onclick="() => activeResultTab = \"graph\"">Cognitive Graph</button>
            <button class="tab @(activeResultTab == "diagnostics" ? "active" : "")" 
                    @onclick="() => activeResultTab = \"diagnostics\"">Diagnostics</button>
            <button class="tab @(activeResultTab == "metrics" ? "active" : "")" 
                    @onclick="() => activeResultTab = \"metrics\"">Metrics</button>
        </div>
        
        <div class="results-content">
            @if (activeResultTab == "graph" && parsedGraph != null)
            {
                <CognitiveGraphEditor RootNode="@parsedGraph" OnGraphChanged="@OnGraphChanged" />
            }
            else if (activeResultTab == "diagnostics")
            {
                <div class="diagnostics-list">
                    @foreach (var diagnostic in diagnostics)
                    {
                        <div class="diagnostic-item @GetDiagnosticClass(diagnostic.Level)">
                            <span class="diagnostic-icon">@GetDiagnosticIcon(diagnostic.Level)</span>
                            <span class="diagnostic-message">@diagnostic.Message</span>
                            <span class="diagnostic-location">Line @diagnostic.Line, Column @diagnostic.Column</span>
                        </div>
                    }
                </div>
            }
            else if (activeResultTab == "metrics")
            {
                <div class="metrics-display">
                    <div class="metric-item">
                        <label>Parse Time:</label>
                        <span>@parseMetrics.ParseTime ms</span>
                    </div>
                    <div class="metric-item">
                        <label>Token Count:</label>
                        <span>@parseMetrics.TokenCount</span>
                    </div>
                    <div class="metric-item">
                        <label>Node Count:</label>
                        <span>@parseMetrics.NodeCount</span>
                    </div>
                    <div class="metric-item">
                        <label>Memory Usage:</label>
                        <span>@parseMetrics.MemoryUsage KB</span>
                    </div>
                </div>
            }
        </div>
    </div>
</div>

@code {
    [Parameter] public EventCallback<CognitiveGraphNode> OnParseCompleted { get; set; }

    private ParserConfiguration parserConfig = new();
    private string selectedLanguage = "csharp";
    private string inputCode = "";
    private string activeResultTab = "graph";
    private bool isIntegrationActive = false;
    private List<ILanguagePlugin> availablePlugins = new();
    private CognitiveGraphNode parsedGraph;
    private List<ParseDiagnostic> diagnostics = new();
    private ParseMetrics parseMetrics = new();

    protected override async Task OnInitializedAsync()
    {
        availablePlugins = PluginManager.RegisteredPlugins.Values.ToList();
        selectedLanguage = availablePlugins.FirstOrDefault()?.LanguageId ?? "csharp";
        
        await TestIntegration();
    }

    private async Task TestIntegration()
    {
        try
        {
            using var integration = StepParserIntegrationFactory.CreateForLanguage(selectedLanguage);
            var testResult = await integration.ValidateSourceAsync("// Test");
            isIntegrationActive = testResult.IsValid || testResult.Errors.Length == 0;
        }
        catch
        {
            isIntegrationActive = false;
        }
        StateHasChanged();
    }

    private async Task ParseWithStepParser()
    {
        if (string.IsNullOrEmpty(inputCode)) return;

        diagnostics.Clear();
        var startTime = DateTime.UtcNow;

        try
        {
            using var integration = StepParserIntegrationFactory.CreateForLanguage(selectedLanguage);
            
            // First validate
            var validation = await integration.ValidateSourceAsync(inputCode);
            foreach (var error in validation.Errors)
            {
                diagnostics.Add(new ParseDiagnostic
                {
                    Level = DiagnosticLevel.Error,
                    Message = error.Message,
                    Line = 0, // Would extract from error if available
                    Column = 0
                });
            }

            // If valid, parse to cognitive graph
            if (validation.IsValid)
            {
                using var editor = await integration.ParseToEditableGraphAsync(inputCode);
                parsedGraph = editor.Root;
                
                parseMetrics = new ParseMetrics
                {
                    ParseTime = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    TokenCount = validation.TokenCount,
                    NodeCount = CountNodes(parsedGraph),
                    MemoryUsage = GC.GetTotalMemory(false) / 1024
                };

                await OnParseCompleted.InvokeAsync(parsedGraph);
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new ParseDiagnostic
            {
                Level = DiagnosticLevel.Error,
                Message = $"Parse failed: {ex.Message}",
                Line = 0,
                Column = 0
            });
        }

        StateHasChanged();
    }

    private async Task ValidateIntegration()
    {
        diagnostics.Clear();
        
        // Test plugin availability
        var plugin = PluginManager.GetPlugin(selectedLanguage);
        if (plugin == null)
        {
            diagnostics.Add(new ParseDiagnostic
            {
                Level = DiagnosticLevel.Warning,
                Message = $"Plugin for {selectedLanguage} not found",
                Line = 0,
                Column = 0
            });
        }

        // Test StepParser integration
        await TestIntegration();
        
        diagnostics.Add(new ParseDiagnostic
        {
            Level = isIntegrationActive ? DiagnosticLevel.Info : DiagnosticLevel.Error,
            Message = $"StepParser integration: {(isIntegrationActive ? "Active" : "Failed")}",
            Line = 0,
            Column = 0
        });

        activeResultTab = "diagnostics";
        StateHasChanged();
    }

    private async Task OnGraphChanged(CognitiveGraphNode updatedGraph)
    {
        parsedGraph = updatedGraph;
        await OnParseCompleted.InvokeAsync(updatedGraph);
    }

    private int CountNodes(CognitiveGraphNode node)
    {
        if (node == null) return 0;
        
        int count = 1;
        if (node is NonTerminalNode ntNode)
        {
            count += ntNode.Children.Sum(child => CountNodes(child));
        }
        return count;
    }

    private string GetDiagnosticClass(DiagnosticLevel level) => level switch
    {
        DiagnosticLevel.Error => "diagnostic-error",
        DiagnosticLevel.Warning => "diagnostic-warning",
        DiagnosticLevel.Info => "diagnostic-info",
        _ => "diagnostic-info"
    };

    private string GetDiagnosticIcon(DiagnosticLevel level) => level switch
    {
        DiagnosticLevel.Error => "❌",
        DiagnosticLevel.Warning => "⚠️",
        DiagnosticLevel.Info => "ℹ️",
        _ => "ℹ️"
    };

    public class ParseDiagnostic
    {
        public DiagnosticLevel Level { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class ParseMetrics
    {
        public double ParseTime { get; set; }
        public int TokenCount { get; set; }
        public int NodeCount { get; set; }
        public long MemoryUsage { get; set; }
    }

    public enum DiagnosticLevel
    {
        Info,
        Warning,
        Error
    }
}
```

### 3. Plugin Management Component

#### PluginManagerPanel.razor
```razor
@using Minotaur.Plugins
@inject LanguagePluginManager PluginManager

<div class="plugin-manager-panel">
    <div class="plugin-header">
        <h4>Language Plugin Manager</h4>
        <div class="plugin-actions">
            <button class="btn btn-primary" @onclick="RefreshPlugins">Refresh</button>
            <button class="btn btn-success" @onclick="TestAllPlugins">Test All</button>
        </div>
    </div>
    
    <div class="plugin-stats">
        <div class="stat-item">
            <label>Total Plugins:</label>
            <span class="badge badge-primary">@pluginList.Count</span>
        </div>
        <div class="stat-item">
            <label>Active:</label>
            <span class="badge badge-success">@pluginList.Count(p => p.IsActive)</span>
        </div>
        <div class="stat-item">
            <label>With Symbolic Analysis:</label>
            <span class="badge badge-info">@pluginList.Count(p => p.HasSymbolicAnalysis)</span>
        </div>
    </div>
    
    <div class="plugin-list">
        @foreach (var plugin in pluginList)
        {
            <div class="plugin-card">
                <div class="plugin-header">
                    <div class="plugin-info">
                        <h5>@plugin.DisplayName</h5>
                        <span class="plugin-id">@plugin.LanguageId</span>
                        <span class="plugin-status @(plugin.IsActive ? "status-active" : "status-inactive")">
                            @(plugin.IsActive ? "Active" : "Inactive")
                        </span>
                    </div>
                    
                    <div class="plugin-actions">
                        <button class="btn btn-sm btn-primary" @onclick="() => TestPlugin(plugin)">Test</button>
                        <button class="btn btn-sm btn-info" @onclick="() => ShowPluginDetails(plugin)">Details</button>
                    </div>
                </div>
                
                <div class="plugin-content">
                    <div class="plugin-extensions">
                        <label>Supported Extensions:</label>
                        @foreach (var ext in plugin.SupportedExtensions)
                        {
                            <span class="badge badge-secondary">@ext</span>
                        }
                    </div>
                    
                    @if (plugin.HasSymbolicAnalysis)
                    {
                        <div class="plugin-symbolic">
                            <span class="badge badge-warning">🔍 Symbolic Analysis</span>
                            <span class="plugin-confidence">Confidence: @plugin.AverageConfidence.ToString("P0")</span>
                        </div>
                    }
                    
                    @if (selectedPlugin?.LanguageId == plugin.LanguageId)
                    {
                        <div class="plugin-details">
                            <h6>Plugin Capabilities:</h6>
                            <ul>
                                <li>✅ Code Unparsing</li>
                                <li>✅ Compiler Backend Generation</li>
                                @if (plugin.HasSymbolicAnalysis)
                                {
                                    <li>✅ Symbolic Analysis</li>
                                }
                                <li>✅ Format Validation</li>
                            </ul>
                            
                            @if (plugin.TestResults.Any())
                            {
                                <h6>Recent Test Results:</h6>
                                <div class="test-results">
                                    @foreach (var result in plugin.TestResults.Take(3))
                                    {
                                        <div class="test-result @(result.Success ? "test-success" : "test-failure")">
                                            <span class="test-icon">@(result.Success ? "✅" : "❌")</span>
                                            <span class="test-message">@result.Message</span>
                                            <span class="test-time">@result.Timestamp.ToString("HH:mm:ss")</span>
                                        </div>
                                    }
                                </div>
                            }
                        </div>
                    }
                </div>
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public EventCallback<string> OnLanguageSelected { get; set; }

    private List<PluginInfo> pluginList = new();
    private PluginInfo selectedPlugin;

    protected override async Task OnInitializedAsync()
    {
        await RefreshPlugins();
    }

    private async Task RefreshPlugins()
    {
        pluginList.Clear();
        
        foreach (var kvp in PluginManager.RegisteredPlugins)
        {
            var plugin = kvp.Value;
            var pluginInfo = new PluginInfo
            {
                LanguageId = plugin.LanguageId,
                DisplayName = plugin.DisplayName,
                SupportedExtensions = plugin.SupportedExtensions,
                IsActive = true, // Test if plugin is working
                HasSymbolicAnalysis = plugin is ISymbolicAnalysisPlugin
            };

            // Test the plugin
            try
            {
                var testGraph = new NonTerminalNode("test", 0);
                testGraph.AddChild(new TerminalNode("hello", "string"));
                
                var result = await plugin.UnparseAsync(testGraph);
                pluginInfo.IsActive = !string.IsNullOrEmpty(result);
                
                pluginInfo.TestResults.Add(new TestResult
                {
                    Success = true,
                    Message = "Unparsing test passed",
                    Timestamp = DateTime.Now
                });

                if (plugin is ISymbolicAnalysisPlugin symbolicPlugin)
                {
                    var patterns = symbolicPlugin.GetErrorPatterns();
                    pluginInfo.AverageConfidence = patterns.Average(p => 
                        symbolicPlugin.GetErrorConfidence(SymbolicErrorType.NullReference));
                }
            }
            catch (Exception ex)
            {
                pluginInfo.IsActive = false;
                pluginInfo.TestResults.Add(new TestResult
                {
                    Success = false,
                    Message = $"Test failed: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }

            pluginList.Add(pluginInfo);
        }

        StateHasChanged();
    }

    private async Task TestPlugin(PluginInfo pluginInfo)
    {
        var plugin = PluginManager.GetPlugin(pluginInfo.LanguageId);
        if (plugin == null) return;

        try
        {
            // Test unparsing
            var testGraph = new NonTerminalNode("test_method", 0);
            testGraph.AddChild(new IdentifierNode("TestMethod"));
            testGraph.AddChild(new NonTerminalNode("parameters", 0));
            
            var result = await plugin.UnparseAsync(testGraph);
            
            pluginInfo.TestResults.Insert(0, new TestResult
            {
                Success = !string.IsNullOrEmpty(result),
                Message = $"Unparse test: Generated {result?.Length ?? 0} characters",
                Timestamp = DateTime.Now
            });

            // Test backend generation
            var backendRules = await plugin.GenerateCompilerBackendRulesAsync();
            
            pluginInfo.TestResults.Insert(0, new TestResult
            {
                Success = backendRules.GenerationRules.Count > 0,
                Message = $"Backend test: {backendRules.GenerationRules.Count} rules generated",
                Timestamp = DateTime.Now
            });

            pluginInfo.IsActive = pluginInfo.TestResults.Take(2).All(r => r.Success);
        }
        catch (Exception ex)
        {
            pluginInfo.TestResults.Insert(0, new TestResult
            {
                Success = false,
                Message = $"Test failed: {ex.Message}",
                Timestamp = DateTime.Now
            });
            pluginInfo.IsActive = false;
        }

        StateHasChanged();
    }

    private async Task TestAllPlugins()
    {
        foreach (var plugin in pluginList)
        {
            await TestPlugin(plugin);
        }
    }

    private void ShowPluginDetails(PluginInfo plugin)
    {
        selectedPlugin = selectedPlugin?.LanguageId == plugin.LanguageId ? null : plugin;
        StateHasChanged();
    }

    public class PluginInfo
    {
        public string LanguageId { get; set; }
        public string DisplayName { get; set; }
        public string[] SupportedExtensions { get; set; }
        public bool IsActive { get; set; }
        public bool HasSymbolicAnalysis { get; set; }
        public double AverageConfidence { get; set; }
        public List<TestResult> TestResults { get; set; } = new();
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

### 4. Symbolic Analysis Component

#### SymbolicAnalysisPanel.razor
```razor
@using Minotaur.Analysis.Symbolic
@using Minotaur.Plugins
@inject SymbolicAnalysisEngine AnalysisEngine

<div class="symbolic-analysis-panel">
    <div class="analysis-header">
        <h4>Symbolic Analysis Engine</h4>
        <div class="analysis-controls">
            <button class="btn btn-primary" @onclick="RunAnalysis" disabled="@isAnalyzing">
                @if (isAnalyzing)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Analyze Code
            </button>
            <button class="btn btn-secondary" @onclick="ClearResults">Clear Results</button>
        </div>
    </div>
    
    <div class="analysis-config">
        <div class="config-row">
            <div class="form-group">
                <label>Analysis Depth:</label>
                <select @bind="analysisDepth" class="form-control">
                    <option value="shallow">Shallow (Fast)</option>
                    <option value="medium">Medium (Balanced)</option>
                    <option value="deep">Deep (Thorough)</option>
                </select>
            </div>
            
            <div class="form-group">
                <label>Language:</label>
                <select @bind="selectedLanguage" class="form-control">
                    <option value="csharp">C#</option>
                    <option value="javascript">JavaScript</option>
                    <option value="python">Python</option>
                </select>
            </div>
            
            <div class="form-group">
                <label>Max Execution Paths:</label>
                <input type="number" @bind="maxExecutionPaths" class="form-control" min="1" max="1000" />
            </div>
        </div>
    </div>
    
    <div class="analysis-input">
        <h5>Source Code</h5>
        <textarea @bind="sourceCode" class="form-control code-input" rows="12" 
                  placeholder="Enter source code to analyze for potential errors..."></textarea>
    </div>
    
    @if (analysisResult != null)
    {
        <div class="analysis-results">
            <div class="results-summary">
                <div class="summary-stats">
                    <div class="stat-card error-card">
                        <h6>Errors Found</h6>
                        <span class="stat-number">@analysisResult.Errors.Count</span>
                    </div>
                    <div class="stat-card path-card">
                        <h6>Execution Paths</h6>
                        <span class="stat-number">@analysisResult.ExecutionPaths.Count</span>
                    </div>
                    <div class="stat-card constraint-card">
                        <h6>Constraints</h6>
                        <span class="stat-number">@analysisResult.Constraints.Count</span>
                    </div>
                    <div class="stat-card time-card">
                        <h6>Analysis Time</h6>
                        <span class="stat-number">@analysisResult.AnalysisTime.TotalMilliseconds.ToString("F0")ms</span>
                    </div>
                </div>
            </div>
            
            <div class="results-tabs">
                <button class="tab @(activeTab == "errors" ? "active" : "")" 
                        @onclick="() => activeTab = \"errors\"">
                    Errors (@analysisResult.Errors.Count)
                </button>
                <button class="tab @(activeTab == "paths" ? "active" : "")" 
                        @onclick="() => activeTab = \"paths\"">
                    Execution Paths (@analysisResult.ExecutionPaths.Count)
                </button>
                <button class="tab @(activeTab == "constraints" ? "active" : "")" 
                        @onclick="() => activeTab = \"constraints\"">
                    Constraints (@analysisResult.Constraints.Count)
                </button>
            </div>
            
            <div class="results-content">
                @if (activeTab == "errors")
                {
                    <div class="errors-list">
                        @foreach (var error in analysisResult.Errors.OrderByDescending(e => e.Confidence))
                        {
                            <div class="error-item @GetErrorSeverityClass(error.Type)">
                                <div class="error-header">
                                    <span class="error-icon">@GetErrorIcon(error.Type)</span>
                                    <span class="error-type">@error.Type</span>
                                    <span class="error-confidence">@(error.Confidence.ToString("P0"))</span>
                                </div>
                                <div class="error-details">
                                    <p class="error-message">@error.Message</p>
                                    @if (error.Location != null)
                                    {
                                        <div class="error-location">
                                            <span>Line @error.Location.Line, Column @error.Location.Column</span>
                                            @if (!string.IsNullOrEmpty(error.Location.Function))
                                            {
                                                <span>in @error.Location.Function</span>
                                            }
                                        </div>
                                    }
                                    @if (error.Suggestions.Any())
                                    {
                                        <div class="error-suggestions">
                                            <h6>Suggestions:</h6>
                                            <ul>
                                                @foreach (var suggestion in error.Suggestions)
                                                {
                                                    <li>@suggestion</li>
                                                }
                                            </ul>
                                        </div>
                                    }
                                </div>
                            </div>
                        }
                    </div>
                }
                else if (activeTab == "paths")
                {
                    <div class="paths-list">
                        @foreach (var path in analysisResult.ExecutionPaths.Take(20))
                        {
                            <div class="path-item">
                                <div class="path-header">
                                    <span class="path-id">Path @path.Id</span>
                                    <span class="path-feasible @(path.IsFeasible ? "feasible" : "infeasible")">
                                        @(path.IsFeasible ? "Feasible" : "Infeasible")
                                    </span>
                                    <span class="path-conditions">@path.Conditions.Count conditions</span>
                                </div>
                                <div class="path-conditions-list">
                                    @foreach (var condition in path.Conditions.Take(5))
                                    {
                                        <div class="condition-item">
                                            <span class="condition-variable">@condition.Variable</span>
                                            <span class="condition-operator">@condition.Operator</span>
                                            <span class="condition-value">@condition.Value</span>
                                        </div>
                                    }
                                    @if (path.Conditions.Count > 5)
                                    {
                                        <div class="condition-more">... and @(path.Conditions.Count - 5) more</div>
                                    }
                                </div>
                            </div>
                        }
                    </div>
                }
                else if (activeTab == "constraints")
                {
                    <div class="constraints-list">
                        @foreach (var constraint in analysisResult.Constraints)
                        {
                            <div class="constraint-item">
                                <div class="constraint-expression">@constraint.Expression</div>
                                <div class="constraint-type">Type: @constraint.Type</div>
                                @if (constraint.IsSatisfiable.HasValue)
                                {
                                    <div class="constraint-satisfiable @(constraint.IsSatisfiable.Value ? "satisfiable" : "unsatisfiable")">
                                        @(constraint.IsSatisfiable.Value ? "Satisfiable" : "Unsatisfiable")
                                    </div>
                                }
                            </div>
                        }
                    </div>
                }
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public EventCallback<SymbolicAnalysisResult> OnAnalysisCompleted { get; set; }

    private string sourceCode = @"public class Example 
{
    public void TestMethod(string input)
    {
        if (input != null)
        {
            Console.WriteLine(input.Length);
        }
        
        // Potential null reference here
        Console.WriteLine(input.ToString());
    }
}";
    
    private string analysisDepth = "medium";
    private string selectedLanguage = "csharp";
    private int maxExecutionPaths = 100;
    private bool isAnalyzing = false;
    private string activeTab = "errors";
    private SymbolicAnalysisResult analysisResult;

    private async Task RunAnalysis()
    {
        if (string.IsNullOrWhiteSpace(sourceCode)) return;

        isAnalyzing = true;
        StateHasChanged();

        try
        {
            analysisResult = AnalysisEngine.AnalyzeCode(sourceCode, selectedLanguage);
            await OnAnalysisCompleted.InvokeAsync(analysisResult);
        }
        catch (Exception ex)
        {
            // Handle analysis errors
            analysisResult = new SymbolicAnalysisResult
            {
                Errors = new List<SymbolicError>
                {
                    new SymbolicError
                    {
                        Type = SymbolicErrorType.AnalysisError,
                        Message = $"Analysis failed: {ex.Message}",
                        Confidence = 1.0,
                        Location = new ErrorLocation { Line = 0, Column = 0 }
                    }
                },
                ExecutionPaths = new List<ExecutionPath>(),
                Constraints = new List<SymbolicConstraint>(),
                AnalysisTime = TimeSpan.Zero
            };
        }
        finally
        {
            isAnalyzing = false;
            StateHasChanged();
        }
    }

    private void ClearResults()
    {
        analysisResult = null;
        StateHasChanged();
    }

    private string GetErrorSeverityClass(SymbolicErrorType errorType) => errorType switch
    {
        SymbolicErrorType.NullReference => "error-critical",
        SymbolicErrorType.ArrayBounds => "error-critical",
        SymbolicErrorType.DivisionByZero => "error-high",
        SymbolicErrorType.UnreachableCode => "error-medium",
        SymbolicErrorType.ResourceLeak => "error-medium",
        _ => "error-low"
    };

    private string GetErrorIcon(SymbolicErrorType errorType) => errorType switch
    {
        SymbolicErrorType.NullReference => "🚫",
        SymbolicErrorType.ArrayBounds => "📏",
        SymbolicErrorType.DivisionByZero => "➗",
        SymbolicErrorType.UnreachableCode => "🚪",
        SymbolicErrorType.ResourceLeak => "💧",
        _ => "⚠️"
    };
}
```

## Required Adaptations to Old Code Components

### 1. GrammarGraphViewer Adaptation

**Original Issue**: The old GrammarGraphViewer used simple grammar rule parsing
**New Requirement**: Must work with CognitiveGraphNode structure

#### Key Changes Required:
```typescript
// OLD: Simple grammar rule parsing
function parseGrammarToGraph(grammarCode: string): { nodes: Node[], edges: Edge[] }

// NEW: CognitiveGraphNode integration
function cognitiveGraphToVisualization(rootNode: CognitiveGraphNode): { nodes: Node[], edges: Edge[] }
```

### 2. ParseTreeViewer Adaptation

**Original Issue**: Used mock parse tree data
**New Requirement**: Display actual CognitiveGraphNode tree from StepParser

#### Key Changes Required:
```typescript
// OLD: Mock parse tree structure
interface ParseTreeNode {
  name: string;
  value?: string;
  children?: ParseTreeNode[];
}

// NEW: CognitiveGraphNode integration
interface CognitiveGraphVisualization {
  nodeId: string;
  nodeType: string;
  displayText: string;
  metadata: Record<string, any>;
  children: CognitiveGraphVisualization[];
}
```

### 3. EditorPanel Enhancement

**Original Issue**: Basic text editing only
**New Requirement**: Integration with GraphEditor for cognitive graph manipulation

#### Key Changes Required:
- Add cognitive graph editing mode
- Integration with StepParser for real-time parsing
- Support for multi-language plugin system
- Graph editor controls and visualization

## Implementation Priority

### Phase 1: Core Cognitive Graph UI (Weeks 1-4)
1. ✅ Create `CognitiveGraphEditor` component
2. ✅ Implement basic graph visualization
3. ✅ Add node manipulation capabilities
4. ✅ Integration with GraphEditor backend

### Phase 2: StepParser Integration (Weeks 5-8)
1. ✅ Create `StepParserIntegrationPanel` component
2. ✅ Implement parser configuration UI
3. ✅ Add real-time parsing and validation
4. ✅ Integration with cognitive graph editor

### Phase 3: Plugin System UI (Weeks 9-12)
1. ✅ Create `PluginManagerPanel` component
2. ✅ Implement plugin testing and management
3. ✅ Add language selection and configuration
4. ✅ Integration with unparsing capabilities

### Phase 4: Symbolic Analysis UI (Weeks 13-16)
1. ✅ Create `SymbolicAnalysisPanel` component
2. ✅ Implement error visualization
3. ✅ Add constraint and path analysis
4. ✅ Integration with symbolic analysis engine

This specification provides a comprehensive plan for implementing UI support for all new Minotaur functionality while adapting the old_code components to work with the new cognitive graph system.