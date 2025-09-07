import React, { useState } from 'react';
import { BlocklyEditor } from './BlocklyEditor';
import { CodeEditor } from './CodeEditor';

interface BlocklyPanelProps {
  grammarCode: string;
  setGrammarCode: (code: string) => void;
}

export const BlocklyPanel: React.FC<BlocklyPanelProps> = ({
  grammarCode,
  setGrammarCode,
}) => {
  const [blocklyXml, setBlocklyXml] = useState<string>('');
  const [generatedCode, setGeneratedCode] = useState<string>('');
  const [syncDirection, setSyncDirection] = useState<'blockly-to-code' | 'code-to-blockly'>('blockly-to-code');
  const [isConverting, setIsConverting] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Handle changes from Blockly with error handling
  const handleBlocklyChange = (xml: string, code: string) => {
    try {
      setError(null);
      setBlocklyXml(xml || '');
      setGeneratedCode(code || '');

      if (syncDirection === 'blockly-to-code' && !isConverting && setGrammarCode) {
        setGrammarCode(code || '');
      }
    } catch (err) {
      setError(`Blockly change error: ${err instanceof Error ? err.message : 'Unknown error'}`);
    }
  };

  // Handle changes from code editor with error handling
  const handleCodeChange = (code: string) => {
    try {
      setError(null);
      if (setGrammarCode) {
        setGrammarCode(code || '');
      }

      if (syncDirection === 'code-to-blockly' && !isConverting) {
        // This would require a parser to convert grammar code to Blockly XML
        // For now, we'll just show a message
    // eslint-disable-next-line no-console
        console.log('Converting code to Blockly is not fully implemented yet');
      }
    } catch (err) {
      setError(`Code change error: ${err instanceof Error ? err.message : 'Unknown error'}`);
    }
  };

  // Convert from code to Blockly with error handling
  const convertCodeToBlockly = () => {
    try {
      setError(null);
      setIsConverting(true);
      setSyncDirection('code-to-blockly');

      // In a real implementation, this would parse the grammar code and generate Blockly XML
      // For now, we'll create a simple example XML
      const sampleXml = createSampleBlocklyXml(grammarCode || '');
      setBlocklyXml(sampleXml);

      setIsConverting(false);
    } catch (err) {
      setError(`Conversion error: ${err instanceof Error ? err.message : 'Unknown error'}`);
      setIsConverting(false);
    }
  };

  // Convert from Blockly to code with error handling
  const convertBlocklyToCode = () => {
    try {
      setError(null);
      setIsConverting(true);
      setSyncDirection('blockly-to-code');
      if (setGrammarCode) {
        setGrammarCode(generatedCode || '');
      }
      setIsConverting(false);
    } catch (err) {
      setError(`Conversion error: ${err instanceof Error ? err.message : 'Unknown error'}`);
      setIsConverting(false);
    }
  };

  // Create sample Blockly XML from grammar code with error handling
  const createSampleBlocklyXml = (code: string): string => {
    try {
      // This is a simplified example - a real implementation would parse the grammar
      const lines = (code || '').split('\n');
      let grammarName = 'MyGrammar';
      let tokenSplitter = 'None';

      // Extract grammar name
      for (const line of lines) {
        if (line && line.startsWith('Grammar:')) {
          grammarName = line.substring('Grammar:'.length).trim() || 'MyGrammar';
          break;
        }
      }

      // Extract token splitter
      for (const line of lines) {
        if (line && line.startsWith('TokenSplitter:')) {
          tokenSplitter = line.substring('TokenSplitter:'.length).trim() || 'None';
          break;
        }
      }

      // Create a simple XML with just the grammar block
      return `
        <xml xmlns="https://developers.google.com/blockly/xml">
          <block type="grammar" x="10" y="10">
            <field name="NAME">${grammarName}</field>
            <field name="SPLITTER">${tokenSplitter}</field>
            <field name="REGEX"></field>
          </block>
        </xml>
      `;
    } catch (err) {
    // eslint-disable-next-line no-console
      console.error('Error creating Blockly XML:', err);
      return '<xml xmlns="https://developers.google.com/blockly/xml"></xml>';
    }
  };

  if (error) {
    return (
      <div className="blockly-panel error">
        <h2>Visual Grammar Editor</h2>
        <div className="error-message">
          <p>Error: {error}</p>
          <button onClick={() => setError(null)}>Retry</button>
        </div>
      </div>
    );
  }

  return (
    <div className="blockly-panel">
      <div className="blockly-controls">
        <h2>Visual Grammar Editor</h2>
        <div className="sync-controls">
          <button
            onClick={convertCodeToBlockly}
            disabled={syncDirection === 'code-to-blockly' || !grammarCode || isConverting}
          >
            {isConverting ? 'Converting...' : 'Convert Code to Blocks'}
          </button>
          <button
            onClick={convertBlocklyToCode}
            disabled={syncDirection === 'blockly-to-code' || !blocklyXml || isConverting}
          >
            {isConverting ? 'Converting...' : 'Update Code from Blocks'}
          </button>
          <div className="sync-direction">
            Current sync: {syncDirection === 'blockly-to-code' ? 'Blocks → Code' : 'Code → Blocks'}
          </div>
        </div>
      </div>

      <div className="blockly-workspace-container">
        <BlocklyEditor
          initialXml={blocklyXml}
          onChange={handleBlocklyChange}
        />
      </div>

      <div className="blockly-code-preview">
        <h3>Generated Grammar Code</h3>
        <CodeEditor
          code={grammarCode || ''}
          language="plaintext"
          onChange={handleCodeChange}
          options={{ readOnly: syncDirection === 'blockly-to-code' }}
        />
      </div>
    </div>
  );
};
