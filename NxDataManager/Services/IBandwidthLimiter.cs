using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 带宽限制服务接口
/// </summary>
public interface IBandwidthLimiter
{
    /// <summary>
    /// 设置上传速度限制（字节/秒）
    /// </summary>
    void SetUploadLimit(long bytesPerSecond);
    
    /// <summary>
    /// 设置下载速度限制（字节/秒）
    /// </summary>
    void SetDownloadLimit(long bytesPerSecond);
    
    /// <summary>
    /// 获取当前上传速度
    /// </summary>
    long GetCurrentUploadSpeed();
    
    /// <summary>
    /// 获取当前下载速度
    /// </summary>
    long GetCurrentDownloadSpeed();
    
    /// <summary>
    /// 限速复制文件
    /// </summary>
    Task CopyFileWithLimitAsync(string sourcePath, string destinationPath, TransferDirection direction, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 重置所有限制
    /// </summary>
    void ResetLimits();
}

/// <summary>
/// 传输方向
/// </summary>
public enum TransferDirection
{
    Upload,
    Download
}

/// <summary>
/// 传输进度
/// </summary>
public class TransferProgress
{
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public double Percentage => TotalBytes > 0 ? (double)TransferredBytes / TotalBytes * 100 : 0;
    public long CurrentSpeed { get; set; } // 字节/秒
    public TimeSpan EstimatedTimeRemaining { get; set; }
}
