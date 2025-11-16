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

    public async Task ExportHistoryToCsvAsync(List<BackupHistory> histories, string outputPath)
    {
        var csv = new StringBuilder();
        
        // CSV 表头
        csv.AppendLine("日期,任务名称,备份类型,状态,文件总数,成功文件,失败文件,总大小,耗时,平均速度,源路径,目标路径,错误信息");
        
        // CSV 数据行
        foreach (var history in histories.OrderByDescending(h => h.StartTime))
        {
            var duration = history.EndTime.HasValue 
                ? (history.EndTime.Value - history.StartTime).ToString(@"hh\:mm\:ss")
                : "N/A";
                
            var speed = history.Duration.TotalSeconds > 0
                ? $"{history.AverageSpeed:F2} MB/s"
                : "N/A";
                
            var errorMsg = history.ErrorMessage?.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ") ?? "";
            
            csv.AppendLine($"\"{history.StartTime:yyyy-MM-dd HH:mm:ss}\"," +
                          $"\"{history.TaskName}\"," +
                          $"\"{history.BackupType}\"," +
                          $"\"{history.Status}\"," +
                          $"{history.TotalFiles}," +
                          $"{history.SuccessFiles}," +
                          $"{history.FailedFiles}," +
                          $"\"{FormatBytes(history.TotalSize)}\"," +
                          $"\"{duration}\"," +
                          $"\"{speed}\"," +
                          $"\"{history.SourcePath}\"," +
                          $"\"{history.DestinationPath}\"," +
                          $"\"{errorMsg}\"");
        }
        
        await File.WriteAllTextAsync(outputPath, csv.ToString(), Encoding.UTF8);
    }

    public async Task ExportHistoryToPdfAsync(List<BackupHistory> histories, string outputPath)
    {
        // 创建 HTML 报告，然后转换为 PDF（需要额外的库）
        // 这里先生成 HTML 报告作为替代
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine("<title>备份历史报告</title>");
        html.AppendLine("<style>");
        html.AppendLine(@"
            body {
                font-family: 'Segoe UI', Arial, sans-serif;
                margin: 40px;
                background: #f5f5f5;
            }
            .container {
                max-width: 1400px;
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
            .summary {
                display: grid;
                grid-template-columns: repeat(5, 1fr);
                gap: 20px;
                margin: 30px 0;
            }
            .stat-card {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                padding: 20px;
                border-radius: 8px;
                text-align: center;
            }
            .stat-card.success {
                background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
            }
            .stat-card.error {
                background: linear-gradient(135deg, #f44336 0%, #e53935 100%);
            }
            .stat-card.info {
                background: linear-gradient(135deg, #2196F3 0%, #1976D2 100%);
            }
            .stat-card.warning {
                background: linear-gradient(135deg, #FF9800 0%, #F57C00 100%);
            }
            .stat-value {
                font-size: 32px;
                font-weight: bold;
                margin: 10px 0;
            }
            .stat-label {
                font-size: 14px;
                opacity: 0.9;
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
                font-size: 13px;
            }
            td {
                padding: 10px;
                border-bottom: 1px solid #ddd;
                font-size: 12px;
            }
            tr:hover {
                background: #f9f9f9;
            }
            .status-badge {
                padding: 4px 12px;
                border-radius: 12px;
                font-weight: bold;
                font-size: 11px;
            }
            .status-completed {
                background: #C8E6C9;
                color: #2E7D32;
            }
            .status-failed {
                background: #FFCDD2;
                color: #C62828;
            }
            .status-cancelled {
                background: #FFE0B2;
                color: #E65100;
            }
            .footer {
                margin-top: 30px;
                padding-top: 20px;
                border-top: 1px solid #ddd;
                text-align: center;
                color: #999;
                font-size: 12px;
            }
        ");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");
        
        // 标题
        html.AppendLine("<h1>📊 备份历史报告</h1>");
        html.AppendLine($"<p style='color: #999;'>生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        
        // 统计摘要
        var totalHistories = histories.Count;
        var successCount = histories.Count(h => h.Status == BackupStatus.Completed);
        var failureCount = histories.Count(h => h.Status == BackupStatus.Failed);
        var totalSize = histories.Sum(h => h.TotalSize);
        var avgSpeed = histories.Where(h => h.Duration.TotalSeconds > 0)
                                .Average(h => h.AverageSpeed);
        
        html.AppendLine("<div class='summary'>");
        html.AppendLine($@"
            <div class='stat-card'>
                <div class='stat-label'>总记录数</div>
                <div class='stat-value'>{totalHistories}</div>
            </div>
            <div class='stat-card success'>
                <div class='stat-label'>成功</div>
                <div class='stat-value'>{successCount}</div>
            </div>
            <div class='stat-card error'>
                <div class='stat-label'>失败</div>
                <div class='stat-value'>{failureCount}</div>
            </div>
            <div class='stat-card info'>
                <div class='stat-label'>总大小</div>
                <div class='stat-value'>{FormatBytes(totalSize)}</div>
            </div>
            <div class='stat-card warning'>
                <div class='stat-label'>平均速度</div>
                <div class='stat-value'>{avgSpeed:F1} MB/s</div>
            </div>
        ");
        html.AppendLine("</div>");
        
        // 详细表格
        html.AppendLine("<h2>📋 详细记录</h2>");
        html.AppendLine("<table>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>日期</th>");
        html.AppendLine("<th>任务名称</th>");
        html.AppendLine("<th>类型</th>");
        html.AppendLine("<th>状态</th>");
        html.AppendLine("<th>文件数</th>");
        html.AppendLine("<th>大小</th>");
        html.AppendLine("<th>耗时</th>");
        html.AppendLine("<th>速度</th>");
        html.AppendLine("</tr>");
        
        foreach (var history in histories.OrderByDescending(h => h.StartTime))
        {
            var statusClass = history.Status switch
            {
                BackupStatus.Completed => "status-completed",
                BackupStatus.Failed => "status-failed",
                BackupStatus.Cancelled => "status-cancelled",
                _ => ""
            };
            
            var duration = history.EndTime.HasValue 
                ? (history.EndTime.Value - history.StartTime).ToString(@"hh\:mm\:ss")
                : "N/A";
                
            var speed = history.Duration.TotalSeconds > 0
                ? $"{history.AverageSpeed:F2} MB/s"
                : "N/A";
            
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{history.StartTime:yyyy-MM-dd HH:mm}</td>");
            html.AppendLine($"<td>{history.TaskName}</td>");
            html.AppendLine($"<td>{history.BackupType}</td>");
            html.AppendLine($"<td><span class='status-badge {statusClass}'>{history.Status}</span></td>");
            html.AppendLine($"<td>{history.SuccessFiles}/{history.TotalFiles}</td>");
            html.AppendLine($"<td>{FormatBytes(history.TotalSize)}</td>");
            html.AppendLine($"<td>{duration}</td>");
            html.AppendLine($"<td>{speed}</td>");
            html.AppendLine("</tr>");
        }
        
        html.AppendLine("</table>");
        
        // 页脚
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>此报告由 NxDataManager 备份管理器自动生成</p>");
        html.AppendLine($"<p>© {DateTime.Now.Year} NxDataManager. All rights reserved.</p>");
        html.AppendLine("</div>");
        
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        // 暂时保存为 HTML（完整的 PDF 需要额外的库）
        var htmlPath = outputPath.Replace(".pdf", ".html");
        await File.WriteAllTextAsync(htmlPath, html.ToString(), Encoding.UTF8);
        
        // TODO: 使用 HTML to PDF 转换库（如 IronPdf, SelectPdf）将 HTML 转换为 PDF
        throw new NotImplementedException($"PDF 导出功能开发中，已生成 HTML 报告: {htmlPath}");
    }

    public async Task ExportHistoryToHtmlAsync(List<BackupHistory> histories, string outputPath)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine("<title>备份历史报告</title>");
        html.AppendLine("<style>");
        html.AppendLine(@"
            body {
                font-family: 'Segoe UI', Arial, sans-serif;
                margin: 0;
                padding: 0;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            }
            .container {
                max-width: 1400px;
                margin: 40px auto;
                background: white;
                border-radius: 16px;
                box-shadow: 0 10px 40px rgba(0,0,0,0.2);
                overflow: hidden;
            }
            .header {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                padding: 40px;
                text-align: center;
            }
            .header h1 {
                margin: 0 0 10px 0;
                font-size: 36px;
            }
            .header p {
                margin: 0;
                opacity: 0.9;
                font-size: 14px;
            }
            .summary {
                display: grid;
                grid-template-columns: repeat(5, 1fr);
                gap: 0;
                border-bottom: 1px solid #ddd;
            }
            .stat-card {
                padding: 30px;
                text-align: center;
                border-right: 1px solid #ddd;
            }
            .stat-card:last-child {
                border-right: none;
            }
            .stat-value {
                font-size: 36px;
                font-weight: bold;
                margin: 10px 0;
                color: #667eea;
            }
            .stat-card.success .stat-value { color: #4CAF50; }
            .stat-card.error .stat-value { color: #f44336; }
            .stat-card.info .stat-value { color: #2196F3; }
            .stat-card.warning .stat-value { color: #FF9800; }
            .stat-label {
                font-size: 14px;
                color: #666;
                text-transform: uppercase;
                letter-spacing: 1px;
            }
            .content {
                padding: 40px;
            }
            h2 {
                color: #333;
                margin-bottom: 20px;
                padding-bottom: 10px;
                border-bottom: 2px solid #667eea;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin: 20px 0;
            }
            th {
                background: #f5f5f5;
                color: #333;
                padding: 15px 12px;
                text-align: left;
                font-size: 13px;
                font-weight: 600;
                text-transform: uppercase;
                letter-spacing: 0.5px;
            }
            td {
                padding: 12px;
                border-bottom: 1px solid #eee;
                font-size: 13px;
            }
            tr:hover {
                background: #f9f9f9;
            }
            .status-badge {
                padding: 6px 14px;
                border-radius: 20px;
                font-weight: 600;
                font-size: 11px;
                text-transform: uppercase;
                letter-spacing: 0.5px;
            }
            .status-completed {
                background: #C8E6C9;
                color: #2E7D32;
            }
            .status-failed {
                background: #FFCDD2;
                color: #C62828;
            }
            .status-cancelled {
                background: #FFE0B2;
                color: #E65100;
            }
            .footer {
                background: #f5f5f5;
                padding: 30px;
                text-align: center;
                color: #999;
                font-size: 12px;
                border-top: 1px solid #ddd;
            }
            .footer p {
                margin: 5px 0;
            }
        ");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");
        
        // 标题栏
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>📊 备份历史报告</h1>");
        html.AppendLine($"<p>生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("</div>");
        
        // 统计摘要
        var totalHistories = histories.Count;
        var successCount = histories.Count(h => h.Status == BackupStatus.Completed);
        var failureCount = histories.Count(h => h.Status == BackupStatus.Failed);
        var totalSize = histories.Sum(h => h.TotalSize);
        var avgSpeed = histories.Any(h => h.Duration.TotalSeconds > 0)
            ? histories.Where(h => h.Duration.TotalSeconds > 0).Average(h => h.AverageSpeed)
            : 0;
        
        html.AppendLine("<div class='summary'>");
        html.AppendLine($@"
            <div class='stat-card'>
                <div class='stat-label'>总记录数</div>
                <div class='stat-value'>{totalHistories}</div>
            </div>
            <div class='stat-card success'>
                <div class='stat-label'>成功</div>
                <div class='stat-value'>{successCount}</div>
            </div>
            <div class='stat-card error'>
                <div class='stat-label'>失败</div>
                <div class='stat-value'>{failureCount}</div>
            </div>
            <div class='stat-card info'>
                <div class='stat-label'>总大小</div>
                <div class='stat-value'>{FormatBytes(totalSize)}</div>
            </div>
            <div class='stat-card warning'>
                <div class='stat-label'>平均速度</div>
                <div class='stat-value'>{avgSpeed:F1}<br/><span style='font-size:14px;'>MB/s</span></div>
            </div>
        ");
        html.AppendLine("</div>");
        
        // 详细内容
        html.AppendLine("<div class='content'>");
        html.AppendLine("<h2>📋 详细记录</h2>");
        html.AppendLine("<table>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>日期时间</th>");
        html.AppendLine("<th>任务名称</th>");
        html.AppendLine("<th>备份类型</th>");
        html.AppendLine("<th>状态</th>");
        html.AppendLine("<th>文件数</th>");
        html.AppendLine("<th>总大小</th>");
        html.AppendLine("<th>耗时</th>");
        html.AppendLine("<th>平均速度</th>");
        html.AppendLine("</tr>");
        
        foreach (var history in histories.OrderByDescending(h => h.StartTime))
        {
            var statusClass = history.Status switch
            {
                BackupStatus.Completed => "status-completed",
                BackupStatus.Failed => "status-failed",
                BackupStatus.Cancelled => "status-cancelled",
                _ => ""
            };
            
            var duration = history.EndTime.HasValue 
                ? (history.EndTime.Value - history.StartTime).ToString(@"hh\:mm\:ss")
                : "N/A";
                
            var speed = history.Duration.TotalSeconds > 0
                ? $"{history.AverageSpeed:F2} MB/s"
                : "N/A";
            
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{history.StartTime:yyyy-MM-dd HH:mm:ss}</td>");
            html.AppendLine($"<td><strong>{history.TaskName}</strong></td>");
            html.AppendLine($"<td>{history.BackupType}</td>");
            html.AppendLine($"<td><span class='status-badge {statusClass}'>{history.Status}</span></td>");
            html.AppendLine($"<td>{history.SuccessFiles}/{history.TotalFiles}</td>");
            html.AppendLine($"<td>{FormatBytes(history.TotalSize)}</td>");
            html.AppendLine($"<td>{duration}</td>");
            html.AppendLine($"<td>{speed}</td>");
            html.AppendLine("</tr>");
        }
        
        html.AppendLine("</table>");
        html.AppendLine("</div>");
        
        // 页脚
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p><strong>NxDataManager 备份管理器</strong></p>");
        html.AppendLine($"<p>© {DateTime.Now.Year} NxDataManager. All rights reserved.</p>");
        html.AppendLine("<p>此报告由系统自动生成</p>");
        html.AppendLine("</div>");
        
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        await File.WriteAllTextAsync(outputPath, html.ToString(), Encoding.UTF8);
    }
}
