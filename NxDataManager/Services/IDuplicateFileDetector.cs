using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 重复文件检测服务接口
/// </summary>
public interface IDuplicateFileDetector
{
    /// <summary>
    /// 扫描目录查找重复文件
    /// </summary>
    Task<DuplicateFileScanResult> ScanForDuplicatesAsync(string directoryPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 比较两个目录找出重复文件
    /// </summary>
    Task<DuplicateFileScanResult> CompareDuplicatesAsync(string path1, string path2, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除重复文件（保留一个）
    /// </summary>
    Task<int> RemoveDuplicatesAsync(DuplicateFileScanResult scanResult, DuplicateRemovalStrategy strategy);
    
    /// <summary>
    /// 创建重复文件的硬链接
    /// </summary>
    Task<int> CreateHardLinksAsync(DuplicateFileScanResult scanResult);
}

/// <summary>
/// 重复文件扫描结果
/// </summary>
public class DuplicateFileScanResult
{
    public Dictionary<string, List<FileHashInfo>> DuplicateGroups { get; set; } = new();
    public long TotalDuplicateFiles { get; set; }
    public long TotalDuplicateSize { get; set; }
    public long PotentialSpaceSaving { get; set; }
    public TimeSpan ScanDuration { get; set; }
}

/// <summary>
/// 文件哈希信息
/// </summary>
public class FileHashInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// 重复文件移除策略
/// </summary>
public enum DuplicateRemovalStrategy
{
    /// <summary>
    /// 保留最旧的文件
    /// </summary>
    KeepOldest,
    
    /// <summary>
    /// 保留最新的文件
    /// </summary>
    KeepNewest,
    
    /// <summary>
    /// 保留路径最短的文件
    /// </summary>
    KeepShortestPath,
    
    /// <summary>
    /// 手动选择
    /// </summary>
    Manual
}
