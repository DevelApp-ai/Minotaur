/**
 * Export Service for Railroad Diagrams
 *
 * This service provides functionality for exporting railroad diagrams in various formats.
 * It provides a fallback implementation for when the full backend isn't available.
 */

/**
 * Export service class
 */
export class ExportService {
  constructor() {
    // Initialize without heavy backend dependencies to avoid circular references
    this.backendAvailable = false;
    this.initializingBackend = false;
    this.initializeBackend();
  }

  /**
   * Try to initialize backend services
   */
  async initializeBackend() {
    // Skip initialization in test environment to prevent issues
    if (process.env.NODE_ENV === 'test' || typeof jest !== 'undefined') {
    // eslint-disable-next-line no-console
      console.log('Skipping backend service initialization in test environment');
      this.backendAvailable = false;
      this.initializingBackend = false;
      return;
    }

    if (this.initializingBackend) {
      return;
    }

    this.initializingBackend = true;
    try {
      // Try to dynamically import backend services
      const { RailroadGenerator } = await import('../../../../railroad/RailroadGenerator');
      const { ThemeManager } = await import('../../../../railroad/ThemeManager');

      this.railroadGenerator = new RailroadGenerator();
      this.themeManager = new ThemeManager();
      this.backendAvailable = true;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.warn('Backend services not available, using fallback implementations:', error.message);
      this.backendAvailable = false;
    } finally {
      this.initializingBackend = false;
    }
  }

