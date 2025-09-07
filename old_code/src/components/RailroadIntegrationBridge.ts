/*
 * Copyright 2025 DevelApp.ai
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * Railroad Integration Bridge
 *
 * This module provides a bridge between the UI components and the railroad
 * diagram generation system, ensuring proper integration and consistency.
 */

import { RailroadGenerator, GenerationResult } from '../visualization/railroad/RailroadGenerator';
import { RailroadGenerationOptions, RailroadTheme } from '../visualization/railroad/RailroadTypes';
import { Grammar } from '../core/grammar/Grammar';

/**
 * UI-specific railroad generation options
 */
export interface UIRailroadOptions extends Partial<RailroadGenerationOptions> {
  containerWidth?: number;
  containerHeight?: number;
  responsive?: boolean;
  showControls?: boolean;
  enableExport?: boolean;
  autoRegenerate?: boolean;
}

/**
 * Railroad integration events
 */
export interface RailroadIntegrationEvents {
  onGenerated?: (result: GenerationResult) => void;
  onError?: (error: string) => void;
  onThemeChanged?: (theme: RailroadTheme) => void;
  onExport?: (format: 'svg' | 'png', data: string | Blob) => void;
}

/**
 * Railroad Integration Bridge Class
 */
export class RailroadIntegrationBridge {
  private railroadGenerator: RailroadGenerator;
  private currentOptions: UIRailroadOptions;
  private events: RailroadIntegrationEvents;
  private cache: Map<string, GenerationResult>;

  constructor(options: UIRailroadOptions = {}, events: RailroadIntegrationEvents = {}) {
    this.railroadGenerator = new RailroadGenerator();
    this.currentOptions = {
      theme: 'default',
      layout: {
        direction: 'horizontal',
      },
      includeInheritance: true,
      includeContextSensitive: true,
      includeInteractivity: true,
      ...options,
    };
    this.events = events;
    this.cache = new Map();
  }

  /**
   * Generate railroad diagram from grammar code
   */
  async generateFromGrammarCode(grammarCode: string): Promise<GenerationResult | null> {
    if (!grammarCode.trim()) {
      return null;
    }

    try {
      // Create cache key
      const cacheKey = this.createCacheKey(grammarCode, this.currentOptions);

      // Check cache first
      if (this.cache.has(cacheKey)) {
        const result = this.cache.get(cacheKey)!;
        this.events.onGenerated?.(result);
        return result;
      }

      // Parse grammar
      const grammar = this.parseGrammarCode(grammarCode);

      // Generate diagram
      const generationOptions: RailroadGenerationOptions = {
        theme: this.currentOptions.theme || 'default',
        layout: this.currentOptions.layout || { direction: 'horizontal' },
        includeInheritance: this.currentOptions.includeInheritance ?? true,
        includeContextSensitive: this.currentOptions.includeContextSensitive ?? true,
        includeMetadata: this.currentOptions.includeMetadata ?? true,
        includeInteractivity: this.currentOptions.includeInteractivity ?? true,
        showDebugInfo: this.currentOptions.showDebugInfo ?? false,
        optimizeForSize: this.currentOptions.optimizeForSize ?? true,
      };

      const result = await this.railroadGenerator.generateDiagram(grammar, generationOptions);

      // Cache result
      this.cache.set(cacheKey, result);

      // Trigger event
      this.events.onGenerated?.(result);

      return result;

    } catch (error) {
      const errorMessage = error instanceof Error ? error instanceof Error ? error.message : String(error) : 'Unknown error occurred';
      this.events.onError?.(errorMessage);
      return null;
    }
  }

  /**
   * Update generation options
   */
  updateOptions(newOptions: Partial<UIRailroadOptions>): void {
    this.currentOptions = { ...this.currentOptions, ...newOptions };

    // Clear cache if options changed
    this.cache.clear();
  }

  /**
   * Change theme
   */
  changeTheme(themeName: string): void {
    this.currentOptions.theme = themeName;
    // Note: onThemeChanged event would need the actual theme object if needed

    // Clear cache to force regeneration with new theme
    this.cache.clear();
  }

