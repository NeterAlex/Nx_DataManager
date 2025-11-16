using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Data;
using NxDataManager.Data.Repositories;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// SQLite存储服务实现
/// </summary>
public class SqliteStorageService : IStorageService
{
    private readonly DatabaseContext _context;
    private readonly BackupTaskRepository _taskRepository;
    private readonly BackupHistoryRepository _historyRepository;

    public SqliteStorageService()
    {
        _context = new DatabaseContext();
        _taskRepository = new BackupTaskRepository(_context);
        _historyRepository = new BackupHistoryRepository(_context);
        
        System.Diagnostics.Debug.WriteLine("✅ SQLite存储服务已初始化");
    }

    #region 备份任务管理

    public async Task<List<BackupTask>> LoadBackupTasksAsync()
    {
        try
        {
            var tasks = await _taskRepository.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"✅ 从SQLite加载了 {tasks.Count()} 个备份任务");
            return tasks.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 加载备份任务失败: {ex.Message}");
            return new List<BackupTask>();
        }
    }

    public async Task SaveBackupTaskAsync(BackupTask task)
    {
        try
        {
            var existing = await _taskRepository.GetByIdAsync(task.Id);
            
            if (existing == null)
            {
                await _taskRepository.AddAsync(task);
                System.Diagnostics.Debug.WriteLine($"✅ 添加新任务到SQLite: {task.Name}");
            }
            else
            {
                await _taskRepository.UpdateAsync(task);
                System.Diagnostics.Debug.WriteLine($"✅ 更新SQLite任务: {task.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 保存任务失败: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteBackupTaskAsync(Guid taskId)
    {
        try
        {
            await _taskRepository.DeleteAsync(taskId);
            System.Diagnostics.Debug.WriteLine($"✅ 从SQLite删除任务: {taskId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 删除任务失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region 备份历史管理

    public async Task<List<BackupHistory>> LoadBackupHistoriesAsync(Guid taskId)
    {
        try
        {
            var histories = await _historyRepository.GetByTaskIdAsync(taskId);
            System.Diagnostics.Debug.WriteLine($"✅ 从SQLite加载了 {histories.Count()} 条历史记录");
            return histories.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 加载历史记录失败: {ex.Message}");
            return new List<BackupHistory>();
        }
    }

    public async Task SaveBackupHistoryAsync(BackupHistory history)
    {
        try
        {
            var existing = await _historyRepository.GetByIdAsync(history.Id);
            
            if (existing == null)
            {
                await _historyRepository.AddAsync(history);
                System.Diagnostics.Debug.WriteLine($"✅ 添加历史记录到SQLite");
            }
            else
            {
                await _historyRepository.UpdateAsync(history);
                System.Diagnostics.Debug.WriteLine($"✅ 更新SQLite历史记录");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 保存历史记录失败: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteBackupHistoryAsync(Guid historyId)
    {
        try
        {
            await _historyRepository.DeleteAsync(historyId);
            System.Diagnostics.Debug.WriteLine($"✅ 从SQLite删除历史记录: {historyId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 删除历史记录失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region 文件备份记录管理

    public async Task<Dictionary<string, FileBackupInfo>> GetLastBackupFilesAsync(Guid taskId)
    {
        try
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT RelativePath, FileSize, LastModifiedTime, Hash, BackupTime, BackupType
                FROM FileBackupRecords
                WHERE TaskId = @TaskId 
                AND HistoryId = (
                    SELECT Id FROM BackupHistories 
                    WHERE TaskId = @TaskId AND Status = 3 
                    ORDER BY StartTime DESC LIMIT 1
                )";
            
            command.Parameters.AddWithValue("@TaskId", taskId.ToString());
            
            var files = new Dictionary<string, FileBackupInfo>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var fileInfo = new FileBackupInfo
                {
                    RelativePath = reader.GetString(0),
                    FileSize = reader.GetInt64(1),
                    LastModifiedTime = DateTime.Parse(reader.GetString(2)),
                    Hash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    BackupTime = DateTime.Parse(reader.GetString(4)),
                    BackupType = (BackupType)reader.GetInt32(5)
                };
                
                files[fileInfo.RelativePath] = fileInfo;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ 获取到 {files.Count} 个上次备份的文件信息");
            return files;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 获取上次备份文件失败: {ex.Message}");
            return new Dictionary<string, FileBackupInfo>();
        }
    }

    public async Task<Dictionary<string, FileBackupInfo>> GetLastFullBackupFilesAsync(Guid taskId)
    {
        try
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT RelativePath, FileSize, LastModifiedTime, Hash, BackupTime, BackupType
                FROM FileBackupRecords
                WHERE TaskId = @TaskId 
                AND BackupType = 0
                AND HistoryId = (
                    SELECT Id FROM BackupHistories 
                    WHERE TaskId = @TaskId AND Status = 3 AND BackupType = 0
                    ORDER BY StartTime DESC LIMIT 1
                )";
            
            command.Parameters.AddWithValue("@TaskId", taskId.ToString());
            
            var files = new Dictionary<string, FileBackupInfo>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var fileInfo = new FileBackupInfo
                {
                    RelativePath = reader.GetString(0),
                    FileSize = reader.GetInt64(1),
                    LastModifiedTime = DateTime.Parse(reader.GetString(2)),
                    Hash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    BackupTime = DateTime.Parse(reader.GetString(4)),
                    BackupType = (BackupType)reader.GetInt32(5)
                };
                
                files[fileInfo.RelativePath] = fileInfo;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ 获取到 {files.Count} 个上次全量备份的文件信息");
            return files;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 获取上次全量备份文件失败: {ex.Message}");
            return new Dictionary<string, FileBackupInfo>();
        }
    }

    public async Task SaveFileBackupRecordAsync(FileBackupInfo fileInfo, Guid taskId, Guid historyId)
    {
        try
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO FileBackupRecords 
                (Id, TaskId, HistoryId, RelativePath, FileSize, LastModifiedTime, Hash, BackupTime, BackupType)
                VALUES (@Id, @TaskId, @HistoryId, @RelativePath, @FileSize, @LastModifiedTime, @Hash, @BackupTime, @BackupType)";
            
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@TaskId", taskId.ToString());
            command.Parameters.AddWithValue("@HistoryId", historyId.ToString());
            command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath);
            command.Parameters.AddWithValue("@FileSize", fileInfo.FileSize);
            command.Parameters.AddWithValue("@LastModifiedTime", fileInfo.LastModifiedTime.ToString("o"));
            command.Parameters.AddWithValue("@Hash", fileInfo.Hash ?? string.Empty);
            command.Parameters.AddWithValue("@BackupTime", fileInfo.BackupTime.ToString("o"));
            command.Parameters.AddWithValue("@BackupType", (int)fileInfo.BackupType);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 保存文件备份记录失败: {ex.Message}");
        }
    }

    public async Task SaveFileBackupRecordsBatchAsync(List<FileBackupInfo> fileInfos, Guid taskId, Guid historyId)
    {
        if (fileInfos == null || fileInfos.Count == 0)
            return;

        try
        {
            using var connection = _context.GetConnection();
            using var transaction = connection.BeginTransaction();
            
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT OR REPLACE INTO FileBackupRecords 
                (Id, TaskId, HistoryId, RelativePath, FileSize, LastModifiedTime, Hash, BackupTime, BackupType)
                VALUES (@Id, @TaskId, @HistoryId, @RelativePath, @FileSize, @LastModifiedTime, @Hash, @BackupTime, @BackupType)";
            
            foreach (var fileInfo in fileInfos)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@TaskId", taskId.ToString());
                command.Parameters.AddWithValue("@HistoryId", historyId.ToString());
                command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath);
                command.Parameters.AddWithValue("@FileSize", fileInfo.FileSize);
                command.Parameters.AddWithValue("@LastModifiedTime", fileInfo.LastModifiedTime.ToString("o"));
                command.Parameters.AddWithValue("@Hash", fileInfo.Hash ?? string.Empty);
                command.Parameters.AddWithValue("@BackupTime", fileInfo.BackupTime.ToString("o"));
                command.Parameters.AddWithValue("@BackupType", (int)fileInfo.BackupType);
                
                await command.ExecuteNonQueryAsync();
            }
            
            transaction.Commit();
            System.Diagnostics.Debug.WriteLine($"✅ 批量保存了 {fileInfos.Count} 个文件备份记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 批量保存文件备份记录失败: {ex.Message}");
        }
    }

    public async Task<List<FileBackupInfo>> GetFileBackupInfosByHistoryAsync(Guid taskId, Guid historyId)
    {
        var fileInfos = new List<FileBackupInfo>();

        try
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT RelativePath, FileSize, LastModifiedTime, Hash, BackupTime, BackupType
                FROM FileBackupRecords
                WHERE TaskId = @TaskId AND HistoryId = @HistoryId
                ORDER BY RelativePath";
            
            command.Parameters.AddWithValue("@TaskId", taskId.ToString());
            command.Parameters.AddWithValue("@HistoryId", historyId.ToString());
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var fileInfo = new FileBackupInfo
                {
                    RelativePath = reader.GetString(0),
                    FileSize = reader.GetInt64(1),
                    LastModifiedTime = DateTime.Parse(reader.GetString(2)),
                    Hash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    BackupTime = DateTime.Parse(reader.GetString(4)),
                    BackupType = (BackupType)reader.GetInt32(5)
                };
                
                fileInfos.Add(fileInfo);
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ 获取到 {fileInfos.Count} 条文件备份记录 (HistoryId: {historyId})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 获取文件备份记录失败: {ex.Message}");
        }

        return fileInfos;
    }

    #endregion

    #region 辅助方法

    public async Task<int> GetTaskCountAsync()
    {
        return await _taskRepository.CountAsync();
    }

    public async Task<int> GetHistoryCountAsync()
    {
        return await _historyRepository.CountAsync();
    }

    public async Task CleanupOldHistoriesAsync(int keepDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-keepDays);
            var deleted = await _historyRepository.DeleteOlderThan(cutoffDate);
            System.Diagnostics.Debug.WriteLine($"✅ 清理了 {deleted} 条旧历史记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 清理历史记录失败: {ex.Message}");
        }
    }

    #endregion

    #region 远程连接配置（暂未实现，返回占位符）

    public async Task<List<object>> LoadConnectionConfigsAsync()
    {
        // TODO: 实现远程连接配置加载
        return await Task.FromResult(new List<object>());
    }

    public async Task SaveConnectionConfigAsync(object config)
    {
        // TODO: 实现远程连接配置保存
        await Task.CompletedTask;
    }

    public async Task DeleteConnectionConfigAsync(Guid configId)
    {
        // TODO: 实现远程连接配置删除
        await Task.CompletedTask;
    }

    #endregion
}
