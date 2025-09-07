/**
 * Rule Creation Workspace Component
 *
 * This is the main integration component that brings together all rule creation
 * and management UIs into a unified workspace. It provides seamless navigation
 * between different modes and maintains state consistency across components.
 *
 * Key Features:
 * - Unified workspace with tabbed interface
 * - State management across all rule creation modes
 * - Integration with LLM-agnostic translation engine orchestrator
 * - Real-time rule synchronization and updates
 * - Workflow guidance and user assistance
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { LLMRuleGeneratorPanel } from './LLMRuleGeneratorPanel';
import { RuleManagementDashboard } from './RuleManagementDashboard';
import { VisualRuleBuilder } from './VisualRuleBuilder';
import { RuleTestingInterface } from './RuleTestingInterface';
import { TranslationEngineOrchestrator, OrchestratorConfig, EngineSelectionStrategy } from '../engines/TranslationEngineOrchestrator';

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

interface RuleCreationWorkspaceProps {
    orchestrator: TranslationEngineOrchestrator;
    onRulesChanged: (rules: TransformationRule[]) => void;
    initialRules?: TransformationRule[];
    supportedLanguages?: string[];
    className?: string;
}

type WorkspaceMode = 'dashboard' | 'llm-generator' | 'visual-builder' | 'testing' | 'getting-started';

interface WorkspaceState {
    mode: WorkspaceMode;
    rules: TransformationRule[];
    selectedRule: TransformationRule | null;
    editingRule: TransformationRule | null;
    showWelcome: boolean;
    recentActivity: ActivityItem[];
}

interface ActivityItem {
    id: string;
    type: 'rule-created' | 'rule-updated' | 'rule-tested' | 'rule-deleted';
    description: string;
    timestamp: Date;
    ruleId?: string;
}

export const RuleCreationWorkspace: React.FC<RuleCreationWorkspaceProps> = ({
  orchestrator,
  onRulesChanged,
  initialRules = [],
  supportedLanguages = ['asp', 'vbscript', 'csharp', 'javascript', 'typescript', 'python', 'java'],
  className = '',
}) => {
  const [workspaceState, setWorkspaceState] = useState<WorkspaceState>({
    mode: initialRules.length === 0 ? 'getting-started' : 'dashboard',
    rules: initialRules,
    selectedRule: null,
    editingRule: null,
    showWelcome: initialRules.length === 0,
    recentActivity: [],
  });

  const [orchestratorConfig, setOrchestratorConfig] = useState<OrchestratorConfig>({
    selectionStrategy: EngineSelectionStrategy.PRIORITY,
    enableFallback: true,
    maxEnginesPerTranslation: 3,
    minConfidenceThreshold: 0.7,
    maxCostPerTranslation: 1.0,
    maxTimePerTranslation: 5000,
    engineConfigs: {
      'rule-based': {
        settings: {
          strictMode: false,
          enableOptimizations: true,
        },
      } as any,
      'pattern-based': {
        settings: {
          similarityThreshold: 0.7,
          maxPatterns: 1000,
          learningEnabled: true,
        },
      } as any,
    },
    enginePriorities: {
      'rule-based': 100,
      'pattern-based': 80,
    },
    qualityThresholds: {
      minSyntacticCorrectness: 0.8,
      minSemanticPreservation: 0.7,
      minOverallQuality: 0.75,
    },
    performanceThresholds: {
      maxResponseTime: 5000,
      maxMemoryUsage: 100,
      minSuccessRate: 0.9,
    },
    healthCheck: {
      interval: 30000,
      timeout: 5000,
      failureThreshold: 3,
      recoveryInterval: 60000,
    },
  });

  // Sample rules for demonstration
  const sampleRules: TransformationRule[] = useMemo(() => [
    {
      id: 'rule_asp_response_write',
      name: 'ASP Response.Write to C# WriteAsync',
      description: 'Convert ASP Classic Response.Write calls to C# async WriteAsync',
      sourceLanguage: 'asp',
      targetLanguage: 'csharp',
      pattern: {
        type: 'ast-pattern',
        pattern: 'CallExpression[callee.object.name="Response"][callee.property.name="Write"]',
        variables: { content: 'arguments[0]' },
        context: ['web-page', 'server-side'],
      },
      transformation: {
        type: 'template',
        template: 'await Response.WriteAsync(${content})',
        parameters: { async: true },
        postProcessing: ['add-using-statements'],
      },
      constraints: [
        {
          type: 'context',
          condition: 'in-web-context',
          value: true,
          required: true,
        },
      ],
      confidence: 0.95,
      examples: [
        'Response.Write("Hello World")',
        'Response.Write(userName)',
        'Response.Write("<h1>" & title & "</h1>")',
      ],
      tags: ['asp', 'response', 'web', 'async'],
      createdBy: 'user',
      createdAt: new Date('2024-01-15'),
      lastModified: new Date('2024-01-20'),
      usageCount: 45,
      successRate: 0.92,
      enabled: true,
      version: '1.2',
      category: 'web',
      complexity: 'simple',
      quality: 0.9,
    },
    {
      id: 'rule_vb_dim_statement',
      name: 'VBScript Dim to C# Variable Declaration',
      description: 'Convert VBScript Dim statements to C# typed variable declarations',
      sourceLanguage: 'vbscript',
      targetLanguage: 'csharp',
      pattern: {
        type: 'regex',
        pattern: 'Dim\\s+(\\w+)(?:\\s+As\\s+(\\w+))?',
        variables: { varName: '$1', varType: '$2' },
        context: ['variable-declaration'],
      },
      transformation: {
        type: 'template',
        template: '${inferredType} ${varName}',
        parameters: { inferType: true },
        postProcessing: ['infer-type-from-usage'],
      },
      constraints: [
        {
          type: 'syntax',
          condition: 'valid-identifier',
          value: '${varName}',
          required: true,
        },
      ],
      confidence: 0.88,
      examples: [
        'Dim userName As String',
        'Dim count As Integer',
        'Dim isValid',
      ],
      tags: ['vbscript', 'variable', 'declaration', 'type-inference'],
      createdBy: 'llm',
      createdAt: new Date('2024-01-10'),
      lastModified: new Date('2024-01-18'),
      usageCount: 32,
      successRate: 0.85,
      enabled: true,
      version: '1.1',
      category: 'variables',
      complexity: 'moderate',
      quality: 0.85,
    },
    {
      id: 'rule_asp_form_processing',
      name: 'ASP Form Processing to C# Model Binding',
      description: 'Convert ASP Classic form processing to C# MVC model binding',
      sourceLanguage: 'asp',
      targetLanguage: 'csharp',
      pattern: {
        type: 'ast-pattern',
        pattern: 'MemberExpression[object.object.name="Request"][object.property.name="Form"]',
        variables: { fieldName: 'property.value' },
        context: ['form-processing', 'web-request'],
      },
      transformation: {
        type: 'template',
        template: 'model.${PascalCase(fieldName)}',
        parameters: { generateModel: true, useModelBinding: true },
        postProcessing: ['generate-model-class', 'add-validation-attributes'],
      },
      constraints: [
        {
          type: 'framework',
          condition: 'mvc-available',
          value: true,
          required: true,
        },
        {
          type: 'context',
          condition: 'in-controller-action',
          value: true,
          required: false,
        },
      ],
      confidence: 0.78,
      examples: [
        'Request.Form("username")',
        'Request.Form("email")',
        'Request.Form("password")',
      ],
      tags: ['asp', 'form', 'model-binding', 'mvc', 'web'],
      createdBy: 'pattern-learning',
      createdAt: new Date('2024-01-12'),
      lastModified: new Date('2024-01-22'),
      usageCount: 18,
      successRate: 0.72,
      enabled: true,
      version: '1.0',
      category: 'web',
      complexity: 'complex',
      quality: 0.75,
    },
  ], []);

  // Initialize with sample rules if no initial rules provided
  useEffect(() => {
    if (initialRules.length === 0 && workspaceState.rules.length === 0) {
      setWorkspaceState(prev => ({
        ...prev,
        rules: sampleRules,
      }));
    }
  }, [initialRules, sampleRules, workspaceState.rules.length]);

  // Notify parent when rules change
  useEffect(() => {
    onRulesChanged(workspaceState.rules);
  }, [workspaceState.rules, onRulesChanged]);

  const addActivity = useCallback((activity: Omit<ActivityItem, 'id' | 'timestamp'>) => {
    const newActivity: ActivityItem = {
      ...activity,
      id: `activity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      timestamp: new Date(),
    };

    setWorkspaceState(prev => ({
      ...prev,
      recentActivity: [newActivity, ...prev.recentActivity.slice(0, 9)], // Keep last 10 activities
    }));
  }, []);

  const handleModeChange = useCallback((mode: WorkspaceMode) => {
    setWorkspaceState(prev => ({
      ...prev,
      mode,
      showWelcome: false,
    }));
  }, []);

  const handleRuleCreated = useCallback((rule: TransformationRule) => {
    setWorkspaceState(prev => ({
      ...prev,
      rules: [rule, ...prev.rules],
      selectedRule: rule,
    }));

    addActivity({
      type: 'rule-created',
      description: `Created rule: ${rule.name}`,
      ruleId: rule.id,
    });
  }, [addActivity]);

  const handleRuleUpdated = useCallback((updatedRule: TransformationRule) => {
    setWorkspaceState(prev => ({
      ...prev,
      rules: prev.rules.map(rule => rule.id === updatedRule.id ? updatedRule : rule),
      selectedRule: updatedRule,
      editingRule: null,
    }));

    addActivity({
      type: 'rule-updated',
      description: `Updated rule: ${updatedRule.name}`,
      ruleId: updatedRule.id,
    });
  }, [addActivity]);

  const handleRuleDeleted = useCallback((ruleId: string) => {
    const deletedRule = workspaceState.rules.find(r => r.id === ruleId);

    setWorkspaceState(prev => ({
      ...prev,
      rules: prev.rules.filter(rule => rule.id !== ruleId),
      selectedRule: prev.selectedRule?.id === ruleId ? null : prev.selectedRule,
      editingRule: prev.editingRule?.id === ruleId ? null : prev.editingRule,
    }));

    if (deletedRule) {
      addActivity({
        type: 'rule-deleted',
        description: `Deleted rule: ${deletedRule.name}`,
        ruleId: ruleId,
      });
    }
  }, [workspaceState.rules, addActivity]);

  const handleRuleEdit = useCallback((rule: TransformationRule) => {
    setWorkspaceState(prev => ({
      ...prev,
      editingRule: rule,
      mode: 'visual-builder',
    }));
  }, []);

  const handleRuleTest = useCallback((rule: TransformationRule) => {
    setWorkspaceState(prev => ({
      ...prev,
      selectedRule: rule,
      mode: 'testing',
    }));
  }, []);

  const workspaceStats = useMemo(() => {
    const totalRules = workspaceState.rules.length;
    const enabledRules = workspaceState.rules.filter(r => r.enabled).length;
    const avgSuccessRate = totalRules > 0
      ? workspaceState.rules.reduce((sum, r) => sum + r.successRate, 0) / totalRules
      : 0;
    const totalUsage = workspaceState.rules.reduce((sum, r) => sum + r.usageCount, 0);

    return {
      totalRules,
      enabledRules,
      avgSuccessRate: avgSuccessRate * 100,
      totalUsage,
      recentActivity: workspaceState.recentActivity.length,
    };
  }, [workspaceState.rules, workspaceState.recentActivity]);

  return (
    <div className={`rule-creation-workspace ${className}`}>
      {/* Workspace Header */}
      <div className="workspace-header">
        <div className="header-title">
          <h1>🏗️ Rule Creation Workspace</h1>
          <p>Create, manage, and test transformation rules with AI assistance</p>
        </div>

        <div className="workspace-stats">
          <div className="stat-item">
            <span className="stat-value">{workspaceStats.totalRules}</span>
            <span className="stat-label">Total Rules</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{workspaceStats.enabledRules}</span>
            <span className="stat-label">Enabled</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{workspaceStats.avgSuccessRate.toFixed(1)}%</span>
            <span className="stat-label">Avg Success</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{workspaceStats.totalUsage}</span>
            <span className="stat-label">Total Usage</span>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="workspace-navigation">
        <button
          className={`nav-tab ${workspaceState.mode === 'getting-started' ? 'active' : ''}`}
          onClick={() => handleModeChange('getting-started')}
        >
                    🚀 Getting Started
        </button>
        <button
          className={`nav-tab ${workspaceState.mode === 'dashboard' ? 'active' : ''}`}
          onClick={() => handleModeChange('dashboard')}
        >
                    📊 Rule Dashboard
        </button>
        <button
          className={`nav-tab ${workspaceState.mode === 'llm-generator' ? 'active' : ''}`}
          onClick={() => handleModeChange('llm-generator')}
        >
                    🤖 AI Generator
        </button>
        <button
          className={`nav-tab ${workspaceState.mode === 'visual-builder' ? 'active' : ''}`}
          onClick={() => handleModeChange('visual-builder')}
        >
                    🎨 Visual Builder
        </button>
        <button
          className={`nav-tab ${workspaceState.mode === 'testing' ? 'active' : ''}`}
          onClick={() => handleModeChange('testing')}
        >
                    🧪 Testing
        </button>
      </div>

      {/* Main Content Area */}
      <div className="workspace-content">
        {workspaceState.mode === 'getting-started' && (
          <GettingStartedPanel
            onModeSelect={handleModeChange}
            hasRules={workspaceState.rules.length > 0}
            recentActivity={workspaceState.recentActivity}
            orchestratorConfig={orchestratorConfig}
          />
        )}

        {workspaceState.mode === 'dashboard' && (
          <RuleManagementDashboard
            rules={workspaceState.rules}
            onRuleEdit={handleRuleEdit}
            onRuleDelete={handleRuleDeleted}
            onRuleTest={handleRuleTest}
            onRuleToggle={(ruleId, enabled) => {
              const rule = workspaceState.rules.find(r => r.id === ruleId);
              if (rule) {
                handleRuleUpdated({ ...rule, enabled });
              }
            }}
            onRuleDuplicate={(rule) => {
              const duplicatedRule: TransformationRule = {
                ...rule,
                id: `${rule.id}_copy_${Date.now()}`,
                name: `${rule.name} (Copy)`,
                createdAt: new Date(),
                lastModified: new Date(),
                usageCount: 0,
                version: '1.0',
              };
              handleRuleCreated(duplicatedRule);
            }}
            onRulesImport={(rules) => {
              rules.forEach(rule => handleRuleCreated(rule));
            }}
            onRulesExport={(ruleIds) => {
              const rulesToExport = workspaceState.rules.filter(rule => ruleIds.includes(rule.id));
              // Export logic would go here
    // eslint-disable-next-line no-console
              console.log('Exporting rules:', rulesToExport);
            }}
            className="dashboard-panel"
          />
        )}

        {workspaceState.mode === 'llm-generator' && (
          <LLMRuleGeneratorPanel
            orchestrator={orchestrator}
            existingRules={workspaceState.rules}
            onRuleGenerated={(rules) => {
              rules.forEach(rule => handleRuleCreated(rule));
            }}
            onRuleSelected={(rule) => {
              setWorkspaceState(prev => ({ ...prev, selectedRule: rule }));
            }}
            className="llm-generator-panel"
          />
        )}

        {workspaceState.mode === 'visual-builder' && (
          <VisualRuleBuilder
            initialRule={workspaceState.editingRule || undefined}
            onRuleCreated={handleRuleCreated}
            onRuleUpdated={handleRuleUpdated}
            onCancel={() => {
              setWorkspaceState(prev => ({ ...prev, editingRule: null }));
              handleModeChange('dashboard');
            }}
            supportedLanguages={supportedLanguages}
            className="visual-builder-panel"
          />
        )}

        {workspaceState.mode === 'testing' && (
          <RuleTestingInterface
            rules={workspaceState.rules}
            onRuleUpdate={handleRuleUpdated}
            className="testing-panel"
          />
        )}
      </div>

      {/* Activity Sidebar */}
      <div className="activity-sidebar">
        <div className="sidebar-header">
          <h3>Recent Activity</h3>
        </div>

        <div className="activity-list">
          {workspaceState.recentActivity.length === 0 ? (
            <div className="no-activity">
              <div className="no-activity-icon">📝</div>
              <div className="no-activity-text">No recent activity</div>
            </div>
          ) : (
            workspaceState.recentActivity.map(activity => (
              <div key={activity.id} className="activity-item">
                <div className="activity-icon">
                  {activity.type === 'rule-created' && '✨'}
                  {activity.type === 'rule-updated' && '📝'}
                  {activity.type === 'rule-tested' && '🧪'}
                  {activity.type === 'rule-deleted' && '🗑️'}
                </div>
                <div className="activity-content">
                  <div className="activity-description">{activity.description}</div>
                  <div className="activity-time">
                    {activity.timestamp.toLocaleTimeString()}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Engine Status Panel */}
        <div className="engine-status-panel">
          <div className="panel-header">
            <h4>Engine Status</h4>
          </div>

          <div className="engine-list">
            <div className="engine-item">
              <div className="engine-name">Rule-Based</div>
              <div className={`engine-status ${orchestratorConfig.enableRuleBased ? 'active' : 'inactive'}`}>
                {orchestratorConfig.enableRuleBased ? '🟢 Active' : '🔴 Inactive'}
              </div>
            </div>
            <div className="engine-item">
              <div className="engine-name">Pattern-Based</div>
              <div className={`engine-status ${orchestratorConfig.enablePatternBased ? 'active' : 'inactive'}`}>
                {orchestratorConfig.enablePatternBased ? '🟢 Active' : '🔴 Inactive'}
              </div>
            </div>
            <div className="engine-item">
              <div className="engine-name">LLM-Enhanced</div>
              <div className={`engine-status ${orchestratorConfig.enableLLM ? 'active' : 'inactive'}`}>
                {orchestratorConfig.enableLLM ? '🟢 Active' : '🔴 Inactive'}
              </div>
            </div>
          </div>

          <div className="engine-config">
            <div className="config-item">
              <span className="config-label">Strategy:</span>
              <span className="config-value">{orchestratorConfig.strategy}</span>
            </div>
            <div className="config-item">
              <span className="config-label">Max Cost:</span>
              <span className="config-value">${orchestratorConfig.maxCost}</span>
            </div>
            <div className="config-item">
              <span className="config-label">Min Confidence:</span>
              <span className="config-value">{(orchestratorConfig.minConfidence * 100).toFixed(0)}%</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

/**
 * Getting Started Panel Component
 */
interface GettingStartedPanelProps {
    onModeSelect: (mode: WorkspaceMode) => void;
    hasRules: boolean;
    recentActivity: ActivityItem[];
    orchestratorConfig: OrchestratorConfig;
}

const GettingStartedPanel: React.FC<GettingStartedPanelProps> = ({
  onModeSelect,
  hasRules,
  recentActivity,
  orchestratorConfig,
}) => {
  return (
    <div className="getting-started-panel">
      <div className="welcome-section">
        <div className="welcome-icon">🎯</div>
        <h2>Welcome to the Rule Creation Workspace</h2>
        <p>
                    Create powerful transformation rules to convert legacy code to modern frameworks.
                    Choose from AI-assisted generation, visual building, or comprehensive management tools.
        </p>
      </div>

      <div className="quick-actions">
        <h3>Quick Actions</h3>
        <div className="action-cards">
          <div className="action-card" onClick={() => onModeSelect('llm-generator')}>
            <div className="card-icon">🤖</div>
            <div className="card-title">Generate with AI</div>
            <div className="card-description">
                            Let AI create rules from code examples and natural language descriptions
            </div>
            <div className="card-features">
              <span className="feature">✨ Smart pattern recognition</span>
              <span className="feature">🎯 Context-aware transformations</span>
              <span className="feature">📚 Learn from examples</span>
            </div>
          </div>

          <div className="action-card" onClick={() => onModeSelect('visual-builder')}>
            <div className="card-icon">🎨</div>
            <div className="card-title">Visual Builder</div>
            <div className="card-description">
                            Build rules visually with drag-and-drop components and templates
            </div>
            <div className="card-features">
              <span className="feature">🖱️ Drag-and-drop interface</span>
              <span className="feature">🧩 Pre-built components</span>
              <span className="feature">👁️ Real-time preview</span>
            </div>
          </div>

          {hasRules && (
            <div className="action-card" onClick={() => onModeSelect('dashboard')}>
              <div className="card-icon">📊</div>
              <div className="card-title">Manage Rules</div>
              <div className="card-description">
                                Organize, edit, and monitor your existing transformation rules
              </div>
              <div className="card-features">
                <span className="feature">📋 Rule library</span>
                <span className="feature">📈 Performance analytics</span>
                <span className="feature">🔍 Advanced search</span>
              </div>
            </div>
          )}

          <div className="action-card" onClick={() => onModeSelect('testing')}>
            <div className="card-icon">🧪</div>
            <div className="card-title">Test Rules</div>
            <div className="card-description">
                            Validate rules with comprehensive testing and debugging tools
            </div>
            <div className="card-features">
              <span className="feature">🎯 Single & batch testing</span>
              <span className="feature">🐛 Advanced debugging</span>
              <span className="feature">📊 Performance metrics</span>
            </div>
          </div>
        </div>
      </div>

      <div className="system-status">
        <h3>System Status</h3>
        <div className="status-grid">
          <div className="status-item">
            <div className="status-label">Translation Engines</div>
            <div className="status-value">
              {[orchestratorConfig.enableRuleBased, orchestratorConfig.enablePatternBased, orchestratorConfig.enableLLM]
                .filter(Boolean).length} / 3 Active
            </div>
          </div>
          <div className="status-item">
            <div className="status-label">Recent Activity</div>
            <div className="status-value">{recentActivity.length} items</div>
          </div>
          <div className="status-item">
            <div className="status-label">Strategy</div>
            <div className="status-value">{orchestratorConfig.strategy}</div>
          </div>
          <div className="status-item">
            <div className="status-label">Health Checks</div>
            <div className="status-value">
              {orchestratorConfig.enableHealthChecks ? '🟢 Enabled' : '🔴 Disabled'}
            </div>
          </div>
        </div>
      </div>

      <div className="learning-resources">
        <h3>Learning Resources</h3>
        <div className="resource-list">
          <div className="resource-item">
            <div className="resource-icon">📖</div>
            <div className="resource-content">
              <div className="resource-title">Rule Creation Guide</div>
              <div className="resource-description">
                                Learn best practices for creating effective transformation rules
              </div>
            </div>
          </div>
          <div className="resource-item">
            <div className="resource-icon">🎥</div>
            <div className="resource-content">
              <div className="resource-title">Video Tutorials</div>
              <div className="resource-description">
                                Watch step-by-step tutorials for each creation method
              </div>
            </div>
          </div>
          <div className="resource-item">
            <div className="resource-icon">💡</div>
            <div className="resource-content">
              <div className="resource-title">Example Patterns</div>
              <div className="resource-description">
                                Browse common transformation patterns and templates
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RuleCreationWorkspace;

