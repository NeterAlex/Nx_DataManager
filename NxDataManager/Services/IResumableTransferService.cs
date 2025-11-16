using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 断点续传服务接口
/// </summary>
public interface IResumableTransferService
{
    /// <summary>
    /// 开始或继续传输
    /// </summary>
    Task<TransferResult> TransferAsync(string sourcePath, string destinationPath, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 暂停传输（保存断点）
    /// </summary>
    Task PauseTransferAsync(string transferId);
    
    /// <summary>
    /// 恢复传输
    /// </summary>
    Task<TransferResult> ResumeTransferAsync(string transferId, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 取消传输（删除断点）
    /// </summary>
    Task CancelTransferAsync(string transferId);
    
    /// <summary>
    /// 获取传输状态
    /// </summary>
    Task<TransferState?> GetTransferStateAsync(string transferId);
    
    /// <summary>
    /// 清理所有断点记录
    /// </summary>
    Task CleanupAllCheckpointsAsync();
}

/// <summary>
/// 传输结果
/// </summary>
public class TransferResult
{
    public bool Success { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 传输状态
/// </summary>
public class TransferState
{
    public string Id { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Paused, Completed, Failed
}
