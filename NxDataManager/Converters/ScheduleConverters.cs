using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NxDataManager.Models;

namespace NxDataManager.Converters;

/// <summary>
/// 判断计划任务类型是否不是手动执行
/// </summary>
public class NotManualScheduleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduleType scheduleType)
        {
            return scheduleType != ScheduleType.Manual ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 计划任务类型显示转换器
/// </summary>
public class ScheduleTypeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduleType scheduleType)
        {
            return scheduleType switch
            {
                ScheduleType.Manual => "手动",
                ScheduleType.Daily => "每天",
                ScheduleType.Weekly => "每周",
                ScheduleType.Monthly => "每月",
                ScheduleType.Interval => "间隔",
                _ => scheduleType.ToString()
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转换为状态颜色 (启用=绿色, 禁用=灰色)
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)) // SuccessBrush
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)); // 灰色
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
