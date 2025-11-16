using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 报告导出服务接口
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// 导出为PDF
    /// </summary>
    Task<string> ExportToPdfAsync(ReportData report, string outputPath);
    
    /// <summary>
    /// 导出为Excel
    /// </summary>
    Task<string> ExportToExcelAsync(ReportData report, string outputPath);
    
    /// <summary>
    /// 导出为HTML
    /// </summary>
    Task<string> ExportToHtmlAsync(ReportData report, string outputPath);
    
    /// <summary>
    /// 生成备份总结报告
    /// </summary>
    Task<ReportData> GenerateBackupSummaryReportAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// 生成健康检查报告
    /// </summary>
    Task<ReportData> GenerateHealthReportAsync();
    
    /// <summary>
    /// 生成存储分析报告
    /// </summary>
    Task<ReportData> GenerateStorageReportAsync();
    
    /// <summary>
    /// 导出历史记录为CSV
    /// </summary>
    Task ExportHistoryToCsvAsync(List<BackupHistory> histories, string outputPath);
    
    /// <summary>
    /// 导出历史记录为PDF报告
    /// </summary>
    Task ExportHistoryToPdfAsync(List<BackupHistory> histories, string outputPath);
    
    /// <summary>
    /// 导出历史记录为HTML报告
    /// </summary>
    Task ExportHistoryToHtmlAsync(List<BackupHistory> histories, string outputPath);
}

/// <summary>
/// 报告数据
/// </summary>
public class ReportData
{
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public ReportType Type { get; set; }
    public Dictionary<string, object> Sections { get; set; } = new();
    public List<ReportChart> Charts { get; set; } = new();
    public List<ReportTable> Tables { get; set; } = new();
}

/// <summary>
/// 报告类型
/// </summary>
public enum ReportType
{
    BackupSummary,
    HealthCheck,
    StorageAnalysis,
    Custom
}

/// <summary>
/// 报告图表
/// </summary>
public class ReportChart
{
    public string Title { get; set; } = string.Empty;
    public ChartType Type { get; set; }
    public Dictionary<string, double> Data { get; set; } = new();
}

/// <summary>
/// 图表类型
/// </summary>
public enum ChartType
{
    Pie,
    Bar,
    Line,
    Area
}

/// <summary>
/// 报告表格
/// </summary>
public class ReportTable
{
    public string Title { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}
