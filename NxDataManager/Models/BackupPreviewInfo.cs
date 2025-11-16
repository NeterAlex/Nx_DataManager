using System;
using System.Collections.Generic;

namespace NxDataManager.Models;

/// <summary>
/// 备份预览信息
/// </summary>
public class BackupPreviewInfo
{
    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName { get; set; } = string.Empty;
    
    /// <summary>
    /// 备份类型
    /// </summary>
    public BackupType BackupType { get; set; }
    
    /// <summary>
    /// 源路径
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标路径
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 需要备份的文件列表
    /// </summary>
    public List<FilePreviewItem> FilesToBackup { get; set; } = new();
    
    /// <summary>
    /// 跳过的文件列表
    /// </summary>
    public List<FilePreviewItem> FilesToSkip { get; set; } = new();
    
    /// <summary>
    /// 需要备份的文件数量
    /// </summary>
    public int TotalFilesToBackup => FilesToBackup.Count;
    
    /// <summary>
    /// 跳过的文件数量
    /// </summary>
    public int TotalFilesToSkip => FilesToSkip.Count;
    
    /// <summary>
    /// 需要备份的总大小
    /// </summary>
    public long TotalSizeToBackup { get; set; }
    
    /// <summary>
    /// 跳过的总大小
    /// </summary>
    public long TotalSizeToSkip { get; set; }
}

/// <summary>
/// 文件预览项
/// </summary>
public class FilePreviewItem
{
    /// <summary>
    /// 相对路径
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// 文件大小（格式化）
    /// </summary>
    public string FileSizeFormatted => FormatFileSize(FileSize);
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModifiedTime { get; set; }
    
    /// <summary>
    /// 变化原因
    /// </summary>
    public string ChangeReason { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件状态图标
    /// </summary>
    public string StatusIcon { get; set; } = "📄";
    
    private static string FormatFileSize(long bytes)
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
