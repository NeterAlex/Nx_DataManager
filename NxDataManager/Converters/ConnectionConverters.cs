using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NxDataManager.Converters;

/// <summary>
/// 反转 Visibility 的转换器
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接类型转索引转换器（用于TabControl）
/// </summary>
public class ConnectionTypeToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string connectionType)
        {
            return connectionType == "SMB" ? 0 : 1;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index == 0 ? "SMB" : "WebDAV";
        }
        return "SMB";
    }
}
