using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 存储空间分析服务接口
/// </summary>
public interface IStorageAnalysisService
{
    /// <summary>
    /// 分析存储空间使用情况
    /// </summary>
    Task<StorageAnalysisReport> AnalyzeStorageAsync();
    
    /// <summary>
    /// 获取磁盘使用详情
    /// </summary>
    Task<List<DriveUsageInfo>> GetDriveUsageAsync();
    
    /// <summary>
    /// 预测空间使用趋势
    /// </summary>
    Task<StorageTrend> PredictStorageTrendAsync(TimeSpan forecastPeriod);
    
    /// <summary>
    /// 获取清理建议
    /// </summary>
    Task<List<CleanupRecommendation>> GetCleanupRecommendationsAsync();
}

/// <summary>
/// 存储分析报告
/// </summary>
public class StorageAnalysisReport
{
    public DateTime AnalysisTime { get; set; } = DateTime.Now;
    public long TotalBackupSize { get; set; }
    public long TotalAvailableSpace { get; set; }
    public Dictionary<string, long> SizeByBackupType { get; set; } = new();
    public Dictionary<string, long> SizeByDrive { get; set; } = new();
    public List<LargestBackup> LargestBackups { get; set; } = new();
    public double AverageDailyGrowth { get; set; }
}

/// <summary>
/// 驱动器使用信息
/// </summary>
public class DriveUsageInfo
{
    public string DriveName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long UsedSize { get; set; }
    public long FreeSize { get; set; }
    public double UsagePercentage { get; set; }
    public long BackupDataSize { get; set; }
    public int BackupTaskCount { get; set; }
}

/// <summary>
/// 最大备份
/// </summary>
public class LargestBackup
{
    public string TaskName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastBackup { get; set; }
    public int FileCount { get; set; }
}

/// <summary>
/// 存储趋势
/// </summary>
public class StorageTrend
{
    public DateTime ForecastDate { get; set; }
    public long CurrentSize { get; set; }
    public long PredictedSize { get; set; }
    public double GrowthRate { get; set; } // 每天字节数
    public List<DateTime> OutOfSpaceDates { get; set; } = new(); // 预计空间不足的日期
}

/// <summary>
/// 清理建议
/// </summary>
public class CleanupRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PotentialSpaceSaving { get; set; }
    public List<string> AffectedFiles { get; set; } = new();
}

/// <summary>
/// 存储空间分析服务实现
/// </summary>
public class StorageAnalysisService : IStorageAnalysisService
{
    private readonly IStorageService _storageService;

    public StorageAnalysisService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<StorageAnalysisReport> AnalyzeStorageAsync()
    {
        var report = new StorageAnalysisReport();
        var tasks = await _storageService.LoadBackupTasksAsync();

        // 分析每个任务的存储使用
        foreach (var task in tasks)
        {
            if (!Directory.Exists(task.DestinationPath))
                continue;

            var taskSize = await CalculateDirectorySizeAsync(task.DestinationPath);
            report.TotalBackupSize += taskSize;

            // 按备份类型分组
            var typeKey = task.BackupType.ToString();
            report.SizeByBackupType[typeKey] = report.SizeByBackupType.GetValueOrDefault(typeKey) + taskSize;

            // 按驱动器分组
            var drive = Path.GetPathRoot(task.DestinationPath) ?? "Unknown";
            report.SizeByDrive[drive] = report.SizeByDrive.GetValueOrDefault(drive) + taskSize;

            // 记录最大的备份
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            var lastHistory = histories.OrderByDescending(h => h.StartTime).FirstOrDefault();

            report.LargestBackups.Add(new LargestBackup
            {
                TaskName = task.Name,
                Size = taskSize,
                LastBackup = lastHistory?.StartTime ?? DateTime.MinValue,
                FileCount = (int)(lastHistory?.TotalFiles ?? 0)
            });
        }

        // 排序最大备份
        report.LargestBackups = report.LargestBackups
            .OrderByDescending(b => b.Size)
            .Take(10)
            .ToList();

        // 计算平均每日增长
        report.AverageDailyGrowth = await CalculateAverageDailyGrowthAsync(tasks);

        // 计算总可用空间
        var drives = report.SizeByDrive.Keys.Distinct();
        foreach (var drive in drives)
        {
            try
            {
                var driveInfo = new DriveInfo(drive);
                report.TotalAvailableSpace += driveInfo.AvailableFreeSpace;
            }
            catch
            {
                // 跳过无法访问的驱动器
            }
        }

        return report;
    }

