import React, { useState } from 'react';
import { CodeEditor } from './CodeEditor';

interface EditorPanelProps {
  grammarCode: string;
  setGrammarCode: (code: string) => void;
  sourceCode: string;
  setSourceCode: (code: string) => void;
  onParse: (result: any) => void;
}

export const EditorPanel: React.FC<EditorPanelProps> = ({
  grammarCode,
  setGrammarCode,
  sourceCode,
  setSourceCode,
  onParse,
}) => {
  const [activeEditor, setActiveEditor] = useState<'grammar' | 'source'>('grammar');
  const [grammarName, setGrammarName] = useState<string>('MyGrammar');
  const [fileName, setFileName] = useState<string>('source.txt');
  const [parseError, setParseError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null);

  const handleParse = async () => {
    try {
      setParseError(null); // Clear any previous errors

      // In a real implementation, this would use the actual interpreter
      // For now, we'll create a mock parse result
      const mockResult = {
        success: true,
        tree: {
          name: 'Root',
          children: [
            {
              name: 'Production1',
              value: 'Some value',
              children: [
                { name: 'Terminal1', value: 'token1' },
                { name: 'Terminal2', value: 'token2' },
              ],
            },
          ],
        },
      };

      onParse(mockResult);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Parse error:', error);
      // Proper error handling: show error to user via UI state
      const errorMessage = error instanceof Error ? error.message : 'Parse failed. Please check your grammar and source code.';
      setParseError(errorMessage);

      // Also pass error to parent component
      onParse({
        success: false,
        error: errorMessage,
        tree: null,
      });
    }
  };

  const handleSaveGrammar = () => {
    try {
      // Proper save functionality: create downloadable file
      const blob = new Blob([grammarCode], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${grammarName}.grammar`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);

      setSaveSuccess(`Grammar saved as ${grammarName}.grammar`);
      setTimeout(() => setSaveSuccess(null), 3000);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Save grammar error:', error);
      setParseError('Failed to save grammar. Please try again.');
      setTimeout(() => setParseError(null), 3000);
    }
  };

  const handleSaveSource = () => {
    try {
      // Proper save functionality: create downloadable file
      const blob = new Blob([sourceCode], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);

      setSaveSuccess(`Source code saved as ${fileName}`);
      setTimeout(() => setSaveSuccess(null), 3000);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Save source error:', error);
      setParseError('Failed to save source code. Please try again.');
      setTimeout(() => setParseError(null), 3000);
    }
  };

  const handleLoadGrammar = () => {
    // In a real implementation, this would load from a file
    const sampleGrammar = `Grammar: ${grammarName}
TokenSplitter: Space

<expression> ::= <term> | <expression> "+" <term> | <expression> "-" <term>
<term> ::= <factor> | <term> "*" <factor> | <term> "/" <factor>
<factor> ::= <number> | "(" <expression> ")"
<number> ::= /[0-9]+/`;

    setGrammarCode(sampleGrammar);
  };

  const handleLoadSource = () => {
    // In a real implementation, this would load from a file
    const sampleSource = '42 + 10 * (5 - 3)';
    setSourceCode(sampleSource);
  };

  return (
    <div className="editor-panel">
      <div className="editor-tabs">
        <button
          className={activeEditor === 'grammar' ? 'active' : ''}
          onClick={() => setActiveEditor('grammar')}
        >
          Grammar Editor
        </button>
        <button
          className={activeEditor === 'source' ? 'active' : ''}
          onClick={() => setActiveEditor('source')}
        >
          Source Code Editor
        </button>
      </div>

      <div className="editor-container">
        {activeEditor === 'grammar' ? (
          <>
            <div className="editor-header">
              <input
                type="text"
                value={grammarName}
                onChange={(e) => setGrammarName(e.target.value)}
                placeholder="Grammar Name"
              />
              <button onClick={handleLoadGrammar}>Load Sample</button>
              <button onClick={handleSaveGrammar}>Save</button>
            </div>
            <CodeEditor
              code={grammarCode}
              language="plaintext"
              onChange={setGrammarCode}
              data-testid="grammar-editor"
            />
          </>
        ) : (
          <>
            <div className="editor-header">
              <input
                type="text"
                value={fileName}
                onChange={(e) => setFileName(e.target.value)}
                placeholder="File Name"
              />
              <button onClick={handleLoadSource}>Load Sample</button>
              <button onClick={handleSaveSource}>Save</button>
            </div>
            <CodeEditor
              code={sourceCode}
              language="plaintext"
              onChange={setSourceCode}
            />
          </>
        )}
      </div>

      {/* Status Messages */}
      {parseError && (
        <div className="error-message" style={{
          padding: '8px 12px',
          margin: '8px 0',
          backgroundColor: '#fee',
          border: '1px solid #fcc',
          borderRadius: '4px',
          color: '#c33',
          fontSize: '14px',
        }}>
          ⚠️ {parseError}
        </div>
      )}

      {saveSuccess && (
        <div className="success-message" style={{
          padding: '8px 12px',
          margin: '8px 0',
          backgroundColor: '#efe',
          border: '1px solid #cfc',
          borderRadius: '4px',
          color: '#383',
          fontSize: '14px',
        }}>
          ✅ {saveSuccess}
        </div>
      )}

      <div className="editor-actions">
        <button
          className="parse-button"
          onClick={handleParse}
          disabled={!grammarCode || !sourceCode}
        >
          Parse
        </button>
      </div>
    </div>
  );
};
