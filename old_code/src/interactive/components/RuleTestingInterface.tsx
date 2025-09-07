/**
 * Rule Testing Interface Component
 *
 * This component provides a comprehensive testing environment for transformation rules,
 * allowing users to test rules against code samples, batch test multiple rules,
 * analyze performance metrics, and debug rule behavior.
 *
 * Key Features:
 * - Interactive code testing with real-time results
 * - Batch testing with multiple code samples
 * - Performance metrics and analytics
 * - Rule debugging and step-by-step execution
 * - Test case management and history
 * - Regression testing capabilities
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback, useMemo, useRef, useEffect } from 'react';

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

interface TestCase {
    id: string;
    name: string;
    sourceCode: string;
    expectedOutput?: string;
    description: string;
    tags: string[];
    createdAt: Date;
    lastTested?: Date;
    category: string;
    complexity: 'simple' | 'moderate' | 'complex';
}

interface TestResult {
    id: string;
    testCaseId: string;
    ruleId: string;
    success: boolean;
    transformedCode: string;
    confidence: number;
    executionTime: number;
    memoryUsage: number;
    errors: string[];
    warnings: string[];
    suggestions: string[];
    matchedPattern: boolean;
    appliedTransformation: boolean;
    satisfiedConstraints: boolean;
    debugInfo: DebugInfo;
    timestamp: Date;
}

interface DebugInfo {
    steps: DebugStep[];
    variables: Record<string, any>;
    patternMatches: PatternMatch[];
    transformationSteps: TransformationStep[];
    constraintEvaluations: ConstraintEvaluation[];
}

interface DebugStep {
    id: string;
    type: 'pattern-match' | 'variable-capture' | 'transformation' | 'constraint-check';
    description: string;
    input: string;
    output: string;
    success: boolean;
    duration: number;
    details: Record<string, any>;
}

interface PatternMatch {
    pattern: string;
    matched: boolean;
    position: { start: number; end: number };
    capturedVariables: Record<string, string>;
    confidence: number;
}

interface TransformationStep {
    template: string;
    variables: Record<string, string>;
    result: string;
    success: boolean;
}

interface ConstraintEvaluation {
    constraint: RuleConstraint;
    satisfied: boolean;
    reason: string;
}

interface RuleTestingInterfaceProps {
    rules: TransformationRule[];
    onRuleUpdate: (rule: TransformationRule) => void;
    className?: string;
}

interface BatchTestConfig {
    rules: string[];
    testCases: string[];
    parallel: boolean;
    stopOnFirstFailure: boolean;
    generateReport: boolean;
    includeDebugInfo: boolean;
}

export const RuleTestingInterface: React.FC<RuleTestingInterfaceProps> = ({
  rules,
  onRuleUpdate,
  className = '',
}) => {
  const [selectedRule, setSelectedRule] = useState<TransformationRule | null>(null);
  const [testCases, setTestCases] = useState<TestCase[]>([]);
  const [selectedTestCase, setSelectedTestCase] = useState<TestCase | null>(null);
  const [testResults, setTestResults] = useState<TestResult[]>([]);
  const [currentTest, setCurrentTest] = useState<{
        sourceCode: string;
        expectedOutput: string;
        testName: string;
    }>({
      sourceCode: '',
      expectedOutput: '',
      testName: '',
    });

  const [activeTab, setActiveTab] = useState<'single' | 'batch' | 'history' | 'analytics'>('single');
  const [isTestingRule, setIsTestingRule] = useState(false);
  const [isBatchTesting, setIsBatchTesting] = useState(false);
  const [batchTestConfig, setBatchTestConfig] = useState<BatchTestConfig>({
    rules: [],
    testCases: [],
    parallel: true,
    stopOnFirstFailure: false,
    generateReport: true,
    includeDebugInfo: false,
  });

  const [showDebugPanel, setShowDebugPanel] = useState(false);
  const [selectedTestResult, setSelectedTestResult] = useState<TestResult | null>(null);
  const [testFilter, setTestFilter] = useState({
    success: null as boolean | null,
    rule: '',
    dateRange: { start: null as Date | null, end: null as Date | null },
  });

  // Sample test cases for demonstration
  const sampleTestCases: TestCase[] = useMemo(() => [
    {
      id: 'test_asp_response_write',
      name: 'ASP Response.Write Test',
      sourceCode: 'Response.Write("Hello World")',
      expectedOutput: 'await Response.WriteAsync("Hello World")',
      description: 'Test ASP Response.Write to C# conversion',
      tags: ['asp', 'response', 'basic'],
      createdAt: new Date(),
      category: 'web',
      complexity: 'simple',
    },
    {
      id: 'test_vb_dim_statement',
      name: 'VB Dim Statement Test',
      sourceCode: 'Dim userName As String',
      expectedOutput: 'string userName',
      description: 'Test VBScript Dim to C# variable declaration',
      tags: ['vbscript', 'variable', 'declaration'],
      createdAt: new Date(),
      category: 'variables',
      complexity: 'simple',
    },
    {
      id: 'test_complex_asp_form',
      name: 'Complex ASP Form Processing',
      sourceCode: `
If Request.Form("submit") <> "" Then
    Dim userName
    userName = Request.Form("username")
    Response.Write("Hello " & userName)
End If`,
      expectedOutput: `
if (!string.IsNullOrEmpty(Request.Form["submit"]))
{
    string userName = Request.Form["username"];
    await Response.WriteAsync("Hello " + userName);
}`,
      description: 'Test complex ASP form processing conversion',
      tags: ['asp', 'form', 'complex', 'conditional'],
      createdAt: new Date(),
      category: 'web',
      complexity: 'complex',
    },
  ], []);

  useEffect(() => {
    setTestCases(sampleTestCases);
  }, [sampleTestCases]);

  const handleRuleSelect = useCallback((rule: TransformationRule) => {
    setSelectedRule(rule);
    setSelectedTestResult(null);
  }, []);

  const handleTestCaseSelect = useCallback((testCase: TestCase) => {
    setSelectedTestCase(testCase);
    setCurrentTest({
      sourceCode: testCase.sourceCode,
      expectedOutput: testCase.expectedOutput || '',
      testName: testCase.name,
    });
  }, []);

  const executeRuleTest = useCallback(async (
    rule: TransformationRule,
    sourceCode: string,
    testCaseId?: string,
  ): Promise<TestResult> => {
    // Simulate rule execution with mock data
    await new Promise(resolve => setTimeout(resolve, 500 + Math.random() * 1000));

    const success = Math.random() > 0.2; // 80% success rate
    const transformedCode = success ? mockTransformCode(sourceCode, rule) : sourceCode;

    const debugInfo: DebugInfo = {
      steps: [
        {
          id: 'step_1',
          type: 'pattern-match',
          description: 'Matching pattern against source code',
          input: sourceCode,
          output: success ? 'Pattern matched' : 'Pattern not matched',
          success: success,
          duration: 50 + Math.random() * 100,
          details: { pattern: rule.pattern.pattern },
        },
        {
          id: 'step_2',
          type: 'variable-capture',
          description: 'Capturing pattern variables',
          input: 'Pattern match result',
          output: success ? 'Variables captured' : 'No variables captured',
          success: success,
          duration: 20 + Math.random() * 50,
          details: { variables: success ? { content: '"Hello World"' } : {} },
        },
        {
          id: 'step_3',
          type: 'transformation',
          description: 'Applying transformation template',
          input: 'Captured variables',
          output: transformedCode,
          success: success,
          duration: 100 + Math.random() * 200,
          details: { template: rule.transformation.template },
        },
      ],
      variables: success ? { content: '"Hello World"', functionName: 'Response.Write' } : {},
      patternMatches: [
        {
          pattern: rule.pattern.pattern,
          matched: success,
          position: { start: 0, end: sourceCode.length },
          capturedVariables: success ? { content: '"Hello World"' } : {},
          confidence: success ? 0.9 : 0.1,
        },
      ],
      transformationSteps: [
        {
          template: rule.transformation.template,
          variables: success ? { content: '"Hello World"' } : {},
          result: transformedCode,
          success: success,
        },
      ],
      constraintEvaluations: rule.constraints.map(constraint => ({
        constraint,
        satisfied: success,
        reason: success ? 'Constraint satisfied' : 'Constraint not satisfied',
      })),
    };

    const result: TestResult = {
      id: `result_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      testCaseId: testCaseId || 'manual_test',
      ruleId: rule.id,
      success,
      transformedCode,
      confidence: success ? 0.85 + Math.random() * 0.15 : 0.1 + Math.random() * 0.3,
      executionTime: 150 + Math.random() * 300,
      memoryUsage: 1024 + Math.random() * 2048,
      errors: success ? [] : ['Pattern matching failed', 'Transformation could not be applied'],
      warnings: Math.random() > 0.7 ? ['Consider adding error handling'] : [],
      suggestions: Math.random() > 0.5 ? ['Pattern could be more specific'] : [],
      matchedPattern: success,
      appliedTransformation: success,
      satisfiedConstraints: success,
      debugInfo,
      timestamp: new Date(),
    };

    return result;
  }, []);

  const handleSingleTest = useCallback(async () => {
    if (!selectedRule || !currentTest.sourceCode.trim()) {
      return;
    }

    setIsTestingRule(true);
    try {
      const result = await executeRuleTest(
        selectedRule,
        currentTest.sourceCode,
        selectedTestCase?.id,
      );

      setTestResults(prev => [result, ...prev]);
      setSelectedTestResult(result);
    } finally {
      setIsTestingRule(false);
    }
  }, [selectedRule, currentTest.sourceCode, selectedTestCase, executeRuleTest]);

  const handleBatchTest = useCallback(async () => {
    if (batchTestConfig.rules.length === 0 || batchTestConfig.testCases.length === 0) {
      return;
    }

    setIsBatchTesting(true);
    const batchResults: TestResult[] = [];

    try {
      const selectedRules = rules.filter(r => batchTestConfig.rules.includes(r.id));
      const selectedTestCases = testCases.filter(tc => batchTestConfig.testCases.includes(tc.id));

      for (const rule of selectedRules) {
        for (const testCase of selectedTestCases) {
          const result = await executeRuleTest(rule, testCase.sourceCode, testCase.id);
          batchResults.push(result);

          if (!result.success && batchTestConfig.stopOnFirstFailure) {
            break;
          }
        }

        if (batchResults.some(r => !r.success) && batchTestConfig.stopOnFirstFailure) {
          break;
        }
      }

      setTestResults(prev => [...batchResults, ...prev]);
    } finally {
      setIsBatchTesting(false);
    }
  }, [batchTestConfig, rules, testCases, executeRuleTest]);

  const handleCreateTestCase = useCallback(() => {
    if (!currentTest.testName.trim() || !currentTest.sourceCode.trim()) {
      return;
    }

    const newTestCase: TestCase = {
      id: `test_${Date.now()}`,
      name: currentTest.testName,
      sourceCode: currentTest.sourceCode,
      expectedOutput: currentTest.expectedOutput,
      description: `Test case for ${currentTest.testName}`,
      tags: ['custom'],
      createdAt: new Date(),
      category: 'custom',
      complexity: 'simple',
    };

    setTestCases(prev => [newTestCase, ...prev]);
    setCurrentTest({ sourceCode: '', expectedOutput: '', testName: '' });
  }, [currentTest]);

  const filteredTestResults = useMemo(() => {
    return testResults.filter(result => {
      if (testFilter.success !== null && result.success !== testFilter.success) {
        return false;
      }
      if (testFilter.rule && result.ruleId !== testFilter.rule) {
        return false;
      }
      if (testFilter.dateRange.start && result.timestamp < testFilter.dateRange.start) {
        return false;
      }
      if (testFilter.dateRange.end && result.timestamp > testFilter.dateRange.end) {
        return false;
      }
      return true;
    });
  }, [testResults, testFilter]);

  const testStatistics = useMemo(() => {
    const total = filteredTestResults.length;
    const successful = filteredTestResults.filter(r => r.success).length;
    const failed = total - successful;
    const avgExecutionTime = total > 0 ?
      filteredTestResults.reduce((sum, r) => sum + r.executionTime, 0) / total : 0;
    const avgConfidence = total > 0 ?
      filteredTestResults.reduce((sum, r) => sum + r.confidence, 0) / total : 0;

    return {
      total,
      successful,
      failed,
      successRate: total > 0 ? (successful / total) * 100 : 0,
      avgExecutionTime,
      avgConfidence: avgConfidence * 100,
    };
  }, [filteredTestResults]);

  return (
    <div className={`rule-testing-interface ${className}`}>
      {/* Header */}
      <div className="testing-header">
        <div className="header-title">
          <h2>🧪 Rule Testing Interface</h2>
          <p>Test and validate transformation rules with comprehensive debugging</p>
        </div>

        <div className="header-stats">
          <div className="stat-item">
            <span className="stat-value">{testStatistics.total}</span>
            <span className="stat-label">Total Tests</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{testStatistics.successRate.toFixed(1)}%</span>
            <span className="stat-label">Success Rate</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{testStatistics.avgExecutionTime.toFixed(0)}ms</span>
            <span className="stat-label">Avg Time</span>
          </div>
        </div>
      </div>

      {/* Rule Selection */}
      <div className="rule-selection">
        <h3>Select Rule to Test</h3>
        <div className="rule-selector">
          <select
            value={selectedRule?.id || ''}
            onChange={(e) => {
              const rule = rules.find(r => r.id === e.target.value);
              handleRuleSelect(rule!);
            }}
            className="form-control rule-select"
          >
            <option value="">Choose a rule...</option>
            {rules.map(rule => (
              <option key={rule.id} value={rule.id}>
                {rule.name} ({rule.sourceLanguage} → {rule.targetLanguage})
              </option>
            ))}
          </select>

          {selectedRule && (
            <div className="selected-rule-info">
              <div className="rule-info-item">
                <span className="info-label">Description:</span>
                <span className="info-value">{selectedRule.description}</span>
              </div>
              <div className="rule-info-item">
                <span className="info-label">Success Rate:</span>
                <span className="info-value">{(selectedRule.successRate * 100).toFixed(1)}%</span>
              </div>
              <div className="rule-info-item">
                <span className="info-label">Usage Count:</span>
                <span className="info-value">{selectedRule.usageCount}</span>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Testing Tabs */}
      <div className="testing-tabs">
        <button
          className={`tab-btn ${activeTab === 'single' ? 'active' : ''}`}
          onClick={() => setActiveTab('single')}
        >
                    🎯 Single Test
        </button>
        <button
          className={`tab-btn ${activeTab === 'batch' ? 'active' : ''}`}
          onClick={() => setActiveTab('batch')}
        >
                    📊 Batch Test
        </button>
        <button
          className={`tab-btn ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
                    📜 Test History
        </button>
        <button
          className={`tab-btn ${activeTab === 'analytics' ? 'active' : ''}`}
          onClick={() => setActiveTab('analytics')}
        >
                    📈 Analytics
        </button>
      </div>

      {/* Tab Content */}
      <div className="tab-content">
        {activeTab === 'single' && (
          <div className="single-test-panel">
            <div className="test-input-section">
              <div className="test-cases-sidebar">
                <h4>Test Cases</h4>
                <div className="test-case-list">
                  {testCases.map(testCase => (
                    <div
                      key={testCase.id}
                      className={`test-case-item ${selectedTestCase?.id === testCase.id ? 'selected' : ''}`}
                      onClick={() => handleTestCaseSelect(testCase)}
                    >
                      <div className="test-case-name">{testCase.name}</div>
                      <div className="test-case-tags">
                        {testCase.tags.map(tag => (
                          <span key={tag} className="test-tag">{tag}</span>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>

                <div className="create-test-case">
                  <input
                    type="text"
                    placeholder="Test case name..."
                    value={currentTest.testName}
                    onChange={(e) => setCurrentTest(prev => ({ ...prev, testName: e.target.value }))}
                    className="form-control test-name-input"
                  />
                  <button
                    onClick={handleCreateTestCase}
                    disabled={!currentTest.testName.trim() || !currentTest.sourceCode.trim()}
                    className="btn btn-sm btn-primary create-test-btn"
                  >
                                        💾 Save Test Case
                  </button>
                </div>
              </div>

              <div className="test-editor-area">
                <div className="editor-section">
                  <h4>Source Code</h4>
                  <textarea
                    value={currentTest.sourceCode}
                    onChange={(e) => setCurrentTest(prev => ({ ...prev, sourceCode: e.target.value }))}
                    className="form-control code-textarea"
                    rows={8}
                    placeholder="Enter source code to test..."
                  />
                </div>

                <div className="editor-section">
                  <h4>Expected Output (Optional)</h4>
                  <textarea
                    value={currentTest.expectedOutput}
                    onChange={(e) => setCurrentTest(prev => ({ ...prev, expectedOutput: e.target.value }))}
                    className="form-control code-textarea"
                    rows={8}
                    placeholder="Enter expected transformation result..."
                  />
                </div>

                <div className="test-controls">
                  <button
                    onClick={handleSingleTest}
                    disabled={!selectedRule || !currentTest.sourceCode.trim() || isTestingRule}
                    className="btn btn-primary test-btn"
                  >
                    {isTestingRule ? '⏳ Testing...' : '🧪 Run Test'}
                  </button>

                  <button
                    onClick={() => setShowDebugPanel(!showDebugPanel)}
                    className={`btn btn-outline debug-toggle ${showDebugPanel ? 'active' : ''}`}
                  >
                                        🐛 Debug Mode
                  </button>
                </div>
              </div>
            </div>

            {/* Test Results */}
            {selectedTestResult && (
              <div className="test-results-section">
                <TestResultDisplay
                  result={selectedTestResult}
                  rule={selectedRule!}
                  showDebug={showDebugPanel}
                  expectedOutput={currentTest.expectedOutput}
                />
              </div>
            )}
          </div>
        )}

        {activeTab === 'batch' && (
          <div className="batch-test-panel">
            <div className="batch-config">
              <h4>Batch Test Configuration</h4>

              <div className="config-section">
                <h5>Select Rules</h5>
                <div className="rule-checkboxes">
                  {rules.map(rule => (
                    <label key={rule.id} className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={batchTestConfig.rules.includes(rule.id)}
                        onChange={(e) => {
                          setBatchTestConfig(prev => ({
                            ...prev,
                            rules: e.target.checked
                              ? [...prev.rules, rule.id]
                              : prev.rules.filter(id => id !== rule.id),
                          }));
                        }}
                      />
                      <span className="checkbox-text">{rule.name}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div className="config-section">
                <h5>Select Test Cases</h5>
                <div className="test-case-checkboxes">
                  {testCases.map(testCase => (
                    <label key={testCase.id} className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={batchTestConfig.testCases.includes(testCase.id)}
                        onChange={(e) => {
                          setBatchTestConfig(prev => ({
                            ...prev,
                            testCases: e.target.checked
                              ? [...prev.testCases, testCase.id]
                              : prev.testCases.filter(id => id !== testCase.id),
                          }));
                        }}
                      />
                      <span className="checkbox-text">{testCase.name}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div className="config-section">
                <h5>Options</h5>
                <div className="config-options">
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={batchTestConfig.parallel}
                      onChange={(e) => setBatchTestConfig(prev => ({
                        ...prev,
                        parallel: e.target.checked,
                      }))}
                    />
                    <span className="checkbox-text">Run tests in parallel</span>
                  </label>
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={batchTestConfig.stopOnFirstFailure}
                      onChange={(e) => setBatchTestConfig(prev => ({
                        ...prev,
                        stopOnFirstFailure: e.target.checked,
                      }))}
                    />
                    <span className="checkbox-text">Stop on first failure</span>
                  </label>
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={batchTestConfig.generateReport}
                      onChange={(e) => setBatchTestConfig(prev => ({
                        ...prev,
                        generateReport: e.target.checked,
                      }))}
                    />
                    <span className="checkbox-text">Generate detailed report</span>
                  </label>
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={batchTestConfig.includeDebugInfo}
                      onChange={(e) => setBatchTestConfig(prev => ({
                        ...prev,
                        includeDebugInfo: e.target.checked,
                      }))}
                    />
                    <span className="checkbox-text">Include debug information</span>
                  </label>
                </div>
              </div>

              <div className="batch-controls">
                <button
                  onClick={handleBatchTest}
                  // eslint-disable-next-line max-len
                  disabled={batchTestConfig.rules.length === 0 || batchTestConfig.testCases.length === 0 || isBatchTesting}
                  className="btn btn-primary batch-test-btn"
                >
                  {isBatchTesting ? '⏳ Running Batch Test...' : '🚀 Run Batch Test'}
                </button>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'history' && (
          <div className="test-history-panel">
            <div className="history-filters">
              <div className="filter-group">
                <label htmlFor="success-filter">Result:</label>
                <select
                  id="success-filter"
                  value={testFilter.success === null ? '' : testFilter.success.toString()}
                  onChange={(e) => setTestFilter(prev => ({
                    ...prev,
                    success: e.target.value === '' ? null : e.target.value === 'true',
                  }))}
                  className="form-control"
                >
                  <option value="">All Results</option>
                  <option value="true">Success Only</option>
                  <option value="false">Failures Only</option>
                </select>
              </div>

              <div className="filter-group">
                <label htmlFor="rule-filter">Rule:</label>
                <select
                  id="rule-filter"
                  value={testFilter.rule}
                  onChange={(e) => setTestFilter(prev => ({ ...prev, rule: e.target.value }))}
                  className="form-control"
                >
                  <option value="">All Rules</option>
                  {rules.map(rule => (
                    <option key={rule.id} value={rule.id}>{rule.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="test-results-list">
              {filteredTestResults.map(result => (
                <TestResultCard
                  key={result.id}
                  result={result}
                  rule={rules.find(r => r.id === result.ruleId)!}
                  testCase={testCases.find(tc => tc.id === result.testCaseId)}
                  onClick={() => setSelectedTestResult(result)}
                  selected={selectedTestResult?.id === result.id}
                />
              ))}
            </div>

            {selectedTestResult && (
              <div className="detailed-result-view">
                <TestResultDisplay
                  result={selectedTestResult}
                  rule={rules.find(r => r.id === selectedTestResult.ruleId)!}
                  showDebug={true}
                />
              </div>
            )}
          </div>
        )}

        {activeTab === 'analytics' && (
          <div className="analytics-panel">
            <div className="analytics-summary">
              <h4>Test Analytics</h4>
              <div className="summary-stats">
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.total}</div>
                  <div className="stat-label">Total Tests</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.successful}</div>
                  <div className="stat-label">Successful</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.failed}</div>
                  <div className="stat-label">Failed</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.successRate.toFixed(1)}%</div>
                  <div className="stat-label">Success Rate</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.avgExecutionTime.toFixed(0)}ms</div>
                  <div className="stat-label">Avg Execution Time</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{testStatistics.avgConfidence.toFixed(1)}%</div>
                  <div className="stat-label">Avg Confidence</div>
                </div>
              </div>
            </div>

            <div className="analytics-charts">
              <div className="chart-section">
                <h5>Success Rate by Rule</h5>
                <div className="rule-success-chart">
                  {rules.map(rule => {
                    const ruleResults = testResults.filter(r => r.ruleId === rule.id);
                    const successRate = ruleResults.length > 0
                      ? (ruleResults.filter(r => r.success).length / ruleResults.length) * 100
                      : 0;

                    return (
                      <div key={rule.id} className="rule-success-bar">
                        <div className="rule-name">{rule.name}</div>
                        <div className="success-bar">
                          <div
                            className="success-fill"
                            style={{ width: `${successRate}%` }}
                          />
                        </div>
                        <div className="success-percentage">{successRate.toFixed(1)}%</div>
                      </div>
                    );
                  })}
                </div>
              </div>

              <div className="chart-section">
                <h5>Performance Metrics</h5>
                <div className="performance-metrics">
                  <div className="metric-item">
                    <span className="metric-label">Average Execution Time:</span>
                    <span className="metric-value">{testStatistics.avgExecutionTime.toFixed(2)}ms</span>
                  </div>
                  <div className="metric-item">
                    <span className="metric-label">Fastest Test:</span>
                    <span className="metric-value">
                      {testResults.length > 0 ? Math.min(...testResults.map(r => r.executionTime)).toFixed(2) : 0}ms
                    </span>
                  </div>
                  <div className="metric-item">
                    <span className="metric-label">Slowest Test:</span>
                    <span className="metric-value">
                      {testResults.length > 0 ? Math.max(...testResults.map(r => r.executionTime)).toFixed(2) : 0}ms
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Test Result Display Component
 */
interface TestResultDisplayProps {
    result: TestResult;
    rule: TransformationRule;
    showDebug: boolean;
    expectedOutput?: string;
}

const TestResultDisplay: React.FC<TestResultDisplayProps> = ({
  result,
  rule,
  showDebug,
  expectedOutput,
}) => {
  const [activeDebugTab, setActiveDebugTab] = useState<'steps' | 'variables' | 'patterns' | 'constraints'>('steps');

  return (
    <div className="test-result-display">
      <div className="result-header">
        <div className="result-status">
          <span className={`status-indicator ${result.success ? 'success' : 'failure'}`}>
            {result.success ? '✅' : '❌'}
          </span>
          <span className="status-text">
            {result.success ? 'Test Passed' : 'Test Failed'}
          </span>
          <span className="confidence-score">
                        Confidence: {(result.confidence * 100).toFixed(1)}%
          </span>
        </div>

        <div className="result-metrics">
          <div className="metric">
            <span className="metric-label">Execution Time:</span>
            <span className="metric-value">{result.executionTime.toFixed(2)}ms</span>
          </div>
          <div className="metric">
            <span className="metric-label">Memory Usage:</span>
            <span className="metric-value">{(result.memoryUsage / 1024).toFixed(1)}KB</span>
          </div>
        </div>
      </div>

      <div className="result-content">
        <div className="code-comparison">
          <div className="code-section">
            <h5>Transformed Code</h5>
            <pre className="code-block transformed-code">
              {result.transformedCode}
            </pre>
          </div>

          {expectedOutput && (
            <div className="code-section">
              <h5>Expected Output</h5>
              <pre className="code-block expected-code">
                {expectedOutput}
              </pre>
            </div>
          )}
        </div>

        {(result.errors.length > 0 || result.warnings.length > 0 || result.suggestions.length > 0) && (
          <div className="result-messages">
            {result.errors.length > 0 && (
              <div className="message-section errors">
                <h6>❌ Errors</h6>
                <ul>
                  {result.errors.map((error, index) => (
                    <li key={index}>{error}</li>
                  ))}
                </ul>
              </div>
            )}

            {result.warnings.length > 0 && (
              <div className="message-section warnings">
                <h6>⚠️ Warnings</h6>
                <ul>
                  {result.warnings.map((warning, index) => (
                    <li key={index}>{warning}</li>
                  ))}
                </ul>
              </div>
            )}

            {result.suggestions.length > 0 && (
              <div className="message-section suggestions">
                <h6>💡 Suggestions</h6>
                <ul>
                  {result.suggestions.map((suggestion, index) => (
                    <li key={index}>{suggestion}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {showDebug && (
          <div className="debug-panel">
            <div className="debug-tabs">
              <button
                className={`debug-tab ${activeDebugTab === 'steps' ? 'active' : ''}`}
                onClick={() => setActiveDebugTab('steps')}
              >
                                🔍 Steps
              </button>
              <button
                className={`debug-tab ${activeDebugTab === 'variables' ? 'active' : ''}`}
                onClick={() => setActiveDebugTab('variables')}
              >
                                📦 Variables
              </button>
              <button
                className={`debug-tab ${activeDebugTab === 'patterns' ? 'active' : ''}`}
                onClick={() => setActiveDebugTab('patterns')}
              >
                                🎯 Patterns
              </button>
              <button
                className={`debug-tab ${activeDebugTab === 'constraints' ? 'active' : ''}`}
                onClick={() => setActiveDebugTab('constraints')}
              >
                                ⚙️ Constraints
              </button>
            </div>

            <div className="debug-content">
              {activeDebugTab === 'steps' && (
                <div className="debug-steps">
                  {result.debugInfo.steps.map(step => (
                    <div key={step.id} className={`debug-step ${step.success ? 'success' : 'failure'}`}>
                      <div className="step-header">
                        <span className="step-type">{step.type}</span>
                        <span className="step-duration">{step.duration.toFixed(2)}ms</span>
                        <span className={`step-status ${step.success ? 'success' : 'failure'}`}>
                          {step.success ? '✅' : '❌'}
                        </span>
                      </div>
                      <div className="step-description">{step.description}</div>
                      <div className="step-io">
                        <div className="step-input">
                          <strong>Input:</strong> {step.input}
                        </div>
                        <div className="step-output">
                          <strong>Output:</strong> {step.output}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {activeDebugTab === 'variables' && (
                <div className="debug-variables">
                  <table className="variables-table">
                    <thead>
                      <tr>
                        <th>Variable</th>
                        <th>Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      {Object.entries(result.debugInfo.variables).map(([name, value]) => (
                        <tr key={name}>
                          <td className="variable-name">{name}</td>
                          <td className="variable-value">{String(value)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {activeDebugTab === 'patterns' && (
                <div className="debug-patterns">
                  {result.debugInfo.patternMatches.map((match, index) => (
                    <div key={index} className={`pattern-match ${match.matched ? 'matched' : 'not-matched'}`}>
                      <div className="pattern-header">
                        <span className="pattern-text">{match.pattern}</span>
                        <span className={`match-status ${match.matched ? 'matched' : 'not-matched'}`}>
                          {match.matched ? '✅ Matched' : '❌ Not Matched'}
                        </span>
                      </div>
                      {match.matched && (
                        <div className="captured-variables">
                          <strong>Captured Variables:</strong>
                          {Object.entries(match.capturedVariables).map(([name, value]) => (
                            <div key={name} className="captured-variable">
                              {name}: {value}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {activeDebugTab === 'constraints' && (
                <div className="debug-constraints">
                  {result.debugInfo.constraintEvaluations.map((evaluation, index) => (
                    <div key={index} className={`constraint-evaluation ${evaluation.satisfied ? 'satisfied' : 'not-satisfied'}`}>
                      <div className="constraint-header">
                        <span className="constraint-type">{evaluation.constraint.type}</span>
                        <span className={`satisfaction-status ${evaluation.satisfied ? 'satisfied' : 'not-satisfied'}`}>
                          {evaluation.satisfied ? '✅ Satisfied' : '❌ Not Satisfied'}
                        </span>
                      </div>
                      <div className="constraint-condition">
                        <strong>Condition:</strong> {evaluation.constraint.condition}
                      </div>
                      <div className="constraint-reason">
                        <strong>Reason:</strong> {evaluation.reason}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Test Result Card Component
 */
interface TestResultCardProps {
    result: TestResult;
    rule: TransformationRule;
    testCase?: TestCase;
    onClick: () => void;
    selected: boolean;
}

const TestResultCard: React.FC<TestResultCardProps> = ({
  result,
  rule,
  testCase,
  onClick,
  selected,
}) => {
  return (
    <div className={`test-result-card ${selected ? 'selected' : ''} ${result.success ? 'success' : 'failure'}`} onClick={onClick}>
      <div className="result-card-header">
        <div className="result-info">
          <span className={`result-status ${result.success ? 'success' : 'failure'}`}>
            {result.success ? '✅' : '❌'}
          </span>
          <span className="rule-name">{rule.name}</span>
          {testCase && <span className="test-case-name">({testCase.name})</span>}
        </div>
        <div className="result-time">
          {result.timestamp.toLocaleString()}
        </div>
      </div>

      <div className="result-card-metrics">
        <div className="metric">
          <span className="metric-label">Confidence:</span>
          <span className="metric-value">{(result.confidence * 100).toFixed(1)}%</span>
        </div>
        <div className="metric">
          <span className="metric-label">Time:</span>
          <span className="metric-value">{result.executionTime.toFixed(2)}ms</span>
        </div>
        <div className="metric">
          <span className="metric-label">Memory:</span>
          <span className="metric-value">{(result.memoryUsage / 1024).toFixed(1)}KB</span>
        </div>
      </div>

      {result.errors.length > 0 && (
        <div className="result-card-errors">
          <span className="error-count">{result.errors.length} error{result.errors.length !== 1 ? 's' : ''}</span>
        </div>
      )}
    </div>
  );
};

/**
 * Mock function to transform code for testing purposes
 */
function mockTransformCode(sourceCode: string, rule: TransformationRule): string {
  // This is a simplified mock transformation
  // In production, this would use the actual rule engine

  if (rule.sourceLanguage === 'asp' && rule.targetLanguage === 'csharp') {
    return sourceCode
      .replace(/Response\.Write\s*\(/g, 'await Response.WriteAsync(')
      .replace(/<%\s*/g, '@{ ')
      .replace(/\s*%>/g, ' }')
      .replace(/Dim\s+(\w+)\s+As\s+(\w+)/g, '$2 $1')
      .replace(/End If/g, '}')
      .replace(/If\s+(.+)\s+Then/g, 'if ($1) {')
      .replace(/&/g, '+')
      .replace(/Request\.Form\("(\w+)"\)/g, 'Request.Form["$1"]')
      .replace(/<>\s*""/g, '!string.IsNullOrEmpty');
  }

  return sourceCode + '\n// Transformed by rule: ' + rule.name;
}

export default RuleTestingInterface;

