using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 断点续传服务实现
/// </summary>
public class ResumableTransferService : IResumableTransferService
{
    private readonly string _checkpointPath;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTransfers = new();
    private readonly int _bufferSize = 81920; // 80KB
    private readonly int _checkpointInterval = 5242880; // 5MB - 每5MB保存一次断点

    public ResumableTransferService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _checkpointPath = Path.Combine(appData, "NxDataManager", "Checkpoints");
        Directory.CreateDirectory(_checkpointPath);
    }

    public async Task<TransferResult> TransferAsync(string sourcePath, string destinationPath, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var transferId = GenerateTransferId(sourcePath, destinationPath);
        
        // 检查是否有现有的断点
        var existingState = await GetTransferStateAsync(transferId);
        if (existingState != null && existingState.Status == "Paused")
        {
            return await ResumeTransferAsync(transferId, progress, cancellationToken);
        }

        return await PerformTransferAsync(transferId, sourcePath, destinationPath, 0, progress, cancellationToken);
    }

    public async Task<TransferResult> ResumeTransferAsync(string transferId, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var state = await GetTransferStateAsync(transferId);
        if (state == null)
        {
            throw new InvalidOperationException("传输状态不存在");
        }

        return await PerformTransferAsync(transferId, state.SourcePath, state.DestinationPath, state.TransferredBytes, progress, cancellationToken);
    }

    public async Task PauseTransferAsync(string transferId)
    {
        if (_activeTransfers.TryRemove(transferId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        var state = await GetTransferStateAsync(transferId);
        if (state != null)
        {
            state.Status = "Paused";
            await SaveTransferStateAsync(state);
        }
    }

    public async Task CancelTransferAsync(string transferId)
    {
        if (_activeTransfers.TryRemove(transferId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        // 删除断点文件
        var checkpointFile = GetCheckpointFilePath(transferId);
        if (File.Exists(checkpointFile))
        {
            File.Delete(checkpointFile);
        }
    }

    public async Task<TransferState?> GetTransferStateAsync(string transferId)
    {
        var checkpointFile = GetCheckpointFilePath(transferId);
        if (!File.Exists(checkpointFile))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(checkpointFile);
            return JsonSerializer.Deserialize<TransferState>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task CleanupAllCheckpointsAsync()
    {
        var files = Directory.GetFiles(_checkpointPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        await Task.CompletedTask;
    }

    private async Task<TransferResult> PerformTransferAsync(string transferId, string sourcePath, string destinationPath, long startPosition, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        var result = new TransferResult
        {
            TransferId = transferId
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeTransfers[transferId] = cts;

            var fileInfo = new FileInfo(sourcePath);
            result.TotalBytes = fileInfo.Length;

            var state = new TransferState
            {
                Id = transferId,
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                TotalBytes = result.TotalBytes,
                TransferredBytes = startPosition,
                Status = "InProgress"
            };

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var destinationStream = new FileStream(destinationPath, 
                startPosition > 0 ? FileMode.Append : FileMode.Create, 
                FileAccess.Write, FileShare.None);

            // 移动到断点位置
            if (startPosition > 0)
            {
                sourceStream.Seek(startPosition, SeekOrigin.Begin);
            }

            var buffer = new byte[_bufferSize];
            var transferredBytes = startPosition;
            var lastCheckpointBytes = startPosition;

            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();

                var bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0)
                    break;

                await destinationStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                transferredBytes += bytesRead;

                // 定期保存断点
                if (transferredBytes - lastCheckpointBytes >= _checkpointInterval)
                {
                    state.TransferredBytes = transferredBytes;
                    state.LastUpdateTime = DateTime.Now;
                    await SaveTransferStateAsync(state);
                    lastCheckpointBytes = transferredBytes;
                }

                // 报告进度
                progress?.Report(new TransferProgress
                {
                    TotalBytes = result.TotalBytes,
                    TransferredBytes = transferredBytes,
                    CurrentSpeed = (long)(transferredBytes / stopwatch.Elapsed.TotalSeconds)
                });
            }

            result.Success = true;
            result.TransferredBytes = transferredBytes;
            
            state.Status = "Completed";
            state.TransferredBytes = transferredBytes;
            await SaveTransferStateAsync(state);

            // 验证传输完整性
            if (transferredBytes == result.TotalBytes)
            {
                // 可选：计算校验和验证
                await DeleteCheckpointAsync(transferId);
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "传输被取消";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            _activeTransfers.TryRemove(transferId, out _);
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task SaveTransferStateAsync(TransferState state)
    {
        var checkpointFile = GetCheckpointFilePath(state.Id);
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(checkpointFile, json);
    }

    private async Task DeleteCheckpointAsync(string transferId)
    {
        var checkpointFile = GetCheckpointFilePath(transferId);
        if (File.Exists(checkpointFile))
        {
            File.Delete(checkpointFile);
        }

        await Task.CompletedTask;
    }

    private string GetCheckpointFilePath(string transferId)
    {
        return Path.Combine(_checkpointPath, $"{transferId}.json");
    }

    private string GenerateTransferId(string sourcePath, string destinationPath)
    {
        var combined = $"{sourcePath}|{destinationPath}";
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
