/**
 * Interactive Translation Panel Component
 *
 * This React component provides the main user interface for the interactive
 * AST-to-AST translation system. It displays translation steps, suggestions,
 * and allows users to provide feedback in a stepwise manner.
 *
 * Key Features:
 * - Step-by-step translation workflow
 * - Real-time suggestion display
 * - User feedback collection
 * - Pattern visualization
 * - Progress tracking
 * - Code preview and comparison
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  TranslationSession,
  TranslationStep,
  TranslationSuggestion,
  UserFeedback,
  UserAction,
  SessionStatus,
  TranslationStepStatus,
} from '../InteractiveASTTranslator';
import { ZeroCopyASTNode } from '../../zerocopy/ast/ZeroCopyASTNode';
import {
  TranslationEngineOrchestrator,
  OrchestratorConfig,
  EngineSelectionStrategy,
} from '../engines/TranslationEngineOrchestrator';

interface InteractiveTranslationPanelProps {
    session: TranslationSession;
    orchestrator: TranslationEngineOrchestrator;
    onUserFeedback: (stepId: string, feedback: UserFeedback) => Promise<void>;
    onSessionComplete: (session: TranslationSession) => void;
    onSessionPause: () => void;
    onSessionResume: () => void;
    onSessionCancel: () => void;
    onOrchestratorConfigChange?: (config: Partial<OrchestratorConfig>) => void;
    className?: string;
}

interface EngineStatusPanelProps {
    orchestrator: TranslationEngineOrchestrator;
    onConfigChange?: (config: Partial<OrchestratorConfig>) => void;
}

interface EngineHealthIndicatorProps {
    engineName: string;
    isHealthy: boolean;
    averageResponseTime: number;
    successRate: number;
    lastError?: string;
}

interface OrchestratorControlsProps {
    currentStrategy: EngineSelectionStrategy;
    availableEngines: string[];
    onStrategyChange: (strategy: EngineSelectionStrategy) => void;
    onForceHealthCheck: () => void;
}

interface StepCardProps {
    step: TranslationStep;
    isActive: boolean;
    isCompleted: boolean;
    onFeedback: (feedback: UserFeedback) => Promise<void>;
    onRequestAlternatives: () => void;
    onRequestExplanation: (suggestionId: string) => void;
}

interface SuggestionCardProps {
    suggestion: TranslationSuggestion;
    isSelected: boolean;
    onSelect: () => void;
    onRequestExplanation: () => void;
    onPreview: () => void;
}

interface CodeComparisonProps {
    sourceCode: string;
    targetCode: string;
    sourceLanguage: string;
    targetLanguage: string;
    highlightDifferences?: boolean;
}

interface ProgressIndicatorProps {
    currentStep: number;
    totalSteps: number;
    completedSteps: number;
    qualityScore: number;
}

interface FeedbackModalProps {
    isOpen: boolean;
    step: TranslationStep;
    selectedSuggestion?: TranslationSuggestion;
    onSubmit: (feedback: UserFeedback) => void;
    onCancel: () => void;
}

/**
 * Main Interactive Translation Panel Component
 */
