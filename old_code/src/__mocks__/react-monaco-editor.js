import React from 'react';

// Lightweight Monaco Editor mock optimized for memory efficiency
const MonacoEditor = React.memo(({ value: _value, onChange, language, _theme, _options, ...props }) => {
  // Use minimal DOM structure to reduce memory footprint
  return React.createElement('div', {
    'data-testid': 'monaco-editor',
    'data-language': language || 'text',
    onClick: () => onChange && onChange('test code'),
    style: {
      width: '100%',
      height: '400px',
      border: '1px solid #ccc',
      backgroundColor: '#f5f5f5',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: 'monospace',
      fontSize: '14px',
      color: '#666',
    },
    ...props,
  }, `Monaco Editor Mock (${language || 'text'})`);
});

MonacoEditor.displayName = 'MonacoEditor';

export default MonacoEditor;

