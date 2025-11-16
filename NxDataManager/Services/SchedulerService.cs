using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 计划任务调度服务
/// </summary>
public interface ISchedulerService
{
    /// <summary>
    /// 添加计划任务
    /// </summary>
    void AddScheduledTask(BackupTask task);
    
    /// <summary>
    /// 移除计划任务
    /// </summary>
    void RemoveScheduledTask(Guid taskId);
    
    /// <summary>
    /// 更新计划任务
    /// </summary>
    void UpdateScheduledTask(BackupTask task);
    
    /// <summary>
    /// 启动调度器
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// 停止调度器
    /// </summary>
    Task StopAsync();
    
    /// <summary>
    /// 获取计划任务执行历史
    /// </summary>
    Task<List<ScheduledExecutionHistory>> GetExecutionHistoryAsync(Guid taskId);
    
    /// <summary>
    /// 获取所有计划任务执行历史
    /// </summary>
    Task<List<ScheduledExecutionHistory>> GetAllExecutionHistoryAsync();
}

/// <summary>
/// 计划任务调度服务实现
/// </summary>
public class SchedulerService : ISchedulerService
{
    private readonly IBackupService _backupService;
    private readonly IStorageService _storageService;
    private readonly Dictionary<Guid, System.Threading.Timer> _timers = new();
    private readonly List<ScheduledExecutionHistory> _executionHistory = new();
    private bool _isRunning;

    public SchedulerService(IBackupService backupService, IStorageService storageService)
    {
        _backupService = backupService;
        _storageService = storageService;
    }

    public void AddScheduledTask(BackupTask task)
    {
        if (task.Schedule == null || task.Schedule.Type == ScheduleType.Manual || !task.IsEnabled)
        {
            return;
        }

        RemoveScheduledTask(task.Id);

        var nextRun = CalculateNextRunTime(task.Schedule);
        task.NextRunTime = nextRun;

        var dueTime = nextRun - DateTime.Now;
        if (dueTime < TimeSpan.Zero)
        {
            dueTime = TimeSpan.Zero;
        }

        var timer = new System.Threading.Timer(
            async _ => await ExecuteScheduledTask(task),
            null,
            dueTime,
            Timeout.InfiniteTimeSpan);

        _timers[task.Id] = timer;
    }

    public void RemoveScheduledTask(Guid taskId)
    {
        if (_timers.TryGetValue(taskId, out var timer))
        {
            timer.Dispose();
            _timers.Remove(taskId);
        }
    }

    public void UpdateScheduledTask(BackupTask task)
    {
        RemoveScheduledTask(task.Id);
        AddScheduledTask(task);
    }

    public Task StartAsync()
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isRunning = false;
        
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }
        
        _timers.Clear();
        return Task.CompletedTask;
    }

    private async Task ExecuteScheduledTask(BackupTask task)
    {
        if (!_isRunning || !task.IsEnabled)
        {
            return;
        }

        var history = new ScheduledExecutionHistory
        {
            TaskId = task.Id,
            TaskName = task.Name,
            ExecutionTime = DateTime.Now,
            ScheduleType = task.Schedule?.Type ?? ScheduleType.Manual,
            IsAutomatic = true
        };

        var startTime = DateTime.Now;

        try
        {
            System.Diagnostics.Debug.WriteLine($"⏰ 计划任务自动执行: {task.Name}");
            
            var backupHistory = await _backupService.StartBackupAsync(task);
            
            history.IsSuccess = backupHistory.Status == BackupStatus.Completed;
            history.ProcessedFiles = (int)backupHistory.SuccessFiles;
            history.TotalFiles = (int)backupHistory.TotalFiles;
            history.ProcessedSize = task.ProcessedSize;
            history.TotalSize = task.TotalSize;
            history.ErrorMessage = backupHistory.ErrorMessage;
            
            System.Diagnostics.Debug.WriteLine($"✅ 计划任务执行完成: {task.Name}, 状态: {history.IsSuccess}");
        }
        catch (Exception ex)
        {
            history.IsSuccess = false;
            history.ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"❌ 计划任务执行失败: {task.Name}, 错误: {ex.Message}");
        }

        history.Duration = DateTime.Now - startTime;
        
        // 保存执行历史
        _executionHistory.Add(history);
        await SaveExecutionHistoryAsync(history);

        // 重新安排下一次执行
        if (task.Schedule?.IsRecurring == true)
        {
            AddScheduledTask(task);
        }
    }

    public async Task<List<ScheduledExecutionHistory>> GetExecutionHistoryAsync(Guid taskId)
    {
        return _executionHistory.Where(h => h.TaskId == taskId).OrderByDescending(h => h.ExecutionTime).ToList();
    }

    public async Task<List<ScheduledExecutionHistory>> GetAllExecutionHistoryAsync()
    {
        return _executionHistory.OrderByDescending(h => h.ExecutionTime).ToList();
    }

    private async Task SaveExecutionHistoryAsync(ScheduledExecutionHistory history)
    {
        // TODO: 持久化到数据库
        // 目前只保存在内存中
        await Task.CompletedTask;
    }

    private DateTime CalculateNextRunTime(BackupSchedule schedule)
    {
        var now = DateTime.Now;

        return schedule.Type switch
        {
            ScheduleType.Daily => now.Date.AddDays(1).Add(schedule.StartTime.TimeOfDay),
            ScheduleType.Weekly => CalculateNextWeeklyRun(now, schedule),
            ScheduleType.Monthly => CalculateNextMonthlyRun(now, schedule),
            ScheduleType.Interval => now.AddHours(schedule.IntervalHours),
            _ => now.AddDays(1)
        };
    }

    private DateTime CalculateNextWeeklyRun(DateTime now, BackupSchedule schedule)
    {
        if (schedule.DaysOfWeek == null || !schedule.DaysOfWeek.Any())
        {
            return now.AddDays(7);
        }

        var currentDay = now.DayOfWeek;
        var nextDay = schedule.DaysOfWeek
            .Where(d => d > currentDay || (d == currentDay && now.TimeOfDay < schedule.StartTime.TimeOfDay))
            .OrderBy(d => d)
            .FirstOrDefault();

        if (nextDay == default)
        {
            nextDay = schedule.DaysOfWeek.Min();
            var daysUntil = ((int)nextDay - (int)currentDay + 7) % 7;
            if (daysUntil == 0) daysUntil = 7;
            return now.Date.AddDays(daysUntil).Add(schedule.StartTime.TimeOfDay);
        }

        var daysToAdd = (int)nextDay - (int)currentDay;
        return now.Date.AddDays(daysToAdd).Add(schedule.StartTime.TimeOfDay);
    }

    private DateTime CalculateNextMonthlyRun(DateTime now, BackupSchedule schedule)
    {
        var nextRun = new DateTime(now.Year, now.Month, Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(now.Year, now.Month)))
            .Add(schedule.StartTime.TimeOfDay);

        if (nextRun <= now)
        {
            var nextMonth = now.AddMonths(1);
            nextRun = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)))
                .Add(schedule.StartTime.TimeOfDay);
        }

        return nextRun;
    }
}
