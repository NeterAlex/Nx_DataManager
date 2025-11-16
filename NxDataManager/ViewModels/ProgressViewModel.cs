using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly Guid _taskId;
    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty]
    private string _taskName = "备份任务";

    [ObservableProperty]
    private double _overallPercentage;

    [ObservableProperty]
    private long _totalFiles;

    [ObservableProperty]
    private long _processedFiles;

    [ObservableProperty]
    private long _totalSize;

    [ObservableProperty]
    private long _processedSize;

    [ObservableProperty]
    private string _currentFileName = "准备中...";

    [ObservableProperty]
    private double _currentFilePercentage;

    [ObservableProperty]
    private string _currentSpeed = "0 MB/s";

    [ObservableProperty]
    private string _averageSpeed = "0 MB/s";

    [ObservableProperty]
    private string _elapsedTime = "00:00:00";

    [ObservableProperty]
    private string _estimatedTimeRemaining = "计算中...";

    [ObservableProperty]
    private bool _canPause = true;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private bool _canStop = true;

    public ObservableCollection<string> RecentFiles { get; } = new();

    public long RemainingFiles => TotalFiles - ProcessedFiles;
    public long RemainingSize => TotalSize - ProcessedSize;

    public string TotalSizeFormatted => FormatBytes(TotalSize);
    public string ProcessedSizeFormatted => FormatBytes(ProcessedSize);
    public string RemainingSizeFormatted => FormatBytes(RemainingSize);

    public ProgressViewModel(IBackupService backupService, Guid taskId, string taskName)
    {
        _backupService = backupService;
        _taskId = taskId;
        _taskName = taskName;
        _stopwatch.Start();
    }

    public void UpdateProgress(long totalFiles, long processedFiles, long totalSize, long processedSize, string currentFile)
    {
        TotalFiles = totalFiles;
        ProcessedFiles = processedFiles;
        TotalSize = totalSize;
        ProcessedSize = processedSize;
        CurrentFileName = currentFile;

        OverallPercentage = totalFiles > 0 ? (double)processedFiles / totalFiles * 100 : 0;
        CurrentFilePercentage = 100; // 简化实现

        // 更新速度统计
        var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
        if (elapsedSeconds > 0)
        {
            var bytesPerSecond = processedSize / elapsedSeconds;
            CurrentSpeed = $"{FormatBytes((long)bytesPerSecond)}/s";
            AverageSpeed = CurrentSpeed;

            // 估算剩余时间
            var remainingBytes = totalSize - processedSize;
            if (bytesPerSecond > 0)
            {
                var remainingSeconds = remainingBytes / bytesPerSecond;
                EstimatedTimeRemaining = FormatTimeSpan(TimeSpan.FromSeconds(remainingSeconds));
            }
        }

        ElapsedTime = FormatTimeSpan(_stopwatch.Elapsed);

        // 更新最近文件列表
        if (!string.IsNullOrEmpty(currentFile) && currentFile != "准备中...")
        {
            RecentFiles.Insert(0, currentFile);
            while (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
        }

        OnPropertyChanged(nameof(RemainingFiles));
        OnPropertyChanged(nameof(RemainingSize));
        OnPropertyChanged(nameof(TotalSizeFormatted));
        OnPropertyChanged(nameof(ProcessedSizeFormatted));
        OnPropertyChanged(nameof(RemainingSizeFormatted));
    }

    [RelayCommand]
    private async Task PauseAsync()
    {
        await _backupService.PauseBackupAsync(_taskId);
        CanPause = false;
        CanResume = true;
    }

    [RelayCommand]
    private async Task ResumeAsync()
    {
        await _backupService.ResumeBackupAsync(_taskId);
        CanPause = true;
        CanResume = false;
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        await _backupService.StopBackupAsync(_taskId);
        CanPause = false;
        CanResume = false;
        CanStop = false;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return timeSpan.ToString(@"hh\:mm\:ss");
        else
            return timeSpan.ToString(@"mm\:ss");
    }
}
