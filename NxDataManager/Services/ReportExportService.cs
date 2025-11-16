using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 报告导出服务实现
/// 注意：PDF和Excel导出需要额外的NuGet包（如iTextSharp或ClosedXML）
/// 这里提供基础的HTML导出和框架实现
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly IStorageService _storageService;
    private readonly IBackupHealthCheckService _healthCheckService;
    private readonly IStorageAnalysisService _storageAnalysisService;

    public ReportExportService(
        IStorageService storageService,
        IBackupHealthCheckService healthCheckService,
        IStorageAnalysisService storageAnalysisService)
    {
        _storageService = storageService;
        _healthCheckService = healthCheckService;
        _storageAnalysisService = storageAnalysisService;
    }

    public async Task<string> ExportToPdfAsync(ReportData report, string outputPath)
    {
        // 完整实现需要使用 iTextSharp 或 PdfSharpCore
        // 这里提供框架实现
        
        await Task.CompletedTask;
        
        // 示例代码（需要 iTextSharp）:
        /*
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);
        
        document.Add(new Paragraph(report.Title));
        document.Add(new Paragraph($"生成日期: {report.GeneratedDate:yyyy-MM-dd HH:mm}"));
        
        foreach (var table in report.Tables)
        {
            var pdfTable = new Table(table.Headers.Count);
            // 添加表头
            foreach (var header in table.Headers)
            {
                pdfTable.AddHeaderCell(header);
            }
            // 添加数据行
            foreach (var row in table.Rows)
            {
                foreach (var cell in row)
                {
                    pdfTable.AddCell(cell);
                }
            }
            document.Add(pdfTable);
        }
        */
        
        throw new NotImplementedException("PDF导出需要安装 iTextSharp 或 PdfSharpCore 包");
    }

    public async Task<string> ExportToExcelAsync(ReportData report, string outputPath)
    {
        // 完整实现需要使用 ClosedXML 或 EPPlus
        // 这里提供框架实现
        
        await Task.CompletedTask;
        
        // 示例代码（需要 ClosedXML）:
        /*
        using var workbook = new XLWorkbook();
        
        foreach (var table in report.Tables)
        {
            var worksheet = workbook.Worksheets.Add(table.Title);
            
            // 添加表头
            for (int i = 0; i < table.Headers.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = table.Headers[i];
            }
            
            // 添加数据行
            for (int row = 0; row < table.Rows.Count; row++)
            {
                for (int col = 0; col < table.Rows[row].Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = table.Rows[row][col];
                }
            }
            
            // 自动调整列宽
            worksheet.Columns().AdjustToContents();
        }
        
        workbook.SaveAs(outputPath);
        */
        
        throw new NotImplementedException("Excel导出需要安装 ClosedXML 或 EPPlus 包");
    }

    public async Task<string> ExportToHtmlAsync(ReportData report, string outputPath)
    {
        var html = new StringBuilder();
        
        // HTML头部
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine($"<title>{report.Title}</title>");
        html.AppendLine("<style>");
        html.AppendLine(@"
            body {
                font-family: 'Segoe UI', Arial, sans-serif;
                margin: 40px;
                background: #f5f5f5;
            }
            .container {
                max-width: 1200px;
                margin: 0 auto;
                background: white;
                padding: 30px;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            }
            h1 {
                color: #333;
                border-bottom: 3px solid #4CAF50;
                padding-bottom: 10px;
            }
            h2 {
                color: #555;
                margin-top: 30px;
            }
            .meta {
                color: #999;
                margin-bottom: 30px;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin: 20px 0;
            }
            th {
                background: #4CAF50;
                color: white;
                padding: 12px;
                text-align: left;
            }
            td {
                padding: 10px;
                border-bottom: 1px solid #ddd;
            }
            tr:hover {
                background: #f9f9f9;
            }
            .chart {
                margin: 20px 0;
                padding: 20px;
                background: #f9f9f9;
                border-radius: 5px;
            }
        ");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");
        
        // 标题和元数据
        html.AppendLine($"<h1>{report.Title}</h1>");
        html.AppendLine($"<div class='meta'>生成日期: {report.GeneratedDate:yyyy-MM-dd HH:mm}</div>");
        
        // 图表（简化显示）
        foreach (var chart in report.Charts)
        {
            html.AppendLine($"<div class='chart'>");
            html.AppendLine($"<h2>{chart.Title}</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>项目</th><th>值</th></tr>");
            foreach (var item in chart.Data)
            {
                html.AppendLine($"<tr><td>{item.Key}</td><td>{item.Value:F2}</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");
        }
        
        // 表格
        foreach (var table in report.Tables)
        {
            html.AppendLine($"<h2>{table.Title}</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr>");
            foreach (var header in table.Headers)
            {
                html.AppendLine($"<th>{header}</th>");
            }
            html.AppendLine("</tr>");
            
            foreach (var row in table.Rows)
            {
                html.AppendLine("<tr>");
                foreach (var cell in row)
                {
                    html.AppendLine($"<td>{cell}</td>");
                }
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");
        }
        
        // HTML尾部
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        await File.WriteAllTextAsync(outputPath, html.ToString());
        return outputPath;
    }

    public async Task<ReportData> GenerateBackupSummaryReportAsync(DateTime startDate, DateTime endDate)
    {
        var report = new ReportData
        {
            Title = "备份总结报告",
            Type = ReportType.BackupSummary
        };

        var tasks = await _storageService.LoadBackupTasksAsync();
        var allHistories = new List<BackupHistory>();

        foreach (var task in tasks)
        {
            var histories = await _storageService.LoadBackupHistoriesAsync(task.Id);
            allHistories.AddRange(histories.Where(h => 
                h.StartTime >= startDate && h.StartTime <= endDate));
        }

        // 统计数据
        var totalBackups = allHistories.Count;
        var successfulBackups = allHistories.Count(h => h.Status == BackupStatus.Completed);
        var failedBackups = allHistories.Count(h => h.Status == BackupStatus.Failed);
        var totalSize = allHistories.Sum(h => h.TotalSize);
        var totalFiles = allHistories.Sum(h => h.TotalFiles);

        report.Sections["Summary"] = new Dictionary<string, object>
        {
            ["TotalBackups"] = totalBackups,
            ["SuccessfulBackups"] = successfulBackups,
            ["FailedBackups"] = failedBackups,
            ["SuccessRate"] = totalBackups > 0 ? (double)successfulBackups / totalBackups * 100 : 0,
            ["TotalSize"] = totalSize,
            ["TotalFiles"] = totalFiles
        };

        // 图表：按状态分布
        report.Charts.Add(new ReportChart
        {
            Title = "备份状态分布",
            Type = ChartType.Pie,
            Data = new Dictionary<string, double>
            {
                ["成功"] = successfulBackups,
                ["失败"] = failedBackups,
                ["已取消"] = allHistories.Count(h => h.Status == BackupStatus.Cancelled)
            }
        });

        // 表格：备份详情
        var detailTable = new ReportTable
        {
            Title = "备份详情",
            Headers = new List<string> { "日期", "任务名称", "状态", "文件数", "大小", "耗时" }
        };

        foreach (var history in allHistories.OrderByDescending(h => h.StartTime))
        {
            var duration = history.EndTime.HasValue 
                ? (history.EndTime.Value - history.StartTime).ToString(@"hh\:mm\:ss")
                : "N/A";

            detailTable.Rows.Add(new List<string>
            {
                history.StartTime.ToString("yyyy-MM-dd HH:mm"),
                history.TaskName,
                history.Status.ToString(),
                history.TotalFiles.ToString(),
                FormatBytes(history.TotalSize),
                duration
            });
        }

        report.Tables.Add(detailTable);

        return report;
    }

    public async Task<ReportData> GenerateHealthReportAsync()
    {
        var report = new ReportData
        {
            Title = "备份健康检查报告",
            Type = ReportType.HealthCheck
        };

        var healthReport = await _healthCheckService.PerformFullCheckAsync();

        report.Sections["OverallHealth"] = new Dictionary<string, object>
        {
            ["Score"] = healthReport.OverallScore,
            ["TotalTasks"] = healthReport.TotalTasks,
            ["HealthyTasks"] = healthReport.HealthyTasks,
            ["WarningTasks"] = healthReport.WarningTasks,
            ["CriticalTasks"] = healthReport.CriticalTasks
        };

        // 图表：任务健康分布
        report.Charts.Add(new ReportChart
        {
            Title = "任务健康状态分布",
            Type = ChartType.Pie,
            Data = new Dictionary<string, double>
            {
                ["健康"] = healthReport.HealthyTasks,
                ["警告"] = healthReport.WarningTasks,
                ["严重"] = healthReport.CriticalTasks
            }
        });

        // 表格：任务健康详情
        var taskTable = new ReportTable
        {
            Title = "任务健康详情",
            Headers = new List<string> { "任务名称", "状态", "评分", "问题", "最后成功备份" }
        };

        foreach (var taskStatus in healthReport.TaskStatuses)
        {
            taskTable.Rows.Add(new List<string>
            {
                taskStatus.TaskName,
                taskStatus.Level.ToString(),
                taskStatus.Score.ToString("F0"),
                string.Join("; ", taskStatus.Issues),
                taskStatus.LastSuccessfulBackup != DateTime.MinValue 
                    ? taskStatus.LastSuccessfulBackup.ToString("yyyy-MM-dd")
                    : "从未"
            });
        }

        report.Tables.Add(taskTable);

        // 表格：健康建议
        var recommendationTable = new ReportTable
        {
            Title = "改进建议",
            Headers = new List<string> { "类别", "问题", "建议", "优先级" }
        };

        foreach (var rec in healthReport.Recommendations)
        {
            recommendationTable.Rows.Add(new List<string>
            {
                rec.Category,
                rec.Issue,
                rec.Recommendation,
                rec.Priority.ToString()
            });
        }

        report.Tables.Add(recommendationTable);

        return report;
    }

    public async Task<ReportData> GenerateStorageReportAsync()
    {
        var report = new ReportData
        {
            Title = "存储空间分析报告",
            Type = ReportType.StorageAnalysis
        };

        var storageReport = await _storageAnalysisService.AnalyzeStorageAsync();
        var driveUsages = await _storageAnalysisService.GetDriveUsageAsync();

        report.Sections["StorageSummary"] = new Dictionary<string, object>
        {
            ["TotalBackupSize"] = storageReport.TotalBackupSize,
            ["TotalAvailableSpace"] = storageReport.TotalAvailableSpace,
            ["AverageDailyGrowth"] = storageReport.AverageDailyGrowth
        };

        // 图表：按备份类型分布
        report.Charts.Add(new ReportChart
        {
            Title = "存储空间按备份类型分布",
            Type = ChartType.Pie,
            Data = storageReport.SizeByBackupType.ToDictionary(
                kvp => kvp.Key,
                kvp => (double)kvp.Value
            )
        });

        // 表格：驱动器使用情况
        var driveTable = new ReportTable
        {
            Title = "驱动器使用情况",
            Headers = new List<string> { "驱动器", "总容量", "已用", "可用", "使用率", "备份数据", "备份任务数" }
        };

        foreach (var drive in driveUsages)
        {
            driveTable.Rows.Add(new List<string>
            {
                drive.DriveName,
                FormatBytes(drive.TotalSize),
                FormatBytes(drive.UsedSize),
                FormatBytes(drive.FreeSize),
                $"{drive.UsagePercentage:F1}%",
                FormatBytes(drive.BackupDataSize),
                drive.BackupTaskCount.ToString()
            });
        }

        report.Tables.Add(driveTable);

        // 表格：最大备份
        var largestTable = new ReportTable
        {
            Title = "最大的备份任务",
            Headers = new List<string> { "任务名称", "大小", "文件数", "最后备份" }
        };

        foreach (var backup in storageReport.LargestBackups)
        {
            largestTable.Rows.Add(new List<string>
            {
                backup.TaskName,
                FormatBytes(backup.Size),
                backup.FileCount.ToString(),
                backup.LastBackup != DateTime.MinValue 
                    ? backup.LastBackup.ToString("yyyy-MM-dd")
                    : "N/A"
            });
        }

        report.Tables.Add(largestTable);

        return report;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
