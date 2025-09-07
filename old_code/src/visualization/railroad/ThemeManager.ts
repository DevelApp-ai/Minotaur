/**
 * Theme Management System for Railroad Diagrams
 *
 * This module provides comprehensive theme management including
 * predefined themes, custom theme creation, and theme validation.
 */

import { RailroadTheme, RailroadStyle, DeepPartial, Margin } from './RailroadTypes';

/**
 * Predefined themes for railroad diagrams
 */
export class ThemeManager {
  private themes: Map<string, RailroadTheme> = new Map();
  private currentTheme: string = 'default';

  constructor() {
    this.initializePredefinedThemes();
  }

  /**
     * Initialize predefined themes
     */
  private initializePredefinedThemes(): void {
    // Default theme
    this.registerTheme('default', this.createDefaultTheme());

    // Dark theme
    this.registerTheme('dark', this.createDarkTheme());

    // Light theme
    this.registerTheme('light', this.createLightTheme());

    // High contrast theme
    this.registerTheme('high-contrast', this.createHighContrastTheme());

    // Colorful theme
    this.registerTheme('colorful', this.createColorfulTheme());

    // Minimal theme
    this.registerTheme('minimal', this.createMinimalTheme());

    // Academic theme
    this.registerTheme('academic', this.createAcademicTheme());

    // Minotaur theme
    this.registerTheme('minotaur', this.createMinotaurTheme());
  }

  /**
     * Create default theme
     */
  private createDefaultTheme(): RailroadTheme {
    return {
      name: 'Default',
      description: 'Standard railroad diagram theme with balanced colors and typography',

      global: {
        backgroundColor: '#ffffff',
        fontFamily: 'Arial, sans-serif',
        fontSize: 14,
        lineColor: '#333333',
        textColor: '#333333',
      },

      elements: {
        terminal: {
          backgroundColor: '#f0f8ff',
          borderColor: '#4169e1',
          textColor: '#000080',
          borderWidth: 2,
          borderRadius: 8,
          padding: { top: 8, right: 12, bottom: 8, left: 12 },
          fontWeight: 'bold',
        },

        nonTerminal: {
          backgroundColor: '#f5f5f5',
          borderColor: '#666666',
          textColor: '#333333',
          borderWidth: 1,
          borderRadius: 4,
          padding: { top: 8, right: 12, bottom: 8, left: 12 },
          fontStyle: 'italic',
        },

        choice: {
          backgroundColor: '#fff8dc',
          borderColor: '#daa520',
          textColor: '#b8860b',
          borderWidth: 2,
        },

        sequence: {
          backgroundColor: 'transparent',
          borderColor: '#cccccc',
          textColor: '#666666',
          borderWidth: 1,
        },

        optional: {
          backgroundColor: '#f0fff0',
          borderColor: '#32cd32',
          textColor: '#228b22',
          borderWidth: 2,
        },

        repetition: {
          backgroundColor: '#ffe4e1',
          borderColor: '#dc143c',
          textColor: '#8b0000',
          borderWidth: 2,
        },

        group: {
          backgroundColor: 'rgba(240, 240, 240, 0.5)',
          borderColor: '#999999',
          textColor: '#666666',
          borderWidth: 1,
        },

        start: {
          backgroundColor: '#90ee90',
          borderColor: '#006400',
          textColor: '#006400',
          borderWidth: 3,
        },

        end: {
          backgroundColor: '#ffb6c1',
          borderColor: '#dc143c',
          textColor: '#dc143c',
          borderWidth: 3,
        },

        skip: {
          backgroundColor: 'transparent',
          borderColor: '#999999',
          textColor: '#999999',
          borderWidth: 1,
          strokeDashArray: '5,5',
        },

        comment: {
          backgroundColor: '#fffacd',
          borderColor: '#daa520',
          textColor: '#8b7355',
          borderWidth: 1,
          fontStyle: 'italic',
        },
      },

      states: {
        hover: {
          opacity: 0.8,
          borderWidth: 3,
        },

        selected: {
          borderColor: '#ff4500',
          borderWidth: 3,
          shadow: true,
        },

        highlighted: {
          backgroundColor: '#ffff99',
          borderColor: '#ffd700',
        },

        disabled: {
          opacity: 0.5,
          textColor: '#cccccc',
        },
      },

      grammarForge: {
        inherited: {
          borderColor: '#4169e1',
          backgroundColor: 'url(#inheritedPattern)',
          borderWidth: 2,
        },

        overridden: {
          borderColor: '#ff6347',
          borderWidth: 3,
          strokeDashArray: '3,3',
        },

        contextSensitive: {
          borderColor: '#9370db',
          backgroundColor: '#e6e6fa',
          borderWidth: 2,
        },

        newRule: {
          borderColor: '#32cd32',
          backgroundColor: '#f0fff0',
          borderWidth: 2,
        },
      },
    };
  }

