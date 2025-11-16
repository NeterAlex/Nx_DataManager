using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Models;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 备份历史窗口 ViewModel
/// </summary>
public partial class BackupHistoryViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IStorageService _storageService;
    private readonly INotificationService _notificationService;
    private readonly IReportExportService _reportExportService;

    #region 数据属性

    [ObservableProperty]
    private ObservableCollection<BackupHistory> _allHistories = new();

    [ObservableProperty]
    private ObservableCollection<BackupHistory> _filteredHistories = new();

    [ObservableProperty]
    private BackupHistory? _selectedHistory;

    [ObservableProperty]
    private ObservableCollection<FileBackupInfo> _selectedHistoryFiles = new();

    [ObservableProperty]
    private ObservableCollection<string> _taskNames = new();

    #endregion

    #region 筛选属性

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _selectedTaskFilter;

    [ObservableProperty]
    private BackupStatus? _selectedStatusFilter;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _selectedTimeRange = "全部";

    [ObservableProperty]
    private string? _selectedFileType;

    #endregion

    #region 统计属性

    [ObservableProperty]
    private int _totalHistories;

    [ObservableProperty]
    private int _successCount;

    [ObservableProperty]
    private int _failureCount;

    [ObservableProperty]
    private int _partialSuccessCount;

    [ObservableProperty]
    private long _totalBackupSize;

    [ObservableProperty]
    private double _averageSpeed;

    #endregion

    #region UI 状态

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isDetailsExpanded;

    #endregion

    public BackupHistoryViewModel(
        IBackupService backupService,
        IStorageService storageService,
        INotificationService notificationService,
        IReportExportService reportExportService)
    {
        _backupService = backupService;
        _storageService = storageService;
        _notificationService = notificationService;
        _reportExportService = reportExportService;

        // 监听筛选条件变化
        PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName is nameof(SearchText) or 
                nameof(SelectedTaskFilter) or 
                nameof(SelectedStatusFilter) or 
                nameof(StartDate) or 
                nameof(EndDate) or
                nameof(SelectedFileType))
            {
                await ApplyFiltersAsync();
            }
        };
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载备份历史...";

        try
        {
            // 加载所有任务
            var tasks = await _storageService.LoadBackupTasksAsync();
            TaskNames.Clear();
            TaskNames.Add("全部任务");
            foreach (var task in tasks)
            {
                TaskNames.Add(task.Name);
            }

            // 加载所有历史记录
            var allHistoriesList = new List<BackupHistory>();
            foreach (var task in tasks)
            {
                var histories = await _backupService.GetBackupHistoryAsync(task.Id);
                allHistoriesList.AddRange(histories);
            }

            AllHistories = new ObservableCollection<BackupHistory>(
                allHistoriesList.OrderByDescending(h => h.StartTime));

            await ApplyFiltersAsync();
            UpdateStatistics();

            StatusMessage = $"已加载 {AllHistories.Count} 条历史记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            _notificationService.ShowError("加载失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 应用筛选条件
    /// </summary>
    private async Task ApplyFiltersAsync()
    {
        await Task.Run(() =>
        {
            var filtered = AllHistories.AsEnumerable();

            // 文本搜索
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(h =>
                    h.TaskName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (h.ErrorMessage?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // 任务名称筛选
            if (!string.IsNullOrEmpty(SelectedTaskFilter) && SelectedTaskFilter != "全部任务")
            {
                filtered = filtered.Where(h => h.TaskName == SelectedTaskFilter);
            }

            // 状态筛选
            if (SelectedStatusFilter.HasValue)
            {
                filtered = filtered.Where(h => h.Status == SelectedStatusFilter.Value);
            }

            // 日期范围筛选
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(h => h.StartTime >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                filtered = filtered.Where(h => h.StartTime <= EndDate.Value.AddDays(1));
            }

            // 文件类型筛选
            if (!string.IsNullOrEmpty(SelectedFileType) && SelectedFileType != "全部文件")
            {
                filtered = filtered.Where(h =>
                    h.BackupFiles.Any(f => f.RelativePath.EndsWith(SelectedFileType, StringComparison.OrdinalIgnoreCase)));
            }

            var result = filtered.OrderByDescending(h => h.StartTime).ToList();

            App.Current.Dispatcher.Invoke(() =>
            {
                FilteredHistories.Clear();
                foreach (var history in result)
                {
                    FilteredHistories.Add(history);
                }
            });
        });

        UpdateStatistics();
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        TotalHistories = FilteredHistories.Count;
        SuccessCount = FilteredHistories.Count(h => h.Status == BackupStatus.Completed);
        FailureCount = FilteredHistories.Count(h => h.Status == BackupStatus.Failed);
        PartialSuccessCount = FilteredHistories.Count(h => 
            h.Status == BackupStatus.Completed && h.FailedFiles > 0);
        TotalBackupSize = FilteredHistories.Sum(h => h.TotalSize);

        var completedHistories = FilteredHistories
            .Where(h => h.Status == BackupStatus.Completed && h.Duration.TotalSeconds > 0)
            .ToList();

        if (completedHistories.Any())
        {
            AverageSpeed = completedHistories.Average(h => h.AverageSpeed);
        }
        else
        {
            AverageSpeed = 0;
        }
    }

    /// <summary>
    /// 选择历史记录时加载文件列表
    /// </summary>
    partial void OnSelectedHistoryChanged(BackupHistory? value)
    {
        if (value != null)
        {
            SelectedHistoryFiles = new ObservableCollection<FileBackupInfo>(value.BackupFiles);
            IsDetailsExpanded = true;
        }
        else
        {
            SelectedHistoryFiles.Clear();
            IsDetailsExpanded = false;
        }
    }

    #region 时间范围快速筛选

    [RelayCommand]
    private async Task SelectTimeRange(string range)
    {
        SelectedTimeRange = range;
        var now = DateTime.Now;

        switch (range)
        {
            case "今天":
                StartDate = now.Date;
                EndDate = now.Date;
                break;

            case "本周":
                var dayOfWeek = (int)now.DayOfWeek;
                var diff = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Monday as first day
                StartDate = now.Date.AddDays(-diff);
                EndDate = now.Date;
                break;

            case "本月":
                StartDate = new DateTime(now.Year, now.Month, 1);
                EndDate = now.Date;
                break;

            case "最近7天":
                StartDate = now.Date.AddDays(-6);
                EndDate = now.Date;
                break;

            case "最近30天":
                StartDate = now.Date.AddDays(-29);
                EndDate = now.Date;
                break;

            case "全部":
                StartDate = null;
                EndDate = null;
                break;
        }

        await ApplyFiltersAsync();
    }

    #endregion

    #region 导出功能

    [RelayCommand]
    private async Task ExportToCsv()
    {
        try
        {
            var path = Helpers.DialogHelper.SaveFile(
                "导出为 CSV",
                $"备份历史_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                "CSV 文件|*.csv");

            if (string.IsNullOrEmpty(path))
                return;

            IsLoading = true;
            StatusMessage = "正在导出 CSV...";

            await _reportExportService.ExportHistoryToCsvAsync(FilteredHistories.ToList(), path);

            StatusMessage = "CSV 导出成功";
            _notificationService.ShowSuccess("导出成功", $"已导出到: {path}");

            // 询问是否打开文件
            var result = System.Windows.MessageBox.Show(
                "导出成功！是否打开文件？",
                "导出完成",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
            _notificationService.ShowError("导出失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToPdf()
    {
        try
        {
            var path = Helpers.DialogHelper.SaveFile(
                "导出为 PDF",
                $"备份历史报告_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                "PDF 文件|*.pdf");

            if (string.IsNullOrEmpty(path))
                return;

            IsLoading = true;
            StatusMessage = "正在生成 PDF 报告...";

            await _reportExportService.ExportHistoryToPdfAsync(FilteredHistories.ToList(), path);

            StatusMessage = "PDF 报告生成成功";
            _notificationService.ShowSuccess("导出成功", $"已导出到: {path}");

            // 询问是否打开文件
            var result = System.Windows.MessageBox.Show(
                "导出成功！是否打开文件？",
                "导出完成",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
            _notificationService.ShowError("导出失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToHtml()
    {
        try
        {
            var path = Helpers.DialogHelper.SaveFile(
                "导出为 HTML",
                $"备份历史报告_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                "HTML 文件|*.html");

            if (string.IsNullOrEmpty(path))
                return;

            IsLoading = true;
            StatusMessage = "正在生成 HTML 报告...";

            await _reportExportService.ExportHistoryToHtmlAsync(FilteredHistories.ToList(), path);

            StatusMessage = "HTML 报告生成成功";
            _notificationService.ShowSuccess("导出成功", $"已导出到: {path}");

            // 询问是否打开文件
            var result = System.Windows.MessageBox.Show(
                "导出成功！是否打开文件？",
                "导出完成",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
            _notificationService.ShowError("导出失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region 其他操作

    [RelayCommand]
    private async Task DeleteHistory()
    {
        if (SelectedHistory == null)
        {
            _notificationService.ShowWarning("提示", "请先选择一条历史记录");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "确定要删除此历史记录吗？\n\n此操作不可撤销！",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            await _backupService.DeleteBackupHistoryAsync(SelectedHistory.Id);
            AllHistories.Remove(SelectedHistory);
            FilteredHistories.Remove(SelectedHistory);
            UpdateStatistics();
            StatusMessage = "历史记录已删除";
            _notificationService.ShowSuccess("删除成功", "历史记录已删除");
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            _notificationService.ShowError("删除失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedTaskFilter = null;
        SelectedStatusFilter = null;
        StartDate = null;
        EndDate = null;
        SelectedFileType = null;
        SelectedTimeRange = "全部";
    }

    [RelayCommand]
    private void ViewErrorLog()
    {
        if (SelectedHistory?.ErrorMessage != null)
        {
            System.Windows.MessageBox.Show(
                SelectedHistory.ErrorMessage,
                "错误日志",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    #endregion
}
