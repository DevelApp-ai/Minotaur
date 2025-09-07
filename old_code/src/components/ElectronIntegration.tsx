import React, { useState, useEffect } from 'react';
import { useCallbackInterpreter } from '../utils/useCallbackInterpreter';

interface ElectronIntegrationProps {
  grammarCode: string;
  setGrammarCode: (code: string) => void;
  sourceCode: string;
  setSourceCode: (code: string) => void;
}

export const ElectronIntegration: React.FC<ElectronIntegrationProps> = ({
  grammarCode,
  setGrammarCode,
  sourceCode,
  setSourceCode,
}) => {
  const [appVersion, setAppVersion] = useState<string>('');
  const [exportStatus, setExportStatus] = useState<string>('');
  const [isElectron, setIsElectron] = useState<boolean>(false);

  // Check if running in Electron
  useEffect(() => {
    setIsElectron(window.electron !== undefined);
    if (window.electron) {
      setAppVersion(window.electron.app.getVersion());
    }
  }, []);

  const handleSaveGrammar = async () => {
    if (!isElectron || !grammarCode) {
      return;
    }

    try {
      const result = await window.electron.fileSystem.saveFile(
        grammarCode,
        'grammar.grammar',
        [{ name: 'Grammar Files', extensions: ['grammar'] }],
      );

      if (result.success) {
        setExportStatus(`Grammar saved to ${result.filePath}`);
      }
    } catch (error) {
      setExportStatus(`Error saving grammar: ${error}`);
    }
  };

  const handleOpenGrammar = async () => {
    if (!isElectron) {
      return;
    }

    try {
      const result = await window.electron.fileSystem.openFile(
        undefined,
        [{ name: 'Grammar Files', extensions: ['grammar'] }],
      );

      if (result.success && result.content) {
        setGrammarCode(result.content);
        setExportStatus(`Grammar loaded from ${result.filePath}`);
      }
    } catch (error) {
      setExportStatus(`Error opening grammar: ${error}`);
    }
  };

  const handleSaveSource = async () => {
    if (!isElectron || !sourceCode) {
      return;
    }

    try {
      const result = await window.electron.fileSystem.saveFile(
        sourceCode,
        'source.txt',
        [{ name: 'Text Files', extensions: ['txt'] }],
      );

      if (result.success) {
        setExportStatus(`Source code saved to ${result.filePath}`);
      }
    } catch (error) {
      setExportStatus(`Error saving source code: ${error}`);
    }
  };

  const handleOpenSource = async () => {
    if (!isElectron) {
      return;
    }

    try {
      const result = await window.electron.fileSystem.openFile(
        undefined,
        [{ name: 'Text Files', extensions: ['txt'] }],
      );

      if (result.success && result.content) {
        setSourceCode(result.content);
        setExportStatus(`Source code loaded from ${result.filePath}`);
      }
    } catch (error) {
      setExportStatus(`Error opening source code: ${error}`);
    }
  };

  const handleExportParser = async () => {
    if (!isElectron || !grammarCode) {
      return;
    }

    try {
      // Parse grammar to create a grammar object
      // In a real implementation, this would use the actual interpreter
      const grammarObject = {
        name: 'ExportedGrammar',
        rules: grammarCode.split('\n').filter(line => line.includes('::=')),
        source: grammarCode,
      };

      const result = await window.electron.parser.exportParser(grammarObject);

      if (result.success) {
        setExportStatus(`Parser exported to ${result.filePath}`);
      } else if (result.error) {
        setExportStatus(`Error exporting parser: ${result.error}`);
      }
    } catch (error) {
      setExportStatus(`Error exporting parser: ${error}`);
    }
  };

  if (!isElectron) {
    return (
      <div className="electron-integration electron-unavailable">
        <h3>Desktop Features</h3>
        <p>Desktop features are only available in the Electron application.</p>
        <p>You are currently running in a web browser.</p>
      </div>
    );
  }

  return (
    <div className="electron-integration">
      <h3>Desktop Integration</h3>
      <p>DSL Designer v{appVersion}</p>

      <div className="electron-actions">
        <div className="action-group">
          <h4>Grammar Files</h4>
          <div className="button-group">
            <button onClick={handleOpenGrammar}>Open Grammar</button>
            <button
              onClick={handleSaveGrammar}
              disabled={!grammarCode}
            >
              Save Grammar
            </button>
          </div>
        </div>

        <div className="action-group">
          <h4>Source Code Files</h4>
          <div className="button-group">
            <button onClick={handleOpenSource}>Open Source</button>
            <button
              onClick={handleSaveSource}
              disabled={!sourceCode}
            >
              Save Source
            </button>
          </div>
        </div>

        <div className="action-group">
          <h4>Parser Export</h4>
          <div className="button-group">
            <button
              onClick={handleExportParser}
              disabled={!grammarCode}
            >
              Export Parser
            </button>
          </div>
          <p className="export-help">
            Exports the current grammar as a standalone parser module.
          </p>
        </div>
      </div>

      {exportStatus && (
        <div className="export-status">
          <p>{exportStatus}</p>
        </div>
      )}
    </div>
  );
};
