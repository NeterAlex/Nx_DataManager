using System;
using System.Collections.Generic;

namespace NxDataManager.Models;

/// <summary>
/// 文件备份信息
/// </summary>
public class FileBackupInfo
{
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime BackupTime { get; set; }
    public BackupType BackupType { get; set; }
    
    /// <summary>
    /// 备份状态（成功/失败）
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 备份历史记录
/// </summary>
public class BackupHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public BackupType BackupType { get; set; }
    public BackupStatus Status { get; set; }
    public long TotalFiles { get; set; }
    public long SuccessFiles { get; set; }
    public long FailedFiles { get; set; }
    public long TotalSize { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    
    /// <summary>
    /// 平均速度（MB/s）
    /// </summary>
    public double AverageSpeed => Duration.TotalSeconds > 0 
        ? (TotalSize / 1024.0 / 1024.0) / Duration.TotalSeconds 
        : 0;
    
    /// <summary>
    /// 备份的文件列表
    /// </summary>
    public List<FileBackupInfo> BackupFiles { get; set; } = new();
    
    /// <summary>
    /// 源路径
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标路径
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;
}
