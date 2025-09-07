import { useState } from 'react';
import { motion } from 'framer-motion';
import {
  Palette,
  Sun,
  Moon,
  Monitor,
  Contrast,
  Sparkles,
  Minimize,
  GraduationCap,
  GitBranch,
  Check,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Card, CardContent } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Label } from '@/components/ui/label.jsx';

const ThemeSelector = ({ selectedTheme = 'default', onThemeChange }) => {
  const [previewTheme, setPreviewTheme] = useState(null);

  const themes = [
    {
      id: 'default',
      name: 'Default',
      description: 'Balanced colors and professional appearance',
      icon: Monitor,
      colors: {
        primary: '#4169e1',
        secondary: '#f5f5f5',
        background: '#ffffff',
        text: '#333333',
      },
    },
    {
      id: 'dark',
      name: 'Dark',
      description: 'Optimized for low-light environments',
      icon: Moon,
      colors: {
        primary: '#4299e1',
        secondary: '#2d3748',
        background: '#1e1e1e',
        text: '#e2e8f0',
      },
    },
    {
      id: 'light',
      name: 'Light',
      description: 'Clean light theme with high readability',
      icon: Sun,
      colors: {
        primary: '#1976d2',
        secondary: '#ffffff',
        background: '#fafafa',
        text: '#212121',
      },
    },
    {
      id: 'high-contrast',
      name: 'High Contrast',
      description: 'Maximum accessibility and readability',
      icon: Contrast,
      colors: {
        primary: '#000000',
        secondary: '#ffffff',
        background: '#ffffff',
        text: '#000000',
      },
    },
    {
      id: 'colorful',
      name: 'Colorful',
      description: 'Vibrant theme with distinct element colors',
      icon: Sparkles,
      colors: {
        primary: '#ff6b6b',
        secondary: '#4ecdc4',
        background: '#ffffff',
        text: '#333333',
      },
    },
    {
      id: 'minimal',
      name: 'Minimal',
      description: 'Clean and subtle styling',
      icon: Minimize,
      colors: {
        primary: '#333333',
        secondary: 'transparent',
        background: '#ffffff',
        text: '#333333',
      },
    },
    {
      id: 'academic',
      name: 'Academic',
      description: 'Professional theme for publications',
      icon: GraduationCap,
      colors: {
        primary: '#000000',
        secondary: '#ffffff',
        background: '#ffffff',
        text: '#000000',
      },
    },
    {
      id: 'grammarforge',
      name: 'GrammarForge',
      description: 'Official theme highlighting inheritance features',
      icon: GitBranch,
      colors: {
        primary: '#0366d6',
        secondary: '#f1f8ff',
        background: '#fafbfc',
        text: '#24292e',
      },
    },
  ];

  const handleThemeSelect = (themeId) => {
    onThemeChange?.(themeId);
  };

  const handleThemePreview = (themeId) => {
    setPreviewTheme(themeId);
  };

  const clearPreview = () => {
    setPreviewTheme(null);
  };

  const currentTheme = previewTheme || selectedTheme;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">Theme Selection</Label>
        <Badge variant="secondary" className="text-xs">
          {themes.find(t => t.id === selectedTheme)?.name}
        </Badge>
      </div>

      <div className="grid grid-cols-2 gap-2">
        {themes.map((theme) => {
          const Icon = theme.icon;
          const isSelected = selectedTheme === theme.id;
          const isPreviewing = previewTheme === theme.id;

          return (
            <motion.div
              key={theme.id}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
            >
              <Card
                className={`cursor-pointer transition-all duration-200 ${
                  isSelected
                    ? 'ring-2 ring-primary ring-offset-2'
                    : isPreviewing
                      ? 'ring-1 ring-muted-foreground'
                      : 'hover:shadow-md'
                }`}
                onClick={() => handleThemeSelect(theme.id)}
                onMouseEnter={() => handleThemePreview(theme.id)}
                onMouseLeave={clearPreview}
              >
                <CardContent className="p-3">
                  <div className="space-y-2">
                    {/* Theme Header */}
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <Icon className="h-4 w-4" />
                        <span className="font-medium text-sm">{theme.name}</span>
                      </div>
                      {isSelected && (
                        <Check className="h-4 w-4 text-primary" />
                      )}
                    </div>

                    {/* Color Preview */}
                    <div className="flex space-x-1">
                      <div
                        className="w-4 h-4 rounded-sm border border-border"
                        style={{ backgroundColor: theme.colors.primary }}
                        title="Primary color"
                      />
                      <div
                        className="w-4 h-4 rounded-sm border border-border"
                        style={{ backgroundColor: theme.colors.secondary }}
                        title="Secondary color"
                      />
                      <div
                        className="w-4 h-4 rounded-sm border border-border"
                        style={{ backgroundColor: theme.colors.background }}
                        title="Background color"
                      />
                      <div
                        className="w-4 h-4 rounded-sm border border-border"
                        style={{ backgroundColor: theme.colors.text }}
                        title="Text color"
                      />
                    </div>

                    {/* Description */}
                    <p className="text-xs text-muted-foreground line-clamp-2">
                      {theme.description}
                    </p>
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          );
        })}
      </div>

      {/* Theme Preview */}
      {previewTheme && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: 10 }}
          className="p-3 border border-border rounded-lg bg-card"
        >
          <div className="space-y-2">
            <div className="flex items-center space-x-2">
              <Palette className="h-4 w-4 text-muted-foreground" />
              <span className="text-sm font-medium">Preview</span>
            </div>

            {/* Mini diagram preview */}
            <div className="relative">
              <svg
                width="100%"
                height="60"
                viewBox="0 0 200 60"
                className="border border-border rounded"
                style={{ backgroundColor: themes.find(t => t.id === previewTheme)?.colors.background }}
              >
                {/* Sample terminal */}
                <rect
                  x="10"
                  y="20"
                  width="40"
                  height="20"
                  rx="4"
                  fill={themes.find(t => t.id === previewTheme)?.colors.secondary}
                  stroke={themes.find(t => t.id === previewTheme)?.colors.primary}
                  strokeWidth="1"
                />
                <text
                  x="30"
                  y="32"
                  textAnchor="middle"
                  fontSize="8"
                  fill={themes.find(t => t.id === previewTheme)?.colors.text}
                >
                  term
                </text>

                {/* Connection */}
                <line
                  x1="50"
                  y1="30"
                  x2="70"
                  y2="30"
                  stroke={themes.find(t => t.id === previewTheme)?.colors.text}
                  strokeWidth="1"
                />

                {/* Sample non-terminal */}
                <rect
                  x="70"
                  y="20"
                  width="50"
                  height="20"
                  fill={themes.find(t => t.id === previewTheme)?.colors.secondary}
                  stroke={themes.find(t => t.id === previewTheme)?.colors.primary}
                  strokeWidth="1"
                />
                <text
                  x="95"
                  y="32"
                  textAnchor="middle"
                  fontSize="8"
                  fill={themes.find(t => t.id === previewTheme)?.colors.text}
                  fontStyle="italic"
                >
                  expression
                </text>

                {/* Connection */}
                <line
                  x1="120"
                  y1="30"
                  x2="140"
                  y2="30"
                  stroke={themes.find(t => t.id === previewTheme)?.colors.text}
                  strokeWidth="1"
                />

                {/* Sample terminal */}
                <rect
                  x="140"
                  y="20"
                  width="40"
                  height="20"
                  rx="4"
                  fill={themes.find(t => t.id === previewTheme)?.colors.secondary}
                  stroke={themes.find(t => t.id === previewTheme)?.colors.primary}
                  strokeWidth="1"
                />
                <text
                  x="160"
                  y="32"
                  textAnchor="middle"
                  fontSize="8"
                  fill={themes.find(t => t.id === previewTheme)?.colors.text}
                >
                  term
                </text>
              </svg>
            </div>

            <div className="text-xs text-muted-foreground">
              {themes.find(t => t.id === previewTheme)?.description}
            </div>
          </div>
        </motion.div>
      )}

      {/* Quick Actions */}
      <div className="flex space-x-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleThemeSelect('default')}
          className="flex-1"
        >
          <Monitor className="h-4 w-4 mr-2" />
          Default
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleThemeSelect('dark')}
          className="flex-1"
        >
          <Moon className="h-4 w-4 mr-2" />
          Dark
        </Button>
      </div>
    </div>
  );
};

export default ThemeSelector;

