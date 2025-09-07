/// <reference types="node" />

/**
 * Progress Monitoring and Automated Recovery System for 14-Hour Golem Evaluations
 *
 * Provides real-time monitoring, automated recovery, health checks, and comprehensive
 * dashboards for long-running evaluation processes on Windows 11.
 */

import { EventEmitter } from 'events';
import * as fs from 'fs/promises';
import * as path from 'path';
import * as os from 'os';
import { performance } from 'perf_hooks';

export interface SystemHealth {
    timestamp: string;

    // System Resources
    cpu: {
        usage: number;
        loadAverage: number[];
        cores: number;
    };

    memory: {
        total: number;
        free: number;
        used: number;
        usagePercent: number;
        heapUsed: number;
        heapTotal: number;
    };

    disk: {
        total: number;
        free: number;
        used: number;
        usagePercent: number;
    };

    network: {
        isConnected: boolean;
        latency: number;
        lastSuccessfulRequest: string;
    };

    // Process Health
    process: {
        uptime: number;
        pid: number;
        memoryUsage: {
            rss: number;
            heapTotal: number;
            heapUsed: number;
            external: number;
            arrayBuffers: number;
        };
        cpuUsage: {
            user: number;
            system: number;
        };
    };

    // Evaluation Health
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

    // API Health
    api: {
        isHealthy: boolean;
        responseTime: number;
        successRate: number;
        rateLimitStatus: string;
        tokensUsed: number;
        requestsThisHour: number;
    };
}

export interface PerformanceMetrics {
    timestamp: string;

    // Throughput Metrics
    throughput: {
        problemsPerHour: number;
        tokensPerHour: number;
        requestsPerHour: number;
        averageResponseTime: number;
    };

    // Quality Metrics
    quality: {
        successRate: number;
        passRate: number;
        errorRate: number;
        retryRate: number;
    };

    // Efficiency Metrics
    efficiency: {
        cpuEfficiency: number;
        memoryEfficiency: number;
        networkEfficiency: number;
        costEfficiency: number;
    };

    // Trend Analysis
    trends: {
        throughputTrend: 'increasing' | 'decreasing' | 'stable';
        qualityTrend: 'improving' | 'degrading' | 'stable';
        resourceTrend: 'optimizing' | 'degrading' | 'stable';
    };
}

export interface AlertConfig {
    enabled: boolean;

    // Resource Alerts
    maxCpuUsage: number;
    maxMemoryUsage: number;
    minDiskSpace: number;

    // eslint-disable-next-line max-len
    // Performance Alerts
    minThroughput: number;
    maxErrorRate: number;
    maxResponseTime: number;

    // API Alerts
    maxRateLimitHits: number;
    minSuccessRate: number;

    // System Alerts
    maxDowntime: number;
    networkTimeout: number;

    // Notification Settings
    notifications: {
        console: boolean;
        file: boolean;
        email?: {
            enabled: boolean;
            recipients: string[];
            smtp: {
                host: string;
                port: number;
                secure: boolean;
                auth: {
                    user: string;
                    pass: string;
                };
            };
        };
        webhook?: {
            enabled: boolean;
            url: string;
            headers?: Record<string, string>;
        };
    };
}

export interface RecoveryAction {
    id: string;
    name: string;
    description: string;
    trigger: string;
    action: () => Promise<boolean>;
    cooldown: number;
    maxRetries: number;
    priority: number;
}

export class ProgressMonitoringSystem extends EventEmitter {
  private config: AlertConfig;
  private isMonitoring: boolean = false;
  private monitoringInterval: ReturnType<typeof setInterval> | null = null;
  private healthHistory: SystemHealth[] = [];
  private performanceHistory: PerformanceMetrics[] = [];
  private alertHistory: Array<{ timestamp: string; type: string; message: string; severity: string }> = [];

  // Recovery system
  private recoveryActions: Map<string, RecoveryAction> = new Map();
  private lastRecoveryAttempt: Map<string, number> = new Map();
  private recoveryAttempts: Map<string, number> = new Map();

  // Monitoring state
  private lastCpuUsage: { user: number; system: number } | null = null;
  private startTime: number = Date.now();
  private lastNetworkCheck: number = 0;
  private networkLatency: number = 0;

