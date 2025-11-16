using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NxDataManager.Helpers;
using NxDataManager.Models;
using NxDataManager.Services;
using NxDataManager.Views;

namespace NxDataManager.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IStorageService _storageService;
    private readonly ISchedulerService _schedulerService;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<BackupTask> _backupTasks = new();

    [ObservableProperty]
    private BackupTask? _selectedTask;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBackupRunning;

    public int EnabledTasksCount => BackupTasks.Count(t => t.IsEnabled);

    public MainViewModel(
        IBackupService backupService,
        IStorageService storageService,
        ISchedulerService schedulerService,
        INotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _backupService = backupService;
        _storageService = storageService;
        _schedulerService = schedulerService;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
        
        // 监听 BackupTasks 集合的变化
        BackupTasks.CollectionChanged += (s, e) =>
        {
            // 当集合发生变化时，更新 EnabledTasksCount
            OnPropertyChanged(nameof(EnabledTasksCount));
            
            // 为新添加的任务订阅 IsEnabled 属性变化
            if (e.NewItems != null)
            {
                foreach (BackupTask task in e.NewItems)
                {
                    task.PropertyChanged += Task_PropertyChanged;
                }
            }
            
            // 为移除的任务取消订阅
            if (e.OldItems != null)
            {
                foreach (BackupTask task in e.OldItems)
                {
                    task.PropertyChanged -= Task_PropertyChanged;
                }
            }
        };
    }
    
    private void Task_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 当任何任务的 IsEnabled 属性变化时，更新 EnabledTasksCount
        if (e.PropertyName == nameof(BackupTask.IsEnabled))
        {
            OnPropertyChanged(nameof(EnabledTasksCount));
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载备份任务...";

        try
        {
            var tasks = await _storageService.LoadBackupTasksAsync();
            BackupTasks.Clear();
            
            foreach (var task in tasks)
            {
                BackupTasks.Add(task);
                
                if (task.IsEnabled && task.Schedule != null)
                {
                    _schedulerService.AddScheduledTask(task);
                }
            }

            await _schedulerService.StartAsync();
            StatusMessage = $"已加载 {tasks.Count} 个备份任务";
            _notificationService.ShowInfo("欢迎使用", "NxDataManager 备份管理器已启动");
            
            // 通知 EnabledTasksCount 属性更改
            OnPropertyChanged(nameof(EnabledTasksCount));
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

    [RelayCommand]
    private void CreateTask()
    {
        try
        {
            var newTask = new BackupTask
            {
                Name = "新建备份任务",
                CreatedTime = DateTime.Now,
                Schedule = new BackupSchedule()
            };

            // 打开编辑窗口
            OpenTaskEditor(newTask, isNew: true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建任务失败: {ex.Message}";
            _notificationService.ShowError("创建失败", ex.Message);
        }
    }

    [RelayCommand]
    private void EditTask(object? parameter)
    {
        BackupTask? taskToEdit = null;
        
        // 如果传入了参数（从卡片双击），使用参数
        if (parameter is BackupTask task)
        {
            taskToEdit = task;
        }
        // 否则使用选中的任务
        else if (SelectedTask != null)
        {
            taskToEdit = SelectedTask;
        }
        
        if (taskToEdit == null)
        {
            _notificationService.ShowWarning("提示", "请先选择一个任务");
            return;
        }

        OpenTaskEditor(taskToEdit, isNew: false);
    }

    private void OpenTaskEditor(BackupTask task, bool isNew)
    {
        var viewModel = new TaskEditorViewModel(
            task,
            _storageService,
            _notificationService,
            onSaved: async () =>
            {
                // 保存后刷新列表
                if (isNew)
                {
                    BackupTasks.Add(task);
                    SelectedTask = task;
                }

                // 更新计划任务
                if (task.IsEnabled && task.Schedule != null && task.Schedule.Type != ScheduleType.Manual)
                {
                    _schedulerService.UpdateScheduledTask(task);
                }
                else
                {
                    _schedulerService.RemoveScheduledTask(task.Id);
                }

                StatusMessage = $"任务 '{task.Name}' 已保存";
                
                // 通知属性更改
                OnPropertyChanged(nameof(EnabledTasksCount));
            },
            onDeleted: () =>
            {
                // 删除后从列表移除
                BackupTasks.Remove(task);
                _schedulerService.RemoveScheduledTask(task.Id);
                SelectedTask = null;
                StatusMessage = $"任务已删除";
                
                // 通知属性更改
                OnPropertyChanged(nameof(EnabledTasksCount));
            });

        var window = new Views.TaskEditorWindow(viewModel);
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task DeleteTask()
    {
        if (SelectedTask == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"确定要删除任务 '{SelectedTask.Name}' 吗？\n\n此操作不可撤销！",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            var taskName = SelectedTask.Name;
            await _storageService.DeleteBackupTaskAsync(SelectedTask.Id);
            _schedulerService.RemoveScheduledTask(SelectedTask.Id);
            BackupTasks.Remove(SelectedTask);
            StatusMessage = "任务已删除";
            _notificationService.ShowInfo("任务已删除", $"任务 \"{taskName}\" 已删除");
            
            // 通知属性更改
            OnPropertyChanged(nameof(EnabledTasksCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            _notificationService.ShowError("删除失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveTask()
    {
        if (SelectedTask == null) return;

        try
        {
            await _storageService.SaveBackupTaskAsync(SelectedTask);
            
            if (SelectedTask.IsEnabled && SelectedTask.Schedule != null)
            {
                _schedulerService.UpdateScheduledTask(SelectedTask);
            }
            else
            {
                _schedulerService.RemoveScheduledTask(SelectedTask.Id);
            }

            StatusMessage = "任务已保存";
            _notificationService.ShowSuccess("保存成功", $"任务 \"{SelectedTask.Name}\" 已保存");
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            _notificationService.ShowError("保存失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task StartBackup(object? parameter)
    {
        // Get task from parameter or use SelectedTask
        BackupTask? taskToBackup = null;
        
        if (parameter is BackupTask task)
        {
            taskToBackup = task;
        }
        else if (SelectedTask != null)
        {
            taskToBackup = SelectedTask;
        }
        
        if (taskToBackup == null) return;
        if (IsBackupRunning) return;

        try
        {
            // 对于增量和差异备份，显示预览窗口
            if (taskToBackup.BackupType == BackupType.Incremental || taskToBackup.BackupType == BackupType.Differential)
            {
                var previewService = _serviceProvider.GetRequiredService<IBackupPreviewService>();
                var previewViewModel = new BackupPreviewViewModel(taskToBackup, previewService, confirmed =>
                {
                    if (confirmed)
                    {
                        // 用户确认后开始备份
                        _ = PerformBackupAsync(taskToBackup);
                    }
                });

                var previewWindow = new Views.BackupPreviewWindow(previewViewModel);
                previewWindow.ShowDialog();
                return;
            }

            // 全量备份直接执行
            await PerformBackupAsync(taskToBackup);
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动备份失败: {ex.Message}";
            _notificationService.ShowError("错误", ex.Message);
        }
    }

    private async Task PerformBackupAsync(BackupTask task)
    {
        try
        {
            IsBackupRunning = true;
            task.Status = BackupStatus.Running;
            StatusMessage = $"正在备份: {task.Name}";
            
            var progress = new Progress<BackupProgress>(p =>
            {
                task.ProcessedFiles = p.ProcessedFiles;
                task.TotalFiles = p.TotalFiles;
                task.ProcessedSize = p.ProcessedSize;
                task.TotalSize = p.TotalSize;
                StatusMessage = $"备份中: {p.CurrentFile} ({p.Percentage:F1}%)";
                
                // 每10%显示一次通知
                if (p.Percentage % 10 < 0.1 && p.Percentage > 0)
                {
                    _notificationService.ShowProgress(
                        $"备份进度: {p.Percentage:F0}%", 
                        $"{task.Name}\n{p.ProcessedFiles}/{p.TotalFiles} 文件", 
                        p.Percentage);
                }
            });

            var history = await _backupService.StartBackupAsync(task, progress);
            
            if (history.Status == BackupStatus.Completed)
            {
                StatusMessage = $"备份完成: {task.Name}";
                _notificationService.ShowSuccess("备份完成", 
                    $"任务 \"{task.Name}\" 完成\n成功: {history.SuccessFiles}/{history.TotalFiles} 文件");
            }
            else
            {
                StatusMessage = $"备份失败: {history.ErrorMessage}";
                _notificationService.ShowError("备份失败", history.ErrorMessage ?? "未知错误");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"备份出错: {ex.Message}";
            _notificationService.ShowError("备份出错", ex.Message);
        }
        finally
        {
            IsBackupRunning = false;
        }
    }

    [RelayCommand]
    private async Task StopBackup()
    {
        if (SelectedTask == null) return;

        try
        {
            await _backupService.StopBackupAsync(SelectedTask.Id);
            StatusMessage = "备份已停止";
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PauseBackup()
    {
        if (SelectedTask == null) return;

        try
        {
            await _backupService.PauseBackupAsync(SelectedTask.Id);
            SelectedTask.Status = BackupStatus.Paused;
            StatusMessage = "备份已暂停";
        }
        catch (Exception ex)
        {
            StatusMessage = $"暂停失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ResumeBackup()
    {
        if (SelectedTask == null) return;

        try
        {
            await _backupService.ResumeBackupAsync(SelectedTask.Id);
            SelectedTask.Status = BackupStatus.Running;
            StatusMessage = "备份已恢复";
        }
        catch (Exception ex)
        {
            StatusMessage = $"恢复失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseSourcePath()
    {
        System.Diagnostics.Debug.WriteLine("=== BrowseSourcePath 命令执行 ===");
        
        if (SelectedTask == null)
        {
            System.Diagnostics.Debug.WriteLine("SelectedTask 为 null");
            StatusMessage = "请先选择一个任务";
            _notificationService.ShowWarning("提示", "请先选择一个任务");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"当前任务: {SelectedTask.Name}");
        System.Diagnostics.Debug.WriteLine("准备调用 DialogHelper.SelectFolder...");
        
        try
        {
            var path = DialogHelper.SelectFolder("请选择源文件夹");
            
            System.Diagnostics.Debug.WriteLine($"DialogHelper 返回路径: {path ?? "null"}");
            
            if (!string.IsNullOrEmpty(path))
            {
                System.Diagnostics.Debug.WriteLine($"设置源路径: {path}");
                SelectedTask.SourcePath = path;
                StatusMessage = $"已选择源路径: {path}";
                System.Diagnostics.Debug.WriteLine("源路径设置成功");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("用户取消了选择或返回空路径");
                StatusMessage = "未选择路径";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== BrowseSourcePath 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常: {ex}");
            StatusMessage = $"选择路径失败: {ex.Message}";
            _notificationService.ShowError("错误", $"选择路径失败: {ex.Message}");
        }
        
        System.Diagnostics.Debug.WriteLine("=== BrowseSourcePath 命令结束 ===");
    }

    [RelayCommand]
    private void BrowseDestinationPath()
    {
        System.Diagnostics.Debug.WriteLine("=== BrowseDestinationPath 命令执行 ===");
        
        if (SelectedTask == null)
        {
            System.Diagnostics.Debug.WriteLine("SelectedTask 为 null");
            StatusMessage = "请先选择一个任务";
            _notificationService.ShowWarning("提示", "请先选择一个任务");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"当前任务: {SelectedTask.Name}");
        System.Diagnostics.Debug.WriteLine("准备调用 DialogHelper.SelectFolder...");
        
        try
        {
            var path = DialogHelper.SelectFolder("请选择备份目标文件夹");
            
            System.Diagnostics.Debug.WriteLine($"DialogHelper 返回路径: {path ?? "null"}");
            
            if (!string.IsNullOrEmpty(path))
            {
                System.Diagnostics.Debug.WriteLine($"设置目标路径: {path}");
                SelectedTask.DestinationPath = path;
                StatusMessage = $"已选择目标路径: {path}";
                System.Diagnostics.Debug.WriteLine("目标路径设置成功");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("用户取消了选择或返回空路径");
                StatusMessage = "未选择路径";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== BrowseDestinationPath 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常: {ex}");
            StatusMessage = $"选择路径失败: {ex.Message}";
            _notificationService.ShowError("错误", $"选择路径失败: {ex.Message}");
        }
        
        System.Diagnostics.Debug.WriteLine("=== BrowseDestinationPath 命令结束 ===");
    }

    [RelayCommand]
    private async Task ViewHistory()
    {
        try
        {
            // 直接打开备份历史窗口，显示所有任务的历史记录
            var viewModel = new BackupHistoryViewModel(
                _backupService,
                _storageService,
                _notificationService,
                _serviceProvider.GetRequiredService<IReportExportService>());
            
            var window = new Views.BackupHistoryWindow(viewModel);
            window.ShowDialog();
            
            StatusMessage = "已打开备份历史窗口";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开历史窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开历史窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenRestoreWindow()
    {
        try
        {
            var restoreWindow = _serviceProvider.GetRequiredService<RestoreWindow>();
            restoreWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开恢复窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开恢复窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        try
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开设置失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开设置失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDatabaseStatus()
    {
        try
        {
            var viewModel = new DatabaseStatusViewModel(_storageService);
            var window = new Views.DatabaseStatusWindow(viewModel);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开数据库状态窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开数据库状态窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenVersionControl()
    {
        try
        {
            var versionControlService = _serviceProvider.GetRequiredService<IVersionControlService>();
            var viewModel = new VersionControlViewModel(versionControlService, _notificationService);
            var window = new Views.VersionControlWindow(viewModel);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开版本控制窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开版本控制窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenScheduleHistory()
    {
        try
        {
            var viewModel = new ScheduleHistoryViewModel(_schedulerService);
            var window = new Views.ScheduleHistoryWindow(viewModel);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开执行历史窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开执行历史窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenRemoteConnection()
    {
        try
        {
            var remoteConnectionWindow = _serviceProvider.GetRequiredService<RemoteConnectionWindow>();
            remoteConnectionWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开远程连接窗口失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开远程连接窗口失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        try
        {
            var dashboardWindow = _serviceProvider.GetRequiredService<DashboardWindow>();
            dashboardWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开仪表盘失败: {ex.Message}";
            _notificationService.ShowError("错误", $"打开仪表盘失败: {ex.Message}");
        }
    }
}
