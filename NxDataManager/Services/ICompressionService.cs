using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 压缩服务接口
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// 压缩文件或文件夹
    /// </summary>
    Task<string> CompressAsync(string sourcePath, string destinationPath, CompressionLevel level = CompressionLevel.Normal, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 解压缩文件
    /// </summary>
    Task ExtractAsync(string archivePath, string destinationPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取压缩文件信息
    /// </summary>
    Task<CompressionInfo> GetArchiveInfoAsync(string archivePath);
}

/// <summary>
/// 压缩级别
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// 不压缩（仅存储）
    /// </summary>
    None,
    
    /// <summary>
    /// 快速压缩
    /// </summary>
    Fast,
    
    /// <summary>
    /// 标准压缩
    /// </summary>
    Normal,
    
    /// <summary>
    /// 最大压缩
    /// </summary>
    Maximum
}

/// <summary>
/// 压缩信息
/// </summary>
public class CompressionInfo
{
    public long CompressedSize { get; set; }
    public long UncompressedSize { get; set; }
    public int FileCount { get; set; }
    public double CompressionRatio => UncompressedSize > 0 ? (double)CompressedSize / UncompressedSize * 100 : 0;
}
