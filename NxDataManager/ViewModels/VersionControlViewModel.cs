using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Services;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;
using WpfWindow = System.Windows.Window;

namespace NxDataManager.ViewModels;

/// <summary>
/// 版本控制管理窗口 ViewModel
/// </summary>
public partial class VersionControlViewModel : ObservableObject
{
    private readonly IVersionControlService _versionControlService;
    private readonly INotificationService _notificationService;
    private WpfWindow? _window;

    private string _selectedFilePath = string.Empty;
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetProperty(ref _selectedFilePath, value);
    }

    private ObservableCollection<FileVersionDisplay> _versions = new();
    public ObservableCollection<FileVersionDisplay> Versions
    {
        get => _versions;
        set => SetProperty(ref _versions, value);
    }

    private FileVersionDisplay? _selectedVersion;
    public FileVersionDisplay? SelectedVersion
    {
        get => _selectedVersion;
        set => SetProperty(ref _selectedVersion, value);
    }

    private string _statusMessage = "请选择要管理版本的文件";
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

    private int _totalVersions;
    public int TotalVersions
    {
        get => _totalVersions;
        set => SetProperty(ref _totalVersions, value);
    }

    private long _totalVersionsSize;
    public long TotalVersionsSize
    {
        get => _totalVersionsSize;
        set => SetProperty(ref _totalVersionsSize, value);
    }

    private string _totalVersionsSizeFormatted = "0 B";
    public string TotalVersionsSizeFormatted
    {
        get => _totalVersionsSizeFormatted;
        set => SetProperty(ref _totalVersionsSizeFormatted, value);
    }

    public VersionControlViewModel(
        IVersionControlService versionControlService,
        INotificationService notificationService)
    {
        _versionControlService = versionControlService;
        _notificationService = notificationService;
    }

    public void SetWindow(WpfWindow window)
    {
        _window = window;
    }

    [RelayCommand]
    private void SelectFile()
    {
        try
        {
            var filePath = Helpers.DialogHelper.SelectFile("所有文件|*.*", "选择要管理版本的文件");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                SelectedFilePath = filePath;
                _ = LoadVersionsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件失败: {ex.Message}";
            _notificationService.ShowError("错误", ex.Message);
        }
    }

    [RelayCommand]
    private async Task LoadVersions()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            _notificationService.ShowWarning("提示", "请先选择文件");
            return;
        }

        await LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载版本列表...";

            var versions = await _versionControlService.GetFileVersionsAsync(SelectedFilePath);
            
            Versions.Clear();
            long totalSize = 0;

            foreach (var version in versions)
            {
                Versions.Add(new FileVersionDisplay
                {
                    Id = version.Id,
                    VersionNumber = version.VersionNumber,
                    CreatedTime = version.CreatedTime,
                    CreatedTimeFormatted = version.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = version.FileSize,
                    FileSizeFormatted = FormatFileSize(version.FileSize),
                    Hash = version.Hash,
                    Comment = version.Comment,
                    OriginalFilePath = version.OriginalFilePath,
                    VersionFilePath = version.VersionFilePath,
                    IsCurrent = version.VersionNumber == versions.Max(v => v.VersionNumber)
                });

                totalSize += version.FileSize;
            }

            TotalVersions = versions.Count;
            TotalVersionsSize = totalSize;
            TotalVersionsSizeFormatted = FormatFileSize(totalSize);

            StatusMessage = $"找到 {TotalVersions} 个版本，总大小 {TotalVersionsSizeFormatted}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载版本失败: {ex.Message}";
            _notificationService.ShowError("加载失败", ex.Message);
            System.Diagnostics.Debug.WriteLine($"加载版本失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreVersion()
    {
        if (SelectedVersion == null)
        {
            _notificationService.ShowWarning("提示", "请先选择要恢复的版本");
            return;
        }

        var result = WpfMessageBox.Show(
            $"确定要恢复到版本 #{SelectedVersion.VersionNumber} 吗？\n\n创建时间: {SelectedVersion.CreatedTimeFormatted}\n注释: {SelectedVersion.Comment}\n\n当前文件将被覆盖！",
            "确认恢复",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question);

        if (result != WpfMessageBoxResult.Yes)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在恢复版本...";

            // 选择恢复目标位置
            var targetPath = Helpers.DialogHelper.SaveFile(
                "所有文件|*.*",
                "选择恢复位置",
                Path.GetExtension(SelectedFilePath));

            if (string.IsNullOrEmpty(targetPath))
            {
                StatusMessage = "恢复已取消";
                return;
            }

            await _versionControlService.RestoreVersionAsync(SelectedVersion.Id, targetPath);

            StatusMessage = $"版本 #{SelectedVersion.VersionNumber} 已恢复到 {targetPath}";
            _notificationService.ShowSuccess("恢复成功", 
                $"版本已恢复到:\n{targetPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"恢复版本失败: {ex.Message}";
            _notificationService.ShowError("恢复失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteVersion()
    {
        if (SelectedVersion == null)
        {
            _notificationService.ShowWarning("提示", "请先选择要删除的版本");
            return;
        }

        if (SelectedVersion.IsCurrent)
        {
            _notificationService.ShowWarning("警告", "不能删除当前版本！");
            return;
        }

        var result = WpfMessageBox.Show(
            $"确定要删除版本 #{SelectedVersion.VersionNumber} 吗？\n\n创建时间: {SelectedVersion.CreatedTimeFormatted}\n大小: {SelectedVersion.FileSizeFormatted}\n\n此操作不可撤销！",
            "确认删除",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Warning);

        if (result != WpfMessageBoxResult.Yes)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在删除版本...";

            await _versionControlService.DeleteVersionAsync(SelectedVersion.Id);

            StatusMessage = $"版本 #{SelectedVersion.VersionNumber} 已删除";
            _notificationService.ShowSuccess("删除成功", "版本已删除");

            // 重新加载版本列表
            await LoadVersionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除版本失败: {ex.Message}";
            _notificationService.ShowError("删除失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CompareVersions()
    {
        if (SelectedVersion == null)
        {
            _notificationService.ShowWarning("提示", "请先选择要比较的版本");
            return;
        }

        // 选择另一个版本进行比较
        var otherVersions = Versions.Where(v => v.Id != SelectedVersion.Id).ToList();
        
        if (otherVersions.Count == 0)
        {
            _notificationService.ShowWarning("提示", "没有其他版本可供比较");
            return;
        }

        // TODO: 实现版本选择对话框
        _notificationService.ShowInfo("提示", "版本比较功能开发中...");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CleanupOldVersions()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            _notificationService.ShowWarning("提示", "请先选择文件");
            return;
        }

        var result = WpfMessageBox.Show(
            $"确定要清理旧版本吗？\n\n将保留最新的 5 个版本，删除其余版本。\n\n此操作不可撤销！",
            "确认清理",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question);

        if (result != WpfMessageBoxResult.Yes)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在清理旧版本...";

            await _versionControlService.CleanupOldVersionsAsync(SelectedFilePath, 5);

            StatusMessage = "旧版本已清理";
            _notificationService.ShowSuccess("清理完成", "已保留最新的 5 个版本");

            // 重新加载版本列表
            await LoadVersionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"清理失败: {ex.Message}";
            _notificationService.ShowError("清理失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenVersionFolder()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var versionPath = Path.Combine(appData, "NxDataManager", "Versions");

            if (Directory.Exists(versionPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", versionPath);
                StatusMessage = "已打开版本文件夹";
            }
            else
            {
                _notificationService.ShowWarning("提示", "版本文件夹不存在");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开文件夹失败: {ex.Message}";
            _notificationService.ShowError("错误", ex.Message);
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Close()
    {
        _window?.Close();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 文件版本显示模型
/// </summary>
public class FileVersionDisplay
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedTime { get; set; }
    public string CreatedTimeFormatted { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string VersionFilePath { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    
    public string VersionLabel => $"版本 #{VersionNumber}" + (IsCurrent ? " (当前)" : "");
    public string HashShort => Hash.Length > 8 ? Hash.Substring(0, 8) : Hash;
}
