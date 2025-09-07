/**
 * Rule Generation Configuration Component
 *
 * This component provides configuration options for LLM-assisted rule generation,
 * allowing users to customize generation parameters, quality thresholds, and
 * optimization settings.
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback } from 'react';

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

interface RuleGenerationConfigProps {
    parameters: RuleGenerationParameters;
    onParametersChange: (parameters: RuleGenerationParameters) => void;
    sourceLanguage: string;
    targetLanguage: string;
}

interface ConfigSection {
    title: string;
    description: string;
    expanded: boolean;
}

export const RuleGenerationConfig: React.FC<RuleGenerationConfigProps> = ({
  parameters,
  onParametersChange,
  sourceLanguage,
  targetLanguage,
}) => {
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['basic']));

  const handleParameterChange = useCallback(<K extends keyof RuleGenerationParameters>(
    key: K,
    value: RuleGenerationParameters[K],
  ) => {
    onParametersChange({
      ...parameters,
      [key]: value,
    });
  }, [parameters, onParametersChange]);

  const toggleSection = useCallback((sectionId: string) => {
    setExpandedSections(prev => {
      const newSet = new Set(prev);
      if (newSet.has(sectionId)) {
        newSet.delete(sectionId);
      } else {
        newSet.add(sectionId);
      }
      return newSet;
    });
  }, []);

  const resetToDefaults = useCallback(() => {
    onParametersChange({
      maxRules: 5,
      minConfidence: 0.8,
      includeExplanations: true,
      generateVariations: true,
      optimizeForSpeed: false,
      optimizeForQuality: true,
      contextAware: true,
      frameworkSpecific: true,
    });
  }, [onParametersChange]);

  const applyPreset = useCallback((preset: 'fast' | 'balanced' | 'thorough') => {
    const presets = {
      fast: {
        maxRules: 3,
        minConfidence: 0.7,
        includeExplanations: false,
        generateVariations: false,
        optimizeForSpeed: true,
        optimizeForQuality: false,
        contextAware: false,
        frameworkSpecific: false,
      },
      balanced: {
        maxRules: 5,
        minConfidence: 0.8,
        includeExplanations: true,
        generateVariations: true,
        optimizeForSpeed: false,
        optimizeForQuality: true,
        contextAware: true,
        frameworkSpecific: true,
      },
      thorough: {
        maxRules: 10,
        minConfidence: 0.9,
        includeExplanations: true,
        generateVariations: true,
        optimizeForSpeed: false,
        optimizeForQuality: true,
        contextAware: true,
        frameworkSpecific: true,
      },
    };

    onParametersChange(presets[preset]);
  }, [onParametersChange]);

  return (
    <div className="rule-generation-config">
      {/* Presets */}
      <div className="config-presets">
        <h4>Quick Presets</h4>
        <div className="preset-buttons">
          <button
            onClick={() => applyPreset('fast')}
            className="btn btn-sm btn-outline preset-btn"
            title="Fast generation with basic rules"
          >
                        ⚡ Fast
          </button>
          <button
            onClick={() => applyPreset('balanced')}
            className="btn btn-sm btn-outline preset-btn"
            title="Balanced speed and quality"
          >
                        ⚖️ Balanced
          </button>
          <button
            onClick={() => applyPreset('thorough')}
            className="btn btn-sm btn-outline preset-btn"
            title="Comprehensive rule generation"
          >
                        🔍 Thorough
          </button>
          <button
            onClick={resetToDefaults}
            className="btn btn-sm btn-secondary reset-btn"
            title="Reset to default settings"
          >
                        🔄 Reset
          </button>
        </div>
      </div>

      {/* Basic Settings */}
      <div className="config-section">
        <div
          className="section-header"
          onClick={() => toggleSection('basic')}
        >
          <h4>
            {expandedSections.has('basic') ? '▼' : '▶'} Basic Settings
          </h4>
          <span className="section-description">Core generation parameters</span>
        </div>

        {expandedSections.has('basic') && (
          <div className="section-content">
            <div className="config-row">
              <label htmlFor="max-rules">Maximum Rules to Generate:</label>
              <div className="input-with-info">
                <input
                  id="max-rules"
                  type="number"
                  min="1"
                  max="20"
                  value={parameters.maxRules}
                  onChange={(e) => handleParameterChange('maxRules', parseInt(e.target.value))}
                  className="form-control number-input"
                />
                <span className="input-info">
                                    More rules = higher cost but better coverage
                </span>
              </div>
            </div>

            <div className="config-row">
              <label htmlFor="min-confidence">Minimum Confidence Threshold:</label>
              <div className="input-with-info">
                <input
                  id="min-confidence"
                  type="range"
                  min="0.5"
                  max="1.0"
                  step="0.05"
                  value={parameters.minConfidence}
                  onChange={(e) => handleParameterChange('minConfidence', parseFloat(e.target.value))}
                  className="form-control range-input"
                />
                <span className="range-value">{(parameters.minConfidence * 100).toFixed(0)}%</span>
                <span className="input-info">
                                    Higher threshold = fewer but more reliable rules
                </span>
              </div>
            </div>

            <div className="config-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={parameters.includeExplanations}
                  onChange={(e) => handleParameterChange('includeExplanations', e.target.checked)}
                />
                <span className="checkbox-text">Include Rule Explanations</span>
                <span className="checkbox-info">
                                    Generate natural language explanations for each rule
                </span>
              </label>
            </div>

            <div className="config-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={parameters.generateVariations}
                  onChange={(e) => handleParameterChange('generateVariations', e.target.checked)}
                />
                <span className="checkbox-text">Generate Rule Variations</span>
                <span className="checkbox-info">
                                    Create multiple variations of each rule for different contexts
                </span>
              </label>
            </div>
          </div>
        )}
      </div>

      {/* Optimization Settings */}
      <div className="config-section">
        <div
          className="section-header"
          onClick={() => toggleSection('optimization')}
        >
          <h4>
            {expandedSections.has('optimization') ? '▼' : '▶'} Optimization
          </h4>
          <span className="section-description">Performance and quality trade-offs</span>
        </div>

        {expandedSections.has('optimization') && (
          <div className="section-content">
            <div className="optimization-mode">
              <h5>Optimization Priority</h5>
              <div className="radio-group">
                <label className="radio-label">
                  <input
                    type="radio"
                    name="optimization"
                    checked={parameters.optimizeForSpeed}
                    onChange={(e) => {
                      if (e.target.checked) {
                        handleParameterChange('optimizeForSpeed', true);
                        handleParameterChange('optimizeForQuality', false);
                      }
                    }}
                  />
                  <span className="radio-text">⚡ Speed</span>
                  <span className="radio-info">Faster generation, simpler rules</span>
                </label>
                <label className="radio-label">
                  <input
                    type="radio"
                    name="optimization"
                    checked={parameters.optimizeForQuality}
                    onChange={(e) => {
                      if (e.target.checked) {
                        handleParameterChange('optimizeForQuality', true);
                        handleParameterChange('optimizeForSpeed', false);
                      }
                    }}
                  />
                  <span className="radio-text">🎯 Quality</span>
                  <span className="radio-info">Higher quality rules, longer generation time</span>
                </label>
                <label className="radio-label">
                  <input
                    type="radio"
                    name="optimization"
                    checked={!parameters.optimizeForSpeed && !parameters.optimizeForQuality}
                    onChange={(e) => {
                      if (e.target.checked) {
                        handleParameterChange('optimizeForSpeed', false);
                        handleParameterChange('optimizeForQuality', false);
                      }
                    }}
                  />
                  <span className="radio-text">⚖️ Balanced</span>
                  <span className="radio-info">Balance between speed and quality</span>
                </label>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Context Settings */}
      <div className="config-section">
        <div
          className="section-header"
          onClick={() => toggleSection('context')}
        >
          <h4>
            {expandedSections.has('context') ? '▼' : '▶'} Context Awareness
          </h4>
          <span className="section-description">How rules adapt to different contexts</span>
        </div>

        {expandedSections.has('context') && (
          <div className="section-content">
            <div className="config-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={parameters.contextAware}
                  onChange={(e) => handleParameterChange('contextAware', e.target.checked)}
                />
                <span className="checkbox-text">Context-Aware Rules</span>
                <span className="checkbox-info">
                                    Generate rules that consider surrounding code context
                </span>
              </label>
            </div>

            <div className="config-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={parameters.frameworkSpecific}
                  onChange={(e) => handleParameterChange('frameworkSpecific', e.target.checked)}
                />
                <span className="checkbox-text">Framework-Specific Rules</span>
                <span className="checkbox-info">
                                    Tailor rules to specific frameworks and libraries
                </span>
              </label>
            </div>

            {parameters.frameworkSpecific && (
              <div className="framework-options">
                <h5>Target Framework for {targetLanguage.toUpperCase()}</h5>
                <select className="form-control framework-select">
                  {getFrameworkOptions(targetLanguage).map(framework => (
                    <option key={framework.value} value={framework.value}>
                      {framework.label}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Advanced Settings */}
      <div className="config-section">
        <div
          className="section-header"
          onClick={() => toggleSection('advanced')}
        >
          <h4>
            {expandedSections.has('advanced') ? '▼' : '▶'} Advanced Settings
          </h4>
          <span className="section-description">Expert-level configuration options</span>
        </div>

        {expandedSections.has('advanced') && (
          <div className="section-content">
            <div className="config-row">
              <label htmlFor="pattern-complexity">Pattern Complexity Level:</label>
              <select
                id="pattern-complexity"
                className="form-control"
              >
                <option value="simple">Simple - Basic syntax transformations</option>
                <option value="moderate">Moderate - Include control flow patterns</option>
                <option value="complex">Complex - Advanced semantic patterns</option>
              </select>
            </div>

            <div className="config-row">
              <label htmlFor="rule-scope">Rule Scope:</label>
              <select
                id="rule-scope"
                className="form-control"
              >
                <option value="statement">Statement Level</option>
                <option value="function">Function Level</option>
                <option value="class">Class Level</option>
                <option value="module">Module Level</option>
              </select>
            </div>

            <div className="config-row">
              <label className="checkbox-label">
                <input type="checkbox" />
                <span className="checkbox-text">Enable Experimental Features</span>
                <span className="checkbox-info">
                                    Use cutting-edge rule generation techniques (may be unstable)
                </span>
              </label>
            </div>

            <div className="config-row">
              <label className="checkbox-label">
                <input type="checkbox" />
                <span className="checkbox-text">Generate Test Cases</span>
                <span className="checkbox-info">
                                    Automatically generate test cases for each rule
                </span>
              </label>
            </div>
          </div>
        )}
      </div>

      {/* Configuration Summary */}
      <div className="config-summary">
        <h4>Configuration Summary</h4>
        <div className="summary-grid">
          <div className="summary-item">
            <span className="summary-label">Max Rules:</span>
            <span className="summary-value">{parameters.maxRules}</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Min Confidence:</span>
            <span className="summary-value">{(parameters.minConfidence * 100).toFixed(0)}%</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Optimization:</span>
            <span className="summary-value">
              {parameters.optimizeForSpeed ? 'Speed' :
                parameters.optimizeForQuality ? 'Quality' : 'Balanced'}
            </span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Context Aware:</span>
            <span className="summary-value">{parameters.contextAware ? 'Yes' : 'No'}</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Framework Specific:</span>
            <span className="summary-value">{parameters.frameworkSpecific ? 'Yes' : 'No'}</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Include Explanations:</span>
            <span className="summary-value">{parameters.includeExplanations ? 'Yes' : 'No'}</span>
          </div>
        </div>

        <div className="estimated-cost">
          <span className="cost-label">Estimated Cost:</span>
          <span className="cost-value">
                        ${calculateEstimatedCost(parameters).toFixed(3)}
          </span>
          <span className="cost-info">
                        Based on current configuration and typical usage
          </span>
        </div>
      </div>
    </div>
  );
};

/**
 * Get framework options for a target language
 */
function getFrameworkOptions(language: string): Array<{value: string, label: string}> {
  const frameworks: Record<string, Array<{value: string, label: string}>> = {
    csharp: [
      { value: 'netcore', label: '.NET Core' },
      { value: 'netframework', label: '.NET Framework' },
      { value: 'aspnetcore', label: 'ASP.NET Core' },
      { value: 'aspnet', label: 'ASP.NET' },
      { value: 'xamarin', label: 'Xamarin' },
      { value: 'unity', label: 'Unity' },
    ],
    java: [
      { value: 'spring', label: 'Spring Framework' },
      { value: 'springboot', label: 'Spring Boot' },
      { value: 'android', label: 'Android' },
      { value: 'javase', label: 'Java SE' },
      { value: 'javaee', label: 'Java EE' },
    ],
    javascript: [
      { value: 'nodejs', label: 'Node.js' },
      { value: 'react', label: 'React' },
      { value: 'vue', label: 'Vue.js' },
      { value: 'angular', label: 'Angular' },
      { value: 'express', label: 'Express.js' },
    ],
    python: [
      { value: 'django', label: 'Django' },
      { value: 'flask', label: 'Flask' },
      { value: 'fastapi', label: 'FastAPI' },
      { value: 'pandas', label: 'Pandas' },
      { value: 'tensorflow', label: 'TensorFlow' },
    ],
  };

  return frameworks[language] || [{ value: 'default', label: 'Default' }];
}

/**
 * Calculate estimated cost based on configuration
 */
function calculateEstimatedCost(parameters: RuleGenerationParameters): number {
  let baseCost = 0.02; // Base cost per rule generation request

  // Factor in number of rules
  baseCost += parameters.maxRules * 0.01;

  // Factor in quality optimization
  if (parameters.optimizeForQuality) {
    baseCost *= 1.5;
  }

  // Factor in explanations
  if (parameters.includeExplanations) {
    baseCost *= 1.3;
  }

  // Factor in variations
  if (parameters.generateVariations) {
    baseCost *= 1.4;
  }

  // Factor in context awareness
  if (parameters.contextAware) {
    baseCost *= 1.2;
  }

  return baseCost;
}

export default RuleGenerationConfig;

