using System;

namespace NxDataManager.Models;

/// <summary>
/// 计划任务执行历史记录
/// </summary>
public class ScheduledExecutionHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessedFiles { get; set; }
    public int TotalFiles { get; set; }
    public long ProcessedSize { get; set; }
    public long TotalSize { get; set; }
    public TimeSpan Duration { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public bool IsAutomatic { get; set; } = true; // true=计划自动执行，false=手动执行
}
