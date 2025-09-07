import { useState } from 'react';
import { motion } from 'framer-motion';
import {
  HelpCircle,
  BookOpen,
  Keyboard,
  MousePointer,
  Zap,
  FileText,
  GitBranch,
  Target,
  Download,
  Settings,
  X,
  ChevronRight,
  ExternalLink,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Separator } from '@/components/ui/separator.jsx';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs.jsx';

// Mock Dialog components
const Dialog = ({ open, onOpenChange, children }) => {
  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div
        className="fixed inset-0 bg-black/50"
        onClick={() => onOpenChange(false)}
      />
      <div className="relative bg-background border border-border rounded-lg shadow-lg max-w-4xl w-full mx-4 max-h-[80vh] overflow-y-auto">
        {children}
      </div>
    </div>
  );
};

const DialogContent = ({ children }) => <div className="p-6">{children}</div>;
const DialogHeader = ({ children }) => <div className="mb-4">{children}</div>;
const DialogTitle = ({ children }) => <h2 className="text-lg font-semibold">{children}</h2>;
const DialogDescription = ({ children }) => <p className="text-sm text-muted-foreground">{children}</p>;

const HelpDialog = ({ open, onOpenChange }) => {
  const [activeSection, setActiveSection] = useState('overview');

  const keyboardShortcuts = [
    { key: 'Ctrl + O', description: 'Open grammar file' },
    { key: 'Ctrl + S', description: 'Save current diagram' },
    { key: 'Ctrl + E', description: 'Export diagram' },
    { key: 'Ctrl + +', description: 'Zoom in' },
    { key: 'Ctrl + -', description: 'Zoom out' },
    { key: 'Ctrl + 0', description: 'Reset zoom' },
    { key: 'Space', description: 'Play/pause animation' },
    { key: 'G', description: 'Toggle grid' },
    { key: 'H', description: 'Show/hide help' },
    { key: 'Escape', description: 'Clear selection' },
  ];

  const mouseControls = [
    { action: 'Click', description: 'Select diagram element' },
    { action: 'Double-click', description: 'Zoom to fit element' },
    { action: 'Ctrl + Drag', description: 'Pan diagram view' },
    { action: 'Scroll', description: 'Zoom in/out' },
    { action: 'Hover', description: 'Show element tooltip' },
    { action: 'Right-click', description: 'Context menu (future)' },
  ];

  const grammarFormats = [
    {
      name: 'GrammarForge',
      extension: '.gf',
      description: 'Native format with inheritance and context-sensitive features',
      features: ['Inheritance', 'Context-sensitive parsing', 'Embedded grammars'],
    },
    {
      name: 'ANTLR v4',
      extension: '.g4',
      description: 'Popular parser generator with lexer and parser rules',
      features: ['Lexer rules', 'Parser rules', 'Actions', 'Predicates'],
    },
    {
      name: 'Bison/Yacc',
      extension: '.y',
      description: 'Traditional parser generator with C actions',
      features: ['Grammar rules', 'C actions', 'Precedence', 'Associativity'],
    },
    {
      name: 'Flex/Lex',
      extension: '.l',
      description: 'Lexical analyzer generator',
      features: ['Regular expressions', 'C actions', 'States'],
    },
  ];

  const features = [
    {
      icon: FileText,
      title: 'Grammar Visualization',
      description: 'Convert grammar files into beautiful railroad diagrams',
      details: [
        'Support for multiple grammar formats',
        'Automatic layout and positioning',
        'Professional-quality output',
      ],
    },
    {
      icon: GitBranch,
      title: 'Inheritance Visualization',
      description: 'Visualize grammar inheritance relationships',
      details: [
        'Inheritance tree diagrams',
        'Override indicators',
        'Base grammar highlighting',
      ],
    },
    {
      icon: Target,
      title: 'Context-Sensitive Features',
      description: 'Represent context-sensitive parsing elements',
      details: [
        'Scope boundaries',
        'Symbol tables',
        'Context transitions',
      ],
    },
    {
      icon: Zap,
      title: 'Interactive Experience',
      description: 'Explore diagrams with smooth animations',
      details: [
        'Real-time interaction',
        'Smooth animations',
        'Responsive design',
      ],
    },
  ];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center">
            <HelpCircle className="h-5 w-5 mr-2" />
            Help & Documentation
          </DialogTitle>
          <DialogDescription>
            Learn how to use the GrammarForge Railroad Diagram Viewer
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeSection} onValueChange={setActiveSection} className="space-y-4">
          <TabsList className="grid w-full grid-cols-5">
            <TabsTrigger value="overview">
              <BookOpen className="h-4 w-4 mr-2" />
              Overview
            </TabsTrigger>
            <TabsTrigger value="controls">
              <MousePointer className="h-4 w-4 mr-2" />
              Controls
            </TabsTrigger>
            <TabsTrigger value="grammars">
              <FileText className="h-4 w-4 mr-2" />
              Grammars
            </TabsTrigger>
            <TabsTrigger value="features">
              <Zap className="h-4 w-4 mr-2" />
              Features
            </TabsTrigger>
            <TabsTrigger value="shortcuts">
              <Keyboard className="h-4 w-4 mr-2" />
              Shortcuts
            </TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Welcome to GrammarForge</CardTitle>
                <CardDescription>
                  A powerful tool for visualizing grammar structures as railroad diagrams
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-3">
                  <h4 className="font-medium">Getting Started</h4>
                  <ol className="space-y-2 text-sm">
                    <li className="flex items-start space-x-2">
                      <Badge variant="outline" className="mt-0.5">1</Badge>
                      <span>Upload a grammar file or paste grammar content in the left panel</span>
                    </li>
                    <li className="flex items-start space-x-2">
                      <Badge variant="outline" className="mt-0.5">2</Badge>
                      <span>Select the appropriate grammar format (GrammarForge, ANTLR v4, etc.)</span>
                    </li>
                    <li className="flex items-start space-x-2">
                      <Badge variant="outline" className="mt-0.5">3</Badge>
                      <span>Click &quot;Generate Diagram&quot; to create your railroad diagram</span>
                    </li>
                    <li className="flex items-start space-x-2">
                      <Badge variant="outline" className="mt-0.5">4</Badge>
                      <span>Explore the diagram using mouse controls and view options</span>
                    </li>
                    <li className="flex items-start space-x-2">
                      <Badge variant="outline" className="mt-0.5">5</Badge>
                      <span>Export your diagram in various formats when ready</span>
                    </li>
                  </ol>
                </div>

                <Separator />

                <div className="space-y-3">
                  <h4 className="font-medium">Key Features</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <FileText className="h-4 w-4 text-primary" />
                        <span className="text-sm font-medium">Multi-format Support</span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Support for GrammarForge, ANTLR v4, Bison, and more
                      </p>
                    </div>
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <GitBranch className="h-4 w-4 text-primary" />
                        <span className="text-sm font-medium">Inheritance Visualization</span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Visualize grammar inheritance relationships
                      </p>
                    </div>
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <Target className="h-4 w-4 text-primary" />
                        <span className="text-sm font-medium">Context-Sensitive</span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Support for context-sensitive parsing features
                      </p>
                    </div>
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <Download className="h-4 w-4 text-primary" />
                        <span className="text-sm font-medium">Export Options</span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Export as SVG, PNG, PDF, or interactive HTML
                      </p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="controls" className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base flex items-center">
                    <MousePointer className="h-4 w-4 mr-2" />
                    Mouse Controls
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {mouseControls.map((control, index) => (
                      <div key={index} className="flex justify-between items-center">
                        <Badge variant="outline" className="font-mono text-xs">
                          {control.action}
                        </Badge>
                        <span className="text-sm text-muted-foreground">
                          {control.description}
                        </span>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base flex items-center">
                    <Settings className="h-4 w-4 mr-2" />
                    View Controls
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="space-y-2">
                      <h5 className="text-sm font-medium">View Modes</h5>
                      <div className="space-y-1 text-sm">
                        <div className="flex items-center space-x-2">
                          <FileText className="h-3 w-3" />
                          <span>Diagram - Standard railroad diagram view</span>
                        </div>
                        <div className="flex items-center space-x-2">
                          <GitBranch className="h-3 w-3" />
                          <span>Inheritance - Grammar inheritance visualization</span>
                        </div>
                        <div className="flex items-center space-x-2">
                          <Target className="h-3 w-3" />
                          <span>Context - Context-sensitive parsing view</span>
                        </div>
                      </div>
                    </div>

                    <Separator />

                    <div className="space-y-2">
                      <h5 className="text-sm font-medium">Zoom Controls</h5>
                      <div className="space-y-1 text-sm text-muted-foreground">
                        <div>• Use zoom slider or +/- buttons</div>
                        <div>• Scroll wheel to zoom in/out</div>
                        <div>• Quick zoom buttons: 50%, 100%, 200%</div>
                        <div>• Fit to view button for optimal sizing</div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="grammars" className="space-y-4">
            <div className="space-y-4">
              {grammarFormats.map((format, index) => (
                <Card key={index}>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base">{format.name}</CardTitle>
                      <Badge variant="secondary">{format.extension}</Badge>
                    </div>
                    <CardDescription>{format.description}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      <h5 className="text-sm font-medium">Supported Features</h5>
                      <div className="flex flex-wrap gap-1">
                        {format.features.map((feature, idx) => (
                          <Badge key={idx} variant="outline" className="text-xs">
                            {feature}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Grammar Examples</CardTitle>
                <CardDescription>
                  Click the format buttons in the grammar uploader to load sample grammars
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">
                    Sample grammars are available for:
                  </p>
                  <div className="flex flex-wrap gap-2">
                    <Badge variant="outline">GrammarForge Calculator</Badge>
                    <Badge variant="outline">ANTLR v4 Calculator</Badge>
                    <Badge variant="outline">Bison Calculator</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="features" className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {features.map((feature, index) => {
                const Icon = feature.icon;
                return (
                  <Card key={index}>
                    <CardHeader>
                      <CardTitle className="text-base flex items-center">
                        <Icon className="h-4 w-4 mr-2 text-primary" />
                        {feature.title}
                      </CardTitle>
                      <CardDescription>{feature.description}</CardDescription>
                    </CardHeader>
                    <CardContent>
                      <ul className="space-y-1 text-sm">
                        {feature.details.map((detail, idx) => (
                          <li key={idx} className="flex items-center space-x-2">
                            <ChevronRight className="h-3 w-3 text-muted-foreground" />
                            <span>{detail}</span>
                          </li>
                        ))}
                      </ul>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </TabsContent>

          <TabsContent value="shortcuts" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base flex items-center">
                  <Keyboard className="h-4 w-4 mr-2" />
                  Keyboard Shortcuts
                </CardTitle>
                <CardDescription>
                  Speed up your workflow with these keyboard shortcuts
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {keyboardShortcuts.map((shortcut, index) => (
                    <div key={index} className="flex justify-between items-center">
                      <Badge variant="outline" className="font-mono">
                        {shortcut.key}
                      </Badge>
                      <span className="text-sm text-muted-foreground">
                        {shortcut.description}
                      </span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Tips & Tricks</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2 text-sm">
                  <div className="flex items-start space-x-2">
                    <Badge variant="outline" className="mt-0.5">💡</Badge>
                    <span>Hold Ctrl while dragging to pan around large diagrams</span>
                  </div>
                  <div className="flex items-start space-x-2">
                    <Badge variant="outline" className="mt-0.5">💡</Badge>
                    <span>Use the grid to align elements when taking screenshots</span>
                  </div>
                  <div className="flex items-start space-x-2">
                    <Badge variant="outline" className="mt-0.5">💡</Badge>
                    <span>Export as SVG for the highest quality scalable diagrams</span>
                  </div>
                  <div className="flex items-start space-x-2">
                    <Badge variant="outline" className="mt-0.5">💡</Badge>
                    <span>Use inheritance view to understand grammar relationships</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>

        <Separator />

        {/* Footer */}
        <div className="flex justify-between items-center">
          <div className="flex items-center space-x-4 text-sm text-muted-foreground">
            <span>GrammarForge Railroad Diagram Viewer</span>
            <Badge variant="outline">v1.0.0</Badge>
          </div>

          <div className="flex space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => window.open('https://github.com/DevelApp-ai/GrammarForge', '_blank')}
            >
              <ExternalLink className="h-4 w-4 mr-2" />
              GitHub
            </Button>
            <Button
              variant="ghost"
              onClick={() => onOpenChange(false)}
            >
              <X className="h-4 w-4 mr-2" />
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default HelpDialog;