  /**
     * Create dark theme
     */
  private createDarkTheme(): RailroadTheme {
    const defaultTheme = this.createDefaultTheme();

    return {
      ...defaultTheme,
      name: 'Dark',
      description: 'Dark theme optimized for low-light environments',

      global: {
        backgroundColor: '#1e1e1e',
        fontFamily: 'Consolas, monospace',
        fontSize: 14,
        lineColor: '#cccccc',
        textColor: '#ffffff',
      },

      elements: {
        ...defaultTheme.elements,
        terminal: {
          ...defaultTheme.elements.terminal,
          backgroundColor: '#2d3748',
          borderColor: '#4299e1',
          textColor: '#90cdf4',
        },

        nonTerminal: {
          ...defaultTheme.elements.nonTerminal,
          backgroundColor: '#2d3748',
          borderColor: '#718096',
          textColor: '#e2e8f0',
        },
      },
    };
  }

  /**
     * Create light theme
     */
  private createLightTheme(): RailroadTheme {
    const defaultTheme = this.createDefaultTheme();

    return {
      ...defaultTheme,
      name: 'Light',
      description: 'Clean light theme with high readability',

      global: {
        backgroundColor: '#fafafa',
        fontFamily: 'Helvetica, Arial, sans-serif',
        fontSize: 14,
        lineColor: '#424242',
        textColor: '#212121',
      },

      elements: {
        ...defaultTheme.elements,
        terminal: {
          ...defaultTheme.elements.terminal,
          backgroundColor: '#ffffff',
          borderColor: '#1976d2',
          textColor: '#0d47a1',
        },
      },
    };
  }

