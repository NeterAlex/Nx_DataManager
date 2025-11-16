using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 文件版本控制服务接口
/// </summary>
public interface IVersionControlService
{
    /// <summary>
    /// 创建文件版本
    /// </summary>
    Task<FileVersion> CreateVersionAsync(string filePath, string comment = "");
    
    /// <summary>
    /// 获取文件所有版本
    /// </summary>
    Task<List<FileVersion>> GetFileVersionsAsync(string filePath);
    
    /// <summary>
    /// 恢复到指定版本
    /// </summary>
    Task<string> RestoreVersionAsync(Guid versionId, string targetPath);
    
    /// <summary>
    /// 删除指定版本
    /// </summary>
    Task DeleteVersionAsync(Guid versionId);
    
    /// <summary>
    /// 清理旧版本（保留最新N个版本）
    /// </summary>
    Task CleanupOldVersionsAsync(string filePath, int keepCount = 5);
    
    /// <summary>
    /// 获取版本差异
    /// </summary>
    Task<VersionDiff> GetVersionDiffAsync(Guid oldVersionId, Guid newVersionId);
}

/// <summary>
/// 文件版本
/// </summary>
public class FileVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFilePath { get; set; } = string.Empty;
    public string VersionFilePath { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public long FileSize { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
}

/// <summary>
/// 版本差异
/// </summary>
public class VersionDiff
{
    public long SizeDifference { get; set; }
    public bool IsIdentical { get; set; }
    public DateTime OldVersionTime { get; set; }
    public DateTime NewVersionTime { get; set; }
}
