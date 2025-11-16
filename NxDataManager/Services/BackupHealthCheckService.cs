using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 备份健康检查服务实现
/// </summary>
public class BackupHealthCheckService : IBackupHealthCheckService
{
    private readonly IStorageService _storageService;

    public BackupHealthCheckService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<HealthCheckReport> PerformFullCheckAsync()
    {
        var report = new HealthCheckReport();
        var tasks = await _storageService.LoadBackupTasksAsync();

        report.TotalTasks = tasks.Count;

        foreach (var task in tasks)
        {
            var taskHealth = await CheckTaskHealthAsync(task.Id);
            report.TaskStatuses.Add(taskHealth);

            switch (taskHealth.Level)
            {
                case HealthLevel.Healthy:
                    report.HealthyTasks++;
                    break;
                case HealthLevel.Warning:
                    report.WarningTasks++;
                    break;
                case HealthLevel.Critical:
                    report.CriticalTasks++;
                    break;
            }
        }

        // 计算总体评分
        report.OverallScore = report.TotalTasks > 0
            ? report.TaskStatuses.Average(t => t.Score)
            : 100;

        // 生成建议
        report.Recommendations = await GetRecommendationsAsync();

        return report;
    }

    public async Task<TaskHealthStatus> CheckTaskHealthAsync(Guid taskId)
    {
        var status = new TaskHealthStatus { TaskId = taskId };
        var histories = await _storageService.LoadBackupHistoriesAsync(taskId);

        if (!histories.Any())
        {
            status.Level = HealthLevel.Warning;
            status.Score = 50;
            status.Issues.Add("没有备份历史记录");
            return status;
        }

        var task = (await _storageService.LoadBackupTasksAsync())
            .FirstOrDefault(t => t.Id == taskId);

        if (task != null)
        {
            status.TaskName = task.Name;
        }

        // 检查最后一次成功备份
        var lastSuccess = histories
            .Where(h => h.Status == BackupStatus.Completed)
            .OrderByDescending(h => h.StartTime)
            .FirstOrDefault();

        if (lastSuccess != null)
        {
            status.LastSuccessfulBackup = lastSuccess.StartTime;

            // 检查备份时效性
            var daysSinceLastSuccess = (DateTime.Now - lastSuccess.StartTime).TotalDays;

            if (daysSinceLastSuccess > 7)
            {
                status.Issues.Add($"最后一次成功备份已过去 {daysSinceLastSuccess:F0} 天");
                status.Level = HealthLevel.Critical;
            }
            else if (daysSinceLastSuccess > 3)
            {
                status.Issues.Add($"最后一次成功备份已过去 {daysSinceLastSuccess:F0} 天");
                status.Level = HealthLevel.Warning;
            }
        }
        else
        {
            status.Issues.Add("从未成功备份");
            status.Level = HealthLevel.Critical;
        }

        // 检查连续失败次数
        var recentHistories = histories.OrderByDescending(h => h.StartTime).Take(10).ToList();
        status.ConsecutiveFailures = 0;

        foreach (var history in recentHistories)
        {
            if (history.Status == BackupStatus.Failed)
                status.ConsecutiveFailures++;
            else if (history.Status == BackupStatus.Completed)
                break;
        }

        if (status.ConsecutiveFailures >= 5)
        {
            status.Issues.Add($"连续失败 {status.ConsecutiveFailures} 次");
            status.Level = HealthLevel.Critical;
        }
        else if (status.ConsecutiveFailures >= 3)
        {
            status.Issues.Add($"连续失败 {status.ConsecutiveFailures} 次");
            status.Level = HealthLevel.Warning;
        }

        // 计算成功率
        var successCount = recentHistories.Count(h => h.Status == BackupStatus.Completed);
        status.AverageSuccessRate = recentHistories.Count > 0
            ? (double)successCount / recentHistories.Count * 100
            : 0;

        if (status.AverageSuccessRate < 50)
        {
            status.Issues.Add($"成功率仅 {status.AverageSuccessRate:F0}%");
            status.Level = HealthLevel.Critical;
        }
        else if (status.AverageSuccessRate < 80)
        {
            status.Issues.Add($"成功率 {status.AverageSuccessRate:F0}%，需要改善");
            if (status.Level == HealthLevel.Healthy)
                status.Level = HealthLevel.Warning;
        }

        // 检查源路径和目标路径
        if (task != null)
        {
            if (!Directory.Exists(task.SourcePath))
            {
                status.Issues.Add("源路径不存在或无法访问");
                status.Level = HealthLevel.Critical;
            }

            if (!Directory.Exists(task.DestinationPath))
            {
                try
                {
                    Directory.CreateDirectory(task.DestinationPath);
                }
                catch
                {
                    status.Issues.Add("目标路径不存在且无法创建");
                    status.Level = HealthLevel.Critical;
                }
            }
        }

        // 计算评分
        status.Score = 100;
        status.Score -= status.ConsecutiveFailures * 10;
        status.Score -= (100 - status.AverageSuccessRate);
        status.Score = Math.Max(0, status.Score);

        // 如果没有问题，标记为健康
        if (!status.Issues.Any())
        {
            status.Level = HealthLevel.Healthy;
            status.Score = 100;
        }

        return status;
    }

