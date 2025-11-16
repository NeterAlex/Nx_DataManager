using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NxDataManager.Converters;

/// <summary>
/// 零值转可见性转换器
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转文本转换器
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string text)
        {
            var texts = text.Split('|');
            if (texts.Length == 2)
            {
                return boolValue ? texts[0] : texts[1];
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字节转GB转换器
/// </summary>
public class BytesToGBConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字节转MB转换器
/// </summary>
public class BytesToMBConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes / (1024.0 * 1024.0);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Null转可见性转换器
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isInverse = parameter as string == "Inverse";
        var isNull = value == null;
        
        if (isInverse)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Null转布尔转换器
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
