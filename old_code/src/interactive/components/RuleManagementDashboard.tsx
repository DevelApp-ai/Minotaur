/**
 * Rule Management Dashboard Component
 *
 * This component provides a comprehensive dashboard for managing transformation rules,
 * including browsing, searching, filtering, editing, testing, and organizing rules.
 *
 * Key Features:
 * - Rule library browser with search and filtering
 * - Rule performance metrics and analytics
 * - Bulk operations (import/export, enable/disable)
 * - Version control and rule history
 * - Community rule sharing
 * - Rule categories and tagging
 *
 * @author Minotaur Team
 * @since 2024
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';

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
    communityRating?: number;
    communityVotes?: number;
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

interface RuleManagementDashboardProps {
    rules: TransformationRule[];
    onRuleEdit: (rule: TransformationRule) => void;
    onRuleDelete: (ruleId: string) => void;
    onRuleTest: (rule: TransformationRule) => void;
    onRuleToggle: (ruleId: string, enabled: boolean) => void;
    onRulesImport: (rules: TransformationRule[]) => void;
    onRulesExport: (ruleIds: string[]) => void;
    onRuleDuplicate: (rule: TransformationRule) => void;
    className?: string;
}

interface RuleFilters {
    search: string;
    sourceLanguage: string;
    targetLanguage: string;
    category: string;
    createdBy: string;
    enabled: boolean | null;
    complexity: string;
    minSuccessRate: number;
    tags: string[];
    dateRange: {
        start: Date | null;
        end: Date | null;
    };
}

interface RuleSortOptions {
    field: 'name' | 'createdAt' | 'lastModified' | 'usageCount' | 'successRate' | 'quality';
    direction: 'asc' | 'desc';
}

export const RuleManagementDashboard: React.FC<RuleManagementDashboardProps> = ({
  rules,
  onRuleEdit,
  onRuleDelete,
  onRuleTest,
  onRuleToggle,
  onRulesImport,
  onRulesExport,
  onRuleDuplicate,
  className = '',
}) => {
  const [filters, setFilters] = useState<RuleFilters>({
    search: '',
    sourceLanguage: '',
    targetLanguage: '',
    category: '',
    createdBy: '',
    enabled: null,
    complexity: '',
    minSuccessRate: 0,
    tags: [],
    dateRange: { start: null, end: null },
  });

  const [sortOptions, setSortOptions] = useState<RuleSortOptions>({
    field: 'lastModified',
    direction: 'desc',
  });

  const [selectedRules, setSelectedRules] = useState<Set<string>>(new Set());
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'table'>('grid');
  const [showFilters, setShowFilters] = useState(false);
  const [selectedRule, setSelectedRule] = useState<TransformationRule | null>(null);
  const [showRuleDetails, setShowRuleDetails] = useState(false);

  // Get unique values for filter options
  const filterOptions = useMemo(() => {
    const sourceLanguages = [...new Set(rules.map(r => r.sourceLanguage))].sort();
    const targetLanguages = [...new Set(rules.map(r => r.targetLanguage))].sort();
    const categories = [...new Set(rules.map(r => r.category))].sort();
    const creators = [...new Set(rules.map(r => r.createdBy))].sort();
    const complexities = ['simple', 'moderate', 'complex'];
    const allTags = [...new Set(rules.flatMap(r => r.tags))].sort();

    return {
      sourceLanguages,
      targetLanguages,
      categories,
      creators,
      complexities,
      allTags,
    };
  }, [rules]);

  // Filter and sort rules
  const filteredAndSortedRules = useMemo(() => {
    const filtered = rules.filter(rule => {
      // Text search
      if (filters.search) {
        const searchLower = filters.search.toLowerCase();
        const matchesSearch =
                    rule.name.toLowerCase().includes(searchLower) ||
                    rule.description.toLowerCase().includes(searchLower) ||
                    rule.tags.some(tag => tag.toLowerCase().includes(searchLower));
        if (!matchesSearch) {
          return false;
        }
      }

      // Language filters
      if (filters.sourceLanguage && rule.sourceLanguage !== filters.sourceLanguage) {
        return false;
      }
      if (filters.targetLanguage && rule.targetLanguage !== filters.targetLanguage) {
        return false;
      }

      // Category filter
      if (filters.category && rule.category !== filters.category) {
        return false;
      }

      // Creator filter
      if (filters.createdBy && rule.createdBy !== filters.createdBy) {
        return false;
      }

      // Enabled filter
      if (filters.enabled !== null && rule.enabled !== filters.enabled) {
        return false;
      }

      // Complexity filter
      if (filters.complexity && rule.complexity !== filters.complexity) {
        return false;
      }

      // Success rate filter
      if (rule.successRate < filters.minSuccessRate / 100) {
        return false;
      }

      // Tags filter
      if (filters.tags.length > 0) {
        const hasAllTags = filters.tags.every(tag => rule.tags.includes(tag));
        if (!hasAllTags) {
          return false;
        }
      }

      // Date range filter
      if (filters.dateRange.start && rule.createdAt < filters.dateRange.start) {
        return false;
      }
      if (filters.dateRange.end && rule.createdAt > filters.dateRange.end) {
        return false;
      }

      return true;
    });

    // Sort rules
    filtered.sort((a, b) => {
      let aValue: any = a[sortOptions.field];
      let bValue: any = b[sortOptions.field];

      if (aValue instanceof Date) {
        aValue = aValue.getTime();
      }
      if (bValue instanceof Date) {
        bValue = bValue.getTime();
      }

      if (typeof aValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }

      const comparison = aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      return sortOptions.direction === 'asc' ? comparison : -comparison;
    });

    return filtered;
  }, [rules, filters, sortOptions]);

  // Statistics
  const statistics = useMemo(() => {
    const total = rules.length;
    const enabled = rules.filter(r => r.enabled).length;
    const disabled = total - enabled;
    const avgSuccessRate = rules.length > 0 ?
      rules.reduce((sum, r) => sum + r.successRate, 0) / rules.length : 0;
    const totalUsage = rules.reduce((sum, r) => sum + r.usageCount, 0);

    const byLanguagePair = rules.reduce((acc, rule) => {
      const pair = `${rule.sourceLanguage} → ${rule.targetLanguage}`;
      acc[pair] = (acc[pair] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    const byCategory = rules.reduce((acc, rule) => {
      acc[rule.category] = (acc[rule.category] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return {
      total,
      enabled,
      disabled,
      avgSuccessRate,
      totalUsage,
      byLanguagePair,
      byCategory,
    };
  }, [rules]);

  const handleFilterChange = useCallback(<K extends keyof RuleFilters>(
    key: K,
    value: RuleFilters[K],
  ) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSortChange = useCallback((field: RuleSortOptions['field']) => {
    setSortOptions(prev => ({
      field,
      direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc',
    }));
  }, []);

  const handleRuleSelection = useCallback((ruleId: string, selected: boolean) => {
    setSelectedRules(prev => {
      const newSet = new Set(prev);
      if (selected) {
        newSet.add(ruleId);
      } else {
        newSet.delete(ruleId);
      }
      return newSet;
    });
  }, []);

  const handleSelectAll = useCallback(() => {
    if (selectedRules.size === filteredAndSortedRules.length) {
      setSelectedRules(new Set());
    } else {
      setSelectedRules(new Set(filteredAndSortedRules.map(r => r.id)));
    }
  }, [selectedRules.size, filteredAndSortedRules]);

  const handleBulkAction = useCallback((action: 'enable' | 'disable' | 'delete' | 'export') => {
    const selectedRuleIds = Array.from(selectedRules);

    switch (action) {
      case 'enable':
        selectedRuleIds.forEach(id => onRuleToggle(id, true));
        break;
      case 'disable':
        selectedRuleIds.forEach(id => onRuleToggle(id, false));
        break;
      case 'delete':
        // eslint-disable-next-line no-alert
        if (window.confirm(`Delete ${selectedRuleIds.length} selected rules?`)) {
          selectedRuleIds.forEach(id => onRuleDelete(id));
        }
        break;
      case 'export':
        onRulesExport(selectedRuleIds);
        break;
    }

    setSelectedRules(new Set());
  }, [selectedRules, onRuleToggle, onRuleDelete, onRulesExport]);

  const handleRuleClick = useCallback((rule: TransformationRule) => {
    setSelectedRule(rule);
    setShowRuleDetails(true);
  }, []);

  const clearFilters = useCallback(() => {
    setFilters({
      search: '',
      sourceLanguage: '',
      targetLanguage: '',
      category: '',
      createdBy: '',
      enabled: null,
      complexity: '',
      minSuccessRate: 0,
      tags: [],
      dateRange: { start: null, end: null },
    });
  }, []);

  return (
    <div className={`rule-management-dashboard ${className}`}>
      {/* Header */}
      <div className="dashboard-header">
        <div className="header-title">
          <h2>🛠️ Rule Management Dashboard</h2>
          <p>Manage, organize, and optimize your transformation rules</p>
        </div>

        <div className="header-actions">
          <button
            onClick={() => setShowFilters(!showFilters)}
            className={`btn btn-outline filter-toggle ${showFilters ? 'active' : ''}`}
          >
                        🔍 Filters
          </button>

          <div className="view-mode-selector">
            <button
              onClick={() => setViewMode('grid')}
              className={`view-btn ${viewMode === 'grid' ? 'active' : ''}`}
              title="Grid View"
            >
                            ⊞
            </button>
            <button
              onClick={() => setViewMode('list')}
              className={`view-btn ${viewMode === 'list' ? 'active' : ''}`}
              title="List View"
            >
                            ☰
            </button>
            <button
              onClick={() => setViewMode('table')}
              className={`view-btn ${viewMode === 'table' ? 'active' : ''}`}
              title="Table View"
            >
                            ⊞
            </button>
          </div>
        </div>
      </div>

      {/* Statistics */}
      <div className="dashboard-stats">
        <div className="stat-card">
          <div className="stat-value">{statistics.total}</div>
          <div className="stat-label">Total Rules</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{statistics.enabled}</div>
          <div className="stat-label">Enabled</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{(statistics.avgSuccessRate * 100).toFixed(1)}%</div>
          <div className="stat-label">Avg Success Rate</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{statistics.totalUsage.toLocaleString()}</div>
          <div className="stat-label">Total Usage</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{Object.keys(statistics.byLanguagePair).length}</div>
          <div className="stat-label">Language Pairs</div>
        </div>
      </div>

      {/* Filters Panel */}
      {showFilters && (
        <div className="filters-panel">
          <div className="filters-header">
            <h3>Filter Rules</h3>
            <button onClick={clearFilters} className="btn btn-sm btn-outline">
                            Clear All
            </button>
          </div>

          <div className="filters-grid">
            {/* Search */}
            <div className="filter-group">
              <label htmlFor="search-filter">Search:</label>
              <input
                id="search-filter"
                type="text"
                placeholder="Search rules, descriptions, tags..."
                value={filters.search}
                onChange={(e) => handleFilterChange('search', e.target.value)}
                className="form-control"
              />
            </div>

            {/* Source Language */}
            <div className="filter-group">
              <label htmlFor="source-language-filter">Source Language:</label>
              <select
                id="source-language-filter"
                value={filters.sourceLanguage}
                onChange={(e) => handleFilterChange('sourceLanguage', e.target.value)}
                className="form-control"
              >
                <option value="">All Languages</option>
                {filterOptions.sourceLanguages.map(lang => (
                  <option key={lang} value={lang}>{lang.toUpperCase()}</option>
                ))}
              </select>
            </div>

            {/* Target Language */}
            <div className="filter-group">
              <label htmlFor="target-language-filter">Target Language:</label>
              <select
                id="target-language-filter"
                value={filters.targetLanguage}
                onChange={(e) => handleFilterChange('targetLanguage', e.target.value)}
                className="form-control"
              >
                <option value="">All Languages</option>
                {filterOptions.targetLanguages.map(lang => (
                  <option key={lang} value={lang}>{lang.toUpperCase()}</option>
                ))}
              </select>
            </div>

            {/* Category */}
            <div className="filter-group">
              <label htmlFor="category-filter">Category:</label>
              <select
                id="category-filter"
                value={filters.category}
                onChange={(e) => handleFilterChange('category', e.target.value)}
                className="form-control"
              >
                <option value="">All Categories</option>
                {filterOptions.categories.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>

            {/* Created By */}
            <div className="filter-group">
              <label htmlFor="created-by-filter">Created By:</label>
              <select
                id="created-by-filter"
                value={filters.createdBy}
                onChange={(e) => handleFilterChange('createdBy', e.target.value)}
                className="form-control"
              >
                <option value="">All Creators</option>
                {filterOptions.creators.map(creator => (
                  <option key={creator} value={creator}>
                    {creator === 'user' ? 'User Created' :
                      creator === 'llm' ? 'AI Generated' : 'Pattern Learning'}
                  </option>
                ))}
              </select>
            </div>

            {/* Enabled Status */}
            <div className="filter-group">
              <label htmlFor="enabled-filter">Status:</label>
              <select
                id="enabled-filter"
                value={filters.enabled === null ? '' : filters.enabled.toString()}
                onChange={(e) => handleFilterChange('enabled',
                  e.target.value === '' ? null : e.target.value === 'true')}
                className="form-control"
              >
                <option value="">All Rules</option>
                <option value="true">Enabled Only</option>
                <option value="false">Disabled Only</option>
              </select>
            </div>

            {/* Complexity */}
            <div className="filter-group">
              <label htmlFor="complexity-filter">Complexity:</label>
              <select
                id="complexity-filter"
                value={filters.complexity}
                onChange={(e) => handleFilterChange('complexity', e.target.value)}
                className="form-control"
              >
                <option value="">All Complexities</option>
                {filterOptions.complexities.map(complexity => (
                  <option key={complexity} value={complexity}>
                    {complexity.charAt(0).toUpperCase() + complexity.slice(1)}
                  </option>
                ))}
              </select>
            </div>

            {/* Success Rate */}
            <div className="filter-group">
              <label htmlFor="success-rate-filter">
                                Min Success Rate: {filters.minSuccessRate}%
              </label>
              <input
                id="success-rate-filter"
                type="range"
                min="0"
                max="100"
                value={filters.minSuccessRate}
                onChange={(e) => handleFilterChange('minSuccessRate', parseInt(e.target.value))}
                className="form-control range-input"
              />
            </div>
          </div>
        </div>
      )}

      {/* Bulk Actions */}
      {selectedRules.size > 0 && (
        <div className="bulk-actions">
          <div className="bulk-info">
            <span className="selected-count">{selectedRules.size} rules selected</span>
          </div>
          <div className="bulk-buttons">
            <button
              onClick={() => handleBulkAction('enable')}
              className="btn btn-sm btn-success"
            >
                            ✅ Enable
            </button>
            <button
              onClick={() => handleBulkAction('disable')}
              className="btn btn-sm btn-warning"
            >
                            ⏸️ Disable
            </button>
            <button
              onClick={() => handleBulkAction('export')}
              className="btn btn-sm btn-primary"
            >
                            📤 Export
            </button>
            <button
              onClick={() => handleBulkAction('delete')}
              className="btn btn-sm btn-danger"
            >
                            🗑️ Delete
            </button>
          </div>
        </div>
      )}

      {/* Rules List */}
      <div className="rules-section">
        <div className="rules-header">
          <div className="rules-count">
            {filteredAndSortedRules.length} of {rules.length} rules
          </div>

          <div className="rules-controls">
            <div className="sort-controls">
              <label htmlFor="sort-field">Sort by:</label>
              <select
                id="sort-field"
                value={sortOptions.field}
                onChange={(e) => handleSortChange(e.target.value as RuleSortOptions['field'])}
                className="form-control sort-select"
              >
                <option value="name">Name</option>
                <option value="createdAt">Created Date</option>
                <option value="lastModified">Last Modified</option>
                <option value="usageCount">Usage Count</option>
                <option value="successRate">Success Rate</option>
                <option value="quality">Quality</option>
              </select>
              <button
                onClick={() => setSortOptions(prev => ({
                  ...prev,
                  direction: prev.direction === 'asc' ? 'desc' : 'asc',
                }))}
                className="btn btn-sm btn-outline sort-direction"
                title={`Sort ${sortOptions.direction === 'asc' ? 'Descending' : 'Ascending'}`}
              >
                {sortOptions.direction === 'asc' ? '↑' : '↓'}
              </button>
            </div>

            <button
              onClick={handleSelectAll}
              className="btn btn-sm btn-outline select-all-btn"
            >
              {selectedRules.size === filteredAndSortedRules.length ? 'Deselect All' : 'Select All'}
            </button>
          </div>
        </div>

        <div className={`rules-container ${viewMode}-view`}>
          {filteredAndSortedRules.length === 0 ? (
            <div className="no-rules">
              <div className="no-rules-icon">📝</div>
              <div className="no-rules-text">
                {rules.length === 0
                  ? 'No transformation rules found. Create your first rule to get started!'
                  : 'No rules match the current filters. Try adjusting your search criteria.'
                }
              </div>
              {rules.length === 0 && (
                <button className="btn btn-primary create-rule-btn">
                                    Create First Rule
                </button>
              )}
            </div>
          ) : (
            filteredAndSortedRules.map(rule => (
              <RuleCard
                key={rule.id}
                rule={rule}
                viewMode={viewMode}
                selected={selectedRules.has(rule.id)}
                onSelect={(selected) => handleRuleSelection(rule.id, selected)}
                onClick={() => handleRuleClick(rule)}
                onEdit={() => onRuleEdit(rule)}
                onDelete={() => onRuleDelete(rule.id)}
                onTest={() => onRuleTest(rule)}
                onToggle={(enabled) => onRuleToggle(rule.id, enabled)}
                onDuplicate={() => onRuleDuplicate(rule)}
              />
            ))
          )}
        </div>
      </div>

      {/* Rule Details Modal */}
      {showRuleDetails && selectedRule && (
        <RuleDetailsModal
          rule={selectedRule}
          onClose={() => setShowRuleDetails(false)}
          onEdit={() => {
            onRuleEdit(selectedRule);
            setShowRuleDetails(false);
          }}
          onTest={() => {
            onRuleTest(selectedRule);
            setShowRuleDetails(false);
          }}
        />
      )}
    </div>
  );
};

