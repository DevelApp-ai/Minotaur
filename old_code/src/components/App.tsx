import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BlocklyPanel } from './BlocklyPanel';
import { EditorPanel } from './EditorPanel';
import { VisualizationPanel } from './VisualizationPanel';
import { DebuggingPanel } from './DebuggingPanel';
import { CallbackPanel } from './CallbackPanel';
import { ElectronIntegration } from './ElectronIntegration';
import { LanguageSelector } from './LanguageSelector/LanguageSelector';
import '../App.css';

const App: React.FC = () => {
  const { t } = useTranslation();
  const [grammarCode, setGrammarCode] = useState<string>('');
  const [sourceCode, setSourceCode] = useState<string>('');
  const [parseTree, setParseTree] = useState<any>(null);
  const [debugState, setDebugState] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<string>('editor');
  const [showElectronPanel, setShowElectronPanel] = useState<boolean>(false);

  // Check if running in Electron
  const isElectron = window.electron !== undefined;

  return (
    <div className="app-container">
      <header className="app-header">
        <div className="header-top">
          <div className="header-title">
            <h1>{t('app.title')}</h1>
            <p className="subtitle">{t('app.subtitle')}</p>
          </div>
          <div className="header-controls">
            <LanguageSelector />
            {isElectron && (
              <button
                className="electron-toggle"
                onClick={() => setShowElectronPanel(!showElectronPanel)}
              >
                {showElectronPanel ? t('desktop.hideTools') : t('desktop.showTools')}
              </button>
            )}
          </div>
        </div>
        <nav className="app-nav">
          <button
            className={activeTab === 'editor' ? 'active' : ''}
            onClick={() => setActiveTab('editor')}
          >
            {t('navigation.editor')}
          </button>
          <button
            className={activeTab === 'blockly' ? 'active' : ''}
            onClick={() => setActiveTab('blockly')}
          >
            {t('navigation.visualEditor')}
          </button>
          <button
            className={activeTab === 'callbacks' ? 'active' : ''}
            onClick={() => setActiveTab('callbacks')}
          >
            {t('navigation.callbacks')}
          </button>
          <button
            className={activeTab === 'visualization' ? 'active' : ''}
            onClick={() => setActiveTab('visualization')}
          >
            {t('navigation.visualization')}
          </button>
          <button
            className={activeTab === 'debugging' ? 'active' : ''}
            onClick={() => setActiveTab('debugging')}
          >
            {t('navigation.debugging')}
          </button>
        </nav>
      </header>

      {isElectron && showElectronPanel && (
        <div className="electron-panel">
          <ElectronIntegration
            grammarCode={grammarCode}
            setGrammarCode={setGrammarCode}
            sourceCode={sourceCode}
            setSourceCode={setSourceCode}
          />
        </div>
      )}

      <main className="app-content">
        {activeTab === 'editor' && (
          <EditorPanel
            grammarCode={grammarCode}
            setGrammarCode={setGrammarCode}
            sourceCode={sourceCode}
            setSourceCode={setSourceCode}
            onParse={(result) => {
              setParseTree(result);
              setActiveTab('visualization');
            }}
          />
        )}

        {activeTab === 'blockly' && (
          <BlocklyPanel
            grammarCode={grammarCode}
            setGrammarCode={setGrammarCode}
          />
        )}

        {activeTab === 'callbacks' && (
          <CallbackPanel
            grammarCode={grammarCode}
            sourceCode={sourceCode}
          />
        )}

        {activeTab === 'visualization' && (
          <VisualizationPanel
            parseTree={parseTree}
            grammarCode={grammarCode}
            sourceCode={sourceCode}
          />
        )}

        {activeTab === 'debugging' && (
          <DebuggingPanel
            debugState={debugState}
            setDebugState={setDebugState}
            grammarCode={grammarCode}
            sourceCode={sourceCode}
          />
        )}
      </main>

      <footer className="app-footer">
        <p>{t('app.title')} - {t('app.subtitle')}</p>
        {isElectron && <p className="electron-badge">Desktop Edition</p>}
      </footer>
    </div>
  );
};

export default App;

