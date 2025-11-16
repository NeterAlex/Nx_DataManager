using System;
using System.Windows;
using Ookii.Dialogs.Wpf;
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;

namespace NxDataManager.Helpers;

/// <summary>
/// 文件对话框帮助类
/// 使用 Ookii.Dialogs.Wpf 提供现代化的文件夹选择对话框
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// 选择文件夹（使用Vista风格对话框）
    /// </summary>
    public static string? SelectFolder(string description = "请选择文件夹")
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== SelectFolder 开始 ===");
            System.Diagnostics.Debug.WriteLine($"Description: {description}");
            
            var dialog = new VistaFolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                Multiselect = false
            };

            System.Diagnostics.Debug.WriteLine("对话框已创建，准备显示...");
            
            // 获取主窗口作为所有者
            var owner = WpfApplication.Current?.MainWindow;
            System.Diagnostics.Debug.WriteLine($"Owner窗口: {owner?.Title ?? "null"}");
            
            var result = dialog.ShowDialog(owner);
            
            System.Diagnostics.Debug.WriteLine($"对话框结果: {result}");
            System.Diagnostics.Debug.WriteLine($"选择的路径: {dialog.SelectedPath ?? "null"}");
            
            return result == true ? dialog.SelectedPath : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== SelectFolder 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            
            // 显示消息框提示用户
            WpfMessageBox.Show(
                $"打开文件夹选择对话框失败:\n{ex.Message}", 
                "错误", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            
            return null;
        }
    }

    /// <summary>
    /// 选择单个文件（使用WPF原生对话框）
    /// </summary>
    public static string? SelectFile(string filter = "所有文件|*.*", string title = "请选择文件")
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = false
            };

            var owner = WpfApplication.Current?.MainWindow;
            var result = dialog.ShowDialog(owner);
            return result == true ? dialog.FileName : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"选择文件失败: {ex.Message}");
            WpfMessageBox.Show($"打开文件选择对话框失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    /// <summary>
    /// 选择多个文件（使用WPF原生对话框）
    /// </summary>
    public static string[]? SelectFiles(string filter = "所有文件|*.*", string title = "请选择文件")
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = true
            };

            var owner = WpfApplication.Current?.MainWindow;
            var result = dialog.ShowDialog(owner);
            return result == true ? dialog.FileNames : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"选择文件失败: {ex.Message}");
            WpfMessageBox.Show($"打开文件选择对话框失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    /// <summary>
    /// 保存文件对话框（使用WPF原生对话框）
    /// </summary>
    public static string? SaveFile(string filter = "所有文件|*.*", string title = "保存文件", string defaultExt = "")
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                Title = title,
                DefaultExt = defaultExt
            };

            var owner = WpfApplication.Current?.MainWindow;
            var result = dialog.ShowDialog(owner);
            return result == true ? dialog.FileName : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存文件失败: {ex.Message}");
            WpfMessageBox.Show($"打开保存文件对话框失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
}
