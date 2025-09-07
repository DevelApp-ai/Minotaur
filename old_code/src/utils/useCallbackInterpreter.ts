/**
 * Provides a React hook for using the callback interpreter.
 */
import { useState, useCallback } from 'react';
import { CallbackInterpreter } from './CallbackInterpreter';
import { ProductionMatch } from './ProductionMatch';

export interface UseCallbackInterpreterResult {
  interpreter: CallbackInterpreter;
  parseResults: ProductionMatch[] | null;
  parseContext: any | null;
  parseError: string | null;
  isLoading: boolean;
  loadGrammar: (grammarCode: string, fileName: string) => void;
  parseSourceCode: (sourceCode: string, fileName: string) => void;
  registerCallback: (name: string, callback: Function) => void;
}

export function useCallbackInterpreter(): UseCallbackInterpreterResult {
  const [interpreter] = useState<CallbackInterpreter>(() => new CallbackInterpreter());
  const [parseResults, setParseResults] = useState<ProductionMatch[] | null>(null);
  const [parseContext, setParseContext] = useState<any | null>(null);
  const [parseError, setParseError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [activeGrammarName, setActiveGrammarName] = useState<string | null>(null);

  const loadGrammar = useCallback((grammarCode: string, fileName: string) => {
    setIsLoading(true);
    setParseError(null);

    try {
      const grammar = interpreter.loadGrammarWithCallbacksFromString(grammarCode, fileName);
      setActiveGrammarName(grammar.getName());
      setIsLoading(false);
    } catch (error) {
      // eslint-disable-next-line max-len
      setParseError(`Error loading grammar: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`);
      setIsLoading(false);
    }
  }, [interpreter]);

  const parseSourceCode = useCallback(async (sourceCode: string, fileName: string) => {
    if (!activeGrammarName) {
      setParseError('No grammar loaded. Please load a grammar first.');
      return;
    }

    setIsLoading(true);
    setParseError(null);

    try {
      const { results, context } = await interpreter.parseSourceCodeWithCallbacks(
        activeGrammarName,
        sourceCode,
        fileName,
        {},
      );

      setParseResults(results);
      setParseContext(context);
      setIsLoading(false);
    } catch (error) {
      // eslint-disable-next-line max-len
      setParseError(`Error parsing source code: ${error instanceof Error ? error instanceof Error ? error.message : String(error) : String(error)}`);
      setParseResults(null);
      setParseContext(null);
      setIsLoading(false);
    }
  }, [interpreter, activeGrammarName]);

  const registerCallback = useCallback((name: string, callback: Function) => {
    interpreter.registerCallback(name, callback);
  }, [interpreter]);

  return {
    interpreter,
    parseResults,
    parseContext,
    parseError,
    isLoading,
    loadGrammar,
    parseSourceCode,
    registerCallback,
  };
}
