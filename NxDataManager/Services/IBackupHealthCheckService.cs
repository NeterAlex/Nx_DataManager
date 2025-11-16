using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 备份健康检查服务接口
/// </summary>
public interface IBackupHealthCheckService
{
    /// <summary>
    /// 执行完整健康检查
    /// </summary>
    Task<HealthCheckReport> PerformFullCheckAsync();
    
    /// <summary>
    /// 检查单个任务健康状态
    /// </summary>
    Task<TaskHealthStatus> CheckTaskHealthAsync(Guid taskId);
    
    /// <summary>
    /// 获取健康评分
    /// </summary>
    Task<double> GetHealthScoreAsync();
    
    /// <summary>
    /// 获取健康建议
    /// </summary>
    Task<List<HealthRecommendation>> GetRecommendationsAsync();
}

/// <summary>
/// 健康检查报告
/// </summary>
public class HealthCheckReport
{
    public DateTime CheckTime { get; set; } = DateTime.Now;
    public double OverallScore { get; set; } // 0-100
    public int TotalTasks { get; set; }
    public int HealthyTasks { get; set; }
    public int WarningTasks { get; set; }
    public int CriticalTasks { get; set; }
    public List<TaskHealthStatus> TaskStatuses { get; set; } = new();
    public List<HealthRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// 任务健康状态
/// </summary>
public class TaskHealthStatus
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public HealthLevel Level { get; set; }
    public double Score { get; set; } // 0-100
    public List<string> Issues { get; set; } = new();
    public DateTime LastSuccessfulBackup { get; set; }
    public int ConsecutiveFailures { get; set; }
    public double AverageSuccessRate { get; set; }
}

/// <summary>
/// 健康等级
/// </summary>
public enum HealthLevel
{
    Healthy,    // 健康
    Warning,    // 警告
    Critical    // 严重
}

/// <summary>
/// 健康建议
/// </summary>
public class HealthRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public HealthLevel Priority { get; set; }
}
