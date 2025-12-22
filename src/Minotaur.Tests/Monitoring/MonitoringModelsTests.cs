using Minotaur.Monitoring;
using Xunit;

namespace Minotaur.Tests.Monitoring;

public class MonitoringModelsTests
{
    #region EngineHealthReport Tests

    [Fact]
    public void EngineHealthReport_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var engineId = "engine-001";
        var resourceUtil = new ResourceUtilization
        {
            CPUUsagePercent = 45.5,
            MemoryUsedBytes = 1024L * 1024 * 512, // 512 MB
            MemoryTotalBytes = 1024L * 1024 * 1024 * 2 // 2 GB
        };

        // Act
        var report = new EngineHealthReport
        {
            EngineId = engineId,
            IsHealthy = true,
            ResourceUtilization = resourceUtil,
            AvailableEngines = 3
        };

        // Assert
        Assert.Equal(engineId, report.EngineId);
        Assert.True(report.IsHealthy);
        Assert.Equal(resourceUtil, report.ResourceUtilization);
        Assert.Equal(3, report.AvailableEngines);
        Assert.True(report.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void EngineHealthReport_CanIncludeAdditionalMetrics()
    {
        // Arrange
        var additionalMetrics = new Dictionary<string, object>
        {
            { "QueuedTasks", 15 },
            { "ActiveConnections", 8 },
            { "ErrorRate", 0.02 }
        };

        // Act
        var report = new EngineHealthReport
        {
            EngineId = "engine-002",
            IsHealthy = false,
            ResourceUtilization = new ResourceUtilization
            {
                CPUUsagePercent = 95.0,
                MemoryUsedBytes = 1024L * 1024 * 1800,
                MemoryTotalBytes = 1024L * 1024 * 2048
            },
            AdditionalMetrics = additionalMetrics
        };

        // Assert
        Assert.NotNull(report.AdditionalMetrics);
        Assert.Equal(3, report.AdditionalMetrics.Count);
        Assert.Equal(15, report.AdditionalMetrics["QueuedTasks"]);
    }

    [Fact]
    public void EngineHealthReport_UnhealthyWhenResourcesExhausted()
    {
        // Arrange & Act
        var report = new EngineHealthReport
        {
            EngineId = "engine-003",
            IsHealthy = false,
            ResourceUtilization = new ResourceUtilization
            {
                CPUUsagePercent = 98.5,
                MemoryUsedBytes = 1024L * 1024 * 1900,
                MemoryTotalBytes = 1024L * 1024 * 2048
            },
            AvailableEngines = 0
        };

        // Assert
        Assert.False(report.IsHealthy);
        Assert.Equal(0, report.AvailableEngines);
        Assert.True(report.ResourceUtilization.CPUUsagePercent > 95);
    }

    #endregion

    #region PerformanceMetrics Tests

    [Fact]
    public void PerformanceMetrics_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var metrics = new PerformanceMetrics
        {
            AverageResponseTime = 125.5,
            ThroughputPerSecond = 850.0,
            AverageProcessingTime = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        Assert.Equal(125.5, metrics.AverageResponseTime);
        Assert.Equal(850.0, metrics.ThroughputPerSecond);
        Assert.Equal(TimeSpan.FromMilliseconds(100), metrics.AverageProcessingTime);
    }

    [Fact]
    public void PerformanceMetrics_CalculatesThroughputPerMinute()
    {
        // Arrange
        var throughputPerSecond = 100.0;

        // Act
        var metrics = new PerformanceMetrics
        {
            AverageResponseTime = 50.0,
            ThroughputPerSecond = throughputPerSecond,
            AverageProcessingTime = TimeSpan.FromMilliseconds(25)
        };

        // Assert
        Assert.Equal(6000.0, metrics.ThroughputPerMinute);
        Assert.Equal(throughputPerSecond * 60, metrics.ThroughputPerMinute);
    }

    [Fact]
    public void PerformanceMetrics_CanIncludeCustomMetrics()
    {
        // Arrange
        var customMetrics = new Dictionary<string, double>
        {
            { "P50_Latency", 45.2 },
            { "P95_Latency", 180.5 },
            { "P99_Latency", 350.8 },
            { "ErrorRate", 0.015 }
        };

        // Act
        var metrics = new PerformanceMetrics
        {
            AverageResponseTime = 75.0,
            ThroughputPerSecond = 500.0,
            AverageProcessingTime = TimeSpan.FromMilliseconds(50),
            CustomMetrics = customMetrics
        };

        // Assert
        Assert.NotNull(metrics.CustomMetrics);
        Assert.Equal(4, metrics.CustomMetrics.Count);
        Assert.Equal(45.2, metrics.CustomMetrics["P50_Latency"]);
        Assert.Equal(350.8, metrics.CustomMetrics["P99_Latency"]);
    }

    [Fact]
    public void PerformanceMetrics_HandlesZeroThroughput()
    {
        // Arrange & Act
        var metrics = new PerformanceMetrics
        {
            AverageResponseTime = 0.0,
            ThroughputPerSecond = 0.0,
            AverageProcessingTime = TimeSpan.Zero
        };

        // Assert
        Assert.Equal(0.0, metrics.ThroughputPerSecond);
        Assert.Equal(0.0, metrics.ThroughputPerMinute);
    }

    [Fact]
    public void PerformanceMetrics_HandlesHighThroughput()
    {
        // Arrange & Act
        var metrics = new PerformanceMetrics
        {
            AverageResponseTime = 5.5,
            ThroughputPerSecond = 10000.0,
            AverageProcessingTime = TimeSpan.FromMilliseconds(2)
        };

        // Assert
        Assert.Equal(10000.0, metrics.ThroughputPerSecond);
        Assert.Equal(600000.0, metrics.ThroughputPerMinute);
    }

    #endregion

    #region ResourceUtilization Tests

    [Fact]
    public void ResourceUtilization_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 65.5,
            MemoryUsedBytes = 1024L * 1024 * 800,
            MemoryTotalBytes = 1024L * 1024 * 2048
        };

