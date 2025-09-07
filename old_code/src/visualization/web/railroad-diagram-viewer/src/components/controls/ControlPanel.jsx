import { motion } from 'framer-motion';
import {
  Play,
  Pause,
  RotateCcw,
  ZoomIn,
  ZoomOut,
  Grid,
  Maximize,
  Minimize,
  Move,
  MousePointer,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Slider } from '@/components/ui/slider.jsx';
import { Switch } from '@/components/ui/switch.jsx';
import { Label } from '@/components/ui/label.jsx';
import { Separator } from '@/components/ui/separator.jsx';
import { Badge } from '@/components/ui/badge.jsx';

const ControlPanel = ({
  isPlaying = false,
  zoomLevel = 100,
  showGrid = false,
  onPlayPause,
  onZoomIn,
  onZoomOut,
  onZoomReset,
  onToggleGrid,
  onZoomChange,
}) => {
  const handleZoomSliderChange = (value) => {
    onZoomChange?.(value[0]);
  };

  return (
    <div className="space-y-4">
      {/* Animation Controls */}
      <div className="space-y-3">
        <Label className="text-sm font-medium">Animation</Label>
        <div className="flex items-center space-x-2">
          <Button
            variant={isPlaying ? 'default' : 'outline'}
            size="sm"
            onClick={onPlayPause}
            className="flex-1"
          >
            {isPlaying ? (
              <>
                <Pause className="h-4 w-4 mr-2" />
                Pause
              </>
            ) : (
              <>
                <Play className="h-4 w-4 mr-2" />
                Play
              </>
            )}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onZoomReset}
            title="Reset animation"
          >
            <RotateCcw className="h-4 w-4" />
          </Button>
        </div>
        {isPlaying && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="flex items-center space-x-2 text-sm text-muted-foreground"
          >
            <div className="h-2 w-2 bg-green-500 rounded-full animate-pulse" />
            <span>Animation running</span>
          </motion.div>
        )}
      </div>

      <Separator />

      {/* Zoom Controls */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Zoom</Label>
          <Badge variant="secondary" className="text-xs">
            {zoomLevel}%
          </Badge>
        </div>

        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={onZoomOut}
            disabled={zoomLevel <= 25}
          >
            <ZoomOut className="h-4 w-4" />
          </Button>

          <div className="flex-1 px-2">
            <Slider
              value={[zoomLevel]}
              onValueChange={handleZoomSliderChange}
              min={25}
              max={400}
              step={25}
              className="w-full"
            />
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={onZoomIn}
            disabled={zoomLevel >= 400}
          >
            <ZoomIn className="h-4 w-4" />
          </Button>
        </div>

        <div className="flex items-center space-x-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onZoomChange?.(50)}
            className="text-xs h-6 px-2"
          >
            50%
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onZoomChange?.(100)}
            className="text-xs h-6 px-2"
          >
            100%
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onZoomChange?.(200)}
            className="text-xs h-6 px-2"
          >
            200%
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={onZoomReset}
            className="text-xs h-6 px-2"
            title="Fit to view"
          >
            <Maximize className="h-3 w-3" />
          </Button>
        </div>
      </div>

      <Separator />

      {/* View Options */}
      <div className="space-y-3">
        <Label className="text-sm font-medium">View Options</Label>

        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Grid className="h-4 w-4 text-muted-foreground" />
            <Label htmlFor="grid-toggle" className="text-sm">Show Grid</Label>
          </div>
          <Switch
            id="grid-toggle"
            checked={showGrid}
            onCheckedChange={onToggleGrid}
          />
        </div>
      </div>

      <Separator />

      {/* Navigation Help */}
      <div className="space-y-2">
        <Label className="text-sm font-medium">Navigation</Label>
        <div className="space-y-2 text-xs text-muted-foreground">
          <div className="flex items-center space-x-2">
            <MousePointer className="h-3 w-3" />
            <span>Click to select elements</span>
          </div>
          <div className="flex items-center space-x-2">
            <Move className="h-3 w-3" />
            <span>Ctrl+drag to pan view</span>
          </div>
          <div className="flex items-center space-x-2">
            <ZoomIn className="h-3 w-3" />
            <span>Scroll to zoom</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ControlPanel;