  /**
   * Export diagram
   */
  async exportDiagram(result: GenerationResult, format: 'svg' | 'png'): Promise<void> {
    try {
      if (format === 'svg') {
        if (result.exportResult.content && result.exportResult.format === 'svg') {
          const svgContent = typeof result.exportResult.content === 'string'
            ? result.exportResult.content
            : result.exportResult.content.toString();
          this.events.onExport?.(format, svgContent);
        }
      } else if (format === 'png') {
        // Convert SVG to PNG
        if (result.exportResult.content && result.exportResult.format === 'svg') {
          const svgContent = typeof result.exportResult.content === 'string'
            ? result.exportResult.content
            : result.exportResult.content.toString();
          const pngBlob = await this.convertSvgToPng(svgContent);
          if (pngBlob) {
            this.events.onExport?.(format, pngBlob);
          }
        }
      }
    } catch (error) {
      this.events.onError?.(`Export failed: ${error}`);
    }
  }

  /**
   * Get available themes
   */
  getAvailableThemes(): string[] {
    return ['default', 'dark', 'light', 'colorful', 'minimal', 'classic'];
  }

  /**
   * Validate grammar code
   */
  validateGrammarCode(grammarCode: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!grammarCode.trim()) {
      errors.push('Grammar code is empty');
      return { isValid: false, errors };
    }

    try {
      this.parseGrammarCode(grammarCode);
      return { isValid: true, errors: [] };
    } catch (error) {
      errors.push(error instanceof Error ? error.message : 'Invalid grammar syntax');
      return { isValid: false, errors };
    }
  }

  /**
   * Clear cache
   */
  clearCache(): void {
    this.cache.clear();
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): { size: number; keys: string[] } {
    return {
      size: this.cache.size,
      keys: Array.from(this.cache.keys()),
    };
  }

  // Private methods

  private createCacheKey(grammarCode: string, options: UIRailroadOptions): string {
    const optionsHash = JSON.stringify({
      theme: options.theme,
      layout: options.layout,
      includeInheritance: options.includeInheritance,
      includeContextSensitive: options.includeContextSensitive,
      includeMetadata: options.includeMetadata,
    });

    return `${grammarCode.length}-${this.simpleHash(grammarCode)}-${this.simpleHash(optionsHash)}`;
  }

  private simpleHash(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return hash.toString(36);
  }

  private parseGrammarCode(grammarCode: string): Grammar {
    // This is a simplified parser - in a real implementation,
    // you would use the actual Minotaur grammar parser
    const grammar = new Grammar('ParsedGrammar');

    // Basic validation
    if (!grammarCode.includes('::=') && !grammarCode.includes(':=') && !grammarCode.includes('->')) {
      throw new Error('No production rules found in grammar');
    }

    // Basic parsing logic
    const lines = grammarCode.split('\n');
    let hasValidRules = false;

    for (const line of lines) {
      const trimmed = line.trim();
      if (trimmed && !trimmed.startsWith('//') && !trimmed.startsWith('#')) {
        // Parse production rules
        if (trimmed.includes('::=') || trimmed.includes(':=') || trimmed.includes('->')) {
          hasValidRules = true;
          // Add production to grammar
          // This would be implemented with the actual grammar parsing logic
        }
      }
    }

    if (!hasValidRules) {
      throw new Error('No valid production rules found');
    }

    return grammar;
  }

  private async convertSvgToPng(svgString: string): Promise<Blob | null> {
    return new Promise((resolve) => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) {
        resolve(null);
        return;
      }

      const img = new Image();
      const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' });
      const url = URL.createObjectURL(svgBlob);

      img.onload = () => {
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        canvas.toBlob((blob) => {
          URL.revokeObjectURL(url);
          resolve(blob);
        }, 'image/png');
      };

      img.onerror = () => {
        URL.revokeObjectURL(url);
        resolve(null);
      };

      img.src = url;
    });
  }
}

export default RailroadIntegrationBridge;

