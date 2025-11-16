using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Models;

namespace NxDataManager.ViewModels;

/// <summary>
/// 备份任务详情ViewModel
/// </summary>
public partial class BackupTaskDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private BackupTask? _task;

    [ObservableProperty]
    private ObservableCollection<string> _excludedPatterns = new();

    [ObservableProperty]
    private string _newExcludePattern = string.Empty;

    [ObservableProperty]
    private bool _hasSchedule;

    partial void OnTaskChanged(BackupTask? value)
    {
        if (value != null)
        {
            ExcludedPatterns.Clear();
            foreach (var pattern in value.ExcludedPatterns)
            {
                ExcludedPatterns.Add(pattern);
            }

            HasSchedule = value.Schedule != null;
            
            if (value.Schedule == null)
            {
                value.Schedule = new BackupSchedule();
            }
        }
    }

    [RelayCommand]
    private void AddExcludePattern()
    {
        if (string.IsNullOrWhiteSpace(NewExcludePattern) || Task == null)
            return;

        ExcludedPatterns.Add(NewExcludePattern);
        Task.ExcludedPatterns.Add(NewExcludePattern);
        NewExcludePattern = string.Empty;
    }

    [RelayCommand]
    private void RemoveExcludePattern(string pattern)
    {
        if (Task == null) return;

        ExcludedPatterns.Remove(pattern);
        Task.ExcludedPatterns.Remove(pattern);
    }

    [RelayCommand]
    private void ToggleSchedule()
    {
        if (Task == null) return;

        if (HasSchedule && Task.Schedule == null)
        {
            Task.Schedule = new BackupSchedule
            {
                Type = ScheduleType.Daily,
                StartTime = DateTime.Now.Date.AddHours(2)
            };
        }
        else if (!HasSchedule)
        {
            Task.Schedule = null;
        }
    }
}
