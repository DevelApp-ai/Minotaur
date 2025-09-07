/**
 * SVG Export Demo
 *
 * This demo shows the SVG export functionality in action.
 */

import { ExportService } from '../services/exportService.js';

// Create a demo diagram
const demoGrammar = {
  name: 'Simple Expression Grammar',
  bounds: { width: 1000, height: 400 },
  elements: [
    {
      id: 'start',
      type: 'start',
      name: 'START',
      bounds: { x: 50, y: 180, width: 80, height: 40 },
    },
    {
      id: 'expression',
      type: 'non-terminal',
      name: 'expression',
      bounds: { x: 200, y: 180, width: 120, height: 40 },
    },
    {
      id: 'term',
      type: 'non-terminal',
      name: 'term',
      bounds: { x: 380, y: 180, width: 80, height: 40 },
    },
    {
      id: 'plus',
      type: 'terminal',
      name: 'operator',
      content: '+',
      bounds: { x: 520, y: 180, width: 60, height: 40 },
    },
    {
      id: 'number',
      type: 'terminal',
      name: 'number',
      content: '[0-9]+',
      bounds: { x: 640, y: 180, width: 100, height: 40 },
    },
    {
      id: 'end',
      type: 'end',
      name: 'END',
      bounds: { x: 800, y: 180, width: 80, height: 40 },
    },
  ],
  connections: [
    {
      id: 'conn1',
      from: { element: { id: 'start' }, point: { x: 130, y: 200 } },
      to: { element: { id: 'expression' }, point: { x: 200, y: 200 } },
      type: 'normal',
    },
    {
      id: 'conn2',
      from: { element: { id: 'expression' }, point: { x: 320, y: 200 } },
      to: { element: { id: 'term' }, point: { x: 380, y: 200 } },
      type: 'normal',
    },
    {
      id: 'conn3',
      from: { element: { id: 'term' }, point: { x: 460, y: 200 } },
      to: { element: { id: 'plus' }, point: { x: 520, y: 200 } },
      type: 'normal',
    },
    {
      id: 'conn4',
      from: { element: { id: 'plus' }, point: { x: 580, y: 200 } },
      to: { element: { id: 'number' }, point: { x: 640, y: 200 } },
      type: 'normal',
    },
    {
      id: 'conn5',
      from: { element: { id: 'number' }, point: { x: 740, y: 200 } },
      to: { element: { id: 'end' }, point: { x: 800, y: 200 } },
      type: 'normal',
    },
  ],
};

// Demo function
async function demonstrateSVGExport() {
    // eslint-disable-next-line no-console
  console.log('=== SVG Export Demo ===\n');

  const exportService = new ExportService();

  // Wait a bit for backend initialization to complete
  await new Promise(resolve => setTimeout(resolve, 100));

    // eslint-disable-next-line no-console
  console.log('1. Exporting as SVG...');
  const svgResult = await exportService.exportSVG(demoGrammar);

  if (svgResult.success) {
    // eslint-disable-next-line no-console
    console.log('✅ SVG Export successful!');
    // eslint-disable-next-line no-console
    console.log(`   Format: ${svgResult.format}`);
    // eslint-disable-next-line no-console
    console.log(`   Filename: ${svgResult.filename}`);
    // eslint-disable-next-line no-console
    console.log(`   Size: ${svgResult.metadata.size.width}x${svgResult.metadata.size.height}`);
    // eslint-disable-next-line no-console
    console.log(`   Elements: ${svgResult.metadata.elementCount}`);
    // eslint-disable-next-line no-console
    console.log(`   Content length: ${svgResult.content.length} characters`);
    // eslint-disable-next-line no-console
    console.log(`   First 200 chars: ${svgResult.content.substring(0, 200)}...`);
  } else {
    // eslint-disable-next-line no-console
    console.log('❌ SVG Export failed:', svgResult.error);
  }

    // eslint-disable-next-line no-console
  console.log('\n2. Exporting as HTML...');
  const htmlResult = await exportService.exportHTML(demoGrammar);

  if (htmlResult.success) {
    // eslint-disable-next-line no-console
    console.log('✅ HTML Export successful!');
    // eslint-disable-next-line no-console
    console.log(`   Format: ${htmlResult.format}`);
    // eslint-disable-next-line no-console
    console.log(`   Filename: ${htmlResult.filename}`);
    // eslint-disable-next-line no-console
    console.log(`   Content length: ${htmlResult.content.length} characters`);
  } else {
    // eslint-disable-next-line no-console
    console.log('❌ HTML Export failed:', htmlResult.error);
  }

    // eslint-disable-next-line no-console
  console.log('\n3. Testing PNG export...');
  try {
    const pngResult = await exportService.exportPNG(demoGrammar);

    if (pngResult.success) {
    // eslint-disable-next-line no-console
      console.log('✅ PNG Export successful!');
    // eslint-disable-next-line no-console
      console.log(`   Format: ${pngResult.format}`);
    // eslint-disable-next-line no-console
      console.log(`   Filename: ${pngResult.filename}`);
    } else {
    // eslint-disable-next-line no-console
      console.log('⚠️  PNG Export failed (expected in Node.js environment):', pngResult.error);
    }
  } catch (error) {
    // eslint-disable-next-line no-console
    console.log('⚠️  PNG Export failed (expected in Node.js environment):', error.message);
  }

    // eslint-disable-next-line no-console
  console.log('\n=== Demo Complete ===');

  return {
    svgResult,
    htmlResult,
  };
}

// Run demo if this file is executed directly
if (typeof require !== 'undefined' && require.main === module) {
  demonstrateSVGExport();
}

export { demonstrateSVGExport, demoGrammar };