import { useState } from 'react';
import { motion } from 'framer-motion';
import {
  Settings,
  Palette,
  Eye,
  Zap,
  Grid,
  MousePointer,
  Keyboard,
  Monitor,
  Save,
  RotateCcw,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Label } from '@/components/ui/label.jsx';
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
      <div className="relative bg-background border border-border rounded-lg shadow-lg max-w-2xl w-full mx-4 max-h-[80vh] overflow-y-auto">
        {children}
      </div>
    </div>
  );
};

const DialogContent = ({ children }) => <div className="p-6">{children}</div>;
const DialogHeader = ({ children }) => <div className="mb-4">{children}</div>;
const DialogTitle = ({ children }) => <h2 className="text-lg font-semibold">{children}</h2>;
const DialogDescription = ({ children }) => <p className="text-sm text-muted-foreground">{children}</p>;

// Mock Switch component
const Switch = ({ checked, onCheckedChange, id }) => (
  <button
    id={id}
    onClick={() => onCheckedChange(!checked)}
    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
      checked ? 'bg-primary' : 'bg-muted'
    }`}
  >
    <span
      className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
        checked ? 'translate-x-6' : 'translate-x-1'
      }`}
    />
  </button>
);

// Mock Slider component
const Slider = ({ value, onValueChange, min, max, step, className }) => (
  <input
    type="range"
    min={min}
    max={max}
    step={step}
    value={value[0]}
    onChange={(e) => onValueChange([parseInt(e.target.value)])}
    className={`w-full ${className}`}
  />
);

