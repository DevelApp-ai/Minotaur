/**
 * Generated Rule Preview Component
 *
 * This component displays a preview of a generated transformation rule,
 * allowing users to review, test, modify, accept, or reject the rule.
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback, useMemo } from 'react';

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

interface GeneratedRulePreviewProps {
    rule: TransformationRule;
    examples: CodeExample[];
    onAccept: () => void;
    onReject: () => void;
    onModify: (modifiedRule: TransformationRule) => void;
    onTest: () => void;
}

interface RuleTestResult {
    example: CodeExample;
    success: boolean;
    transformedCode: string;
    confidence: number;
    issues: string[];
    suggestions: string[];
}

export const GeneratedRulePreview: React.FC<GeneratedRulePreviewProps> = ({
  rule,
  examples,
  onAccept,
  onReject,
  onModify,
  onTest,
}) => {
  const [activeTab, setActiveTab] = useState<'overview' | 'pattern' | 'transformation' | 'test' | 'edit'>('overview');
  const [isEditing, setIsEditing] = useState(false);
  const [editedRule, setEditedRule] = useState<TransformationRule>(rule);
  const [testResults, setTestResults] = useState<RuleTestResult[]>([]);
  const [isTestingRule, setIsTestingRule] = useState(false);

  const confidenceColor = useMemo(() => {
    if (rule.confidence >= 0.9) {
      return '#22c55e';
    } // green
    if (rule.confidence >= 0.7) {
      return '#f59e0b';
    } // yellow
    return '#ef4444'; // red
  }, [rule.confidence]);

  const handleEditToggle = useCallback(() => {
    if (isEditing) {
      // Save changes
      onModify(editedRule);
    }
    setIsEditing(!isEditing);
  }, [isEditing, editedRule, onModify]);

  const handleCancelEdit = useCallback(() => {
    setEditedRule(rule);
    setIsEditing(false);
  }, [rule]);

  const handleTestRule = useCallback(async () => {
    setIsTestingRule(true);
    try {
      // Simulate rule testing
      const results: RuleTestResult[] = [];

      for (const example of examples.slice(0, 3)) { // Test with first 3 examples
        await new Promise(resolve => setTimeout(resolve, 500)); // Simulate processing

        const result: RuleTestResult = {
          example,
          success: Math.random() > 0.2, // 80% success rate
          transformedCode: generateMockTransformation(example.sourceCode, rule),
          confidence: 0.7 + Math.random() * 0.3,
          issues: Math.random() > 0.7 ? ['Minor syntax adjustment needed'] : [],
          suggestions: Math.random() > 0.5 ? ['Consider adding error handling'] : [],
        };
        results.push(result);
      }

      setTestResults(results);
      setActiveTab('test');
      onTest();
    } finally {
      setIsTestingRule(false);
    }
  }, [examples, rule, onTest]);

  const renderOverviewTab = () => (
    <div className="rule-overview">
      <div className="rule-header">
        <div className="rule-title-section">
          <h3 className="rule-name">{rule.name}</h3>
          <div className="rule-confidence">
            <div
              className="confidence-bar"
              style={{
                backgroundColor: confidenceColor,
                width: `${rule.confidence * 100}%`,
              }}
            />
            <span className="confidence-text">
              {(rule.confidence * 100).toFixed(1)}% confidence
            </span>
          </div>
        </div>

        <div className="rule-metadata">
          <div className="metadata-item">
            <span className="metadata-label">Language Pair:</span>
            <span className="metadata-value">
              {rule.sourceLanguage.toUpperCase()} → {rule.targetLanguage.toUpperCase()}
            </span>
          </div>
          <div className="metadata-item">
            <span className="metadata-label">Created By:</span>
            <span className="metadata-value">{rule.createdBy}</span>
          </div>
          <div className="metadata-item">
            <span className="metadata-label">Examples Used:</span>
            <span className="metadata-value">{rule.examples.length}</span>
          </div>
        </div>
      </div>

      <div className="rule-description">
        <h4>Description</h4>
        <p>{rule.description}</p>
      </div>

      <div className="rule-tags">
        <h4>Tags</h4>
        <div className="tags-list">
          {rule.tags.map(tag => (
            <span key={tag} className="rule-tag">{tag}</span>
          ))}
        </div>
      </div>

      <div className="rule-constraints">
        <h4>Constraints</h4>
        {rule.constraints.length > 0 ? (
          <ul className="constraints-list">
            {rule.constraints.map((constraint, index) => (
              <li key={index} className="constraint-item">
                <span className="constraint-type">{constraint.type}:</span>
                <span className="constraint-condition">{constraint.condition}</span>
                {constraint.required && <span className="constraint-required">*</span>}
              </li>
            ))}
          </ul>
        ) : (
          <p className="no-constraints">No specific constraints</p>
        )}
      </div>
    </div>
  );

  const renderPatternTab = () => (
    <div className="rule-pattern">
      <h4>Pattern Definition</h4>
      <div className="pattern-info">
        <div className="pattern-type">
          <span className="info-label">Pattern Type:</span>
          <span className="info-value">{rule.pattern.type}</span>
        </div>
      </div>

      <div className="pattern-code">
        <h5>Pattern</h5>
        <pre className="code-block pattern-block">
          <code>{rule.pattern.pattern}</code>
        </pre>
      </div>

      {Object.keys(rule.pattern.variables).length > 0 && (
        <div className="pattern-variables">
          <h5>Variables</h5>
          <table className="variables-table">
            <thead>
              <tr>
                <th>Variable</th>
                <th>Extraction</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries(rule.pattern.variables).map(([name, extraction]) => (
                <tr key={name}>
                  <td className="variable-name">${name}</td>
                  <td className="variable-extraction">{extraction}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {rule.pattern.context && rule.pattern.context.length > 0 && (
        <div className="pattern-context">
          <h5>Context Requirements</h5>
          <ul className="context-list">
            {rule.pattern.context.map((ctx, index) => (
              <li key={index} className="context-item">{ctx}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );

  const renderTransformationTab = () => (
    <div className="rule-transformation">
      <h4>Transformation Definition</h4>
      <div className="transformation-info">
        <div className="transformation-type">
          <span className="info-label">Transformation Type:</span>
          <span className="info-value">{rule.transformation.type}</span>
        </div>
      </div>

      <div className="transformation-template">
        <h5>Template</h5>
        <pre className="code-block transformation-block">
          <code>{rule.transformation.template}</code>
        </pre>
      </div>

      {Object.keys(rule.transformation.parameters).length > 0 && (
        <div className="transformation-parameters">
          <h5>Parameters</h5>
          <table className="parameters-table">
            <thead>
              <tr>
                <th>Parameter</th>
                <th>Value</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries(rule.transformation.parameters).map(([name, value]) => (
                <tr key={name}>
                  <td className="parameter-name">{name}</td>
                  <td className="parameter-value">
                    {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {rule.transformation.postProcessing && rule.transformation.postProcessing.length > 0 && (
        <div className="post-processing">
          <h5>Post-Processing Steps</h5>
          <ol className="post-processing-list">
            {rule.transformation.postProcessing.map((step, index) => (
              <li key={index} className="post-processing-step">{step}</li>
            ))}
          </ol>
        </div>
      )}
    </div>
  );

  const renderTestTab = () => (
    <div className="rule-test">
      <div className="test-header">
        <h4>Rule Testing Results</h4>
        <button
          onClick={handleTestRule}
          disabled={isTestingRule}
          className="btn btn-primary test-btn"
        >
          {isTestingRule ? '⏳ Testing...' : '🧪 Run Test'}
        </button>
      </div>

      {testResults.length > 0 && (
        <div className="test-results">
          <div className="test-summary">
            <div className="summary-stat">
              <span className="stat-label">Success Rate:</span>
              <span className="stat-value">
                {((testResults.filter(r => r.success).length / testResults.length) * 100).toFixed(0)}%
              </span>
            </div>
            <div className="summary-stat">
              <span className="stat-label">Average Confidence:</span>
              <span className="stat-value">
                {(testResults.reduce((sum, r) => sum + r.confidence, 0) / testResults.length * 100).toFixed(1)}%
              </span>
            </div>
          </div>

          <div className="test-cases">
            {testResults.map((result, index) => (
              <div key={index} className={`test-case ${result.success ? 'success' : 'failure'}`}>
                <div className="test-case-header">
                  <span className="test-case-name">{result.example.name}</span>
                  <span className={`test-case-status ${result.success ? 'success' : 'failure'}`}>
                    {result.success ? '✅ Success' : '❌ Failed'}
                  </span>
                  <span className="test-case-confidence">
                    {(result.confidence * 100).toFixed(1)}%
                  </span>
                </div>

                <div className="test-case-content">
                  <div className="code-comparison">
                    <div className="source-code">
                      <h6>Source Code</h6>
                      <pre className="code-block">
                        <code>{result.example.sourceCode}</code>
                      </pre>
                    </div>
                    <div className="transformed-code">
                      <h6>Transformed Code</h6>
                      <pre className="code-block">
                        <code>{result.transformedCode}</code>
                      </pre>
                    </div>
                  </div>

                  {result.issues.length > 0 && (
                    <div className="test-issues">
                      <h6>Issues</h6>
                      <ul className="issues-list">
                        {result.issues.map((issue, issueIndex) => (
                          <li key={issueIndex} className="issue-item">{issue}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {result.suggestions.length > 0 && (
                    <div className="test-suggestions">
                      <h6>Suggestions</h6>
                      <ul className="suggestions-list">
                        {result.suggestions.map((suggestion, suggestionIndex) => (
                          <li key={suggestionIndex} className="suggestion-item">{suggestion}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {testResults.length === 0 && !isTestingRule && (
        <div className="no-test-results">
          <p>Click &quot;Run Test&quot; to test this rule against the provided examples.</p>
        </div>
      )}
    </div>
  );

  const renderEditTab = () => (
    <div className="rule-edit">
      <h4>Edit Rule</h4>
      <div className="edit-form">
        <div className="form-row">
          <label htmlFor="edit-name">Rule Name:</label>
          <input
            id="edit-name"
            type="text"
            value={editedRule.name}
            onChange={(e) => setEditedRule({...editedRule, name: e.target.value})}
            className="form-control"
          />
        </div>

        <div className="form-row">
          <label htmlFor="edit-description">Description:</label>
          <textarea
            id="edit-description"
            value={editedRule.description}
            onChange={(e) => setEditedRule({...editedRule, description: e.target.value})}
            className="form-control"
            rows={3}
          />
        </div>

        <div className="form-row">
          <label htmlFor="edit-pattern">Pattern:</label>
          <textarea
            id="edit-pattern"
            value={editedRule.pattern.pattern}
            onChange={(e) => setEditedRule({
              ...editedRule,
              pattern: {...editedRule.pattern, pattern: e.target.value},
            })}
            className="form-control code-textarea"
            rows={4}
          />
        </div>

        <div className="form-row">
          <label htmlFor="edit-transformation">Transformation Template:</label>
          <textarea
            id="edit-transformation"
            value={editedRule.transformation.template}
            onChange={(e) => setEditedRule({
              ...editedRule,
              transformation: {...editedRule.transformation, template: e.target.value},
            })}
            className="form-control code-textarea"
            rows={4}
          />
        </div>

        <div className="form-row">
          <label htmlFor="edit-tags">Tags (comma-separated):</label>
          <input
            id="edit-tags"
            type="text"
            value={editedRule.tags.join(', ')}
            onChange={(e) => setEditedRule({
              ...editedRule,
              tags: e.target.value.split(',').map(tag => tag.trim()).filter(tag => tag),
            })}
            className="form-control"
          />
        </div>

        <div className="edit-actions">
          <button
            onClick={handleEditToggle}
            className="btn btn-primary save-btn"
          >
                        Save Changes
          </button>
          <button
            onClick={handleCancelEdit}
            className="btn btn-secondary cancel-btn"
          >
                        Cancel
          </button>
        </div>
      </div>
    </div>
  );

  return (
    <div className="generated-rule-preview">
      {/* Tab Navigation */}
      <div className="preview-tabs">
        <button
          className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
                    📋 Overview
        </button>
        <button
          className={`tab-btn ${activeTab === 'pattern' ? 'active' : ''}`}
          onClick={() => setActiveTab('pattern')}
        >
                    🔍 Pattern
        </button>
        <button
          className={`tab-btn ${activeTab === 'transformation' ? 'active' : ''}`}
          onClick={() => setActiveTab('transformation')}
        >
                    🔄 Transformation
        </button>
        <button
          className={`tab-btn ${activeTab === 'test' ? 'active' : ''}`}
          onClick={() => setActiveTab('test')}
        >
                    🧪 Test
        </button>
        <button
          className={`tab-btn ${activeTab === 'edit' ? 'active' : ''}`}
          onClick={() => setActiveTab('edit')}
        >
                    ✏️ Edit
        </button>
      </div>

      {/* Tab Content */}
      <div className="preview-content">
        {activeTab === 'overview' && renderOverviewTab()}
        {activeTab === 'pattern' && renderPatternTab()}
        {activeTab === 'transformation' && renderTransformationTab()}
        {activeTab === 'test' && renderTestTab()}
        {activeTab === 'edit' && renderEditTab()}
      </div>

      {/* Action Buttons */}
      <div className="preview-actions">
        <button
          onClick={onAccept}
          className="btn btn-success accept-btn"
        >
                    ✅ Accept Rule
        </button>
        <button
          onClick={onReject}
          className="btn btn-danger reject-btn"
        >
                    ❌ Reject Rule
        </button>
        <button
          onClick={handleTestRule}
          disabled={isTestingRule}
          className="btn btn-secondary test-rule-btn"
        >
          {isTestingRule ? '⏳ Testing...' : '🧪 Test Rule'}
        </button>
      </div>
    </div>
  );
};

/**
 * Generate a mock transformation for testing purposes
 */
function generateMockTransformation(sourceCode: string, rule: TransformationRule): string {
  // This is a simplified mock transformation
  // In production, this would use the actual rule engine

  if (rule.sourceLanguage === 'asp' && rule.targetLanguage === 'csharp') {
    return sourceCode
      .replace(/Response\.Write\s*\(/g, 'await Response.WriteAsync(')
      .replace(/<%\s*/g, '@{ ')
      .replace(/\s*%>/g, ' }')
      .replace(/Dim\s+(\w+)\s+As\s+(\w+)/g, '$2 $1')
      .replace(/End If/g, '}')
      .replace(/If\s+(.+)\s+Then/g, 'if ($1) {');
  }

  return sourceCode + '\n// Transformed by rule: ' + rule.name;
}

export default GeneratedRulePreview;