export const InteractiveTranslationPanel: React.FC<InteractiveTranslationPanelProps> = ({
  session,
  orchestrator,
  onUserFeedback,
  onSessionComplete,
  onSessionPause,
  onSessionResume,
  onSessionCancel,
  onOrchestratorConfigChange,
  className = '',
}) => {
  const [selectedSuggestion, setSelectedSuggestion] = useState<TranslationSuggestion | null>(null);
  const [showFeedbackModal, setShowFeedbackModal] = useState(false);
  const [currentStepIndex, setCurrentStepIndex] = useState(session.currentStepIndex);
  const [previewCode, setPreviewCode] = useState<string>('');
  const [showCodeComparison, setShowCodeComparison] = useState(false);
  const [showEngineStatus, setShowEngineStatus] = useState(false);
  const [engineHealth, setEngineHealth] = useState(orchestrator.getEngineHealth());
  const [availableEngines, setAvailableEngines] = useState(orchestrator.getAvailableEngineNames());
  const [currentStrategy, setCurrentStrategy] = useState<EngineSelectionStrategy>(EngineSelectionStrategy.PRIORITY);

  // Update current step when session changes
  useEffect(() => {
    setCurrentStepIndex(session.currentStepIndex);
  }, [session.currentStepIndex]);

  // Handle session completion
  useEffect(() => {
    if (session.status === SessionStatus.COMPLETED) {
      onSessionComplete(session);
    }
  }, [session.status, session, onSessionComplete]);

  const currentStep = useMemo(() => {
    return session.steps[currentStepIndex];
  }, [session.steps, currentStepIndex]);

  const handleStepFeedback = useCallback(async (stepId: string, feedback: UserFeedback) => {
    try {
      await onUserFeedback(stepId, feedback);
      setShowFeedbackModal(false);
      setSelectedSuggestion(null);
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to submit feedback:', error);
      // Show error message to user
    }
  }, [onUserFeedback]);

  const handleSuggestionSelect = useCallback((suggestion: TranslationSuggestion) => {
    setSelectedSuggestion(suggestion);
    setPreviewCode(nodeToCode(suggestion.targetNode));
  }, []);

  const handleAcceptSuggestion = useCallback(() => {
    if (currentStep && selectedSuggestion) {
      const feedback: UserFeedback = {
        stepId: currentStep.id,
        action: UserAction.ACCEPT,
        selectedSuggestion: selectedSuggestion.id,
        rating: 4, // Default good rating
        timestamp: new Date(),
      };
      handleStepFeedback(currentStep.id, feedback);
    }
  }, [currentStep, selectedSuggestion, handleStepFeedback]);

  const handleRejectSuggestion = useCallback(() => {
    if (currentStep && selectedSuggestion) {
      const feedback: UserFeedback = {
        stepId: currentStep.id,
        action: UserAction.REJECT,
        selectedSuggestion: selectedSuggestion.id,
        rating: 2, // Default poor rating
        timestamp: new Date(),
      };
      handleStepFeedback(currentStep.id, feedback);
    }
  }, [currentStep, selectedSuggestion, handleStepFeedback]);

  const handleRequestAlternatives = useCallback(() => {
    if (currentStep) {
      const feedback: UserFeedback = {
        stepId: currentStep.id,
        action: UserAction.REQUEST_ALTERNATIVES,
        rating: 3, // Neutral rating
        timestamp: new Date(),
      };
      handleStepFeedback(currentStep.id, feedback);
    }
  }, [currentStep, handleStepFeedback]);

  const handleRequestExplanation = useCallback((suggestionId: string) => {
    if (currentStep) {
      const feedback: UserFeedback = {
        stepId: currentStep.id,
        action: UserAction.REQUEST_EXPLANATION,
        selectedSuggestion: suggestionId,
        rating: 3, // Neutral rating
        timestamp: new Date(),
      };
      handleStepFeedback(currentStep.id, feedback);
    }
  }, [currentStep, handleStepFeedback]);

  const handleShowFeedbackModal = useCallback(() => {
    setShowFeedbackModal(true);
  }, []);

  const handleToggleCodeComparison = useCallback(() => {
    setShowCodeComparison(!showCodeComparison);
  }, [showCodeComparison]);

  if (!currentStep) {
    return (
      <div className={`interactive-translation-panel ${className}`}>
        <div className="no-steps-message">
          <h3>No translation steps available</h3>
          <p>The translation session appears to be empty or not properly initialized.</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`interactive-translation-panel ${className}`}>
      {/* Header */}
      <div className="translation-header">
        <h2>Interactive Translation: {session.sourceLanguage} → {session.targetLanguage}</h2>
        <div className="session-controls">
          <button
            onClick={() => setShowEngineStatus(!showEngineStatus)}
            className="btn btn-info"
          >
            {showEngineStatus ? 'Hide' : 'Show'} Engine Status
          </button>
          <button
            onClick={onSessionPause}
            disabled={session.status !== SessionStatus.TRANSLATING}
            className="btn btn-secondary"
          >
                        Pause
          </button>
          <button
            onClick={onSessionResume}
            disabled={session.status !== SessionStatus.PAUSED}
            className="btn btn-secondary"
          >
                        Resume
          </button>
          <button
            onClick={onSessionCancel}
            className="btn btn-danger"
          >
                        Cancel
          </button>
          <button
            onClick={handleToggleCodeComparison}
            className="btn btn-info"
          >
            {showCodeComparison ? 'Hide' : 'Show'} Code Comparison
          </button>
        </div>
      </div>

      {/* Progress Indicator */}
      <ProgressIndicator
        currentStep={currentStepIndex + 1}
        totalSteps={session.steps.length}
        completedSteps={session.metadata.completedSteps}
        qualityScore={session.metadata.qualityScore}
      />

      {/* Engine Status Panel */}
      {showEngineStatus && (
        <EngineStatusPanel
          orchestrator={orchestrator}
          onConfigChange={onOrchestratorConfigChange}
        />
      )}

      {/* Main Content */}
      <div className="translation-content">
        {/* Code Comparison (if enabled) */}
        {showCodeComparison && (
          <CodeComparison
            sourceCode={nodeToCode(currentStep.sourceNode)}
            targetCode={previewCode || nodeToCode(currentStep.targetNode)}
            sourceLanguage={session.sourceLanguage}
            targetLanguage={session.targetLanguage}
            highlightDifferences={true}
          />
        )}

        {/* Current Step */}
        <div className="current-step-section">
          <StepCard
            step={currentStep}
            isActive={true}
            isCompleted={currentStep.status === TranslationStepStatus.COMPLETED}
            onFeedback={(feedback) => handleStepFeedback(currentStep.id, feedback)}
            onRequestAlternatives={handleRequestAlternatives}
            onRequestExplanation={handleRequestExplanation}
          />
        </div>

        {/* Suggestions */}
        <div className="suggestions-section">
          <h3>Translation Suggestions</h3>
          <div className="suggestions-grid">
            {currentStep.suggestions.map((suggestion, index) => (
              <SuggestionCard
                key={suggestion.id}
                suggestion={suggestion}
                isSelected={selectedSuggestion?.id === suggestion.id}
                onSelect={() => handleSuggestionSelect(suggestion)}
                onRequestExplanation={() => handleRequestExplanation(suggestion.id)}
                onPreview={() => setPreviewCode(nodeToCode(suggestion.targetNode))}
              />
            ))}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="action-buttons">
          <button
            onClick={handleAcceptSuggestion}
            disabled={!selectedSuggestion}
            className="btn btn-success btn-lg"
          >
                        Accept Selected
          </button>
          <button
            onClick={handleRejectSuggestion}
            disabled={!selectedSuggestion}
            className="btn btn-warning btn-lg"
          >
                        Reject Selected
          </button>
          <button
            onClick={handleShowFeedbackModal}
            className="btn btn-info btn-lg"
          >
                        Provide Detailed Feedback
          </button>
          <button
            onClick={handleRequestAlternatives}
            className="btn btn-secondary btn-lg"
          >
                        Request More Options
          </button>
        </div>
      </div>

      {/* Feedback Modal */}
      {showFeedbackModal && (
        <FeedbackModal
          isOpen={showFeedbackModal}
          step={currentStep}
          selectedSuggestion={selectedSuggestion || undefined}
          onSubmit={(feedback) => handleStepFeedback(currentStep.id, feedback)}
          onCancel={() => setShowFeedbackModal(false)}
        />
      )}

      {/* Session Summary (if completed) */}
      {session.status === SessionStatus.COMPLETED && (
        <div className="session-summary">
          <h3>Translation Complete!</h3>
          <div className="summary-stats">
            <div className="stat">
              <label>Quality Score:</label>
              <span>{(session.metadata.qualityScore * 100).toFixed(1)}%</span>
            </div>
            <div className="stat">
              <label>Steps Completed:</label>
              <span>{session.metadata.completedSteps} / {session.metadata.totalSteps}</span>
            </div>
            <div className="stat">
              <label>User Interactions:</label>
              <span>{session.metadata.userInteractions}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

/**
 * Step Card Component
 */
const StepCard: React.FC<StepCardProps> = ({
  step,
  isActive,
  isCompleted,
  onFeedback,
  onRequestAlternatives,
  onRequestExplanation,
}) => {
  const getStatusIcon = () => {
    switch (step.status) {
      case TranslationStepStatus.COMPLETED:
        return '✅';
      case TranslationStepStatus.IN_PROGRESS:
        return '🔄';
      case TranslationStepStatus.AWAITING_USER:
        return '⏳';
      case TranslationStepStatus.FAILED:
        return '❌';
      case TranslationStepStatus.SKIPPED:
        return '⏭️';
      default:
        return '⭕';
    }
  };

  const getStatusColor = () => {
    switch (step.status) {
      case TranslationStepStatus.COMPLETED:
        return 'success';
      case TranslationStepStatus.IN_PROGRESS:
        return 'primary';
      case TranslationStepStatus.AWAITING_USER:
        return 'warning';
      case TranslationStepStatus.FAILED:
        return 'danger';
      case TranslationStepStatus.SKIPPED:
        return 'secondary';
      default:
        return 'light';
    }
  };

  return (
    <div className={`step-card ${isActive ? 'active' : ''} ${isCompleted ? 'completed' : ''}`}>
      <div className="step-header">
        <div className="step-status">
          <span className="status-icon">{getStatusIcon()}</span>
          <span className={`status-text text-${getStatusColor()}`}>
            {step.status.replace('_', ' ').toUpperCase()}
          </span>
        </div>
        <div className="step-confidence">
          <label>Confidence:</label>
          <span className="confidence-score">
            {(step.confidence * 100).toFixed(1)}%
          </span>
        </div>
      </div>

      <div className="step-content">
        <div className="source-node">
          <h4>Source ({step.sourceNode.nodeType})</h4>
          <pre className="code-block">
            {nodeToCode(step.sourceNode)}
          </pre>
        </div>

        {step.targetNode && (
          <div className="target-node">
            <h4>Target ({step.targetNode.nodeType})</h4>
            <pre className="code-block">
              {nodeToCode(step.targetNode)}
            </pre>
          </div>
        )}

        <div className="pattern-info">
          <h4>Applied Pattern</h4>
          <div className="pattern-details">
            <div><strong>Name:</strong> {step.pattern.name}</div>
            <div><strong>Description:</strong> {step.pattern.description}</div>
            <div><strong>Success Rate:</strong> {(step.pattern.successRate * 100).toFixed(1)}%</div>
            <div><strong>Usage Count:</strong> {step.pattern.usageCount}</div>
          </div>
        </div>

        {step.userFeedback && (
          <div className="user-feedback">
            <h4>Your Feedback</h4>
            <div className="feedback-details">
              <div><strong>Action:</strong> {step.userFeedback.action}</div>
              <div><strong>Rating:</strong> {step.userFeedback.rating}/5</div>
              {step.userFeedback.comments && (
                <div><strong>Comments:</strong> {step.userFeedback.comments}</div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Suggestion Card Component
 */
const SuggestionCard: React.FC<SuggestionCardProps> = ({
  suggestion,
  isSelected,
  onSelect,
  onRequestExplanation,
  onPreview,
}) => {
  const getTypeIcon = () => {
    switch (suggestion.type) {
      case 'direct_mapping':
        return '🎯';
      case 'pattern_match':
        return '🔍';
      case 'semantic_equivalent':
        return '🧠';
      case 'framework_specific':
        return '🏗️';
      case 'best_practice':
        return '⭐';
      case 'performance_optimization':
        return '⚡';
      case 'security_improvement':
        return '🔒';
      default:
        return '💡';
    }
  };

  const getConfidenceColor = () => {
    if (suggestion.confidence >= 0.8) {
      return 'success';
    }
    if (suggestion.confidence >= 0.6) {
      return 'warning';
    }
    return 'danger';
  };

  return (
    <div
      className={`suggestion-card ${isSelected ? 'selected' : ''}`}
      onClick={onSelect}
    >
      <div className="suggestion-header">
        <div className="suggestion-type">
          <span className="type-icon">{getTypeIcon()}</span>
          <span className="type-text">{suggestion.type.replace('_', ' ')}</span>
        </div>
        <div className={`confidence-badge badge-${getConfidenceColor()}`}>
          {(suggestion.confidence * 100).toFixed(0)}%
        </div>
      </div>

      <div className="suggestion-content">
        <div className="description">
          {suggestion.description}
        </div>

        <div className="target-preview">
          <pre className="code-preview">
            {nodeToCode(suggestion.targetNode)}
          </pre>
        </div>

        {suggestion.reasoning && (
          <div className="reasoning">
            <strong>Reasoning:</strong> {suggestion.reasoning}
          </div>
        )}

        <div className="suggestion-actions">
          <button
            onClick={(e) => {
              e.stopPropagation();
              onPreview();
            }}
            className="btn btn-sm btn-outline-primary"
          >
                        Preview
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onRequestExplanation();
            }}
            className="btn btn-sm btn-outline-info"
          >
                        Explain
          </button>
        </div>

        {suggestion.alternatives.length > 0 && (
          <div className="alternatives-count">
                        +{suggestion.alternatives.length} alternatives available
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Code Comparison Component
 */
const CodeComparison: React.FC<CodeComparisonProps> = ({
  sourceCode,
  targetCode,
  sourceLanguage,
  targetLanguage,
  highlightDifferences = false,
}) => {
  return (
    <div className="code-comparison">
      <div className="comparison-header">
        <h3>Code Comparison</h3>
        {highlightDifferences && (
          <div className="highlight-toggle">
            <label>
              <input type="checkbox" checked={highlightDifferences} readOnly />
                            Highlight Differences
            </label>
          </div>
        )}
      </div>

      <div className="comparison-content">
        <div className="source-panel">
          <div className="panel-header">
            <h4>Source ({sourceLanguage})</h4>
          </div>
          <pre className="code-block source-code">
            {sourceCode}
          </pre>
        </div>

        <div className="target-panel">
          <div className="panel-header">
            <h4>Target ({targetLanguage})</h4>
          </div>
          <pre className="code-block target-code">
            {targetCode}
          </pre>
        </div>
      </div>
    </div>
  );
};

/**
 * Progress Indicator Component
 */
const ProgressIndicator: React.FC<ProgressIndicatorProps> = ({
  currentStep,
  totalSteps,
  completedSteps,
  qualityScore,
}) => {
  const progressPercentage = (completedSteps / totalSteps) * 100;
  const qualityPercentage = qualityScore * 100;

  return (
    <div className="progress-indicator">
      <div className="progress-stats">
        <div className="stat">
          <label>Step:</label>
          <span>{currentStep} / {totalSteps}</span>
        </div>
        <div className="stat">
          <label>Completed:</label>
          <span>{completedSteps} / {totalSteps}</span>
        </div>
        <div className="stat">
          <label>Quality:</label>
          <span>{qualityPercentage.toFixed(1)}%</span>
        </div>
      </div>

      <div className="progress-bars">
        <div className="progress-bar">
          <label>Translation Progress</label>
          <div className="bar">
            <div
              className="fill progress-fill"
              style={{ width: `${progressPercentage}%` }}
            />
          </div>
          <span>{progressPercentage.toFixed(1)}%</span>
        </div>

        <div className="progress-bar">
          <label>Quality Score</label>
          <div className="bar">
            <div
              className="fill quality-fill"
              style={{ width: `${qualityPercentage}%` }}
            />
          </div>
          <span>{qualityPercentage.toFixed(1)}%</span>
        </div>
      </div>
    </div>
  );
};

/**
 * Feedback Modal Component
 */
const FeedbackModal: React.FC<FeedbackModalProps> = ({
  isOpen,
  step,
  selectedSuggestion,
  onSubmit,
  onCancel,
}) => {
  const [action, setAction] = useState<UserAction>(UserAction.ACCEPT);
  const [rating, setRating] = useState<number>(3);
  const [comments, setComments] = useState<string>('');
  const [customModification, setCustomModification] = useState<string>('');

  const handleSubmit = () => {
    const feedback: UserFeedback = {
      stepId: step.id,
      action,
      selectedSuggestion: selectedSuggestion?.id,
      rating,
      comments: comments.trim() || undefined,
      timestamp: new Date(),
    };

    // Handle custom modification if provided
    if (action === UserAction.MODIFY && customModification.trim()) {
      // In a real implementation, this would parse the custom code
      // and create a proper AST node
      feedback.customModification = step.targetNode; // Placeholder
    }

    onSubmit(feedback);
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="feedback-modal-overlay">
      <div className="feedback-modal">
        <div className="modal-header">
          <h3>Provide Detailed Feedback</h3>
          <button onClick={onCancel} className="close-button">×</button>
        </div>

        <div className="modal-content">
          <div className="feedback-section">
            <label>Action:</label>
            <select
              value={action}
              onChange={(e) => setAction(e.target.value as UserAction)}
              className="form-control"
            >
              <option value={UserAction.ACCEPT}>Accept</option>
              <option value={UserAction.REJECT}>Reject</option>
              <option value={UserAction.MODIFY}>Modify</option>
              <option value={UserAction.SKIP}>Skip</option>
            </select>
          </div>

          <div className="feedback-section">
            <label>Rating (1-5):</label>
            <div className="rating-input">
              {[1, 2, 3, 4, 5].map(value => (
                <button
                  key={value}
                  onClick={() => setRating(value)}
                  className={`rating-button ${rating === value ? 'selected' : ''}`}
                >
                  {value}
                </button>
              ))}
            </div>
          </div>

          <div className="feedback-section">
            <label>Comments:</label>
            <textarea
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder="Optional comments about this translation step..."
              className="form-control"
              rows={3}
            />
          </div>

          {action === UserAction.MODIFY && (
            <div className="feedback-section">
              <label>Custom Modification:</label>
              <textarea
                value={customModification}
                onChange={(e) => setCustomModification(e.target.value)}
                placeholder="Enter your custom code modification..."
                className="form-control code-input"
                rows={5}
              />
            </div>
          )}

          {selectedSuggestion && (
            <div className="selected-suggestion">
              <h4>Selected Suggestion:</h4>
              <div className="suggestion-summary">
                <div><strong>Type:</strong> {selectedSuggestion.type}</div>
                <div><strong>Confidence:</strong> {(selectedSuggestion.confidence * 100).toFixed(1)}%</div>
                <div><strong>Description:</strong> {selectedSuggestion.description}</div>
              </div>
            </div>
          )}
        </div>

        <div className="modal-actions">
          <button onClick={onCancel} className="btn btn-secondary">
                        Cancel
          </button>
          <button onClick={handleSubmit} className="btn btn-primary">
                        Submit Feedback
          </button>
        </div>
      </div>
    </div>
  );
};

/**
 * Engine Status Panel Component
 */
const EngineStatusPanel: React.FC<EngineStatusPanelProps> = ({
  orchestrator,
  onConfigChange,
}) => {
  const [engineHealth, setEngineHealth] = useState(orchestrator.getEngineHealth());
  const [availableEngines, setAvailableEngines] = useState(orchestrator.getAvailableEngineNames());
  const [currentStrategy, setCurrentStrategy] = useState<EngineSelectionStrategy>(EngineSelectionStrategy.PRIORITY);

  const handleRefreshHealth = useCallback(async () => {
    await orchestrator.forceHealthCheck();
    setEngineHealth(orchestrator.getEngineHealth());
    setAvailableEngines(orchestrator.getAvailableEngineNames());
  }, [orchestrator]);

  const handleStrategyChange = useCallback((strategy: EngineSelectionStrategy) => {
    setCurrentStrategy(strategy);
    if (onConfigChange) {
      onConfigChange({ selectionStrategy: strategy });
    }
  }, [onConfigChange]);

  return (
    <div className="engine-status-panel">
      <div className="panel-header">
        <h3>Translation Engine Status</h3>
        <button onClick={handleRefreshHealth} className="btn btn-sm btn-secondary">
                    Refresh
        </button>
      </div>

      <div className="engine-controls">
        <OrchestratorControls
          currentStrategy={currentStrategy}
          availableEngines={availableEngines}
          onStrategyChange={handleStrategyChange}
          onForceHealthCheck={handleRefreshHealth}
        />
      </div>

      <div className="engine-health-grid">
        {Object.entries(engineHealth).map(([engineName, health]) => (
          <EngineHealthIndicator
            key={engineName}
            engineName={engineName}
            isHealthy={health.isHealthy}
            averageResponseTime={health.averageResponseTime}
            successRate={health.successRate}
            lastError={health.lastError}
          />
        ))}
      </div>
    </div>
  );
};

/**
 * Engine Health Indicator Component
 */
const EngineHealthIndicator: React.FC<EngineHealthIndicatorProps> = ({
  engineName,
  isHealthy,
  averageResponseTime,
  successRate,
  lastError,
}) => {
  const getEngineDisplayName = (name: string) => {
    switch (name) {
      case 'rule-based': return 'Rule-Based Engine';
      case 'pattern-based': return 'Pattern-Based Engine';
      case 'llm-enhanced': return 'LLM-Enhanced Engine';
      default: return name;
    }
  };

  const getEngineDescription = (name: string) => {
    switch (name) {
      case 'rule-based': return 'Direct syntax mappings, always available offline';
      case 'pattern-based': return 'Learned patterns from successful translations';
      case 'llm-enhanced': return 'AI-powered complex semantic understanding';
      default: return 'Translation engine';
    }
  };

  return (
    <div className={`engine-health-card ${isHealthy ? 'healthy' : 'unhealthy'}`}>
      <div className="engine-header">
        <div className="engine-name">
          <h4>{getEngineDisplayName(engineName)}</h4>
          <div className={`status-indicator ${isHealthy ? 'online' : 'offline'}`}>
            {isHealthy ? '🟢' : '🔴'} {isHealthy ? 'Online' : 'Offline'}
          </div>
        </div>
      </div>

      <div className="engine-description">
        {getEngineDescription(engineName)}
      </div>

      <div className="engine-metrics">
        <div className="metric">
          <span className="metric-label">Response Time:</span>
          <span className="metric-value">{averageResponseTime.toFixed(0)}ms</span>
        </div>
        <div className="metric">
          <span className="metric-label">Success Rate:</span>
          <span className="metric-value">{(successRate * 100).toFixed(1)}%</span>
        </div>
      </div>

      {lastError && (
        <div className="engine-error">
          <span className="error-label">Last Error:</span>
          <span className="error-message">{lastError}</span>
        </div>
      )}
    </div>
  );
};

/**
 * Orchestrator Controls Component
 */
const OrchestratorControls: React.FC<OrchestratorControlsProps> = ({
  currentStrategy,
  availableEngines,
  onStrategyChange,
  onForceHealthCheck,
}) => {
  const strategyOptions = [
    { value: EngineSelectionStrategy.PRIORITY, label: 'Priority Order', description: 'Use highest priority available engine' },
    { value: EngineSelectionStrategy.SPEED, label: 'Fastest', description: 'Use fastest available engine' },
    { value: EngineSelectionStrategy.COST, label: 'Most Cost-Effective', description: 'Use most cost-effective engine' },
    { value: EngineSelectionStrategy.QUALITY, label: 'Highest Quality', description: 'Use highest quality engine' },
    { value: EngineSelectionStrategy.RELIABILITY, label: 'Most Reliable', description: 'Use most reliable engine' },
    { value: EngineSelectionStrategy.BEST_RESULT, label: 'Best Result', description: 'Try all engines and pick best result' },
  ];

  return (
    <div className="orchestrator-controls">
      <div className="control-group">
        <label htmlFor="strategy-select">Translation Strategy:</label>
        <select
          id="strategy-select"
          value={currentStrategy}
          onChange={(e) => onStrategyChange(e.target.value as EngineSelectionStrategy)}
          className="form-control"
        >
          {strategyOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <div className="strategy-description">
          {strategyOptions.find(opt => opt.value === currentStrategy)?.description}
        </div>
      </div>

      <div className="control-group">
        <label>Available Engines:</label>
        <div className="available-engines">
          {availableEngines.length > 0 ? (
            availableEngines.map(engine => (
              <span key={engine} className="engine-tag">
                {engine}
              </span>
            ))
          ) : (
            <span className="no-engines">No engines available</span>
          )}
        </div>
      </div>

      <div className="control-actions">
        <button onClick={onForceHealthCheck} className="btn btn-sm btn-primary">
                    Check Engine Health
        </button>
      </div>
    </div>
  );
};

/**
 * Utility function to convert AST node to code string
 */
function nodeToCode(node: ZeroCopyASTNode | undefined): string {
  if (!node) {
    return '';
  }

  // This is a simplified implementation
  // In practice, this would use a proper code generator for each language
  const children = node.getChildren();
  if (children.length === 0) {
    return `${node.nodeType}(${node.value || ''})`;
  }

  const childrenCode = children.map(child => nodeToCode(child)).join(', ');
  return `${node.nodeType}(${childrenCode})`;
}

export default InteractiveTranslationPanel;