  // File paths
  private logPath: string;
  private healthLogPath: string;
    // eslint-disable-next-line max-len
  private performanceLogPath: string;
  private alertLogPath: string;

  constructor(config: AlertConfig, outputDirectory: string) {
    super();
    this.config = config;

    // Setup file paths
    this.logPath = path.join(outputDirectory, 'monitoring.log');
    this.healthLogPath = path.join(outputDirectory, 'health_history.json');
    this.performanceLogPath = path.join(outputDirectory, 'performance_history.json');
    this.alertLogPath = path.join(outputDirectory, 'alert_history.json');

    // Setup default recovery actions
    this.setupDefaultRecoveryActions();

    // Setup graceful shutdown
    this.setupGracefulShutdown();

    this.log('info', 'Progress Monitoring System initialized');
  }

  private setupDefaultRecoveryActions(): void {
    // Memory cleanup recovery
    this.addRecoveryAction({
      id: 'memory_cleanup',
      name: 'Memory Cleanup',
      description: 'Force garbage collection and clear caches',
      trigger: 'high_memory_usage',
      action: async () => {
        try {
          if (global.gc) {
            global.gc();
            this.log('info', 'Forced garbage collection completed');
          }

          // Clear internal caches
          this.healthHistory = this.healthHistory.slice(-100);
          this.performanceHistory = this.performanceHistory.slice(-100);
          this.alertHistory = this.alertHistory.slice(-1000);

          return true;
        } catch (error) {
          this.log('error', `Memory cleanup failed: ${error instanceof Error ? error.message : String(error)}`);
          return false;
        }
      },
      cooldown: 300000, // 5 minutes
      maxRetries: 3,
      priority: 1,
    });

    // Network recovery
    this.addRecoveryAction({
      id: 'network_recovery',
      name: 'Network Recovery',
      description: 'Test and recover network connectivity',
      trigger: 'network_failure',
      action: async () => {
        try {
          // Test network connectivity
          const isConnected = await this.testNetworkConnectivity();
          if (isConnected) {
            this.log('info', 'Network connectivity restored');
            return true;
          }

          // Wait and retry
          await this.sleep(5000);
          return await this.testNetworkConnectivity();
        } catch (error) {
          this.log('error', `Network recovery failed: ${error instanceof Error ? error.message : String(error)}`);
          return false;
        }
      },
      cooldown: 60000, // 1 minute
      maxRetries: 5,
      priority: 2,
    });

    // Disk cleanup recovery
    this.addRecoveryAction({
      id: 'disk_cleanup',
      name: 'Disk Cleanup',
      description: 'Clean temporary files and logs',
      trigger: 'low_disk_space',
      action: async () => {
        try {
          // Clean old log files
          const tempDir = os.tmpdir();
          const files = await fs.readdir(tempDir);

          let cleanedFiles = 0;
          for (const file of files) {
            if (file.startsWith('golem_') || file.startsWith('eval_')) {
              try {
                await fs.unlink(path.join(tempDir, file));
                cleanedFiles++;
              } catch {
                // Ignore errors for individual files
              }
            }
          }

          this.log('info', `Disk cleanup completed, removed ${cleanedFiles} temporary files`);
          return true;
        } catch (error) {
          this.log('error', `Disk cleanup failed: ${error instanceof Error ? error.message : String(error)}`);
          return false;
        }
      },
      cooldown: 600000, // 10 minutes
      maxRetries: 2,
      priority: 3,
    });

    // eslint-disable-next-line max-len
    // Process restart recovery (last resort)
    this.addRecoveryAction({
      id: 'process_restart',
      name: 'Process Restart',
    // eslint-disable-next-line max-len
      description: 'Restart the evaluation process',
      trigger: 'critical_failure',
      action: async () => {
        try {
          this.log('warn', 'Critical failure detected, initiating process restart...');

          // Save current state
          await this.saveMonitoringState();

          // Emit restart signal
          this.emit('requestRestart', {
            reason: 'critical_failure',
            timestamp: new Date().toISOString(),
          });

          return true;
        } catch (error) {
          this.log('error', `Process restart failed: ${error instanceof Error ? error.message : String(error)}`);
          return false;
        }
      },
      cooldown: 1800000, // 30 minutes
      maxRetries: 1,
      priority: 10,
    });
  }

