using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 智能备份策略服务接口
/// </summary>
public interface ISmartBackupStrategy
{
    /// <summary>
    /// 分析文件变化模式
    /// </summary>
    Task<FileChangePattern> AnalyzeFileChangePatternsAsync(string directoryPath, TimeSpan analysisWindow);
    
    /// <summary>
    /// 推荐最佳备份策略
    /// </summary>
    Task<BackupStrategyRecommendation> RecommendStrategyAsync(string directoryPath);
    
    /// <summary>
    /// 计算最佳备份时间
    /// </summary>
    Task<DateTime> CalculateOptimalBackupTimeAsync(Guid taskId);
    
    /// <summary>
    /// 预测存储空间需求
    /// </summary>
    Task<StorageRequirement> PredictStorageRequirementAsync(BackupTask task, TimeSpan duration);
    
    /// <summary>
    /// 自动调整备份频率
    /// </summary>
    Task<BackupSchedule> AutoAdjustScheduleAsync(Guid taskId);
}

/// <summary>
/// 文件变化模式
/// </summary>
public class FileChangePattern
{
    public double AverageChangeRate { get; set; } // 平均变化率（文件/天）
    public double AverageChangeSize { get; set; } // 平均变化大小（字节/天）
    public List<int> PeakChangeHours { get; set; } = new(); // 高峰变化时段
    public Dictionary<string, int> ChangeFrequencyByType { get; set; } = new(); // 按文件类型统计
    public bool IsStable { get; set; } // 是否稳定
}

/// <summary>
/// 备份策略推荐
/// </summary>
public class BackupStrategyRecommendation
{
    public BackupType RecommendedType { get; set; }
    public ScheduleType RecommendedSchedule { get; set; }
    public TimeSpan RecommendedInterval { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; } // 置信度 0-1
}

/// <summary>
/// 存储需求预测
/// </summary>
public class StorageRequirement
{
    public long InitialSize { get; set; }
    public long PredictedSize { get; set; }
    public long MaxSize { get; set; }
    public double GrowthRate { get; set; } // 每天增长率
    public DateTime PredictionDate { get; set; }
}
