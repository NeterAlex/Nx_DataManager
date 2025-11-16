using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Models;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 数据库状态查看窗口 ViewModel
/// </summary>
public partial class DatabaseStatusViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private Window? _window;

    [ObservableProperty]
    private ObservableCollection<BackupTask> _tasks = new();

    [ObservableProperty]
    private BackupTask? _selectedTask;

    [ObservableProperty]
    private ObservableCollection<FileBackupRecord> _fileRecords = new();

    [ObservableProperty]
    private string _statusMessage = "正在加载...";

    [ObservableProperty]
    private int _totalTasks;

    [ObservableProperty]
    private int _totalHistories;

    [ObservableProperty]
    private int _totalFileRecords;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _selectedBackupType = "全部";

    public DatabaseStatusViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载数据库状态...";

            // 加载所有任务
            var tasks = await _storageService.LoadBackupTasksAsync();
            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }

            TotalTasks = tasks.Count;
            StatusMessage = $"加载完成：共 {TotalTasks} 个任务";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"数据库状态加载失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTaskDetails()
    {
        if (SelectedTask == null)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = $"正在加载任务 '{SelectedTask.Name}' 的文件记录...";

            // 加载文件记录
            var records = await LoadFileBackupRecordsAsync(SelectedTask.Id);
            
            FileRecords.Clear();
            foreach (var record in records)
            {
                FileRecords.Add(record);
            }

            TotalFileRecords = records.Count;
            StatusMessage = $"任务 '{SelectedTask.Name}' 有 {TotalFileRecords} 条文件记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载文件记录失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"加载文件记录失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await InitializeAsync();
        if (SelectedTask != null)
        {
            await LoadTaskDetails();
        }
    }

    [RelayCommand]
    private void Close()
    {
        _window?.Close();
    }

    private async Task<ObservableCollection<FileBackupRecord>> LoadFileBackupRecordsAsync(Guid taskId)
    {
        var records = new ObservableCollection<FileBackupRecord>();

        try
        {
            // 获取所有备份历史
            var histories = await _storageService.LoadBackupHistoriesAsync(taskId);

            foreach (var history in histories.OrderByDescending(h => h.StartTime))
            {
                // 根据历史记录获取文件记录
                var fileInfos = await _storageService.GetFileBackupInfosByHistoryAsync(taskId, history.Id);

                foreach (var fileInfo in fileInfos)
                {
                    records.Add(new FileBackupRecord
                    {
                        HistoryId = history.Id,
                        HistoryStartTime = history.StartTime,
                        BackupType = fileInfo.BackupType,
                        BackupTypeDisplay = GetBackupTypeDisplay(fileInfo.BackupType),
                        RelativePath = fileInfo.RelativePath,
                        FileSize = fileInfo.FileSize,
                        FileSizeFormatted = FormatFileSize(fileInfo.FileSize),
                        LastModifiedTime = fileInfo.LastModifiedTime,
                        BackupTime = fileInfo.BackupTime
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载文件备份记录失败: {ex.Message}");
        }

        return records;
    }

    private string GetBackupTypeDisplay(BackupType backupType)
    {
        return backupType switch
        {
            BackupType.Full => "全量",
            BackupType.Incremental => "增量",
            BackupType.Differential => "差异",
            _ => backupType.ToString()
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 文件备份记录显示模型
/// </summary>
public class FileBackupRecord
{
    public Guid HistoryId { get; set; }
    public DateTime HistoryStartTime { get; set; }
    public BackupType BackupType { get; set; }
    public string BackupTypeDisplay { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public DateTime LastModifiedTime { get; set; }
    public DateTime BackupTime { get; set; }
}
