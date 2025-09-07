import React from 'react';

interface StateInspectorProps {
  debugState: any;
  setDebugState: (state: any) => void;
}

export const StateInspector: React.FC<StateInspectorProps> = ({
  debugState,
  setDebugState,
}) => {
  if (!debugState) {
    return <div className="state-inspector-empty">No debug state available</div>;
  }

  // Format the debug state for display
  const formattedState = JSON.stringify(debugState, null, 2);

  // Handle state modification
  const handleStateChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    try {
      const newState = JSON.parse(e.target.value);
      setDebugState(newState);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Invalid JSON:', error);
      // Don't update state if JSON is invalid
    }
  };

  return (
    <div className="state-inspector">
      <h3>Interpreter State</h3>

      <div className="state-view">
        <div className="state-summary">
          <div className="state-item">
            <span className="state-label">Position:</span>
            <span className="state-value">{debugState.position}</span>
          </div>
          <div className="state-item">
            <span className="state-label">Current Character:</span>
            <span className="state-value">&apos;{debugState.character}&apos;</span>
          </div>
          <div className="state-item">
            <span className="state-label">Lexer Paths:</span>
            <span className="state-value">{debugState.lexerPaths?.length || 0}</span>
          </div>
          <div className="state-item">
            <span className="state-label">Parser Paths:</span>
            <span className="state-value">{debugState.parserPaths?.length || 0}</span>
          </div>
          <div className="state-item">
            <span className="state-label">Tokens:</span>
            <span className="state-value">{debugState.tokens?.length || 0}</span>
          </div>
        </div>

        <div className="state-editor">
          <h4>Raw State (Editable)</h4>
          <textarea
            value={formattedState}
            onChange={handleStateChange}
            rows={20}
            cols={80}
          />
          <p className="state-editor-help">
            Edit the state directly (must be valid JSON). Changes will be applied immediately.
          </p>
        </div>
      </div>
    </div>
  );
};
