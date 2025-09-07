import React from 'react';

interface CharacterInspectorProps {
  sourceCode: string;
  currentPosition: number;
  debugState: any;
}

export const CharacterInspector: React.FC<CharacterInspectorProps> = ({
  sourceCode,
  currentPosition,
  debugState,
}) => {
  if (!sourceCode) {
    return <div className="character-inspector-empty">No source code available</div>;
  }

  // Calculate the context around the current position
  const contextSize = 20;
  const start = Math.max(0, currentPosition - contextSize);
  const end = Math.min(sourceCode.length, currentPosition + contextSize + 1);
  const before = sourceCode.substring(start, currentPosition);
  const current = sourceCode.charAt(currentPosition);
  const after = sourceCode.substring(currentPosition + 1, end);

  // Get lexer and parser state information
  const lexerPaths = debugState?.lexerPaths || [];
  const parserPaths = debugState?.parserPaths || [];
  const activeProductions = debugState?.activeProductions || [];

  return (
    <div className="character-inspector">
      <div className="character-view">
        <h3>Character View</h3>
        <div className="character-context">
          <span className="before">{before}</span>
          <span className="current">{current}</span>
          <span className="after">{after}</span>
        </div>
        <div className="character-info">
          <div>Position: {currentPosition}</div>
          <div>Character: &apos;{current}&apos; (ASCII: {current ? current.charCodeAt(0) : 'N/A'})</div>
        </div>
      </div>

      <div className="lexer-state">
        <h3>Lexer State</h3>
        {lexerPaths.length > 0 ? (
          <div className="lexer-paths">
            <h4>Active Lexer Paths: {lexerPaths.length}</h4>
            <ul>
              {lexerPaths.map((path: any) => (
                <li key={`lexer-path-${path.id}`} className={path.active ? 'active' : 'inactive'}>
                  Path {path.id}: Position {path.position}
                </li>
              ))}
            </ul>
          </div>
        ) : (
          <div className="no-lexer-paths">No active lexer paths</div>
        )}
      </div>

      <div className="parser-state">
        <h3>Parser State</h3>
        {parserPaths.length > 0 ? (
          <div className="parser-paths">
            <h4>Active Parser Paths: {parserPaths.length}</h4>
            <ul>
              {parserPaths.map((path: any) => (
                <li key={`parser-path-${path.id}`} className={path.active ? 'active' : 'inactive'}>
                  Path {path.id}: Linked to Lexer Path {path.lexerPathId}
                </li>
              ))}
            </ul>
          </div>
        ) : (
          <div className="no-parser-paths">No active parser paths</div>
        )}

        {activeProductions.length > 0 ? (
          <div className="active-productions">
            <h4>Active Productions: {activeProductions.length}</h4>
            <ul>
              {activeProductions.map((prod: any, index: number) => (
                <li key={`production-${index}`}>
                  {prod.name}: Progress {prod.progress}
                </li>
              ))}
            </ul>
          </div>
        ) : (
          <div className="no-active-productions">No active productions</div>
        )}
      </div>
    </div>
  );
};
