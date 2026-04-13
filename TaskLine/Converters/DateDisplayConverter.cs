using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskLine.Converters;

/// <summary>
/// 将 <see cref="DateTime"/> 转换为可读的中文日期标签。
/// 今天显示"今天"，昨天显示"昨天"，其余显示"M月d日"。
/// </summary>
[ValueConversion(typeof(DateTime), typeof(string))]
public class DateDisplayConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
            return value;

        var today = DateTime.Today;

        if (date.Date == today)
            return "今天";

        if (date.Date == today.AddDays(-1))
            return "昨天";

        return date.ToString("M月d日", culture);
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
