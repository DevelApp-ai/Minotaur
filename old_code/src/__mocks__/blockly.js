// Lightweight Blockly mock optimized for memory efficiency
const createMockWorkspace = () => ({
  addChangeListener: jest.fn((callback) => {
    // Immediately return to avoid storing callbacks
    return callback;
  }),
  removeChangeListener: jest.fn(),
  dispose: jest.fn(),
  clear: jest.fn(),
  render: jest.fn(),
  resize: jest.fn(),
  getAllBlocks: jest.fn(() => []),
  getBlockById: jest.fn(() => null),
  newBlock: jest.fn(() => createMockBlock()),
  // Minimal properties to avoid memory bloat
  id: 'mock-workspace',
  rendered: true,
});

const createMockBlock = () => ({
  id: 'mock-block',
  type: 'mock',
  dispose: jest.fn(),
  render: jest.fn(),
  getSvgRoot: jest.fn(() => null),
  // Minimal properties
  workspace: null,
});

const createMockField = () => ({
  getValue: jest.fn(() => ''),
  setValue: jest.fn(),
  getText: jest.fn(() => ''),
  setText: jest.fn(),
  dispose: jest.fn(),
});

// Lightweight XML mock
const mockXml = {
  domToText: jest.fn(() => '<xml></xml>'),
  textToDom: jest.fn(() => ({ nodeName: 'xml', childNodes: [] })),
  workspaceToDom: jest.fn(() => ({ nodeName: 'xml', childNodes: [] })),
  domToWorkspace: jest.fn(),
};

// Lightweight JavaScript generator mock
const mockJavaScript = {
  workspaceToCode: jest.fn(() => 'Grammar: TestGrammar\n'),
  blockToCode: jest.fn(() => ''),
  statementToCode: jest.fn(() => ''),
  valueToCode: jest.fn(() => ''),
};

// Main Blockly mock with minimal memory footprint
const Blockly = {
  inject: jest.fn((container) => {
    if (!container) {
      return null;
    }

    // Create minimal workspace
    const workspace = createMockWorkspace();

    // Simulate DOM injection without heavy operations
    if (container.innerHTML !== undefined) {
      container.innerHTML = '<div class="blockly-mock">Mock Workspace</div>';
    }

    return workspace;
  }),

  Blocks: {},
  Xml: mockXml,
  JavaScript: mockJavaScript,

  // Lightweight field constructors
  FieldTextInput: jest.fn(() => createMockField()),
  FieldDropdown: jest.fn(() => createMockField()),

  // Minimal events
  Events: {
    BLOCK_CHANGE: 'change',
    BLOCK_CREATE: 'create',
    BLOCK_DELETE: 'delete',
    BLOCK_MOVE: 'move',
  },

  // Cleanup function for tests
  __cleanup: jest.fn(() => {
    // Clear any stored references
    Blockly.Blocks = {};
  }),
};

// Prevent memory leaks by avoiding circular references
Object.defineProperty(Blockly, 'mainWorkspace', {
  get: () => null,
  set: () => {},
  configurable: true,
});

module.exports = Blockly;