  private setupGracefulShutdown(): void {
    const gracefulShutdown = async () => {
      this.log('info', 'Shutting down monitoring system...');
      await this.stopMonitoring();
      await this.saveMonitoringState();
    };

    process.on('SIGINT', gracefulShutdown);
    process.on('SIGTERM', gracefulShutdown);

    if (process.platform === 'win32') {
      process.on('SIGBREAK', gracefulShutdown);
    }
  }

  public async startMonitoring(intervalMs: number = 30000): Promise<void> {
    if (this.isMonitoring) {
      this.log('warn', 'Monitoring is already running');
      return;
    }

    this.isMonitoring = true;
    this.startTime = Date.now();

    this.log('info', `Starting monitoring with ${intervalMs}ms interval`);

    // Initial health check
    await this.performHealthCheck();

    // Start monitoring loop
    this.monitoringInterval = setInterval(async () => {
      try {
        await this.performHealthCheck();
        await this.analyzePerformance();
        await this.checkAlerts();
        await this.cleanupHistory();
      } catch (error) {
        this.log('error', `Monitoring cycle failed: ${error instanceof Error ? error.message : String(error)}`);
      }
    }, intervalMs);

    this.emit('monitoringStarted');
  }

  public async stopMonitoring(): Promise<void> {
    if (!this.isMonitoring) {
      return;
    }

    this.isMonitoring = false;

    if (this.monitoringInterval) {
      clearInterval(this.monitoringInterval);
      this.monitoringInterval = null;
    }

    this.log('info', 'Monitoring stopped');
    this.emit('monitoringStopped');
  }

  private async performHealthCheck(): Promise<SystemHealth> {
    const timestamp = new Date().toISOString();

    // System resources
    const cpuUsage = await this.getCpuUsage();
    const memoryInfo = this.getMemoryInfo();
    const diskInfo = await this.getDiskInfo();
    const networkInfo = await this.getNetworkInfo();

    // Process health
    const processInfo = this.getProcessInfo();

    // Evaluation health (will be updated by external systems)
    const evaluationInfo = this.getEvaluationInfo();

    // API health (will be updated by external systems)
    const apiInfo = this.getApiInfo();

    const health: SystemHealth = {
      timestamp,
      cpu: cpuUsage,
      memory: memoryInfo,
      disk: diskInfo,
      network: networkInfo,
      process: processInfo,
      evaluation: evaluationInfo,
      api: apiInfo,
    };

    // Store in history
    this.healthHistory.push(health);

    // Emit health update
    this.emit('healthUpdate', health);

    return health;
  }

  private async getCpuUsage(): Promise<SystemHealth['cpu']> {
    const cpus = os.cpus();
    const loadAverage = os.loadavg();

    // Calculate CPU usage
    let usage = 0;
    if (this.lastCpuUsage) {
      const currentUsage = process.cpuUsage(this.lastCpuUsage);
      const totalUsage = currentUsage.user + currentUsage.system;
      usage = (totalUsage / 1000000) / cpus.length; // Convert to percentage
    }

    this.lastCpuUsage = process.cpuUsage();

    return {
      usage: Math.min(100, Math.max(0, usage)),
      loadAverage,
      cores: cpus.length,
    };
  }

  private getMemoryInfo(): SystemHealth['memory'] {
    const totalMem = os.totalmem();
    const freeMem = os.freemem();
    const usedMem = totalMem - freeMem;
    const processMemory = process.memoryUsage();

    return {
      total: totalMem,
      free: freeMem,
      used: usedMem,
      usagePercent: (usedMem / totalMem) * 100,
      heapUsed: processMemory.heapUsed,
      heapTotal: processMemory.heapTotal,
    };
  }

  private async getDiskInfo(): Promise<SystemHealth['disk']> {
    // This is a simplified implementation
    // In a real Windows environment, you'd use Windows-specific APIs

    // Placeholder values - would need Windows-specific implementation
    return {
      total: 1000000000000, // 1TB placeholder
      free: 500000000000,   // 500GB placeholder
      used: 500000000000,   // 500GB placeholder
      usagePercent: 50,
    };
  }

