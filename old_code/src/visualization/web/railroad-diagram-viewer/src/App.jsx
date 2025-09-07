import { useState, useEffect, useMemo } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Button } from '@/components/ui/button.jsx';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card.jsx';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Separator } from '@/components/ui/separator.jsx';
import {
  FileText,
  Zap,
  Eye,
  Settings,
  Download,
  Upload,
  Play,
  Pause,
  RotateCcw,
  ZoomIn,
  ZoomOut,
  Maximize,
  Minimize,
  Layers,
  GitBranch,
  Target,
  Code,
  Palette,
  Grid,
  Search,
  Filter,
  Share2,
  BookOpen,
  HelpCircle,
} from 'lucide-react';
import './App.css';

// Import our custom components
import DiagramViewer from './components/diagram/DiagramViewer';
import ControlPanel from './components/controls/ControlPanel';
import PropertiesPanel from './components/panels/PropertiesPanel';
import ThemeSelector from './components/controls/ThemeSelector';
import GrammarUploader from './components/controls/GrammarUploader';
import ExportDialog from './components/modals/ExportDialog';
import SettingsDialog from './components/modals/SettingsDialog';
import HelpDialog from './components/modals/HelpDialog';

function App() {
  // Application state
  const [currentGrammar, setCurrentGrammar] = useState(null);
  const [currentDiagram, setCurrentDiagram] = useState(null);
  const [selectedTheme, setSelectedTheme] = useState('default');
  const [viewMode, setViewMode] = useState('diagram'); // diagram, inheritance, context
  const [isPlaying, setIsPlaying] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(100);
  const [showGrid, setShowGrid] = useState(false);
  const [selectedElement, setSelectedElement] = useState(null);
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [showSettingsDialog, setShowSettingsDialog] = useState(false);
  const [showHelpDialog, setShowHelpDialog] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  // Sample grammar data for demonstration
  const sampleGrammar = useMemo(() => ({
    name: 'Sample Grammar',
    format: 'GrammarForge',
    content: `
      grammar SampleGrammar inherits BaseGrammar {
        start: expression;
        
        expression: 
          | term (('+' | '-') term)*
          | '(' expression ')'
          ;
          
        term:
          | factor (('*' | '/') factor)*
          ;
          
        factor:
          | NUMBER
          | IDENTIFIER
          | '(' expression ')'
          ;
      }
    `,
    metadata: {
      rules: 4,
      terminals: 8,
      inheritance: true,
      contextSensitive: false,
    },
  }), []);

  // Load sample grammar on startup
  useEffect(() => {
    setCurrentGrammar(sampleGrammar);
    generateDiagram(sampleGrammar);
  }, [sampleGrammar]);

  // Generate diagram from grammar
  const generateDiagram = async (grammar) => {
    setIsLoading(true);
    try {
      // Simulate diagram generation
      await new Promise(resolve => setTimeout(resolve, 1000));

      const mockDiagram = {
        id: `diagram_${Date.now()}`,
        name: grammar.name,
        elements: [
          { id: 'start', type: 'start', name: 'START', x: 50, y: 100 },
          { id: 'expression', type: 'non-terminal', name: 'expression', x: 200, y: 100 },
          { id: 'term', type: 'non-terminal', name: 'term', x: 400, y: 100 },
          { id: 'factor', type: 'non-terminal', name: 'factor', x: 600, y: 100 },
          { id: 'end', type: 'end', name: 'END', x: 800, y: 100 },
        ],
        connections: [
          { from: 'start', to: 'expression' },
          { from: 'expression', to: 'term' },
          { from: 'term', to: 'factor' },
          { from: 'factor', to: 'end' },
        ],
        bounds: { width: 900, height: 200 },
      };

      setCurrentDiagram(mockDiagram);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to generate diagram:', error);
    } finally {
      setIsLoading(false);
    }
  };

  // Handle grammar upload
  const handleGrammarUpload = (grammar) => {
    setCurrentGrammar(grammar);
    generateDiagram(grammar);
  };

  // Handle theme change
  const handleThemeChange = (theme) => {
    setSelectedTheme(theme);
  };

  // Handle zoom controls
  const handleZoomIn = () => setZoomLevel(prev => Math.min(prev + 25, 400));
  const handleZoomOut = () => setZoomLevel(prev => Math.max(prev - 25, 25));
  const handleZoomReset = () => setZoomLevel(100);

  // Handle view mode change
  const handleViewModeChange = (mode) => {
    setViewMode(mode);
  };

  // Handle element selection
  const handleElementSelect = (element) => {
    setSelectedElement(element);
  };

  // Handle animation controls
  const handlePlayPause = () => {
    setIsPlaying(!isPlaying);
  };

  return (
    <Router>
      <div className="min-h-screen bg-background text-foreground">
        {/* Header */}
        <header className="border-b border-border bg-card">
          <div className="flex items-center justify-between px-6 py-4">
            <div className="flex items-center space-x-4">
              <motion.div
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                className="flex items-center space-x-2"
              >
                <GitBranch className="h-8 w-8 text-primary" />
                <div>
                  <h1 className="text-2xl font-bold">GrammarForge</h1>
                  <p className="text-sm text-muted-foreground">Railroad Diagram Viewer</p>
                </div>
              </motion.div>

              {currentGrammar && (
                <motion.div
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  className="flex items-center space-x-2"
                >
                  <Separator orientation="vertical" className="h-8" />
                  <div>
                    <p className="font-medium">{currentGrammar.name}</p>
                    <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                      <Badge variant="secondary">{currentGrammar.format}</Badge>
                      <span>{currentGrammar.metadata.rules} rules</span>
                      {currentGrammar.metadata.inheritance && (
                        <Badge variant="outline">Inheritance</Badge>
                      )}
                    </div>
                  </div>
                </motion.div>
              )}
            </div>

            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowHelpDialog(true)}
              >
                <HelpCircle className="h-4 w-4 mr-2" />
                Help
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowSettingsDialog(true)}
              >
                <Settings className="h-4 w-4 mr-2" />
                Settings
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowExportDialog(true)}
                disabled={!currentDiagram}
              >
                <Download className="h-4 w-4 mr-2" />
                Export
              </Button>
            </div>
          </div>
        </header>

        {/* Main Content */}
        <div className="flex h-[calc(100vh-80px)]">
          {/* Left Sidebar - Controls */}
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            className="w-80 border-r border-border bg-card overflow-y-auto"
          >
            <div className="p-4 space-y-4">
              {/* Grammar Upload */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center">
                    <Upload className="h-5 w-5 mr-2" />
                    Grammar Input
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <GrammarUploader onGrammarUpload={handleGrammarUpload} />
                </CardContent>
              </Card>

              {/* View Mode Selection */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center">
                    <Eye className="h-5 w-5 mr-2" />
                    View Mode
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <Tabs value={viewMode} onValueChange={handleViewModeChange}>
                    <TabsList className="grid w-full grid-cols-3">
                      <TabsTrigger value="diagram">
                        <FileText className="h-4 w-4 mr-1" />
                        Diagram
                      </TabsTrigger>
                      <TabsTrigger value="inheritance">
                        <GitBranch className="h-4 w-4 mr-1" />
                        Inheritance
                      </TabsTrigger>
                      <TabsTrigger value="context">
                        <Target className="h-4 w-4 mr-1" />
                        Context
                      </TabsTrigger>
                    </TabsList>
                  </Tabs>
                </CardContent>
              </Card>

              {/* Theme Selection */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center">
                    <Palette className="h-5 w-5 mr-2" />
                    Theme
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <ThemeSelector
                    selectedTheme={selectedTheme}
                    onThemeChange={handleThemeChange}
                  />
                </CardContent>
              </Card>

              {/* Controls */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center">
                    <Settings className="h-5 w-5 mr-2" />
                    Controls
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <ControlPanel
                    isPlaying={isPlaying}
                    zoomLevel={zoomLevel}
                    showGrid={showGrid}
                    onPlayPause={handlePlayPause}
                    onZoomIn={handleZoomIn}
                    onZoomOut={handleZoomOut}
                    onZoomReset={handleZoomReset}
                    onToggleGrid={() => setShowGrid(!showGrid)}
                  />
                </CardContent>
              </Card>
            </div>
          </motion.div>

          {/* Center - Diagram Viewer */}
          <div className="flex-1 relative">
            <AnimatePresence mode="wait">
              {isLoading ? (
                <motion.div
                  key="loading"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="absolute inset-0 flex items-center justify-center bg-background/80"
                >
                  <div className="text-center">
                    <motion.div
                      animate={{ rotate: 360 }}
                      transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                      className="mx-auto mb-4"
                    >
                      <Zap className="h-12 w-12 text-primary" />
                    </motion.div>
                    <p className="text-lg font-medium">Generating diagram...</p>
                    <p className="text-sm text-muted-foreground">Analyzing grammar structure</p>
                  </div>
                </motion.div>
              ) : currentDiagram ? (
                <motion.div
                  key="diagram"
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.95 }}
                  className="h-full"
                >
                  <DiagramViewer
                    diagram={currentDiagram}
                    theme={selectedTheme}
                    viewMode={viewMode}
                    zoomLevel={zoomLevel}
                    showGrid={showGrid}
                    isPlaying={isPlaying}
                    selectedElement={selectedElement}
                    onElementSelect={handleElementSelect}
                  />
                </motion.div>
              ) : (
                <motion.div
                  key="empty"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="absolute inset-0 flex items-center justify-center"
                >
                  <div className="text-center max-w-md">
                    <FileText className="h-16 w-16 text-muted-foreground mx-auto mb-4" />
                    <h3 className="text-xl font-semibold mb-2">No Grammar Loaded</h3>
                    <p className="text-muted-foreground mb-4">
                      Upload a grammar file or paste grammar content to generate a railroad diagram.
                    </p>
                    <Button onClick={() => document.querySelector('input[type="file"]')?.click()}>
                      <Upload className="h-4 w-4 mr-2" />
                      Upload Grammar
                    </Button>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Right Sidebar - Properties */}
          <motion.div
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            className="w-80 border-l border-border bg-card overflow-y-auto"
          >
            <div className="p-4">
              <PropertiesPanel
                selectedElement={selectedElement}
                diagram={currentDiagram}
                grammar={currentGrammar}
              />
            </div>
          </motion.div>
        </div>

        {/* Modals */}
        <ExportDialog
          open={showExportDialog}
          onOpenChange={setShowExportDialog}
          diagram={currentDiagram}
        />

        <SettingsDialog
          open={showSettingsDialog}
          onOpenChange={setShowSettingsDialog}
        />

        <HelpDialog
          open={showHelpDialog}
          onOpenChange={setShowHelpDialog}
        />
      </div>
    </Router>
  );
}

export default App;

