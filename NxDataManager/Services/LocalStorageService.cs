using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 本地存储服务实现
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _dataDirectory;
    private readonly string _tasksFile;
    private readonly string _historiesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _dataDirectory = Path.Combine(appData, "NxDataManager");
        _tasksFile = Path.Combine(_dataDirectory, "tasks.json");
        _historiesDirectory = Path.Combine(_dataDirectory, "histories");

        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        if (!Directory.Exists(_historiesDirectory))
        {
            Directory.CreateDirectory(_historiesDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task SaveBackupTaskAsync(BackupTask task)
    {
        var tasks = await LoadBackupTasksAsync();
        var existingTask = tasks.FirstOrDefault(t => t.Id == task.Id);
        
        if (existingTask != null)
        {
            tasks.Remove(existingTask);
        }
        
        tasks.Add(task);
        
        var json = JsonSerializer.Serialize(tasks, _jsonOptions);
        await File.WriteAllTextAsync(_tasksFile, json);
    }

    public async Task<List<BackupTask>> LoadBackupTasksAsync()
    {
        if (!File.Exists(_tasksFile))
        {
            return new List<BackupTask>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_tasksFile);
            var tasks = JsonSerializer.Deserialize<List<BackupTask>>(json);
            return tasks ?? new List<BackupTask>();
        }
        catch
        {
            return new List<BackupTask>();
        }
    }

    public async Task DeleteBackupTaskAsync(Guid taskId)
    {
        var tasks = await LoadBackupTasksAsync();
        tasks.RemoveAll(t => t.Id == taskId);
        
        var json = JsonSerializer.Serialize(tasks, _jsonOptions);
        await File.WriteAllTextAsync(_tasksFile, json);
    }

    public async Task SaveBackupHistoryAsync(BackupHistory history)
    {
        var taskHistoryFile = Path.Combine(_historiesDirectory, $"{history.TaskId}.json");
        var histories = await LoadBackupHistoriesAsync(history.TaskId);
        
        histories.Add(history);
        
        var json = JsonSerializer.Serialize(histories, _jsonOptions);
        await File.WriteAllTextAsync(taskHistoryFile, json);
    }

    public async Task<List<BackupHistory>> LoadBackupHistoriesAsync(Guid taskId)
    {
        var taskHistoryFile = Path.Combine(_historiesDirectory, $"{taskId}.json");
        
        if (!File.Exists(taskHistoryFile))
        {
            return new List<BackupHistory>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(taskHistoryFile);
            var histories = JsonSerializer.Deserialize<List<BackupHistory>>(json);
            return histories ?? new List<BackupHistory>();
        }
        catch
        {
            return new List<BackupHistory>();
        }
    }

    public async Task<Dictionary<string, FileBackupInfo>> GetLastBackupFilesAsync(Guid taskId)
    {
        // 暂时返回空字典，实际实现需要扩展存储逻辑
        return await Task.FromResult(new Dictionary<string, FileBackupInfo>());
    }

    public async Task<Dictionary<string, FileBackupInfo>> GetLastFullBackupFilesAsync(Guid taskId)
    {
        // 暂时返回空字典，实际实现需要扩展存储逻辑
        return await Task.FromResult(new Dictionary<string, FileBackupInfo>());
    }

    public async Task SaveFileBackupRecordAsync(FileBackupInfo fileInfo, Guid taskId, Guid historyId)
    {
        // 暂时不实现，LocalStorageService 可能被弃用
        await Task.CompletedTask;
    }

    public async Task SaveFileBackupRecordsBatchAsync(List<FileBackupInfo> fileInfos, Guid taskId, Guid historyId)
    {
        // 暂时不实现，LocalStorageService 可能被弃用
        await Task.CompletedTask;
    }

    public async Task<List<FileBackupInfo>> GetFileBackupInfosByHistoryAsync(Guid taskId, Guid historyId)
    {
        // 暂时不实现，LocalStorageService 可能被弃用
        return await Task.FromResult(new List<FileBackupInfo>());
    }
}