  private async getNetworkInfo(): Promise<SystemHealth['network']> {
    const now = Date.now();

    // Only check network every 60 seconds to avoid overhead
    if (now - this.lastNetworkCheck < 60000) {
      return {
        isConnected: this.networkLatency > 0,
        latency: this.networkLatency,
        lastSuccessfulRequest: new Date(this.lastNetworkCheck).toISOString(),
      };
    }

    const isConnected = await this.testNetworkConnectivity();
    this.lastNetworkCheck = now;

    return {
      isConnected,
      latency: this.networkLatency,
      lastSuccessfulRequest: isConnected ? new Date().toISOString() : 'Never',
    };
  }

  private async testNetworkConnectivity(): Promise<boolean> {
    try {
      const startTime = performance.now();

      // Create AbortController for timeout
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 5000);

      // Test with a simple HTTP request
      const response = await fetch('https://api.mistral.ai/v1/models', {
        method: 'HEAD',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);
      const endTime = performance.now();
      this.networkLatency = endTime - startTime;

      return response.ok;
    } catch (error) {
      this.networkLatency = 0;
      return false;
    }
  }

  private getProcessInfo(): SystemHealth['process'] {
    return {
      uptime: process.uptime(),
      pid: process.pid,
      memoryUsage: process.memoryUsage(),
      cpuUsage: process.cpuUsage(),
    };
  }

  private getEvaluationInfo(): SystemHealth['evaluation'] {
    // This will be updated by the evaluation system
    return {
      isRunning: false,
      phase: 'idle',
      progress: 0,
      problemsCompleted: 0,
      problemsTotal: 0,
      successRate: 0,
      errorRate: 0,
      averageTimePerProblem: 0,
      estimatedTimeRemaining: 0,
    };
  }

  private getApiInfo(): SystemHealth['api'] {
    // This will be updated by the API client
    return {
      isHealthy: false,
      responseTime: 0,
      successRate: 0,
      rateLimitStatus: 'unknown',
      tokensUsed: 0,
      requestsThisHour: 0,
    };
  }

  private async analyzePerformance(): Promise<PerformanceMetrics> {
    const timestamp = new Date().toISOString();
    const recentHealth = this.healthHistory.slice(-10); // Last 10 readings

    if (recentHealth.length === 0) {
      return null;
    }

    // Calculate throughput metrics
    const throughput = this.calculateThroughputMetrics(recentHealth);

    // Calculate quality metrics
    const quality = this.calculateQualityMetrics(recentHealth);
    // eslint-disable-next-line max-len

    // Calculate efficiency metrics
    const efficiency = this.calculateEfficiencyMetrics(recentHealth);

    // Analyze trends
    const trends = this.analyzeTrends();

    const metrics: PerformanceMetrics = {
      timestamp,
      throughput,
      quality,
      efficiency,
      trends,
    };

    this.performanceHistory.push(metrics);
    this.emit('performanceUpdate', metrics);

    return metrics;
  }

  private calculateThroughputMetrics(healthData: SystemHealth[]): PerformanceMetrics['throughput'] {
    // Calculate averages from recent health data
    const avgResponseTime = healthData.reduce((sum, h) => sum + h.api.responseTime, 0) / healthData.length;

    return {
      problemsPerHour: 0, // Will be updated by evaluation system
    // eslint-disable-next-line max-len
      tokensPerHour: 0,   // Will be updated by API client
      requestsPerHour: 0, // Will be updated by API client
      averageResponseTime: avgResponseTime,
    };
  }

  private calculateQualityMetrics(healthData: SystemHealth[]): PerformanceMetrics['quality'] {
    const avgSuccessRate = healthData.reduce((sum, h) => sum + h.evaluation.successRate, 0) / healthData.length;
    const avgErrorRate = healthData.reduce((sum, h) => sum + h.evaluation.errorRate, 0) / healthData.length;

    return {
      successRate: avgSuccessRate,
      passRate: 0,    // Will be calculated from evaluation results
      errorRate: avgErrorRate,
      retryRate: 0,    // Will be calculated from retry statistics
    };
  }

