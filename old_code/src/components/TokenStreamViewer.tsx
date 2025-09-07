import React from 'react';

interface TokenStreamViewerProps {
  tokens: any[];
  currentPosition: number;
}

export const TokenStreamViewer: React.FC<TokenStreamViewerProps> = ({
  tokens,
  currentPosition,
}) => {
  if (!tokens || tokens.length === 0) {
    return <div className="token-stream-empty">No tokens available</div>;
  }

  return (
    <div className="token-stream-viewer">
      <h3>Token Stream</h3>

      <div className="token-list">
        <table>
          <thead>
            <tr>
              <th>Index</th>
              <th>Type</th>
              <th>Value</th>
              <th>Position</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {tokens.map((token, index) => (
              <tr
                key={`token-${index}`}
                className={token.position === currentPosition ? 'current-token' : ''}
              >
                <td>{index}</td>
                <td>{token.type}</td>
                <td>&quot;{token.value}&quot;</td>
                <td>{token.position}</td>
                <td>
                  {token.position < currentPosition ? 'Processed' :
                    token.position === currentPosition ? 'Current' : 'Pending'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="token-stats">
        <p>Total Tokens: {tokens.length}</p>
        <p>Current Position: {currentPosition}</p>
      </div>
    </div>
  );
};
