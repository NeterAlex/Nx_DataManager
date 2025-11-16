using System;
using System.Windows;

namespace NxDataManager.Services;

/// <summary>
/// 简单的通知服务实现（使用MessageBox作为后备）
/// </summary>
public class ToastNotificationService : INotificationService
{
    public void ShowInfo(string title, string message)
    {
        ShowNotification(title, message, "信息");
    }

    public void ShowSuccess(string title, string message)
    {
        ShowNotification(title, message, "成功");
    }

    public void ShowWarning(string title, string message)
    {
        ShowNotification(title, message, "警告");
    }

    public void ShowError(string title, string message)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    public void ShowProgress(string title, string message, double progress)
    {
        // 进度通知暂时不使用弹窗，避免干扰用户
        System.Diagnostics.Debug.WriteLine($"{title}: {message} - {progress:F1}%");
    }

    private void ShowNotification(string title, string message, string type)
    {
        try
        {
            // 在后台线程中不显示通知，避免阻塞
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            {
                // 在UI线程中，可以显示通知
                System.Diagnostics.Debug.WriteLine($"[{type}] {title}: {message}");
            }
        }
        catch (Exception)
        {
            // 静默失败
        }
    }
}
