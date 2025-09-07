/**
 * LLM Rule Generator Panel Component
 *
 * This component provides a user interface for generating transformation rules
 * using LLM assistance. Users can upload code examples, configure generation
 * parameters, and review/refine generated rules.
 *
 * Key Features:
 * - Code example upload and management
 * - LLM-assisted rule generation
 * - Real-time generation progress
 * - Rule preview and refinement
 * - Batch rule generation
 * - Rule validation and testing
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback, useEffect, useMemo } from 'react';
import { TranslationEngineOrchestrator } from '../engines/TranslationEngineOrchestrator';
import { TranslationContext } from '../engines/TranslationEngineInterface';
import RuleGenerationConfig from './RuleGenerationConfig';
import GenerationProgress from './GenerationProgress';
import GeneratedRulePreview from './GeneratedRulePreview';

interface CodeExample {
    id: string;
    name: string;
    sourceCode: string;
    targetCode?: string;
    sourceLanguage: string;
    targetLanguage: string;
    description?: string;
    tags: string[];
    quality: number;
    verified: boolean;
    createdAt: Date;
}

interface TransformationRule {
    id: string;
    name: string;
    description: string;
    sourceLanguage: string;
    targetLanguage: string;
    pattern: RulePattern;
    transformation: RuleTransformation;
    constraints: RuleConstraint[];
    confidence: number;
    examples: string[];
    tags: string[];
    createdBy: 'user' | 'llm' | 'pattern-learning';
    createdAt: Date;
    lastModified: Date;
    usageCount: number;
    successRate: number;
    enabled: boolean;
    version: string;
    category: string;
    complexity: 'simple' | 'moderate' | 'complex';
    quality: number;
}

interface RulePattern {
    type: 'ast-pattern' | 'regex' | 'template';
    pattern: string;
    variables: Record<string, string>;
    context?: string[];
}

interface RuleTransformation {
    type: 'template' | 'function' | 'ast-transform';
    template: string;
    parameters: Record<string, any>;
    postProcessing?: string[];
}

interface RuleConstraint {
    type: 'context' | 'syntax' | 'semantic' | 'framework';
    condition: string;
    value: any;
    required: boolean;
}

interface RuleGenerationRequest {
    examples: CodeExample[];
    sourceLanguage: string;
    targetLanguage: string;
    generationType: 'single' | 'batch' | 'pattern-family';
    parameters: RuleGenerationParameters;
}

interface RuleGenerationParameters {
    maxRules: number;
    minConfidence: number;
    includeExplanations: boolean;
    generateVariations: boolean;
    optimizeForSpeed: boolean;
    optimizeForQuality: boolean;
    contextAware: boolean;
    frameworkSpecific: boolean;
}

interface RuleGenerationResult {
    rules: TransformationRule[];
    confidence: number;
    explanation: string;
    alternatives: TransformationRule[];
    warnings: string[];
    suggestions: string[];
    processingTime: number;
    cost: number;
}

interface LLMRuleGeneratorPanelProps {
    orchestrator: TranslationEngineOrchestrator;
    onRuleGenerated: (rules: TransformationRule[]) => void;
    onRuleSelected: (rule: TransformationRule) => void;
    existingRules: TransformationRule[];
    className?: string;
}

interface CodeExampleUploaderProps {
    onExamplesAdded: (examples: CodeExample[]) => void;
    supportedLanguages: string[];
    maxExamples: number;
}

interface RuleGenerationConfigProps {
    parameters: RuleGenerationParameters;
    onParametersChange: (parameters: RuleGenerationParameters) => void;
    sourceLanguage: string;
    targetLanguage: string;
}

interface GeneratedRulePreviewProps {
    rule: TransformationRule;
    examples: CodeExample[];
    onAccept: () => void;
    onReject: () => void;
    onModify: (modifiedRule: TransformationRule) => void;
    onTest: () => void;
}

interface GenerationProgressProps {
    status: 'idle' | 'preparing' | 'analyzing' | 'generating' | 'validating' | 'complete' | 'error';
    progress: number;
    currentStep: string;
    estimatedTimeRemaining: number;
    onCancel: () => void;
}

/**
 * Main LLM Rule Generator Panel Component
 */
