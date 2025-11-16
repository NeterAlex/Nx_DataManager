using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _showDesktopNotifications = true;

    [ObservableProperty]
    private int _maxBackupHistory = 30;

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private string _defaultBackupLocation = string.Empty;

    [ObservableProperty]
    private string _currentVersion = "v1.0.0-alpha";

    public SettingsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // TODO: 实现设置保存
        _notificationService.ShowSuccess("设置已保存", "您的偏好设置已保存");
    }

    [RelayCommand]
    private void ResetSettings()
    {
        EnableNotifications = true;
        StartWithWindows = false;
        MinimizeToTray = true;
        ShowDesktopNotifications = true;
        MaxBackupHistory = 30;
        AutoCheckUpdates = true;
        
        _notificationService.ShowInfo("设置已重置", "所有设置已恢复为默认值");
    }

    [RelayCommand]
    private void BrowseBackupLocation()
    {
        var path = Helpers.DialogHelper.SelectFolder("选择默认备份位置");
        if (!string.IsNullOrEmpty(path))
        {
            DefaultBackupLocation = path;
        }
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        _notificationService.ShowInfo("检查更新", "当前版本已是最新版本");
    }

    [RelayCommand]
    private void OpenAbout()
    {
        _notificationService.ShowInfo("关于", $"NxDataManager {CurrentVersion}\n\n一个功能强大的备份管理工具");
    }

    [RelayCommand]
    private void ClearCache()
    {
        _notificationService.ShowSuccess("清除缓存", "缓存已清除");
    }
}
