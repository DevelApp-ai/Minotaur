import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BlocklyPanel } from './BlocklyPanel';

// Mock the child components
jest.mock('./BlocklyEditor', () => ({
  BlocklyEditor: ({ onChange }: { onChange: (xml: string, code: string) => void }) => (
    <div data-testid="blockly-editor">
      <button onClick={() => onChange('<xml></xml>', 'generated code')}>
        Generate Code
      </button>
    </div>
  ),
}));

jest.mock('./CodeEditor', () => ({
  CodeEditor: ({ code, onChange }: { code: string; onChange: (code: string) => void }) => (
    <div data-testid="code-editor">
      <textarea
        data-testid="code-textarea"
        value={code}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  ),
}));

// Mock Blockly before importing the component
jest.mock('blockly', () => ({
  inject: jest.fn(() => ({
    addChangeListener: jest.fn(),
    dispose: jest.fn(),
    clear: jest.fn(),
    getTopBlocks: jest.fn(() => []),
  })),
  Xml: {
    workspaceToDom: jest.fn(() => ({
      outerHTML: '<xml></xml>',
    })),
    domToWorkspace: jest.fn(),
  },
  JavaScript: {
    workspaceToCode: jest.fn(() => 'generated code'),
  },
  Python: {
    workspaceToCode: jest.fn(() => 'generated python code'),
  },
}));

describe('BlocklyPanel', () => {
  const mockSetGrammarCode = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    // Reset DOM
    document.body.innerHTML = '';
  });

  const defaultProps = {
    grammarCode: '',
    setGrammarCode: mockSetGrammarCode,
  };

  test('renders Blockly workspace container', () => {
    render(<BlocklyPanel {...defaultProps} />);

    const blocklyEditor = screen.getByTestId('blockly-editor');
    expect(blocklyEditor).toBeInTheDocument();
  });

  test('generates code when generate button is clicked', async () => {
    const user = userEvent.setup();
    render(<BlocklyPanel {...defaultProps} />);

    const generateButton = screen.getByText('Generate Code');
    await user.click(generateButton);

    expect(mockSetGrammarCode).toHaveBeenCalledWith('generated code');
  });

  test('renders code editor', () => {
    render(<BlocklyPanel {...defaultProps} />);

    const codeEditor = screen.getByTestId('code-editor');
    expect(codeEditor).toBeInTheDocument();
  });

  test('updates code editor when grammar code changes', () => {
    const grammarCode = 'test grammar code';
    render(<BlocklyPanel {...defaultProps} grammarCode={grammarCode} />);

    const codeTextarea = screen.getByTestId('code-textarea');
    expect(codeTextarea).toHaveValue(grammarCode);
  });

  test('handles code editor changes', async () => {
    const user = userEvent.setup();
    const mockSetGrammarCode = jest.fn();
    render(<BlocklyPanel {...defaultProps} setGrammarCode={mockSetGrammarCode} />);

    const codeTextarea = screen.getByTestId('code-textarea');
    await user.clear(codeTextarea);
    await user.type(codeTextarea, 'test');

    // Check that the callback was called (simplified test)
    expect(mockSetGrammarCode).toHaveBeenCalled();
  });
});

