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
/// 计划任务执行历史 ViewModel
/// </summary>
public partial class ScheduleHistoryViewModel : ObservableObject
{
    private readonly ISchedulerService _schedulerService;
    private Window? _window;

    private ObservableCollection<ScheduledExecutionHistory> _histories = new();
    public ObservableCollection<ScheduledExecutionHistory> Histories
    {
        get => _histories;
        set => SetProperty(ref _histories, value);
    }

    private string _statusMessage = "正在加载...";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private int _totalExecutions;
    public int TotalExecutions
    {
        get => _totalExecutions;
        set => SetProperty(ref _totalExecutions, value);
    }

    private int _successfulExecutions;
    public int SuccessfulExecutions
    {
        get => _successfulExecutions;
        set => SetProperty(ref _successfulExecutions, value);
    }

    private int _failedExecutions;
    public int FailedExecutions
    {
        get => _failedExecutions;
        set => SetProperty(ref _failedExecutions, value);
    }

    private double _successRate;
    public double SuccessRate
    {
        get => _successRate;
        set => SetProperty(ref _successRate, value);
    }

    public ScheduleHistoryViewModel(ISchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    public async Task InitializeAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载执行历史...";

            var histories = await _schedulerService.GetAllExecutionHistoryAsync();
            
            Histories.Clear();
            foreach (var history in histories)
            {
                Histories.Add(history);
            }

            TotalExecutions = histories.Count;
            SuccessfulExecutions = histories.Count(h => h.IsSuccess);
            FailedExecutions = histories.Count(h => !h.IsSuccess);
            SuccessRate = TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

            StatusMessage = $"共 {TotalExecutions} 条执行记录，成功率 {SuccessRate:F1}%";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"加载执行历史失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        _window?.Close();
    }
}
