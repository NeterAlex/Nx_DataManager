using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 智能备份策略服务实现
/// </summary>
public class SmartBackupStrategy : ISmartBackupStrategy
{
    private readonly IStorageService _storageService;

    public SmartBackupStrategy(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<FileChangePattern> AnalyzeFileChangePatternsAsync(string directoryPath, TimeSpan analysisWindow)
    {
        var pattern = new FileChangePattern();

        if (!Directory.Exists(directoryPath))
            return pattern;

        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        var now = DateTime.Now;
        var cutoffTime = now - analysisWindow;

        var recentChanges = new List<FileInfo>();
        var changesByHour = new Dictionary<int, int>();
        var changesByType = new Dictionary<string, int>();

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.LastWriteTime >= cutoffTime)
                {
                    recentChanges.Add(fileInfo);

                    // 统计按小时的变化
                    var hour = fileInfo.LastWriteTime.Hour;
                    changesByHour[hour] = changesByHour.GetValueOrDefault(hour) + 1;

                    // 统计按类型的变化
                    var extension = fileInfo.Extension.ToLowerInvariant();
                    changesByType[extension] = changesByType.GetValueOrDefault(extension) + 1;
                }
            }
            catch
            {
                // 跳过无法访问的文件
            }
        }

        // 计算平均变化率
        var days = analysisWindow.TotalDays;
        pattern.AverageChangeRate = days > 0 ? recentChanges.Count / days : 0;
        pattern.AverageChangeSize = days > 0 ? recentChanges.Sum(f => f.Length) / days : 0;

        // 找出高峰时段（变化最频繁的前3个小时）
        pattern.PeakChangeHours = changesByHour
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToList();

        pattern.ChangeFrequencyByType = changesByType;

        // 判断是否稳定（变化率低于每天10个文件）
        pattern.IsStable = pattern.AverageChangeRate < 10;

        await Task.CompletedTask;
        return pattern;
    }

    public async Task<BackupStrategyRecommendation> RecommendStrategyAsync(string directoryPath)
    {
        var recommendation = new BackupStrategyRecommendation();

        // 分析最近7天的文件变化
        var pattern = await AnalyzeFileChangePatternsAsync(directoryPath, TimeSpan.FromDays(7));

        // 根据变化模式推荐策略
        if (pattern.IsStable)
        {
            // 稳定的文件：每周全量备份
            recommendation.RecommendedType = BackupType.Full;
            recommendation.RecommendedSchedule = ScheduleType.Weekly;
            recommendation.RecommendedInterval = TimeSpan.FromDays(7);
            recommendation.Reason = "文件变化较少，适合每周全量备份";
            recommendation.Confidence = 0.85;
        }
        else if (pattern.AverageChangeRate < 50)
        {
            // 中等变化：每日增量 + 每周全量
            recommendation.RecommendedType = BackupType.Incremental;
            recommendation.RecommendedSchedule = ScheduleType.Daily;
            recommendation.RecommendedInterval = TimeSpan.FromDays(1);
            recommendation.Reason = "文件变化适中，建议每日增量备份 + 每周全量备份";
            recommendation.Confidence = 0.90;
        }
        else
        {
            // 频繁变化：每小时增量
            recommendation.RecommendedType = BackupType.Incremental;
            recommendation.RecommendedSchedule = ScheduleType.Interval;
            recommendation.RecommendedInterval = TimeSpan.FromHours(1);
            recommendation.Reason = "文件变化频繁，建议每小时增量备份";
            recommendation.Confidence = 0.80;
        }

        return recommendation;
    }

    public async Task<DateTime> CalculateOptimalBackupTimeAsync(Guid taskId)
    {
        // 获取任务历史
        var histories = await _storageService.LoadBackupHistoriesAsync(taskId);

        if (!histories.Any())
        {
            // 默认凌晨2点
            return DateTime.Today.AddHours(2);
        }

        // 分析历史备份的执行时长
        var avgDuration = histories
            .Where(h => h.EndTime.HasValue)
            .Average(h => (h.EndTime!.Value - h.StartTime).TotalMinutes);

        // 选择非工作时间（凌晨1-5点）且预留足够时间
        var optimalHour = avgDuration > 60 ? 1 : 2; // 如果备份时间超过1小时，从1点开始

        var nextBackup = DateTime.Today.AddHours(optimalHour);
        if (nextBackup <= DateTime.Now)
        {
            nextBackup = nextBackup.AddDays(1);
        }

        return nextBackup;
    }

    public async Task<StorageRequirement> PredictStorageRequirementAsync(BackupTask task, TimeSpan duration)
    {
        var requirement = new StorageRequirement
        {
            PredictionDate = DateTime.Now.Add(duration)
        };

        // 获取当前目录大小
        var currentSize = await CalculateDirectorySizeAsync(task.SourcePath);
        requirement.InitialSize = currentSize;

        // 分析历史增长
        var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);

        if (histories.Count >= 2)
        {
            var orderedHistories = histories.OrderBy(h => h.StartTime).ToList();
            var firstBackup = orderedHistories.First();
            var lastBackup = orderedHistories.Last();

            var timeSpan = (lastBackup.StartTime - firstBackup.StartTime).TotalDays;
            var sizeGrowth = lastBackup.TotalSize - firstBackup.TotalSize;

            if (timeSpan > 0)
            {
                requirement.GrowthRate = sizeGrowth / timeSpan;
                requirement.PredictedSize = currentSize + (long)(requirement.GrowthRate * duration.TotalDays);
                requirement.MaxSize = (long)(requirement.PredictedSize * 1.5); // 留50%余量
            }
        }
        else
        {
            // 没有历史数据，假设每天增长1%
            requirement.GrowthRate = currentSize * 0.01;
            requirement.PredictedSize = currentSize + (long)(requirement.GrowthRate * duration.TotalDays);
            requirement.MaxSize = (long)(requirement.PredictedSize * 1.5);
        }

        // 根据备份类型调整
        requirement.PredictedSize = task.BackupType switch
        {
            BackupType.Full => requirement.PredictedSize * (long)duration.TotalDays, // 全量每次都需要完整空间
            BackupType.Incremental => requirement.PredictedSize, // 增量只需增量空间
            BackupType.Differential => requirement.PredictedSize * 2, // 差异需要基准+差异
            _ => requirement.PredictedSize
        };

        return requirement;
    }

    public async Task<BackupSchedule> AutoAdjustScheduleAsync(Guid taskId)
    {
        var histories = await _storageService.LoadBackupHistoriesAsync(taskId);

        if (histories.Count < 5)
        {
            // 数据不足，返回默认配置
            return new BackupSchedule
            {
                Type = ScheduleType.Daily,
                StartTime = DateTime.Today.AddHours(2)
            };
        }

        // 分析失败率
        var recentHistories = histories.OrderByDescending(h => h.StartTime).Take(10).ToList();
        var failureRate = recentHistories.Count(h => h.Status == BackupStatus.Failed) / (double)recentHistories.Count;

        // 分析平均执行时长
        var avgDuration = recentHistories
            .Where(h => h.EndTime.HasValue)
            .Average(h => (h.EndTime!.Value - h.StartTime).TotalMinutes);

        var schedule = new BackupSchedule();

        // 根据失败率和执行时长调整策略
        if (failureRate > 0.3)
        {
            // 失败率高，降低频率
            schedule.Type = ScheduleType.Weekly;
            schedule.StartTime = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek).AddHours(2);
        }
        else if (avgDuration > 120)
        {
            // 执行时间长，降低频率
            schedule.Type = ScheduleType.Weekly;
            schedule.StartTime = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek).AddHours(1);
        }
        else
        {
            // 正常情况，每日备份
            schedule.Type = ScheduleType.Daily;
            schedule.StartTime = DateTime.Today.AddHours(2);
        }

        return schedule;
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
}
