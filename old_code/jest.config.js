module.exports = {
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/src/setupTests.ts'],
  moduleNameMapper: {
    '\\.(css|less|scss|sass)$': 'identity-obj-proxy',
    '^react-monaco-editor$': '<rootDir>/src/__mocks__/react-monaco-editor.js',
    '^blockly$': '<rootDir>/src/__mocks__/blockly.js',
    '^blockly/blocks$': '<rootDir>/src/__mocks__/blockly.js',
    '^blockly/javascript$': '<rootDir>/src/__mocks__/blockly.js',
    // Add module resolution for common paths
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@jest/globals$': 'jest',
    '^@jest/test$': 'jest',
    '^@jest/testing-framework$': 'jest'
  },
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': 'babel-jest',
  },
  transformIgnorePatterns: [],
  testMatch: [
    '<rootDir>/src/**/__tests__/**/*.{js,jsx,ts,tsx}',
    '<rootDir>/src/**/*.{spec,test}.{js,jsx,ts,tsx}',
  ],
  collectCoverageFrom: [
    'src/**/*.{js,jsx,ts,tsx}',
    '!src/**/*.d.ts',
    '!src/index.tsx',
    '!src/setupTests.ts',
    '!src/**/cli/**',
    '!src/**/mcp/**',
    '!src/main.ts',
    '!src/preload.ts',
  ],
  coverageReporters: ['text'],
  testTimeout: 15000,
  verbose: true,
  maxWorkers: 1,
  detectOpenHandles: true,
  forceExit: true,
  clearMocks: true,
  resetMocks: true,
  restoreMocks: true,
  bail: false,
  cache: false,
  watchman: false,
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json', 'node'],
  // Add resolver configuration
  resolver: undefined,
  moduleDirectories: ['node_modules', 'src'],
  // Handle ES modules properly
  extensionsToTreatAsEsm: ['.ts', '.tsx'],
  globals: {
    'ts-jest': {
      useESM: false
    }
  }
};