  /**
     * Create high contrast theme
     */
  private createHighContrastTheme(): RailroadTheme {
    return {
      name: 'High Contrast',
      description: 'High contrast theme for accessibility',

      global: {
        backgroundColor: '#ffffff',
        fontFamily: 'Arial, sans-serif',
        fontSize: 16,
        lineColor: '#000000',
        textColor: '#000000',
      },

      elements: {
        terminal: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 3,
          fontWeight: 'bold',
        },

        nonTerminal: {
          backgroundColor: '#000000',
          borderColor: '#000000',
          textColor: '#ffffff',
          borderWidth: 3,
          fontWeight: 'bold',
        },

        choice: {
          backgroundColor: '#ffff00',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 3,
        },

        sequence: {
          backgroundColor: 'transparent',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 2,
        },

        optional: {
          backgroundColor: '#00ff00',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 3,
        },

        repetition: {
          backgroundColor: '#ff0000',
          borderColor: '#000000',
          textColor: '#ffffff',
          borderWidth: 3,
        },

        group: {
          backgroundColor: '#cccccc',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 2,
        },

        start: {
          backgroundColor: '#00ff00',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 4,
        },

        end: {
          backgroundColor: '#ff0000',
          borderColor: '#000000',
          textColor: '#ffffff',
          borderWidth: 4,
        },

        skip: {
          backgroundColor: 'transparent',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 2,
        },

        comment: {
          backgroundColor: '#ffff00',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 2,
        },
      },

      states: {
        hover: {
          borderWidth: 4,
        },

        selected: {
          backgroundColor: '#0000ff',
          textColor: '#ffffff',
          borderWidth: 4,
        },

        highlighted: {
          backgroundColor: '#ffff00',
          borderColor: '#000000',
        },

        disabled: {
          opacity: 0.3,
        },
      },

      grammarForge: {
        inherited: {
          borderColor: '#0000ff',
          borderWidth: 4,
        },

        overridden: {
          borderColor: '#ff00ff',
          borderWidth: 4,
        },

        contextSensitive: {
          borderColor: '#800080',
          borderWidth: 4,
        },

        newRule: {
          borderColor: '#008000',
          borderWidth: 4,
        },
      },
    };
  }

  /**
     * Create colorful theme
     */
  private createColorfulTheme(): RailroadTheme {
    const defaultTheme = this.createDefaultTheme();

    return {
      ...defaultTheme,
      name: 'Colorful',
      description: 'Vibrant theme with distinct colors for each element type',

      elements: {
        ...defaultTheme.elements,
        terminal: {
          ...defaultTheme.elements.terminal,
          backgroundColor: '#ff6b6b',
          borderColor: '#ee5a52',
          textColor: '#ffffff',
        },

        nonTerminal: {
          ...defaultTheme.elements.nonTerminal,
          backgroundColor: '#4ecdc4',
          borderColor: '#26d0ce',
          textColor: '#ffffff',
        },

        choice: {
          ...defaultTheme.elements.choice,
          backgroundColor: '#ffe66d',
          borderColor: '#ffcc02',
          textColor: '#333333',
        },

        optional: {
          ...defaultTheme.elements.optional,
          backgroundColor: '#95e1d3',
          borderColor: '#3dd5f3',
          textColor: '#333333',
        },

        repetition: {
          ...defaultTheme.elements.repetition,
          backgroundColor: '#a8e6cf',
          borderColor: '#88d8a3',
          textColor: '#333333',
        },
      },
    };
  }

  /**
     * Create minimal theme
     */
  private createMinimalTheme(): RailroadTheme {
    return {
      name: 'Minimal',
      description: 'Clean minimal theme with subtle styling',

      global: {
        backgroundColor: '#ffffff',
        fontFamily: 'system-ui, -apple-system, sans-serif',
        fontSize: 13,
        lineColor: '#666666',
        textColor: '#333333',
      },

      elements: {
        terminal: {
          backgroundColor: 'transparent',
          borderColor: '#333333',
          textColor: '#333333',
          borderWidth: 1,
          borderRadius: 2,
          padding: { top: 4, right: 8, bottom: 4, left: 8 },
        },

        nonTerminal: {
          backgroundColor: 'transparent',
          borderColor: '#666666',
          textColor: '#666666',
          borderWidth: 1,
          borderRadius: 0,
          padding: { top: 4, right: 8, bottom: 4, left: 8 },
        },

        choice: {
          backgroundColor: 'transparent',
          borderColor: '#999999',
          textColor: '#999999',
          borderWidth: 1,
        },

        sequence: {
          backgroundColor: 'transparent',
          borderColor: 'transparent',
          textColor: '#999999',
          borderWidth: 0,
        },

        optional: {
          backgroundColor: 'transparent',
          borderColor: '#cccccc',
          textColor: '#cccccc',
          borderWidth: 1,
        },

        repetition: {
          backgroundColor: 'transparent',
          borderColor: '#cccccc',
          textColor: '#cccccc',
          borderWidth: 1,
        },

        group: {
          backgroundColor: 'transparent',
          borderColor: '#eeeeee',
          textColor: '#999999',
          borderWidth: 1,
        },

        start: {
          backgroundColor: 'transparent',
          borderColor: '#333333',
          textColor: '#333333',
          borderWidth: 2,
        },

        end: {
          backgroundColor: 'transparent',
          borderColor: '#333333',
          textColor: '#333333',
          borderWidth: 2,
        },

        skip: {
          backgroundColor: 'transparent',
          borderColor: '#cccccc',
          textColor: '#cccccc',
          borderWidth: 1,
        },

        comment: {
          backgroundColor: 'transparent',
          borderColor: '#eeeeee',
          textColor: '#999999',
          borderWidth: 1,
        },
      },

      states: {
        hover: {
          opacity: 0.7,
        },

        selected: {
          borderColor: '#333333',
          borderWidth: 2,
        },

        highlighted: {
          backgroundColor: '#f5f5f5',
        },

        disabled: {
          opacity: 0.3,
        },
      },

      grammarForge: {
        inherited: {
          borderColor: '#666666',
          strokeDashArray: '2,2',
        },

        overridden: {
          borderColor: '#333333',
          borderWidth: 2,
        },

        contextSensitive: {
          borderColor: '#999999',
          strokeDashArray: '1,1',
        },

        newRule: {
          borderColor: '#333333',
        },
      },
    };
  }

  /**
     * Create academic theme
     */
  private createAcademicTheme(): RailroadTheme {
    return {
      name: 'Academic',
      description: 'Professional theme suitable for academic publications',

      global: {
        backgroundColor: '#ffffff',
        fontFamily: 'Times, serif',
        fontSize: 12,
        lineColor: '#000000',
        textColor: '#000000',
      },

      elements: {
        terminal: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
          borderRadius: 0,
          padding: { top: 6, right: 10, bottom: 6, left: 10 },
          fontFamily: 'Times, serif',
        },

        nonTerminal: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
          borderRadius: 0,
          padding: { top: 6, right: 10, bottom: 6, left: 10 },
          fontStyle: 'italic',
          fontFamily: 'Times, serif',
        },

        choice: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
        },

        sequence: {
          backgroundColor: 'transparent',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
        },

        optional: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
        },

        repetition: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
        },

        group: {
          backgroundColor: '#f8f8f8',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
        },

        start: {
          backgroundColor: '#ffffff',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 2,
        },

        end: {
          backgroundColor: '#000000',
          borderColor: '#000000',
          textColor: '#ffffff',
          borderWidth: 2,
        },

        skip: {
          backgroundColor: 'transparent',
          borderColor: '#000000',
          textColor: '#000000',
          borderWidth: 1,
          strokeDashArray: '3,3',
        },

        comment: {
          backgroundColor: '#ffffff',
          borderColor: '#666666',
          textColor: '#666666',
          borderWidth: 1,
          fontStyle: 'italic',
        },
      },

      states: {
        hover: {
          backgroundColor: '#f0f0f0',
        },

        selected: {
          backgroundColor: '#e0e0e0',
          borderWidth: 2,
        },

        highlighted: {
          backgroundColor: '#f5f5f5',
        },

        disabled: {
          opacity: 0.5,
        },
      },

      grammarForge: {
        inherited: {
          strokeDashArray: '5,2',
        },

        overridden: {
          borderWidth: 2,
        },

        contextSensitive: {
          strokeDashArray: '2,1',
        },

        newRule: {
          borderWidth: 2,
        },
      },
    };
  }

  /**
     * Create Minotaur-specific theme
     */
  private createMinotaurTheme(): RailroadTheme {
    const defaultTheme = this.createDefaultTheme();

    return {
      ...defaultTheme,
      name: 'Minotaur',
      description: 'Official Minotaur theme highlighting inheritance and context features',

      global: {
        backgroundColor: '#fafbfc',
        fontFamily: 'SF Pro Display, -apple-system, BlinkMacSystemFont, sans-serif',
        fontSize: 14,
        lineColor: '#586069',
        textColor: '#24292e',
      },

      grammarForge: {
        inherited: {
          borderColor: '#0366d6',
          backgroundColor: '#f1f8ff',
          borderWidth: 2,
          strokeDashArray: '8,4',
        },

        overridden: {
          borderColor: '#d73a49',
          backgroundColor: '#ffeef0',
          borderWidth: 3,
          strokeDashArray: '4,2',
        },

        contextSensitive: {
          borderColor: '#6f42c1',
          backgroundColor: '#f5f0ff',
          borderWidth: 2,
          strokeDashArray: '6,3',
        },

        newRule: {
          borderColor: '#28a745',
          backgroundColor: '#f0fff4',
          borderWidth: 2,
        },
      },
    };
  }

  /**
     * Register a new theme
     */
  public registerTheme(name: string, theme: RailroadTheme): void {
    this.themes.set(name, theme);
  }

  /**
     * Get a theme by name
     */
  public getTheme(name: string): RailroadTheme | undefined {
    return this.themes.get(name);
  }

  /**
     * Get current theme
     */
  public getCurrentTheme(): RailroadTheme {
    return this.themes.get(this.currentTheme) || this.themes.get('default')!;
  }

  /**
     * Set current theme
     */
  public setCurrentTheme(name: string): boolean {
    if (this.themes.has(name)) {
      this.currentTheme = name;
      return true;
    }
    return false;
  }

  /**
     * Get all available theme names
     */
  public getAvailableThemes(): string[] {
    return Array.from(this.themes.keys());
  }

  /**
     * Create a custom theme based on an existing theme
     */
  public createCustomTheme(
    baseName: string,
    customizations: DeepPartial<RailroadTheme>,
    newName: string,
  ): RailroadTheme | null {
    const baseTheme = this.getTheme(baseName);
    if (!baseTheme) {
      return null;
    }

    const customTheme = this.mergeThemes(baseTheme, customizations);
    customTheme.name = newName;

    this.registerTheme(newName, customTheme);
    return customTheme;
  }

  /**
   * Merge margin objects, providing defaults for missing properties
   */
  private mergeMargin(base: Margin, customization?: DeepPartial<Margin>): Margin {
    if (!customization) {
return base;
}
    return {
      top: customization.top ?? base.top,
      right: customization.right ?? base.right,
      bottom: customization.bottom ?? base.bottom,
      left: customization.left ?? base.left,
    };
  }

  /**
   * Merge style objects, properly handling nested Margin types
   */
  private mergeStyle(base: RailroadStyle, customization?: DeepPartial<RailroadStyle>): RailroadStyle {
    if (!customization) {
return base;
}

    const result: RailroadStyle = { ...base };

    // Handle scalar properties
    if (customization.backgroundColor !== undefined) {
result.backgroundColor = customization.backgroundColor;
}
    if (customization.borderColor !== undefined) {
result.borderColor = customization.borderColor;
}
    if (customization.textColor !== undefined) {
result.textColor = customization.textColor;
}
    if (customization.lineColor !== undefined) {
result.lineColor = customization.lineColor;
}
    if (customization.fontFamily !== undefined) {
result.fontFamily = customization.fontFamily;
}
    if (customization.fontSize !== undefined) {
result.fontSize = customization.fontSize;
}
    if (customization.fontWeight !== undefined) {
result.fontWeight = customization.fontWeight;
}
    if (customization.fontStyle !== undefined) {
result.fontStyle = customization.fontStyle;
}
    if (customization.borderWidth !== undefined) {
result.borderWidth = customization.borderWidth;
}
    if (customization.borderRadius !== undefined) {
result.borderRadius = customization.borderRadius;
}
    if (customization.strokeWidth !== undefined) {
result.strokeWidth = customization.strokeWidth;
}

    // Handle padding specifically
    if (base.padding || customization.padding) {
      result.padding = this.mergeMargin(
        base.padding || { top: 0, right: 0, bottom: 0, left: 0 },
        customization.padding,
      );
    }

    // Handle margin specifically
    if (base.margin || customization.margin) {
      result.margin = this.mergeMargin(
        base.margin || { top: 0, right: 0, bottom: 0, left: 0 },
        customization.margin,
      );
    }

    return result;
  }

  /**
     * Merge themes with deep merging
     */
  private mergeThemes(base: RailroadTheme, customizations: DeepPartial<RailroadTheme>): RailroadTheme {
    return {
      name: customizations.name || base.name,
      description: customizations.description || base.description,

      global: {
        ...base.global,
        ...customizations.global,
      },

      elements: {
        terminal: this.mergeStyle(base.elements.terminal, customizations.elements?.terminal),
        nonTerminal: this.mergeStyle(base.elements.nonTerminal, customizations.elements?.nonTerminal),
        choice: this.mergeStyle(base.elements.choice, customizations.elements?.choice),
        sequence: this.mergeStyle(base.elements.sequence, customizations.elements?.sequence),
        optional: this.mergeStyle(base.elements.optional, customizations.elements?.optional),
        repetition: this.mergeStyle(base.elements.repetition, customizations.elements?.repetition),
        group: this.mergeStyle(base.elements.group, customizations.elements?.group),
        start: this.mergeStyle(base.elements.start, customizations.elements?.start),
        end: this.mergeStyle(base.elements.end, customizations.elements?.end),
        skip: this.mergeStyle(base.elements.skip, customizations.elements?.skip),
        comment: this.mergeStyle(base.elements.comment, customizations.elements?.comment),
      },

      states: {
        hover: this.mergeStyle(base.states.hover, customizations.states?.hover),
        selected: this.mergeStyle(base.states.selected, customizations.states?.selected),
        highlighted: this.mergeStyle(base.states.highlighted, customizations.states?.highlighted),
        disabled: this.mergeStyle(base.states.disabled, customizations.states?.disabled),
      },

      grammarForge: {
        inherited: this.mergeStyle(base.grammarForge.inherited, customizations.grammarForge?.inherited),
        overridden: this.mergeStyle(base.grammarForge.overridden, customizations.grammarForge?.overridden),
        // eslint-disable-next-line max-len
        contextSensitive: this.mergeStyle(base.grammarForge.contextSensitive, customizations.grammarForge?.contextSensitive),
        newRule: this.mergeStyle(base.grammarForge.newRule, customizations.grammarForge?.newRule),
      },
    };
  }

  /**
     * Validate theme structure
     */
  public validateTheme(theme: RailroadTheme): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Check required properties
    if (!theme.name) {
      errors.push('Theme name is required');
    }
    if (!theme.global) {
      errors.push('Global configuration is required');
    }
    if (!theme.elements) {
      errors.push('Element styles are required');
    }
    if (!theme.states) {
      errors.push('State styles are required');
    }
    if (!theme.grammarForge) {
      errors.push('Minotaur styles are required');
    }

    // Check global properties
    if (theme.global) {
      if (!theme.global.backgroundColor) {
        errors.push('Global background color is required');
      }
      if (!theme.global.fontFamily) {
        errors.push('Global font family is required');
      }
      if (!theme.global.fontSize || theme.global.fontSize <= 0) {
        errors.push('Valid global font size is required');
      }
      if (!theme.global.lineColor) {
        errors.push('Global line color is required');
      }
      if (!theme.global.textColor) {
        errors.push('Global text color is required');
      }
    }

    // Check element styles
    const requiredElements = ['terminal', 'nonTerminal', 'choice', 'sequence', 'optional', 'repetition', 'group', 'start', 'end', 'skip', 'comment'];
    for (const elementType of requiredElements) {
      if (!theme.elements || !theme.elements[elementType as keyof typeof theme.elements]) {
        errors.push(`Style for ${elementType} element is required`);
      }
    }

    return {
      valid: errors.length === 0,
      errors,
    };
  }

  /**
     * Export theme to JSON
     */
  public exportTheme(name: string): string | null {
    const theme = this.getTheme(name);
    if (!theme) {
      return null;
    }

    return JSON.stringify(theme, null, 2);
  }

  /**
     * Import theme from JSON
     */
  public importTheme(json: string, name?: string): boolean {
    try {
      const theme: RailroadTheme = JSON.parse(json);

      // Validate imported theme
      const validation = this.validateTheme(theme);
      if (!validation.valid) {
    // eslint-disable-next-line no-console
        console.error('Invalid theme:', validation.errors);
        return false;
      }

      // Use provided name or theme's name
      const themeName = name || theme.name;
      this.registerTheme(themeName, theme);

      return true;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to import theme:', error);
      return false;
    }
  }

  /**
     * Get theme statistics
     */
  public getThemeStats(): {
        totalThemes: number;
        currentTheme: string;
        availableThemes: string[];
        } {
    return {
      totalThemes: this.themes.size,
      currentTheme: this.currentTheme,
      availableThemes: this.getAvailableThemes(),
    };
  }
}