  private calculateEfficiencyMetrics(healthData: SystemHealth[]): PerformanceMetrics['efficiency'] {
    const avgCpuUsage = healthData.reduce((sum, h) => sum + h.cpu.usage, 0) / healthData.length;
    const avgMemoryUsage = healthData.reduce((sum, h) => sum + h.memory.usagePercent, 0) / healthData.length;

    return {
      cpuEfficiency: Math.max(0, 100 - avgCpuUsage),
      memoryEfficiency: Math.max(0, 100 - avgMemoryUsage),
      networkEfficiency: 100, // Placeholder
      costEfficiency: 100,     // Free tier = 100% efficient
    };
  }
    // eslint-disable-next-line max-len

  private analyzeTrends(): PerformanceMetrics['trends'] {
    const recentMetrics = this.performanceHistory.slice(-5);

    if (recentMetrics.length < 2) {
      return {
        throughputTrend: 'stable',
        qualityTrend: 'stable',
        resourceTrend: 'stable',
      };
    }

    // Simple trend analysis
    const first = recentMetrics[0];
    const last = recentMetrics[recentMetrics.length - 1];

    const baseThroughputTrend = this.getTrend(first.throughput.problemsPerHour, last.throughput.problemsPerHour);
    const baseQualityTrend = this.getTrend(first.quality.successRate, last.quality.successRate);
    const baseResourceTrend = this.getTrend(first.efficiency.cpuEfficiency, last.efficiency.cpuEfficiency);

    // Map to correct types
    const throughputTrend = baseThroughputTrend;
    const qualityTrend = baseQualityTrend === 'increasing' ? 'improving' :
                         baseQualityTrend === 'decreasing' ? 'degrading' : 'stable';
    const resourceTrend = baseResourceTrend === 'increasing' ? 'optimizing' :
                          baseResourceTrend === 'decreasing' ? 'degrading' : 'stable';

    return {
      throughputTrend,
      qualityTrend,
      resourceTrend,
    };
  }

  private getTrend(oldValue: number, newValue: number): 'increasing' | 'decreasing' | 'stable' {
    const change = ((newValue - oldValue) / oldValue) * 100;

    if (change > 5) {
      return 'increasing';
    }
    if (change < -5) {
      return 'decreasing';
    }
    return 'stable';
  }

  private async checkAlerts(): Promise<void> {
    if (!this.config.enabled) {
      return;
    }

    const currentHealth = this.healthHistory[this.healthHistory.length - 1];
    // eslint-disable-next-line max-len
    if (!currentHealth) {
      return;
    }

    // Check resource alerts
    await this.checkResourceAlerts(currentHealth);

    // Check performance alerts
    await this.checkPerformanceAlerts(currentHealth);

    // Check API alerts
    await this.checkApiAlerts(currentHealth);

    // Check system alerts
    await this.checkSystemAlerts(currentHealth);
  }

  private async checkResourceAlerts(health: SystemHealth): Promise<void> {
    // CPU usage alert
    if (health.cpu.usage > this.config.maxCpuUsage) {
      await this.triggerAlert('high_cpu_usage', `CPU usage is ${health.cpu.usage.toFixed(1)}%`, 'warning');
      await this.triggerRecovery('memory_cleanup');
    }

    // Memory usage alert
    if (health.memory.usagePercent > this.config.maxMemoryUsage) {
      await this.triggerAlert('high_memory_usage', `Memory usage is ${health.memory.usagePercent.toFixed(1)}%`, 'warning');
      await this.triggerRecovery('memory_cleanup');
    }

    // Disk space alert
    if (health.disk.usagePercent > (100 - this.config.minDiskSpace)) {
      await this.triggerAlert('low_disk_space', `Disk usage is ${health.disk.usagePercent.toFixed(1)}%`, 'error');
      await this.triggerRecovery('disk_cleanup');
    }
  }

  private async checkPerformanceAlerts(health: SystemHealth): Promise<void> {
    // Low throughput alert
    const recentMetrics = this.performanceHistory.slice(-3);
    if (recentMetrics.length > 0) {
      // eslint-disable-next-line max-len
      const avgThroughput = recentMetrics.reduce((sum, m) => sum + m.throughput.problemsPerHour, 0) / recentMetrics.length;

      if (avgThroughput < this.config.minThroughput && avgThroughput > 0) {
        await this.triggerAlert('low_throughput', `Throughput is ${avgThroughput.toFixed(1)} problems/hour`, 'warning');
      }
    }

    // High error rate alert
    if (health.evaluation.errorRate > this.config.maxErrorRate) {
      await this.triggerAlert('high_error_rate', `Error rate is ${health.evaluation.errorRate.toFixed(1)}%`, 'error');
    }

    // High response time alert
    if (health.api.responseTime > this.config.maxResponseTime) {
      await this.triggerAlert('high_response_time', `API response time is ${health.api.responseTime.toFixed(0)}ms`, 'warning');
    }
  }