const SettingsDialog = ({ open, onOpenChange }) => {
  const [settings, setSettings] = useState({
    // Appearance
    theme: 'default',
    showGrid: false,
    showAnimations: true,
    showTooltips: true,

    // Performance
    enableCaching: true,
    maxCacheSize: 100,
    animationSpeed: 50,
    renderQuality: 'high',

    // Interaction
    enablePanning: true,
    enableZooming: true,
    enableSelection: true,
    doubleClickAction: 'zoom',

    // Export
    defaultExportFormat: 'svg',
    includeMetadata: true,
    highQualityExport: true,

    // Advanced
    debugMode: false,
    showPerformanceMetrics: false,
    enableExperimentalFeatures: false,
  });

  const updateSetting = (key, value) => {
    setSettings(prev => ({ ...prev, [key]: value }));
  };

  const resetSettings = () => {
    setSettings({
      theme: 'default',
      showGrid: false,
      showAnimations: true,
      showTooltips: true,
      enableCaching: true,
      maxCacheSize: 100,
      animationSpeed: 50,
      renderQuality: 'high',
      enablePanning: true,
      enableZooming: true,
      enableSelection: true,
      doubleClickAction: 'zoom',
      defaultExportFormat: 'svg',
      includeMetadata: true,
      highQualityExport: true,
      debugMode: false,
      showPerformanceMetrics: false,
      enableExperimentalFeatures: false,
    });
  };

  const saveSettings = () => {
    // Save settings to localStorage
    localStorage.setItem('grammarforge-settings', JSON.stringify(settings));
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center">
            <Settings className="h-5 w-5 mr-2" />
            Settings
          </DialogTitle>
          <DialogDescription>
            Customize your railroad diagram viewer experience
          </DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="appearance" className="space-y-4">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="appearance">
              <Palette className="h-4 w-4 mr-2" />
              Appearance
            </TabsTrigger>
            <TabsTrigger value="performance">
              <Zap className="h-4 w-4 mr-2" />
              Performance
            </TabsTrigger>
            <TabsTrigger value="interaction">
              <MousePointer className="h-4 w-4 mr-2" />
              Interaction
            </TabsTrigger>
            <TabsTrigger value="advanced">
              <Monitor className="h-4 w-4 mr-2" />
              Advanced
            </TabsTrigger>
          </TabsList>

          <TabsContent value="appearance" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Visual Settings</CardTitle>
                <CardDescription>
                  Customize the appearance of diagrams
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Show Grid</Label>
                    <p className="text-sm text-muted-foreground">
                      Display background grid for alignment
                    </p>
                  </div>
                  <Switch
                    checked={settings.showGrid}
                    onCheckedChange={(checked) => updateSetting('showGrid', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Animations</Label>
                    <p className="text-sm text-muted-foreground">
                      Enable smooth transitions and animations
                    </p>
                  </div>
                  <Switch
                    checked={settings.showAnimations}
                    onCheckedChange={(checked) => updateSetting('showAnimations', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Tooltips</Label>
                    <p className="text-sm text-muted-foreground">
                      Show helpful tooltips on hover
                    </p>
                  </div>
                  <Switch
                    checked={settings.showTooltips}
                    onCheckedChange={(checked) => updateSetting('showTooltips', checked)}
                  />
                </div>

                <Separator />

                <div className="space-y-2">
                  <Label>Animation Speed</Label>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-muted-foreground">Slow</span>
                    <Slider
                      value={[settings.animationSpeed]}
                      onValueChange={(value) => updateSetting('animationSpeed', value[0])}
                      min={10}
                      max={100}
                      step={10}
                      className="flex-1"
                    />
                    <span className="text-sm text-muted-foreground">Fast</span>
                  </div>
                  <div className="text-center">
                    <Badge variant="outline">{settings.animationSpeed}%</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="performance" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Performance Settings</CardTitle>
                <CardDescription>
                  Optimize performance for large diagrams
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Enable Caching</Label>
                    <p className="text-sm text-muted-foreground">
                      Cache rendered diagrams for faster loading
                    </p>
                  </div>
                  <Switch
                    checked={settings.enableCaching}
                    onCheckedChange={(checked) => updateSetting('enableCaching', checked)}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Cache Size Limit</Label>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-muted-foreground">10MB</span>
                    <Slider
                      value={[settings.maxCacheSize]}
                      onValueChange={(value) => updateSetting('maxCacheSize', value[0])}
                      min={10}
                      max={500}
                      step={10}
                      className="flex-1"
                    />
                    <span className="text-sm text-muted-foreground">500MB</span>
                  </div>
                  <div className="text-center">
                    <Badge variant="outline">{settings.maxCacheSize}MB</Badge>
                  </div>
                </div>

                <Separator />

                <div className="space-y-2">
                  <Label>Render Quality</Label>
                  <div className="grid grid-cols-3 gap-2">
                    {['low', 'medium', 'high'].map((quality) => (
                      <Button
                        key={quality}
                        variant={settings.renderQuality === quality ? 'default' : 'outline'}
                        size="sm"
                        onClick={() => updateSetting('renderQuality', quality)}
                        className="capitalize"
                      >
                        {quality}
                      </Button>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="interaction" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Interaction Settings</CardTitle>
                <CardDescription>
                  Configure how you interact with diagrams
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Enable Panning</Label>
                    <p className="text-sm text-muted-foreground">
                      Drag to move around large diagrams
                    </p>
                  </div>
                  <Switch
                    checked={settings.enablePanning}
                    onCheckedChange={(checked) => updateSetting('enablePanning', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Enable Zooming</Label>
                    <p className="text-sm text-muted-foreground">
                      Scroll to zoom in and out
                    </p>
                  </div>
                  <Switch
                    checked={settings.enableZooming}
                    onCheckedChange={(checked) => updateSetting('enableZooming', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Enable Selection</Label>
                    <p className="text-sm text-muted-foreground">
                      Click to select diagram elements
                    </p>
                  </div>
                  <Switch
                    checked={settings.enableSelection}
                    onCheckedChange={(checked) => updateSetting('enableSelection', checked)}
                  />
                </div>

                <Separator />

                <div className="space-y-2">
                  <Label>Double-click Action</Label>
                  <div className="grid grid-cols-2 gap-2">
                    {[
                      { id: 'zoom', label: 'Zoom to Fit' },
                      { id: 'center', label: 'Center View' },
                    ].map((action) => (
                      <Button
                        key={action.id}
                        variant={settings.doubleClickAction === action.id ? 'default' : 'outline'}
                        size="sm"
                        onClick={() => updateSetting('doubleClickAction', action.id)}
                      >
                        {action.label}
                      </Button>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="advanced" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Advanced Settings</CardTitle>
                <CardDescription>
                  Developer and experimental features
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Debug Mode</Label>
                    <p className="text-sm text-muted-foreground">
                      Show debug information and logs
                    </p>
                  </div>
                  <Switch
                    checked={settings.debugMode}
                    onCheckedChange={(checked) => updateSetting('debugMode', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Performance Metrics</Label>
                    <p className="text-sm text-muted-foreground">
                      Display rendering performance data
                    </p>
                  </div>
                  <Switch
                    checked={settings.showPerformanceMetrics}
                    onCheckedChange={(checked) => updateSetting('showPerformanceMetrics', checked)}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label>Experimental Features</Label>
                    <p className="text-sm text-muted-foreground">
                      Enable beta features (may be unstable)
                    </p>
                  </div>
                  <Switch
                    checked={settings.enableExperimentalFeatures}
                    onCheckedChange={(checked) => updateSetting('enableExperimentalFeatures', checked)}
                  />
                </div>

                <Separator />

                <div className="space-y-2">
                  <Label>Export Defaults</Label>
                  <div className="grid grid-cols-2 gap-2">
                    {['svg', 'png', 'pdf', 'html'].map((format) => (
                      <Button
                        key={format}
                        variant={settings.defaultExportFormat === format ? 'default' : 'outline'}
                        size="sm"
                        onClick={() => updateSetting('defaultExportFormat', format)}
                        className="uppercase"
                      >
                        {format}
                      </Button>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>

        <Separator />

        {/* Action Buttons */}
        <div className="flex justify-between">
          <Button
            variant="outline"
            onClick={resetSettings}
          >
            <RotateCcw className="h-4 w-4 mr-2" />
            Reset to Defaults
          </Button>

          <div className="flex space-x-2">
            <Button
              variant="ghost"
              onClick={() => onOpenChange(false)}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button onClick={saveSettings}>
              <Save className="h-4 w-4 mr-2" />
              Save Settings
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default SettingsDialog;

