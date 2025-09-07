import React, { useState, useEffect } from 'react';
import { useCallbackInterpreter } from '../utils/useCallbackInterpreter';

interface CallbackPanelProps {
  grammarCode: string;
  sourceCode: string;
}

export const CallbackPanel: React.FC<CallbackPanelProps> = ({
  grammarCode,
  sourceCode,
}) => {
  const {
    interpreter: _interpreter,
    parseResults,
    parseContext,
    parseError,
    isLoading,
    loadGrammar,
    parseSourceCode,
    registerCallback,
  } = useCallbackInterpreter();

  const [customCallbacks, setCustomCallbacks] = useState<{ name: string, code: string }[]>([
    // eslint-disable-next-line no-console
    { name: 'log', code: '(match, context, position) => { console.log(`Match: "${match}" at position ${position}`); return match; }' },
    { name: 'collect', code: '(match, context, position) => { if (!context.collected) context.collected = []; context.collected.push(match); return match; }' },
  ]);

  const [newCallbackName, setNewCallbackName] = useState('');
  const [newCallbackCode, setNewCallbackCode] = useState('');
  const [executionResults, setExecutionResults] = useState<any[]>([]);

  // Register default callbacks
  useEffect(() => {
    customCallbacks.forEach(cb => {
      try {
        // eslint-disable-next-line no-new-func
        const callbackFn = new Function('return ' + cb.code)();
        registerCallback(cb.name, callbackFn);
      } catch (error) {
    // eslint-disable-next-line no-console
        console.error(`Error registering callback ${cb.name}:`, error);
      }
    });
  }, [customCallbacks, registerCallback]);

  // Load grammar when it changes
  useEffect(() => {
    if (grammarCode) {
      loadGrammar(grammarCode, 'grammar.grammar');
    }
  }, [grammarCode, loadGrammar]);

  const handleAddCallback = () => {
    if (!newCallbackName || !newCallbackCode) {
      return;
    }

    setCustomCallbacks([...customCallbacks, {
      name: newCallbackName,
      code: newCallbackCode,
    }]);

    try {
      // eslint-disable-next-line no-new-func
      const callbackFn = new Function('return ' + newCallbackCode)();
      registerCallback(newCallbackName, callbackFn);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error(`Error registering callback ${newCallbackName}:`, error);
    }

    setNewCallbackName('');
    setNewCallbackCode('');
  };

  const handleRemoveCallback = (index: number) => {
    setCustomCallbacks(customCallbacks.filter((_, i) => i !== index));
  };

  const handleParse = () => {
    if (sourceCode) {
      setExecutionResults([]);
      parseSourceCode(sourceCode, 'source.txt');
    }
  };

  // Update execution results when parse context changes
  useEffect(() => {
    if (parseContext) {
      const results = [];

      if (parseContext.collected) {
        results.push({
          type: 'collection',
          label: 'Collected Values',
          data: parseContext.collected,
        });
      }

      if (parseContext.variables) {
        results.push({
          type: 'variables',
          label: 'Variables',
          data: parseContext.variables,
        });
      }

      if (parseContext.logs) {
        results.push({
          type: 'logs',
          label: 'Logs',
          data: parseContext.logs,
        });
      }

      setExecutionResults(results);
    }
  }, [parseContext]);

  return (
    <div className="callback-panel">
      <div className="callback-header">
        <h2>Rule Activation Callbacks</h2>
        <p>Define callbacks that will be executed when grammar rules are activated during parsing.</p>
      </div>

      <div className="callback-definition">
        <h3>Custom Callbacks</h3>
        <div className="callback-list">
          {customCallbacks.map((callback, index) => (
            <div key={index} className="callback-item">
              <div className="callback-name">
                <strong>{callback.name}</strong>
                <button
                  className="remove-callback"
                  onClick={() => handleRemoveCallback(index)}
                >
                  Remove
                </button>
              </div>
              <pre className="callback-code">{callback.code}</pre>
            </div>
          ))}
        </div>

        <div className="add-callback">
          <h4>Add New Callback</h4>
          <div className="callback-form">
            <div className="form-group">
              <label>Name:</label>
              <input
                type="text"
                value={newCallbackName}
                onChange={(e) => setNewCallbackName(e.target.value)}
                placeholder="myCallback"
              />
            </div>
            <div className="form-group">
              <label>Function:</label>
              <textarea
                value={newCallbackCode}
                onChange={(e) => setNewCallbackCode(e.target.value)}
                placeholder="(match, context, position) => { /* your code here */ return match; }"
                rows={4}
              />
            </div>
            <button
              onClick={handleAddCallback}
              disabled={!newCallbackName || !newCallbackCode}
            >
              Add Callback
            </button>
          </div>
        </div>
      </div>

      <div className="callback-execution">
        <h3>Callback Execution</h3>
        <div className="execution-controls">
          <button
            onClick={handleParse}
            disabled={!grammarCode || !sourceCode || isLoading}
          >
            {isLoading ? 'Parsing...' : 'Parse with Callbacks'}
          </button>
        </div>

        {parseError && (
          <div className="execution-error">
            <h4>Error</h4>
            <pre>{parseError}</pre>
          </div>
        )}

        {executionResults.length > 0 ? (
          <div className="execution-results">
            <h4>Execution Results</h4>
            {executionResults.map((result, index) => (
              <div key={index} className="result-section">
                <h5>{result.label}</h5>
                {result.type === 'collection' && (
                  <ul className="collection-list">
                    {result.data.map((item: any, i: number) => (
                      <li key={i}>{JSON.stringify(item)}</li>
                    ))}
                  </ul>
                )}
                {result.type === 'variables' && (
                  <table className="variables-table">
                    <thead>
                      <tr>
                        <th>Name</th>
                        <th>Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      {Object.entries(result.data).map(([key, value]: [string, any]) => (
                        <tr key={key}>
                          <td>{key}</td>
                          <td>{JSON.stringify(value)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
                {result.type === 'logs' && (
                  <pre className="logs-output">
                    {result.data.join('\n')}
                  </pre>
                )}
              </div>
            ))}
          </div>
        ) : parseResults && (
          <div className="execution-results">
            <h4>Parse Completed</h4>
            <p>No callback results to display.</p>
          </div>
        )}
      </div>
    </div>
  );
};
