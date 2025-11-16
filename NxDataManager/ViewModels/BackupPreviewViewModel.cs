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
/// 备份预览窗口 ViewModel
/// </summary>
public partial class BackupPreviewViewModel : ObservableObject
{
    private readonly BackupTask _task;
    private readonly IBackupPreviewService _previewService;
    private readonly Action<bool> _onComplete;
    private Window? _window;

    [ObservableProperty]
    private string _taskName = string.Empty;

    [ObservableProperty]
    private string _backupTypeDisplay = string.Empty;

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private int _totalFilesToBackup;

    [ObservableProperty]
    private int _totalFilesToSkip;

    [ObservableProperty]
    private string _totalSizeToBackup = string.Empty;

    [ObservableProperty]
    private string _totalSizeToSkip = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FilePreviewItem> _filesToBackup = new();

    [ObservableProperty]
    private ObservableCollection<FilePreviewItem> _filesToSkip = new();

    [ObservableProperty]
    private bool _isAnalyzing = true;

    [ObservableProperty]
    private string _statusMessage = "正在分析文件...";

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public BackupPreviewViewModel(
        BackupTask task,
        IBackupPreviewService previewService,
        Action<bool> onComplete)
    {
        _task = task;
        _previewService = previewService;
        _onComplete = onComplete;
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsAnalyzing = true;
            StatusMessage = "正在分析文件变化...";

            var preview = await _previewService.AnalyzeBackupAsync(_task);

            TaskName = preview.TaskName;
            BackupTypeDisplay = GetBackupTypeDisplayName(preview.BackupType);
            SourcePath = preview.SourcePath;
            DestinationPath = preview.DestinationPath;

            TotalFilesToBackup = preview.TotalFilesToBackup;
            TotalFilesToSkip = preview.TotalFilesToSkip;
            TotalSizeToBackup = FormatFileSize(preview.TotalSizeToBackup);
            TotalSizeToSkip = FormatFileSize(preview.TotalSizeToSkip);

            FilesToBackup.Clear();
            foreach (var file in preview.FilesToBackup)
            {
                FilesToBackup.Add(file);
            }

            FilesToSkip.Clear();
            foreach (var file in preview.FilesToSkip)
            {
                FilesToSkip.Add(file);
            }

            StatusMessage = $"分析完成：将备份 {TotalFilesToBackup} 个文件，跳过 {TotalFilesToSkip} 个文件";
        }
        catch (Exception ex)
        {
            StatusMessage = $"分析失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"备份预览失败: {ex}");
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        _onComplete?.Invoke(true);
        _window?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        _onComplete?.Invoke(false);
        _window?.Close();
    }

    private string GetBackupTypeDisplayName(Models.BackupType backupType)
    {
        return backupType switch
        {
            Models.BackupType.Full => "全量备份 (Full)",
            Models.BackupType.Incremental => "增量备份 (Incremental)",
            Models.BackupType.Differential => "差异备份 (Differential)",
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
