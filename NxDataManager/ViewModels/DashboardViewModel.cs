using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Controls;
using NxDataManager.Models;
using NxDataManager.Services;
using WpfColor = System.Windows.Media.Color;

namespace NxDataManager.ViewModels;

/// <summary>
/// 仪表盘 ViewModel
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IStorageService _storageService;
    private readonly ISchedulerService _schedulerService;
    private readonly INotificationService _notificationService;

    #region 快速统计卡片

    [ObservableProperty]
    private int _totalTasks;

    [ObservableProperty]
    private int _enabledTasks;

    [ObservableProperty]
    private int _weekSuccessCount;

    [ObservableProperty]
    private int _weekFailureCount;

    [ObservableProperty]
    private double _averageSpeed; // MB/s

    [ObservableProperty]
    private double _storageUtilization; // %

    [ObservableProperty]
    private long _totalBackupSize;

    [ObservableProperty]
    private string _totalBackupSizeFormatted = "0 GB";

    #endregion

    #region 图表数据

    [ObservableProperty]
    private List<ChartDataPoint> _successRateChartData = new();

    [ObservableProperty]
    private List<ChartDataPoint> _dailyBackupSizeData = new();

    [ObservableProperty]
    private List<ChartDataPoint> _taskStorageDistribution = new();

    [ObservableProperty]
    private List<ChartDataPoint> _backupTimeData = new();

    #endregion

    #region 实时状态

    [ObservableProperty]
    private ObservableCollection<BackupTask> _runningTasks = new();

    [ObservableProperty]
    private ObservableCollection<BackupTask> _queuedTasks = new();

    [ObservableProperty]
    private ObservableCollection<BackupHistory> _recentCompletedTasks = new();

    #endregion

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _lastUpdated = DateTime.Now;

    public DashboardViewModel(
        IBackupService backupService,
        IStorageService storageService,
        ISchedulerService schedulerService,
        INotificationService notificationService)
    {
        _backupService = backupService;
        _storageService = storageService;
        _schedulerService = schedulerService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 初始化仪表盘数据
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;

        try
        {
            await Task.WhenAll(
                LoadQuickStatsAsync(),
                LoadChartsDataAsync(),
                LoadRealTimeStatusAsync()
            );

            LastUpdated = DateTime.Now;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("加载失败", $"仪表盘数据加载失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载快速统计数据
    /// </summary>
    private async Task LoadQuickStatsAsync()
    {
        var tasks = await _storageService.LoadBackupTasksAsync();
        TotalTasks = tasks.Count;
        EnabledTasks = tasks.Count(t => t.IsEnabled);

        // 获取最近7天的历史记录
        var weekStart = DateTime.Now.AddDays(-7);
        var allHistories = new List<BackupHistory>();
        
        foreach (var task in tasks)
        {
            var histories = await _backupService.GetBackupHistoryAsync(task.Id);
            allHistories.AddRange(histories.Where(h => h.StartTime >= weekStart));
        }

        WeekSuccessCount = allHistories.Count(h => h.Status == BackupStatus.Completed);
        WeekFailureCount = allHistories.Count(h => h.Status == BackupStatus.Failed);

        // 计算平均速度
        var completedHistories = allHistories.Where(h => h.Status == BackupStatus.Completed && h.Duration.TotalSeconds > 0).ToList();
        if (completedHistories.Any())
        {
            AverageSpeed = completedHistories.Average(h => (h.TotalSize / 1024.0 / 1024.0) / h.Duration.TotalSeconds);
        }

        // 计算总备份大小
        TotalBackupSize = allHistories.Sum(h => h.TotalSize);
        TotalBackupSizeFormatted = FormatBytes(TotalBackupSize);

        // 存储利用率（示例：假设总可用空间为 500GB）
        var totalAvailableSpace = 500L * 1024 * 1024 * 1024;
        StorageUtilization = (TotalBackupSize / (double)totalAvailableSpace) * 100;
    }

    /// <summary>
    /// 加载图表数据
    /// </summary>
    private async Task LoadChartsDataAsync()
    {
        var tasks = await _storageService.LoadBackupTasksAsync();
        
        // 1. 最近7天备份成功率曲线
        var successRateData = new List<ChartDataPoint>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Now.AddDays(-i);
            var dateStr = date.ToString("MM-dd");
            
            var dayHistories = new List<BackupHistory>();
            foreach (var task in tasks)
            {
                var histories = await _backupService.GetBackupHistoryAsync(task.Id);
                dayHistories.AddRange(histories.Where(h => h.StartTime.Date == date.Date));
            }

            var totalCount = dayHistories.Count;
            var successCount = dayHistories.Count(h => h.Status == BackupStatus.Completed);
            var successRate = totalCount > 0 ? (successCount / (double)totalCount) * 100 : 0;

            successRateData.Add(new ChartDataPoint
            {
                Label = dateStr,
                Value = successRate,
                Color = WpfColor.FromRgb(33, 150, 243) // 蓝色
            });
        }
        SuccessRateChartData = successRateData;

        // 2. 每日备份数据量
        var dailySizeData = new List<ChartDataPoint>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Now.AddDays(-i);
            var dateStr = date.ToString("MM-dd");
            
            var dayHistories = new List<BackupHistory>();
            foreach (var task in tasks)
            {
                var histories = await _backupService.GetBackupHistoryAsync(task.Id);
                dayHistories.AddRange(histories.Where(h => h.StartTime.Date == date.Date && h.Status == BackupStatus.Completed));
            }

            var totalSize = dayHistories.Sum(h => h.TotalSize) / 1024.0 / 1024.0 / 1024.0; // 转换为 GB

            dailySizeData.Add(new ChartDataPoint
            {
                Label = dateStr,
                Value = totalSize,
                Color = WpfColor.FromRgb(16, 185, 129) // 绿色
            });
        }
        DailyBackupSizeData = dailySizeData;

        // 3. 各任务存储占比（饼图）
        var storageDistribution = new List<ChartDataPoint>();
        var colors = new[] {
            WpfColor.FromRgb(33, 150, 243),   // 蓝色
            WpfColor.FromRgb(16, 185, 129),   // 绿色
            WpfColor.FromRgb(245, 158, 11),   // 黄色
            WpfColor.FromRgb(239, 68, 68),    // 红色
            WpfColor.FromRgb(147, 51, 234),   // 紫色
            WpfColor.FromRgb(236, 72, 153)    // 粉色
        };

        var taskSizes = new Dictionary<string, long>();
        foreach (var task in tasks.Take(6)) // 只显示前6个
        {
            var histories = await _backupService.GetBackupHistoryAsync(task.Id);
            var latestHistory = histories.OrderByDescending(h => h.StartTime).FirstOrDefault();
            if (latestHistory != null)
            {
                taskSizes[task.Name] = latestHistory.TotalSize;
            }
        }

        int colorIndex = 0;
        foreach (var kvp in taskSizes.OrderByDescending(kv => kv.Value))
        {
            storageDistribution.Add(new ChartDataPoint
            {
                Label = kvp.Key,
                Value = kvp.Value / 1024.0 / 1024.0 / 1024.0, // GB
                Color = colors[colorIndex++ % colors.Length]
            });
        }
        TaskStorageDistribution = storageDistribution;

        // 4. 备份耗时趋势
        var timeData = new List<ChartDataPoint>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Now.AddDays(-i);
            var dateStr = date.ToString("MM-dd");
            
            var dayHistories = new List<BackupHistory>();
            foreach (var task in tasks)
            {
                var histories = await _backupService.GetBackupHistoryAsync(task.Id);
                dayHistories.AddRange(histories.Where(h => h.StartTime.Date == date.Date && h.Status == BackupStatus.Completed));
            }

            var avgTime = dayHistories.Any() 
                ? dayHistories.Average(h => h.Duration.TotalMinutes) 
                : 0;

            timeData.Add(new ChartDataPoint
            {
                Label = dateStr,
                Value = avgTime,
                Color = WpfColor.FromRgb(245, 158, 11) // 橙色
            });
        }
        BackupTimeData = timeData;
    }

    /// <summary>
    /// 加载实时状态
    /// </summary>
    private async Task LoadRealTimeStatusAsync()
    {
        var tasks = await _storageService.LoadBackupTasksAsync();

        // 正在运行的任务
        RunningTasks.Clear();
        foreach (var task in tasks.Where(t => t.Status == BackupStatus.Running))
        {
            RunningTasks.Add(task);
        }

        // 队列中的任务（已启用但未运行的）
        QueuedTasks.Clear();
        foreach (var task in tasks.Where(t => t.IsEnabled && t.Status == BackupStatus.Idle))
        {
            QueuedTasks.Add(task);
        }

        // 最近完成的任务
        RecentCompletedTasks.Clear();
        var allHistories = new List<BackupHistory>();
        foreach (var task in tasks)
        {
            var histories = await _backupService.GetBackupHistoryAsync(task.Id);
            allHistories.AddRange(histories);
        }

        foreach (var history in allHistories
            .OrderByDescending(h => h.EndTime ?? h.StartTime)
            .Take(5))
        {
            RecentCompletedTasks.Add(history);
        }
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    [RelayCommand]
    private async Task Refresh()
    {
        await InitializeAsync();
        _notificationService.ShowSuccess("刷新成功", "仪表盘数据已更新");
    }

    /// <summary>
    /// 格式化字节大小
    /// </summary>
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
}
