using System;
using System.Collections.Generic;

namespace NxDataManager.Models;

/// <summary>
/// 备份计划配置
/// </summary>
public class BackupSchedule
{
    public ScheduleType Type { get; set; } = ScheduleType.Manual;
    public DateTime StartTime { get; set; } = DateTime.Now;
    public int IntervalHours { get; set; } = 24;
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public int DayOfMonth { get; set; } = 1;
    public bool IsRecurring { get; set; } = true;
}

/// <summary>
/// 计划类型
/// </summary>
public enum ScheduleType
{
    Manual,      // 手动
    Daily,       // 每天
    Weekly,      // 每周
    Monthly,     // 每月
    Interval     // 间隔时间
}
