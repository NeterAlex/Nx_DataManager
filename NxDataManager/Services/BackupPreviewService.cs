using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 备份预览服务接口
/// </summary>
public interface IBackupPreviewService
{
    /// <summary>
    /// 分析并生成备份预览
    /// </summary>
    Task<BackupPreviewInfo> AnalyzeBackupAsync(BackupTask task);
}

/// <summary>
/// 备份预览服务实现
/// </summary>
public class BackupPreviewService : IBackupPreviewService
{
    private readonly IStorageService _storageService;

    public BackupPreviewService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<BackupPreviewInfo> AnalyzeBackupAsync(BackupTask task)
    {
        var preview = new BackupPreviewInfo
        {
            TaskName = task.Name,
            BackupType = task.BackupType,
            SourcePath = task.SourcePath,
            DestinationPath = task.DestinationPath
        };

        // 获取源文件列表
        var sourceFiles = await GetSourceFilesAsync(task.SourcePath, task.ExcludedPatterns);

        switch (task.BackupType)
        {
            case BackupType.Full:
                await AnalyzeFullBackupAsync(task, sourceFiles, preview);
                break;
            
            case BackupType.Incremental:
                await AnalyzeIncrementalBackupAsync(task, sourceFiles, preview);
                break;
            
            case BackupType.Differential:
                await AnalyzeDifferentialBackupAsync(task, sourceFiles, preview);
                break;
        }

        return preview;
    }

    private async Task AnalyzeFullBackupAsync(BackupTask task, List<string> sourceFiles, BackupPreviewInfo preview)
    {
        // 全量备份：所有文件都需要备份
        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var fileInfo = new FileInfo(sourceFile);
                var relativePath = Path.GetRelativePath(task.SourcePath, sourceFile);

                preview.FilesToBackup.Add(new FilePreviewItem
                {
                    RelativePath = relativePath,
                    FileSize = fileInfo.Length,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    ChangeReason = "全量备份",
                    StatusIcon = "📦"
                });

                preview.TotalSizeToBackup += fileInfo.Length;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分析文件失败: {sourceFile}, 错误: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task AnalyzeIncrementalBackupAsync(BackupTask task, List<string> sourceFiles, BackupPreviewInfo preview)
    {
        // 获取上次备份的文件信息
        var lastBackupFiles = await _storageService.GetLastBackupFilesAsync(task.Id);

        foreach (var sourceFile in sourceFiles)
        {
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

                var item = new FilePreviewItem
                {
                    RelativePath = relativePath,
                    FileSize = fileInfo.Length,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    ChangeReason = changeReason,
                    StatusIcon = needsBackup ? "🔄" : "✅"
                };

                if (needsBackup)
                {
                    preview.FilesToBackup.Add(item);
                    preview.TotalSizeToBackup += fileInfo.Length;
                }
                else
                {
                    preview.FilesToSkip.Add(item);
                    preview.TotalSizeToSkip += fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分析文件失败: {sourceFile}, 错误: {ex.Message}");
            }
        }
    }

    private async Task AnalyzeDifferentialBackupAsync(BackupTask task, List<string> sourceFiles, BackupPreviewInfo preview)
    {
        // 获取上次全量备份的文件信息
        var lastFullBackupFiles = await _storageService.GetLastFullBackupFilesAsync(task.Id);

        foreach (var sourceFile in sourceFiles)
        {
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

                var item = new FilePreviewItem
                {
                    RelativePath = relativePath,
                    FileSize = fileInfo.Length,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    ChangeReason = changeReason,
                    StatusIcon = needsBackup ? "🔀" : "✅"
                };

                if (needsBackup)
                {
                    preview.FilesToBackup.Add(item);
                    preview.TotalSizeToBackup += fileInfo.Length;
                }
                else
                {
                    preview.FilesToSkip.Add(item);
                    preview.TotalSizeToSkip += fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分析文件失败: {sourceFile}, 错误: {ex.Message}");
            }
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取源文件列表失败: {ex.Message}");
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
}
