using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 重复文件检测服务实现
/// </summary>
public class DuplicateFileDetector : IDuplicateFileDetector
{
    public async Task<DuplicateFileScanResult> ScanForDuplicatesAsync(string directoryPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DuplicateFileScanResult();

        // 步骤1: 获取所有文件
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        var totalFiles = files.Length;

        // 步骤2: 按大小分组（快速预筛选）
        var sizeGroups = new Dictionary<long, List<string>>();
        var processedFiles = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(file);
                if (!sizeGroups.ContainsKey(fileInfo.Length))
                {
                    sizeGroups[fileInfo.Length] = new List<string>();
                }
                sizeGroups[fileInfo.Length].Add(file);

                processedFiles++;
                progress?.Report((double)processedFiles / totalFiles * 50); // 50%进度用于大小分组
            }
            catch
            {
                // 跳过无法访问的文件
            }
        }

        // 步骤3: 对大小相同的文件计算哈希
        var hashGroups = new Dictionary<string, List<FileHashInfo>>();
        var filesToHash = sizeGroups.Where(g => g.Value.Count > 1).SelectMany(g => g.Value).ToList();
        var hashedFiles = 0;

        foreach (var file in filesToHash)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(file);
                var hash = await CalculateFileHashAsync(file);

                if (!hashGroups.ContainsKey(hash))
                {
                    hashGroups[hash] = new List<FileHashInfo>();
                }

                hashGroups[hash].Add(new FileHashInfo
                {
                    FilePath = file,
                    Hash = hash,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });

                hashedFiles++;
                progress?.Report(50 + (double)hashedFiles / filesToHash.Count * 50); // 剩余50%进度用于哈希计算
            }
            catch
            {
                // 跳过无法读取的文件
            }
        }

        // 步骤4: 提取重复文件组
        foreach (var group in hashGroups.Where(g => g.Value.Count > 1))
        {
            result.DuplicateGroups[group.Key] = group.Value;
            result.TotalDuplicateFiles += group.Value.Count - 1; // 减1是因为保留一个
            result.TotalDuplicateSize += group.Value[0].FileSize * (group.Value.Count - 1);
        }

        result.PotentialSpaceSaving = result.TotalDuplicateSize;
        result.ScanDuration = stopwatch.Elapsed;

        stopwatch.Stop();
        return result;
    }

    public async Task<DuplicateFileScanResult> CompareDuplicatesAsync(string path1, string path2, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DuplicateFileScanResult();

        // 扫描两个目录
        var files1 = Directory.GetFiles(path1, "*.*", SearchOption.AllDirectories);
        var files2 = Directory.GetFiles(path2, "*.*", SearchOption.AllDirectories);

        var allFiles = files1.Concat(files2).ToList();
        var totalFiles = allFiles.Count;
        var processedFiles = 0;

        // 计算所有文件的哈希
        var hashMap = new Dictionary<string, List<FileHashInfo>>();

        foreach (var file in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(file);
                var hash = await CalculateFileHashAsync(file);

                if (!hashMap.ContainsKey(hash))
                {
                    hashMap[hash] = new List<FileHashInfo>();
                }

                hashMap[hash].Add(new FileHashInfo
                {
                    FilePath = file,
                    Hash = hash,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });

                processedFiles++;
                progress?.Report((double)processedFiles / totalFiles * 100);
            }
            catch
            {
                // 跳过错误
            }
        }

        // 找出跨目录的重复文件
        foreach (var group in hashMap.Where(g => g.Value.Count > 1))
        {
            var hasPath1 = group.Value.Any(f => f.FilePath.StartsWith(path1, StringComparison.OrdinalIgnoreCase));
            var hasPath2 = group.Value.Any(f => f.FilePath.StartsWith(path2, StringComparison.OrdinalIgnoreCase));

            if (hasPath1 && hasPath2)
            {
                result.DuplicateGroups[group.Key] = group.Value;
                result.TotalDuplicateFiles += group.Value.Count - 1;
                result.TotalDuplicateSize += group.Value[0].FileSize * (group.Value.Count - 1);
            }
        }

        result.PotentialSpaceSaving = result.TotalDuplicateSize;
        result.ScanDuration = stopwatch.Elapsed;

        stopwatch.Stop();
        return result;
    }

    public async Task<int> RemoveDuplicatesAsync(DuplicateFileScanResult scanResult, DuplicateRemovalStrategy strategy)
    {
        var removedCount = 0;

        foreach (var group in scanResult.DuplicateGroups.Values)
        {
            if (group.Count < 2)
                continue;

            // 根据策略选择要保留的文件
            FileHashInfo fileToKeep = strategy switch
            {
                DuplicateRemovalStrategy.KeepOldest => group.OrderBy(f => f.LastModified).First(),
                DuplicateRemovalStrategy.KeepNewest => group.OrderByDescending(f => f.LastModified).First(),
                DuplicateRemovalStrategy.KeepShortestPath => group.OrderBy(f => f.FilePath.Length).First(),
                _ => group.First()
            };

            // 删除其他文件
            foreach (var file in group.Where(f => f.FilePath != fileToKeep.FilePath))
            {
                try
                {
                    File.Delete(file.FilePath);
                    removedCount++;
                }
                catch
                {
                    // 记录但继续
                }
            }
        }

        await Task.CompletedTask;
        return removedCount;
    }

    public async Task<int> CreateHardLinksAsync(DuplicateFileScanResult scanResult)
    {
        var linkedCount = 0;

        foreach (var group in scanResult.DuplicateGroups.Values)
        {
            if (group.Count < 2)
                continue;

            var sourceFile = group.First();

            foreach (var targetFile in group.Skip(1))
            {
                try
                {
                    // 删除目标文件
                    File.Delete(targetFile.FilePath);

                    // 创建硬链接 (Windows)
                    var result = CreateHardLink(targetFile.FilePath, sourceFile.FilePath, IntPtr.Zero);
                    if (result)
                    {
                        linkedCount++;
                    }
                }
                catch
                {
                    // 记录但继续
                }
            }
        }

        await Task.CompletedTask;
        return linkedCount;
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    // P/Invoke for CreateHardLink
    [System.Runtime.InteropServices.DllImport("Kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
}
