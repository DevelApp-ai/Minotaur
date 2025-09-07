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

import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { RailroadGenerator, GenerationResult } from '../visualization/railroad/RailroadGenerator';
import { RailroadGenerationOptions } from '../visualization/railroad/RailroadTypes';
import { Grammar } from '../core/grammar/Grammar';

interface RailroadDiagramViewerProps {
  grammarCode: string;
  options?: Partial<RailroadGenerationOptions>;
  onError?: (error: string) => void;
  onGenerated?: (result: GenerationResult) => void;
}

export const RailroadDiagramViewer: React.FC<RailroadDiagramViewerProps> = ({
  grammarCode,
  options = {},
  onError,
  onGenerated,
}) => {
  const [isGenerating, setIsGenerating] = useState(false);
  const [generationResult, setGenerationResult] = useState<GenerationResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const svgContainerRef = useRef<HTMLDivElement>(null);
  const railroadGenerator = useRef<RailroadGenerator>(new RailroadGenerator());

  // Default generation options
  const defaultOptions: RailroadGenerationOptions = useMemo(() => ({
    theme: 'default',
    layout: {
      direction: 'horizontal',
    },
    includeInheritance: true,
    includeContextSensitive: true,
    includeMetadata: true,
    embedStyles: true,
    includeInteractivity: true,
    showDebugInfo: false,
    optimizeForSize: true,
    ...options,
  }), [options]);

  const showTooltip = useCallback((element: HTMLElement, text: string) => {
    const tooltip = document.createElement('div');
    tooltip.className = 'railroad-tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
      position: absolute;
      background: #333;
      color: white;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      pointer-events: none;
      z-index: 1000;
      white-space: nowrap;
    `;

    const rect = element.getBoundingClientRect();
    tooltip.style.left = `${rect.left + rect.width / 2}px`;
    tooltip.style.top = `${rect.top - 30}px`;

    document.body.appendChild(tooltip);
  }, []);

  const hideTooltip = useCallback(() => {
    const tooltip = document.querySelector('.railroad-tooltip');
    if (tooltip) {
      tooltip.remove();
    }
  }, []);

  const highlightElement = useCallback((container: HTMLElement, elementId: string) => {
    // Remove previous highlights
    container.querySelectorAll('.highlighted').forEach(el => {
      el.classList.remove('highlighted');
    });

    // Add highlight to selected element
    const element = container.querySelector(`[data-element-id="${elementId}"]`);
    if (element) {
      element.classList.add('highlighted');
    }
  }, []);

  const addInteractivity = useCallback((container: HTMLElement) => {
    // Add hover effects for railroad elements
    const elements = container.querySelectorAll('[data-railroad-element]');
    elements.forEach(element => {
      element.addEventListener('mouseenter', (e) => {
        const target = e.target as HTMLElement;
        target.style.opacity = '0.8';

        // Show tooltip with element information
        const elementType = target.getAttribute('data-element-type');
        const elementName = target.getAttribute('data-element-name');
        if (elementType && elementName) {
          showTooltip(target, `${elementType}: ${elementName}`);
        }
      });

      element.addEventListener('mouseleave', (e) => {
        const target = e.target as HTMLElement;
        target.style.opacity = '1';
        hideTooltip();
      });

      element.addEventListener('click', (e) => {
        const target = e.target as HTMLElement;
        const elementId = target.getAttribute('data-element-id');
        if (elementId) {
          highlightElement(container, elementId);
        }
      });
    });
  }, [showTooltip, hideTooltip, highlightElement]);

  const generateRailroadDiagram = useCallback(async () => {
    setIsGenerating(true);
    setError(null);

    try {
      // Parse grammar code into Grammar object
      const grammar = parseGrammarCode(grammarCode);

      // Generate railroad diagram
      const result = await railroadGenerator.current.generateDiagram(grammar, defaultOptions);

      setGenerationResult(result);

      // Render SVG to container
      if (svgContainerRef.current && result.exportResult.content && typeof result.exportResult.content === 'string') {
        svgContainerRef.current.innerHTML = result.exportResult.content;

        // Add interactivity if enabled
        if (defaultOptions.includeInteractivity) {
          addInteractivity(svgContainerRef.current);
        }
      }

      onGenerated?.(result);

    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      setError(errorMessage);
      onError?.(errorMessage);
    } finally {
      setIsGenerating(false);
    }
  }, [grammarCode, defaultOptions, onGenerated, onError, addInteractivity]);

  useEffect(() => {
    if (!grammarCode.trim()) {
      setGenerationResult(null);
      setError(null);
      return;
    }

    generateRailroadDiagram();
  }, [grammarCode, defaultOptions, generateRailroadDiagram]);

  const downloadSVG = () => {
    if (!generationResult?.exportResult.content || typeof generationResult.exportResult.content !== 'string') {
      return;
    }

    const blob = new Blob([generationResult.exportResult.content], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'railroad-diagram.svg';
    a.click();
    URL.revokeObjectURL(url);
  };

  const downloadPNG = async () => {
    if (!svgContainerRef.current) {
      return;
    }

    const svg = svgContainerRef.current.querySelector('svg');
    if (!svg) {
      return;
    }

    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    const img = new Image();
    const svgData = new XMLSerializer().serializeToString(svg);
    const svgBlob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
    const url = URL.createObjectURL(svgBlob);

    img.onload = () => {
      canvas.width = img.width;
      canvas.height = img.height;
      ctx.drawImage(img, 0, 0);

      canvas.toBlob((blob) => {
        if (blob) {
          const pngUrl = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = pngUrl;
          a.download = 'railroad-diagram.png';
          a.click();
          URL.revokeObjectURL(pngUrl);
        }
      });

      URL.revokeObjectURL(url);
    };

    img.src = url;
  };

  if (!grammarCode.trim()) {
    return (
      <div className="railroad-diagram-viewer empty">
        <div className="empty-state">
          <h3>No Grammar Available</h3>
          <p>Enter a grammar in the editor to see its railroad diagram.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="railroad-diagram-viewer">
      <div className="railroad-controls">
        <div className="control-group">
          <button
            onClick={generateRailroadDiagram}
            disabled={isGenerating}
            className="btn btn-primary"
          >
            {isGenerating ? 'Generating...' : 'Regenerate'}
          </button>

          {generationResult && (
            <>
              <button onClick={downloadSVG} className="btn btn-secondary">
                Download SVG
              </button>
              <button onClick={downloadPNG} className="btn btn-secondary">
                Download PNG
              </button>
            </>
          )}
        </div>

        {generationResult && (
          <div className="diagram-metrics">
            <span>Elements: {generationResult.metrics.diagram.elementCount}</span>
            <span>Connections: {generationResult.metrics.diagram.connectionCount}</span>
            <span>Generation: {generationResult.metrics.generation.totalTime}ms</span>
          </div>
        )}
      </div>

      {error && (
        <div className="error-message">
          <strong>Error:</strong> {error}
        </div>
      )}

      {isGenerating && (
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Generating railroad diagram...</p>
        </div>
      )}

      <div
        ref={svgContainerRef}
        className="railroad-svg-container"
        style={{
          minHeight: '400px',
          border: '1px solid #ddd',
          borderRadius: '4px',
          padding: '16px',
          backgroundColor: '#fff',
        }}
      />

      {generationResult && (
        <div className="diagram-info">
          <details>
            <summary>Diagram Information</summary>
            <div className="info-grid">
              <div>
                <strong>Theme:</strong> {typeof generationResult.diagram.theme === 'string' ? generationResult.diagram.theme : generationResult.diagram.theme.name}
              </div>
              <div>
                <strong>Layout:</strong> {generationResult.diagram.layout.direction}
              </div>
              <div>
                <strong>Validation:</strong>
                <span className={generationResult.validation.valid ? 'valid' : 'invalid'}>
                  {generationResult.validation.valid ? 'Valid' : 'Invalid'}
                </span>
              </div>
              {generationResult.validation.errors.filter(e => e.type === 'warning').length > 0 && (
                <div>
                  <strong>Warnings:</strong>
                  <ul>
                    {generationResult.validation.errors.filter(e => e.type === 'warning').map((warning, index) => (
                      <li key={index}>{warning.message}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          </details>
        </div>
      )}
    </div>
  );
};

// Helper function to parse grammar code into Grammar object
function parseGrammarCode(grammarCode: string): Grammar {
  // This is a simplified parser - in a real implementation,
  // you would use the actual Minotaur grammar parser
  const grammar = new Grammar('ParsedGrammar');

  // Basic parsing logic
  const lines = grammarCode.split('\n');
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed && !trimmed.startsWith('//') && !trimmed.startsWith('#')) {
      // Parse production rules
      if (trimmed.includes('::=') || trimmed.includes(':=') || trimmed.includes('->')) {
        // Add production to grammar
        // This would be implemented with the actual grammar parsing logic
      }
    }
  }

  return grammar;
}

export default RailroadDiagramViewer;