/**
 * Rule Card Component for different view modes
 */
interface RuleCardProps {
    rule: TransformationRule;
    viewMode: 'grid' | 'list' | 'table';
    selected: boolean;
    onSelect: (selected: boolean) => void;
    onClick: () => void;
    onEdit: () => void;
    onDelete: () => void;
    onTest: () => void;
    onToggle: (enabled: boolean) => void;
    onDuplicate: () => void;
}

const RuleCard: React.FC<RuleCardProps> = ({
  rule,
  viewMode,
  selected,
  onSelect,
  onClick,
  onEdit,
  onDelete,
  onTest,
  onToggle,
  onDuplicate,
}) => {
  const getComplexityColor = (complexity: string) => {
    switch (complexity) {
      case 'simple': return '#22c55e';
      case 'moderate': return '#f59e0b';
      case 'complex': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getCreatorIcon = (createdBy: string) => {
    switch (createdBy) {
      case 'user': return '👤';
      case 'llm': return '🤖';
      case 'pattern-learning': return '🧠';
      default: return '❓';
    }
  };

  if (viewMode === 'table') {
    return (
      <tr className={`rule-table-row ${selected ? 'selected' : ''}`}>
        <td>
          <input
            type="checkbox"
            checked={selected}
            onChange={(e) => onSelect(e.target.checked)}
            onClick={(e) => e.stopPropagation()}
          />
        </td>
        <td onClick={onClick} className="rule-name-cell">
          <div className="rule-name">{rule.name}</div>
          <div className="rule-languages">
            {rule.sourceLanguage.toUpperCase()} → {rule.targetLanguage.toUpperCase()}
          </div>
        </td>
        <td>{rule.category}</td>
        <td>
          <span className="creator-badge">
            {getCreatorIcon(rule.createdBy)} {rule.createdBy}
          </span>
        </td>
        <td>
          <div
            className="complexity-badge"
            style={{ backgroundColor: getComplexityColor(rule.complexity) }}
          >
            {rule.complexity}
          </div>
        </td>
        <td>{(rule.successRate * 100).toFixed(1)}%</td>
        <td>{rule.usageCount}</td>
        <td>
          <label className="toggle-switch">
            <input
              type="checkbox"
              checked={rule.enabled}
              onChange={(e) => onToggle(e.target.checked)}
              onClick={(e) => e.stopPropagation()}
            />
            <span className="toggle-slider"></span>
          </label>
        </td>
        <td>
          <div className="rule-actions">
            <button onClick={(e) => {
              e.stopPropagation(); onEdit();
            }} className="btn-icon" title="Edit">
                            ✏️
            </button>
            <button onClick={(e) => {
              e.stopPropagation(); onTest();
            }} className="btn-icon" title="Test">
                            🧪
            </button>
            <button onClick={(e) => {
              e.stopPropagation(); onDuplicate();
            }} className="btn-icon" title="Duplicate">
                            📋
            </button>
            <button onClick={(e) => {
              e.stopPropagation(); onDelete();
            }} className="btn-icon" title="Delete">
                            🗑️
            </button>
          </div>
        </td>
      </tr>
    );
  }

  return (
    <div className={`rule-card ${viewMode}-card ${selected ? 'selected' : ''} ${!rule.enabled ? 'disabled' : ''}`}>
      <div className="rule-card-header">
        <div className="rule-card-checkbox">
          <input
            type="checkbox"
            checked={selected}
            onChange={(e) => onSelect(e.target.checked)}
            onClick={(e) => e.stopPropagation()}
          />
        </div>

        <div className="rule-card-title" onClick={onClick}>
          <h4 className="rule-name">{rule.name}</h4>
          <div className="rule-languages">
            {rule.sourceLanguage.toUpperCase()} → {rule.targetLanguage.toUpperCase()}
          </div>
        </div>

        <div className="rule-card-status">
          <label className="toggle-switch">
            <input
              type="checkbox"
              checked={rule.enabled}
              onChange={(e) => onToggle(e.target.checked)}
              onClick={(e) => e.stopPropagation()}
            />
            <span className="toggle-slider"></span>
          </label>
        </div>
      </div>

      <div className="rule-card-content" onClick={onClick}>
        <p className="rule-description">{rule.description}</p>

        <div className="rule-metadata">
          <div className="metadata-row">
            <span className="metadata-item">
              <span className="metadata-icon">{getCreatorIcon(rule.createdBy)}</span>
              <span className="metadata-text">{rule.createdBy}</span>
            </span>
            <span className="metadata-item">
              <span className="metadata-icon">📊</span>
              <span className="metadata-text">{(rule.successRate * 100).toFixed(1)}%</span>
            </span>
            <span className="metadata-item">
              <span className="metadata-icon">🔢</span>
              <span className="metadata-text">{rule.usageCount} uses</span>
            </span>
          </div>

          <div className="metadata-row">
            <span className="metadata-item">
              <span className="metadata-icon">📁</span>
              <span className="metadata-text">{rule.category}</span>
            </span>
            <div
              className="complexity-badge"
              style={{ backgroundColor: getComplexityColor(rule.complexity) }}
            >
              {rule.complexity}
            </div>
          </div>
        </div>

        <div className="rule-tags">
          {rule.tags.slice(0, 3).map(tag => (
            <span key={tag} className="rule-tag">{tag}</span>
          ))}
          {rule.tags.length > 3 && (
            <span className="rule-tag more-tags">+{rule.tags.length - 3}</span>
          )}
        </div>
      </div>

      <div className="rule-card-actions">
        <button onClick={(e) => {
          e.stopPropagation(); onEdit();
        }} className="btn btn-sm btn-outline" title="Edit">
                    ✏️ Edit
        </button>
        <button onClick={(e) => {
          e.stopPropagation(); onTest();
        }} className="btn btn-sm btn-outline" title="Test">
                    🧪 Test
        </button>
        <button onClick={(e) => {
          e.stopPropagation(); onDuplicate();
        }} className="btn btn-sm btn-outline" title="Duplicate">
                    📋 Copy
        </button>
        <button onClick={(e) => {
          e.stopPropagation(); onDelete();
        }} className="btn btn-sm btn-danger" title="Delete">
                    🗑️
        </button>
      </div>
    </div>
  );
};

/**
 * Rule Details Modal Component
 */
interface RuleDetailsModalProps {
    rule: TransformationRule;
    onClose: () => void;
    onEdit: () => void;
    onTest: () => void;
}

const RuleDetailsModal: React.FC<RuleDetailsModalProps> = ({
  rule,
  onClose,
  onEdit,
  onTest,
}) => {
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content rule-details-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{rule.name}</h3>
          <button onClick={onClose} className="modal-close">×</button>
        </div>

        <div className="modal-body">
          <div className="rule-details-content">
            <div className="detail-section">
              <h4>Description</h4>
              <p>{rule.description}</p>
            </div>

            <div className="detail-section">
              <h4>Pattern</h4>
              <pre className="code-block">{rule.pattern.pattern}</pre>
            </div>

            <div className="detail-section">
              <h4>Transformation</h4>
              <pre className="code-block">{rule.transformation.template}</pre>
            </div>

            <div className="detail-section">
              <h4>Statistics</h4>
              <div className="stats-grid">
                <div className="stat-item">
                  <span className="stat-label">Success Rate:</span>
                  <span className="stat-value">{(rule.successRate * 100).toFixed(1)}%</span>
                </div>
                <div className="stat-item">
                  <span className="stat-label">Usage Count:</span>
                  <span className="stat-value">{rule.usageCount}</span>
                </div>
                <div className="stat-item">
                  <span className="stat-label">Quality Score:</span>
                  <span className="stat-value">{(rule.quality * 100).toFixed(1)}%</span>
                </div>
                <div className="stat-item">
                  <span className="stat-label">Confidence:</span>
                  <span className="stat-value">{(rule.confidence * 100).toFixed(1)}%</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button onClick={onEdit} className="btn btn-primary">
                        ✏️ Edit Rule
          </button>
          <button onClick={onTest} className="btn btn-secondary">
                        🧪 Test Rule
          </button>
          <button onClick={onClose} className="btn btn-outline">
                        Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default RuleManagementDashboard;

