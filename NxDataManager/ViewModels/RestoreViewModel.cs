using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Helpers;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 恢复/还原功能 ViewModel
/// </summary>
public partial class RestoreViewModel : ObservableObject
{
    private readonly IEncryptionService _encryptionService;
    private readonly ICompressionService _compressionService;
    private readonly INotificationService _notificationService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _encryptedFilePath = string.Empty;

    [ObservableProperty]
    private string _decryptionPassword = string.Empty;

    [ObservableProperty]
    private string _restorePath = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _currentStep = string.Empty;

    public RestoreViewModel(
        IEncryptionService encryptionService,
        ICompressionService compressionService,
        INotificationService notificationService)
    {
        _encryptionService = encryptionService;
        _compressionService = compressionService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void BrowseEncryptedFile()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择加密备份文件",
            Filter = "加密文件 (*.encrypted)|*.encrypted|ZIP文件 (*.zip)|*.zip|所有文件 (*.*)|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            EncryptedFilePath = openFileDialog.FileName;
            StatusMessage = $"已选择: {Path.GetFileName(EncryptedFilePath)}";
        }
    }

    [RelayCommand]
    private void BrowseRestorePath()
    {
        var path = DialogHelper.SelectFolder("选择恢复目标文件夹");
        if (!string.IsNullOrEmpty(path))
        {
            RestorePath = path;
            StatusMessage = $"恢复到: {path}";
        }
    }

    [RelayCommand]
    private async Task StartRestore()
    {
        if (string.IsNullOrEmpty(EncryptedFilePath))
        {
            _notificationService.ShowWarning("提示", "请选择要恢复的备份文件");
            return;
        }

        if (!File.Exists(EncryptedFilePath))
        {
            _notificationService.ShowError("错误", "选择的文件不存在");
            return;
        }

        if (string.IsNullOrEmpty(RestorePath))
        {
            _notificationService.ShowWarning("提示", "请选择恢复目标文件夹");
            return;
        }

        IsProcessing = true;
        Progress = 0;
        _cts = new CancellationTokenSource();

        try
        {
            var fileExtension = Path.GetExtension(EncryptedFilePath).ToLowerInvariant();
            string currentFile = EncryptedFilePath;

            // 步骤 1: 如果是加密文件，先解密
            if (fileExtension == ".encrypted")
            {
                if (string.IsNullOrEmpty(DecryptionPassword))
                {
                    _notificationService.ShowWarning("提示", "请输入解密密码");
                    IsProcessing = false;
                    return;
                }

                CurrentStep = "正在解密...";
                StatusMessage = "解密备份文件中...";

                var decryptProgress = new Progress<double>(p =>
                {
                    Progress = p * 0.5; // 解密占50%进度
                    StatusMessage = $"解密中: {p:F1}%";
                });

                var decryptedFile = await _encryptionService.DecryptFileAsync(
                    EncryptedFilePath,
                    DecryptionPassword,
                    decryptProgress,
                    _cts.Token);

                _notificationService.ShowSuccess("解密完成", $"文件已解密: {Path.GetFileName(decryptedFile)}");
                currentFile = decryptedFile;
            }

            // 步骤 2: 如果是 ZIP 文件，解压
            if (Path.GetExtension(currentFile).ToLowerInvariant() == ".zip")
            {
                CurrentStep = "正在解压...";
                StatusMessage = "解压备份文件中...";

                var extractProgress = new Progress<double>(p =>
                {
                    Progress = 50 + (p * 0.5); // 解压占50%进度
                    StatusMessage = $"解压中: {p:F1}%";
                });

                await _compressionService.ExtractAsync(
                    currentFile,
                    RestorePath,
                    extractProgress,
                    _cts.Token);

                _notificationService.ShowSuccess("恢复完成", $"备份已恢复到:\n{RestorePath}");
                
                // 清理临时解密文件
                if (currentFile != EncryptedFilePath && File.Exists(currentFile))
                {
                    try
                    {
                        File.Delete(currentFile);
                    }
                    catch { /* 忽略清理错误 */ }
                }
            }
            else
            {
                // 如果不是 ZIP，直接复制解密后的文件
                var targetPath = Path.Combine(RestorePath, Path.GetFileName(currentFile));
                File.Copy(currentFile, targetPath, true);
                _notificationService.ShowSuccess("恢复完成", $"文件已恢复到:\n{targetPath}");
            }

            Progress = 100;
            CurrentStep = "完成";
            StatusMessage = "恢复成功！";
        }
        catch (Exception ex)
        {
            StatusMessage = $"恢复失败: {ex.Message}";
            _notificationService.ShowError("恢复失败", ex.Message);
            System.Diagnostics.Debug.WriteLine($"恢复失败: {ex}");
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelRestore()
    {
        _cts?.Cancel();
        StatusMessage = "操作已取消";
    }

    [RelayCommand]
    private async Task QuickDecryptOnly()
    {
        if (string.IsNullOrEmpty(EncryptedFilePath) || !File.Exists(EncryptedFilePath))
        {
            _notificationService.ShowWarning("提示", "请选择有效的加密文件");
            return;
        }

        if (string.IsNullOrEmpty(DecryptionPassword))
        {
            _notificationService.ShowWarning("提示", "请输入解密密码");
            return;
        }

        IsProcessing = true;
        Progress = 0;
        CurrentStep = "仅解密";
        _cts = new CancellationTokenSource();

        try
        {
            var decryptProgress = new Progress<double>(p =>
            {
                Progress = p;
                StatusMessage = $"解密中: {p:F1}%";
            });

            var decryptedFile = await _encryptionService.DecryptFileAsync(
                EncryptedFilePath,
                DecryptionPassword,
                decryptProgress,
                _cts.Token);

            Progress = 100;
            StatusMessage = $"解密完成: {decryptedFile}";
            _notificationService.ShowSuccess("解密完成", 
                $"文件已解密:\n{decryptedFile}\n\n您现在可以手动处理这个文件");
        }
        catch (Exception ex)
        {
            StatusMessage = $"解密失败: {ex.Message}";
            _notificationService.ShowError("解密失败", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }
}
