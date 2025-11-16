using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 存储服务接口
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// 保存备份任务
    /// </summary>
    Task SaveBackupTaskAsync(BackupTask task);
    
    /// <summary>
    /// 加载所有备份任务
    /// </summary>
    Task<List<BackupTask>> LoadBackupTasksAsync();
    
    /// <summary>
    /// 删除备份任务
    /// </summary>
    Task DeleteBackupTaskAsync(Guid taskId);
    
    /// <summary>
    /// 保存备份历史
    /// </summary>
    Task SaveBackupHistoryAsync(BackupHistory history);
    
    /// <summary>
    /// 加载备份历史
    /// </summary>
    Task<List<BackupHistory>> LoadBackupHistoriesAsync(Guid taskId);
    
    /// <summary>
    /// 获取最后一次备份的文件信息
    /// </summary>
    Task<Dictionary<string, FileBackupInfo>> GetLastBackupFilesAsync(Guid taskId);
    
    /// <summary>
    /// 获取最后一次全量备份的文件信息
    /// </summary>
    Task<Dictionary<string, FileBackupInfo>> GetLastFullBackupFilesAsync(Guid taskId);
    
    /// <summary>
    /// 保存文件备份记录
    /// </summary>
    Task SaveFileBackupRecordAsync(FileBackupInfo fileInfo, Guid taskId, Guid historyId);
    
    /// <summary>
    /// 批量保存文件备份记录
    /// </summary>
    Task SaveFileBackupRecordsBatchAsync(List<FileBackupInfo> fileInfos, Guid taskId, Guid historyId);
    
    /// <summary>
    /// 根据历史记录ID获取文件备份记录
    /// </summary>
    Task<List<FileBackupInfo>> GetFileBackupInfosByHistoryAsync(Guid taskId, Guid historyId);
}