export const LLMRuleGeneratorPanel: React.FC<LLMRuleGeneratorPanelProps> = ({
  orchestrator,
  onRuleGenerated,
  onRuleSelected,
  existingRules,
  className = '',
}) => {
  const [codeExamples, setCodeExamples] = useState<CodeExample[]>([]);
  const [selectedExamples, setSelectedExamples] = useState<Set<string>>(new Set());
  const [sourceLanguage, setSourceLanguage] = useState<string>('asp');
  const [targetLanguage, setTargetLanguage] = useState<string>('csharp');
  const [generationParameters, setGenerationParameters] = useState<RuleGenerationParameters>({
    maxRules: 5,
    minConfidence: 0.8,
    includeExplanations: true,
    generateVariations: true,
    optimizeForSpeed: false,
    optimizeForQuality: true,
    contextAware: true,
    frameworkSpecific: true,
  });
  const [generationStatus, setGenerationStatus] = useState<'idle' | 'preparing' | 'analyzing' | 'generating' | 'validating' | 'complete' | 'error'>('idle');
  const [generationProgress, setGenerationProgress] = useState<number>(0);
  const [currentStep, setCurrentStep] = useState<string>('');
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = useState<number>(0);
  const [generatedRules, setGeneratedRules] = useState<TransformationRule[]>([]);
  const [selectedRule, setSelectedRule] = useState<TransformationRule | null>(null);
  const [generationResult, setGenerationResult] = useState<RuleGenerationResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const supportedLanguages = useMemo(() => [
    'asp', 'vbscript', 'csharp', 'java', 'javascript', 'typescript',
    'python', 'cpp', 'go', 'rust', 'cobol', 'powerbuilder',
  ], []);

  const canGenerate = useMemo(() => {
    return codeExamples.length > 0 &&
               selectedExamples.size > 0 &&
               sourceLanguage &&
               targetLanguage &&
               generationStatus === 'idle';
  }, [codeExamples.length, selectedExamples.size, sourceLanguage, targetLanguage, generationStatus]);

  const handleExamplesAdded = useCallback((newExamples: CodeExample[]) => {
    setCodeExamples(prev => [...prev, ...newExamples]);
    // Auto-select new examples
    const newIds = new Set(newExamples.map(ex => ex.id));
    setSelectedExamples(prev => new Set([...prev, ...newIds]));
  }, []);

  const handleExampleSelection = useCallback((exampleId: string, selected: boolean) => {
    setSelectedExamples(prev => {
      const newSet = new Set(prev);
      if (selected) {
        newSet.add(exampleId);
      } else {
        newSet.delete(exampleId);
      }
      return newSet;
    });
  }, []);

  const handleParametersChange = useCallback((newParameters: RuleGenerationParameters) => {
    setGenerationParameters(newParameters);
  }, []);

  const handleGenerateRules = useCallback(async () => {
    if (!canGenerate) {
      return;
    }

    try {
      setGenerationStatus('preparing');
      setGenerationProgress(0);
      setCurrentStep('Preparing examples for analysis...');
      setError(null);

      const selectedExamplesList = codeExamples.filter(ex => selectedExamples.has(ex.id));

      const request: RuleGenerationRequest = {
        examples: selectedExamplesList,
        sourceLanguage,
        targetLanguage,
        generationType: selectedExamplesList.length > 3 ? 'batch' : 'single',
        parameters: generationParameters,
      };

      // Simulate progress updates
      const progressSteps = [
        { status: 'analyzing' as const, progress: 20, step: 'Analyzing code examples...' },
        { status: 'generating' as const, progress: 60, step: 'Generating transformation rules...' },
        { status: 'validating' as const, progress: 90, step: 'Validating generated rules...' },
        { status: 'complete' as const, progress: 100, step: 'Rule generation complete!' },
      ];

      for (const progressStep of progressSteps) {
        await new Promise(resolve => setTimeout(resolve, 1500));
        setGenerationStatus(progressStep.status);
        setGenerationProgress(progressStep.progress);
        setCurrentStep(progressStep.step);
        setEstimatedTimeRemaining(Math.max(0, (100 - progressStep.progress) * 0.1));
      }

      // Generate rules using the orchestrator
      const result = await generateRulesWithLLM(request, orchestrator);

      setGeneratedRules(result.rules);
      setGenerationResult(result);

      if (result.rules.length > 0) {
        setSelectedRule(result.rules[0]);
      }

      onRuleGenerated(result.rules);

    } catch (err) {
      setGenerationStatus('error');
      setError(err instanceof Error ? err.message : 'Failed to generate rules');
    }
  // eslint-disable-next-line max-len
  }, [canGenerate, codeExamples, selectedExamples, sourceLanguage, targetLanguage, generationParameters, orchestrator, onRuleGenerated]);

  const handleCancelGeneration = useCallback(() => {
    setGenerationStatus('idle');
    setGenerationProgress(0);
    setCurrentStep('');
    setEstimatedTimeRemaining(0);
  }, []);

  const handleRuleAccept = useCallback((rule: TransformationRule) => {
    onRuleSelected(rule);
    // Add to existing rules
    setGeneratedRules(prev => prev.filter(r => r.id !== rule.id));
  }, [onRuleSelected]);

  const handleRuleReject = useCallback((rule: TransformationRule) => {
    setGeneratedRules(prev => prev.filter(r => r.id !== rule.id));
    if (selectedRule?.id === rule.id) {
      const remaining = generatedRules.filter(r => r.id !== rule.id);
      setSelectedRule(remaining.length > 0 ? remaining[0] : null);
    }
  }, [selectedRule, generatedRules]);

  const handleRuleModify = useCallback((modifiedRule: TransformationRule) => {
    setGeneratedRules(prev => prev.map(r => r.id === modifiedRule.id ? modifiedRule : r));
    if (selectedRule?.id === modifiedRule.id) {
      setSelectedRule(modifiedRule);
    }
  }, [selectedRule]);

  const handleRuleTest = useCallback(async (rule: TransformationRule) => {
    // Test the rule against the examples
    // eslint-disable-next-line no-console
    console.log('Testing rule:', rule.name);
    // Implementation would test the rule and show results
  }, []);

  return (
    <div className={`llm-rule-generator-panel ${className}`}>
      {/* Header */}
      <div className="panel-header">
        <h2>🤖 LLM-Assisted Rule Generation</h2>
        <p>Generate transformation rules from code examples using AI assistance</p>
      </div>

      {/* Main Content */}
      <div className="panel-content">
        {/* Step 1: Code Examples */}
        <div className="generation-step">
          <h3>1. Code Examples</h3>
          <CodeExampleUploader
            onExamplesAdded={handleExamplesAdded}
            supportedLanguages={supportedLanguages}
            maxExamples={20}
          />

          {codeExamples.length > 0 && (
            <div className="examples-list">
              <h4>Uploaded Examples ({codeExamples.length})</h4>
              {codeExamples.map(example => (
                <div key={example.id} className="example-item">
                  <label className="example-checkbox">
                    <input
                      type="checkbox"
                      checked={selectedExamples.has(example.id)}
                      onChange={(e) => handleExampleSelection(example.id, e.target.checked)}
                    />
                    <span className="example-name">{example.name}</span>
                    <span className="example-languages">
                      {example.sourceLanguage} → {example.targetLanguage}
                    </span>
                    <span className="example-quality">
                                            Quality: {(example.quality * 100).toFixed(0)}%
                    </span>
                  </label>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Step 2: Language Selection */}
        <div className="generation-step">
          <h3>2. Language Pair</h3>
          <div className="language-selection">
            <div className="language-selector">
              <label htmlFor="source-language">Source Language:</label>
              <select
                id="source-language"
                value={sourceLanguage}
                onChange={(e) => setSourceLanguage(e.target.value)}
                className="form-control"
              >
                {supportedLanguages.map(lang => (
                  <option key={lang} value={lang}>
                    {lang.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>
            <div className="language-arrow">→</div>
            <div className="language-selector">
              <label htmlFor="target-language">Target Language:</label>
              <select
                id="target-language"
                value={targetLanguage}
                onChange={(e) => setTargetLanguage(e.target.value)}
                className="form-control"
              >
                {supportedLanguages.map(lang => (
                  <option key={lang} value={lang}>
                    {lang.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Step 3: Generation Configuration */}
        <div className="generation-step">
          <h3>3. Generation Settings</h3>
          <RuleGenerationConfig
            parameters={generationParameters}
            onParametersChange={handleParametersChange}
            sourceLanguage={sourceLanguage}
            targetLanguage={targetLanguage}
          />
        </div>

        {/* Step 4: Generate Rules */}
        <div className="generation-step">
          <h3>4. Generate Rules</h3>
          <div className="generation-controls">
            <button
              onClick={handleGenerateRules}
              disabled={!canGenerate}
              className={`btn btn-primary generate-btn ${!canGenerate ? 'disabled' : ''}`}
            >
              {generationStatus === 'idle' ? '🚀 Generate Rules' : '⏳ Generating...'}
            </button>

            {generationStatus !== 'idle' && generationStatus !== 'complete' && (
              <button
                onClick={handleCancelGeneration}
                className="btn btn-secondary cancel-btn"
              >
                                Cancel
              </button>
            )}
          </div>

          {generationStatus !== 'idle' && (
            <GenerationProgress
              status={generationStatus}
              progress={generationProgress}
              currentStep={currentStep}
              estimatedTimeRemaining={estimatedTimeRemaining}
              onCancel={handleCancelGeneration}
            />
          )}

          {error && (
            <div className="error-message">
              <span className="error-icon">❌</span>
              <span className="error-text">{error}</span>
            </div>
          )}
        </div>

        {/* Step 5: Review Generated Rules */}
        {generatedRules.length > 0 && (
          <div className="generation-step">
            <h3>5. Review Generated Rules</h3>
            <div className="generated-rules-section">
              <div className="rules-sidebar">
                <h4>Generated Rules ({generatedRules.length})</h4>
                {generatedRules.map(rule => (
                  <div
                    key={rule.id}
                    className={`rule-item ${selectedRule?.id === rule.id ? 'selected' : ''}`}
                    onClick={() => setSelectedRule(rule)}
                  >
                    <div className="rule-name">{rule.name}</div>
                    <div className="rule-confidence">
                                            Confidence: {(rule.confidence * 100).toFixed(0)}%
                    </div>
                    <div className="rule-tags">
                      {rule.tags.map(tag => (
                        <span key={tag} className="rule-tag">{tag}</span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>

              <div className="rule-preview-area">
                {selectedRule && (
                  <GeneratedRulePreview
                    rule={selectedRule}
                    examples={codeExamples.filter(ex => selectedExamples.has(ex.id))}
                    onAccept={() => handleRuleAccept(selectedRule)}
                    onReject={() => handleRuleReject(selectedRule)}
                    onModify={handleRuleModify}
                    onTest={() => handleRuleTest(selectedRule)}
                  />
                )}
              </div>
            </div>
          </div>
        )}

        {/* Generation Results Summary */}
        {generationResult && (
          <div className="generation-summary">
            <h4>Generation Summary</h4>
            <div className="summary-stats">
              <div className="stat">
                <span className="stat-label">Rules Generated:</span>
                <span className="stat-value">{generationResult.rules.length}</span>
              </div>
              <div className="stat">
                <span className="stat-label">Average Confidence:</span>
                <span className="stat-value">
                  {(generationResult.confidence * 100).toFixed(1)}%
                </span>
              </div>
              <div className="stat">
                <span className="stat-label">Processing Time:</span>
                <span className="stat-value">{generationResult.processingTime.toFixed(1)}s</span>
              </div>
              <div className="stat">
                <span className="stat-label">Cost:</span>
                <span className="stat-value">${generationResult.cost.toFixed(3)}</span>
              </div>
            </div>

            {generationResult.warnings.length > 0 && (
              <div className="warnings">
                <h5>⚠️ Warnings</h5>
                {generationResult.warnings.map((warning, index) => (
                  <div key={index} className="warning-item">{warning}</div>
                ))}
              </div>
            )}

            {generationResult.suggestions.length > 0 && (
              <div className="suggestions">
                <h5>💡 Suggestions</h5>
                {generationResult.suggestions.map((suggestion, index) => (
                  <div key={index} className="suggestion-item">{suggestion}</div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Code Example Uploader Component
 */
const CodeExampleUploader: React.FC<CodeExampleUploaderProps> = ({
  onExamplesAdded,
  supportedLanguages,
  maxExamples,
}) => {
  const [dragActive, setDragActive] = useState(false);
  const [uploadMode, setUploadMode] = useState<'file' | 'paste' | 'url'>('paste');
  const [pastedCode, setPastedCode] = useState('');
  const [exampleName, setExampleName] = useState('');
  const [sourceLanguage, setSourceLanguage] = useState('asp');
  const [targetLanguage, setTargetLanguage] = useState('csharp');

  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleFiles = useCallback(async (files: File[]) => {
    const examples: CodeExample[] = [];

    for (const file of files.slice(0, maxExamples)) {
      try {
        const content = await file.text();
        const example: CodeExample = {
          id: `file_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
          name: file.name,
          sourceCode: content,
          sourceLanguage: detectLanguageFromFilename(file.name) || sourceLanguage,
          targetLanguage,
          tags: ['uploaded', 'file'],
          quality: 0.8, // Default quality for uploaded files
          verified: false,
          createdAt: new Date(),
        };
        examples.push(example);
      } catch (error) {
    // eslint-disable-next-line no-console
        console.error('Error reading file:', file.name, error);
      }
    }

    if (examples.length > 0) {
      onExamplesAdded(examples);
    }
  }, [maxExamples, sourceLanguage, targetLanguage, onExamplesAdded]);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    const files = Array.from(e.dataTransfer.files);
    handleFiles(files);
  }, [handleFiles]);

  const handlePasteCode = useCallback((e?: React.MouseEvent<HTMLButtonElement>): void => {
    if (!pastedCode.trim() || !exampleName.trim()) {
      return;
    }

    const example: CodeExample = {
      id: `paste_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      name: exampleName,
      sourceCode: pastedCode,
      sourceLanguage,
      targetLanguage,
      tags: ['pasted', 'manual'],
      quality: 0.7, // Default quality for pasted code
      verified: false,
      createdAt: new Date(),
    };

    onExamplesAdded([example]);
    setPastedCode('');
    setExampleName('');
  }, [pastedCode, exampleName, sourceLanguage, targetLanguage, onExamplesAdded]);

  return (
    <div className="code-example-uploader">
      <div className="upload-mode-selector">
        <button
          className={`mode-btn ${uploadMode === 'paste' ? 'active' : ''}`}
          onClick={() => setUploadMode('paste')}
        >
                    📝 Paste Code
        </button>
        <button
          className={`mode-btn ${uploadMode === 'file' ? 'active' : ''}`}
          onClick={() => setUploadMode('file')}
        >
                    📁 Upload Files
        </button>
        <button
          className={`mode-btn ${uploadMode === 'url' ? 'active' : ''}`}
          onClick={() => setUploadMode('url')}
        >
                    🔗 From URL
        </button>
      </div>

      {uploadMode === 'paste' && (
        <div className="paste-mode">
          <div className="form-row">
            <input
              type="text"
              placeholder="Example name (e.g., 'ASP Login Form')"
              value={exampleName}
              onChange={(e) => setExampleName(e.target.value)}
              className="form-control example-name-input"
            />
          </div>
          <div className="form-row">
            <select
              value={sourceLanguage}
              onChange={(e) => setSourceLanguage(e.target.value)}
              className="form-control language-select"
            >
              {supportedLanguages.map(lang => (
                <option key={lang} value={lang}>{lang.toUpperCase()}</option>
              ))}
            </select>
            <span className="arrow">→</span>
            <select
              value={targetLanguage}
              onChange={(e) => setTargetLanguage(e.target.value)}
              className="form-control language-select"
            >
              {supportedLanguages.map(lang => (
                <option key={lang} value={lang}>{lang.toUpperCase()}</option>
              ))}
            </select>
          </div>
          <textarea
            placeholder="Paste your source code here..."
            value={pastedCode}
            onChange={(e) => setPastedCode(e.target.value)}
            className="form-control code-textarea"
            rows={10}
          />
          <button
            onClick={handlePasteCode}
            disabled={!pastedCode.trim() || !exampleName.trim()}
            className="btn btn-primary add-example-btn"
          >
                        Add Example
          </button>
        </div>
      )}

      {uploadMode === 'file' && (
        <div className="file-mode">
          <div
            className={`drop-zone ${dragActive ? 'active' : ''}`}
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
          >
            <div className="drop-zone-content">
              <div className="drop-icon">📁</div>
              <div className="drop-text">
                <strong>Drop code files here</strong> or{' '}
                <label className="file-input-label">
                                    browse files
                  <input
                    type="file"
                    multiple
                    accept=".asp,.vb,.cs,.java,.js,.ts,.py,.cpp,.go,.rs,.cbl,.pb"
                    onChange={(e) => e.target.files && handleFiles(Array.from(e.target.files))}
                    className="file-input"
                  />
                </label>
              </div>
              <div className="drop-hint">
                                Supports: .asp, .vb, .cs, .java, .js, .ts, .py, .cpp, .go, .rs, .cbl, .pb
              </div>
            </div>
          </div>
        </div>
      )}

      {uploadMode === 'url' && (
        <div className="url-mode">
          <input
            type="url"
            placeholder="Enter GitHub URL, Gist URL, or direct file URL..."
            className="form-control url-input"
          />
          <button className="btn btn-primary fetch-btn">
                        Fetch Code
          </button>
        </div>
      )}
    </div>
  );
};

/**
 * Utility function to detect language from filename
 */
function detectLanguageFromFilename(filename: string): string | null {
  const ext = filename.split('.').pop()?.toLowerCase();
  const languageMap: Record<string, string> = {
    'asp': 'asp',
    'vb': 'vbscript',
    'cs': 'csharp',
    'java': 'java',
    'js': 'javascript',
    'ts': 'typescript',
    'py': 'python',
    'cpp': 'cpp',
    'cc': 'cpp',
    'cxx': 'cpp',
    'go': 'go',
    'rs': 'rust',
    'cbl': 'cobol',
    'cob': 'cobol',
    'pb': 'powerbuilder',
  };
  return ext ? languageMap[ext] || null : null;
}

/**
 * Mock function for LLM rule generation
 * In production, this would call the actual LLM engine
 */
async function generateRulesWithLLM(
  request: RuleGenerationRequest,
  orchestrator: TranslationEngineOrchestrator,
): Promise<RuleGenerationResult> {
  // Simulate LLM processing time
  await new Promise(resolve => setTimeout(resolve, 2000));

  // Generate mock rules based on the request
  const rules: TransformationRule[] = [];

  for (let i = 0; i < Math.min(request.parameters.maxRules, 3); i++) {
    const rule: TransformationRule = {
      id: `llm_rule_${Date.now()}_${i}`,
      name: `${request.sourceLanguage.toUpperCase()} to ${request.targetLanguage.toUpperCase()} Rule ${i + 1}`,
      description: `Generated rule for transforming ${request.sourceLanguage} patterns to ${request.targetLanguage}`,
      sourceLanguage: request.sourceLanguage,
      targetLanguage: request.targetLanguage,
      pattern: {
        type: 'ast-pattern',
        pattern: 'CallExpression[callee.object.name="Response"][callee.property.name="Write"]',
        variables: { content: 'arguments[0]' },
        context: ['web', 'server'],
      },
      transformation: {
        type: 'template',
        template: 'await Response.WriteAsync(${content})',
        parameters: { async: true, framework: 'aspnetcore' },
      },
      constraints: [
        {
          type: 'framework',
          condition: 'target_framework',
          value: 'aspnetcore',
          required: true,
        },
      ],
      confidence: 0.85 + (Math.random() * 0.1),
      examples: request.examples.map(ex => ex.id),
      tags: ['llm-generated', 'web', 'response'],
      createdBy: 'llm',
      createdAt: new Date(),
      lastModified: new Date(),
      usageCount: 0,
      successRate: 0,
      enabled: true,
      version: '1.0',
      category: 'llm-generated',
      complexity: 'simple',
      quality: 0.85 + (Math.random() * 0.1),
    };
    rules.push(rule);
  }

  return {
    rules,
    confidence: 0.87,
    explanation: 'Generated rules based on common patterns found in the provided examples. Rules focus on web response handling and framework-specific transformations.',
    alternatives: [],
    warnings: rules.length < request.parameters.maxRules ? ['Fewer rules generated than requested due to limited pattern diversity'] : [],
    suggestions: [
      'Consider providing more diverse examples for better rule coverage',
      'Test generated rules with additional code samples before deployment',
    ],
    processingTime: 3.2,
    cost: 0.05,
  };
}

export default LLMRuleGeneratorPanel;