  private async checkApiAlerts(health: SystemHealth): Promise<void> {
    // API health alert
    if (!health.api.isHealthy) {
      await this.triggerAlert('api_unhealthy', 'API is not responding properly', 'error');
      await this.triggerRecovery('network_recovery');
    }

    // Low success rate alert
    if (health.api.successRate < this.config.minSuccessRate && health.api.successRate > 0) {
      await this.triggerAlert('low_api_success_rate', `API success rate is ${health.api.successRate.toFixed(1)}%`, 'warning');
    }
  }

  private async checkSystemAlerts(health: SystemHealth): Promise<void> {
    // Network connectivity alert
    if (!health.network.isConnected) {
      await this.triggerAlert('network_disconnected', 'Network connectivity lost', 'error');
      await this.triggerRecovery('network_recovery');
    }

    // Process uptime alert
    if (health.process.uptime > this.config.maxDowntime) {
      await this.triggerAlert('long_running_process', `Process has been running for ${(health.process.uptime / 3600).toFixed(1)} hours`, 'info');
    }
  }

  private async triggerAlert(type: string, message: string, severity: string): Promise<void> {
    const alert = {
      timestamp: new Date().toISOString(),
      type,
      message,
      severity,
    };

    this.alertHistory.push(alert);

    this.log(severity, `ALERT [${type}]: ${message}`);
    this.emit('alert', alert);

    // Send notifications
    if (this.config.notifications.console) {
    // eslint-disable-next-line no-console
      console.log(`🚨 [${severity.toUpperCase()}] ${message}`);
    }

    if (this.config.notifications.file) {
      await this.saveAlertToFile(alert);
    }

    if (this.config.notifications.email?.enabled) {
      await this.sendEmailNotification(alert);
    }

    if (this.config.notifications.webhook?.enabled) {
      await this.sendWebhookNotification(alert);
    }
  }

  private async triggerRecovery(actionId: string): Promise<void> {
    const action = this.recoveryActions.get(actionId);
    if (!action) {
      return;
    }

    const now = Date.now();
    const lastAttempt = this.lastRecoveryAttempt.get(actionId) || 0;
    const attempts = this.recoveryAttempts.get(actionId) || 0;

    // Check cooldown
    if (now - lastAttempt < action.cooldown) {
      this.log('debug', `Recovery action ${actionId} is in cooldown`);
      return;
    }

    // Check max retries
    if (attempts >= action.maxRetries) {
      this.log('warn', `Recovery action ${actionId} has exceeded max retries`);
      return;
    }

    this.log('info', `Triggering recovery action: ${action.name}`);

    try {
      this.lastRecoveryAttempt.set(actionId, now);
      this.recoveryAttempts.set(actionId, attempts + 1);

      const success = await action.action();

      if (success) {
        this.log('info', `Recovery action ${action.name} completed successfully`);
        this.recoveryAttempts.set(actionId, 0); // Reset on success
        this.emit('recoverySuccess', { actionId, action: action.name });
      } else {
        this.log('error', `Recovery action ${action.name} failed`);
        this.emit('recoveryFailure', { actionId, action: action.name });
      }

    } catch (error) {
      this.log('error', `Recovery action ${action.name} threw error: ${error instanceof Error ? error.message : String(error)}`);
      this.emit('recoveryError', { actionId, action: action.name, error: error instanceof Error ? error.message : String(error) });
    }
  }

  private async cleanupHistory(): Promise<void> {
    // Keep last 1000 health records (about 8 hours at 30s intervals)
    if (this.healthHistory.length > 1000) {
      this.healthHistory = this.healthHistory.slice(-1000);
    }

    // Keep last 500 performance records
    if (this.performanceHistory.length > 500) {
      this.performanceHistory = this.performanceHistory.slice(-500);
    }

    // Keep last 5000 alerts
    if (this.alertHistory.length > 5000) {
      this.alertHistory = this.alertHistory.slice(-5000);
    }
  }

