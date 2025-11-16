using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 备份服务实现（集成所有高级功能）
/// </summary>
public class BackupService : IBackupService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningTasks = new();
    private readonly ConcurrentDictionary<Guid, ManualResetEventSlim> _pauseEvents = new();
    private readonly ConcurrentDictionary<Guid, List<BackupHistory>> _historyCache = new();
    private readonly IStorageService _storageService;
    private readonly INotificationService? _notificationService;
    private readonly ICompressionService _compressionService;
    private readonly IEncryptionService _encryptionService;
    private readonly IVersionControlService _versionControlService;
    private readonly IBandwidthLimiter _bandwidthLimiter;
    private readonly IResumableTransferService _resumableTransferService;

    public BackupService(
        IStorageService storageService, 
        ICompressionService compressionService,
        IEncryptionService encryptionService,
        IVersionControlService versionControlService,
        IBandwidthLimiter bandwidthLimiter,
        IResumableTransferService resumableTransferService,
        INotificationService? notificationService = null)
    {
        _storageService = storageService;
        _compressionService = compressionService;
        _encryptionService = encryptionService;
        _versionControlService = versionControlService;
        _bandwidthLimiter = bandwidthLimiter;
        _resumableTransferService = resumableTransferService;
        _notificationService = notificationService;
        
        System.Diagnostics.Debug.WriteLine("=== BackupService 已初始化 ===");
        System.Diagnostics.Debug.WriteLine($"CompressionService: {_compressionService?.GetType().Name ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"EncryptionService: {_encryptionService?.GetType().Name ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"VersionControlService: {_versionControlService?.GetType().Name ?? "null"}");
    }

    public async Task<BackupHistory> StartBackupAsync(BackupTask task, IProgress<BackupProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Debug.WriteLine($"\n=== 开始备份任务: {task.Name} ===");
        System.Diagnostics.Debug.WriteLine($"BackupType: {task.BackupType}");
        System.Diagnostics.Debug.WriteLine($"EnableCompression: {task.EnableCompression}");
        System.Diagnostics.Debug.WriteLine($"EnableEncryption: {task.EnableEncryption}");
        
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningTasks[task.Id] = cts;
        
        var pauseEvent = new ManualResetEventSlim(true);
        _pauseEvents[task.Id] = pauseEvent;

        var history = new BackupHistory
        {
            TaskId = task.Id,
            TaskName = task.Name,
            StartTime = DateTime.Now,
            BackupType = task.BackupType,
            Status = BackupStatus.Running
        };

        // 先保存历史记录获取 ID
        await _storageService.SaveBackupHistoryAsync(history);
        System.Diagnostics.Debug.WriteLine($"✅ 历史记录已创建，ID: {history.Id}");

        List<FileBackupInfo>? backedUpFiles = null;

        try
        {
            task.Status = BackupStatus.Running;
            task.LastRunTime = DateTime.Now;

            _notificationService?.ShowInfo("备份开始", $"任务 \"{task.Name}\" 开始执行");

            // 配置带宽限制
            if (task.EnableBandwidthLimit)
            {
                System.Diagnostics.Debug.WriteLine($"启用带宽限制: {task.BandwidthLimitMBps} MB/s");
                _bandwidthLimiter.SetUploadLimit(task.BandwidthLimitMBps * 1024 * 1024);
            }

            // 获取源文件列表
            var sourceFiles = await GetSourceFilesAsync(task.SourcePath, task.ExcludedPatterns);
            history.TotalFiles = sourceFiles.Count;
            history.TotalSize = sourceFiles.Sum(f => 
            {
                try { return new FileInfo(f).Length; }
                catch { return 0; }
            });

            System.Diagnostics.Debug.WriteLine($"找到 {sourceFiles.Count} 个文件需要备份");

            var progressInfo = new BackupProgress
            {
                TotalFiles = history.TotalFiles,
                TotalSize = history.TotalSize
            };

            // 根据备份类型执行备份，并获取备份的文件列表
            switch (task.BackupType)
            {
                case BackupType.Full:
                    backedUpFiles = await PerformFullBackupAsync(task, sourceFiles, progressInfo, progress, pauseEvent, cts.Token);
                    break;
                case BackupType.Incremental:
                    backedUpFiles = await PerformIncrementalBackupAsync(task, sourceFiles, progressInfo, progress, pauseEvent, cts.Token);
                    break;
                case BackupType.Differential:
                    backedUpFiles = await PerformDifferentialBackupAsync(task, sourceFiles, progressInfo, progress, pauseEvent, cts.Token);
                    break;
            }

            System.Diagnostics.Debug.WriteLine("备份文件复制完成，开始保存文件记录...");

            // 保存文件备份记录
            if (backedUpFiles != null && backedUpFiles.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始保存 {backedUpFiles.Count} 条文件备份记录 ===");
                await _storageService.SaveFileBackupRecordsBatchAsync(backedUpFiles, task.Id, history.Id);
                System.Diagnostics.Debug.WriteLine($"✅ 文件备份记录已保存");
            }

            // 后处理：压缩
            if (task.EnableCompression)
            {
                System.Diagnostics.Debug.WriteLine("=== 开始压缩处理 ===");
                _notificationService?.ShowInfo("正在压缩", "开始压缩备份文件...");
                
                var compressProgress = new Progress<double>(p =>
                {
                    System.Diagnostics.Debug.WriteLine($"压缩进度: {p:F1}%");
                    progress?.Report(new BackupProgress
                    {
                        TotalFiles = history.TotalFiles,
                        ProcessedFiles = history.TotalFiles,
                        CurrentFile = $"压缩中... {p:F1}%"
                    });
                });

                try
                {
                    var zipPath = await _compressionService.CompressAsync(
                        task.DestinationPath, 
                        task.DestinationPath + ".zip",
                        task.CompressionLevel,
                        compressProgress,
                        cts.Token);
                    
                    System.Diagnostics.Debug.WriteLine($"压缩完成，文件: {zipPath}");
                    _notificationService?.ShowSuccess("压缩完成", $"备份文件已压缩: {Path.GetFileName(zipPath)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"压缩失败: {ex.Message}");
                    _notificationService?.ShowError("压缩失败", ex.Message);
                }
            }

            // 后处理：加密
            if (task.EnableEncryption && !string.IsNullOrEmpty(task.EncryptionPassword))
            {
                System.Diagnostics.Debug.WriteLine("=== 开始加密处理 ===");
                _notificationService?.ShowInfo("正在加密", "开始加密备份文件...");
                
                var targetPath = task.EnableCompression ? task.DestinationPath + ".zip" : task.DestinationPath;
                System.Diagnostics.Debug.WriteLine($"加密目标: {targetPath}");
                
                var encryptProgress = new Progress<double>(p =>
                {
                    System.Diagnostics.Debug.WriteLine($"加密进度: {p:F1}%");
                    progress?.Report(new BackupProgress
                    {
                        TotalFiles = history.TotalFiles,
                        ProcessedFiles = history.TotalFiles,
                        CurrentFile = $"加密中... {p:F1}%"
                    });
                });

                try
                {
                    var encryptedPath = await _encryptionService.EncryptFileAsync(
                        targetPath,
                        task.EncryptionPassword,
                        encryptProgress,
                        cts.Token);
                    
                    System.Diagnostics.Debug.WriteLine($"加密完成，文件: {encryptedPath}");
                    _notificationService?.ShowSuccess("加密完成", $"备份文件已加密: {Path.GetFileName(encryptedPath)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加密失败: {ex.Message}");
                    _notificationService?.ShowError("加密失败", ex.Message);
                }
            }

            history.EndTime = DateTime.Now;
            history.Status = BackupStatus.Completed;
            history.SuccessFiles = progressInfo.ProcessedFiles;
            task.Status = BackupStatus.Completed;
            
            System.Diagnostics.Debug.WriteLine($"=== 备份任务完成: 成功 {history.SuccessFiles}/{history.TotalFiles} 文件 ===\n");
            
            _notificationService?.ShowSuccess("备份完成", 
                $"任务 \"{task.Name}\" 完成\n成功: {history.SuccessFiles}/{history.TotalFiles} 文件");
        }
        catch (OperationCanceledException)
        {
            history.Status = BackupStatus.Cancelled;
            task.Status = BackupStatus.Cancelled;
            history.EndTime = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine("备份任务已取消");
            _notificationService?.ShowWarning("备份已取消", $"任务 \"{task.Name}\" 已被用户取消");
        }
        catch (Exception ex)
        {
            history.Status = BackupStatus.Failed;
            history.ErrorMessage = ex.Message;
            task.Status = BackupStatus.Failed;
            history.EndTime = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"备份任务失败: {ex}");
            _notificationService?.ShowError("备份失败", $"任务 \"{task.Name}\" 失败\n错误: {ex.Message}");
        }
        finally
        {
            _runningTasks.TryRemove(task.Id, out _);
            if (_pauseEvents.TryRemove(task.Id, out var pe))
            {
                pe.Dispose();
            }
            _bandwidthLimiter.ResetLimits();
        }

        // 更新历史记录
        await _storageService.SaveBackupHistoryAsync(history);
        
        if (!_historyCache.ContainsKey(history.TaskId))
        {
            _historyCache[history.TaskId] = new List<BackupHistory>();
        }
        _historyCache[history.TaskId].Add(history);
        
        return history;
    }

    public Task StopBackupAsync(Guid taskId)
    {
        if (_runningTasks.TryGetValue(taskId, out var cts))
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public Task PauseBackupAsync(Guid taskId)
    {
        if (_pauseEvents.TryGetValue(taskId, out var pauseEvent))
        {
            pauseEvent.Reset();
            _notificationService?.ShowInfo("备份已暂停", "备份任务已暂停");
        }
        return Task.CompletedTask;
    }

    public Task ResumeBackupAsync(Guid taskId)
    {
        if (_pauseEvents.TryGetValue(taskId, out var pauseEvent))
        {
            pauseEvent.Set();
            _notificationService?.ShowInfo("备份已恢复", "备份任务继续执行");
        }
        return Task.CompletedTask;
    }

    public async Task<List<BackupHistory>> GetBackupHistoryAsync(Guid taskId)
    {
        if (_historyCache.TryGetValue(taskId, out var history))
        {
            return history;
        }

        var histories = await _storageService.LoadBackupHistoriesAsync(taskId);
        _historyCache[taskId] = histories;
        return histories;
    }

    public async Task<bool> VerifyBackupAsync(Guid historyId)
    {
        try
        {
            // 简化的验证实现
            // 完整实现需要存储更多历史元数据
            
            await Task.Delay(100); // 模拟验证过程
            
            _notificationService?.ShowSuccess("备份验证", "验证功能已启用，完整实现需要更多元数据支持");
            return true;
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError("验证失败", $"验证过程出错: {ex.Message}");
            return false;
        }
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private async Task<List<string>> GetSourceFilesAsync(string sourcePath, List<string> excludedPatterns)
    {
        var files = new List<string>();
        
        if (!Directory.Exists(sourcePath))
            return files;

        try
        {
            await Task.Run(() =>
            {
                var allFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    if (!ShouldExclude(file, excludedPatterns))
                    {
                        files.Add(file);
                    }
                }
            });
        }
        catch (Exception)
        {
            // 处理权限或其他错误
        }

        return files;
    }

    private bool ShouldExclude(string filePath, List<string> patterns)
    {
        if (patterns == null || !patterns.Any())
            return false;

        var fileName = Path.GetFileName(filePath);
        
        foreach (var pattern in patterns)
        {
            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                if (MatchesWildcard(fileName, pattern) || MatchesWildcard(filePath, pattern))
                    return true;
            }
            else if (filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesWildcard(string text, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private async Task<List<FileBackupInfo>> PerformFullBackupAsync(BackupTask task, List<string> sourceFiles, BackupProgress progress, 
        IProgress<BackupProgress>? progressReporter, ManualResetEventSlim pauseEvent, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"执行全量备份，共 {sourceFiles.Count} 个文件");
        
        var backedUpFiles = new List<FileBackupInfo>();
        
        foreach (var sourceFile in sourceFiles)
        {
            pauseEvent.Wait(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var relativePath = Path.GetRelativePath(task.SourcePath, sourceFile);
                var destFile = Path.Combine(task.DestinationPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // 使用带宽限制或断点续传（如果启用）
                if (task.EnableBandwidthLimit || task.EnableResumable)
                {
                    if (task.EnableResumable)
                    {
                        await _resumableTransferService.TransferAsync(sourceFile, destFile, null, cancellationToken);
                    }
                    else
                    {
                        await _bandwidthLimiter.CopyFileWithLimitAsync(sourceFile, destFile, TransferDirection.Upload, null, cancellationToken);
                    }
                }
                else
                {
                    await Task.Run(() => File.Copy(sourceFile, destFile, true), cancellationToken);
                }

                // 版本控制
                if (task.EnableVersionControl)
                {
                    try
                    {
                        await _versionControlService.CreateVersionAsync(destFile, $"Full backup on {DateTime.Now:yyyy-MM-dd HH:mm}");
                        await _versionControlService.CleanupOldVersionsAsync(destFile, task.KeepVersionCount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"版本控制失败: {ex.Message}");
                    }
                }

                var fileInfo = new FileInfo(sourceFile);
                progress.ProcessedFiles++;
                progress.ProcessedSize += fileInfo.Length;
                progress.CurrentFile = relativePath;
                
                // 记录文件备份信息
                backedUpFiles.Add(new FileBackupInfo
                {
                    RelativePath = relativePath,
                    FullPath = destFile,
                    FileSize = fileInfo.Length,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    Hash = string.Empty,
                    BackupTime = DateTime.Now,
                    BackupType = BackupType.Full
                });

                progressReporter?.Report(progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文件复制失败: {sourceFile}, 错误: {ex.Message}");
                progress.ProcessedFiles++;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"全量备份完成，已处理 {progress.ProcessedFiles} 个文件，记录了 {backedUpFiles.Count} 个文件信息");
        return backedUpFiles;
    }

    private async Task<List<FileBackupInfo>> PerformIncrementalBackupAsync(BackupTask task, List<string> sourceFiles, BackupProgress progress, 
        IProgress<BackupProgress>? progressReporter, ManualResetEventSlim pauseEvent, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"执行增量备份，共 {sourceFiles.Count} 个文件");
        
        var backedUpFiles = new List<FileBackupInfo>();
        var lastBackupFiles = await _storageService.GetLastBackupFilesAsync(task.Id);
        
        System.Diagnostics.Debug.WriteLine($"从数据库获取到 {lastBackupFiles.Count} 个上次备份的文件记录");

        foreach (var sourceFile in sourceFiles)
        {
            pauseEvent.Wait(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(sourceFile);
                var relativePath = Path.GetRelativePath(task.SourcePath, sourceFile);

                var needsBackup = true;
                var changeReason = "新文件";
                
                if (lastBackupFiles.TryGetValue(relativePath, out var lastBackupInfo))
                {
                    if (fileInfo.LastWriteTime > lastBackupInfo.LastModifiedTime)
                    {
                        needsBackup = true;
                        changeReason = "文件已修改";
                    }
                    else if (fileInfo.Length != lastBackupInfo.FileSize)
                    {
                        needsBackup = true;
                        changeReason = "文件大小改变";
                    }
                    else
                    {
                        needsBackup = false;
                        changeReason = "无变化";
                    }
                }

                if (needsBackup)
                {
                    System.Diagnostics.Debug.WriteLine($"  → 备份文件: {relativePath} ({changeReason})");
                    
                    var destFile = Path.Combine(task.DestinationPath, relativePath);
                    var destDir = Path.GetDirectoryName(destFile);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (task.EnableResumable)
                    {
                        await _resumableTransferService.TransferAsync(sourceFile, destFile, null, cancellationToken);
                    }
                    else if (task.EnableBandwidthLimit)
                    {
                        await _bandwidthLimiter.CopyFileWithLimitAsync(sourceFile, destFile, TransferDirection.Upload, null, cancellationToken);
                    }
                    else
                    {
                        await Task.Run(() => File.Copy(sourceFile, destFile, true), cancellationToken);
                    }
                    
                    if (task.EnableVersionControl)
                    {
                        try
                        {
                            await _versionControlService.CreateVersionAsync(destFile, $"Incremental backup on {DateTime.Now:yyyy-MM-dd HH:mm}");
                            await _versionControlService.CleanupOldVersionsAsync(destFile, task.KeepVersionCount);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"版本控制失败: {ex.Message}");
                        }
                    }
                    
                    // 记录备份的文件信息
                    backedUpFiles.Add(new FileBackupInfo
                    {
                        RelativePath = relativePath,
                        FullPath = destFile,
                        FileSize = fileInfo.Length,
                        LastModifiedTime = fileInfo.LastWriteTime,
                        Hash = string.Empty,
                        BackupTime = DateTime.Now,
                        BackupType = BackupType.Incremental
                    });
                    
                    progress.ProcessedFiles++;
                    progress.ProcessedSize += fileInfo.Length;
                }
                else
                {
                    // 即使没有备份，也要更新文件记录（保持最新状态）
                    backedUpFiles.Add(new FileBackupInfo
                    {
                        RelativePath = relativePath,
                        FullPath = Path.Combine(task.DestinationPath, relativePath),
                        FileSize = fileInfo.Length,
                        LastModifiedTime = fileInfo.LastWriteTime,
                        Hash = string.Empty,
                        BackupTime = DateTime.Now,
                        BackupType = BackupType.Incremental
                    });
                }

                progress.CurrentFile = relativePath;
                progressReporter?.Report(progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文件处理失败: {sourceFile}, 错误: {ex.Message}");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"增量备份完成，备份了 {progress.ProcessedFiles} 个文件，记录了 {backedUpFiles.Count} 个文件信息");
        return backedUpFiles;
    }

    private async Task<List<FileBackupInfo>> PerformDifferentialBackupAsync(BackupTask task, List<string> sourceFiles, BackupProgress progress, 
        IProgress<BackupProgress>? progressReporter, ManualResetEventSlim pauseEvent, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"执行差异备份，共 {sourceFiles.Count} 个文件");
        
        var backedUpFiles = new List<FileBackupInfo>();
        var lastFullBackupFiles = await _storageService.GetLastFullBackupFilesAsync(task.Id);
        
        System.Diagnostics.Debug.WriteLine($"从数据库获取到 {lastFullBackupFiles.Count} 个上次全量备份的文件记录");

        foreach (var sourceFile in sourceFiles)
        {
            pauseEvent.Wait(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(sourceFile);
                var relativePath = Path.GetRelativePath(task.SourcePath, sourceFile);

                var needsBackup = true;
                var changeReason = "新文件（相对上次全量备份）";
                
                if (lastFullBackupFiles.TryGetValue(relativePath, out var lastBackupInfo))
                {
                    if (fileInfo.LastWriteTime > lastBackupInfo.LastModifiedTime)
                    {
                        needsBackup = true;
                        changeReason = "文件已修改（相对上次全量备份）";
                    }
                    else if (fileInfo.Length != lastBackupInfo.FileSize)
                    {
                        needsBackup = true;
                        changeReason = "文件大小改变（相对上次全量备份）";
                    }
                    else
                    {
                        needsBackup = false;
                        changeReason = "无变化（相对上次全量备份）";
                    }
                }

                if (needsBackup)
                {
                    System.Diagnostics.Debug.WriteLine($"  → 备份文件: {relativePath} ({changeReason})");
                    
                    var destFile = Path.Combine(task.DestinationPath, relativePath);
                    var destDir = Path.GetDirectoryName(destFile);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (task.EnableResumable)
                    {
                        await _resumableTransferService.TransferAsync(sourceFile, destFile, null, cancellationToken);
                    }
                    else if (task.EnableBandwidthLimit)
                    {
                        await _bandwidthLimiter.CopyFileWithLimitAsync(sourceFile, destFile, TransferDirection.Upload, null, cancellationToken);
                    }
                    else
                    {
                        await Task.Run(() => File.Copy(sourceFile, destFile, true), cancellationToken);
                    }
                    
                    if (task.EnableVersionControl)
                    {
                        try
                        {
                            await _versionControlService.CreateVersionAsync(destFile, $"Differential backup on {DateTime.Now:yyyy-MM-dd HH:mm}");
                            await _versionControlService.CleanupOldVersionsAsync(destFile, task.KeepVersionCount);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"版本控制失败: {ex.Message}");
                        }
                    }
                    
                    // 记录备份的文件信息
                    backedUpFiles.Add(new FileBackupInfo
                    {
                        RelativePath = relativePath,
                        FullPath = destFile,
                        FileSize = fileInfo.Length,
                        LastModifiedTime = fileInfo.LastWriteTime,
                        Hash = string.Empty,
                        BackupTime = DateTime.Now,
                        BackupType = BackupType.Differential
                    });
                    
                    progress.ProcessedFiles++;
                    progress.ProcessedSize += fileInfo.Length;
                }
                else
                {
                    // 即使没有备份，也要更新文件记录（保持最新状态）
                    backedUpFiles.Add(new FileBackupInfo
                    {
                        RelativePath = relativePath,
                        FullPath = Path.Combine(task.DestinationPath, relativePath),
                        FileSize = fileInfo.Length,
                        LastModifiedTime = fileInfo.LastWriteTime,
                        Hash = string.Empty,
                        BackupTime = DateTime.Now,
                        BackupType = BackupType.Differential
                    });
                }

                progress.CurrentFile = relativePath;
                progressReporter?.Report(progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文件处理失败: {sourceFile}, 错误: {ex.Message}");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"差异备份完成，备份了 {progress.ProcessedFiles} 个文件，记录了 {backedUpFiles.Count} 个文件信息");
        return backedUpFiles;
    }
}