    public async Task<List<DriveUsageInfo>> GetDriveUsageAsync()
    {
        var driveUsages = new List<DriveUsageInfo>();
        var tasks = await _storageService.LoadBackupTasksAsync();

        // 获取所有驱动器
        var allDrives = DriveInfo.GetDrives().Where(d => d.IsReady);

        foreach (var drive in allDrives)
        {
            var usage = new DriveUsageInfo
            {
                DriveName = drive.Name,
                TotalSize = drive.TotalSize,
                FreeSize = drive.AvailableFreeSpace,
                UsedSize = drive.TotalSize - drive.AvailableFreeSpace,
                UsagePercentage = (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100
            };

            // 计算该驱动器上的备份数据
            var tasksOnDrive = tasks.Where(t => 
                Path.GetPathRoot(t.DestinationPath)?.Equals(drive.Name, StringComparison.OrdinalIgnoreCase) == true);

            foreach (var task in tasksOnDrive)
            {
                if (Directory.Exists(task.DestinationPath))
                {
                    usage.BackupDataSize += await CalculateDirectorySizeAsync(task.DestinationPath);
                    usage.BackupTaskCount++;
                }
            }

            driveUsages.Add(usage);
        }

        return driveUsages;
    }

    public async Task<StorageTrend> PredictStorageTrendAsync(TimeSpan forecastPeriod)
    {
        var trend = new StorageTrend
        {
            ForecastDate = DateTime.Now.Add(forecastPeriod)
        };

        var report = await AnalyzeStorageAsync();
        trend.CurrentSize = report.TotalBackupSize;
        trend.GrowthRate = report.AverageDailyGrowth;

        // 预测未来大小
        trend.PredictedSize = trend.CurrentSize + (long)(trend.GrowthRate * forecastPeriod.TotalDays);

        // 预测何时空间不足
        var driveUsages = await GetDriveUsageAsync();
        foreach (var drive in driveUsages.Where(d => d.BackupTaskCount > 0))
        {
            var dailyGrowthOnDrive = trend.GrowthRate * ((double)drive.BackupDataSize / trend.CurrentSize);
            var daysUntilFull = dailyGrowthOnDrive > 0 
                ? drive.FreeSize / dailyGrowthOnDrive 
                : double.MaxValue;

            if (daysUntilFull < forecastPeriod.TotalDays)
            {
                trend.OutOfSpaceDates.Add(DateTime.Now.AddDays(daysUntilFull));
            }
        }

        return trend;
    }

    public async Task<List<CleanupRecommendation>> GetCleanupRecommendationsAsync()
    {
        var recommendations = new List<CleanupRecommendation>();
        var tasks = await _storageService.LoadBackupTasksAsync();

        // 推荐1: 删除旧备份
        foreach (var task in tasks)
        {
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            var oldHistories = histories
                .Where(h => (DateTime.Now - h.StartTime).TotalDays > 90)
                .OrderBy(h => h.StartTime)
                .ToList();

            if (oldHistories.Any())
            {
                var potentialSaving = oldHistories.Sum(h => h.TotalSize);
                recommendations.Add(new CleanupRecommendation
                {
                    Category = "旧备份",
                    Description = $"任务 '{task.Name}' 有 {oldHistories.Count} 个超过90天的备份",
                    PotentialSpaceSaving = potentialSaving,
                    AffectedFiles = oldHistories.Select(h => $"{h.StartTime:yyyy-MM-dd} - {h.TotalSize / 1024 / 1024}MB").ToList()
                });
            }
        }

        // 推荐2: 启用压缩
        var uncompressedTasks = tasks.Where(t => !t.EnableCompression).ToList();
        if (uncompressedTasks.Any())
        {
            var potentialSaving = 0L;
            foreach (var task in uncompressedTasks)
            {
                if (Directory.Exists(task.DestinationPath))
                {
                    var size = await CalculateDirectorySizeAsync(task.DestinationPath);
                    potentialSaving += (long)(size * 0.4); // 假设40%压缩率
                }
            }

            recommendations.Add(new CleanupRecommendation
            {
                Category = "压缩",
                Description = $"{uncompressedTasks.Count} 个任务未启用压缩",
                PotentialSpaceSaving = potentialSaving,
                AffectedFiles = uncompressedTasks.Select(t => t.Name).ToList()
            });
        }

        // 推荐3: 删除失败的备份
        foreach (var task in tasks)
        {
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            var failedHistories = histories.Where(h => h.Status == BackupStatus.Failed).ToList();

            if (failedHistories.Any())
            {
                recommendations.Add(new CleanupRecommendation
                {
                    Category = "失败备份",
                    Description = $"任务 '{task.Name}' 有 {failedHistories.Count} 个失败的备份记录",
                    PotentialSpaceSaving = 0, // 通常失败备份不占空间
                    AffectedFiles = failedHistories.Select(h => $"{h.StartTime:yyyy-MM-dd} - {h.ErrorMessage}").ToList()
                });
            }
        }

        return recommendations.OrderByDescending(r => r.PotentialSpaceSaving).ToList();
    }

    private async Task<long> CalculateDirectorySizeAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        long totalSize = 0;

        await Task.Run(() =>
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch
                    {
                        // 跳过无法访问的文件
                    }
                }
            }
            catch
            {
                // 目录访问失败
            }
        });

        return totalSize;
    }

    private async Task<double> CalculateAverageDailyGrowthAsync(List<BackupTask> tasks)
    {
        double totalGrowth = 0;
        int daysAnalyzed = 0;

        foreach (var task in tasks)
        {
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            if (histories.Count < 2)
                continue;

            var orderedHistories = histories.OrderBy(h => h.StartTime).ToList();
            for (int i = 1; i < orderedHistories.Count; i++)
            {
                var prev = orderedHistories[i - 1];
                var current = orderedHistories[i];

                var days = (current.StartTime - prev.StartTime).TotalDays;
                if (days > 0 && days < 365) // 忽略异常值
                {
                    var growth = current.TotalSize - prev.TotalSize;
                    totalGrowth += growth / days;
                    daysAnalyzed++;
                }
            }
        }

        return daysAnalyzed > 0 ? totalGrowth / daysAnalyzed : 0;
    }
}
