using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 带宽限制服务实现
/// </summary>
public class BandwidthLimiter : IBandwidthLimiter
{
    private long _uploadLimitBytesPerSecond;
    private long _downloadLimitBytesPerSecond;
    private long _currentUploadSpeed;
    private long _currentDownloadSpeed;
    private readonly int _bufferSize = 81920; // 80KB

    public void SetUploadLimit(long bytesPerSecond)
    {
        _uploadLimitBytesPerSecond = bytesPerSecond;
    }

    public void SetDownloadLimit(long bytesPerSecond)
    {
        _downloadLimitBytesPerSecond = bytesPerSecond;
    }

    public long GetCurrentUploadSpeed()
    {
        return _currentUploadSpeed;
    }

    public long GetCurrentDownloadSpeed()
    {
        return _currentDownloadSpeed;
    }

    public async Task CopyFileWithLimitAsync(string sourcePath, string destinationPath, TransferDirection direction, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var limitBytesPerSecond = direction == TransferDirection.Upload 
            ? _uploadLimitBytesPerSecond 
            : _downloadLimitBytesPerSecond;

        if (limitBytesPerSecond <= 0)
        {
            // 无限制，直接复制
            await Task.Run(() => File.Copy(sourcePath, destinationPath, true), cancellationToken);
            return;
        }

        var fileInfo = new FileInfo(sourcePath);
        var totalBytes = fileInfo.Length;
        var transferredBytes = 0L;

        var stopwatch = Stopwatch.StartNew();
        var lastReportTime = stopwatch.Elapsed;
        var lastTransferredBytes = 0L;

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[_bufferSize];
        var intervalStopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 读取数据
            var bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0)
                break;

            // 写入数据
            await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            transferredBytes += bytesRead;

            // 计算需要的延迟以满足速度限制
            var elapsedSeconds = intervalStopwatch.Elapsed.TotalSeconds;
            var expectedSeconds = (double)transferredBytes / limitBytesPerSecond;
            
            if (elapsedSeconds < expectedSeconds)
            {
                var delayMilliseconds = (int)((expectedSeconds - elapsedSeconds) * 1000);
                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds, cancellationToken);
                }
            }

            // 更新速度统计（每秒更新一次）
            var currentTime = stopwatch.Elapsed;
            if ((currentTime - lastReportTime).TotalSeconds >= 1.0)
            {
                var bytesInInterval = transferredBytes - lastTransferredBytes;
                var timeInterval = (currentTime - lastReportTime).TotalSeconds;
                var currentSpeed = (long)(bytesInInterval / timeInterval);

                if (direction == TransferDirection.Upload)
                    _currentUploadSpeed = currentSpeed;
                else
                    _currentDownloadSpeed = currentSpeed;

                // 报告进度
                if (progress != null)
                {
                    var remainingBytes = totalBytes - transferredBytes;
                    var estimatedSeconds = currentSpeed > 0 ? remainingBytes / currentSpeed : 0;
                    
                    progress.Report(new TransferProgress
                    {
                        TotalBytes = totalBytes,
                        TransferredBytes = transferredBytes,
                        CurrentSpeed = currentSpeed,
                        EstimatedTimeRemaining = TimeSpan.FromSeconds(estimatedSeconds)
                    });
                }

                lastReportTime = currentTime;
                lastTransferredBytes = transferredBytes;
            }
        }

        stopwatch.Stop();
        
        // 最后一次进度报告
        progress?.Report(new TransferProgress
        {
            TotalBytes = totalBytes,
            TransferredBytes = transferredBytes,
            CurrentSpeed = (long)(totalBytes / stopwatch.Elapsed.TotalSeconds),
            EstimatedTimeRemaining = TimeSpan.Zero
        });
    }

    public void ResetLimits()
    {
        _uploadLimitBytesPerSecond = 0;
        _downloadLimitBytesPerSecond = 0;
        _currentUploadSpeed = 0;
        _currentDownloadSpeed = 0;
    }
}
