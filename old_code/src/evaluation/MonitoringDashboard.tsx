/**
 * Real-Time Monitoring Dashboard for Golem Quality Testing System
 *
 * Provides comprehensive visual monitoring for 14-hour evaluation runs
 * with real-time updates, interactive charts, and system controls.
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  LineChart, Line, AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts';

interface SystemHealth {
    timestamp: string;
    cpu: { usage: number; cores: number; loadAverage: number[] };
    memory: { total: number; free: number; used: number; usagePercent: number };
    disk: { total: number; free: number; used: number; usagePercent: number };
    network: { isConnected: boolean; latency: number };
    evaluation: {
        isRunning: boolean;
        phase: string;
        progress: number;
        problemsCompleted: number;
        problemsTotal: number;
        successRate: number;
        errorRate: number;
        averageTimePerProblem: number;
        estimatedTimeRemaining: number;
    };
    api: {
        isHealthy: boolean;
        responseTime: number;
        successRate: number;
        rateLimitStatus: string;
        tokensUsed: number;
        requestsThisHour: number;
    };
}

interface Alert {
    timestamp: string;
    type: string;
    message: string;
    severity: 'info' | 'warning' | 'error';
}

interface DashboardProps {
    websocketUrl?: string;
    refreshInterval?: number;
    theme?: 'light' | 'dark' | 'auto';
}

export const MonitoringDashboard: React.FC<DashboardProps> = ({
  websocketUrl = 'ws://localhost:3001',
  refreshInterval = 5000,
  theme = 'auto',
}) => {
  // State management
  const [currentHealth, setCurrentHealth] = useState<SystemHealth | null>(null);
  const [healthHistory, setHealthHistory] = useState<SystemHealth[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [selectedTimeRange, setSelectedTimeRange] = useState('1h');
  const [activeTab, setActiveTab] = useState('overview');

  // WebSocket connection for real-time updates
  useEffect(() => {
    const ws = new WebSocket(websocketUrl);

    ws.onopen = () => {
      setIsConnected(true);
    // eslint-disable-next-line no-console
      console.log('Dashboard connected to monitoring system');
    };

    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);

      switch (data.type) {
        case 'healthUpdate':
          setCurrentHealth(data.payload);
          setHealthHistory(prev => [...prev.slice(-100), data.payload]);
          break;
        case 'alert':
          setAlerts(prev => [data.payload, ...prev.slice(0, 99)]);
          break;
        case 'historyUpdate':
          setHealthHistory(data.payload);
          break;
      }
    };

    ws.onclose = () => {
      setIsConnected(false);
    // eslint-disable-next-line no-console
      console.log('Dashboard disconnected from monitoring system');
    };

    ws.onerror = (error) => {
    // eslint-disable-next-line no-console
      console.error('WebSocket error:', error);
      setIsConnected(false);
    };

    return () => {
      ws.close();
    };
  }, [websocketUrl]);

  // Fallback polling if WebSocket fails
  useEffect(() => {
    if (!isConnected) {
      const interval = setInterval(async () => {
        try {
          const response = await fetch('/api/health');
          const health = await response.json();
          setCurrentHealth(health);
          setHealthHistory(prev => [...prev.slice(-100), health]);
        } catch (error) {
    // eslint-disable-next-line no-console
          console.error('Failed to fetch health data:', error);
        }
      }, refreshInterval);

      return () => clearInterval(interval);
    }
  }, [isConnected, refreshInterval]);

  // Helper functions
  const formatBytes = (bytes: number): string => {
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) {
      return '0 B';
    }
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDuration = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    return `${hours}h ${minutes}m ${secs}s`;
  };

  const getStatusColor = (value: number, thresholds: { warning: number; error: number }): string => {
    if (value >= thresholds.error) {
      return '#ef4444';
    }
    if (value >= thresholds.warning) {
      return '#f59e0b';
    }
    return '#10b981';
  };

  const getFilteredHistory = useCallback(() => {
    if (!healthHistory.length) {
      return [];
    }

    const now = Date.now();
    const timeRanges = {
      '15m': 15 * 60 * 1000,
      '1h': 60 * 60 * 1000,
      '6h': 6 * 60 * 60 * 1000,
      '24h': 24 * 60 * 60 * 1000,
    };

    const cutoff = now - timeRanges[selectedTimeRange as keyof typeof timeRanges];
    return healthHistory.filter(h => new Date(h.timestamp).getTime() > cutoff);
  }, [healthHistory, selectedTimeRange]);

  // Render components
  const renderConnectionStatus = () => (
    <div className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
      <div className="status-indicator">
        <div className={`indicator-dot ${isConnected ? 'green' : 'red'}`}></div>
        <span>{isConnected ? 'Connected' : 'Disconnected'}</span>
      </div>
      {currentHealth && (
        <div className="last-update">
                    Last update: {new Date(currentHealth.timestamp).toLocaleTimeString()}
        </div>
      )}
    </div>
  );

  const renderSystemOverview = () => {
    if (!currentHealth) {
      return <div className="loading">Loading system data...</div>;
    }

    const { cpu, memory, disk, network, evaluation, api } = currentHealth;

    return (
      <div className="system-overview">
        <div className="overview-grid">
          {/* System Resources */}
          <div className="metric-card">
            <h3>CPU Usage</h3>
            <div className="metric-value">
              <span className="value" style={{ color: getStatusColor(cpu.usage, { warning: 70, error: 85 }) }}>
                {cpu.usage.toFixed(1)}%
              </span>
              <div className="metric-details">
                <div>Cores: {cpu.cores}</div>
                <div>Load: {cpu.loadAverage[0]?.toFixed(2)}</div>
              </div>
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{
                  width: `${cpu.usage}%`,
                  backgroundColor: getStatusColor(cpu.usage, { warning: 70, error: 85 }),
                }}
              ></div>
            </div>
          </div>

          <div className="metric-card">
            <h3>Memory Usage</h3>
            <div className="metric-value">
              <span className="value" style={{ color: getStatusColor(memory.usagePercent, { warning: 75, error: 90 }) }}>
                {memory.usagePercent.toFixed(1)}%
              </span>
              <div className="metric-details">
                <div>{formatBytes(memory.used)} / {formatBytes(memory.total)}</div>
                <div>Free: {formatBytes(memory.free)}</div>
              </div>
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{
                  width: `${memory.usagePercent}%`,
                  backgroundColor: getStatusColor(memory.usagePercent, { warning: 75, error: 90 }),
                }}
              ></div>
            </div>
          </div>

          <div className="metric-card">
            <h3>Disk Usage</h3>
            <div className="metric-value">
              <span className="value" style={{ color: getStatusColor(disk.usagePercent, { warning: 80, error: 95 }) }}>
                {disk.usagePercent.toFixed(1)}%
              </span>
              <div className="metric-details">
                <div>{formatBytes(disk.used)} / {formatBytes(disk.total)}</div>
                <div>Free: {formatBytes(disk.free)}</div>
              </div>
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{
                  width: `${disk.usagePercent}%`,
                  backgroundColor: getStatusColor(disk.usagePercent, { warning: 80, error: 95 }),
                }}
              ></div>
            </div>
          </div>

          <div className="metric-card">
            <h3>Network Status</h3>
            <div className="metric-value">
              <span className={`status-badge ${network.isConnected ? 'connected' : 'disconnected'}`}>
                {network.isConnected ? 'Connected' : 'Disconnected'}
              </span>
              <div className="metric-details">
                <div>Latency: {network.latency.toFixed(0)}ms</div>
                <div>API: {api.isHealthy ? '✅ Healthy' : '❌ Unhealthy'}</div>
              </div>
            </div>
          </div>

          {/* Evaluation Status */}
          <div className="metric-card evaluation-card">
            <h3>Evaluation Progress</h3>
            <div className="evaluation-status">
              <div className="status-header">
                <span className={`status-badge ${evaluation.isRunning ? 'running' : 'stopped'}`}>
                  {evaluation.isRunning ? 'Running' : 'Stopped'}
                </span>
                <span className="phase">{evaluation.phase}</span>
              </div>
              <div className="progress-section">
                <div className="progress-text">
                  {evaluation.problemsCompleted} / {evaluation.problemsTotal} problems
                                    ({evaluation.progress.toFixed(1)}%)
                </div>
                <div className="progress-bar large">
                  <div
                    className="progress-fill"
                    style={{ width: `${evaluation.progress}%` }}
                  ></div>
                </div>
              </div>
              <div className="evaluation-metrics">
                <div className="metric">
                  <span className="label">Success Rate:</span>
                  <span className="value">{evaluation.successRate.toFixed(1)}%</span>
                </div>
                <div className="metric">
                  <span className="label">Error Rate:</span>
                  <span className="value">{evaluation.errorRate.toFixed(1)}%</span>
                </div>
                <div className="metric">
                  <span className="label">Avg Time:</span>
                  <span className="value">{(evaluation.averageTimePerProblem / 1000).toFixed(1)}s</span>
                </div>
                <div className="metric">
                  <span className="label">ETA:</span>
                  <span className="value">{formatDuration(evaluation.estimatedTimeRemaining * 3600)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* API Status */}
          <div className="metric-card api-card">
            <h3>API Status</h3>
            <div className="api-status">
              <div className="api-health">
                <span className={`status-badge ${api.isHealthy ? 'healthy' : 'unhealthy'}`}>
                  {api.isHealthy ? 'Healthy' : 'Unhealthy'}
                </span>
                <span className="rate-limit">{api.rateLimitStatus}</span>
              </div>
              <div className="api-metrics">
                <div className="metric">
                  <span className="label">Response Time:</span>
                  <span className="value">{api.responseTime.toFixed(0)}ms</span>
                </div>
                <div className="metric">
                  <span className="label">Success Rate:</span>
                  <span className="value">{api.successRate.toFixed(1)}%</span>
                </div>
                <div className="metric">
                  <span className="label">Tokens Used:</span>
                  <span className="value">{api.tokensUsed.toLocaleString()}</span>
                </div>
                <div className="metric">
                  <span className="label">Requests/Hour:</span>
                  <span className="value">{api.requestsThisHour}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

  const renderPerformanceCharts = () => {
    const filteredHistory = getFilteredHistory();

    if (!filteredHistory.length) {
      return <div className="loading">No performance data available</div>;
    }

    const chartData = filteredHistory.map(h => ({
      time: new Date(h.timestamp).toLocaleTimeString(),
      cpu: h.cpu.usage,
      memory: h.memory.usagePercent,
      disk: h.disk.usagePercent,
      responseTime: h.api.responseTime,
      successRate: h.evaluation.successRate,
      errorRate: h.evaluation.errorRate,
      throughput: h.evaluation.problemsCompleted > 0 ?
        // eslint-disable-next-line max-len
        (h.evaluation.problemsCompleted / (Date.now() - new Date(filteredHistory[0].timestamp).getTime()) * 3600000) : 0,
    }));

    return (
      <div className="performance-charts">
        <div className="chart-controls">
          <div className="time-range-selector">
            {['15m', '1h', '6h', '24h'].map(range => (
              <button
                key={range}
                className={`range-button ${selectedTimeRange === range ? 'active' : ''}`}
                onClick={() => setSelectedTimeRange(range)}
              >
                {range}
              </button>
            ))}
          </div>
        </div>

        <div className="charts-grid">
          <div className="chart-container">
            <h3>System Resources</h3>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis domain={[0, 100]} />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="cpu" stroke="#ef4444" name="CPU %" />
                <Line type="monotone" dataKey="memory" stroke="#f59e0b" name="Memory %" />
                <Line type="monotone" dataKey="disk" stroke="#10b981" name="Disk %" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-container">
            <h3>Evaluation Performance</h3>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="successRate" stroke="#10b981" name="Success Rate %" />
                <Line type="monotone" dataKey="errorRate" stroke="#ef4444" name="Error Rate %" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-container">
            <h3>API Performance</h3>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Area type="monotone" dataKey="responseTime" stroke="#8884d8" fill="#8884d8" name="Response Time (ms)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-container">
            <h3>Throughput</h3>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={chartData.slice(-20)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Bar dataKey="throughput" fill="#06b6d4" name="Problems/Hour" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    );
  };

  const renderAlerts = () => (
    <div className="alerts-panel">
      <div className="alerts-header">
        <h3>Recent Alerts</h3>
        <div className="alert-summary">
          <span className="alert-count error">{alerts.filter(a => a.severity === 'error').length} Errors</span>
          <span className="alert-count warning">{alerts.filter(a => a.severity === 'warning').length} Warnings</span>
          <span className="alert-count info">{alerts.filter(a => a.severity === 'info').length} Info</span>
        </div>
      </div>
      <div className="alerts-list">
        {alerts.length === 0 ? (
          <div className="no-alerts">No recent alerts</div>
        ) : (
          alerts.slice(0, 50).map((alert, index) => (
            <div key={index} className={`alert-item ${alert.severity}`}>
              <div className="alert-header">
                <span className="alert-type">{alert.type}</span>
                <span className="alert-time">
                  {new Date(alert.timestamp).toLocaleString()}
                </span>
              </div>
              <div className="alert-message">{alert.message}</div>
            </div>
          ))
        )}
      </div>
    </div>
  );

  const renderSystemControls = () => (
    <div className="system-controls">
      <div className="control-section">
        <h3>Evaluation Controls</h3>
        <div className="control-buttons">
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn start" onClick={() => console.log('Start evaluation')}>
                        ▶️ Start Evaluation
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn pause" onClick={() => console.log('Pause evaluation')}>
                        ⏸️ Pause Evaluation
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn stop" onClick={() => console.log('Stop evaluation')}>
                        ⏹️ Stop Evaluation
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn checkpoint" onClick={() => console.log('Save checkpoint')}>
                        💾 Save Checkpoint
          </button>
        </div>
      </div>

      <div className="control-section">
        <h3>System Controls</h3>
        <div className="control-buttons">
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn cleanup" onClick={() => console.log('Cleanup system')}>
                        🧹 Cleanup System
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn restart" onClick={() => console.log('Restart monitoring')}>
                        🔄 Restart Monitoring
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn export" onClick={() => console.log('Export data')}>
                        📊 Export Data
          </button>
          {/* eslint-disable-next-line no-console */}
          <button className="control-btn settings" onClick={() => console.log('Open settings')}>
                        ⚙️ Settings
          </button>
        </div>
      </div>
    </div>
  );

  return (
    <div className={`monitoring-dashboard theme-${theme}`}>
      <header className="dashboard-header">
        <div className="header-left">
          <h1>🎯 Golem Quality Testing Dashboard</h1>
          <div className="header-subtitle">
                        Real-time monitoring for 14-hour benchmark evaluations
          </div>
        </div>
        <div className="header-right">
          {renderConnectionStatus()}
        </div>
      </header>

      <nav className="dashboard-nav">
        <div className="nav-tabs">
          {[
            { id: 'overview', label: '📊 Overview', icon: '📊' },
            { id: 'performance', label: '📈 Performance', icon: '📈' },
            { id: 'alerts', label: '🚨 Alerts', icon: '🚨' },
            { id: 'controls', label: '🎛️ Controls', icon: '🎛️' },
          ].map(tab => (
            <button
              key={tab.id}
              className={`nav-tab ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              <span className="tab-icon">{tab.icon}</span>
              <span className="tab-label">{tab.label}</span>
            </button>
          ))}
        </div>
      </nav>

      <main className="dashboard-content">
        {activeTab === 'overview' && renderSystemOverview()}
        {activeTab === 'performance' && renderPerformanceCharts()}
        {activeTab === 'alerts' && renderAlerts()}
        {activeTab === 'controls' && renderSystemControls()}
      </main>

      <footer className="dashboard-footer">
        <div className="footer-info">
          <span>Golem Quality Testing System v1.0</span>
          <span>•</span>
          <span>Windows 11 Deployment</span>
          <span>•</span>
          <span>Last updated: {currentHealth ? new Date(currentHealth.timestamp).toLocaleString() : 'Never'}</span>
        </div>
      </footer>
    </div>
  );
};

export default MonitoringDashboard;