  /**
   * Export diagram as SVG
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Promise<Object>} Export result
   */
  async exportSVG(diagram, options = {}) {
    try {
      // Try to use the real SVG export if backend is available
      if (this.backendAvailable && this.railroadGenerator) {
        const exportResult = await this.railroadGenerator.exportDiagram(
          diagram,
          'svg',
          {
            includeStyles: options.includeStyles !== false,
            includeInteractivity: options.includeInteractivity !== false,
            includeAnimations: options.includeAnimations !== false,
            optimizeOutput: options.optimizeOutput !== false,
            minifyOutput: options.minifyOutput || false,
            includeDebugInfo: options.includeDebugInfo || false,
            ...options,
          },
        );

        return {
          success: true,
          content: exportResult.content,
          format: exportResult.format,
          metadata: exportResult.metadata,
          filename: `${diagram.name || 'railroad-diagram'}.svg`,
          mimeType: 'image/svg+xml',
        };
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Full SVG export failed, falling back to basic implementation:', error);
    }

    // Fallback to basic SVG generation
    return this.generateBasicSVG(diagram, options);
  }

  /**
   * Export diagram as PNG (converts from SVG)
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Promise<Object>} Export result
   */
  async exportPNG(diagram, options = {}) {
    // Guard browser-only functionality
    if (typeof window === 'undefined' || typeof document === 'undefined' || typeof URL === 'undefined') {
      return {
        success: false,
        error: 'PNG export is only supported in a browser environment',
      };
    }

    try {
      // First get SVG
      const svgResult = await this.exportSVG(diagram, options);

      if (svgResult.success) {
        // Convert SVG to PNG using canvas
        const pngBlob = await this.convertSVGToPNG(svgResult.content, options);

        if (pngBlob) {
          return {
            success: true,
            content: pngBlob,
            format: 'png',
            metadata: svgResult.metadata,
            filename: `${diagram.name || 'railroad-diagram'}.png`,
            mimeType: 'image/png',
          };
        }
      }

      throw new Error('Failed to generate PNG from SVG');
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('PNG export failed:', error);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  /**
   * Export diagram as HTML (with embedded SVG)
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Promise<Object>} Export result
   */
  async exportHTML(diagram, options = {}) {
    try {
      // Try to use the real HTML export if backend is available
      if (this.backendAvailable && this.railroadGenerator) {
        const exportResult = await this.railroadGenerator.exportDiagram(
          diagram,
          'html',
          options,
        );

        return {
          success: true,
          content: exportResult.content,
          format: exportResult.format,
          metadata: exportResult.metadata,
          filename: `${diagram.name || 'railroad-diagram'}.html`,
          mimeType: 'text/html',
        };
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Full HTML export failed, falling back to basic implementation:', error);
    }

    // Fallback to basic HTML generation
    return this.generateBasicHTML(diagram, options);
  }

  /**
   * Generate basic SVG as fallback
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Object} Basic SVG export result
   */
  generateBasicSVG(diagram, options = {}) {
    const { width = 1000, height = 600 } = diagram.bounds || {};
    const backgroundColor = options.backgroundColor || 'white';
    const textColor = options.textColor || '#333333';

    const svgContent = `<?xml version="1.0" encoding="UTF-8"?>
<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <style>
      .diagram-title { font-family: Arial, sans-serif; font-size: 18px; font-weight: bold; }
      .diagram-element { font-family: Arial, sans-serif; font-size: 12px; }
      .terminal { fill: #f0f0f0; stroke: #333; stroke-width: 1; }
      .non-terminal { fill: #e8f4fd; stroke: #1e90ff; stroke-width: 1; }
      .connection { stroke: #333; stroke-width: 2; fill: none; }
    </style>
  </defs>
  
  <!-- Background -->
  <rect width="100%" height="100%" fill="${backgroundColor}"/>
  
  <!-- Title -->
  <text x="${width/2}" y="40" text-anchor="middle" class="diagram-title" fill="${textColor}">
    ${diagram.name || 'Railroad Diagram'}
  </text>
  
  <!-- Basic diagram representation -->
  ${this.renderBasicElements(diagram, width, height)}
  
  <!-- Metadata -->
  <text x="20" y="${height - 20}" font-size="10" fill="#666">
    Generated by Minotaur • Elements: ${diagram.elements?.length || 0} • ${new Date().toLocaleDateString()}
  </text>
</svg>`;

    return {
      success: true,
      content: svgContent,
      format: 'svg',
      metadata: {
        size: { width, height },
        elementCount: diagram.elements?.length || 0,
        connectionCount: diagram.connections?.length || 0,
        generationTime: Date.now(),
      },
      filename: `${diagram.name || 'railroad-diagram'}.svg`,
      mimeType: 'image/svg+xml',
    };
  }

  /**
   * Render basic diagram elements for fallback SVG
   * @param {Object} diagram - The railroad diagram data
   * @param {number} width - SVG width
   * @param {number} height - SVG height
   * @returns {string} SVG elements
   */
  renderBasicElements(diagram, width, height) {
    if (!diagram.elements || diagram.elements.length === 0) {
      return `<text x="${width/2}" y="${height/2}" text-anchor="middle" class="diagram-element" fill="#999">
        No diagram elements to display
      </text>`;
    }

    let elementsHTML = '';
    const centerY = height / 2;
    const elementsWidth = width - 100;
    const elementSpacing = elementsWidth / (diagram.elements.length + 1);

    diagram.elements.forEach((element, index) => {
      const x = 50 + elementSpacing * (index + 1);
      const y = centerY;

      if (element.type === 'terminal') {
        elementsHTML += `
          <rect x="${x - 40}" y="${y - 15}" width="80" height="30" rx="15" class="terminal"/>
          <text x="${x}" y="${y + 5}" text-anchor="middle" class="diagram-element">${element.content || element.name || 'terminal'}</text>
        `;
      } else if (element.type === 'non-terminal') {
        elementsHTML += `
          <rect x="${x - 40}" y="${y - 15}" width="80" height="30" class="non-terminal"/>
          <text x="${x}" y="${y + 5}" text-anchor="middle" class="diagram-element" font-style="italic">${element.name || 'non-terminal'}</text>
        `;
      } else {
        elementsHTML += `
          <circle cx="${x}" cy="${y}" r="15" fill="#fff" stroke="#333" stroke-width="1"/>
          <text x="${x}" y="${y + 4}" text-anchor="middle" class="diagram-element" font-size="10">${element.type || '?'}</text>
        `;
      }

      // Add connection line to next element
      if (index < diagram.elements.length - 1) {
        const nextX = 50 + elementSpacing * (index + 2);
        elementsHTML += `<line x1="${x + 40}" y1="${y}" x2="${nextX - 40}" y2="${y}" class="connection"/>`;
      }
    });

    return elementsHTML;
  }

  /**
   * Generate basic HTML as fallback
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Object} Basic HTML export result
   */
  async generateBasicHTML(diagram, options = {}) {
    const svgResult = this.generateBasicSVG(diagram, options);

    const htmlContent = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${diagram.name || 'Railroad Diagram'} - Minotaur</title>
    <style>
        body {
            margin: 0;
            padding: 20px;
            font-family: Arial, sans-serif;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        .header {
            background: #2563eb;
            color: white;
            padding: 20px;
            text-align: center;
        }
        .diagram-container {
            padding: 20px;
            text-align: center;
        }
        .metadata {
            background: #f8f9fa;
            padding: 15px;
            border-top: 1px solid #e9ecef;
            font-size: 14px;
            color: #6c757d;
        }
        svg {
            max-width: 100%;
            height: auto;
            border: 1px solid #e9ecef;
            border-radius: 4px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>${diagram.name || 'Railroad Diagram'}</h1>
            <p>Generated by Minotaur Grammar Visualization System</p>
        </div>
        
        <div class="diagram-container">
            ${svgResult.content}
        </div>
        
        <div class="metadata">
            <strong>Diagram Information:</strong><br>
            Elements: ${diagram.elements?.length || 0} • 
            Connections: ${diagram.connections?.length || 0} • 
            Generated: ${new Date().toLocaleString()}
        </div>
    </div>
</body>
</html>`;

    return {
      success: true,
      content: htmlContent,
      format: 'html',
      metadata: svgResult.metadata,
      filename: `${diagram.name || 'railroad-diagram'}.html`,
      mimeType: 'text/html',
    };
  }

  /**
   * Convert SVG string to PNG blob
   * @param {string} svgString - SVG content
   * @param {Object} options - Conversion options
   * @returns {Promise<Blob|null>} PNG blob or null if failed
   */
  async convertSVGToPNG(svgString, options = {}) {
    return new Promise((resolve) => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');

      if (!ctx) {
        resolve(null);
        return;
      }

      // Guard browser-specific code: Check for URL.createObjectURL availability
      if (typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') {
    // eslint-disable-next-line no-console
        console.warn('PNG export is not supported in this environment.');
        resolve(null);
        return;
      }

      const img = new Image();
      const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' });
      const url = URL.createObjectURL(svgBlob);

      img.onload = () => {
        // Set canvas size based on options or image dimensions
        canvas.width = options.width || img.naturalWidth || 1000;
        canvas.height = options.height || img.naturalHeight || 600;

        // Fill background if specified
        if (options.backgroundColor) {
          ctx.fillStyle = options.backgroundColor;
          ctx.fillRect(0, 0, canvas.width, canvas.height);
        }

        // Draw the SVG image
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

        // Convert to PNG blob
        canvas.toBlob((blob) => {
          URL.revokeObjectURL(url);
          resolve(blob);
        }, 'image/png', options.quality || 0.95);
      };

      img.onerror = () => {
        URL.revokeObjectURL(url);
        resolve(null);
      };

      img.src = url;
    });
  }

  /**
   * Download export result as file
   * @param {Object} exportResult - Export result from exportSVG/exportPNG/exportHTML
   */
  downloadExport(exportResult) {
    if (!exportResult.success) {
      throw new Error(exportResult.error || 'Export failed');
    }

    // Guard browser-only functionality
    if (typeof window === 'undefined' || typeof document === 'undefined' || typeof URL === 'undefined') {
      throw new Error('Download functionality is only supported in a browser environment');
    }

    let blob;
    if (exportResult.content instanceof Blob) {
      blob = exportResult.content;
    } else {
      blob = new Blob([exportResult.content], { type: exportResult.mimeType });
    }

    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = exportResult.filename;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    URL.revokeObjectURL(url);
  }

  /**
   * Copy SVG content to clipboard
   * @param {Object} diagram - The railroad diagram data
   * @param {Object} options - Export options
   * @returns {Promise<boolean>} Success status
   */
  async copyToClipboard(diagram, options = {}) {
    try {
      const svgResult = await this.exportSVG(diagram, options);

      if (svgResult.success) {
        await navigator.clipboard.writeText(svgResult.content);
        return true;
      }

      return false;
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to copy to clipboard:', error);
      return false;
    }
  }
}

// Create a singleton instance
export const exportService = new ExportService();
export default exportService;