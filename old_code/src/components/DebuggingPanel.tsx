import React, { useState } from 'react';
import { CharacterInspector } from './CharacterInspector';
import { TokenStreamViewer } from './TokenStreamViewer';
import { StateInspector } from './StateInspector';

interface DebuggingPanelProps {
  debugState: any;
  setDebugState: (state: any) => void;
  grammarCode: string;
  sourceCode: string;
}

export const DebuggingPanel: React.FC<DebuggingPanelProps> = ({
  debugState,
  setDebugState,
  grammarCode: _grammarCode,
  sourceCode,
}) => {
  const [activeView, setActiveView] = useState<'character' | 'token' | 'state'>('character');
  const [isDebugging, setIsDebugging] = useState(false);
  const [currentPosition, setCurrentPosition] = useState(0);
  const [speed, setSpeed] = useState(1); // 1 = normal, 0.5 = slow, 2 = fast

  const startDebugging = () => {
    if (!sourceCode) {
      return;
    }

    setIsDebugging(true);
    setCurrentPosition(0);

    // Initialize debug state
    const initialState = {
      position: 0,
      character: sourceCode.charAt(0),
      tokens: [],
      lexerPaths: [{ id: 0, position: 0, active: true }],
      parserPaths: [{ id: 0, lexerPathId: 0, active: true }],
      activeProductions: [],
    };

    setDebugState(initialState);
  };

  const stopDebugging = () => {
    setIsDebugging(false);
  };

  const stepForward = () => {
    if (!isDebugging || !sourceCode || currentPosition >= sourceCode.length - 1) {
      return;
    }

    const newPosition = currentPosition + 1;
    setCurrentPosition(newPosition);

    // Update debug state
    // In a real implementation, this would use the actual interpreter
    // For now, we'll create mock debug state
    const newState = {
      position: newPosition,
      character: sourceCode.charAt(newPosition),
      tokens: [...(debugState?.tokens || []),
        newPosition % 3 === 0 ? {
          type: 'identifier',
          value: sourceCode.charAt(newPosition),
          position: newPosition,
        } : null,
      ].filter(Boolean),
      lexerPaths: [
        { id: 0, position: newPosition, active: true },
        newPosition % 5 === 0 ? { id: 1, position: newPosition, active: true } : null,
      ].filter(Boolean),
      parserPaths: [
        { id: 0, lexerPathId: 0, active: true },
        newPosition % 5 === 0 ? { id: 1, lexerPathId: 1, active: true } : null,
      ].filter(Boolean),
      activeProductions: [
        newPosition % 4 === 0 ? { name: 'expression', progress: newPosition % 3 } : null,
        newPosition % 7 === 0 ? { name: 'term', progress: newPosition % 2 } : null,
      ].filter(Boolean),
    };

    setDebugState(newState);
  };

  const stepBackward = () => {
    if (!isDebugging || currentPosition <= 0) {
      return;
    }

    const newPosition = currentPosition - 1;
    setCurrentPosition(newPosition);

    // Update debug state (simplified for demo)
    const newState = {
      ...debugState,
      position: newPosition,
      character: sourceCode.charAt(newPosition),
    };

    setDebugState(newState);
  };

  const playPause = () => {
    if (!isDebugging) {
      startDebugging();
      return;
    }

    // Toggle play/pause
    if (debugState?.playing) {
      setDebugState({ ...debugState, playing: false });
    } else {
      setDebugState({ ...debugState, playing: true });

      // Start stepping forward automatically
      const interval = setInterval(() => {
        stepForward();

        // Check if we should stop
        if (currentPosition >= sourceCode.length - 1) {
          clearInterval(interval);
          setDebugState(prev => ({ ...prev, playing: false }));
        }
      }, 1000 / speed);

      // Store interval ID in debug state
      setDebugState(prev => ({ ...prev, intervalId: interval }));
    }
  };

  const handleSpeedChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newSpeed = parseFloat(e.target.value);
    setSpeed(newSpeed);

    // If currently playing, restart with new speed
    if (debugState?.playing) {
      clearInterval(debugState.intervalId);
      setDebugState({ ...debugState, playing: false });
      playPause();
    }
  };

  return (
    <div className="debugging-panel">
      <div className="debugging-tabs">
        <button
          className={activeView === 'character' ? 'active' : ''}
          onClick={() => setActiveView('character')}
        >
          Character Inspector
        </button>
        <button
          className={activeView === 'token' ? 'active' : ''}
          onClick={() => setActiveView('token')}
        >
          Token Stream
        </button>
        <button
          className={activeView === 'state' ? 'active' : ''}
          onClick={() => setActiveView('state')}
        >
          State Inspector
        </button>
      </div>

      <div className="debugging-controls">
        <button
          onClick={startDebugging}
          disabled={isDebugging || !sourceCode}
        >
          Start
        </button>
        <button
          onClick={stopDebugging}
          disabled={!isDebugging}
        >
          Stop
        </button>
        <button
          onClick={stepBackward}
          disabled={!isDebugging || currentPosition <= 0}
        >
          Step Back
        </button>
        <button
          onClick={playPause}
          disabled={!sourceCode}
        >
          {debugState?.playing ? 'Pause' : 'Play'}
        </button>
        <button
          onClick={stepForward}
          disabled={!isDebugging || currentPosition >= sourceCode.length - 1}
        >
          Step Forward
        </button>
        <div className="speed-control">
          <label>Speed:</label>
          <input
            type="range"
            min="0.25"
            max="4"
            step="0.25"
            value={speed}
            onChange={handleSpeedChange}
          />
          <span>{speed}x</span>
        </div>
      </div>

      <div className="debugging-container">
        {activeView === 'character' && (
          <CharacterInspector
            sourceCode={sourceCode}
            currentPosition={currentPosition}
            debugState={debugState}
          />
        )}
        {activeView === 'token' && (
          <TokenStreamViewer
            tokens={debugState?.tokens || []}
            currentPosition={currentPosition}
          />
        )}
        {activeView === 'state' && (
          <StateInspector
            debugState={debugState}
            setDebugState={setDebugState}
          />
        )}
      </div>

      <div className="debugging-info">
        {isDebugging ? (
          <div className="debug-status">
            Debugging: Character {currentPosition + 1} of {sourceCode.length}
          </div>
        ) : (
          <div className="debug-status">
            Not debugging. Press Start to begin.
          </div>
        )}
      </div>
    </div>
  );
};
