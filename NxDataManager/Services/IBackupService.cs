using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 备份服务接口
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// 开始备份任务
    /// </summary>
    Task<BackupHistory> StartBackupAsync(BackupTask task, IProgress<BackupProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止备份任务
    /// </summary>
    Task StopBackupAsync(Guid taskId);
    
    /// <summary>
    /// 暂停备份任务
    /// </summary>
    Task PauseBackupAsync(Guid taskId);
    
    /// <summary>
    /// 恢复备份任务
    /// </summary>
    Task ResumeBackupAsync(Guid taskId);
    
    /// <summary>
    /// 获取备份历史
    /// </summary>
    Task<List<BackupHistory>> GetBackupHistoryAsync(Guid taskId);
    
    /// <summary>
    /// 验证备份完整性
    /// </summary>
    Task<bool> VerifyBackupAsync(Guid historyId);
    
    /// <summary>
    /// 删除备份历史记录
    /// </summary>
    Task DeleteBackupHistoryAsync(Guid historyId);
}

/// <summary>
/// 备份进度信息
/// </summary>
public class BackupProgress
{
    public long TotalFiles { get; set; }
    public long ProcessedFiles { get; set; }
    public long TotalSize { get; set; }
    public long ProcessedSize { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public double Percentage => TotalSize > 0 ? (double)ProcessedSize / TotalSize * 100 : 0;
}