  private async saveMonitoringState(): Promise<void> {
    try {
      await fs.writeFile(this.healthLogPath, JSON.stringify(this.healthHistory, null, 2));
      await fs.writeFile(this.performanceLogPath, JSON.stringify(this.performanceHistory, null, 2));
      await fs.writeFile(this.alertLogPath, JSON.stringify(this.alertHistory, null, 2));
    } catch (error) {
      this.log('error', `Failed to save monitoring state: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private async saveAlertToFile(alert: any): Promise<void> {
    try {
      const logLine = `${alert.timestamp} [${alert.severity.toUpperCase()}] [${alert.type}] ${alert.message}\n`;
      await fs.appendFile(this.alertLogPath, logLine);
    } catch (error) {
      this.log('error', `Failed to save alert to file: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private async sendEmailNotification(alert: any): Promise<void> {
    const emailConfig = this.config.notifications.email;

    if (!emailConfig?.enabled || !emailConfig.recipients.length) {
      this.log('debug', 'Email notifications disabled or no recipients configured');
      return;
    }

    try {
      // Enhanced email notification implementation
      const emailContent = {
        to: emailConfig.recipients,
        subject: `Minotaur Alert: ${alert.type}`,
        html: this.generateEmailHTML(alert),
        text: this.generateEmailText(alert),
        metadata: {
          alertId: alert.id || `alert_${Date.now()}`,
          severity: alert.severity,
          timestamp: new Date().toISOString(),
        },
      };

      this.log('info', `Email notification prepared for ${emailConfig.recipients.length} recipients`);
      this.log('debug', `Email content prepared - Subject: ${emailContent.subject}`);

      // Enhanced notification tracking - store in a queue for actual sending
      await this.enqueueNotification('email', emailContent);

    } catch (error) {
      this.log('error', `Failed to send email notification: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private generateEmailHTML(alert: any): string {
    return `
      <html>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
    // eslint-disable-next-line max-len
          <h2 style="color: ${this.getSeverityColor(alert.severity)};">Minotaur System Alert</h2>
          // eslint-disable-next-line max-len
          <div style="background-color: #f5f5f5; padding: 15px; border-left: 4px solid ${this.getSeverityColor(alert.severity)};">
            <p><strong>Alert Type:</strong> ${alert.type}</p>
            <p><strong>Severity:</strong> ${alert.severity || 'Unknown'}</p>
            <p><strong>Component:</strong> ${alert.component || 'Unknown'}</p>
            <p><strong>Timestamp:</strong> ${new Date().toISOString()}</p>
            <p><strong>Message:</strong></p>
            <p style="background-color: white; padding: 10px; border-radius: 4px;">${alert.message || 'No message provided'}</p>
          </div>
          <p style="font-size: 12px; color: #666; margin-top: 20px;">
            This is an automated notification from Minotaur ProgressMonitoringSystem.
          </p>
        </body>
      </html>
    `;
  }

  private generateEmailText(alert: any): string {
    return `
MINOTAUR SYSTEM ALERT

Alert Type: ${alert.type}
Severity: ${alert.severity || 'Unknown'}
Component: ${alert.component || 'Unknown'}
Timestamp: ${new Date().toISOString()}

Message:
${alert.message || 'No message provided'}

---
This is an automated notification from Minotaur ProgressMonitoringSystem.
    `.trim();
  }

  private getSeverityColor(severity: string): string {
    switch ((severity || '').toLowerCase()) {
      case 'critical': return '#dc3545';
      case 'high': return '#fd7e14';
      case 'medium': return '#ffc107';
      case 'low': return '#28a745';
      default: return '#6c757d';
    }
  }

  private async enqueueNotification(type: 'email' | 'webhook', content: any): Promise<void> {
    try {
      // Store notification in a queue for processing
      const notification = {
        id: `${type}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        type,
        content,
        timestamp: new Date().toISOString(),
        status: 'pending',
      };

      // In a real implementation, this would be stored in a queue (Redis, database, etc.)
      this.log('info', `Notification queued: ${notification.id} (${type})`);

      // For now, simulate successful queuing
      this.log('debug', `Notification content: ${JSON.stringify(content, null, 2)}`);

    } catch (error) {
      this.log('error', `Failed to enqueue notification: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private async sendWebhookNotification(alert: any): Promise<void> {
    const webhookConfig = this.config.notifications.webhook;

    if (!webhookConfig?.enabled || !webhookConfig.url) {
      this.log('debug', 'Webhook notifications disabled or no URL configured');
      return;
    }

    try {
      const payload = {
        timestamp: new Date().toISOString(),
        alert: {
          type: alert.type,
          severity: alert.severity || 'Unknown',
          message: alert.message || 'No message provided',
          component: alert.component || 'Unknown',
        },
        source: 'Minotaur ProgressMonitoringSystem',
      };

      // Basic webhook implementation using fetch
      const response = await fetch(webhookConfig.url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...webhookConfig.headers,
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        this.log('info', `Webhook notification sent successfully to ${webhookConfig.url}`);
      } else {
        throw new Error(`Webhook request failed with status ${response.status}`);
      }

    } catch (error) {
      this.log('error', `Failed to send webhook notification: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private log(level: string, message: string): void {
    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] [MONITOR-${level.toUpperCase()}] ${message}`;

    // eslint-disable-next-line no-console
    console.log(logMessage);

    // eslint-disable-next-line max-len
    // Also write to log file
    fs.appendFile(this.logPath, logMessage + '\n').catch(() => {
      // Ignore log file errors
    });
  }

    // eslint-disable-next-line max-len
  // Public methods for external integration

  public addRecoveryAction(action: RecoveryAction): void {
    this.recoveryActions.set(action.id, action);
    this.log('info', `Added recovery action: ${action.name}`);
  }

  public updateEvaluationStatus(status: Partial<SystemHealth['evaluation']>): void {
    const currentHealth = this.healthHistory[this.healthHistory.length - 1];
    if (currentHealth) {
      Object.assign(currentHealth.evaluation, status);
    }
  }

  public updateApiStatus(status: Partial<SystemHealth['api']>): void {
    const currentHealth = this.healthHistory[this.healthHistory.length - 1];
    if (currentHealth) {
      Object.assign(currentHealth.api, status);
    }
  }

  public getCurrentHealth(): SystemHealth | null {
    return this.healthHistory[this.healthHistory.length - 1] || null;
  }

  public getHealthHistory(limit?: number): SystemHealth[] {
    return limit ? this.healthHistory.slice(-limit) : [...this.healthHistory];
  }

  public getPerformanceHistory(limit?: number): PerformanceMetrics[] {
    return limit ? this.performanceHistory.slice(-limit) : [...this.performanceHistory];
  }

  public getAlertHistory(limit?: number): any[] {
    return limit ? this.alertHistory.slice(-limit) : [...this.alertHistory];
  }

  public getStatistics() {
    const totalAlerts = this.alertHistory.length;
    const recentAlerts = this.alertHistory.filter(a =>
      Date.now() - new Date(a.timestamp).getTime() < 3600000, // Last hour
    ).length;

    const uptime = Date.now() - this.startTime;

    return {
      uptime,
      totalHealthChecks: this.healthHistory.length,
      totalAlerts,
      recentAlerts,
      recoveryActionsAvailable: this.recoveryActions.size,
      isMonitoring: this.isMonitoring,
    };
  }
}

// Factory function for easy setup
// eslint-disable-next-line max-len
export function createMonitoringSystem(outputDirectory: string, options?: Partial<AlertConfig>): ProgressMonitoringSystem {
  const defaultConfig: AlertConfig = {
    enabled: true,

    // Resource thresholds
    maxCpuUsage: 80,
    maxMemoryUsage: 85,
    minDiskSpace: 10, // 10% free space

    // Performance thresholds
    minThroughput: 50, // problems per hour
    maxErrorRate: 15,  // 15% error rate
    maxResponseTime: 30000, // 30 seconds

    // API thresholds
    maxRateLimitHits: 10,
    minSuccessRate: 80,

    // System thresholds
    maxDowntime: 86400, // 24 hours
    networkTimeout: 10000,

    // Notifications
    notifications: {
      console: true,
      file: true,
    },
  };

  return new ProgressMonitoringSystem({ ...defaultConfig, ...options }, outputDirectory);
}

