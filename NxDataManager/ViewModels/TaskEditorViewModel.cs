using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Models;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 任务编辑窗口 ViewModel
/// </summary>
public partial class TaskEditorViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly INotificationService _notificationService;
    private readonly Action _onSaved;
    private readonly Action _onDeleted;
    private Window? _window;

    [ObservableProperty]
    private BackupTask _currentTask;

    [ObservableProperty]
    private string _windowTitle = "编辑备份任务";

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _encryptionPassword = string.Empty;

    public void SetWindow(Window window)
    {
        _window = window;
    }

    public TaskEditorViewModel(
        BackupTask task,
        IStorageService storageService,
        INotificationService notificationService,
        Action onSaved,
        Action onDeleted)
    {
        _currentTask = task;
        _storageService = storageService;
        _notificationService = notificationService;
        _onSaved = onSaved;
        _onDeleted = onDeleted;

        WindowTitle = string.IsNullOrEmpty(task.Name) ? "新建备份任务" : $"编辑任务 - {task.Name}";
        _encryptionPassword = task.EncryptionPassword ?? string.Empty;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveAsync()
    {
        try
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(CurrentTask.Name))
            {
                _notificationService.ShowWarning("验证失败", "请输入任务名称");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentTask.SourcePath))
            {
                _notificationService.ShowWarning("验证失败", "请选择源路径");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentTask.DestinationPath))
            {
                _notificationService.ShowWarning("验证失败", "请选择目标路径");
                return;
            }

            IsSaving = true;

            // 保存加密密码（如果启用了加密）
            if (CurrentTask.EnableEncryption && !string.IsNullOrEmpty(EncryptionPassword))
            {
                CurrentTask.EncryptionPassword = EncryptionPassword;
            }

            // 确保Schedule不为null
            if (CurrentTask.Schedule == null)
            {
                CurrentTask.Schedule = new BackupSchedule();
            }

            await _storageService.SaveBackupTaskAsync(CurrentTask);
            
            _notificationService.ShowSuccess("保存成功", $"任务 '{CurrentTask.Name}' 已保存");
            
            _onSaved?.Invoke();
            
            // 关闭窗口
            _window?.Close();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("保存失败", ex.Message);
            System.Diagnostics.Debug.WriteLine($"保存任务失败: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteAsync()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                $"确定要删除任务 '{CurrentTask.Name}' 吗？\n\n此操作不可撤销！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _storageService.DeleteBackupTaskAsync(CurrentTask.Id);
            
            _notificationService.ShowInfo("删除成功", $"任务 '{CurrentTask.Name}' 已删除");
            
            _onDeleted?.Invoke();
            
            // 关闭窗口
            _window?.Close();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("删除失败", ex.Message);
            System.Diagnostics.Debug.WriteLine($"删除任务失败: {ex}");
        }
    }

    [RelayCommand]
    private void BrowseSourcePath()
    {
        var path = Helpers.DialogHelper.SelectFolder("请选择源文件夹");
        if (!string.IsNullOrEmpty(path))
        {
            CurrentTask.SourcePath = path;
        }
    }

    [RelayCommand]
    private void BrowseDestinationPath()
    {
        var path = Helpers.DialogHelper.SelectFolder("请选择备份目标文件夹");
        if (!string.IsNullOrEmpty(path))
        {
            CurrentTask.DestinationPath = path;
        }
    }
}
