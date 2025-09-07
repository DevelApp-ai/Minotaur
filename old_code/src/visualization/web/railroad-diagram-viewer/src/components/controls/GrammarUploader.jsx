import { useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Upload,
  FileText,
  Code,
  Check,
  X,
  AlertCircle,
  Loader2,
  File,
  Trash2,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Textarea } from '@/components/ui/textarea.jsx';
import { Label } from '@/components/ui/label.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Card, CardContent } from '@/components/ui/card.jsx';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs.jsx';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select.jsx';

const GrammarUploader = ({ onGrammarUpload }) => {
  const [uploadMethod, setUploadMethod] = useState('file');
  const [grammarText, setGrammarText] = useState('');
  const [grammarFormat, setGrammarFormat] = useState('grammarforge');
  const [uploadedFile, setUploadedFile] = useState(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const fileInputRef = useRef(null);

  const grammarFormats = [
    { id: 'grammarforge', name: 'GrammarForge', extension: '.gf' },
    { id: 'antlr4', name: 'ANTLR v4', extension: '.g4' },
    { id: 'bison', name: 'Bison/Yacc', extension: '.y' },
    { id: 'flex', name: 'Flex/Lex', extension: '.l' },
    { id: 'ebnf', name: 'EBNF', extension: '.ebnf' },
    { id: 'bnf', name: 'BNF', extension: '.bnf' },
  ];

  const sampleGrammars = {
    grammarforge: `grammar Calculator inherits BaseGrammar {
  start: expression;
  
  expression: 
    | term (('+' | '-') term)*
    | '(' expression ')'
    ;
    
  term:
    | factor (('*' | '/') factor)*
    ;
    
  factor:
    | NUMBER
    | IDENTIFIER
    | '(' expression ')'
    ;
}`,
    antlr4: `grammar Calculator;

expression
    : term (('+' | '-') term)*
    | '(' expression ')'
    ;

term
    : factor (('*' | '/') factor)*
    ;

factor
    : NUMBER
    | IDENTIFIER
    | '(' expression ')'
    ;

NUMBER : [0-9]+ ('.' [0-9]+)? ;
IDENTIFIER : [a-zA-Z][a-zA-Z0-9]* ;
WS : [ \\t\\r\\n]+ -> skip ;`,
    bison: `%{
#include <stdio.h>
#include <stdlib.h>
%}

%token NUMBER IDENTIFIER
%left '+' '-'
%left '*' '/'

%%

expression:
    term
    | expression '+' term
    | expression '-' term
    ;

term:
    factor
    | term '*' factor
    | term '/' factor
    ;

factor:
    NUMBER
    | IDENTIFIER
    | '(' expression ')'
    ;

%%`,
  };

  const handleFileUpload = (event) => {
    const file = event.target.files[0];
    if (!file) {
      return;
    }

    setError(null);
    setUploadedFile(file);

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target.result;
      setGrammarText(content);

      // Auto-detect format based on file extension
      const extension = file.name.split('.').pop().toLowerCase();
      const detectedFormat = grammarFormats.find(f =>
        f.extension.slice(1) === extension,
      );
      if (detectedFormat) {
        setGrammarFormat(detectedFormat.id);
      }
    };
    reader.onerror = () => {
      setError('Failed to read file');
    };
    reader.readAsText(file);
  };

  const handleTextChange = (value) => {
    setGrammarText(value);
    setError(null);
    setSuccess(false);
  };

  const handleFormatChange = (format) => {
    setGrammarFormat(format);
    setError(null);
  };

  const loadSampleGrammar = (format) => {
    const sample = sampleGrammars[format];
    if (sample) {
      setGrammarText(sample);
      setGrammarFormat(format);
      setUploadedFile(null);
      setError(null);
    }
  };

  const validateGrammar = (text, format) => {
    if (!text.trim()) {
      throw new Error('Grammar content is empty');
    }

    // Basic validation based on format
    switch (format) {
      case 'grammarforge':
        if (!text.includes('grammar ')) {
          throw new Error('GrammarForge grammar must start with "grammar" keyword');
        }
        break;
      case 'antlr4':
        if (!text.includes('grammar ')) {
          throw new Error('ANTLR v4 grammar must start with "grammar" keyword');
        }
        break;
      case 'bison':
        if (!text.includes('%%')) {
          throw new Error('Bison grammar must contain "%%" sections');
        }
        break;
    }

    return true;
  };

  const processGrammar = async () => {
    if (!grammarText.trim()) {
      setError('Please provide grammar content');
      return;
    }

    setIsProcessing(true);
    setError(null);

    try {
      // Validate grammar
      validateGrammar(grammarText, grammarFormat);

      // Simulate processing delay
      await new Promise(resolve => setTimeout(resolve, 1000));

      // Create grammar object
      const grammar = {
        name: uploadedFile?.name.replace(/\.[^/.]+$/, '') || 'Untitled Grammar',
        format: grammarFormats.find(f => f.id === grammarFormat)?.name || 'Unknown',
        content: grammarText,
        metadata: {
          rules: (grammarText.match(/^\s*\w+\s*:/gm) || []).length,
          terminals: (grammarText.match(/[A-Z_][A-Z0-9_]*/g) || []).length,
          inheritance: grammarText.includes('inherits'),
          contextSensitive: grammarText.includes('context') || grammarText.includes('scope'),
          size: grammarText.length,
          lines: grammarText.split('\n').length,
        },
      };

      onGrammarUpload?.(grammar);
      setSuccess(true);

      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(false), 3000);

    } catch (err) {
      setError(err.message);
    } finally {
      setIsProcessing(false);
    }
  };

  const clearGrammar = () => {
    setGrammarText('');
    setUploadedFile(null);
    setError(null);
    setSuccess(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className="space-y-4">
      {/* Upload Method Tabs */}
      <Tabs value={uploadMethod} onValueChange={setUploadMethod}>
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="file">
            <File className="h-4 w-4 mr-2" />
            File Upload
          </TabsTrigger>
          <TabsTrigger value="text">
            <Code className="h-4 w-4 mr-2" />
            Text Input
          </TabsTrigger>
        </TabsList>

        <TabsContent value="file" className="space-y-3">
          <div className="space-y-2">
            <Label>Grammar File</Label>
            <div className="flex space-x-2">
              <Button
                variant="outline"
                onClick={() => fileInputRef.current?.click()}
                className="flex-1"
              >
                <Upload className="h-4 w-4 mr-2" />
                Choose File
              </Button>
              {uploadedFile && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={clearGrammar}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              )}
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept=".gf,.g4,.y,.l,.ebnf,.bnf,.txt"
              onChange={handleFileUpload}
              className="hidden"
            />

            {uploadedFile && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className="flex items-center space-x-2 p-2 bg-muted rounded-md"
              >
                <FileText className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm flex-1">{uploadedFile.name}</span>
                <Badge variant="secondary" className="text-xs">
                  {(uploadedFile.size / 1024).toFixed(1)} KB
                </Badge>
              </motion.div>
            )}
          </div>
        </TabsContent>

        <TabsContent value="text" className="space-y-3">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Grammar Content</Label>
              <div className="flex space-x-1">
                {Object.keys(sampleGrammars).map(format => (
                  <Button
                    key={format}
                    variant="ghost"
                    size="sm"
                    onClick={() => loadSampleGrammar(format)}
                    className="text-xs h-6 px-2"
                  >
                    {format.toUpperCase()}
                  </Button>
                ))}
              </div>
            </div>
            <Textarea
              value={grammarText}
              onChange={(e) => handleTextChange(e.target.value)}
              placeholder="Paste your grammar content here..."
              className="min-h-[200px] font-mono text-sm"
            />
            {grammarText && (
              <div className="flex items-center space-x-2 text-xs text-muted-foreground">
                <span>{grammarText.split('\n').length} lines</span>
                <span>•</span>
                <span>{grammarText.length} characters</span>
              </div>
            )}
          </div>
        </TabsContent>
      </Tabs>

      {/* Format Selection */}
      <div className="space-y-2">
        <Label>Grammar Format</Label>
        <Select value={grammarFormat} onValueChange={handleFormatChange}>
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {grammarFormats.map(format => (
              <SelectItem key={format.id} value={format.id}>
                <div className="flex items-center space-x-2">
                  <span>{format.name}</span>
                  <Badge variant="outline" className="text-xs">
                    {format.extension}
                  </Badge>
                </div>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Error Display */}
      <AnimatePresence>
        {error && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
          >
            <Card className="border-destructive">
              <CardContent className="p-3">
                <div className="flex items-center space-x-2 text-destructive">
                  <AlertCircle className="h-4 w-4" />
                  <span className="text-sm">{error}</span>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Success Display */}
      <AnimatePresence>
        {success && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
          >
            <Card className="border-green-500">
              <CardContent className="p-3">
                <div className="flex items-center space-x-2 text-green-600">
                  <Check className="h-4 w-4" />
                  <span className="text-sm">Grammar processed successfully!</span>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Process Button */}
      <Button
        onClick={processGrammar}
        disabled={!grammarText.trim() || isProcessing}
        className="w-full"
      >
        {isProcessing ? (
          <>
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            Processing...
          </>
        ) : (
          <>
            <FileText className="h-4 w-4 mr-2" />
            Generate Diagram
          </>
        )}
      </Button>
    </div>
  );
};

export default GrammarUploader;

