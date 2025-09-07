import React from 'react';
import MonacoEditor from 'react-monaco-editor';

interface EditorProps {
  code: string;
  language: string;
  onChange: (value: string) => void;
  options?: any;
  'data-testid'?: string;
}

export const CodeEditor: React.FC<EditorProps> = ({
  code,
  language,
  onChange,
  options = {},
  'data-testid': testId,
}) => {
  const defaultOptions = {
    selectOnLineNumbers: true,
    roundedSelection: false,
    readOnly: false,
    cursorStyle: 'line',
    automaticLayout: true,
    minimap: { enabled: true },
    ...options,
  };

  const handleEditorChange = (value: string) => {
    onChange(value);
  };

  return (
    <div className="code-editor" data-testid={testId}>
      <MonacoEditor
        width="100%"
        height="500px"
        language={language}
        theme="vs-dark"
        value={code}
        options={defaultOptions}
        onChange={handleEditorChange}
      />
    </div>
  );
};