        // Assert
        Assert.Equal(65.5, utilization.CPUUsagePercent);
        Assert.Equal(1024L * 1024 * 800, utilization.MemoryUsedBytes);
        Assert.Equal(1024L * 1024 * 2048, utilization.MemoryTotalBytes);
        Assert.True(utilization.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void ResourceUtilization_CanIncludeGPUUsage()
    {
        // Arrange & Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 45.0,
            MemoryUsedBytes = 1024L * 1024 * 512,
            MemoryTotalBytes = 1024L * 1024 * 1024,
            GPUUsagePercent = 78.5
        };

        // Assert
        Assert.NotNull(utilization.GPUUsagePercent);
        Assert.Equal(78.5, utilization.GPUUsagePercent.Value);
    }

    [Fact]
    public void ResourceUtilization_GPUUsageCanBeNull()
    {
        // Arrange & Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 45.0,
            MemoryUsedBytes = 1024L * 1024 * 512,
            MemoryTotalBytes = 1024L * 1024 * 1024
        };

        // Assert
        Assert.Null(utilization.GPUUsagePercent);
    }

    [Fact]
    public void ResourceUtilization_CanIncludeCustomMetrics()
    {
        // Arrange
        var customMetrics = new Dictionary<string, double>
        {
            { "DiskIOPS", 1500.0 },
            { "NetworkBandwidthMbps", 950.0 },
            { "ThreadPoolUtilization", 0.75 }
        };

        // Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 55.0,
            MemoryUsedBytes = 1024L * 1024 * 600,
            MemoryTotalBytes = 1024L * 1024 * 1024,
            CustomMetrics = customMetrics
        };

        // Assert
        Assert.NotNull(utilization.CustomMetrics);
        Assert.Equal(3, utilization.CustomMetrics.Count);
        Assert.Equal(1500.0, utilization.CustomMetrics["DiskIOPS"]);
    }

    [Fact]
    public void ResourceUtilization_HandlesHighMemoryUsage()
    {
        // Arrange
        var memoryTotal = 1024L * 1024 * 1024 * 8; // 8 GB
        var memoryUsed = 1024L * 1024 * 1024 * 7; // 7 GB (87.5% usage)

        // Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 90.0,
            MemoryUsedBytes = memoryUsed,
            MemoryTotalBytes = memoryTotal
        };

        // Assert
        Assert.True(utilization.MemoryUsedBytes > utilization.MemoryTotalBytes * 0.8);
        Assert.Equal(memoryUsed, utilization.MemoryUsedBytes);
    }

    [Fact]
    public void ResourceUtilization_HandlesZeroUsage()
    {
        // Arrange & Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 0.0,
            MemoryUsedBytes = 0,
            MemoryTotalBytes = 1024L * 1024 * 1024
        };

        // Assert
        Assert.Equal(0.0, utilization.CPUUsagePercent);
        Assert.Equal(0, utilization.MemoryUsedBytes);
    }

    [Fact]
    public void ResourceUtilization_Handles100PercentCPU()
    {
        // Arrange & Act
        var utilization = new ResourceUtilization
        {
            CPUUsagePercent = 100.0,
            MemoryUsedBytes = 1024L * 1024 * 1024,
            MemoryTotalBytes = 1024L * 1024 * 1024
        };

        // Assert
        Assert.Equal(100.0, utilization.CPUUsagePercent);
    }

    #endregion
}