    public async Task<double> GetHealthScoreAsync()
    {
        var report = await PerformFullCheckAsync();
        return report.OverallScore;
    }

    public async Task<List<HealthRecommendation>> GetRecommendationsAsync()
    {
        var recommendations = new List<HealthRecommendation>();
        var tasks = await _storageService.LoadBackupTasksAsync();

        // 检查磁盘空间
        foreach (var task in tasks)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(task.DestinationPath) ?? "C:\\");
                var freeSpacePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;

                if (freeSpacePercent < 10)
                {
                    recommendations.Add(new HealthRecommendation
                    {
                        Category = "存储空间",
                        Issue = $"目标磁盘 {driveInfo.Name} 剩余空间不足 ({freeSpacePercent:F1}%)",
                        Recommendation = "清理磁盘空间或更换更大的存储设备",
                        Priority = HealthLevel.Critical
                    });
                }
                else if (freeSpacePercent < 20)
                {
                    recommendations.Add(new HealthRecommendation
                    {
                        Category = "存储空间",
                        Issue = $"目标磁盘 {driveInfo.Name} 剩余空间较少 ({freeSpacePercent:F1}%)",
                        Recommendation = "考虑清理旧备份或扩展存储空间",
                        Priority = HealthLevel.Warning
                    });
                }
            }
            catch
            {
                // 跳过无法访问的驱动器
            }
        }

        // 检查备份频率
        foreach (var task in tasks)
        {
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            if (histories.Count >= 2)
            {
                var lastTwo = histories.OrderByDescending(h => h.StartTime).Take(2).ToList();
                var interval = (lastTwo[0].StartTime - lastTwo[1].StartTime).TotalDays;

                if (interval > 30)
                {
                    recommendations.Add(new HealthRecommendation
                    {
                        Category = "备份频率",
                        Issue = $"任务 '{task.Name}' 备份间隔过长 ({interval:F0} 天)",
                        Recommendation = "增加备份频率以降低数据丢失风险",
                        Priority = HealthLevel.Warning
                    });
                }
            }
        }

        // 检查是否启用高级功能
        var tasksWithoutEncryption = tasks.Count(t => !t.EnableEncryption);
        if (tasksWithoutEncryption > 0 && tasksWithoutEncryption == tasks.Count)
        {
            recommendations.Add(new HealthRecommendation
            {
                Category = "安全性",
                Issue = "所有任务都未启用加密",
                Recommendation = "对敏感数据启用AES-256加密保护",
                Priority = HealthLevel.Warning
            });
        }

        var tasksWithoutVersionControl = tasks.Count(t => !t.EnableVersionControl);
        if (tasksWithoutVersionControl > 0 && tasksWithoutVersionControl == tasks.Count)
        {
            recommendations.Add(new HealthRecommendation
            {
                Category = "版本控制",
                Issue = "所有任务都未启用版本控制",
                Recommendation = "启用版本控制以保留历史版本",
                Priority = HealthLevel.Warning
            });
        }

        return recommendations;
    }
}
