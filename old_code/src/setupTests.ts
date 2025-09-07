// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';
import React from 'react';

// Canvas support for PNG export tests
try {
  const { createCanvas, Image } = require('canvas');

  // Patch global objects for jsdom
  if (typeof global.HTMLCanvasElement !== 'undefined') {
    global.HTMLCanvasElement.prototype.getContext = function getContext(type: string) {
      // Provide a real canvas context using node-canvas
      const canvas = createCanvas(this.width || 300, this.height || 150);
      return canvas.getContext(type);
    };
  }

  if (typeof global.Image === 'undefined') {
    global.Image = Image;
  }
} catch (error) {
  // Canvas not available, skip configuration
    // eslint-disable-next-line no-console
  console.warn('Canvas package not available for PNG export tests');
}

// Add TextEncoder and TextDecoder polyfills for Node.js environment
if (typeof global.TextEncoder === 'undefined') {
  const { TextEncoder, TextDecoder } = require('util');
  global.TextEncoder = TextEncoder;
  global.TextDecoder = TextDecoder;
}

// Memory management for tests
beforeEach(() => {
  // Log memory checkpoint if available
  if (global.logMemoryCheckpoint) {
    global.logMemoryCheckpoint('test-start');
  }

  // Force garbage collection if available
  if (global.gc) {
    global.gc();
  }

  // Clear DOM to prevent memory accumulation (only if in browser-like environment)
  if (typeof document !== 'undefined' && document.body) {
    document.body.innerHTML = '';
  }

  // Reset any global state
  if (typeof window !== 'undefined') {
    if (window.localStorage) {
      window.localStorage.clear();
    }
    if (window.sessionStorage) {
      window.sessionStorage.clear();
    }
  }
});

afterEach(() => {
  // Log memory checkpoint if available
  if (global.logMemoryCheckpoint) {
    global.logMemoryCheckpoint('test-end');
  }

  // Clean up DOM (only if in browser-like environment)
  if (typeof document !== 'undefined' && document.body) {
    document.body.innerHTML = '';
  }

  // Clear any timers
  jest.clearAllTimers();

  // Force garbage collection if available
  if (global.gc) {
    global.gc();
  }
});

// Mock react-i18next to fix translation warnings
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      // Return proper translations for test expectations
      const translations: { [key: string]: string } = {
        'app.title': 'DSL Designer',
        'app.subtitle': 'Advanced Grammar Development Environment',
        'tabs.editor': 'Editor',
        'tabs.blockly': 'Blockly',
        'tabs.visualization': 'Visualization',
        'tabs.debugging': 'Debugging',
        'tabs.callbacks': 'Callbacks',
        'navigation.editor': 'Editor',
        'navigation.visualEditor': 'Visual Editor',
        'navigation.callbacks': 'Callbacks',
        'navigation.visualization': 'Visualization',
        'navigation.debugging': 'Debugging',
        'electron.toggle': 'Toggle Electron Panel',
        'electron.panel.title': 'Electron Integration',
        'desktop.showTools': 'Show Desktop Tools',
        'desktop.hideTools': 'Hide Desktop Tools',
      };
      return translations[key] || key;
    },
    i18n: {
      changeLanguage: jest.fn(),
    },
  }),
  initReactI18next: {
    type: '3rdParty',
    init: jest.fn(),
  },
}));

// Mock window.electron for Electron integration tests
Object.defineProperty(window, 'electron', {
  value: {
    ipcRenderer: {
      invoke: jest.fn(),
      on: jest.fn(),
      removeAllListeners: jest.fn(),
    },
  },
  writable: true,
});

// Mock react-monaco-editor to fix ES module import issues
jest.mock('react-monaco-editor', () => {
  const mockReact = require('react');
  return {
    __esModule: true,
    default: jest.fn(({ value, onChange, ...props }) => {
      return mockReact.createElement('div', {
        'data-testid': 'monaco-editor',
        'data-value': value,
        onClick: () => onChange && onChange('test code'),
        ...props,
      });
    }),
  };
}, { virtual: true });

// Create virtual mocks for external dependencies
jest.mock('monaco-editor', () => ({
  editor: {
    create: jest.fn(() => ({
      dispose: jest.fn(),
      getValue: jest.fn(() => ''),
      setValue: jest.fn(),
      onDidChangeModelContent: jest.fn(),
      getModel: jest.fn(() => ({
        onDidChangeContent: jest.fn(),
      })),
    })),
    defineTheme: jest.fn(),
    setTheme: jest.fn(),
  },
  languages: {
    register: jest.fn(),
    setMonarchTokensProvider: jest.fn(),
  },
}), { virtual: true });

jest.mock('blockly', () => ({
  inject: jest.fn(() => ({
    dispose: jest.fn(),
    clear: jest.fn(),
    getToolbox: jest.fn(),
    addChangeListener: jest.fn(),
    removeChangeListener: jest.fn(),
    getAllBlocks: jest.fn(() => []),
    getBlockById: jest.fn(),
    newBlock: jest.fn(),
    undo: jest.fn(),
    redo: jest.fn(),
    resize: jest.fn(),
  })),
  Xml: {
    domToText: jest.fn(() => '<xml></xml>'),
    textToDom: jest.fn(() => ({ tagName: 'xml' })),
    domToWorkspace: jest.fn(),
    workspaceToDom: jest.fn(() => ({ tagName: 'xml' })),
  },
  JavaScript: {
    workspaceToCode: jest.fn(() => 'generated code'),
  },
  Python: {
    workspaceToCode: jest.fn(() => 'generated python code'),
  },
}), { virtual: true });

jest.mock('d3', () => ({
  select: jest.fn(() => ({
    selectAll: jest.fn(() => ({
      data: jest.fn(() => ({
        enter: jest.fn(() => ({
          append: jest.fn(() => ({
            attr: jest.fn(() => ({
              text: jest.fn(),
            })),
          })),
        })),
      })),
    })),
  })),
}), { virtual: true });

// Optimize React Testing Library cleanup
import { cleanup } from '@testing-library/react';

// Ensure cleanup happens after each test
afterEach(() => {
  cleanup();
});

// Mock performance API to reduce memory usage
Object.defineProperty(window, 'performance', {
  value: {
    now: () => Date.now(),
    mark: () => {},
    measure: () => {},
    getEntriesByType: () => [],
    getEntriesByName: () => [],
  },
  writable: true,
});

// Mock IntersectionObserver to prevent memory leaks
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Mock ResizeObserver to prevent memory leaks
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Global error handler to prevent memory leaks from unhandled errors
    // eslint-disable-next-line no-console
const originalError = console.error;
    // eslint-disable-next-line no-console
console.error = (...args) => {
  // TEMPORARILY DISABLED: Filter out known non-critical warnings that can cause memory issues
  // const message = args[0];
  // if (typeof message === 'string') {
  //   if (message.includes('Warning: ReactDOM.render is deprecated') ||
  //       message.includes('Warning: componentWillReceiveProps') ||
  //       message.includes('punycode module is deprecated')) {
  //     return; // Suppress these warnings
  //   }
  // }
  originalError.apply(console, args);
};

