/**
 * Visual Rule Builder Component
 *
 * This component provides a drag-and-drop visual interface for creating
 * transformation rules from scratch. Users can build patterns and transformations
 * using visual components, templates, and interactive editors.
 *
 * Key Features:
 * - Drag-and-drop pattern builder
 * - Visual transformation editor
 * - Pattern templates and snippets
 * - Real-time rule preview
 * - Code syntax highlighting
 * - Rule validation and testing
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

interface VisualRuleBuilderProps {
    initialRule?: Partial<TransformationRule>;
    onRuleCreated: (rule: TransformationRule) => void;
    onRuleUpdated: (rule: TransformationRule) => void;
    onCancel: () => void;
    supportedLanguages: string[];
    className?: string;
}

interface PatternComponent {
    id: string;
    type: 'node' | 'property' | 'literal' | 'variable' | 'wildcard' | 'constraint';
    label: string;
    description: string;
    icon: string;
    category: string;
    template: string;
    parameters: Record<string, any>;
    draggable: boolean;
}

interface DroppedComponent {
    id: string;
    componentType: string;
    position: { x: number; y: number };
    properties: Record<string, any>;
    connections: string[];
}

interface BuilderState {
    rule: Partial<TransformationRule>;
    droppedComponents: DroppedComponent[];
    selectedComponent: DroppedComponent | null;
    draggedComponent: PatternComponent | null;
    canvasSize: { width: number; height: number };
    zoom: number;
    panOffset: { x: number; y: number };
}

export const VisualRuleBuilder: React.FC<VisualRuleBuilderProps> = ({
  initialRule,
  onRuleCreated,
  onRuleUpdated,
  onCancel,
  supportedLanguages,
  className = '',
}) => {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [builderState, setBuilderState] = useState<BuilderState>({
    rule: {
      name: '',
      description: '',
      sourceLanguage: 'asp',
      targetLanguage: 'csharp',
      pattern: {
        type: 'ast-pattern',
        pattern: '',
        variables: {},
        context: [],
      },
      transformation: {
        type: 'template',
        template: '',
        parameters: {},
        postProcessing: [],
      },
      constraints: [],
      tags: [],
      category: 'custom',
      complexity: 'simple',
      ...initialRule,
    },
    droppedComponents: [],
    selectedComponent: null,
    draggedComponent: null,
    canvasSize: { width: 800, height: 600 },
    zoom: 1,
    panOffset: { x: 0, y: 0 },
  });

  const [activeTab, setActiveTab] = useState<'pattern' | 'transformation' | 'constraints' | 'preview'>('pattern');
  const [showComponentLibrary, setShowComponentLibrary] = useState(true);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  // Pattern component library
  const patternComponents: PatternComponent[] = useMemo(() => [
    // AST Node Components
    {
      id: 'call-expression',
      type: 'node',
      label: 'Call Expression',
      description: 'Function or method call',
      icon: '📞',
      category: 'AST Nodes',
      template: 'CallExpression[callee.name="${functionName}"]',
      parameters: { functionName: 'string' },
      draggable: true,
    },
    {
      id: 'member-expression',
      type: 'node',
      label: 'Member Expression',
      description: 'Object property access',
      icon: '🔗',
      category: 'AST Nodes',
      template: 'MemberExpression[object.name="${objectName}"][property.name="${propertyName}"]',
      parameters: { objectName: 'string', propertyName: 'string' },
      draggable: true,
    },
    {
      id: 'identifier',
      type: 'node',
      label: 'Identifier',
      description: 'Variable or function name',
      icon: '🏷️',
      category: 'AST Nodes',
      template: 'Identifier[name="${name}"]',
      parameters: { name: 'string' },
      draggable: true,
    },
    {
      id: 'literal',
      type: 'literal',
      label: 'Literal',
      description: 'String, number, or boolean literal',
      icon: '💎',
      category: 'AST Nodes',
      template: 'Literal[value="${value}"]',
      parameters: { value: 'any', type: 'string|number|boolean' },
      draggable: true,
    },

    // Pattern Components
    {
      id: 'variable-capture',
      type: 'variable',
      label: 'Variable Capture',
      description: 'Capture part of the pattern as a variable',
      icon: '📦',
      category: 'Pattern Elements',
      template: '${variableName}',
      parameters: { variableName: 'string', type: 'string' },
      draggable: true,
    },
    {
      id: 'wildcard',
      type: 'wildcard',
      label: 'Wildcard',
      description: 'Match any expression',
      icon: '🌟',
      category: 'Pattern Elements',
      template: '*',
      parameters: { optional: 'boolean' },
      draggable: true,
    },
    {
      id: 'sequence',
      type: 'node',
      label: 'Sequence',
      description: 'Match a sequence of statements',
      icon: '📋',
      category: 'Pattern Elements',
      template: 'Sequence[statements.length>=${minLength}]',
      parameters: { minLength: 'number' },
      draggable: true,
    },

    // Language-Specific Components
    {
      id: 'asp-response-write',
      type: 'node',
      label: 'ASP Response.Write',
      description: 'ASP Classic Response.Write call',
      icon: '📝',
      category: 'ASP Classic',
      template: 'CallExpression[callee.object.name="Response"][callee.property.name="Write"]',
      parameters: { content: 'variable' },
      draggable: true,
    },
    {
      id: 'vb-dim-statement',
      type: 'node',
      label: 'VB Dim Statement',
      description: 'VBScript variable declaration',
      icon: '📊',
      category: 'VBScript',
      template: 'VariableDeclaration[kind="Dim"][id.name="${variableName}"]',
      parameters: { variableName: 'string', type: 'string' },
      draggable: true,
    },
    {
      id: 'csharp-await',
      type: 'node',
      label: 'C# Await Expression',
      description: 'C# async/await pattern',
      icon: '⏳',
      category: 'C#',
      template: 'AwaitExpression[argument=${expression}]',
      parameters: { expression: 'variable' },
      draggable: true,
    },
  ], []);

  // Transformation templates
  const transformationTemplates = useMemo(() => [
    {
      id: 'asp-to-csharp-response',
      name: 'ASP Response.Write → C# WriteAsync',
      template: 'await Response.WriteAsync(${content})',
      description: 'Convert ASP Response.Write to C# async equivalent',
    },
    {
      id: 'vb-to-csharp-variable',
      name: 'VB Dim → C# Variable Declaration',
      template: '${type} ${variableName}',
      description: 'Convert VBScript Dim to C# typed variable',
    },
    {
      id: 'method-call-conversion',
      name: 'Method Call Conversion',
      template: '${object}.${newMethod}(${arguments})',
      description: 'Convert method calls between frameworks',
    },
  ], []);

  const handleDragStart = useCallback((component: PatternComponent, event: React.DragEvent) => {
    setBuilderState(prev => ({ ...prev, draggedComponent: component }));
    event.dataTransfer.setData('application/json', JSON.stringify(component));
  }, []);

  const handleDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
  }, []);

  const handleDrop = useCallback((event: React.DragEvent) => {
    event.preventDefault();

    if (!canvasRef.current || !builderState.draggedComponent) {
      return;
    }

    const rect = canvasRef.current.getBoundingClientRect();
    const x = (event.clientX - rect.left - builderState.panOffset.x) / builderState.zoom;
    const y = (event.clientY - rect.top - builderState.panOffset.y) / builderState.zoom;

    const newComponent: DroppedComponent = {
      id: `${builderState.draggedComponent.id}_${Date.now()}`,
      componentType: builderState.draggedComponent.id,
      position: { x, y },
      properties: { ...builderState.draggedComponent.parameters },
      connections: [],
    };

    setBuilderState(prev => ({
      ...prev,
      droppedComponents: [...prev.droppedComponents, newComponent],
      draggedComponent: null,
      selectedComponent: newComponent,
    }));
  }, [builderState.draggedComponent, builderState.panOffset, builderState.zoom]);

  const handleComponentSelect = useCallback((component: DroppedComponent) => {
    setBuilderState(prev => ({ ...prev, selectedComponent: component }));
  }, []);

  const handleComponentDelete = useCallback((componentId: string) => {
    setBuilderState(prev => ({
      ...prev,
      droppedComponents: prev.droppedComponents.filter(c => c.id !== componentId),
      selectedComponent: prev.selectedComponent?.id === componentId ? null : prev.selectedComponent,
    }));
  }, []);

  const handlePropertyChange = useCallback((property: string, value: any) => {
    if (!builderState.selectedComponent) {
      return;
    }

    setBuilderState(prev => ({
      ...prev,
      droppedComponents: prev.droppedComponents.map(c =>
        c.id === prev.selectedComponent?.id
          ? { ...c, properties: { ...c.properties, [property]: value } }
          : c,
      ),
      selectedComponent: prev.selectedComponent
        ? { ...prev.selectedComponent, properties: { ...prev.selectedComponent.properties, [property]: value } }
        : null,
    }));
  }, [builderState.selectedComponent]);

  const generatePatternFromComponents = useCallback(() => {
    if (builderState.droppedComponents.length === 0) {
      return '';
    }

    // Simple pattern generation - in production this would be more sophisticated
    const patterns = builderState.droppedComponents.map(component => {
      const patternComponent = patternComponents.find(pc => pc.id === component.componentType);
      if (!patternComponent) {
        return '';
      }

      let pattern = patternComponent.template;

      // Replace parameter placeholders with actual values
      Object.entries(component.properties).forEach(([key, value]) => {
        pattern = pattern.replace(new RegExp(`\\$\\{${key}\\}`, 'g'), String(value));
      });

      return pattern;
    });

    return patterns.join(' ');
  }, [builderState.droppedComponents, patternComponents]);

  const generateTransformationFromTemplate = useCallback((templateId: string) => {
    const template = transformationTemplates.find(t => t.id === templateId);
    return template ? template.template : '';
  }, [transformationTemplates]);

  const validateRule = useCallback(() => {
    const errors: string[] = [];

    if (!builderState.rule.name?.trim()) {
      errors.push('Rule name is required');
    }

    if (!builderState.rule.description?.trim()) {
      errors.push('Rule description is required');
    }

    if (!builderState.rule.pattern?.pattern?.trim()) {
      errors.push('Pattern is required');
    }

    if (!builderState.rule.transformation?.template?.trim()) {
      errors.push('Transformation template is required');
    }

    setValidationErrors(errors);
    return errors.length === 0;
  }, [builderState.rule]);

  const handleSaveRule = useCallback(() => {
    if (!validateRule()) {
      return;
    }

    const rule: TransformationRule = {
      id: initialRule?.id || `rule_${Date.now()}`,
      name: builderState.rule.name!,
      description: builderState.rule.description!,
      sourceLanguage: builderState.rule.sourceLanguage!,
      targetLanguage: builderState.rule.targetLanguage!,
      pattern: builderState.rule.pattern!,
      transformation: builderState.rule.transformation!,
      constraints: builderState.rule.constraints || [],
      confidence: 0.8, // Default confidence for user-created rules
      examples: [],
      tags: builderState.rule.tags || [],
      createdBy: 'user',
      createdAt: initialRule?.id ? new Date(initialRule.createdAt!) : new Date(),
      lastModified: new Date(),
      usageCount: 0,
      successRate: 0,
      enabled: true,
      version: '1.0',
      category: builderState.rule.category || 'custom',
      complexity: builderState.rule.complexity || 'simple',
      quality: 0.8,
    };

    if (initialRule?.id) {
      onRuleUpdated(rule);
    } else {
      onRuleCreated(rule);
    }
  }, [builderState.rule, initialRule, validateRule, onRuleCreated, onRuleUpdated]);

  // Update pattern when components change
  useEffect(() => {
    const generatedPattern = generatePatternFromComponents();
    if (generatedPattern !== builderState.rule.pattern?.pattern) {
      setBuilderState(prev => ({
        ...prev,
        rule: {
          ...prev.rule,
          pattern: {
            ...prev.rule.pattern!,
            pattern: generatedPattern,
          },
        },
      }));
    }
  }, [builderState.droppedComponents, builderState.rule.pattern?.pattern, generatePatternFromComponents]);

  return (
    <div className={`visual-rule-builder ${className}`}>
      {/* Header */}
      <div className="builder-header">
        <div className="header-title">
          <h2>🎨 Visual Rule Builder</h2>
          <p>Create transformation rules using drag-and-drop components</p>
        </div>

        <div className="header-actions">
          <button
            onClick={() => setShowComponentLibrary(!showComponentLibrary)}
            className={`btn btn-outline toggle-library ${showComponentLibrary ? 'active' : ''}`}
          >
                        📚 Components
          </button>
          <button onClick={onCancel} className="btn btn-secondary">
                        Cancel
          </button>
          <button onClick={handleSaveRule} className="btn btn-primary">
            {initialRule?.id ? 'Update Rule' : 'Create Rule'}
          </button>
        </div>
      </div>

      {/* Rule Basic Info */}
      <div className="rule-basic-info">
        <div className="info-row">
          <div className="info-group">
            <label htmlFor="rule-name">Rule Name:</label>
            <input
              id="rule-name"
              type="text"
              value={builderState.rule.name || ''}
              onChange={(e) => setBuilderState(prev => ({
                ...prev,
                rule: { ...prev.rule, name: e.target.value },
              }))}
              className="form-control"
              placeholder="Enter rule name..."
            />
          </div>

          <div className="info-group">
            <label htmlFor="rule-category">Category:</label>
            <select
              id="rule-category"
              value={builderState.rule.category || 'custom'}
              onChange={(e) => setBuilderState(prev => ({
                ...prev,
                rule: { ...prev.rule, category: e.target.value },
              }))}
              className="form-control"
            >
              <option value="custom">Custom</option>
              <option value="web">Web Development</option>
              <option value="database">Database</option>
              <option value="ui">User Interface</option>
              <option value="business-logic">Business Logic</option>
              <option value="framework">Framework Migration</option>
            </select>
          </div>
        </div>

        <div className="info-row">
          <div className="info-group">
            <label htmlFor="source-language">Source Language:</label>
            <select
              id="source-language"
              value={builderState.rule.sourceLanguage || 'asp'}
              onChange={(e) => setBuilderState(prev => ({
                ...prev,
                rule: { ...prev.rule, sourceLanguage: e.target.value },
              }))}
              className="form-control"
            >
              {supportedLanguages.map(lang => (
                <option key={lang} value={lang}>{lang.toUpperCase()}</option>
              ))}
            </select>
          </div>

          <div className="language-arrow">→</div>

          <div className="info-group">
            <label htmlFor="target-language">Target Language:</label>
            <select
              id="target-language"
              value={builderState.rule.targetLanguage || 'csharp'}
              onChange={(e) => setBuilderState(prev => ({
                ...prev,
                rule: { ...prev.rule, targetLanguage: e.target.value },
              }))}
              className="form-control"
            >
              {supportedLanguages.map(lang => (
                <option key={lang} value={lang}>{lang.toUpperCase()}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="info-row">
          <div className="info-group full-width">
            <label htmlFor="rule-description">Description:</label>
            <textarea
              id="rule-description"
              value={builderState.rule.description || ''}
              onChange={(e) => setBuilderState(prev => ({
                ...prev,
                rule: { ...prev.rule, description: e.target.value },
              }))}
              className="form-control"
              rows={2}
              placeholder="Describe what this rule does..."
            />
          </div>
        </div>
      </div>

      {/* Main Builder Area */}
      <div className="builder-main">
        {/* Component Library */}
        {showComponentLibrary && (
          <div className="component-library">
            <div className="library-header">
              <h3>Component Library</h3>
              <button
                onClick={() => setShowComponentLibrary(false)}
                className="btn btn-sm btn-outline close-library"
              >
                                ×
              </button>
            </div>

            <div className="component-categories">
              {Object.entries(
                patternComponents.reduce((acc, component) => {
                  if (!acc[component.category]) {
                    acc[component.category] = [];
                  }
                  acc[component.category].push(component);
                  return acc;
                }, {} as Record<string, PatternComponent[]>),
              ).map(([category, components]) => (
                <div key={category} className="component-category">
                  <h4 className="category-title">{category}</h4>
                  <div className="component-list">
                    {components.map(component => (
                      <div
                        key={component.id}
                        className="component-item"
                        draggable={component.draggable}
                        onDragStart={(e) => handleDragStart(component, e)}
                        title={component.description}
                      >
                        <div className="component-icon">{component.icon}</div>
                        <div className="component-label">{component.label}</div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Builder Canvas */}
        <div className="builder-canvas-container">
          <div className="canvas-tabs">
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
              className={`tab-btn ${activeTab === 'constraints' ? 'active' : ''}`}
              onClick={() => setActiveTab('constraints')}
            >
                            ⚙️ Constraints
            </button>
            <button
              className={`tab-btn ${activeTab === 'preview' ? 'active' : ''}`}
              onClick={() => setActiveTab('preview')}
            >
                            👁️ Preview
            </button>
          </div>

          <div className="canvas-content">
            {activeTab === 'pattern' && (
              <div className="pattern-builder">
                <div className="canvas-toolbar">
                  <div className="toolbar-group">
                    <button
                      onClick={() => setBuilderState(prev => ({
                        ...prev,
                        droppedComponents: [],
                      }))}
                      className="btn btn-sm btn-outline"
                    >
                                            🗑️ Clear
                    </button>
                    <button
                      onClick={() => setBuilderState(prev => ({
                        ...prev,
                        zoom: Math.min(prev.zoom + 0.1, 2),
                      }))}
                      className="btn btn-sm btn-outline"
                    >
                                            🔍+
                    </button>
                    <button
                      onClick={() => setBuilderState(prev => ({
                        ...prev,
                        zoom: Math.max(prev.zoom - 0.1, 0.5),
                      }))}
                      className="btn btn-sm btn-outline"
                    >
                                            🔍-
                    </button>
                  </div>
                </div>

                <div
                  ref={canvasRef}
                  className="pattern-canvas"
                  onDragOver={handleDragOver}
                  onDrop={handleDrop}
                  style={{
                    // eslint-disable-next-line max-len
                    transform: `scale(${builderState.zoom}) translate(${builderState.panOffset.x}px, ${builderState.panOffset.y}px)`,
                  }}
                >
                  {builderState.droppedComponents.map(component => (
                    <DroppedComponentRenderer
                      key={component.id}
                      component={component}
                      patternComponent={patternComponents.find(pc => pc.id === component.componentType)!}
                      selected={builderState.selectedComponent?.id === component.id}
                      onSelect={() => handleComponentSelect(component)}
                      onDelete={() => handleComponentDelete(component.id)}
                    />
                  ))}

                  {builderState.droppedComponents.length === 0 && (
                    <div className="canvas-placeholder">
                      <div className="placeholder-icon">🎯</div>
                      <div className="placeholder-text">
                                                Drag components from the library to build your pattern
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {activeTab === 'transformation' && (
              <div className="transformation-builder">
                <div className="transformation-templates">
                  <h4>Templates</h4>
                  <div className="template-list">
                    {transformationTemplates.map(template => (
                      <div
                        key={template.id}
                        className="template-item"
                        onClick={() => setBuilderState(prev => ({
                          ...prev,
                          rule: {
                            ...prev.rule,
                            transformation: {
                              ...prev.rule.transformation!,
                              template: template.template,
                            },
                          },
                        }))}
                      >
                        <div className="template-name">{template.name}</div>
                        <div className="template-description">{template.description}</div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="transformation-editor">
                  <h4>Transformation Template</h4>
                  <textarea
                    value={builderState.rule.transformation?.template || ''}
                    onChange={(e) => setBuilderState(prev => ({
                      ...prev,
                      rule: {
                        ...prev.rule,
                        transformation: {
                          ...prev.rule.transformation!,
                          template: e.target.value,
                        },
                      },
                    }))}
                    className="form-control code-textarea"
                    rows={10}
                    placeholder="Enter transformation template..."
                  />
                </div>
              </div>
            )}

            {activeTab === 'constraints' && (
              <div className="constraints-builder">
                <h4>Rule Constraints</h4>
                <p>Add constraints to control when this rule should be applied.</p>

                <div className="constraint-list">
                  {builderState.rule.constraints?.map((constraint, index) => (
                    <div key={index} className="constraint-item">
                      <select
                        value={constraint.type}
                        onChange={(e) => {
                          const newConstraints = [...(builderState.rule.constraints || [])];
                          newConstraints[index] = { ...constraint, type: e.target.value as any };
                          setBuilderState(prev => ({
                            ...prev,
                            rule: { ...prev.rule, constraints: newConstraints },
                          }));
                        }}
                        className="form-control constraint-type"
                      >
                        <option value="context">Context</option>
                        <option value="syntax">Syntax</option>
                        <option value="semantic">Semantic</option>
                        <option value="framework">Framework</option>
                      </select>

                      <input
                        type="text"
                        value={constraint.condition}
                        onChange={(e) => {
                          const newConstraints = [...(builderState.rule.constraints || [])];
                          newConstraints[index] = { ...constraint, condition: e.target.value };
                          setBuilderState(prev => ({
                            ...prev,
                            rule: { ...prev.rule, constraints: newConstraints },
                          }));
                        }}
                        className="form-control constraint-condition"
                        placeholder="Constraint condition..."
                      />

                      <button
                        onClick={() => {
                          const newConstraints = builderState.rule.constraints?.filter((_, i) => i !== index) || [];
                          setBuilderState(prev => ({
                            ...prev,
                            rule: { ...prev.rule, constraints: newConstraints },
                          }));
                        }}
                        className="btn btn-sm btn-danger"
                      >
                                                🗑️
                      </button>
                    </div>
                  )) || []}
                </div>

                <button
                  onClick={() => {
                    const newConstraint: RuleConstraint = {
                      type: 'context',
                      condition: '',
                      value: '',
                      required: false,
                    };
                    setBuilderState(prev => ({
                      ...prev,
                      rule: {
                        ...prev.rule,
                        constraints: [...(prev.rule.constraints || []), newConstraint],
                      },
                    }));
                  }}
                  className="btn btn-primary add-constraint-btn"
                >
                                    ➕ Add Constraint
                </button>
              </div>
            )}

            {activeTab === 'preview' && (
              <div className="rule-preview">
                <h4>Rule Preview</h4>

                <div className="preview-section">
                  <h5>Pattern</h5>
                  <pre className="code-block pattern-preview">
                    {builderState.rule.pattern?.pattern || 'No pattern defined'}
                  </pre>
                </div>

                <div className="preview-section">
                  <h5>Transformation</h5>
                  <pre className="code-block transformation-preview">
                    {builderState.rule.transformation?.template || 'No transformation defined'}
                  </pre>
                </div>

                {builderState.rule.constraints && builderState.rule.constraints.length > 0 && (
                  <div className="preview-section">
                    <h5>Constraints</h5>
                    <ul className="constraints-preview">
                      {builderState.rule.constraints.map((constraint, index) => (
                        <li key={index}>
                          <strong>{constraint.type}:</strong> {constraint.condition}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {validationErrors.length > 0 && (
                  <div className="validation-errors">
                    <h5>⚠️ Validation Errors</h5>
                    <ul className="error-list">
                      {validationErrors.map((error, index) => (
                        <li key={index} className="error-item">{error}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Properties Panel */}
        {builderState.selectedComponent && (
          <div className="properties-panel">
            <div className="panel-header">
              <h3>Properties</h3>
              <button
                onClick={() => setBuilderState(prev => ({ ...prev, selectedComponent: null }))}
                className="btn btn-sm btn-outline"
              >
                                ×
              </button>
            </div>

            <div className="panel-content">
              <div className="component-info">
                <div className="component-type">
                  {patternComponents.find(pc => pc.id === builderState.selectedComponent?.componentType)?.label}
                </div>
              </div>

              <div className="property-list">
                {Object.entries(builderState.selectedComponent.properties).map(([key, value]) => (
                  <div key={key} className="property-item">
                    <label htmlFor={`prop-${key}`}>{key}:</label>
                    <input
                      id={`prop-${key}`}
                      type="text"
                      value={String(value)}
                      onChange={(e) => handlePropertyChange(key, e.target.value)}
                      className="form-control property-input"
                    />
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Dropped Component Renderer
 */
interface DroppedComponentRendererProps {
    component: DroppedComponent;
    patternComponent: PatternComponent;
    selected: boolean;
    onSelect: () => void;
    onDelete: () => void;
}

const DroppedComponentRenderer: React.FC<DroppedComponentRendererProps> = ({
  component,
  patternComponent,
  selected,
  onSelect,
  onDelete,
}) => {
  return (
    <div
      className={`dropped-component ${selected ? 'selected' : ''}`}
      style={{
        left: component.position.x,
        top: component.position.y,
      }}
      onClick={onSelect}
    >
      <div className="component-header">
        <div className="component-icon">{patternComponent.icon}</div>
        <div className="component-label">{patternComponent.label}</div>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
          className="delete-btn"
        >
                    ×
        </button>
      </div>

      <div className="component-properties">
        {Object.entries(component.properties).map(([key, value]) => (
          <div key={key} className="property-display">
            <span className="property-key">{key}:</span>
            <span className="property-value">{String(value)}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default VisualRuleBuilder;

